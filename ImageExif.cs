using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage;

/// <summary>Reads EXIF through ImageMagick, preserving metadata values as JSON strings.</summary>
public static class ImageExif
{
    /// <summary>
    /// Converts an EXIF date and its explicit UTC offset into a typed instant.
    /// The caller supplies the matching field pair, for example DateTimeOriginal
    /// and OffsetTimeOriginal. The machine's local timezone is never consulted.
    /// </summary>
    public static DateTimeOffset ParseDateTime(string value, string utcOffset)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(utcOffset);
        if (!Regex.IsMatch(utcOffset, @"\A[+-][0-9]{2}:[0-9]{2}\z") ||
            !DateTimeOffset.TryParseExact(value + " " + utcOffset,
                "yyyy:MM:dd HH:mm:ss zzz", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            throw new FormatException("EXIF date requires 'yyyy:MM:dd HH:mm:ss' and an explicit '+HH:mm' or '-HH:mm' UTC offset.");
        return result;
    }

    /// <summary>
    /// Converts a supported EXIF date using only its own companion offset tag.
    /// Returns null when either member of the pair is missing; invalid values
    /// raise FormatException. No file timestamp or local timezone is substituted.
    /// </summary>
    public static DateTimeOffset? GetDateTime(IReadOnlyDictionary<string, string> exif, string dateTimeTag)
    {
        ArgumentNullException.ThrowIfNull(exif);
        var offsetTag = dateTimeTag switch
        {
            "DateTimeOriginal" => "OffsetTimeOriginal",
            "DateTimeDigitized" => "OffsetTimeDigitized",
            "DateTime" => "OffsetTime",
            _ => throw new ArgumentException("Unsupported EXIF date tag.", nameof(dateTimeTag))
        };
        if (!exif.TryGetValue(dateTimeTag, out var date) || string.IsNullOrWhiteSpace(date) ||
            !exif.TryGetValue(offsetTag, out var offset) || string.IsNullOrWhiteSpace(offset))
            return null;
        return ParseDateTime(date, offset);
    }

    /// <summary>Original capture instant only; never falls back to digitization or modification time.</summary>
    public static DateTimeOffset? GetCaptureTime(IReadOnlyDictionary<string, string> exif)
        => GetDateTime(exif, "DateTimeOriginal");

    public static void ValidateSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector) || selector.Split(',').Any(tag => !Regex.IsMatch(tag.Trim(), @"\A[A-Za-z0-9_.*?]+\z")))
            throw new ArgumentException("--exif requires '*' or comma-separated EXIF tag names/patterns such as DateTime,DateTimeOriginal.");
    }

    internal static bool MatchesSelector(string name, string selector) => selector.Split(',').Any(tag =>
        Regex.IsMatch(name, "\\A" + Regex.Escape(tag.Trim()).Replace("\\*", ".*").Replace("\\?", ".") + "\\z",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

    /// <summary>Read selected structured fields, fetching companion offsets even if not selected explicitly.</summary>
    public static async Task<ExifMetadata> ReadMetadataAsync(RaiFile image, string selector = "*", CancellationToken cancellationToken = default)
    {
        ValidateSelector(selector);
        var raw = await ReadAsync(image, "*", cancellationToken).ConfigureAwait(false);
        return ExifMetadata.FromExif(raw, selector);
    }

    public static async Task<IReadOnlyDictionary<string, string>> ReadAsync(
        RaiFile image, string selector = "*", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ValidateSelector(selector);
        if (!image.Exists()) throw new FileNotFoundException("Image does not exist.", image.FullName);
        var command = new ImageMagickCommand();
        if (!command.IsAvailable()) throw new InvalidOperationException("--exif requires ImageMagick 7 ('magick') on PATH or the configured ImageMagick path.");
        // Request lazy EXIF properties, then let ImageMagick's JSON encoder escape
        // multiline/quoted metadata. Parsing %[exif:*] as lines loses such values.
        // Read the primary frame; never write or transform the source image.
        var result = await command.RunAsync(new[]
        {
            "-ping", image.FullName + "[0]", "-set", "option:iorg-exif", "%[exif:*]", "json:-"
        }, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
            throw new IOException("Unable to read image EXIF: " + result.StandardError.Trim());
        using var json = JsonDocument.Parse(result.StandardOutput);
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (json.RootElement.GetArrayLength() == 0) return values;
        if (!json.RootElement[0].GetProperty("image").TryGetProperty("properties", out var properties)) return values;
        var patterns = selector.Split(',').Select(tag => "\\A" + Regex.Escape(tag.Trim()).Replace("\\*", ".*").Replace("\\?", ".") + "\\z").ToArray();
        foreach (var property in properties.EnumerateObject())
        {
            if (!property.Name.StartsWith("exif:", StringComparison.OrdinalIgnoreCase)) continue;
            var name = property.Name[5..];
            if (patterns.Any(pattern => Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
                values[name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()! : property.Value.GetRawText();
        }
        return values;
    }
}
