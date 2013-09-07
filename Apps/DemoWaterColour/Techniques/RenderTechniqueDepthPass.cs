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
	/// Depth-pass pre-process
	/// </example>
	public class RenderTechniqueDepthPass : RenderTechniqueBase
	{
		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_P3>				m_Material = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects
		protected List<IDepthPassRenderable>	m_Renderables = new List<IDepthPassRenderable>();

		#endregion

		#region METHODS

		public RenderTechniqueDepthPass( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = m_Renderer.LoadMaterial<VS_P3>( "DepthPass Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/DepthPass.fx" ) );
		}

		/// <summary>
		/// Adds a renderable object/technique
		/// </summary>
		/// <param name="_Renderable"></param>
		public void		AddRenderable( IDepthPassRenderable _Renderable )
		{
			m_Renderables.Add( _Renderable );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			m_Device.AddProfileTask( this, "Depth Pass", "<START>" );

 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				// Setup only a depth stencil (no render target) and clear it
				m_Device.SetRenderTarget( null as IRenderTarget, m_Device.DefaultDepthStencil );
				m_Device.SetViewport( 0, 0, m_Device.DefaultDepthStencil.Width, m_Device.DefaultDepthStencil.Height, 0.0f, 1.0f );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;

				int FrameToken = m_FrameToken-10;	// -10 so we can always render objects for the ZPass and so they can render again in normal passes afterward
				foreach ( IDepthPassRenderable Renderable in m_Renderables )
					Renderable.RenderDepthPass( FrameToken, Pass, vLocal2World );

				// Perform the MSAA pass
				if ( m_Renderer.MSAADepthTarget != null )
				{
					m_Device.AddProfileTask( this, "Depth Pass", "MSAA Depth" );

					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawDepthMSAA" );
					Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

					m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
					m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.MIN );

					m_Device.SetRenderTarget( m_Renderer.MSAADepthTarget );
					m_Device.ClearRenderTarget( m_Renderer.MSAADepthTarget, new Color4( 1e5f ) );

					FrameToken++;
					foreach ( IDepthPassRenderable Renderable in m_Renderables )
						Renderable.RenderDepthPass( FrameToken, Pass, vLocal2World );

					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawDepth" );
				}
			}

			m_Device.AddProfileTask( this, "Depth Pass", "<END>" );
		}

		#endregion
	}
}
