// This shader is responsible for applying colorimetry (including tone mapping)
//	to produce the final LDR image on the render target
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"

static const float3	LUMA = float3( 0.30, 0.59, 0.11 );		// Luma Rec 601

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// RGB / HCY conversion from http://en.wikipedia.org/wiki/HSL_and_HSV
// HCY is Hue/Chroma/Luma
float3	RGB2HCY( float3 _RGB )
{
	float	alpha = _RGB.x - 0.5 * (_RGB.y + _RGB.z);
	float	beta = 0.86602540378443864676372317075294 * (_RGB.y - _RGB.z);	// sqrt(3)/2
	float	H = 0.0;
	if ( abs(alpha) > 1e-6 && abs(beta) > 1e-6 )
		H = 0.15915494309189533576888376337251 * atan2( beta, alpha );	// atan( beta, alpha ) / 2PI
	float	C = sqrt( alpha*alpha + beta*beta );
	float	Y = dot( LUMA, _RGB );

 	return float3( H, C, Y );
}

float3	HCY2RGB( float3 _HCY )
{
	float	alpha, beta;
	sincos( 6.283185307179586476925286766559 * _HCY.x, beta, alpha );
	alpha *= _HCY.y;
	beta *= _HCY.y * 0.57735026918962576450914878050196;	// beta is always used over sqrt(3)
	float	Y = _HCY.z;

// 	float	R = 1.0 - LUMA.y * (beta - alpha) + LUMA.z * (beta + alpha);
// 	float	G = R + beta - alpha;
// 	float	B = R - beta - alpha;
// 	return Y * float3( R, G, B );

	float	R = Y - LUMA.y * (beta - alpha) + LUMA.z * (beta + alpha);
	float	G = R + beta - alpha;
	float	B = R - beta - alpha;
	return float3( R, G, B );
}

// ===================================================================================
// RGB / HSL conversion from http://en.wikipedia.org/wiki/HSL_and_HSV
float3	RGB2HSL( float3 _RGB )
{
	float2  MinMaxRGB = float2( min( _RGB.x, min( _RGB.y, _RGB.z ) ), max( _RGB.x, max( _RGB.y, _RGB.z ) ) );
	float3  HSL = float3( 0.0, 0.0, 0.0 );

	// 1] Luminance is 0.5 * (min + max)
	HSL.z = 0.5 * (MinMaxRGB.x + MinMaxRGB.y);

	if ( MinMaxRGB.x != MinMaxRGB.y )
	{	// H and S can be defined...

		// 2] Saturation
		if ( HSL.z < 0.5 )
			HSL.y = (MinMaxRGB.y - MinMaxRGB.x) / (MinMaxRGB.y + MinMaxRGB.x);
		else
			HSL.y = (MinMaxRGB.y - MinMaxRGB.x) / (2.0 - MinMaxRGB.y - MinMaxRGB.x);

		// 3] Hue
		float OneOverMaxMinusMin = 1.0 / (MinMaxRGB.y - MinMaxRGB.x);
		if ( MinMaxRGB.y == _RGB.r )
			HSL.r = (_RGB.g-_RGB.b) * OneOverMaxMinusMin;
		else if ( MinMaxRGB.y == _RGB.g )
			HSL.r = 2.0 + (_RGB.b-_RGB.r) * OneOverMaxMinusMin;
		else
			HSL.r = 4.0 + (_RGB.r-_RGB.g) * OneOverMaxMinusMin;
	}

	return HSL;
}

float3	HSL2RGB( float3 _HSL )
{
	_HSL.z = saturate( _HSL.z );
	float Chroma = (1.0 - abs( 2.0 * _HSL.z - 1.0 )) * _HSL.y;
	float X = Chroma * (1.0 - abs( (_HSL.x % 2.0) - 1.0 ));

	float3  tempRGB;
	if ( _HSL.x < 1.0 )
		tempRGB = float3( Chroma, X, 0 );
	else if ( _HSL.x < 2.0 )
		tempRGB = float3( X, Chroma, 0 );
	else if ( _HSL.x < 3.0 )
		tempRGB = float3( 0, Chroma, X );
	else if ( _HSL.x < 4.0 )
		tempRGB = float3( 0, X, Chroma );
	else if ( _HSL.x < 5.0 )
		tempRGB = float3( X, 0, Chroma );
	else
		tempRGB = float3( Chroma, 0, X );

	return tempRGB + (_HSL.z - 0.5 * Chroma).xxx;
}

float	Contrast( float _Luma, float _RangeCenter, float _Contrast )
{
	return saturate( _RangeCenter + 0.25 * _Contrast * 4.0 * (_Luma - _RangeCenter) );
}

bool	Enabled = true;
float	MaxLuminance = 1.0;								// The maximum encoded luminance in the input image
float	ToneSharpness = 1.0;							// The sharpness factor used to delimit the shadows/midtones/highlights boundaries (0 is smooth, 1 is ultra-crisp)
float3	Shift_Shadows = 0.0;							// RGB shift for shadows
float2	SatContrast_Shadows = float2( 0.0, 1.0 );		// Saturation + Contrast for shadows
float3	Shift_Midtones = 0.0;							// RGB shift for midtones
float2	SatContrast_Midtones = float2( 0.0, 1.0 );		// Saturation + Contrast for shadows
float3	Shift_Highlights = 0.0;							// RGB shift for highlights
float2	SatContrast_Highlights = float2( 0.0, 1.0 );	// Saturation + Contrast for shadows

float4	PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * GBufferInvSize.xy;
	float3	RGB = max( 0.0, GBufferTexture0.SampleLevel( NearestClamp, UV, 0 ).xyz );
	if ( !Enabled )
		return float4( RGB, 1.0 );

	RGB /= MaxLuminance;

	// Apply luma contrast and brightness
	float	LumaSource = dot( RGB, LUMA );
	float	LumaShadows = max( Shift_Shadows.x, max( Shift_Shadows.y, Shift_Shadows.z ) );
	float	NewLumaShadows = saturate( Contrast( LumaSource, 0.25, SatContrast_Shadows.y ) + (2.0 * LumaShadows - 1.0) );
	float	LumaMidtones = max( Shift_Midtones.x, max( Shift_Midtones.y, Shift_Midtones.z ) );
	float	NewLumaMidtones = saturate( Contrast( LumaSource, 0.5, SatContrast_Midtones.y ) + (2.0 * LumaMidtones - 1.0) );
	float	LumaHighlights = max( Shift_Highlights.x, max( Shift_Highlights.y, Shift_Highlights.z ) );
	float	NewLumaHighlights = saturate( Contrast( LumaSource, 0.75, SatContrast_Highlights.y ) + (2.0 * LumaHighlights - 1.0) );

	// Apply color shift based on saturation
	float	InvLumaSource = 1.0 / max( 1e-6, LumaSource );
	float3	RGB_Shadows = lerp( RGB, Shift_Shadows * LumaSource / LumaShadows, SatContrast_Shadows.x ) * NewLumaShadows * InvLumaSource;
	float3	RGB_Midtones= lerp( RGB, Shift_Midtones * LumaSource / LumaMidtones, SatContrast_Midtones.x ) * NewLumaMidtones * InvLumaSource;
	float3	RGB_Highlights = lerp( RGB, Shift_Highlights * LumaSource / LumaHighlights, SatContrast_Highlights.x ) * NewLumaHighlights * InvLumaSource;

	// Recombine into a single RGB based on luma weights for shadows, midtones and highlights
	float	ToneContrast = 1.0 + ToneSharpness;
	float	x = 1.0 - saturate( 2.0 * LumaSource );
	float	Weight_Shadows = saturate( 0.5 + ToneContrast * (x*x * (3.0 - 2.0 * x) - 0.5) );
	x = 1.0 - 2.0 * abs( 0.5-LumaSource );
	float	Weight_Midtones = saturate( 0.5 + ToneContrast * (x*x * (3.0 - 2.0 * x) - 0.5) );
	x = saturate( 2.0 * LumaSource - 1.0 );
	float	Weight_Highlights = saturate( 0.5 + ToneContrast * (x*x * (3.0 - 2.0 * x) - 0.5) );

	RGB = Weight_Shadows * RGB_Shadows + Weight_Midtones * RGB_Midtones + Weight_Highlights * RGB_Highlights;

	return float4( RGB, 1.0 );
}

// ===================================================================================

technique10 ApplyColorimetry
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
