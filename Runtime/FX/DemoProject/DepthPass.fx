// Performs the depth pass at full resolution
//
#include "../Camera.fx"

float4x4	Local2World : LOCAL2WORLD;

struct VS_IN
{
	float3	Position		: POSITION;	// Depth-pass renderables need only declare a position
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
};

struct PS_IN2
{
	float4	__Position		: SV_POSITION;
	float	Depth			: Depth;
};

PS_IN VS( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World );

	PS_IN	Out;
	Out.Position = mul( WorldPosition, World2Proj );

	return Out;
}

// Doesn't do anything but we must declare a PS otherwise this is super slow ! (it must come back to the default pipeline or something I suppose)
void PS( PS_IN _In )
{
}


// ===================================================================================
//
PS_IN2 VS2( VS_IN _In )
{
	float4	WorldPosition = mul( float4( _In.Position, 1.0 ), Local2World );
	float4	CameraPosition = mul( WorldPosition, World2Camera );

	PS_IN2	Out;
	Out.__Position = mul( WorldPosition, World2Proj );
// 	Out.Depth = Out.__Position.z / Out.__Position.w;
	Out.Depth = CameraPosition.z;

	return Out;
}

float PS2( PS_IN2 _In ) : SV_TARGET
{
	return _In.Depth;
//	return 0.5;
}

// ===================================================================================
//
technique10 DrawDepth
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 DrawDepthMSAA
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
