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
	/// This wraps a DirectX 3D render target
	/// </summary>
	public class RenderTarget3D<PF> : Texture3D<PF>, IRenderTarget3D where PF:struct,IPixelFormat
	{
		#region FIELDS

		protected RenderTargetView		m_RenderTargetView = null;
		protected RenderTargetView[]	m_RenderTargetViews = null;

		#endregion

		#region PROPERTIES

		#region IRenderTarget Members

		public RenderTargetView		RenderTargetView	{ get { return m_RenderTargetView; } }

		#endregion

		#endregion

		#region METHODS

		/// <summary>
		/// Builds a standard 3D render target
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_Image"></param>
		public	RenderTarget3D( Device _Device, string _Name, int _Width, int _Height, int _Depth, int _MipLevelsCount ) : base( _Device, _Name, _Width, _Height, _Depth, _MipLevelsCount )
		{
			Init( null );
		}

		/// <summary>
		/// Builds a standard render target from an initial image
		/// </summary>
		public	RenderTarget3D( Device _Device, string _Name, Image3D<PF> _Image ) : base( _Device, _Name, _Image )
		{
		}

		protected override void	Init( Image3D<PF> _Image )
		{
			Texture3DDescription	Desc = new Texture3DDescription();
			Desc.BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource;
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.Depth = m_Depth;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Default;	// Compulsory otherwise the GPU won't be able to render to it !

			if ( _Image != null )
			{
				m_bHasAlpha = _Image.HasAlpha;
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc, _Image.DataBoxes ) );
			}
			else
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc ) );

			// Create the view
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Dimension = ShaderResourceViewDimension.Texture3D;
			ViewDesc.Format = m_Format;
			ViewDesc.Texture3D.MipLevels = m_MipLevelsCount;
            ViewDesc.Texture3D.MostDetailedMip = 0;

			m_TextureView = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			// Create an empty array of texture views that we will fill if GetSingleTextureView() gets called...
			m_TextureViews = new ShaderResourceView[m_MipLevelsCount];
			m_RenderTargetViews = new RenderTargetView[m_MipLevelsCount];

			// Create the default view (a particular case of a single mip view)
			m_RenderTargetView = GetSingleRenderTargetView( 0 );
		}

		public RenderTargetView		GetSingleRenderTargetView( int _MipLevelIndex )
		{
			if ( m_RenderTargetViews[_MipLevelIndex] != null )
				return m_RenderTargetViews[_MipLevelIndex];

			// Create the view for that particular mip entry
			RenderTargetViewDescription	RTDesc = new RenderTargetViewDescription();
			RTDesc.Dimension = RenderTargetViewDimension.Texture3D;
			RTDesc.Format = m_Format;
			RTDesc.Texture3D.MipSlice = _MipLevelIndex;
            RTDesc.Texture3D.FirstDepthSlice = 0;
            RTDesc.Texture3D.DepthSliceCount = -1;

			m_RenderTargetViews[_MipLevelIndex] = ToDispose( new RenderTargetView( m_Device.DirectXDevice, m_Texture, RTDesc ) );

			return m_RenderTargetViews[_MipLevelIndex];
		}

		/// <summary>
		/// Creates an instance of render target from the content of a stream that was saved using Texture3D.Save()
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <param name="_Stream"></param>
		/// <returns></returns>
		public static RenderTarget3D<PF>	CreateFromStream( Device _Device, string _Name, System.IO.Stream _Stream )
		{
			using ( System.IO.BinaryReader Reader = new System.IO.BinaryReader( _Stream ) )
			{
				long	FormerPosition = _Stream.Position;

				//////////////////////////////////////////////////////////////////////////
				// Read dimensions and create the render target
				SharpDX.DXGI.Format Format = (SharpDX.DXGI.Format) Reader.ReadInt32();
				if ( Format != new PF().DirectXFormat )
					throw new Exception( "Format mismatch ! Read value is " + Format + "." );
				int Width = Reader.ReadInt32();
				int Height = Reader.ReadInt32();
				int Depth = Reader.ReadInt32();
				int MipLevelsCount = Reader.ReadInt32();

				RenderTarget3D<PF>	Result = new RenderTarget3D<PF>( _Device, _Name, Width, Height, Depth, MipLevelsCount );

				// Rewind
				_Stream.Position = FormerPosition;

				//////////////////////////////////////////////////////////////////////////
				// Load from stream
				Result.Load( _Stream );

				return Result;
			}
		}

		#endregion
	}
}
