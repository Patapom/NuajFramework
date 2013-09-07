// Holds Key/Rim/Fill shadow map support
//
#define SPLITS_COUNT	1
#define SHADOW_MAP_SUPPORT

float4x4		World2LightProjKey[SPLITS_COUNT]	: WORLD2LIGHTPROJ_KEY;
Texture2DArray	ShadowMapKey			: SHADOWMAP_KEY;
bool			ShadowEnabledKey		: SHADOW_ENABLED_KEY;

float4x4		World2LightProjRim[SPLITS_COUNT]	: WORLD2LIGHTPROJ_RIM;
Texture2DArray	ShadowMapRim			: SHADOWMAP_RIM;
bool			ShadowEnabledRim		: SHADOW_ENABLED_RIM;

float4x4		World2LightProjFill[SPLITS_COUNT]	: WORLD2LIGHTPROJ_FILL;
Texture2DArray	ShadowMapFill			: SHADOWMAP_FILL;
bool			ShadowEnabledFill		: SHADOW_ENABLED_FILL;

float4			ShadowSplits			: SHADOW_SPLITS;

float	ShadowExponent : SHADOW_EXPONENT = 80.0;


float	GetShadowFromSlice( float3 _WorldPosition, float4x4 _World2LightProj, Texture2DArray _ShadowMap, float _SlideIndex )
{
	// Transform into light projective space
	float4	LightPosition = mul( float4( _WorldPosition, 1.0 ), _World2LightProj );
	if ( LightPosition.z < 0.0 )
		return 1.0;	// Behind light...

	LightPosition /= LightPosition.w;
	if ( abs( LightPosition.x ) > 1.0 || abs( LightPosition.y ) > 1.0 )
		return 1.0;	// Out of frustum...

	// Sample the shadow map
	float2	UV = float2( 0.5 * (1.0+LightPosition.x), 0.5 * (1.0-LightPosition.y) );
	float	ExpDistanceBlocker2Light = _ShadowMap.SampleLevel( LinearClamp, float3( UV, _SlideIndex ), 0 ).x;

//	return LightPosition.z;
	return saturate( exp( -ShadowExponent * LightPosition.z ) * ExpDistanceBlocker2Light );
}

float3	GetShadowDebugFromSlice( float3 _WorldPosition, float4x4 _World2LightProj, Texture2DArray _ShadowMap, float _SlideIndex )
{
	// Transform into light projective space
	float4	LightPosition = mul( float4( _WorldPosition, 1.0 ), _World2LightProj );
	LightPosition /= LightPosition.w;

	// Sample the shadow map
	float2	UV = float2( 0.5 * (1.0+LightPosition.x), 0.5 * (1.0-LightPosition.y) );
	float	ExpDistanceBlocker2Light = _ShadowMap.SampleLevel( LinearClamp, float3( UV, _SlideIndex ), 0 ).x;

//	return float3( LightPosition.xyz );
	return float3( LightPosition.xy, ExpDistanceBlocker2Light );
}

// Computes the shadow term for the KEY light
float	ComputeShadowKey( float3 _WorldPosition )
{
	if ( !ShadowEnabledKey )
		return 1.0;	// No shadow...

	// Compute depth as viewed from camera
	float	Depth = dot( _WorldPosition - World2Camera[3].xyz, World2Camera[2].xyz );
//	return 0.1 * Depth;

	float	Shadow = 1.0;
	if ( Depth < ShadowSplits.y )		Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjKey[0], ShadowMapKey, 0.0 );
#if SPLITS_COUNT > 1
	else if ( Depth < ShadowSplits.z )	Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjKey[1], ShadowMapKey, 1.0 );
#endif
#if SPLITS_COUNT > 2
	else if ( Depth < ShadowSplits.w )	Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjKey[2], ShadowMapKey, 2.0 );
#endif
	return Shadow;
}

float4	ComputeShadowDebugKey( float3 _WorldPosition )
{
	// Compute depth as viewed from camera
	float	Depth = dot( _WorldPosition - World2Camera[3].xyz, World2Camera[2].xyz );

	float3	Shadow = 0.0;
	if ( Depth < ShadowSplits.y )		Shadow = GetShadowDebugFromSlice( _WorldPosition, World2LightProjKey[0], ShadowMapKey, 0.0 );
#if SPLITS_COUNT > 1
	else if ( Depth < ShadowSplits.z )	Shadow = GetShadowDebugFromSlice( _WorldPosition, World2LightProjKey[1], ShadowMapKey, 1.0 );
#endif
#if SPLITS_COUNT > 2
	else if ( Depth < ShadowSplits.w )	Shadow = GetShadowDebugFromSlice( _WorldPosition, World2LightProjKey[2], ShadowMapKey, 2.0 );
#endif
	return float4( Shadow, Depth );
}

// Computes the shadow term for the RIM light
float	ComputeShadowRim( float3 _WorldPosition )
{
	if ( !ShadowEnabledRim )
		return 1.0;	// No shadow...

	// Compute depth as viewed from camera
	float	Depth = dot( _WorldPosition - World2Camera[3].xyz, World2Camera[2].xyz );

	float	Shadow = 1.0;
	if ( Depth < ShadowSplits.y )		Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjRim[0], ShadowMapRim, 0.0 );
#if SPLITS_COUNT > 1
	else if ( Depth < ShadowSplits.z )	Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjRim[1], ShadowMapRim, 1.0 );
#endif
#if SPLITS_COUNT > 2
	else if ( Depth < ShadowSplits.w )	Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjRim[2], ShadowMapRim, 2.0 );
#endif
	return Shadow;
}

// Computes the shadow term for the FILL light
float	ComputeShadowFill( float3 _WorldPosition )
{
	if ( !ShadowEnabledFill )
		return 1.0;	// No shadow...

	// Compute depth as viewed from camera
	float	Depth = dot( _WorldPosition - World2Camera[3].xyz, World2Camera[2].xyz );

	float	Shadow = 1.0;
	if ( Depth < ShadowSplits.y )		Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjFill[0], ShadowMapFill, 0.0 );
#if SPLITS_COUNT > 1
	else if ( Depth < ShadowSplits.z )	Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjFill[1], ShadowMapFill, 1.0 );
#endif
#if SPLITS_COUNT > 2
	else if ( Depth < ShadowSplits.w )	Shadow = GetShadowFromSlice( _WorldPosition, World2LightProjFill[2], ShadowMapFill, 2.0 );
#endif
	return Shadow;
}

