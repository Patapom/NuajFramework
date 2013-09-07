// Performs the depth pass at full resolution
//
#include "../Camera.fx"

float4x4	Local2World : LOCAL2WORLD;

struct VS_IN
{
	float3	Position		: POSITION;	// Depth-pass renderables need only declare a position
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
//	float	Depth			: DEPTH;
};

PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World );

	PS_IN	Out;
	Out.Position = mul( WorldPosition, World2Proj );
// 	Out.Depth = mul( WorldPosition, World2Camera ).z;

	return Out;
}

void PS( PS_IN _In )// : SV_TARGET0
{
//	return 1.0;//0.01 * _In.Depth;
}

// ===================================================================================
//
technique10 DrawDepth
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
