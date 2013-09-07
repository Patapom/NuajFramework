#define SCATTERING1	// Scattering order

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CloudPreComputer
{
	public partial class Form1OLD : Form
	{
		#region CONSTANTS

		public const int		SH_ORDER = 3;
		public const int		SH_SQORDER = SH_ORDER*SH_ORDER;
//		public const int		SH_SAMPLES_THETA_COUNT = 10;
		public const int		SH_SAMPLES_THETA_COUNT = 64;
		public const int		MEASURES_COUNT = 512;
		public const int		LSF_COUNT = 4;

		// Scattering & Extinction coefficients computation
		protected const double	N0						= 300.0 * 1e6;						// Density of droplets = 300cm-3
		protected const double	Re						= 7.0 * 1e-6;						// Droplets effective radius = 7µm
		protected const double	EXTINCTION_CROSS_SECTION= 0.5 * Math.PI * Re * Re;			// Extinction Cross Section = Pi * Re * Re (see that as a disc covering a single droplet)
														// Notice the 0.5 factor here to take into account the fact that we removed the forward peak of the Mie phase function (induces a reduced cross section)
		protected const double	EXTINCTION_COEFFICIENT	= N0 * EXTINCTION_CROSS_SECTION;	// Extinction Coefficient in m^-1 (Sigma = 0.046181412007769960605400857734209)
		protected const double	SCATTERING_COEFFICIENT	= EXTINCTION_COEFFICIENT;			// Scattering and extinction are almost the same in clouds because of albedo almost == 1

		#endregion

		#region FIELDS

		protected float	m_Value = 0.0f;

		protected Atmospheric.PhaseFunction	m_Phase = new Atmospheric.PhaseFunction();
		protected SphericalHarmonics.SHSamplesCollection	m_Samples = new SphericalHarmonics.SHSamplesCollection( 1 );
		protected double[]		m_PhaseFactors = null;


		protected double[]	m_SingleScatteringMaximums = new double[101]
		{
			21.66015625,
			21.66015625,
			21.66015625,
			21.660156249999996,
			21.66015625,
			21.66015625,
			21.660156249999996,
			21.66015625,
			21.66015625,
			21.66015625,
			21.660156250000004,
			21.660156250000004,
			21.66015625,
			21.66015625,
			21.66015625,
			21.660156250000004,
			21.660156250000004,
			21.66015625,
			21.66015625,
			21.66015625,
			25.546875,
			25.546875,
			25.546874999999996,
			25.546875,
			25.546874999999996,
			25.546875,
			25.546874999999996,
			25.546875,
			25.546875,
			25.546875,
			25.546875,
			25.546874999999996,
			25.546875,
			25.546875,
			25.546875,
			25.546875000000004,
			25.546875000000004,
			25.546875,
			29.43359375,
			29.433593750000004,
			29.43359375,
			29.433593750000004,
			29.43359375,
			29.433593749999996,
			29.433593750000007,
			29.433593750000007,
			29.433593749999996,
			29.433593750000004,
			29.433593750000004,
			33.3203125,
			33.3203125,
			33.3203125,
			33.3203125,
			33.320312499999993,
			33.3203125,
			33.3203125,
			33.3203125,
			33.3203125,
			37.20703125,
			37.20703125,
			37.20703125,
			37.207031249999993,
			37.207031249999993,
			37.207031250000007,
			41.09375,
			41.09375,
			41.09375,
			41.093750000000007,
			41.093750000000007,
			44.98046875,
			44.98046875,
			44.98046875,
			44.98046875,
			48.8671875,
			48.867187500000007,
			48.8671875,
			52.753906250000007,
			52.753906250000007,
			56.640625000000007,
			56.640625,
			60.52734375,
			60.527343749999986,
			64.414062499999986,
			64.414062500000014,
			68.30078125,
			72.1875,
			76.07421875,
			79.9609375,
			83.84765625,
			87.734374999999986,
			91.621093750000014,
			99.394531250000014,
			107.16796875,
			118.82812499999999,
			130.48828125,
			146.03515625,
			169.35546875,
			200.44921874999997,
			258.75,
			390.89843750000006,
			0.0,
		};


		#endregion

		#region METHODS

		public Form1OLD()
		{
			InitializeComponent();

			// Initialize the SH samples
			m_Samples.Initialize( SH_ORDER, SH_SAMPLES_THETA_COUNT );

			// Initialize the phase function
 			m_Phase.Init( Atmospheric.CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION, 5.0f * (float) Math.PI / 180.0f, (float) Math.PI, 1024 );

			// Pre-compute phase factors and make sure they integrate to 0.5
			// They must not integrate to 1 as we clipped the tip of the phase function below 5° and this
			//	tip represents about 50% of the energy gained through strong forward scattering
			// This additional 50% energy missing from multiple scattering will be added on top of the
			//	value retrieved from the scattering table, through analytic computation...
			//
			m_PhaseFactors = new double[m_Samples.SamplesCount];
			double	fNormalizationFactor = 1.0 / m_Samples.SamplesCount;

			double	IntegralCheck = 0.0;
			for ( int PhaseFactorIndex=0; PhaseFactorIndex < m_PhaseFactors.Length; PhaseFactorIndex++ )
			{
				float	fAngle = (float) Math.Acos( -m_Samples.Samples[PhaseFactorIndex].m_Direction.x );
				m_PhaseFactors[PhaseFactorIndex] = fNormalizationFactor * m_Phase.GetPhaseFactor( fAngle );
				IntegralCheck += m_PhaseFactors[PhaseFactorIndex] * Math.Sin( fAngle );
			}

			fNormalizationFactor = 0.5 / IntegralCheck;
			IntegralCheck = 0.0;
			for ( int PhaseFactorIndex=0; PhaseFactorIndex < m_PhaseFactors.Length; PhaseFactorIndex++ )
			{
				float	fAngle = (float) Math.Acos( -m_Samples.Samples[PhaseFactorIndex].m_Direction.x );
				m_PhaseFactors[PhaseFactorIndex] *= fNormalizationFactor;
				IntegralCheck += m_PhaseFactors[PhaseFactorIndex] * Math.Sin( fAngle );
			}
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

//			OffCenterTest( 0.0f );
//			SquishTest( 3.0f );
//			ScatteringTest2D( m_Value );

//			CurvesComparisonTest( 0 );

			Application.Idle += new EventHandler(Application_Idle);
		}

		protected bool	m_bComputed = false;
		void  Application_Idle(object sender, EventArgs e)
		{
			if ( m_bComputed )
				return;

// 			// Retrieve the maximums
// 			double[]	Maximums = new double[101];
// 			for ( int i=0; i <= 100; i++ )
// 			{
// 				ScatteringTest2D( i / 100.0f );
// 				Maximums[i] = outputPanel.m_MaximumX;
// 				Application.DoEvents();
// 			}

			// Compute the coefficients and save
			int OffsetXStepsCount = 64;
			int RadiusStepsCount = 32;

//			throw new Exception( "Tutututu ! Tu vas écraser tes data crétin !" );

//			for ( int RadiusIndex=0; RadiusIndex <= RadiusStepsCount; RadiusIndex++ )
			for ( int RadiusIndex=RadiusStepsCount/2; RadiusIndex <= RadiusStepsCount; RadiusIndex++ )
			{
				int		RadiusPow = RadiusIndex - RadiusStepsCount/2;
				float	fRadiusFactor = (float) Math.Pow( 2.0, 0.125f * RadiusPow );

// 				string TargetPathCurves = "../../Tables/2D RadiusY +Position Variation/SingleScattering£_?.tb";
// 				string TargetPathFitting = "../../Tables/2D RadiusY +Position Variation/SingleScattering£_?.pf";
				string TargetPathCurves = "../../Tables/2D RadiusZ +Position Variation/SingleScattering£_?.tb";
				string TargetPathFitting = "../../Tables/2D RadiusZ +Position Variation/SingleScattering£_?.pf";

				TargetPathCurves = TargetPathCurves.Replace( "£", RadiusIndex.ToString() );
				TargetPathFitting = TargetPathFitting.Replace( "£", RadiusIndex.ToString() );

				for ( int OffsetXIndex = 0; OffsetXIndex < OffsetXStepsCount; OffsetXIndex++ )
				{
					float	fOffsetX = (float) OffsetXIndex / OffsetXStepsCount;

					ScatteringTest2D( fOffsetX, fRadiusFactor );

// 					// Write the result curves
// 					System.IO.FileInfo File = new System.IO.FileInfo( TargetPathCurves.Replace( "?", OffsetXIndex.ToString() ) );
// 					System.IO.FileStream Stream = File.Create();
// 					System.IO.BinaryWriter Writer = new System.IO.BinaryWriter( Stream );
// 
// 					for ( int ResultIndex = 0; ResultIndex < MEASURES_COUNT; ResultIndex++ )
// 						for ( int l = 0; l < SH_SQORDER; l++ )
// 							Writer.Write( outputPanel.m_Curves[l, ResultIndex] );
// 
// 					Writer.Close();
// 					Writer.Dispose();
// 					Stream.Close();
// 					Stream.Dispose();
// 
// 					// Write the result curve fitting
// 					File = new System.IO.FileInfo( TargetPathFitting.Replace( "?", OffsetXIndex.ToString() ) );
// 					Stream = File.Create();
// 					Writer = new System.IO.BinaryWriter( Stream );
// 
// 					for ( int l = 0; l < SH_SQORDER; l++ )
// 					{
// 						Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][0] );
// 						Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][1] );
// 						Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][2] );
// 						Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][3] );
// 						Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][4] );
// 						Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][5] );
// 					}
// 
// 					Writer.Close();
// 					Writer.Dispose();
// 					Stream.Close();
// 					Stream.Dispose();

					// Display
					Application.DoEvents();
				}
			}

			m_bComputed = true;
		}

		#region OffCenter Test

		/// <summary>
		/// This test consists in displaying the variation of the SH coefficients when computing
		///  the attenuated energy reaching a point within a dense sphere as the point moves from
		///  the bottom to the top of the sphere
		/// </summary>
		protected void	OffCenterTest( float _fAngle )
		{
			float		fSphereRadius = 100.0f;
			double		fExtinctionFactor = -Math.Log( 0.5 ) / fSphereRadius;	// So that the energy is halved when going through half the sphere

			WMath.Point	Center = new WMath.Point( 0, 0, 0 );
			float		fPreviousPosX = -1000.0f;
			float[]		PreviousPosY = new float[SH_SQORDER];
			float[]		CurrentPosY = new float[SH_SQORDER];
			double[]	SHCoeffs = new double[SH_SQORDER];

			// Varying along X
// 			WMath.Point	StartPos = new WMath.Point( -fSphereRadius, 0, 0 );
// 			WMath.Point	EndPos = new WMath.Point( +fSphereRadius, 0, 0 );
			// Varying along Y
// 			WMath.Point	StartPos = new WMath.Point( 0, -fSphereRadius, 0 );
// 			WMath.Point	EndPos = new WMath.Point( 0, +fSphereRadius, 0 );
			// Varying along Z
// 			WMath.Point	StartPos = new WMath.Point( 0, 0, -fSphereRadius );
// 			WMath.Point	EndPos = new WMath.Point( 0, 0, +fSphereRadius );

			// Varying along X & Y
// 			WMath.Point	StartPos = fSphereRadius * new WMath.Point( -(float) Math.Sin( _fAngle ), -(float) Math.Cos( _fAngle ), 0 );
// 			WMath.Point	EndPos = fSphereRadius * new WMath.Point( +(float) Math.Sin( _fAngle ), +(float) Math.Cos( _fAngle ), 0 );
			// Varying along Y & Z
			WMath.Point	StartPos = fSphereRadius * new WMath.Point( 0, -(float) Math.Cos( _fAngle ), +(float) Math.Sin( _fAngle ) );
			WMath.Point	EndPos = fSphereRadius * new WMath.Point( 0, +(float) Math.Cos( _fAngle ), -(float) Math.Sin( _fAngle ) );

			using ( Graphics G = Graphics.FromImage( outputPanel.m_Bitmap ) )
			{
				for ( int i=0; i <= 200; i++ )
				{
					float	t = i / 200.0f;
					Center = StartPos + t * (EndPos - StartPos);

					Array.Copy( CurrentPosY, PreviousPosY, SH_SQORDER );
					Array.Clear( SHCoeffs, 0, SH_SQORDER );

					float	c = (Center.x*Center.x+Center.y*Center.y+Center.z*Center.z) - fSphereRadius*fSphereRadius;	// This never changes, it's the origin point
					foreach ( SphericalHarmonics.SHSamplesCollection.SHSample Sample in m_Samples )
					{
						// Compute the intersection of that sample's ray with the sphere
						float	b = Sample.m_Direction.x * Center.x + Sample.m_Direction.y * Center.y + Sample.m_Direction.z * Center.z;
						float	Delta = b*b-c;
						if ( Delta < 0.0f )
							continue;	// ???

						float	s = -b + (float) Math.Sqrt( Delta );

						// Compute the attenuation resulting from following that sample
						double	fEnergy = 1.0 * Math.Exp( -fExtinctionFactor * s );

						// Accumulate SH coefficients
						for ( int l=0; l < SH_SQORDER; l++ )
							SHCoeffs[l] += fEnergy * Sample.m_SHFactors[l];
					}

					// Normalize
					for ( int l=0; l < SH_SQORDER; l++ )
						SHCoeffs[l] /= m_Samples.SamplesCount;

					// Paint the result
					float	fFactor = 4.0f;
					Pen[]	Pens = new Pen[]
					{
						System.Drawing.Pens.DarkSlateGray,
						System.Drawing.Pens.Red,
						System.Drawing.Pens.DarkGreen,
						System.Drawing.Pens.Blue,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
					};

					float	fCurrentPosX = outputPanel.Width * t;
					for ( int l=SH_SQORDER-1; l >= 0; l-- )
					{
						CurrentPosY[l] = outputPanel.Height * (1.0f - 0.5f * (1.0f + fFactor * (float) SHCoeffs[l]));	// Should map to top of the screen if 1 and bottom if -1
						G.DrawLine( Pens[l], fPreviousPosX, PreviousPosY[l], fCurrentPosX, CurrentPosY[l] );
					}

					fPreviousPosX = fCurrentPosX;
				}

				for ( int l=0; l < SH_SQORDER; l++ )
					G.DrawString( l.ToString(), Font, Brushes.Black, Width-30, CurrentPosY[l] );

				G.DrawString( "OffCenter Test - Angle " + ((_fAngle * 180.0 / Math.PI) % 360.0).ToString( "G4" ) + "°", Font, Brushes.Black, 0, 0 );
			}
			outputPanel.Refresh();
		}

		#endregion

		#region Squish Test

		/// <summary>
		/// This test consists in displaying the variation of the SH coefficients when computing
		///  the attenuated energy reaching a point within a dense sphere as the sphere squishes
		///  into an ellipsoid along one or several specific axes
		/// </summary>
		protected void	SquishTest( float _fRadiusFactor )
		{
			float		fSphereRadius = 100.0f;
			double		fExtinctionFactor = -Math.Log( 0.5 ) / fSphereRadius;	// So that the energy is halved when going through half the sphere

			WMath.Point		Position = new WMath.Point( -fSphereRadius, 0, 0 );
			WMath.Vector	Radii = new WMath.Vector( fSphereRadius, fSphereRadius, fSphereRadius );

			float		fPreviousPosX = -1000.0f;
			float[]		PreviousPosY = new float[SH_SQORDER];
			float[]		CurrentPosY = new float[SH_SQORDER];
			double[]	SHCoeffs = new double[SH_SQORDER];

			using ( Graphics G = Graphics.FromImage( outputPanel.m_Bitmap ) )
			{
				for ( int i=0; i <= 200; i++ )
				{
					float	t = i / 200.0f;
					float	fRadius =  (0.1f + (10.0f - 0.1f) * t) * fSphereRadius;

					// Varying along X
					Position.x = -0.999f * fRadius;	// A little inside
					Radii.X = fRadius;
 					// Varying along Y
// 					Position.x = -0.999f * fSphereRadius;	// A little inside
// 					Radii.y = fRadius;

					Array.Copy( CurrentPosY, PreviousPosY, SH_SQORDER );
					Array.Clear( SHCoeffs, 0, SH_SQORDER );

					foreach ( SphericalHarmonics.SHSamplesCollection.SHSample Sample in m_Samples )
					{
						WMath.Point		C = new WMath.Point( 0, 0, 0 );
						WMath.Vector	V = Sample.m_Direction / Radii;
						WMath.Vector	D = (Position - C) / Radii;		// P - C

						// Compute the intersection of that sample's ray with the sphere
						float	a = V | V;
						float	b = D | V;
						float	c = (D | D) - 1.0f;
						float	Delta = b*b - a*c;
						if ( Delta < 0.0f )
							continue;	// ???

						float	s = (-b + (float) Math.Sqrt( Delta )) / a;

// WMath.Point	Hit = Position + s * Sample.m_Direction;
// float		fHitDistance = (Hit - Position).Magnitude();

						// Compute  the attenuation resulting from following that sample
						double	fEnergy = 1.0 * Math.Exp( -fExtinctionFactor * s );

						// Accumulate SH coefficients
						for ( int l=0; l < SH_SQORDER; l++ )
							SHCoeffs[l] += fEnergy * Sample.m_SHFactors[l];
					}

					// Normalize
					for ( int l=0; l < SH_SQORDER; l++ )
						SHCoeffs[l] /= m_Samples.SamplesCount;

					// Paint the result
					Pen[]	Pens = new Pen[]
					{
						System.Drawing.Pens.DarkSlateGray,
						System.Drawing.Pens.Red,
						System.Drawing.Pens.DarkGreen,
						System.Drawing.Pens.Blue,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
						System.Drawing.Pens.Gold,
					};

					float	fCurrentPosX = outputPanel.Width * t;
					for ( int l=SH_SQORDER-1; l >= 0; l-- )
					{
						CurrentPosY[l] = outputPanel.Height * (1.0f - 0.5f * (1.0f + _fRadiusFactor * (float) SHCoeffs[l]));	// Should map to top of the screen if 1 and bottom if -1
						G.DrawLine( Pens[l], fPreviousPosX, PreviousPosY[l], fCurrentPosX, CurrentPosY[l] );
					}

					fPreviousPosX = fCurrentPosX;
				}

				for ( int l=0; l < SH_SQORDER; l++ )
					G.DrawString( l.ToString(), Font, Brushes.Black, Width-30, CurrentPosY[l] );

				G.DrawString( "Squish Test - Factor = " + _fRadiusFactor.ToString( "G4" ), Font, Brushes.Black, 0, 0 );
			}
			outputPanel.Refresh();
		}

		#endregion

		protected void	CurvesComparisonTest( int _OffsetXIndex )
		{
			// Reload plenty of curves
			string TargetPathCurvesY = "../../Tables/2D RadiusY +Position Variation/SingleScattering£_?.tb";
			string TargetPathFittingY = "../../Tables/2D RadiusY +Position Variation/SingleScattering£_?.pf";
			string TargetPathCurvesZ = "../../Tables/2D RadiusZ +Position Variation/SingleScattering£_?.tb";
			string TargetPathFittingZ = "../../Tables/2D RadiusZ +Position Variation/SingleScattering£_?.pf";

			TargetPathCurvesY = TargetPathCurvesY.Replace( "?", _OffsetXIndex.ToString() );
			TargetPathFittingY = TargetPathFittingY.Replace( "?", _OffsetXIndex.ToString() );
			TargetPathCurvesZ = TargetPathCurvesZ.Replace( "?", _OffsetXIndex.ToString() );
			TargetPathFittingZ = TargetPathFittingZ.Replace( "?", _OffsetXIndex.ToString() );

			for ( int CurveIndex=0; CurveIndex <= 32; CurveIndex++ )
			{
				// Y Variations
				System.IO.FileInfo File = new System.IO.FileInfo( TargetPathCurvesY.Replace( "£", CurveIndex.ToString() ) );
				System.IO.FileStream Stream = File.OpenRead();
				System.IO.BinaryReader Reader = new System.IO.BinaryReader( Stream );

				outputPanel.m_CurvesComparisonY[CurveIndex] = new double[SH_SQORDER,MEASURES_COUNT];
				for ( int ResultIndex = 0; ResultIndex < MEASURES_COUNT; ResultIndex++ )
					for ( int l = 0; l < SH_SQORDER; l++ )
						outputPanel.m_CurvesComparisonY[CurveIndex][l,ResultIndex] = Reader.ReadDouble();

				Reader.Close();
				Reader.Dispose();
				Stream.Close();
				Stream.Dispose();

				// Z Variations
				File = new System.IO.FileInfo( TargetPathCurvesZ.Replace( "£", CurveIndex.ToString() ) );
				Stream = File.OpenRead();
				Reader = new System.IO.BinaryReader( Stream );

				outputPanel.m_CurvesComparisonZ[CurveIndex] = new double[SH_SQORDER,MEASURES_COUNT];
				for ( int ResultIndex = 0; ResultIndex < MEASURES_COUNT; ResultIndex++ )
					for ( int l = 0; l < SH_SQORDER; l++ )
						outputPanel.m_CurvesComparisonZ[CurveIndex][l,ResultIndex] = Reader.ReadDouble();

				Reader.Close();
				Reader.Dispose();
				Stream.Close();
				Stream.Dispose();

// 				// Write the result curve fitting
// 				File = new System.IO.FileInfo( TargetPathFitting.Replace( "£", CurveIndex.ToString() ) );
// 				Stream = File.OpenRead();
// 				Reader = Reader = new System.IO.BinaryReader( Stream );
// 
// 				for ( int l = 0; l < SH_SQORDER; l++ )
// 				{
// 					Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][0] );
// 					Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][1] );
// 					Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][2] );
// 					Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][3] );
// 					Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][4] );
// 					Writer.Write( outputPanel.m_LeastSquareFitOrder6[l][5] );
// 				}
// 
// 				Reader.Close();
// 				Reader.Dispose();
// 				Stream.Close();
// 				Stream.Dispose();
			}

			outputPanel.m_Title = "OffsetX = " + _OffsetXIndex;

			outputPanel.UpdateBitmap2();
		}

		/// <summary>
		/// This test only changes 2 parameters at a time and displays SH coefficients variations for that change in 2 dimensions
		/// </summary>
		/// <param name="_Value">Some value to make the test with</param>
		/// <param name="_Value2">Some other value</param>
		protected void	ScatteringTest2D( float _Value, float _Value2 )
		{
			double[]	SHCoeffs = new double[SH_SQORDER];

			double[,]	Sumi = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumYi = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumYiXi = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumYiXi2 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumYiXi3 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumYiXi4 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumYiXi5 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi2 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi3 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi4 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi5 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi6 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi7 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi8 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi9 = new double[LSF_COUNT,SH_SQORDER];
			double[,]	SumXi10 = new double[LSF_COUNT,SH_SQORDER];

/*			float	fRadiusStart = 10.0f;
			float	fRadiusEnd = 500.0f;

			outputPanel.m_Title = "SingleScattering Test (OffsetX=" + _Value + " - RadiusY Factor=" + _Value2 + ")";
			outputPanel.m_RadiusStart = fRadiusStart;
			outputPanel.m_RadiusEnd = fRadiusEnd;

			for ( int i=0; i <= MEASURES_COUNT; i++ )
			{
				float	t = (float) i / MEASURES_COUNT;
				float	fRadius = fRadiusStart * (1-t) + fRadiusEnd * t;;

#if SCATTERING1
				//////////////////////////////////////////////////////////////////////////
				// Compute in-scattering of order 1

				// Make the radius vary uniformly
//				SingleScatteringWithOffset( -_Scale * 0.1f, _Scale * 0.1f, new WMath.Vector( fRadius, fRadius, fRadius ), SHCoeffs );

// Y Variation				
//				SingleScatteringWithOffset( _Value, 0.0f, 0.0f, new WMath.Vector( fRadius, _Value2 * fRadius, fRadius ), SHCoeffs );
// Z Variation				
				SingleScatteringWithOffset( _Value, 0.0f, 0.0f, new WMath.Vector( fRadius, fRadius, _Value2 * fRadius ), SHCoeffs );

				// Anisotropic radius
// 					float	fModifiedRadius = fRadius * (0.25f + 4.0f * t);
// 					SingleScatteringWithOffset( 0.0f, 0.0f, new WMath.Vector( fRadius, fRadius, fModifiedRadius ), SHCoeffs );
#elif SCATTERING2
				//////////////////////////////////////////////////////////////////////////
				// Compute in-scattering of order 2

				// Make the radius vary uniformly
				DoubleScatteringWithOffset( -_Scale * 0.1f, _Scale * 0.1f, new WMath.Vector( fRadius, fRadius, fRadius ), SHCoeffs );
#endif
				// Update curves
				for ( int l=0; l < SH_SQORDER; l++ )
					outputPanel.m_Curves[l,i] = SHCoeffs[l];
			}

			// Retrieve maximum peaks
			double[]	MaximumsY = new double[9];
			double[]	MaximumsX = new double[9];
			double		SumMaximumsX = 0.0;
			double		SumWeights = 0.0;
			for ( int l=0; l < Form1OLD.SH_SQORDER; l++ )
			{
				for ( int i=0; i <= Form1OLD.MEASURES_COUNT; i++ )
					if ( outputPanel.m_Curves[l,i] > MaximumsY[l] )
					{
						MaximumsY[l] = outputPanel.m_Curves[l,i];
						MaximumsX[l] = fRadiusStart + (fRadiusEnd-fRadiusStart) * i / MEASURES_COUNT;
					}

				double Weight = Math.Abs( MaximumsY[l] );
				SumMaximumsX += MaximumsX[l] * Weight;
				SumWeights += Weight;
			}
			if ( SumWeights > 1e-4 )
				SumMaximumsX /= SumWeights;	// Average

			outputPanel.m_MaximumX = SumMaximumsX;

			// Perform least-square fits using the maximum peak as reference
//			float	fLSFRadiusLimit0 = 1.5f * GetLeastSquareRadiusLimit( _Value );
			float	fLSFRadiusLimit0 = 1.5f * (float) SumMaximumsX;
			float	fLSFRadiusLimit1 = 2.5f * fLSFRadiusLimit0;
			float	fLSFRadiusLimit2 = 2.0f * fLSFRadiusLimit1;
			float	fLSFRadiusLimit3 = fRadiusEnd;
			outputPanel.m_RadiusLeastSquareFitLimits[0] = fLSFRadiusLimit0;
			outputPanel.m_RadiusLeastSquareFitLimits[1] = fLSFRadiusLimit1;
			outputPanel.m_RadiusLeastSquareFitLimits[2] = fLSFRadiusLimit2;
			outputPanel.m_RadiusLeastSquareFitLimits[3] = fLSFRadiusLimit3;

			// Adaptative step
			{
				int i=0;
				while ( i < MEASURES_COUNT )
				{
					float	t = (float) i / MEASURES_COUNT;
					float	fRadius = fRadiusStart * (1-t) + fRadiusEnd * t;

					double	x = fRadius;
					double	x2 = x*x;
					double	x3 = x2*x;
					double	x4 = x3*x;
					double	x5 = x4*x;
					double	x6 = x5*x;
					double	x7 = x6*x;
					double	x8 = x7*x;
					double	x9 = x8*x;
					double	x10 = x9*x;

//					int	LSFIndex = fRadius < fLSFRadiusLimit0 ? 0 : (fRadius < fLSFRadiusLimit1 ? 1 : (fRadius < fLSFRadiusLimit2 ? 2 : 3));
					int	LSFIndex = 0;

					double	MaxDy = 0.0;
					for ( int l=0; l < SH_SQORDER; l++ )
					{
						double	y = outputPanel.m_Curves[l,i];
						double	Ny = outputPanel.m_Curves[l,i+1];
						double	Dy = Math.Abs( Ny-y );
						MaxDy = Math.Max( MaxDy, Dy );

						Sumi[LSFIndex,l] += 1.0;
						SumXi[LSFIndex,l] += x;
						SumXi2[LSFIndex,l] += x2;
						SumXi3[LSFIndex,l] += x3;
						SumXi4[LSFIndex,l] += x4;
						SumXi5[LSFIndex,l] += x5;
						SumXi6[LSFIndex,l] += x6;
						SumXi7[LSFIndex,l] += x7;
						SumXi8[LSFIndex,l] += x8;
						SumXi9[LSFIndex,l] += x9;
						SumXi10[LSFIndex,l] += x10;
						SumYi[LSFIndex,l] += y;
						SumYiXi[LSFIndex,l] += x*y;
						SumYiXi2[LSFIndex,l] += x2*y;
						SumYiXi3[LSFIndex,l] += x3*y;
						SumYiXi4[LSFIndex,l] += x4*y;
						SumYiXi5[LSFIndex,l] += x5*y;
					}

					// Compute some step to take based on the curve variation at current position
// 					MaxDy *= 20.0;
// 					i += (int) Math.Ceiling( 1.0 / Math.Max( MaxDy, 20.0 / MEASURES_COUNT ) );
					i++;	// Linear step
				}
			}

			// Finalize least-square fit
			Array.Clear( outputPanel.m_LeastSquareFit, 0, LSF_COUNT*9*4 );

			// Try a polynomial fit of order 6
			double[]	Source = new double[6];
			for ( int i=0; i < Source.Length; i++ )
				Source[i] = 1.0;
			for ( int l=0; l < SH_SQORDER; l++ )
			{
				WMath.Matrix	M = new WMath.Matrix( 6 );

				double	fFactor = 1.0 / SumYiXi5[0,l];
				if ( double.IsInfinity( fFactor ) )
					continue;
				M[0,0] = SumXi10[0,l] * fFactor;
				M[0,1] = SumXi9[0,l] * fFactor;
				M[0,2] = SumXi8[0,l] * fFactor;
				M[0,3] = SumXi7[0,l] * fFactor;
				M[0,4] = SumXi6[0,l] * fFactor;
				M[0,5] = SumXi5[0,l] * fFactor;

				fFactor = 1.0 / SumYiXi4[0,l];
				if ( double.IsInfinity( fFactor ) )
					continue;
				M[1,0] = SumXi9[0,l] * fFactor;
				M[1,1] = SumXi8[0,l] * fFactor;
				M[1,2] = SumXi7[0,l] * fFactor;
				M[1,3] = SumXi6[0,l] * fFactor;
				M[1,4] = SumXi5[0,l] * fFactor;
				M[1,5] = SumXi4[0,l] * fFactor;

				fFactor = 1.0 / SumYiXi3[0,l];
				if ( double.IsInfinity( fFactor ) )
					continue;
				M[2,0] = SumXi8[0,l] * fFactor;
				M[2,1] = SumXi7[0,l] * fFactor;
				M[2,2] = SumXi6[0,l] * fFactor;
				M[2,3] = SumXi5[0,l] * fFactor;
				M[2,4] = SumXi4[0,l] * fFactor;
				M[2,5] = SumXi3[0,l] * fFactor;

				fFactor = 1.0 / SumYiXi2[0,l];
				if ( double.IsInfinity( fFactor ) )
					continue;
				M[3,0] = SumXi7[0,l] * fFactor;
				M[3,1] = SumXi6[0,l] * fFactor;
				M[3,2] = SumXi5[0,l] * fFactor;
				M[3,3] = SumXi4[0,l] * fFactor;
				M[3,4] = SumXi3[0,l] * fFactor;
				M[3,5] = SumXi2[0,l] * fFactor;

				fFactor = 1.0 / SumYiXi[0,l];
				if ( double.IsInfinity( fFactor ) )
					continue;
				M[4,0] = SumXi6[0,l] * fFactor;
				M[4,1] = SumXi5[0,l] * fFactor;
				M[4,2] = SumXi4[0,l] * fFactor;
				M[4,3] = SumXi3[0,l] * fFactor;
				M[4,4] = SumXi2[0,l] * fFactor;
				M[4,5] = SumXi[0,l] * fFactor;

				fFactor = 1.0 / SumYi[0,l];
				if ( double.IsInfinity( fFactor ) )
					continue;
				M[5,0] = SumXi5[0,l] * fFactor;
				M[5,1] = SumXi4[0,l] * fFactor;
				M[5,2] = SumXi3[0,l] * fFactor;
				M[5,3] = SumXi2[0,l] * fFactor;
				M[5,4] = SumXi[0,l] * fFactor;
				M[5,5] = Sumi[0,l] * fFactor;

// 				// Try order 5
// 				WMath.Matrix	M = new WMath.Matrix( 5 );
// 
// 				double	fFactor = 1.0 / SumYiXi4[0,l];
// 				if ( double.IsInfinity( fFactor ) )
// 					continue;
// 				M[0,0] = SumXi8[0,l] * fFactor;
// 				M[0,1] = SumXi7[0,l] * fFactor;
// 				M[0,2] = SumXi6[0,l] * fFactor;
// 				M[0,3] = SumXi5[0,l] * fFactor;
// 				M[0,4] = SumXi4[0,l] * fFactor;
// 
// 				fFactor = 1.0 / SumYiXi3[0,l];
// 				if ( double.IsInfinity( fFactor ) )
// 					continue;
// 				M[1,0] = SumXi7[0,l] * fFactor;
// 				M[1,1] = SumXi6[0,l] * fFactor;
// 				M[1,2] = SumXi5[0,l] * fFactor;
// 				M[1,3] = SumXi4[0,l] * fFactor;
// 				M[1,4] = SumXi3[0,l] * fFactor;
// 
// 				fFactor = 1.0 / SumYiXi2[0,l];
// 				if ( double.IsInfinity( fFactor ) )
// 					continue;
// 				M[2,0] = SumXi6[0,l] * fFactor;
// 				M[2,1] = SumXi5[0,l] * fFactor;
// 				M[2,2] = SumXi4[0,l] * fFactor;
// 				M[2,3] = SumXi3[0,l] * fFactor;
// 				M[2,4] = SumXi2[0,l] * fFactor;
// 
// 				fFactor = 1.0 / SumYiXi[0,l];
// 				if ( double.IsInfinity( fFactor ) )
// 					continue;
// 				M[3,0] = SumXi5[0,l] * fFactor;
// 				M[3,1] = SumXi4[0,l] * fFactor;
// 				M[3,2] = SumXi3[0,l] * fFactor;
// 				M[3,3] = SumXi2[0,l] * fFactor;
// 				M[3,4] = SumXi[0,l] * fFactor;
// 
// 				fFactor = 1.0 / SumYi[0,l];
// 				if ( double.IsInfinity( fFactor ) )
// 					continue;
// 				M[4,0] = SumXi4[0,l] * fFactor;
// 				M[4,1] = SumXi3[0,l] * fFactor;
// 				M[4,2] = SumXi2[0,l] * fFactor;
// 				M[4,3] = SumXi[0,l] * fFactor;
// 				M[4,4] = Sumi[0,l] * fFactor;

				// Solve
				outputPanel.m_LeastSquareFitOrder6[l] = M.Solve( Source );
			}


// 			for ( int LSFIndex=0; LSFIndex < LSF_COUNT; LSFIndex++ )
// 				for ( int l=0; l < SH_SQORDER; l++ )
// 				{
// 					// Use order 2 polynomial
// 					WMath.Matrix3x3	LeastSquareCoeffs = new WMath.Matrix3x3();
// 					double	fFactor = 1.0 / SumYiXi2[LSFIndex,l];
// 					if ( double.IsInfinity( fFactor ) )
// 						continue;
// 					LeastSquareCoeffs.m[0,0] = (float) (SumXi4[LSFIndex,l] * fFactor);
// 					LeastSquareCoeffs.m[0,1] = (float) (SumXi3[LSFIndex,l] * fFactor);
// 					LeastSquareCoeffs.m[0,2] = (float) (SumXi2[LSFIndex,l] * fFactor);
// 					fFactor = 1.0 / SumYiXi[LSFIndex,l];
// 					if ( double.IsInfinity( fFactor ) )
// 						continue;
// 					LeastSquareCoeffs.m[1,0] = (float) (SumXi3[LSFIndex,l] * fFactor);
// 					LeastSquareCoeffs.m[1,1] = (float) (SumXi2[LSFIndex,l] * fFactor);
// 					LeastSquareCoeffs.m[1,2] = (float) (SumXi[LSFIndex,l] * fFactor);
// 					fFactor = 1.0 / SumYi[LSFIndex,l];
// 					if ( double.IsInfinity( fFactor ) )
// 						continue;
// 					LeastSquareCoeffs.m[2,0] = (float) (SumXi2[LSFIndex,l] * fFactor);
// 					LeastSquareCoeffs.m[2,1] = (float) (SumXi[LSFIndex,l] * fFactor);
// 					LeastSquareCoeffs.m[2,2] = (float) (Sumi[LSFIndex,l] * fFactor);
// 
// 					// Invert...
// 					try { LeastSquareCoeffs.Invert(); } catch ( Exception )
// 					{
// 						continue;
// 					}
// 
// 					// Solve
// 					double	a = 0.0;
// 					double	b = LeastSquareCoeffs.GetRow( 0 ).Sum();
// 					double	c = LeastSquareCoeffs.GetRow( 1 ).Sum();
// 					double	d = LeastSquareCoeffs.GetRow( 2 ).Sum();
// 
// 					outputPanel.m_LeastSquareFit[LSFIndex,l,0] = a;
// 					outputPanel.m_LeastSquareFit[LSFIndex,l,1] = b;
// 					outputPanel.m_LeastSquareFit[LSFIndex,l,2] = c;
// 					outputPanel.m_LeastSquareFit[LSFIndex,l,3] = d;
// 
// 					// Use order 3 polynomial
// // 					WMath.Matrix4x4	LeastSquareCoeffs = new WMath.Matrix4x4();
// // 					double	fFactor = 1.0 / SumYiXi3[LSFIndex,l];
// // 					if ( double.IsInfinity( fFactor ) )
// // 						continue;
// // 					LeastSquareCoeffs.m[0,0] = (float) (SumXi6[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[0,1] = (float) (SumXi5[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[0,2] = (float) (SumXi4[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[0,3] = (float) (SumXi3[LSFIndex,l] * fFactor);
// // 					fFactor = 1.0 / SumYiXi2[LSFIndex,l];
// // 					if ( double.IsInfinity( fFactor ) )
// // 						continue;
// // 					LeastSquareCoeffs.m[1,0] = (float) (SumXi5[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[1,1] = (float) (SumXi4[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[1,2] = (float) (SumXi3[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[1,3] = (float) (SumXi2[LSFIndex,l] * fFactor);
// // 					fFactor = 1.0 / SumYiXi[LSFIndex,l];
// // 					if ( double.IsInfinity( fFactor ) )
// // 						continue;
// // 					LeastSquareCoeffs.m[2,0] = (float) (SumXi4[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[2,1] = (float) (SumXi3[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[2,2] = (float) (SumXi2[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[2,3] = (float) (SumXi[LSFIndex,l] * fFactor);
// // 					fFactor = 1.0 / SumYi[LSFIndex,l];
// // 					if ( double.IsInfinity( fFactor ) )
// // 						continue;
// // 					LeastSquareCoeffs.m[3,0] = (float) (SumXi3[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[3,1] = (float) (SumXi2[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[3,2] = (float) (SumXi[LSFIndex,l] * fFactor);
// // 					LeastSquareCoeffs.m[3,3] = (float) (Sumi[LSFIndex,l] * fFactor);
// // 
// // 					// Invert...
// // 					try { LeastSquareCoeffs.Invert(); } catch ( Exception )
// // 					{
// // 						continue;
// // 					}
// // 
// // 					// Solve
// // 					double	a = LeastSquareCoeffs.GetRow( 0 ).Sum();
// // 					double	b = LeastSquareCoeffs.GetRow( 1 ).Sum();
// // 					double	c = LeastSquareCoeffs.GetRow( 2 ).Sum();
// // 					double	d = LeastSquareCoeffs.GetRow( 3 ).Sum();
// // 
// // 					outputPanel.m_LeastSquareFit[LSFIndex,l,0] = a;
// // 					outputPanel.m_LeastSquareFit[LSFIndex,l,1] = b;
// // 					outputPanel.m_LeastSquareFit[LSFIndex,l,2] = c;
// // 					outputPanel.m_LeastSquareFit[LSFIndex,l,3] = d;
// 
// // 					// Finish the interpolation by a simple cubic fit
// // 					// We like the value for LSF at fLSFRadiusLimit0 and we keep the slope at this position too
// // 					// We also know we should end at the end radius (i.e. 500) with a null slope
// // 					double	y0 = d + fLSFRadiusLimit0 * (c + fLSFRadiusLimit0 * (b + fLSFRadiusLimit0 * a));
// // 					double	dy0 = c + fLSFRadiusLimit0 * (2.0 * b + fLSFRadiusLimit0 * 3.0 * a);
// // 					dy0 *= fRadiusEnd - fLSFRadiusLimit0;
// // 					double	y1 = 0.0;
// // 					double	dy1 = 0.0;
// // 
// // 					double	a2 = 2.0 * y0 - 2.0 * y1 + dy0 + dy1;
// // 					double	b2 = 3.0 * y1 - 3.0 * y0 - 2.0 * dy0 - dy1;
// // 					double	c2 = dy0;
// // 					double	d2 = y0;
// // 
// // 					outputPanel.m_LeastSquareFit1[l,0] = a2;
// // 					outputPanel.m_LeastSquareFit1[l,1] = b2;
// // 					outputPanel.m_LeastSquareFit1[l,2] = c2;
// // 					outputPanel.m_LeastSquareFit1[l,3] = d2;
// 				}

			// Draw curves
			outputPanel.UpdateBitmap();

*/

			// Ray-traceing debug
			using ( Graphics G = Graphics.FromImage( outputPanel.m_Bitmap ) )
			{
				int	Width = outputPanel.Width;
				int	Height = outputPanel.Height;

				G.FillRectangle( Brushes.White, 0, 0, Width, Height );
				G.DrawLine( Pens.Black, 0, Height/2, Width, Height/2 );
				G.DrawLine( Pens.Black, Width/2, 0, Width/2, Height );

				foreach ( SphericalHarmonics.SHSamplesCollection.SHSample Sample in m_Samples )
				{
					float	HitDistance = (float) ComputeHitDistance( Sample.m_Direction, _Value * 200.0, 200.0, 200.0, 100.0 );

					float	X = 0.5f*Width + HitDistance * (Sample.m_Direction.x-0.5f*Sample.m_Direction.z);
					float	Y = 0.5f*Height - HitDistance * (Sample.m_Direction.y+0.5f*Sample.m_Direction.x+Sample.m_Direction.z);

					G.DrawRectangle( Pens.Black, X, Y, 2, 2 );
				}
			}
			outputPanel.Refresh();
		}

		protected float		GetLeastSquareRadiusLimit( float _Value )
		{
			int		x0 = (int) Math.Floor( _Value * 100 );
			int		x1 = Math.Min( 100, x0+1 );
			float	t = _Value * 100 - x0;
			double	v0 = m_SingleScatteringMaximums[x0];
			double	v1 = m_SingleScatteringMaximums[x1];
			return (float) (v0 * (1.0 - t) + v1 * t);
		}

		/// <summary>
		/// Computes the SH coefficients for an ellipsoid of specified radii, starting at the given offset
		/// The ellipsoid is looked at from -X in the +X direction the viewpoint can be offset on Y and Z.
		/// </summary>
		/// <param name="_OffsetX">Offset on X (0 is entry point, +1 is exit point)</param>
		/// <param name="_OffsetY">Offset on Y (0 is center, -1 is bottom of ellipsoid and +1 is top)</param>
		/// <param name="_OffsetZ">Offset on Z (0 is center, -1 is left of ellipsoid and +1 is right)</param>
		/// <param name="_Radii">The 3 radii of the ellipsoid</param>
		/// <param name="_SHCoeffs">The array of SH coefficients to compute</param>
		protected void	SingleScatteringWithOffset( float _OffsetX, float _OffsetY, float _OffsetZ, WMath.Vector _Radii, double[] _SHCoeffs )
		{
			Array.Clear( _SHCoeffs, 0, SH_SQORDER );

			// Start somewhere on the -X side of the ellipsoid
			WMath.Point		Position = new WMath.Point( _Radii.x * (2.0f*_OffsetX-1.0f), _OffsetY * _Radii.y, _OffsetZ * _Radii.z );
			WMath.Vector	View = new WMath.Vector( 1.0f, 0.0f, 0.0f );

			// Retrieve entry point
			WMath.Point		C = new WMath.Point( 0, 0, 0 );	// Ellipsoid center always is 0
			WMath.Vector	V = View / _Radii;
			WMath.Vector	D = (Position - C) / _Radii;	// P - C

			// Compute the intersection of that sample's ray with the sphere
			float	a = V | V;
			float	b = D | V;
			float	c = (D | D) - 1.0f;
			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
			{	// No intersection, the offsets must have taken the ray on a path that doesn't hit the ellipsoid...
				_SHCoeffs[0] = 1.0;	// We get all the energy anyway...
				return;				// kthxbye
			}

			float	t0 = (-b - (float) Math.Sqrt( Delta )) / a;
			float	t1 = (-b + (float) Math.Sqrt( Delta )) / a;

			if ( t1 < 0.0f )
			{	// We must have exited the cloud...
				_SHCoeffs[0] = 1.0;	// We get all the energy anyway...
				return;				// kthxbye
			}

			t0 = Math.Max( 0.0f, t0 );	// In case we're inside...
			t0 += 1e-3f;				// Offset a bit so we start inside the ellipsoid

			AccumulateScattering( Position + t0 * View, View, t1-t0, _Radii, _SHCoeffs, 32 );
		}

		/// <summary>
		/// Computes the SH coefficients for an ellipsoid of specified radii, starting at the given offset
		/// The ellipsoid is looked at from -X in the +X direction the viewpoint can be offset on Y and Z.
		/// </summary>
		/// <param name="_OffsetY">Offset on Y (0 is center, -1 is bottom of ellipsoid and +1 is top)</param>
		/// <param name="_OffsetZ">Offset on Z (0 is center, -1 is left of ellipsoid and +1 is right)</param>
		/// <param name="_Radii">The 3 radii of the ellipsoid</param>
		/// <param name="_SHCoeffs">The array of SH coefficients to compute</param>
		protected void	DoubleScatteringWithOffset( float _OffsetY, float _OffsetZ, WMath.Vector _Radii, double[] _SHCoeffs )
		{
			const float OFFSET = 1e-3f;
			const int	STEPS_COUNT = 8;

			Array.Clear( _SHCoeffs, 0, SH_SQORDER );
			double[]	SHCoeffs2 = new double[SH_SQORDER];

			// Start somewhere on the -X side of the ellipsoid
			WMath.Point		Position = new WMath.Point( -2 * _Radii.x, _OffsetY * _Radii.y, _OffsetZ * _Radii.z );
			WMath.Vector	View = new WMath.Vector( 1.0f, 0.0f, 0.0f );

			// Retrieve entry point
			WMath.Point		C = new WMath.Point( 0, 0, 0 );	// Ellipsoid center always is 0
			WMath.Vector	V = View / _Radii;
			WMath.Vector	D = (Position - C) / _Radii;	// P - C

			// Compute the intersection of that sample's ray with the sphere
			float	a = V | V;
			float	b = D | V;
			float	c = (D | D) - 1.0f;
			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
			{	// No intersection, the offsets must have taken the ray on a path that doesn't hit the ellipsoid...
				_SHCoeffs[0] = 1.0;	// We get all the energy anyway...
				return;				// kthxbye
			}

			float	t0 = (-b - (float) Math.Sqrt( Delta )) / a;
			float	t1 = (-b + (float) Math.Sqrt( Delta )) / a;
			t0 += OFFSET;	// Offset a bit so we start inside the ellipsoid

			float	CroppedDistance = (t1-t0) - OFFSET;

			Position += t0 * View;	// March 'till we get at start position...

			WMath.Vector	MarchStep = CroppedDistance / STEPS_COUNT * View;
			Position += OFFSET * View;

			for ( int StepIndex=1; StepIndex <= STEPS_COUNT; StepIndex++ )
			{
				float	t = CroppedDistance * StepIndex / STEPS_COUNT;

				// Compute extinction when light attempts to reach the eye
				double	Extinction = Math.Exp( -EXTINCTION_COEFFICIENT * t ) * EXTINCTION_COEFFICIENT * t;

				// Sample some directions
				D = (Position - C) / _Radii;		// P - C
				for ( int SampleIndex=0; SampleIndex < m_Samples.SamplesCount; SampleIndex++ )
				{
					SphericalHarmonics.SHSamplesCollection.SHSample Sample = m_Samples.Samples[SampleIndex];

					V = Sample.m_Direction / _Radii;

					// Compute the intersection of that sample's ray with the sphere
					a = V | V;
					b = D | V;
					c = (D | D) - 1.0f;
					Delta = b*b - c;
					if ( Delta < 0.0f )
						throw new Exception( "This is inacceptable !" );

					float	s = (-b + (float) Math.Sqrt( Delta )) / a;

					WMath.Point Hit = Position + s * V;
					float	HitDistance = (Hit - Position).Magnitude();

					// Accumulate scattering along this first scattering event direction (2nd scattering order)
					Array.Clear( SHCoeffs2, 0, SH_SQORDER );
					AccumulateScattering( Position, Sample.m_Direction, HitDistance, _Radii, SHCoeffs2, 8 );

					double	Phase = Extinction * m_PhaseFactors[SampleIndex];
					for ( int l=0; l < SH_SQORDER; l++ )
						_SHCoeffs[l] += Phase * SHCoeffs2[l];
				}

				// March
				Position += MarchStep;
			}
		}

		/// <summary>
		/// Accumulates scattering from a position within the ellipsoid along a given view direction and for a given distance
		/// 
		/// This is a correction of the routine below (AccumulateScattering__OLD) that multiplied in-scattering by the total length
		///  of the ray to the exit point instead of multiplying by a constant step...
		/// Anyway, we still get the peak and decrease to 0 as opposed to ray-tracing the cylinder in the new pre-computer...
		/// 
		/// </summary>
		/// <param name="_StartPos"></param>
		/// <param name="_View"></param>
		/// <param name="_Distance"></param>
		/// <param name="_Radii"></param>
		/// <param name="_SHCoeffs"></param>
		/// <param name="_StepsCount"></param>
		protected void	AccumulateScattering( WMath.Point _StartPos, WMath.Vector _View, float _Distance, WMath.Vector _Radii, double[] _SHCoeffs, int _StepsCount )
		{
			const float		OFFSET = 1e-3f;
			float			CroppedDistance = _Distance - OFFSET;
			if ( CroppedDistance <= 0.0f )
				return;

			WMath.Point		C = new WMath.Point( 0, 0, 0 );	// Center of the ellipsoid
			float a, b, c, Delta;

			float			MarchStepLength = CroppedDistance / _StepsCount;
			WMath.Vector	MarchStep = MarchStepLength * _View;
			_StartPos += (OFFSET + MarchStepLength * _StepsCount) * _View;

			// Compute extinction when light attempts to reach the eye
			double	ExtinctionFactor = Math.Exp( -EXTINCTION_COEFFICIENT * MarchStepLength );
			double	InScatteringFactor = EXTINCTION_COEFFICIENT * MarchStepLength;

			for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
			{
				float	t = CroppedDistance * (1.0f-(1.0f+StepIndex) / _StepsCount);

 				// Perform extinction of last accumulated energy
				for ( int l=0; l < SH_SQORDER; l++ )
					_SHCoeffs[l] *= ExtinctionFactor;

				// Sample some directions
				WMath.Vector	D = (_StartPos - C) / _Radii;		// P - C
				for ( int SampleIndex=0; SampleIndex < m_Samples.SamplesCount; SampleIndex++ )
				{
					SphericalHarmonics.SHSamplesCollection.SHSample Sample = m_Samples.Samples[SampleIndex];

					WMath.Vector	V = Sample.m_Direction / _Radii;

// 					// Compute the intersection of that sample's ray with the sphere
// 					a = V | V;
// 					b = D | V;
// 					c = (D | D) - 1.0f;
// 					Delta = b*b - c;
// 					if ( Delta < 0.0f )
// 						continue;
// //						throw new Exception( "This is inacceptable !" );
// 
// 					float	s = (-b + (float) Math.Sqrt( Delta )) / a;
// 
// 					WMath.Point Hit = _StartPos + s * V;
// 					float	HitDistanceFromCenter = (Hit - C).Magnitude();
// 					float	HitDistance = (Hit - _StartPos).Magnitude();

 					// Compute the intersection of that sample's ray with the cylinder
					float	HitDistance = (float) ComputeHitDistance( Sample.m_Direction, t, 2.0 * _Radii.x, 2.0 * _Radii.z, _Radii.z );


					// Compute the attenuation resulting from following that sample
					double	fEnergy = InScatteringFactor * m_PhaseFactors[SampleIndex] * Math.Exp( -EXTINCTION_COEFFICIENT * HitDistance );

					// Accumulate SH coefficients
					for ( int l=0; l < SH_SQORDER; l++ )
						_SHCoeffs[l] += fEnergy * Sample.m_SHFactors[l];
				}

				// March
				_StartPos -= MarchStep;
			}
		}

		/// <summary>
		/// Accumulates scattering from a position within the ellipsoid along a given view direction and for a given distance
		/// </summary>
		/// <param name="_StartPos"></param>
		/// <param name="_View"></param>
		/// <param name="_Distance"></param>
		/// <param name="_Radii"></param>
		/// <param name="_SHCoeffs"></param>
		/// <param name="_StepsCount"></param>
		protected void	AccumulateScattering__OLD( WMath.Point _StartPos, WMath.Vector _View, float _Distance, WMath.Vector _Radii, double[] _SHCoeffs, int _StepsCount )
		{
			const float		OFFSET = 1e-3f;
			float			CroppedDistance = _Distance - OFFSET;
			if ( CroppedDistance <= 0.0f )
				return;

			WMath.Point		C = new WMath.Point( 0, 0, 0 );	// Center of the ellipsoid
			float a, b, c, Delta;

			WMath.Vector	MarchStep = CroppedDistance / _StepsCount * _View;
			_StartPos += OFFSET * _View;

			for ( int StepIndex=1; StepIndex <= _StepsCount; StepIndex++ )
			{
				float	t = CroppedDistance * StepIndex / _StepsCount;

				// Compute extinction when light attempts to reach the eye
				double	Extinction = Math.Exp( -EXTINCTION_COEFFICIENT * t ) * EXTINCTION_COEFFICIENT * t;

				// Sample some directions
				WMath.Vector	D = (_StartPos - C) / _Radii;		// P - C
				for ( int SampleIndex=0; SampleIndex < m_Samples.SamplesCount; SampleIndex++ )
				{
					SphericalHarmonics.SHSamplesCollection.SHSample Sample = m_Samples.Samples[SampleIndex];

					WMath.Vector	V = Sample.m_Direction / _Radii;

					// Compute the intersection of that sample's ray with the sphere
					a = V | V;
					b = D | V;
					c = (D | D) - 1.0f;
					Delta = b*b - c;
					if ( Delta < 0.0f )
						continue;
//						throw new Exception( "This is inacceptable !" );

					float	s = (-b + (float) Math.Sqrt( Delta )) / a;

					WMath.Point Hit = _StartPos + s * V;
					float	HitDistanceFromCenter = (Hit - C).Magnitude();
					float	HitDistance = (Hit - _StartPos).Magnitude();

					// Compute the attenuation resulting from following that sample
					double	fEnergy = Extinction * m_PhaseFactors[SampleIndex] * Math.Exp( -EXTINCTION_COEFFICIENT * HitDistance );

					// Accumulate SH coefficients
					for ( int l=0; l < SH_SQORDER; l++ )
						_SHCoeffs[l] += fEnergy * Sample.m_SHFactors[l];
				}

				// March
				_StartPos += MarchStep;
			}
		}


		/// <summary>
		/// Computes the distance at which the ray hits the slab given the slab's dimensions and the viewer's position and view vector
		/// The "slab" is actually a piece of cylinder aligned with the X axis : the "thickness" represents the diameter of the cylinder
		/// Thus, the ray can either exit through the front or back faces of the cylinder, or through the cylinder's sides
		/// </summary>
		/// <param name="_Direction">NORMALIZED</param>
		/// <param name="X">in [0,L]</param>
		/// <param name="L"></param>
		/// <param name="T"></param>
		/// <param name="H">in [0,T]</param>
		/// <returns></returns>
		protected double	ComputeHitDistance( WMath.Vector _Direction, double X, double L, double T, double H )
		{
			// Compute front (or back) hit
			double	HitFront = Math.Abs( _Direction.x ) > 1e-6 ? ((_Direction.x > 0.0f ? (L-X) : -X) / _Direction.x) : double.PositiveInfinity;

			// Compute side hit
			double	R = 0.5*T;	// Radius is half thickness
			double	C = R;		// Center is at half the diameter above
			double	P = H - C;
			double	c = P*P - R*R;												// P.P - R²
			double	b = P*_Direction.y;											// P.V
			double	a = _Direction.y*_Direction.y+_Direction.z*_Direction.z;	// V.V
			if ( Math.Abs( a ) < 1e-6 )
				return HitFront;	// Direction is aligned with slab's axis : no possible side hit...
			double	Delta = b*b-a*c;
			if ( Delta < 0.0 )
				throw new Exception( "This is inacceptable !" );	// Shouldn't happen as we're INSIDE the slab at all times
//				return HitFront;	// No side hit !

			double	HitSide = (-b+Math.Sqrt( Delta )) / a;	// Got our hit !

// 			// DEBUG CHECK
// 			double	HitY = P + HitSide * _Direction.y;
// 			double	HitZ = 0 + HitSide * _Direction.z;
// 			double	HitRadius = Math.Sqrt( HitY*HitY+HitZ*HitZ );	// Should be equal to R
// 			// DEBUG CHECK

			return Math.Max( 0.0, Math.Min( HitSide, HitFront ) );
		}

		#endregion

		#region EVENT HANDLERS

		protected int m_OffsetXIndex = 0;
		private void Form1_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			switch ( e.KeyCode )
			{
				case Keys.NumPad8:
					outputPanel.m_ScaleY += 0.01f;
 					outputPanel.UpdateBitmap2();
					break;
				case Keys.NumPad2:
					outputPanel.m_ScaleY -= 0.01f;
 					outputPanel.UpdateBitmap2();
					break;

				case Keys.NumPad4:
					m_OffsetXIndex = (m_OffsetXIndex - 1) & 0x3F;
					CurvesComparisonTest( m_OffsetXIndex );
					break;
				case Keys.NumPad6:
					m_OffsetXIndex = (m_OffsetXIndex + 1) & 0x3F;
					CurvesComparisonTest( m_OffsetXIndex );
					break;

				case Keys.Add:
// 					m_Value += 0.05f;
// 					ScatteringTest2D( m_Value, 1.0f );
					outputPanel.m_ComparisonSHCoeffIndex = (outputPanel.m_ComparisonSHCoeffIndex + 1) % SH_SQORDER;
					outputPanel.UpdateBitmap2();
					break;

				case Keys.Subtract:
// 					m_Value -= 0.05f;
// 					ScatteringTest2D( m_Value, 1.0f );
					outputPanel.m_ComparisonSHCoeffIndex = (outputPanel.m_ComparisonSHCoeffIndex + SH_SQORDER - 1) % SH_SQORDER;
					outputPanel.UpdateBitmap2();
					break;

				case Keys.NumPad0:
					outputPanel.m_ComparisonShow = 1 + (outputPanel.m_ComparisonShow % 3);
					outputPanel.UpdateBitmap2();
					break;

				case Keys.Enter:
					outputPanel.m_bDrawLeastSquareFit = !outputPanel.m_bDrawLeastSquareFit;
 					outputPanel.UpdateBitmap2();
					break;
			}
		}

		#endregion
	}
}
