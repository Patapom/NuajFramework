// This shader applies wind dynamics to the particles
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"

float		Dt;
int			ParticlesCount;
Texture2D	TexturePreviousPositions;
Texture2D	TextureCurrentPositions;
float3		BBoxMin;
float3		BBoxMax;

float3		WindVelocity = float3( 2.0, 0.0, 0.0 );
float		ParticleWeightFactor = 1.0;

float3		TurbulenceOffset = 0.0;
float		TurbulenceScale = 1.0;
float		TurbulenceFactor = 1.0;
float		TurbulenceBias = 1.0;
float		TurbulenceVerticalBias = 0.0;

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// Scribouille some random number
float Random( int x )
{
	int n = x;
	n = (n * (n * n * 75731 + 189221) + 1371312589);
	return float( n & 0x7FFFFFFF ) / 2147483648.0;
}

// Computes a turbulent wind velocity
//
float3	Turb( float3 _WorldPosition )
{
	_WorldPosition *= TurbulenceScale;

	float	WindStrength = length( WindVelocity );
	float3	ResultVelocity = 0.0;
	float3	Bias = TurbulenceBias * WindVelocity / TurbulenceFactor;

	float3	Derivatives;
	float	W = 1.0;
	for ( int OctaveIndex=0; OctaveIndex < 4; OctaveIndex++ )
	{
		ResultVelocity.x += W * (2.0 * Noise( _WorldPosition, NoiseTexture0, Derivatives ) - 1.0 + Bias.x);
		ResultVelocity.y += W * (2.0 * Noise( _WorldPosition, NoiseTexture1, Derivatives ) - 1.0 + Bias.y + TurbulenceVerticalBias);
		ResultVelocity.z += W * (2.0 * Noise( _WorldPosition, NoiseTexture2, Derivatives ) - 1.0 + Bias.z);

		_WorldPosition *= 2.0;
		W *= 0.8;
	}

	return TurbulenceFactor * WindStrength * ResultVelocity;
}

float4	PS( VS_IN _In ) : SV_TARGET
{
	float2	UV = float2( _In.Position.x / ParticlesCount, 0.5 );
	float4	p1 = TextureCurrentPositions.SampleLevel( NearestClamp, UV, 0 );

	float	Weight = ParticleWeightFactor * p1.w;

	// Apply motion
	float3	A = Weight * float3( 0.0, -1.0, 0.0 );
	float3	p2 = p1.xyz + (1.0 - 100.0 * Weight) * Turb( p1.xyz + TurbulenceOffset ) * Dt;	// Add turbulent wind
			p2 += A * Dt * Dt;
//	float3	V = 0.5 * (p2 - p0) / Dt;

	// Handle looping by drawing a new random position at the beginning of the wind tunnel
	if ( p2.x > BBoxMax.x )
	{
		p2.x = BBoxMin.x + BBoxMax.x - p2.x;
		p2.y = lerp( BBoxMin.y, BBoxMax.y, Random( _In.Position.x ) );
		p2.z = lerp( BBoxMin.z, BBoxMax.z, Random( 2 * _In.Position.x ) );
	}

//p2 = p1;

	return float4( p2, p1.w );
}

// ===================================================================================

technique10 ProcessDynamics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
