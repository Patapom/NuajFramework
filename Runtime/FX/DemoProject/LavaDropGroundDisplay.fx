// This shader animates and displays blob particles
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"
//#include "GBufferSupport.fx"

// ===================================================================================
// Tiles display
struct VS_IN
{
	float3	Position	: POSITION;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
};

float3		TileOrigin;
Texture2DArray	RockTexture;

PS_IN	VS_Display( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = TileOrigin + _In.Position;
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = 1.0 * Out.Position.xz;

	return Out;
}

float4	PS_Display( PS_IN _In ) : SV_TARGET0
{
//	return float4( 1, 0, 0, 0 );
	return RockTexture.Sample( LinearWrap, float3( _In.UV, 0.0 ) );
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Display() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Display() ) );
	}
}
