// This shader animates and displays blob particles
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
// Particles Simulation
//
Texture2D	DefaultPositions;
Texture2D	LastPositions;
float3		BufferInvSize;

float		Wind;
float		Spring;
float		NoiseIntensity;
float		Time;
float		DeltaTime;

float4	PS_Dynamics( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	
	float3	TargetPosition = DefaultPositions.SampleLevel( NearestClamp, UV, 0.0 ).xyz;

	float2	cs = float2( cos( 0.2 * Time ), sin( 0.2 * Time ) );
	float3	X = float3( cs.x, TargetPosition.y, -cs.y );
	float3	Z = float3( cs.y, TargetPosition.y, cs.x );

//	TargetPosition = TargetPosition.x * X + TargetPosition.z * Z;

	float3	LastPosition = LastPositions.SampleLevel( NearestClamp, UV, 0.0 ).xyz;

	// Compute noise values
	float3	Deriv;
	float3	NoiseDisplace = float3(
		2.0 * Noise( 0.4 * (LastPosition + 0.1 * Time * float3( 0.0, 2.0, 0.0 )), NoiseTexture0, Deriv ) - 1.0,
		2.0 * Noise( 0.4 * (LastPosition + 0.1 * Time * float3( -1.0, 2.0, 0.0 )), NoiseTexture1, Deriv ) - 1.0,
		2.0 * Noise( 0.4 * (LastPosition + 0.1 * Time * float3( 0.0, -3.0, 1.0 )), NoiseTexture2, Deriv ) - 1.0
		);
	float	NoiseValue = Noise( 0.05 * (LastPosition + float3( 0.0, Time, 0.0 )), NoiseTexture3, Deriv );

	// Compute force due to wind at current position
	float	Distance2Sphere = length( LastPosition );
	float3	ToPosition = LastPosition / Distance2Sphere;
	float	AngularPressure = lerp( 0.1, 1.0, 0.5 * (1.0 - ToPosition.y) );	// Maximal pressure below the sphere, minimal above

	float	NoiseWind = AngularPressure * Wind * (0.5 + 0.5 * NoiseValue);
//	float3	WindForce = float3( 0.0, NoiseWind, 0.0 );	// Vertical force
	float3	WindForce = NoiseIntensity * NoiseDisplace + NoiseWind * float3( 0.0, 1.0, 0.0 );	// Vertical force

	// Compute force attracting to default position
	float3	ToTarget = TargetPosition - LastPosition;
	float	Distance2Target = length( ToTarget );
	ToTarget /= Distance2Target;
	float3	TargetForce = ToTarget * Distance2Target * Spring;

	// Compute new position
	float3	NewPosition = LastPosition + DeltaTime * (TargetForce + WindForce);

	// Constrain to the surface of the sphere
	float	NewDistance2Sphere = length( NewPosition );
	if ( NewDistance2Sphere < 1.0 )
		NewPosition /= NewDistance2Sphere;

	// Write final position
//	return float4( NewPosition, saturate( 1.0 - 0.1 * NoiseWind ) );
	return float4( NewPosition, 0.1 + saturate( 1.0 - 0.2 * (Distance2Sphere-1.0)*(Distance2Sphere-1.0) ) );
//	return float4( NewPosition, length(WindForce) );
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
TextureCube	EnvMap;

float4	PS_Display( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;

	float4	Zc = SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 );
	if ( Zc.w > 99.0 )
		return 0.0;

// 	float4	Zx0 = SourceBuffer.SampleLevel( LinearClamp, UV - 1.5 * BufferInvSize.xz, 0.0 );
// 	if ( abs(Zx0-Zc) > ZThreshold ) Zx0 = Zc;
// 	float4	Zx1 = SourceBuffer.SampleLevel( LinearClamp, UV + 1.5 * BufferInvSize.xz, 0.0 );
// 	if ( abs(Zx1-Zc) > ZThreshold ) Zx1 = Zc;
// 	float4	Zy0 = SourceBuffer.SampleLevel( LinearClamp, UV - 1.5 * BufferInvSize.zy, 0.0 );
// 	if ( abs(Zy0-Zc) > ZThreshold ) Zy0 = Zc;
// 	float4	Zy1 = SourceBuffer.SampleLevel( LinearClamp, UV + 1.5 * BufferInvSize.zy, 0.0 );
// 	if ( abs(Zy1-Zc) > ZThreshold ) Zy1 = Zc;
// 
// 	float3	Dx = float3( NormalSmooth * BufferInvSize.x, 0.0, Zx1 - Zx0 );
// 	float3	Dy = float3( 0.0, NormalSmooth * BufferInvSize.y, Zy1 - Zy0 );
// 	float3	N = normalize( cross( Dx, Dy ) );

// 	float3	N = Zc.xyz;
// 	return float4( 0.5 * (1.0 + N), 0.0 );

//	return 0.08 * Zc.wwww;
//	return float4( 0.5 * (1.0+SourceBuffer.SampleLevel( LinearClamp, UV, 0.0 ).xyz), 0.0 );
//	return float4( 0.5 * (1.0+Zc.xyz), 0.0 );
	return EnvMap.Sample( LinearClamp, Zc.xyz );
}

// ===================================================================================
technique10 Dynamics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Dynamics() ) );
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
