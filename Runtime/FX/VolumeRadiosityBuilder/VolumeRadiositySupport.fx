// ===============================================================
// Include this file to obtain volume radiosity support
// ===============================================================
//
#if !defined( VOLUME_RADIOSITY )
#define VOLUME_RADIOSITY

#include "../Camera.fx"

float3		FieldOrigin				: VR_FIELD_ORIGIN;
float		VoxelSize				: VR_VOXEL_SIZE;
float		VoxelInvSize			: VR_VOXEL_INV_SIZE;
float		SamplingMipBias			: VR_MIP_BIAS;
float3		VolumeInvSize			: VR_VOLUME_INV_SIZE;
Texture3D	IndirectLightingField	: VR_INDIRECT_LIGHTING_FIELD;
Texture3D	DistanceField			: VR_DISTANCE_FIELD;
Texture3D	DiffuseField			: VR_DIFFUSE_FIELD;

// Converts a WORLD space position into a radiosity field (i.e. voxel) coordinate
//
float3	World2RadiosityField( float3 _WorldPosition )
{
	// This is an annoying offset I have to take for some reasons that elude me...
	_WorldPosition += VoxelSize * float3( -0.5, 1.0, -0.5 );

	return (_WorldPosition - FieldOrigin) * float3( VoxelInvSize, -VoxelInvSize, VoxelInvSize );
}

// Computes the appropriate mip level for indirect lighting sampling
//	_Distance2Camera, the distance of the point to sample from the camera
//	_ScreenHeight, the amount of vertical pixels on screen
//
float	ComputeIndirectLightingMipLevel( float _Distance2Camera, float _ScreenHeight )
{
	float	PixelSize = 2.0 * CameraData.x / _ScreenHeight;		// tan(FOV/2) / (Height/2)
	float	RadiusAtDistance = _Distance2Camera * PixelSize;
	return max( SamplingMipBias, log2( RadiusAtDistance * VoxelInvSize ) );
//	return max( 1.0, log2( RadiusAtDistance * VoxelInvSize ) );
}

// Samples the indirect lighting at the provided voxel coordinate and mip level
float3	SampleIndirectLighting( float3 _VoxelCoordinates, float _MipLevel )
{
	float3	UVW = (_VoxelCoordinates + 0.5) * VolumeInvSize;
	return IndirectLightingField.SampleLevel( LinearClamp, UVW, _MipLevel ).xyz;
}

// Gets the indirect lighting at the given world position
// Use this method to compute indirect lighting for a static mesh that was part of the radiosity computation
// (e.g. the walls of a building that was used in the computation)
//
float3	GetIndirectLightingStatic( float3 _WorldPosition, float _ScreenHeight )
{
	float	MipLevel = ComputeIndirectLightingMipLevel( length(_WorldPosition - Camera2World[3].xyz), _ScreenHeight );
	float3	VoxelCoordinates = World2RadiosityField( _WorldPosition );
	return SampleIndirectLighting( VoxelCoordinates, MipLevel );
}

// This is the same routine as above but with distance field correction
// If you're getting indirect lighting for a static mesh that was used in the computation then
//	this method is quite useless as the distance field on the surface of the mesh will likely
//	to be 0. It might be useful if computing for low resolution 3D textures...
//
float3	GetIndirectLightingStatic2( float3 _WorldPosition, float _ScreenHeight )
{
	float	MipLevel = ComputeIndirectLightingMipLevel( length(_WorldPosition - Camera2World[3].xyz), _ScreenHeight );
	float3	VoxelCoordinates = World2RadiosityField( _WorldPosition );

	// Use the distance field to obtain the distance to the nearest surface where indirect lighting was computed
	float3	UVW = (VoxelCoordinates + 0.5) * VolumeInvSize;
	float4	ToNearestSurface = DistanceField.SampleLevel( LinearClamp, UVW, 0.0 );

	// Go to surface
	VoxelCoordinates += ToNearestSurface.xyz;
//	return 10.0 * ToNearestSurface.w;

	return SampleIndirectLighting( VoxelCoordinates, MipLevel );
}

// Gets the indirect lighting at the given world position with a given normal
// Use this method to compute indirect lighting for a dynamic mesh that was NOT part of the radiosity computation
// (e.g. your character walking in a building that was used in the computation)
//
float3	GetIndirectLightingDynamic( float3 _WorldPosition, float3 _WorldNormal )
{
	float3	VoxelCoordinates = World2RadiosityField( _WorldPosition );

	// Use the distance field to obtain the distance to the nearest surface where indirect lighting was computed
	// TODO : Use normal to fetch the correct distance field
	float3	UVW = (VoxelCoordinates + 0.5) * VolumeInvSize;
	float4	ToNearestSurface = DistanceField.SampleLevel( NearestClamp, UVW, 0.0 );

	// Go to surface
	VoxelCoordinates += ToNearestSurface.xyz;

	// Compute mip level using the distance from lighting point to surface
	// We assume that the sampling ray has an aperture angle of about 10°
	float	DistancePoint2Surface = ToNearestSurface.w * VoxelSize;					// Distance from lighting point to nearest surface in WORLD space
	float	sqDistancePoint2Surface = DistancePoint2Surface * DistancePoint2Surface;
	float	ProbeRaySolidAngle = 6.283185307179586476925286766559 * (1.0 - 0.98480775301220805936674302458952);	// 2PI*(1-cos(10°))
	float	ProbeDiscAreaAtDistance = sqDistancePoint2Surface * ProbeRaySolidAngle;	// Area of a disc subtended by the solid angle at distance to surface
	float	ProbeSphereRadiusAtDistance = sqrt( ProbeDiscAreaAtDistance );			// Radius of a probe sphere at distance to surface
	float	MipLevel = max( 0.0, log2( ProbeSphereRadiusAtDistance * VoxelInvSize ) );

	// Sample indirect lighting and albedo at surface
	float3	SurfaceIndirectLighting = SampleIndirectLighting( VoxelCoordinates, MipLevel );
	UVW = (VoxelCoordinates + 0.5) * VolumeInvSize;
	float3	SurfaceAlbedo = DiffuseField.SampleLevel( LinearClamp, UVW, 0.0 ).xyz;

	// Return indirect lighting emitted from surface
	// I(x) ~= Rho(surface)/PI * Irradiance(surface) / R²
	// with R the distance to the surface
	//
//	return SurfaceAlbedo * 0.31830988618379067153776752674503 * SurfaceIndirectLighting / sqDistancePoint2Surface;
	return SurfaceAlbedo * 0.31830988618379067153776752674503 * SurfaceIndirectLighting;
}

#endif