// Adds support for physically accurate sky rendering
// It's an implementation of "Display of Earth Taking into Account Atmospheric Scattering" (1993)
//  by Nishita et Al. (http://nis-lab.is.s.u-tokyo.ac.jp/~nis/cdrom/sig93_nis.pdf)
//
// We compute both sky color for pixels at infinity as well as scattering and extinction for objects in the scene.
//
// =======================
// We remind the basic equations below and explain what choices we made to optimize the computation:
//
// I(λ,θ) = Io(λ).K.ρ.Fr(θ) / λ^4 
// where :
//	Io is the intensity of incident light
//	K is the molecular density at sea level (constant)
//	θ is the scattering angle (angle between light and view)
//	λ is the wavelength (as the equation uses the fourth power of wavelength, shorter wavelength are more scattered than others, hence the blue color of the sky)
//	Fr is the scattering phase function (we're using Rayleigh for air molecules and modified Henyey-Greenstein for aerosols)
//	ρ is the density ratio that is 1 at sea level and decreases with altitude following the formula below :
//
// ρ(h) = exp( -h / H0 )
// where :
//	h is the considered altitude
//	H0 is a scale factor (H0=7994m for air and H0=1200m for aerosols whose density decreases faster than air)
//
// Expression for K is :
// K = 2π² (n² - 1)² / (3 Ns)
// where :
//	n is the index of refraction of air (n=1.000293)
//	Ns is the molecular density of the standard atmosphere (Ns=904.046)
//
// Extinction coefficient is given by :
// β(λ) = 4π K / λ^4
//
// =======================
// Rayleigh scattering for air molecules uses :
// Fr(θ) = 0.75 (1 + cos²(θ))
// K = 2.5e-9
//
// Mie scattering for aerosols uses :
// Fr(θ) = (1-g²) / (1+g²-2gcos(θ))^3/2   <= Henyey Greenstein
// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick equivalent
// K = 2.5e-9
//
// =======================
// Full computation involves integrating scattering along the ray :
// 
// I(λ,θ) = Integral[0,Z]( τ(0,x,λ) * InScattering(x,λ,θ) * dx )
// where :
//	τ(s,s',λ) = exp( -β(λ).Integral[s,s']( ρ(h(l)) dl ) )	is the extinction along the ray from x to x' which we precompute in a table
//	InScattering(x,λ,θ) = L(x,θ) K.ρ.Fr(θ) / λ^4			is the in-scattered energy at x
//	L(x,θ) = Lsun(λ) . τ(x,x',λ)							is the sun's energy attenuated from the top of the atmosphere (x') to the current point (x)
//
// The optimizations lies in precomputing the optical depth in a texture, saving us the trouble of a second order integration.
// We're still left with a standard ray-marching integration though...
//
#if !defined( SKY_SUPPORT_FX )
#define SKY_SUPPORT_FX

#include "../Samplers.fx"

const static float	EARTH_RADIUS = 6400.0;										// In kilometers
const static float	ATMOSPHERE_THICKNESS = 60.0;								// In kilometers
const static float	ATMOSPHERE_RADIUS = EARTH_RADIUS + ATMOSPHERE_THICKNESS;	// In kilometers
const static float	INV_ATMOSPHERE_THICKNESS = 1.0 / ATMOSPHERE_THICKNESS;

const static float	H0_AIR = 7.994;										// Altitude scale factor for air molecules
const static float	H0_AEROSOLS = 1.200;								// Altitude scale factor for aerosols
const static float3	INV_WAVELENGTHS_POW4 = float3( 5.6020447463324113301354994573019, 9.4732844379230354373782268493533, 19.643802610477206282947491194819 );	// 1/λ^4 

float	_MiePhaseAnisotropy : SKYSUPPORT_MIE_ANISOTROPY = -0.75;		// Phase anisotropy factor in [-1,+1]
float	_DensitySeaLevel_Rayleigh : SKYSUPPORT_DENSITY_RAYLEIGH = 1e-5;	// Molecular density at sea level constant
float	_DensitySeaLevel_Mie : SKYSUPPORT_DENSITY_MIE = 1e-5;			// Aerosols density at sea level constant
float3	_Sigma_Rayleigh : SKYSUPPORT_SIGMA_RAYLEIGH = 0.0;				// 4.0 * PI * _DensitySeaLevel_Rayleigh / WAVELENGTHS_POW4;
float	_Sigma_Mie : SKYSUPPORT_SIGMA_MIE = 0.0;						// 4.0 * PI * _DensitySeaLevel_Mie;
float	_WorldUnit2Kilometer : SKYSUPPORT_UNITS_SCALE = 0.1;			// 1 World Unit equals XXX kilometers

float	_SunIntensity : SKYSUPPORT_SUN_INTENSITY = 1.0;
float3	_SkyZenith : SKYSUPPORT_SKY_ZENITH = 1.0;
float3	_SunDirection : SKYSUPPORT_SUN_DIRECTION = float3( 0.0, 1.0, 0.0 );

Texture2D	_DensityTexture : SKYSUPPORT_DENSITY_TEXTURE;

// Computes the optical depth along the ray
//	_AltitudeKm, the current camera altitude in kilometers ***from sea level***
//	_ViewDirection, the current view direction
//	_EarthNormal, the Earth's normal at current position
// Returns :
//  X,Y = ρ(h(_ViewPosition)), the density of air molecules (i.e. Rayleigh) and aerosols (i.e. Mie) respectively, at view position's altitude
//	Z,W = ρ(s,s') = Integral[s,s']( ρ(h(l)) dl ), the optical depth of air moleculs and aerosols respectively, from view position's altitude to the upper atmosphere
//
float4	ComputeOpticalDepth( float _AltitudeKm, float3 _ViewDirection, float3 _EarthNormal )
{
	// Normalize altitude
	_AltitudeKm *= INV_ATMOSPHERE_THICKNESS;

	// Actual view direction
	float	CosTheta = dot( _ViewDirection, _EarthNormal );

	float2	UV = float2( 0.5 * (1.0 - CosTheta), _AltitudeKm );
	return _DensityTexture.SampleLevel( LinearClamp, UV, 0 );
}

// Analytically computes the density of air molecules and aerosols at view position's altitude
//	_ViewPosition, the view position in WORLD space (not in kilometers but simple world units)
// (remarks: this method does NOT tap into the density texture)
// Returns :
//  X,Y = ρ(h(_ViewPosition)), the density of air molecules (i.e. Rayleigh) and aerosols (i.e. Mie) respectively, at view position's altitude
//
float2	ComputeAirDensity( float3 _ViewPosition )
{
	float	AltitudeKm = _WorldUnit2Kilometer * max( 0.0, _ViewPosition.y );
	return exp( -float2( AltitudeKm / H0_AIR, AltitudeKm / H0_AEROSOLS ) );
}

// Computes the in-scattered and attenuated energy due to atmospheric scattering
//	_AltitudeKm, the viewer's altitude in kilometers **from sea level**
//	_View, the viewer's direction in WORLD space
//	_EarthNormal, the Earth's normal at view position (usually, (0,1,0))
//	_DistanceKm, the distance of the point to light from the view position, in kilometers
//	_Extinction, the extinction coefficient at _Distance from view position
//	_Terminator, the terminator value (1 is fully lit, 0 is in Earth's shadow)
//	_StepsCount, the amount of marching steps to use
//
// The resulting values should be used like this :
// -----------------------------------------------
//	float3	InScatteredLight = ComputeSkyColor( ... )
//	float3	FinalColor = BackgroundColor * _Extinction + _Terminator * InScatteredLight;
//
float3	ComputeSkyColor( float _AltitudeKm, float3 _View, float3 _EarthNormal, float _DistanceKm, out float3 _Extinction, out float _Terminator, const int _StepsCount=8 )
{
	// Compute phases
	float	CosTheta = dot( _View, _SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
//	float	PhaseMie = 1.0 / (1.0 + _MiePhaseAnisotropy * CosTheta);
//			PhaseMie = (1.0 - _MiePhaseAnisotropy*_MiePhaseAnisotropy) * PhaseMie * PhaseMie;
	float	OnePlusG = 1.0 + _MiePhaseAnisotropy;
	float	PhaseMie = OnePlusG * OnePlusG / pow( 1.0 + _MiePhaseAnisotropy*_MiePhaseAnisotropy + 2.0 * _MiePhaseAnisotropy * CosTheta, 1.5 );

	// Compute potential intersection with earth's shadow
	_Terminator = 1.0;
	if ( dot( _EarthNormal, _SunDirection ) < 0.0 )
	{	// Project current position in the 2D plane normal to the light to test the intersection with the shadow cylinder cast by the Earth
		float3	CurrentPosition = (EARTH_RADIUS + _AltitudeKm) * _EarthNormal;

		float3	X = normalize( cross( _SunDirection, _View ) );
		float3	Y = cross( X, _SunDirection );
		float2	P = float2( dot( CurrentPosition, X ), dot( CurrentPosition, Y ) );
		float2	V = float2( dot( _View, X ), dot( _View, Y ) );
		float	a = dot( V, V );
		float	b = dot( P, V );
		float	c = dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
		float	Delta = b*b - a*c;
		if ( Delta >= 0.0 )
			_Terminator = 1.0 - saturate( (-b+sqrt(Delta)) / (a * _DistanceKm) );
	}

	// Ray-march the view ray
	float3	AccumulatedLightRayleigh = 0.0.xxx;
	float3	AccumulatedLightMie = 0.0.xxx;
	_Extinction = 1.0.xxx;

	float	StepSizeKm = _DistanceKm / _StepsCount;
	float	StepAltitudeKm = StepSizeKm * dot( _View, _EarthNormal );
	float	CurrentAltitudeKm = _AltitudeKm + 0.5 * StepAltitudeKm;	// Start at half a step

	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float4	OpticalDepth = ComputeOpticalDepth( CurrentAltitudeKm, _SunDirection, _EarthNormal );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth.x;
		float	Rho_aerosols = OpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere
		float3	SunExtinction = exp( -_Sigma_Rayleigh * OpticalDepth.z - _Sigma_Mie * OpticalDepth.w );
		float3	Light = _SunIntensity * SunExtinction;

		// =============================================
		// Compute in-scattered light
		float3	ScatteringRayleigh = Rho_air * _DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4 * PhaseRayleigh;
		float	ScatteringMie = Rho_aerosols * _DensitySeaLevel_Mie * PhaseMie;
		float3	InScatteringRayleigh = Light * ScatteringRayleigh;
		float3	InScatteringMie = Light * ScatteringMie;

		// =============================================
		// Accumulate in-scattered light
		AccumulatedLightRayleigh += InScatteringRayleigh * _Extinction * StepSizeKm;
		AccumulatedLightMie += InScatteringMie * _Extinction * StepSizeKm;

		// =============================================
		// Accumulate extinction along view
		_Extinction *= exp( -(_Sigma_Rayleigh * Rho_air + _Sigma_Mie * Rho_aerosols) * StepSizeKm );

		// March
		CurrentAltitudeKm += StepAltitudeKm;
	}

	return AccumulatedLightRayleigh + AccumulatedLightMie;
}

//	_ViewPosition, the viewer's position in WORLD space (not offset by Earth radius and not in kilometers but simple world units)
//	_DistanceKm, the distance of the point to light from the view position, in WORLD space
float3	ComputeSkyColor( float3 _ViewPosition, float3 _View, float _Distance, out float3 _Extinction, out float _Terminator, const int _StepsCount=8 )
{
	// Compute camera height & hit distance in kilometers
	float	AltitudeKm = _WorldUnit2Kilometer * _ViewPosition.y;
	float	DistanceKm = _WorldUnit2Kilometer * _Distance;

	return ComputeSkyColor( AltitudeKm, _View, float3( 0.0, 1.0, 0.0 ), DistanceKm, _Extinction, _Terminator, _StepsCount );
}

#endif