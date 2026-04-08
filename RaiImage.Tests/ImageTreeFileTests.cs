using OsLib;
namespace RaiImage.Tests;
public class ImageTreeFileTests
{
	[Fact]
	public void ParsesItemIdAndTemplateName_WithItemTemplateConvention()
	{
		var sut = new ImageTreeFile("/tmp/root/1234567890_thumbnail.webp",
			naming: ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal("thumbnail", sut.TemplateName);
		Assert.Equal("webp", sut.Ext);
	}
	[Fact]
	public void ParsesItemIdOnly_WhenNoTemplateName()
	{
		var sut = new ImageTreeFile("/tmp/root/1234567890.webp",
			naming: ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("webp", sut.Ext);
	}
	[Fact]
	public void LegacyDefault_ParsesFullComponents()
	{
		var sut = new ImageTreeFile("/tmp/root/308024_0DEAD0_01_zoom,4x4tile-17.tiff");
		Assert.Equal("308024", sut.ItemId);
		Assert.Equal("#0DEAD0", sut.Color.Code);
		Assert.Equal(1, sut.ImageNumber);
		Assert.Equal("zoom", sut.NameExt);
		Assert.Equal("4x4tile", sut.TileTemplate);
		Assert.Equal("17", sut.TileNumber);
	}
	[Fact]
	public void Name_ComposesItemTemplate_Correctly()
	{
		var sut = new ImageTreeFile("/tmp/root/1234567890_thumbnail.webp",
			naming: ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890_thumbnail", sut.Name);
		Assert.Equal("1234567890_thumbnail.webp", sut.NameWithExtension);
	}
	[Fact]
	public void Name_OmitsUnderscore_WhenTemplateNameIsEmpty()
	{
		var sut = new ImageTreeFile("/tmp/root/1234567890.webp",
			naming: ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890", sut.Name);
		Assert.Equal("1234567890.webp", sut.NameWithExtension);
	}
	[Fact]
	public void FullName_IncludesTreeStructure()
	{
		// 8x2 default: topdir = first 8 chars, subdir = first 10 chars
		var sut = new ImageTreeFile("/tmp/root/1234567890_thumbnail.webp",
			naming: ImageNamingConvention.ItemTemplate);
		var expected = new RaiPath("/tmp/root/12345678/1234567890/").FullPath
			+ "1234567890_thumbnail.webp";
		Assert.Equal(expected, sut.FullName);
	}
	[Fact]
	public void ComponentConstructor_BuildsCorrectFullName()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ImageTreeFile(root, "1234567890", "thumbnail", "webp");
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal("thumbnail", sut.TemplateName);
		Assert.Equal("webp", sut.Ext);
		var expected = new RaiPath("/tmp/root/12345678/1234567890/").FullPath
			+ "1234567890_thumbnail.webp";
		Assert.Equal(expected, sut.FullName);
	}
	[Fact]
	public void ComponentConstructor_WithEmptyTemplate()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ImageTreeFile(root, "1234567890", "", "webp");
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("1234567890.webp", sut.NameWithExtension);
	}
	[Fact]
	public void FullName_RoundTrips_ThroughParse()
	{
		var root = new RaiPath("/tmp/root/");
		var original = new ImageTreeFile(root, "1234567890", "thumbnail", "webp");
		var fullName = original.FullName;
		// Parse back — component constructor uses ItemTemplate convention
		var parsed = new ImageTreeFile(fullName,
			naming: ImageNamingConvention.ItemTemplate);
		Assert.Equal(original.ItemId, parsed.ItemId);
		Assert.Equal(original.TemplateName, parsed.TemplateName);
		Assert.Equal(original.Ext, parsed.Ext);
		Assert.Equal(original.FullName, parsed.FullName);
		Assert.Equal(root.FullPath, parsed.Path.FullPath);
	}
	[Fact]
	public void FullName_RoundTrips_WithDifferentPathConventions()
	{
		var root = new RaiPath("/tmp/root/");
		var itemId = "1234567890";
		var template = "zoom";
		// 3x3 convention
		var itf3x3 = new ImageTreeFile(root, itemId, template, "webp", PathConventionType.ItemIdTree3x3);
		var parsed3x3 = new ImageTreeFile(itf3x3.FullName, PathConventionType.ItemIdTree3x3,
			ImageNamingConvention.ItemTemplate);
		Assert.Equal(itf3x3.FullName, parsed3x3.FullName);
		Assert.Equal(root.FullPath, parsed3x3.Path.FullPath);
		// 8x2 convention
		var itf8x2 = new ImageTreeFile(root, itemId, template, "webp", PathConventionType.ItemIdTree8x2);
		var parsed8x2 = new ImageTreeFile(itf8x2.FullName, PathConventionType.ItemIdTree8x2,
			ImageNamingConvention.ItemTemplate);
		Assert.Equal(itf8x2.FullName, parsed8x2.FullName);
		Assert.Equal(root.FullPath, parsed8x2.Path.FullPath);
	}
	[Fact]
	public void PathProperty_ReturnsRootWithoutTreeSegments()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ImageTreeFile(root, "1234567890", "thumbnail", "webp");
		Assert.Equal(root.FullPath, sut.Path.FullPath);
		Assert.Equal(new RaiPath("/tmp/root/12345678/1234567890/").FullPath, sut.SubdirRoot.FullPath);
	}
	[Fact]
	public void SettingTemplateName_UpdatesName()
	{
		var root = new RaiPath("/tmp/root/");
		var sut = new ImageTreeFile(root, "1234567890", "thumbnail", "webp");
		Assert.Equal("1234567890_thumbnail", sut.Name);
		sut.TemplateName = "zoom";
		Assert.Equal("1234567890_zoom", sut.Name);
		sut.TemplateName = "";
		Assert.Equal("1234567890", sut.Name);
	}
}
