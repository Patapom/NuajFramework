// Lens-Flare post-process
// This technique displays lens-flare Irises
// 

// ===================================================================================
// Includes that are replaced by actual code during code generation
#include "PostProcessLensFlareRandom.fx"
#include "PostProcessLensFlareCommon.fx"

// Effect-specific variables
float	ColorRandom;
float	Spread;
float	SpreadRandom;
int		IrisesCount;
float	ScaleRandom;
float	BrightnessRandom;
float	RotationRandom;
float	OffsetRandom;
float	CompletionRotationRandom;

int		ShapeType;			// Polygon/Circle/Texture
float	ShapeOrientation;
float	OrientationRandom;
int		PolygonSides;
float	PolygonRoundness;
float	BladeNotching;
float	Smoothness;
float	SmoothnessRandom;
float	OutlineIntensity;
float	OutlineThickness;
float	OutlineFeathering;
float	OutlineIntensityOffset;

Texture2D	IrisTexture;

// ===================================================================================
// MULTI-IRIS EFFECT
// ===================================================================================
//
struct VS_IN
{
	float2	UV				: TEXCOORD0;
};

struct GS_IN
{
	float2	Distance			: DISTANCE;
	float	Scale				: SCALE;
	float	ScaleFactor			: SCALE_FACTOR;
	float	Brightness			: BRIGHTNESS;
	float3	BrightnessFactor	: BRIGHTNESS_FACTOR;
	float3	Color1				: COLOR;
	float	Rotation			: ROTATION;
	float2	Offset				: OFFSET;
	float	ShapeOrientation	: SHAPE_ORIENTATION;
	float	Smoothness			: SMOOTHNESS;
	float	CompletionRotation	: COMPLETION_ROTATION;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float2	Position			: POSITION;			// Position in EFFECT space
	float	DynamicTriggering	: DYN_TRIGGER;
	float3	Brightness			: BRIGHTNESS;
	float3	Color1				: COLOR;
	float	ShapeOrientation	: SHAPE_ORIENTATION;
	float	Smoothness			: SMOOTHNESS;
	float	CompletionRotation	: COMPLETION_ROTATION;
};

// ===================================================================================
GS_IN VS( VS_IN _In, uint _IrisIndex : SV_INSTANCEID )
{
	// Read back light data
	float4	LightData = LightIntensityScale.SampleLevel( NearestClamp, 0.5.xx, 0 );

 	float4	Random0 = GetNoise( 59.17 * _IrisIndex, RandomSeed );
 	float4	Random1 = GetNoise( -59.17 * _IrisIndex + 137.43, RandomSeed );
 	float4	Random2 = GetNoise( 37.13 * _IrisIndex - 137.43, RandomSeed );

	// Perform Hue variation of the source color
	float3	ColorHSL = RGB2HSL( ColorSource == 0 ? GlobalColor : Color1 );
	ColorHSL.x = (60.0 + ColorHSL.x + ColorRandom * 3.0 * (2.0 * Random0.x - 1.0)) % 6.0;
	float3	ColorRGB = HSL2RGB( ColorHSL );

	// Forward vertex data
	GS_IN	Out;
	Out.Distance = IrisesCount > 1 ?	// Watch out for 2 different treatments of the Distance parameter based on the Single-/Multi-Iris property
		float2( 1.0 + Distance.x + Spread * (1.0 + 0*SpreadRandom * Random2.w) * (2.0 * Random0.y - 1.0), Distance.y ) : 
		Distance;
	Out.Scale = Scale * lerp( 1.0, Random0.z, ScaleRandom );
	Out.ScaleFactor = LightData.w;
	Out.Brightness = Brightness * lerp( 1.0, Random0.w, BrightnessRandom );
	Out.BrightnessFactor = GlobalBrightnessFactor * LightData.xyz;
	Out.Color1 = ColorRGB;
	Out.Rotation = Rotation + RotationRandom * PI * (2.0 * Random1.x - 1.0);
	Out.Offset = Offset + OffsetRandom * (2.0 * Random1.zw - 1.0);
	Out.ShapeOrientation = ShapeOrientation + OrientationRandom * PI * (2.0 * Random2.x - 1.0);
	Out.Smoothness = Smoothness * lerp( 1.0, Random2.y, SmoothnessRandom );
	Out.CompletionRotation = CompletionRotation + CompletionRotationRandom * PI * (2.0 * Random2.z - 1.0);;

	return	Out;
}

[maxvertexcount( 4 )]
void	GS( point GS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> _OutStream )
{
	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( _In[0].Distance, _In[0].Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float		Trig = ApplyDynamicTriggering( EffectCenter );
	float		LocalBrightness = _In[0].BrightnessFactor * (_In[0].Brightness + Trig*BrightnessOffset);
	float		LocalScale = _In[0].ScaleFactor * LightSize * (_In[0].Scale + Trig*ScaleOffset);
	float2		LocalStretch = Stretch + Trig*StretchOffset;
	float		LocalRotation = _In[0].Rotation - Trig*RotationOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	// Output vertices
	PS_IN	Out;
	Out.DynamicTriggering = Trig;
	Out.Brightness = LocalBrightness;
	Out.Color1 = _In[0].Color1;
	Out.ShapeOrientation = _In[0].ShapeOrientation;
	Out.Smoothness = _In[0].Smoothness;
	Out.CompletionRotation = _In[0].CompletionRotation + ComputeCircularCompletionAdditionalRotation( AutoRotateModeCompletion, LocalRotation, EffectCenter );

	Out.Position = float2( -20.0, -20.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( -20.0, +20.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( +20.0, -20.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( +20.0, +20.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
}

// Draws a single Iris
float3	PS( PS_IN _In ) : SV_TARGET0
{
	float2	P = _In.Position;

	// Apply dynamic triggering
	float	LocalCompletion = Completion + _In.DynamicTriggering*CompletionOffset;
	float	LocalOutlineIntensity = OutlineIntensity + _In.DynamicTriggering*OutlineIntensityOffset;
	int		LocalColorSource = ColorSource == 0 ? 1 : ColorSource;

	float	Angle = atan2( P.x, P.y );
	float	Distance2Center = 0.05 * length( P );
	if ( ShapeType == 2 )
	{	// Sample texture
		float	Intensity = ComputeCircularCompletion( PI - Angle, LocalCompletion, CompletionFeathering, _In.CompletionRotation );

		float2	AxisX, AxisY;
		sincos( _In.ShapeOrientation, AxisX.y, AxisX.x );
		AxisY = float2( -AxisX.y, AxisX.x );

		float2	UV = 0.025 * (1.5 * (P.x * AxisX + P.y * AxisY) + 20.0);
		float3	Color = IrisTexture.Sample( LinearBorder, UV ).xyz;
				Color *= Colorize( Distance2Center, _In.Brightness, LocalColorSource, _In.Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );

		return 0.08 * Intensity * Color;
	}

	// Modify distance to center
	if ( ShapeType == 0 )
	{
		float	BladeMaxAngle = (2.0 * PI) / PolygonSides;
		float	BladeAngle = ((200.0 * PI - _In.ShapeOrientation + Angle) % BladeMaxAngle) - 0.5 * BladeMaxAngle;
		float	Distance2BladeEdge = cos( 0.5 * BladeMaxAngle ) / cos( BladeAngle );

		// Apply blade notching
		Distance2BladeEdge *= 1.0 + 0.02 * BladeNotching * 2.0 * BladeAngle / BladeMaxAngle;

		// Apply distance scale
		Distance2Center *= PolygonRoundness > 0.0 ?
							lerp( 1.0 / Distance2BladeEdge, 1.0, PolygonRoundness ) :
							lerp( 1.0 / Distance2BladeEdge, 1.0 / (Distance2BladeEdge - 2.0 * (1.0 - Distance2BladeEdge)), -0.125 * PolygonRoundness );
	}
	clip( 1.0-Distance2Center );

	// Compute intensity
	float	OutlineDistance = 1.0-OutlineThickness;
	float	Intensity = lerp( 1.0, 1.0+LocalOutlineIntensity, saturate( (Distance2Center-OutlineDistance) / (OutlineFeathering * OutlineThickness) ) );
			Intensity *= 1.0 - saturate( (Distance2Center + _In.Smoothness - 1.0) / _In.Smoothness );
			Intensity *= ComputeCircularCompletion( PI - Angle, LocalCompletion, CompletionFeathering, _In.CompletionRotation );

	// Apply colorize
	float3	Color = Colorize( Distance2Center, _In.Brightness, LocalColorSource, _In.Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );
	return 0.15625 * Intensity * Color;
}

// ===================================================================================
//
technique10 DisplayIris
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
