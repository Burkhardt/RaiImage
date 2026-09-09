using System;
using System.Linq;
using OsLib;

namespace RaiImage;

/// <summary>
/// A non-image text file placed with the existing ItemTree subscriber root,
/// item id, and <see cref="ItemTreePath"/> convention.
/// </summary>
public class ItemTreeTextFile : TextFile
{
	public ItemTreeTextFile(
		ItemTreePath itemPath,
		string nameExt,
		string ext)
		: this(
			itemPath ?? throw new ArgumentNullException(nameof(itemPath)),
			itemPath.ItemId,
			ComposeStem(itemPath.ItemId, nameExt),
			nameExt,
			ext)
	{
	}

	public ItemTreeTextFile(
		RaiPath subscriberRoot,
		string itemId,
		string nameExt,
		string ext,
		PathConventionType convention = PathConventionType.ItemIdTree8x2)
		: this(
			CreateItemPath(subscriberRoot, itemId, convention),
			itemId,
			ComposeStem(itemId, nameExt),
			nameExt,
			ext)
	{
	}

	protected ItemTreeTextFile(
		RaiPath subscriberRoot,
		string itemId,
		string fileStem,
		string nameExt,
		string ext,
		PathConventionType convention)
		: this(
			CreateItemPath(subscriberRoot, itemId, convention),
			itemId,
			fileStem,
			nameExt,
			ext)
	{
	}

	protected ItemTreeTextFile(
		ItemTreePath itemPath,
		string fileStem,
		string nameExt,
		string ext)
		: this(
			itemPath ?? throw new ArgumentNullException(nameof(itemPath)),
			itemPath.ItemId,
			fileStem,
			nameExt,
			ext)
	{
	}

	private ItemTreeTextFile(
		ItemTreePath itemPath,
		string itemId,
		string fileStem,
		string nameExt,
		string ext)
		: base(
			itemPath.SubdirRoot,
			ValidateFileStem(fileStem),
			ValidateExtension(ext))
	{
		ItemPath = itemPath;
		ItemId = ValidateItemId(itemId);
		NameExt = ValidateNameExt(nameExt);
	}

	/// <summary>Standalone authoring file outside an ImageTree subscriber location.</summary>
	protected ItemTreeTextFile(string fullName)
		: base(fullName)
	{
	}

	/// <summary>Standalone authoring file outside an ImageTree subscriber location.</summary>
	protected ItemTreeTextFile(RaiPath path, string itemId, string nameExt, string ext)
		: base(
			path ?? throw new ArgumentNullException(nameof(path)),
			ComposeStem(itemId, nameExt),
			ValidateExtension(ext))
	{
		ItemId = ValidateItemId(itemId);
		NameExt = ValidateNameExt(nameExt);
	}

	public ItemTreePath ItemPath { get; }
	public RaiPath SubscriberRoot => ItemPath?.RootPath;
	public RaiPath SubdirRoot => ItemPath?.SubdirRoot ?? Path;
	public PathConventionType Convention => ItemPath?.Convention ?? PathConventionType.ItemIdTree8x2;
	public string ItemId { get; } = string.Empty;
	public string NameExt { get; } = string.Empty;

	public ItemTreeTextFile CreateSibling(string nameExt, string ext)
	{
		if (ItemPath is null)
			throw new InvalidOperationException("A standalone text file has no subscriber ItemTreePath for sibling creation.");
		return new ItemTreeTextFile(SubscriberRoot, ItemId, nameExt, ext, Convention);
	}

	private static ItemTreePath CreateItemPath(
		RaiPath subscriberRoot,
		string itemId,
		PathConventionType convention)
		=> new(
			subscriberRoot ?? throw new ArgumentNullException(nameof(subscriberRoot)),
			ValidateItemId(itemId),
			convention);

	protected static string ComposeStem(string itemId, string nameExt)
	{
		var id = ValidateItemId(itemId);
		var normalized = ValidateNameExt(nameExt);
		return string.IsNullOrEmpty(normalized) ? id : $"{id}_{normalized}";
	}

	protected static string ValidateNameExt(string nameExt)
	{
		if (string.IsNullOrEmpty(nameExt))
			return string.Empty;
		var canonical = ImageTreeUnicode.Normalize(nameExt);
		if (canonical.Any(character => !char.IsLetterOrDigit(character) && character is not ('-' or '_')))
			throw new ArgumentException("An ItemTree artifact NameExt contains unsupported characters.", nameof(nameExt));
		return canonical;
	}

	protected static string ValidateFileStem(string fileStem)
	{
		var canonical = ImageTreeUnicode.Normalize(fileStem);
		if (string.IsNullOrWhiteSpace(canonical)
			|| canonical is "." or ".."
			|| canonical.Contains('/')
			|| canonical.Contains('\\'))
			throw new ArgumentException("An ItemTree artifact filename stem is invalid.", nameof(fileStem));
		return canonical;
	}

	protected static string ValidateExtension(string ext)
	{
		var canonical = ImageTreeUnicode.Normalize(ext);
		if (string.IsNullOrWhiteSpace(canonical)
			|| canonical.Any(character => !char.IsLetterOrDigit(character)))
			throw new ArgumentException("An ItemTree artifact extension must be one file-type token.", nameof(ext));
		return canonical;
	}

	private static string ValidateItemId(string itemId)
	{
		var canonical = ImageTreeUnicode.NormalizeTrimmed(itemId);
		if (string.IsNullOrWhiteSpace(canonical)
			|| canonical is "." or ".."
			|| canonical.Contains('/')
			|| canonical.Contains('\\'))
			throw new ArgumentException("An ItemTree artifact item id must be a plain file stem.", nameof(itemId));
		return canonical;
	}
}
