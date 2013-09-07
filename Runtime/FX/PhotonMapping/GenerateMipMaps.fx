// This shader generates the mip maps of a 3D render target.
//
#include "../Samplers.fx"

Texture3D	HigherMipLevel;
float4		dUVWSource;
float4		dUVWTarget;

struct VS_IN
{
	float4	__Position			: SV_POSITION;
	uint	SliceIndex			: SV_INSTANCEID;
};

struct GS_IN
{
	float4	__Position			: SV_POSITION;
	float3	UVW					: TEXCOORD0;
	uint	SliceIndex			: SLICE_INDEX;
};

struct PS_IN
{
	float4	__Position			: SV_POSITION;
	float3	UVW					: TEXCOORD0;
	uint	SliceIndex			: SV_RENDERTARGETARRAYINDEX;
};

GS_IN	VS( VS_IN _In )
{
	GS_IN	Out;
	Out.__Position = _In.__Position;
	Out.SliceIndex = _In.SliceIndex;
	Out.UVW = float3( 0.5 * (1.0 + _In.__Position.xy), _In.SliceIndex * dUVWTarget.z );

	return Out;
}

[maxvertexcount( 3 )]
void	GS( triangle GS_IN _In[3], inout TriangleStream<PS_IN> _Stream )
{
	PS_IN	Out;
	Out.__Position = _In[0].__Position;
	Out.UVW = _In[0].UVW;
	Out.SliceIndex = _In[0].SliceIndex;
	_Stream.Append( Out );

	Out.__Position = _In[1].__Position;
	Out.UVW = _In[1].UVW;
	Out.SliceIndex = _In[1].SliceIndex;
	_Stream.Append( Out );

	Out.__Position = _In[2].__Position;
	Out.UVW = _In[2].UVW;
	Out.SliceIndex = _In[2].SliceIndex;
	_Stream.Append( Out );
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	_In.UVW.xy = _In.__Position.xy * dUVWTarget.xy;
//	_In.UVW.z += 0.5 * dUVWTarget.z;

	float4	V000 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW, 0 );
	float4	V001 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.xww, 0 );
	float4	V011 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.xyw, 0 );
	float4	V010 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.wyw, 0 );

	float4	V100 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.wwz, 0 );
	float4	V101 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.xwz, 0 );
	float4	V111 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.xyz, 0 );
	float4	V110 = HigherMipLevel.SampleLevel( LinearClamp, _In.UVW + dUVWSource.wyz, 0 );

	return 0.125 * (V000+V001+V010+V011 + V100+V101+V110+V111);
}

technique10 GenerateMipMaps
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
