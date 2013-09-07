// HDR post-processing demo
// Defocus shader

Texture2D	TexSource;
Texture2D	TexKernel;
float2		TextureFullSize;
float2		TextureSubSize;
float		fKernelMipIndex;
float2		BlurSize;

int			KernelSamplesCount;					// Use this to make the kernel size vary (shader slower but more accurate)
//static const int	KernelSamplesCount = 8;		// Use this to make the kernel size constant (shader is much faster !)

SamplerState SourceTextureSampler
{
    Filter = MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

SamplerState KernelTextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
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
	float2	OffsetUVInTexels = UVInTexels + _OffsetInTexels;

	// Clamp
	OffsetUVInTexels = clamp( OffsetUVInTexels, 0.0, TextureSubSize-0.5 );

	return	OffsetUVInTexels / TextureFullSize;	// Re-normalize
}

float4 PS( VS_IN _In ) : SV_Target
{
//	return TexKernel.SampleLevel( KernelTextureSampler, _In.UV, fKernelMipIndex );

	// Perform convolution
	float3	ColorAccumulator = 0.0;
	float3	WeightAccumulator = 0.0;

	float2	KernelUV;
	float2	NeighborUV;
	for ( int Y=0; Y < KernelSamplesCount; Y++ )
	{
		KernelUV.y = (Y+0.5) / KernelSamplesCount;

		for ( int X=0; X < KernelSamplesCount; X++ )
		{
			KernelUV.x = (X+0.5) / KernelSamplesCount;

			// Sample kernel weight
			float3	Weight = TexKernel.SampleLevel( KernelTextureSampler, KernelUV, fKernelMipIndex ).rgb;

			// Sample neighbor color
			NeighborUV = ComputeUV( _In.UV, -BlurSize * (KernelUV-0.5) );
			float3	NeighborColor = TexSource.SampleLevel( SourceTextureSampler, NeighborUV, 0.0 ).rgb;

			// Convolve
			ColorAccumulator += Weight * NeighborColor;
			WeightAccumulator += Weight;
		}
	}

	return float4( ColorAccumulator / max( 1e-3, WeightAccumulator ), 1.0 );
}

technique10 PostProcessRender_Defocus
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
