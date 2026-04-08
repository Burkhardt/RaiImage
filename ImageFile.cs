using System;
using System.IO;
using System.Threading;
using OsLib;
using System.Collections.Generic;
namespace RaiImage
{
	public static class Extensions
	{
		public static Size Parse(this string str)
		{
			try
			{
				var a = str.Split('x');
				return new Size { Width = int.Parse(a[0]), Height = int.Parse(a[1]) };
			}
			catch (Exception) { }
			return Size.noSize;
		}
	}
	public class Size
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public override string ToString() => $"{Width}x{Height}";
		public Size(System.Drawing.Size from)
		{
			Width = from.Width;
			Height = from.Height;
		}
		public Size() { }
		public static System.Drawing.Size nosize = new(0, 0);
		public static Size noSize = new(nosize);
		public static System.Drawing.Size HSEmidsize = new(1040, 1300);
		public static System.Drawing.Size HSEfullsize = new(2080, 2600);
	}
	/// <summary>
	/// ImageFile — a file with structured naming for image assets.
	/// Implements INamingConvention to compose and parse filenames from components.
	/// </summary>
	/// <remarks>Convention: all string properties return an empty string, never null.</remarks>
	public class ImageFile : RaiFile, INamingConvention
	{
		public const int NoImageNumber = -1;
		#region naming convention
		/// <summary>
		/// Controls how the file name is composed from and parsed into components.
		/// Legacy: ItemId_Color_ImageNumber_NameExt,TileTemplate-TileNumber
		/// ItemTemplate: ItemId_TemplateName (TemplateName maps to NameExt)
		/// </summary>
		public ImageNamingConvention NamingConvention
		{
			get => namingConvention;
			set
			{
				namingConvention = value;
				ApplyNamingConvention();
			}
		}
		private ImageNamingConvention namingConvention = ImageNamingConvention.Legacy;
		/// <summary>
		/// True after the constructor has finished and the naming convention
		/// governs the Name property. During construction, Parse() runs legacy
		/// parsing regardless of the convention setting.
		/// </summary>
		protected bool conventionActive;
		public void ApplyNamingConvention()
		{
			if (!conventionActive)
				return;
			if (namingConvention == ImageNamingConvention.ItemTemplate)
				ParseItemTemplateName();
		}
		/// <summary>
		/// Parse the raw name field into ItemId and TemplateName (stored in NameExt).
		/// "1234567890_thumbnail" -> ItemId="1234567890", NameExt="thumbnail"
		/// "1234567890"           -> ItemId="1234567890", NameExt=""
		/// "abc_thumb_large"      -> ItemId="abc",        NameExt="thumb_large"
		/// </summary>
		private void ParseItemTemplateName()
		{
			var rawName = name ?? string.Empty;
			var pos = rawName.IndexOf('_');
			if (pos >= 0)
			{
				itemId = rawName[..pos];
				nameExt = rawName[(pos + 1)..];
			}
			else
			{
				itemId = rawName;
				nameExt = string.Empty;
			}
			imageNumber = NoImageNumber;
			Color = null;
			tileTemplate = string.Empty;
			tileNumber = string.Empty;
		}
		#endregion
		#region Name composition
		/// <summary>
		/// FullName: Path + Name + "." + Ext
		/// </summary>
		public override string FullName
		{
			get
			{
				string n = Path + Name;
				if (!string.IsNullOrEmpty(Ext))
					n += "." + Ext;
				return n;
			}
		}
		/// <summary>
		/// Just ItemId and ImageNumber, e.g. "308024_01"
		/// </summary>
		public string ShortName
		{
			get
			{
				string n = "";
				if (!string.IsNullOrEmpty(ItemId))
					n += ItemId;
				if (imageNumber >= 0)
					n += "_" + ImageNumber.ToString("D2");
				return n.Length > 0 ? n : base.Name;
			}
		}
		/// <summary>
		/// The composed file name (without path or extension).
		/// Composition depends on NamingConvention:
		/// Legacy:       ItemId_Color_ImageNumber_NameExt,TileTemplate-TileNumber
		/// ItemTemplate: ItemId_TemplateName (TemplateName = NameExt)
		/// </summary>
		/// <remarks>
		/// Example Legacy: 308024_0DEAD0_01_zoom,4x4tile-17
		/// Example ItemTemplate: 1234567890_thumbnail
		/// Subscribers are NOT part of the name — they come through the folder structure.
		/// </remarks>
		public override string Name
		{
			get
			{
				if (conventionActive && namingConvention == ImageNamingConvention.ItemTemplate)
				{
					var id = ItemId ?? string.Empty;
					return string.IsNullOrEmpty(nameExt) ? id : $"{id}_{nameExt}";
				}
				return ComposeLegacyName();
			}
			set
			{
				base.Name = value;
				if (conventionActive)
					ApplyNamingConvention();
			}
		}
		/// <summary>
		/// Compose the legacy format: ItemId_Color_ImageNumber_NameExt,TileTemplate-TileNumber
		/// </summary>
		private string ComposeLegacyName()
		{
			string n = "";
			if (!string.IsNullOrEmpty(ItemId))
				n += ItemId;
			if (Color != null)
				n += "_" + Color.Code[1..];
			if (imageNumber >= 0)
				n += "_" + ImageNumber.ToString("D2");
			if (!string.IsNullOrEmpty(NameExt))
				n += "_" + NameExt;
			if (!string.IsNullOrEmpty(TileTemplate))
				n += "," + TileTemplate;
			if (!string.IsNullOrEmpty(TileNumber))
				n += "-" + TileNumber;
			return n.Length > 0 ? n : base.Name;
		}
		/// <summary>
		/// Without dir structure but with "." and with extension, e.g. "123456.png"
		/// </summary>
		public override string NameWithExtension =>
			string.IsNullOrEmpty(Name) ? string.Empty : Name + (string.IsNullOrEmpty(Ext) ? string.Empty : "." + Ext);
		#endregion
		#region naming components
		public System.Drawing.Image Image { get; set; }
		public virtual string ItemId
		{
			get => itemId;
			set => itemId = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
		protected string itemId = string.Empty;
		public virtual string Sku
		{
			get => ItemId;
			set => ItemId = value;
		}
		/// <summary>
		/// Name extension / template name component.
		/// In Legacy convention: the NameExt segment after ImageNumber.
		/// In ItemTemplate convention: the TemplateName (ImageMagick rendering template).
		/// </summary>
		public virtual string NameExt
		{
			get => nameExt;
			set => nameExt = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
		protected string nameExt = string.Empty;
		/// <summary>
		/// Alias for NameExt when using the ItemTemplate naming convention.
		/// The ImageMagick rendering template name used to produce this image variant.
		/// </summary>
		public string TemplateName
		{
			get => nameExt;
			set => nameExt = value ?? string.Empty;
		}
		public int ImageNumber
		{
			get => imageNumber;
			set => imageNumber = value < 0 ? NoImageNumber : value;
		}
		protected int imageNumber = NoImageNumber;
		public string TileTemplate
		{
			get => string.IsNullOrEmpty(tileTemplate) ? string.Empty : tileTemplate;
			set => tileTemplate = value;
		}
		protected string tileTemplate = string.Empty;
		public string TileNumber
		{
			get => tileNumber;
			set => tileNumber = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
		protected string tileNumber = string.Empty;
		public ColorInfo Color { get; set; }
		#endregion
		#region image loading
		/// <summary>Load Image from file into a System.Drawing.Bitmap object.</summary>
		/// <param name="clone">when true, clones the image and stream</param>
		/// <remarks>Closes the file handle as soon as possible.</remarks>
		/// <returns>the new System.Drawing.Image or null</returns>
		public System.Drawing.Image FromFile(bool clone)
		{
			if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
				return null;
			string fileName = FullName;
			if (!File.Exists(fileName))
				return null;
			int retry = 10;
			while (retry > 0)
			{
				try
				{
					var ms = new MemoryStream();
					using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
						fs.CopyTo(ms);
					var img = System.Drawing.Image.FromStream(ms);
					if (clone)
					{
						Image = (System.Drawing.Image)img.Clone();
						var ms2 = new MemoryStream();
						ms.CopyTo(ms2);
						Image.Tag = ms2;
					}
					else
					{
						Image = img;
						Image.Tag = ms;
					}
					retry = 0;
				}
				catch (Exception)
				{
					Image = null;
					retry--;
					Thread.Sleep(new Random().Next(50));
				}
			}
			return Image;
		}
		#endregion
		#region parsing
		public static string BlankToCamelCase(string filename)
		{
			if (filename.Length == 0)
				return filename;
			var array = filename.Split([' ', '\u00b7'], StringSplitOptions.RemoveEmptyEntries);
			for (int i = 1; i < array.Length; i++)
			{
				var x = array[i].ToCharArray();
				x[0] = char.ToUpper(array[i][0]);
				array[i] = new string(x);
			}
			return string.Join("", array);
		}
		/// <summary>
		/// Parse the raw name string into structured components (legacy convention).
		/// Handles camera/phone naming patterns, color codes, image numbers,
		/// tile templates and tile numbers.
		/// </summary>
		/// <example>"c:/temp/kill/308024_01_200x300,4x4tile-17.tiff"</example>
		protected void Parse()
		{
			#region translate special phone and camera naming conventions
			base.Name = BlankToCamelCase(
				base.Name
				.Replace("_Film", "Film_")
				.Replace("(", "")
				.Replace(")", ""));
			var currentName = base.Name;
			if (currentName.ToUpper().StartsWith("WP_20"))
				base.Name = currentName[5..];
			currentName = base.Name;
			if (currentName.ToLower().StartsWith("photo-"))
				base.Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + "_" + currentName[6..];
			currentName = base.Name;
			if (currentName.ToLower().StartsWith("photo") || currentName.ToLower().StartsWith("image"))
				base.Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + currentName[5..];
			currentName = base.Name;
			if (currentName.ToUpper().StartsWith("IMG") || currentName.ToUpper().StartsWith("_MG"))
				base.Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + currentName[3..];
			currentName = base.Name;
			if (currentName.StartsWith("20") && currentName.Length > 5 && currentName[..5].Contains('-'))
			{
				var fields = currentName.Split(['-', ' ', '.', ':']);
				base.Name = (int.Parse(fields[0]) - 2000).ToString("D2") + fields[1] + fields[2] + fields[3] + "_" + fields[4] + fields[5];
			}
			#endregion
			var csvValues = name.Split(',');
			#region Sku, Color, ImageNumber and NameExt
			var parts = csvValues[0].Split('_');
			int j = parts.Length;
			imageNumber = NoImageNumber;
			Color = null;
			nameExt = string.Empty;
			if (j == 2) // Sku_Number or Sku_NameExt
			{
				if (char.IsLetter(parts[1][0]))
					nameExt = parts[1];
				else SetImageNumber(parts[1]);
			}
			else if (j == 3) // Sku_Dye_Number or Sku_Number_NameExt
			{
				ColorInfo cInfo;
				if (parts[1].Length != 6 || (cInfo = new ColorInfo("#" + parts[1])).Color == System.Drawing.Color.Empty)
				{
					SetImageNumber(parts[1]);
					nameExt = BlankToCamelCase(parts[2]);
				}
				else
				{
					Color = cInfo;
					SetImageNumber(parts[2]);
					nameExt = string.Empty;
				}
			}
			else if (j >= 4) // Sku_Dye_Number_NameExt or Sku_Number_NameExt_SomeOtherStuff
			{
				ColorInfo cInfo = null;
				bool correctLength = parts[1].Length == 6;
				if (correctLength)
					cInfo = new ColorInfo("#" + parts[1]);
				if (correctLength && cInfo.Color != System.Drawing.Color.Empty)
				{
					Color = cInfo;
					SetImageNumber(parts[2]);
					nameExt = BlankToCamelCase(parts[3]);
				}
				else
				{
					Color = null;
					SetImageNumber(parts[1]);
					nameExt = BlankToCamelCase(parts[2]);
				}
			}
			ItemId = parts[0];
			#endregion
			#region TileTemplate
			if (csvValues.Length > 1)
			{
				tileTemplate = csvValues[1];
				int k = tileTemplate.IndexOf('-');
				if (k >= 0)
				{
					string tileNumberString = tileTemplate[(k + 1)..];
					int i = 0;
					while (i < tileNumberString.Length && char.IsDigit(tileNumberString[i]))
						i++;
					if (i < tileNumberString.Length)
						tileNumberString = tileNumberString.Remove(i);
					tileNumber = tileNumberString;
					tileTemplate = tileTemplate[..k];
				}
				else
				{
					tileNumber = string.Empty;
				}
			}
			#endregion
		}
		public void SetImageNumber(string s)
		{
			ImageNumber = int.TryParse(s, out int number) ? number : NoImageNumber;
		}
		#endregion
		/// <summary>Get first file that is a match in the filesystem.</summary>
		/// <param name="extensions">comma separated string with extensions</param>
		/// <param name="splitMode">explicit split mode to use for ImageTreeFile probing</param>
		/// <param name="colorInfo">null by default; will be wildcarded if null</param>
		/// <returns>false if no file exists for any passed-in extensions</returns>
		public bool ExtendToFirstExistingFile(string extensions, PathConventionType splitMode = PathConventionType.ItemIdTree8x2, ColorInfo colorInfo = null)
		{
			var itf = new ImageTreeFile(FullName, splitMode);
			itf.Color = colorInfo ?? new ColorInfo("#0DEAD0");
			itf.Ext = "*";
			var searchPattern = colorInfo == null ? itf.NameWithExtension.Replace("_0DEAD0", "*") : itf.NameWithExtension;
			string[] dirEntries = Directory.GetFileSystemEntries(itf.SubdirRoot.ToString(), searchPattern);
			string[] extArray = extensions.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
			if (extArray.Length > 0)
				foreach (var dirEntry in dirEntries)
				{
					itf = new ImageTreeFile(dirEntry, splitMode);
					foreach (string extension in extArray)
					{
						if (extension == itf.Ext)
						{
							Ext = extension;
							Color = itf.Color;
							if (ImageNumber == NoImageNumber)
								ImageNumber = new ImageTreeFile(dirEntry, splitMode).ImageNumber;
							return true;
						}
					}
				}
			return false;
		}
		#region constructors
		/// <summary>
		/// Construct from a full file path string. Parses the filename into
		/// structured components. Convention defaults to Legacy.
		/// </summary>
		public ImageFile(string filename,
			ImageNamingConvention naming = ImageNamingConvention.Legacy)
			: base(filename)
		{
			Image = null;
			itemId = string.Empty;
			nameExt = string.Empty;
			tileTemplate = string.Empty;
			Parse();
			namingConvention = naming;
			conventionActive = true;
			ApplyNamingConvention();
		}
		public ImageFile(string name, string path, string nameExt, string ext)
			: this(new RaiPath(path), name, nameExt, ext)
		{
		}
		public ImageFile(RaiPath path, string name, string nameExt, string ext)
			: base(path, $"{name}_{nameExt}", ext)
		{
			Image = null;
			itemId = string.Empty;
			this.nameExt = string.Empty;
			tileTemplate = string.Empty;
			Parse();
			conventionActive = true;
		}
		#endregion
	}
	/// <summary>
	/// ImageTreeFile — an ImageFile stored in a directory tree derived from ItemId.
	/// Implements IPathConvention for the directory layout.
	/// Inherits INamingConvention from ImageFile.
	/// </summary>
	public class ImageTreeFile : ImageFile, IPathConvention
	{
		#region path convention — directory tree layout
		public PathConventionType Convention
		{
			get { return field; }
			set
			{
				field = value;
				ApplyPathConvention();
			}
		} = PathConventionType.ItemIdTree8x2;
		public void ApplyPathConvention()
		{
			var (tLen, sLen) = ItemTreePath.ConventionSplit(Convention, ItemId);
			var id = ItemId ?? string.Empty;
			// strip any existing topdir/subdir segments from Path
			if (id.Length > 0)
			{
				var top = ItemTreePath.SanitizeSegment(id[..Math.Min(id.Length, tLen)]);
				var sub = sLen > 0
					? ItemTreePath.SanitizeSegment(id[..Math.Min(id.Length, tLen + sLen)])
					: string.Empty;
				var marker = string.IsNullOrEmpty(sub)
					? Os.DIR + top
					: Os.DIR + top + Os.DIR + sub;
				var pathStr = base.Path.ToString();
				var pos = pathStr.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
				if (pos >= 0)
					base.Path = new RaiPath(pathStr.Remove(pos + 1));
			}
			topdir = id.Length > 0
				? new RaiRelPath(ItemTreePath.SanitizeSegment(id[..Math.Min(id.Length, tLen)]))
				: new RaiRelPath();
			subdir = sLen > 0 && id.Length > 0
				? new RaiRelPath(ItemTreePath.SanitizeSegment(id[..Math.Min(id.Length, tLen + sLen)]))
				: new RaiRelPath();
		}
		#endregion
		#region composed path — SubdirRoot + Name + Ext
		/// <summary>
		/// FullName uses SubdirRoot (Path + topdir + subdir) instead of just Path.
		/// </summary>
		public override string FullName
		{
			get
			{
				string n = SubdirRoot + Name;
				if (!string.IsNullOrEmpty(Ext))
					n += "." + Ext;
				return n;
			}
		}
		private RaiRelPath topdir = new RaiRelPath();
		public RaiRelPath Topdir => topdir;
		private RaiRelPath subdir = new RaiRelPath();
		public RaiRelPath Subdir => subdir;
		/// <summary>Path + topdir/</summary>
		public RaiPath TopdirRoot => topdir.IsEmpty ? new RaiPath(base.Path.ToString()) : base.Path / topdir;
		/// <summary>Path + topdir/ + subdir/</summary>
		public RaiPath SubdirRoot => subdir.IsEmpty ? TopdirRoot : base.Path / topdir / subdir;
		/// <summary>
		/// Path is the root without topdir/subdir.
		/// Use TopdirRoot or SubdirRoot for the full tree path.
		/// </summary>
		public override RaiPath Path
		{
			get
			{
				ApplyPathConvention();
				return base.Path;
			}
			set
			{
				base.Path = value;
				ApplyPathConvention();
			}
		}
		#endregion
		#region zoom — resize via ImageMagick
		protected System.Drawing.Size size;
		protected int zoomHseMedium(ref ImageFile from)
		{
			string fromName = Os.Escape(from.FullName, EscapeMode.paramEsc);
			int rc = new ImageMagick().Convert("-density 72 -quality 100 -adaptive-resize 1040x1300", fromName, Os.Escape(FullName, EscapeMode.paramEsc));
			if (rc == 0)
				size = Size.HSEmidsize;
			return rc;
		}
		protected int zoomHse(ref ImageFile from)
		{
			string fromName = Os.Escape(from.FullName, EscapeMode.paramEsc);
			int rc = new ImageMagick().Convert("-density 72 -quality 100 -adaptive-resize 2080x2600", fromName, Os.Escape(FullName, EscapeMode.paramEsc));
			if (rc == 0)
				size = Size.HSEfullsize;
			return rc;
		}
		protected int dontZoom(ref ImageFile from)
		{
			string fromName = Os.Escape(from.FullName, EscapeMode.paramEsc);
			int rc = new ImageMagick().Convert("-density 72 -quality 100", fromName, Os.Escape(FullName, EscapeMode.paramEsc));
			if (rc == 0)
				size = Size.nosize;
			return rc;
		}
		#endregion
		public override string ItemId
		{
			get => base.ItemId;
			set
			{
				base.ItemId = value;
				ApplyPathConvention();
			}
		}
		public override string Sku
		{
			get => ItemId;
			set => ItemId = value;
		}
		public new void mkdir() => SubdirRoot.mkdir();
		/// <summary>
		/// Copy the file to multiple destinations, preserving the tree structure.
		/// </summary>
		public new bool CopyTo(List<RaiPath> destDirs)
		{
			try
			{
				foreach (var destDir in destDirs)
				{
					var dest = new ImageTreeFile(FullName, Convention);
					dest.Path = destDir;
					dest.mkdir();
					dest.cp(this);
				}
			}
			catch (Exception) { return false; }
			return true;
		}
		/// <summary>
		/// Move files from fromDir into a directory tree structure in toDirRoot.
		/// </summary>
		public static int MoveToTree(string fromDir, string toDirRoot, PathConventionType splitMode = PathConventionType.ItemIdTree8x2, string filter = "*.jpg", string remove = "")
		{
			int count = 0;
			foreach (var source in new RaiPath(fromDir).EnumerateFiles(filter))
			{
				var dest = new ImageTreeFile(source.FullName.Replace(remove, ""), splitMode);
				dest.Path = new RaiPath(toDirRoot);
				dest.mv(source);
				Console.WriteLine($"{source.FullName} moved to {dest.FullName}");
				count++;
			}
			return count;
		}
		/// <summary>
		/// Deletes the directory tree (depth 2) for this file.
		/// </summary>
		public void rmdir() => SubdirRoot.rmdir(2, true);
		#region constructors
		/// <summary>
		/// Construct from a full file path string.
		/// Parse() runs the legacy parser, then the naming convention is applied.
		/// </summary>
		public ImageTreeFile(string file,
			PathConventionType splitMode = PathConventionType.ItemIdTree8x2,
			ImageNamingConvention naming = ImageNamingConvention.Legacy)
			: base(file, naming)
		{
			Convention = splitMode;
		}
		/// <summary>
		/// Construct from structured components using the ItemTemplate naming convention.
		/// The directory tree is built from the itemId according to the path convention.
		/// </summary>
		public ImageTreeFile(RaiPath rootPath, string itemId, string templateName, string ext,
			PathConventionType pathConvention = PathConventionType.ItemIdTree8x2)
			: this(rootPath.FullPath + itemId
				+ (string.IsNullOrEmpty(templateName) ? "" : "_" + templateName)
				+ "." + ext,
				pathConvention, ImageNamingConvention.ItemTemplate)
		{
		}
		/// <summary>
		/// Construct from legacy components (name, path, nameExt, ext).
		/// </summary>
		public ImageTreeFile(string itemName, RaiPath path, string nameExt, string ext,
			PathConventionType splitMode = PathConventionType.ItemIdTree8x2,
			ImageNamingConvention naming = ImageNamingConvention.Legacy)
			: base(itemName, naming)
		{
			base.Path = path;
			NameExt = string.IsNullOrEmpty(nameExt) ? string.Empty : nameExt;
			Ext = string.IsNullOrEmpty(ext) ? string.Empty : ext;
			Convention = splitMode;
		}
		#endregion
	}
}
