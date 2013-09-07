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
	/// Tone-mapping post process
	/// This technique applies "filmic curve" tone mapping as described by John Hable
	///  in http://filmicgames.com/archives/75#more-75 or in his GDC talk about tone mapping
	///  in Uncharted 2 (http://www.gdcvault.com/play/1012459/Uncharted_2__HDR_Lighting)
	/// The filmic curve is a S-shaped curve that has been used for decades by the film
	///  industry (i.e. Kodak or Fuji, not Holywood) as the "film impression" response curve.
	/// For example, consult : http://i217.photobucket.com/albums/cc75/nikonf2/scurve.jpg
	/// </example>
	public class RenderTechniquePostProcessToneMapping : DeferredRenderTechnique
	{
		#region FIELDS

		protected Material<VS_Pt4V3T2>	m_Material = null;

		protected Camera				m_Camera = null;
		protected Helpers.ScreenQuad	m_Quad = null;
		protected IRenderTarget			m_SourceImage = null;

		// Two 1x1 render targets to perform temporal adaptation
		protected RenderTarget<PF_R16F>[]	m_TemporalAdaptationTargets = new RenderTarget<PF_R16F>[2];

		protected bool					m_bEnableToneMapping = true;
		protected float					m_SamplingOffset = 2.0f;
		protected float					m_WhiteLuminanceLevel = 4.0f;
		protected float					m_TemporalAdaptationSpeed = 0.7f;
		protected float					m_Gamma = 1.0f;
		protected float					m_AdaptationLevelMin = 5.0f;
		protected float					m_AdaptationLevelMax = 200.0f;

		// Glow parameters
		protected bool					m_bEnableGlow = false;
		protected float					m_GlowLuminanceThreshold = 15.0f;
		protected float					m_GlowFactor = 0.4f;
		protected float					m_GlowWhite = 1.0f;
		protected float					m_GlowOffset = 0.1f;

		// Filmic operator parameters
		protected float					m_WhitePointValue = 11.2f;
		protected float					m_A = 0.15f;
		protected float					m_B = 0.50f;
		protected float					m_C = 0.10f;
		protected float					m_D = 0.80f;//0.20f;
		protected float					m_E = 0.02f;
		protected float					m_F = 0.30f;

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Browsable( false )]
		public Camera				Camera					{ get { return m_Camera; } set { m_Camera = value; } }
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		SourceImage				{ get { return m_SourceImage; } set { m_SourceImage = value; } }

		public bool					EnableToneMapping					{ get { return m_bEnableToneMapping; } set { m_bEnableToneMapping = value; } }
		public float				SubPixelSamplingOffset	{ get { return m_SamplingOffset; } set { m_SamplingOffset = value; } }

		[System.ComponentModel.Description( "The tone mapped white level will be mapped to that value" )]
		public float				WhiteLuminanceLevel		{ get { return m_WhiteLuminanceLevel; } set { m_WhiteLuminanceLevel = value; } }
		[System.ComponentModel.Description( "The speed at which the camera adapts to luminance" )]
		public float				TemporalAdaptationSpeed	{ get { return m_TemporalAdaptationSpeed; } set { m_TemporalAdaptationSpeed = value; } }
		[System.ComponentModel.Description( "The gamma correction" )]
		public float				Gamma					{ get { return m_Gamma; } set { m_Gamma = value; } }
		[System.ComponentModel.Description( "The minimal adaptable luminance" )]
		public float				AdaptationLevelMin		{ get { return m_AdaptationLevelMin; } set { m_AdaptationLevelMin = value; } }
		[System.ComponentModel.Description( "The maximal adaptable luminance" )]
		public float				AdaptationLevelMax		{ get { return m_AdaptationLevelMax; } set { m_AdaptationLevelMax = value; } }

		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(W) The linear white point value" )]
		public float				WhitePointValue			{ get { return m_WhitePointValue; } set { m_WhitePointValue = value; } }

		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(A) Shoulder strength" )]
		public float				A		{ get { return m_A; } set { m_A = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(B) Linear Strength" )]
		public float				B		{ get { return m_B; } set { m_B = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(C) Linear Angle" )]
		public float				C		{ get { return m_C; } set { m_C = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(D) Toe Strength" )]
		public float				D		{ get { return m_D; } set { m_D = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(E) Toe Numerator" )]
		public float				E		{ get { return m_E; } set { m_E = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(F) Toe Denominator" )]
		public float				F		{ get { return m_F; } set { m_F = value; } }

		[System.ComponentModel.Category( "Glow" )]
		[System.ComponentModel.Description( "Enables/Disables glow" )]
		public bool					EnableGlow					{ get { return m_bEnableGlow; } set { m_bEnableGlow = value; } }
		[System.ComponentModel.Category( "Glow" )]
		[System.ComponentModel.Description( "Luminance threshold below which there is no glow" )]
		public float				GlowLuminanceThreshold		{ get { return m_GlowLuminanceThreshold; } set { m_GlowLuminanceThreshold = value; } }
		[System.ComponentModel.Category( "Glow" )]
		[System.ComponentModel.Description( "Glow factor" )]
		public float				GlowFactor		{ get { return m_GlowFactor; } set { m_GlowFactor = value; } }
		[System.ComponentModel.Category( "Glow" )]
		[System.ComponentModel.Description( "Glow white reference" )]
		public float				GlowWhite		{ get { return m_GlowWhite; } set { m_GlowWhite = value; } }
		[System.ComponentModel.Category( "Glow" )]
		[System.ComponentModel.Description( "Glow offset" )]
		public float				GlowOffset		{ get { return m_GlowOffset; } set { m_GlowOffset = value; } }

		#endregion

		#region METHODS

		public RenderTechniquePostProcessToneMapping( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "ToneMapping Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/PostProcessToneMapping.fx" ) ) );

			// Create our 2 temporal adaptation targets
			m_TemporalAdaptationTargets[0] = ToDispose( new RenderTarget<PF_R16F>( m_Device, "TemporalTarget0", 1, 1, 1 ) );
			m_TemporalAdaptationTargets[1] = ToDispose( new RenderTarget<PF_R16F>( m_Device, "TemporalTarget1", 1, 1, 1 ) );
			m_Device.ClearRenderTarget( m_TemporalAdaptationTargets[0], new Color4( 1.0f, 1.0f, 1.0f, 1.0f ) );
			m_Device.ClearRenderTarget( m_TemporalAdaptationTargets[1], new Color4( 1.0f, 1.0f, 1.0f, 1.0f ) );

			// Create our post-process quad
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Quad" ) );//, m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height ) );
		}

		public override void	Render( int _FrameToken )
		{
			using ( m_Material.UseLock() )
			{
 				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				CurrentMaterial.GetVariableByName( "EnableToneMapping" ).AsScalar.Set( m_bEnableToneMapping );
				CurrentMaterial.GetVariableByName( "EnableGlow" ).AsScalar.Set( m_bEnableGlow );
				VariableVector		vSourceInfos = CurrentMaterial.GetVariableByName( "SourceInfos" ).AsVector;
				VariableVector		vParams = CurrentMaterial.GetVariableByName( "Params" ).AsVector;
				VariableVector		vAdaptationLevels = CurrentMaterial.GetVariableByName( "AdaptationLevels" ).AsVector;
				VariableVector		vGlowParams = CurrentMaterial.GetVariableByName( "GlowParams" ).AsVector;
				VariableResource	vSourceTexture = CurrentMaterial.GetVariableByName( "SourceTexture" ).AsResource;
				VariableResource	vAverageLuminanceTexture = CurrentMaterial.GetVariableByName( "AverageLuminanceTexture" ).AsResource;
				CurrentMaterial.GetVariableByName( "A" ).AsScalar.Set( m_A );
				CurrentMaterial.GetVariableByName( "B" ).AsScalar.Set( m_B );
				CurrentMaterial.GetVariableByName( "C" ).AsScalar.Set( m_C );
				CurrentMaterial.GetVariableByName( "D" ).AsScalar.Set( m_D );
				CurrentMaterial.GetVariableByName( "E" ).AsScalar.Set( m_E );
				CurrentMaterial.GetVariableByName( "F" ).AsScalar.Set( m_F );
				CurrentMaterial.GetVariableByName( "W" ).AsScalar.Set( m_WhitePointValue );

				//////////////////////////////////////////////////////////////////////////
				// First, we need to downsample the source image
				// We __know__ the source image has been created with mip-maps so we simply build those mip-maps...
				//
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DownSampleFirstStage" );	// First stage takes the log( luminance )
				EffectPass	P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				int	PreviousWith, PreviousHeight;
				int	CurrentWidth = m_SourceImage.Width, CurrentHeight = m_SourceImage.Height;

				int	DownSampleStepsCount = m_SourceImage.MipLevelsCount;
				for ( int DownSampleStepIndex=1; DownSampleStepIndex < DownSampleStepsCount; DownSampleStepIndex++ )
				{
					// Setup source and target data
					PreviousWith = CurrentWidth;
					PreviousHeight = CurrentHeight;
					CurrentWidth = Math.Max( 1, CurrentWidth >> 1 );
					CurrentHeight = Math.Max( 1, CurrentHeight >> 1 );

					m_Device.SetRenderTarget( m_SourceImage.GetSingleRenderTargetView( DownSampleStepIndex, 0 ) );
					m_Device.SetViewport( 0, 0, CurrentWidth, CurrentHeight, 0.0f, 1.0f );

					vSourceInfos.Set( new Vector2( 1.0f / PreviousWith, 1.0f / PreviousHeight ) );
					vSourceTexture.SetResource( m_SourceImage.GetSingleTextureView( DownSampleStepIndex-1, 0 ) );
					P.Apply();
					m_Quad.Render();

					if ( DownSampleStepIndex == 1 )
					{	// Swap to normal downscale (i.e. no log)
						CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DownSample" );
						P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
					}
				}

				//////////////////////////////////////////////////////////////////////////
				// Perform temporal adaptation
				RenderTarget<PF_R16F>	Temp = m_TemporalAdaptationTargets[0];
				m_TemporalAdaptationTargets[0] = m_TemporalAdaptationTargets[1];
				m_TemporalAdaptationTargets[1] = Temp;

				m_Device.SetRenderTarget( m_TemporalAdaptationTargets[1] );
				m_Device.SetViewport( 0, 0, 1, 1, 0.0f, 1.0f );

				// Use our 1x1 mip level (global average luminance) to adapt with previous frame's average luminance
				vSourceTexture.SetResource( m_SourceImage.GetSingleTextureView( DownSampleStepsCount-1, 0 ) );

				// And feedback our currently adapted luminance from last frame
				vAverageLuminanceTexture.SetResource( m_TemporalAdaptationTargets[0].TextureView );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "TemporalAdaptation" );
				P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				P.Apply();
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// Display the tone mapped result
				m_Device.SetRenderTarget( m_Device.DefaultRenderTarget );
				m_Device.SetViewport( 0, 0, m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0.0f, 1.0f );

				vSourceInfos.Set( new Vector3( 1.0f / m_SourceImage.Width, 1.0f / m_SourceImage.Height, 0.0f ) );
				vParams.Set( new Vector4( m_SamplingOffset, m_WhiteLuminanceLevel, m_TemporalAdaptationSpeed, 1.0f / m_Gamma ) );
				vAdaptationLevels.Set( new Vector2( m_AdaptationLevelMin, m_AdaptationLevelMax ) );
				vGlowParams.Set( new Vector4( m_GlowLuminanceThreshold, m_GlowFactor, m_GlowWhite, m_GlowOffset ) );
				vSourceTexture.SetResource( m_SourceImage.TextureView );
				vAverageLuminanceTexture.SetResource( m_TemporalAdaptationTargets[1] );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ToneMap" );
				P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				P.Apply();
				m_Quad.Render();
			}
		}

		#endregion
	}
}
