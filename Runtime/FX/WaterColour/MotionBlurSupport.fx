// Holds the method to compute the velocity of a WORLD position given the relative motion and rotation of the vertex's matrix
//
float3		DeltaPosition : MOTION_DELTA_POSITION;	// Contains the WORLD previous->current matrix translation
float4		DeltaRotation : MOTION_DELTA_ROTATION;	// Contains the WORLD previous->current matrix rotation (in the form of a quaternion)
float3		DeltaPivot : MOTION_DELTA_PIVOT;		// Contains the WORLD pivot point about which the object rotated

// Converts a quaternion to a matrix (from http://en.wikipedia.org/wiki/Rotation_matrix#Rotation_matrix_from_axis_and_angle)
//
float3x3	Quat2Mat( float4 _Quat )
{
	float	wx, wy, wz, xx, xy, xz, yy, yz, zz;

	float	xs = 2.0 * _Quat.x;
	float	ys = 2.0 * _Quat.y;
	float	zs = 2.0 * _Quat.z;

	wx = _Quat.w * xs;	wy = _Quat.w * ys;	wz = _Quat.w * zs;
	xx = _Quat.x * xs;	xy = _Quat.x * ys;	xz = _Quat.x * zs;
	yy = _Quat.y * ys;	yz = _Quat.y * zs;	zz = _Quat.z * zs;

	float3x3	Rot;
	Rot[0] = float3( 1.0 - yy - zz, xy - wz, xz + wy );
	Rot[1] = float3( xy + wz, 1.0 - xx - zz, yz - wx );
	Rot[2] = float3( xz - wy, yz + wx, 1.0 - xx - yy );

	return Rot;
}

float4	QuatProduct( float4 _Q0, float4 _Q1 )
{
	return float4( (_Q0.w * _Q1.xyz) + (_Q0.xyz * _Q1.w) + cross( _Q0.xyz, _Q1.xyz ), (_Q0.w * _Q1.w) - dot( _Q0.xyz, _Q1.xyz ) );
}

// Rotates a point using a quaternion
// Maths are simple :
// 1) We form a quaternion Q = {Px,Py,Pz,0} from the point to rotate 
// 2) We rotate Q using our rotation quaternion R by writing Q' = R P R* (R* being the conjugate of R)
//
float3	RotateAbout( float3 _Point, float3 _Pivot, float4 _R )
{
	float4	Q = float4( _Point - _Pivot, 0.0 );
	float4	Rc = float4( -_R.xyz, _R.w );
	return _Pivot + QuatProduct( QuatProduct( _R, Q ), Rc ).xyz;
}

// Computes the velocity of a WORLD position given the delta position/rotation sustained by this point's LOCAL=>WORLD matrix
//	_WorldPosition, the position of a point in WORLD space
//	_ObjectDeltaPosition, the delta position of the object (in WORLD space)
//	_ObjectDeltaRotation, the delta rotation of the object (in WORLD space)
//	_ObjectDeltaPivot, the rotation pivot of the object matrix (in WORLD space)
//
float3	ComputeVelocity( float3 _WorldPosition, float3 _ObjectDeltaPosition, float4 _ObjectDeltaRotation, float3 _ObjectDeltaPivot )
{
	float3	Velocity = _ObjectDeltaPosition;	// Really simple for translation : just add !

	// Compute the delta-rotation matrix from the delta-rotation quaternion
	float3	RotatedPosition = RotateAbout( _WorldPosition, _ObjectDeltaPivot, _ObjectDeltaRotation );

	// Angular velocity is simply the difference between current and rotated position
	Velocity += RotatedPosition - _WorldPosition;

	return Velocity;
}

// Same but uses variables declared at the top
float3	ComputeVelocity( float3 _WorldPosition )
{
	return ComputeVelocity( _WorldPosition, DeltaPosition, DeltaRotation, DeltaPivot );
}
