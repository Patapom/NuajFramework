// Define this to load the simple scene
//#define SIMPLE_SCENE

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

		protected Scene						m_Scene = null;

		protected int						m_ShadowMapDisplayIndex = 0;

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
			// Create the renderer & a default scene
			try
			{
				RendererSetupShadowMap.InitParams	Params = new RendererSetupShadowMap.InitParams()
				{
					bUseAlphaToCoverage = true,
					CameraFOV = 45.0f * (float) Math.PI / 180.0f,
					CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
					CameraClipNear=0.01f,
					CameraClipFar=1000.0f,
					ShadowMapSlicesCount=3,
					ShadowMapSize=1024
				};

				m_Renderer = ToDispose( new RendererSetupShadowMap( m_Device, "Renderer", Params ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			m_Renderer.MainLight.Color = 10.0f * Vector4.One;
			m_Renderer.FillLight.Color = 3.0f * (Vector4) (Color4) Color.CornflowerBlue;
			m_Renderer.ToneMappingFactor = 0.2f;

			m_Renderer.Renderer.PrimitiveAdded += new PrimitiveCollectionChangedEventHandler( Renderer_PrimitiveAdded );
			m_Renderer.Renderer.PrimitiveRemoved += new PrimitiveCollectionChangedEventHandler( Renderer_PrimitiveRemoved );

			m_Renderer.UseCameraNearFarOverride = true;
#if SIMPLE_SCENE
			m_Renderer.LambdaCorrection = 0.6f;
			m_Renderer.CameraNearOverride = 0.1f;
			m_Renderer.CameraFarOverride = 200.0f;
#else
			m_Renderer.LambdaCorrection = 0.90f;
			m_Renderer.CameraNearOverride = 0.1f;
			m_Renderer.CameraFarOverride = 400.0f;
#endif

			// Create the scene
			m_Scene = ToDispose( new Scene( m_Device, "Default Scene", m_Renderer.Renderer ) );


			//////////////////////////////////////////////////////////////////////////
			// Create a camera manipulator
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
#if SIMPLE_SCENE
			CamManip.InitializeCamera( new Vector3( 0.0f, 20.0f, 80.0f ), Vector3.Zero, Vector3.UnitY );
#else
			CamManip.InitializeCamera( new Vector3( -300.0f, 150.0f, -246.0f ), new Vector3( -72.0f, 37.0f, -90.0f ), Vector3.UnitY );
#endif


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
#if SIMPLE_SCENE
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Test0" ) ) );
#else
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Castle/textures" ) ) );
			TextureProvider.AddSourceRootPathToStripOff( @"T:\TheWitcher_Project\Packages\Environments\levels\L08\_env_l08\textures\" );
			TextureProvider.AddSourceRootPathToStripOff( @"textures\" );
#endif
			TextureProvider.ForceCreateMipMaps = true;


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

// 					if ( _MaterialParameters.Name == "Mtl_body" )
// 						return m_RenderTechniqueShadowMap;	// Use the skin technique for that one...

					return m_Renderer.DefaultTechnique;
				} );


			//////////////////////////////////////////////////////////////////////////
			// Load the scene
			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
#if SIMPLE_SCENE
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Test0/Test0.fbx" ), m_Scene, MMap, TextureProvider );
#else
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Castle/CastleL08.fbx" ), m_Scene, MMap, TextureProvider );
//				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Castle/CastleL08_Light.fbx" ), m_Scene, MMap, TextureProvider );
#endif
			}


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

			Nuaj.Helpers.ScreenQuad	Quad = new Nuaj.Helpers.ScreenQuad( m_Device, "Quad" );
			Material<VS_Pt4V3T2>	QuadMaterial = new Material<VS_Pt4V3T2>( m_Device, "QuadMat", ShaderModel.SM4_0, Properties.Resources.ShadowMapPostDisplay );

			VariableScalar			vShadowMapDisplayIndex = QuadMaterial.GetVariableByName( "ShadowMapDisplayIndex" ).AsScalar;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;


				// =============== Cull Scene ===============
				m_Scene.PerformCulling( m_Renderer.Camera.World2Camera, m_Renderer.Camera.Frustum );


				// =============== Render Scene ===============

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_Renderer.Render();

				// Debug shadow maps
				int	ShadowMapDisplayIndex = m_ShadowMapDisplayIndex % (m_Renderer.ShadowMapTechnique.Params.SlicesCount+1);
				if ( ShadowMapDisplayIndex > 0 )
				{
					vShadowMapDisplayIndex.Set( ShadowMapDisplayIndex-1 );
					m_Device.SetViewport( 0, 0, 256, 256, 0.0f, 1.0f );
					using ( QuadMaterial.UseLock() )
						QuadMaterial.Render( ( a, b, c ) => { Quad.Render(); } );
				}

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

		void Renderer_PrimitiveAdded( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			// Add any shadow caster to the shadow technique
			if ( _Primitive.CastShadow )
				m_Renderer.ShadowMapTechnique.AddShadowCaster( _Primitive );
		}

		void Renderer_PrimitiveRemoved( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			if ( _Primitive.CastShadow )
				m_Renderer.ShadowMapTechnique.RemoveShadowCaster( _Primitive );
		}

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

		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			if ( e.KeyData == Keys.Add )
				m_ShadowMapDisplayIndex++;
			else if ( e.KeyData == Keys.Subtract )
				m_ShadowMapDisplayIndex--;
		}

		#endregion
	}
}
