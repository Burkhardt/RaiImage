using OsLib;
namespace RaiImage.Tests;
public class ItemTreePathTests
{
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
}
