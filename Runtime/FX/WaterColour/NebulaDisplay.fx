// This shader displays the propagated light volume using small opaque particles
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "3DNoise.fx"

float3		CellOffset = float3( 64, 64, 32 );			// The offset to reach the center cell from the origin)
float		Nebula2WorldRatio = 0.1;
float		SpriteSizeFactor = 10.0;

float3		LightPosition = float3( 0.0, 0.0, 0.0 );
float		ScatteringAnisotropy = 0.7;					// Scattering direction in [-1,+1]

float		DiffuseFactor = 1.0;
float		SpecularFactor = 1.0;
float		AmbientFactor = 1.0;
float		AlphaPower = 0.5;

float3		SliceInvSize;
Texture3D	SourceDiffusionTexture;
Texture2DArray	CloudTexture;
Texture2DArray	CloudNormalTexture;

struct VS_IN
{
	float4	PositionSize	: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position		: SV_POSITION;
	float3	UV				: TEXCOORD0;
	float3	Position		: POSITION;
	float3	Flux			: FLUX;
//	float4	Color			: COLOR;
};

VS_IN	VS( VS_IN _In ) { return _In; }

[maxvertexcount(4)]
void GS( point VS_IN _In[1], inout TriangleStream<PS_IN> Stream )
{
	float3	WorldPosition = _In[0].PositionSize.xyz;
// 	float	Size = _In[0].PositionSize.w;
// 	float	SpriteIndex = floor( Size );
// 	Size = (Size - SpriteIndex) * SpriteSizeFactor;

	// Sample flux at position
	float3	CellIndex = WorldPosition + CellOffset;
	float3	UVW = CellIndex * SliceInvSize;
	float3	Flux = SourceDiffusionTexture.SampleLevel( LinearClamp, UVW, 0 ).xyz;

	// Compute size & sprite index based on density
	float	Density = saturate( Flux.z );
	float	Size = SpriteSizeFactor * Density;
	float3	Derivatives;
	float	SpriteIndex = 4.0 + 11.5 * pow( Density, 1.0 );// + 2.0 * Noise( 0.0001 * WorldPosition, NoiseTexture0, Derivatives ) - 1.0;

	// Scale down & rotate nebula
	WorldPosition = WorldPosition.xzy * Nebula2WorldRatio;
	Size *= Nebula2WorldRatio;

	// Piss out quad
	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;

	PS_IN	Out;
	Out.Flux = Flux;
	Out.Position = WorldPosition + Size * (-Right + Up); Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj ); Out.UV = float3( 0.0, 1.0, SpriteIndex ); Stream.Append( Out );
	Out.Position = WorldPosition + Size * (-Right - Up); Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj ); Out.UV = float3( 0.0, 0.0, SpriteIndex ); Stream.Append( Out );
	Out.Position = WorldPosition + Size * (+Right + Up); Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj ); Out.UV = float3( 1.0, 1.0, SpriteIndex ); Stream.Append( Out );
	Out.Position = WorldPosition + Size * (+Right - Up); Out.__Position = mul( float4( Out.Position, 1.0 ), World2Proj ); Out.UV = float3( 1.0, 0.0, SpriteIndex ); Stream.Append( Out );
}

// ===================================================================================
float4	PS( PS_IN _In ) : SV_TARGET0
{
	float3	Flux = _In.Flux;

	// Simple color scheme (red for directional, green for diffuse)
// 	float3	Color = float3( 1, 0, 0 ) * Flux.x
// 				  + float3( 0, 10, 0 ) * Flux.y;

	// Some other simple color scheme 
// 	float3	Color = 1.0 * Flux.xxx * Phase;
// 
// 	// Apply isotropic diffuse color
// 	Color += (0.2 * Flux.x * Phase + 50.0 * Flux.y) * float3( 0.01, 0.1, 0.3 );


	// Compute normal
	float2	dXY = 2.0 * (_In.UV - 0.5);
//	float	Z = sqrt( saturate( 1.0 - dot(dXY,dXY) ) );
	float	Z = exp( -0.7 * dot(dXY,dXY) );
	float3	CameraNormal = normalize( float3( dXY, Z ) );

	float3	Right = Camera2World[0].xyz;
	float3	Up = Camera2World[1].xyz;
	float3	At = -Camera2World[2].xyz;
	float3	Normal = normalize( CameraNormal.x * Right + CameraNormal.y * Up + CameraNormal.z * At );
	float3	Tangent = normalize( cross( Up, Normal ) );
	float3	BiTangent = cross( Normal, Tangent );

	// Sample textures
	float4	TextureColor = CloudTexture.Sample( LinearClamp, _In.UV );
	float4	NormalDisp = CloudNormalTexture.Sample( LinearClamp, _In.UV );
	NormalDisp.xyz = 2.0 * NormalDisp.xyz - 1.0;

	float	Thickness = NormalDisp.w;

	// Apply normal map
	Normal = NormalDisp.x * Tangent + NormalDisp.y * BiTangent + NormalDisp.z * Normal;

//	return float4( CameraNormal, 1 );
//	return float4( Normal, 1 );

	// Compute color
	float3	ToPixel = normalize( _In.Position - Camera2World[3].xyz );
	float3	ToLight = normalize( LightPosition * Nebula2WorldRatio - _In.Position );

	// Compute simple Phong shading for directional lighting
	float	DiffuseDotRayleigh = dot( Normal, ToLight );
	float	DiffuseDot = saturate( DiffuseDotRayleigh );
	DiffuseDotRayleigh = 0.25 + 0.5 * DiffuseDotRayleigh*DiffuseDotRayleigh;
	float3	ReflectedView = reflect( ToPixel, Normal );
	float	SpecularDot = DiffuseDotRayleigh * pow( saturate( dot( ReflectedView, ToLight ) ), 1.0 );
	float3	Diffuse = (DiffuseFactor * DiffuseDotRayleigh + SpecularFactor * SpecularDot) * Flux.xxx;
//	float3	Diffuse = DiffuseDotRayleigh * Flux.xxx;

	// Add some translucent ambient
	// To compute phase, we use Shlick's equivalent to Henyey-Greenstein
	//	Fr(θ) = (1-k²) / (1+kcos(θ))^2
	float	Dot = dot( ToPixel, ToLight );
	float	Den = 1.0 / (1.0 - ScatteringAnisotropy * Dot);
	float	Phase = (1.0 - ScatteringAnisotropy*ScatteringAnisotropy) * Den * Den;
	Phase = saturate( Phase );
		// Use Rayleigh
//	float	Phase = 0.5 * (1.0 + Dot*Dot);
	float3	Ambient = AmbientFactor * Flux.y * Phase * float3( 0.01, 0.1, 0.3 );

	// Finalize color
	float3	Color = TextureColor.xyz * lerp( Ambient, Ambient + Diffuse, saturate( 0.0 + 0.8 * Thickness ) ); 
//	float3	Color = TextureColor.xyz * (Ambient + Diffuse); 

	float	Alpha = pow( Flux.z * TextureColor.w, AlphaPower );

	return float4( Color, Alpha );
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ));
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
