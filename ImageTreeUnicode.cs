using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OsLib;

namespace RaiImage;

/// <summary>
/// Canonical Unicode handling for logical names owned by the ImageTree boundary.
/// Caller-supplied filesystem roots are deliberately not normalized here.
/// </summary>
internal static class ImageTreeUnicode
{
	internal static string Normalize(string value) =>
		(value ?? string.Empty).Normalize(NormalizationForm.FormC);

	internal static string NormalizeTrimmed(string value) => Normalize(value?.Trim());

	internal static bool CanonicalEquals(
		string left,
		string right,
		StringComparison comparison = StringComparison.Ordinal) =>
		string.Equals(Normalize(left), Normalize(right), comparison);

	internal static int TextElementCount(string value) =>
		StringInfo.ParseCombiningCharacters(Normalize(value)).Length;

	internal static string TakeTextElements(string value, int count)
	{
		if (count <= 0)
			return string.Empty;

		var normalized = Normalize(value);
		var starts = StringInfo.ParseCombiningCharacters(normalized);
		return starts.Length <= count ? normalized : normalized[..starts[count]];
	}

	internal static RaiPath ResolveEquivalentDirectory(RaiPath parent, string expectedSegment)
	{
		ArgumentNullException.ThrowIfNull(parent);
		var canonicalSegment = Normalize(expectedSegment);
		var canonicalPath = parent / new RaiRelPath(canonicalSegment);
		if (!parent.Exists())
			return canonicalPath;

		var matches = parent.EnumerateDirectories("*")
			.Where(candidate => CanonicalEquals(Leaf(candidate), canonicalSegment))
			.Take(2)
			.ToList();

		return matches.Count switch
		{
			0 => canonicalPath,
			1 => matches[0],
			_ => throw new RaiImageIOException(
				$"Multiple canonically equivalent ImageTree directories match '{canonicalSegment}' under '{parent.FullPath}'.")
		};
	}

	internal static RaiPath ResolveExistingBucket(ImageTreeFile file)
	{
		ArgumentNullException.ThrowIfNull(file);
		var current = file.Path;
		if (!file.Topdir.IsEmpty)
			current = ResolveEquivalentDirectory(current, file.Topdir.Segments.Single());
		if (!file.Subdir.IsEmpty)
			current = ResolveEquivalentDirectory(current, file.Subdir.Segments.Single());
		return current;
	}

	internal static string RemoveEquivalentBucketSuffix(string path, params string[] expectedSegments)
	{
		if (string.IsNullOrEmpty(path) || expectedSegments.Length == 0)
			return path;

		var original = new RaiPath(path);
		var current = original;
		for (var index = expectedSegments.Length - 1; index >= 0; index--)
		{
			if (!CanonicalEquals(Leaf(current), expectedSegments[index], StringComparison.OrdinalIgnoreCase))
				return original.FullPath;
			current = current.Parent;
		}
		return current.FullPath;
	}

	private static string Leaf(RaiPath path)
	{
		var segments = path.Segments;
		return segments.Length == 0 ? string.Empty : segments[^1];
	}
}
