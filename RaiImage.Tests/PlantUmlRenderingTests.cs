using System.Runtime.CompilerServices;
using OsLib;

namespace RaiImage.Tests;

public class PlantUmlRenderingTests : IDisposable
{
	private readonly RaiPath plantUmlPath = PlantUml.PlantUmlPath;
	private readonly string commandName = PlantUml.CommandName;
	private readonly string javaCommand = PlantUml.JavaCommand;

	public void Dispose()
	{
		PlantUml.PlantUmlPath = plantUmlPath;
		PlantUml.CommandName = commandName;
		PlantUml.JavaCommand = javaCommand;
	}

	[Fact]
	public void RenderPlantUml_CreatesPumlAndSvgInsideSubscriberTree()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = new RaiFile(root, "plantuml", "log");
			var script = CreateFakePlantUmlScript(tools, log.FullName);
			PlantUml.PlantUmlPath = tools;
			PlantUml.CommandName = new RaiFile(script).NameWithExtension;

			var result = ImageTreeFile.RenderPlantUml(
				root / "images",
				"Dr2RAI",
				"AfricanPicnic_01",
				"@startuml\nAlice -> Bob : hello\n@enduml",
				ImageNamingConvention.Structured);

			Assert.True(result.Source.Exists());
			Assert.True(result.Svg.Exists());
			Assert.EndsWith("AfricanPicnic_01.puml", result.Source.FullName);
			Assert.EndsWith("AfricanPicnic_01.svg", result.Svg.FullName);
			Assert.Contains("@startuml", new TextFile(result.Source.FullName).ReadAllText());
			Assert.Contains("-tsvg", string.Join("\n", new TextFile(log.FullName).Lines));
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void RenderPlantUml_WithConfigPersistsSidecarAndPassesConfigArgument()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = new RaiFile(root, "plantuml-config", "log");
			var script = CreateFakePlantUmlScript(tools, log.FullName);
			PlantUml.PlantUmlPath = tools;
			PlantUml.CommandName = new RaiFile(script).NameWithExtension;

			var result = ImageTreeFile.RenderPlantUml(
				root / "images",
				"Dr2RAI",
				"AfricanPicnic_01",
				"@startuml\nAlice -> Bob : hello\n@enduml",
				"!theme cerulean",
				ImageNamingConvention.Structured);

			Assert.True(result.Config.Exists());
			Assert.EndsWith("AfricanPicnic_01_config.puml", result.Config.FullName);
			Assert.Equal("config", result.ConfigArtifact.NameExt);
			Assert.Equal("puml", result.ConfigArtifact.Ext);
			Assert.Equal("!theme cerulean", new TextFile(result.Config.FullName).ReadAllText().Trim());
			var invocation = string.Join("\n", new TextFile(log.FullName).Lines);
			Assert.Contains("-config", invocation);
			Assert.Contains(result.Config.FullName, invocation);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public async Task PlantUmlCommand_RunAsync_UsesJavaForJarCommands()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			var log = new RaiFile(root, "java", "log");
			var javaScript = CreateFakeJavaScript(tools, log.FullName);
			_ = new TextFile(new RaiFile(tools, "plantuml", "jar").FullName, "fake-jar");
			var sut = new PlantUmlCommand(tools, "plantuml.jar", javaScript);

			var result = await sut.RenderSvgAsync(
				"diagram.puml",
				"diagram_config.puml",
				TestContext.Current.CancellationToken);

			Assert.Equal(0, result.ExitCode);
			var lines = new TextFile(log.FullName).Lines;
			Assert.Equal("-Djava.awt.headless=true", lines[0]);
			Assert.Equal("-jar", lines[1]);
			Assert.EndsWith("plantuml.jar", lines[2]);
			Assert.Equal("-config", lines[3]);
			Assert.Equal("diagram_config.puml", lines[4]);
			Assert.Equal("-tsvg", lines[5]);
			Assert.Equal("diagram.puml", lines[6]);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void RenderPlantUml_FailsFast_WhenPlantUmlIsMissing()
	{
		var root = NewTestRoot();
		try
		{
			var tools = root / "tools";
			tools.mkdir();
			PlantUml.PlantUmlPath = tools;
			PlantUml.CommandName = "definitely-missing-plantuml";

			var error = Assert.Throws<RaiUtils.ToolNotFoundException>(() =>
				ImageTreeFile.RenderPlantUml(
					root / "images",
					"Dr2RAI",
					"AfricanPicnic_01",
					"@startuml\nAlice -> Bob : hello\n@enduml",
					ImageNamingConvention.Structured));

			Assert.Equal("PlantUML", error.ToolName);
			Assert.Contains("definitely-missing-plantuml", error.ExecutablePath);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void CreateSiblingWithNameExt_PreservesSubscriberPlacementAndRejectsPathInjection()
	{
		var source = ImageTreeFile.FromName(new RaiPath("/tmp/images/AfricaStage/Tenant-A"), "ScheduleRehearsal");

		var config = source.CreateSiblingWithNameExt("config", "puml");

		Assert.Equal(source.SubdirRoot.FullPath, config.SubdirRoot.FullPath);
		Assert.Equal("config", config.NameExt);
		Assert.Equal("puml", config.Ext);
		Assert.EndsWith("ScheduleRehearsal_config.puml", config.FullName);
		Assert.Throws<ArgumentException>(() => source.CreateSiblingWithExtension("../outside"));
		Assert.Throws<ArgumentException>(() => source.CreateSiblingWithNameExt("../outside", "puml"));
	}

	private static RaiPath NewTestRoot([CallerMemberName] string testName = "")
	{
		var root = Os.TempDir / "RAIkeep" / "raiimage-tests" / "plantuml" / testName;
		Cleanup(root);
		root.mkdir();
		return root;
	}

	private static void Cleanup(RaiPath root)
	{
		if (root?.Exists() == true)
			root.rmdir(depth: 5, deleteFiles: true);
	}

	private static string CreateFakePlantUmlScript(RaiPath toolsDir, string logPath)
	{
		if (OperatingSystem.IsWindows())
		{
			return RaiSystem.CreateScript(toolsDir, "fake plantuml.cmd",
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
				"  set \"svg=%last:.puml=.svg%\"\r\n" +
				"  for %%I in (\"%svg%\") do if not exist \"%%~dpI\" mkdir \"%%~dpI\"\r\n" +
				"  > \"%svg%\" echo generated\r\n" +
				")\r\n" +
				"exit /b 0\r\n").FullName;
		}

		return RaiSystem.CreateScript(toolsDir, "fake plantuml.sh",
			$"#!/bin/sh\n" +
			$"printf '%s\\n' \"$*\" >> \"{logPath}\"\n" +
			"last=\"\"\n" +
			"for arg in \"$@\"; do\n" +
			"  last=\"$arg\"\n" +
			"done\n" +
			"svg=\"${last%.puml}.svg\"\n" +
			"mkdir -p \"$(dirname \"$svg\")\"\n" +
			"printf 'generated' > \"$svg\"\n" +
			"exit 0\n").FullName;
	}

	private static string CreateFakeJavaScript(RaiPath toolsDir, string logPath)
	{
		if (OperatingSystem.IsWindows())
		{
			return RaiSystem.CreateScript(toolsDir, "fake java.cmd",
				$"@echo off\r\n" +
				$"> \"{logPath}\" echo %1\r\n" +
				$">> \"{logPath}\" echo %2\r\n" +
				$">> \"{logPath}\" echo %3\r\n" +
				$">> \"{logPath}\" echo %4\r\n" +
				$">> \"{logPath}\" echo %5\r\n" +
				$">> \"{logPath}\" echo %6\r\n" +
				$">> \"{logPath}\" echo %7\r\n" +
				"exit /b 0\r\n").FullName;
		}

		return RaiSystem.CreateScript(toolsDir, "fake java.sh",
			$"#!/bin/sh\n" +
			$"printf '%s\\n' \"$1\" > \"{logPath}\"\n" +
			$"printf '%s\\n' \"$2\" >> \"{logPath}\"\n" +
			$"printf '%s\\n' \"$3\" >> \"{logPath}\"\n" +
			$"printf '%s\\n' \"$4\" >> \"{logPath}\"\n" +
			$"printf '%s\\n' \"$5\" >> \"{logPath}\"\n" +
			$"printf '%s\\n' \"$6\" >> \"{logPath}\"\n" +
			$"printf '%s\\n' \"$7\" >> \"{logPath}\"\n" +
			"exit 0\n").FullName;
	}
}
