// This shader generates the depth & entry maps
//
float4x4	World2View : WORLD2VIEW;	// Transforms a WORLD space coordinate into a VIEW space coordinate (view can either be light or camera)
float4x4	World2Proj : WORLD2PROJ;	// Transforms and projects a WORLD space coordinate into the VIEW render target's projection space
float3		CameraPosition;

// ===================================================================================

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	ViewPosition	: TEXCOORD0;
	float3	ProjNormal		: TEXCOORD1;
};


// ========================
// Light depth map
PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), World2Proj );
	Out.ViewPosition = mul( float4( _In.Position, 1 ), World2View ).xyz;	// In LIGHT space
	Out.ProjNormal = mul( float4( _In.Normal, 0 ), World2Proj ).xyz;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float fSign = -sign( _In.ProjNormal.z );
	return fSign * _In.ViewPosition.z;	// Should accumulate depth (negatively for front faces, positively for back faces)
}


// ========================
// Camera depth map
PS_IN VS2( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), World2Proj );
	Out.ViewPosition = _In.Position;	// Don't transform, keep in world space...
	Out.ProjNormal = _In.Normal;		// Don't transform, keep in world space...

	return Out;
}

float4	PS2( PS_IN _In ) : SV_TARGET0
{
	float3	ToCamera = CameraPosition - _In.ViewPosition;	// Vector pointing to camera in WORLD space
	float	fLength = length( ToCamera );

	float fSign = -sign( dot( _In.ProjNormal, ToCamera ) );
	return fSign * fLength;	// Should accumulate depth (negatively for front faces, positively for back faces)
}


// ========================
// Camera/Light entry map
PS_IN VS3( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), World2Proj );
	Out.ViewPosition = _In.Position;	// Don't transform, keep in world space...
	Out.ProjNormal = _In.Normal;		// Don't transform, keep in world space...

	return Out;
}

float4	PS3( PS_IN _In ) : SV_TARGET0
{
	return float4( _In.ViewPosition, 0 );	// Simply write the position...
}


// ===================================================================================
// First technique computes the depth map in additive blend mode from the LIGHT's POV
technique10 BuildDepthMapLight
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

// ===================================================================================
// Second technique computes the depth map in additive blend mode from the CAMERA's POV
technique10 BuildDepthMapCamera
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}

// ===================================================================================
// Third technique computes the front/back entry points for light and camera
technique10 BuildDepthEntry
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS3() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS3() ) );
	}
}
