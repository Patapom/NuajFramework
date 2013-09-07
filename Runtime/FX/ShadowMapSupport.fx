// ===============================================================
// Include this file to obtain shadow map support
// ===============================================================
//
// There can be up to 6 Z-slices of shadow maps
// IMPORTANT NOTE: To be able to use the shadow map interface,
//	you must also include the camera support (i.e. Camera.fx)
//
#if !defined(SHADOW_MAP_SUPPORT_FX)
#define SHADOW_MAP_SUPPORT_FX

cbuffer	cbCascadedShadowMaps
{
	int			ShadowSlicesCount : SHADOW_SLICES_COUNT;
	float4		ShadowSliceRanges[6] : SHADOW_SLICE_RANGES;
	float4x4	World2ShadowMaps[6] : WORLD2SHADOWMAPS;
	float4x4	ShadowMaps2World[6] : SHADOWMAPS2WORLD;
}

Texture2DArray	ShadowMaps : SHADOW_MAPS;

SamplerComparisonState ShadowMapComparisonSampler
{
	Filter = COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
	AddressU = Clamp;
	AddressV = Clamp;
	ComparisonFunc = LESS;
};

SamplerState ShadowMapSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};


// This gives a boolean telling if the point is in shadow
//	_WorldPosition, the point in world position
// Returns true if the point is in shadow or false if in light
//
bool	IsInShadow2( float3 _WorldPosition )
{
	float4	WorldPosition = float4( _WorldPosition, 1 );
	float4	CameraPosition = mul( WorldPosition, World2Camera );

	// Retrieve the slice in which that position lies
	for ( int SliceIndex=0; SliceIndex < ShadowSlicesCount; SliceIndex++ )
		if ( CameraPosition.z >= ShadowSliceRanges[SliceIndex].x &&
			 CameraPosition.z <= ShadowSliceRanges[SliceIndex].y )
		{	// Within range ! Use that slice
			float4	ShadowMapPosition = mul( WorldPosition, World2ShadowMaps[SliceIndex] );

			// Read back depth at which an object may start shadowing our point...
			float	fShadowDepth = ShadowMaps.SampleCmpLevelZero( ShadowMapComparisonSampler, float3( ShadowMapPosition.xy, SliceIndex ), ShadowMapPosition.z ).r;

			// Compare depths
			return fShadowDepth > 0.5;
		}

	// Out of range ???
	return	false;
}

// This gives the distance in WORLD space the current point is from the shadow caster
//	_WorldPosition, the point in world position
// Returns a positive distance if the point is in shadow and negative otherwise
//
float	GetShadowLength( float3 _WorldPosition )
{
	float4	WorldPosition = float4( _WorldPosition, 1 );
	float4	CameraPosition = mul( WorldPosition, World2Camera );

	// Retrieve the slice in which that position lies
	for ( int SliceIndex=0; SliceIndex < ShadowSlicesCount; SliceIndex++ )
		if ( CameraPosition.z >= ShadowSliceRanges[SliceIndex].x &&
			 CameraPosition.z <= ShadowSliceRanges[SliceIndex].y )
		{	// Within range ! Use that slice
			float4	ShadowMapPosition = mul( WorldPosition, World2ShadowMaps[SliceIndex] );
			float	fShadowMapNear = ShadowSliceRanges[SliceIndex].z;
			float	fShadowMapFar = ShadowSliceRanges[SliceIndex].w;
			float	fShadowMapDepth = fShadowMapFar - fShadowMapNear;

			// Read back depth at which an object may start shadowing our point...
			float	fShadowDepth = fShadowMapNear + fShadowMapDepth * ShadowMaps.SampleLevel( ShadowMapSampler, float3( ShadowMapPosition.xy, SliceIndex ), 0.0 ).r;

			// Compute the provided point's depth from light's point of view
			float	fPointDepth = fShadowMapNear + fShadowMapDepth * ShadowMapPosition.z;

// 			return fShadowDepth;
// 			return fPointDepth;

			// Return delta
			return fPointDepth - fShadowDepth;
		}

	// Out of range ???
	return	0.0;
}

bool	IsInShadow( float3 _WorldPosition )
{
	return GetShadowLength( _WorldPosition ) > 0.0;
}


// This computes the position in WORLD space of the point casting its shadow on the provided point
//	_WorldPosition, the point in world position
// Returns the WORLD space position of the shadow caster
//
float3	GetShadowCasterPosition( float3 _WorldPosition )
{
	float4	WorldPosition = float4( _WorldPosition, 1 );
	float4	CameraPosition = mul( WorldPosition, World2Camera );

	// Retrieve the slice in which that position lies
	for ( int SliceIndex=0; SliceIndex < ShadowSlicesCount; SliceIndex++ )
		if ( CameraPosition.z >= ShadowSliceRanges[SliceIndex].x &&
			 CameraPosition.z <= ShadowSliceRanges[SliceIndex].y )
		{	// Within range ! Use that slice
			float4	ShadowMapPosition = mul( WorldPosition, World2ShadowMaps[SliceIndex] );
			float	fShadowMapNear = ShadowSliceRanges[SliceIndex].z;
			float	fShadowMapFar = ShadowSliceRanges[SliceIndex].w;
			float	fShadowMapDepth = fShadowMapFar - fShadowMapNear;

			// Read back depth at which an object may start shadowing our point...
			float	fShadowCasterPosition = ShadowMaps.SampleLevel( ShadowMapSampler, float3( ShadowMapPosition.xy, SliceIndex ), 0.0 ).r;

			// Replace our depth by the shadow map depth
			ShadowMapPosition.z = fShadowCasterPosition;

			// Transform back into WORLD space
			WorldPosition = mul( ShadowMapPosition, ShadowMaps2World[SliceIndex] );
			break;
		}

	return WorldPosition.xyz;
}


// This is a debug purpose function that returns a pre-defined color indicating
//	the shadow slice depending on the point's depth
//
float3	GetShadowSliceColor( float3 _WorldPosition )
{
	float4	WorldPosition = float4( _WorldPosition, 1 );
	float4	CameraPosition = mul( WorldPosition, World2Camera );

	float3	SliceColors[6] = {
		float3( 1.0, 0.5, 0.5 ),
		float3( 0.5, 1.0, 0.5 ),
		float3( 0.5, 0.5, 1.0 ),
		float3( 1.0, 1.0, 0.5 ),
		float3( 1.0, 0.5, 1.0 ),
		float3( 0.5, 1.0, 1.0 ),
	};

	// Retrieve the slice in which that position lies
	for ( int SliceIndex=0; SliceIndex < ShadowSlicesCount; SliceIndex++ )
		if ( CameraPosition.z >= ShadowSliceRanges[SliceIndex].x &&
			 CameraPosition.z <= ShadowSliceRanges[SliceIndex].y )
		{	// Within range ! Use that slice
			return	SliceColors[SliceIndex];
		}

	return float3( 0, 0, 0 );
}

#endif