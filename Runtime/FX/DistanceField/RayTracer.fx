// This is a post-process that performs ray-casting into a distance field
// (source: http://www.iquilezles.org/www/material/nvscene2008/rwwtt.pdf)
//
// NOTE: I don't know if everybody does this but I found a nice improvement for the distance field
//	method since I don't provide the "ComputeDistance()" method with only the position but also
//	the view vector.
// This way, I can easily discard the objects that have no chance of being hit by the view ray,
//	like objects standing behind the view point or objects whose solid angle is too small to be
//	intercepted by the view ray.
// This little improvement saves a HUUUUGE amount of rays and even if it adds a few more instructions
//	per object, the operations done to check for solid angles interception are often also the
//	same ones done to compute the distance field for an object, so it's often a matter of better
//	exploiting these operations.
//
// Another tip thanks to Nystep is to change the epsilon based on distance to camera.
//
#include "../Camera.fx"
#include "ComputeDistanceField.fx"

static const float	INFINITY = 1e6;
static const float	INFINITY_TEST = 1e5;
static const int	MAX_STEPS_COUNT = 32;
static const float	MIN_STEP_SIZE = 1e-2;
static const float2	NORMAL_EPSILON = float2( 1e-5, 0.0 );

float3		AspectRatio = float3( 1.0, 1.0, 1.0 );
float		StartEpsilon = 0.01;
Texture2D	SpectrumTexture;

struct VS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_Position;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
	float	StartDistance: TEXCOORD1;
};

// Returns either the distance to the object or "infinity" if there's no chance to hit the object (i.e. the ray is looking away)
float	SolidAngle( float3 _Position, float3 _View, float3 _ObjectCenter, float _ObjectRadius, uniform bool _bUseView )
{
	float3	ToObject = _ObjectCenter - _Position;
	float	Distance2Object = length( ToObject );
	if ( !_bUseView )
		return Distance2Object;

	ToObject /= Distance2Object;

	float	RoverDist = _ObjectRadius / Distance2Object;
	return  dot( ToObject, _View ) > sqrt( 1.0 - RoverDist*RoverDist ) ? Distance2Object : INFINITY;
}

float	Distance2Plane( float3 _Position, float3 _View, uniform float3 _Center, uniform float3 _Normal, uniform bool _bUseView )
{
	float3	ToPosition = _Position - _Center;
	float	Distance2Plane = dot( ToPosition, _Normal );
	float	ViewDirection = _bUseView ? dot( _View, _Normal ) : -1.0;
	return abs(Distance2Plane) * ViewDirection < 0.0 ? Distance2Plane : INFINITY;
}

float	Distance2Sphere( float3 _Position, float3 _View, uniform float3 _Center, uniform float _Radius, uniform bool _bUseView )
{
	return max( 0.0, SolidAngle( _Position, _View, _Center, _Radius, _bUseView ) - _Radius );
}

float	Distance2SphereNoise( float3 _Position, float3 _View, uniform float3 _Center, uniform float _Radius, uniform bool _bUseView )
{
	float	Distance2Sphere = SolidAngle( _Position, _View, _Center, _Radius + 0.3, _bUseView );
	if ( Distance2Sphere > INFINITY_TEST )
		return Distance2Sphere;

	float	Noise  = 0.2 * NHQu( 0.25 * _Position, NoiseTexture0 );
			Noise += 0.1 * NHQu( 0.5 * _Position, NoiseTexture1 );
			Noise += 0.05 * NHQu( 1.0 * _Position, NoiseTexture2 );
//			Noise = 0.0;

	return max( 0.0, Distance2Sphere - (_Radius + Noise) );
}

float	Distance2Cylinder( float3 _Position, float3 _View, uniform float3 _Center, uniform float _Radius, uniform float _HalfHeight, uniform bool _bUseView )
{
	float2	ToPosition2D = _Center.xz - _Position.xz;
	float	Distance2Object = length( ToPosition2D );
	float	Distance = Distance2Object - _Radius;

	float3	ToPosition = _Position - _Center;
			ToPosition.y /= _HalfHeight;
	float	DistanceToCap = length( ToPosition ) * (1.0 - 1.0 / abs( ToPosition.y ));
	Distance = max( Distance, DistanceToCap );
	if ( !_bUseView )
		return Distance;

	ToPosition2D /= Distance2Object;

	float	RoverDist = _Radius / Distance2Object;
	float2	View2D = normalize( _View.xz );
	return  abs( _View.y ) < 1e-3 || dot( ToPosition2D, View2D ) > sqrt( 1.0 - RoverDist*RoverDist ) ? Distance : INFINITY;
}

float	Distance2Box( float3 _Position, float3 _View, uniform float3 _Center, uniform float3 _InvHalfSize, uniform bool _bUseView )
{
	float3	D = abs( _Position - _Center) * _InvHalfSize;
	return max( D.x, max( D.y, D.z ) ) - 1.0;
}

// Computes the distance field at the given world position
//
float	ComputeDistanceField( float3 _Position, uniform float3 _View, uniform bool _bUseView )
{
	float	Distance = INFINITY;
	Distance = min( Distance, Distance2Plane( _Position, _View, float3( 0.0, 0.0, 0.0 ), float3( 0.0, 1.0, 0.0 ), _bUseView ) );
//	Distance = min( Distance, Distance2Sphere( abs( _Position % float3( 4.0, 1000.0, 4.0 ) ), _View, float3( 1.0, 1.0, 1.0 ), 1.0, false ) );
//	Distance = min( Distance, Distance2Sphere( _Position, _View, float3( 0.0, 1.0, 0.0 ), 1.0, _bUseView ) );
//	Distance = min( Distance, Distance2SphereNoise( _Position, _View, float3( 0.0, 1.0, 0.0 ), 1.0, _bUseView ) );
//	Distance = min( Distance, Distance2Box( _Position, _View, float3( 0.0, 0.0, 0.0 ), float3( 0.01, 1.0, 0.01 ), false ));//_bUseView ) );
	Distance = min( Distance, Distance2Box( _Position, _View, float3( 0.0, 1.0, 0.0 ), float3( 0.25, 1.0, 0.5 ), false ));//_bUseView ) );
//	Distance = min( Distance, Distance2Cylinder( _Position, _View, float3( 0.0, 2.0, 0.0 ), 1.0, 2.0, _bUseView ) );
//	Distance = min( Distance0, Distance1 );
//	Distance = lerp( Distance0, Distance1, smoothstep( 0.0, 2.0, Distance1 ) );
//	Distance = 0.5 * (Distance0 + Distance1);

	return Distance;
}

// Compute the normal using the distance gradient
//
float3	ComputeNormal( float3 _Position, float _Epsilon )
{
	float2	Epsilon = float2( _Epsilon, 0.0 );
	return normalize( float3( 
		ComputeDistanceField( _Position + Epsilon.xyy, 0.0.xxx, false ) - ComputeDistanceField( _Position - Epsilon.xyy, 0.0.xxx, false ),
		ComputeDistanceField( _Position + Epsilon.yxy, 0.0.xxx, false ) - ComputeDistanceField( _Position - Epsilon.yxy, 0.0.xxx, false ),
		ComputeDistanceField( _Position + Epsilon.yyx, 0.0.xxx, false ) - ComputeDistanceField( _Position - Epsilon.yyx, 0.0.xxx, false )
		) );
}

// Computes AO by sampling distance along the normal direction
//
float	ComputeAO( float3 _Position, float3 _Normal )
{
	static const float	AO_START = 0.01;
	static const float	AO_STEP_REACH = 0.015;
	static const int	AO_STEPS_COUNT = 5;

	float	AO = 0.0;
	float	Factor = 10.0;

	for ( int StepIndex=0; StepIndex < AO_STEPS_COUNT; StepIndex++ )
	{
		float	Offset = 0.01 + 0.015 * StepIndex*StepIndex;
		float3	OffPos = _Position + Offset * _Normal;

		// Accumulate AO
		AO += Factor * (Offset - ComputeDistanceField( OffPos, 0.0.xxx, false ));
		Factor *= 0.5;
	}

	return saturate( 1.0 - AO );
}

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
	Out.View = mul( float4( _In.View * AspectRatio, 0.0 ), Camera2World ).xyz;
	Out.UV = _In.UV;
//	Out.StartDistance = ComputeDistanceField( Camera2World[3].xyz );
	Out.StartDistance = 0.0;
	
	return	Out;
}

float4	GetFalseColorsSpectrum( int _StepsCount )
{
	return float4( SpectrumTexture.SampleLevel( NearestClamp, (float) _StepsCount / MAX_STEPS_COUNT, 0 ).xyz, 1.0 );
}

float4 PS( PS_IN _In ) : SV_Target
{
	float3	Position = Camera2World[3].xyz;
	float3	View = normalize( _In.View );

	static const float	EpsilonFactor = 10.0 * StartEpsilon;

	float	PreviousDistance = 0.0;
	float	Distance = 0.1;
	float	MarchStep = 0.0;
	float	MarchedDistance = 0.0;
	float	Epsilon = 0.0;
	for ( int StepIndex=0; StepIndex <= MAX_STEPS_COUNT; StepIndex++ )
	{
		// March
		MarchStep = max( Epsilon, max( MIN_STEP_SIZE, Distance ) );
//		MarchStep = Distance * max( 1.0, 0.1 * MarchedDistance );
//		MarchStep = max( MIN_STEP_SIZE, 1.0 * Distance * max( 1.0, 0.125 * MarchedDistance ) );
		Position += MarchStep * View;
		MarchedDistance += MarchStep;
		Epsilon = EpsilonFactor * MarchedDistance;

		// Estimate distance
		PreviousDistance = Distance;
		Distance = ComputeDistanceField( Position, View, false );
		if ( Distance < Epsilon )
			break;	// Hit !
		else if ( Distance > INFINITY_TEST )
//			return GetFalseColorsSpectrum( StepIndex );
			return float4( 1.0, 0.0, 1.0, 1.0 );
//			return 0.5 * (float4( 1.0, 0.0, 1.0, 1.0 ) + GetFalseColorsSpectrum( StepIndex ));
//			break;	// No possible hit...
	}

	if ( Distance > 10.0 * Epsilon )
//		return float4( Epsilon.xxx, 1.0 );
		return float4( 0.0, 0.0, 0.0, 1.0 );	// NO FUCKING HIT AFTER MAX STEPS !

	// We have a hit so display a nice thing
	// March backward to exact hit
	float	ToHitFactor = Distance / (PreviousDistance-Distance);	// This is the factor that should bring us back to the intersection point
	Position += ToHitFactor * MarchStep * View;

 	float3	Normal = ComputeNormal( Position, Epsilon );
 	float	AO = ComputeAO( Position, Normal );
	return GetFalseColorsSpectrum( StepIndex );
//	return float4( AO.xxx, 1.0 );
//	return float4( Normal, 1.0 );
	return float4( 0.5 * Position, 1.0 );
}

technique10 RayTracer
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
