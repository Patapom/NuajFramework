// This shader computes a density given a world space position
// Various 3D noise textures are combined in such manner that
//	a single density value is formed in a seemingly random way.
//
#include "../Samplers.fx"
#include "3DNoise.fx"

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

float4x4 octaveMat0;
float4x4 octaveMat1;
float4x4 octaveMat2;
float4x4 octaveMat3;
float4x4 octaveMat4;
float4x4 octaveMat5;
float4x4 octaveMat6;
float4x4 octaveMat7;

// Computes the density function for the given world position
// Returns a density value. Values > 0 mean full while values < 0 mean empty.
//
float	ComputeDensity( float3 _WorldPosition )
{
//	float	Y = 2.0 + exp( -0.1 * _WorldPosition.y );

	float3	Center = float3( 0.0, 2.0, 0.0 );

	float3	Position = _WorldPosition;
	Position.y = Position.y < Center.y ? Center.y+1.0-exp( 0.1 * (Center.y - Position.y) ) : Position.y;

	float	fDensity = 15.8 - length( Position - Center );

	return fDensity;
}