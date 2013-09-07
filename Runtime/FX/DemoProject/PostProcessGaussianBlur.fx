// This shader performs a motion blur on the material buffer using the velocity buffer
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"

#define STEPS_COUNT 6

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// Separates front & back
float		BlurDepth;
float		FrontAlpha = 100.0;
float		BackAlpha = 100.0;
float		DepthBias = 0.5;	// A little bias when isolating back from front so blending is nicer

struct PS_COMBINE_OUT
{
	float4	Front	: SV_TARGET0;
	float4	Back	: SV_TARGET1;
};

PS_COMBINE_OUT	PS_Separate( VS_IN _In )
{
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float4	Color = GBufferTexture0.SampleLevel( NearestClamp, UV, 0 );
	float	Depth = ReadDepth( UV );

	PS_COMBINE_OUT	Out;
	Out.Front = float4( Color.xyz, Depth <= BlurDepth+DepthBias ? FrontAlpha : 0.0 );
	Out.Back = Depth >= BlurDepth-DepthBias ? float4( Color.xyz, 0.0 ) : float4( 0, 0, 0, FrontAlpha );

	return Out;
}

// ===================================================================================
// Bokeh effect
// The bokeh consists in a fixed number of samples pre-computed by the BokehSamplesGenerator program found in Tools
// These samples define a rotated and jittered bokeh pattern in a [-1,+1] rectangular range that we later multiply
//	by the requested BokehSize parameter
// 
float	BokehSize = 0.0;
float2	BokehMaxUV = float2( 1.0, 1.0 );

#define SAMPLES_COUNT	93
static const int	BokehSamplesCount = SAMPLES_COUNT;
static const float	BokehSamplesNormalizer = 1.0 / BokehSamplesCount;

#if SAMPLES_COUNT == 24
static const float2	BokehSamples[] =
{
	float2( -0.3877, 0.8227 ),
	float2( -0.3104, 0.7899 ),
	float2( 0.1143, 0.7524 ),
	float2( 0.5147, 0.6934 ),
	float2( 0.8025, 0.3334 ),
	float2( -0.7219, 0.5364 ),
	float2( -0.4001, 0.3998 ),
	float2( -0.002599, 0.4055 ),
	float2( 0.402, 0.3923 ),
	float2( 0.791, 0.3065 ),
	float2( -0.7529, 0.1055 ),
	float2( -0.4011, 0.003717 ),
	float2( 0.003585, -0.009451 ),
	float2( 0.4011, -0.003717 ),
	float2( 0.7529, -0.1055 ),
	float2( -0.791, -0.3065 ),
	float2( -0.402, -0.3923 ),
	float2( 0.002599, -0.4055 ),
	float2( 0.4001, -0.3998 ),
	float2( 0.6833, -0.52 ),
	float2( -0.7967, -0.32 ),
	float2( -0.5147, -0.6934 ),
	float2( -0.1143, -0.7524 ),
	float2( 0.2976, -0.7845 ),
};
#elif SAMPLES_COUNT == 30
static const float2	BokehSamples[] =
{
	float2( -0.1234, 0.8386 ),
	float2( 0.1468, 0.7219 ),
	float2( 0.4051, 0.6133 ),
	float2( -0.4956, 0.6768 ),
	float2( -0.2385, 0.5686 ),
	float2( 0.03217, 0.455 ),
	float2( 0.2896, 0.3434 ),
	float2( 0.5612, 0.2291 ),
	float2( 0.8177, 0.1204 ),
	float2( -0.6028, 0.4218 ),
	float2( -0.344, 0.3138 ),
	float2( -0.07602, 0.1992 ),
	float2( 0.1833, 0.08975 ),
	float2( 0.4535, -0.02538 ),
	float2( 0.7104, -0.1363 ),
	float2( -0.7173, 0.1531 ),
	float2( -0.4587, 0.04557 ),
	float2( -0.1885, -0.07164 ),
	float2( 0.06954, -0.1788 ),
	float2( 0.3385, -0.2944 ),
	float2( 0.5948, -0.4047 ),
	float2( -0.825, -0.1015 ),
	float2( -0.5685, -0.2112 ),
	float2( -0.2973, -0.3257 ),
	float2( -0.04075, -0.4342 ),
	float2( 0.2305, -0.5488 ),
	float2( 0.487, -0.6593 ),
	float2( -0.6821, -0.4786 ),
	float2( -0.4102, -0.5937 ),
	float2( -0.1531, -0.7041 ),
};
#elif SAMPLES_COUNT == 32
static const float2	BokehSamples[] =
{	float2( -0.2304, 0.8353 ),
	float2( -0.02589, 0.7802 ),
	float2( 0.3344, 0.77 ),
	float2( 0.6149, 0.5716 ),
	float2( -0.6706, 0.6573 ),
	float2( -0.4646, 0.5699 ),
	float2( -0.1457, 0.498 ),
	float2( 0.2089, 0.4743 ),
	float2( 0.5693, 0.4641 ),
	float2( 0.8497, 0.2658 ),
	float2( -0.7632, 0.3319 ),
	float2( -0.4799, 0.2116 ),
	float2( -0.1625, 0.1721 ),
	float2( 0.1664, 0.1593 ),
	float2( 0.4953, 0.1466 ),
	float2( 0.7785, 0.02638 ),
	float2( -0.7785, -0.02638 ),
	float2( -0.4953, -0.1466 ),
	float2( -0.1664, -0.1593 ),
	float2( 0.1625, -0.1721 ),
	float2( 0.4799, -0.2116 ),
	float2( 0.7632, -0.3319 ),
	float2( -0.8497, -0.2658 ),
	float2( -0.5693, -0.4641 ),
	float2( -0.2089, -0.4743 ),
	float2( 0.1457, -0.498 ),
	float2( 0.4646, -0.5699 ),
	float2( 0.632, -0.6409 ),
	float2( -0.6092, -0.5582 ),
	float2( -0.3344, -0.77 ),
	float2( 0.02589, -0.7802 ),
	float2( 0.1532, -0.8025 ),
};
#elif SAMPLES_COUNT == 93
static const float2	BokehSamples[] =
{
	float2( 0.1669, 0.8423 ),
	float2( 0.3212, 0.7747 ),
	float2( 0.4765, 0.7099 ),
	float2( -0.2055, 0.8233 ),
	float2( -0.05137, 0.7588 ),
	float2( 0.1034, 0.6944 ),
	float2( 0.2578, 0.6265 ),
	float2( 0.4136, 0.5614 ),
	float2( 0.5671, 0.4964 ),
	float2( 0.7226, 0.4301 ),
	float2( -0.5805, 0.7948 ),
	float2( -0.4283, 0.7293 ),
	float2( -0.272, 0.6636 ),
	float2( -0.1176, 0.5977 ),
	float2( 0.03626, 0.5305 ),
	float2( 0.1903, 0.4657 ),
	float2( 0.3459, 0.4019 ),
	float2( 0.5003, 0.3338 ),
	float2( 0.6552, 0.2704 ),
	float2( 0.8083, 0.204 ),
	float2( -0.646, 0.6453 ),
	float2( -0.4897, 0.5809 ),
	float2( -0.3362, 0.5149 ),
	float2( -0.1809, 0.4496 ),
	float2( -0.02737, 0.3848 ),
	float2( 0.1281, 0.3194 ),
	float2( 0.2815, 0.2526 ),
	float2( 0.4367, 0.1884 ),
	float2( 0.5927, 0.1224 ),
	float2( 0.7468, 0.05573 ),
	float2( 0.9009, -0.008294 ),
	float2( -0.7139, 0.4863 ),
	float2( -0.5597, 0.4205 ),
	float2( -0.404, 0.3545 ),
	float2( -0.249, 0.2869 ),
	float2( -0.09425, 0.2233 ),
	float2( 0.0585, 0.1586 ),
	float2( 0.2129, 0.09054 ),
	float2( 0.3676, 0.02548 ),
	float2( 0.5242, -0.03894 ),
	float2( 0.6773, -0.1045 ),
	float2( 0.8305, -0.1701 ),
	float2( -0.7763, 0.3378 ),
	float2( -0.6213, 0.2732 ),
	float2( -0.4671, 0.2051 ),
	float2( -0.312, 0.1395 ),
	float2( -0.1586, 0.07675 ),
	float2( -0.002278, 0.009766 ),
	float2( 0.1519, -0.05647 ),
	float2( 0.3053, -0.1205 ),
	float2( 0.4591, -0.1865 ),
	float2( 0.6145, -0.2525 ),
	float2( 0.7692, -0.318 ),
	float2( -0.8395, 0.1909 ),
	float2( -0.6848, 0.1244 ),
	float2( -0.5288, 0.05896 ),
	float2( -0.3766, -0.00776 ),
	float2( -0.2193, -0.07284 ),
	float2( -0.06526, -0.1393 ),
	float2( 0.08885, -0.2049 ),
	float2( 0.2437, -0.2701 ),
	float2( 0.398, -0.3344 ),
	float2( 0.5509, -0.3989 ),
	float2( 0.7057, -0.4647 ),
	float2( -0.9074, 0.02929 ),
	float2( -0.7543, -0.038 ),
	float2( -0.5987, -0.1037 ),
	float2( -0.4421, -0.168 ),
	float2( -0.2886, -0.2349 ),
	float2( -0.1332, -0.2989 ),
	float2( 0.01958, -0.3642 ),
	float2( 0.1752, -0.4302 ),
	float2( 0.3274, -0.4964 ),
	float2( 0.4838, -0.5614 ),
	float2( 0.6364, -0.6272 ),
	float2( -0.8158, -0.1841 ),
	float2( -0.6608, -0.2511 ),
	float2( -0.5074, -0.3162 ),
	float2( -0.3513, -0.3816 ),
	float2( -0.198, -0.4474 ),
	float2( -0.04444, -0.514 ),
	float2( 0.1129, -0.5778 ),
	float2( 0.2652, -0.6449 ),
	float2( 0.4212, -0.7095 ),
	float2( -0.729, -0.4134 ),
	float2( -0.5748, -0.4769 ),
	float2( -0.4202, -0.5442 ),
	float2( -0.2666, -0.6092 ),
	float2( -0.1107, -0.6746 ),
	float2( 0.0421, -0.739 ),
	float2( -0.484, -0.6919 ),
	float2( -0.3301, -0.7566 ),
	float2( -0.1735, -0.8236 ),
};
#endif

float4	PS_Bokeh( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float2	UVFactor = BokehSize * GBufferInvSize.xy;

	float4	Sum = 0.0;
	for ( int SampleIndex=0; SampleIndex < BokehSamplesCount; SampleIndex++ )
//		Sum += GBufferTexture0.SampleLevel( NearestClamp, clamp( UV + UVFactor * BokehSamples[SampleIndex], 0.0.xx, BokehMaxUV ), 0 );
		Sum = max( Sum, GBufferTexture0.SampleLevel( NearestClamp, clamp( UV + UVFactor * BokehSamples[SampleIndex], 0.0.xx, BokehMaxUV ), 0 ) );

//	return Sum * BokehSamplesNormalizer;
	return Sum;
}

// ===================================================================================
// Recombines front & back (one of them blurred)
Texture2D	TextureFront;
float2		SourceSizeFactorFront;
Texture2D	TextureBack;
float2		SourceSizeFactorBack;
bool		BlurFront;
float		BlendPower = 1.0;

float4	PS_Combine( VS_IN _In ) : SV_TARGET
{
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float4	Front = TextureFront.SampleLevel( LinearClamp, SourceSizeFactorFront * UV, 0 );
	float4	Back = TextureBack.SampleLevel( LinearClamp, SourceSizeFactorBack * UV, 0 ); 
//	return Front.wwww;
//	return Back;
//	return Back.wwww;

	float	Blend = pow( saturate( Front.w ), BlendPower ); 
	return lerp( Back, Front, Blend );	// Simple blending with alpha
}

// ===================================================================================

technique10 Separate
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Separate() ) );
	}
}

technique10 Bokeh
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Bokeh() ) );
	}
}

technique10 Combine
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Combine() ) );
	}
}
