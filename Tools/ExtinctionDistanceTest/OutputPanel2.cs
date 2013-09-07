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

namespace ExtinctionDistanceTest
{
	public partial class OutputPanel2 : Panel
	{
		protected const int		STEPS_COUNT = 200;

		public Bitmap		m_Bitmap = null;
		public Form1		m_Owner = null;

		public float		m_HDRMaxIntensity = 10.0f;
		public float		m_HDRWhitePoint = 10.0f;
		public float		m_LDRWhitePoint = 1.0f;
		public float		m_MaxX = 10.0f;
		public float		m_MaxY = 1.0f;

		protected Pen[]	MyPens = new Pen[]
		{
			new Pen( System.Drawing.Brushes.Black, 1 ),
			new Pen( System.Drawing.Brushes.Black, 2 ),
			new Pen( System.Drawing.Brushes.Red, 1 ),
			new Pen( System.Drawing.Brushes.DarkGreen, 1 ),
			new Pen( System.Drawing.Brushes.Blue, 4 ),
			new Pen( System.Drawing.Brushes.Gold, 1 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
		};

		public OutputPanel2()
		{
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


// 		public float A = 0.15f;	// A = Shoulder Strength
// 		public float B = 0.50f;	// B = Linear Strength
// 		public float C = 0.10f;	// C = Linear Angle
// 		public float D = 0.20f;	// D = Toe Strength
// 		public float E = 0.02f;	// E = Toe Numerator
// 		public float F = 0.30f;	// F = Toe Denominator
								// (Note: E/F = Toe Angle)

		public float A = 0.22f;		// A = Shoulder Strength
		public float B = 0.30f;		// B = Linear Strength
		public float C = 0.025f;	// C = Linear Angle
		public float D = 0.5f;		// D = Toe Strength
		public float E = 0.02f;		// E = Toe Numerator
		public float F = 0.30f;		// F = Toe Denominator

		float FilmicOperator( float _In )
		{
		   return ((_In*(A*_In+C*B) + D*E) / (_In*(A*_In+B) + D*F)) - E/F;
		}

		float	ToneMap( float _HDRLuminance )
		{
			return m_LDRWhitePoint * FilmicOperator( _HDRLuminance ) / Math.Max( 1e-3f, FilmicOperator( m_HDRWhitePoint ) );
//			return m_LDRWhitePoint * FilmicOperator( _HDRLuminance / Math.Max( 1e-3f, m_HDRWhitePoint * m_HDRMaxIntensity ) );
		}

		Vector2		m_Min, m_Max, m_Scale;
		public void		UpdateBitmap()
		{
			if ( m_Bitmap == null || IsDisposed )
				return;

			m_Min = new Vector2( 0, 0 );
			m_Max = new Vector2( m_MaxX, m_MaxY );

			D = Math.Max( 1e-3f, D );

			Vector2[]	Values = new Vector2[STEPS_COUNT+1];
			for ( int i=0; i <= STEPS_COUNT; i++ )
			{
				float	HDRLuminance = m_HDRMaxIntensity * i / STEPS_COUNT;
				float	LDRLuminance = ToneMap( HDRLuminance );

				Values[i] = new Vector2( HDRLuminance, LDRLuminance );
//				m_Max.Y = Math.Max( m_Max.Y, Values[i].Y );
			}

			// Draw the graph
			Vector2	Center = 0.5f * (m_Min + m_Max);

			Vector2	Dimensions = m_Max - m_Min;
			Dimensions = Vector2.Max( Dimensions, 0.001f * Vector2.One );
			Dimensions *= 1.2f;

			m_Min = Center - 0.5f * Dimensions;
			m_Max = Center + 0.5f * Dimensions;
			m_Scale = new Vector2( 1.0f / Dimensions.X, 1.0f / Dimensions.Y );

			using ( Graphics G = Graphics.FromImage( m_Bitmap ) )
			{
				G.FillRectangle( Brushes.White, 0, 0, Width, Height );

				// Draw the axes
				DrawLine( G, Vector2.Zero, new Vector2( 0.0f, m_Max.Y ), 0, false );
				DrawLine( G, Vector2.Zero, new Vector2( m_Max.X, 0.0f ), 0, false );

				DrawLine( G, new Vector2( 0, 1.0f ), new Vector2( m_Max.X, 1.0f ), 0, true );	// LDR=1

				// Draw points for each step
// 				for ( int i=0; i < STEPS_COUNT; i++ )
// 					DrawPoint( G, Brushes.Blue, Values[i], i % 10 == 0 ? i.ToString() : "" );

				// Draw lines
				for ( int i=0; i < STEPS_COUNT; i++ )
					DrawLine( G, Values[i], Values[i+1], 2, false );

				// Draw last value
				Vector2	EndPoint = new Vector2( Values[STEPS_COUNT-1].X, 0.0f );
				DrawLine( G, Values[STEPS_COUNT-1], EndPoint, 3, true );
				PointF	EndPointTransformed = Transform( EndPoint );
				EndPointTransformed.Y = Height - 20;
				G.DrawString( Values[STEPS_COUNT-1].X.ToString( "G5" ), Font, Brushes.Black, EndPointTransformed );

				EndPoint = new Vector2( 0.0f, Values[STEPS_COUNT-1].Y );
				DrawLine( G, Values[STEPS_COUNT-1], EndPoint, 3, true );
				EndPointTransformed = Transform( EndPoint );
				EndPointTransformed.X = 0;
				G.DrawString( Values[STEPS_COUNT-1].Y.ToString( "G5" ), Font, Brushes.Black, EndPointTransformed );

				// Draw white point
				DrawLine( G, new Vector2( m_HDRWhitePoint, 0.0f ), new Vector2( m_HDRWhitePoint, ToneMap( m_HDRWhitePoint ) ), 4, true );

			}
			Refresh();
		}

		protected void DrawPoint( Graphics _G, Brush _Brush, Vector2 _Position, string _Text )
		{
			int		PointSize = 4;
			PointF	P = Transform( _Position );
			_G.FillEllipse( Brushes.Black, P.X-PointSize, P.Y-PointSize, 2*PointSize, 2*PointSize );
			_G.DrawString( _Text, Font, _Brush, P.X + 2, P.Y + 2 );
		}
		protected void DrawLine( Graphics _G, Vector2 _P0, Vector2 _P1, int _PenIndex, bool _bDashed )
		{
			PointF	P0 = Transform( _P0 );
			PointF	P1 = Transform( _P1 );

			Pen	P = MyPens[_PenIndex];
			P.DashStyle = _bDashed ? System.Drawing.Drawing2D.DashStyle.Dash : System.Drawing.Drawing2D.DashStyle.Solid;

			_G.DrawLine( P, P0, P1 );
		}

		protected PointF	Transform( Vector2 _Position )
		{
			Vector2	NormalizedPosition = new Vector2( (_Position.X - m_Min.X) * m_Scale.X, 1.0f - (_Position.Y - m_Min.Y) * m_Scale.Y );
			return new PointF( NormalizedPosition.X * Width, NormalizedPosition.Y * Height );
		}
		public Vector2	TransformInverse( PointF _Position )
		{
			Vector2	NormalizedPosition = new Vector2( _Position.X / Width, _Position.Y / Height );
			return new Vector2( m_Min.X + NormalizedPosition.X * (m_Max.X - m_Min.X), m_Max.Y + NormalizedPosition.Y * (m_Min.Y - m_Max.Y) );
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
