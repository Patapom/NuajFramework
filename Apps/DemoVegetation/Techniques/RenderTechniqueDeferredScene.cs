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
	/// Deferred Rendering Technique using Full Vertex Infos (VS_P3N3G3T2)
	/// (supports standard imported Cirrus scenes)
	/// </example>
	public class DeferredRenderingScene : RenderTechniqueDefault, IDepthPassRenderable
	{
		#region FIELDS

		protected Renderer					m_Renderer = null;

		#endregion

		#region METHODS

		public	DeferredRenderingScene( Renderer _Renderer, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;
		}

		public DeferredRenderingScene( Renderer _Renderer, string _Name, bool _bUseAlpha2Coverage ) : this( _Renderer, _Name )
		{
			m_bUseAlphaToCoverage = _bUseAlpha2Coverage;

			// Build the signature we can support
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Tangent", VERTEX_FIELD_USAGE.TANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "UV", VERTEX_FIELD_USAGE.TEX_COORD2D, VERTEX_FIELD_TYPE.FLOAT2, 0 );

			// Create our main materials
			m_Material = ToDispose( new Material<VS_P3N3G3T2>( m_Device, "Scene Phong Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/RenderMRTScene.fx" ) ) );
		}

		protected override void	Render( List<Scene.Mesh.Primitive> _Primitives )
		{
#if DEBUG
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Main Pass", "Render Scene" );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Render scene
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.NOWRITE_CLOSEST_OR_EQUAL );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix;
				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				m_ProcessedPrimitivesCount = m_Primitives.Count;
				m_VisiblePrimitivesCount = 0;
				m_CulledPrimitivesCount = 0;
				m_OpaquePrimitivesCount = 0;

				//////////////////////////////////////////////////////////////////////////
				// Render all opaque materials
				Scene.MaterialParameters	PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( !P.Culled && P.CanRender( m_FrameToken ) )//&& P.Parameters.EvalOpaque )
					{
						Matrix	Transform = P.Parent.Local2World;
						vLocal2World.SetMatrix( Transform );

						P.Parameters.AttachMaterial( m_Material );
						P.Parameters.ApplyDifference( PreviousParams );

						Pass.Apply();
						P.Render( m_FrameToken );

						PreviousParams = P.Parameters;

						m_VisiblePrimitivesCount++;
						m_OpaquePrimitivesCount++;
					}
			}
		}

		#region IDepthPassRenderable Members

		public void RenderDepthPass( int _FrameToken, EffectPass _Pass, VariableMatrix _vLocal2World )
		{
#if DEBUG
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Depth Pass", "Render Scene" );
#endif

			foreach ( Scene.Mesh.Primitive P in m_Primitives )
				if ( !P.Culled && P.CanRender( _FrameToken ) )//&& P.Parameters.EvalOpaque )
				{
					Matrix	Transform = P.Parent.Local2World;
					_vLocal2World.SetMatrix( Transform );
					_Pass.Apply();

					P.Render( _FrameToken );
				}
		}

		#endregion

		#region ITechniqueSupportsObjects Members

		public override IPrimitive	CreatePrimitive( string _Name, IVertexSignature _Signature, int _VerticesCount, IVertexFieldProvider _VertexFieldProvider, int _IndicesCount, IIndexProvider _IndexProvider )
		{
			// Get the vertex fields map
			Dictionary<int,int>	VertexFieldsMap = m_Signature.GetVertexFieldsMap( _Signature );
			if ( VertexFieldsMap == null )
				throw new Exception( "The provided signature is unable to provide a complete match for our signature !\r\nAre you sure this primitive should be rendered with that technique ?" );

			//////////////////////////////////////////////////////////////////////////
			// Query vertices
			VS_P3N3G3T2[]	Vertices = new VS_P3N3G3T2[_VerticesCount];

			// Read back positions
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
// 
// 			// Read back bitangents
// 			VertexFieldIndex = VertexFieldsMap[3];	// BiTangent is field #3 in our signature
// 			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
// 				Vertices[VertexIndex].BiTangent = (Vector3) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			// Read back UVs
			VertexFieldIndex = VertexFieldsMap[3];	// UV is field #3 in our signature
			for ( int VertexIndex=0; VertexIndex < _VerticesCount; VertexIndex++ )
				Vertices[VertexIndex].UV = (Vector2) _VertexFieldProvider.GetField( VertexIndex, VertexFieldIndex );

			return CreatePrimitive( _Name, Vertices, _IndicesCount, _IndexProvider );
		}

		public override void GetPrimitiveInfos( IPrimitive _Primitive, out string _Name, out int _VerticesCount, out byte[] _VertexBufferContent, out int _IndicesCount, out byte[] _IndexBufferContent )
		{
			GetPrimitiveInfos<VS_P3N3G3T2>( _Primitive, out _Name, out _VerticesCount, out _VertexBufferContent, out _IndicesCount, out _IndexBufferContent );
		}

		#endregion

		#endregion
	}
}
