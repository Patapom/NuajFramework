// This shader deforms the input positions & normals using 3D Perlin noise

Texture2D ParticlesPositionAlphaTexture;
Texture2D ParticlesNormalUVTexture;

Texture3D NoiseTexture0;
Texture3D NoiseTexture1;
Texture3D NoiseTexture2;
Texture3D NoiseTexture3;

float	Time;

SamplerState LinearClamp
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};
SamplerState LinearRepeat
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};
SamplerState NearestClamp
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};
SamplerState NearestRepeat
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

#include "3DNoise.fx"

struct VS_IN
{
	float4	TransformedPosition : SV_POSITION;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	TransformedPosition : SV_Position;
	float2	UV : TEXCOORD0;
};

struct PS_OUT
{
	float4	Position_Alpha : SV_TARGET0;
	float4	PackedNormal_UV : SV_TARGET1;
};

// ===================================================================================
//
PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.TransformedPosition = _In.TransformedPosition;
	Out.UV = _In.UV;

	return Out;
}

float3 Rotate(float3 coord, float4x4 mat)
{
	return float3( dot(mat._11_12_13, coord),   // 3x3 transform,
				   dot(mat._21_22_23, coord),   // no translation
				   dot(mat._31_32_33, coord) );
}

float smooth_snap(float t, float m)
{
	// input: t in [0..1]
	// maps input to an output that goes from 0..1,
	// but spends most of its time at 0 or 1, except for
	// a quick, smooth jump from 0 to 1 around input values of 0.5.
	// the slope of the jump is roughly determined by 'm'.
	// note: 'm' shouldn't go over ~16 or so (precision breaks down).

	//float t1 =     pow((  t)*2, m)*0.5;
	//float t2 = 1 - pow((1-t)*2, m)*0.5;
	//return (t > 0.5) ? t2 : t1;
  
	// optimized:
	float c = (t > 0.5) ? 1 : 0;
	float s = 1-c*2;
	return c + s*pow(abs(c+s*t)*2, m)*0.5;  
}

PS_OUT PS( PS_IN _In )
{
	// Start by reading back source values
	PS_OUT	Out;
	Out.Position_Alpha = ParticlesPositionAlphaTexture.SampleLevel( NearestClamp, _In.UV, 0 );
	Out.PackedNormal_UV = ParticlesNormalUVTexture.SampleLevel( NearestClamp, _In.UV, 0 );

	// Apply deformation
	float3	SourcePosition = Out.Position_Alpha.xyz;
	float3	SourceNormal = float3( Out.PackedNormal_UV.xy, 0 );
			SourceNormal.z = Out.Position_Alpha.w * sqrt( 1-dot(SourceNormal.xy,SourceNormal.xy) );

	float3	NoiseSamplingPosition = SourcePosition;
			NoiseSamplingPosition.y += 0.05 * Time;
	float3	Offset  = abs( float3( NHQs( NoiseSamplingPosition, NoiseTexture0 ), NHQs( NoiseSamplingPosition, NoiseTexture1 ), NHQs( NoiseSamplingPosition, NoiseTexture2 ) ) );
			NoiseSamplingPosition *= 2.0;
			Offset += 0.25 * abs( float3( NHQs( NoiseSamplingPosition, NoiseTexture0 ), NHQs( NoiseSamplingPosition, NoiseTexture1 ), NHQs( NoiseSamplingPosition, NoiseTexture2 ) ) );
			NoiseSamplingPosition *= 2.0;
			Offset += 0.125 * abs( float3( NHQs( NoiseSamplingPosition, NoiseTexture0 ), NHQs( NoiseSamplingPosition, NoiseTexture1 ), NHQs( NoiseSamplingPosition, NoiseTexture2 ) ) );

	Offset *= smooth_snap( saturate( 10.0 * (SourcePosition.y - 0.05) ), 1.0 ) * float3( 1, 2, 1 );

	float3	TargetPosition = SourcePosition + 0.1 * Offset;

	Out.Position_Alpha.xyz = TargetPosition;

	return Out;
}

// ===================================================================================
//
technique10 Deform
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
