using System.Text.Json;

namespace RaiImage.Tests;

public sealed class ExifMetadataTests
{
    [Fact]
    public void SelectionFetchesOnlyMatchingDateCompanionAndEmitsTypedDates()
    {
        var values = new Dictionary<string, string>
        {
            ["DateTimeOriginal"] = "2026:09:28 17:40:31", ["OffsetTimeOriginal"] = "-07:00",
            ["DateTimeDigitized"] = "2026:09:28 20:00:00", ["OffsetTimeDigitized"] = "+02:00",
            ["DateTime"] = "2026:10:01 10:00:00"
        };
        var metadata = ExifMetadata.FromExif(values, "DateTimeOriginal,DateTimeDigitized,DateTime");
        Assert.Equal(TimeSpan.FromHours(-7), metadata.DateTimeOriginal!.Value.Offset);
        Assert.Equal(TimeSpan.FromHours(2), metadata.DateTimeDigitized!.Value.Offset);
        Assert.Null(metadata.DateTime);
        Assert.Equal(values["DateTime"], metadata.Unconverted!["DateTime"]);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(metadata));
        Assert.Equal("2026-09-28T17:40:31-07:00", json.RootElement.GetProperty("DateTimeOriginal").GetString());
        Assert.False(json.RootElement.TryGetProperty("Captured", out _));
        Assert.False(json.RootElement.TryGetProperty("OffsetTimeOriginal", out _));
        Assert.Single(metadata.Diagnostics);
    }

    [Fact]
    public void GroupsRelatedFieldsWithoutLosingExactFractionsOrUnknownMetadata()
    {
        var metadata = ExifMetadata.FromExif(new Dictionary<string, string>
        {
            ["Thumbnail.Compression"] = "6", ["Thumbnail.XResolution"] = "72/1",
            ["LensModel"] = "E 70-180mm F2.8 A056", ["LensSpecification"] = "700/10, 1800/10, 28/10, 28/10",
            ["FocalLength"] = "1220/10", ["FocalPlaneXResolution"] = "54965632/32768",
            ["ExposureMode"] = "1", ["ExposureTime"] = "1/250", ["UserComment"] = "quoted \"metadata\"\nnext line"
        });
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(metadata));
        var root = json.RootElement;
        Assert.Equal(6, root.GetProperty("Thumbnail").GetProperty("Compression").GetInt32());
        Assert.Equal(4, root.GetProperty("Lens").GetProperty("Specification").GetArrayLength());
        Assert.Equal(122, root.GetProperty("FocalLength").GetProperty("Value").GetDouble());
        Assert.Equal(250, root.GetProperty("Exposure").GetProperty("Time").GetProperty("Denominator").GetInt64());
        Assert.Equal(0.004, root.GetProperty("Exposure").GetProperty("Time").GetProperty("Value").GetDouble());
        Assert.Equal("quoted \"metadata\"\nnext line", root.GetProperty("UserComment").GetString());
    }

    [Fact]
    public void InvalidRationalsAndDatesRemainAvailableWithoutInventingTypedValues()
    {
        var metadata = ExifMetadata.FromExif(new Dictionary<string, string>
        {
            ["ExposureTime"] = "1/0", ["DateTimeOriginal"] = "invalid", ["OffsetTimeOriginal"] = "-07:00"
        });
        Assert.Null(metadata.DateTimeOriginal);
        Assert.Null(metadata.Exposure);
        Assert.Equal("1/0", metadata.Unconverted!["ExposureTime"]);
        Assert.Equal("-07:00", metadata.Unconverted["OffsetTimeOriginal"]);
        Assert.Equal(2, metadata.Diagnostics.Count);
    }

    [Fact]
    public void DottedThumbnailSelectorDoesNotSelectOtherGroups()
    {
        var metadata = ExifMetadata.FromExif(new Dictionary<string, string> { ["Thumbnail.XResolution"] = "72/1", ["LensModel"] = "Camera" }, "Thumbnail.*");
        Assert.NotNull(metadata.Thumbnail);
        Assert.Null(metadata.Lens);
    }
}
