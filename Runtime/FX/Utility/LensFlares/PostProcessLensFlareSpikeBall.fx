// Lens-Flare post-process
// This technique displays lens-flare SpikeBalls
// 

// ===================================================================================
// Includes that are replaced by actual code during code generation
#include "PostProcessLensFlareRandom.fx"
#include "PostProcessLensFlareCommon.fx"

// Effect-specific variables
int		Complexity;
float	Length;
float	LengthRandom;
float	Thickness;
float	ThicknessRandom;
float	BrightnessRandom;
float	SpacingRandom;
float	ShapeOrientation;


// ===================================================================================
// SPIKEBALL EFFECT
// ===================================================================================
//
// Spike Ball is a set of "petals" radiating from the center
//
struct VS_IN
{
	float2	UV				: TEXCOORD0;
};

struct GS_IN
{
	float	ScaleFactor			: SCALE_FACTOR;		// The global light scale factor
	float3	BrightnessFactor	: BRIGHTNESS_FACTOR;// The global brightness factor
	float	Angle				: ANGLE;
	float	Length				: LENGTH;
	float	Thickness			: THICKNESS;
	float	Brightness			: BRIGHTNESS;
};

struct PS_IN
{
	float4	__Position		: SV_POSITION;
	float2	Position		: POSITION;			// Position in EFFECT space
	float3	Brightness		: BRIGHTNESS;
	float2	UV0				: COORDS0;
	float2	UV1				: COORDS1;
};

GS_IN VS( VS_IN _In, uint _SpikeIndex : SV_INSTANCEID )
{
	GS_IN	Out;

	// Read back light data
	float4	LightData = LightIntensityScale.SampleLevel( NearestClamp, 0.5.xx, 0 );

	float4	Random = FBM2( 59.17 * _SpikeIndex, RandomSeed );

	// Forward vertex data
	Out.ScaleFactor = LightData.w;
	Out.BrightnessFactor = GlobalBrightnessFactor * LightData.xyz;

	// Build random branch angle, length, brightness and thickness
	float	AnimAmplitude = 1.0 - AnimAmount * 0.5 * (1.0 + sin( PI * (Time + 57.0 * Random.y) * AnimSpeed ));
	Out.Angle = ShapeOrientation + PI * (2.0 * _SpikeIndex / Complexity + SpacingRandom * (2.0 * Random.x - 1.0));
	Out.Length = lerp( Length, 0.0, AnimAmplitude * LengthRandom * Random.x );
	Out.Thickness = lerp( Thickness, 0.0, ThicknessRandom * Random.y );
	Out.Brightness = lerp( Brightness, 0.0, AnimAmplitude * BrightnessRandom * Random.z );

	return	Out;
}

[maxvertexcount( 6 )]
void	GS( point GS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( Distance, Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float		Trig = ApplyDynamicTriggering( EffectCenter );
	float3		LocalBrightness = _In[0].BrightnessFactor * (_In[0].Brightness + Trig*BrightnessOffset);
	float		LocalScale = _In[0].ScaleFactor * LightSize * (Scale + Trig*ScaleOffset);
	float2		LocalStretch = Stretch + Trig*StretchOffset;
	float		LocalRotation = Rotation - Trig*RotationOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	const float	PETAL_LENGTH_RATIO = 0.20;
	const float	U_SIZE = 1.0;
	const float	V_SIZE = 1.0;
	const float	ELLIPSE_SIZE_FACTOR_U = 1.4;
	const float	ELLIPSE_SIZE_FACTOR_V = 1.18;
	float		LocalLength = _In[0].Length;
	float		LocalThickness = _In[0].Thickness * 0.4 * LocalLength;

	// Build spike axes
	float2		X, Y;
	sincos( _In[0].Angle, Y.y, Y.x );
	X = float2( -Y.y, Y.x );

	// Output vertices
	PS_IN	Out;
	Out.Brightness = LocalBrightness;

	Out.UV0 = float2( 0.0, 0.0 );
	Out.UV1 = float2( 0.0, 0.0 );
	Out.Position = 0.0;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV0 = float2( U_SIZE, 0.0 );
	Out.UV1 = float2( ELLIPSE_SIZE_FACTOR_U, ELLIPSE_SIZE_FACTOR_V * PETAL_LENGTH_RATIO );
	Out.Position = LocalThickness * X + PETAL_LENGTH_RATIO * LocalLength * Y;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV0 = float2( 0.0, V_SIZE );
	Out.UV1 = float2( 0.0, ELLIPSE_SIZE_FACTOR_V );
	Out.Position = LocalLength * Y;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	// 2nd triangle
	Out.UV0 = float2( 0.0, V_SIZE );
	Out.UV1 = float2( 0.0, ELLIPSE_SIZE_FACTOR_V );
	Out.Position = LocalLength * Y;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV0 = float2( U_SIZE, 0.0 );
	Out.UV1 = float2( ELLIPSE_SIZE_FACTOR_U, ELLIPSE_SIZE_FACTOR_V * PETAL_LENGTH_RATIO );
	Out.Position = -LocalThickness * X + PETAL_LENGTH_RATIO * LocalLength * Y;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV0 = float2( 0.0, 0.0 );
	Out.UV1 = float2( 0.0, 0.0 );
	Out.Position = 0.0;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
	float2	UV0 = _In.UV0;
	float	Length0 = length( UV0 );
	UV0 /= Length0;

	float2	UV1 = _In.UV1;
	float	Length1 = length( UV1 );
	clip( 1.0 - Length1 );

 	// Compute intensity
	float	Intensity = (1.0-Length1)*(1.0-Length1);			// First value is given by the distance to the center of a circle
			Intensity *= UV0.y;									// Modulate by the cosine lobe with the "top border"
			Intensity *= smoothlerp( 1.0, 0.2, sqrt(Length0) );	// Modulate by distance from center
			Intensity *= (1.0-UV0.x);							// Finally, modulate by distance to central axis

	// Apply colorize
	float3	Color = Colorize( _In.UV0.y, _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );

	float	Lum = dot( _In.Brightness, float3( 0.3, 0.5, 0.2 ) );
	float	t = pow( max( 0.0, (Lum-1.0) / (10.0-1.0) ), 0.25 );
	return 2.5*lerp( 0.8, 0.3325, t ) * Intensity * Color;
}

// ===================================================================================
//
technique10 DisplaySpikeBall
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
