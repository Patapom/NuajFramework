// This shader displays a generated voxel mesh from a stream output
//
#include "../Camera.fx"
#include "../DirectionalLighting.fx"
#include "../LinearToneMapping.fx"

// ===================================================================================

Texture2D TextureGrass;
Texture2D TextureRock;

SamplerState LinearWrap
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	WorldPosition	: TEXCOORD0;
	float3	WorldNormal		: TEXCOORD1;
};

PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), World2Proj );
	Out.WorldPosition = _In.Position;
	Out.WorldNormal = _In.Normal;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
//	return float4( 0.5 * (1 + _In.WorldNormal), 1 );
//	return saturate( abs( dot( _In.WorldNormal, LightDirection ) ) );
//	return 0.1 * float4( 0.1, 0.2, 0.5, 1 ) + saturate( dot( _In.WorldNormal, LightDirection ) );

	float	fBlendY = saturate( pow( _In.WorldNormal.y, 1.0 ) );
	float	fBlendXZ = saturate( pow( abs( _In.WorldNormal.x ), 1.0 ) );

	float2	GrassUV = 0.1 * _In.WorldPosition.xz;
	float3	GrassColor = TextureGrass.Sample( LinearWrap, GrassUV ).rgb;
	float2	RockUV;
			RockUV.x = 0.2 * _In.WorldPosition.x;
			RockUV.y = 0.2 * _In.WorldPosition.y;
	float3	RockColorX = TextureRock.Sample( LinearWrap, RockUV ).rgb;
			RockUV.x = 0.2 * _In.WorldPosition.z;
			RockUV.y = 0.2 * (_In.WorldPosition.y + 1);
	float3	RockColorZ = TextureRock.Sample( LinearWrap, RockUV ).rgb;
	float3	RockColor = float3( 0.8, 0.6, 0.5 ) * lerp( RockColorX, RockColorZ, fBlendXZ );

	float	fDiffuseFactor = saturate( dot( _In.WorldNormal, LightDirection ) );
	float3	DiffuseColor = 2.0 * lerp( RockColor, GrassColor, fBlendY );
	float3	AmbientColor = 0.3 * float3( 0.1, 0.2, 0.5 );

	return float4( fToneMappingFactor * (lerp( AmbientColor, DiffuseColor, fDiffuseFactor) ), 1 );
}


// ===================================================================================
// Default technique displays the volume using quads and ray-marching
technique10 DisplayVoxel
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
