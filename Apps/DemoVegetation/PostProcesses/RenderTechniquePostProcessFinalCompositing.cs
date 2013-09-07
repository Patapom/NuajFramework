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
	/// Final image composition post process
	/// This pass composes the deferred lighting and material buffers to create the final HDR image that will need post-processing
	/// </example>
	public class RenderTechniquePostProcessFinalCompositing : RenderTechniqueBase
	{
		#region FIELDS

		protected Material<VS_Pt4>		m_Material = null;

		protected Camera				m_Camera = null;
		protected Helpers.ScreenQuad	m_Quad = null;

		// The final, composited image
		protected RenderTarget<PF_RGBA16F>	m_CompositedImage = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the final composited image
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public RenderTarget<PF_RGBA16F>	CompositedImage	{ get { return m_CompositedImage; } }

		#endregion

		#region METHODS

		public RenderTechniquePostProcessFinalCompositing( RendererSetup _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_Pt4>( m_Device, "FinalCompositing Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/PostProcessFinalCompositing.fx" ) ) );

			// Create our post-process quad
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Quad", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height ) );

			// Create our final composited buffer
			m_CompositedImage = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Composited Image (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.SetRenderTarget( m_CompositedImage, null );	// Stop using the depth stencil so we can bind it to the shader
			m_Device.SetViewport( 0, 0, m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0.0f, 1.0f );
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector.Set( m_Device.DefaultRenderTarget.InvSize2 );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}
		}

		#endregion
	}
}
