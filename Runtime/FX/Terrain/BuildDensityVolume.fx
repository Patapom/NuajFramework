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

float PS( PS_IN _In ) : SV_TARGET0
{
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
