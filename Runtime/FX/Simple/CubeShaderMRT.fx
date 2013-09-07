// Simple shader that outputs R, G and B into 3 different render targets
// These 3 targets are later recombined into a post-process
// This is to test the MRT feature
//

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

struct PS_OUT
{
	float4 Color0	: SV_Target0;
	float4 Color1	: SV_Target1;
	float4 Color2	: SV_Target2;
};


PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN) 0;
	
	output.pos = mul( float4( input.pos, 1.0 ), Local2Proj );
	output.col = input.col;
	output.uv = input.uv;
	
	return output;
}

PS_OUT PS( PS_IN input )
{
	PS_OUT	Result = (PS_OUT) 0;

	float4	Diffuse = TexDiffuse.Sample( DiffuseTextureSampler, input.uv );

	Result.Color0 = float4( Diffuse.r, 1, 1, 1 );
	Result.Color1 = float4( Diffuse.g, 1, 1, 1 );
	Result.Color2 = float4( Diffuse.b, 1, 1, 1 );

	return Result;
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