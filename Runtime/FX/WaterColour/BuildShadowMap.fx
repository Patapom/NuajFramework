// Builds the cascaded exponential shadow map
// The geometry is output in multiple "split zones" using the geometry shader
// If a triangle lies in a split zone, it writes its depth as exp( c * z ) with
//	'c' the exponent constant as defined in http://citeseerx.ist.psu.edu/viewdoc/download;jsessionid=598F8B3440C24E9121D37F9773F02F7A?doi=10.1.1.146.177&rep=rep1&type=pdf
//
#define SPLITS_COUNT	1

#include "../Samplers.fx"

float	ShadowExponent = 80.0;

cbuffer	PerLight	{ float4x4 World2LightProj[SPLITS_COUNT]; }
cbuffer PerObject	{ float4x4 Local2World; }

struct VS_IN
{
	float3	Position	: POSITION;		// Shadow-pass renderables need only declare a LOCAL position
};

struct GS_IN
{
	float3	Position	: POSITION;		// WORLD position
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;	// Projected position in light space
	float4	Position	: POSITION;		// Same
	uint	RTIndex		: SV_RENDERTARGETARRAYINDEX;
};

GS_IN VS( VS_IN _In )
{
	GS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1.0 ), Local2World ).xyz;
	return Out;
}

[maxvertexcount(3*SPLITS_COUNT)]
void GS( triangle GS_IN _In[3], inout TriangleStream<PS_IN> Stream )
{
	PS_IN	Out;
	for ( int SplitIndex=0; SplitIndex < SPLITS_COUNT; SplitIndex++ )
	{
		// Project the 3 vertices in that slice
		float4x4	CurrentWorld2LightProj = World2LightProj[SplitIndex];
		float4		LightProj[3] = 
		{
			mul( float4( _In[0].Position, 1.0 ), CurrentWorld2LightProj ),
			mul( float4( _In[1].Position, 1.0 ), CurrentWorld2LightProj ),
			mul( float4( _In[2].Position, 1.0 ), CurrentWorld2LightProj ),
		};
		LightProj[0] /= LightProj[0].w;
		LightProj[1] /= LightProj[1].w;
		LightProj[2] /= LightProj[2].w;

 		// Perform basic clipping
		if ( LightProj[0].x > +1.0 && LightProj[1].x > +1.0 && LightProj[2].x > +1.0 )	continue;
		if ( LightProj[0].x < -1.0 && LightProj[1].x < -1.0 && LightProj[2].x < -1.0 )	continue;
		if ( LightProj[0].y > +1.0 && LightProj[1].y > +1.0 && LightProj[2].y > +1.0 )	continue;
		if ( LightProj[0].y < -1.0 && LightProj[1].y < -1.0 && LightProj[2].y < -1.0 )	continue;
		if ( LightProj[0].z > +1.0 && LightProj[1].z > +1.0 && LightProj[2].z > +1.0 )	continue;
		if ( LightProj[0].z < -1.0 && LightProj[1].z < -1.0 && LightProj[2].z < -1.0 )	continue;

		// Output triangle
		Out.RTIndex = SplitIndex;
		Out.__Position = Out.Position = LightProj[0];
		Stream.Append( Out );
		Out.__Position = Out.Position = LightProj[1];
		Stream.Append( Out );
		Out.__Position = Out.Position = LightProj[2];
		Stream.Append( Out );
		Stream.RestartStrip();
	}
}

// Writes the exponential shadow depth
float	PS( PS_IN _In ) : SV_TARGET
{
//	return _In.Position.z;
	return exp( ShadowExponent * _In.Position.z );
}

// ===================================================================================
//
static const float	VIEWPORT_SIZE = 128;

Texture2DArray	DEBUGShadowMap;
float			DEBUGSplitIndex;

struct VS_IN2
{
	float4	__Position : SV_POSITION;
};

VS_IN2	VS2( VS_IN2 _In )	{ return _In; }
float4	PS2( VS_IN2 _In ) : SV_TARGET
{
	float2	UV = float2( _In.__Position.x / VIEWPORT_SIZE, _In.__Position.y / VIEWPORT_SIZE );
//	return float4( UV, 0, 1 );
//	return DEBUGShadowMap.SampleLevel( LinearClamp, float3( UV, DEBUGSplitIndex ), 0 ).xxxx;
//	return DEBUGShadowMap.SampleLevel( LinearClamp, float3( UV, 1 ), 0 ).xxxx;
	return 1.0/ShadowExponent * log( DEBUGShadowMap.SampleLevel( LinearClamp, float3( UV, 0 ), 0 ).xxxx );
}

// ===================================================================================
//
technique10 BuildShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 DebugShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
