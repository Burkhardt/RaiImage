using OsLib;
namespace RaiImage.Tests;
public class ImageFileTests
{
	[Fact]
	public void LegacyConvention_ParsesAllComponents()
	{
		// "308024_0DEAD0_01_zoom,4x4tile-17.tiff"
		var sut = new ImageFile("/tmp/root/308024_0DEAD0_01_zoom,4x4tile-17.tiff");
		Assert.Equal("308024", sut.ItemId);
		Assert.Equal("#0DEAD0", sut.Color.Code);
		Assert.Equal(1, sut.ImageNumber);
		Assert.Equal("zoom", sut.NameExt);
		Assert.Equal("4x4tile", sut.TileTemplate);
		Assert.Equal("17", sut.TileNumber);
		Assert.Equal("tiff", sut.Ext);
	}
	[Fact]
	public void LegacyConvention_ComposesName()
	{
		var sut = new ImageFile("/tmp/root/308024_0DEAD0_01_zoom,4x4tile-17.tiff");
		Assert.Equal("308024_0DEAD0_01_zoom,4x4tile-17", sut.Name);
		Assert.Equal("308024_0DEAD0_01_zoom,4x4tile-17.tiff", sut.NameWithExtension);
	}
	[Fact]
	public void LegacyConvention_ParsesSimpleName()
	{
		var sut = new ImageFile("/tmp/root/1234567890.webp");
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal(ImageFile.NoImageNumber, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.NameExt);
		Assert.Equal(string.Empty, sut.TileTemplate);
		Assert.Equal("webp", sut.Ext);
	}
	[Fact]
	public void LegacyConvention_ParsesSkuAndNumber()
	{
		var sut = new ImageFile("/tmp/root/308024_01.jpg");
		Assert.Equal("308024", sut.ItemId);
		Assert.Equal(1, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.NameExt);
		Assert.Equal("308024_01", sut.Name);
	}
	[Fact]
	public void LegacyConvention_ParsesSkuAndNameExt()
	{
		var sut = new ImageFile("/tmp/root/308024_zoom.jpg");
		Assert.Equal("308024", sut.ItemId);
		Assert.Equal(ImageFile.NoImageNumber, sut.ImageNumber);
		Assert.Equal("zoom", sut.NameExt);
		Assert.Equal("zoom", sut.TemplateName);
	}
	[Fact]
	public void ItemTemplateConvention_ParsesFromLegacyParsedName()
	{
		var sut = new ImageFile("/tmp/root/1234567890_thumbnail.webp", ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal("thumbnail", sut.TemplateName);
		Assert.Equal("thumbnail", sut.NameExt);
		Assert.Equal("webp", sut.Ext);
	}
	[Fact]
	public void ItemTemplateConvention_ComposesName()
	{
		var sut = new ImageFile("/tmp/root/1234567890_thumbnail.webp", ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890_thumbnail", sut.Name);
		Assert.Equal("1234567890_thumbnail.webp", sut.NameWithExtension);
	}
	[Fact]
	public void ItemTemplateConvention_WithoutTemplate()
	{
		var sut = new ImageFile("/tmp/root/1234567890.webp", ImageNamingConvention.ItemTemplate);
		Assert.Equal("1234567890", sut.ItemId);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("1234567890", sut.Name);
		Assert.Equal("1234567890.webp", sut.NameWithExtension);
	}
	[Fact]
	public void TemplateName_IsAlias_ForNameExt()
	{
		var sut = new ImageFile("/tmp/root/abc_zoom.png");
		sut.TemplateName = "thumbnail";
		Assert.Equal("thumbnail", sut.NameExt);
		sut.NameExt = "large";
		Assert.Equal("large", sut.TemplateName);
	}
	[Fact]
	public void ShortName_ComposesItemIdAndImageNumber()
	{
		var sut = new ImageFile("/tmp/root/308024_01_zoom.jpg");
		Assert.Equal("308024_01", sut.ShortName);
	}
}
