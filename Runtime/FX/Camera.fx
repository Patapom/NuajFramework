// ===============================================================
// Include this file to obtain camera support
// ===============================================================
//
#if !defined(CAMERA_SUPPORT_FX)
#define CAMERA_SUPPORT_FX

cbuffer	cbCamera
{
	float4x4	World2Proj : WORLD2PROJ;
	float4x4	Camera2Proj : CAMERA2PROJ;
	float4x4	Camera2World : CAMERA2WORLD;
	float4x4	World2Camera : WORLD2CAMERA;
	float4		CameraData : CAMERA_DATA;		// For perspective cameras : X = tan(FOV/2) Y = AspectRatio Z = Near Clip W = Far Clip
												// For orthographic cameras : X = Height Y = AspectRatio Z = Near Clip W = Far Clip
}

// Computes the camera view vector given a UV screen coordinate in [0,1]
//
float3	GetCameraView( float2 _UV )
{
	return normalize( float3( CameraData.y * CameraData.x * (2.0 * _UV.x - 1.0), CameraData.x * (1.0 - 2.0 * _UV.y), 1.0 ) );
}

// Computes the non-normalized camera view vector given a UV screen coordinate in [0,1]
//
float3	GetCameraViewUnNormalized( float2 _UV )
{
	return float3( CameraData.y * CameraData.x * (2.0 * _UV.x - 1.0), CameraData.x * (1.0 - 2.0 * _UV.y), 1.0 );
}

#endif