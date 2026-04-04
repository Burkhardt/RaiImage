using System;
using System.IO;
using System.Threading;
using OsLib;
using System.Collections.Generic;

// release notes:
// new min length = 1 for an ImageTreeFile, i.e.:
// 1.jpg => /1/1/1.jpg
// 12345678 => /123/123456/12345678.jpg

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
	/// ImageFile class
	/// </summary>
	/// <remarks>convention: all string Properties return an empty string and never null; instead of s == null use s.Length == 0</remarks>
	public class ImageFile : RaiFile
	{
		public const int NoImageNumber = -1;

		/// <summary>
		/// readonly - set components to change, ie Sku, NameExt, ...
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
		/// Just Sku and Image number
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
		/// readonly - set components to change, ie Sku, NameExt, ...
		/// </summary>
		/// <remarks>
		/// Convention: ItemId_Color_ImageNumber_NameExt,TileTemplate-TileNumber
		/// Example: 308024_0DEAD0_01_zoom,4x4tile-17
		/// Subscribers are NOT part of the name — they come through the folder structure.
		/// </remarks>
		public override string Name
		{
			get
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
		}

		/// <summary>
		/// without dir structure but with "." and with extension, ie 123456.png
		/// </summary>
		public override string NameWithExtension =>
			string.IsNullOrEmpty(Name) ? string.Empty : Name + (string.IsNullOrEmpty(Ext) ? string.Empty : "." + Ext);

		public System.Drawing.Image Image { get; set; }

		public virtual string ItemId
		{
			get => itemId;
			set => itemId = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
		private string itemId;

		public virtual string Sku
		{
			get => ItemId;
			set => ItemId = value;
		}

		public virtual string NameExt
		{
			get => nameExt;
			set => nameExt = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
		private string nameExt;

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
		private string tileTemplate;

		public string TileNumber
		{
			get => tileNumber;
			set => tileNumber = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
		protected string tileNumber = string.Empty;

		public ColorInfo Color { get; set; }

		/// <summary>load Image from file into a System.Drawing.Bitmap object</summary>
		/// <param name="clone"></param>
		/// <remarks>closes the file handle asap</remarks>
		/// <returns>the new System.Drawing.Image or null</returns>
		// TODO Rainer — FromFile uses File.Exists, File.Open directly; consider RaiFile abstractions
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
					retry = 0; // successful
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

		public static string BlankToCamelCase(string filename)
		{
			if (filename.Length == 0)
				return filename;
			var array = filename.Split([' ', '·'], StringSplitOptions.RemoveEmptyEntries);
			for (int i = 1; i < array.Length; i++)
			{
				var x = array[i].ToCharArray();
				x[0] = char.ToUpper(array[i][0]);
				array[i] = new string(x);
			}
			return string.Join("", array);
		}

		public static string EasyFileName(string pic, bool renameFile = false)
		{
			while (pic.EndsWith('_'))
			{
				pic = pic[..^1];
				if (pic.Length == 0)
					pic = "0";
			}
			var imgFile = new ImageFile(pic);
			if (imgFile.ImageNumber == NoImageNumber)
				imgFile.ImageNumber = 1;
			if (string.IsNullOrEmpty(imgFile.Ext))
				imgFile.Ext = "jpg";
			if (imgFile.ItemId.Length < 4)
			{
				if (imgFile.ItemId.ToLower() == "img")
				{
					if (imgFile.ImageNumber > 100000)
					{
						imgFile.ItemId = (imgFile.ImageNumber / 100000).ToString();
						imgFile.ImageNumber %= 100000;
					}
					else if (imgFile.ImageNumber > 10)
					{
						imgFile.ItemId += (imgFile.ImageNumber / 10).ToString();
						imgFile.ImageNumber %= 10;
					}
					else imgFile.ItemId = nameof(Image);
				}
				else if (string.IsNullOrWhiteSpace(imgFile.ItemId))
					imgFile.ItemId = "0";

				if (System.Text.RegularExpressions.Regex.IsMatch(imgFile.ItemId, "^[0-9]+$"))
					while (imgFile.ItemId.Length < 4)
						imgFile.ItemId = "0" + imgFile.ItemId;
				else
					while (imgFile.ItemId.Length < 4)
						imgFile.ItemId += "0";
			}
			if (renameFile)
			{
				var from = new RaiFile(pic);
				if (from.FullName != imgFile.FullName)
					imgFile.mv(from);
			}
			return imgFile.FullName;
		}

		/// <summary>
		/// convert string representation into ImageFile representation
		/// </summary>
		/// <example>"c:/temp/kill/308024_01_200x300,4x4tile-17.tiff"</example>
		/// <remarks>removes/replaces name prefixes for phones and cameras, removes blanks (-> camelCase)</remarks>
		protected void Parse()
		{
			#region translate special phone and camera naming conventions
			Name = BlankToCamelCase(
				Name
				.Replace("_Film", "Film_")
				.Replace("(", "")
				.Replace(")", ""));
			if (Name.ToUpper().StartsWith("WP_20"))
				Name = Name[5..];
			if (Name.ToLower().StartsWith("photo-"))
				Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + "_" + Name[6..];
			if (Name.ToLower().StartsWith("photo") || Name.ToLower().StartsWith("image"))
				Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + Name[5..];
			if (Name.ToUpper().StartsWith("IMG") || Name.ToUpper().StartsWith("_MG"))
				Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + Name[3..];
			if (Name.StartsWith("20") && Name.Length > 5 && Name[..5].Contains('-'))
			{
				var fields = Name.Split(['-', ' ', '.', ':']);
				Name = (int.Parse(fields[0]) - 2000).ToString("D2") + fields[1] + fields[2] + fields[3] + "_" + fields[4] + fields[5];
			}
			#endregion

			var csvValues = base.Name.Split(',');

			#region Sku, Color, ImageNumber and NameExt
			var parts = csvValues[0].Split('_');
			int j = parts.Length;
			imageNumber = NoImageNumber;
			Color = null;
			NameExt = string.Empty;

			if (j == 2) // Sku_Number or Sku_NameExt => Sku_Dye without Number is not allowed
			{
				if (char.IsLetter(parts[1][0]))
					NameExt = parts[1];
				else SetImageNumber(parts[1]);
			}
			else if (j == 3) // Sku_Dye_Number or Sku_Number_NameExt
			{
				ColorInfo cInfo;
				if (parts[1].Length != 6 || (cInfo = new ColorInfo("#" + parts[1])).Color == System.Drawing.Color.Empty)
				{
					SetImageNumber(parts[1]);
					NameExt = BlankToCamelCase(parts[2]);
				}
				else
				{
					Color = cInfo;
					SetImageNumber(parts[2]);
					NameExt = null;
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
					NameExt = BlankToCamelCase(parts[3]);
				}
				else
				{
					Color = null;
					SetImageNumber(parts[1]);
					NameExt = BlankToCamelCase(parts[2]);
				}
			}
			ItemId = parts[0]; // also sets topdir and subdir if called for an ImageTreeFile since property ItemId is virtual
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

		/// <summary>get first file that is a match in the filesystem</summary>
		/// <param name="extensions">comma separated string with extensions</param>
		/// <param name="splitMode">explicit split mode to use for ImageTreeFile probing</param>
		/// <param name="colorInfo">null by default; will be wildcarded if null</param>
		/// <returns>false if no file exists for any passed-in extensions - extends the ImageTreeFile accordingly otherwise and returns true</returns>
		// TODO Rainer — Directory.GetFileSystemEntries; consider RaiPath.GetFiles or similar OsLib abstraction
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

		/// <summary>
		/// Constructor that identifies what it knows and throws out the rest
		/// </summary>
		/// <param name="filename">ie c:/temp/image_01_zoom.png</param>
		public ImageFile(string filename) : base(filename)
		{
			Image = null;
			ItemId = null;
			NameExt = null;
			TileTemplate = null;
			Parse();
		}

		public ImageFile(string name, string path, string nameExt, string ext)
			: base($"{path}{name}_{nameExt}.{ext}")
		{
			Image = null;
			ItemId = null;
			NameExt = null;
			TileTemplate = null;
			Parse();
		}
	}

	public class ImageTreeFile : ImageFile, IPathConvention
	{
		public PathConventionType Convention {
			get { return field; }
			set
			{
				field = value;
				ApplyPathConvention();
			}
		} = PathConventionType.ItemIdTree8x2;

		public void ApplyPathConvention()
		{
			var (tLen, sLen) = ItemTreePath.GetSplit(Convention, ItemId);

			// strip any old topdir/subdir segments from Path
			if (!string.IsNullOrEmpty(topdir))
			{
				string old = (Os.DIR + topdir + (string.IsNullOrEmpty(subdir) ? "" : Os.DIR + subdir)).ToLower();
				var path = base.Path.ToString();
				int pos = path.ToLower().IndexOf(old);
				if (pos >= 0)
					base.Path = new RaiPath(path.Remove(pos + 1));
			}

			var id = ItemId ?? string.Empty;

			topdir = id.Length > 0 ? id[..Math.Min(id.Length, tLen)] : string.Empty;
			// subdir is cumulative: first (tLen + sLen) chars of ItemId, so it always starts with topdir
			subdir = sLen > 0 && id.Length > 0 ? id[..Math.Min(id.Length, tLen + sLen)] : string.Empty;

			// DOS reserved device name — "con" as a directory kills Windows
			topdir = ItemTreePath.SanitizeSegment(topdir);
			subdir = ItemTreePath.SanitizeSegment(subdir);
		}

		/// <summary>
		/// FullName uses SubdirRoot (Path + topdir + subdir) to build the complete file path
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

		#region topdir and subdir, i.e. "20190919/2019091914/20190919145258_244.jpg"
		private string topdir;
		public string Topdir => topdir;
		private string subdir;
		public string Subdir => subdir;
		#endregion

		/// <summary>Path + topdir/</summary>
		public RaiPath TopdirRoot => base.Path / topdir;

		/// <summary>Path + topdir/ + subdir/</summary>
		public RaiPath SubdirRoot => string.IsNullOrEmpty(subdir) ? TopdirRoot : base.Path / topdir / subdir;

		/// <summary>
		/// Path is the root without topdir/subdir — use TopdirRoot or SubdirRoot for the full tree path.
		/// The getter ensures ApplyPathConvention has run; the setter strips any embedded topdir/subdir.
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
		/// copies the file on disk to multiple destinations, preserving the tree structure
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
		/// MoveToTree moves files in fromDir to a directory structure created in toDirRoot
		/// using the file's name as names for the folders in the directory structure
		/// </summary>
		// TODO Rainer — Directory.EnumerateFiles; consider RaiPath.GetFiles
		public static int MoveToTree(string fromDir, string toDirRoot, PathConventionType splitMode = PathConventionType.ItemIdTree8x2, string filter = "*.jpg", string remove = "")
		{
			int count = 0;
			foreach (var source in new RaiPath(fromDir).GetFiles(filter))
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
		/// Deletes the directory tree (depth 2) for this file
		/// </summary>
		public void rmdir() => SubdirRoot.rmdir(2, true);

		/// <summary>
		/// Create an ImageTreeFile with its basic components as parameters
		/// </summary>
		/// <param name="name">i.e. 123456</param>
		/// <param name="path">any path including Os.DIRSEPERATOR; topdir/subdir will be inserted</param>
		/// <param name="nameExt">i.e. _01</param>
		/// <param name="ext">i.e. xml</param>
		/// <param name="splitMode">how to split the ItemId for naming of topdir and subdir</param>
		public ImageTreeFile(string name, RaiPath path, string nameExt, string ext, PathConventionType splitMode = PathConventionType.ItemIdTree8x2)
			: base(name)
		{
			base.Path = path;
			NameExt = string.IsNullOrEmpty(nameExt) ? null : nameExt;
			Ext = string.IsNullOrEmpty(ext) ? null : ext;
			Convention = splitMode; // sets Split, calls ApplyPathConvention
		}

		public ImageTreeFile(string file, PathConventionType splitMode = PathConventionType.ItemIdTree8x2)
			: base(file)
		{
			Convention = splitMode; // sets Split, calls ApplyPathConvention
		}
	}
}
