// Some constants
static const int	MAX_STEPS_COUNT = 64;
static const float	MIN_STEP_SIZE = 1e-2;
static const float2	NORMAL_EPSILON = float2( 1e-5, 0.0 );
static const float	HIT_EPSILON = 1e-3;
static const float	INFINITY = 1e6;

// This describes the bounding-boxes present in the scene (static array for now)
static const int	BBOXES_COUNT = 1;
static float3	BBoxes[BBOXES_COUNT][2] = 
{
	{ float3( 0.0, 0.0, 0.0 ), float3( 2.0,2.0,2.0 ) / 4.0 },
};


float3	RotateY( float3 _Position, float3 _Axis )
{
//	return _Position.x * _Axis + _Position.y * float3( 0.0, 1.0, 0.0 ) + _Position.z * float3( -_Axis.z, 0.0, _Axis.x );
	return float3(
			_Position.x * _Axis.x + _Position.z * -_Axis.z,
			_Position.y,
			_Position.x * _Axis.z + _Position.z * _Axis.x
		);
}

// Computes the distance to a box (the scene is composed of bounding-boxes)
//
float	Distance2Box( float3 _Position, uniform float3 _Center, uniform float3 _InvHalfSize )
{
	float3	D = abs( _Position - _Center) * _InvHalfSize;
	return max( D.x, max( D.y, D.z ) ) - 1.0;
}

// Computes the distance to the scene
//
float	GetDistance_OLD( float3 _Position )
{
	float	Distance = INFINITY;
	for ( int BBoxIndex=0; BBoxIndex < BBOXES_COUNT; BBoxIndex++ )
	{
		float3	BBoxCenter = BBoxes[BBoxIndex][0];
		float3	BBoxInvHalfSize = BBoxes[BBoxIndex][1];
		float	BBoxDistance = Distance2Box( _Position, BBoxCenter, BBoxInvHalfSize );

BBoxDistance = -BBoxDistance;	// Since the only box we have is REVERSED (i.e. we're inside the box)

		Distance = min( Distance, BBoxDistance );
	}

	return Distance;
}

static const float3	Rot0 = float3( 0.86602540378443864676372317075294, 0.0, 0.5 );
float	GetDistance( float3 _Position )
{
	// Room
	float	Distance = -Distance2Box( _Position, float3( 0.0, 0.0, 0.0 ), float3( 2.0,2.0,2.0 ) / 4.0 );

	// Tall thin box
	Distance = min( Distance, Distance2Box( RotateY( _Position, Rot0 ), float3( -0.7, -1.0, -0.8 ), float3( 2.0,1.0,2.0 ) ) );

	// Small box
	Distance = min( Distance, Distance2Box( _Position, float3( +0.9, -1.5, -0.8 ), float3( 2.0,2.0,2.0 ) ) );

	return Distance;
}

// Computes the surface normal and albedo at given position
//
void	GetNormalAlbedo( float3 _Position, out float3 _Normal, out float3 _Albedo )
{
	// Compute normal
	_Normal = normalize( float3(
		GetDistance( _Position + NORMAL_EPSILON.xyy ) - GetDistance( _Position - NORMAL_EPSILON.xyy ),
		GetDistance( _Position + NORMAL_EPSILON.yxy ) - GetDistance( _Position - NORMAL_EPSILON.yxy ),
		GetDistance( _Position + NORMAL_EPSILON.yyx ) - GetDistance( _Position - NORMAL_EPSILON.yyx )
		) );

	// TODO: Isolate the closest bbox and, depending on normal, choose appropriate bbox side's color
	const float	WALL_EPS = 1e-2;

	float	Max = max( max( abs(_Position.x), abs(_Position.y) ), abs(_Position.z) );
	if ( Max > 1.98 )
	{	// Room box
		float3	Albedos[] = {
			float3( 1,0,0 ), float3( 0,1,0 ),	// Red on left, green on right
			float3( 1,1,1 ), float3( 1,1,1 ),
			float3( 1,1,1 ), float3( 1,1,1 ),
		};

		float3	AbsNormal = abs(_Normal);
		if ( AbsNormal.x > AbsNormal.y )
		{
			if ( AbsNormal.x > AbsNormal.z )
				_Albedo = _Normal.x >= 0.0 ? Albedos[1] : Albedos[0];
			else
				_Albedo = _Normal.z >= 0.0 ? Albedos[5] : Albedos[4];
		}
		else
		{
			if ( AbsNormal.y > AbsNormal.z )
				_Albedo = _Normal.y >= 0.0 ? Albedos[3] : Albedos[2];
			else
				_Albedo = _Normal.z >= 0.0 ? Albedos[5] : Albedos[4];
		}

//		_Albedo = float3( 0, 0, 1 );
	}
	else
		_Albedo = 1.0;	// Other boxes are white
//		_Albedo = float3( 1, 0, 0 );

	_Albedo *= 0.4;	// Global multiplier
}

// Performs ray-marching using the distance field
//
bool	ComputeIntersection( float3 _Position, float3 _Direction, out float3 _HitPosition, out float3 _HitNormal, out float3 _HitAlbedo )
{
	for ( int StepIndex=0; StepIndex < MAX_STEPS_COUNT; StepIndex++ )
	{
		float	Distance = GetDistance( _Position );
		if ( Distance < HIT_EPSILON )
		{	// We've got a hit !
			_HitPosition = _Position;
			GetNormalAlbedo( _Position, _HitNormal, _HitAlbedo );
			return true;
		}

		// March...
		_Position += max( Distance, MIN_STEP_SIZE ) * _Direction;
	}

// DEBUG
// _HitPosition = _Position;
// GetNormalAlbedo( _Position, _HitNormal, _HitAlbedo );
// return true;
// DEBUG

	_HitPosition = INFINITY;
	_HitNormal = 0.0;
	_HitAlbedo = 0.0;
	return false;
}