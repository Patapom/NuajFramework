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
	public partial class OutputPanel3 : Panel
	{
		protected const int		STEPS_COUNT = 200;

		public Bitmap	m_Bitmap = null;

		protected int	m_ControlPointsCount = 5;
		public int		ControlPointsCount
		{
			get { return m_ControlPointsCount; }
			set
			{
				m_ControlPointsCount = value;
				m_Points = new Vector2[m_ControlPointsCount];
				for ( int i=0; i < m_ControlPointsCount; i++ )
					m_Points[i] = new Vector2( 0.0f, (float) i / (m_ControlPointsCount-1) );
				UpdateBitmap();
			}
		}

		public Vector2[]	m_Points = null;

		protected Pen[]	MyPens = new Pen[]
		{
			new Pen( System.Drawing.Brushes.Black, 1 ),
			new Pen( System.Drawing.Brushes.Black, 2 ),
			new Pen( System.Drawing.Brushes.Red, 2 ),
			new Pen( System.Drawing.Brushes.DarkGreen, 1 ),
			new Pen( System.Drawing.Brushes.Blue, 4 ),
			new Pen( System.Drawing.Brushes.Gold, 1 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
			new Pen( System.Drawing.Brushes.Gold, 2 ),
		};

		public OutputPanel3()
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
		public void		UpdateBitmap()
		{
			if ( m_Bitmap == null || m_Points == null || IsDisposed )
				return;

			m_Min = new Vector2( 0.0f, 0.0f );
			m_Max = new Vector2( 1.0f, 1.0f );

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

				DrawLine( G, new Vector2( 0.0f, 1.0f ), new Vector2( 1.0f, 1.0f ), 0, true );
				DrawLine( G, new Vector2( 1.0f, 0.0f ), new Vector2( 1.0f, 1.0f ), 0, true );

				// Draw polynomial
				Vector2	PreviousValue = new Vector2( ComputePolynomial( 0.0f ), 0.0f );
				for ( int i=1; i <= STEPS_COUNT; i++ )
				{
					float	y = (float) i / STEPS_COUNT;
					Vector2	CurrentValue = new Vector2( ComputePolynomial( y ), y );
					DrawLine( G, PreviousValue, CurrentValue, 2, false );

					PreviousValue = CurrentValue;
				}

				// Draw points
				for ( int i=0; i < m_ControlPointsCount; i++ )
					DrawPoint( G, Brushes.Blue, m_Points[i], "(" + m_Points[i].X.ToString( "G4" ) + ", " + m_Points[i].Y.ToString( "G4" ) + ")" );

			}
			Invalidate();
		}

		protected float	ComputePolynomial( float y )
		{
			float	Result = 0.0f;
			for ( int i=0; i < m_ControlPointsCount; i++ )
			{
				float	Coeff = 1.0f;
				for ( int j=0; j < m_ControlPointsCount; j++ )
					if ( j != i )
						Coeff *= (y - m_Points[j].Y) / (m_Points[i].Y - m_Points[j].Y);

				Result += Coeff * m_Points[i].X;
			}

			return Result;
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

		//////////////////////////////////////////////////////////////////////////
		// Manipulation
		protected int		m_ManipulatingPoint = -1;
		protected Vector2	m_ButtonDownPointPosition;
		protected Vector2	m_ButtonDownMousePosition;

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );
			if ( e.Button != MouseButtons.Left )
				return;

			// Attempt to retrieve the point under the mouse
			m_ManipulatingPoint = -1;
			for ( int i=0; i < m_ControlPointsCount; i++ )
			{
				PointF	PointClient = Transform( m_Points[i] );
				Vector2	DPos = new Vector2( PointClient.X - e.X, PointClient.Y - e.Y );
				if ( DPos.Length() < 4 )
				{	// Found a point !
					m_ManipulatingPoint = i;
					break;
				}
			}
			if ( m_ManipulatingPoint == -1 )
				return;

			// We can start manipulating !
			m_ButtonDownMousePosition = TransformInverse( e.Location );
			m_ButtonDownPointPosition = m_Points[m_ManipulatingPoint];
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );
			if ( m_ManipulatingPoint == -1 )
				return;	// Not manipulating...

			Vector2	MousePosition = TransformInverse( e.Location );
			Vector2	NewPosition = MousePosition + m_ButtonDownMousePosition - m_ButtonDownPointPosition;
			m_Points[m_ManipulatingPoint] = NewPosition;
			UpdateBitmap();
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );

			if ( e.Button == MouseButtons.Left )
				m_ManipulatingPoint = -1;	// Stop manipulation...
		}
	}
}
