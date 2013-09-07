// This shader renders the sky probes
// Search for TODO !
//
//#include "../Camera.fx"
//#include "../Samplers.fx"
//#include "../ReadableZBufferSupport.fx"
//#include "AmbientSkySupport.fx"

// These must be declared BEFORE the include
float3		_SkyProbePositionKm;	// Position of the probe in WORLD units
float4		_SkyProbeAngles;		// XY=Phi/Theta Min ZW=Delta Phi/Theta

#define RENDER_SKY_PROBE
#include "CloudComputeSupport.fx"

// Ray-marching precision
static const int	STEPS_COUNT_CLOUDS = 32;
static const int	STEPS_COUNT_AIR = 8;
static const int	STEPS_COUNT_AIR_GOD_RAYS = 12;

struct VS_IN
{
	float4	Position		: SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

// ===================================================================================
// Ambient sky computation
// This shader computes the sky light and clouds in a small hemispherical texture
//
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
	TraceAir( _SkyProbePositionKm, View, 0.0, HitDistanceCloudInKm, Extinction, InScattering, EarthShadowDistances, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
	TraceCloud( _SkyProbePositionKm, View, HitDistanceCloudInKm, HitDistanceCloudOutKm, Extinction, InScattering, EarthShadowDistances, Phases, 0.0.xxx, 0.0.xxx, CloudMipLevel, STEPS_COUNT_CLOUDS );
	TraceAir( _SkyProbePositionKm, View, HitDistanceCloudOutKm, HitDistanceAtmosphereKm, Extinction, InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR );

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
	TraceAir( _SkyProbePositionKm, View, 0.0, HitDistanceAtmosphereKm, Extinction, InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR_GOD_RAYS );

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
