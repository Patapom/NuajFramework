// Displays grass using method 3
// No instance data are provided except the SV-generated InstanceID
//
#include "../Camera.fx"
#include "../DirectionalLighting.fx"
#include "ComputeWind.fx"

float			Time = 0.0;
float			GrassSize = 0.1;
float3			WindPosition0 = float3( 0.0, 0.0, 0.0 );
float3			WindPosition1 = float3( 0.0, 0.0, 0.0 );
float3			WindDirection = float3( 1.0, 0.0, 0.0 );
Texture2DArray	GrassTextures;
Texture2D		GrassPositionTexture;
Texture2D		GrassColorTexture;

struct VS_IN
{
	uint	InstanceID		: SV_INSTANCEID;
};

struct GS_IN
{
	float3	Position		: POSITION;
	float3	Color			: COLOR;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float4	Color			: COLOR;
	float2	UV				: TEXCOORD1;
};

// GS_IN VS( VS_IN _In )
// {
// 	// Fetch position & color from textures
// 	float2	UV = float2( (_In.InstanceID & 0xFF) / 255.0, (_In.InstanceID >> 8) / 255.0 );
// 
// 	GS_IN	Out;
// 	Out.Position = GrassPositionTexture.SampleLevel( NearestClamp, UV, 0 ).xyz;
// 	Out.Color = GrassColorTexture.SampleLevel( NearestClamp, UV, 0 ).xyz;
// 
//	return Out;
// }

VS_IN VS( VS_IN _In )
{
	return _In;
}

[maxvertexcount( 4 )]
void GS( point VS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> _OutStream )
{
	// Fetch position & color from textures
	float2	UV = float2( (_PrimitiveID & 0xFF) / 255.0, (_PrimitiveID >> 8) / 255.0 );
	float3	Position = GrassPositionTexture.SampleLevel( NearestClamp, UV, 0 ).xyz;
	float3	Color = GrassColorTexture.SampleLevel( NearestClamp, UV, 0 ).xyz;

// 	float3	Position = _In[0].Position;
// 	float3	Color = _In[0].Color;

	// Compute world position
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	// Disturb by wind
	float3	WindDisplacement = ComputeGrassWindDisplacement( Position );

	// Compute diffuse lighting
	float3	Normal = float3( 0.0, 1.0, 0.0 );	// Should better follow terrain orientation
	float	DiffuseDot = saturate( dot( Normal, LightDirection ) );
	float3	DiffuseColorTop = DiffuseDot * LightColor.xyz;
	float3	DiffuseColorBottom = DiffuseDot * Color * LightColor.xyz;

	// Compute alpha based on camera view of the tuft (viewing grass vertically will reveal the face-cam sprites)
//	float3	ToPosition = normalize( Camera2World[3].xyz - Position );
// 	float	Alpha = saturate( dot( Normal, ToPosition ) );
// 	Alpha *= Alpha;
// 	Alpha *= Alpha;
//	Alpha = 1.0 - Alpha;
	float	Alpha = 1.0;

	PS_IN	Out;
	Out.Position = mul( float4( Position + GrassSize * (-Right+2.0*Up) + WindDisplacement, 1.0 ), World2Proj );
	Out.UV = float2( 0.0, 0.0 );
	Out.Color = float4( DiffuseColorTop, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( Position + GrassSize * (-Right), 1.0 ), World2Proj );
	Out.UV = float2( 0.0, 1.0 );
	Out.Color = float4( DiffuseColorBottom, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( Position + GrassSize * (+Right+2.0*Up) + WindDisplacement, 1.0 ), World2Proj );
	Out.UV = float2( 1.0, 0.0 );
	Out.Color = float4( DiffuseColorTop, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( Position + GrassSize * (+Right), 1.0 ), World2Proj );
	Out.UV = float2( 1.0, 1.0 );
	Out.Color = float4( DiffuseColorBottom, Alpha );
	_OutStream.Append( Out );

	_OutStream.RestartStrip();
}

float4 PS( PS_IN _In ) : SV_TARGET
{
	return _In.Color * GrassTextures.Sample( LinearWrap, float3( _In.UV, 0.0 ) );
}


// ===================================================================================
//
technique10 DrawGrass
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
