// HDR post-processing demo
// This simply outputs the rendered texture...

Texture2D	TexBackground;

SamplerState OutputTextureSampler
{
    Filter = MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VS_IN
{
	float4	TransformedPosition : SV_POSITION;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

float4 PS( VS_IN _In ) : SV_Target
{
	return TexBackground.Sample( OutputTextureSampler, _In.UV );
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
