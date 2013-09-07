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
	/// <summary>
	/// This little app demonstrates the use of "Optical Flares" in Nuaj'
	/// It loads several lens flares from disk and creates display objects for each of them that you can then select via a combo box.
	/// 
	/// NOTE: Some lens flares require lens textures that come with the Optical Flares plug-in for After Effect. These textures should
	///  be located in "./Runtime/Media/LensFlares/Optical Flares Textures" but are NOT included in SVN by default !
	/// You can download the plug-in and install these textures yourself by going to the Video Copilot website http://www.videocopilot.net/products/opticalflares/
	/// </summary>
	public partial class DemoForm : Form, IFileLoader
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
		protected RenderTechniquePostProcessLensFlares<PF_RGBA16F>	m_RenderTechniqueLensFlares = null;	// The advanced render technique for lens flares

		protected StringBuilder				m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		// The texture loader
		protected TextureLoader				m_TextureLoader = null;
		protected RenderTargetFactory		m_RenderTargetFactory = null;

		// Lens-Flares
		protected RenderTechniquePostProcessLensFlares<PF_RGBA16F>.LensFlareDisplay[]	m_LensFlares = null;
		protected RenderTechniquePostProcessLensFlares<PF_RGBA16F>.LensFlareDisplay		m_CurrentLensFlare = null;
		protected RenderTechniquePostProcessLensFlares<PF_RGBA16F>.Light				m_CurrentLight = null;

		// The camera & object manipulators
		protected Nuaj.Helpers.CameraManipulator	m_CameraManipulator = new Nuaj.Helpers.CameraManipulator();
		protected Nuaj.Helpers.ObjectsManipulator	m_ObjectsManip = new Nuaj.Helpers.ObjectsManipulator();

		#endregion

		#region PROPERTIES

		protected RenderTechniquePostProcessLensFlares<PF_RGBA16F>.LensFlareDisplay	CurrentLensFlare
		{
			get { return m_CurrentLensFlare; }
			set
			{
				if ( value == m_CurrentLensFlare )
					return;

				if ( m_CurrentLensFlare != null )
				{
					// Remove the main light from the previous flare
					m_CurrentLensFlare.DetachLight( m_CurrentLight );
				}

				m_CurrentLensFlare = value;

				if ( m_CurrentLensFlare != null )
				{
					// Attach the main light to the new flare
					m_CurrentLight = m_CurrentLensFlare.AttachLight( m_Renderer.MainLight );

					// Update brightness
					m_CurrentLensFlare.GlobalBrightness = floatTrackbarBrightness.Value;
				}
			}
		}

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
			// Create our scene render target, with mip-maps for tone mapping
			m_TextureLoader = ToDispose( new TextureLoader( m_Device, "Texture Loader", this ) );
			m_RenderTargetFactory = ToDispose( new RenderTargetFactory( m_Device, "Render Target Factory" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the renderer
			RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
			{
				CameraFOV = 45.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 100.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );
			m_Renderer.MainLight.Position = new Vector3( 4.0f, 3.0f, 0.0f );	// Top right

			// Setup lens-flare technique
			m_RenderTechniqueLensFlares = ToDispose( new RenderTechniquePostProcessLensFlares<PF_RGBA16F>( m_Device, "Lens Flares Render Technique", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, m_TextureLoader, m_RenderTargetFactory ) );
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).AddTechnique( m_RenderTechniqueLensFlares );

			m_RenderTechniqueLensFlares.Camera = m_Renderer.Camera;
			m_RenderTechniqueLensFlares.TargetImage = m_Device.DefaultRenderTarget;

			//////////////////////////////////////////////////////////////////////////
			// Create the manipulator
			m_CameraManipulator.Attach( panelOutput, m_Renderer.Camera );
			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 0.0f, 10.0f ), new Vector3( 0.0f, 0.0f, 0.0f ), Vector3.UnitY );
			m_CameraManipulator.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CameraManipulator_EnableMouseAction );

			m_ObjectsManip.Attach( panelOutput, m_Renderer.Camera );
			m_ObjectsManip.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
			m_ObjectsManip.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
			m_ObjectsManip.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );

			// Register objects
			m_ObjectsManip.RegisterMovableObject( 0, m_Renderer.MainLight.Position, 1.0f );


			//////////////////////////////////////////////////////////////////////////
			// Load the lens-flares
			string[]	LensFlareNames = new string[]
			{
				"Beached",
				"Beam",
				"Blue Spark",
				"Crazy Light",
				"Evening Sun",
				"Gold Spot",
				"Green Spot Light",
				"JayJay",
				"Light Scatter",
				"Main Light",
				"Pink Glow",
//				"Purple Bird",
				"Real Sun",	
				"Red Light",
				"Search Light",
				"Subtle Cool",
				"Subtle Green",
//				"Sun Digital",
//				"Sun Glint",	
				"Tactical Light",
				"Patapom",
			};
			m_LensFlares = new RenderTechniquePostProcessLensFlares<PF_RGBA16F>.LensFlareDisplay[LensFlareNames.Length];

			for ( int LensFlareIndex=0; LensFlareIndex < LensFlareNames.Length; LensFlareIndex++ )
			{
				string		LensFlareName = LensFlareNames[LensFlareIndex];

				// Create and load the lens-flare
				LensFlare	LF = new LensFlare();

				System.IO.FileInfo	FI = new System.IO.FileInfo( "./Data/LensFlares/Lens Flares/Light/" + LensFlareName + ".ofp" );
				using ( System.IO.Stream S = OpenFile( FI ) )
					LF.Load( S );

				// Create the lens-flare display that will take care of displaying that objet
				m_LensFlares[LensFlareIndex] = m_RenderTechniqueLensFlares.CreateLensFlare( LensFlareName, LF );
				comboBoxLensFlare.Items.Add( m_LensFlares[LensFlareIndex] );
			}

			// Select the first lens-flare
			comboBoxLensFlare.SelectedIndex = 0;

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

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Vector4.Zero );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_RenderTechniqueLensFlares.Time = fTotalTime;
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

		#region IFileLoader Members

		public System.IO.Stream OpenFile( System.IO.FileInfo _FileName )
		{
			return _FileName.OpenRead();
		}

		public void ReadBinaryFile( System.IO.FileInfo _FileName, FileReaderDelegate _Reader )
		{
			using ( System.IO.BinaryReader Reader = new System.IO.BinaryReader( OpenFile( _FileName ) ) )
				_Reader( Reader );
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

		private void comboBoxLensFlare_SelectedIndexChanged( object sender, EventArgs e )
		{
			CurrentLensFlare = m_LensFlares[comboBoxLensFlare.SelectedIndex];
		}

		private void floatTrackbarBrightness_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( CurrentLensFlare != null )
				CurrentLensFlare.GlobalBrightness = _Sender.Value;
		}

		#region Manipulation

		bool CameraManipulator_EnableMouseAction( MouseEventArgs _e )
		{
			return false;	// Disable...
		}

		protected bool ObjectsManip_EnableMouseAction( MouseEventArgs _e )
		{
			return true;
		}

		protected void ObjectsManip_ObjectSelected( object _PickedObject, bool _Selected )
		{
			labelHoveredObject.Text = _Selected ? "Hovering light" : "No hovered object...";
		}

		protected void ObjectsManip_ObjectMoving( object _MovedObject, Vector3 _NewPosition )
		{
			m_Renderer.MainLight.Position = _NewPosition;
		}

		#endregion

		#endregion
	}
}
