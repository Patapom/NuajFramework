// ===============================================================
// Include this file to obtain shadow map support
// ===============================================================
//
#if !defined( SHADOW_MAP_SUPPORT )
#define SHADOW_MAP_SUPPORT

#include "../Samplers.fx"

static const float	ShadowBias = -0.01;

float3		ShadowMapInvSize	: SHADOWMAPINVSIZE;
float2		ShadowMapProjFactor	: SHADOWMAPPROJFACTOR;
float4x4	World2Light			: WORLD2LIGHT;
float4x4	Light2ShadowMap		: LIGHT2SHADOWMAP;
float4x4	World2ShadowMap		: WORLD2SHADOWMAP;
Texture2D	ShadowMap			: SHADOWMAP;

// Returns the shadowing of a point
//
float	GetShadow( float3 _WorldPosition, float _BiasFactor=1.0 )
{
	float4	ShadowPos = mul( float4( _WorldPosition, 1.0 ), World2ShadowMap );	// Gives us the Z we'll compare with
	ShadowPos.z += _BiasFactor * ShadowBias;
	ShadowPos.z /= ShadowPos.w;
	float2	ShadowUV = (float2( 1.0+ShadowPos.x, 1.0-ShadowPos.y )+0.5*ShadowMapInvSize.xy) * ShadowMapProjFactor;
	float	ShadowZ = ShadowMap.SampleLevel( LinearClamp, ShadowUV, 0.0 ).x;
	return step( ShadowPos.z, ShadowZ );
}

// Returns the shadowing of a point using PCF
//
float	GetShadowPCF( float3 _WorldPosition )
{
	float4	ShadowPos = mul( float4( _WorldPosition, 1.0 ), World2ShadowMap );	// Gives us the Z we'll compare with
	ShadowPos.z += ShadowBias;
	ShadowPos.z /= ShadowPos.w;
	float2	ShadowUV = (float2( 1.0+ShadowPos.x, 1.0-ShadowPos.y )+0.5*ShadowMapInvSize.xy) * ShadowMapProjFactor;

	float	KernelRadius = 0.5;
	float	Shadow = 0.0;
	float	ShadowZ = ShadowMap.SampleLevel( LinearClamp, ShadowUV, 0.0 ).x;										Shadow += step( ShadowPos.z, ShadowZ );
			ShadowZ = ShadowMap.SampleLevel( LinearClamp, ShadowUV + KernelRadius * ShadowMapInvSize.xz, 0.0 ).x;	Shadow += step( ShadowPos.z, ShadowZ );
			ShadowZ = ShadowMap.SampleLevel( LinearClamp, ShadowUV - KernelRadius * ShadowMapInvSize.xz, 0.0 ).x;	Shadow += step( ShadowPos.z, ShadowZ );
			ShadowZ = ShadowMap.SampleLevel( LinearClamp, ShadowUV + KernelRadius * ShadowMapInvSize.zy, 0.0 ).x;	Shadow += step( ShadowPos.z, ShadowZ );
			ShadowZ = ShadowMap.SampleLevel( LinearClamp, ShadowUV - KernelRadius * ShadowMapInvSize.zy, 0.0 ).x;	Shadow += step( ShadowPos.z, ShadowZ );

	return 0.2 * Shadow;
}

#endif