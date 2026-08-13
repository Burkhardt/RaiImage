using System.Runtime.CompilerServices;
using OsLib;

namespace RaiImage.Tests;

public class AsyncIngestionTests
{
	[Fact]
	public async Task ImageTreeFile_WriteFromAsync_AcceptsByteChunksWithoutStreamBoundary()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var root = Os.TempDir / "RAIkeep" / "raiimage-tests" / "async-ingestion" / Guid.NewGuid().ToString("N");
		var target = new ImageTreeFile(root, "ChunkedImage", string.Empty, "bin");
		try
		{
			await target.WriteFromAsync(Chunks(cancellationToken), cancellationToken);

			Assert.True(target.Exists());
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, await target.ReadAllBytesAsync(cancellationToken));
		}
		finally
		{
			if (root.Exists())
				root.rmdir(depth: 5, deleteFiles: true);
		}
	}

	private static async IAsyncEnumerable<byte[]> Chunks(
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await Task.Yield();
		cancellationToken.ThrowIfCancellationRequested();
		yield return new byte[] { 1, 2 };
		yield return Array.Empty<byte>();
		yield return new byte[] { 3, 4, 5 };
	}
}
