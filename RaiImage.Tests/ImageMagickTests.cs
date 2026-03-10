namespace RaiImage.Tests;

public class ImageMagickTests
{
    [Fact]
    public void Constructor_DoesNotRequireFixedInstallPath_WhenImPathEmpty()
    {
        var before = ImageMagick.ImPath;
        try
        {
            ImageMagick.ImPath = string.Empty;
            var sut = new ImageMagick();
            Assert.NotNull(sut);
        }
        finally
        {
            ImageMagick.ImPath = before;
        }
    }

    [Fact]
    public void Constructor_Throws_WhenImPathSetButMagickNotFound()
    {
        var before = ImageMagick.ImPath;
        try
        {
            ImageMagick.ImPath = "/tmp/raimage-missing-magick";
            Assert.Throws<System.IO.FileNotFoundException>(() => new ImageMagick());
        }
        finally
        {
            ImageMagick.ImPath = before;
        }
    }
}
