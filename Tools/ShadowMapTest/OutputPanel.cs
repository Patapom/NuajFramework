﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

using SharpDX;

namespace ShadowMapTest
{
	public partial class OutputPanel : Panel
	{
		public Bitmap		m_Bitmap = null;
		public Form1		m_Owner = null;

		// Original projected frustum positions
		public SharpDX.Vector2		m_CameraPosition;
		public SharpDX.Vector2[]	m_FrustumBase = new SharpDX.Vector2[4];

		// Convex hull
		public SharpDX.Vector2[]	m_ConvexHull = new SharpDX.Vector2[4];

		// Enclosing quadrilateral
		public SharpDX.Vector2[]	m_Quadrilateral = new SharpDX.Vector2[4];

		public Vector2				m_P = Vector2.Zero;
		public Vector2				m_UV = Vector2.Zero;

		protected Pen[]	MyPens = new Pen[]
		{
			new Pen( System.Drawing.Brushes.Black, 1 ),
			new Pen( System.Drawing.Brushes.Black, 2 ),
			new Pen( System.Drawing.Brushes.Red, 1 ),
			new Pen( System.Drawing.Brushes.DarkGreen, 4 ),
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

		Vector2	m_Min, m_Max, m_Scale;
		public void		UpdateBitmap()
		{
			if ( m_Bitmap == null || IsDisposed )
				return;

			// Compute bounding box
			m_Min = +float.MaxValue * Vector2.One;
			m_Max = -float.MaxValue * Vector2.One;
			UpdateBBox( m_CameraPosition );
			for ( int i=0; i < 4; i++ )
			{
				UpdateBBox( m_FrustumBase[i] );
				UpdateBBox( m_Quadrilateral[i] );
			}
			for ( int i=0; i < m_ConvexHull.Length; i++ )
				UpdateBBox( m_ConvexHull[i] );

			Vector2	Dimensions = m_Max - m_Min;
			Vector2	Center = 0.5f * (m_Min + m_Max);
			Dimensions *= 1.3f;
			
			m_Min = Center - 0.5f * Dimensions;
			m_Max = Center + 0.5f * Dimensions;
			m_Scale = new Vector2( 1.0f / Dimensions.X, 1.0f / Dimensions.Y );

			// Draw
			using ( Graphics G = Graphics.FromImage( m_Bitmap ) )
			{
				G.FillRectangle( Brushes.White, 0, 0, Width, Height );

				// Draw UV lines
				DrawUVLines( G );
				DrawPoint( G, Brushes.Black, m_P, "P" );

				// Draw convex hull first
				for ( int i=0; i < m_ConvexHull.Length; i++ )
					DrawLine( G, m_ConvexHull[i], m_ConvexHull[(i+1)%m_ConvexHull.Length], 3, false );

				// Then bounding quadrilateral
				for ( int i=0; i < 4; i++ )
					DrawLine( G, m_Quadrilateral[i], m_Quadrilateral[(i+1)&3], 4, false );
				for ( int i=0; i < 4; i++ )
					DrawPoint( G, Brushes.Blue, m_Quadrilateral[i], i.ToString() );

				// Then frustum quadrilateral
				for ( int i=0; i < 4; i++ )
				{
					DrawLine( G, m_FrustumBase[i], m_FrustumBase[(i+1)&3], 1, false );
					DrawLine( G, m_CameraPosition, m_FrustumBase[i], 2, true );
				}
				for ( int i=0; i < 4; i++ )
					DrawPoint( G, Brushes.Black, m_FrustumBase[i], "" );//i.ToString() );
				DrawPoint( G, Brushes.Red, m_CameraPosition, "C" );
			}
			Refresh();
		}

		protected void	UpdateBBox( Vector2 _Position )
		{
			m_Min.X = Math.Min( m_Min.X, _Position.X );
			m_Min.Y = Math.Min( m_Min.Y, _Position.Y );
			m_Max.X = Math.Max( m_Max.X, _Position.X );
			m_Max.Y = Math.Max( m_Max.Y, _Position.Y );
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
			P.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

			_G.DrawLine( P, P0, P1 );
		}
		protected void	DrawUVLines( Graphics _G )
		{
			if ( m_Owner == null )
				return;
			// U-line
// 			Vector2	P0 = m_Quadrilateral[0] + (m_Quadrilateral[1] - m_Quadrilateral[0]) * m_UV.X;
// 			Vector2	P1 = m_Quadrilateral[3] + (m_Quadrilateral[2] - m_Quadrilateral[3]) * m_UV.X;
			Vector2	P0 = m_Owner.UV2XY( new Vector2( m_UV.X, 0.0f ) );
			Vector2	P1 = m_Owner.UV2XY( new Vector2( m_UV.X, 1.0f ) );
			DrawLine( _G, P0, P1, 5, false );

			// V-line
// 			P0 = m_Quadrilateral[0] + (m_Quadrilateral[3] - m_Quadrilateral[0]) * m_UV.Y;
// 			P1 = m_Quadrilateral[1] + (m_Quadrilateral[2] - m_Quadrilateral[1]) * m_UV.Y;
			P0 = m_Owner.UV2XY( new Vector2( 0.0f, m_UV.Y ) );
			P1 = m_Owner.UV2XY( new Vector2( 1.0f, m_UV.Y ) );
			DrawLine( _G, P0, P1, 5, false );

			_G.DrawString( "P=(" + m_P.X.ToString( "G6" ) + ", " + m_P.Y.ToString( "G6" ) + ")", Font, Brushes.Black, 0, Height-32 );
			_G.DrawString( "UV=(" + m_UV.X.ToString( "G6" ) + ", " + m_UV.Y.ToString( "G6" ) + ")", Font, Brushes.Black, 0, Height-16 );
		}

		protected PointF	Transform( Vector2 _Position )
		{
			Vector2	NormalizedPosition = new Vector2( (_Position.X - m_Min.X) * m_Scale.X, (_Position.Y - m_Min.Y) * m_Scale.Y );
			return new PointF( NormalizedPosition.X * Width, NormalizedPosition.Y * Height );
		}
		public Vector2	TransformInverse( PointF _Position )
		{
			Vector2	NormalizedPosition = new Vector2( _Position.X / Width, _Position.Y / Height );
			return new Vector2( m_Min.X + NormalizedPosition.X * (m_Max.X - m_Min.X), m_Min.Y + NormalizedPosition.Y * (m_Max.Y - m_Min.Y) );
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
