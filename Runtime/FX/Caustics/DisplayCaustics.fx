// This shader simply displays the caustics texture
//
#include "../Camera.fx"
#include "../DirectionalLighting.fx"
#include "../LinearToneMapping.fx"

float4x4	Local2World : LOCAL2WORLD;

// ===================================================================================

SamplerState LinearWrap
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

Texture2D CausticsTexture;

struct VS_IN
{
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float2	UV				: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );

	PS_IN	Out;
	Out.Position = mul( WorldPosition, World2Proj );
	Out.UV = _In.UV;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float	CausticIntensity = CausticsTexture.SampleLevel( LinearWrap, _In.UV, 0 ).x;
	return float4( CausticIntensity.xxx, 1 );
}


// ===================================================================================
// Backward technique from Ponce

float		Time = 0.0;
float		TriangleNominalArea = 1.0;
float2		InvTexture = float2( 1.0 / 50.0, 0.0 );
Texture2D	NormalMap0;
Texture2D	NormalMap1;

static const float	PI = 3.14159265358979;

// Reads a normal from the scrolling normal maps
//
float3	FetchNormal( float2 _UV )
{
//	float Time = 0.0;
	float2	UV0 = 0.4 * _UV + 0.01 * Time * float2( 1, -0.2 );
	float2	UV1 = 0.8 * _UV + 0.04 * Time * float2( -0.5, 0.7 );
	float3	Normal0  = 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 - InvTexture.xy, 0 ).xyz - 1.0;
			Normal0 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.xy, 0 ).xyz - 1.0;
			Normal0 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 - InvTexture.yx, 0 ).xyz - 1.0;
			Normal0 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.yx, 0 ).xyz - 1.0;
	float3	Normal1  = 2.0 * NormalMap1.SampleLevel( LinearWrap, UV1 - InvTexture.xy, 0 ).xyz - 1.0;
			Normal1 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.xy, 0 ).xyz - 1.0;
			Normal1 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 - InvTexture.yx, 0 ).xyz - 1.0;
			Normal1 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.yx, 0 ).xyz - 1.0;
	
	float3	Attenuation0 = 1.0 * float3( 1.0, 1.0, 1.5 );
	float3	Attenuation1 = 1.0 * float3( 1.0, 1.0, 1.5 );

	return normalize( Attenuation0 * Normal0 + Attenuation1 * Normal1 );
}

float4	PS2( PS_IN _In ) : SV_TARGET0
{
	static const int	SamplesThetaCount = 4;
	float	ProjectionPlaneDistance = 0.5;

	float	Energy = 0.0;
	for ( int ThetaIndex=0; ThetaIndex < SamplesThetaCount; ThetaIndex++ )
		for ( int PhiIndex=0; PhiIndex < 2*SamplesThetaCount; PhiIndex++ )
		{
			// Draw a random direction
//			float	Theta = asin( sqrt( 0.5 * float(ThetaIndex+1) / (SamplesThetaCount+1) ) );
			float	SqSinTheta = 0.125 * float(ThetaIndex+1) / (SamplesThetaCount+1);
			float	SinTheta = sqrt( SqSinTheta );
			float	CosTheta = sqrt( 1.0 - SqSinTheta );
			float	Phi = PI * PhiIndex / SamplesThetaCount;
			float3	Direction = float3( cos( Phi ) * SinTheta, sin( Phi ) * SinTheta, CosTheta );

			// Compute intersection with deformed plane
			float2	HitPosition = _In.UV + Direction.xy * ProjectionPlaneDistance / Direction.z;

			// Get normal at that position
			float3	Normal = FetchNormal( HitPosition.xy );

			// Refract ray
			float3	RefractedRay = refract( Direction, Normal, 1.33 );

			// Accumulate energy using dot product with light ray (conveniently oriented toward the Z plane)
			Energy -= RefractedRay.z;
		}
	Energy = 0.65 * Energy / (SamplesThetaCount*SamplesThetaCount);
	Energy *= Energy;
	Energy *= Energy;

 	return float4( Energy.xxx, 1 );
}


// ===================================================================================
technique10 DisplayCaustics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 DisplayCausticsBackward
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}