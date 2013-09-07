// This shader computes light diffusion through a 3D volume
//
#include "../Samplers.fx"
#include "3DNoise.fx"

SamplerState VolumeSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Clamp;
};

int			SliceIndex = 0;

float3		CellOffset = float3( 64, 64, 32 );			// The offset to reach the center cell from the origin)
float		CloudTime = 0.0;							// The time to use to animate the noise

float3		LightDirection = float3( 0.0, 0.0, 1.0 );	// Light direction in WORLD space
float		LightFlux = 1.0;							// Light source flux

float		NoiseScale = 0.5;
float		NoiseOffset = -0.4;
float		NoiseSize = 0.4;

float		Sigma_s;									// Scattering coefficient
float		Sigma_t;									// Extinction coefficient = Scattering + Absorption coefficient
float		Sigma_Aerosols;								// Scattering coefficient for aerosols (i.e. Rayleigh)
float		ScatteringAnisotropy = 0.7;					// Scattering direction in [-1,+1]

float3		SliceInvSize;
Texture3D	SourceDiffusionTexture;

// Array of 26 normalized vectors pointing to neighbor cells
static const float3	ToNeighbors[] =
{
	float3( -0.57735, -0.57735, -0.57735 ),
	float3( 0, -0.70711, -0.70711 ),
	float3( 0.57735, -0.57735, -0.57735 ),
	float3( -0.70711, 0, -0.70711 ),
	float3( 0, 0, -1 ),
	float3( 0.70711, 0, -0.70711 ),
	float3( -0.57735, 0.57735, -0.57735 ),
	float3( 0, 0.70711, -0.70711 ),
	float3( 0.57735, 0.57735, -0.57735 ),
	float3( -0.70711, -0.70711, 0 ),
	float3( 0, -1, 0 ),
	float3( 0.70711, -0.70711, 0 ),
	float3( -1, 0, 0 ),
	float3( 1, 0, 0 ),
	float3( -0.70711, 0.70711, 0 ),
	float3( 0, 1, 0 ),
	float3( 0.70711, 0.70711, 0 ),
	float3( -0.57735, -0.57735, 0.57735 ),
	float3( 0, -0.70711, 0.70711 ),
	float3( 0.57735, -0.57735, 0.57735 ),
	float3( -0.70711, 0, 0.70711 ),
	float3( 0, 0, 1 ),
	float3( 0.70711, 0, 0.70711 ),
	float3( -0.57735, 0.57735, 0.57735 ),
	float3( 0, 0.70711, 0.70711 ),
	float3( 0.57735, 0.57735, 0.57735 ),
};

struct VS_IN
{
	float4	Position	: SV_POSITION;
	uint	ID			: SV_INSTANCEID;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
	uint	ID			: SV_RENDERTARGETARRAYINDEX;
};

VS_IN	VS( VS_IN _In )
{
	_In.ID = SliceIndex;
	return _In;
}

[maxvertexcount(3)]
void GS( triangle VS_IN _In[3], inout TriangleStream<PS_IN> Stream )
{
	Stream.Append( (PS_IN) _In[0] );
	Stream.Append( (PS_IN) _In[1] );
	Stream.Append( (PS_IN) _In[2] );
}

// Builds density from noise
//
float	ComputeDensity( float3 _UVW )
{
	float3	Derivatives;
//	float	Density  = 1.0   * Noise( _UVW * 1.0, NoiseTexture0, Derivatives );
//			Density += 0.5   * Noise( _UVW * 2.0, NoiseTexture1, Derivatives );
//			Density += 0.25  * Noise( _UVW * 4.0, NoiseTexture2, Derivatives );
//			Density += 0.125 * Noise( _UVW * 8.0, NoiseTexture3, Derivatives );
//	return Density;

	return LargeNoiseTexture.SampleLevel( VolumeSampler, _UVW, 0 ).x;
}

float3	SampleFlux( float3 _UVW, float3 _Offset )
{
	return SourceDiffusionTexture.SampleLevel( VolumeSampler, _UVW + _Offset * SliceInvSize, 0 ).xyz;
}

// Computes extinction/scattering between 2 cells of different densities
//	_Rho0, the density of the target cell
//	_Rho1, the density of the source cell
//
// For extinction, K = exp( -Tau( x0, x1 ) )
// and Tau( x, x' ) = Int[x,x']{ Sigma_t(s) ds }
// where Sigma_t(s) = Rho(s) * Sigma_t  is dependent on medium density at position s
//
// Tau(x, x') = 0.5 * [Sigma_t(x') + Sigma_t(x)] * Dx  is the average of extinction coefficients times the traveled distance
//
float4	ExtinctionScattering( float _Rho0, float _Rho1 )
{
	float2	Sigma0 = float2( Sigma_t, Sigma_s ) * _Rho0 + Sigma_Aerosols;
	float2	Sigma1 = float2( Sigma_t, Sigma_s ) * _Rho1 + Sigma_Aerosols;
	float2	AvgSigma = 0.5 * (Sigma0 + Sigma1);
	return float4( exp( -AvgSigma ), AvgSigma );
}

// ===================================================================================
// The source/target diffusion buffers contain 3 values :
//	_ The directional flux (i.e. radiance) that propagates radially away from the light position.
//	_ The isotropic flux that propagates in all directions
//	_ The medium density in [0,1]
//
// The idea is that directional flux traveling in a participating medium is either
//	absorbed or scattered based on the extinction coefficient sigma_t = sigma_absorption + sigma_scattering.
// The directional flux that is scattered is then transformed into isotropic flux.
// Meanwhile, isotropic flux of a cell is itself either absorbed by or scattered to other neighbor cells.
//
float3	PS( PS_IN _In ) : SV_TARGET0
{
	float3	CellIndex = float3( _In.Position.xy, _In.ID + 0.5 );
	float3	UVW = CellIndex * SliceInvSize;

	// Sample current cell's flux & density
	float3	CurrentFlux = SampleFlux( UVW, 0.0 );

	/////////////////////////////////////////////////////////////////////////////////////
	// Directional flux is attenuated by extinction (i.e. absorption + scattering)
	float3	PreviousUVW = UVW + LightDirection * SliceInvSize;
	float3	PreviousFluxDirectional = CurrentFlux;
	if ( PreviousUVW.z < 1.0 )
		PreviousFluxDirectional = SampleFlux( PreviousUVW, 0.0 );	// Propagate flux in light's direction
	else
		PreviousFluxDirectional.x = LightFlux;						// Otherwise, use light's own flux

	float	CurrentFluxDirectional = PreviousFluxDirectional.x * ExtinctionScattering( CurrentFlux.z, PreviousFluxDirectional.z ).x;

	/////////////////////////////////////////////////////////////////////////////////////
	// Isotropic flux is affected by neighbors
	float	Extinction = 1.0;
	float	Scattering = 0.0;

	const float	FourPI = 12.566370614359172953850573533118;

	[unroll]
	for ( int NeighborIndex=0; NeighborIndex < 26; NeighborIndex++ )
	{
		float3	ToNeighbor = ToNeighbors[NeighborIndex];
		float3	NeighborFlux = SampleFlux( UVW, ToNeighbor );

		float4	ES = ExtinctionScattering( CurrentFlux.z, NeighborFlux.z );

		// 1] Isotropic flux is attenuated by extinction through neighbors
		//	Phi(x) *= exp( -Tau(x,x') )
//		Extinction *= ES.x;
		Extinction = min( Extinction, ES.x );

		// 2] Isotropic flux is augmented by scattering from neighbors
		//	Phi(x) += exp( -Tau(x,x') ) * Sigma_s * Phi(x')
		Scattering += ES.x * ES.w * NeighborFlux.y;

		// 3] Isotropic flux is augmented by scattering from directional flux (i.e. radiance)
		//	Phi(x) += exp( -Tau(x,x') ) * Sigma_s * p(w,w') * L(x',w)
		// where :
		//	p(w,w') is the phase function
		//	L(x',w) = neighbor radiance
		//	w = direction to light
		//	w' = direction to neighbor cell

		// To compute phase, we use Shlick's equivalent to Henyey-Greenstein
		//	Fr(θ) = (1-k²) / (1+kcos(θ))^2
		float	Dot = dot( ToNeighbor, LightDirection );
		float	Den = 1.0 / (1.0 - ScatteringAnisotropy * Dot);
		float	Phase = (1.0 - ScatteringAnisotropy*ScatteringAnisotropy) * Den * Den;

		Scattering += FourPI * ES.x * ES.w * Phase * NeighborFlux.x;
	}

	const float	Normalizer = 1.0 / 26.0;	// 26 samples
	float	CurrentFluxIsotropic = CurrentFlux.y * Extinction + Scattering * Normalizer;

	/////////////////////////////////////////////////////////////////////////////////////
	// Density is noise
//	UVW.x += 0.002 * CloudTime;// * (1.0 + 0.2 * UVW.z);
	CurrentFlux.z = saturate( NoiseScale * ComputeDensity( NoiseSize * UVW ) + NoiseOffset );

//	CurrentFlux.z = saturate( 0.1 * UVW.x - 0.0 );

	return float3( CurrentFluxDirectional, CurrentFluxIsotropic, CurrentFlux.z );
}

// ===================================================================================
// This pass renders a "deep shadow map" : RGBA will contain noise values for different
//	slices within the layer of clouds.
//
float4	PS2( VS_IN _In ) : SV_TARGET0
{
//	float3	CellIndex = float3( _In.Position.xy, _In.ID + 0.5 );
	return 0.0;
}

// ===================================================================================
technique10 DiffusionPass
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 ShadowPass
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}