// This shader displays particles in the cloud density map to clear out the fog
// I'm using DX10 Geometry Shaders to expand particles into quads but there's really no use
//
#include "../Camera.fx"
#include "../Samplers.fx"

static const float	PI = 3.14159265358979;

float2	BufferInvSize;
float3	PositionRadius;	// 2D Position (XY) + Radius (Z)
float3	Color;


struct VS_IN
{
	float3	Position	: POSITION;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
};


VS_IN	VS( VS_IN _In ) { return _In; }

float4	BuildPosition( float2 _UV )
{
	return float4( 2.0 * _UV.x - 1.0, 1.0 - 2.0 * _UV.y, 0.5, 1.0 );
}

// Generate a single quad strip from the point
[maxvertexcount(4)]
void	GS( point VS_IN _In[1], inout TriangleStream<PS_IN> _Stream )
{
	PS_IN	Out;
	Out.Position = BuildPosition( PositionRadius.xy + PositionRadius.z * float2( -1.0, -1.0 ) ); _Stream.Append( Out );
	Out.Position = BuildPosition( PositionRadius.xy + PositionRadius.z * float2( -1.0, +1.0 ) ); _Stream.Append( Out );
	Out.Position = BuildPosition( PositionRadius.xy + PositionRadius.z * float2( +1.0, -1.0 ) ); _Stream.Append( Out );
	Out.Position = BuildPosition( PositionRadius.xy + PositionRadius.z * float2( +1.0, +1.0 ) ); _Stream.Append( Out );
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float2	ToCenter = UV - PositionRadius.xy;
	float	SqDistance = dot( ToCenter, ToCenter );
	float	t = 1.0 - saturate( SqDistance / (PositionRadius.z*PositionRadius.z) );
	return float4( t, t * Color );
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
