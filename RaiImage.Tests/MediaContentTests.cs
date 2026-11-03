using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsLib;
using Xunit;

namespace RaiImage.Tests;

public sealed class MediaContentTests : IDisposable
{
	private readonly RaiPath root = Os.TempDir / ("cr075-media-" + Guid.NewGuid().ToString("N"));
	public MediaContentTests() => root.mkdir();
	[Fact]
	public async Task SameSizeDifferentContentsNeverPassByteComparison()
	{
		var a = new TextFile(root, "a.mov", content: "123456"); var b = new TextFile(root, "b.mp4", content: "654321");
		Assert.NotEqual(await MediaContent.Sha256Async(a), await MediaContent.Sha256Async(b));
		Assert.False(await MediaContent.ByteEqualsAsync(a, b));
		Assert.True(await MediaContent.ByteEqualsAsync(a, a));
	}
	[Fact]
	public async Task StreamingByteComparisonHandlesLargeBuffersAndCancellation()
	{
		var a = new RaiFile(root, "a.mov"); var b = new RaiFile(root, "b.mp4");
		var content = new byte[81920 * 3 + 7]; new Random(42).NextBytes(content);
		await a.WriteFromAsync(new MemoryStream(content)); await b.WriteFromAsync(new MemoryStream(content));
		Assert.True(await MediaContent.ByteEqualsAsync(a, b));
		using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MediaContent.Sha256Async(a, cancellation.Token));
	}
	private RaiFile Video(string name, string source, bool lossless = true)
	{
		var file = new RaiFile(root, name); var command = new FfmpegCommand(); Assert.True(command.IsAvailable());
		var run = command.Run(["-v", "error", "-f", "lavfi", "-i", source, "-t", "1", "-an", "-c:v", "libx264", "-threads", "1", "-crf", lossless ? "0" : "4", "-pix_fmt", "yuv420p", file.FullName]);
		Assert.Equal(0, run.ExitCode); return file;
	}
	[Fact]
	public async Task CompleteVideoFramesMatchAcrossContainersAndAudioIsNotClaimed()
	{
		var a = Video("a.mov", "testsrc2=size=32x32:rate=5"); var b = Video("b.mp4", "testsrc2=size=32x32:rate=5");
		var result = await new MediaVisualComparison().CompareAsync(a, b);
		Assert.NotNull(result); Assert.False(result.AudioCompared); Assert.False(result.ByteComparisonVerified);
		Assert.Equal("DecodedExact", result.Method); Assert.Equal(5, result.FramesCompared);
	}
	[Fact]
	public async Task LossyEncodeUsesFullSequenceSsim()
	{
		var a = Video("source.mov", "testsrc2=size=32x32:rate=5"); var b = Video("lossy.mp4", "testsrc2=size=32x32:rate=5", lossless: false);
		var result = await new MediaVisualComparison().CompareAsync(a, b);
		Assert.NotNull(result); Assert.Equal("Perceptual", result.Method); Assert.Equal(5, result.FramesCompared);
		Assert.True(result.MeanSsim >= MediaVisualComparison.MeanThreshold);
	}
	[Fact]
	public async Task DifferentMiddleFrameCannotBeHiddenByMatchingFirstAndLastFrames()
	{
		var a = Video("a.mov", "color=red:size=32x32:rate=5");
		var b = Video("b.mp4", "color=red:size=32x32:rate=5,drawbox=color=blue:t=fill:enable='eq(n,2)'");
		Assert.Null(await new MediaVisualComparison().CompareAsync(a, b));
	}
	[Fact]
	public async Task MissingVisualToolsFailExplicitly()
	{
		var a = new TextFile(root, "a.mov", content: "data");
		await Assert.ThrowsAsync<InvalidOperationException>(() => new MediaVisualComparison(new FfmpegCommand("cr075-no-such-ffmpeg"))
			.CompareAsync(a, a));
	}
	public void Dispose() => root.rmdir(depth: 10, deleteFiles: true);
}
