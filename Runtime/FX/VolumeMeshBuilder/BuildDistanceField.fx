// Processes the stacks of slices to build mip-maps and compose the final 3D textures
//
#include "../Samplers.fx"

//#define SIGNED_DISTANCE_FIELD

static const float	INFINITY = 1e6;
static const float	ZERO_DISTANCE = 1e-3;
static const float	SQRT2 = 1.4142135623730950488016887242097;
static const float	SQRT3 = 1.7320508075688772935274463415059;

float4		BufferInvSize;
Texture3D	PreviousDistanceField;
Texture3D	NormalField;	// The normal field is used during propagation to create a signed distance

struct VS_IN
{
	float4	Position	: SV_POSITION;
	uint	SliceIndex	: SV_INSTANCEID;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
	uint	SliceIndex	: SV_RENDERTARGETARRAYINDEX;
};

VS_IN VS( VS_IN _In ) { return _In; }

[maxvertexcount(3)]
void GS( triangle VS_IN _In[3], inout TriangleStream<PS_IN> _Stream )
{
	PS_IN	Out;
	Out.SliceIndex = _In[0].SliceIndex;
	Out.Position = _In[0].Position;
	_Stream.Append( Out );
	Out.Position = _In[1].Position;
	_Stream.Append( Out );
	Out.Position = _In[2].Position;
	_Stream.Append( Out );
}

// ===================================================================================
// Initializes the distance field
//
float4 PS_Init( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = float3( _In.Position.xy, _In.SliceIndex + 0.5 ) * BufferInvSize.xyz;
	float	Opacity = PreviousDistanceField.SampleLevel( NearestClamp, UVW, 0 ).w;		// Opacity guides the emptyness
	return float4( 0.0.xxx, Opacity > 1e-3 ? 0.0 : INFINITY );
}

// ===================================================================================
// Propagate the distance field
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

float4	PS_Propagate( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = float3( _In.Position.xy, _In.SliceIndex + 0.5 ) * BufferInvSize.xyz;

	// Sample current distance
	float4	CurrentDirectionDistance = PreviousDistanceField.SampleLevel( NearestClamp, UVW, 0 );
	if ( abs(CurrentDirectionDistance.w) < 1e-3f )
		return CurrentDirectionDistance;	// This is a place where we can find a filled voxel immediately ! No need to sample neighbors...

	// Sample neighbor distances
	float4	ClosestDirectionDistance = float4( 0.0.xxx, INFINITY );
	int	NeighborsNegativeCount = CurrentDirectionDistance.w > 0.0 && CurrentDirectionDistance.w < 0.9 * INFINITY ? 1 : 0;
	int	NeighborsPositiveCount = CurrentDirectionDistance.w < 0.0 ? 1 : 0;
	for ( int NeighborIndex=0; NeighborIndex < 3*9-1; NeighborIndex++ )
	{
		float3	NeighborDirection = NeighborDirections[NeighborIndex];
		float	NeighborDistance = NeighborDistances[NeighborIndex];
		float3	NeighborUVW = UVW + BufferInvSize.xyz * NeighborDirection;
		float4	NeighborDirectionDistance = PreviousDistanceField.SampleLevel( NearestClamp, NeighborUVW, 0 );
		if ( NeighborDirectionDistance.w > 0.9 * INFINITY )
			continue;	// Un-initialized distance...

		if ( abs(NeighborDirectionDistance.w) < ZERO_DISTANCE )
		{	// The neighbor is a filled voxel...
			// We must sample the voxel's normal to check if we're standing in front or behind the mesh's surface
			// If we're behind it, we propagate a negative distance. Positive otherwise...
			// Of course, this does not work for open manifolds but the propagation should take care of the positive/negative distances
			//	boundaries as we always take the minimal distance in absolute value.
			float3	SurfaceNormal = normalize( NormalField.SampleLevel( NearestClamp, NeighborUVW, 0.0 ).xyz );
			float3	NeighborDirectionWorld = normalize( float3( -NeighborDirection.x, NeighborDirection.y, -NeighborDirection.z ) );
#ifdef SIGNED_DISTANCE_FIELD
			float	DotNormal = dot( SurfaceNormal, NeighborDirectionWorld );
			NeighborDistance *= 2.0 * step( -0.0, DotNormal ) - 1.0;		// Negative if DotNormal is negative
#endif
		}
		else
		{	// The neighbor is an empty voxel...
			NeighborDistance *= 2.0 * step( 0.0, NeighborDirectionDistance.w ) - 1.0;	// Keep the sign of the neighbor distance
		}

		// Accumulate neighbor direction and increase neighbor distance
		NeighborDirectionDistance.xyz += NeighborDirection;
		NeighborDirectionDistance.w += NeighborDistance;

		// Increase sign counters
		if ( NeighborDistance > 0.0 )
			NeighborsPositiveCount++;	// One more neighbor has a positive distance
		else// if ( NeighborDirectionDistance.w < -1e-3 )
			NeighborsNegativeCount++;	// One more neighbor has a negative distance

		// Check if it's closer than what we currently have
		if ( abs(NeighborDirectionDistance.w) < abs(ClosestDirectionDistance.w) )
			ClosestDirectionDistance = NeighborDirectionDistance;	// Found a closer texel !
	}

	// Use the majority vote to choose the sign of our distance
// 	if ( NeighborsPositiveCount > NeighborsNegativeCount )
// 		ClosestDirectionDistance.w = abs(ClosestDirectionDistance.w);	// We must go positive
// 	else if ( NeighborsPositiveCount < NeighborsNegativeCount )
// 		ClosestDirectionDistance.w = -abs(ClosestDirectionDistance.w);	// We must go negative

	if ( NeighborsPositiveCount > 20 && ClosestDirectionDistance.w < 0.0 )
		ClosestDirectionDistance.w *= -1.0;	// Negative in a majority of positive ? Flip !
	if ( NeighborsNegativeCount > 20 && ClosestDirectionDistance.w > 0.0 )
		ClosestDirectionDistance.w *= -1.0;	// Positive in a majority of negative ? Flip !

	// We must choose between current & closest neighbor texels here...
	return abs(ClosestDirectionDistance.w) < abs(CurrentDirectionDistance.w) ? ClosestDirectionDistance : CurrentDirectionDistance;
}

// ===================================================================================
// Builds the mip-maps
//
float4 PS_MipMaps( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = float3( _In.Position.xy, 0.5 + _In.SliceIndex ) * BufferInvSize.xyz;
	float4	dUVW = 0.5 * BufferInvSize;	// We're sampling the higher mip level which has twice the resolution of this one

	float4	V  = PreviousDistanceField.SampleLevel( NearestClamp, UVW, 0 );
			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.xww, 0 );
			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.wyw, 0 );
			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.xyw, 0 );

			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.wwz, 0 );
			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.xwz, 0 );
			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.wyz, 0 );
			V += PreviousDistanceField.SampleLevel( NearestClamp, UVW + dUVW.xyz, 0 );

	return 0.125 * V;
}

// ===================================================================================
//
technique10 InitDistanceField
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Init() ) );
	}
}

technique10 PropagateDistanceField
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Propagate() ) );
	}
}

technique10 BuildMipMaps
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_MipMaps() ) );
	}
}
