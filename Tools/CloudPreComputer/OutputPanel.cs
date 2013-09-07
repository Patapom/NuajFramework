using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace CloudPreComputer
{
	public partial class OutputPanel : Panel
	{
		public Bitmap		m_Bitmap = null;

		public string		m_Title = "";
		public float		m_ScaleY = 1.0f;

		public double		m_ViewModeThickness = 200.0;
		public double		m_ViewModeHeight = 100.0;

		public Form1.SHVector[,,]	m_ComputedTable = null;
		public bool[]				m_bComputedDepthSlices = new bool[Form1.SLAB_TEXTURE_DEPTH];

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

				// Draw the curves
				if ( m_ComputedTable != null )
				{
// 					double	T = 100.0;	// Total thickness is 100m
// 					double	H = T/2;	// View at half thickness (i.e. center of the cylinder)

					double	T = m_ViewModeThickness;
					double	H = m_ViewModeHeight;

					int		X = (int) (Form1.SLAB_TEXTURE_SIZE * (1.0 - Math.Log( Form1.SLAB_MAX_THICKNESS / T ) / Math.Log( Form1.SLAB_MAX_THICKNESS / Form1.SLAB_MIN_THICKNESS )));
					X = Math.Max( 0, Math.Min( Form1.SLAB_TEXTURE_SIZE-1, X ) );
					int		Y = (int) (Form1.SLAB_TEXTURE_SIZE * H / T);
					Y = Math.Max( 0, Math.Min( Form1.SLAB_TEXTURE_SIZE-1, Y ) );

					for ( int l=Form1.SH_SQORDER-1; l >= 0; l-- )
					{
						float	fPreviousPosX = -1000.0f;
 						float	fPreviousPosY = 0.0f;
						int		MaxComputedSliceIndex = -1;
						for ( int Z=0; Z < Form1.SLAB_TEXTURE_DEPTH; Z++ )
							if ( m_bComputedDepthSlices[Z] )
							{
								float	fCurrentPosX = Width * Z / (Form1.SLAB_TEXTURE_DEPTH-1);
								float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) m_ComputedTable[X,Y,Z].V[l]));	// Should map to top of the screen if 1 and bottom if -1
								G.DrawLine( MyPens[l], fPreviousPosX, fPreviousPosY, fCurrentPosX, fCurrentPosY );

								fPreviousPosX = fCurrentPosX;
								fPreviousPosY = fCurrentPosY;
								MaxComputedSliceIndex = Math.Max( MaxComputedSliceIndex, Z );
							}

						if ( MaxComputedSliceIndex >= 0 )
							G.DrawString( l.ToString() + " (" + MaxComputedSliceIndex + ")", Font, Brushes.Black, Width-30, Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) m_ComputedTable[X,Y,MaxComputedSliceIndex].V[l])) - 13 );
					}
				}

 				G.DrawString( m_Title + " - Scale = " + m_ScaleY.ToString( "G4" ) + " - T=" + m_ViewModeThickness + "m H=" + m_ViewModeHeight + "m", Font, Brushes.Black, 0, 0 );
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
