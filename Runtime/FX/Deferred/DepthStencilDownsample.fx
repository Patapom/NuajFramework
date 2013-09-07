// DepthStencil Downsampler
//
#include "../Camera.fx"
#include "../Samplers.fx"

float3		Offset;				// X=1/SourceWidth Y=1/SourceHeight Z=0
Texture2D	Geometry;
Texture2D	DepthStencil;

struct VS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float3	View : VIEW;
	float2	UV					: TEXCOORD0;
};

struct PS_OUT
{
	float4	Geometry : SV_TARGET0;
	float	Depth : SV_DEPTH;
};

VS_IN VS( VS_IN _In )
{
	return _In;
}

PS_OUT	PS( VS_IN _In )
{
	float	D00 = DepthStencil.SampleLevel( NearestClamp, _In.UV, 0 ).x;
	float	D01 = DepthStencil.SampleLevel( NearestClamp, _In.UV + Offset.xz, 0 ).x;
	float	D10 = DepthStencil.SampleLevel( NearestClamp, _In.UV + Offset.zy, 0 ).x;
	float	D11 = DepthStencil.SampleLevel( NearestClamp, _In.UV + Offset.xy, 0 ).x;
//	float	D = 0.25 * (D00 + D01 + D10 + D11);
	float	D = max( D00, max( D01, max( D10, D11 ) ) );

	float4	G00 = Geometry.SampleLevel( NearestClamp, _In.UV, 0 );
	float4	G01 = Geometry.SampleLevel( NearestClamp, _In.UV + Offset.xz, 0 );
	float4	G10 = Geometry.SampleLevel( NearestClamp, _In.UV + Offset.zy, 0 );
	float4	G11 = Geometry.SampleLevel( NearestClamp, _In.UV + Offset.xy, 0 );
	float4	G = 0.25 * (G00 + G01 + G10 + G11);
			G.z = max( G00.z, max( G01.z, max( G10.z, G11.z ) ) );

	PS_OUT	Out;
	Out.Geometry = G;
	Out.Depth = D;

	return Out;
}


// ===================================================================================
//
technique10 DownSample
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
