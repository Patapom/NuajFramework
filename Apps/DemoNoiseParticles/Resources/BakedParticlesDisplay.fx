// Baked particles debug display

Texture2D	ParticlesPositionAlphaTexture;
Texture2D	ParticlesNormalUVTexture;

SamplerState TextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float4	TransformedPosition : SV_POSITION;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	TransformedPosition : SV_Position;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out = (PS_IN) 0;
	
	Out.TransformedPosition = _In.TransformedPosition;
	Out.View = _In.View;
	Out.UV = _In.UV;
	
	return	Out;
}

float4 PS( PS_IN _In ) : SV_Target
{
	// Debug Normals
// 	float	ZSign = ParticlesPositionAlphaTexture.Sample( TextureSampler, _In.UV ).w;
// 	float3	Normal = float3( ParticlesNormalUVTexture.Sample( TextureSampler, _In.UV ).xy, 0 );
// 			Normal.z = ZSign * sqrt( 1-dot(Normal.xy,Normal.xy) );
// 	return float4( 0.5*(1+Normal), 1 );

	// Debug UVs
//	return float4( ParticlesNormalUVTexture.Sample( TextureSampler, _In.UV ).zw, 0, 1 );

	// Debug Positions
	return float4( 2 * ParticlesPositionAlphaTexture.Sample( TextureSampler, _In.UV ).xyz, 1 );
}

technique10 PostProcessRender
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
