using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;

namespace Nuaj
{
	/// <summary>
	/// The Primitive class contains a material, a vertex buffer and an optional index buffer
	/// It's designed to be initialized with vertex/index data and to render easily using a
	///  single material
	/// The material must be set but can be changed at runtime, provinding it's compatible with
	///  the primitive's Vertex structure
	/// </summary>
	/// <typeparam name="VS">The vertex structure that should be used with this primitive</typeparam>
	/// <typeparam name="I">The index type that should be used with this primitive (only int, uint, short and ushort are supported !)</typeparam>
	public class Primitive<VS,I> : Component, IPrimitive where VS:struct where I:struct
	{
		#region FIELDS

		protected VertexBuffer<VS>	m_VertexBuffer = null;
		protected IndexBuffer<I>	m_IndexBuffer = null;
		protected Material<VS>		m_Material = null;
		protected PrimitiveTopology	m_Topology = PrimitiveTopology.Undefined;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the vertex buffer
		/// </summary>
		public VertexBuffer<VS>		VertexBuffer	{ get { return m_VertexBuffer; } }

		/// <summary>
		/// Gets the index buffer
		/// </summary>
		public IndexBuffer<I>		IndexBuffer		{ get { return m_IndexBuffer; } }

		/// <summary>
		/// Gets or sets the Material used to draw that primitive with
		/// </summary>
		public Material<VS>			Material		{ get { return m_Material; } set { m_Material = value; } }

		/// <summary>
		/// Gets or sets the primitive topology
		/// </summary>
		public PrimitiveTopology	Topology		{ get { return m_Topology; } set { m_Topology = value; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an empty primitive
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Topology"></param>
		public	Primitive( Device _Device, string _Name, PrimitiveTopology _Topology ) : base( _Device, _Name )
		{
			m_Topology = _Topology;
		}

		/// <summary>
		/// Creates a non-indexed primitive with a single vertex buffer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Topology"></param>
		public	Primitive( Device _Device, string _Name, PrimitiveTopology _Topology, VS[] _Vertices ) : base( _Device, _Name )
		{
			m_Topology = _Topology;
			Init( _Vertices );
		}

		/// <summary>
		/// Creates a non-indexed primitive with a single vertex buffer and a material
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Topology"></param>
		public	Primitive( Device _Device, string _Name, PrimitiveTopology _Topology, VS[] _Vertices, Material<VS> _Material ) : base( _Device, _Name )
		{
			m_Topology = _Topology;
			Init( _Vertices, null, _Material );
		}

		/// <summary>
		/// Creates an indexed primitive with a single vertex buffer and index buffer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Topology"></param>
		public	Primitive( Device _Device, string _Name, PrimitiveTopology _Topology, VS[] _Vertices, I[] _Indices ) : base( _Device, _Name )
		{
			m_Topology = _Topology;
			Init( _Vertices, _Indices, null );
		}

		/// <summary>
		/// Creates an indexed primitive with a single vertex buffer, index buffer and a material
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Topology"></param>
		public	Primitive( Device _Device, string _Name, PrimitiveTopology _Topology, VS[] _Vertices, I[] _Indices, Material<VS> _Material ) : base( _Device, _Name )
		{
			m_Topology = _Topology;
			Init( _Vertices, _Indices, _Material );
		}

		/// <summary>
		/// Initializes an internal vertex buffer given an array of vertices
		/// </summary>
		/// <param name="_Vertices"></param>
		public void	Init( VS[] _Vertices )
		{
			if ( m_VertexBuffer != null )
				throw new NException( this, "Vertex buffer already initialized for primitive \"" + this + "\" !" );
			if ( _Vertices != null )
				m_VertexBuffer = ToDispose( new VertexBuffer<VS>( m_Device, m_Name + "VB", _Vertices ) );
		}

		/// <summary>
		/// Initializes an internal index buffer given an array of indices
		/// </summary>
		/// <param name="_Vertices"></param>
		public void	Init( I[] _Indices )
		{
			if ( m_IndexBuffer != null )
				throw new NException( this, "Index buffer already initialized for primitive \"" + this + "\" !" );
			if ( _Indices != null )
				m_IndexBuffer = ToDispose( new IndexBuffer<I>( m_Device, m_Name + "IB", _Indices ) );
		}

		/// <summary>
		/// Initializes a complete primitive
		/// </summary>
		/// <param name="_Vertices"></param>
		/// <param name="_Indices"></param>
		/// <param name="_Material"></param>
		public void	Init( VS[] _Vertices, I[] _Indices, Material<VS> _Material )
		{
			Init( _Vertices );
			Init( _Indices );
			m_Material = _Material;
		}

		#region IPrimitive Members

		/// <summary>
		/// Renders the primitive using its associated material
		/// </summary>
		public void Render()
		{
			if ( m_VertexBuffer == null )
				throw new NException( this, "Invalid vertex buffer for drawing primitive \"" + this + "\" !" );

			// Setup topology, input layout and bind vertex buffer...
			if ( m_Material == null )
				RenderOverride();	// The material was set externally, simply assume there is one...
			else
			{	// Render with a material
				m_Device.InputAssembler.PrimitiveTopology = m_Topology;
				m_VertexBuffer.Use();

				using ( m_Material.UseLock() )
				{
					if ( m_IndexBuffer != null )
					{	// Render an indexed primitive...
						m_IndexBuffer.Use();
						m_Material.Render( ( _Material, _Pass, _PassIndex ) =>
						{
							m_IndexBuffer.Draw();
						} );
						m_IndexBuffer.UnUse();
					}
					else
					{	// Render a non-indexed primitive...
						m_Material.Render( ( _Material, _Pass, _PassIndex ) =>
						{
							m_VertexBuffer.Draw();
						} );
					}
				}
			}
		}

		/// <summary>
		/// Renders the primitive using its associated material
		/// </summary>
		public void RenderInstanced( int _StartInstance, int _InstancesCount )
		{
			if ( m_VertexBuffer == null )
				throw new NException( this, "Invalid vertex buffer for drawing primitive \"" + this + "\" !" );

			// Setup topology, input layout and bind vertex buffer...
			if ( m_Material == null )
				RenderInstancedOverride( _StartInstance, _InstancesCount );	// The material was set externally, simply assume there is one...
			else
			{	// Render with a material
				m_Device.InputAssembler.PrimitiveTopology = m_Topology;
				m_VertexBuffer.Use();

				using ( m_Material.UseLock() )
				{
					if ( m_IndexBuffer != null )
					{	// Render an indexed primitive...
						m_IndexBuffer.Use();
						m_Material.Render( ( _Material, _Pass, _PassIndex ) =>
						{
							m_IndexBuffer.DrawInstanced( _StartInstance, _InstancesCount );
						} );
						m_IndexBuffer.UnUse();
					}
					else
					{	// Render a non-indexed primitive...
						m_Material.Render( ( _Material, _Pass, _PassIndex ) =>
						{
							m_VertexBuffer.DrawInstanced( _StartInstance, _InstancesCount );
						} );
					}
				}
			}
		}

		/// <summary>
		/// Renders the primitive using the currently set material
		/// </summary>
		/// <remarks>The primitive can be assigned a material of its own but using this method will not use the primitive's material, rather the currently used material</remarks>
		public void RenderOverride()
		{
			if ( m_VertexBuffer == null )
				throw new NException( this, "Invalid vertex buffer for drawing primitive \"" + this + "\" !" );

			m_Device.InputAssembler.PrimitiveTopology = m_Topology;
			m_VertexBuffer.Use();

			if ( m_IndexBuffer != null )
			{
				m_IndexBuffer.Use();
				m_IndexBuffer.Draw();
			}
			else
				m_VertexBuffer.Draw();
		}

		/// <summary>
		/// Renders instances of the primitive using the currently set material
		/// </summary>
		/// <remarks>The primitive can be assigned a material of its own but using this method will not use the primitive's material, rather the currently used material</remarks>
		public void RenderInstancedOverride( int _StartInstance, int _InstancesCount )
		{
			if ( m_VertexBuffer == null )
				throw new NException( this, "Invalid vertex buffer for drawing primitive \"" + this + "\" !" );

			m_Device.InputAssembler.PrimitiveTopology = m_Topology;
			m_VertexBuffer.Use();

			if ( m_IndexBuffer != null )
			{
				m_IndexBuffer.Use();
				m_IndexBuffer.DrawInstanced( _StartInstance, _InstancesCount );
			}
			else
				m_VertexBuffer.DrawInstanced( _StartInstance, _InstancesCount );
		}

		#endregion

		#endregion
	}
}
