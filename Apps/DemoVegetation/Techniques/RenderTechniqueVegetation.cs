﻿using System;
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
	/// Vegetation rendering
	/// </example>
	public class RenderTechniqueVegetation : RenderTechniqueDefault
	{
		#region CONSTANTS

		#endregion

		#region NESTED TYPES

		#endregion

		#region FIELDS

		protected RendererSetup			m_Renderer = null;

		protected new Material<VS_P3N3G3T2>	m_Material = null;

		#endregion

		#region PROPERTIES

		public override IMaterial	MainMaterial	{ get { return m_Material; } }

		#endregion

		#region METHODS

		public RenderTechniqueVegetation( RendererSetup _Renderer, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;

			// Build the signature we can support
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Tangent", VERTEX_FIELD_USAGE.TANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "UV", VERTEX_FIELD_USAGE.TEX_COORD2D, VERTEX_FIELD_TYPE.FLOAT2, 0 );

			// Create our main materials
			m_Material = ToDispose( new Material<VS_P3N3G3T2>( m_Device, "Tree Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/TreeRendering.fx" ) ) );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Main Pass", "Render Vegetation" );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;
				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				Scene.MaterialParameters	PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in m_RegisteredPrimitives )
					if ( P.CanRender( _FrameToken ) )
					{
						P.Parameters.ApplyDifference( PreviousParams );
						PreviousParams = P.Parameters;

						vLocal2World.SetMatrix( P.Parent.Local2World );
						Pass.Apply();
						P.Render( _FrameToken );
					}
			}
		}

		/// <summary>
		/// Creates a primitive of the provided signature that must be compatible with this technique's signature
		/// </summary>
		/// <param name="_Name">The name of the primitive to create</param>
		/// <param name="_Signature">The vertex signature to create a primitive for</param>
		/// <param name="_VerticesCount">The amount of vertices to build</param>
		/// <param name="_VertexFieldProvider">A provider that is able to return the value of a field of a vertex given both their indices</param>
		/// <param name="_IndicesCount">The amount of indices to build</param>
		/// <param name="_IndexProvider">A provider that is able to return an index given its position in the stream of indices</param>
		/// <param name="_Parameters">The material parameters used to render the primitive</param>
		/// <returns></returns>
		public override IPrimitive	CreatePrimitive( string _Name, IVertexSignature _Signature, int _VerticesCount, IVertexFieldProvider _VertexFieldProvider, int _IndicesCount, IIndexProvider _IndexProvider )
		{
			// Get the vertex fields map
			Dictionary<int,int>	VertexFieldsMap = m_Signature.GetVertexFieldsMap( _Signature );
			if ( VertexFieldsMap == null )
				throw new Exception( "The provided signature is unable to provide a complete match for our signature !\r\nAre you sure this primitive should be rendered with that technique ?" );
			if ( VertexFieldsMap.Count == 0 )
				throw new Exception( "The signature for technique \"" + Name + "\" is empty ! Did you create it in the constructor ?" );
			if ( VertexFieldsMap.Count != 4 )
				throw new Exception( "The signature for technique \"RenderTechniqueVegetation\" does not contain exactly 4 fields as we need ! If you inherited the technique, did you forget to override the CreatePrimitive() and GetPrimitiveInfos() methods ?" );

			//////////////////////////////////////////////////////////////////////////
			// Query and build vertices
			VS_P3N3G3T2[]	Vertices = new VS_P3N3G3T2[_VerticesCount];

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

			// Read back UVs
			VertexFieldIndex = VertexFieldsMap[3];	// UV is field #3 in our signature
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
		public override void			GetPrimitiveInfos( IPrimitive _Primitive, out string _Name, out int _VerticesCount, out byte[] _VertexBufferContent, out int _IndicesCount, out byte[] _IndexBufferContent )
		{
			GetPrimitiveInfos<VS_P3N3G3T2>( _Primitive, out _Name, out _VerticesCount, out _VertexBufferContent, out _IndicesCount, out _IndexBufferContent );
		}

		#endregion
	}
}
