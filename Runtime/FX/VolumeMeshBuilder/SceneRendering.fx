// Renders the scene's albedo and normal into 2 render targets
//
#include "../Camera.fx"
#include "../Samplers.fx"

bool		HasDiffuseTexture;
float4		DiffuseColor;
Texture2D	DiffuseTexture;
bool		HasNormalTexture;
Texture2D	NormalTexture;

bool		PreMultiplyByAlpha;

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
	float3	Normal		: NORMAL;
	float3	Tangent		: TANGENT;
	float2	UV			: TEXCOORD0;
};

struct PS_OUT
{
	float4	DiffuseAlpha	: SV_TARGET0;
	float4	Normal			: SV_TARGET1;
};

PS_IN VS( VS_IN _In )
{
	float3	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World ).xyz;
	float3	WorldNormal = mul( float4( _In.Normal, 0.0 ), Local2World ).xyz;
	float3	WorldTangent  = mul( float4( _In.Tangent, 0.0 ), Local2World ).xyz;

	PS_IN	Out;
	Out.__Position = mul( float4( WorldPosition, 1.0 ), World2Proj );
	Out.Normal = WorldNormal;
	Out.Tangent = WorldTangent;
	Out.UV = _In.UV;

	return Out;
}

PS_OUT	PS( PS_IN _In )
{
	float4	TexColorDiffuse = HasDiffuseTexture ? DiffuseTexture.Sample( LinearWrap, _In.UV ) : float4( DiffuseColor.xyz, 1.0 );
	if ( PreMultiplyByAlpha ) TexColorDiffuse.xyz *= TexColorDiffuse.w;	// Use premultiplied alpha

//	float3	TexColorNormal = HasNormalTexture ? 2.0 * NormalTexture.Sample( LinearWrap, _In.UV ).xyz - 1.0 : float3( 0.0, 0.0, 1.0 );
	float3	TexColorNormal = float3( 0.0, 0.0, 1.0 );
	if ( PreMultiplyByAlpha ) TexColorNormal *= TexColorDiffuse.w;	// Use premultiplied alpha

// Store baked lighting already
// float3	ToLight = normalize( float3( 1, 1, 1 ) );
// TexColorDiffuse.xyz *= saturate( dot( TexColorNormal, ToLight ) );

	_In.Tangent = normalize( _In.Tangent );
	_In.Normal = normalize( _In.Normal );
	float3	BiTangent = normalize( cross( _In.Tangent, _In.Normal ) );
	float3	WorldNormal = _In.Tangent * TexColorNormal.x + BiTangent * TexColorNormal.y + _In.Normal * TexColorNormal.z;

	PS_OUT	Out;
	Out.DiffuseAlpha = TexColorDiffuse;
	Out.Normal = float4( WorldNormal, 0.0 );

	return Out;
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
