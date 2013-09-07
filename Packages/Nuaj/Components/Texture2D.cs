using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D10.Buffer;

namespace Nuaj
{
	/// <summary>
	/// This wraps a DirectX 2D texture
	/// </summary>
	public class Texture2D<PF> : Component, ITexture2D where PF:struct,IPixelFormat
	{
		#region FIELDS

		// Standard default parameters
		protected int							m_Width = 0;
		protected int							m_Height = 0;
		protected Format						m_Format = Format.Unknown;
		protected int							m_ArraySize = 1;
		protected bool							m_bIsCubeMap = false;
		protected int							m_MipLevelsCount = 0;
		protected int							m_MultiSamplesCount = 1;
		protected bool							m_bHasAlpha = false;

		protected SharpDX.Direct3D10.Texture2D	m_Texture = null;
		protected ShaderResourceView			m_TextureView = null;
		protected ShaderResourceView[,]			m_TextureViewsSingle = null;
		protected ShaderResourceView[,]			m_TextureViewsMipBand = null;
		protected ShaderResourceView[,]			m_TextureViewsArrayBand = null;

		protected Vector2						m_Size2;
		protected Vector3						m_Size3;
		protected Vector2						m_InvSize2;
		protected Vector3						m_InvSize3;

		#endregion

		#region PROPERTIES

		#region ITexture2D Members

		public int		Width							{ get { return m_Width; } }
		public int		Height							{ get { return m_Height; } }
		public Format	Format							{ get { return m_Format; } }
		public bool		HasMipMaps						{ get { return m_MipLevelsCount > 1; } }
		public bool		HasAlpha						{ get { return m_bHasAlpha; } }
		public bool		IsCubeMap						{ get { return m_bIsCubeMap; } }
		public int		MipLevelsCount					{ get { return m_MipLevelsCount; } }
		public int		ArraySize						{ get { return m_ArraySize; } }
		public int		MultiSamplesCount				{ get { return m_MultiSamplesCount; } }
		public FormatSupport	Support					{ get { return m_Device.DirectXDevice.CheckFormatSupport( m_Format ); } }

		public SharpDX.Direct3D10.Texture2D	Texture		{ get { return m_Texture; } }
		public ShaderResourceView			TextureView	{ get { return m_TextureView; } }

		public float	AspectRatio						{ get { return (float) m_Width / m_Height; } }
		public Vector2	Size2							{ get { return m_Size2; } }
		public Vector3	Size3							{ get { return m_Size3; } }
		public Vector2	InvSize2						{ get { return m_InvSize2; } }
		public Vector3	InvSize3						{ get { return m_InvSize3; } }

		#endregion

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default Texture2D
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		protected	Texture2D( Device _Device, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize, int _MultiSamplesCount ) : base( _Device, _Name )
		{
			m_Width = _Width;
			m_Height = _Height;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_MipLevelsCount = CheckMipLevels( _MipLevelsCount );
			m_ArraySize = _ArraySize;
			m_MultiSamplesCount = Math.Min( Device.GetMaximumMSAASamples<PF>(), _MultiSamplesCount );

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );
		}

		/// <summary>
		/// Initializes the texture as a readonly (i.e. immutable) texture
		/// As such, the texture must be initialized with an image immediately
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Image">Initial image data (compulsory for immutable textures !)</param>
		public	Texture2D( Device _Device, string _Name, Image<PF> _Image ) : base( _Device, _Name )
		{
			if ( _Image == null )
				throw new NException( this, "Immutable textures must be provided their data at initialization !" );

			m_Width = _Image.Width;
			m_Height = _Image.Height;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_MipLevelsCount = _Image.MipLevelsCount;

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );

			Init( _Image );
		}

		/// <summary>
		/// Initializes the texture as a readonly (i.e. immutable) texture
		/// As such, the texture must be initialized with an image immediately
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Image">Initial image data (compulsory for immutable textures !)</param>
		public	Texture2D( Device _Device, string _Name, Image<PF> _Image, int _MultiSamplesCount ) : base( _Device, _Name )
		{
			if ( _Image == null )
				throw new NException( this, "Immutable textures must be provided their data at initialization !" );

			m_Width = _Image.Width;
			m_Height = _Image.Height;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_MipLevelsCount = _Image.MipLevelsCount;
			m_MultiSamplesCount = Math.Min( Device.GetMaximumMSAASamples<PF>(), _MultiSamplesCount );

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );

			Init( _Image );
		}

		/// <summary>
		/// Initializes the texture as a readonly (i.e. immutable) texture array
		/// As such, the texture must be initialized with an array of images immediately
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">Initial array of image data (compulsory for immutable textures !)
		/// NOTE: All the textures in the array must have the same size, format and mip-map depth</param>
		public	Texture2D( Device _Device, string _Name, Image<PF>[] _Images ) : base( _Device, _Name )
		{
			if ( _Images == null )
				throw new NException( this, "Immutable textures must be provided their data at initialization !" );

			m_Width = _Images[0].Width;
			m_Height = _Images[0].Height;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_ArraySize = _Images.Length;
			m_MipLevelsCount = _Images[0].MipLevelsCount;

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );

			InitArray( _Images );
		}

		/// <summary>
		/// Initializes the texture as a readonly (i.e. immutable) texture array
		/// As such, the texture must be initialized with an array of images immediately
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">Initial array of image data (compulsory for immutable textures !)
		/// NOTE: All the textures in the array must have the same size, format and mip-map depth</param>
		public	Texture2D( Device _Device, string _Name, Image<PF>[] _Images, int _MultiSamplesCount ) : base( _Device, _Name )
		{
			if ( _Images == null )
				throw new NException( this, "Immutable textures must be provided their data at initialization !" );

			m_Width = _Images[0].Width;
			m_Height = _Images[0].Height;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_MipLevelsCount = _Images[0].MipLevelsCount;
			m_ArraySize = _Images.Length;
			m_MultiSamplesCount = Math.Min( Device.GetMaximumMSAASamples<PF>(), _MultiSamplesCount );

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );

			InitArray( _Images );
		}

		/// <summary>
		/// Initializes the texture as a readonly (i.e. immutable) texture cube
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_ImageCube">Initial array of cube image data (compulsory for immutable textures !)</param>
		public	Texture2D( Device _Device, string _Name, ImageCube<PF> _ImageCube ) : base( _Device, _Name )
		{
			if ( _ImageCube == null )
				throw new NException( this, "Immutable textures must be provided their data at initialization !" );

			m_Width = _ImageCube.Size;
			m_Height = _ImageCube.Size;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_ArraySize = 6;
			m_bIsCubeMap = true;
			m_MipLevelsCount = _ImageCube.MipLevelsCount;

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );

			InitArray( new Image<PF>[] { _ImageCube[0], _ImageCube[1], _ImageCube[2], _ImageCube[3], _ImageCube[4], _ImageCube[5] } );
		}

		/// <summary>
		/// Copies this texture to the specified target
		/// </summary>
		/// <param name="_Target"></param>
		/// <remarks>The target texture must have identical dimensions and format</remarks>
		public void		CopyTo( ITexture2D _Target )
		{
			if ( _Target == null )
				throw new NException( this, "Invalid target texture to copy to !" );
			if ( _Target.Width != m_Width || _Target.Height != m_Height || _Target.ArraySize != m_ArraySize || _Target.MipLevelsCount != m_MipLevelsCount )
				throw new NException( this, "The provided target texture doesn't have the same dimensions or mip levels count as the source texture !" );

			m_Device.DirectXDevice.CopyResource( m_Texture, _Target.Texture );
		}

		protected virtual void	Init( Image<PF> _Image )
		{
			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.ArraySize = 1;
			Desc.BindFlags = BindFlags.ShaderResource;
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.SampleDescription = new SampleDescription( m_MultiSamplesCount, 0 );
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Immutable;

			if ( _Image != null )
			{
				m_bHasAlpha = _Image.HasAlpha;	// Copy alpha state
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc, _Image.DataRectangles ) );
			}
			else
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc ) );

			// Create the view
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
			ViewDesc.Format = m_Format;
			ViewDesc.Texture2D.MipLevels = m_MipLevelsCount;
			ViewDesc.Texture2D.MostDetailedMip = 0;

			m_TextureView = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			// Create an empty array of texture views that we will fill if Get????TextureView() gets called...
			m_TextureViewsSingle = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsMipBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsArrayBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
		}

		protected virtual void	InitArray( Image<PF>[] _Images )
		{
			if ( _Images.Length != m_ArraySize )
				throw new NException( this, "Images and array size mismatch !" );
			for ( int ImageIndex=1; ImageIndex < m_ArraySize; ImageIndex++ )
			{
				if ( _Images[ImageIndex].Width != _Images[0].Width )
					throw new NException( this, "Image widths mismatch !" );
				if ( _Images[ImageIndex].Height != _Images[0].Height )
					throw new NException( this, "Image heights mismatch !" );
				if ( _Images[ImageIndex].MipLevelsCount != _Images[0].MipLevelsCount )
					throw new NException( this, "Image mip levels count mismatch !" );

				m_bHasAlpha |= _Images[ImageIndex].HasAlpha;	// Copy alpha state
			}

			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.ArraySize = m_ArraySize;
			Desc.BindFlags = BindFlags.ShaderResource;
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.SampleDescription = new SampleDescription( m_MultiSamplesCount, 0 );
			Desc.OptionFlags = m_bIsCubeMap ? ResourceOptionFlags.TextureCube : ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Immutable;

			// Build the global array of data rectangles
 			// This array is first ordered by mip level then array index.
			DataRectangle[]	DataRectangles = new DataRectangle[m_MipLevelsCount * m_ArraySize];
			for ( int ImageIndex=0; ImageIndex < _Images.Length; ImageIndex++ )
				for ( int MipLevelIndex=0; MipLevelIndex < m_MipLevelsCount; MipLevelIndex++ )
					DataRectangles[ImageIndex*m_MipLevelsCount+MipLevelIndex] = _Images[ImageIndex].DataRectangles[MipLevelIndex];

			m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc, DataRectangles ) );

			// Create the view
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Format = m_Format;
			if ( m_ArraySize > 1 )
			{
				if ( !m_bIsCubeMap )
				{
					ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampledArray : ShaderResourceViewDimension.Texture2DArray;
					if ( m_MultiSamplesCount > 1 )
					{
						ViewDesc.Texture2DMSArray.ArraySize = m_ArraySize;
						ViewDesc.Texture2DMSArray.FirstArraySlice = 0;
					}
					else
					{
						ViewDesc.Texture2DArray.ArraySize = m_ArraySize;
						ViewDesc.Texture2DArray.FirstArraySlice = 0;
						ViewDesc.Texture2DArray.MipLevels = m_MipLevelsCount;
						ViewDesc.Texture2DArray.MostDetailedMip = 0;
					}
				}
				else
				{
					ViewDesc.Dimension = ShaderResourceViewDimension.TextureCube;
					ViewDesc.TextureCube.MipLevels = m_MipLevelsCount;
					ViewDesc.TextureCube.MostDetailedMip = 0;
				}
			}
			else
			{
				ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
				{
					ViewDesc.Texture2D.MipLevels = m_MipLevelsCount;
					ViewDesc.Texture2D.MostDetailedMip = 0;
				}
			}

			m_TextureView = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			// Create an empty array of texture views that we will fill if Get????TextureView() gets called...
			m_TextureViewsSingle = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsMipBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsArrayBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
		}

		public ShaderResourceView	GetSingleTextureView( int _MipLevelIndex, int _ArrayIndex )
		{
			if ( m_TextureViewsSingle[_MipLevelIndex,_ArrayIndex] != null )
				return m_TextureViewsSingle[_MipLevelIndex,_ArrayIndex];

			// Create the view for that particular array entry
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Format = m_Format;

			if ( m_ArraySize > 1 )
			{
				if ( !m_bIsCubeMap )
				{
					ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampledArray : ShaderResourceViewDimension.Texture2DArray;
					if ( m_MultiSamplesCount > 1 )
					{
						ViewDesc.Texture2DMSArray.FirstArraySlice = _ArrayIndex;
						ViewDesc.Texture2DMSArray.ArraySize = 1;
					}
					else
					{
						ViewDesc.Texture2DArray.FirstArraySlice = _ArrayIndex;
						ViewDesc.Texture2DArray.ArraySize = 1;
						ViewDesc.Texture2DArray.MostDetailedMip = _MipLevelIndex;
						ViewDesc.Texture2DArray.MipLevels = 1;
					}
				}
				else
				{
					ViewDesc.Dimension = ShaderResourceViewDimension.TextureCube;
					ViewDesc.TextureCube.MostDetailedMip = _MipLevelIndex;
					ViewDesc.TextureCube.MipLevels = 1;
				}
			}
			else
			{
				ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
				{
					ViewDesc.Texture2D.MostDetailedMip = _MipLevelIndex;
					ViewDesc.Texture2D.MipLevels = 1;
				}
			}

			m_TextureViewsSingle[_MipLevelIndex,_ArrayIndex] = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			return m_TextureViewsSingle[_MipLevelIndex,_ArrayIndex];
		}

		public ShaderResourceView	GetMipBandTextureView( int _MipLevelIndex, int _ArrayIndex )
		{
			if ( m_TextureViewsMipBand[_MipLevelIndex,_ArrayIndex] != null )
				return m_TextureViewsMipBand[_MipLevelIndex,_ArrayIndex];

			// Create the view for that particular array entry
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Format = m_Format;

			if ( m_ArraySize > 1 )
			{
				if ( !m_bIsCubeMap )
				{
					ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampledArray : ShaderResourceViewDimension.Texture2DArray;
					if ( m_MultiSamplesCount > 1 )
					{
						ViewDesc.Texture2DMSArray.FirstArraySlice = _ArrayIndex;
						ViewDesc.Texture2DMSArray.ArraySize = 1;
					}
					else
					{
						ViewDesc.Texture2DArray.FirstArraySlice = _ArrayIndex;
						ViewDesc.Texture2DArray.ArraySize = 1;
						ViewDesc.Texture2DArray.MostDetailedMip = _MipLevelIndex;
						ViewDesc.Texture2DArray.MipLevels = m_MipLevelsCount-_MipLevelIndex;
					}
				}
				else
				{
					ViewDesc.Dimension = ShaderResourceViewDimension.TextureCube;
					ViewDesc.TextureCube.MostDetailedMip = _MipLevelIndex;
					ViewDesc.TextureCube.MipLevels = m_MipLevelsCount-_MipLevelIndex;
				}
			}
			else
			{
				ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
				{
					ViewDesc.Texture2D.MostDetailedMip = _MipLevelIndex;
					ViewDesc.Texture2D.MipLevels = m_MipLevelsCount-_MipLevelIndex;
				}
			}

			m_TextureViewsMipBand[_MipLevelIndex,_ArrayIndex] = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			return m_TextureViewsMipBand[_MipLevelIndex,_ArrayIndex];
		}

		public ShaderResourceView	GetArrayBandTextureView( int _MipLevelIndex, int _ArrayIndex )
		{
			if ( m_TextureViewsArrayBand[_MipLevelIndex,_ArrayIndex] != null )
				return m_TextureViewsArrayBand[_MipLevelIndex,_ArrayIndex];

			// Create the view for that particular array entry
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Format = m_Format;

			if ( m_ArraySize > 1 )
			{
				if ( !m_bIsCubeMap )
				{
					ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampledArray : ShaderResourceViewDimension.Texture2DArray;
					if ( m_MultiSamplesCount > 1 )
					{
						ViewDesc.Texture2DMSArray.FirstArraySlice = _ArrayIndex;
						ViewDesc.Texture2DMSArray.ArraySize = m_ArraySize-_ArrayIndex;
					}
					else
					{
						ViewDesc.Texture2DArray.FirstArraySlice = _ArrayIndex;
						ViewDesc.Texture2DArray.ArraySize = m_ArraySize-_ArrayIndex;
						ViewDesc.Texture2DArray.MostDetailedMip = _MipLevelIndex;
						ViewDesc.Texture2DArray.MipLevels = 1;
					}
				}
				else
				{
					ViewDesc.Dimension = ShaderResourceViewDimension.TextureCube;
					ViewDesc.TextureCube.MostDetailedMip = _MipLevelIndex;
					ViewDesc.TextureCube.MipLevels = m_MipLevelsCount-_MipLevelIndex;
				}
			}
			else
			{
				ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
				{
					ViewDesc.Texture2D.MostDetailedMip = _MipLevelIndex;
					ViewDesc.Texture2D.MipLevels = 1;
				}
			}

			m_TextureViewsArrayBand[_MipLevelIndex,_ArrayIndex] = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			return m_TextureViewsArrayBand[_MipLevelIndex,_ArrayIndex];
		}

		/// <summary>
		/// Creates a texture from a bitmap file
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_FileName"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmapFile( Device _Device, string _Name, System.IO.FileInfo _FileName, int _MipLevelsCount, float _ImageGamma )
		{
			using ( Image<PF> I = Image<PF>.CreateFromBitmapFile( _Device, _Name, _FileName, _MipLevelsCount, _ImageGamma ) )
				return new Texture2D<PF>( _Device, _Name, I );
		}

		/// <summary>
		/// Creates a texture array from several bitmap files
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_FileNames"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmapFiles( Device _Device, string _Name, System.IO.FileInfo[] _FileNames, int _MipLevelsCount, float _ImageGamma )
		{
			// Create the array of images
			Image<PF>[]		Images = Image<PF>.CreateFromBitmapFiles( _Device, _Name, _FileNames, _MipLevelsCount, _ImageGamma );

			// Create the texture array
			Texture2D<PF>	Result = new Texture2D<PF>( _Device, _Name, Images );

			// Dispose of images
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				Images[ImageIndex].Dispose();

			return Result;
		}

		/// <summary>
		/// Creates a texture from a bitmap in memory
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapFileContent"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmapFileInMemory( Device _Device, string _Name, byte[] _BitmapFileContent, int _MipLevelsCount, float _ImageGamma )
		{
			using ( Image<PF> I = Image<PF>.CreateFromBitmapFileInMemory( _Device, _Name, _BitmapFileContent, _MipLevelsCount, _ImageGamma ) )
				return new Texture2D<PF>( _Device, _Name, I );
		}

		/// <summary>
		/// Creates a texture array from several bitmaps in memory
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapFileContents"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmapFilesInMemory( Device _Device, string _Name, byte[][] _BitmapFileContents, int _MipLevelsCount, float _ImageGamma )
		{
			// Create the array of images
			Image<PF>[]	Images = Image<PF>.CreateFromBitmapFilesInMemory( _Device, _Name, _BitmapFileContents, _MipLevelsCount, _ImageGamma );

			// Create the texture array
			Texture2D<PF>	Result = new Texture2D<PF>( _Device, _Name, Images );

			// Dispose of images
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				Images[ImageIndex].Dispose();

			return Result;
		}

		/// <summary>
		/// Creates a texture from a bitmap stream
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapStream"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmapStream( Device _Device, string _Name, System.IO.Stream _BitmapStream, int _MipLevelsCount, float _ImageGamma )
		{
			using ( Image<PF> I = Image<PF>.CreateFromBitmapStream( _Device, _Name, _BitmapStream, _MipLevelsCount, _ImageGamma ) )
				return new Texture2D<PF>( _Device, _Name, I );
		}

		/// <summary>
		/// Creates a texture array from several bitmap streams
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BitmapStreams"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmapStreams( Device _Device, string _Name, System.IO.Stream[] _BitmapStreams, int _MipLevelsCount, float _ImageGamma )
		{
			// Create the array of images
			Image<PF>[]	Images = Image<PF>.CreateFromBitmapStreams( _Device, _Name, _BitmapStreams, _MipLevelsCount, _ImageGamma );

			// Create the texture array
			Texture2D<PF>	Result = new Texture2D<PF>( _Device, _Name, Images );

			// Dispose of images
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				Images[ImageIndex].Dispose();

			return Result;
		}

		/// <summary>
		/// Creates a texture from a bitmap
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Bitmap"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmap( Device _Device, string _Name, System.Drawing.Bitmap _Bitmap, int _MipLevelsCount, float _ImageGamma )
		{
			using ( Image<PF> I = new Image<PF>( _Device, _Name, _Bitmap, _MipLevelsCount, _ImageGamma ) )
				return new Texture2D<PF>( _Device, _Name, I );
		}

		/// <summary>
		/// Creates a texture array from several bitmaps
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Bitmaps"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		/// <remarks>Contrary to the 2 methods below that rely on "D3DX11CreateTextureFromMemory()", this method returns a texture of known dimension.</remarks>
		public static Texture2D<PF>	CreateFromBitmaps( Device _Device, string _Name, System.Drawing.Bitmap[] _Bitmaps, int _MipLevelsCount, float _ImageGamma )
		{
			// Create the array of images
			Image<PF>[]	Images = new Image<PF>[_Bitmaps.Length];
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				Images[ImageIndex] = new Image<PF>( _Device, _Name, _Bitmaps[ImageIndex], _MipLevelsCount, _ImageGamma );

			// Create the texture array
			Texture2D<PF>	Result = new Texture2D<PF>( _Device, _Name, Images );

			// Dispose of images
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				Images[ImageIndex].Dispose();

			return Result;
		}

		/// <summary>
		/// Creates a texture from a file.
		/// Supported file formats are .bmp, .dds, .dib, .hdr, .jpg, .pfm, .png, .ppm, and .tga
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_TextureFile">The texture file to load and create a texture from</param>
		/// <remarks>This method relies on "D3DX11CreateTextureFromMemory()" and returns a BLIND texture of no known dimension.</remarks>
		public static ITexture2D	CreateFromFile( Device _Device, string _Name, System.IO.FileInfo _TextureFile )
		{
			return CreateFromFileInMemory( _Device, _Name, Device.LoadFileContent( _TextureFile ) );
		}
		/// <summary>
		/// Creates a texture from a file in memory.
		/// Supported file formats are .bmp, .dds, .dib, .hdr, .jpg, .pfm, .png, .ppm, and .tga
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Image">Initial image data  of a file </param>
		/// <remarks>This method relies on "D3DX11CreateTextureFromMemory()" and returns a BLIND texture of no known dimension.</remarks>
		public static ITexture2D	CreateFromFileInMemory( Device _Device, string _Name, byte[] _Image )
		{
			try
			{
				ShaderResourceView	TextureView = ShaderResourceView.FromMemory( _Device.DirectXDevice, _Image );
				return new Texture2D<PF_Empty>( _Device, _Name, TextureView );
			}
			catch ( Exception _e )
			{
				throw new Exception( "An error occurred while creating the texture !", _e );
			}
		}

		/// <summary>
		/// This constructor is only called by the "CreateFromFileInMemory()" method
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_ResourceView"></param>
		protected	Texture2D( Device _Device, string _Name, ShaderResourceView _ResourceView ) : base( _Device, _Name )
		{
			if ( _ResourceView == null )
				throw new NException( this, "Invalid resource view !" );

			m_TextureView = _ResourceView;
			m_Width = -1;
			m_Height = -1;
			m_Format = m_TextureView.Description.Format;
			m_ArraySize = m_TextureView.Description.Texture2DArray.ArraySize;
			m_MipLevelsCount = m_TextureView.Description.Texture2D.MipLevels;

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );

			// Create an empty array of texture views that we will fill if Get????TextureView() gets called...
			m_TextureViewsSingle = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsMipBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsArrayBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
		}

		/// <summary>
		/// This constructor is used by the device to initialize the default render target and shouldn't be used otherwise
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Texture"></param>
		/// <param name="_View"></param>
		internal Texture2D( Device _Device, string _Name, SharpDX.Direct3D10.Texture2D _Texture ) : base( _Device, _Name )
		{
			m_Width = _Texture.Description.Width;
			m_Height = _Texture.Description.Height;
			m_Format = _Texture.Description.Format;
			m_MipLevelsCount = 1;
			m_Texture = _Texture;

			m_Size2 = new Vector2( m_Width, m_Height );
			m_Size3 = new Vector3( m_Size2, 0.0f );
			m_InvSize2 = new Vector2( 1.0f / m_Width, 1.0f / m_Height );
			m_InvSize3 = new Vector3( m_InvSize2, 0.0f );
		}

		protected int	CheckMipLevels( int _MipLevelsCount )
		{
			if ( _MipLevelsCount > 0 )
				return	_MipLevelsCount;

			int	Size = Math.Max( m_Width, m_Height );
			_MipLevelsCount = (int) Math.Ceiling( Math.Log( Size ) / Math.Log( 2.0 ) );

			return _MipLevelsCount;
		}

		#endregion
	}
}
