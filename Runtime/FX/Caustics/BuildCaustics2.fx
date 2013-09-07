// This shader writes a deformed hexagonal grid into an additive target
//

#include "ComputeWallColor.fx"	// <= Normal, deformation & hit routines

struct VS_IN
{
	float3	Direction		: POSITION;
//	float2	UV				: TEXCOORD0;
};

struct GS_IN
{
	int		FaceIndex		: FACE_INDEX;	// The cube face index into which this vertex was projected
	float4	UVX_UVY			: TEXCOORD0;	// The projected UV coordinates for X and Y faces
	float3	UVZ_Intensity	: TEXCOORD1;	// The projected UV coordinates for Z faces + light intensity
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	uint	FaceIndex		: SV_RenderTargetArrayIndex;
	float2	UV				: TEXCOORD0;	// The UV coordinates within the face where this vertex was projected
	float	Intensity		: INTENSITY;	// Light intensity
};

float		TriangleNominalArea = 1.0;

GS_IN VS( VS_IN _In )
{
	GS_IN	Out;

	// Compute light direction for vertex
	float3	VertexPosition = SpherePosition + SphereRadius * _In.Direction;
	float3	LightRay = normalize( VertexPosition - LightPosition );	// View direction from the light toward the vertex

	// Fetch normal at vertex position on the sphere
	float3	Normal = FetchNormalHeight3D( _In.Direction ).xyz;

	// Compute Fresnel term using Shlick
	float	ICos = 1.0 - dot( -LightRay, Normal );
	float	ICos2 = ICos*ICos;
	float	ICos4 = ICos2*ICos2;
	float	Fresnel = R0 + (1.0-R0) * (ICos4 * ICos);

	// Blend between reflected & refracted ray
	float3	ReflectedRay = reflect( LightRay, Normal );
	float3	RefractedRay = refract( LightRay, Normal, IOR );
//	if ( dot(RefractedRay,RefractedRay) > 1e-4 )
		LightRay = lerp( RefractedRay, ReflectedRay, Fresnel );
// 	else
// 		LightRay = ReflectedRay;
//	float	DotLightBrewster = (1.0-saturate( dot( LightRay, Normal ) ));// / COS_BREWSTER;
//	Out.UVZ_Intensity.z = DotLightBrewster;//LightIntensity * saturate( length( LightRay ) );
	Out.UVZ_Intensity.z = LightIntensity;// * saturate( length( LightRay ) );

	// Project light ray onto one of the faces of the surrounding cube
	float2	HitUV;
	float	HitDistance;
	ComputeWallHit( VertexPosition, LightRay, Out.FaceIndex, HitUV, Out.UVX_UVY.xy, Out.UVX_UVY.zw, Out.UVZ_Intensity.xy, HitDistance );

	HitDistance *= 5.0;
	Out.UVZ_Intensity.z /= (1.0 + HitDistance*HitDistance);

	return Out;
}

// We use the GS to compute the triangle area and project it to the appropriate render target
//
float3	UVWeights[6] =
{
	float3( 1.0, 0.0, 0.0 ), float3( 1.0, 0.0, 0.0 ),	// Left/Right faces => Keep X UVs
	float3( 0.0, 1.0, 0.0 ), float3( 0.0, 1.0, 0.0 ),	// Top/Bottom faces => Keep Y UVs
	float3( 0.0, 0.0, 1.0 ), float3( 0.0, 0.0, 1.0 ),	// Front/Back faces => Keep Z UVs
};
[maxvertexcount(3*3)]
void GS( triangle GS_IN _In[3], inout TriangleStream<PS_IN> Stream )
{
	PS_IN	Out;

	// Determine which faces should be generated (as we have 3 vertices that can span 3 cube faces, we can generate up to 3 triangles)
	int		FaceIndices[3];
			FaceIndices[0] = _In[0].FaceIndex;
	int		FacesCount = 1;

	if ( _In[0].FaceIndex != _In[1].FaceIndex )
	{
		FaceIndices[FacesCount++] = _In[1].FaceIndex;
		if ( _In[0].FaceIndex != _In[2].FaceIndex && _In[1].FaceIndex != _In[2].FaceIndex )
			FaceIndices[FacesCount++] = _In[2].FaceIndex;
	}
	else if ( _In[0].FaceIndex != _In[2].FaceIndex )
		FaceIndices[FacesCount++] = _In[2].FaceIndex;

	// Generate as many triangles as necessary
	for ( int FaceIndexIndex=0; FaceIndexIndex < FacesCount; FaceIndexIndex++ )
	{
		Out.FaceIndex = FaceIndices[FaceIndexIndex];

		// Retrieve UVs for each vertex, selecting the UVs for the current vertex's projection face
		float3	Weights = UVWeights[Out.FaceIndex];		// These are the UV weights for the current face (they will isolate either X, Y or Z UVs)
		float2	UV0 = Weights.x * _In[0].UVX_UVY.xy
					+ Weights.y * _In[0].UVX_UVY.zw
					+ Weights.z * _In[0].UVZ_Intensity.xy;
		float2	UV1 = Weights.x * _In[1].UVX_UVY.xy
					+ Weights.y * _In[1].UVX_UVY.zw
					+ Weights.z * _In[1].UVZ_Intensity.xy;
		float2	UV2 = Weights.x * _In[2].UVX_UVY.xy
					+ Weights.y * _In[2].UVX_UVY.zw
					+ Weights.z * _In[2].UVZ_Intensity.xy;

		// Compute face intensity (intensity is the inverse area of the face)
		float	Area = 2.0 * cross( float3( UV2 - UV1, 0 ), float3( UV0 - UV1, 0 ) ).z;
		float	Intensity = INTENSITY_FACTOR * TriangleNominalArea / abs( Area );	// Intensity of our face

		// Build output triangle
		Out.Position = float4( 2.0 * UV0 - 1.0, 0.5, 1 );
		Out.UV = UV0;
		Out.Intensity = saturate( _In[0].UVZ_Intensity.z * Intensity );
		Stream.Append( Out );

		Out.Position = float4( 2.0 * UV1 - 1.0, 0.5, 1 );
		Out.UV = UV1;
		Out.Intensity = saturate( _In[1].UVZ_Intensity.z * Intensity );
		Stream.Append( Out );

		Out.Position = float4( 2.0 * UV2 - 1.0, 0.5, 1 );
		Out.UV = UV2;
		Out.Intensity = saturate( _In[2].UVZ_Intensity.z * Intensity );
		Stream.Append( Out );

		Stream.RestartStrip();
	}
}

float PS( PS_IN _In ) : SV_TARGET
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
