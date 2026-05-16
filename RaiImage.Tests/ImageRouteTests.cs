using Xunit;

namespace RaiImage.Tests;

public class ImageRouteTests
{
	[Fact]
	public void ModernImgRouteConvention_ParsesRouteWithTmpQuery()
	{
		var route = ModernImgRouteConvention.Default;

		var ok = route.TryParse(
			"/img/Dr2RAI/Screenshot20260502At13.06.17.png?tmp=SmallNewBargain&cache=warm",
			out var request);

		Assert.True(ok);
		Assert.NotNull(request);
		Assert.Equal("Dr2RAI", request.Subscriber);
		Assert.Equal("Screenshot20260502At13.06.17", request.ItemId);
		Assert.Equal("Small", request.TemplateName);
		Assert.Equal(["New", "Bargain"], request.OverlayNames);
	}

	[Fact]
	public void ModernImgRouteConvention_BuildsRouteWithTmpQuery()
	{
		var request = new ImageRenderRequest(
			"Dr2RAI",
			"Screenshot20260502At13.06.17",
			"Small",
			["New", "Bargain"]);

		var link = ModernImgRouteConvention.Default.Build(request);

		Assert.Equal("/img/Dr2RAI/Screenshot20260502At13.06.17?tmp=SmallNewBargain", link);
	}

	[Fact]
	public void ModernImgRouteConvention_RejectsNonImgRoutes()
	{
		var ok = ModernImgRouteConvention.Default.TryParse(
			"/assets/Dr2RAI/Screenshot20260502At13.06.17?tmp=Small",
			out var request);

		Assert.False(ok);
		Assert.Null(request);
	}

	[Fact]
	public void Tmp_ParsesBareParamAndFullQuery_WithoutLeakingOldOverlays()
	{
		var tmp = new Tmp("LargeNew");

		Assert.Equal("Large", tmp.Template);
		Assert.Equal(["New"], tmp.Overlays);

		tmp.Template = "TMP=SmallBargain";

		Assert.Equal("Small", tmp.Template);
		Assert.Equal(["Bargain"], tmp.Overlays);

		tmp.Template = "/img/Dr2RAI/Foo?tmp=Tiny&cache=warm";

		Assert.Equal("Tiny", tmp.Template);
		Assert.Empty(tmp.Overlays);
		Assert.Equal("Tiny", tmp.String);
	}
}
