// Displays grass using method 1
// There only are instance input data with this method, geometry is entirely GS-generated
//
#include "../Camera.fx"
#include "../DirectionalLighting.fx"
#include "ComputeWind.fx"

float			GrassSize = 0.1;
Texture2DArray	GrassTextures;

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
//	float	PrimitiveID		: PIPO;	// To verify pre-oriented buffers
	float3	TouffPos		: TEXCOORD2;
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
	float	DiffuseDot = saturate( dot( Normal, LightDirection ) );
	float3	DiffuseColorTop = DiffuseDot * LightColor.xyz;
	float3	DiffuseColorBottom = DiffuseDot * _In[0].Color * LightColor.xyz;

	// Compute alpha based on camera view of the tuft (viewing grass vertically will reveal the face-cam sprites)
//	float3	ToPosition = normalize( Camera2World[3].xyz - _In[0].TouffPos );
// 	float	Alpha = saturate( dot( Normal, ToPosition ) );
// 	Alpha *= Alpha;
// 	Alpha *= Alpha;
//	Alpha = 1.0 - Alpha;
	float	Alpha = 1.0;

	PS_IN	Out;
	Out.TouffPos = _In[0].TouffPos;
//	Out.PrimitiveID = _PrimitiveID / 65536.0;
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

float4 PS( PS_IN _In ) : SV_TARGET
{
// 	float3	_GrassPosition = _In.TouffPos;
// 
// 	// Handle wind gust
// 	float	GustPosition = GustTime - 10.0f;	// Gust position along wind direction vector
// 	float	GrassPositionAlongGustAxis = dot( _GrassPosition, WindDirection );
// 	float3	GrassPositionOnAxis = WindDirection * GrassPositionAlongGustAxis;
// 	float	GrassDistanceToGustAxis = length( _GrassPosition - GrassPositionOnAxis );				// Grass X value in the XY gust plane
// 			GrassPositionAlongGustAxis = GustPosition - GrassPositionAlongGustAxis;					// Grass Y value in the XY gust plane
// 
// 	float	XFactor = 0.25;
// 	float	GustFrontY = (XFactor * GrassDistanceToGustAxis * XFactor * GrassDistanceToGustAxis);	// Y value of the gust front in the XY gust plane
// 	float	DY = GustFrontY - GrassPositionAlongGustAxis;
// 	if ( DY < 0.0 )
// 		DY = 0.25 * DY;	// Get up slowly
// 	else
// 		DY = 2.5 * DY;	// Bend quickly
// 	float	Distance2Gust = 2 * exp( -DY*DY );		// Relative distance between the grass and the gust front
// 
// //	return	DY < 0.0 ? float4( Distance2Gust, 0, 0, 1 ) :  float4( 0, Distance2Gust, 0, 1 );
// // 	return	float4( GrassPositionOnAxis, 1 );
// // 	return	float4( GrassPositionAlongGustAxis, 0, 0, 1 );
// // 	return	float4( GustPosition, 0, 0, 1 );



//	return float4( _In.PrimitiveID.xxx, 1.0 );
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
