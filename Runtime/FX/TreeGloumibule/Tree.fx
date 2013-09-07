// Displays the growing tree
//
#include "..\Camera.fx"
#include "..\DirectionalLighting.fx"

static const float	PI = 3.14159265358978;
static const int	RING_VERTS = 8;

float		Time = 0.0;

struct VS_IN
{
	float3	Position	: P;
	float3	X			: X;
	float3	Y			: Y;
	float3	Z			: Z;
	float	Radius		: R;
	float	Mass		: M;
	float	Torque		: T;
	float	LightAbs	: LA;
	float	LightRel	: LR;
	float	NutrientAbs	: NA;
	float	NutrientRel	: NR;
};

struct PS_IN
{
	float4	Position	: SV_POSITION;
	float3	Normal		: NORMAL;
	float	Mass		: M;
	float	Torque		: T;
	float	LightAbs	: LA;
	float	LightRel	: LR;
	float	NutrientAbs	: NA;
	float	NutrientRel	: NR;
};

VS_IN VS( VS_IN _In )
{
	return _In;
}

[maxvertexcount( 2*(RING_VERTS+1) )]
void GS( line VS_IN _In[2], uint _PrimitiveID : SV_PRIMITIVEID, inout TriangleStream<PS_IN> _OutStream )
{
	PS_IN	Out;

	// Build ring linking segment vertex 0 to vertex 1
	for ( int RingIndex=0; RingIndex <= RING_VERTS; RingIndex++ )
	{
		float	Angle = 2.0 * PI * RingIndex / RING_VERTS;
		float	C = cos( Angle );
		float	S = sin( Angle );

		float3	Normal0 = S * _In[0].X + C * _In[0].Z;
		float3	Pos0 = _In[0].Position + _In[0].Radius * Normal0;
		float3	Normal1 = S * _In[1].X + C * _In[1].Z;
		float3	Pos1 = _In[1].Position + _In[1].Radius * Normal1;

		// Write next segment's vertex
		float	t = 1.0;
		Out.Position = mul( float4( Pos1, 1.0 ), World2Proj );
		Out.Normal = Normal1;
		Out.Mass = lerp( _In[0].Mass, _In[1].Mass, t );
		Out.Torque = lerp( _In[0].Torque, _In[1].Torque, t );
		Out.LightAbs = lerp( _In[0].LightAbs, _In[1].LightAbs, t );
		Out.LightRel = lerp( _In[0].LightRel, _In[1].LightRel, t );
		Out.NutrientAbs = lerp( _In[0].NutrientAbs, _In[1].NutrientAbs, t );
		Out.NutrientRel = lerp( _In[0].NutrientRel, _In[1].NutrientRel, t );
		_OutStream.Append( Out );

		// Write current segment's vertex
		t = 0.0;
		Out.Position = mul( float4( Pos0, 1.0 ), World2Proj );
		Out.Normal = Normal0;
		Out.Mass = lerp( _In[0].Mass, _In[1].Mass, t );
		Out.Torque = lerp( _In[0].Torque, _In[1].Torque, t );
		Out.LightAbs = lerp( _In[0].LightAbs, _In[1].LightAbs, t );
		Out.LightRel = lerp( _In[0].LightRel, _In[1].LightRel, t );
		Out.NutrientAbs = lerp( _In[0].NutrientAbs, _In[1].NutrientAbs, t );
		Out.NutrientRel = lerp( _In[0].NutrientRel, _In[1].NutrientRel, t );
		_OutStream.Append( Out );
	}
}

float4 PS( PS_IN _In ) : SV_TARGET0
{
	return float4( float3( 0.2, 0.2, 0.3 ) + saturate( dot( _In.Normal, LightDirection ) ) * LightColor.xyz, 1.0 );
}


// ===================================================================================
//
technique10 DrawTree
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
