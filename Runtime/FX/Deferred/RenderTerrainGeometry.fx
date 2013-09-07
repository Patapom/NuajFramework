// This renders the terrain tiles (Normal+Depth in CAMERA space) into the terrain geometry buffer
//
#include "../Camera.fx"
#include "../Samplers.fx"

float		TileSize;			// Size of a tile in WORLD units (tile vertices are normalized in [0,1] so we expand the vertices to actual WORLD coordinates using this as a factor)
float2		TilePosition;		// Tile's position (in TILES units)

// TPage management
float2		TPageUVOffset;		// The UV offset at which the current tile stays in the global TPage
float		InvTPageUVTileSize;	// 1 / size of a tile texture in the TPage in UV space
Texture2D	TilesTPage;			// The global TPage to read WORLD normals & heights from for each tile

struct VS_IN
{
	float3	Position		: POSITION;
	float2	HeightFactors	: TEXCOORD0;	// X=0 for internal vertices, 1 for border vertices
											// Y=0 for immutable vertices ("apron vertices"), 1 for vertices subject to height change
};

struct PS_IN
{
	float4	__Position		: SV_POSITION;
	float3	Position		: POSITION;		// Position in CAMERA space
	float3	Normal			: NORMAL;		// Normal in CAMERA space
};

PS_IN VS( VS_IN _In )
{
	// Retrieve WORLD normal + height values from the global TPage for that vertex
	float2		TPageUV = TPageUVOffset + _In.Position.xz * InvTPageUVTileSize;
	float4		TileNormalHeight = TilesTPage.SampleLevel( NearestClamp, TPageUV, 0 );

	// Compute WORLD position
	float3		Position = float3( (TilePosition + _In.Position.xz - 0.5) * TileSize, 0.0 );
	if ( _In.HeightFactors.y > 0.5 )
		Position.z += TileNormalHeight.w;	// Add height
//	Position.z = 0.5 * (Position.x + Position.y);

	float4		CameraPosition = mul( float4( Position.xzy, 1.0 ), World2Camera );
	float3		CameraNormal = mul( float4( TileNormalHeight.xyz, 0.0 ), World2Camera ).xyz;
//	float3		CameraNormal = TileNormalHeight.xyz;
//CameraNormal = 0.25 + 0.75 * float3( _In.Position.x, 0.0, _In.Position.z );
//CameraNormal = 1.0;

	PS_IN	Out;
	Out.__Position = mul( CameraPosition, Camera2Proj );
	Out.Position = CameraPosition.xyz;
	Out.Normal = CameraNormal;
	
	return	Out;
}

// Write CAMERA normal & depth
float4 PS( PS_IN _In ) : SV_TARGET0
{
	return float4( _In.Normal, _In.Position.z );
}


// ===================================================================================
//
technique10 RenderTerrainGeometry
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
