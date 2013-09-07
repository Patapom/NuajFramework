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
	float2	UV				: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	// Fetch bump height in normal direction
	float	BumpHeight = 0.5 * (FetchNormalHeight3D( _In.Position ).w - 0.5);

	// Displace position along normal
	float3	WorldPosition = SpherePosition + SphereRadius * _In.Position * (1.25 + BumpHeight);
//	float4	WorldPosition = mul( float4( _In.Position, 1 ), Local2World );

	PS_IN	Out;
	Out.Position = mul( float4( WorldPosition, 1 ), World2Proj );
	Out.WorldPosition = WorldPosition.xyz;
	Out.WorldNormal = normalize( _In.Position );
	Out.UV = _In.UV;

	return Out;
}

float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	View = normalize( _In.WorldPosition - Camera2World[3].xyz );
	float3	ToLight = normalize( LightPosition - _In.WorldPosition );
	float3	Normal = FetchNormalHeight3D( _In.WorldNormal ).xyz;

	// Compute Fresnel term using Shlick
	float	R0 = 0.4;
	float	ICos = 1.0 - saturate( dot( -View, Normal ) );
	float	ICos2 = ICos*ICos;
 	float	ICos4 = ICos2*ICos2;
 	float	fFresnelView = R0 + (1.0-R0) * (ICos4 * ICos);
//	float	fFresnelView = ComputeComplexFresnel( dot( -View, Normal ), 4.0 );
//	return fFresnelView;

	ICos = 1.0 - dot( ToLight, Normal );
	ICos2 = ICos*ICos;
	ICos4 = ICos2*ICos2;
	float	fFresnelLight = R0 + (1.0-R0) * (ICos4 * ICos);

	// Compute wall reflection
	float3	Reflection = reflect( View, Normal );

	int		HitFaceIndex;
	float2	HitUV, HitX, HitY, HitZ;
	float	HitDistance;
	ComputeWallHit( _In.WorldPosition, Reflection, HitFaceIndex, HitUV, HitX, HitY, HitZ, HitDistance );
	float3	WallColorReflection = ComputeWallColor( HitFaceIndex, HitUV, _In.WorldPosition + HitDistance*Reflection );

	// Compute specular light reflection
	float	Spec = saturate( dot( ToLight, Reflection ) );
	Spec *= Spec;	// ^2
	Spec *= Spec;	// ^4
	Spec *= Spec;	// ^8
//	Spec *= Spec;	// ^16
//	Spec *= Spec;	// ^32
//	Spec *= Spec;	// ^64
	float3	LightReflection = fFresnelLight * Spec.xxx;

	// Compute wall refraction
	float3	WallColorRefraction = INTERNAL_SPHERE_COLOR;
	float3	Refraction = refract( View, Normal, IOR );
	if ( dot( Refraction, Refraction ) > 1e-4 )
	{
		// We must compute the next intersection with the sphere (exit point)
		float3	SphereVertexPosition = _In.WorldPosition - SpherePosition;
		float	HitDistance = -2.0 * dot( SphereVertexPosition, Refraction );	// As the start point already lies on the surface of the sphere, the solution is much simpler !

		// Compute light scattering
		float	Extinction = exp( -INTERNAL_EXTINCTION_COEFFICIENT * HitDistance );
		float	InScattering = (1.0 - Extinction) * INTERNAL_SCATTERING_COEFFICIENT * HitDistance;

 		// Then we bend again...
		float3	ExitNormal = FetchNormalHeight3D( SphereVertexPosition + HitDistance * Refraction ).xyz;
 		float3	NewRefraction = refract( Refraction, ExitNormal, IOR );
		if ( dot( NewRefraction, NewRefraction ) < 1e-4 )
			NewRefraction = reflect( Refraction, ExitNormal );	// Reflect instead

		// Compute hit distance
		ComputeWallHit( _In.WorldPosition, NewRefraction, HitFaceIndex, HitUV, HitX, HitY, HitZ, HitDistance );
		WallColorRefraction = ComputeWallColor( HitFaceIndex, HitUV, _In.WorldPosition + HitDistance*Refraction );

		WallColorRefraction *= Extinction;
		WallColorRefraction += InScattering * INTERNAL_SPHERE_COLOR;
	}

	return float4( lerp( WallColorRefraction, WallColorReflection + LightReflection, fFresnelView ), 1 );
}

// ===================================================================================
technique10 DisplaySphere
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
