using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This is the default render technique that supports all objects that have at least a position, tangent space and one channel of texture coordinates
	/// You should inherit this technique and override the appropriate virtual methods to change rendering, primitives support type, primitive creation, etc.
	/// 
	/// NOTE :
	/// ----
	/// By default, this technique displays all the primitives that were created with it through the "ITechniqueSupportsObjects.CreatePrimitive()" method.
	/// If you assign it a Scene though, it will restrict its display to the scene's primitives instead (provided some of the scene's primitives were created using this technique).
	/// </example>
	public class RenderTechniqueDefault : RenderTechnique, ITechniqueSupportsObjects
	{
		#region NESTED TYPES

		/// <summary>
		/// This internal provider is able to feed vertices data and indices data from memory, it's used when loading a primitive from a stream (i.e. reloading a Cirrus mesh)
		/// </summary>
		protected class		InternalProvider : IVertexFieldProvider, IIndexProvider, IDisposable
		{
			#region FIELDS

			protected int								m_VertexSize = 0;
			protected Dictionary<int,int>				m_FieldIndex2Offset = new Dictionary<int,int>();
			protected Dictionary<int,VERTEX_FIELD_TYPE>	m_FieldIndex2Type = new Dictionary<int,VERTEX_FIELD_TYPE>();

			protected int								m_VerticesCount = 0;
			protected System.IO.MemoryStream			m_VertexBufferContent = null;
			protected System.IO.BinaryReader			m_VertexReader = null;
			protected int								m_IndicesCount = 0;
			protected System.IO.MemoryStream			m_IndexBufferContent = null;
			protected System.IO.BinaryReader			m_IndexReader = null;

			#endregion

			#region METHODS

			public	InternalProvider( IVertexSignature _Signature, int _VerticesCount, byte[] _VertexBufferContent, int _IndicesCount, byte[] _IndexBufferContent )
			{
				// Build vertex description from signature
				for ( int FieldIndex=0; FieldIndex < _Signature.VertexFieldsCount; FieldIndex++ )
				{
					m_FieldIndex2Offset[FieldIndex] = m_VertexSize;
					VERTEX_FIELD_TYPE	FieldType = _Signature.GetVertexFieldType( FieldIndex );
					m_FieldIndex2Type[FieldIndex] = FieldType;

					switch ( FieldType )
					{
						case VERTEX_FIELD_TYPE.FLOAT:
							m_VertexSize += sizeof(float);
							break;
						case VERTEX_FIELD_TYPE.FLOAT2:
							m_VertexSize += 2*sizeof(float);
							break;
						case VERTEX_FIELD_TYPE.FLOAT3:
						case VERTEX_FIELD_TYPE.COLOR3:
							m_VertexSize += 3*sizeof(float);
							break;
						case VERTEX_FIELD_TYPE.FLOAT4:
						case VERTEX_FIELD_TYPE.COLOR4:
							m_VertexSize += 4*sizeof(float);
							break;
					}
				}

				m_VerticesCount = _VerticesCount;
				m_VertexBufferContent = new System.IO.MemoryStream( _VertexBufferContent );
				m_VertexReader = new System.IO.BinaryReader( m_VertexBufferContent );

				m_IndicesCount = _IndicesCount;
				m_IndexBufferContent = new System.IO.MemoryStream( _IndexBufferContent );
				m_IndexReader = new System.IO.BinaryReader( m_IndexBufferContent );
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_VertexReader.Dispose();
				m_VertexBufferContent.Dispose();
				m_IndexReader.Dispose();
				m_IndexBufferContent.Dispose();
			}

			#endregion

			#region IVertexFieldProvider Members

			public object GetField( int _VertexIndex, int _FieldIndex )
			{
				switch ( m_FieldIndex2Type[_FieldIndex] )
				{
					case VERTEX_FIELD_TYPE.FLOAT:
						return ReadVertexFloat( _VertexIndex, m_FieldIndex2Offset[_FieldIndex] );
					case VERTEX_FIELD_TYPE.FLOAT2:
						return ReadVertexFloat2( _VertexIndex, m_FieldIndex2Offset[_FieldIndex] );
					case VERTEX_FIELD_TYPE.FLOAT3:
					case VERTEX_FIELD_TYPE.COLOR3:
						return ReadVertexFloat3( _VertexIndex, m_FieldIndex2Offset[_FieldIndex] );
					case VERTEX_FIELD_TYPE.FLOAT4:
					case VERTEX_FIELD_TYPE.COLOR4:
						return ReadVertexFloat4( _VertexIndex, m_FieldIndex2Offset[_FieldIndex] );

					default:
						throw new Exception( "Unsupported field type !" );
				}
			}

			#endregion

			#region IIndexProvider Members

			public int GetIndex( int _TriangleIndex, int _TriangleVertexIndex )
			{
				int	IndexIndex = 3*_TriangleIndex + _TriangleVertexIndex;
				if ( m_VerticesCount < 65536 )
					return ReadIndex16( IndexIndex );
				else
					return ReadIndex32( IndexIndex );
			}

			#endregion

			protected float	ReadVertexFloat( int _VertexIndex, int _FieldOffset )
			{
				m_VertexBufferContent.Position = m_VertexSize*_VertexIndex+_FieldOffset;
				return m_VertexReader.ReadSingle();
			}

			protected Vector2	ReadVertexFloat2( int _VertexIndex, int _FieldOffset )
			{
				m_VertexBufferContent.Position = m_VertexSize*_VertexIndex+_FieldOffset;
				return new Vector2( m_VertexReader.ReadSingle(), m_VertexReader.ReadSingle() );
			}

			protected Vector3	ReadVertexFloat3( int _VertexIndex, int _FieldOffset )
			{
				m_VertexBufferContent.Position = m_VertexSize*_VertexIndex+_FieldOffset;
				return new Vector3( m_VertexReader.ReadSingle(), m_VertexReader.ReadSingle(), m_VertexReader.ReadSingle() );
			}

			protected Vector4	ReadVertexFloat4( int _VertexIndex, int _FieldOffset )
			{
				m_VertexBufferContent.Position = m_VertexSize*_VertexIndex+_FieldOffset;
				return new Vector4( m_VertexReader.ReadSingle(), m_VertexReader.ReadSingle(), m_VertexReader.ReadSingle(), m_VertexReader.ReadSingle() );
			}

			protected int	ReadIndex16( int _IndexIndex )
			{
				m_IndexBufferContent.Position = 2*_IndexIndex;
				return (int) m_IndexReader.ReadInt16();
			}

			protected int	ReadIndex32( int _IndexIndex )
			{
				m_IndexBufferContent.Position = 4*_IndexIndex;
				return m_IndexReader.ReadInt32();
			}

			#endregion
		}

		#endregion

		#region FIELDS

		// The list of primitives created through this technique
		// This list is updated through the "CreatePrimitive()" and "RemovePrimitive()" methods
		protected List<Scene.Mesh.Primitive>	m_RegisteredPrimitives = new List<Scene.Mesh.Primitive>();

		// The list of primitives displayed with this technique
		// This is updated through the assignment of a scene (by default, when no scene is set, this list will be equal to the list of registered primitives above)
		protected List<Scene.Mesh.Primitive>	m_Primitives = new List<Scene.Mesh.Primitive>();

		// The scene currently being displayed (by default, there is no scene : all registered primitives are displayed)
		protected Scene							m_Scene = null;
		protected Scene.Mesh[]					m_Meshes = new Scene.Mesh[0];

		// Our supported vertex signature
		protected DynamicVertexSignature		m_Signature = new DynamicVertexSignature();

		// Our main material
		protected IMaterial						m_Material = null;
		protected bool							m_bUseAlphaToCoverage = false;
		protected bool							m_bForceAllOpaque = false;

		// Statistics
		protected int							m_ProcessedPrimitivesCount = 0;
		protected int							m_VisiblePrimitivesCount = 0;
		protected int							m_CulledPrimitivesCount = 0;
		protected int							m_OpaquePrimitivesCount = 0;
		protected int							m_TransparentPrimitivesCount = 0;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the Scene this technique is displaying
		/// </summary>
		/// <remarks>By default, the technique doesn't display a single scene but rather all the primitives of all the scenes that were loaded
		/// and which have been registered to the technique (i.e. a union of all the primitives of all the loaded scenes).
		/// If you assign a Scene though, the list of primitives will be rebuilt to only include the scene's primitives.
		/// </remarks>
		public Scene			Scene
		{
			get { return m_Scene; }
			set
			{
				if ( value == m_Scene )
					return;	// No change...

				m_Scene = value;
				UpdatePrimitivesFromScene();
			}
		}

		/// <summary>
		/// Gets the parent meshes of the primitives we're displaying
		/// </summary>
		public Scene.Mesh[]		Meshes
		{
			get { return m_Meshes; }
		}

		#region ITechniqueSupportsObjects Members

		public virtual IVertexSignature		RecognizedSignature	{ get { return m_Signature; } }
		public virtual IMaterial			MainMaterial		{ get { return m_Material; } }

		public event PrimitiveCollectionChangedEventHandler	PrimitiveAdded;
		public event PrimitiveCollectionChangedEventHandler	PrimitiveRemoved;

		#endregion

		/// <summary>
		/// Set to true if you want all your materials to be treated as opaque
		/// </summary>
		public bool							ForceAllOpaque		{ get { return m_bForceAllOpaque; } set { m_bForceAllOpaque = value; } }

		#endregion

		#region METHODS

		public	RenderTechniqueDefault( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		public	RenderTechniqueDefault( Device _Device, string _Name, bool _bUseAlphaToCoverage ) : base( _Device, _Name )
		{
			// Build the signatures we can support
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Tangent", VERTEX_FIELD_USAGE.TANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "BiTangent", VERTEX_FIELD_USAGE.BITANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "TexCoord0", VERTEX_FIELD_USAGE.TEX_COORD2D, VERTEX_FIELD_TYPE.FLOAT2, 0 );

			// Create our main material
			m_Material = ToDispose( new Material<VS_P3N3G3B3T2>( m_Device, "DefaultMaterial", ShaderModel.SM4_0, Properties.Resources.DefaultShader ) );

			m_bUseAlphaToCoverage = _bUseAlphaToCoverage;
		}

		public override void		Dispose()
		{
			base.Dispose();

			// Dispose of the primitives
			foreach ( Scene.Mesh.Primitive P in m_RegisteredPrimitives )
				P.RenderingPrimitive.Dispose();
		}

		public override void		Render( int _FrameToken )
		{
			Render( _FrameToken, m_Primitives );
		}

		/// <summary>
		/// Renders the registered primitives using the technique
		/// </summary>
		/// <param name="_FrameToken">A frame token that should be increased by one each new frame</param>
		public void					Render( int _FrameToken, List<Scene.Mesh.Primitive> _Primitives )
		{
			// If we have no scene and no primitives to display then use all of the registered primitives
			if ( m_Scene == null && m_Primitives.Count == 0 && m_RegisteredPrimitives.Count != 0 )
				UpdatePrimitivesFromScene();

			if ( _Primitives == null )
				_Primitives = m_Primitives;	// Use ours...

			m_FrameToken = _FrameToken;

			// Render using our technique
			Render( _Primitives );
		}

		/// <summary>
		/// Renders the registered primitives using the technique
		/// </summary>
		/// <param name="_Primitives">The list of primitives to render</param>
		protected virtual void		Render( List<Scene.Mesh.Primitive> _Primitives )
		{
			// Render un-optimized...
			using ( m_Material.UseLock() )
			{
				VariableMatrix	vLocal2World = m_Material.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix; 

				m_ProcessedPrimitivesCount = _Primitives.Count;
				m_VisiblePrimitivesCount = 0;
				m_CulledPrimitivesCount = 0;
				m_OpaquePrimitivesCount = 0;
				m_TransparentPrimitivesCount = 0;

				//////////////////////////////////////////////////////////////////////////
				// Render all opaque materials
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );

				Scene.MaterialParameters	PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( (m_bForceAllOpaque || P.Parameters.EvalOpaque) && !P.Culled && P.CanRender( m_FrameToken ) )
					{
						Matrix	Transform = P.Parent.Local2World;
						vLocal2World.SetMatrix( Transform );

						P.Parameters.ApplyDifference( PreviousParams );

						m_Material.Render( ( _Sender, _Pass, _PassIndex ) => { P.Render( m_FrameToken ); } );

						PreviousParams = P.Parameters;

						m_VisiblePrimitivesCount++;
						m_OpaquePrimitivesCount++;
					}

				if ( m_bForceAllOpaque )
					return;

				//////////////////////////////////////////////////////////////////////////
				// Render all "transparent" materials using either blending or alpha to coverage (depends on the way you created the default technique)
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING_MULTISAMPLING );
				m_Device.SetStockBlendState( m_bUseAlphaToCoverage ? Device.HELPER_BLEND_STATES.ALPHA2COVERAGE : Device.HELPER_BLEND_STATES.BLEND );
				if ( !m_bUseAlphaToCoverage )
					m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.NOWRITE_CLOSEST );	// Don't write if blend enabled

				PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( !P.Parameters.EvalOpaque && !P.Culled && P.CanRender( m_FrameToken ) )
					{
						Matrix	Transform = P.Parent.Local2World;
						vLocal2World.SetMatrix( Transform );

						P.Parameters.ApplyDifference( PreviousParams );

						m_Material.Render( ( _Sender, _Pass, _PassIndex ) => { P.Render( m_FrameToken ); } );

						PreviousParams = P.Parameters;

						m_VisiblePrimitivesCount++;
						m_TransparentPrimitivesCount++;
					}
			}
		}

		#region ITechniqueSupportsObjects Members

		/// <summary>
		/// Creates and registers a Cirrus primitive that uses that technique
		/// </summary>
		/// <param name="_Parent">The parent mesh for the primitive</param>
		/// <param name="_Name">The name of the primitive to create</param>
		/// <param name="_Signature">The vertex signature for vertices that are provided</param>
		/// <param name="_VerticesCount">The amount of provided vertices</param>
		/// <param name="_VertexFieldProvider">The vertex provider interface</param>
		/// <param name="_IndicesCount">The amount of provided indices</param>
		/// <param name="_IndexProvider">The index provider interface</param>
		/// <param name="_Parameters">The material parameters associated to the primitive</param>
		/// <returns>The Cirrus primitive that was created and registered by the technique</returns>
		public Scene.Mesh.Primitive	CreatePrimitive( Scene.Mesh _Parent, string _Name, IVertexSignature _Signature, int _VerticesCount, IVertexFieldProvider _VertexFieldProvider, int _IndicesCount, IIndexProvider _IndexProvider, Scene.MaterialParameters _Parameters )
		{
			// Create the basic Nuaj primitive
			IPrimitive	Prim = CreatePrimitive( _Name, _Signature, _VerticesCount, _VertexFieldProvider, _IndicesCount, _IndexProvider );

			// Attach our material to the primitive's parameters
			// (I know several primitive can be created using the same material parameters
			//	but I'd rather attach several times our material to the same parameters rather
			//	than forget to do so, which results in a rightful exception at rendering time)
			_Parameters.AttachMaterial( MainMaterial );

			// Create the Cirrus primitive that will wrap the Nuaj primitive
			Scene.Mesh.Primitive	Result = new Scene.Mesh.Primitive( _Parent, this, Prim, _Parameters );
			m_RegisteredPrimitives.Add( Result );

			// Notify
			if ( PrimitiveAdded != null )
				PrimitiveAdded( this, Result );

			return	Result;
		}

		/// <summary>
		/// Creates a primitive from a stream
		/// </summary>
		/// <param name="_Parent">The parent mesh that owns the primitive</param>
		/// <param name="_Reader"></param>
		/// <returns>The loaded primitive</returns>
		public Scene.Mesh.Primitive	CreatePrimitive( Scene.Mesh _Parent, System.IO.BinaryReader _Reader )
		{
			bool	bVisible = _Reader.ReadBoolean();

			// Get back our material parameters by ID
			int		MatParamsID = _Reader.ReadInt32();
			Scene.MaterialParameters	Parameters = _Parent.Owner.FindMaterialParameters( MatParamsID );

			// Read back primitive data
			string	PrimitiveName = _Reader.ReadString();
			int		VerticesCount = _Reader.ReadInt32();
			byte[]	VertexBufferContent = new byte[_Reader.ReadInt32()];
			_Reader.Read( VertexBufferContent, 0, VertexBufferContent.Length );
			int		IndicesCount = _Reader.ReadInt32();
			byte[]	IndexBufferContent = new byte[_Reader.ReadInt32()];
			_Reader.Read( IndexBufferContent, 0, IndexBufferContent.Length );

			// Re-create the primitive using an ad-hoc internal provider
			Scene.Mesh.Primitive	Result = null;
			using ( InternalProvider Provider = new InternalProvider( RecognizedSignature, VerticesCount, VertexBufferContent, IndicesCount, IndexBufferContent ) )
			{
				Result = CreatePrimitive( _Parent, PrimitiveName, RecognizedSignature, VerticesCount, Provider, IndicesCount, Provider, Parameters );
			}
			Result.Visible = bVisible;

			return Result;
		}

		/// <summary>
		/// Saves a primitive to a stream
		/// </summary>
		/// <param name="_Primitive"></param>
		/// <param name="_Writer"></param>
		public void					SavePrimitive( Scene.Mesh.Primitive _Primitive, System.IO.BinaryWriter _Writer )
		{
			// Save primitive data
			_Writer.Write( _Primitive.Visible );
			_Writer.Write( _Primitive.Parameters.ID );

			// Retrieve infos about the primitive
			string	PrimitiveName = null;
			int		VerticesCount = 0;
			byte[]	VertexBufferContent = null;
			int		IndicesCount = 0;
			byte[]	IndexBufferContent = null;
			GetPrimitiveInfos( _Primitive.RenderingPrimitive, out PrimitiveName, out VerticesCount, out VertexBufferContent, out IndicesCount, out IndexBufferContent );

			_Writer.Write( PrimitiveName );

			// Write vertices
			_Writer.Write( VerticesCount );
			if ( VertexBufferContent != null )
			{
				_Writer.Write( VertexBufferContent.Length );
				_Writer.Write( VertexBufferContent );
			}
			else
				_Writer.Write( (int) 0 );

			// Write indices
			_Writer.Write( IndicesCount );
			if ( IndexBufferContent != null )
			{
				_Writer.Write( IndexBufferContent.Length );
				_Writer.Write( IndexBufferContent );
			}
			else
				_Writer.Write( (int) 0 );
		}

		/// <summary>
		/// Removes an existing primitive
		/// </summary>
		/// <param name="_Primitive"></param>
		public void					RemovePrimitive( Scene.Mesh.Primitive _Primitive )
		{
			if ( !m_RegisteredPrimitives.Contains( _Primitive ) )
				return;

			_Primitive.RenderingPrimitive.Dispose();

			m_RegisteredPrimitives.Remove( _Primitive );

			// Notify
			if ( PrimitiveRemoved != null )
				PrimitiveRemoved( this, _Primitive );
		}

		#endregion

		/// <summary>
		/// Creates a primitive of the provided signature that must be compatible with this technique's signature
		/// </summary>
		/// <param name="_Name">The name of the primitive to create</param>
		/// <param name="_Signature">The vertex signature to create a primitive for</param>
		/// <param name="_VerticesCount">The amount of vertices to build</param>
		/// <param name="_VertexFieldProvider">A provider that is able to return the value of a field of a vertex given both their indices</param>
		/// <param name="_IndicesCount">The amount of provided indices (use 0 for non-indexed triangle lists and -1 for triangle strips)</param>
		/// <param name="_IndexProvider">A provider that is able to return an index given its position in the stream of indices</param>
		/// <returns></returns>
		public virtual IPrimitive	CreatePrimitive( string _Name, IVertexSignature _Signature, int _VerticesCount, IVertexFieldProvider _VertexFieldProvider, int _IndicesCount, IIndexProvider _IndexProvider )
		{
			// Get the vertex fields map
			Dictionary<int,int>	VertexFieldsMap = m_Signature.GetVertexFieldsMap( _Signature );
			if ( VertexFieldsMap == null )
				throw new Exception( "The provided signature is unable to provide a complete match for our signature !\r\nAre you sure this primitive should be rendered with that technique ?" );
			if ( VertexFieldsMap.Count == 0 )
				throw new Exception( "The signature for technique \"" + Name + "\" is empty ! Did you create it in the constructor ?" );
			if ( VertexFieldsMap.Count != 5 )
				throw new Exception( "The signature for technique \"RenderTechniqueDefault\" does not contain exactly 5 fields as we need ! If you inherited the technique, did you forget to override the CreatePrimitive() and GetPrimitiveInfos() methods ?" );

			//////////////////////////////////////////////////////////////////////////
			// Query and build vertices
			VS_P3N3G3B3T2[]	Vertices = new VS_P3N3G3B3T2[_VerticesCount];

			// Reqd back positions
			int	VertexFieldIndex = VertexFieldsMap[0];	// Position is field #0 in our signature
			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
				Vertices[VertexIndex].Position = (Vector3) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			// Read back normals
			VertexFieldIndex = VertexFieldsMap[1];	// Normal is field #1 in our signature
			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
				Vertices[VertexIndex].Normal = (Vector3) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			// Read back tangents
			VertexFieldIndex = VertexFieldsMap[2];	// Tangent is field #2 in our signature
			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
				Vertices[VertexIndex].Tangent = (Vector3) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			// Read back bitangents
			VertexFieldIndex = VertexFieldsMap[3];	// BiTangent is field #3 in our signature
			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
				Vertices[VertexIndex].BiTangent = (Vector3) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			// Read back UVs
			VertexFieldIndex = VertexFieldsMap[4];	// UV is field #4 in our signature
			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
				Vertices[VertexIndex].UV = (Vector2) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			return CreatePrimitive( _Name, Vertices, _IndicesCount, _IndexProvider );
		}

		/// <summary>
		/// Gets serializable informations from a primitive
		/// </summary>
		/// <param name="_Primitive">The primitive to extract the infos from</param>
		/// <param name="_Name">Returns the name of the primitive</param>
		/// <param name="_VerticesCount">Returns the amount of vertices in the vertex buffer</param>
		/// <param name="_VertexBufferContent">Returns the content of the vertex buffer</param>
		/// <param name="_IndicesCount">Returns the amount of indices in the index buffer</param>
		/// <param name="_IndexBufferContent">Returns the content of the index buffer</param>
		public virtual void			GetPrimitiveInfos( IPrimitive _Primitive, out string _Name, out int _VerticesCount, out byte[] _VertexBufferContent, out int _IndicesCount, out byte[] _IndexBufferContent )
		{
			if ( _Primitive is Primitive<VS_P3N3G3B3T2,UInt16> )
			{
				Primitive<VS_P3N3G3B3T2,UInt16>	P = _Primitive as Primitive<VS_P3N3G3B3T2,UInt16>;

				_Name = P.Name;

				// Get vertex buffer infos
				_VerticesCount = P.VertexBuffer.VerticesCount;
				_VertexBufferContent = GetBufferContent<VS_P3N3G3B3T2>( P.VertexBuffer.Buffer, _VerticesCount );

				// Get index buffer infos
				_IndicesCount = P.IndexBuffer.IndicesCount;
				_IndexBufferContent = GetBufferContent<UInt16>( P.IndexBuffer.Buffer, _IndicesCount );
			}
			else if ( _Primitive is Primitive<VS_P3N3G3B3T2,int> )
			{
				Primitive<VS_P3N3G3B3T2,int>	P = _Primitive as Primitive<VS_P3N3G3B3T2,int>;

				_Name = P.Name;

				// Get vertex buffer infos
				_VerticesCount = P.VertexBuffer.VerticesCount;
				_VertexBufferContent = GetBufferContent<VS_P3N3G3B3T2>( P.VertexBuffer.Buffer, _VerticesCount );

				// Get index buffer infos
				_IndicesCount = P.IndexBuffer.IndicesCount;
				_IndexBufferContent = GetBufferContent<int>( P.IndexBuffer.Buffer, _IndicesCount );
			}
			else
				throw new NException( this, "Provided primitive is not supported by that technique !" );
		}

		/// <summary>
		/// Helper to create a primitive when vertices are ready
		/// Typically, you should create your vertex array in the virtual CreatePrimitive() method then forward it to that helper
		///  that will create the indices and finalize the primitive...
		/// </summary>
		/// <typeparam name="VS">The type of vertices to create</typeparam>
		/// <param name="_Name">The name of the primitive to create</param>
		/// <param name="_Vertices">The prepared array of vertices</param>
		/// <param name="_IndicesCount">The amount of provided indices (use 0 for non-indexed triangle lists and -1 for triangle strips)</param>
		/// <param name="_IndexProvider">A provider that is able to return an index given its position in the stream of indices</param>
		/// <returns></returns>
		protected IPrimitive		CreatePrimitive<VS>( string _Name, VS[] _Vertices, int _IndicesCount, IIndexProvider _IndexProvider ) where VS:struct
		{
			// Query and build indices
			IPrimitive	Result = null;
			if ( _Vertices.Length < 65536 )
			{
				UInt16[]	Indices = null;
				if ( _IndicesCount > 0 )
				{
					Indices = new UInt16[_IndicesCount];
					int			TrianglesCount = _IndicesCount / 3;
					int			IndexIndex = 0;
					for ( int TriangleIndex=0; TriangleIndex < TrianglesCount; TriangleIndex++ )
					{
						Indices[IndexIndex++] = (UInt16) _IndexProvider.GetIndex( TriangleIndex, 0 );
						Indices[IndexIndex++] = (UInt16) _IndexProvider.GetIndex( TriangleIndex, 1 );
						Indices[IndexIndex++] = (UInt16) _IndexProvider.GetIndex( TriangleIndex, 2 );
					}
				}

				// Build the actual primitive
				Result = new Primitive<VS,UInt16>( m_Device, _Name, _IndicesCount != -1 ? PrimitiveTopology.TriangleList : PrimitiveTopology.TriangleStrip, _Vertices, Indices, MainMaterial as Material<VS> );
			}
			else
			{
				// Query indices
				int[]	Indices = null;
				if ( _IndicesCount > 0 )
				{
					Indices = new int[_IndicesCount];
					int		TrianglesCount = _IndicesCount / 3;
					int		IndexIndex = 0;
					for ( int TriangleIndex=0; TriangleIndex < TrianglesCount; TriangleIndex++ )
					{
						Indices[IndexIndex++] = _IndexProvider.GetIndex( TriangleIndex, 0 );
						Indices[IndexIndex++] = _IndexProvider.GetIndex( TriangleIndex, 1 );
						Indices[IndexIndex++] = _IndexProvider.GetIndex( TriangleIndex, 2 );
					}
				}

				// Build the actual primitive
				Result = new Primitive<VS,int>( m_Device, _Name, _IndicesCount != -1 ? PrimitiveTopology.TriangleList : PrimitiveTopology.TriangleStrip, _Vertices, Indices, MainMaterial as Material<VS> );
			}

			return Result;
		}

		/// <summary>
		/// Helper to retrieve the primitive infos for vertex and index buffers of a given format
		/// </summary>
		/// <typeparam name="VS">The vertex structure of the vertex buffer to read back</typeparam>
		/// <param name="_Primitive">The primitive to extract the infos from</param>
		/// <param name="_Name">Returns the name of the primitive</param>
		/// <param name="_VerticesCount">Returns the amount of vertices in the vertex buffer</param>
		/// <param name="_VertexBufferContent">Returns the content of the vertex buffer</param>
		/// <param name="_IndicesCount">Returns the amount of indices in the index buffer</param>
		/// <param name="_IndexBufferContent">Returns the content of the index buffer</param>
		protected void				GetPrimitiveInfos<VS>( IPrimitive _Primitive, out string _Name, out int _VerticesCount, out byte[] _VertexBufferContent, out int _IndicesCount, out byte[] _IndexBufferContent ) where VS:struct
		{
			_Name = _Primitive.Name;
			if ( _Primitive is Primitive<VS,UInt16> )
			{
				Primitive<VS,UInt16>	P = _Primitive as Primitive<VS,UInt16>;

				// Get vertex buffer infos
				_VerticesCount = P.VertexBuffer.VerticesCount;
				_VertexBufferContent = GetBufferContent<VS>( P.VertexBuffer.Buffer, _VerticesCount );

				// Get index buffer infos
				_IndicesCount = P.IndexBuffer.IndicesCount;
				_IndexBufferContent = GetBufferContent<UInt16>( P.IndexBuffer.Buffer, _IndicesCount );
			}
			else if ( _Primitive is Primitive<VS,int> )
			{
				Primitive<VS,int>	P = _Primitive as Primitive<VS,int>;

				// Get vertex buffer infos
				_VerticesCount = P.VertexBuffer.VerticesCount;
				_VertexBufferContent = GetBufferContent<VS>( P.VertexBuffer.Buffer, _VerticesCount );

				// Get index buffer infos
				_IndicesCount = P.IndexBuffer.IndicesCount;
				_IndexBufferContent = GetBufferContent<int>( P.IndexBuffer.Buffer, _IndicesCount );
			}
			else
				throw new NException( this, "Provided primitive is not supported by that technique !" );
		}

		/// <summary>
		/// Helper to get the content of a vertex/index buffer
		/// </summary>
		/// <param name="_Buffer"></param>
		/// <param name="_ElementsCount">The amount of elements in the buffer</param>
		/// <typeparam name="T">The type of elements in the buffer</typeparam>
		/// <returns></returns>
		protected byte[]			GetBufferContent<T>( SharpDX.Direct3D10.Buffer _Buffer, int _ElementsCount ) where T:struct
		{
			int	ElementSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

			using ( SharpDX.Direct3D10.Buffer Temp = new SharpDX.Direct3D10.Buffer( m_Device.DirectXDevice, new BufferDescription()
				{
					BindFlags = BindFlags.None,
					CpuAccessFlags = CpuAccessFlags.Read,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = ElementSize * _ElementsCount,
					Usage = ResourceUsage.Staging
				} ) )
			{
				m_Device.DirectXDevice.CopyResource( _Buffer, Temp );

				DataStream	Stream = Temp.Map( MapMode.Read, MapFlags.None );
				byte[]		MappedBuffer = new byte[Stream.Length];
				Stream.Read( MappedBuffer, 0, (int) Stream.Length );
				Temp.Unmap();

				return MappedBuffer;
			}
		}

		/// <summary>
		/// Updates the list of primitives and meshes from the currently assigned scene
		/// </summary>
		protected void				UpdatePrimitivesFromScene()
		{
			m_Primitives.Clear();
			if ( m_Scene == null )
			{	// If no scene is used then use all registered primitives
				Dictionary<Scene.Mesh,Scene.Mesh>	RegisteredMeshes = new Dictionary<Scene.Mesh,Scene.Mesh>();
				foreach ( Scene.Mesh.Primitive P in m_RegisteredPrimitives )
				{
					RegisteredMeshes[P.Parent] = P.Parent;
					m_Primitives.Add( P );
				}
				m_Meshes = RegisteredMeshes.Keys.ToArray<Scene.Mesh>();
				return;
			}

			// Here, we simply retrieve all the primitives of every mesh that need to be rendered
			//	with this technique and collapse these primitives into an array for rendering.
			// Meshes are also kept in another array for bounding-boxes and various other needs.
			//
			List<Scene.Mesh>	Meshes = new List<Scene.Mesh>();
			foreach ( Scene.Mesh M in m_Scene.Meshes )
			{
				bool	bPrimitivedAdded = false;
				foreach ( Scene.Mesh.Primitive P in M.Primitives )
					if ( P.RenderTechnique == this )
					{	// This primitive is displayed using this technique
						m_Primitives.Add( P );
						bPrimitivedAdded = true;
					}
			
				if ( bPrimitivedAdded )
					Meshes.Add( M );	// Register this mesh as one of its primitives was used...
			}

			m_Meshes = Meshes.ToArray();
		}

		#endregion
	}
}
