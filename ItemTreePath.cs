using System;
using System.Collections.Generic;
using System.Linq;
using OsLib;
namespace RaiImage
{
	/// <summary>
	/// The conventional tree home of one subscriber-local item and all files that
	/// belong to that item, regardless of their physical file type.
	/// </summary>
	public class ItemTreePath : RaiPath, IPathConvention
	{
		public override string ToString() => FullPath;
		/// <summary>
		/// The fully composed path: Root + Topdir + Subdir as a string.
		/// </summary>
		public override string FullPath
		{
			get
			{
				ApplyPathConvention();
				return SubdirRoot.FullPath;
			}
		}
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
				itemId = ImageTreeUnicode.Normalize(value);
				ApplyPathConvention();
			}
		}
		private string itemId = string.Empty;
		/// <summary>
		/// The top-level directory segment derived from the ItemId prefix.
		/// For ItemIdTree8x2 with ItemId "ABCDEFGHIJ", this would be "ABCDEFGH/".
		/// </summary>
		public RaiRelPath Topdir { get; private set; } = new RaiRelPath();
		/// <summary>
		/// The sub-level directory segment derived from the ItemId prefix.
		/// Cumulative with Topdir: for ItemIdTree8x2 with ItemId "ABCDEFGHIJ",
		/// this would be "ABCDEFGHIJ/" (first tLen+sLen chars).
		/// </summary>
		public RaiRelPath Subdir { get; private set; } = new RaiRelPath();
		/// <summary>
		/// RootPath with Topdir appended (when non-empty).
		/// </summary>
		public RaiPath TopdirRoot => Topdir.IsEmpty ? RootPath : RootPath / Topdir;
		/// <summary>
		/// RootPath with Topdir and Subdir appended. This is the full convention path.
		/// </summary>
		public RaiPath SubdirRoot => Subdir.IsEmpty ? TopdirRoot : RootPath / Topdir / Subdir;
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
		/// Single source of truth for mapping PathConventionType to (topdirLen, subdirLen).
		/// CanonicalByName uses the full ItemId as topdir, no subdir.
		/// </summary>
		public static (int tLen, int sLen) ConventionSplit(PathConventionType convention, string itemId = null) => convention switch
		{
			PathConventionType.ItemIdTree3x3   => (3, 3),
			PathConventionType.ItemIdTree8x2   => (8, 2),
			PathConventionType.CanonicalByName => (string.IsNullOrEmpty(itemId) ? 0 : ImageTreeUnicode.TextElementCount(itemId), 0),
			PathConventionType.Flat            => (0, 0),
			_ => throw new ArgumentOutOfRangeException(nameof(convention), convention, "Unknown path convention")
		};
		public void ApplyPathConvention()
		{
			var (tLen, sLen) = Convention == PathConventionType.CanonicalByName
				? ConventionSplit(Convention, ItemId)
				: Split;
			base.Path = NormalizeRootPath(base.Path, ItemId, tLen, sLen);
			Topdir = string.IsNullOrEmpty(ItemId) || tLen <= 0
				? new RaiRelPath()
				: new RaiRelPath(SanitizeSegment(ImageTreeUnicode.TakeTextElements(ItemId, tLen)));
			// subdir is cumulative: first (tLen + sLen) chars of ItemId, so it always starts with topdir
			Subdir = string.IsNullOrEmpty(ItemId) || sLen <= 0
				? new RaiRelPath()
				: new RaiRelPath(SanitizeSegment(ImageTreeUnicode.TakeTextElements(ItemId, tLen + sLen)));
		}
		private static string NormalizeRootPath(string rootCandidate, string itemId, int tLen, int sLen)
		{
			var normalized = string.IsNullOrEmpty(rootCandidate)
				? string.Empty
				: new RaiFile(rootCandidate).Path.ToString();
			if (string.IsNullOrEmpty(normalized) || string.IsNullOrEmpty(itemId) || tLen <= 0)
				return normalized;
			var top = SanitizeSegment(ImageTreeUnicode.TakeTextElements(itemId, tLen));
			var sub = sLen > 0
				? SanitizeSegment(ImageTreeUnicode.TakeTextElements(itemId, tLen + sLen))
				: string.Empty;
			return string.IsNullOrEmpty(sub)
				? ImageTreeUnicode.RemoveEquivalentBucketSuffix(normalized, top)
				: ImageTreeUnicode.RemoveEquivalentBucketSuffix(normalized, top, sub);
		}
		/// <summary>
		/// DOS reserved device name: "con" as a directory kills Windows; replace 'o' with '0'.
		/// </summary>
		internal static string SanitizeSegment(string segment) =>
			segment.Length == 3 && segment.Equals("con", StringComparison.OrdinalIgnoreCase) ? "C0N" : segment;

		/// <summary>
		/// Select every physical file owned by this exact ItemId. The search is
		/// restricted to this item's convention-derived bucket, but sibling items
		/// sharing that bucket are excluded.
		/// </summary>
		public IReadOnlyList<RaiFile> SelectFiles()
		{
			var bucket = ResolveExistingBucket();
			if (!bucket.Exists())
				return Array.Empty<RaiFile>();

			return bucket.EnumerateFiles("*")
				.Where(Owns)
				.OrderBy(file => ImageTreeUnicode.Normalize(file.NameWithExtension), StringComparer.Ordinal)
				.ToList();
		}

		/// <summary>
		/// Move all files owned by <paramref name="from"/> into this item home.
		/// When the ItemIds differ, only the leading ItemId portion of each filename
		/// is renamed; image numbers, NameExt values, diagram config suffixes and
		/// physical extensions are preserved.
		/// </summary>
		public IReadOnlyList<RaiFile> mv(
			ItemTreePath from,
			bool replace = false,
			bool keepBackup = false)
		{
			ArgumentNullException.ThrowIfNull(from);
			var sources = from.SelectFiles();
			if (sources.Count == 0)
				throw new RaiImageNotFoundException(
					$"No files were found for ItemId '{from.ItemId}' under '{from.RootPath.FullPath}'.",
					from.FullPath);

			var moves = sources
				.Select(source => (Source: source, Destination: DestinationFor(from, source)))
				.ToList();
			var duplicateDestination = moves
				.GroupBy(move => move.Destination.FullName, StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault(group => group.Count() > 1);
			if (duplicateDestination != null)
				throw new RaiImageIOException(
					$"Multiple item files resolve to destination '{duplicateDestination.Key}'.");

			if (!replace)
			{
				var collision = moves.FirstOrDefault(move =>
					!SameFile(move.Source, move.Destination) && move.Destination.Exists());
				if (collision.Destination != null)
					throw new RaiImageIOException(
						$"Destination file already exists: {collision.Destination.FullName}");
			}

			var completed = new List<(RaiFile Source, RaiFile Destination)>();
			try
			{
				foreach (var move in moves)
				{
					if (!SameFile(move.Source, move.Destination))
						move.Destination.mv(move.Source, replace, keepBackup);
					completed.Add(move);
				}
			}
			catch (Exception exception)
			{
				foreach (var move in completed.AsEnumerable().Reverse())
				{
					try
					{
						if (!SameFile(move.Source, move.Destination) && move.Destination.Exists())
							move.Source.mv(move.Destination, replace: true, keepBackup: false);
					}
					catch
					{
						// Preserve the original move failure; rollback is best effort.
					}
				}
				throw new RaiImageIOException(
					$"Unable to move ItemId '{from.ItemId}' to '{ItemId}'.",
					exception);
			}

			from.PruneVacatedBuckets();
			return moves.Select(move => move.Destination).ToList();
		}

		private RaiFile DestinationFor(ItemTreePath from, RaiFile source)
		{
			var sourceStem = ImageTreeUnicode.Normalize(source.Name);
			var sourceItemId = ImageTreeUnicode.Normalize(from.ItemId);
			var suffix = sourceStem.Length == sourceItemId.Length
				? string.Empty
				: sourceStem[sourceItemId.Length..];
			return new RaiFile(SubdirRoot, ItemId + suffix, source.Ext);
		}

		private bool Owns(RaiFile file)
		{
			var stem = ImageTreeUnicode.Normalize(file?.Name);
			var id = ImageTreeUnicode.Normalize(ItemId);
			if (string.Equals(stem, id, StringComparison.OrdinalIgnoreCase))
				return true;
			if (stem.Length <= id.Length
				|| !stem.StartsWith(id, StringComparison.OrdinalIgnoreCase))
				return false;
			return stem[id.Length] is '_' or ',';
		}

		private RaiPath ResolveExistingBucket()
		{
			var current = RootPath;
			if (!Topdir.IsEmpty)
				current = ImageTreeUnicode.ResolveEquivalentDirectory(current, Topdir.Segments.Single());
			if (!Subdir.IsEmpty)
				current = ImageTreeUnicode.ResolveEquivalentDirectory(current, Subdir.Segments.Single());
			return current;
		}

		private void PruneVacatedBuckets()
		{
			if (Convention == PathConventionType.Flat)
				return;

			var bucket = ResolveExistingBucket();
			if (IsDirectoryEmpty(bucket))
				bucket.rmdir();

			if (!Subdir.IsEmpty)
			{
				var top = ImageTreeUnicode.ResolveEquivalentDirectory(RootPath, Topdir.Segments.Single());
				if (IsDirectoryEmpty(top))
					top.rmdir();
			}
		}

		/// <summary>
		/// Remove empty convention-derived bucket directories after callers have
		/// deleted this item's files. The subscriber root itself is never removed.
		/// </summary>
		public void PruneEmptyDirectories() => PruneVacatedBuckets();

		private static bool IsDirectoryEmpty(RaiPath path) =>
			path.Exists()
			&& !path.EnumerateFiles("*").Any()
			&& !path.EnumerateDirectories("*").Any();

		private static bool SameFile(RaiFile left, RaiFile right) =>
			string.Equals(left.FullName, right.FullName, StringComparison.Ordinal);

		public ItemTreePath(RaiPath rootPath, string itemId, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			: base(rootPath?.ToString() ?? string.Empty)
		{
			Convention = convention;
			Split = ConventionSplit(convention, itemId);
			this.itemId = ImageTreeUnicode.Normalize(itemId);
			base.Path = NormalizeRootPath(rootPath?.ToString(), this.itemId, Split.tLen, Split.sLen);
			ApplyPathConvention();
		}
		public ItemTreePath(string rootPath, string itemId, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			: this(new RaiPath(rootPath), itemId, convention)
		{
		}

		/// <summary>Create an item home below an ImageTree root and subscriber segment.</summary>
		public ItemTreePath(
			RaiPath imageTreeRoot,
			string subscriber,
			string itemId,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			: this(
				ImageTreeUnicode.ResolveEquivalentDirectory(
					imageTreeRoot ?? throw new ArgumentNullException(nameof(imageTreeRoot)),
					ValidateSubscriber(subscriber)),
				itemId,
				convention)
		{
		}

		private static string ValidateSubscriber(string subscriber)
		{
			var canonical = ImageTreeUnicode.NormalizeTrimmed(subscriber);
			if (string.IsNullOrWhiteSpace(canonical)
				|| canonical.Contains('/')
				|| canonical.Contains('\\'))
				throw new ArgumentException("Subscriber must be a plain ImageTree path segment.", nameof(subscriber));
			return canonical;
		}
	}
}
