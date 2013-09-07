// This shader generates the distance field to access the photon map.
// The first technique initializes the distance field from the photon map by assigning a nil distance if a photon is present or infinity otherwise
// The second technique propagates the distance field by using closest neighbor distances
//
#include "../Samplers.fx"

static const float	INFINITY = 1e6;	// Far away...
static const float	SQRT2 = 1.4142135623730950488016887242097;
static const float	SQRT3 = 1.7320508075688772935274463415059;

Texture3D	PhotonMap;
Texture3D	PreviousDistanceField;
float4		dUVWSource;
float4		dUVWTarget;

struct VS_IN
{
	float4	__Position			: SV_POSITION;
	uint	SliceIndex			: SV_INSTANCEID;
};

struct GS_IN
{
	float4	__Position			: SV_POSITION;
	float3	UVW					: TEXCOORD0;
	uint	SliceIndex			: SLICE_INDEX;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float3	UVW					: TEXCOORD0;
	uint	SliceIndex			: SV_RENDERTARGETARRAYINDEX;
};

GS_IN	VS( VS_IN _In )
{
	GS_IN	Out;
	Out.__Position = _In.__Position;
	Out.SliceIndex = _In.SliceIndex;
//	Out.UVW = float3( 0.5 * (1.0 + _In.__Position.xy), 1.0 * _In.SliceIndex * dUVWTarget.z );
	Out.UVW = float3( 0.5 * (1.0 + _In.__Position.xy), _In.SliceIndex / 64.0 );

	return Out;
}

[maxvertexcount( 3 )]
void	GS( triangle GS_IN _In[3], inout TriangleStream<PS_IN> _Stream )
{
	PS_IN	Out;
	Out.__Position = _In[0].__Position;
	Out.UVW = _In[0].UVW;
	Out.SliceIndex = _In[0].SliceIndex;
	_Stream.Append( Out );

	Out.__Position = _In[1].__Position;
	Out.UVW = _In[1].UVW;
	Out.SliceIndex = _In[1].SliceIndex;
	_Stream.Append( Out );

	Out.__Position = _In[2].__Position;
	Out.UVW = _In[2].UVW;
	Out.SliceIndex = _In[2].SliceIndex;
	_Stream.Append( Out );
}

// Initialize distance field
//
float4	PS( PS_IN _In ) : SV_TARGET0
{
	_In.UVW.xy = _In.__Position.xy * dUVWTarget.xy;
//	_In.UVW -= 0.5 * dUVWTarget.xyz;

	float	PhotonsCount = PhotonMap.SampleLevel( NearestClamp, _In.UVW, 0 ).w;
	return float4( 0.0.xxx, PhotonsCount > 0.0 ? 0.0 : INFINITY );
}

// Propagate distance field
//
static const float3	NeighborDirections[] =
{
	// Back samples (9)
	float3( -1.0, -1.0, -1.0 ),	float3( 0.0, -1.0, -1.0 ), float3( +1.0, -1.0, -1.0 ),
	float3( -1.0,  0.0, -1.0 ),	float3( 0.0,  0.0, -1.0 ), float3( +1.0,  0.0, -1.0 ),
	float3( -1.0, +1.0, -1.0 ),	float3( 0.0, +1.0, -1.0 ), float3( +1.0, +1.0, -1.0 ),

	// Middle samples (8)
	float3( -1.0, -1.0,  0.0 ),	float3( 0.0, -1.0,  0.0 ), float3( +1.0, -1.0,  0.0 ),
	float3( -1.0,  0.0,  0.0 ),	                           float3( +1.0,  0.0,  0.0 ),
	float3( -1.0, +1.0,  0.0 ),	float3( 0.0, +1.0,  0.0 ), float3( +1.0, +1.0,  0.0 ),

	// Front samples (9)
	float3( -1.0, -1.0, +1.0 ),	float3( 0.0, -1.0, +1.0 ), float3( +1.0, -1.0, +1.0 ),
	float3( -1.0,  0.0, +1.0 ),	float3( 0.0,  0.0, +1.0 ), float3( +1.0,  0.0, +1.0 ),
	float3( -1.0, +1.0, +1.0 ),	float3( 0.0, +1.0, +1.0 ), float3( +1.0, +1.0, +1.0 ),
};

static const float	NeighborDistances[] =
{
	// Back distances (9)
	SQRT3, SQRT2, SQRT3,
	SQRT2,  1.0,  SQRT2,
	SQRT3, SQRT2, SQRT3,

	// Middle distances (8)
	SQRT2,  1.0,  SQRT2,
	 1.0,          1.0,
	SQRT2,  1.0,  SQRT2,

	// Front distances (9)
	SQRT3, SQRT2, SQRT3,
	SQRT2,  1.0,  SQRT2,
	SQRT3, SQRT2, SQRT3,
};

float4	PS2( PS_IN _In ) : SV_TARGET0
{
	_In.UVW.xy = _In.__Position.xy * dUVWTarget.xy;
//	_In.UVW += 0.1 * dUVWTarget.xyz;
	_In.UVW.z += 0.5 * dUVWTarget.z;

	// Sample current distance
	float4	CurrentDirectionDistance = PreviousDistanceField.SampleLevel( LinearClamp, _In.UVW, 0 );
	if ( CurrentDirectionDistance.w < 1e-3f )
		return CurrentDirectionDistance;	// This is a place where we can find a photon immediately ! No need to sample neighbors...

	// Sample neighbor distances
	float4	ClosestDirectionDistance = float4( 0.0.xxx, INFINITY );
	for ( int NeighborIndex=0; NeighborIndex < 3*9-1; NeighborIndex++ )
	{
		float3	NeighborDirection = NeighborDirections[NeighborIndex];
		float	NeighborDistance = NeighborDistances[NeighborIndex];
		float4	NeighborDirectionDistance = PreviousDistanceField.SampleLevel( NearestClamp, _In.UVW + dUVWSource.xyz * NeighborDirection, 0 );

		// Accumulate neighbor direction and increase neighbor distance since we're evaluating direction/distance for current texel
		NeighborDirectionDistance.xyz += NeighborDirection;
		NeighborDirectionDistance.w += NeighborDistance;

//NeighborDirectionDistance.xyz += float3( 0, 0, 1 );

		// Check if it's closer than what we currently have
		if ( NeighborDirectionDistance.w < ClosestDirectionDistance.w )
			ClosestDirectionDistance = NeighborDirectionDistance;	// Found a closer texel !
	}

// 	if ( ClosestDirectionDistance.w > 0.5 * INFINITY )
// 		return CurrentDirectionDistance;	// This texel is surrounded by invalid texels...

	// We must choose between current & closest neighbor texels here...
	return ClosestDirectionDistance.w < CurrentDirectionDistance.w ? ClosestDirectionDistance : CurrentDirectionDistance;
}


technique10 InitializeDistanceField
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 PropagateDistanceField
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
