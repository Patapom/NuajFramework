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

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupShadowMap	m_Renderer = null;
		protected RenderTechniqueSkin		m_RenderTechniqueSkin = null;	// The advanced render technique for realistic skin

		protected Scene						m_Scene = null;

		protected StringBuilder				m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public DemoForm()
		{
			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( panelOutput.Width, panelOutput.Height, new Rational(60, 1),


						// IMPORTANT => We use linear space textures here so we need gamma correction in the end
						Format.R8G8B8A8_UNorm_SRgb
						// IMPORTANT


						),
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
			// Create the renderer & a default scene
			try
			{
				RendererSetupShadowMap.InitParams	Params = new RendererSetupShadowMap.InitParams()
				{
					bUseAlphaToCoverage = true,
					CameraFOV = 45.0f * (float) Math.PI / 180.0f,
					CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
					CameraClipNear=0.01f,
					CameraClipFar=100.0f,
					ShadowMapSlicesCount=3,
					ShadowMapSize=2048
				};

				m_Renderer = ToDispose( new RendererSetupShadowMap( m_Device, "Renderer", Params ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Setup the default lights
			m_Renderer.MainLight.Color = 10 * Vector4.One;
			m_Renderer.FillLight.Color = 4 * Vector4.One;
			m_Renderer.ToneMappingFactor = 0.12f;

			try
			{
				m_RenderTechniqueSkin = ToDispose( new RenderTechniqueSkin( m_Device, "Skin Render Technique", 2048 ) );
				m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).InsertTechnique( 0, m_RenderTechniqueSkin );	// Insert our technique at the beginning
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Make the shadow map technique running
			m_Renderer.Renderer.PrimitiveAdded += new PrimitiveCollectionChangedEventHandler( Renderer_PrimitiveAdded );
			m_Renderer.Renderer.PrimitiveRemoved += new PrimitiveCollectionChangedEventHandler( Renderer_PrimitiveRemoved );

			m_Renderer.UseCameraNearFarOverride = true;
			m_Renderer.LambdaCorrection = 0.8f;
			m_Renderer.CameraNearOverride = 1.0f;
			m_Renderer.CameraFarOverride = 80.0f;

			m_Scene = ToDispose( new Scene( m_Device, "Default Scene", m_Renderer.Renderer ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
//			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Masha" ) ) );
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/3DHead" ) ) );
			TextureProvider.ForceCreateMipMaps = true;
			TextureProvider.TextureGammaRequested += ( System.IO.FileInfo _File, out float _ImageGamma, out bool _sRGB ) =>
				{
					if ( _File.Extension.ToLower() == ".jpg" )
					{	// JPEGs use 2.2 gamma
						_ImageGamma = Image<PF_Empty>.GAMMA_JPEG;
						_sRGB = false;
						return;
					}
#if true
					_ImageGamma = 1.0f;	// No gamma
					_sRGB = true;		// Consider input as sRGB
#else
					_ImageGamma = 2.2f;	// Apply default gamma correction for JPEGs
					_sRGB = false;		// Don't consider it sRGB
#endif
				};


			//////////////////////////////////////////////////////////////////////////
			// Create the material mapper
			MaterialMap	MMap = new MaterialMap();
			MMap.RegisterDefaultMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{	// Default mapper returns nothing...
					return null;	// We don't really support Lambert mode anyway... ^_^
				} );
			MMap.RegisterMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{	// Phong mapper
					if ( _MaterialParameters.ShaderURL != "Phong" )
						return	null;

					if ( _MaterialParameters.Name == "Mtl_body" )
						return m_RenderTechniqueSkin;	// Use the skin technique for that one...

					return m_Renderer.DefaultTechnique;
				} );

			//////////////////////////////////////////////////////////////////////////
			// Load the scene
			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
//				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Masha/MashaHead.fbx" ), m_Scene, MMap, TextureProvider );
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/3DHead/3DHead.fbx" ), m_Scene, MMap, TextureProvider );
			}

			// Display statistics & errors
			richTextBoxOutput.LogSceneTextureProvider( TextureProvider );

			BuildHierarchyTree();

			// Setup default values
			floatTrackbarControlDiffusionDistance.Value = m_RenderTechniqueSkin.DiffusionDistance;
			floatTrackbarControlNormalAmplitude.Value = m_RenderTechniqueSkin.NormalAmpltiude;
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
			// Create a perspective camera
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( 0.0f, 10.0f, 30.0f ), Vector3.Zero, Vector3.UnitY );


			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============
				if ( !m_Device.CheckCanRender( 10 ) )
					return;

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_Renderer.Render();

				// Show !
				m_Device.Present();
			});
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

		void Renderer_PrimitiveAdded( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			m_Renderer.ShadowMapTechnique.AddShadowCaster( _Primitive );
		}

		void Renderer_PrimitiveRemoved( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			m_Renderer.ShadowMapTechnique.RemoveShadowCaster( _Primitive );
		}

		private void floatTrackbarControlDiffusionDistance_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueSkin.DiffusionDistance = _Sender.Value;
		}

		private void floatTrackbarControlNormalAmplitude_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueSkin.NormalAmpltiude = _Sender.Value;
		}

		private void integerTrackbarControlDebug_ValueChanged( Nuaj.Cirrus.Utility.IntegerTrackbarControl _Sender, int _FormerValue )
		{
			m_RenderTechniqueSkin.DebugInfos = (RenderTechniqueSkin.DEBUG_INFOS) _Sender.Value;
		}

		#endregion
	}
}
