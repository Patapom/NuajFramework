// Renders grass
// There only are instance input data with this method, geometry is entirely GS-generated
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "ComputeWind.fx"
#include "DeferredSupport.fx"

float		GrassSize = 0.1;
Texture2D	GrassTexture;

struct VS_IN
{
	float3	TouffPos		: POSITION;
	float3	Color			: COLOR;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float4	Color			: COLOR;
	float2	UV				: TEXCOORD1;
	float4	NormalDepth		: TEXCOORD2;
};

VS_IN VS( VS_IN _In )
{
	return _In;
}

[maxvertexcount( 4 )]
void GS( point VS_IN _In[1], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> _OutStream )
{
	// Compute world position
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	// Disturb by wind
	float3	WindDisplacement = ComputeGrassWindDisplacement( _In[0].TouffPos );

	// Compute diffuse lighting
	float3	Normal = float3( 0.0, 1.0, 0.0 );	// Should better follow terrain orientation
	float3	DiffuseColorTop = float3( 1.0, 1.0, 1.0 );
	float3	DiffuseColorBottom = _In[0].Color;

	// Compute alpha based on camera view of the tuft (viewing grass vertically will reveal the face-cam sprites)
//	float3	ToPosition = normalize( Camera2World[3].xyz - _In[0].TouffPos );
// 	float	Alpha = saturate( dot( Normal, ToPosition ) );
// 	Alpha *= Alpha;
// 	Alpha *= Alpha;
//	Alpha = 1.0 - Alpha;
	float	Alpha = 1.0;

	float3	NormalCamera = mul( float4( Normal, 0.0 ), World2Camera ).xyz;
	float	Depth = mul( float4( _In[0].TouffPos + GrassSize * Up + 0.5 * WindDisplacement, 1.0 ), World2Camera ).z;

	PS_IN	Out;
	Out.NormalDepth = float4( NormalCamera, Depth );
	Out.Position = mul( float4( _In[0].TouffPos + GrassSize * (-Right+2.0*Up) + WindDisplacement, 1.0 ), World2Proj );
	Out.UV = float2( 0.0, 0.0 );
	Out.Color = float4( DiffuseColorTop, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( _In[0].TouffPos + GrassSize * (-Right), 1.0 ), World2Proj );
	Out.UV = float2( 0.0, 1.0 );
	Out.Color = float4( DiffuseColorBottom, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( _In[0].TouffPos + GrassSize * (+Right+2.0*Up) + WindDisplacement, 1.0 ), World2Proj );
	Out.UV = float2( 1.0, 0.0 );
	Out.Color = float4( DiffuseColorTop, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( _In[0].TouffPos + GrassSize * (+Right), 1.0 ), World2Proj );
	Out.UV = float2( 1.0, 1.0 );
	Out.Color = float4( DiffuseColorBottom, Alpha );
	_OutStream.Append( Out );
	
	_OutStream.RestartStrip();
}

PS_OUT PS( PS_IN _In )
{
	float4	Albedo = _In.Color * GrassTexture.Sample( LinearWrap, _In.UV );
	clip( Albedo.a - 0.5 );

	Albedo *= 0.25;	// Albedo for green grass (from http://en.wikipedia.org/wiki/Albedo)

	return WriteDeferredMRT( Albedo.xyz, 0.5.xxx, 2.0, _In.NormalDepth.xyz, _In.NormalDepth.w );
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
