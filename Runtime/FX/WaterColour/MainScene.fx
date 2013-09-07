// Default shader that is used to replace the Phong-type materials
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"
#include "ShadowMapSupport.fx"
#include "LightSupport.fx"
#include "MotionBlurSupport.fx"

float4x4	Local2World : LOCAL2WORLD;

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


struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float3	Tangent		: TANGENT;
	float3	BiTangent	: BITANGENT;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Normal		: NORMAL;
	float3	Tangent		: TANGENT;
	float3	BiTangent	: BITANGENT;
	float2	UV			: TEXCOORD0;
	float3	Position	: TEXCOORD1;
	float3	Velocity	: VELOCITY;
};

PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );

	PS_IN	Out;
	Out.Position = WorldPosition.xyz;
	Out.Velocity = ComputeVelocity( WorldPosition.xyz );;
	Out.__Position = mul( WorldPosition, World2Proj );;
	Out.Normal = mul( float4( _In.Normal, 0 ), Local2World ).xyz;
	Out.Tangent = mul( float4( _In.Tangent, 0 ), Local2World ).xyz;
	Out.BiTangent = mul( float4( _In.BiTangent, 0 ), Local2World ).xyz;
	Out.UV = _In.UV;

	return	Out;
}

PS_OUT PS( PS_IN _In )
{
	float3	Normal = normalize( _In.Normal );
	if ( HasNormalTexture )
	{
		float3	Tangent = normalize( _In.Tangent );
		float3	BiTangent = normalize( _In.BiTangent );
		float3	NormalSample = 2.0 * NormalTexture.Sample( LinearWrap, _In.UV ).xyz - 1.0;
		
		// Compute normal from normal map
		Normal = NormalSample.x * Tangent + NormalSample.y * BiTangent + NormalSample.z * Normal;
	}
//	return float4( Normal, 1 );

	float3	ToCamera = Camera2World[3].xyz - _In.Position;
	float	fDistance2View = length( ToCamera );
			ToCamera /= fDistance2View;

	// Compute diffuse color
	float4	DiffuseTextureColor = DiffuseColor;
	if ( HasDiffuseTexture )
		DiffuseTextureColor = DiffuseTexture.Sample( LinearWrap, _In.UV );

	// Compute specular color
	float4	SpecularTextureColor = SpecularFactor * SpecularColor;
	if ( HasSpecularTexture )
		SpecularTextureColor = SpecularFactor * SpecularTexture.Sample( LinearWrap, _In.UV );

	// Compute Key/Rim/Fill lighting
	float4	Diffuse, Specular;
	ComputeLighting( _In.Position, Normal, -ToCamera, Shininess, Diffuse, Specular );

	PS_OUT	Out;
	Out.Color0 = float4( (AmbientColor + Diffuse * DiffuseTextureColor + Specular * SpecularTextureColor).xyz, DiffuseTextureColor.a );
	Out.Color1 = float4( _In.Velocity, 0.0 );

//	Out.Color0 = DiffuseTextureColor;

//	DEBUG shadowing
// 	Out.Color0  = float4( 1, 0, 0, 1 ) * (1.0 - ComputeShadowKey( _In.Position ));
// 	Out.Color0 += float4( 0, 1, 0, 1 ) * (1.0 - ComputeShadowRim( _In.Position ));
// 	Out.Color0 += float4( 0, 0, 1, 1 ) * (1.0 - ComputeShadowFill( _In.Position ));
// //	float4	ShadowDebug = ComputeShadowDebugKey( _In.Position );
// //	Out.Color0 = float4( 0.5 * (1.0+ShadowDebug.xy), 0, 0 );
// //	Out.Color0 = float4( 0.1 * log( ShadowDebug.zzz ) / ShadowExponent, 0 );
// 	Out.Color1 = 0.0;
// 	return Out;

	return Out;
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