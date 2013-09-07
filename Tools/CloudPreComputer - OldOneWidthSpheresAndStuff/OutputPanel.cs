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
		public bool			m_bDrawLeastSquareFit = true;
		public double		m_RadiusStart = 10.0;
		public double		m_RadiusEnd = 500.0;
		public double[]		m_RadiusLeastSquareFitLimits = new double[Form1OLD.LSF_COUNT];
		public double[,]	m_Curves = new double[9,Form1OLD.MEASURES_COUNT+1];
		public double[,,]	m_LeastSquareFit = new double[Form1OLD.LSF_COUNT,9,4];
		public double[][]	m_LeastSquareFitOrder6 = new double[9][];
		public float		m_ScaleY = 4.0f;

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


		public double		m_MaximumX = 0.0;

// 		public OutputPanel()
// 		{
// 			InitializeComponent();
// 		}

		public OutputPanel( IContainer container )
		{
			container.Add( this );

			InitializeComponent();

			// Build the pens for curves comparison
			int RadiusStepsCount = 32;
			for ( int RadiusIndex=0; RadiusIndex <= RadiusStepsCount; RadiusIndex++ )
			{
				int		RadiusPow = RadiusIndex - RadiusStepsCount/2;
				float	fRadiusFactor = (float) Math.Pow( 2.0, 0.125f * RadiusPow );

				m_ComparisonCurvesPens[RadiusIndex] = new Pen( Color.FromArgb( (int) (255 * (1 - Math.Abs( RadiusPow / 32.0f))), fRadiusFactor > 1.0f ? 0 : 127, fRadiusFactor > 1.0f ? 127 : 0 ) );
			}
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

				float	MaxX = (float) (Width * (m_MaximumX-m_RadiusStart) / (m_RadiusEnd - m_RadiusStart));
				G.DrawLine( Pens.Red, MaxX, 0, MaxX, Height );
				G.DrawString( m_MaximumX.ToString( "G3" ), Font, Brushes.Red, MaxX + 2, 20 );

				// Draw the curves
				for ( int l=Form1OLD.SH_SQORDER-1; l >= 0; l-- )
				{
					float	fPreviousPosX = -1000.0f;
 					float	fPreviousPosY = 0.0f;
					for ( int x=0; x <= Form1OLD.MEASURES_COUNT; x++ )
					{
						float	fCurrentPosX = Width * x / Form1OLD.MEASURES_COUNT;
						float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) m_Curves[l,x]));	// Should map to top of the screen if 1 and bottom if -1
						G.DrawLine( MyPens[l], fPreviousPosX, fPreviousPosY, fCurrentPosX, fCurrentPosY );

						fPreviousPosX = fCurrentPosX;
						fPreviousPosY = fCurrentPosY;
					}

					G.DrawString( l.ToString(), Font, Brushes.Black, Width-30, Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) m_Curves[l,Form1OLD.MEASURES_COUNT])) - 13 );
				}

				// Draw least-square fitting of the above curves
				if ( m_bDrawLeastSquareFit && m_LeastSquareFitOrder6[0] != null )
				{
					for ( int l=Form1OLD.SH_SQORDER-1; l >= 0; l-- )
					{
						float	fPreviousX = -1000.0f;
						float	fPreviousY = 0.0f;
						for ( int i=0; i <= Form1OLD.MEASURES_COUNT; i++ )
						{
							float	t = (float) i / Form1OLD.MEASURES_COUNT;
							double	x = m_RadiusStart + (m_RadiusEnd - m_RadiusStart) * t;

//							double	y = ComputeLSF( l, x );
//							double	y = ComputeLSFOrder5( l, x );
							double	y = ComputeLSFOrder6( l, x );

							float	fCurrentPosX = Width * t;
							float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) y));	// Should map to top of the screen if 1 and bottom if -1

							G.DrawLine( Pens.IndianRed, fPreviousX, fPreviousY, fCurrentPosX, fCurrentPosY );
							fPreviousX = fCurrentPosX;
							fPreviousY = fCurrentPosY;
						}
					}
				}

				// Draw additional infos
				for ( int LSFIndex=0; LSFIndex < Form1OLD.LSF_COUNT; LSFIndex++ )
				{
					float	x2 = (float) ((m_RadiusLeastSquareFitLimits[LSFIndex] - m_RadiusStart) * Width / (m_RadiusEnd-m_RadiusStart));
					G.DrawLine( System.Drawing.Pens.Blue, x2, 0, x2, Height );
					G.DrawString( m_RadiusLeastSquareFitLimits[LSFIndex].ToString( "G4" ), Font, Brushes.Blue, x2 + 2, 40 );
				}
				G.DrawString( m_Title + " - Scale = " + m_ScaleY.ToString( "G4" ), Font, Brushes.Black, 0, 0 );
			}
			Refresh();
		}


		// This should contain multiple curves from different simulation parameters
		public double[][,]	m_CurvesComparisonY = new double[33][,];
		public double[][,]	m_CurvesComparisonZ = new double[33][,];
		public int			m_ComparisonSHCoeffIndex = 0;	// The index of the SH coefficient curve to show
		public int			m_ComparisonShow = 1;	// 1 is Y only, 2 is Z only, 3 is both
		protected Pen[]		m_ComparisonCurvesPens = new Pen[33];

		public void		UpdateBitmap2()
		{
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

				float	MaxX = (float) (Width * (m_MaximumX-m_RadiusStart) / (m_RadiusEnd - m_RadiusStart));
				G.DrawLine( Pens.Red, MaxX, 0, MaxX, Height );
				G.DrawString( m_MaximumX.ToString( "G3" ), Font, Brushes.Red, MaxX + 2, 20 );

				// Draw the curves
 				if ( m_bDrawLeastSquareFit )
				{
					if ( (m_ComparisonShow & 1) != 0 )
						for ( int CurveIndex=0; CurveIndex < m_CurvesComparisonY.Length; CurveIndex++ )
						{
							float	fPreviousPosX = -1000.0f;
 							float	fPreviousPosY = 0.0f;
							for ( int x=0; x < Form1OLD.MEASURES_COUNT; x++ )
							{
								float	fCurrentPosX = Width * x / Form1OLD.MEASURES_COUNT;
								float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) m_CurvesComparisonY[CurveIndex][m_ComparisonSHCoeffIndex,x]));	// Should map to top of the screen if 1 and bottom if -1
								G.DrawLine( m_ComparisonCurvesPens[CurveIndex], fPreviousPosX, fPreviousPosY, fCurrentPosX, fCurrentPosY );

								fPreviousPosX = fCurrentPosX;
								fPreviousPosY = fCurrentPosY;
							}
						}
					if ( (m_ComparisonShow & 2) != 0 )
						for ( int CurveIndex=0; CurveIndex < m_CurvesComparisonY.Length; CurveIndex++ )
						{
							float	fPreviousPosX = -1000.0f;
 							float	fPreviousPosY = 0.0f;
							for ( int x=0; x < Form1OLD.MEASURES_COUNT; x++ )
							{
								float	fCurrentPosX = Width * x / Form1OLD.MEASURES_COUNT;
								float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) m_CurvesComparisonZ[CurveIndex][m_ComparisonSHCoeffIndex,x]));	// Should map to top of the screen if 1 and bottom if -1
								G.DrawLine( m_ComparisonCurvesPens[CurveIndex], fPreviousPosX, fPreviousPosY, fCurrentPosX, fCurrentPosY );

								fPreviousPosX = fCurrentPosX;
								fPreviousPosY = fCurrentPosY;
							}
						}
				}
				else
				{	// Approximate from 1st curve + offset/scale
					if ( (m_ComparisonShow & 1) != 0 )
						for ( int CurveIndex=0; CurveIndex < m_CurvesComparisonY.Length; CurveIndex++ )
						{
							int		RadiusPow = CurveIndex - m_CurvesComparisonY.Length/2;
							float	fRadiusFactor = (float) Math.Pow( 2.0, 0.125f * RadiusPow );

							float	fXOffset = 0.0f;
							float	fScaleX = 1.0f;
							float	fScaleY = 1.0f;
							if ( RadiusPow < 0 )
							{
								fScaleY = 1.0f - RadiusPow / 130.0f;
								fScaleX = 1.0f + RadiusPow / 120.0f;
							}
							else if ( RadiusPow > 0 )
							{
								fScaleY = 1.0f - RadiusPow / 520.0f;
								fScaleX = 1.0f + RadiusPow / 480.0f;
							}

							float	fPreviousPosX = -1000.0f;
 							float	fPreviousPosY = 0.0f;
							for ( int x=0; x < Form1OLD.MEASURES_COUNT; x++ )
							{
								int		CurveX = (int) Math.Min( Form1OLD.MEASURES_COUNT-1, (x + fXOffset) * fScaleX );
								float	fCurveY = fScaleY * (float) m_CurvesComparisonY[16][m_ComparisonSHCoeffIndex,CurveX];

								float	fCurrentPosX = Width * x / Form1OLD.MEASURES_COUNT;
								float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * fCurveY));	// Should map to top of the screen if 1 and bottom if -1
								G.DrawLine( m_ComparisonCurvesPens[CurveIndex], fPreviousPosX, fPreviousPosY, fCurrentPosX, fCurrentPosY );

								fPreviousPosX = fCurrentPosX;
								fPreviousPosY = fCurrentPosY;
							}
						}
					if ( (m_ComparisonShow & 2) != 0 )
						for ( int CurveIndex=0; CurveIndex < m_CurvesComparisonY.Length; CurveIndex++ )
						{
							int		RadiusPow = CurveIndex - m_CurvesComparisonY.Length/2;
							float	fRadiusFactor = (float) Math.Pow( 2.0, 0.125f * RadiusPow );

							float	fXOffset = 0.0f;
							float	fScaleX = 1.0f;
							float	fScaleY = 1.0f;
							if ( RadiusPow < 0 )
							{
								fScaleY = 1.0f - RadiusPow / 130.0f;
								fScaleX = 1.0f + RadiusPow / 120.0f;
							}
							else if ( RadiusPow > 0 )
							{
								fScaleY = 1.0f - RadiusPow / 520.0f;
								fScaleX = 1.0f + RadiusPow / 480.0f;
							}

							float	fPreviousPosX = -1000.0f;
 							float	fPreviousPosY = 0.0f;
							for ( int x=0; x < Form1OLD.MEASURES_COUNT; x++ )
							{
								int		CurveX = (int) Math.Min( Form1OLD.MEASURES_COUNT-1, (x + fXOffset) * fScaleX );
								float	fCurveY = fScaleY * (float) m_CurvesComparisonZ[16][m_ComparisonSHCoeffIndex,CurveX];

								float	fCurrentPosX = Width * x / Form1OLD.MEASURES_COUNT;
								float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * fCurveY));	// Should map to top of the screen if 1 and bottom if -1
								G.DrawLine( m_ComparisonCurvesPens[CurveIndex], fPreviousPosX, fPreviousPosY, fCurrentPosX, fCurrentPosY );

								fPreviousPosX = fCurrentPosX;
								fPreviousPosY = fCurrentPosY;
							}
						}
				}

// 				// Draw least-square fitting of the above curves
// 				if ( m_bDrawLeastSquareFit && m_LeastSquareFitOrder6[0] != null )
// 				{
// 					for ( int l=Form1.SH_SQORDER-1; l >= 0; l-- )
// 					{
// 						float	fPreviousX = -1000.0f;
// 						float	fPreviousY = 0.0f;
// 						for ( int i=0; i <= Form1.MEASURES_COUNT; i++ )
// 						{
// 							float	t = (float) i / Form1.MEASURES_COUNT;
// 							double	x = m_RadiusStart + (m_RadiusEnd - m_RadiusStart) * t;
// 
// //							double	y = ComputeLSF( l, x );
// //							double	y = ComputeLSFOrder5( l, x );
// 							double	y = ComputeLSFOrder6( l, x );
// 
// 							float	fCurrentPosX = Width * t;
// 							float	fCurrentPosY = Height * (1.0f - 0.5f * (1.0f + m_ScaleY * (float) y));	// Should map to top of the screen if 1 and bottom if -1
// 
// 							G.DrawLine( Pens.IndianRed, fPreviousX, fPreviousY, fCurrentPosX, fCurrentPosY );
// 							fPreviousX = fCurrentPosX;
// 							fPreviousY = fCurrentPosY;
// 						}
// 					}
// 				}

// 				// Draw additional infos
// 				for ( int LSFIndex=0; LSFIndex < Form1.LSF_COUNT; LSFIndex++ )
// 				{
// 					float	x2 = (float) ((m_RadiusLeastSquareFitLimits[LSFIndex] - m_RadiusStart) * Width / (m_RadiusEnd-m_RadiusStart));
// 					G.DrawLine( System.Drawing.Pens.Blue, x2, 0, x2, Height );
// 					G.DrawString( m_RadiusLeastSquareFitLimits[LSFIndex].ToString( "G4" ), Font, Brushes.Blue, x2 + 2, 40 );
// 				}
				string ComparisonShow = m_ComparisonShow == 1 ? "Y Only" : (m_ComparisonShow == 2 ? "Z Only" : "Y+Z");
				G.DrawString( m_Title + " Show = " + ComparisonShow + " - SHCoeff = " + m_ComparisonSHCoeffIndex + " - Scale = " + m_ScaleY.ToString( "G4" ), Font, Brushes.Black, 0, 0 );
			}
			Refresh();
		}

		public double	ComputeLSF( int l, double x )
		{
			x = Math.Min( x, m_RadiusLeastSquareFitLimits[Form1OLD.LSF_COUNT-1] );

			const double	ThresholdRatio = 0.15;	// The ratio of the interval width at which we start interpolating between 2 neighbor LSF

			double	y = 0.0;
			double	SumW = 0.0;

			double	PrevLimit = 0.0;
			double	PrevWidth = 0.0;
			double	PrevThreshold = 0.0;
			for ( int LSFIndex=0; LSFIndex < Form1OLD.LSF_COUNT; LSFIndex++ )
			{
				double	Limit = m_RadiusLeastSquareFitLimits[LSFIndex];
				double	Width = Limit - PrevLimit;
				double	Threshold = ThresholdRatio * Width;

				double	NextWidth = m_RadiusLeastSquareFitLimits[Math.Min( Form1OLD.LSF_COUNT-1, LSFIndex+1 )] - Limit;
				double	NextThreshold = ThresholdRatio * NextWidth;

				double	w = 0.0;
				if ( x >= PrevLimit-PrevThreshold && x <= Limit+NextThreshold )
				{
					if ( x < PrevLimit )
						w = 0.5*(PrevThreshold-PrevLimit+x) / PrevThreshold;
					else if ( x < PrevLimit+Threshold )
						w = 0.5*(Threshold+x-PrevLimit) / Threshold;
					else if ( x > Limit )
						w = 0.5*(NextThreshold+Limit-x) / NextThreshold;
					else if ( x > Limit-Threshold )
						w = 0.5*(Threshold+Limit-x) / Threshold;
					else
						w = 1.0;
				}

				y += w * (m_LeastSquareFit[LSFIndex,l,3] + x * (m_LeastSquareFit[LSFIndex,l,2]  + x * (m_LeastSquareFit[LSFIndex,l,1]  + x * m_LeastSquareFit[LSFIndex,l,0])));
				SumW += w;

				PrevLimit = Limit;
				PrevWidth = Width;
				PrevThreshold = Threshold;
			}

			return y / SumW;
		}

		protected double	ComputeLSFOrder5( int l, double x )
		{
			return	m_LeastSquareFitOrder6[l][4]
				+ x * (m_LeastSquareFitOrder6[l][3]
				+ x * (m_LeastSquareFitOrder6[l][2]
				+ x * (m_LeastSquareFitOrder6[l][1]
				+ x * (m_LeastSquareFitOrder6[l][0]
				))));
		}

		protected double	ComputeLSFOrder6( int l, double x )
		{
			return	m_LeastSquareFitOrder6[l][5]
				+ x * (m_LeastSquareFitOrder6[l][4]
				+ x * (m_LeastSquareFitOrder6[l][3]
				+ x * (m_LeastSquareFitOrder6[l][2]
				+ x * (m_LeastSquareFitOrder6[l][1]
				+ x * (m_LeastSquareFitOrder6[l][0]
				)))));
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
