// Final image composition post-process
// This post-process combines the materials and the lighting to form the final HDR image yet to be tone-mapped and displayed to the screen
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"

float4		ScreenInfos;		// XY=1/ScreenSize ZW=ScreenSize
float4		LightBufferInfos;	// XY=1/LightBufferSize ZW=LightBufferSize

// Normal depth stencil
Texture2D	DepthStencil;

// Downscaled light buffer infos
Texture2D	LightDepthStencil;
Texture2D	LightGeometryBuffer;
Texture2D	LightBuffer;

float4		BilateralInfos;	// X=OffsetAmplitude, that is used to displace sub-pixel fetch based on the 4 light samples' rejection criteria
							// Y=NormalDifferenceFactor, the factor applied to differences in neighboring normals
							// Z=PositionDifferencesFactor, the factor applied to differences in neighboring positions
							// W=SlopeAttenuation, the attenuation of differences in neighboring positions based on viewing angle (differences are decreased when viewing at grazing camera angles)
float4		BilateralInfos2;// X=LuminanceDifferenceFactor, the factor applied to differences in neighboring luminances
							// Y=DiffuseAlbedoFactor
							// Z=SpecularAlbedoFactor

// Shadow Mapping support
Texture2D	ShadowMap : SHADOWMAP;
float4x4	Camera2Shadow : CAMERA2SHADOW;
float4x4	Light2Camera : LIGHT2CAMERA;
float3		LightCameraCenter : LIGHT_CAMERA_CENTER;


struct VS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float3	View				: VIEW;
	float2	UV					: TEXCOORD0;
};

struct PS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float3	View				: VIEW;
	float2	UV					: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	return _In;
}

float4 PS( PS_IN _In ) : SV_TARGET0
{
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf-Zn)

	// =================================================================================
	// Read depth & normal for actual pixel to light
	float3	Emissive, DiffuseAlbedo, SpecularAlbedo, Normal;
	float	Depth, SpecularPower, Extinction;
	ReadDeferredMRT( _In.UV, Emissive, Extinction, DiffuseAlbedo, SpecularAlbedo, SpecularPower, Normal, Depth );

//	return float4( 0.5 * (1.0 + DiffuseAlbedo), 1.0 );
//	return float4( DiffuseAlbedo, 1.0 );
//	return float4( abs( DiffuseAlbedo ), 1.0 );

	DiffuseAlbedo *= BilateralInfos2.y;
	DiffuseAlbedo *= 0.31830988618379067153776752674503;	// Albedo / PI

	SpecularAlbedo *= BilateralInfos2.z;

	float	Z = -CameraData.z * Q / (DepthStencil.SampleLevel( NearestClamp, _In.UV, 0 ).x - Q);
// 	return float4( 0.01 * Depth.xxx, 1.0 );
// 	return float4( 0.01 * Z.xxx, 1.0 );
//	return float4( abs( Z - Depth ).xxx, 1.0 );	// Depth is 16 bits whereas Z is from the 32 bits stencil so we better use the latter !
	float3	Position = float3( (2.0 * _In.UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * _In.UV.y) * CameraData.x, 1.0 ) * Z;


/*	// =================================================================================
	// Test shadow mapping
	float	Q2 = 400.0 / (400.0 - 0.1);	// Zf / (Zf-Zn)

	float4	PositionShadow = mul( float4( Position, 1.0 ), Camera2Shadow );
			PositionShadow /= PositionShadow.w;

// 	return float4( PositionShadow.xy, 0, 1 );

	float	ProjectionLength = 200.0;
	float3	LightDirection = normalize( float3( 1, 1, 1 ) );
	float3	PositionWorld = mul( float4( Position, 1.0 ), Camera2World ).xyz;
	float3	PixelInLightPlane = PositionWorld + ProjectionLength * LightDirection;
	float3	LightPlaneCenter = LightCameraCenter + ProjectionLength * LightDirection;

	float3	LightPlaneRight = normalize( cross( LightDirection, float3( 0.0, 1.0, 0.0 ) ) );
	float3	LightPlaneUp = cross( LightPlaneRight, LightDirection );

	float2	LightPlaneUV = float2(
		dot( PixelInLightPlane - LightPlaneCenter, LightPlaneRight ),
		dot( LightPlaneCenter - PixelInLightPlane, LightPlaneUp )
		);
	LightPlaneUV = 0.5 * (1.0 + LightPlaneUV / ProjectionLength);

//	return float4( LightPlaneUV, 0, 1 );
//	float	Rha = -0.1 * Q2 / (ShadowMap.SampleLevel( NearestClamp, LightPlaneUV, 0 ).x - Q2);
//	return float4( 0.01 * Rha.xxx, 1 );

	static const int	StepsCount = 16;
	float2	Step = float2( 0.5, 0.5 ) - LightPlaneUV;
			Step /= StepsCount;
	float	Light = 1.0;
	for ( int StepIndex=0; StepIndex < StepsCount; StepIndex++ )
	{
		float	ShadowZ = -0.1 * Q2 / (ShadowMap.SampleLevel( NearestClamp, LightPlaneUV, 0 ).x - Q2);
		if ( ShadowZ < 199.0 )
			Light *= 0.5;

		LightPlaneUV += Step;
	}

	return float4( Light * DiffuseAlbedo, 1.0 );
*/
	// =================================================================================
	// Compute UVs for the 4 light pixels to sample
	float2	Pixel = _In.UV * (ScreenInfos.zw+1) * 0.5;		// Pixel index in light buffer
	float2	rs = Pixel - floor( Pixel );					// Interpolants
	Pixel -= rs;
	
	float2	LightUV00 = Pixel / (LightBufferInfos.zw+1);
	float2	LightUV01 = LightUV00 + float2( LightBufferInfos.x, 0.0 );
	float2	LightUV10 = LightUV00 + float2( 0.0, LightBufferInfos.y );
	float2	LightUV11 = LightUV00 + LightBufferInfos.xy;
	
//	return float4( rs, 0, 1 );
// 	if ( abs( _In.UV.x - 1.0 ) < 1e-3 )
// 		return float4( 1, 1, 0, 0 );
//
// 	if ( abs( rs.x - 0.25 ) < 1e-3 )
// 		return float4( 1, 0, 0, 1 );
// 	else if ( abs( rs.x - 0.75 ) < 1e-3 )
// 		return float4( 0, 0, 1, 1 );
// 
// 	return float4( 1, 1, 1, 1 );
	
	// =================================================================================
	// Read depth, normal and light for these 4 pixels
	float3	Normal00 = float3( LightGeometryBuffer.SampleLevel( NearestClamp, LightUV00, 0 ).xy, 0.0 );
	Normal00.z = sqrt( 1.0 - dot( Normal00.xy, Normal00.xy ) );
	Normal00.xy = 2.0 * Normal00.z * Normal00.xy;
	Normal00.z = 1.0 - 2.0 * Normal00.z * Normal00.z;
	float3	Normal01 = float3( LightGeometryBuffer.SampleLevel( NearestClamp, LightUV01, 0 ).xy, 0.0 );
	Normal01.z = sqrt( 1.0 - dot( Normal01.xy, Normal01.xy ) );
	Normal01.xy = 2.0 * Normal01.z * Normal01.xy;
	Normal01.z = 1.0 - 2.0 * Normal01.z * Normal01.z;
	float3	Normal10 = float3( LightGeometryBuffer.SampleLevel( NearestClamp, LightUV10, 0 ).xy, 0.0 );
	Normal10.z = sqrt( 1.0 - dot( Normal10.xy, Normal10.xy ) );
	Normal10.xy = 2.0 * Normal10.z * Normal10.xy;
	Normal10.z = 1.0 - 2.0 * Normal10.z * Normal10.z;
	float3	Normal11 = float3( LightGeometryBuffer.SampleLevel( NearestClamp, LightUV11, 0 ).xy, 0.0 );
	Normal11.z = sqrt( 1.0 - dot( Normal11.xy, Normal11.xy ) );
	Normal11.xy = 2.0 * Normal11.z * Normal11.xy;
	Normal11.z = 1.0 - 2.0 * Normal11.z * Normal11.z;
	
	float	Z00 = -CameraData.z * Q / (LightDepthStencil.SampleLevel( NearestClamp, LightUV00, 0 ).x - Q);
	float	Z01 = -CameraData.z * Q / (LightDepthStencil.SampleLevel( NearestClamp, LightUV01, 0 ).x - Q);
	float	Z10 = -CameraData.z * Q / (LightDepthStencil.SampleLevel( NearestClamp, LightUV10, 0 ).x - Q);
	float	Z11 = -CameraData.z * Q / (LightDepthStencil.SampleLevel( NearestClamp, LightUV11, 0 ).x - Q);
	
	float4	L00 = LightBuffer.SampleLevel( NearestClamp, LightUV00, 0 );
	float4	L01 = LightBuffer.SampleLevel( NearestClamp, LightUV01, 0 );
	float4	L10 = LightBuffer.SampleLevel( NearestClamp, LightUV10, 0 );
	float4	L11 = LightBuffer.SampleLevel( NearestClamp, LightUV11, 0 );
	
	float	Lum00 = dot( L00.xyz, LUMINANCE_WEIGHTS );
	float	Lum01 = dot( L01.xyz, LUMINANCE_WEIGHTS );
	float	Lum10 = dot( L10.xyz, LUMINANCE_WEIGHTS );
	float	Lum11 = dot( L11.xyz, LUMINANCE_WEIGHTS );

	// NOTE FOR LATER : Try and sample with linear clamp and offset UVs to nice positions using something like the metrics below

//	return float4( L00.xyz, 1.0 );

	// =================================================================================
	// Compute discard factors for the 4 light samples
	float3	ToCamera = normalize( Position );
	float	DotView = BilateralInfos.w * abs( dot( Normal, ToCamera ) );	// This helps minimizing position discrepancies when view angle increases
	
	// Discard by difference in normal
	float	DN00 = 1.0 - abs( dot(Normal00, Normal) );
	float	DN01 = 1.0 - abs( dot(Normal01, Normal) );
	float	DN10 = 1.0 - abs( dot(Normal10, Normal) );
	float	DN11 = 1.0 - abs( dot(Normal11, Normal) );

	// Discard by difference in position
	float	DP00 = DotView * abs(Z00-Z);
	float	DP01 = DotView * abs(Z01-Z);
	float	DP10 = DotView * abs(Z10-Z);
	float	DP11 = DotView * abs(Z11-Z);
	
	// Discard by difference in luminance
	float	AverageLum = 0.25 * (L00+L01+L10+L11);
// 	float	DL00 = 1.0 - saturate( 0.01 * abs(L00 - (L01+L10+L11)/3) );
// 	float	DL01 = 1.0 - saturate( 0.01 * abs(L01 - (L00+L10+L11)/3) );
// 	float	DL10 = 1.0 - saturate( 0.01 * abs(L10 - (L00+L01+L11)/3) );
// 	float	DL11 = 1.0 - saturate( 0.01 * abs(L11 - (L00+L01+L10)/3) );
	float	DL00 = 1.0 - saturate( BilateralInfos2.x * abs(L00 - AverageLum) );
	float	DL01 = 1.0 - saturate( BilateralInfos2.x * abs(L01 - AverageLum) );
	float	DL10 = 1.0 - saturate( BilateralInfos2.x * abs(L10 - AverageLum) );
	float	DL11 = 1.0 - saturate( BilateralInfos2.x * abs(L11 - AverageLum) );

	// First metric using an addition of both normal and position discrepancies
// 	float	Weight00 = saturate( BilateralInfos.y * DN00 + BilateralInfos.z * DP00 );
// 	float	Weight01 = saturate( BilateralInfos.y * DN01 + BilateralInfos.z * DP01 );
// 	float	Weight10 = saturate( BilateralInfos.y * DN10 + BilateralInfos.z * DP10 );
// 	float	Weight11 = saturate( BilateralInfos.y * DN11 + BilateralInfos.z * DP11 );
	
	// Second metric using a modulation of normal and position discrepancies
	float	Weight00 = 1.0 - saturate( DL00 * (1.0 - BilateralInfos.y * DN00) * (1.0 - BilateralInfos.z * DP00) );
	float	Weight01 = 1.0 - saturate( DL01 * (1.0 - BilateralInfos.y * DN01) * (1.0 - BilateralInfos.z * DP01) );
	float	Weight10 = 1.0 - saturate( DL10 * (1.0 - BilateralInfos.y * DN10) * (1.0 - BilateralInfos.z * DP10) );
	float	Weight11 = 1.0 - saturate( DL11 * (1.0 - BilateralInfos.y * DN11) * (1.0 - BilateralInfos.z * DP11) );
	
//	return float4( 0.2 * DiffuseAlbedo + 0.8 * float3( Weight00, Weight01, Weight10 ), 1.0 );
// 	return float4( Weight00, Weight01, Weight10, 1.0 );
	
	
	// =================================================================================
	// Offset interpolants
// 	float2	Offset = Weight00 * float2( -0.5, -0.5 );
// 	Offset += Weight01 * float2( +0.5, -0.5 );
// 	Offset += Weight10 * float2( -0.5, +0.5 );
// 	Offset += Weight11 * float2( +0.5, +0.5 );
// 	Offset /= (Weight00 + Weight01 + Weight10 + Weight11);
// 	rs = saturate( rs + Offset );
// 	return float4( 4.0 * Offset, 0, 1 );
// // 	return float4( 1.0 + 2.0 * Offset, 0, 1 );
// // 	return float4( rs, 0, 1 );
// 
// // 	rs = 1.0;
// // 	rs = lerp( rs, BilateralInfos.xy, BilateralInfos.z );
// 
// 	float4	L0 = lerp( L00, L01, rs.x );
// 	float4	L1 = lerp( L10, L11, rs.x );
// 	float4	L = lerp( L0, L1, rs.y );
	
	
	// =================================================================================
	// Weight lights
// 	float	Threshold = 0.5;
// 	float	Discard00 = 1.0 - Weight00;
// 	float	Discard01 = 1.0 - Weight01;
// 	float	Discard10 = 1.0 - Weight10;
// 	float	Discard11 = 1.0 - Weight11;
// 	float4	L = L00 * Discard00 + L01 * Discard01 + L10 * Discard10 + L11 * Discard11;
// 			L /= (Discard00 + Discard01 + Discard10 + Discard11);
	
	
	// =================================================================================
	// Hybrid method that offsets interpolants and weights lights, both based on differences in normal and position
	float	Delta = BilateralInfos.x;
	float2	Offset  = Weight00 * float2( -Delta, -Delta );
			Offset += Weight01 * float2( +Delta, -Delta );
			Offset += Weight10 * float2( -Delta, +Delta );
			Offset += Weight11 * float2( +Delta, +Delta );
	Offset /= (Weight00 + Weight01 + Weight10 + Weight11);
	rs = saturate( rs + Offset );
	
	L00 *= saturate( 1.0 - Weight00 );
	L01 *= saturate( 1.0 - Weight01 );
	L10 *= saturate( 1.0 - Weight10 );
	L11 *= saturate( 1.0 - Weight11 );
	
	float4	L0 = lerp( L00, L01, rs.x );
	float4	L1 = lerp( L10, L11, rs.x );
	float4	L = lerp( L0, L1, rs.y );
	
	// Add a little ambient
//	L.xyz += 0.025;
	L.w = saturate( L.w );
	
//	return float4( DiffuseAlbedo, 1.0 );
	return float4( Emissive + Extinction * (DiffuseAlbedo * L.xyz + SpecularAlbedo * L.w), 1.0 );
}


// ===================================================================================
//
technique10 PostProcess
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
