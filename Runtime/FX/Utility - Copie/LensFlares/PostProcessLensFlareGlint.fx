// Lens-Flare post-process
// This technique displays lens-flare Glints
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
// Glint is a set of spikes (i.e. simple triangles) radiating from the center
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
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float2	Position			: POSITION;			// Position in EFFECT space
	float3	Brightness			: BRIGHTNESS;
	float	CompletionRotation	: COMPLETION_ROT;
 	float	DynamicTriggering	: DYN_TRIGGER;
	float2	UV					: COORDS;
	float	Angle				: ANGLE;
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

	// Build random branch angle
	Out.Angle = ShapeOrientation + PI * (2.0 * _SpikeIndex / Complexity + SpacingRandom * (2.0 * Random.x - 1.0));

	// Build random length
	float	AnimAmplitude = 1.0 - AnimAmount * 0.5 * (1.0 + sin( PI * (Time + 57.0 * Random.y) * AnimSpeed ));
	Out.Length = lerp( Length, 0.0, AnimAmplitude * LengthRandom * Random.x );

	return	Out;
}

[maxvertexcount( 6 )]
void	GS( point GS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( Distance, Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float		Trig = ApplyDynamicTriggering( EffectCenter );
	float3		LocalBrightness = _In[0].BrightnessFactor * (Brightness + Trig*BrightnessOffset) * lerp( 1.0.xxx, ColorShift, Trig );
	float		LocalScale = _In[0].ScaleFactor * LightSize * (Scale + Trig*ScaleOffset);
	float2		LocalStretch = Stretch + Trig*StretchOffset;
	float		LocalRotation = Rotation - Trig*RotationOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	float		LocalLength = 0.9 * _In[0].Length;
	float		LocalThickness = Thickness * 0.025 * LocalLength;
	float		DeltaAngle = 1.0 * PI / Complexity;

	// Build spike axes
	float2		X, Y;
	sincos( _In[0].Angle, Y.y, Y.x );
	X = float2( -Y.y, Y.x );

	// Output vertices
	PS_IN	Out;
	Out.DynamicTriggering = Trig;
	Out.CompletionRotation = -CompletionRotation + ComputeCircularCompletionAdditionalRotation( AutoRotateModeCompletion, LocalRotation, EffectCenter );
	Out.Brightness = LocalBrightness;

	Out.Angle = LocalRotation;
	Out.UV = float2( 0.0, 1.0 );
	Out.Position = LocalLength * Y;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.Angle = LocalRotation + DeltaAngle;
	Out.UV = float2( -1.0, 0.0 );
	Out.Position = -LocalThickness * X;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.Angle = LocalRotation - DeltaAngle;
	Out.UV = float2( +1.0, 0.0 );
	Out.Position = +LocalThickness * X;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
// 	float2	Pos = 100.0 * _In.__Position.xy * InvSourceSize.xy;
// 
// 	float2	Offset = TriggerOffset;
// 	Offset.y = -Offset.y;
// 	Offset += 50.0;
// 
// 	float2	Center2Position = (Pos - Offset) / TriggerStretch;
// 	float	Distance2Center = max( 0, length( Center2Position ) - Expansion );
// 	float	TriggerIntensity = InterpolateSingle( Distance2Center, 0.0, OuterFallOffRange, FallOffType );
// 	return bInvertTrigger ? TriggerIntensity : 1.0 - TriggerIntensity;
// 
// 	return TestDynamicTriggering( _In.__Position.xy );

	// Apply dynamic triggering
	float	LocalCompletion = Completion + _In.DynamicTriggering*CompletionOffset;

 	// Compute intensity
	float	Du = max( 0.0, abs(_In.UV.x) + 1.0 * _In.UV.y );
			Du = 1.0-0.7*Du;
			Du = Du*Du*Du;
	float	Dv = 1.0-0.9*_In.UV.y;
	float	Intensity = Du*Dv;
			Intensity *= ComputeCircularCompletion( PI + _In.Angle, LocalCompletion, CompletionFeathering, _In.CompletionRotation );

	// Apply colorize
	float3	Color = Colorize( _In.UV.y, _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );
	return 0.117 * Intensity * Color;
}

// ===================================================================================
//
technique10 DisplayGlint
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
