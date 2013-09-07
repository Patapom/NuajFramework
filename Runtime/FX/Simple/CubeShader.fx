// First VS/PS

float4x4	Local2Proj : WORLD2PROJ;
Texture2D	TexDiffuse : TEX_DIFFUSE;

SamplerState DiffuseTextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};


struct VS_IN
{
	float3 pos : POSITION;
	float4 col : COLOR;
	float2 uv : TEXCOORD0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
	float2 uv : TEXCOORD0;
};

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN) 0;
	
	output.pos = mul( float4( input.pos, 1.0 ), Local2Proj );
	output.col = input.col;
	output.uv = input.uv;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	return TexDiffuse.Sample( DiffuseTextureSampler, input.uv );
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}