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
	/// The Vertex Buffer class encompasses a DirectX vertex buffer
	/// </summary>
	/// <typeparam name="VS">The vertex structure that should be used with this vertex buffer</typeparam>
	public class VertexBuffer<VS> : Component where VS:struct
	{
		#region FIELDS

		protected Buffer				m_Buffer = null;
		protected int					m_VerticesCount = 0;
		protected VertexBufferBinding	m_Binding;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the vertex buffer
		/// </summary>
		public Buffer				Buffer	{ get { return m_Buffer; } }

		/// <summary>
		/// Gets the vertex buffer binding
		/// </summary>
		public VertexBufferBinding	Binding	{ get { return m_Binding; } }

		/// <summary>
		/// Gets the amount of vertices in the vertex buffer
		/// </summary>
		public int					VerticesCount	{ get { return m_VerticesCount; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an empty vertex buffer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_EffectSource">The source code for the shader</param>
		public	VertexBuffer( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Creates a vertex buffer from an array of vertices
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Vertices">The array of vertices to build the buffer with</param>
		public	VertexBuffer( Device _Device, string _Name, VS[] _Vertices ) : base( _Device, _Name )
		{
			Init( _Vertices );
		}

		/// <summary>
		/// Initializes the vertex buffer with an array of vertices
		/// </summary>
		/// <param name="_Vertices">The array of vertices to build the buffer with</param>
		public void		Init( VS[] _Vertices )
		{
			if ( _Vertices == null )
				throw new NException( this, "Invalid array of vertices !" );

			int	StructureSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(VS) );
			m_VerticesCount = _Vertices.Length;

			using ( var	VertexStream = new DataStream( m_VerticesCount * StructureSize, true, true ) )
			{
				// Write the vertices in the stream
				VertexStream.WriteRange<VS>( _Vertices );
				VertexStream.Position = 0;

				// Build the buffer from the stream
				m_Buffer = ToDispose( new Buffer( m_Device.DirectXDevice, VertexStream,
					new BufferDescription()
					{
						BindFlags = BindFlags.VertexBuffer,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.None,
						SizeInBytes = (int) VertexStream.Length,
						Usage = ResourceUsage.Default
					} ) );
			}

			// Build the binding
			m_Binding = new VertexBufferBinding( m_Buffer, StructureSize, 0 );
		}

		/// <summary>
		/// Uses the vertex buffer (i.e. sends it to the Input Assembler)
		/// </summary>
		public void	Use()
		{
#if DEBUG
			m_Device.LastUsedVertexBuffer = this;
#endif
			m_Device.InputAssembler.SetVertexBuffers( 0, m_Binding );
		}

		/// <summary>
		/// Draws any kind of non-indexed primitive using all indices in this index buffer
		/// </summary>
		public void	Draw()
		{
#if DEBUG
			if ( m_Device.LastUsedVertexBuffer != this )
				throw new NException( this, "Attempting to draw with vertex buffer \"" + Name + "\" whereas Use() was not called !" );
#endif
			m_Device.Draw( m_VerticesCount, 0 );
		}

		/// <summary>
		/// Draws any kind of non-indexed primitive using all indices in this index buffer
		/// </summary>
		public void	Draw( int _StartVertex, int _VerticesCount )
		{
#if DEBUG
			if ( m_Device.LastUsedVertexBuffer != this )
				throw new NException( this, "Attempting to draw with vertex buffer \"" + Name + "\" whereas Use() was not called !" );
#endif
			m_Device.Draw( _VerticesCount, _StartVertex );
		}

		/// <summary>
		/// Draws any kind of non-indexed instanced primitive using all indices in this index buffer
		/// </summary>
		public void	DrawInstanced( int _InstancesCount )
		{
			DrawInstanced( 0, _InstancesCount );
		}

		/// <summary>
		/// Draws any kind of non-indexed instanced primitive using all indices in this index buffer
		/// </summary>
		public void	DrawInstanced( int _StartInstance, int _InstancesCount )
		{
#if DEBUG
			if ( m_Device.LastUsedVertexBuffer != this )
				throw new NException( this, "Attempting to draw with vertex buffer \"" + Name + "\" whereas Use() was not called !" );
#endif
			m_Device.DrawInstanced( m_VerticesCount, _InstancesCount, 0, _StartInstance );
		}

		/// <summary>
		/// Un-Uses the vertex buffer (i.e. clears it from the Input Assembler)
		/// </summary>
		public void	UnUse()
		{
#if DEBUG
			m_Device.LastUsedVertexBuffer = null;
#endif
			m_Device.InputAssembler.SetVertexBuffers( 0, null );
		}

		#endregion
	}
}
