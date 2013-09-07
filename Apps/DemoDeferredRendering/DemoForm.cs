#define USE_SH_ENV_COEFFS		// Define this to use the SH env map coeffs for the current scene
								// If not defined, 4 default cosine lobes will be placed in the map, enabling global ambient...
//#define RELOAD_SH_ENV_COEFFS	// Define this to simply reload the previously computed coeffs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

using Nuaj;
using Nuaj.Cirrus;
using Nuaj.Cirrus.Utility;

namespace Demo
{
	public partial class DemoForm : Form, IMaterialLoader
	{
		#region CONSTANTS

		protected const int		ENV_SH_MAP_SIZE = 256;			// The size of the 3D SH environment map

		#endregion

		#region FIELDS

		protected Nuaj.Device					m_Device = null;

		protected RendererSetupDeferred			m_Renderer = null;		// The Cirrus renderer
		protected Scene							m_Scene = null;
		protected Scene							m_Scene2 = null;

		// SH Env Map manager
		protected SHEnvMapManager				m_SHEnvMapManager = null;

		// Millions of lights !
		protected RenderTechniqueInferredLighting.LightOmni	m_Omni = null;
		protected RenderTechniqueInferredLighting.LightSpot	m_Spot = null;
		protected List<Vector3>										m_DynamicLightsOriginalPositions = new List<Vector3>();
		protected List<RenderTechniqueInferredLighting.LightOmni>	m_DynamicLights = new List<RenderTechniqueInferredLighting.LightOmni>();

		protected StringBuilder					m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>			m_Disposables = new Stack<IDisposable>();

		// Profiler form
		protected Nuaj.Cirrus.Utility.ProfilerForm	m_ProfilerForm = null;

		#endregion

		#region METHODS

		public DemoForm()
		{
//			PreComputeClebschGordan();

			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( panelOutput.Width, panelOutput.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
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
			// Create the renderer & a default scene
			m_Renderer = ToDispose( new RendererSetupDeferred( m_Device, "Renderer", true, 45.0f * (float) Math.PI / 180.0f, (float) ClientSize.Width / ClientSize.Height, 0.05f, 800.0f ) );

			trackBarSunPhi.Value = (int) (m_Renderer.SkyTechnique.SunPhi * trackBarSunPhi.Maximum / 360.0f);
			trackBarSunTheta.Value = (int) (m_Renderer.SkyTechnique.SunTheta * trackBarSunTheta.Maximum / 180.0f);

// 			m_Omni = m_Renderer.LightingTechnique.CreateLightOmni( "Omni Test",
// 						new Vector3( 0.0f, 1.0f, 0.0f ),
// 						new Vector3( 1.0f, 1.0f, 0.5f ),
// 						1.5f,
// 						2.0f );

			m_Spot = m_Renderer.LightingTechnique.CreateLightSpot( "Spot Test",
						new Vector3( 4.0f, 2.0f, -5.0f ),
						new Vector3( 0.0f, -1.0f, 0.0f ),
						new Vector3( 0.5f, 1.0f, 1.0f ),
						2.5f,
						3.0f,
						30.0f * (float) Math.PI / 180.0f,
						45.0f * (float) Math.PI / 180.0f );

	
			// Create plenty of omni lights
			Random	RNG = new Random( 1 );
			for ( int Z=0; Z < 32; Z++ )
				for ( int X=0; X < 32; X++ )
				{
					float	fX = (X + (float) RNG.NextDouble() - 16) * 100.0f / 16;
					float	fY = 1.0f + 4.0f * (float) RNG.NextDouble();
					float	fZ = (Z + (float) RNG.NextDouble() - 16) * 100.0f / 16;
					Vector3	Position = new Vector3( fX, fY, fZ );

					float	LightFactor = 100.0f;
					float	R = LightFactor * 0.5f * (1.0f + (float) RNG.NextDouble());
					float	G = LightFactor * 0.5f * (1.0f + (float) RNG.NextDouble());
					float	B = LightFactor * 0.5f * (1.0f + (float) RNG.NextDouble());

					float	fRadius = 3.0f + 6.0f * (float) RNG.NextDouble();

					RenderTechniqueInferredLighting.LightOmni	L = m_Renderer.LightingTechnique.CreateLightOmni( "Omni Light #"+ (10*Z+X).ToString(), Position, new Vector3( R, G, B ), 0.75f * fRadius, fRadius );

					m_DynamicLightsOriginalPositions.Add( Position );
					m_DynamicLights.Add( L );
				}

			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/TestDarkBox" ) ) );
			TextureProvider.ForceCreateMipMaps = true;

			//////////////////////////////////////////////////////////////////////////
			// Load some static test scene
			m_Scene = ToDispose( new Scene( m_Device, "Test Scene", m_Renderer.Renderer ) );

			MaterialMap	MMap = new MaterialMap();
			MMap.RegisterMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{
//					return _MaterialParameters.ShaderURL == "Phong" ? m_Renderer.FullSceneTechnique : null;
					return m_Renderer.SceneTechnique;
				} );

			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Test0/Test0.fbx" ), m_Scene, MMap, TextureProvider );
//				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/TestDarkBox/DarkBox.fbx" ), m_Scene, MMap, TextureProvider );
//				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/TestDarkBox/Cornell.fbx" ), m_Scene, MMap, TextureProvider );

				Matrix	RootPos = m_Scene.RootNode.Local2Parent;
				RootPos.Row4 = new SharpDX.Vector4( 0.0f, -0.5f, 0.0f, 1.0f );
				m_Scene.RootNode.Local2Parent = RootPos;
			}


			//////////////////////////////////////////////////////////////////////////
			// Create the SHEnvMap manager
			m_SHEnvMapManager = ToDispose( new SHEnvMapManager( m_Device, "SHEnvMapManager", ENV_SH_MAP_SIZE, this ) );

			// Create some environment SH nodes that cover the whole playground
#if !USE_SH_ENV_COEFFS
			float		SHBounds = 200.0f;
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( -SHBounds, 0.0f, +SHBounds ) ).MakeCosineLobe( Vector3.UnitY );
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( -SHBounds, 0.0f, -SHBounds ) ).MakeCosineLobe( Vector3.UnitY );
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( +SHBounds, 0.0f, -SHBounds ) ).MakeCosineLobe( Vector3.UnitY );
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( +SHBounds, 0.0f, +SHBounds ) ).MakeCosineLobe( Vector3.UnitY );
#else

#if RELOAD_SH_ENV_COEFFS
			m_SHEnvMapManager.LoadEnvironmentNodes( new System.IO.FileInfo( "./Data/DemoDeferredEnvNodes.SHEnvNodes" ) );
#else
			int		ENV_MAP_CUBE_SIZE = 64;
			int		ENV_NODES_SIDE_COUNT = 5;
			float	ENV_NODES_SIZE = 40.0f;
			float	CUBE_MAP_NEAR_CLIP = 0.01f;
			float	CUBE_MAP_FAR_CLIP = 100.0f;
			float	INDIRECT_LIGHTING_BOOST_FACTOR = 1.0f;
			int		RENDERING_PASSES_COUNT = 4;

			// Add 4 corner nodes
			float		SHBounds = 200.0f;
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( -SHBounds, 0.0f, +SHBounds ) );
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( -SHBounds, 0.0f, -SHBounds ) );
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( +SHBounds, 0.0f, -SHBounds ) );
			m_SHEnvMapManager.AddEnvironmentNode( new Vector3( +SHBounds, 0.0f, +SHBounds ) );

			// Create the env nodes on a grid
			for ( int Y=0; Y < ENV_NODES_SIDE_COUNT; Y++ )
				for ( int X=0; X < ENV_NODES_SIDE_COUNT; X++ )
				{
					Vector3	Position = new Vector3(	ENV_NODES_SIZE * ((X+0.5f) / ENV_NODES_SIDE_COUNT - 0.5f),
													8.0f,
													ENV_NODES_SIZE * ((Y+0.5f) / ENV_NODES_SIDE_COUNT - 0.5f)
													);

					m_SHEnvMapManager.AddEnvironmentNode( Position );
				}

			// Call the helper renderer
			Pipeline	DepthPassPipeline = m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.DEPTH_PASS );
			Pipeline	MaterialPassPipeline = m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING );

			using ( SHCubeMapRendererExample CubeMapsRenderer = new SHCubeMapRendererExample( m_Device, "CubeMapsRenderer", ENV_MAP_CUBE_SIZE, DepthPassPipeline, MaterialPassPipeline, CUBE_MAP_NEAR_CLIP, CUBE_MAP_FAR_CLIP, INDIRECT_LIGHTING_BOOST_FACTOR ) )
				SHEnvNodesRenderer.RenderSHEnvironmentNodes( m_SHEnvMapManager, CubeMapsRenderer, ENV_MAP_CUBE_SIZE, CUBE_MAP_NEAR_CLIP, CUBE_MAP_FAR_CLIP, INDIRECT_LIGHTING_BOOST_FACTOR, RENDERING_PASSES_COUNT );

			m_SHEnvMapManager.SaveEnvironmentNodes( new System.IO.FileInfo( "./Data/DemoDeferredEnvNodes.SHEnvNodes" ) );
#endif
// 
// 			bool	LOAD_ENV_NODES = false;
// 
// 			if ( LOAD_ENV_NODES )
// 			{
// 			}
// 			else
// 			{
// 
// 				m_Renderer.BeginEnvironmentRendering( ENV_MAP_CUBE_SIZE, INDIRECT_LIGHTING_BOOST_FACTOR );
// 
// 				// =============================================================
// 				// Render multiple passes
// 				for ( int PassIndex=0; PassIndex < 4; PassIndex++ )
// 				{
// 					// Compute SH coefficients for each node
// 					for ( int Y=0; Y < ENV_NODES_SIDE_COUNT; Y++ )
// 						for ( int X=0; X < ENV_NODES_SIDE_COUNT; X++ )
// 						{
// 							RendererSetupDeferred.EnvironmentNode	EnvNode = EnvNodes[X,Y];
// 
// 							// Render the cube map
// 							m_Renderer.RenderCubeMap( EnvNode.V.Position, new Vector3( 0.0f, 0.0f, 1.0f ), Vector3.UnitY, 0.01f, 200.0f, PassIndex );
// 			
// 							// Encode into SH
// 							if ( PassIndex == 0 )
// 								SHCoefficients[X,Y] = m_Renderer.EncodeSHEnvironmentDirect();
// 							else
// 								SHCoefficients[X,Y] = m_Renderer.EncodeSHEnvironmentIndirect();
// 						}
// 
// 					// Update coefficients for next stage & accumulate
// 					for ( int Y=0; Y < ENV_NODES_SIDE_COUNT; Y++ )
// 						for ( int X=0; X < ENV_NODES_SIDE_COUNT; X++ )
// 						{
// 							RendererSetupDeferred.EnvironmentNode	EnvNode = EnvNodes[X,Y];
// 							EnvNode.UpdateCoefficients( SHCoefficients[X,Y] );
// 
// 							for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
// 								SHCoefficientsAcc[X,Y][SHCoeffIndex] += SHCoefficients[X,Y][SHCoeffIndex];
// 						}
// 				}
// 
// 				// =============================================================
// 				// Update with accumulated coefficients for result
// 				for ( int Y=0; Y < ENV_NODES_SIDE_COUNT; Y++ )
// 					for ( int X=0; X < ENV_NODES_SIDE_COUNT; X++ )
// 					{
// 						RendererSetupDeferred.EnvironmentNode	EnvNode = EnvNodes[X,Y];
// 						EnvNode.UpdateCoefficientsReflected( SHCoefficientsAcc[X,Y] );
// //						EnvNode.UpdateCoefficients( SHCoefficientsAcc[X,Y] );
// 					}
// 
// //				m_Renderer.EndEnvironmentRendering();
// 
// 				// Save resulting env nodes
// 				m_Renderer.SaveEnvironmentNodes( new System.IO.FileInfo( "./Data/DemoDeferredEnvNodes.SHEnvNodes" ) );
// 			}
#endif


			//////////////////////////////////////////////////////////////////////////
			// Load the head we'll use as a probe
			SceneTextureProvider	TextureProvider2 = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/3DHead/Infinite_Scan_Ver0.1/ImagesLoRes" ) ) );
			TextureProvider2.AddSourceRootPathToStripOff( "Infinite_Scan_Ver0.1/Images" );
			m_Scene2 = ToDispose( new Scene( m_Device, "Second Scene", m_Renderer.Renderer ) );
			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene2" ) )
			{
//				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/3DHead/3DHead.fbx" ), m_Scene2, MMap, TextureProvider2 );
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/TestDarkBox/SphereProbe.fbx" ), m_Scene2, MMap, TextureProvider2 );

				Matrix	RootPos = m_Scene2.RootNode.Local2Parent;
				RootPos.Row1 = 0.25f * new Vector4( 0.0f, 0.0f, 1.0f, 0.0f );
				RootPos.Row2 = 0.25f * new Vector4( 1.0f, 0.0f, 0.0f, 0.0f );
				RootPos.Row3 = 0.25f * new Vector4( 0.0f, 1.0f, 0.0f, 0.0f );
				RootPos.Row4 = new SharpDX.Vector4( 13.0f, 2.0f, 0.0f, 1.0f );
				m_Scene2.RootNode.Local2Parent = RootPos;
			}



			//////////////////////////////////////////////////////////////////////////
			// Display statistics & errors
			richTextBoxOutput.Log( "Texture Provider :\n" );
			richTextBoxOutput.Log( "> " + TextureProvider.LoadedTexturesCount + " textures loaded.\n" );
			int	MinSize = (int) Math.Sqrt( TextureProvider.MinTextureSurface );
			int	MaxSize = (int) Math.Sqrt( TextureProvider.MaxTextureSurface );
			int	AvgSize = (int) Math.Sqrt( TextureProvider.AverageTextureSurface );
			int	TotalSize = (int) Math.Sqrt( TextureProvider.TotalTextureSurface );
			richTextBoxOutput.Log( "> Surface Min=" + MinSize + "x" + MinSize + " Max=" + MaxSize + "x" + MaxSize + " Avg=" + AvgSize + "x" + AvgSize + "\n" );
			richTextBoxOutput.LogWarning( "> Surface Total=" + TotalSize + "x" + TotalSize + " (Memory=" + (TextureProvider.TotalTextureMemory>>10) + " Kb)\n" );

			if ( TextureProvider.HasErrors )
			{	// Display errors
				richTextBoxOutput.Log( "The texture provider has some errors !\r\n\r\n" );
				foreach ( string Error in TextureProvider.TextureErrors )
					richTextBoxOutput.LogError( "   ●  " + Error + "\r\n" );
			}
			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );

			BuildHierarchyTree();

			//////////////////////////////////////////////////////////////////////////
			// Create the profiler form
			m_ProfilerForm = new Nuaj.Cirrus.Utility.ProfilerForm( m_Device );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			// Create manipulators
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( 0.0f, 2.0f, 100.0f ), new Vector3( 0.0f, 0.0f, 0.0f ), Vector3.UnitY );
//			CamManip.InitializeCamera( new Vector3( 0.0f, 0.25f, 0.0f ), new Vector3( 0.0f, 1.0f, -8.0f ), Vector3.UnitY );
			CamManip.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CamManip_EnableMouseAction );

			Nuaj.Helpers.ObjectsManipulator	ObjectsManip = new Nuaj.Helpers.ObjectsManipulator();
			ObjectsManip.Attach( panelOutput, m_Renderer.Camera );
//			ObjectsManip.RegisterMovableObject( 0, m_Omni.Position, m_Omni.OuterRadius );
			ObjectsManip.RegisterMovableObject( 0, (Vector3) m_Scene2.RootNode.Local2Parent.Row4, 4.0f );
			ObjectsManip.RegisterMovableObject( 1, m_Spot.Position, m_Spot.OuterRadius );
			ObjectsManip.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
			ObjectsManip.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
			ObjectsManip.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );


			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;
			DateTime	LastFPSTime = DateTime.Now;
			int			FPSFramesCount = 0;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============

				m_Device.StartProfiling( m_ProfilerForm.FlushEveryOnTask );
				m_Device.AddProfileTask( null, "Frame", "<START>" );

				m_Device.AddProfileTask( null, "Update", "Omni Lights Animation" );

				// Funny lights animation
				Random	RNG = new Random( 1 );
				for ( int LightIndex=0; LightIndex < m_DynamicLights.Count; LightIndex++ )
				{
					float	fSpeed = 0.5f + 4.0f * (float) RNG.NextDouble();
					float	fRadiusX = 0.5f + 4.0f * (float) RNG.NextDouble();
					float	fRadiusZ = 0.5f + 4.0f * (float) RNG.NextDouble();
					Vector3	NewPosition = new Vector3(
						m_DynamicLightsOriginalPositions[LightIndex].X + fRadiusX * (float) Math.Cos( fSpeed * fTotalTime ),
						m_DynamicLightsOriginalPositions[LightIndex].Y,
						m_DynamicLightsOriginalPositions[LightIndex].Z + fRadiusZ * (float) Math.Sin( fSpeed * fTotalTime ) );

					m_DynamicLights[LightIndex].Position = NewPosition;
				}

				// Render environment map using current sky & sun light
				m_Device.AddProfileTask( null, "SHEnvMap", "Render EnvMap" );
				m_SHEnvMapManager.RenderSHEnvironmentMap( m_Renderer.Camera, m_Renderer.SkyTechnique.SkyLightSH );


				// Draw
				m_Renderer.Time = fTotalTime;
				m_Renderer.Render();

				// Show !
				m_Device.AddProfileTask( null, "Device", "Present" );
				m_Device.Present();

				m_Device.AddProfileTask( null, "Frame", "<END>" );
				m_Device.EndProfiling();

				// Update FPS
				FPSFramesCount++;
				DateTime	Now = DateTime.Now;
				if ( (Now - LastFPSTime).TotalMilliseconds > 1000 )
				{
					float	FPS = (float) (FPSFramesCount / (Now - LastFPSTime).TotalSeconds);
					LastFPSTime = Now;
					FPSFramesCount = 0;
					Text = "Demo - " + FPS.ToString( "G4" ) + " FPS";
				}
			} );
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

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

			// Build the interface nodes
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

		#region IMaterialLoader Members

		public Material<VS> LoadMaterial<VS>( string _Name, ShaderModel _SM, System.IO.FileInfo _FileName ) where VS : struct
		{
			return ToDispose( new Material<VS>( m_Device, _Name, _SM, _FileName ) );
		}

		#endregion

		#region Clebsch-Gordan Coefficients & Triple Product Code Generation

		protected class CBCoeff
		{
			public int Acc;
			public int Index0, Index1;
			public double	Sign;
			public double	Coeff;
			public int		CoeffIndex;
		}

		/// <summary>
		/// Precomputes the shader code for SH convolutions
		/// </summary>
		protected void	PreComputeClebschGordan()
		{
			// Precompute convolution coefficients
			double[]	SH0 = new double[9];
			double[]	SH1 = new double[9];
			for ( int i=0; i < 9; i++ )
			{
				SH0[i] = 1.0;
				SH1[i] = 1.0;
			}

			string[]	CBCoeffs = new string[]
			{
				"0.282094791773878",
				"0.126156626101008",
				"0.218509686118416",
				"0.309019361618552",
				"0.252313252202016",
				"0.180223751572869",
				"0.220728115441823",
				"0.090111875786434",
			};

			List<CBCoeff>	Coeffs = new List<CBCoeff>();
			double[]	ResultSH = SphericalHarmonics.SHFunctions.Convolve( SH0, SH1, 3,
				new SphericalHarmonics.SHFunctions.ConvolutionDelegate( ( int _Acc, int _Index0, int _Index1, double _Sign, double _Coeff ) =>
					{
						CBCoeff	C = new CBCoeff();
						C.Acc = _Acc;
						C.Index0 = _Index0;
						C.Index1 = _Index1;
						C.Sign = _Sign;
						C.Coeff = _Coeff;

						string	CoeffValue = _Coeff.ToString();
						bool	bFound = false;
						for ( C.CoeffIndex=0; C.CoeffIndex < CBCoeffs.Length; C.CoeffIndex++ )
							if ( CoeffValue.StartsWith( CBCoeffs[C.CoeffIndex] ) )
							{	// Found it !
								bFound = true;
								break;
							}

						if ( !bFound )
							throw new Exception( "Invalid coeff !" );

						Coeffs.Add( C );
					} ) );

			string	ShaderCode = "const float	C0 = 0.282094791773878;\r\n" +
								 "const float	C1 = 0.126156626101008;\r\n" +
								 "const float	C2 = 0.218509686118416;\r\n" +
								 "const float	C3 = 0.309019361618552;\r\n" +
								 "const float	C4 = 0.252313252202016;\r\n" +
								 "const float	C5 = 0.180223751572869;\r\n" +
								 "const float	C6 = 0.220728115441823;\r\n" +
								 "const float	C7 = 0.090111875786434;\r\n";

			Dictionary<int,List<string>>	CoeffIndex2Terms = new Dictionary<int,List<string>>();
			int		CurrentSHIndex = 0;
			foreach ( CBCoeff C in Coeffs )
			{
				if ( C.Acc != CurrentSHIndex )
				{	// Dump factored line
					ShaderCode += DumpFactorizationDictionary( CurrentSHIndex, CoeffIndex2Terms );
					CoeffIndex2Terms.Clear();
					CurrentSHIndex = C.Acc;
				}

				if ( !CoeffIndex2Terms.ContainsKey( C.CoeffIndex ) )
					CoeffIndex2Terms[C.CoeffIndex] = new List<string>();
				CoeffIndex2Terms[C.CoeffIndex].Add( (C.Sign < 0.0 ? "-" : "+") + "_In.SH" + C.Index0 + "*SHLight[" + C.Index1 + "]" );
			}
			// Dump final factored line
			ShaderCode += DumpFactorizationDictionary( 8, CoeffIndex2Terms );
			ShaderCode = ShaderCode.Replace( "(+", "(" );
		}

		protected string	DumpFactorizationDictionary( int _AccumulatedSHIndex, Dictionary<int,List<string>> _CoeffIndex2Terms )
		{
			string	Result = "Out.SH" + _AccumulatedSHIndex + ".xyz =";
			bool	bFirstCoeff = true;
			for ( int CoeffIndex=0; CoeffIndex < 9; CoeffIndex++ )
				if ( _CoeffIndex2Terms.ContainsKey( CoeffIndex ) )
				{
					List<string>	Terms = _CoeffIndex2Terms[CoeffIndex];
					bool			MoreThanOneTerm = Terms.Count > 1;
					Result += (bFirstCoeff ? "" : " +") + " C" + CoeffIndex + " * " + (MoreThanOneTerm ? "(" : "");
					foreach ( string Term in Terms )
						Result += Term;
					Result += (MoreThanOneTerm ? ")" : "");
					bFirstCoeff = false;
				}

			Result += ";\r\n";

			return Result;
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
			richTextBoxOutput.Log( "\"" + M.ToString() + "\" recompiled...\r\n" );
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

		bool CamManip_EnableMouseAction( MouseEventArgs _e )
		{
			return (Control.ModifierKeys & Keys.Control) == 0;	// Can't manipulate camera if Control is pressed
		}

		bool ObjectsManip_EnableMouseAction( MouseEventArgs _e )
		{
			return (Control.ModifierKeys & Keys.Control) != 0;	// Can't manipulate objects if Control is NOT pressed
		}

		void ObjectsManip_ObjectSelected( object _PickedObject, bool _Selected )
		{
			if ( !_Selected )
			{
				labelHoveredObject.Text = "No hovered object...";
				return;
			}

			switch ( (int) _PickedObject )
			{
				case 0:
//					labelHoveredObject.Text = "Hovering Omni Light";
					labelHoveredObject.Text = "Hovering Head";
					break;
				case 1:
					labelHoveredObject.Text = "Hovering Spot Light";
					break;
			}
		}

		void ObjectsManip_ObjectMoving( object _MovedObject, Vector3 _NewPosition )
		{
			switch ( (int) _MovedObject )
			{
				case 0:
				{
//					m_Omni.Position = _NewPosition;
					Matrix	Temp = m_Scene2.RootNode.Local2Parent;
					Temp.Row4 = new SharpDX.Vector4( _NewPosition, 1.0f );
					m_Scene2.RootNode.Local2Parent = Temp;
					break;
				}
				case 1:
					m_Spot.Position = _NewPosition;
					break;
			}
		}

		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			if ( e.KeyCode == Keys.Space )
				m_Renderer.GrassTechnique.Gust = true;
		}

		private void trackBarSunPhi_Scroll( object sender, EventArgs e )
		{
			m_Renderer.SkyTechnique.SunPhi = 360.0f * trackBarSunPhi.Value / trackBarSunPhi.Maximum;
		}

		private void trackBarSunTheta_Scroll( object sender, EventArgs e )
		{
			m_Renderer.SkyTechnique.SunTheta = 180.0f * trackBarSunTheta.Value / trackBarSunTheta.Maximum;
		}

		private void buttonProfiling_Click( object sender, EventArgs e )
		{
			m_ProfilerForm.Show( this );
		}

		#endregion
	}
}
