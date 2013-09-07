// This shader performs the actual rendering of particles
//
Texture2D	OriginalParticlesPositionAlphaTexture;	// Undeformed positions
Texture2D	ParticlesPositionAlphaTexture;
Texture2D	ParticlesNormalUVTexture;
Texture2D	DiffuseTexture : TEX_DIFFUSE;
float4		AmbientColor;

float4x4	Local2World : LOCAL2WORLD;

#include "..\Camera.fx"
#include "..\DirectionalLighting.fx"
#include "..\DirectionalLighting2.fx"
#include "..\LinearToneMapping.fx"

SamplerState ParticleSampler
{
    Filter = MIN_MAG_MIP_POINT;	// No linear filtering here, we don't want to interpolate the 3D positions from one texel to the other !
    AddressU = Clamp;
    AddressV = Clamp;
};

SamplerState TexSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	Normal : NORMAL;
	float3	OriginalWorldPosition : TEXCOORD1;
	float3	WorldPosition : TEXCOORD2;
	float4	Color : TEXCOORD3;
};

//////////////////////////////////////////////////////////////////////////////
//
PS_IN VS( VS_IN _In )
{
	// Sample particle position & normal from our baked textures
	float4	OriginalPositionAlpha = OriginalParticlesPositionAlphaTexture.SampleLevel( ParticleSampler, _In.UV, 0 );
	float4	PositionAlpha = ParticlesPositionAlphaTexture.SampleLevel( ParticleSampler, _In.UV, 0 );
	float4	NormalUV = ParticlesNormalUVTexture.SampleLevel( ParticleSampler, _In.UV, 0 );

	// Unpack stuff
	float3	LocalPosition = PositionAlpha.xyz;
	float	Alpha = abs( PositionAlpha.a );
	float3	Normal = float3( NormalUV.xy, PositionAlpha.a*sqrt( 1-dot(NormalUV.xy,NormalUV.xy) ) );
	float2	UV = NormalUV.zw;

	// Sample particle color (optional if you wish to render the particles with procedural colors)
 	float4	Color = DiffuseTexture.SampleLevel( TexSampler, UV, 0 );
//	float4	Color = 1;	// Uniformly white..
	Color.a = Alpha;

	// Generate the seed data for the GS
	PS_IN	Out;
	Out.Position = mul( float4( LocalPosition, 1 ), Local2World );
	Out.OriginalWorldPosition = mul( float4( OriginalPositionAlpha.xyz, 1 ), Local2World ).xyz;
	Out.WorldPosition = Out.Position.xyz;
	Out.Normal = normalize( mul( float4( Normal, 0 ), Local2World ).xyz );
	Out.Color = Color;

	return	Out;
}

[maxvertexcount( 4 )]
void	GS( point PS_IN _In[1], inout TriangleStream<PS_IN> _OutStream )
{
	if ( _In[0].Color.a < 0.5 )
		return;	// This particle is not on the mesh. Don't even bother to generate it !

	float3	SourcePosition = _In[0].WorldPosition;

	PS_IN	Out;
	Out.OriginalWorldPosition = _In[0].OriginalWorldPosition;
	Out.Color = _In[0].Color;
	Out.Normal = _In[0].Normal;

	float	fParticleSize = 0.1f;

	// Compute camera plane
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	// Use this to make the sprites face the surface normal instead of always facing the camera plane
	Right = normalize( cross( Up, Out.Normal ) );
	Up = cross( Out.Normal, Right );

	// Upper left vertex
	Out.WorldPosition = SourcePosition + fParticleSize * (-Right+Up);
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	_OutStream.Append( Out );

	// Bottom left vertex
	Out.WorldPosition = SourcePosition + fParticleSize * (-Right-Up);
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	_OutStream.Append( Out );

	// Upper right vertex
	Out.WorldPosition = SourcePosition + fParticleSize * (+Right+Up);
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	_OutStream.Append( Out );

	// Bottom right vertex
	Out.WorldPosition = SourcePosition + fParticleSize * (+Right-Up);
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	_OutStream.Append( Out );

	_OutStream.RestartStrip();
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	SkinColor = _In.Color.rgb;

	// Make the skin "on fire"
	float3	OriginalWorldPosition = _In.OriginalWorldPosition;
	float3	DeformedWorldPosition = _In.WorldPosition;
	float	fFireIntensity = 1.0 * saturate( 0.1 * length( DeformedWorldPosition - OriginalWorldPosition ) );
// 	SkinColor.r *= 1.0 + 2.0 * fFireIntensity;
// 	SkinColor.g *= 1.0 + 0.5 * fFireIntensity;
// 	SkinColor.b *= 1.0 + 0.2 * fFireIntensity;
//	SkinColor = saturate( SkinColor );

	SkinColor = lerp( SkinColor, float3( 4.0, 1.2, 0.3 ), fFireIntensity );

	// Simple lighting for fun
	float	fDiffuseFactor0 = saturate( dot( _In.Normal, LightDirection ) + fFireIntensity );
	float	fDiffuseFactor1 = saturate( dot( _In.Normal, LightDirection2 ) + fFireIntensity );

	return float4( ApplyToneMapping( AmbientColor.rgb + SkinColor * (fDiffuseFactor0 * LightColor.rgb + fDiffuseFactor1 * LightColor2.rgb) ), 1 );
}

technique10 RenderParticles
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
