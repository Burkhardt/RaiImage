using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using OsLib;

namespace RaiImage
{
	public class ImageMagick
	{
		// Hard break: use ImageMagick 7+ unified CLI (magick) with subcommands.
		public static RaiPath ImPath = null;
		public static string MagickCommand = "magick";
		// External tools are executed through RaiSystem with path-agnostic command names.
		public static string OptiPngCommand = "optipng";
		public static string JpegTranCommand = "jpegtran";
		public static string JpegTranOptions = "-optimize -progressive";
		private string callString;
		private string message;
		public string Message
		{
			get { return message; }
		}

		private static string ResolveMagickExecutable()
		{
			if (ImPath == null || string.IsNullOrWhiteSpace(ImPath.Path))
				return MagickCommand;
			var cmd = new RaiFile(ImPath, MagickCommand);
			return cmd.FullName;
		}

		private static string BuildMagickArguments(string subcommand, string args)
		{
			return $"{subcommand} {args}".Trim();
		}

		private static ImageMagickCommand CreateCommand()
		{
			return new ImageMagickCommand(ImPath, MagickCommand);
		}

		private RaiSystemResult RunMagickSubcommand(string subcommand, string args)
		{
			var result = CreateCommand().RunSubcommand(subcommand, args);
			callString = result.CommandLine;
			message = result.Output;
			return result;
		}

		private RaiSystem CreateMagickCall(string subcommand, string args)
		{
			var executable = ResolveMagickExecutable();
			var magickArgs = BuildMagickArguments(subcommand, args);
			callString = string.IsNullOrWhiteSpace(magickArgs) ? executable : $"{executable} {magickArgs}";
			return new RaiSystem(executable, magickArgs);
		}

		public ImageMagick()
		{
			if (ImPath != null && !string.IsNullOrWhiteSpace(ImPath.Path))
			{
				var exe = new RaiFile(ResolveMagickExecutable());
				if (!exe.Exists())
					throw new FileNotFoundException("ImageMagick (magick CLI) must be installed in ImPath.", exe.FullName);
			}
			callString = "";
		}
		// todo RSB use Exec option that logs to windows log file
		public int Convert(string commandline)
		{
			return RunMagickSubcommand("convert", commandline).ExitCode;
		}
		public int Convert(string options, string from, string to)
		{
			message = "";
			int exitCode = 0;
			bool zip = to.EndsWith(".zip");
			if (zip)
				to = to.Substring(0, to.Length - 4);
			exitCode = RunMagickSubcommand("convert", options + " " + Os.EscapeParam(from) + " " + Os.EscapeParam(to)).ExitCode;
			if (exitCode != 0 && message.Contains("Permission denied") && OperatingSystem.IsWindows())
			{
				try
				{
					var fiIntern = new FileInfo(Path.GetFullPath(from));
					var fsec = fiIntern.GetAccessControl();
					IdentityReference currentIdentity = new NTAccount(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
					fsec.SetOwner(currentIdentity);
					var permissions = new FileSystemAccessRule(currentIdentity, FileSystemRights.ReadAndExecute, AccessControlType.Allow);
					fsec.AddAccessRule(permissions);
					fiIntern.SetAccessControl(fsec);
					// try it again
					exitCode = RunMagickSubcommand("convert", options + " " + Os.EscapeParam(from) + " " + Os.EscapeParam(to)).ExitCode;
				}
				catch (Exception)
				{
				}
			}
			if (zip)
			{
				var zipFile = new RaiFile(to).Zip();
				if (zipFile == null)
					exitCode += 1;
			}
			return exitCode;
		}
		public int Convert(string options, string from, string to, bool OptionsInTheMiddle)
		{
			message = "";
			if (!OptionsInTheMiddle)
				return Convert(options, from, to);
			return RunMagickSubcommand("convert", Os.EscapeParam(from) + " " + options + " " + Os.EscapeParam(to)).ExitCode;
		}
		public int Mogrify(string commandline)
		{
			return RunMagickSubcommand("mogrify", commandline).ExitCode;
		}
		public int Mogrify(string options, string file)
		{
			message = "";
			return RunMagickSubcommand("mogrify", options + " " + Os.EscapeParam(file)).ExitCode;
		}
		public int Composite(string commandline)
		{
			return RunMagickSubcommand("composite", commandline).ExitCode;
		}
		// e.g.: Composite("-gravity SouthWest", from.FullName, EscapeMode.paramEsc), to.FullName, EscapeMode.paramEsc));
		public int Composite(string options, string overlay, string to)
		{
			message = "";
			string dest = Os.EscapeParam(to);
			return RunMagickSubcommand("composite", options + " " + Os.EscapeParam(overlay) + " " + dest + " -matte " + dest).ExitCode;
		}
		// e.g.: composite -dissolve 75 minus04percent60x60.png -gravity South rect.png -matte minus04percent60x60.jpg
		public int Composite(string options, string overlay, string image, string target)
		{
			message = "";
			return RunMagickSubcommand("composite", options + " " + Os.EscapeParam(overlay) + " " + Os.EscapeParam(image) + " -matte " + Os.EscapeParam(target)).ExitCode;
		}
		public int Identify(string options, string image, ref string result)
		{
			var identify = CreateCommand().RunSubcommand("identify", options + " " + Os.EscapeParam(image));
			result = identify.Output;
			return identify.ExitCode;
		}
		public bool EmptyForm(ImageFile imgFile, int imageWidth, int imageHeight, string drawString)
		{
			//ImageFile imgFile = new ImageFile(imageFileName);
			var tempFile = new ImageFile(Path.GetTempPath() + "i" + DateTimeOffset.UtcNow.UtcTicks.ToString("x") + ".png");
			// example: convert -size 180x225 xc:white -fill 292990_01_Gallery.png -draw "circle 30,110 32,82" -fuzz 5% -trim CircleW.png 
			string msg = RunMagickSubcommand("convert", "-size " + imageWidth + "x" + imageHeight +
				" xc:white -fill " + Os.Escape(imgFile.FullName, EscapeMode.paramEsc) +
				" -draw \"" + drawString + "\" -fuzz 50% -trim " + Os.Escape(tempFile.FullName, EscapeMode.paramEsc)).Output;
			try
			{
				tempFile.rm();
			}
			catch (Exception)
			{
			}
			if (msg.Contains("geometry does not contain image"))
				return true;
			return false;
		}
		/// <summary>
		/// Create a histogram for an image file
		/// </summary>
		/// <param name="colorTableFile">use this colortable</param>
		/// <param name="fromName">original image file to create the histogram for</param>
		/// <param name="coreName">create the coreFile here (temporary)</param>
		/// <param name="destName">full name of histogram file to create</param>
		/// <returns>0 if successful</returns>
		public int CreateHistogram(string colorTableFile, string fromName, string coreName, string destName)
		{
			int ret = (new ImageMagick()).Convert("-gravity Center -crop 32x40+0+0 +repage +dither -map " + colorTableFile, fromName, coreName);
			ret += RunMagickSubcommand("convert", "-format %c " + coreName + " -colors 32 -depth 8 histogram:info:" + destName).ExitCode;
			#region sort resulting file
			// cannot use Histogram here because of module dependency - use it to read it and sort it in memory after reading
			//TextFile tf = new TextFile(destName);	// not helping - wrong order: 14, 1258, 4, 2, 2; use HistogramFile from HDitem.Image.Histogram to read the file when using the histogram
			//tf.Sort(true);
			#endregion
			return ret;
		}
		public int Histogram(string imageFile, string histogramFileName)
		{
			string commandline = " -format %c " + Os.Escape(imageFile, EscapeMode.paramEsc) + " -colors 32 -depth 8 histogram:info:" + Os.Escape(histogramFileName, EscapeMode.paramEsc);
			return RunMagickSubcommand("convert", commandline).ExitCode;
		}
		/// <summary>
		/// Optipng is an external program but included here as if it were a part of Imagemagick
		/// </summary>
		/// <param name="imgFileName"></param>
		/// <remarks>http://optipng.sourceforge.net/</remarks>
		/// <remarks>http://prdownloads.sourceforge.net/optipng/optipng-0.7.5-win32.zip?download</remarks>
		/// <returns>0 if ok, sth else otherwise => get error message from Message property</returns>
		public int OptiPng(string imgFileName)
		{
			int result = 0;
			var image = new ImageFile(imgFileName);
			var originalFile = new RaiFile(imgFileName);
			if (image.Ext != "png")
			{
				result = Mogrify("-format png", image.FullName);
				if (result == 0)
				{
					originalFile.rm(); // mogrify -format creates a copy and leaves the original file there => remove it
					image.Ext = "png";
				}
			}
			if (result == 0)
			{
				var optiPng = new RaiSystem(OptiPngCommand, Os.EscapeParam(image.FullName));
				result = optiPng.Exec(out message);
			}
			return result;
		}
		/// <summary>
		/// JpegTran is an external program but included here as if it were a part of Imagemagick
		/// </summary>
		/// <param name="imgFileName"></param>
		/// <returns>0 if ok, sth else otherwise => get error message from Message property</returns>
		public int JpegTran(string imgFileName)
		{
			int result = 0;
			var image = new ImageFile(imgFileName);
			var originalFile = new RaiFile(imgFileName);
			if (image.Ext != "jpg")
			{
				result = Mogrify("-format jpg", image.FullName);
				if (result == 0)
				{
					originalFile.rm(); // mogrify -format creates a copy and leaves the original file there => remove it
					image.Ext = "jpg";
				}
			}
			if (result != 0)
				return result;
			var tempFile = new ImageFile(GetTempFileName(image.FullName));
			tempFile.mv(image);
			var jpegTranArgs = JpegTranOptions + " " + Os.EscapeParam(tempFile.FullName) + " " + Os.EscapeParam(image.FullName);
			var jpegTran = new RaiSystem(JpegTranCommand, jpegTranArgs);
			result = jpegTran.Exec(out message);
			if (result != 0 || !image.Exists()) // TODO Rainer — File.Exists → RaiFile.Exists; verify rollback logic uses OsLib throughout
			{
				if (image.Exists())
					image.rm();
				if (tempFile.Exists())
					image.mv(tempFile, replace: true, keepBackup: false);
			}
			if (tempFile.Exists())
				tempFile.rm();
			return result;
		}
		/// <summary>
		/// creates a name from a passed-in fileName in the system's TempDir and assures that the name is different from all other names in that directory
		/// </summary>
		/// <param name="fromFile"></param>
		private string GetTempFileName(string fromFile)
		{
			var tempFile = new ImageFile(fromFile);
			tempFile.Path = Os.TempDir;
			while (tempFile.Exists())
				tempFile.NameExt = DateTimeOffset.UtcNow.UtcTicks.ToString("x");
			return tempFile.FullName;
		}
		#region some specialized methods
		public System.Drawing.Size GetSize(string imageFileName)
		{
			//DateTimeOffset start = DateTimeOffset.UtcNow;
			//TimeSpan deltaT;
			string result = "2081 2601";
			if (Identify("-ping -format \"%[fx:w] %[fx:h]\"", imageFileName, ref result) == 0)
			{
				char[] whitespace = { ' ', '\r', '\n', '\t' };
				string[] results = result.Split(whitespace);
				if (results.Length < 2)
					throw new InvalidDataException("in ImageMagick.GetSize/Identify call of " + imageFileName);
				return new System.Drawing.Size(System.Convert.ToInt16(results[0]), System.Convert.ToInt16(results[1]));
			}
			throw new FileNotFoundException("in ImageMagick.GetSize;", imageFileName);
		}
		/// <summary>
		/// creates tiles from a master image; master image stays unaffected
		/// </summary>
		/// <param name="master">full qualifying name with path</param>
		/// <param name="tileWidth">width of a tile in permille to cut out of the master; 0..1000</param>
		/// <param name="tileHeight">hight of a tile in permille to cut out of the master; 0..1000</param>
		/// <param name="resizeWidth">0 if no resize requested; in px</param>
		/// <param name="resizeHeight">0 if no resize requested; in px</param>
		/// <param name="adaptive">true|false</param>
		/// <param name="quality">ie "95%"</param>
		/// <param name="strip">remove metainfo from tiles if true</param>
		/// <param name="sharpen"></param>
		/// <param name="asharpen"></param>
		/// <param name="unsharp">unsharpmask if sharpening is requested or else null; ie "1.2x0.9+1.5+0.04"</param>
		/// <param name="destFiles">complete path of destination file; has to contain %d</param>
		/// <param name="deleteFirst">true|false</param>
		/// <returns>0 if IM call was successful; else errorcode</returns>
		public int CreateTiles(ImageTreeFile master, int tileWidth, int tileHeight, int resizeWidth, int resizeHeight, bool adaptive,
						string quality, bool strip, string sharpen, string asharpen, string unsharp, ImageTreeFile destFiles, bool deleteFirst)
		{
			System.Drawing.Size size = default(System.Drawing.Size);
			#region delete old tiles if necessary
			if (deleteFirst)
			{
				#region assume that file pattern has to be updated if resizeWidth or resizeHeight are set to 0 (autocalc)
				// code duplication (see below) tolerated as trade-off for better performance
				if (resizeWidth == 0 || resizeHeight == 0)
				{
					size = GetSize(master.FullName);
					#region handle no resize requested
					if (resizeWidth == 0)
						resizeWidth = size.Width / (int)(Math.Round(1000 / (float)tileWidth, 0));
					if (resizeHeight == 0)
						resizeHeight = size.Height / (int)(Math.Round(1000 / (float)tileHeight, 0));
					destFiles.NameExt = resizeWidth.ToString() + "x" + resizeHeight.ToString();
					#endregion
				}
				#endregion
				var oldFiles = destFiles.Path.GetFiles(destFiles.NameWithExtension.Replace("%d", "*"));
				foreach (var oldRaiFile in oldFiles)
				{
					#region robust delete
					try
					{
						oldRaiFile.rm();
					}
#pragma warning disable 0168
					catch (Exception fileInUse)
					{
						try
						{
							Thread.Sleep(100);
							oldRaiFile.rm();
						}
						catch (Exception fileStillInUse)
						{
							try
							{
								Thread.Sleep(300);
								oldRaiFile.rm();
							}
							catch (Exception giveItUp)
							{
								continue;
							}
						}
					}
#pragma warning restore 0168
					#endregion
				}
			}
			#endregion
			else if (File.Exists(destFiles.FullName.Replace("%d", "0"))) // TODO Rainer — File.Exists with string manipulation; needs RaiFile abstraction
				return 0;
			if (size.IsEmpty)
			{
				size = GetSize(master.FullName);
				#region handle no resize requested
				if (resizeWidth == 0)
					resizeWidth = size.Width / (int)(Math.Round(1000 / (float)tileWidth, 0));
				if (resizeHeight == 0)
					resizeHeight = size.Height / (int)(Math.Round(1000 / (float)tileHeight, 0));
				destFiles.NameExt = resizeWidth.ToString() + "x" + resizeHeight.ToString();
				#endregion
			}
			#region create new fullsize image in the requested size from master and scale up to the implicitely requested total size if necessary
			int totalWidth = (int)(Math.Round(1000 / (float)tileWidth, 0) * resizeWidth);
			int totalHeight = (int)(Math.Round(1000 / (float)tileHeight, 0) * resizeHeight);
			string options = "";
			if (totalWidth != size.Width || totalHeight != size.Height)
			{
				if (adaptive)
					options = options + " -adaptive-resize " + totalWidth.ToString() + "x" + totalHeight.ToString();
				else options = options + " -resize " + totalWidth.ToString() + "x" + totalHeight.ToString();
			}
			if (asharpen != null && asharpen.Length > 0)
				options = options + " -adaptive-sharpen " + asharpen;
			else if (sharpen != null && sharpen.Length > 0)
				options = options + " -sharpen " + sharpen;
			else if (unsharp != null && unsharp.Length > 0)
				options = options + " -unsharp " + unsharp;
			if (strip)
				options = "-strip " + options;
			ImageTreeFile total = new ImageTreeFile(destFiles.FullName, destFiles.Convention);
			total.Ext = "png";
			total.TileNumber = "all";
			int rc = Convert(options, master.FullName, total.FullName, true);
			#endregion
			#region cut tiles
			if (rc == 0)
			{
				options = "-crop " + resizeWidth.ToString() + "x" + resizeHeight.ToString();
				if (!string.IsNullOrEmpty(quality))
					options = "-quality " + quality + "% " + options;
				rc = Convert(options, total.FullName, destFiles.FullName, true);
			}
			#endregion
			#region clean up
			total.rm();
			#endregion
			#region old solution
			//return Convert(options, master, destFiles.FullName, true);
			#endregion
			#region optimization for png images - NOT IMPLEMENTED YET
			//if (tSet.Ext == "png" && tSet.Compress)
			//	rc = rc & new ImageMagick().Optipng(dest.FullName);
			#endregion
			return rc;
		}
		#endregion
	}
}