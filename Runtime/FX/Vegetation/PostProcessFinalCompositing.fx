// Final image composition post-process
// This post-process combines the materials and the lighting to form the final HDR image yet to be tone-mapped and displayed to the screen
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../DirectionalLighting.fx"
#include "DeferredSupport.fx"

float2		BufferInvSize;

// Shadow Mapping support
// Texture2D	ShadowMap : SHADOWMAP;
// float4x4	Camera2Shadow : CAMERA2SHADOW;
// float4x4	Light2Camera : LIGHT2CAMERA;
// float3		LightCameraCenter : LIGHT_CAMERA_CENTER;


struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN VS( VS_IN _In ) { return _In; }

float4 PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	View = normalize( float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 ) );
	float3	ToLight = mul( float4( LightDirection, 0.0 ), World2Camera ).xyz;

	// =================================================================================
	// Read depth & normal for actual pixel to light
	float3	Emissive, DiffuseAlbedo, SpecularAlbedo, Normal;
	float	Depth, SpecularPower, Extinction;
	ReadDeferredMRT( UV, Emissive, Extinction, DiffuseAlbedo, SpecularAlbedo, SpecularPower, Normal, Depth );

	// =================================================================================
	// Perform lighting
	float	DotDiffuse = saturate( dot( ToLight, Normal ) );

	float3	ReflectedView = reflect( View, Normal );
	float	DotSpecular = pow( saturate( dot( ReflectedView, ToLight ) ), max( 1e-3, SpecularPower ) );

 	return float4( Emissive + Extinction * LightColor.xyz * (DotDiffuse * DiffuseAlbedo + DotSpecular * SpecularAlbedo), 0.0 );
// 	return mul( float4( Normal, 0.0 ), Camera2World );
//	return float4( Emissive, 0.0 );
}


// ===================================================================================
//
technique10 PostProcess
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
