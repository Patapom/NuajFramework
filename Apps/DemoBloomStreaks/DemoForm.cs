//#define USE_HDR_IMAGE

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
	public partial class DemoForm : Form, IMaterialLoader
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic			m_Renderer = null;
		protected RenderTechniqueRenderScene					m_RenderScene = null;
		protected RenderTechniquePostProcessToneMappingFilmic	m_ToneMapping = null;
		protected RenderTechniqueBloomStreaks<PF_RGBA16F>		m_RenderTechniqueBloom = null;

		// Temp targets
		protected RenderTarget<PF_RGBA16F>		m_HDRSource = null;
		protected RenderTarget<PF_RGBA16F>[]	m_RenderTargets = new RenderTarget<PF_RGBA16F>[2];

		// Scene
		protected Scene						m_Scene = null;

		// Helpers
		protected RenderTargetFactory		m_RTFactory = null;
		protected SceneTextureProvider		m_TextureProvider = null;


		protected StringBuilder				m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public DemoForm()
		{
			InitializeComponent();

#if USE_HDR_IMAGE
			this.Size = new Size( 0x34E, 0x347 );
#endif

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

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, panelOutput ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}


			//////////////////////////////////////////////////////////////////////////
			// Create our scene render target, with mip-maps for tone mapping
			m_RenderTargets[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Render Target 0 (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );
			m_RenderTargets[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Render Target 1 (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );

			m_RTFactory = ToDispose( new RenderTargetFactory( m_Device, "RTFactory" ) );
			m_TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider", new System.IO.DirectoryInfo( "./Meshes/BloomStreaksTest/" ) ) );
			m_TextureProvider.ForceCreateMipMaps = true;


			//////////////////////////////////////////////////////////////////////////
			// Load some HDR image + alpha = Light Phase (hand painted)
#if USE_HDR_IMAGE
			byte[]	HDRBinary = null;
			using ( System.IO.FileStream S = new System.IO.FileInfo( "./Media/HDR Images/memorial.hdr" ).OpenRead() )
			{
				HDRBinary = new byte[S.Length];
				S.Read( HDRBinary, 0, (int) S.Length );
			}

			byte[]				AlphaContent = null;
			int					AlphaWidth, AlphaHeight;
			using ( System.IO.Stream S = new System.IO.FileInfo( "./Media/HDR Images/memorial_spec_emissive.png" ).OpenRead() )
				AlphaContent = Image<PF_RGBA16F>.LoadBitmap( S, out AlphaWidth, out AlphaHeight );

			Vector4[,]			HDRImage = Image<PF_RGBA16F>.LoadAndDecodeHDRFormat( HDRBinary );
			using ( Image<PF_RGBA16F>	I = new Image<PF_RGBA16F>( m_Device, "HDRImage", HDRImage, 0.0f, 0, ( int _X, int _Y, ref Vector4 _Color ) =>
				{
					float	Phase = AlphaContent[(AlphaWidth*_Y+_X)<<2] / 255.0f;
					float	Emissive = AlphaContent[((AlphaWidth*_Y+_X)<<2)+1] / 255.0f;
					_Color.W = Phase * (1.0f + Emissive);	// Replace alpha
				} ) )
					m_HDRSource = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "HDR Source", I ) );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Create the renderer
			RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
			{
				CameraFOV = 45.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 1000.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

			// Setup the default lights
			m_Renderer.MainLight.Color = 1 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.MainLight.Direction = new Vector3( 1.0f, 0.5f, 1.0f );


			//////////////////////////////////////////////////////////////////////////
			// Main scene display
			m_RenderScene = ToDispose( new RenderTechniqueRenderScene( m_Device, "Render Scene", m_RenderTargets[0] ) );
//			m_RenderScene = ToDispose( new RenderTechniqueRenderScene( m_Device, "Render Scene", m_Device.DefaultRenderTarget ) );
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).AddTechnique( m_RenderScene );

			//////////////////////////////////////////////////////////////////////////
			// Tone mapping technique
			m_ToneMapping = ToDispose( new RenderTechniquePostProcessToneMappingFilmic( m_Renderer.Device, "Tone Mapping", this, false ) );
//			m_ToneMapping.SourceImage = m_HDRSource;
			m_ToneMapping.SourceImage = m_RenderTargets[0];
			m_ToneMapping.TargetImage = m_RenderTargets[1];
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).AddTechnique( m_ToneMapping );

			m_ToneMapping.AdaptationLevelMin = 0.0f;
			m_ToneMapping.AdaptationLevelMax = 1.0f;
#if !USE_HDR_IMAGE
			m_ToneMapping.HDRWhitePointLuminance = 26.5f;
#endif

			//////////////////////////////////////////////////////////////////////////
			// Bloom/Streaks technique (goes AFTER tone mapping)
			int		BloomWidth = m_Device.DefaultRenderTarget.Width / 2;
			int		BloomHeight = m_Device.DefaultRenderTarget.Height / 2;

			m_RenderTechniqueBloom = ToDispose( new RenderTechniqueBloomStreaks<PF_RGBA16F>( m_Renderer, "Bloom Render Technique", BloomWidth, BloomHeight, 1.0f, m_RTFactory ) );
			m_RenderTechniqueBloom.SourceImage = m_RenderTargets[1];
			m_RenderTechniqueBloom.TargetImage = m_Device.DefaultRenderTarget;
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).AddTechnique( m_RenderTechniqueBloom );


			//////////////////////////////////////////////////////////////////////////
			// Load the scene
			m_Scene = ToDispose( new Scene( m_Device, "Scene", m_Renderer.Renderer ) );

			MaterialMap	MMap = new MaterialMap();
			MMap.RegisterMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{
					return m_RenderScene;
				} );

			using ( Nuaj.Cirrus.FBX.SceneLoader Loader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "SceneLoader" ) )
			{
				Loader.Load( new System.IO.FileInfo( "./Meshes/BloomStreaksTest/BloomStreaksTest0.fbx" ), m_Scene, MMap, m_TextureProvider );
			}

			BuildHierarchyTree();
			SetupParams();
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
			// Create a camera manipulator
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( -6.0f, 4.5f, 11.0f ), new Vector3( 0.0f, 1.0f, 0.0f ), Vector3.UnitY );

			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			string		InitialText = Text;
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

				// Clear depth stencil
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_RenderTechniqueBloom.Time = fTotalTime;
				m_Renderer.Render();

				// Show !
				m_Device.Present();

				// Update FPS
				FPSFramesCount++;
				DateTime	Now = DateTime.Now;
				if ( (Now - LastFPSTime).TotalMilliseconds > 1000 )
				{
					float	FPS = (float) (FPSFramesCount / (Now - LastFPSTime).TotalSeconds);
					LastFPSTime = Now;
					FPSFramesCount = 0;
					Text = InitialText + " - " + FPS.ToString( "G4" ) + " FPS";
				}
			});
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

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

		protected bool	m_bInternalChange = false;
		protected void	SetupParams()
		{
			m_bInternalChange = true;
			floatTrackbarControlWhiteLevel.Value = m_ToneMapping.HDRWhitePointLuminance;

			floatTrackbarControlBloomThreshold.Value = m_RenderTechniqueBloom.BloomThreshold;
			floatTrackbarControlBloomFactor.Value = m_RenderTechniqueBloom.BloomFactor;
			floatTrackbarControlBloomRadius.Value = m_RenderTechniqueBloom.BloomRadius;
			floatTrackbarControlBloomGamma.Value = m_RenderTechniqueBloom.BloomGamma;

			floatTrackbarControlStreaksThreshold.Value = m_RenderTechniqueBloom.StreaksThreshold;
			floatTrackbarControlStreaksFactor.Value = m_RenderTechniqueBloom.StreaksFactor;
			floatTrackbarControlStreaksAngle.Value = m_RenderTechniqueBloom.StreaksAngle * 180.0f / (float) Math.PI;
			floatTrackbarControlStreaksCoverAngle.Value = m_RenderTechniqueBloom.StreaksCoverAngle * 180.0f / (float) Math.PI;
			floatTrackbarControlStreaksAttenuation.Value = m_RenderTechniqueBloom.StreaksAttenuation;
			m_bInternalChange = false;
		}

		private void floatTrackbarControlStreaksFactor_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( m_bInternalChange )
				return;

			m_ToneMapping.HDRWhitePointLuminance = floatTrackbarControlWhiteLevel.Value;

			m_RenderTechniqueBloom.BloomThreshold = floatTrackbarControlBloomThreshold.Value;
			m_RenderTechniqueBloom.BloomFactor = floatTrackbarControlBloomFactor.Value;
			m_RenderTechniqueBloom.BloomRadius = floatTrackbarControlBloomRadius.Value;
			m_RenderTechniqueBloom.BloomGamma = floatTrackbarControlBloomGamma.Value;

			m_RenderTechniqueBloom.StreaksThreshold = floatTrackbarControlStreaksThreshold.Value;
			m_RenderTechniqueBloom.StreaksFactor = floatTrackbarControlStreaksFactor.Value;
			m_RenderTechniqueBloom.StreaksAngle = floatTrackbarControlStreaksAngle.Value * (float) Math.PI / 180.0f;
			m_RenderTechniqueBloom.StreaksCoverAngle = floatTrackbarControlStreaksCoverAngle.Value * (float) Math.PI / 180.0f;
			m_RenderTechniqueBloom.StreaksAttenuation = floatTrackbarControlStreaksAttenuation.Value;
		}

		#endregion
	}
}
