using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage;

public sealed class FfmpegCommand : CliCommand
{
	public FfmpegCommand(string executable = "ffmpeg") : base(executable, "ffmpeg") { }
}
public sealed class FfprobeCommand : CliCommand
{
	public FfprobeCommand(string executable = "ffprobe") : base(executable, "ffmpeg") { }
}

/// <summary>Complete visual comparison under strict-v1; audio is intentionally never compared.</summary>
public sealed class MediaVisualComparison
{
	public const double MeanThreshold = 0.995, MinimumThreshold = 0.99;
	private readonly FfmpegCommand ffmpeg;
	private readonly FfprobeCommand ffprobe;
	private readonly ImageMagickCommand magick;
	public MediaVisualComparison(FfmpegCommand ffmpeg = null, FfprobeCommand ffprobe = null, ImageMagickCommand magick = null)
	{
		this.ffmpeg = ffmpeg ?? new(); this.ffprobe = ffprobe ?? new(); this.magick = magick ?? new();
	}
	public async Task<IorgMatchEvidence> CompareAsync(RaiFile a, RaiFile b, CancellationToken ct = default)
	{
		var sa = MediaContent.Inspect(a); var sb = MediaContent.Inspect(b);
		var av = MediaTypes.IsVideo(a.Ext); var bv = MediaTypes.IsVideo(b.Ext);
		if (av != bv) return null;
		IorgMatchEvidence result = av ? await CompareVideo(a, b, ct) : await CompareImage(a, b, ct);
		MediaContent.RequireUnchanged(a, sa); MediaContent.RequireUnchanged(b, sb);
		return result;
	}
	public async Task ValidateAsync(RaiFile file, CancellationToken ct = default)
	{
		var video = MediaTypes.IsVideo(file.Ext); RequireTools(video);
		if (video) _ = await Probe(file, ct); else _ = await ImageSize(file, ct);
	}
	public void RequireTools(bool video)
	{
		if (video ? !ffmpeg.IsAvailable() || !ffprobe.IsAvailable() : !magick.IsAvailable())
			throw new InvalidOperationException(video ? "Visual video comparison requires ffmpeg and ffprobe." : "Visual image comparison requires ImageMagick.");
	}
	private static IorgMatchEvidence Evidence(bool exact, double mean, double minimum, long frames, string decoder, string normalization)
		=> new() { Profile = "strict-v1", Method = exact ? "DecodedExact" : "Perceptual", Algorithm = exact ? "SHA-256 decoded sequence" : "SSIM",
			AudioCompared = false, ByteComparisonVerified = false, MeanSsim = mean, MinimumSsim = minimum,
			MeanThreshold = MeanThreshold, MinimumThreshold = MinimumThreshold, FramesCompared = frames, Decoder = decoder, Normalization = normalization };
	private static async Task<string> ToolText(CliCommand tool, string[] args, CancellationToken ct)
	{
		return await tool.RunBinaryAsync(args, async (stream, token) =>
		{
			using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
			var text = new char[65537];
			var n = await reader.ReadBlockAsync(text.AsMemory(), token);
			if (n > 65536) throw new InvalidOperationException("Media metadata exceeded the bounded response limit.");
			return new string(text, 0, n);
		}, ct);
	}
	private async Task<(int Width, int Height)> ImageSize(RaiFile file, CancellationToken ct)
	{
		if (file.Ext.TrimStart('.').Equals("svg", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("strict-v1 does not decode SVG external resources.");
		var text = await ToolText(magick, ["-limit", "disk", "0", "-limit", "map", "0", file.FullName, "-auto-orient", "-format", "%w %h\\n", "info:"], ct);
		var lines = text.Trim().Split('\n');
		if (lines.Length != 1) throw new InvalidOperationException("strict-v1 requires a single-frame still image.");
		var fields = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var width = int.Parse(fields[0], CultureInfo.InvariantCulture); var height = int.Parse(fields[1], CultureInfo.InvariantCulture);
		if (width <= 0 || height <= 0 || (long)width * height > 8_000_000)
			throw new InvalidOperationException("strict-v1 still-image memory limit is 8 million pixels; operation is incomplete.");
		return (width, height);
	}
	private async Task<IorgMatchEvidence> CompareImage(RaiFile a, RaiFile b, CancellationToken ct)
	{
		RequireTools(false);
		var size = await ImageSize(a, ct); if (size != await ImageSize(b, ct)) return null;
		string[] Args(RaiFile file) => ["-limit", "disk", "0", "-limit", "map", "0", file.FullName, "-auto-orient", "-colorspace", "sRGB", "-alpha", "on", "-depth", "8", "rgba:-"];
		var result = await magick.RunBinaryAsync(Args(a), (first, token) => magick.RunBinaryAsync(Args(b), async (second, inner) =>
		{
			var bytes = checked(size.Width * size.Height * 4);
			var x = new byte[bytes]; var y = new byte[bytes];
			await first.ReadExactlyAsync(x, inner); await second.ReadExactlyAsync(y, inner);
			if (await first.ReadAsync(new byte[1], inner) != 0 || await second.ReadAsync(new byte[1], inner) != 0)
				throw new InvalidOperationException("Unexpected additional image frames.");
			bool exact = x.AsSpan().SequenceEqual(y);
			var mean = exact ? 1 : PixelSsim(x, y, size.Width, size.Height);
			return mean >= MeanThreshold ? Evidence(exact, mean, mean, 1, "ImageMagick", "auto-orient; sRGB RGBA8; equal dimensions; alpha exact") : null;
		}, token), ct);
		return result is null ? null : result with { Decoder = (await ToolText(magick, ["-version"], ct)).Split('\n')[0] };
	}
	private static double PixelSsim(byte[] a, byte[] b, int width, int height)
	{
		for (int i = 3; i < a.Length; i += 4) if (a[i] != b[i]) return 0;
		double total = 0; long count = 0;
		for (int top = 0; top < height; top += 8)
		for (int left = 0; left < width; left += 8)
		for (int c = 0; c < 3; c++)
		{
			double sx = 0, sy = 0, xx = 0, yy = 0, xy = 0; int n = 0;
			for (int row = top; row < Math.Min(top + 8, height); row++)
			for (int col = left; col < Math.Min(left + 8, width); col++)
			{
				var index = (row * width + col) * 4 + c; double x = a[index], y = b[index];
				sx += x; sy += y; xx += x * x; yy += y * y; xy += x * y; n++;
			}
			var mx = sx / n; var my = sy / n;
			const double c1 = 6.5025, c2 = 58.5225;
			total += ((2 * mx * my + c1) * (2 * (xy / n - mx * my) + c2))
				/ ((mx * mx + my * my + c1) * (xx / n - mx * mx + yy / n - my * my + c2)); count++;
		}
		return total / count;
	}
	private sealed record Video(int Width, int Height, string Aspect, string Transfer, string Primaries, string Space, string Rotation);
	private async Task<Video> Probe(RaiFile file, CancellationToken ct)
	{
		var text = await ToolText(ffprobe, ["-v", "error", "-select_streams", "v", "-show_entries", "stream=width,height,pix_fmt,sample_aspect_ratio,color_transfer,color_primaries,color_space:stream_side_data=rotation", "-of", "json", file.FullName], ct);
		using var json = JsonDocument.Parse(text);
		var streams = json.RootElement.GetProperty("streams");
		if (streams.GetArrayLength() != 1) throw new InvalidOperationException("strict-v1 requires exactly one video stream.");
		var s = streams[0]; string Get(string key) => s.TryGetProperty(key, out var value) ? value.ToString() : "unknown";
		var transfer = Get("color_transfer");
		if (transfer is "smpte2084" or "arib-std-b67") throw new InvalidOperationException("strict-v1 does not normalize HDR video; no visual identity is asserted.");
		var pixels = Get("pix_fmt");
		if (pixels.StartsWith("yuva", StringComparison.Ordinal) || pixels.Contains("rgba", StringComparison.Ordinal) || pixels.Contains("gbrap", StringComparison.Ordinal))
			throw new InvalidOperationException("strict-v1 does not discard alpha from video.");
		var rotation = "0";
		if (s.TryGetProperty("side_data_list", out var sides))
			foreach (var side in sides.EnumerateArray()) if (side.TryGetProperty("rotation", out var r)) rotation = r.ToString();
		var w = s.GetProperty("width").GetInt32(); var h = s.GetProperty("height").GetInt32();
		if ((long)w * h > 8_000_000 || w <= 0 || h <= 0) throw new InvalidOperationException("strict-v1 video dimension limit exceeded.");
		var aspect = Get("sample_aspect_ratio");
		if (int.TryParse(rotation, NumberStyles.Integer, CultureInfo.InvariantCulture, out var degrees) && Math.Abs(degrees % 180) == 90)
		{
			(w, h) = (h, w);
			var parts = aspect.Split(':'); if (parts.Length == 2) aspect = parts[1] + ":" + parts[0];
		}
		return new(w, h, aspect, transfer, Get("color_primaries"), Get("color_space"), "0");
	}
	private sealed class FrameReader(Stream stream)
	{
		private readonly StreamReader reader = new(stream);
		private decimal timeBase;
		private decimal? origin;
		public async Task<(decimal Time, decimal Duration, string Hash)?> Next(CancellationToken ct)
		{
			while (await reader.ReadLineAsync(ct) is { } line)
			{
				if (line.StartsWith("#tb 0:", StringComparison.Ordinal))
				{
					var ratio = line[6..].Trim().Split('/'); timeBase = decimal.Parse(ratio[0], CultureInfo.InvariantCulture) / decimal.Parse(ratio[1], CultureInfo.InvariantCulture);
				}
				if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
				var fields = line.Split(',');
				if (fields.Length != 6 || timeBase == 0) throw new InvalidDataException("Invalid decoded frame hash stream.");
				var pts = decimal.Parse(fields[2], CultureInfo.InvariantCulture) * timeBase; origin ??= pts;
				return (pts - origin.Value, decimal.Parse(fields[3], CultureInfo.InvariantCulture) * timeBase, fields[5].Trim());
			}
			return null;
		}
	}
	private async Task<IorgMatchEvidence> CompareVideo(RaiFile a, RaiFile b, CancellationToken ct)
	{
		RequireTools(true);
		if (await Probe(a, ct) != await Probe(b, ct)) return null;
		string[] HashArgs(RaiFile f) => ["-v", "error", "-nostdin", "-threads", "1", "-i", f.FullName, "-map", "0:v:0", "-an", "-vf", "format=yuv444p", "-fps_mode", "passthrough", "-threads", "1", "-f", "framehash", "-hash", "sha256", "pipe:1"];
		var sequence = await ffmpeg.RunBinaryAsync(HashArgs(a), (one, token) => ffmpeg.RunBinaryAsync(HashArgs(b), async (two, inner) =>
		{
			var x = new FrameReader(one); var y = new FrameReader(two); long frames = 0; bool exact = true, timing = true;
			while (true)
			{
				var first = await x.Next(inner); var second = await y.Next(inner);
				if (first is null || second is null)
				{
					if (first is not null || second is not null) timing = false;
					return (Frames: frames, Exact: exact, Timing: timing);
				}
				frames++; exact &= first.Value.Hash == second.Value.Hash;
				timing &= Math.Abs(first.Value.Time - second.Value.Time) <= 0.000002m
					&& Math.Abs(first.Value.Duration - second.Value.Duration) <= 0.000002m;
			}
		}, token), ct);
		if (!sequence.Timing) return null;
		if (sequence.Frames == 0) throw new InvalidDataException("Video decoded no frames.");
		var version = (await ToolText(ffmpeg, ["-version"], ct)).Split('\n')[0];
		if (sequence.Exact) return Evidence(true, 1, 1, sequence.Frames, version, "display orientation; YUV444P8 SDR; complete frame timings");
		var scores = await ffmpeg.RunBinaryAsync(["-v", "error", "-nostdin", "-threads", "1", "-i", a.FullName, "-threads", "1", "-i", b.FullName,
			"-filter_complex_threads", "1", "-filter_complex", "[0:v:0]format=yuv444p,setpts=PTS-STARTPTS[a];[1:v:0]format=yuv444p,setpts=PTS-STARTPTS[b];[a][b]ssim=stats_file=-:shortest=1[out]",
			"-map", "[out]", "-an", "-f", "null", "-"], async (stream, token) =>
		{
			using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true); double sum = 0, minimum = 1; long n = 0;
			while (await reader.ReadLineAsync(token) is { } line)
			{
				var all = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(f => f.StartsWith("All:", StringComparison.Ordinal));
				if (all is null || !double.TryParse(all[4..], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
					throw new InvalidDataException("Invalid complete-sequence SSIM evidence.");
				sum += value; minimum = Math.Min(minimum, value); n++;
			}
			return (Count: n, Mean: sum / Math.Max(1, n), Minimum: minimum);
		}, ct);
		if (scores.Count != sequence.Frames) throw new InvalidDataException("SSIM did not cover every decoded frame.");
		return scores.Mean >= MeanThreshold && scores.Minimum >= MinimumThreshold
			? Evidence(false, scores.Mean, scores.Minimum, scores.Count, version, "display orientation; YUV444P8 SDR; full-sequence SSIM; no crop/resize") : null;
	}
}
