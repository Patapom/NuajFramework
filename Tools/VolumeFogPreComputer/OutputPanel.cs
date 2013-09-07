using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace VolumeFogPreComputer
{
	public partial class OutputPanel : Panel
	{
		public Bitmap		m_Bitmap = null;

		public string		m_Title = "";

		protected Pen[]	MyPens = new Pen[]
		{
			new Pen( System.Drawing.Brushes.Black, 2 ),
			new Pen( System.Drawing.Brushes.Red, 2 ),
			new Pen( System.Drawing.Brushes.DarkGreen, 2 ),
			new Pen( System.Drawing.Brushes.Blue, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
		};

		public OutputPanel( IContainer container )
		{
			container.Add( this );

			InitializeComponent();
		}

		protected override void OnSizeChanged( EventArgs e )
		{
			base.OnSizeChanged( e );

			if ( m_Bitmap != null )
				m_Bitmap.Dispose();

			m_Bitmap = new Bitmap( Width, Height, PixelFormat.Format32bppArgb );
			UpdateBitmap();
		}

		public void		UpdateBitmap()
		{
			if ( m_Bitmap == null || IsDisposed )
				return;

			using ( Graphics G = Graphics.FromImage( m_Bitmap ) )
			{
				G.FillRectangle( Brushes.White, 0, 0, Width, Height );
				G.DrawLine( Pens.Black, 0, Height/2, Width, Height/2 );
				G.DrawLine( Pens.Black, Width/2, 0, Width/2, Height );

				for ( int i=1; i < 4; i++ )
				{
					G.DrawLine( Pens.Black, Width/2-8, Height/2+Height*i/8, Width/2+8, Height/2+Height*i/8 );
					G.DrawLine( Pens.Black, Width/2-8, Height/2-Height*i/8, Width/2+8, Height/2-Height*i/8 );
					G.DrawLine( Pens.Black, Width/2-Width*i/8, Height/2+8, Width/2-Width*i/8, Height/2-8 );
					G.DrawLine( Pens.Black, Width/2+Width*i/8, Height/2+8, Width/2+Width*i/8, Height/2-8 );
				}

 				G.DrawString( m_Title + " - ", Font, Brushes.Black, 0, 0 );
			}
			Refresh();
		}

		protected override void OnPaintBackground( PaintEventArgs e )
		{
//			base.OnPaintBackground( e );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint( e );

			if ( m_Bitmap != null )
				e.Graphics.DrawImage( m_Bitmap, 0, 0 );
		}
	}
}
