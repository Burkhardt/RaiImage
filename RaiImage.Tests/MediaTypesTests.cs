namespace RaiImage.Tests;

public sealed class MediaTypesTests
{
    [Theory]
    [InlineData("mp4")]
    [InlineData(".mov")]
    [InlineData("WEBM")]
    [InlineData("mkv")]
    [InlineData("avi")]
    public void VideoContainersAreRecognized(string extension)
    {
        Assert.True(MediaTypes.IsVideo(extension));
        Assert.Contains(extension.TrimStart('.'), MediaTypes.Default.Array, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void StillImageTypesRemainSeparateFromVideoContainers()
    {
        Assert.False(MediaTypes.IsVideo("jpg"));
        Assert.Contains("svg", MediaTypes.Default.Array);
        Assert.DoesNotContain("mp4", ImageTypes.Default.Array);
    }
}
