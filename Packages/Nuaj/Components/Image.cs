using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj
{
	#region ------------------- GAMMA CORRECTION -------------------
	// The Image class completely supports gamma-corrected images so your internal representation of images always is in linear space.
	// The _ImageGamma parameter present in numerous methods of the Image class is very important to understand in order to obtain
	//  linear space images so you can work peacefully with your pipeline.
	// 
	// The Problem :
	// -------------
	// To sum up, images digitized by cameras or scanners (and even images hand-painted by a software that does not support gamma correction)
	// are stored with gamma-correction "unapplied". That means your camera knows that your monitor has a display curve with a
	// gamma correction factor of about 2.2 so, in order to get back the image you took with the camera, it will store the image
	// with the inverse gamma factor of the monitor.
	// 
	// In short, here is what happens in a few steps :
	//   1) Photons and radiance in the real scene you shot with the camera are in "linear space"
	//       => This means that receiving twice more photons will double the radiance
	// 
	//   2) The camera sensor grabs the radiance and stores it internally in linear space (all is well until now)
	// 
	//   3) When you store the RAW camera image into JPEG or PNG for example, the camera will write the gamma-corrected radiance
	//       => This means the color written to the disk file is not RGB but pow( RGB, 1/Gamma ) instead
	//       => For JPEG or PNG, the usual Gamma value is 2.2 to compensate for the average 2.2 gamma of the CRT displays
	//       
	//   4) When you load the JPEG image as a texture, it's not in linear space but in *GAMMA SPACE*
	// 
	//   5) Finally, displaying the texture to the screen will apply the final gamma correction that will, ideally, convert back
	//      the gamma space image into linear space radiance for your eyes to see.
	//       => This means the monitor will not display the color RGB but pow( RGB, Gamma ) instead
	//       => The usual gamma of a CRT is 2.2, thus nullifying the effect of the JPEG 2.2 gamma correction
	// 
	// So, if you are assuming you are dealing with linear space images from point 4) then you are utterly **WRONG** and will lead to many problems !
	// (even creating mip-maps in gamma space is a problem)
	// 
	// 
	// The Solution :
	// --------------
	// The idea is simply to negate the effect of JPEG/PNG/Whatever gamma-uncorrection by applying pow( RGB, Gamma ) as soon as
	//  point 4) so you obtain nice linear-space textures you can work with peacefully.
	// You can either choose to apply the pow() providing the appropriate _ImageGamma parameter, or you can
	//	use the PF_RGBA8_sRGB pixel format with a _ImageGamma of 1.0 if you know your image is sRGB encoded.
	// 
	// If everything is in linear space then all is well in your rendering pipeline until the result is displayed back.
	// Indeed, right before displaying the final (linear space) color, you should apply gamma correction and write pow( RGB, 1/Gamma )
	//  so the monitor will then apply pow( RGB, Gamma ) and so your linear space color is correctly viewed by your eyes.
	// That may seem like a lot of stupid operations queued together to amount to nothing, but these are merely here to circumvent a physical
	//  property of the screens (which should have been handled by the screen constructors a long time ago IMHO).
	// 
	// 
	// The complete article you should read to make up your mind about gamma : http://http.developer.nvidia.com/GPUGems3/gpugems3_ch24.html
	#endregion

	/// <summary>
	/// This is the base image class that loads images into different supported formats so they can later be provided to textures
	/// This class is also able to create mip maps from a source image
	/// </summary>
	/// <remarks>You can find various existing pixel formats in the PixelFormat.cs file</remarks>
	public class Image<PF> : Component where PF:struct,IPixelFormat
	{
		#region CONSTANTS

		public const float	GAMMA_NONE = 1.0f;			// Value to use for no gamma correction
		public const float	GAMMA_RAW = 1.0f;			// Value to use for RAW images already in linear space
		public const float	GAMMA_JPEG = 2.2f;			// JPEG uses a gamma correction of 2.2
		public const float	GAMMA_SRGB = -1.0f;			// sRGB-encoded image

		protected const float	BYTE_TO_FLOAT = 1.0f / 255.0f;

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// A delegate used to process pixels (i.e. either build or modify the pixel)
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Y"></param>
		/// <param name="_Color"></param>
		public delegate void	ImageProcessDelegate( int _X, int _Y, ref Vector4 _Color );

		#endregion

		#region FIELDS

		protected int				m_Width = 0;
		protected int				m_Height = 0;
		protected int				m_MipLevelsCount = 1;
		protected bool				m_bHasAlpha = false;
		protected float				m_ImageGamma = 1.0f;
		protected DataStream[]		m_DataStreams = null;		// Data stream for all mip levels
		protected DataRectangle[]	m_DataRectangles = null;	// Data rectangles for all mip levels

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the image width
		/// </summary>
		public int		Width					{ get { return m_Width; } }

		/// <summary>
		/// Gets the image height
		/// </summary>
		public int		Height					{ get { return m_Height; } }

		/// <summary>
		/// Gets the amount of mip levels for the image
		/// </summary>
		public int		MipLevelsCount			{ get { return m_MipLevelsCount; } }

		/// <summary>
		/// Tells if the image has an alpha channel
		/// </summary>
		public bool		HasAlpha				{ get { return m_bHasAlpha; } }

		/// <summary>
		/// Gets the gamma correction the image was imported with
		/// </summary>
		public float	ImageGamma				{ get { return m_ImageGamma; } }

		/// <summary>
		/// Gets the image's data stream for all mip levels
		/// </summary>
		public DataStream[]		DataStreams		{ get { return m_DataStreams; } }

		/// <summary>
		/// Gets the DataRectangles for all mip levels
		/// </summary>
		public DataRectangle[]	DataRectangles	{ get { return m_DataRectangles; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an image placeholder
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public	Image( Device _Device, string _Name, int _Width, int _Height, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_Width = _Width;
			m_Height = _Height;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataRectangles = new DataRectangle[m_MipLevelsCount];
		}

		/// <summary>
		/// Creates an image from a bitmap
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess ) : this( _Device, _Name, _Image, false, _MipLevelsCount, _ImageGamma, _PreProcess )
		{
		}
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, int _MipLevelsCount, float _ImageGamma ) : this( _Device, _Name, _Image, false, _MipLevelsCount, _ImageGamma, null )	{}

		/// <summary>
		/// Creates an image from a bitmap
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, bool _MirrorY, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess ) : base( _Device, _Name )
		{
			m_Width = _Image.Width;
			m_Height = _Image.Height;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataRectangles = new DataRectangle[m_MipLevelsCount];

			Load( _Image, _MirrorY, _ImageGamma, _PreProcess );
		}
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, bool _MirrorY, int _MipLevelsCount, float _ImageGamma ) : this( _Device, _Name, _Image, _MirrorY, _MipLevelsCount, _ImageGamma, null )	{}

		/// <summary>
		/// Creates an image from a bitmap and an alpha
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, System.Drawing.Bitmap _Alpha, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess ) : this( _Device, _Name, _Image, _Alpha, false, _MipLevelsCount, _ImageGamma, _PreProcess )
		{
		}
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, System.Drawing.Bitmap _Alpha, int _MipLevelsCount, float _ImageGamma ) : this( _Device, _Name, _Image, _Alpha, _MipLevelsCount, _ImageGamma, null )	{}

		/// <summary>
		/// Creates an image from a bitmap and an alpha
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, System.Drawing.Bitmap _Alpha, bool _MirrorY, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess ) : base( _Device, _Name )
		{
			m_Width = _Image.Width;
			m_Height = _Image.Height;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataRectangles = new DataRectangle[m_MipLevelsCount];

			Load( _Image, _Alpha, _MirrorY, _ImageGamma, _PreProcess );
		}
		public	Image( Device _Device, string _Name, System.Drawing.Bitmap _Image, System.Drawing.Bitmap _Alpha, bool _MirrorY, int _MipLevelsCount, float _ImageGamma ) : this( _Device, _Name, _Image, _Alpha, _MirrorY, _MipLevelsCount, _ImageGamma, null )	{}

		/// <summary>
		/// Creates an image from a HDR array
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public	Image( Device _Device, string _Name, Vector4[,] _Image, float _Exposure, int _MipLevelsCount, ImageProcessDelegate _PreProcess ) : base( _Device, _Name )
		{
			m_Width = _Image.GetLength( 0 );
			m_Height = _Image.GetLength( 1 );
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataRectangles = new DataRectangle[m_MipLevelsCount];

			Load( _Image, _Exposure, _PreProcess );
		}
		public	Image( Device _Device, string _Name, Vector4[,] _Image, float _Exposure, int _MipLevelsCount ) : this( _Device, _Name, _Image, _Exposure, _MipLevelsCount, null ) {}

		/// <summary>
		/// Creates a custom image using a pixel writer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public	Image( Device _Device, string _Name, int _Width, int _Height, ImageProcessDelegate _PixelWriter, int _MipLevelsCount ) : base( _Device, _Name )
		{
			if ( _PixelWriter == null )
				throw new NException( this, "Invalid pixel writer !" );

			m_Width = _Width;
			m_Height = _Height;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataRectangles = new DataRectangle[m_MipLevelsCount];

			int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

			// Create the data rectangle
			try
			{
				PF[]	Scanline = new PF[m_Width];
				m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * PixelSize, true, true ) );

				// Convert all scanlines into the desired format and write them into the stream
				Vector4	C = new Vector4();
				for ( int Y=0; Y < m_Height; Y++ )
				{
					for ( int X=0; X < m_Width; X++ )
					{
						_PixelWriter( X, Y, ref C );
						Scanline[X].Write( C );

						m_bHasAlpha |= C.W != 1.0f;	// Check if it has alpha...
					}

					m_DataStreams[0].WriteRange<PF>( Scanline );
				}

				// Build the data rectangle from that stream
				m_DataRectangles[0] = new DataRectangle( m_DataStreams[0].DataPointer, m_Width*PixelSize );

				// Build mip levels
				BuildMissingMipLevels();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while creating the custom image !", _e );
			}
		}

		/// <summary>
		/// Loads a LDR image from memory
		/// </summary>
		/// <param name="_Image">Source image to load</param>
		/// <param name="_MirrorY">True to mirror the image vertically</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public void	Load( System.Drawing.Bitmap _Image, bool _MirrorY, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			if ( _Image.Width != m_Width )
				throw new NException( this, "Provided image width mismatch !" );
			if ( _Image.Height != m_Height )
				throw new NException( this, "Provided image height mismatch !" );

			int		PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

#if DEBUG
			// Ensure we're passing a unit image gamma
			bool	bUsesSRGB = new PF().sRGB;
			if ( bUsesSRGB && Math.Abs( _ImageGamma - 1.0f ) > 1e-3f )
				throw new NException( this, "You specified a sRGB pixel format but provided an image gamma different from 1 !" );
#endif

			byte[]	ImageContent = LoadBitmap( _Image, out m_Width, out m_Height );

			// Create the data rectangle
			try
			{
				PF[]	Scanline = new PF[m_Width];
				m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * PixelSize, true, true ) );

				// Convert all scanlines into the desired format and write them into the stream
				Vector4	Temp = new Vector4();
				int		Offset;
				for ( int Y=0; Y < m_Height; Y++ )
				{
					Offset = (m_Width * (_MirrorY ? m_Height-1-Y : Y)) << 2;
					for ( int X=0; X < m_Width; X++ )
					{
						Temp.X = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset++], _ImageGamma );
						Temp.Y = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset++], _ImageGamma );
						Temp.Z = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset++], _ImageGamma );
						Temp.W = BYTE_TO_FLOAT * ImageContent[Offset++];

						if ( _PreProcess != null )
							_PreProcess( X, Y, ref Temp );

						Scanline[X].Write( Temp );

						m_bHasAlpha |= Scanline[X].Alpha != 1.0f;	// Check if it has alpha...
					}

					m_DataStreams[0].WriteRange<PF>( Scanline );
				}

				// Build the data rectangle from that stream
				m_DataRectangles[0] = new DataRectangle( m_DataStreams[0].DataPointer, m_Width*PixelSize );

				// Build mip levels
				BuildMissingMipLevels();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while loading the image !", _e );
			}
		}

		/// <summary>
		/// Loads a LDR image and its alpha from memory
		/// </summary>
		/// <param name="_Image">Source image to load</param>
		/// <param name="_Alpha">Source alpha to load</param>
		/// <param name="_MirrorY">True to mirror the images vertically</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public void	Load( System.Drawing.Bitmap _Image, System.Drawing.Bitmap _Alpha, bool _MirrorY, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			if ( _Image.Width != m_Width )
				throw new NException( this, "Provided image width mismatch !" );
			if ( _Image.Height != m_Height )
				throw new NException( this, "Provided image height mismatch !" );
			if ( _Alpha.Width != m_Width )
				throw new NException( this, "Provided alpha width mismatch !" );
			if ( _Alpha.Height != m_Height )
				throw new NException( this, "Provided alpha height mismatch !" );

			int		PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

#if DEBUG
			// Ensure we're passing a unit image gamma
			bool	bUsesSRGB = new PF().sRGB;
			if ( bUsesSRGB && Math.Abs( _ImageGamma - 1.0f ) > 1e-3f )
				throw new NException( this, "You specified a sRGB pixel format but provided an image gamma different from 1 !" );
#endif
			// Lock source image
			byte[]	ImageContent = LoadBitmap( _Image, out m_Width, out m_Height );
			byte[]	AlphaContent = LoadBitmap( _Alpha, out m_Width, out m_Height );

			m_bHasAlpha = true;	// We know it has alpha...

			// Create the data rectangle
			try
			{
				PF[]	Scanline = new PF[m_Width];
				m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * PixelSize, true, true ) );

				// Convert all scanlines into the desired format and write them into the stream
				int		Offset;
				Vector4	Temp = new Vector4();

				for ( int Y=0; Y < m_Height; Y++ )
				{
					Offset = (m_Width * (_MirrorY ? m_Height-1-Y : Y)) << 2;
					for ( int X=0; X < m_Width; X++, Offset+=4 )
					{
						Temp.X = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset+0], _ImageGamma );
						Temp.Y = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset+1], _ImageGamma );
						Temp.Z = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset+2], _ImageGamma );
						Temp.W = BYTE_TO_FLOAT * AlphaContent[Offset+0];

						if ( _PreProcess != null )
							_PreProcess( X, Y, ref Temp );

						Scanline[X].Write( Temp );
					}

					m_DataStreams[0].WriteRange<PF>( Scanline );
				}

				// Build the data rectangle from that stream
				m_DataRectangles[0] = new DataRectangle( m_DataStreams[0].DataPointer, m_Width*PixelSize );

				// Build mip levels
				BuildMissingMipLevels();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while loading the image !", _e );
			}
		}

		/// <summary>
		/// Loads a HDR image from memory
		/// </summary>
		/// <param name="_Image">Source image to load</param>
		/// <param name="_Exposure">The exposure correction to apply (default should be 0)</param>
		public void	Load( Vector4[,] _Image, float _Exposure, ImageProcessDelegate _PreProcess )
		{
			int	Width = _Image.GetLength( 0 );
			if ( Width != m_Width )
				throw new NException( this, "Provided image width mismatch !" );

			int	Height = _Image.GetLength( 1 );
			if ( Height != m_Height )
				throw new NException( this, "Provided image height mismatch !" );

			int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

			// Create the data rectangle
			PF[]	Scanline = new PF[m_Width];
			try
			{
				m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * PixelSize, true, true ) );

				// Convert all scanlines into the desired format and write them into the stream
				Vector4	Temp = new Vector4();
				for ( int Y=0; Y < m_Height; Y++ )
				{
					for ( int X=0; X < m_Width; X++ )
					{
						float	fLuminance = 0.3f * _Image[X,Y].X + 0.5f * _Image[X,Y].Y + 0.2f * _Image[X,Y].Z;
						float	fCorrectedLuminance = (float) Math.Pow( 2.0f, Math.Log( fLuminance ) / Math.Log( 2.0 ) + _Exposure );

						Temp = _Image[X,Y] * fCorrectedLuminance / fLuminance;

						if ( _PreProcess != null )
							_PreProcess( X, Y, ref Temp );

						Scanline[X].Write( Temp );

						m_bHasAlpha |= Scanline[X].Alpha != 1.0f;	// Check if it has alpha...
					}

					m_DataStreams[0].WriteRange<PF>( Scanline );
				}

				// Build the data rectangle from that stream
				m_DataRectangles[0] = new DataRectangle( m_DataStreams[0].DataPointer, m_Width*PixelSize );

				// Build mip levels
				BuildMissingMipLevels();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while loading the image !", _e );
			}
		}

		#region HDR Loaders

		/// <summary>
		/// Loads a bitmap in .HDR format into a Vector4 array directly useable by the image constructor
		/// </summary>
		/// <param name="_HDRFormatBinary"></param>
		/// <returns></returns>
		public static Vector4[,]	LoadAndDecodeHDRFormat( byte[] _HDRFormatBinary )
		{
			return DecodeRGBEImage( LoadHDRFormat( _HDRFormatBinary ) );
		}

		/// <summary>
		/// Loads a bitmap in .HDR format into a RGBE array
		/// </summary>
		/// <param name="_HDRFormatBinary"></param>
		/// <returns></returns>
		public static unsafe PF_RGBE[,]	LoadHDRFormat( byte[] _HDRFormatBinary )
		{
			try
			{
				// The header of a .HDR image file consists of lines terminated by '\n'
				// It ends when there are 2 successive '\n' characters, then follows a single line containing the resolution of the image and only then, real scanlines begin...
				//

				// 1] We must isolate the header and find where it ends.
				//		To do this, we seek and replace every '\n' characters by '\0' (easier to read) until we find a double '\n'
				List<string>	HeaderLines = new List<string>();
				int				CharacterIndex = 0;
				int				LineStartCharacterIndex = 0;

				while ( true )
				{
					if ( _HDRFormatBinary[CharacterIndex] == '\n' || _HDRFormatBinary[CharacterIndex] == '\0' )
					{	// Found a new line!
						_HDRFormatBinary[CharacterIndex] = 0;
						fixed ( byte* pLineStart = &_HDRFormatBinary[LineStartCharacterIndex] )
							HeaderLines.Add( new string( (sbyte*) pLineStart, 0, CharacterIndex-LineStartCharacterIndex, System.Text.Encoding.ASCII ) );

						LineStartCharacterIndex = CharacterIndex + 1;

						// Check for header end
						if ( _HDRFormatBinary[CharacterIndex + 2] == '\n' )
						{
							CharacterIndex += 3;
							break;
						}
						if ( _HDRFormatBinary[CharacterIndex + 1] == '\n' )
						{
							CharacterIndex += 2;
							break;
						}
					}

					// Next character
					CharacterIndex++;
				}

				// 2] Read the last line containing the resolution of the image
				byte*	pScanlines = null;
				string	Resolution = null;
				LineStartCharacterIndex = CharacterIndex;
				while ( true )
				{
					if ( _HDRFormatBinary[CharacterIndex] == '\n' || _HDRFormatBinary[CharacterIndex] == '\0' )
					{
						_HDRFormatBinary[CharacterIndex] = 0;
						fixed ( byte* pLineStart = &_HDRFormatBinary[LineStartCharacterIndex] )
							Resolution = new string( (sbyte*) pLineStart, 0, CharacterIndex-LineStartCharacterIndex, System.Text.Encoding.ASCII );

						fixed ( byte* pScanlinesStart = &_HDRFormatBinary[CharacterIndex + 1] )
							pScanlines = pScanlinesStart;

						break;
					}

					// Next character
					CharacterIndex++;
				}

				// 3] Check format and retrieve resolution
					// 3.1] Search lines for "#?RADIANCE" or "#?RGBE"
				bool	bRecognizedFormat = false;
				foreach ( string Line in HeaderLines )
				{
					if ( Line.IndexOf( "#?RADIANCE" ) != -1 || Line.IndexOf( "#?RGBE" ) != -1 )
					{	// Found file type!
						bRecognizedFormat = true;
						break;
					}
				}

				if ( !bRecognizedFormat )
					throw new Exception( "Unknown HDR format!" );					// Unknown format!

					// 3.2] Search lines for format
				string	FileFormat = null;
				foreach ( string Line in HeaderLines )
				{
					if ( Line.IndexOf( "FORMAT=" ) != -1 )
					{	// Found format!
						FileFormat = Line;
						break;
					}
				}
				if ( FileFormat == null )
					throw new Exception( "No format description!" );			// Couldn't get FORMAT

				FileFormat = FileFormat.Replace( "FORMAT=", "" );
				if ( FileFormat.IndexOf( "32-bit_rle_rgbe" ) == -1 )
					throw new Exception( "Can't read format \"" + FileFormat + "\". Only 32-bit-rle-rgbe is currently supported!" );

					// 3.3] Search lines for the exposure
				float	fExposure = 1.0f;
				foreach ( string Line in HeaderLines )
				{
					if ( Line.IndexOf( "EXPOSURE=" ) != -1 )
					{	// Okay, we found the exposure...
						fExposure = float.Parse( Line.Replace( "EXPOSURE=", "" ) );
						break;
					}
				}

					// 3.4] Read the resolution out of the last line
				int		WayX = +1, WayY = +1;
				int		Width = 0, Height = 0;

				int	XIndex = Resolution.IndexOf( "+X" );
				if ( XIndex == -1 )
				{	// Wrong way!
					WayX = -1;
					XIndex = Resolution.IndexOf( "-X" );
				}
				if ( XIndex == -1 )
					throw new Exception( "Couldn't find image width in resolution string \"" + Resolution + "\"!" );
				int	WidthEndCharacterIndex = Resolution.IndexOf( ' ', XIndex + 3 );
				if ( WidthEndCharacterIndex == -1 )
					WidthEndCharacterIndex = Resolution.Length;
				Width = int.Parse( Resolution.Substring( XIndex + 2, WidthEndCharacterIndex - XIndex - 2 ) );

				int	YIndex = Resolution.IndexOf( "+Y" );
				if ( YIndex == -1 )
				{	// Wrong way!
					WayY = -1;
					YIndex = Resolution.IndexOf( "-Y" );
				}
				if ( YIndex == -1 )
					throw new Exception( "Couldn't find image height in resolution string \"" + Resolution + "\"!" );
				int	HeightEndCharacterIndex = Resolution.IndexOf( ' ', YIndex + 3 );
				if ( HeightEndCharacterIndex == -1 )
					HeightEndCharacterIndex = Resolution.Length;
				Height = int.Parse( Resolution.Substring( YIndex + 2, HeightEndCharacterIndex - YIndex - 2 ) );

				// The encoding of the image data is quite simple:
				//
				//	_ Each floating-point component is first encoded in Greg Ward's packed-pixel format which encodes 3 floats into a single DWORD organized this way: RrrrrrrrGgggggggBbbbbbbbEeeeeeee (E being the common exponent)
				//	_ Each component of the packed-pixel is then encoded separately using a simple run-length encoding format
				//

				// 1] Allocate memory for the image and the temporary p_HDRFormatBinaryScanline
				PF_RGBE[,]	Dest = new PF_RGBE[Width, Height];
				byte[,]		TempScanline = new byte[Width,4];

				// 2] Read the scanlines
				int	ImageY = WayY == +1 ? 0 : Height - 1;
				for ( int y=0; y < Height; y++, ImageY += WayY )
				{
					if ( Width < 8 || Width > 0x7FFF || pScanlines[0] != 0x02 )
						throw new Exception( "Unsupported old encoding format!" );

					byte	Temp;
					byte	Green, Blue;

					// 2.1] Read an entire scanline
					pScanlines++;
					Green = *pScanlines++;
					Blue = *pScanlines++;
					Temp = *pScanlines++;

					if ( Green != 2 || (Blue & 0x80) != 0 )
						throw new Exception( "Unsupported old encoding format!" );

					if ( ((Blue << 8) | Temp) != Width )
						throw new Exception( "Line and image widths mismatch!" );

					for ( int ComponentIndex=0; ComponentIndex < 4; ComponentIndex++ )
					{
						for ( int x=0; x < Width; )
						{
							byte	Code = *pScanlines++;
							if ( Code > 128 )
							{	// Run-Length encoding
								Code &= 0x7F;
								byte	RLValue = *pScanlines++;
								while ( Code-- > 0 && x < Width )
									TempScanline[x++,ComponentIndex] = RLValue;
							}
							else
							{	// Normal encoding
								while ( Code-- > 0 && x < Width )
									TempScanline[x++, ComponentIndex] = *pScanlines++;
							}
						}	// For every pixels of the scanline
					}	// For every color components (including exponent)

					// 2.2] Post-process the scanline and re-order it correctly
					int	ImageX = WayX == +1 ? 0 : Width - 1;
					for ( int x=0; x < Width; x++, ImageX += WayX )
					{
						Dest[x,y].R = TempScanline[ImageX, 0];
						Dest[x,y].G = TempScanline[ImageX, 1];
						Dest[x,y].B = TempScanline[ImageX, 2];
						Dest[x,y].E = TempScanline[ImageX, 3];
					}
				}

				return	Dest;
			}
			catch ( Exception _e )
			{	// Ouch!
				throw new Exception( "An exception occured while attempting to load an HDR file!", _e );
			}
		}

		/// <summary>
		/// Decodes a RGBE formatted image into a plain floating-point image
		/// </summary>
		/// <param name="_Source">The source RGBE formatted image</param>
		/// <returns>A HDR image as floats</returns>
		public static Vector4[,]	DecodeRGBEImage( PF_RGBE[,] _Source )
		{
			if ( _Source == null )
				return	null;

			Vector4[,]	Result = new Vector4[_Source.GetLength( 0 ), _Source.GetLength( 1 )];
			for ( int Y=0; Y < _Source.GetLength( 1 ); Y++ )
				for ( int X=0; X < _Source.GetLength( 0 ); X++ )
					Result[X,Y] = _Source[X,Y].DecodedColorAsVector;

			return	Result;
		}

		#endregion

		/// <summary>
		/// Computes the correct amount of mip levels given an input levels count
		/// If the count is 0 then all possible mip levels are generated
		/// </summary>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		protected int	ComputeMipLevelsCount( int _MipLevelsCount )
		{
			int	MaxSize = Math.Max( m_Width, m_Height );
			int	MaxMipLevelsCount = (int) Math.Ceiling( Math.Log( MaxSize+1 ) / Math.Log( 2.0 ) );
			return	_MipLevelsCount == 0 ? MaxMipLevelsCount : Math.Min( MaxMipLevelsCount, _MipLevelsCount );
		}

		/// <summary>
		/// Builds the missing mip levels assuming level 0 is already computed
		/// </summary>
		protected void	BuildMissingMipLevels()
		{
			int		PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );
			bool	bUsesSRGB = new PF().sRGB;

			int				SourceLevelIndex = 0;
			DataStream		SourceStream = m_DataStreams[SourceLevelIndex];
			DataRectangle	SourceRectangle = m_DataRectangles[SourceLevelIndex];
			int				SourceWidth = m_Width;
			int				SourceHeight = m_Height;

			SourceStream.Position = 0;

			while ( SourceLevelIndex < m_MipLevelsCount-1 )
			{
				int	CurrentLevelIndex = SourceLevelIndex+1;
				int	CurrentWidth = Math.Max( 1, SourceWidth >> 1 );
				int	CurrentHeight = Math.Max( 1, SourceHeight >> 1 );

				// Create source & target streams
				DataStream	CurrentStream = ToDispose( new DataStream( CurrentWidth * CurrentHeight * PixelSize, true, true ) );
				m_DataStreams[CurrentLevelIndex] = CurrentStream;

				// Read scanlines 2 by 2
				PF[]	Scanline0 = new PF[SourceWidth];
				PF[]	Scanline1 = new PF[SourceWidth];
				PF[]	ResultScanline = new PF[CurrentWidth];
				Vector4	AverageColor = new Vector4();

				if ( SourceHeight > 1 )
					for ( int Y=0; Y < CurrentHeight; Y++ )
					{
						SourceStream.ReadRange<PF>( Scanline0, 0, SourceWidth );
						SourceStream.ReadRange<PF>( Scanline1, 0, SourceWidth );

						if ( bUsesSRGB )
						{	// Assume sRGB
							PF		C0, C1, C2, C3;
							if ( SourceWidth > 1 )
							{	// Perform averaging of pixels 4 by 4
								for ( int X=0; X < CurrentWidth; X++ )
								{
									C0 = Scanline0[2*X+0];
									C1 = Scanline0[2*X+1];
									C2 = Scanline1[2*X+0];
									C3 = Scanline1[2*X+1];

									// Average in linear space
									AverageColor.X = 0.25f * (sRGB2Linear( C0.Red ) + sRGB2Linear( C1.Red ) + sRGB2Linear( C2.Red ) + sRGB2Linear( C3.Red ));
									AverageColor.Y = 0.25f * (sRGB2Linear( C0.Green ) + sRGB2Linear( C1.Green ) + sRGB2Linear( C2.Green ) + sRGB2Linear( C3.Green ));
									AverageColor.Z = 0.25f * (sRGB2Linear( C0.Blue ) + sRGB2Linear( C1.Blue ) + sRGB2Linear( C2.Blue ) + sRGB2Linear( C3.Blue ));
									AverageColor.W = 0.25f * (C0.Alpha + C1.Alpha + C2.Alpha + C3.Alpha);

									// Convert back to gamma space
									AverageColor.X = Linear2sRGB( AverageColor.X );
									AverageColor.Y = Linear2sRGB( AverageColor.Y );
									AverageColor.Z = Linear2sRGB( AverageColor.Z );

									ResultScanline[X].Write( AverageColor );
								}
							}
							else
							{	// Average pixel from both lines
								C0 = Scanline0[0];
								C1 = Scanline1[0];

								// Average in linear space
								AverageColor.X = 0.5f * (sRGB2Linear( C0.Red ) + sRGB2Linear( C1.Red ));
								AverageColor.Y = 0.5f * (sRGB2Linear( C0.Green ) + sRGB2Linear( C1.Green ));
								AverageColor.Z = 0.5f * (sRGB2Linear( C0.Blue ) + sRGB2Linear( C1.Blue ));
								AverageColor.W = 0.5f * (C0.Alpha + C1.Alpha);

								// Convert back to gamma space
								AverageColor.X = Linear2sRGB( AverageColor.X );
								AverageColor.Y = Linear2sRGB( AverageColor.Y );
								AverageColor.Z = Linear2sRGB( AverageColor.Z );

								ResultScanline[0].Write( AverageColor );
							}
						}
						else
						{	// Assume Linear Space
							if ( SourceWidth > 1 )
							{	// Perform averaging of pixels 4 by 4
								for ( int X=0; X < CurrentWidth; X++ )
								{
									AverageColor.X = 0.25f * (Scanline0[2*X+0].Red + Scanline0[2*X+1].Red + Scanline1[2*X+0].Red + Scanline1[2*X+1].Red);
									AverageColor.Y = 0.25f * (Scanline0[2*X+0].Green + Scanline0[2*X+1].Green + Scanline1[2*X+0].Green + Scanline1[2*X+1].Green);
									AverageColor.Z = 0.25f * (Scanline0[2*X+0].Blue + Scanline0[2*X+1].Blue + Scanline1[2*X+0].Blue + Scanline1[2*X+1].Blue);
									AverageColor.W = 0.25f * (Scanline0[2*X+0].Alpha + Scanline0[2*X+1].Alpha + Scanline1[2*X+0].Alpha + Scanline1[2*X+1].Alpha);
									ResultScanline[X].Write( AverageColor );
								}
							}
							else
							{	// Average pixel from both lines
								AverageColor.X = 0.5f * (Scanline0[0].Red + Scanline1[0].Red);
								AverageColor.Y = 0.5f * (Scanline0[0].Green + Scanline1[0].Green);
								AverageColor.Z = 0.5f * (Scanline0[0].Blue + Scanline1[0].Blue);
								AverageColor.W = 0.5f * (Scanline0[0].Alpha + Scanline1[0].Alpha);
								ResultScanline[0].Write( AverageColor );
							}
						}

						CurrentStream.WriteRange<PF>( ResultScanline );
					}
				else
				{	// Treat a single line
					SourceStream.ReadRange<PF>( Scanline0, 0, SourceWidth );

					// Average pixels 2 by 2 from our single line
					if ( bUsesSRGB )
					{	// Assume sRGB
						PF	C0, C1;
						for ( int X=0; X < CurrentWidth; X++ )
						{
							C0 = Scanline0[2*X+0];
							C1 = Scanline0[2*X+1];

							// Average in linear space
							AverageColor.X = 0.5f * (sRGB2Linear( C0.Red ) + sRGB2Linear( C1.Red ));
							AverageColor.Y = 0.5f * (sRGB2Linear( C0.Green ) + sRGB2Linear( C1.Green ));
							AverageColor.Z = 0.5f * (sRGB2Linear( C0.Blue ) + sRGB2Linear( C1.Blue ));
							AverageColor.W = 0.5f * (C0.Alpha + C1.Alpha);

							// Convert back to gamma space
							AverageColor.X = Linear2sRGB( AverageColor.X );
							AverageColor.Y = Linear2sRGB( AverageColor.Y );
							AverageColor.Z = Linear2sRGB( AverageColor.Z );

							ResultScanline[X].Write( AverageColor );
						}
					}
					else
					{
						for ( int X=0; X < CurrentWidth; X++ )
						{
							AverageColor.X = 0.5f * (Scanline0[2*X+0].Red + Scanline0[2*X+1].Red);
							AverageColor.Y = 0.5f * (Scanline0[2*X+0].Green + Scanline0[2*X+1].Green);
							AverageColor.Z = 0.5f * (Scanline0[2*X+0].Blue + Scanline0[2*X+1].Blue);
							AverageColor.W = 0.5f * (Scanline0[2*X+0].Alpha + Scanline0[2*X+1].Alpha);
							ResultScanline[X].Write( AverageColor );
						}
					}

					CurrentStream.WriteRange<PF>( ResultScanline );
				}

				// Create final data rectangle
				m_DataRectangles[CurrentLevelIndex] = new DataRectangle( CurrentStream.DataPointer, CurrentWidth * PixelSize );

				// Rewind streams
				SourceStream.Position = 0;
				CurrentStream.Position = 0;

				// Scroll data
				SourceLevelIndex = CurrentLevelIndex;
				SourceStream = m_DataStreams[CurrentLevelIndex];
				SourceRectangle = m_DataRectangles[CurrentLevelIndex];
				SourceWidth = CurrentWidth;
 				SourceHeight = CurrentHeight;
			}
		}

		/// <summary>
		/// Creates an image from a bitmap file
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_FileName"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static Image<PF>	CreateFromBitmapFile( Device _Device, string _Name, System.IO.FileInfo _FileName, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( _FileName.FullName ) as System.Drawing.Bitmap )
				return new Image<PF>( _Device, _Name, B, _MipLevelsCount, _ImageGamma, _PreProcess );
		}
		public static Image<PF>	CreateFromBitmapFile( Device _Device, string _Name, System.IO.FileInfo _FileName, int _MipLevelsCount, float _ImageGamma )
		{
			return CreateFromBitmapFile( _Device, _Name, _FileName, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an image array from several bitmap files
		/// (the images must all be disposed of properly by the caller)
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_FileNames"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static Image<PF>[]	CreateFromBitmapFiles( Device _Device, string _Name, System.IO.FileInfo[] _FileNames, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			// Create the array of images
			Image<PF>[]	Images = new Image<PF>[_FileNames.Length];
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( _FileNames[ImageIndex].FullName ) as System.Drawing.Bitmap )
					Images[ImageIndex] = new Image<PF>( _Device, _Name, B, _MipLevelsCount, _ImageGamma, _PreProcess );

			return Images;
		}
		public static Image<PF>[]	CreateFromBitmapFiles( Device _Device, string _Name, System.IO.FileInfo[] _FileNames, int _MipLevelsCount, float _ImageGamma )
		{
			return CreateFromBitmapFiles( _Device, _Name, _FileNames, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an image from a bitmap in memory
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapFileContent"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static Image<PF>	CreateFromBitmapFileInMemory( Device _Device, string _Name, byte[] _BitmapFileContent, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			using ( System.IO.MemoryStream Stream = new System.IO.MemoryStream( _BitmapFileContent ) )
				using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromStream( Stream ) as System.Drawing.Bitmap )
					return new Image<PF>( _Device, _Name, B, _MipLevelsCount, _ImageGamma, _PreProcess );
		}
		public static Image<PF>	CreateFromBitmapFileInMemory( Device _Device, string _Name, byte[] _BitmapFileContent, int _MipLevelsCount, float _ImageGamma )
		{
			return CreateFromBitmapFileInMemory( _Device, _Name, _BitmapFileContent, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an image array from several bitmaps in memory
		/// (the images must all be disposed of properly by the caller)
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapFileContents"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static Image<PF>[]	CreateFromBitmapFilesInMemory( Device _Device, string _Name, byte[][] _BitmapFileContents, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			// Create the array of images
			Image<PF>[]	Images = new Image<PF>[_BitmapFileContents.GetLength( 1 )];
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				using ( System.IO.MemoryStream Stream = new System.IO.MemoryStream( _BitmapFileContents[ImageIndex] ) )
					using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromStream( Stream ) as System.Drawing.Bitmap )
						Images[ImageIndex] = new Image<PF>( _Device, _Name, B, _MipLevelsCount, _ImageGamma, _PreProcess );

			return Images;
		}
		public static Image<PF>[]	CreateFromBitmapFilesInMemory( Device _Device, string _Name, byte[][] _BitmapFileContents, int _MipLevelsCount, float _ImageGamma )
		{
			return CreateFromBitmapFilesInMemory( _Device, _Name, _BitmapFileContents, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an image from a bitmap stream
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapStream"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static Image<PF>	CreateFromBitmapStream( Device _Device, string _Name, System.IO.Stream _BitmapStream, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromStream( _BitmapStream ) as System.Drawing.Bitmap )
				return new Image<PF>( _Device, _Name, B, _MipLevelsCount, _ImageGamma, _PreProcess );
		}
		public static Image<PF>	CreateFromBitmapStream( Device _Device, string _Name, System.IO.Stream _BitmapStream, int _MipLevelsCount, float _ImageGamma )
		{
			return CreateFromBitmapStream( _Device, _Name, _BitmapStream, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an image array from several bitmap streams
		/// (the images must all be disposed of properly by the caller)
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapStreams"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Image<PF>[]	CreateFromBitmapStreams( Device _Device, string _Name, System.IO.Stream[] _BitmapStreams, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			// Create the array of images
			Image<PF>[]	Images = new Image<PF>[_BitmapStreams.GetLength( 1 )];
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromStream( _BitmapStreams[ImageIndex] ) as System.Drawing.Bitmap )
					Images[ImageIndex] = new Image<PF>( _Device, _Name, B, _MipLevelsCount, _ImageGamma, _PreProcess );

			return Images;
		}
		public static Image<PF>[]	CreateFromBitmapStreams( Device _Device, string _Name, System.IO.Stream[] _BitmapStreams, int _MipLevelsCount, float _ImageGamma )
		{
			return CreateFromBitmapStreams( _Device, _Name, _BitmapStreams, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an array of sprite images from a single Texture Page
		/// </summary>
		/// <param name="_SpriteWidth">Width of the sprites</param>
		/// <param name="_SpriteHeight">Height of the sprites</param>
		/// <param name="_TPage">The source TPage</param>
		/// <param name="_MipLevelsCount">The amount of mip levels to create for each image</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns>An array of sprite images, all having the same width, height and mips count (ideal to create a texture array)</returns>
		public static Image<PF>[]	LoadFromTPage( Device _Device, string _Name, int _SpriteWidth, int _SpriteHeight, System.Drawing.Bitmap _TPage, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			// Create the crop rectangles
			int	RectanglesCountX = _TPage.Width / _SpriteWidth;
			int	RectanglesCountY = _TPage.Height / _SpriteHeight;

			System.Drawing.Rectangle[]	CropRectangles = new System.Drawing.Rectangle[RectanglesCountX*RectanglesCountY];
			for ( int RectangleIndexX=0; RectangleIndexX < RectanglesCountX; RectangleIndexX++ )
				for ( int RectangleIndexY=0; RectangleIndexY < RectanglesCountY; RectangleIndexY++ )
					CropRectangles[RectangleIndexY*RectanglesCountX+RectangleIndexX] = new System.Drawing.Rectangle(
						RectangleIndexX * _SpriteWidth,
						RectangleIndexY * _SpriteHeight,
						_SpriteWidth,
						_SpriteHeight
						);

			// Create the sprites
			return LoadFromTPage( _Device, _Name, CropRectangles, _TPage, _MipLevelsCount, _ImageGamma, _PreProcess );
		}
		public static Image<PF>[]	LoadFromTPage( Device _Device, string _Name, int _SpriteWidth, int _SpriteHeight, System.Drawing.Bitmap _TPage, int _MipLevelsCount, float _ImageGamma )
		{
			return LoadFromTPage( _Device, _Name, _SpriteWidth, _SpriteHeight, _TPage, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Creates an array of images from a single Texture Page
		/// </summary>
		/// <param name="_CropRectangles">The array of crop rectangles to isolate individual sprites</param>
		/// <param name="_TPage">The source TPage</param>
		/// <param name="_MipLevelsCount">The amount of mip levels to create for each image</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static Image<PF>[]	LoadFromTPage( Device _Device, string _Name, System.Drawing.Rectangle[] _CropRectangles, System.Drawing.Bitmap _TPage, int _MipLevelsCount, float _ImageGamma, ImageProcessDelegate _PreProcess )
		{
			int		Width, Height;
			byte[]	ImageContent = LoadBitmap( _TPage, out Width, out Height );
			int		Offset;

			Image<PF>[]	Result = new Image<PF>[_CropRectangles.Length];
			for ( int CropRectangleIndex=0; CropRectangleIndex < _CropRectangles.Length; CropRectangleIndex++ )
			{
				System.Drawing.Rectangle	Rect = _CropRectangles[CropRectangleIndex];
				Result[CropRectangleIndex] = new Image<PF>( _Device, _Name, Rect.Width, Rect.Height,
					( int _X, int _Y, ref Vector4 _Color ) =>
						{
							Offset = ((Rect.Y + _Y) + (Rect.X + _X)) << 2;

							_Color.X = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset++], _ImageGamma );
							_Color.Y = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset++], _ImageGamma );
							_Color.Z = GammaUnCorrect( BYTE_TO_FLOAT * ImageContent[Offset++], _ImageGamma );
							_Color.W = BYTE_TO_FLOAT * ImageContent[Offset++];

							if ( _PreProcess != null )
								_PreProcess( _X, _Y, ref _Color );
						},
						_MipLevelsCount );
			}

			return Result;
		}
		public static Image<PF>[]	LoadFromTPage( Device _Device, string _Name, System.Drawing.Rectangle[] _CropRectangles, System.Drawing.Bitmap _TPage, int _MipLevelsCount, float _ImageGamma )
		{
			return LoadFromTPage( _Device, _Name, _CropRectangles, _TPage, _MipLevelsCount, _ImageGamma, null );
		}

		/// <summary>
		/// Loads a bitmap from a stream into a byte[] of RGBA values
		/// Read the array like this :
		/// byte R = ReturnedArray[((Width*Y+X)<<2) + 0];
		/// byte G = ReturnedArray[((Width*Y+X)<<2) + 1];
		/// byte B = ReturnedArray[((Width*Y+X)<<2) + 2];
		/// byte A = ReturnedArray[((Width*Y+X)<<2) + 3];
		/// </summary>
		/// <param name="_BitmapStream"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <returns></returns>
		public static byte[]	LoadBitmap( System.IO.Stream _BitmapStream, out int _Width, out int _Height )
		{
			using ( System.Drawing.Bitmap Bitmap = System.Drawing.Bitmap.FromStream( _BitmapStream ) as System.Drawing.Bitmap )
			{
				return LoadBitmap( Bitmap, out _Width, out _Height );
			}
		}

		/// <summary>
		/// Loads a bitmap into a byte[] of RGBA values
		/// Read the array like this :
		/// byte R = ReturnedArray[((Width*Y+X)<<2) + 0];
		/// byte G = ReturnedArray[((Width*Y+X)<<2) + 1];
		/// byte B = ReturnedArray[((Width*Y+X)<<2) + 2];
		/// byte A = ReturnedArray[((Width*Y+X)<<2) + 3];
		/// </summary>
		/// <param name="_BitmapStream"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <returns></returns>
		public static unsafe byte[]	LoadBitmap( System.Drawing.Bitmap _Bitmap, out int _Width, out int _Height )
		{
			byte[]	Result = null;
			byte*	pScanline;
			byte	R, G, B, A;

			_Width = _Bitmap.Width;
			_Height = _Bitmap.Height;
			Result = new byte[4*_Width*_Height];

			System.Drawing.Imaging.BitmapData	LockedBitmap = _Bitmap.LockBits( new System.Drawing.Rectangle( 0, 0, _Width, _Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

			for ( int Y=0; Y < _Height; Y++ )
			{
				pScanline = (byte*) LockedBitmap.Scan0.ToPointer() + Y * LockedBitmap.Stride;
				for ( int X=0; X < _Width; X++ )
				{
					// Read in shitty order
					B = *pScanline++;
					G = *pScanline++;
					R = *pScanline++;
					A = *pScanline++;

					// Write in correct order
					Result[((_Width*Y+X)<<2) + 0] = R;
					Result[((_Width*Y+X)<<2) + 1] = G;
					Result[((_Width*Y+X)<<2) + 2] = B;
					Result[((_Width*Y+X)<<2) + 3] = A;
				}
			}
			_Bitmap.UnlockBits( LockedBitmap );

			return Result;
		}

		/// <summary>
		/// Applies gamma correction to the provided color
		/// </summary>
		/// <param name="c">The color to gamma-correct</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static float		GammaCorrect( float c, float _ImageGamma )
		{
			if ( _ImageGamma == GAMMA_SRGB )
				return Linear2sRGB( c );

			return (float) Math.Pow( c, 1.0f / _ImageGamma );
		}

		/// <summary>
		/// Un-aplies gamma correction to the provided color
		/// </summary>
		/// <param name="c">The color to gamma-uncorrect</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public static float		GammaUnCorrect( float c, float _ImageGamma )
		{
			if ( _ImageGamma == GAMMA_SRGB )
				return sRGB2Linear( c );

			return (float) Math.Pow( c, _ImageGamma );
		}

		/// <summary>
		/// Converts from linear space to sRGB
		/// Code borrowed from D3DX_DXGIFormatConvert.inl from the DX10 SDK
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public static float		Linear2sRGB( float c )
		{
			if ( c < 0.0031308f )
				return c * 12.92f;

			return 1.055f * (float) Math.Pow( c, 1.0f / 2.4f ) - 0.055f;
		}

		/// <summary>
		/// Converts from sRGB to linear space
		/// Code borrowed from D3DX_DXGIFormatConvert.inl from the DX10 SDK
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public static float		sRGB2Linear( float c )
		{
			if ( c < 0.04045f )
				return c / 12.92f;

			return (float) Math.Pow( (c + 0.055f) / 1.055f, 2.4f );
		}

		#endregion
	}
}
