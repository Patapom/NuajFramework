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
		protected RenderTechniqueGrass	m_RenderTechniqueGrass = null;	// The advanced render technique for distance field ray-tracing

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
				CameraFOV = 45.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 50.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

			// Setup the default lights
			m_Renderer.MainLight.Color = new Vector4( 1, 1, 1, 1 );
			m_Renderer.MainLight.Direction = new Vector3( 0, 1, 0 );
			m_Renderer.FillLight.Color = new Vector4( 1, 1, 1, 1 );
			m_Renderer.ToneMappingFactor = 1.0f;

			m_RenderTechniqueGrass = ToDispose( new RenderTechniqueGrass( m_Device, "Grass Render Technique" ) );
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).InsertTechnique( 0, m_RenderTechniqueGrass );	// Insert our technique at the beginning

			m_RenderTechniqueGrass.Camera = m_Renderer.Camera;


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
			// Create a perspective camera
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( 7.0f, 0.2f, 8.0f ), new Vector3( 0.0f, -4.0f, 0.0f ), Vector3.UnitY );
			CamManip.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CamManip_EnableMouseAction );

// 			Nuaj.Helpers.ObjectsManipulator	ObjectsManip = new Nuaj.Helpers.ObjectsManipulator();
// 			ObjectsManip.Attach( panelOutput, Cam );
// 			ObjectsManip.RegisterMovableObject( 0, m_RenderTechniqueGrass.SpherePosition, m_RenderTechniqueGrass.SphereRadius );
// 			ObjectsManip.RegisterMovableObject( 1, m_RenderTechniqueGrass.LightPosition, m_RenderTechniqueGrass.LightRadius );
// 			ObjectsManip.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
// 			ObjectsManip.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
// 			ObjectsManip.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );


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

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				m_RenderTechniqueGrass.Time = fTotalTime;

				// Draw
				m_Renderer.Render();

				// Show !
				m_Device.Present();

				// Update FPS
				FPSFramesCount++;
				DateTime	Now = DateTime.Now;
				if ( (Now - LastFPSTime).TotalMilliseconds > 1000 )
				{
					float	FPS = (float) (FPSFramesCount * 1000.0f / (Now - LastFPSTime).TotalMilliseconds);
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
					labelHoveredObject.Text = "Hovering sphere";
					break;
				case 1:
					labelHoveredObject.Text = "Hovering light";
					break;
			}
		}

		void ObjectsManip_ObjectMoving( object _MovedObject, Vector3 _NewPosition )
		{
// 			switch ( (int) _MovedObject )
// 			{
// 				case 0:
// 					m_RenderTechniqueGrass.SpherePosition = _NewPosition;
// 					break;
// 				case 1:
// 					m_RenderTechniqueGrass.LightPosition = _NewPosition;
// 					break;
// 			}
		}

		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			if ( e.KeyCode == Keys.Space )
				m_RenderTechniqueGrass.Gust = true;
		}

		#endregion
	}
}
