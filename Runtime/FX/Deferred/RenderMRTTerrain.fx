// Renders geometry terrain buffer into the deferred rendering render targets
// Also render individual terrain tiles into textures
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"
#include "2DNoise.fx"

// Terrain tile rendering
float			TileSize;
float			TileUVFactor;			// TILE_MAX_VERTEX / (TILE_MAX_VERTEX-1)
float2			TilePosition;			// Tile Position (in TILES units)

static const int	FBM_OCTAVES_COUNT = 8;
float	TerrainWorldPosition2NoiseScale = 0.0002;
float	TerrainHeightOffset = -100.0;
float	TerrainHeightScale = 100.0;
float	TextureScale = 0.04;			// Scale factor from WORLD position XZ to local textures

static const float2	WORLD2GLOBALTEXTURE_SCALE = float2( 0.001, 0.001 );	// Scale factor from WORLD position XZ to global texture

// Terrain material building
Texture2D		TerrainGeometry;		// The terrain geometry (XYZ=WorldNormal W=Distance)
Texture2DArray	TerrainAtlas : TEX_ATLAS;
Texture2D		TerrainMixTexture : TEX_MIX;


struct VS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float3	View				: VIEW;
	float2	UV					: TEXCOORD0;
};

struct PS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float2	UV					: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.TransformedPosition = _In.TransformedPosition;
	Out.UV = _In.UV;
	
	return	Out;
}

// =====================================================================================
// Noise

/* Mandelbulb tracing from http://www.fractalforums.com/mandelbulb-implementation/realtime-renderingoptimisations/

//--------------------------------------------------------------------------------------
// Constant Buffer Variables
//--------------------------------------------------------------------------------------
TextureCube txEnv;

SamplerState samLinear
{
    Filter = MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
};

cbuffer cbNeverChanges
{
    matrix View;
};

cbuffer cbChangeOnResize
{
    matrix Projection;
    float2 vReverseRes;
};

cbuffer cbChangesEveryFrame
{
    matrix World;
    matrix InvWorldViewProjection;
    matrix InvProjection;
    float4 vMeshColor;
};

struct VS_INPUT
{
    float4 Pos : POSITION;
    float3 Tex : TEXCOORD;
};

struct GS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Tex : TEXCOORD0;
    float4 View: POSITION;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Tex1 : TEXCOORD0;
    float3 Tex2 : TEXCOORD1;
};

//--------------------------------------------------------------------------------------
// Vertex Shader
//--------------------------------------------------------------------------------------
PS_INPUT QuakeVS( VS_INPUT input )
{
    PS_INPUT output = (PS_INPUT)0;
    input.Pos.z = 1;
    output.Tex1.xyz = 0.15+vMeshColor.xyz*0.05;   //WSAD movement
    output.Tex2 = normalize(mul( input.Pos, InvWorldViewProjection ));
    output.Tex1 += output.Tex2*0.01;
    output.Pos =input.Pos;
    output.Pos.z = 0;
    
    return output;
}

GS_INPUT VS( VS_INPUT input )
{
    GS_INPUT output = (GS_INPUT)0;
    output.Pos = mul( input.Pos, World );
    output.Pos = mul( output.Pos, View );
    output.View = output.Pos;
    output.Pos = mul( output.Pos, Projection );
    output.Tex = input.Tex;
    
    return output;
}

[maxvertexcount(3)]
void GS( triangle GS_INPUT input[3], inout TriangleStream<PS_INPUT> TriStream )
{
    PS_INPUT output = (PS_INPUT)0;

    float3x3 m,n;
    m[0] = input[1].Tex - input[0].Tex;
    m[1] = input[2].Tex - input[0].Tex;
    m[2] = cross(m[0], m[1]);

    n[0] = normalize(input[1].View - input[0].View);
    n[1] = normalize(input[2].View - input[0].View);
    n[2] = cross(n[0], n[1]);
    
    for(int i=0; i<3; i++)
    {
        output.Pos = input[i].Pos;
        output.Tex1 = input[i].Tex;
        float3 Norm;
        Norm = input[i].View;
        Norm = mul(n,Norm);
        Norm = -mul(Norm,m);
        output.Tex2 = Norm;
        
        TriStream.Append( output );
    }
    TriStream.RestartStrip();
}

// power
#define P 8
inline void powN1(inout float3 z, float zr0, inout float dr) {
//  float zr = sqrt( dot(z,z) );
  float zo0 = asin( z.z/zr0 );
  float zi0 = atan2( z.y,z.x );

  float zr = pow( zr0, P-1 );
  float zo = zo0 * P;
  float zi = zi0 * P;
  
  dr = zr*dr*P + 1;
  zr *= zr0;
  z  = zr*float3( cos(zo)*cos(zi), cos(zo)*sin(zi), sin(zo) );
}

inline float DE(float3 z0)
{
  float3 z=z0;
  float r;
  float dr=1;
  int i=20;                   //max iteration count
  r=length(z);
  while(r<16. && i--) {
    powN1(z,r,dr);
    z+=z0;
    r=length(z);
  }
  return -0.5*log(r)*r/dr;
}

// 5% faster but pow8 only,  rename to use
inline float DE1(float3 z0)
{
  float3 z=z0;
  float r,r2;
  float dr=1;
  int i=4;                   //max iteration count
  r2=dot(z,z);
  r =sqrt(r2);
  while(r<2 && i--) {
    float zo0 = asin( z.z/r );
    float zi0 = atan2( z.y,z.x );

    float zr = r2*r2*r2*r;//pow( zr0, P-1 );
    float zo = zo0 * P;
    float zi = zi0 * P;
    
    dr = zr*dr*P + 1;
    zr *= r;
    z  = zr*float3( cos(zo)*cos(zi), cos(zo)*sin(zi), sin(zo) );

    z+=z0;
    r2=dot(z,z);
    r =sqrt(r2);
  }
  return -0.5*log(r)*r/dr;
}

inline float Tex(float3 t)
{
   float c2 = DE( t );
   return c2;
}

inline float3 CalcNorm(float3 t, float c)
{
   float delta=4.0/25600.0;
   float3 tx1 = t;
   tx1.x+=delta;
   float cx1 = Tex( tx1 );
   float3 ty1 = t;
   ty1.y+=delta;
   float cy1 = Tex( ty1 );
   float3 tz1 = t;
   tz1.z+=delta;
   float cz1 = Tex( tz1 );
   float3 d1 = float3(c-cx1,c-cy1,c-cz1);
   return normalize(d1);//*25600;
}

inline float3 CalcNormDD(float3 t, float c)
{
   float3 n1=ddx(t);
   float3 n2=ddy(t);
   return normalize(cross(n1,n2));
}

inline void Ray1(inout float3 t, inout float c, in float3 Norm)
{
   //max raytrace step count
   //distance threshold
   float3 t0 = t;
   for (int i = 0;i<450;i++) { t += .75*Norm*c; c = Tex(t);  [branch] if(c>-0.0003*length(t-t0)) break;       }; 
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 PS2( PS_INPUT input) : SV_Target
{
   float3 Norm = normalize(input.Tex2);
   float3 t = input.Tex1;

   t-=0.5;t*=2.5;

   float c;
   c = Tex(t);
   Ray1(t,c,Norm);

   float3 dx = CalcNorm(t,c);

   float ao=-Tex(t+dx*0.05)*40+0.2;

   float3 reflVec = reflect(Norm,dx);
   float3 refl = txEnv.Sample( samLinear, -reflVec.zxy);

   float l =dot(dx,Norm);
   l *= l;

//   return float4(0,ao,0,c*256+1.0f);// * vMeshColor;+vMeshColor.x*4
   return float4(refl*2*l*ao,c*256*0.4+1.0f);// * vMeshColor;+vMeshColor.x*4
}

float4 PS2AA( PS_INPUT input) : SV_Target
{
  float3 x=ddx(input.Tex1)*0.5;
  float3 y=ddy(input.Tex1)*0.5;
  float3 nx=ddx(input.Tex2)*0.5;
  float3 ny=ddy(input.Tex2)*0.5;
  float4 c;
  c=PS2(input);
  input.Tex1+=x;
  input.Tex2+=nx;
  c+=PS2(input);
  input.Tex1+=y;
  input.Tex2+=ny;
  c+=PS2(input);
  input.Tex1-=x;
  input.Tex2-=nx;
  c+=PS2(input);
  return c*0.25;
}

float4 PS2AAA( PS_INPUT input) : SV_Target
{
  float3 x=ddx(input.Tex1)*0.5;
  float3 y=ddy(input.Tex1)*0.5;
  float3 nx=ddx(input.Tex2)*0.5;
  float3 ny=ddy(input.Tex2)*0.5;
  float4 c;
  c=PS2AA(input);
  input.Tex1+=x;
  input.Tex2+=nx;
  c+=PS2AA(input);
  input.Tex1+=y;
  input.Tex2+=ny;
  c+=PS2AA(input);
  input.Tex1-=x;
  input.Tex2-=nx;
  c+=PS2AA(input);
  return c*0.25;
}

BlendState NoBlending
{
    AlphaToCoverageEnable = FALSE;
    BlendEnable[0] = FALSE;
};

BlendState SrcBlending
{
    AlphaToCoverageEnable = FALSE;
    BlendEnable[0] = TRUE;
    SrcBlend = SRC_ALPHA;
    DestBlend = INV_SRC_ALPHA;
    BlendOp = ADD;
};

DepthStencilState DisableDepth
{
    DepthEnable = FALSE;
    DepthWriteMask = ZERO;
};

//--------------------------------------------------------------------------------------
technique10 Render1
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( NULL );
        SetPixelShader( NULL );
        SetBlendState( SrcBlending, float4( 0.0f, 0.0f, 0.0f, 0.0f ), 0xFFFFFFFF );
        SetDepthStencilState( DisableDepth, 0 );
    }
}

technique10 Render2
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( CompileShader( gs_4_0, GS() ) );
        // PS2AA  for 4x antialiasing
        // PS2AAA for 16x antialiasing
        SetPixelShader( CompileShader( ps_4_0, PS2() ) );
        SetBlendState( SrcBlending, float4( 0.0f, 0.0f, 0.0f, 0.0f ), 0xFFFFFFFF );
        SetDepthStencilState( DisableDepth, 0 );
//        SetPixelShader( NULL );
    }
}

// fly mode
technique10 RenderQuad
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, QuakeVS() ) );
        SetGeometryShader( NULL );
//        SetPixelShader( NULL );
        // PS2AA  for 4x antialiasing
        // PS2AAA for 16x antialiasing
        SetPixelShader( CompileShader( ps_4_0, PS2() ) );
        SetDepthStencilState( DisableDepth, 0 );
    }
}
*/

float	fbm( float2 _Position, out float2 _SumDerivatives )
{
	_Position *= TerrainWorldPosition2NoiseScale;
	_SumDerivatives = 0.0.xx;

	float	Value = 0.0;
	float	Weight = 1.0;
	for( int i=0; i < FBM_OCTAVES_COUNT; i++ )
	{
		float2	Derivative;
		float	NoiseValue = Noise( _Position, NoiseTexture0, Derivative );

		_SumDerivatives += Weight * Derivative;
//		Value += Weight * NoiseValue;													// Standard fbm
//		Value += Weight * NoiseValue / (1.0 + dot(_SumDerivatives,_SumDerivatives));	// Nice modification by Iñigo Quilez (from http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
		Value += Weight * NoiseValue / (1.0 + dot(Derivative,Derivative));	// Nice modification by Iñigo Quilez (from http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)

//		Weight *= 0.25;		// w=1/2^(b*i) with b=2. Lipschitz condition = 3*(2-1/(2^(n-1)) <= 3 implying a maximum limit to the slope
		Weight *= 0.5;		// w=1/2^(b*i) with b=1. Lipschitz condition = 3*n implying a maximum limit to the slope
		_Position *= 2.0;
	}

	return Value;
}

float	fbmDeriv2( float2 _Position, out float2 _SumDerivatives, out float2 _SumDerivatives2 )
{
	_Position *= TerrainWorldPosition2NoiseScale;
	_SumDerivatives = 0.0.xx;
	_SumDerivatives2 = 0.0.xx;

	float	Value = 0.0;
	float	Weight = 1.0;
	for( int i=0; i < FBM_OCTAVES_COUNT; i++ )
	{
		float2	Derivative, Derivative2;
		float	NoiseValue = NoiseDeriv2( _Position, NoiseTexture0, Derivative, Derivative2 );

		_SumDerivatives += Weight * Derivative;
		_SumDerivatives2 += Weight * Derivative2;
		Value += Weight * NoiseValue;													// Standard fbm
//		Value += Weight * NoiseValue / (1.0 + dot(_SumDerivatives,_SumDerivatives));	// Nice modification by Iñigo Quilez (from http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
//		Value += Weight * NoiseValue / (1.0 + dot(Derivative,Derivative));	// Nice modification by Iñigo Quilez (from http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)

//		Weight *= 0.25;		// w=1/2^(b*i) with b=2. Lipschitz condition = 3*(2-1/(2^(n-1)) <= 3 implying a maximum limit to the slope
		Weight *= 0.5;		// w=1/2^(b*i) with b=1. Lipschitz condition = 3*n implying a maximum limit to the slope
		_Position *= 2.0;
	}

	return Value;
}

// =====================================================================================
// Renders the terrain tile into the TPage
//
float4 PS( PS_IN _In ) : SV_TARGET0
{
	// Compute position where to evaluate fbm noise
	float2	WorldPosition = (TilePosition + _In.UV * TileUVFactor - 0.5) * TileSize;

	// Evaluate noise
	float2	Derivatives;
//	float	Height = TerrainHeightOffset + TerrainHeightScale * fbm( WorldPosition, Derivatives );
//Height = 0.5 * (WorldPosition.x + WorldPosition.y);
	float2	Derivatives2;
	float	Height = TerrainHeightOffset + TerrainHeightScale * fbmDeriv2( WorldPosition, Derivatives, Derivatives2 );

	float	Attenuation = 1.0;
	float3	WorldNormal = cross( float3( 0.0, Derivatives.y, Attenuation ), float3( Attenuation, Derivatives.x, 0.0 ) );

	float	Deriv2Length = 1.0 - saturate( 0.05 * length( Derivatives2 ) );
	WorldNormal *= Deriv2Length;

	return float4( WorldNormal, Height );
//	return float4( WorldNormal, 10.0 * length( Derivatives2 ) );
}


// =====================================================================================
// Renders the terrain material & geometry into the deferred targets
//
PS_OUTZ PS2( PS_IN _In )
{
	// Read world normal and depth from pre-rendered geometry
	float4	GeometryValue = TerrainGeometry.SampleLevel( NearestClamp, _In.UV, 0 );
	clip( CameraData.w - GeometryValue.w );

//	return WriteDeferredMRTWithDepth( float3( _In.UV, 0 ), GeometryValue.www, 0.0, 1.0.xxx, GeometryValue.w );
//	return WriteDeferredMRTWithDepth( normalize( GeometryValue.xyz ), GeometryValue.www, 0.0, 1.0.xxx, GeometryValue.w );
//	return WriteDeferredMRTWithDepth( GeometryValue.www, GeometryValue.www, 0.0, 1.0.xxx, GeometryValue.w );

//	float	Depth = GeometryValue.w - 0.01;	// Subtract a little bias to make sure we always render in front of the depth pass
											// That's because we have 16-bits depth precision in the geometry buffer whereas
											// the depth stencil buffer is 32-bits
	float	Depth = 0.90 * GeometryValue.w;

	float3	CameraNormal = normalize( GeometryValue.xyz );
//	float3	CameraNormal = normalize( mul( float4( 0.0, 1.0, 0.0, 0.0 ), World2Camera ).xyz );

	// Compute camera view & world position
	float3	CameraView = normalize( float3( (2.0 * _In.UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * _In.UV.y) * CameraData.x, 1.0 ) );
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	WorldPosition = Camera2World[3].xyz + View * Depth / CameraView.z;

	///////////////////////////////////////////////////////////////////////////
	// Compute terrain material
	float2	GlobalUV = WorldPosition.xz * WORLD2GLOBALTEXTURE_SCALE;
	float2	TileUV = WorldPosition.xz * TextureScale;

	// Read mix
	float4	Mix = TerrainMixTexture.SampleLevel( LinearClamp, GlobalUV, 0 );

	// Read atlas
	float4	C0 = 0.25 * TerrainAtlas.Sample( LinearWrap, float3( TileUV, 0 ) );	// 0.25 is albedo for green grass (from http://en.wikipedia.org/wiki/Albedo)
	float4	C1 = 0.17 * TerrainAtlas.Sample( LinearWrap, float3( TileUV, 1 ) );	// 0.17 is albedo for bare soil (from http://en.wikipedia.org/wiki/Albedo)
	// TODO: Add more textures

	// Mix colors
	float4	Albedo = lerp( C0, C1, Mix.x );
	// TODO: Mix more colors

//	Albedo = 0.0;

	///////////////////////////////////////////////////////////////////////////
	// Write resulting data into the MRT
	return WriteDeferredMRTWithDepth( Albedo.xyz, 0* Albedo.www, 4.0, CameraNormal, Depth );
}


// ===================================================================================
//
technique10 RenderTileTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 WriteTerrainMRT
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
