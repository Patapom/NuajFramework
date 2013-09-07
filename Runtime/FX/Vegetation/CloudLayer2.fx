// Draws a cloud layer in the sky
//
#include "../Camera.fx"
#include "../Samplers.fx"

static const float	EARTH_RADIUS = 6400.0;

float		WorldUnit2Kilometer;		// World scale factor
float3		SunDirection;				// Pointing TOWARD the Sun
float3		SunColor;
float3		SkyColor;

Texture2D	PhaseMie;
Texture2D	PhaseMie_Convolved;
float		ScatteringCoeff;

Texture2D	NoiseTexture2D0;
Texture2D	NoiseTexture2D1;
Texture2D	NoiseTexture2D2;
Texture2D	NoiseTexture2D3;

float2		BufferInvSize;

// Cloud parameters
float		CloudAltitudeKm;
float2		CloudThicknessKm;			// X=Thickness Y=1/Thickness
float		CloudDensityOffset;
float		NoiseSize;
float		CloudEvolutionSpeed;
float		CloudSpeed;
float		CloudTime;
float2		FrequencyFactor;
float4		AmplitudeFactor;
float		NoiseMipBias;
float		NormalAmplitude;

float4		ScatteringFactors;
float		ScatteringSkyFactor;

#include "ShadowMapSupport.fx"


struct VS_IN
{
	float4	Position	: SV_POSITION;	// Depth-pass renderables need only declare a position
};

VS_IN VS( VS_IN _In )	{ return _In; }

// ===================================================================================
// Cloud Noise Computation

// Computes the mip level at which sample the cloud texture given the cloud and camera position
float	GetMipLevel( float3 _WorldPositionKm, float3 _CameraPosKm, float3 _SurfaceNormal )
{
	float3	ToPosition = _WorldPositionKm - _CameraPosKm;
	float	Distance2Camera = length( ToPosition );
			ToPosition /= Distance2Camera;

	float	PixelSize = (2.0 * Distance2Camera * CameraData.x * BufferInvSize.y) / abs( dot( ToPosition, _SurfaceNormal ) );
	float	TexelSize = 512.0 * NoiseSize;

	return NoiseMipBias + log2( PixelSize / TexelSize );
}

// Converts a WORLD position into a 2D cloud position
float2	World2Surface( float3 _WorldPositionKm )
{
	float2	SurfacePosition = _WorldPositionKm.xz;

	SurfacePosition.x += CloudSpeed * CloudTime;
	SurfacePosition *= NoiseSize;

	return SurfacePosition;
}

// Dynamic 2D version (4 octaves)
float	GetNoise2D( float3 _WorldPositionKm, float _MipLevel, out float3 _Normal )
{
	float2	UV = World2Surface( _WorldPositionKm );
	float	Amplitude = 1.0;

	float4	Value  = NoiseTexture2D0.SampleLevel( LinearWrap, UV, _MipLevel );
	float	Noise = Value.w;
	_Normal = 2.0 * Value.xyz - 1.0;

	Amplitude *= AmplitudeFactor.x;
	UV *= FrequencyFactor;
	UV.x += CloudEvolutionSpeed * CloudTime;
	Value = NoiseTexture2D1.SampleLevel( LinearWrap, UV, _MipLevel );
	Noise += Amplitude * Value.w;
	_Normal += Amplitude * (2.0 * Value.xyz - 1.0);

return AmplitudeFactor.y * Noise;	// 2 octaves

	Amplitude *= AmplitudeFactor.x;
	UV *= FrequencyFactor;
	UV.x += CloudEvolutionSpeed * CloudTime;
	Value = NoiseTexture2D2.SampleLevel( LinearWrap, UV, _MipLevel );
	Noise += Amplitude * Value.w;
	_Normal += Amplitude * (2.0 * Value.xyz - 1.0);

return AmplitudeFactor.z * Noise;	// 3 octaves

	Amplitude *= AmplitudeFactor.x;
	UV *= FrequencyFactor;
	UV.x += CloudEvolutionSpeed * CloudTime;
	Value = NoiseTexture2D3.SampleLevel( LinearWrap, UV, _MipLevel );
	Noise += Amplitude * Value.w;
	_Normal += Amplitude * (2.0 * Value.xyz - 1.0);

	return AmplitudeFactor.w * Noise;
}

float	ComputeCloudDensity( float3 _WorldPositionKm, float _MipLevel, out float3 _Normal )
{
//	float	N = saturate( GetNoise2D( _WorldPositionKm, _MipLevel, _Normal ) - CloudDensityOffset ) / max( 1e-2, 1.0 - CloudDensityOffset);
	float	N = saturate( GetNoise2D( _WorldPositionKm, _MipLevel, _Normal ) + CloudDensityOffset - 0.5 );
	return N*N;
}

// Computes the thickness of the cloud layer at given position
//
float	ComputeCloudThickness( float3 _WorldPositionKm, float3 _View, out float3 _CloudNormal, float _MipLevel )
{
	float	Thickness = CloudThicknessKm.x * ComputeCloudDensity( _WorldPositionKm, _MipLevel, _CloudNormal );
	_CloudNormal = normalize( float3( NormalAmplitude.xx, 1.0 ) * _CloudNormal );

	return Thickness;
}

float4	ComputeCloudLighting( float3 _WorldPositionKm, float3 _CameraPosKm, float3 _View, float3 _SphereNormal, float3 _SphereTangent, float3 _SphereBiTangent, float _MipLevel )
{
	float3	CloudNormal;
	float	H = 1000.0 * ComputeCloudThickness( _WorldPositionKm, _View, CloudNormal, _MipLevel );

	float3	Normal = CloudNormal.x * _SphereTangent + CloudNormal.z * _SphereNormal + CloudNormal.y * _SphereBiTangent;

 	float	DotV = dot( _View, Normal );
 	float	DotL = dot( SunDirection, Normal );
	float	DotVL = dot( SunDirection, _View );
	float	Hv = H / max( 0.01, DotV );
	float	Hl = H / max( 0.01, DotL );

	float	CosTheta = 0.5 * (1.0 - DotVL);
	float	Phase = PhaseMie.SampleLevel( LinearClamp, CosTheta, 0.0 ).x;
	float	Phase2 = PhaseMie_Convolved.SampleLevel( LinearClamp, CosTheta, 0.0 ).x;
//todo: merge 2 phases in 1 texture

	float	Kappa_sc = ScatteringCoeff;
	float	KappaS_sc = 0.5 * ScatteringCoeff;

	// Compute extinction
	float	Extinction = exp( -Kappa_sc * Hv );

	// Compute 0-scattering (narrow forward peak)
	float	I0 = ScatteringFactors.x * Hl * KappaS_sc * exp( -KappaS_sc * Hl ) * pow( 0.5 * Phase, KappaS_sc * Hl );	// Narrow forward scattering

	// Compute first-order scattering (analytical)
	float	ExpHv = exp( -Kappa_sc * Hv );
	float	ExpHl = exp( -Kappa_sc * Hl );
	float	ExpHVmL = (ExpHv - ExpHl) * DotL / (DotV - DotL);
	float	ExpHVpL = (1.0 - ExpHv * ExpHl) * max( 0.0, DotL / max( 0.1, DotV + DotL ) );
	float	Common = ScatteringFactors.y * Kappa_sc * Phase;
	float	I1t = Common * ExpHVmL;
	float	I1r = Common * ExpHVpL / max( 0.2, DotL );

	// Compute second-order scattering (analytical)
			Common = ScatteringFactors.z * KappaS_sc * KappaS_sc * Phase2;
	float	I2t = Common * ExpHVmL;
	float	I2r = Common * ExpHVmL;// / max( 0.01, DotL );

	// Compute multiple scattering (diffuse approximation)
	float	Tau_c = exp( -Kappa_sc * H );
			Common = H * Kappa_sc;
	float	Tms = Common * Tau_c;
	float	Rms = Common * (1.0 - Tau_c);
	float	I3t = ScatteringFactors.w * Tms * 0.07957747154594766788444188168626 * DotL / max( 0.2, DotV );
	float	I3r = ScatteringFactors.w * Rms * 0.07957747154594766788444188168626 * DotL / max( 0.2, DotV );

	// Compute shadow at position
//	float	Shadow = GetShadowAtPosition( _WorldPositionKm );
float	Shadow = 1.0;	// Disabled for now until it's bug free

	// Build final colors
	float4	ReflectionColor = float4( Shadow * SunColor * (I1r+I2r+I3r) + SkyColor * ScatteringSkyFactor * Rms, Extinction );
	float4	TransmissionColor = float4( Shadow * SunColor * (I0+I1t+I2t+I3t) + SkyColor * ScatteringSkyFactor * Tms, Extinction );

	float	Transition = smoothstep( 0.1, 0.15, dot( SunDirection, _SphereNormal ) );

	return lerp(			// Result is an interpolation of
		ReflectionColor,	// Reflection
		TransmissionColor,	// Transmission
		Transition );		// Based on the angle of the Sun with the cloud's surface
}


// ===================================================================================
// Render the extinction of light going through the cloud layer
// NOTE: The blend state is set so that we only render into R, G, B or A depending on the cloud layer
float4	PS_Shadow( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	Direction = ComputeWorldDirectionFromShadowUV( UV );

	// Compute position on cloud layer in WORLD space
	float3	CloudPositionKm = EARTH_CENTER + (EARTH_RADIUS + CloudAltitudeKm) * Direction;
	float3	CloudNormal = normalize( CloudPositionKm );

	// Compute layer thickness at position
	float	Thickness = 1000.0 * ComputeCloudThickness( CloudPositionKm, CloudNormal, CloudNormal, NoiseMipBias );
	Thickness /= max( 0.01, dot( CloudNormal, SunDirection ) );

	// Return extinction through layer
	return exp( -ScatteringCoeff * Thickness );
}

// TODO: use sharper noise

// ===================================================================================

float4	PS_Cloud( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	View = mul( float4( GetCameraView( UV ), 0 ), Camera2World ).xyz;
	float3	Pos = Camera2World[3].xyz;
//return ShadowMap.SampleLevel( LinearWrap, UV, 0.0 );

//	float4	SourceColor = SourceBuffer.SampleLevel( LinearClamp, UV, 0.0 );

	float3	CameraPosKm = WorldUnit2Kilometer * Pos + float3( 0.0, EARTH_RADIUS, 0.0 );		// Offset to Earth's surface
	float	CloudHitDistanceKm = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS + CloudAltitudeKm );
	if ( CloudHitDistanceKm < 0.0 )
		return float4( 0, 0, 0, 1 );

// 	float	EarthHitDistance = ComputeSphereIntersection( CameraPosKm, View, EARTH_CENTER, EARTH_RADIUS );
// 	if ( EarthHitDistance >= 0.0 )
// 		return float4( 0, 0, 0, 1 );

	// Compute lighting
 	float3	HitPositionCloudKm = CameraPosKm + CloudHitDistanceKm * View;
	float3	SphereNormal = normalize( HitPositionCloudKm - EARTH_CENTER );
	float3	SphereTangent = float3( 1, 0, 0 );
	float3	SphereBiTangent = normalize( cross( SphereTangent, SphereNormal ) );
			SphereTangent = cross( SphereNormal, SphereBiTangent );

// float2	ShadowUV = ProjectWorld2Shadow( HitPositionCloudKm );
// return float4( GetShadowAtAltitude( ShadowUV, 0.0 ).xxx, 0 );

	return ComputeCloudLighting( HitPositionCloudKm, CameraPosKm, View, SphereNormal, SphereTangent, SphereBiTangent, GetMipLevel( HitPositionCloudKm, CameraPosKm, SphereNormal ) );
}

// ===================================================================================
//
technique10 DrawShadow
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Shadow() ) );
	}
}

technique10 DrawCloud
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Cloud() ) );
	}
}
