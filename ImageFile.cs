using System;
using System.IO;
using System.Threading;
using System.IO.Compression;
using OsLib;
using RaiImage;

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
				var a = str.Split(new char[] { 'x' });
				return new Size() { Width = int.Parse(a[0]), Height = int.Parse(a[1]) };
			}
			catch (Exception) { }
			return Size.noSize;
		}
	}
	public class Size
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public override string ToString()
		{
			return Width.ToString() + "x" + Height.ToString();
		}
		public Size(System.Drawing.Size from)
		{
			Width = from.Width;
			Height = from.Height;
		}
		public Size()
		{
		}
		public static System.Drawing.Size nosize = new System.Drawing.Size(0, 0);
		public static Size noSize = new Size(nosize);
		public static System.Drawing.Size HSEmidsize = new System.Drawing.Size(1040, 1300);
		public static System.Drawing.Size HSEfullsize = new System.Drawing.Size(2080, 2600);
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
		public override string Name
		{
			get
			{
				string n = "";
				if (!string.IsNullOrEmpty(ItemId))
					n += ItemId;
				if (Color != null)
					n += "_" + Color.Code.Substring(1);
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
		public override string NameWithExtension
		{
			get { return string.IsNullOrEmpty(Name) ? string.Empty : Name + (string.IsNullOrEmpty(Ext) ? string.Empty : "." + Ext); }
		}
		public System.Drawing.Image Image { get; set; }
		public virtual string ItemId
		{
			get { return itemId; }
			set { itemId = string.IsNullOrEmpty(value) ? string.Empty : value; }
		}
		private string itemId;
		public virtual string Sku
		{
			get { return ItemId; }
			set { ItemId = value; }
		}
		public virtual string NameExt
		{
			get { return nameExt; }
			set { nameExt = string.IsNullOrEmpty(value) ? string.Empty : value; }
		}
		private string nameExt;
		public int ImageNumber
		{
			get { return imageNumber; }
			set { imageNumber = value < 0 ? NoImageNumber : value; }
		}
		protected int imageNumber = -1;
		public string TileTemplate
		{
			get { return string.IsNullOrEmpty(tileTemplate) ? string.Empty : tileTemplate; }
			set { tileTemplate = value; }
		}
		private string tileTemplate;
		public string TileNumber
		{
			get { return tileNumber; }
			set { tileNumber = string.IsNullOrEmpty(value) ? string.Empty : value; }
		}
		protected string tileNumber = string.Empty;
		public ColorInfo Color { get; set; }
		/// <summary>load Image from file into an System.Drawing.Bitmap object
		/// </summary>
		/// <param name="clone"></param>
		/// <remarks>closes the file handle asap</remarks>
		/// <returns>the new System.Drawing.Image or null</returns>
		public System.Drawing.Image FromFile(bool clone)
		{   // no thread safe version of this method necessary; 
			// does not prevent various ImageFiles to reference the same file on disk
			if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
				return null;

			string fileName = FullName;
			if (!File.Exists(fileName))
				return null;
			System.Drawing.Image img;
			int retry = 10;
			// max. 500ms delay
			while (retry > 0)
			{
				try
				{
					//img = System.Drawing.Image.FromFile(fileName);
					// Use streams, as suggested by http://nathanaeljones.com/163/20-image-resizing-pitfalls/ #5, #6
					// However, I currently think that disposing the MemoryStream will be done by the GC automatically
					MemoryStream ms = new MemoryStream();
					using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
					{
						fs.CopyTo(ms);
					}
					// fs should be closed by now
					img = System.Drawing.Image.FromStream(ms);
					if (clone)
					{
						Image = (System.Drawing.Image)img.Clone();
						MemoryStream ms2 = new MemoryStream();
						ms.CopyTo(ms2);
						Image.Tag = ms2;
					}
					else
					{
						Image = img;
						Image.Tag = ms;
					}
					retry = 0;  // means successful
				}
				catch (Exception /*e*/)
				{
					//if (Image.Tag != null)
					//    ((MemoryStream)Image.Tag).Dispose();
					Image = null;
					#region wait a little while and try again
					retry--;
					Thread.Sleep((new System.Random()).Next(50));   // max. 50ms delay / avg 250ms until timeout
					#endregion
				}
			}
			return Image;
		}
		public static string BlankToCamelCase(string filename)
		{
			if (filename.Length == 0)
				return filename;
			var array = filename.Split(new char[] { ' ', '�' }, StringSplitOptions.RemoveEmptyEntries);
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
			while (pic.EndsWith("_"))
			{
				pic = pic.Substring(0, pic.Length - 1);
				if (pic.Length == 0)
					pic = "0";
			}
			var imgFile = new ImageFile(pic);       //file.FullName);
			if (imgFile.ImageNumber == ImageFile.NoImageNumber)
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
						imgFile.ImageNumber = imgFile.ImageNumber % 100000;
					}
					else if (imgFile.ImageNumber > 10)
					{
						imgFile.ItemId += (imgFile.ImageNumber / 10).ToString();
						imgFile.ImageNumber = imgFile.ImageNumber % 10;
					}
					else imgFile.ItemId = nameof(Image); // "Image";
				}
				else if (string.IsNullOrWhiteSpace(imgFile.ItemId))
					imgFile.ItemId = "0";
				if (System.Text.RegularExpressions.Regex.IsMatch(imgFile.ItemId, "^[0-9]+$")) // numerical only => extend in the front
					while (imgFile.ItemId.Length < 4)
						imgFile.ItemId = "0" + imgFile.ItemId;
				else
				{   // extend in the back
					while (imgFile.ItemId.Length < 4)
						imgFile.ItemId += "0";
				}
			}
			if (renameFile)
			{
				var from = new RaiFile(pic);
				if (from.FullName != imgFile.FullName)
					imgFile.mv(from);
			}
			return imgFile.FullName; // file.FullName;
		}
		/// <summary>
		/// convert string representation into ImageFile representation
		/// </summary>
		/// <example>"c:/temp/kill/308024_01_200x300,4x4tile-17.tiff"</example>
		/// <remarks>new features
		/// + removes/replaces name prefixes for iphones and windows phones
		/// + removes blanks (-> caMelCase)</remarks>
		protected void Parse()
		{
			#region translate special phone and camera naming conventions - removed; RSB 20150705
			Name = BlankToCamelCase(
				Name
				.Replace("_Film", "Film_")
				.Replace("(", "")
				.Replace(")", ""));
			if (Name.ToUpper().StartsWith("WP_20"))
				Name = Name.Substring(5);
			if (Name.ToLower().StartsWith("photo-"))
				Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + "_" + Name.Substring(6);
			if (Name.ToLower().StartsWith("photo") || Name.ToLower().StartsWith("image"))
				Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + Name.Substring(5);
			if (Name.ToUpper().StartsWith("IMG") || Name.ToUpper().StartsWith("_MG"))
				Name = DateTimeOffset.UtcNow.ToString("yyMMdd") + Name.Substring(3);
			if (Name.StartsWith("20") && Name.Length > 5 && Name.Substring(0, 5).Contains('-')) // assume: u: 2008-06-15 21.15.07Z 
			{
				var fields = Name.Split(new char[] { '-', ' ', '.', ':' });
				Name = (int.Parse(fields[0]) - 2000).ToString("D2") + fields[1] + fields[2] + fields[3] + "_" + fields[4] + fields[5];
			}
			#endregion
			int j, k;
			var csvValues = base.Name.Split(new char[] { ',' });
			#region Sku, Color, ImageNumber and NameExt
			var parts = csvValues[0].Split(new char[] { '_' });
			j = parts.Length;
			imageNumber = NoImageNumber;
			Color = null;
			NameExt = string.Empty;
			if (j == 2) // Sku_Number or Sku_NameExt => Sku_Dye without Number is not allowed
			{
				if (char.IsLetter(parts[1][0]))
					NameExt = parts[1];
				else SetImageNumber(parts[1]);
			}
			else if (j == 3)    // Sku_Dye_Number or Sku_Number_NameExt
			{
				ColorInfo cInfo;
				if (parts[1].Length != 6 || (cInfo = new ColorInfo("#" + parts[1])).Color == System.Drawing.Color.Empty)    // throws FormatException
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
					NameExt = BlankToCamelCase(parts[2]); ;
				}
			}
			ItemId = parts[0]; // also sets topdir and subdir if called for an ImageTreeFile since property ItemId is virtual
			#endregion
			#region TileTemplate
			if (csvValues.Length > 1)
			{
				tileTemplate = csvValues[1];
				k = tileTemplate.IndexOf('-');
				if (k >= 0)
				{
					string tileNumberString = tileTemplate.Substring(k + 1);
					#region only keep leading digits for tileNumberString
					int i = 0;
					int len = tileNumberString.Length;
					while (i < len && char.IsDigit(tileNumberString[i]))
						i++;
					if (i < len)
						tileNumberString = tileNumberString.Remove(i);
					#endregion
					tileNumber = tileNumberString;
					tileTemplate = tileTemplate.Substring(0, k);
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
			int number;
			if (int.TryParse(s, out number))
				ImageNumber = number;
			else ImageNumber = NoImageNumber;
		}
		/// <summary>get first file that is a match in the filesystem</summary>
		/// <param name="extensions">comma seperated string with extensions</param>
		/// <param name="splitMode">explicit split mode to use for ImageTreeFile probing</param>
		/// <param name="colorInfo">null by default; will be wildcarded if null</param>
		/// <returns>false if no file exists for any passed-in extensions - extends the ImageTreeFile accordingly otherwise and returns true</returns>
		public bool ExtendToFirstExistingFile(string extensions, PathConventionType splitMode = PathConventionType.ItemIdTree8x2, ColorInfo colorInfo = null)
		{
			//try
			//{
			var itf = new ImageTreeFile(FullName, splitMode);
			itf.Color = colorInfo ?? new ColorInfo("#0DEAD0");
			itf.Ext = "*";
			var searchPattern = colorInfo == null ? itf.NameWithExtension.Replace("_0DEAD0", "*") : itf.NameWithExtension;
			string[] dirEntries = Directory.GetFileSystemEntries(Path, searchPattern);
			string[] extArray = extensions.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
							if (ImageNumber == NoImageNumber)   // BildInArbeit.tiff requested; BildInArbeit_01.tiff exists => take it
								ImageNumber = new ImageTreeFile(dirEntry, splitMode).ImageNumber;
							return true;
						}
					}
				}
			//}
			//catch (DirectoryNotFoundException) { }
			//catch (FileNotFoundException) { }
			//catch (Exception) { }
			return false;
		}
		/// <summary>
		/// Constructor that identifies what it knows and throws out the rest
		/// </summary>
		/// <param name="filename">ie c:/temp/image_01_zoom.png</param>
		public ImageFile(string filename)
			: base(filename)
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
		/*
        /// <summary>
        /// Destructor
        /// </summary>
        /// <remarks>close file handle if nobody did it before</remarks>
        ~ImageFile()
        {
           if (Image != null && Image.Tag != null)
               ((MemoryStream)Image.Tag).Close();
        }
        */
	}
	public class ImageTreeFile : ImageFile, IPathConventionFile
	{
		private readonly PathConventionType splitMode;
		private readonly int topdirLen;
		private readonly int subdirLen;

		public PathConventionType ConventionName
		{
			get { return splitMode; }
		}

		private static PathConventionType ValidateSplitMode(PathConventionType mode)
		{
			if (mode != PathConventionType.ItemIdTree8x2 &&
				mode != PathConventionType.ItemIdTree3x3 &&
				mode != PathConventionType.CanonicalByName)
				throw new ArgumentException("ImageTreeFile requires ItemIdTree8x2, ItemIdTree3x3 or CanonicalByName split mode.", nameof(mode));
			return mode;
		}

		private static void GetSplitLengths(PathConventionType mode, out int topLen, out int subLen)
		{
			if (mode == PathConventionType.ItemIdTree3x3)
			{
				topLen = 3;
				subLen = 3;
				return;
			}

			if (mode == PathConventionType.CanonicalByName)
			{
				topLen = 0;
				subLen = 0;
				return;
			}

			topLen = 8;
			subLen = 2;
		}

		public void ApplyPathConvention()
		{
			ItemId = ItemId;
		}

		/// <summary>
		/// readonly - set components to change, ie ItemId, NameExt, ...
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
		#region topdir and subdir, i.e, "20190919/2019091914/20190919145258_244.jpg"
		private string topdir;      // without seperators
		public string Topdir
		{
			get { return topdir; }
		}
		private string subdir;      // without seperators
		public string Subdir
		{
			get { return subdir; }
		}
		#endregion
		//protected System.Drawing.Size size;								// compare DimensionFromFile.Size
		//protected int zoomHseMedium(ref ImageFile from)			// zooms file using ImageMagick to mid Size and moves file to this location
		//{
		//    string fromName = Os.Escape(FullName, EscapeMode.paramEsc);
		//    int rc = (new ImageMagick()).Convert("-density 72 -quality 100 -adaptive-resize 1040x1300", fromName, Os.Escape(FullName, EscapeMode.paramEsc));
		//    if (rc == 0)
		//        size = Size.HSEmidsize;
		//    return rc;
		//}
		//protected int zoomHse(ref ImageFile from)						// zooms file using ImageMagick to full Size and moves file to this location
		//{
		//    string fromName = Os.Escape(from.FullName, EscapeMode.paramEsc);
		//    int rc = (new ImageMagick()).Convert("-density 72 -quality 100 -adaptive-resize 2080x2600", fromName, Os.Escape(FullName, EscapeMode.paramEsc));
		//    if (rc == 0)
		//        size = Size.HSEfullsize;
		//    return rc;
		//}
		//protected int dontZoom(ref ImageFile from)					// replaces zoomMedium or zoom if no change of size is wanted; uses ImageMagick
		//{
		//    string fromName = Os.Escape(from.FullName, EscapeMode.paramEsc);
		//    int rc = (new ImageMagick()).Convert("-density 72 -quality 100", fromName, Os.Escape(FullName, EscapeMode.paramEsc));
		//    if (rc == 0)
		//        size = Size.nosize;
		//    return rc;
		//}
		public string TopdirRoot => base.Path;
		public string SubdirRoot
		{
			get
			{
				if (splitMode == PathConventionType.CanonicalByName)
				{
					string p0 = base.Path;
					if (!string.IsNullOrEmpty(topdir))
						p0 += topdir + Os.DIRSEPERATOR;
					return p0;
				}
				string p = base.Path;
				if (!string.IsNullOrEmpty(topdir))
					p += topdir + Os.DIRSEPERATOR;
				return p;
			}
		}
		public override string Path
		{
			get // make sure that the result still makes sense even when get is called before first set
			{
				if (splitMode == PathConventionType.CanonicalByName)
				{
					string p0 = base.Path;
					if (!string.IsNullOrEmpty(topdir))
						p0 += topdir + Os.DIRSEPERATOR;
					return p0;
				}

				string p = base.Path;
				if (!string.IsNullOrEmpty(topdir))
					p += topdir + Os.DIRSEPERATOR;
				if (!string.IsNullOrEmpty(subdir))
					p += subdir + Os.DIRSEPERATOR;
				return p;
			}
			set // set new path but make sure that topdir and subdir are not duplicated
			{
				if (splitMode == PathConventionType.CanonicalByName)
				{
					topdir = string.IsNullOrEmpty(value) ? string.Empty : ItemId;
					subdir = null;
					string s0 = Os.DIRSEPERATOR + topdir;
					int pos0 = value.IndexOf(s0);
					if (pos0 >= 0)
						base.Path = value.Remove(pos0 + 1);
					else
						base.Path = value;
					return;
				}

				// set topdir and subdir
				topdir = string.IsNullOrEmpty(value) ? string.Empty : ItemId.Substring(0, Math.Min(ItemId.Length, topdirLen));
				#region special treatment for dos device name con
				if (topdir.Length == 3 && topdir.ToLower() == "con")
					topdir = "C0N";
				#endregion
				subdir = string.IsNullOrEmpty(value) ? string.Empty : ItemId.Substring(0, Math.Min(ItemId.Length, topdirLen + subdirLen));
				// remove subdir and topdir of new path if necessary
				string s = Os.DIRSEPERATOR + topdir + Os.DIRSEPERATOR + subdir;
				int pos = value.IndexOf(s);
				if (pos >= 0)
					base.Path = value.Remove(pos + 1);
				else base.Path = value;
			}
		}
		public override string ItemId
		{
			get { return base.ItemId; }
			set // set new itemId and redefine Path
			{
				if (splitMode == PathConventionType.CanonicalByName)
				{
					string s0 = (Os.DIRSEPERATOR + topdir).ToLower();
					int pos0 = (Path.ToLower()).IndexOf(s0);
					if (pos0 >= 0)
						Path = Path.Remove(pos0 + 1);
					topdir = string.IsNullOrEmpty(value) ? string.Empty : value;
					subdir = null;
					base.ItemId = value;
					return;
				}

				// remove subdir and topdir of old sku from path if necessary
				string s = (Os.DIRSEPERATOR + topdir + Os.DIRSEPERATOR + subdir).ToLower(); ;
				int pos = (Path.ToLower()).IndexOf(s);
				if (pos >= 0)
					Path = Path.Remove(pos + 1);
				topdir = string.IsNullOrEmpty(value) ? string.Empty : value.Substring(0, Math.Min(value.Length, topdirLen));
				#region special treatment for dos device name con
				if (topdir.Length == 3 && topdir.ToLower() == "con")
					topdir = "C0N";
				#endregion
				subdir = string.IsNullOrEmpty(value) ? string.Empty : value.Substring(0, Math.Min(value.Length, topdirLen + subdirLen));
				base.ItemId = value;
			}
		}
		public override string Sku
		{
			get { return ItemId; }
			set { ItemId = value; }
		}
		public new void mkdir()
		{
			mkdir(Path);
		}
		/// <summary>
		/// copies the file on disk identified by the current ImageTreeFile object to multiple destinations
		/// </summary>
		/// <param name="destDirs"></param>
		/// <returns></returns>
		public new bool CopyTo(string[] destDirs)
		{
			try
			{
				ImageFile dest;
				foreach (var destDir in destDirs)
				{
					dest = new ImageFile(FullName);
					dest.Path = destDir;
					dest.mkdir();
					dest.cp(this);
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}
		/// <summary>
		/// MoveToTree moves files in fromDir to a directory structure created in toDirRoot 
		/// using the file's name as names for the folders in the directory structure
		/// </summary>
		/// <param name="fromDir">original directory</param>
		/// <param name="toDirRoot">root of the new directory structure </param>
		/// <param name="splitMode">explicit split mode for destination tree calculation</param>
		/// <param name="filter"></param>
		/// <param name="remove"></param>
		/// <returns></returns>
		public static int MoveToTree(string fromDir, string toDirRoot, PathConventionType splitMode = PathConventionType.ItemIdTree8x2, string filter = "*.jpg", string remove = "")
		{
			ImageTreeFile dest;
			RaiFile source;
			int count = 0;
			foreach (var file in Directory.EnumerateFiles(new RaiFile(fromDir).FullName, filter))
			{
				dest = new ImageTreeFile(file.Replace(remove, ""), splitMode);
				dest.Path = new RaiFile(toDirRoot).FullName;
				source = new RaiFile(file);
				dest.mv(source);
				Console.WriteLine($"{source.FullName} moved to {dest.FullName}");
				count++;
			}
			return count;
		}
		/// <summary>
		/// assumes the file is a directory tree and deletes it with all files in it (depth 2)
		/// </summary>
		public void rmdir()
		{
			new RaiFile(FullName).rmdir(2); // maybe: new RaiFile(base.Path).rmdir(2)
		}
		/// <summary>
		/// Create an ImageTreeFile with its basic components as parameters
		/// </summary>
		/// <param name="name">i.e. 123456</param>
		/// <param name="path">any path including Os.DIRSEPERATOR; topdir/subdir will be inserted</param>
		/// <param name="nameExt">i.e. _01</param>
		/// <param name="ext">i.e. xml</param>
		/// <param name="splitMode">legacy 8/10 split or itemId 3/6 split</param>
		/// <remarks>special treatment for topdir CON (Windows restriction)</remarks>
		public ImageTreeFile(string name, string path, string nameExt, string ext, PathConventionType splitMode = PathConventionType.ItemIdTree8x2)
			: base(name)
		{
			this.splitMode = ValidateSplitMode(splitMode);
			GetSplitLengths(this.splitMode, out topdirLen, out subdirLen);
			Path = string.IsNullOrEmpty(path) ? null : path;
			NameExt = string.IsNullOrEmpty(nameExt) ? null : nameExt;
			Ext = string.IsNullOrEmpty(ext) ? null : ext;
			ItemId = ItemId;  // removes potential duplicates of Os.DIRSEPERATOR + topdir + Os.DIRSEPERATOR + subdir
		}
		public ImageTreeFile(string file, PathConventionType splitMode = PathConventionType.ItemIdTree8x2)
			: base(file)
		{
			this.splitMode = ValidateSplitMode(splitMode);
			GetSplitLengths(this.splitMode, out topdirLen, out subdirLen);
			//size = Size.nosize;
			ItemId = ItemId;  // removes potential duplicates of Os.DIRSEPERATOR + topdir + Os.DIRSEPERATOR + subdir
		}
	}
}
