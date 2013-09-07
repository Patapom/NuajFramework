// This shader computes a density given a world space position
// Various 3D noise textures are combined in such manner that
//	a single fDensity value is formed in a seemingly random way.
//
//#define METABALLS	// Uncomment to generate a simple bunch of metaballs

Texture3D NoiseTexture0;
Texture3D NoiseTexture1;
Texture3D NoiseTexture2;
Texture3D NoiseTexture3;

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

// Computes the fDensity function for the given world position
// Returns a fDensity value. Values > 0 mean full while values < 0 mean empty.
//
float	ComputeDensity( float3 _WorldPosition )
{
//	return -_WorldPosition.y;				// Below 0 is the ground, above is air... Simple
//	return 4.0 - length( _WorldPosition );	// Defines a sphere of radius 4

//	return NLQu( 0.25 * _WorldPosition, NoiseTexture0 ).x;
//	return NLQs( 0.25 * _WorldPosition, NoiseTexture0 ).x;
//	return NHQu( 0.25 * _WorldPosition, NoiseTexture0, 0 );

	float	fDensity = 0.0;

#ifdef METABALLS
	// Make a sum of metaballs
	float4	CentersAndRadii[] =
	{
		float4( 0, 0, 0, 1.0 ),
		float4( -2.0, 0, 0, 1.0 ),
		float4( +2.0, 0, 0, 1.0 ),
		float4( 0, -2.0, 0, 1.0 ),
		float4( 0, +2.0, 0, 1.0 ),
		float4( 0, 0, -2.0, 1.0 ),
		float4( 0, 0, +2.0, 1.0 ),
	};

	fDensity = -0.01;
	for ( int MetaBouleIndex=0; MetaBouleIndex < 7; MetaBouleIndex++ )
	{
		float3	Center = CentersAndRadii[MetaBouleIndex].xyz;
		float	fRadius = CentersAndRadii[MetaBouleIndex].w + PlaneDistance;

		float	fDistance = length( _WorldPosition - Center );
		fDensity += exp( -2.0 * fDistance*fDistance / (fRadius*fRadius) );
	}

#else

	float3	ws = _WorldPosition - 10.0;

	// sample an ultra-ultra-low-frequency (slowly-varying) float4 
	// noise value we can use to vary high-level terrain features 
	// over space.
	float4 uulf_rand  = saturate( NMQu(ws*0.000718, NoiseTexture0) * 2 - 0.5 );
	float4 uulf_rand2 =           NMQu(ws*0.000632, NoiseTexture1);
	float4 uulf_rand3 =           NMQu(ws*0.000695, NoiseTexture2);


	//-----------------------------------------------
	// PRE-WARP the world-space coordinate.
	const float prewarp_str = 25;   // recommended range: 5..25
	float3 ulf_rand = 0;
#if 1  // high-quality version
	ulf_rand.x = NHQs(ws*0.0041*0.971, NoiseTexture2, 1)*0.64
			   + NHQs(ws*0.0041*0.461, NoiseTexture3, 1)*0.32;
	ulf_rand.y = NHQs(ws*0.0041*0.997, NoiseTexture1, 1)*0.64
			   + NHQs(ws*0.0041*0.453, NoiseTexture0, 1)*0.32;
	ulf_rand.z = NHQs(ws*0.0041*1.032, NoiseTexture3, 1)*0.64
			   + NHQs(ws*0.0041*0.511, NoiseTexture2, 1)*0.32;
#endif
	ws += ulf_rand.xyz * prewarp_str * saturate(uulf_rand3.x*1.4 - 0.3);


	//-----------------------------------------------
	// compute 8 randomly-rotated versions of 'ws'.  
	// we probably won't use them all, but they're here for experimentation.
	// (and if they're not used, the shader compiler will optimize them out.)
	float3 c0 = Rotate(ws,octaveMat0);
	float3 c1 = Rotate(ws,octaveMat1);
	float3 c2 = Rotate(ws,octaveMat2);
	float3 c3 = Rotate(ws,octaveMat3);
	float3 c4 = Rotate(ws,octaveMat4);
	float3 c5 = Rotate(ws,octaveMat5);
	float3 c6 = Rotate(ws,octaveMat6);
	float3 c7 = Rotate(ws,octaveMat7);


	//-----------------------------------------------
	// MAIN SHAPE: CHOOSE ONE
  
#if 1
	// very general ground plane:
	fDensity = -ws.y * 1;

	// to add a stricter ground plane further below:
	float	fStrictGroundHeight = -0.5;
	fDensity += saturate((fStrictGroundHeight - _WorldPosition.y*0.3)*3.0)*40 * uulf_rand2.z;
#endif
#if 0
	// infinite network of caves:  (small bias)
	fDensity = 12;  // positive value -> more rock; negative value -> more open space
#endif
  
  
	//----------------------------------------

  
 

	// CRUSTY SHELF
	// often creates smooth tops (~grass) and crumbly, eroded underneath parts.
#if 0
	float shelf_thickness_y = 2.5;//2.5;
	float shelf_pos_y = -1;//-2;
	float shelf_strength = 9.5;   // 1-4 is good
	fDensity = lerp(fDensity, shelf_strength, 0.83*saturate(  shelf_thickness_y - abs(ws.y - shelf_pos_y) ) * saturate(uulf_rand.y*1.5-0.5) );
#endif    
    
	// FLAT TERRACES
#if 1
	{
		const float terraces_can_warp = 0.5 * uulf_rand2.y;
		const float terrace_freq_y = 0.13;
		const float terrace_str  = 3*saturate(uulf_rand.z*2-1);  // careful - high str here diminishes strength of noise, etc.
		const float overhang_str = 1*saturate(uulf_rand.z*2-1);  // careful - too much here and LODs interfere (most visible @ silhouettes because zbias can't fix those).
		float fy = -lerp(_WorldPosition.y, ws.y, terraces_can_warp)*terrace_freq_y;
		float orig_t = frac(fy);
		float t = orig_t;
		t = smooth_snap(t, 16);  // faster than using 't = t*t*(3-2*t)' four times
		fy = floor(fy) + t;
		fDensity += fy*terrace_str;
		fDensity += (t - orig_t) * overhang_str;
	}
#endif
    
	// other random effects...
#if 0
	// repeating ridges on [warped] Y coord:
	fDensity += NLQs(ws.xyz*float3(2,27,2)*0.0037, NoiseTexture0).x*2 * saturate(uulf_rand2.w*2-1);
#endif
#if 0
	// to make it extremely mountainous & climby:
	fDensity += ulf_rand.x*80;
#endif
   
#if 1
	// sample 9 octaves of noise, w/rotated ws coord for the last few.
	// note: sometimes you'll want to use NHQs (high-quality noise)
	//   instead of NMQs for the lowest 3 frequencies or so; otherwise
	//   they can introduce UNWANTED high-frequency noise (jitter).
	//   BE SURE TO PASS IN 'PackedNoiseVolX' instead of 'NoiseVolX'
	//   WHEN USING NHQs()!!!
	// note: if you want to randomly rotate various octaves,
	//   feed c0..c7 (instead of ws) into the noise functions.
	//   This is especially good to do with the lowest frequency,
	//   so that it doesn't repeat (across the ground plane) as often...
	//   and so that you can actually randomize the terrain!
	//   Note that the shader compiler will skip generating any rotated
	//   coords (c0..c7) that are never used.
	fDensity += 
			( 0
//			+ NLQs(ws*0.3200*0.934, NoiseTexture3).x*0.16*1.20
			+ NLQs(ws*0.1600*1.021, NoiseTexture1).x*0.32*1.16
 			+ NLQs(ws*0.0800*0.985, NoiseTexture2).x*0.64*1.12
 			+ NLQs(ws*0.0400*1.051, NoiseTexture0).x*1.28*1.08
 			+ NLQs(ws*0.0200*1.020, NoiseTexture1).x*2.56*1.04
 			+ NLQs(ws*0.0100*0.968, NoiseTexture3).x*5 
 			+ NMQs(ws*0.0050*0.994, NoiseTexture0).x*10*1.0 // MQ
// 			+ NMQs(c6*0.0025*1.045, NoiseTexture2).x*20*0.9 // MQ
// 			+ NHQs(c7*0.0012*0.972, NoiseTexture3).x*40*0.8 // HQ and *rotated*!
			);
#endif

#endif

	return fDensity;
}