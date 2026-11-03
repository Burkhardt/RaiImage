using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage;

/// <summary>Streaming, read-only container identity. A digest is followed by byte confirmation.</summary>
public static class MediaContent
{
	public readonly record struct Stamp(long Size, DateTimeOffset Modified);
	public static Stamp Inspect(RaiFile file) => new(file.Length, file.LastWriteTimeUtc);
	public static void RequireUnchanged(RaiFile file, Stamp before)
	{
		if (!file.Exists() || Inspect(file) != before) throw new IOException($"File changed during comparison: {file.FullName}");
	}
	public static async Task<string> Sha256Async(RaiFile file, CancellationToken ct = default)
	{
		var stamp = Inspect(file);
		await using var stream = file.OpenRead();
		var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
		RequireUnchanged(file, stamp);
		return Convert.ToHexStringLower(hash);
	}
	public static async Task<bool> ByteEqualsAsync(RaiFile left, RaiFile right, CancellationToken ct = default)
	{
		var a = Inspect(left); var b = Inspect(right);
		if (a.Size != b.Size) return false;
		await using var first = left.OpenRead();
		await using var second = right.OpenRead();
		var x = new byte[81920]; var y = new byte[81920];
		bool same = true;
		while (true)
		{
			var nx = await first.ReadAtLeastAsync(x, x.Length, throwOnEndOfStream: false, ct).ConfigureAwait(false);
			var ny = await second.ReadAtLeastAsync(y, y.Length, throwOnEndOfStream: false, ct).ConfigureAwait(false);
			if (nx != ny || !x.AsSpan(0, nx).SequenceEqual(y.AsSpan(0, ny))) { same = false; break; }
			if (nx == 0) break;
		}
		RequireUnchanged(left, a); RequireUnchanged(right, b);
		return same;
	}
}
