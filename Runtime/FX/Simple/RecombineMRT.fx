// Basic post-processing that recombines 3 targets, each containing R, G and B, into a single final target

Texture2D	TexOutputR : TEX_OUTPUT_R;	// This is the render target that contains our rendered scene that was passed as a texture for post-processing
Texture2D	TexOutputG : TEX_OUTPUT_G;	// This is the render target that contains our rendered scene that was passed as a texture for post-processing
Texture2D	TexOutputB : TEX_OUTPUT_B;	// This is the render target that contains our rendered scene that was passed as a texture for post-processing

SamplerState OutputTextureSampler
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
	float3	Color = float3(
		TexOutputR.Sample( OutputTextureSampler, input.UV ).r,
		TexOutputG.Sample( OutputTextureSampler, input.UV ).r,
		TexOutputB.Sample( OutputTextureSampler, input.UV ).r );

	return float4( Color, 1 );
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
