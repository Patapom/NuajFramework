// This shader interface provides support for the ambient sky light
//
#if !defined(AMBIENT_SKY_SUPPORT_FX)
#define AMBIENT_SKY_SUPPORT_FX

#include "../Samplers.fx"

// Ambient sky map
// A texture array of 3 elements => 3*4 components = 4 RGB SH coefficients
Texture2DArray	_TexAmbientSkySH		: AMBIENT_SKY_SH_TEXTURE;

// ===================================================================================

// Samples SH Coefficients
void	SampleSkySHCoefficents( out float3 _SHCoeffs[4] )
{
	float4	V0 = _TexAmbientSkySH.SampleLevel( NearestClamp, float3( 0.5.xx, 0.0 ), 0.0 );
	float4	V1 = _TexAmbientSkySH.SampleLevel( NearestClamp, float3( 0.5.xx, 1.0 ), 0.0 );
	float4	V2 = _TexAmbientSkySH.SampleLevel( NearestClamp, float3( 0.5.xx, 2.0 ), 0.0 );

	_SHCoeffs[0] = V0.xyz;
	_SHCoeffs[1] = float3( V0.w, V1.xy );
	_SHCoeffs[2] = float3( V1.zw, V2.x );
	_SHCoeffs[3] = V2.yzw;
}

// Computes ambient sky radiance in the given direction
//
float3	ComputeAmbientSkyRadiance( float3 _Direction )
{
	float3	SHCoeffs[4];
	SampleSkySHCoefficents( SHCoeffs );

	// Create the SH coefficients in the specified direction
	// From http://www1.cs.columbia.edu/~ravir/papers/envmap/envmap.pdf
	const float	f0 = 0.28209479177387814347403972578039;		// 0.5 / Sqrt(PI);
	const float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0

	float4	EvalSH = float4( f0,
							-f1 * _Direction.x,
							 f1 * _Direction.y,
							-f1 * _Direction.z );

	// Dot the SH together
	return max( 0.0,
		   EvalSH.x * SHCoeffs[0]
		 + EvalSH.y * SHCoeffs[1]
		 + EvalSH.z * SHCoeffs[2]
		 + EvalSH.w * SHCoeffs[3] );
}

// Computes ambient sky irradiance in the given direction
//
float3	ComputeAmbientSkyIrradiance( float3 _Direction )
{
	float3	SHCoeffs[4];
	SampleSkySHCoefficents( SHCoeffs );

	// Create the SH coefficients in the specified direction
	// From http://www1.cs.columbia.edu/~ravir/papers/envmap/envmap.pdf
	const float	f0 = 0.28209479177387814347403972578039;		// 0.5 / Sqrt(PI);
	const float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0

	const float	A0 = 3.1415926535897932384626433832795;			// PI
	const float	A1 = 2.0943951023931954923084289221863;			// 2PI/3

	const float	c0 = A0 * f0;
	const float	c1 = A1 * f1;

	float4	EvalSH = float4( c0,
							-c1 * _Direction.x,
							 c1 * _Direction.y,
							-c1 * _Direction.z );

	// Dot the SH together
	return max( 0.0,
		   EvalSH.x * SHCoeffs[0]
		 + EvalSH.y * SHCoeffs[1]
		 + EvalSH.z * SHCoeffs[2]
		 + EvalSH.w * SHCoeffs[3] );
}

#endif	// AMBIENT_SKY_SUPPORT_FX
