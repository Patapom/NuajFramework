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
	/// Antialiasing
	/// </example>
	public class PostProcessAntiAliasing : RenderTechniqueBase
	{
		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected float						m_DepthThreshold = 0.01f;
		protected float						m_SmoothDistance = 1.0f;
		protected float						m_SmoothWeights = 1.0f;

		#endregion

		#region PROPERTIES

		public float						DepthThreshold			{ get { return m_DepthThreshold; } set { m_DepthThreshold = value; } }
		public float						SmoothDistance			{ get { return m_SmoothDistance; } set { m_SmoothDistance = value; } }
		public float						SmoothWeights			{ get { return m_SmoothWeights; } set { m_SmoothWeights = value; } }

		#endregion

		#region METHODS

		public	PostProcessAntiAliasing( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			if ( m_Renderer.MSAADepthTarget == null )
				return;

			// Create our main material
 			m_MaterialPostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "Post-Process AntiAliasing Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/PostProcessAntiAliasing.fx" ) );

			// Choose technique based on multisamples count
			if ( m_Renderer.MSAADepthTarget.MultiSamplesCount == 8 )
				m_MaterialPostProcess.CurrentTechnique = m_MaterialPostProcess.GetTechniqueByName( "AntiAliasing8" );
			else
				m_MaterialPostProcess.CurrentTechnique = m_MaterialPostProcess.GetTechniqueByName( "AntiAliasing4" );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled || m_Renderer.MSAADepthTarget == null )
				return;

			m_Device.AddProfileTask( this, "Anti-Aliasing Pass", "<START>" );

			using ( m_MaterialPostProcess.UseLock() )
			{
#if DEBUG
				if ( m_Renderer.MSAADepthTarget.MultiSamplesCount == 8 )
					m_MaterialPostProcess.CurrentTechnique = m_MaterialPostProcess.GetTechniqueByName( "AntiAliasing8" );
				else
					m_MaterialPostProcess.CurrentTechnique = m_MaterialPostProcess.GetTechniqueByName( "AntiAliasing4" );
#endif
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
				m_Renderer.SetFinalRenderTarget();	// Should render in MaterialBuffer2

 				CurrentMaterial.GetVariableByName( "DepthThreshold" ).AsScalar.Set( m_DepthThreshold );
 				CurrentMaterial.GetVariableByName( "SmoothDistance" ).AsScalar.Set( m_SmoothDistance );
 				CurrentMaterial.GetVariableByName( "SmoothWeights" ).AsScalar.Set( m_SmoothWeights );
 				CurrentMaterial.GetVariableByName( "SmoothInvWeight" ).AsScalar.Set( 1.0f / (1.0f + 4.0f * m_SmoothWeights) );
 				CurrentMaterial.GetVariableByName( "MSAADepth4" ).AsResource.SetResource( m_Renderer.MSAADepthTarget );
 				CurrentMaterial.GetVariableByName( "MSAADepth8" ).AsResource.SetResource( m_Renderer.MSAADepthTarget );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();
				m_Renderer.SwapFinalRenderTarget();
			}

			m_Device.AddProfileTask( this, "Anti-Aliasing Pass", "<END>" );
		}

		#endregion
	}
}
