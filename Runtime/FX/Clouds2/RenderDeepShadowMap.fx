// This shader computes the deep shadow map
//
#include "../Camera.fx"
#include "../Samplers.fx"

#define RENDER_DSM
#include "CloudSupport.fx"

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

// ===================================================================================
// Renders the deep shadow map
//
struct PS_OUT
{
	float4	Thickness0 : SV_TARGET0;
#if defined(DEEP_SHADOW_MAP_HI_RES)
	float4	Thickness1 : SV_TARGET1;
#endif
};

PS_OUT	PS( VS_IN _In )
{
	PS_OUT	Out;
	float2	UV = _In.Position.xy * _BufferInvSize;

	// Retrieve the world position from the shadow UV
	float2	CloudDistancesKm;
	float3	WorldPositionKm = Shadow2World( UV, CloudDistancesKm );

	// Compute mip level that will depend on the resolution of the deep shadow map
	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
	float	MipLevel = ComputeNoiseMipLevel( CameraPositionKm, WorldPositionKm );
MipLevel = 0.0;

// 	// Check the computed point is not blocked by the Earth... (projected Earth shadow case)
// 	float	DistanceCamera2PlaneKm = dot( _ShadowPlaneCenterKm - CameraPositionKm, _SunDirection );
// 	float	Distance2OtherSide = ComputeSphereIntersectionExit( WorldPositionKm, -_SunDirection, 0.0 );	// Distance where the sun ray exits from the Earth
// 	if ( Distance2OtherSide < DistanceCamera2PlaneKm )
// 	{	// This means the Earth definitely blocks the Sun !
// //		Out.SumDensity0 = Out.SumDensity1 = 1.0;	// Max density !
// 		Out.SumDensity0 = Out.SumDensity1 = float4( 1.0, 0, 0 ,0 );	// Max density !
// 		return Out;
// 	}

	// Compute the shadow vector
	float3	CloudPositionInKm = WorldPositionKm - CloudDistancesKm.x * _SunDirection;	// Position where we enter the cloud (into top sphere)
	float3	CloudPositionOutKm = WorldPositionKm - CloudDistancesKm.y * _SunDirection;	// Position where we exit the cloud (out of bottom sphere)
	float3	ShadowVectorKm = CloudPositionOutKm - CloudPositionInKm;
#if defined(DEEP_SHADOW_MAP_HI_RES)
	ShadowVectorKm *= 0.03125;	// Divide into 32 equal parts
#else
	ShadowVectorKm *= 0.25;		// Divide into 4 equal parts
#endif

	WorldPositionKm = CloudPositionInKm + 0.5 * ShadowVectorKm;	// Start half a step within the cloud layer

	// Accumulate 8 density levels into 2 render targets
	float	HeightInCloudKm;
	float	StepThicknessKm = _CloudSigma_t * _ShadowOpacity * length(ShadowVectorKm);	// Optical thickness of a single step

	// 1st render target
#if defined(DEEP_SHADOW_MAP_HI_RES)
	float4	Density0;
	Density0.x = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.x += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.x += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.x += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.y = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.y += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.y += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.y += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.z = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.z += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.z += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.z += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.w = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.w += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.w += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.w += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;

	// 2nd render target
	float4	Density1;
	Density1.x = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.x += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.x += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.x += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.y = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.y += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.y += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.y += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.z = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.z += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.z += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.z += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density1.w = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;
	Density1.w += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;
	Density1.w += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;
	Density1.w += ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;

	Out.Thickness0 = 0.25 * StepThicknessKm * Density0;
	Out.Thickness1 = 0.25 * StepThicknessKm * Density1;
#else
	float4	Density0;
	Density0.x = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.y = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.z = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density0.w = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;

	Out.Thickness0 = StepThicknessKm * Density0;
#endif

	return Out;
}

// static const int	STEPS_COUNT = 8;
// static const float	INV_STEPS_COUNT = 1.0 / STEPS_COUNT;
// static const float	MAX_MARCH_DISTANCE = 1.0;
// 
// float4	PS2( VS_IN _In ) : SV_TARGET0
// {
// 	PS_OUT	Out;
// 	float2	UV = _In.Position.xy * _BufferInvSize;
// 
// 	// Retrieve the world position from the shadow UV
// 	float2	CloudDistancesKm;
// 	float3	ShadowPlanePositionKm = Shadow2World( UV, CloudDistancesKm );
// 
// 	// Compute mip level that will depend on the resolution of the deep shadow map
//  	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
// // 	float	MipLevel = ComputeNoiseMipLevel( CameraPositionKm, ShadowPlanePositionKm );
// //MipLevel = 0.0;
// 
// 	// Check the computed point is not blocked by the Earth... (projected Earth shadow case)
// 	float	DistanceCamera2PlaneKm = dot( _ShadowPlaneCenterKm - CameraPositionKm, _SunDirection );
// 	float	Distance2OtherSide = ComputeSphereIntersectionExit( ShadowPlanePositionKm, -_SunDirection, 0.0 );	// Distance where the sun ray exits from the Earth
// 	if ( Distance2OtherSide < DistanceCamera2PlaneKm )
// 		return float4( 0.0, 0.001, 0.002, 0.003 );	// This means the Earth definitely blocks the Sun !
// 
// 	// Compute the shadow vector
// 	float3	PositionKm = ShadowPlanePositionKm - CloudDistancesKm.x * _SunDirection;	// Position where we enter the cloud (into top sphere)
// 	float3	EndPositionKm = ShadowPlanePositionKm - CloudDistancesKm.y * _SunDirection;	// Position where we exit the cloud (out of bottom sphere)
// 	float3	View = EndPositionKm - PositionKm;
// 	float	CloudThicknessKm = length( View );
// 	View /= CloudThicknessKm;
// 
// 	float	CurrentDistanceKm = 0.5 * INV_STEPS_COUNT * CloudThicknessKm;
// 	PositionKm += CurrentDistanceKm * View;	// March a little inside the cloud
// 
// 	float	MaxMarchDistanceKm = min( MAX_MARCH_DISTANCE, CloudThicknessKm / STEPS_COUNT );	// We can't take steps longer than this !
// 	float	InvMaxMarchDistanceKm = 1.0 / MaxMarchDistanceKm;
// 
// 	float4	Z = float4( 100.0, 101.0, 102.0, 103.0 );	// By default, we reach the target opacities at +oo)
// 	float	PreviousDistanceKm = 0.0;	// Start from 0 (entering the cloud)
// 	float	PreviousSigma = 0.0;		// Start from 0 (empty space)
// 	float	PreviousExtinction = 1.0;	// Start from full transparency
// 	for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
// 	{
// 		float	Distance2CameraKm = length( PositionKm - CameraPositionKm );
// 		float	MipLevel = ComputeNoiseMipLevel( Distance2CameraKm );
// MipLevel = 0.0;
// 
// 		// Compute new extinction value
// 		float	CurrentSigma = _ShadowOpacity * _CloudSigma_t * ComputeCloudDensity( PositionKm, Distance2CameraKm, MipLevel ).x;
// 		float	OpticalDepth = (CurrentDistanceKm - PreviousDistanceKm) * 0.5 * (CurrentSigma + PreviousSigma);
// 		float	StepExtinction = exp( -OpticalDepth );
// 		float	CurrentExtinction = PreviousExtinction * StepExtinction;
// 
// 		// Check if we reached a target opacity !
// 		float	InvDeltaExtinctions = 1.0 / (CurrentExtinction - PreviousExtinction);
// 		if ( PreviousExtinction >= SHADOW_OPACITIES.x && CurrentExtinction <= SHADOW_OPACITIES.x )
// 			Z.x = lerp( PreviousDistanceKm, CurrentDistanceKm, (SHADOW_OPACITIES.x - PreviousExtinction) * InvDeltaExtinctions );
// 		if ( PreviousExtinction >= SHADOW_OPACITIES.y && CurrentExtinction <= SHADOW_OPACITIES.y )
// 			Z.y = lerp( PreviousDistanceKm, CurrentDistanceKm, (SHADOW_OPACITIES.y - PreviousExtinction) * InvDeltaExtinctions );
// 		if ( PreviousExtinction >= SHADOW_OPACITIES.z && CurrentExtinction <= SHADOW_OPACITIES.z )
// 			Z.z = lerp( PreviousDistanceKm, CurrentDistanceKm, (SHADOW_OPACITIES.z - PreviousExtinction) * InvDeltaExtinctions );
// 		if ( PreviousExtinction >= SHADOW_OPACITIES.w && CurrentExtinction <= SHADOW_OPACITIES.w )
// 			Z.w = lerp( PreviousDistanceKm, CurrentDistanceKm, (SHADOW_OPACITIES.w - PreviousExtinction) * InvDeltaExtinctions );
// 
// 		// Backup current values
// 		PreviousDistanceKm = CurrentDistanceKm;
// 		PreviousSigma = CurrentSigma;
// 		PreviousExtinction = CurrentExtinction;
// 
// 		// March a step
// 		float	MarchDistanceKm = 1.0 / max( InvMaxMarchDistanceKm, CurrentSigma );
// 		CurrentDistanceKm += MarchDistanceKm;
// 		PositionKm += MarchDistanceKm * View;
// 	}
// 
// 	return Z;
// }

// ===================================================================================
// Renders the deep shadow map for the ambient probe with clouds
//
float3	_SkyProbePositionKm;	// Position of the probe in WORLD units
float4	_SkyProbeAngles;		// XY=Phi/Theta Min ZW=Delta Phi/Theta

float4	PS_Probe( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * _BufferInvSize;
	float2	PhiTheta = _SkyProbeAngles.xy + UV * _SkyProbeAngles.zw;
	float2	SinCosPhi; sincos( PhiTheta.x, SinCosPhi.x, SinCosPhi.y );
	float2	SinCosTheta; sincos( PhiTheta.y, SinCosTheta.x, SinCosTheta.y );
	float3	View = float3( SinCosPhi.x * SinCosTheta.x, SinCosTheta.y, SinCosPhi.y * SinCosTheta.x );	// Phi=0 => +Z Phi=PI/2 => +X

	// Compute hit distances
	float	HitDistanceCloudInKm = ComputeSphereIntersectionExit( _SkyProbePositionKm, View, _CloudAltitudeKm.y );	// Enter from the top
	float	HitDistanceCloudOutKm = ComputeSphereIntersectionExit( _SkyProbePositionKm, View, _CloudAltitudeKm.x );	// Exit through bottom

	// Compute mip level that will depend on the resolution of the deep shadow map
	float3	CameraPositionKm = _WorldUnit2Kilometer * Camera2World[3].xyz;
	float	MipLevel = ComputeNoiseMipLevel( CameraPositionKm, _SkyProbePositionKm );
MipLevel = 0.0;

	// Compute the shadow vector
	float3	CloudPositionInKm = _SkyProbePositionKm + HitDistanceCloudInKm * View;		// Position where we enter the cloud (into top sphere)
	float3	CloudPositionOutKm = _SkyProbePositionKm + HitDistanceCloudOutKm * View;	// Position where we exit the cloud (out of bottom sphere)
	float3	ShadowVectorKm = CloudPositionOutKm - CloudPositionInKm;
	ShadowVectorKm *= 0.25;		// Divide into 4 equal parts

	float3	WorldPositionKm = CloudPositionInKm + 0.5 * ShadowVectorKm;	// Start half a step within the cloud layer

	// Accumulate 4 density levels into the render target
	float	HeightInCloudKm;
	float	StepThicknessKm = _CloudSigma_t * _ShadowOpacity * length(ShadowVectorKm);	// Optical thickness of a single step

	float4	Density;
	Density.x = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density.y = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density.z = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;	WorldPositionKm += ShadowVectorKm;
	Density.w = ComputeCloudDensity( WorldPositionKm, length( WorldPositionKm - CameraPositionKm ), HeightInCloudKm, MipLevel ).x;

	return StepThicknessKm * Density;
}

// ===================================================================================
technique10 RenderDeepShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 RenderDeepShadowMapProbe
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Probe() ) );
	}
}
