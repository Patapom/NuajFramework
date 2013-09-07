// Undefine this to load from the proprietary format
#define LOAD_FBX

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
		protected RendererSetupBasic		m_Renderer = null;
		protected Scene						m_Scene = null;

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
					ModeDescription = new ModeDescription( ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					OutputHandle = Handle,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, this ) );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}


			//////////////////////////////////////////////////////////////////////////
			// Create the renderer & a default scene
			try
			{
				RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
				{
					CameraFOV = 0.3f * (float) Math.PI,
					CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
					CameraClipNear = 0.01f,
					CameraClipFar = 1000.0f,
					bUseAlphaToCoverage = true
				};

				m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			m_Scene = ToDispose( new Scene( m_Device, "Default Scene", m_Renderer.Renderer ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Test0" ) ) );
			TextureProvider.ForceCreateMipMaps = true;


#if LOAD_FBX
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

					return m_Renderer.DefaultTechnique;
				} );


			//////////////////////////////////////////////////////////////////////////
			// Load the scene
			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/Test0/Test0.fbx" ), m_Scene, MMap, TextureProvider );
			}

			if ( TextureProvider.HasErrors )
			{	// Display errors
				string	Errors = "";
				foreach ( string Error in TextureProvider.TextureErrors )
					Errors += "   ●  " + Error + "\r\n";
				MessageBox.Show( this, "The texture provider has some errors !\r\n\r\n" + Errors, "Texture Errors !", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}

			//////////////////////////////////////////////////////////////////////////
			// Serialization tests
			System.IO.FileInfo	SceneFile = new System.IO.FileInfo( "./Scenes/Test0.nuaj" );
			using ( System.IO.FileStream SceneStream = SceneFile.Create() )
			{
				using ( System.IO.BinaryWriter SceneWriter = new System.IO.BinaryWriter( SceneStream ) )
				{
					m_Scene.Save( SceneWriter );
				}
			}

			// Reload it...
			using ( System.IO.FileStream SceneStream = SceneFile.OpenRead() )
			{
				using ( System.IO.BinaryReader SceneReader = new System.IO.BinaryReader( SceneStream ) )
				{
					m_Scene.Load( SceneReader, TextureProvider );
				}
			}

			if ( TextureProvider.HasErrors )
			{	// Display errors
				string	Errors = "";
				foreach ( string Error in TextureProvider.TextureErrors )
					Errors += "   ●  " + Error + "\r\n";
				MessageBox.Show( this, "The texture provider has some errors !\r\n\r\n" + Errors, "Texture Errors !", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
#else
			// Load the scene directly from proprietary format...
			System.IO.FileInfo	SceneFile = new System.IO.FileInfo( "./Scenes/Test0.nuaj" );
			using ( System.IO.FileStream SceneStream = SceneFile.OpenRead() )
			{
				using ( System.IO.BinaryReader SceneReader = new System.IO.BinaryReader( SceneStream ) )
				{
					m_Scene.Load( SceneReader, TextureProvider );
				}
			}
#endif
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
			CamManip.InitializeCamera( new Vector3( 0.0f, 4.0f, 10.0f ), Vector3.Zero, Vector3.UnitY );

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

		#endregion
	}
}
