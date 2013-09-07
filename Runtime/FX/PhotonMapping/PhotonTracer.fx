// This shader performs photon tracing in a scene described by a distance field
// Each photon is either absorbed or reflected based on russian roulette
// The photon is then accumulated as a tiny 1 pixel quad in the 2D slice of the 3D render target
//	at the position where it was absorbed.
//
#include "../Camera.fx"
#include "ComputeDistanceField.fx"

static const float	PHOTON_TEXTURE_SIZE = 64.0;
static const int	PHOTON_MAX_BOUNCE_COUNT = 6;
static const float	ORTHOGONALITY = 0.8;

float3		SceneBBoxMin;
float3		SceneBBoxMax;

struct VS_IN
{
	float3	Position			: POSITION;
	float3	Direction			: NORMAL;
	uint	PhotonIndex			: SV_VERTEXID;
};

struct GS_IN
{
	float3	Position			: POSITION;
	float3	Direction			: DIRECTION;
	float3	Flux				: FLUX;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float3	Direction			: DIRECTION;
	float3	Flux				: FLUX;
	uint	SliceIndex			: SV_RENDERTARGETARRAYINDEX;
};

struct PS_OUT
{
	float4	DirectionCount		: SV_TARGET0;
	float4	Flux				: SV_TARGET1;
};

float	Random( int _Seed )
{
	int n = _Seed;
	n = (n * (n * n * 75731 + 189221) + 1371312589);
	return (float) n / 2147483648.0f;
}


GS_IN	VS( VS_IN _In )
{
	// Start photon tracing
	float3	PhotonPosition = _In.Position;
	float3	PhotonDirection = _In.Direction;
	float3	PhotonFlux = 1.0;

	float3	HitPosition, HitNormal, HitAlbedo;
	for ( int BounceIndex=0; BounceIndex < PHOTON_MAX_BOUNCE_COUNT; BounceIndex++ )
	{
		if ( !ComputeIntersection( PhotonPosition, PhotonDirection, HitPosition, HitNormal, HitAlbedo ) )
		{	// The photon escaped !
			PhotonFlux = 0.0;	// This will prevent writing the photon anywhere
			break;
		}

		int		RandomSeed = 4 * (PHOTON_MAX_BOUNCE_COUNT * _In.PhotonIndex + BounceIndex);

		// No matter what, we're now at hit position
		PhotonPosition = HitPosition;

		// Modify the flux's color
		float	BounceThreshold = (HitAlbedo.x+HitAlbedo.y+HitAlbedo.z) / 3.0;
		PhotonFlux *= HitAlbedo / BounceThreshold;

		// Russian roulette : should the photon continue or stop here ?
		if ( BounceIndex == PHOTON_MAX_BOUNCE_COUNT-1 || Random( RandomSeed++ ) > BounceThreshold )
			break;	// Absorption where it hit...

		// Bounce off the surface...
		// As it's a diffuse surface, the distribution should create a cosine lobe
		float3	Tangent = normalize( cross( 1.0.xxx, HitNormal ) );
		float3	BiTangent = cross( Tangent, HitNormal );

		float	RandomY = Random( RandomSeed++ );
				RandomY = 1.0 - ORTHOGONALITY * (RandomY*RandomY);	// More chances to bounce off in orthogonal direction. The true formula here should be asin( sqrt( Random ) ) but I don't want to involve an arcsinus here !
		float3	BounceDirection = normalize( float3( 2.0 * ORTHOGONALITY * (Random( RandomSeed++ ) - 0.5), RandomY, 2.0 * ORTHOGONALITY * (Random( RandomSeed++ ) - 0.5) ) );
		PhotonDirection = BounceDirection.x * Tangent + BounceDirection.y * HitNormal + BounceDirection.z * BiTangent;

		// + a little nudge off the surface to avoid acnea
		PhotonPosition += PhotonDirection * 1e-2 / BounceDirection.y;
	}

	// Write out photon
	GS_IN	Out;
	Out.Position = PhotonPosition;
	Out.Direction = PhotonDirection;
	Out.Flux = PhotonFlux;
	return	Out;
}

[maxvertexcount( 4 )]
void	GS( point GS_IN _Photon[1], inout TriangleStream<PS_IN> _Stream )
{
	float3	Flux = _Photon[0].Flux;
	if ( dot(Flux,Flux) < 1e-3 )
		return;	// Don't write anything...

	// Transform photon position into projected 2D + slice index
	float3	SceneBBoxSize = SceneBBoxMax - SceneBBoxMin;
	float3	NormalizedPosition = (_Photon[0].Position - SceneBBoxMin) / SceneBBoxSize;
	uint	SliceIndex = uint( floor( NormalizedPosition.z * (PHOTON_TEXTURE_SIZE-1) ) );
	float4	ProjPosition = float4( 2.0 * NormalizedPosition.x - 1.0, 1.0 - 2.0 * NormalizedPosition.y, 0.0, 1.0 );
	float	DeltaPosition = 1.0 / PHOTON_TEXTURE_SIZE;

	// Generate a single pixel quad that will write the photon
	PS_IN	Out;
	Out.Direction = _Photon[0].Direction;
	Out.Flux = Flux;
	Out.SliceIndex = SliceIndex;

	Out.__Position = ProjPosition + DeltaPosition * float4( -1.0, -1.0, 0.0, 0.0 );
	_Stream.Append( Out );

	Out.__Position = ProjPosition + DeltaPosition * float4( -1.0, +1.0, 0.0, 0.0 );
	_Stream.Append( Out );

	Out.__Position = ProjPosition + DeltaPosition * float4( +1.0, -1.0, 0.0, 0.0 );
	_Stream.Append( Out );

	Out.__Position = ProjPosition + DeltaPosition * float4( +1.0, +1.0, 0.0, 0.0 );
	_Stream.Append( Out );
}

PS_OUT PS( PS_IN _In )
{
	PS_OUT	Out;
	Out.DirectionCount = float4( _In.Direction, 1.0 );	// One more photon in that direction...
	Out.Flux = float4( _In.Flux, 0.0 );					// Accumulate flux
	return Out;
}

technique10 RayTracer
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
