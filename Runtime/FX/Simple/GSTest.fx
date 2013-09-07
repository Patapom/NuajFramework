// This demonstrates the use of a Geometry Shader to generate Camera-oriented quads from single vertices
//
float4x4	World2Proj : WORLD2PROJ;
float4x4	Camera2World : CAMERA2WORLD;
float4x4	World2Camera : WORLD2CAMERA;

struct VS_IN
{
	float3 pos : POSITION;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
	float2 uv : TEXCOORD0;
};

// Pass-through
VS_IN VS( VS_IN _In )
{
	return _In;
}
// Second pass-through used for stream output content
PS_IN VS2( PS_IN _In )
{
	return _In;
}

// Generates 2 triangles from each vertex
[maxvertexcount( 4 )]
void GS( point VS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	PS_IN	Out;

	float3	SourcePosition = _In[0].pos;
	float4	SourceColor = _In[0].col;
	float	fParticleSize = 0.05f;

	// Compute camera plane
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	// Upper left vertex
	Out.pos = mul( float4( SourcePosition + fParticleSize * (-Right+Up), 1 ), World2Proj );
	Out.col = SourceColor;
	Out.uv = float2( -1.0, +1.0 );
	_OutStream.Append( Out );

	// Bottom left vertex
	Out.pos = mul( float4( SourcePosition + fParticleSize * (-Right-Up), 1 ), World2Proj );
	Out.col = SourceColor;
	Out.uv = float2( -1.0, -1.0 );
	_OutStream.Append( Out );

	// Upper right vertex
	Out.pos = mul( float4( SourcePosition + fParticleSize * (+Right+Up), 1 ), World2Proj );
	Out.col = SourceColor;
	Out.uv = float2( +1.0, +1.0 );
	_OutStream.Append( Out );

	// Bottom right vertex
	Out.pos = mul( float4( SourcePosition + fParticleSize * (+Right-Up), 1 ), World2Proj );
	Out.col = SourceColor;
	Out.uv = float2( +1.0, -1.0 );
	_OutStream.Append( Out );

	_OutStream.RestartStrip();
}

float4 PS( PS_IN _In ) : SV_Target
{
	float	fDistanceFactor = 0.05 * (1.0 - length( _In.uv ));

	return float4( _In.col.rgb * fDistanceFactor, 1 );
}

// ===================================================================================
// Direct rendering VS -> GS -> PS
technique10 RenderDirect
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

// ===================================================================================
// Deferred rendering in 2 passes : 1) GS -> StreamOutput then 2) StreamOutput -> PS
technique10 RenderToStreamOutput
{
	pass
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
 		SetGeometryShader( ConstructGSWithSO( CompileShader( gs_4_0, GS() ),
 							"SV_POSITION.xyzw; COLOR.xyzw; TEXCOORD0.xy" ) );
		SetPixelShader( 0 );
	}
}

technique10 RenderFromStreamOutput
{
	pass 
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
