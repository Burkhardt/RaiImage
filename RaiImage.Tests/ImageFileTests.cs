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
	#region Structured naming convention
	[Fact]
	public void StructuredConvention_ParsesAllComponents()
	{
		var sut = new ImageFile(
			"/tmp/root/471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100", sut.ItemId);
		Assert.Equal(3, sut.ImageNumber);
		Assert.Equal("FullSizeHQ", sut.NameExt);
		Assert.Equal("Himmelblau", sut.Color.Name);
		Assert.Equal("TilesZoomLevel3", sut.TileTemplate);
		Assert.Equal("37", sut.TileNumber);
		Assert.Equal("webp", sut.Ext);
	}
	[Fact]
	public void StructuredConvention_ComposesAllComponents()
	{
		var sut = new ImageFile(
			"/tmp/root/471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37", sut.Name);
		Assert.Equal("471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37.webp", sut.NameWithExtension);
	}
	[Fact]
	public void StructuredConvention_ParsesPositionalOnly()
	{
		var sut = new ImageFile(
			"/tmp/root/471100_03_FullSizeHQ.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100", sut.ItemId);
		Assert.Equal(3, sut.ImageNumber);
		Assert.Equal("FullSizeHQ", sut.NameExt);
		Assert.Null(sut.Color);
		Assert.Equal(string.Empty, sut.TileTemplate);
		Assert.Equal("471100_03_FullSizeHQ", sut.Name);
	}
	[Fact]
	public void StructuredConvention_ParsesItemIdOnly()
	{
		var sut = new ImageFile(
			"/tmp/root/471100.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100", sut.ItemId);
		Assert.Equal(ImageFile.NoImageNumber, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.NameExt);
		Assert.Equal("471100", sut.Name);
	}
	[Fact]
	public void StructuredConvention_ParsesItemIdAndNumber()
	{
		var sut = new ImageFile(
			"/tmp/root/471100_03.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100", sut.ItemId);
		Assert.Equal(3, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.NameExt);
		Assert.Equal("471100_03", sut.Name);
	}
	[Fact]
	public void StructuredConvention_ParsesItemIdAndNameExt()
	{
		var sut = new ImageFile(
			"/tmp/root/471100_FullSizeHQ.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100", sut.ItemId);
		Assert.Equal(ImageFile.NoImageNumber, sut.ImageNumber);
		Assert.Equal("FullSizeHQ", sut.NameExt);
		Assert.Equal("471100_FullSizeHQ", sut.Name);
	}
	[Fact]
	public void StructuredConvention_ParsesColorWithoutTile()
	{
		var sut = new ImageFile(
			"/tmp/root/471100_03_FullSizeHQ,Himmelblau.webp",
			ImageNamingConvention.Structured);
		Assert.Equal("471100", sut.ItemId);
		Assert.Equal(3, sut.ImageNumber);
		Assert.Equal("FullSizeHQ", sut.NameExt);
		Assert.Equal("Himmelblau", sut.Color.Name);
		Assert.Equal(string.Empty, sut.TileTemplate);
		Assert.Equal("471100_03_FullSizeHQ,Himmelblau", sut.Name);
	}
	[Fact]
	public void StructuredConvention_RoundTrips()
	{
		var original = "471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37";
		var sut = new ImageFile(
			$"/tmp/root/{original}.webp",
			ImageNamingConvention.Structured);
		Assert.Equal(original, sut.Name);
	}
	[Fact]
	public void StructuredConvention_GlobPrefix_MatchesAllColorVariants()
	{
		// The positional prefix before the comma is the glob-searchable part
		var sut = new ImageFile(
			"/tmp/root/471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37.webp",
			ImageNamingConvention.Structured);
		var prefix = $"{sut.ItemId}_{sut.ImageNumber:D2}_{sut.NameExt}";
		Assert.Equal("471100_03_FullSizeHQ", prefix);
		// A glob of "471100_03_FullSizeHQ,*" would match any color/tile variant
		Assert.StartsWith(prefix + ",", sut.NameWithExtension);
	}
	#endregion
}
