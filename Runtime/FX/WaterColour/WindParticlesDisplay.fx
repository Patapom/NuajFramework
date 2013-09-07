// This shader display the particles animated by wind
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "GBufferSupport.fx"
#include "ShadowMapSupport.fx"
#include "LightSupport.fx"

int			ParticlesCount;
Texture2D	TexturePreviousPositions;
Texture2D	TextureCurrentPositions;
float		VelocitySpread = 1.0;
float		AmbientFactor = 0.05;
float		DiffuseFactor = 0.1;
float		SpecularFactor = 1.0;
float		SpecularPower = 2.0;

// ===================================================================================

struct VS_IN
{
	uint	Index		: SV_VERTEXID;
	float	Radius		: RADIUS;
	float4	Color		: COLOR;
};

struct GS_IN
{
	float3	Position	: POSITION;
	float	Radius		: RADIUS;
	float4	Color		: COLOR;
	float3	Velocity	: VELOCITY;		// Velocity in WORLD space
	float2	Shadow		: SHADOW;		// Shadow term for key & rim light
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;		// Position in WORLD space
	float2	UV			: TEXCOORD0;
	float4	Color		: COLOR;
	float3	Velocity	: VELOCITY;		// Velocity in WORLD space
	float2	Shadow		: SHADOW;		// Shadow term for key & rim light
};

// ===================================================================================
// Render particles
GS_IN VS( VS_IN _In )
{
	float2	UV = float2( float( _In.Index ) / ParticlesCount, 0.5 );
	float3	PreviousPosition = TexturePreviousPositions.SampleLevel( NearestClamp, UV, 0 ).xyz;
	float3	CurrentPosition = TextureCurrentPositions.SampleLevel( NearestClamp, UV, 0 ).xyz;

	// Prevent brutal loops
	if ( length( CurrentPosition - PreviousPosition ) > 4.0 )
		PreviousPosition = CurrentPosition;

//PreviousPosition = CurrentPosition - float3( 0.2, 0.0, 0.0 );

	GS_IN	Out;
	Out.Position = CurrentPosition;
	Out.Radius = _In.Radius;
	Out.Color = _In.Color;
	Out.Velocity = CurrentPosition - PreviousPosition;
	Out.Shadow = float2(
		ComputeShadowKey( CurrentPosition ),
		ComputeShadowRim( CurrentPosition )
		);

	return Out;
}

[maxvertexcount(2*4)]
void GS( point GS_IN _In[1], inout TriangleStream<PS_IN> Stream )
{
	float3	Up = normalize( cross( Camera2World[2].xyz, _In[0].Velocity ) );
	float	VelocityLength = length( _In[0].Velocity );
	float3	Right = _In[0].Velocity / VelocityLength;

 	VelocityLength *= VelocitySpread;

	float	Radius = _In[0].Radius;

	// Expand particle in camera plane
	float	ExpandFactor = 1.0;// - PhaseVelocity;

	PS_IN	Out;
	Out.Color = _In[0].Color;
	Out.Velocity = _In[0].Velocity;
	Out.Shadow = _In[0].Shadow;
	Out.UV = float2( -VelocityLength/Radius, +1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );

	Out.UV = float2( -VelocityLength/Radius, -1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );

	Out.UV = float2( 1.0, +1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );

	Out.UV = float2( 1.0, -1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );
	Stream.RestartStrip();

	// Expand ortho particle
	ExpandFactor = 1.0;//PhaseVelocity;

	Up = Camera2World[1].xyz;
	Right = Camera2World[0].xyz;

	Out.UV = float2( -1.0, +1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );
	Out.UV = float2( -1.0, -1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );
	Out.UV = float2( +1.0, +1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );
	Out.UV = float2( +1.0, -1.0 );
	Out.Position = _In[0].Position + ExpandFactor * Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );
}

PS_OUT PS( PS_IN _In )
{
	PS_OUT	Out;

	float2	UV = _In.UV;
	float	Distance2Center = length( UV );
//	clip( 1.0 - Distance2Center );	// Particles are spheres...

	float4	Color = 0.0;
	float3	Velocity = -0.4 * _In.Velocity;
	if ( Distance2Center < 1.0 )
	{
		// Compute normal in WORLD space
		float3	NormalCamera = float3( UV, -sqrt( 1.0 - Distance2Center*Distance2Center ) );
		float3	Normal = mul( float4( NormalCamera, 0.0 ), Camera2World ).xyz;

		// Compute view vector
		float3	ToPixel = normalize( _In.Position - Camera2World[3].xyz );

		// Compute lighting
		float4	Diffuse, Specular;
		ComputeLighting( _In.Position, Normal, ToPixel, 2.0, Diffuse, Specular );

		Color = _In.Color * (AmbientFactor + DiffuseFactor * Diffuse + SpecularFactor * Specular);
		Color += _In.Color * (1.0 - Diffuse) * pow( 1.0 - Distance2Center, 0.1 );
		Color.w = 1.0;//-NormalCamera.z;// 0.0;//_In.Color.w;
//		Color = float4( 1, 0, 0, 1 );
//		Color = float4( Color.www, 1 );
//		Color = float4( Normal, 1 );

//		Velocity = 0.4 * _In.Velocity;
	}

//	Color = float4( 1, 0, 0, 1 );
//	Color = float4( 0.5 * (1.0+UV), 0, 1 );
//	Color = float4( abs(0.5*(1.0+UV)), 0, 1 );

	Out.Color0 = Color;
	Out.Color1 = float4( Velocity, 1.0 );

	return Out;
}

// ===================================================================================
// Same particles but without velocity spread
[maxvertexcount(4)]
void GS2( point GS_IN _In[1], inout TriangleStream<PS_IN> Stream )
{
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	float	Radius = _In[0].Radius;

	// Expand particle in camera plane
	PS_IN	Out;
	Out.Color = _In[0].Color;
	Out.Velocity = _In[0].Velocity;
	Out.UV = float2( -1.0, +1.0 );
	Out.Shadow = _In[0].Shadow;
	
	Out.Position = _In[0].Position + Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );

	Out.UV = float2( -1.0, -1.0 );
	Out.Position = _In[0].Position + Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );

	Out.UV = float2( 1.0, +1.0 );
	Out.Position = _In[0].Position + Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );

	Out.UV = float2( 1.0, -1.0 );
	Out.Position = _In[0].Position + Radius * (Right * Out.UV.x + Up * Out.UV.y);
	Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj );
	Stream.Append( Out );
	Stream.RestartStrip();
}

PS_OUT PS2( PS_IN _In )
{
	float2	UV = _In.UV;
	float	SqDistance2Center = dot( UV,UV );
	clip( 1.0 - SqDistance2Center );	// Particles are spheres...

	// Compute normal in WORLD space
	float3	NormalCamera = float3( UV, -sqrt( 1.0 - SqDistance2Center ) );
	float3	Normal = mul( float4( NormalCamera, 0.0 ), Camera2World ).xyz;

	// Compute view vector
	float3	ToPixel = normalize( _In.Position - Camera2World[3].xyz );
 	float3	ReflectedView = reflect( ToPixel, Normal );

	// Compute lighting
 	float4	Diffuse = 0.0, Specular = 0.0;
	float3	ToLight;

	float	SpotAttenuation = ComputeSpotLightAttenuationKey( _In.Position, ToLight );
 	float	DotLight = dot( ToLight, Normal );
 	float	LightPhase = 0.5 * (1.0 + DotLight*DotLight);
	Diffuse += _In.Shadow.x * LightColorKey * SpotAttenuation * LightPhase;
//	Specular += _In.Shadow.x;

	SpotAttenuation = ComputeSpotLightAttenuationRim( _In.Position, ToLight );
 	DotLight = dot( ToLight, Normal );
 	LightPhase = 0.5 * (1.0 + DotLight*DotLight);
	Diffuse += _In.Shadow.y * LightColorRim * SpotAttenuation * LightPhase;
//	Specular += _In.Shadow.y;

	float4	Color = _In.Color * (AmbientFactor + DiffuseFactor * Diffuse + SpecularFactor * Specular);

	PS_OUT	Out;
	Out.Color0 = Color;
	Out.Color1 = float4( 1.0 * _In.Velocity, 1.0 );

	return Out;
}

// ===================================================================================
technique10 DisplayParticles
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS2() ) );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}

technique10 DisplayParticlesSpread
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
