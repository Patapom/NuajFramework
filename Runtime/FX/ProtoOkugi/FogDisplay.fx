// This shader displays fog
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"
#include "3DNoise.fx"

static const float	FOG_MAP_SIZE = 100.0;

float2		BufferInvSize;
Texture2D	FogDensity;

float		Time;

float3		LightDirection;
float		LightIntensity;
float		AmbientIntensity;

float		NoiseOffset;
float		NoiseSize;
float		NoiseScrollSpeed;

int			StepsCount = 32;
float		ExtinctionCrossSection = 0.5;
float		ScatteringRatio = 0.5;
float		ScatteringAnisotropy = 0.0;


struct VS_IN
{
	float4	Position		: SV_POSITION;
};


VS_IN	VS( VS_IN _In ) { return _In; }

// Computes the intersection of the view ray with the fog volume, which is a flat box
float	FogAltitudeTop;
float	FogAltitudeBottom;

bool	ComputeFogVolumeIntersection( float3 _Position, float3 _View, float _DistanceObstacle, out float _DistanceIn, out float _DistanceOut )
{
	float	HitTop = (FogAltitudeTop-_Position.y) / _View.y;
	float	HitBottom = (FogAltitudeBottom-_Position.y) / _View.y;

	// Here we assume the camera will always be over or inside the fog
	_DistanceIn = max( 0.0, min( _DistanceObstacle, HitTop ) );
	_DistanceOut = min( _DistanceObstacle, HitBottom );

	return _DistanceIn < _DistanceOut;
}

// Converts a world position into a density map UV
float2	World2DensityMap( float3 _WorldPosition )
{
	return float2( 0.5 * (1.0+_WorldPosition.x / FOG_MAP_SIZE), 0.5 * (1.0-_WorldPosition.z / FOG_MAP_SIZE) );
}

// Bevels the noise values so the clouds completely disappear at top and bottom
float	Bevel( float3 _WorldPosition )
{
	// Compute a normalized height that is 0 at the center of the fog layer, and 1 at the top/bottom
	float	NormalizedHeight = 1.0 - abs( 2.0 * (FogAltitudeTop - _WorldPosition.y) / (FogAltitudeTop - FogAltitudeBottom) - 1.0 );
	float	Bevel = smoothstep( 0.0, 1.0, NormalizedHeight );
//	return pow( Bevel, 0.1 );

	Bevel = 1.0 - Bevel;
	Bevel *= Bevel;
	Bevel *= Bevel;
	Bevel *= Bevel;
	Bevel *= Bevel;
	return 1.0 - Bevel;
}

// Gets the fog density at the specified world position
float	GetFogDensityAndLight( float3 _WorldPosition, float3 _View, float _ViewPhase, out float3 _Light )
{
	// Sample density map
	float4	DensityColor = FogDensity.SampleLevel( LinearClamp, World2DensityMap( _WorldPosition ), 0 );
	float	DensityMap = 1.0 - DensityColor.x;

	// Scale fog vertically based on density map (so the fog smoothly gets higher further away from density zones)
	float3	Grad;
	float	PipoHeight = 0.8 + 0.5 * (Noise( 0.002 * (_WorldPosition-4.0*Time), NoiseTexture3, Grad ) - 0.2);
	float	FogAlteredTop = lerp( FogAltitudeBottom, FogAltitudeTop, smoothstep( 0.0, 1.0, PipoHeight * DensityMap ) );

	DensityMap = saturate( 0.4 * (FogAlteredTop - _WorldPosition.y) ) * DensityMap;

	// Sample noise & compute final density
	float3	Grad0, Grad1, Grad2;
	float3	UVW = 0.01 * NoiseSize * _WorldPosition + NoiseScrollSpeed * Time;
	float	N  = Noise( UVW, NoiseTexture0, Grad0 );		UVW = UVW * 2.0 + NoiseScrollSpeed * Time;
			N += 0.5 * Noise( UVW, NoiseTexture1, Grad1 );	UVW = UVW * 2.0 + NoiseScrollSpeed * Time;
			N += 0.25 * Noise( UVW, NoiseTexture2, Grad2 );
	N += NoiseOffset;
	N *= Bevel( _WorldPosition );

	float	Density = saturate( DensityMap * N );

	// Compute incoming light
	_Light = DensityColor.yzw;	// Too bad we don't have the direction for that one !

	float	FogHeight = FogAltitudeTop - _WorldPosition.y;

	// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick's equivalent to Henyey-Greenstein
// 	float	CosTheta = dot( 0.5*Grad0, _View );
//	float	Phase = saturate( 0.75 * (2.0 - CosTheta*CosTheta) );
//	float	CosTheta = saturate( dot( 0.25*Grad1, LightDirection ) );
// 	float3	H = normalize( 0.25*(Grad0+Grad1+Grad2) + _View );
// 	float	CosTheta = dot( H, LightDirection );
//	float	CosTheta = dot( normalize( 0.1*(Grad0+Grad1+Grad2) + _View ), LightDirection );
	float	CosTheta = dot( normalize( 10.0*(Grad0+Grad1+Grad2) ), _View );

	float	Den = 1.0 / (1.0 + ScatteringAnisotropy * CosTheta);
	float	Phase = (1.0 - ScatteringAnisotropy*ScatteringAnisotropy) * Den * Den;

	_Light += exp( -Density * ExtinctionCrossSection * FogHeight / LightDirection.y ) * LightIntensity * Phase + AmbientIntensity.xxx;

	return Density;
}

// ===================================================================================
float4	PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;

	// Compute view data
	float3	CameraView = float3( CameraData.y * CameraData.x * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0 * UV.y), 1.0 );
	float	CameraViewLength = length( CameraView );
			CameraView /= CameraViewLength;
	float	Z = ReadDepth( _In.Position.xy * BufferInvSize / ZBufferInvSize );
	float	Distance2Pixel = CameraViewLength * Z;
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPosition = Camera2World[3].xyz;


	// Compute phase for in-scattering
	float	CosPhase = dot( View, LightDirection );
	float	ViewPhase = 0.75 * (1.0 + CosPhase*CosPhase);


	// Compute intersection with the fog volume
	float	HitIn, HitOut;
	if ( !ComputeFogVolumeIntersection( CameraPosition, View, Distance2Pixel, HitIn, HitOut ) )
		return 0.0;	// No hit...

	// Perform ray-marching
	float	Transparency = 1.0;
	float3	Energy = 0.0;

	int		ActualStepsCount = StepsCount * saturate( 4.0 * (FogAltitudeTop - FogAltitudeBottom) / (HitOut - HitIn) );

	float	StepSize = min( 2.0, (HitOut - HitIn) / (ActualStepsCount+1) );
	float3	Step = View * StepSize;
	float3	CurrentPosition = CameraPosition + HitIn * View + 0.0 * Step;

	for ( int StepIndex=0; StepIndex < ActualStepsCount; StepIndex++ )
	{
		// Compute fog density at position
		float3	Light;
		float	Density = GetFogDensityAndLight( CurrentPosition, View, ViewPhase, Light );

		float	Sigma_t = Density * ExtinctionCrossSection;
		float	Sigma_s = Sigma_t * ScatteringRatio;

		// Compute in-scattering
		Energy += Sigma_s * StepSize * Light * Transparency;

		// Compute extinction
		float	Extinction = exp( -Sigma_t * StepSize );
		Transparency *= Extinction;

		// March
		CurrentPosition += Step;
	}

	return float4( Energy, 1.0-Transparency );
}

// ===================================================================================
// Combination of in-scattering and extinction buffers with the source buffer
Texture2D	FogBuffer;
float3		FogBufferInvSize;
Texture2D	VolumeTextureInScattering;
Texture2D	VolumeTextureExtinction;

float		BilateralThreshold = 10.0;

float4	PS_Combine( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float	Z = ReadDepth( _In.Position.xy );

const float	ZThreshold = 0.5;

	// Sample neighbors and filter with Z
	float4	Value = 0.0;
	float	SumWeight = 0.0;
	float	ZNeighbor = ReadDepth( _In.Position.xy - 0.5*FogBufferInvSize.xz );
	float	Dz = abs(ZNeighbor-Z);
	if ( Dz < ZThreshold )
	{	// Use left sample
		Value += FogBuffer.SampleLevel( LinearClamp, UV - 0.5*FogBufferInvSize.xz, 0 );
		SumWeight += 1.0 - Dz / ZThreshold;
	}
	ZNeighbor = ReadDepth( _In.Position.xy + 0.5*FogBufferInvSize.xz );
	Dz = abs(ZNeighbor-Z);
	if ( Dz < ZThreshold )
	{	// Use right sample
		Value += FogBuffer.SampleLevel( LinearClamp, UV + 0.5*FogBufferInvSize.xz, 0 );
		SumWeight += 1.0 - Dz / ZThreshold;
	}

	ZNeighbor = ReadDepth( _In.Position.xy - 0.5*FogBufferInvSize.zy );
	Dz = abs(ZNeighbor-Z);
	if ( Dz < ZThreshold )
	{	// Use top sample
		Value += FogBuffer.SampleLevel( LinearClamp, UV - 0.5*FogBufferInvSize.zy, 0 );
		SumWeight += 1.0 - Dz / ZThreshold;
	}
	ZNeighbor = ReadDepth( _In.Position.xy + 0.5*FogBufferInvSize.zy );
	Dz = abs(ZNeighbor-Z);
	if ( Dz < ZThreshold )
	{	// Use bottom sample
		Value += FogBuffer.SampleLevel( LinearClamp, UV + 0.5*FogBufferInvSize.zy, 0 );
		SumWeight += 1.0 - Dz / ZThreshold;
	}

// 	float2	CenterUV = floor(_In.Position.xy / FogBufferInvSize.xy) * FogBufferInvSize.xy;
// 	float	ZCenter = ReadDepth( CenterUV );
// 	Dz = abs(ZCenter-Z);
// 	if ( SumWeight == 0.0 || Dz < ZThreshold )
// 	{
// 		Value += FogBuffer.SampleLevel( LinearClamp, CenterUV, 0 );
// 		SumWeight += 1.1 - Dz / ZThreshold;
// 	}

	Value += FogBuffer.SampleLevel( LinearClamp, UV, 0 );
	SumWeight++;

	return Value / SumWeight;
//	return SourceColor;
/*
	
	// Perform an average of extinction & in-scattering values based on differences in Z : samples that are closest to actual depth will be used
	float2	dUVs[] =
	{
		-FogBufferInvSize.xz,
		+FogBufferInvSize.xz,
		-FogBufferInvSize.zy,
		+FogBufferInvSize.zy,
	
		-FogBufferInvSize.xy,
		+FogBufferInvSize.xy,
		float2( FogBufferInvSize.x, -FogBufferInvSize.y ),
		float2( -FogBufferInvSize.x, FogBufferInvSize.y ),
	};
	
	float3	InScattering = 0.0;
	float4	ExtinctionZ = 0.0;
	float	SumWeights = 0.0;
	for ( int SampleIndex=0; SampleIndex < 8; SampleIndex++ )
	{
		float4	ExtinctionZSample = VolumeTextureExtinction.SampleLevel( LinearClamp, UV + dUVs[SampleIndex], 0 );
		if ( abs( ExtinctionZSample.w - Z ) > BilateralThreshold )
			continue;
	
		float3	InScatteringSample = VolumeTextureInScattering.SampleLevel( LinearClamp, UV + dUVs[SampleIndex], 0 ).xyz;
	
		InScattering += InScatteringSample;
		ExtinctionZ += ExtinctionZSample;
		SumWeights++;
	}
	
	// Sample center
	float3	CenterInScattering = VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 ).xyz;
	float4	CenterExtinctionZ = VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );
	if ( SumWeights == 0.0 || abs( CenterExtinctionZ.w - Z ) < BilateralThreshold )
	{
		InScattering += CenterInScattering;
		ExtinctionZ += CenterExtinctionZ;
		SumWeights++;
	}
	
	InScattering /= SumWeights;
	ExtinctionZ /= SumWeights;
	
	// =============================================
	// Display the Sun
	float3	CameraView = normalize( float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * BufferInvSize.y), 1.0 ) );
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	
	// 	149.5978875 = Distance to the Sun in millions of kilometers
	// 	0.695       = Radius of the Sun in millions of kilometers
	float	CosAngle = 0.998;	// Cos( SunCoverAngle ) but arbitrary instead of physical computation, otherwise the Sun is really too small
	float	DotSun = dot( View, SunDirection );
	
	float	Infinity = CameraData.w-5.0;
	float3	SunExtinction = ExtinctionZ.xyz * step( Infinity, ExtinctionZ.w ) * smoothstep( CosAngle, 1.0, DotSun );
	
	// =============================================
	// Small radial blur for glare effect
// 	float4	SunPositionProj = mul( float4( SunDirection, 0.0 ), World2Proj );
// 	SunPositionProj /= SunPositionProj.w;
//	float2	Center = float2( 0.5 * (1.0 + SunPositionProj.x), 0.5 * (1.0 - SunPositionProj.y) );
// 
// 	float2	Direction = UV - Center;
// 	float	Distance2Center = length( Direction );
// 			Direction /= Distance2Center;
// 
// 	// Accumulate samples
// 	float3	SumExtinction = 0.0;
// 	for ( int i=0; i < 16; i++ )
// 	{
// 		float	SampleDistance = 0.1 * (i+0.5) / 16.0;
// 		float4	ExtinctionZ = VolumeTextureExtinction.SampleLevel( LinearClamp, Center + Direction * SampleDistance, 0 );
// 		SumExtinction += ExtinctionZ.xyz * step( Infinity, ExtinctionZ.w );
// 	}
// 	SumExtinction *= 1.0 / max( 1.0, 16.0 * 8.0 * Distance2Center );
	
	return float4( SunExtinction * SunIntensity + InScattering + ExtinctionZ.xyz * SourceColor.xyz, SourceColor.w );*/
}


// ===================================================================================
technique10 RenderFog
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 Combine
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Combine() ) );
	}
}
