// This shader computes the mega particles' deep shadow map and camera depth map
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"

float4x4	World2Light;	// The WORLD => LIGHT transform
float4x4	Light2Proj;		// The LIGHT => PROJECTION transform
float4x4	World2LightProj;// The WORLD => LIGHT => PROJECTION transform
float4		SliceMin;		// XYZW contain each the minimum depth value for slice 0, 1, 2 and 3 respectively
float4		SliceMax;		// XYZW contain each the maximum depth value for slice 0, 1, 2 and 3 respectively
float3		LightPosition;	// Light position
//float3		ToLight;		// Vector pointing toward the light
float		LightIntensity;	// Light intensity (no kidding !)

// ===================================================================================

struct VS_IN
{
	// Per-vertex data
	float3	Position			: POSITION;
	float3	Normal				: NORMAL;
	float2	UV					: TEXCOORD0;

	// Per-instance data
	float3	ParticlePosition	: PARTICLE_POSITION;
	float	ParticleRadius		: PARTICLE_RADIUS;
};

struct PS_IN_DSM
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;		// Position in LIGHT space
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
 	float4	NormalDepth	: NORMAL;
};

// ===================================================================================
// Noise distortion (same as post-process but for vertices)
int		OctavesCount = 4;
float	FrequencyFactor = 0.5;
float	OffsetFactor = 0.02;

float3	NoiseDeform( float3 _WorldPosition )
{
	float3	Offset = 0.0.xxx;
	float	Weight = 1.0;
	for ( int OctaveIndex=0; OctaveIndex < OctavesCount; OctaveIndex++ )
	{
		float3	Derivatives;
		Offset.x += Weight * (2.0 * Noise( FrequencyFactor * _WorldPosition, NoiseTexture0, Derivatives ) - 1.0);
		Offset.y += Weight * (2.0 * Noise( FrequencyFactor * _WorldPosition, NoiseTexture1, Derivatives ) - 1.0);
		Offset.z += Weight * (2.0 * Noise( FrequencyFactor * _WorldPosition, NoiseTexture2, Derivatives ) - 1.0);

		Weight *= 0.5;
		_WorldPosition *= 2.0;
	}

	return 2.0 * OffsetFactor * Offset;
//	return 1.0 * 0.02 * Offset;
}

// ===================================================================================
// Deep Shadow Map rendering
PS_IN_DSM VS_DSM( VS_IN _In )
{
	// Displace position along normal
	float3	WorldPosition = _In.ParticlePosition + _In.ParticleRadius * _In.Position;
	WorldPosition += NoiseDeform( WorldPosition );

	float4	LightPosition = mul( float4( WorldPosition, 1 ), World2Light );

	PS_IN_DSM	Out;
	Out.__Position = mul( LightPosition, Light2Proj );
	Out.Position = LightPosition.xyz;

	return Out;
}

float4	PS_DSM( PS_IN_DSM _In ) : SV_TARGET
{
	float	Depth = _In.Position.z;
	return max( SliceMin, min( SliceMax, Depth.xxxx ) );
}

// ===================================================================================
// Render particles' normal + depth
PS_IN VS( VS_IN _In )
{
	float3	WorldPosition = _In.ParticlePosition + _In.ParticleRadius * _In.Position;
//	WorldPosition += NoiseDeform( WorldPosition );

	float4	CameraPosition = mul( float4( WorldPosition, 1 ), World2Camera );

	PS_IN	Out;
	Out.__Position = mul( CameraPosition, Camera2Proj );
 	Out.NormalDepth = float4( _In.Normal, CameraPosition.z );

	return Out;
}

float4 PS( PS_IN _In ) : SV_TARGET
{
	return _In.NormalDepth;
}

// ===================================================================================
technique10 RenderDeepShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_DSM() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_DSM() ) );
	}
}

technique10 RenderParticles
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
