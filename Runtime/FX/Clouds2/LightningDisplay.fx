// Lightning display
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../DirectionalLighting.fx"
#include "CloudSupport.fx"

float	Scale;	// Lightning scale

struct VS_IN
{
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float	U			: U;
	float	Intensity	: INTENSITY;
	float	Distance	: DISTANCE;
};

// ===================================================================================
// Display lightning
VS_IN	VS( VS_IN _In ) { return _In; }

[maxvertexcount(4)]
void GS( lineadj VS_IN _In[4], inout TriangleStream<PS_IN> _Stream )
{
	float3	At = Camera2World[2].xyz;

	float3	PrevPosition = LightningPosition + _In[0].Position * Scale;
	float3	Position0 = LightningPosition + _In[1].Position * Scale;
	float3	Position1 = LightningPosition + _In[2].Position * Scale;
	float3	NextPosition = LightningPosition + _In[3].Position * Scale;

	float3	DirectionPrev = Position0 - PrevPosition;
	float3	Direction = Position1 - Position0;
	float3	DirectionNext = NextPosition - Position1;

	float	Intensity0 = _In[1].UV.x;
	float	Intensity1 = _In[2].UV.x;
	float	Distance0 = _In[1].UV.y;
	float	Distance1 = _In[2].UV.y;

	float3	OrthoPrev = cross( DirectionPrev, At );
	float3	Ortho = cross( Direction, At );
	float3	OrthoNext = cross( DirectionNext, At );
	float3	Ortho0 = normalize( OrthoPrev + Ortho );
	float3	Ortho1 = normalize( OrthoNext + Ortho );

	float	Width0 = 0.02 * lerp( 0.1, 1.0, Intensity0 ) * Scale;
	float	Width1 = 0.02 * lerp( 0.1, 1.0, Intensity1 ) * Scale;

	PS_IN	Out;
	Out.Position = Position0 - Ortho0 * Width0;
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.U = -1.0;
	Out.Intensity = Intensity0;
	Out.Distance = Distance0;
	_Stream.Append( Out );

	Out.Position = Position1 - Ortho1 * Width1;
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.Intensity = Intensity1;
	Out.Distance = Distance1;
	_Stream.Append( Out );

	Out.Position = Position0 + Ortho0 * Width0;
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.U = +1.0;
	Out.Intensity = Intensity0;
	Out.Distance = Distance0;
	_Stream.Append( Out );

	Out.Position = Position1 + Ortho1 * Width1;
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Out.Intensity = Intensity1;
	Out.Distance = Distance1;
	_Stream.Append( Out );
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
// 	float3	Pos = Camera2World[3].xyz;
// 	float3	View = _In.Position - Pos;
// 	float	Distance = length( View );
// 	View /= Distance;
// 
// 	// Standard phong
// 	_In.Normal.y *= 0.01;
// 
// 	float3	Normal = normalize( _In.Normal );
// 	float3	DiffuseFactor = 0.05 + 0.95 * saturate( dot( Normal, LightDirection ) );
// 
// 	float2	GroundUV = 0.01 * _In.Position.xz;
// 
// 	float	DetailImportance = 1.0 - saturate( (Distance - 6.0) / 20.0 );
// 	float	DetailStrength = 1.0 * DetailImportance;
// 	float3	TextureColor = (1.0 - DetailStrength) * GroundTexture.Sample( LinearWrap, GroundUV ).xyz;
// 			TextureColor += DetailStrength * GroundTexture.Sample( LinearWrap, 4.0 * GroundUV ).xyz;
// 
// 	TextureColor *= 0.1;	// Pipo albedo
// 
// 	float3	SunLight = ComputeIncomingLight( _In.Position ) * DiffuseFactor;
// 	float3	Lightning = ComputeLightningLightingSurface( _In.Position, Normal );
// 
// 	return (SunLight + Lightning) * TextureColor;

	float	Attenuation = sqrt( 1.0 - _In.U*_In.U );

	return 10 * LightningIntensity * _In.Intensity * Attenuation;
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
