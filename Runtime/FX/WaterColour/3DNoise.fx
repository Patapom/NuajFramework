#define NOISE_LATTICE_SIZE 16
#define INV_LATTICE_SIZE (1.0/(float)(NOISE_LATTICE_SIZE))

static const float2 NoiseDelta = float2( INV_LATTICE_SIZE, 0.0 );

Texture3D NoiseTexture0 : NOISE3D_TEX0;
Texture3D NoiseTexture1 : NOISE3D_TEX1;
Texture3D NoiseTexture2 : NOISE3D_TEX2;
Texture3D NoiseTexture3 : NOISE3D_TEX3;

// Noise + Derivatives
// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
//
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

/*

// Noise + Derivatives
// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
//
float Noise2D( float2 _UV, Texture2D _Noise, out float2 _Derivatives ) 
{
	float2	FloorUV = floor(_UV * NOISE_LATTICE_SIZE) * INV_LATTICE_SIZE;
	float2	uv = (_UV - FloorUV) * NOISE_LATTICE_SIZE;

	// Quintic interpolation from Ken Perlin :
	//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
	//	du/dx = 30x^4 - 60x^3 + 30x^2
	//
    float2	dudv = 30.0*uv*uv*(uv*(uv-2.0)+1.0);
 	uv = uv*uv*uv*(uv*(uv*6.0-15.0)+10.0);

	float4	N = _Noise.SampleLevel( NearestWrap, FloorUV, 0 );	// <+0, +x,  +y,  +xy>

	float	a = N.x;	// (0,0)
	float	b = N.y;	// (+x,0)
	float	c = N.z;	// (0,+y)
	float	d = N.w;	// (+x,+y)

    float	k0 = a;
    float	k1 = b - a;
    float	k2 = c - a;
    float	k3 = a - b - c + d;

	_Derivatives = dudv * float2( k1 + k3*uv.y, k2 + k3*uv.x );

	return k0 + k1*uv.x + k2*uv.y + k3*uv.x*uv.y;
}

*/