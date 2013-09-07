// Reaction-Diffusion Simulation
//
#include "../Samplers.fx"

const static float	h = 0.1;			// Space between cells
const static float2	InvTextureSize = float2( 1.0 / 512.0, 0.0 );
const static float	TimeStep = 0.2;		// A simulation time step

const static float	Ru = 0.01;			// U diffusion rate
const static float	Rv = 0.005;			// V diffusion rate


Texture2D	SourceTexture;
float		F, K;
float		DrawStrength = 0.0;
float2		DrawCenter = float2( 0.5, 0.5 );
bool		bShowEnvironment = true;
TextureCube	EnvironmentTexture;

struct VS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
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
	float4	Previous = SourceTexture.SampleLevel( NearestClamp, _In.UV, 0 );
	float	u = Previous.x;
	float	v = Previous.y;

	// Evaluate laplacian for both U and V
	float4	PreviousXm = SourceTexture.SampleLevel( NearestWrap, _In.UV - InvTextureSize.xy, 0 );	// x-h
	float4	PreviousXp = SourceTexture.SampleLevel( NearestWrap, _In.UV + InvTextureSize.xy, 0 );	// x+h
	float4	PreviousYm = SourceTexture.SampleLevel( NearestWrap, _In.UV - InvTextureSize.yx, 0 );	// y-h
	float4	PreviousYp = SourceTexture.SampleLevel( NearestWrap, _In.UV + InvTextureSize.yx, 0 );	// y+h
	float4	Laplacian = (PreviousXm + PreviousXp + PreviousYm + PreviousYp - 4.0 * Previous) / (h*h);

	// The time derivatives of u and v quantities are given by the Gray-Scott model :
	// (from http://groups.csail.mit.edu/mac/projects/amorphous/GrayScott/)
	//
	//	∂u/∂t = Ru Δ²u - uv² + f(1-u)
	//	∂v/∂t = Rv Δ²v + uv² - (f+k)v
	//
	// For the chemical reaction :
	//
	//	U + 2V -> 3V
	//	V -> P
	//
	// Where :
	//	U, V and P are chemical species
	//	u and v are U and V's concentrations
	//	ru and rv are their diffusion rates
	//	k represents the rate of conversion from V to P
	//	f represents the rate of process that feeds U and drains U,V and P
	//
	float	uv2 = u*v*v;
	float	DuDt = Ru * Laplacian.x - uv2 + F * (1.0 - u);
	float	DvDt = Rv * Laplacian.y + uv2 - (F+K) * v;

	// Use simple integration scheme with a constant time step
	u += DuDt * TimeStep;
	v += DvDt * TimeStep;

	// Draw some stuff if authorized to
	if ( DrawStrength > 0.0 )
	{
		float	Distance2Center = length( _In.UV - DrawCenter );
		float	Intensity = DrawStrength * 0.1 * exp( -1000.0 * Distance2Center*Distance2Center );
		u += Intensity * TimeStep;
		v += 0.5 * Intensity * TimeStep;
	}

	return float4( u, v, 0.0, 0.0 );
}

float4 PS2( PS_IN _In ) : SV_Target
{
	// Direct result
	if ( !bShowEnvironment )
		return SourceTexture.SampleLevel( LinearClamp, _In.UV, 0 );

	// Normal sampling a cube map
	float4	VXm = SourceTexture.SampleLevel( LinearWrap, _In.UV - InvTextureSize.xy, 0 );
	float4	VXp = SourceTexture.SampleLevel( LinearWrap, _In.UV + InvTextureSize.xy, 0 );
	float4	VYm = SourceTexture.SampleLevel( LinearWrap, _In.UV - InvTextureSize.yx, 0 );
	float4	VYp = SourceTexture.SampleLevel( LinearWrap, _In.UV + InvTextureSize.yx, 0 );

// 	// Show gradient
// 	float2	GradU = float2( VXp.x - VXm.x, VYp.x - VYm.x );
// 	float2	GradV = float2( VXp.y - VXm.y, VYp.y - VYm.y );
// 	return float4( 4.0 * GradV, 0.0, 1.0 );

	float4	V = SourceTexture.SampleLevel( LinearWrap, _In.UV, 0 );
	float	PipoAO = max( 0.0, VXm.x - V.x ) + max( 0.0, VXp.x - V.x ) + max( 0.0, VYm.x - V.x ) + max( 0.0, VYp.x - V.x );
	PipoAO = 1.0 - saturate( 10.0 * PipoAO );
//	return float4( PipoAO.xxx, 1.0 );

	float3	Dx = float3( h, 0.0, VXp.x ) - float3( -h, 0.0, VXm.x );
	float3	Dy = float3( 0.0, h, VYp.x ) - float3( 0.0, -h, VYm.x );
	float3	Normal = cross( Dx, Dy );
	Normal = normalize( Normal );

	float3	CameraPos = float3( 0.0, 0.0, 6.0 );
	float3	PixelPos = 10.0 * float3( 1.0-2.0*_In.UV.xy, 0.0 );
	float3	View = normalize( PixelPos - CameraPos );
	float3	ReflectedView = reflect( View, Normal );

	float3	Environment = EnvironmentTexture.SampleLevel( LinearWrap, ReflectedView.yzx, 0 ).xyz;

	// Add some lighting
	float3	LightPosition = 10.0 * float3( 1.0-2.0*DrawCenter, 1.0 );
	float3	ToLight = normalize( LightPosition - PixelPos );
	float	Light = pow( dot( ToLight, Normal ), 8.0 );					// Some "diffuse"
	Light += pow( saturate( dot( ToLight, ReflectedView ) ), 100.0 );	// Some specular

	return float4( PipoAO * (Environment + 0.7 * Light.xxx), 1.0 );
}

technique10 ReactionDiffusion
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 Display
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ));
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
