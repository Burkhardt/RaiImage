namespace RaiImage.Tests;

public sealed class MediaTypesTests
{
	[Fact]
	public void MediaTypes_AcceptsCmafSegmentsManifestsAndJson()
	{
		foreach (var ext in new[] { "m4s", "mpd", "m3u8", "json", "vtt" })
		{
			Assert.Contains(ext, MediaTypes.Default.Array);
			Assert.False(MediaTypes.IsVideo(ext));
			Assert.Equal(ext == "json", MediaTypes.IsCatalogOrMetadata("." + ext.ToUpperInvariant()));
			Assert.Equal(ext != "json", MediaTypes.IsStreamingAsset("." + ext.ToUpperInvariant()));
		}
	}

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
