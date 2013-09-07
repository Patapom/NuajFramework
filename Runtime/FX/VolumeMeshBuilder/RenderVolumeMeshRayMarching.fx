// Renders the volume mesh by ray-marching
//
#include "../Camera.Fx"
#include "../Samplers.fx"

//static const int	MAX_STEPS_COUNT = 96;
static const int	MAX_STEPS_COUNT = 192;
static const float	MIN_STEP_SIZE = 4e-2;
static const float	HIT_EPSILON = 1e-3;
static const float	INFINITY = 1e6;

SamplerState LinearBorderDiffuse
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Border;
	AddressV = Border;
	AddressW = Border;
	BorderColor = float4( 0, 0, 0, 0.0 );
};

SamplerState LinearBorderDistance
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Border;
	AddressV = Border;
	AddressW = Border;
	BorderColor = float4( 0, 0, 0, 10.0 );
//	BorderColor = float4( 0, 0, 0, 1e-2 );
};

float4		BufferInvSize;
float3		VolumeSize;
float3		VolumeInvSize;
Texture3D	DiffuseField;
Texture3D	NormalField;
Texture3D	DistanceField;

bool		PreMultiplyByAlpha;

struct VS_IN
{
	float4	Position	: SV_POSITION;
	uint	SliceIndex	: SV_INSTANCEID;
};

// ===================================================================================
// Render using ray-marching
VS_IN VS( VS_IN _In ) { return _In; }

float	VoxelSize;	// Size of a volume element in WORLD space
float	VoxelInvSize;

float3	World2Volume( float3 _WorldPosition, float3 _TilingSize )
{
// 	return float3( _WorldPosition.x, _TilingSize.y - _WorldPosition.y, _WorldPosition.z ) * VoxelInvSize * VolumeInvSize
// 		+ float3( 0, 0, 0.5 * VolumeInvSize.z );	// Add a little bias in Z
	return (float3( _WorldPosition.x, _TilingSize.y - _WorldPosition.y, _WorldPosition.z ) * VoxelInvSize + 0.5) * VolumeInvSize;
}

float	ComputeMipLevel( float _Distance2Camera )
{
	float	PixelSizeAt1 = CameraData.x * 2.0 * BufferInvSize.y;							// tan(FOV/2) / (0.5*Height)
//			PixelSizeAt1 *= 4.0;	// Arbitrary decrease of the LOD
	float	PixelSize = _Distance2Camera * PixelSizeAt1;									// Pixel size at current distance
	return max( 0.0, log2( PixelSize / VoxelSize ) );
}

// Applies the technique described in "Mipmapping normal maps" by Michael Toksvig (nVidia 2004)
// A normal whose length is not 1 can be considered many things, for example for a length of 0
//	that could mean 2 normals of equal magnitude in opposite directions but Toksvig prefers to
//	imagine many normals spread in a gaussian lobe. The least the magnitude, the larger the lobe.
// A magnitude of 1 turns back into a singular directional lobe and we're back on our feet with
//	the original formula.
//
float	GaussianSpecular( float3 _HalfVector, float3 _Normal, float _SpecularPower )
{
	float	NormalLength = length( _Normal );
	float	Sigma = sqrt( (1.0 - NormalLength) / NormalLength );
	float	Ft = NormalLength / (NormalLength + _SpecularPower * (1.0 - NormalLength));	// "Toksvig factor" to attenuate spec power due to correction

	// Now, use the standard Phong exponentiation with a reduced specular power to account for gaussian repartition
	float	DotSpec = pow( max( 1e-3, dot( _Normal / NormalLength, _HalfVector ) ), Ft * _SpecularPower );

	// And return a scaled down result to compensate for the widened highlight
	return (1.0 + Ft * _SpecularPower) / (1.0 + _SpecularPower) * DotSpec;
}

float4 PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize.xy;
	float3	View = float3( CameraData.x * CameraData.y * (2.0 * UV.x - 1.0), CameraData.x * (1.0 - 2.0f * UV.y), 1.0 );
	View = mul( float4( View, 0.0 ), Camera2World ).xyz;
	View = normalize( View );

	float3	Position = Camera2World[3].xyz;

//	float3	TilingSize = 1.4 * VoxelSize * VolumeSize;
	float3	TilingSize = 1.0 * VoxelSize * VolumeSize;
	Position = ((Position % TilingSize) + TilingSize) % TilingSize;	// Make it tile

	// Ray-march accross space
	float	MarchedDistance = 0.0;
	float4	DirectionDistance = 0.0;
	float4	PreviousDirectionDistance = 0.0;
	for ( int StepIndex=0; StepIndex < MAX_STEPS_COUNT; StepIndex++ )
	{
		PreviousDirectionDistance = DirectionDistance;

		float	MipLevel = ComputeMipLevel( MarchedDistance );
		float3	UVW = World2Volume( Position, TilingSize );

		// Sample distance
//		DirectionDistance = DistanceField.SampleLevel( LinearBorderDistance, UVW, MipLevel );
//		DirectionDistance = DistanceField.SampleLevel( LinearWrap, UVW, MipLevel );
		DirectionDistance = DistanceField.SampleLevel( LinearWrap, UVW, 0.0 );

		// Check if we're crossing a 0-distance barrier
		if ( (DirectionDistance.w < HIT_EPSILON && PreviousDirectionDistance.w > HIT_EPSILON) ||
			 (DirectionDistance.w > -HIT_EPSILON && PreviousDirectionDistance.w < -HIT_EPSILON) )
		{	// We have a hit !

			// Go to exact hit
			Position += DirectionDistance.w * View;
			UVW = World2Volume( Position, TilingSize );



			float4	DiffuseAlpha = DiffuseField.SampleLevel( LinearClamp, UVW, MipLevel );
//			float	Opacity = DiffuseField.SampleLevel( LinearBorderDiffuse, UVW, MipLevel ).w;
			if ( DiffuseAlpha.w > 1e-3 )
			{	// We definitely have a hit !
//				UVW += DirectionDistance.xyz * VolumeInvSize;

//				return 1.0 - 0.25 * MipLevel;

//MipLevel = 0.0;

				DiffuseAlpha = DiffuseField.SampleLevel( LinearClamp, UVW, MipLevel );
				float3	Normal = NormalField.SampleLevel( LinearClamp, UVW, MipLevel ).xyz;

				// Un-premultiply by alpha
				if ( PreMultiplyByAlpha )
				{
					DiffuseAlpha.xyz /= DiffuseAlpha.w;
					Normal.xyz /= DiffuseAlpha.w;
				}

//				return DiffuseAlpha;
//				return float4( Normal, 1.0 );
//				return float4( abs(Normal), 1.0 );

				// Pipo lighting
//				DiffuseAlpha = DiffuseField.SampleLevel( LinearClamp, UVW, MipLevel );
				float3	ToLight = 0.57735026918962576450914878050196;
//				float3	ToLight = float3( 0, 1, 0 );

				// Pipo specular
				// Here, normals may be largely attenuated due to smoothing so we use the technique
				//	described in "Mipmapping normal maps" by Michael Toksvig (nVidia 2004)
				float3	Half = View + ToLight;
				float	DotSpecular = GaussianSpecular( Half, Normal, 4.0 );

				// Pipo diffuse
				float	InvNormalLength = 1.0 / length( Normal );
				Normal = Normal * InvNormalLength;
				float	DotDiffuse = saturate( 0.2 + 0.8 * dot( Normal, ToLight ) );

//				return InvNormalLength * DiffuseAlpha * (0.05 + DotDiffuse + DotSpecular);
				return 1.0 * DiffuseAlpha * (0.05 + DotDiffuse + DotSpecular);
			}

			// That was a false hit...
		}

		float	WorldDistance = abs(DirectionDistance.w) * VoxelSize;

// 		// Perform acceleration at edges
// 		float	PhaseThreshold = 0.01;
// 		float3	WorldDirection = normalize( float3( DirectionDistance.x, -DirectionDistance.y, DirectionDistance.z ) );
// 		float	Phase = abs( dot( View, WorldDirection ) );
// 		if ( Phase < PhaseThreshold )
// 		{	// If the direction to the nearest point is almost orthogonal to the view direction then we're approaching an edge
// 			// Let's decide to accelerate rather than decelerate
// 			WorldDistance *= lerp( 10.0, 1.0, Phase / PhaseThreshold );
// 		}

		// March forward
		float	MarchStep = max( MIN_STEP_SIZE, WorldDistance );
		Position += MarchStep * View;
		Position = (Position + TilingSize) % TilingSize;
		MarchedDistance += WorldDistance;
	}

//	return float4( 0.5, 0, 0, 0 );	// No hit...
	return 0.0;	// No hit...
}

// ===================================================================================
//
float3	TextureSize;

Texture2DArray	DebugStack;
Texture2DArray	DebugStackNormal;
float			DebugSlicesCount;

struct PS_IN
{
	float4	Position	: SV_POSITION;
	float3	UVW			: TEXCOORD0;
	uint	SliceIndex	: SV_INSTANCEID;
};

PS_IN VS_Slices( VS_IN _In )
{
	float3	ProjPosition = float3( _In.Position.xy, 2.0 * _In.SliceIndex / TextureSize.z - 1.0 );
	float3	WorldPosition = 0.5 * ProjPosition * VolumeSize;

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1.0 ), World2Proj );

	Out.UVW.x = 0.5 * (1.0 + _In.Position.x) * (TextureSize.x-1) / TextureSize.x;
	Out.UVW.y = 0.5 * (1.0 - _In.Position.y) * (TextureSize.y-1) / TextureSize.y;
	Out.UVW.z = _In.SliceIndex;// + 0.5; <= 0.5 is wrong !

	Out.SliceIndex = _In.SliceIndex;
	return Out;
}

float4 PS_Slices( PS_IN _In ) : SV_TARGET0
{
// DEBUG SLICES (use blend mode here)
// float4	StackColor = DebugStack.SampleLevel( NearestClamp, float3( _In.UVW.xy, _In.UVW.z ), 0.0 );
// clip( StackColor.w - 1e-2 );
// //return float4( StackColor.xyz / StackColor.w, StackColor.w );
// return float4( DebugStackNormal.SampleLevel( NearestClamp, float3( _In.UVW.xy, _In.UVW.z ), 0.0 ).xyz, StackColor.w );
// DEBUG SLICES

	_In.UVW.z = (_In.UVW.z + 0.5) / TextureSize.z;	// Little bias on Z
//	_In.UVW.z /= TextureSize.z;	// Without the bias, it gives shit

// DEBUG DIFFUSE/NORMAL (use blend mode here)
//  	float4	DiffuseAlpha = DiffuseField.SampleLevel( LinearClamp, _In.UVW, 0.0 );
//  	clip( DiffuseAlpha.w - 1e-2 );
// //	return float4( DiffuseAlpha.xyz / DiffuseAlpha.w, DiffuseAlpha.w );
// 	return float4( NormalField.SampleLevel( LinearClamp, _In.UVW, 0.0 ).xyz / DiffuseAlpha.w, DiffuseAlpha.w );
// DEBUG DIFFUSE/NORMAL


// DEBUG DISTANCE FIELD (use additive mode here)
 	float4	Distance = DistanceField.SampleLevel( LinearClamp, _In.UVW, 0.0 );
// 	return 0.001 * Distance;
// 	return 0.001 * Distance.w;
	return 10.0 * 0.001 * (Distance.w > 0.0 ? float4( Distance.w, 0, 0, 0 ) : float4( 0, 0, -100.0 * Distance.w, 0 ));
//	return 1.0 * (Distance.w > 0.0 ? float4( 0, 0, 0, 0 ) : float4( 0, 0, -Distance.w, 0 ));	// Only visualize inside values

	float3	dUVW = Distance.xyz / TextureSize;
	_In.UVW += dUVW;
//	return 0.01 * DistanceField.SampleLevel( LinearWrap, _In.UVW, 0.0 ).w;
	return 0.01 * abs( DistanceField.SampleLevel( LinearWrap, _In.UVW, 0.0 ).w );
// 	float	SignedDistance = DistanceField.SampleLevel( LinearWrap, _In.UVW, 0.0 ).w;
// 	return 0.01 * (SignedDistance > 0.0 ? float4( SignedDistance, 0, 0, 0 ) : float4( 0, 0, -SignedDistance, 0 ));
//	return 0.01 * DiffuseField.SampleLevel( LinearClamp, _In.UVW, 0.0 );
// DEBUG DISTANCE FIELD

	// Test diffuse without blending
	float4	Value = DiffuseField.SampleLevel( LinearClamp, _In.UVW, 0.0 );
	clip( Value.w - 0.5 );

	return 0.04 * Value;
}

// ===================================================================================
//
technique10 RenderVolumeMesh
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 RenderVolumeMesh_Slices
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Slices() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Slices() ) );
	}
}
