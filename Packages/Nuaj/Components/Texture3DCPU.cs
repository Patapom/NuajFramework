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
	/// This wraps a CPU-readable DirectX 3D texture
	/// Such a texture can be mapped to system memory for back reading.
	/// Check "Copying and Accessing Resource Data" in the Direct3D 10 documentation for care about realtime texture reading.
	/// 
	/// You can either:
	///	 _ map this particular texture to CPU memory, fill it with data then copy it to a bindable texture resource.
	///	 _ copy a bindable texture resource to that texture, map it to CPU memory then read the data.	
	/// 
	/// Use Texture3D.CopyTo() to perform resource copy.
	/// 
	/// Note that this texture CANNOT be bound to a shader !
	/// </summary>
	public class Texture3DCPU<PF> : Texture3D<PF> where PF:struct,IPixelFormat
	{
		#region FIELDS

		protected bool	m_bReadOnly = true;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default Texture3D
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_ArraySize"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_bReadOnly">True if the texture is read-only, False for read-write CPU access</param>
		public Texture3DCPU( Device _Device, string _Name, int _Width, int _Height, int _Depth, int _MipLevelsCount, bool _bReadOnly ) : base( _Device, _Name, _Width, _Height, _Depth, _MipLevelsCount )
		{
			m_bReadOnly = _bReadOnly;

			Texture3DDescription	Desc = new Texture3DDescription();
			Desc.BindFlags = BindFlags.None;
			Desc.CpuAccessFlags = CpuAccessFlags.Read | (m_bReadOnly ? CpuAccessFlags.None : CpuAccessFlags.Write);
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.Depth = m_Depth;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Staging;

			m_Texture = ToDispose( new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc ) );
		}

		/// <summary>
		/// Maps the texture to CPU-readable memory
		/// </summary>
		/// <param name="_MipLevel">Indicates the mip level to map</param>
		public DataBox	Map( int _MipLevel )
		{
			return m_Texture.Map( _MipLevel, m_bReadOnly ? MapMode.Read : MapMode.ReadWrite, SharpDX.Direct3D10.MapFlags.None );
		}

		/// <summary>
		/// Maps the texture to CPU-readable memory via a stream
		/// </summary>
		/// <param name="_MipLevel">Indicates the mip level to map</param>
		/// <param name="_Stream"></param>
		public DataBox	Map( int _MipLevel, out DataStream _Stream )
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
