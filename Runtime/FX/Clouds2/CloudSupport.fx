// This shader interface provides support for the cloud's shadow map and lightning spots
//
#if !defined(CLOUD_SUPPORT_FX)
#define CLOUD_SUPPORT_FX

#include "../Camera.fx"
#include "../Samplers.fx"
#include "../Atmosphere/SkySupport.fx"
#include "3DNoise.fx"

static const float	INV_4PI = 0.07957747154594766788444188168626;	// 1/4PI
static const float	INFINITY = 1e6;

#define DEEP_SHADOW_MAP_HI_RES	// Define this to use 8 deep shadow map values instead of 4 (low-res)

static const float4	SHADOW_OPACITIES = float4( 0.8, 0.6, 0.4, 0.2 );

float2		_BufferInvSize;
float		_InvVoxelSizeKm;							// Inverse of the size of a 3D texture voxel in kilometer (used to compute sampling mip level)

// Noise data
float4		_NoiseOffsets;
float		_NoiseConstrast;
Texture2D	_CloudProfileTexture;						// This texture controls the cloud profile

// Fade data
float		_UniformNoiseDensity;
float2		_UniformNoiseFadeDistancesKm;

float		_CloudSigma_s			: CLOUD_SIGMA_S;			// Scattering coefficient
float		_CloudSigma_t			: CLOUD_SIGMA_T;			// Extinction coefficient = Scattering + Absorption coefficient
float		_ShadowOpacity			: CLOUD_SHADOW_OPACITY;

// Cloud layer altitude & thickness in kilometers
float4		_CloudAltitudeKm		: CLOUD_ALTITUDE_THICKNESS;	// X=Bottom Altitude Y=Top Altitude Z=Thickness W=1/Thickness (all in kilometers)

// Lightning
float3		_LightningPosition		: CLOUD_LIGHTNING_POSITION;
float		_LightningIntensity		: CLOUD_LIGHTNING_INTENSITY;


// ===================================================================================
// Ray/Sphere Intersections

// Computes the intersection of the camera ray with the sphere at given altitude
//	_Altitude, altitude of the sphere in kilometers (relative to sea level)
//
float	ComputeSphereIntersection( float3 _PositionKm, float3 _View, float _AltitudeKm )
{
	float	RadiusKm = EARTH_RADIUS + _AltitudeKm;
	float3	D = float3( 0.0, EARTH_RADIUS, 0.0 ) + _PositionKm;

	float	b = dot(D,_View);
	float	c = dot(D,D) - RadiusKm*RadiusKm;
	float	Delta = b*b-c;
	if ( Delta < 0.0 )
		return INFINITY;

	return c > 0.0 ?
			-b - sqrt(Delta) :		// Entering the sphere
			-b + sqrt(Delta);		// Exiting the sphere
}

// Computes the intersection of the camera ray ENTERING the sphere at given altitude
//	_Altitude, altitude of the sphere in kilometers (relative to sea level)
//
float	ComputeSphereIntersectionEnter( float3 _PositionKm, float3 _View, float _AltitudeKm )
{
	float	RadiusKm = EARTH_RADIUS + _AltitudeKm;
	float3	D = float3( 0.0, EARTH_RADIUS, 0.0 ) + _PositionKm;

	float	b = dot(D,_View);
	float	c = dot(D,D) - RadiusKm*RadiusKm;
	float	Delta = b*b-c;

	return Delta >= 0.0 ? -b - sqrt(Delta) : INFINITY;
}

// Computes the intersection of the camera ray EXITING the sphere at given altitude
//	_Altitude, altitude of the sphere in kilometers (relative to sea level)
//
float	ComputeSphereIntersectionExit( float3 _PositionKm, float3 _View, float _AltitudeKm )
{
	float	RadiusKm = EARTH_RADIUS + _AltitudeKm;
	float3	D = float3( 0.0, EARTH_RADIUS, 0.0 ) + _PositionKm;

	float	b = dot(D,_View);
	float	c = dot(D,D) - RadiusKm*RadiusKm;
	float	Delta = b*b-c;

	return Delta >= 0.0 ? -b + sqrt(Delta) : INFINITY;
}


// ===================================================================================
// Shadow map transforms
float3		_ShadowPlaneCenterKm	: SHADOW_PLANE_CENTER;
float3		_ShadowPlaneX			: SHADOW_PLANE_X;
float3		_ShadowPlaneY			: SHADOW_PLANE_Y;
float2		_ShadowPlaneOffsetKm	: SHADOW_PLANE_OFFSET;

// P => UV
float4		_ShadowQuadVertices		: SHADOW_QUAD_VERTICES;	// The 2 opposite vertices at uv=(0,0) and uv=(1,1)
float4		_ShadowNormalsU			: SHADOW_NORMALS_U;		// The 2 edge normals used for U computation
float4		_ShadowNormalsV			: SHADOW_NORMALS_V;		// The 2 edge normals used for V computation

// UV => P
float3		_ShadowABC				: SHADOW_ABC;
float3		_ShadowDEF				: SHADOW_DEF;
float3		_ShadowGHI				: SHADOW_GHI;
float3		_ShadowJKL				: SHADOW_JKL;

// Transforms a 2D point lying in the shadow quadrilateral into UV
float2	Quad2Shadow( float2 _P )
{
	float4	D = _P.xyxy - _ShadowQuadVertices;
	float2	dUV0 = float2( dot( D.xy, _ShadowNormalsU.xy ), dot( D.xy, _ShadowNormalsV.xy ) );
	float2	dUV1 = float2( dot( D.zw, _ShadowNormalsU.zw ), dot( D.zw, _ShadowNormalsV.zw ) );
	return dUV0 / (dUV0 + dUV1);
}

// Transforms a shadow map UV coordinate into a 2D point lying in the shadow quadrilateral
float2	Shadow2Quad( float2 _UV )
{
	float3	U = _ShadowDEF * _UV.x - _ShadowABC;
	float3	V = _ShadowJKL * _UV.y - _ShadowGHI;
	float	IDen = 1.0 / (V.x*U.y - V.y*U.x);
	return float2( V.y*U.z - V.z*U.y, V.z*U.x - V.x*U.z ) * IDen;
}

// Projects a WORLD position (in kilometers) onto the shadow plane
//	_Distance2PlaneKm, the distance of the position to the shadow plane
float2	World2Quad( float3 _PositionKm, out float _Distance2PlaneKm )
{
	float3	D = _PositionKm - _ShadowPlaneCenterKm;
	_Distance2PlaneKm = -dot( D, _SunDirection );
	float3	ProjPosition = D + _Distance2PlaneKm * _SunDirection;								// Project onto plane
	return float2( dot( ProjPosition, _ShadowPlaneX ), dot( ProjPosition, _ShadowPlaneY ) );	// Retrieve 2D coordinates on the plane
}

// Projects a shadow plane (i.e. quad) position into a WORLD position (in kilometers)
float3	Quad2World( float2 _QuadPositionKm )
{
	return _ShadowPlaneCenterKm + _QuadPositionKm.x * _ShadowPlaneX + _QuadPositionKm.y * _ShadowPlaneY;
}

// Computes the distances to reach the top and bottom cloud sphere from a position in the shadow plane
// X = Distance to top of the cloud (minimum distance) (X < Y)
// Y = Distance to the bottom of the cloud (maximum distance) (Y > X)
float2	ComputeCloudHeights( float2 _QuadPositionKm )
{
	_QuadPositionKm += _ShadowPlaneOffsetKm;	// Offset back to plane's tangent position (the real center)

	float	SqDistance = dot( _QuadPositionKm, _QuadPositionKm );
	float	CloudRadiusKmTop = EARTH_RADIUS + _CloudAltitudeKm.y;
	float	CloudRadiusKmBottom = EARTH_RADIUS + _CloudAltitudeKm.x;

	return float2( CloudRadiusKmTop - sqrt( CloudRadiusKmTop*CloudRadiusKmTop - SqDistance ), CloudRadiusKmTop - sqrt( CloudRadiusKmBottom*CloudRadiusKmBottom - SqDistance ) );
}

// Transforms a WORLD position (in kilometers) into shadow UV
//	_CloudHeights, will contain the distances to the top and bottom cloud spheres
float2	World2Shadow( float3 _PositionKm, out float2 _CloudHeights, out float _Distance2PlaneKm )
{
	float2	QuadPos = World2Quad( _PositionKm, _Distance2PlaneKm );	// Project world => quad following the Sun's direction
	_CloudHeights = ComputeCloudHeights( QuadPos );					// Get distances to top and bottom cloud spheres
	return Quad2Shadow( QuadPos );									// Get UV from quad pos
}

// Transforms a shadow UV into WORLD position (in kilometers)
//	_CloudHeights, will contain the distances to the top and bottom cloud spheres
float3	Shadow2World( float2 _UV, out float2 _CloudHeights )
{
	float2	QuadPos = Shadow2Quad( _UV );
	_CloudHeights = ComputeCloudHeights( QuadPos );	// Get distances to top and bottom cloud spheres
	return Quad2World( QuadPos );
}


// ===================================================================================
// Computes shadowing due to Earth blocking the Sun
// Returns a pair of distances along the ray where the Earth shadow is cast
float2	ComputeEarthShadowDistances( float3 _PositionKm, float3 _View )
{
// TODO!
// 	if ( _SunDirection.y < 0.0 )
// 	{	// Project current position in the 2D plane orthogonal to the light to test the intersection with the shadow cylinder cast by the Earth
// 		float3	X = normalize( cross( _SunDirection, float3( 0.0, 1.0, 0.0 ) ) );
// 		float3	Z = cross( X, _SunDirection );
// 
// 		float2	P = float2( HeightKm*X.y, HeightKm*Z.y );			// Position in plane
// 		float2	V = float2( dot( View, X ), dot( View, Z ) );	// View in plane
// 		float	a = dot( V, V );
// 		float	b = dot( P, V );
// 		float	c = dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
// 		float	Delta = b*b - a*c;
// 		if ( Delta > 0.0 )
// 		{	// Distance til we hit the terminator, in WORLD space
// 			if ( c < 0 )
// 				TerminatorDistance = float2( 0.0, (-b+sqrt(Delta)) / max( 1e-4, _WorldUnit2Kilometer * a) );	// From inside
// 			else
// 				TerminatorDistance = float2( (-b-sqrt(Delta)) / max( 1e-4, _WorldUnit2Kilometer * a), 1e6 );	// From outside
// 		}
// 	}

	return float2( -1e6, -1e5 );	// NEVER !
}

float	ComputeEarthShadow( float _CurrentDistanceKm, float2 _EarthShadowDistances )
{
	float	ShadowThicknessKm = _EarthShadowDistances.y - _EarthShadowDistances.x;
	float	ShadowTransitionKm = 0.01 * ShadowThicknessKm;
//	float	InvShadowTransitionKm = 1.0 / ShadowTransitionKm;
	float2	ShadowIn = float2( _EarthShadowDistances.x - ShadowTransitionKm, _EarthShadowDistances.x + ShadowTransitionKm );
	float2	ShadowOut = float2( _EarthShadowDistances.y - ShadowTransitionKm, _EarthShadowDistances.y + ShadowTransitionKm );

	return 1.0 - smoothstep( ShadowIn.x, ShadowIn.y, _CurrentDistanceKm ) * smoothstep( ShadowOut.y, ShadowOut.x, _CurrentDistanceKm );
}

// ===================================================================================
// Computes the mip level to use for sampling the 3D noise texture
//	_CameraPositionKm, the position of the viewer (in kilometers)
//	_PositionKm, the sampling position (in kilometers)
//
float	ComputeNoiseMipLevel( float _Distance2CameraKm )
{
	// Compute the amount of 3D noise voxels seen in a single image pixel
//	float	VoxelSizeKm = 1.0 / (16.0 * _NoiseSize * _CloudAltitudeKm.w);				// Size of a noise voxel in kilometers
	float	PixelSizeKm = 2.0 * _Distance2CameraKm * CameraData.x * _BufferInvSize.y;	// Vertical size of a pixel in kilometers at the distance where we're sampling the texture

	float	EncompassedVoxels = PixelSizeKm * _InvVoxelSizeKm;							// This is the amount of voxels we can see in that pixel
	return log2( EncompassedVoxels );													// And the miplevel we need to sample the texture with...
}
float	ComputeNoiseMipLevel( float3 _CameraPositionKm, float3 _PositionKm )
{
	return ComputeNoiseMipLevel( length( _PositionKm - _CameraPositionKm ) ); 
}

// ===================================================================================
// Bevels the noise values so the clouds completely disappear at top and bottom
//
float	Bevel( float _HeightInCloudKm )
{
// Sample profile
return _CloudProfileTexture.SampleLevel( LinearClamp, float2( _HeightInCloudKm * _CloudAltitudeKm.w, 0.5 ), 0.0 ).x;

	// Compute a normalized height that is 0 at the center of the cloud layer, and 1 at the top/bottom
	float	NormalizedHeight = 1.0 - abs( 2.0 * _HeightInCloudKm * _CloudAltitudeKm.w - 1.0 );
	float	Bevel = smoothstep( 0.0, 1.0, NormalizedHeight );
	return pow( Bevel, 0.1 );
}

float	ComputeNoiseOffset( float3 _PositionKm )
{
return _NoiseOffsets.x;
	float	NormalizedHeight = pow( saturate( (_PositionKm.y - _CloudAltitudeKm.x) * _CloudAltitudeKm.w ), _NoiseOffsets.z );
	return lerp( _NoiseOffsets.x, _NoiseOffsets.y, NormalizedHeight );
}

// ===================================================================================
// Converts a position in kilometers into a 3D volume cloud position
float3	Kilometer2Volume( float3 _PositionKm, out float _HeightInCloudKm )
{
	float3	EarthCenter2Position = _PositionKm - float3( 0.0, -EARTH_RADIUS, 0.0 );
	_HeightInCloudKm = length( EarthCenter2Position ) - EARTH_RADIUS - _CloudAltitudeKm.x;

	return _PositionKm.xzy * _NoiseSize;	// A simple scale
}

// ===================================================================================
// Computes the cloud density at current position

float2	ComputeIsotropicDensities( float3 _PositionKm, float _HeightInCloudKm )
{
// 	// Compute amounts of volume elements for each octave
// 	float	CubeFreq = _NoiseFrequencyFactor.x * _NoiseFrequencyFactor.x * _NoiseFrequencyFactor.x;
// 	float	NoiseSizeKm = 1.0 / _NoiseSize;				// Size of the unit noise cube in kilometers
// 	float	V0 = NoiseSizeKm*NoiseSizeKm*NoiseSizeKm;		// Cube volume (km^3)
// 	float	V1 = V0 * 0.52359877559829887307710723054658;	// 0.5 radius sphere volume = Cube Volume * 4/3PI(0.5)^3
// 	float	V2 = V0 * 0.06544984694978735913463840381832;	// 0.25 radius sphere volume = Cube Volume * 4/3PI(0.25)^3
// 
// 	// Top sphere
// 	float	SphereRadiusTop = _CloudAltitudeKm.z - _HeightInCloudKm;
// 	float	SphereVolumeTop = 4.1887902047863909846168578443727 * SphereRadiusTop * SphereRadiusTop * SphereRadiusTop;				// Sphere volume in km^3 = 4/3PIr^3
// 
// 	float	V = SphereVolumeTop;
// 	float4	Nt = V / V0;
// 	Nt.y = Nt.x * CubeFreq;
// 	Nt.z = Nt.y * CubeFreq;
// 	Nt.w = Nt.z * CubeFreq;
// 
// 	// Bottom sphere
// 	float	SphereRadiusBottom = _HeightInCloudKm;
// 	float	SphereVolumeBottom = 4.1887902047863909846168578443727 * SphereRadiusBottom * SphereRadiusBottom * SphereRadiusBottom;	// Sphere volume in km^3 = 4/3PIr^3
// 
// 	V = SphereVolumeBottom;
// 	float4	Nb = V / V0;
// 	Nb.y = Nb.x * CubeFreq;
// 	Nb.z = Nb.y * CubeFreq;
// 	Nb.w = Nb.z * CubeFreq;
// 
// 	float	Rt = Nt.x;			// Amount of cubes/spheres at octave #0
// 	float	Rb = Nb.x;
// 	float	Amplitude = _NoiseAmplitudeFactor.x;
// 	Rt +=  Amplitude * Nt.y;	// + Amount of cubes/spheres at octave #1
// 	Rb +=  Amplitude * Nb.y;
// 	Amplitude *= _NoiseAmplitudeFactor.x;
// 	Rt += Amplitude * Nt.z;		// + Amount of cubes/spheres at octave #2
// 	Rb += Amplitude * Nb.z;
// 	Amplitude *= _NoiseAmplitudeFactor.x;
// 	Rt += Amplitude * Nt.w;		// + Amount of cubes/spheres at octave #3
// 	Rb += Amplitude * Nb.w;
// 
//  float	IsotropicDensityTop = 2.0 * Rt * V0 / (4096.0 * SphereVolumeTop);		// Average noise value for top isotropic lighting
//  float	IsotropicDensityBottom = 2.0 * Rb * V0 / (4096.0 * SphereVolumeBottom);	// Average noise value for bottom isotropic lighting
// 	float	IsotropicDensityTop = Rt * 0.000244140625 * _NoiseSumValues;
// 	float	IsotropicDensityBottom = Rb * 0.000244140625 * _NoiseSumValues;

	// After many tergiversations, I found that a magic constant is best for isotropic diffusion ! Back to the begining... UU'
	return 0.6;
}

// Computes the cloud density at current position
//	_PositionKm, World position (in kilometers)
//	_Distance2CameraKm, distance of the sampling position from the camera (in kilometers)
//
float3	ComputeCloudDensity( float3 _PositionKm, float _Distance2CameraKm, out float _HeightInCloudKm, float _MipLevel=0.0 )
{
	float3	UVW = Kilometer2Volume( _PositionKm, _HeightInCloudKm );

	float	NoiseDensity = Bevel( _HeightInCloudKm ) * fbm( UVW, _MipLevel );
//	float	NoiseDensity = fbm( UVW, _MipLevel );
//	float	NoiseDensity = 0.5 * (1.0 + sin( UVW.x ) * sin( UVW.z ));
	float	NoiseOffset = ComputeNoiseOffset( _PositionKm );
	float	CloudDensity = saturate( _NoiseConstrast * (NoiseDensity + NoiseOffset) );

	// Fade to uniform density with distance
	float	UniformDensity = saturate( _NoiseConstrast * (_UniformNoiseDensity + NoiseOffset) );
	float	DistanceFade = smoothstep( _UniformNoiseFadeDistancesKm.x, _UniformNoiseFadeDistancesKm.y, _Distance2CameraKm );
	CloudDensity = lerp( CloudDensity, UniformDensity, DistanceFade );

	return float3( CloudDensity, ComputeIsotropicDensities( _PositionKm, _HeightInCloudKm ) );

// 	// Alter offset based on position so we alternate areas of high and low cloud coverage
// 	float3	AreaOffset = _CloudSpeed * _CloudTime * float3( 1.0, 0.0, 1.0 );
// 	float	Value  = _NoiseTexture0.SampleLevel( VolumeSampler, World2Volume( 0.1 * (_WorldPosition + AreaOffset) ), _MipLevel ).x;
// 	float	AlteredOffset = NoiseOffset * lerp( 0.25, 1.75, Value);
// 
// 	return _NoiseConstrast * saturate( NoiseDensity + AlteredOffset );
}

// ===================================================================================
// Computes the traversed optical depth at current position within the cloud

#ifndef RENDER_SKY_PROBE
// The standard cloud rendering uses planar deep shadow maps

// Deep shadow maps
Texture2D	_DeepShadowMap0			: CLOUD_DSM0;
Texture2D	_DeepShadowMap1			: CLOUD_DSM1;

float	ComputeCloudOpticalDepth( float3 _PositionKm, float _Distance2CameraKm=0.0 )
{
	// Project into shadow map
	float	Distance2PlaneKm;
	float2	CloudDistancesKm;
	float2	UV = World2Shadow( _PositionKm, CloudDistancesKm, Distance2PlaneKm );

	float	CloudThicknessKm = CloudDistancesKm.y - CloudDistancesKm.x;

#if defined(DEEP_SHADOW_MAP_HI_RES)
	// Retrieve densities
// 	float4	OpticalThickness0 = _DeepShadowMap0.SampleLevel( LinearMirror, UV, 0 );
// 	float4	OpticalThickness1 = _DeepShadowMap1.SampleLevel( LinearMirror, UV, 0 );
	float4	OpticalThickness0 = _DeepShadowMap0.SampleLevel( LinearClamp, UV, 0 );
	float4	OpticalThickness1 = _DeepShadowMap1.SampleLevel( LinearClamp, UV, 0 );

//OpticalThickness0 = OpticalThickness1 = 0.0;

	// Normalize distance into 8 equal steps
	Distance2PlaneKm = 8.0 * (Distance2PlaneKm - CloudDistancesKm.x) / CloudThicknessKm;

	// Interpolate densities
	float	OpticalThickness = 0.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.x;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.y;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.z;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.w;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness1.x;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness1.y;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness1.z;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness1.w;

#else	// Low-res version uses only 1 DSM tap and 4 samples
	// Retrieve densities
	float4	OpticalThickness0 = _DeepShadowMap0.SampleLevel( LinearMirror, UV, 0 );

	// Normalize distance into 4 equal steps
	Distance2PlaneKm = 4.0 * Distance2PlaneKm / CloudThicknessKm;

	// Interpolate densities
	float	OpticalThickness = 0.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.x;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.y;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.z;	Distance2PlaneKm -= 1.0;
	OpticalThickness += saturate( Distance2PlaneKm ) * OpticalThickness0.w;
#endif

	// Fade to uniform density with distance
// 	float	UniformDensity = saturate( _NoiseConstrast * (_UniformNoiseDensity + _NoiseOffsets.w) );
// 	float	DistanceFade = smoothstep( _UniformNoiseFadeDistancesKm.x, _UniformNoiseFadeDistancesKm.y, _Distance2CameraKm );
// 	Density = lerp( Density, UniformDensity, DistanceFade );

	return OpticalThickness;
}

#else

// Probe rendering uses a reduced hemispherical "deep shadow map"
//
Texture2D	_SkyProbeShadowMap;

float	ComputeCloudOpticalDepth( float3 _PositionKm, float _Distance2CameraKm=0.0 )
{
	float3	View = _PositionKm, _SkyProbePositionKm;
	float	Distance2CloudKm = length(View);
			View /= Distance2CloudKm;
	float2	PhiTheta = float2( atan2( View.x, View.z ), acos( View.y ) );
	float2	UV = (PhiTheta - _SkyProbeAngles.xy) / _SkyProbeAngles.zw;

	float4	OpticalThickness0 = _SkyProbeShadowMap.SampleLevel( LinearClamp, UV, 0 );

	Distance2CloudKm = 4.0 * (_CloudAltitudeKm.y - Distance2CloudKm) * _CloudAltitudeKm.w;

	float	OpticalThickness = 0.0;
	OpticalThickness += saturate( Distance2CloudKm ) * OpticalThickness0.x; Distance2CloudKm -= 1.0;
	OpticalThickness += saturate( Distance2CloudKm ) * OpticalThickness0.y; Distance2CloudKm -= 1.0;
	OpticalThickness += saturate( Distance2CloudKm ) * OpticalThickness0.z; Distance2CloudKm -= 1.0;
	OpticalThickness += saturate( Distance2CloudKm ) * OpticalThickness0.w;

	return OpticalThickness;
}

#endif

// float	ComputeCloudExtinction( float3 _PositionKm, float _Distance2CameraKm=0.0 )
// {
// 	// Project into shadow map
// 	float	Z;
// 	float2	CloudDistancesKm;
// 	float2	UV = World2Shadow( _PositionKm, CloudDistancesKm, Z );
// 
// 	Z -= CloudDistancesKm.x;	// Offset by distance
// 	if ( Z < 0.0 )
// 		return 0.0;				// Ensure we're on the right side of the plane, otherwise no shadow is cast anyway...
// 
// 	// Read back distance intervals
// 	float4	ShadowZ = _DeepShadowMap0.SampleLevel( LinearMirror, UV, 0 );
// 	float	Z0 = ShadowZ.x;
// 	float	Z1 = ShadowZ.y;
// 	float	Z2 = ShadowZ.z;
// 	float	Z3 = ShadowZ.w;
// 
// 	float	MinZ0 = step( Z, Z0 );
// 	float	MinZ1 = step( Z, Z1 );
// 	float	MinZ2 = step( Z, Z2 );
// 	float	MinZ3 = step( Z, Z3 );
// 
// 	float	IsIn0 = MinZ0;					// Inside [0,Z0]
// 	float	IsIn1 = MinZ1 * (1.0 - MinZ0);	// Inside [Z0,Z1]
// 	float	IsIn2 = MinZ2 * (1.0 - MinZ1);	// Inside [Z1,Z2]
// // 	float	IsIn3 = MinZ3 * (1.0 - MinZ2);	// Inside [Z2,Z3]
// // 	float	IsIn4 = 1.0-MinZ3;				// Inside [Z3,+oo[
// 	float	IsIn3 = 1.0 - MinZ2;			// Inside [Z2,+oo]
// 
// 	// Compute extinction based on distance interval
// 	float	A0 = SHADOW_OPACITIES.x;
// 	float	A1 = SHADOW_OPACITIES.y;
// 	float	A2 = SHADOW_OPACITIES.z;
// 	float	A3 = SHADOW_OPACITIES.w;
// 
// 	return	saturate( IsIn0 * lerp( 1.0, A0, (Z - 0.0) / (Z0 - 0.0) )		// Interpolate from extinction = [1 -> 0.8]
// 					+ IsIn1 * lerp(  A0, A1, (Z -  Z0) / (Z1 -  Z0) )		// Interpolate from extinction = [0.8 -> 0.6]
// 					+ IsIn2 * lerp(  A1, A2, (Z -  Z1) / (Z2 -  Z1) )		// Interpolate from extinction = [0.6 -> 0.4]
// 					+ IsIn3 * lerp(  A2, A3, (Z -  Z2) / (Z3 -  Z2) )		// Interpolate from extinction = [0.4 -> 0.2] (and extrapolate further)
// //					+ IsIn4 * max( 0.0, lerp( A2, A3, (Z -  Z2) / (Z3 -  Z2) );
// 					);
// }

// ===================================================================================
// Simple helper to compute lighting for standard objects
float3	ComputeIncomingLight( float3 _WorldPosition )
{
	float3	PositionKm = _WorldUnit2Kilometer * _WorldPosition;

	// =============================================
	// Sample density at current altitude and optical depth in Sun direction
	float	SkyAltitudeKm = max( 0, PositionKm.y );
	float4	OpticalDepth = ComputeOpticalDepth( SkyAltitudeKm, _SunDirection, float3( 0, 1, 0 ) );

	// =============================================
	// Retrieve sun light attenuated when passing through the atmosphere
	float3	OpticalDepth_cloud = ComputeCloudOpticalDepth( PositionKm );
	float3	SunExtinction = exp( -_Sigma_Rayleigh * OpticalDepth.z - _Sigma_Mie * OpticalDepth.w - OpticalDepth_cloud );

	return _SunIntensity * SunExtinction;
}


// ===================================================================================
// Computes lighting by lightning
//	_WorldPosition, the position of the point to light
//	_WorldNormal, the normal of the point to light
//
float	ScatteringAnisotropyLightning = 0.0;

float	ComputeLightningLightingCloud( float3 _WorldPosition, float3 _View )
{
	float3	ToLightning = _LightningPosition - _WorldPosition;
	float	SqDistance2Lightning = dot( ToLightning, ToLightning );
	float	CurrentLightningIntensity = _LightningIntensity / max( 1.0, SqDistance2Lightning );

	ToLightning /= sqrt( SqDistance2Lightning );
	float	LightningDot = dot( _View, ToLightning );
	float	Den = 1.0 / (1.0 + ScatteringAnisotropyLightning * LightningDot);
	float	LightningPhase = (1.0 - ScatteringAnisotropyLightning*ScatteringAnisotropyLightning) * Den * Den;

	return CurrentLightningIntensity * LightningPhase;
}

float	ComputeLightningLightingSurface( float3 _WorldPosition, float3 _WorldNormal )
{
	float3	ToLightning = _LightningPosition - _WorldPosition;
	float	SqDistance2Lightning = dot( ToLightning, ToLightning );
	float	CurrentLightningIntensity = _LightningIntensity / max( 1.0, SqDistance2Lightning );

	ToLightning /= sqrt( SqDistance2Lightning );

	float	DotLightning = saturate( dot( ToLightning, _WorldNormal ) );

	return 1000.0 * DotLightning * CurrentLightningIntensity;
}

#endif	// CLOUD_SUPPORT_FX
