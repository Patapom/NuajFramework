// Use this to obtain shadow map support
//
static const float	HALF_PI = 1.5707963267948966192313216916398;
static const float3	EARTH_CENTER = 0.0;

// Shadow parameters
float4		ShadowAngularBounds		: SHADOW_ANGULAR_BOUNDS;	// X=AngleMinX Y=AngleMinY Z=DeltaAngleX W=DeltaAngleY
float4		ShadowInvAngularBounds	: SHADOW_INVANGULAR_BOUNDS;	// X=-AngleMinX Y=-AngleMinY Z=1/DeltaAngleX W=1/DeltaAngleY
float4x4	Shadow2World			: SHADOW2WORLD;
float4x4	World2Shadow			: WORLD2SHADOW;
float4		ShadowAltitudesMinKm	: SHADOW_ALTITUDES_MIN;		// RGBA contain each the MIN altitudes for each of the 4 possible cloud layers (from top to bottom)
float4		ShadowAltitudesMaxKm	: SHADOW_ALTITUDES_MAX;		// RGBA contain each the MAX altitudes for each of the 4 possible cloud layers (from top to bottom)
Texture2D	ShadowMap				: SHADOW_MAP;				// RGBA contain each an extinction value for each of the 4 possible cloud layers (from top to bottom)


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

// ===================================================================================
// Shadow Map Computation

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

// Projects a WORLD position into the shadow map space
//	_WorldPositionKm, the world position where to sample the shadow map (this position is in kilometers and is in Earth coordinates, meaning that if you're standing on the ground then your position is (0,EARTH_RADIUS,0))
//	_SphereRadiusKm, the radius of the sphere to which to project the position to (including the Earth radius)
//	_SunDirection, the direction pointing toward the Sun
// Returns a UV to sample the shadow map
float2	ProjectWorld2Shadow( float3 _WorldPositionKm, float _SphereRadiusKm, float3 _SunDirection )
{
// This is wrong ! We must not project to sphere radially but in the direction of the Sun !
// 	float3	ShadowSpherePosition = _WorldPositionKm - EARTH_CENTER;
// 			ShadowSpherePosition = mul( float4( ShadowSpherePosition, 0.0 ), World2Shadow ).xyz;	// Position in SHADOW space
// 			ShadowSpherePosition = normalize( ShadowSpherePosition );

	float	HitDistance = ComputeSphereIntersection( _WorldPositionKm, _SunDirection, EARTH_CENTER, _SphereRadiusKm );
 	float3	ShadowSpherePosition = _WorldPositionKm + HitDistance * _SunDirection - EARTH_CENTER;
 			ShadowSpherePosition = mul( float4( ShadowSpherePosition, 0.0 ), World2Shadow ).xyz;	// Position in SHADOW space
 			ShadowSpherePosition = normalize( ShadowSpherePosition );

	// Compute angular deviations
	float2	Angles = float2( Atan_DL( ShadowSpherePosition.z / ShadowSpherePosition.x ), Asin_DL( ShadowSpherePosition.y ) );

	// Normalize deviations
	Angles = (Angles + ShadowInvAngularBounds.xy) * ShadowInvAngularBounds.zw;

	return Angles;
}

// Computes the extinction due to shadowing at given altitude
float	GetShadowAtAltitude( float2 _ShadowUV, float _AltitudeKm )
{
	if ( _AltitudeKm >= ShadowAltitudesMaxKm.x )
		return 1.0;	// Above highest cloud layer : no shadow !

	float4	Shadow = ShadowMap.SampleLevel( LinearClamp, _ShadowUV, 0.0 );
return Shadow.x;

	float	Weight0 = smoothstep( ShadowAltitudesMaxKm.x, ShadowAltitudesMinKm.x, _AltitudeKm );
	float	Weight1 = smoothstep( ShadowAltitudesMaxKm.y, ShadowAltitudesMinKm.y, _AltitudeKm );
	float	Weight2 = smoothstep( ShadowAltitudesMaxKm.z, ShadowAltitudesMinKm.z, _AltitudeKm );
	float	Weight3 = smoothstep( ShadowAltitudesMaxKm.w, ShadowAltitudesMinKm.w, _AltitudeKm );

	return lerp( 1.0, Shadow.x, Weight0 )
		*  lerp( 1.0, Shadow.y, Weight1 )
		*  lerp( 1.0, Shadow.z, Weight2 )
		*  lerp( 1.0, Shadow.w, Weight3 );
}

// Computes the extinction due to shadowing at given position
//	_WorldPositionKm, the world position where to sample the shadow map (this position is in kilometers and is in Earth coordinates, meaning that if you're standing on the ground then your position is (0,EARTH_RADIUS,0))
//	_SphereRadiusKm, the radius of the sphere to which to project the position to (including the Earth radius)
//	_SunDirection, the direction pointing toward the Sun
float	GetShadowAtPosition( float3 _WorldPositionKm, float _SphereRadiusKm, float3 _SunDirection )
{
	// Compute altitude (relative to sea level)
	float	Altitude = length( _WorldPositionKm - EARTH_CENTER ) - EARTH_RADIUS;
	if ( Altitude >= ShadowAltitudesMaxKm.x )
		return 1.0;	// Above highest cloud layer : no shadow !

	// Compute shadow UV from position
	float2	UV = ProjectWorld2Shadow( _WorldPositionKm, _SphereRadiusKm, _SunDirection );

	return GetShadowAtAltitude( UV, Altitude );
}
