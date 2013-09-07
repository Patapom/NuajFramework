// Low-quality bloom will use a single tap for downscale & 5 taps for upscale
// Hi-quality bloom will use a5 taps for downscale and upscale
#define HI_QUALITY_BLOOM
//#define USE_HDR_IMAGE

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
	/// Bloom/Streaks post process
	/// </example>
	public class RenderTechniqueBloomStreaks<PF> : RenderTechnique where PF:struct,IPixelFormat
	{
		#region CONSTANTS

		protected const int				BLOOM_PASSES_COUNT = 3;
		protected const int				BLOOM_UPSCALE_PASSES_COUNT = 2;

		protected const int				STREAKS_COUNT = 4;
		protected const int				STREAK_PASSES_COUNT = 3;

		#endregion

		#region NESTED TYPES

		public delegate IRenderTarget	QuerySourceBufferEventHandler();
		public delegate void			SetRenderTargetEventHandler();

		#endregion

		#region FIELDS

		protected RendererSetupBasic		m_Renderer = null;
		protected IRenderTargetFactory		m_RTFactory = null;

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_Pt4>			m_Material = null;

		//////////////////////////////////////////////////////////////////////////
		// Primitives
		protected Nuaj.Helpers.ScreenQuad	m_Quad = null;				// Screen quad for post-processing

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected IRenderTarget				m_SourceImage = null;
		protected IRenderTarget				m_TargetImage = null;

		protected RenderTarget<PF>			m_TargetBloom;
		protected RenderTarget<PF>			m_TargetStreaks;
		protected RenderTarget<PF>[]		m_TargetsStreaks = new RenderTarget<PF>[2];


		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected float						m_Time = 0.0f;

#if USE_HDR_IMAGE
		// Bloom
		protected float						m_BloomThreshold = 0.3f;
		protected float						m_BloomFactor = 1.25f;
		protected float						m_BloomRadius = 0.76f;
		protected float						m_BloomGamma = 2.0f;

		// Streaks
		protected float						m_StreaksThreshold = 1.127f;
		protected float						m_StreaksFactor = 0.15f;
		protected float						m_StreaksAngle = 0.0f * (float) Math.PI / 180.0f;
		protected float						m_StreaksCoverAngle = 92.0f * (float) Math.PI / 180.0f;
		protected float						m_StreaksAttenuation = 0.9216f;
#else
		// Bloom
		protected float						m_BloomThreshold = 0.6609f;
		protected float						m_BloomFactor = 1.8f;
		protected float						m_BloomRadius = 1.51f;
		protected float						m_BloomGamma = 1.496f;

		// Streaks
		protected float						m_StreaksThreshold = 0.686f;
		protected float						m_StreaksFactor = 0.513f;
		protected float						m_StreaksAngle = 0.0f * (float) Math.PI / 180.0f;
		protected float						m_StreaksCoverAngle = 180.0f * (float) Math.PI / 180.0f;
		protected float						m_StreaksAttenuation = 0.8869f;

		protected Vector2[,]				m_StreakDirections = new Vector2[STREAKS_COUNT,STREAK_PASSES_COUNT];
		protected Vector4[]					m_StreakWeights = new Vector4[STREAK_PASSES_COUNT];
#endif

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the source image to tone map
		/// The source image MUST have been created with all mip levels down to the 1x1 size !
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		SourceImage				{ get { return m_SourceImage; } set { m_SourceImage = value; } }

		/// <summary>
		/// Gets or sets the target image to render to
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		TargetImage				{ get { return m_TargetImage; } set { m_TargetImage = value; } }

		/// <summary>
		/// Occurs when the post-process is rendering to query which image it should sample from
		/// This event, if set, takes precedence over the "SourceImage" property
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public event QuerySourceBufferEventHandler			QuerySourceBuffer;

		/// <summary>
		/// Occurs when the post-process is rendering to setup the render target it should render to
		/// This event, if set, takes precedence over the "TargetImage" property
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public event SetRenderTargetEventHandler			SetRenderTarget;

		[System.ComponentModel.Browsable( false )]
		public float				Time					{ get { return m_Time; } set { m_Time = value; } }

		public float				BloomThreshold			{ get { return m_BloomThreshold; } set { m_BloomThreshold = value; } }
		public float				BloomFactor				{ get { return m_BloomFactor; } set { m_BloomFactor = value; } }
		public float				BloomRadius				{ get { return m_BloomRadius; } set { m_BloomRadius = value; } }
		public float				BloomGamma				{ get { return m_BloomGamma; } set { m_BloomGamma = value; } }

		public float				StreaksThreshold		{ get { return m_StreaksThreshold; } set { m_StreaksThreshold = value; } }
		public float				StreaksFactor			{ get { return m_StreaksFactor; } set { m_StreaksFactor = value; } }
		public float				StreaksAngle			{ get { return m_StreaksAngle; } set { m_StreaksAngle = value; RebuildStreaksData(); } }
		public float				StreaksCoverAngle		{ get { return m_StreaksCoverAngle; } set { m_StreaksCoverAngle = value; RebuildStreaksData(); } }
		public float				StreaksAttenuation		{ get { return m_StreaksAttenuation; } set { m_StreaksAttenuation = value; RebuildStreaksData(); } }

		#endregion

		#region METHODS

		/// <summary>
		/// Initializes the technique
		/// </summary>
		/// <param name="_Renderer"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width">The width at which the effect should be rendered</param>
		/// <param name="_Height">The height at which the effect should be rendered</param>
		/// <param name="_StreaksSizeFactor">Streaks can be downscaled even below the provided width/height using that factor (default is 1.0) (This has a strong effect of rendering speed !)</param>
		/// <param name="_RTFactory">A factory of render targets</param>
		public RenderTechniqueBloomStreaks( RendererSetupBasic _Renderer, string _Name, int _Width, int _Height, float _StreaksSizeFactor, IRenderTargetFactory _RTFactory ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;
			m_RTFactory = _RTFactory;

			// Create our main materials
			m_Material = ToDispose( new Material<VS_Pt4>( m_Device, "Bloom/Streaks Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/BloomStreaks/BloomStreaks.fx" ) ) );

			// Create temp targets for bloom/streaks luminance separation
			m_TargetBloom = m_RTFactory.QueryRenderTarget<PF>( this, RENDER_TARGET_USAGE.DISCARD, "BloomTemp", _Width, _Height, 0 );
			m_TargetStreaks = m_RTFactory.QueryRenderTarget<PF>( this, RENDER_TARGET_USAGE.DISCARD, "StreaksTemp", _Width, _Height, 0 );

			// Create temp array targets for streaks processing
			int	StreaksWidth = (int) (_Width * _StreaksSizeFactor);
			int	StreaksHeight = (int) (_Height * _StreaksSizeFactor);
			m_TargetsStreaks[0] = m_RTFactory.QueryRenderTarget<PF>( this, RENDER_TARGET_USAGE.DISCARD, "StreaksTemp Array #0", StreaksWidth, StreaksHeight, 1, STREAKS_COUNT );
			m_TargetsStreaks[1] = m_RTFactory.QueryRenderTarget<PF>( this, RENDER_TARGET_USAGE.DISCARD, "StreaksTemp Array #1", StreaksWidth, StreaksHeight, 1, STREAKS_COUNT );

			// Create the post-process quad
			m_Quad = ToDispose( new Nuaj.Helpers.ScreenQuad( m_Device, "PostProcess Quad" ) );

			RebuildStreaksData();
		}

		public override void	Render( int _FrameToken )
		{
			IRenderTarget	SourceImage = m_SourceImage;
			if ( QuerySourceBuffer != null )
				SourceImage = QuerySourceBuffer();
			if ( SourceImage == null )
				throw new NException( this, "Source image to tone map is not set !" );

			if ( m_TargetImage == null && SetRenderTarget == null )
				throw new NException( this, "Target image to render to is not set !" );

			using ( m_Material.UseLock() )
			{
 				m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

				VariableVector		vBufferInvSize = CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector;
				VariableResource	vSourceBuffer = CurrentMaterial.GetVariableByName( "SourceBuffer" ).AsResource;

				//////////////////////////////////////////////////////////////////////////
				// Separate bloom & streaks
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "LuminanceSeparation" );
				m_Device.SetMultipleRenderTargets( new RenderTarget<PF>[] { m_TargetBloom, m_TargetStreaks } );
				m_Device.SetViewport( 0, 0, m_TargetBloom.Width, m_TargetBloom.Height, 0.0f, 1.0f );

				vBufferInvSize.Set( m_TargetBloom.InvSize3 );
				vSourceBuffer.SetResource( SourceImage );
				CurrentMaterial.GetVariableByName( "LuminanceThresholdBloom" ).AsScalar.Set( m_BloomThreshold );
				CurrentMaterial.GetVariableByName( "BloomFactor" ).AsScalar.Set( m_BloomFactor );
				CurrentMaterial.GetVariableByName( "BloomGamma" ).AsScalar.Set( m_BloomGamma );
				CurrentMaterial.GetVariableByName( "LuminanceThresholdStreaks" ).AsScalar.Set( m_StreaksThreshold );
				CurrentMaterial.GetVariableByName( "StreaksFactor" ).AsScalar.Set( m_StreaksFactor );

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// Apply bloom
#if HI_QUALITY_BLOOM
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ApplyBloomUpScale" );
				EffectPass	Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
#else
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ApplyBloom" );
				EffectPass	Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
#endif
				int	TargetWidth = m_TargetBloom.Width;
				int	TargetHeight = m_TargetBloom.Height;

				CurrentMaterial.GetVariableByName( "BloomRadius" ).AsScalar.Set( m_BloomRadius );

				// Downscale
				for ( int BloomIndex=0; BloomIndex < BLOOM_PASSES_COUNT; BloomIndex++ )
				{
					TargetWidth >>= 1;
					TargetHeight >>= 1;
					m_Device.SetRenderTarget( m_TargetBloom.GetSingleRenderTargetView( 1+BloomIndex, 0 ) );	// Render into next mip level
					m_Device.SetViewport( 0, 0, TargetWidth, TargetHeight, 0.0f, 1.0f );
					vBufferInvSize.Set( new Vector3( 1.0f / TargetWidth, 1.0f / TargetHeight, 0.0f ) );

					vSourceBuffer.SetResource( m_TargetBloom.GetSingleTextureView( BloomIndex, 0 ) );	// Process previous pass

					Pass.Apply();
					m_Quad.Render();
				}

				// Upscale
#if !HI_QUALITY_BLOOM
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ApplyBloomUpScale" );
				Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
#endif
				for ( int BloomIndex=BLOOM_PASSES_COUNT; BloomIndex > BLOOM_PASSES_COUNT-BLOOM_UPSCALE_PASSES_COUNT; BloomIndex-- )
				{
					TargetWidth = m_TargetBloom.Width >> (BloomIndex-1);
					TargetHeight = m_TargetBloom.Height >> (BloomIndex-1);
					m_Device.SetRenderTarget( m_TargetBloom.GetSingleRenderTargetView( BloomIndex-1, 0 ) );	// Render into next mip level
					m_Device.SetViewport( 0, 0, TargetWidth, TargetHeight, 0.0f, 1.0f );
					vBufferInvSize.Set( new Vector3( 1.0f / TargetWidth, 1.0f / TargetHeight, 0.0f ) );

					vSourceBuffer.SetResource( m_TargetBloom.GetSingleTextureView( BloomIndex, 0 ) );	// Process previous pass

					Pass.Apply();
					m_Quad.Render();
				}


				//////////////////////////////////////////////////////////////////////////
				// Apply streaks
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ApplyStreaks" );
				Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				m_Device.SetViewport( 0, 0, m_TargetsStreaks[0].Width, m_TargetsStreaks[0].Height, 0.0f, 1.0f );

				VariableVector	vStreakDirection = CurrentMaterial.GetVariableByName( "StreakDirection" ).AsVector;
				VariableVector	vStreakWeights = CurrentMaterial.GetVariableByName( "Weights" ).AsVector;
				vBufferInvSize.Set( m_TargetsStreaks[0].InvSize3 );

				for ( int StreakIndex=0; StreakIndex < STREAKS_COUNT; StreakIndex++ )
				{
					for ( int PassIndex=0; PassIndex < STREAK_PASSES_COUNT; PassIndex++ )
					{
						int	SourceIndex = PassIndex & 1;
						int	TargetIndex = 1 - SourceIndex;

						m_Device.SetRenderTarget( m_TargetsStreaks[TargetIndex].GetSingleRenderTargetView( 0, StreakIndex ) );

						vStreakDirection.Set( m_StreakDirections[StreakIndex,PassIndex] );
						vStreakWeights.Set( m_StreakWeights[PassIndex] );
						vSourceBuffer.SetResource( PassIndex == 0 ? m_TargetStreaks.TextureView : m_TargetsStreaks[SourceIndex].GetSingleTextureView( 0, StreakIndex ) );

						Pass.Apply();
						m_Quad.Render();
					}
				}


				//////////////////////////////////////////////////////////////////////////
				// Display final result
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Combine" );

				vBufferInvSize.Set( SourceImage.InvSize3 );
				vSourceBuffer.SetResource( SourceImage );
				CurrentMaterial.GetVariableByName( "BloomBuffer" ).AsResource.SetResource( m_TargetBloom.GetSingleTextureView( BLOOM_PASSES_COUNT-BLOOM_UPSCALE_PASSES_COUNT, 0 ) );
				CurrentMaterial.GetVariableByName( "StreaksBuffer" ).AsResource.SetResource( m_TargetsStreaks[STREAK_PASSES_COUNT&1] );

				if ( SetRenderTarget != null )
					SetRenderTarget();
				else
				{
					m_Device.SetRenderTarget( m_TargetImage );
					m_Device.SetViewport( 0, 0, m_TargetImage.Width, m_TargetImage.Height, 0.0f, 1.0f );
				}

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();
			}
		}

		protected void	RebuildStreaksData()
		{
			// Rebuild the set of directions
			for ( int StreakIndex=0; StreakIndex < STREAKS_COUNT; StreakIndex++ )
			{
				float	Angle = m_StreaksAngle + StreakIndex * m_StreaksCoverAngle / STREAKS_COUNT;
				Vector2	Direction = new Vector2( (float) Math.Cos( Angle ), (float) Math.Sin( Angle ) );
				for ( int PassIndex=0; PassIndex < STREAK_PASSES_COUNT; PassIndex++ )
					m_StreakDirections[StreakIndex,PassIndex] = (float) Math.Pow( 4.0, PassIndex ) * Direction;
			}

			for ( int PassIndex=0; PassIndex < STREAK_PASSES_COUNT; PassIndex++ )
			{
				float	b = (float) Math.Pow( 4.0, PassIndex );
				m_StreakWeights[PassIndex] = new Vector4(
					(float) Math.Pow( m_StreaksAttenuation, b*0 ),
					(float) Math.Pow( m_StreaksAttenuation, b*1 ),
					(float) Math.Pow( m_StreaksAttenuation, b*2 ),
					(float) Math.Pow( m_StreaksAttenuation, b*3 ) );
			}
		}

		#endregion
	}
}
