using System;
using System.Collections.Generic;
using System.Text;

using WMath;

namespace Atmospheric
{
	/// <summary>
	/// This class hosts an adaptation of the article "A Practical Analytic Model for Daylight" by Preetham et al.
	/// It is capable of returning the color of a natural sky for a set of directions expressed in spherical coordinates.
	/// It's also capable of computing the associated extinction & in-scattering tables (without the turbidity) that
	///  can be used for computation of aerial perspective.
	/// 
	/// Phi is the azimuth and Theta is the elevation where 0 is the dome's zenith.
	/// 
	/// The convention for transformation between spherical and cartesian coordinates should be chosen to be the same
	///	as for Spherical Harmonics (cf. SphericalHarmonics.SHFunctions) :
	///
	///		_ Azimuth Phi is zero on +Z and increases CW (i.e. PI/2 at -X, PI at -Z and 3PI/2 at +X)
	///		_ Elevation Theta is zero on +Y and PI on -Y
	/// 
	/// 
	///                     Y Theta=0
	///                     |
	///                     |
	///                     |
	///  Phi=PI/2 -X........o------+X Phi=3PI/2
	///                    /
	///                   / 
	///                 +Z Phi=0
	/// 
	/// 
	/// Here is the sample conversion from spherical to cartesian coordinates :
	///		X = Sin( Theta ) * Sin( -Phi )
	///		Y = Cos( Theta )
	///		Z = Sin( Theta ) * Cos( -Phi )
	/// 
	/// So cartesian to polar coordinates is computed this way:
	///		Theta = acos( Y );
	///		Phi = -atan2( X, Z );
	/// 
	/// </summary>
	public class SkyDome
	{
		#region CONSTANTS

		// Night & Sun horizon settings
		protected const float		BELOW_HORIZON_ANGLE					= 1.7308109f;
		protected const float		NIGHT_SKY_AMBIENT_LUMINANCE			= 500.0f;

		protected const double		TABLES_SPECTRUM_LAMBDA_MIN			= 380.0e-9;
		protected const double 		TABLES_SPECTRUM_LAMBDA_MAX			= 825.0e-9;
		protected const int			TABLES_SPECTRUM_SAMPLES_COUNT		= 100;
		protected const int			TABLES_PHASE_SAMPLING_RESOLUTION	= 512;

		#endregion

		#region NESTED TYPES

		[System.Diagnostics.DebuggerDisplay( "Direction={Direction} Color={Color}" )]
		public class	SkyDomePositionCell
		{
			#region FIELDS

			protected Vector2D		m_Direction = null;
			protected Vector		m_Color = new WMath.Vector();

			#endregion

			#region PROPERTIES

			public Vector2D		Direction
			{
				get { return m_Direction; }
			}

			public Vector		Color
			{
				get { return m_Color; }
				set { m_Color = value; }
			}

			#endregion

			#region METHODS

			/// <summary>
			/// Builds a sampling direction using a 2D vector
			/// </summary>
			/// <param name="_Direction">The 2D vector hosting [Phi,Theta] (cf. SkyDome class for infos on direction's convention)</param>
			public	SkyDomePositionCell( Vector2D _Direction )
			{
				m_Direction = _Direction;
			}

			/// <summary>
			/// Builds the collection of skydome positions from a collection of SH Samples
			/// </summary>
			/// <param name="_SHSamplesCollection"></param>
			public static SkyDomePositionCell[]	BuildFromSHSamplesCollection( SphericalHarmonics.SHSamplesCollection _SHSamplesCollection )
			{
				SkyDomePositionCell[]	SkyDomeSamples = new SkyDomePositionCell[_SHSamplesCollection.SamplesCount];
				for ( int SampleIndex=0; SampleIndex < _SHSamplesCollection.SamplesCount; SampleIndex++ )
				{
					SphericalHarmonics.SHSamplesCollection.SHSample	SHSample = _SHSamplesCollection.Samples[SampleIndex];
					SkyDomeSamples[SampleIndex] = new SkyDomePositionCell( new WMath.Vector2D( SHSample.m_Phi, SHSample.m_Theta ) );
				}

				return	SkyDomeSamples;
			}

			#endregion
		};

		#endregion

		#region FIELDS

		protected bool						m_bHDR = true;

		protected SkyDomePositionCell[]		m_SkyDomePositions = null;

		// Result data available after a computation
		protected double					m_SkyLuminanceMin = double.MaxValue;
		protected double					m_SkyLuminanceMax = 0.0;
		protected double					m_SkyLuminanceAverage = 0.0;
		protected Vector					m_AmbientSkyColor = new Vector();
			// Input values
		protected Vector2D					m_SunDirection = new Vector2D();
		protected Vector					m_SunColor = new Vector();
		protected float						m_Turbidity = 2.0f;

		// Temporary precomputed in-scattering data
		protected Vector					m_SigmaMolecules = null;
		protected Vector					m_SigmaHaze = null;
		protected Vector[]					m_AngularSigmaMolecules = new Vector[TABLES_PHASE_SAMPLING_RESOLUTION+1];
		protected Vector[]					m_AngularSigmaHaze = new Vector[TABLES_PHASE_SAMPLING_RESOLUTION+1];

		#endregion

		#region PROPERTIES

		/// <summary>
		/// The positions the skydome was initialized with
		/// </summary>
		public SkyDomePositionCell[]	SkyDomePositions
		{
			get { return m_SkyDomePositions; }
		}

		/// <summary>
		/// The direction of the sun in spherical coordinates that was input for the last computation
		/// </summary>
		public Vector2D					SunDirectionSpherical
		{
			get { return m_SunDirection; }
		}

		/// <summary>
		/// The direction of the Sun in cartesian coordinates
		/// NOTE: This gives the vector pointing TO the Sun
		/// </summary>
		public Vector					SunDirection
		{
			get
			{
				float	fSunPhi = m_SunDirection.x;
				float	fSunTheta = m_SunDirection.y;

				return new WMath.Vector( (float) (Math.Sin( -fSunPhi ) * Math.Sin( fSunTheta )), (float) Math.Cos( fSunTheta ), (float) (Math.Cos( -fSunPhi ) * Math.Sin( fSunTheta )) );
			}
		}

		/// <summary>
		/// The color of the sun that was input for the last computation
		/// </summary>
		public Vector					SunColor
		{
			get { return m_SunColor; }
			set { m_SunColor = value; }
		}

		/// <summary>
		/// The turbidity that was input for the last computation
		/// </summary>
		public float					Turbidity
		{
			get { return m_Turbidity; }
		}


		/// <summary>
		/// The resulting minimum sky luminance
		/// </summary>
		public double					SkyLuminanceMin
		{
			get { return m_SkyLuminanceMin; }
		}

		/// <summary>
		/// The resulting maximum sky luminance
		/// </summary>
		public double					SkyLuminanceMax
		{
			get { return m_SkyLuminanceMax; }
		}

		/// <summary>
		/// The resulting average sky luminance
		/// </summary>
		public double					SkyLuminanceAverage
		{
			get { return m_SkyLuminanceAverage; }
		}

		/// <summary>
		/// The resulting ambient sky color
		/// </summary>
		public Vector					AmbientSkyColor
		{
			get { return m_AmbientSkyColor; }
		}

		#endregion

		#region METHODS

		public	SkyDome( bool _HDR )
		{
			m_bHDR = _HDR;
		}

		public void		Initialize( SkyDomePositionCell[] _SkyDomePositions )
		{
			m_SkyDomePositions = _SkyDomePositions;

			PreComputeInScatteringTables();
		}

		/// <summary>
		/// Computes the sky dome color given Sun position & color
		/// </summary>
		/// <param name="_SunDirection">The position of the Sun in spherical coordinates (i.e. Phi, Theta)</param>
		/// <param name="_Turbidity">The atomspheric turbidity</param>
		/// <remarks>The resulting sky dome color will be computed and stored in the array of SkyDomePositionCells provided by the Initialize method</remarks>
		public void		ComputeSkyDomeColor( Vector2D _SunDirection, float _Turbidity )
		{
			// Keep track of the Sun's direction & turbidity
			m_SunDirection = _SunDirection;
			m_Turbidity = _Turbidity;

			ComputeSkyDomeColor( m_SkyDomePositions, _SunDirection, _Turbidity );
		}

		/// <summary>
		/// Computes the sky dome color given Sun position & color
		/// </summary>
		/// <param name="_SkyDomePositions">The array of skydome positions to compute sky colors for</param>
		/// <param name="_SunDirection">The position of the Sun in spherical coordinates (i.e. Phi, Theta)</param>
		/// <param name="_Turbidity">The atomspheric turbidity</param>
		public void		ComputeSkyDomeColor( SkyDomePositionCell[] _SkyDomePositions, Vector2D _SunDirection, float _Turbidity )
		{
			// Horizon clamping
			float	fSunTheta = System.Math.Min( BELOW_HORIZON_ANGLE, System.Math.Max( -BELOW_HORIZON_ANGLE, _SunDirection.y ) );	// Here, we clamp theta so we can't reach opposite sky values which are buggy (out of sampling range)

			// The Perez functions' coefficients
			float[]		Coefficients_x = new float[] {	-0.0193f * _Turbidity - 0.2592f,
														-0.0665f * _Turbidity + 0.0008f,
														-0.0004f * _Turbidity + 0.2125f,
														-0.0641f * _Turbidity - 0.8989f,
														-0.0033f * _Turbidity + 0.0452f };
			float[]		Coefficients_y = new float[] {	-0.0167f * _Turbidity - 0.2608f,
														-0.0950f * _Turbidity + 0.0092f,
														-0.0079f * _Turbidity + 0.2102f,
														-0.0441f * _Turbidity - 1.6537f,
														-0.0109f * _Turbidity + 0.0529f };
			float[]		Coefficients_Y = new float[] {	+0.1787f * _Turbidity - 1.4630f,
														-0.3554f * _Turbidity + 0.4275f,
														-0.0227f * _Turbidity + 5.3251f,
														+0.1206f * _Turbidity - 2.5771f,
														-0.0670f * _Turbidity + 0.3703f };

			float[,]	SKY_ZENITH_x = new float[3,4] {	{ +0.00165f, -0.00374f,  0.00208f,  0.00000f },
														{ -0.02902f,  0.06377f, -0.03202f,  0.00394f },
														{ +0.11693f, -0.21196f,  0.06052f,  0.25885f } };

			float[,]	SKY_ZENITH_y = new float[3,4] {	{ +0.00275f, -0.00610f,  0.00316f,  0.00000f },
														{ -0.04214f,  0.08970f, -0.04153f,  0.00515f },
														{ +0.15346f, -0.26756f,  0.06669f,  0.26688f } };

			// Compute the sky's zenith values
			double		Chi = (4.0 / 9.0 - _Turbidity / 120.0) * (System.Math.PI - 2.0 * fSunTheta);
			double[]	ZenithConstants = new double[]	{				 ( (SKY_ZENITH_x[2,3] + fSunTheta * (SKY_ZENITH_x[2,2] + fSunTheta * (SKY_ZENITH_x[2,1] + fSunTheta * SKY_ZENITH_x[2,0]))) +
															_Turbidity * ( (SKY_ZENITH_x[1,3] + fSunTheta * (SKY_ZENITH_x[1,2] + fSunTheta * (SKY_ZENITH_x[1,1] + fSunTheta * SKY_ZENITH_x[1,0]))) +
															_Turbidity *   (SKY_ZENITH_x[0,3] + fSunTheta * (SKY_ZENITH_x[0,2] + fSunTheta * (SKY_ZENITH_x[0,1] + fSunTheta * SKY_ZENITH_x[0,0]))) ) ),

																		 ( (SKY_ZENITH_y[2,3] + fSunTheta * (SKY_ZENITH_y[2,2] + fSunTheta * (SKY_ZENITH_y[2,1] + fSunTheta * SKY_ZENITH_y[2,0]))) +
															_Turbidity * ( (SKY_ZENITH_y[1,3] + fSunTheta * (SKY_ZENITH_y[1,2] + fSunTheta * (SKY_ZENITH_y[1,1] + fSunTheta * SKY_ZENITH_y[1,0]))) +
															_Turbidity *   (SKY_ZENITH_y[0,3] + fSunTheta * (SKY_ZENITH_y[0,2] + fSunTheta * (SKY_ZENITH_y[0,1] + fSunTheta * SKY_ZENITH_y[0,0]))) ) ),

															1000.0f * ((4.0453f * _Turbidity - 4.9710f) * System.Math.Tan( Chi ) - 0.2155f * _Turbidity + 2.4192f)	// in cd/m²
														};

			// Compute zenith Perez values
			Vector	Zenith = new Vector(	(float) (ZenithConstants[0] / Perez( Coefficients_x, 0.0f, fSunTheta )),
											(float) (ZenithConstants[1] / Perez( Coefficients_y, 0.0f, fSunTheta )),
											(float) (ZenithConstants[2] / Perez( Coefficients_Y, 0.0f, fSunTheta )) );

			// Reset sky dome data
			m_SkyLuminanceMax = 0.0f;
			m_SkyLuminanceMin = float.MaxValue;
			m_SkyLuminanceAverage = 0.0f;
			m_AmbientSkyColor.Zero();

			// Update sky dome colors
			Vector		SkyColorxyY = new Vector();

 			float		fSlopeCrossingAngle = .25f * (float) System.Math.PI;
// 			float		a = -fSlopeCrossingAngle * fSlopeCrossingAngle;
// 			float		b = fSlopeCrossingAngle + System.Math.Log( 0.5f );
// 			float		c = fSlopeCrossingAngle - 0.25f;
// 			float		fDelta = b * b - 4.0f * a * c;
// 						fDelta = System.Math.Sqrt( fDelta );
// 			float		fLuminanceWeightFactor0 = .5f * (-b - fDelta) / a;
// 			float		fLuminanceWeightFactor1 = .5f * (-b + fDelta) / a;
// 			float		fLuminanceWeightFactor = System.Math.Max( fLuminanceWeightFactor0, fLuminanceWeightFactor1 );
			float		fLuminanceWeightFactor = -(float) System.Math.Log( 0.05 ) / (fSlopeCrossingAngle * fSlopeCrossingAngle);

			float		fAverageLuminanceWeightsSum = 0.0f;
			foreach ( SkyDomePositionCell PositionCell in _SkyDomePositions )
			{
				// Compute phase between vertex and Sun
				float	fGamma = ComputePhase( PositionCell.Direction.y, PositionCell.Direction.x, fSunTheta, _SunDirection.x );

				// Compute sky color in the direction of the vertex
				SkyColorxyY.x = (float) (Perez( Coefficients_x, PositionCell.Direction.y, fGamma ) * Zenith.x);
				SkyColorxyY.y = (float) (Perez( Coefficients_y, PositionCell.Direction.y, fGamma ) * Zenith.y);
				SkyColorxyY.z = (float) (Perez( Coefficients_Y, PositionCell.Direction.y, fGamma ) * Zenith.z);
				if ( SkyColorxyY.z < 0.0f )
					throw new Exception( "Watch your parameters! Don't use too low values for turbidity or too high values at \"night\"." );

				// Update color
				PositionCell.Color = Colorimetry.XYZ2RGB( new Vector(	SkyColorxyY.x * SkyColorxyY.z / SkyColorxyY.y,
																		SkyColorxyY.z,
																		(1.0f - SkyColorxyY.x - SkyColorxyY.y) * SkyColorxyY.z / SkyColorxyY.y ) );

				// Update sky luminance data
				float		fSkyLuminance = 0.212643862f * PositionCell.Color.x + 0.715135574f * PositionCell.Color.y + 0.0721568465f * PositionCell.Color.z;		// We extract the 2nd column of the inverted XYZ_TO_RGB matrix to recompute Y from an RGB color

					// Compute luminance weight based on phase with Sun
//				float		fLuminanceWeight = 1.0f - System.Math.Exp( -fLuminanceWeightFactor * fGamma * fGamma );
				float		fLuminanceWeight = 1.0f;		// Always 1 so there is no weighting

				m_SkyLuminanceMax = System.Math.Max( m_SkyLuminanceMax, fSkyLuminance );
				m_SkyLuminanceMin = System.Math.Min( m_SkyLuminanceMin, fSkyLuminance );
				m_SkyLuminanceAverage += fSkyLuminance * fLuminanceWeight;
				m_AmbientSkyColor += fLuminanceWeight * PositionCell.Color;

				fAverageLuminanceWeightsSum += fLuminanceWeight;
			}

			// Finalize luminance data
			m_SkyLuminanceAverage /= fAverageLuminanceWeightsSum;
			m_AmbientSkyColor /= fAverageLuminanceWeightsSum;

			// Special night sky ambient color
			if ( System.Math.Abs( _SunDirection.y ) > BELOW_HORIZON_ANGLE )
			{
				Vector	NightSkyColor = new Vector( 13.0f, 21.0f, 38.0f );
				float	fNightSkyColorLuminance = 0.212643862f * NightSkyColor.x + 0.715135574f * NightSkyColor.y + 0.0721568465f * NightSkyColor.z;
				m_AmbientSkyColor = NightSkyColor * NIGHT_SKY_AMBIENT_LUMINANCE / fNightSkyColorLuminance;
				m_SkyLuminanceAverage = NIGHT_SKY_AMBIENT_LUMINANCE;
			}

			// Non-HDR rendering
			if ( !m_bHDR )
			{
				// Divide by the average luminance
				foreach ( SkyDomePositionCell PositionCell in m_SkyDomePositions )
					PositionCell.Color /= (float) m_SkyLuminanceAverage;

				// Divide sky & sun color
				m_AmbientSkyColor /= (float) m_SkyLuminanceAverage;
				m_SunColor /= (float) m_SkyLuminanceAverage;
			}
		}

		/// <summary>
		/// Applies in-scattering & extinction from the Sun
		/// </summary>
		/// <param name="_SunDirection">The position of the Sun in spherical coordinates (i.e. Phi, Theta)</param>
		/// <param name="_SunColor">The color of the Sun</param>
		/// <param name="_Turbidity">The atomspheric turbidity</param>
		/// <param name="_fAtmosphereHorizonDistance">The distance to the sky dome at horizon</param>
		/// <param name="_fAtmosphereZenithDistance">The distance to the sky dome at zenith</param>
		/// <remarks>The resulting sky dome color will be computed and stored in the array of SkyDomePositionCells provided by the Initialize method</remarks>
		public void		ApplyInScatteringAndExtinction( Vector2D _SunDirection, Vector _SunColor, float _Turbidity, float _fAtmosphereHorizonDistance, float _fAtmosphereZenithDistance )
		{
			// Keep track of Sun's color
			m_SunColor = new Vector( _SunColor );

			// Blend Sun color with black if below horizon
			if ( _SunDirection.y > BELOW_HORIZON_ANGLE && _SunDirection.y < 0.5f * (float) System.Math.PI )
				m_SunColor *= System.Math.Min( 1.0f, 1.0f - (System.Math.Abs( _SunDirection.y ) - (.5f * (float) System.Math.PI)) / (BELOW_HORIZON_ANGLE - (.5f * (float) System.Math.PI)) );
			else if ( _SunDirection.y >= 0.5f * (float) System.Math.PI )
				m_SunColor.Zero();	// Full black Sun...

			ApplyInScatteringAndExtinction( m_SkyDomePositions, _SunDirection, m_SunColor, _Turbidity, _fAtmosphereHorizonDistance, _fAtmosphereZenithDistance );
		}

		/// <summary>
		/// Applies in-scattering & extinction from the Sun
		/// </summary>
		/// <param name="_SkyDomePositions">The array of skydome positions to compute sky colors for</param>
		/// <param name="_SunDirection">The position of the Sun in spherical coordinates (i.e. Phi, Theta)</param>
		/// <param name="_SunColor">The color of the Sun</param>
		/// <param name="_Turbidity">The atomspheric turbidity</param>
		/// <param name="_fAtmosphereHorizonDistance">The distance to the sky dome at horizon</param>
		/// <param name="_fAtmosphereZenithDistance">The distance to the sky dome at zenith</param>
		public void		ApplyInScatteringAndExtinction( SkyDomePositionCell[] _SkyDomePositions, Vector2D _SunDirection, Vector _SunColor, float _Turbidity, float _fAtmosphereHorizonDistance, float _fAtmosphereZenithDistance )
		{
			// Horizon clamping
			float	fSunTheta = System.Math.Min( BELOW_HORIZON_ANGLE, System.Math.Max( -BELOW_HORIZON_ANGLE, _SunDirection.y ) );	// Here, we clamp theta so we can't reach opposite sky values which are buggy (out of sampling range)

			// Compute extinction & in-scattering factors
			float	fHazeTurbidityFactor = 0.6544204545455f * _Turbidity - 0.6509886363636f;

			Vector	ExtinctionFactor = m_SigmaMolecules + fHazeTurbidityFactor * m_SigmaHaze;
			Vector	InScatteringFactor = new Vector( _SunColor.x / ExtinctionFactor.x,
													 _SunColor.y / ExtinctionFactor.y,
													 _SunColor.z / ExtinctionFactor.z );

			// Reset sky dome data
			m_SkyLuminanceMax = 0.0f;
			m_SkyLuminanceMin = float.MaxValue;
			m_SkyLuminanceAverage = 0.0f;
			m_AmbientSkyColor.Zero();

			float		fAverageLuminanceWeightsSum = 0.0f;
			foreach ( SkyDomePositionCell PositionCell in _SkyDomePositions )
			{
				// Compute phase between vertex and Sun
				float		fGamma = ComputePhase( PositionCell.Direction.y, PositionCell.Direction.x, fSunTheta, _SunDirection.x );

				// Compute in-scattering from the Sun
				float		fDistance2Atmosphere = (float) (_fAtmosphereZenithDistance + PositionCell.Direction.y * (_fAtmosphereHorizonDistance - _fAtmosphereZenithDistance) / (.5f * System.Math.PI));
				int			PhaseIndex = (int) System.Math.Floor( fGamma * TABLES_PHASE_SAMPLING_RESOLUTION / System.Math.PI );
				Vector		AngularSigmaMolecules = m_AngularSigmaMolecules[PhaseIndex];
				Vector		AngularSigmaHaze = m_AngularSigmaHaze[PhaseIndex];

				Vector		TotalExtinction = new Vector(	1.0f - (float) System.Math.Exp( -ExtinctionFactor.x * fDistance2Atmosphere ),
															1.0f - (float) System.Math.Exp( -ExtinctionFactor.y * fDistance2Atmosphere ),
															1.0f - (float) System.Math.Exp( -ExtinctionFactor.z * fDistance2Atmosphere ) );

				Vector		InScattering = new Vector(	(AngularSigmaMolecules.x + fHazeTurbidityFactor * AngularSigmaHaze.x) * TotalExtinction.x * InScatteringFactor.x,
														(AngularSigmaMolecules.y + fHazeTurbidityFactor * AngularSigmaHaze.y) * TotalExtinction.y * InScatteringFactor.y,
														(AngularSigmaMolecules.z + fHazeTurbidityFactor * AngularSigmaHaze.z) * TotalExtinction.z * InScatteringFactor.z );

				// Accumulate in-scattering to original sky color
				PositionCell.Color += InScattering;

				// Blend if sun is below horizon
				PositionCell.Color *= (float) (System.Math.Max( 0.0f, System.Math.Min( 1.0f, 1.0f - (System.Math.Abs( fSunTheta ) - (.5f * System.Math.PI)) / (BELOW_HORIZON_ANGLE - (.5f * System.Math.PI)) ) ));

				// Update sky luminance data
				float		fSkyLuminance = 0.212643862f * PositionCell.Color.x + 0.715135574f * PositionCell.Color.y + 0.0721568465f * PositionCell.Color.z;		// We extract the 2nd column of the inverted XYZ_TO_RGB matrix to recompute Y from an RGB color

				m_SkyLuminanceMax = System.Math.Max( m_SkyLuminanceMax, fSkyLuminance );
				m_SkyLuminanceMin = System.Math.Min( m_SkyLuminanceMin, fSkyLuminance );
				m_SkyLuminanceAverage += fSkyLuminance;
				m_AmbientSkyColor += PositionCell.Color;

				fAverageLuminanceWeightsSum += 1.0f;
			}

			// Finalize luminance data
			m_SkyLuminanceAverage /= fAverageLuminanceWeightsSum;
			m_AmbientSkyColor /= fAverageLuminanceWeightsSum;
		}

		/// <summary>
		/// Computes the sky dome coefficients given a collection of SH samples and their associated skydome positions
		/// </summary>
		/// <param name="_SHSamples">The collection of SH Samples</param>
		/// <param name="_SkyDomePositions">The associated skydome positions in the same direction as every SH sample.
		/// NOTE: These positions must already have their color computed using ComputeSkyDomeColor() when calling this method!</param>
		/// <param name="_MinLuminance">The minimum luminance (any sky luminance below this value will be brought back to that value) (leave it to 0 if you're not sure)</param>
		/// <returns>The resulting array of SH Coefficients</returns>
		public WMath.Vector[]	BuildSkyDomeSHCoefficients( SphericalHarmonics.SHSamplesCollection _SHSamples, SkyDomePositionCell[] _SkyDomePositions, float _MinLuminance )
		{
			if ( _SHSamples.SamplesCount != _SkyDomePositions.Length )
				throw new Exception( "SH samples and sky dome positions count mismatch!" );

			Vector[]	SHCoefficients = new Vector[_SHSamples.Order * _SHSamples.Order];
			for ( int i=0; i < SHCoefficients.Length; i++ )
				SHCoefficients[i] = new Vector();

			// Transform colors into SH
			for ( int SampleIndex=0; SampleIndex < _SHSamples.SamplesCount; SampleIndex++ )
			{
				SphericalHarmonics.SHSamplesCollection.SHSample	Sample = _SHSamples.Samples[SampleIndex];
				SkyDomePositionCell								SkyDomePosition = _SkyDomePositions[SampleIndex];

				WMath.Vector	Color = SkyDomePosition.Color;
				float			fColorLuminance = Color.x * 0.3f + Color.y * 0.5f + Color.z * 0.2f;
				if ( fColorLuminance < _MinLuminance )
					Color = _MinLuminance / fColorLuminance * m_AmbientSkyColor;	// Maximize...

				// Retrieve sky's color for current position
				for ( int i=0; i < SHCoefficients.Length; i++ )
					SHCoefficients[i] += Color * (float) Sample.m_SHFactors[i];
			}

			// Normalize
			float	fNormalizer = 4.0f * (float) Math.PI / _SHSamples.SamplesCount;
			for ( int i=0; i < SHCoefficients.Length; i++ )
				SHCoefficients[i] *= fNormalizer;

			return	SHCoefficients;
		}

		/// <summary>
		/// Computes the sky dome SH coefficients given the Sun position
		/// Borrowed from http://www.cg.tuwien.ac.at/research/publications/2008/Habel_08_SSH/
		/// </summary>
		/// <param name="_SunDirection">The direction of the Sun in spherical coordinates (i.e. Phi, Theta)</param>
		/// <param name="_Turbidity">The atmospheric turbidity</param>
		/// <param name="_SHOrder">The SH order (up to 7!)</param>
		/// <param name="_fScale">The scale factor to apply to the SH coefficients</param>
		/// <returns>The resulting array of SH Coefficients</returns>
		public WMath.Vector[]	BuildSkyDomeSHCoefficients( Vector2D _SunDirection, float _Turbidity, int _SHOrder, float _fScale )
		{
			float	fPhi = _SunDirection.x;
			float	fTheta = Math.Min( 0.45f * (float) Math.PI, _SunDirection.y );

			Vector[]	SHCoefficients = new Vector[_SHOrder * _SHOrder];
			for ( int i=0; i < SHCoefficients.Length; i++ )
				SHCoefficients[i] = new Vector();

 			// Generate the parameter matrix
			double[,]	Matrix = new double[14,8];
			double[]	ThetaPow = new double[14];
			double[]	TurbidityPow = new double[8];

			ThetaPow[0] = Matrix[0,0] = 1.0;
			ThetaPow[1] = Matrix[1,0] = fTheta;
			for ( int i=2; i < 14; ++i )
			{
				ThetaPow[i] = ThetaPow[i-1] * fTheta;
				Matrix[i,0] = ThetaPow[i];
			}

			TurbidityPow[0] = 1.0;
			TurbidityPow[1] = Matrix[0,1] = _Turbidity;
			for ( int j=2; j < 8; ++j )
			{
				TurbidityPow[j] = TurbidityPow[j-1] * _Turbidity;
				Matrix[0,j] = TurbidityPow[j];
			}

			for ( int i=0; i < 14; ++i )
				for(int j = 0;j < 8;++j)
					Matrix[i,j] = ThetaPow[i] * TurbidityPow[j];

			// Execute coefficient multiplication for each coefficient
			for ( int l=0; l < _SHOrder; ++l )
				for ( int m=-l; m <= l ; ++m )
				{
					int bandindex =  l+m;

					double	cr = 0, cg = 0, cb = 0;
				
					for ( int i=0; i < 14; ++i )
						for ( int j=0; j < 8; ++j )
						{
							cr += Matrix[i,j] * Constants.SkyDomeSHTables.GetSkydomeSHBand( l )[bandindex,i,j,0];
							cg += Matrix[i,j] * Constants.SkyDomeSHTables.GetSkydomeSHBand( l )[bandindex,i,j,1];
							cb += Matrix[i,j] * Constants.SkyDomeSHTables.GetSkydomeSHBand( l )[bandindex,i,j,2];
						}

					int	k = l*(l+1) + m;

					SHCoefficients[k].x = (float) cr;
					SHCoefficients[k].y = (float) cg;
					SHCoefficients[k].z = (float) cb;
				}

			for ( int l=0; l < _SHOrder; ++l )
				for ( int m=1; m <= l; ++m )
				{
					int k_m = l*(l+1) + m;
					int k_minus_m = l*(l+1) - m;

					double c_m_r = SHCoefficients[k_m].x;
					double c_m_g = SHCoefficients[k_m].y;
					double c_m_b = SHCoefficients[k_m].z;

					double c_minus_m_r = SHCoefficients[k_minus_m].x;
					double c_minus_m_g = SHCoefficients[k_minus_m].y;
					double c_minus_m_b = SHCoefficients[k_minus_m].z;
					
					double tcos = Math.Cos( m * fPhi );
					double tsin = Math.Sin( m * fPhi );

					SHCoefficients[k_m].x = (float) (c_m_r*tcos - c_minus_m_r*tsin);
					SHCoefficients[k_m].y = (float) (c_m_g*tcos - c_minus_m_g*tsin);
					SHCoefficients[k_m].z = (float) (c_m_b*tcos - c_minus_m_b*tsin);
					
					SHCoefficients[k_minus_m].x = (float) (c_minus_m_r*tcos + c_m_r*tsin);
					SHCoefficients[k_minus_m].y = (float) (c_minus_m_g*tcos + c_m_g*tsin);
					SHCoefficients[k_minus_m].z = (float) (c_minus_m_b*tcos + c_m_b*tsin);
				}

			// Gibbs suppression (avoids ringing)
			for ( int l=1; l < _SHOrder; ++l )
			{
				int		k = l*(l+1);
				double	fAngle = Math.PI * l / _SHOrder;
				float	fFactor = (float) (Math.Sin( fAngle ) / fAngle);

				SHCoefficients[k].x *= fFactor;
				SHCoefficients[k].y *= fFactor;
				SHCoefficients[k].z *= fFactor;
			}

			// Apply scaling
 			for ( int i=0; i < _SHOrder * _SHOrder; ++i )
 				SHCoefficients[i] *= _fScale;

			return	SHCoefficients;
		}

		#region Helpers

		/// The monochromatic Perez function modeling sky appearance using a single parameter : Turbidity
		/// \param the 5 coefficients for the Perez function
		/// \param the polar angle for the Sun
		/// \param the phase angle between Sun direction and view direction
		/// \return the result of the Perez function
		protected double				Perez( float[] _Coefficients, float _Theta, float _Gamma )
		{
			// Horizon is a singularity... Make sure we're always a bit above it...
			_Theta = (float) System.Math.Min( .5f * System.Math.PI - 0.00001f, _Theta );

			double	fCosGamma = System.Math.Cos( _Gamma );
			return	(1.0f + _Coefficients[0] * System.Math.Exp( _Coefficients[1] / System.Math.Cos( _Theta ) )) *
					(1.0f + _Coefficients[2] * System.Math.Exp( _Coefficients[3] * _Gamma ) + _Coefficients[4] * fCosGamma * fCosGamma);
		}

		// Computes the phase angle between 2 sets of spherical coordinates
		protected float					ComputePhase( float _fTheta0, float _fPhi0, float _fTheta1, float _fPhi1 )
		{
			double	fCosPsi = System.Math.Sin( _fTheta0 ) * System.Math.Sin( _fTheta1 ) * System.Math.Cos( _fPhi1 - _fPhi0 ) + System.Math.Cos( _fTheta0 ) * System.Math.Cos( _fTheta1 );
			return	(float) (fCosPsi > 1.0f ? 0 : (fCosPsi < -1.0f ? System.Math.PI : System.Math.Acos( fCosPsi )));
		}

		/// Pre-Computes the in-scattering tables of extinction factors without the turbidity
		/// Remarks: The turbidity plays a role in the haze extinction but only as a global factor that we can add later in the computations.
		///			 This saves us a LOT of troubles as computing these tables is very expensive!
		protected void	PreComputeInScatteringTables()
		{
			double		fAirMoleculesDensity = 2.545e25;
			double		fDepolarizationFactor = 0.035;
			double		fSigmaMoleculesFactor = 2.0 * System.Math.PI * System.Math.PI / (3.0 * fAirMoleculesDensity);
						fSigmaMoleculesFactor *= (6.0 + 3.0 * fDepolarizationFactor) / (6.0 - 7.0 * fDepolarizationFactor);

//			double		fConcentrationFactor = (0.6544204545455 * _Turbidity - 0.6509886363636) * 1e-16;	// <= Turbidity is only a factor to sigma haze
			double		fConcentrationFactor = 1e-16;														// <= So, for now, we simply precompute the unit tables without this factor which we will use at update
			double		fSigmaHazeFactor = 0.01 * 0.434 * fConcentrationFactor * 4.0 * System.Math.PI * System.Math.PI;

			Spectrum.SpectrumRegular	SigmaMoleculesSpectrum = new Atmospheric.Spectrum.SpectrumRegular( TABLES_SPECTRUM_SAMPLES_COUNT, TABLES_SPECTRUM_LAMBDA_MIN, (TABLES_SPECTRUM_LAMBDA_MAX - TABLES_SPECTRUM_LAMBDA_MIN) / TABLES_SPECTRUM_SAMPLES_COUNT );
			Spectrum.SpectrumRegular	SigmaHazeSpectrum = new Atmospheric.Spectrum.SpectrumRegular( TABLES_SPECTRUM_SAMPLES_COUNT, TABLES_SPECTRUM_LAMBDA_MIN, (TABLES_SPECTRUM_LAMBDA_MAX - TABLES_SPECTRUM_LAMBDA_MIN) / TABLES_SPECTRUM_SAMPLES_COUNT );

			// Compute standard ambient scattering coefficients
			for ( int SlotIndex=0; SlotIndex < TABLES_SPECTRUM_SAMPLES_COUNT; SlotIndex++ )
			{
				double	fLambda = SigmaHazeSpectrum.GetSlotLambda( SlotIndex );

				SigmaMoleculesSpectrum.SetSlotValue( SlotIndex, 4.0 * System.Math.PI * fSigmaMoleculesFactor * Constants.Spectra.AerialPerspective.AIR_REFRACTION_SQUARE_MINUS_ONE[fLambda] * System.Math.Pow( fLambda, -4.0 ) );
				SigmaHazeSpectrum.SetSlotValue( SlotIndex, System.Math.PI * fSigmaHazeFactor / (fLambda * fLambda) * Constants.Spectra.AerialPerspective.K[fLambda] );
			}

			m_SigmaMolecules = Colorimetry.XYZ2RGB( Colorimetry.Spectrum2XYZ( SigmaMoleculesSpectrum ) );
			m_SigmaHaze = Colorimetry.XYZ2RGB( Colorimetry.Spectrum2XYZ( SigmaHazeSpectrum ) );

			// Precompute an array of in-scattered radiance factors for any phase in [0,PI]
			for ( int ThetaIndex=0; ThetaIndex <= TABLES_PHASE_SAMPLING_RESOLUTION; ThetaIndex++ )
			{
				double	fTheta = System.Math.PI * ThetaIndex / TABLES_PHASE_SAMPLING_RESOLUTION;

				for ( int SlotIndex=0; SlotIndex < TABLES_SPECTRUM_SAMPLES_COUNT; SlotIndex++ )
				{
					double	fLambda = SigmaHazeSpectrum.GetSlotLambda( SlotIndex );

					SigmaMoleculesSpectrum.SetSlotValue( SlotIndex, 0.7629 * fSigmaMoleculesFactor * Constants.Spectra.AerialPerspective.AIR_REFRACTION_SQUARE_MINUS_ONE[fLambda] * System.Math.Pow( fLambda, -4.0 ) * (1.0 + 0.9324 * System.Math.Cos( fTheta ) * System.Math.Cos( fTheta )) );
					SigmaHazeSpectrum.SetSlotValue( SlotIndex, 0.5 * fSigmaHazeFactor / (fLambda * fLambda) * Atmospheric.Constants.NetaTable.GetNeta( fLambda, fTheta ) );
				}

				m_AngularSigmaMolecules[ThetaIndex] = Colorimetry.XYZ2RGB( Colorimetry.Spectrum2XYZ( SigmaMoleculesSpectrum ) );
				m_AngularSigmaHaze[ThetaIndex] = Colorimetry.XYZ2RGB( Colorimetry.Spectrum2XYZ( SigmaHazeSpectrum ) );
			}
		}

		#endregion

		#endregion
	}
}
