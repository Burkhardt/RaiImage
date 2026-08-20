using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OsLib;

namespace RaiImage
{
	public sealed class OptiPngCommand : CliCommand
	{
		public OptiPngCommand(string commandName = "optipng")
			: base(string.IsNullOrWhiteSpace(commandName) ? "optipng" : commandName, packageName: "optipng")
		{
		}

		public IReadOnlyList<string> BuildArguments(RaiFile image)
		{
			if (image == null || string.IsNullOrWhiteSpace(image.FullName))
				throw new ArgumentException("An image file is required.", nameof(image));
			return new[] { image.FullName };
		}

		public RaiSystemResult Optimize(RaiFile image) => Run(BuildArguments(image));
		public Task<RaiSystemResult> OptimizeAsync(RaiFile image, CancellationToken cancellationToken = default)
			=> RunAsync(BuildArguments(image), cancellationToken);
	}

	public sealed class JpegTranCommand : CliCommand
	{
		public JpegTranCommand(string commandName = "jpegtran")
			: base(string.IsNullOrWhiteSpace(commandName) ? "jpegtran" : commandName, packageName: "jpegtran")
		{
		}

		public IReadOnlyList<string> BuildArguments(
			IEnumerable<string> options,
			RaiFile source,
			RaiFile destination)
		{
			if (source == null || string.IsNullOrWhiteSpace(source.FullName))
				throw new ArgumentException("A source JPEG file is required.", nameof(source));
			if (destination == null || string.IsNullOrWhiteSpace(destination.FullName))
				throw new ArgumentException("A destination JPEG file is required.", nameof(destination));

			var arguments = (options ?? Enumerable.Empty<string>())
				.Where(option => !string.IsNullOrWhiteSpace(option))
				.ToList();
			arguments.Add(source.FullName);
			arguments.Add(destination.FullName);
			return arguments;
		}

		public RaiSystemResult Transform(
			IEnumerable<string> options,
			RaiFile source,
			RaiFile destination)
			=> Run(BuildArguments(options, source, destination));

		public Task<RaiSystemResult> TransformAsync(
			IEnumerable<string> options,
			RaiFile source,
			RaiFile destination,
			CancellationToken cancellationToken = default)
			=> RunAsync(BuildArguments(options, source, destination), cancellationToken);
	}
}
