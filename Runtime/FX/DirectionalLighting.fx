// ===============================================================
// Include this file to obtain directional lighting support
// ===============================================================
//
#if !defined(DIRECTIONAL_LIGHT_SUPPORT_FX)
#define DIRECTIONAL_LIGHT_SUPPORT_FX

cbuffer	cbLighting
{
	float3		LightDirection : LIGHT_DIRECTION;	// Vector pointing toward the light
	float4		LightColor : LIGHT_COLOR;
}

float	DotLight( float3 _Normal )
{
	return saturate( dot( LightDirection, _Normal ) );
}

float	DotLightUnclamped( float3 _Normal )
{
	return dot( LightDirection, _Normal );
}

#endif