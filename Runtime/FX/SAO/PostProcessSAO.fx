// Separable Ambient Occlusion post-process
// This shader applies the technique described in http://perso.telecom-paristech.fr/~jhuang/paper/SAO.pdf
// The idea is quite clever and allows to bring a O(N²) process down to a O(N) one.
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"

static const int	SAMPLES_COUNT = 3;

// Source texture
float3		InvSourceSize;		// XY=1/Size Z=0
float2		AOBufferToZBufferRatio;	// Since we're sampling the Z buffer from the AO buffer that maybe smaller than the screen, we need that ratio to scale the input position
Texture2D	SourceBuffer;
Texture2D	AOBuffer;

int			AOState;
float		AOSphereRadius;
float		AOStrength;
float		AOFetchScale;

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN VS( VS_IN _In ) {	 return	_In; }

// ===================================================================================
// Perform ambient occlusion along a predefined set of 3x3 axes wrapping on the screen
float2	Axes[] = { float2( 0.342020148718266, 0.939692618823163 ), float2( 0.939692614065982, 0.342020161788514 ), float2( 0.766044417860317, 0.642787639788638 ), float2( 0.866025396499207, 0.500000012618391 ), float2( 0.642787606303771, 0.766044445957458 ), float2( 0.499999974763217, 0.866025418354902 ), float2( 1, 0 ), float2( 0.173648100269905, 0.984807766659389 ), float2( 0.98480775130631, 0.173648187341558 ) };

float	PerformAO( float2 _Position, float2 _Axis, float _Zref )
{
	// sqrt( 1 - Distance² )  Distances are 1/2, 1/3 and 1/4 as we take 3 samples
	float	SphereHeights[] = { 0.9682458365518542212948163499456, 0.86602540378443864676372317075294, 0.66143782776614764762540393840982 };

	float	AO = 0.0;
	float	SumWeights = 1.0;
	float2	P0 = _Position.xy;
	float2	P1 = _Position.xy;
	float	Z;

	for ( int i=0; i < SAMPLES_COUNT; i++ )
	{
		// Step left and right
		P0 -= _Axis;
		P1 += _Axis;

		// Compute the sphere depth at current distance
		float	SphereHeight = SphereHeights[i] * AOSphereRadius;

		// Accumulate AO
		Z = _Zref - ReadDepth( P0 );
		if ( Z > 0.0 && Z < SphereHeight )
			AO += Z / SphereHeight;

		Z = _Zref - ReadDepth( P1 );
		if ( Z > 0.0 && Z < SphereHeight )
 			AO += Z / SphereHeight;
	}

	return saturate( 1.0 - AOStrength * AO );
}

float2 PS_ComputeAO( VS_IN _In ) : SV_TARGET0
{
	int		X = ((int) floor( _In.Position.x )) % 3;
	int		Y = ((int) floor( _In.Position.y )) % 3;
	float2	Axis = Axes[3*Y+X];

	// Sample reference Z
	float2	Position = _In.Position.xy * AOBufferToZBufferRatio.xy;
	float	Zref = ReadDepth( Position );
//	return 0.1 * Zref;
// 	if ( Zref > CameraData.w - 1.0 )
// 		return 1.0;	// Infinity : no occlusion

	// Scale the axis based on the sampling sphere radius's projected to screen
	float	ExpandedRadius = AOSphereRadius * (SAMPLES_COUNT+1) / SAMPLES_COUNT;				// Expand the sphere by 1 sample so the last sample doesn't fall exactly on the sphere but a little inside
	float	RadiusInPixels = 0.5 * ExpandedRadius / (CameraData.x * Zref * InvSourceSize.y);	// ProjectedRadiusInPixels = Radius / (tan(FOV/2) * Zref) * Height / 2
//	RadiusInPixels = min( 8.0, RadiusInPixels );
	Axis *= AOFetchScale * RadiusInPixels;
//	return RadiusInPixels;

	// Perform AO along X and Y axes
	return float2( PerformAO( Position, Axis, Zref ), PerformAO( Position, float2( -Axis.y, Axis.x ), Zref ) );
}

// ===================================================================================
// Apply ambient occlusion to the computed scene
// This is quite wrong as AO should be applied to the ambient lighting term only instead of the entire scene
//	since SSAO is not a post-process like in this example but rather something you call right after the depth pass
//	to generate a per-pixel AO value that you later use in your deferred renderer.
// Anyway, I wrote it as a post-process for the sake of simplicity here...
//
float3 PS_ApplyAO( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * InvSourceSize.xy;
	float3	SourceColor = SourceBuffer.SampleLevel( LinearClamp, UV, 0.0 ).xyz;

	// Perform 4 taps of AO that should blur the interleaved sampling
	float2	AO0 = AOBuffer.SampleLevel( LinearClamp, UV - 0.5 * InvSourceSize.xy, 0.0 ).xy;
	float2	AO1 = AOBuffer.SampleLevel( LinearClamp, UV + 0.5 * InvSourceSize.xy, 0.0 ).xy;
	float2	AO2 = AOBuffer.SampleLevel( LinearClamp, UV + 0.5 * float2( InvSourceSize.x, -InvSourceSize.y ), 0.0 ).xy;
	float2	AO3 = AOBuffer.SampleLevel( LinearClamp, UV - 0.5 * float2( InvSourceSize.x, -InvSourceSize.y ), 0.0 ).xy;

// 	// Screen mode
// 	float	AO =  1.0-(1.0-AO0.x)*(1.0-AO0.y);
// 			AO += 1.0-(1.0-AO1.x)*(1.0-AO1.y);
// 			AO += 1.0-(1.0-AO2.x)*(1.0-AO2.y);
// 			AO += 1.0-(1.0-AO3.x)*(1.0-AO3.y);
//			AO = 0.25 * AO;

	// Multiply mode
// 	float	AO =  AO0.x*AO0.y;
// 			AO += AO1.x*AO1.y;
// 			AO += AO2.x*AO2.y;
// 			AO += AO3.x*AO3.y;
//			AO = 0.25 * AO;

	// Additive mode
	float	AO =  AO0.x+AO0.y;
			AO += AO1.x+AO1.y;
			AO += AO2.x+AO2.y;
			AO += AO3.x+AO3.y;
			AO = 0.125 * AO;

	if ( AOState == 2 )
		return AO;

	return SourceColor * (AOState == 1 ? AO : 1.0);
}

// ===================================================================================
//
technique10 ComputeSAO
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ComputeAO() ) );
	}
}

technique10 DisplaySAO
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ApplyAO() ) );
	}
}
