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
	/// This is the image cube class that loads 6 images to create a cube map
	/// There are various ways of creating a cube map, as can be seen through the many constructors
	/// _ You can create LDR cube maps from 6 images or 6 images and their 6 alphas
	/// _ You can create HDR cube maps from 6 HDR images
	/// _ You can create LDR or HDR cube maps from "formatted images" like horizontal/vertical crosses or probes
	/// 
	/// See the examples below.
	/// </summary>
	/// <example>
	/// To load a LDR cube map using 6 bitmaps you can use :
	///	 System.Drawing.Bitmap B0 = Some bitmap for +X face
	///	 System.Drawing.Bitmap B1 = Some bitmap for -X face
	///	 System.Drawing.Bitmap B2 = Some bitmap for +Y face
	///	 System.Drawing.Bitmap B3 = Some bitmap for -Y face
	///	 System.Drawing.Bitmap B4 = Some bitmap for +Z face
	///	 System.Drawing.Bitmap B5 = Some bitmap for -Z face
	/// 	using ( ImageCube&lt;PF_RGBA8&gt; CubeImage = new ImageCube&lt;PF_RGBA8&gt;( m_Device, "CubeMap", new System.Drawing.Bitmap[] { B0, B1, B2, B3, B4, B5 }, 1 ) )
	/// 	
	/// To load a LDR Horizontal Cross cube map you can use :
	///	 using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "SomeHorizontalCrossCubeMap.jpg" ) as System.Drawing.Bitmap )
	/// 	using ( ImageCube&lt;PF_RGBA8&gt; CubeImage = new ImageCube&lt;PF_RGBA8&gt;( m_Device, "CubeMap", B, ImageCube&lt;PF_RGBA8&gt;.FORMATTED_IMAGE_TYPE.HORIZONTAL_CROSS, 1 ) )
	/// 
	/// To load a HDR Probe cube map you can use :
	///  Vector4[,]	HDRProbe = Image&lt;PF_RGBA8&gt;.LoadAndDecodeHDRFormat( Device.LoadFileContent( new System.IO.FileInfo( "Media/CubeMaps/galileo_probe.hdr" ) ) );
	///  using ( ImageCube&lt;PF_RGBA8&gt; CubeImage = new ImageCube&lt;PF_RGBA8&gt;( m_Device, "CubeMap", HDRProbe, ImageCube&lt;PF_RGBA8&gt;.FORMATTED_IMAGE_TYPE.PROBE, 0.0f, 1 ) )
	///	 	m_CubeMap = ToDispose( new Texture2D&lt;PF_RGBA8&gt;( m_Device, "CubeMap", CubeImage ) );
	/// 
	/// To load a DDS cube map, you don't need to use the ImageCube but can simply do :
	///	 m_CubeMap = ToDispose( Texture2D&lt;PF_RGBA8&gt;.CreateFromFile( m_Device, "CubeMap", new System.IO.FileInfo( "SomeCubeMap.dds" ) ) );
	/// 
	/// </example>
	public class ImageCube<PF> : Component where PF:struct,IPixelFormat
	{
		#region CONSTANTS

		protected const float	BYTE_TO_FLOAT = 1.0f / 255.0f;

		#endregion

		#region NESTED TYPES

		public enum	CUBE_FACE
		{
			X_POS,
			X_NEG,
			Y_POS,
			Y_NEG,
			Z_POS,
			Z_NEG,
		}

		public enum FORMATTED_IMAGE_TYPE
		{
			/// <summary>
			/// The 6 cube map faces are packed into a horizontal cross (+-)
			/// </summary>
			HORIZONTAL_CROSS,

			/// <summary>
			/// The 6 cube map faces are packed into a vertical cross (+)
			/// </summary>
			VERTICAL_CROSS,

			/// <summary>
			/// The 6 cube map faces are packed in a spherical probe map (i.e. phi around the circle, theta=0 at the center, theta=PI at the border)
			/// </summary>
			PROBE,
			/// <summary>
			/// The 6 cube map faces are packed in a cylindrical map (i.e. phi along X, theta along height)
			/// </summary>
			CYLINDRICAL,
			/// <summary>
			/// The 6 cube map faces are packed in half a cylindrical map (i.e. phi along X, theta [0,90] along height)
			/// </summary>
			HEMI_CYLINDRICAL,
			/// <summary>
			/// Only 4 cube map faces are packed in 4 aligned images, like the horizontal cross without the up/down images
			/// </summary>
			HORIZONTAL_BAND,
			/// <summary>
			/// All 6 cube faces are mapped with a single tiling image
			/// </summary>
			FACE_MAP,
		}

		/// <summary>
		/// A delegate used to write a pixel from a custom image constructor
		/// </summary>
		/// <param name="_Face">Cube face index</param>
		/// <param name="_X">X coordinate on that cube face</param>
		/// <param name="_Y">Y coordinate on that cube face</param>
		/// <param name="_Direction">Direction of the written pixel in LOCAL space (i.e. vector pointing toward the cube face)</param>
		/// <param name="_Color"></param>
		public delegate void	ImageProcessDelegate( CUBE_FACE _Face, int _X, int _Y, Vector3 _Direction, ref Vector4 _Color );

		#endregion

		#region FIELDS

		protected int				m_Size = 0;
		protected int				m_MipLevelsCount = 1;
		protected Image<PF>[]		m_Images = new Image<PF>[6];

		#endregion

		#region PROPERTIES

		public int			Size					{ get { return m_Size; } }
		public int			MipLevelsCount			{ get { return m_MipLevelsCount; } }
		public bool			HasAlpha				{ get { for ( int i=0; i < 6; i++ ) if ( m_Images[i].HasAlpha ) return true; return false; } }
		public Image<PF>	this[int _ImageIndex]	{ get { return m_Images[_ImageIndex]; } }
		public Image<PF>	this[CUBE_FACE _Face]	{ get { return m_Images[(int) _Face]; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an image placeholder
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public	ImageCube( Device _Device, string _Name, int _Size, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_Size = _Size;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			for ( int ImageIndex=0; ImageIndex < 6; ImageIndex++ )
				m_Images[ImageIndex] = ToDispose( new Image<PF>( m_Device, m_Name + ".Face#" + ImageIndex, m_Size, m_Size, m_MipLevelsCount ) );
		}

		/// <summary>
		/// Creates an image from 6 bitmaps
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">An array of 6 bitmaps</param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <remarks>NOTE: All the bitmaps in the array must have the same size and be square images</remarks>
		public	ImageCube( Device _Device, string _Name, System.Drawing.Bitmap[] _Images, int _MipLevelsCount, float _ImageGamma ) : base( _Device, _Name )
		{
			m_Size = _Images[0].Width;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );
			Load( _Images, _ImageGamma );
		}

		/// <summary>
		/// Creates an image from 6 bitmaps and 6 alphas
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">An array of 6 bitmaps</param>
		/// <param name="_Alphas">An array of 6 bitmaps used as alpha for the 6 above bitmaps</param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <remarks>NOTE: All the bitmaps in both arrays must have the same size and be square images</remarks>
		public	ImageCube( Device _Device, string _Name, System.Drawing.Bitmap[] _Images, System.Drawing.Bitmap[] _Alphas, int _MipLevelsCount, float _ImageGamma ) : base( _Device, _Name )
		{
			m_Size = _Images[0].Width;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );

			Load( _Images, _Alphas, _ImageGamma );
		}

		/// <summary>
		/// Creates an image from 6 HDR arrays
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		public	ImageCube( Device _Device, string _Name, Vector4[][,] _Images, float _Exposure, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_Size = _Images.GetLength( 1 );
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );

			Load( _Images, _Exposure );
		}

		/// <summary>
		/// Creates a LDR cube image from a single formatted bitmap
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">A single bitmap formatted in a special way described by the next parameter</param>
		/// <param name="_FormattedImage">The type of image formatting used</param>
		/// <param name="_MipLevelsCount">Amount of mip levels to generate (0 is for entire mip maps pyramid)</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public	ImageCube( Device _Device, string _Name, System.Drawing.Bitmap _FormattedImage, FORMATTED_IMAGE_TYPE _Type, int _MipLevelsCount, float _ImageGamma ) : base( _Device, _Name )
		{
			m_MipLevelsCount = _MipLevelsCount;	// Temporary, will be checked later once the image is loaded
			LoadFormattedImage( _FormattedImage, _Type, _ImageGamma );
		}

		/// <summary>
		/// Creates an HDR cube image from a single formatted bitmap
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">A single bitmap formatted in a special way described by the next parameter</param>
		/// <param name="_FormattedImage">The type of image formatting used</param>
		public	ImageCube( Device _Device, string _Name, Vector4[,] _FormattedImage, FORMATTED_IMAGE_TYPE _Type, float _Exposure, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_MipLevelsCount = _MipLevelsCount;	// Temporary, will be checked later once the image is loaded
			LoadFormattedImage( _FormattedImage, _Type, _Exposure );
		}

		/// <summary>
		/// Creates a cube image from a single formatted custom bitmap
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Writer">The pixel writer delegate to use to fetch image data</param>
		/// <param name="_FormattedImage">The type of image formatting used</param>
		public	ImageCube( Device _Device, string _Name, int _ImageWidth, int _ImageHeight, FORMATTED_IMAGE_TYPE _Type, ImageProcessDelegate _Writer, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_MipLevelsCount = _MipLevelsCount;	// Temporary, will be checked later once the image is loaded
			LoadFormattedImage( _ImageWidth, _ImageHeight, _Writer, _Type );
		}

		/// <summary>
		/// Creates a custom image using a pixel writer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	ImageCube( Device _Device, string _Name, int _Size, ImageProcessDelegate _PixelWriter, int _MipLevelsCount ) : base( _Device, _Name )
		{
			if ( _PixelWriter == null )
				throw new NException( this, "Invalid pixel writer !" );

			m_Size = _Size;
			m_MipLevelsCount = ComputeMipLevelsCount( _MipLevelsCount );

			for ( int ImageIndex=0; ImageIndex < 6; ImageIndex++ )
			{
				CUBE_FACE	Face = (CUBE_FACE) ImageIndex;
				Vector3		Dir = new Vector3();

				m_Images[ImageIndex] = ToDispose( new Image<PF>( m_Device, m_Name + ".Face#" + ImageIndex, m_Size, m_Size, ( int _X, int _Y, ref Vector4 _Color ) =>
					{
						ComputeDirectionFromCubeFace( Face, _X, _Y, ref Dir );
						_PixelWriter( Face, _X, _Y, Dir, ref _Color );
					}, m_MipLevelsCount ) );
			}
		}

		/// <summary>
		/// Loads an array of 6 LDR images from memory
		/// </summary>
		/// <param name="_Images">Source images to load</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public void	Load( System.Drawing.Bitmap[] _Images, float _ImageGamma )
		{
			if ( _Images.Length != 6 )
				throw new NException( this, "Provided images array must contain 6 images !" );

			for ( int ImageIndex=0; ImageIndex < 6; ImageIndex++ )
			{
				if ( _Images[ImageIndex].Width != m_Size )
					throw new NException( this, "Provided image widths mismatch ! All images must have the same size and be squares." );
				if ( _Images[ImageIndex].Height != m_Size )
					throw new NException( this, "Provided image heights mismatch ! All images must have the same size and be squares." );

				m_Images[ImageIndex] = ToDispose( new Image<PF>( m_Device, m_Name + ".Face#" + ImageIndex, _Images[ImageIndex], m_MipLevelsCount, _ImageGamma ) );
			}
		}

		/// <summary>
		/// Loads an array of 6 LDR images and their alphas from memory
		/// </summary>
		/// <param name="_Images">Source images to load</param>
		/// <param name="_Alphas">Source alphas to load</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		public unsafe void	Load( System.Drawing.Bitmap[] _Images, System.Drawing.Bitmap[] _Alphas, float _ImageGamma )
		{
			if ( _Images.Length != 6 )
				throw new NException( this, "Provided images array must contain 6 images !" );
			if ( _Alphas.Length != 6 )
				throw new NException( this, "Provided alphas array must contain 6 images !" );

			for ( int ImageIndex=0; ImageIndex < 6; ImageIndex++ )
			{
				if ( _Images[ImageIndex].Width != m_Size )
					throw new NException( this, "Provided image widths mismatch ! All images must have the same size and be squares." );
				if ( _Images[ImageIndex].Height != m_Size )
					throw new NException( this, "Provided image heights mismatch ! All images must have the same size and be squares." );
				if ( _Alphas[ImageIndex].Width != m_Size )
					throw new NException( this, "Provided alpha widths mismatch ! All images must have the same size and be squares." );
				if ( _Alphas[ImageIndex].Height != m_Size )
					throw new NException( this, "Provided alpha heights mismatch ! All images must have the same size and be squares." );

				m_Images[ImageIndex] = ToDispose( new Image<PF>( m_Device, m_Name + ".Face#" + ImageIndex, _Images[ImageIndex], _Alphas[ImageIndex], m_MipLevelsCount, _ImageGamma ) );
			}
		}

		/// <summary>
		/// Loads HDR images from memory
		/// </summary>
		/// <param name="_Images">Source images to load</param>
		/// <param name="_Exposure">The exposure correction to apply (default should be 0)</param>
		public void	Load( Vector4[][,] _Images, float _Exposure )
		{
			if ( _Images.Length != 6 )
				throw new NException( this, "Provided images array must contain 6 images !" );

			for ( int ImageIndex=0; ImageIndex < 6; ImageIndex++ )
			{
				if ( _Images[ImageIndex].GetLength( 0 ) != m_Size )
					throw new NException( this, "Provided image widths mismatch ! All images must have the same size and be squares." );
				if ( _Images[ImageIndex].GetLength( 1 ) != m_Size )
					throw new NException( this, "Provided image heights mismatch ! All images must have the same size and be squares." );

				m_Images[ImageIndex] = ToDispose( new Image<PF>( m_Device, m_Name + ".Face#" + ImageIndex, _Images[ImageIndex], _Exposure, m_MipLevelsCount ) );
			}
		}

		/// <summary>
		/// Computes the correct amount of mip levels given an input levels count
		/// If the count is 0 then all possible mip levels are generated
		/// </summary>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		protected int	ComputeMipLevelsCount( int _MipLevelsCount )
		{
			int	MaxMipLevelsCount = (int) Math.Ceiling( Math.Log( m_Size+1 ) / Math.Log( 2.0 ) );
			return	_MipLevelsCount == 0 ? MaxMipLevelsCount : Math.Min( MaxMipLevelsCount, _MipLevelsCount );
		}

		#region Formatted Image Loading

		private int									m_FaceIndex = 0;
		private System.Drawing.Imaging.BitmapData	m_LockedBitmap = null;
		private Vector4[,]							m_HDRImage = null;
		private float								m_HDRExposure = 0.0f;
		private float								m_GammaCorrection = 0.0f;
		private ImageProcessDelegate				m_CustomFetchPixel = null;

		protected unsafe void	FetchPixelLDR( int _X, int _Y, ref Vector4 _Color )
		{
			byte*	pPixel = (byte*) m_LockedBitmap.Scan0.ToPointer() + _Y*m_LockedBitmap.Stride + (_X<<2);
			_Color.Z = Image<PF>.GammaUnCorrect( BYTE_TO_FLOAT * *pPixel++, m_GammaCorrection );
			_Color.Y = Image<PF>.GammaUnCorrect( BYTE_TO_FLOAT * *pPixel++, m_GammaCorrection );
			_Color.X = Image<PF>.GammaUnCorrect( BYTE_TO_FLOAT * *pPixel++, m_GammaCorrection );
			_Color.W = BYTE_TO_FLOAT * *pPixel++;
		}

		protected unsafe void	FetchPixelHDR( int _X, int _Y, ref Vector4 _Color )
		{
			float	fLuminance = 0.3f * m_HDRImage[_X,_Y].X + 0.5f * m_HDRImage[_X,_Y].Y + 0.2f * m_HDRImage[_X,_Y].Z;
			float	fCorrectedLuminance = (float) Math.Pow( 2.0f, Math.Log( fLuminance ) / Math.Log( 2.0 ) + m_HDRExposure );
			float	fFactor = fCorrectedLuminance / fLuminance;

			_Color.X = fFactor * m_HDRImage[_X,_Y].X;
			_Color.Y = fFactor * m_HDRImage[_X,_Y].Y;
			_Color.Z = fFactor * m_HDRImage[_X,_Y].Z;
			_Color.W = m_HDRImage[_X,_Y].W;
		}

		private Vector3	Dir = new Vector3();
		protected unsafe void	FetchPixelCustom( int _X, int _Y, ref Vector4 _Color )
		{
			CUBE_FACE	Face = (CUBE_FACE) m_FaceIndex;
			ComputeDirectionFromCubeFace( Face, _X, _Y, ref Dir );
			m_CustomFetchPixel( Face, _X, _Y, Dir, ref _Color );
		}

		protected unsafe void	LoadFormattedImage( System.Drawing.Bitmap _Image, FORMATTED_IMAGE_TYPE _Type, float _ImageGamma )
		{
			try
			{
				m_LockedBitmap = _Image.LockBits( new System.Drawing.Rectangle( 0, 0, _Image.Width, _Image.Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
				m_GammaCorrection = _ImageGamma;
				LoadFormattedImage( _Image.Width, _Image.Height, new Image<PF>.ImageProcessDelegate( FetchPixelLDR ), _Type );
			}
			catch ( Exception ) { throw; }
			finally
			{
				if ( m_LockedBitmap != null )
					_Image.UnlockBits( m_LockedBitmap );
			}
		}

		protected void	LoadFormattedImage( Vector4[,] _Image, FORMATTED_IMAGE_TYPE _Type, float _Exposure )
		{
			m_HDRImage = _Image;
			m_HDRExposure = _Exposure;
			LoadFormattedImage( _Image.GetLength( 0 ), _Image.GetLength( 1 ), new Image<PF>.ImageProcessDelegate( FetchPixelHDR ), _Type );
		}

		protected void	LoadFormattedImage( int _ImageWidth, int _ImageHeight, ImageProcessDelegate _FetchPixel, FORMATTED_IMAGE_TYPE _Type )
		{
			m_CustomFetchPixel = _FetchPixel;
			LoadFormattedImage( _ImageWidth, _ImageHeight, new Image<PF>.ImageProcessDelegate( FetchPixelCustom ), _Type );
		}

		protected unsafe void	LoadFormattedImage( int _ImageWidth, int _ImageHeight, Image<PF>.ImageProcessDelegate _FetchPixel, FORMATTED_IMAGE_TYPE _Type )
		{
			// Handle cases
			Image<PF>.ImageProcessDelegate	Delegate = null;
			switch ( _Type )
			{
				case FORMATTED_IMAGE_TYPE.HORIZONTAL_CROSS:
					{
						// The horizontal cross model has cube faces oriented as follows :
						//   U
						//  LFRB
						//   D

						// Retrieve image size from the image's dimensions
						m_Size = _ImageWidth >> 2;	// 4 images in the length
						if ( _ImageHeight != 3*m_Size )
							throw new NException( this, "The provided image does not seem to fit the horizontal cross model !" );

						int[][]	Offsets = new int[6][]
						{
							new int[2] { 2, 1 },
							new int[2] { 0, 1 },
							new int[2] { 1, 0 },
							new int[2] { 1, 2 },
							new int[2] { 1, 1 },
							new int[2] { 3, 1 },
						};
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{
								_FetchPixel( m_Size*Offsets[m_FaceIndex][0]+_X, m_Size*Offsets[m_FaceIndex][1]+_Y, ref _Color );
							};
					}
					break;

				case FORMATTED_IMAGE_TYPE.VERTICAL_CROSS:
					{
						// The vertical cross model has cube faces oriented as follows :
						//   U
						//  LFR
						//   D
						//   B  <= Back is reversed vertically

						// Retrieve image size from the image's dimensions
						m_Size = _ImageHeight >> 2;	// 4 images in the length
						if ( _ImageWidth != 3*m_Size )
							throw new NException( this, "The provided image does not seem to fit the vertical cross model !" );

						int[][]	Offsets = new int[6][]
						{
							new int[2] { 2, 1 },
							new int[2] { 0, 1 },
							new int[2] { 1, 0 },
							new int[2] { 1, 2 },
							new int[2] { 1, 1 },
							new int[2] { 1, 3 },
						};
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{
								if ( m_FaceIndex == 5 )
									_FetchPixel( m_Size*Offsets[m_FaceIndex][0]+_X, m_Size*Offsets[m_FaceIndex][1]+m_Size-1-_Y, ref _Color );
								else
									_FetchPixel( m_Size*Offsets[m_FaceIndex][0]+_X, m_Size*Offsets[m_FaceIndex][1]+_Y, ref _Color );
							};
					}
					break;

				case FORMATTED_IMAGE_TYPE.PROBE:
					{
						// Probes cover a whole 4PI steradians in a single image
						// Here is the explanation from http://ict.debevec.org/~debevec/Probes/ :
						//
						// If we consider the images to be normalized to have coordinates u=[-1,1], v=[-1,1],
						//	we have theta=atan2(v,u), phi=pi*sqrt(u*u+v*v).
						// The unit vector pointing in the corresponding direction is obtained by rotating (0,0,-1)
						//	by phi degrees around the y (up) axis and then theta degrees around the -z (forward) axis.
						// If for a direction vector in the world (Dx, Dy, Dz), the corresponding (u,v) coordinate in
						//	the light probe image is (Dx*r,Dy*r) where r=(1/pi)*acos(Dz)/sqrt(Dx^2 + Dy^2).
						//
						m_Size = _ImageWidth / 4;	// Arbitrary

						Vector3	Dir = new Vector3();
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{
								ComputeDirectionFromCubeFace( (CUBE_FACE) m_FaceIndex, _X, _Y, ref Dir );
	
								// Apply coordinates transform
								double	D = Math.PI * Math.Sqrt( Dir.X*Dir.X+Dir.Y*Dir.Y );
								double	Radius = D > 1e-6f ? Math.Acos( Dir.Z ) / D : 0.0;
								double	U = Radius * Dir.X;
								double	V = Radius * Dir.Y;

								// Read pixel there
								_FetchPixel( (int) Math.Floor( 0.5 * (1.0 + U) * _ImageWidth ), (int) Math.Floor( 0.5 * (1.0 - V) * _ImageHeight ), ref _Color );
							};
					}
					break;

				case FORMATTED_IMAGE_TYPE.HORIZONTAL_BAND:
					{
						// Retrieve image size from the image's dimensions
						m_Size = _ImageHeight;	// 4 images in the length, 1 line of images
						if ( _ImageWidth != 4*m_Size )
							throw new NException( this, "The provided image does not seem to fit the horizontal band model !" );

						int[]	Offsets = new int[6]
						{
							2,
							0,
							-1,
							-1,
							1,
							3,
						};
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{
								if ( m_FaceIndex == 2 )
									_FetchPixel( 0, 0, ref _Color );
								else if ( m_FaceIndex == 3 )
									_FetchPixel( 0, m_Size-1, ref _Color );
								else
									_FetchPixel( m_Size*Offsets[m_FaceIndex]+_X, _Y, ref _Color );
							};
					}
					break;

				case FORMATTED_IMAGE_TYPE.HEMI_CYLINDRICAL:
					{
						// The hemicylindrical projection covers the top hemisphere of the cube map
						// Phi is along the width of the image and theta varies from 0 to PI/2 from the top to the bottom of the image
						m_Size = _ImageHeight;

						Vector3	Dir = new Vector3();
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{	
								ComputeDirectionFromCubeFace( (CUBE_FACE) m_FaceIndex, _X, _Y, ref Dir );

								// Convert to cylindrical coordinates
								double	Phi = Math.Atan2( Dir.Z, Dir.X );
								double	Theta = Math.Acos( Dir.Y );
								double	U = 0.5 * Phi / Math.PI;
										U = (1.0 + U) % 1.0;
								double	V = 2.0 * Theta / Math.PI;
										V = Math.Min( 1.0, V );

								// Read pixel there
								_FetchPixel( (int) Math.Floor( U * _ImageWidth ), Math.Min( _ImageHeight-1, (int) Math.Floor( V * _ImageHeight ) ), ref _Color );
							};
					}
					break;

				case FORMATTED_IMAGE_TYPE.CYLINDRICAL:
					{
						// The cylindrical projection covers both hemispheres of the cube map
						// Phi is along the width of the image and theta varies from 0 to PI from the top to the bottom of the image
						m_Size = _ImageHeight;

						Vector3	Dir = new Vector3();
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{
								ComputeDirectionFromCubeFace( (CUBE_FACE) m_FaceIndex, _X, _Y, ref Dir );

								// Convert to cylindrical coordinates
								double	Phi = Math.Atan2( Dir.Z, Dir.X );
								double	Theta = Math.Acos( Dir.Y );
								double	U = 0.5 * Phi / Math.PI;
										U = (1.0 + U) % 1.0;
								double	V = Theta / Math.PI;

								// Read pixel there
								_FetchPixel( (int) Math.Floor( U * _ImageWidth ), Math.Min( _ImageHeight-1, (int) Math.Floor( V * _ImageHeight ) ), ref _Color );
							};
					}
					break;

				case FORMATTED_IMAGE_TYPE.FACE_MAP:
					{
						m_Size = Math.Min( _ImageWidth, _ImageHeight );

						Vector2	DirX = new Vector2();
						Vector2	DirY = new Vector2();
						Delegate = ( int _X, int _Y, ref Vector4 _Color ) =>
							{	
								// Project X/Y on a sphere sextant
								DirX.X = 2.0f * _X / _ImageWidth - 1.0f;
								DirX.Y = 1.0f;
								DirX.Normalize();
								DirY.X = 2.0f * _Y / _ImageHeight - 1.0f;
								DirY.Y = 1.0f;
								DirY.Normalize();

								double	ThetaX = Math.Asin( DirX.X );
								double	ThetaY = Math.Asin( DirY.X );

								// Build normal and height from that position
								int		X = (int) (2.0 * _ImageWidth * (ThetaX + 0.25 * Math.PI) / Math.PI);
										X = Math.Min( _ImageWidth-1, X );
								int		Y = (int) (2.0 * _ImageHeight * (ThetaY + 0.25 * Math.PI) / Math.PI);
										Y = Math.Min( _ImageHeight-1, Y );

								// Read pixel there
								_FetchPixel( X, Y, ref _Color );
							};
					}
					break;

				default:
					throw new NException( this, "TODO!" );
			}

			// Update mip levels data
			m_MipLevelsCount = ComputeMipLevelsCount( m_MipLevelsCount );

			// Build the 6 images
			for ( m_FaceIndex=0; m_FaceIndex < 6; m_FaceIndex++ )
				m_Images[m_FaceIndex] = ToDispose( new Image<PF>( m_Device, m_Name + ".Face#" + m_FaceIndex, m_Size, m_Size, Delegate, m_MipLevelsCount ) );
		}

		#endregion

		/// <summary>
		/// Computes a normalized 3D vector starting from the cube map center and pointing toward the cube face
		/// given the cube face index and 2D coordinates within the face's image
		/// </summary>
		/// <param name="_Face"></param>
		/// <param name="_X"></param>
		/// <param name="_Y"></param>
		/// <param name="_Direction"></param>
		public void	ComputeDirectionFromCubeFace( CUBE_FACE _Face, int _X, int _Y, ref Vector3 _Direction )
		{
			// Transform into object space
			// Normalize in [-1,1]
			_Direction.X = 2.0f * _X / m_Size - 1.0f;
			_Direction.Y = 1.0f - 2.0f * _Y / m_Size;
			_Direction.Z = 1.0f;
			_Direction.Normalize();	// Normalize direction

			// Switch components according to cube face
			float	Temp;
			switch ( (int) _Face )
			{
				case 0:	// +X
					Temp = _Direction.X;
					_Direction.X = _Direction.Z;
					_Direction.Z = -Temp;
					break;
				case 1:	// -X
					Temp = _Direction.X;
					_Direction.X = -_Direction.Z;
					_Direction.Z = Temp;
					break;
				case 2:	// +Y
					Temp = _Direction.Y;
					_Direction.Y = _Direction.Z;
					_Direction.Z = -Temp;
					break;
				case 3:	// -Y
					Temp = _Direction.Y;
					_Direction.Y = -_Direction.Z;
					_Direction.Z = Temp;
					break;
				case 4:	// +Z
					break;
				case 5:	// -Z
					_Direction.X = -_Direction.X;
					_Direction.Z = -_Direction.Z;
					break;
			}
		}

		#endregion
	}
}
