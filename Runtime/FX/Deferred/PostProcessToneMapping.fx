// Tone-mapping post-process
// This technique applies "filmic curve" tone mapping as described by John Hable
//  in http://filmicgames.com/archives/75#more-75 or in his GDC talk about tone mapping
//  in Uncharted 2 (http://www.gdcvault.com/play/1012459/Uncharted_2__HDR_Lighting)
// The filmic curve is a S-shaped curve that has been used for decades by the film
//  industry (i.e. Kodak or Fuji, not Holywood) as the "film impression" response curve.
// For example, consult : http://i217.photobucket.com/albums/cc75/nikonf2/scurve.jpg
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"

// Source texture
float3		SourceInfos;		// XY=1/SourceTexture Z=0.0
Texture2D	SourceTexture;
Texture2D	AverageLuminanceTexture;

bool		EnableToneMapping = true;
bool		EnableGlow = false;
float4		Params;				// X=Sub-Pixel Sampling Offset (default is 0)
								// Y=Max Luminance Level (default is 1.2)
								// Z=Temporal Adaptation Speed (default is 0.7)
								// W=1.0/Gamma (default is 1.0/2.2)
float2		AdaptationLevels;	// XY=Min/Max adaptation levels (defaults are 0.8 and 20.0)
float4		GlowParams;			// X=LuminanceThreshold
								// Y=GlowFactor
								// Z=WhiteValue
								// W=Offset

struct VS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{	
	return	_In;
}

// ===================================================================================
// Downsampling

float4 PS_DownSampleLog( PS_IN _In ) : SV_TARGET0
{
//	float3	Color = SourceTexture.SampleLevel( LinearClamp, _In.UV + Params.x * SourceInfos.xy, 0 ).xyz;
	float3	Color = SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 ).xyz;
	float	Luminance = max( 0.0, dot( Color, LUMINANCE_WEIGHTS ) );
	float	LogLuminance = log( 1e-4 + Luminance );
	return float4( Color, LogLuminance );
}

float4 PS_DownSample( PS_IN _In ) : SV_TARGET0
{
	// Single sample version
//	return SourceTexture.SampleLevel( LinearClamp, _In.UV + Params.x * SourceInfos.xy, 0 );

	// Small 9 taps gaussian
	float	Weights[3][3] = {
		{ 0.1, 0.3, 0.1 },
		{ 0.3, 1.0, 0.3 },
		{ 0.1, 0.3, 0.1 } };
// 		{ 1.0, 2.0, 1.0 },
// 		{ 2.0, 4.0, 2.0 },
// 		{ 1.0, 2.0, 1.0 } };
	float4	SumColor = 0.0.xxxx;
	float	SumWeight = 0.0;
	for ( int i=0;i<9;i++ )
	{
		float	Y = i / 3;
		float	X = i % 3;

		float	Weight = Weights[X][Y];
		SumColor += Weight * SourceTexture.SampleLevel( LinearClamp, _In.UV + Params.x * float2( (X-1) * SourceInfos.x, (Y-1) * SourceInfos.y ), 0 );
		SumWeight += Weight;
	}

	return SumColor / SumWeight;
}

// ===================================================================================
// TemporalAdaptation

float PS_TemporalAdaptation( PS_IN _In ) : SV_TARGET0
{
	// Read adapted luminance from last frame
	float	AverageLuminanceCurrent = AverageLuminanceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 ).x;

	// Read our currently perceived average luminance
	float	AverageLuminanceTarget = exp( SourceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 ).w );

	// Clamp perceived luminance as the eye cannot adapt to very low or very bright levels
	AverageLuminanceTarget = max( AdaptationLevels.x, AverageLuminanceTarget );
	AverageLuminanceTarget = min( AdaptationLevels.y, AverageLuminanceTarget );

	// Try and reach the average luminance
	float	fDeltaTime = 1.0 / 30.0;
	float	fTemporalAdaptationSpeed = Params.z;

 	// The user's adapted luminance level is simulated by closing the gap between adapted luminance and current luminance by 2% every frame, based on a 30 fps rate.
	// This is not an accurate model of human adaptation, which can take longer than half an hour.
	return AverageLuminanceCurrent + (AverageLuminanceTarget - AverageLuminanceCurrent) * (1.0 - pow( 1.0 - fTemporalAdaptationSpeed, fDeltaTime ));
}

// ===================================================================================
// Tone Mapping
PS_IN VS_ToneMap( VS_IN _In )
{	
	// Read average luminance once for the entire quad
	_In.View.x = AverageLuminanceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 ).x;

	return	_In;
}

float A = 0.15;			// A = Shoulder Strength
float B = 0.50;			// B = Linear Strength
float C = 0.10;			// C = Linear Angle
float D = 0.20;			// D = Toe Strength
float E = 0.02;			// E = Toe Numerator
float F = 0.30;			// F = Toe Denominator
						// (Note: E/F = Toe Angle)
float W = 11.2;			// LinearWhite = Linear White Point Value

float3 ToneMapColor( float3 _In )
{
   return ((_In*(A*_In+C*B) + D*E) / (_In*(A*_In+B) + D*F)) - E/F;
}

// RGB -> XYZ conversion 
// http://www.w3.org/Graphics/Color/sRGB 
// The official sRGB to XYZ conversion matrix is (following ITU-R BT.709)
// 0.4125 0.3576 0.1805
// 0.2126 0.7152 0.0722 
// 0.0193 0.1192 0.9505 
//
float3 RGB2xyY( float3 _RGB )
{
	const float3x3 RGB2XYZ = {
		0.5141364, 0.3238786, 0.16036376,
		0.265068, 0.67023428, 0.06409157,
		0.0241188, 0.1228178, 0.84442666 };

	float3 XYZ = mul( RGB2XYZ, _RGB ); 

	// XYZ -> Yxy conversion
	float3 xyY;
	xyY.z = XYZ.g; 
 
	// x = X / (X + Y + Z) 
	// y = X / (X + Y + Z) 
	xyY.rg = XYZ.xy / dot( 1.0.xxx, XYZ );

	return xyY;
}

// XYZ -> RGB conversion 
// The official XYZ to sRGB conversion matrix is (following ITU-R BT.709) 
// 3.2410 -1.5374 -0.4986
// -0.9692 1.8760 0.0416 
// 0.0556 -0.2040 1.0570 
//
float3	xyY2RGB( float3 _xyY )
{
	const float3x3 XYZ2RGB = {
		2.5651, -1.1665, -0.3986,
		-1.0217, 1.9777, 0.0439,
		0.0753, -0.2543, 1.1892 };

	// xyY -> XYZ conversion
	float3	XYZ;
	XYZ.y = _xyY.z;
	XYZ.x = _xyY.x * _xyY.z / _xyY.y;					// X = x * Y / y
	XYZ.z = (1.0 - _xyY.x - _xyY.y) * _xyY.z / _xyY.y;	// Z = (1-x-y) * Y / y

	// RGB conversion
	return mul( XYZ2RGB, XYZ );
}

float4 PS_ToneMap( PS_IN _In ) : SV_TARGET0
{
	float3	SourceColor = SourceTexture.SampleLevel( LinearClamp, _In.UV, 0.0 ).xyz;
	float	AverageLuminance = _In.View.x;
//	return float4( Params.y * AverageLuminance.xxx, 1.0 );

// DEBUG LUMINANCE
// return SourceTexture.SampleLevel( LinearClamp, _In.UV, 5.0 ).wwww;
// return exp( SourceTexture.SampleLevel( LinearClamp, _In.UV, 5.0 ).w ).xxxx;

	if ( !EnableToneMapping )
		return float4( pow( SourceColor, Params.w ), 1.0 );

	float	MiddleGrey = 1.03 - 2.0 / (2.0 + log10( 1.0 + AverageLuminance ));	// From eq. 10 in http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering

	// ===========================================================================
// 	// Add some glow for "free"
// 	float3	GlowColor  = 4.0 * SourceTexture.SampleLevel( LinearClamp, _In.UV, 3.0 ).xyz;
// 			GlowColor += 1.0 * SourceTexture.SampleLevel( LinearClamp, _In.UV - 4.0 * SourceInfos.xz, 3.0 ).xyz;
// 			GlowColor += 1.0 * SourceTexture.SampleLevel( LinearClamp, _In.UV + 4.0 * SourceInfos.xz, 3.0 ).xyz;
// 			GlowColor += 1.0 * SourceTexture.SampleLevel( LinearClamp, _In.UV - 4.0 * SourceInfos.zy, 3.0 ).xyz;
// 			GlowColor += 1.0 * SourceTexture.SampleLevel( LinearClamp, _In.UV + 4.0 * SourceInfos.zy, 3.0 ).xyz;

	float3	GlowColor  = SourceTexture.SampleLevel( LinearClamp, _In.UV, 3.0 ).xyz;
			// Basic stuff
			GlowColor = max( 0.0, GlowColor - GlowParams.x );
			// Reinhard stuff
// 			GlowColor *= 2.0 * MiddleGrey / AverageLuminance;
// 			GlowColor = max( 0.0, GlowColor * (1.0 + GlowColor / (GlowParams.z * GlowParams.z)) - GlowParams.x );
// 			GlowColor = GlowColor / (GlowParams.w + GlowColor);
//	SourceColor += GlowParams.y * GlowColor;
// DEBUG GLOW
//return float4( GlowParams.y * GlowColor, 1.0 );

	// ===========================================================================
	// Apply tone mapping
	float3	ToneMappedColor = SourceColor;

	if ( false )
	{	// RGB Version
		SourceColor *= MiddleGrey / AverageLuminance;	// Exposure correction
		ToneMappedColor = ToneMapColor( Params.y * SourceColor ) / ToneMapColor( W );
	}
	else
	{	// xyY Version (colors are much more vivid)
		float3	xyY = RGB2xyY( SourceColor );
				xyY.z *= MiddleGrey / AverageLuminance;								// Exposure correction
				xyY.z = (ToneMapColor( Params.y * xyY.zzz ) / ToneMapColor( W )).x;	// Tone mapped luminance
		ToneMappedColor = xyY2RGB( xyY );
	}

	// Gamma correction
	float3	FinalColor = pow( ToneMappedColor, Params.w );	// Gamma corrected LDR color...

	// ===========================================================================
	// Apply glow AFTER tone mapping
	if ( EnableGlow )
		FinalColor += GlowParams.y * GlowColor;

	return float4( FinalColor, 1.0 );
}

// ===================================================================================
//
technique10 DownSampleFirstStage
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_DownSampleLog() ) );
	}
}

technique10 DownSample
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_DownSample() ) );
	}
}

technique10 TemporalAdaptation
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_TemporalAdaptation() ) );
	}
}

technique10 ToneMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_ToneMap() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ToneMap() ) );
	}
}
