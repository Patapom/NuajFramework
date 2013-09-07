// Computes the direct lighting
//
#include "../Samplers.fx"
#include "ShadowMapSupport.fx"

// User data
float3		LightDirection;
float3		LightColor;

// Data made available to the user
float3		BufferInvSize;
float3		VolumeInvSize;
float3		VolumeOrigin;
float		VoxelSize;
Texture2D	NonEmptyVoxelsPositions;
Texture3D	NormalField;
Texture3D	DiffuseField;

struct VS_IN
{
	float4	Position	: SV_POSITION;
	uint	SliceIndex	: SV_INSTANCEID;
};

VS_IN VS( VS_IN _In ) { return _In; }

void	ComputePositionNormal( float2 _ScreenPosition, out float3 _Position, out float3 _Normal )
{
	float2	UV = _ScreenPosition * BufferInvSize.xy;
	float3	VoxelCoordinates = floor( NonEmptyVoxelsPositions.SampleLevel( NearestClamp, UV, 0.0 ).xyz * 255.0 ) + 0.5;	// Unpack
//	float3	VoxelCoordinates = floor( NonEmptyVoxelsPositions.SampleLevel( NearestClamp, UV, 0.0 ).xyz * 255.0 );	// Unpack

//	VoxelCoordinates.x -= 1.0;
//	VoxelCoordinates.y -= 1.0;
//	VoxelCoordinates.z -= 1.0;

// 	float3	VolumeSize = 1.0 / VolumeInvSize;
// 	VoxelCoordinates.x *= (VolumeSize.x-1.0) / VolumeSize.x;
// 	VoxelCoordinates.y *= (VolumeSize.y-1.0) / VolumeSize.y;
// 	VoxelCoordinates.z *= (VolumeSize.z-1.0) / VolumeSize.z;

	// Compute position in WORLD space, centered in the middle of a voxel
	// (note how Y is reversed as positive Y voxel coordinates go downward in WORLD space)
	_Position = VolumeOrigin + float3( VoxelSize, -VoxelSize, VoxelSize ) * VoxelCoordinates;

	// This is an annoying offset I have to take for some reasons that elude me...
	_Position += VoxelSize * float3( 0.5, -1.0, 0.5 );

	// Normal is sampled from the normal field at specified voxel coordinate
	float3	UVW = VoxelCoordinates * VolumeInvSize;
	_Normal = normalize( NormalField.SampleLevel( NearestClamp, UVW, 0.0 ).xyz );
}

float3	PS( VS_IN _In ) : SV_TARGET0
{
	// Find which position and normal we're asked to light
	float3	Position, Normal;
	ComputePositionNormal( _In.Position.xy, Position, Normal );

//	return 0.06 * Position;
//	return abs(Normal);
//	return dot( Normal, LightDirection );
//	return -Normal;
//	return GetShadow( Position, 5.0 );

	// Perform diffuse lighting

#if 1
	// Really simple here, we use a single directional light and simple shadow mapping
	float	DotDiffuse = saturate( dot( Normal, LightDirection ) );
	return LightColor * DotDiffuse * GetShadow( Position, 2.0 );
#else
	// A couple of omni lights
	float3	Lights[] = 
	{
		float3( 0.0, 0.0, 0.0 ),
		float3( 0.25, 0.0, 0.0 ),
		float3( 0.5, 0.0, 0.0 ),
		float3( 0.75, 0.0, 0.0 ),
		float3( 1.0, 0.0, 0.0 ),

		float3( 0.0, 0.0, 0.5 ),
		float3( 1.0, 0.0, 0.5 ),

		float3( 0.0, 0.0, 1.0 ),
		float3( 0.25, 0.0, 1.0 ),
		float3( 0.5, 0.0, 1.0 ),
		float3( 0.75, 0.0, 1.0 ),
		float3( 1.0, 0.0, 1.0 ),
	};

	float3	Min = float3( -14.0, 9.0, -6.0 );
	float3	Max = float3( +14.0, 9.0, +7.0 );

	float3	Result = 0.0;
	for ( int LightIndex=0; LightIndex < 12; LightIndex++ )
	{
		float3	LightPosition = Min + Lights[LightIndex] * (Max-Min);
		float3	ToLight = LightPosition-Position;
		float	Distance2Light = length(ToLight);
		ToLight /= Distance2Light;

		float	Diffuse = saturate( dot( Normal, ToLight ) );
		Diffuse *= smoothstep( 2.0, 1.0, Distance2Light );
		Result += Diffuse * float3( 1.0, 0.8, 0.2 ) * LightColor;	// Yellowish
	}
	return Result;
#endif
}

// ===================================================================================
//
technique10 ComputeDirectLighting
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
