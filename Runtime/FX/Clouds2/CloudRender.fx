// This shader displays clouds and sky
// Search for TODO !
//
// #include "../Camera.fx"
// #include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"
// #include "AmbientSkySupport.fx"
#include "CloudComputeSupport.fx"

// Ray-marching precision
static const int	STEPS_COUNT_CLOUDS = 64;
static const int	STEPS_COUNT_AIR = 8;
static const int	STEPS_COUNT_AIR_GOD_RAYS = 32;

static const float	GROUND_ALTITUDE_KM = -2.0;			// We define a "ground sphere" so rays can't go below the ground

Texture2D	_DownscaledZBuffer;							// ZBuffer downscaled by 2 and 4 (in the 2 first mip levels)

float		_CloudOpacityFactor = 100.0;				// Factor for cloud's pseudo ZBuffer
float		_JitterNoiseSize = 1.0;						// Noise size for jittering
float		_SamplingJitterAmplitude = 1.0;				// UV jittering amplitude in kilometer

struct VS_IN
{
	float4	Position		: SV_POSITION;
};

struct PS_IN
{
	float4	Position				: SV_POSITION;
	float3	AmbientSkyColor			: AMBIENT_SKY;
	float3	AmbientSkyColorNoCloud	: AMBIENT_SKY_NO_CLOUD;
};

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
// 	Out.AmbientSkyColor = ComputeAmbientSkyIrradiance( float3( 0, 1, 0 ) );
// 	Out.AmbientSkyColorNoCloud = ComputeAmbientSkyIrradianceNoCloud( float3( 0, 1, 0 ) );
	Out.AmbientSkyColor = ComputeAmbientSkyIrradiance( _SunDirection );
	Out.AmbientSkyColorNoCloud = ComputeAmbientSkyIrradianceNoCloud( _SunDirection );

	return Out;
}

// ===================================================================================
// Downscaled ZBuffer readback
float	ReadDownscaledDepth( float2 _Position )
{
	return _DownscaledZBuffer.SampleLevel( NearestClamp, _Position * _BufferInvSize, 1.0 ).x;
}

// ===================================================================================
// Renders the entire sky+clouds
void	RenderSky( float3 _CameraPositionKm, float3 _View, float _Z, float _ViewLength, float3 _AmbientSkyColor, float3 _AmbientSkyColorNoCloud, out float3 _Extinction, out float3 _InScattering, out float _CloudOpticalDepth )
{
	float	MaxDistanceKm = _WorldUnit2Kilometer * _Z * _ViewLength;

	// If pixel is at "infinity" then compute intersection with upper atmosphere
	if ( _Z > CameraData.w-5.0 )
		MaxDistanceKm = ComputeSphereIntersectionExit( _CameraPositionKm, _View, ATMOSPHERE_THICKNESS );

	// Also compute intersection with the ground sphere to avoid tracing to the other side of the planet
	float	Distance2GroundKm = ComputeSphereIntersection( _CameraPositionKm, _View, GROUND_ALTITUDE_KM );
	if ( Distance2GroundKm > 0.0 )
		MaxDistanceKm = min( MaxDistanceKm, Distance2GroundKm );

	// Compute hit with the cloud layer
	Hit		H = ComputeLayerIntersection( _CameraPositionKm, _View, MaxDistanceKm );

	// Compute a single mip level at the average hit distance with the cloud
	// We prefer doing that only once rather than at every ray marching step because using a log2() is quite expensive
	float	CloudMipLevel = ComputeNoiseMipLevel( 0.5 * (H.HitDistanceCloudInKm + H.HitDistanceCloudOutKm) );

// Compute the amount of 3D noise voxels seen in a single image pixel
// float3	PositionKm = CameraPositionKm + View * H.HitDistanceCloudInKm;
// float	Distance2CameraKm = length( PositionKm - CameraPositionKm );
// float	PixelSizeKm = 2.0 * Distance2CameraKm * CameraData.x * _BufferInvSize.y;	// Vertical size of a pixel in kilometers at the distance where we're sampling the texture
// 
// float	EncompassedVoxels = _InvVoxelSizeKm * PixelSizeKm;							// This is the amount of voxels we can see in that pixel
// float	MipLevel = log2( EncompassedVoxels );										// And the miplevel we need to sample the texture with...
// 
// PS_OUT	Merde;
// Merde.InScattering = Merde.ExtinctionZ = 0.125 * MipLevel;
// return Merde;

// 	// Compute potential intersection with earth's shadow
// 	float	HeightKm = EARTH_RADIUS + _WorldUnit2Kilometer * CameraPosition.y;
// 	float	HitDistanceKm = _WorldUnit2Kilometer * Distance2Pixel;
// 	float3	CurrentPosition = float3( 0.0, HeightKm, 0.0 );
 
 	float2	EarthShadowDistances = ComputeEarthShadowDistances( _CameraPositionKm, _View );

	// Compute light phases
	float	CosTheta = dot( _View, _SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	OneMinusG = 1.0 - _MiePhaseAnisotropy;
	float	PhaseMie = OneMinusG * OneMinusG / pow( abs(1.0 + _MiePhaseAnisotropy*_MiePhaseAnisotropy - 2.0 * _MiePhaseAnisotropy * CosTheta), 1.5 );
	float2	Phases = INV_4PI * float2( PhaseRayleigh, PhaseMie );

	// Trace
	_InScattering = 0.0;
	_Extinction = 1.0;
	_CloudOpticalDepth = 0.0;

// TraceCloud( _CameraPositionKm, _View, H.HitDistanceCloudInKm, H.HitDistanceCloudOutKm, _Extinction, _InScattering, EarthShadowDistances, Phases, _AmbientSkyColor, _AmbientSkyColorNoCloud, CloudMipLevel, STEPS_COUNT_CLOUDS );
// return;

	if ( H.bTraceAirBefore )
	{
		if ( H.bTraceAirBeforeAccurate )
			TraceAir( _CameraPositionKm, _View, H.HitDistanceStartKm, H.HitDistanceCloudInKm, _Extinction, _InScattering, EarthShadowDistances, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( _CameraPositionKm, _View, H.HitDistanceStartKm, H.HitDistanceCloudInKm, _Extinction, _InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR );
	}

	if ( H.bTraceCloud )
		_CloudOpticalDepth = TraceCloud( _CameraPositionKm, _View, H.HitDistanceCloudInKm, H.HitDistanceCloudOutKm, _Extinction, _InScattering, EarthShadowDistances, Phases, _AmbientSkyColor, _AmbientSkyColorNoCloud, CloudMipLevel, STEPS_COUNT_CLOUDS );
	else
		_CloudOpticalDepth = 1.0;

	if ( H.bTraceAirAfter )
	{
		if ( H.bTraceAirAfterAccurate )
			TraceAir( _CameraPositionKm, _View, H.HitDistanceCloudOutKm, H.HitDistanceEndKm, _Extinction, _InScattering, EarthShadowDistances, Phases, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( _CameraPositionKm, _View, H.HitDistanceCloudOutKm, H.HitDistanceEndKm, _Extinction, _InScattering, EarthShadowDistances, Phases, false, STEPS_COUNT_AIR );
	}
}

// ===================================================================================
// Main cloud computation
// This shader outputs 2 colors:
//	Color0 = InScattered energy
//	Color1 = Extinction
//
struct PS_OUT
{
	float4	InScattering	: SV_TARGET0;
	float4	ExtinctionZ		: SV_TARGET1;
};

PS_OUT	PS_Render( PS_IN _In )
{
	// Compute intersections with ground & cloud planes
	float3	CameraView = float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * _BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * _BufferInvSize.y), 1.0 );
	float	CameraViewLength = length( CameraView );
			CameraView /= CameraViewLength;
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
	float	Z = ReadDownscaledDepth( _In.Position.xy );

	// Render sky
	PS_OUT	Out;
	Out.ExtinctionZ.w = Z;	// Store the Z at which we computed the cloud
	RenderSky( CameraPositionKm, View, Z, CameraViewLength, _In.AmbientSkyColor, _In.AmbientSkyColorNoCloud, Out.ExtinctionZ.xyz, Out.InScattering.xyz, Out.InScattering.w );

// Out.InScattering = 0.0;
// Out.ExtinctionZ.xyz = float3( _In.Position.xy * _BufferInvSize, 0.0 );
// Out.InScattering = float4( 1, 0, 0, 0 );
// Out.ExtinctionZ = float4( 0, 0, 0, 0 );

	return Out;
}

// ===================================================================================
// Combination of in-scattering and extinction buffers with the source buffer
Texture2D	_SourceBuffer;
float3		_VolumeBufferInvSize;
Texture2D	_VolumeTextureInScattering;
Texture2D	_VolumeTextureExtinction;

float		_BilateralThreshold = 10.0;
float2		_RefinementZThreshold = float2( 50.0, 0.0 );

float4	PS_Combine( PS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * _BufferInvSize;
	float4	SourceColor = _SourceBuffer.SampleLevel( LinearClamp, UV, 0 );

	float3	CameraView = float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * _BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * _BufferInvSize.y), 1.0 );
	float	CameraViewLength = length( CameraView );
			CameraView /= CameraViewLength;
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
	float	Z = ReadDepth( _In.Position.xy );

	/////////////////////////////////////////////////////////////
	// Jitter UVs based on clouds' pseudo-ZBuffer
// float	CloudDistanceKm = _VolumeTextureInScattering.SampleLevel( NearestClamp, UV, 0 ).w;	// This is roughly the distance to the cloud
// float3	CloudPositionKm = CameraPositionKm + CloudDistanceKm * View;
// float3	UVW = _JitterNoiseSize * CloudPositionKm;
// float2	NoiseValue  = 2.0 * _NoiseTexture0.SampleLevel( VolumeSampler, UVW, 0.0 ).xy - 1.0;
// 		NoiseValue += _NoiseTexture0.SampleLevel( VolumeSampler, 6.0 * UVW, 0.0 ).xy - 0.5;
// 
// float	ScreenHalfSizeKm = CameraData.y * CloudDistanceKm;		// Height of the screen in kilometers at cloud distance
// float	Jitter2D = _SamplingJitterAmplitude / ScreenHalfSizeKm;	// Projected 2D jitter
// UV += Jitter2D * NoiseValue;


	/////////////////////////////////////////////////////////////
	// Sample cloud
#if 1	// BOURRIN SAMPLING
	float4	InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 );
	float4	ExtinctionZ = _VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );

#elif 0	// ACCURATE REFINEMENT
	float2	DownscaledPosition = 0.5 + floor( 0.25 * (_In.Position.xy - 0.5) );
//return _In.Position.x == 1023.5 ? float4( 1, 0, 0, 0 ) : float4( 0, 0, 0, 0 );
	float2	DownscaledUV = DownscaledPosition * 4.0 * _BufferInvSize;
//return float4( DownscaledUV, 0, 0 );

//float4	ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV, 0 );
//return float4( ExtinctionZ.xy, 0, 0 );
//return ExtinctionZ.x == 1.5 ? float4( 1, 0, 0, 0 ) : float4( 0, 0, 0, 0 );

//	float	CloudZ = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV, 1.0 ).x;
	float	CloudZ = _DownscaledZBuffer.SampleLevel( LinearClamp, UV, 1.0 ).x;
//return DownscaledZ == 1.5 ? float4( 1, 0, 0, 0 ) : float4( 0, 0, 0, 0 );

//return float4( 100.0 * abs( ExtinctionZ.xy - DownscaledPosition ), 0, 0 );
//return float4( 100.0 * abs( ExtinctionZ.xy - UV ), 0, 0 );
//return 100.0 * abs( DownscaledZ - DownscaledPosition.y );

	float4	InScatteringOpticalDepth;
	float4	ExtinctionZ;

	if ( abs( Z - CloudZ ) > _RefinementZThreshold.x )
	{	// Refinement
		RenderSky( CameraPositionKm, View, Z, CameraViewLength, ExtinctionZ.xyz, InScatteringOpticalDepth.xyz, InScatteringOpticalDepth.w );

// DEBUG REFINEMENT
if ( _RefinementZThreshold.y > 0.0 )
{
	ExtinctionZ = 0.0;
	InScatteringOpticalDepth = float4( 10, 0, 0, 0 );
}
	}
	else
	{	// Easy sampling
		InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 );
		ExtinctionZ = _VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );
	}

#else	// "SMART" INTERPOLATION
	// TODO... I have no "smart" idea right now
	float2	DownscaledPosition = 0.5 + floor( 0.25 * (_In.Position.xy - 0.5) );
	float3	dUV = float3( 4.0 * _BufferInvSize, 0.0 );
	float2	DownscaledUV = DownscaledPosition * dUV.xy;

// 	float4	Extinction00 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV, 0 );
// 	float4	Extinction01 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
// 	float4	Extinction10 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
// 	float4	Extinction11 = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
// 	float4	InScattering00 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV, 0 );
// 	float4	InScattering01 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
// 	float4	InScattering10 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
// 	float4	InScattering11 = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
// 
// 	float	Z00 = Extinction00.w;
// 	float	Z01 = Extinction01.w;
// 	float	Z10 = Extinction10.w;
// 	float	Z11 = Extinction11.w;

	float	Z00 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV, 0 ).x;
	float	Z01 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 ).x;
	float	Z10 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 ).x;
	float	Z11 = _DownscaledZBuffer.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 ).x;

	float	DZ00 = abs(Z-Z00);
	float	DZ01 = abs(Z-Z01);
	float	DZ10 = abs(Z-Z10);
	float	DZ11 = abs(Z-Z11);
	float	MaxDz = max( max( max( DZ00, DZ01 ), DZ10 ), DZ11 );
	float	MinDz = min( min( min( DZ00, DZ01 ), DZ10 ), DZ11 );

	float4	InScatteringOpticalDepth;
	float4	ExtinctionZ;
	if ( MaxDz > _RefinementZThreshold.x )
	{
		if ( DZ00 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV, 0 );
		}
		else if ( DZ01 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xz, 0 );
		}
		else if ( DZ10 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.zy, 0 );
		}
		else if ( DZ11 == MinDz )
		{
			InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
			ExtinctionZ = _VolumeTextureExtinction.SampleLevel( NearestClamp, DownscaledUV + dUV.xy, 0 );
		}
	}
	else
	{	// Easy sampling
		InScatteringOpticalDepth = _VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 );
		ExtinctionZ = _VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );
	}
#endif


/////////////////////////////////////////////////////////////
// DEBUG
float2	DebugUV = UV / 0.2;
		DebugUV.y -= 4.0;
if ( DebugUV.x > 0.0 && DebugUV.x < 1.0 && DebugUV.y > 0.0 && DebugUV.y < 1.0 )
{
//	return float4( DebugUV, 0, 0 );
 	return _DeepShadowMap0.SampleLevel( LinearClamp, DebugUV, 0.0 );	// Display the shadow map
//	return _SkyLightProbe.SampleLevel( LinearClamp, DebugUV, 0.0 );		// Display the ambient sky probe
//	return _TexSHConvolution.SampleLevel( LinearClamp, DebugUV, 0.0 );		// Display the ambient sky probe
//	return float4( DebugUV, 0, 0 );

// 	float2	PhiTheta = _SkyProbeAngles.xy + DebugUV * _SkyProbeAngles.zw;
// 	float3	View = float3( cos( PhiTheta.x ) * sin( PhiTheta.y ), cos( PhiTheta.y ), sin( PhiTheta.x ) * sin( PhiTheta.y ) );
// 
// 	const float	f0 = 0.28209479177387814347403972578039;		// 0.5 / Sqrt(PI);
// 	const float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0
// 
// 	return float4( f0,
// 		-f1 * View.x,
// 		 f1 * View.y,
// 		-f1 * View.z );
}

//// if ( UV.x < 0.5 )
// 	return float4( InScatteringOpticalDepth.xyz, 0 );
// else
// //	return ExtinctionZ;
// //	return InScatteringOpticalDepth.w;
// 	return exp( -_VolumeTextureInScattering.SampleLevel( NearestClamp, UV, 0 ).w );

// Display the clouds' "ZBuffer"
//if ( UV.x > 0.5 )
//  	return 0.01 * _VolumeTextureInScattering.SampleLevel( NearestClamp, UV, 0 ).w;
//	return 0.001 * _DownscaledZBuffer.SampleLevel( LinearClamp, UV, 1 ).x;
//
// DEBUG
/////////////////////////////////////////////////////////////


//return float4( ExtinctionZ.xyz, 1.0 );


	/////////////////////////////////////////////////////////////
	// Display the Sun as background

	// 	149.5978875 = Distance to the Sun in millions of kilometers
	// 	0.695       = Radius of the Sun in millions of kilometers
	float	CosAngle = 0.9998;	// Cos( SunCoverAngle ) but arbitrary instead of physical computation, otherwise the Sun is really too small
	float	DotSun = dot( View, _SunDirection );

	float	Infinity = CameraData.w-5.0;
	SourceColor += _SunIntensity  * step( Infinity, Z ) * smoothstep( CosAngle, 1.0, DotSun );

	return float4( InScatteringOpticalDepth.xyz + ExtinctionZ.xyz * SourceColor.xyz, SourceColor.w );
}

// ===================================================================================
technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Render() ) );
	}
}

technique10 Combine
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Combine() ) );
	}
}
