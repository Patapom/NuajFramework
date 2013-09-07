// The shadow-map shader simply transforms vertices into light
//	space then projects them into the light frustum and writes
//	the resulting depth in the shadow map...
//

float4x4	Local2World : LOCAL2WORLD;
float4x4	World2LightProj : WORLD2LIGHTPROJ;
float4		LightRanges : LIGHT_RANGES;				// Light range is stored in the ZW coordinates
float3		LightDirection : LIGHT_DIRECTION;
float3		ShadowBias : SHADOW_BIAS;			// Shadow bias in WORLD units

struct VS_IN
{
	float3	Position : POSITION;
	float3	Normal : NORMAL;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	WorldNormal : NORMAL;
	float3	ShadowMapPosition : TEXCOORD0;
};

struct PS_OUT
{
	float4	Color : SV_Target;
	float	Depth : SV_Depth;
};

PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );
	float4	LightProjectedPosition = mul( WorldPosition, World2LightProj );

	PS_IN	Out;
	Out.ShadowMapPosition = LightProjectedPosition.xyz;
	Out.WorldNormal = mul( float4( _In.Normal, 0 ), Local2World ).xyz;

	// Project depth in [0,1]
	LightProjectedPosition.z = saturate( (LightProjectedPosition.z - LightRanges.z) / (LightRanges.w - LightRanges.z) );

	Out.Position = LightProjectedPosition;

	return	Out;
}

PS_OUT	PS( PS_IN _In )
{
	PS_OUT	Out;

	// Add (sloped) bias ?
	//
	// Use depth gradient = float2(dz/du, dz/dv)
	// Make depth d follow tangent plane
	// d = d0 + dot(uv_offset, gradient)
	// [Schuler06] and [Isidoro06]

	// Default constant bias
	float	DefaultBias = 1.0;

	// Sloped bias
	float3	WorldNormal = normalize( _In.WorldNormal );
	float	SlopeBias = 1.0 - saturate( dot( WorldNormal, LightDirection ) );

	_In.ShadowMapPosition.z += ShadowBias.x * lerp( ShadowBias.y, ShadowBias.z, SlopeBias );

	// Project depth in [0,1]
	_In.ShadowMapPosition.z = saturate( (_In.ShadowMapPosition.z - LightRanges.z) / (LightRanges.w - LightRanges.z) );

	Out.Depth = _In.ShadowMapPosition.z;
	Out.Color = Out.Depth;					// Simply write depth as seen from light...

	return Out;
}

technique10 ComputeShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}