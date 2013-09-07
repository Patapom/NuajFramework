#define USE_SEQUENCOR_FORM

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
	/// <summary>
	/// First of my demos ever to use my brand new sequencer !! ^o^
	/// </summary>
	public partial class DemoForm : Form
	{
		#region CONSTANTS
		
		protected const int		MULTISAMPLES_COUNT = 8;	// MSAA count for anti-aliasing

		#endregion

		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		protected SoundLib.MP3Player		m_Sound = null;
		protected System.IO.Stream			m_MP3Stream = null;

		// Sequencor
		protected SequencorLib.Sequencor	m_Sequencor = null;
#if USE_SEQUENCOR_FORM
		protected SequencorEditor.SequencerForm	m_SequencorForm = null;
#endif

		// The Cirrus renderer
		protected RendererSetupDemo			m_Renderer = null;

		// The camera & object manipulators
		protected Nuaj.Helpers.CameraManipulator	m_CameraManipulator = new Nuaj.Helpers.CameraManipulator();
		protected Nuaj.Helpers.ObjectsManipulator	m_ObjectsManip = new Nuaj.Helpers.ObjectsManipulator();

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

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, panelOutput, true ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}

			//////////////////////////////////////////////////////////////////////////
			// Create the renderer
			m_Renderer = ToDispose( new RendererSetupDemo( m_Device, "Renderer", MULTISAMPLES_COUNT, 70.0f * (float) Math.PI / 180.0f, (float) panelOutput.Width / panelOutput.Height, 0.01f, 100.0f ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the camera manipulator
			m_CameraManipulator.Attach( panelOutput, m_Renderer.DefaultCamera );
//			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 2.0f, 10.0f ), new Vector3( 0.0f, 2.5f, 0.0f ), Vector3.UnitY );
			m_CameraManipulator.InitializeCamera( new Vector3( 0.0f, 0.0f, 10.0f ), new Vector3( 0.0f, 0.0f, 0.0f ), Vector3.UnitY );
			m_CameraManipulator.EnableMouseAction += new Nuaj.Helpers.CameraManipulator.EnableMouseActionEventHandler( CamManip_EnableMouseAction );

			//////////////////////////////////////////////////////////////////////////
			// Create the objects manipulator
			m_ObjectsManip.Attach( panelOutput, m_Renderer.Camera );
			m_ObjectsManip.EnableMouseAction += new Nuaj.Helpers.ObjectsManipulator.EnableMouseActionEventHandler( ObjectsManip_EnableMouseAction );
			m_ObjectsManip.ObjectSelected += new Nuaj.Helpers.ObjectsManipulator.ObjectSelectionEventHandler( ObjectsManip_ObjectSelected );
			m_ObjectsManip.ObjectMoving += new Nuaj.Helpers.ObjectsManipulator.ObjectMoveEventHandler( ObjectsManip_ObjectMoving );

			// Register objects
			if ( m_Renderer.TechniqueInk != null )
			{
				m_ObjectsManip.RegisterMovableObject( 0, m_Renderer.TechniqueInk.SpherePosition, m_Renderer.TechniqueInk.SphereRadius );
				m_ObjectsManip.RegisterMovableObject( 1, m_Renderer.TechniqueInk.LightPosition, m_Renderer.TechniqueInk.LightRadius );
			}


			m_Renderer.CameraChanged += new EventHandler( Renderer_CameraChanged );
			Renderer_CameraChanged( m_Renderer, EventArgs.Empty );


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
//			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Masha" ) ) );
// 			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Media/Terrain" ) ) );
// 			TextureProvider.ForceCreateMipMaps = true;
// 
// 
// 			// Display statistics & errors
// 			richTextBoxOutput.Log( "Texture Provider :\n" );
// 			richTextBoxOutput.Log( "> " + TextureProvider.LoadedTexturesCount + " textures loaded.\n" );
// 			int	MinSize = (int) Math.Sqrt( TextureProvider.MinTextureSurface );
// 			int	MaxSize = (int) Math.Sqrt( TextureProvider.MaxTextureSurface );
// 			int	AvgSize = (int) Math.Sqrt( TextureProvider.AverageTextureSurface );
// 			int	TotalSize = (int) Math.Sqrt( TextureProvider.TotalTextureSurface );
// 			richTextBoxOutput.Log( "> Surface Min=" + MinSize + "x" + MinSize + " Max=" + MaxSize + "x" + MaxSize + " Avg=" + AvgSize + "x" + AvgSize + "\n" );
// 			richTextBoxOutput.LogWarning( "> Surface Total=" + TotalSize + "x" + TotalSize + " (Memory=" + (TextureProvider.TotalTextureMemory>>10) + " Kb)\n" );
// 
// 			if ( TextureProvider.HasErrors )
// 			{	// Display errors
// 				richTextBoxOutput.Log( "The texture provider has some errors !\r\n\r\n" );
// 				foreach ( string Error in TextureProvider.TextureErrors )
// 					richTextBoxOutput.LogError( "   ●  " + Error + "\r\n" );
// 			}
// 			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );

			BuildHierarchyTree();

			//////////////////////////////////////////////////////////////////////////
			// Load music
			m_Sound = ToDispose( new SoundLib.MP3Player() );
			System.IO.FileInfo	MP3File = new System.IO.FileInfo( "./Sound/Watercolour.mp3" );
			if ( !MP3File.Exists )
				throw new Exception( "Failed to find music file !" );

			m_MP3Stream = m_Renderer.OpenFile( MP3File );
			m_Sound.Load( m_MP3Stream );

			//////////////////////////////////////////////////////////////////////////
			// Create the sequencor & form
#if USE_SEQUENCOR_FORM
			System.IO.FileInfo	SequencerFile = new System.IO.FileInfo( "./Sound/Watercolour.sqcProj" );
			m_SequencorForm = new SequencorEditor.SequencerForm();
			m_SequencorForm.IsEmbedded = true;	// We're using it embedded...

			// Subscribing to that event allows the sequencer to sample our parameters
			m_SequencorForm.SequencerControl.ParameterValueNeeded += new SequencorEditor.SequencerControl.ProvideParameterValueEventHandler(SequencerControl_ParameterValueNeeded);

			// Simple play/stop events
			m_SequencorForm.SequencerControl.SequencePlay += new EventHandler( SequencerControl_SequencePlay );
			m_SequencorForm.SequencerControl.SequencePause += new EventHandler( SequencerControl_SequencePause );
			m_SequencorForm.SequencerControl.SequenceTimeChanged += new EventHandler( SequencerControl_SequenceTimeChanged );
			m_SequencorForm.SequencerControl.SequenceTimeNeeded += new SequencorEditor.SequencerControl.ProvideSequenceTimeEventHandler( SequencerControl_SequenceTimeNeeded );

			// Reload project
			m_SequencorForm.LoadProject( SequencerFile, new SequencorLib.Sequencor.TagNeededEventHander( Sequencor_TagNeeded ) );

			m_Sequencor = m_SequencorForm.Sequencer;
#else
			System.IO.FileInfo	SequencerFile = new System.IO.FileInfo( "./Sound/Watercolour.sqc" );	// A binary export of the project file
			m_Sequencor = new SequencorLib.Sequencor();
			m_Sequencor.TagNeeded += new SequencorLib.Sequencor.TagNeededEventHander( Sequencor_TagNeeded );

//			m_Sequencor.LoadFromBinaryFile( SequencerFile );
			m_Renderer.ReadBinaryFile( SequencerFile, ( Reader ) => { m_Sequencor.Load( Reader ); } );
#endif

			// Subscribe to the sequencor events
			m_Sequencor.ParameterChanged += new SequencorLib.Sequencor.ParameterChangedEventHandler( Sequencor_ParameterChanged );
			m_Sequencor.EventFired += new SequencorLib.Sequencor.EventFiredEventHandler( Sequencor_EventFired );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			// Dispose of shit
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			// Hide sequencor
			m_SequencorForm.Dispose();

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

#if !USE_SEQUENCOR_FORM
			m_Sound.Play();
#endif

#if true// DEBUG
			string		InitialText = Text;
			DateTime	LastFPSTime = DateTime.Now;
			int			FPSFramesCount = 0;
#endif
			SharpDX.Windows.MessagePump.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

//				float	fTotalTime = m_Sound.Position * 0.001f;

				// =============== Render Scene ===============

				m_Device.StartProfiling( false );
				m_Device.AddProfileTask( null, "Frame", "<START>" );

				// Draw
				m_Renderer.Time = fTotalTime;
				m_Renderer.Render();

				// Show !
				m_Device.AddProfileTask( null, "Device", "Present" );
				m_Device.Present();

				m_Device.AddProfileTask( null, "Frame", "<END>" );
				m_Device.EndProfiling();

#if true// DEBUG
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
#endif
			} );
		}

		protected float		m_FlashTimeStart = -1000.0f;
		protected void	Flash()
		{
			m_FlashTimeStart = m_Sequencor.Time;
		}
		protected void	ClearAnimations()
		{
			m_FlashTimeStart = -1000.0f;
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

		protected bool	m_bEnableTime = true;
		protected override void OnKeyDown( KeyEventArgs e )
		{
			if ( e.KeyCode == Keys.Space )
				m_Renderer.PPColorimetry.ShowMire = !m_Renderer.PPColorimetry.ShowMire;

			base.OnKeyDown( e );
		}

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
#if DEBUG
			switch ( e.KeyCode )
			{
#if USE_SEQUENCOR_FORM
				case Keys.F1:
					if ( m_SequencorForm.Visible )
						m_SequencorForm.Visible = false;
					else
						m_SequencorForm.Show( this );
					break;

				case Keys.F6:
					m_Renderer.CycleClips();
					break;
#endif

				// Camera replay
				case Keys.NumPad4:
					m_Renderer.PPMotionBlur.m_RecordedCameraFrameIndex--;
					break;
				case Keys.NumPad6:
					m_Renderer.PPMotionBlur.m_RecordedCameraFrameIndex++;
					break;
				case Keys.Return:
					m_Renderer.PPMotionBlur.m_bUseReverse = !m_Renderer.PPMotionBlur.m_bUseReverse;
					break;
			}
#endif
		}

		protected void Renderer_CameraChanged( object sender, EventArgs e )
		{
			m_CameraManipulator.ManipulatedCamera = m_Renderer.Camera;
			m_ObjectsManip.AttachedCamera = m_Renderer.Camera;
		}

		protected bool CamManip_EnableMouseAction( MouseEventArgs _e )
		{
			return (Control.ModifierKeys & Keys.Control) == 0;	// Can't manipulate camera if Control is pressed
		}

		protected bool ObjectsManip_EnableMouseAction( MouseEventArgs _e )
		{
			return (Control.ModifierKeys & Keys.Control) != 0;	// Can't manipulate objects if Control is NOT pressed
		}

		protected void ObjectsManip_ObjectSelected( object _PickedObject, bool _Selected )
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

		protected void ObjectsManip_ObjectMoving( object _MovedObject, Vector3 _NewPosition )
		{
			switch ( (int) _MovedObject )
			{
				case 0:
					m_Renderer.TechniqueInk.SpherePosition = _NewPosition;
					break;
				case 1:
					m_Renderer.TechniqueInk.LightPosition = _NewPosition;
					m_Renderer.TechniqueMegaParticles.LightPosition = _NewPosition;
					break;
			}
		}

		#region Sequencing

		protected object Sequencor_TagNeeded( SequencorLib.Sequencor.ParameterTrack _Parameter )
		{
			// Attach tags to parameters...
			switch ( _Parameter.GUID )
			{
				case 1:
					return 1;
			}

//			throw new Exception( "Unsupported parameter GUID " + _Parameter + " !" );
			return null;
		}

		protected void Sequencor_ParameterChanged( SequencorLib.Sequencor.ParameterTrack _Parameter, SequencorLib.Sequencor.ParameterTrack.Interval _Interval )
		{
			switch ( _Parameter.GUID )
			{
				case 1:
					float	Flash = (float) Math.Exp( -4.0 * Math.Max( 0.0f, m_Sequencor.Time - m_FlashTimeStart) );
					m_Renderer.TechniqueInk.LightIntensity = _Parameter.ValueAsFloat + Flash;
					m_Renderer.TechniqueMegaParticles.LightIntensity = 2.5f * (_Parameter.ValueAsFloat + Flash);
					break;

				case 3:	// Room matrix
					m_Renderer.TechniqueInk.Local2World = _Parameter.ValueAsPRS;
					break;

				case 4:	// Light Position
					// Update the manipulator, that should update the position
					m_ObjectsManip.UpdateMovableObjectPosition( 1, _Parameter.ValueAsFloat3 );
					break;


				// Colorimetry
				case 1000:	// HSL shadows
					m_Renderer.PPColorimetry.SetShadowsShiftSatContrast( _Parameter.ValueAsFloat4 );
					break;

				case 1001:	// HSL midtones
					m_Renderer.PPColorimetry.SetMidtonesShiftSatContrast( _Parameter.ValueAsFloat4 );
					break;

				case 1002:	// HSL highlights
					m_Renderer.PPColorimetry.SetHighlightsShiftSatContrast( _Parameter.ValueAsFloat4 );
					break;
			}
		}

		protected void Sequencor_EventFired( SequencorLib.Sequencor.ParameterTrack _Parameter, SequencorLib.Sequencor.ParameterTrack.Interval _Interval, SequencorLib.Sequencor.AnimationTrack _Track, int _EventGUID )
		{
			switch ( _EventGUID )
			{
				case 1:
					Flash();
					break;
			}
		}

#if USE_SEQUENCOR_FORM
		protected void SequencerControl_SequencePlay( object sender, EventArgs e )
		{
			m_Sound.Play();
		}

		protected void SequencerControl_SequencePause( object sender, EventArgs e )
		{
			m_Sound.Pause();
		}

		protected void SequencerControl_SequenceTimeChanged( object sender, EventArgs e )
		{
			int		CurrentPos = m_Sound.Position;
			int		NewPos =  (int) (m_SequencorForm.SequencerControl.SequenceTime * 1000.0f);
			if ( m_Sound.Playing && Math.Abs( NewPos-CurrentPos ) < 100 )
 				return;	// Don't change sound time for small values if it's playing as it's the sound that guides timing...

			m_Sound.Position = NewPos;

			// If going backward, then clear animations
			if ( NewPos < CurrentPos )
				ClearAnimations();
		}

		protected float SequencerControl_SequenceTimeNeeded( SequencorEditor.SequencerControl _Sender )
		{
			return m_Sound.Position * 0.001f;
		}

		// This is the sampling event so we can provide current values for the parameter
		protected object SequencerControl_ParameterValueNeeded( SequencorEditor.SequencerControl _Sender, SequencorLib.Sequencor.ParameterTrack _Track )
		{
			switch ( _Track.GUID )
			{
				case 1:
					return m_Renderer.TechniqueInk.LightIntensity;
				case 3:
					return m_Renderer.TechniqueInk.Local2World;
				case 4:
					return m_Renderer.TechniqueInk.LightPosition;

				// Colorimetry
				case 1000:
					return m_Renderer.PPColorimetry.Shift_Shadows;
				case 1001:
					return m_Renderer.PPColorimetry.Shift_Midtones;
				case 1002:
					return m_Renderer.PPColorimetry.Shift_Highlights;
			}

			return null;
		}
#endif

		#endregion

		#endregion
	}
}
