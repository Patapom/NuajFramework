// This shader smoothes the fog density across time
//
#include "../Camera.fx"
#include "../Samplers.fx"

float2		BufferInvSize;
float		DeltaTime;
float		SmoothingSpeed;		// The speed at which density should be accomodated (in density units/second)
Texture2D	PreviousDensity;
Texture2D	TargetDensity;

struct VS_IN
{
	float4	Position	: SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

float4	PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float4	Density0 = PreviousDensity.SampleLevel( NearestClamp, UV, 0 );
	float4	Density1 = TargetDensity.SampleLevel( NearestClamp, UV, 0 );

	float	DeltaDensity = abs(Density1-Density0);
	float	t = saturate( SmoothingSpeed * DeltaTime / max( 1e-3, DeltaDensity ) );	// Maximum density ratio we can accomodate that frame...
	return lerp( Density0, Density1, t );
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
