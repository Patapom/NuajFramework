// This shader displays clouds and sky
// Search for TODO !
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"
#include "AmbientSkySupport.fx"
#include "CloudSupport.fx"

// Ray-marching precision
static const int	STEPS_COUNT_CLOUDS = 64;
static const int	STEPS_COUNT_AIR = 8;
static const int	STEPS_COUNT_AIR_GOD_RAYS = 32;

// Ray-marching precision for sky probe
static const int	STEPS_COUNT_CLOUDS_PROBE = 32;
static const int	STEPS_COUNT_AIR_PROBE = 8;
static const int	STEPS_COUNT_AIR_GOD_RAYS_PROBE = 12;

static const float	GROUND_ALTITUDE_KM = -2.0;			// We define a "ground sphere" so rays can't go below the ground

Texture2D	_DownscaledZBuffer;							// ZBuffer downscaled by 2 and 4 (in the 2 first mip levels)

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

float		_CloudOpacityFactor = 100.0;				// Factor for cloud's pseudo ZBuffer
float		_JitterNoiseSize = 1.0;						// Noise size for jittering
float		_SamplingJitterAmplitude = 1.0;				// UV jittering amplitude in kilometer

struct VS_IN
{
	float4	Position		: SV_POSITION;
};

struct PS_IN
{
	float4	Position				: SV_POSITION;
	float3	AmbientSkyColor			: AMBIENT_SKY;
	float3	AmbientSkyColorNoCloud	: AMBIENT_SKY_NO_CLOUD;
};

VS_IN	VS( VS_IN _In ) { return _In; }

PS_IN	VS_Ambient( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
// 	Out.AmbientSkyColor = ComputeAmbientSkyIrradiance( float3( 0, 1, 0 ) );
// 	Out.AmbientSkyColorNoCloud = ComputeAmbientSkyIrradianceNoCloud( float3( 0, 1, 0 ) );
	Out.AmbientSkyColor = ComputeAmbientSkyIrradiance( _SunDirection );
	Out.AmbientSkyColorNoCloud = ComputeAmbientSkyIrradianceNoCloud( _SunDirection );

	return Out;
}

// ===================================================================================
// Downscaled ZBuffer readback
float	ReadDownscaledDepth( float2 _Position )
{
	return _DownscaledZBuffer.SampleLevel( NearestClamp, _Position * _BufferInvSize, 1.0 ).x;
}

// ===================================================================================
// Compute the intersection with ground and cloud spheres

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
_MipLevel = ComputeNoiseMipLevel( CurrentPositionKm.w );

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


// ===================================================================================
// Renders the entire sky+clouds
void	RenderSky( float3 _CameraPositionKm, float3 _View, float _Z, float _ViewLength, float3 _AmbientSkyColor, float3 _AmbientSkyColorNoCloud, out float3 _Extinction, out float3 _InScattering, out float _CloudOpticalDepth )
{
	float	MaxDistanceKm = _WorldUnit2Kilometer * _Z * _ViewLength;

	// If pixel is at "infinity" then compute intersection with upper atmosphere
	if ( _Z > CameraData.w-5.0 )
		MaxDistanceKm = ComputeSphereIntersectionExit( _CameraPositionKm, _View, ATMOSPHERE_THICKNESS );

	// Also compute intersection with the ground sphere to avoid tracing to the other side of the planet
	float	Distance2GroundKm = ComputeSphereIntersection( _CameraPositionKm, _View, GROUND_ALTITUDE_KM );
	if ( Distance2GroundKm > 0.0 )
		MaxDistanceKm = min( MaxDistanceKm, Distance2GroundKm );

	// Compute hit with the cloud layer
	Hit		H = ComputeLayerIntersection( _CameraPositionKm, _View, MaxDistanceKm );

	// Compute a single mip level at the average hit distance with the cloud
	// We prefer doing that only once rather than at every ray marching step because using a log2() is quite expensive
	float	CloudMipLevel = ComputeNoiseMipLevel( 0.5 * (H.HitDistanceCloudInKm + H.HitDistanceCloudOutKm) );

// Compute the amount of 3D noise voxels seen in a single image pixel
// float3	PositionKm = CameraPositionKm + View * H.HitDistanceCloudInKm;
// float	Distance2CameraKm = length( PositionKm - CameraPositionKm );
// float	PixelSizeKm = 2.0 * Distance2CameraKm * CameraData.x * _BufferInvSize.y;	// Vertical size of a pixel in kilometers at the distance where we're sampling the texture
// 
// float	EncompassedVoxels = _InvVoxelSizeKm * PixelSizeKm;							// This is the amount of voxels we can see in that pixel
// float	MipLevel = log2( EncompassedVoxels );										// And the miplevel we need to sample the texture with...
// 
// PS_OUT	Merde;
// Merde.InScattering = Merde.ExtinctionZ = 0.125 * MipLevel;
// return Merde;

// 	// Compute potential intersection with earth's shadow
// 	float	HeightKm = EARTH_RADIUS + _WorldUnit2Kilometer * CameraPosition.y;
// 	float	HitDistanceKm = _WorldUnit2Kilometer * Distance2Pixel;
// 	float3	CurrentPosition = float3( 0.0, HeightKm, 0.0 );
 
 	float2	EarthShadowDistances = ComputeEarthShadowDistances( _CameraPositionKm, _View );

	// Compute light phases
	float	CosTheta = dot( _View, _SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	OneMinusG = 1.0 - _MiePhaseAnisotropy;
	float	PhaseMie = OneMinusG * OneMinusG / pow( abs(1.0 + _MiePhaseAnisotropy*_MiePhaseAnisotropy - 2.0 * _MiePhaseAnisotropy * CosTheta), 1.5 );
	float2	Phases = INV_4PI * float2( PhaseRayleigh, PhaseMie );

	// Trace
	_InScattering = 0.0;
	_Extinction = 1.0;
	_CloudOpticalDepth = 0.0;

// TraceCloud( _CameraPositionKm, _View, H.HitDistanceCloudInKm, H.HitDistanceCloudOutKm, _Extinction, _InScattering, EarthShadowDistances, Phases, _AmbientSkyColor, _AmbientSkyColorNoCloud, CloudMipLevel, STEPS_COUNT_CLOUDS );
// return;

	if ( H.bTraceAirBefore )
	{
		if ( H.bTraceAirBeforeAccurate )
			TraceAir( _CameraPositionKm, _View, H.HitDistanceStartKm, H.HitDistanceCloudInKm, _Extinction, _InScattering, EarthShadowDistances, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( _CameraPositionKm, _View, H.HitDistanceStartKm, H.HitDistanceCloudInKm, _Extinction, _InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR );
	}

	if ( H.bTraceCloud )
		_CloudOpticalDepth = TraceCloud( _CameraPositionKm, _View, H.HitDistanceCloudInKm, H.HitDistanceCloudOutKm, _Extinction, _InScattering, EarthShadowDistances, Phases, _AmbientSkyColor, _AmbientSkyColorNoCloud, CloudMipLevel, STEPS_COUNT_CLOUDS );
	else
		_CloudOpticalDepth = 1.0;

	if ( H.bTraceAirAfter )
	{
		if ( H.bTraceAirAfterAccurate )
			TraceAir( _CameraPositionKm, _View, H.HitDistanceCloudOutKm, H.HitDistanceEndKm, _Extinction, _InScattering, EarthShadowDistances, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( _CameraPositionKm, _View, H.HitDistanceCloudOutKm, H.HitDistanceEndKm, _Extinction, _InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR );
	}
}

// ===================================================================================
// Main cloud computation
// This shader outputs 2 colors:
//	Color0 = InScattered energy
//	Color1 = Extinction
//
struct PS_OUT
{
	float4	InScattering	: SV_TARGET0;
	float4	ExtinctionZ		: SV_TARGET1;
};

PS_OUT	PS_Render( PS_IN _In )
{
	// Compute intersections with ground & cloud planes
	float3	CameraView = float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * _BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * _BufferInvSize.y), 1.0 );
	float	CameraViewLength = length( CameraView );
			CameraView /= CameraViewLength;
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
	float	Z = ReadDownscaledDepth( _In.Position.xy );

	// Render sky
	PS_OUT	Out;
	Out.ExtinctionZ.w = Z;	// Store the Z at which we computed the cloud
	RenderSky( CameraPositionKm, View, Z, CameraViewLength, _In.AmbientSkyColor, _In.AmbientSkyColorNoCloud, Out.ExtinctionZ.xyz, Out.InScattering.xyz, Out.InScattering.w );

// Out.InScattering = 0.0;
// Out.ExtinctionZ.xyz = float3( _In.Position.xy * _BufferInvSize, 0.0 );
// Out.InScattering = float4( 1, 0, 0, 0 );
// Out.ExtinctionZ = float4( 0, 0, 0, 0 );

	return Out;
}


// ===================================================================================
// Combination of in-scattering and extinction buffers with the source buffer
Texture2D	_SourceBuffer;
float3		_VolumeBufferInvSize;
Texture2D	_VolumeTextureInScattering;
Texture2D	_VolumeTextureExtinction;

float		_BilateralThreshold = 10.0;
float2		_RefinementZThreshold = float2( 50.0, 0.0 );

float4	PS_Combine( PS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * _BufferInvSize;
	float4	SourceColor = _SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	float3	CameraView = float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * _BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * _BufferInvSize.y), 1.0 );
	float	CameraViewLength = length( CameraView );
			CameraView /= CameraViewLength;
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
	float	Z = ReadDepth( _In.Position.xy );

	/////////////////////////////////////////////////////////////
	// Jitter UVs based on clouds' pseudo-ZBuffer
// float	CloudDistanceKm = _VolumeTextureInScattering.SampleLevel( NearestClamp, UV, 0 ).w;	// This is roughly the distance to the cloud
// float3	CloudPositionKm = CameraPositionKm + CloudDistanceKm * View;
// float3	UVW = _JitterNoiseSize * CloudPositionKm;
// float2	NoiseValue  = 2.0 * _NoiseTexture0.SampleLevel( VolumeSampler, UVW, 0.0 ).xy - 1.0;
// 		NoiseValue += _NoiseTexture0.SampleLevel( VolumeSampler, 6.0 * UVW, 0.0 ).xy - 0.5;
// 
// float	ScreenHalfSizeKm = CameraData.y * CloudDistanceKm;		// Height of the screen in kilometers at cloud distance
// float	Jitter2D = _SamplingJitterAmplitude / ScreenHalfSizeKm;	// Projected 2D jitter
// UV += Jitter2D * NoiseValue;


	/////////////////////////////////////////////////////////////
	// Sample cloud
#if 1	// BOURRIN SAMPLING
	float4	InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 );
	float4	ExtinctionZ = _VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );

#elif 0	// ACCURATE REFINEMENT
	float2	DownscaledPosition = 0.5 + floor( 0.25 * (_In.Position.xy - 0.5) );
//return _In.Position.x == 1023.5 ? float4( 1, 0, 0, 0 ) : float4( 0, 0, 0, 0 );
	float2	DownscaledUV = DownscaledPosition * 4.0 * _BufferInvSize;
//return float4( DownscaledUV, 0, 0 );

//float4	ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV, 0 );
//return float4( ExtinctionZ.xy, 0, 0 );
//return ExtinctionZ.x == 1.5 ? float4( 1, 0, 0, 0 ) : float4( 0, 0, 0, 0 );

//	float	CloudZ = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV, 1.0 ).x;
	float	CloudZ = _DownscaledZBuffer.SampleLevel( LinearClamp, UV, 1.0 ).x;
//return DownscaledZ == 1.5 ? float4( 1, 0, 0, 0 ) : float4( 0, 0, 0, 0 );

//return float4( 100.0 * abs( ExtinctionZ.xy - DownscaledPosition ), 0, 0 );
//return float4( 100.0 * abs( ExtinctionZ.xy - UV ), 0, 0 );
//return 100.0 * abs( DownscaledZ - DownscaledPosition.y );

	float4	InScatteringOpticalDepth;
	float4	ExtinctionZ;

	if ( abs( Z - CloudZ ) > _RefinementZThreshold.x )
	{	// Refinement
		RenderSky( CameraPositionKm, View, Z, CameraViewLength, ExtinctionZ.xyz, InScatteringOpticalDepth.xyz, InScatteringOpticalDepth.w );

// DEBUG REFINEMENT
if ( _RefinementZThreshold.y > 0.0 )
{
	ExtinctionZ = 0.0;
	InScatteringOpticalDepth = float4( 10, 0, 0, 0 );
}
	}
	else
	{	// Easy sampling
		InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 );
		ExtinctionZ = _VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );
	}

#else	// "SMART" INTERPOLATION
	// TODO... I have no "smart" idea right now
	float2	DownscaledPosition = 0.5 + floor( 0.25 * (_In.Position.xy - 0.5) );
	float3	dUV = float3( 4.0 * _BufferInvSize, 0.0 );
	float2	DownscaledUV = DownscaledPosition * dUV.xy;

// 	float4	Extinction00 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV, 0 );
// 	float4	Extinction01 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
// 	float4	Extinction10 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
// 	float4	Extinction11 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
// 	float4	InScattering00 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV, 0 );
// 	float4	InScattering01 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
// 	float4	InScattering10 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
// 	float4	InScattering11 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
// 
// 	float	Z00 = Extinction00.w;
// 	float	Z01 = Extinction01.w;
// 	float	Z10 = Extinction10.w;
// 	float	Z11 = Extinction11.w;

	float	Z00 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV, 0 ).x;
	float	Z01 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 ).x;
	float	Z10 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 ).x;
	float	Z11 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 ).x;

	float	DZ00 = abs(Z-Z00);
	float	DZ01 = abs(Z-Z01);
	float	DZ10 = abs(Z-Z10);
	float	DZ11 = abs(Z-Z11);
	float	MaxDz = max( max( max( DZ00, DZ01 ), DZ10 ), DZ11 );
	float	MinDz = min( min( min( DZ00, DZ01 ), DZ10 ), DZ11 );

	float4	InScatteringOpticalDepth;
	float4	ExtinctionZ;
	if ( MaxDz > _RefinementZThreshold.x )
	{
		if ( DZ00 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV, 0 );
		}
		else if ( DZ01 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
		}
		else if ( DZ10 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
		}
		else if ( DZ11 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
		}
	}
	else
	{	// Easy sampling
		InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 );
		ExtinctionZ = _VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );
	}
#endif


/////////////////////////////////////////////////////////////
// DEBUG
float2	DebugUV = UV / 0.2;
		DebugUV.y -= 4.0;
if ( DebugUV.x > 0.0 && DebugUV.x < 1.0 && DebugUV.y > 0.0 && DebugUV.y < 1.0 )
{
//	return float4( DebugUV, 0, 0 );
 	return _DeepShadowMap0.SampleLevel( LinearClamp, DebugUV, 0.0 );	// Display the shadow map
//	return _SkyLightProbe.SampleLevel( LinearClamp, DebugUV, 0.0 );		// Display the ambient sky probe
//	return _TexSHConvolution.SampleLevel( LinearClamp, DebugUV, 0.0 );		// Display the ambient sky probe
//	return float4( DebugUV, 0, 0 );

// 	float2	PhiTheta = _SkyProbeAngles.xy + DebugUV * _SkyProbeAngles.zw;
// 	float3	View = float3( cos( PhiTheta.x ) * sin( PhiTheta.y ), cos( PhiTheta.y ), sin( PhiTheta.x ) * sin( PhiTheta.y ) );
// 
// 	const float	f0 = 0.28209479177387814347403972578039;		// 0.5 / Sqrt(PI);
// 	const float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0
// 
// 	return float4( f0,
// 		-f1 * View.x,
// 		 f1 * View.y,
// 		-f1 * View.z );
}

//// if ( UV.x < 0.5 )
// 	return float4( InScatteringOpticalDepth.xyz, 0 );
// else
// //	return ExtinctionZ;
// //	return InScatteringOpticalDepth.w;
// 	return exp( -_VolumeTextureInScattering.SampleLevel( NearestClamp, UV, 0 ).w );

// Display the clouds' "ZBuffer"
//if ( UV.x > 0.5 )
//  	return 0.01 * _VolumeTextureInScattering.SampleLevel( NearestClamp, UV, 0 ).w;
//	return 0.001 * _DownscaledZBuffer.SampleLevel( LinearClamp, UV, 1 ).x;
//
// DEBUG
/////////////////////////////////////////////////////////////


//return float4( ExtinctionZ.xyz, 1.0 );


	/////////////////////////////////////////////////////////////
	// Display the Sun as background

	// 	149.5978875 = Distance to the Sun in millions of kilometers
	// 	0.695       = Radius of the Sun in millions of kilometers
	float	CosAngle = 0.9998;	// Cos( SunCoverAngle ) but arbitrary instead of physical computation, otherwise the Sun is really too small
	float	DotSun = dot( View, _SunDirection );

	float	Infinity = CameraData.w-5.0;
	SourceColor += _SunIntensity  * step( Infinity, Z ) * smoothstep( CosAngle, 1.0, DotSun );

	return float4( InScatteringOpticalDepth.xyz + ExtinctionZ.xyz * SourceColor.xyz, SourceColor.w );
}


// ===================================================================================
// Ambient sky computation
// This shader computes the sky light and clouds in a small hemispherical texture
//
float3	_SkyProbePositionKm;	// Position of the probe in WORLD units
float4	_SkyProbeAngles;		// XY=Phi/Theta Min ZW=Delta Phi/Theta

float4	PS_RenderAmbientSky( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * _BufferInvSize;
	float2	PhiTheta = _SkyProbeAngles.xy + UV * _SkyProbeAngles.zw;
	float2	SinCosPhi; sincos( PhiTheta.x, SinCosPhi.x, SinCosPhi.y );
	float2	SinCosTheta; sincos( PhiTheta.y, SinCosTheta.x, SinCosTheta.y );
	float3	View = float3( SinCosPhi.x * SinCosTheta.x, SinCosTheta.y, SinCosPhi.y * SinCosTheta.x );	// Phi=0 => +Z Phi=PI/2 => +X

	// Compute hit distances
	float	HitDistanceCloudInKm = ComputeSphereIntersectionExit( _SkyProbePositionKm, View, _CloudAltitudeKm.x );
	float	HitDistanceCloudOutKm = ComputeSphereIntersectionExit( _SkyProbePositionKm, View, _CloudAltitudeKm.y );
	float	HitDistanceAtmosphereKm = ComputeSphereIntersectionExit( _SkyProbePositionKm, View, ATMOSPHERE_THICKNESS );

	// Compute earth shadow
	float2	EarthShadowDistances = ComputeEarthShadowDistances( 0.0, View );

	// Compute light phases
	float	CosTheta = dot( View, _SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	OneMinusG = 1.0 - _MiePhaseAnisotropy;
	float	PhaseMie = OneMinusG * OneMinusG / pow( 1.0 + _MiePhaseAnisotropy*_MiePhaseAnisotropy - 2.0 * _MiePhaseAnisotropy * CosTheta, 1.5 );
	float2	Phases = INV_4PI * float2( PhaseRayleigh, PhaseMie );

	// Compute cloud mip level
	// TODO: Unique for the entire probe !
	float	CloudMipLevel = 1.0;

	// Trace
	float3	InScattering = 0.0;
	float3	Extinction = 1.0;
	float	CloudOpticalDepth = 0.0;
	TraceAir( _SkyProbePositionKm, View, 0.0, HitDistanceCloudInKm, Extinction, InScattering, EarthShadowDistances, Phases, true, STEPS_COUNT_AIR_GOD_RAYS_PROBE );
	TraceCloud( _SkyProbePositionKm, View, HitDistanceCloudInKm, HitDistanceCloudOutKm, Extinction, InScattering, EarthShadowDistances, Phases, 0.0.xxx, 0.0.xxx, CloudMipLevel, STEPS_COUNT_CLOUDS_PROBE );
	TraceAir( _SkyProbePositionKm, View, HitDistanceCloudOutKm, HitDistanceAtmosphereKm, Extinction, InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_CLOUDS_PROBE );

	// Combine
	float3	SpaceBackground = 0.0;	// TODO: Use "ambient" space background

// ENCODE DIRECT SUNLIGHT FOR TESTING
// float	CosAngle = 0.998;	// Cos( SunCoverAngle ) but arbitrary instead of physical computation, otherwise the Sun is really too small
// float	DotSun = dot( View, _SunDirection );
// SpaceBackground = smoothstep( CosAngle, 1.0, DotSun ) * _SunIntensity;

	return float4( SpaceBackground * Extinction + InScattering, 0.0 );
}

float4	PS_RenderAmbientSkyNoClouds( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * _BufferInvSize;
	float2	PhiTheta = _SkyProbeAngles.xy + UV * _SkyProbeAngles.zw;
	float2	SinCosPhi; sincos( PhiTheta.x, SinCosPhi.x, SinCosPhi.y );
	float2	SinCosTheta; sincos( PhiTheta.y, SinCosTheta.x, SinCosTheta.y );
	float3	View = float3( SinCosPhi.x * SinCosTheta.x, SinCosTheta.y, SinCosPhi.y * SinCosTheta.x );	// Phi=0 => +Z Phi=PI/2 => +X

	// Compute hit distances
	float	HitDistanceAtmosphereKm = ComputeSphereIntersectionExit( _SkyProbePositionKm, View, ATMOSPHERE_THICKNESS );

	// Compute earth shadow
	float2	EarthShadowDistances = ComputeEarthShadowDistances( 0.0, View );

	// Compute light phases
	float	CosTheta = dot( View, _SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	OneMinusG = 1.0 - _MiePhaseAnisotropy;
	float	PhaseMie = OneMinusG * OneMinusG / pow( 1.0 + _MiePhaseAnisotropy*_MiePhaseAnisotropy - 2.0 * _MiePhaseAnisotropy * CosTheta, 1.5 );
	float2	Phases = INV_4PI * float2( PhaseRayleigh, PhaseMie );

	// Trace
	float3	InScattering = 0.0;
	float3	Extinction = 1.0;
	TraceAir( _SkyProbePositionKm, View, 0.0, HitDistanceAtmosphereKm, Extinction, InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR_GOD_RAYS_PROBE );

	// Combine
	float3	SpaceBackground = 0.0;	// TODO: Use "ambient" space background

// ENCODE DIRECT SUNLIGHT FOR TESTING
// float	CosAngle = 0.998;	// Cos( SunCoverAngle ) but arbitrary instead of physical computation, otherwise the Sun is really too small
// float	DotSun = dot( View, _SunDirection );
// SpaceBackground = smoothstep( CosAngle, 1.0, DotSun ) * _SunIntensity;

	return float4( SpaceBackground * Extinction + InScattering, 0.0 );
}

// ===================================================================================
// Ambient sky convolution into SH basis
// This shader convolves the input ambient sky hemispherical texture into SH
//
Texture2D	_SkyLightProbe;
Texture2D	_TexSHConvolution;

static const int	LIGHT_PROBE_SIZE_X = 64;
static const int	LIGHT_PROBE_SIZE_Y = 32;
static const float	dPhi = 2.0 * 3.1415926535897932384626433832795 / LIGHT_PROBE_SIZE_X;
static const float	dTheta = 0.5 * 3.1415926535897932384626433832795 / LIGHT_PROBE_SIZE_Y;
static const float	SOLID_ANGLE_CONSTANT = dPhi * dTheta;

struct PS_OUT_SH
{
	float4	SHCoeffs0	: SV_TARGET0;
	float4	SHCoeffs1	: SV_TARGET1;
	float4	SHCoeffs2	: SV_TARGET2;
};

PS_OUT_SH	PS_ConvolveAmbientSky( VS_IN _In )
{
	const float	f0 = 0.28209479177387814347403972578039;		// 0.5 / Sqrt(PI);
	const float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0

	float3	ResultSH[4] = { 0.0.xxx, 0.0.xxx, 0.0.xxx, 0.0.xxx };
	float2	UV;
	for ( int Y=0; Y < LIGHT_PROBE_SIZE_Y; Y++ )
	{
		UV.y = Y * _BufferInvSize.y;

		for ( int X=0; X < LIGHT_PROBE_SIZE_X; X++ )
		{
			UV.x = X * _BufferInvSize.x;

			float2	PhiTheta = _SkyProbeAngles.xy + UV * _SkyProbeAngles.zw;
			float2	SinCosPhi; sincos( PhiTheta.x, SinCosPhi.x, SinCosPhi.y );
			float2	SinCosTheta; sincos( PhiTheta.y, SinCosTheta.x, SinCosTheta.y );
			float3	View = float3( SinCosPhi.x * SinCosTheta.x, SinCosTheta.y, SinCosPhi.y * SinCosTheta.x );	// Phi=0 => +Z Phi=PI/2 => +X

			// Sample sky radiance
			float3	Radiance = SOLID_ANGLE_CONSTANT * SinCosTheta.x * _SkyLightProbe.SampleLevel( NearestClamp, UV, 0.0 ).xyz;

			// Sample SH coeffs for that direction
//			float4	SHCoeffs = _TexSHConvolution.SampleLevel( NearestClamp, UV, 0.0 );

			// Create the SH coefficients in the specified direction
			// From http://www1.cs.columbia.edu/~ravir/papers/envmap/envmap.pdf
			float4	SHCoeffs = float4(	f0,
										f1 * View.x,
										f1 * View.y,
										f1 * View.z );

			// Accumulate SH
			ResultSH[0] += SHCoeffs.x * Radiance;
			ResultSH[1] += SHCoeffs.y * Radiance;
			ResultSH[2] += SHCoeffs.z * Radiance;
			ResultSH[3] += SHCoeffs.w * Radiance;
		}
	}

	// Pack 4 RGB SH coefficients into 3 RGBA values
	PS_OUT_SH	Out;
	Out.SHCoeffs0 = float4( ResultSH[0], ResultSH[1].x );
	Out.SHCoeffs1 = float4( ResultSH[1].yz, ResultSH[2].xy );
	Out.SHCoeffs2 = float4( ResultSH[2].z, ResultSH[3] );
	return Out;
}

// ===================================================================================
technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Ambient() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Render() ) );
	}
}

technique10 RenderProbe
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_RenderAmbientSky() ) );
	}
}

technique10 RenderProbeNoClouds
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_RenderAmbientSkyNoClouds() ) );
	}
}

technique10 ConvolveProbe
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ConvolveAmbientSky() ) );
	}
}

technique10 Combine
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Ambient() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Combine() ) );
	}
}

// technique10 RenderDeepShadowMap
// {
// 	pass P0
// 	{
// 		SetVertexShader( CompileShader( vs_4_0, VS() ) );
// 		SetGeometryShader( 0 );
// 		SetPixelShader( CompileShader( ps_4_0, PS_Shadow() ) );
// 	}
// }
