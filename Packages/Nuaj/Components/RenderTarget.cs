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
	/// This wraps a DirectX render target
	/// </summary>
	public class RenderTarget<PF> : Texture2D<PF>, IRenderTarget where PF:struct,IPixelFormat
	{
		#region CONSTANTS

		/// <summary>
		/// Use this in the array constructor as the array size to create a cube map render target
		/// </summary>
		public const int	CUBE_MAP = -6;

		#endregion

		#region FIELDS

		protected RenderTargetView		m_RenderTargetView = null;
		protected RenderTargetView[,]	m_RenderTargetViewsSingle = null;
		protected RenderTargetView[,]	m_RenderTargetViewsArrayBand = null;

		#endregion

		#region PROPERTIES

		#region IRenderTarget Members

		public RenderTargetView		RenderTargetView	{ get { return m_RenderTargetView; } }

		#endregion

		#endregion

		#region METHODS

		/// <summary>
		/// Builds a standard render target
		/// </summary>
		public	RenderTarget( Device _Device, string _Name, int _Width, int _Height, int _MipLevelsCount ) : base( _Device, _Name, _Width, _Height, _MipLevelsCount, 1, 1 )
		{
			Init( null );
		}

		/// <summary>
		/// Builds a multi-sampling render target
		/// </summary>
		public	RenderTarget( Device _Device, string _Name, int _Width, int _Height, int _MipLevelsCount, int _MultiSamplesCount ) : base( _Device, _Name, _Width, _Height, _MipLevelsCount, 1, _MultiSamplesCount )
		{
			Init( null );
		}

		/// <summary>
		/// Builds an array of render targets
		/// </summary>
		/// <param name="_ArraySize">The size of the render target array (use CUBE_MAP to build a cube map render target)</param>
		public	RenderTarget( Device _Device, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize, int _MultiSamplesCount ) : base( _Device, _Name, _Width, _Height, _MipLevelsCount, _ArraySize, _MultiSamplesCount )
		{
			if ( m_ArraySize < 0 )
			{	// Create a cube map
				m_ArraySize = 6;
				m_bIsCubeMap = true;
			}
			InitArray( null );
		}

		/// <summary>
		/// Builds a standard render target from an initial image
		/// </summary>
		public	RenderTarget( Device _Device, string _Name, Image<PF> _Image ) : base( _Device, _Name, _Image )
		{
		}

		/// <summary>
		/// Builds an array of render targets from an array of initial images
		/// </summary>
		public	RenderTarget( Device _Device, string _Name, Image<PF>[] _Images, int _MultiSamplesCount ) : base( _Device, _Name, _Images, _MultiSamplesCount )
		{
		}

		/// <summary>
		/// Builds an array of render targets from an initial cube map
		/// </summary>
		public	RenderTarget( Device _Device, string _Name, ImageCube<PF> _ImageCube, int _MultiSamplesCount ) : base( _Device, _Name, _ImageCube )
		{
		}

		/// <summary>
		/// This constructor is used by the device to initialize the default render target and shouldn't be used otherwise
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Texture"></param>
		/// <param name="_View"></param>
		internal RenderTarget( Device _Device, string _Name, SharpDX.Direct3D10.Texture2D _Texture, RenderTargetView _View ) : base( _Device, _Name, _Texture )
		{
			m_RenderTargetView = _View;
		}

		protected override void	Init( Image<PF> _Image )
		{
			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.ArraySize = 1;
			Desc.BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource;
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.SampleDescription = new SampleDescription( m_MultiSamplesCount, 0 );
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Default;	// Compulsory otherwise the GPU won't be able to render to it !

			if ( _Image != null )
			{
				m_bHasAlpha = _Image.HasAlpha;
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc, _Image.DataRectangles ) );
			}
			else
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc ) );

			// Create the view
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Dimension = m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
			ViewDesc.Format = m_Format;
			if ( m_MultiSamplesCount <= 1 )
            {
                ViewDesc.Texture2D.MipLevels = m_MipLevelsCount;
                ViewDesc.Texture2D.MostDetailedMip = 0;
            }

			m_TextureView = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			// Create the view
			RenderTargetViewDescription	RTDesc = new RenderTargetViewDescription();
			RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
			RTDesc.Format = m_Format;
			if ( m_MultiSamplesCount <= 1 )
                RTDesc.Texture2D.MipSlice = 0;

			m_RenderTargetView = ToDispose( new RenderTargetView( m_Device.DirectXDevice, m_Texture, RTDesc ) );

			// Create an empty array of texture/render target views that we will fill if Get????TextureView() and Get????RenderTargetView() get called...
			m_TextureViewsSingle = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsMipBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsArrayBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_RenderTargetViewsSingle = new RenderTargetView[m_MipLevelsCount,m_ArraySize];
			m_RenderTargetViewsArrayBand = new RenderTargetView[m_MipLevelsCount,m_ArraySize];
		}

		protected override void	InitArray( Image<PF>[] _Images )
		{
			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.ArraySize = m_ArraySize;
			Desc.BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource;
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.SampleDescription = new SampleDescription( m_MultiSamplesCount, 0 );
			Desc.OptionFlags = m_bIsCubeMap ? ResourceOptionFlags.TextureCube : ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Default;	// Compulsory otherwise the GPU won't be able to render to it !

			if ( _Images != null )
			{	// Build the global array of data rectangles
 				// This array is first ordered by mip level then array index.
				DataRectangle[]	DataRectangles = new DataRectangle[m_MipLevelsCount * m_ArraySize];
				for ( int ImageIndex=0; ImageIndex < _Images.Length; ImageIndex++ )
				{
					m_bHasAlpha |= _Images[ImageIndex].HasAlpha;
					for ( int MipLevelIndex=0; MipLevelIndex < m_MipLevelsCount; MipLevelIndex++ )
						DataRectangles[ImageIndex*m_MipLevelsCount+MipLevelIndex] = _Images[ImageIndex].DataRectangles[MipLevelIndex];
				}

				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc, DataRectangles ) );
			}
			else
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc ) );

			// Create the texture view
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

			// Create the render target view
			RenderTargetViewDescription	RTDesc = new RenderTargetViewDescription();
			RTDesc.Format = m_Format;
			if ( m_ArraySize > 1 )
			{
				RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampledArray : RenderTargetViewDimension.Texture2DArray;
				if ( m_MultiSamplesCount > 1 )
				{
					RTDesc.Texture2DMSArray.ArraySize = m_ArraySize;
					RTDesc.Texture2DMSArray.FirstArraySlice = 0;
				}
				else
				{
					RTDesc.Texture2DArray.ArraySize = m_ArraySize;
					RTDesc.Texture2DArray.FirstArraySlice = 0;
					RTDesc.Texture2DArray.MipSlice = 0;
				}
			}
			else
			{
				RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
					RTDesc.Texture2D.MipSlice = 0;
			}

			m_RenderTargetView = ToDispose( new RenderTargetView( m_Device.DirectXDevice, m_Texture, RTDesc ) );

			// Create an empty array of texture/render target views that we will fill if Get????TextureView() and Get????RenderTargetView() get called...
			m_TextureViewsSingle = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsMipBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_TextureViewsArrayBand = new ShaderResourceView[m_MipLevelsCount,m_ArraySize];
			m_RenderTargetViewsSingle = new RenderTargetView[m_MipLevelsCount,m_ArraySize];
			m_RenderTargetViewsArrayBand = new RenderTargetView[m_MipLevelsCount,m_ArraySize];
		}

		public RenderTargetView	GetSingleRenderTargetView( int _MipLevelIndex, int _ArrayIndex )
		{
			if ( m_RenderTargetViewsSingle[_MipLevelIndex,_ArrayIndex] != null )
				return m_RenderTargetViewsSingle[_MipLevelIndex,_ArrayIndex];

			// Create the view for that particular array entry
			RenderTargetViewDescription	RTDesc = new RenderTargetViewDescription();
			RTDesc.Format = m_Format;

			if ( m_ArraySize > 1 )
			{
				RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampledArray : RenderTargetViewDimension.Texture2DArray;
				if ( m_MultiSamplesCount > 1 )
				{
					RTDesc.Texture2DMSArray.FirstArraySlice = _ArrayIndex;
					RTDesc.Texture2DMSArray.ArraySize = 1;
				}
				else
				{
					RTDesc.Texture2DArray.FirstArraySlice = _ArrayIndex;
					RTDesc.Texture2DArray.ArraySize = 1;
					RTDesc.Texture2DArray.MipSlice = _MipLevelIndex;
				}
			}
			else
			{
				RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
				{
					RTDesc.Texture2D.MipSlice = _MipLevelIndex;
				}
			}

			m_RenderTargetViewsSingle[_MipLevelIndex,_ArrayIndex] = ToDispose( new RenderTargetView( m_Device.DirectXDevice, m_Texture, RTDesc ) );

			return m_RenderTargetViewsSingle[_MipLevelIndex,_ArrayIndex];
		}

		public RenderTargetView	GetArrayBandRenderTargetView( int _MipLevelIndex, int _ArrayIndex )
		{
			if ( m_RenderTargetViewsArrayBand[_MipLevelIndex,_ArrayIndex] != null )
				return m_RenderTargetViewsArrayBand[_MipLevelIndex,_ArrayIndex];

			// Create the view for that particular array entry
			RenderTargetViewDescription	RTDesc = new RenderTargetViewDescription();
			RTDesc.Format = m_Format;

			if ( m_ArraySize > 1 )
			{
				RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampledArray : RenderTargetViewDimension.Texture2DArray;
				if ( m_MultiSamplesCount > 1 )
				{
					RTDesc.Texture2DMSArray.FirstArraySlice = _ArrayIndex;
					RTDesc.Texture2DMSArray.ArraySize = m_ArraySize-_ArrayIndex;
				}
				else
				{
					RTDesc.Texture2DArray.FirstArraySlice = _ArrayIndex;
					RTDesc.Texture2DArray.ArraySize = m_ArraySize-_ArrayIndex;
					RTDesc.Texture2DArray.MipSlice = _MipLevelIndex;
				}
			}
			else
			{
				RTDesc.Dimension = m_MultiSamplesCount > 1 ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
				if ( m_MultiSamplesCount <= 1 )
				{
					RTDesc.Texture2D.MipSlice = _MipLevelIndex;
				}
			}

			m_RenderTargetViewsArrayBand[_MipLevelIndex,_ArrayIndex] = ToDispose( new RenderTargetView( m_Device.DirectXDevice, m_Texture, RTDesc ) );

			return m_RenderTargetViewsArrayBand[_MipLevelIndex,_ArrayIndex];
		}

		#endregion
	}
}
