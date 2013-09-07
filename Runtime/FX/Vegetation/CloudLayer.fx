// Draws a cloud layer in the sky
//
#include "../Camera.fx"
#include "../Samplers.fx"

static const float	HALF_PI = 1.5707963267948966192313216916398;

static const float3	EARTH_CENTER = 0.0;
static const float	EARTH_RADIUS = 6400.0;
static const float	CLOUD_RADIUS = EARTH_RADIUS + 10.0;	// 10km high

Texture2D	SourceBuffer;				// Original emissive buffer rendered from last pass

float4		ShadowAngularBounds;		// X=AngleMinX Y=AngleMinY Z=DeltaAngleX W=DeltaAngleY
float4		ShadowInvAngularBounds;		// X=-AngleMinX Y=-AngleMinY Z=1/DeltaAngleX W=1/DeltaAngleY
float4x4	Shadow2World;
float4x4	World2Shadow;
Texture2D	ShadowMap;

float		WorldUnit2Kilometer;		// World scale factor
float3		SunDirection;				// Pointing TOWARD the Sun
float3		SunColor;
float3		SkyColor;

Texture2D	PhaseMie;
Texture2D	PhaseMie_Convolved;
float		ScatteringCoeff;
float		ExtinctionCoeff;

Texture2D	NoiseTexture2D0;
Texture2D	NoiseTexture2D1;
Texture2D	NoiseTexture2D2;
Texture2D	NoiseTexture2D3;
Texture3D	NoiseTexture0;
Texture3D	NoiseTexture1;
Texture3D	NoiseTexture2;
Texture3D	NoiseTexture3;

float2		BufferInvSize;

float		CloudDensityOffset;
float		LayerThickness;
float		NoiseSize;
float		CloudEvolutionSpeed;
float		CloudSpeed;
float		CloudTime;
float		CloudPower;
float2		FrequencyFactor;
float4		AmplitudeFactor;
float		NoiseMipBias;
float		NormalAmplitude;

float4		ScatteringFactors;
float		ScatteringSkyFactor;

struct VS_IN
{
	float4	Position	: SV_POSITION;	// Depth-pass renderables need only declare a position
};

VS_IN VS( VS_IN _In )	{ return _In; }


// ===================================================================================
// Taylor series for Atan (source: http://en.wikipedia.org/wiki/Taylor_series)
//
float	Atan_DL( float x )
{
	float	x2 = x*x;
	float	x3 = x*x2;
	float	x5 = x3*x2;
	return x - 0.33333333333333 * x3 + 0.2 * x5;
}

// Taylor series for Asin (source: http://en.wikipedia.org/wiki/Taylor_series)
//
float	Asin_DL( float x )
{
	if ( x < 0.5f )
	{
		float	x2 = x*x;
		float	x3 = x*x2;
		float	x5 = x3*x2;
		return x + 0.16666666666666666666666666666667 * x3 + 0.075 * x5;
	}
	else
	{
		x = sqrt( 1.0f - x*x );
		float	x2 = x*x;
		float	x3 = x*x2;
		float	x5 = x3*x2;
		return HALF_PI - (x + 0.16666666666666666666666666666667 * x3 + 0.075 * x5);
	}
}

// Taylor series for Cos (source: http://en.wikipedia.org/wiki/Taylor_series)
//
float	Sin_DL( float x )
{
	float	x2 = x * x;
	float	x3 = x2 * x;
	float	x5 = x2 * x3;
	return x - 0.16666666666666666666666666666667 * x3 + 0.00833333333333333333333333333333 * x5;
}

// Taylor series for Sin (source: http://en.wikipedia.org/wiki/Taylor_series)
//
float	Cos_DL( float x )
{
	float	x2 = x * x;
	float	x4 = x2 * x2;
	float	x6 = x4 * x2;
	return 1.0 - 0.5 * x2 + 0.04166666666666666666666666666667 * x4 - 0.00138888888888888888888888888889 * x6;
}

float2	SinCos_DL( float x )
{
	float	x2 = x * x;
	float	x3 = x2 * x;
	float	x4 = x2 * x2;
	float	x5 = x2 * x3;
	float	x6 = x4 * x2;
	return float2(	x - 0.16666666666666666666666666666667 * x3 + 0.00833333333333333333333333333333 * x5,
					1.0 - 0.5 * x2 + 0.04166666666666666666666666666667 * x4 - 0.00138888888888888888888888888889 * x6 );
}

// Compute the forward intersection of a ray with a sphere
// Returns -1 if no forward hit
float	ComputeSphereIntersection( float3 _Position, float3 _Direction, float3 _SphereCenter, float _SphereRadius )
{
	float3	D = _Position - _SphereCenter;
	float	a = dot( _Direction, _Direction );
	float	b = dot( _Direction, D );
	float	c = dot( D, D ) - _SphereRadius*_SphereRadius;
	float	Delta = b*b - a*c;
	if ( Delta < 0.0 )
		return -1.0;

	Delta = sqrt( Delta );
	a = 1.0 / a;

	float	t0 = (-b-Delta) * a;
	float	t1 = (-b+Delta) * a;

	if ( t1 < 0.0 )
		return -1.0;	// Both hits stand behind start position
	if ( t0 < 0.0 )
		return t1;		// First hit stands behind start position

	return t0;
}

// Projects a WORLD position (in kilometers !) into the shadow map space
// Returns a UV to sample the shadow map
float2	ProjectWorld2Shadow( float3 _WorldPositionKm )
{
	float3	ShadowSpherePosition = _WorldPositionKm - EARTH_CENTER;
			ShadowSpherePosition = mul( float4( ShadowSpherePosition, 0.0 ), World2Shadow ).xyz;	// Position in SHADOW space
			ShadowSpherePosition = normalize( ShadowSpherePosition );

	// Compute angular deviations
	float2	Angles = float2( Atan_DL( ShadowSpherePosition.z / ShadowSpherePosition.x ), Asin_DL( ShadowSpherePosition.y ) );

	// Normalize deviations
	Angles = (Angles + ShadowInvAngularBounds.xy) * ShadowInvAngularBounds.zw;

	return Angles;
}

// ===================================================================================
// Converts a WORLD position into a 3D volume cloud position
float3	World2Volume( float3 _WorldPositionKm )
{
// 	float	DeltaHeight = CloudPlaneHeightTop - CloudPlaneHeightBottom;
// 
// 	// Offset so cloud top is 0
// 	_WorldPositionKm.y = min( DeltaHeight, CloudPlaneHeightTop - _WorldPositionKm.y );
// 
// 	// Scale into [0,1]
// 	_WorldPositionKm *= NoiseSize / DeltaHeight;
// 
// 	// Scroll...
// 	_WorldPositionKm.x += CloudSpeed * CloudTime;

	_WorldPositionKm += CloudSpeed * CloudTime;
	_WorldPositionKm *= NoiseSize;

	return _WorldPositionKm;
}

float2	World2Surface( float3 _WorldPositionKm )
{
	float2	SurfacePosition = _WorldPositionKm.xz;

	SurfacePosition.x += CloudSpeed * CloudTime;
	SurfacePosition *= NoiseSize;

	return SurfacePosition;
}

float	GetMipLevel( float3 _WorldPositionKm, float3 _CameraPosKm, float3 _SurfaceNormal )
{
	float3	ToPosition = _WorldPositionKm - _CameraPosKm;
	float	Distance2Camera = length( ToPosition );
			ToPosition /= Distance2Camera;

	float	PixelSize = (2.0 * Distance2Camera * CameraData.x * BufferInvSize.y) / abs( dot( ToPosition, _SurfaceNormal ) );
	float	TexelSize = 512.0 * NoiseSize;

	return NoiseMipBias + log2( PixelSize / TexelSize );
}

// Given a SHADOW map UV, this computes the WORLD direction vector
float3	ComputeWorldDirectionFromShadowUV( float2 _UV )
{
	// Remap UVs into torus (X,Y) angles
	float2	Angle = ShadowAngularBounds.xy + _UV * ShadowAngularBounds.zw;

	// Compute direction in SHADOW space
	float2	SinCosX = SinCos_DL( Angle.x );
	float2	SinCosY = SinCos_DL( Angle.y );
	float3	Direction = float3( SinCosX.y * SinCosY.y, SinCosY.x, SinCosX.x * SinCosY.y );

// 	float2	SinCosX;
// 	sincos( Angle.x, SinCosX.x, SinCosX.y );
// 	float2	SinCosY;
// 	sincos( Angle.y, SinCosY.x, SinCosY.y );
// 	float3	Direction = float3( SinCosX.y * SinCosY.y, SinCosY.x, SinCosX.x * SinCosY.y );

	// Transform into WORLD space
	return mul( float4( Direction, 0.0 ), Shadow2World ).xyz;
}

// Dynamic 3D version (4 octaves)
float	GetNoise3D( float3 _WorldPositionKm, float _MipLevel )
{
	float3	UVW = World2Volume( _WorldPositionKm );

	float	Value  = NoiseTexture0.SampleLevel( LinearWrap, UVW, _MipLevel ).x;
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.5   * NoiseTexture1.SampleLevel( LinearWrap, UVW, _MipLevel ).x;
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.25  * NoiseTexture2.SampleLevel( LinearWrap, UVW, _MipLevel ).x;
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.125 * NoiseTexture3.SampleLevel( LinearWrap, UVW, _MipLevel ).x;

	return Value;
}

// Dynamic 2D version (4 octaves)
float	GetNoise2D( float3 _WorldPositionKm, float _MipLevel, out float3 _Normal )
{
	float2	UV = World2Surface( _WorldPositionKm );
	float	Amplitude = 1.0;

	float4	Value  = NoiseTexture2D0.SampleLevel( LinearWrap, UV, _MipLevel );
	float	Noise = Value.w;
	_Normal = 2.0 * Value.xyz - 1.0;

	Amplitude *= AmplitudeFactor.x;
	UV *= FrequencyFactor;
	UV.x += CloudEvolutionSpeed * CloudTime;
	Value = NoiseTexture2D1.SampleLevel( LinearWrap, UV, _MipLevel );
	Noise += Amplitude * Value.w;
	_Normal += Amplitude * (2.0 * Value.xyz - 1.0);

	return AmplitudeFactor.y * Noise;

	Amplitude *= AmplitudeFactor.x;
	UV *= FrequencyFactor;
	UV.x += CloudEvolutionSpeed * CloudTime;
	Value = NoiseTexture2D2.SampleLevel( LinearWrap, UV, _MipLevel );
	Noise += Amplitude * Value.w;
	_Normal += Amplitude * (2.0 * Value.xyz - 1.0);

	return AmplitudeFactor.z * Noise;

	Amplitude *= AmplitudeFactor.x;
	UV *= FrequencyFactor;
	UV.x += CloudEvolutionSpeed * CloudTime;
	Value = NoiseTexture2D3.SampleLevel( LinearWrap, UV, _MipLevel );
	Noise += Amplitude * Value.w;
	_Normal += Amplitude * (2.0 * Value.xyz - 1.0);

	return AmplitudeFactor.w * Noise;
}

float	ComputeCloudDensity( float3 _WorldPositionKm, float _MipLevel, out float3 _Normal )
{
	float	N = saturate( GetNoise2D( _WorldPositionKm, _MipLevel, _Normal ) - CloudDensityOffset ) / max( 1e-2, 1.0 - CloudDensityOffset);
//	return sqrt( 1.0 - (1.0 - N)*(1.0 - N) );
	return sqrt( 1.0-pow( 1.0-N, CloudPower ) );
}

float	GetCloudAltitudeBottom( float3 _WorldPositionKm, float _MipLevel, out float3 _Normal )
{
	float	N = saturate( (GetNoise2D( _WorldPositionKm, _MipLevel, _Normal ) - CloudDensityOffset) / max( 1e-2, 1.0 - CloudDensityOffset ) );
	return LayerThickness * N;
}

float	GetCloudAltitudeTop( float3 _WorldPositionKm, float _MipLevel, out float3 _Normal )
{
	float	N = saturate( (1.0 - GetNoise2D( _WorldPositionKm + float3( 195.0, 0.0, 177.12 ), _MipLevel, _Normal ) + CloudDensityOffset) / max( 1e-2, 1.0 - CloudDensityOffset ) );
	return LayerThickness * N;
}

// Computes the thickness of the cloud layer at given position
// We first sample the noise at specified position to obtain the bottom altitude then we sample the noise at an offset to obtain top altitude
//	We then use parallax to read actual top altitude at another position
// Returns :
//	X = Bottom Altitude
//	Y = Thickness
//
float2	ComputeCloudLayerAltitudeThickness_OLD( float3 _WorldPositionKm, float3 _View, float3 _SphereNormal, float _MipLevel )
{
	float3	NormalBottom;
	float	AltitudeBottom = GetCloudAltitudeBottom( _WorldPositionKm, _MipLevel, NormalBottom );
	float3	NormalTop;
	float	AltitudeTop = GetCloudAltitudeTop( _WorldPositionKm, _MipLevel, NormalTop );

	// Offset top position based on predicted top altitude
	float	DotViewNormal = dot( _View, _SphereNormal );
	float3	ViewOffset = _View - _SphereNormal * DotViewNormal;	// Tangent view vector
			ViewOffset /= max( 1.0, abs( DotViewNormal ) );		// Scale by 1/cos(view)
	float3	WorldPositionKmBottom = _WorldPositionKm + ViewOffset * AltitudeBottom;
	float3	WorldPositionKmTop = _WorldPositionKm + ViewOffset * AltitudeTop;

	// Read bottom and top altitude again, with offset this time
	AltitudeBottom = GetCloudAltitudeBottom( WorldPositionKmBottom, _MipLevel, NormalBottom );
	AltitudeTop = GetCloudAltitudeTop( WorldPositionKmTop, _MipLevel, NormalTop );

	return float2( AltitudeBottom, max( 0.0, AltitudeTop - AltitudeBottom ) );
}

float2	ComputeCloudLayerAltitudeThickness( float3 _WorldPositionKm, float3 _View, out float3 _CloudNormal, float _MipLevel )
{
	float3	NormalBottom;
	float	AltitudeBottom = GetCloudAltitudeBottom( _WorldPositionKm, _MipLevel, NormalBottom );
	float3	NormalTop;
	float	AltitudeTop = GetCloudAltitudeTop( _WorldPositionKm, _MipLevel, NormalTop );

	_CloudNormal = normalize( float3( NormalAmplitude.xx, 1.0 ) * NormalBottom );

	return float2( AltitudeBottom, max( 0.0, AltitudeTop - AltitudeBottom ) );
//	return float2( 0.0, LayerThickness * ComputeCloudDensity( _WorldPositionKm, _MipLevel ) );
}

float	GetPhase( float _DotVL )
{
	return PhaseMie.SampleLevel( LinearClamp, 0.5 * (1.0 - _DotVL), 0.0 ).x;
}

float	GetPhaseConvolved( float _DotVL )
{
	return PhaseMie_Convolved.SampleLevel( LinearClamp, 0.5 * (1.0 - _DotVL), 0.0 ).x;
}

float4	ComputeCloudLighting_Transmit( float3 _WorldPositionKm, float3 _CameraPosKm, float3 _View, float3 _SphereNormal, float3 _SphereTangent, float3 _SphereBiTangent, float _MipLevel, float3 _SkyColor )
{
	float3	CloudNormal;
	float2	LayerAltitudeThickness = ComputeCloudLayerAltitudeThickness( _WorldPositionKm, _View, CloudNormal, _MipLevel );
	float	H = 1000.0 * LayerAltitudeThickness.y;	// We need the thickness in meters !

	_SphereNormal = CloudNormal.x * _SphereTangent + CloudNormal.z * _SphereNormal + CloudNormal.y * _SphereBiTangent;

	float	DotV = abs( dot( _View, _SphereNormal ) );
	float	DotL = abs( dot( SunDirection, _SphereNormal ) );
	float	DotVL = dot( SunDirection, _View );
	float	Hv = H / max( 0.01, DotV );
	float	Hl = H / max( 0.01, DotL );

	float	Phase = GetPhase( DotVL );
	float	Phase2 = GetPhaseConvolved( DotVL );

	float	Kappa_ex = ExtinctionCoeff;
	float	Kappa_sc = ScatteringCoeff;
	float	KappaS_sc = 0.5 * ScatteringCoeff;

	// Compute extinction
	float	Extinction = exp( -Kappa_ex * Hv );
	float	Alpha = 1.0 - Extinction;		// Transparency

	// Compute 0-scattering (narrow forward peak)
	float	I0 = ScatteringFactors.x * DotV * exp( -KappaS_sc * Hv ) * pow( 0.5 * Phase, KappaS_sc * Hv );	// Narrow forward scattering
			I0 *= Alpha;
//return I0 * (1.0-Extinction);

	// Compute first-order scattering (analytical)
	float	ExpHL = (exp( -Kappa_sc * Hv ) - exp( -Kappa_sc * Hl )) * DotL / (DotV - DotL);
//return ExpHL * (1.0-Extinction);
	float	I1 = ScatteringFactors.y * Kappa_sc * Phase * ExpHL;
//return I1;// * (1.0-Extinction);

	// Compute second-order scattering (analytical)
	float	I2 = ScatteringFactors.z * KappaS_sc * KappaS_sc * Phase2 * ExpHL;
//return I2 * (1.0-Extinction);
//return I0 + I1 + I2;

	// Compute multiple scattering (diffuse approximation)
	const float		Constants[] =
	{
		// b       c    Kappa_c    t      r
//		1.1796, 0.0138, 0.0265, 0.8389, 0.0547,	// 0°
// 		1.1293, 0.0154, 0.0262, 0.8412, 0.0547	// 10°
// 		1.1382, 0.0131, 0.0272, 0.8334, 0.0552,	// 20°
// 		1.0953, 0.0049, 0.0294, 0.8208, 0.0564,	// 30°
 		0.9808, 0.0012, 0.0326, 0.8010, 0.0603,	// 40°
// 		0.9077, 0.0047, 0.0379, 0.7774, 0.0705,	// 50°
// 		0.7987, 0.0207, 0.0471, 0.7506, 0.0984,	// 60°
// 		0.6629, 0.0133, 0.0616, 0.7165, 0.1700,	// 70°
// 		0.5043, 0.0280, 0.0700, 0.7149, 0.3554,	// 80°
//		0.3021, 0.0783, 0.0700, 0.1000, 0.9500	// 90°
	};

	float	Beta = 0.9961;

	float	b = Constants[0];
	float	c = Constants[1] * ScatteringCoeff / 0.01;			// We must use our scattering coeff projected to a nominal c here...
	float	Kappa_c = Constants[2] * ScatteringCoeff / 0.03;	// We must use our scattering coeff projected to a nominal Kappa_c here...
	float	t = Constants[3];
	float	r = Constants[4];

	float	tHKc = t * H * Kappa_c;

	float	Tau_c = exp( -Kappa_c * H );
	float	Tau_c2 = Tau_c * Tau_c;
	float	T0 = ScatteringFactors.x * exp( -Kappa_ex * Hl ) * Alpha;
	float	T1 = ScatteringFactors.y * tHKc * Tau_c;
	float	T2a = 0.5 * tHKc * tHKc * Tau_c;
	float	T2b = 0.25 * r*r * (1.0 + (2.0 * H * Kappa_c - 1.0) * Tau_c2) * Tau_c2*Tau_c;
	float	T2 = ScatteringFactors.z * (T2a + T2b);
//	float	Tms = (b + (1.0 - b) * exp( -c * H )) * Beta / (H - Beta * (H-1.0));
	float	Tms = DotV * H * Kappa_c * Tau_c * Beta / (H - Beta * (H-1.0));
//	float	T3 = saturate( Tms - 0*T2 - T1 - T0);
	float	T3 = saturate( Tms - 0*(I2 + T1 + T0) );

	float	I3 = ScatteringFactors.w * T3 * 0.07957747154594766788444188168626 * DotL / DotV;	// DotL/(4PI DotV)

	return float4( SunColor * (I0+I1+I2+I3) + SkyColor * ScatteringSkyFactor * Tms, Extinction );
}


// ===================================================================================

float	ShadowAltitudeMin;
float	ShadowAltitudeMax;

float	ShadowGetThicknessAtAltitude( float _Altitude, float2 _LayerAltitudeThickness )
{
	float	NormalizedAltitude = saturate( (_LayerAltitudeThickness.x - _LayerAltitudeThickness.y - _Altitude) / _LayerAltitudeThickness.y );
	return NormalizedAltitude * _LayerAltitudeThickness.y;
}

float4	PS_Shadow( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	Direction = ComputeWorldDirectionFromShadowUV( UV );

	// Compute position on cloud layer in WORLD space
	float3	CloudPositionKm = EARTH_CENTER + CLOUD_RADIUS * Direction;
	float3	CloudNormal = normalize( CloudPositionKm );

//	return float4( 1, 0, 0, 0 );
//	return float4( Direction, 0 );
//	return float4( UV, 0, 0 );
//	return float4( CloudPosition, 0 );

	// Compute layer thickness at position
	float2	LayerAltitudeThickness = ComputeCloudLayerAltitudeThickness( CloudPositionKm, CloudNormal, CloudNormal, NoiseMipBias );
	float	Thickness = 1000.0 * LayerAltitudeThickness.y;	// WE need the thickness in meters !

	// Compute extinction through layer at 4 distinct altitudes
	float	DeltaAltitude = ShadowAltitudeMax - ShadowAltitudeMin;
	return float4(
		exp( -ExtinctionCoeff * ShadowGetThicknessAtAltitude( ShadowAltitudeMax - 0.25 * DeltaAltitude, LayerAltitudeThickness ) ),
		exp( -ExtinctionCoeff * ShadowGetThicknessAtAltitude( ShadowAltitudeMax - 0.5 * DeltaAltitude, LayerAltitudeThickness ) ),
		exp( -ExtinctionCoeff * ShadowGetThicknessAtAltitude( ShadowAltitudeMax - 0.75 * DeltaAltitude, LayerAltitudeThickness ) ),
		exp( -ExtinctionCoeff * ShadowGetThicknessAtAltitude( ShadowAltitudeMin, LayerAltitudeThickness ) ) );
}


// ===================================================================================

float4	PS_Cloud( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	View = mul( float4( GetCameraView( UV ), 0 ), Camera2World ).xyz;
	float3	Pos = Camera2World[3].xyz;
//return ShadowMap.SampleLevel( LinearWrap, UV, 0.0 );

	float4	SourceColor = SourceBuffer.SampleLevel( LinearClamp, UV, 0.0 );

	float3	CameraPosKm = WorldUnit2Kilometer * Pos + float3( 0.0, EARTH_RADIUS, 0.0 );		// Offset to Earth's surface
	float	CloudHitDistanceKm = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, CLOUD_RADIUS );
	if ( CloudHitDistanceKm < 0.0 )
		return float4( 1, 0, 1, 0 );

	float	EarthHitDistance = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS );
	if ( EarthHitDistance >= 0.0 )
//		return float4( 0, 0.2, 0, 0 );	// Hitting the ground...
//		return float4( saturate( SunDirection.y ) * SunColor * float3( 0, 0.1, 0 ), 1.0 );	// Hitting the ground...
		return SourceColor;

	// Compute lighting
 	float3	HitPositionCloudKm = CameraPosKm + CloudHitDistanceKm * View;
	float3	SphereNormal = normalize( HitPositionCloudKm - EARTH_CENTER );
	float3	SphereTangent = float3( 1, 0, 0 );
	float3	SphereBiTangent = normalize( cross( SphereTangent, SphereNormal ) );
			SphereTangent = cross( SphereNormal, SphereBiTangent );

// 	if ( dot( SunDirection, SphereNormal ) > 0.0 )
// 		return float4( 10, 0, 0, 0 );
// 	else
// 		return float4( 0, 10, 0, 0 );

	float4	CloudScatteringExtinction = ComputeCloudLighting_Transmit( HitPositionCloudKm, CameraPosKm, View, SphereNormal, SphereTangent, SphereBiTangent, GetMipLevel( HitPositionCloudKm, CameraPosKm, SphereNormal ), SourceColor.xyz );

//	return CloudScatteringExtinction;
//	return float4( CloudScatteringExtinction.xyz, 0.0 );
	SourceColor *= CloudScatteringExtinction.w;

	return float4( SourceColor.xyz + CloudScatteringExtinction.xyz, SourceColor.w );

//	// Display cloud density from direct computation or from sampling the shadow map
// 	float3	HitPositionCloudKm = CameraPosKm + CloudHitDistanceKm * View;
// 	float2	ShadowUV = ProjectWorld2Shadow( HitPositionCloudKm );
// 
// 	if ( abs(ShadowUV.x) > 1.0 || abs(ShadowUV.y) > 1.0 )
// 		return float4( 1, 1, 0, 0 );
// 
// 	float	ShadowMapDensity = ShadowMap.SampleLevel( LinearWrap, ShadowUV, 0.0 ).x;
// // 	return ShadowMapDensity;
// 	float	AccurateDensity = ComputeCloudDensity( HitPositionCloudKm, GetMipLevel( HitPositionCloudKm, CameraPosKm, SphereNormal ) );
//  	return AccurateDensity;
// 	return 10.0 * abs( AccurateDensity - ShadowMapDensity );	// This shows the sampling from the shadow map is pretty accurate !
// 
//  	float3	CloudPosition = ShadowMap.SampleLevel( LinearWrap, ShadowUV, 0.0 ).xyz;
//  	float	Distance2Cloud = length( CloudPosition - CameraPosKm );
// 	return 0.01 * abs( Distance2Cloud - CloudHitDistanceKm );
//  	return 0.01 * Distance2Cloud;


// 	// Display ground plane
// 	float	HitDistance = -Pos.y / View.y;
// 	if ( HitDistance < 0.0 )
// 		return float4( 0.0, 0.0, 0.0, 1.0 );
// 
// 	float3	GroundPos = Pos + HitDistance * View;
// 			GroundPos *= WorldUnit2Kilometer;					// Transform into kilometers
// //return float4( 0.1 * GroundPos, 1 );
// 			GroundPos += float3( 0.0, EARTH_RADIUS, 0.0 );		// Offset to Earth's surface
// 
// 	// Cast a ray in Sun's direction to hit the cloud sphere
// 	HitDistance = ComputeSphereIntersection( GroundPos, SunDirection, EARTH_CENTER, CLOUD_RADIUS );
// 	if ( HitDistance < 0.0 )
// 		return float4( 1, 0, 1, 0 );	// !!!
// 
// 	return float4( 0.001 * HitDistance, 0, 0, 1 );
}

// ===================================================================================
//
technique10 DrawShadow
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Shadow() ) );
	}
}

technique10 DrawCloud
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Cloud() ) );
	}
}
