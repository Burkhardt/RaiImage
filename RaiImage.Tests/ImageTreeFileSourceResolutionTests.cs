using System.Runtime.CompilerServices;
using OsLib;
using Xunit;

namespace RaiImage.Tests;

/// <summary>
/// Bug A regression coverage for source resolution.
///
/// <para>The image tree stores a source <c>{itemId}.{ext}</c> alongside its
/// rendered derivatives <c>{itemId}_{Template}.{ext}</c> in the SAME 8x2 bucket.
/// Source probing must locate the actual source by <c>ItemId</c> across whatever
/// extension genuinely exists, and must NOT be fooled by a derivative that
/// merely shares the <c>ItemId</c> stem.</para>
///
/// <para>The observed bug: with <c>GageElementary.png</c> as the only source and
/// <c>GageElementary_Large.webp</c> / <c>GageElementary_Small.webp</c> sitting in
/// the bucket, <see cref="ImageTreeFile.FromImageTree(RaiPath,string,string,string,PathConventionType)"/>
/// adopted the derivative's <c>webp</c> extension and resolved to the
/// non-existent <c>GageElementary.webp</c>, throwing <see cref="FileNotFoundException"/>.</para>
/// </summary>
public class ImageTreeFileSourceResolutionTests
{
	[Fact]
	public void FromImageTree_ResolvesPngSource_IgnoringWebpDerivatives()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			const string subscriber = "Dr2RAI";
			const string itemId = "GageElementary";
			var subscriberRoot = imageTreeRoot / subscriber;

			// Seed the polluting rendered derivatives FIRST so a naive
			// directory-order probe is biased toward picking a webp.
			SeedFile(new ImageTreeFile(subscriberRoot, itemId, "Large", "webp"), "large-derivative");
			SeedFile(new ImageTreeFile(subscriberRoot, itemId, "Small", "webp"), "small-derivative");

			// The one and only real source is a .png.
			var source = new ImageTreeFile(subscriberRoot, itemId, string.Empty, "png");
			SeedFile(source, "png-source");

			var resolved = ImageTreeFile.FromImageTree(imageTreeRoot, subscriber, itemId);

			Assert.Equal(itemId, resolved.ItemId);
			Assert.Equal("png", resolved.Ext);
			Assert.Equal(source.FullName, resolved.FullName);
			Assert.True(resolved.Exists(), $"resolved source must exist on disk: {resolved.FullName}");
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void FromImageTree_ResolvesPngSource_WhenNoDerivativesPresent()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			const string subscriber = "Dr2RAI";
			const string itemId = "AfricanPicnic";
			var subscriberRoot = imageTreeRoot / subscriber;

			var source = new ImageTreeFile(subscriberRoot, itemId, string.Empty, "png");
			SeedFile(source, "png-source");

			var resolved = ImageTreeFile.FromImageTree(imageTreeRoot, subscriber, itemId);

			Assert.Equal("png", resolved.Ext);
			Assert.Equal(source.FullName, resolved.FullName);
			Assert.True(resolved.Exists());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ExtendToFirstExistingFile_Structured_WithImageNumber_ResolvesCorrectSource()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			
			// Seed base/source image: AfricanPicnic_01.png
			var sourceFile = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_01.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			SeedFile(sourceFile, "real-source");

			// Seed derivative image: AfricanPicnic_01_small.webp
			var derivativeFile = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_01_small.webp", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			SeedFile(derivativeFile, "derivative");

			// Seed another base image with different image number: AfricanPicnic_02.png
			var otherSourceFile = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_02.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			SeedFile(otherSourceFile, "other-source");

			// Construct object we want to resolve: target image number is 1, extension is wildcard/temporary
			var target = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_01.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			target.Ext = "*"; // Clear target extension to simulate discovery

			var resolved = target.ExtendToFirstExistingFile("webp,png", PathConventionType.ItemIdTree8x2);

			Assert.True(resolved);
			Assert.Equal("png", target.Ext);
			Assert.Equal(sourceFile.FullName, target.FullName);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ExtendToFirstExistingFile_Structured_WithoutImageNumber_ResolvesCorrectSource()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			
			// Seed base/source image: GageElementary.png
			var sourceFile = new ImageTreeFile(subscriberRoot.FullPath + "GageElementary.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			SeedFile(sourceFile, "real-source");

			// Seed derivative image: GageElementary_Small.png
			var derivativeFile = new ImageTreeFile(subscriberRoot.FullPath + "GageElementary_Small.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			SeedFile(derivativeFile, "derivative");

			var target = new ImageTreeFile(subscriberRoot.FullPath + "GageElementary.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);
			target.Ext = "*";

			var resolved = target.ExtendToFirstExistingFile("webp,png", PathConventionType.ItemIdTree8x2);

			Assert.True(resolved);
			Assert.Equal("png", target.Ext);
			Assert.Equal(sourceFile.FullName, target.FullName);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ExtendToFirstExistingFile_Legacy_WithImageNumber_ResolvesCorrectSource()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			
			// Seed base/source image: AfricanPicnic_0DEAD0_01.png (under Legacy)
			var sourceFile = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_0DEAD0_01.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Legacy);
			SeedFile(sourceFile, "real-source");

			// Seed derivative image: AfricanPicnic_0DEAD0_01_small.webp
			var derivativeFile = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_0DEAD0_01_small.webp", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Legacy);
			SeedFile(derivativeFile, "derivative");

			var target = new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_0DEAD0_01.png", PathConventionType.ItemIdTree8x2, ImageNamingConvention.Legacy);
			target.Ext = "*";

			var resolved = target.ExtendToFirstExistingFile("webp,png", PathConventionType.ItemIdTree8x2);

			Assert.True(resolved);
			Assert.Equal("png", target.Ext);
			Assert.Equal(sourceFile.FullName, target.FullName);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ExtendToFirstExistingFile_ItemTemplate_ResolvesCorrectSource()
	{
		var root = NewTestRoot();
		try
		{
			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			
			// Seed base/source image: BeMyNeighborDay2026.png (under ItemTemplate)
			var sourceFile = new ImageTreeFile(subscriberRoot, "BeMyNeighborDay2026", string.Empty, "png", PathConventionType.ItemIdTree8x2);
			SeedFile(sourceFile, "real-source");

			// Seed derivative image: BeMyNeighborDay2026_Huge.webp
			var derivativeFile = new ImageTreeFile(subscriberRoot, "BeMyNeighborDay2026", "Huge", "webp", PathConventionType.ItemIdTree8x2);
			SeedFile(derivativeFile, "derivative");

			var target = new ImageTreeFile(subscriberRoot, "BeMyNeighborDay2026", string.Empty, "png", PathConventionType.ItemIdTree8x2);
			target.Ext = "*";

			var resolved = target.ExtendToFirstExistingFile("webp,png", PathConventionType.ItemIdTree8x2);

			Assert.True(resolved);
			Assert.Equal("png", target.Ext);
			Assert.Equal(sourceFile.FullName, target.FullName);
		}
		finally
		{
			Cleanup(root);
		}
	}

	private static void SeedFile(ImageTreeFile file, string content)
	{
		file.mkdir();
		new TextFile(file.FullName, content);
	}

	private static RaiPath NewTestRoot([CallerMemberName] string testName = "")
	{
		var root = new RaiPath(Path.GetTempPath()) / "RAIkeep" / "raiimage-tests" / "source-resolution" / testName;
		Cleanup(root);
		root.mkdir();
		return root;
	}

	private static void Cleanup(RaiPath root)
	{
		if (root?.Exists() == true)
			root.rmdir(depth: 5, deleteFiles: true);
	}
}
