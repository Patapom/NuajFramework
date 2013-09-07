
#define NOISE_LATTICE_SIZE 16
#define INV_LATTICE_SIZE (1.0/(float)(NOISE_LATTICE_SIZE))

Texture3D NoiseTexture0 : NOISE3D_TEX0;
Texture3D NoiseTexture1 : NOISE3D_TEX1;
Texture3D NoiseTexture2 : NOISE3D_TEX2;
Texture3D NoiseTexture3 : NOISE3D_TEX3;
Texture3D LargeNoiseTexture : LARGE_NOISE3D_TEX;

// This file contains helper functions for sampling noise volumes.
// 
//  NLQu    sample noise, low quality, unsigned
//  NLQs    sample noise, low quality, signed
//  NMQu    sample noise, medium quality, unsigned
//  NMQs    sample noise, medium quality, signed
//  NHQu    sample noise, high quality, unsigned
//  NHQs    sample noise, high quality, signed
//
// WARNING: DON'T USE THESE HELPER FUNCTIONS FROM PIXEL SHADERS
//          DOING TEXTURING - THEY ALL FORCE THE ZERO MIP LEVEL
//          AND YOU'LL GET ALIASING.
//
// The low and medium qualities are pretty close to each other,
// both in computational complexity and speed.  The high quality
// functions do manual trilinear interpolation and are much
// slower, but also much more accurate, especially when 'ws'
// is changing very slowly over space, and the result of the
// noise value fetched is being highly amplified.  
//
// The low quality is a simple HW trilinear fetch.
// The medium quality is similar but first-order continuous,
//   because it warps the input coordinate to be first-order
//   continuous.  (Looks much better for lighting because
//   lighting is based on the derivative.)
// The high quality is much slower.  It can optionally do the 
//   warping/smoothing that the medium quality gives you
//   (set 'smooth' to zero or one).  See comments inside
//   the function itself, as well as the file 
//   textures\about_these_noise_volumes.txt.

float4 NLQu( float3 _UVW, Texture3D _Noise ) { return _Noise.SampleLevel(LinearWrap, _UVW, 0); }
float4 NLQs( float3 _UVW, Texture3D _Noise ) { return NLQu(_UVW, _Noise)*2-1; }

float4 NMQu( float3 _UVW, Texture3D _Noise )
{
	// smooth the input coord
	float3	t = frac(_UVW * NOISE_LATTICE_SIZE + 0.5);
	float3	t2 = (3 - 2*t)*t*t;
	float3	uvw2 = _UVW + (t2-t)/(float)(NOISE_LATTICE_SIZE);

	// fetch
	return NLQu(uvw2, _Noise);
}

float4 NMQs( float3 _UVW, Texture3D _Noise )
{
	// smooth the input coord
	float3	t = frac(_UVW * NOISE_LATTICE_SIZE + 0.5);
	float3	t2 = (3 - 2*t)*t*t;
	float3	uvw2 = _UVW + (t2-t)/(float)(NOISE_LATTICE_SIZE);

	// fetch  
	return NLQs(uvw2, _Noise);
}


// SUPER MEGA HIGH QUALITY noise sampling (unsigned)
float NHQu(float3 _UVW, Texture3D _Noise, const float smooth = 1) 
{
	float3	FloorUVW = floor(_UVW * NOISE_LATTICE_SIZE) * INV_LATTICE_SIZE;
	float3	t = (_UVW - FloorUVW) * NOISE_LATTICE_SIZE;
			t = lerp( t, t*t*(3 - 2*t), smooth );
 
	float2	d = float2( INV_LATTICE_SIZE, 0 );

#if 0
	// the 8-lookup version... (SLOW)
	float4	f1 = float4( _Noise.SampleLevel(NearestWrap, FloorUVW + d.xxx, 0).x, 
						 _Noise.SampleLevel(NearestWrap, FloorUVW + d.yxx, 0).x, 
						 _Noise.SampleLevel(NearestWrap, FloorUVW + d.xyx, 0).x, 
						 _Noise.SampleLevel(NearestWrap, FloorUVW + d.yyx, 0).x );
	float4	f2 = float4( _Noise.SampleLevel(NearestWrap, FloorUVW + d.xxy, 0).x, 
						 _Noise.SampleLevel(NearestWrap, FloorUVW + d.yxy, 0).x, 
						 _Noise.SampleLevel(NearestWrap, FloorUVW + d.xyy, 0).x, 
						 _Noise.SampleLevel(NearestWrap, FloorUVW + d.yyy, 0).x );
	float4	f3 = lerp(f2, f1, t.zzzz);
	float2	f4 = lerp(f3.zw, f3.xy, t.yy);
	float	f5 = lerp(f4.y, f4.x, t.x);
#else
	// THE TWO-SAMPLE VERSION: much faster!
	// NOTE: requires that three YZ-neighbor texels' original .x values are packed into .yzw values of each texel.
	float4	f1 = _Noise.SampleLevel(NearestWrap, FloorUVW        , 0);	// <+0, +y,  +z,  +yz>
	float4	f2 = _Noise.SampleLevel(NearestWrap, FloorUVW + d.xyy, 0);	// <+x, +xy, +xz, +xyz>
	float4	f3 = lerp(f1, f2, t.xxxx);										// <+0, +y,  +z,  +yz> (X interpolation)
	float2	f4 = lerp(f3.xz, f3.yw, t.yy);									// <+0, +z> (Y interpolation)
	float	f5 = lerp(f4.x, f4.y, t.z);										// Z interpolation
#endif
  
	return f5;
}

float NHQs(float3 _UVW, Texture3D _Noise, const float smooth = 1) { return NHQu(_UVW, _Noise, smooth)*2-1; }

// ===================================================================================
// Noise + Derivatives
// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
//
static const float2 NoiseDelta = float2( INV_LATTICE_SIZE, 0.0 );

float Noise( float3 _UVW, Texture3D _Noise, out float3 _Derivatives ) 
{
	float3	FloorUVW = floor(_UVW * NOISE_LATTICE_SIZE) * INV_LATTICE_SIZE;
	float3	uvw = (_UVW - FloorUVW) * NOISE_LATTICE_SIZE;

	// Quintic interpolation from Ken Perlin :
	//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
	//	du/dx = 30x^4 - 60x^3 + 30x^2
	//
    float3	dudvdw = 30.0*uvw*uvw*(uvw*(uvw-2.0)+1.0);
	uvw = uvw*uvw*uvw*(uvw*(uvw*6.0-15.0)+10.0);

	float4	N0 = _Noise.SampleLevel(NearestWrap, FloorUVW , 0);					// <+0, +y,  +z,  +yz>
	float4	N1 = _Noise.SampleLevel(NearestWrap, FloorUVW + NoiseDelta.xyy, 0);	// <+x, +xy, +xz, +xyz>

	float	a = N0.x;
	float	b = N1.x;
	float	c = N0.y;
	float	d = N1.y;
	float	e = N0.z;
	float	f = N1.z;
	float	g = N0.w;
	float	h = N1.w;

    float	k0 =   a;
    float	k1 =   b - a;
    float	k2 =   c - a;
    float	k3 =   e - a;
    float	k4 =   a - b - c + d;
    float	k5 =   a - c - e + g;
    float	k6 =   a - b - e + f;
    float	k7 = - a + b + c - d + e - f - g + h;

	_Derivatives.x = dudvdw.x * (k1 + k4*uvw.y + k6*uvw.z + k7*uvw.y*uvw.z);
    _Derivatives.y = dudvdw.y * (k2 + k5*uvw.z + k4*uvw.x + k7*uvw.z*uvw.x);
    _Derivatives.z = dudvdw.z * (k3 + k6*uvw.x + k5*uvw.y + k7*uvw.x*uvw.y);

	return k0 + k1*uvw.x + k2*uvw.y + k3*uvw.z + k4*uvw.x*uvw.y + k5*uvw.y*uvw.z + k6*uvw.z*uvw.x + k7*uvw.x*uvw.y*uvw.z;
}
