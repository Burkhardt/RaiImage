using OsLib;
using Xunit;

namespace RaiImage.Tests;

/// <summary>
/// Executable acceptance spec for naming-aware <see cref="ImageTreeFile"/> construction
/// path — see <c>doc/RaiImage_CR_ImageTreeFile_NamingAwareCtor-resolved-in-v3.9.0.md</c>
/// at the RAIkeep repo root.
///
/// <para>Contract: construct/parse an <see cref="ImageTreeFile"/> from a route
/// <b>ShortName</b> (e.g. <c>AfricanPicnic_04</c>) or an unnumbered name
/// (e.g. <c>GageElementary</c>) rooted at a <see cref="RaiPath"/>, <b>auto-inferring</b>
/// the <see cref="ImageNamingConvention"/> without a static factory
/// (Structured when a numeric ImageNumber segment is present, otherwise Legacy) —
/// with NO extension required, NO source file on disk, and NO caller-side
/// inference. <c>SubdirRoot</c> must resolve to the same 8x2 bucket regardless of
/// the TemplateName, so callers can enumerate the bucket and delete derivatives
/// (non-empty <c>TemplateName</c>) while keeping the source (empty
/// <c>TemplateName</c>).</para>
///
/// </summary>
public class ImageTreeFileConstructionTests
{
	private const int NoImageNumber = -1;
	private static readonly RaiPath Root = new("/tmp/img-root/");

	[Fact]
	public void Constructor_NumberedShortName_ParsesItemIdAndImageNumber()
	{
		var sut = new ImageTreeFile(Root, "AfricanPicnic_04");
		Assert.Equal("AfricanPicnic", sut.ItemId);
		Assert.Equal(4, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("AfricanPicnic_04", sut.ShortName);
	}

	[Fact]
	public void Constructor_NumberedDerivative_KeepsShortNameAndBucket_ExposesTemplate()
	{
		var src = new ImageTreeFile(Root, "AfricanPicnic_04");
		var deriv = new ImageTreeFile(Root, "AfricanPicnic_04_Small");

		Assert.Equal("AfricanPicnic", deriv.ItemId);
		Assert.Equal(4, deriv.ImageNumber);
		Assert.Equal("Small", deriv.TemplateName);
		Assert.Equal("AfricanPicnic_04", deriv.ShortName);
		// Source and derivative live in the same 8x2 bucket.
		Assert.Equal(src.SubdirRoot.FullPath, deriv.SubdirRoot.FullPath);
	}

	[Fact]
	public void Constructor_UnnumberedShortName_HasNoImageNumber()
	{
		var sut = new ImageTreeFile(Root, "GageElementary");
		Assert.Equal("GageElementary", sut.ItemId);
		Assert.Equal(NoImageNumber, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("GageElementary", sut.ShortName);
	}

	[Fact]
	public void Constructor_UnnumberedDerivative_KeepsShortNameAndBucket_ExposesTemplate()
	{
		var src = new ImageTreeFile(Root, "GageElementary");
		var deriv = new ImageTreeFile(Root, "GageElementary_Huge");

		Assert.Equal("GageElementary", deriv.ItemId);
		Assert.Equal(NoImageNumber, deriv.ImageNumber);
		Assert.Equal("Huge", deriv.TemplateName);
		Assert.Equal("GageElementary", deriv.ShortName);
		Assert.Equal(src.SubdirRoot.FullPath, deriv.SubdirRoot.FullPath);
	}

	[Fact]
	public void Constructor_StripsKnownExtensionOnInput()
	{
		// A route value may carry a known extension; FromName must strip it
		// (so the caller never has to), and the original's format is irrelevant.
		var sut = new ImageTreeFile(Root, "AfricanPicnic_04.png");
		Assert.Equal("AfricanPicnic", sut.ItemId);
		Assert.Equal(4, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("AfricanPicnic_04", sut.ShortName);
	}

	[Fact]
	public void InferSourceNamingConvention_IsPublicAndMatchesConstructorBehavior()
	{
		Assert.Equal(ImageNamingConvention.Structured,
			ImageTreeFile.InferSourceNamingConvention("AfricanPicnic_04_Small.webp"));
		Assert.Equal(ImageNamingConvention.Legacy,
			ImageTreeFile.InferSourceNamingConvention("GageElementary_Huge.webp"));
	}

	[Fact]
	public void NamingAwareComponentConstructor_ParsesCombinedStructuredName()
	{
		var sut = new ImageTreeFile(Root, "AfricanPicnic_04", "Small", string.Empty,
			PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured);

		Assert.Equal("AfricanPicnic", sut.ItemId);
		Assert.Equal(4, sut.ImageNumber);
		Assert.Equal("Small", sut.TemplateName);
		Assert.Equal("AfricanPicnic_04", sut.ShortName);
		Assert.Equal(new ImageTreeFile(Root, "AfricanPicnic_04").SubdirRoot.FullPath,
			sut.SubdirRoot.FullPath);
	}
}
