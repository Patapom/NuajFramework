// Displays grass using method 2
// There only are instance input data with this method, geometry is entirely GS-generated
// Basically same as method 1 except I'm using half vectors everywhere instead of floats
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

struct VS_IN
{
	half	PositionX		: POSITION0;
	half	PositionY		: POSITION1;
	half	PositionZ		: POSITION2;
	half	ColorR			: COLOR0;
	half	ColorG			: COLOR1;
	half	ColorB			: COLOR2;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	half4	Color			: COLOR;
	half2	UV				: TEXCOORD1;
};

VS_IN VS( VS_IN _In )
{
	return _In;
}

[maxvertexcount( 4 )]
void GS( point VS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	half3	Position = half3( _In[0].PositionX, _In[0].PositionY, _In[0].PositionZ );
	half3	Color = half3( _In[0].ColorR, _In[0].ColorG, _In[0].ColorB );
//	half3	Color = half3( 1.0, 1.0, 1.0 );

	// Compute world position
	half3	Right = half3( Camera2World[0].xyz );
	half3	Up = half3( Camera2World[1].xyz );

	// Disturb by wind
	half3	WindDisplacement = half2( ComputeGrassWindDisplacement( _In[0].TouffPos ) );

	// Compute diffuse lighting
	half3	Normal = half3( 0.0, 1.0, 0.0 );	// Should better follow terrain orientation
	half	DiffuseDot = half( saturate( dot( Normal, LightDirection ) ) );
	half3	DiffuseColorTop = half3( DiffuseDot * LightColor.xyz );
	half3	DiffuseColorBottom = half3( DiffuseDot * Color * LightColor.xyz );

	// Compute alpha based on camera view of the tuft (viewing grass vertically will reveal the face-cam sprites)
//	half3	ToPosition = normalize( Camera2World[3].xyz - Position );
// 	half	Alpha = saturate( dot( Normal, ToPosition ) );
// 	Alpha *= Alpha;
// 	Alpha *= Alpha;
//	Alpha = 1.0 - Alpha;
	half	Alpha = 1.0;

	PS_IN	Out;
	Out.Position = mul( float4( Position + GrassSize * (-Right+2.0*Up) + WindDisplacement, 1.0 ), World2Proj );
	Out.UV = half2( 0.0, 0.0 );
	Out.Color = half4( DiffuseColorTop, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( Position + GrassSize * (-Right), 1.0 ), World2Proj );
	Out.UV = half2( 0.0, 1.0 );
	Out.Color = half4( DiffuseColorBottom, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( Position + GrassSize * (+Right+2.0*Up) + WindDisplacement, 1.0 ), World2Proj );
	Out.UV = half2( 1.0, 0.0 );
	Out.Color = half4( DiffuseColorTop, Alpha );
	_OutStream.Append( Out );

	Out.Position = mul( float4( Position + GrassSize * (+Right), 1.0 ), World2Proj );
	Out.UV = half2( 1.0, 1.0 );
	Out.Color = half4( DiffuseColorBottom, Alpha );
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
