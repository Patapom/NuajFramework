// There are several techniques in this shader :
// _ Gaussian blur to downscale and blur the cloud map
// _ Fractal displacement
// _ Final compositing
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"

float3	CloudPlaneCenter = float3( 0.0, 0.0, 0.0 );
float3	LightPosition;	// Vector pointing toward the light
float	LightIntensity;	// Light intensity (no kidding !)

Texture2D	SourceTexture;
float3		InvSourceTextureSize;
Texture2D	SourceDepthTexture;

struct VS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct VS_IN2
{
	float4	PositionRadius	: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_Position;
	float2	UV			: TEXCOORD0;
};

struct PS_OUT
{
	float4	Color		: SV_TARGET;
	float	Depth		: SV_DEPTH;
};

struct PS_IN2
{
	float4	__Position	: SV_Position;
	float3	Position	: POSITION;
	float3	UV			: TEXCOORD0;
};

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
	Out.UV = _In.UV;
	
	return	Out;
}

// ===================================================================================
// Cloud lighting
// Lighting is performed in screen space (like deferred shading) on pixels that are not at infinity
// Each cloud pixel samples its perceived cloud thickness from the deep shadow map.
// It then computes extinction and scattering due to light travelling through the cloud
// Some bizarre phase functions (espacially the side one) are then combined together to yield
//	the final color...
//
float4x4	World2LightProj;// The WORLD => LIGHT => PROJECTION transform
Texture2D	DeepShadowMap;	// The deep shadow map => RGBA contain each a slice of shadow map

static const int	STEPS_COUNT = 16;

float		ExtinctionCoefficient = 0.6;
float		ScatteringCoefficient = 0.01;
float		ScatteringAnisotropyForward = 0.8;
float		PhaseWeightForward = 0.5;
float		ScatteringAnisotropyBackward = -0.7;
float		PhaseWeightBackward = 0.5;
float		ScatteringAnisotropySide = -0.4;
float		PhaseWeightSide = 1.5;
float		MaxMarchDistance = 0.5;
float		DiffuseFactor = 0.4;
float		DiffuseBias = 0.1;
float		SpecularFactor = 0.05;
float		SpecularPower = 2.0;

float	ComputeThickness( float3 _WorldPosition )
{
	float4	ShadowMapPosition = mul( float4( _WorldPosition, 1.0 ), World2LightProj );
	float2	ShadowMapUV = 0.5 * float2( 1.0 + ShadowMapPosition.x, 1.0 - ShadowMapPosition.y );

	const float2	Offset = float2( 4.0 / 512.0, 0.0 );
	float4	Shadow = DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV - Offset.xy, 0 );
			Shadow += DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV + Offset.xy, 0 );
			Shadow += DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV - Offset.yx, 0 );
			Shadow += DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV + Offset.yx, 0 );
			Shadow *= 0.25;

	// Bias so we reach maximum earlier in a slice center
	ShadowMapPosition.z += +0.025;

	// Accumulate thickness based on depth
	float	Thickness = 0.0;
	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.x;
	ShadowMapPosition.z -= 0.25;
	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.y;
 	ShadowMapPosition.z -= 0.25;
 	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.z;
 	ShadowMapPosition.z -= 0.25;
 	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.w;

	return Thickness;
}

float4	PS_Render( PS_IN _In ) : SV_TARGET0
{
	float4	PixelData00 = SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 );
	clip( 999.0f - PixelData00.w );	// Infinity !
	float4	PixelData01 = SourceTexture.SampleLevel( LinearClamp, _In.UV + InvSourceTextureSize.xz, 0 );
	float4	PixelData10 = SourceTexture.SampleLevel( LinearClamp, _In.UV + InvSourceTextureSize.zy, 0 );
	float4	PixelData11 = SourceTexture.SampleLevel( LinearClamp, _In.UV + InvSourceTextureSize.xy, 0 );

	float3	Normal = normalize( 0.25 * (PixelData00.xyz + PixelData01.xyz + PixelData10.xyz + PixelData11.xyz) );
	float	Z = min( PixelData00.w, min( PixelData01.w, min( PixelData10.w, PixelData11.w ) ) );

	// Retrieve pixel's WORLD position
	float2	ProjPosition = float2( 2.0 * _In.UV.x - 1.0, 1.0 - 2.0 * _In.UV.y );
	float3	ViewCamera = float3( CameraData.y * CameraData.x * ProjPosition.x, CameraData.x * ProjPosition.y, 1.0 );
	float3	ViewWorld = mul( float4( ViewCamera, 0.0 ), Camera2World ).xyz;
	float3	Position = Camera2World[3].xyz + ViewWorld * Z;

	// Perform lighting
	float3	ToLight = normalize( LightPosition - Position );
	float3	ToPixel = normalize( Position - Camera2World[3].xyz );

	// Compute light phase
	// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick's equivalent to Henyey-Greenstein
	float	DotLightView = -dot( ToPixel, ToLight );

		// Forward phase
	float	Den = 1.0 / (1.0 + ScatteringAnisotropyForward * DotLightView);
	float	PhaseForward = (1.0 - ScatteringAnisotropyForward*ScatteringAnisotropyForward) * Den * Den;
		// Backward phase
	Den = 1.0 / (1.0 + ScatteringAnisotropyBackward * DotLightView);
	float	PhaseBackward = (1.0 - ScatteringAnisotropyBackward*ScatteringAnisotropyBackward) * Den * Den;
		// Side phase
//	DotLightView = 0.4 + 0.6 * DotLightView;	// Add bias
	float	PhaseSide = saturate( pow( sqrt(1.0 - 0.8 * DotLightView*DotLightView), ScatteringAnisotropySide ) );

	float	PhaseDirect = PhaseWeightForward * PhaseForward + PhaseWeightBackward * PhaseBackward;
	float	PhaseAmbient = PhaseWeightSide * PhaseSide;

	// Trace a little inside the cloud
	const float	STEP_FACTOR = exp( log( 1.0 + MaxMarchDistance ) / STEPS_COUNT );

	float2	ScatteredLight = 0.0;
	float	StepLength = STEP_FACTOR;
	float	MarchedDistance = 0.0;
	float3	CurrentPosition = Position;
	for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
	{
		// March one step
		MarchedDistance += (StepLength-1.0);
		CurrentPosition += (StepLength-1.0) * ToPixel;
		StepLength *= STEP_FACTOR;

		// Sample thickness at that position
		float	Thickness = ComputeThickness( CurrentPosition );

		// Compute extinguished light energy reaching sample position
		//	and in-scattered toward view direction
		float	Extinction = exp( -ExtinctionCoefficient * Thickness );
		float2	InScatteredLight = LightIntensity * Extinction * float2( PhaseDirect, PhaseAmbient );

		// Compute extinction to exit point
		float	Extinction2Exit = exp( -ExtinctionCoefficient * MarchedDistance );

		// Accumulate
		ScatteredLight += ScatteringCoefficient * Extinction2Exit * InScatteredLight;
	}

	// Add standard diffuse
	float	ThicknessAtPixel = ComputeThickness( Position );
	float	DotLight = saturate( (DiffuseBias + dot( Normal, ToLight )) / (1.0+DiffuseBias) );
	float	Diffuse = DiffuseFactor * exp( -ExtinctionCoefficient * ThicknessAtPixel ) * DotLight;

	// Add standard specular
	float3	Reflection = reflect( ToPixel, Normal );
	float	DotSpecular = saturate( dot( Reflection, ToLight ) );
	float	Specular = SpecularFactor * pow( DotSpecular, SpecularPower );

	// Decrease alpha with view angle
	float	DotNormal = dot( -ToPixel, Normal );
	float	Alpha = pow( DotNormal, 1.0 );

// 	return float4(
// 		Diffuse + Specular + ScatteredLight.x,	// Direct scattered lighting
// 		ScatteredLight.y,						// Ambient scattered
// 		PixelDepth,							// Distance 2 camera
// 		1.0//Alpha
// 		);
//

	float	DirectScatteredEnergy = Diffuse + Specular + ScatteredLight.x;
	float	AmbientScatteredEnergy = ScatteredLight.y;

	float3	Color = LightIntensity.xxx * (DirectScatteredEnergy + AmbientScatteredEnergy)
				  + AmbientScatteredEnergy * saturate( 1.0 - DirectScatteredEnergy * AmbientScatteredEnergy ) * float3( 0.2, 0.3, 0.4 );

	return float4( Color, 1.0 );
}

// ===================================================================================
// Gaussian Blurs

static const float	SIGMA = 5.0;
static const float	PI = 3.14159265358979;
static const float	GAUSS_DISTANCE = 4.0;
static const float	GAUSS_FACTOR = 1.0 / sqrt( 2.0 * PI * SIGMA*SIGMA );
static const float	GAUSS_PIXEL_FACTOR = -1.0 / ( 2.0 * SIGMA*SIGMA );
static const float	GAUSS_WEIGHTS[] = {
	exp( 1.0 * GAUSS_PIXEL_FACTOR ),
	exp( 2.0 * GAUSS_PIXEL_FACTOR ),
	exp( 3.0 * GAUSS_PIXEL_FACTOR ),
	exp( 4.0 * GAUSS_PIXEL_FACTOR ),
	exp( 5.0 * GAUSS_PIXEL_FACTOR ),
	exp( 6.0 * GAUSS_PIXEL_FACTOR ),
	exp( 7.0 * GAUSS_PIXEL_FACTOR ),
	exp( 8.0 * GAUSS_PIXEL_FACTOR ),
};

float	GaussDistance;
float	GaussDistanceDepth;
float	GaussWeight;
float2	PlaneDepthFactors = float2( 1.2, 1.0 );
float	OffsetFactor = 0.02;

float4 PS_GaussDiffuse( PS_IN _In ) : SV_Target
{
	float4	Color = SourceTexture.SampleLevel( LinearMirror, _In.UV, 0 );
//	return Color;

	float2	Delta = GaussDistance * InvSourceTextureSize.xy;
	float2	Offset = Delta;
	for ( int StepIndex=1; StepIndex < 8; StepIndex++ )
	{
		float4	V0 = SourceTexture.SampleLevel( LinearMirror, _In.UV - Offset, 0 );
		float4	V1 = SourceTexture.SampleLevel( LinearMirror, _In.UV + Offset, 0 );
		Color += GAUSS_WEIGHTS[StepIndex-1] * (V0 + V1);
		Offset += Delta;
	}

	return GaussWeight * GAUSS_FACTOR * Color;
}

/* Old tests with Z

float4 PS_GaussDiffuse( PS_IN _In ) : SV_Target
{
	float4	OriginalColor = SourceTexture.SampleLevel( LinearMirror, _In.UV, 0 );
//	return OriginalColor;

	// Compute view vector & plane distance
	float3	ViewCamera = float3( CameraData.y * (2.0 * _In.UV.x - 1.0) * CameraData.x, (1.0 - 2.0 * _In.UV.y) * CameraData.x, 1.0 );
	float	ViewLength = length( ViewCamera );
	float	PlaneDepth = PlaneDepthFactors.x * dot( CloudPlaneCenter - Camera2World[3].xyz, Camera2World[2].xyz ) * ViewLength;

//	float	MinZ = PlaneDepth;
	float	MinZ = 1e6;
 	float	SumZ = 0.0;
 	float	SumWeightZ = 0.0;
 	float	SumWeightMinZ = 0.0;
	float2	Delta = GaussDistance * InvSourceTextureSize.xy;
	float2	Offset = Delta;
	float4	Color = OriginalColor;
	for ( int StepIndex=1; StepIndex < 8; StepIndex++ )
	{
		float4	V0 = SourceTexture.SampleLevel( LinearMirror, _In.UV - Offset, 0 );
		float4	V1 = SourceTexture.SampleLevel( LinearMirror, _In.UV + Offset, 0 );

 		MinZ = min( MinZ, V0.z );
 		MinZ = min( MinZ, V1.z );

		SumZ += V0.z * V0.w;
		SumWeightZ += V0.w;
		SumWeightMinZ += 1.0 - V0.w;
		SumZ += V1.z * V1.w;
		SumWeightZ += V1.w;
		SumWeightMinZ += 1.0 - V1.w;

// 		if ( abs( V0.z - OriginalColor.z ) < PlaneDepthFactors.x )
// 			SumZ += V0.z;
// 		else
// 			SumWeightMinZ += 1.0;
// 		if ( abs( V1.z - OriginalColor.z ) < PlaneDepthFactors.x )
// 			SumZ += V1.z;
// 		else
// 			SumWeightMinZ += 1.0;

//  //		if ( V0.z < 10.0 )
// 		{
// 			SumZ += min( PlaneDepth, V0.z );
// 			SumWeightZ+=1.0;
// 		}
// //		if ( V1.z < 10.0 )
// 		{
// 			SumZ += min( PlaneDepth, V1.z );
// 			SumWeightZ+=1.0;
// 		}
// 
// 		V0.z = min( V0.z, PlaneDepth );
// 		V1.z = min( V1.z, PlaneDepth );

		float	Weight = GAUSS_WEIGHTS[StepIndex-1];
		Color += Weight * (V0 + V1);
		Offset += Delta;
	}

	float4	Result = GaussWeight * GAUSS_FACTOR * Color;
	Result.z = MinZ;
//	Result.z = SumZ / max( 1.0, SumWeightZ );
//	Result.z = (SumZ + SumWeightMinZ * MinZ) / 16.0f;
//	Result.z = (SumZ + SumWeightMinZ * MinZ) / (SumWeightZ + SumWeightMinZ);
//	Result.z = OriginalColor.z;

	return Result;
}

*/


static const int	EXPAND_SIZE = 12;

float2 PS_GaussDepthH( PS_IN _In ) : SV_Target
{
	float	OriginalDepth = SourceTexture.SampleLevel( LinearMirror, _In.UV, 0 ).w;
	float	MinDepths[EXPAND_SIZE];
	MinDepths[0] = OriginalDepth;

	float	MinZ = 1e5;
// 	float	AvgZ = 0.0;
// 	float	AvgWeight = 0.0;
	float2	Delta = GaussDistanceDepth * InvSourceTextureSize.xy;
	float2	Offset = Delta;
	for ( int StepIndex=1; StepIndex < EXPAND_SIZE; StepIndex++ )
	{
		float	Z0 = SourceTexture.SampleLevel( LinearMirror, _In.UV - Offset, 0 ).w;
		float	Z1 = SourceTexture.SampleLevel( LinearMirror, _In.UV + Offset, 0 ).w;
		float	Z = min( Z0, Z1 );

// 		// Average distance only if not at infinity
// 		if ( Z0 < 999.0 )
// 		{
// 			AvgZ += Z0;
// 			AvgWeight += 1.0;
// 		}
// 		if ( Z1 < 999.0 )
// 		{
// 			AvgZ += Z1;
// 			AvgWeight += 1.0;
// 		}

//		MinZ = min( MinZ, Z );
		if ( Z0 < MinZ-PlaneDepthFactors.x )
			MinZ = Z0;
		if ( Z1 < MinZ-PlaneDepthFactors.x )
			MinZ = Z1;

		MinDepths[StepIndex] = min( MinDepths[StepIndex-1], MinZ );

		Offset += Delta;
	}

	// Determine which MinZ we should take based on average distance
//	AvgZ /= max( 1.0, AvgWeight );
//	float	MaxOffsetProj = OffsetFactor / (AvgZ * CameraData.x);	// = Offset / (Z * tan(FOV/2)
	float	MaxOffsetProj = OffsetFactor / (MinZ * CameraData.x);	// = Offset / (Z * tan(FOV/2)
	float	MaxOffsetPixels = 0.5 * MaxOffsetProj / InvSourceTextureSize.x;
	MaxOffsetPixels = min( MaxOffsetPixels, EXPAND_SIZE-1 );

	int	MaxOffsetPixels0 = int( floor( MaxOffsetPixels ) );
	int	MaxOffsetPixels1 = min( EXPAND_SIZE-1, MaxOffsetPixels0+1 );

	MinZ = lerp( MinDepths[MaxOffsetPixels0], MinDepths[MaxOffsetPixels1], MaxOffsetPixels-MaxOffsetPixels0 );

	return float2( OriginalDepth, MinZ );
}

float2 PS_GaussDepthV( PS_IN _In ) : SV_Target
{
	float2	OriginalDepth = SourceTexture.SampleLevel( LinearMirror, _In.UV, 0 ).xy;
//	return OriginalDepth;

	float	MinDepths[EXPAND_SIZE];
	MinDepths[0] = OriginalDepth.y;

	float	MinOriginalZ = 1e5;
	float	MinZ = 1e5;
	float2	Delta = GaussDistanceDepth * InvSourceTextureSize.xy;
	float2	Offset = Delta;
	for ( int StepIndex=1; StepIndex < EXPAND_SIZE; StepIndex++ )
	{
		float2	Z0 = SourceTexture.SampleLevel( LinearMirror, _In.UV - Offset, 0 ).xy;
		float2	Z1 = SourceTexture.SampleLevel( LinearMirror, _In.UV + Offset, 0 ).xy;
		float	OriginalZ = min( Z0.x, Z1.x );
		MinOriginalZ = min( MinOriginalZ, OriginalZ );

//		float	Z = min( Z0.y, Z1.y );
//		MinZ = min( MinZ, Z );
		if ( Z0.y < MinZ-PlaneDepthFactors.x )
			MinZ = Z0.y;
		if ( Z1.y < MinZ-PlaneDepthFactors.x )
			MinZ = Z1.y;

		MinDepths[StepIndex] = min( MinDepths[StepIndex-1], MinZ );

		Offset += Delta;
	}

	// Determine which MinZ we should take based on average distance
	float	MaxOffsetProj = OffsetFactor / (MinOriginalZ * CameraData.x);	// = Offset / (Z * tan(FOV/2)
	float	MaxOffsetPixels = 0.5 * MaxOffsetProj / InvSourceTextureSize.y;
	MaxOffsetPixels = min( MaxOffsetPixels, EXPAND_SIZE-1 );

	int	MaxOffsetPixels0 = int( floor( MaxOffsetPixels ) );
	int	MaxOffsetPixels1 = min( EXPAND_SIZE-1, MaxOffsetPixels0+1 );

	MinZ = lerp( MinDepths[MaxOffsetPixels0], MinDepths[MaxOffsetPixels1], MaxOffsetPixels-MaxOffsetPixels0 );

	OriginalDepth.y = MinZ;
	return OriginalDepth;
}


// ===================================================================================
// Fractal Distort
int		OctavesCount = 4;
float	FrequencyFactor = 0.5;

float4	GP;

float3	NoiseDeform( float3 _WorldPosition )
{
	float3	Offset = 0.0.xxx;
	float	Weight = 1.0;
	for ( int OctaveIndex=0; OctaveIndex < OctavesCount; OctaveIndex++ )
	{
		float3	Derivatives;
		Offset.x += Weight * (2.0 * Noise( FrequencyFactor * _WorldPosition, NoiseTexture0, Derivatives ) - 1.0);
		Offset.y += Weight * (2.0 * Noise( FrequencyFactor * _WorldPosition, NoiseTexture1, Derivatives ) - 1.0);
		Offset.z += Weight * (2.0 * Noise( FrequencyFactor * _WorldPosition, NoiseTexture2, Derivatives ) - 1.0);

		Weight *= 0.5;
		_WorldPosition *= 2.0;
	}

	return Offset;
}

float4 PS_FractalDistort( PS_IN _In ) : SV_Target
{
// 	float4	Pipo = SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 );
// 	return float4( LightIntensity.xxx * (Pipo.x + Pipo.y) + saturate( 1.0 - Pipo.x * Pipo.y ) * 0.5 * float3( 0.4, 0.6, 0.8 ) * Pipo.y, Pipo.w*Pipo.w );

	float3	CameraPosition = Camera2World[3].xyz;

	// Retrieve depth at current position
	float2	CurrentDepth = SourceDepthTexture.SampleLevel( LinearClamp, _In.UV, 0 ).xy;
	float	Z = CurrentDepth.x;
	float	MinZ = CurrentDepth.y;
//	return float4( 0.3 * MinZ.xxx, 1 );

	// Compute view vector & plane distance
	float2	PixelPositionProj = float2( 2.0 * _In.UV.x - 1.0, 1.0 - 2.0 * _In.UV.y );
	float3	ViewCamera = float3( CameraData.y * CameraData.x * PixelPositionProj.x, CameraData.x * PixelPositionProj.y, 1.0 );
	float3	ViewWorld = mul( float4( ViewCamera, 0.0 ), Camera2World ).xyz;
	float	PlaneDepth = PlaneDepthFactors.y * dot( CloudPlaneCenter - CameraPosition, Camera2World[2].xyz );

	// ===================================
	// Compute noise offset for MinZ
	float3	PixelPosition = CameraPosition + MinZ * ViewWorld;
	float3	Offset = NoiseDeform( PixelPosition );

	// Project offset in 2D
	float4	PixelPositionOffsetProj = mul( float4( PixelPosition + OffsetFactor * Offset, 1.0 ), World2Proj );
	PixelPositionOffsetProj /= PixelPositionOffsetProj.w;
	float2	Offset2D = PixelPositionOffsetProj.xy - PixelPositionProj;
//	return float4( PixelPositionProj, 0, 1 );
//	return float4( 1.0 * PixelPositionOffsetProj.xy, 0, 1 );
//	return float4( abs( PixelPositionOffsetProj.xy - PixelPositionProj ), 0, 1 );
//	return float4( abs( Offset2D ), 0, 1 );

	// Sample at offset
	float4	Color0 = SourceTexture.SampleLevel( LinearMirror, _In.UV + Offset2D, 0 );

	// ===================================
	// Compute noise offset for Z
	PixelPosition = CameraPosition + Z * ViewWorld;
	Offset = NoiseDeform( PixelPosition );

	// Project offset in 2D
	PixelPositionOffsetProj = mul( float4( PixelPosition + OffsetFactor * Offset, 1.0 ), World2Proj );
	PixelPositionOffsetProj /= PixelPositionOffsetProj.w;
	Offset2D = PixelPositionOffsetProj.xy - PixelPositionProj;
//	return float4( PixelPositionProj, 0, 1 );
//	return float4( 1.0 * PixelPositionOffsetProj.xy, 0, 1 );
//	return float4( abs( PixelPositionOffsetProj.xy - PixelPositionProj ), 0, 1 );
//	return float4( abs( Offset2D ), 0, 1 );

	// Sample at offset
	float4	Color1 = SourceTexture.SampleLevel( LinearMirror, _In.UV + Offset2D, 0 );
//	Color1.w *= Color1.w;

	// ===================================
	// Mix colors
//	float4	Color = 0.5 * (Color0 + Color1);
	float4	Color = lerp( Color0, 0.5*(Color0+Color1), Color0.w );

	if ( GP.x > 0.1 )
		Color = Color0;
	else if ( GP.x < 0.1 )
		Color = Color1;

	if ( GP.y > 0.1 )
		Color = lerp( Color0, Color1, Color0.w*Color0.w );
	else if ( GP.y < 0.1 )
	{
		Color = 0.5 * (Color0 + Color1);
		Color.w = min( Color0.w, Color1.w );
	}

//	Color.w *= Color.w;

//display background displace pour voir, pas normal qu'on voie encore du vide à travers !

	return Color;

//	float	Alpha = Color.w;
//	float	Alpha = CurrentPixelData.w;
//	return float4( Alpha.xxx, 1 );

// 	if ( CurrentPixelData.w > Color.w )
// 	{
// 		Color = CurrentPixelData;
// 	}
//	Color.xy = max( Color.xy, CurrentPixelData.xy );
//	Color.w = max( Color.w, CurrentPixelData.w );

	// Soluce Gaël 
// 	float	DiffuseIntensity = dot( Color, Color );
// 	Alpha = saturate( 10.0 * DiffuseIntensity ) * CurrentPixelData.w;

//	return float4( Alpha.xxx, 1 );

//	return float4( LightIntensity.xxx * Color.x + 0.6 * float3( 0.4, 0.6, 0.8 ) * Color.y, Color.w*Color.w );
//	return float4( LightIntensity.xxx * (Color.x + Color.y), Color.w*Color.w );
//	return float4( LightIntensity.xxx * (Color.x + Color.y) + Color.y * saturate( 1.0 - Color.x * Color.y ) * 0.5 * float3( 0.4, 0.6, 0.8 ), Color.w*Color.w );
}

// ===================================================================================
// Per-Particle Fractal Distort
/*
VS_IN2	VS_Particle( VS_IN2 _In )
{
	return _In;
}

[maxvertexcount(4)]
void GS( point VS_IN2 _In[1], inout TriangleStream<PS_IN2> Stream )
{
	float3	Position = _In[0].PositionRadius.xyz;
	float	Radius = _In[0].PositionRadius.w;
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	PS_IN2	Out;
	Out.Position = Position + Radius * (-Right + Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = Radius * float3( -1.0, +1.0, 0.5 );
	Stream.Append( Out );

	Out.Position = Position + Radius * (-Right - Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = Radius * float3( -1.0, -1.0, 0.5 );
	Stream.Append( Out );

	Out.Position = Position + Radius * (+Right + Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = Radius * float3( +1.0, +1.0, 0.5 );
	Stream.Append( Out );

	Out.Position = Position + Radius * (+Right - Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = Radius * float3( +1.0, -1.0, 0.5 );
	Stream.Append( Out );
}

float4 PS_FractalDistortParticleOld( PS_IN2 _In ) : SV_Target
{
//	return float4( _In.UV, 0, 1 );

	float2	SourceUV = _In.__Position.xy;// / _In.__Position.w;

// HARDCODED !!
//SourceUV /= float2( 456, 261 );
SourceUV /= float2( 913, 522 );
// HARDCODED !!

//	float2	SourceUV = float2( 0.5 * (1.0 + ScreenPos.x), 0.5 * (1.0 - ScreenPos.y) );
//	float2	SourceUV = float2( 0.5 * (1.0 + ScreenPos.x), 0.5 * (1.0 - ScreenPos.y) );
//	return float4( SourceUV, 0, 1 );
	float	Distance2Pixel = length( _In.Position - Camera2World[3].xyz );

	// Sample fractal shit
	const int	OctavesCount = 9;
	const float	FrequencyFactor = 0.5;
	const float	OffsetFactor = 0.04;

// 	float3	Position = _In.Position;
// 	float2	Offset = 0.0.xx;
// 	float	Weight = 1.0;
// 	for ( int OctaveIndex=0; OctaveIndex < OctavesCount; OctaveIndex++ )
// 	{
// 		float3	Derivatives;
// 		Offset.x += Weight * (2.0 * Noise( FrequencyFactor * Position, NoiseTexture0, Derivatives ) - 1.0);
// 		Offset.y += Weight * (2.0 * Noise( FrequencyFactor * Position, NoiseTexture1, Derivatives ) - 1.0);
// 
// 		Weight *= 0.5;
// 		Position *= float3( 2.1, 1.9, 2.0 );
// 	}

	float3	Position = _In.Position;
	float	RadialOffset = 0.0;
	float	Weight = 1.0;
	for ( int OctaveIndex=0; OctaveIndex < OctavesCount; OctaveIndex++ )
	{
		float3	Derivatives;
		RadialOffset += Weight * Noise( FrequencyFactor * Position, NoiseTexture0, Derivatives );

		Weight *= 0.5;
		Position *= float3( 2.1, 1.9, 2.0 );
	}

	// Idée : projeter le fetch en 2D et si en dehors du rayon de la boule alors alpha = 0

	float2	ToCenter = -_In.UV.xy;
	float	Radius = length( ToCenter );
	ToCenter *= 4.0 * saturate(Radius) / Radius;
	float2	Offset = RadialOffset * ToCenter / Distance2Pixel;

//	return float4( ToCenter, 0, 1 );

	// Sample with offset
	return SourceTexture.SampleLevel( LinearClamp, SourceUV + OffsetFactor * Offset, 0 );
}

// Rendering parameters
float		ExtinctionCoefficient = 0.6;
float		ScatteringCoefficient = 0.01;
float		ScatteringAnisotropyForward = 0.8;
float		PhaseWeightForward = 0.5;
float		ScatteringAnisotropyBackward = -0.7;
float		PhaseWeightBackward = 0.5;
float		ScatteringAnisotropySide = -0.4;
float		PhaseWeightSide = 1.5;
float		MaxMarchDistance = 0.5;

float4x4	World2LightProj;	// The WORLD => LIGHT => PROJECTION transform
Texture2D	DeepShadowMap;		// The deep shadow map => RGBA contain each a slice of shadow map

float	ComputeThickness( float3 _WorldPosition )
{
	float4	ShadowMapPosition = mul( float4( _WorldPosition, 1.0 ), World2LightProj );
//	float2	ShadowMapUV = 0.5 * (1.0 + ShadowMapPosition.xy);
	float2	ShadowMapUV = 0.5 * float2( 1.0 + ShadowMapPosition.x, 1.0 - ShadowMapPosition.y );

	const float2	Offset = float2( 4.0 / 512.0, 0.0 );
	float4	Shadow = DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV - Offset.xy, 0 );
			Shadow += DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV + Offset.xy, 0 );
			Shadow += DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV - Offset.yx, 0 );
			Shadow += DeepShadowMap.SampleLevel( LinearClamp, ShadowMapUV + Offset.yx, 0 );
			Shadow *= 0.25;

	// Bias so we reach maximum earlier in a slice center
	ShadowMapPosition.z += +0.025;

	// Accumulate thickness based on depth
	float	Thickness = 0.0;
	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.x;
	ShadowMapPosition.z -= 0.25;
	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.y;
 	ShadowMapPosition.z -= 0.25;
 	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.z;
 	ShadowMapPosition.z -= 0.25;
 	Thickness += saturate( 4.0 * ShadowMapPosition.z ) * Shadow.w;

	return Thickness;
}

float4 ComputeCloudColor( float3 _WorldPosition, float3 _WorldNormal )
{
 	float3	ToPixel = normalize( _WorldPosition - Camera2World[3].xyz );
	float3	ToLight = normalize( LightPosition - _WorldPosition );

	const int		STEPS_COUNT = 16;
	const float		STEP_FACTOR = exp( log( 1.0 + MaxMarchDistance ) / STEPS_COUNT );

	// Compute light phase
	// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick equivalent to Henyey-Greenstein
	float	DotLightView = -dot( ToPixel, ToLight );

		// Forward phase
	float	Den = 1.0 / (1.0 + ScatteringAnisotropyForward * DotLightView);
	float	PhaseForward = (1.0 - ScatteringAnisotropyForward*ScatteringAnisotropyForward) * Den * Den;
		// Backward phase
	Den = 1.0 / (1.0 + ScatteringAnisotropyBackward * DotLightView);
	float	PhaseBackward = (1.0 - ScatteringAnisotropyBackward*ScatteringAnisotropyBackward) * Den * Den;
		// Side phase
//	Den = 1.0 / (1.0 + ScatteringAnisotropySide * sqrt(1.0-DotLightView*DotLightView));
//	float	PhaseSide = (1.0 - ScatteringAnisotropySide*ScatteringAnisotropySide) * Den * Den;

	float	PhaseSide = pow( sqrt(1.0-DotLightView*DotLightView), ScatteringAnisotropySide );

	float	Phase =	  PhaseWeightForward * PhaseForward
					+ PhaseWeightBackward * PhaseBackward
					+ PhaseWeightSide * PhaseSide;

	// Trace a little inside the cloud
	float	ScatteredLight = 0.0;
	float	StepLength = STEP_FACTOR;
	float	MarchedDistance = 0.0;
	float3	CurrentPosition = _WorldPosition;
	for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
	{
		// March one step
		MarchedDistance += (StepLength-1.0);
		CurrentPosition += (StepLength-1.0) * ToPixel;
		StepLength *= STEP_FACTOR;

		// Sample thickness at that position
		float	Thickness = ComputeThickness( CurrentPosition );

		// Compute extinguished light energy reaching sample position
		//	and in-scattered toward view direction
		float	Extinction = exp( -ExtinctionCoefficient * Thickness );
		float	InScatteredLight = LightIntensity * Extinction * Phase;

		// Compute extinction to exit point
		float	Extinction2Exit = exp( -ExtinctionCoefficient * MarchedDistance );

		// Accumulate
		ScatteredLight += InScatteredLight * ScatteringCoefficient * Extinction2Exit;
	}

	// Add standard diffuse
	float	ThicknessAtPixel = ComputeThickness( _WorldPosition );
	float	DotLight = saturate( 0.9 * (0.1 + dot( _WorldNormal, ToLight )) );
	float	Diffuse = 0.4 * exp( -ExtinctionCoefficient * ThicknessAtPixel ) * DotLight;

	// Add pseudo-specular
	float3	Reflection = reflect( ToPixel, _WorldNormal );
	float	DotSpecular = saturate( dot( Reflection, ToLight ) );
	float	Specular = 0.05 * pow( DotSpecular, 2.0 );

	return float4( (Diffuse + Specular + ScatteredLight).xxx, 1.0 );
}

PS_OUT PS_FractalDistortParticle( PS_IN2 _In )
{
	PS_OUT	Out;

	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;
	float3	At = Camera2World[2].xyz;
	float	SphereRadius = _In.UV.z;
	float3	ToCenter = -_In.UV.x * Right - _In.UV.y * Up;
	float3	SphereCenter = _In.Position + ToCenter;
	float	Distance2Center = length( ToCenter );

	// Compute noise amplitude based on normalized distance to center
	float	NoiseAmplitude = saturate( Distance2Center / SphereRadius );
//	return float4( NoiseAmplitude.xxx, 1.0 );

	// Sample fractal shit
	const int	OctavesCount = 3;
	const float	FrequencyFactor = 2.0;
	const float	OffsetFactor = 0.1;

	float3	CurrentPosition = _In.Position;
	float2	Offset = 0.0.xx;
	float	Weight = 1.0;
	for ( int OctaveIndex=0; OctaveIndex < OctavesCount; OctaveIndex++ )
	{
		float3	Derivatives;
		Offset.x += Weight * (Noise( FrequencyFactor * CurrentPosition, NoiseTexture0, Derivatives ) - 0.5);
		Offset.y += Weight * (Noise( FrequencyFactor * CurrentPosition, NoiseTexture1, Derivatives ) - 0.5);

		Weight *= 0.5;
		CurrentPosition *= 2.0;
	}

	// Apply 2D offset to current position
	float3	OffsetPosition = _In.Position + NoiseAmplitude * OffsetFactor * (Offset.x * Right + Offset.y * Up);

	// Compute distance to center
	Distance2Center = length( SphereCenter - OffsetPosition );
	clip( SphereRadius - Distance2Center );	// Exit if asking to sample outside of the sphere...

	// Compute Z
	float	NormalizedDistance = Distance2Center / SphereRadius;
	float	Z = SphereRadius * sqrt( 1.0 - NormalizedDistance*NormalizedDistance );

	// Compute 3D position on the sphere
	float3	SpherePosition = OffsetPosition - Z * At;
	float3	SphereNormal = normalize( SpherePosition - SphereCenter );
	Out.Color = float4( SphereNormal, 1.0 );

	// Compute cloud shading
	float4	CloudColor = ComputeCloudColor( SpherePosition, SphereNormal );
	Out.Color = float4( CloudColor.xyz, 1.0 - pow( NormalizedDistance, 20.0 ) );

	// Compute final depth
	float	CameraZ = dot( SpherePosition - Camera2World[3].xyz, At );
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf-Zn)
	Out.Depth = Q * (1.0 - CameraData.z / CameraZ);

	return Out;
}
*/

// ===================================================================================
// Blend Result

float4 PS_BlendResult( PS_IN _In ) : SV_Target
{
//	return float4( _In.UV, 0.0, 1.0 );
//	return float4( 1.0, 0.0, 0.0, 1.0 );
//	return float4( SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 ).xyz, 1.0 );
//	return float4( SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 ).www, 1.0 );
	return SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 );
}

// ===================================================================================

technique10 RenderCloudLighting
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Render() ) );
	}
}

technique10 GaussianBlurDiffuse
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_GaussDiffuse() ) );
	}
}

technique10 GaussianBlurDepthH
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_GaussDepthH() ) );
	}
}

technique10 GaussianBlurDepthV
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_GaussDepthV() ) );
	}
}

technique10 FractalDistort
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_FractalDistort() ) );
	}
}

// technique10 FractalDistort_Particle
// {
// 	pass P0
// 	{
// 		SetVertexShader( CompileShader( vs_4_0, VS_Particle() ));
// 		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
// 		SetPixelShader( CompileShader( ps_4_0, PS_FractalDistortParticle() ) );
// 	}
// }

technique10 BlendResult
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_BlendResult() ) );
	}
}
