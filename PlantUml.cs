using System;
using OsLib;

namespace RaiImage
{
	public sealed class PlantUmlRenderResult
	{
		public ImageTreeFile Source { get; }
		public ImageTreeFile Svg { get; }

		public PlantUmlRenderResult(ImageTreeFile source, ImageTreeFile svg)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			Svg = svg ?? throw new ArgumentNullException(nameof(svg));
		}
	}

	public sealed class PlantUml
	{
		public static RaiPath PlantUmlPath = null;
		public static string CommandName = "plantuml";
		public static string JavaCommand = "java";
		private string message = string.Empty;

		public string Message => message;

		private static PlantUmlCommand CreateCommand()
			=> new PlantUmlCommand(PlantUmlPath, CommandName, JavaCommand);

		public PlantUml()
		{
			var command = CreateCommand();
			if (!command.IsAvailable())
				throw new InvalidOperationException(GetMissingExecutableMessage(command));
		}

		public RaiSystemResult RenderSvg(string pumlFileName)
		{
			var result = CreateCommand().RenderSvg(pumlFileName);
			message = result.Output;
			return result;
		}

		private static string GetMissingExecutableMessage(PlantUmlCommand command)
		{
			var install = command.GetInstallCommand();
			var configuredName = string.IsNullOrWhiteSpace(CommandName) ? "plantuml" : CommandName;
			return string.IsNullOrWhiteSpace(install)
				? $"PlantUML is required to render .puml files, but '{configuredName}' was not found."
				: $"PlantUML is required to render .puml files, but '{configuredName}' was not found. Install it first, for example with: {install}";
		}
	}
}
