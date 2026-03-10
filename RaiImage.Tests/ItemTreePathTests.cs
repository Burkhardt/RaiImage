using OsLib;

namespace RaiImage.Tests;

public class ItemTreePathTests
{
    [Fact]
    public void Apply_BuildsTopdirAndSubdirPath()
    {
        var root = new RaiFile("/tmp/root/").Path;
        var sut = new ItemTreePath(root, "12345678", topdirLength: 3, subdirLength: 3);

        Assert.Equal("123", sut.Topdir);
        Assert.Equal("123456", sut.Subdir);
        Assert.Equal(new RaiFile("/tmp/root/123/123456/").Path, sut.Path);
    }

    [Fact]
    public void Apply_RewritesConTopdirToC0N()
    {
        var root = new RaiFile("/tmp/root/").Path;
        var sut = new ItemTreePath(root, "con1234", topdirLength: 3, subdirLength: 3);

        Assert.Equal("C0N", sut.Topdir);
        Assert.Equal("con123", sut.Subdir);
        Assert.Equal(new RaiFile("/tmp/root/C0N/con123/").Path, sut.Path);
    }

    [Fact]
    public void RootPathSetter_RemovesDuplicateTreeSegments()
    {
        var sut = new ItemTreePath(new RaiFile("/tmp/root/123/123456/").Path, "12345678", topdirLength: 3, subdirLength: 3);

        Assert.Equal(new RaiFile("/tmp/root/").Path, sut.RootPath);
        Assert.Equal(new RaiFile("/tmp/root/123/123456/").Path, sut.Path);
    }
}
