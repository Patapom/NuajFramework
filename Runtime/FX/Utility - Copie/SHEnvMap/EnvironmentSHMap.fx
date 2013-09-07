// Renders the environment SH mesh into the 3D environment SH texture
//
#include "../../Camera.fx"
#include "../../Samplers.fx"
#include "SHSupport.fx"

float4		SHLight[9];		// The 9 SH coefficients for the light
							// XYZ encodes ambient light
							// W encodes direct light

bool		EnableAmbientSH;
bool		EnableIndirectSH;

struct VS_IN
{
	float3	Position	: POSITION;
	float4	SH0			: SH0;
	float4	SH1			: SH1;
	float4	SH2			: SH2;
	float4	SH3			: SH3;
	float4	SH4			: SH4;
	float4	SH5			: SH5;
	float4	SH6			: SH6;
	float4	SH7			: SH7;
	float4	SH8			: SH8;
};

struct GS_IN
{
	float4	__Position	: SV_POSITION;
	float4	SH0			: SH0;
	float4	SH1			: SH1;
	float4	SH2			: SH2;
	float4	SH3			: SH3;
	float4	SH4			: SH4;
	float4	SH5			: SH5;
	float4	SH6			: SH6;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float4	SH			: SH;
	uint	RTIndex		: SV_RenderTargetArrayIndex;
	uint	ID			: Bisou;
};

GS_IN VS( VS_IN _In )
{
	// Transform position into camera space then offset & scale to map to the 2D bounding rectangle
	float2		Position2D = (_In.Position.xz - SHEnvOffset) * SHEnvScale;
	Position2D.x = 2.0 * Position2D.x - 1.0;
	Position2D.y = 1.0 - 2.0 * Position2D.y;

	GS_IN	Out;
	Out.__Position = float4( Position2D, 0.5, 1.0 );

	// First product is the occlusion of direct ambient light
	float3	a0[9] = { SHLight[0].xyz, SHLight[1].xyz, SHLight[2].xyz, SHLight[3].xyz, SHLight[4].xyz, SHLight[5].xyz, SHLight[6].xyz, SHLight[7].xyz, SHLight[8].xyz };
//	float3	a0[9] = { SHLight[0].www, SHLight[1].www, SHLight[2].www, SHLight[3].www, SHLight[4].www, SHLight[5].www, SHLight[6].www, SHLight[7].www, SHLight[8].www };
	float	b0[9] = { _In.SH0.w, _In.SH1.w, _In.SH2.w, _In.SH3.w, _In.SH4.w, _In.SH5.w, _In.SH6.w, _In.SH7.w, _In.SH8.w };
	float3	c0[9];
	if ( EnableAmbientSH )
		SHProduct( a0, b0, c0 );
	else
		c0[0] = c0[1] = c0[2] = c0[3] = c0[4] = c0[5] = c0[6] = c0[7] = c0[8] = 0.0;

	// Second product is the indirect lighting created by direct sun light
	// We want the _reflected_ lighting so we use the opposite light direction here
	// (and the stored indirect coefficients are also from the opposite direction)
	float3	a1[9] = { _In.SH0.xyz, _In.SH1.xyz, _In.SH2.xyz, _In.SH3.xyz, _In.SH4.xyz, _In.SH5.xyz, _In.SH6.xyz, _In.SH7.xyz, _In.SH8.xyz };
//	float3	a1[9] = { _In.SH0.xyz, -_In.SH1.xyz, -_In.SH2.xyz, -_In.SH3.xyz, _In.SH4.xyz, _In.SH5.xyz, _In.SH6.xyz, _In.SH7.xyz, _In.SH8.xyz };
//	float	b1[9] = { SHLight[0].w, SHLight[1].w, SHLight[2].w, SHLight[3].w, SHLight[4].w, SHLight[5].w, SHLight[6].w, SHLight[7].w, SHLight[8].w };
	float	b1[9] = { SHLight[0].w, -SHLight[1].w, -SHLight[2].w, -SHLight[3].w, SHLight[4].w, SHLight[5].w, SHLight[6].w, SHLight[7].w, SHLight[8].w };
	float3	c1[9];
	if ( EnableIndirectSH )
		SHProduct( a1, b1, c1 );
	else
		c1[0] = c1[1] = c1[2] = c1[3] = c1[4] = c1[5] = c1[6] = c1[7] = c1[8] = 0.0;

	// Sum coefficients
	float3	c[9];
	c[0] = c0[0] + c1[0];
	c[1] = c0[1] + c1[1];
	c[2] = c0[2] + c1[2];
	c[3] = c0[3] + c1[3];
	c[4] = c0[4] + c1[4];
	c[5] = c0[5] + c1[5];
	c[6] = c0[6] + c1[6];
	c[7] = c0[7] + c1[7];
	c[8] = c0[8] + c1[8];

	// Pack them into 7 RGBA slots
	Out.SH0.xyz = c[0];		Out.SH0.w = c[1].x;
	Out.SH1.xy = c[1].yz;	Out.SH1.zw = c[2].xy;
	Out.SH2.x = c[2].z;		Out.SH2.yzw = c[3];
	Out.SH3.xyz = c[4];		Out.SH3.w = c[5].x;
	Out.SH4.xy = c[5].yz;	Out.SH4.zw = c[6].xy;
	Out.SH5.x = c[6].z;		Out.SH5.yzw = c[7];
	Out.SH6.xyz = c[8];		Out.SH6.w = 0.0;

	return Out;
}

[maxvertexcount(3*7)]
void GS( triangle GS_IN _In[3], uint _ID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> Stream )
{
	// Build the triangle
	PS_IN	Out[3];
	Out[0].__Position = _In[0].__Position;
	Out[1].__Position = _In[1].__Position;
	Out[2].__Position = _In[2].__Position;
	Out[0].ID = _ID;
	Out[1].ID = _ID;
	Out[2].ID = _ID;

	// Output the 7 triangle variations
	Out[0].SH = _In[0].SH0;
	Out[1].SH = _In[1].SH0;
	Out[2].SH = _In[2].SH0;
	Out[0].RTIndex = 0;
	Out[1].RTIndex = 0;
	Out[2].RTIndex = 0;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();

	Out[0].SH = _In[0].SH1;
	Out[1].SH = _In[1].SH1;
	Out[2].SH = _In[2].SH1;
	Out[0].RTIndex = 1;
	Out[1].RTIndex = 1;
	Out[2].RTIndex = 1;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();

	Out[0].SH = _In[0].SH2;
	Out[1].SH = _In[1].SH2;
	Out[2].SH = _In[2].SH2;
	Out[0].RTIndex = 2;
	Out[1].RTIndex = 2;
	Out[2].RTIndex = 2;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();

	Out[0].SH = _In[0].SH3;
	Out[1].SH = _In[1].SH3;
	Out[2].SH = _In[2].SH3;
	Out[0].RTIndex = 3;
	Out[1].RTIndex = 3;
	Out[2].RTIndex = 3;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();

	Out[0].SH = _In[0].SH4;
	Out[1].SH = _In[1].SH4;
	Out[2].SH = _In[2].SH4;
	Out[0].RTIndex = 4;
	Out[1].RTIndex = 4;
	Out[2].RTIndex = 4;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();

	Out[0].SH = _In[0].SH5;
	Out[1].SH = _In[1].SH5;
	Out[2].SH = _In[2].SH5;
	Out[0].RTIndex = 5;
	Out[1].RTIndex = 5;
	Out[2].RTIndex = 5;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();

	Out[0].SH = _In[0].SH6;
	Out[1].SH = _In[1].SH6;
	Out[2].SH = _In[2].SH6;
	Out[0].RTIndex = 6;
	Out[1].RTIndex = 6;
	Out[2].RTIndex = 6;
	Stream.Append( Out[0] );
	Stream.Append( Out[1] );
	Stream.Append( Out[2] );
	Stream.RestartStrip();
}

float4 PS( PS_IN _In ) : SV_TARGET0
{
	return _In.SH;

// 	if ( _In.RTIndex != 6 )
// 		return 0.0;

	// Debug Delaunay
	float4	Colors[] =
	{
		float4( 1, 0, 0, 1 ),
		float4( 0, 1, 0, 1 ),
		float4( 0, 0, 1, 1 ),
		float4( 1, 1, 0, 1 ),
		float4( 0, 1, 1, 1 ),
		float4( 1, 0, 1, 1 ),
	};
	return 8.0 * Colors[_In.ID%6];
}


// ===================================================================================
//
technique10 DrawEnvMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
