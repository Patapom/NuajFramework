using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj
{
	/// <summary>
	/// This is the base 3D image class that loads images into different supported formats so they can later be provided to 3D textures
	/// This class is also able to create mip maps from a source image
	/// You can find various existing pixel formats in the PixelFormat.cs file
	/// </summary>
	public class Image3D<PF> : Component where PF:struct,IPixelFormat
	{
		#region CONSTANTS

		protected const float	BYTE_TO_FLOAT = 1.0f / 255.0f;

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// A delegate used to write a pixel from a custom image constructor
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Y"></param>
		/// <param name="_Color"></param>
		public delegate void	PixelWriterDelegate( int _X, int _Y, int _Z, ref Vector4 _Color );

		#endregion

		#region FIELDS

		protected int				m_Width = 0;
		protected int				m_Height = 0;
		protected int				m_Depth = 0;
		protected int				m_MipLevelsCount = 1;
		protected bool				m_bHasAlpha = false;
		protected DataStream[]		m_DataStreams = null;
		protected DataBox[]			m_DataBoxes = null;		// Data boxes for all mip levels

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the image width
		/// </summary>
		public int			Width				{ get { return m_Width; } }

		/// <summary>
		/// Gets the image height
		/// </summary>
		public int			Height				{ get { return m_Height; } }

		/// <summary>
		/// Gets the image depth
		/// </summary>
		public int			Depth				{ get { return m_Depth; } }

		/// <summary>
		/// Gets the amount of mip levels for the image
		/// </summary>
		public int			MipLevelsCount		{ get { return m_MipLevelsCount; } }

		/// <summary>
		/// Tells if the image has an alpha channel
		/// </summary>
		public bool			HasAlpha			{ get { return m_bHasAlpha; } }

		/// <summary>
		/// Gets the image's data stream for all mip levels
		/// </summary>
		public DataStream[]	DataStream			{ get { return m_DataStreams; } }

		/// <summary>
		/// Gets the DataBoxes for all mip levels
		/// </summary>
		public DataBox[]	DataBoxes			{ get { return m_DataBoxes; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a 3D image placeholder
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public	Image3D( Device _Device, string _Name, int _Width, int _Height, int _Depth, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_Width = _Width;
			m_Height = _Height;
			m_Depth = _Depth;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataBoxes = new DataBox[m_MipLevelsCount];
		}

		/// <summary>
		/// Creates a 3D image from a bitmaps array
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">The array of bitmaps that will determine the depth of the 3D image</param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <remarks>All the bitmaps in the array must have the same size</remarks>
		public	Image3D( Device _Device, string _Name, System.Drawing.Bitmap[] _Images, int _MipLevelsCount, float _ImageGamma ) : base( _Device, _Name )
		{
			m_Width = _Images[0].Width;
			m_Height = _Images[0].Height;
			m_Depth = _Images.Length;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataBoxes = new DataBox[m_MipLevelsCount];

			Load( _Images, _ImageGamma );
		}

		/// <summary>
		/// Creates an image from a 2D images array
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">The array of images that will determine the depth of the 3D image</param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <remarks>All the images in the array must have the same size</remarks>
		public	Image3D( Device _Device, string _Name, Image<PF>[] _Images, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_Width = _Images[0].Width;
			m_Height = _Images[0].Height;
			m_Depth = _Images.Length;
			m_MipLevelsCount = _Images[0].MipLevelsCount;
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataBoxes = new DataBox[m_MipLevelsCount];

			Load( _Images );
		}

		/// <summary>
		/// Creates a custom image using a pixel writer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_Depth"></param>
		/// <param name="_PixelWriter">The delegate that will be called to generate the image data for each 3D position</param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public unsafe	Image3D( Device _Device, string _Name, int _Width, int _Height, int _Depth, PixelWriterDelegate _PixelWriter, int _MipLevelsCount ) : base( _Device, _Name )
		{
			if ( _PixelWriter == null )
				throw new NException( this, "Invalid pixel writer !" );

			m_Width = _Width;
			m_Height = _Height;
			m_Depth = _Depth;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			m_DataStreams = new DataStream[m_MipLevelsCount];
			m_DataBoxes = new DataBox[m_MipLevelsCount];

			int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

			// Create the data box for level #0
			try
			{
				PF[]	Scanline = new PF[m_Width];
				m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * m_Depth * PixelSize, true, true ) );

				// Convert all scanlines into the desired format and write them into the stream
				Vector4	C = new Vector4();
				for ( int Z=0; Z < m_Depth; Z++ )
				{
					for ( int Y=0; Y < m_Height; Y++ )
					{
						for ( int X=0; X < m_Width; X++ )
						{
							_PixelWriter( X, Y, Z, ref C );
							Scanline[X].Write( C );

							m_bHasAlpha |= C.W != 1.0f;	// Check if it has alpha...
						}

						m_DataStreams[0].WriteRange<PF>( Scanline );
					}
				}

				// Build the data rectangle from that stream
				m_DataBoxes[0] = new DataBox( m_DataStreams[0].DataPointer, m_Width*PixelSize, m_Width*m_Height*PixelSize );

				// Build mip levels
				BuildMissingMipLevels();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while creating the custom image !", _e );
			}
		}

		/// <summary>
		/// Loads a 3D LDR image from a bunch of 2D bitmaps
		/// </summary>
		/// <param name="_Images">Source images to load</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public unsafe void	Load( System.Drawing.Bitmap[] _Images, float _ImageGamma )
		{
			if ( _Images.Length != m_Depth )
				throw new NException( this, "Provided images count and depth mismatch !" );

			int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

			m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * m_Depth * PixelSize, true, true ) );

			// Write each slice
			PF[]	Scanline = new PF[m_Width];
			for ( int Z=0; Z < m_Depth; Z++ )
			{
				if ( _Images[Z].Width != m_Width )
					throw new NException( this, "Provided image at Z=" + Z + " width mismatch !" );
				if ( _Images[Z].Height != m_Height )
					throw new NException( this, "Provided image at Z=" + Z + " height mismatch !" );

				// Lock source image
				System.Drawing.Imaging.BitmapData	LockedBitmap = _Images[Z].LockBits( new System.Drawing.Rectangle( 0, 0, m_Width, m_Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				// Create the data rectangle
				try
				{
					// Convert all scanlines into the desired format and write them into the stream
					Vector4	Temp = new Vector4();
					for ( int Y=0; Y < m_Height; Y++ )
					{
						byte*	pScanline = (byte*) LockedBitmap.Scan0.ToPointer() + LockedBitmap.Stride * Y;
						for ( int X=0; X < m_Width; X++ )
						{
							Temp.Z = Image<PF>.GammaUnCorrect( BYTE_TO_FLOAT * *pScanline++, _ImageGamma );
							Temp.Y = Image<PF>.GammaUnCorrect( BYTE_TO_FLOAT * *pScanline++, _ImageGamma );
							Temp.X = Image<PF>.GammaUnCorrect( BYTE_TO_FLOAT * *pScanline++, _ImageGamma );
							Temp.W = BYTE_TO_FLOAT * *pScanline++;

							Scanline[X].Write( Temp );

							m_bHasAlpha |= Scanline[X].Alpha != 1.0f;	// Check if it has alpha...
						}

						m_DataStreams[0].WriteRange<PF>( Scanline );
					}
				}
				catch ( Exception _e )
				{
					throw new NException( this, "An error occurred while loading the image !", _e );
				}
				finally
				{
					_Images[Z].UnlockBits( LockedBitmap );
				}
			}

			// Build the data rectangle from that stream
			m_DataBoxes[0] = new DataBox( m_DataStreams[0].DataPointer, m_Width*PixelSize, m_Width*m_Height*PixelSize );

			// Build mip levels
			BuildMissingMipLevels();
		}

		/// <summary>
		/// Loads a 3D LDR image from a bunch of 2D images
		/// </summary>
		/// <param name="_Images">Source images to load</param>
		public unsafe void	Load( Image<PF>[] _Images )
		{
			if ( _Images.Length != m_Depth )
				throw new NException( this, "Provided images count and depth mismatch !" );

			int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

			m_DataStreams[0] = ToDispose( new DataStream( m_Width * m_Height * m_Depth * PixelSize, true, true ) );

			// Write each slice
			for ( int Z=0; Z < m_Depth; Z++ )
			{
				if ( _Images[Z].Width != m_Width )
					throw new NException( this, "Provided image at Z=" + Z + " width mismatch !" );
				if ( _Images[Z].Height != m_Height )
					throw new NException( this, "Provided image at Z=" + Z + " width mismatch !" );

				m_DataStreams[0].WriteRange<PF>( _Images[Z].DataStreams[0].ReadRange<PF>( m_Width * m_Height ) );
			}

			// Build the data rectangle from that stream
			m_DataBoxes[0] = new DataBox( m_DataStreams[0].DataPointer, m_Width*PixelSize, m_Width*m_Height*PixelSize );

			// Build mip levels
			BuildMissingMipLevels();
		}

		/// <summary>
		/// Computes the correct amount of mip levels given an input levels count
		/// If the count is 0 then all possible mip levels are generated
		/// </summary>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		protected int	ComputeMipLevelsCount( int _MipLevelsCount )
		{
			int	MaxSize = Math.Max( m_Width, m_Height );
				MaxSize = Math.Max( MaxSize, m_Depth );
			int	MaxMipLevelsCount = (int) Math.Ceiling( Math.Log( MaxSize+1 ) / Math.Log( 2.0 ) );
			return	_MipLevelsCount == 0 ? MaxMipLevelsCount : Math.Min( MaxMipLevelsCount, _MipLevelsCount );
		}

		/// <summary>
		/// Builds the missing mip levels assuming level 0 is already computed
		/// </summary>
		protected void	BuildMissingMipLevels()
		{
			int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

			int			SourceLevelIndex = 0;
			DataStream	SourceStream = m_DataStreams[SourceLevelIndex];
			DataBox		SourceBox = m_DataBoxes[SourceLevelIndex];
			int			SourceWidth = m_Width;
			int			SourceHeight = m_Height;
			int			SourceDepth = m_Depth;

			SourceStream.Position = 0;

			while ( SourceLevelIndex < m_MipLevelsCount-1 )
			{
				int	CurrentLevelIndex = SourceLevelIndex+1;
				int	CurrentWidth = Math.Max( 1, SourceWidth >> 1 );
				int	CurrentHeight = Math.Max( 1, SourceHeight >> 1 );
				int	CurrentDepth = Math.Max( 1, SourceDepth >> 1 );

				// Build mip level stream
				DataStream	CurrentStream = ToDispose( new DataStream( CurrentWidth * CurrentHeight * CurrentDepth * PixelSize, true, true ) );
				m_DataStreams[CurrentLevelIndex] = CurrentStream;

				// Read scanlines 2 by 2
				PF[]	Scanline00 = new PF[SourceWidth];
				PF[]	Scanline01 = new PF[SourceWidth];
				PF[]	ResultScanline = new PF[CurrentWidth];

				int		SliceSize = SourceWidth*SourceHeight*PixelSize;
				if ( SourceDepth > 1 )
				{	// Treat 2 slices at the same time
					PF[]	Scanline10 = new PF[SourceWidth];
					PF[]	Scanline11 = new PF[SourceWidth];

					for ( int Z=0; Z < CurrentDepth; Z++ )
					{
						DataStream	SourceStream0 = new DataStream( SourceStream.DataPointer, SourceStream.Length, true, true );
									SourceStream0.Position = (2*Z+0) * SliceSize;
						DataStream	SourceStream1 = new DataStream( SourceStream.DataPointer, SourceStream.Length, true, true );
									SourceStream1.Position = (2*Z+1) * SliceSize;

						if ( SourceHeight > 1 )
							for ( int Y=0; Y < CurrentHeight; Y++ )
							{
								SourceStream0.ReadRange<PF>( Scanline00, 0, SourceWidth );
								SourceStream0.ReadRange<PF>( Scanline01, 0, SourceWidth );
								SourceStream1.ReadRange<PF>( Scanline10, 0, SourceWidth );
								SourceStream1.ReadRange<PF>( Scanline11, 0, SourceWidth );

								Vector4	AverageColor = new Vector4();
								if ( SourceWidth > 1 )
								{	// Perform averaging of pixels 8 by 8
									for ( int X=0; X < CurrentWidth; X++ )
									{
										AverageColor.X  = Scanline00[2*X+0].Red + Scanline00[2*X+1].Red + Scanline01[2*X+0].Red + Scanline01[2*X+1].Red;
										AverageColor.X += Scanline10[2*X+0].Red + Scanline10[2*X+1].Red + Scanline11[2*X+0].Red + Scanline11[2*X+1].Red;
										AverageColor.Y  = Scanline00[2*X+0].Green + Scanline00[2*X+1].Green + Scanline01[2*X+0].Green + Scanline01[2*X+1].Green;
										AverageColor.Y += Scanline10[2*X+0].Green + Scanline10[2*X+1].Green + Scanline11[2*X+0].Green + Scanline11[2*X+1].Green;
										AverageColor.Z  = Scanline00[2*X+0].Blue + Scanline00[2*X+1].Blue + Scanline01[2*X+0].Blue + Scanline01[2*X+1].Blue;
										AverageColor.Z += Scanline10[2*X+0].Blue + Scanline10[2*X+1].Blue + Scanline11[2*X+0].Blue + Scanline11[2*X+1].Blue;
										AverageColor.W  = Scanline00[2*X+0].Alpha + Scanline00[2*X+1].Alpha + Scanline01[2*X+0].Alpha + Scanline01[2*X+1].Alpha;
										AverageColor.W += Scanline10[2*X+0].Alpha + Scanline10[2*X+1].Alpha + Scanline11[2*X+0].Alpha + Scanline11[2*X+1].Alpha;
										ResultScanline[X].Write( 0.125f * AverageColor );
									}
								}
								else
								{	// Average pixel from both lines
									AverageColor.X  = Scanline00[0].Red + Scanline01[0].Red;
									AverageColor.X += Scanline10[0].Red + Scanline11[0].Red;
									AverageColor.Y  = Scanline00[0].Green + Scanline01[0].Green;
									AverageColor.Y += Scanline10[0].Green + Scanline11[0].Green;
									AverageColor.Z  = Scanline00[0].Blue + Scanline01[0].Blue;
									AverageColor.Z += Scanline10[0].Blue + Scanline11[0].Blue;
									AverageColor.W  = Scanline00[0].Alpha + Scanline01[0].Alpha;
									AverageColor.W += Scanline10[0].Alpha + Scanline11[0].Alpha;
									ResultScanline[0].Write( 0.25f * AverageColor );
								}

								CurrentStream.WriteRange<PF>( ResultScanline );
							}
						else
						{	// Treat a single line
							SourceStream0.ReadRange<PF>( Scanline00, 0, SourceWidth );
							SourceStream1.ReadRange<PF>( Scanline10, 0, SourceWidth );

							Vector4	AverageColor = new Vector4();

							// Average pixels 4 by 4 from our single line
							for ( int X=0; X < CurrentWidth; X++ )
							{
								AverageColor.X  = Scanline00[2*X+0].Red + Scanline00[2*X+1].Red;
								AverageColor.X += Scanline10[2*X+0].Red + Scanline10[2*X+1].Red;
								AverageColor.Y  = Scanline00[2*X+0].Green + Scanline00[2*X+1].Green;
								AverageColor.Y += Scanline10[2*X+0].Green + Scanline10[2*X+1].Green;
								AverageColor.Z  = Scanline00[2*X+0].Blue + Scanline00[2*X+1].Blue;
								AverageColor.Z += Scanline10[2*X+0].Blue + Scanline10[2*X+1].Blue;
								AverageColor.W  = Scanline00[2*X+0].Alpha + Scanline00[2*X+1].Alpha;
								AverageColor.W += Scanline10[2*X+0].Alpha + Scanline10[2*X+1].Alpha;
								ResultScanline[X].Write( 0.25f * AverageColor );
							}

							CurrentStream.WriteRange<PF>( ResultScanline );
						}

						// Dispose of streams
						SourceStream0.Dispose();
						SourceStream1.Dispose();
					}
				}
				else
				{	// Treat a single slice
					DataStream	SourceStream0 = SourceStream;

					if ( SourceHeight > 1 )
						for ( int Y=0; Y < CurrentHeight; Y++ )
						{
							SourceStream0.ReadRange<PF>( Scanline00, 0, SourceWidth );
							SourceStream0.ReadRange<PF>( Scanline01, 0, SourceWidth );

							Vector4	AverageColor = new Vector4();
							if ( SourceWidth > 1 )
							{	// Perform averaging of pixels 4 by 4
								for ( int X=0; X < CurrentWidth; X++ )
								{
									AverageColor.X = Scanline00[2*X+0].Red + Scanline00[2*X+1].Red + Scanline01[2*X+0].Red + Scanline01[2*X+1].Red;
									AverageColor.Y = Scanline00[2*X+0].Green + Scanline00[2*X+1].Green + Scanline01[2*X+0].Green + Scanline01[2*X+1].Green;
									AverageColor.Z = Scanline00[2*X+0].Blue + Scanline00[2*X+1].Blue + Scanline01[2*X+0].Blue + Scanline01[2*X+1].Blue;
									AverageColor.W = Scanline00[2*X+0].Alpha + Scanline00[2*X+1].Alpha + Scanline01[2*X+0].Alpha + Scanline01[2*X+1].Alpha;
									ResultScanline[X].Write( 0.25f * AverageColor );
								}
							}
							else
							{	// Average pixel from both lines
								AverageColor.X = Scanline00[0].Red + Scanline01[0].Red;
								AverageColor.Y = Scanline00[0].Green + Scanline01[0].Green;
								AverageColor.Z = Scanline00[0].Blue + Scanline01[0].Blue;
								AverageColor.W = Scanline00[0].Alpha + Scanline01[0].Alpha;
								ResultScanline[0].Write( 0.5f * AverageColor );
							}

							CurrentStream.WriteRange<PF>( ResultScanline );
						}
					else
					{	// Treat a single line
						SourceStream0.ReadRange<PF>( Scanline00, 0, SourceWidth );

						Vector4	AverageColor = new Vector4();

						// Average pixels 2 by 2 from our single line
						for ( int X=0; X < CurrentWidth; X++ )
						{
							AverageColor.X = Scanline00[2*X+0].Red + Scanline00[2*X+1].Red;
							AverageColor.Y = Scanline00[2*X+0].Green + Scanline00[2*X+1].Green;
							AverageColor.Z = Scanline00[2*X+0].Blue + Scanline00[2*X+1].Blue;
							AverageColor.W = Scanline00[2*X+0].Alpha + Scanline00[2*X+1].Alpha;
							ResultScanline[X].Write( 0.5f * AverageColor );
						}

						CurrentStream.WriteRange<PF>( ResultScanline );
					}
				}

				// Create final data rectangle
				m_DataBoxes[CurrentLevelIndex] = new DataBox( CurrentStream.DataPointer, CurrentWidth * PixelSize, CurrentWidth * CurrentHeight * PixelSize );

				// Rewind streams
				SourceStream.Position = 0;
				CurrentStream.Position = 0;

				// Scroll data
				SourceLevelIndex = CurrentLevelIndex;
				SourceStream = m_DataStreams[CurrentLevelIndex];
				SourceBox = m_DataBoxes[CurrentLevelIndex];
				SourceWidth = CurrentWidth;
 				SourceHeight = CurrentHeight;
 				SourceDepth = CurrentDepth;
			}
		}

		#endregion
	}
}
