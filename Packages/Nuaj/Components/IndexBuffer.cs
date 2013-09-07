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
	/// The Index Buffer class encompasses a DirectX index buffer
	/// </summary>
	/// <typeparam name="I">The index type that should be used with this index buffer (only int, uint, short and ushort are supported !)</typeparam>
	public class IndexBuffer<I> : Component where I:struct
	{
		#region FIELDS

		protected Buffer	m_Buffer = null;
		protected Format	m_BufferFormat = Format.Unknown;
		protected int		m_IndicesCount = 0;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the index buffer
		/// </summary>
		public Buffer			Buffer	{ get { return m_Buffer; } }

		/// <summary>
		/// Gets the amount of indices in the index buffer
		/// </summary>
		public int				IndicesCount	{ get { return m_IndicesCount; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an empty index buffer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_EffectSource">The source code for the shader</param>
		public	IndexBuffer( Device _Device, string _Name ) : base( _Device, _Name )
		{
			ValidateFormat();
		}

		/// <summary>
		/// Creates a index buffer from an array of vertices
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Indices">The array of indices to build the buffer with</param>
		public	IndexBuffer( Device _Device, string _Name, I[] _Indices ) : base( _Device, _Name )
		{
			ValidateFormat();
			Init( _Indices );
		}

		/// <summary>
		/// Initializes the index buffer with an array of indices
		/// </summary>
		/// <param name="_Indices">The array of indices to build the buffer with</param>
		public void		Init( I[] _Indices )
		{
			if ( _Indices == null )
				throw new NException( this, "Invalid array of indices !" );

			int	StructureSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(I) );
			m_IndicesCount = _Indices.Length;

			using ( var	IndexStream = new DataStream( m_IndicesCount * StructureSize, true, true ) )
			{
				// Write the vertices in the stream
				IndexStream.WriteRange<I>( _Indices );
				IndexStream.Position = 0;

				m_Buffer = ToDispose( new Buffer( m_Device.DirectXDevice, IndexStream,
					new BufferDescription()
					{
						BindFlags = BindFlags.IndexBuffer,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.None,
						SizeInBytes = (int) IndexStream.Length,
						Usage = ResourceUsage.Default
					} ) );
			}
		}

		/// <summary>
		/// Uses the index buffer (i.e. sends it to the Input Assembler)
		/// </summary>
		public void	Use()
		{
#if DEBUG
			m_Device.LastUsedIndexBuffer = this;
#endif
			m_Device.InputAssembler.SetIndexBuffer( m_Buffer, m_BufferFormat, 0 );
		}

		/// <summary>
		/// Draws any kind of indexed primitive using all indices in this index buffer
		/// </summary>
		public void	Draw()
		{
#if DEBUG
			if ( m_Device.LastUsedIndexBuffer != this )
				throw new NException( this, "Attempting to draw with index buffer \"" + Name + "\" whereas Use() was not called !" );
#endif
			m_Device.DrawIndexed( m_IndicesCount, 0, 0 );
		}

		/// <summary>
		/// Draws any kind of indexed primitive instances using all indices in this index buffer
		/// </summary>
		public void	DrawInstanced( int _InstancesCount )
		{
			DrawInstanced( 0, _InstancesCount );
		}

		/// <summary>
		/// Draws any kind of indexed primitive instances using all indices in this index buffer
		/// </summary>
		public void	DrawInstanced( int _StartInstance, int _InstancesCount )
		{
#if DEBUG
			if ( m_Device.LastUsedIndexBuffer != this )
				throw new NException( this, "Attempting to draw with index buffer \"" + Name + "\" whereas Use() was not called !" );
#endif
			m_Device.DrawIndexedInstanced( m_IndicesCount, _InstancesCount, 0, 0, _StartInstance );
		}

		/// <summary>
		/// Stops using the index buffer
		/// </summary>
		public void	UnUse()
		{
#if DEBUG
			m_Device.LastUsedIndexBuffer = null;
#endif
			m_Device.InputAssembler.SetIndexBuffer( null, Format.Unknown, 0 );
		}

		/// <summary>
		/// Retrieves the internal form used for the index buffer from the template type
		/// </summary>
		/// <remarks>Only signed or unsigned ints and shorts can be used as template arguments otherwise an exception is thrown !</remarks>
		protected void	ValidateFormat()
		{
			Type	IndexType = typeof(I);
			if ( IndexType == typeof(uint) )
 				m_BufferFormat = Format.R32_UInt;
			else if ( IndexType == typeof(int) )
 				m_BufferFormat = Format.R32_UInt;	// Index buffers don't support signed integers !
			else if ( IndexType == typeof(ushort) )
 				m_BufferFormat = Format.R16_UInt;
			else if ( IndexType == typeof(short) )
 				m_BufferFormat = Format.R16_UInt;	// Index buffers don't support signed integers !
			else
				throw new NException( this, "The only supported template types for an index buffer are signed or unsigned integers and shorts !" );
		}

		#endregion
	}
}
