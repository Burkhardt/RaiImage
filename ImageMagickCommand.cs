using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage
{
	public class ImageMagickCommand : CliCommand
	{
		private readonly string imPath;
		private readonly string commandName;

		public ImageMagickCommand(string imPath = null, string commandName = null)
			: base(commandName ?? ImageMagick.MagickCommand, packageName: "imagemagick")
		{
			this.imPath = imPath ?? ImageMagick.ImPath;
			this.commandName = commandName ?? ImageMagick.MagickCommand;
		}

		public override IEnumerable<string> CandidateExecutables
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(imPath))
				{
					var cmd = new RaiFile(commandName) { Path = imPath };
					yield return cmd.FullName;
				}

				yield return commandName;
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