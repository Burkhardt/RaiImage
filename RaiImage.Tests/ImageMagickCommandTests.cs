using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OsLib;
using Xunit;

namespace RaiImage.Tests
{
	public class ImageMagickCommandTests
	{
		private static RaiPath CreateTempRootDir([CallerMemberName] string testName = "")
		{
			var root = new RaiPath(Path.GetTempPath()) / "RAIkeep" / "raiimage-tests" / "command" / SanitizeSegment(testName);
			Cleanup(root);
			root.mkdir();
			//return root.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return root;
		}

		private static void Cleanup(RaiPath root)
		{
			try
			{
				if (root.Exists())
					root.rmdir(depth: 3, deleteFiles: true);
			}
			catch
			{
			}
		}

		private static string CreateExecutableScript(RaiPath rootDir, string scriptName, string content = "", string ext = "sh")
		{
			return RaiSystem.CreateScript(rootDir, scriptName, ext: ext, content: content).FullName;
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

		[Fact]
		public void ImageMagickCommand_UsesExecutableInsideConfiguredImPath()
		{
			var root = CreateTempRootDir();
			try
			{
				var script = OperatingSystem.IsWindows()
					? CreateExecutableScript(root, "magick", ext: "cmd", content: "@echo off\r\n")
					: CreateExecutableScript(root, "magick", ext: "sh", content: "#!/bin/sh\nexit 0\n");
				var sut = new ImageMagickCommand(root, new RaiFile(script).NameWithExtension);

				Assert.True(sut.TryResolveExecutable(out var resolved));
				Assert.Equal(script, resolved);
			}
			finally
			{
				Cleanup(root);
			}
		}

		[Fact]
		public async Task ImageMagickCommand_RunSubcommandAsync_PrefixesSubcommand_AndUsesWorkerThread()
		{
			var root = CreateTempRootDir();
			try
			{
				var log = new RaiFile("magick.log") { Path = root }.FullName;
				var script = OperatingSystem.IsWindows()
					? CreateExecutableScript(root, "magick.cmd",
						$"@echo off\r\n> \"{log}\" echo %1\r\n>> \"{log}\" echo %2\r\necho ok\r\n")
					: CreateExecutableScript(root, "magick",
						$"#!/bin/sh\nprintf '%s\\n' \"$1\" > \"{log}\"\nprintf '%s\\n' \"$2\" >> \"{log}\"\nprintf 'ok'\n");
				var sut = new ImageMagickCommand(root, new RaiFile(script).NameWithExtension);
				var callingThreadId = Environment.CurrentManagedThreadId;

				var result = await sut.RunSubcommandAsync("identify", "sample.png", TestContext.Current.CancellationToken);

				Assert.Equal(0, result.ExitCode);
				Assert.Contains("ok", result.Output);
				Assert.NotEqual(callingThreadId, result.WorkerThreadId);
				var lines = new TextFile(log).Lines;
				Assert.Equal("identify", lines[0]);
				Assert.Equal("sample.png", lines[1]);
			}
			finally
			{
				Cleanup(root);
			}
		}

		[Fact]
		public void ImageMagickCommand_ProvidesInstallAndUpdateCommands()
		{
			var sut = new ImageMagickCommand();

			Assert.False(string.IsNullOrWhiteSpace(sut.GetInstallCommand()));
			Assert.False(string.IsNullOrWhiteSpace(sut.GetUpdateCommand()));
		}
	}
}
