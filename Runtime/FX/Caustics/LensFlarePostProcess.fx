// This performs radial blur and displays the lens flare as an additive post process

float2		RadialCenter = float2( 0.5, 0.5 );
float2		InvAspectRatio = float2( 1.0, 1.0 );
float3		InvTextureSize = float3( 1.0/256.0, 1.0/256.0, 0.0 );
Texture2D	RadialPreviousPassTexture;

SamplerState RadialSampler
{
    Filter = MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Mirror;
    AddressV = Mirror;
};

SamplerState TextureSampler
{
    Filter = MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VS_IN
{
	float4	Position	: SV_POSITION;
//	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_Position;
	float2	UV			: TEXCOORD0;
};

PS_IN	VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = _In.Position;
	Out.UV = _In.UV;
	
	return	Out;
}

float4 PS( PS_IN _In ) : SV_Target
{
	float3	Color =  RadialPreviousPassTexture.SampleLevel( RadialSampler, _In.UV - InvTextureSize.xz, 0 ).xyz;
			Color += RadialPreviousPassTexture.SampleLevel( RadialSampler, _In.UV + InvTextureSize.xz, 0 ).xyz;
			Color += RadialPreviousPassTexture.SampleLevel( RadialSampler, _In.UV - InvTextureSize.zy, 0 ).xyz;
			Color += RadialPreviousPassTexture.SampleLevel( RadialSampler, _In.UV + InvTextureSize.zy, 0 ).xyz;

	return float4( Color, 1.0 );
}

float4 PS2( PS_IN _In ) : SV_Target
{
 	float2	Center = float2( RadialCenter.x, 1.0 - RadialCenter.y );

	float2	Direction = _In.UV - Center;
	float	Distance2Center = length( Direction );
			Direction /= Distance2Center;

	// Accumulate samples
	float3	Sum = float3( 0.0, 0.0, 0.0 );
	for ( int i=0; i < 16; i++ )
	{
		float	SampleDistance = 0.2 * (i+0.5) / 16.0;
		Sum += RadialPreviousPassTexture.SampleLevel( RadialSampler, Center + Direction * SampleDistance, 0 ).xyz;
	}
	Sum *= 1.0 / (16.0 * 10.0 * Distance2Center);
	return float4( Sum, 1.0 );
}

float4 PS3( PS_IN _In ) : SV_Target
{
	return RadialPreviousPassTexture.SampleLevel( TextureSampler, _In.UV, 0 );
}

technique10 Blur
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 RadialBlur
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}

technique10 DrawPostProcess
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS3() ) );
	}
}
