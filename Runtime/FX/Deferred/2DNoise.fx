
#define NOISE_LATTICE_SIZE 32
#define INV_LATTICE_SIZE (1.0 / NOISE_LATTICE_SIZE)

Texture2D NoiseTexture0;
Texture2D NoiseTexture1;
Texture2D NoiseTexture2;
Texture2D NoiseTexture3;

// Noise + Derivatives
// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
//
float Noise( float2 _UV, Texture2D _Noise, out float2 _Derivatives ) 
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

float NoiseDeriv2( float2 _UV, Texture2D _Noise, out float2 _Derivatives, out float2 _Derivatives2 )
{
	float2	FloorUV = floor(_UV * NOISE_LATTICE_SIZE) * INV_LATTICE_SIZE;
	float2	uv = (_UV - FloorUV) * NOISE_LATTICE_SIZE;

	// Quintic interpolation from Ken Perlin :
	//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
	//	du/dx = 30x^4 - 60x^3 + 30x^2
	//	du²/dx² = 120x^3 - 180x^2 + 60x
	//
	float2	dudv = 30.0*uv*uv*(uv*(uv-2.0)+1.0);
	float2	dudv2 = 60.0*uv*(uv*(uv*2.0-3.0)+1.0);
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
	_Derivatives2 = dudv2 * float2( k1 + k3*uv.y, k2 + k3*uv.x );

	return k0 + k1*uv.x + k2*uv.y + k3*uv.x*uv.y;
}
