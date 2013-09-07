// This shader computes wind for a given world position
//
float			Time = 0.0;
float			GustTime = 0.0;
float3			WindDirection = float3( 1.0, 0.0, 0.0 );
float			WindForce = 1.0;
Texture2D		MotionTexture;


// Computes displacement vector for grass due to wind
//	_GrassPosition, the world position of a grass tuft
// returns the displacement vector to add to the grass's upper vertices
//
float3	ComputeGrassWindDisplacement( float3 _GrassPosition )
{
	// Handle wind gust
	float	GustPosition = WindForce * GustTime - 10.0f;	// Gust position along wind direction vector
	float	GrassPositionAlongGustAxis = dot( _GrassPosition, WindDirection );
	float3	GrassPositionOnAxis = WindDirection * GrassPositionAlongGustAxis;
	float	GrassDistanceToGustAxis = length( _GrassPosition - GrassPositionOnAxis );				// Grass X value in the XY gust plane
			GrassPositionAlongGustAxis = GustPosition - GrassPositionAlongGustAxis;					// Grass Y value in the XY gust plane

	float	XFactor = 0.25;
	float	GustFrontY = (XFactor * GrassDistanceToGustAxis * XFactor * GrassDistanceToGustAxis);	// Y value of the gust front in the XY gust plane
	float	DY = GustFrontY - GrassPositionAlongGustAxis;
	if ( DY < 0.0 )
		DY = 0.25 * DY;	// Get up slowly
	else
		DY = 2.5 * DY;	// Bend quickly
	float	Distance2Gust = 2.0 * exp( -DY*DY );		// Relative distance between the grass and the gust front

	// Increase wind force when closer to wind gust
	float	ActualWindForce = WindForce + 0.5 * Distance2Gust;


	// Handle standard wind based on stochastic motion texture
	float	MotionVelocity = min( 0.5, 0.125 * ActualWindForce );					// Velocity at which we should scroll in the motion texture
	float	MotionDisplacementFactor = min( 0.08, 0.04 * sqrt( ActualWindForce ) );	// Factor to apply to normalized motion texture values

	float2	UV	= 0.2 * _GrassPosition.xz											// So the motions are coherent from one instance to the other
				+ MotionVelocity * Time * float2( 0.053, 0.137 );					// So we move coherently through time

	static const float	MotionMax = 38.0;	// we read that from the texture itself
	float	Motion = 0.5 * (1.0 + MotionTexture.SampleLevel( LinearWrap, UV, 0 ).x / MotionMax);

	// Bend with gust
//	Motion = max( Motion, Distance2Gust );	// Mix the motion with gust front
	Motion += Distance2Gust;	// Mix the motion with gust front

	return MotionDisplacementFactor * Motion * WindDirection;
}
