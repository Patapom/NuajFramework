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
	/// Colorimetry & Tone Mapping
	/// </example>
	public class PostProcessColorimetry : RenderTechniqueBase
	{
		#region CONSTANTS

		#endregion

		#region NESTED TYPES

		#endregion

		#region FIELDS

		protected RendererSetupDemo			m_Renderer = null;

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects
		protected Helpers.ScreenQuad		m_Quad = null;		// Screen quad for post-processing

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected Camera					m_Camera = null;
		protected float						m_Time = 0.0f;
		protected Vector3					m_LightPosition = new Vector3( 1, 1, -1 );
		protected float						m_LightIntensity = 1.0f;

		#endregion

		#region PROPERTIES

		public Camera						Camera				{ get { return m_Camera; } set { m_Camera = value; } }
		public float						Time				{ get { return m_Time; } set { m_Time = value; } }
		public Vector3						LightPosition		{ get { return m_LightPosition; } set { m_LightPosition = value; } }
		public float						LightIntensity		{ get { return m_LightIntensity; } set { m_LightIntensity = value; } }

		#endregion

		#region METHODS

		public	PostProcessColorimetry( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;

			// Create our main materials
// 			m_MaterialPostProcess = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Post-Process Colorimetry Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/PostProcessColorimetry.fx" ) ) );

// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the caustics texture array (6 cube faces)
// 			m_CausticsTexture = ToDispose( new RenderTarget<PF_R16F>( Device, "Caustics Texture", RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 1, 6, 1 ) );
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the 2 lens flare textures
// 			m_LensFlareTextures[0] = ToDispose( new RenderTarget<PF_RGBA8>( Device, "LensFlare Texture 0", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, 1 ) );
// 			m_LensFlareTextures[1] = ToDispose( new RenderTarget<PF_RGBA8>( Device, "LensFlare Texture 1", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, 1 ) );
// 			m_LensFlareDepthStencil = ToDispose( new DepthStencil<PF_D32>( Device, "LensFlare DepthStencil", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, false ) );

			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Lens-Flare Quad" ) );
		}

		public override void	Render( int _FrameToken )
		{
			//////////////////////////////////////////////////////////////////////////
			// 3] Perform cloud rendering in screen space
// 			using ( m_MaterialPostProcess.UseLock() )
// 			{
// 				VariableResource	vSourceTexture = CurrentMaterial.GetVariableByName( "SourceTexture" ).AsResource;
// 				VariableVector		vInvSourceTextureSize = CurrentMaterial.GetVariableByName( "InvSourceTextureSize" ).AsVector;
// 
// 				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderCloudLighting" );
// 
// 				m_Device.SetRenderTarget( m_CloudMaps[0] );
// 				m_Device.SetViewport( 0, 0, m_CloudMaps[0].Width, m_CloudMaps[0].Height, 0.0f, 1.0f );
// 				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
// 				m_Device.ClearRenderTarget( m_CloudMaps[0], new Color4( 0.0f ) );
// 
// 				vSourceTexture.SetResource( m_CloudMapNormalDepth );
// 				vInvSourceTextureSize.Set( new Vector3( 1.0f / m_Device.DefaultRenderTarget.Width, 1.0f / m_Device.DefaultRenderTarget.Height, 0.0f ) );
// 
// 				CurrentMaterial.ApplyPass( 0 );
// 				m_Quad.Render();
// 
// 			}
		}

		#endregion
	}
}
