using System;
using System.Collections.Generic;
using System.Text;

using WMath;

namespace Atmospheric
{
	public class Sun
	{
		/// Computes the Sun's position given a time of day and a position on Earth's surface
		/// \param the observer's longitude (in radians)
		/// \param the observer's latitude (in radians)
		/// \param the observation date (in Julian days. That is, the day of year in [0,365])
		/// \param the observation time in hours (in [0,24])
		/// \return a vector containing (Theta, Phi) => (Polar angle from zenith, azimuth from south CCW)
		public static Vector2D	ComputeSunPosition( float _fLongitude, float _fLatitude, int _JulianDay, float _TimeOfDay )
		{
			// Compute solar time
			double	fSolarTime = _TimeOfDay + 0.17f * System.Math.Sin( 4.0f * System.Math.PI * (_JulianDay - 80) / 373 ) - 0.129f * System.Math.Sin( 2.0f * System.Math.PI * (_JulianDay - 8) / 355 ) - 12.0f * _fLongitude / System.Math.PI;

			// Compute solar declination
			double	fSolarDeclination = 0.4093f * System.Math.Sin( 2.0f * System.Math.PI * (_JulianDay - 81) / 368 );

			// Compute solar position
			return new Vector2D(	(float) (System.Math.Atan2( -System.Math.Cos( fSolarDeclination ) * System.Math.Sin( System.Math.PI * fSolarTime / 12 ), System.Math.Cos( _fLatitude ) * System.Math.Sin( fSolarDeclination ) - System.Math.Sin( _fLatitude ) * System.Math.Cos( fSolarDeclination ) * System.Math.Cos( System.Math.PI * fSolarTime / 12.0 ) )),
									(float) ((.5f * System.Math.PI - System.Math.Asin( System.Math.Sin( _fLatitude ) * System.Math.Sin( fSolarDeclination ) - System.Math.Cos( _fLatitude ) * System.Math.Cos( fSolarDeclination ) * System.Math.Cos( System.Math.PI * fSolarTime / 12 ) ))) );
		}

		/// Computes the Sun's solid angle as perceived from the surface of the Earth
		/// \return the Sun's solid angle (in steradian)
		public static double	ComputeSunSolidAngle()
		{
			return	0.25f * System.Math.PI * 1.39f * 1.39f / (150 * 150);	// = 6.7443e-05 sr
		}

		/// Computes the Sun's color as perceived from the surface of the Earth, attenuated by the atmosphere constituents (i.e. ozone, gases, aerosols, molecules, water vapor)
		/// \param the polar angle for the Sun
		/// \param the atmosphere's turbidity
		/// \return the Sun's color
		public static Vector	ComputeAttenuatedSunColor( float _fTheta, float _fTurbidity )
		{
			_fTheta = System.Math.Min( .95f * (.5f * (float) System.Math.PI), _fTheta );	// Here, we clamp theta as PI/2 is a singularity

			double		fAlpha = 1.3;														// Ratio of small to large particle sizes. (0:4,usually 1.3)
			double		fBeta = 0.04608365822050 * _fTurbidity - 0.04586025928522;			// Amount of aerosols present
			double		lOzone = .35;														// Amount of ozone in cm(NTP) 
			double		w = 2.0;															// Precipitable water vapor in centimeters (standard = 2) 

			double		SunOpticalMass = 1.0 / (System.Math.Cos( _fTheta ) + 0.15 * System.Math.Pow( 93.885 - _fTheta * 180.0 / System.Math.PI, -1.253 ) );	// Relative Optical Mass

			// Compute attenuated Sun energy on a spectrum from 350 to 800nm
			Spectrum.SpectrumRegular	Result = new Spectrum.SpectrumRegular( 91, 350e-9, 5e-9 );
			for ( int i = 0; i < 91; i++ )
			{
				double	fLambda = 350e-9 + i * 5e-9;

				// Rayleigh Scattering
				// Results agree with the graph (pg 115, MI) */
				double	fTauR = System.Math.Exp( -SunOpticalMass * 0.008735 * System.Math.Pow( fLambda * 1e6, -4.08 ) );		// fLambda should be in um

				// Aerosol (water + dust) attenuation
				// Results agree with the graph (pg 121, MI) 
				double	fTauA = System.Math.Exp( -SunOpticalMass * fBeta * System.Math.Pow( fLambda * 1e6, -fAlpha ) );			// fLambda should be in um

				// Attenuation due to ozone absorption  
				// Results agree with the graph (pg 128, MI) 
				double	fTauO = System.Math.Exp( -SunOpticalMass * Constants.Spectra.Sun.ATTENUATION_OZONE[fLambda] * lOzone );

				// Attenuation due to mixed gases absorption  
				// Results agree with the graph (pg 131, MI)
				double	fGasValue = Constants.Spectra.Sun.ATTENUATION_GASES[fLambda];
				double	fTauG = System.Math.Exp( -1.41 * fGasValue * SunOpticalMass / System.Math.Pow( 1 + 118.93 * fGasValue * SunOpticalMass, 0.45 ) );

				// Attenuation due to water vapor absorbtion  
				// Results agree with the graph (pg 132, MI)
				double	fWaterValue = Constants.Spectra.Sun.ATTENUATION_WATER[fLambda];
				double	fTauWA = System.Math.Exp( -0.2385 * fWaterValue * w * SunOpticalMass / System.Math.Pow( 1 + 20.07 * fWaterValue * w * SunOpticalMass, 0.45 ) );

				Result.SetSlotValue( i, Constants.Spectra.Sun.RADIANCE[fLambda] * fTauR * fTauA * fTauO * fTauG * fTauWA );
			}

			// Integrate spectrum into XYZ
			Vector	SunColor = 683.0f * Colorimetry.Spectrum2XYZ( Result );		// The 683 factor is here because we map radiance (W/m²/sr) into luminance (cd/m² or lm/m²/sr) and the peak energetic intensity at 555nm is 683 lm/W (or cd.sr/W)

			return	SunColor;
		}
	}
}
