// Renders the volume mesh slices to debug lighting
//
#include "../Camera.Fx"
#include "../Samplers.fx"


float4		BufferInvSize;
float3		WorldSize;			// Volume size in WORLD space

float3		VolumeSize;
float3		VolumeInvSize;
Texture3D	DiffuseField;
Texture3D	NormalField;
Texture3D	DistanceField;

float		MipBias = 0.0;

float2		DirectIrradianceMapInvSize;
Texture2D	NonEmptyVoxels2Dto3D;
Texture3D	NonEmptyVoxels3Dto2D;
Texture3D	IndirectLightingField;

struct VS_IN
{
	float4	Position	: SV_POSITION;
	uint	SliceIndex	: SV_INSTANCEID;
};

// ===================================================================================
//
struct PS_IN
{
	float4	Position	: SV_POSITION;
	float3	UVW			: TEXCOORD0;
	uint	SliceIndex	: SV_INSTANCEID;
};

PS_IN VS_Slices( VS_IN _In )
{
	float3	ProjPosition = float3( _In.Position.xy, 2.0 * _In.SliceIndex * VolumeInvSize.z - 1.0 );
	float3	WorldPosition = 0.5 * ProjPosition * WorldSize;

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1.0 ), World2Proj );

	Out.UVW.x = 0.5 * (1.0 + _In.Position.x) * (VolumeSize.x-1) * VolumeInvSize.x;
	Out.UVW.y = 0.5 * (1.0 - _In.Position.y) * (VolumeSize.y-1) * VolumeInvSize.y;
	Out.UVW.z = (_In.SliceIndex + 0.5) * VolumeInvSize.z;

	Out.SliceIndex = _In.SliceIndex;
	return Out;
}

float4 PS_Slices( PS_IN _In ) : SV_TARGET0
{
//	float4	Diffuse = DiffuseField.SampleLevel( LinearClamp, _In.UVW, 0.0 );
	float4	Diffuse = DiffuseField.SampleLevel( NearestClamp, _In.UVW, 0.0 );
//	return Diffuse;
//	return float4( _In.UVW, Diffuse.w );


// This is used to ensure that both NonEmptyVoxels3Dto2D and the DiffuseField have coincident non-empty values
// This test shows the scene in black. Every non-coincident voxel will be shown in red or green ! Watch out for those !
// 	float2	Test = NonEmptyVoxels3Dto2D.SampleLevel( NearestClamp, _In.UVW, 0.0 ).xy * 65535.0;
// 	bool	IsEmpty = Test.x >= 65535.0 && Test.x >= 65535.0;
// 	bool	IsEmpty2 = Diffuse.w < 1e-3;
// //	return float4( _In.UVW, !IsEmpty || !IsEmpty2 ? 1 : 0 );
// 	return float4( IsEmpty ? 1 : 0, IsEmpty2 ? 1 : 0, 0, !IsEmpty || !IsEmpty2 ? 1 : 0 );


// This is to investigate precision problems and the fact that the 2D->3D and 3D->2D maps should be perfectly bijective
// For instance, I discovered the +0.5 offset in UVs is MOST important otherwise everything gets totally shifted
// 	float2	NonEmptyVoxel2DUV = NonEmptyVoxels3Dto2D.SampleLevel( NearestWrap, _In.UVW, 0.0 ).xy * 65535.0;
// 			NonEmptyVoxel2DUV += 0.5;	// SUPER IMPORTANT LINE !
// 			NonEmptyVoxel2DUV *= DirectIrradianceMapInvSize;
// //	return float4( NonEmptyVoxel2DUV, 0, Diffuse.w );
// 	float3	NonEmptyVoxel3DUVW = NonEmptyVoxels2Dto3D.SampleLevel( NearestWrap, NonEmptyVoxel2DUV, 0.0 ).xyz * 255.0;
// 			NonEmptyVoxel3DUVW *= VolumeInvSize;
// 	return float4( NonEmptyVoxel3DUVW, Diffuse.w );

//	return float4( NormalField.SampleLevel( NearestClamp, _In.UVW, 0.0 ).xyz, Diffuse.w );
	float4	IndirectLighting = IndirectLightingField.SampleLevel( LinearClamp, _In.UVW, MipBias );

//	IndirectLighting *= 2.0;

//	return float4( IndirectLighting.xyz, 1.0 );
	return float4( IndirectLighting.xyz, Diffuse.w );
}

// ===================================================================================
VS_IN	VS_Direct( VS_IN _In )	{ return _In; }

Texture2D	DirectIrradiance;
Texture2D	PrecisionTest;

float4	PS_Direct( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
//	UV = _In.Position.xy / 128.0;		// This is pixel-wise
//	UV = (_In.Position.xy-0.5) / 128.0;	// This is fully bilerped (or same pixel (i.e. floor) in nearest)
//	UV = (_In.Position.xy+0.5) / 128.0;	// This is fully bilerped (or next pixel (i.e. ceil) in nearest)

//	UV.x = 0.5 / 128.0;		// First column
//	UV.x = 127.5 / 128.0;	// Last column

//	return float4( PrecisionTest.SampleLevel( NearestWrap, UV, 0.0 ).xyz, 1.0 );
// 	return float4( PrecisionTest.SampleLevel( LinearWrap, UV, 0.0 ).xyz, 1.0 );

// 	if ( abs( _In.Position.x - (832-0.5)) < 0.0001 )
// 		return float4( 1, 0, 0, 1 );
// 	else
// 		return float4( 0, 0, 0, 1);

	return float4( DirectIrradiance.SampleLevel( LinearClamp, UV, 0.0 ).xyz, 1.0 );
}

// ===================================================================================
//
technique10 RenderVolumeMesh_Slices
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Slices() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Slices() ) );
	}
}

technique10 RenderVolumeMesh_Irradiance2D
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Direct() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Direct() ) );
	}
}
