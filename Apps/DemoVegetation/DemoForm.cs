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

		protected RendererSetup					m_Renderer = null;		// The Cirrus renderer
		protected Scene							m_Scene = null;

		// SH Env Map manager
		protected SHEnvMapManager				m_SHEnvMapManager = null;

		protected Nuaj.Helpers.CameraManipulator	m_CameraManipulator = null;
		protected Nuaj.Helpers.ObjectsManipulator	m_ObjectsManipulator = null;

		protected StringBuilder					m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>			m_Disposables = new Stack<IDisposable>();

		// Profiler form
		protected Nuaj.Cirrus.Utility.ProfilerForm	m_ProfilerForm = null;

		#endregion

		#region METHODS

		public DemoForm()
		{
// 			float	MaxDiff = -float.MaxValue;
// 			float	MinDiff = +float.MaxValue;
// 			int		MaxTerms = 2;
// 			for ( int i=0; i < 500; i++ )
// 			{
// 				float	CosTheta = (float) Math.Cos( i * Math.PI / 1000.0f );
// 				float	AcosTheta = (float) Math.Acos( CosTheta );
// 
// 				// Acos DL
// 				float	x = CosTheta;
// 				float	x2 = x*x;
// 
// 				float	AcosThetaDL = 0.5f * (float) Math.PI - x;
// 
// 				float	CurrX = x;
// 				float	Den = 1.0f;
// 				float	Num = 1.0f;
// 				for ( int Term=0; Term < MaxTerms; Term++ )
// 				{
// 					Num *= (1.0f + 2.0f * Term) / (2.0f * (1+Term));
// 					Den += 2.0f;
// 					CurrX *= x2;
// 
// 					AcosThetaDL -= Num * CurrX / Den;
// 				}
// 
// 				float	DiffAcos = Math.Abs( AcosThetaDL - AcosTheta );
// 
// 				// Asin DL
// 				x = (float) Math.Sin( i * Math.PI / 1000.0f );
// 				x2 = x*x;
// 
// 				float	AsinThetaDL = x;
// 
// 				CurrX = x;
// 				Den = 1.0f;
// 				Num = 1.0f;
// 				for ( int Term=0; Term < MaxTerms; Term++ )
// 				{
// 					Num *= (1.0f + 2.0f * Term) / (2.0f * (1+Term));
// 					Den += 2.0f;
// 					CurrX *= x2;
// 
// 					AsinThetaDL -= Num * CurrX / Den;
// 				}
// 
// 				float	DiffAsin = Math.Abs( AsinThetaDL - AcosTheta );
// 
// 				float	Diff = Math.Min( DiffAcos, DiffAsin );
// //				float	Diff = DiffAsin;
// 
// 				MaxDiff = Math.Max( Diff, MaxDiff );
// 				MinDiff = Math.Min( Diff, MinDiff );
// 			}

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
			m_Renderer = ToDispose( new RendererSetup( m_Device, "Renderer", true, 45.0f * (float) Math.PI / 180.0f, (float) ClientSize.Width / ClientSize.Height, 0.05f, 800.0f ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Media/Vegetation/Frecle" ) ) );
			TextureProvider.ForceCreateMipMaps = true;
			TextureProvider.AddSourceRootPathToStripOff( "C:/Program Files (x86)/tree[d]/Textures/" );


			//////////////////////////////////////////////////////////////////////////
			// Load some trees scene
			m_Scene = ToDispose( new Scene( m_Device, "Test Scene", m_Renderer.Renderer ) );

/*
			MaterialMap	MMap = new MaterialMap();
			MMap.RegisterMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{
//					return _MaterialParameters.ShaderURL == "Phong" ? m_Renderer.FullSceneTechnique : null;
					return m_Renderer.Vegetation;
				} );

			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Trees/Frecle/Test0.fbx" ), m_Scene, MMap, TextureProvider );

// 				Matrix	RootPos = m_Scene.RootNode.Local2Parent;
// 				RootPos.Row4 = new SharpDX.Vector4( 0.0f, -0.5f, 0.0f, 1.0f );
// 				m_Scene.RootNode.Local2Parent = RootPos;
			}

*/

			//////////////////////////////////////////////////////////////////////////
			// Create the SHEnvMap manager
/*			m_SHEnvMapManager = ToDispose( new SHEnvMapManager( m_Device, "SHEnvMapManager", ENV_SH_MAP_SIZE, this ) );


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
#endif
*/

			//////////////////////////////////////////////////////////////////////////
			// Create manipulators
			m_CameraManipulator = new Nuaj.Helpers.CameraManipulator();
			m_CameraManipulator.Attach( panelOutput, m_Renderer.Camera );
			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 2.0f, 0.0f ), new Vector3( 0.0f, 2.1f, 1.0f ), Vector3.UnitY );
//			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 0.25f, 0.0f ), new Vector3( 0.0f, 1.0f, -8.0f ), Vector3.UnitY );
			m_CameraManipulator.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CamManip_EnableMouseAction );

			m_ObjectsManipulator = new Nuaj.Helpers.ObjectsManipulator();
			m_ObjectsManipulator.Attach( panelOutput, m_Renderer.Camera );
//			m_ObjectsManipulator.RegisterMovableObject( 0, m_Omni.Position, m_Omni.OuterRadius );
// 			m_ObjectsManipulator.RegisterMovableObject( 0, (Vector3) m_Scene2.RootNode.Local2Parent.Row4, 4.0f );
// 			m_ObjectsManipulator.RegisterMovableObject( 1, m_Spot.Position, m_Spot.OuterRadius );
			m_ObjectsManipulator.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
			m_ObjectsManipulator.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
			m_ObjectsManipulator.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );


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


			//////////////////////////////////////////////////////////////////////////
			// Enable existing layers
			switch ( m_Renderer.CloudLayer.CloudLayers.Length )
			{
				case 1:
					radioButton1.Enabled = true;
					radioButton1.Checked = true;
					break;
				case 2:
					radioButton1.Enabled = true;
					radioButton2.Enabled = true;
					radioButton1.Checked = true;
					break;
				case 3:
					radioButton1.Enabled = true;
					radioButton2.Enabled = true;
					radioButton3.Enabled = true;
					radioButton1.Checked = true;
					break;
				case 4:
					radioButton1.Enabled = true;
					radioButton2.Enabled = true;
					radioButton3.Enabled = true;
					radioButton4.Enabled = true;
					radioButton1.Checked = true;
					break;
			}
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
			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;
			DateTime	LastFPSTime = DateTime.Now;
			int			FPSFramesCount = 0;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				if ( !m_Device.CheckCanRender( 10 ) )
				{
					Text = "OBSTRUCTED";
					return;
				}

				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============

				m_Device.StartProfiling( m_ProfilerForm.FlushEveryOnTask );
				m_Device.AddProfileTask( null, "Frame", "<START>" );

				// Render environment map using current sky & sun light
				m_Device.AddProfileTask( null, "SHEnvMap", "Render EnvMap" );
//				m_SHEnvMapManager.RenderSHEnvironmentMap( m_Renderer.Camera, m_Renderer.SkyTechnique.SkyLightSH );

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
				double		DeltaMilliseconds = (Now - LastFPSTime).TotalMilliseconds;
				if ( DeltaMilliseconds > 1000 )
				{
					float	FPS = (float) (1000.0 * FPSFramesCount / DeltaMilliseconds);
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
// 			switch ( (int) _MovedObject )
// 			{
// 				case 0:
// 				{
// //					m_Omni.Position = _NewPosition;
// 					Matrix	Temp = m_Scene2.RootNode.Local2Parent;
// 					Temp.Row4 = new SharpDX.Vector4( _NewPosition, 1.0f );
// 					m_Scene2.RootNode.Local2Parent = Temp;
// 					break;
// 				}
// 				case 1:
// 					m_Spot.Position = _NewPosition;
// 					break;
// 			}
		}

		private void buttonProfiling_Click( object sender, EventArgs e )
		{
			m_ProfilerForm.Show( this );
		}

		protected void	SetupDefaultParameters()
		{
			floatTrackbarControlSunPhi.Value = -m_Renderer.Sky.SunPhi;
			floatTrackbarControlSunTheta.Value = m_Renderer.Sky.SunTheta;
		}

		private void floatTrackbarControlSunPhi_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_Renderer.Sky.SunPhi = -_Sender.Value;
		}

		private void floatTrackbarControlSunTheta_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_Renderer.Sky.SunTheta = _Sender.Value;

			// Update scattering coefficients
			float	t = _Sender.Value / 90.0f;
			m_Renderer.Sky.DensityRayleigh = 1e-5f * Lerp( 8.0f, 40.0f, t );
			m_Renderer.Sky.DensityMie = 1e-4f * Lerp( 8.0f, 20.0f, t );
		}

		protected float	Lerp( float _a, float _b, float t )
		{
			return _a + (_b-_a) * Math.Max( 0.0f, Math.Min( 1.0f, t ) );
		}

		private void floatTrackbarControlAltitude_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.Altitude = _Sender.Value;
		}

		private void floatTrackbarControlCloudExtinction_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.ScatteringCoeff = _Sender.Value;
		}

		private void floatTrackbarControlCloudDensity_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.DensityOffset = _Sender.Value;
		}

		private void floatTrackbarControlCloudThickness_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.Thickness = _Sender.Value;
		}

		private void floatTrackbarControlCloudNormalAmplitude_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.NormalAmplitude = _Sender.Value;
		}

		private void floatTrackbarControlCloudSmoothness_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.NoiseMipBias = _Sender.Value;
		}

		private void floatTrackbarControlCloudFrequencyFactor_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.FrequencyFactor = _Sender.Value;
		}

		private void floatTrackbarControlCloudNoiseSize_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.NoiseSize = 0.01f * _Sender.Value;
		}

		private void floatTrackbarControlCloudFrequencyFactorAnisotropy_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.AmplitudeFactor = _Sender.Value;
		}

		private void floatTrackbarControlCloudSpeed_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			CurrentLayer.Speed = 0.01f * _Sender.Value;
		}

		private void radioButton1_CheckedChanged( object sender, EventArgs e )
		{
			floatTrackbarControlAltitude.Value = CurrentLayer.Altitude;
			floatTrackbarControlCloudExtinction.Value = CurrentLayer.ScatteringCoeff;
			floatTrackbarControlCloudDensity.Value = CurrentLayer.DensityOffset;
			floatTrackbarControlCloudThickness.Value = CurrentLayer.Thickness;
			floatTrackbarControlCloudNormalAmplitude.Value = CurrentLayer.NormalAmplitude;
			floatTrackbarControlCloudSmoothness.Value = CurrentLayer.NoiseMipBias;
			floatTrackbarControlCloudSpeed.Value = 100.0f * CurrentLayer.Speed;
			floatTrackbarControlCloudNoiseSize.Value = 100.0f * CurrentLayer.NoiseSize;
			floatTrackbarControlCloudFrequencyFactor.Value = CurrentLayer.FrequencyFactor;
			floatTrackbarControlCloudAmplitudeFactor.Value = CurrentLayer.AmplitudeFactor;
		}

		protected RenderTechniqueCloudLayer.CloudLayer	CurrentLayer
		{
			get
			{
				if ( radioButton1.Checked )
					return m_Renderer.CloudLayer.CloudLayers[0];
				if ( radioButton2.Checked )
					return m_Renderer.CloudLayer.CloudLayers[1];
				if ( radioButton3.Checked )
					return m_Renderer.CloudLayer.CloudLayers[2];
				if ( radioButton4.Checked )
					return m_Renderer.CloudLayer.CloudLayers[3];
				return null;
			}
		}

		#endregion
	}
}
