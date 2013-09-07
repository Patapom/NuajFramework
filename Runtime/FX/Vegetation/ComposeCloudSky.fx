// Composes the cloud layers and sky
//
#include "../Camera.fx"
#include "../Samplers.fx"

static const float3	LUMINANCE = float3( 0.2126, 0.7152, 0.0722 );	// RGB => Y (taken from http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering#Light_Adaptation)

float2		BufferInvSize;

// The four possible cloud layers + sky map
Texture2D	CloudLayerTexture0;
Texture2D	CloudLayerTexture1;
Texture2D	CloudLayerTexture2;
Texture2D	CloudLayerTexture3;
Texture2D	SkyTexture;
Texture2D	ShadowMap;

float3		SunDirection;
float3		SunIntensity;
TextureCube	NightSkyCubeMap;

struct VS_IN
{
	float4	Position	: SV_POSITION;	// Depth-pass renderables need only declare a position
};

VS_IN VS( VS_IN _In )	{ return _In; }


// ===================================================================================

float4	PS( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	View = mul( float4( GetCameraView( UV ), 0 ), Camera2World ).xyz;
	float3	Pos = Camera2World[3].xyz;

//return float4( ShadowMap.SampleLevel( LinearClamp, UV, 0.0 ).xyz, 0.0 );

	// Compute Sun color
	// 	149.5978875 = Distance to the Sun in millions of kilometers
	// 	0.695       = Radius of the Sun in millions of kilometers (toupiti !!)
	float	CosAngle = 0.999;	// Cos( SunCoverAngle )
	float	DotSun = dot( View, SunDirection );
	float3	LocalSunColor = DotSun >= CosAngle
							? SunIntensity		// Either sunlight...
							: 0.0.xxx;			// ...or the black and cold emptiness of space !

	// Compute night sky
	float	Terminator = 1.0;	// TODO !!
	float3	NightSkyColor = Terminator * pow( NightSkyCubeMap.Sample( LinearClamp, View.yzx ).xyz, 2.0 );

	float3	BackgroundColor = LocalSunColor + NightSkyColor;

	// Read the 4 cloud layers + sky
	float4	CloudColor0 = CloudLayerTexture0.SampleLevel( LinearClamp, UV, 0.0 );
	float4	CloudColor1 = CloudLayerTexture1.SampleLevel( LinearClamp, UV, 0.0 );
	float4	CloudColor2 = CloudLayerTexture2.SampleLevel( LinearClamp, UV, 0.0 );
	float4	CloudColor3 = CloudLayerTexture3.SampleLevel( LinearClamp, UV, 0.0 );
	float4	SkyColor = SkyTexture.SampleLevel( LinearClamp, UV, 0.0 );

//	return CloudColor0;
// return float4( SkyColor.xyz, 0.0 );
// return SkyColor;

	// Compose
	float3	Extinction = SkyColor.w;
//	float3	Scattering = CloudColor0.xyz * CloudColor1.w + CloudColor1.xyz * CloudColor2.w + CloudColor2.xyz * CloudColor3.w + CloudColor3.xyz * SkyColor.w;
	float3	Scattering = (CloudColor0.xyz + CloudColor1.xyz + CloudColor2.xyz + CloudColor3.xyz) * Extinction + SkyColor.xyz;

	return float4( BackgroundColor * Extinction + Scattering, dot(Extinction, LUMINANCE) );
}

// ===================================================================================
//
technique10 Compose
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
