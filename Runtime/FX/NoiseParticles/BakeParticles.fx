// This shader is designed to render a mesh into 2 render targets
//
Texture2D	NormalTexture : TEX_NORMAL;

SamplerState TexSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float3	Position : POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
	float3	LocalPosition : TEXCOORD1;
};

struct PS_OUT
{
	float4	Position_Alpha : SV_TARGET0;
	float4	PackedNormal_UV : SV_TARGET1;
};

//////////////////////////////////////////////////////////////////////////////
//
PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.LocalPosition = _In.Position;
	Out.Position = float4( 2*_In.UV.x-1, 1-2*_In.UV.y, 0, 1 );	// We render in UV space here...
	Out.Normal = _In.Normal;
	Out.Tangent = _In.Tangent;
	Out.BiTangent = _In.BiTangent;
	Out.UV = _In.UV;

	return	Out;
}

PS_OUT PS( PS_IN _In )
{
	// Fetch actual normal from the normal map
	float3	Normal = normalize( _In.Normal );
	float3	Tangent = normalize( _In.Tangent );
	float3	BiTangent = normalize( _In.BiTangent );

	float3	NormalSample = 2.0 * NormalTexture.Sample( TexSampler, _In.UV ).xyz - 1.0;
	float	fAttenuation = 0.5;
	Normal = normalize( fAttenuation * NormalSample.x * Tangent + fAttenuation * NormalSample.y * BiTangent + NormalSample.z * Normal );

	// Write all out
	PS_OUT	Out;
	Out.Position_Alpha.xyz = _In.LocalPosition;
	Out.Position_Alpha.w = sign( Normal.z );	// That render target should be cleared to 0 so invalid particles have an alpha of 0 (we also use the sign of alpha to encode the sign of the Z component of the normal)
	Out.PackedNormal_UV.xy = Normal.xy;			// Z will be extracted using sqrt( 1-x²-y² )
	Out.PackedNormal_UV.zw = _In.UV;

	return Out;
}

technique10 BakeParticles
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
