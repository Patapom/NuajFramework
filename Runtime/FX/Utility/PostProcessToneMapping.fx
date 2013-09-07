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

static const float3	LUMINANCE_WEIGHTS = float3( 0.2126, 0.7152, 0.0722 );	// RGB => Y (D65 Illuminant 2° Observer)

#define USE_LOG					// Define this to use log() values
#define APPLY_TO_LUMINANCE_ONLY	// Define this to apply tone mapping to luminance only instead of entire color

// Source texture
float3		_SourceInfos;		// XY=1/SourceTextureSize Z=0.0
Texture2D	_SourceTexture;
Texture2D	_AverageLuminanceTexture;

float4		_Params;			// X=Sub-Pixel Sampling Offset (default is 0)
								// Y=Temporal Adaptation Speed (default is 0.7)
								// Z=1.0/Gamma (default is 1.0/2.2)

struct VS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position			: SV_POSITION;
	float2	UV					: TEXCOORD0;
	float	LuminanceAverage	: LUMINANCE_AVERAGE;
	float	LuminanceMin		: LUMINANCE_MIN;
	float	LuminanceMax		: LUMINANCE_MAX;
	float	MiddleGrey			: MIDDLE_GREY;
};

VS_IN VS( VS_IN _In ) { return	_In; }


// ===================================================================================
// Color conversion

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
	xyY.z = XYZ.y; 
 
	// x = X / (X + Y + Z) 
	// y = X / (X + Y + Z) 
	xyY.xy = XYZ.xy / dot( 1.0.xxx, XYZ );

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
	return max( 0.0, mul( XYZ2RGB, XYZ ) );
}


// ===================================================================================
// Downsampling

float4 PS_DownSampleFirstStage( VS_IN _In ) : SV_TARGET0
{
	float3	Color = _SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 ).xyz;
	float	Luminance = max( 0.0, dot( Color, LUMINANCE_WEIGHTS ) );
#ifdef USE_LOG
	return log( 1e-4 + Luminance );
#else
	return Luminance;
#endif
}

float4 PS_DownSample( VS_IN _In ) : SV_TARGET0
{
	// Small 9 taps gaussian
	float	Weights[3][3] = {
		{ 0.1, 0.3, 0.1 },
		{ 0.3, 1.0, 0.3 },
		{ 0.1, 0.3, 0.1 } };
// 		{ 1.0, 2.0, 1.0 },
// 		{ 2.0, 4.0, 2.0 },
// 		{ 1.0, 2.0, 1.0 } };

	float	MinLuminance = 1e6f;
	float	MaxLuminance = -1e6f;
	float	SumLuminance = 0.0;
	float	SumWeight = 0.0;

	[unroll]
	for ( uint i=0; i < 9; i++ )
	{
		float	Y = i / 3;
		float	X = i % 3;

		float	Weight = Weights[X][Y];
		float4	Luminance = _SourceTexture.SampleLevel( LinearClamp, _In.UV + _Params.x * float2( (X-1) * _SourceInfos.x, (Y-1) * _SourceInfos.y ), 0 );

		SumLuminance += Weight * Luminance.x;
		SumWeight += Weight;

		MinLuminance = min( MinLuminance, Luminance.y );
		MaxLuminance = max( MaxLuminance, Luminance.z );
	}

	return float4( SumLuminance / SumWeight, MinLuminance, MaxLuminance, 0.0 );
}

// ===================================================================================
// TemporalAdaptation
// Slowly adapts the current luminance level across time
float	_DeltaTime;
float	_AverageOrMax;		// 0=Use average luminance 1=Use max luminance
float2	_AdaptationLevels;	// XY=Min/Max adaptation levels (defaults are 0.8 and 20.0)

float4	PS_TemporalAdaptation( VS_IN _In ) : SV_TARGET0
{
	// Read adapted luminance from last frame
	float4	LuminanceCurrent = _AverageLuminanceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 );

	// Read our currently perceived average luminance
#ifdef USE_LOG
	float4	LuminanceTarget = exp( _SourceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 ) );
#else
	float4	LuminanceTarget = _SourceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 );
#endif

	// Compute actual target luminance based on the fact we want to use the average or max luminance
	float	ActualLuminanceTarget = lerp( LuminanceTarget.x, LuminanceTarget.z, _AverageOrMax );

	// Clamp perceived luminance as the eye cannot adapt to very low or very bright levels
//	ActualLuminanceTarget = clamp( ActualLuminanceTarget, _AdaptationLevels.x, _AdaptationLevels.y );

 	// The user's adapted luminance level is simulated by closing the gap between adapted luminance and current luminance by some % every frame
	// This is not an accurate model of human adaptation, which can sometimes take longer than half an hour.
	float	fTemporalAdaptationSpeed = _Params.y;
	float	AdaptedLuminance = lerp( LuminanceCurrent.x, ActualLuminanceTarget, 1.0 - pow( abs(1.0 - fTemporalAdaptationSpeed), _DeltaTime ) );

	return float4( AdaptedLuminance, LuminanceTarget.yzw );
}

// ===================================================================================
// Tone Mapping
PS_IN VS_ToneMap( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
	Out.UV = _In.UV;

	float4	AdaptedLuminances = _AverageLuminanceTexture.SampleLevel( LinearClamp, 0.5.xx, 0 );

	Out.LuminanceAverage = AdaptedLuminances.x;
	Out.LuminanceMin = AdaptedLuminances.y;
	Out.LuminanceMax = AdaptedLuminances.z;

	// Clamp perceived luminance as the eye cannot adapt to very low or very bright levels
	Out.LuminanceAverage = clamp( Out.LuminanceAverage, _AdaptationLevels.x, _AdaptationLevels.y );

	// Compute middle grey value
	// From eq. 10 in http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering
	Out.MiddleGrey = 1.03 - 2.0 / (2.0 + log10( 1.0 + AdaptedLuminances.x ));

	return	Out;
}

float A = 0.15;			// A = Shoulder Strength
float B = 0.50;			// B = Linear Strength
float C = 0.10;			// C = Linear Angle
float D = 0.20;			// D = Toe Strength
float E = 0.02;			// E = Toe Numerator
float F = 0.30;			// F = Toe Denominator
						// (Note: E/F = Toe Angle)

float _ExposureBias = 1.0;
float _HDRWhitePoint = 11.2;			// HDR White Point Value
float _LDRWhitePoint = 1.0;

float3 FilmicOperator( float3 _In )
{
   return ((_In*(A*_In+C*B) + D*E) / (_In*(A*_In+B) + D*F)) - E/F;
}

float3	ToneMapColor_FILMIC( float3 _ColorHDR, float _ImageLuminance )
{
#ifdef APPLY_TO_LUMINANCE_ONLY
// 	// xyY Version (colors are much more vivid)
// 	float3	xyY = RGB2xyY( _ColorHDR );
// 			xyY.z *= _In.MiddleGrey / _ImageLuminance;												// Exposure correction
// 			xyY.z = (FilmicOperator( _ExposureBias * xyY.zzz ) / FilmicOperator( _HDRWhitePoint )).x;	// Tone mapped luminance
// 	return xyY2RGB( xyY );

// 	float	Luminance = dot( _ColorHDR, LUMINANCE_WEIGHTS );
// 			Luminance /= _ImageLuminance;
// 	float	ToneMappedLuminance = _LDRWhitePoint * FilmicOperator( _ExposureBias * Luminance ).x / FilmicOperator( _HDRWhitePoint ).x;
// 	return _ColorHDR * ToneMappedLuminance / Luminance;

	float	Luminance = dot( _ColorHDR, LUMINANCE_WEIGHTS );
			Luminance /= _ImageLuminance;
//	float	ToneMappedLuminance = _LDRWhitePoint * FilmicOperator( _ExposureBias * Luminance ).x / FilmicOperator( _HDRWhitePoint ).x;
	float	ToneMappedLuminance = _LDRWhitePoint * FilmicOperator( _ExposureBias * Luminance ).x;
	return _ColorHDR * ToneMappedLuminance / Luminance;

#else
	// RGB Version
// 	_ColorHDR *= _In.MiddleGrey / _ImageLuminance;	// Exposure correction
// 	return _LDRWhitePoint * FilmicOperator( _ExposureBias * _ColorHDR ) / FilmicOperator( _HDRWhitePoint );

	return _LDRWhitePoint * FilmicOperator( _ExposureBias * _ColorHDR ) / FilmicOperator( _HDRWhitePoint );
#endif
}

float	_DragoMaxDisplayLuminance = 40.0f;	// Nominal = 50.0
float	_DragoBias = 0.85f;					// Nominal = 0.85

float3	ToneMapColor_DRAGO( float3 _ColorHDR, float _ImageLuminance )
{
	// Apply tone mapping
	float	Ldmax = _DragoMaxDisplayLuminance;
	float	Lw = _ExposureBias * dot( _ColorHDR, LUMINANCE_WEIGHTS );
	float	Lwmax = _ImageLuminance;
	float	bias = _DragoBias;

	float	NewLuminance = Ldmax * 0.01 * log( Lw + 1.0 );
	NewLuminance /= log10( Lwmax + 1.0 ) * log( 2.0 + 0.8 * pow( Lw / Lwmax, -1.4426950408889634073599246810019 * log( bias ) ) );

	return _ColorHDR * NewLuminance / Lw;
}

float3	ToneMapColor( float3 _ColorHDR, float _ImageLuminance )
{
// 	return ToneMapColor_FILMIC( _ColorHDR, _ImageLuminance );
	return ToneMapColor_DRAGO( _ColorHDR, _ImageLuminance );
}

float4 PS_ToneMap( PS_IN _In ) : SV_TARGET0
{
	float4	SourceValue = _SourceTexture.SampleLevel( LinearClamp, _In.UV, 0.0 );
	float3	SourceColor = SourceValue.xyz;

	// Apply tone mapping
	float3	ToneMappedColor = ToneMapColor( SourceColor, _In.LuminanceAverage );

	// Gamma correction
	float3	FinalColor = pow( ToneMappedColor, _Params.z );	// Gamma corrected LDR color...

	return float4( FinalColor, SourceValue.w );
}

float4 PS_ToneMap_PassThrough( PS_IN _In ) : SV_TARGET0
{
	float4	SourceValue = _SourceTexture.SampleLevel( LinearClamp, _In.UV, 0.0 );
	return float4( pow( SourceValue.xyz, _Params.z ), SourceValue.w );
}

////////////////////////////////////////////////////// DEBUG DISPLAY ////////////////////////////////////////////////////// 
int			_DEBUG_Type;
float4		_DEBUG_LuminanceMinMaxMarker;
Texture2D	_DEBUG_FalseColors;
Texture2D	_DEBUG_RGBRamps;

float4 PS_ToneMap_Debug( PS_IN _In ) : SV_TARGET0
{
	float4	SourceValue = _SourceTexture.SampleLevel( LinearClamp, _In.UV, 0.0 );
	float3	SourceColor = SourceValue.xyz;

	if ( _DEBUG_Type == 3 )
		SourceColor = _In.LuminanceMax * _DEBUG_RGBRamps.SampleLevel( LinearClamp, _In.UV, 0.0 ).xyz;

	float	Luminance = dot( SourceColor, LUMINANCE_WEIGHTS );

	// Apply tone mapping
	float3	ToneMappedColor = ToneMapColor( SourceColor, _In.LuminanceAverage );

	// ===========================================================================
	// DEBUG DISPLAY
	//
	// 1 = LUMINANCE_NORMALIZED,		// Display luminance as a color gradient. Gradient extremes are exactly Min and Max luminance
	// 2 = LUMINANCE_CUSTOM,			// Display luminance as a color gradient. Gradient extremes are specified manually
	// 3 = GRADIENTS_FULLSCREEN,		// Display a gradient table. The gradient is tone mapped
	// 4 = GRADIENTS_INSET,				// Display a gradient table as an inset in the lower right corner of the screen. The gradient is tone mapped
	if ( _DEBUG_Type == 1 )
	{
		float2	UV = float2(
				(Luminance - _In.LuminanceMin) / (_In.LuminanceMax - _In.LuminanceMin),
				fmod( 8.0 * (_In.Position.x + _In.Position.y) * _SourceInfos.y, 1.0 )
			);
		ToneMappedColor = _DEBUG_FalseColors.SampleLevel( LinearClamp, UV, 0.0 ).xyz;
	}
	else if ( _DEBUG_Type == 2 )
	{
		float2	UV = float2(
				(Luminance - _DEBUG_LuminanceMinMaxMarker.x) / (_DEBUG_LuminanceMinMaxMarker.y - _DEBUG_LuminanceMinMaxMarker.x),
				fmod( 8.0 * (_In.Position.x + _In.Position.y) * _SourceInfos.y, 1.0 )
			);

		ToneMappedColor = _DEBUG_FalseColors.SampleLevel( LinearClamp, UV, 0.0 ).xyz;
	}
	else if ( _DEBUG_Type == 4 )
	{
		float2	UV = _In.UV * 5.0 - 4.0;
		if ( UV.x >= 0.0 && UV.x < 1.0 && UV.y >= 0.0 && UV.y < 1.0 )
		{
			SourceColor = _In.LuminanceMax * _DEBUG_RGBRamps.SampleLevel( LinearClamp, UV, 0.0 ).xyz;
			Luminance = dot( SourceColor, LUMINANCE_WEIGHTS );
			ToneMappedColor = ToneMapColor( SourceColor, _In.LuminanceAverage );
		}
	}

	if ( abs( Luminance - _DEBUG_LuminanceMinMaxMarker.z ) < _DEBUG_LuminanceMinMaxMarker.w )
		ToneMappedColor = 0.3;
	//
	// DEBUG DISPLAY
	// ===========================================================================

	// Gamma correction
	float3	FinalColor = pow( ToneMappedColor, _Params.z );	// Gamma corrected LDR color...

	return float4( FinalColor, SourceValue.w );
}

// ===================================================================================
//
technique10 DownSampleFirstStage
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_DownSampleFirstStage() ) );
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

technique10 ToneMap_PassThrough
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_ToneMap() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ToneMap_PassThrough() ) );
	}
}

technique10 ToneMap_Debug
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_ToneMap() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_ToneMap_Debug() ) );
	}
}
