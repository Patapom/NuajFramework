// Skin shader that uses sub-surface scattering to render skin
// (cf. GPU Gems 3 : Part III Chapter 14 on skin rendering)
//
// This shader is the 3rd and last pass of the algorithm.
// _ First pass renders the irradiance map in UV space
// _ Second pass downscales the irradiance map into several (4) smaller and smoother maps
// _ Third pass recombines these scaled down smoothed versions of the irradiance with different
//		weights that fit a diffusion profile for RGB wavelength in a typical caucasian white skin
//
float4x4	Local2World : LOCAL2WORLD;

#include "DirectionalLighting.fx"
#include "DirectionalLighting2.fx"
#include "LinearToneMapping.fx"
#include "Camera.fx"
#include "ShadowMapSupport.fx"

float4		AmbientColor;
Texture2D	DiffuseTexture : TEX_DIFFUSE;
Texture2D	NormalTexture : TEX_NORMAL;
bool		HasNormalTexture;

float		NormalAmplitude = 1.0f;
float		DiffusionDistance = 32.0f;

// Irradiance textures
Texture2D	IrradianceTexture0 : TEX_IRRADIANCE0;
Texture2D	IrradianceTexture1 : TEX_IRRADIANCE1;
Texture2D	IrradianceTexture2 : TEX_IRRADIANCE2;
Texture2D	IrradianceTexture3 : TEX_IRRADIANCE3;
Texture2D	IrradianceTexture4 : TEX_IRRADIANCE4;

float		GlobalRoughnessFactor = 1.0;
float		GlobalSpecularShininessFactor = 5.0;


SamplerState DiffuseTextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float3	Position : POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
	float3	WorldPosition : TEXCOORD1;
};

//////////////////////////////////////////////////////////////////////////////
// Irradiance rendering
//
PS_IN VS_Irradiance( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );

	PS_IN	Out;
	Out.WorldPosition = WorldPosition.xyz;
	Out.Position = float4( 2*_In.UV.x-1, 1-2*_In.UV.y, 0, 1 );	// We render in UV space here...
	Out.Normal = mul( float4( _In.Normal, 0 ), Local2World ).xyz;
	Out.Tangent = mul( float4( _In.Tangent, 0 ), Local2World ).xyz;
	Out.BiTangent = mul( float4( _In.BiTangent, 0 ), Local2World ).xyz;
	Out.UV = _In.UV;

	return	Out;
}

// Computes the Fresnel reflectance
// H is the standard half-angle vector. F0 is reflectance at normal incidence (for skin, we use 0.028).
//
float FresnelReflectance( float3 H, float3 V, float F0 )
{
	float base = 1.0 - dot( V, H );
	float exponential = base*base;
	exponential *= exponential;	// ^4
	exponential *= base;		// ^5

	return exponential + F0 * (1.0 - exponential);
}

// Computes Beckmann distribution function
//
float PHBeckmann( float ndoth, float m )
{
	float	sa = sqrt( 1 - ndoth*ndoth );
	float	ta = sa / ndoth;	// tan( ndoth )
	float	ndoth4 = ndoth*ndoth;
			ndoth4 *= ndoth4;

	return exp( -(ta*ta) / (m*m) ) / (m*m*ndoth4);
}

// Computes Kelemen/Szirmay-Kalos Specular
// Default values : m=0.3 & RhoS=0.18
//
float ComputeSkinSpecular(	float3 N,	// Bumped surface normal
							float3 L,	// Points to light
							float3 V,	// Points to eye
							float m,	// Roughness
							float rho_s	// Specular brightness
							)
{
	float	ndotl = dot( N, L );
	if ( ndotl < 0.0 )
		return 0.0;

	float3	h = L + V; // Unnormalized half-way vector
	float3	H = 0.5 * ( h );
	float	ndoth = dot( N, H );

	float	PH = PHBeckmann( ndoth, m );
	float	F = FresnelReflectance( H, V, 0.028 );
	float	frSpec = max( PH * F / dot( h, h ), 0 );

	return saturate( ndotl * rho_s * frSpec ); // BRDF * dot(N,L) * rho_s
}

// Encapsulated specular factor computation
// This tweaks a specular factor in [0,1] from a texture into an approximate roughness and specular brightness
//
float	ComputeSpecular( float3 N, float3 L, float3 V, float fSpecularFactor )
{
	float	fRoughness = clamp( 0.9-GlobalRoughnessFactor * fSpecularFactor, 0.05, 0.5 );
	float	fSpecularBrightness = GlobalSpecularShininessFactor * fSpecularFactor;
	return ComputeSkinSpecular( N, L, V, fRoughness, fSpecularBrightness );
}

// Computes a normal from a normal map
//
float3	ComputeNormal( float3 _T, float3 _B, float3 _N, float2 _UV )
{
	if ( !HasNormalTexture )
		return _N;

	float3	NormalSample = 2.0 * NormalTexture.Sample( DiffuseTextureSampler, _UV ).xyz - 1.0;
		
	// Compute normal from normal map
	return normalize( NormalAmplitude * NormalSample.x * _T + NormalAmplitude * NormalSample.y * _B + NormalSample.z * _N );
}

// Computes translucency approximation based on shadow length
//	_LightColor, the intensity of the light to compute translucency for
//	_Thickness, the thickness of the material the light is traversing
//
float3	ComputeSkinTranslucency( float3 _LightColor, float3 _LightDirection, float3 _Normal, float _Thickness )
{
//	return 8 * (1 - saturate( 0.5 * _Thickness ));

	// Extinction coefficients (from "Light Diffusion in Multi-Layered Translucent Materials" by Jensen et Al.)
	// Epidermis 
// 	float3	Sigma_a = float3( 2.1, 2.1, 5.0 );
// 	float3	Sigma_s = float3( 48.0, 60.0, 65.0 );
 	// Upper Dermis
	float3	Sigma_a = float3( 0.16, 0.19, 0.30 );
	float3	Sigma_s = float3( 32.0, 40.0, 46.0 );
	// Bloody Dermis
// 	float3	Sigma_a = float3( 0.085, 1.0, 25.0 );
// 	float3	Sigma_s = float3( 4.5, 4.7, 4.8 );

	// Apply scale factor for our scaled model
	_Thickness = 2 * 0.125 * _Thickness;

	// Compute energy attenuation due to extinction (absorption+scattering)
	float3	Sigma_t = Sigma_a + Sigma_s;
	float3	Absorption = exp( -Sigma_t * _Thickness );

	// Compute energy increase due to in-scattering
	float3	Scattering = _Thickness * Sigma_s * Absorption;//(1.0 - exp( -Sigma_s * _Thickness ));

	return abs( dot( _Normal, _LightDirection ) ) * _LightColor * (Absorption + Scattering);
}

float4 PS_Irradiance( PS_IN _In ) : SV_Target
{
	float3	Normal = normalize( _In.Normal );
	float3	Tangent = normalize( _In.Tangent );
	float3	BiTangent = normalize( _In.BiTangent );
	Normal = ComputeNormal( Tangent, BiTangent, Normal, _In.UV );

	float3	ToCamera = Camera2World[3].xyz - _In.WorldPosition;
	float	fDistance2View = length( ToCamera );
			ToCamera /= fDistance2View;

	// Compute shadowing
	float3	ShadowCasterPosition = GetShadowCasterPosition( _In.WorldPosition );
	float	fShadowLength = length( _In.WorldPosition - ShadowCasterPosition );
// 	return 1 * fShadowLength;
// 	return float4( ShadowCasterPosition, 1 );
//	bool	bIsInShadow = IsInShadow( _In.WorldPosition );

	// Compute specular factor
	float	fSpecular = DiffuseTexture.Sample( DiffuseTextureSampler, _In.UV ).a;
	float	fSpecularFactor0 = ComputeSpecular( Normal, LightDirection, ToCamera, fSpecular );
	float	fSpecularFactor1 = ComputeSpecular( Normal, LightDirection2, ToCamera, fSpecular );

	// Compute diffuse factor
	float	fDotLight = dot( Normal, LightDirection );
	float4	DiffuseColor0 = float4( saturate( fDotLight ) * LightColor.rgb, 1 );
	float4	DiffuseColor1 = float4( saturate( dot( Normal, LightDirection2 ) ) * LightColor2.rgb, 1 );

// 	if ( fShadowLength > 0.1 )
// 	{	// Compute material translucency
// 		if ( fDotLight < 0.0 )
// 			DiffuseColor0 = float4( ComputeSkinTranslucency( LightColor.rgb, LightDirection, Normal, fShadowLength ), 1 );
// 		else
// 			DiffuseColor0 = 0.0;	// Simple cast shadow...
// 		fSpecularFactor0 = 0.0;
// 	}

	return float4( (AmbientColor + (1-fSpecularFactor0) * DiffuseColor0 + (1-fSpecularFactor1) * DiffuseColor1).rgb, 1 );
}

//////////////////////////////////////////////////////////////////////////////
// Actual rendering
//
PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );

	PS_IN	Out;
	Out.WorldPosition = WorldPosition.xyz;
	Out.Position = mul( WorldPosition, World2Proj );
	Out.Normal = mul( float4( _In.Normal, 0 ), Local2World ).xyz;
	Out.Tangent = mul( float4( _In.Tangent, 0 ), Local2World ).xyz;
	Out.BiTangent = mul( float4( _In.BiTangent, 0 ), Local2World ).xyz;
	Out.UV = _In.UV;

	return	Out;
}

// These are the RGB weights for gaussians with varying variance
// used to fit the caucasian skin light diffusion profile...
//
float3	GaussianWeightsRGB[] =
{
	float3( 0.233, 0.455, 0.649 ),	// Variance = 0.0064 mm²
	float3( 0.100, 0.336, 0.344 ),	// Variance = 0.0484 mm²
	float3( 0.118, 0.198, 0.000 ),	// Variance = 0.1870 mm²
	float3( 0.113, 0.007, 0.007 ),	// Variance = 0.5670 mm²
	float3( 0.358, 0.004, 0.000 ),	// Variance = 1.9900 mm²
	float3( 0.078, 0.000, 0.000 ),	// Variance = 7.4100 mm²
};

float4 PS( PS_IN _In ) : SV_Target
{
	float3	Normal = normalize( _In.Normal );
	float3	Tangent = normalize( _In.Tangent );
	float3	BiTangent = normalize( _In.BiTangent );
	Normal = ComputeNormal( Tangent, BiTangent, Normal, _In.UV );

	float3	ToCamera = Camera2World[3].xyz - _In.WorldPosition;
	float	fDistance2View = length( ToCamera );
			ToCamera /= fDistance2View;

	// Sample diffuse color
	float4	DiffuseColor = DiffuseTexture.Sample( DiffuseTextureSampler, _In.UV );

	// Compute specular factor
	float	fSpecularFactor = ComputeSpecular( Normal, LightDirection, ToCamera, DiffuseColor.a );
//	return float4( fSpecularFactor.xxx, 1 );

	// Correct factors
	float	fFactor0 = saturate( DiffusionDistance );
	float	fFactor1 = DiffusionDistance > 1.0 ? saturate(0.5*(DiffusionDistance-1.0)) : 0.0;
	float	fFactor2 = DiffusionDistance > 3.0 ? saturate(0.25*(DiffusionDistance-3.0)) : 0.0;
	float	fFactor3 = DiffusionDistance > 7.0 ? saturate(0.125*(DiffusionDistance-7.0)) : 0.0;
	float	fFactor4 = DiffusionDistance > 15.0 ? saturate(0.0625*(DiffusionDistance-15.0)) : 0.0;

	// Compute diffuse factor
	float3	DiffuseIrradiance = 0;
	DiffuseIrradiance += fFactor0 * GaussianWeightsRGB[0] * IrradianceTexture0.Sample( DiffuseTextureSampler, _In.UV ).rgb;
	DiffuseIrradiance += fFactor1 * GaussianWeightsRGB[1] * IrradianceTexture1.Sample( DiffuseTextureSampler, _In.UV ).rgb;
	DiffuseIrradiance += fFactor2 * GaussianWeightsRGB[2] * IrradianceTexture2.Sample( DiffuseTextureSampler, _In.UV ).rgb;
	DiffuseIrradiance += fFactor3 * GaussianWeightsRGB[3] * IrradianceTexture3.Sample( DiffuseTextureSampler, _In.UV ).rgb;
	DiffuseIrradiance += fFactor4 * GaussianWeightsRGB[4] * IrradianceTexture4.Sample( DiffuseTextureSampler, _In.UV ).rgb;
//	DiffuseIrradiance += GaussianWeightsRGB[5] * IrradianceTexture5.Sample( DiffuseTextureSampler, _In.UV ).rgb;	// We don't have that one...

	// Renormalize to white irradiance
	float3	fSumWeights = fFactor0 * GaussianWeightsRGB[0]
						+ fFactor1 * GaussianWeightsRGB[1]
						+ fFactor2 * GaussianWeightsRGB[2]
						+ fFactor3 * GaussianWeightsRGB[3]
						+ fFactor4 * GaussianWeightsRGB[4];
	DiffuseIrradiance /= fSumWeights;

	// Determine skin color from a diffuseColor map
	DiffuseIrradiance *= DiffuseColor.rgb;

	return float4( ApplyToneMapping( DiffuseIrradiance + fSpecularFactor * LightColor.rgb ), 1 );
}


//////////////////////////////////////////////////////////////////////////////
// DEBUG
int		DebugType = 1;
float4	BufferInvSize;

struct PS_IN_DEBUG
{
	float4	Position : SV_POSITION;
};

PS_IN_DEBUG	VS_DEBUG( PS_IN_DEBUG _In )
{
	return _In;
}

float4	PS_DEBUG( PS_IN_DEBUG _In ) : SV_TARGET
{
	return _In.Position * BufferInvSize;
}


//////////////////////////////////////////////////////////////////////////////

technique10 IrradianceComputation
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Irradiance() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Irradiance() ) );
	}
}

technique10 SkinRendering
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 Debug
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_DEBUG() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_DEBUG() ) );
	}
}
