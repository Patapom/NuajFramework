// This shader performs the actual display that uses the photon maps
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "ComputeDistanceField.fx"

static const int	RAYS_COUNT = 150;
static const float	ORTHOGONALITY = 0.8;

float3		SceneBBoxMin;
float3		SceneBBoxMax;
float4		dUVW;
Texture3D	TexPhotonDirections;
Texture3D	TexPhotonFlux;
Texture3D	TexDistanceField;

float3		BoxCenter;
float3		BoxHalfSize;
float3		BoxRotationAxis;

struct VS_IN
{
	float3	Position			: POSITION;
	float3	Normal				: NORMAL;
	float3	Tangent				: TANGENT;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float3	Position			: POSITION;
	float3	Normal				: NORMAL;
	float3	Tangent				: TANGENT;
};

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = RotateY( BoxCenter + BoxHalfSize * _In.Position, BoxRotationAxis );
	Out.Normal = RotateY( _In.Normal, BoxRotationAxis );
	Out.Tangent = RotateY( _In.Tangent, BoxRotationAxis );
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );

	return Out;
}

float	Random( int _Seed )
{
	int n = _Seed;
	n = (n * (n * n * 75731 + 189221) + 1371312589);
	return (float) n / 2147483648.0f;
}


float3	EvaluatePhotons( float3 _Position, float3 _Normal )
{
	// Normalize position in photon map space
	float3	UVW = (_Position - SceneBBoxMin) / (SceneBBoxMax - SceneBBoxMin);
// 	return UVW;

	// Sample direction distance field at that position
	float4	DirectionDistance = TexDistanceField.SampleLevel( LinearClamp, UVW, 0 );

	// Sample photon map at indicated position
	float3	PhotonUVW = UVW + DirectionDistance.xyz * dUVW.xyz;
//	return PhotonUVW;

	float4	PhotonDirectionCount = TexPhotonDirections.SampleLevel( LinearClamp, PhotonUVW, 1 );
	float4	PhotonFlux = TexPhotonFlux.SampleLevel( LinearClamp, PhotonUVW, 1 );
//	return 1.0 * PhotonDirectionCount.w;

//	return PhotonFlux.xyz / max( 1e-3, PhotonDirectionCount.w );

	// Compute photon flux & direction
	float	Normalizer = 1.0 / max( 1e-3, PhotonDirectionCount.w );
	float3	Flux = PhotonFlux.xyz * Normalizer;
	float3	Direction = normalize( -PhotonDirectionCount.xyz );

	// Apply diffuse lighting
	return Flux * saturate( dot( Direction, _Normal ) );
}

float3	FinalGather( float3 _Position, float3 _Normal, float3 _Tangent )
{
	float3	BiTangent = cross( _Normal, _Tangent );

	float3	Result = 0.0;
	float	SumWeights = 0.0;

	float3	HitPosition, HitNormal, HitAlbedo;
	for ( int RayIndex=0; RayIndex < RAYS_COUNT; RayIndex++ )
	{
		// Draw a random direction
		int		RandomSeed = 3 * RayIndex;
//		int		RandomSeed = 3 * (RayIndex + int( _Position.x * 137.0 + _Position.y * 28.9 + _Position.z * 978.0));

		float	RandomY = Random( RandomSeed++ );
				RandomY = 1.0 - ORTHOGONALITY * (RandomY*RandomY);	// More chances to bounce off in orthogonal direction. The true formula here should be asin( sqrt( Random ) ) but I don't want to involve an arcsinus here !
		float3	ShootDirection = normalize( float3( 2.0 * ORTHOGONALITY * (Random( RandomSeed++ ) - 0.5), RandomY, 2.0 * ORTHOGONALITY * (Random( RandomSeed++ ) - 0.5) ) );

		// Transform into world space
		float3	Direction = ShootDirection.x * _Tangent + ShootDirection.y * _Normal + ShootDirection.z * BiTangent;

		// Offset a little to avoid direct contact
		float3	Position = _Position + Direction * 1e-2 / ShootDirection.y;

		// Shoot ray
		if ( !ComputeIntersection( Position, Direction, HitPosition, HitNormal, HitAlbedo ) )
			continue;

		// Accumulate photons flux at hit position
		float	DotDiffuse = dot( Direction, _Normal );
		Result += DotDiffuse * EvaluatePhotons( HitPosition, HitNormal );
		SumWeights += 1.0;
	}

	return Result / SumWeights;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
//	return float4( _In.Tangent, 0.0 );
	return float4( FinalGather( _In.Position, _In.Normal, _In.Tangent ), 0.0 );
}

technique10 DisplayScene
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
