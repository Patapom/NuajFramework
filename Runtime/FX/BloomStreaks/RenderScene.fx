// This shader renders the main scene
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../DirectionalLighting.fx"

float4x4	Local2World;
float3		AmbientColor;
float3		DiffuseColor;
float		SpecularFactor;
float3		SpecularColor;
float		Shininess;
float3		EmissiveColor;

bool		HasDiffuseTexture;
Texture2D	DiffuseTexture;
bool		HasNormalTexture;
Texture2D	NormalTexture;

struct VS_IN
{
	float3	Position			: POSITION;
	float3	Normal				: NORMAL;
	float3	Tangent				: TANGENT;
	float3	BiTangent			: BITANGENT;
	float2	UV					: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float3	Position			: POSITION;
	float3	Normal				: NORMAL;
	float3	Tangent				: TANGENT;
	float3	BiTangent			: BITANGENT;
	float2	UV					: TEXCOORD0;
};

PS_IN	VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World );

	PS_IN	Out;
	Out.__Position = mul( WorldPosition, World2Proj );
	Out.Position = WorldPosition.xyz;
	Out.Normal = mul( float4( _In.Normal, 0.0 ), Local2World ).xyz;
	Out.Tangent = mul( float4( _In.Tangent, 0.0 ), Local2World ).xyz;
	Out.BiTangent = mul( float4( _In.BiTangent, 0.0 ), Local2World ).xyz;
	Out.UV = _In.UV;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	View = normalize( Camera2World[3].xyz - _In.Position );

	float3	LocalDiffuseColor = HasDiffuseTexture ? DiffuseTexture.Sample( LinearWrap, _In.UV ).xyz : DiffuseColor;
	float3	NormalColor = HasNormalTexture ? NormalTexture.Sample( LinearWrap, _In.UV ).xyz : float3( 0.5, 0.5, 1.0 );
	float3	LocalNormal = 2.0 * NormalColor - 1.0;
	LocalNormal.z *= 0.1;
	float3	WorldNormal = normalize( LocalNormal.x * _In.Tangent + LocalNormal.y * _In.BiTangent + LocalNormal.z * _In.Normal );

	// Compute diffuse lighting
	float	DotDiffuse = saturate( dot( WorldNormal, LightDirection ) );
	float	Diffuse = DotDiffuse;

	// Compute specular lighting
	float3	ReflectedView = reflect( View, WorldNormal );
	float	DotSpecular = pow( saturate( dot( ReflectedView, LightDirection ) ), Shininess );
	float	Specular = SpecularFactor * DotSpecular;

	// Compute emissive lighting
	float	DotEmissive = pow( saturate( dot( View, WorldNormal ) ), 0.25 );

	// Compute light phase
	float	LightPhase = lerp( 0.1 * DotDiffuse, DotSpecular, saturate( Specular - Diffuse ) );
			LightPhase += 2.0 * DotEmissive * max( max( EmissiveColor.x, EmissiveColor.y ), EmissiveColor.z );	// Emissive color has huge light phase to perform different streaks

//LightPhase = 0.0;	// Bloom only
//LightPhase = 1.0;	// Streaks only

	// Finalize lighting
	return float4( AmbientColor + 4.0 * DotEmissive * max( 1e-3, EmissiveColor ) + LightColor.xyz * (LocalDiffuseColor * Diffuse + SpecularColor * Specular), LightPhase );
}

// ===================================================================================
technique10 RenderScene
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
