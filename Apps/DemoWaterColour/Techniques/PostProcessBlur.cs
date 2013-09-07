using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{

/*
BOKEH :
Fixed taps count in standard buffer (WIDTH/N)
High taps count in smaller buffer (WIDTH/4N)
33 taps

SLIDE 57
The size of bokeh can be computed with the Lens Equation

x = abs( of/(o-f) - pf/(p-f) ) * (p-f)/(pF)

x: diameter of blur (CoC)
o: object distance
p: focal distance
f: focal length
F: F-stop

or check http://en.wikipedia.org/wiki/Circle_of_confusion


Don't use GAUSSIAN ! (cf slide 59)

SLIDE 69
Aperture shape
F-Stop describes how wide the aperture is
The shape of aperture is…
More circular when opened
Tends towards a polygonal shape as it is closed
Photo: F5.6 circle F13 hexagon

SLIDE 76
Cat's eye effect
search Cosine Fourth Law:
	http://www.canon.com/camera-museum/tech/report/200801/column.html
	http://www.cartage.org.lb/en/themes/Arts/photography/photproces/pinholephot/pinholcam/pinholecam.htm
	http://toothwalker.org/optics/vignetting.html#optical
	http://doug.kerr.home.att.net/pumpkin/index.htm#CosFourth <== seems to be down

struct LensParameter
{
	string	szName[64];
	u8	nAppertureAngleNumber;
	f32	fDesignedFilmSize;
	f32	fMinFStop;
	f32	fMaxFStop;
	f32	fFStopZoom;
	f32	fMinFocusDepth;
	f32	fMinProjectionDistance;
	f32	fMaxProjectionDistance;
	//       -----------------               Vignetting Distance
	//         ||              ---------------  <----------->
	//         ||                             ----------------               |
	//         ||                  ||    |                 ||                |
	//       Entrance              ||  Open Ap ||Vignetting||                |
	//       Size                  ||   Size   ||Size      ||                |
	//         ||                  ||    |                 ||                |
	//         ||                             ----------------               |
	//         ||              ---------------               <-Frange Back ->
	//       -----------------           ^Iris
	//                                    <- Iris Distance ->
	//          <------------ Entrance Distance ------------>
	f32	fEntranceDistance;
	f32	fEntranceSize;
	f32	fApertureDistance;
	f32	fOpenApertureSize;

SLIDE 81
Lens Formula 

1/a + 1/b = 1/f

a: distance to object
b: distance to image plane
f: focal length

m = a/b magnification (FOV) 

If you want to change the focal distance (a) and don’t want to change the view angle (m), change the distance to the image plane (b)
However the distance to the image plane can’t be moved because it’s based on camera design
For some lenses, distance to the image plane can be “virtually” changed 
Therefore, the view angle changes when changing the focal distance

*/

	/// <summary>
	/// Blur & bokeh effect
	/// </example>
	public class PostProcessBlur : RenderTechniqueBase
	{
		#region CONSTANTS

		protected const int		GAUSSIAN_STEPS_COUNT = 6;

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;

		Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>	m_ImageScaler = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected RenderTarget<PF_RGBA16F>	m_GBufferBack = null;
		protected RenderTarget<PF_RGBA16F>	m_GBufferFront = null;

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected float						m_BlurSize = 4.0f;
		protected float						m_BlurDepth = 8.0f;
		protected bool						m_bBlurFront = false;
		protected bool						m_bEnableBokeh = true;
		protected float						m_BokehSize = 6.0f;
		protected float						m_DepthBias = 0.5f;
		protected float						m_BlendPower = 0.1f;

		#endregion

		#region PROPERTIES

		public float						BlurSize			{ get { return m_BlurSize; } set { m_BlurSize = value; } }
		public float						BlurDepth			{ get { return m_BlurDepth; } set { m_BlurDepth = value; } }
		public bool							BlurFront			{ get { return m_bBlurFront; } set { m_bBlurFront = value; } }
		public bool							EnableBokeh			{ get { return m_bEnableBokeh; } set { m_bEnableBokeh = value; } }
		public float						BokehSize			{ get { return m_BokehSize; } set { m_BokehSize = value; } }
		public float						DepthBias			{ get { return m_DepthBias; } set { m_DepthBias = value; } }
		public float						BlendPower			{ get { return m_BlendPower; } set { m_BlendPower = value; } }

		#endregion

		#region METHODS

		public	PostProcessBlur( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
m_bEnabled = false;

			// Create our main materials
 			m_MaterialPostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "Post-Process DOF Blur Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/PostProcessBlur.fx" ) );

			// Create the separate GBuffer images
			m_GBufferBack = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "GBuffer Back", m_Renderer.GeometryBuffer.Width, m_Renderer.GeometryBuffer.Height, 1 ) );
			m_GBufferFront = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "GBuffer Front", m_Renderer.GeometryBuffer.Width, m_Renderer.GeometryBuffer.Height, 1 ) );

			// Build the precise image scaler
			m_ImageScaler = ToDispose( new Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>( m_Device, "ImageScaler",
					Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>.QUALITY.DEFAULT,
					Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>.METHOD.DEFAULT ) );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			using ( m_MaterialPostProcess.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

 				VariableResource	vGBufferSource = CurrentMaterial.GetVariableBySemantic( "GBUFFER_TEX0" ).AsResource;

				//////////////////////////////////////////////////////////////////////////
				// 1] Separate front from back
				m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_GBufferFront, m_GBufferBack } );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Separate" );
				CurrentMaterial.GetVariableByName( "BlurDepth" ).AsScalar.Set( m_BlurDepth );
				CurrentMaterial.GetVariableByName( "DepthBias" ).AsScalar.Set( m_DepthBias );
				CurrentMaterial.GetVariableByName( "BlurFront" ).AsScalar.Set( m_bBlurFront );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();

				//////////////////////////////////////////////////////////////////////////
				// 2] Apply blur
				float	fImageFactor = 1.0f / Math.Max( 1.0f, m_BlurSize );
				int		TargetWidth = (int) Math.Ceiling( fImageFactor * m_GBufferFront.Width );
				int		TargetHeight = (int) Math.Ceiling( fImageFactor * m_GBufferFront.Height );

				// 2.1] Downscale
				m_ImageScaler.SetTexture( m_bBlurFront ? m_GBufferFront : m_GBufferBack );
				m_ImageScaler.PreviouslyRenderedRenderTarget = m_Renderer.MaterialBuffer2;
				m_ImageScaler.LastRenderedRenderTarget = m_Renderer.MaterialBuffer;
				m_ImageScaler.Scale( TargetWidth, TargetHeight, ( RenderTarget<PF_RGBA16F> _RenderTarget, int _Width, int _Height, bool _bLastStage ) =>
					{
						m_Renderer.SwapFinalRenderTarget();
					} );

				// 2.2] Bokeh
				if ( m_bEnableBokeh )
				{
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Bokeh" );
					m_Device.SetRenderTarget( m_ImageScaler.PreviouslyRenderedRenderTarget );

					CurrentMaterial.GetVariableByName( "BokehSize" ).AsScalar.Set( m_BokehSize );
					CurrentMaterial.GetVariableByName( "BokehMaxUV" ).AsVector.Set( new Vector2( (TargetWidth-0.5f) / m_GBufferFront.Width, (TargetHeight-0.5f) / m_GBufferFront.Height ) );
					CurrentMaterial.GetVariableByName( "BokehUVScale" ).AsVector.Set( new Vector2( (float) m_GBufferFront.Width / TargetWidth, (float) m_GBufferFront.Height / TargetHeight ) );
					vGBufferSource.SetResource( m_ImageScaler.LastRenderedRenderTarget );

					CurrentMaterial.ApplyPass( 0 );
					m_Renderer.RenderPostProcessQuad();
					m_ImageScaler.SwapRenderTargets();
					m_Renderer.SwapFinalRenderTarget();
				}

				// 2.3] Upscale back but no more than 1/4 original size
				float	fMaxOriginalSizeRatio = 0.5f;
				TargetWidth = (int) Math.Max( TargetWidth, fMaxOriginalSizeRatio * m_GBufferFront.Width );
				TargetHeight = (int) Math.Max( TargetHeight, fMaxOriginalSizeRatio * m_GBufferFront.Height );
				m_ImageScaler.Scale( TargetWidth, TargetHeight, ( RenderTarget<PF_RGBA16F> _RenderTarget, int _Width, int _Height, bool _bLastStage ) =>
					{
						m_Renderer.SwapFinalRenderTarget();
					} );

				//////////////////////////////////////////////////////////////////////////
				// 3] Recombine front & back
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Combine" );
				m_Renderer.SetFinalRenderTarget();

				CurrentMaterial.GetVariableByName( "TextureBack" ).AsResource.SetResource( m_bBlurFront ? m_GBufferBack : m_Renderer.MaterialBuffer );
				CurrentMaterial.GetVariableByName( "SourceSizeFactorFront" ).AsVector.Set(
					m_bBlurFront ?
					new Vector2( (float) TargetWidth / m_GBufferBack.Width, (float) TargetHeight / m_GBufferBack.Height ) :
					new Vector2( 1.0f, 1.0f ) );

				CurrentMaterial.GetVariableByName( "TextureFront" ).AsResource.SetResource( m_bBlurFront ? m_Renderer.MaterialBuffer : m_GBufferFront );
				CurrentMaterial.GetVariableByName( "SourceSizeFactorBack" ).AsVector.Set(
					m_bBlurFront ?
					new Vector2( 1.0f, 1.0f ) :
					new Vector2( (float) TargetWidth / m_GBufferFront.Width, (float) TargetHeight / m_GBufferFront.Height ) );

				CurrentMaterial.GetVariableByName( "BlendPower" ).AsScalar.Set( m_BlendPower );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();
				m_Renderer.SwapFinalRenderTarget();
			}
		}

		#endregion
	}
}
