// This shader simply displays the caustics texture
//
#include "../Camera.fx"
#include "ComputeWallColor.fx"

// ===================================================================================

struct VS_IN
{
	float3	Position	: POSITION;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	WorldPosition	: POSITION;
	float3	WorldNormal		: NORMAL;
};

PS_IN VS( VS_IN _In )
{
	// Displace position along normal
	float3	WorldPosition = SpherePosition + SphereRadius * _In.Position;

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1 ), World2Proj );
	Out.WorldPosition = WorldPosition;
	Out.WorldNormal = _In.Position;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	View = normalize( Camera2World[3].xyz - _In.WorldPosition );
	float	Dot = saturate( dot( View, _In.WorldNormal ) );
	return float4( LightIntensity * Dot.xxx, 1.0 );
}

PS_IN VS2( VS_IN _In )
{
	// Fetch bump height in normal direction
	float	BumpHeight = 0.5 * (FetchNormalHeight3D( _In.Position ).w - 0.5);

	// Displace position along normal
	float3	WorldPosition = SpherePosition + SphereRadius * _In.Position * (1.25 + BumpHeight);

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1 ), World2Proj );
	Out.WorldPosition = WorldPosition;
	Out.WorldNormal = _In.Position;

	return Out;
}

float4	PS2( PS_IN _In ) : SV_TARGET0
{
	// Bend the rays as they do it in NY baby !
	float3	View = normalize( _In.WorldPosition - Camera2World[3].xyz );
	float3	ToLight = normalize( LightPosition - _In.WorldPosition );
	float3	Normal = FetchNormalHeight3D( _In.WorldNormal ).xyz;

	float3	SphereVertexPosition = _In.WorldPosition;// - SpherePosition;

	// Compute Fresnel term using Shlick
	float	R0 = 0.4;
	float	ICos = 1.0 - saturate( dot( -View, Normal ) );
	float	ICos2 = ICos*ICos;
 	float	ICos4 = ICos2*ICos2;
 	float	Fresnel = R0 + (1.0-R0) * (ICos4 * ICos);

	////////////////////////////////////////////////////////
	// Compute light reflection
	float3	LightColorReflected = 0.0;
	float3	Reflection = reflect( View, Normal );

	// Compute intersection with the light ball using our reflected ray direction
	float3	D = SphereVertexPosition - LightPosition;
	float	b = dot( D, Reflection );
	float	c = dot( D, D ) - LightRadius*LightRadius;
	float	Delta = b*b-c;
	if ( Delta >= 0.0 )
	{	// Compute light reflection's importance
		float	LightHitDistance = -b + sqrt(Delta);
		float3	LightDirection = normalize( SphereVertexPosition + LightHitDistance * Reflection );
		LightColorReflected = 4.0 * saturate( dot( LightDirection, -Reflection ) );
	}

	////////////////////////////////////////////////////////
	float3	LightColorRefracted = 0.0;
	float3	Refraction = refract( View, Normal, IOR );
	if ( dot( Refraction, Refraction ) > 1e-4 )
	{
		// We must compute the next intersection with the sphere (exit point)
		float	ExitHitDistance = -2.0 * dot( SphereVertexPosition, Refraction );	// As the start point already lies on the surface of the sphere, the solution is much simpler !

		// Compute intersection with the light ball using our refracted ray direction
		float3	D = SphereVertexPosition - LightPosition;
		float	b = dot( D, Refraction );
		float	c = dot( D, D ) - LightRadius*LightRadius;
		float	Delta = b*b-c;
		float	LightHitDistance = Delta >= 0.0 ? -b - sqrt(Delta) : 1e4f;

		float	LightPhase = 0.0;
		float	MediumDistance = 0.0;
		if ( LightHitDistance < ExitHitDistance )
		{	// We hit the light ball before the exit point : light is within the sphere !
			float3	LightDirection = normalize( SphereVertexPosition + LightHitDistance * Refraction );
			LightPhase = saturate( dot( LightDirection, -Refraction ) );
			MediumDistance = LightHitDistance;	// We only roam the medium till we hit the light
		}
		else
		{	// We must make the ray exit the sphere and exit again
			MediumDistance = ExitHitDistance;

			SphereVertexPosition += ExitHitDistance * Refraction ;
			float3	ExitNormal = FetchNormalHeight3D( SphereVertexPosition ).xyz;
 			float3	NewRefraction = refract( Refraction, ExitNormal, IOR );
			if ( dot( NewRefraction, NewRefraction ) < 1e-4 )
				NewRefraction = reflect( Refraction, ExitNormal );	// Reflect instead

			// Compute intersection with the light ball with our exit ray direction
			float3	D = SphereVertexPosition - LightPosition;
			float	b = dot( D, NewRefraction );
			float	c = dot( D, D ) - LightRadius*LightRadius;
			float	Delta = b*b-c;
			if ( Delta >= 0 )
			{
				LightHitDistance = -b + sqrt(Delta);
				float3	LightDirection = normalize( SphereVertexPosition + LightHitDistance * NewRefraction );
				LightPhase = saturate( dot( LightDirection, -NewRefraction ) );
			}
		}

		// Compute light scattering
		float	Extinction = exp( -INTERNAL_EXTINCTION_COEFFICIENT * MediumDistance );
		float	InScattering = (1.0 - Extinction) * INTERNAL_SCATTERING_COEFFICIENT * MediumDistance;

		LightColorRefracted = LightPhase.xxx * Extinction;
		LightColorRefracted += LightPhase * InScattering * INTERNAL_SPHERE_COLOR;
	}

	return float4( lerp( LightColorRefracted, LightColorReflected, Fresnel ), 1.0 );
}

// ===================================================================================
technique10 DrawLensFlare
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 DrawDeformedSphere
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
