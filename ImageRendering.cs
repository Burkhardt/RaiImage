using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsLib;

namespace RaiImage
{
	/// <summary>
	/// Per-subscriber rendering template consumed by <see cref="ImageTreeFile.ApplyTemplate(TemplateSetting)"/>.
	/// </summary>
	/// <remarks>
	/// Hybrid String Schema:
	/// <list type="bullet">
	/// <item>Explicit properties form the schema the UI uses to generate form fields.</item>
	/// <item>Optional scalars are <see cref="string"/> (so ImageMagick's mini-language — <c>800x</c>, <c>50%</c>,
	/// <c>800x600^</c>, <c>85%</c> — passes through unchanged) or <see cref="Nullable{T}"/> for true tri-state semantics.</item>
	/// <item>No hardcoded defaults: a <c>null</c>/whitespace property omits the flag entirely.</item>
	/// <item><see cref="Extra"/> is the duck-typed escape hatch for any ImageMagick flag not modeled explicitly.</item>
	/// </list>
	/// </remarks>
	public sealed class TemplateSetting
	{
		public const string DefaultWipImage = "WorkInProgress";
		public const string DefaultFormat = "webp";

		// Identity / metadata (not ImageMagick flags).
		public string Name { get; set; } = string.Empty;
		public string Note { get; set; } = string.Empty;

		/// <summary>Output container extension (without leading dot). Not an ImageMagick flag; selects the target file name.</summary>
		public string Format { get; set; }

		// Canonical sizing: a raw ImageMagick geometry string. The single source of truth.
		// Examples: "800x", "x600", "800x600", "800x600!", "800x600^", "800x600>", "50%".
		public string Resize { get; set; }

		// Scalar ImageMagick flag arguments — strings so they preserve %, units, and other modifiers.
		public string Quality { get; set; }
		public string Density { get; set; }
		public string Unsharp { get; set; }

		// Boolean ImageMagick flags — nullable so absent ≠ false.
		public bool? AdaptiveResize { get; set; }
		public bool? Strip { get; set; }
		public bool? Compress { get; set; }
		public bool? Flatten { get; set; }
		public bool? AutoOrient { get; set; }

		// Raw pre/post pass-through (already free-form).
		public string CustomFirst { get; set; }
		public string CustomLast { get; set; }

		// WIP fallback metadata (not ImageMagick flags).
		public bool? WipFallback { get; set; }
		public string WipImage { get; set; }
		public string EffectiveWipImage => string.IsNullOrWhiteSpace(WipImage)
			? DefaultWipImage
			: StripKnownExtension(WipImage);

		/// <summary>
		/// Duck-typed escape hatch for any ImageMagick flag not modeled as an explicit property.
		/// Key is the flag name without leading dash (e.g., <c>"colorspace"</c>); value is the flag argument,
		/// or <c>null</c>/empty for boolean-style flags emitted on their own.
		/// </summary>
		public IDictionary<string, string> Extra { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public string BuildConvertOptions()
		{
			var options = new List<string>();
			AppendRaw(options, CustomFirst);
			AppendValueFlag(options, "density", Density);
			AppendBooleanFlag(options, "auto-orient", AutoOrient);
			if (!string.IsNullOrWhiteSpace(Resize))
			{
				var resizeOperator = AdaptiveResize == true ? "-adaptive-resize" : "-resize";
				options.Add($"{resizeOperator} {Resize.Trim()}");
			}
			AppendBooleanFlag(options, "flatten", Flatten);
			AppendValueFlag(options, "unsharp", Unsharp);
			AppendBooleanFlag(options, "strip", Strip);
			AppendBooleanFlag(options, "compress", Compress);
			AppendValueFlag(options, "quality", Quality);
			AppendExtra(options, Extra);
			AppendRaw(options, CustomLast);
			return string.Join(" ", options);
		}

		public TemplateSetting()
		{
		}

		internal static void AppendRaw(ICollection<string> options, string raw)
		{
			if (!string.IsNullOrWhiteSpace(raw))
				options.Add(raw.Trim());
		}

		internal static void AppendValueFlag(ICollection<string> options, string flag, string value)
		{
			if (!string.IsNullOrWhiteSpace(value))
				options.Add("-" + flag + " " + value.Trim());
		}

		internal static void AppendBooleanFlag(ICollection<string> options, string flag, bool? value)
		{
			if (value == true)
				options.Add("-" + flag);
		}

		internal static void AppendExtra(ICollection<string> options, IDictionary<string, string> extra)
		{
			if (extra == null)
				return;
			foreach (var kv in extra)
			{
				if (string.IsNullOrWhiteSpace(kv.Key))
					continue;
				var flag = "-" + kv.Key.Trim().TrimStart('-');
				options.Add(string.IsNullOrWhiteSpace(kv.Value) ? flag : flag + " " + kv.Value.Trim());
			}
		}

		internal static string NormalizeExtension(string value, string fallback = "")
		{
			var ext = (value ?? string.Empty).Trim();
			if (ext.StartsWith(".", StringComparison.Ordinal))
				ext = ext[1..];
			return string.IsNullOrWhiteSpace(ext) ? fallback : ext.ToLowerInvariant();
		}

		internal static string StripKnownExtension(string raw)
		{
			var trimmed = (raw ?? string.Empty).Trim();
			var dot = trimmed.LastIndexOf('.');
			if (dot <= 0 || dot >= trimmed.Length - 1)
				return trimmed;
			var ext = trimmed[(dot + 1)..];
			return ImageTreeFile.DefaultSourceExtensions
				.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
				.Any(candidate => string.Equals(candidate.TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase))
					? trimmed[..dot]
					: trimmed;
		}
	}

	/// <summary>
	/// Overlay definition consumed by <see cref="ImageTreeFile.ApplyOverlay(OverlaySetting)"/>.
	/// </summary>
	/// <remarks>
	/// Follows the same Hybrid String Schema as <see cref="TemplateSetting"/>: explicit string properties
	/// form the UI schema, optional flags use <c>string</c> or <see cref="Nullable{T}"/> so absent ≠ default,
	/// and <see cref="Extra"/> carries any unmodelled ImageMagick composite flags.
	/// </remarks>
	public sealed class OverlaySetting
	{
		public string Name { get; set; } = string.Empty;
		public string Note { get; set; } = string.Empty;
		public string Image { get; set; } = string.Empty;

		/// <summary>ImageMagick gravity (e.g., <c>Center</c>, <c>NorthEast</c>, or shorthand <c>NE</c>). Null = no <c>-gravity</c> flag.</summary>
		public string Gravity { get; set; }

		/// <summary>ImageMagick dissolve argument as a raw string (e.g., <c>"70"</c> or <c>"70x40"</c>). Null = no <c>-dissolve</c> flag.</summary>
		public string Dissolve { get; set; }

		/// <summary>Canonical overlay sizing as a raw ImageMagick geometry string (e.g., <c>"60x"</c>, <c>"x80"</c>, <c>"60x60!"</c>). Null = overlay used at native size.</summary>
		public string Resize { get; set; }

		public string AutoGravity { get; set; }
		public RaiPath OverlayRoot { get; set; }

		/// <summary>Stacking order for multiple overlays. Lower values render first.</summary>
		public int RenderOrder { get; set; }

		/// <summary>
		/// Duck-typed escape hatch for any ImageMagick composite flag not modeled as an explicit property.
		/// See <see cref="TemplateSetting.Extra"/> for the contract.
		/// </summary>
		public IDictionary<string, string> Extra { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public string BuildCompositeOptions()
		{
			var options = new List<string>();
			TemplateSetting.AppendValueFlag(options, "dissolve", Dissolve);
			if (!string.IsNullOrWhiteSpace(Gravity))
				options.Add("-gravity " + NormalizeGravity(Gravity));
			TemplateSetting.AppendExtra(options, Extra);
			return string.Join(" ", options);
		}

		public RaiFile ResolveImageFile(RaiPath relativeRoot = null)
		{
			if (string.IsNullOrWhiteSpace(Image))
				throw new ArgumentException("Overlay image is required.", nameof(Image));

			var normalized = Os.NormSeperator(Image.Trim());
			var rooted = System.IO.Path.IsPathRooted(normalized)
				|| normalized.StartsWith("~" + Os.DIR, StringComparison.Ordinal)
				|| normalized == "~";
			if (rooted)
				return new RaiFile(normalized);

			var root = OverlayRoot ?? relativeRoot;
			return root == null
				? new RaiFile(normalized)
				: new RaiFile(root.FullPath + normalized.TrimStart(Os.DIR[0]));
		}

		public OverlaySetting()
		{
		}

		private static string NormalizeGravity(string gravity)
		{
			return (gravity ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"N" => "North",
				"NE" => "NorthEast",
				"E" => "East",
				"SE" => "SouthEast",
				"S" => "South",
				"SW" => "SouthWest",
				"W" => "West",
				"NW" => "NorthWest",
				"C" => "Center",
				_ => gravity.Trim()
			};
		}
	}

	public sealed class ImageRenderingException : InvalidOperationException
	{
		public ImageRenderingException(string message) : base(message)
		{
		}

		public ImageRenderingException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}

	public partial class ImageTreeFile
	{
		public static string DefaultSourceExtensions { get; set; } =
			"webp,jpg,jpeg,png,heic,heif,tif,tiff,gif,bmp,psd";

		public static ImageTreeFile FromName(RaiPath rootPath, string name,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromName(rootPath, name, InferSourceNamingConvention(name), convention);

		public static ImageTreeFile FromName(string rootPath, string name,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromName(new RaiPath(rootPath), name, convention);

		public static ImageTreeFile FromName(RaiPath rootPath, string name,
			ImageNamingConvention namingConvention,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			if (rootPath == null)
				throw new ArgumentNullException(nameof(rootPath));
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Name is required.", nameof(name));
			if (name.Contains('/') || name.Contains('\\'))
				throw new ArgumentException("Name must be a plain file name or stem.", nameof(name));

			var stem = TemplateSetting.StripKnownExtension(name);
			if (string.IsNullOrWhiteSpace(stem))
				throw new ArgumentException("Name is required.", nameof(name));
			return new ImageTreeFile(rootPath, stem, string.Empty, string.Empty, convention, namingConvention);
		}

		public static ImageTreeFile FromName(string rootPath, string name,
			ImageNamingConvention namingConvention,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromName(new RaiPath(rootPath), name, namingConvention, convention);

		public static ImageTreeFile FromImageTree(RaiPath imageTreeRoot, string subscriber, string itemId,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(imageTreeRoot, subscriber, itemId, InferSourceNamingConvention(itemId), sourceExtensions, convention);

		public static ImageTreeFile FromImageTree(RaiPath imageTreeRoot, string subscriber, string itemId,
			ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			if (imageTreeRoot == null)
				throw new ArgumentNullException(nameof(imageTreeRoot));
			ValidatePlainSegment(subscriber, nameof(subscriber));
			if (string.IsNullOrWhiteSpace(itemId))
				throw new ArgumentException("ItemId is required.", nameof(itemId));
			if (itemId.Contains('/') || itemId.Contains('\\'))
				throw new ArgumentException("ItemId must be a plain file stem.", nameof(itemId));

			var ownerRoot = imageTreeRoot / new RaiRelPath(subscriber);
			var source = FromName(ownerRoot, itemId, namingConvention, convention);
			if (!TryExtendToFirstExistingFile(source, sourceExtensions ?? DefaultSourceExtensions, convention))
				throw new FileNotFoundException(
					$"Source image '{itemId}' was not found for subscriber '{subscriber}' under '{imageTreeRoot.FullPath}'.",
					source.FullName);
			return source;
		}

		public static ImageTreeFile FromImageTree(string imageTreeRoot, string subscriber, string itemId,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(new RaiPath(imageTreeRoot), subscriber, itemId, sourceExtensions, convention);

		public static ImageTreeFile FromImageTree(string imageTreeRoot, string subscriber, string itemId,
			ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(new RaiPath(imageTreeRoot), subscriber, itemId, namingConvention, sourceExtensions, convention);

		public static ImageTreeFile FromExternalLink(RaiPath imageTreeRoot, string externalLink,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			var request = ParseExternalLink(externalLink, ModernImgRouteConvention.Default);
			return FromImageTree(imageTreeRoot, request.Subscriber, request.ItemId, sourceExtensions, convention);
		}

		public static ImageTreeFile FromExternalLink(RaiPath imageTreeRoot, string externalLink,
			ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			var request = ParseExternalLink(externalLink, ModernImgRouteConvention.Default);
			return FromImageTree(imageTreeRoot, request.Subscriber, request.ItemId, namingConvention, sourceExtensions, convention);
		}

		public static ImageTreeFile FromExternalLink(string imageTreeRoot, string externalLink,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(new RaiPath(imageTreeRoot), externalLink, sourceExtensions, convention);

		public static ImageTreeFile FromExternalLink(string imageTreeRoot, string externalLink,
			ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(new RaiPath(imageTreeRoot), externalLink, namingConvention, sourceExtensions, convention);

		public static ImageTreeFile FromExternalLink(RaiPath imageTreeRoot, string externalLink,
			IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			var request = ParseExternalLink(externalLink, routeConvention);
			return FromImageTree(imageTreeRoot, request.Subscriber, request.ItemId, sourceExtensions, convention);
		}

		public static ImageTreeFile FromExternalLink(RaiPath imageTreeRoot, string externalLink,
			IImageRouteConvention routeConvention, ImageNamingConvention namingConvention,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			var request = ParseExternalLink(externalLink, routeConvention);
			return FromImageTree(imageTreeRoot, request.Subscriber, request.ItemId, namingConvention, sourceExtensions, convention);
		}

		public static ImageTreeFile FromExternalLink(string imageTreeRoot, string externalLink,
			IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(new RaiPath(imageTreeRoot), externalLink, routeConvention, sourceExtensions, convention);

		public static ImageTreeFile FromExternalLink(string imageTreeRoot, string externalLink,
			IImageRouteConvention routeConvention, ImageNamingConvention namingConvention,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(new RaiPath(imageTreeRoot), externalLink, routeConvention, namingConvention, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string subscriber, string itemId,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(imageTreeRoot, subscriber, itemId, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string subscriber, string itemId,
			TemplateSetting tmp, ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(imageTreeRoot, subscriber, itemId, namingConvention, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string subscriber, string itemId,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), subscriber, itemId, tmp, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string subscriber, string itemId,
			TemplateSetting tmp, ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), subscriber, itemId, tmp, namingConvention, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string externalLink,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(imageTreeRoot, externalLink, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string externalLink,
			TemplateSetting tmp, ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(imageTreeRoot, externalLink, namingConvention, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string externalLink,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), externalLink, tmp, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string externalLink,
			TemplateSetting tmp, ImageNamingConvention namingConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), externalLink, tmp, namingConvention, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string externalLink,
			TemplateSetting tmp, IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(imageTreeRoot, externalLink, routeConvention, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string externalLink,
			TemplateSetting tmp, IImageRouteConvention routeConvention, ImageNamingConvention namingConvention,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(imageTreeRoot, externalLink, routeConvention, namingConvention, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string externalLink,
			TemplateSetting tmp, IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), externalLink, tmp, routeConvention, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string externalLink,
			TemplateSetting tmp, IImageRouteConvention routeConvention, ImageNamingConvention namingConvention,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), externalLink, tmp, routeConvention, namingConvention, sourceExtensions, convention);

		public ImageTreeFile ApplyTemplate(TemplateSetting tmp)
		{
			if (tmp == null)
				throw new ArgumentNullException(nameof(tmp));
			ValidateTemplateName(tmp.Name, nameof(tmp.Name));
			EnsureSourceExists(this);

			var target = CreateRenderingTarget(tmp.Name, tmp.Format);
			target.mkdir();

			var magick = new ImageMagick();
			var exitCode = magick.Convert(tmp.BuildConvertOptions(), FullName, target.FullName);
			EnsureRenderSucceeded(exitCode, target, magick.Message, "template", tmp.Name);
			return target;
		}

		public ImageTreeFile ApplyOverlay(OverlaySetting overlay)
		{
			if (overlay == null)
				throw new ArgumentNullException(nameof(overlay));
			ValidateTemplateName(overlay.Name, nameof(overlay.Name));
			EnsureSourceExists(this);

			var target = CreateRenderingTarget(AppendRenderingName(TemplateName, overlay.Name), Ext);
			target.mkdir();

			var magick = new ImageMagick();
			var overlayFile = overlay.ResolveImageFile(Path);
			if (!overlayFile.Exists())
				throw new FileNotFoundException("Overlay image was not found.", overlayFile.FullName);

			RaiFile compositedOverlay = overlayFile;
			RaiFile tempOverlay = null;
			try
			{
				if (!string.IsNullOrWhiteSpace(overlay.Resize))
				{
					tempOverlay = CreateTempOverlayFile(target);
					var resizeExit = magick.Convert("-resize " + overlay.Resize.Trim(), overlayFile.FullName, tempOverlay.FullName, true);
					EnsureRenderSucceeded(resizeExit, tempOverlay, magick.Message, "overlay resize", overlay.Name);
					compositedOverlay = tempOverlay;
				}

				var exitCode = magick.Composite(overlay.BuildCompositeOptions(), compositedOverlay.FullName, FullName, target.FullName);
				EnsureRenderSucceeded(exitCode, target, magick.Message, "overlay", overlay.Name);
			}
			finally
			{
				if (tempOverlay?.Exists() == true)
					tempOverlay.rm();
			}

			return target;
		}

		public ImageTreeFile ApplyOverlays(List<OverlaySetting> overlays)
			=> ApplyOverlays((IEnumerable<OverlaySetting>)overlays);

		public ImageTreeFile ApplyOverlays(IEnumerable<OverlaySetting> overlays)
		{
			if (overlays == null)
				return this;
			var current = this;
			foreach (var overlay in overlays
				.Select((setting, index) => new { setting, index })
				.Where(item => item.setting != null)
				.OrderBy(item => item.setting.RenderOrder)
				.ThenBy(item => item.index))
			{
				current = current.ApplyOverlay(overlay.setting);
			}
			return current;
		}

		public static PlantUmlRenderResult RenderPlantUml(RaiPath imageTreeRoot, string subscriber, string itemId,
			string plantUmlContent, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> RenderPlantUml(imageTreeRoot, subscriber, itemId, plantUmlContent,
				InferSourceNamingConvention(itemId), convention);

		public static PlantUmlRenderResult RenderPlantUml(string imageTreeRoot, string subscriber, string itemId,
			string plantUmlContent, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> RenderPlantUml(new RaiPath(imageTreeRoot), subscriber, itemId, plantUmlContent, convention);

		public static PlantUmlRenderResult RenderPlantUml(RaiPath imageTreeRoot, string subscriber, string itemId,
			string plantUmlContent, ImageNamingConvention namingConvention,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			if (imageTreeRoot == null)
				throw new ArgumentNullException(nameof(imageTreeRoot));
			ValidatePlainSegment(subscriber, nameof(subscriber));
			if (string.IsNullOrWhiteSpace(itemId))
				throw new ArgumentException("ItemId is required.", nameof(itemId));

			var ownerRoot = imageTreeRoot / new RaiRelPath(subscriber);
			var diagram = FromName(ownerRoot, itemId, namingConvention, convention);
			return diagram.RenderPlantUml(plantUmlContent);
		}

		public static PlantUmlRenderResult RenderPlantUml(string imageTreeRoot, string subscriber, string itemId,
			string plantUmlContent, ImageNamingConvention namingConvention,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> RenderPlantUml(new RaiPath(imageTreeRoot), subscriber, itemId, plantUmlContent, namingConvention, convention);

		public PlantUmlRenderResult RenderPlantUml(string plantUmlContent)
		{
			if (string.IsNullOrWhiteSpace(plantUmlContent))
				throw new ArgumentException("PlantUML content is required.", nameof(plantUmlContent));

			var source = CreateSiblingWithExtension("puml");
			source.mkdir();
			var pumlText = new TextFile(source.FullName);
			pumlText.DeleteAll().Append(plantUmlContent).Save();

			var svg = CreateSiblingWithExtension("svg");
			var plantUml = new PlantUml();
			var result = plantUml.RenderSvg(source.FullName);
			EnsureRenderSucceeded(result.ExitCode, svg, plantUml.Message, "PlantUML", source.Name);
			return new PlantUmlRenderResult(source, svg);
		}

		private ImageTreeFile CreateRenderingTarget(string renderingName, string ext)
		{
			var normalizedExt = TemplateSetting.NormalizeExtension(ext, TemplateSetting.DefaultFormat);
			if (ImageNumber == NoImageNumber)
				return new ImageTreeFile(Path, ItemId, renderingName, normalizedExt, Convention);

			var targetStem = ItemId + "_" + ImageNumber.ToString("D2") + "_" + renderingName;
			return new ImageTreeFile(Path.FullPath + targetStem + "." + normalizedExt,
				Convention, ImageNamingConvention.Structured);
		}

		public static ImageNamingConvention InferSourceNamingConvention(string itemId)
		{
			var stem = TemplateSetting.StripKnownExtension(itemId);
			var comma = stem.IndexOf(',');
			var positional = comma >= 0 ? stem[..comma] : stem;
			var parts = positional.Split('_');
			return parts.Length >= 2 && int.TryParse(parts[1], out _)
				? ImageNamingConvention.Structured
				: ImageNamingConvention.Legacy;
		}

		private static string AppendRenderingName(string current, string addition)
			=> string.IsNullOrWhiteSpace(current) ? addition : current + addition;

		private static RaiFile CreateTempOverlayFile(ImageTreeFile target)
			=> new RaiFile(target.SubdirRoot, ".raimage-overlay-" + Guid.NewGuid().ToString("N"), "png");

		private ImageTreeFile CreateSiblingWithExtension(string ext)
			=> new ImageTreeFile(SubdirRoot.FullPath + Name + "." + TemplateSetting.NormalizeExtension(ext),
				Convention, NamingConvention);

		private static void EnsureSourceExists(ImageTreeFile source)
		{
			if (!source.Exists())
				throw new FileNotFoundException("Source image was not found.", source.FullName);
		}

		private static void EnsureRenderSucceeded(int exitCode, ImageTreeFile target, string message,
			string operation, string settingName)
		{
			if (exitCode == 0 && target.Exists())
				return;
			throw new ImageRenderingException(
				$"ImageMagick {operation} render failed for '{settingName}' to '{target.FullName}' " +
				$"(exit {exitCode}). {message}".Trim());
		}

		private static void EnsureRenderSucceeded(int exitCode, RaiFile target, string message,
			string operation, string settingName)
		{
			if (exitCode == 0 && target.Exists())
				return;
			throw new ImageRenderingException(
				$"ImageMagick {operation} render failed for '{settingName}' to '{target.FullName}' " +
				$"(exit {exitCode}). {message}".Trim());
		}

		private static bool TryExtendToFirstExistingFile(ImageTreeFile source, string sourceExtensions,
			PathConventionType convention)
		{
			try
			{
				return source.ExtendToFirstExistingFile(sourceExtensions, convention);
			}
			catch (DirectoryNotFoundException)
			{
				return false;
			}
		}

		private static ImageRenderRequest ParseExternalLink(string externalLink, IImageRouteConvention routeConvention)
		{
			if (routeConvention == null)
				throw new ArgumentNullException(nameof(routeConvention));
			if (routeConvention.TryParse(externalLink, out var request))
				return request;
			throw new FormatException($"External link does not match the {routeConvention.GetType().Name} format.");
		}

		private static void ValidatePlainSegment(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("Value is required.", parameterName);
			if (value.Contains('/') || value.Contains('\\'))
				throw new ArgumentException("Value must be a plain path segment.", parameterName);
		}

		private static void ValidateTemplateName(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("Rendering setting name is required.", parameterName);
			if (value.Contains('/') || value.Contains('\\') || value.Contains('.') || value.Contains(','))
				throw new ArgumentException("Rendering setting name must be a plain name segment.", parameterName);
		}
	}
}
