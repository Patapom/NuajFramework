// Lens-Flare compositing
// This technique performs the final compositing of the lens-flare buffer with the actual render target
// It handles the additive/screen blending as well as general other modifiers
//

// ===================================================================================
#include "PostProcessLensFlareCommon.fx"

Texture2D	SourceTexture;		// Buffer in which the lens-flare was rendered

bool		bScreenMode;

// An optional lens texture revealed by the lights
bool		bUseLensTexture;
Texture2D	LensTexture;
float		IlluminationRadius;
float		LensTextureFallOff;
float		LensTextureBrightness;
float2		LensTextureScale;
float2		LensTextureOffset;

// Chromatic aberration
int			AberrationType;		// None/PurpleFringe/RedBlueShift
float		AberrationIntensity;
float		AberrationSpread;

// Color correction
float		CCBrightness;
float		CCContrast;
float		CCSaturation;


// ===================================================================================
// BLENDING
// ===================================================================================
//
// Blending is a simple additive mode of the source lens-flare image
// Anyway, the source lens-flare image is not as easy as it can come with chromatic aberration
//	and lens texture that gets revealed by light in front of it (i.e. dirt on the lens)
//
struct VS_IN
{
	float4	Position	: SV_POSITION;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// Those bastards apply color correction on their lens textures ! ^^
// I tried to replicate their settings using photoshop and it comes quite right with a desaturation and a curve filter
float3	SampleLensTexture( float2 _UV )
{
	float3	Color = LensTexture.SampleLevel( LinearBorder, _UV, 0 ).xyz;
// 	Color = RGB2HSL( Color );
// //	Color.y = max( 0.0, Color.y - 0.5 );				// De-saturate
// 	Color.z = max( 0.0, (Color.z - 0.125) * 1.225 );	// Level
// 	Color.z *= Color.z;									// Gamma
// 	return HSL2RGB( Color );

	// This seems to do the job equally well ! ^^
	float	Lum = dot( Color, float3( 0.3, 0.5, 0.2 ) );
	Color /= Lum;
	Lum = max( 0.0, (Lum - 0.05) * 1.1 );	// Level
	Lum *= Lum;

	return Color * Lum;
}

float4	PS_Blend( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * InvSourceSize.xy;

	float3	Result = 0.0;
	if ( AberrationType == 0 )
	{	// No aberration => Single sampling

		// Sample lens-flare buffer
		float4	Color = SourceTexture.SampleLevel( LinearClamp, UV, 0 );
		Result = Color.xyz;
		if ( bScreenMode )
			Result.xyz = 1.0 - Result.xyz;	// Final complement for perfect screen mode

		// Apply lens texture
		if ( bUseLensTexture )
		{
			float2	LensUV = 0.5 + (UV - 0.5) / LensTextureScale - 0.01 * LensTextureOffset;
			float3	LensColor = SampleLensTexture( LensUV );
			Result += LensTextureBrightness * Color.w * LensColor;	// Lens texture is modulated by the lens-flare texture's alpha that was rendered with lights' influence
		}
	}
	else if ( AberrationType == 1 )
	{	// Purple Fringe => Sample in 2 places
		float	AberrationPower = length( UV - 0.5 );
		float2	UVAbb = 0.5 + (UV - 0.5) * (AberrationPower - 0.002 * AberrationSpread) / AberrationPower;

		// Sample lens-flare buffer
		float4	Color0 = SourceTexture.SampleLevel( LinearClamp, UV, 0 );
		if ( bScreenMode )
			Color0.xyz = 1.0 - Color0.xyz;	// Final complement for perfect screen mode
		float4	Color1 = SourceTexture.SampleLevel( LinearClamp, UVAbb, 0 );
		if ( bScreenMode )
			Color1.xyz = 1.0 - Color1.xyz;	// Final complement for perfect screen mode

		// Apply lens texture
		if ( bUseLensTexture )
		{
			float2	LensUV = 0.5 + (UV - 0.5) / LensTextureScale - 0.01 * LensTextureOffset;
			float3	LensColor0 = SampleLensTexture( LensUV );
			Color0.xyz += LensTextureBrightness * Color0.w * LensColor0;	// Lens texture is modulated by the lens-flare texture's alpha that was rendered with lights' influence

			LensUV = 0.5 + (UVAbb - 0.5) / LensTextureScale - 0.01 * LensTextureOffset;
			float3	LensColor1 = SampleLensTexture( LensUV );
			Color1.xyz += LensTextureBrightness * Color0.w * LensColor1;	// Lens texture is modulated by the lens-flare texture's alpha that was rendered with lights' influence
		}

		// Purple fringe => Purple gets away from current pixel
		Result = lerp( Color0.xyz, float3( Color1.x, Color0.y, Color1.z ), AberrationIntensity );
	}
	else
	{	// Red/Blue Shift => Sample in 3 places
		float	AberrationPower = length( UV - 0.5 );
		float2	UVAbbGreen = 0.5 + (UV - 0.5) * (AberrationPower - 0.0015 * AberrationSpread) / AberrationPower;
		float2	UVAbbBlue = 0.5 + (UV - 0.5) * (AberrationPower - 0.003 * AberrationSpread) / AberrationPower;

		// Sample lens-flare buffer
		float4	Color0 = SourceTexture.SampleLevel( LinearClamp, UV, 0 );
		if ( bScreenMode )
			Color0.xyz = 1.0 - Color0.xyz;	// Final complement for perfect screen mode
		float4	Color1 = SourceTexture.SampleLevel( LinearClamp, UVAbbGreen, 0 );
		if ( bScreenMode )
			Color1.xyz = 1.0 - Color1.xyz;	// Final complement for perfect screen mode
		float4	Color2 = SourceTexture.SampleLevel( LinearClamp, UVAbbBlue, 0 );
		if ( bScreenMode )
			Color2.xyz = 1.0 - Color2.xyz;	// Final complement for perfect screen mode

		// Apply lens texture
		if ( bUseLensTexture )
		{
			float2	LensUV = 0.5 + (UV - 0.5) / LensTextureScale - 0.01 * LensTextureOffset;
			float3	LensColor0 = SampleLensTexture( LensUV );
			Color0.xyz += LensTextureBrightness * Color0.w * LensColor0;	// Lens texture is modulated by the lens-flare texture's alpha that was rendered with lights' influence

			LensUV = 0.5 + (UVAbbGreen - 0.5) / LensTextureScale - 0.01 * LensTextureOffset;
			float3	LensColor1 = SampleLensTexture( LensUV );
			Color1.xyz += LensTextureBrightness * Color0.w * LensColor1;	// Lens texture is modulated by the lens-flare texture's alpha that was rendered with lights' influence

			LensUV = 0.5 + (UVAbbBlue - 0.5) / LensTextureScale - 0.01 * LensTextureOffset;
			float3	LensColor2 = SampleLensTexture( LensUV );
			Color2.xyz += LensTextureBrightness * Color0.w * LensColor2;	// Lens texture is modulated by the lens-flare texture's alpha that was rendered with lights' influence
		}

		// Red/Blue Shift
		Result = lerp( Color0.xyz, float3( Color0.x, Color1.y, Color2.z ), AberrationIntensity );
	}

	// Apply color correction
	return float4( Result, 0.0 );

	float3	HSL = RGB2HSL( Result );
	HSL.y += CCSaturation;
	HSL.z = CCBrightness + 0.5 + (HSL.z-0.5) * (1.0 + CCContrast);
	return float4( HSL2RGB( HSL ), 0.0 );
}

float4	PS_Light( VS_IN _In ) : SV_TARGET0
{
	float2	Position = 100.0 * _In.Position.xy * InvSourceSize;
	float	Distance2Light = length( Position - LightPosition );
	float	NormalizedDistance = Distance2Light / IlluminationRadius;
	clip( 1.0 - NormalizedDistance );
	return smoothstep( 1.0, 1.0 - LensTextureFallOff, sqrt(NormalizedDistance) );
}

// ===================================================================================
//
technique10 DisplayBlend
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Blend() ) );
	}
}

technique10 DisplayLightsAlpha
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Light() ) );
	}
}
