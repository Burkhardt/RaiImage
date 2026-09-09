using System;
using System.Linq;
using OsLib;
using RaiUtils;

namespace RaiImage
{
	public sealed class PlantUmlRenderResult
	{
		/// <summary>Compatibility image-shaped handle retained from RaiImage 4.2.0.</summary>
		public ImageTreeFile Source { get; }
		/// <summary>Compatibility image-shaped handle retained from RaiImage 4.2.0.</summary>
		public ImageTreeFile Config { get; }
		public ImageTreeFile Svg { get; }
		public ItemTreeTextFile SourceArtifact { get; }
		public ItemTreeTextFile ConfigArtifact { get; }

		public PlantUmlRenderResult(ImageTreeFile source, ImageTreeFile svg)
			: this(source, null, svg)
		{
		}

		public PlantUmlRenderResult(ImageTreeFile source, ImageTreeFile config, ImageTreeFile svg)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			Config = config;
			Svg = svg ?? throw new ArgumentNullException(nameof(svg));
		}

		public PlantUmlRenderResult(
			ItemTreeTextFile source,
			ItemTreeTextFile config,
			ImageTreeFile svg)
		{
			SourceArtifact = source ?? throw new ArgumentNullException(nameof(source));
			ConfigArtifact = config;
			Svg = svg ?? throw new ArgumentNullException(nameof(svg));
			if (source.ItemPath is null)
				throw new ArgumentException("PlantUML source requires subscriber ItemTree placement.", nameof(source));
			Source = new ImageTreeFile(
				source.ItemPath,
				source.NameExt,
				source.Ext);
			Config = config is null
				? null
				: new ImageTreeFile(
					config.ItemPath,
					config.NameExt,
					config.Ext);
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
				throw new ToolNotFoundException(
					"PlantUML", command.CandidateExecutables.FirstOrDefault() ?? CommandName);
		}

		public RaiSystemResult RenderSvg(string pumlFileName)
		{
			var result = CreateCommand().RenderSvg(pumlFileName);
			message = result.Output;
			return result;
		}

		public RaiSystemResult RenderSvg(string pumlFileName, string configFileName)
		{
			var result = CreateCommand().RenderSvg(pumlFileName, configFileName);
			message = result.Output;
			return result;
		}

	}
}
