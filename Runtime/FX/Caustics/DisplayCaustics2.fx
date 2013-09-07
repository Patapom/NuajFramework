// This shader simply displays the caustics texture
//
#include "../Camera.fx"

// ===================================================================================

#include "ComputeWallColor.fx"

float4x4	Local2World;

struct VS_IN
{
	float3	Position		: POSITION;
	float3	Normal			: NORMAL;
	float2	UV				: TEXCOORD0;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	WorldPosition	: WORLDPOSITION;
	float3	Normal			: NORMAL;
	float2	UV				: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	float4x4	Local2Proj = mul( Local2World, World2Proj );

	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), Local2Proj );
	Out.WorldPosition = _In.Position;
	Out.Normal = _In.Normal;
	Out.UV = _In.UV;

	return Out;
}

float4	PS( PS_IN _In, uint _PrimitiveID : SV_PRIMITIVEID ) : SV_TARGET0
{
//	return float4( 0.1 * _PrimitiveID, 1, 0, 1 );
//	return float4( _In.UV, 0, 1 );
//	return float4( _In.WorldPosition, 1 );
	int		FaceIndex = _PrimitiveID / 2;
	return float4( ComputeWallColor( FaceIndex, _In.UV, _In.WorldPosition ), 1 );
}

// ===================================================================================
technique10 DisplayCaustics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
