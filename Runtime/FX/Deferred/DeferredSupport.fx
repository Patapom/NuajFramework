// Holds the method to pack/unpack data to and from the deferred buffers
//

// This is the structured stored in the multiple render targets use for deferred rendering
// NOTE: The "Normal" component is stored in CAMERA space !
//
struct PS_OUT
{
	float4	Color0	: SV_TARGET0;	// ALBEDO TARGET => Diffuse Albedo (XYZ) + Specular Albedo (W)
	float4	Color1	: SV_TARGET1;	// GEOMETRY TARGET => Normal (CAMERA SPACE!!) (XY) + Depth (Z) + Specular Power (W)
};

struct PS_OUTZ : PS_OUT
{
	float	Depth	: SV_DEPTH;		// Also write depth...
};

static const float3	LUMINANCE_WEIGHTS = float3( 0.2126, 0.7152, 0.0722 );	// RGB => Y (taken from http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering#Light_Adaptation)

// Deferred buffers reading
Texture2D	GBufferTexture0		: GBUFFER_TEX0;		// Contains Diffuse Albedo (XYZ) + Specular Albedo (W)
Texture2D	GBufferTexture1		: GBUFFER_TEX1;		// Contains Normal (XY) + Depth (Z) + Specular Power (W)
Texture2D	GBufferTexture2		: GBUFFER_TEX2;		// Contains Emissive (XYZ) + Extinction (W)
Texture2D	LightBufferTexture	: LIGHTBUFFER_TEX;	// The Light Buffer texture array to read from when reading back the deferred MRTs

// Writes a pixel to deferred format
//	_DiffuseAlbedo, the diffuse pixel albedo to write (straight from the diffuse textures, no lighting !)
//	_SpecularAlbedo, the specular pixel albedo to write (straight from the specular textures, no lighting !)
//	_SpecularPower, the pixel's specular power to write
//	_Normal, the normal in CAMERA space
//	_Depth, the linear depth in CAMERA space (i.e. current position's Z coordinate)
//
PS_OUT	WriteDeferredMRT( float3 _DiffuseAlbedo, float3 _SpecularAlbedo, float _SpecularPower, float3 _Normal, float _Depth )
{
	PS_OUT	Out;

	// 1st buffer contains color informations
	Out.Color0.xyz = _DiffuseAlbedo;
	Out.Color0.w = dot( _SpecularAlbedo, LUMINANCE_WEIGHTS );

	// 2nd buffer contains geometry informations
	_Normal.z = -_Normal.z;	// Our camera's Z is reversed...
	float3	OffsetNormal = normalize( _Normal + float3( 0.0, 0.0, 1.0 ) );
	Out.Color1.xy = OffsetNormal.xy;
	Out.Color1.z = _Depth;
	Out.Color1.w = _SpecularPower;

	return Out;
}

// Writes a pixel to deferred format and also updates SV_Depth
//	_DiffuseAlbedo, the diffuse pixel albedo to write (straight from the diffuse textures, no lighting !)
//	_SpecularAlbedo, the specular pixel albedo to write (straight from the specular textures, no lighting !)
//	_SpecularPower, the pixel's specular power to write
//	_Normal, the normal in CAMERA space
//	_Depth, the linear depth in CAMERA space (i.e. current position's Z coordinate)
//
PS_OUTZ	WriteDeferredMRTWithDepth( float3 _DiffuseAlbedo, float3 _SpecularAlbedo, float _SpecularPower, float3 _Normal, float _Depth )
{
	PS_OUTZ	Out;

	// 1st buffer contains color informations
	Out.Color0.xyz = _DiffuseAlbedo;
	Out.Color0.w = dot( _SpecularAlbedo, LUMINANCE_WEIGHTS );

	// 2nd buffer contains geometry informations
	_Normal.z = -_Normal.z;	// Our camera's Z is reversed...
	float3	OffsetNormal = normalize( _Normal + float3( 0.0, 0.0, 1.0 ) );
	Out.Color1.xy = OffsetNormal.xy;
	Out.Color1.z = _Depth;
	Out.Color1.w = _SpecularPower;

	// Write depth
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf - Zn)
	Out.Depth = (1.0 - CameraData.z / _Depth) * Q;

	return Out;
}

// Reads back a pixel from the deferred format
//	_ScreenPosition, the screen position in [0,1] to read back pixel infos from
//
void	ReadDeferredMRT( float2 _ScreenPosition, out float3 _EmissiveColor, out float _Extinction, out float3 _DiffuseAlbedo, out float3 _SpecularAlbedo, out float _SpecularPower, out float3 _Normal, out float _Depth )
{
	// Read back the packed data
	float4	Color0 = GBufferTexture0.SampleLevel( LinearClamp, _ScreenPosition, 0 );
	float4	Color1 = GBufferTexture1.SampleLevel( LinearClamp, _ScreenPosition, 0 );
	float4	Color2 = GBufferTexture2.SampleLevel( LinearClamp, _ScreenPosition, 0 );

	// Unpack them
	_DiffuseAlbedo = Color0.xyz;
	_SpecularAlbedo = Color0.www;
	_Normal.z = sqrt( 1.0 - dot( Color1.xy, Color1.xy ) );
	_Normal.xy = 2.0 * _Normal.z * Color1.xy;
	_Normal.z = 1.0 - 2.0 * _Normal.z * _Normal.z;
	_Depth = Color1.z;
	_SpecularPower = Color1.w;
	_EmissiveColor.xyz = Color2.xyz;
	_Extinction = Color2.w;
}

// Reads back the normal, depth and specular power of a pixel from the deferred format
//	_ScreenPosition, the screen position in [0,1] to read back pixel infos from
//
void	ReadDeferredMRTNormalDepthSpec( float2 _ScreenPosition, out float3 _Normal, out float _Depth, out float _SpecularPower )
{
	// Read back the packed data
	float4	Color1 = GBufferTexture1.SampleLevel( LinearClamp, _ScreenPosition, 0 );

	// Unpack them
	_Normal.z = sqrt( 1.0 - dot( Color1.xy, Color1.xy ) );
	_Normal.xy = 2.0 * _Normal.z * Color1.xy;
	_Normal.z = 1.0 - 2.0 * _Normal.z * _Normal.z;
	_Depth = Color1.z;
	_SpecularPower = Color1.w;
}
