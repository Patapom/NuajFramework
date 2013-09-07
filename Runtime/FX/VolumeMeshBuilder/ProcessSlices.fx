// Processes the stacks of slices to build mip-maps and compose the final 3D textures
//
#include "../Samplers.fx"

#define USE_AVERAGE	// Define this to take the average of the 6 texels instead of the MAX

static const float	SQRT2 = 1.4142135623730950488016887242097;
static const float	SQRT3 = 1.7320508075688772935274463415059;

bool		PreMultiplyByAlpha;

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

struct PS_OUT
{
	float4	DiffuseAlpha	: SV_TARGET0;
	float4	Normal			: SV_TARGET1;
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
// Build the mip maps by averaging the pixels of a same slice and alpha blending the averages of 2 different slices
//
float3			PrevInvSize;
float3			BufferInvSize;
Texture2DArray	SlicesDiffuse;
Texture2DArray	SlicesNormal;

PS_OUT PS_MipMaps( PS_IN _In )
{
	PS_OUT	Out;

	float2	UV = _In.Position.xy * BufferInvSize.xy;
			UV -= 0.5 * PrevInvSize.xy;	// Move to the center of the higher level texture's texel
	float	Slice0 = 2.0 * _In.SliceIndex + 0.0;
	float	Slice1 = 2.0 * _In.SliceIndex + 1.0;
	float3	dUV = PrevInvSize;			// Sampling from higher mip level at twice this level's resolution

	/////////////////////////////////////////////////////////
	// Sample diffuse

	// Tap 4 samples of the first slice
	float4	V000 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV, Slice0 ), 0 );
	float4	V001 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV + dUV.xz, Slice0 ), 0 );
	float4	V010 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV + dUV.zy, Slice0 ), 0 );
	float4	V011 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV + dUV.xy, Slice0 ), 0 );
	float4	D0 = 0.25 * (V000 + V001 + V010 + V011);	// Average

	// Tap 4 samples of the second slice
	float4	V100 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV, Slice1 ), 0 );
	float4	V101 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV + dUV.xz, Slice1 ), 0 );
	float4	V110 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV + dUV.zy, Slice1 ), 0 );
	float4	V111 = SlicesDiffuse.SampleLevel( NearestClamp, float3( UV + dUV.xy, Slice1 ), 0 );
	float4	D1 = 0.25 * (V100 + V101 + V110 + V111);	// Average

	// Alpha blend slices together
	if ( PreMultiplyByAlpha )	
		Out.DiffuseAlpha = D0 + D1 * (1.0 - D0.w);			// C = C_front + (1-A_front) * C_back
	else
		Out.DiffuseAlpha = D0 * D0.w + D1 * (1.0 - D0.w);	// C = C_front * A_front + (1-A_front) * C_back

	/////////////////////////////////////////////////////////
	// Sample normal

	// Tap 4 samples of the first slice
	V000 = SlicesNormal.SampleLevel( NearestClamp, float3( UV, Slice0 ), 0 );
	V001 = SlicesNormal.SampleLevel( NearestClamp, float3( UV + dUV.xz, Slice0 ), 0 );
	V010 = SlicesNormal.SampleLevel( NearestClamp, float3( UV + dUV.zy, Slice0 ), 0 );
	V011 = SlicesNormal.SampleLevel( NearestClamp, float3( UV + dUV.xy, Slice0 ), 0 );
	float4	N0 = 0.25 * (V000 + V001 + V010 + V011);	// Average

	// Tap 4 samples of the second slice
	V100 = SlicesNormal.SampleLevel( NearestClamp, float3( UV, Slice1 ), 0 );
	V101 = SlicesNormal.SampleLevel( NearestClamp, float3( UV + dUV.xz, Slice1 ), 0 );
	V110 = SlicesNormal.SampleLevel( NearestClamp, float3( UV + dUV.zy, Slice1 ), 0 );
	V111 = SlicesNormal.SampleLevel( NearestClamp, float3( UV + dUV.xy, Slice1 ), 0 );
	float4	N1 = 0.25 * (V100 + V101 + V110 + V111);	// Average

	// Alpha blend slices together using diffuse opacity
	if ( PreMultiplyByAlpha )
		Out.Normal = N0 + N1 * (1.0 - D0.w);		// N = N_front + (1-A_front) * N_back
	else
		Out.Normal = N0 * D0.w + N1 * (1.0 - D0.w);	// N = N_front * A_front + (1-A_front) * N_back

	return	Out;
}

// ===================================================================================
// Precise downscaling
//
Texture2D	DownscaleSourceDiffuse;
Texture2D	DownscaleSourceNormal;
float2		SourceTextureSize;	// Size of the texture we're sampling
float2		TargetTextureSize;	// Size of the small texture to scale within that big texture
float2		SubPixelUVScale;	// Sub-pixel accuracy scale factor

float2	ComputeUV( float2 _UV, float2 _OffsetInTexels )
{
	float2	UVInTexels = _UV * SourceTextureSize;	// De-normalize
	float2	OffsetUVInTexels = UVInTexels + _OffsetInTexels * SubPixelUVScale;
	return	OffsetUVInTexels / SourceTextureSize;	// Re-normalize
}

PS_OUT PS_Downscale( VS_IN _In )
{
	float2	UV = _In.Position.xy / TargetTextureSize;

	PS_OUT	Out;

	float	W0 = 1.0;
	float	W1 = 0.5;

	// Sample diffuse
	float4	C = DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( 0.0, 0.0 ) ) );

	float4	C0 = W0 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( -1.0, 0.0 ) ) );
	float4	C1 = W0 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( +1.0, 0.0 ) ) );
	float4	C2 = W0 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( 0.0, -1.0 ) ) );
	float4	C3 = W0 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( 0.0, +1.0 ) ) );

	float4	C4 = W1 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( -1.0, -1.0 ) ) );
	float4	C5 = W1 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( +1.0, -1.0 ) ) );
	float4	C6 = W1 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( -1.0, +1.0 ) ) );
	float4	C7 = W1 * DownscaleSourceDiffuse.Sample( LinearClamp, ComputeUV( UV, float2( +1.0, +1.0 ) ) );

	Out.DiffuseAlpha = (C + C0 + C1 + C2 + C3 + C4 + C5 + C6 + C7) / (1.0 + 4.0 * (W0 + W1));

	// Sample normal
	float4	N = DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( 0.0, 0.0 ) ) );

	float4	N0 = W0 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( -1.0, 0.0 ) ) );
	float4	N1 = W0 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( +1.0, 0.0 ) ) );
	float4	N2 = W0 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( 0.0, -1.0 ) ) );
	float4	N3 = W0 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( 0.0, +1.0 ) ) );

	float4	N4 = W1 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( -1.0, -1.0 ) ) );
	float4	N5 = W1 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( +1.0, -1.0 ) ) );
	float4	N6 = W1 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( -1.0, +1.0 ) ) );
	float4	N7 = W1 * DownscaleSourceNormal.Sample( LinearClamp, ComputeUV( UV, float2( +1.0, +1.0 ) ) );

	Out.Normal = (N + N0 + N1 + N2 + N3 + N4 + N5 + N6 + N7) / (1.0 + 4.0 * (W0 + W1));

	return Out;
}

// ===================================================================================
// Builds the final 3D texture from the 6 stacks of slices
//
float3			TargetSize;
float4			InvTargetSize;

Texture2DArray	StackXm_Diffuse;	// -X
Texture2DArray	StackXp_Diffuse;	// +X
Texture2DArray	StackYm_Diffuse;	// -Y
Texture2DArray	StackYp_Diffuse;	// +Y
Texture2DArray	StackZm_Diffuse;	// -Z
Texture2DArray	StackZp_Diffuse;	// +Z

Texture2DArray	StackXm_Normal;		// -X
Texture2DArray	StackXp_Normal;		// +X
Texture2DArray	StackYm_Normal;		// -Y
Texture2DArray	StackYp_Normal;		// +Y
Texture2DArray	StackZm_Normal;		// -Z
Texture2DArray	StackZp_Normal;		// +Z

void	SampleXm( int3 _VoxelPos, int3 _VoxelNegPos, float3 _VoxelUVW, float3 _VoxelNegUVW, out float4 _Diffuse, out float4 _Normal )
{
	float3	UVW = float3( _VoxelNegUVW.z, _VoxelUVW.y, _VoxelNegPos.x );
	_Diffuse = StackXm_Diffuse.SampleLevel( NearestClamp, UVW, 0 );
	_Normal  = StackXm_Normal.SampleLevel( NearestClamp, UVW, 0 );
}

void	SampleXp( int3 _VoxelPos, int3 _VoxelNegPos, float3 _VoxelUVW, float3 _VoxelNegUVW, out float4 _Diffuse, out float4 _Normal )
{
	float3	UVW = float3( _VoxelUVW.z, _VoxelUVW.y, _VoxelPos.x );
	_Diffuse = StackXp_Diffuse.SampleLevel( NearestClamp, UVW, 0 );
	_Normal  = StackXp_Normal.SampleLevel( NearestClamp, UVW, 0 );
}

void	SampleYm( int3 _VoxelPos, int3 _VoxelNegPos, float3 _VoxelUVW, float3 _VoxelNegUVW, out float4 _Diffuse, out float4 _Normal )
{
	float3	UVW = float3( _VoxelUVW.x, _VoxelUVW.z, _VoxelPos.y );
	_Diffuse = StackYm_Diffuse.SampleLevel( NearestClamp, UVW, 0 );
	_Normal  = StackYm_Normal.SampleLevel( NearestClamp, UVW, 0 );
}

void	SampleYp( int3 _VoxelPos, int3 _VoxelNegPos, float3 _VoxelUVW, float3 _VoxelNegUVW, out float4 _Diffuse, out float4 _Normal )
{
	float3	UVW = float3( _VoxelUVW.x, _VoxelNegUVW.z, _VoxelNegPos.y );
	_Diffuse = StackYp_Diffuse.SampleLevel( NearestClamp, UVW, 0 );
	_Normal  = StackYp_Normal.SampleLevel( NearestClamp, UVW, 0 );
}

void	SampleZm( int3 _VoxelPos, int3 _VoxelNegPos, float3 _VoxelUVW, float3 _VoxelNegUVW, out float4 _Diffuse, out float4 _Normal )
{
	float3	UVW = float3( _VoxelNegUVW.x, _VoxelUVW.y, _VoxelPos.z );
	_Diffuse = StackZm_Diffuse.SampleLevel( NearestClamp, UVW, 0 );
	_Normal  = StackZm_Normal.SampleLevel( NearestClamp, UVW, 0 );
}

void	SampleZp( int3 _VoxelPos, int3 _VoxelNegPos, float3 _VoxelUVW, float3 _VoxelNegUVW, out float4 _Diffuse, out float4 _Normal )
{
	float3	UVW = float3( _VoxelUVW.x, _VoxelUVW.y, _VoxelNegPos.z );
	_Diffuse = StackZp_Diffuse.SampleLevel( NearestClamp, UVW, 0 );
	_Normal  = StackZp_Normal.SampleLevel( NearestClamp, UVW, 0 );
}

PS_OUT	PS_3D( PS_IN _In )
{
	float3	VoxelPosition = int3( floor( _In.Position.xy ), TargetSize.z - 1 - _In.SliceIndex );
	float3	VoxelUVW = (VoxelPosition+0.5) * InvTargetSize.xyz;
	float3	VoxelNegPosition = TargetSize - 1 - VoxelPosition;	// 1 - Position
	float3	VoxelNegUVW = (VoxelNegPosition+0.5) * InvTargetSize.xyz;

	// Sample the 6 view stacks
	float4	D[6];
	float4	N[6];
	SampleXm( VoxelPosition, VoxelNegPosition, VoxelUVW, VoxelNegUVW, D[0], N[0] );
	SampleXp( VoxelPosition, VoxelNegPosition, VoxelUVW, VoxelNegUVW, D[1], N[1] );
	SampleYm( VoxelPosition, VoxelNegPosition, VoxelUVW, VoxelNegUVW, D[2], N[2] );
	SampleYp( VoxelPosition, VoxelNegPosition, VoxelUVW, VoxelNegUVW, D[3], N[3] );
	SampleZm( VoxelPosition, VoxelNegPosition, VoxelUVW, VoxelNegUVW, D[4], N[4] );
	SampleZp( VoxelPosition, VoxelNegPosition, VoxelUVW, VoxelNegUVW, D[5], N[5] );

	PS_OUT	Out;
	Out.DiffuseAlpha = 0.0;
	Out.Normal = 0.0;

#ifdef USE_AVERAGE
	// Combine using the AVG operator
	float	SumWeights = 1e-3;
	for ( int SideIndex=0; SideIndex < 6; SideIndex++ )
		if ( D[SideIndex].w > 1e-3 )
		{
			float	Weight = D[SideIndex].w;
			Out.DiffuseAlpha += Weight * D[SideIndex];
			Out.Normal += Weight * N[SideIndex];
			SumWeights += Weight;
		}
	Out.DiffuseAlpha /= SumWeights;
	Out.Normal /= SumWeights;
#else
	// Combine using the MAX operator
	for ( int SideIndex=0; SideIndex < 6; SideIndex++ )
		if ( D[SideIndex].w > Out.DiffuseAlpha.w )
		{	// Found a larger component in that side !
			Out.DiffuseAlpha = D[SideIndex];
			Out.Normal = N[SideIndex];
		}

// 	// Combine using the MAX of normals
// 	float	MaxNormalSqLength = 0.0;
// 	for ( int SideIndex=0; SideIndex < 6; SideIndex++ )
// 	{
// 		float	NormalSqLength = dot( N[SideIndex].xyz, N[SideIndex].xyz );
// 		if ( NormalSqLength > MaxNormalSqLength )
// 		{	// Found a larger normal in that side !
// 			Out.DiffuseAlpha = D[SideIndex];
// 			Out.Normal = N[SideIndex];
// 			MaxNormalSqLength = NormalSqLength;
// 		}
// 	}
#endif

// Out.DiffuseAlpha = D[5];
// Out.Normal = N[5];

	return Out;
}

// ===================================================================================
// Smooth the 3D textures out
//
Texture3D	SourceDiffuse;
Texture3D	SourceNormal;
float		SmoothRadius;

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

static const float	NeighborWeights[] =
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

PS_OUT	PS_Smooth( PS_IN _In )
{
	float3	UVW = float3( _In.Position.xy, _In.SliceIndex + 0.5 ) * InvTargetSize.xyz;

	// Sample current pixel
	PS_OUT	Out;
	Out.DiffuseAlpha = SourceDiffuse.SampleLevel( NearestClamp, UVW, 0 );
	Out.Normal = SourceNormal.SampleLevel( NearestClamp, UVW, 0 );

	if ( Out.DiffuseAlpha.w > 1e-3 )
		return Out;	// Not empty, don't average with neighbors

	// Sample neighbor pixels and average
	float4	SumDiffuse = 0.0;
	float4	SumNormal = 0.0;
	float	SumWeights = 0.0;
	for ( int NeighborIndex=0; NeighborIndex < 3*9-1; NeighborIndex++ )
	{
		float3	NeighborDirection = SmoothRadius * NeighborDirections[NeighborIndex];
		float	NeighborWeight = 1.0 / NeighborWeights[NeighborIndex];
		float3	NeighborUVW = UVW + InvTargetSize.xyz * NeighborDirection;

		float4	Nd = SourceDiffuse.SampleLevel( LinearClamp, NeighborUVW, 0 );
		float4	Nn = SourceNormal.SampleLevel( LinearClamp, NeighborUVW, 0 );

		SumDiffuse += NeighborWeight * Nd;
		SumNormal += NeighborWeight * Nn;
		SumWeights += NeighborWeight;
	}

	Out.DiffuseAlpha = SumDiffuse / SumWeights;
	Out.Normal = SumNormal / SumWeights;

	return Out;
}

// ===================================================================================
//
technique10 BuildMipMaps
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_MipMaps() ) );
	}
}

technique10 Downscale
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Downscale() ) );
	}
}

technique10 Build3DTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_3D() ) );
	}
}

technique10 Smooth3DTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Smooth() ) );
	}
}
