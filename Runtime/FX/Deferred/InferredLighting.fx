// Performs the light accumulation in a downscaled light buffer
// This shader supports omni, spot and (TODO: directional lights)
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"
#include "Utility/SHEnvMap/SHSupport.fx"


// check source code inferred from http://mynameismjp.wordpress.com/2010/01/10/inferred-rendering/
// for better isolation of pixels => edge removals


// Standard render parameters for lights
cbuffer	PerLight
{
	float3		LightPosition;		// Position in CAMERA space (Only for omni & spots)
	float3		LightDirection;		// Direction in CAMERA space (Only for spots & directionals)
	float3		LightColor;
	float4		LightParams;		// Packed parameters for light
									// Omnis have : X=Inner Radius Y=OuterRadius
									// Spots have : X=Inner Radius Y=OuterRadius Z=cos(0.5*InnerAngle) W=cos(0.5*OuterAngle)
									// Directionals have : Nothing really
	float4		LightParams2;		// 2nd set of Packed parameters for light
									// Spots have : X=ConeRadius YZW=0
}

float4	ScreenInfos;				// XY = 1 / ScreenSize

struct VS_IN
{
	float3	Position		: POSITION;
};

struct VS_IN_ScreenQuad
{
	float4	Position		: SV_POSITION;
	float3	View			: VIEW;
	float2	UV				: TEXCOORD0;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
};

////////////////////////////////////////////////////////////////////////
// Render omnis
// Omnis are drawn as face-cam 2D discs instead of spheres
//
PS_IN VS_Omni( VS_IN _In )
{
	float3	Position = LightPosition;

	// Project sphere radius to obtain 2D disc radius
	float	Radius = 1.0f;
	if ( LightPosition.z > LightParams.y )
		Radius = LightParams.y * LightPosition.z / sqrt( max( 1e-3, LightPosition.z*LightPosition.z - LightParams.y*LightParams.y ) );
	else
		Position = float3( 0.0, 0.0, CameraData.z );	// Center light on camera and use radius 1 that should cover the whole screen

	// Expand unit disc
	Position.x += Radius * _In.Position.x;
	Position.y += Radius * _In.Position.y;
	Position.z = max( CameraData.z, Position.z );	// Push Z to near clip

	// Project
	PS_IN	Out;
	Out.Position = mul( float4( Position, 1.0 ), Camera2Proj );

	return Out;
}

float4 PS_Omni( PS_IN _In ) : SV_TARGET0
{
	// Read normal and depth at current pixel
	float2	UV = _In.Position.xy * ScreenInfos.xy;
	float3	Normal;
	float	Depth, SpecularPower;
	ReadDeferredMRTNormalDepthSpec( UV, Normal, Depth, SpecularPower );

	// Rebuild 3D position in CAMERA space
	float3	Position = float3( (2.0 * UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * UV.y) * CameraData.x, 1.0 ) * Depth;

	// =======================================
	// Perform lighting
	float3	ToPixel = Position - LightPosition;
	float	Distance2Light = length( ToPixel );
	clip( LightParams.y - Distance2Light );

	// Radial attenuation based on distance to light
	float	AttenuationRadial = saturate( (Distance2Light - LightParams.y) / (LightParams.x - LightParams.y) );
	AttenuationRadial *= AttenuationRadial;	// Square attenuation
	float	Light = AttenuationRadial;
	clip( Light - 1e-3 );

	ToPixel /= Distance2Light;	// Normalize vector to pixel

	// Attenuation based on angle between normal and light
	float	DotLightNormal = saturate( dot( Normal, -ToPixel ) );
	Light *= DotLightNormal;
	clip( Light - 1e-3 );

	// Specular reflection
	float3	LightReflection = reflect( ToPixel, Normal );
	float3	ToCamera = normalize( -Position );
	float	DotLightView = saturate( dot( LightReflection, ToCamera ) );
	float	Specular = pow( DotLightView, SpecularPower );

	// Accumulate light
	return Light * float4( LightColor, Specular );
}


////////////////////////////////////////////////////////////////////////
// Render spot lights
// Spot lights are drawn as cones
//
PS_IN VS_Spot( VS_IN _In )
{
	float3	X = normalize( cross( LightDirection, float3( 0.0, 0.0, 1.0 ) ) );
	float3	Y = cross( LightDirection, X );

	// Retrieve actual position in camera space using spot's height and radius as multipliers for the input position
	float3	Position = LightPosition + LightParams2.x * (_In.Position.x * X + _In.Position.y * Y) + LightParams.y * LightDirection * _In.Position.z;
	Position.z = max( CameraData.z, Position.z );	// Push Z to near clip

	// Project
	PS_IN	Out;
	Out.Position = mul( float4( Position, 1.0 ), Camera2Proj );

	return Out;
}

float4 PS_Spot( PS_IN _In ) : SV_TARGET0
{
	// Read normal and depth at current pixel
	float2	UV = _In.Position.xy * ScreenInfos.xy;
	float3	Normal;
	float	Depth, SpecularPower;
	ReadDeferredMRTNormalDepthSpec( UV, Normal, Depth, SpecularPower );

//	return float4( UV, 0.0, 1.0 );
//	return float4( Normal, 1.0 );
//	return float4( 0.01 * Depth.xxx, 1.0 );
//	return float4( 0.05 * Position, 1.0 );

	// Rebuild 3D position in CAMERA space
	float3	Position = float3( (2.0 * UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * UV.y) * CameraData.x, 1.0 ) * Depth;

	// =======================================
	// Perform lighting
	float3	ToPixel = Position - LightPosition;
	float	Distance2Light = length( ToPixel );

	// Radial attenuation based on distance to light
	float	AttenuationRadial = saturate( (Distance2Light - LightParams.y) / (LightParams.x - LightParams.y) );
	AttenuationRadial *= AttenuationRadial;	// Square attenuation
	float	Light = AttenuationRadial;
	clip( Light - 1e-3 );

	ToPixel /= Distance2Light;	// Normalize vector to pixel

	// Attenuation based on angle between normal and light
	float	DotLightNormal = saturate( dot( Normal, -ToPixel ) );
	Light *= DotLightNormal;
	clip( Light - 1e-3 );

	// Angular attenuation based on angle with light
	float	DotLightDirection = dot( LightDirection, ToPixel );
	float	AttenuationAngular = saturate( (DotLightDirection - LightParams.w) / (LightParams.z - LightParams.w) );
	Light *= AttenuationAngular;
	clip( Light - 1e-3 );

	// Specular reflection
	float3	LightReflection = reflect( ToPixel, Normal );
	float3	ToCamera = normalize( -Position );
	float	DotLightView = saturate( dot( LightReflection, ToCamera ) );
	float	Specular = pow( DotLightView, SpecularPower );

	// Accumulate light
	return Light * float4( LightColor, Specular );
}

////////////////////////////////////////////////////////////////////////
// Render directionals
// Directionals are fullscreen quads
//
VS_IN_ScreenQuad VS_Directional( VS_IN_ScreenQuad _In )
{
	return _In;
}

float4 PS_Directional( VS_IN_ScreenQuad _In ) : SV_TARGET0
{
	// Read normal and depth at current pixel
	float2	UV = _In.Position.xy * ScreenInfos.xy;
	float3	Normal;
	float	Depth, SpecularPower;
	ReadDeferredMRTNormalDepthSpec( UV, Normal, Depth, SpecularPower );

	// =======================================
	// Perform lighting

	// Attenuation based on angle between normal and light
	float	DotLightNormal = dot( Normal, LightDirection );
	clip( DotLightNormal );

	// Rebuild 3D position in CAMERA space
	float3	Position = float3( (2.0 * UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * UV.y) * CameraData.x, 1.0 ) * Depth;

	// Specular reflection
	float3	CameraReflection = reflect( normalize( Position ), Normal );
	float	DotLightView = saturate( dot( LightDirection, CameraReflection ) );
	float	Specular = pow( DotLightView, SpecularPower );

	// Accumulate light
	return DotLightNormal * float4( LightColor, Specular );
}


////////////////////////////////////////////////////////////////////////
// Render SH ambient
//
VS_IN_ScreenQuad VS_AmbientSH( VS_IN_ScreenQuad _In )
{
	return _In;
}

float4 PS_AmbientSH( VS_IN_ScreenQuad _In ) : SV_TARGET0
{
	// Read normal and depth at current pixel
	float2	UV = _In.Position.xy * ScreenInfos.xy;
	float3	Normal;
	float	Depth, SpecularPower;
	ReadDeferredMRTNormalDepthSpec( UV, Normal, Depth, SpecularPower );

	// Rebuild 3D position in CAMERA space
	float3	Position = float3( (2.0 * UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * UV.y) * CameraData.x, 1.0 ) * Depth;

	// Transform back into WORLD space
	float3	WorldPosition = mul( float4( Position, 1.0 ), Camera2World ).xyz;
	float3	WorldNormal = mul( float4( Normal, 0.0 ), Camera2World ).xyz;

// Debug Delaunay
// float3	SH[9];
// GetAmbientSH( WorldPosition, SH );
// return float4( SH[0], 0.0 );

	// =======================================
	// Perform ambient SH lighting
	return float4( EvaluateAmbientSHIrradiance( WorldPosition, WorldNormal ).xyz, 0.0 );
//	return float4( EvaluateAmbientSH( WorldPosition, WorldNormal ).xyz, 0.0 );
}

// ===================================================================================
//
technique10 DrawOmniLights
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Omni() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Omni() ) );
	}
}

technique10 DrawSpotLights
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Spot() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Spot() ) );
	}
}

technique10 DrawDirectionalLights
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Directional() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Directional() ) );
	}
}

technique10 DrawAmbientSH
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_AmbientSH() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_AmbientSH() ) );
	}
}
