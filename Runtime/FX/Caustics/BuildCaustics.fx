// This shader writes a deformed hexagonal grid into an additive target
//

struct VS_IN
{
	float2	UV				: TEXCOORD0;
};

struct GS_IN
{
	float2	UV				: TEXCOORD0;	// Same as VS_IN but the UVs are displaced at this point
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float2	UV				: TEXCOORD0;
	float	Intensity		: TEXCOORD1;
};

float		Time = 0.0;
float		TriangleNominalArea = 1.0;
float2		InvTexture = float2( 1.0 / 50.0, 0.0 );
Texture2D	NormalMap0;
Texture2D	NormalMap1;

SamplerState LinearWrap
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

// Reads a normal from the scrolling normal maps
//
float3	FetchNormal( float2 _UV )
{
//	float Time = 0.0;
	float2	UV0 = 0.4 * _UV + 0.01 * Time * float2( 1, -0.2 );
	float2	UV1 = 0.8 * _UV + 0.04 * Time * float2( -0.5, 0.7 );
	float3	Normal0  = 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 - InvTexture.xy, 0 ).xyz - 1.0;
 			Normal0 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.xy, 0 ).xyz - 1.0;
			Normal0 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 - InvTexture.yx, 0 ).xyz - 1.0;
			Normal0 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.yx, 0 ).xyz - 1.0;
	float3	Normal1  = 2.0 * NormalMap1.SampleLevel( LinearWrap, UV1 - InvTexture.xy, 0 ).xyz - 1.0;
			Normal1 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.xy, 0 ).xyz - 1.0;
			Normal1 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 - InvTexture.yx, 0 ).xyz - 1.0;
			Normal1 += 2.0 * NormalMap0.SampleLevel( LinearWrap, UV0 + InvTexture.yx, 0 ).xyz - 1.0;
	
	float3	Attenuation0 = 1.0 * float3( 1.0, 1.0, 1.5 );
	float3	Attenuation1 = 1.0 * float3( 1.0, 1.0, 1.5 );

	return normalize( Attenuation0 * Normal0 + Attenuation1 * Normal1 );
}

GS_IN VS( VS_IN _In )
{
	GS_IN	Out;
	Out.UV = _In.UV;

	// Deform UVs
	float	ProjectionPlaneDistance = 0.3;
	float3	Normal = FetchNormal( _In.UV );
	float3	RefractedRay = -refract( float3( 0.0, 0.0, -1.0 ), Normal, 1.33 );
	float3	HitPosition = 0.0;
	if ( RefractedRay.z > 0.01 )
	{
		HitPosition = RefractedRay * ProjectionPlaneDistance / RefractedRay.z;
		float MaxDisplacement = 0.05;
		HitPosition = max( -MaxDisplacement.xxx, min( MaxDisplacement.xxx, HitPosition ) );
	}
	Out.UV -= HitPosition.xy;

	return Out;
}

// We use the GS to compute the triangle area
//
[maxvertexcount(3)]
void GS( triangleadj GS_IN _In[6], inout TriangleStream<PS_IN> Stream )
{
	PS_IN	Out;

	// Intensity is the inverse area of the face
	float	fIntensityFactor = 1.0;

	float	Area = 2.0 * cross( float3( _In[4].UV - _In[2].UV, 0 ), float3( _In[0].UV - _In[2].UV, 0 ) ).z;
	float	Area0 = 2.0 * cross( float3( _In[2].UV - _In[1].UV, 0 ), float3( _In[0].UV - _In[1].UV, 0 ) ).z;	// Area of triangle adjacent to edge 0
	float	Area1 = 2.0 * cross( float3( _In[4].UV - _In[3].UV, 0 ), float3( _In[2].UV - _In[3].UV, 0 ) ).z;	// Area of triangle adjacent to edge 1
	float	Area2 = 2.0 * cross( float3( _In[0].UV - _In[5].UV, 0 ), float3( _In[4].UV - _In[5].UV, 0 ) ).z;	// Area of triangle adjacent to edge 2

	float	Intensity =  fIntensityFactor * TriangleNominalArea / abs( Area );	// Intensity of our face
	float	Intensity0 = fIntensityFactor * TriangleNominalArea / abs( Area0 );	// Intensity of adjacent face from edge 0
	float	Intensity1 = fIntensityFactor * TriangleNominalArea / abs( Area1 );	// Intensity of adjacent face from edge 1
	float	Intensity2 = fIntensityFactor * TriangleNominalArea / abs( Area2 );	// Intensity of adjacent face from edge 2

	float	Intensities[3];
	Intensities[0] = 0.25 * (1.0 * Intensity + Intensity0 + Intensity2);
	Intensities[1] = 0.25 * (1.0 * Intensity + Intensity0 + Intensity1);
	Intensities[2] = 0.25 * (1.0 * Intensity + Intensity1 + Intensity2);

	// Build output triangle
	for ( int v=0; v<3; v++ )
    {
		Out.Position = float4( 2.0 * _In[2*v].UV - 1.0, 0.5, 1 );
		Out.UV = _In[2*v].UV;
		Out.Intensity = min( 1.0, Intensities[v] );
//		Out.Intensity = Intensity;
		Stream.Append( Out );
    }
    Stream.RestartStrip();
}

float PS( PS_IN _In ) : SV_TARGET0
{
	return _In.Intensity;
}


// ===================================================================================
//
technique10 BuildCaustics
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
