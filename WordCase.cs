using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RaiImage
{
    public static class StringHelper
	{
		public static string ToTitle(this string anyCase)
		{
			if (string.IsNullOrEmpty(anyCase))
				return anyCase;

			return char.ToUpperInvariant(anyCase[0]) + anyCase.Substring(1).ToLowerInvariant();
		}
		public static string[] CamelSplit(this string anyCase)
		{
			return anyCase.WordSplit();
		}

		public static string[] WordSplit(this string anyCase)
		{
			return new WordCase(anyCase).Array;
		}
	}

	public class WordCase
	{
		private static readonly Regex CamelOrPascalWordRegex = new(@"[\p{Lu}]+(?=[\p{Lu}][\p{Ll}]|\b)|[\p{Lu}]?[\p{Ll}]+|\d+", RegexOptions.Compiled);
		private static readonly Regex SeparatorRegex = new(@"[^\p{L}\p{Nd}]+", RegexOptions.Compiled);

		public string[] Array
		{
			get => array;
			set
			{
				array = CleanWords(value);
			}
		}
		private string[] array;

		public string String
		{
			get => PascalCase;
			set
			{
				array = SplitAnyCase(value);
			}
		}

		public string PascalCase => string.Concat(array.Select(FormatPascalWord));

		public string CamelCaseString
		{
			get
			{
				if (array.Length == 0)
					return string.Empty;

				return FormatCamelFirstWord(array[0]) + string.Concat(array.Skip(1).Select(FormatPascalWord));
			}
		}

		public string LowerCamelCase => CamelCaseString;

		public string SnakeCase => string.Join("_", array.Select(FormatSnakeWord));
		public string KebabCase => string.Join("-", array.Select(FormatSnakeWord));
		public string DashCase => KebabCase;

		public WordCase(string[] words)
		{
			Array = words;
		}

		public WordCase(string anyCase)
		{
			String = anyCase;
		}

		private static string[] SplitAnyCase(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return [];

			return CleanWords(SeparatorRegex
				.Split(value)
				.SelectMany(SplitCamelOrPascalCase));
		}

		private static string[] SplitCamelOrPascalCase(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return [];

			return CleanWords(CamelOrPascalWordRegex.Matches(value).Select(match => match.Value));
		}

		private static string[] CleanWords(IEnumerable<string> words)
		{
			return words
				.Where(word => !string.IsNullOrWhiteSpace(word))
				.Select(word => word.Trim())
				.ToArray();
		}

		private static string FormatPascalWord(string word)
		{
			if (char.IsDigit(word[0]) || IsAllUppercaseWord(word))
				return word;

			return word.ToTitle();
		}

		private static string FormatCamelFirstWord(string word)
		{
			return char.IsDigit(word[0]) ? word : word.ToLowerInvariant();
		}

		private static string FormatSnakeWord(string word)
		{
			return word.ToLowerInvariant();
		}

		private static bool IsAllUppercaseWord(string word)
		{
			return word.Any(char.IsLetter)
				&& word.Where(char.IsLetter).All(char.IsUpper);
		}
	}
}
