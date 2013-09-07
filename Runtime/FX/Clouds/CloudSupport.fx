// This shader interface provides support for the cloud's shadow map and lightning spots
//
#if !defined(CLOUD_SUPPORT_FX)
#define CLOUD_SUPPORT_FX

#include "../Camera.fx"
#include "../Samplers.fx"
#include "../Atmosphere/SkySupport.fx"
#include "3DNoise.fx"

#define DEEP_SHADOW_MAP_HI_RES	// Define this to use 8 deep shadow map values instead of 4 (low-res)

static const float	INV_LOG2 = 1.4426950408889634073599246810019;

SamplerState VolumeSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
//	AddressV = Clamp;	// Vertically, the noise doesn't wrap so clouds have a solid top and bottom limit
	AddressV = Wrap;
	AddressW = Wrap;
};

float		CloudSigma_s			: CLOUD_SIGMA_S;			// Scattering coefficient
float		CloudSigma_t			: CLOUD_SIGMA_T;			// Extinction coefficient = Scattering + Absorption coefficient
float		DensitySumFactor		: CLOUD_DENSITY_SUM_FACTOR;

// Deep shadow maps
float4		ShadowRectangle			: CLOUD_SHADOW_RECTANGLE;	// XY = World position (X,Z) | ZW = World Size (X,Z)
float		ShadowDistance			: CLOUD_SHADOW_DISTANCE;	// The maximum distance encoded in the shadow map
Texture2D	DeepShadowMap0			: CLOUD_DSM0;
Texture2D	DeepShadowMap1			: CLOUD_DSM1;

// Cloud layer altitude
float		CloudPlaneHeightTop		: CLOUD_PLANE_HEIGHT_TOP;
float		CloudPlaneHeightBottom	: CLOUD_PLANE_HEIGHT_BOTTOM;

// Lightning
float3		LightningPosition		: CLOUD_LIGHTNING_POSITION;
float		LightningIntensity		: CLOUD_LIGHTNING_INTENSITY;


// ===================================================================================
// Computes the traversed optical depth at current position within the cloud
//
float	ComputeCloudOpticalDepth( float3 _WorldPosition )
{
	// Project world position onto top cloud plane
	float	Distance2Top = (CloudPlaneHeightTop - _WorldPosition.y) / SunDirection.y;
	float3	TopCloudPosition = _WorldPosition + SunDirection * Distance2Top;

	// Compute shadow coordinates
	float2	UV = (TopCloudPosition.xz + ShadowRectangle.xy) * ShadowRectangle.zw;	// Offset + scale

#if defined(DEEP_SHADOW_MAP_HI_RES)
	// Retrieve densities
	float4	Density0 = DeepShadowMap0.SampleLevel( LinearMirror, UV, 0 );
	float4	Density1 = DeepShadowMap1.SampleLevel( LinearMirror, UV, 0 );

	// Interpolate densities
	float	CloudDistance = max( 0.0, min( Distance2Top, ShadowDistance ) );
	Distance2Top = 9.0 * CloudDistance / ShadowDistance;	// Normalized distance is divided into 9 equal steps
	Distance2Top += 0.5;									// First density value is at half a step

	float	Thickness = 0.0;
	Thickness += saturate( Distance2Top ) * Density0.x;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density0.y;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density0.z;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density0.w;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density1.x;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density1.y;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density1.z;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density1.w;

#else	// Low-res version uses only 1 DSM tap and 4 samples
	// Retrieve densities
	float4	Density0 = DeepShadowMap0.SampleLevel( LinearMirror, UV, 0 );

	// Interpolate densities
	float	CloudDistance = max( 0.0, min( Distance2Top, ShadowDistance ) );
	Distance2Top = 5.0 * CloudDistance / ShadowDistance;	// Normalized distance is divided into 5 equal steps
	Distance2Top += 0.5;									// First density value is at half a step

	float	Thickness = 0.0;
	Thickness += saturate( Distance2Top ) * Density0.x;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density0.y;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density0.z;	Distance2Top -= 1.0;
	Thickness += saturate( Distance2Top ) * Density0.w;	Distance2Top -= 1.0;
#endif

	return DensitySumFactor * CloudDistance * Thickness;
}

// ===================================================================================
// Simple helper to compute lighting for standard objects
float3	ComputeIncomingLight( float3 _WorldPosition )
{
	// =============================================
	// Sample density at current altitude and optical depth in Sun direction
	float	SkyAltitude = WorldUnit2Kilometer * max( 0, _WorldPosition.y );
	float4	OpticalDepth = ComputeOpticalDepth( SkyAltitude, SunDirection, float3( 0, 1, 0 ) );

	// =============================================
	// Retrieve sun light attenuated when passing through the atmosphere
	float3	OpticalDepth_cloud = ComputeCloudOpticalDepth( _WorldPosition );
	float3	SunExtinction = exp( -Sigma_Rayleigh * OpticalDepth.z - Sigma_Mie * OpticalDepth.w - CloudSigma_t * OpticalDepth_cloud );

	return _SunIntensity * SunExtinction;
}

// ===================================================================================
// Computes lighting by lightning
//	_WorldPosition, the position of the point to light
//	_WorldNormal, the normal of the point to light
//
float	ScatteringAnisotropyLightning = 0.0;

float	ComputeLightningLightingCloud( float3 _WorldPosition, float3 _View )
{
	float3	ToLightning = LightningPosition - _WorldPosition;
	float	SqDistance2Lightning = dot( ToLightning, ToLightning );
	float	CurrentLightningIntensity = LightningIntensity / max( 1.0, SqDistance2Lightning );

	ToLightning /= sqrt( SqDistance2Lightning );
	float	LightningDot = dot( _View, ToLightning );
	float	Den = 1.0 / (1.0 + ScatteringAnisotropyLightning * LightningDot);
	float	LightningPhase = (1.0 - ScatteringAnisotropyLightning*ScatteringAnisotropyLightning) * Den * Den;

	return CurrentLightningIntensity * LightningPhase;
}

float	ComputeLightningLightingSurface( float3 _WorldPosition, float3 _WorldNormal )
{
	float3	ToLightning = LightningPosition - _WorldPosition;
	float	SqDistance2Lightning = dot( ToLightning, ToLightning );
	float	CurrentLightningIntensity = LightningIntensity / max( 1.0, SqDistance2Lightning );

	ToLightning /= sqrt( SqDistance2Lightning );

	float	DotLightning = saturate( dot( ToLightning, _WorldNormal ) );

	return 1000.0 * DotLightning * CurrentLightningIntensity;
}

#endif	// CLOUD_SUPPORT_FX
