// This shader writes a density function into a 3D texture
// The shader should be called using DrawInstanced() as it
//	writes into several	slices of a 3D render target
//

struct VS_IN
{
	float3	Position	: POSITION;			// -1..1, -1..1, 0.5 [quadproxy_geo]
	float2	UV			: TEXCOORD0;		// 0..1, 0..1 for the slice
	uint	InstanceID	: SV_InstanceID;	// Each slice is an instance
};

struct GS_IN
{
	float4	Position		: POSITION;
	float3	WorldPosition	: TEXCOORD0;
	float3	VoxelCoordinates: TEXCOORD1;
	uint	InstanceID		: BLAH;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	WorldPosition	: TEXCOORD0;
	float3	VoxelCoordinates: TEXCOORD1;
	uint	RTIndex			: SV_RenderTargetArrayIndex;
};

cbuffer cbLOD
{
	float	TextureSize = 1+65+1;				// Size of the texture (i.e. corners count + a margin of 1 on each side)
	float	BlockWorldSize = 4.0;				// 1.0, 2.0, or 4.0 depending on LOD
}

// This should be updated for every block
float3	BlockWorldPosition = float3( 0, 0, 0 );	// Position of the lower-left corner of the current block

float	PlaneDistance = 0;

#include "ComputeDensity.fx"

GS_IN VS( VS_IN _In )
{
	GS_IN	Out;
	float4	ProjectedPosition = float4( _In.Position.xy, 0.5, 1 );

	// UVW in [0..1] range
	float3	UVW = float3( _In.UV, _In.InstanceID / TextureSize );

	// We know that voxel coordinates span [0,1] from pixel 1 to pixel TextureSize-2
	// We also know that UVW spans [0,1] from pixel 0 to pixel TextureSize (P=TextureSize * UVW)
	// We pose :
	//	Vox(P) = 0  for P=(1, 1, 1)
	//	Vox(P) = 1  for P=(TextureSize-2, TextureSize-2, TextureSize-2)
	//
	// So grad(Vox(P)) = (1-0) / (TextureSize-3)
	// And Vox(P) = -grad(Vox(P)) for P=(0, 0, 0)
	//
	// We get Vox(P) = -grad(Vox(P)) + P * grad(Vox(P))
	// or Vox(P) = grad(Vox(P)) * (P-1) = (P-1) / (TextureSize-3)
	//
	// As P = TextureSize * UVW
	// We finaly get Vox(UVW) = (UVW*TextureSize-1) / (TextureSize-3)
	//
	float3	VoxelCoordinates = (UVW*TextureSize - 1) / (TextureSize-3);

	Out.Position = ProjectedPosition;
	Out.WorldPosition = BlockWorldPosition + VoxelCoordinates * BlockWorldSize;
	Out.InstanceID = _In.InstanceID;
	Out.VoxelCoordinates  = VoxelCoordinates;

	return Out;
}

// This GS only passes the triangles through
// It's here because the VS can't specify SV_RenderTargetArrayIndex...
// (i.e. only a GS can render to slices of a 3D texture)
//
[maxvertexcount(3)]
void GS( triangle GS_IN _In[3], inout TriangleStream<PS_IN> Stream )
{
	PS_IN	Out;
    for ( int v=0; v<3; v++ ) 
    {
		Out.Position		= _In[v].Position;
		Out.WorldPosition	= _In[v].WorldPosition;
		Out.RTIndex			= _In[v].InstanceID;
		Out.VoxelCoordinates	= _In[v].VoxelCoordinates;
		Stream.Append( Out );
    }
    Stream.RestartStrip();
}

// Funky stuff
float	SpheresInCells( float3 _WorldPosition )
{
	float	fDensity = 0.0;
	for ( int OctaveIndex=0; OctaveIndex < 1; OctaveIndex++ )
	{
		float	fOctavePower = pow( 2.0, OctaveIndex-1 );
		float	fWorldCellScale = 0.1 * fOctavePower;
		float	fWorldCellSize = 8.0 / fOctavePower;
		float3	WorldCellPosition = fWorldCellSize * floor( _WorldPosition / fWorldCellSize );
		float3	CellSphereCenter = float3(
			fWorldCellSize * NHQu( fWorldCellScale * WorldCellPosition, NoiseTexture0 ),
			fWorldCellSize * NHQu( fWorldCellScale * WorldCellPosition, NoiseTexture1 ),
			fWorldCellSize * NHQu( fWorldCellScale * WorldCellPosition, NoiseTexture2 )
			);

		fDensity += 0.5 * fWorldCellSize - length( _WorldPosition - WorldCellPosition - CellSphereCenter );
	}

	return fDensity;
}

float3	Perturbate( float3 _Position )
{
	// Notice the anisotropy on Y
	return float3( NHQs( _Position, NoiseTexture0 ), 0.5 * NHQs( _Position, NoiseTexture1 ), NHQs( _Position, NoiseTexture2 ) );
}

// Computes the density from a polynomial function
//	_Position, the position to evaluate the density for
//	_Center, the center of the ellipsoid
//	_Radius, the 3 radii of the ellipsoid
//
float	PolynomialDensity( float3 _Position, float3 _Center, float3 _Radius )
{
	float	r = saturate( length( (_Position - _Center) / _Radius ) );
	float3	Poly;
	Poly.x = r * r;				// r^2
	Poly.y = Poly.x * Poly.x;	// r^4
	Poly.z = Poly.y * Poly.y;	// r^6

	const float3	Factors = float3( -22.0 / 9.0, 17.0 / 9.0, -4.0 / 9.0 );
	
	return 1.0 + dot( Poly, Factors );
}

float PS( PS_IN _In ) : SV_TARGET0
{
	float3	Position = _In.WorldPosition;



// Simple sphere
//return 7.0 - length( Position * float3( 0.9, 1.0, 1.0 ) - float3( 0, 8, 0 ) );


	// Perturbate position with noise
	Position += 4.0 * Perturbate( 0.01 * Rotate( _In.WorldPosition, octaveMat0 ) );
 	Position += 7.0 * Perturbate( 0.005 * _In.WorldPosition );
 	Position += 2.0 * Perturbate( 0.02 * _In.WorldPosition );
 	Position += 1.0 * Perturbate( 0.04 * _In.WorldPosition );
	Position += 0.5 * Perturbate( 0.08 * _In.WorldPosition );

	// Add density from several flattened ellipsoids
	float3	Centers[] = {
		float3( -4.0, 6.0, 0.0 ),
		float3( 6.0, 12.0, 8.0 ),
	};
	float3	Radii[] = {
		float3( 10.0, 4.0, 8.0 ),
		float3( 10.0, 6.0, 6.0 ),
	};

	float	fDensity = -0.05f;
	for ( int CloudIndex=0; CloudIndex < 2; CloudIndex++ )
	{
		float3	Center = Centers[CloudIndex];
		float3	Radius = Radii[CloudIndex];

		float3	RelativePosition = Position;
		RelativePosition.y = Center.y + (Position.y < Center.y ? 1.0-exp( 0.6 * (Center.y - Position.y) ) : (Position.y-Center.y) * 0.7);

		float	fScaleXZ = lerp( 0.5, 1.0, pow( Position.y / 32.0, 4.0 ) );
		RelativePosition.xz = Center.xz + (Position.xz - Center.xz) * fScaleXZ;

		fDensity += PolynomialDensity( RelativePosition, Center, Radius );
	}

	return fDensity;
	return ComputeDensity( _In.WorldPosition );
}


// ===================================================================================
//
technique10 BuildDensityVolume
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
