using System.Globalization;
using System.Text;
using OsLib;

namespace RaiImage.Tests;

public sealed class UnicodeNormalizationTests
{
	private const string NfcItemId = "SchwäbischHallPlaceDiagram";
	private const string NfdItemId = "Schwa\u0308bischHallPlaceDiagram";

	[Fact]
	public void ItemTreePath_NormalizesBeforeDerivingEveryBucketSegment()
	{
		var path = new ItemTreePath(new RaiPath("/tmp/images/"), NfdItemId);

		Assert.Equal(NfcItemId, path.ItemId);
		Assert.Equal("Schwäbis", path.Topdir.Segments.Single());
		Assert.Equal("Schwäbisch", path.Subdir.Segments.Single());
		Assert.True(path.ItemId.IsNormalized(NormalizationForm.FormC));
		Assert.True(path.Topdir.Segments.Single().IsNormalized(NormalizationForm.FormC));
		Assert.True(path.Subdir.Segments.Single().IsNormalized(NormalizationForm.FormC));
	}

	[Fact]
	public void ItemTreePath_SlicesAtUnicodeTextElementBoundaries()
	{
		const string itemId = "1234567👩🏽‍💻ABCD";
		var path = new ItemTreePath(new RaiPath("/tmp/images/"), itemId);

		Assert.Equal("1234567👩🏽‍💻", path.Topdir.Segments.Single());
		Assert.Equal("1234567👩🏽‍💻AB", path.Subdir.Segments.Single());
		Assert.Equal(8, StringInfo.ParseCombiningCharacters(path.Topdir.Segments.Single()).Length);
		Assert.Equal(10, StringInfo.ParseCombiningCharacters(path.Subdir.Segments.Single()).Length);
	}

	[Theory]
	[InlineData(PathConventionType.ItemIdTree3x3, 3, 6)]
	[InlineData(PathConventionType.ItemIdTree8x2, 8, 10)]
	[InlineData(PathConventionType.CanonicalByName, 26, 0)]
	public void EveryPathConvention_UsesCanonicalTextElementPrefixes(
		PathConventionType convention,
		int topElements,
		int subElements)
	{
		var path = new ItemTreePath(new RaiPath("/tmp/images/"), NfdItemId, convention);

		Assert.True(path.Topdir.Segments.Single().IsNormalized(NormalizationForm.FormC));
		Assert.Equal(topElements, StringInfo.ParseCombiningCharacters(path.Topdir.Segments.Single()).Length);
		if (subElements == 0)
			Assert.True(path.Subdir.IsEmpty);
		else
		{
			Assert.True(path.Subdir.Segments.Single().IsNormalized(NormalizationForm.FormC));
			Assert.Equal(subElements, StringInfo.ParseCombiningCharacters(path.Subdir.Segments.Single()).Length);
		}
	}

	[Fact]
	public void ImageTreeFile_AuthorsCanonicalNfcNamesAndBuckets()
	{
		var file = new ImageTreeFile(
			new RaiPath("/tmp/images/AIA/"),
			NfdItemId,
			"Pra\u0308sentation",
			"svg");

		Assert.Equal(NfcItemId, file.ItemId);
		Assert.Equal("Präsentation", file.NameExt);
		Assert.Equal("Schwäbis", file.Topdir.Segments.Single());
		Assert.Equal("Schwäbisch", file.Subdir.Segments.Single());
		Assert.True(file.NameWithExtension.IsNormalized(NormalizationForm.FormC));
	}

	[Fact]
	public void ImageTreeTextFile_AuthorsCanonicalNfcNamesAndBuckets()
	{
		var file = new ImageTreeTextFile(
			new RaiPath("/tmp/images/AIA/"),
			NfdItemId,
			"Pra\u0308sentation",
			"puml");

		Assert.Equal(NfcItemId, file.ItemId);
		Assert.Equal("Präsentation", file.NameExt);
		Assert.Equal("Schwäbis", file.ItemPath.Topdir.Segments.Single());
		Assert.Equal("Schwäbisch", file.ItemPath.Subdir.Segments.Single());
		Assert.True(file.NameWithExtension.IsNormalized(NormalizationForm.FormC));
	}

	[Theory]
	[InlineData("Sa\u0303oTome\u0301Concert", "SãoToméConcert")]
	[InlineData("Garc\u0327onFestival", "GarçonFestival")]
	[InlineData("Tshivend\u032DaDiagram", "TshivenḓaDiagram")]
	public void ImageTreeFile_NormalizesInternationalIdentifiers(string decomposed, string expected)
	{
		var file = new ImageTreeFile(new RaiPath("/tmp/images/AIA/"), decomposed, string.Empty, "svg");

		Assert.Equal(expected, file.ItemId);
		Assert.True(file.ItemId.IsNormalized(NormalizationForm.FormC));
	}

	[Fact]
	public void ImageRenderRequest_NormalizesDecodedRouteValuesToNfc()
	{
		var request = ModernImgRouteConvention.Default.Parse(
			"/img/Sa\u0303oTome\u0301/Schwa\u0308bischHallPlaceDiagram");

		Assert.Equal("SãoTomé", request.Subscriber);
		Assert.Equal(NfcItemId, request.ItemId);
		Assert.True(request.Subscriber.IsNormalized(NormalizationForm.FormC));
		Assert.True(request.ItemId.IsNormalized(NormalizationForm.FormC));
	}

	[Fact]
	public void FromImageTree_ResolvesLegacyMixedNormalizationAtEveryLevel()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			imageTreeRoot.mkdir();
			const string subscriberNfc = "SãoTomé";
			const string subscriberNfd = "Sa\u0303oTome\u0301";
			var physicalSubscriber = imageTreeRoot / subscriberNfd;
			var physicalTop = physicalSubscriber / "Schwäbis";
			var physicalBucket = physicalTop / "Schwa\u0308bisch";
			physicalBucket.mkdir();
			new TextFile(physicalBucket, NfdItemId, "svg", "svg-source");

			var resolved = ImageTreeFile.FromImageTree(
				imageTreeRoot,
				subscriberNfc,
				NfcItemId,
				"svg");

			Assert.Equal(NfcItemId, resolved.ItemId);
			Assert.Equal("svg", resolved.Ext);
			Assert.True(resolved.Exists());
			Assert.Equal("svg-source", new TextFile(resolved.FullName).ReadAllText().Trim());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Theory]
	[InlineData("Sa\u0303oTome\u0301Concert", "SãoToméConcert")]
	[InlineData("Moc\u0327ambiqueFestival", "MoçambiqueFestival")]
	public void FromImageTree_ResolvesPortugueseDiacriticsInBucketsAndFilename(
		string decomposedItemId,
		string canonicalItemId)
	{
		var root = NewTestRoot();
		try
		{
			var subscriberRoot = root / "images" / "AIA";
			var physicalTop = subscriberRoot / TakeTextElements(canonicalItemId, 8).Normalize(NormalizationForm.FormD);
			var physicalBucket = physicalTop / TakeTextElements(canonicalItemId, 10).Normalize(NormalizationForm.FormD);
			physicalBucket.mkdir();
			new TextFile(physicalBucket, decomposedItemId, "svg", "portuguese-source");

			var resolved = ImageTreeFile.FromImageTree(
				root / "images",
				"AIA",
				canonicalItemId,
				"svg");

			Assert.Equal(canonicalItemId, resolved.ItemId);
			Assert.True(resolved.Exists());
			Assert.Equal("portuguese-source", new TextFile(resolved.FullName).ReadAllText().Trim());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void FromImageTree_RejectsAmbiguousCanonicalEquivalentFiles_WhenFilesystemAllowsBoth()
	{
		var root = NewTestRoot();
		try
		{
			var subscriberRoot = root / "images" / "AIA";
			var canonical = new ImageTreeFile(subscriberRoot, NfcItemId, string.Empty, "svg");
			canonical.mkdir();
			new TextFile(canonical.SubdirRoot, NfcItemId, "svg", "nfc");
			new TextFile(canonical.SubdirRoot, NfdItemId, "svg", "nfd");

			var physicalMatches = canonical.SubdirRoot.EnumerateFiles("*.svg").ToList();
			if (physicalMatches.Count < 2)
				return;

			Assert.Throws<RaiImageIOException>(() =>
				ImageTreeFile.FromImageTree(root / "images", "AIA", NfcItemId, "svg"));
		}
		finally
		{
			Cleanup(root);
		}
	}

	private static RaiPath NewTestRoot()
	{
		var root = Os.TempDir / "RAIkeep" / "raiimage-tests" / "unicode-normalization" / Guid.NewGuid().ToString("N");
		root.mkdir();
		return root;
	}

	private static string TakeTextElements(string value, int count)
	{
		var starts = StringInfo.ParseCombiningCharacters(value);
		return starts.Length <= count ? value : value[..starts[count]];
	}

	private static void Cleanup(RaiPath root)
	{
		if (root?.Exists() == true)
			root.rmdir(depth: 8, deleteFiles: true);
	}
}
