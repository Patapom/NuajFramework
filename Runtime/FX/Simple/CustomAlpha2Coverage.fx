// This demonstrates the use of a Geometry Shader to generate
// Camera-oriented quads from single vertices

float4x4	Local2World : LOCAL2WORLD;

float4x4	World2Proj : WORLD2PROJ;
float4x4	Camera2World : CAMERA2WORLD;
float4x4	World2Camera : WORLD2CAMERA;

Texture2D	TexDiffuse : TEX_DIFFUSE;

float2		OneOverScreenSize;

static const int	CoverageMasks[] =
{
	0x00,	// 0 sample (no coverage)
	0x01,	// 1 sample
	0x11,	// 2 samples
	0x15,	// 3 samples
	0x55,	// 4 samples
	0x57,	// 5 samples
	0x77,	// 6 samples
	0x7F,	// 7 samples
	0xFF,	// 8 samples (full coverage)
};

cbuffer JitterBuffer
{
	float	JitterOffsets[64];
}

SamplerState DiffuseTextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};


struct VS_IN
{
	float3	Position : POSITION;
	float3	Normal : NORMAL;
	float2	UV : TEXCOORD0;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	Normal : NORMAL;
	float2	UV : TEXCOORD0;
};

struct PS_OUT
{
	float4	Color : SV_Target;
	uint	Coverage : SV_Coverage;
};

// Transform coordinates
PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( mul( float4( _In.Position, 1.0 ), Local2World ), World2Proj );
	Out.Normal = _In.Normal;
	Out.UV = _In.UV;

	return Out;
}

float4 PS( PS_IN _In ) : SV_Target
{
	return TexDiffuse.Sample( DiffuseTextureSampler, _In.UV );
}

PS_OUT PS2( PS_IN _In )
{
	PS_OUT	Out;
	Out.Color = TexDiffuse.Sample( DiffuseTextureSampler, _In.UV );

	// Jitter input alpha by screen position
	float	Alpha = Out.Color.a;

	float2	NormalizedPosition = _In.Position.xy * OneOverScreenSize;

	float2	BisouPosition = (float2( 100.0, 100.0 ) / _In.Position.z * NormalizedPosition * 64.0) % 63.0;
	int		X = int( floor( BisouPosition.x ) );
	int		Y = int( floor( BisouPosition.y ) );
	float2	Rems = BisouPosition - float2( X, Y );

	float	JitterX = lerp( JitterOffsets[X], JitterOffsets[X+1], Rems.x );
	float	JitterY = lerp( JitterOffsets[Y], JitterOffsets[Y+1], Rems.y );
	float	Jitter = 0.5 * (JitterX + JitterY);	// This should yield a jittering in [-1,+1]

	Alpha += Jitter * 0.125;	// 0.125 because we have 8 levels of coverage


	// Fetch the appropriate coverage mask
	Out.Coverage = CoverageMasks[min( 8, Alpha * 9 )];

// DEBUG
// Out.Coverage = 0xFF;
// Out.Color = float4( Jitter.xxx, 1 );

	return Out;
}

technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_1, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_1, PS() ) );
	}
}

technique10 RenderCustomCoverage
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_1, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_1, PS2() ) );
	}
}
