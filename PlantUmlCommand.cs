using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage
{
	public sealed class PlantUmlCommand : CliCommand
	{
		private readonly RaiPath plantUmlPath;
		private readonly string commandName;
		private readonly string javaCommand;

		public PlantUmlCommand(RaiPath plantUmlPath = null, string commandName = null, string javaCommand = null)
			: base(commandName ?? PlantUml.CommandName, packageName: "plantuml")
		{
			this.plantUmlPath = plantUmlPath ?? PlantUml.PlantUmlPath;
			this.commandName = commandName ?? PlantUml.CommandName;
			this.javaCommand = string.IsNullOrWhiteSpace(javaCommand) ? PlantUml.JavaCommand : javaCommand;
		}

		public override IEnumerable<string> CandidateExecutables
		{
			get
			{
				if (plantUmlPath != null
					&& !string.IsNullOrWhiteSpace(plantUmlPath.Path)
					&& !string.IsNullOrWhiteSpace(commandName)
					&& !commandName.Contains('/') && !commandName.Contains('\\'))
				{
					var cmd = new RaiFile(commandName);
					cmd.Path = plantUmlPath;
					yield return cmd.FullName;
				}

				yield return commandName;
			}
		}

		public string BuildSvgArguments(string pumlFileName)
			=> $"-tsvg {Os.EscapeParam(pumlFileName)}";

		public string BuildSvgArguments(string pumlFileName, string configFileName)
		{
			if (string.IsNullOrWhiteSpace(configFileName))
				return BuildSvgArguments(pumlFileName);
			return $"-config {Os.EscapeParam(configFileName)} -tsvg {Os.EscapeParam(pumlFileName)}";
		}

		public RaiSystemResult RenderSvg(string pumlFileName)
			=> Run(BuildSvgArguments(pumlFileName));

		public RaiSystemResult RenderSvg(string pumlFileName, string configFileName)
			=> Run(BuildSvgArguments(pumlFileName, configFileName));

		public Task<RaiSystemResult> RenderSvgAsync(string pumlFileName, CancellationToken cancellationToken = default)
			=> RunAsync(BuildSvgArguments(pumlFileName), cancellationToken);

		public Task<RaiSystemResult> RenderSvgAsync(
			string pumlFileName,
			string configFileName,
			CancellationToken cancellationToken = default)
			=> RunAsync(BuildSvgArguments(pumlFileName, configFileName), cancellationToken);

		public override RaiSystemResult Run(string arguments = "")
			=> RunAsync(arguments).GetAwaiter().GetResult();

		public override Task<RaiSystemResult> RunAsync(string arguments = "", CancellationToken cancellationToken = default)
		{
			var executable = ResolveExecutable();
			if (!IsJar(executable))
				return base.RunAsync(arguments, cancellationToken);

			var javaExecutable = FindExecutableOnPath(javaCommand) ?? javaCommand;
			var javaArgs = $"-Djava.awt.headless=true -jar {Os.EscapeParam(executable)} {arguments}".Trim();
			return new RaiSystem(javaExecutable, javaArgs).ExecAsync(cancellationToken);
		}

		private static bool IsJar(string executable)
			=> !string.IsNullOrWhiteSpace(executable)
				&& executable.EndsWith(".jar", System.StringComparison.OrdinalIgnoreCase);
	}
}
