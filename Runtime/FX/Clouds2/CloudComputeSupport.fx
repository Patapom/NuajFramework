// This shader displays clouds and sky
// Search for TODO !
//
#ifndef CLOUD_COMPUTE_SUPPORT
#define CLOUD_COMPUTE_SUPPORT

#include "../Camera.fx"
#include "../Samplers.fx"
#include "AmbientSkySupport.fx"
#include "CloudSupport.fx"

// Phase function is a mix of 4 henyey-greenstein lobes : strong forward, forward, backward and medium side
float		_ScatteringAnisotropyStrongForward = 0.95;
float		_PhaseWeightStrongForward = 0.08;
float		_ScatteringAnisotropyForward = 0.8;
float		_PhaseWeightForward = 0.2;
float		_ScatteringAnisotropyBackward = -0.7;
float		_PhaseWeightBackward = 0.1;
float		_ScatteringAnisotropySide = -0.4;
float		_PhaseWeightSide = 0.8;
float		_PhaseWeightSide2 = 0.2;

float		_DirectionalFactor = 1.0;					// Factor to apply to directional energy
float		_IsotropicFactor = 1.0;						// Factor to apply to isotropic energy
float2		_IsotropicScatteringFactors = 1.0;			// Factor to apply to TOP (X) and BOTTOM (Y) scattering


float4		_DEBUG0;
float4		_DEBUG1;
float4		_DEBUG2;


// Night Sky
float3		_AmbientNightSky;							// The ambient night sky color
float3		_TerrainAlbedo;								// The terrain albedo used for lighting of the base of the clouds
float3		_SunColorFromGround;						// The color of the Sun attenuated by the atmosphere (but not by the clouds)


// ===================================================================================
// Computes the intersection with ground and cloud spheres
struct	Hit
{
	bool	bTraceAirBefore;			// True to trace air -> cloud-in
	bool	bTraceAirBeforeAccurate;	// True if camera is below clouds
	bool	bTraceCloud;				// True to trace cloud-in -> cloud-out
	bool	bTraceAirAfter;				// True to trace cloud-out -> air
	bool	bTraceAirAfterAccurate;		// True if camera is above clouds

	float	HitDistanceStartKm;			// Start =>
	float	HitDistanceCloudInKm;		// Cloud In =>
	float	HitDistanceCloudOutKm;		// Cloud Out =>
	float	HitDistanceEndKm;			// End
};

// Computes the intersections of the camera ray with the cloud planes
//	_EndDistanceKm, the distance to the end of the ray (either the top of the atmosphere or the ZBuffer)
//
Hit		ComputeLayerIntersection( float3 _PositionKm, float3 _View, float _EndDistanceKm )
{
	// Compute intersections with cloud spheres
	Hit		Result;
	Result.HitDistanceEndKm = _EndDistanceKm;					// Always end where we're supposed to : only the other intervals' distance will be modified
	Result.bTraceCloud = true;									// We need to trace the cloud anyway

	float	CameraAltitudeKm = _PositionKm.y;
	if ( CameraAltitudeKm < _CloudAltitudeKm.x )
	{	// Below the clouds
		Result.HitDistanceStartKm = 0.0;																					// Start from camera
		Result.HitDistanceCloudInKm = ComputeSphereIntersectionExit( _PositionKm, _View, _CloudAltitudeKm.x );				// Exit bottom sphere
		Result.HitDistanceCloudOutKm = ComputeSphereIntersectionExit( _PositionKm, _View, _CloudAltitudeKm.y );				// Exit top sphere

		Result.bTraceAirBefore = true;							// We have air before entering the clouds
		Result.bTraceAirBeforeAccurate = true;					// We're below the clouds so the FIRST air section must be traced accurately for godrays
		Result.bTraceAirAfter = true;							// We have air after exiting the clouds
		Result.bTraceAirAfterAccurate = false;					// Second air section needs not be accurate
	}
	else if ( CameraAltitudeKm < _CloudAltitudeKm.y )
	{	// Inside the clouds
		Result.HitDistanceStartKm = 0.0;																					// Start from camera
		Result.HitDistanceCloudInKm = 0.0;																					// Start from camera
		float	EnterBelow = ComputeSphereIntersectionEnter( _PositionKm, _View, _CloudAltitudeKm.x );						// Either enter bottom sphere (i.e. viewing down)
				EnterBelow = EnterBelow < 0.0 ? INFINITY : EnterBelow;														// If the bottom sphere is behind the camera then reject intersection
		float	ExitAbove = ComputeSphereIntersectionExit( _PositionKm, _View, _CloudAltitudeKm.y );						// Or exit top sphere (i.e. viewing up)
		Result.HitDistanceCloudOutKm = min( EnterBelow, ExitAbove );				


		// IMPORTANT NOTE (maybe) :
		// We're missing a case here that may represent a very thin layer of pixels right below the clouds.
		// That's when the camera looks down through the clouds layer once then grazes the ground but enter the clouds again a second time to finally exit through the atmosphere...
		// We can't handle that as that would involve tracing the clouds layer twice!

// 		float	ReExitBelow = ComputeSphereIntersectionExit( _PositionKm, _View, _CloudAltitudeKm.x );
// 		if ( ReExitBelow < _EndDistanceKm )
// 			Result.HitDistanceEndKm = _EndDistanceKm = ReExitBelow;


		Result.bTraceAirBefore = false;							// We are already inside the cloud, no need to trace air before
		Result.bTraceAirBeforeAccurate = false;					// Don't care
		Result.bTraceAirAfter = true;							// We have air after exiting the clouds
		Result.bTraceAirAfterAccurate = EnterBelow < ExitAbove;	// Second air section needs to be accurate only if viewing down
	}
	else
	{	// Above the clouds
		Result.HitDistanceStartKm = max( 0.0, ComputeSphereIntersectionEnter( _PositionKm, _View, ATMOSPHERE_THICKNESS ) );	// Start either from camera or top of the atmosphere
		float	EnterAbove = ComputeSphereIntersectionEnter( _PositionKm, _View, _CloudAltitudeKm.y );						// Enter clouds from above (i.e. looking down at the clouds)
				EnterAbove = EnterAbove < 0.0 ? INFINITY : EnterAbove;														// If the top sphere is behind the camera then reject intersection (i.e. viewing straight up)
		float	ExitAtmosphere = ComputeSphereIntersectionExit( _PositionKm, _View, ATMOSPHERE_THICKNESS );					// Or exit the atmosphere again (i.e. not looking at the clouds)
		Result.HitDistanceCloudInKm = min( EnterAbove, ExitAtmosphere );
		float	EnterBelow = ComputeSphereIntersectionEnter( _PositionKm, _View, _CloudAltitudeKm.x );						// Either enter bottom sphere (i.e. viewing straight down through the clouds)
				EnterBelow = EnterBelow < 0.0 ? INFINITY : EnterBelow;														// If the bottom sphere is behind the camera then reject intersection (i.e. viewing straight up)
		float	ExitAbove = ComputeSphereIntersectionExit( _PositionKm, _View, _CloudAltitudeKm.y );						// Or exit top sphere again (i.e. grazing bottom sphere but missing then exiting again to finally exit atmosphere again)
		Result.HitDistanceCloudOutKm = min( EnterBelow, ExitAbove );


		// IMPORTANT NOTE (maybe) :
		// We're missing a case here that may represent a very thin layer of pixels right below the clouds.
		// That's when the camera looks down through the clouds layer once then grazes the ground but enter the clouds again a second time to finally exit through the atmosphere...
		// We can't handle that as that would involve tracing the clouds layer twice!


		Result.bTraceAirBefore = true;							// We have air before entering the clouds
		Result.bTraceAirBeforeAccurate = false;					// First air section needs not be accurate
		Result.bTraceAirAfter = true;							// We have air after exiting the clouds
		Result.bTraceAirAfterAccurate = EnterBelow < ExitAbove;	// Second air section needs to be accurate only if viewing down
	}

	// Final testing for empty intervals
	if ( _EndDistanceKm <= Result.HitDistanceStartKm )
	{	// We're not hitting anything !
		// This happens if we're totally out of the atmosphere looking out...
		Result.bTraceAirBefore = Result.bTraceCloud = Result.bTraceAirAfter = false;	// No need to trace anything...
	}
	else if ( _EndDistanceKm <= Result.HitDistanceCloudInKm )
	{	// We're hitting the end before entering the clouds
		Result.bTraceCloud = Result.bTraceAirAfter = false;	// No need to trace cloud or air after the clouds anyway
		Result.HitDistanceCloudInKm = _EndDistanceKm;
	}
	else if ( _EndDistanceKm <= Result.HitDistanceCloudOutKm )
	{	// We're hitting the end before exiting the clouds
		Result.bTraceAirAfter = false;						// No need to trace air after the clouds anyway
		Result.HitDistanceCloudOutKm = _EndDistanceKm;
	}

	return Result;
}

// ===================================================================================
// Computes the Sun intensity arriving at the specified position within the cloud
float3	ComputeSunIntensity( float3 _PositionKm, float _Distance2CameraKm, float2 _OpticalDepth_air, uniform bool _bBelowClouds )
{
	float	OpticalDepth_cloud = 0.0;
	if ( _bBelowClouds )
		OpticalDepth_cloud = ComputeCloudOpticalDepth( _PositionKm, _Distance2CameraKm );
	return _SunIntensity * exp( -_Sigma_Rayleigh * _OpticalDepth_air.x - _Sigma_Mie * _OpticalDepth_air.y - OpticalDepth_cloud );
}

// ===================================================================================
// Traces through air
float	_FarClipAirKm = 10.0;

void	TraceAir( float3 _CameraPositionKm, float3 _View, float _DistanceInKm, float _DistanceOutKm, inout float3 _Extinction, inout float3 _InScatteredEnergy, float2 _EarthShadowDistances, float2 _Phases, uniform bool _bBelowClouds, uniform int _StepsCount )
{
	float	DistanceKm = _DistanceOutKm - _DistanceInKm;	// Distance we need to trace

	float	OriginalTraceDistanceKm = DistanceKm;
	if ( _bBelowClouds )
		DistanceKm = min( _FarClipAirKm, DistanceKm );		// Clip to some value...

	float4	View = float4( _View, 1.0 );
	float	StepSizeKm = DistanceKm / _StepsCount;
	float4	StepKm = StepSizeKm * View;
	float4	CurrentPositionKm = float4( _CameraPositionKm, 0.0 ) + _DistanceInKm * View;
			CurrentPositionKm += 0.5 * StepKm;	// Start at half a step

	// Ray-march the view ray
	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float	CurrentAltitudeKm = length( CurrentPositionKm.xyz - float3( 0.0, -EARTH_RADIUS, 0.0 ) ) - EARTH_RADIUS;
		float4	OpticalDepth = ComputeOpticalDepth( CurrentAltitudeKm, _SunDirection, float3( 0, 1, 0 ) );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth.x;
		float	Rho_aerosols = OpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere
		float3	Light = ComputeSunIntensity( CurrentPositionKm.xyz, CurrentPositionKm.w, OpticalDepth.zw, _bBelowClouds );

		// =============================================
		// Accumulate in-scattered light
		float3	ScatteringCoeffRayleigh = Rho_air * _DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4;
		float3	InScatteringRayleigh = Light * ScatteringCoeffRayleigh * _Phases.x;

		float	ScatteringCoeffMie = Rho_aerosols * _DensitySeaLevel_Mie;
		float3	InScatteringMie = Light * ScatteringCoeffMie * _Phases.y;

		_InScatteredEnergy += ComputeEarthShadow( CurrentPositionKm.w, _EarthShadowDistances ) * StepSizeKm * _Extinction * (InScatteringRayleigh + InScatteringMie);

		// =============================================
		// Accumulate extinction along view
		_Extinction *= exp( -(_Sigma_Rayleigh * Rho_air + _Sigma_Mie * Rho_aerosols) * StepSizeKm );

		// =============================================
		// March
		CurrentPositionKm += StepKm;
	}
}

// ===================================================================================
// Traces through the cloud layer
float	_FarClipCloudKm = 10.0;

float	ComputeAdaptativeStepSize( float _Sigma_t, float _Distance2CameraKm, float _CloudInKm, float _CloudOutKm, float _InvMaxStepSizeKm )
{
// 	float2	StepDataNear = float2(	 3.0,	1.0 / 0.25 );	// Near range parameters
// 	float2	StepDataMiddle = float2( 2.0,	1.0 / 0.4 );	// Mid range parameters
// 	float2	StepDataFar = float2(	 1.0,	1.0 / 1.0 );	// Far range parameters
// 
// // 	float	DistanceNearKm = 0.0;
// // 	float	DistanceMiddleKm = 4.0;
// // 	float	DistanceFarKm = 8.0;
// 	float	DistanceNearKm = _DEBUG1.x;
// 	float	DistanceMiddleKm = _DEBUG1.y;
// 	float	DistanceFarKm = _DEBUG1.z;
// 
// 	float	d0 = (_Distance2CameraKm - DistanceNearKm) / (DistanceMiddleKm - DistanceNearKm);
// 	float	d1 = saturate( (_Distance2CameraKm - DistanceMiddleKm) / (DistanceFarKm - DistanceMiddleKm) );
// 
// 	float2	StepData = lerp( StepDataNear, StepDataMiddle, d0 ) * step( d0, 1.0 )
// 					 + lerp( StepDataMiddle, StepDataFar, d1 ) * step( 1.0, d0 );
// 	return StepData.y / max( StepData.x * _InvMaxStepSizeKm, _Sigma_t );

	return _DEBUG0.y / max( _InvMaxStepSizeKm, _Sigma_t );	// Mean free path
}

// Computes light in-scattering at the current position
float3	ComputeSingleStepScattering( float3 _WorldPositionKm, float3 _View, float3 _DirectionalLight, float3 _AmbientSkyColor, float3 _AmbientSkyColorNoCloud, float _Density, float2 _IsotropicDensity, float _HeightKm, float _PhaseDirect, float _DotEarthSun, float4 _TerrainEmissive )
{
	// =============================================
	// Invent a scattered light based on traversed cloud depth
	// The more light goes through cloud matter, the more it's scattered within the cloud
//	float3	IsotropicLightTop = _IsotropicScatteringFactors.x * (_AmbientSkyColorNoCloud + _AmbientNightSky);
	float3	IsotropicLightTop = _IsotropicScatteringFactors.x * (_AmbientSkyColor + _AmbientNightSky);

	float3	TerrainColor = INV_4PI * _TerrainEmissive.w * _TerrainAlbedo * (_SunColorFromGround + _AmbientSkyColor) * saturate( _DotEarthSun )	// Diffuse reflection of Sun & Sky on terrain
						 + _TerrainEmissive.xyz;	// Emissive terrain color
	float3	IsotropicLightBottom = _IsotropicScatteringFactors.y * TerrainColor;

// 	float	Sigma_isotropic_top = _IsotropicDensity.x * _CloudSigma_s;
// 	float	Sigma_isotropic_bottom = _IsotropicDensity.y * _CloudSigma_s;
	float	Sigma_isotropic_top = _DEBUG0.w * _CloudSigma_s;
	float	Sigma_isotropic_bottom = _DEBUG0.w * _CloudSigma_s;
	float	IsotropicSphereRadiusTop = _CloudAltitudeKm.z - _HeightKm;
	float	IsotropicSphereRadiusBottom = _HeightKm;

//	float3	IsotropicScatteringTop = _Density * Sigma_isotropic_top * IsotropicLightTop * exp( -Sigma_isotropic_top * IsotropicSphereRadiusTop );
//	float3	IsotropicScatteringBottom = _Density * Sigma_isotropic_bottom * IsotropicLightBottom * exp( -Sigma_isotropic_bottom * IsotropicSphereRadiusBottom );
//	float3	IsotropicScattering = _IsotropicFactor * (IsotropicScatteringTop + IsotropicScatteringBottom);

 	float3	IsotropicScatteringTop = IsotropicLightTop * Sigma_isotropic_top * IsotropicSphereRadiusTop * exp( -Sigma_isotropic_top * IsotropicSphereRadiusTop );
 	float3	IsotropicScatteringBottom = IsotropicLightBottom * Sigma_isotropic_bottom * IsotropicSphereRadiusBottom * exp( -Sigma_isotropic_bottom * IsotropicSphereRadiusBottom );
 	float3	IsotropicScattering = _IsotropicFactor * saturate( 0.0 + _Density ) * (IsotropicScatteringTop + IsotropicScatteringBottom);

	// =============================================
	// Compute in-scattered direct light
	float	ScatteringCoeffCloud = _CloudSigma_s * _Density;
//	return ScatteringCoeffCloud * (_DirectionalLight * _PhaseDirect + ComputeLightningColor( _WorldPositionKm, _View, _MiePhaseAnisotropy )) + IsotropicScattering;
	return ScatteringCoeffCloud * (_DirectionalLight * _PhaseDirect) + IsotropicScattering;
//	return ScatteringCoeffCloud * _DirectionalLight * _PhaseDirect;
//	return ScatteringCoeffCloud * IsotropicScattering;
//	return ScatteringCoeffCloud * IsotropicScatteringTop;
}

float	TraceCloud( float3 _CameraPositionKm, float3 _View, float _DistanceInKm, float _DistanceOutKm, inout float3 _Extinction, inout float3 _InScatteredEnergy, float2 _EarthShadowDistances, float2 _Phases, float3 _AmbientSkyColor, float3 _AmbientSkyColorNoCloud, float _MipLevel, uniform int _StepsCount )
{
	float4	CurrentPositionKm = float4( _CameraPositionKm + _DistanceInKm * _View, _DistanceInKm );
	float	DistanceKm = _DistanceOutKm - _DistanceInKm;	// Distance we need to trace

	float	OriginalTraceDistanceKm = DistanceKm;
	DistanceKm = min( _FarClipCloudKm, DistanceKm );

	// Compute cloud Light phases
	float	CosTheta = dot( _View, _SunDirection );

		// Strong forward phase
	float	OneMinusG = 1.0 - _ScatteringAnisotropyStrongForward;
	float	PhaseStrongForward = OneMinusG * OneMinusG / pow( abs(1.0 + _ScatteringAnisotropyStrongForward*_ScatteringAnisotropyStrongForward - 2.0 * _ScatteringAnisotropyStrongForward * CosTheta), 1.5 );
		// Forward phase
			OneMinusG = 1.0 - _ScatteringAnisotropyForward;
	float	PhaseForward = OneMinusG * OneMinusG / pow( abs(1.0 + _ScatteringAnisotropyForward*_ScatteringAnisotropyForward - 2.0 * _ScatteringAnisotropyForward * CosTheta), 1.5 );
		// Backward phase
			OneMinusG = 1.0 - _ScatteringAnisotropyBackward;
	float	PhaseBackward = OneMinusG * OneMinusG / pow( abs(1.0 + _ScatteringAnisotropyBackward*_ScatteringAnisotropyBackward - 2.0 * _ScatteringAnisotropyBackward * CosTheta), 1.5 );
		// Side phase
//	CosTheta = 0.4 + 0.6 * CosTheta;	// Add bias
	float	PhaseSide = saturate( pow( sqrt(1.0 - 0.8 * CosTheta*CosTheta), _ScatteringAnisotropySide ) );

	float	PhaseDirect = _PhaseWeightSide * PhaseSide + _PhaseWeightStrongForward * PhaseStrongForward + _PhaseWeightForward * PhaseForward + _PhaseWeightBackward * PhaseBackward;
	PhaseDirect *= INV_4PI;

const float	RHO_TOLERANCE = _DEBUG0.z;
const float	INV_MAX_STEP_SIZE_KM = _StepsCount / (_DEBUG0.x * DistanceKm);

	// Compute start parameters
	float	PreviousRho = 0.0;							// Empty space
	float	PreviousDRho = 0.0;
	float4	PreviousPositionKm = CurrentPositionKm;
	float4	StepKm = float4( _View, 1.0 );
//	CurrentPositionKm += 0.01 * DistanceKm * StepKm;	// Walk a little into the cloud

	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
//_MipLevel = ComputeNoiseMipLevel( CurrentPositionKm.w );

		// =============================================
		// Compute cloud density at position
		float	HeightInCloudKm;
		float3	CloudDensities = ComputeCloudDensity( CurrentPositionKm.xyz, CurrentPositionKm.w, HeightInCloudKm, _MipLevel );
		float	CurrentRho = CloudDensities.x;
//return CurrentRho;

// 		float	ExpectedRho = PreviousRho + PreviousDRho * (CurrentPositionKm.w - PreviousPositionKm.w);
// 		float	RhoDiff = abs( CurrentRho - ExpectedRho );
// 		if ( RhoDiff > RHO_TOLERANCE )
// 		{	// We need to go back a little or we may miss important stuff !
// 			CurrentPositionKm += ((RhoDiff - RHO_TOLERANCE) / RHO_TOLERANCE) * StepKm;
// 			continue;
// 		}

		float	CurrentSigma = _CloudSigma_t * CurrentRho;
//return CurrentSigma;

		// =============================================
		// Compute new adaptative step
//		float	StepSizeKm = _DEBUG0.y / max( INV_MAX_STEP_SIZE_KM, CurrentSigma );	// Mean free path
		float	StepSizeKm = ComputeAdaptativeStepSize( CurrentSigma, CurrentPositionKm.w, _DistanceInKm, _DistanceOutKm, INV_MAX_STEP_SIZE_KM );
//StepSizeKm = 1.0 / INV_MAX_STEP_SIZE_KM;
//StepSizeKm = 0.1;
//return	StepSizeKm;

		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float	CurrentAltitudeKm = _CloudAltitudeKm.x + HeightInCloudKm;
		float4	OpticalDepth_air = ComputeOpticalDepth( CurrentAltitudeKm, _SunDirection, float3( 0.0, 1.0, 0.0 ) );
		float	Rho_air = OpticalDepth_air.x;
		float	Rho_aerosols = OpticalDepth_air.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere & clouds
		float3	Light = _DirectionalFactor * ComputeSunIntensity( CurrentPositionKm.xyz, CurrentPositionKm.w, OpticalDepth_air.zw, true );

		// =============================================
		// Accumulate in-scattered light
		float3	ScatteringCoeffRayleigh = Rho_air * _DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4;
		float3	InScatteringRayleigh = Light * ScatteringCoeffRayleigh * _Phases.x;

		float	ScatteringCoeffMie = Rho_aerosols * _DensitySeaLevel_Mie;
		float3	InScatteringMie = Light * ScatteringCoeffMie * _Phases.y;

		float3	InScatteringCloud = ComputeSingleStepScattering(
			CurrentPositionKm.xyz,
			_View, 
			Light, 
			_AmbientSkyColor, 
			_AmbientSkyColorNoCloud, 
			CurrentRho, 
			CloudDensities.yz,
			HeightInCloudKm, 
			PhaseDirect, 
			_SunDirection.y,
			float4( 0.0.xxx, 1.0 ) );

		_InScatteredEnergy += ComputeEarthShadow( CurrentPositionKm.w, _EarthShadowDistances ) * StepSizeKm * _Extinction * (InScatteringRayleigh + InScatteringMie + InScatteringCloud);

		// =============================================
		// Accumulate extinction along view
		_Extinction *= exp( -(_Sigma_Rayleigh * Rho_air + _Sigma_Mie * Rho_aerosols + _CloudSigma_t * CurrentRho) * StepSizeKm );

		// =============================================
		// March forward
		PreviousDRho = (CurrentRho - PreviousRho) / (CurrentPositionKm.w - PreviousPositionKm.w);	// New density derivative
		PreviousRho = CurrentRho;																	// New density
		PreviousPositionKm = CurrentPositionKm;														// New position
		CurrentPositionKm += StepSizeKm * StepKm;													// March !
	}

	return _InScatteredEnergy;
	return _Extinction;
	return 0.0;
}

#endif	// CLOUD_COMPUTE_SUPPORT
