// This file gathers standard SH helpers
//
#ifndef SH_SUPPORT
#define SH_SUPPORT

// Ambient SH data
float2			SHEnvMapSize: SHENVMAP_SIZE;		// The size factors
float2			SHEnvOffset : SHENVMAP_OFFSET;		// The offset to apply to XZ world coordinates
float2			SHEnvScale	: SHENVMAP_SCALE;		// The scale to apply to offseted XZ world coordinates
Texture3D		SHEnvMap	: SHENVMAP;				// The 3D map storing 9 SH coefficients in WORLD space (UV=WorldPosition W=SHCoeffIndex)

// Rotates ZH coefficients in the specified direction (from "Stupid SH Tricks")
// Rotating ZH comes to evaluating scaled SH in the given direction.
// The scaling factors for each band are equal to the ZH coefficients multiplied by sqrt( 4PI / (2l+1) )
//
void ZHRotate( const in float3 _Direction, const in float3 _ZHCoeffs, out float _Coeffs[9] )
{
	float	cl0 = 3.5449077018110320545963349666823 * _ZHCoeffs.x;	// sqrt(4PI)
	float	cl1 = 2.0466534158929769769591032497785 * _ZHCoeffs.y;	// sqrt(4PI/3)
	float	cl2 = 1.5853309190424044053380115060481 * _ZHCoeffs.z;	// sqrt(4PI/5)

	float	f0 = cl0 * 0.28209479177387814347403972578039;	// 0.5 / sqrt(PI);
	float	f1 = cl1 * 0.48860251190291992158638462283835;	// 0.5 * sqrt(3.0/PI);
	float	f2 = cl2 * 1.0925484305920790705433857058027;	// 0.5 * sqrt(15.0/PI);
	_Coeffs[0] = f0;
	_Coeffs[1] = -f1 * _Direction.x;
	_Coeffs[2] = f1 * _Direction.y;
	_Coeffs[3] = -f1 * _Direction.z;
	_Coeffs[4] = f2 * _Direction.x * _Direction.z;
	_Coeffs[5] = -f2 * _Direction.x * _Direction.y;
	_Coeffs[6] = f2 * 0.28209479177387814347403972578039 * (3.0 * _Direction.y*_Direction.y - 1.0);
	_Coeffs[7] = -f2 * _Direction.z * _Direction.y;
	_Coeffs[8] = f2 * 0.5 * (_Direction.z*_Direction.z - _Direction.x*_Direction.x);
}

// Builds a spherical harmonics cone lobe
// (from "Stupid SH Tricks")
//
void BuildSHCone( const in float3 _Direction, float _Angle, out float _Coeffs[9] )
{
	float3 ZHCoeffs = float3(
			1.7724538509055160272981674833411 * (1.0 - cos(_Angle)),									// sqrt(PI) (1 - cos(a))
			1.5349900619197327327193274373339 * sin(_Angle) * sin(_Angle),								// 0.5 sqrt(3PI) sin(a)^2
			1.9816636488030055066725143825601 * cos(_Angle) * (cos(_Angle) - 1.0) * (cos(_Angle) + 1.0)	// 0.5 sqrt(5PI) cos(a) (cos(a)-1) (cos(a)+1)
		 );

	ZHRotate( _Direction, ZHCoeffs, _Coeffs );
}

// Builds a spherical harmonics cosine lobe
// (from "Stupid SH Tricks")
//
void BuildSHCosineLobe( const in float3 _Direction, out float _Coeffs[9] )
{
	static const float3 ZHCoeffs = float3(
		0.88622692545275801364908374167057,	// sqrt(PI) / 2
		1.0233267079464884884795516248893,	// sqrt(PI / 3)
		0.49541591220075137666812859564002	// sqrt(5PI) / 8
		);
	ZHRotate( _Direction, ZHCoeffs, _Coeffs );
}

// Performs r = a * b
//
void SHProduct( const in float4 a[9], const in float4 b[9], out float4 r[9] )
{
// 	// The convolution's Clebsch-Gordan coefficients were precomputed using the SphericalHarmonics.SHFunctions.Convolve() method
// 	const float	C0 = 0.282094791773878;
// 	const float	C1 = 0.126156626101008;
// 	const float	C2 = 0.218509686118416;
// 	const float	C3 = 0.309019361618552;
// 	const float	C4 = 0.252313252202016;
// 	const float	C5 = 0.180223751572869;
// 	const float	C6 = 0.220728115441823;
// 	const float	C7 = 0.0901118757864343;
// 	// DC Band
// 	Out.SH0.xyz = C0 * (a[0]*b[0]+a[1]*b[1]+a[2]*b[2]+a[3]*b[3]+a[4]*b[4]+a[5]*b[5]+a[6]*b[6]+a[7]*b[7]+a[8]*b[8]);
// 	// 1st band
// 	Out.SH1.xyz = C0 * (-a[0]*b[3]+a[1]*b[0]) + C1 * (-a[1]*b[6]+a[6]*b[3]) + C2 * (-a[2]*b[7]+a[5]*b[2]) + C3 * (-a[3]*b[8]+a[4]*b[1]);
// 	Out.SH3.xyz = C0 * (-a[0]*b[1]+a[3]*b[0]) + C1 * (-a[3]*b[6]+a[6]*b[1]) + C2 * (-a[2]*b[5]+a[7]*b[2]) + C3 * (-a[1]*b[4]+a[8]*b[3]);
// 	Out.SH2.xyz = C0 * (a[0]*b[2]+a[2]*b[0]) + C2 * (a[1]*b[5]+a[3]*b[7]+a[5]*b[1]+a[7]*b[3]) + C4 * (a[2]*b[6]+a[6]*b[2]);
// 	// 2nd band
// 	Out.SH4.xyz = C0 * (a[0]*b[8]+a[4]*b[0]) + C3 * -a[1]*b[3] + C5 * (-a[4]*b[6]-a[6]*b[8]) + C6 * -a[5]*b[7];
// 	Out.SH8.xyz = C0 * (a[0]*b[4]+a[8]*b[0]) + C3 * -a[3]*b[1] + C5 * (-a[6]*b[4]-a[8]*b[6]) + C6 * -a[7]*b[5];
// 	Out.SH5.xyz = C0 * (-a[0]*b[7]+a[5]*b[0]) + C2 * (a[1]*b[2]-a[2]*b[3]) + C6 * (a[4]*b[5]-a[7]*b[8]) + C7 * (a[5]*b[6]-a[6]*b[7]);
// 	Out.SH7.xyz = C0 * (-a[0]*b[5]+a[7]*b[0]) + C2 * (-a[2]*b[1]+a[3]*b[2]) + C6 * (-a[5]*b[4]+a[8]*b[7]) + C7 * (-a[6]*b[5]+a[7]*b[6]);
// 	Out.SH6.xyz = C0 * (a[0]*b[6]+a[6]*b[0]) + C1 * (-a[1]*b[1]-a[3]*b[3]) + C4 * +a[2]*b[2] + C5 * (-a[4]*b[4]+a[6]*b[6]-a[8]*b[8]) + C7 * (a[5]*b[5]+a[7]*b[7]);
// 	// 104 muls 84 adds


	// This code from John Snyder
	//
	float4	ta, tb, t;

	const float	C0 = 0.282094792935999980;
	const float	C1 = -0.126156626101000010;
	const float	C2 = 0.218509686119999990;
	const float	C3 = 0.252313259986999990;
	const float	C4 = 0.180223751576000010;
	const float	C5 = 0.156078347226000000;
	const float	C6 = 0.090111875786499998;

	// [0,0]: 0,
	r[0] = C0*a[0]*b[0];

	// [1,1]: 0,6,8,
	ta = C0*a[0]+C1*a[6]-C2*a[8];
	tb = C0*b[0]+C1*b[6]-C2*b[8];
	r[1] = ta*b[1]+tb*a[1];
	t = a[1]*b[1];
	r[0] += C0*t;
	r[6] = C1*t;
	r[8] = -C2*t;

	// [1,2]: 5,
	ta = C2*a[5];
	tb = C2*b[5];
	r[1] += ta*b[2]+tb*a[2];
	r[2] = ta*b[1]+tb*a[1];
	t = a[1]*b[2]+a[2]*b[1];
	r[5] = C2*t;

	// [1,3]: 4,
	ta = C2*a[4];
	tb = C2*b[4];
	r[1] += ta*b[3]+tb*a[3];
	r[3] = ta*b[1]+tb*a[1];
	t = a[1]*b[3]+a[3]*b[1];
	r[4] = C2*t;

	// [2,2]: 0,6,
	ta = C0*a[0]+C3*a[6];
	tb = C0*b[0]+C3*b[6];
	r[2] += ta*b[2]+tb*a[2];
	t = a[2]*b[2];
	r[0] += C0*t;
	r[6] += C3*t;

	// [2,3]: 7,
	ta = C2*a[7];
	tb = C2*b[7];
	r[2] += ta*b[3]+tb*a[3];
	r[3] += ta*b[2]+tb*a[2];
	t = a[2]*b[3]+a[3]*b[2];
	r[7] = C2*t;

	// [3,3]: 0,6,8,
	ta = C0*a[0]+C1*a[6]+C2*a[8];
	tb = C0*b[0]+C1*b[6]+C2*b[8];
	r[3] += ta*b[3]+tb*a[3];
	t = a[3]*b[3];
	r[0] += C0*t;
	r[6] += C1*t;
	r[8] += C2*t;

	// [4,4]: 0,6,
	ta = C0*a[0]-C4*a[6];
	tb = C0*b[0]-C4*b[6];
	r[4] += ta*b[4]+tb*a[4];
	t = a[4]*b[4];
	r[0] += C0*t;
	r[6] -= C4*t;

	// [4,5]: 7,
	ta = C5*a[7];
	tb = C5*b[7];
	r[4] += ta*b[5]+tb*a[5];
	r[5] += ta*b[4]+tb*a[4];
	t = a[4]*b[5]+a[5]*b[4];
	r[7] += C5*t;

	// [5,5]: 0,6,8,
	ta = C0*a[0]+C6*a[6]-C5*a[8];
	tb = C0*b[0]+C6*b[6]-C5*b[8];
	r[5] += ta*b[5]+tb*a[5];
	t = a[5]*b[5];
	r[0] += C0*t;
	r[6] += C6*t;
	r[8] -= C5*t;

	// [6,6]: 0,6,
	ta = C0*a[0];
	tb = C0*b[0];
	r[6] += ta*b[6]+tb*a[6];
	t = a[6]*b[6];
	r[0] += C0*t;
	r[6] += C4*t;

	// [7,7]: 0,6,8,
	ta = C0*a[0]+C6*a[6]+C5*a[8];
	tb = C0*b[0]+C6*b[6]+C5*b[8];
	r[7] += ta*b[7]+tb*a[7];
	t = a[7]*b[7];
	r[0] += C0*t;
	r[6] += C6*t;
	r[8] += C5*t;

	// [8,8]: 0,6,
	ta = C0*a[0]-C4*a[6];
	tb = C0*b[0]-C4*b[6];
	r[8] += ta*b[8]+tb*a[8];
	t = a[8]*b[8];
	r[0] += C0*t;
	r[6] -= C4*t;
	// entry count=13
	// multiply count=120
	// addition count=74
}

// Performs r = a * b
// where b is a monochromatic SH vector
//
void SHProduct( const in float3 a[9], const in float b[9], out float3 r[9] )
{
	// This code from John Snyder
	//
	float3	ta, tb, t;

	const float	C0 = 0.282094792935999980;
	const float	C1 = -0.126156626101000010;
	const float	C2 = 0.218509686119999990;
	const float	C3 = 0.252313259986999990;
	const float	C4 = 0.180223751576000010;
	const float	C5 = 0.156078347226000000;
	const float	C6 = 0.090111875786499998;

	// [0,0]: 0,
	r[0] = C0*a[0]*b[0];

	// [1,1]: 0,6,8,
	ta = C0*a[0]+C1*a[6]-C2*a[8];
	tb = C0*b[0]+C1*b[6]-C2*b[8];
	r[1] = ta*b[1]+tb*a[1];
	t = a[1]*b[1];
	r[0] += C0*t;
	r[6] = C1*t;
	r[8] = -C2*t;

	// [1,2]: 5,
	ta = C2*a[5];
	tb = C2*b[5];
	r[1] += ta*b[2]+tb*a[2];
	r[2] = ta*b[1]+tb*a[1];
	t = a[1]*b[2]+a[2]*b[1];
	r[5] = C2*t;

	// [1,3]: 4,
	ta = C2*a[4];
	tb = C2*b[4];
	r[1] += ta*b[3]+tb*a[3];
	r[3] = ta*b[1]+tb*a[1];
	t = a[1]*b[3]+a[3]*b[1];
	r[4] = C2*t;

	// [2,2]: 0,6,
	ta = C0*a[0]+C3*a[6];
	tb = C0*b[0]+C3*b[6];
	r[2] += ta*b[2]+tb*a[2];
	t = a[2]*b[2];
	r[0] += C0*t;
	r[6] += C3*t;

	// [2,3]: 7,
	ta = C2*a[7];
	tb = C2*b[7];
	r[2] += ta*b[3]+tb*a[3];
	r[3] += ta*b[2]+tb*a[2];
	t = a[2]*b[3]+a[3]*b[2];
	r[7] = C2*t;

	// [3,3]: 0,6,8,
	ta = C0*a[0]+C1*a[6]+C2*a[8];
	tb = C0*b[0]+C1*b[6]+C2*b[8];
	r[3] += ta*b[3]+tb*a[3];
	t = a[3]*b[3];
	r[0] += C0*t;
	r[6] += C1*t;
	r[8] += C2*t;

	// [4,4]: 0,6,
	ta = C0*a[0]-C4*a[6];
	tb = C0*b[0]-C4*b[6];
	r[4] += ta*b[4]+tb*a[4];
	t = a[4]*b[4];
	r[0] += C0*t;
	r[6] -= C4*t;

	// [4,5]: 7,
	ta = C5*a[7];
	tb = C5*b[7];
	r[4] += ta*b[5]+tb*a[5];
	r[5] += ta*b[4]+tb*a[4];
	t = a[4]*b[5]+a[5]*b[4];
	r[7] += C5*t;

	// [5,5]: 0,6,8,
	ta = C0*a[0]+C6*a[6]-C5*a[8];
	tb = C0*b[0]+C6*b[6]-C5*b[8];
	r[5] += ta*b[5]+tb*a[5];
	t = a[5]*b[5];
	r[0] += C0*t;
	r[6] += C6*t;
	r[8] -= C5*t;

	// [6,6]: 0,6,
	ta = C0*a[0];
	tb = C0*b[0];
	r[6] += ta*b[6]+tb*a[6];
	t = a[6]*b[6];
	r[0] += C0*t;
	r[6] += C4*t;

	// [7,7]: 0,6,8,
	ta = C0*a[0]+C6*a[6]+C5*a[8];
	tb = C0*b[0]+C6*b[6]+C5*b[8];
	r[7] += ta*b[7]+tb*a[7];
	t = a[7]*b[7];
	r[0] += C0*t;
	r[6] += C6*t;
	r[8] += C5*t;

	// [8,8]: 0,6,
	ta = C0*a[0]-C4*a[6];
	tb = C0*b[0]-C4*b[6];
	r[8] += ta*b[8]+tb*a[8];
	t = a[8]*b[8];
	r[0] += C0*t;
	r[6] -= C4*t;
	// entry count=13
	// multiply count=120
	// addition count=74
}

// Retrieves the ambient SH at the specified world position
//
void GetAmbientSH( const in float3 _WorldPosition, out float3 _EnvSH[9] )
{
	float2	EnvMapUV = (_WorldPosition.xz - SHEnvOffset) * SHEnvScale;

	// Read the SH environment at current position
	const bool bUseBilinear = true;
	if ( !bUseBilinear )
	{	// Use Iñigo's (originally Perlin's !) trick of using a quintic curve to interpolate values
		EnvMapUV = EnvMapUV * SHEnvMapSize.xx;
		float2	IntEnvMapUV = floor( EnvMapUV );
		float2	uv = EnvMapUV - IntEnvMapUV;
		uv = uv*uv*uv*(uv*(uv*6.0-15.0)+10.0);
//		uv = uv*uv*(3.0-2.0*uv);
		EnvMapUV = IntEnvMapUV + uv;
		EnvMapUV = EnvMapUV * SHEnvMapSize.yy;
	}

	// Read the SH environment at current position
	float4 C0 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 0.5 / 7.0 ), 0 );
	float4 C1 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 1.5 / 7.0 ), 0 );
	float4 C2 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 2.5 / 7.0 ), 0 );
	float4 C3 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 3.5 / 7.0 ), 0 );
	float4 C4 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 4.5 / 7.0 ), 0 );
	float4 C5 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 5.5 / 7.0 ), 0 );
	float4 C6 = SHEnvMap.SampleLevel( LinearClamp, float3( EnvMapUV, 6.5 / 7.0 ), 0 );

	// Unpack the 9 coefficients
	_EnvSH[0] = C0.xyz;
	_EnvSH[1] = float3( C0.w, C1.xy );
	_EnvSH[2] = float3( C1.zw, C2.x );
	_EnvSH[3] = C2.yzw;
	_EnvSH[4] = C3.xyz;
	_EnvSH[5] = float3( C3.w, C4.xy );
	_EnvSH[6] = float3( C4.zw, C5.x );
	_EnvSH[7] = C5.yzw;
	_EnvSH[8] = C6.xyz;
}

// Evaluates the ambient SH coefficients in the requested direction
//
float3	EvaluateAmbientSH( const in float3 _WorldPosition, const in float3 _Direction )
{
	// Read the SH environment at current position
	float3	SH[9];
	GetAmbientSH( _WorldPosition, SH );

	float4	EvalSH1234, EvalSH5678;
	float	f0 = 0.28209479177387814347403972578039;		//0.5 / Math.Sqrt(Math.PI);
	float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0
	float	f2 = 3.8729833462074168851792653997824 * f0;	// sqrt(15.0) * f0

	float	EvalSH0 = f0;
	EvalSH1234.x = -f1 * _Direction.x;
	EvalSH1234.y = f1 * _Direction.y;
	EvalSH1234.z = -f1 * _Direction.z;
	EvalSH1234.w = f2 * _Direction.x * _Direction.z;
	EvalSH5678.x = -f2 * _Direction.x * _Direction.y;
	EvalSH5678.y = f2 * 0.28867513459481288225457439025097 * (3.0 * _Direction.y*_Direction.y - 1.0);
	EvalSH5678.z = -f2 * _Direction.z * _Direction.y;
	EvalSH5678.w = f2 * 0.5 * (_Direction.z*_Direction.z - _Direction.x*_Direction.x);

	// Dot the SH together
	return max( 0.0,
		   EvalSH0 * SH[0]
		 + EvalSH1234.x * SH[1]
		 + EvalSH1234.y * SH[2]
		 + EvalSH1234.z * SH[3]
		 + EvalSH1234.w * SH[4]
		 + EvalSH5678.x * SH[5]
		 + EvalSH5678.y * SH[6]
		 + EvalSH5678.z * SH[7]
		 + EvalSH5678.w * SH[8] );
}

// Evaluates the irradiance perceived in the provided direction
// Analytic method from http://www1.cs.columbia.edu/~ravir/papers/envmap/envmap.pdf
//
float3	EvaluateAmbientSHIrradiance( float3 _WorldPosition, float3 _Direction )
{
	// Read the SH environment at current position
	float3	SH[9];
	GetAmbientSH( _WorldPosition, SH );

	const float	c1 = 0.429043;
	const float	c2 = 0.511664;
	const float	c3 = 0.743125;
	const float	c4 = 0.886227;
	const float	c5 = 0.247708;

	float3	XYZ = float3( -_Direction.z, -_Direction.x, _Direction.y );
	return	max( 0.0,
		    c1*SH[8]*(XYZ.x*XYZ.x - XYZ.y*XYZ.y)
		  + SH[6]*(c3*XYZ.z*XYZ.z - c5)
		  + c4*SH[0]
		  + 2.0*c1*(SH[4]*XYZ.x*XYZ.y + SH[7]*XYZ.x*XYZ.z + SH[5]*XYZ.y*XYZ.z)
		  + 2.0*c2*(SH[3]*XYZ.x + SH[1]*XYZ.y + SH[2]*XYZ.z) );
}

#endif