// Simple phong shader with texture for the terrain
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../DirectionalLighting.fx"
#include "CloudSupport.fx"

Texture2D	GroundTexture;

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
};

// ===================================================================================
// Display ground
PS_IN	VS( VS_IN _In )
{
	PS_IN	Out;
	Out.__Position = mul( float4( _In.Position, 1.0 ), World2Proj );
	Out.Position = _In.Position;
	Out.Normal = _In.Normal;

	return Out;
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
	float3	Pos = Camera2World[3].xyz;
	float3	View = _In.Position - Pos;
	float	Distance = length( View );
	View /= Distance;

	// Standard phong
	_In.Normal.y *= 0.01;

	float3	Normal = normalize( _In.Normal );
	float3	DiffuseFactor = 0.05 + 0.95 * saturate( dot( Normal, LightDirection ) );

	float2	GroundUV = 0.01 * _In.Position.xz;

	float	DetailImportance = 1.0 - saturate( (Distance - 6.0) / 20.0 );
	float	DetailStrength = 1.0 * DetailImportance;
	float3	TextureColor = (1.0 - DetailStrength) * GroundTexture.Sample( LinearWrap, GroundUV ).xyz;
			TextureColor += DetailStrength * GroundTexture.Sample( LinearWrap, 4.0 * GroundUV ).xyz;

	TextureColor *= 0.1;	// Pipo albedo

	float3	SunLight = ComputeIncomingLight( _In.Position ) * DiffuseFactor;
	float3	Lightning = ComputeLightningLightingSurface( _In.Position, Normal );

	return (SunLight + Lightning) * TextureColor;
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
