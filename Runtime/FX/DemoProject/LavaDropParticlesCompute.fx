// This shader animates and displays lava particles
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// Fluid Simulation
// The texture slots are XY=Velocity and Z=Pressure
// The steady state fluid remains at a 0 units altitude on a unit length patch represented by the texture.
// A solid sphere penetrates the liquid in the center, which is simulated by a pressure increase.
//
Texture2D	Fluid0;			// Fluid at time t-2
Texture2D	Fluid1;			// Fluid at time t-1
float3		BufferInvSize;

float		Density;		// Liquid density
float		Viscosity;		// Liquid viscosity
float		SphereVelocity;	// Velocity at which the sphere enters the fluid

// Sphere animation parameters
float		DeltaTime;
float		Time;

// float4	PS_Dynamics( VS_IN _In ) : SV_TARGET0
// {
// 	float2	UV = _In.Position.xy * BufferInvSize.xy;
// 
// // 	// Compute level gradient
// // 	float	LX0 = Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.xz, 0.0 ).x;
// // 	float	LX1 = Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.xz, 0.0 ).x;
// // 	float	LY0 = Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.zy, 0.0 ).x;
// // 	float	LY1 = Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.zy, 0.0 ).x;
// // 	float2	GradLevel = float2( LX1-LX0, LY1-LY0 );
// // 	float2	GradLevelUV = UV + GradLevel * BufferInvSize.xy;
// // 
// // 	// Compute difference in fluid level
// // 	float	FluidLevel0 = FluidLevel0.SampleLevel( NearestClamp, GradLevelUV, 0.0 ).x;
// // 	float	Fluid1 = Fluid1.SampleLevel( NearestClamp, GradLevelUV, 0.0 ).x;
// // 	float	DeltaLevel = Fluid1 - FluidLevel0;
// 
// 	float3	V = Fluid1.SampleLevel( NearestClamp, UV, 0.0 ).xyz;
// 	float3	Vx0 = Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.xz, 0.0 ).xyz;
// 	float3	Vx1 = Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.xz, 0.0 ).xyz;
// 	float3	Vy0 = Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.zy, 0.0 ).xyz;
// 	float3	Vy1 = Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.zy, 0.0 ).xyz;
// 
// 	// Compute sphere position at current time and the level for current pixel
// 	float	SphereRadius = 0.25;
// 	float	SphereHeight = 0.5 - SphereVelocity * Time;
// 	float3	SpherePos = float3( UV - 0.5, SphereHeight );
// 
// 	float	SqDistance2Center = dot( SpherePos.xy, SpherePos.xy ) / (SphereRadius*SphereRadius);
// 	float	SphereLevel = SqDistance2Center < 1.0 ?
// 		SphereHeight - SphereRadius * sqrt( SqDistance2Center ) :
// 		1e4f;
// 
// 	// Here, if sphere level > 0 then the sphere is still above the fluid level
// 	// If sphere level < 0 then the sphere has penetrated the fluid and we must apply pressure...
// 	float2	ExternalForces = 0.0;
// 	if ( SphereLevel < 0.0 )
// 	{
// 		float2	FromCenter = SpherePos.xy * rsqrt( SqDistance2Center );
// 		ExternalForces += saturate( -SphereLevel ) * FromCenter;
// 	}
// 
// //	return SphereLevel;
// //	return float4( abs( ExternalForces ), 0, 0 );
// 
// 	// Compute Navier-Stokes terms
// 	float2	PressureGradient = -float2( Vx1.z - Vx0.z, Vy1.z - Vy0.z ) / Density;
// 
// 	float	SqVx0 = dot( Vx0.xy, Vx0.xy );
// 	float	SqVx1 = dot( Vx1.xy, Vx1.xy );
// 	float	SqVy0 = dot( Vy0.xy, Vy0.xy );
// 	float	SqVy1 = dot( Vy1.xy, Vy1.xy );
// 	float2	ConvectiveAcceleration = -0.5 * float2( SqVx1 - SqVx0, SqVy1 - SqVy0 );
// 
// 	float2	Drag = -Viscosity * (Vx0.xy + Vx1.xy + Vy0.xy + Vy1.xy - 4.0 * V.xy) / Density;
// 
// 	float2	Acceleration = PressureGradient + ConvectiveAcceleration + Drag + ExternalForces;
// 
// 	// Integreate velocity
// 	float2	NewVelocity = V.xy + Acceleration * DeltaTime;
// 
// 	// Advect pressure
// 	float	NewPressure = Fluid1.SampleLevel( LinearClamp, UV - NewVelocity * BufferInvSize.xy, 0.0 ).z;
// 
// 	// Hop !
// 	return float4( NewVelocity, NewPressure, 0.0 );
// }

// Strange
// float	PS_Dynamics( VS_IN _In ) : SV_TARGET0
// {
// 	float2	UV = _In.Position.xy * BufferInvSize.xy;
// 
// 	// Compute current level by transfering neighbor level difference
// 	float	Level = Fluid0.SampleLevel( NearestClamp, UV, 0.0 ).x;
// 
// 	float	NeighborLevel0 = 0.25 * (
// 		Fluid0.SampleLevel( NearestClamp, UV - BufferInvSize.xz, 0.0 ).x +
// 		Fluid0.SampleLevel( NearestClamp, UV + BufferInvSize.xz, 0.0 ).x +
// 		Fluid0.SampleLevel( NearestClamp, UV - BufferInvSize.zy, 0.0 ).x +
// 		Fluid0.SampleLevel( NearestClamp, UV + BufferInvSize.zy, 0.0 ).x);
// 
// 	float	NeighborLevel1 = 0.25 * (
// 		Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.xz, 0.0 ).x +
// 		Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.xz, 0.0 ).x +
// 		Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.zy, 0.0 ).x +
// 		Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.zy, 0.0 ).x);
// 
// 	Level += Density * NeighborLevel1 - Viscosity * NeighborLevel0;
// 
// 	// Compute sphere position at current time and the level for current pixel
// 	float	SphereRadius = 0.25;
// 	float	SphereHeight = 1.0 - SphereVelocity * Time;
// 	float3	SpherePos = float3( UV - 0.5, SphereHeight );
// 
// 	float	SqDistance2Center = dot( SpherePos.xy, SpherePos.xy ) / (SphereRadius*SphereRadius);
// 	float	SphereLevel = SqDistance2Center < 1.0 ?
// 							max( -5.0, SphereHeight - SphereRadius * sqrt( SqDistance2Center )) :
// 							1e4f;
// 
// 	// Constrain level to sphere level
// 	return min( Level, SphereLevel );
// }

float4	PS_FluidDynamics( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;

	// Compute level gradient
	float2	GradLevel = float2( Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.xz, 0.0 ).x - Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.xz, 0.0 ).x,
								Fluid1.SampleLevel( NearestClamp, UV + BufferInvSize.zy, 0.0 ).x - Fluid1.SampleLevel( NearestClamp, UV - BufferInvSize.zy, 0.0 ).x );
	float2	GradLevelUV = UV + Density * GradLevel * BufferInvSize.xy;

	// Advect level difference towards gradient
	float	DeltaLevel = Fluid1.SampleLevel( LinearWrap, GradLevelUV, 0.0 ).x - Fluid0.SampleLevel( LinearWrap, GradLevelUV, 0.0 ).x;

	float	PreviousLevel = Fluid1.SampleLevel( NearestClamp, UV, 0.0 ).x;
	float	CurrentLevel = PreviousLevel - Viscosity * DeltaLevel;

	// Compute sphere position at current time and the level for current pixel
	float	SphereRadius = 0.25;
	float	SphereHeight = max( -10.0, 1.0 - SphereVelocity * Time );
	float3	SpherePos = float3( UV - 0.5, SphereHeight );

	float	SqDistance2Center = dot( SpherePos.xy, SpherePos.xy ) / (SphereRadius*SphereRadius);
	float	SphereLevel = SqDistance2Center < 1.0 ?
							SphereHeight - SphereRadius * sqrt( SqDistance2Center ) :
							1e4f;

	// Constrain level to sphere level
	return float4( min( CurrentLevel, SphereLevel ), PreviousLevel, GradLevel );
}

float4	PS_FluidDynamicsDebug( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;

	float4	Value = Fluid1.SampleLevel( LinearClamp, UV, 0.0 );

//	return float4( 0.5 * (1.0 + 100.0 * Value.zw), 0.0, 0.0 );

	return float4( 0.5 * (1.0 + 0.4 * Value.xxx), 0.0 );
//	return float4( 0.5 * (1.0 + Value.xx), Value.z, 0.0 );
}

// ===================================================================================
// Particles Simulation
//
Texture2D	LastPositions;
Texture2D	LastVelocities;

float		Mass;
float		FluidForce;
float		SolidTime;
float		SolidRate;

struct PS_OUT
{
	float4	Position : SV_TARGET0;
	float4	Velocity : SV_TARGET1;
};

PS_OUT	PS_ParticleDynamics( VS_IN _In )
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;

	// Retrieve last particle's position & velocity
	float4	LastPosition = LastPositions.SampleLevel( NearestClamp, UV, 0.0 );
	float4	LastVelocity = LastVelocities.SampleLevel( NearestClamp, UV, 0.0 );

	float	LastHeat = LastPosition.w;
	float	LastHeatVelocity = LastVelocity.w;

	// Retrieve fluid level and gradient at current position
	float2	FluidUV = LastPosition.xz;
	float4	FluidValue = Fluid1.SampleLevel( LinearClamp, FluidUV, 0.0 );

	float	PreviousFluidLevel = FluidValue.y;
	float	CurrentFluidLevel = FluidValue.x;
	float2	FluidLevelGradient = FluidValue.zw;

	// Compute acceleration due to fluid motion
	float3	Force = FluidForce * float3( -FluidLevelGradient.x, 50.0 * (CurrentFluidLevel - PreviousFluidLevel), -FluidLevelGradient.y );
	float4	Acceleration = float4( Force, 1.0 * length(Force) );

	// Add gravity & lava cooling
	if ( LastPosition.y > 0.0 )
		Acceleration += Mass * float4( 0.0, -1.0, 0.0, -1.0 );

	// Also, account for solidification
	Acceleration.xyz *= saturate( SolidRate * (SolidTime - Time) );

	// Integrate
	PS_OUT	Out;
	Out.Velocity = LastVelocity + DeltaTime * Acceleration;

	// Also, account for solidification
//	Out.Velocity *= saturate( SolidRate * (SolidTime - Time) );
	Out.Velocity.xyz *= saturate( 1.0 * (10.0 - Time) );

	Out.Position = LastPosition + DeltaTime * Out.Velocity;

	return Out;
}

// ===================================================================================
// Particles Buffer Blur
//
Texture2D	SourceBuffer;
float		ZThreshold;		// Threshold above which a sample is discarded
float		NormalSmooth;
float2		BlurDirection;

float4	PS_Blur( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	float4	Zc = SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 );

	float4	SumZ = Zc;
	float	SumWeights = 1.0;

	// Sample neighbors
	float4	Z = SourceBuffer.SampleLevel( NearestClamp, UV + 1.0 * BlurDirection, 0.0 ); if ( abs(Z.w-Zc.w) < ZThreshold ) { SumZ += Z; SumWeights++; }
			Z = SourceBuffer.SampleLevel( NearestClamp, UV - 1.0 * BlurDirection, 0.0 ); if ( abs(Z.w-Zc.w) < ZThreshold ) { SumZ += Z; SumWeights++; }
			Z = SourceBuffer.SampleLevel( NearestClamp, UV + 2.0 * BlurDirection, 0.0 ); if ( abs(Z.w-Zc.w) < ZThreshold ) { SumZ += Z; SumWeights++; }
			Z = SourceBuffer.SampleLevel( NearestClamp, UV - 2.0 * BlurDirection, 0.0 ); if ( abs(Z.w-Zc.w) < ZThreshold ) { SumZ += Z; SumWeights++; }
			Z = SourceBuffer.SampleLevel( NearestClamp, UV + 3.0 * BlurDirection, 0.0 ); if ( abs(Z.w-Zc.w) < ZThreshold ) { SumZ += Z; SumWeights++; }
			Z = SourceBuffer.SampleLevel( NearestClamp, UV - 3.0 * BlurDirection, 0.0 ); if ( abs(Z.w-Zc.w) < ZThreshold ) { SumZ += Z; SumWeights++; }

	return SumZ / SumWeights;
}

// ===================================================================================
// Particles Final Compositing
//
TextureCube		EnvMap;
Texture2DArray	RockTexture;

float4	PS_Display( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;

	float4	Zc = SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 );
// 	if ( Zc.w > 99.0 )
// 		return 0.0;
	clip( 999.0 - Zc.w );

//	return 4.0 * Zc.z;

	// Retrieve world position
	float3	View = float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0 ), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 );
	float3	Position = mul( float4( Zc.w * View, 1.0 ), Camera2World ).xyz;

	// Retrieve normal
	float3	Normal = float3( Zc.xy, sqrt( 1.0 - saturate( dot(Zc.xy,Zc.xy) ) ) );
	Normal = mul( float4( Normal, 0.0 ), Camera2World ).xyz;	// Transform into world space

	// Sample multiple times in 3 directions and mix
	float	TileFactor = 1.0;
	float	FusionFactor = saturate( 100.0 * Zc.z );

	float2	UVx = TileFactor * Position.yz;
	float3	Cx = lerp( RockTexture.Sample( LinearWrap, float3( UVx, 0 ) ), RockTexture.Sample( LinearWrap, float3( UVx, 1 ) ), FusionFactor ).xyz;
	float2	UVy = TileFactor * Position.xz;
	float3	Cy = lerp( RockTexture.Sample( LinearWrap, float3( UVy, 0 ) ), RockTexture.Sample( LinearWrap, float3( UVy, 1 ) ), FusionFactor ).xyz;
	float2	UVz = TileFactor * Position.xy;
	float3	Cz = lerp( RockTexture.Sample( LinearWrap, float3( UVz, 0 ) ), RockTexture.Sample( LinearWrap, float3( UVz, 1 ) ), FusionFactor ).xyz;

	float3	MixColorNormal = Cx * abs(Normal.x) + Cy * abs(Normal.y) + Cz * abs(Normal.z);
	float3	MixColor = lerp( Cy, MixColorNormal, Position.y );

//	return float4( Position, 0.0 );
// 	return float4( Cy, 0.0 );
 	return float4( MixColor, 0.0 );
	return float4( Normal, 0.0 );

//	return 0.08 * Zc.wwww;
//	return float4( 0.5 * (1.0+SourceBuffer.SampleLevel( LinearClamp, UV, 0.0 ).xyz), 0.0 );
//	return float4( 0.5 * (1.0+Zc.xyz), 0.0 );
	return EnvMap.Sample( LinearClamp, Normal );
}

// ===================================================================================
technique10 FluidDynamics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_FluidDynamics() ) );
	}
}

technique10 FluidDynamicsDebug
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_FluidDynamicsDebug() ) );
	}
}

technique10 ParticleDynamics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ParticleDynamics() ) );
	}
}

technique10 Blur
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Blur() ) );
	}
}

technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Display() ) );
	}
}
