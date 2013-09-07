// Renders trees
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../Atmosphere/SkySupport.fx"
#include "DeferredSupport.fx"

float4x4	Local2World;
Texture2D	DiffuseTexture;

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float3	Tangent		: TANGENT;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float3	Tangent		: TANGENT;
	float2	UV			: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	float4x4	Local2Camera = mul( Local2World, World2Camera );
	float4		CameraPosition = mul( float4( _In.Position, 1.0 ), Local2Camera );
	float3		CameraNormal = mul( float4( _In.Normal, 1.0 ), Local2Camera ).xyz;
	float3		CameraTangent = mul( float4( _In.Tangent, 1.0 ), Local2Camera ).xyz;

	PS_IN	Out;
	Out.__Position = mul( CameraPosition, Camera2Proj );
	Out.Position = CameraPosition.xyz;
	Out.Normal = CameraNormal;
	Out.Tangent = CameraTangent;
	Out.UV = _In.UV;

	return Out;
}

PS_OUT	PS( PS_IN _In )
{
	float4	DiffuseColor = DiffuseTexture.Sample( LinearWrap, _In.UV );
	clip( DiffuseColor.w - 0.5 );

	return WriteDeferredMRT( DiffuseColor.xyz, 0.0, 1.0, _In.Normal, _In.Position.z );
}


// ===================================================================================
//
technique10 RenderTree
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
