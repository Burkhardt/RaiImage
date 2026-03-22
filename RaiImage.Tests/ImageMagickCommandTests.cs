using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OsLib;
using Xunit;

namespace RaiImage.Tests
{
	public class ImageMagickCommandTests
	{
		private static string CreateTempRoot([CallerMemberName] string testName = "")
		{
			var root = Os.TempDir / "RAIkeep" / "raiimage-tests" / "command" / SanitizeSegment(testName);
			Cleanup(root.Path);
			root.mkdir();
			return root.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		private static void Cleanup(string root)
		{
			try
			{
				if (Directory.Exists(root))
					Directory.Delete(root, recursive: true);
			}
			catch
			{
			}
		}

		private static string CreateExecutableScript(string root, string scriptName, string content)
		{
			return RaiSystem.CreateScript(new RaiPath(root), scriptName, content).FullName;
		}

		private static string SanitizeSegment(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "test";

			var invalid = Path.GetInvalidFileNameChars();
			var cleaned = new string(value
				.Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '-' : ch)
				.ToArray())
				.Trim('-');

			return string.IsNullOrWhiteSpace(cleaned) ? "test" : cleaned;
		}

		[Fact]
		public void ImageMagickCommand_UsesExecutableInsideConfiguredImPath()
		{
			var root = CreateTempRoot();
			try
			{
				var script = OperatingSystem.IsWindows()
					? CreateExecutableScript(root, "magick.cmd", "@echo off\r\n")
					: CreateExecutableScript(root, "magick", "#!/bin/sh\nexit 0\n");
				var sut = new ImageMagickCommand(root + Path.DirectorySeparatorChar, Path.GetFileName(script));

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
			var root = CreateTempRoot();
			try
			{
				var log = new RaiFile("magick.log") { Path = root }.FullName;
				var script = OperatingSystem.IsWindows()
					? CreateExecutableScript(root, "magick.cmd",
						$"@echo off\r\n> \"{log}\" echo %1\r\n>> \"{log}\" echo %2\r\necho ok\r\n")
					: CreateExecutableScript(root, "magick",
						$"#!/bin/sh\nprintf '%s\\n' \"$1\" > \"{log}\"\nprintf '%s\\n' \"$2\" >> \"{log}\"\nprintf 'ok'\n");
				var sut = new ImageMagickCommand(root + Path.DirectorySeparatorChar, Path.GetFileName(script));
				var callingThreadId = Environment.CurrentManagedThreadId;

				var result = await sut.RunSubcommandAsync("identify", "sample.png", TestContext.Current.CancellationToken);

				Assert.Equal(0, result.ExitCode);
				Assert.Contains("ok", result.Output);
				Assert.NotEqual(callingThreadId, result.WorkerThreadId);
				var lines = File.ReadAllLines(log);
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