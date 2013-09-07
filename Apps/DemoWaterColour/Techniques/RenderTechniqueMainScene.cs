//#define USE_PERPARTICLE_FRACTAL_DISTORT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Default Scene Rendering
	/// </example>
	public class RenderTechniqueMainScene : RenderTechniqueDefault, IShadowMapRenderable
	{
		#region FIELDS

		protected RendererSetupDemo		m_Renderer = null;
		protected bool					m_bEnabled = false;

		#endregion

		#region PROPERTIES

		public bool				Enabled
		{
			get { return m_bEnabled; }
			set { m_bEnabled = value; }
		}

		#region IShadowMapRenderable Members

		public BoundingBox[] ShadowCastersWorldAABB
		{
			get
			{
				List<BoundingBox>	Result = new List<BoundingBox>();
				foreach ( Scene.Mesh M in m_Meshes )
					if ( M.Visible && M.CastShadow )
						Result.Add( M.WorldBBox );

				return Result.ToArray();
			}
		}

		public BoundingBox[] ShadowReceiversWorldAABB
		{
			get
			{
				List<BoundingBox>	Result = new List<BoundingBox>();
				foreach ( Scene.Mesh M in m_Meshes )
					if ( M.Visible && M.ReceiveShadow )
						Result.Add( M.WorldBBox );

				return Result.ToArray();
			}
		}

		#endregion

		#endregion

		#region METHODS

		public	RenderTechniqueMainScene( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;

			// Build the signatures we can support
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Tangent", VERTEX_FIELD_USAGE.TANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "BiTangent", VERTEX_FIELD_USAGE.BITANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "TexCoord0", VERTEX_FIELD_USAGE.TEX_COORD2D, VERTEX_FIELD_TYPE.FLOAT2, 0 );

			m_Material = m_Renderer.LoadMaterial<VS_P3N3G3B3T2>( "Scene Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/MainScene.fx" ) );
		}

		protected override void Render( List<Scene.Mesh.Primitive> _Primitives )
		{
			if ( !m_bEnabled )
				return;

			// Render un-optimized...
			using ( m_Material.UseLock() )
			{
				VariableMatrix	vLocal2World = m_Material.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix; 
				VariableVector	vDeltaPosition = m_Material.GetVariableBySemantic( "MOTION_DELTA_POSITION" ).AsVector; 
				VariableVector	vDeltaRotation = m_Material.GetVariableBySemantic( "MOTION_DELTA_ROTATION" ).AsVector; 
				VariableVector	vDeltaPivot = m_Material.GetVariableBySemantic( "MOTION_DELTA_PIVOT" ).AsVector; 

				m_ProcessedPrimitivesCount = _Primitives.Count;
				m_VisiblePrimitivesCount = 0;
				m_CulledPrimitivesCount = 0;
				m_OpaquePrimitivesCount = 0;
				m_TransparentPrimitivesCount = 0;

				//////////////////////////////////////////////////////////////////////////
				// Render all opaque materials
				m_Renderer.SetDefaultRenderTarget();
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST_OR_EQUAL );	// We have a depth pass !

				Vector3		DeltaPosition, DeltaPivot;
				Quaternion	DeltaRotation;

				Scene.MaterialParameters	PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( P.Visible && !P.Culled && P.CanRender( m_FrameToken ) && (m_bForceAllOpaque || P.Parameters.EvalOpaque) )
					{
						Matrix	Transform = P.Parent.Local2World;

						P.Parent.ComputeDeltaPositionRotation( out DeltaPosition, out DeltaRotation, out DeltaPivot );

						vLocal2World.SetMatrix( Transform );
						vDeltaPosition.Set( DeltaPosition );
						vDeltaRotation.Set( DeltaRotation );
						vDeltaPivot.Set( DeltaPivot );

						P.Parameters.AttachMaterial( m_Material );
						P.Parameters.ApplyDifference( PreviousParams );

						m_Material.Render( ( _Sender, _Pass, _PassIndex ) => { P.Render( m_FrameToken ); } );

						PreviousParams = P.Parameters;

						m_VisiblePrimitivesCount++;
						m_OpaquePrimitivesCount++;
					}
			}
		}

		#region IDepthPassRenderable Members

		public void RenderDepthPass( int _FrameToken, EffectPass _Pass, VariableMatrix _vLocal2World )
		{
			if ( !m_bEnabled )
				return;

			foreach ( Scene.Mesh.Primitive P in m_Primitives )
				if ( P.Visible && !P.Culled && P.CanRender( _FrameToken ) )
				{
					Matrix	Transform = P.Parent.Local2World;
					_vLocal2World.SetMatrix( Transform );
					_Pass.Apply();
					P.Render( _FrameToken );
				}
		}

		#endregion

		#endregion
	}
}
