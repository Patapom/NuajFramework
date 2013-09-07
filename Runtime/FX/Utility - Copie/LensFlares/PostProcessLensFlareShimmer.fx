// Lens-Flare post-process
// This technique displays lens-flare Shimmers
// 

// ===================================================================================
// Includes that are replaced by actual code during code generation
#include "PostProcessLensFlareRandom.fx"
#include "PostProcessLensFlareCommon.fx"

// Effect-specific variables
int		Complexity;
float	Detail;
float	ShapeOrientation;


// ===================================================================================
// SHIMMER EFFECT
// ===================================================================================
//
// Shimmers are quadrangles whose vertices nicely undulate in the wind in a timely fashion
//
struct VS_IN
{
	float2	UV				: TEXCOORD0;
};

struct GS_IN
{
	uint	VertexIndex			: SHAPE_INDEX;
	float	ScaleFactor			: SCALE_FACTOR;		// The global light scale factor
	float3	BrightnessFactor	: BRIGHTNESS_FACTOR;// The global brightness factor
	float	Angle				: ANGLE;
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

GS_IN VS( VS_IN _In, uint _InstanceIndex : SV_INSTANCEID )
{
	GS_IN	Out;

	// Read back light data
	float4	LightData = LightIntensityScale.SampleLevel( NearestClamp, 0.5.xx, 0 );

	// Forward vertex data
	Out.VertexIndex = _InstanceIndex;
	Out.ScaleFactor = LightData.w;
	Out.BrightnessFactor = GlobalBrightnessFactor * LightData.xyz;
	Out.Angle = ShapeOrientation + 0.165 * PI + (0.8*0.666666 * PI * (0.5+(_InstanceIndex%3))) / Complexity;

	return	Out;
}

[maxvertexcount( 3 )]
void	GS( point GS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( Distance, Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float		Trig = ApplyDynamicTriggering( EffectCenter );
	float3		LocalBrightness = _In[0].BrightnessFactor * (Brightness + Trig*BrightnessOffset);
	float		LocalScale = _In[0].ScaleFactor * LightSize * (Scale + Trig*ScaleOffset);
	float2		LocalStretch = Stretch + Trig*StretchOffset;
	float		LocalRotation = Rotation - Trig*RotationOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	// Build vertex lengths
	const float	SIZE = 100.0;

	int		ShapeIndex = _In[0].VertexIndex % 3;	// 3 Shapes
	int		SideIndex = _In[0].VertexIndex / 3;		// N Angles (depending on complexity)
	int		MaxSide = Complexity;
	int		NextSideIndex = (SideIndex+1) % MaxSide;

	float4	Random = GetNoise( 59.17 * (3*SideIndex+ShapeIndex), RandomSeed );
	float	AnimAmplitude = AnimAmount * 0.5 * (1.0 + sin( PI * (Time + 57.0 * Random.x) * AnimSpeed ));
	float	Length0 = SIZE * (1.0 - (1.0-AnimAmplitude) * Detail * Random.y);

 	Random = GetNoise( 59.17 * (3*NextSideIndex+ShapeIndex), RandomSeed );
	AnimAmplitude = AnimAmount * 0.5 * (1.0 + sin( PI * (Time + 57.0 * Random.x) * AnimSpeed ));
	float	Length1 = SIZE * (1.0 - (1.0-AnimAmplitude) * Detail * Random.y);

	// Build shape axes
	float	DeltaAngle = 2.0 * PI / MaxSide;
	float	Angle0 = _In[0].Angle + SideIndex * DeltaAngle;
	float	Angle1 = _In[0].Angle + (SideIndex+1) * DeltaAngle;

	float2	Axis0, Axis1;
	sincos( Angle0, Axis0.y, Axis0.x );
	sincos( Angle1, Axis1.y, Axis1.x );

	// Patch angles for circular completion
	Angle0 += 0.5 * PI;
	Angle1 += 0.5 * PI;

	// Output vertices
	PS_IN	Out;
	Out.DynamicTriggering = Trig;
	Out.CompletionRotation = -CompletionRotation + ComputeCircularCompletionAdditionalRotation( AutoRotateModeCompletion, LocalRotation, EffectCenter );
	Out.Brightness = LocalBrightness;

	Out.UV = float2( 0.0, 1.0 );
	Out.Position = Length0 * Axis0;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	Out.Angle = Angle0;
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 1.0 );
	Out.Position = Length1 * Axis1;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	Out.Angle = Angle1;
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 0.0 );
	Out.Position = 0.0;
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	Out.Angle = 0.5*(Angle0+Angle1);
	_OutStream.Append( Out );
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
	float	Length = _In.UV.y;

	// Apply dynamic triggering
	float	LocalCompletion = Completion + _In.DynamicTriggering*CompletionOffset;

	// Compute intensity
	float	Intensity = 1.0 - Length;
			Intensity *= Intensity;
			Intensity *= ComputeCircularCompletion( _In.Angle, LocalCompletion, CompletionFeathering, _In.CompletionRotation );

	// Apply colorize
	float3	Color = Colorize( Length, _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );
	return 0.137 * Intensity * Color;
}

// ===================================================================================
//
technique10 DisplayShimmer
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
