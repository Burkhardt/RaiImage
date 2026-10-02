using System.Globalization;
using System.Text.Json;

namespace RaiImage.Tests;

public sealed class ImageExifDateTimeTests
{
    [Theory]
    [InlineData("-07:00", "2026-09-29T00:40:31Z")]
    [InlineData("+02:00", "2026-09-28T15:40:31Z")]
    [InlineData("+05:30", "2026-09-28T12:10:31Z")]
    [InlineData("+00:00", "2026-09-28T17:40:31Z")]
    public void ExplicitOffsetProducesCorrectUtcInstant(string offset, string utc)
    {
        var captured = ImageExif.ParseDateTime("2026:09:28 17:40:31", offset);
        Assert.Equal(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture).UtcDateTime, captured.UtcDateTime);
    }

    [Fact]
    public void ConvertsCapturePairToIso8601JsonAndUtc()
    {
        var exif = new Dictionary<string, string>
        {
            ["DateTimeOriginal"] = "2026:09:28 17:40:31", ["OffsetTimeOriginal"] = "-07:00"
        };
        var captured = ImageExif.GetCaptureTime(exif);
        Assert.NotNull(captured);
        Assert.Equal("{\"Captured\":\"2026-09-28T17:40:31-07:00\"}", JsonSerializer.Serialize(new { Captured = captured }));
        Assert.Equal(new DateTime(2026, 9, 29, 0, 40, 31, DateTimeKind.Utc), captured.Value.UtcDateTime);
    }

    [Theory]
    [InlineData("DateTimeOriginal", "OffsetTimeOriginal", -7)]
    [InlineData("DateTimeDigitized", "OffsetTimeDigitized", 2)]
    [InlineData("DateTime", "OffsetTime", 5)]
    public void EachDateUsesOnlyItsOwnCompanion(string dateTag, string offsetTag, int expectedHours)
    {
        var exif = new Dictionary<string, string>
        {
            ["DateTimeOriginal"] = "2026:09:28 17:40:31", ["OffsetTimeOriginal"] = "-07:00",
            ["DateTimeDigitized"] = "2026:09:28 17:40:31", ["OffsetTimeDigitized"] = "+02:00",
            ["DateTime"] = "2026:09:28 17:40:31", ["OffsetTime"] = "+05:00"
        };
        Assert.Equal(TimeSpan.FromHours(expectedHours), ImageExif.GetDateTime(exif, dateTag)!.Value.Offset);
        exif.Remove(offsetTag);
        Assert.Null(ImageExif.GetDateTime(exif, dateTag));
    }

    [Fact]
    public void CaptureDoesNotFallBackToDigitizedModificationOrFilesystemDates()
    {
        var exif = new Dictionary<string, string>
        {
            ["DateTimeDigitized"] = "2026:09:28 17:40:31", ["OffsetTimeDigitized"] = "-07:00",
            ["DateTime"] = "2026:10:01 10:00:00", ["OffsetTime"] = "-07:00",
            ["date:create"] = "2026-10-01T10:00:00-07:00"
        };
        Assert.Null(ImageExif.GetCaptureTime(exif));
        exif["DateTimeOriginal"] = "2026:09:28 17:40:31";
        Assert.Null(ImageExif.GetCaptureTime(exif));
    }

    [Fact]
    public void ExplicitOffsetDisambiguatesAutumnOverlap()
    {
        var first = ImageExif.ParseDateTime("2026:11:01 01:30:00", "-07:00");
        var second = ImageExif.ParseDateTime("2026:11:01 01:30:00", "-08:00");
        Assert.Equal(TimeSpan.FromHours(1), second - first);
    }

    [Theory]
    [InlineData("2026:02:30 17:40:31", "-07:00")]
    [InlineData("2026:09:28 17:40:31", "")]
    [InlineData("2026:09:28 17:40:31", "PST")]
    [InlineData("2026:09:28 17:40:31", "+25:00")]
    [InlineData("2026:09:28 17:40:31", "+05:75")]
    public void RejectsInvalidDatesAndOffsets(string date, string offset)
    {
        Assert.Throws<FormatException>(() => ImageExif.ParseDateTime(date, offset));
    }
}
