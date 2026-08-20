using OsLib;

namespace RaiImage.Tests;

public sealed class ImageTreeTextFileTests
{
	[Fact]
	public void TextAndImageFilesShareItemBucketForEveryPathConvention()
	{
		var subscriberRoot = Os.TempDir / "RAIkeep" / "raiimage-tests" / "AfricaStage";
		foreach (var convention in Enum.GetValues<PathConventionType>())
		{
			var source = new ImageTreeTextFile(subscriberRoot, "CenterUseCase", string.Empty, "puml", convention);
			var config = new ImageTreeTextFile(subscriberRoot, "CenterUseCase", "config", "puml", convention);
			var svg = ImageTreeFile.FromItemTree(
				subscriberRoot,
				"CenterUseCase",
				string.Empty,
				"svg",
				convention);

			Assert.Equal(source.SubdirRoot.FullPath, config.SubdirRoot.FullPath);
			Assert.Equal(source.SubdirRoot.FullPath, svg.SubdirRoot.FullPath);
			Assert.Equal("CenterUseCase_config.puml", config.NameWithExtension);
			Assert.Equal("config", config.NameExt);
			Assert.Equal("puml", config.Ext);
		}
	}

	[Fact]
	public void TextFileRejectsItemNameExtAndExtensionInjection()
	{
		var subscriberRoot = Os.TempDir / "RAIkeep" / "raiimage-tests" / "AfricaStage";

		Assert.Throws<ArgumentException>(() =>
			new ImageTreeTextFile(subscriberRoot, "../CenterUseCase", string.Empty, "puml"));
		Assert.Throws<ArgumentException>(() =>
			new ImageTreeTextFile(subscriberRoot, "CenterUseCase", "../config", "puml"));
		Assert.Throws<ArgumentException>(() =>
			new ImageTreeTextFile(subscriberRoot, "CenterUseCase", "config", "config.puml"));
	}

	[Fact]
	public void TextSiblingPreservesSubscriberItemPathAndUsesNameExt()
	{
		var subscriberRoot = Os.TempDir / "RAIkeep" / "raiimage-tests" / "AfricaStage";
		var source = new ImageTreeTextFile(subscriberRoot, "CenterUseCase", string.Empty, "puml");

		var config = source.CreateSibling("config", "puml");

		Assert.Equal(source.SubscriberRoot.FullPath, config.SubscriberRoot.FullPath);
		Assert.Equal(source.SubdirRoot.FullPath, config.SubdirRoot.FullPath);
		Assert.Equal(source.ItemId, config.ItemId);
		Assert.Equal("CenterUseCase_config.puml", config.NameWithExtension);
	}
}
