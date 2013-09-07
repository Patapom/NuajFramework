// Default shader that is used to replace the Phong-type materials
//

float4x4	Local2World : LOCAL2WORLD;
float4x4	Camera2World : CAMERA2WORLD;
float4x4	World2Proj : WORLD2PROJ;

#include "DirectionalLighting.fx"
#include "DirectionalLighting2.fx"
#include "LinearToneMapping.fx"


float4		AmbientColor;

float4		DiffuseColor;
float		DiffuseFactor;
Texture2D	DiffuseTexture : TEX_DIFFUSE;
bool		HasDiffuseTexture;

float4		SpecularColor;
float		SpecularFactor;
float		Shininess;
Texture2D	SpecularTexture : TEX_SPECULAR;
bool		HasSpecularTexture;

Texture2D	NormalTexture : TEX_NORMAL;
bool		HasNormalTexture;


SamplerState DiffuseTextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float3	Position : POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
	float3	WorldPosition : TEXCOORD1;
};

PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );

	PS_IN	Out;
	Out.WorldPosition = WorldPosition.xyz;
	Out.Position = mul( WorldPosition, World2Proj );
	Out.Normal = mul( float4( _In.Normal, 0 ), Local2World ).xyz;
	Out.Tangent = mul( float4( _In.Tangent, 0 ), Local2World ).xyz;
	Out.BiTangent = mul( float4( _In.BiTangent, 0 ), Local2World ).xyz;
	Out.UV = _In.UV;

	return	Out;
}

float4 PS( PS_IN _In ) : SV_Target
{
	float3	Normal = normalize( _In.Normal );
	if ( HasNormalTexture )
	{
		float3	Tangent = normalize( _In.Tangent );
		float3	BiTangent = normalize( _In.BiTangent );
		float3	NormalSample = 2.0 * NormalTexture.Sample( DiffuseTextureSampler, _In.UV ).xyz - 1.0;
		
		// Compute normal from normal map
		Normal = NormalSample.x * Tangent + NormalSample.y * BiTangent + NormalSample.z * Normal;
	}
//	return float4( Normal, 1 );

	float3	ToCamera = Camera2World[3].xyz - _In.WorldPosition;
	float	fDistance2View = length( ToCamera );
			ToCamera /= fDistance2View;

	// Compute diffuse color
	float4	DiffuseTextureColor = DiffuseColor;
	if ( HasDiffuseTexture )
		DiffuseTextureColor = DiffuseTexture.Sample( DiffuseTextureSampler, _In.UV );

	float	fDiffuseDot0 = DiffuseFactor * saturate( dot( Normal, LightDirection ) );
	float	fDiffuseDot1 = DiffuseFactor * saturate( dot( Normal, LightDirection2 ) );

	// Compute specular color
	float4	SpecularTextureColor = SpecularFactor * SpecularColor;
	if ( HasSpecularTexture )
		SpecularTextureColor = SpecularFactor * SpecularTexture.Sample( DiffuseTextureSampler, _In.UV );

	float3	Half = normalize( LightDirection + ToCamera );
	float	fSpecularDot0 = pow( saturate( dot( Half, Normal ) ), Shininess );
	Half = normalize( LightDirection2 + ToCamera );
	float	fSpecularDot1 = pow( saturate( dot( Half, Normal ) ), Shininess );

	return float4( ApplyToneMapping(
		(AmbientColor
		+ fDiffuseDot0 * (DiffuseTextureColor + fSpecularDot0 * SpecularTextureColor) * LightColor
		+ fDiffuseDot1 * (DiffuseTextureColor + fSpecularDot1 * SpecularTextureColor) * LightColor2).rgb ),
		DiffuseTextureColor.a );
}

technique10 Phong
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}