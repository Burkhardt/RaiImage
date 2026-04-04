using System;
using OsLib;

namespace RaiImage
{
	public class ItemTreePath : RaiPath, IPathConvention
	{
		public override string ToString() => Path.ToString();

		public PathConventionType Convention { get; }

		public RaiPath RootPath
		{
			get => new(base.Path);
			set
			{
				base.Path = NormalizeRootPath(value?.ToString(), ItemId, Split.tLen, Split.sLen);
				ApplyPathConvention();
			}
		}

		public string ItemId
		{
			get => itemId;
			set
			{
				itemId = string.IsNullOrEmpty(value) ? string.Empty : value;
				ApplyPathConvention();
			}
		}
		private string itemId = string.Empty;

		public string Topdir { get; private set; } = string.Empty;
		public string Subdir { get; private set; } = string.Empty;

		public RaiPath TopdirRoot => string.IsNullOrEmpty(Topdir) ? RootPath : RootPath / Topdir;
		public RaiPath SubdirRoot => string.IsNullOrEmpty(Subdir) ? TopdirRoot : RootPath / Topdir / Subdir;

		public new RaiPath Path
		{
			get
			{
				ApplyPathConvention();
				return SubdirRoot;
			}
			set => RootPath = value;
		}

		private (int tLen, int sLen) Split { get; }

		/// <summary>
		/// Single source of truth for mapping PathConventionType → (topdirLen, subdirLen).
		/// CanonicalByName uses the full ItemId as topdir, no subdir.
		/// </summary>
		public static (int tLen, int sLen) GetSplit(PathConventionType convention, string itemId = null) => convention switch
		{
			PathConventionType.ItemIdTree3x3  => (3, 3),
			PathConventionType.ItemIdTree8x2  => (8, 2),
			PathConventionType.CanonicalByName => (string.IsNullOrEmpty(itemId) ? 0 : itemId.Length, 0),
			_ => throw new ArgumentOutOfRangeException(nameof(convention), convention, "Unknown path convention")
		};

		public void ApplyPathConvention()
		{
			var (tLen, sLen) = Convention == PathConventionType.CanonicalByName
				? GetSplit(Convention, ItemId)
				: Split;

			base.Path = NormalizeRootPath(base.Path, ItemId, tLen, sLen);

			Topdir = string.IsNullOrEmpty(ItemId) || tLen <= 0
				? string.Empty
				: SanitizeSegment(ItemId[..Math.Min(ItemId.Length, tLen)]);

			// subdir is cumulative: first (tLen + sLen) chars of ItemId, so it always starts with topdir
			Subdir = string.IsNullOrEmpty(ItemId) || sLen <= 0
				? string.Empty
				: SanitizeSegment(ItemId[..Math.Min(ItemId.Length, tLen + sLen)]);
		}

		private static string NormalizeRootPath(string rootCandidate, string itemId, int tLen, int sLen)
		{
			var normalized = string.IsNullOrEmpty(rootCandidate)
				? string.Empty
				: new RaiFile(rootCandidate).Path.ToString();

			if (string.IsNullOrEmpty(normalized) || string.IsNullOrEmpty(itemId))
				return normalized;

			var top = SanitizeSegment(itemId[..Math.Min(itemId.Length, tLen)]);
			var sub = sLen > 0
				? SanitizeSegment(itemId[..Math.Min(itemId.Length, tLen + sLen)])
				: string.Empty;

			var marker = string.IsNullOrEmpty(sub)
				? Os.DIR + top
				: Os.DIR + top + Os.DIR + sub;
			var pos = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
			return pos >= 0 ? normalized.Remove(pos + 1) : normalized;
		}

		/// <summary>
		/// DOS reserved device name — "con" as a directory kills Windows; replace 'o' with '0'.
		/// </summary>
		internal static string SanitizeSegment(string segment) =>
			segment.Length == 3 && segment.Equals("con", StringComparison.OrdinalIgnoreCase) ? "C0N" : segment;

		public ItemTreePath(RaiPath rootPath, string itemId, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			: base(rootPath?.ToString() ?? string.Empty)
		{
			Convention = convention;
			Split = GetSplit(convention, itemId);
			this.itemId = string.IsNullOrEmpty(itemId) ? string.Empty : itemId;
			base.Path = NormalizeRootPath(rootPath?.ToString(), this.itemId, Split.tLen, Split.sLen);
			ApplyPathConvention();
		}

		public ItemTreePath(string rootPath, string itemId, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			: this(new RaiPath(rootPath), itemId, convention)
		{
		}
	}
}
