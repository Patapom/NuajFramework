// ===============================================================
// Include this file to obtain secondary directional lighting support
// ===============================================================
//
#if !defined(DIRECTIONAL_LIGHT2_SUPPORT_FX)
#define DIRECTIONAL_LIGHT2_SUPPORT_FX

cbuffer	cbLighting2
{
	float3		LightDirection2 : LIGHT_DIRECTION2;	// Vector pointing toward the light
	float4		LightColor2 : LIGHT_COLOR2;
}

float	DotLight2( float3 _Normal )
{
	return saturate( dot( LightDirection2, _Normal ) );
}

float	DotLightUnclamped2( float3 _Normal )
{
	return dot( LightDirection2, _Normal );
}

#endif