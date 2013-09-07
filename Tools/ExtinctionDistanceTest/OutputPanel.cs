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
	public partial class OutputPanel : Panel
	{
		protected const int		STEPS_COUNT = 101;

		public Bitmap		m_Bitmap = null;
		public Form1		m_Owner = null;

		public float		m_CloudExtinction = 1.0f;
		public float		m_OpacityFactor = 1.0f;
		public float		m_StepSize = 0.01f;

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

		public OutputPanel()
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

		Vector2		m_Min, m_Max, m_Scale;
		Vector2[]	m_Values = new Vector2[STEPS_COUNT];
		public void		UpdateBitmap()
		{
			if ( m_Bitmap == null || IsDisposed )
				return;

			float	SigmaExtinction = 4.0f * (float) Math.PI * m_CloudExtinction;
			SigmaExtinction *= m_OpacityFactor;

			// Build the array of values
			m_Min = new Vector2( 0.0f, 0.0f );
//			m_Max = new Vector2( -float.MaxValue, 1.0f );
			m_Max = new Vector2( STEPS_COUNT * m_StepSize, 1.0f );

			float	Z = 0.0f;
			float	Extinction = 1.0f;
			m_Values[0] = new Vector2( Z, Extinction );
			for ( int i=1; i < STEPS_COUNT; i++ )
			{
				float	StepExtinction = (float) Math.Exp( -SigmaExtinction * m_StepSize );
				Extinction *= StepExtinction;
				Z += Extinction * m_StepSize;
				Vector2	V = new Vector2( Z, Extinction );
				m_Values[i] = V;

				m_Min = Vector2.Min( m_Min, V );
				m_Max = Vector2.Max( m_Max, V );
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
				DrawLine( G, Vector2.Zero, new Vector2( 0.0f, 1.0f ), 0, false );
				DrawLine( G, Vector2.Zero, new Vector2( 1.0f, 0.0f ), 0, false );

				// Draw points for each step
				for ( int i=0; i < STEPS_COUNT; i++ )
					DrawPoint( G, Brushes.Blue, m_Values[i], i % 10 == 0 ? i.ToString() : "" );

				// Draw lines
				for ( int i=0; i < STEPS_COUNT-1; i++ )
					DrawLine( G, m_Values[i], m_Values[i+1], 2, false );

				// Draw last value
				Vector2	EndPoint = new Vector2( m_Values[STEPS_COUNT-1].X, 0.0f );
				DrawLine( G, m_Values[STEPS_COUNT-1], EndPoint, 3, true );
				PointF	EndPointTransformed = Transform( EndPoint );
				EndPointTransformed.Y = Height - 20;
				G.DrawString( m_Values[STEPS_COUNT-1].X.ToString( "G5" ), Font, Brushes.Black, EndPointTransformed );

				EndPoint = new Vector2( 0.0f, m_Values[STEPS_COUNT-1].Y );
				DrawLine( G, m_Values[STEPS_COUNT-1], EndPoint, 3, true );
				EndPointTransformed = Transform( EndPoint );
				EndPointTransformed.X = 0;
				G.DrawString( m_Values[STEPS_COUNT-1].Y.ToString( "G5" ), Font, Brushes.Black, EndPointTransformed );
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
