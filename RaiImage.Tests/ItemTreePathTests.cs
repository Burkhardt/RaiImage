using OsLib;
namespace RaiImage.Tests;
public class ItemTreePathTests
{
	private static RaiPath NewRoot()
	{
		var root = Os.TempDir / "RAIkeep" / "raiimage-tests" / "item-tree-path" / Guid.NewGuid().ToString("N");
		root.mkdir();
		return root;
	}

	[Fact]
	public void Apply_BuildsTopdirAndSubdirPath()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ItemTreePath(root, "12345678", PathConventionType.ItemIdTree3x3);
		Assert.Equal("123", sut.Topdir.Segments[0]);
		Assert.Equal("123456", sut.Subdir.Segments[0]);
		Assert.Equal(new RaiPath("/tmp/root/123/123456/").ToString(), sut.Path.ToString());
	}
	[Fact]
	public void Apply_DefaultsToItemIdTree8x2()
	{
		var sut = new ItemTreePath("/tmp/root/", "1234567890AB");
		Assert.Equal(PathConventionType.ItemIdTree8x2, sut.Convention);
		Assert.Equal("12345678", sut.Topdir.Segments[0]);
		Assert.Equal("1234567890", sut.Subdir.Segments[0]);
	}
	[Fact]
	public void Apply_RewritesConTopdirToC0N()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ItemTreePath(root, "con1234", PathConventionType.ItemIdTree3x3);
		Assert.Equal("C0N", sut.Topdir.Segments[0]);
		Assert.Equal("con123", sut.Subdir.Segments[0]);
		Assert.Equal(new RaiPath("/tmp/root/C0N/con123/").ToString(), sut.Path.ToString());
	}
	[Fact]
	public void RootPathSetter_RemovesDuplicateTreeSegments()
	{
		var sut = new ItemTreePath(new RaiPath("/tmp/root/123/123456/"), "12345678", PathConventionType.ItemIdTree3x3);
		Assert.Equal(new RaiPath("/tmp/root/").ToString(), sut.RootPath.ToString());
		Assert.Equal(new RaiPath("/tmp/root/123/123456/").ToString(), sut.Path.ToString());
	}
	[Fact]
	public void ConventionSplit_ReturnsCorrectLengthsForAllConventions()
	{
		Assert.Equal((3, 3), ItemTreePath.ConventionSplit(PathConventionType.ItemIdTree3x3));
		Assert.Equal((8, 2), ItemTreePath.ConventionSplit(PathConventionType.ItemIdTree8x2));
		Assert.Equal((7, 0), ItemTreePath.ConventionSplit(PathConventionType.CanonicalByName, "hello12"));
		Assert.Equal((0, 0), ItemTreePath.ConventionSplit(PathConventionType.CanonicalByName));
		Assert.Equal((0, 0), ItemTreePath.ConventionSplit(PathConventionType.Flat));
	}
	[Fact]
	public void FullPath_ReturnsComposedString()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ItemTreePath(root, "1234567890AB");
		// Convention defaults to 8x2: topdir=12345678, subdir=1234567890
		var expected = new RaiPath("/tmp/root/12345678/1234567890/").FullPath;
		Assert.Equal(expected, sut.FullPath);
	}
	[Fact]
	public void FullPath_RoundTrips_ThroughRaiPath()
	{
		// Construct an ItemTreePath the proper way: root + itemId
		var root = new RaiPath("/tmp/samples/");
		var itemId = "1234567890";
		var convention = PathConventionType.ItemIdTree3x3;
		var itp = new ItemTreePath(root, itemId, convention);
		// FullPath gives us the composed string: /tmp/samples/123/123456/
		var composedPath = itp.FullPath;
		Assert.EndsWith("/", composedPath);
		// We can wrap it in a plain RaiPath — this preserves the absolute string
		var p2 = new RaiPath(composedPath);
		Assert.Equal(composedPath, p2.FullPath);
		// Copy-constructing a RaiPath also preserves it
		var p3 = new RaiPath(itp.Path);
		Assert.Equal(composedPath, p3.FullPath);
		// But reconstructing an ItemTreePath requires root + itemId again,
		// because the convention split is not recoverable from the string alone.
		var itp2 = new ItemTreePath(root, itemId, convention);
		Assert.Equal(itp.FullPath, itp2.FullPath);
		Assert.Equal(itp.RootPath.FullPath, itp2.RootPath.FullPath);
	}
	[Fact]
	public void ItemTreePath_Reconstructs_WhenGivenComposedPathAsRoot()
	{
		// If someone feeds the composed path back as root, NormalizeRootPath
		// strips the topdir/subdir segments and recovers the original root.
		var root = new RaiPath("/tmp/samples/");
		var itemId = "1234567890";
		var convention = PathConventionType.ItemIdTree3x3;
		var itp = new ItemTreePath(root, itemId, convention);
		// Feed the full composed path as root — NormalizeRootPath should strip 123/123456/
		var itp2 = new ItemTreePath(itp.FullPath, itemId, convention);
		Assert.Equal(itp.FullPath, itp2.FullPath);
		Assert.Equal(root.FullPath, itp2.RootPath.FullPath);
	}

	[Fact]
	public void SelectFiles_ReturnsEveryFileForExactItemId_AndExcludesBucketSibling()
	{
		var root = NewRoot();
		try
		{
			var item = new ItemTreePath(root / "Nomsa", "AfricanBrisket");
			string[] expected =
			[
				"AfricanBrisket_01.png",
				"AfricanBrisket_02.webp",
				"AfricanBrisket.svg",
				"AfricanBrisket.puml",
				"AfricanBrisket_config.puml",
				"AfricanBrisket.raid"
			];
			foreach (var name in expected)
				Write(item.SubdirRoot, name);

			var sibling = new ItemTreePath(root / "Nomsa", "AfricanBrigadine");
			Assert.Equal(item.SubdirRoot.FullPath, sibling.SubdirRoot.FullPath);
			Write(sibling.SubdirRoot, "AfricanBrigadine.png");

			var selected = item.SelectFiles();

			Assert.Equal(expected.Order(), selected.Select(file => file.NameWithExtension).Order());
			Assert.DoesNotContain(selected, file => file.NameWithExtension == "AfricanBrigadine.png");
		}
		finally
		{
			root.rmdir(depth: 8, deleteFiles: true);
		}
	}

	[Fact]
	public void Move_RenamesCompleteMixedFileFamily_AndLeavesBucketSiblingUntouched()
	{
		var root = NewRoot();
		try
		{
			var source = new ItemTreePath(root / "Nomsa", "AfricanBrisket");
			Write(source.SubdirRoot, "AfricanBrisket_01.png");
			Write(source.SubdirRoot, "AfricanBrisket_02.webp");
			Write(source.SubdirRoot, "AfricanBrisket.svg");
			Write(source.SubdirRoot, "AfricanBrisket.puml");
			Write(source.SubdirRoot, "AfricanBrisket_config.puml");
			Write(source.SubdirRoot, "AfricanBrisket.raid");
			var sibling = Write(source.SubdirRoot, "AfricanBrigadine.png");
			var destination = new ItemTreePath(root / "Nomsa", "AfricanDinner");

			var moved = destination.mv(source);

			Assert.Equal(6, moved.Count);
			Assert.Empty(source.SelectFiles());
			Assert.True(sibling.Exists());
			Assert.Equal(
				new[]
				{
					"AfricanDinner.raid",
					"AfricanDinner.puml",
					"AfricanDinner.svg",
					"AfricanDinner_01.png",
					"AfricanDinner_02.webp",
					"AfricanDinner_config.puml"
				}.Order(),
				destination.SelectFiles().Select(file => file.NameWithExtension).Order().ToArray());
		}
		finally
		{
			root.rmdir(depth: 8, deleteFiles: true);
		}
	}

	[Fact]
	public void Move_ChangesSubscriberWithoutRenamingItem()
	{
		var root = NewRoot();
		try
		{
			var source = new ItemTreePath(root / "Nomsa", "AfricanBrisket");
			Write(source.SubdirRoot, "AfricanBrisket.png");
			Write(source.SubdirRoot, "AfricanBrisket.raid");
			var destination = new ItemTreePath(root / "AIA", source.ItemId);

			destination.mv(source);

			Assert.Empty(source.SelectFiles());
			Assert.Equal(2, destination.SelectFiles().Count);
		}
		finally
		{
			root.rmdir(depth: 8, deleteFiles: true);
		}
	}

	[Fact]
	public void Move_MigratesBetween8x2_3x3_AndFlatConventions()
	{
		var root = NewRoot();
		try
		{
			var eightByTwo = new ItemTreePath(root / "Nomsa", "AfricanBrisket", PathConventionType.ItemIdTree8x2);
			Write(eightByTwo.SubdirRoot, "AfricanBrisket.png");
			Write(eightByTwo.SubdirRoot, "AfricanBrisket.puml");
			var threeByThree = new ItemTreePath(root / "Nomsa", "AfricanBrisket", PathConventionType.ItemIdTree3x3);

			threeByThree.mv(eightByTwo);
			Assert.Equal(2, threeByThree.SelectFiles().Count);
			Assert.Empty(eightByTwo.SelectFiles());

			var flat = new ItemTreePath(root / "Nomsa", "AfricanBrisket", PathConventionType.Flat);
			flat.mv(threeByThree);
			Assert.Equal((root / "Nomsa").FullPath, flat.SubdirRoot.FullPath);
			Assert.Equal(2, flat.SelectFiles().Count);
			Assert.Empty(threeByThree.SelectFiles());

			eightByTwo.mv(flat);
			Assert.Equal(2, eightByTwo.SelectFiles().Count);
			Assert.Empty(flat.SelectFiles());
		}
		finally
		{
			root.rmdir(depth: 8, deleteFiles: true);
		}
	}

	[Fact]
	public void ImageTreeFile_ConstructedFromItemPath_ExtendsToFirstExistingImageOnly()
	{
		var root = NewRoot();
		try
		{
			var item = new ItemTreePath(root / "Nomsa", "AfricanBrisket");
			Write(item.SubdirRoot, "AfricanBrisket.raid");
			Write(item.SubdirRoot, "AfricanBrisket.png");
			var image = new ImageTreeFile(item);

			var found = image.ExtendToFirstExistingFile("jpg,png,webp,svg");

			Assert.True(found);
			Assert.Equal("png", image.Ext);
			Assert.Equal("AfricanBrisket.png", image.NameWithExtension);
		}
		finally
		{
			root.rmdir(depth: 8, deleteFiles: true);
		}
	}

	private static RaiFile Write(RaiPath directory, string name)
	{
		var file = new TextFile(directory, name);
		file.DeleteAll().Append(name).Save();
		return new RaiFile(file.FullName);
	}
}
