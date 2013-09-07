// Computes the form factors of non-empty voxels by ray-casting 10 rays into the 3D scene
// The possible intersections of these 10 rays yield 10 XYZ voxel positions that are packed
//	into the render targets
//
#include "../Samplers.fx"

static const int	MAX_STEPS_COUNT = 256;
static const float	MIN_STEP_SIZE = 4e-2;
static const float	HIT_EPSILON = 1e-3;


float3		BufferInvSize;
float3		VolumeInvSize;
Texture2D	NonEmptyVoxelsPositions;
Texture3D	DiffuseField;
Texture3D	NormalField;
Texture3D	DistanceField;

float		OffsetOffWalls;	// The offset from the walls to avoid bad intersections (acnea)
float4		Rays[10];		// The 10 ray orientations

struct VS_IN
{
	float4	Position	: SV_POSITION;
	uint	SliceIndex	: SV_INSTANCEID;
};

struct PS_OUT
{
	float4	T0 : SV_TARGET0;
	float4	T1 : SV_TARGET1;
	float4	T2 : SV_TARGET2;
	float4	T3 : SV_TARGET3;
	float4	T4 : SV_TARGET4;
	float4	T5 : SV_TARGET5;
	float4	T6 : SV_TARGET6;
	float4	T7 : SV_TARGET7;
};

SamplerState LinearBorder
{
	Filter = MIN_MAG_MIP_LINEAR;
//	Filter = MIN_MAG_MIP_POINT;
	AddressU = Border;
	AddressV = Border;
	AddressW = Border;
	BorderColor = 1e3 * float4(1,1,1,1);	// Yields large distances outside the volume...
};

VS_IN VS( VS_IN _In ) { return _In; }

// This performs the actual ray-marching step
//	_Position, the start position in VOXEL space (i.e. non-normalized, discrete voxel coordinates)
//	_View, the view direction in WORLD space (same as voxel space without the discrete attitude)
//	_Normal, the surface's normal we're shooting from
//	_HitPosition, the hit position in VOXEL space
//
bool	ComputeIntersection( float3 _Position, float3 _View, float3 _Normal, out float3 _HitPosition )
{
	// Offset start position a bit to avoid acnea
	_Position += OffsetOffWalls / dot( _View, _Normal );

	// Start tracing
	float4	DirectionDistance = 0.0;
	float4	PreviousDirectionDistance = 1000.0;
	for ( int StepIndex=0; StepIndex < MAX_STEPS_COUNT; StepIndex++ )
	{
		PreviousDirectionDistance = DirectionDistance;

		// Sample distance
		float3	UVW = (_Position + 0.5) * VolumeInvSize.xyz;
		DirectionDistance = DistanceField.SampleLevel( LinearBorder, UVW, 0.0 );

		// Check if we're crossing a 0-distance barrier
		if ( DirectionDistance.w < HIT_EPSILON && PreviousDirectionDistance.w > HIT_EPSILON )
		{	// We may have a hit !

			// Go to exact hit position (according to distance field)
			_Position += DirectionDistance.w * _View;
//			_Position += DirectionDistance.xyz;

// _HitPosition = _Position;
// return true;

			UVW = (_Position + 0.5) * VolumeInvSize.xyz;
			float4	DiffuseAlpha = DiffuseField.SampleLevel( NearestClamp, UVW, 0.0 );
			if ( DiffuseAlpha.w > 1e-3 )
			{	// We definitely have a hit !
				_HitPosition = _Position;
				return true;
			}

			// That was a false hit... Damn ! You almost got me here !
		}

		// March forward
		_Position += max( MIN_STEP_SIZE, DirectionDistance.w ) * _View;
	}

	// Check if we're still inside the volume
	float3	VolumeSize = 1.0 / VolumeInvSize.xyz;
	if ( _Position.x < 0.0 || _Position.x > VolumeSize.x-1.0 ||
		 _Position.y < 0.0 || _Position.y > VolumeSize.y-1.0 ||
		 _Position.z < 0.0 || _Position.z > VolumeSize.z-1.0 )
	{	// This must mean we hit the infinite environment
		_HitPosition = 255.0;
	}
	else
	{	// This is an actual bad ray that missed to hit !
//		_HitPosition = _Position;	// Stay in place, even though we claim not to hit...
		_HitPosition = 255.0;
		_HitPosition.z = 0.0;
	}

	return false;
}

PS_OUT PS( VS_IN _In )
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	float3	VoxelCoordinatesFloat = NonEmptyVoxelsPositions.SampleLevel( NearestClamp, UV, 0.0 ).xyz;
	float3	VoxelCoordinates = floor( VoxelCoordinatesFloat * 255.0 );	// Unpack

	PS_OUT	Out;
	if ( VoxelCoordinates.x >= 255 && VoxelCoordinates.y >= 255 && VoxelCoordinates.z >= 255 )
	{	// Invalid voxel !
		Out.T0 = Out.T1 = Out.T2 = Out.T3 = Out.T4 = Out.T5 = Out.T6 = Out.T7 = 1.0;	// Store 255 as intersection coordinates (i.e. invalid)
		return Out;
	}

	float3	SourceUVW = (VoxelCoordinates + 0.5) * VolumeInvSize.xyz;

	// Sample normal at voxel position
	// Note the normal is in WORLD space but the 3D field textures are also aligned along WORLD
	//	coordinates so we can use the normal's direction components as 3D texture directions as well...
	float3	Normal = normalize( NormalField.SampleLevel( NearestClamp, SourceUVW, 0.0 ).xyz );

	// Build tangent space
	float3	OrthoAxis0 = float3( 1.0, 0.0, 0.0 );
	float3	OrthoAxis1 = float3( 0.0, 0.0, 1.0 );
	float3	OrthoAxis = abs(dot(Normal,OrthoAxis0)) < abs(dot(Normal,OrthoAxis1)) ? OrthoAxis0 : OrthoAxis1;
	float3	AxisZ = normalize( cross( Normal, OrthoAxis ) );
	float3	AxisX = cross( AxisZ, Normal );

	// Compute hits for the 10 rays
	float3	RayHits[10];
	for ( int RayIndex=0; RayIndex < 10; RayIndex++ )
	{
		// Transform ray into texture space
		float3	Ray = Rays[RayIndex].x * AxisX + Rays[RayIndex].y * Normal + Rays[RayIndex].z * AxisZ;
		Ray.y = -Ray.y;	// In texture space, +Y goes downward
		ComputeIntersection( VoxelCoordinates, Ray, Normal, RayHits[RayIndex] );

		// Normalize hit coordinate
		RayHits[RayIndex] /= 255.0;
	}

	// Pack rays into the 8 RTs
	Out.T0.xyz = RayHits[0];
	Out.T0.w = RayHits[1].x;
	Out.T1.xy = RayHits[1].yz;
	Out.T1.zw = RayHits[2].xy;
	Out.T2.x = RayHits[2].z;
	Out.T2.yzw = RayHits[3];

	Out.T3.xyz = RayHits[4];
	Out.T3.w = RayHits[5].x;
	Out.T4.xy = RayHits[5].yz;
	Out.T4.zw = RayHits[6].xy;
	Out.T5.x = RayHits[6].z;
	Out.T5.yzw = RayHits[7];

	Out.T6.xyz = RayHits[8];
	Out.T6.w = RayHits[9].x;
	Out.T7.xy = RayHits[9].yz;
	Out.T7.zw = 0.0;

	return Out;
}

// ===================================================================================
//
technique10 ComputeFormFactors
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
