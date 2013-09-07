/******************************************************************/
/*****                                                        *****/
/*****     Project:           Adobe Color Picker Clone 1      *****/
/*****     Filename:          AdobeColors.cs                  *****/
/*****     Original Author:   Danny Blanchard                 *****/
/*****                        - scrabcakes@gmail.com          *****/
/*****     Updates:	                                          *****/
/*****      3/28/2005 - Initial Version : Danny Blanchard     *****/
/*****                                                        *****/
/******************************************************************/

using System;
using System.Drawing;

using SharpDX;

namespace SequencorEditor
{
	/// <summary>
	/// Summary description for AdobeColors
	/// </summary>
	public class AdobeColors
	{
		#region Constructors / Destructors

		public AdobeColors() 
		{ 
		} 

		#endregion

		#region Public Methods

		/// <summary> 
		/// Sets the absolute brightness of a colour 
		/// </summary> 
		/// <param name="c">Original colour</param> 
		/// <param name="brightness">The luminance level to impose</param> 
		/// <returns>an adjusted colour</returns> 
		public static Vector3	SetBrightness( Vector3 c, double brightness )
		{ 
			HSL	hsl = RGB_to_HSL( c ); 
				hsl.L = brightness;

			return HSL_to_RGB( hsl ); 
		} 


		/// <summary> 
		/// Modifies an existing brightness level 
		/// </summary> 
		/// <remarks> 
		/// To reduce brightness use a number smaller than 1. To increase brightness use a number larger tnan 1 
		/// </remarks> 
		/// <param name="c">The original colour</param> 
		/// <param name="brightness">The luminance delta</param> 
		/// <returns>An adjusted colour</returns> 
		public static Vector3	ModifyBrightness( Vector3 c, double brightness )
		{ 
			HSL	hsl = RGB_to_HSL( c );
				hsl.L *= brightness;
 
			return HSL_to_RGB( hsl );
		} 


		/// <summary> 
		/// Sets the absolute saturation level 
		/// </summary> 
		/// <remarks>Accepted values 0-1</remarks> 
		/// <param name="c">An original colour</param> 
		/// <param name="Saturation">The saturation value to impose</param> 
		/// <returns>An adjusted colour</returns> 
		public static Vector3	SetSaturation( Vector3 c, double Saturation )
		{ 
			HSL	hsl = RGB_to_HSL( c ); 
				hsl.S = Saturation; 

			return HSL_to_RGB( hsl );
		} 


		/// <summary> 
		/// Modifies an existing Saturation level 
		/// </summary> 
		/// <remarks> 
		/// To reduce Saturation use a number smaller than 1. To increase Saturation use a number larger tnan 1 
		/// </remarks> 
		/// <param name="c">The original colour</param> 
		/// <param name="Saturation">The saturation delta</param> 
		/// <returns>An adjusted colour</returns> 
		public static Vector3	ModifySaturation( Vector3 c, double Saturation )
		{ 
			HSL	hsl = RGB_to_HSL( c );
				hsl.S *= Saturation;

			return HSL_to_RGB( hsl );
		} 


		/// <summary> 
		/// Sets the absolute Hue level 
		/// </summary> 
		/// <remarks>Accepted values 0-1</remarks> 
		/// <param name="c">An original colour</param> 
		/// <param name="Hue">The Hue value to impose</param> 
		/// <returns>An adjusted colour</returns> 
		public static Vector3	SetHue( Vector3 c, double Hue )
		{ 
			HSL	hsl = RGB_to_HSL( c );
				hsl.H = Hue;

			return HSL_to_RGB( hsl );
		} 


		/// <summary> 
		/// Modifies an existing Hue level 
		/// </summary> 
		/// <remarks> 
		/// To reduce Hue use a number smaller than 1. To increase Hue use a number larger tnan 1 
		/// </remarks> 
		/// <param name="c">The original colour</param> 
		/// <param name="Hue">The Hue delta</param> 
		/// <returns>An adjusted colour</returns> 
		public static Vector3	ModifyHue( Vector3 c, double Hue )
		{ 
			HSL	hsl = RGB_to_HSL( c );
				hsl.H *= Hue;

			return HSL_to_RGB( hsl ); 
		} 


		/// <summary> 
		/// Converts a colour from HSL to RGB 
		/// </summary> 
		/// <remarks>Adapted from the algoritm in Foley and Van-Dam</remarks> 
		/// <param name="hsl">The HSL value</param> 
		/// <returns>A Vector3 structure containing the equivalent RGB values</returns> 
		public static Vector3	HSL_to_RGB( HSL hsl )
		{
			float	Max, Mid, Min;
			double q;

			Max = (float) hsl.L;
			Min = (float) ((1.0 - hsl.S) * hsl.L);
			q   = (double)(Max - Min);

			if ( hsl.H >= 0 && hsl.H <= 1.0/6.0 )
			{
				Mid = (float) (((hsl.H - 0) * q) * 6 + Min);
				return new Vector3( Max,Mid,Min );
			}
			else if ( hsl.H <= 1.0/3.0 )
			{
				Mid = (float) (-((hsl.H - 1.0/6.0) * q) * 6 + Max);
				return new Vector3( Mid,Max,Min);
			}
			else if ( hsl.H <= 0.5 )
			{
				Mid = (float) (((hsl.H - 1.0/3.0) * q) * 6 + Min);
				return new Vector3( Min,Max,Mid);
			}
			else if ( hsl.H <= 2.0/3.0 )
			{
				Mid = (float) (-((hsl.H - 0.5) * q) * 6 + Max);
				return new Vector3( Min,Mid,Max);
			}
			else if ( hsl.H <= 5.0/6.0 )
			{
				Mid = (float) (((hsl.H - 2.0/3.0) * q) * 6 + Min);
				return new Vector3( Mid,Min,Max);
			}
			else if ( hsl.H <= 1.0 )
			{
				Mid = (float) (-((hsl.H - 5.0/6.0) * q) * 6 + Max);
				return new Vector3( Max,Min,Mid);
			}
			else	return new Vector3( 0,0,0);
		} 

		public static Color		HSL_to_RGB_LDR( HSL _HSL )
		{
			Vector3	RGB = AdobeColors.HSL_to_RGB( new AdobeColors.HSL( _HSL.H, _HSL.S, Math.Min( 1.0f, _HSL.L ) ) );
 			return Color.FromArgb( (int) Math.Max( 0.0f, Math.Min( 255.0f, RGB.X * 255.0f ) ), (int) Math.Max( 0.0f, Math.Min( 255.0f, RGB.Y * 255.0f ) ), (int) Math.Max( 0.0f, Math.Min( 255.0f, RGB.Z * 255.0f ) ) );
		}

		public static Color		ConvertHDR2LDR( Vector3 _RGBHDR )
		{
			return	HSL_to_RGB_LDR( RGB_to_HSL( _RGBHDR ) );
		}

		public static Vector4	RGB_LDR_to_RGB_HDR( Color _RGBLDR )
		{
			return RGB_LDR_to_RGB_HDR( _RGBLDR.R, _RGBLDR.G, _RGBLDR.B, _RGBLDR.A );
		}

		public static Vector4	RGB_LDR_to_RGB_HDR( int _R, int _G, int _B, int _A )
		{
			return new Vector4( _R / 255.0f, _G / 255.0f, _B / 255.0f, _A / 255.0f );
		}

		/// <summary> 
		/// Converts RGB to HSL 
		/// </summary> 
		/// <param name="c">The RGB Vector3 to convert</param> 
		/// <returns>An HSL value</returns> 
		public static HSL RGB_to_HSL( Vector3 c )
		{ 
			HSL hsl =  new HSL(); 
          
			float Max, Min, Diff, Sum;

			//	Of our RGB values, assign the highest value to Max, and the Smallest to Min
			if ( c.X > c.Y )	{ Max = c.X; Min = c.Y; }
			else				{ Max = c.Y; Min = c.X; }
			if ( c.Z > Max )	  Max = c.Z;
			else if ( c.Z < Min ) Min = c.Z;

			Diff = Max - Min;
			Sum = Max + Min;

			//	Luminance - a.k.a. Brightness - Adobe photoshop uses the logic that the
			//	site VBspeed regards (regarded) as too primitive = superior decides the 
			//	level of brightness.
			hsl.L = Max;
//			hsl.L = 0.5f * (Min + Max);

			//	Saturation
			if ( Max == 0 ) hsl.S = 0;		//	Protecting from the impossible operation of division by zero.
			else hsl.S = (double)Diff/Max;	//	The logic of Adobe Photoshops is this simple.

			//	Hue		R is situated at the angel of 360 eller noll degrees; 
			//			G vid 120 degrees
			//			B vid 240 degrees
			double q;
			if ( Diff == 0 ) q = 0; // Protecting from the impossible operation of division by zero.
			else q = (double)60/Diff;
			
			if ( Max == c.X )
			{
				if ( c.Y < c.Z )	hsl.H = (double)(360 + q * (c.Y - c.Z))/360;
				else				hsl.H = (double)(q * (c.Y - c.Z))/360;
			}
			else if ( Max == c.Y )	hsl.H = (double)(120 + q * (c.Z - c.X))/360;
			else if ( Max == c.Z )	hsl.H = (double)(240 + q * (c.X - c.Y))/360;
			else					hsl.H = 0.0;

			return hsl; 
		} 

		#endregion

		#region Public Classes

		[System.Diagnostics.DebuggerDisplay( "HSL=[{_h}, {_s}, {_l}]" )]
		public class HSL 
		{ 
			#region Class Variables

			public HSL() 
			{ 
				_h=0; 
				_s=0; 
				_l=0; 
			} 

			public HSL( HSL _Source )
			{ 
				_h=_Source._h;
				_s=_Source._s;
				_l=_Source._l;
			} 

			public HSL( double _H, double _S, double _L )
			{ 
				_h=_H; 
				_s=_S; 
				_l=_L; 
			} 

			double _h; 
			double _s; 
			double _l; 

			#endregion

			#region Public Methods

			public double H 
			{ 
				get{return _h;} 
				set 
				{ 
					_h=value; 
					_h=_h>1 ? 1 : _h<0 ? 0 : _h; 
				} 
			} 


			public double S 
			{ 
				get{return _s;} 
				set 
				{ 
					_s=value; 
					_s=_s>1 ? 1 : _s<0 ? 0 : _s; 
				} 
			} 


			public double L 
			{ 
				get{return _l;} 
				set 
				{ 
					_l=value; 
					_l = Math.Max( 0.0, _l ); 
				} 
			} 


			#endregion
		} 


		[System.Diagnostics.DebuggerDisplay( "CMYK=[{ch}, {_m}, {_y}, {_k}]" )]
		public class CMYK 
		{ 
			#region Class Variables

			public CMYK() 
			{ 
				_c=0; 
				_m=0; 
				_y=0; 
				_k=0; 
			} 


			double _c; 
			double _m; 
			double _y; 
			double _k;

			#endregion

			#region Public Methods

			public double C 
			{ 
				get{return _c;} 
				set 
				{ 
					_c=value; 
					_c=_c>1 ? 1 : _c<0 ? 0 : _c; 
				} 
			} 


			public double M 
			{ 
				get{return _m;} 
				set 
				{ 
					_m=value; 
					_m=_m>1 ? 1 : _m<0 ? 0 : _m; 
				} 
			} 


			public double Y 
			{ 
				get{return _y;} 
				set 
				{ 
					_y=value; 
					_y=_y>1 ? 1 : _y<0 ? 0 : _y; 
				} 
			} 


			public double K 
			{ 
				get{return _k;} 
				set 
				{ 
					_k=value; 
					_k=_k>1 ? 1 : _k<0 ? 0 : _k; 
				} 
			} 


			#endregion
		} 

		#endregion
	}
}
