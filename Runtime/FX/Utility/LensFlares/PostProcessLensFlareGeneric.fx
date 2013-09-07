// Lens-Flare post-process
// This technique displays generic lens-flare effects.
// Effects qualifying for individual display are :
//	_ Hoop
//	_ Ring
//	_ Glow
//	_ Sparkle
//	_ Caustic (TODO)
// 

// ===================================================================================
// Includes that are replaced by actual code during code generation
#include "PostProcessLensFlareRandom.fx"
#include "PostProcessLensFlareCommon.fx"

// Effect-specific variables

	// For rings
float	Thickness;
float	InsideFeathering;
float	OutsideFeathering;
float	ThicknessOffset;

	// For hoops
float	Complexity;
float	Detail;
float	Length;

	// For glows
float	Gamma;

	// For glints
float	LengthRandom;
float	SpacingRandom;
float	ShapeOrientation;

	// For sparkles
float	ThicknessRandom;
float	BrightnessRandom;
float	Spread;
float	SpreadRandom;
float	SpreadOffset;


// ===================================================================================
// COMMON VERTEX SHADER
// ===================================================================================
//
struct VS_IN
{
	float2	UV			: TEXCOORD0;
};

struct GS_IN
{
	float	ScaleFactor			: SCALE_FACTOR;		// The global light scale factor
	float3	BrightnessFactor	: BRIGHTNESS_FACTOR;// The global brightness factor
};

GS_IN VS( VS_IN _In )
{
	// Read back light data
	float4	LightData = LightIntensityScale.SampleLevel( NearestClamp, 0.5.xx, 0 );

	// Forward vertex data
	GS_IN	Out;
	Out.BrightnessFactor = GlobalBrightnessFactor * LightData.xyz;
	Out.ScaleFactor = LightData.w;

	return	Out;
}

// ===================================================================================
// RING EFFECT
// ===================================================================================
//
struct PS_IN_RING
{
	float4	__Position			: SV_POSITION;
	float2	Position			: POSITION;			// Position in EFFECT space
	float	CompletionRotation	: COMPLETION_ROT;
 	float	DynamicTriggering	: DYN_TRIGGER;
	float3	Brightness			: BRIGHTNESS;
};

[maxvertexcount( 4 )]
void	GS_Ring( point GS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN_RING> _OutStream )
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

	// Output vertices
	PS_IN_RING	Out;
	Out.CompletionRotation = -CompletionRotation + ComputeCircularCompletionAdditionalRotation( AutoRotateModeCompletion, LocalRotation, EffectCenter );
	Out.DynamicTriggering = Trig;
	Out.Brightness = LocalBrightness;

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

float3	PS_Ring( PS_IN_RING _In ) : SV_TARGET0
{
	float2	P = _In.Position;

	// Apply dynamic triggering
	float	LocalCompletion = Completion + _In.DynamicTriggering*CompletionOffset;

	// Compute distance to center
	float	Angle = atan2( P.x, P.y );
	float	Distance2Center = 0.05 * length( P );

	float	MinDistance = 1.0 - Thickness;
	float	NormalizedDistance = (Distance2Center - MinDistance) / (1.0 - MinDistance);
	float	RelativeDistance = 2.0 * NormalizedDistance - 1.0;	// -1 at min distance, +1 at max distance
	clip( 1.0 - abs(RelativeDistance) );

	// Compute intensity
	float	Inside = smoothstep( -1.0, -1.0+InsideFeathering, RelativeDistance );
	float	Outside = smoothstep( 1.0, 1.0-OutsideFeathering, RelativeDistance );
	float	Intensity = Inside * Outside;
			Intensity *= ComputeCircularCompletion( PI - Angle, LocalCompletion, CompletionFeathering, _In.CompletionRotation );

	// Apply colorize
	float3	Color = Colorize( NormalizedDistance, _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );
	return 0.1 * Intensity * Color;
}

// ===================================================================================
// HOOP EFFECT
// ===================================================================================
//
// The hoop is drawn between 2 circles that have both a common point which is the light's position
//	and a "middle point" between the 2 circles that stands at "Distance" distance from the light
//
struct PS_IN_HOOP
{
	float4	__Position			: SV_POSITION;
	float2	Position			: POSITION;			// Position in EFFECT space
	float	CompletionRotation	: COMPLETION_ROT;
 	float	DynamicTriggering	: DYN_TRIGGER;
	float3	Brightness			: BRIGHTNESS;
	float2	Diameters			: CIRCLE_DIAMETERS;
};

[maxvertexcount( 4 )]
void	GS_Hoop( point GS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN_HOOP> _OutStream )
{
	// Compute effect center
	float2	Light2Center = float2( ScreenAspectRatio, 1.0 ) * (50.0.xx - LightPosition);
	float	Distance2Center = length( Light2Center );
	float2	EffectCenter = LightPosition + Offset;

	// Compute diameters of in and out circles
	float	HoopLength = 0.4;
	float	DiameterIn = (abs(Distance.x) - 0.25 * HoopLength) * Distance2Center;
	float	DiameterOut = (abs(Distance.x) + (Length-0.25) * HoopLength) * Distance2Center;

	float	Angle = atan2( -Light2Center.x, Light2Center.y );
	if ( Distance.x < 0.0 )
		Angle += PI;

	// Apply dynamic triggering
	float	Trig = ApplyDynamicTriggering( EffectCenter );
	float3	LocalBrightness = _In[0].BrightnessFactor * (Brightness + Trig*BrightnessOffset) * saturate( Distance2Center / 50.0 );
	float	LocalScale = _In[0].ScaleFactor * LightSize * (Scale + Trig*ScaleOffset);
	float2	LocalStretch = Stretch + Trig*StretchOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, Angle, 0, AspectRatio );

	// Output vertices
	PS_IN_HOOP	Out;
	Out.CompletionRotation = CompletionRotation + ComputeCircularCompletionAdditionalRotation( AutoRotateModeCompletion, Rotation, EffectCenter );
	Out.DynamicTriggering = Trig;
	Out.Brightness = LocalBrightness;
	Out.Diameters = float2( DiameterIn, DiameterOut );

	Out.Position = float2( -0.7 * DiameterOut, DiameterOut );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( -0.7 * DiameterOut, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( +0.7 * DiameterOut, DiameterOut );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( +0.7 * DiameterOut, 0.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
}

float3	PS_Hoop( PS_IN_HOOP _In ) : SV_TARGET0
{
	float2	P = _In.Position;

	// Using magic trigonometry, given the angle of the current pixel with the origin,
	//	we can retrieve the Min and Max distances at which this point is inside the hoop
	//
	//  |        /
	//  . .     /
	//  |   .  /
	//  |     x
	//  |    / .
	// D| d /  .
	//  |  /   .
	//  | /   .
	//  |/_)_._______
	//
	// The law of cosines tells us that: d² = 2R² * (1-cos(2*Theta))
	// And cos(2*Theta) = 2 cos²(Theta) - 1
	// Thus : d² = 4R² * (1-cos²(Theta)) = 4R²sin²(Theta)
	// So : d=2R*sin(Theta) = D*sin(Theta)
	//
	float2	ToPoint = P;
	float	Distance2PointIn = length( ToPoint );
	ToPoint /= Distance2PointIn;
	float	SinThetaIn = ToPoint.y;
	float	HoopDistanceIn = _In.Diameters.x * SinThetaIn;

	ToPoint = P;
	ToPoint.x *= 0.6;
	float	Distance2PointOut = length( ToPoint );
	ToPoint /= Distance2PointOut;
	float	SinThetaOut = ToPoint.y;
	float	HoopDistanceOut = _In.Diameters.y * SinThetaOut;

	// Modulate outside distance by complexity and random
	float	Angle = asin( SinThetaIn );
	float4	N = GetNoise( 0.0037 * (1.0+Angle) * Complexity );//, RandomSeed );
	float	AnimAmplitude = 1.0 - AnimAmount * 0.5 * (1.0 + sin( 0.01 * PI * (Time + 7.0 * N.z) * AnimSpeed ));

	HoopDistanceOut = lerp( HoopDistanceOut, HoopDistanceIn, 0.5 * min( 1.0, Detail*(N.x + AnimAmplitude) ) );

	// Clip whatever lies outside
	clip( Distance2PointIn - HoopDistanceIn );
	clip( HoopDistanceOut - Distance2PointIn );

	// Apply dynamic triggering
	float	LocalCompletion = Completion + _In.DynamicTriggering*CompletionOffset;

	// Compute intensity
	float	NormalizedDistance = (Distance2PointIn - HoopDistanceIn) / (HoopDistanceOut-HoopDistanceIn);
	float	RelativeDistance = 2.0 * NormalizedDistance - 1.0;
	float	Intensity = 1.0 - RelativeDistance*RelativeDistance;
			Intensity *= ComputeCircularCompletion( 2.0 * Angle, LocalCompletion, 0.5 * CompletionFeathering, _In.CompletionRotation );

	// Apply colorize
	float3	Color = Colorize( NormalizedDistance, _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );

	float	Lum = dot( float3( 0.3, 0.5, 0.2 ), _In.Brightness );
	float	IntensityFactor = lerp( 0.4, 0.1, sqrt( Lum * 0.1 ) );	// Intensity factor varies with brightnes..
	return IntensityFactor * Intensity * Color;
}

// ===================================================================================
// GLOW EFFECT
// ===================================================================================
//
// Glow is a "simple" attenuation based on distance (actually, I couldn't retrieve the exact formula they used but I'm close enough)
//
struct PS_IN_GLOW
{
	float4	__Position			: SV_POSITION;
	float2	Position			: POSITION;			// Position in EFFECT space
	float3	Brightness			: BRIGHTNESS;
	float	IntensityScale		: INTENSITy_SCALE;
};

[maxvertexcount( 4 )]
void	GS_Glow( point GS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN_GLOW> _OutStream )
{
	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( Distance, Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float		Trig = ApplyDynamicTriggering( EffectCenter );
	float3		LocalBrightness = _In[0].BrightnessFactor * (Brightness + Trig*BrightnessOffset);
	float		LocalScale = _In[0].ScaleFactor * LightSize * (Scale + Trig*ScaleOffset);
	float2		LocalStretch = Stretch + Trig*StretchOffset;
	float		LocalRotation = Rotation - Trig*RotationOffset;

	// Effect scale depends on brightness
	float	Lum = dot( LocalBrightness, float3( 0.3, 0.5, 0.2 ) );

	float3	SourceColor = 1.0;
	if ( ColorSource == 0 )
		SourceColor = GlobalColor;	// Global color
	else if ( ColorSource != 2 )
		SourceColor = Color1;		// Custom/Gradient
	Lum *= dot( SourceColor, float3( 0.3, 0.5, 0.2 ) );				// Luminance also depends on source color (except in spectrum mode)

	float	t = saturate( (Lum - 10.0) / (1.25 - 10.0) );			// 1 at Brightness=125, 0 at Brightness=1000
	float	IntensityScale = 2.45 + (2.93 - 2.45) * t * t * t;		// The ratio is 2.45 at 1000 and 2.93 at 125 and decreases "cubically" (or so it seems)
	LocalScale *= IntensityScale * 0.2 * Lum;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	// Compute glow's dimensions
	// We know that Intensity = 0.175 * lerp( 0.5, 2.125, (_In.Brightness-1.0) / (4.0-1.0) ) / max( 1e-2, Gamma * 0.006 * Distance2Center )
	// So we need to reverse that formula to find at which distance the Intensity goes below a given threshold
	static const float	GLOW_INTENSITY_THRESHOLD = 5e-3;
	float	GlowSize = 100000.0;//0.175 * lerp( 0.5, 2.125, (LocalBrightness-1.0) / (4.0-1.0) ) / (Gamma * 0.006 * GLOW_INTENSITY_THRESHOLD);
	GlowSize = min( 1e6, GlowSize );

	// Output vertices
	PS_IN_GLOW	Out;
	Out.Brightness = LocalBrightness;
	Out.IntensityScale = IntensityScale;

	Out.Position = float2( -GlowSize, -GlowSize );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( -GlowSize, +GlowSize );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( +GlowSize, -GlowSize );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = float2( +GlowSize, +GlowSize );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
}

float3	PS_Glow( PS_IN_GLOW _In ) : SV_TARGET0
{
	float2	P = _In.Position;

	// Compute intensity
 	float	Lum = dot( _In.Brightness, float3( 0.3, 0.5, 0.2 ) );

	float	Distance2Center = length( P );
	float	Dist = pow( 0.06 * Distance2Center, 1.0 / 2.5 );
	float	Intensity = _In.IntensityScale * max( 1.0, Lum ) * exp( -Dist );
 			Intensity = pow( Intensity, 1.0 / Gamma );

	// Apply colorize
	float	NormalizedDistance = 0.0052 * Distance2Center * _In.IntensityScale;
			NormalizedDistance *= 0.18 * (Lum > 1.25 ? Lum : 1.5625 / Lum);
	float3	Color = Colorize( NormalizedDistance, 1.0, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );

	return Intensity * Color;
}

// ===================================================================================
// SPARKLE EFFECT
// ===================================================================================
//
// Sparkle is a set of thin rays radiating from a fixed ring away from the center
//
struct PS_IN_SPARKLE
{
	float4	__Position			: SV_POSITION;
	float2	Position			: POSITION;			// Position in EFFECT space
	float	CompletionRotation	: COMPLETION_ROT;
 	float	DynamicTriggering	: DYN_TRIGGER;
	float3	Brightness			: BRIGHTNESS;
};

[maxvertexcount( 4 )]
void	GS_Sparkle( point GS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN_SPARKLE> _OutStream )
{
	// Compute effect center
	float2		EffectCenter = ComputeEffectCenter( Distance, Offset, TranslationMode, CustomTranslation );

	// Apply dynamic triggering
	float		Trig = ApplyDynamicTriggering( EffectCenter );
	float		LocalBrightness = _In[0].BrightnessFactor * (Brightness + Trig*BrightnessOffset);
	float		LocalScale = _In[0].ScaleFactor * LightSize * (Scale + Trig*ScaleOffset);
	float2		LocalStretch = Stretch + Trig*StretchOffset;
	float		LocalRotation = Rotation - Trig*RotationOffset;

	// Compute effect transform
	float3x2	Effect2Screen = ComputeEffect2ScreenTransform( EffectCenter, LocalScale, LocalStretch, LocalRotation, AutoRotateMode, AspectRatio );

	// Compute effect size
	float		LocalSpread = Spread + Trig*SpreadOffset;
	float		SparkleSize = 0.3 * LocalSpread + 0.533 * 0.4 * Length;

	// Output vertices
	PS_IN_SPARKLE	Out;
	Out.CompletionRotation = CompletionRotation + ComputeCircularCompletionAdditionalRotation( AutoRotateModeCompletion, LocalRotation, EffectCenter );
	Out.DynamicTriggering = Trig;
	Out.Brightness = LocalBrightness;

	Out.Position = SparkleSize * float2( -1.0, -1.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = SparkleSize * float2( -1.0, +1.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = SparkleSize * float2( +1.0, -1.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );

	Out.Position = SparkleSize * float2( +1.0, +1.0 );
	Out.__Position = TransformEffect2ClipSpace( Out.Position, Effect2Screen );
	_OutStream.Append( Out );
}

float3	PS_Sparkle( PS_IN_SPARKLE _In ) : SV_TARGET0
{
	float2	P = _In.Position;

	// Apply dynamic triggering
	float	LocalCompletion = Completion + _In.DynamicTriggering*CompletionOffset;
	float	LocalSpread = Spread + _In.DynamicTriggering*SpreadOffset;
			LocalSpread *= 0.3;				// Base distance at which the sparkles radiate
	float	LocalLength = 0.533 * Length;	// Distance unto which the sparkes radiate

	// Compute sparkle
	float	Distance2Center = length( P );
	float	Angle = PI + ShapeOrientation + atan2( P.x, P.y );
	float	BranchAngularCover = 2.0 * PI / Complexity;		// Angle covered by a single branch
	float	BranchIndex = floor( Angle / BranchAngularCover );
	float	BranchAngle = (BranchIndex+0.5) * BranchAngularCover;

	// Add random
	float4	Random = FBM2( 59.17 * BranchIndex, RandomSeed );

	float	AnimAmplitude = 1.0 - AnimAmount * 0.5 * (1.0 + sin( PI * (Time + 57.0 * Random.z) * AnimSpeed ));

		// Length random (Animated)
	LocalLength = lerp( LocalLength, 0.0, AnimAmplitude * LengthRandom * Random.x );

		// Spread random
	LocalSpread = lerp( LocalSpread, 0.0, SpreadRandom * Random.y );

		// Angular random
	BranchAngle += BranchAngularCover * SpacingRandom * (2.0 * Random.z - 1.0);

		// Thickness random
	float	LocalThickness = lerp( Thickness, 0.01, ThicknessRandom * Random.w );

		// Brightness random (Animated)
	_In.Brightness = lerp( _In.Brightness, 0.1, AnimAmplitude * BrightnessRandom * 0.5 * (Random.w + Random.x) );

	// Branches of the sparkle are like little ellipsoids
	// The ellipsoid is constant in effect space : it does not change size based on the angular coverage of a branch
	// We need to determines its 2 radii : the first radius is given by the length, the second by the thickness
	float2	cs;
	sincos( 0.5*PI+BranchAngle, cs.y, cs.x );
	float2	AxisX = cs.xy;					// Axis of length
	float2	AxisY = float2( -cs.y, cs.x );	// Axis of thickness
	float2	Center = AxisX * LocalSpread;	// Center of ellipsoid
	float2	Radius = 0.4 * float2( LocalLength, LocalThickness );	// The 2 radii of the ellipsoid

	sincos( 0.5*PI+Angle, cs.y, cs.x );
	P = Distance2Center * cs;

	// Transform the position into ellipsoid space
	float2	PositionEllipsoid = P - Center;
			PositionEllipsoid = float2( dot(PositionEllipsoid,AxisX), dot(PositionEllipsoid,AxisY) );
			PositionEllipsoid /= Radius;

	// Now, intensity will depend on distance from the center
	float	Dot = dot( PositionEllipsoid, PositionEllipsoid );
	clip( 1.0 - Dot );
	float	Intensity = 1.0 - Dot;
			Intensity *= ComputeCircularCompletion( Angle - ShapeOrientation, LocalCompletion, CompletionFeathering, _In.CompletionRotation );

	// We must determine statistical overlap of branches at that distance
	// It's not exact science here since branches don't necessarily overlap due to spread/thickness randomness
	float	BranchSize = 2.0*PI*Distance2Center / Complexity;	// Size of the branch at that distance
	float	Overlap = max( 1.0, 2.0 * Radius.y / BranchSize );	// We always cover at least our branch but can overlap other branches too
//	Intensity *= Overlap;

	// Colorize
	float	NormalizedDistance = 0.05 * (Distance2Center + 110.0 - 2.0*LocalSpread);
	float3	Color = Colorize( NormalizedDistance, _In.Brightness, ColorSource, Color1, Color2, GradientLoops, GradientOffset, bReverseGradient );

	return 0.1 * Intensity * Color;
}

// ===================================================================================
//
technique10 DisplayRing
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS_Ring() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Ring() ) );
	}
}

technique10 DisplayHoop
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS_Hoop() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Hoop() ) );
	}
}

technique10 DisplayGlow
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS_Glow() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Glow() ) );
	}
}

technique10 DisplaySparkle
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS_Sparkle() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Sparkle() ) );
	}
}
