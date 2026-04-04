using OsLib;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace RaiImage.Tests;

[Collection("ImageMagick static state")]
public class ImageMagickTests
{
	private const string NomsaRed = "#d32f2f";

	private static RaiPath NewTestRoot([CallerMemberName] string testName = "")
	{
		var root = new RaiPath(Path.GetTempPath()) / "RAIkeep" / "raiimage-tests" / "imagemagick" / SanitizeSegment(testName);
		Cleanup(root);
		return root;
	}

	private static string FilePath(RaiPath root, string fileName)
	{
		return new RaiFile(fileName) { Path = root }.FullName;
	}

	private sealed class ImageMagickStateScope : IDisposable
	{
		private static readonly FieldInfo? OsTempDirField = typeof(Os).GetField("tempDir", BindingFlags.Static | BindingFlags.NonPublic);
		private readonly object? _tempDir = OsTempDirField?.GetValue(null);
		private readonly RaiPath _imPath = ImageMagick.ImPath;
		private readonly string _magickCommand = ImageMagick.MagickCommand;
		private readonly string _optiPngCommand = ImageMagick.OptiPngCommand;
		private readonly string _jpegTranCommand = ImageMagick.JpegTranCommand;
		private readonly string _jpegTranOptions = ImageMagick.JpegTranOptions;

		public ImageMagickStateScope()
		{
			OsTempDirField?.SetValue(null, new RaiPath(Path.GetTempPath()));
		}

		public void Dispose()
		{
			OsTempDirField?.SetValue(null, _tempDir);
			ImageMagick.ImPath = _imPath;
			ImageMagick.MagickCommand = _magickCommand;
			ImageMagick.OptiPngCommand = _optiPngCommand;
			ImageMagick.JpegTranCommand = _jpegTranCommand;
			ImageMagick.JpegTranOptions = _jpegTranOptions;
		}
	}

	private static RaiPath CreateTempRoot()
	{
		var root = NewTestRoot();
		root.mkdir();
		return root;
	}

	private static void Cleanup(RaiPath root)
	{
		if (root?.Exists() == true)
			root.rmdir(depth: 3, deleteFiles: true);
	}

	private static string SanitizeSegment(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return "test";

		var invalid = RaiPath.InvalidFileNameChars;
		var cleaned = new string(value
			.Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '-' : ch)
			.ToArray())
			.Trim('-');

		return string.IsNullOrWhiteSpace(cleaned) ? "test" : cleaned;
	}

	private static string CreateExecutableScript(RaiPath root, string scriptName, string content)
	{
		return RaiSystem.CreateScript(root, scriptName, content).FullName;
	}

	private static string CreateFakeMagickScript(RaiPath toolsDir, string logPath)
	{
		if (OperatingSystem.IsWindows())
		{
			return CreateExecutableScript(toolsDir, "fake magick.cmd",
				$"@echo off\r\n" +
				"setlocal EnableExtensions EnableDelayedExpansion\r\n" +
				$"> \"{logPath}\" echo %~1\r\n" +
				"set \"sub=%~1\"\r\n" +
				"shift\r\n" +
				"set \"last=\"\r\n" +
				":loop\r\n" +
				"if \"%~1\"==\"\" goto after\r\n" +
				$">> \"{logPath}\" echo %~1\r\n" +
				"set \"last=%~1\"\r\n" +
				"shift\r\n" +
				"goto loop\r\n" +
				":after\r\n" +
				"if /I \"%sub%\"==\"identify\" (\r\n" +
				"  <nul set /p =123 456\r\n" +
				"  exit /b 0\r\n" +
				")\r\n" +
				"if /I \"%sub%\"==\"convert\" if not \"%last%\"==\"\" > \"%last%\" echo generated\r\n" +
				"if /I \"%sub%\"==\"composite\" if not \"%last%\"==\"\" > \"%last%\" echo generated\r\n" +
				"exit /b 0\r\n");
		}

		return CreateExecutableScript(toolsDir, "fake magick.sh",
			$"#!/bin/sh\n" +
			$": > \"{logPath}\"\n" +
			"sub=\"$1\"\n" +
			$"printf '%s\\n' \"$sub\" >> \"{logPath}\"\n" +
			"shift\n" +
			"last=\"\"\n" +
			"for arg in \"$@\"; do\n" +
			$"  printf '%s\\n' \"$arg\" >> \"{logPath}\"\n" +
			"  last=\"$arg\"\n" +
			"done\n" +
			"if [ \"$sub\" = \"identify\" ]; then\n" +
			"  printf '123 456'\n" +
			"  exit 0\n" +
			"fi\n" +
			"if [ \"$sub\" = \"convert\" ] || [ \"$sub\" = \"composite\" ]; then\n" +
			"  if [ -n \"$last\" ]; then\n" +
			"    mkdir -p \"$(dirname \"$last\")\"\n" +
			"    printf 'generated' > \"$last\"\n" +
			"  fi\n" +
			"fi\n" +
			"exit 0\n");
	}

	private static string CreateFakeCopyScript(RaiPath toolsDir, string scriptName, string logPath)
	{
		if (OperatingSystem.IsWindows())
		{
			return CreateExecutableScript(toolsDir, scriptName,
				$"@echo off\r\n" +
				"setlocal EnableExtensions\r\n" +
				$"> \"{logPath}\" echo %*\r\n" +
				"copy /Y %3 %4 >nul\r\n" +
				"exit /b 0\r\n");
		}

		return CreateExecutableScript(toolsDir, scriptName,
			$"#!/bin/sh\n" +
			$"printf '%s\\n' \"$*\" > \"{logPath}\"\n" +
			"cp \"$3\" \"$4\"\n" +
			"exit 0\n");
	}

	private static string CreateFakeArgumentLogger(RaiPath toolsDir, string scriptName, string logPath)
	{
		if (OperatingSystem.IsWindows())
		{
			return CreateExecutableScript(toolsDir, scriptName,
				$"@echo off\r\n" +
				$"> \"{logPath}\" echo %1\r\n" +
				"exit /b 0\r\n");
		}

		return CreateExecutableScript(toolsDir, scriptName,
			$"#!/bin/sh\n" +
			$"printf '%s\\n' \"$1\" > \"{logPath}\"\n" +
			"exit 0\n");
	}

	private static string CreateFailingScript(RaiPath toolsDir, string scriptName, string stderrText, int exitCode, string? logPath = null)
	{
		if (OperatingSystem.IsWindows())
		{
			var logLine = string.IsNullOrWhiteSpace(logPath) ? string.Empty : $"> \"{logPath}\" echo %*\r\n";
			return CreateExecutableScript(toolsDir, scriptName,
				$"@echo off\r\n" +
				"setlocal EnableExtensions\r\n" +
				logLine +
				$">&2 echo {stderrText}\r\n" +
				$"exit /b {exitCode}\r\n");
		}

		var unixLog = string.IsNullOrWhiteSpace(logPath) ? string.Empty : $"printf '%s\\n' \"$*\" > \"{logPath}\"\n";
		return CreateExecutableScript(toolsDir, scriptName,
			$"#!/bin/sh\n" +
			unixLog +
			$"printf '%s\\n' '{stderrText}' >&2\n" +
			$"exit {exitCode}\n");
	}

	private static bool TryGetInstalledMagick(out string executable, out string versionInfo)
	{
		executable = ImageMagick.MagickCommand;
		versionInfo = string.Empty;
		try
		{
			var rs = new RaiSystem(executable, "-version");
			if (rs.Exec(out versionInfo) == 0 && !string.IsNullOrWhiteSpace(versionInfo))
				return true;
		}
		catch (Exception ex)
		{
			versionInfo = ex.Message;
		}

		return false;
	}

	private static bool SupportsWebp(string executable)
	{
		try
		{
			var rs = new RaiSystem(executable, "-list format");
			return rs.Exec(out var formats) == 0 && formats.Contains("WEBP", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static string IdentifyPixel(ImageMagick sut, string image, int x, int y)
	{
		string result = string.Empty;
		var exitCode = sut.Identify($"-format %[pixel:p{{{x},{y}}}]", image, ref result);
		Assert.Equal(0, exitCode);
		return result.Trim();
	}

	private static void AssertLooksLikeSolidColor(string pixel, params string[] expectedFragments)
	{
		Assert.Contains(expectedFragments, fragment => pixel.Contains(fragment, StringComparison.OrdinalIgnoreCase));
	}

	private static void AssertLooksTransparent(string pixel)
	{
		var normalized = pixel.Replace(" ", string.Empty, StringComparison.Ordinal);
		Assert.True(
			normalized.Contains("none", StringComparison.OrdinalIgnoreCase) ||
			normalized.Contains(",0)", StringComparison.OrdinalIgnoreCase) ||
			normalized.Contains(",0%", StringComparison.OrdinalIgnoreCase),
			$"Expected a transparent pixel but got '{pixel}'.");
	}

	[Fact]
	public void Constructor_DoesNotRequireFixedInstallPath_WhenImPathEmpty()
	{
		using var scope = new ImageMagickStateScope();
		ImageMagick.ImPath = null;
		var sut = new ImageMagick();
		Assert.NotNull(sut);
	}

	[Fact]
	public void Constructor_Throws_WhenImPathSetButMagickNotFound()
	{
		using var scope = new ImageMagickStateScope();
		ImageMagick.ImPath = new RaiPath("/tmp/raimage-missing-magick");
		Assert.Throws<FileNotFoundException>(() => new ImageMagick());
	}

	[Fact]
	public void Constructor_UsesExecutableInsideConfiguredImPath()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools with spaces";
			tools.mkdir();
			var log = FilePath(root, "magick.log");
			var scriptPath = CreateFakeMagickScript(tools, log);

			using var scope = new ImageMagickStateScope();
			ImageMagick.ImPath = tools;
			ImageMagick.MagickCommand = new RaiFile(scriptPath).NameWithExtension;

			var sut = new ImageMagick();
			Assert.NotNull(sut);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void Convert_ExecutesMagickWithStructuredArguments_AndPreservesPathsWithSpaces()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools with spaces";
			tools.mkdir();
			var log = FilePath(root, "magick.log");
			var scriptPath = CreateFakeMagickScript(tools, log);
			var from = FilePath(root, "from file.png");
			var to = FilePath(root, "to file.png");
			new TextFile(from, "source");

			using var scope = new ImageMagickStateScope();
			ImageMagick.ImPath = tools;
			ImageMagick.MagickCommand = new RaiFile(scriptPath).NameWithExtension;

			var sut = new ImageMagick();
			var exitCode = sut.Convert("-resize 10x10", from, to);

			Assert.Equal(0, exitCode);
			Assert.True(new RaiFile(to).Exists());
			var lines = new TextFile(log).Lines;
			Assert.Equal("convert", lines[0]);
			Assert.Contains("-resize", lines);
			Assert.Contains("10x10", lines);
			Assert.Contains(from, lines);
			Assert.Contains(to, lines);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void Identify_ReturnsStdout_FromConfiguredExecutable()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools with spaces";
			tools.mkdir();
			var log = FilePath(root, "magick.log");
			var scriptPath = CreateFakeMagickScript(tools, log);
			var image = FilePath(root, "source image.png");
			new TextFile(image, "source");
			string result = string.Empty;

			using var scope = new ImageMagickStateScope();
			ImageMagick.ImPath = tools;
			ImageMagick.MagickCommand = new RaiFile(scriptPath).NameWithExtension;

			var sut = new ImageMagick();
			var exitCode = sut.Identify("-ping -format \"%[fx:w] %[fx:h]\"", image, ref result);

			Assert.Equal(0, exitCode);
			Assert.Equal("123 456", result);
			var lines = new TextFile(log).Lines;
			Assert.Equal("identify", lines[0]);
			Assert.Contains(image, lines);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void OptiPng_ExecutesConfiguredOptimizer_ForPngFile()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools with spaces";
			tools.mkdir();
			var log = FilePath(root, "optipng.log");
			var scriptPath = CreateFakeArgumentLogger(tools, OperatingSystem.IsWindows() ? "fake optipng.cmd" : "fake optipng.sh", log);
			var image = new ImageFile(FilePath(root, "image file.png")).FullName;
			new TextFile(image, "pngdata");

			using var scope = new ImageMagickStateScope();
			ImageMagick.OptiPngCommand = scriptPath;

			var sut = new ImageMagick();
			var exitCode = sut.OptiPng(image);

			Assert.Equal(0, exitCode);
			Assert.Equal(image, new TextFile(log).ReadAllText().Trim());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void JpegTran_ExecutesConfiguredTool_AndRestoresOutputFile()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools with spaces";
			tools.mkdir();
			var log = FilePath(root, "jpegtran.log");
			var scriptPath = CreateFakeCopyScript(tools, OperatingSystem.IsWindows() ? "fake jpegtran.cmd" : "fake jpegtran.sh", log);
			var image = new ImageFile(FilePath(root, "image file.jpg")).FullName;
			new TextFile(image, "jpgdata");

			using var scope = new ImageMagickStateScope();
			ImageMagick.JpegTranCommand = scriptPath;

			var sut = new ImageMagick();
			var exitCode = sut.JpegTran(image);

			Assert.Equal(0, exitCode);
			Assert.True(new RaiFile(image).Exists());
			Assert.Equal("jpgdata", new TextFile(image).ReadAllText().Trim());
			Assert.Contains(new RaiFile(image).NameWithExtension, new TextFile(log).ReadAllText());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void Convert_ReturnsNonZero_AndCapturesStdErr_WhenMagickFails()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var scriptPath = CreateFailingScript(tools, OperatingSystem.IsWindows() ? "fake magick.cmd" : "fake magick.sh", "convert failed on purpose", 17);
			var from = FilePath(root, "source.png");
			var to = FilePath(root, "dest.png");
			new TextFile(from, "src");

			using var scope = new ImageMagickStateScope();
			ImageMagick.MagickCommand = scriptPath;
			ImageMagick.ImPath = null;

			var sut = new ImageMagick();
			var exitCode = sut.Convert("-resize 10x10", from, to);

			Assert.Equal(17, exitCode);
			Assert.Contains("convert failed on purpose", sut.Message, StringComparison.OrdinalIgnoreCase);
			Assert.False(new RaiFile(to).Exists());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void OptiPng_WhenIntermediateConversionFails_PreservesOriginalFile_AndSkipsOptimizer()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var optimizerLog = FilePath(root, "optipng.log");
			var magickPath = CreateFailingScript(tools, OperatingSystem.IsWindows() ? "fake magick.cmd" : "fake magick.sh", "mogrify failed on purpose", 8);
			var optimizerPath = CreateFakeArgumentLogger(tools, OperatingSystem.IsWindows() ? "fake optipng.cmd" : "fake optipng.sh", optimizerLog);
			var original = FilePath(root, "profile source.jpg");
			new TextFile(original, "jpgdata");

			using var scope = new ImageMagickStateScope();
			ImageMagick.MagickCommand = magickPath;
			ImageMagick.ImPath = null;
			ImageMagick.OptiPngCommand = optimizerPath;

			var sut = new ImageMagick();
			var exitCode = sut.OptiPng(original);

			Assert.Equal(8, exitCode);
			Assert.Contains("mogrify failed on purpose", sut.Message, StringComparison.OrdinalIgnoreCase);
			Assert.True(new RaiFile(original).Exists());
			Assert.Equal("jpgdata", new TextFile(original).ReadAllText().Trim());
			Assert.False(new RaiFile(optimizerLog).Exists());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void JpegTran_WhenToolFails_RestoresOriginalFile_AndCapturesStdErr()
	{
		var root = CreateTempRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = FilePath(root, "jpegtran.log");
			var scriptPath = CreateFailingScript(tools, OperatingSystem.IsWindows() ? "fake jpegtran.cmd" : "fake jpegtran.sh", "jpegtran failed on purpose", 9, log);
			var image = new ImageFile(FilePath(root, "image file.jpg")).FullName;
			new TextFile(image, "jpgdata");

			using var scope = new ImageMagickStateScope();
			ImageMagick.JpegTranCommand = scriptPath;

			var sut = new ImageMagick();
			var exitCode = sut.JpegTran(image);

			Assert.Equal(9, exitCode);
			Assert.Contains("jpegtran failed on purpose", sut.Message, StringComparison.OrdinalIgnoreCase);
			Assert.True(new RaiFile(image).Exists());
			Assert.Equal("jpgdata", new TextFile(image).ReadAllText().Trim());
			Assert.Contains(new RaiFile(image).NameWithExtension, new TextFile(log).ReadAllText());
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void BackgroundRemoval_CreatesTransparentPngAndWebp_ThatCompositeOntoNomsaRedBlackAndWhite()
	{
		using var scope = new ImageMagickStateScope();
		ImageMagick.ImPath = null;

		if (!TryGetInstalledMagick(out var magickExecutable, out var versionInfo))
		{
			Console.WriteLine($"ImageMagick not available. Skipping transparency integration test. Detail: {versionInfo}");
			return;
		}

		if (!SupportsWebp(magickExecutable))
		{
			Console.WriteLine($"ImageMagick is available but WEBP support was not detected. Version: {versionInfo.Split(Environment.NewLine)[0]}");
			return;
		}

		ImageMagick.MagickCommand = magickExecutable;

		var root = CreateTempRoot();
		try
		{
			var svg = FilePath(root, "portrait.svg");
			var source = FilePath(root, "portrait-source.png");
			var transparentPng = FilePath(root, "portrait-transparent.png");
			var transparentWebp = FilePath(root, "portrait-transparent.webp");

			new TextFile(svg).Append(
				"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"240\" height=\"320\" viewBox=\"0 0 240 320\">" +
				"<rect width=\"240\" height=\"320\" fill=\"#18d86b\"/>" +
				"<ellipse cx=\"120\" cy=\"104\" rx=\"44\" ry=\"54\" fill=\"#f1c7a6\"/>" +
				"<path d=\"M76 114 C84 50,156 50,164 114 L164 136 C152 120,88 120,76 136 Z\" fill=\"#24160f\"/>" +
				"<rect x=\"78\" y=\"156\" width=\"84\" height=\"118\" rx=\"24\" fill=\"#202938\"/>" +
				"<rect x=\"104\" y=\"148\" width=\"32\" height=\"24\" rx=\"8\" fill=\"#f1c7a6\"/>" +
				"</svg>").Save();

			var sut = new ImageMagick();
			Assert.Equal(0, sut.Convert(string.Empty, svg, source));
			Assert.Equal(0, sut.Convert("-alpha set -fuzz 5% -transparent #18d86b", source, transparentPng));
			Assert.Equal(0, sut.Convert(string.Empty, transparentPng, transparentWebp));
			Assert.True(new RaiFile(transparentPng).Exists());
			Assert.True(new RaiFile(transparentWebp).Exists());
			AssertLooksTransparent(IdentifyPixel(sut, transparentPng, 0, 0));
			AssertLooksTransparent(IdentifyPixel(sut, transparentWebp, 0, 0));

			var backgrounds = new[]
			{
				(Name: "nomsa-red", Color: NomsaRed, Expected: new[] { "211,47,47", "#D32F2F" }),
				(Name: "black", Color: "#000000", Expected: new[] { "0,0,0", "gray(0)", "#000000" }),
				(Name: "white", Color: "#ffffff", Expected: new[] { "255,255,255", "gray(255)", "#FFFFFF" })
			};

			foreach (var background in backgrounds)
			{
				var backgroundFile = FilePath(root, $"{background.Name}.png");
				var compositePng = FilePath(root, $"{background.Name}-png.png");
				var compositeWebp = FilePath(root, $"{background.Name}-webp.png");

				Assert.Equal(0, sut.Convert($"-size 240x320 xc:{background.Color} {Os.EscapeParam(backgroundFile)}"));
				Assert.Equal(0, sut.Composite("-gravity center", transparentPng, backgroundFile, compositePng));
				Assert.Equal(0, sut.Composite("-gravity center", transparentWebp, backgroundFile, compositeWebp));

				var cornerFromPng = IdentifyPixel(sut, compositePng, 0, 0);
				var cornerFromWebp = IdentifyPixel(sut, compositeWebp, 0, 0);
				var centerFromPng = IdentifyPixel(sut, compositePng, 120, 110);
				var centerFromWebp = IdentifyPixel(sut, compositeWebp, 120, 110);

				AssertLooksLikeSolidColor(cornerFromPng, background.Expected);
				AssertLooksLikeSolidColor(cornerFromWebp, background.Expected);
				Assert.DoesNotContain(background.Expected, expected => centerFromPng.Contains(expected, StringComparison.OrdinalIgnoreCase));
				Assert.DoesNotContain(background.Expected, expected => centerFromWebp.Contains(expected, StringComparison.OrdinalIgnoreCase));
			}
		}
		finally
		{
			Cleanup(root);
		}
	}
}
