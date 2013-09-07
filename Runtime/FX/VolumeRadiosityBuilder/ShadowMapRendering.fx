// Renders the scene to the shadow map
//
float4x4	World2ShadowMap;
float4x4	Local2World;

struct VS_IN
{
	float3	Position	: POSITION;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
};

PS_IN VS( VS_IN _In )
{
	float3	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World ).xyz;

	PS_IN	Out;
	Out.__Position = mul( float4( WorldPosition, 1.0 ), World2ShadowMap );

	return Out;
}

float	PS( PS_IN _In ) : SV_TARGET0
{
	return 0.0;
}


// ===================================================================================
//
technique10 RenderSceneShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
