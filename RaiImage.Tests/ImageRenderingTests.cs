using System.Runtime.CompilerServices;
using OsLib;
using Xunit;

namespace RaiImage.Tests;

[Collection("ImageMagick static state")]
public class ImageRenderingTests : IDisposable
{
	private readonly RaiPath imPath = ImageMagick.ImPath;
	private readonly string magickCommand = ImageMagick.MagickCommand;

	public void Dispose()
	{
		ImageMagick.ImPath = imPath;
		ImageMagick.MagickCommand = magickCommand;
	}

	[Fact]
	public void ApplyTemplate_PreservesImageNumberInRenderedOutputNames()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = new RaiFile(root, "magick", "log");
			var script = CreateFakeMagickScript(tools, log.FullName);
			ImageMagick.ImPath = tools;
			ImageMagick.MagickCommand = new RaiFile(script).NameWithExtension;

			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			SeedFile(new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_01.png",
				PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured), "source-01");
			SeedFile(new ImageTreeFile(subscriberRoot.FullPath + "AfricanPicnic_02.png",
				PathConventionType.ItemIdTree8x2, ImageNamingConvention.Structured), "source-02");
			SeedFile(new ImageTreeFile(subscriberRoot, "GageElementary", string.Empty, "png"), "source-no-number");

			var template = new TemplateSetting
			{
				Name = "Huge",
				Resize = "300x200>",
				Format = "webp"
			};

			var rendered01 = ImageTreeFile.ApplyTemplate(imageTreeRoot, "Dr2RAI", "AfricanPicnic_01", template);
			var rendered02 = ImageTreeFile.ApplyTemplate(imageTreeRoot, "Dr2RAI", "AfricanPicnic_02", template);
			var renderedNoNumber = ImageTreeFile.ApplyTemplate(imageTreeRoot, "Dr2RAI", "GageElementary", template);

			Assert.True(rendered01.Exists());
			Assert.True(rendered02.Exists());
			Assert.True(renderedNoNumber.Exists());
			Assert.NotEqual(rendered01.FullName, rendered02.FullName);
			Assert.EndsWith("AfricanPicnic_01_Huge.webp", rendered01.FullName);
			Assert.EndsWith("AfricanPicnic_02_Huge.webp", rendered02.FullName);
			Assert.EndsWith("GageElementary_Huge.webp", renderedNoNumber.FullName);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void StaticApplyTemplate_ResolvesExternalLinkSource_AndCreatesRenderedImage()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = new RaiFile(root, "magick", "log");
			var script = CreateFakeMagickScript(tools, log.FullName);
			ImageMagick.ImPath = tools;
			ImageMagick.MagickCommand = new RaiFile(script).NameWithExtension;

			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			var source = new ImageTreeFile(subscriberRoot, "Screenshot20260509At14.46.25Copy", string.Empty, "png");
			source.mkdir();
			new TextFile(source.FullName, "source");

			var template = new TemplateSetting
			{
				Name = "Huge",
				Resize = "300x200>",
				Format = "webp",
				Quality = "82",
				Strip = true
			};
			var rendered = ImageTreeFile.ApplyTemplate(
				imageTreeRoot,
				"/img/Dr2RAI/Screenshot20260509At14.46.25Copy?tmp=HugeNewBargain",
				template);

			Assert.True(rendered.Exists());
			Assert.Equal("Screenshot20260509At14.46.25Copy", rendered.ItemId);
			Assert.Equal("Huge", rendered.TemplateName);
			Assert.Equal("webp", rendered.Ext);
			Assert.Contains("300x200>", string.Join("\n", new TextFile(log.FullName).Lines));
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ApplyOverlays_UsesRenderOrder_AndReturnsFinalDaisyChainImage()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = new RaiFile(root, "magick", "log");
			var script = CreateFakeMagickScript(tools, log.FullName);
			ImageMagick.ImPath = tools;
			ImageMagick.MagickCommand = new RaiFile(script).NameWithExtension;

			var imageTreeRoot = root / "images";
			var subscriberRoot = imageTreeRoot / "Dr2RAI";
			var current = new ImageTreeFile(subscriberRoot, "Product1234", "Huge", "png");
			current.mkdir();
			new TextFile(current.FullName, "rendered");

			var overlayRoot = root / "overlays";
			overlayRoot.mkdir();
			new TextFile(new RaiFile(overlayRoot, "new", "png").FullName, "new");
			new TextFile(new RaiFile(overlayRoot, "bargain", "png").FullName, "bargain");

			var final = current.ApplyOverlays(new List<OverlaySetting>
			{
				new()
				{
					Name = "Bargain",
					Image = "bargain.png",
					Gravity = "SouthEast",
					Dissolve = "70",
					RenderOrder = 2,
					OverlayRoot = overlayRoot
				},
				new()
				{
					Name = "New",
					Image = "new.png",
					Gravity = "NorthWest",
					Dissolve = "90",
					Resize = "60x",
					RenderOrder = 1,
					OverlayRoot = overlayRoot
				}
			});

			Assert.True(final.Exists());
			Assert.Equal("HugeNewBargain", final.TemplateName);
			Assert.EndsWith("Product1234_HugeNewBargain.png", final.FullName);
		}
		finally
		{
			Cleanup(root);
		}
	}

	private static RaiPath NewTestRoot([CallerMemberName] string testName = "")
	{
		var root = new RaiPath(Path.GetTempPath()) / "RAIkeep" / "raiimage-tests" / "rendering" / testName;
		Cleanup(root);
		root.mkdir();
		return root;
	}

	private static void Cleanup(RaiPath root)
	{
		if (root?.Exists() == true)
			root.rmdir(depth: 5, deleteFiles: true);
	}

	private static void SeedFile(ImageTreeFile file, string content)
	{
		file.mkdir();
		new TextFile(file.FullName, content);
	}

	private static string CreateFakeMagickScript(RaiPath toolsDir, string logPath)
	{
		if (OperatingSystem.IsWindows())
		{
			return RaiSystem.CreateScript(toolsDir, "fake magick.cmd",
				$"@echo off\r\n" +
				"setlocal EnableExtensions EnableDelayedExpansion\r\n" +
				$">> \"{logPath}\" echo %*\r\n" +
				"set \"last=\"\r\n" +
				":loop\r\n" +
				"if \"%~1\"==\"\" goto after\r\n" +
				"set \"last=%~1\"\r\n" +
				"shift\r\n" +
				"goto loop\r\n" +
				":after\r\n" +
				"if not \"%last%\"==\"\" (\r\n" +
				"  for %%I in (\"%last%\") do if not exist \"%%~dpI\" mkdir \"%%~dpI\"\r\n" +
				"  > \"%last%\" echo generated\r\n" +
				")\r\n" +
				"exit /b 0\r\n").FullName;
		}

		return RaiSystem.CreateScript(toolsDir, "fake magick.sh",
			$"#!/bin/sh\n" +
			$"printf '%s\\n' \"$*\" >> \"{logPath}\"\n" +
			"last=\"\"\n" +
			"for arg in \"$@\"; do\n" +
			"  last=\"$arg\"\n" +
			"done\n" +
			"if [ -n \"$last\" ]; then\n" +
			"  mkdir -p \"$(dirname \"$last\")\"\n" +
			"  printf 'generated' > \"$last\"\n" +
			"fi\n" +
			"exit 0\n").FullName;
	}
}
