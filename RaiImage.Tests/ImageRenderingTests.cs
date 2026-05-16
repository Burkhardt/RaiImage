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

			var template = new TemplateSetting("Huge", "300x200", false, "webp", 82, true);
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
				new("Bargain", "bargain.png", "SouthEast", 70, renderOrder: 2, overlayRoot: overlayRoot),
				new("New", "new.png", "NorthWest", 90, width: 60, renderOrder: 1, overlayRoot: overlayRoot)
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
