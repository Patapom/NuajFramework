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
bool		BlurFront;

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
float2	BokehUVScale = float2( 1.0, 1.0 );

#define SAMPLES_COUNT	97
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
#elif SAMPLES_COUNT == 97
static const float2	BokehSamples[] =
{
	float2( 0.06488, 0.8668 ),
	float2( 0.2188, 0.8537 ),
	float2( 0.3535, 0.7846 ),
	float2( -0.3222, 0.779 ),
	float2( -0.1633, 0.7814 ),
	float2( 0.006541, 0.7539 ),
	float2( 0.1822, 0.7069 ),
	float2( 0.3578, 0.6598 ),
	float2( 0.5148, 0.5923 ),
	float2( 0.6447, 0.5149 ),
	float2( -0.6966, 0.6863 ),
	float2( -0.5625, 0.6872 ),
	float2( -0.3916, 0.6711 ),
	float2( -0.2163, 0.6247 ),
	float2( -0.04069, 0.5777 ),
	float2( 0.1349, 0.5306 ),
	float2( 0.3106, 0.4835 ),
	float2( 0.4862, 0.4365 ),
	float2( 0.6596, 0.3879 ),
	float2( 0.8138, 0.3115 ),
	float2( 0.92, 0.249 ),
	float2( -0.7296, 0.5733 ),
	float2( -0.6148, 0.5426 ),
	float2( -0.4392, 0.4955 ),
	float2( -0.2635, 0.4485 ),
	float2( -0.08792, 0.4014 ),
	float2( 0.0877, 0.3543 ),
	float2( 0.2633, 0.3073 ),
	float2( 0.439, 0.2602 ),
	float2( 0.6146, 0.2132 ),
	float2( 0.7902, 0.1661 ),
	float2( 0.8915, 0.139 ),
	float2( -0.7768, 0.3971 ),
	float2( -0.662, 0.3663 ),
	float2( -0.4864, 0.3192 ),
	float2( -0.3108, 0.2722 ),
	float2( -0.1351, 0.2251 ),
	float2( 0.04048, 0.1781 ),
	float2( 0.2161, 0.131 ),
	float2( 0.3917, 0.08396 ),
	float2( 0.5673, 0.0369 ),
	float2( 0.743, -0.01016 ),
	float2( 0.8443, -0.03731 ),
	float2( -0.8241, 0.2208 ),
	float2( -0.7092, 0.19 ),
	float2( -0.5336, 0.143 ),
	float2( -0.358, 0.09593 ),
	float2( -0.1824, 0.04887 ),
	float2( -0.006755, 0.00181 ),
	float2( 0.1689, -0.04525 ),
	float2( 0.3445, -0.09231 ),
	float2( 0.5201, -0.1394 ),
	float2( 0.6957, -0.1864 ),
	float2( 0.7971, -0.2136 ),
	float2( -0.8713, 0.04455 ),
	float2( -0.7565, 0.01378 ),
	float2( -0.5809, -0.03328 ),
	float2( -0.4052, -0.08034 ),
	float2( -0.2296, -0.1274 ),
	float2( -0.05398, -0.1745 ),
	float2( 0.1216, -0.2215 ),
	float2( 0.2973, -0.2686 ),
	float2( 0.4729, -0.3156 ),
	float2( 0.6485, -0.3627 ),
	float2( 0.7498, -0.3898 ),
	float2( -0.9185, -0.1317 ),
	float2( -0.8037, -0.1625 ),
	float2( -0.6281, -0.2095 ),
	float2( -0.4525, -0.2566 ),
	float2( -0.2768, -0.3037 ),
	float2( -0.1012, -0.3507 ),
	float2( 0.07441, -0.3978 ),
	float2( 0.25, -0.4448 ),
	float2( 0.4257, -0.4919 ),
	float2( 0.6013, -0.5389 ),
	float2( 0.7026, -0.5661 ),
	float2( -0.9404, -0.2335 ),
	float2( -0.8237, -0.3025 ),
	float2( -0.6707, -0.3824 ),
	float2( -0.4997, -0.4329 ),
	float2( -0.3241, -0.4799 ),
	float2( -0.1484, -0.527 ),
	float2( 0.02718, -0.574 ),
	float2( 0.2028, -0.6211 ),
	float2( 0.375, -0.6591 ),
	float2( 0.5473, -0.6632 ),
	float2( 0.6745, -0.6619 ),
	float2( -0.6468, -0.512 ),
	float2( -0.5258, -0.5825 ),
	float2( -0.3701, -0.6554 ),
	float2( -0.1957, -0.7032 ),
	float2( -0.02188, -0.7467 ),
	float2( 0.1502, -0.7566 ),
	float2( 0.2823, -0.7568 ),
	float2( -0.3601, -0.7783 ),
	float2( -0.2344, -0.8347 ),
	float2( -0.09907, -0.8433 ),
};
#endif

float4	PS_Bokeh( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float2	UVFactor = BokehSize * GBufferInvSize.xy;

	float	Depth = ReadDepth( UV * BokehUVScale );
//	return float4( UV * BokehUVScale, 0, 0 );
//	return 0.02 * Depth;

	float4	Sum = 0.0;
	float4	Max = 0.0;
	for ( int SampleIndex=0; SampleIndex < BokehSamplesCount; SampleIndex++ )
	{
		float4	Color = GBufferTexture0.SampleLevel( NearestClamp, clamp( UV + UVFactor * BokehSamples[SampleIndex], 0.0.xx, BokehMaxUV ), 0 );
		Sum += Color;
		Max = max( Max, Color );
	}
	Sum *= BokehSamplesNormalizer;

//	return Sum * BokehSamplesNormalizer;
//	return Max;

	if ( BlurFront )
		return Sum;

	// This line makes us use Max if the pixel's depth is further away than 1/4 blur depth off of blur depth
	// (meaning we'll use Max if depth > BlurDepth + BlurDepth/4 or depth < BlurDepth - BlurDepth/4)
	// This is so pixels away from focus plane use the Max bokeh, which yields clearer and more defined bokehs
	//	rather than the Sum bokeh. But using Max bokehs all the time show a very definite separation line which
	//	is not ideal...
	float	UseMaxAfterDepthRatio = 0.5;
	float	UseMax = saturate( abs( Depth - BlurDepth - DepthBias ) / (UseMaxAfterDepthRatio * BlurDepth) );
	UseMax *= UseMax;
	UseMax *= UseMax;
//	return  UseMax;
	return lerp( Sum, Max, UseMax );
}

// ===================================================================================
// Recombines front & back (one of them blurred)
Texture2D	TextureFront;
float2		SourceSizeFactorFront;
Texture2D	TextureBack;
float2		SourceSizeFactorBack;
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
