using OsLib;

namespace RaiImage.Tests;

public class ItemTreePathTests
{
	[Fact]
	public void Apply_BuildsTopdirAndSubdirPath()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ItemTreePath(root, "12345678", PathConventionType.ItemIdTree3x3);

		Assert.Equal("123", sut.Topdir);
		Assert.Equal("123456", sut.Subdir);
		Assert.Equal(new RaiPath("/tmp/root/123/123456/").ToString(), sut.Path.ToString());
	}

	[Fact]
	public void Apply_DefaultsToItemIdTree8x2()
	{
		var sut = new ItemTreePath("/tmp/root/", "1234567890AB");

		Assert.Equal(PathConventionType.ItemIdTree8x2, sut.Convention);
		Assert.Equal("12345678", sut.Topdir);
		Assert.Equal("1234567890", sut.Subdir);
	}

	[Fact]
	public void Apply_RewritesConTopdirToC0N()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ItemTreePath(root, "con1234", PathConventionType.ItemIdTree3x3);

		Assert.Equal("C0N", sut.Topdir);
		Assert.Equal("con123", sut.Subdir);
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
	public void GetSplit_ReturnsCorrectLengthsForAllConventions()
	{
		Assert.Equal((3, 3), ItemTreePath.GetSplit(PathConventionType.ItemIdTree3x3));
		Assert.Equal((8, 2), ItemTreePath.GetSplit(PathConventionType.ItemIdTree8x2));
		Assert.Equal((7, 0), ItemTreePath.GetSplit(PathConventionType.CanonicalByName, "hello12"));
		Assert.Equal((0, 0), ItemTreePath.GetSplit(PathConventionType.CanonicalByName));
	}
}
