// This performs ray-marching of a terrain distance field
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "DeferredSupport.fx"
#include "3DNoise.fx"

static const float	INFINITY = 1e5f;
static const int	FBM_OCTAVES_COUNT = 3;
static const int	HEIGHTFIELD_STEPS_COUNT = 32;
static const float	HEIGHTFIELD_POSITION_SCALE = 0.004;
static const float	HEIGHTFIELD_HEIGHT_SCALE = 4.0;
static const float	FBM_LIPSCHITZ = 1.25 * HEIGHTFIELD_HEIGHT_SCALE * HEIGHTFIELD_HEIGHT_SCALE;	// The Lipschitz boundary when using 3 octaves of FBM

Texture2D	TerrainGeometry;	// The downscaled terrain geometry (XYZ=WorldNormal W=Distance)

float2		ScreenSize;			// Size of the screen in pixels

struct VS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float3	View				: VIEW;
	float2	UV					: TEXCOORD0;
};

struct PS_IN
{
	float4	TransformedPosition	: SV_POSITION;
	float3	View				: VIEW;			// World view
	float3	CameraView			: CAMERA_VIEW;	// Untransformed camera view
	float2	UV					: TEXCOORD0;
};

PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.TransformedPosition = _In.TransformedPosition;
	Out.View = mul( float4( _In.View, 0.0 ), Camera2World ).xyz;	// Transform view ray in WORLD space
	Out.CameraView = _In.View;
	Out.UV = _In.UV;
	
	return	Out;
}

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

float	fbm( float3 _Position, out float3 _SumDerivatives )
{
	_Position *= HEIGHTFIELD_POSITION_SCALE;
	_Position.y = 0.5;
	_SumDerivatives = 0.0.xxx;

	float	Value = 0.0;
	float	Weight = 1.0;
	for( int i=0; i < FBM_OCTAVES_COUNT; i++ )
	{
		float3	Derivative;
		float	NoiseValue = Noise( _Position, NoiseTexture0, Derivative );

		_SumDerivatives += Derivative;
		Value += Weight * NoiseValue;// / (1.0 + dot(_SumDerivatives,_SumDerivatives));	// Replace with "w * NoiseValue" for a classic fbm()

//		Weight *= 0.25;		// w=1/2^(b*i) with b=2. Lipschitz condition = 3*(2-1/(2^(n-1)) <= 3 implying a maximum limit to the slope
		Weight *= 0.5;		// w=1/2^(b*i) with b=2. Lipschitz condition = 3*(2-1/(2^(n-1)) <= 3 implying a maximum limit to the slope
		_Position.xz *= 2.0;
	}

	return HEIGHTFIELD_HEIGHT_SCALE * Value;
}

// This pixel shader ray-marches through the terrain's distance field
//
float4 PS( PS_IN _In ) : SV_TARGET0
{
	float3	CameraView = normalize( float3( (2.0 * _In.UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * _In.UV.y) * CameraData.x, 1.0 ) );
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	Pos = Camera2World[3].xyz;

// 	float	HitDistance = -Pos.y / View.y;
// 	if ( HitDistance < CameraData.z )
// 		HitDistance = INFINITY;
// 	return float4( 0.0, 1.0, 0.0, HitDistance * CameraView.z );



	float	PixelFOV = CameraData.x / ScreenSize.y;			// The half FOV for a single pixel that will help us determine the radius of the pixel cone at a given distance
	float	PixelSlope = PixelFOV * (1.0 - abs(View.y));	// The additional slope contribution from the pixel cone (full contribution if the ray is horizontal)
	float	InvDen = 1.0 / (FBM_LIPSCHITZ + PixelSlope - View.y);

	float	PixelConeRadius = 0.0, StepLength = 0.0;
	float	PreviousHeight, CurrentHeight = Pos.y;
	float3	PreviousDerivatives, CurrentDerivatives = 0.0.xxx;
	for ( int StepIndex=0; StepIndex < HEIGHTFIELD_STEPS_COUNT; StepIndex++ )
	{
		PreviousDerivatives = CurrentDerivatives;
		PreviousHeight = CurrentHeight;

		// Estimate height and derivatives at current position
		CurrentHeight = fbm( Pos, CurrentDerivatives );
		if ( Pos.y-PixelConeRadius < CurrentHeight )
			break;	// There's a hit !

		// Here, the Lipschitz condition on fbm using 3 octaves ensures us the maximum slope of the fbm is 1.25 at any point in the noise volume
		// This means that we can get a solid estimate of the step we can march before we (perhaps) hit the terrain.
		// We assume the pixel is following :
		//	Py(Z) = Py(Z0) - k.Z0 + (ViewY - k).(Z-Z0)
		// with k=PixelSlope, the rate at which the pixel's cone radius grows regarding to depth
		//
		// And the Lipschitz-bound terrain's height :
		//	Hy(Z) = H(Z0) + Lip.(Z-Z0)
		// We need to compute MarchStep = Z-Z0 which is given by :
		//	Z-Z0 = (Py(Z0) - k.Z0 - H(Z0)) / (Lip - ViewY + k)
		StepLength = (Pos.y - PixelConeRadius - CurrentHeight) * InvDen;
		if ( StepLength < 0.0 )
	 		return float4( 0.0, 0.0, 0.0, INFINITY );	// The terrain will never catch up our ray...

		// We also can get a minimum distance boundary using the pixel cone radius at current position
		StepLength = max( PixelConeRadius, StepLength );

		// March & estimate new pixel cone radius
		Pos += StepLength * View;
		PixelConeRadius += StepLength * PixelSlope;
	}

	// Compute interpolated hit
	float	t = (Pos.y - PixelConeRadius - CurrentHeight) / (PreviousHeight - CurrentHeight + View.y - PixelFOV);
// 	if ( t < 0.0  || t > 1.0 )
// 	 	return float4( 0.0, 0.0, 0.0, INFINITY );

//	float	FinalHeight = lerp( CurrentHeight, PreviousHeight, t );
	float3	FinalDerivatives = lerp( CurrentDerivatives, PreviousDerivatives, t );
	Pos -= t * StepLength * View;

	float3	Normal = normalize( FinalDerivatives );

// 	return float4( Normal, length( Pos - Camera2World[3].xyz ) );
 	return float4( 0.0.xxx, length( Pos - Camera2World[3].xyz ) );
// 	return float4( 0.0.xxx, dot( Pos - Camera2World[3].xyz, Camera2World[2].xyz ) );
}

// This pixel shader simply writes terrain depth in the depth stencil target (used by the DEPTH PASS)
//
float PS2( PS_IN _In ) : SV_DEPTH
{
	float	Z = max( CameraData.z, min( CameraData.w, TerrainGeometry.SampleLevel( LinearClamp, _In.UV, 0 ).w ) );
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf - Zn)
	return (1.0 - CameraData.z / Z) * Q;
}

// This pixel shader writes terrain geometry & material into the MRT setup by the deferred rendering
//
PS_OUTZ PS3( PS_IN _In )
{
	float3	CameraView = normalize( float3( (2.0 * _In.UV.x - 1.0) * CameraData.x * CameraData.y, (1.0 - 2.0 * _In.UV.y) * CameraData.x, 1.0 ) );
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	Pos = Camera2World[3].xyz;

	float4	TerrainData = TerrainGeometry.SampleLevel( LinearClamp, _In.UV, 0 );

//	float3	Normal = TerrainData.xyz;	// In WORLD space
	float3	Normal = World2Camera[1].xyz;
	float	Depth = TerrainData.w;
	float3	DiffuseAlbedo = float3( 0.2, 0.2, 0.2 );
	float3	SpecularAlbedo = float3( 0.0, 0.0, 0.0 );

	// Test fbm
	float3	Temp;
	DiffuseAlbedo.xyz = fbm( Pos + Depth * View, Temp );

	// Write resulting data into the MRT and also alter depth
	return WriteDeferredMRTWithDepth( DiffuseAlbedo, SpecularAlbedo, 40.0, Normal, Depth );
}


// ===================================================================================
//
technique10 RayMarchTerrain
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 WriteTerrainDepth
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}

technique10 WriteTerrainMRT
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS3() ) );
	}
}
