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
	public class DepthStencil<PF> : Texture2D<PF>, IDepthStencil where PF:struct,IDepthFormat
	{
		#region FIELDS

		protected Format			m_ReadableFormat = Format.Unknown;
		protected Format			m_ShaderResourceFormat = Format.Unknown;
		protected DepthStencilView	m_DepthStencilView = null;

		#endregion

		#region PROPERTIES

		#region IDepthStencil Members

		public DepthStencilView		DepthStencilView	{ get { return m_DepthStencilView; } }

		#endregion

		#endregion

		#region METHODS

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_bReadable">True if the depth stencil should be readable by a shader and created as a shader resource</param>
		public	DepthStencil( Device _Device, string _Name, int _Width, int _Height, bool _bReadable ) : base( _Device, _Name, _Width, _Height, 1, 1, 1 )
		{
			if ( _bReadable )
			{
				m_ReadableFormat = new PF().ReadableDirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
				m_ShaderResourceFormat = new PF().ShaderResourceDirectXFormat;
			}
			Init( _bReadable );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MultiSamplesCount"></param>
		/// <param name="_bReadable">True if the depth stencil should be readable by a shader and created as a shader resource</param>
		public	DepthStencil( Device _Device, string _Name, int _Width, int _Height, int _MultiSamplesCount, bool _bReadable ) : base( _Device, _Name, _Width, _Height, 1, 1, _MultiSamplesCount )
		{
			if ( _bReadable )
			{
				m_ReadableFormat = new PF().ReadableDirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
				m_ShaderResourceFormat = new PF().ShaderResourceDirectXFormat;
			}
			Init( _bReadable );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MultiSamplesCount"></param>
		/// <param name="_bReadable">True if the depth stencil should be readable by a shader and created as a shader resource</param>
		public	DepthStencil( Device _Device, string _Name, int _Width, int _Height, int _ArraySize, int _MultiSamplesCount, bool _bReadable ) : base( _Device, _Name, _Width, _Height, 1, _ArraySize, _MultiSamplesCount )
		{
			if ( _bReadable )
			{
				m_ReadableFormat = new PF().ReadableDirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
				m_ShaderResourceFormat = new PF().ShaderResourceDirectXFormat;
			}
			Init( _bReadable );
		}

		protected void		Init( bool _bReadable )
		{
			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.ArraySize = m_ArraySize;
			Desc.BindFlags = BindFlags.DepthStencil | (_bReadable ? BindFlags.ShaderResource : BindFlags.None);
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = _bReadable ? m_ReadableFormat : m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.SampleDescription = new SampleDescription( m_MultiSamplesCount, 0 );
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Default;	// Compulsory otherwise the GPU won't be able to render to it !

			m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc ) );

			// Create the depth stencil view
			DepthStencilViewDescription	DSDesc = new DepthStencilViewDescription();
			DSDesc.Dimension = m_ArraySize > 1 ?
				(m_MultiSamplesCount > 1 ? DepthStencilViewDimension.Texture2DMultisampledArray : DepthStencilViewDimension.Texture2DArray) :
				(m_MultiSamplesCount > 1 ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D);
			DSDesc.Format = m_Format;
			if ( m_ArraySize > 1 )
			{
				if ( m_MultiSamplesCount <= 1 )
				{
					DSDesc.Texture2DArray.FirstArraySlice = 0;
					DSDesc.Texture2DArray.ArraySize = m_ArraySize;
					DSDesc.Texture2DArray.MipSlice = 0;
				}
				else
				{
					DSDesc.Texture2DMSArray.FirstArraySlice = 0;
					DSDesc.Texture2DMSArray.ArraySize = m_ArraySize;
				}
			}
			else
			{
				if ( m_MultiSamplesCount <= 1 )
					DSDesc.Texture2D.MipSlice = 0;
			}

			m_DepthStencilView = ToDispose( new DepthStencilView( m_Device.DirectXDevice, m_Texture, DSDesc ) );

			if ( !_bReadable )
				return;

			// Create the shader view
			ShaderResourceViewDescription	SVDesc = new ShaderResourceViewDescription();
			SVDesc.Dimension = m_ArraySize > 1 ?
				(m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampledArray : ShaderResourceViewDimension.Texture2DArray) :
				(m_MultiSamplesCount > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D);
			SVDesc.Format = m_ShaderResourceFormat;

			if ( m_ArraySize > 1 )
			{
				if ( m_MultiSamplesCount <= 1 )
				{
					SVDesc.Texture2DArray.FirstArraySlice = 0;
					SVDesc.Texture2DArray.ArraySize = m_ArraySize;
					SVDesc.Texture2DArray.MipLevels = m_MipLevelsCount;
					SVDesc.Texture2DArray.MostDetailedMip = 0;
				}
				else
				{
					SVDesc.Texture2DMSArray.ArraySize = m_ArraySize;
					SVDesc.Texture2DMSArray.FirstArraySlice = 0;
				}
			}
			else
			{
				SVDesc.Texture2D.MipLevels = m_MipLevelsCount;
				SVDesc.Texture2D.MostDetailedMip = 0;
			}

			m_TextureView = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, SVDesc ) );
		}

		#endregion
	}
}
