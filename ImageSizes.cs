using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using OsLib;

namespace RaiImage
{
	public class ImageTypes
	{
		public static char[] Seperator = [',', ';', ' ', '\t'];
		public static ImageTypes Default = new(["tiff", "png", "jpg", "psd", "webp"]);

		public string[] Array { get; set; }

		public string String
		{
			get => string.Join(", ", Array);
			set => Array = value.Split(Seperator, StringSplitOptions.RemoveEmptyEntries);
		}

		public ImageTypes(string extensions) => String = extensions;
		public ImageTypes(string[] extensions) => Array = extensions;
	}

	public class Pane
	{
		public static Pane DefaultPane = new("160x200");
		private string wxh;

		public string String => wxh;

		public System.Drawing.Size Size
		{
			get
			{
				if (string.IsNullOrEmpty(wxh) || wxh.IndexOf('x') < 0) return new(0, 0);
				int i = wxh.IndexOf('x');
				return new(int.Parse(wxh[..i]), int.Parse(wxh[(i + 1)..]));
			}
			set => wxh = $"{value.Width}x{value.Height}";
		}

		public Pane(string WxH) => wxh = WxH;
		public Pane(int width, int height) => wxh = $"{width}x{height}";
	}

	public class Panes
	{
		private readonly Pane[] p;
		private readonly string s;

		public int Count => p.Length;
		public string String => s;
		public Pane this[int i] => p[i];

		/// <summary>
		/// Panes the viewer defines; zoomPortPane and ControlPortPane
		/// </summary>
		public Pane ZoomPort => p.Length > 0 ? p[0] : Pane.DefaultPane;

		/// <summary>
		/// Panes the viewer defines; zoomPortPane and ControlPortPane
		/// </summary>
		public Pane ControlPort => p.Length > 1 ? p[1] : ZoomPort;

		/// <summary>
		/// Constructor from formatted string: ZoomPort in [0], ControlPort in [1]
		/// </summary>
		/// <param name="panes">i.e. 352x440,320x400 or 352x440</param>
		public Panes(string panes)
		{
			s = panes;
			p = panes.Split(',').Select(size => new Pane(size)).ToArray();
		}

		public Panes(int zoomWidth, int zoomHeight, int controlWidth, int controlHeight)
		{
			p = [new Pane(zoomWidth, zoomHeight), new Pane(controlWidth, controlHeight)];
			s = $"{p[0].String},{p[1].String}";
		}
	}

	/// <summary>
	/// Lightweight class for separating the parameters of a src parameter as used in HDitem.aspx URLs
	/// </summary>
	public class Src
	{
		private readonly string src;
		private ImageFile image;

		public bool HasMultipleSkus =>
			ParseSource().Contains(' ') || ParseSource().Contains("%20") || ParseSource().Count(f => f == '/') > 1;

		public string[] Skus =>
			ParseSource()[(ParseSource().IndexOf('/') + 1)..].Replace("%20", ",").Replace(" ", ",").Split(',');

		public string Sku
		{
			get
			{
				image ??= new ImageFile(ParseSource());
				return image.Sku;
			}
		}

		public string Subscriber
		{
			get
			{
				var source = ParseSource();
				var separator = source.IndexOfAny(['/', '\\']);
				return separator > 0 ? source[..separator] : string.Empty;
			}
		}

		public int ImageNumber
		{
			get
			{
				image ??= new ImageFile(ParseSource());
				return image.ImageNumber;
			}
		}

		public string Image
		{
			get
			{
				image ??= new ImageFile(ParseSource());
				return image.Name;
			}
		}

		public string ImageWithExtension
		{
			get
			{
				image ??= new ImageFile(ParseSource());
				return image.NameWithExtension;
			}
		}

		public string String => src;
		public string Param() => "src=" + String;

		public Src(string src)
		{
			this.src = (src ?? string.Empty).Replace("%2F", "/");
		}

		private string ParseSource()
		{
			var value = src;
			var route = value.Trim();
			if (Uri.TryCreate(route, UriKind.Absolute, out var uri)
			    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
			{
				route = uri.AbsolutePath;
			}
			else
			{
				var fragment = route.IndexOf('#');
				if (fragment >= 0)
					route = route[..fragment];
				var query = route.IndexOf('?');
				if (query >= 0)
					route = route[..query];
			}

			var segments = route.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length >= 3 && string.Equals(segments[^3], "img", StringComparison.OrdinalIgnoreCase))
				return Uri.UnescapeDataString(segments[^2]) + "/" + Uri.UnescapeDataString(segments[^1]);
			return value;
		}
	}

	public class Tmp
	{
		public string Template
		{
			get => template;
			set
			{
				var val = ExtractTmpValue(value);
				Overlays = [];
				if (string.IsNullOrEmpty(val))
				{
					template = val;
					return;
				}
				var array = val.CamelSplit();
				template = array[0];
				if (array.Length > 1)
					Overlays = array.Skip(1).ToList();
			}
		}
		private string template;

		public List<string> Overlays
		{
			get => overlays;
			set
			{
				overlays = value == null || !value.Any()
					? []
					: value.Select(s => s.ToTitle()).ToList();
			}
		}
		private List<string> overlays = [];

		public string String => (Template ?? string.Empty) + string.Join("", Overlays);
		public string Param() => "tmp=" + String;

		public Tmp(string tmpString) => Template = tmpString;

		private static string ExtractTmpValue(string value)
		{
			var val = (value ?? string.Empty).Trim();
			if (val.Length == 0)
				return string.Empty;

			var queryStart = val.IndexOf('?');
			if (queryStart >= 0)
				val = val[(queryStart + 1)..];

			if (val.Contains('&') || val.StartsWith("tmp=", StringComparison.OrdinalIgnoreCase))
			{
				var query = HttpUtility.ParseQueryString(val);
				foreach (var key in query.AllKeys)
					if (string.Equals(key, "tmp", StringComparison.OrdinalIgnoreCase))
						return query[key]?.Trim() ?? string.Empty;
				return string.Empty;
			}

			return val;
		}
	}

	public class IservUrl
	{
		protected UriBuilder u { get; set; }
		public string Subscriber { get; protected set; }
		public string Protocol => u.Scheme;
		public string Host => u.Host;
		public int Port => u.Port;

		/// <summary>
		/// i.e. /iserv/Office/Login.aspx or /iserv/Office/
		/// </summary>
		public string Path => u.Path;

		public string App
		{
			get
			{
				var segments = u.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
				return segments.Length > 0 ? segments[0] : "";
			}
			set
			{
				var oldApp = App;
				u.Path = u.Path.Replace(oldApp + "/", value + "/");
			}
		}

		public string Page
		{
			get
			{
				var r = u.Path.LastIndexOf('/');
				return u.Path[r..].Contains('.') ? u.Path[(r + 1)..] : "";
			}
			set
			{
				var r = u.Path.LastIndexOf('/');
				u.Path = u.Path[r..].Contains('.')
					? u.Path[..r] + value
					: u.Path.EndsWith('/') ? u.Path + value : u.Path + "/" + value;
			}
		}

		public Uri Uri => u.Uri;

		protected void init(Uri uri)
		{
			u = new UriBuilder(uri);
		}

		public IservUrl(string uri) => init(new Uri(uri, UriKind.RelativeOrAbsolute));
		public IservUrl(Uri uri) => init(uri);
	}

	public class ServiceUrl : IservUrl
	{
		public void init(Uri uri, bool callBaseInit = true) { }

		public ServiceUrl(Uri uri) : base(uri)
		{
			init(uri, false);
		}
	}

	/// <summary>
	/// Breaking down everything a valid HDitem.aspx call contains
	/// </summary>
	public class ImageUrl : IservUrl
	{
		public Src Src { get; set; }
		public Tmp Tmp { get; set; }

		public bool isHDitemLink() => Tmp != null && Src != null && !string.IsNullOrEmpty(Src.Subscriber);

		public string Url
		{
			get
			{
				var s = "";
				if (!string.IsNullOrEmpty(Protocol))
					s += Protocol + "://";
				if (!string.IsNullOrEmpty(Host))
					s += Host;
				if (Port != 0 && Port != 80)
					s += Port.ToString();
				if (!string.IsNullOrEmpty(Path))
					s += Path;
				if (Tmp == null && Src != null)
					return s + "/" + Src.Image;     // traditional image link without subscriber and template
				return s + "?" + Src.Param() + "&" + Tmp.Param();
			}
			set => init(new Uri(new Uri("http://pic.hse24.de/"), value));
		}

		private void init(Uri uri, bool callBaseInit = true)
		{
			if (callBaseInit)
				init(uri);
			if (string.IsNullOrEmpty(u.Query))
			{
				ApplyRouteSrc();
				return;
			}

			var p = HttpUtility.ParseQueryString(u.Query);
			Tmp = p["tmp"] == null ? null : new Tmp(p["tmp"]);
			if (!string.IsNullOrWhiteSpace(p["src"]))
			{
				Src = new Src(p["src"]);
				Subscriber = Src.Subscriber;
			}
			else
			{
				ApplyRouteSrc();
			}

			#region try to get the SKU for non-HDitem links
			if (Src != null && string.IsNullOrEmpty(Src.Sku))
			{
				var regex = new Regex(@"([a-z]{2}\d{6}-\d{3}-\d{2})-(\d{1})x");
				var img = new ImageFile(Src.Image);
				if (img.ImageNumber < 0)
				{
					var match = regex.Match(img.Name);
					if (match.Success && match.Groups.Count >= 3)
					{
						img.Name = match.Groups[1].Value;
						img.ImageNumber = int.Parse(match.Groups[2].Value);
					}
				}
			}
			#endregion
		}

		private void ApplyRouteSrc()
		{
			var segments = u.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length < 2)
				return;

			var subscriber = Uri.UnescapeDataString(segments[^2]);
			var image = Uri.UnescapeDataString(segments[^1]);
			if (string.IsNullOrWhiteSpace(subscriber) || string.IsNullOrWhiteSpace(image))
				return;

			Src = new Src(subscriber + "/" + image);
			Subscriber = subscriber;
		}

		public ImageUrl(string imageUrl) : base(imageUrl)
		{
			init(new Uri(new Uri("http://pic.hse24.de/"), imageUrl), false);
		}

		/// <summary>
		/// Constructor — make sure uri is either constructed in an active context or absolute or a baseUrl is added as context
		/// </summary>
		/// <param name="uri">new Uri(new Uri(baseUrlString), relativeUrlString)</param>
		public ImageUrl(Uri uri) : base(uri)
		{
			init(uri, false);
		}
	}

	public class TwoSizes : IComparable<TwoSizes>
	{
		public float Rating { get; set; }
		public System.Drawing.Size SmallRect => smallRect;
		public System.Drawing.Size LargeRect => largeRect;
		internal System.Drawing.Size smallRect;
		internal System.Drawing.Size largeRect;

		public int CompareTo(TwoSizes other)
		{
			if (other == null) return 1;
			float f = Rating - other.Rating;
			return f < 0 ? -1 : f > 0 ? 1 : 0;
		}

		public bool Equals(TwoSizes other) =>
			SmallRect.Width == other.SmallRect.Width && SmallRect.Height == other.SmallRect.Height;

		public TwoSizes(int smallW = 0, int smallH = 0, int largeW = 0, int largeH = 0, float rating = 0F)
		{
			smallRect = new(smallW, smallH);
			largeRect = new(largeW, largeH);
			Rating = rating;
		}
	}
}
