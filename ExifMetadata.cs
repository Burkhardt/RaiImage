#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RaiImage;

/// <summary>An exact EXIF rational plus its numeric value for downstream calculations.</summary>
public sealed record ExifRational(long Numerator, long Denominator)
{
	public double Value => (double)Numerator / Denominator;
	internal static ExifRational Parse(string value)
	{
		var parts = value.Trim().Split('/');
		if (parts.Length != 2 || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator) ||
			!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator) || denominator == 0)
			throw new FormatException("Invalid EXIF rational.");
		return new(numerator, denominator);
	}
}

/// <summary>A group of related EXIF fields, including vendor fields when present.</summary>
public sealed class ExifFields
{
	[JsonExtensionData] public Dictionary<string, object> Fields { get; } = new(StringComparer.Ordinal);
}

/// <summary>Selected EXIF metadata. Dates use their own embedded UTC offsets only.</summary>
public sealed class ExifMetadata
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? DateTimeOriginal { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? DateTimeDigitized { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? DateTime { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ExifFields? Thumbnail { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ExifFields? Lens { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ExifFields? FocalPlane { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ExifFields? Exposure { get; internal set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, string>? Unconverted { get; internal set; }
	[JsonExtensionData] public Dictionary<string, object> Other { get; } = new(StringComparer.Ordinal);
	[JsonIgnore] public List<string> Diagnostics { get; } = [];

	private static readonly HashSet<string> RationalTags = new(StringComparer.Ordinal)
	{
		"FocalLength", "FocalPlaneXResolution", "FocalPlaneYResolution", "ExposureBiasValue", "ExposureTime",
		"Thumbnail.XResolution", "Thumbnail.YResolution", "XResolution", "YResolution", "FNumber",
		"ApertureValue", "MaxApertureValue", "ShutterSpeedValue", "BrightnessValue", "SubjectDistance"
	};
	private static readonly HashSet<string> IntegerTags = new(StringComparer.Ordinal)
	{
		"FocalLengthIn35mmFilm", "FocalPlaneResolutionUnit", "ExposureMode", "ExposureProgram",
		"Thumbnail.Compression", "Thumbnail.JPEGInterchangeFormat", "Thumbnail.JPEGInterchangeFormatLength",
		"Thumbnail.ResolutionUnit", "Orientation", "ResolutionUnit", "ISOSpeedRatings", "PixelXDimension", "PixelYDimension"
	};

	public static ExifMetadata FromExif(IReadOnlyDictionary<string, string> exif, string selector = "*")
	{
		ArgumentNullException.ThrowIfNull(exif);
		ImageExif.ValidateSelector(selector);
		var selected = exif.Where(p => ImageExif.MatchesSelector(p.Key, selector)).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
		var result = new ExifMetadata();
		foreach (var (date, offset) in new[] { ("DateTimeOriginal", "OffsetTimeOriginal"), ("DateTimeDigitized", "OffsetTimeDigitized"), ("DateTime", "OffsetTime") })
		{
			if (!selected.Remove(date, out var raw)) continue;
			try
			{
				var instant = ImageExif.GetDateTime(exif, date);
				if (instant is null) throw new FormatException("Missing date or matching " + offset + ".");
				if (date == "DateTimeOriginal") result.DateTimeOriginal = instant;
				else if (date == "DateTimeDigitized") result.DateTimeDigitized = instant;
				else result.DateTime = instant;
				selected.Remove(offset);
			}
			catch (FormatException ex)
			{
				(result.Unconverted ??= new())[date] = raw;
				if (exif.TryGetValue(offset, out var rawOffset))
				{
					result.Unconverted[offset] = rawOffset;
					selected.Remove(offset);
				}
				result.Diagnostics.Add(date + " was not converted: " + ex.Message);
			}
		}
		foreach (var (tag, raw) in selected)
		{
			object value = raw;
			try
			{
				if (tag == "LensSpecification") value = Regex.Split(raw.Trim(), @"[,\s]+").Select(ExifRational.Parse).ToArray();
				else if (RationalTags.Contains(tag)) value = ExifRational.Parse(raw);
				else if (IntegerTags.Contains(tag)) value = long.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
			}
			catch (Exception ex) when (ex is FormatException or OverflowException)
			{
				(result.Unconverted ??= new())[tag] = raw;
				result.Diagnostics.Add(tag + " was not converted: invalid numeric value.");
				continue;
			}
			if (tag.StartsWith("Thumbnail.", StringComparison.Ordinal)) (result.Thumbnail ??= new()).Fields[tag[10..]] = value;
			else if (tag.StartsWith("Lens", StringComparison.Ordinal) && tag.Length > 4) (result.Lens ??= new()).Fields[tag[4..]] = value;
			else if (tag.StartsWith("FocalPlane", StringComparison.Ordinal) && tag.Length > 10) (result.FocalPlane ??= new()).Fields[tag[10..]] = value;
			else if (tag.StartsWith("Exposure", StringComparison.Ordinal) && tag.Length > 8) (result.Exposure ??= new()).Fields[tag[8..]] = value;
			else result.Other[tag] = value;
		}
		return result;
	}
}
