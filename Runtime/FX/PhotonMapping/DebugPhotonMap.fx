// This shader displays the content of the photon map.
//
#include "../Camera.fx"
#include "../Samplers.fx"

Texture3D	TexPhotonDirections;
Texture3D	TexPhotonFlux;
Texture3D	TexDistanceField;
float		QuadSize;
float		SlicesCount;

struct VS_IN
{
	float3	Position			: POSITION;
	uint	SliceIndex			: SV_INSTANCEID;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float3	UVW					: TEXCOORD0;
};

PS_IN	VS( VS_IN _In )
{
	float3	Position = float3( QuadSize * _In.Position.xy, -QuadSize + 2.0 * QuadSize * _In.SliceIndex / (SlicesCount-1.0) );
	
	PS_IN	Out;
	Out.__Position = mul( float4( Position, 1.0 ), World2Proj );
	Out.UVW = (Position + QuadSize) / (2.0 * QuadSize);

	return	Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float4	DirectionCount = TexPhotonDirections.SampleLevel( LinearClamp, _In.UVW, 0 );
	float4	Flux = TexPhotonFlux.SampleLevel( LinearClamp, _In.UVW, 2 );
	float4	DistanceField = TexDistanceField.SampleLevel( LinearClamp, _In.UVW, 0 );

//	return 0.04 * float4( _In.UVW, 0.0 );
//	return 0.3 * float4( normalize( DirectionCount.xyz ), 0 );
//	return 0.002 * float4( DistanceField.xyz, 0 );
//	return 0.002 * float4( abs(DistanceField.xyz), 0 );
//	return 0.001 * DistanceField.w;
	return 0.1 * Flux;
	return float4( 0.2 * abs( DirectionCount.xyz ) / max( 1e-3, DirectionCount.w ), 0 );
	return DirectionCount.w;
}

technique10 DebugPhotonMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
