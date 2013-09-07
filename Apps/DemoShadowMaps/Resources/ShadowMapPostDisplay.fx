// Shadow-Map display

#include "Camera.fx"
#include "ShadowMapSupport.fx"

int	ShadowMapDisplayIndex = 1;

SamplerState TextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float4	TransformedPosition : SV_POSITION;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	TransformedPosition : SV_Position;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out = (PS_IN) 0;
	
	Out.TransformedPosition = _In.TransformedPosition;
	Out.View = _In.View;
	Out.UV = _In.UV;
	
	return	Out;
}

float4 PS( PS_IN input ) : SV_Target
{
	float	fDepth = 1.0 * ShadowMaps.Sample( TextureSampler, float3( input.UV, ShadowMapDisplayIndex ) ).r;
	return float4( fDepth.xxx, 1 );
}

technique10 PostProcessRender
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
