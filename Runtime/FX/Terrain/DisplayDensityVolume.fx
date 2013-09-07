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
	float2	UV				: TEXCOORD1;
};

PS_IN VS( VS_IN _In )
{
	float3	Position = 2.0 * _In.Position;	// As voxels are 4 units long and our quad coordinates are in [-1,+1] !
	float4	WorldPosition = mul( float4( Position, 1 ), Local2World );

	PS_IN	Out;
	Out.Position = mul( WorldPosition, World2Proj );
	Out.WorldPosition = WorldPosition.xyz;
	Out.UV = _In.UV;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float2	UV = float2( _In.UV.x, 1-_In.UV.y );
//	return float4( UV, 0, 1 );

	float	fDensity = DensityVolumeTexture.SampleLevel( LinearClamp, float3( UV, 0.5 ), 0.0 ).r;
	if ( fDensity < 0.0f )
	{
		if ( fDensity > -1.0f )
			return float4( 0,0,0, 0 );
		else if ( fDensity > -2.0f )
			return float4( 1, 0.5, 0.5, 0 );
		else if ( fDensity > -3.0f )
			return float4( 0.5, 1, 0.5, 0 );
		else
			return float4( 0.5, 0.5, 1, 0 );
	}
	else if ( fDensity < 1.0f )
		return float4( 1, 1, 0, 0 );
	else if ( fDensity < 2.0f )
		return float4( 1, 0, 0, 0 );
	else if ( fDensity < 3.0f )
		return float4( 0, 1, 0, 0 );
	else
		return float4( 0, 0, 1, 0 );

	// Accumulate density along the Z axis
	float	fSumDensity = 0.0;
	for ( int i=0; i <= 64; i++ )
		fSumDensity += max( 0.0, DensityVolumeTexture.SampleLevel( LinearClamp, float3( UV, float(i) / 64.0 ), 0.0 ) ).r;

	fSumDensity /= 63.0 * 64.0 / 32.0;
//	fSumDensity *= 1000;

	return float4( fSumDensity.xxx, 1 );
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
