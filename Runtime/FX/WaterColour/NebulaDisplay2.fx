﻿// This shader displays the propagated light volume using small opaque particles
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"
#include "GBufferSupport.fx"

static const int	STEPS_COUNT = 64;

float2		BufferInvSize;

float3		CellOffset = float3( 64, 64, 32 );			// The offset to reach the center cell from the origin)
float		Nebula2WorldRatio = 0.1;

float3		LightPosition = float3( 0.0, 0.0, 0.0 );	// Light position in WORLD space
float		LightRadius = 2.5;							// Light radius in WORLD space
float		LightFlux = 1.0;							// Light source flux

float		Sigma_s;									// Scattering coefficient
float		Sigma_t;									// Extinction coefficient = Scattering + Absorption coefficient
float		Sigma_Aerosols = 0.03;
float		ScatteringAnisotropy = 0.7;					// Scattering direction in [-1,+1]

float		DirectionalFactor = 1.0;					// Factor to apply to directional energy
float		IsotropicFactor = 1.0;						// Factor to apply to isotropic energy
float		AerosolsFactor = 1.0;						// Factor to apply to energy scattered by aerosols


float3		SliceInvSize;
Texture3D	SourceDiffusionTexture;

struct VS_IN
{
	float4	Position		: SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

// Computes the intersection with the computed box
//
const static float3	PlaneNormals[] =
{
	float3( 0, 0, +1 ),
	float3( 0, 0, -1 ),
	float3( 0, +1, 0 ),
	float3( 0, -1, 0 ),
	float3( +1, 0, 0 ),
	float3( -1, 0, 0 ),
};

const static float3	PlaneTangents[] =
{
	float3( +1, 0, 0 ),
	float3( -1, 0, 0 ),
	float3( +1, 0, 0 ),
	float3( -1, 0, 0 ),
	float3( 0, 0, +1 ),
	float3( 0, 0, -1 ),
};

const static float3	PlaneBiTangents[] =
{
	float3( 0, +1, 0 ),
	float3( 0, +1, 0 ),
	float3( 0, 0, +1 ),
	float3( 0, 0, +1 ),
	float3( 0, +1, 0 ),
	float3( 0, +1, 0 ),
};

bool	ComputeIntersection( float3 _CameraPosition, float3 _CameraView, out float3 _In, out float3 _Out )
{
	float3	HalfSize = float3( 64.5, 32.5, 64.5 ) * Nebula2WorldRatio;
	float3	Normalizer = 1.0 / HalfSize;

	float3	Pos = _CameraPosition * Normalizer;
	float3	View = _CameraView * Normalizer;

	float	MinHitDistance = 1e38;
	float	MaxHitDistance = -1e38;
	for ( int SideIndex=0; SideIndex < 6; SideIndex++ )
	{
		float3	PlaneNormal = PlaneNormals[SideIndex];
		float3	PlaneOffset = PlaneNormal;

		float	HitDistance = dot( PlaneOffset - Pos, PlaneNormal ) / dot( View, PlaneNormal );
		float3	HitPosition = Pos + HitDistance * View;
		if ( abs(dot( PlaneTangents[SideIndex], HitPosition )) > 1.0 || abs(dot( PlaneBiTangents[SideIndex], HitPosition )) > 1.0 )
			continue;	// Outside of box quad...

		MinHitDistance = min( MinHitDistance, HitDistance );
		MaxHitDistance = max( MaxHitDistance, HitDistance );
	}

	MinHitDistance = max( 0.0, MinHitDistance );

	if ( MinHitDistance >= MaxHitDistance )
		return false;

	_In = _CameraPosition + MinHitDistance * _CameraView;
	_Out = _CameraPosition + MaxHitDistance * _CameraView;

	return true;
}

// ===================================================================================

float		ScatteringAnisotropyForward = 0.8;
float		PhaseWeightForward = 0.2;
float		ScatteringAnisotropyBackward = -0.7;
float		PhaseWeightBackward = 0.1;
float		ScatteringAnisotropySide = -0.4;
float		PhaseWeightSide = 1.5;


float4	PS( VS_IN _In ) : SV_TARGET0
{
	float3	CameraView = normalize( float3( (2.0 * _In.Position.xy * BufferInvSize - 1.0) * float2( CameraData.y, 1.0 ) * CameraData.x, 1.0 ) );
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPosition = Camera2World[3].xyz;
	float3	In, Out;
	if ( !ComputeIntersection( CameraPosition, View, In, Out ) )
		return 0.0;

	// Convert In/Out positions into cell indices in our 3D volume
	In = In.xzy / Nebula2WorldRatio;
	Out = Out.xzy / Nebula2WorldRatio;
//return float4( In / 64.0, 1.0 );

	View = View.xzy;

	float3	Step = Out - In;
	Step /= (STEPS_COUNT+1);
	float	StepSize = length(Step);
	float3	Position = In + 0.5 * Step;
//return float4( Position / 64.0, 1.0 );
//return float4( (Position + 64 * Step + CellOffset) * SliceInvSize, 1.0 );

	float	Extinction = 1.0;
	float	ExtinctionAerosols = 1.0;
	float	EnergyDirectional = 0.0;
	float	EnergyIsotropic = 0.0;
	float	EnergyAerosols = 0.0;
	for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
	{
		float3	UVW = (Position + CellOffset) * SliceInvSize;
		float3	Flux = SourceDiffusionTexture.SampleLevel( LinearWrap, UVW, 0 ).xyz;

		// Compute light phase
		// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick's equivalent to Henyey-Greenstein
 		float3	ToLight = LightPosition - Position;
		float	Distance2Light = length( ToLight );
		ToLight /= Distance2Light;
		float	DotLightView = -dot( View, ToLight );

			// Forward phase
		float	Den = 1.0 / (1.0 + ScatteringAnisotropyForward * DotLightView);
		float	PhaseForward = (1.0 - ScatteringAnisotropyForward*ScatteringAnisotropyForward) * Den * Den;
			// Backward phase
				Den = 1.0 / (1.0 + ScatteringAnisotropyBackward * DotLightView);
		float	PhaseBackward = (1.0 - ScatteringAnisotropyBackward*ScatteringAnisotropyBackward) * Den * Den;
			// Side phase
//		DotLightView = 0.4 + 0.6 * DotLightView;	// Add bias
		float	PhaseSide = saturate( pow( sqrt(1.0 - 0.8 * DotLightView*DotLightView), ScatteringAnisotropySide ) );

		float	PhaseAmbient = saturate( PhaseWeightSide * PhaseSide );
		float	PhaseDirect = saturate( 0.2 * PhaseAmbient + PhaseWeightForward * PhaseForward + PhaseWeightBackward * PhaseBackward );


// 		// Compute phase with light, we use Shlick's equivalent to Henyey-Greenstein
// 		//	Fr(θ) = (1-k²) / (1+kcos(θ))^2
// 		float3	ToLight = LightPosition - Position;
// 		float	Distance2Light = length( ToLight );
// 		ToLight /= Distance2Light;
// 		float	Dot = dot( View, ToLight );
// 		float	Den = 1.0 / (1.0 - ScatteringAnisotropy * Dot);
// 		float	Phase = (1.0 - ScatteringAnisotropy*ScatteringAnisotropy) * Den * Den;
// 		Phase = saturate( Phase );
// //Phase = 1.0;

		// Accumulate extinction
		Extinction *= exp( -Flux.z * (Sigma_t + Sigma_Aerosols) * StepSize );
		ExtinctionAerosols *= exp( -Sigma_Aerosols * StepSize );
		float	CurrentSigmaS = Flux.z * Sigma_s;// + Sigma_Aerosols;

		// Accumulate directional energy
		EnergyDirectional += Extinction * PhaseDirect * Flux.x * CurrentSigmaS * StepSize;

		// Accumulate isotropic energy
		EnergyIsotropic += Extinction * PhaseAmbient * Flux.y * CurrentSigmaS * StepSize;

		// Aerosols
		float	PhaseRayleigh = 0.5 + 0.5 * DotLightView*DotLightView;
		float	IncomingRadiance = Flux.x;// * exp( -Sigma_Aerosols * Distance2Light );
		EnergyAerosols += ExtinctionAerosols * PhaseRayleigh * IncomingRadiance * Sigma_Aerosols * StepSize;

		Position += Step;
	}

	float3	DirectionalLightColor = DirectionalFactor * 0.2.xxx;
	float3	IsotropicLightColor = IsotropicFactor * 0.05 * float3( 0.5294118, 0.807843149, 0.921568632 );

//	return float4( Extinction.xxx, 1.0 );
//	return float4( DirectionalFactor * EnergyDirectional.xxx, 1.0 );
//	return float4( IsotropicFactor * EnergyIsotropic.xxx, 1.0 );
//	return float4( AerosolsFactor * EnergyAerosols.xxx, 1.0 );
	return float4( AerosolsFactor * EnergyAerosols + EnergyDirectional * DirectionalLightColor + EnergyIsotropic * IsotropicLightColor, 1.0 - Extinction );
}

// ===================================================================================
Texture2D	VolumeTexture;

float4	PS2( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	UV.y = 1.0 - UV.y;
	return VolumeTexture.SampleLevel( LinearClamp, UV, 0 );
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 Splat
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}