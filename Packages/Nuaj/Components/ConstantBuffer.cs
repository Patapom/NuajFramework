using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D10.Buffer;

namespace Nuaj
{
	/// <summary>
	/// The Constant Buffer class encompasses a DirectX constant buffer
	/// </summary>
	/// <typeparam name="T">The structure type used for the buffer</typeparam>
	public class ConstantBuffer<T> : Component where T:struct
	{
		#region FIELDS

		protected Buffer	m_Buffer = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the constant buffer
		/// </summary>
		public Buffer			Buffer	{ get { return m_Buffer; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an empty constant buffer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_EffectSource">The source code for the shader</param>
		public	ConstantBuffer( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Creates a constant buffer from a single structure
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Indices">The array of indices to build the buffer with</param>
		public	ConstantBuffer( Device _Device, string _Name, T _Buffer ) : base( _Device, _Name )
		{
			Init( _Buffer );
		}

		/// <summary>
		/// Initializes the constant buffer with a single structure
		/// </summary>
		/// <param name="_Buffer">The structure to build the buffer with</param>
		public void		Init( T _Buffer )
		{
			int	StructureSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(T) );

			using ( var	Stream = new DataStream( StructureSize, true, true ) )
			{
				// Write the structure to the stream
				Stream.Write<T>( _Buffer );
				Stream.Position = 0;

				m_Buffer = ToDispose( new Buffer( m_Device.DirectXDevice, null,
					new BufferDescription()
					{
						BindFlags = BindFlags.ConstantBuffer | BindFlags.ShaderResource,
						CpuAccessFlags = CpuAccessFlags.Write,
						OptionFlags = ResourceOptionFlags.None,
						SizeInBytes = (int) StructureSize,
						Usage = ResourceUsage.Dynamic
					} ) );
			}
		}

		#endregion
	}
}
