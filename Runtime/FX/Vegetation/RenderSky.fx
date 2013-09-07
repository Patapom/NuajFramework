// Renders sky object as emissive
// This shader operates almost as a post-process except it renders a skydome object instead of a quad.
// The sky color is computed at the vertices rather than per-pixel, which would be too much.
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../Atmosphere/SkySupport.fx"
#include "../ReadableZBufferSupport.fx"
#include "DeferredSupport.fx"

float2		BufferInvSize;
float4x4	SkyDomeRotation;
TextureCube	NightSkyCubeMap;

struct VS_IN
{
	float3	Position	: POSITION;
	float3	View		: NORMAL;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
	float3	View		: VIEW;
	float4	SkyColor	: SKY_COLOR;
	float3	Extinction	: EXTINCTION;
};

PS_IN VS( VS_IN _In )
{
	float3	ViewPosition = Camera2World[3].xyz;
//	float3	RotatedPosition = mul( float4( _In.Position, 1.0 ), SkyDomeRotation ).xyz;	// The dome rotates to align with the Sun
	float3	RotatedPosition = _In.Position;												// The hemispherical skydome doesn't rotate
	float3	WorldPosition = RotatedPosition + ViewPosition;								// The dome moves along with the camera...

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1.0 ), World2Proj );
	Out.View = _In.View;

	// Compute view ray intersection with the upper atmosphere
	float	CameraHeight = WorldUnit2Kilometer * ViewPosition.y;	// Relative height from sea level
	float	D = CameraHeight + EARTH_RADIUS;
	float	b = D * Out.View.y;
	float	c = D*D-ATMOSPHERE_RADIUS*ATMOSPHERE_RADIUS;
	float	Delta = sqrt( b*b-c );
	float	HitDistanceKm = Delta - b;	// Distance at which we hit the upper atmosphere (in kilometers)
			HitDistanceKm /= WorldUnit2Kilometer;

	// Compute sky color
	Out.SkyColor.xyz = ComputeSkyColor( ViewPosition, Out.View, HitDistanceKm, Out.Extinction, Out.SkyColor.w, 32 );
	Out.SkyColor.xyz *= Out.SkyColor.w;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float	Z = ReadDepth( _In.Position.xy );
	if ( Z < 0.99 * CameraData.w )
		return float4( 0.0.xxx, 1.0 );
//	clip( Z - 0.99 * CameraData.w );

	float3	View = normalize( _In.View );

	// Compute Sun color
	// 	149.5978875 = Distance to the Sun in millions of kilometers
	// 	0.695       = Radius of the Sun in millions of kilometers (toupiti !!)
	float	CosAngle = 0.999;	// Cos( SunCoverAngle )
	float	DotSun = dot( View, SunDirection );
	float3	LocalSunColor = DotSun >= CosAngle
							? SunIntensity.xxx	// Either sunlight...
							: 0.0.xxx;			// ...or the black and cold emptiness of space !

	// Compute night sky
	float3	NightSkyColor = (1.0-_In.SkyColor.w) * pow( NightSkyCubeMap.Sample( LinearClamp, View.yzx ).xyz, 2.0 );

	return float4( _In.SkyColor.xyz + _In.Extinction * (LocalSunColor + NightSkyColor), 0.0 );
}


// ===================================================================================
//
technique10 RenderSky
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
