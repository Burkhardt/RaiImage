using System;

namespace RaiImage;

/// <summary>
/// Binary compatibility facade for callers compiled against RaiImage before the
/// general-purpose string helpers moved to RaiUtils in RAIkeep 4.2.6.
/// </summary>
[Obsolete("WordCase moved to the RaiUtils package and namespace in RAIkeep 4.2.6.")]
public class WordCase : RaiUtils.WordCase
{
	public WordCase(string[] words) : base(words)
	{
	}

	public WordCase(string anyCase) : base(anyCase)
	{
	}
}

/// <summary>
/// Binary compatibility facade for pre-4.2.6 static calls. These methods are
/// intentionally not extension methods; new source imports RaiUtils instead.
/// </summary>
[Obsolete("StringHelper moved to the RaiUtils package and namespace in RAIkeep 4.2.6.")]
public static class StringHelper
{
	public static string ToTitle(string anyCase) => RaiUtils.StringHelper.ToTitle(anyCase);
	public static string[] CamelSplit(string anyCase) => RaiUtils.StringHelper.CamelSplit(anyCase);
	public static string[] WordSplit(string anyCase) => RaiUtils.StringHelper.WordSplit(anyCase);
}
