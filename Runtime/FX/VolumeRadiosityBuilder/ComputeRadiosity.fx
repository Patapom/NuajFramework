// Computes the radiosity
//
#include "../Samplers.fx"

#define INV_PI	0.31830988618379067153776752674503

int				SliceOffset;

float2			DirectIrradianceMapInvSize;
Texture2D		DirectIrradiance;
float3			VolumeInvSize;
Texture3D		Volume;

Texture3D		DiffuseField;
Texture3D		NormalField;
Texture3D		DistanceField;

Texture3D		VolumeTo2D;		// Maps a voxel coordinate into a 2D UV for form factors query
float2			FormFactorsMapInvSize;
Texture2DArray	FormFactors;


SamplerState NearestClampMipLinear
{
	Filter = MIN_MAG_POINT_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

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
	Out.SliceIndex = SliceOffset + _In[0].SliceIndex;
	Out.Position = _In[0].Position;
	_Stream.Append( Out );
	Out.Position = _In[1].Position;
	_Stream.Append( Out );
	Out.Position = _In[2].Position;
	_Stream.Append( Out );
}

// ===================================================================================
// This shader fills up a 3D direct irradiance volume texture from the 2D direct irradiance map
//
float3 PS_Direct2DTo3D( PS_IN _In ) : SV_TARGET0
{
	// Read back the 2D UV coordinates of that voxel in the 3D->2D map
	float3	UVW = float3( _In.Position.xy, _In.SliceIndex+0.5 ) * VolumeInvSize;
	float2	UV = Volume.SampleLevel( NearestClamp, UVW, 0.0 ).xy * 65535.0;
	if ( UV.x >= 65535.0 || UV.y >= 65535.0 )
		return 0.0;	// This is an empty voxel

	// Sample the direct irradiance in the 2D map
	UV = (UV+0.5) * DirectIrradianceMapInvSize;
	return DirectIrradiance.SampleLevel( NearestClamp, UV, 0.0 ).xyz;
}

// ===================================================================================
// This shader simply builds mip maps for a 3D target
float3	PrevVolumeInvSize;

float3	PS_MipMaps( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = float3( _In.Position.xy, _In.SliceIndex+0.5 ) * VolumeInvSize;
			UVW -= 0.5 * PrevVolumeInvSize;	// Move to the center of the higher level texture's texel
	float4	dUVW = float4( PrevVolumeInvSize, 0.0 );

	float3	V0 = Volume.SampleLevel( NearestClamp, UVW + dUVW.www, 0.0 ).xyz;
	float3	V1 = Volume.SampleLevel( NearestClamp, UVW + dUVW.xww, 0.0 ).xyz;
	float3	V2 = Volume.SampleLevel( NearestClamp, UVW + dUVW.xyw, 0.0 ).xyz;
	float3	V3 = Volume.SampleLevel( NearestClamp, UVW + dUVW.wyw, 0.0 ).xyz;
	float3	V4 = Volume.SampleLevel( NearestClamp, UVW + dUVW.wwz, 0.0 ).xyz;
	float3	V5 = Volume.SampleLevel( NearestClamp, UVW + dUVW.xwz, 0.0 ).xyz;
	float3	V6 = Volume.SampleLevel( NearestClamp, UVW + dUVW.xyz, 0.0 ).xyz;
	float3	V7 = Volume.SampleLevel( NearestClamp, UVW + dUVW.wyz, 0.0 ).xyz;

	return 0.125 * (V0+V1+V2+V3+V4+V5+V6+V7);
}

// ===================================================================================
// This shader is the main part of the algorithm
// It accumulates irradiance from neighbors using the map of form factors
int		RayBundlesCount;
float	RaySolidAngle;
float	VoxelSize;
float	VoxelInvSize;
float	RayWeights[100];
float	GatherMipBias = 1.0;
float3	EnvironmentColor;

// Samples and unpacks the 10 ray intersections for the specified ray bundle
//
void	GetRayBundleIntersections( float2 _UV, int _RayBundleIndex, out float3 _Intersections[10] )
{
	float	ArraySlice = 8.0 * _RayBundleIndex;

	float4	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[0] = I.xyz;	_Intersections[1].x = I.x;
	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[1].yz = I.xy;	_Intersections[2].xy = I.zw;
	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[2].z = I.x;	_Intersections[3] = I.yzw;

	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[4] = I.xyz;	_Intersections[5].x = I.x;
	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[5].yz = I.xy;	_Intersections[6].xy = I.zw;
	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[6].z = I.x;	_Intersections[7] = I.yzw;

	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[8] = I.xyz;	_Intersections[9].x = I.x;
	I = FormFactors.SampleLevel( NearestClamp, float3( _UV, ArraySlice++ ), 0.0 );
	_Intersections[9].yz = I.xy;
}

float3	SampleIrradiance( float3 _VoxelCoordinates, float2 _UV, int _RayBundleIndex )
{
	// Retrieve the 10 ray intersections for that bundle
	float3	RayIntersections[10];
	GetRayBundleIntersections( _UV, _RayBundleIndex, RayIntersections );

	// Accumulate irradiance
	int		WeightIndex = 10 * _RayBundleIndex;
	float3	Irradiance = 0.0;
	float	InvalidRaysCount = 0.0;
	for ( int IntersectionIndex=0; IntersectionIndex < 10; IntersectionIndex++ )
	{
		float3	IntersectionCoordinates = floor( RayIntersections[IntersectionIndex] * 255.0 );
		if ( IntersectionCoordinates.x >= 255.0 && IntersectionCoordinates.y >= 255.0 )
		{	// Invalid intersection
			if ( IntersectionCoordinates.z >= 255.0 )
 				Irradiance += RayWeights[WeightIndex++] * EnvironmentColor;	// This should mean the ray exited the volume and escaped into the environment
 			else
				InvalidRaysCount++;		// This is an invalid ray...
			continue;
		}

		float3	UVW = (IntersectionCoordinates + 0.5) * VolumeInvSize;

		// Measure world distance between origin and intersection
		float	WorldDistance = VoxelSize * length( _VoxelCoordinates - IntersectionCoordinates );

		// From the solid angle of a single ray, we can deduce the area of the disc element at such distance
		float	DiscArea = RaySolidAngle * WorldDistance * WorldDistance;

		// ...and the radius of a "sampling sphere"
		float	SphereRadius = sqrt( DiscArea * INV_PI );	// r = sqrt(A/PI)

		// Deduce the mip level to use for sampling at such distance
		float	MipLevel = max( GatherMipBias, log2( SphereRadius * VoxelInvSize ) );

		// Sample previous bounce irradiance & diffuse reflectance
		float3	IntersectionIrradiance = Volume.SampleLevel( NearestClampMipLinear, UVW, MipLevel ).xyz;
		float3	Reflectance = DiffuseField.SampleLevel( NearestClampMipLinear, UVW, MipLevel ).xyz * INV_PI;	// Diffuse reflectance = Albedo / PI

		// Accumulate with weight (the weight is the cos(theta) with the origin's normal)
		Irradiance += RayWeights[WeightIndex++] * IntersectionIrradiance * Reflectance;
	}

//	return 1.0 - 0.1 * InvalidRaysCount;	// Count the amount of invalid rays
//	return Irradiance / (0.001 + InvalidRaysCount);

//	return RaySolidAngle * Irradiance * 10.0 / (10.001 - InvalidRaysCount);
	return RaySolidAngle * Irradiance;
}

float3	PS_Gather( PS_IN _In ) : SV_TARGET0
{
	// Read back form factors UV from 3D->2D map of non-empty voxels
	float3	VoxelCoordinates = float3( _In.Position.xy, _In.SliceIndex + 0.5 );
	float3	UVW = VoxelCoordinates * VolumeInvSize;
	float2	UV = floor( VolumeTo2D.SampleLevel( NearestClamp, UVW, 0.0 ).xy * 65535.0 );
	if ( UV.x >= 65535.0 || UV.y >= 65535.0 )
		return 0.0;	// This is an empty voxel

	UV = (UV + 0.5) * FormFactorsMapInvSize;

// 	// DEBUG : check the average neighbor distance given by the form factors
//	// Note: this should also give some equivalent of a lousy ambient occlusion
// 	float	AverageNeighborDistance = 0.0;
// 	float	ValidNeighborsCount = 1e-5;
// 	for ( int RayBundleIndex=0; RayBundleIndex < RayBundlesCount; RayBundleIndex++ )
// 	{
// 		float3	RayIntersections[10];
// 		GetRayBundleIntersections( UV, RayBundleIndex, RayIntersections );
// 		for ( int i=0; i < 10; i++ )
// 		{
// 			float3	IntersectionCoordinates = floor( RayIntersections[i] * 255.0 );
// 			if ( IntersectionCoordinates.x >= 255.0 && IntersectionCoordinates.y >= 255.0 && IntersectionCoordinates.z >= 255.0 )
// 				continue;	// Invalid intersection
// 
// 			AverageNeighborDistance += length( IntersectionCoordinates - VoxelCoordinates );
// 			ValidNeighborsCount++;
// 		}
// 	}
// //	return 0.05 * ValidNeighborsCount;
// 	return 0.025 * AverageNeighborDistance / ValidNeighborsCount;
// 	// DEBUG

	// Read back form factors
	float3	Irradiance = 0.0;
	for ( int RayBundleIndex=0; RayBundleIndex < RayBundlesCount; RayBundleIndex++ )
		Irradiance += SampleIrradiance( VoxelCoordinates, UV, RayBundleIndex );

	return Irradiance;
}

// ===================================================================================
// This shader simply accumulates current bounce irradiance into the result target
float3	PS_Accumulate( PS_IN _In ) : SV_TARGET0
{
	float3	UVW = float3( _In.Position.xy, _In.SliceIndex+0.5 ) * VolumeInvSize;
	return Volume.SampleLevel( NearestClamp, UVW, 0.0 ).xyz;
}

// ===================================================================================
//
technique10 Build3DIrradianceFrom2DMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Direct2DTo3D() ) );
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

technique10 GatherIrradiance
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Gather() ) );
	}
}

technique10 AccumulateIrradiance
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Accumulate() ) );
	}
}
