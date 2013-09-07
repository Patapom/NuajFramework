using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D10.Device;
using Buffer = SharpDX.Direct3D10.Buffer;
using Device1 = SharpDX.Direct3D10.Device1;
using DriverType = SharpDX.Direct3D10.DriverType;

namespace MyFirstCube
{
	public partial class Form1 : Form
	{
		#region FIELDS

		protected Device			m_Device = null;
		protected SwapChain			m_SwapChain = null;

		protected Texture2D			m_BackBuffer = null;
		protected RenderTargetView	m_RenderTarget = null;

		// Cube data
		protected InputLayout		m_CubeVertexLayout = null;
		protected Buffer			m_CubeVertexBuffer = null;
		protected Buffer			m_CubeIndexBuffer = null;
		protected Effect			m_CubeEffect = null;
		protected EffectTechnique	m_CubeTechnique = null;
		protected EffectPass		m_CubePass0 = null;

		// Dispose stack
		protected Stack<IDisposable>	m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public Form1()
		{
			InitializeComponent();

			// Create m_Device
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

			Device1.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, Desc, out m_Device, out m_SwapChain );
			ToDispose( m_Device );
			ToDispose( m_SwapChain );

			// Create render target
			m_BackBuffer = ToDispose( Texture2D.FromSwapChain<Texture2D>( m_SwapChain, 0 ) );
			m_RenderTarget = ToDispose( new RenderTargetView( m_Device, m_BackBuffer ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the cube effect
			string	Errors = "";
			m_CubeEffect = ToDispose( new Effect(m_Device, ShaderBytecode.Compile(Properties.Resources.CubeShader, "", "fx_4_0", ShaderFlags.None, EffectFlags.None, null, null, out Errors ) ));
			m_CubeTechnique = m_CubeEffect.GetTechniqueByIndex( 0 );
			m_CubePass0 = m_CubeTechnique.GetPassByIndex( 0 );

			// Create the cube vertex layout
			m_CubeVertexLayout = ToDispose(
				new InputLayout( m_Device, 
					m_CubePass0.Description.Signature,	// We need the shader so that its input signature is validated against the array...
					new[] {	new InputElement( "POSITION", 0, Format.R32G32B32_Float, 0, 0 ),
							new InputElement( "COLOR", 0, Format.R32G32B32A32_Float, 12, 0 ) 
						  }
					 ) );

			// Create the vertex buffer
			using ( var	VertexStream = new DataStream( 8 * (3+4) * sizeof(float), true, true ) )
			{
				Vector3[]	Vertices = new Vector3[]
				{
					new Vector3( -1.0f, -1.0f, -1.0f ),
					new Vector3( +1.0f, -1.0f, -1.0f ),
					new Vector3( +1.0f, +1.0f, -1.0f ),
					new Vector3( -1.0f, +1.0f, -1.0f ),
					new Vector3( -1.0f, -1.0f, +1.0f ),
					new Vector3( +1.0f, -1.0f, +1.0f ),
					new Vector3( +1.0f, +1.0f, +1.0f ),
					new Vector3( -1.0f, +1.0f, +1.0f ),
				};
				for ( int VertexIndex=0; VertexIndex < 8; VertexIndex++ )
				{
					VertexStream.Write( Vertices[VertexIndex] );
					VertexStream.Write( new Vector4( 0.5f * Vertices[VertexIndex] + new Vector3( 0.5f ), 1.0f ) );
				}
				VertexStream.Position = 0;


				m_CubeVertexBuffer = ToDispose( new Buffer( m_Device, VertexStream,
					new BufferDescription()
					{
						BindFlags = BindFlags.VertexBuffer,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.None,
						SizeInBytes = (int) VertexStream.Length,
						Usage = ResourceUsage.Default
					} ) );
			}

			// Create the index buffer
			using ( var	IndexStream = new DataStream( 2*3 * 6 * sizeof(int), true, true ) )
			{
				IndexStream.WriteRange(
					new[] { 0, 2, 1,
							0, 3, 2,
							4, 5, 6,
							4, 6, 7,
							0, 4, 7,
							0, 7, 3,
							1, 6, 5,
							1, 2, 6,
							0, 1, 5,
							0, 5, 4, 
							3, 7, 6,
							3, 6, 2
						} ); 
				IndexStream.Position = 0;

				m_CubeIndexBuffer = ToDispose( new Buffer( m_Device, IndexStream,
					new BufferDescription()
					{
						BindFlags = BindFlags.IndexBuffer,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.None,
						SizeInBytes = (int) IndexStream.Length,
						Usage = ResourceUsage.Default
					} ) );
			}
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
            m_Device.OutputMerger.SetTargets( m_RenderTarget );
            m_Device.Rasterizer.SetViewports( new Viewport( 0, 0, ClientSize.Width, ClientSize.Height, 0.0f, 1.0f ) );

			EffectVariable	vLocal2Proj = m_CubeEffect.GetVariableBySemantic( "LOCAL2PROJ" );

			// Use clock-wise culling as we're using left-handed camera matrices !
			RasterizerStateDescription	StateDesc = new RasterizerStateDescription();
			StateDesc.CullMode = CullMode.Back;	// Cull back faces
			StateDesc.FillMode = FillMode.Solid;
			StateDesc.DepthBias = 0;
			StateDesc.DepthBiasClamp = 0.0f;
			StateDesc.IsAntialiasedLineEnabled = false;
			StateDesc.IsDepthClipEnabled = true;
			StateDesc.IsFrontCounterClockwise = true;
			StateDesc.IsMultisampleEnabled = false;
			StateDesc.IsScissorEnabled = false;
			StateDesc.SlopeScaledDepthBias = 0.0f;

			RasterizerState	MyState = ToDispose( new RasterizerState( m_Device, StateDesc ) );
			m_Device.Rasterizer.State = MyState;

			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

            SharpDX.Windows.MessagePump.Run( this, () =>
            {
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// Update camera matrix
				double	fPhi = 0.2f * 2.0f * Math.PI * fTotalTime;	// 1 turn in 5 seconds
				double	fTheta = 0.25f * Math.PI * Math.Sin( 0.3f * 2.0f * Math.PI * fTotalTime );	// 1 oscillation in 5 seconds
				float	fRadius = 2.5f;

 				Vector3	Eye = new Vector3( fRadius * (float) (Math.Sin( fPhi ) * Math.Cos( fTheta )), fRadius * (float) Math.Sin( fTheta ), fRadius * (float) (Math.Cos( fPhi ) * Math.Cos( fTheta )) );

				Vector3	At = -Eye;
						At.Normalize();
				Vector3	Right = Vector3.Cross( At, new Vector3( 0.0f, 1.0f, 0.0f ) );
						Right.Normalize();
				Vector3	Up = Vector3.Cross( Right, At );

				Matrix	Camera2World = new Matrix();
						Camera2World.Row1 = new Vector4( Right, 0.0f );
						Camera2World.Row2 = new Vector4( Up, 0.0f );
						Camera2World.Row3 = new Vector4( At, 0.0f );
						Camera2World.Row4 = new Vector4( Eye, 1.0f );


				Matrix	World2Camera = Camera2World;
						World2Camera.Invert();
				Matrix	Camera2Proj = Matrix.PerspectiveFovLH( 0.5f * (float) Math.PI, (float) ClientSize.Width / ClientSize.Height, 0.001f, 100.0f );

				Matrix	World2Proj = World2Camera * Camera2Proj;

				vLocal2Proj.AsMatrix().SetMatrix( World2Proj );


				// Clear
                m_Device.ClearRenderTargetView( m_RenderTarget, Color.CornflowerBlue );

				// Draw
                m_Device.InputAssembler.SetInputLayout( m_CubeVertexLayout );
                m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.TriangleList );
                m_Device.InputAssembler.SetVertexBuffers( 0, new VertexBufferBinding( m_CubeVertexBuffer, 28, 0 ) );
				m_Device.InputAssembler.SetIndexBuffer( m_CubeIndexBuffer, Format.R32_UInt, 0 );

                for ( int i = 0; i < m_CubeTechnique.Description.PassCount; ++i )
                {
                    m_CubePass0.Apply();
                    m_Device.DrawIndexed( 2*3 * 6, 0, 0 );
                }

				// Show !
                m_SwapChain.Present( 0, PresentFlags.None );
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
