using System.Drawing;

namespace RaiImage
{
	/// <summary>
	/// for ImageMagick 
	/// </summary>
	public class DyeDelta
	{
		public int DeltaB;
		public int DeltaS;
		public int Phi;
		public DyeDelta(Dye color1, Dye color2)
		{
			Phi = color1.Phi(color2);
			DeltaB = color1.DeltaB(color2);
			DeltaS = color1.DeltaS(color2);
		}
	}
	public class Dye
	{
		public System.Drawing.Color Color;
		public float Hue
		{
			get
			{
				return Color.GetHue();
			}
		}
		/// <summary>
		/// Calculate angle between two colors in the color wheel
		/// </summary>
		/// <param name="otherColor"></param>
		/// <returns> IM wants it -100 .. 0 .. 100</returns>
		public int Phi(Dye otherColor)
		{
			var hueDelta = otherColor.Hue - Hue;
			var rotation = 100 + (int)(hueDelta / 180 * 100 + 0.5);
			return /*rotation > 200 ? rotation - 200 : */rotation;
		}
		public int DeltaB(Dye other)
		{
			return (int)(100 * (other.Color.GetBrightness() / Color.GetBrightness()) + .5);
			// ImageMagick interprets 100 as unchanged (100%), 200 as twice as bright 50 as half as bright
		}
		public int DeltaSa(Dye other)
		{
			var myS = Color.GetSaturation();
			var otherS = other.Color.GetSaturation();
			return (int)(100 * (myS < otherS ? 1.0 + otherS - myS : 1.0 - (myS - otherS)) + 0.5);
			// ImageMagick interprets 100 as unchanged (100%), 0 is grayscale, 200 is very colorful (cartoonish)
		}
		public int DeltaSb(Dye other)
		{
			var myS = Color.GetSaturation();
			var otherS = other.Color.GetSaturation();
			return (int)(100 * (1.0 + (otherS - myS) / myS) + 0.5);
			// ImageMagick interprets 100 as unchanged (100%), 0 is grayscale, 200 is very colorful (cartoonish)
		}
		/// <summary>
		/// reduced version: colors were too bright 
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int DeltaS(Dye other)
		{
			return DeltaSa(other);
			//return (DeltaSa(other) + DeltaSb(other)) / 2;
			// ImageMagick interprets 100 as unchanged (100%), 0 is grayscale, 200 is very colorful (cartoonish)
		}
		// public Dye(string rrggbb)
		// {
		// 	Color = System.Drawing.ColorTranslator.FromHtml(rrggbb.Replace("%23", "#"));
		// }
		public Dye(byte red, byte blue, byte green)
		{
			Color = System.Drawing.Color.FromArgb(red, green, blue);
		}
	}
}
