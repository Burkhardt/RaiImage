using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage
{
	public class ImageMagickCommand : CliCommand
	{
		private readonly RaiPath imPath;
		private readonly string commandName;

		public ImageMagickCommand(RaiPath imPath = null, string commandName = null)
			: base(commandName ?? ImageMagick.MagickCommand, packageName: "imagemagick")
		{
			this.imPath = imPath ?? ImageMagick.ImPath;
			this.commandName = commandName ?? ImageMagick.MagickCommand;
		}

		public override IEnumerable<string> CandidateExecutables
		{
			get
			{
				if (imPath == null || string.IsNullOrWhiteSpace(imPath.Path))
					yield return commandName; // no path is fine as long as the command is in the system PATH
				var cmd = new RaiFile(commandName);
				cmd.Path = imPath;
				yield return cmd.FullName;
			}
		}

		protected override string WindowsPackageId => "ImageMagick.ImageMagick";

		public string BuildArguments(string subcommand, string args = "")
		{
			return $"{subcommand} {args}".Trim();
		}

		public RaiSystemResult RunSubcommand(string subcommand, string args = "")
		{
			return Run(BuildArguments(subcommand, args));
		}

		public Task<RaiSystemResult> RunSubcommandAsync(string subcommand, string args = "", CancellationToken cancellationToken = default)
		{
			return RunAsync(BuildArguments(subcommand, args), cancellationToken);
		}
	}
}