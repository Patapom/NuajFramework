// Displays grass using method 0
//
#include "../Camera.fx"
#include "../DirectionalLighting.fx"
#include "ComputeWind.fx"

float			Time = 0.0;
float			GrassSize = 0.1;
float3			WindPosition0 = float3( 0.0, 0.0, 0.0 );
float3			WindPosition1 = float3( 0.0, 0.0, 0.0 );
float3			WindDirection = float3( 1.0, 0.0, 0.0 );
Texture2DArray	GrassTextures;

struct VS_IN
{
	// Per-vertex data
	float2	UV				: TEXCOORD0;
	// Per-instance data
	float3	TouffPos		: POSITION;
	float3	Color			: COLOR;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
// 	float3	WorldPosition	: TEXCOORD0;
// 	float3	WorldNormal		: NORMAL;
	float4	Color			: COLOR;
	float2	UV				: TEXCOORD1;
};

PS_IN VS( VS_IN _In )
{
	// Compute world position
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;
	float3	WorldPosition = _In.TouffPos + GrassSize * ((2.0 * _In.UV.x - 1.0) * Right + 2.0 * (1.0 - _In.UV.y) * Up);

	// Disturb by wind
	float	WindInfluence = 1.0 - _In.UV.y;	// Full wind at top of the grass
	float3	WindDisplacement = ComputeGrassWindDisplacement( _In.TouffPos );
	WorldPosition += WindInfluence * WindDisplacement;

	// Compute diffuse lighting
	float3	Normal = float3( 0.0, 1.0, 0.0 );	// Should better follow terrain orientation
	float	DiffuseDot = saturate( dot( Normal, LightDirection ) );
	float3	VertexColor = lerp( float3( 1.0, 1.0, 1.0 ), _In.Color, _In.UV.y );	// White on top, defined color on bottom
	float3	DiffuseColor = DiffuseDot * VertexColor * LightColor.xyz;

	// Compute alpha based on camera view of the tuft (viewing grass vertically will reveal the face-cam sprites)
//	float3	ToPosition = normalize( Camera2World[3].xyz - WorldPosition );
// 	float	Alpha = saturate( dot( Normal, ToPosition ) );
// 	Alpha *= Alpha;
// 	Alpha *= Alpha;
//	Alpha = 1.0 - Alpha;
	float	Alpha = 1.0;

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1.0 ), World2Proj );
// 	Out.WorldPosition = WorldPosition;
// 	Out.WorldNormal = float3( 0.0, 1.0, 0.0 );
	Out.Color = float4( DiffuseColor, Alpha );
	Out.UV  = _In.UV;

	return Out;
}

float4 PS( PS_IN _In ) : SV_TARGET0
{
	return _In.Color * GrassTextures.Sample( LinearWrap, float3( _In.UV, 0.0 ) );
}


// ===================================================================================
//
technique10 DrawGrass
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
