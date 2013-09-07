//////////////////////////////////////////////////////////////////////////
// This examples demonstrates the use of Alpha to Coverage and, if you have
//	a garphic cards that suppots the DirectX 10.1 extension, it also shows
//	how to customize the coverage mask per pixel to break the otherwise
//	uniform banding occurring with the default mask.
//
//////////////////////////////////////////////////////////////////////////
//
// Comment the define below to use alpha blending instead of alpha to coverage
// Doing this, you will see that alpha blending is unable to display the second leaf
//  behind the first one, which is drawn first unless we resort to sorting them back to front...
//
//#define USE_ALPHA_BLENDING

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

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region CONSTANTS

#if USE_ALPHA_BLENDING
		protected const int	MSAA_SAMPLES_COUNT = 1;		// No multisampling
#else
		protected const int	MSAA_SAMPLES_COUNT = 8;		// This will be clamped to the max amount anyway...
#endif

		#endregion

		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// Leaf primitive
		protected Material<VS_P3N3T2>		m_LeafMaterial = null;
		protected Primitive<VS_P3N3T2,int>	m_Leaf = null;
		protected Texture2D<PF_RGBA8>		m_LeafTexture = null;

		// Multisample render target
		protected RenderTarget<PF_RGBA8>	m_RenderTarget = null;

		protected bool						m_bCustomCoverage = true;

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
//					SampleDescription = new SampleDescription( 8, 0 ),	// NOTE: We can enable MSAA easily at this stage by specifying a MSAA quality here...
																		// The main problem is that we have not created a device yet and thus we can't ask it
																		//  to check if that MSAA samples count is supported, there is a risk the device won't be created at all...
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput,
				};

#if USE_ALPHA_BLENDING
				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, this ) );
#else
				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, this, MSAA_SAMPLES_COUNT ) );	// Use 8 samples for depth stencil
#endif
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}

			//////////////////////////////////////////////////////////////////////////
			// Load the leaf texture
			Bitmap	LeafDiffuse = Bitmap.FromFile( "./Media/Vegetation/Cannabis0.jpg" ) as Bitmap;
			Bitmap	LeafAlpha = Bitmap.FromFile( "./Media//Vegetation/Cannabis0_Alpha2.jpg" ) as Bitmap;
// 			Bitmap	LeafDiffuse = Bitmap.FromFile( "./Media/Vegetation/leaf2/leaf_diffuse.jpg" ) as Bitmap;
// 			Bitmap	LeafAlpha = Bitmap.FromFile( "./Media/Vegetation/leaf2/leaf_alpha.jpg" ) as Bitmap;

			using ( Nuaj.Image<PF_RGBA8> DiffuseImage = new Nuaj.Image<PF_RGBA8>( m_Device, "LeafDiffuse", LeafDiffuse, LeafAlpha, 0, 1.0f ) )
			{
				m_LeafTexture = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Diffuse Texture", DiffuseImage ) );
			}
			LeafDiffuse.Dispose();
			LeafAlpha.Dispose();


			//////////////////////////////////////////////////////////////////////////
			// Build the facing quad components

			// Create the leaf material
			try
			{
				m_LeafMaterial = ToDispose( new Material<VS_P3N3T2>( m_Device, "LeafMaterial", ShaderModel.SM4_1, new System.IO.FileInfo( "./FX/Simple/CustomAlpha2Coverage.fx" ) ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Create the leaf primitive
			VS_P3N3T2[]	Vertices = new VS_P3N3T2[]
			{
				new VS_P3N3T2() { Position=new Vector3( -1.0f, +1.0f, 0.0f ), Normal=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_P3N3T2() { Position=new Vector3( -1.0f, -1.0f, 0.0f ), Normal=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f ) },
				new VS_P3N3T2() { Position=new Vector3( +1.0f, +1.0f, 0.0f ), Normal=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 0.0f ) },
				new VS_P3N3T2() { Position=new Vector3( +1.0f, -1.0f, 0.0f ), Normal=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ) },
			};

			m_Leaf = ToDispose( new Primitive<VS_P3N3T2,int>( m_Device, "Leaf", PrimitiveTopology.TriangleStrip, Vertices, m_LeafMaterial ) );

			//////////////////////////////////////////////////////////////////////////
			// Create a multi-sampled render target
			m_RenderTarget = ToDispose( new RenderTarget<PF_RGBA8>( m_Device, "Multi-Sample Render Target", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 1, MSAA_SAMPLES_COUNT ) );

	
			//////////////////////////////////////////////////////////////////////////
			// Create the jitter constant buffer for the custom coverage
			float[]	JitterOffsets = new float[64];
			Random	RNG = new Random( 1 );
			for ( int i=0; i < 64; i++ )
				JitterOffsets[i] = 2.0f * (float) RNG.NextDouble() - 1.0f;	// Create samples in [-1,+1]
			for ( int i=0; i < 64; i++ )
			{
				int	j = RNG.Next( 64 );
				float	Temp = JitterOffsets[i];
				JitterOffsets[i] = JitterOffsets[j];
				JitterOffsets[j] = Temp;
			}

//			m_JitterBuffer = ToDispose( new ConstantBuffer<JitterBuffer>( m_Device, "Jitter Buffer", Buffer ) );
// 			EffectConstantBuffer	ConstantBuffer = m_LeafMaterial.GetVariableByName( "JitterBuffer" ).AsConstantBuffer();
// 			ConstantBuffer.SetConstantBuffer( m_JitterBuffer.Buffer );

			VariableScalar	vJitterOffsets = m_LeafMaterial.GetVariableByName( "JitterOffsets" ).AsScalar;
			vJitterOffsets.Set( JitterOffsets );
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
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
			Camera		Cam = ToDispose( new Camera( m_Device, "Default Camera" ) );
						Cam.CreatePerspectiveCamera( 0.5f * (float) Math.PI, (float) ClientSize.Width / ClientSize.Height, 0.01f, 100.0f );

			Cam.Activate();


			//////////////////////////////////////////////////////////////////////////
			// Setup alpha test blend state
#if USE_ALPHA_BLENDING
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.BLEND );
#else
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.ALPHA2COVERAGE );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Enable multisampling
			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING_MULTISAMPLING );

#if USE_ALPHA_BLENDING
			//////////////////////////////////////////////////////////////////////////
			// Disable depth test if alpha blending
			// If I don't disable it then the front leaf will hide the back one completely
			//  as the leaves are not sorted (on purpose, obviously).
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Get both render techniques
			EffectTechnique	Render0 = m_LeafMaterial.GetTechniqueByName( "Render" );
			EffectTechnique	Render1 = m_LeafMaterial.GetTechniqueByName( "RenderCustomCoverage" );


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

				// Update camera matrix
				double	fPhi = 0.15f * 2.0f * Math.PI *  Math.Sin( 0.1f * 2.0f * Math.PI * fTotalTime );
				double	fTheta = 0.15f * Math.PI * Math.Sin( 0.15f * 2.0f * Math.PI * fTotalTime );	// 1 oscillation in 2.5 seconds
				float	fRadius = 0.3f;

				Vector3	Eye = new Vector3( fRadius * (float) (Math.Sin( fPhi ) * Math.Cos( fTheta )), fRadius * (float) Math.Sin( fTheta ), fRadius * (float) (Math.Cos( fPhi ) * Math.Cos( fTheta )) );

				Cam.LookAt( Eye, new Vector3( 0.1f, 0.0f, 0.0f ), Vector3.UnitY );

				// Setup material texture
				VariableResource	vTexDiffuse = m_LeafMaterial.GetVariableBySemantic( "TEX_DIFFUSE" ).AsResource;
				vTexDiffuse.SetResource( m_LeafTexture.TextureView );

				VariableMatrix	vLocal2World = m_LeafMaterial.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix;

				VariableVector	vOneOverScreenSize = m_LeafMaterial.GetVariableByName( "OneOverScreenSize" ).AsVector;
								vOneOverScreenSize.Set( new Vector2( 1.0f / ClientSize.Width, 1.0f / ClientSize.Height ) );

				//////////////////////////////////////////////////////////////////////////
				// Setup appropriate material technique
				m_LeafMaterial.CurrentTechnique = m_bCustomCoverage ? Render1 : Render0;

				//////////////////////////////////////////////////////////////////////////
				// Setup the multi-sampled render target
				m_Device.SetRenderTarget( m_RenderTarget, m_Device.DefaultDepthStencil );

				// Clear
				m_Device.ClearRenderTarget( m_RenderTarget, (Color4) Color.Gold );// Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw 1 instance in (0,0,0)
				Matrix	Local2World = Matrix.Identity;
				Local2World.M43 = 0.0f;
				vLocal2World.SetMatrix( Local2World );
				m_Leaf.Render();

				// Draw another instance behind it in (0,0,-1)
				//
				// If alpha blending is active and depth test is disabled, it will display this instance in front
				// If alpha blending is active and depth test is enabled, this leaf will be hidden by the previous one
				// Whatever the choice you lose, this is why alpha blending objects need back to front sorting...
				//
				// If alpha to coverage is active, it will correctly display this instance in the back as it will filter through non written semi-opaque samples
				//
				Local2World.M43 = -1.0f;
				vLocal2World.SetMatrix( Local2World );
				m_Leaf.Render();

				// Ask to resolve the MSAA resource (but we could write a shader to read the multiple samples ourselves)
				m_Device.DirectXDevice.ResolveSubresource(	m_RenderTarget.Texture, 0,
															m_Device.DefaultRenderTarget.Texture, 0,
															m_Device.DefaultRenderTarget.Format );

				//////////////////////////////////////////////////////////////////////////
				// Setup default render target
				m_Device.SetDefaultRenderTarget();

				// Show !
				m_Device.Present();

				// Draw some text
//				using ( Graphics G = Graphics.FromHwnd( Handle ) )
//					G.DrawString( "Press space to toggle custom coverage.", Font, Brushes.Black, 5.0f, 12.0f );
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

		#region EVENTS

		private void DemoForm_KeyUp( object sender, KeyEventArgs e )
		{
			if ( e.KeyData == Keys.Space )
				m_bCustomCoverage = !m_bCustomCoverage;
		}

		#endregion
	}
}
