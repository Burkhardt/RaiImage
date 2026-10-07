using OsLib;

namespace RaiImage.Tests;

public sealed class FilenameNormalizationTests
{
    [Theory]
    [InlineData("Order_1.jpg", 1, "Order_001.jpg")]
    [InlineData("Order_01.jpg", 1, "Order_001.jpg")]
    [InlineData("Order_001.jpg", 1, "Order_001.jpg")]
    [InlineData("Order_100.jpg", 100, "Order_100.jpg")]
    [InlineData("Order_1000.jpg", 1000, "Order_1000.jpg")]
    public void StructuredReaderAcceptsAnyWidthAndGeneratorUsesMinimumThree(string source, int number, string expected)
    {
        var image = new ImageFile(source, ImageNamingConvention.Structured);
        Assert.Equal(number, image.ImageNumber);
        Assert.Equal(expected, image.NameWithExtension);
    }

    [Theory]
    [InlineData("Customer-Order-Sheet-26-10.jpg", "CustomerOrderSheet", 10)]
    [InlineData("Customer_Order_Sheet_26_10.jpg", "CustomerOrderSheet", 10)]
    [InlineData("Order_SHEET_001.jpg", "OrderSheet", 1)]
    [InlineData("Order-2026-Sheet-26-10.jpg", "Order2026Sheet", 10)]
    [InlineData("CustomerOrder10.jpg", "CustomerOrder", 10)]
    public void NumericSuffixIsSeparatedFromPascalCaseStem(string source, string stem, int number)
    {
        var normalized = new ImageFile(ImageFile.EasyFileName(System.IO.Path.Combine(System.IO.Path.GetTempPath(), source)), ImageNamingConvention.Structured);
        Assert.Equal(stem, normalized.ItemId);
        Assert.DoesNotContain("_", normalized.ItemId);
        Assert.Equal(number, normalized.ImageNumber);
        var expected = new ImageFile(System.IO.Path.Combine(System.IO.Path.GetTempPath(), stem + ".jpg"), ImageNamingConvention.Structured) { ImageNumber = number };
        Assert.Equal(expected.NameWithExtension, normalized.NameWithExtension);
    }
}
