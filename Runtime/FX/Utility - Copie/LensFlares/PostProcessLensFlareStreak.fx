// Lens-Flare post-process
// This technique displays lens-flare Streaks
// 

// ===================================================================================
// Includes that are replaced by actual code during code generation
#include "PostProcessLensFlareRandom.fx"
#include "PostProcessLensFlareCommon.fx"

// Effect-specific variables
float	Length;
float	Thickness;
float	CoreIntensity;
float	Symmetry;
float	FanEnds;
float	FanFeathering;
float	ReplicatorCopies;
float	ReplicatorAngle;
float	ScaleRandom;
float	SpacingRandom;


// ===================================================================================
// STREAK EFFECT
// ===================================================================================
//
// Streaks are quite complicated in terms of geometry.
// After careful study I think the general aspect of a streak looks like this :
//
// .------------------------------.
// .\             |              /.
// . \            |             / .
// .  +-----------+------------+  .
// .  |F          |            |  .
// .  |           |            |  .
// .  |           |            |  .
// .--+-----------+------------+--.---> X
// .  |           |            |  .
// .  |           |            |  .
// .  |F'         |            |  .
// .  +-----------+------------+  .
// . /            |             \ .
// ./             |              \.
// .------------------------------.
//
// Points F and F' are "fan ends" that can collapse down to the X axis when the parameter is -100%
//
struct VS_IN
{
	float2	UV			: TEXCOORD0;
};

struct GS_IN
{
	float	ScaleFactor			: SCALE_FACTOR;		// The global light scale factor
	float3	BrightnessFactor	: BRIGHTNESS_FACTOR;// The global brightness factor
	float	Rotation			: ROTATION;
	float	Scale				: SCALE;
};

struct PS_IN
{
	float4	__Position		: SV_POSITION;
	float2	Position		: POSITION;			// Position in EFFECT space
	float	Brightness		: BRIGHTNESS;
	float2	UV				: COORDS;
};

GS_IN VS( VS_IN _In, uint _StreakIndex : SV_INSTANCEID )
{
	GS_IN	Out;

	// Read back light data
	float4	LightData = LightIntensityScale.SampleLevel( NearestClamp, 0.5.xx, 0 );

	// Forward vertex data
	Out.ScaleFactor = LightData.w;
	Out.BrightnessFactor = GlobalBrightnessFactor * LightData.xyz;

 	float4	Random = GetNoise( 59.17 * _StreakIndex, RandomSeed );
	Out.Rotation = ReplicatorAngle * ((float) _StreakIndex / ReplicatorCopies + SpacingRandom * (2.0 * Random.x - 1.0));
	Out.Scale = lerp( 1.0, 0.5, ScaleRandom * Random.y );

	return	Out;
}

[maxvertexcount( 6*4 )]
void	GS( point GS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> _OutStream )
{
	static const float	LengthFactor = 7.87;
	static const float	ThicknessFactor = LengthFactor;
	static const float	FanRatio = 0.8;					// Inner rectangle is 80% of the total rectangle

	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( Distance, Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float	Trig = ApplyDynamicTriggering( EffectCenter );
	float	LocalBrightness = _In[0].BrightnessFactor * (Brightness + Trig*BrightnessOffset);
	float	LocalScale = _In[0].ScaleFactor * LightSize * (_In[0].Scale + Trig*ScaleOffset);
	float2	LocalStretch = Stretch + Trig*StretchOffset;
	float	LocalRotation = Rotation + _In[0].Rotation - Trig*RotationOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	// Compute left/right distances
	float	RFactor = lerp( 0.1, 1.0, Symmetry );
	float	Lout = -Length * LengthFactor;
	float	Rout = Length * LengthFactor * RFactor;
	float	Lin = -FanRatio * Length * LengthFactor;
	float	Rin = FanRatio * Length * LengthFactor * RFactor;

	// Compute vertical distances
	float	Yout = Thickness * ThicknessFactor;
	float	Yin = FanRatio * Thickness * ThicknessFactor;
	float	Yin2 = FanRatio * Thickness * ThicknessFactor * (1.0+FanEnds);
	float	Yout2 = Yin2 + Thickness * ThicknessFactor * (1.0-FanRatio);

	// Compute internal UVs
	float2	UVin = lerp( FanRatio, float2( FanRatio, FanRatio * (1.0+FanEnds)), FanFeathering );

	// Output vertices
	PS_IN	Out;
	Out.Brightness = LocalBrightness;

	// Left fan
	Out.UV = float2( -1.1, 0.0 );
	Out.Position = float2( 1.1 * Lout, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( -UVin.x, -UVin.y );
	Out.Position = float2( Lin, -Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( -UVin.x, UVin.y );
	Out.Position = float2( Lin, Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 0.0 );
	Out.Position = float2( 0.0, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	_OutStream.RestartStrip();

	// Right fan
	Out.UV = float2( 1.1, 0.0 );
	Out.Position = float2( 1.1 * Rout, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( UVin.x, -UVin.y );
	Out.Position = float2( Rin, -Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( UVin.x, UVin.y );
	Out.Position = float2( Rin, Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 0.0 );
	Out.Position = float2( 0.0, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	_OutStream.RestartStrip();

	// Middle top fan
	Out.UV = float2( -UVin.x, UVin.y );
	Out.Position = float2( Lin, Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 0.0 );
	Out.Position = float2( 0.0, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, FanRatio );
	Out.Position = float2( 0.0, Yin );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( UVin.x, UVin.y );
	Out.Position = float2( Rin, Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	_OutStream.RestartStrip();

	// Middle bottom fan
	Out.UV = float2( -UVin.x, -UVin.y );
	Out.Position = float2( Lin, -Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 0.0 );
	Out.Position = float2( 0.0, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, -FanRatio );
	Out.Position = float2( 0.0, -Yin );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( UVin.x, -UVin.y );
	Out.Position = float2( Rin, -Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	_OutStream.RestartStrip();

	// Top fan
	Out.UV = float2( -UVin.x, UVin.y );
	Out.Position = float2( Lin, Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, 1.1 );
	Out.Position = float2( 0.0, 1.1*Yout );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, FanRatio );
	Out.Position = float2( 0.0, Yin );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( UVin.x, UVin.y );
	Out.Position = float2( Rin, Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	_OutStream.RestartStrip();

	// Bottom fan
	Out.UV = float2( -UVin.x, -UVin.y );
	Out.Position = float2( Lin, -Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, -1.1 );
	Out.Position = float2( 0.0, -1.1*Yout );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( 0.0, -FanRatio );
	Out.Position = float2( 0.0, -Yin );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	Out.UV = float2( UVin.x, -UVin.y );
	Out.Position = float2( Rin, -Yin2 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
	_OutStream.RestartStrip();
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
	float	Distance = length( _In.UV );
	clip( 1.0 - Distance );

	// Compute intensity
	float	Intensity = 0.1 * CoreIntensity / max( 1e-3, Distance );
			Intensity *= smoothlerp( 1.0, 0.0, Distance );

	// Apply colorize
	float3	Color = Colorize( 2.0 * abs(_In.UV), _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );
	return 0.128 * Intensity * Color;
}

// ===================================================================================
//
technique10 DisplayStreak
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
