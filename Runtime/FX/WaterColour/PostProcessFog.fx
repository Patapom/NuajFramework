// This shader performs volumetric fog computations
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"
#include "LightSupport.fx"

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// Volume fog
Texture3D	VolumeFogTexture;
float		Time = 0.0;
float		ExtinctionFactor = 1.0;
float		InScatteringFactor = 0.2;
float		ScatteringAnisotropy = 0.2;

float		FogHeight = 4.0;
float		FogDepthStart = 4.0;
float		FogDepthEnd = -8.0;

static const float	SLAB_THICKNESS	= 8.0;
static const float	SLAB_HEIGHT		= 8.0;
static const float	SLAB_LENGTH		= 32.0;
static const int	FOG_MAX_SLABS_COUNT	= 6;

static const float	SlabVelocityFactors[] = { 1.0, 1.1, 0.9, 0.7, 1.3, 1.1 };
static const float	SlabHeightOffsets[] = { 0.0, 1.2, -3.7, 2.3, -0.3, 0.7 };

float2	ComputeExtinctionInScattering( float _SlabIndex, float2 _EntryPoint, float2 _ExitPoint )
{
	float2	FogOffset = float2(
		8.0 * SlabVelocityFactors[_SlabIndex] * Time,	// Time changes with slab
		FogHeight + SlabHeightOffsets[_SlabIndex]		// Height changes with slab
	);

	// Compute equivalent 3D texture coordinates
	float	U_In = 0.5 + (_EntryPoint.y - FogOffset.y) / SLAB_HEIGHT;
	float	V_In = 0.5 + (_EntryPoint.x - FogOffset.x) / SLAB_LENGTH;
	float	U_Out = 0.5 + (_ExitPoint.y - FogOffset.y) / SLAB_HEIGHT;
	float	V_Out = 0.5 + (_ExitPoint.x - FogOffset.x) / SLAB_LENGTH;
	float3	UVW_In = float3( U_In, U_Out, V_In );
	float3	UVW_Out = float3( U_In, U_Out, V_Out );

	// Sample the volume texture both at entry & exit point
	float2	ExtinctionInScattering_In = VolumeFogTexture.SampleLevel( LinearWrap, UVW_In, 0 ).xy;
	float2	ExtinctionInScattering_Out = VolumeFogTexture.SampleLevel( LinearWrap, UVW_Out, 0 ).xy;

	// Actual values are averaged
	return 0.5 * (ExtinctionInScattering_In + ExtinctionInScattering_Out);
}

float4	PS( VS_IN _In ) : SV_TARGET0
{
	// Read color & depth of the current pixel
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float3	RGB = GBufferTexture0.SampleLevel( NearestClamp, UV, 0 ).xyz;
	float	Depth = ReadDepth( UV );
	float3	CameraPosition = Camera2World[3].xyz;

	// Rebuild WORLD position from depth
	float3	View = float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 );
	float3	WorldPosition = mul( float4( View * Depth, 1.0 ), Camera2World ).xyz;
//	return float4( WorldPosition, 1.0 );

	// Compute camera ray
	float3	ToPixel = WorldPosition - CameraPosition;
	float	Distance2Pixel = length( ToPixel );
	ToPixel /= Distance2Pixel;

/*	// Compute entry & exit points for the slab
	float3	ExitPoint = CameraPosition + ToPixel * (min( FogDepthStart, max( FogDepthEnd, WorldPosition.z ) ) - CameraPosition.z) / ToPixel.z;
	float3	EntryPoint = CameraPosition + ToPixel * (min( FogDepthStart, max( FogDepthEnd, CameraPosition.z ) ) - CameraPosition.z) / ToPixel.z;
	float3	March = EntryPoint - ExitPoint;
//	return float4( 0.1 * EntryPoint, 1.0 );
//	return float4( 0.1 * ExitPoint, 1.0 );
//	return float4( 0.2 * March, 1.0 );
//	return float4( 0.4 * March.zzz, 1.0 );

	float	MarchLength = length( March );
	float	SlabsCount = min( FOG_MAX_SLABS_COUNT, floor( March.z / SLAB_THICKNESS ) );			// Amount of integer slabs we cross
	float	SlabRemainder = March.z / SLAB_THICKNESS - SlabsCount;	// Remaining slab thickness after integer slabs
//	March = March * SLAB_THICKNESS / (MarchLength * March.z);		// With this, we march through one slab every step
	March = March / SlabsCount;	// With this, we march through one slab every step

//	return float4( 0.2 * abs(March), 1.0 );
//	return float4( 0.1 * MarchLength.xxx, 1.0 );
//	return float4( 0.2 * SlabsCount.xxx, 1.0 );
//	return float4( SlabRemainder.xxx, 1.0 );

	// Trace through first slab (We enter into an integer slab from the BACK)
	int		CurrentSlabIndex = SlabsCount+1;
	float3	CurrentPoint = EntryPoint - March * (SlabsCount+1);		// Integer slab exit point (WorldPosition might be inside a slab so ExitPoint is not an actual slab exit point)
	float3	NextPoint = CurrentPoint + March;
	float2	TempExtinctionInScattering = ComputeExtinctionInScattering( CurrentSlabIndex, NextPoint.xy, CurrentPoint.xy );
//	return float4( 0.1 * CurrentPoint, 1.0 );
//	return float4( (EntryPoint - CurrentPoint), 1.0 );

	// Compute first non-integer extinction/in-scattering values
	float2	ExtinctionInScattering = lerp( float2( 1.0, 0.0 ), TempExtinctionInScattering, SlabRemainder );

	// Trace through remaining slabs
	for ( int SlabIndex=0; SlabIndex < SlabsCount; SlabIndex++ )
	{
		CurrentPoint = NextPoint;
		NextPoint += March;
		CurrentSlabIndex--;

		// Trace through integer slab
		TempExtinctionInScattering = ComputeExtinctionInScattering( CurrentSlabIndex, NextPoint.xy, CurrentPoint.xy );

		// Accumulate
		ExtinctionInScattering.x *= TempExtinctionInScattering.x;
		ExtinctionInScattering.y += TempExtinctionInScattering.y;
	}
//*/

//*
	float3	SlabCenter = float3( 8.0 * Time, FogHeight, FogDepthStart );

	float3	SlabBackPlanePos = SlabCenter - 0.5 * SLAB_THICKNESS * float3( 0.0, 0.0, 1.0 );
	float3	SlabFrontPlanePos = SlabCenter + 0.5 * SLAB_THICKNESS * float3( 0.0, 0.0, 1.0 );
//	return float4( SlabFrontPlanePos, 1.0 );

	float3	HitDistanceIn = (SlabFrontPlanePos - CameraPosition).z / ToPixel.z;
	float3	HitDistanceOut = (SlabBackPlanePos - CameraPosition).z / ToPixel.z;
//	return float4( 0.1 * (HitDistanceOut - HitDistanceIn).xxx, 1.0 );

	float	t = saturate( min( Distance2Pixel, HitDistanceOut ) / HitDistanceOut );
	HitDistanceOut *= t;
//	return float4( t.xxx, 1.0 );

	float3	HitPosIn = CameraPosition + HitDistanceIn * ToPixel;
	float3	HitPosOut = CameraPosition + HitDistanceIn * ToPixel;
//	return float4( 0.1 * HitPosIn, 1.0 );

	// Compute equivalent 3D texture coordinates
	float	U_In = 0.5 + (HitPosIn.y - SlabCenter.y) / SLAB_HEIGHT;
	float	V_In = 0.5 + (HitPosIn.x - SlabCenter.x) / SLAB_LENGTH;
	float	U_Out = 0.5 + (HitPosOut.y - SlabCenter.y) / SLAB_HEIGHT;
	float	V_Out = 0.5 + (HitPosOut.x - SlabCenter.x) / SLAB_LENGTH;
	float3	UVW_In = float3( U_In, U_Out, V_In );
	float3	UVW_Out = float3( U_In, U_Out, V_Out );

	// Sample the volume texture
	float2	ExtinctionInScattering_In = VolumeFogTexture.SampleLevel( LinearWrap, UVW_In, 0 ).xy;
	float2	ExtinctionInScattering_Out = VolumeFogTexture.SampleLevel( LinearWrap, UVW_Out, 0 ).xy;
	float2	ExtinctionInScattering = 0.5 * (ExtinctionInScattering_In + ExtinctionInScattering_Out);
//*/

	// Attenuate if traing less than the entire slab
	ExtinctionInScattering.x = exp( lerp( 0.0, log( 1e-3 + ExtinctionInScattering.x ), t ) );
	ExtinctionInScattering.y = ExtinctionInScattering.y * t;

	// Compute phase with light
	// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick's equivalent to Henyey-Greenstein
	float3	ToLight = normalize( LightPositionKey - WorldPosition );
	float	DotLightView = -dot( ToPixel, ToLight );
	float	Den = 1.0 / (1.0 + ScatteringAnisotropy * DotLightView);
	float	Phase = (1.0 - ScatteringAnisotropy*ScatteringAnisotropy) * Den * Den;

	ExtinctionInScattering.x = lerp( 1.0, ExtinctionInScattering.x, ExtinctionFactor );
	ExtinctionInScattering.y *= InScatteringFactor;

	// Choucroute and combine
 	RGB *= ExtinctionInScattering.x;								// Extinction
 	RGB += Phase * ExtinctionInScattering.y * LightColorKey.xyz;	// In-scattering

	return float4( RGB, 1.0 );
}

// ===================================================================================

technique10 VolumeFog
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
