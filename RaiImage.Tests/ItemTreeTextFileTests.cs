using OsLib;

namespace RaiImage.Tests;

public sealed class ItemTreeTextFileTests
{
	[Fact]
	public void TextAndImageFilesShareItemBucketForEveryPathConvention()
	{
		var subscriberRoot = Os.TempDir / "RAIkeep" / "raiimage-tests" / "AfricaStage";
		foreach (var convention in Enum.GetValues<PathConventionType>())
		{
			var source = new ItemTreeTextFile(subscriberRoot, "CenterUseCase", string.Empty, "puml", convention);
			var config = new ItemTreeTextFile(subscriberRoot, "CenterUseCase", "config", "puml", convention);
			var svg = new ImageTreeFile(
				new ItemTreePath(subscriberRoot, "CenterUseCase", convention),
				ext: "svg");

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
			new ItemTreeTextFile(subscriberRoot, "../CenterUseCase", string.Empty, "puml"));
		Assert.Throws<ArgumentException>(() =>
			new ItemTreeTextFile(subscriberRoot, "CenterUseCase", "../config", "puml"));
		Assert.Throws<ArgumentException>(() =>
			new ItemTreeTextFile(subscriberRoot, "CenterUseCase", "config", "config.puml"));
	}

	[Fact]
	public void TextSiblingPreservesSubscriberItemPathAndUsesNameExt()
	{
		var subscriberRoot = Os.TempDir / "RAIkeep" / "raiimage-tests" / "AfricaStage";
		var source = new ItemTreeTextFile(subscriberRoot, "CenterUseCase", string.Empty, "puml");

		var config = source.CreateSibling("config", "puml");

		Assert.Equal(source.SubscriberRoot.FullPath, config.SubscriberRoot.FullPath);
		Assert.Equal(source.SubdirRoot.FullPath, config.SubdirRoot.FullPath);
		Assert.Equal(source.ItemId, config.ItemId);
		Assert.Equal("CenterUseCase_config.puml", config.NameWithExtension);
	}
}
