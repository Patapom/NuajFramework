using System;
using System.Collections.Generic;
using System.Text;

using WMath;

namespace Atmospheric
{
	public class Colorimetry
	{
		#region CONSTANTS

		public static readonly float[,]	XYZ_TO_RGB		= new float[3,3] {	{  3.240790f, -0.969256f,  0.055648f },
																			{ -1.537150f,  1.875992f, -0.204043f },
																			{ -0.498535f,  0.041556f,  1.057311f } };

		#endregion

		#region METHODS

		// Converts a spectrum into an XYZ color
		public static Vector		Spectrum2XYZ( Atmospheric.Spectrum.Spectrum _Spectrum )
		{
			double	fDeltaLambda = (_Spectrum.LambdaMax - _Spectrum.LambdaMin) / 100.0f;

			return	new Vector(	(float) (_Spectrum.KernelIntegrate( Atmospheric.Constants.Spectra.ColorMatchingFunctionXYZ.X, fDeltaLambda ) * 1e9),
								(float) (_Spectrum.KernelIntegrate( Atmospheric.Constants.Spectra.ColorMatchingFunctionXYZ.Y, fDeltaLambda ) * 1e9),
								(float) (_Spectrum.KernelIntegrate( Atmospheric.Constants.Spectra.ColorMatchingFunctionXYZ.Z, fDeltaLambda ) * 1e9) );
		}

		// Converts XYZ into RGB
		public static Vector		XYZ2RGB( Vector _XYZ )
		{
			return new Vector(	_XYZ.x * XYZ_TO_RGB[0,0] + _XYZ.y * XYZ_TO_RGB[1,0] + _XYZ.z * XYZ_TO_RGB[2,0],
								_XYZ.x * XYZ_TO_RGB[0,1] + _XYZ.y * XYZ_TO_RGB[1,1] + _XYZ.z * XYZ_TO_RGB[2,1],
								_XYZ.x * XYZ_TO_RGB[0,2] + _XYZ.y * XYZ_TO_RGB[1,2] + _XYZ.z * XYZ_TO_RGB[2,2] );
		}

		#endregion
	}
}
