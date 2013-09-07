// This shader displays clouds and sky
// Search for TODO !
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"
#include "CloudSupport.fx"

// Ray-marching precision
static const int	STEPS_COUNT_CLOUDS = 128;
static const int	STEPS_COUNT_AIR = 8;
static const int	STEPS_COUNT_AIR_GOD_RAYS = 48;

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

float		_CloudOpacityFactor = 100.0;				// Factor for cloud's pseudo ZBuffer
float		_JitterNoiseSize = 1.0;						// Noise size for jittering
float		_SamplingJitterAmplitude = 1.0;				// UV jittering amplitude in kilometer

struct VS_IN
{
	float4	Position		: SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

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
// Traces through air
float	_FarClipAirKm = 10.0;

void	TraceAir( float3 _CameraPositionKm, float3 _View, float _DistanceInKm, float _DistanceOutKm, inout float3 _Extinction, inout float3 _InScatteredEnergy, float2 _TerminatorDistance, float2 _Phases, uniform bool _bBelowClouds, uniform int _StepsCount )
{
	float3	CurrentPositionKm = _CameraPositionKm + _DistanceInKm * _View;
	float	DistanceKm = _DistanceOutKm - _DistanceInKm;	// Distance we need to trace

	float	OriginalTraceDistanceKm = DistanceKm;
// 	if ( _bBelowClouds )
// 		DistanceKm = min( _FarClipAirKm, DistanceKm );		// Clip to some value...

	float	StepSizeKm = DistanceKm / _StepsCount;
	float3	StepKm = StepSizeKm * _View;
	CurrentPositionKm += 0.5 * StepKm;	// Start at half a step
	float	Distance2CameraKm = _DistanceInKm;

	// Ray-march the view ray
	float3	AccumulatedLightRayleigh = 0.0.xxx;
	float3	AccumulatedLightMie = 0.0.xxx;

	float3	SumExtinctionCoefficients = 0.0;
	float3	SumInScattering = 0.0;

	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float4	OpticalDepth = ComputeOpticalDepth( CurrentPositionKm.y, _SunDirection, float3( 0, 1, 0 ) );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth.x;
		float	Rho_aerosols = OpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere
#ifdef FAKE_DSM
		float3	OpticalDepth_cloud = 0.0;
		if ( _bBelowClouds )
			OpticalDepth_cloud = ComputeCloudOpticalDepth( CurrentPositionKm, length( CurrentPositionKm - _CameraPositionKm ) );
		float3	SunExtinction = exp( -_Sigma_Rayleigh * OpticalDepth.z - _Sigma_Mie * OpticalDepth.w - _CloudSigma_t * OpticalDepth_cloud );
#else
		float3	CloudExtinction = 1.0;
		if ( _bBelowClouds )
			CloudExtinction = ComputeCloudExtinction( CurrentPositionKm, length( CurrentPositionKm - _CameraPositionKm ) );
		float3	SunExtinction = exp( -_Sigma_Rayleigh * OpticalDepth.z - _Sigma_Mie * OpticalDepth.w ) * CloudExtinction;
#endif
		float3	Light = _SunIntensity * SunExtinction;

		// =============================================
		// Compute in-scattered light
		float3	ScatteringRayleigh = Rho_air * _DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4;
		float	ScatteringMie = Rho_aerosols * _DensitySeaLevel_Mie;
		float3	InScatteringRayleigh = Light * ScatteringRayleigh * _Phases.x;
		float3	InScatteringMie = Light * ScatteringMie * _Phases.y;

		SumInScattering += InScatteringRayleigh + InScatteringMie;

		// =============================================
		// Accumulate in-scattered light (only if not in Earth's shadow)
		if ( Distance2CameraKm >= _TerminatorDistance.x && Distance2CameraKm <= _TerminatorDistance.y )
		{
			float3	ExtinctionAlongStep = _Extinction * StepSizeKm;
			AccumulatedLightRayleigh += InScatteringRayleigh * ExtinctionAlongStep;
			AccumulatedLightMie += InScatteringMie * ExtinctionAlongStep;
		}

		// =============================================
		// Accumulate extinction along view
		float3	CurrentExtinctionCoefficient = exp( -(_Sigma_Rayleigh * Rho_air + _Sigma_Mie * Rho_aerosols) * StepSizeKm );
		SumExtinctionCoefficients += CurrentExtinctionCoefficient;
		_Extinction *= CurrentExtinctionCoefficient;

		// =============================================
		// March
		CurrentPositionKm += StepKm;
		Distance2CameraKm += StepSizeKm;
	}

	_InScatteredEnergy += AccumulatedLightRayleigh + AccumulatedLightMie;

// 	// Finish by extrapolating extinction & in-scattering
// 	if ( OriginalDistance > Distance )
// 	{
// 		float3	AverageExtinctionCoefficient = SumExtinctionCoefficients / _StepsCount;
// 		float3	AverageInScattering = SumInScattering / _StepsCount;
// 
// 		float	DeltaDistance = _WorldUnit2Kilometer * (OriginalDistance - Distance);
// 		float3	ExtrapolatedExtinction = exp( -AverageExtinctionCoefficient * DeltaDistance );
// 		_Extinction *= ExtrapolatedExtinction;
// 		_InScatteredEnergy += AverageInScattering * ExtrapolatedExtinction * DeltaDistance;
// 	}
}

#ifdef FAKE_DSM
float2	ComputeCloudDensityAndOpticalDepth( float3 _CameraPositionKm, float3 _PositionKm, float _MipLevel )
{
//	float	MipLevel = ComputeNoiseMipLevel( _CameraPositionKm, _PositionKm );
//	float	MipLevel = log2( max( 1.0001, _PositionKm.x ) );
	float	MipLevel = _MipLevel;	// We prefer to compute mip level once when entering the clouds because the log2 takes quite a lot of time !
	float	Distance2Camera = length( _PositionKm - _CameraPositionKm );
	return float2( ComputeCloudDensity( _PositionKm, Distance2Camera, MipLevel ), ComputeCloudOpticalDepth( _PositionKm, Distance2Camera ) );
}
#else
float2	ComputeCloudDensityAndExtinction( float3 _CameraPositionKm, float3 _PositionKm, float _MipLevel )
{
//	float	MipLevel = ComputeNoiseMipLevel( _CameraPositionKm, _PositionKm );
//	float	MipLevel = log2( max( 1.0001, _PositionKm.x ) );
	float	MipLevel = _MipLevel;	// We prefer to compute mip level once when entering the clouds because the log2 takes quite a lot of time !
	float	Distance2Camera = length( _PositionKm - _CameraPositionKm );
	return float2( ComputeCloudDensity( _PositionKm, Distance2Camera, MipLevel ), ComputeCloudExtinction( _PositionKm, Distance2Camera ) );
}
#endif

// ===================================================================================
// Traces through the cloud layer
float	_FarClipCloudKm = 10.0;

void	TraceCloud( float3 _CameraPositionKm, float3 _View, float _DistanceInKm, float _DistanceOutKm, inout float3 _Extinction, inout float3 _InScatteredEnergy, float2 _TerminatorDistance, float2 _Phases, float _MipLevel, inout float _CloudOpticalDepth, uniform int _StepsCount )
{
	float4	CurrentPositionKm = float4( _CameraPositionKm + _DistanceInKm * _View, _DistanceInKm );
	float	DistanceKm = _DistanceOutKm - _DistanceInKm;	// Distance we need to trace

	float	OriginalTraceDistanceKm = DistanceKm;
	DistanceKm = min( _FarClipCloudKm, DistanceKm );

	float4	StepKm = DistanceKm * float4( _View, 1.0 ) / _StepsCount;
	CurrentPositionKm += 0.5 * StepKm;	// Start at half a step

	// Compute cloud Light phases
	float	CosTheta = dot( _View, _SunDirection );

		// Strong forward phase
	float	OneMinusG = 1.0 - _ScatteringAnisotropyStrongForward;
	float	PhaseStrongForward = OneMinusG * OneMinusG / pow( 1.0 + _ScatteringAnisotropyStrongForward*_ScatteringAnisotropyStrongForward - 2.0 * _ScatteringAnisotropyStrongForward * CosTheta, 1.5 );
		// Forward phase
			OneMinusG = 1.0 - _ScatteringAnisotropyForward;
	float	PhaseForward = OneMinusG * OneMinusG / pow( 1.0 + _ScatteringAnisotropyForward*_ScatteringAnisotropyForward - 2.0 * _ScatteringAnisotropyForward * CosTheta, 1.5 );
		// Backward phase
			OneMinusG = 1.0 - _ScatteringAnisotropyBackward;
	float	PhaseBackward = OneMinusG * OneMinusG / pow( 1.0 + _ScatteringAnisotropyBackward*_ScatteringAnisotropyBackward - 2.0 * _ScatteringAnisotropyBackward * CosTheta, 1.5 );
		// Side phase
//	CosTheta = 0.4 + 0.6 * CosTheta;	// Add bias
	float	PhaseSide = saturate( pow( sqrt(1.0 - 0.8 * CosTheta*CosTheta), _ScatteringAnisotropySide ) );

	float	PhaseAmbient = _PhaseWeightSide * PhaseSide;
	float	PhaseDirect = _PhaseWeightSide2 * PhaseAmbient + _PhaseWeightStrongForward * PhaseStrongForward + _PhaseWeightForward * PhaseForward + _PhaseWeightBackward * PhaseBackward;

	PhaseAmbient *= INV_4PI;
	PhaseDirect *= INV_4PI;

	float3	AccumulatedLightRayleigh = 0.0.xxx;
	float3	AccumulatedLightMie = 0.0.xxx;
	float3	AccumulatedLightCloud = 0.0.xxx;

//	_CloudOpticalDepth = 0.0;
	_CloudOpticalDepth = _DistanceInKm;	// Start from cloud entry point
	float	CloudExtinction = 1.0;

	float3	SumExtinctionCoefficients = 0.0;
	float3	SumInScattering = 0.0;

//	[unroll] // <= Takes hell of a time to compile !
	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float4	OpticalDepth_air = ComputeOpticalDepth( CurrentPositionKm.y, _SunDirection, float3( 0.0, 1.0, 0.0 ) );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth_air.x;
		float	Rho_aerosols = OpticalDepth_air.y;

		// =============================================
		// Sample cloud density at current altitude and extrapolate optical depth in Sun direction from the shadow map


//_MipLevel = ComputeNoiseMipLevel( CurrentPositionKm.w );


#ifdef FAKE_DSM
		float2	DensityOpticalDepth = ComputeCloudDensityAndOpticalDepth( _CameraPositionKm, CurrentPositionKm.xyz, _MipLevel );
		float	Rho_cloud = DensityOpticalDepth.x;
		float	OpticalDepth_cloud = DensityOpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere & cloud
		float3	SunExtinction = _DirectionalFactor * exp( -_Sigma_Rayleigh * OpticalDepth_air.z - _Sigma_Mie * OpticalDepth_air.w - _CloudSigma_t * OpticalDepth_cloud );
#else
		float2	DensityExtinction = ComputeCloudDensityAndExtinction( _CameraPositionKm, CurrentPositionKm.xyz, _MipLevel );
		float	Rho_cloud = DensityExtinction.x;
		float	OpticalDepth_cloud = DensityExtinction.y;	// This is extinction, really, not optical depth...

		float3	SunExtinction = _DirectionalFactor * exp( -_Sigma_Rayleigh * OpticalDepth_air.z - _Sigma_Mie * OpticalDepth_air.w ) * OpticalDepth_cloud;
#endif
		float3	Light = _SunIntensity * SunExtinction;

		// =============================================
		// Invent a scattered light based on traversed cloud depth
		// The more light goes through cloud matter, the more it's scattered within the cloud
//		float3	ScatteredLight = _IsotropicFactor * _SkyZenith * _SunIntensity * _CloudSigma_s * SumRho_cloud;
		float3	ScatteredLight = _IsotropicFactor * 0.5 * (_SkyZenith + _SunIntensity) * _CloudSigma_s * OpticalDepth_cloud;

		// =============================================
		// Compute in-scattered light
		float3	ScatteringCoeffRayleigh = Rho_air * _DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4;
		float3	InScatteringRayleigh = Light * ScatteringCoeffRayleigh * _Phases.x;

		float	ScatteringCoeffMie = Rho_aerosols * _DensitySeaLevel_Mie;
		float3	InScatteringMie = Light * ScatteringCoeffMie * _Phases.y;

		float	ScatteringCoeffCloud = Rho_cloud * _CloudSigma_s;
		float3	InScatteringCloud  = Light * ScatteringCoeffCloud * PhaseDirect;
				InScatteringCloud += ScatteredLight * ScatteringCoeffCloud * PhaseAmbient;

		float3	InScatteringLightning = 0.0;//ComputeLightningLightingCloud( CurrentPositionKm.xyz, _View ).xxx;

		SumInScattering += InScatteringRayleigh + InScatteringMie + InScatteringCloud + InScatteringLightning;

		// =============================================
		// Accumulate in-scattered light (only if not in Earth's shadow)
		if ( CurrentPositionKm.w >= _TerminatorDistance.x && CurrentPositionKm.w <= _TerminatorDistance.y )
		{
			float3	ExtinctionAlongStep = _Extinction * StepKm.w;

			AccumulatedLightRayleigh += InScatteringRayleigh * ExtinctionAlongStep;
			AccumulatedLightMie += InScatteringMie * ExtinctionAlongStep;
			AccumulatedLightCloud += InScatteringCloud * ExtinctionAlongStep;
			AccumulatedLightCloud += InScatteringLightning * ExtinctionAlongStep;
		}

		// =============================================
		// Accumulate extinction along view
		float3	CurrentExtinctionCoefficient = _Sigma_Rayleigh * Rho_air + _Sigma_Mie * Rho_aerosols + _CloudSigma_t * Rho_cloud;
		SumExtinctionCoefficients += CurrentExtinctionCoefficient;
		_Extinction *= exp( -CurrentExtinctionCoefficient * StepKm.w );
//		_CloudOpticalDepth += _CloudSigma_t * Rho_cloud * StepKm.w;		// Accumulate cloud optical depth


		// Use optical depth to store pseudo Z
		float	StepCloudExtinction = exp( -_CloudOpacityFactor * _CloudSigma_t * Rho_cloud * StepKm.w );	// Extinction due to cloud opacity for this step
		CloudExtinction *= StepCloudExtinction;
		_CloudOpticalDepth += StepKm.w * CloudExtinction;													// We march less and less depending on cloud opacity so we somewhat end up inside the cloud when opacity is 0


		// =============================================
		// March
		CurrentPositionKm += StepKm;
	}

	_InScatteredEnergy += AccumulatedLightRayleigh + AccumulatedLightMie + AccumulatedLightCloud;

// 	// Finish by extrapolating extinction & in-scattering
// 	if ( OriginalDistance > Distance )
// 	{
// 		float3	AverageExtinctionCoefficient = SumExtinctionCoefficients / _StepsCount;
// 		float3	AverageInScattering = SumInScattering / _StepsCount;
// 
// 		float	DeltaDistance = _WorldUnit2Kilometer * (OriginalDistance - Distance);
// 		float3	ExtrapolatedExtinction = exp( -AverageExtinctionCoefficient * DeltaDistance );
// 		_Extinction *= ExtrapolatedExtinction;
// //		_InScatteredEnergy += AverageInScattering * ExtrapolatedExtinction * DeltaDistance;
// 	}
}

// ===================================================================================
// Renders the entire sky+clouds
void	RenderSky( float3 _CameraPositionKm, float3 _View, float _Z, float _ViewLength, out float3 _Extinction, out float3 _InScattering, out float _CloudOpticalDepth )
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
 
 	float2	TerminatorDistance = float2( 0.0, 1e6 );
	// TODO!

// 	if ( _SunDirection.y < 0.0 )
// 	{	// Project current position in the 2D plane orthogonal to the light to test the intersection with the shadow cylinder cast by the Earth
// 		float3	X = normalize( cross( _SunDirection, float3( 0.0, 1.0, 0.0 ) ) );
// 		float3	Z = cross( X, _SunDirection );
// 
// 		float2	P = float2( HeightKm*X.y, HeightKm*Z.y );			// Position in plane
// 		float2	V = float2( dot( View, X ), dot( View, Z ) );	// View in plane
// 		float	a = dot( V, V );
// 		float	b = dot( P, V );
// 		float	c = dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
// 		float	Delta = b*b - a*c;
// 		if ( Delta > 0.0 )
// 		{	// Distance til we hit the terminator, in WORLD space
// 			if ( c < 0 )
// 				TerminatorDistance = float2( 0.0, (-b+sqrt(Delta)) / max( 1e-4, _WorldUnit2Kilometer * a) );	// From inside
// 			else
// 				TerminatorDistance = float2( (-b-sqrt(Delta)) / max( 1e-4, _WorldUnit2Kilometer * a), 1e6 );	// From outside
// 		}
// 	}

	// Compute light phases
	float	CosTheta = dot( _View, _SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	OneMinusG = 1.0 - _MiePhaseAnisotropy;
	float	PhaseMie = OneMinusG * OneMinusG / pow( 1.0 + _MiePhaseAnisotropy*_MiePhaseAnisotropy - 2.0 * _MiePhaseAnisotropy * CosTheta, 1.5 );
	float2	Phases = INV_4PI * float2( PhaseRayleigh, PhaseMie );

	// Trace
	_InScattering = 0.0;
	_Extinction = 1.0;

	if ( H.bTraceAirBefore )
	{
		if ( H.bTraceAirBeforeAccurate )
			TraceAir( _CameraPositionKm, _View, H.HitDistanceStartKm, H.HitDistanceCloudInKm, _Extinction, _InScattering, TerminatorDistance, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( _CameraPositionKm, _View, H.HitDistanceStartKm, H.HitDistanceCloudInKm, _Extinction, _InScattering, TerminatorDistance, Phases, false, STEPS_COUNT_AIR );
	}

	_CloudOpticalDepth = 1.0;
	if ( H.bTraceCloud )
		TraceCloud( _CameraPositionKm, _View, H.HitDistanceCloudInKm, H.HitDistanceCloudOutKm, _Extinction, _InScattering, TerminatorDistance, Phases, CloudMipLevel, _CloudOpticalDepth, STEPS_COUNT_CLOUDS );

	if ( H.bTraceAirAfter )
	{
		if ( H.bTraceAirAfterAccurate )
			TraceAir( _CameraPositionKm, _View, H.HitDistanceCloudOutKm, H.HitDistanceEndKm, _Extinction, _InScattering, TerminatorDistance, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( _CameraPositionKm, _View, H.HitDistanceCloudOutKm, H.HitDistanceEndKm, _Extinction, _InScattering, TerminatorDistance, Phases, false, STEPS_COUNT_AIR );
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

PS_OUT	PS_Render( VS_IN _In )
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
	RenderSky( CameraPositionKm, View, Z, CameraViewLength, Out.ExtinctionZ.xyz, Out.InScattering.xyz, Out.InScattering.w );

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

float4	PS_Combine( VS_IN _In ) : SV_TARGET0
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
	float2	TerminatorDistance = float2( 0.0, 1e6 );
	// TODO!

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
	TraceAir( _SkyProbePositionKm, View, 0.0, HitDistanceCloudInKm, Extinction, InScattering, TerminatorDistance, Phases, true, STEPS_COUNT_AIR_GOD_RAYS_PROBE );
	TraceCloud( _SkyProbePositionKm, View, HitDistanceCloudInKm, HitDistanceCloudOutKm, Extinction, InScattering, TerminatorDistance, Phases, CloudMipLevel, CloudOpticalDepth, STEPS_COUNT_CLOUDS_PROBE );
	TraceAir( _SkyProbePositionKm, View, HitDistanceCloudOutKm, HitDistanceAtmosphereKm, Extinction, InScattering, TerminatorDistance, Phases, false, STEPS_COUNT_CLOUDS_PROBE );

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
// Renders the deep shadow map
//
// struct SHADOW_PS_OUT
// {
// 	float4	SumDensity0 : SV_TARGET0;
// #if defined(DEEP_SHADOW_MAP_HI_RES)
// 	float4	SumDensity1 : SV_TARGET1;
// #endif
// };
// 
// SHADOW_PS_OUT	PS_Shadow( VS_IN _In )
// {
// 	float2	UV = _In.Position.xy * _BufferInvSize;
// 
// 	// Retrieve the world position from the shadow UV
// 	float2	CloudDistancesKm;
// 	float3	WorldPositionKm = Shadow2World( UV, CloudDistancesKm );
// 
// 	// Compute mip level that will depend on the resolution of the deep shadow map
// 	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
// 	float	MipLevel = ComputeNoiseMipLevel( CameraPositionKm, WorldPositionKm );
// 
// 	// Compute the shadow vector
// 	float3	CloudPositionInKm = WorldPositionKm + CloudDistancesKm.x * _SunDirection;	// Position where we enter the cloud (into top sphere)
// 	float3	CloudPositionOutKm = WorldPositionKm + CloudDistancesKm.y * _SunDirection;	// Position where we exit the cloud (out of bottom sphere)
// 	float3	ShadowVectorKm = CloudPositionOutKm - CloudPositionInKm;
// #if defined(DEEP_SHADOW_MAP_HI_RES)
// 	ShadowVectorKm *= 0.111111111;	// Divide into 9 equal parts
// #else
// 	ShadowVectorKm *= 0.2;			// Divide into 5 equal parts
// #endif
// 
// 	WorldPositionKm = CloudPositionInKm + 0.5 * ShadowVectorKm;	// Start half a step within the cloud layer
// 
// 	// Accumulate 8 density levels into 2 render targets
// 	SHADOW_PS_OUT	Out;
// 
// 	// 1st render target
// 	Out.SumDensity0.x = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 	Out.SumDensity0.y = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 	Out.SumDensity0.z = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 	Out.SumDensity0.w = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 
// #if defined(DEEP_SHADOW_MAP_HI_RES)
// 	// 2nd render target										 	  															
// 	Out.SumDensity1.x = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 	Out.SumDensity1.y = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 	Out.SumDensity1.z = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );	WorldPositionKm += ShadowVectorKm;
// 	Out.SumDensity1.w = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), MipLevel );
// #endif
// 
// //Out.SumDensity0 = Out.SumDensity1 = 0.0;
// //Out.SumDensity0 = Out.SumDensity1 = 0.01 * length( WorldPositionKm - CameraPositionKm );
// //Out.SumDensity0 =  Out.SumDensity1 = 1.0 * float4( WorldPositionKm, 0 );
// //Out.SumDensity0 = Out.SumDensity1 = float4( _ShadowPlaneY, 0 );
// 
// 
// float	X = CameraData.x * CameraData.y * (2.0 * UV.x - 1.0);
// float	Z = 0;//120.0 * UV.y;
// float3	TestPositionKm = CameraPositionKm + mul( float4( Z*X, 0, Z, 0 ), Camera2World ).xyz;
// float	TestDistanceKm;
// 
// float2	QuadPos = World2Quad( TestPositionKm, TestDistanceKm );
// float4	MinMax = float4( min( _ShadowQuadVertices.x, _ShadowQuadVertices.z ), max( _ShadowQuadVertices.x, _ShadowQuadVertices.z ), min( _ShadowQuadVertices.y, _ShadowQuadVertices.w ), max( _ShadowQuadVertices.y, _ShadowQuadVertices.w ) );
// Out.SumDensity0 = Out.SumDensity1 = float4( (QuadPos.x - MinMax.x) / (MinMax.y - MinMax.x), (QuadPos.y - MinMax.z) / (MinMax.w - MinMax.z), 0, 0 );
// Out.SumDensity0 = Out.SumDensity1 = 0.001 * TestDistanceKm;
// Out.SumDensity0 = Out.SumDensity1 = 0.1 * length( QuadPos - float2( 0.0, 2706.798 ) );
// Out.SumDensity0 = Out.SumDensity1 = 1.0 * CameraPositionKm.y;	CameraPosition is OK !!!!
// return	Out;
// 
// float2	ShadowUV = World2Shadow( TestPositionKm, CloudDistancesKm, TestDistanceKm );
// 
// Out.SumDensity0 = Out.SumDensity1 = float4( ShadowUV, 0, 0 );
// 
// 	return Out;
// }

// ===================================================================================
technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
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
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
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
