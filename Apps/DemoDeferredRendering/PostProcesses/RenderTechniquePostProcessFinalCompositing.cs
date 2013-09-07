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
	public class RenderTechniquePostProcessFinalCompositing : DeferredRenderTechnique
	{
		#region FIELDS

		protected Material<VS_Pt4V3T2>	m_Material = null;

		protected Camera				m_Camera = null;
		protected Helpers.ScreenQuad	m_Quad = null;
		protected IRenderTarget			m_LightBuffer = null;
		protected IRenderTarget			m_LightGeometryBuffer = null;
		protected IDepthStencil			m_LightDepthStencil = null;

		// The final, composited image
		protected RenderTarget<PF_RGBA16F>	m_CompositedImage = null;

		protected float					m_OffsetAmplitude = -0.5f;
		protected float					m_NormalDifferencesFactor = +1.0f;
		protected float					m_PositionDifferencesFactor = +1.0f;
		protected float					m_LuminanceDifferencesFactor = +0.0f;
		protected float					m_SlopeAttenuation = 0.1f;
		protected float					m_DiffuseAlbedoFactor = 1.0f;
		protected float					m_SpecularAlbedoFactor = 1.0f;

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Browsable( false )]
		public Camera				Camera				{ get { return m_Camera; } set { m_Camera = value; } }
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		LightBuffer			{ get { return m_LightBuffer; } set { m_LightBuffer = value; } }
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		LightGeometryBuffer	{ get { return m_LightGeometryBuffer; } set { m_LightGeometryBuffer = value; } }
		[System.ComponentModel.Browsable( false )]
		public IDepthStencil		LightDepthStencil	{ get { return m_LightDepthStencil; } set { m_LightDepthStencil = value; } }

		/// <summary>
		/// Gets the final composited image
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public RenderTarget<PF_RGBA16F>	CompositedImage	{ get { return m_CompositedImage; } }

		/// <summary>
		/// Gets or sets the amplitude of the offset to displace the sub-pixel fetch position
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the amplitude of the offset to displace the sub-pixel fetch position" )]
		public float				OffsetAmplitude				{ get { return m_OffsetAmplitude; } set { m_OffsetAmplitude = value; } }

		/// <summary>
		/// Gets or sets the factor applied to differences in neighboring normals
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the factor applied to differences in neighboring normals" )]
		public float				NormalDifferencesFactor		{ get { return m_NormalDifferencesFactor; } set { m_NormalDifferencesFactor = value; } }

		/// <summary>
		/// Gets or sets the factor applied to differences in neighboring positions
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the factor applied to differences in neighboring positions" )]
		public float				PositionDifferencesFactor	{ get { return m_PositionDifferencesFactor; } set { m_PositionDifferencesFactor = value; } }

		/// <summary>
		/// Gets or sets the factor applied to differences in neighboring luminances
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the factor applied to differences in neighboring luminances" )]
		public float				LuminanceDifferencesFactor	{ get { return m_LuminanceDifferencesFactor; } set { m_LuminanceDifferencesFactor = value; } }

		/// <summary>
		/// Gets or sets the attenuation of differences in neighboring positions based on viewing angle (differences are decreased when viewing at grazing camera angles)
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the attenuation of differences in neighboring positions based on viewing angle (differences are decreased when viewing at grazing camera angles)" )]
		public float				SlopeAttenuation			{ get { return m_SlopeAttenuation; } set { m_SlopeAttenuation = value; } }

		/// <summary>
		/// Gets or sets the global factor for diffuse albedo
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the global factor for diffuse albedo" )]
		public float				DiffuseAlbedoFactor				{ get { return m_DiffuseAlbedoFactor; } set { m_DiffuseAlbedoFactor = value; } }

		/// <summary>
		/// Gets or sets the global factor for specular albedo
		/// </summary>
		[System.ComponentModel.Description( "Gets or sets the global factor for specular albedo" )]
		public float				SpecularAlbedoFactor				{ get { return m_SpecularAlbedoFactor; } set { m_SpecularAlbedoFactor = value; } }

		#endregion

		#region METHODS

		public RenderTechniquePostProcessFinalCompositing( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "FinalCompositing Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/PostProcessFinalCompositing.fx" ) ) );

			// Create our post-process quad
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Quad", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height ) );

			// Create our final composited buffer
			m_CompositedImage = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Composited Image (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.SetRenderTarget( m_CompositedImage, null );	// Stop using a depth stencil so we can bind it to the shader
			m_Device.SetViewport( 0, 0, m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0.0f, 1.0f );
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				CurrentMaterial.GetVariableByName( "ScreenInfos" ).AsVector.Set( new Vector4( 1.0f / m_Device.DefaultRenderTarget.Width, 1.0f / m_Device.DefaultRenderTarget.Height, m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height ) );
				CurrentMaterial.GetVariableByName( "LightBufferInfos" ).AsVector.Set( new Vector4( 1.0f / m_LightBuffer.Width, 1.0f / m_LightBuffer.Height, m_LightBuffer.Width, m_LightBuffer.Height ) );
				CurrentMaterial.GetVariableByName( "BilateralInfos" ).AsVector.Set( new Vector4( m_OffsetAmplitude, m_NormalDifferencesFactor, m_PositionDifferencesFactor, m_SlopeAttenuation ) );
				CurrentMaterial.GetVariableByName( "BilateralInfos2" ).AsVector.Set( new Vector4( m_LuminanceDifferencesFactor, m_DiffuseAlbedoFactor, m_SpecularAlbedoFactor, 0.0f ) );

				CurrentMaterial.GetVariableByName( "DepthStencil" ).AsResource.SetResource( m_Device.DefaultDepthStencil );
				CurrentMaterial.GetVariableByName( "LightDepthStencil" ).AsResource.SetResource( m_LightDepthStencil );
				CurrentMaterial.GetVariableByName( "LightGeometryBuffer" ).AsResource.SetResource( m_LightGeometryBuffer );
				CurrentMaterial.GetVariableByName( "LightBuffer" ).AsResource.SetResource( m_LightBuffer );

				CurrentMaterial.Render( ( A, B, C ) => { m_Quad.Render(); } );

				// Unbind depth stencil
//				CurrentMaterial.GetVariableByName( "DepthStencil" ).AsResource.SetResource( null as ITexture2D );
			}
		}

		#endregion
	}
}
