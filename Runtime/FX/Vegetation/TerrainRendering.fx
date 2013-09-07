// Renders terrain
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"

float3		TilePosition;
float		HeightFactor;
Texture2D	DiffuseTexture;
Texture2D	HeightTexture;

struct VS_IN
{
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	UV			: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	float3		WorldPosition = TilePosition + _In.Position;

	// Compute world position
	float		H = HeightTexture.SampleLevel( LinearClamp, _In.UV, 0 ).x;
				WorldPosition.y += 5.0 * H;

	// Compute world normal
	float2		dUV = float2( 1.0, 0.0 ) / 1024.0;
	float		HX0 = HeightTexture.SampleLevel( LinearClamp, _In.UV - dUV.xy, 0 ).x;
	float		HX1 = HeightTexture.SampleLevel( LinearClamp, _In.UV + dUV.xy, 0 ).x;
	float		HZ0 = HeightTexture.SampleLevel( LinearClamp, _In.UV - dUV.yx, 0 ).x;
	float		HZ1 = HeightTexture.SampleLevel( LinearClamp, _In.UV + dUV.yx, 0 ).x;

	float		HeightFactor = 15.0;
	float3		HdX = float3( 1.0, HeightFactor * (HX1 - HX0), 0.0 );
	float3		HdZ = float3( 0.0, HeightFactor * (HZ1 - HZ0), 1.0 );
	float3		WorldNormal = cross( HdZ, HdX );

// Bidouilling
//WorldPosition += 0.1 * WorldNormal / max( 1e-2, pow( 4.0 * WorldNormal.y, 0.1 ) );

	// Transform into camera space
	float4		CameraPosition = mul( float4( WorldPosition, 1.0 ), World2Camera );
	float3		CameraNormal = mul( float4( WorldNormal, 0.0 ), World2Camera ).xyz;

	PS_IN	Out;
	Out.__Position = mul( CameraPosition, Camera2Proj );
	Out.Position = CameraPosition.xyz;
	Out.Normal = CameraNormal;
	Out.UV = _In.UV;

	return Out;
}

PS_OUT	PS( PS_IN _In )
{
	float4	DiffuseColor = DiffuseTexture.Sample( LinearWrap, _In.UV );

	DiffuseColor *= 0.25;	// Terrain albedo is very low !

	return WriteDeferredMRT( DiffuseColor.xyz, 0.4 * DiffuseColor.xyz, 0.5, _In.Normal, _In.Position.z );
}


// ===================================================================================
//
technique10 RenderTerrain
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
