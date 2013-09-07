#define FILE_PROVIDER_IS_DISK
#define LOAD_FROM_FBX
//#define RECORD_CAMERA
//#define REPLAY_CAMERA

// Clips definitions
#define CLIP_WIND_PARTICLES
//#define CLIP_BOX_CLOUD
//#define CLIP_NEBULA
//#define CLIP_FERRO_FLUID

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
	/// This setups and encapsulates an advanced HDR renderer with post-processing
	/// </summary>
	public class RendererSetupDemo : Component, IShaderInterfaceProvider, IFileLoader, SharpDX.D3DCompiler.Include
	{
		#region CONSTANTS

		public const float		DEPTH_BUFFER_INFINITY = 10000.0f;
		public const float		GLOBAL_LIGHT_INTENSITY_MULTIPLIER = 10.0f;

		#endregion

		#region NESTED TYPES

		public class	UnsupportedMultiSamplesCountException : Exception
		{
			public UnsupportedMultiSamplesCountException( string _Message ) : base( _Message ) {}
			public UnsupportedMultiSamplesCountException( string _Message, Exception _e ) : base( _Message, _e ) {}
		}

		#region Clips

		public abstract class	Clip
		{
			#region FIELDS

			protected RendererSetupDemo	m_Owner = null;

			#endregion
	
			#region METHODS

			public Clip( RendererSetupDemo _Owner )
			{
				m_Owner = _Owner;
			}

			public abstract void		Enable( bool _bEnable );

			protected T	ToDispose<T>( T _Item ) where T : IDisposable
			{
				m_Owner.ToDispose( _Item );

				return _Item;
			}

			#endregion
		}

		public class	ClipBoxCloud : Clip
		{
			#region FIELDS

			#endregion

			#region PROPERTIES

			#endregion

			#region METHODS

			public ClipBoxCloud( RendererSetupDemo _Owner ) : base( _Owner )
			{

			}

			public override void Enable( bool _bEnable )
			{
				if ( _bEnable )
				{
					m_Owner.Camera = null;
					m_Owner.LightKey = null;
					m_Owner.LightRim = null;
					m_Owner.LightFill = null;
				}

				m_Owner.m_RenderTechniqueInk.Enabled = _bEnable;
				m_Owner.m_RenderTechniqueInk.LightIntensity = 0.25f;
				m_Owner.m_RenderTechniqueMP.Enabled = _bEnable;
			}

			#endregion
		}

		public class	ClipWindParticles : Clip
		{
			#region FIELDS

			protected Scene			m_Scene = null;

			protected Scene.Camera	m_NodeCamera = null;
			protected Scene.Light	m_NodeLightKey = null;
			protected Scene.Light	m_NodeLightRim = null;
			protected Scene.Light	m_NodeLightFill = null;

			#endregion

			#region PROPERTIES

			public Scene		Scene	{ get { return m_Scene; } }

			#endregion

			#region METHODS

			public ClipWindParticles( RendererSetupDemo _Owner ) : base( _Owner )
			{
				//////////////////////////////////////////////////////////////////////////
				// Load the main scene
				MaterialMap	MMap = new MaterialMap();
				MMap.RegisterMapper( ( Cirrus.Scene.MaterialParameters _MaterialParameters ) =>
					{
						if ( _MaterialParameters.ShaderURL == "Phong" || _MaterialParameters.ShaderURL == "Blinn" )
							return m_Owner.m_RenderTechniqueMainScene;

						return null;	// Unsupported...
					} );
				m_Scene = m_Owner.LoadScene( new System.IO.FileInfo( "Meshes/WaterColour/Stage.FBX" ), "Main Scene", MMap, 0.01f );

				// Post-process some objects
				Scene.Mesh		NodeParticlesBox = m_Scene.FindMesh( "BoxParticles", true );
				m_Owner.m_RenderTechniqueWindParticles.SetParticlesBoxFromMesh( NodeParticlesBox );

				// Disable shadow casting for the floor
				m_Scene.FindMesh( "Plane01", true ).CastShadow = false;

				// Retrieve cameras & lights
				m_NodeCamera = m_Scene.FindNode( "Camera001", true ) as Scene.Camera;
				m_NodeLightKey = m_Scene.FindNode( "SpotKey", true ) as Scene.Light;
				m_NodeLightRim = m_Scene.FindNode( "SpotRim", true ) as Scene.Light;
				m_NodeLightFill = m_Scene.FindNode( "SpotFill", true ) as Scene.Light;

				// Use the application's aspect ratio
				m_NodeCamera.AspectRatio = m_Owner.m_CameraAspectRatio;
			}

			public override void Enable( bool _bEnable )
			{
				if ( _bEnable )
				{
					m_Owner.Camera = m_NodeCamera.InternalCamera;
					m_Owner.LightKey = m_NodeLightKey.InternalLightSpot;
					m_Owner.m_bLightKeyCastShadow = m_NodeLightKey.CastShadow;
					m_Owner.LightRim = m_NodeLightRim.InternalLightSpot;
					m_Owner.m_bLightRimCastShadow = m_NodeLightRim.CastShadow;
					m_Owner.LightFill = m_NodeLightFill.InternalLightSpot;
					m_Owner.m_bLightFillCastShadow = m_NodeLightFill.CastShadow;

					m_Owner.m_RenderTechniqueMainScene.Scene = m_Scene;
				}
				else
				{
					m_Owner.Camera = null;
					m_Owner.LightKey = null;
					m_Owner.LightRim = null;
					m_Owner.LightFill = null;

					m_Owner.m_RenderTechniqueMainScene.Scene = null;
				}

				m_Owner.m_RenderTechniqueMainScene.Enabled = _bEnable;
				m_Owner.m_RenderTechniqueWindParticles.Enabled = _bEnable;
				m_Owner.m_PPDOFBlur.Enabled = _bEnable;
				m_Owner.m_PPMotionBlur.Enabled = _bEnable;
			}

			#endregion
		}

		public class	ClipFerroFluid : Clip
		{
			#region FIELDS

			protected Scene			m_Scene = null;

			protected Scene.Camera	m_NodeCamera = null;
			protected Scene.Light	m_NodeLightKey = null;
			protected Scene.Light	m_NodeLightRim = null;
			protected Scene.Light	m_NodeLightFill = null;

			#endregion

			#region PROPERTIES

			public Scene		Scene	{ get { return m_Scene; } }

			#endregion

			#region METHODS

			public ClipFerroFluid( RendererSetupDemo _Owner ) : base( _Owner )
			{
// 				//////////////////////////////////////////////////////////////////////////
// 				// Load the main scene
// 				MaterialMap	MMap = new MaterialMap();
// 				MMap.RegisterMapper( ( Cirrus.Scene.MaterialParameters _MaterialParameters ) =>
// 					{
// 						if ( _MaterialParameters.ShaderURL == "Phong" || _MaterialParameters.ShaderURL == "Blinn" )
// 							return m_Owner.m_RenderTechniqueMainScene;
// 
// 						return null;	// Unsupported...
// 					} );
// 				m_Owner.LoadScene( new System.IO.FileInfo( "Meshes/WaterColour/Stage.FBX" ), "Main Scene", MMap, 0.01f );
// 
// 				// Post-process some objects
// 				Scene.Mesh		NodeParticlesBox = m_Scene.FindMesh( "BoxParticles", true );
// 				m_Owner.m_RenderTechniqueWindParticles.SetParticlesBoxFromMesh( NodeParticlesBox );
// 
// 				// Disable shadow casting for the floor
// 				m_Scene.FindMesh( "Plane01", true ).CastShadow = false;
// 
// 				// Retrieve cameras & lights
// 				m_NodeCamera = m_Scene.FindNode( "Camera001", true ) as Scene.Camera;
// 				m_NodeLightKey = m_Scene.FindNode( "SpotKey", true ) as Scene.Light;
// 				m_NodeLightRim = m_Scene.FindNode( "SpotRim", true ) as Scene.Light;
// 				m_NodeLightFill = m_Scene.FindNode( "SpotFill", true ) as Scene.Light;
// 
// 				// Use the application's aspect ratio
// 				m_NodeCamera.AspectRatio = m_Owner.m_CameraAspectRatio;
			}

			public override void Enable( bool _bEnable )
			{
				if ( _bEnable )
				{
// 					m_Owner.Camera = m_NodeCamera.InternalCamera;
// 					m_Owner.LightKey = m_NodeLightKey.InternalLightSpot;
// 					m_Owner.m_bLightKeyCastShadow = m_NodeLightKey.CastShadow;
// 					m_Owner.LightRim = m_NodeLightRim.InternalLightSpot;
// 					m_Owner.m_bLightRimCastShadow = m_NodeLightRim.CastShadow;
// 					m_Owner.LightFill = m_NodeLightFill.InternalLightSpot;
// 					m_Owner.m_bLightFillCastShadow = m_NodeLightFill.CastShadow;
				}
			}

			#endregion
		}

		public class	ClipNebula : Clip
		{
			#region FIELDS

			protected Scene.Camera	m_NodeCamera = null;
			protected Scene.Light	m_NodeLightKey = null;
			protected Scene.Light	m_NodeLightRim = null;
			protected Scene.Light	m_NodeLightFill = null;

			#endregion

			#region PROPERTIES

			#endregion

			#region METHODS

			public ClipNebula( RendererSetupDemo _Owner ) : base( _Owner )
			{
			}

			public override void Enable( bool _bEnable )
			{
				if ( _bEnable )
				{
// 					m_Owner.Camera = m_NodeCamera.InternalCamera;
// 					m_Owner.LightKey = m_NodeLightKey.InternalLightSpot;
// 					m_Owner.m_bLightKeyCastShadow = m_NodeLightKey.CastShadow;
// 					m_Owner.LightRim = m_NodeLightRim.InternalLightSpot;
// 					m_Owner.m_bLightRimCastShadow = m_NodeLightRim.CastShadow;
// 					m_Owner.LightFill = m_NodeLightFill.InternalLightSpot;
// 					m_Owner.m_bLightFillCastShadow = m_NodeLightFill.CastShadow;
				}

				m_Owner.TechniqueNebula.Enabled = _bEnable;
			}

			#endregion
		}

		#endregion

		#region Shader Interfaces

		/// <summary>
		/// Deferred Shading Support
		/// </summary>
		protected class	IDeferredRendering : ShaderInterfaceBase
		{
			[Semantic( "GBUFFER_TEX0" )]
			public RenderTarget<PF_RGBA16F>	GBuffer0	{ set { SetResource( "GBUFFER_TEX0", value ); } }
			[Semantic( "GBUFFER_TEX1" )]
			public RenderTarget<PF_RGBA16F>	GBuffer1	{ set { SetResource( "GBUFFER_TEX1", value ); } }
			[Semantic( "GBUFFER_TEX2" )]
			public RenderTarget<PF_RGBA16F>	GBuffer2	{ set { SetResource( "GBUFFER_TEX2", value ); } }
			[Semantic( "GBUFFER_TEX3" )]
			public RenderTarget<PF_RGBA16F>	GBuffer3	{ set { SetResource( "GBUFFER_TEX3", value ); } }
			[Semantic( "DEPTH_BUFFER" )]
			public DepthStencil<PF_D32>		DepthBuffer	{ set { SetResource( "DEPTH_BUFFER", value ); } }
			[Semantic( "GBUFFER_SIZE" )]
			public Vector3					GBufferSize		{ set { SetVector( "GBUFFER_SIZE", value ); } }
			[Semantic( "GBUFFER_INV_SIZE" )]
			public Vector3					GBufferInvSize	{ set { SetVector( "GBUFFER_INV_SIZE", value ); } }
		}

		protected class	INoise3D : ShaderInterfaceBase
		{
			[Semantic( "NOISE3D_TEX0" )]
			public Texture3D<PF_RGBA16F>	Noise0		{ set { SetResource( "NOISE3D_TEX0", value ); } }
			[Semantic( "NOISE3D_TEX1" )]
			public Texture3D<PF_RGBA16F>	Noise1		{ set { SetResource( "NOISE3D_TEX1", value ); } }
			[Semantic( "NOISE3D_TEX2" )]
			public Texture3D<PF_RGBA16F>	Noise2		{ set { SetResource( "NOISE3D_TEX2", value ); } }
			[Semantic( "NOISE3D_TEX3" )]
			public Texture3D<PF_RGBA16F>	Noise3		{ set { SetResource( "NOISE3D_TEX3", value ); } }
		}

		/// <summary>
		/// This interface is for 3 points lighting with 3 spot lights (cf. http://www.3drender.com/light/3point.html)
		/// </summary>
		protected class	ILightKeyRimFill : ShaderInterfaceBase
		{
			[Semantic( "LIGHT_KEY_POSITION" )]
			public Vector3					KeyPosition		{ set { SetVector( "LIGHT_KEY_POSITION", value ); } }
			[Semantic( "LIGHT_KEY_DIRECTION" )]
			public Vector3					KeyDirection	{ set { SetVector( "LIGHT_KEY_DIRECTION", value ); } }
			[Semantic( "LIGHT_KEY_COLOR" )]
			public Vector4					KeyColor		{ set { SetVector( "LIGHT_KEY_COLOR", value ); } }
			[Semantic( "LIGHT_KEY_DATA" )]
			public Vector4					KeyData			{ set { SetVector( "LIGHT_KEY_DATA", value ); } }
			[Semantic( "LIGHT_KEY_DATA2" )]
			public Vector4					KeyData2		{ set { SetVector( "LIGHT_KEY_DATA2", value ); } }

			[Semantic( "LIGHT_RIM_POSITION" )]
			public Vector3					RimPosition		{ set { SetVector( "LIGHT_RIM_POSITION", value ); } }
			[Semantic( "LIGHT_RIM_DIRECTION" )]
			public Vector3					RimDirection	{ set { SetVector( "LIGHT_RIM_DIRECTION", value ); } }
			[Semantic( "LIGHT_RIM_COLOR" )]
			public Vector4					RimColor		{ set { SetVector( "LIGHT_RIM_COLOR", value ); } }
			[Semantic( "LIGHT_RIM_DATA" )]
			public Vector4					RimData			{ set { SetVector( "LIGHT_RIM_DATA", value ); } }
			[Semantic( "LIGHT_RIM_DATA2" )]
			public Vector4					RimData2		{ set { SetVector( "LIGHT_RIM_DATA2", value ); } }

			[Semantic( "LIGHT_FILL_POSITION" )]
			public Vector3					FillPosition	{ set { SetVector( "LIGHT_FILL_POSITION", value ); } }
			[Semantic( "LIGHT_FILL_DIRECTION" )]
			public Vector3					FillDirection	{ set { SetVector( "LIGHT_FILL_DIRECTION", value ); } }
			[Semantic( "LIGHT_FILL_COLOR" )]
			public Vector4					FillColor		{ set { SetVector( "LIGHT_FILL_COLOR", value ); } }
			[Semantic( "LIGHT_FILL_DATA" )]
			public Vector4					FillData		{ set { SetVector( "LIGHT_FILL_DATA", value ); } }
			[Semantic( "LIGHT_FILL_DATA2" )]
			public Vector4					FillData2		{ set { SetVector( "LIGHT_FILL_DATA2", value ); } }
		}

		#endregion

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Main renderer & techniques
		protected Renderer							m_Renderer = null;
		protected RenderTechniqueDepthPass			m_DepthPass = null;
		protected Demo.RenderTechniqueShadowMap		m_ShadowMapping = null;
		protected RenderTechniqueMainScene			m_RenderTechniqueMainScene = null;
		protected RenderTechniqueCaustics2			m_RenderTechniqueInk = null;	// The advanced render technique for caustics
		protected RenderTechniqueMegaParticles		m_RenderTechniqueMP = null;
		protected RenderTechniqueWindParticles		m_RenderTechniqueWindParticles = null;
		protected RenderTechniqueNebula				m_RenderTechniqueNebula = null;

		// Post-processes
		protected PostProcessAntiAliasing			m_PPAntiAliasing = null;
		protected PostProcessFog					m_PPVolumetricFog = null;
		protected PostProcessMotionBlur				m_PPMotionBlur = null;
		protected PostProcessBlur					m_PPDOFBlur = null;
		protected PostProcessColorimetry			m_PPColorimetry = null;

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected RenderTarget<PF_R32F>				m_MSAADepthTarget = null;	// MSAA Depth target used for anti-aliasing	

		// GBuffers at normal resolution
 		protected RenderTarget<PF_RGBA16F>			m_GeometryBuffer = null;	// The geometry buffer that will store the normals, depth and surface roughness
 		protected RenderTarget<PF_RGBA16F>			m_MaterialBuffer = null;	// The material buffer that will diffuse & specular albedos of materials
 		protected RenderTarget<PF_RGBA16F>			m_MaterialBuffer2 = null;	// The 2nd material buffer that will be swapped for post processing passes
 		protected RenderTarget<PF_RGBA16F>			m_EmissiveBuffer = null;	// The emissive buffer that will store the emissive/unlit object colors and the global extinction factor
 		protected RenderTarget<PF_RGBA16F>			m_VelocityBuffer = null;	// The velocity buffer that will store the 2D velocities in PROJECTIVE space
 		protected RenderTarget<PF_RGBA16F>			m_VelocityBuffer2 = null;	// The 2nd velocity buffer that will be swapped for motion blur
		protected Vector3							m_GBufferSize;
		protected Vector3							m_GBufferInvSize;

		// The 16x16x16 noise textures
		protected Texture3D<PF_RGBA16F>[]			m_NoiseTextures = new Texture3D<PF_RGBA16F>[4];

		protected Utility.TextureLoader				m_TextureLoader = null;
		protected SceneTextureProvider				m_SceneTextureProvider = null;

		//////////////////////////////////////////////////////////////////////////
		// Primitives & Objects
		protected Helpers.ScreenQuad				m_Quad = null;				// Screen quad for post-processing

		protected Camera							m_DefaultCamera = null;

		//////////////////////////////////////////////////////////////////////////
		// Attributes
		protected float								m_Time = 0.0f;
		protected float								m_CameraAspectRatio = 1.0f;
		protected Camera							m_Camera = null;
		protected SpotLight							m_LightKey = null;
		protected bool								m_bLightKeyCastShadow = true;
		protected SpotLight							m_LightRim = null;
		protected bool								m_bLightRimCastShadow = true;
		protected SpotLight							m_LightFill = null;
		protected bool								m_bLightFillCastShadow = true;


		//////////////////////////////////////////////////////////////////////////
		// Clips
		protected List<Clip>						m_Clips = new List<Clip>();
		protected int								m_CurrentClipIndex = 0;

		#endregion

		#region PROPERTIES

		public Renderer						Renderer					{ get { return m_Renderer; } }

		public float						Time						{ get { return m_Time; } set { m_Time = value; } }
		public Camera						Camera
		{
			get { return m_Camera; }
			set
			{
				if ( value == m_Camera )
					return;	// No change...

				m_Camera = value != null ? value : m_DefaultCamera;

				// Notify
				if ( CameraChanged != null )
					CameraChanged( this, EventArgs.Empty );
			}
		}
		public Camera						DefaultCamera				{ get { return m_DefaultCamera; } }
		public SpotLight					LightKey					{ get { return m_LightKey; } set { m_LightKey = value; m_bLightKeyCastShadow &= value != null; } }
		public bool							LightKeyCastShadow			{ get { return m_bLightKeyCastShadow; } }
		public SpotLight					LightRim					{ get { return m_LightRim; } set { m_LightRim = value; m_bLightRimCastShadow &= value != null; } }
		public bool							LightRimCastShadow			{ get { return m_bLightRimCastShadow; } }
		public SpotLight					LightFill					{ get { return m_LightFill; } set { m_LightFill = value; m_bLightFillCastShadow &= value != null; } }
		public bool							LightFillCastShadow			{ get { return m_bLightFillCastShadow; } }

		public RenderTechniqueDepthPass		DepthPass					{ get { return m_DepthPass; } }
		public Demo.RenderTechniqueShadowMap	ShadowMapping			{ get { return m_ShadowMapping; } }
		public RenderTechniqueMainScene		MainScene					{ get { return m_RenderTechniqueMainScene; } }
		public RenderTechniqueCaustics2		TechniqueInk				{ get { return m_RenderTechniqueInk; } }
		public RenderTechniqueMegaParticles	TechniqueMegaParticles		{ get { return m_RenderTechniqueMP; } }
		public RenderTechniqueWindParticles	TechniqueWindParticles		{ get { return m_RenderTechniqueWindParticles; } }
		public RenderTechniqueNebula		TechniqueNebula				{ get { return m_RenderTechniqueNebula; } }

		public PostProcessAntiAliasing		PPAntiAliasing				{ get { return m_PPAntiAliasing; } }
		public PostProcessFog				PPVolumetricFog				{ get { return m_PPVolumetricFog; } }
		public PostProcessBlur				PPDOFBlur					{ get { return m_PPDOFBlur; } }
		public PostProcessMotionBlur		PPMotionBlur				{ get { return m_PPMotionBlur; } }
		public PostProcessColorimetry		PPColorimetry				{ get { return m_PPColorimetry; } }

		public RenderTarget<PF_R32F>		MSAADepthTarget				{ get { return m_MSAADepthTarget; } }
		public RenderTarget<PF_RGBA16F>		GeometryBuffer				{ get { return m_GeometryBuffer; } }
		public RenderTarget<PF_RGBA16F>		MaterialBuffer				{ get { return m_MaterialBuffer; } }
		public RenderTarget<PF_RGBA16F>		MaterialBuffer2				{ get { return m_MaterialBuffer2; } }
		public RenderTarget<PF_RGBA16F>		EmissiveBuffer				{ get { return m_EmissiveBuffer; } }
		public RenderTarget<PF_RGBA16F>		VelocityBuffer				{ get { return m_VelocityBuffer; } }
		public RenderTarget<PF_RGBA16F>		VelocityBuffer2				{ get { return m_VelocityBuffer2; } }
		public Vector3						GBufferSize					{ get { return m_GBufferSize; } }
		public Vector3						GBufferInvSize				{ get { return m_GBufferInvSize; } }

		/// <summary>
		/// Occurs when the camera changes
		/// </summary>
		public event EventHandler			CameraChanged;

		public Utility.TextureLoader		TextureLoader				{ get { return m_TextureLoader; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Setups a default renderer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_AntiAliasingMultiSamplesCount">The amount of multisamples used for antialiasing</param>
		/// <param name="_ShadowMapSize"></param>
		public	RendererSetupDemo( Device _Device, string _Name, int _AntiAliasingMultiSamplesCount, float _CameraFOV, float _CameraAspectRatio, float _CameraNear, float _CameraFar ) : base( _Device, _Name )
		{
			m_Renderer = ToDispose( new Renderer( m_Device, m_Name ) );

			//////////////////////////////////////////////////////////////////////////
			// Register shader interfaces
// 			m_Device.RegisterShaderInterfaceProvider( typeof(ILinearToneMapping), this );	// Register the ILinearToneMapping interface
// 			m_Device.RegisterShaderInterfaceProvider( typeof(IDirectionalLight), this );	// Register the IDirectionalLight interface
// 			m_Device.RegisterShaderInterfaceProvider( typeof(IDirectionalLight2), this );	// Register the IDirectionalLight2 interface
			m_Device.RegisterShaderInterfaceProvider( typeof(ICamera), this );				// Register the ICamera interface
			m_Device.DeclareShaderInterface( typeof(IDeferredRendering) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IDeferredRendering), this );	// Register the IDeferredRendering interface
			m_Device.DeclareShaderInterface( typeof(INoise3D) );
			m_Device.RegisterShaderInterfaceProvider( typeof(INoise3D), this );				// Register the INoise3D interface
			m_Device.DeclareShaderInterface( typeof(ILightKeyRimFill) );
			m_Device.RegisterShaderInterfaceProvider( typeof(ILightKeyRimFill), this );		// Register the ILightKeyRimFill interface

			//////////////////////////////////////////////////////////////////////////
			// Create rendering buffers
			int	DefaultWidth = m_Device.DefaultRenderTarget.Width;
			int	DefaultHeight = m_Device.DefaultRenderTarget.Height;

			// Create the MSAA depth stencil
			if ( _AntiAliasingMultiSamplesCount > 0 )//&& m_Device.SupportsShaderModel( ShaderModel.SM4_1 ) )
				try
				{
					m_MSAADepthTarget = ToDispose( new RenderTarget<PF_R32F>( m_Device, "MSAA Depth", DefaultWidth, DefaultHeight, 1, _AntiAliasingMultiSamplesCount ) );
					if ( (m_MSAADepthTarget.Support & FormatSupport.MultisampleLoad) == 0 )
						throw new UnsupportedMultiSamplesCountException( "PF_R32F MSAA does not support Load() instructions !" );
				}
				catch ( Exception _e )
				{
					throw new UnsupportedMultiSamplesCountException( "An error occurred while creating the MSAA depth stencil buffer !", _e );
				}

			// Build screen resolution targets
			m_GeometryBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "GeometryBuffer", DefaultWidth, DefaultHeight, 1 ) );
			m_MaterialBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "MaterialBuffer0", DefaultWidth, DefaultHeight, 1 ) );
			m_MaterialBuffer2 = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "MaterialBuffer1", DefaultWidth, DefaultHeight, 1 ) );
			m_EmissiveBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "EmissiveBuffer", DefaultWidth, DefaultHeight, 1 ) );
			m_VelocityBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "VelocityBuffer0", DefaultWidth, DefaultHeight, 1 ) );
			m_VelocityBuffer2 = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "VelocityBuffer1", DefaultWidth, DefaultHeight, 1 ) );
			m_GBufferSize = new Vector3( DefaultWidth, DefaultHeight, 0.0f );
			m_GBufferInvSize = new Vector3( 1.0f / DefaultWidth, 1.0f / DefaultHeight, 0.0f );

			//////////////////////////////////////////////////////////////////////////
			// Create the pipelines
			Pipeline	Depth = ToDispose( new Pipeline( m_Device, "Depth Pass Pipeline", Pipeline.TYPE.DEPTH_PASS ) );
			m_Renderer.AddPipeline( Depth );

			Pipeline	Shadow = ToDispose( new Pipeline( m_Device, "Shadow Pipeline", Pipeline.TYPE.SHADOW_MAPPING ) );
			m_Renderer.AddPipeline( Shadow );

			Pipeline	Main = ToDispose( new Pipeline( m_Device, "Main Pipeline", Pipeline.TYPE.MAIN_RENDERING ) );
			m_Renderer.AddPipeline( Main );
			Main.RenderingStart += new Pipeline.PipelineRenderingEventHandler( MainPipeline_RenderingStart );

			Pipeline	DeferredLighting = ToDispose( new Pipeline( m_Device, "Deferred Lighting Pipeline", Pipeline.TYPE.DEFERRED_LIGHTING ) );
			m_Renderer.AddPipeline( DeferredLighting );

			Pipeline	Emissive = ToDispose( new Pipeline( m_Device, "Emissive Pipeline", Pipeline.TYPE.EMISSIVE_UNLIT ) );
			m_Renderer.AddPipeline( Emissive );
			Emissive.RenderingStart += new Pipeline.PipelineRenderingEventHandler( EmissivePipeline_RenderingStart );

			Pipeline	PostProcessing = ToDispose( new Pipeline( m_Device, "Post-Processing Pipeline", Pipeline.TYPE.POST_PROCESSING ) );
			m_Renderer.AddPipeline( PostProcessing );

			//////////////////////////////////////////////////////////////////////////
			// Create the depth pass render technique
			m_DepthPass = ToDispose( new RenderTechniqueDepthPass( this, "Depth Pass" ) );
			Depth.AddTechnique( m_DepthPass );

			//////////////////////////////////////////////////////////////////////////
			// Create the shadow mapping technique
			m_ShadowMapping = ToDispose( new Demo.RenderTechniqueShadowMap( this, "Shadow Mapping" ) );
			Shadow.AddTechnique( m_ShadowMapping );

			//////////////////////////////////////////////////////////////////////////
			// Create the render techniques for drawing a scene into the deferred render targets
#if CLIP_BOX_CLOUD
			m_RenderTechniqueInk = ToDispose( new RenderTechniqueCaustics2( this, "Caustics Render Technique" ) );
			m_RenderTechniqueInk.LightIntensity = 0.25f;
			Main.InsertTechnique( 0, m_RenderTechniqueInk );	// Insert our technique at the beginning
//			m_DepthPass.AddRenderable( m_RenderTechniqueInk );

			// Mega particles effect
			m_RenderTechniqueMP = ToDispose( new RenderTechniqueMegaParticles( this, "Mega Particles" ) );
			Main.AddTechnique( m_RenderTechniqueMP );
#endif

			// Standard Phong scene display
			m_RenderTechniqueMainScene = ToDispose( new RenderTechniqueMainScene( this, "Main Scene" ) );
			Main.AddTechnique( m_RenderTechniqueMainScene );
			m_DepthPass.AddRenderable( m_RenderTechniqueMainScene );
			m_ShadowMapping.AddRenderable( m_RenderTechniqueMainScene );

#if CLIP_WIND_PARTICLES
			// Wind particles effect
			m_RenderTechniqueWindParticles = ToDispose( new RenderTechniqueWindParticles( this, "Wind Particles" ) );
			Main.AddTechnique( m_RenderTechniqueWindParticles );
#endif

#if CLIP_NEBULA
			// Nebula effect
			m_RenderTechniqueNebula = ToDispose( new RenderTechniqueNebula( this, "Nebula" ) );
			Main.AddTechnique( m_RenderTechniqueNebula );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Create the post-process render techniques
			m_PPVolumetricFog = ToDispose( new PostProcessFog( this, "PostProcess Volumetric Fog" ) );
			PostProcessing.AddTechnique( m_PPVolumetricFog );

			m_PPDOFBlur = ToDispose( new PostProcessBlur( this, "PostProcess DOF Blur" ) );
			PostProcessing.AddTechnique( m_PPDOFBlur );

			m_PPMotionBlur = ToDispose( new PostProcessMotionBlur( this, "PostProcess MotionBlur" ) );
			PostProcessing.AddTechnique( m_PPMotionBlur );

			m_PPAntiAliasing = ToDispose( new PostProcessAntiAliasing( this, "PostProcess AntiAliasing" ) );
			PostProcessing.AddTechnique( m_PPAntiAliasing );

			m_PPColorimetry = ToDispose( new PostProcessColorimetry( this, "PostProcess Colorimetry" ) );
			PostProcessing.AddTechnique( m_PPColorimetry );


			//////////////////////////////////////////////////////////////////////////
			// Create the textures
			m_TextureLoader = ToDispose( new Utility.TextureLoader( m_Device, "Texture Loader", this ) );
			m_SceneTextureProvider = ToDispose( new SceneTextureProvider( m_Device, "File Texture Provider", new System.IO.DirectoryInfo( "Images/WaterColour" ), this, false ) );

			// 3D Noise textures
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = CreateNoiseTexture( NoiseIndex );


			//////////////////////////////////////////////////////////////////////////
			// Create the default camera
			m_DefaultCamera = ToDispose( new Camera( m_Device, "Default Camera" ) );
			m_Camera = m_DefaultCamera;
			m_DefaultCamera.CreatePerspectiveCamera( _CameraFOV, _CameraAspectRatio, _CameraNear, _CameraFar );
			m_CameraAspectRatio = _CameraAspectRatio;


			//////////////////////////////////////////////////////////////////////////
			// Create the special objects & primitives
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "PostProcess Quad" ) );

			// Setup the global light intensity factor so we can work in LDR in MAX and in HDR here...
			Scene.Light.GlobalIntensityMultiplier = GLOBAL_LIGHT_INTENSITY_MULTIPLIER;


			//////////////////////////////////////////////////////////////////////////
			// Additional data

			// Create the clips
#if CLIP_WIND_PARTICLES
			ClipWindParticles	C0 = new ClipWindParticles( this );
			m_Clips.Add( C0 );
#endif

#if CLIP_BOX_CLOUD
			ClipBoxCloud		C1 = new ClipBoxCloud( this );
			m_Clips.Add( C1 );
#endif

#if CLIP_FERRO_FLUID
			ClipFerroFluid		C2 = new ClipFerroFluid( this );
			m_Clips.Add( C2 );
#endif

#if CLIP_NEBULA
			ClipNebula		C3 = new ClipNebula( this );
			m_Clips.Add( C3 );
#endif

			// Disable all
			foreach ( Clip C in m_Clips )
				C.Enable( false );

			// Enable our default clip
			if ( m_Clips.Count > 0 )
				m_Clips[0].Enable( true );

#if REPLAY_CAMERA
			System.IO.FileInfo		CameraRecordFileName = new System.IO.FileInfo( "CameraRecord.rec" );
			if ( CameraRecordFileName.Exists )
			{
				System.IO.FileStream	CameraRecordFile = CameraRecordFileName.OpenRead();
				System.IO.BinaryReader	Reader = new System.IO.BinaryReader( CameraRecordFile );
				m_FrameCounter = Reader.ReadInt32();
				for ( int FrameIndex=0; FrameIndex < m_FrameCounter; FrameIndex++ )
				{
					Matrix	M = Matrix.Zero;
					M.M11 = Reader.ReadSingle( ); M.M12 = Reader.ReadSingle( ); M.M13 = Reader.ReadSingle( ); M.M14 = Reader.ReadSingle( );
					M.M21 = Reader.ReadSingle( ); M.M22 = Reader.ReadSingle( ); M.M23 = Reader.ReadSingle( ); M.M24 = Reader.ReadSingle( );
					M.M31 = Reader.ReadSingle( ); M.M32 = Reader.ReadSingle( ); M.M33 = Reader.ReadSingle( ); M.M34 = Reader.ReadSingle( );
					M.M41 = Reader.ReadSingle( ); M.M42 = Reader.ReadSingle( ); M.M43 = Reader.ReadSingle( ); M.M44 = Reader.ReadSingle( );
					m_CameraMatrices[FrameIndex] = M;
				}
				CameraRecordFile.Close();
				CameraRecordFile.Dispose();
			}
#endif	
		}

		public override void Dispose()
		{
#if RECORD_CAMERA
			if ( m_FrameCounter > 0 )
			{
				System.IO.FileInfo		CameraRecordFileName = new System.IO.FileInfo( "CameraRecord.rec" );
				System.IO.FileStream	CameraRecordFile = CameraRecordFileName.Create();
				System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( CameraRecordFile );
				Writer.Write( m_FrameCounter );
				for ( int FrameIndex=0; FrameIndex < m_FrameCounter; FrameIndex++ )
				{
					Matrix	M = m_CameraMatrices[FrameIndex];
					Writer.Write( M.M11 ); Writer.Write( M.M12 ); Writer.Write( M.M13 ); Writer.Write( M.M14 );
					Writer.Write( M.M21 ); Writer.Write( M.M22 ); Writer.Write( M.M23 ); Writer.Write( M.M24 );
					Writer.Write( M.M31 ); Writer.Write( M.M32 ); Writer.Write( M.M33 ); Writer.Write( M.M34 );
					Writer.Write( M.M41 ); Writer.Write( M.M42 ); Writer.Write( M.M43 ); Writer.Write( M.M44 );
				}
				CameraRecordFile.Close();
				CameraRecordFile.Dispose();
			}
#endif

			base.Dispose();
		}

		/// <summary>
		/// Renders the objects registered to our renderer
		/// </summary>
		public void	Render()
		{
			//////////////////////////////////////////////////////////////////////////
			// Clear stuff
			m_Device.AddProfileTask( this, "Prepare Rendering", "Clear Targets" );

			// Clear normals to 0 and depth to "infinity"
			m_Device.ClearRenderTarget( m_GeometryBuffer, new Color4( 0.0f, 0.0f, 0.0f, DEPTH_BUFFER_INFINITY ) );

			// Clear materials
//			m_Device.ClearRenderTarget( m_MaterialBuffer, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
			m_Device.ClearRenderTarget( m_MaterialBuffer, new Color4( System.Drawing.Color.SkyBlue.ToArgb() ) );

			// Clear emissive to black and no extinction
			m_Device.ClearRenderTarget( m_EmissiveBuffer, new Color4( 1.0f, 0.0f, 0.0f, 0.0f ) );

			// Clear velocity to 0
			m_Device.ClearRenderTarget( m_VelocityBuffer, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );

			// Clear render target
//			m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, (Color4) System.Drawing.Color.CornflowerBlue );
			m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

			//////////////////////////////////////////////////////////////////////////
// 			// Propagate parameters
// 			m_DeferredGrass.Time = m_Time;
// 			m_SunLight.Direction = m_EmissiveSky.SunDirection;
// 			m_SunLight.Color = m_EmissiveSky.SunColor;


#if TEST_MOTION
			// BOX MOTION
			Scene.Mesh	NodeBox = m_Scene.FindMesh( "Box03", true );

			Matrix	Temp = Matrix.RotationZ( m_Angle );
//			Matrix	Temp = NodeBox.Local2Parent;
			Temp.Row4 = NodeBox.Local2Parent.Row4;
//			Temp.M43 = 1.0f * (float) Math.Abs( Math.Sin( m_Angle ) );
			NodeBox.Local2Parent = Temp;

// 			Matrix	Prev = new Matrix();
// 		Prev.M11 = 1.0f;
// 		Prev.M12 = 0.0f;
// 		Prev.M13 = 0.0f;
// 		Prev.M14 = 0.0f;
// 		Prev.M21 = 0.0f;
// 		Prev.M22 = 0.0f;
// 		Prev.M23 = -1.0f;
// 		Prev.M24 = 0.0f;
// 		Prev.M31 = 0.0f;
// 		Prev.M32 = 1.0f;
// 		Prev.M33 = 0.0f;
// 		Prev.M34 = 0.0f;
// 		Prev.M41 = 0.929684043f;
// 		Prev.M42 = 0.07488296f;
// 		Prev.M43 = -1.71377409f;
// 
// 			Matrix	Curr = Prev;
// // 		Curr.M11 = 0.987688363f;
// // 		Curr.M12 = 0.0f;
// // 		Curr.M13 = -0.156434476f;
// // 		Curr.M14 = 0.0f;
// // 		Curr.M21 = -0.156434476f;
// // 		Curr.M22 = 0.0f;
// // 		Curr.M23 = -0.987688363f;
// 		Curr.M24 = 0.0f;
// 		Curr.M31 = 0.0f;
// 		Curr.M32 = 1.0f;
// 		Curr.M33 = 0.0f;
// 		Curr.M34 = 0.0f;
// 		Curr.M41 = 0.929684043f;
// 		Curr.M42 = 0.57488296f;
// 		Curr.M43 = -1.71377409f;
// 		Curr.M44 = 1.0f;
// 
// 			NodeBox.PreviousLocal2World = Prev;
// 			NodeBox.Local2World = Curr;

			// CAMERA MOTION
			Vector3		Target = new Vector3( 0.0f, 2.0f, 0.0f );
			float		Dist = 15.0f;
			float		fAngle = 0.25f * m_Angle;
			Vector3		Pos = Target + new Vector3( Dist * (float) Math.Cos( fAngle ), 10.0f, Dist * (float) Math.Sin( fAngle ) );
// 			float		fAngle = (float) (0.25f * Math.PI * Math.Abs( Math.Sin( 0.1f * m_Angle ) ));
// 			Vector3		Pos = Target + new Vector3( Dist * (float) Math.Sin( fAngle ), 10.0f, Dist * (float) Math.Cos( fAngle ) );
// 			float		fJitter = 0.1f;
// 			Pos.X += 0*fJitter * (2.0f * (float) RNG.NextDouble() - 1.0f);
// 			Pos.Y += fJitter * (2.0f * (float) RNG.NextDouble() - 1.0f);
// 			Pos.Z += 0*fJitter * (2.0f * (float) RNG.NextDouble() - 1.0f);
//			m_Camera.LookAt( Pos, Target, Vector3.UnitY );

			m_Angle += 0.05f * (float) Math.PI;
#endif

#if RECORD_CAMERA
			if ( System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt )
				m_CameraMatrices[m_FrameCounter++] = m_Camera.Camera2World;
#elif REPLAY_CAMERA
			if ( m_FrameCounter > 0)
			{

m_ReplayFrame = m_PPMotionBlur.m_RecordedCameraFrameIndex;

				m_Camera.Camera2World = m_CameraMatrices[m_ReplayFrame];
//				m_ReplayFrame = (m_ReplayFrame+1) % m_FrameCounter;
			}
#endif

			//////////////////////////////////////////////////////////////////////////
			// Render !
			m_Device.AddProfileTask( this, "Rendering", "<START>" );
			m_Renderer.Render();
			m_Device.AddProfileTask( this, "Rendering", "<END>" );


			//////////////////////////////////////////////////////////////////////////
			// SHADOW MAP DEBUG
// 			m_Device.SetDefaultRenderTarget();
// 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
// 			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
// 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
//			m_ShadowMapping.RenderShadowMapDebug( 1, 1 );
		}

		protected Random	RNG = new Random(1);
		protected float		m_Angle = 0.0f;

		protected int		m_FrameCounter = 0;
		protected int		m_ReplayFrame = 0;
		protected Matrix[]	m_CameraMatrices = new Matrix[16384];

		public Matrix[]		RecordedCameraMatrices		{ get { return m_CameraMatrices; } }
		public int			RecordedCameraMatricesCount	{ get { return m_FrameCounter; } }

		/// <summary>
		/// For post-processing techniques that render the entire screen in a quad
		/// </summary>
		public void	RenderPostProcessQuad()
		{
			m_Quad.Render();
		}

		/// <summary>
		/// For post-processing techniques that rendeer the entire screen in a quad
		/// </summary>
		public void	RenderPostProcessQuadInstanced( int _InstancesCount )
		{
			m_Quad.RenderInstanced( 0, _InstancesCount );
		}

		/// <summary>
		/// For post-processing techniques that rendeer the entire screen in a quad
		/// </summary>
		public void	RenderPostProcessQuadInstanced( int _StartInstance, int _InstancesCount )
		{
			m_Quad.RenderInstanced( _StartInstance, _InstancesCount );
		}

		/// <summary>
		/// Sets the default offscreen target
		/// </summary>
		public void	SetDefaultRenderTarget()
		{
//			m_Device.SetRenderTarget( m_MaterialBuffer, m_Device.DefaultDepthStencil );
			m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_MaterialBuffer, m_VelocityBuffer }, m_Device.DefaultDepthStencil );
			m_Device.SetViewport( 0, 0, m_GeometryBuffer.Width, m_GeometryBuffer.Height, 0.0f, 1.0f );
		}

		public void	SetFinalRenderTarget()
		{
			m_Device.SetRenderTarget( m_MaterialBuffer2 );
			m_Device.SetViewport( 0, 0, m_MaterialBuffer2.Width, m_MaterialBuffer2.Height, 0.0f, 1.0f );
		}

		public void	SwapFinalRenderTarget()
		{
			RenderTarget<PF_RGBA16F>	Temp = m_MaterialBuffer;
			m_MaterialBuffer = m_MaterialBuffer2;
			m_MaterialBuffer2 = Temp;
		}

		public void	SetVelocityRenderTarget()
		{
			m_Device.SetRenderTarget( m_VelocityBuffer2 );
			m_Device.SetViewport( 0, 0, m_VelocityBuffer2.Width, m_VelocityBuffer2.Height, 0.0f, 1.0f );
		}

		public void	SwapVelocityRenderTarget()
		{
			RenderTarget<PF_RGBA16F>	Temp = m_VelocityBuffer;
			m_VelocityBuffer = m_VelocityBuffer2;
			m_VelocityBuffer2 = Temp;
		}

		#region IShaderInterfaceProvider Members

		protected Vector4	BLACK = Vector4.Zero;
		public void ProvideData( IShaderInterface _Interface )
		{
			// Provide deferred rendering interface data
			IDeferredRendering	I = _Interface as IDeferredRendering;
			if ( I != null )
			{
				I.GBuffer0 = m_MaterialBuffer;
				I.GBuffer1 = m_GeometryBuffer;
				I.GBuffer2 = m_EmissiveBuffer;
				I.GBuffer3 = m_VelocityBuffer;
				I.DepthBuffer = m_Device.DefaultDepthStencil;
				I.GBufferSize = m_GBufferSize;
				I.GBufferInvSize = m_GBufferInvSize;
				return;
			}

			// Provide camera data
			ICamera	I1 = _Interface as ICamera;
			if ( I1 != null )
				m_Camera.ProvideData( _Interface );

			// Provide Key/Rim/Fill data
			ILightKeyRimFill	I2 = _Interface as ILightKeyRimFill;
			if ( I2 != null )
			{
				if ( m_LightKey != null )
				{
					I2.KeyPosition = m_LightKey.Position;
					I2.KeyDirection = m_LightKey.Direction;
					I2.KeyColor = m_LightKey.Color;
					I2.KeyData = m_LightKey.CachedData;
					I2.KeyData2 = m_LightKey.CachedData2;
				}
				else
					I2.KeyColor = BLACK;

				if ( m_LightRim != null )
				{
					I2.RimPosition = m_LightRim.Position;
					I2.RimDirection = m_LightRim.Direction;
					I2.RimColor = m_LightRim.Color;
					I2.RimData = m_LightRim.CachedData;
					I2.RimData2 = m_LightRim.CachedData2;
				}
				else
					I2.RimColor = BLACK;

				if ( m_LightFill != null )
				{
					I2.FillPosition = m_LightFill.Position;
					I2.FillDirection = m_LightFill.Direction;
					I2.FillColor = m_LightFill.Color;
					I2.FillData = m_LightFill.CachedData;
					I2.FillData2 = m_LightFill.CachedData2;
				}
				else
					I2.FillColor = BLACK;

				return;
			}

			// Provide noise data
			INoise3D	I3 = _Interface as INoise3D;
			if ( I3 != null )
			{
				I3.Noise0 = m_NoiseTextures[0];
				I3.Noise1 = m_NoiseTextures[1];
				I3.Noise2 = m_NoiseTextures[2];
				I3.Noise3 = m_NoiseTextures[3];
				return;
			}

			// Provide linear tone mapping data
			ILinearToneMapping	I4 = _Interface as ILinearToneMapping;
			if ( I4 != null )
			{
				I4.ToneMappingFactor = 1.0f;
			}

			// Provide directional lighting data
			IDirectionalLight	I5 = _Interface as IDirectionalLight;
			if ( I5 != null )
			{
				I5.Direction = m_LightKey.Direction;
				I5.Color = m_LightKey.Color;
			}
			IDirectionalLight2	I6 = _Interface as IDirectionalLight2;
			if ( I6 != null )
			{
				I6.Direction = m_LightFill.Direction;
				I6.Color = m_LightFill.Color;
			}
		}

		#endregion

		#region IFileLoader Members

		// For now, we use a disk provider

#if FILE_PROVIDER_IS_DISK
		public System.IO.Stream OpenFile( System.IO.FileInfo _FileName )
		{
			return _FileName.OpenRead();
		}

		public void				ReadBinaryFile( System.IO.FileInfo _FileName, FileReaderDelegate _Reader )
		{
			using ( System.IO.Stream Stream = OpenFile( _FileName ) )
			{
				using ( System.IO.BinaryReader Reader = new System.IO.BinaryReader( Stream  ) )
				{
					_Reader( Reader );
				}
				Stream.Close();
			}
		}
#else
		Implement Archive Provider !
#endif

		#endregion

		#region IMaterialLoader Members

		protected System.IO.FileInfo	m_CurrentOpeningMaterial = null;
		public Material<VS> LoadMaterial<VS>( string _Name, ShaderModel _SM, System.IO.FileInfo _FileName ) where VS : struct
		{
			m_CurrentOpeningMaterial = _FileName;

#if FILE_PROVIDER_IS_DISK
			return ToDispose( new Material<VS>( m_Device, _Name, _SM, _FileName ) );
#else
			using ( System.IO.Stream EffectStream = OpenFile( _FileName ) )
				return ToDispose( new Material<VS>( m_Device, _Name, _SM, EffectStream, this ) );
#endif
		}

		#endregion

		#region Include Members

		public void Close( System.IO.Stream stream )
		{
			stream.Close();
			stream.Dispose();
		}

		public void Open( SharpDX.D3DCompiler.IncludeType type, string fileName, System.IO.Stream parentStream, out System.IO.Stream stream )
		{
			System.IO.FileInfo	IncludeFile = new System.IO.FileInfo( System.IO.Path.Combine( m_CurrentOpeningMaterial.DirectoryName, fileName ) );
			stream = OpenFile( IncludeFile );
		}

		#endregion

		/// <summary>
		/// Generic scene loader
		/// </summary>
		/// <param name="_FileName"></param>
		/// <param name="_SceneName"></param>
		/// <param name="_MMap">ONLY USED FOR FBX LOADING</param>
		/// <param name="_ScaleFactor">ONLY USED FOR FBX LOADING</param>
		/// <returns></returns>
		public Scene			LoadScene( System.IO.FileInfo _FileName, string _SceneName, MaterialMap _MMap, float _ScaleFactor )
		{
			Scene	S = ToDispose( new Scene( m_Device, _SceneName, m_Renderer ) );

			// Load scene
#if LOAD_FROM_FBX
			using ( Nuaj.Helpers.FBX.SceneLoader SceneLoader = new Nuaj.Helpers.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
				SceneLoader.Load( _FileName, S, _MMap, m_SceneTextureProvider, _ScaleFactor );
			}
#else
			// Load proprietary scene format
			ReadBinaryFile( _FileName, ( _Reader ) => { S.Load( _Reader, this ); } );
#endif
			return S;
		}

		/// <summary>
		/// Cycle through clips
		/// </summary>
		public void				CycleClips()
		{
			if ( m_Clips.Count < 2 )
				return;	// No clip to cycle through...

			m_Clips[m_CurrentClipIndex].Enable( false );
			m_CurrentClipIndex = (m_CurrentClipIndex+1) % m_Clips.Count;
			m_Clips[m_CurrentClipIndex].Enable( true );
		}

		/// <summary>
		/// Creates a 3D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		protected Texture3D<PF_RGBA16F>	CreateNoiseTexture( int _NoiseIndex )
		{
			const int	NOISE_SIZE = 16;

			// Build the volume filled with noise
			byte[][]	NoiseResources = new byte[4][]
			{
				Demo.Properties.Resources.packednoise_half_16cubed_mips_00,
				Demo.Properties.Resources.packednoise_half_16cubed_mips_01,
				Demo.Properties.Resources.packednoise_half_16cubed_mips_02,
				Demo.Properties.Resources.packednoise_half_16cubed_mips_03,
			};

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( NoiseResources[_NoiseIndex] );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			int	XS, YS, ZS, PS;
			XS = Reader.ReadInt32();
			YS = Reader.ReadInt32();
			ZS = Reader.ReadInt32();
			PS = Reader.ReadInt32();

			Half		Temp = new Half();
			float[,,]	Noise = new float[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];
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
			Reader.Close();
			Reader.Dispose();

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

		#region EVENT HANDLERS

		protected void EmissivePipeline_RenderingStart( Pipeline _Sender )
		{
// 			// Setup the emissive render target and clear it
// 			m_Device.SetRenderTarget( m_EmissiveBuffer, m_Device.DefaultDepthStencil );
// 			m_Device.SetViewport( 0, 0, m_EmissiveBuffer.Width, m_EmissiveBuffer.Height, 0.0f, 1.0f );
		}

		protected void MainPipeline_RenderingStart( Pipeline _Sender )
		{
// 			// Setup our multiple render targets
// 			m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_MaterialBuffer, m_GeometryBuffer }, m_Device.DefaultDepthStencil );
// 			m_Device.SetViewport( 0, 0, m_MaterialBuffer.Width, m_MaterialBuffer.Height, 0.0f, 1.0f );
		}

		#endregion
	}
}
