// ===============================================================
// Include this file to obtain readable ZBuffer support
// ===============================================================
// You must create a readable DepthStencil target to be able to read from the ZBuffer
// Also, this shader interface makes the assumption there is no stencil value encoded besides depth
//
#if !defined(READABLE_ZBUFFER_SUPPORT_FX)
#define READABLE_ZBUFFER_SUPPORT_FX

#include "Camera.fx"
#include "Samplers.fx"

float2		ZBufferInvSize	: ZBUFFER_INV_SIZE;
Texture2D	ZBuffer			: ZBUFFER;

// Reads the depth of a pixel from its SV_POSITION
//	_Position, the SV_POSITION of the pixel
//
float	ReadDepth( float2 _Position )
{
	float2	UV = _Position * ZBufferInvSize;
	float	Zproj = ZBuffer.SampleLevel( LinearClamp, UV, 0 ).x;
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf-Zn)
	return (Q * CameraData.z) / (Q - Zproj);
}

// Reads the CAMERA position of a pixel from its SV_POSITION
//	_Position, the SV_POSITION of the pixel
// Remarks: to obtain the distance of a pixel rather than its depth, use length(ReadCameraPosition(...))
//
float3	ReadCameraPosition( float2 _Position )
{
	float2	UV = _Position * ZBufferInvSize;
	float	Zproj = ZBuffer.SampleLevel( LinearClamp, UV, 0 ).x;
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf-Zn)
	float	Z = (Q * CameraData.z) / (Q - Zproj);
	float3	View = float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 );	// Un-normalized view vector
	return Z * View;
}

#endif