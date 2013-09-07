// Displays the growing tree
//
#include "..\Camera.fx"
#include "..\DirectionalLighting.fx"

Texture2D	TextureLeaf;

SamplerState LinearClamp
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Direction	: NORMAL;
	float2	Size		: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
	float2	UV			: TEXCOORD0;
};

VS_IN VS( VS_IN _In )
{
	return _In;
}

[maxvertexcount( 4 )]
void GS( point VS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> _OutStream )
{
	float3	At = Camera2World[2].xyz;
	float3	PositionStart = _In[0].Position;
	float3	PositionEnd = PositionStart + _In[0].Size.y * _In[0].Direction;
	float3	Ortho = 0.5 * _In[0].Size.x * normalize( cross( At, _In[0].Direction ) );

	// Expand the point into a textured quad
	PS_IN	Out;
	Out.Position = mul( float4( PositionEnd - Ortho, 1.0 ), World2Proj );
	Out.UV = float2( 0.0, 0.0 );
	_OutStream.Append( Out );

	Out.Position = mul( float4( PositionStart - Ortho, 1.0 ), World2Proj );
	Out.UV = float2( 0.0, 1.0 );
	_OutStream.Append( Out );

	Out.Position = mul( float4( PositionEnd + Ortho, 1.0 ), World2Proj );
	Out.UV = float2( 1.0, 0.0 );
	_OutStream.Append( Out );

	Out.Position = mul( float4( PositionStart + Ortho, 1.0 ), World2Proj );
	Out.UV = float2( 1.0, 1.0 );
	_OutStream.Append( Out );
}

float4 PS( PS_IN _In ) : SV_TARGET0
{
	return TextureLeaf.Sample( LinearClamp, _In.UV );
}


// ===================================================================================
//
PS_IN VS2( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1.0 ), World2Proj );
	Out.UV = _In.Size;
	return Out;
}

float4	PS2( PS_IN _In ) : SV_TARGET0
{
	return float4( 0.3, 0.2, 0.1, 1.0 );
}

// ===================================================================================
//
technique10 DrawLeaf
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 DrawGroundPlane
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
