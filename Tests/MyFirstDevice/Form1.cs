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
using Device = SharpDX.Direct3D10.Device;
using Device1 = SharpDX.Direct3D10.Device1;

namespace MyFirstDevice
{
	public partial class Form1 : Form
	{
		#region FIELDS

		protected Device			m_Device = null;
		protected SwapChain			m_SwapChain = null;

		protected Texture2D			m_BackBuffer = null;
		protected RenderTargetView	m_RenderTarget = null;

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

			// Create render target
            m_BackBuffer = Texture2D.FromSwapChain<Texture2D>( m_SwapChain, 0 );
            m_RenderTarget = new RenderTargetView( m_Device, m_BackBuffer );
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			m_BackBuffer.Dispose();
			m_RenderTarget.Dispose();
			m_SwapChain.Dispose();
			m_Device.Dispose();

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

            SharpDX.Windows.MessagePump.Run( this, () =>
            {
                m_Device.ClearRenderTargetView( m_RenderTarget, Color.CornflowerBlue );

				// (...) do your amazingly beautiful stuff here

                m_SwapChain.Present( 0, PresentFlags.None );
            });
		}

		#endregion
	}
}
