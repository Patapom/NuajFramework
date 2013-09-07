// Post-processes the environment cube maps
//
#include "../../Camera.fx"
#include "../../Samplers.fx"
#include "SHSupport.fx"

float		BufferInvSize;
Texture2D	MaterialBuffer;
Texture2D	GeometryBuffer;
float		IndirectLightingBoostFactor = 1.0;

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

struct PS_OUT
{
	float4	Coeffs0 : SV_TARGET0;
	float4	Coeffs1 : SV_TARGET1;
	float4	Coeffs2 : SV_TARGET2;
	float4	Coeffs3 : SV_TARGET3;
	float4	Coeffs4 : SV_TARGET4;
	float4	Coeffs5 : SV_TARGET5;
	float4	Coeffs6 : SV_TARGET6;
};

VS_IN VS( VS_IN _In ) { return _In; }

// This shader is more complicated as it will evaluate the SH using an existing environment SH map,
//	combine them with material albedo and perform a product with a SH cosine lobe to account for
//	diffuse reflection at the pixel's position.
//
PS_OUT PS( VS_IN _In )
{
	float2	UV = _In.Position * BufferInvSize;

	// Read back normal & depth
	float4	Geometry = GeometryBuffer.Sample( NearestClamp, UV );
	float3	Normal = Geometry.xyz;
	float	Z = Geometry.w;

	// Read back diffuse albedo
	float3	Albedo = MaterialBuffer.Sample( NearestClamp, UV ).xyz;
	Albedo *= IndirectLightingBoostFactor * 0.31830988618379067153776752674503;	// Albedo / PI

	// Rebuild 3D position in CAMERA space
	float3	Position = float3( (2.0 * UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * UV.y) * CameraData.x, 1.0 ) * Z;

	// Transform position & normal into WORLD space
	float3	WorldPosition = mul( float4( Position, 1.0 ), Camera2World ).xyz;
	float3	WorldNormal = mul( float4( Normal, 0.0 ), Camera2World ).xyz;

	// Evaluate existing SH at world position
	float3	SH[9];
	GetAmbientSH( WorldPosition, SH );

	// Take surface color
	SH[0] *= Albedo;
	SH[1] *= Albedo;
	SH[2] *= Albedo;
	SH[3] *= Albedo;
	SH[4] *= Albedo;
	SH[5] *= Albedo;
	SH[6] *= Albedo;
	SH[7] *= Albedo;
	SH[8] *= Albedo;

	// Evaluate cosine lobe in normal direction
	float	CosineSH[9];
	BuildSHCosineLobe( WorldNormal, CosineSH );

	// Combine together
	float3	CombinedSH[9];
	SHProduct( SH, CosineSH, CombinedSH );

	// Pack combined SH
	PS_OUT	Out;
	Out.Coeffs0 = float4( CombinedSH[0], CombinedSH[1].x );
	Out.Coeffs1 = float4( CombinedSH[1].yz, CombinedSH[2].xy );
	Out.Coeffs2 = float4( CombinedSH[2].z, CombinedSH[3] );
	Out.Coeffs3 = float4( CombinedSH[4], CombinedSH[5].x );
	Out.Coeffs4 = float4( CombinedSH[5].yz, CombinedSH[6].xy );
	Out.Coeffs5 = float4( CombinedSH[6].z, CombinedSH[7] );
	Out.Coeffs6 = float4( CombinedSH[8], 0.0 );

	return Out;
}
/*

// Once the environment has been successfully computed, we gather the computed
//	irradiance from the perceived pixels by multiplying the computed SH coefficients
//	by the coefficients in the view direction
//
PS_OUT PS3( PS_IN _In )
{
	float2	UV = _In.Position * BufferInvSize;

	// Read back distance
	float4	Geometry = GeometryBuffer.Sample( NearestClamp, UV );

	// Rebuild 3D position in CAMERA space
	float3	Position = float3( (2.0 * UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * UV.y) * CameraData.x, 1.0 ) * Geometry.w;
	float3	WorldPosition = mul( float4( Position, 1.0 ), Camera2World ).xyz;

	// Build view direction
	float3	View = normalize( Camera2World[3].xyz - WorldPosition);

	// Evaluate existing SH at world position
	float3	SH[9];
	GetAmbientSH( WorldPosition, SH );

 	// Evaluate SH in view direction
	float	f0 = 0.28209479177387814347403972578039;		// 0.5 / Math.Sqrt(Math.PI);
	float	f1 = 1.7320508075688772935274463415059 * f0;	// sqrt(3) * f0
	float	f2 = 3.8729833462074168851792653997824 * f0;	// sqrt(15.0) * f0

	float	ViewSH[9];
	ViewSH[0] = f0;
	ViewSH[1] = -f1 * View.x;
	ViewSH[2] = f1 * View.y;
	ViewSH[3] = -f1 * View.z;
	ViewSH[4] = f2 * View.x * View.z;
	ViewSH[5] = -f2 * View.x * View.y;
	ViewSH[6] = f2 * 0.28867513459481288225457439025097 * (3.0 * View.y*View.y - 1.0);
	ViewSH[7] = -f2 * View.z * View.y;
	ViewSH[8] = f2 * 0.5 * (View.z*View.z - View.x*View.x);

	// Combine together
	float3	CombinedSH[9];
	SHProduct( SH, ViewSH, CombinedSH );


// DEBUG
// CombinedSH[0] = SH[0];
// CombinedSH[1] = -SH[1];
// CombinedSH[2] = -SH[2];
// CombinedSH[3] = -SH[3];
// CombinedSH[4] = SH[4];
// CombinedSH[5] = SH[5];
// CombinedSH[6] = SH[6];
// CombinedSH[7] = SH[7];
// CombinedSH[8] = SH[8];

// CombinedSH[0] = ViewSH[0];
// CombinedSH[1] = ViewSH[1];
// CombinedSH[2] = ViewSH[2];
// CombinedSH[3] = ViewSH[3];
// CombinedSH[4] = ViewSH[4];
// CombinedSH[5] = ViewSH[5];
// CombinedSH[6] = ViewSH[6];
// CombinedSH[7] = ViewSH[7];
// CombinedSH[8] = ViewSH[8];


	// Pack combined SH
	PS_OUT	Out;
	Out.Coeffs0 = float4( CombinedSH[0], CombinedSH[1].x );
	Out.Coeffs1 = float4( CombinedSH[1].yz, CombinedSH[2].xy );
	Out.Coeffs2 = float4( CombinedSH[2].z, CombinedSH[3] );
	Out.Coeffs3 = float4( CombinedSH[4], CombinedSH[5].x );
	Out.Coeffs4 = float4( CombinedSH[5].yz, CombinedSH[6].xy );
	Out.Coeffs5 = float4( CombinedSH[6].z, CombinedSH[7] );
	Out.Coeffs6 = float4( CombinedSH[8], 0.0 );

	return Out;
}
*/

// ===================================================================================
//
technique10 IndirectLighting
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
/*
technique10 IndirectLighting_FinalGather
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS3() ) );
	}
}
*/
