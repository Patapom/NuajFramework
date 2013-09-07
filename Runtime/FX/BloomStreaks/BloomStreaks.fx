// This shader displays a bloom & streaks effect
//
//#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ToneMappingSupport.fx"

static const float3	LUMINANCE_WEIGHTS = float3( 0.2126, 0.7152, 0.0722 );	// RGB => Y (taken from http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering#Light_Adaptation)

float3		BufferInvSize;
Texture2D	SourceBuffer;

struct VS_IN
{
	float4	Position			: SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }


// ===================================================================================
// Separates the high luminance levels into the bloom & streak targets based on light phase
//
float	LuminanceThresholdBloom = 0.5;
float	BloomFactor = 1.0;
float	BloomGamma = 1.0;
float	LuminanceThresholdStreaks = 0.7;
float	StreaksFactor = 1.0;

struct PS_IN
{
	float4	Position			: SV_POSITION;
	float	ToneMappedLuminance	: LUMINANCE;
};

struct PS_OUT
{
	float4	BloomColors			: SV_TARGET0;
	float4	StreakColors		: SV_TARGET1;
};

PS_IN	VS_LuminanceSeparation( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
	Out.ToneMappedLuminance = GetImageAverageLuminance();
	return Out;
}

float	SmoothThreshold( float _Luminance, float _Threshold )
{
	float	MinThreshold = 0.95 * _Threshold;
	float	MaxThreshold = 1.05 * _Threshold;
	return smoothstep( 0.0, 1.0, saturate( _Luminance-MinThreshold) / (MaxThreshold-MinThreshold) );
}

PS_OUT	PS_LuminanceSeparation( PS_IN _In )
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	float	ToneMappedLuminance = _In.ToneMappedLuminance;
	float4	C0 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );
	float4	C1 = SourceBuffer.SampleLevel( LinearClamp, UV + BufferInvSize.xz, 0 );
	float4	C2 = SourceBuffer.SampleLevel( LinearClamp, UV + BufferInvSize.zy, 0 );
	float4	C3 = SourceBuffer.SampleLevel( LinearClamp, UV + BufferInvSize.xy, 0 );
	float4	ToneMappedColor = 0.25 * (C0+C1+C2+C3);

	float	LightPhase = ToneMappedColor.w;	// Phase with light stands in alpha
	float	Luminance = dot( ToneMappedColor.xyz, LUMINANCE_WEIGHTS );

	PS_OUT	Out;
	Out.BloomColors = float4( SmoothThreshold( Luminance, LuminanceThresholdBloom ) * BloomFactor * saturate(1.0-LightPhase) * pow( ToneMappedColor.xyz, 1.0 / BloomGamma ), LightPhase );
//	Out.StreakColors = float4( SmoothThreshold( Luminance, LuminanceThresholdStreaks ) * StreaksFactor *  LightPhase * max( 0.0, ToneMappedColor.xyz - LuminanceThresholdStreaks.xxx), LightPhase );
	Out.StreakColors = float4( SmoothThreshold( Luminance, LuminanceThresholdStreaks ) * StreaksFactor *  LightPhase * max( 0.0, ToneMappedColor.xyz - LuminanceThresholdStreaks.xxx),
		LightPhase * Luminance / ToneMappedLuminance
		);

	return Out;
}

// ===================================================================================
// Applies bloom to the source
float	BloomRadius = 1.0;

float4	PS_BloomDownScaleLoDef( VS_IN _In ) : SV_TARGET0
{
	// Uses 1 sample
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	return SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	// Uses 4 samples
// 	float2	UV = (_In.Position.xy-0.5) * BufferInvSize.xy;
// 	float4	C0 = SourceBuffer.SampleLevel( LinearMirror, UV, 0 );
// 	UV.x += BufferInvSize.x;
// 	float4	C1 = SourceBuffer.SampleLevel( LinearMirror, UV, 0 );
// 	UV.y += BufferInvSize.y;
// 	float4	C2 = SourceBuffer.SampleLevel( LinearMirror, UV, 0 );
// 	UV.x -= BufferInvSize.x;
// 	float4	C3 = SourceBuffer.SampleLevel( LinearMirror, UV, 0 );
// 	return 0.25 * (C0+C1+C2+C3);
}

float4	PS_Bloom( VS_IN _In ) : SV_TARGET0
{
	float2	UV = (_In.Position.xy-0.0) * BufferInvSize.xy;
	float4	C = SourceBuffer.SampleLevel( LinearMirror, UV, 0 );

	// Tap 4 samples
	float2	dUV = BloomRadius * BufferInvSize.xy;
	float4	C0 = SourceBuffer.SampleLevel( LinearMirror, UV - dUV, 0 );
	float4	C1 = SourceBuffer.SampleLevel( LinearMirror, UV + dUV, 0 );
			dUV = BloomRadius * float2( -BufferInvSize.x, BufferInvSize.y );
	float4	C2 = SourceBuffer.SampleLevel( LinearMirror, UV - dUV, 0 );
	float4	C3 = SourceBuffer.SampleLevel( LinearMirror, UV + dUV, 0 );

	return 0.3333 * (C + 0.5 * (C0+C1+C2+C3));
}

// ===================================================================================
// Applies streaks to the source
float2	StreakDirection;
float4	Weights;

float4	PS_Streaks( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	float4	C0 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	float	Directionality = saturate( C0.w - 1.0 );
	float2	LocalStreakDirection = StreakDirection;
//	float2	LocalStreakDirection = lerp( StreakDirection, float2( length(StreakDirection), 0.0 ), Directionality );
	float2	dUV = LocalStreakDirection * BufferInvSize.xy;
//	dUV.x *= 0.0 + min( 1.5, C0.w );	// Augment width with directionality

	// Sample right
	UV += dUV;
	float4	C1 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );
	UV += dUV;
	float4	C2 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );
	UV += dUV;
	float4	C3 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	// Sample left
	UV = _In.Position.xy * BufferInvSize.xy - dUV;
	float4	C4 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );
	UV -= dUV;
	float4	C5 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );
	UV -= dUV;
	float4	C6 = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	return Weights.x * C0 + Weights.y * (C1+C4) + Weights.z * (C2+C5) + Weights.w * (C3+C6);
}

// ===================================================================================
// Final combination of bloom and streaks over the original buffer
Texture2D		BloomBuffer;
Texture2DArray	StreaksBuffer;

float4	PS_Combine( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float4	Color = 0.3 * SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	// Add bloom
 	Color.xyz += BloomBuffer.SampleLevel( LinearClamp, UV, 0 ).xyz;

	// Add streaks
	Color.xyz += StreaksBuffer.SampleLevel( LinearClamp, float3( UV, 0.0 ), 0 ).xyz;
	Color.xyz += StreaksBuffer.SampleLevel( LinearClamp, float3( UV, 1.0 ), 0 ).xyz;
	Color.xyz += StreaksBuffer.SampleLevel( LinearClamp, float3( UV, 2.0 ), 0 ).xyz;
	Color.xyz += StreaksBuffer.SampleLevel( LinearClamp, float3( UV, 3.0 ), 0 ).xyz;

	return Color;
}

// ===================================================================================
technique10 LuminanceSeparation
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_LuminanceSeparation() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_LuminanceSeparation() ) );
	}
}

technique10 ApplyBloom
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_BloomDownScaleLoDef() ) );
	}
}

technique10 ApplyBloomUpScale
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Bloom() ) );
	}
}

technique10 ApplyStreaks
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Streaks() ) );
	}
}

technique10 Combine
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Combine() ) );
	}
}
