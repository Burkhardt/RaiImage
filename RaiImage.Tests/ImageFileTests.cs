namespace RaiImage.Tests;

public class ImageFileTests
{
    [Fact]
    public void EasyFileName_PadsNumericSkuAndDefaultsImageNumberAndExtension()
    {
        var result = ImageFile.EasyFileName("12", renameFile: false);

        Assert.Equal("0012_01.jpg", result);
    }

    [Fact]
    public void EasyFileName_RemovesTrailingUnderscore()
    {
        var result = ImageFile.EasyFileName("abc_", renameFile: false);

        Assert.Equal("abc0_01.jpg", result);
    }
}
