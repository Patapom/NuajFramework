// Lens-Flare post-process common functions
// Standard functions are :
//	_ Dynamic Triggering, that handles the dynamic lens object attenuation based on its position on screen
//	_ Transformation, that handles the PRS transform of screen positions into effect positions
//	_ Colorization, that handles the coloring of lens objects using single colors or gradients
//	_ Circular Completion, that handles lens object angular attenuation
//
// ===================================================================================

static const float	PI = 3.14159265358979;

float		Time;
float3		GlobalColor;		// Global color (i.e. lens flare's reference color)
float		Brightness;			// Object's brightness
float		GlobalBrightnessFactor;	// Global brightness level

float2		LightPosition;		// Projected light position in [0,100]
float		LightSize;			// Light scale factor (1.0 is default flare size)
float		LightManualTrigger;	// A manual trigger driven by the light (this parameters allows the user to override the dynamic trigger mechanism by hand)

float3		InvSourceSize;		// Source image normalizers (1/width, 1/height, 0.0)
float		ScreenAspectRatio;	// Source image aspect ratio

// This is a 1x1 texture provided by the user that gives the light's intensity
// We assume the lens-flare user computed his light's occlusion and intensity in a previous stage
//	that finally resulted into a 1x1 texture containing the float4 (XYZ=Color, W=Scale) to use for the light
Texture2D	LightIntensityScale;


// Common Settings
float		Scale;
float2		Stretch;
float2		Distance;
float		Rotation;
int			AutoRotateMode;		// None/ToLight/ToCenter
float2		Offset;
int			TranslationMode;	// Free/Horizontal/Vertical/None/Custom
float2		CustomTranslation;
float		AspectRatio;
float		RandomSeed;

// Colorize
int			ColorSource;		// Global/Custom/Gradient/Spectrum
float3		Color1;
float3		Color2;
float		GradientLoops;
float		GradientOffset;
bool		bReverseGradient;

// Circular completion
float		Completion;
float		CompletionFeathering;
float		CompletionRotation;
int			AutoRotateModeCompletion;	// None/Object/ToLight/ToCenter

// Dynamic triggering
bool		bEnableTrigger;
float		BrightnessOffset;
float		ScaleOffset;
float2		StretchOffset;
float		RotationOffset;
float		CompletionOffset;
float3		ColorShift;
int			TriggerType;		// Border/Center/Light
int			TriggerMode;		// Object/Light
bool		bInvertTrigger;
float		BorderWidth;
float		Expansion;
float		InnerFallOffRange;
float		OuterFallOffRange;
int			FallOffType;		// Linear/Smooth/Exponential
float2		TriggerStretch;
float2		TriggerOffset;

// Animation
float		AnimSpeed;
float		AnimAmount;


// ===================================================================================

SamplerState NearestClamp
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

SamplerState LinearClamp
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

SamplerState LinearBorder
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Border;
	AddressV = Border;
	AddressW = Border;
	BorderColor = float4( 0, 0, 0, 0 );
};

// ===================================================================================
// Helpers

// RGB / HSL conversion from http://en.wikipedia.org/wiki/HSL_and_HSV
// Note : Hue is in [0,6], beware !
//
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

		// 3] Hue (in [0,6], beware !)
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

// Determines if the position is within the given rectangle
bool	IsInsideRect( float2 _Position, float4 _Rect )
{
	return _Position.x >= _Rect.x &&  _Position.x <= _Rect.z && _Position.y >= _Rect.y && _Position.y <= _Rect.w;
}

// Interpolates position knowing it's standing between left and right
float	InterpolateSingle( float _Position, float _Left, float _Right, int _FallOffType )
{
	float	t = (_Position - _Left) / (_Right - _Left);
	if ( _FallOffType == 0 )
		return saturate( t );			// Linear
	else if ( _FallOffType == 1 )
	{									// Smooth (3x^2-2x^3)
		t = saturate( t );
		return t*t*(3.0-t*2.0);
	}

	// else if ( _FallOffType == 2 )
	return 1.0 - exp( -4.0 * t*t );		// Exponential
}

// Interpolates the trigger value given a position and 2 rectangles
// _R0 must be the innermost rectangle
// _R1 must be the outermost rectangle
float	InterpolateTrigger( float2 _Position, float4 _R0, float4 _R1, int _FallOffType )
{
	float2	Center = 0.5 * (_R0.xy + _R0.zw);	// Both rectangle have the same center so use any of them...

	float	TestBorder0X, TestBorder1X;
	if ( _Position.x < Center.x )
	{	// Left border
		TestBorder0X = _R0.x;
		TestBorder1X = _R1.x;
	}
	else
	{	// Right border
		TestBorder0X = _R0.z;
		TestBorder1X = _R1.z;
	}

	float	TestBorder0Y, TestBorder1Y;
	if ( _Position.y < Center.y )
	{	// Top border
		TestBorder0Y = _R0.y;
		TestBorder1Y = _R1.y;
	}
	else
	{	// Bottom border
		TestBorder0Y = _R0.w;
		TestBorder1Y = _R1.w;
	}

	return max(	InterpolateSingle( _Position.x, TestBorder0X, TestBorder1X, _FallOffType ),
				InterpolateSingle( _Position.y, TestBorder0Y, TestBorder1Y, _FallOffType ) );
}

// ===================================================================================
// Dynamic triggering functions

// Computes the dynamic triggering data for an object based on the provided position
// My computation is slightly wrong compared to the one in Optical Flares as the outer zone
//	in Optical Flares is round in the corners, mine is square... They certainly mess with 
//	cartesian distance at some point while I'm only using manhattan distance to the nearest border...
//
//	_Position, the position of the object to measure triggering for
//	_bInvertTrigger, inverts trigger zone
//	_BorderWidth, size of the trigger zone
//	_Expansion, screen scale factor measured in screen units (e.g. -50 brings the border to the middle of the screen)
//	_InnerFallOffRange, range of falloff after which the trigger has no influence (inside of screen)
//	_OuterFallOffRange, range of falloff after which the trigger has no influence (outside of screen)
//	_FallOffType, type of falloff (0=linear, 1=smooth cubic, 2=gaussian)
//	_TriggerStretch, trigger scale factor in X and Y
//	_TriggerOffset, center offset in X and Y
//
float	ComputeDynamicTriggeringBorder( float2 _Position, bool _bInvertTrigger, float _BorderWidth, float _Expansion, float _InnerFallOffRange, float _OuterFallOffRange, int _FallOffType, float2 _TriggerStretch, float2 _TriggerOffset )
{
	_TriggerOffset.y = -_TriggerOffset.y;
	_TriggerOffset += 50.0;
	_BorderWidth *= 0.5;

	float2	BorderDistance = 50.0 + _Expansion;

	// Compute inner box rectangles
	float4	InnerBoxStart = float4(
		_TriggerOffset - (BorderDistance - _TriggerStretch * (_BorderWidth)),
		_TriggerOffset + (BorderDistance - _TriggerStretch * (_BorderWidth)) );
	float4	InnerBoxEnd = float4(
		_TriggerOffset - (BorderDistance - _TriggerStretch * (_BorderWidth + _InnerFallOffRange)),
		_TriggerOffset + (BorderDistance - _TriggerStretch * (_BorderWidth + _InnerFallOffRange)) );

	// Compute outer box rectangles
	float4	OuterBoxStart = float4(
		_TriggerOffset - (BorderDistance + _TriggerStretch * (_BorderWidth)),
		_TriggerOffset + (BorderDistance + _TriggerStretch * (_BorderWidth)) );
	float4	OuterBoxEnd = float4(
		_TriggerOffset - (BorderDistance + _TriggerStretch * (_BorderWidth + _OuterFallOffRange)),
		_TriggerOffset + (BorderDistance + _TriggerStretch * (_BorderWidth + _OuterFallOffRange)) );

	// Check inside which box we are
	bool	bInside0 = IsInsideRect( _Position, InnerBoxEnd );		// 0 zone
	bool	bInside1 = IsInsideRect( _Position, InnerBoxStart );	// 0->1 zone
	bool	bInside2 = IsInsideRect( _Position, OuterBoxStart );	// 1 zone
	bool	bInside3 = IsInsideRect( _Position, OuterBoxEnd );		// 1->0 zone

	float	TriggerIntensity = 0.0;
	if ( bInside0 )
	{	// Inside the innermost zone : intensity is 0
	}
	else if ( bInside1 )
	{	// Inside the 0->1 interpolating zone...
		TriggerIntensity = InterpolateTrigger( _Position, InnerBoxEnd, InnerBoxStart, _FallOffType );
	}
	else if ( bInside2 )
	{	// Inside the full trigger zone : intensity is 1
		TriggerIntensity = 1.0;
	}
	else if ( bInside3 )
	{	// Inside the 1->0 interpolating zone...
		TriggerIntensity = 1.0 - InterpolateTrigger( _Position, OuterBoxStart, OuterBoxEnd, _FallOffType );
	}
	else
	{	// Inside the outermost zone : intensity is 0
	}

	return _bInvertTrigger ? 1.0 - TriggerIntensity : TriggerIntensity;
}

// Computes the dynamic triggering data for an object based on the provided position
//
//	_Position, the position of the object to measure triggering for
//	_bInvertTrigger, inverts trigger zone
//	_Expansion, screen scale factor measured in screen units (e.g. -50 brings the border to the middle of the screen)
//	_OuterFallOffRange, range of falloff after which the trigger has no influence
//	_FallOffType, type of falloff (0=linear, 1=smooth cubic, 2=gaussian)
//	_TriggerStretch, trigger scale factor in X and Y
//	_TriggerOffset, center offset in X and Y
//
float	ComputeDynamicTriggeringCenter( float2 _Position, bool _bInvertTrigger, float _Expansion, float _OuterFallOffRange, int _FallOffType, float2 _TriggerStretch, float2 _TriggerOffset )
{
	_TriggerOffset.y = -_TriggerOffset.y;
	_TriggerOffset += 50.0;

	float2	Center2Position = (_Position - _TriggerOffset) / _TriggerStretch;
	float	Distance2Center = max( 0, length( Center2Position ) - _Expansion );

	float	TriggerIntensity = InterpolateSingle( Distance2Center, 0.0, _OuterFallOffRange, _FallOffType );

	return _bInvertTrigger ? TriggerIntensity : 1.0 - TriggerIntensity;
}

// Applies dynamic triggering to the provided position (i.e. usually the effect's position)
float	ApplyDynamicTriggering( float2 _Position )
{
	if ( !bEnableTrigger )
		return LightManualTrigger;

	// Check if we should use the light's position instead
	if ( TriggerMode == 1 )
		_Position = LightPosition;

	float	Result = 0.0;
	if ( TriggerType == 0 )
		Result = ComputeDynamicTriggeringBorder( _Position, bInvertTrigger, BorderWidth, Expansion, OuterFallOffRange, InnerFallOffRange, FallOffType, TriggerStretch, TriggerOffset );
	else
		Result = ComputeDynamicTriggeringCenter( _Position, bInvertTrigger, Expansion, OuterFallOffRange, FallOffType, TriggerStretch, TriggerOffset );

	// Compose with manual trigger
	return max( LightManualTrigger, Result );
}

// Pass-in the SV_POSITION.xy in the pixel shader to show the trigger area
float3	TestDynamicTriggering( float2 _ScreenPosition )
{
	return float3( 0.5 * ApplyDynamicTriggering( 100.0 * _ScreenPosition * InvSourceSize.xy ), 0.0, 0.0 );
}

// ===================================================================================
// Position / Rotation / Scale transform
// In "Optical Flares", the positions range from (0,0) in the top left corner to (100,100) in the bottom right corner (50,50 being the center of the screen)
// This is true no matter the aspect ratio of the image.
// This is important to use the same convention because all offset, translation and range values in the plug-in use that metric.

// Computes the position of the center of the effect on screen
//
float2		ComputeEffectCenter( float2 _Distance, float2 _Offset, int _TranslationType, float2 _CustomTranslation )
{
	float2	Center = 50.0;	// Screen center
	float2	Light2Center = Center - LightPosition;

	float2	Translation = _Distance;
	switch ( _TranslationType )
	{
	case 0:	// Free : Distance.x guides both distances
		Translation = _Distance.xx;
		break;
	case 1:	// Horizontal free + Vertical constrained
		Translation.x += Translation.y;
		break;
	case 2:	// Vertical free + Horizontal constrained
		Translation.y += Translation.x;
		break;
	case 3:	// None
		Translation = 1.0 + Translation.yy;
		break;
	case 4:	// Custom
		Translation *= _CustomTranslation;
		break;
	}

	return LightPosition + _Offset + Translation * Light2Center;	// A translation of 0 leaves us at light position, 1 brings us to center
}

// Computes the 3x2 transform matrix capable of transforming an effect point into a screen point
// In EFFECT SPACE, (0,0) is the center of the effect
//
float3x2	ComputeEffect2ScreenTransform( float2 _EffectCenter, float _Scale, float2 _Stretch, inout float _Rotation, int _AutoRotateMode, float _AspectRatio )
{
	float2	Center = 50.0;	// Screen center

	// Compute auto-rotation
	if ( _AutoRotateMode == 1 )
	{	// To light => the rotation angle depends on the angle formed between the light and the object
		float2	Delta = LightPosition - _EffectCenter;
		if ( abs(Delta.x) < 1e-6 )
			Delta.x = 1e-6;
		_Rotation += atan2( Delta.y, ScreenAspectRatio * Delta.x );
	}
	else if ( _AutoRotateMode == 2 )
	{	// To center => the rotation angle depends on the angle formed between the center and the object
		float2	Delta = Center - _EffectCenter;
		if ( abs(Delta.x) < 1e-6 )
			Delta.x = 1e-6;
		_Rotation += atan2( Delta.y, ScreenAspectRatio * Delta.x );
	}

	// Compute axes and scale
	float2	cs;
	sincos( _Rotation, cs.y, cs.x );

	float2	X = cs;
	float2	Y = float2( -cs.y, cs.x );
	float2	S = float2( _Stretch.x * _Scale, _Stretch.y * _Scale );
	float	RatiosRatio = _AspectRatio / ScreenAspectRatio;	// :)

	X *= S.x;
	Y *= S.y;

	// Build final transform matrix
	float3x2	Result;
	Result[0][0] = X.x * RatiosRatio;
	Result[0][1] = X.y;
	Result[1][0] = Y.x * RatiosRatio;
	Result[1][1] = Y.y;
	Result[2][0] = _EffectCenter.x;
	Result[2][1] = _EffectCenter.y;

	return Result;
}

float4	TransformEffect2ClipSpace( float2 _EffectPosition, float3x2 _Effect2Screen )
{
	float2	ScreenPosition = mul( float3( _EffectPosition, 1.0 ), _Effect2Screen );
	return float4( (0.02 * ScreenPosition.x - 1.0), 1.0 - 0.02 * ScreenPosition.y, 0.5, 1.0 );
}

// ===================================================================================
// Colorization
// I hand picked their gradient colors here... Not an easy job ! >:(
static const float3	SpectrumGradient[] = {
	float3( 195,0,210 ) / 255.0,
	float3( 240,0,115 ) / 255.0,
	float3( 253,112,0 ) / 255.0,
	float3( 237,187,0 ) / 255.0,
	float3( 192,235,0 ) / 255.0,
	float3( 113,253,107 ) / 255.0,
	float3( 0,237,191 ) / 255.0,
	float3( 0,193,239 ) / 255.0,
	float3( 0,116,258 ) / 255.0,
	float3( 114,0,242 ) / 255.0,
	float3( 195,0,210 ) / 255.0,	// Loop
};

float	smoothlerp( float a, float b, float t )
{
	return lerp( a, b, smoothstep( 0.0, 1.0, t ) );
}

float3	smoothlerp( float3 a, float3 b, float t )
{
	return lerp( a, b, smoothstep( 0.0, 1.0, t ) );
}

float3	Colorize( float _Distance2Center, float3 _Brightness, int _ColorSource, float3 _Color0, float3 _Color1, float _GradientLoops, float _GradientOffset, bool _bReverseGradient )
{
	float3	Result = _ColorSource == 0 ? GlobalColor : _Color0;

	// Handle gradient modes
	float	GradientValue = _GradientLoops * _Distance2Center + 0.15915494309189533576888376337251 * _GradientOffset;	// 1/2PI
	if ( _ColorSource == 3 )
	{	// Gradient
		float3	Gradient[] = { _Color0, _Color1, _Color0 };
		float	t = (2.0 * (GradientValue+0.75)) % 2.0;
		int		Index = int( floor( t ) );
		t = _bReverseGradient ? Index+1-t : t-Index;
		Result = smoothlerp( Gradient[Index+0], Gradient[Index+1], t );
	}
	else if ( _ColorSource == 2 )
	{	// Spectrum
		float	t = (10.0 * GradientValue) % 10.0;
		int		Index = int( floor( t ) );
		t = _bReverseGradient ? Index+1-t : t-Index;
		Result = lerp( SpectrumGradient[Index+0], SpectrumGradient[Index+1], t );
	}

	return max( 0.0, _Brightness ) * max( 0.0039, Result );	// We never accept 0 values so even a pure red light always gets white when saturated..
}

// ===================================================================================
// Circular completion
float	ComputeCircularCompletionAdditionalRotation( int _CompletionRotationType, float _ObjectRotation, float2 _EffectCenter )
{
	// Auto-rotation
	if ( _CompletionRotationType == 1 )
	{	// ObjectRotation => Add the rotation of the object
		return 0.0;//-_ObjectRotation;
	}
	else if ( _CompletionRotationType == 2 )
	{	// To Light => the rotation angle depends on the angle formed between the light and the object
		float2	Delta = LightPosition - _EffectCenter;
#ifdef _DEBUG
		if ( dot(Delta,Delta) == 0.0 )	// Exactly 0 yields NaN
			return 0.0;
#endif
		return _ObjectRotation + atan2( ScreenAspectRatio * Delta.x, Delta.y );
	}
	else if ( _CompletionRotationType == 3 )
	{	// To Center => the rotation angle depends on the angle formed between the center and the object
		float2	Delta = 50.0 - _EffectCenter;
#ifdef _DEBUG
		if ( dot(Delta,Delta) == 0.0 )	// Exactly 0 yields NaN
			return 0.0;
#endif
		return _ObjectRotation + atan2( ScreenAspectRatio * Delta.x, Delta.y );
	}

	return _ObjectRotation;
}

// Light version of the computation that does not computes the completion rotation's angle on the fly but takes the one computed by the ComputeCircularCompletionAdditionalRotation() function
// Usually, you call ComputeCircularCompletionAdditionalRotation() in the VS or GS and pass the angle to the PS
// Then you call this function in the PS passing it the CompletionRotation angle + the angle computed by the VS/GS
float	ComputeCircularCompletion( float _Angle, float _Completion, float _CompletionFeathering, float _CompletionRotation )
{
	// Compute attenuation based on completion
	_Angle = abs( ((5.0*PI+_Angle+_CompletionRotation) % (2.0 * PI)) - PI );

	float	CompletionEnd = PI - 0.5 * _Completion;
	float	CompletionStart = CompletionEnd * (1.0 - 2.0 * _CompletionFeathering);

	return lerp( 0.0, 1.0, saturate((_Angle-CompletionStart) / (CompletionEnd-CompletionStart)) );
//	return smoothstep( CompletionStart-1e-3, CompletionEnd, _Angle );
}

// Full completion computation (ill advised to use in a PS, rather separate the 2 calls by hand : ComputeCircularCompletionAdditionalRotation() in the VS/GS and light ComputeCircularCompletion() in the PS)
// _Angle, the effect angle from the center (usually atan2( Y, X ))
// _Completion, the completion angle (0 is full, 2*PI is empty)
// _ObjectRotation, the object's own rotation (only used in rotation type 1)
// _EffectCenter, the effect center in SCREEN space
float	ComputeCircularCompletion( float _Angle, float _Completion, float _CompletionFeathering, float _CompletionRotation, int _CompletionRotationType, float _ObjectRotation, float2 _EffectCenter )
{
	// Compute additional angle
	_CompletionRotation += ComputeCircularCompletionAdditionalRotation( _CompletionRotationType, _ObjectRotation, _EffectCenter );

	return ComputeCircularCompletion( _Angle, _Completion, _CompletionFeathering, _CompletionRotation );
}

