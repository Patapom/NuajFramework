// Define this to load the FBX file format (much slower !) instead of Nuaj' proprietary format
//#define LOAD_FBX
//#define LOAD_SUBWAY	// Define this to load the Subway scene instead of the simple occluded boxes scene

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
	/// <summary>
	/// This little app demonstrates the Separable Ambient Occlusion technique described in the http://perso.telecom-paristech.fr/~jhuang/paper/SAO.pdf paper
	/// 
	/// </summary>
	public partial class DemoForm : Form
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
		protected RenderTechniquePostProcessSAO	m_TechniqueSAO = null;

		// Default render target
		protected RenderTarget<PF_RGBA8>	m_TempTarget = null;

		// Default scene
		protected Scene						m_Scene = null;

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		protected void	BuildAxes()
		{
			// Build angles
			float[]	Angles = new float[9];
			for ( int i=0; i < 9; i++ )
				Angles[i] = 0.5f * (float) Math.PI * i / 9;

			// Shuffle
			Random	RNG = new Random( 1 );
			for ( int i=0; i < 9; i++ )
			{
				int	j = RNG.Next( 9 );
				float	Temp = Angles[i];
				Angles[i] = Angles[j];
				Angles[j] = Temp;
			}

			// Build string
			string	Axes = "";
			for ( int i=0; i < 9; i++ )
				Axes += ", float2( " + Math.Cos( Angles[i] ) + ", " + Math.Sin( Angles[i] ) + " )";
		}

		public DemoForm()
		{
			InitializeComponent();

			BuildAxes();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					OutputHandle = Handle,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, this, true ) );	// <= Make sure the ZBuffer is readable here !
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}


			//////////////////////////////////////////////////////////////////////////
			// Create the rendere & techniques
			try
			{
				RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
				{
					CameraFOV = 63.0f * (float) Math.PI / 180.0f,
					CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
					CameraClipNear = 0.1f,
					CameraClipFar = 1000.0f,
					bUseAlphaToCoverage = true
				};

				m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

				m_Renderer.MainLight.Intensity = 3.0f;
				m_Renderer.FillLight.Color = new Vector4( 0.2f, 0.4f, 0.8f, 0.0f );
				m_Renderer.FillLight.Intensity = 0.5f;
				m_Renderer.FillLight.Direction = new Vector3( -1.0f, 0.2f, -0.5f );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			//////////////////////////////////////////////////////////////////////////
			// Create the temp target the scene will render to
			m_TempTarget = ToDispose( new RenderTarget<PF_RGBA8>( m_Device, "TempTarget", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 1 ) );


			//////////////////////////////////////////////////////////////////////////
			// Build the SAO technique & the post-process pipeline
			Pipeline	PPPipeline = new Pipeline( m_Device, "Post-Process", Pipeline.TYPE.POST_PROCESSING );
			m_Renderer.Renderer.AddPipeline( PPPipeline );

			m_TechniqueSAO = ToDispose( new RenderTechniquePostProcessSAO( m_Device, "SAO Render Technique", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height ) );
			PPPipeline.AddTechnique( m_TechniqueSAO );
			m_TechniqueSAO.SourceBuffer = m_TempTarget;

			// Make the main pipeline render into a temporary target
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).RenderingStart += new Pipeline.PipelineRenderingEventHandler( MainPipeline_RenderingStart );


			//////////////////////////////////////////////////////////////////////////
			// Load the scene
			m_Scene = ToDispose( new Scene( m_Device, "Default Scene", m_Renderer.Renderer ) );

			// Create the Texture Provider
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Test0" ) ) );
			TextureProvider.ForceCreateMipMaps = true;

#if LOAD_SUBWAY
			string	SceneFileName = "./Meshes/Subway/Subway";
#else
			string	SceneFileName = "./Meshes/OccludedBoxes/OccludedBoxes";
#endif

#if LOAD_FBX
			// Create the material mapper
			MaterialMap	MMap = new MaterialMap();
			MMap.RegisterMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{	// Phong mapper
					return m_Renderer.DefaultTechnique;
				} );

			// Load !
			using ( Nuaj.Helpers.FBX.SceneLoader SceneLoader = new Nuaj.Helpers.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
				SceneLoader.Load( new System.IO.FileInfo( SceneFileName + ".fbx" ), m_Scene, MMap, TextureProvider );
			}

			using ( System.IO.FileStream F = new System.IO.FileInfo( SceneFileName + ".nuaj" ).Create() )
				using ( System.IO.BinaryWriter W = new System.IO.BinaryWriter( F ) )
					m_Scene.Save( W );
#else
			using ( System.IO.FileStream F = new System.IO.FileInfo( SceneFileName + ".nuaj" ).OpenRead() )
				using ( System.IO.BinaryReader R = new System.IO.BinaryReader( F ) )
					m_Scene.Load( R, TextureProvider );
#endif

			if ( TextureProvider.HasErrors )
			{	// Display errors
				string	Errors = "";
				foreach ( string Error in TextureProvider.TextureErrors )
					Errors += "   ●  " + Error + "\r\n";
				MessageBox.Show( this, "The texture provider has some errors !\r\n\r\n" + Errors, "Texture Errors !", MessageBoxButtons.OK, MessageBoxIcon.Error );
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
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( this, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( 0.0f, 2.0f, 10.0f ), Vector3.Zero, Vector3.UnitY );

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
				m_Device.ClearRenderTarget( m_TempTarget, (Color4) Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
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
					Text = InitialText + " - Radius = " + m_TechniqueSAO.AOSphereRadius.ToString( "G4" ) + " - Strength = " + m_TechniqueSAO.AOStrength.ToString( "G4" ) + " - Scale = " + m_TechniqueSAO.AOFetchScale.ToString( "G4" ) + " - " + FPS.ToString( "G4" ) + " FPS";
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

		protected bool										m_bAOOnly = false;
		protected RenderTechniquePostProcessSAO.AO_STATE	m_OldState;
		protected override void OnPreviewKeyDown( PreviewKeyDownEventArgs e )
		{
			base.OnPreviewKeyDown( e );

			switch ( e.KeyCode )
			{
				case Keys.NumPad9:
					m_TechniqueSAO.AOSphereRadius += 0.02f;
					break;
				case Keys.NumPad7:
					m_TechniqueSAO.AOSphereRadius -= 0.02f;
					break;

				case Keys.NumPad6:
					m_TechniqueSAO.AOStrength += 0.01f;
					break;
				case Keys.NumPad4:
					m_TechniqueSAO.AOStrength -= 0.01f;
					break;

				case Keys.NumPad3:
					m_TechniqueSAO.AOFetchScale += 0.05f;
					break;
				case Keys.NumPad1:
					m_TechniqueSAO.AOFetchScale -= 0.05f;
					break;

				case Keys.Space:
					if ( m_TechniqueSAO.AOState == RenderTechniquePostProcessSAO.AO_STATE.ENABLED )
						m_TechniqueSAO.AOState = RenderTechniquePostProcessSAO.AO_STATE.DISABLED;
					else
						m_TechniqueSAO.AOState = RenderTechniquePostProcessSAO.AO_STATE.ENABLED;
					m_bAOOnly = false;
					break;

				case Keys.Return:
					if ( m_bAOOnly )
					{
						m_TechniqueSAO.AOState = m_OldState;
						m_bAOOnly = false;
					}
					else
					{
						m_OldState = m_TechniqueSAO.AOState;
						m_TechniqueSAO.AOState = RenderTechniquePostProcessSAO.AO_STATE.AO_ONLY;
						m_bAOOnly = true;
					}
					break;
			}
		}

		#endregion

		#region EVENT HANDLERS

		void MainPipeline_RenderingStart( Pipeline _Sender )
		{
			m_Device.SetRenderTarget( m_TempTarget, m_Device.DefaultDepthStencil );
		}

		#endregion
	}
}
