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
	float	TextureSize = 65;				// Size of the texture (i.e. corners count + a margin of 1 on each side)
	float	BlockWorldSize = 4.0;			// 1.0, 2.0, or 4.0 depending on LOD
}

Texture3D OldDensityVolumeTexture0;	// Density texture at time -2 Dt
Texture3D OldDensityVolumeTexture1;	// Density texture at time -Dt

// This should be updated for every block
float3	BlockWorldPosition = float3( 0, 0, 0 );	// Position of the lower-left corner of the current block

bool	Draw = false;
float3	DrawCenter = float3( 0.0, 0.0, 0.0 );

#include "ComputeDensity.fx"

GS_IN VS( VS_IN _In )
{
	GS_IN	Out;
	float4	ProjectedPosition = float4( _In.Position.xy, 0.5, 1 );

	// UVW in [0..1] range
	float3	UVW = float3( _In.UV, _In.InstanceID / TextureSize );

	Out.Position = ProjectedPosition;
	Out.WorldPosition = BlockWorldPosition + UVW * BlockWorldSize;
	Out.InstanceID = _In.InstanceID;
	Out.VoxelCoordinates = UVW;

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
		Out.VoxelCoordinates= _In[v].VoxelCoordinates;
		Stream.Append( Out );
    }
    Stream.RestartStrip();
}

float PS( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = _In.VoxelCoordinates;

	// Sample buffer Dt-1 along main directions
	float2	Delta = float2( 1.0 / TextureSize, 0.0 );
	float	DX0 = OldDensityVolumeTexture1.SampleLevel( LinearClamp, UVW + Delta.xyy, 0 ).x;
	float	DX1 = OldDensityVolumeTexture1.SampleLevel( LinearClamp, UVW - Delta.xyy, 0 ).x;
	float	DY0 = OldDensityVolumeTexture1.SampleLevel( LinearClamp, UVW + Delta.yxy, 0 ).x;
	float	DY1 = OldDensityVolumeTexture1.SampleLevel( LinearClamp, UVW - Delta.yxy, 0 ).x;
	float	DZ0 = OldDensityVolumeTexture1.SampleLevel( LinearClamp, UVW + Delta.yyx, 0 ).x;
	float	DZ1 = OldDensityVolumeTexture1.SampleLevel( LinearClamp, UVW - Delta.yyx, 0 ).x;
	float	PrevDC = (DX0+DX1+DY0+DY1+DZ0+DZ1) / 3.0;

	// Replace by drawing
	float	fDistance2Center = length( _In.WorldPosition - DrawCenter );
	if ( Draw && fDistance2Center < 0.1 )
		PrevDC = 1.0 * exp( -14.39 * fDistance2Center * fDistance2Center);

	// Same old magic differential equation formula, but in 3D this time !
	float	DC = OldDensityVolumeTexture0.SampleLevel( LinearClamp, UVW, 0 ).x;
	float	NewDC = 0.92 * (0.9 * PrevDC - DC);

	return NewDC;
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
