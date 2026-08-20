using System;
using System.Linq;
using OsLib;

namespace RaiImage;

/// <summary>
/// A non-image text file placed with the existing ImageTree subscriber root,
/// item id, and <see cref="ItemTreePath"/> convention.
/// </summary>
public class ImageTreeTextFile : TextFile
{
	public ImageTreeTextFile(
		RaiPath subscriberRoot,
		string itemId,
		string nameExt,
		string ext,
		PathConventionType convention = PathConventionType.ItemIdTree8x2)
		: this(CreateItemPath(subscriberRoot, itemId, convention), itemId, nameExt, ext)
	{
	}

	private ImageTreeTextFile(
		ItemTreePath itemPath,
		string itemId,
		string nameExt,
		string ext)
		: base(
			itemPath.SubdirRoot,
			ComposeStem(itemId, nameExt),
			ValidateExtension(ext))
	{
		ItemPath = itemPath;
		ItemId = ValidateItemId(itemId);
		NameExt = ValidateNameExt(nameExt);
	}

	protected ImageTreeTextFile(
		RaiPath subscriberRoot,
		string itemId,
		string fileStem,
		string nameExt,
		string ext,
		PathConventionType convention)
		: base(
			CreateItemPath(subscriberRoot, itemId, convention).SubdirRoot,
			ValidateFileStem(fileStem),
			ValidateExtension(ext))
	{
		ItemPath = CreateItemPath(subscriberRoot, itemId, convention);
		ItemId = ValidateItemId(itemId);
		NameExt = ValidateNameExt(nameExt);
	}

	/// <summary>Standalone authoring file outside an ImageTree subscriber location.</summary>
	protected ImageTreeTextFile(string fullName)
		: base(fullName)
	{
	}

	/// <summary>Standalone authoring file outside an ImageTree subscriber location.</summary>
	protected ImageTreeTextFile(RaiPath path, string itemId, string nameExt, string ext)
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

	public ImageTreeTextFile CreateSibling(string nameExt, string ext)
	{
		if (ItemPath is null)
			throw new InvalidOperationException("A standalone text file has no subscriber ItemTreePath for sibling creation.");
		return new ImageTreeTextFile(SubscriberRoot, ItemId, nameExt, ext, Convention);
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
		if (nameExt.Any(character => !char.IsLetterOrDigit(character) && character is not ('-' or '_')))
			throw new ArgumentException("An ImageTree artifact NameExt contains unsupported characters.", nameof(nameExt));
		return nameExt;
	}

	protected static string ValidateFileStem(string fileStem)
	{
		if (string.IsNullOrWhiteSpace(fileStem)
			|| fileStem is "." or ".."
			|| fileStem.Contains('/')
			|| fileStem.Contains('\\'))
			throw new ArgumentException("An ImageTree artifact filename stem is invalid.", nameof(fileStem));
		return fileStem;
	}

	protected static string ValidateExtension(string ext)
	{
		if (string.IsNullOrWhiteSpace(ext)
			|| ext.Any(character => !char.IsLetterOrDigit(character)))
			throw new ArgumentException("An ImageTree artifact extension must be one file-type token.", nameof(ext));
		return ext;
	}

	private static string ValidateItemId(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId)
			|| itemId is "." or ".."
			|| itemId.Contains('/')
			|| itemId.Contains('\\'))
			throw new ArgumentException("An ImageTree artifact item id must be a plain file stem.", nameof(itemId));
		return itemId.Trim();
	}
}
