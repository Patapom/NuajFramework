// Draws the sky layer between cloud layers
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../Atmosphere/SkySupport.fx"
#include "../ReadableZBufferSupport.fx"

#include "ShadowMapSupport.fx"

static const float3	LUMINANCE = float3( 0.2126, 0.7152, 0.0722 );	// RGB => Y (taken from http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering#Light_Adaptation)

float2		BufferInvSize;

int			SkyStepsCount = 64;
int			SkyAboveStepsCount = 8;
Texture2D	CloudLayerTexture0;
Texture2D	CloudLayerTexture1;
Texture2D	CloudLayerTexture2;
Texture2D	CloudLayerTexture3;
Texture2D	CloudLayerTexture4;


float4		DEBUG;

struct VS_IN
{
	float4	Position	: SV_POSITION;	// Depth-pass renderables need only declare a position
};

VS_IN VS( VS_IN _In )	{ return _In; }

// ===================================================================================

// Actual sky color computation
void	ComputeSkyColorGodRays( float3 _PositionKm, float3 _View, float3 _EarthNormal, float _DistanceStartKm, float _DistanceEndKm, float2 _ShadowUVStart, float2 _ShadowUVEnd, float _EarthShadowDistanceKmMin, float _EarthShadowDistanceKmMax, inout float3 _Scattering, inout float3 _Extinction, const int _StepsCount=8 )
{
	// Update position
	float3	PositionStartKm = _PositionKm + _DistanceStartKm * _View;
	float3	PositionEndKm = _PositionKm + _DistanceEndKm * _View;

	float	AltitudeStartKm = dot( PositionStartKm - EARTH_CENTER, _EarthNormal );
	float	AltitudeEndKm = dot( PositionEndKm - EARTH_CENTER, _EarthNormal );

	// Compute phases
	float	CosTheta = dot( _View, SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	PhaseMie = 1.0 / (1.0 + MiePhaseAnisotropy * CosTheta);
			PhaseMie = (1.0 - MiePhaseAnisotropy*MiePhaseAnisotropy) * PhaseMie * PhaseMie;

	// Ray-march the view ray
	float	InvStepsCount = 1.0 / _StepsCount;

	float	DistanceKm = _DistanceEndKm - _DistanceStartKm;
	float	StepSizeKm = DistanceKm * InvStepsCount;
	float2	StepShadowUV = (_ShadowUVEnd - _ShadowUVStart) * InvStepsCount;
	float	StepAltitudeKm = (AltitudeEndKm - AltitudeStartKm) * InvStepsCount;

	float	CurrentAltitudeKm = AltitudeEndKm;// - 0.5 * StepAltitudeKm;	// Start at half a step
	float	MarchedDistanceKm = _DistanceEndKm;// - 0.5 * StepSizeKm;
	float2	ShadowUV = _ShadowUVEnd;// - 0.5 * StepShadowUV;

	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float4	OpticalDepth = ComputeOpticalDepth( CurrentAltitudeKm, SunDirection, _EarthNormal );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth.x;
		float	Rho_aerosols = OpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere
		float3	SunExtinction = exp( -Sigma_Rayleigh * OpticalDepth.z - Sigma_Mie * OpticalDepth.w );
		float3	Light = SunIntensity * SunExtinction;// * step( MarchedDistanceKm, _EarthShadowDistanceKmMin ) * step( _EarthShadowDistanceKmMax, MarchedDistanceKm );

// TODO: re-enable Earth shadow !

		// Sample shadow map
Light *= GetShadowAtPosition( PositionStartKm + (MarchedDistanceKm-_DistanceStartKm) * _View, AltitudeEndKm, SunDirection );
//ShadowUV = ProjectWorld2Shadow( PositionStartKm + (MarchedDistanceKm-_DistanceStartKm) * _View, AltitudeEndKm, SunDirection );
//Light *= GetShadowAtAltitude( ShadowUV, CurrentAltitudeKm - EARTH_RADIUS );

// TODO: Angular interpolation of shadow UVs here !

		// =============================================
		// Compute in-scattered light
		float3	ScatteringRayleigh = Rho_air * DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4 * PhaseRayleigh;
		float	ScatteringMie = Rho_aerosols * DensitySeaLevel_Mie * PhaseMie;
		float3	InScatteringRayleigh = Light * ScatteringRayleigh;
		float3	InScatteringMie = Light * ScatteringMie;

		// =============================================
		// Accumulate extinction along view
		float3	StepExtinction = exp( -(Sigma_Rayleigh * Rho_air + Sigma_Mie * Rho_aerosols) * StepSizeKm );
		_Extinction *= StepExtinction;

		// =============================================
		// Accumulate in-scattered light
		_Scattering *= StepExtinction;
		_Scattering += (InScatteringRayleigh + InScatteringMie) * StepSizeKm;

		// March
		CurrentAltitudeKm -= StepAltitudeKm;
		MarchedDistanceKm -= StepSizeKm;
		ShadowUV -= StepShadowUV;
	}
}

// Trace a single slice of sky
void	TraceSkySlice( float3 _PositionKm, float3 _View, float3 _EarthNormal, inout float _SliceStartKm, inout float _SliceEndKm, float _CloudLayerDistanceKm, float _MinDistanceKm, float _MaxDistanceKm, float2 _ShadowUVStart, float2 _ShadowUVEnd, float _EarthShadowDistanceKmMin, float _EarthShadowDistanceKmMax, Texture2D _CloudLayer, float2 _UV, float _InvStepLengthKm, inout float3 _Scattering, inout float3 _Extinction )
{
	_SliceEndKm = _SliceStartKm;
	_SliceStartKm = max( _MinDistanceKm, min( _MaxDistanceKm, _CloudLayerDistanceKm ) );
	if ( _SliceStartKm + 1e-2 >= _SliceEndKm )
		return;	// Empty slice...

	float	SliceSizeKm = _SliceEndKm - _SliceStartKm;
	int		SliceStepsCount = min( SkyStepsCount, ceil( SliceSizeKm * _InvStepLengthKm ) );
	if ( SliceStepsCount <= 0 )
		return;	// Shouldn't happen since slice isn't supposed to be empty...
//int	SliceStepsCount = _InvStepLengthKm;

	// Sample next cloud layer
	float	CloudExtinction = _CloudLayer.SampleLevel( LinearClamp, _UV, 0.0 ).w;
// 	if ( CloudExtinction < 1e-3 )
// 	{	// No need to trace that slice... The cloud is too opaque anyway !
// 		_Scattering *= CloudExtinction;
// 		_Extinction *= CloudExtinction;
// 		return;
// 	}

	// Compute sky light
	ComputeSkyColorGodRays( _PositionKm, _View, _EarthNormal, _SliceStartKm, _SliceEndKm, _ShadowUVStart, _ShadowUVEnd, _EarthShadowDistanceKmMin, _EarthShadowDistanceKmMax, _Scattering, _Extinction, SliceStepsCount );

	// Apply cloud exinction 
	_Scattering *= CloudExtinction;
	_Extinction *= CloudExtinction;
}

float4	PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	View = GetCameraViewUnNormalized( UV );
	float	ViewLength = length( View );
			View /= ViewLength;
			View = mul( float4( View, 0 ), Camera2World ).xyz;
	float3	Pos = Camera2World[3].xyz;

	// Compute view ray intersection with the upper atmosphere
	float3	CameraPosKm = WorldUnit2Kilometer * Pos + float3( 0.0, EARTH_RADIUS, 0.0 );		// Offset to Earth's surface
	float	AtmosphereHitDistanceKm = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, ATMOSPHERE_RADIUS );
 	if ( AtmosphereHitDistanceKm < 0.0 )
 		return float4( 0, 0, 0, 1 );	// No hit anyway...


	float	CameraPosAltitudeKm = length( CameraPosKm - EARTH_CENTER );
	bool	bOutsideAtmosphere = CameraPosAltitudeKm > ATMOSPHERE_RADIUS;
	float	MinDistanceKm = bOutsideAtmosphere ? AtmosphereHitDistanceKm : 0.0;

	// Read back depth
	float	Z = ReadDepth( _In.Position.xy );
	float	ZBufferDistanceKm = Z * ViewLength * WorldUnit2Kilometer;

float	EarthHitDistanceKm = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS );
if ( EarthHitDistanceKm > 0.0 )
	ZBufferDistanceKm = EarthHitDistanceKm;

	float	MaxDistanceKm = lerp( ZBufferDistanceKm, AtmosphereHitDistanceKm, step( 0.99 * CameraData.w, Z ) );	// If Z == ZFar then start from top of the atmosphere


	// Compute potential intersection with earth's shadow
	float	Terminator = 1.0;
	float	EarthShadowDistanceMin = 1e6, EarthShadowDistanceMax = -1e6;
	float3	ToPositionKm = CameraPosKm  - EARTH_CENTER;
	if ( dot( ToPositionKm, SunDirection ) <= 0.0 )
	{	// Project current position in the 2D plane normal to the light to test the intersection with the shadow cylinder cast by the Earth
		float3	X = normalize( cross( SunDirection, View ) );
		float3	Y = cross( X, SunDirection );
		float2	P = float2( dot( ToPositionKm, X ), dot( ToPositionKm, Y ) );	// 2D Position on the plane
		float2	V = float2( dot( View, X ), dot( View, Y ) );					// 2D View on the plane
		float	a = dot( V, V );
		float	b = dot( P, V );
		float	c = dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
		float	Delta = b*b - a*c;
		if ( Delta >= 0.0 )
		{
			Delta = sqrt(Delta);
			a = 1.0 / a;

			EarthShadowDistanceMin = max( 0.0, (-b-Delta) * a );
			EarthShadowDistanceMax = max( 0.0, (-b+Delta) * a );
			Terminator = 1.0 - saturate( EarthShadowDistanceMax / AtmosphereHitDistanceKm );
		}
	}

	float3	EarthNormal = float3( 0.0, 1.0, 0.0 );

	// Now we have to split sky rendering depending on the distances to each cloud layer
	float	ClouLayerDistanceKm[4];
	ClouLayerDistanceKm[0] = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS + ShadowAltitudesMinKm.x );
	ClouLayerDistanceKm[1] = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS + ShadowAltitudesMinKm.y );
	ClouLayerDistanceKm[2] = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS + ShadowAltitudesMinKm.z );
	ClouLayerDistanceKm[3] = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS + ShadowAltitudesMinKm.w );

	// Compute shadow UVs for each position
	float2	ShadowUVs[6];
	ShadowUVs[0] = ProjectWorld2Shadow( CameraPosKm + MaxDistanceKm * View, ATMOSPHERE_RADIUS, SunDirection );
	ShadowUVs[1] = ProjectWorld2Shadow( CameraPosKm + max( 0.0, ClouLayerDistanceKm[0]) * View, EARTH_RADIUS + ShadowAltitudesMinKm.x, SunDirection );
	ShadowUVs[2] = ProjectWorld2Shadow( CameraPosKm + max( 0.0, ClouLayerDistanceKm[1]) * View, EARTH_RADIUS + ShadowAltitudesMinKm.y, SunDirection );
	ShadowUVs[3] = ProjectWorld2Shadow( CameraPosKm + max( 0.0, ClouLayerDistanceKm[2]) * View, EARTH_RADIUS + ShadowAltitudesMinKm.z, SunDirection );
	ShadowUVs[4] = ProjectWorld2Shadow( CameraPosKm + max( 0.0, ClouLayerDistanceKm[3]) * View, EARTH_RADIUS + ShadowAltitudesMinKm.w, SunDirection );
	ShadowUVs[5] = ProjectWorld2Shadow( CameraPosKm + MinDistanceKm * View, CameraPosAltitudeKm, SunDirection );

	// Trace...
	float3	Extinction = 1.0;
	float3	Scattering = 0.0;

	float	SliceStartKm = MaxDistanceKm;			// Start from very far
	float	SliceEndKm;
	float	InvStepLengthLastSliceKm = SkyAboveStepsCount / (MaxDistanceKm - ClouLayerDistanceKm[0]);	// For the last slice above the highest cloud layer, we use less steps as there are no godrays anyway
	float	InvStepLengthKm = SkyStepsCount / (ClouLayerDistanceKm[0] - MinDistanceKm);					// We divide steps between camera and highest layer as this is where godrays need the most resolution

TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[0], MinDistanceKm, MaxDistanceKm, ShadowUVs[0], ShadowUVs[1], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture0, UV, InvStepLengthLastSliceKm, Scattering, Extinction );
TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[1], MinDistanceKm, MaxDistanceKm, ShadowUVs[1], ShadowUVs[2], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture1, UV, InvStepLengthKm, Scattering, Extinction );

// if ( DEBUG.x > 0.0 )
// {
// 	// 1 of 64
// //	ComputeSkyColorGodRays( CameraPosKm, View, EarthNormal, 0.0, MaxDistanceKm, 0, 0, 0, 0, Scattering, Extinction, 64 );
// 	TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, 0.0, MinDistanceKm, MaxDistanceKm, 0, 0, 0, 0, CloudLayerTexture0, UV, 64, Scattering, Extinction );
// }
// else
// {
// 	// 2 of 32
// // 	ComputeSkyColorGodRays( CameraPosKm, View, EarthNormal, 0.5 * MaxDistanceKm, MaxDistanceKm, 0, 0, 0, 0, Scattering, Extinction, 32 );
// // 	ComputeSkyColorGodRays( CameraPosKm, View, EarthNormal, 0.0, 0.5 * MaxDistanceKm, 0, 0, 0, 0, Scattering, Extinction, 32 );
// 	TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, 0.5 * MaxDistanceKm, MinDistanceKm, MaxDistanceKm, 0, 0, 0, 0, CloudLayerTexture0, UV, 32, Scattering, Extinction );
// 	TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, 0.0, MinDistanceKm, MaxDistanceKm, 0, 0, 0, 0, CloudLayerTexture0, UV, 32, Scattering, Extinction );
// }

return float4( Scattering, dot(Extinction, LUMINANCE) );



//return ClouLayerDistanceKm[0];
//SliceStartKm = ClouLayerDistanceKm[0];
//TraceSkySlice( CameraPosKm, View, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[0], MinDistanceKm, MaxDistanceKm, EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture0, UV, InvStepLengthKm, Scattering, Extinction );
//TraceSkySlice( CameraPosKm, View, SliceStartKm, SliceEndKm, MinDistanceKm, MinDistanceKm, MaxDistanceKm, EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture0, UV, InvStepLengthKm, Scattering, Extinction );
//return float4( Scattering, dot(Extinction, LUMINANCE) );

	if ( View.y >= 0.0 )
	{	// View up => Render from top to bottom
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[0], MinDistanceKm, MaxDistanceKm, ShadowUVs[0], ShadowUVs[1], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture0, UV, InvStepLengthLastSliceKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[1], MinDistanceKm, MaxDistanceKm, ShadowUVs[1], ShadowUVs[2], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture1, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[2], MinDistanceKm, MaxDistanceKm, ShadowUVs[2], ShadowUVs[3], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture2, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[3], MinDistanceKm, MaxDistanceKm, ShadowUVs[3], ShadowUVs[4], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture3, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, MinDistanceKm,			 MinDistanceKm, MaxDistanceKm, ShadowUVs[4], ShadowUVs[5], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture4, UV, InvStepLengthKm, Scattering, Extinction );	// Last slice down to camera
	}
	else
	{	// View down => Render from bottom to top
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[3], MinDistanceKm, MaxDistanceKm, ShadowUVs[3], ShadowUVs[4], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture3, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[2], MinDistanceKm, MaxDistanceKm, ShadowUVs[2], ShadowUVs[3], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture2, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[1], MinDistanceKm, MaxDistanceKm, ShadowUVs[1], ShadowUVs[2], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture1, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, ClouLayerDistanceKm[0], MinDistanceKm, MaxDistanceKm, ShadowUVs[0], ShadowUVs[1], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture0, UV, InvStepLengthKm, Scattering, Extinction );
		TraceSkySlice( CameraPosKm, View, EarthNormal, SliceStartKm, SliceEndKm, MinDistanceKm,			 MinDistanceKm, MaxDistanceKm, ShadowUVs[0], ShadowUVs[5], EarthShadowDistanceMin, EarthShadowDistanceMax, CloudLayerTexture4, UV, InvStepLengthKm, Scattering, Extinction );	// Last slice up to camera
	}

	return float4( Scattering, dot(Extinction, LUMINANCE) );
}

// ===================================================================================
//
technique10 DrawSky
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
