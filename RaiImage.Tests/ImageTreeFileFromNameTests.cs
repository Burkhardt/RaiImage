using OsLib;
using Xunit;

namespace RaiImage.Tests;

/// <summary>
/// Executable acceptance spec for the requested <c>ImageTreeFile.FromName(...)</c>
/// factory — see <c>Request_ImageTreeFile_NamingAwareCtor.md</c> at the RaiImage
/// repo root.
///
/// <para>Contract: construct/parse an <see cref="ImageTreeFile"/> from a route
/// <b>ShortName</b> (e.g. <c>AfricanPicnic_04</c>) or an unnumbered name
/// (e.g. <c>GageElementary</c>) rooted at a <see cref="RaiPath"/>, <b>auto-inferring</b>
/// the <see cref="ImageNamingConvention"/> the same way <c>FromImageTree</c> does
/// (Structured when a numeric ImageNumber segment is present, otherwise Legacy) —
/// with NO extension required, NO source file on disk, and NO caller-side
/// inference. <c>SubdirRoot</c> must resolve to the same 8x2 bucket regardless of
/// the TemplateName, so callers can enumerate the bucket and delete derivatives
/// (non-empty <c>TemplateName</c>) while keeping the source (empty
/// <c>TemplateName</c>).</para>
///
/// <para><b>NOTE:</b> <c>ImageTreeFile.FromName</c> does not exist yet — this file
/// will not compile until the API is added. That is the intentional TDD target;
/// the only undefined symbol is <c>FromName</c>. (<c>-1</c> is the
/// <c>NoImageNumber</c> sentinel, matching the existing tests.)</para>
/// </summary>
public class ImageTreeFileFromNameTests
{
	private const int NoImageNumber = -1;
	private static readonly RaiPath Root = new("/tmp/img-root/");

	[Fact]
	public void FromName_NumberedShortName_ParsesItemIdAndImageNumber()
	{
		var sut = ImageTreeFile.FromName(Root, "AfricanPicnic_04");
		Assert.Equal("AfricanPicnic", sut.ItemId);
		Assert.Equal(4, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("AfricanPicnic_04", sut.ShortName);
	}

	[Fact]
	public void FromName_NumberedDerivative_KeepsShortNameAndBucket_ExposesTemplate()
	{
		var src = ImageTreeFile.FromName(Root, "AfricanPicnic_04");
		var deriv = ImageTreeFile.FromName(Root, "AfricanPicnic_04_Small");

		Assert.Equal("AfricanPicnic", deriv.ItemId);
		Assert.Equal(4, deriv.ImageNumber);
		Assert.Equal("Small", deriv.TemplateName);
		Assert.Equal("AfricanPicnic_04", deriv.ShortName);
		// Source and derivative live in the same 8x2 bucket.
		Assert.Equal(src.SubdirRoot.FullPath, deriv.SubdirRoot.FullPath);
	}

	[Fact]
	public void FromName_UnnumberedShortName_HasNoImageNumber()
	{
		var sut = ImageTreeFile.FromName(Root, "GageElementary");
		Assert.Equal("GageElementary", sut.ItemId);
		Assert.Equal(NoImageNumber, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("GageElementary", sut.ShortName);
	}

	[Fact]
	public void FromName_UnnumberedDerivative_KeepsShortNameAndBucket_ExposesTemplate()
	{
		var src = ImageTreeFile.FromName(Root, "GageElementary");
		var deriv = ImageTreeFile.FromName(Root, "GageElementary_Huge");

		Assert.Equal("GageElementary", deriv.ItemId);
		Assert.Equal(NoImageNumber, deriv.ImageNumber);
		Assert.Equal("Huge", deriv.TemplateName);
		Assert.Equal("GageElementary", deriv.ShortName);
		Assert.Equal(src.SubdirRoot.FullPath, deriv.SubdirRoot.FullPath);
	}

	[Fact]
	public void FromName_StripsKnownExtensionOnInput()
	{
		// A route value may carry a known extension; FromName must strip it
		// (so the caller never has to), and the original's format is irrelevant.
		var sut = ImageTreeFile.FromName(Root, "AfricanPicnic_04.png");
		Assert.Equal("AfricanPicnic", sut.ItemId);
		Assert.Equal(4, sut.ImageNumber);
		Assert.Equal(string.Empty, sut.TemplateName);
		Assert.Equal("AfricanPicnic_04", sut.ShortName);
	}

	[Fact]
	public void InferSourceNamingConvention_IsPublicAndMatchesFromNameBehavior()
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
		Assert.Equal(ImageTreeFile.FromName(Root, "AfricanPicnic_04").SubdirRoot.FullPath,
			sut.SubdirRoot.FullPath);
	}
}
