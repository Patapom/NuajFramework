// Renders opaque objects into the deferred rendering render targets
// This shader is a full renderer that handles diffuse, specular and normal textures
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"

// Per-instance parameter
float4x4	Local2World : LOCAL2WORLD;

// Standard render parameters for current material
cbuffer	PerMaterial
{
	float4		AmbientColor;

	float4		DiffuseColor;
	float		DiffuseFactor;
	bool		HasDiffuseTexture;

	float4		SpecularColor;
	float		SpecularFactor;
	float		Shininess;
	bool		HasSpecularTexture;

	bool		HasNormalTexture;
}

// Standard textures
Texture2D	DiffuseTexture : TEX_DIFFUSE;
Texture2D	SpecularTexture : TEX_SPECULAR;
Texture2D	NormalTexture : TEX_NORMAL;

struct VS_IN
{
	float3	Position		: POSITION;
	float3	Normal			: NORMAL;
	float3	Tangent			: TANGENT;
	float2	UV				: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position		: SV_POSITION;
 	float3	Position		: POSITION;		// Position in CAMERA space
 	float3	Normal			: NORMAL;		// Normal in CAMERA space
	float3	Tangent			: TANGENT;		// Tangent in CAMERA space
	float2	UV				: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	float4x4	Local2Camera = mul( Local2World, World2Camera );
	float4		WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World );
//	float3		BiTangent = cross( _In.Normal, _In.Tangent );

	PS_IN	Out;
	Out.__Position = mul( WorldPosition, World2Proj );
	Out.Position = mul( float4( _In.Position, 1.0 ), Local2Camera ).xyz;
	Out.Normal = mul( float4( _In.Normal, 0.0 ), Local2Camera ).xyz;
	Out.Tangent = mul( float4( _In.Tangent, 0.0 ), Local2Camera ).xyz;
	Out.UV = _In.UV;

	return Out;
}

PS_OUT PS( PS_IN _In )
{
	// Read back normal
	float3	Normal = normalize( _In.Normal );
	if ( HasNormalTexture )
	{
		float3	Tangent = normalize( _In.Tangent );
		float3	BiTangent = cross( Normal, Tangent );
		float3	NormalSample = 2.0 * NormalTexture.Sample( LinearWrap, _In.UV ).xyz - 1.0;
		
		// Compute normal from normal map
		Normal = NormalSample.x * Tangent + NormalSample.y * BiTangent + NormalSample.z * Normal;
	}

	// Compute diffuse color
	float4	DiffuseAlbedo = DiffuseFactor * DiffuseColor;
	if ( HasDiffuseTexture )
		DiffuseAlbedo = DiffuseFactor * DiffuseTexture.Sample( LinearWrap, _In.UV );

	// Compute specular color
	float4	SpecularAlbedo = SpecularFactor * SpecularColor;
	if ( HasSpecularTexture )
		SpecularAlbedo = SpecularFactor * SpecularTexture.Sample( LinearWrap, _In.UV );

DiffuseAlbedo *= 0.2;	// Dummy albedo factor (cf. http://en.wikipedia.org/wiki/Albedo)
SpecularAlbedo *= 1.0;

	float	SpecularPower = 20.0;

	// Write resulting data into the MRT
	return WriteDeferredMRT( DiffuseAlbedo.xyz, SpecularAlbedo.xyz, SpecularPower, Normal, _In.Position.z );
}


// ===================================================================================
//
technique10 DrawDeferred
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
