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

		#endregion

		#region FIELDS

		protected Nuaj.Device					m_Device = null;

		protected RenderTechniquePhotonMapping	m_PhotonTracer = null;

		protected Camera						m_Camera = null;

		// Manipulators
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
			m_PhotonTracer = ToDispose( new RenderTechniquePhotonMapping( m_Device, "Photon Tracer" ) );


			//////////////////////////////////////////////////////////////////////////
			// Create manipulators
			m_Camera = ToDispose( new Camera( m_Device, "Camrea" ) );
			m_Camera.CreatePerspectiveCamera( 70.0f * (float) Math.PI / 180.0f, m_Device.DefaultRenderTarget.AspectRatio, 0.01f, 1000.0f );
			m_Camera.Activate();

			m_CameraManipulator = new Nuaj.Helpers.CameraManipulator();
			m_CameraManipulator.Attach( panelOutput, m_Camera );
			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 0.0f, 8.0f ), new Vector3( 0.0f, 0.0f, 0.0f ), Vector3.UnitY );
//			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 0.25f, 0.0f ), new Vector3( 0.0f, 1.0f, -8.0f ), Vector3.UnitY );
			m_CameraManipulator.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CamManip_EnableMouseAction );

			m_ObjectsManipulator = new Nuaj.Helpers.ObjectsManipulator();
			m_ObjectsManipulator.Attach( panelOutput, m_Camera );
//			m_ObjectsManipulator.RegisterMovableObject( 0, m_Omni.Position, m_Omni.OuterRadius );
// 			m_ObjectsManipulator.RegisterMovableObject( 0, (Vector3) m_Scene2.RootNode.Local2Parent.Row4, 4.0f );
// 			m_ObjectsManipulator.RegisterMovableObject( 1, m_Spot.Position, m_Spot.OuterRadius );
			m_ObjectsManipulator.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
			m_ObjectsManipulator.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
			m_ObjectsManipulator.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );


			//////////////////////////////////////////////////////////////////////////
			// Display statistics & errors
			richTextBoxOutput.Log( "Texture Provider :\n" );

//			BuildHierarchyTree();

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
			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;
			DateTime	LastFPSTime = DateTime.Now;
			int			FPSFramesCount = 0;

			SharpDX.Windows.MessagePump.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============

				m_Device.StartProfiling( m_ProfilerForm.FlushEveryOnTask );
				m_Device.AddProfileTask( null, "Frame", "<START>" );

				// Clear
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Vector4.Zero );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Render environment map using current sky & sun light
//				m_Device.AddProfileTask( null, "SHEnvMap", "Render EnvMap" );
//				m_SHEnvMapManager.RenderSHEnvironmentMap( m_Renderer.Camera, m_Renderer.SkyTechnique.SkyLightSH );

				// Draw
				m_PhotonTracer.Render( 0 );

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

/*
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
*/

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

		#endregion
	}
}
