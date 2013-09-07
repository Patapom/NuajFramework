//////////////////////////////////////////////////////////////
// Default Shader that is use in replacement of a shader that
//	failed to compile.
//
// This shader works for most cases as the only required semantics
//	are a POSITION for vertices and the LOCAL2PROJ matrix which
//	most materials provide...
//
//////////////////////////////////////////////////////////////
//
float4x4	Local2Proj : LOCAL2PROJ;

struct VS_IN
{
	float3 pos : POSITION;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
};

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN) 0;
	
	output.pos = mul( float4( input.pos, 1.0 ), Local2Proj );
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	int	X = int(floor( 0.1 * input.pos.x ));
	int	Y = int(floor( 0.1 * input.pos.y ));
	return ((X + Y) & 1) < 0.5 ? float4( 1.0, 0.0, 0.0, 1.0 ) : float4( 1.0, 1.0, 1.0, 0.8 );
}

technique10 Default
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}