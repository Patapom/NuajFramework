// This shader reads density values from a previously written density volume texture.
// It then generates a marching-cube case in [0,255] and the geometry shader finally
//	outputs the necessary triangles for each voxel of the computed block.
//

#include "../Camera.fx"

struct VS_IN
{
	uint	PositionX	: POSITION_X;		// Cell X position
	uint	PositionY	: POSITION_Y;		// Cell Y position
	uint	PositionZ	: SV_InstanceID;	// Cell Z position
};

struct GS_IN
{
	float4	Field0123 : TEXCOORD0;			// Density field values for vertices 0, 1, 2 and 3
	float4	Field4567 : TEXCOORD1;			// Density field values for vertices 4, 5, 6 and 7
	uint	z8_y8_x8_case8	: TEXCOORD2;	// XYZ integer voxel position within the block (i.e. cell index) + cube case
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	WorldPosition : POSITION;
	float3	WorldNormal : NORMAL;
};

Texture3D DensityVolumeTexture;

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

cbuffer	cbTables
{
	float3	EdgeStart[] = {
		float3( 0, 0, 1 ), float3( 0, 1, 1 ), float3( 1, 1, 1 ), float3( 1, 0, 1 ), 
        float3( 0, 0, 0 ), float3( 0, 1, 0 ), float3( 1, 1, 0 ), float3( 1, 0, 0 ), 
        float3( 0, 0, 1 ), float3( 0, 1, 1 ), float3( 1, 1, 1 ), float3( 1, 0, 1 ), 
	};

	float3	EdgeDirection[] = {
		float3( 0, 1, 0 ),  float3( 1, 0, 0 ),  float3( 0, -1, 0 ), float3( -1, 0, 0 ), 
        float3( 0, 1, 0 ),  float3( 1, 0, 0 ),  float3( 0, -1, 0 ), float3( -1, 0, 0 ), 
        float3( 0, 0, -1 ),  float3( 0, 0, -1 ),  float3( 0, 0, -1 ), float3( 0, 0, -1 ), 
	};

	// Density masks to unpack densities from float4 to float based on edge index
	float4	DensityMasks0123[12][2] = {
		{ float4( 1, 0, 0, 0 ), float4( 0, 1, 0, 0 ) },	// v0->v1
		{ float4( 0, 1, 0, 0 ), float4( 0, 0, 1, 0 ) },	// v1->v2
		{ float4( 0, 0, 1, 0 ), float4( 0, 0, 0, 1 ) },	// v2->v3
		{ float4( 0, 0, 0, 1 ), float4( 1, 0, 0, 0 ) },	// v3->v0
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v4->v5
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v5->v6
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v6->v7
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v7->v4
		{ float4( 1, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v0->v4
		{ float4( 0, 1, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v1->v5
		{ float4( 0, 0, 1, 0 ), float4( 0, 0, 0, 0 ) },	// v2->v6
		{ float4( 0, 0, 0, 1 ), float4( 0, 0, 0, 0 ) },	// v3->v7
	};
	float4	DensityMasks4567[12][2] = {
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v0->v1
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v1->v2
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v2->v3
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 0 ) },	// v3->v0
		{ float4( 1, 0, 0, 0 ), float4( 0, 1, 0, 0 ) },	// v4->v5
		{ float4( 0, 1, 0, 0 ), float4( 0, 0, 1, 0 ) },	// v5->v6
		{ float4( 0, 0, 1, 0 ), float4( 0, 0, 0, 1 ) },	// v6->v7
		{ float4( 0, 0, 0, 1 ), float4( 1, 0, 0, 0 ) },	// v7->v4
		{ float4( 0, 0, 0, 0 ), float4( 1, 0, 0, 0 ) },	// v0->v4
		{ float4( 0, 0, 0, 0 ), float4( 0, 1, 0, 0 ) },	// v1->v5
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 1, 0 ) },	// v2->v6
		{ float4( 0, 0, 0, 0 ), float4( 0, 0, 0, 1 ) },	// v3->v7
	};

#include "Case2TrianglesTable.fx"
}

cbuffer cbLOD
{
	float	TextureSize = 65;
	float2	InvTextureSize = float2( 1.0 / 65, 0 );
	float	InvVoxelCellsCount = 1.0 / 64.0;
	float	BlockWorldSize = 4.0;
	float	BlockCellWorldSize = 4.0 / 64.0;	// Block world size / amount of cells
}

// This should be updated for every block
float3	BlockWorldPosition = float3( 0, 0, 0 );	// Position of the lower-left corner of the current block


float	ComputeDensity( float3 _UVW )
{
	float	Density = 0.15 * DensityVolumeTexture.SampleLevel( NearestClamp, _UVW + InvTextureSize.yyx, 0).x;

	// Add a steady spherical density
	float3	Position = _UVW - 0.5.xxx;
	Density += 0.4 - length( Position );

	return Density;
}

// Samples the density at the 8 corners of the voxel cell and generates the corresponding marching-cube case
//
GS_IN VS( VS_IN _In )
{
 	float3	CellCoordinates = float3( _In.PositionX, _In.PositionY, _In.PositionZ );
	float3	UVW = CellCoordinates * InvTextureSize.xxx;

	GS_IN	Out;
	Out.Field0123.x = ComputeDensity( UVW + InvTextureSize.yyx );
	Out.Field0123.y = ComputeDensity( UVW + InvTextureSize.yxx );
	Out.Field0123.z = ComputeDensity( UVW + InvTextureSize.xxx );
	Out.Field0123.w = ComputeDensity( UVW + InvTextureSize.xyx );
	Out.Field4567.x = ComputeDensity( UVW + InvTextureSize.yyy );
	Out.Field4567.y = ComputeDensity( UVW + InvTextureSize.yxy );
	Out.Field4567.z = ComputeDensity( UVW + InvTextureSize.xxy );
	Out.Field4567.w = ComputeDensity( UVW + InvTextureSize.xyy );

	uint4	i0123 = (uint4) saturate( Out.Field0123*99999 );
	uint4	i4567 = (uint4) saturate( Out.Field4567*99999 );
	int	CubeCase = (i0123.x     ) | (i0123.y << 1) | (i0123.z << 2) | (i0123.w << 3) |
				   (i4567.x << 4) | (i4567.y << 5) | (i4567.z << 6) | (i4567.w << 7);

	Out.z8_y8_x8_case8 = (_In.PositionZ << 24) |
						 (_In.PositionY << 16) |
						 (_In.PositionX <<  8) |
						 (CubeCase           ) ;

	return Out;
}

// Computes the normal at the given world position by extracting the gradient from the density function
//
float3	ComputeVertexNormal( float3 _UVW )
{
	float3	VoxelCoordinates = (_UVW - BlockWorldPosition) / BlockWorldSize;

	// From the last shader, we determined that
	// Vox(UVW) = (UVW*TextureSize-1) / (TextureSize-3)
	//
	// So :
	// UVW(Vox) = (Vox * (TextureSize-3) + 1) / TextureSize
	//
	float3	UVW = (VoxelCoordinates * (TextureSize-3) + 1) * InvTextureSize.x;

	float2	Delta = 0.5 * InvTextureSize;	// This is so we don't sample too far appart, otherwise discrepancies still arise on boundaries...

	float3	Gradient;
	Gradient.x =  ComputeDensity( UVW + Delta.xyy ).x
				- ComputeDensity( UVW - Delta.xyy ).x;
	Gradient.y =  ComputeDensity( UVW + Delta.yxy ).x
				- ComputeDensity( UVW - Delta.yxy ).x;
	Gradient.z =  ComputeDensity( UVW + Delta.yyx ).x
				- ComputeDensity( UVW - Delta.yyx ).x;

	return -normalize( Gradient );
}

// Computes the vertex position given the edge index
//
float3	ComputeVertexPosition( int _EdgeIndex, float4 _Densities0123, float4 _Densities4567, float3 _CellCornerWorldPosition )
{
	float3	EdgePos0 = _CellCornerWorldPosition + EdgeStart[_EdgeIndex] * BlockCellWorldSize;
	float3	EdgePos1 = EdgePos0 + EdgeDirection[_EdgeIndex] * BlockCellWorldSize;
	float	Density0 = dot( _Densities0123, DensityMasks0123[_EdgeIndex][0] ) + dot( _Densities4567, DensityMasks4567[_EdgeIndex][0] );
	float	Density1 = dot( _Densities0123, DensityMasks0123[_EdgeIndex][1] ) + dot( _Densities4567, DensityMasks4567[_EdgeIndex][1] );

	return lerp( EdgePos0, EdgePos1, (0.0 - Density0) / (Density1 - Density0) );
//	return lerp( EdgePos0, EdgePos1, saturate( (0.0 - Density0) / (Density1 - Density0) ) );
}

// Generates the actual geometry
[maxvertexcount(15)]
void GS( point GS_IN _In[1], inout TriangleStream<PS_IN> _Stream )
{
	uint	In = _In[0].z8_y8_x8_case8;
	uint	CubeCase = In & 0xFF;
	uint3	UVW = (In.xxx >> uint3( 8, 16, 24 )) & 0xFF;

	float3	CellCornerPosition = BlockWorldPosition + UVW * BlockCellWorldSize;	// Cell's corner position #0 in world space
	int		TrianglesCount = Case2TrianglesCount[CubeCase];						// Amount of triangles to generate for the current case

	float3	VertexPositions[3];
	float3	VertexNormals[3];
	PS_IN	Out;

	for ( int TriangleIndex=0; TriangleIndex < TrianglesCount; TriangleIndex++ )
	{
		uint3	TriangleEdges = Case2Triangles[CubeCase][TriangleIndex];

		// Build vertex #0
		VertexPositions[0] = ComputeVertexPosition( TriangleEdges.x, _In[0].Field0123, _In[0].Field4567, CellCornerPosition );
		VertexNormals[0] = ComputeVertexNormal( VertexPositions[0] );

		// Build vertex #1
		VertexPositions[1] = ComputeVertexPosition( TriangleEdges.y, _In[0].Field0123, _In[0].Field4567, CellCornerPosition );
		VertexNormals[1] = ComputeVertexNormal( VertexPositions[1] );

		// Build vertex #2
		VertexPositions[2] = ComputeVertexPosition( TriangleEdges.z, _In[0].Field0123, _In[0].Field4567, CellCornerPosition );
		VertexNormals[2] = ComputeVertexNormal( VertexPositions[2] );

		// Generate the triangle
		Out.WorldPosition = VertexPositions[0];
		Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
		Out.WorldNormal = VertexNormals[0];
		_Stream.Append( Out );
		Out.WorldPosition = VertexPositions[1];
		Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
		Out.WorldNormal = VertexNormals[1];
		_Stream.Append( Out );
		Out.WorldPosition = VertexPositions[2];
		Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
		Out.WorldNormal = VertexNormals[2];
		_Stream.Append( Out );
		_Stream.RestartStrip();
	}
}

float4	PS( PS_IN _In ) : SV_TARGET
{
	return float4( _In.WorldNormal, 1 );
}

// ===================================================================================
//
technique10 BuildGeometry
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
