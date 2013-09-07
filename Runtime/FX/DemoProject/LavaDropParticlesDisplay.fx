// This shader animates and displays blob particles
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"
//#include "GBufferSupport.fx"

// ===================================================================================
// Particles Display
//
Texture2D	ParticlesPositions;

struct VS_IN
{
	float2	UV			: TEXCOORD0;
};

struct GS_IN
{
	float3	Position	: POSITION;
	float	Size		: SIZE;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
	float	Size		: SIZE;
	float	Velocity	: VELOCITY;
};

struct PS_OUT
{
	float4	Color		: SV_TARGET0;
	float	Depth		: SV_DEPTH;
};

GS_IN	VS_Display( VS_IN _In )
{
	float4	Value = ParticlesPositions.SampleLevel( NearestClamp, _In.UV, 0.0 );
	GS_IN	Out;
			Out.Position = Value.xyz;
			Out.Size = Value.w;

	return Out;
}

[maxvertexcount( 4 )]
void	GS_Display( point GS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	float3	SourcePosition = _In[0].Position;

	PS_IN	Out;
//	Out.Size = 0.05 * _In[0].Size;
	Out.Size = 0.04;
	Out.Velocity = _In[0].Size;

//	float	Elongation = 1.0 + 0.5 * _In[0].Size;
	float	Elongation = 1.0;

	// Compute camera plane
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	// Upper left vertex
	Out.Position = SourcePosition + Out.Size * (-Right+Elongation*Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = float2( -1.0, +1.0 );
	_OutStream.Append( Out );

	// Bottom left vertex
	Out.Position = SourcePosition + Out.Size * (-Right-Elongation*Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = float2( -1.0, -1.0 );
	_OutStream.Append( Out );

	// Upper right vertex
	Out.Position = SourcePosition + Out.Size * (+Right+Elongation*Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = float2( +1.0, +1.0 );
	_OutStream.Append( Out );

	// Bottom right vertex
	Out.Position = SourcePosition + Out.Size * (+Right-Elongation*Up);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.UV = float2( +1.0, -1.0 );
	_OutStream.Append( Out );

	_OutStream.RestartStrip();
}

PS_OUT	PS_Display( PS_IN _In )
{
	float	SqRadius = dot( _In.UV, _In.UV );
	clip( 1.0 - SqRadius );
	float	dZ = sqrt( 1.0 - SqRadius );

	float	Z = dot( _In.Position - Camera2World[3].xyz, Camera2World[2].xyz ) - _In.Size * dZ;

	PS_OUT	Out;
//	Out.Color = float4( _In.UV, dZ, Z );
	Out.Color = float4( _In.UV, _In.Velocity, Z );

	// Write depth
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf - Zn)
	Out.Depth = (1.0 - CameraData.z / Z) * Q;

	return Out;
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Display() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS_Display() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Display() ) );
	}
}
