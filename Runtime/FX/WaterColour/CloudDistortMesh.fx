// There are several techniques in this shader :
// _ Gaussian blur to downscale and blur the cloud map
// _ Fractal displacement
// _ Final compositing
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"

Texture2D	SourceTexture;
Texture2D	SourceDepthTexture;

struct VS_IN
{
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_Position;
	float2	UV			: TEXCOORD0;
};

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

	return Offset;
}

PS_IN	VS( VS_IN _In )
{
	float2	PixelPositionProj = float2( 2.0 * _In.UV.x - 1.0, 1.0 - 2.0 * _In.UV.y );

	// Read back Z at this vertex
	float2	Depth = SourceDepthTexture.SampleLevel( LinearClamp, _In.UV, 0 ).xy;
	float	Z = Depth.y;

	float4	PixelPositionOffsetProj = float4( PixelPositionProj, 0.5, 1.0 );
	if ( Z < 20.0f )
	{
		// Retrieve vertex position in WORLD space
		float3	ViewCamera = float3( CameraData.y * CameraData.x * PixelPositionProj.x, CameraData.x * PixelPositionProj.y, 1.0 );
		float3	ViewWorld = mul( float4( ViewCamera, 0.0 ), Camera2World ).xyz;
		float3	PixelPosition = Camera2World[3].xyz + Z * ViewWorld;

		// Compute distortion offset
		float3	Offset = NoiseDeform( PixelPosition );

		// Compute distorted pixel position
		PixelPositionOffsetProj = mul( float4( PixelPosition + 2.0 * OffsetFactor * Offset, 1.0 ), World2Proj );
	}

	PS_IN	Out;
	Out.Position = PixelPositionOffsetProj;
	Out.UV = _In.UV;
	
	return	Out;
}

float4	PS( PS_IN _In ) : SV_TARGET
{
	return SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 );
}

// ===================================================================================

technique10 DistortCloud
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
