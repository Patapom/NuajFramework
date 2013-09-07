//////////////////////////////////////////////////////////////////////////
// This examples demonstrates a simple reaction-diffusion simulation
// Draw some stuff on the screen using the mouse and use numpad to change
//	parameters.
//
// 7 increase f, 1 decreases f
// 8 increase k, 2 decreases k
//
//////////////////////////////////////////////////////////////////////////
//
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

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region CONSTANTS

		protected const	int		TEXTURE_SIZE = 512;
		protected const	float	F_STEP = 0.0001f;
		protected const	float	K_STEP = 0.0001f;

		#endregion

		#region FIELDS

		protected Nuaj.Device	m_Device = null;

		protected RenderTarget<PF_RGBA32F>[]	m_Textures = new RenderTarget<PF_RGBA32F>[2];
		protected Texture2D<PF_RGBA8>			m_EnvMap = null;
		protected Nuaj.Helpers.ScreenQuad		m_Quad = null;
		protected Material<VS_Pt4V3T2>			m_Material = null;


// 		// Damn funny that one !
// 		protected float			m_ParamF = 0.0163f;
// 		protected float			m_ParamK = 0.0452f;

// 		// Dangerous equilibrium
// 		protected float			m_ParamF = 0.0205f;
// 		protected float			m_ParamK = 0.0476f;

// 		// Nice dot patterns from interferences !!
// 		protected float			m_ParamF = 0.0322f;
// 		protected float			m_ParamK = 0.0555f;

// 		// Nice ... maze ?
// 		protected float			m_ParamF = 0.0322f;
// 		protected float			m_ParamK = 0.0560f;

		// Hiii !
		protected float			m_ParamF = 0.0267f;
		protected float			m_ParamK = 0.0544f;

// 		// Everlasting gloub
// 		protected float			m_ParamF = 0.0115f;
// 		protected float			m_ParamK = 0.0359f;

// 		// Rotating spirals (delicate !)
// 		protected float			m_ParamF = 0.0109f;
// 		protected float			m_ParamK = 0.0041f;
//  
		// From the video
// 		protected float			m_ParamF = 0.0011f;
// 		protected float			m_ParamK = 0.0352f;
// 		protected float			m_ParamF = 0.0019f;
// 		protected float			m_ParamK = 0.0144f;
// 		protected float			m_ParamF = 0.0003f;
// 		protected float			m_ParamK = 0.0363f;
// 		protected float			m_ParamF = 0.0105f;
// 		protected float			m_ParamK = 0.0334f;
// 		protected float			m_ParamF = 0.0020f;
// 		protected float			m_ParamK = 0.0144f;
// 		protected float			m_ParamF = 0.0005f;
// 		protected float			m_ParamK = 0.0537f;

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
			// Build the swapping textures
			m_Textures[0] = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "RD0", TEXTURE_SIZE, TEXTURE_SIZE, 1 ) );
			m_Textures[1] = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "RD1", TEXTURE_SIZE, TEXTURE_SIZE, 1 ) );

			m_Device.ClearRenderTarget( m_Textures[0], new Color4( 0.0f, 0.0f, 0.0f, 0.0f) );
			m_Device.ClearRenderTarget( m_Textures[1], new Color4( 0.0f, 0.0f, 0.0f, 0.0f) );

			//////////////////////////////////////////////////////////////////////////
			// Load the env map
//			using ( Bitmap B = Bitmap.FromFile( "./Media/CubeMaps/F6-example_horizontalcross.png" ) as Bitmap )
//				using ( ImageCube<PF_RGBA8> EnvMapImage = new ImageCube<PF_RGBA8>( m_Device, "EnvMap", B, ImageCube<PF_RGBA8>.FORMATTED_IMAGE_TYPE.HORIZONTAL_CROSS, 0, 1.0f ) )
			using ( Bitmap B = Bitmap.FromFile( "./Media/CubeMaps/ReactionDiffusion.png" ) as Bitmap )
				using ( ImageCube<PF_RGBA8> EnvMapImage = new ImageCube<PF_RGBA8>( m_Device, "EnvMap", B, ImageCube<PF_RGBA8>.FORMATTED_IMAGE_TYPE.HORIZONTAL_BAND, 0, 1.0f ) )
					m_EnvMap = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "EnvMap", EnvMapImage ) );

			//////////////////////////////////////////////////////////////////////////
			// Build the screen quad
			m_Quad = ToDispose( new Nuaj.Helpers.ScreenQuad( m_Device, "Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Finally, build the material
			m_Material = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "ReactionDiffusion Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/ReactionDiffusion/ReactionDiffusion.fx" ) ) );
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

			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Perform reaction-diffusion step ===============
				m_Device.SetRenderTarget( m_Textures[1] );
				m_Device.SetViewport( 0, 0, TEXTURE_SIZE, TEXTURE_SIZE, 0.0f, 1.0f );

				using ( m_Material.UseLock() )
				{
					m_Material.GetVariableByName( "SourceTexture" ).AsResource.SetResource( m_Textures[0] );
					m_Material.GetVariableByName( "EnvironmentTexture" ).AsResource.SetResource( m_EnvMap );
					m_Material.GetVariableByName( "F" ).AsScalar.Set( m_ParamF );
					m_Material.GetVariableByName( "K" ).AsScalar.Set( m_ParamK );
					m_Material.GetVariableByName( "DrawStrength" ).AsScalar.Set( m_MouseButtonsDownCount );
					m_Material.GetVariableByName( "DrawCenter" ).AsVector.Set( m_MousePosition );
					m_Material.GetVariableByName( "bShowEnvironment" ).AsScalar.Set( m_bShowEnvironment );

					m_Material.CurrentTechnique = m_Material.GetTechniqueByName( "ReactionDiffusion" );
					m_Material.Render( (a,b,c)=>{ m_Quad.Render(); } );
				}

				// =============== Display result ===============
				m_Device.SetDefaultRenderTarget();

				using ( m_Material.UseLock() )
				{
					m_Material.GetVariableByName( "SourceTexture" ).AsResource.SetResource( m_Textures[1] );

					m_Material.CurrentTechnique = m_Material.GetTechniqueByName( "Display" );
					m_Material.Render( (a,b,c)=>{ m_Quad.Render(); } );
				}

				// Swap textures
				RenderTarget<PF_RGBA32F>	Temp = m_Textures[0];
				m_Textures[0] = m_Textures[1];
				m_Textures[1] = Temp;

				// Show !
				m_Device.Present();

				Text = "Reaction-Diffusion  F=" + m_ParamF.ToString( "G5" ) + " K=" + m_ParamK.ToString( "G5" );
			});
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		protected MouseButtons	m_MouseButtonsDown = MouseButtons.None;
		protected int			m_MouseButtonsDownCount = 0;
		protected Vector2		m_MousePosition = Vector2.Zero;
		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );
			m_MouseButtonsDown |= e.Button;
			UpdateCount();
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );
			m_MouseButtonsDown &= ~e.Button;
			UpdateCount();
		}

		protected void	UpdateCount()
		{
			m_MouseButtonsDownCount = 0;
			m_MouseButtonsDownCount += (m_MouseButtonsDown & System.Windows.Forms.MouseButtons.Left) != 0 ? 1 : 0;
			m_MouseButtonsDownCount += (m_MouseButtonsDown & System.Windows.Forms.MouseButtons.Middle) != 0 ? 1 : 0;
			m_MouseButtonsDownCount += (m_MouseButtonsDown & System.Windows.Forms.MouseButtons.Right) != 0 ? 1 : 0;
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );
			m_MousePosition.X = (float) e.X / ClientSize.Width;
			m_MousePosition.Y = (float) e.Y / ClientSize.Height;
		}

		protected bool	m_bShowEnvironment = true;
		protected override void OnPreviewKeyDown( PreviewKeyDownEventArgs e )
		{
			base.OnPreviewKeyDown( e );

			if ( e.KeyCode == Keys.NumPad1 )
				m_ParamF -= F_STEP;
			else if ( e.KeyCode == Keys.NumPad7 )
				m_ParamF += F_STEP;
			else if ( e.KeyCode == Keys.NumPad2 )
				m_ParamK -= K_STEP;
			else if ( e.KeyCode == Keys.NumPad8 )
				m_ParamK += K_STEP;
			else if ( e.KeyCode == Keys.Return )
				m_bShowEnvironment = !m_bShowEnvironment;
			else if ( e.KeyCode == Keys.Space )
			{	// Clear
				m_Device.ClearRenderTarget( m_Textures[0], new Color4( 0.0f, 0.0f, 0.0f, 0.0f) );
				m_Device.ClearRenderTarget( m_Textures[1], new Color4( 0.0f, 0.0f, 0.0f, 0.0f) );
			}
		}

		#endregion
	}
}
