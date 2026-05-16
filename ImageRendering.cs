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
	public sealed class TemplateSetting
	{
		public const string DefaultWipImage = "WorkInProgress";
		public const string DefaultFormat = "webp";

		private string resize = string.Empty;
		private string format = DefaultFormat;
		private int width;
		private int height;

		public string Name { get; set; } = string.Empty;
		public string Note { get; set; } = string.Empty;

		public string Resize
		{
			get => resize;
			set
			{
				resize = value?.Trim() ?? string.Empty;
				(width, height) = ParseSize(resize);
			}
		}

		public int Width
		{
			get => width;
			set
			{
				width = Math.Max(0, value);
				resize = ComposeSize(width, height);
			}
		}

		public int Height
		{
			get => height;
			set
			{
				height = Math.Max(0, value);
				resize = ComposeSize(width, height);
			}
		}

		/// <summary>Legacy alias for <see cref="Resize"/>.</summary>
		public string Size
		{
			get => getSize();
			set => setSize(value);
		}

		/// <summary>Output extension without leading dot.</summary>
		public string Ext
		{
			get => Format;
			set => Format = value;
		}

		public string Format
		{
			get => string.IsNullOrWhiteSpace(format) ? DefaultFormat : format;
			set => format = NormalizeExtension(value, DefaultFormat);
		}

		public int Quality { get; set; } = 85;
		public bool ForceAR { get; set; }
		public bool Strip { get; set; } = true;
		public bool AdaptiveResize { get; set; }
		public bool Compress { get; set; }
		public bool Flatten { get; set; }
		public string Unsharp { get; set; } = string.Empty;
		public int Density { get; set; }
		public string CustomFirst { get; set; } = string.Empty;
		public string CustomLast { get; set; } = string.Empty;
		public bool WipFallback { get; set; }
		public string WipImage { get; set; } = string.Empty;
		public string EffectiveWipImage => string.IsNullOrWhiteSpace(WipImage)
			? DefaultWipImage
			: StripKnownExtension(WipImage);

		public string getSize() => string.IsNullOrEmpty(Resize) ? null : Resize;
		public void setSize(string value) => Resize = value;

		public string BuildConvertOptions()
		{
			var options = new List<string>();
			AddOption(options, CustomFirst);
			if (Density > 0)
				options.Add("-density " + Density);
			options.Add("-auto-orient");
			if (!string.IsNullOrWhiteSpace(Resize))
			{
				var resizeOperator = AdaptiveResize ? "-adaptive-resize" : "-resize";
				options.Add($"{resizeOperator} {BuildResizeGeometry(Resize, ForceAR)}");
			}
			if (Flatten)
				options.Add("-flatten");
			if (!string.IsNullOrWhiteSpace(Unsharp))
				options.Add("-unsharp " + Unsharp.Trim());
			if (Strip)
				options.Add("-strip");
			if (Quality > 0)
				options.Add("-quality " + Quality);
			AddOption(options, CustomLast);
			return string.Join(" ", options);
		}

		public TemplateSetting()
		{
		}

		public TemplateSetting(string name, string resize, bool forceAR, string format, int quality, bool strip,
			bool wipFallback = false, string wipImage = null)
		{
			Name = name ?? string.Empty;
			Resize = resize;
			ForceAR = forceAR;
			Format = format;
			Quality = quality;
			Strip = strip;
			WipFallback = wipFallback;
			WipImage = wipImage ?? string.Empty;
		}

		public TemplateSetting(string name, string note, string size, string ext, bool strip, string quality,
			string wipName, bool adaptiveResize, string unsharp, int density = 0, bool compress = false,
			bool flatten = false, string customFirst = null, string customLast = null)
		{
			Name = name ?? string.Empty;
			Note = note ?? string.Empty;
			Resize = size;
			Format = ext;
			Strip = strip;
			Quality = int.TryParse((quality ?? string.Empty).TrimEnd('%'), out var q) ? q : 0;
			WipImage = wipName ?? string.Empty;
			AdaptiveResize = adaptiveResize;
			Unsharp = unsharp ?? string.Empty;
			Density = density;
			Compress = compress;
			Flatten = flatten;
			CustomFirst = customFirst ?? string.Empty;
			CustomLast = customLast ?? string.Empty;
		}

		private static void AddOption(ICollection<string> options, string option)
		{
			if (!string.IsNullOrWhiteSpace(option))
				options.Add(option.Trim());
		}

		private static string BuildResizeGeometry(string geometry, bool forceAR)
		{
			var trimmed = geometry.Trim();
			if (trimmed.Length == 0)
				return trimmed;
			var last = trimmed[^1];
			return last == '!' || last == '>' || last == '<' || last == '^' || last == '@'
				? trimmed
				: trimmed + (forceAR ? "!" : ">");
		}

		private static (int width, int height) ParseSize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return (0, 0);
			var core = value.Trim().TrimEnd('!', '>', '<', '^', '@');
			var pos = core.IndexOf('x');
			if (pos < 0)
				return (0, 0);
			_ = int.TryParse(core[..pos], out var parsedWidth);
			_ = int.TryParse(core[(pos + 1)..], out var parsedHeight);
			return (Math.Max(0, parsedWidth), Math.Max(0, parsedHeight));
		}

		private static string ComposeSize(int width, int height)
		{
			if (width <= 0 && height <= 0)
				return string.Empty;
			return width + "x" + height;
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
	public sealed class OverlaySetting
	{
		public string Name { get; set; } = string.Empty;
		public string Note { get; set; } = string.Empty;
		public string Image { get; set; } = string.Empty;
		public string Gravity { get; set; } = "Center";
		public int Dissolve { get; set; } = 100;
		public int? Width { get; set; }
		public int? Height { get; set; }
		public string AutoGravity { get; set; } = string.Empty;
		public RaiPath OverlayRoot { get; set; }

		/// <summary>Stacking order for multiple overlays. Lower values render first.</summary>
		public int RenderOrder { get; set; }

		/// <summary>Legacy alias for <see cref="RenderOrder"/>.</summary>
		public int RenderTime
		{
			get => RenderOrder;
			set => RenderOrder = value;
		}

		/// <summary>Common z-order alias for <see cref="RenderOrder"/>.</summary>
		public int ZIndex
		{
			get => RenderOrder;
			set => RenderOrder = value;
		}

		public string Size
		{
			get => getSize();
			set => setSize(value);
		}

		public string getSize()
		{
			if ((Width ?? 0) <= 0 && (Height ?? 0) <= 0)
				return null;
			return (Width ?? 0) + "x" + (Height ?? 0);
		}

		public void setSize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				Width = null;
				Height = null;
				return;
			}
			var pos = value.IndexOf('x');
			if (pos < 0)
				throw new FormatException("Overlay size must be WIDTHxHEIGHT.");
			Width = int.TryParse(value[..pos], out var width) && width > 0 ? width : null;
			Height = int.TryParse(value[(pos + 1)..], out var height) && height > 0 ? height : null;
		}

		public string BuildCompositeOptions()
		{
			var options = new List<string>();
			options.Add("-dissolve " + Dissolve);
			options.Add("-gravity " + NormalizeGravity(Gravity));
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

		public OverlaySetting(string name, string image, string gravity, int dissolve, int? width = null,
			int renderOrder = 0, RaiPath overlayRoot = null)
		{
			Name = name ?? string.Empty;
			Image = image ?? string.Empty;
			Gravity = string.IsNullOrWhiteSpace(gravity) ? "Center" : gravity;
			Dissolve = dissolve;
			Width = width;
			RenderOrder = renderOrder;
			OverlayRoot = overlayRoot;
		}

		public OverlaySetting(string name, string note, string image, int width, int height, string dissolve,
			string gravity, string autoGravity, int renderTime, string overlayDir)
			: this(name, note, image, dissolve, gravity, autoGravity, renderTime, overlayDir)
		{
			if (Width == null && width > 0)
				Width = width;
			if (Height == null && height > 0)
				Height = height;
		}

		public OverlaySetting(string name, string note, string image, string dissolve, string gravity,
			string autoGravity, int renderTime, string overlayDir)
		{
			Name = name ?? string.Empty;
			Note = note ?? string.Empty;
			Image = image ?? string.Empty;
			Dissolve = int.TryParse((dissolve ?? string.Empty).TrimEnd('%'), out var d) ? d : 100;
			Gravity = string.IsNullOrWhiteSpace(gravity) ? "Center" : gravity;
			AutoGravity = autoGravity ?? string.Empty;
			RenderOrder = renderTime;
			OverlayRoot = string.IsNullOrWhiteSpace(overlayDir) ? null : new RaiPath(overlayDir);
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
				"" => "Center",
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

		public static ImageTreeFile FromImageTree(RaiPath imageTreeRoot, string subscriber, string itemId,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			if (imageTreeRoot == null)
				throw new ArgumentNullException(nameof(imageTreeRoot));
			ValidatePlainSegment(subscriber, nameof(subscriber));
			if (string.IsNullOrWhiteSpace(itemId))
				throw new ArgumentException("ItemId is required.", nameof(itemId));
			if (itemId.Contains('/') || itemId.Contains('\\'))
				throw new ArgumentException("ItemId must be a plain file stem.", nameof(itemId));

			var ownerRoot = imageTreeRoot / new RaiRelPath(subscriber);
			var source = new ImageTreeFile(ownerRoot, TemplateSetting.StripKnownExtension(itemId), string.Empty, "png", convention);
			if (!TryExtendToFirstExistingFile(source, sourceExtensions ?? DefaultSourceExtensions, convention))
				throw new FileNotFoundException(
					$"Source image '{itemId}' was not found for subscriber '{subscriber}' under '{imageTreeRoot.FullPath}'.",
					source.FullName);
			return source;
		}

		public static ImageTreeFile FromImageTree(string imageTreeRoot, string subscriber, string itemId,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(new RaiPath(imageTreeRoot), subscriber, itemId, sourceExtensions, convention);

		public static ImageTreeFile FromExternalLink(RaiPath imageTreeRoot, string externalLink,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			var request = ParseExternalLink(externalLink, ModernImgRouteConvention.Default);
			return FromImageTree(imageTreeRoot, request.Subscriber, request.ItemId, sourceExtensions, convention);
		}

		public static ImageTreeFile FromExternalLink(string imageTreeRoot, string externalLink,
			string sourceExtensions = null, PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(new RaiPath(imageTreeRoot), externalLink, sourceExtensions, convention);

		public static ImageTreeFile FromExternalLink(RaiPath imageTreeRoot, string externalLink,
			IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
		{
			var request = ParseExternalLink(externalLink, routeConvention);
			return FromImageTree(imageTreeRoot, request.Subscriber, request.ItemId, sourceExtensions, convention);
		}

		public static ImageTreeFile FromExternalLink(string imageTreeRoot, string externalLink,
			IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(new RaiPath(imageTreeRoot), externalLink, routeConvention, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string subscriber, string itemId,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromImageTree(imageTreeRoot, subscriber, itemId, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string subscriber, string itemId,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), subscriber, itemId, tmp, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string externalLink,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(imageTreeRoot, externalLink, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string externalLink,
			TemplateSetting tmp, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), externalLink, tmp, sourceExtensions, convention);

		public static ImageTreeFile ApplyTemplate(RaiPath imageTreeRoot, string externalLink,
			TemplateSetting tmp, IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> FromExternalLink(imageTreeRoot, externalLink, routeConvention, sourceExtensions, convention).ApplyTemplate(tmp);

		public static ImageTreeFile ApplyTemplate(string imageTreeRoot, string externalLink,
			TemplateSetting tmp, IImageRouteConvention routeConvention, string sourceExtensions = null,
			PathConventionType convention = PathConventionType.ItemIdTree8x2)
			=> ApplyTemplate(new RaiPath(imageTreeRoot), externalLink, tmp, routeConvention, sourceExtensions, convention);

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
				if ((overlay.Width ?? 0) > 0 || (overlay.Height ?? 0) > 0)
				{
					tempOverlay = CreateTempOverlayFile(target);
					var resize = BuildOverlayResizeGeometry(overlay);
					var resizeExit = magick.Convert("-resize " + resize, overlayFile.FullName, tempOverlay.FullName, true);
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

		private ImageTreeFile CreateRenderingTarget(string renderingName, string ext)
		{
			var target = new ImageTreeFile(Path, ItemId, renderingName,
				TemplateSetting.NormalizeExtension(ext, TemplateSetting.DefaultFormat), Convention);
			return target;
		}

		private static string AppendRenderingName(string current, string addition)
			=> string.IsNullOrWhiteSpace(current) ? addition : current + addition;

		private static string BuildOverlayResizeGeometry(OverlaySetting overlay)
		{
			var width = overlay.Width.GetValueOrDefault();
			var height = overlay.Height.GetValueOrDefault();
			if (width > 0 && height > 0)
				return width + "x" + height;
			return width > 0 ? width + "x" : "x" + height;
		}

		private static RaiFile CreateTempOverlayFile(ImageTreeFile target)
			=> new RaiFile(target.SubdirRoot, ".raimage-overlay-" + Guid.NewGuid().ToString("N"), "png");

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
