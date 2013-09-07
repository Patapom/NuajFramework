// Downscaling and Upscaling shader
//
Texture2D	TexSource;			// The source texture to sample from (our scaled image is contained within a smaller portion of that texture)
float2		TextureFullSize;	// Full size of the texture we're sampling
float2		TextureSubSize;		// Size of the small texture to scale within that big texture
float2		SubPixelUVScale;	// Sub-pixel accuracy scale factor

SamplerState SourceTextureSampler
{
    Filter = MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VS_IN
{
	float4	TransformedPosition : SV_POSITION;
	float3	View : VIEW;
	float2	UV : TEXCOORD0;
};

VS_IN	VS( VS_IN _In )	{ return _In; }

float2	ComputeUV( float2 _UV, float2 _OffsetInTexels )
{
	float2	UVInTexels = _UV * TextureSubSize;	// De-normalize
	float2	OffsetUVInTexels = UVInTexels + _OffsetInTexels * SubPixelUVScale;

	// Clamp
	OffsetUVInTexels = clamp( OffsetUVInTexels, 0.0, TextureSubSize-0.5 );	// Clamp so bilinear won't go fetch un-needed pixels across the border...

	return	OffsetUVInTexels / TextureFullSize;	// Re-normalize
}

// Single sample LOW quality version
float4 PS_LO( VS_IN _In ) : SV_Target
{
	return TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.5, 0.5 ) ) );
}

// ===================================================================================
// METHOD: AVERAGE

// 5 samples MEDIUM quality version
float4 PS_MED( VS_IN _In ) : SV_Target
{
	float4	C  = TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, 0.0 ) ) );

	float	W0 = 1.0;
	float4	C0 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, 0.0 ) ) );
	float4	C1 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, 0.0 ) ) );
	float4	C2 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, -1.0 ) ) );
	float4	C3 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, +1.0 ) ) );
	return (C+C0+C1+C2+C3) / (1.0 + 4*W0);
}

// 9 Samples HI quality version
float4 PS_HI( VS_IN _In ) : SV_Target
{
	float4	C = TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, 0.0 ) ) );

	float	W0 = 1.0;
	float4	C0 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, 0.0 ) ) );
	float4	C1 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, 0.0 ) ) );
	float4	C2 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, -1.0 ) ) );
	float4	C3 = W0 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, +1.0 ) ) );

	float	W1 = 0.5;
	float4	C4 = W1 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, -1.0 ) ) );
	float4	C5 = W1 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, -1.0 ) ) );
	float4	C6 = W1 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, +1.0 ) ) );
	float4	C7 = W1 * TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, +1.0 ) ) );

	return (C + C0 + C1 + C2 + C3 + C4 + C5 + C6 + C7) / (1.0 + 4.0 * (W0 + W1));
}

// ===================================================================================
// METHOD: MAX

// 5 samples MEDIUM quality version
float4 PS_MED_MAX( VS_IN _In ) : SV_Target
{
	float4	C  = TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, 0.0 ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, 0.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, 0.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, -1.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, +1.0 ) ) ) );
	return C;
}

// 9 Samples HI quality version
float4 PS_HI_MAX( VS_IN _In ) : SV_Target
{
	float4	C = TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, 0.0 ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, 0.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, 0.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, -1.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, +1.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, -1.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, -1.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, +1.0 ) ) ) );
			C = max( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, +1.0 ) ) ) );

	return C;
}

// ===================================================================================
// METHOD: MIN

// 5 samples MEDIUM quality version
float4 PS_MED_MIN( VS_IN _In ) : SV_Target
{
	float4	C  = TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, 0.0 ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, 0.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, 0.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, -1.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, +1.0 ) ) ) );
	return C;
}

// 9 Samples HI quality version
float4 PS_HI_MIN( VS_IN _In ) : SV_Target
{
	float4	C = TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, 0.0 ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, 0.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, 0.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, -1.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( 0.0, +1.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, -1.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, -1.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( -1.0, +1.0 ) ) ) );
			C = min( C, TexSource.Sample( SourceTextureSampler, ComputeUV( _In.UV, float2( +1.0, +1.0 ) ) ) );

	return C;
}


// ===================================================================================
technique10 PostProcessRender_Scale_LOWQUALITY
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_LO() ) );
	}
}

technique10 PostProcessRender_Scale_MEDIUMQUALITY
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_MED() ) );
	}
}

technique10 PostProcessRender_Scale_HIGHQUALITY
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_HI() ) );
	}
}

technique10 PostProcessRender_Scale_MEDIUMQUALITY_MAX
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_MED_MAX() ) );
	}
}

technique10 PostProcessRender_Scale_HIGHQUALITY_MAX
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_HI_MAX() ) );
	}
}

technique10 PostProcessRender_Scale_MEDIUMQUALITY_MIN
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_MED_MIN() ) );
	}
}

technique10 PostProcessRender_Scale_HIGHQUALITY_MIN
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS_HI_MIN() ) );
	}
}
