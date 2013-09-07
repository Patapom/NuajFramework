// Renders the scene to screen, applying the volume radiosity algorithm
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "VolumeRadiositySupport.fx"
#include "ShadowMapSupport.fx"

bool		HasDiffuseTexture;
float4		DiffuseColor;
Texture2D	DiffuseTexture;
bool		HasNormalTexture;
Texture2D	NormalTexture;

float3		LightDirection;
float3		LightColor;
float		DirectLightingBoost;
float		IndirectLightingBoost;

float4x4	Local2World;

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
	float3	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World ).xyz;
	float3	WorldNormal = mul( float4( _In.Normal, 0.0 ), Local2World ).xyz;
	float3	WorldTangent  = mul( float4( _In.Tangent, 0.0 ), Local2World ).xyz;

	PS_IN	Out;
	Out.__Position = mul( float4( WorldPosition, 1.0 ), World2Proj );
	Out.Position = WorldPosition;
	Out.Normal = WorldNormal;
	Out.Tangent = WorldTangent;
	Out.UV = _In.UV;

	return Out;
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
	// Sample albedo & normal
	float4	TexColorDiffuse = HasDiffuseTexture ? DiffuseTexture.Sample( LinearWrap, _In.UV ) : float4( DiffuseColor.xyz, 1.0 );

//	float3	TexColorNormal = HasNormalTexture ? 2.0 * NormalTexture.Sample( LinearWrap, _In.UV ).xyz - 1.0 : float3( 0.0, 0.0, 1.0 );
	float3	TexColorNormal = float3( 0.0, 0.0, 1.0 );

	// Build tangent space
	_In.Tangent = normalize( _In.Tangent );
	_In.Normal = normalize( _In.Normal );
	float3	BiTangent = normalize( cross( _In.Tangent, _In.Normal ) );
	float3	WorldNormal = _In.Tangent * TexColorNormal.x + BiTangent * TexColorNormal.y + _In.Normal * TexColorNormal.z;

	// Perform direct diffuse lighting
	float	DotDiffuse = saturate( dot( WorldNormal, LightDirection ) );
			DotDiffuse *= 0.31830988618379067153776752674503;	// Diffuse Reflectance = Albedo / PI

	// Apply indirect diffuse lighting
	float3	IndirectDiffuse = GetIndirectLightingStatic( _In.Position, 663.0 );
//	float3	IndirectDiffuse = GetIndirectLightingDynamic( _In.Position, WorldNormal );
//	float3	IndirectDiffuse = GetIndirectLightingStatic2( _In.Position, 663.0 );

	return TexColorDiffuse.xyz * (DirectLightingBoost * LightColor * DotDiffuse * GetShadowPCF( _In.Position ) + IndirectLightingBoost * IndirectDiffuse);
}


// ===================================================================================
//
technique10 RenderScene
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
