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
	/// This wraps a CPU-readable DirectX 2D texture
	/// Such a texture can be mapped to system memory for back reading.
	/// Check "Copying and Accessing Resource Data" in the Direct3D 10 documentation for care about realtime texture reading.
	/// 
	/// You can either:
	///	 _ map this particular texture to CPU memory, fill it with data then copy it to a bindable texture resource.
	///	 _ copy a bindable texture resource to that texture, map it to CPU memory then read the data.	
	/// 
	/// Use Texture2D.CopyTo() to perform resource copy.
	/// 
	/// Note that this texture CANNOT be bound to a shader !
	/// </summary>
	public class Texture2DCPU<PF> : Texture2D<PF> where PF:struct,IPixelFormat
	{
		#region FIELDS

		protected bool	m_bReadOnly = true;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default Texture2D
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_ArraySize"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_bReadOnly">True if the texture is read-only, False for read-write CPU access</param>
		public Texture2DCPU( Device _Device, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize, bool _bReadOnly ) : base( _Device, _Name, _Width, _Height, _MipLevelsCount, _ArraySize, 1 )
		{
			m_bReadOnly = _bReadOnly;

			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.ArraySize = m_ArraySize;
			Desc.BindFlags = BindFlags.None;
			Desc.CpuAccessFlags = CpuAccessFlags.Read | (m_bReadOnly ? CpuAccessFlags.None : CpuAccessFlags.Write);
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.SampleDescription = new SampleDescription( m_MultiSamplesCount, 0 );
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Staging;

			m_Texture = ToDispose( new SharpDX.Direct3D10.Texture2D( m_Device.DirectXDevice, Desc ) );
		}

		/// <summary>
		/// Maps the texture to CPU-readable memory
		/// </summary>
		/// <param name="_MipLevel">Indicates the mip level to map</param>
		public DataRectangle	Map( int _MipLevel )
		{
			return m_Texture.Map( _MipLevel, m_bReadOnly ? MapMode.Read : MapMode.ReadWrite, SharpDX.Direct3D10.MapFlags.None );
		}

		/// <summary>
		/// Maps the texture to CPU-readable memory via a stream
		/// </summary>
		/// <param name="_MipLevel"></param>
		/// <param name="_Stream"></param>
		/// <returns></returns>
		public DataRectangle	Map( int _MipLevel, out DataStream _Stream )
		{
			return m_Texture.Map( _MipLevel, m_bReadOnly ? MapMode.Read : MapMode.ReadWrite, SharpDX.Direct3D10.MapFlags.None, out _Stream );
		}

		/// <summary>
		/// Un-Maps the texture from CPU-readable memory
		/// </summary>
		/// <param name="_MipLevel">Indicates the mip level to unmap</param>
		public void				UnMap( int _MipLevel )
		{
			m_Texture.Unmap( _MipLevel );
		}

		#endregion
	}
}
