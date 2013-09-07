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

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
		protected RenderTechniqueCaustics2	m_RenderTechniqueCaustics = null;	// The advanced render technique for caustics

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
			RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
			{
				CameraFOV = 70.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 10.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

			// Setup the default lights
			m_Renderer.MainLight.Color = 10 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.MainLight.Direction = new Vector3( 0, 1, 0 );
			m_Renderer.FillLight.Color = 4 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.ToneMappingFactor = 0.9f;

			m_RenderTechniqueCaustics = ToDispose( new RenderTechniqueCaustics2( m_Device, "Caustics Render Technique" ) );
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).InsertTechnique( 0, m_RenderTechniqueCaustics );	// Insert our technique at the beginning

			m_RenderTechniqueCaustics.Camera = m_Renderer.Camera;
			m_RenderTechniqueCaustics.LightPosition = new Vector3( 0.5f, 0.5f, -0.1f );
			m_RenderTechniqueCaustics.LightIntensity = 0.25f;
//			m_RenderTechniqueCaustics.SpherePosition = new Vector3( -0.5f, 0.0f, 0.0f );


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
//			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Masha" ) ) );
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Media/Terrain" ) ) );
			TextureProvider.ForceCreateMipMaps = true;


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
			// Create a camera manipulator
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( 0.0f, 0.0f, 2.0f ), new Vector3( 0.0f, 0.0f, 0.0f ), Vector3.UnitY );
			CamManip.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CamManip_EnableMouseAction );

			Nuaj.Helpers.ObjectsManipulator	ObjectsManip = new Nuaj.Helpers.ObjectsManipulator();
			ObjectsManip.Attach( panelOutput, m_Renderer.Camera );
			ObjectsManip.RegisterMovableObject( 0, m_RenderTechniqueCaustics.SpherePosition, m_RenderTechniqueCaustics.SphereRadius );
			ObjectsManip.RegisterMovableObject( 1, m_RenderTechniqueCaustics.LightPosition, m_RenderTechniqueCaustics.LightRadius );
			ObjectsManip.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
			ObjectsManip.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
			ObjectsManip.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );


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

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, (Color4) Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				m_RenderTechniqueCaustics.Time = fTotalTime;


				// Animate light position
				double	Phi = 0.1 * fTotalTime * 2.0 * Math.PI;
				double	Theta = 0.03 * fTotalTime * Math.PI;
				double	Radius = 0.5;
//				Vector3	Pos = new Vector3( (float) (Radius * Math.Sin( Phi ) * Math.Sin( Theta )), (float) (Radius * Math.Cos( Theta )), (float) (Radius * Math.Cos( Phi ) * Math.Sin( Theta )) );
				Vector3	Pos = new Vector3(
					(float) (Radius * (Math.Sin( 3.0 * Phi ) * Math.Cos( 2.7 * Theta ) + Math.Cos( 1.3 * Phi ) * Math.Sin( 0.3 * Theta ) )),
					(float) (Radius * (Math.Sin( 1.0 * Phi ) * Math.Cos( -0.7 * Theta ) + Math.Cos( 1.0 * Phi ) * Math.Sin( 0.5 * Theta ) )),
					(float) (Radius * (Math.Sin( 0.7 * Phi ) * Math.Cos( -.1 * Theta ) + Math.Cos( -1.8 * Phi ) * Math.Sin( -0.3 * Theta ) ))
					);
//				m_RenderTechniqueCaustics.LightPosition = Pos;

				// Draw
				m_Renderer.Render();

				// Show !
				m_Device.Present();
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

		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			float	fSpeed = 0.01f;
//			Vector3	Pos = m_RenderTechniqueCaustics.LightPosition;
			Vector3	Pos = m_RenderTechniqueCaustics.SpherePosition;

			switch ( e.KeyCode )
			{
				case Keys.NumPad4:
					Pos.X -= fSpeed;
					break;
				case Keys.NumPad6:
					Pos.X += fSpeed;
					break;
				case Keys.NumPad2:
					Pos.Y -= fSpeed;
					break;
				case Keys.NumPad8:
					Pos.Y += fSpeed;
					break;
				case Keys.NumPad3:
					Pos.Z -= fSpeed;
					break;
				case Keys.NumPad9:
					Pos.Z += fSpeed;
					break;
				case Keys.NumPad5:
					Pos = Vector3.Zero;	// Reset
					break;
			}

//			m_RenderTechniqueCaustics.LightPosition = Pos;
			m_RenderTechniqueCaustics.SpherePosition = Pos;
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
					labelHoveredObject.Text = "Hovering sphere";
					break;
				case 1:
					labelHoveredObject.Text = "Hovering light";
					break;
			}
		}

		void ObjectsManip_ObjectMoving( object _MovedObject, Vector3 _NewPosition )
		{
			switch ( (int) _MovedObject )
			{
				case 0:
					m_RenderTechniqueCaustics.SpherePosition = _NewPosition;
					break;
				case 1:
					m_RenderTechniqueCaustics.LightPosition = _NewPosition;
					break;
			}
		}

		#endregion
	}
}
