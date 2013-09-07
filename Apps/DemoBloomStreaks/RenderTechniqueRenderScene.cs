using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;
using Nuaj.Cirrus;

namespace Demo
{
	/// <summary>
	/// Standard scene display + light phase written in alpha
	/// </example>
	public class RenderTechniqueRenderScene : RenderTechniqueDefault
	{
		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected new Material<VS_P3N3G3B3T2>	m_Material = null;

		protected IRenderTarget		m_RenderTarget = null;

		#endregion

		#region PROPERTIES

		public override IMaterial	MainMaterial { get { return m_Material; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Initializes the technique
		/// </summary>
		/// <param name="_Renderer"></param>
		/// <param name="_Name"></param>
		public RenderTechniqueRenderScene( Nuaj.Device _Device, string _Name, IRenderTarget _RenderTarget ) : base( _Device, _Name )
		{
			m_RenderTarget = _RenderTarget;

			// Create the vertex signature we can support
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Tangent", VERTEX_FIELD_USAGE.TANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "BiTangent", VERTEX_FIELD_USAGE.BITANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "TexCoord0", VERTEX_FIELD_USAGE.TEX_COORD2D, VERTEX_FIELD_TYPE.FLOAT2, 0 );

			// Create our main materials
			m_Material = ToDispose( new Material<VS_P3N3G3B3T2>( m_Device, "Render Scene Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/BloomStreaks/RenderScene.fx" ) ) );
		}

		public override void	Render( int _FrameToken )
		{
			using ( m_Material.UseLock() )
			{
				m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

				m_Device.SetRenderTarget( m_RenderTarget, m_Device.DefaultDepthStencil );
				m_Device.SetViewport( 0, 0, m_RenderTarget.Width, m_RenderTarget.Height, 0.0f, 1.0f );
//				m_Device.ClearRenderTarget( m_RenderTarget, new Color4( System.Drawing.Color.CornflowerBlue.ToArgb() ) );
				m_Device.ClearRenderTarget( m_RenderTarget, 0.2f * Vector4.One );

				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;

				Scene.MaterialParameters	PreviousParameters = null;
				foreach ( Scene.Mesh.Primitive P in m_RegisteredPrimitives )
					if ( P.CanRender( _FrameToken ) )
					{
						P.Parameters.ApplyDifference( PreviousParameters );
						PreviousParameters = P.Parameters;

						vLocal2World.SetMatrix( P.Parent.Local2World );

						Pass.Apply();

						P.Render( _FrameToken );
					}
			}
		}

		#endregion
	}
}
