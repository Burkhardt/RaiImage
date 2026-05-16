using System;
using System.Collections.Generic;
using System.Linq;

namespace RaiImage
{
	/// <summary>
	/// External-facing image route convention.
	/// Parallel to <c>PathConventionType</c>, which governs filesystem layout.
	/// </summary>
	public enum ImageRouteConventionType
	{
		/// <summary>
		/// Modern ImageServer route: /img/{subscriber}/{itemId}?tmp={templateAndOverlays}
		/// </summary>
		ModernImg
	}

	/// <summary>
	/// Neutral request model produced by an image route convention before path resolution and rendering.
	/// </summary>
	public sealed class ImageRenderRequest
	{
		public string Subscriber { get; }
		public string ItemId { get; }
		public string TemplateName { get; }
		public IReadOnlyList<string> OverlayNames { get; }

		public ImageRenderRequest(string subscriber, string itemId, string templateName = null,
			IEnumerable<string> overlayNames = null)
		{
			Subscriber = RequirePlainSegment(subscriber, nameof(subscriber));
			ItemId = RequirePlainSegment(TemplateSetting.StripKnownExtension(itemId), nameof(itemId));
			TemplateName = string.IsNullOrWhiteSpace(templateName) ? string.Empty : RequirePlainSegment(templateName, nameof(templateName));
			OverlayNames = (overlayNames ?? Enumerable.Empty<string>())
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.Select(name => RequirePlainSegment(name, nameof(overlayNames)))
				.ToArray();
		}

		private static string RequirePlainSegment(string value, string parameterName)
		{
			var trimmed = (value ?? string.Empty).Trim();
			if (trimmed.Length == 0)
				throw new ArgumentException("Value is required.", parameterName);
			if (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains('?') || trimmed.Contains('#'))
				throw new ArgumentException("Value must be a plain route segment.", parameterName);
			return trimmed;
		}
	}

	/// <summary>
	/// Parses and builds external image URLs without deciding how files are laid out on disk.
	/// </summary>
	public interface IImageRouteConvention
	{
		ImageRouteConventionType Type { get; }
		bool TryParse(string externalLink, out ImageRenderRequest request);
		string Build(ImageRenderRequest request);
	}

	/// <summary>
	/// Modern, focused ImageServer route convention: /img/{subscriber}/{itemId}?tmp={templateAndOverlays}.
	/// </summary>
	public sealed class ModernImgRouteConvention : IImageRouteConvention
	{
		public const string DefaultRouteRoot = "img";
		public static ModernImgRouteConvention Default { get; } = new ModernImgRouteConvention();

		public ImageRouteConventionType Type => ImageRouteConventionType.ModernImg;
		public string RouteRoot { get; }

		public ModernImgRouteConvention(string routeRoot = DefaultRouteRoot)
		{
			RouteRoot = RequireRouteRoot(routeRoot);
		}

		public bool TryParse(string externalLink, out ImageRenderRequest request)
		{
			request = null;
			if (string.IsNullOrWhiteSpace(externalLink))
				return false;

			try
			{
				var (path, query) = ExtractPathAndQuery(externalLink);
				var segments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
					.Select(Uri.UnescapeDataString)
					.ToArray();
				if (segments.Length < 3)
					return false;

				var routeRootIndex = segments.Length - 3;
				if (!string.Equals(segments[routeRootIndex], RouteRoot, StringComparison.OrdinalIgnoreCase))
					return false;

				var tmpValue = GetQueryValue(query, "tmp");
				var tmp = string.IsNullOrWhiteSpace(tmpValue) ? null : new Tmp(tmpValue);
				request = new ImageRenderRequest(
					segments[routeRootIndex + 1],
					segments[routeRootIndex + 2],
					tmp?.Template,
					tmp?.Overlays);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
			catch (UriFormatException)
			{
				return false;
			}
		}

		public ImageRenderRequest Parse(string externalLink)
		{
			if (TryParse(externalLink, out var request))
				return request;
			throw new FormatException($"External link does not match the {nameof(ModernImgRouteConvention)} format.");
		}

		public string Build(ImageRenderRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));
			var route = "/" + RouteRoot + "/" +
				Uri.EscapeDataString(request.Subscriber) + "/" +
				Uri.EscapeDataString(request.ItemId);
			var tmp = request.TemplateName + string.Join("", request.OverlayNames);
			return string.IsNullOrEmpty(tmp)
				? route
				: route + "?tmp=" + Uri.EscapeDataString(tmp);
		}

		private static (string path, string query) ExtractPathAndQuery(string externalLink)
		{
			var link = externalLink.Trim();
			if (link.StartsWith("/", StringComparison.Ordinal) || link.StartsWith("\\", StringComparison.Ordinal))
				return SplitRelativePathAndQuery(link);
			if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
				return (uri.AbsolutePath, uri.Query.TrimStart('?'));

			return SplitRelativePathAndQuery(link);
		}

		private static (string path, string query) SplitRelativePathAndQuery(string link)
		{
			var fragment = link.IndexOf('#');
			if (fragment >= 0)
				link = link[..fragment];
			var query = link.IndexOf('?');
			return query >= 0
				? (link[..query], link[(query + 1)..])
				: (link, string.Empty);
		}

		private static string GetQueryValue(string query, string key)
		{
			if (string.IsNullOrWhiteSpace(query))
				return string.Empty;
			foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
			{
				var separator = pair.IndexOf('=');
				var rawKey = separator >= 0 ? pair[..separator] : pair;
				var decodedKey = Uri.UnescapeDataString(rawKey.Replace("+", " "));
				if (!string.Equals(decodedKey, key, StringComparison.OrdinalIgnoreCase))
					continue;
				var rawValue = separator >= 0 ? pair[(separator + 1)..] : string.Empty;
				return Uri.UnescapeDataString(rawValue.Replace("+", " "));
			}
			return string.Empty;
		}

		private static string RequireRouteRoot(string routeRoot)
		{
			var trimmed = (routeRoot ?? string.Empty).Trim().Trim('/');
			if (trimmed.Length == 0)
				throw new ArgumentException("Route root is required.", nameof(routeRoot));
			if (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains('?') || trimmed.Contains('#'))
				throw new ArgumentException("Route root must be a plain route segment.", nameof(routeRoot));
			return trimmed;
		}
	}
}
