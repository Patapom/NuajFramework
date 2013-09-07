using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

using SharpDX;

namespace TreeGloumibule
{
	public partial class CurvePanel : Panel
	{
		public Bitmap			m_Bitmap = null;

		public string			m_Title = "";
		public float			m_ScaleY = 1.0f;

		public List<Vector2>[]	m_Curves = null;
		public Vector2[]		m_CurveMinimums = null;
		public Vector2[]		m_CurveMaximums = null;
		public Vector2			m_CurvesGlobalMinimum = Vector2.Zero;
		public Vector2			m_CurvesGlobalMaximum = Vector2.Zero;
		protected Brush			m_BackgroundBrush = null;
		protected Pen[]			m_CurvePens = null;
		protected int[]			m_CurvesRelativity = null;

		public CurvePanel()
		{
			InitializeComponent();
			m_BackgroundBrush = new SolidBrush( BackColor );
//			OnSizeChanged( EventArgs.Empty );
		}

		protected override void OnSizeChanged( EventArgs e )
		{
			base.OnSizeChanged( e );

			if ( m_Bitmap != null )
				m_Bitmap.Dispose();

			m_Bitmap = new Bitmap( Width, Height, PixelFormat.Format32bppArgb );
			UpdateBitmap();
		}

		protected override void OnBackColorChanged( EventArgs e )
		{
			base.OnBackColorChanged( e );

			m_BackgroundBrush.Dispose();
			m_BackgroundBrush = new SolidBrush( BackColor );

			UpdateBitmap();
		}

		public void		InitCurves( int _CurvesCount, Pen[] _CurvePens, int[] _CurvesRelativity )
		{
			m_CurvePens = _CurvePens;
			m_CurvesRelativity = _CurvesRelativity;
			m_Curves = new List<Vector2>[_CurvesCount];
			for ( int CurveIndex=0; CurveIndex < _CurvesCount; CurveIndex++ )
				m_Curves[CurveIndex] = new List<Vector2>();
			m_CurveMinimums = new Vector2[_CurvesCount];
			m_CurveMaximums = new Vector2[_CurvesCount];
			ClearCurves();
		}

		public void		ClearCurves()
		{
			for ( int CurveIndex=0; CurveIndex < m_Curves.Length; CurveIndex++ )
			{
				m_Curves[CurveIndex].Clear();
				m_CurveMinimums[CurveIndex] = new Vector2( 0.0f, 0.0f );
				m_CurveMaximums[CurveIndex] = new Vector2( -float.MaxValue, -float.MaxValue );
			}

			m_CurvesGlobalMinimum = new Vector2( 0.0f, 0.0f );
			m_CurvesGlobalMaximum = new Vector2( -float.MaxValue, -float.MaxValue );
		}

		public void		AddCurvePoints( Vector2[] _Points )
		{
			for ( int CurveIndex=0; CurveIndex < m_Curves.Length; CurveIndex++ )
			{
				m_Curves[CurveIndex].Add( _Points[CurveIndex] );

				// Update local maximums
				m_CurveMinimums[CurveIndex].X = Math.Min( m_CurveMinimums[CurveIndex].X, _Points[CurveIndex].X );
				m_CurveMinimums[CurveIndex].Y = Math.Min( m_CurveMinimums[CurveIndex].Y, _Points[CurveIndex].Y );
				m_CurveMaximums[CurveIndex].X = Math.Max( m_CurveMaximums[CurveIndex].X, _Points[CurveIndex].X );
				m_CurveMaximums[CurveIndex].Y = Math.Max( m_CurveMaximums[CurveIndex].Y, _Points[CurveIndex].Y );

				// Update global maximums
				m_CurvesGlobalMinimum.X = Math.Min( m_CurvesGlobalMinimum.X, m_CurveMinimums[CurveIndex].X );
				m_CurvesGlobalMinimum.Y = Math.Min( m_CurvesGlobalMinimum.Y, m_CurveMinimums[CurveIndex].Y );
				m_CurvesGlobalMaximum.X = Math.Max( m_CurvesGlobalMaximum.X, m_CurveMaximums[CurveIndex].X );
				m_CurvesGlobalMaximum.Y = Math.Max( m_CurvesGlobalMaximum.Y, m_CurveMaximums[CurveIndex].Y );
			}
		}

		public void		UpdateBitmap()
		{
			if ( m_Bitmap == null || IsDisposed )
				return;

			using ( Graphics G = Graphics.FromImage( m_Bitmap ) )
			{
				G.FillRectangle( m_BackgroundBrush, 0, 0, Width, Height );

				if ( m_Curves != null )
					for ( int CurveIndex=0; CurveIndex < m_Curves.Length; CurveIndex++ )
					{
						List<Vector2>	Curve = m_Curves[CurveIndex];
						if ( Curve.Count < 2 )
							continue;

						float	IDx = 1.0f / (m_CurveMaximums[m_CurvesRelativity[CurveIndex]].X - m_CurveMinimums[m_CurvesRelativity[CurveIndex]].X);
						float	IDy = 1.0f / (m_CurveMaximums[m_CurvesRelativity[CurveIndex]].Y - m_CurveMinimums[m_CurvesRelativity[CurveIndex]].Y);
						float	X = (Curve[0].X - m_CurveMinimums[m_CurvesRelativity[CurveIndex]].X) * IDx;
						float	Y = 0.1f + 0.8f * (Curve[0].Y - m_CurveMinimums[m_CurvesRelativity[CurveIndex]].Y) * IDy;
						for ( int PointIndex=1; PointIndex < Curve.Count; PointIndex++ )
						{
							float	Px = X, Py = Y;
							X = (Curve[PointIndex].X - m_CurveMinimums[m_CurvesRelativity[CurveIndex]].X) * IDx;
							Y = 0.1f + 0.8f * (Curve[PointIndex].Y - m_CurveMinimums[m_CurvesRelativity[CurveIndex]].Y) * IDy;
							G.DrawLine( m_CurvePens[CurveIndex], m_Bitmap.Width * Px, m_Bitmap.Height * (1.0f - Py), m_Bitmap.Width * X, m_Bitmap.Height * (1.0f - Y) );
						}
					}

 				G.DrawString( m_Title, Font, Brushes.Black, 0, 0 );
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
