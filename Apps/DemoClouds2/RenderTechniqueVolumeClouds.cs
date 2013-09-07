#define DEEP_SHADOW_MAP_HI_RES	// Define this to use hi-def deep shadow map (make sure to also define this in the shader !)
//#define USE_2D_JITTERING

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
	/// Motherfucking Cloud Effect
	/// </example>
	public class RenderTechniqueVolumeClouds : RenderTechnique, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const float			PLANET_RADIUS_KM = 6400.0f;
		protected const float			CLOUD_DOWNSCALE_FACTOR = 0.25f;
		protected const int				DEEP_SHADOW_MAP_SIZE = 1024;

		protected const int				LIGHTNINGS_COUNT = 1;

		// Ambient sky rendering
		protected const int				AMBIENT_PROBE_SIZE = 32;			// Size of the ambient sky rendering probe (actual size is (2*AMBIENT_PROBE_SIZE) x AMBIENT_PROBE_SIZE)
		protected const float			AMBIENT_PROBE_THETA_START = 0.02f * (float) Math.PI;	// Start almost from the top
		protected const float			AMBIENT_PROBE_THETA_END = 0.5f * (float) Math.PI;		// End at the horizon
		protected const float			AMBIENT_PROBE_PHI_START = 0.0f * (float) Math.PI;
		protected const float			AMBIENT_PROBE_PHI_END = 2.0f * (float) Math.PI;

		#endregion

		#region NESTED TYPES

		protected class	ICloudSupport : ShaderInterfaceBase
		{
			[Semantic( "CLOUD_SIGMA_S" )]
			public float		SigmaScattering				{ set { SetScalar( "CLOUD_SIGMA_S", value ); } }
			[Semantic( "CLOUD_SIGMA_T" )]
			public float		SigmaExtinction				{ set { SetScalar( "CLOUD_SIGMA_T", value ); } }

			[Semantic( "CLOUD_SHADOW_OPACITY" )]
			public float		ShadowOpacity				{ set { SetScalar( "CLOUD_SHADOW_OPACITY", value ); } }
			[Semantic( "CLOUD_ALTITUDE_THICKNESS" )]
			public Vector4		CloudAltitudeThicknessKm	{ set { SetVector( "CLOUD_ALTITUDE_THICKNESS", value ); } }
			[Semantic( "CLOUD_LIGHTNING_INTENSITY" )]
			public float		LightningIntensity			{ set { SetScalar( "CLOUD_LIGHTNING_INTENSITY", value ); } }

			// Shadow Map parameters
			[Semantic( "SHADOW_PLANE_CENTER" )]
			public Vector3		ShadowPlaneCenterKm			{ set { SetVector( "SHADOW_PLANE_CENTER", value ); } }
			[Semantic( "SHADOW_PLANE_X" )]
			public Vector3		ShadowPlaneX				{ set { SetVector( "SHADOW_PLANE_X", value ); } }
			[Semantic( "SHADOW_PLANE_Y" )]
			public Vector3		ShadowPlaneY				{ set { SetVector( "SHADOW_PLANE_Y", value ); } }
			[Semantic( "SHADOW_PLANE_OFFSET" )]
			public Vector2		ShadowPlaneOffset			{ set { SetVector( "SHADOW_PLANE_OFFSET", value ); } }
			[Semantic( "SHADOW_QUAD_VERTICES" )]
			public Vector4		ShadowQuadVertices			{ set { SetVector( "SHADOW_QUAD_VERTICES", value ); } }
			[Semantic( "SHADOW_NORMALS_U" )]
			public Vector4		ShadowNormalsU				{ set { SetVector( "SHADOW_NORMALS_U", value ); } }
			[Semantic( "SHADOW_NORMALS_V" )]
			public Vector4		ShadowNormalsV				{ set { SetVector( "SHADOW_NORMALS_V", value ); } }
			[Semantic( "SHADOW_ABC" )]
			public Vector3		ShadowABC					{ set { SetVector( "SHADOW_ABC", value ); } }
			[Semantic( "SHADOW_DEF" )]
			public Vector3		ShadowDEF					{ set { SetVector( "SHADOW_DEF", value ); } }
			[Semantic( "SHADOW_GHI" )]
			public Vector3		ShadowGHI					{ set { SetVector( "SHADOW_GHI", value ); } }
			[Semantic( "SHADOW_JKL" )]
			public Vector3		ShadowJKL					{ set { SetVector( "SHADOW_JKL", value ); } }
			[Semantic( "CLOUD_DSM0" )]
			public ITexture2D	DeepShadowMap0				{ set { SetResource( "CLOUD_DSM0", value ); } }
			[Semantic( "CLOUD_DSM1" )]
			public ITexture2D	DeepShadowMap1				{ set { SetResource( "CLOUD_DSM1", value ); } }

			// Lightning parameters
			[Semantic( "CLOUD_LIGHTNING_POSITION" )]
			public Vector3		LightningPosition			{ set { SetVector( "CLOUD_LIGHTNING_POSITION", value ); } }
		}

		protected class IAmbientSkySH : ShaderInterfaceBase
		{
			[Semantic( "AMBIENT_SKY_SH_TEXTURE" )]
			public ITexture2D	AmbientSkySHTexture			{ set { SetResource( "AMBIENT_SKY_SH_TEXTURE", value ); } }
			[Semantic( "AMBIENT_SKY_SH_NO_CLOUD_TEXTURE" )]
			public ITexture2D	AmbientSkySHTextureNoCloud	{ set { SetResource( "AMBIENT_SKY_SH_NO_CLOUD_TEXTURE", value ); } }
		}

		#endregion

		#region FIELDS

		protected RendererSetupBasic		m_Renderer = null;

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_Pt4>			m_MaterialDownscaleZ = null;
		protected Material<VS_Pt4>			m_MaterialCloud = null;
		protected Material<VS_Pt4>			m_MaterialSkyProbe = null;
		protected Material<VS_Pt4>			m_MaterialDeepShadowMap = null;

		//////////////////////////////////////////////////////////////////////////
		// Sky support
		protected Nuaj.Cirrus.Atmosphere.SkySupport	m_SkySupport = null;

		//////////////////////////////////////////////////////////////////////////
		// Primitives
		protected Nuaj.Helpers.ScreenQuad	m_Quad = null;				// Screen quad for post-processing
// 		protected Lightning[]				m_Lightnings = new Lightning[LIGHTNINGS_COUNT];

		protected BoundingBox				m_TerrainAABB = new BoundingBox();

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected IRenderTarget[]			m_RenderTargets = null;

		protected RenderTarget<PF_RGBA16F>	m_VolumeRenderInScattering = null;
		protected RenderTarget<PF_RGBA16F>	m_VolumeRenderExtinction = null;
		protected RenderTarget<PF_RGBA16F>	m_AmbientProbe = null;
		protected RenderTarget<PF_RGBA16F>	m_AmbientProbeShadowMap = null;
		protected Texture2D<PF_RGBA16F>		m_SHConvolution = null;

		protected RenderTarget<PF_R16F>		m_DownscaledZBuffer = null;
		protected RenderTarget<PF_RGBA16F>[]	m_DeepShadowMaps = new RenderTarget<PF_RGBA16F>[2];

		protected Texture2D<PF_R16F>		m_CloudProfile = null;

		protected ITexture2D				m_AmbientSkySH = null;
		protected ITexture2D				m_AmbientSkySHNoCloud = null;


		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected Camera					m_Camera = null;
		protected DirectionalLight			m_Light = null;

		protected float						m_BilateralOffset = 0.9f;
		protected float						m_BilateralThreshold = 10.0f;

		protected float						m_Density = 1.0f;					// Cloud density
		protected float						m_Albedo = 0.95f;					// Cloud albeod
		protected float						m_ShadowOpacity = 1.0f;
		protected float						m_DirectionalFactor = 1.0f;
		protected float						m_IsotropicFactor = 1.0f;

		// Isotropic scattering
//		protected Vector2					m_IsotropicScatteringFactors = new Vector2( 4.0f, 0.05f );
		protected Vector2					m_IsotropicScatteringFactors = new Vector2( 40.0f, 0.2f );

//		protected Vector3					m_NightSkyAmbientColor = 0.11764705882352941176470588235294f * new Vector3( 190 / 255.0f, 220 / 255.0f, 255 / 255.0f );
		protected Vector3					m_NightSkyAmbientColor = Vector3.Zero;
 		protected Vector3					m_TerrainAlbedo = 0.5f * new Vector3( 74.0f / 255.0f, 48.0f / 255.0f, 32.0f / 255.0f );
//		protected Vector3					m_TerrainAlbedo = Vector3.Zero;

#if USE_2D_JITTERING
		protected float						m_CloudOpacityFactor = 5.0f;
		protected float						m_JitterNoiseSize = 0.02f;
		protected float						m_SamplingJitterAmplitude = 0.2f;
#else
		protected float						m_CloudOpacityFactor = 5.0f;
		protected float						m_JitterNoiseSize = 0.02f;
		protected float						m_SamplingJitterAmplitude = 0.0f;
#endif

// 		protected float						m_PhaseWeightStrongForward = 0.08f;
// 		protected float						m_PhaseWeightForward = 0.3f;
// 		protected float						m_PhaseWeightBackward = 0.25f;
// 		protected float						m_PhaseWeightSide = 0.8f;
// 		protected float						m_PhaseWeightSide2 = 0.2f;
// 		protected float						m_PhaseWeightStrongForward = 1.0f;
// 		protected float						m_PhaseWeightForward = 1.0f;
// 		protected float						m_PhaseWeightBackward = 0.2f;
// 		protected float						m_PhaseWeightSide = 1.0f;
// 		protected float						m_PhaseWeightSide2 = 1.0f;
// 		protected float						m_PhaseWeightStrongForward = 1.0f;
// 		protected float						m_PhaseWeightForward = 1.0f;
// 		protected float						m_PhaseWeightBackward = 0.1f;
// 		protected float						m_PhaseWeightSide = 0.1f;
// 		protected float						m_PhaseWeightSide2 = 10.0f;
		protected float						m_PhaseWeightStrongForward = 1.0f;
		protected float						m_PhaseWeightForward = 2.0f;
		protected float						m_PhaseWeightBackward = 4.0f;
		protected float						m_PhaseWeightSide = 2.0f;
		protected float						m_PhaseWeightSide2 = 0.0f;

		protected float						m_ScatteringAnisotropyStrongForward = 0.95f;
		protected float						m_ScatteringAnisotropyForward = 0.8f;
		protected float						m_ScatteringAnisotropyBackward = -0.2f;
		protected float						m_ScatteringAnisotropySide = -0.2f;

		protected Vector4					m_CloudAltitudeThicknessKm = new Vector4( 2.0f, 4.0f, 2.0f, 1.0f / 2.0f );

		protected Vector4					m_CoverageOffsets = new Vector4( 0.1f, 0.1f, 1.0f, 0.1f );
		protected float						m_CoverageContrast = 0.5f;
		protected float						m_NoiseSize = 0.005f;
		protected float						m_NoiseSizeVerticalFactor = 1.0f;
		protected float						m_NoiseFrequencyFactor = 3.0f;
		protected float						m_NoiseAmplitudeFactor = 0.4f;

		protected float						m_WindForce = 0.02f;
		protected Vector2					m_WindDirection = Vector2.UnitX;
		protected float						m_EvolutionSpeed = 8.0f;

// 		protected float						m_CloudSpeed = 0.1f;
// 		protected float						m_CloudEvolutionSpeed = 0.2f;

		protected float						m_VoxelMipFactor = 0.05f;			// Mip factor for cloud tracing
		protected float						m_VoxelMipFactorShadow = 0.002f;	// Mip factor for shadow tracing

		// Noise fade
		protected float						m_UniformNoiseDensityAtDistance = 1.1f;
		protected Vector2					m_UniformNoiseFadeDistancesKm = new Vector2( 80.0f, 150.0f );

		// Trace clipping
		protected float						m_FarClipAirKm = 80.0f;
		protected float						m_FarClipCloudsKm = 200.0f;

		// Upscale
		protected bool						m_bShowRefinement = false;
		protected float						m_RefinementZThreshold = 200.0f;

		// Shadow
		protected float						m_ShadowFarDistanceKm = 100.0f;


// DEBUG
//protected Vector4	m_DEBUG0 = new Vector4( 30.0f, 0.025f, 1, 0.6f );
protected Vector4	m_DEBUG0 = new Vector4( 2.0f, 0.4f, 1, 0.6f );
protected Vector4	m_DEBUG1 = Vector4.Zero;
protected Vector4	m_DEBUG2 = Vector4.Zero;


		protected float						m_PreviousTime = 0.0f;
		protected float						m_Time = 0.0f;

		//////////////////////////////////////////////////////////////////////////
		// Internal
		protected Vector4					m_CloudPosition = Vector4.Zero;	// Our very own position accumulators

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the camera used by the clouds simulation
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Camera						Camera					{ get { return m_Camera; } set { m_Camera = value; } }

		/// <summary>
		/// Gets or sets the light driven by the clouds simulation
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public DirectionalLight				Light					{ get { return m_Light; } set { m_Light = value; UpdateLightValues(); } }

		[System.ComponentModel.Browsable( false )]
		public float						Time					{ get { return m_Time; } set { m_PreviousTime = m_Time; m_Time = value; } }

		[System.ComponentModel.Browsable( false )]
		public BoundingBox					TerrainAABB				{ get { return m_TerrainAABB; } set { m_TerrainAABB = value; } }

		[System.ComponentModel.Browsable( false )]
		public ITexture2D					AmbientSkySH			{ get { return m_AmbientSkySH; } set { m_AmbientSkySH = value; } }
		[System.ComponentModel.Browsable( false )]
		public ITexture2D					AmbientSkySHNoCloud		{ get { return m_AmbientSkySHNoCloud; } set { m_AmbientSkySHNoCloud = value; } }


		// Sky support variables
		[System.ComponentModel.Browsable( false )]
		public Nuaj.Cirrus.Atmosphere.SkySupport	Sky				{ get { return m_SkySupport; } }

		[System.ComponentModel.Category( "Sky" )]
		public float						SunPhi					{ get { return m_SkySupport.SunPhi; } set { m_SkySupport.SunPhi = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						SunTheta				{ get { return m_SkySupport.SunTheta; } set { m_SkySupport.SunTheta = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						SunIntensity			{ get { return m_SkySupport.SunIntensity; } set { m_SkySupport.SunIntensity = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						SkyDensityRayleigh		{ get { return m_SkySupport.DensityRayleigh; } set { m_SkySupport.DensityRayleigh = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						SkyDensityMie			{ get { return m_SkySupport.DensityMie; } set { m_SkySupport.DensityMie = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						ScatteringAnisotropy	{ get { return m_SkySupport.ScatteringAnisotropy; } set { m_SkySupport.ScatteringAnisotropy = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						WorldUnit2Kilometer		{ get { return m_SkySupport.WorldUnit2Kilometer; } set { m_SkySupport.WorldUnit2Kilometer = value; UpdateLightValues(); } }

		[System.ComponentModel.Browsable( false )]
		public Vector3						SunDirection			{ get { return m_SkySupport.SunDirection; } }

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudAltitudeBottomKm	{ get { return m_CloudAltitudeThicknessKm.X; } set { m_CloudAltitudeThicknessKm.X = value; m_CloudAltitudeThicknessKm.Y = m_CloudAltitudeThicknessKm.X + m_CloudAltitudeThicknessKm.Z; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudAltitudeTopKm		{ get { return m_CloudAltitudeThicknessKm.Y; } set { m_CloudAltitudeThicknessKm.Y = value; m_CloudAltitudeThicknessKm.X = m_CloudAltitudeThicknessKm.Y - m_CloudAltitudeThicknessKm.Z; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudThicknessKm		{ get { return m_CloudAltitudeThicknessKm.Z; } set { m_CloudAltitudeThicknessKm.Z = value; m_CloudAltitudeThicknessKm.Y = m_CloudAltitudeThicknessKm.X + m_CloudAltitudeThicknessKm.Z; m_CloudAltitudeThicknessKm.W = 1.0f / m_CloudAltitudeThicknessKm.Z; } }

		[System.ComponentModel.Category( "Clouds Animation" )]
		public float						WindForce				{ get { return m_WindForce; } set { m_WindForce = value; } }
		[System.ComponentModel.Category( "Clouds Animation" )]
		public Vector2						WindDirection			{ get { return m_WindDirection; } set { m_WindDirection = value; m_WindDirection.Normalize(); } }
		[System.ComponentModel.Category( "Clouds Animation" )]
		public float						EvolutionSpeed			{ get { return m_EvolutionSpeed; } set { m_EvolutionSpeed = value; } }
		[System.ComponentModel.Category( "Clouds Animation" )]
		public float						WindAngle
		{
			get { return (float) Math.Atan2( m_WindDirection.Y, m_WindDirection.X ); }
			set { m_WindDirection = new Vector2( (float) Math.Cos( value ), (float) Math.Sin( value ) ); }
		}

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CoverageOffsetBottom	{ get { return m_CoverageOffsets.X; } set { m_CoverageOffsets.X = value; m_CoverageOffsets.W = 0.5f * (m_CoverageOffsets.X + m_CoverageOffsets.Y); } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CoverageOffsetTop		{ get { return m_CoverageOffsets.Y; } set { m_CoverageOffsets.Y = value; m_CoverageOffsets.W = 0.5f * (m_CoverageOffsets.X + m_CoverageOffsets.Y); } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CoverageOffsetPow		{ get { return m_CoverageOffsets.Z; } set { m_CoverageOffsets.Z = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CoverageContrast		{ get { return m_CoverageContrast; } set { m_CoverageContrast = value; } }

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						NoiseSize				{ get { return m_NoiseSize; } set { m_NoiseSize = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						NoiseSizeVerticalFactor	{ get { return m_NoiseSizeVerticalFactor; } set { m_NoiseSizeVerticalFactor = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						VoxelMipFactor			{ get { return m_VoxelMipFactor; } set { m_VoxelMipFactor = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						VoxelMipFactorShadow	{ get { return m_VoxelMipFactorShadow; } set { m_VoxelMipFactorShadow = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						NoiseFrequencyFactor	{ get { return m_NoiseFrequencyFactor; } set { m_NoiseFrequencyFactor = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						NoiseAmplitudeFactor	{ get { return m_NoiseAmplitudeFactor; } set { m_NoiseAmplitudeFactor = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						FarClipAirKm			{ get { return m_FarClipAirKm; } set { m_FarClipAirKm = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						FarClipCloudsKm			{ get { return m_FarClipCloudsKm; } set { m_FarClipCloudsKm = value; } }

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						UniformNoiseDensityAtDistance	{ get { return m_UniformNoiseDensityAtDistance; } set { m_UniformNoiseDensityAtDistance = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						UniformNoiseFadeDistancesStartKm{ get { return m_UniformNoiseFadeDistancesKm.X; } set { m_UniformNoiseFadeDistancesKm.X = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						UniformNoiseFadeDistancesEndKm	{ get { return m_UniformNoiseFadeDistancesKm.Y; } set { m_UniformNoiseFadeDistancesKm.Y = value; } }

		[System.ComponentModel.Category( "Clouds Physics" )]
		[System.ComponentModel.Description( "Gives the cloud's density" )]
		public float						CloudDensity			{ get { return m_Density; } set { m_Density = value; } }
		[System.ComponentModel.Description( "Gives the cloud's albedo" )]
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						CloudAlbedo				{ get { return m_Albedo; } set { m_Albedo = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						ShadowOpacity			{ get { return m_ShadowOpacity; } set { m_ShadowOpacity = value; } }

// 		[System.ComponentModel.Category( "Upscale" )]
// 		public float						CloudOpacityFactor		{ get { return m_CloudOpacityFactor; } set { m_CloudOpacityFactor = value; } }
// 		[System.ComponentModel.Category( "Upscale" )]
// 		public float						SamplingJitterAmplitude	{ get { return m_SamplingJitterAmplitude; } set { m_SamplingJitterAmplitude = value; } }
// 		[System.ComponentModel.Category( "Upscale" )]
// 		public float						JitterNoiseSize			{ get { return m_JitterNoiseSize; } set { m_JitterNoiseSize = value; } }
		[System.ComponentModel.Category( "Upscale" )]
		public bool							ShowRefinement			{ get { return m_bShowRefinement; } set { m_bShowRefinement = value; } }
		[System.ComponentModel.Category( "Upscale" )]
		public float						RefinementZThreshold	{ get { return m_RefinementZThreshold; } set { m_RefinementZThreshold = value; } }

		[System.ComponentModel.Category( "Shadow" )]
		public float						ShadowFarDistanceKm		{ get { return m_ShadowFarDistanceKm; } set { m_ShadowFarDistanceKm = value; } }

		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						DirectionalFactor		{ get { return m_DirectionalFactor; } set { m_DirectionalFactor = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						IsotropicFactor			{ get { return m_IsotropicFactor; } set { m_IsotropicFactor = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						IsotropicScatteringFactorSky		{ get { return m_IsotropicScatteringFactors.X; } set { m_IsotropicScatteringFactors.X = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						IsotropicScatteringFactorTerrain	{ get { return m_IsotropicScatteringFactors.Y; } set { m_IsotropicScatteringFactors.Y = value; } }

		[System.ComponentModel.Category( "Clouds Physics" )]
		public Vector3						NightSkyAmbientColor	{ get { return m_NightSkyAmbientColor; } set { m_NightSkyAmbientColor = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public Vector3						TerrainAlbedo			{ get { return m_TerrainAlbedo; } set { m_TerrainAlbedo = value; } }

		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropyForward		{ get { return m_ScatteringAnisotropyForward; } set { m_ScatteringAnisotropyForward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightForward		{ get { return m_PhaseWeightForward; } set { m_PhaseWeightForward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropyBackward	{ get { return m_ScatteringAnisotropyBackward; } set { m_ScatteringAnisotropyBackward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightBackward		{ get { return m_PhaseWeightBackward; } set { m_PhaseWeightBackward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropySide		{ get { return m_ScatteringAnisotropySide; } set { m_ScatteringAnisotropySide = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightSide			{ get { return m_PhaseWeightSide; } set { m_PhaseWeightSide = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightSide2		{ get { return m_PhaseWeightSide2; } set { m_PhaseWeightSide2 = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropyStrongForward	{ get { return m_ScatteringAnisotropyStrongForward; } set { m_ScatteringAnisotropyStrongForward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightStrongForward	{ get { return m_PhaseWeightStrongForward; } set { m_PhaseWeightStrongForward = value; } }

		[System.ComponentModel.Category( "Combine" )]
		public float						BilateralOffset			{ get { return m_BilateralOffset; } set { m_BilateralOffset = value; } }
		[System.ComponentModel.Category( "Combine" )]
		public float						BilateralDepthThreshold	{ get { return m_BilateralThreshold; } set { m_BilateralThreshold = value; } }


[System.ComponentModel.Category( "DEBUG" )]
public Vector4						DEBUG0					{ get { return m_DEBUG0; } set { m_DEBUG0 = value; } }
[System.ComponentModel.Category( "DEBUG" )]
public Vector4						DEBUG1					{ get { return m_DEBUG1; } set { m_DEBUG1 = value; } }
[System.ComponentModel.Category( "DEBUG" )]
public Vector4						DEBUG2					{ get { return m_DEBUG2; } set { m_DEBUG2 = value; } }


		#endregion

		#region METHODS

		public RenderTechniqueVolumeClouds( RendererSetupBasic _Renderer, IRenderTarget[] _RenderTargets, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;
			m_RenderTargets = _RenderTargets;

			// Create and configure the sky support
			// We must do that BEFORE compiling our material that uses the sky support
			m_SkySupport = ToDispose( new Nuaj.Cirrus.Atmosphere.SkySupport( m_Device, "Cloud Sky Support" ) );

			// Register us as a cloud support provider
			m_Device.DeclareShaderInterface( typeof(ICloudSupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(ICloudSupport), this );
			m_Device.DeclareShaderInterface( typeof(IAmbientSkySH) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IAmbientSkySH), this );

			// Create our main materials
			m_MaterialDeepShadowMap = ToDispose( new Material<VS_Pt4>( m_Device, "DeepShadowMap Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Clouds2/RenderDeepShadowMap.fx" ) ) );
			m_MaterialDownscaleZ = ToDispose( new Material<VS_Pt4>( m_Device, "Downscale ZBuffer Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Clouds2/DownscaleZBuffer.fx" ) ) );
			m_MaterialCloud = ToDispose( new Material<VS_Pt4>( m_Device, "Cloud Render Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Clouds2/CloudRender.fx" ) ) );
			m_MaterialSkyProbe = ToDispose( new Material<VS_Pt4>( m_Device, "SkyProbe Render Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Clouds2/SkyProbeRender.fx" ) ) );

			// Create downscaled render buffer
			int	TargetWidth = (int) Math.Floor( CLOUD_DOWNSCALE_FACTOR * m_RenderTargets[0].Width );
			int	TargetHeight = (int) Math.Floor( CLOUD_DOWNSCALE_FACTOR * m_RenderTargets[0].Height );
			m_VolumeRenderInScattering = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Volume Target InScattering", TargetWidth, TargetHeight, 1 ) );
			m_VolumeRenderExtinction = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Volume Target Extinction", TargetWidth, TargetHeight, 1 ) );

			// Create the deep shadow maps
			m_DeepShadowMaps[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Deep Shadow Map 0", DEEP_SHADOW_MAP_SIZE, DEEP_SHADOW_MAP_SIZE, 1 ) );
			m_DeepShadowMaps[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Deep Shadow Map 1", DEEP_SHADOW_MAP_SIZE, DEEP_SHADOW_MAP_SIZE, 1 ) );

			// Create the downscaled ZBuffer
			int	DownscaledWidth = m_RenderTargets[0].Width / 2;
			int	DownscaledHeight = m_RenderTargets[0].Height / 2;
			m_DownscaledZBuffer = ToDispose( new RenderTarget<PF_R16F>( m_Device, "DownscaledZBuffer", DownscaledWidth, DownscaledHeight, 2 ) );

			// Create the ambient probe
			m_AmbientProbe = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Ambient Sky", 2*AMBIENT_PROBE_SIZE, AMBIENT_PROBE_SIZE, 1 ) );
			m_AmbientProbeShadowMap = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Ambient Sky Shadow Map", 2*AMBIENT_PROBE_SIZE, AMBIENT_PROBE_SIZE, 1 ) );
			m_SHConvolution = ToDispose( BuildSHConvolutionTexture() );

			// Create the post-process quad
			m_Quad = ToDispose( new Nuaj.Helpers.ScreenQuad( m_Device, "PostProcess Quad" ) );
		}

		public override void Dispose()
		{
			base.Dispose();

			if ( m_CloudProfile != null )
				m_CloudProfile.Dispose();
			m_CloudProfile = null;
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Clouds", "Start" );

			//////////////////////////////////////////////////////////////////////////
			// Update cloud position
			float		DeltaTime = m_Time - m_PreviousTime;

			Vector2		Wind = m_WindForce * m_WindDirection;
			Vector2		CloudPositionMain = new Vector2( m_CloudPosition.X, m_CloudPosition.Y );
			Vector2		CloudPositionOctave = new Vector2( m_CloudPosition.Z, m_CloudPosition.W );
						CloudPositionMain += m_NoiseSize * Wind * DeltaTime;
						CloudPositionOctave += m_EvolutionSpeed * m_NoiseSize * Wind * DeltaTime;
			m_CloudPosition = new Vector4( CloudPositionMain.X, CloudPositionMain.Y, CloudPositionOctave.X, CloudPositionOctave.Y );

			//////////////////////////////////////////////////////////////////////////
			m_Device.AddProfileTask( this, "Clouds", "Compute ShadowMap Data" );
			ComputeShadowProjection();

			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

			//////////////////////////////////////////////////////////////////////////
			// Downscale ZBuffer twice
			using ( m_MaterialDownscaleZ.UseLock() )
			{
				// Render first downscale (/2)
				m_Device.SetRenderTarget( m_DownscaledZBuffer.GetSingleRenderTargetView( 0, 0 ) );
				m_Device.SetViewport( 0, 0, m_DownscaledZBuffer.Width, m_DownscaledZBuffer.Height, 0.0f, 1.0f );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DownscaleFirstPass" );
				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

				// Render second downscale (/4)
				m_Device.SetRenderTarget( m_DownscaledZBuffer.GetSingleRenderTargetView( 1, 0 ) );
				m_Device.SetViewport( 0, 0, m_DownscaledZBuffer.Width / 2, m_DownscaledZBuffer.Height / 2, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "_SourceBuffer" ).AsResource.SetResource( m_DownscaledZBuffer.GetSingleTextureView( 0, 0 ) );
				CurrentMaterial.GetVariableByName( "_BufferInvSize" ).AsVector.Set( new Vector2( 1.0f / m_DownscaledZBuffer.Width, 1.0f / m_DownscaledZBuffer.Height ) );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Downscale" );
				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}

			//////////////////////////////////////////////////////////////////////////
			// DSM Rendering
			using ( m_MaterialDeepShadowMap.UseLock() )
			{
				//////////////////////////////////////////////////////////////////////////
				// Set global variables
				CurrentMaterial.GetVariableByName( "_NoiseOffsets" ).AsVector.Set( m_CoverageOffsets );
				CurrentMaterial.GetVariableByName( "_NoiseConstrast" ).AsScalar.Set( m_CoverageContrast );
				CurrentMaterial.GetVariableByName( "_NoiseSize" ).AsVector.Set( m_NoiseSize * new Vector3( 1.0f, m_NoiseSizeVerticalFactor, 1.0f ) );

				CurrentMaterial.GetVariableByName( "_NoiseFrequencyFactor" ).AsVector.Set( new Vector2( m_NoiseFrequencyFactor, -(float) (Math.Log( m_NoiseFrequencyFactor ) / Math.Log( 2.0 )) ) );
				CurrentMaterial.GetVariableByName( "_NoiseAmplitudeFactor" ).AsVector.Set( new Vector2( m_NoiseAmplitudeFactor, 1.0f / (m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor)))) ) );
				CurrentMaterial.GetVariableByName( "_CloudPosition" ).AsVector.Set( m_CloudPosition );
				CurrentMaterial.GetVariableByName( "_CloudProfileTexture" ).AsResource.SetResource( m_CloudProfile );

				CurrentMaterial.GetVariableByName( "_UniformNoiseDensity" ).AsScalar.Set( m_UniformNoiseDensityAtDistance );
				CurrentMaterial.GetVariableByName( "_UniformNoiseFadeDistancesKm" ).AsVector.Set( m_UniformNoiseFadeDistancesKm );

				//////////////////////////////////////////////////////////////////////////
				// Render the deep shadow map
				m_Device.AddProfileTask( this, "Clouds", "Render DSM" );
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderDeepShadowMap" );

				m_Device.SetMultipleRenderTargets( m_DeepShadowMaps );
				m_Device.SetViewport( 0, 0, m_DeepShadowMaps[0].Width, m_DeepShadowMaps[0].Height, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "_BufferInvSize" ).AsVector.Set( m_DeepShadowMaps[0].InvSize2 );

//				CurrentMaterial.GetVariableByName( "_InvVoxelSizeKm" ).AsScalar.Set( m_NoiseSize * m_CloudAltitudeThicknessKm.W / m_VoxelMipFactorShadow );
				CurrentMaterial.GetVariableByName( "_InvVoxelSizeKm" ).AsScalar.Set( m_NoiseSize / m_VoxelMipFactorShadow );

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();
			}

			//////////////////////////////////////////////////////////////////////////
			// Main rendering
			using ( m_MaterialCloud.UseLock() )
			{
				//////////////////////////////////////////////////////////////////////////
				// Set global variables
				CurrentMaterial.GetVariableByName( "_NoiseOffsets" ).AsVector.Set( m_CoverageOffsets );
				CurrentMaterial.GetVariableByName( "_NoiseConstrast" ).AsScalar.Set( m_CoverageContrast );
				CurrentMaterial.GetVariableByName( "_NoiseSize" ).AsVector.Set( m_NoiseSize * new Vector3( 1.0f, m_NoiseSizeVerticalFactor, 1.0f ) );

				CurrentMaterial.GetVariableByName( "_NoiseFrequencyFactor" ).AsVector.Set( new Vector2( m_NoiseFrequencyFactor, -(float) (Math.Log( m_NoiseFrequencyFactor ) / Math.Log( 2.0 )) ) );
				CurrentMaterial.GetVariableByName( "_NoiseAmplitudeFactor" ).AsVector.Set( new Vector2( m_NoiseAmplitudeFactor, 1.0f / (m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor)))) ) );
				CurrentMaterial.GetVariableByName( "_CloudPosition" ).AsVector.Set( m_CloudPosition );
				CurrentMaterial.GetVariableByName( "_CloudProfileTexture" ).AsResource.SetResource( m_CloudProfile );

				CurrentMaterial.GetVariableByName( "_UniformNoiseDensity" ).AsScalar.Set( m_UniformNoiseDensityAtDistance );
				CurrentMaterial.GetVariableByName( "_UniformNoiseFadeDistancesKm" ).AsVector.Set( m_UniformNoiseFadeDistancesKm );

				CurrentMaterial.GetVariableByName( "_DownscaledZBuffer" ).AsResource.SetResource( m_DownscaledZBuffer );

				//////////////////////////////////////////////////////////////////////////
				// Render the low resolution clouds as 2 float3 : Extinction & In-Scattering
				m_Device.AddProfileTask( this, "Clouds", "Render Clouds" );

				m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_VolumeRenderInScattering, m_VolumeRenderExtinction } );
				m_Device.SetViewport( 0, 0, m_VolumeRenderInScattering.Width, m_VolumeRenderInScattering.Height, 0.0f, 1.0f );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Render" );

				CurrentMaterial.GetVariableByName( "_BufferInvSize" ).AsVector.Set( m_VolumeRenderInScattering.InvSize2 );

//				CurrentMaterial.GetVariableByName( "_InvVoxelSizeKm" ).AsScalar.Set( m_NoiseSize * m_CloudAltitudeThicknessKm.W / m_VoxelMipFactor );
				CurrentMaterial.GetVariableByName( "_InvVoxelSizeKm" ).AsScalar.Set( m_NoiseSize / m_VoxelMipFactor );

				CurrentMaterial.GetVariableByName( "_FarClipAirKm" ).AsScalar.Set( m_FarClipAirKm );
				CurrentMaterial.GetVariableByName( "_FarClipCloudKm" ).AsScalar.Set( m_FarClipCloudsKm );

				CurrentMaterial.GetVariableByName( "_DirectionalFactor" ).AsScalar.Set( m_DirectionalFactor );
				CurrentMaterial.GetVariableByName( "_IsotropicFactor" ).AsScalar.Set( m_IsotropicFactor );
				CurrentMaterial.GetVariableByName( "_IsotropicScatteringFactors" ).AsVector.Set( m_IsotropicScatteringFactors );

				CurrentMaterial.GetVariableByName( "_AmbientNightSky" ).AsVector.Set( m_NightSkyAmbientColor );
				CurrentMaterial.GetVariableByName( "_TerrainAlbedo" ).AsVector.Set( m_TerrainAlbedo );
				CurrentMaterial.GetVariableByName( "_SunColorFromGround" ).AsVector.Set( m_SkySupport.SunColor );

				CurrentMaterial.GetVariableByName( "_CloudOpacityFactor" ).AsScalar.Set( m_CloudOpacityFactor );
				CurrentMaterial.GetVariableByName( "_SamplingJitterAmplitude" ).AsScalar.Set( m_SamplingJitterAmplitude );
				CurrentMaterial.GetVariableByName( "_JitterNoiseSize" ).AsScalar.Set( m_JitterNoiseSize );

				CurrentMaterial.GetVariableByName( "_ScatteringAnisotropyStrongForward" ).AsScalar.Set( m_ScatteringAnisotropyStrongForward );
				CurrentMaterial.GetVariableByName( "_PhaseWeightStrongForward" ).AsScalar.Set( m_PhaseWeightStrongForward );
				CurrentMaterial.GetVariableByName( "_ScatteringAnisotropyForward" ).AsScalar.Set( m_ScatteringAnisotropyForward );
				CurrentMaterial.GetVariableByName( "_PhaseWeightForward" ).AsScalar.Set( m_PhaseWeightForward );
				CurrentMaterial.GetVariableByName( "_ScatteringAnisotropyBackward" ).AsScalar.Set( m_ScatteringAnisotropyBackward );
				CurrentMaterial.GetVariableByName( "_PhaseWeightBackward" ).AsScalar.Set( m_PhaseWeightBackward );
				CurrentMaterial.GetVariableByName( "_ScatteringAnisotropySide" ).AsScalar.Set( m_ScatteringAnisotropySide );
				CurrentMaterial.GetVariableByName( "_PhaseWeightSide" ).AsScalar.Set( m_PhaseWeightSide );
				CurrentMaterial.GetVariableByName( "_PhaseWeightSide2" ).AsScalar.Set( m_PhaseWeightSide2 );

				// Deep shadow map
				CurrentMaterial.GetVariableByName( "_DeepShadowMap0" ).AsResource.SetResource( m_DeepShadowMaps[0] );
				CurrentMaterial.GetVariableByName( "_DeepShadowMap1" ).AsResource.SetResource( m_DeepShadowMaps[1] );

				// Lightning
// 				CurrentMaterial.GetVariableByName( "_LightningPosition" ).AsVector.Set( m_LightningPosition );
// 				CurrentMaterial.GetVariableByName( "_LightningIntensity" ).AsScalar.Set( m_LightningIntensity );



CurrentMaterial.GetVariableByName( "_DEBUG0" ).AsVector.Set( m_DEBUG0 );
CurrentMaterial.GetVariableByName( "_DEBUG1" ).AsVector.Set( m_DEBUG1 );
CurrentMaterial.GetVariableByName( "_DEBUG2" ).AsVector.Set( m_DEBUG2 );



				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// Upscale clouds & combine
				m_Device.AddProfileTask( this, "Clouds", "Combine" );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Combine" );
				m_Device.SetRenderTarget( m_RenderTargets[1], null );
				m_Device.SetViewport( 0, 0, m_RenderTargets[1].Width, m_RenderTargets[1].Height, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "_BufferInvSize" ).AsVector.Set( m_RenderTargets[1].InvSize2 );
				CurrentMaterial.GetVariableByName( "_SourceBuffer" ).AsResource.SetResource( m_RenderTargets[0] );
				CurrentMaterial.GetVariableByName( "_VolumeBufferInvSize" ).AsVector.Set( new Vector3( m_BilateralOffset / m_VolumeRenderInScattering.Width, m_BilateralOffset / m_VolumeRenderInScattering.Height, 0.0f ) );
				CurrentMaterial.GetVariableByName( "_VolumeTextureInScattering" ).AsResource.SetResource( m_VolumeRenderInScattering );
				CurrentMaterial.GetVariableByName( "_VolumeTextureExtinction" ).AsResource.SetResource( m_VolumeRenderExtinction );
				CurrentMaterial.GetVariableByName( "_BilateralThreshold" ).AsScalar.Set( m_BilateralThreshold );
				CurrentMaterial.GetVariableByName( "_RefinementZThreshold" ).AsVector.Set( new Vector2( m_RefinementZThreshold, m_bShowRefinement ? 1 : 0 ) );

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();
			}

			m_Device.AddProfileTask( this, "Clouds", "End" );
		}

		public delegate float	CloudProfileEventHandler( float Y );
		public void		BuildCloudProfile( CloudProfileEventHandler _Profile )
		{
			if ( m_CloudProfile != null )
				m_CloudProfile.Dispose();

			// Create the 1D cloud profile
			using ( Image<PF_R16F> I = new Image<PF_R16F>( m_Device, "Cloud Profile Image", 256, 1, ( int _X, int _Y, ref Vector4 _Color ) =>
				{
					_Color.X = _Profile( _X / 255.0f );
				}, 1 ) )
				m_CloudProfile = new Texture2D<PF_R16F>( m_Device, "Cloud Profile", I );
		}

		#region Shadow Map Computation

//		protected Matrix	m_Light2World;
// 		protected Vector4	m_ShadowRectangle;
// 		protected Vector4	m_ShadowInvRectangle;
// 		protected Vector3	m_ShadowVectorKm;
// 		protected Vector2	m_ShadowBufferInvSize;
// 		protected Vector2	m_ShadowDistanceKm;
// 
// 		protected void	ComputeShadowProjection_OLD()
// 		{
// 			// Compute camera frustum's AABB
// 			BoundingBox	CameraAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
// 
// 			Matrix	Camera2World = m_Renderer.Camera.Camera2World;
// 			float	CameraFOV = m_Renderer.Camera.PerspectiveFOV;
// 			float	CameraAspectRatio = m_Renderer.Camera.AspectRatio;
// 			float	CameraNear = m_Renderer.Camera.Near;
// 			float	CameraFar = m_SkySupport.Kilometer2WorldUnit * m_ShadowFarDistanceKm;
// 
// 			float	TanHalfFOV = (float) Math.Tan( 0.5 * CameraFOV );
// 			for ( int Y=0; Y < 2; Y++ )
// 				for ( int X=0; X < 2; X++ )
// 				{
// 					Vector3	View = new Vector3( CameraAspectRatio * TanHalfFOV * (2.0f * X - 1.0f), TanHalfFOV * (2.0f * Y - 1.0f), 1.0f );
// 					View.Normalize();
// 
// 					Vector3	Corner = Vector3.TransformCoordinate( CameraNear * View, Camera2World );
// 					CameraAABB.Minimum = Vector3.Min( CameraAABB.Minimum, Corner );
// 					CameraAABB.Maximum = Vector3.Max( CameraAABB.Maximum, Corner );
// 
// 					Corner = Vector3.TransformCoordinate( CameraFar * View, Camera2World );
// 					CameraAABB.Minimum = Vector3.Min( CameraAABB.Minimum, Corner );
// 					CameraAABB.Maximum = Vector3.Max( CameraAABB.Maximum, Corner );
// 				}
// 
// 			// Clamp object's AABB (i.e. terrain) into camera's AABB
// // 			BoundingBox	WorldAABB = new BoundingBox(
// // 				Vector3.Max( CameraAABB.Minimum, m_TerrainAABB.Minimum ),
// // 				Vector3.Min( CameraAABB.Maximum, m_TerrainAABB.Maximum ) );
// 
// 			BoundingBox	WorldAABB = CameraAABB;
// 
// 
// 			// Compute LIGHT2WORLD matrix
// 			Vector3	LightDirection = m_SkySupport.SunDirection;
// // 			Vector3	Right = LightDirection.Y < 0.99f ?
// // 				Vector3.Cross( Vector3.UnitY, LightDirection ) :
// // 				Vector3.Cross( Vector3.UnitX, LightDirection );
// // 			Right.Normalize();
// // 			Vector3	Up = Vector3.Cross( LightDirection, Right );
// // 
// // 			m_Light2World = Matrix.Identity;
// // 			m_Light2World.Row1 = new Vector4( Right, 0.0f );
// // 			m_Light2World.Row2 = new Vector4( Up, 0.0f );
// // 			m_Light2World.Row3 = new Vector4( -LightDirection, 0.0f );
// // 			m_Light2World.Row4 = Vector4.UnitW;
// 
// 			// Project world AABB to top cloud plane
// 			// The idea is to determine which square area on the top cloud plane will encompass
// 			//	the objects visible to the camera and to render only that particular area into
// 			//	the shadow map :
// 			//
// 			//   Min                      Max
// 			//  ==*========================*================= Top Cloud Plane
// 			//     \                        \
// 			//      \                        \
// 			//       \                        \
// 			//        \                        \
// 			//         \                        \
// 			//          \                        \
// 			//           \     ____________________
// 			//            \    |                  |
// 			//             \   |                  |
// 			//              \  |     A A B B      |
// 			//               \ |                  |
// 			//                \|                  |
// 			//                 --------------------
// 			//
// 			float	MinX = +float.MaxValue, MinZ = +float.MaxValue;
// 			float	MaxX = -float.MaxValue, MaxZ = -float.MaxValue;
// 			foreach ( Vector3 Corner in WorldAABB.GetCorners() )
// 			{
// 				// Convert into kilometers
// 				Vector3	CornerKm = m_SkySupport.WorldUnit2Kilometer * Corner;
// 				float	HitDistanceKm = (m_CloudAltitudeThicknessKm.Y - CornerKm.Y) / LightDirection.Y;
// 				Vector3	ProjectedCornerKm = CornerKm + HitDistanceKm * LightDirection;
// 
// 				MinX = Math.Min( MinX, ProjectedCornerKm.X );
// 				MaxX = Math.Max( MaxX, ProjectedCornerKm.X );
// 				MinZ = Math.Min( MinZ, ProjectedCornerKm.Z );
// 				MaxZ = Math.Max( MaxZ, ProjectedCornerKm.Z );
// 			}
// 
// 			m_ShadowRectangle = new Vector4( MinX, MinZ, MaxX-MinX, MaxZ-MinZ );
// 			m_ShadowInvRectangle = new Vector4( -MinX, -MinZ, 1.0f / (MaxX-MinX), 1.0f / (MaxZ-MinZ) );
// 
// 			// Compute the shadow vector that will start from top cloud plane and arrive at bottom cloud plane in 8 steps
// 			// (because the deep shadow map encodes 8 density samples)
// 			m_ShadowDistanceKm.X = m_CloudAltitudeThicknessKm.Z / LightDirection.Y;
// //			m_ShadowDistanceKm.X = Math.Min( m_ShadowDistanceKm.X, 100.0f );								// No more than that amount
// 			m_ShadowDistanceKm.X = Math.Min( m_ShadowDistanceKm.X, 3.0f * m_CloudAltitudeThicknessKm.Z );	// No more than that amount
// 
// #if DEEP_SHADOW_MAP_HI_RES
// 			m_ShadowVectorKm = -LightDirection * 0.11111111111111f * m_ShadowDistanceKm.X;	// Divide into 9 equal steps as we start at 0.5 a step and end at 16.5 steps so we're always within the cloud layer
// 			m_ShadowDistanceKm.Y = 9.0f / m_ShadowDistanceKm.X;
// #else
// 			m_ShadowVectorKm = -LightDirection * 0.2f * m_ShadowDistanceKm.X;				// Divide into 5 equal steps as we start at 0.5 a step and end at 4.5 steps so we're always within the cloud layer
// 			m_ShadowDistanceKm.Y = 5.0f / m_ShadowDistanceKm.X;
// #endif
// 		}

		// Shadow data
		internal Vector3	m_ShadowPlaneCenterKm = Vector3.Zero;
		internal Vector3	m_ShadowPlaneX = Vector3.Zero;
		internal Vector3	m_ShadowPlaneY = Vector3.Zero;
		internal Vector3	m_ShadowPlaneNormal = Vector3.Zero;
		internal Vector2	m_ShadowPlaneOffset = Vector2.Zero;
		internal Vector2	m_ShadowPlaneClippingY = Vector2.Zero;

		// The 4 points of the shadow quad
		internal Vector2[]	m_ShadowQuadKm = new Vector2[4];

		// Vectors for (x,y) => (u,v) projection
		internal Vector2	m_NU0, m_NU1;
		internal Vector2	m_NV0, m_NV1;

		// Vectors for (u,v) => (x,y) projection
		internal Vector3	m_ABC, m_DEF;
		internal Vector3	m_GHI, m_JKL;

		/// <summary>
		/// Computes the variables needed for the shadow map projection
		/// </summary>
		protected void	ComputeShadowProjection()
		{
			// The idea is this:
			//
			//        ..           /
			//  top       ..      / Light Direction
			//  cloud ------ ..  /
			//               ---x..
			//                     --..
			//        -------        --  ..
			//        ///////-----      -    .. Tangent plane to the top cloud sphere
			//        /////////////--     -
			//        //Earth/////////-
			//
			// 1) We compute the tangent plane to the top cloud sphere by projecting the Earth's center to the cloud sphere's surface following the Sun's direction.
			// 2) We project the camera frustum onto that plane
			// 3) We compute the bounding quadrilateral to that frustum
			// 4) We compute the data necessary to transform a world position into a shadow map position, and the reverse
			//

			//////////////////////////////////////////////////////////////////////////
			// Compute shadow plane tangent space
			Vector3	PlanetCenterKm = new Vector3( 0.0f, -PLANET_RADIUS_KM, 0.0f );
			m_ShadowPlaneNormal = SunDirection;
			m_ShadowPlaneCenterKm = PlanetCenterKm + (PLANET_RADIUS_KM + m_CloudAltitudeThicknessKm.Y) * m_ShadowPlaneNormal;	// Point tangent to the top cloud plane
// 			m_ShadowPlaneX = Vector3.Normalize( Vector3.Cross( m_Camera.At, m_ShadowPlaneNormal ) );
// 			m_ShadowPlaneY = Vector3.Cross( m_ShadowPlaneNormal, m_ShadowPlaneX );
			m_ShadowPlaneX = Vector3.Normalize( Vector3.Cross( Vector3.UnitZ, m_ShadowPlaneNormal ) );
			m_ShadowPlaneY = Vector3.Cross( m_ShadowPlaneNormal, m_ShadowPlaneX );

			//////////////////////////////////////////////////////////////////////////
			// Build camera frustum
			float	TanFovV = (float) Math.Tan( 0.5f * m_Camera.PerspectiveFOV );
			float	TanFovH = m_Camera.AspectRatio * TanFovV;

			Vector3		CameraPositionKm = m_SkySupport.WorldUnit2Kilometer * m_Camera.Position;
			Vector3[]	CameraFrustumKm = new Vector3[4]
			{
				m_ShadowFarDistanceKm * new Vector3( -TanFovH, -TanFovV, 1.0f ),
				m_ShadowFarDistanceKm * new Vector3( +TanFovH, -TanFovV, 1.0f ),
				m_ShadowFarDistanceKm * new Vector3( +TanFovH, +TanFovV, 1.0f ),
				m_ShadowFarDistanceKm * new Vector3( -TanFovH, +TanFovV, 1.0f ),
			};

			// Transform into WORLD space
			for ( int i=0; i < 4; i++ )
				CameraFrustumKm[i] = Vector3.TransformNormal( CameraFrustumKm[i], m_Camera.Camera2World );

			// Clip frustum with Earth's horizon
//			CameraFrustumKm = ClipFrustum( CameraPositionKm, CameraFrustumKm );

			// Transform frustum vectors into points
			for ( int i=0; i < 4; i++ )
				CameraFrustumKm[i] += CameraPositionKm;


			//////////////////////////////////////////////////////////////////////////
			// Compute center offset.
			// The "center" of the shadow plane is the intersection of the ray starting from the camera and intersecting the shadow plane in the Sun's direction
			float		Distance2PlaneKm;
			m_ShadowPlaneClippingY = new Vector2( -float.MaxValue, +float.MaxValue );
			m_ShadowPlaneOffset = World2ShadowQuad( CameraPositionKm, out Distance2PlaneKm );
			m_ShadowPlaneCenterKm += m_ShadowPlaneOffset.X * m_ShadowPlaneX + m_ShadowPlaneOffset.Y * m_ShadowPlaneY;


			//////////////////////////////////////////////////////////////////////////
			// Compute vertical clipping
			Vector3	ProjectionMin = Project2ShadowPlane( new Vector3( 0, 0, 0 ), out Distance2PlaneKm );
			Vector3	ProjectionMax = Project2ShadowPlane( new Vector3( 0, m_CloudAltitudeThicknessKm.Y, 0 ), out Distance2PlaneKm );
			m_ShadowPlaneClippingY = new Vector2( ProjectionMin.Y, ProjectionMax.Y );


			//////////////////////////////////////////////////////////////////////////
			// Project frustum to shadow plane
			Vector2		CameraProjKm = World2ShadowQuad( CameraPositionKm, out Distance2PlaneKm );
			Vector2[]	FrustumProjKm = new Vector2[4];
			for ( int i=0; i < 4; i++ )
				FrustumProjKm[i] = World2ShadowQuad( CameraFrustumKm[i], out Distance2PlaneKm );


			//////////////////////////////////////////////////////////////////////////
			// Compute convex hull
			int[]		ConvexHullIndices = null;
			Vector2[]	ConvexHullKm = ComputeConvexHull( new Vector2[] { CameraProjKm, FrustumProjKm[0], FrustumProjKm[1], FrustumProjKm[2], FrustumProjKm[3] }, out ConvexHullIndices );


// 			//////////////////////////////////////////////////////////////////////////
// 			// Compute bounding quadrilateral
// 			//
// 			// At this point, our convex hull can either have 3, 4 or 5 vertices.
// 			// Since we always need a quadrilateral, the only case we need to deal with is the 5 vertices case
// 			//	that we need to reduce to 4 vertices.
// 			//
// 			switch ( ConvexHullKm.Length )
// 			{
// 				case 3:
// 					{	// The case of the triangle leads to numerical instabilities
// 						// We need to create an additional vertex
// 						Vector2	Direction = ConvexHullKm[2] - ConvexHullKm[0];
// 						Vector2	Offset = Direction * 0.01f;	// 1% of the opposite edge's length
// 						m_ShadowQuadKm = new Vector2[4]
// 						{
// 							ConvexHullKm[0],
// 							ConvexHullKm[1],
// 							ConvexHullKm[1] + Offset,
// 							ConvexHullKm[2],
// 						};
// 					}
// 					break;
// 
// 				case 4:
// 					m_ShadowQuadKm = ConvexHullKm;	// Easy !
// 					break;
// 
// 				case 5:
// 					m_ShadowQuadKm = ReduceConvexHull( ConvexHullKm, ConvexHullIndices, 0 );
// 					break;
// 			}


			Vector2	Min = +float.MaxValue * Vector2.One;
			Vector2	Max = -float.MaxValue * Vector2.One;
			Min = Vector2.Min( Min, CameraProjKm );	Max = Vector2.Max( Max, CameraProjKm );
			Min = Vector2.Min( Min, FrustumProjKm[0] );	Max = Vector2.Max( Max, FrustumProjKm[0] );
			Min = Vector2.Min( Min, FrustumProjKm[1] );	Max = Vector2.Max( Max, FrustumProjKm[1] );
			Min = Vector2.Min( Min, FrustumProjKm[2] );	Max = Vector2.Max( Max, FrustumProjKm[2] );
			Min = Vector2.Min( Min, FrustumProjKm[3] );	Max = Vector2.Max( Max, FrustumProjKm[3] );

			m_ShadowQuadKm = new Vector2[]
			{
				new Vector2( Min.X, Min.Y ),
				new Vector2( Max.X, Min.Y ),
				new Vector2( Max.X, Max.Y ),
				new Vector2( Min.X, Max.Y ),
			};


			//////////////////////////////////////////////////////////////////////////
			// Compute the transform matrices converting between UV <=> Shadow Quadrilateral Space
			//
			// The idea is to parametrize the shadow quadrilateral with a simple set of (UV) parameters
			// We need to find a unique (UV) for each P(x,y), and the inverse to map a (UV) back into a P(x,y) inside the quad
			//
			// We know the 4 positions of the quad and P(x,y) to begin with.
			// We compute the 4 vectors normal to each edge of the quadrilateral.
			//
			// The trick is to compute the distance from P to each edge of the quad:
			//
			//   |---> N
			//   |
			//   +----d----P
			//   |       .
			//   |     .
			//   |   .
			//   | .
			//   Q
			//
			//  Q = A vertex of the quad
			//	P = Our point
			//	N = normal to the edge
			//
			//	d = (P-Q).N = Distance to the edge
			//
			// Doing this for each edge, we can choose the following parametrization:
			//
			//	u = d0 / (d0+d1)
			//	v = d2 / (d2+d3)
			//
			// Assuming the simple example of a square, d0 & d1 are the distances to the left and right edges of the square respectively
			//	and d2 & d3 are the distances to the bottom and top edges of the square respectively.
			//
			//                      v
			// Q3 .----------. Q2  -+-
			//    |  :       |    v | d3
			//    |..P.......|----+-+-
			//    |  :       |    | ^
			//    |  :       |    |
			//    |  :       |    | d2
			//    |  :       |    |
			// Q0 .----------. Q1-+---
			//    |  |       |    ^
			//   >+--+<      | 
			//     d0|       | 
			//      >+-------+<
			//           d1
			//
			//
			// It's easy to see that (u,v) thus correctly define our entire square, or any given convex quadrilateral for that matter.
			// ---------------------------------------------------------
			// 
			// Now that we have (u,v), it's a bit trickier to retrieve back P(x,y) from these.
			//
			// We know that :
			//	u = d0 / (d0 + d1)
			//	v = d2 / (d2 + d3)
			//
			// Or:
			//	u = [(P-Q0).N0] / [(P-Q0).N0 + (P-Q2).N2]
			//	v = [(P-Q0).N1] / [(P-Q0).N1 + (P-Q2).N3]
			//
			// Rewriting in terms of x and y :
			//
			//	u = [x.N0x + y.N0y - Q0.N0] / [x.(N0x+N2x) + y.(N0y+N2y) - (Q0.N0 + Q2.N2)]
			//	v = [x.N1x + y.N1y - Q0.N1] / [x.(N1x+N3x) + y.(N1y+N3y) - (Q0.N1 + Q2.N3)]
			//
			// Or in a general form:
			//
			//	u = (A.x + B.y + C) / (D.x + E.y + F)
			//	v = (G.x + H.y + I) / (J.x + K.y + L)
			//
			// I suppose you get it so I'll let you do the maths to invert these equations from here... 
			//
			Vector2	p00 = m_ShadowQuadKm[0];
			Vector2	p10 = m_ShadowQuadKm[1];
			Vector2	p11 = m_ShadowQuadKm[2];
			Vector2	p01 = m_ShadowQuadKm[3];

			// Compute edge normals for U coordinate
			Vector2	NU0 = p00 - p01;
					NU0.Normalize();
					NU0 = new Vector2( -NU0.Y, NU0.X );
			Vector2	NU1 = p11 - p10;
					NU1.Normalize();
					NU1 = new Vector2( -NU1.Y, NU1.X );

			// Compute edge normals for V coordinates
			Vector2	NV0 = p10 - p00;
					NV0.Normalize();
					NV0 = new Vector2( -NV0.Y, NV0.X );
			Vector2	NV1 = p01 - p11;
					NV1.Normalize();
					NV1 = new Vector2( -NV1.Y, NV1.X );

			m_NU0 = NU0;
			m_NU1 = NU1;
			m_NV0 = NV0;
			m_NV1 = NV1;

			// Compute data for (u,v) => (x,y) transform
			float	A = m_NU0.X,			B = m_NU0.Y,			C = -Vector2.Dot( p00, m_NU0 );
			float	D = m_NU0.X + m_NU1.X,	E = m_NU0.Y + m_NU1.Y,	F = -Vector2.Dot( p00, m_NU0 ) - Vector2.Dot( p11, m_NU1 );

			float	G = m_NV0.X,			H = m_NV0.Y,			I = -Vector2.Dot( p00, m_NV0 );
			float	J = m_NV0.X + m_NV1.X,	K = m_NV0.Y + m_NV1.Y,	L = -Vector2.Dot( p00, m_NV0 ) - Vector2.Dot( p11, m_NV1 );

			m_ABC = new Vector3( A, B, C );
			m_DEF = new Vector3( D, E, F );
			m_GHI = new Vector3( G, H, I );
			m_JKL = new Vector3( J, K, L );

// DEBUG (easily writeable in HLSL so we can debug the same code)
float	U = 0.0f;
float	V = 0.0f;
float	X = m_Camera.AspectRatio * (float) Math.Tan( 0.5f * m_Camera.PerspectiveFOV ) * (2.0f * U - 1.0f);
float	Z = 40.0f * V;
Vector3	TestPositionKm = CameraPositionKm + Vector3.TransformNormal( new Vector3( Z * X, 0, Z ), m_Camera.Camera2World );

float	TempDistance2PlaneKm;
Vector2	QuadPosition = World2ShadowQuad( TestPositionKm, out TempDistance2PlaneKm );
Vector2	ShadowUV = ShadowQuad2UV( QuadPosition );
Vector2	QuadPositionBack = UV2ShadowQuad( ShadowUV );
// DEBUG

// DEBUG
m_DEBUGCameraPosition = CameraProjKm;
m_DEBUGFrustumPosition = FrustumProjKm;
m_DEBUGConvexHull = ConvexHullKm;
if ( DEBUGEventRefreshShadow != null )
	DEBUGEventRefreshShadow( this, EventArgs.Empty );
// DEBUG
		}

		internal Vector2	m_DEBUGCameraPosition;
		internal Vector2[]	m_DEBUGFrustumPosition;
		internal Vector2[]	m_DEBUGConvexHull;

		internal event EventHandler	DEBUGEventRefreshShadow = null;


		/// <summary>
		/// Computes the convex hull of a set of points using the gift wrapping algorithm
		/// (cf. http://en.wikipedia.org/wiki/Gift_wrapping_algorithm)
		/// </summary>
		/// <param name="_Points"></param>
		/// <param name="_ConvexHullIndices">The list of indices used in the convex hull</param>
		/// <returns>The 2D convex hull</returns>
		protected Vector2[]	ComputeConvexHull( Vector2[] _Points, out int[] _HullIndices )
		{
			int	Count = _Points.Length;

			// Find left most point
			int		LeftMostIndex = -1;
			float	LeftMostPosition = +float.MaxValue;
			for ( int i=0; i < Count; i++ )
				if ( _Points[i].X < LeftMostPosition )
				{
					LeftMostPosition = _Points[i].X;
					LeftMostIndex = i;
				}

			// Start building the convex hull
			_HullIndices = new int[Count];
			List<Vector2>	Hull = new List<Vector2>();
			int		HullIndex = 0, EndPoint;
			int		PointOnHull = LeftMostIndex;
			do 
			{
				_HullIndices[HullIndex++] = PointOnHull;
				Vector2	P0 = _Points[PointOnHull];
				Hull.Add( P0 );

				EndPoint = 0;	// Start from first point
				Vector2	P1 = _Points[EndPoint];
				for ( int i=1; i < Count; i++ )
					if ( EndPoint == PointOnHull || IsLeftOf( _Points[i], P0, P1 ) )
					{	// Found a new better outer point for the hull !
						EndPoint = i;
						P1 = _Points[i];
					}

				PointOnHull = EndPoint;	// Assign our new point

			} while ( EndPoint != _HullIndices[0] );

			return Hull.ToArray();
		}

		/// <summary>
		/// Returns true if P is "to the left" of segment [P0,P1]
		/// </summary>
		/// <param name="P"></param>
		/// <param name="P0"></param>
		/// <param name="P1"></param>
		/// <returns></returns>
		protected bool	IsLeftOf( Vector2 P, Vector2 P0, Vector2 P1 )
		{
			Vector2	D0 = P1 - P0;
			Vector2	D1 = P - P0;
			float	Cross = D0.X * D1.Y - D0.Y * D1.X;
			return Cross > 0.0f;
		}

		/// <summary>
		/// Reduces a 5-vertices convex hull into a 4-vertices quadrilateral by removing one specific vertex (the camera vertex)
		/// The routine is a simplification of the one found in ftp://ftp.cs.unc.edu/pub/techreports/02-024.pdf
		/// </summary>
		/// <param name="_ConvexHull"></param>
		/// <param name="_ConvexHullIndices"></param>
		/// <param name="_VertexToRemove"></param>
		/// <returns></returns>
		protected Vector2[]	ReduceConvexHull( Vector2[] _ConvexHull, int[] _ConvexHullIndices, int _VertexToRemove )
		{
			// We have a 5-vertices convex hull when the camera position stands outside of the convex hull to create a "letter shape":
			//
			//         x <== Camera standing out
			//       -- --
			//     --     --
			//  x--         --x
			//  |             |
			//  |             |
			//  |             |
			//  x-------------x
			//
			// We then use the method described in the paper quoted earlier to reduce one of the 2 edges sharing the camera vertex.
			//
			//  x <== New vertex
			//  |--
			//  |  --
			//  |    --
			//  |      o <== Camera vertex eliminated
			//  |    .  --
			//  |  .      --
			//  o.  \       --x
			//  |    removed  |
			//  |    edge     |
			//  |             |
			//  x-------------x
			//
			int	Count = _ConvexHull.Length;

			// 1] First, we find the occurence of the vertex to remove
			int	PivotIndex = -1;
			for ( int i=0; i < Count; i++ )
				if ( _ConvexHullIndices[i] == _VertexToRemove )
				{
					PivotIndex = i;
					break;
				}

			if ( PivotIndex == -1 )
				throw new Exception( "Vertex #" + _VertexToRemove + " was not found as part of the convex hull ! (how is that possible ?)" );

			// 2] Build the list of concerned vertices in an ordered list:
			// . V0 = 2 vertices BEFORE pivot
			// . V1 = 1 vertex BEFORE pivot
			// . V2 = pivot
			// . V3 = 1 vertex AFTER pivot
			// . V4 = 2 vertices AFTER pivot
			//
			int	EdgeIndex_m2 = (PivotIndex + Count-2) % Count;
			int	EdgeIndex_m1 = (PivotIndex + Count-1) % Count;
			int	EdgeIndex_p1 = (PivotIndex+1) % Count;
			int	EdgeIndex_p2 = (PivotIndex+2) % Count;

			Vector2[]	Vertices = new Vector2[]
			{
				_ConvexHull[EdgeIndex_m2],
				_ConvexHull[EdgeIndex_m1],
				_ConvexHull[PivotIndex],
				_ConvexHull[EdgeIndex_p1],
				_ConvexHull[EdgeIndex_p2],
			};

			// 3] Compute the intersection of the concerned edges and choose the edge removal that adds the minimal area
			float	Area0;
			Vector2	Intersection0 = ComputeIntersection( Vertices[1], Vertices[1] - Vertices[0], Vertices[2], Vertices[2] - Vertices[3], out Area0 );
			float	Area1;
			Vector2	Intersection1 = ComputeIntersection( Vertices[2], Vertices[2] - Vertices[1], Vertices[3], Vertices[3] - Vertices[4], out Area1 );

// 			if ( Area0 < 0.0f && Area1 < 0.0f )
// 				throw new Exception( "No valid edge to remove ! Can't reduce convex hull ! WTH ?" );

			bool	bRemoveEdge0 = false;
			if ( Area0 > 0.0f && Area1 > 0.0f )
				bRemoveEdge0 = Area0 < Area1;	// Choose the minimal area
			else
				bRemoveEdge0 = Area0 > 0.0f;	// Choose the only one with a valid intersection

			// 4] Build final list
			if ( bRemoveEdge0 )
				return new Vector2[]
				{
					Vertices[0],
					Intersection0,	// This intersection replaces vertices 1 and 2
					Vertices[3],
					Vertices[4]
				};
			else
				return new Vector2[]
				{
					Vertices[0],
					Vertices[1],
					Intersection1,	// This intersection replaces vertices 2 and 3
					Vertices[4]
				};
		}

		/// <summary>
		/// Computes the intersection of 2 lines (P0,V0) and (P1,V1) if it exists and computes the
		///  area added by the (P0,P1,Intersection) triangle.
		/// </summary>
		/// <param name="_P0"></param>
		/// <param name="_V0"></param>
		/// <param name="_P1"></param>
		/// <param name="_V1"></param>
		/// <param name="_Area">The additional area, which will be negative if there is no intersection</param>
		/// <returns></returns>
		protected Vector2	ComputeIntersection( Vector2 _P0, Vector2 _V0, Vector2 _P1, Vector2 _V1, out float _Area )
		{
			Vector2	D = _P0 - _P1;
			float	t = -(D.X * _V1.Y - D.Y * _V1.X) / (_V0.X * _V1.Y - _V0.Y * _V1.X);
			Vector2	I = _P0 + t * _V0;

			Vector2	E = I - _P1;
			_Area = 0.5f * (E.X * D.Y - E.Y * D.X);

			return I;
		}

		/// <summary>
		/// Computes the reduced camera frustum when intersecting the Earth's surface
		/// </summary>
		/// <param name="_CameraPositionKm"></param>
		/// <param name="_CameraFrustumKm"></param>
		protected Vector3[]	ClipFrustum( Vector3 _CameraPositionKm, Vector3[] _CameraFrustumKm )
		{
			Vector3	PlanetCenterKm = new Vector3( 0.0f, -PLANET_RADIUS_KM, 0.0f );

			// 1] The idea here is that the camera's frustum is quite reduced by its intersection with the Earth's surface
			// We first determine which camera rays intersect the Earth
			bool[]	Hits = new bool[4];
			float[]	HitDistancesKm = new float[4];
			int		HitsCount = 0;
			for ( int i=0; i < 4; i++ )
			{
				Hits[i] = ComputeSphereEnterIntersection( _CameraPositionKm, _CameraFrustumKm[i], PlanetCenterKm, PLANET_RADIUS_KM, out HitDistancesKm[i] );
				HitsCount += Hits[i] ? 1 : 0;
			}

			if ( HitsCount == 2 || HitsCount == 3 )
			{	// Browse the rays 2 by 2, the current ray and its successor (since the rays are circularly ordered)
				//	* If current & next rays don't hit then store only next ray
				//	* If current ray hits but next ray doesn't, store tangent ray and next ray
				//	* If next ray hits but current ray doesn't, store only tangent ray
				//	* If none of the rays hit, store nothing
				Vector3[]	NewFrustum = new Vector3[4];
				int			CornerIndex = 0;
				for ( int i=0; i < 4; i++ )
				{
					int	Ni = (i+1)&3;
					if ( Hits[i] && !Hits[Ni] )
					{
						NewFrustum[CornerIndex++] = ComputeTangentRay( _CameraPositionKm, _CameraFrustumKm[i], _CameraFrustumKm[Ni], PlanetCenterKm, PLANET_RADIUS_KM );
						NewFrustum[CornerIndex++] = _CameraFrustumKm[Ni];
					}
					else if ( !Hits[i] && Hits[Ni] )
						NewFrustum[CornerIndex++] = ComputeTangentRay( _CameraPositionKm, _CameraFrustumKm[Ni], _CameraFrustumKm[i], PlanetCenterKm, PLANET_RADIUS_KM );
					else if ( !Hits[i] && !Hits[Ni] )
						NewFrustum[CornerIndex++] = _CameraFrustumKm[Ni];
				}

				if ( CornerIndex == 3 )
					NewFrustum[CornerIndex++] = NewFrustum[CornerIndex-1];	// Double the last vertex...

				_CameraFrustumKm = NewFrustum;
			}
			else if ( HitsCount == 4 )
			{	// When all the rays hit the planet (like when viewing down), we need to use the frustum's projection onto the planet
				Vector3[]	NewFrustum = new Vector3[4];
				for ( int i=0; i < 4; i++ )
					NewFrustum[i] = HitDistancesKm[i] * _CameraFrustumKm[i];	// Go to hit position...

				_CameraFrustumKm = NewFrustum;
			}
			else
			{	// No change... Only 1 hit is unlikely, unless the camera is rolling
			}

			// 2] Next, we check for rays intersection with the top cloud plane so we stop tracing shadow further
			float	CloudRadiusKm = PLANET_RADIUS_KM + m_CloudAltitudeThicknessKm.Y;
			float	CameraRadiusKm = (_CameraPositionKm - PlanetCenterKm).Length();
			if ( CameraRadiusKm < CloudRadiusKm )
			{	// Below the clouds : check for exit intersections
				for ( int i=0; i < 4; i++ )
					if ( ComputeSphereExitIntersection( _CameraPositionKm, _CameraFrustumKm[i], PlanetCenterKm, CloudRadiusKm, out HitDistancesKm[i] ) )
						_CameraFrustumKm[i] *= HitDistancesKm[i];	// Shorten the ray
			}
// We don't limit rays to the top cloud plane when seeing clouds from above !
//	=> Shadow goes below the clouds and projects to the ground: we need to test for ground, and we already did earlier
// 			else
// 			{	// Above the clouds : check for entry intersections
// 				for ( int i=0; i < 4; i++ )
// 					if ( ComputeSphereEnterIntersection( _CameraPositionKm, _CameraFrustumKm[i], PlanetCenterKm, CloudRadiusKm, out HitDistancesKm[i] ) )
// 						_CameraFrustumKm[i] *= HitDistancesKm[i];	// Shorten the ray
// 			}

			return _CameraFrustumKm;
		}

		/// <summary>
		/// Computes the ray tangent to the specified sphere, an interpolation of 2 rays : one hits the sphere, the other one doesn't
		/// </summary>
		/// <param name="_CameraPositionKm"></param>
		/// <param name="_RayDown"></param>
		/// <param name="_RayUp"></param>
		/// <param name="_Center"></param>
		/// <param name="_RadiusKm"></param>
		/// <returns></returns>
		protected Vector3	ComputeTangentRay( Vector3 _CameraPositionKm, Vector3 _RayDown, Vector3 _RayUp, Vector3 _CenterKm, float _RadiusKm )
		{
			float	L0 = _RayDown.Length();
			_RayDown /= L0;
			float	L1 = _RayUp.Length();
			_RayUp /= L1;

			Vector3	D = _CameraPositionKm - _CenterKm;
			Vector3	W = _RayUp - _RayDown;

			float	k = Vector3.Dot( D, D ) - _RadiusKm*_RadiusKm;

			float	DV0 = Vector3.Dot( D, _RayDown );
			float	V0V0 = Vector3.Dot( _RayDown, _RayDown );
			float	DW = Vector3.Dot( D, W );
			float	V0W = Vector3.Dot( _RayDown, W );
			float	WW = Vector3.Dot( W, W );

			float	a = DW*DW - k*WW*WW;
			float	b = DV0*DW - k*V0W;
			float	c = DV0*DV0 - k*V0V0;

			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
//				throw new Exception( "Can't find tangent ray ! This means both rays don't intersect the sphere. Check your rays before calling this function !" );
				return _RayDown;

			Delta = (float) Math.Sqrt( Delta );
			a = 1.0f / a;

			float	t0 = (-b - Delta) * a;
			float	t1 = (-b + Delta) * a;

			if ( t0 < 0.0f || t0 > 1.0f )
				t0 = t1;	// This is the other solution...
// 			if ( t0 < 0.0f || t0 > 1.0f )
// 				throw new Exception( "No valid tangent was found !" );

			return (L0 + (L1-L0) * t0) * (_RayDown + t0 * W);
		}

		/// <summary>
		/// Computes the ENTRY intersection of a ray with a sphere
		/// </summary>
		/// <param name="_P"></param>
		/// <param name="_V"></param>
		/// <param name="_Center"></param>
		/// <param name="_RadiusKm"></param>
		/// <param name="_fDistanceKm"></param>
		/// <returns></returns>
		protected bool	ComputeSphereEnterIntersection( Vector3 _P, Vector3 _V, Vector3 _Center, float _RadiusKm, out float _fDistanceKm )
		{
			_fDistanceKm = -1.0f;

			Vector3	D = _P - _Center;
			float	a = Vector3.Dot( _V, _V );
			float	b = Vector3.Dot( _V, D );
			float	c = Vector3.Dot( D, D ) - _RadiusKm*_RadiusKm;
			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
				return false;

			Delta = (float) Math.Sqrt( Delta );

			_fDistanceKm = (-b - Delta) / a;
			return _fDistanceKm >= 0.0f && _fDistanceKm < 1.0f;
		}

		/// <summary>
		/// Computes the EXIT intersection of a ray with a sphere
		/// </summary>
		/// <param name="_P"></param>
		/// <param name="_V"></param>
		/// <param name="_Center"></param>
		/// <param name="_RadiusKm"></param>
		/// <param name="_fDistanceKm"></param>
		/// <returns></returns>
		protected bool	ComputeSphereExitIntersection( Vector3 _P, Vector3 _V, Vector3 _Center, float _RadiusKm, out float _fDistanceKm )
		{
			_fDistanceKm = -1.0f;

			Vector3	D = _P - _Center;
			float	a = Vector3.Dot( _V, _V );
			float	b = Vector3.Dot( _V, D );
			float	c = Vector3.Dot( D, D ) - _RadiusKm*_RadiusKm;
			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
				return false;

			Delta = (float) Math.Sqrt( Delta );

			_fDistanceKm = (-b + Delta) / a;
			return _fDistanceKm >= 0.0f && _fDistanceKm < 1.0f;
		}

		/// <summary>
		/// Projects a world position in kilometers into the shadow plane
		/// </summary>
		/// <param name="_Position"></param>
		/// <returns></returns>
		public Vector3	Project2ShadowPlane( Vector3 _PositionKm, out float _Distance2PlaneKm )
		{
			Vector3	Position2CenterKm = m_ShadowPlaneCenterKm - _PositionKm;
			_Distance2PlaneKm = Vector3.Dot( Position2CenterKm, m_ShadowPlaneNormal );
			return _PositionKm + _Distance2PlaneKm * m_ShadowPlaneNormal;
		}

		/// <summary>
		/// Projects a world position in kilometers into the shadow quad
		/// </summary>
		/// <param name="_Position"></param>
		/// <returns></returns>
		public Vector2	World2ShadowQuad( Vector3 _PositionKm, out float _Distance2PlaneKm )
		{
			Vector3	ProjectedPositionKm = Project2ShadowPlane( _PositionKm, out _Distance2PlaneKm );

			// Vertical clip
			ProjectedPositionKm.Y = Math.Max( m_ShadowPlaneClippingY.X, Math.Min( m_ShadowPlaneClippingY.Y, ProjectedPositionKm.Y ) );

			Vector3	Center2ProjPositionKm = ProjectedPositionKm - m_ShadowPlaneCenterKm;
			return new Vector2( Vector3.Dot( Center2ProjPositionKm, m_ShadowPlaneX ), Vector3.Dot( Center2ProjPositionKm, m_ShadowPlaneY ) );
		}

		/// <summary>
		/// Transforms a shadow quad position into an UV parametric position
		/// </summary>
		/// <param name="P"></param>
		/// <returns></returns>
		public Vector2	ShadowQuad2UV( Vector2 P )
		{
			Vector2	p00 = m_ShadowQuadKm[0];
			Vector2	p11 = m_ShadowQuadKm[2];

			float	dU0 = Vector2.Dot( P - p00, m_NU0 );
			float	dU1 = Vector2.Dot( P - p11, m_NU1 );
			float	U = dU0 / (dU0 + dU1);

			float	dV0 = Vector2.Dot( P - p00, m_NV0 );
			float	dV1 = Vector2.Dot( P - p11, m_NV1 );
			float	V = dV0 / (dV0 + dV1);

			return new Vector2( U, V );
		}

		/// <summary>
		/// Transforms an UV parametric position into a shadow quad position
		/// </summary>
		/// <param name="P"></param>
		/// <returns></returns>
		public Vector2	UV2ShadowQuad( Vector2 P )
		{
			Vector3	U = P.X * m_DEF - m_ABC;
			Vector3	V = P.Y * m_JKL - m_GHI;

			float	Den = V.X*U.Y - V.Y*U.X;
			float	X = V.Y*U.Z - V.Z*U.Y;
			float	Y = V.Z*U.X - V.X*U.Z;

			return new Vector2( X, Y ) / Den;
		}

		protected void	UpdateLightValues()
		{
			m_Light.Direction = m_SkySupport.SunDirection;
			m_Light.Color = new Vector4( m_SkySupport.SunColor, 1.0f );
		}

		#endregion

		#region Ambient Sky Probe Computation

		/// <summary>
		/// Computes the ambient sky at specified position and encodes it into SH
		/// </summary>
		/// <param name="_Position">The sky probe's position in WORLD units</param>
		/// <param name="_Targets1x1">A 1x1 render target array of 3 targets</param>
		/// <param name="_IncludeClouds">True to render clouds in the process</param>
		public void		ComputeAmbientSkySH( Vector3 _Position, IRenderTarget _Targets1x1, bool _IncludeClouds )
		{
			if ( _Targets1x1.Width != 1 || _Targets1x1.Height != 1 )
				throw new NException( this, "Ambient sky render targets must be 1x1 !" );
			if ( _Targets1x1.ArraySize != 3 )
				throw new NException( this, "Ambient sky render targets array must be 3 targets long !" );

			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

			if ( _IncludeClouds )
			{
				using ( m_MaterialDeepShadowMap.UseLock() )
				{
					//////////////////////////////////////////////////////////////////////////
					// Set global variables
					CurrentMaterial.GetVariableByName( "_NoiseOffsets" ).AsVector.Set( m_CoverageOffsets );
					CurrentMaterial.GetVariableByName( "_NoiseConstrast" ).AsScalar.Set( m_CoverageContrast );
					CurrentMaterial.GetVariableByName( "_NoiseSize" ).AsVector.Set( m_NoiseSize * new Vector3( 1.0f, m_NoiseSizeVerticalFactor, 1.0f ) );

					CurrentMaterial.GetVariableByName( "_NoiseFrequencyFactor" ).AsVector.Set( new Vector2( m_NoiseFrequencyFactor, -(float) (Math.Log( m_NoiseFrequencyFactor ) / Math.Log( 2.0 )) ) );
					CurrentMaterial.GetVariableByName( "_NoiseAmplitudeFactor" ).AsVector.Set( new Vector2( m_NoiseAmplitudeFactor, 1.0f / (m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor)))) ) );
					CurrentMaterial.GetVariableByName( "_CloudPosition" ).AsVector.Set( m_CloudPosition );
					CurrentMaterial.GetVariableByName( "_CloudProfileTexture" ).AsResource.SetResource( m_CloudProfile );

					//////////////////////////////////////////////////////////////////////////
					// Render the low resolution sky probe
					m_Device.SetRenderTarget( m_AmbientProbeShadowMap );
					m_Device.SetViewport( 0, 0, m_AmbientProbeShadowMap.Width, m_AmbientProbeShadowMap.Height, 0.0f, 1.0f );

					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderDeepShadowMapProbe" );

					CurrentMaterial.GetVariableByName( "_BufferInvSize" ).AsVector.Set( m_AmbientProbeShadowMap.InvSize2 );
					CurrentMaterial.GetVariableByName( "_SkyProbePositionKm" ).AsVector.Set( m_SkySupport.WorldUnit2Kilometer * _Position );
					CurrentMaterial.GetVariableByName( "_SkyProbeAngles" ).AsVector.Set( new Vector4( AMBIENT_PROBE_PHI_START, AMBIENT_PROBE_THETA_START, AMBIENT_PROBE_PHI_END - AMBIENT_PROBE_PHI_START, AMBIENT_PROBE_THETA_END - AMBIENT_PROBE_THETA_START ) );

					CurrentMaterial.ApplyPass( 0 );
					m_Quad.Render();
				}
			}

			using ( m_MaterialSkyProbe.UseLock() )
			{
				//////////////////////////////////////////////////////////////////////////
				// Set global variables
				CurrentMaterial.GetVariableByName( "_NoiseOffsets" ).AsVector.Set( m_CoverageOffsets );
				CurrentMaterial.GetVariableByName( "_NoiseConstrast" ).AsScalar.Set( m_CoverageContrast );
				CurrentMaterial.GetVariableByName( "_NoiseSize" ).AsVector.Set( m_NoiseSize * new Vector3( 1.0f, m_NoiseSizeVerticalFactor, 1.0f ) );

				CurrentMaterial.GetVariableByName( "_NoiseFrequencyFactor" ).AsVector.Set( new Vector2( m_NoiseFrequencyFactor, -(float) (Math.Log( m_NoiseFrequencyFactor ) / Math.Log( 2.0 )) ) );
				CurrentMaterial.GetVariableByName( "_NoiseAmplitudeFactor" ).AsVector.Set( new Vector2( m_NoiseAmplitudeFactor, 1.0f / (m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor * (1.0f + m_NoiseAmplitudeFactor)))) ) );
				CurrentMaterial.GetVariableByName( "_CloudPosition" ).AsVector.Set( m_CloudPosition );
				CurrentMaterial.GetVariableByName( "_CloudProfileTexture" ).AsResource.SetResource( m_CloudProfile );

				//////////////////////////////////////////////////////////////////////////
				// Render the low resolution sky probe
				m_Device.SetRenderTarget( m_AmbientProbe );
				m_Device.SetViewport( 0, 0, m_AmbientProbe.Width, m_AmbientProbe.Height, 0.0f, 1.0f );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( _IncludeClouds ? "RenderProbe" : "RenderProbeNoClouds" );

				CurrentMaterial.GetVariableByName( "_BufferInvSize" ).AsVector.Set( m_AmbientProbe.InvSize2 );
				CurrentMaterial.GetVariableByName( "_SkyProbePositionKm" ).AsVector.Set( m_SkySupport.WorldUnit2Kilometer * _Position );
				CurrentMaterial.GetVariableByName( "_SkyProbeAngles" ).AsVector.Set( new Vector4( AMBIENT_PROBE_PHI_START, AMBIENT_PROBE_THETA_START, AMBIENT_PROBE_PHI_END - AMBIENT_PROBE_PHI_START, AMBIENT_PROBE_THETA_END - AMBIENT_PROBE_THETA_START ) );

				if ( _IncludeClouds )
					CurrentMaterial.GetVariableByName( "_SkyProbeShadowMap" ).AsResource.SetResource( m_AmbientProbeShadowMap );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// Convolve into SH
				m_Device.SetMultipleRenderTargets( new RenderTargetView[] { _Targets1x1.GetSingleRenderTargetView( 0, 0 ), _Targets1x1.GetSingleRenderTargetView( 0, 1 ), _Targets1x1.GetSingleRenderTargetView( 0, 2 ) } );
				m_Device.SetViewport( 0, 0, 1, 1, 0.0f, 1.0f );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ConvolveProbe" );

				CurrentMaterial.GetVariableByName( "_SkyLightProbe" ).AsResource.SetResource( m_AmbientProbe );
				CurrentMaterial.GetVariableByName( "_TexSHConvolution" ).AsResource.SetResource( m_SHConvolution );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}
		}

		#region SH Sky Coefficients

		/// <summary>
		/// Builds the texture used for SH convolution
		/// </summary>
		protected Texture2D<PF_RGBA16F>	BuildSHConvolutionTexture()
		{
			float	C0 = (float) SphericalHarmonics.SHFunctions.ComputeSH( 0, 0, 0.0f, 0.0f );
			using ( Image<PF_RGBA16F> I = new Image<PF_RGBA16F>( m_Device, "SHConvolution", 2*AMBIENT_PROBE_SIZE, AMBIENT_PROBE_SIZE, ( int X, int Y, ref Vector4 _Color ) =>
			{
				float	Theta = AMBIENT_PROBE_THETA_START + Y * (AMBIENT_PROBE_THETA_END - AMBIENT_PROBE_THETA_START) / AMBIENT_PROBE_SIZE;
				float	Phi = AMBIENT_PROBE_PHI_START + 0.5f * X * (AMBIENT_PROBE_PHI_END - AMBIENT_PROBE_PHI_START) / AMBIENT_PROBE_SIZE;
					
				_Color.X = C0;
// 				_Color.Y = (float) SphericalHarmonics.SHFunctions.ComputeSH( 1, -1, Theta, Phi );
// 				_Color.Z = (float) SphericalHarmonics.SHFunctions.ComputeSH( 1, 0, Theta, Phi );
// 				_Color.W = (float) SphericalHarmonics.SHFunctions.ComputeSH( 1, +1, Theta, Phi );

				// Try with a windowed SH
				_Color.Y = (float) SphericalHarmonics.SHFunctions.ComputeSHWindowedCos( 1, -1, Theta, Phi, 2 );
				_Color.Z = (float) SphericalHarmonics.SHFunctions.ComputeSHWindowedCos( 1, 0, Theta, Phi, 2 );
				_Color.W = (float) SphericalHarmonics.SHFunctions.ComputeSHWindowedCos( 1, +1, Theta, Phi, 2 );

// 				_Color.Y = (float) SphericalHarmonics.SHFunctions.ComputeSHWindowedSinc( 1, -1, Theta, Phi, 2 );
// 				_Color.Z = (float) SphericalHarmonics.SHFunctions.ComputeSHWindowedSinc( 1, 0, Theta, Phi, 2 );
// 				_Color.W = (float) SphericalHarmonics.SHFunctions.ComputeSHWindowedSinc( 1, +1, Theta, Phi, 2 );

			}, 1 ) )
				return new Texture2D<PF_RGBA16F>( m_Device, "SHConvolution", I );
		}

		#endregion

		#endregion

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			ICloudSupport	I = _Interface as ICloudSupport;
			if ( I != null )
			{
				float	SigmaExtinction = 4.0f * (float) Math.PI * m_Density;
				float	SigmaScattering = m_Albedo * m_Density;

				I.SigmaExtinction = SigmaExtinction;
				I.SigmaScattering = SigmaScattering;
				I.ShadowOpacity = m_ShadowOpacity;
				I.CloudAltitudeThicknessKm = m_CloudAltitudeThicknessKm;
//				I.LightningIntensity = m_LightningIntensity;
//				I.LightningPosition = m_LightningPosition;

				// Shadow data
				I.ShadowPlaneCenterKm = m_ShadowPlaneCenterKm;
				I.ShadowPlaneX = m_ShadowPlaneX;
				I.ShadowPlaneY = m_ShadowPlaneY;
				I.ShadowPlaneOffset = m_ShadowPlaneOffset;
				I.ShadowQuadVertices = new Vector4( m_ShadowQuadKm[0].X, m_ShadowQuadKm[0].Y, m_ShadowQuadKm[2].X, m_ShadowQuadKm[2].Y );
				I.ShadowNormalsU = new Vector4( m_NU0.X, m_NU0.Y, m_NU1.X, m_NU1.Y );
				I.ShadowNormalsV = new Vector4( m_NV0.X, m_NV0.Y, m_NV1.X, m_NV1.Y );
				I.ShadowABC = m_ABC;
				I.ShadowDEF = m_DEF;
				I.ShadowGHI = m_GHI;
				I.ShadowJKL = m_JKL;
				I.DeepShadowMap0 = m_DeepShadowMaps[0];
				I.DeepShadowMap1 = m_DeepShadowMaps[1];
				return;
			}

			IAmbientSkySH	I2 = _Interface as IAmbientSkySH;
			I2.AmbientSkySHTexture = m_AmbientSkySH;
			I2.AmbientSkySHTextureNoCloud = m_AmbientSkySHNoCloud;
		}

		#endregion

		#endregion
	}
}
