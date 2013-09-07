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
	public class RenderTechniqueCloudLayer : RenderTechniqueBase, IComparer<RenderTechniqueCloudLayer.CloudLayer>, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const int		SHADOW_MAP_SIZE = 512;
		protected const int		PHASE_TEXTURE_SIZE = 256;

		protected const float	EARTH_RADIUS = 6400.0f;			// Earth radius in kilometers
		protected const float	ATMOSPHERE_ATLTITUDE = 100.0f;	// Altitude of top atmosphere in kilometers

		#endregion

		#region NESTED TYPES

		[System.ComponentModel.TypeConverter( typeof(CloudLayerTypeConverter) )]
		public class	CloudLayer
		{
 			protected float					m_Altitude = 10.0f;
			protected float					m_Thickness = 0.1f;

			// Appearance
			protected float					m_CloudSpeed = 0.01f;
			protected float					m_CloudEvolutionSpeed = 1.0f;
			protected float					m_DensityOffset = 0.0f;
			protected float					m_FrequencyFactor = 1.337f;
			protected float					m_FrequencyFactorAnisotropy = 1.0f;
			protected float					m_AmplitudeFactor = 0.5f;
			protected float					m_NoiseMipBias = 4.0f;
			protected float					m_NormalAmplitude = 0.5f;
			protected float					m_NoiseSize = 0.008f;

			// Lighting
			protected float					m_ScatteringCoeff = 0.01f;
// OLD PARAMS WHEN SKY WAS NOT SPLIT INTO SLICES
// 			protected float					m_FactorDoubleScattering = 50000.0f;
// 			protected float					m_FactorMultipleScattering = 0.1f;
// 			protected float					m_FactorSingleScattering = 50.0f;
// 			protected float					m_FactorSkyColor = 1.0f;
// 			protected float					m_FactorZeroScattering = 3.0f;
			protected float					m_FactorDoubleScattering = 500000.0f;
			protected float					m_FactorMultipleScattering = 2.0f;
			protected float					m_FactorSingleScattering = 100.0f;
			protected float					m_FactorSkyColor = 2.0f;
			protected float					m_FactorZeroScattering = 2.0f;

			#region PROPERTIES

			public float					Altitude
			{
				get { return m_Altitude; }
				set { m_Altitude = value; }
			}

			public float					ScatteringCoeff
			{
				get { return m_ScatteringCoeff; }
				set { m_ScatteringCoeff = value; }
			}

			public float					FactorZeroScattering
			{
				get { return m_FactorZeroScattering; }
				set { m_FactorZeroScattering = value; }
			}

			public float					FactorSingleScattering
			{
				get { return m_FactorSingleScattering; }
				set { m_FactorSingleScattering = value; }
			}

			public float					FactorDoubleScattering
			{
				get { return m_FactorDoubleScattering; }
				set { m_FactorDoubleScattering = value; }
			}

			public float					FactorMultipleScattering
			{
				get { return m_FactorMultipleScattering; }
				set { m_FactorMultipleScattering = value; }
			}

			public float					FactorSkyColor
			{
				get { return m_FactorSkyColor; }
				set { m_FactorSkyColor = value; }
			}

			public float					DensityOffset
			{
				get { return m_DensityOffset; }
				set { m_DensityOffset = value; }
			}

			public float					Thickness
			{
				get { return m_Thickness; }
				set { m_Thickness = value; }
			}

			public float					EvolutionSpeed
			{
				get { return m_CloudEvolutionSpeed; }
				set { m_CloudEvolutionSpeed = value; }
			}

			public float					Speed
			{
				get { return m_CloudSpeed; }
				set { m_CloudSpeed = value; }
			}

			public float					FrequencyFactor
			{
				get { return m_FrequencyFactor; }
				set { m_FrequencyFactor = value; }
			}

			public float					AmplitudeFactor
			{
				get { return m_AmplitudeFactor; }
				set { m_AmplitudeFactor = value; }
			}

			public float					FrequencyFactorAnisotropy
			{
				get { return m_FrequencyFactorAnisotropy; }
				set { m_FrequencyFactorAnisotropy = value; }
			}

			public float					NoiseMipBias
			{
				get { return m_NoiseMipBias; }
				set { m_NoiseMipBias = value; }
			}

			public float					NormalAmplitude
			{
				get { return m_NormalAmplitude; }
				set { m_NormalAmplitude = value; }
			}

			public float					NoiseSize
			{
				get { return m_NoiseSize; }
				set { m_NoiseSize = value; }
			}

			#endregion
		}

		public class	CloudLayerTypeConverter : System.ComponentModel.TypeConverter
		{
			// Sub-properties
			public override bool GetPropertiesSupported( System.ComponentModel.ITypeDescriptorContext _Context )
			{
				return	true;
			}

			public override System.ComponentModel.PropertyDescriptorCollection	GetProperties( System.ComponentModel.ITypeDescriptorContext _Context, object _Value, System.Attribute[] _Attributes )
			{
				return System.ComponentModel.TypeDescriptor.GetProperties( typeof(CloudLayer), new System.Attribute[] { new System.ComponentModel.BrowsableAttribute( true ) } );
			}
		}

		public class	IShadowMapSupport : ShaderInterfaceBase
		{
			[Semantic( "SHADOW_ANGULAR_BOUNDS" )]
			public Vector4		ShadowAngularBounds		{ set { SetVector( "SHADOW_ANGULAR_BOUNDS", value ); } }
			[Semantic( "SHADOW_INVANGULAR_BOUNDS" )]
			public Vector4		ShadowInvAngularBounds	{ set { SetVector( "SHADOW_INVANGULAR_BOUNDS", value ); } }
			[Semantic( "SHADOW2WORLD" )]
			public Matrix		Shadow2World			{ set { SetMatrix( "SHADOW2WORLD", value ); } }
			[Semantic( "WORLD2SHADOW" )]
			public Matrix		World2Shadow			{ set { SetMatrix( "WORLD2SHADOW", value ); } }
			[Semantic( "SHADOW_ALTITUDES_MIN" )]
			public Vector4		ShadowAltitudesMinKm	{ set { SetVector( "SHADOW_ALTITUDES_MIN", value ); } }
			[Semantic( "SHADOW_ALTITUDES_MAX" )]
			public Vector4		ShadowAltitudesMaxKm	{ set { SetVector( "SHADOW_ALTITUDES_MAX", value ); } }
			[Semantic( "SHADOW_MAP" )]
			public ITexture2D	ShadowMap				{ set { SetResource( "SHADOW_MAP", value ); } }
		}

		#endregion

		#region FIELDS

		protected Material<VS_Pt4>				m_MaterialCloud = null;
		protected Material<VS_Pt4>				m_MaterialSky = null;
		protected Material<VS_Pt4>				m_MaterialCompose = null;

		// Render targets
		protected RenderTarget<PF_RGBA16F>		m_ShadowMap = null;
		protected RenderTarget<PF_RGBA16F>[]	m_CloudMaps = new RenderTarget<PF_RGBA16F>[4];
		protected RenderTarget<PF_RGBA16F>		m_SkyMap = null;

		// Textures
		protected Texture2D<PF_RGBA16F>			m_ShadowMapInit = null;
		protected Texture2D<PF_RGBA16F>			m_CloudMapEmpty = null;
		protected Texture2D<PF_R16F>			m_TexturePhase = null;
		protected Texture2D<PF_R16F>			m_TexturePhaseConvolved = null;
		protected Texture2D<PF_RGBA8>[]			m_NoiseTextures2D = new Texture2D<PF_RGBA8>[4];
		protected ITexture2D					m_NightSkyCubeMap = null;

		// 4 Render states to write R, G, B, A individually
		protected BlendState[]					m_ShadowBlendStates = new BlendState[4];

		protected Nuaj.Helpers.ScreenQuad		m_Quad = null;

		// Cloud layers
		protected List<CloudLayer>				m_CloudLayers = new List<CloudLayer>();
		protected List<CloudLayer>				m_WorkLayers = new List<CloudLayer>();

		// Parameters
		protected int							m_SkyStepsCount = 32;
		protected int							m_SkyAboveStepsCount = 8;

protected Vector4	m_DEBUG = Vector4.Zero;

		// IShadowMapSupport cached parameters
		protected Vector4						m_ShadowMapAngularBounds;
		protected Vector4						m_ShadowMapInvAngularBounds;
		protected Matrix						m_ShadowMap2World;
		protected Matrix						m_World2ShadowMap;
		protected Vector4						m_ShadowMapAltitudesMin;
		protected Vector4						m_ShadowMapAltitudesMax;

		#endregion

		#region PROPERTIES

		public CloudLayer[]			CloudLayers			{ get { return m_CloudLayers.ToArray(); } }

		public int					SkyStepsCount		{ get { return m_SkyStepsCount; } set { m_SkyStepsCount = value; } }
		public int					SkyAboveStepsCount	{ get { return m_SkyAboveStepsCount; } set { m_SkyAboveStepsCount = value; } }
		public Vector4				DEBUG				{ get { return m_DEBUG; } set { m_DEBUG = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueCloudLayer( RendererSetup _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Register the IShadowMapSupport interface
			m_Device.DeclareShaderInterface( typeof(IShadowMapSupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IShadowMapSupport), this );

			//////////////////////////////////////////////////////////////////////////
			// Build materials
			m_MaterialCloud = ToDispose( new Material<VS_Pt4>( m_Device, "Cloud Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/CloudLayer2.fx" ) ) );
			m_MaterialSky = ToDispose( new Material<VS_Pt4>( m_Device, "Sky Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/SkyLayer.fx" ) ) );
			m_MaterialCompose = ToDispose( new Material<VS_Pt4>( m_Device, "Compose Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/ComposeCloudSky.fx" ) ) );

			//////////////////////////////////////////////////////////////////////////
			// Build maps
			int		Width = m_Renderer.GeometryBuffer.Width;
			int		Height = m_Renderer.GeometryBuffer.Height;
			int		SkyWidth = Width >> 2;
			int		SkyHeight = Height >> 2;

			m_ShadowMap = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Cloud Shadow Map", SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, 1 ) );
			for ( int MapIndex=0; MapIndex < 4; MapIndex++ )
				m_CloudMaps[MapIndex] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Cloud Map", Width, Height, 1 ) );

			m_SkyMap = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Sky Map", SkyWidth, SkyHeight, 1 ) );

			BuildPhaseFunction();

			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures2D[NoiseIndex] = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Noise2D", new System.IO.FileInfo( "./Media/NoiseMaps/NoiseNormalHeight" + NoiseIndex + ".png" ), 0, 1.0f ) );

			// Build dummy shadow map for initialization
			using ( Image<PF_RGBA16F> I = new Image<PF_RGBA16F>( m_Device, "Shadow Map Init", 1, 1, (int _X, int _Y, ref Vector4 _Color ) => { _Color = Vector4.One; }, 1 ) )
				m_ShadowMapInit = ToDispose( new Texture2D<PF_RGBA16F>( m_Device, "Shadow Map Init", I ) );

			using ( Image<PF_RGBA16F> I = new Image<PF_RGBA16F>( m_Device, "Cloud Map Empty", 1, 1, (int _X, int _Y, ref Vector4 _Color ) => { _Color = new Vector4( 0, 0, 0, 1 ); }, 1 ) )
				m_CloudMapEmpty = ToDispose( new Texture2D<PF_RGBA16F>( m_Device, "Cloud Map Empty", I ) );

			// Load the night sky cube map
// 			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "./Media/CubeMaps/milkywaypan_brunier_2048.jpg" ) as System.Drawing.Bitmap )
// 				using ( ImageCube<PF_RGBA8> I = new ImageCube<PF_RGBA8>( m_Device, "NightSkyImage", B, ImageCube<PF_RGBA8>.FORMATTED_IMAGE_TYPE.CYLINDRICAL, 0 ))
// 					m_NightSkyCubeMap = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "NightSkyCube", I ) );

			//////////////////////////////////////////////////////////////////////////
			// Build layers
			CloudLayer	CL = new CloudLayer();
// 			CL.Altitude = 5.0f;
// 			m_CloudLayers.Add( CL );

			CL = new CloudLayer();
			CL.Altitude = 10.0f;
			m_CloudLayers.Add( CL );

			//////////////////////////////////////////////////////////////////////////
			// Build geometry
			m_Quad = ToDispose( new Nuaj.Helpers.ScreenQuad( m_Device, "Cloud Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Build 4 render states with blend mask
			BlendStateDescription	BSDesc = m_Device.GetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED ).Description;
			BSDesc.RenderTargetWriteMask[0] = SharpDX.Direct3D10.ColorWriteMaskFlags.Red;
			m_ShadowBlendStates[0] = ToDispose( new BlendState( m_Device.DirectXDevice, ref BSDesc ) );
			BSDesc.RenderTargetWriteMask[0] = SharpDX.Direct3D10.ColorWriteMaskFlags.Green;
			m_ShadowBlendStates[1] = ToDispose( new BlendState( m_Device.DirectXDevice, ref BSDesc ) );
			BSDesc.RenderTargetWriteMask[0] = SharpDX.Direct3D10.ColorWriteMaskFlags.Blue;
			m_ShadowBlendStates[2] = ToDispose( new BlendState( m_Device.DirectXDevice, ref BSDesc ) );
			BSDesc.RenderTargetWriteMask[0] = SharpDX.Direct3D10.ColorWriteMaskFlags.Alpha;
			m_ShadowBlendStates[3] = ToDispose( new BlendState( m_Device.DirectXDevice, ref BSDesc ) );
		}

		public override void	Render( int _FrameToken )
		{
			Vector3	SunDirection = m_Renderer.Sun.Direction;

			//////////////////////////////////////////////////////////////////////////
			// Sort cloud layers top to bottom
//			float	CameraAltitude = m_Renderer.Sky.WorldUnit2Kilometers * Camera2World.Row4.Y;

			m_WorkLayers.Clear();
			foreach ( CloudLayer CL in m_CloudLayers )
				if ( CL.Thickness > 0.0f && CL.DensityOffset > -1.0f )
					m_WorkLayers.Add( CL );
			m_WorkLayers.Sort( this );	// From top to bottom

			// Retrieve highest cloud altitude
			float	CloudAltitudeMax = -float.MaxValue;
			foreach ( CloudLayer CL in m_WorkLayers )
				CloudAltitudeMax = Math.Max( CloudAltitudeMax, CL.Altitude );

			if ( m_WorkLayers.Count > 0 )
				PrepareShadowMap( CloudAltitudeMax, SunDirection );

			//////////////////////////////////////////////////////////////////////////
			// Render Clouds
			m_ShadowMapAltitudesMin = -EARTH_RADIUS * Vector4.One;
			m_ShadowMapAltitudesMax = -EARTH_RADIUS * Vector4.One;
			if ( m_WorkLayers.Count < 4 )
				m_Device.ClearRenderTarget( m_ShadowMap, Vector4.One );	// Clear to unit extinction so non-existing layers don't interfere

			if ( m_WorkLayers.Count > 0 )
				using ( m_MaterialCloud.UseLock() )
				{
					CurrentMaterial.SetVector( "SunDirection", SunDirection );
					CurrentMaterial.SetVector( "SunColor", m_Renderer.Sky.SunColor );
					CurrentMaterial.SetVector( "SkyColor", m_Renderer.Sky.AmbientSkyColor );
					CurrentMaterial.SetScalar( "WorldUnit2Kilometer", m_Renderer.Sky.WorldUnit2Kilometers );

					CurrentMaterial.SetResource( "PhaseMie", m_TexturePhase );
					CurrentMaterial.SetResource( "PhaseMie_Convolved", m_TexturePhaseConvolved );
					CurrentMaterial.SetResource( "NoiseTexture2D0", m_NoiseTextures2D[0] );
					CurrentMaterial.SetResource( "NoiseTexture2D1", m_NoiseTextures2D[1] );
					CurrentMaterial.SetResource( "NoiseTexture2D2", m_NoiseTextures2D[2] );
					CurrentMaterial.SetResource( "NoiseTexture2D3", m_NoiseTextures2D[3] );

					for ( int CloudIndex=0; CloudIndex < m_WorkLayers.Count; CloudIndex++ )
					{
						CloudLayer	CL = m_WorkLayers[CloudIndex];

						CurrentMaterial.SetScalar( "CloudAltitudeKm", CL.Altitude );
						CurrentMaterial.SetVector( "CloudThicknessKm", new Vector2( CL.Thickness, 1.0f / CL.Thickness ) );
						CurrentMaterial.SetScalar( "CloudDensityOffset", CL.DensityOffset );
						CurrentMaterial.SetScalar( "NoiseSize", CL.NoiseSize );
						CurrentMaterial.SetScalar( "CloudSpeed", CL.Speed );
						CurrentMaterial.SetScalar( "CloudEvolutionSpeed", CL.Speed * CL.EvolutionSpeed );
						CurrentMaterial.SetScalar( "CloudTime", m_Renderer.Time );
						CurrentMaterial.SetVector( "FrequencyFactor", new Vector2( CL.FrequencyFactor, CL.FrequencyFactor * CL.FrequencyFactorAnisotropy ) );
						CurrentMaterial.SetVector( "AmplitudeFactor", new Vector4( CL.AmplitudeFactor, 1.0f / (1.0f + CL.AmplitudeFactor), 1.0f / (1.0f + CL.AmplitudeFactor * (1.0f + CL.AmplitudeFactor)), 1.0f / (1.0f + CL.AmplitudeFactor * (1.0f + CL.AmplitudeFactor * (1.0f + CL.AmplitudeFactor))) ) );
						CurrentMaterial.SetScalar( "NoiseMipBias", CL.NoiseMipBias );
						CurrentMaterial.SetScalar( "NormalAmplitude", CL.NormalAmplitude );

						CurrentMaterial.SetScalar( "ScatteringCoeff", CL.ScatteringCoeff );
						CurrentMaterial.SetVector( "ScatteringFactors", new Vector4( CL.FactorZeroScattering, CL.FactorSingleScattering, CL.FactorDoubleScattering, CL.FactorMultipleScattering ) );
						CurrentMaterial.SetScalar( "ScatteringSkyFactor", CL.FactorSkyColor );

						//////////////////////////////////////////////////////////////////////////
						// Render Shadow map
						m_Device.OutputMerger.BlendState = m_ShadowBlendStates[CloudIndex];
						m_Device.SetRenderTarget( m_ShadowMap );
						m_Device.SetViewport( m_ShadowMap );
						CurrentMaterial.SetVector( "BufferInvSize", m_ShadowMap.InvSize2 );
						CurrentMaterial.SetResource( "ShadowMap", null as ITexture2D );

						CurrentMaterial.CurrentTechnique = m_MaterialCloud.GetTechniqueByName( "DrawShadow" );
						CurrentMaterial.ApplyPass( 0 );
						m_Quad.Render();

						//////////////////////////////////////////////////////////////////////////
						// Render Cloud map
						m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );
						m_Device.SetRenderTarget( m_CloudMaps[CloudIndex] );
						m_Device.SetViewport( m_CloudMaps[CloudIndex] );
						CurrentMaterial.SetVector( "BufferInvSize", m_CloudMaps[CloudIndex].InvSize2 );

						CurrentMaterial.SetVector( "ShadowAltitudesMinKm", m_ShadowMapAltitudesMin );
						CurrentMaterial.SetVector( "ShadowAltitudesMaxKm", m_ShadowMapAltitudesMax );
						CurrentMaterial.SetResource( "ShadowMap", m_ShadowMap );

						CurrentMaterial.CurrentTechnique = m_MaterialCloud.GetTechniqueByName( "DrawCloud" );
						CurrentMaterial.ApplyPass( 0 );
						m_Quad.Render();

						// Update shadow altitudes
						switch ( CloudIndex )
						{
							case 0:
								m_ShadowMapAltitudesMin.X = CL.Altitude;
								m_ShadowMapAltitudesMax.X = CL.Altitude + CL.Thickness;
								break;
							case 1:
								m_ShadowMapAltitudesMin.Y = CL.Altitude;
								m_ShadowMapAltitudesMax.Y = CL.Altitude + CL.Thickness;
								break;
							case 2:
								m_ShadowMapAltitudesMin.Z = CL.Altitude;
								m_ShadowMapAltitudesMax.Z = CL.Altitude + CL.Thickness;
								break;
							case 3:
								m_ShadowMapAltitudesMin.W = CL.Altitude;
								m_ShadowMapAltitudesMax.W = CL.Altitude + CL.Thickness;
								break;
						}
					}
				}

			//////////////////////////////////////////////////////////////////////////
			// Render Sky
			using ( m_MaterialSky.UseLock() )
			{
				m_Device.SetRenderTarget( m_SkyMap );
				m_Device.SetViewport( m_SkyMap );

				CurrentMaterial.SetVector( "BufferInvSize", m_SkyMap.InvSize2 );

CurrentMaterial.SetVector( "DEBUG", m_DEBUG );

				CurrentMaterial.SetScalar( "SkyStepsCount", m_SkyStepsCount );
				CurrentMaterial.SetScalar( "SkyAboveStepsCount", m_SkyAboveStepsCount + (m_WorkLayers.Count == 0 ? m_SkyStepsCount : 0) );
				for ( int CloudLayerIndex=0; CloudLayerIndex < m_WorkLayers.Count; CloudLayerIndex++ )
					CurrentMaterial.SetResource( "CloudLayerTexture" + CloudLayerIndex, m_CloudMaps[CloudLayerIndex] );
				for ( int CloudLayerIndex=m_WorkLayers.Count; CloudLayerIndex < 5; CloudLayerIndex++ )
					CurrentMaterial.SetResource( "CloudLayerTexture" + CloudLayerIndex, m_CloudMapEmpty );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}

			//////////////////////////////////////////////////////////////////////////
			// Compose Clouds & Sky
			using ( m_MaterialCompose.UseLock() )
			{
				m_Renderer.SetRenderTargets( RendererSetup.RENDER_TARGET_TYPES.EMISSIVE );

				CurrentMaterial.SetVector( "BufferInvSize", m_Renderer.EmissiveBuffers[1].InvSize2 );

				for ( int CloudIndex=0; CloudIndex < m_WorkLayers.Count; CloudIndex++ )
					CurrentMaterial.SetResource( "CloudLayerTexture" + CloudIndex, m_CloudMaps[CloudIndex] );
				for ( int CloudLayerIndex=m_WorkLayers.Count; CloudLayerIndex < 4; CloudLayerIndex++ )
					CurrentMaterial.SetResource( "CloudLayerTexture" + CloudLayerIndex, m_CloudMapEmpty );
				CurrentMaterial.SetResource( "SkyTexture", m_SkyMap );

CurrentMaterial.SetResource( "ShadowMap", m_ShadowMap );

				CurrentMaterial.SetVector( "SunDirection", SunDirection );
				CurrentMaterial.SetVector( "SunIntensity", m_Renderer.Sky.SunColor );
				CurrentMaterial.SetResource( "NightSkyCubeMap", m_NightSkyCubeMap );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}

			m_Renderer.SwapEmissiveBuffers();
		}

		protected void		PrepareShadowMap( float _CloudAltitudeMax, Vector3 _SunDirection )
		{
			//////////////////////////////////////////////////////////////////////////
			// Determine a solid basis for shadows
			// The shadows lie on a hemisphere facing the Sun
			// The X axis is then the Sun's direction
			// We need to determine the Y and Z axes based on camera orientation
			Matrix	Camera2World = m_Renderer.Camera.Camera2World;

			Vector3	CameraRight = (Vector3) Camera2World.Row1;
			Vector3	CameraUp = (Vector3) Camera2World.Row2;
			Vector3	CameraAt = (Vector3) Camera2World.Row3;

			float	DotX = Vector3.Dot( CameraRight, _SunDirection );
			float	DotY = Vector3.Dot( CameraUp, _SunDirection );

			Vector3	SunY, SunZ;
			if ( Math.Abs( DotX ) <= Math.Abs( DotY ) )
			{	// X can be used as our Z-like
				SunY = Vector3.Cross( CameraRight, _SunDirection );
				SunY.Normalize();
				SunZ = Vector3.Cross( _SunDirection, SunY );
			}
			else
			{	// Y can be used as our Y-like
				SunZ = Vector3.Cross( _SunDirection, CameraUp );
				SunZ.Normalize();
				SunY = Vector3.Cross( SunZ, _SunDirection );
			}

			//////////////////////////////////////////////////////////////////////////
			// Compute camera frustum's main directions' intersections with the upper atmosphere
			Vector3	EarthCenter = Vector3.Zero;
			float	CloudRadius = EARTH_RADIUS + _CloudAltitudeMax;

			// We first try to compute the intersections of the camera frustum's rays
			//	(i.e. the 4 directions of the 4 corners) with Earth and the upper atmosphere
			Vector3	CameraPosition = (Vector3) Camera2World.Row4;
			CameraPosition *= m_Renderer.Sky.WorldUnit2Kilometers;	// We need its position in kilometers
//			Vector3	ToCamera = CameraPosition - EarthCenter;
//			ToCamera.Normalize();
//			CameraPosition += EARTH_RADIUS * ToCamera;					// Offset by Earth radius so we're above the surface
			CameraPosition += new Vector3( 0.0f, EARTH_RADIUS, 0.0f );	// Offset by Earth radius so we're above the surface

			float		TanHalfFOVy = (float) Math.Tan( 0.5f * m_Renderer.Camera.PerspectiveFOV );
			float		TanHalfFOVx = m_Renderer.Camera.AspectRatio * TanHalfFOVy;

			Vector3[]	CameraRays = new Vector3[]
			{
				new Vector3( -TanHalfFOVx, +TanHalfFOVy, 1.0f ),
				new Vector3( -TanHalfFOVx, -TanHalfFOVy, 1.0f ),
				new Vector3( +TanHalfFOVx, -TanHalfFOVy, 1.0f ),
				new Vector3( +TanHalfFOVx, +TanHalfFOVy, 1.0f ),
			};

			CameraRays[0] = Vector3.TransformNormal( CameraRays[0], Camera2World );
			CameraRays[1] = Vector3.TransformNormal( CameraRays[1], Camera2World );
			CameraRays[2] = Vector3.TransformNormal( CameraRays[2], Camera2World );
			CameraRays[3] = Vector3.TransformNormal( CameraRays[3], Camera2World );

			float[]		IntersectionsEarth = new float[4];
			Vector3[]	IntersectionsAtmosphere = new Vector3[6];	// We'll have a maximum of 6 intersections

			// Compute intersections with Earth
			for ( int RayIndex=0; RayIndex < 4; RayIndex++ )
				ComputeForwardSphereIntersection( ref CameraPosition, ref CameraRays[RayIndex], ref EarthCenter, EARTH_RADIUS, out IntersectionsEarth[RayIndex] );

			// Compute intersections with atmosphere
			Vector3	Tangent, StartPosition;
			int		TargetRayIndex = 0;
			float	HitDistance = -1.0f;
			for ( int RayIndex=0; RayIndex < 4; RayIndex++ )
			{
				int	NextRayIndex = (RayIndex+1) & 3;

				// We need to distinguish several cases here :
				// . Current and Next rays don't hit the Earth (i.e. viewing up) => We can cast current ray to atmosphere THEN cast it again in Sun's direction
				// . Current ray hits the Earth, Next ray doesn't => We compute the tangent vector between current and next and cast this ray to the atmosphere THEN cast it again in Sun's direction
				// . Next ray hits the Earth, Current ray doesn't => We compute the tangent vector between current and next and cast BOTH current and tangent rays to the atmosphere THEN cast both of them again in Sun's direction
				// . Current and Next rays hit the Earth => No ray can hit the atmosphere but we attempt to project current ray to atmosphere, following the Sun's direction
				//	=> In this last case, if the Sun is behind the Earth then no ray can hit the atmosphere

				int	HitCase = (IntersectionsEarth[RayIndex] > 0.0f ? 1 : 0) | (IntersectionsEarth[NextRayIndex] > 0.0f ? 2 : 0);
				switch ( HitCase )
				{
					case 0:	// Current and Next can hit the atmosphere => Compute current's intersection only
						if ( ComputeForwardSphereIntersection( ref CameraPosition, ref CameraRays[RayIndex], ref EarthCenter, CloudRadius, out HitDistance ) )
						{
							StartPosition = CameraPosition + HitDistance * CameraRays[RayIndex];	// Start on atmosphere
							ReprojectToSunHemisphere( ref StartPosition, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance );
							IntersectionsAtmosphere[TargetRayIndex++] = StartPosition + HitDistance * _SunDirection;
						}
						break;

					case 1:	// Current ray hits the Earth, Next doesn't => Compute tangent and cast it toward the atmosphere
						if ( ComputeTangentVector( ref CameraPosition, ref CameraRays[NextRayIndex], ref CameraRays[RayIndex], ref EarthCenter, EARTH_RADIUS, out Tangent ) )
						{
							if ( ComputeForwardSphereIntersection( ref CameraPosition, ref Tangent, ref EarthCenter, CloudRadius, out HitDistance ) )
							{
								StartPosition = CameraPosition + HitDistance * Tangent;	// Start on atmosphere
								ReprojectToSunHemisphere( ref StartPosition, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance );
								IntersectionsAtmosphere[TargetRayIndex++] = StartPosition + HitDistance * _SunDirection;
							}
						}
						break;

					case 2:	// Next ray hits the Earth, Current doesn't => Compute tangent and cast BOTH current and tangent toward the atmosphere
						if ( ComputeForwardSphereIntersection( ref CameraPosition, ref CameraRays[RayIndex], ref EarthCenter, CloudRadius, out HitDistance ) )
						{
							StartPosition = CameraPosition + HitDistance * CameraRays[RayIndex];	// Start on atmosphere
							ReprojectToSunHemisphere( ref StartPosition, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance );
							IntersectionsAtmosphere[TargetRayIndex++] = StartPosition + HitDistance * _SunDirection;
						}
						if ( ComputeTangentVector( ref CameraPosition, ref CameraRays[RayIndex], ref CameraRays[NextRayIndex], ref EarthCenter, EARTH_RADIUS, out Tangent ) )
						{
							if ( ComputeForwardSphereIntersection( ref CameraPosition, ref Tangent, ref EarthCenter, CloudRadius, out HitDistance ) )
							{
								StartPosition = CameraPosition + HitDistance * Tangent;	// Start on atmosphere
								ReprojectToSunHemisphere( ref StartPosition, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance );
								IntersectionsAtmosphere[TargetRayIndex++] = StartPosition + HitDistance * _SunDirection;
							}
						}
						break;

					case 3:	// Both current and next rays hit the Earth => Project current to atmosphere by following Sun's direction
						StartPosition = CameraPosition + IntersectionsEarth[RayIndex] * CameraRays[RayIndex];	// Start on Earth
						if ( Vector3.Dot( StartPosition - EarthCenter, _SunDirection ) > 0.0f )
						{	// Check the intersection is not on the other side of the planet !
							if ( !ComputeForwardSphereIntersection( ref StartPosition, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance ) )
								throw new Exception( "We should have a hit !" );

							IntersectionsAtmosphere[TargetRayIndex++] = StartPosition + HitDistance * _SunDirection;
						}
						break;
				}
			}

			// We perform a last projection of the camera position's toward the atmosphere by following the Sun's direction
			if ( ComputeForwardSphereIntersection( ref CameraPosition, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance ) )
			{
				// Check the intersection is not on the other side of the planet !
				float	EarthHitDistance;
				if ( ComputeForwardSphereIntersection( ref CameraPosition, ref _SunDirection, ref EarthCenter, EARTH_RADIUS, out EarthHitDistance ) && EarthHitDistance < HitDistance )
				{	// Use Earth's tangent at camera position
					// We projet the camera to the Earth following the Sun's direction
					Vector3	PositionOnEarth = CameraPosition + EarthHitDistance * _SunDirection;

					// Compute the circle we have to follow (indeed, we're following Earth surface in camera view direction, which doesn't have to be a geodesic)
					Vector3	ToPositionEarth = PositionOnEarth - EarthCenter;
					Vector3	CircleOrtho = Vector3.Cross( CameraAt, ToPositionEarth );
					CircleOrtho.Normalize();

					float	CircleDistance = Vector3.Dot( ToPositionEarth, CircleOrtho );	// Position on Earth projected to the circle's normal
					float	CircleRadius = (float) Math.Sqrt( EARTH_RADIUS*EARTH_RADIUS - CircleDistance*CircleDistance );
					Vector3	CircleCenter = EarthCenter + CircleDistance * CircleOrtho;

					// Then we must follow the Earth's surface until we reach a tangential position
					Vector3	ToTangent = Vector3.Cross( CircleOrtho, _SunDirection );
					ToTangent.Normalize();
					float	DotViewDir = Vector3.Dot( CameraAt, ToTangent );
					Vector3	CircleTangent = CircleCenter + (DotViewDir > 0.0 ? +1.0f : -1.0f) * CircleRadius * ToTangent;

					// And finally, project that tangential position toward the atmosphere following the Sun's direction
					if ( !ComputeForwardSphereIntersection( ref CircleTangent, ref _SunDirection, ref EarthCenter, CloudRadius, out HitDistance ) )
						throw new Exception( "We should hit the Atmosphere !" );

					IntersectionsAtmosphere[TargetRayIndex++] = CircleTangent + HitDistance * _SunDirection;
				}
				else
					IntersectionsAtmosphere[TargetRayIndex++] = CameraPosition + HitDistance * _SunDirection;
			}

			//////////////////////////////////////////////////////////////////////////
			// Use the average atmosphere intersection as center direction for the SHADOW->WORLD transform
			Vector3	ShadowX = Vector3.Zero;
			for ( int RayIndex=0; RayIndex < TargetRayIndex; RayIndex++ )
				ShadowX += IntersectionsAtmosphere[RayIndex];
			ShadowX.Normalize();

			Vector3	ShadowY = Vector3.Cross( SunZ, ShadowX );
			ShadowY.Normalize();
			Vector3	ShadowZ = Vector3.Cross( ShadowX, ShadowY );

			// Build the SHADOW->WORLD transform
			m_ShadowMap2World = Matrix.Identity;
			m_ShadowMap2World.Row1 = new Vector4( ShadowX, 0.0f );
			m_ShadowMap2World.Row2 = new Vector4( ShadowY, 0.0f );
			m_ShadowMap2World.Row3 = new Vector4( ShadowZ, 0.0f );

			// And its inverse
			m_World2ShadowMap = m_ShadowMap2World;
			m_World2ShadowMap.Invert();


			//////////////////////////////////////////////////////////////////////////
			// Compute shadow angular boundaries
			// By default, the entire hemisphere casts shadows
			// The goal is to reduce the hemisphere to the angular portion the camera is really able to see...

			// Now, we compute angle couples for each atmosphere intersection and build boundaries
			float	AngleMinX = +0.5f * (float) Math.PI;
			float	AngleMaxX = -0.5f * (float) Math.PI;
			float	AngleMinY = +0.5f * (float) Math.PI;
			float	AngleMaxY = -0.5f * (float) Math.PI;

			for ( int RayIndex=0; RayIndex < TargetRayIndex; RayIndex++ )
			{
				Vector3	ToSphere = IntersectionsAtmosphere[RayIndex] - EarthCenter;
						ToSphere /= CloudRadius;	// Normalize

				// Transform into Sun space
				Vector3	Position2Sun;
				Position2Sun.X = Vector3.Dot( ToSphere, ShadowX );
				Position2Sun.Y = Vector3.Dot( ToSphere, ShadowY );
				Position2Sun.Z = Vector3.Dot( ToSphere, ShadowZ );

				// Compute angles
				float	AngleX = (float) Math.Atan( Position2Sun.Z / Position2Sun.X );
				float	AngleY = (float) Math.Asin( Position2Sun.Y );

				float	AngleX_TestDL = Atan_DL( Position2Sun.Z / Position2Sun.X );
				float	AngleY_TestDL = Asin_DL( Position2Sun.Y );

				float	AngleX_TestPadé = Atan_Padé( Position2Sun.Z / Position2Sun.X );
				float	AngleY_TestPadé = Asin_Padé( Position2Sun.Y );

				// Update boundaries
				AngleMinX = Math.Min( AngleMinX, AngleX );
				AngleMaxX = Math.Max( AngleMaxX, AngleX );
				AngleMinY = Math.Min( AngleMinY, AngleY );
				AngleMaxY = Math.Max( AngleMaxY, AngleY );
			}

			float	AngleCenterX = 0.5f * (AngleMinX + AngleMaxX);
			float	AngleCenterY = 0.5f * (AngleMinY + AngleMaxY);
			float	DeltaAngleX = Math.Max( 1e-5f, AngleMaxX - AngleMinX );
			float	DeltaAngleY = Math.Max( 1e-5f, AngleMaxY - AngleMinY );

			AngleMinX = AngleCenterX - 0.5f * DeltaAngleX;
			AngleMaxX = AngleCenterX + 0.5f * DeltaAngleX;
			AngleMinY = AngleCenterY - 0.5f * DeltaAngleY;
			AngleMaxY = AngleCenterY + 0.5f * DeltaAngleY;

			m_ShadowMapAngularBounds = new Vector4( AngleMinX, AngleMinY, DeltaAngleX, DeltaAngleY );
			m_ShadowMapInvAngularBounds = new Vector4( -AngleMinX, -AngleMinY, 1.0f / DeltaAngleX, 1.0f / DeltaAngleY );
		}

		#region Shadow Computation

		/// <summary>
		/// Taylor series for Atan (source: http://en.wikipedia.org/wiki/Taylor_series)
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		protected float	Atan_DL( float x )
		{
			float	x2 = x*x;
			float	x3 = x*x2;
			float	x5 = x3*x2;
			return x - 0.33333333333333f * x3 + 0.2f * x5;
		}

		/// <summary>
		/// Taylor series for Asin (source: http://en.wikipedia.org/wiki/Taylor_series)
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		protected float	Asin_DL( float x )
		{
			if ( x < 0.5f )
			{
				float	x2 = x*x;
				float	x3 = x*x2;
				float	x5 = x3*x2;
				return x + 0.16666666666666666666666666666667f * x3 + 0.075f * x5;
			}
			else
			{
				x = (float) Math.Sqrt( 1.0f - x*x );
				float	x2 = x*x;
				float	x3 = x*x2;
				float	x5 = x3*x2;
				return 0.5f * (float) Math.PI - (x + 0.16666666666666666666666666666667f * x3 + 0.075f * x5);
			}
		}

		/// <summary>
		/// Padé approximation for Atan
		/// (source: http://math.fullerton.edu/mathews/n2003/pade/PadeApproximationMod/Links/PadeApproximationMod_lnk_14.html)
		/// </summary>
		/// <param name="_Tan"></param>
		/// <returns></returns>
		protected float	Atan_Padé( float x )
		{
			float	x2 = x*x;
			float	x3 = x*x2;
			float	x4 = x2*x2;
			float	Num = 105.0f * x + 55.0f * x3;
			float	Den = 105.0f + 150.0f * x2 + 9.0f * x4;
			return Num / Den;
		}

		/// <summary>
		/// Padé approximation for Asin
		/// (source: http://math.fullerton.edu/mathews/n2003/pade/PadeApproximationMod/Links/PadeApproximationMod_lnk_15.html)
		/// </summary>
		/// <param name="_Tan"></param>
		/// <returns></returns>
		protected float	Asin_Padé( float x )
		{
			float	x2 = x*x;
			float	x3 = x*x2;
			float	x4 = x2*x2;
			float	Num = 14280.0f * x - 7340.0f * x3;
			float	Den = 14280.0f - 9720.0f * x2 + 549.0f * x4;
			return Num / Den;
		}

		/// <summary>
		/// Computes the intersection of a ray with a sphere
		/// </summary>
		/// <param name="_Position"></param>
		/// <param name="_Direction"></param>
		/// <param name="_SphereCenter"></param>
		/// <param name="_SphereRadius"></param>
		/// <param name="_Distance0"></param>
		/// <param name="_Distance1"></param>
		/// <returns></returns>
		protected bool	ComputeSphereIntersection( ref Vector3 _Position, ref Vector3 _Direction, ref Vector3 _SphereCenter, float _SphereRadius, out float _Distance0, out float _Distance1 )
		{
			_Distance0 = _Distance1 = 0.0f;

			Vector3	DC = _Position - _SphereCenter;	// Center => Position
			double	a = Vector3.Dot( _Direction, _Direction );
			double	b = Vector3.Dot( _Direction, DC );
			double	c = Vector3.Dot( DC, DC ) - _SphereRadius*_SphereRadius;
			double	Delta = b*b - a*c;
			if ( Delta < 0.0f )
				return false;

			Delta = Math.Sqrt( Delta );

			a = 1.0 / a;
			_Distance0 = (float) ((-b - Delta) * a);
			_Distance1 = (float) ((-b + Delta) * a);

			return true;
		}

		/// <summary>
		/// Computes only the forward intersection of a ray with a sphere, returns no intersection even if there is one but standing behind origin
		/// </summary>
		/// <param name="_Position"></param>
		/// <param name="_Direction"></param>
		/// <param name="_SphereCenter"></param>
		/// <param name="_SphereRadius"></param>
		/// <param name="_Distance"></param>
		/// <returns></returns>
		protected bool	ComputeForwardSphereIntersection( ref Vector3 _Position, ref Vector3 _Direction, ref Vector3 _SphereCenter, float _SphereRadius, out float _Distance )
		{
			_Distance = -1.0f;

			float	t0, t1;
			if ( !ComputeSphereIntersection( ref _Position, ref _Direction, ref _SphereCenter, _SphereRadius, out t0, out t1 ) )
				return false;	// No intersection anyway !

			if ( t1 < 0.0f )
				return false;	// Both intersections stand behind us...
			
			if ( t0 < 0.0f )
				_Distance = t1;
			else
				_Distance = t0;

			return true;
		}

		/// <summary>
		/// Reprojects a point on the atmosphere by casting it in the Sun's direction
		/// If the Sun is on the same side as the point then the point stays here,
		///  otherwise it traverses the atmosphere until it hits again
		/// </summary>
		/// <param name="_Position"></param>
		/// <param name="_Direction"></param>
		/// <param name="_SphereCenter"></param>
		/// <param name="_SphereRadius"></param>
		/// <param name="_Distance"></param>
		/// <returns></returns>
		protected void	ReprojectToSunHemisphere( ref Vector3 _Position, ref Vector3 _Direction, ref Vector3 _SphereCenter, float _SphereRadius, out float _Distance )
		{
 			_Distance = -1.0f;

			float	t0, t1;
			if ( !ComputeSphereIntersection( ref _Position, ref _Direction, ref _SphereCenter, _SphereRadius, out t0, out t1 ) )
			{
				//throw new Exception( "We should have an intersection here !" );
				_Distance = 0.0f;	// Tant pis...
				return;
			}

			_Distance = Math.Abs( t0 ) < Math.Abs( t1 ) ? t0 : t1;

// 			if ( Math.Abs( t0 ) < 1.0f )
// 				_Distance = t1;		// Follow the Sun
// 			else if ( Math.Abs( t1 ) < 1.0f )
// 				_Distance = 0.0f;	// We're already as close as possible
// 			else
// 				throw new Exception( "One of the 2 intersections should be 0 !" );
		}

		/// <summary>
		/// Computes the vector tangent to the sphere given two directions, one of them pointing above the horizon, the other one pointing below
		/// </summary>
		/// <param name="_Position"></param>
		/// <param name="_Direction0">Direction pointing ABOVE the horizon</param>
		/// <param name="_Direction1">Directin pointing BELOW the horizon</param>
		/// <param name="_SphereCenter"></param>
		/// <param name="_SphereRadius"></param>
		/// <param name="_Tangent"></param>
		/// <returns></returns>
		protected bool	ComputeTangentVector( ref Vector3 _Position, ref Vector3 _Direction0, ref Vector3 _Direction1, ref Vector3 _SphereCenter, float _SphereRadius, out Vector3 _Tangent )
		{
			_Tangent = Vector3.Zero;

			Vector3	V = _Direction0;
			Vector3	DeltaV = _Direction1 - V;
			Vector3	D = _Position - _SphereCenter;
			double	H = Vector3.Dot(D,D) - _SphereRadius*_SphereRadius;

			double	VD = Vector3.Dot( V, D );
			double	dVD = Vector3.Dot( DeltaV, D );
			double	c = VD*VD - H * Vector3.Dot( V, V );
			double	b = VD*dVD - H * Vector3.Dot( V, DeltaV );
			double	a = dVD*dVD - H * Vector3.Dot( DeltaV, DeltaV );

			double	Delta = b*b - a*c;
			if ( Delta < 0.0 )
				return false;

			Delta = Math.Sqrt( Delta );
			a = 1.0 / a;

			double	t0 = (-b-Delta) * a;
			double	t1 = (-b+Delta) * a;

			float	t = -1.0f;
			if ( t0 >= 0.0f && t0 <= 1.0f )
				t = (float) t0;
			if ( t1 >= 0.0f && t1 <= 1.0f )
				t = (float) t1;
			
			if ( t < 0.0f )
				return false;	// No intersection in [0,1]

			_Tangent = V + t * DeltaV;

			return true;
		}

		#endregion

		#region Noise Computation

		/// <summary>
		/// Creates a 3D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateNoiseTexture( int _NoiseIndex )
		{
			const int	NOISE_SIZE = 16;
//			const float	GLOBAL_SCALE = 2.0f;

			// Build the volume filled with noise
			float[,,]	Noise = new float[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];

			// Read noise from resources
			byte[][]	NoiseTextures = new byte[][]
			{
				Properties.Resources.packednoise_half_16cubed_mips_00,
				Properties.Resources.packednoise_half_16cubed_mips_01,
				Properties.Resources.packednoise_half_16cubed_mips_02,
				Properties.Resources.packednoise_half_16cubed_mips_03,
			};

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( NoiseTextures[_NoiseIndex] );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			int	XS, YS, ZS, PS;
			XS = Reader.ReadInt32();
			YS = Reader.ReadInt32();
			ZS = Reader.ReadInt32();
			PS = Reader.ReadInt32();

			Half	Temp = new Half();
			for ( int Z=0; Z < NOISE_SIZE; Z++ )
				for ( int Y=0; Y < NOISE_SIZE; Y++ )
					for ( int X=0; X < NOISE_SIZE; X++ )
					{
						Temp.RawValue = Reader.ReadUInt16();
						Reader.ReadUInt16();
						Reader.ReadUInt16();
						Reader.ReadUInt16();
						Noise[X,Y,Z] = (float) Temp;
					}
			Reader.Dispose();
			Stream.Dispose();

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_SIZE, NOISE_SIZE, NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{																			// (XYZ)
					_Color.X = Noise[_X,_Y,_Z];												// (000)
					_Color.Y = Noise[_X,(_Y+1) & (NOISE_SIZE-1),_Z];						// (010)
					_Color.Z = Noise[_X,_Y,(_Z+1) & (NOISE_SIZE-1)];						// (001)
					_Color.W = Noise[_X,(_Y+1) & (NOISE_SIZE-1),(_Z+1) & (NOISE_SIZE-1)];	// (011)
				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}

		#endregion

		#region Phase Function

		/// <summary>
		/// Builds the table containing the Mie phase function
		/// </summary>
		protected void	BuildPhaseFunction()
		{
			Atmospheric.PhaseFunction	PhaseFunction = new Atmospheric.PhaseFunction();
			PhaseFunction.Init( Atmospheric.CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION, 5.0f * (float) Math.PI / 180.0f, (float) Math.PI, PHASE_TEXTURE_SIZE );

			using ( Image<PF_R16F> Image = new Image<PF_R16F>( m_Device, "PhaseImage", PHASE_TEXTURE_SIZE, 1,
				( int _X, int _Y, ref Vector4 _Color ) =>
				{	// Third table encodes the last coefficient
					_Color.X = (float) PhaseFunction.GetPhaseFactor( (float) Math.PI * _X / (PHASE_TEXTURE_SIZE-1) );
				}, 1 ) )
			{
				m_TexturePhase = ToDispose( new Texture2D<PF_R16F>( m_Device, "Phase", Image ) );
			}

			// Build the convolved phase function
			double[]	PhaseConvolved = new double[PHASE_TEXTURE_SIZE];
			double		DeltaAngle = Math.PI / PHASE_TEXTURE_SIZE;
			for ( int AngleIndex0=0; AngleIndex0 < PHASE_TEXTURE_SIZE; AngleIndex0++ )
			{
				float	Angle0 = (float) Math.PI * AngleIndex0 / (PHASE_TEXTURE_SIZE-1);
				double	Phase0 = PhaseFunction.GetPhaseFactor( Angle0 );

				double	Convolution = 0.0;
				for ( int AngleIndex1=0; AngleIndex1 < PHASE_TEXTURE_SIZE; AngleIndex1++ )
				{
					float	Angle1 = (float) Math.PI * AngleIndex1 / (PHASE_TEXTURE_SIZE-1);
					Convolution += Phase0 * PhaseFunction.GetPhaseFactor( Angle0 - Angle1 );
				}

				PhaseConvolved[AngleIndex0] = (float) (DeltaAngle * Convolution);
			}

			using ( Image<PF_R16F> Image = new Image<PF_R16F>( m_Device, "PhaseImage", PHASE_TEXTURE_SIZE, 1,
				( int _X, int _Y, ref Vector4 _Color ) =>
				{	// Third table encodes the last coefficient
					_Color.X = (float) PhaseConvolved[_X];
				}, 1 ) )
			{
				m_TexturePhaseConvolved = ToDispose( new Texture2D<PF_R16F>( m_Device, "PhaseConvolved", Image ) );
			}
		}

		#endregion

		#region IComparer<CloudLayer> Members

		public int Compare( RenderTechniqueCloudLayer.CloudLayer x, RenderTechniqueCloudLayer.CloudLayer y )
		{
			if ( x.Altitude < y.Altitude )
				return 1;
			else if ( x.Altitude > y.Altitude )
				return -1;
			else
				return 0;
		}

		#endregion

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IShadowMapSupport	I = _Interface as IShadowMapSupport;
			I.ShadowAngularBounds = m_ShadowMapAngularBounds;
			I.ShadowInvAngularBounds = m_ShadowMapInvAngularBounds;
			I.Shadow2World = m_ShadowMap2World;
			I.World2Shadow = m_World2ShadowMap;
			I.ShadowAltitudesMinKm = m_ShadowMapAltitudesMin;
			I.ShadowAltitudesMaxKm = m_ShadowMapAltitudesMax;
			I.ShadowMap = m_ShadowMap;
		}

		#endregion

		#endregion
	}
}
