// This shader displays a density function previously stored into a 3D texture
//

float4x4	Local2World : LOCAL2WORLD;
#include "../Camera.fx"

Texture3D DensityVolumeTexture;

SamplerState LinearClamp
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

struct VS_IN
{
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	WorldPosition	: TEXCOORD0;
	float3	UVW				: TEXCOORD1;
};

PS_IN VS( VS_IN _In )
{
// 	float3	Position = 2.0 * _In.Position;	// As voxels are 4 units long and our quad coordinates are in [-1,+1] !
// 	float4	WorldPosition = mul( float4( Position, 1 ), Local2World );

	float4	Offset = _In.Position.x * Camera2World[0] + _In.Position.y * Camera2World[1];
	float4	WorldPosition = Local2World[3] + 2.0 * Offset;

	PS_IN	Out;
	Out.Position = mul( WorldPosition, World2Proj );
	Out.WorldPosition = WorldPosition.xyz;
	Out.UVW = float3( 0.5, 0.5, 0.5 ) + 0.5 * Offset.xyz;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = float3( _In.UVW.x, 1.0-_In.UVW.y, _In.UVW.z );
//	return float4( UVW, 1 );

	float	fDensity = 1.0 * DensityVolumeTexture.SampleLevel( LinearClamp, _In.UVW, 0.0 ).r;

	if ( fDensity >= 0.0 )
		return float4( fDensity.xx, 0, 0.5 );

	return float4( -fDensity.xx, 0, 0.5 );
}


// ===================================================================================
// Default technique displays the volume using quads and ray-marching
technique10 DisplayDensityVolume
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
