// This shader performs MSAA anti-aliasing
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"

float		DepthThreshold = 0.01;
float		SmoothDistance = 1.0;
float		SmoothWeights = 1.0;
float		SmoothInvWeight = 0.2;

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// Anti-aliasing
// The algorithm here is similar to what many people are doing (e.g. http://mynameismjp.wordpress.com/2010/08/16/deferred-msaa/ or
//	http://directtovideo.wordpress.com/2009/11/13/deferred-rendering-in-frameranger/ or
//	http://developer.amd.com/assets/Riguer-DX10_tips_and_tricks_for_print.pdf)
//
// Basically, we have 2 depth passes : a standard one, the other one rendering to a MSAA depth target with N multisamples.
// What we need to do is to compare the depth from each sample from the MSAA depth buffer with the "average" depth
//	from the non MSAA depth stencil buffer (i.e. default depth stencil).
// By comparing N differences in depth, we can determine a ratio between a fully aliased pixel (i.e. all Z tests 
//	failed) to a fully correct pixel (i.e. all Z tests pass).
// We use that ratio to blend between the current pixel's color and an "average" color.
//
Texture2DMS<float,4>	MSAADepth4;

float4	PS4( VS_IN _In ) : SV_TARGET0
{
	// Read "average" depth of the current pixel
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float	Depth = ReadDepth( UV );

	// Read multiple samples from the MSAA depth target and compare to non MSAA depth stencil
	int3	XY = int3( _In.Position.xy, 0 );
	float	AA = 0.0;
	[unroll]
	for ( int SampleIndex=0; SampleIndex < 4; SampleIndex++ )
	{
		float	DepthMS = MSAADepth4.Load( XY, SampleIndex );
		AA += saturate( abs(DepthMS - Depth) / DepthThreshold - 1.0 );
	}
	AA *= 0.25;

	float4	RGB = GBufferTexture0.SampleLevel( NearestClamp, UV, 0 );
	if ( AA < 1e-4f )
		return RGB;

	// Blend between color & averaged color
	float4	RGBAA  = GBufferTexture0.SampleLevel( LinearClamp, UV - SmoothDistance * GBufferInvSize.xz, 0 );
			RGBAA += GBufferTexture0.SampleLevel( LinearClamp, UV + SmoothDistance * GBufferInvSize.xz, 0 );
			RGBAA += GBufferTexture0.SampleLevel( LinearClamp, UV - SmoothDistance * GBufferInvSize.zy, 0 );
			RGBAA += GBufferTexture0.SampleLevel( LinearClamp, UV + SmoothDistance * GBufferInvSize.zy, 0 );
			RGBAA = SmoothInvWeight * (SmoothWeights * RGBAA + RGB);

	return lerp( RGB, RGBAA, AA );
}

Texture2DMS<float,8>	MSAADepth8;

float4	PS8( VS_IN _In ) : SV_TARGET0
{
	// Read "average" depth of the current pixel
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float	Depth = ReadDepth( UV );

	// Read multiple samples from the MSAA depth target and compare to non MSAA depth stencil
	int3	XY = int3( _In.Position.xy, 0 );
	float	AA = 0.0;
	[unroll]
	for ( int SampleIndex=0; SampleIndex < 8; SampleIndex++ )
	{
		float	DepthMS = MSAADepth8.Load( XY, SampleIndex );
		AA += saturate( abs(DepthMS - Depth) / DepthThreshold - 1.0 );
	}
	AA *= 0.125;

	// Blend between color & averaged color
	float4	RGB = GBufferTexture0.SampleLevel( NearestClamp, UV, 0 );
	if ( AA < 1e-4f )
		return RGB;

	// Blend between color & averaged color
	float4	RGBAA  = GBufferTexture0.SampleLevel( LinearClamp, UV - SmoothDistance * GBufferInvSize.xz, 0 );
			RGBAA += GBufferTexture0.SampleLevel( LinearClamp, UV + SmoothDistance * GBufferInvSize.xz, 0 );
			RGBAA += GBufferTexture0.SampleLevel( LinearClamp, UV - SmoothDistance * GBufferInvSize.zy, 0 );
			RGBAA += GBufferTexture0.SampleLevel( LinearClamp, UV + SmoothDistance * GBufferInvSize.zy, 0 );
			RGBAA = SmoothInvWeight * (SmoothWeights * RGBAA + RGB);

	return lerp( RGB, RGBAA, AA );
}

// ===================================================================================
technique10 AntiAliasing4
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS4() ) );
	}
}
technique10 AntiAliasing8
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS8() ) );
	}
}
