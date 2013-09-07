// This shader performs a motion blur on the material buffer using the velocity buffer
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"
#include "MotionBlurSupport.fx"

#define STEPS_COUNT 16

Texture2D	SourceVelocityTexture;
float3		SourceVelocityTextureInvSize;

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// Project WORLD velocities
float2	PS_Project( VS_IN _In ) : SV_TARGET0
{
	// Read velocity & depth of the current pixel
	float2	UV = _In.Position.xy * SourceVelocityTextureInvSize.xy;
	float3	WorldVelocity = SourceVelocityTexture.SampleLevel( NearestClamp, UV, 0 ).xyz;
	float	Depth = ReadDepth( UV );

	// Rebuild WORLD position from depth
	float3	View = float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 );
	float3	WorldPosition = mul( float4( View * Depth, 1.0 ), Camera2World ).xyz;

	// Add camera velocity to the pixel's own velocity
	WorldVelocity += ComputeVelocity( WorldPosition );
//	WorldVelocity += float3( 0.1, 0.0, 0.0 );
//	return float4( WorldVelocity, 0.0 );

	// Project into 2D
	float4	VelocityProj = mul( float4( WorldPosition + WorldVelocity, 1.0 ), World2Proj );
	VelocityProj /= VelocityProj.w;
	float2	Velocity = 0.5 * float2( 1.0 + VelocityProj.x, 1.0 - VelocityProj.y ) - UV;

//	return float2( 1.0, 0.0 );
	return Velocity;
}

// ===================================================================================
// DownSample velocities
float2	PS_DownSample( VS_IN _In ) : SV_TARGET0
{
	float2	UV = 4.0 * _In.Position.xy * SourceVelocityTextureInvSize.xy;
	float2	dUV = 4.0 * SourceVelocityTextureInvSize.xy;
	float2	V00 = SourceVelocityTexture.SampleLevel( LinearClamp, UV + dUV * float2( 0.5, 0.5 ), 0 ).xy;
	float2	V01 = SourceVelocityTexture.SampleLevel( LinearClamp, UV + dUV * float2( 1.5, 0.5 ), 0 ).xy;
	float2	V10 = SourceVelocityTexture.SampleLevel( LinearClamp, UV + dUV * float2( 0.5, 1.5 ), 0 ).xy;
	float2	V11 = SourceVelocityTexture.SampleLevel( LinearClamp, UV + dUV * float2( 1.5, 1.5 ), 0 ).xy;
	return 0.25 * (V00 + V01 + V10 + V11);

// This is a (failed) attempt at keeping only the maximum velocity
// 	float	Dot00 = dot( V00, V00 );
// 	float	Dot01 = dot( V01, V01 );
// 	float	Dot10 = dot( V10, V10 );
// 	float	Dot11 = dot( V11, V11 );
// 
//  	float2	MaxV = V00;
// 	float	MaxDot = Dot00;
// 	if ( Dot01 > MaxDot )
// 	{
// 		MaxV = V01;
// 		MaxDot = Dot01;
// 	}
// 	if ( Dot10 > MaxDot )
// 	{
// 		MaxV = V10;
// 		MaxDot = Dot10;
// 	}
// 	if ( Dot11 > MaxDot )
// 		MaxV = V11;
// 
// 	return MaxV;

// This is a (failed) attempt at expanding velocity vectors in their original direction
// 	float2	Offsets[] =
// 	{
// 		float2( 1.0, 0.0 ),
// 		float2( 0.0, 1.0 ),
// 		float2( -1.0, 0.0 ),
// 		float2( 0.0, -1.0 ),
// 		float2( 1.0, 1.0 ),
// 		float2( -1.0, 1.0 ),
// 		float2( 1.0, -1.0 ),
// 		float2( -1.0, -1.0 ),
// 	};
// 
// 	float2	V = SourceVelocityTexture.SampleLevel( LinearClamp, UV, 0 ).xy;
// 	float	SumWeight = 1.0;
// 
// // 	V += SourceVelocityTexture.SampleLevel( LinearClamp, UV + dUV * float2( 6.0, 0.0 ), 0 ).xy;
// // 	SumWeight += 1.0;
// 
// 	dUV *= 6.0;
// 
// 	[unroll]
// 	for ( int SampleIndex=0; SampleIndex < 8; SampleIndex++ )
// 	{
// 		float2	Offset = Offsets[SampleIndex];
// 		float2	SampleV = SourceVelocityTexture.SampleLevel( LinearClamp, UV + dUV * Offset, 0 ).xy;
// 
// 		float	Weight = saturate( 4.0 * dot( Offset, normalize( SampleV ) ) - 3.0 );
// 		V += Weight * SampleV;
// 		SumWeight += Weight;
// 	}
// 
// 	return V / SumWeight;
}

// ===================================================================================
// Motion blur is performed by averaging pixels' colors along their velocity vector
Texture2D	SourceTexture;
float		BlurSize = 1.0;
float		BlurWeightFactor = 1.0;
float2		VelocityUVScale = 1.0;

float4	PS( VS_IN _In ) : SV_TARGET0
{
	// Read color, velocity & depth of the current pixel
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float3	RGB = GBufferTexture0.SampleLevel( NearestClamp, UV, 0 ).xyz;

// 	float3	WorldVelocity = GBufferTexture3.SampleLevel( NearestClamp, UV * VelocityUVScale, 0 ).xyz;
// 	float	Depth = ReadDepth( UV );
// 
// 	// Rebuild WORLD position from depth
// 	float3	View = float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 );
// 	float3	CameraPosition = View * Depth;
// 	float3	WorldPosition = mul( float4( CameraPosition, 1.0 ), Camera2World ).xyz;
// 
// 	// Add camera velocity to the pixel's own velocity
// 	WorldVelocity += ComputeVelocity( WorldPosition );
// //	return float4( WorldVelocity, 0.0 );
// 
// 	// Project into 2D
// 	float4	VelocityProj = mul( float4( WorldPosition + WorldVelocity, 1.0 ), World2Proj );
// 	VelocityProj /= VelocityProj.w;
// 	float2	Velocity = 0.5 * float2( 1.0 + VelocityProj.x, 1.0 - VelocityProj.y ) - UV;
// //	return float4( 20.0 * abs( Velocity ), 0.0, 0.0 );


	float2	Velocity = SourceVelocityTexture.SampleLevel( LinearClamp, UV * VelocityUVScale, 0 ).xy;
//	return float4( 10.0 * Velocity, 0, 0 );

	// Limit delta-velocity to 2 pixels (for which it's still acceptable)
	float2	dVelocity = Velocity / STEPS_COUNT;
	float	Speed = length(dVelocity);
	dVelocity *= min( Speed, 2.0 * GBufferInvSize.x ) / max( 1e-5, Speed );

	// Perform blur
	float2	dUV = -BlurSize * dVelocity;
	float	SumWeight = 1.0;
	for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
	{
		UV += dUV;
		RGB += GBufferTexture0.SampleLevel( NearestClamp, UV, 0 ).xyz;
		SumWeight += 1.0;
	}

	RGB *= BlurWeightFactor / SumWeight;

	return float4( RGB, 1.0 );
}

// ===================================================================================

technique10 ProjectVelocities
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Project() ) );
	}
}

technique10 DownSampleVelocities
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_DownSample() ) );
	}
}

technique10 MotionBlur
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
