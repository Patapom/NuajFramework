//
// TODO:
// 
// * Compute Sky ambient
//	=> Pack into SH
//
// * Downscale Z with MAX filter
//	=> Always render __behind__ the scene unless all pixels are in front
//
// * Refine borders
//	=> Tester Noise 2D avec un Z approximatif là où l'opacité est maximale
//		-> Pas top mais à voir
//
// * Compute light diffusion for ambient lighting
//
// * Better shadow map
//	=> Try better precision around camera
//	=> In fact, better precision at intersection point between (Camera,Light) and cloud plane
//	=> Try progressive cascades oriented following the curvature of the Earth
//
// * DSM
//	=> Use RGBA16_UNorm
//	=> Store 2 densities per component (D0*256 + D1)
// ############### CA MARCHE PAS ET CA RAME ! ###############
//
// * Cloud painting
//
// * Fade vers cloud density uniforme avec la distance
// ############### DONE (p'têt à revoir) ! ###############
//
// * RNG
//	=> Utiliser un RNG simple qui prend un int en paramètre
//	=> I = x + 16 * (y + 16 * z)
//	=> Retourne 2 float4 pour les 8 coins d'un cube
//	=> Interpoile des 8 valeurs
// ############### CA MARCHE BIEN MAIS CA RAME + QUE SAMPLER LA TEXTURE 3D ! ###############
//
// * Progressive tracing
//	=> Advance by Dx, if too much difference in density/opacity, go back Dx/2, etc.
//	=> Dx = -log( Random ) / Sigma_t(x)
//
//
// * Accurate Refinement
//	=> Diminuer le threshold Z avec la distance (i.e. + important de refiner quand on a une différence de Z et qu'on est loin. Tandis que quand on est proche de la caméra, la perspective atmosphérique joue très peu)
//	=> Ca dépend également du facteur de fog et d'air...
//
//
// Mail "idées" :
// --------------
// => Tracer en log( 1 / sigma_t ) et s'arrêter rapidement
// => Utiliser mon superbe pseudo-Z pour isoler les bordures et refiner
// => Rajouter des choux-fleurs sur la périphérie
//   => Utiliser un downscale / 2 et ajouter une 10aine de pas de ray-march avec un noise plus défini et plus haute fréquence ?
// * Augmenter le fog avec la distance vers l'horizon pour cacher les pas belles choses...
//  => Faire gaffe au coucher de soleil, ça risque de masquer les jolies choses :)
//
//
//
//
// ******************* FOCALISER LA SHADOW MAP SUR LA CAMERA !!
//	=> Mapping en 1/distance ???
//
//
//
//
// * Incorporer l'ombre de la Terre dans la DSM !!
//
//


// * Essayer de trouver une formule où l'on peut spécifier une opacité (100% => 0%) à laquelle s'arrêter
//  en utilisant ma fonction magique exp( -Value * Sigma(x) * Dx )
//   => Si j'y parviens alors ça signifie que je l'ai mon ZBuffer magique que je peux interpoler linéairement !
//   => Il suffirait de le stocker dans une 3ème render target et HOP ! Order-Independent Rendering de tout ! FX, alphas, etc. !
//   => Faire pareil pour la shadow map, il se pourrait même que je n'aie plus besoin de 2 targets
//   => Tracé conditionnel où l'on s'arrête en dessous d'une certaine opacité.
//     -> idem pour nuages en fait...
// 
// * Calculer l'ombre tous les N steps de ray-marching seulement !
//   
// * Remarquons que comme je calcule les nuages avec un Z = max( Z0, Z1, Z2, Z3 ), je calcule toujours ce qui se trouve
//  _derrière_ le décor.
//   => Ca signifie que ce qui va être faux sera le décor lui-même, qui bloque la vue des nuages puisqu'on a spécifié explicitement que les nuages
//    n'intersecteraient pas le décor (à voir pour les sommets, on peut gruger)
//   => Ca veut donc dire qu'il nous suffit juste de recalculer 2 choses :
//    1) Ciel (avec un pas réduit) puisqu'on va toucher le décor "tôt"
//    2) Brouillard


// Ta tâche d'aujourd'hui :
//  1) Isoler la source du ralentissement (a priori, le calcul des nuages lui-même)
//  2) Implémenter un tracé différent (exp, récursif, etc.?)
//  3) Faire un meilleur Bevel pour empêcher les nuages d'être coupés de manière aussi abrupte !


//#define LOAD_PRECOMP_NOISE
#define CREATE_NOISE_TEXTURES

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

using Nuaj;
using Nuaj.Cirrus;
using Nuaj.Cirrus.Utility;

namespace Demo
{
	public partial class DemoForm : Form, IShaderInterfaceProvider, IMaterialLoader
	{
		#region CONSTANTS

		protected const int		TERRAIN_TILES_COUNT = 512;
		protected const float	TERRAIN_SCALE_HORIZONTAL = 20000.0f;
		protected const float	TERRAIN_OFFSET_VERTICAL = -80.0f;
		protected const float	TERRAIN_SCALE_VERTICAL = 200.0f;
		protected const float	TERRAIN_NOISE_SCALE = 0.003f;

		protected const int		SH_NOISE_ENCODING_SAMPLES_COUNT = 32;

		protected const int		LARGE_NOISE_SIZE = 64;
		protected const int		NOISE_TEXTURE_SIZE = 16;

		#endregion

		#region NESTED TYPES

		protected class	INoise3D : ShaderInterfaceBase
		{
			[Semantic( "NOISE3D_TEX0" )]
			public ITexture3D				Noise0		{ set { SetResource( "NOISE3D_TEX0", value ); } }
			[Semantic( "NOISE3D_TEX1" )]
			public ITexture3D				Noise1		{ set { SetResource( "NOISE3D_TEX1", value ); } }
			[Semantic( "NOISE3D_TEX2" )]
			public ITexture3D				Noise2		{ set { SetResource( "NOISE3D_TEX2", value ); } }
			[Semantic( "NOISE3D_TEX3" )]
			public ITexture3D				Noise3		{ set { SetResource( "NOISE3D_TEX3", value ); } }
// 			[Semantic( "LARGE_NOISE3D_TEX" )]
// 			public ITexture3D				LargeNoise	{ set { SetResource( "LARGE_NOISE3D_TEX", value ); } }
		}

		#endregion

		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
		protected RenderTechniqueVolumeClouds	m_RenderTechniqueClouds = null;	// The advanced render technique for volume clouds
		protected RenderTechniquePostProcessToneMappingFilmic	m_ToneMapping = null;

		protected RenderTarget<PF_RGBA16F>[]	m_RenderTargets = new RenderTarget<PF_RGBA16F>[2];

		// Ground terrain (no fancy stuff here, just a large mesh to test ZBuffer interaction)
		protected Material<VS_P3N3>			m_MaterialGround = null;
		protected Texture2D<PF_RGBA8>		m_TerrainTexture = null;
		protected Primitive<VS_P3N3,int>	m_Terrain = null;

		// The 16x16x16 noise textures
		protected Texture3D<PF_RGBA16F>[]	m_NoiseTextures = new Texture3D<PF_RGBA16F>[4];
		protected Vector4[][,,]				m_CPUNoiseTextures = new Vector4[4][,,];
//		protected Texture3D<PF_RGBA16F>		m_LargeNoiseTexture = null;
		protected Texture3D<PF_R16F>		m_LargeNoiseTexture2 = null;

		// The 1x1 render target array for the ambient sky probe
		protected RenderTarget<PF_RGBA16F>	m_SkyProbe = null;
		protected RenderTarget<PF_RGBA16F>	m_SkyProbeNoCloud = null;


		// Helper Forms
		protected Nuaj.Cirrus.Utility.ProfilerForm	m_ProfilerForm = null;
		protected Nuaj.Cirrus.Utility.FilmicToneMappingSetupForm	m_ToneMappingForm = null;
		protected ShadowMapViewForm	m_ShadowForm = null;
		protected CloudProfilerForm					m_CloudProfilerForm = null;


		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public unsafe DemoForm()
		{
			InitializeComponent();

//			BuildNoiseArray();

// int	LIGHT_PROBE_SIZE_Y = 5000;
// float	dTheta = 0.5f * 3.1415926535897932384626433832795f / LIGHT_PROBE_SIZE_Y;
// float	fSumAngle = 0.0f;
// for ( int Y=0; Y < LIGHT_PROBE_SIZE_Y; Y++ )
// 	fSumAngle += dTheta * (float) Math.Sin( 0.5 * Math.PI * Y / LIGHT_PROBE_SIZE_Y );
// 
// fSumAngle *= 2.0f * 3.1415926535897932384626433832795f;

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( panelOutput.Width, panelOutput.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm_SRgb ),
					IsWindowed = true,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, panelOutput, true ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}

			//////////////////////////////////////////////////////////////////////////
			// Register ourselves as shader interface provider
			m_Device.DeclareShaderInterface( typeof(INoise3D) );
			m_Device.RegisterShaderInterfaceProvider( typeof(INoise3D), this );				// Register the INoise3D interface

			//////////////////////////////////////////////////////////////////////////
			// Create our scene render target, with mip-maps for tone mapping
			m_RenderTargets[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Render Target 0 (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );
			m_RenderTargets[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Render Target 1 (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the noise textures
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = CreateNoiseTexture( NoiseIndex, out m_CPUNoiseTextures[NoiseIndex] );

//			m_LargeNoiseTexture = CreateLargeNoiseTexture();
//			m_LargeNoiseTexture2 = CreateLargeNoiseTexture2();

			//////////////////////////////////////////////////////////////////////////
			// Create the 1x1 render targets array for ambient sky rendering into SH
			m_SkyProbe = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "AmbientSkySH", 1, 1, 1, 3, 1 ) );
			m_SkyProbeNoCloud = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "AmbientSkySHNoCloud", 1, 1, 1, 3, 1 ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the renderer
			RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
			{
				CameraFOV = 60.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) panelOutput.Width / panelOutput.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 5000.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

			m_RenderTechniqueClouds = ToDispose( new RenderTechniqueVolumeClouds( m_Renderer, m_RenderTargets, "Volume Clouds Render Technique" ) );
			m_RenderTechniqueClouds.Camera = m_Renderer.Camera;
			m_RenderTechniqueClouds.Light = m_Renderer.MainLight;
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).InsertTechnique( 0, m_RenderTechniqueClouds );	// Insert our technique at the beginning

			m_RenderTechniqueClouds.NoiseSize = 0.005f;;
//			m_RenderTechniqueClouds.NoiseSize = 0.2f;	// DEBUG
			m_RenderTechniqueClouds.CoverageOffsetBottom = -1.29f;	// DEBUG
			m_RenderTechniqueClouds.WindForce = 0.02f;	// DEBUG => Don't move you fuck !


			//////////////////////////////////////////////////////////////////////////
			// Tone mapping technique
			m_ToneMapping = ToDispose( new RenderTechniquePostProcessToneMappingFilmic( m_Renderer.Device, "Tone Mapping", this, true ) );
			m_ToneMapping.SourceImage = m_RenderTargets[1];
			m_ToneMapping.TargetImage = m_Device.DefaultRenderTarget;
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).AddTechnique( m_ToneMapping );

			m_ToneMapping.AdaptationLevelMin = 0.4f;
			m_ToneMapping.AdaptationLevelMax = 1.0f;
			m_ToneMapping.TemporalAdaptationSpeed = 0.9999f;

			m_ToneMapping.DebugLuminanceMax = m_RenderTechniqueClouds.Sky.SunIntensity;

			// Filmic parameters
// 			m_ToneMapping.ExposureBias = 1.54f;
// 			m_ToneMapping.HDRWhitePointLuminance = 100.0f;	// The HDR intensity we consider to be white
// 			m_ToneMapping.LDRWhitePointLuminance = 2.2f;	// The LDR intensity white will be mapped to
//
// 			m_ToneMapping.A = 0.46f;
// 			m_ToneMapping.B = 0.21f;
// 			m_ToneMapping.C = 0.40f;
// 			m_ToneMapping.D = 0.33f;
// 			m_ToneMapping.E = 0.30f;
// 			m_ToneMapping.F = 0.85f;

// 			m_ToneMapping.ExposureBias = 4.0f;
// 			m_ToneMapping.HDRWhitePointLuminance = 100.0f;	// The HDR intensity we consider to be white
// 			m_ToneMapping.LDRWhitePointLuminance = 2.353f;	// The LDR intensity white will be mapped to
// 
// 			m_ToneMapping.A = 0.375f;
// 			m_ToneMapping.B = 0.1513f;
// 			m_ToneMapping.C = 0.3355f;
// 			m_ToneMapping.D = 0.5987f;
// 			m_ToneMapping.E = 0.3882f;
// 			m_ToneMapping.F = 0.85f;

//			m_ToneMapping.ExposureBias = 2.279f;
			m_ToneMapping.ExposureBias = 1.0f;
			m_ToneMapping.HDRWhitePointLuminance = 100.0f;	// The HDR intensity we consider to be white
			m_ToneMapping.LDRWhitePointLuminance = 2.059f;	// The LDR intensity white will be mapped to

			m_ToneMapping.A = 0.375f;
			m_ToneMapping.B = 0.5329f;
			m_ToneMapping.C = 0.3355f;
			m_ToneMapping.D = 0.1579f;
			m_ToneMapping.E = 0.1776f;
			m_ToneMapping.F = 0.7895f;

			m_ToneMapping.AverageOrMax = 0.1f;

m_ToneMapping.EnableToneMapping = true;


			//////////////////////////////////////////////////////////////////////////
			// Create the terrain
			m_MaterialGround = ToDispose( new Material<VS_P3N3>( m_Device, "Ground Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Clouds2/GroundDisplay.fx" ) ) );
//			m_TerrainTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Ground Texture", new System.IO.FileInfo( "./Media/Terrain/ground_grass_1024_tile.jpg" ), 0, 1.0f ) );
			m_TerrainTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Ground Texture", new System.IO.FileInfo( "./Media/White32x32.png" ), 0, 1.0f ) );
			CreateTerrainPrimitive();


			BuildHierarchyTree();

			// Assign default values to parameters
			InitializeTrackbars();

			// Create the profiler & tone mapping forms
			m_ProfilerForm = new Nuaj.Cirrus.Utility.ProfilerForm( m_Device );
			m_ToneMappingForm = new FilmicToneMappingSetupForm();
			m_ToneMappingForm.ToneMappingTechnique = m_ToneMapping;
			m_ShadowForm = new ShadowMapViewForm();
			m_ShadowForm.Clouds = m_RenderTechniqueClouds;

			m_CloudProfilerForm = new CloudProfilerForm();
			m_CloudProfilerForm.Clouds = m_RenderTechniqueClouds;
			m_CloudProfilerForm.RebuildProfileTexture();
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			//////////////////////////////////////////////////////////////////////////
			// Create a camera manipulator
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
//			CamManip.InitializeCamera( new Vector3( -235.0f, 54.0f, -210.0f ), new Vector3( -233.0f, 54.0f, -190.0f ), Vector3.UnitY );
			CamManip.InitializeCamera( new Vector3( 0.0f, 50.0f, 0.0f ), new Vector3( 0.0f, 50.0f, 200.0f ), Vector3.UnitY );

			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			string		InitialText = Text;
			DateTime	LastFPSTime = DateTime.Now;
			int			FPSFramesCount = 0;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				if ( !m_Device.CheckCanRender( 5 ) )
					return;

				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

// 				// =============== Lightning Animation ===============
// 				float	DeltaTimeLightning = (float) (CurrentFrameTime - m_LightningStrikeTime).TotalSeconds;
// 
// 				float	IntensityEnveloppe = (float) Math.Exp( -10.0 * DeltaTimeLightning*DeltaTimeLightning );
// 				float	LightningAmplitude = (float) Math.Max( 0.0, Math.Cos( 10.0 * DeltaTimeLightning * Math.PI ) );
// 				m_RenderTechniqueClouds.LightningIntensity = 50000.0f * IntensityEnveloppe * LightningAmplitude;

				// =============== Render Scene ===============

				m_Device.StartProfiling( m_ProfilerForm.FlushEveryOnTask );
				m_Device.AddProfileTask( null, "==RENDER LOOP==", "Start" );

				// Clear render target
				m_Device.AddProfileTask( null, "==RENDER LOOP==", "Clear Target + Stencil" );
				m_Device.ClearRenderTarget( m_RenderTargets[0], new Color4( 0, 0, 0, 1 ) );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Render terrain
				m_Device.AddProfileTask( null, "==RENDER LOOP==", "Render Terrain" );
				using ( m_MaterialGround.UseLock() )
				{
					m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.CULL_BACK );
					m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
					m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );
					m_Device.SetRenderTarget( m_RenderTargets[0], m_Device.DefaultDepthStencil );
					m_Device.SetViewport( 0, 0, m_RenderTargets[0].Width, m_RenderTargets[0].Height, 0.0f, 1.0f );

					m_MaterialGround.GetVariableByName( "GroundTexture" ).AsResource.SetResource( m_TerrainTexture );
					m_MaterialGround.ApplyPass(0);
					m_Terrain.Render();
				}

// 				// Render lightning
// 				m_RenderTechniqueClouds.DisplayLightning();

				// Render sky probe
				m_Device.AddProfileTask( null, "==RENDER LOOP==", "Compute Sky SH" );
//				Vector3	ProbePosition = m_Renderer.Camera.Position;
				Vector3	ProbePosition = Vector3.Zero;			// Always center on the terrain
				m_RenderTechniqueClouds.ComputeAmbientSkySH( ProbePosition, m_SkyProbe, true );
				m_RenderTechniqueClouds.ComputeAmbientSkySH( ProbePosition, m_SkyProbeNoCloud, false );
				m_RenderTechniqueClouds.AmbientSkySH = m_SkyProbe;					// Feed the sky its own probe
				m_RenderTechniqueClouds.AmbientSkySHNoCloud = m_SkyProbeNoCloud;	// Feed the sky its own probe

				// Render everything else
				m_Device.AddProfileTask( null, "==RENDER LOOP==", "Render" );
				m_RenderTechniqueClouds.Time = fTotalTime;
				m_ToneMapping.DeltaTime = fDeltaTime;
				m_Renderer.Render();

				// Show !
				m_Device.AddProfileTask( null, "==RENDER LOOP==", "Present" );
				m_Device.Present();

				m_Device.AddProfileTask( null, "==RENDER LOOP==", "End" );
				m_Device.EndProfiling();

				// Update FPS
				FPSFramesCount++;
				DateTime	Now = DateTime.Now;
				if ( (Now - LastFPSTime).TotalMilliseconds > 1000 )
				{
					float	FPS = (float) (FPSFramesCount / (Now - LastFPSTime).TotalSeconds);
					LastFPSTime = Now;
					FPSFramesCount = 0;
					Text = InitialText + " - " + FPS.ToString( "G4" ) + " FPS - Luminance Avg=" + m_ToneMapping.AverageLuminance + " Min=" + m_ToneMapping.MinLuminance + " Max=" + m_ToneMapping.MaxLuminance;
				}

				if ( m_ProfilerForm.Visible || m_ToneMappingForm.Visible || m_ShadowForm.Visible || m_CloudProfilerForm.Visible )
					Application.DoEvents();	// Otherwise additional forms stall...
			});
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			// Provide noise data
			INoise3D	I = _Interface as INoise3D;
			if ( I != null )
			{
				I.Noise0 = m_NoiseTextures[0];
// 				I.Noise1 = m_NoiseTextures[1];
// 				I.Noise2 = m_NoiseTextures[2];
// 				I.Noise3 = m_NoiseTextures[3];
//				I.LargeNoise = m_LargeNoiseTexture2;
				return;
			}
		}

		#endregion

		#region IMaterialLoader Members

		public Material<VS> LoadMaterial<VS>( string _Name, ShaderModel _SM, System.IO.FileInfo _FileName ) where VS : struct
		{
			return ToDispose( new Material<VS>( m_Device, _Name, _SM, _FileName ) );
		}

		#endregion

		#region Tree View Management

		protected TreeNode	m_ShaderInterfaceProvidersNode = null;
		protected Dictionary<IShaderInterfaceProvider,TreeNode>	m_ShaderInterfaceProvider2TreeNode = new Dictionary<IShaderInterfaceProvider,TreeNode>();
		protected void	BuildHierarchyTree()
		{
			// Build the renderer nodes
			TreeNode	RendererNode = new TreeNode( "Renderer" );
						RendererNode.Tag = m_Renderer;
			treeViewObjects.Nodes.Add( RendererNode );

			foreach ( Pipeline P in m_Renderer.Renderer.Pipelines )
			{
				TreeNode	PipelineNode = new TreeNode( P.Name + " (" + P.Type + ")" );
							PipelineNode.Tag = P;
				RendererNode.Nodes.Add( PipelineNode );

				foreach ( RenderTechnique RT in P.RenderTechniques )
				{
					TreeNode	RenderTechniqueNode = new TreeNode( RT.Name );
								RenderTechniqueNode.Tag = RT;
					PipelineNode.Nodes.Add( RenderTechniqueNode );
				}
			}

			// Build the renderer nodes
			m_ShaderInterfaceProvidersNode = new TreeNode( "Shader Providers" );
			m_ShaderInterfaceProvidersNode.Tag = m_Device;
			treeViewObjects.Nodes.Add( m_ShaderInterfaceProvidersNode );

			foreach ( IShaderInterfaceProvider SIP in m_Device.RegisteredShaderInterfaceProviders )
			{
				TreeNode	ProviderNode = new TreeNode( SIP.ToString() );
							ProviderNode.Tag = SIP;
				m_ShaderInterfaceProvidersNode.Nodes.Add( ProviderNode );
				m_ShaderInterfaceProvider2TreeNode[SIP] = ProviderNode;
			}

			m_Device.ShaderInterfaceProviderAdded += new Nuaj.Device.ShaderInterfaceEventHandler( Device_ShaderInterfaceProviderAdded );
			m_Device.ShaderInterfaceProviderRemoved += new Nuaj.Device.ShaderInterfaceEventHandler( Device_ShaderInterfaceProviderRemoved );

			treeViewObjects.ExpandAll();
		}

		void Device_ShaderInterfaceProviderAdded( IShaderInterfaceProvider _Provider )
		{
			TreeNode	ProviderNode = new TreeNode( _Provider.ToString() );
						ProviderNode.Tag = _Provider;
			m_ShaderInterfaceProvidersNode.Nodes.Add( ProviderNode );
			m_ShaderInterfaceProvider2TreeNode[_Provider] = ProviderNode;
		}

		void Device_ShaderInterfaceProviderRemoved( IShaderInterfaceProvider _Provider )
		{
			m_ShaderInterfaceProvidersNode.Nodes.Remove( m_ShaderInterfaceProvider2TreeNode[_Provider] );
		}

		#endregion

		#region Noise Computation

#if CREATE_NOISE_TEXTURES
		/// <summary>
		/// Creates a 3D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateNoiseTexture( int _NoiseIndex, out Vector4[,,] _CPUNoiseTexture )
		{
//			WMath.SimpleRNG.SetSeed( 1 );

			// Build the volume filled with noise
			Vector4[,,]	Noise = new Vector4[NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE];
			for ( int Z=0; Z < NOISE_TEXTURE_SIZE; Z++ )
				for ( int Y=0; Y < NOISE_TEXTURE_SIZE; Y++ )
					for ( int X=0; X < NOISE_TEXTURE_SIZE; X++ )
					{
//						Noise[X,Y,Z] = (float) WMath.SimpleRNG.GetNormal();
#if false
						Noise[X,Y,Z].X = 2.0f * (float) WMath.SimpleRNG.GetUniform() - 1.0f;
						Noise[X,Y,Z].Y = 2.0f * (float) WMath.SimpleRNG.GetUniform() - 1.0f;
						Noise[X,Y,Z].Z = 2.0f * (float) WMath.SimpleRNG.GetUniform() - 1.0f;
						Noise[X,Y,Z].W = 2.0f * (float) WMath.SimpleRNG.GetUniform() - 1.0f;
#else
						Noise[X,Y,Z].X = (float) WMath.SimpleRNG.GetUniform();
						Noise[X,Y,Z].Y = (float) WMath.SimpleRNG.GetUniform();
						Noise[X,Y,Z].Z = (float) WMath.SimpleRNG.GetUniform();
						Noise[X,Y,Z].W = (float) WMath.SimpleRNG.GetUniform();
#endif
					}

			// Build the 3D image and the 3D texture from it...
			Vector4[,,]	CPUTexture = new Vector4[NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE];
			_CPUNoiseTexture = CPUTexture;

			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{																							// (XYZ)
					_Color.X = Noise[_X,_Y,_Z].X;															// (000)
					_Color.Y = Noise[_X,(_Y+1) & (NOISE_TEXTURE_SIZE-1),_Z].X;								// (010)
					_Color.Z = Noise[_X,_Y,(_Z+1) & (NOISE_TEXTURE_SIZE-1)].X;								// (001)
					_Color.W = Noise[_X,(_Y+1) & (NOISE_TEXTURE_SIZE-1),(_Z+1) & (NOISE_TEXTURE_SIZE-1)].X;	// (011)
					CPUTexture[_X,_Y,_Z] = _Color;

					_Color = Noise[_X,_Y,_Z];

				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}
#else
		/// <summary>
		/// Creates a 3D noise texture by loading them from resources
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateNoiseTexture( int _NoiseIndex, out Vector4[,,] _CPUNoiseTexture )
		{
//			const float	GLOBAL_SCALE = 2.0f;

			// Build the volume filled with noise
			float[,,]	Noise = new float[NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE];

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
			for ( int Z=0; Z < NOISE_TEXTURE_SIZE; Z++ )
				for ( int Y=0; Y < NOISE_TEXTURE_SIZE; Y++ )
					for ( int X=0; X < NOISE_TEXTURE_SIZE; X++ )
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
			Vector4[,,]	CPUTexture = new Vector4[NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE];
			_CPUNoiseTexture = CPUTexture;

			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{																			// (XYZ)
					_Color.X = Noise[_X,_Y,_Z];												// (000)
					_Color.Y = Noise[_X,(_Y+1) & (NOISE_TEXTURE_SIZE-1),_Z];						// (010)
					_Color.Z = Noise[_X,_Y,(_Z+1) & (NOISE_TEXTURE_SIZE-1)];						// (001)
					_Color.W = Noise[_X,(_Y+1) & (NOISE_TEXTURE_SIZE-1),(_Z+1) & (NOISE_TEXTURE_SIZE-1)];	// (011)

					CPUTexture[_X,_Y,_Z] = _Color;
				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}
#endif

		/// <summary>
		/// Creates the large noise texture that encodes 4 octaves of the 16x16x16 standard noise
		/// </summary>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateLargeNoiseTexture()
		{
			// Build the volume filled with noise
			Vector3		UVW = Vector3.Zero;
			Vector3		Derivatives, SumDerivatives = Vector3.Zero;
			float[,,]	Noise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			float[,,]	SumNoise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						UVW.X = (float) X / LARGE_NOISE_SIZE;
						UVW.Y = (float) Y / LARGE_NOISE_SIZE;
						UVW.Z = (float) Z / LARGE_NOISE_SIZE;

						float	Value = 0.0f;

						float	TempValue = ComputeNoise( UVW, m_CPUNoiseTextures[0], out Derivatives );
						SumDerivatives = Derivatives;
						Value += 1.0f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 2.0f * UVW, m_CPUNoiseTextures[1], out Derivatives );
						SumDerivatives += 0.5f * Derivatives;
						Value += 0.5f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 4.0f * UVW, m_CPUNoiseTextures[2], out Derivatives );
						SumDerivatives += 0.25f * Derivatives;
						Value += 0.25f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 8.0f * UVW, m_CPUNoiseTextures[3], out Derivatives );
						SumDerivatives += 0.125f * Derivatives;
						Value += 0.125f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						Noise[X,Y,Z] = Value;

						if ( Y > 0 )
							SumNoise[X,Y,Z] = SumNoise[X,Y-1,Z] + Value;
						else
							SumNoise[X,Y,Z] = 0.0f;
					}

// 			Vector4[,,]	SHNoise = new Vector4[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
// 			ComputeSHNoise( Noise, SHNoise );

			Vector3[,,]	PDNoise = new Vector3[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			ComputePrincipalDirectionsNoise( Noise, PDNoise );

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "LargeNoiseImage", LARGE_NOISE_SIZE, LARGE_NOISE_SIZE, LARGE_NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{
					_Color.X = Noise[_X,_Y,_Z];

//					_Color.Y = SumNoise[_X,_Y,_Z];

// 					_Color.Y = SHNoise[_X,_Y,_Z].X;
// 					_Color.Z = SHNoise[_X,_Y,_Z].Y;
// 					_Color.W = SHNoise[_X,_Y,_Z].Z;

					_Color.Y = PDNoise[_X,_Y,_Z].X;
					_Color.Z = PDNoise[_X,_Y,_Z].Y;
					_Color.W = PDNoise[_X,_Y,_Z].Z;

				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "LargeNoise", NoiseImage ) );
			}
		}

		/// <summary>
		/// Creates the large noise texture that encodes 4 octaves of the 16x16x16 standard noise
		/// </summary>
		/// <returns></returns>
		public Texture3D<PF_R16F>		CreateLargeNoiseTexture2()
		{
			// Build the volume filled with noise
			Vector3		UVW = Vector3.Zero;
			Vector3		Derivatives, SumDerivatives = Vector3.Zero;
			float[,,]	Noise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			float[,,]	SumNoise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						UVW.X = (float) X / LARGE_NOISE_SIZE;
						UVW.Y = (float) Y / LARGE_NOISE_SIZE;
						UVW.Z = (float) Z / LARGE_NOISE_SIZE;

						float	Value = 0.0f;

						float	TempValue = ComputeNoise( UVW, m_CPUNoiseTextures[0], out Derivatives );
						SumDerivatives = Derivatives;
						Value += 1.0f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 2.0f * UVW, m_CPUNoiseTextures[1], out Derivatives );
						SumDerivatives += 0.5f * Derivatives;
						Value += 0.5f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 4.0f * UVW, m_CPUNoiseTextures[2], out Derivatives );
						SumDerivatives += 0.25f * Derivatives;
						Value += 0.25f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 8.0f * UVW, m_CPUNoiseTextures[3], out Derivatives );
						SumDerivatives += 0.125f * Derivatives;
						Value += 0.125f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						Noise[X,Y,Z] = Value;

						if ( Y > 0 )
							SumNoise[X,Y,Z] = SumNoise[X,Y-1,Z] + Value;
						else
							SumNoise[X,Y,Z] = 0.0f;
					}

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_R16F>	NoiseImage = new Image3D<PF_R16F>( m_Device, "LargeNoiseImage", LARGE_NOISE_SIZE, LARGE_NOISE_SIZE, LARGE_NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{
					_Color.X = Noise[_X,_Y,_Z];
				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_R16F>( m_Device, "LargeNoise", NoiseImage ) );
			}
		}

		/// <summary>
		/// Computes the accumulated density in several directions and encodes it into SH using 2 bands (i.e. 4 coefficients)
		/// </summary>
		/// <param name="_SHNoise"></param>
		protected void	ComputeSHNoise( float[,,] _SourceNoise, Vector4[,,] _SHNoise )
		{
			System.IO.FileInfo		SHNoiseFile = new System.IO.FileInfo( "Data/PIPO_DemoClouds.SHNoise" );

#if LOAD_PRECOMP_NOISE

			if ( !SHNoiseFile.Exists )
				return;

			// Load the result
			System.IO.FileStream	Stream = SHNoiseFile.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						_SHNoise[X,Y,Z].X = Reader.ReadSingle();
						_SHNoise[X,Y,Z].Y = Reader.ReadSingle();
						_SHNoise[X,Y,Z].Z = Reader.ReadSingle();
						_SHNoise[X,Y,Z].W = Reader.ReadSingle();
					}

			Reader.Close();
			Reader.Dispose();
			Stream.Dispose();

#else
/*
			SphericalHarmonics.SHSamplesCollection	SHSamples = new SphericalHarmonics.SHSamplesCollection( 1 );
			SHSamples.Initialize( 2, SH_NOISE_ENCODING_SAMPLES_COUNT );

			Vector3		CurrentPosition, Step;
			float		DirY, HitDistance, MarchDistance;
			double[]	SH = new double[4];

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						SH[0] = 0.0; SH[1] = 0.0; SH[2] = 0.0; SH[3] = 0.0;
						foreach ( SphericalHarmonics.SHSamplesCollection.SHSample Sample in SHSamples )
						{
							CurrentPosition.X = X + 0.5f;
							CurrentPosition.Y = Y + 0.5f;
							CurrentPosition.Z = Z + 0.5f;
							Step.X = Sample.m_Direction.x;
							Step.Y = Sample.m_Direction.y;
							Step.Z = Sample.m_Direction.z;

							DirY = Math.Max( 1e-4f, Math.Abs( Step.Y ) );
							HitDistance = Sample.m_Direction.y > 0.0f ? Y / DirY : (LARGE_NOISE_SIZE - Y) / DirY;
							MarchDistance = Math.Min( LARGE_NOISE_SIZE, HitDistance );

							int		MarchStepsCount = (int) Math.Floor( MarchDistance );
							double	OpticalDepth = 0.0;
							for ( int StepIndex=0; StepIndex < MarchStepsCount; StepIndex++ )
							{
								OpticalDepth += SampleLargeNoise( ref CurrentPosition, _SourceNoise );
								CurrentPosition.X += Step.X;
								CurrentPosition.Y += Step.Y;
								CurrentPosition.Z += Step.Z;
							}
							OpticalDepth /= LARGE_NOISE_SIZE;

							// Encode into SH
							SH[0] += OpticalDepth * Sample.m_SHFactors[0];
							SH[1] += OpticalDepth * Sample.m_SHFactors[1];
							SH[2] += OpticalDepth * Sample.m_SHFactors[2];
							SH[3] += OpticalDepth * Sample.m_SHFactors[3];
						}
						SH[0] /= SHSamples.SamplesCount;
						SH[1] /= SHSamples.SamplesCount;
						SH[2] /= SHSamples.SamplesCount;
						SH[3] /= SHSamples.SamplesCount;

						_SHNoise[X,Y,Z] = new Vector4( (float) SH[1], (float) SH[2], (float) SH[3], (float) SH[0] );
					}
*/

			// Faster precomputation than SH
			// => Precompute 

			// Save the result
			System.IO.FileStream	Stream = SHNoiseFile.OpenWrite();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						Writer.Write( _SHNoise[X,Y,Z].X );
						Writer.Write( _SHNoise[X,Y,Z].Y );
						Writer.Write( _SHNoise[X,Y,Z].Z );
						Writer.Write( _SHNoise[X,Y,Z].W );
					}

			Writer.Close();
			Writer.Dispose();
			Stream.Dispose();

#endif
		}

		/// <summary>
		/// Computes the accumulated density in 3 principal directions
		/// </summary>
		/// <param name="_SourceNoise"></param>
		/// <param name="_SHNoise"></param>
		protected void	ComputePrincipalDirectionsNoise( float[,,] _SourceNoise, Vector3[,,] _PDNoise )
		{
			System.IO.FileInfo		PDNoiseFile = new System.IO.FileInfo( "Data/DemoClouds.PDNoise" );

#if LOAD_PRECOMP_NOISE

			if ( !PDNoiseFile.Exists )
				return;

			// Load the result
			System.IO.FileStream	Stream = PDNoiseFile.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						_PDNoise[X,Y,Z].X = Reader.ReadSingle();
						_PDNoise[X,Y,Z].Y = Reader.ReadSingle();
						_PDNoise[X,Y,Z].Z = Reader.ReadSingle();
					}

			Reader.Close();
			Reader.Dispose();
			Stream.Dispose();

#else

			int		BEAM_COUNT = 32;
			float	BEAM_OFF_ANGLE = 10.0f * (float) Math.PI / 180.0f;
			float	STEP_SIZE = 2.0f;

			Random		RNG = new Random( 1 );

			// These are the 3 main directions as defined in http://www2.ati.com/developer/gdc/D3DTutorial10_Half-Life2_Shading.pdf
			Vector3	PX = new Vector3( (float) Math.Sqrt( 2.0 / 3.0 ), (float) Math.Sqrt( 1.0 / 3.0 ), 0.0f );
			Vector3	PY = new Vector3( -(float) Math.Sqrt( 1.0 / 6.0 ), (float) Math.Sqrt( 1.0 / 3.0 ), -(float) Math.Sqrt( 1.0 / 2.0 ) );
			Vector3	PZ = new Vector3( -(float) Math.Sqrt( 1.0 / 6.0 ), (float) Math.Sqrt( 1.0 / 3.0 ), (float) Math.Sqrt( 1.0 / 2.0 ) );

			Vector3[]	Basis = new Vector3[]
			{
				PX, PY, PZ
			};

			// Draw random vectors to make a beam about the central Y direction
			Vector3[]	Beam = new Vector3[BEAM_COUNT];
			float		SumWeights = 0.0f;
			for ( int BeamIndex=0; BeamIndex < BEAM_COUNT; BeamIndex++ )
			{
				float	Theta = BEAM_OFF_ANGLE * (float) RNG.NextDouble();
				float	Phi = 2.0f * (float) Math.PI * (float) RNG.NextDouble();
				Beam[BeamIndex] = new Vector3( (float) (Math.Cos( Phi ) * Math.Sin( Theta )), (float) Math.Cos( Theta ), (float) (Math.Cos( Phi ) * Math.Sin( Theta )) );
				SumWeights += Beam[BeamIndex].Y;
			}
			float	InvSumWeights = 1.0f / SumWeights;

			int			StepsCount;
			Vector3		DX, DY, DZ, BeamDirection, CurrentPosition, Step;
			float		HitDistance, Weight;
			double[]	SumDensities = new double[3];
			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						// Iterate through the 3 main directions
						for ( int MainDirectionIndex=0; MainDirectionIndex < 3; MainDirectionIndex++ )
						{
							DX = Basis[(MainDirectionIndex+2)%3];
							DY = Basis[MainDirectionIndex+0];
							DZ = Basis[(MainDirectionIndex+1)%3];

							// Trace along all directions of the beam
							SumDensities[MainDirectionIndex] = 0.0;
							for ( int BeamIndex=0; BeamIndex < BEAM_COUNT; BeamIndex++ )
							{
								BeamDirection = Beam[BeamIndex].X * DX + Beam[BeamIndex].Y * DY + Beam[BeamIndex].Z * DZ;
								Weight = Beam[BeamIndex].Y;	// Weight is a measure of off-axis

								// Compute distance at which the beam escapes through the top of the cloud
								HitDistance = Y / BeamDirection.Y;
								StepsCount = (int) Math.Floor( HitDistance / STEP_SIZE );
								Step = STEP_SIZE * BeamDirection;

								CurrentPosition.X = X + 0.5f;
								CurrentPosition.Y = Y + 0.5f;
								CurrentPosition.Z = Z + 0.5f;

								double	SumDensity = 0.0;
								for ( int StepIndex=0; StepIndex < StepsCount; StepIndex++ )
								{
									SumDensity += STEP_SIZE * SampleLargeNoise( ref CurrentPosition, _SourceNoise );
									CurrentPosition.X += Step.X;
									CurrentPosition.Y -= Step.Y;
									CurrentPosition.Z += Step.Z;
								}
								SumDensities[MainDirectionIndex] += Weight * SumDensity / LARGE_NOISE_SIZE;
							}
							SumDensities[MainDirectionIndex] *= InvSumWeights;
						}

						_PDNoise[X,Y,Z] = new Vector3( (float) SumDensities[0], (float) SumDensities[1], (float) SumDensities[2] );
					}

			// Save the result
			System.IO.FileStream	Stream = PDNoiseFile.OpenWrite();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						Writer.Write( _PDNoise[X,Y,Z].X );
						Writer.Write( _PDNoise[X,Y,Z].Y );
						Writer.Write( _PDNoise[X,Y,Z].Z );
					}

			Writer.Close();
			Writer.Dispose();
			Stream.Dispose();

#endif
		}

		protected float	SampleLargeNoise( ref Vector3 _Position, float[,,] _Noise )
		{
			int		X = (int) Math.Floor( _Position.X );
			int		Y = (int) Math.Floor( _Position.Y );
			int		Z = (int) Math.Floor( _Position.Z );
			float	dX = _Position.X - X;
			float	dY = _Position.Y - Y;
			float	dZ = _Position.Z - Z;
			float	rdX = 1.0f - dX;
			float	rdY = 1.0f - dY;
			float	rdZ = 1.0f - dZ;
			X = X & (LARGE_NOISE_SIZE-1);
			Y = Y & (LARGE_NOISE_SIZE-1);
			Z = Z & (LARGE_NOISE_SIZE-1);
			int		NX = (X+1) & (LARGE_NOISE_SIZE-1);
			int		NY = (Y+1) & (LARGE_NOISE_SIZE-1);
			int		NZ = (Z+1) & (LARGE_NOISE_SIZE-1);

			float	V000 = _Noise[X,Y,Z];
			float	V001 = _Noise[NX,Y,Z];
			float	V011 = _Noise[NX,NY,Z];
			float	V010 = _Noise[X,NY,Z];
			float	V100 = _Noise[X,Y,NZ];
			float	V101 = _Noise[NX,Y,NZ];
			float	V111 = _Noise[NX,NY,NZ];
			float	V110 = _Noise[X,NY,NZ];

			float	V00 = rdX * V000 + dX * V001;
			float	V01 = rdX * V010 + dX * V011;
			float	V0 = rdY * V00 + dX * V01;
			float	V10 = rdX * V100 + dX * V101;
			float	V11 = rdX * V110 + dX * V111;
			float	V1 = rdY * V10 + dX * V11;

			return rdZ * V0 + dZ * V1;
		}

		// Noise + Derivatives
		// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
		//
		protected float ComputeNoise2( Vector3 _UVW, Vector4[,,] _Noise, out Vector3 _Derivatives )
		{
			_UVW *= NOISE_TEXTURE_SIZE;

			int		X = (int) Math.Floor( _UVW.X );
			int		Y = (int) Math.Floor( _UVW.Y );
			int		Z = (int) Math.Floor( _UVW.Z );

			Vector3	uvw;
			uvw.X = _UVW.X - X;
			uvw.Y = _UVW.Y - Y;
			uvw.Z = _UVW.Z - Z;

			X &= (NOISE_TEXTURE_SIZE-1);
			Y &= (NOISE_TEXTURE_SIZE-1);
			Z &= (NOISE_TEXTURE_SIZE-1);
			int		NX = (X+1) & (NOISE_TEXTURE_SIZE-1);

			Vector4	N0 = _Noise[X,Y,Z];
			Vector4	N1 = _Noise[NX,Y,Z];

			// Quintic interpolation from Ken Perlin :
			//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
			//	du/dx = 30x^4 - 60x^3 + 30x^2
			//
			Vector3	dudvdw;
			dudvdw.X = 30.0f*uvw.X*uvw.X*(uvw.X*(uvw.X-2.0f)+1.0f);
			dudvdw.Y = 30.0f*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y-2.0f)+1.0f);
			dudvdw.Z = 30.0f*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z-2.0f)+1.0f);

			uvw.X = uvw.X*uvw.X*uvw.X*(uvw.X*(uvw.X*6.0f-15.0f)+10.0f);
			uvw.Y = uvw.Y*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y*6.0f-15.0f)+10.0f);
			uvw.Z = uvw.Z*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z*6.0f-15.0f)+10.0f);

			float	a = N0.X;
			float	b = N1.X;
			float	c = N0.Y;
			float	d = N1.Y;
			float	e = N0.Z;
			float	f = N1.Z;
			float	g = N0.W;
			float	h = N1.W;

			float	k0 =   a;
			float	k1 =   b - a;
			float	k2 =   c - a;
			float	k3 =   e - a;
			float	k4 =   a - b - c + d;
			float	k5 =   a - c - e + g;
			float	k6 =   a - b - e + f;
			float	k7 = - a + b + c - d + e - f - g + h;

			_Derivatives.X = dudvdw.X * (k1 + k4*uvw.Y + k6*uvw.Z + k7*uvw.Y*uvw.Z);
			_Derivatives.Y = dudvdw.Y * (k2 + k5*uvw.Z + k4*uvw.X + k7*uvw.Z*uvw.X);
			_Derivatives.Z = dudvdw.Z * (k3 + k6*uvw.X + k5*uvw.Y + k7*uvw.X*uvw.Y);

			return k0 + k1*uvw.X + k2*uvw.Y + k3*uvw.Z + k4*uvw.X*uvw.Y + k5*uvw.Y*uvw.Z + k6*uvw.Z*uvw.X + k7*uvw.X*uvw.Y*uvw.Z;
		}

		protected float ComputeNoise( Vector3 _UVW, Vector4[,,] _Noise, out Vector3 _Derivatives ) 
		{
			_UVW *= NOISE_TEXTURE_SIZE;

			int		X = (int) Math.Floor( _UVW.X );
			int		Y = (int) Math.Floor( _UVW.Y );
			int		Z = (int) Math.Floor( _UVW.Z );

			Vector3	uvw;
			uvw.X = _UVW.X - X;
			uvw.Y = _UVW.Y - Y;
			uvw.Z = _UVW.Z - Z;

			X &= (NOISE_TEXTURE_SIZE-1);
			Y &= (NOISE_TEXTURE_SIZE-1);
			Z &= (NOISE_TEXTURE_SIZE-1);
			int		NX = (X+1) & (NOISE_TEXTURE_SIZE-1);

			Vector4	N0 = _Noise[X,Y,Z];
			Vector4	N1 = _Noise[NX,Y,Z];

// 			// Quintic interpolation from Ken Perlin :
// 			//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
// 			//	du/dx = 30x^4 - 60x^3 + 30x^2
// 			//
// 			Vector3	dudvdw;
// 			dudvdw.X = 30.0f*uvw.X*uvw.X*(uvw.X*(uvw.X-2.0f)+1.0f);
// 			dudvdw.Y = 30.0f*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y-2.0f)+1.0f);
// 			dudvdw.Z = 30.0f*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z-2.0f)+1.0f);
// 
// 			uvw.X = uvw.X*uvw.X*uvw.X*(uvw.X*(uvw.X*6.0f-15.0f)+10.0f);
// 			uvw.Y = uvw.Y*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y*6.0f-15.0f)+10.0f);
// 			uvw.Z = uvw.Z*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z*6.0f-15.0f)+10.0f);

			float	a = N0.X;
			float	b = N1.X;
			float	c = N0.Y;
			float	d = N1.Y;
			float	e = N0.Z;
			float	f = N1.Z;
			float	g = N0.W;
			float	h = N1.W;

			float	k0 =   a;
			float	k1 =   b - a;
			float	k2 =   c - a;
			float	k3 =   e - a;
			float	k4 =   a - b - c + d;
			float	k5 =   a - c - e + g;
			float	k6 =   a - b - e + f;
			float	k7 = - a + b + c - d + e - f - g + h;

			_Derivatives.X = 0.0f;
			_Derivatives.Y = 0.0f;
			_Derivatives.Z = 0.0f;

			return k0 + k1*uvw.X + k2*uvw.Y + k3*uvw.Z + k4*uvw.X*uvw.Y + k5*uvw.Y*uvw.Z + k6*uvw.Z*uvw.X + k7*uvw.X*uvw.Y*uvw.Z;
		}

// Uncorrelated noise version
// 		protected void	BuildNoiseArray()
// 		{
// 			int	TableSize = 8;
// 
// 			// Build the permutation table
// 			Random	RNG = new Random( 17 );
// 			int[]	Permutations = new int[TableSize];
// 			for ( int i=0; i < TableSize; i++ )
// 				Permutations[i] = i;	// Initialize
// 			for ( int SlotIndex=0; SlotIndex < TableSize; SlotIndex++ )
// 			{
// 				int	RandomIndex = RNG.Next( TableSize );
// 
// 				// Random swap
// 				int	Temp = Permutations[RandomIndex];
// 				Permutations[RandomIndex] = Permutations[SlotIndex];
// 				Permutations[SlotIndex] = Temp;
// 			}
// 
// 			// Build the tables
// 			string	Table = "float4	NoiseTable[" + (TableSize+1) + "] = { ";
// 			string	PermutationsTable = "";
// 			string	FirstValue = "";
// 			for ( int i=0; i < TableSize; i++ )
// 			{
// 				string	Value = "float4( " + RNG.NextDouble().ToString() + ", "  + RNG.NextDouble().ToString() + ", " + RNG.NextDouble().ToString() + ", " + RNG.NextDouble().ToString() + " )";
// 				if ( i == 0 )
// 				{
// 					FirstValue = Value;
// 					Table += Value;
// 					PermutationsTable += Permutations[i];
// 				}
// 				else
// 				{
// 					Table += ", " + Value;
// 					PermutationsTable += ", " + Permutations[i];
// 				}
// 			}
// 
// 			Table += ", " + FirstValue + " };";
// 			PermutationsTable = "int	PermutationsTable[" + (2*TableSize) + "] = { " + PermutationsTable + ", " + PermutationsTable + " };";
// 		}

		// Correlated noise version
		// Here, we build a noise array of float2 where X is the current noise value fetched by N[p[x]]
		//	and y is the next noise value fetched by N[p[x+1]]
		//
		// It's possible since we know that, to fetch Noise[index], index comes from a single place inside the permutation table
		// which we can retrieve. From index=p[x] we thus retrieve x and this also gives us p[x+1] so we can determine N[p[x+1]]
		// This is possible because there is a bijection between x and p[x].
		//
		// Unfortunately, this stops here since z=p[p[x]+y] is NOT bijective : there are several x and y values that can lead to the
		//	same z and we cannot retrieve a unique (x,y) for each z...
		//
		// Anyway, this saves us half the operations on permutation tables, which is not negligible.
		//
		protected void	BuildNoiseArray()
		{
			int	TableSize = 16;

			// Build the permutation table
			Random	RNG = new Random( 17 );
			int[]	Permutations = new int[TableSize];
			int[]	PermutationIndices = new int[TableSize];
			for ( int i=0; i < TableSize; i++ )
				Permutations[i] = i;	// Initialize
			for ( int SlotIndex=0; SlotIndex < TableSize; SlotIndex++ )
			{
				int	RandomIndex = RNG.Next( TableSize );

				// Random swap
				int	Temp = Permutations[RandomIndex];
				Permutations[RandomIndex] = Permutations[SlotIndex];
				Permutations[SlotIndex] = Temp;
			}

			// Build the reverse permutation table that allows us to retrieve
			//	the index in the table where a permutation value has a specific value
			for ( int SlotIndex=0; SlotIndex < TableSize; SlotIndex++ )
				PermutationIndices[Permutations[SlotIndex]] = SlotIndex;

			// Build the tables
			float[]	RandomTable = new float[TableSize];
			for ( int i=0; i < TableSize; i++ )
				RandomTable[i] = (float) RNG.NextDouble();

			string	Table = "float2	NoiseTable[" + (TableSize+1) + "] = { ";
			string	PermutationsTable = "";
			string	FirstValue = "";
			for ( int i=0; i < TableSize; i++ )
			{
				int	PermutationIndex = PermutationIndices[i];	// This is the index in the permutation table where we find the permutation index that will fetch the noise at index i
				int	i2 = Permutations[(PermutationIndex+1) & (TableSize-1)];	// This the "next" noise value the permutation table will give us

				string	Value = "float2( " + RandomTable[i].ToString() + ", "  + RandomTable[i2].ToString() + " )";
				if ( i == 0 )
				{
					FirstValue = Value;
					Table += Value;
					PermutationsTable += Permutations[i];
				}
				else
				{
					Table += ", " + Value;
					PermutationsTable += ", " + Permutations[i];
				}
			}

			Table += ", " + FirstValue + " };";
			PermutationsTable = "int		PermutationsTable[" + (2*TableSize) + "] = { " + PermutationsTable + ", " + PermutationsTable + " };";
		}

		#endregion

		#region Terrain

		protected BoundingBox	m_TerrainAABB;

		/// <summary>
		/// This creates a simple fbm terrain as a fixed size mesh
		/// </summary>
		protected void	CreateTerrainPrimitive()
		{
			VS_P3N3[]	Vertices = new VS_P3N3[(TERRAIN_TILES_COUNT+1)*(TERRAIN_TILES_COUNT+1)];
			int[]		Indices = new int[TERRAIN_TILES_COUNT*2*(TERRAIN_TILES_COUNT+2)];

			// Compute vertices
			m_TerrainAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			Vector3	P = Vector3.Zero;
			for ( int Z=0; Z <= TERRAIN_TILES_COUNT; Z++ )
			{
				P.Z = TERRAIN_SCALE_HORIZONTAL * ((float) Z / TERRAIN_TILES_COUNT - 0.5f);
				for ( int X=0; X <= TERRAIN_TILES_COUNT; X++ )
				{
					P.X = TERRAIN_SCALE_HORIZONTAL * ((float) X / TERRAIN_TILES_COUNT - 0.5f);
					P.Y = SampleTerrain( X, Z );
					Vertices[(TERRAIN_TILES_COUNT+1)*Z+X] = new VS_P3N3() { Position = P };

					m_TerrainAABB.Minimum = Vector3.Min( m_TerrainAABB.Minimum, P );
					m_TerrainAABB.Maximum = Vector3.Max( m_TerrainAABB.Maximum, P );
				}
			}

			m_RenderTechniqueClouds.TerrainAABB = m_TerrainAABB;

			// Redo a pass to compute normals
			Vector3	DX = new Vector3( 2.0f, 0.0f, 0.0f );
			Vector3	DZ = new Vector3( 0.0f, 0.0f, 2.0f );
			Vector3	N = Vector3.Zero;
			for ( int Z=0; Z <= TERRAIN_TILES_COUNT; Z++ )
			{
				int	PZ = (Z+TERRAIN_TILES_COUNT)%(TERRAIN_TILES_COUNT+1);
				int	NZ = (Z+1)%(TERRAIN_TILES_COUNT+1);

				for ( int X=0; X <= TERRAIN_TILES_COUNT; X++ )
				{
					int	PX = (X+TERRAIN_TILES_COUNT)%(TERRAIN_TILES_COUNT+1);
					int	NX = (X+1)%(TERRAIN_TILES_COUNT+1);

					float	HPX = Vertices[(TERRAIN_TILES_COUNT+1)*Z+PX].Position.Y / TERRAIN_SCALE_VERTICAL;
					float	HNX = Vertices[(TERRAIN_TILES_COUNT+1)*Z+NX].Position.Y / TERRAIN_SCALE_VERTICAL;
					float	HPZ = Vertices[(TERRAIN_TILES_COUNT+1)*PZ+X].Position.Y / TERRAIN_SCALE_VERTICAL;
					float	HNZ = Vertices[(TERRAIN_TILES_COUNT+1)*NZ+X].Position.Y / TERRAIN_SCALE_VERTICAL;

					DX.Y = HNX - HPX;
					DZ.Y = HNZ - HPZ;
					N = Vector3.Cross( DZ, DX );

					Vertices[(TERRAIN_TILES_COUNT+1)*Z+X].Normal = N;
				}
			}

			// Build indices
			int	IndexCount = 0;
			for ( int Z=0; Z < TERRAIN_TILES_COUNT; Z++ )
			{
				for ( int X=0; X < TERRAIN_TILES_COUNT; X++ )
				{
					Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+0)+X;
					Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+X;
				}

				// Finalize strip
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+0)+TERRAIN_TILES_COUNT;
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+TERRAIN_TILES_COUNT;

				// Add 2 degenerate points to link to next strip
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+TERRAIN_TILES_COUNT;
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+0;
			}

			m_Terrain = ToDispose( new Primitive<VS_P3N3,int>( m_Device, "Terrain", PrimitiveTopology.TriangleStrip, Vertices, Indices ) );
		}

		protected float	SampleTerrain( float _X, float _Z )
		{
			Vector3	Pos = new Vector3( TERRAIN_NOISE_SCALE * _X, 0.0f, TERRAIN_NOISE_SCALE * _Z );
			Vector3	TempNormal;
			float	Value = 0.0f;
			Value += 1.0f   * ComputeNoise2( 1.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );
			Value += 0.5f   * ComputeNoise2( 2.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );
			Value += 0.25f  * ComputeNoise2( 4.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );
			Value += 0.125f * ComputeNoise2( 8.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );

			return TERRAIN_SCALE_VERTICAL * Value + TERRAIN_OFFSET_VERTICAL;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		private void Device_MaterialEffectRecompiled( object sender, EventArgs e )
		{
			if ( richTextBoxOutput.InvokeRequired )
			{
				richTextBoxOutput.BeginInvoke( new EventHandler( Device_MaterialEffectRecompiled ), sender, e );
				return;
			}

			IMaterial	M = sender as IMaterial;
			richTextBoxOutput.Log( DateTime.Now.ToString( "HH:mm:ss" ) + " > \"" + M.ToString() + "\" recompiled...\r\n" );
			if ( M.HasErrors )
				richTextBoxOutput.LogError( "ERRORS:\r\n" + M.CompilationErrors );
			else if ( M.CompilationErrors != "" )
				richTextBoxOutput.LogWarning( "WARNINGS:\r\n" + M.CompilationErrors );
			else
				richTextBoxOutput.LogSuccess( "0 error...\r\n" );
			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );
		}

		private void treeViewObjects_AfterSelect( object sender, TreeViewEventArgs e )
		{
			propertyGrid.SelectedObject = e.Node.Tag;
		}

		protected DateTime	m_LightningStrikeTime = DateTime.Today;
		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			// Lightning strike !
			if ( e.KeyCode == Keys.Space )
				m_LightningStrikeTime = DateTime.Now;
		}


		protected void	InitializeTrackbars()
		{
			// Page 1
			m_RenderTechniqueClouds.SunPhi = -(float) Math.PI / 180.0f * floatTrackbarControlSunAzimuth.Value;
			m_RenderTechniqueClouds.SunTheta = (float) Math.PI / 180.0f * floatTrackbarControlSunElevation.Value;

			m_RenderTechniqueClouds.CloudAltitudeBottomKm = floatTrackbarControlCloudAltitude.Value;
			m_RenderTechniqueClouds.CloudThicknessKm = floatTrackbarControlCloudSize.Value;
			m_RenderTechniqueClouds.CoverageOffsetTop = floatTrackbarControlCoverageOffsetTop.Value;
			m_RenderTechniqueClouds.CoverageOffsetBottom = floatTrackbarControlCoverageOffsetBottom.Value;
			m_RenderTechniqueClouds.CoverageOffsetPow = floatTrackbarControlCoverageOffsetPow.Value;
			m_RenderTechniqueClouds.CoverageContrast = floatTrackbarControlCoverageContrast.Value;

			m_RenderTechniqueClouds.CloudDensity = floatTrackbarControlCloudDensity.Value;
			m_RenderTechniqueClouds.CloudAlbedo = floatTrackbarControlCloudAlbedo.Value;
			m_RenderTechniqueClouds.SkyDensityMie = 1e-4f * floatTrackbarControlFogAmount.Value;
			m_RenderTechniqueClouds.SkyDensityRayleigh = 1e-5f * floatTrackbarControlAerosolsAmount.Value;
			m_RenderTechniqueClouds.ShadowOpacity = floatTrackbarControlShadowOpacity.Value;

			m_RenderTechniqueClouds.IsotropicFactor = floatTrackbarControlIsotropicFactor.Value;
			m_RenderTechniqueClouds.DirectionalFactor = floatTrackbarControlDirectionalFactor.Value;

			// Page 2
			m_ToneMapping.AverageOrMax = floatTrackbarControlToneMapAvgMax.Value;
			SetDebugType();
			m_ToneMapping.DebugLuminanceMarker = floatTrackbarControlDEBUGLuminanceMarker.Value;
			m_ToneMapping.DebugLuminanceMarkerTolerance = floatTrackbarControlDEBUGLuminanceMarkerTolerance.Value;
			floatTrackbarControlWavelength_ValueChanged( null, 0 );
		}

		#region Page 1

		private void floatTrackbarControlSunAzimuth_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.SunPhi = -(float) Math.PI / 180.0f * _Sender.Value;
		}

		private void floatTrackbarControlSunElevation_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.SunTheta = (float) Math.PI / 180.0f * _Sender.Value;
		}

		private void floatTrackbarControlCoverage_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
//			m_RenderTechniqueClouds.CoverageOffset = Lerp( -0.4f, 0.6f, _Sender.Value );
			m_RenderTechniqueClouds.CoverageOffsetTop = floatTrackbarControlCoverageOffsetTop.Value;
			m_RenderTechniqueClouds.CoverageOffsetBottom = floatTrackbarControlCoverageOffsetBottom.Value;
		}

		private void floatTrackbarControlCoverageOffsetPow_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CoverageOffsetPow = floatTrackbarControlCoverageOffsetPow.Value;
		}

		private void floatTrackbarControlCloudDensity_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CoverageContrast = _Sender.Value;
		}

		private void floatTrackbarControlCloudExtinction_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CloudDensity = _Sender.Value;
		}

		private void floatTrackbarControlCloudScattering_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CloudAlbedo = _Sender.Value;
		}

		private void floatTrackbarControlFogAmount_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.SkyDensityMie = 1e-4f * floatTrackbarControlFogAmount.Value;
		}

		private void floatTrackbarControlAerosolsAmount_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.SkyDensityRayleigh = 1e-5f * floatTrackbarControlAerosolsAmount.Value;
		}

		private void floatTrackbarControlCloudSize_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CloudThicknessKm = floatTrackbarControlCloudSize.Value;
		}

		private void floatTrackbarControlCloudAltitude_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CloudAltitudeBottomKm = floatTrackbarControlCloudAltitude.Value;
		}

		private void floatTrackbarControlDensitySumFactor_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.ShadowOpacity = floatTrackbarControlShadowOpacity.Value;
		}

		private void floatTrackbarControlIsotropicFactor_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.IsotropicFactor = floatTrackbarControlIsotropicFactor.Value;
		}

		private void floatTrackbarControlDirectionalFactor_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.DirectionalFactor = floatTrackbarControlDirectionalFactor.Value;
		}

		protected float		Lerp( float _v0, float _v1, float _t )
		{
			return _v0 + (_v1 - _v0) * _t;
		}

		private void buttonProfiler_Click( object sender, EventArgs e )
		{
			if ( !m_ProfilerForm.Visible )
				m_ProfilerForm.Show( this );
			else
				m_ProfilerForm.Close();
		}

		private void buttonToneMappingSetup_Click( object sender, EventArgs e )
		{
			if ( !m_ToneMappingForm.Visible )
				m_ToneMappingForm.Show( this );
			else
				m_ToneMappingForm.Close();
		}

		private void buttonShadowMapViewer_Click( object sender, EventArgs e )
		{
			if ( !m_ShadowForm.Visible )
				m_ShadowForm.Show( this );
			else
				m_ShadowForm.Close();
		}

		#endregion

		#region Page 2

		private void floatTrackbarControlToneMapAvgMax_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_ToneMapping.AverageOrMax = _Sender.Value;
		}

		private void radioButtonDEBUGNone_CheckedChanged( object sender, EventArgs e )
		{
			SetDebugType();
		}

		private void radioButtonDEBUGLuminanceNormalized_CheckedChanged( object sender, EventArgs e )
		{
			SetDebugType();
		}

		private void radioButtonDEBUGLuminanceCustom_CheckedChanged( object sender, EventArgs e )
		{
			SetDebugType();
		}

		private void radioButtonRGBRampFullscreen_CheckedChanged( object sender, EventArgs e )
		{
			SetDebugType();
		}

		private void radioButtonRGBRampInset_CheckedChanged( object sender, EventArgs e )
		{
			SetDebugType();
		}

		protected void	SetDebugType()
		{
			if ( radioButtonDEBUGNone.Checked )
				m_ToneMapping.DebugType = Nuaj.Cirrus.Utility.RenderTechniquePostProcessToneMappingFilmic.DEBUG_TYPE.DISABLED;
			else if ( radioButtonDEBUGLuminanceNormalized.Checked )
				m_ToneMapping.DebugType = Nuaj.Cirrus.Utility.RenderTechniquePostProcessToneMappingFilmic.DEBUG_TYPE.LUMINANCE_NORMALIZED;
			else if ( radioButtonDEBUGLuminanceCustom.Checked )
				m_ToneMapping.DebugType = Nuaj.Cirrus.Utility.RenderTechniquePostProcessToneMappingFilmic.DEBUG_TYPE.LUMINANCE_CUSTOM;
			else if ( radioButtonRGBRampFullscreen.Checked )
				m_ToneMapping.DebugType = Nuaj.Cirrus.Utility.RenderTechniquePostProcessToneMappingFilmic.DEBUG_TYPE.GRADIENTS_FULLSCREEN;
			else if ( radioButtonRGBRampInset.Checked )
				m_ToneMapping.DebugType = Nuaj.Cirrus.Utility.RenderTechniquePostProcessToneMappingFilmic.DEBUG_TYPE.GRADIENTS_INSET;
		}

		private void floatTrackbarControlDEBUGLuminanceMarker_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_ToneMapping.DebugLuminanceMarker = _Sender.Value;
		}

		private void floatTrackbarControlDEBUGLuminanceMarkerTolerance_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_ToneMapping.DebugLuminanceMarkerTolerance = _Sender.Value;
		}

		private void buttonRebuildSkyDensity_Click( object sender, EventArgs e )
		{
			m_RenderTechniqueClouds.Sky.BuildDensityTexture( ( double _AltitudeKm ) =>
				{
					return Math.Exp( -Math.Max( 0.0, _AltitudeKm + floatTrackbarControlSkyDensityAltitudeOffset.Value ) / floatTrackbarControlSkyDensityAerosolsFactor.Value );
				} );
		}

		private void buttonRebuildLinear_Click( object sender, EventArgs e )
		{
			m_RenderTechniqueClouds.Sky.BuildDensityTexture( ( double _AltitudeKm ) =>
				{
					return floatTrackbarControlSkyDensityAerosolsFactor.Value;
				} );
		}

		private void floatTrackbarControlWavelength_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.Sky.Wavelengths = 0.001f * new Vector3( floatTrackbarControlWavelengthR.Value, floatTrackbarControlWavelengthG.Value, floatTrackbarControlWavelengthB.Value );
		}

		private void buttonCloudProfiler_Click( object sender, EventArgs e )
		{
			if ( !m_CloudProfilerForm.Visible )
				m_CloudProfilerForm.Show( this );
			else
				m_CloudProfilerForm.Close();
		}

		#endregion

		private void buttonGoToPage1_Click( object sender, EventArgs e )
		{
			panelSettings2.Visible = false;
			panelSettings1.Visible = true;
		}

		private void buttonGoToPage2_Click( object sender, EventArgs e )
		{
			panelSettings1.Visible = false;
			panelSettings2.Visible = true;
		}

		#endregion
	}
}
