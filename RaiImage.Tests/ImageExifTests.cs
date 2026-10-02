using OsLib;
using System.Text.Json;

namespace RaiImage.Tests;

public sealed class ImageExifTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "RAIkeep-exif-" + Guid.NewGuid().ToString("N"));
    [Theory]
    [InlineData("*", 3)]
    [InlineData("DateTimeOriginal", 1)]
    [InlineData("DateTime*", 2)]
    [InlineData("NoSuchTag", 0)]
    [InlineData("DateTime,DateTimeOriginal", 2)]
    [InlineData("DateTime, DateTimeOriginal,DateTime", 2)]
    public async Task ReadsActualExifAsAnObjectWithoutChangingImage(string selector, int count)
    {
        Directory.CreateDirectory(root);
        var image = Path.Combine(root, "metadata sample.jpg");
        var original = Convert.FromBase64String("/9j/4QBoRXhpZgAASUkqAAgAAAACADIBAgAUAAAAOAAAAGmHBAABAAAAJgAAAAAAAAABAAOQAgAUAAAATAAAAAAAAAAyMDI2OjA5OjI4IDIwOjI0OjMxADIwMjY6MDk6MjggMTc6NDA6MzEA/+AAEEpGSUYAAQEAAAEAAQAA/9sAQwADAgICAgIDAgICAwMDAwQGBAQEBAQIBgYFBgkICgoJCAkJCgwPDAoLDgsJCQ0RDQ4PEBAREAoMEhMSEBMPEBAQ/8AACwgAAgACAQERAP/EABQAAQAAAAAAAAAAAAAAAAAAAAn/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oACAEBAAA/ACqf/9k=");
        File.WriteAllBytes(image, original);
        var exif = await ImageExif.ReadAsync(new RaiFile(image), selector, TestContext.Current.CancellationToken);
        Assert.Equal(count, exif.Count);
        if (exif.ContainsKey("DateTimeOriginal")) Assert.Equal("2026:09:28 17:40:31", exif["DateTimeOriginal"]);
        if (exif.ContainsKey("DateTime")) Assert.Equal("2026:09:28 20:24:31", exif["DateTime"]);
        Assert.Equal(original, File.ReadAllBytes(image));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(exif));
        Assert.Equal(JsonValueKind.Object, json.RootElement.ValueKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DateTimeOriginal] %[filename")]
    [InlineData("*; echo injected")]
    [InlineData("DateTime,")]
    public void RejectsFormatInjection(string selector) => Assert.Throws<ArgumentException>(() => ImageExif.ValidateSelector(selector));

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
