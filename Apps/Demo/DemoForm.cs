﻿using System;
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
	/// <summary>
	/// Simplest of all apps that displays a cube
	/// </summary>
	public partial class DemoForm : Form
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// Cube primitive
		protected Material<VS_P3C4T2>		m_CubeMaterial = null;
		protected Primitive<VS_P3C4T2,int>	m_Cube = null;
		protected Texture2D<PF_RGBA8>		m_CubeDiffuseTexture = null;

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
			// Build cube components

			// Create the cube material
			try
			{
				m_CubeMaterial = ToDispose( new Material<VS_P3C4T2>( m_Device, "CubeMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Simple/CubeShader.fx" ) ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Create the cube primitive
			VS_P3C4T2[]	Vertices = new VS_P3C4T2[]
			{
				new VS_P3C4T2() { Position=new Vector3( -1.0f, -1.0f, -1.0f ), Color=new Vector4( 0.0f, 0.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_P3C4T2() { Position=new Vector3( +1.0f, -1.0f, -1.0f ), Color=new Vector4( 1.0f, 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 0.0f ) },
				new VS_P3C4T2() { Position=new Vector3( +1.0f, +1.0f, -1.0f ), Color=new Vector4( 1.0f, 1.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ) },
				new VS_P3C4T2() { Position=new Vector3( -1.0f, +1.0f, -1.0f ), Color=new Vector4( 0.0f, 1.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f ) },
				new VS_P3C4T2() { Position=new Vector3( -1.0f, -1.0f, +1.0f ), Color=new Vector4( 0.0f, 0.0f, 1.0f, 1.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_P3C4T2() { Position=new Vector3( +1.0f, -1.0f, +1.0f ), Color=new Vector4( 1.0f, 0.0f, 1.0f, 1.0f ), UV=new Vector2( 1.0f, 0.0f ) },
				new VS_P3C4T2() { Position=new Vector3( +1.0f, +1.0f, +1.0f ), Color=new Vector4( 1.0f, 1.0f, 1.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ) },
				new VS_P3C4T2() { Position=new Vector3( -1.0f, +1.0f, +1.0f ), Color=new Vector4( 0.0f, 1.0f, 1.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f ) },
			};
			int[]	Indices = new[] { 0, 2, 1,
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
						}; 

			m_Cube = ToDispose( new Primitive<VS_P3C4T2,int>( m_Device, "Cube", PrimitiveTopology.TriangleList, Vertices, Indices, m_CubeMaterial ) );

			// Create the cube diffuse texture
			Nuaj.Image<PF_RGBA8>	DiffuseImage = ToDispose( new Nuaj.Image<PF_RGBA8>( m_Device, "Diffuse", Properties.Resources.TextureBisou, 0, 1.0f ) );

			m_CubeDiffuseTexture = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Diffuse Texture", DiffuseImage ) );
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
				float	fRadius = 2.5f;

				Vector3	Eye = new Vector3( fRadius * (float) (Math.Sin( fPhi ) * Math.Cos( fTheta )), fRadius * (float) Math.Sin( fTheta ), fRadius * (float) (Math.Cos( fPhi ) * Math.Cos( fTheta )) );

				Cam.LookAt( Eye, Vector3.Zero, Vector3.UnitY );

				// Set the diffuse texture
				VariableResource	vDiffuseTexture = m_CubeMaterial.GetVariableBySemantic( "TEX_DIFFUSE" ).AsResource;
									vDiffuseTexture.SetResource( m_CubeDiffuseTexture.TextureView );

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_Cube.Render();

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