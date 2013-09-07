// Renders sky object as emissive
// This shader operates as a post-process as it renders a quad covering the entire screen
//
// It's an implementation of ""Display of Earth Taking into Account Atmospheric Scattering" (1993)
//  by Nishita et Al. (http://nis-lab.is.s.u-tokyo.ac.jp/~nis/cdrom/sig93_nis.pdf)
//
// We compute both sky color for pixels at infinity as well as scattering and extinction for objects
//	in the scene.
//
// We remind the basic equations below and explain what choices we made to optimize the computation:
//
// =======================
// I(λ,θ) = Io(λ).K.ρ.Fr(θ) / λ^4 
// where :
//	Io is the intensity of incident light
//	K is the molecular density at sea level (constant)
//	θ is the scattering angle (angle between light and view)
//	λ is the wavelength (as it's at the fourth power, shorter wavelength are more scattered than others, hence the blue color of the sky)
//	Fr is the scattering phase function (we're using Rayleigh for air molecules and modified greenstein for aerosols)
//	ρ is the density ratio that is 1 at sea level and decreases with altitude following the formula below :
//
// ρ(h) = exp( -h / H0 )
// where :
//	h is the considered altitude
//	H0 is a scale factor (H0=7994m for air and H0=1200m for aerosols whose density decreases faster than air)
//
// Expression for K is :
// K = 2π² (n² - 1)² / (3 Ns)
// where :
//	n is the index of refraction of air (n=1.000293)
//	Ns is the molecular density of the standard atmosphere (Ns=904.046)
//
// Extinction coefficient is given by :
// β(λ) = 4π K / λ^4
//
// =======================
// Rayleigh scattering for air molecules uses :
// Fr(θ) = 0.75 (1 + cos²(θ))
// K = 2.5e-9
//
// Mie scattering for aerosols uses :
// Fr(θ) = (1-g²) / (1+g²-2gcos(θ))^3/2   <= Henyey Greenstein
// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick equivalent
// K = 2.5e-9
//
// =======================
// Full computation involves integrating scattering along the ray :
// 
// I(λ,θ) = Integral[0,Z]( τ(0,x,λ) * InScattering(x,λ,θ) * dx )
// where :
//	τ(s,s',λ) = exp( -β(λ).Integral[s,s']( ρ(h(l)) dl ) )	is the extinction along the ray from x to x' which we precompute in a table
//	InScattering(x,λ,θ) = L(x,θ) K.ρ.Fr(θ) / λ^4			is the in-scattered energy at x
//	L(x,θ) = Lsun(λ) . τ(x,x',λ)							is the sun's energy attenuated from the top of the atmosphere (x') to the current point (x)
//
// The optimizations lies in precomputing the optical depth in a texture, saving us the trouble
//	of a second order integration.
// We're still left with a standard ray-marching integration though...
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"

const static float	PI = 3.14159265358979;
const static float	EARTH_RADIUS = 6400.0;				// In kilometers
const static float	ATMOSPHERE_RADIUS = 6500.0;			// In kilometers
const static float	WORLD_UNIT_TO_KILOMETER = 0.1;		// 1 World Unit equals XXX kilometers
const static float	MAX_CAMERA_ALTITUDE = 20.0;			// The maximum camera altitude encoded in the extinction texture
const static float3	INV_WAVELENGTHS_POW4 = float3( 5.6020447463324113301354994573019, 9.4732844379230354373782268493533, 19.643802610477206282947491194819 );	// 1/λ^4 

const static int	STEPS_COUNT = 8;					// 8 steps of ray-marching the atmosphere

float2	ScreenInfos;		// XY = 1 / ScreenSize

float	MiePhaseAnisotropy = -0.75;		// Phase anisotropy factor in [-1,+1]
float	K_RAYLEIGH = 1e-5;
float	K_MIE = 1e-5;
float3	ExtinctionCoeffRayleigh = 0.0;	// 4.0 * PI * K_RAYLEIGH / WAVELENGTHS_POW4;
float	ExtinctionCoeffMie = 0.0;		// 4.0 * PI * K_MIE;

float3	SunColor = 1.0.xxx;
float3	SunDirection = float3( 0.0, 1.0, 0.0 );

float	FinalFactor = 1.0;

Texture2D	DensityTexture;
TextureCube	NightSkyCubeMap;
Texture2D	GeometryBuffer;

TextureCube	EnvironmentTestCubeMap0;
TextureCube	EnvironmentTestCubeMap1;


struct VS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position			: SV_POSITION;
	float3	View				: VIEW;
	float3	CameraView			: CAMERA_VIEW;		// The original, untransformed camera view
	float2	UV					: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	// Scale view to actual FOV
	_In.View.xy *= CameraData.x;

	// Transform view ray in WORLD space
	float3	View = mul( float4( _In.View, 0.0 ), Camera2World ).xyz;

	PS_IN	Out;
	Out.Position = _In.Position;
	Out.View = View;
	Out.CameraView = _In.View;
	Out.UV = _In.UV;
	
	return	Out;
}

// Computes the optical depth along the ray
// ρ(s,s') = Integral[s,s']( ρ(h(l)) dl )
//
float4	ComputeOpticalDepth( float3 _ViewPosition, float3 _ViewDirection )
{
	float3	EarthNormal = _ViewPosition;
	float	Altitude = length( EarthNormal );
	EarthNormal /= Altitude;

	// Normalize altitude
	Altitude = (Altitude - EARTH_RADIUS) / (ATMOSPHERE_RADIUS - EARTH_RADIUS);

	// Actual view direction
	float	CosTheta = dot( _ViewDirection, EarthNormal );

	float2	UV = float2( 0.5 * (1.0 - CosTheta), Altitude );
	return DensityTexture.SampleLevel( LinearClamp, UV, 0 );
}

// Computes the in-scattered and attenuated energy because of atmospheric scattering
//	_ViewPosition, the viewer's position in WORLD space
//	_View, the viewer's direction in WORLD space
//	_Distance, the distance of the point to light in WORLD space
//	_InitialColor, an optional initial color that will get attenuated along the ray
//	_Terminator, the terminator value (1 is fully lit, 0 is in Earth's shadow)
//
float3	ComputeSkyColor( float3 _ViewPosition, float3 _View, float _Distance, inout float3 _InitialColor, out float _Terminator )
{
	// Compute camera height & hit distance in kilometers
	float	Height = EARTH_RADIUS + WORLD_UNIT_TO_KILOMETER * _ViewPosition.y;
	float	HitDistance = WORLD_UNIT_TO_KILOMETER * _Distance;

	// Compute phases
	float	CosTheta = dot( _View, SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	PhaseMie = 1.0 / (1.0 + MiePhaseAnisotropy * CosTheta);
			PhaseMie = (1.0 - MiePhaseAnisotropy*MiePhaseAnisotropy) * PhaseMie * PhaseMie;

	// Compute potential intersection with earth's shadow
	float3	CurrentPosition = float3( 0.0, Height, 0.0 );

	_Terminator = 1.0;
	if ( dot( CurrentPosition, SunDirection ) < 0.0 )
	{	// Project current position in the 2D plane normal to the light to test the intersection with the shadow cylinder cast by the Earth
		float3	X = normalize( cross( SunDirection, _View ) );
		float3	Y = cross( X, SunDirection );
		float2	P = float2( dot( CurrentPosition, X ), dot( CurrentPosition, Y ) );
		float2	V = float2( dot( _View, X ), dot( _View, Y ) );
		float	a = dot( V, V );
		float	b = dot( P, V );
		float	c = dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
		float	Delta = b*b - a*c;
		if ( Delta >= 0.0 )
			_Terminator = 1.0 - saturate( (-b+sqrt(Delta)) / (a * HitDistance) );
	}

	// Prepare light plane
	float3	LightPlanePosition = Camera2World[3].xyz + SunDirection * CameraData.w;	// Set the light plane at far clip

	// Ray-march the view ray
	float3	AccumulatedLightRayleigh = 0.0.xxx;
	float3	AccumulatedLightMie = 0.0.xxx;
	float	AccumulatedShadowWeight = 0.0;

	float	StepSize = HitDistance / STEPS_COUNT;
	float3	Step = StepSize * _View;
	float3	ShadowStep = _Distance / STEPS_COUNT * _View;

	CurrentPosition += (STEPS_COUNT-0.5) * Step;	// Start from end point
	float3	CurrentShadowPosition = _ViewPosition + (STEPS_COUNT-0.5) * ShadowStep;

	for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
	{
		// =============================================
		// Sample extinction at current altitude and view direction
		float4	OpticalDepth = ComputeOpticalDepth( CurrentPosition, SunDirection );

		// Retrieve densities
		float	Rho_air = OpticalDepth.x;
		float	Rho_aerosols = OpticalDepth.y;

		// ...and extinctions
		float3	ExtinctionRayleigh = exp( -ExtinctionCoeffRayleigh * Rho_air * StepSize );
		float	ExtinctionMie = exp( -ExtinctionCoeffMie * Rho_aerosols * StepSize );

		// =============================================
		// Perform extinction for previous step's energy
		AccumulatedLightRayleigh *= ExtinctionRayleigh;
		AccumulatedLightMie *= ExtinctionMie;
		_InitialColor *= ExtinctionRayleigh * ExtinctionMie;


		// =============================================
		// Compute volumetric shadow

		// Project current position in the light plane
		float3	CurrentPositionInLightPlane = CurrentShadowPosition - dot( CurrentShadowPosition - LightPlanePosition, SunDirection ) * SunDirection;

		// Project light plane position in camera view
		float4	ProjectedLightPlane = mul( float4( CurrentPositionInLightPlane, 1.0 ), World2Proj );
		ProjectedLightPlane /= ProjectedLightPlane.w;

		// Read depth value at that position
		float2	ShadowUV = 0.5 * (1.0 + ProjectedLightPlane.xy);
				ShadowUV.y = 1.0 - ShadowUV.y;
		float	ShadowDepth = min( CameraData.w, GeometryBuffer.SampleLevel( LinearClamp, ShadowUV, 0 ).z );
//		float	ShadowWeight = ShadowDepth / CameraData.w;
		float	ShadowWeight = 1.0;
		AccumulatedShadowWeight += ShadowWeight;

		// =============================================

		// Retrieve sun light attenuated when passing through the atmosphere
		ExtinctionRayleigh = exp( -ExtinctionCoeffRayleigh * OpticalDepth.z );
		ExtinctionMie = exp( -ExtinctionCoeffMie * OpticalDepth.w );
		float3	Light = ShadowWeight * SunColor * (ExtinctionRayleigh * ExtinctionMie);

		// Compute in-scattering
		float3	InScatteringRayleigh = Light * K_RAYLEIGH * PhaseRayleigh * Rho_air * INV_WAVELENGTHS_POW4 * ExtinctionRayleigh;
		float3	InScatteringMie = Light * K_MIE * PhaseMie * Rho_aerosols * ExtinctionMie;

		// Accumulate light
		AccumulatedLightRayleigh += InScatteringRayleigh;
		AccumulatedLightMie += InScatteringMie;

		// March
		CurrentPosition -= Step;
		CurrentShadowPosition -= ShadowStep;
	}

	return _Terminator * (AccumulatedLightRayleigh + AccumulatedLightMie) * HitDistance / (AccumulatedShadowWeight * STEPS_COUNT);
}

float4 PS_Sky( PS_IN _In ) : SV_TARGET0
{
	float3	ViewPosition =  Camera2World[3].xyz;
 	float3	View = normalize( _In.View );

	// Compute view ray intersection with the upper atmosphere
	float	CameraHeight = WORLD_UNIT_TO_KILOMETER * ViewPosition.y;	// Relative height from sea level
	float	D = CameraHeight + EARTH_RADIUS;
	float	b = D * View.y;
	float	c = D*D-ATMOSPHERE_RADIUS*ATMOSPHERE_RADIUS;
	float	Delta = sqrt( b*b-c );
	float	HitDistance = Delta - b;	// Distance at which we hit the upper atmosphere (in kilometers)
//	return float4( 0.001 * HitDistance.xxx, 1.0 );

	// Compute sky color
// 	149.5978875 = Distance to the Sun in millions of kilometers
// 	0.695       = Radius of the Sun in millions of kilometers
//	float	CosAngle = 0.99998926605350230740344753126817;	// Cos( SunCoverAngle )
	float	CosAngle = 0.999;	// Cos( SunCoverAngle )

	float	DotSun = dot( View, SunDirection );
	float3	InitialColor = DotSun >= CosAngle
						? SunColor		// Either sunlight...
						: 0.0.xxx;		// ...or the black of space !
	float	Terminator;
	float3	SkyColor = ComputeSkyColor( ViewPosition, View, HitDistance / WORLD_UNIT_TO_KILOMETER, InitialColor, Terminator );

	return float4( FinalFactor * (InitialColor + SkyColor), 1.0 );
}

float4 PS_AtmosphericPerspective( PS_IN _In ) : SV_TARGET0
{
	float3	ViewPosition =  Camera2World[3].xyz;
 	float3	View = normalize( _In.View );

	// Read depth at current pixel
	float2	UV = _In.Position.xy * ScreenInfos.xy;
	float3	Normal;
	float	Depth, SpecularPower;
	ReadDeferredMRTNormalDepthSpec( UV, Normal, Depth, SpecularPower );

	// Retrieve hit position & distance
	float3	CameraView = normalize( _In.CameraView );
	float3	HitPosition = ViewPosition + Depth / CameraView.z;
	float	HitDistance = length( HitPosition - ViewPosition );

	// Compute sky color
	float3	InitialColor = 1.0.xxx;	// Full energy
	float	Terminator;
	float3	SkyColor = ComputeSkyColor( ViewPosition, View, HitDistance, InitialColor, Terminator );

	return float4( SkyColor, dot( InitialColor, LUMINANCE_WEIGHTS ) );
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	ViewPosition =  Camera2World[3].xyz;
 	float3	View = normalize( _In.View );

	// Read depth at current pixel
	float2	UV = _In.Position.xy * ScreenInfos.xy;
	float3	Normal;
	float	Depth, SpecularPower;
	ReadDeferredMRTNormalDepthSpec( UV, Normal, Depth, SpecularPower );

	// Check if we should compute sky color or aerial perspective
	float3	InitialColor = 1.0.xxx;
	float3	SkyColor = 0.0.xxx;
	float	HitDistance = 0.0;
	float	Terminator;

	if ( Depth < 5000.0 )
	{	// Hitting an object of the scene...
		float3	CameraView = normalize( _In.CameraView );
		HitDistance = Depth / CameraView.z;

		float3	WorldNormal = mul( float4( Normal, 0.0 ), Camera2World ).xyz;
		// Compute sky color
//		if ( dot( WorldNormal, SunDirection ) > 0.0 )
			SkyColor = ComputeSkyColor( ViewPosition, View, HitDistance, InitialColor, Terminator );

//SkyColor = 0.0;
	}
	else
	{	// Hitting the sky

		// Compute view ray intersection with the upper atmosphere
		float	CameraHeight = WORLD_UNIT_TO_KILOMETER * ViewPosition.y;	// Relative height from sea level
		float	D = CameraHeight + EARTH_RADIUS;
		float	b = D * View.y;
		float	c = D*D-ATMOSPHERE_RADIUS*ATMOSPHERE_RADIUS;
		float	Delta = sqrt( b*b-c );
		HitDistance = Delta - b;	// Distance at which we hit the upper atmosphere (in kilometers)
		HitDistance /= WORLD_UNIT_TO_KILOMETER;

		// Compute sky color
		// 	149.5978875 = Distance to the Sun in millions of kilometers
		// 	0.695       = Radius of the Sun in millions of kilometers
		float	CosAngle = 0.999;	// Cos( SunCoverAngle )
		float	DotSun = dot( View, SunDirection );
		InitialColor = DotSun >= CosAngle
					 ? SunColor			// Either sunlight...
					 : 0.0.xxx;			// ...or the black and cold emptiness of space !

		// Compute sky color
		SkyColor = ComputeSkyColor( ViewPosition, View, HitDistance, InitialColor, Terminator );
		SkyColor += InitialColor;

		// Add night sky
		float3	NightSkyColor = 1.0 * pow( NightSkyCubeMap.Sample( LinearClamp, View.yzx ).xyz, 2.0 );
		SkyColor += (1.0 - Terminator) * NightSkyColor;
	}

	return float4( FinalFactor * SkyColor, dot( InitialColor, LUMINANCE_WEIGHTS ) );
}


// ===================================================================================
//
technique10 DrawSky
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Sky() ) );
	}
}

technique10 DrawAtmosphericScattering
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_AtmosphericPerspective() ) );
	}
}

technique10 DrawHybrid
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
