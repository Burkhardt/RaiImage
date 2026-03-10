using System;
using System.Collections.Generic;
using System.IO;
using OsLib;
//using Handy.DotNETCoreCompatibility.ColourTranslations;

namespace RaiImage
{
    /// <summary>
    /// ImageMagick compatible Color
    /// </summary>
    public class ColorInfo
	{
		public static string ColorNamesFile { get; set; }
		/// <summary>
		/// get .Net, ImageMagick and JavaScript names translated - both directions
		/// </summary>
		/// <param name="nameOrHexCode">get the name this parameter starts with a #, otherwise get the hex code</param>
		/// <returns>null, if not found; otherwise the matching ColorInfo</returns>
		public static ColorInfo Get(string nameOrHexCode)
		{
			init();
			if (nameOrHexCode[0] == '#')
				return colorIndexed.ContainsKey(nameOrHexCode) ? new ColorInfo(nameOrHexCode, colorIndexed[nameOrHexCode]) : null;
			else if (nameIndexed.ContainsKey(nameOrHexCode))
			{
				string code = nameIndexed[nameOrHexCode];
				string name = colorIndexed[code];	// to get the CamelCase version
				return new ColorInfo(code, name);
			}
			return null;
		}
		private static void init()
		{
			if (nameIndexed == null || colorIndexed == null)
			{
				if (ColorNamesFile == null)
					throw new FileLoadException("ColorInfo.Get initialization error; set ColorInfo.ColorNamesFile = Server.MapPath(@\"Resources\\ColorNamesFile.txt\"); from the aspx.cs code before you call ColorInfo.Get().");
				nameIndexed = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
				colorIndexed = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
				foreach (var line in new TextFile(ColorNamesFile).Lines)
				{
					string[] token = line.Split(new char[] { '\t' });
					string code = "#" + token[1].ToLower();
					string name = token[0];
					if (!colorIndexed.ContainsKey(code))
						colorIndexed.Add(code, name);
					if (!nameIndexed.ContainsKey(name))	// only pick one if two names have the same code (i.e. aquamarine and aquamarine1)
						nameIndexed.Add(name, code);
				}
			}
		}
		static Dictionary<string, string> colorIndexed = null;
		static Dictionary<string, string> nameIndexed = null;
		public static Dictionary<string, string> NamedColors { 
			get 
			{ 
				init(); 
				return nameIndexed; 
			} 
		}
		/// <summary>
		/// This is utilizing the System.Drawing.ColorTranslater - use ColorInfo.Get() to also get ImageMagick and JavaScript names translated
		/// </summary>
		public System.Drawing.Color Color { get; private set; }
		/// <summary>
		/// number of pixel with this color found in the image
		/// </summary>
		public int Count;
		/// <summary>
		/// hex notation with the #, ie #AAAACCCCEEEE
		/// </summary>
		public string Code {
			get
			{
				return code;
			}
			set
			{
				code = value;
				Color = System.Drawing.Color.FromArgb(int.Parse("#FFFFFF".Replace("#",""), System.Globalization.NumberStyles.AllowHexSpecifier));
				//Color = System.Drawing.ColorTranslator.FromHtml(code);
			}
		} private string code = null;
		/// <summary>
		/// if the color has a std name (known to ImageMagick) this field will have the English name for the color, otherwise RGB(r, g, b)
		/// </summary>
		public string Name;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="colorCode">with leading '#', 3, 6 or 12 hex digits</param>
		/// <param name="colorName">will be left empty if not passed-in; use ColorInfo.Get to lookup a color name</param>
		/// <param name="pixelsInThisColor"></param>
		/// <exception cref="System.FormatException">System.FormatException</exception>
		/// <remarks>32bit per color added as of 20140818</remarks>
		public ColorInfo(string colorCode, string colorName = null, int pixelsInThisColor = 0)
		{
			if (colorCode[0] != '#' || colorCode.Length < 2)
				throw new FormatException("colorCode has to start with '#' followed by one or more hex digits;");
			Code = colorCode;
			Name = colorName;
			Count = pixelsInThisColor;
		}
	}
}
