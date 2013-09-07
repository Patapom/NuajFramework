//////////////////////////////////////////////////////////////////////////
// This examples demonstrates the use of the Geometry Shader
//
// It generates 10,000 vertices containing only position and color
//	then the GS generates 4 vertices expanded using camera plane vectors.
//
// These 4 vertices are then used to build 2 triangles in a strip which
//	are finally rendered by the pixel shader.
//
//
// It also demonstrates the use of a StreamOutput as a temporary buffer
//  that stores the results of a first pass containing only a VS and a GS
//
// Then it's bound as a vertex source for the second pass containing a
//	pass-through VS and a PS
//
// Uncomment the define below to enable the stream output demo
//
//////////////////////////////////////////////////////////////////////////

#define USE_STREAM_OUTPUT	// Define this to render in 2 stages, first filling a stream output then drawing from it


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
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// Particles primitive
		protected Material<VS_P3C4>			m_ParticlesMaterial = null;
		protected Primitive<VS_P3C4,int>	m_Particles = null;

		// An option stream output where particles will be streamed to before being rendered
		protected StreamOutputBuffer<VS_Pt4C4T2>	m_StreamedOutParticles = null;

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
			// Build the particles components

			// Create the particles material
			try
			{
				m_ParticlesMaterial = ToDispose( new Material<VS_P3C4>( m_Device, "ParticlesMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Simple/GSTest.fx" ) ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Create the cube primitive
			VS_P3C4[]	Vertices = new VS_P3C4[10000];

			Random	RNG = new Random();
			for ( int ParticleIndex=0; ParticleIndex < 10000; ParticleIndex++ )
			{
				float	fRadius = (float) (Math.Sqrt( RNG.NextDouble() ) );
				float	fPhi = (float) (RNG.NextDouble() * 2.0 * Math.PI);
				float	fTheta = 2.0f * (float) Math.Acos( Math.Sqrt( RNG.NextDouble() ) );

				Vertices[ParticleIndex].Position.X = fRadius * (float) (Math.Cos( fPhi ) * Math.Sin( fTheta ));
				Vertices[ParticleIndex].Position.Y = fRadius * (float) Math.Cos( fTheta );
				Vertices[ParticleIndex].Position.Z = fRadius * (float) (Math.Sin( fPhi ) * Math.Sin( fTheta ));

				float	fGreenRandom = (float) RNG.NextDouble();
				float	fBlueRandom = (float) RNG.NextDouble();
				float	fColor = (float) Math.Sqrt( 1.0f - fRadius );
				Vertices[ParticleIndex].Color.X = 2 * fColor;
				Vertices[ParticleIndex].Color.Y = fColor * (0.25f + 0.5f * fGreenRandom);
				Vertices[ParticleIndex].Color.Z = fColor * (0.1f + 0.4f * fGreenRandom);
				Vertices[ParticleIndex].Color.W = 1.0f;
			}

			m_Particles = ToDispose( new Primitive<VS_P3C4,int>( m_Device, "Particles", PrimitiveTopology.TriangleStrip, Vertices, m_ParticlesMaterial ) );

#if USE_STREAM_OUTPUT
			// Create the stream output
			m_StreamedOutParticles = ToDispose( new StreamOutputBuffer<VS_Pt4C4T2>( m_Device, "StreamOut", 4*Vertices.Length ) );
#endif
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
			// Setup states
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.ADDITIVE );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );

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
				double	fPhi = 0.2f * 2.0f * Math.PI * fTotalTime;	// 1 turn in 5 seconds
				double	fTheta = 0.25f * Math.PI * Math.Sin( 0.4f * 2.0f * Math.PI * fTotalTime );	// 1 oscillation in 2.5 seconds
				float	fRadius = 1.5f;

				Vector3	Eye = new Vector3( fRadius * (float) (Math.Sin( fPhi ) * Math.Cos( fTheta )), fRadius * (float) Math.Sin( fTheta ), fRadius * (float) (Math.Cos( fPhi ) * Math.Cos( fTheta )) );

				Cam.LookAt( Eye, Vector3.Zero, Vector3.UnitY );

				// Clear
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );

#if !USE_STREAM_OUTPUT
				m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.PointList );
				m_ParticlesMaterial.CurrentTechnique = m_ParticlesMaterial.GetTechniqueByName( "RenderDirect" );

				// Draw
				m_Particles.Render();
#else
				// Render to the stream output first...
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
				m_ParticlesMaterial.CurrentTechnique = m_ParticlesMaterial.GetTechniqueByName( "RenderToStreamOutput" );
				m_StreamedOutParticles.UseAsOutput();
			m_StreamedOutParticles.BeginQuery();	// DEBUG
				m_Particles.Render();
			m_StreamedOutParticles.EndQuery( true );// DEBUG
				m_StreamedOutParticles.UnUse();	// VERY IMPORTANT LINE HERE !

				// Then display the stream output content 
				m_Device.InputAssembler.PrimitiveTopology =PrimitiveTopology.TriangleList;
				m_ParticlesMaterial.CurrentTechnique = m_ParticlesMaterial.GetTechniqueByName( "RenderFromStreamOutput" );
				m_StreamedOutParticles.UseAsInput( m_ParticlesMaterial.CurrentTechnique );
				m_ParticlesMaterial.Render( ( _Material, _Pass, _PassIndex ) => { m_StreamedOutParticles.Draw(); } );
#endif

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
