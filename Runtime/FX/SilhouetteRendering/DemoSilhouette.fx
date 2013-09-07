// This demonstrates silhouette clipping
// The first stage draws the object using POM
// The second stage outputs silhouette from the object as simple quads for each silhouette triangle and applies grazing-angle POM + alpha-to-coverage for AA
//

static const float	PI = 3.1415926535897932384626433832795;

#include "Camera.fx"
#include "DirectionalLighting.fx"

float2		Height = float2( 0.1, 0.1 );	// This is the height (in WORLD space) encoded by the height map
float2		dUdV = float2( 1, 1 );			// This tells how much the UVs vary per world unit on that object
float		SilhouetteAngleThreshold = 0.5;	// This represents the cos(angle) threshold at which the silhouette starts to be generated

Texture2D	TexHeight : TEX_HEIGHT;
Texture2D	TexDiffuseSpecular : TEX_DIFFUSE;
Texture2D	TexNormal : TEX_NORMAL;
//Texture2D	TexHeight2 : TEX_HEIGHT2;

SamplerState TextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float3	Position : POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
	float2	Curvature : CURVATURE;
};

struct PS_IN
{
	float4	Position : SV_POSITION;
	float3	Normal : NORMAL;
	float3	Tangent : TANGENT;
	float3	BiTangent : BITANGENT;
	float2	UV : TEXCOORD0;
	float3	WorldPosition : TEXCOORD1;
	float3	CurvatureHeight : TEXCOORD2;
};

VS_IN VS2( VS_IN _In )
{
	return _In;	// Pass through if GS enabled
}

PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), World2Proj );
	Out.Normal = _In.Normal;
	Out.Tangent = _In.Tangent;
	Out.BiTangent = _In.BiTangent;
	Out.UV = _In.UV;
	Out.WorldPosition = _In.Position;
	Out.CurvatureHeight = float3( _In.Curvature, 0.0 );	// For standard mesh rendering, we should hit at height = 0

	return	Out;
}

// Pass through
VS_IN	VS_Silhouette( VS_IN _In )
{
	return _In;
}

//#define EXTENDED_FINS	// Define this to build extended fins

void	GenerateFin( VS_IN _In0, VS_IN _In1, inout TriangleStream<PS_IN> _OutStream )
{
	PS_IN	Out;

#if !defined(EXTENDED_FINS)

	Out.WorldPosition = _In0.Position + Height.y * _In0.Normal;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In0.Tangent;
	Out.Normal = _In0.Normal;
	Out.BiTangent = _In0.BiTangent;
	Out.UV = _In0.UV;
	Out.CurvatureHeight = float3( _In0.Curvature, Height.y );
	_OutStream.Append( Out );

	Out.WorldPosition = _In0.Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In0.Tangent;
	Out.Normal = _In0.Normal;
	Out.BiTangent = _In0.BiTangent;
	Out.UV = _In0.UV;
	Out.CurvatureHeight = float3( _In0.Curvature, 0.0 );
	_OutStream.Append( Out );

	Out.WorldPosition = _In1.Position + Height.y * _In1.Normal;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In1.Tangent;
	Out.Normal = _In1.Normal;
	Out.BiTangent = _In1.BiTangent;
	Out.UV = _In1.UV;
	Out.CurvatureHeight = float3( _In1.Curvature, Height.y );
	_OutStream.Append( Out );

	Out.WorldPosition = _In1.Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In1.Tangent;
	Out.Normal = _In1.Normal;
	Out.BiTangent = _In1.BiTangent;
	Out.UV = _In1.UV;
	Out.CurvatureHeight = float3( _In1.Curvature, 0.0 );
	_OutStream.Append( Out );

#else

	float3	Delta = _In1.Position - _In0.Position;
	float	fILength = 1.0 / length( Delta );
			Delta *= fILength;

	// Extra triangle
	Out.WorldPosition = _In0.Position + Height.y * (_In0.Normal - Delta);
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In0.Tangent;
	Out.Normal = _In0.Normal;
	Out.BiTangent = _In0.BiTangent;
	Out.UV = lerp( _In0.UV, _In0.UV, -Height.y * fILength );
	Out.CurvatureHeight = Height.y;
	_OutStream.Append( Out );

	// Main fin
	Out.WorldPosition = _In0.Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In0.Tangent;
	Out.Normal = _In0.Normal;
	Out.BiTangent = _In0.BiTangent;
	Out.UV = _In0.UV;
	Out.CurvatureHeight = 0.0;
	_OutStream.Append( Out );

	Out.WorldPosition = _In0.Position + Height.y * _In0.Normal;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In0.Tangent;
	Out.Normal = _In0.Normal;
	Out.BiTangent = _In0.BiTangent;
	Out.UV = _In0.UV;
	Out.CurvatureHeight = Height.y;
	_OutStream.Append( Out );

	Out.WorldPosition = _In1.Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In1.Tangent;
	Out.Normal = _In1.Normal;
	Out.BiTangent = _In1.BiTangent;
	Out.UV = _In1.UV;
	Out.CurvatureHeight = 0.0;
	_OutStream.Append( Out );

	Out.WorldPosition = _In1.Position + Height.y * _In1.Normal;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In1.Tangent;
	Out.Normal = _In1.Normal;
	Out.BiTangent = _In1.BiTangent;
	Out.UV = _In1.UV;
	Out.CurvatureHeight = Height.y;
	_OutStream.Append( Out );

	// Extra triangle
	Out.WorldPosition = _In1.Position + Height.y * (_In1.Normal + Delta);
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Tangent = _In1.Tangent;
	Out.Normal = _In1.Normal;
	Out.BiTangent = _In1.BiTangent;
	Out.UV = lerp( _In1.UV, _In0.UV, 1.0+Height.y * fILength );
	Out.CurvatureHeight = Height.y;
	_OutStream.Append( Out );

#endif

	_OutStream.RestartStrip();
}

// Generates an edge silhouette if one of the edge vertices is visible while the other is not
//
void	GenerateEdgeSilhouette( float3 _CameraWorldPosition, VS_IN _V0, VS_IN _V1, inout TriangleStream<PS_IN> _OutStream )
{
	float3	Views[2] =
	{
		_CameraWorldPosition - _V0.Position,
		_CameraWorldPosition - _V1.Position,
	};	

	float	Iz[2] =
	{
		1.0 / length( Views[0] ),
		1.0 / length( Views[1] ),
	};

	Views[0] *= Iz[0];
	Views[1] *= Iz[1];

	float3	Normals[2] =
	{
		normalize( _V0.Normal ),
		normalize( _V1.Normal )
	};

	float	fDot0 = dot( Normals[0], Views[0] );
	fDot0 -= SilhouetteAngleThreshold;
	float	fDot1 = dot( Normals[1], Views[1] );
	fDot1 -= SilhouetteAngleThreshold;

	if ( fDot0 * fDot1 > 0.0 )
		return;	// Same facing...

	// This is a silhouette edge !
	float	t0, t1;
	if ( fDot0 >= 0.0 )
	{
		t1 = 1.0f;
		t0 = (0.0 - fDot0) / (fDot1 - fDot0);
	}
	else
	{
		t0 = (0.0 - fDot0) / (fDot1 - fDot0);
		t1 = 0.0f;
	}

	VS_IN	NewEdge0;
	float	Z0 = 1.0 / lerp( Iz[0], Iz[1], t0 );
	NewEdge0.Position = lerp( _V0.Position * Iz[0], _V1.Position * Iz[1], t0 ) * Z0;
	NewEdge0.Normal = lerp( _V0.Normal * Iz[0], _V1.Normal * Iz[1], t0 ) * Z0;
	NewEdge0.Tangent = lerp( _V0.Tangent * Iz[0], _V1.Tangent * Iz[1], t0 ) * Z0;
	NewEdge0.BiTangent = lerp( _V0.BiTangent * Iz[0], _V1.BiTangent * Iz[1], t0 ) * Z0;
	NewEdge0.UV = lerp( _V0.UV * Iz[0], _V1.UV * Iz[1], t0 ) * Z0;

	VS_IN	NewEdge1;
	float	Z1 = 1.0 / lerp( Iz[0], Iz[1], t1 );
	NewEdge1.Position = lerp( _V0.Position * Iz[0], _V1.Position * Iz[1], t1 ) * Z1;
	NewEdge1.Normal = lerp( _V0.Normal * Iz[0], _V1.Normal * Iz[1], t1 ) * Z1;
	NewEdge1.Tangent = lerp( _V0.Tangent * Iz[0], _V1.Tangent * Iz[1], t1 ) * Z1;
	NewEdge1.BiTangent = lerp( _V0.BiTangent * Iz[0], _V1.BiTangent * Iz[1], t1 ) * Z1;
	NewEdge1.UV = lerp( _V0.UV * Iz[0], _V1.UV * Iz[1], t1 ) * Z1;

	if ( fDot0 >= 0.0 )
		GenerateFin( NewEdge1, NewEdge0, _OutStream );
	else
		GenerateFin( NewEdge0, NewEdge1, _OutStream );
}

// Generate a brute fin out from an edge that separates a visible face from a hidden one (standard silhouette algo)
//
void	GenerateEdgeSilhouetteWithAdjacency( float3 _CameraWorldPosition, VS_IN _V0, VS_IN _V1, VS_IN _V2, VS_IN _Adj, inout TriangleStream<PS_IN> _OutStream )
{
	float3	FaceNormal = cross( _V2.Position - _V1.Position, _V0.Position - _V1.Position );
	float3	AdjacentFaceNormal = cross( _V0.Position - _V1.Position, _Adj.Position - _V1.Position );
	float3	View = _CameraWorldPosition - _V0.Position;

	float	fDot0 = dot( View, FaceNormal );
	float	fDot1 = dot( View, AdjacentFaceNormal );
	if ( fDot0 * fDot1 > 0.0f )
		return;	// Same orientation !

	if ( fDot0 > 0.0 )
		GenerateFin( _V1, _V0, _OutStream );	// It's a silhouette edge !
	else
		GenerateFin( _V0, _V1, _OutStream );	// It's a silhouette edge !
}

// Generates a silhouette strip from a triangle if view angle falls below a given angle threshold
//
void	GenerateSilhouette( float3 _CameraWorldPosition, VS_IN _In[3], inout TriangleStream<PS_IN> _OutStream )
{
	float3	Views[3] =
	{
		_CameraWorldPosition - _In[0].Position,
		_CameraWorldPosition - _In[1].Position,
		_CameraWorldPosition - _In[2].Position
	};	

	// Prepare 1/Z values for perspective correction (good aulde times striking back ^_^)
	// Actually, I'm lying, I'm using 1/distance...
	float	Iz[3] =
	{
		1.0 / length( Views[0] ),
		1.0 / length( Views[1] ),
		1.0 / length( Views[2] )
	};

	Views[0] *= Iz[0];
	Views[1] *= Iz[1];
	Views[2] *= Iz[2];

	float3	Normals[3] =
	{
		normalize( _In[0].Normal ),
		normalize( _In[1].Normal ),
		normalize( _In[2].Normal )
	};

	float	Dots[3] =
	{
		dot( Views[0], Normals[0] ),
		dot( Views[1], Normals[1] ),
		dot( Views[2], Normals[2] )
	};

	// Decrease dots using some cos(angle) threshold
	Dots[0] -= SilhouetteAngleThreshold;
	Dots[1] -= SilhouetteAngleThreshold;
	Dots[2] -= SilhouetteAngleThreshold;

	int	FrontCount  = Dots[0] >= 0.0 ? 1 : 0;
		FrontCount += Dots[1] >= 0.0 ? 1 : 0;
		FrontCount += Dots[2] >= 0.0 ? 1 : 0;
	if ( FrontCount == 3 )
		return;	// This face is completely above the horizon
	if ( FrontCount == 0 )
		return;	// This face is completely below the horizon
	
	// We're handling the case where only one vertex is below the horizon (i.e. front count = 2)
	// If we have two points below the horizon then we simple invert the dot products
	int		I0, I1, I2;
	float	t0, t1;
	if ( FrontCount == 1 )
	{	// 2 points below the horizon, reversed case => Revert triangle indices otherwise strip will be generated backward !
		// Make sure I0 always is the index of the point above the horizon
		if ( Dots[1] > 0.0 )		{ I0 = 1; I1 = 0; I2 = 2; }
		else if ( Dots[2] > 0.0 )	{ I0 = 2; I1 = 1; I2 = 0; }
		else						{ I0 = 0; I1 = 2; I2 = 1; }
	}
	else
	{	// 2 points above the horizon, expected case
		// Make sure I0 always is the index of the point below the horizon
		if ( Dots[1] < 0.0 )		{ I0 = 1; I1 = 2; I2 = 0; }
		else if ( Dots[2] < 0.0 )	{ I0 = 2; I1 = 0; I2 = 1; }
		else						{ I0 = 0; I1 = 1; I2 = 2; }
	}
	
	// Compute the 2 intersections with the camera ray and the horizon
	t0 = (Dots[I1] - 0.0) / (Dots[I1] - Dots[I0]);
	t1 = (Dots[I2] - 0.0) / (Dots[I2] - Dots[I0]);

	// Interpolate left data using perspective correction
	float	ZL = 1.0 / lerp( Iz[I1], Iz[I0], t0 );
	VS_IN	Left;
	Left.Position = lerp( _In[I1].Position * Iz[I1], _In[I0].Position * Iz[I0], t0 ) * ZL;
	Left.Tangent = lerp( _In[I1].Tangent * Iz[I1], _In[I0].Tangent * Iz[I0], t0 ) * ZL;
	Left.Normal = lerp( _In[I1].Normal * Iz[I1], _In[I0].Normal * Iz[I0], t0 ) * ZL;
	Left.BiTangent = lerp( _In[I1].BiTangent * Iz[I1], _In[I0].BiTangent * Iz[I0], t0 ) * ZL;
	Left.UV = lerp( _In[I1].UV * Iz[I1], _In[I0].UV * Iz[I0], t0 ) * ZL;
	Left.Curvature = lerp( _In[I1].Curvature * Iz[I1], _In[I0].Curvature * Iz[I0], t0 ) * ZL;

	Left.Normal = normalize( Left.Normal );

	// Interpolate right data using perspective correction
	VS_IN	Right;
	float	ZR = 1.0 / lerp( Iz[I2], Iz[I0], t1 );
	Right.Position = lerp( _In[I2].Position * Iz[I2], _In[I0].Position * Iz[I0], t1 ) * ZR;
	Right.Tangent = lerp( _In[I2].Tangent * Iz[I2], _In[I0].Tangent * Iz[I0], t1 ) * ZR;
	Right.Normal = lerp( _In[I2].Normal * Iz[I2], _In[I0].Normal * Iz[I0], t1 ) * ZR;
	Right.BiTangent = lerp( _In[I2].BiTangent * Iz[I2], _In[I0].BiTangent * Iz[I0], t1 ) * ZR;
	Right.UV = lerp( _In[I2].UV * Iz[I2], _In[I0].UV * Iz[I0], t1 ) * ZR;
	Right.Curvature = lerp( _In[I2].Curvature * Iz[I2], _In[I0].Curvature * Iz[I0], t1 ) * ZR;

	Right.Normal = normalize( Right.Normal );

	GenerateFin( Left, Right, _OutStream );
}

// Generates strips for silhouette triangles
[maxvertexcount( 7*6 )]
void GS_ComplexeSilhouetteWithAdjacency( triangleadj VS_IN _In[6], inout TriangleStream<PS_IN> _OutStream )
{
	float3	CameraWorldPosition = Camera2World[3].xyz;

	// Main triangle
	VS_IN	Vertices0[3] = { _In[0], _In[1], _In[2] };
	GenerateSilhouette( CameraWorldPosition, Vertices0, _OutStream );

// 	// Edges that attempt to join the main triangle silhouette
// 	GenerateEdgeSilhouette( CameraWorldPosition, _In[0], _In[2], _OutStream );
// 	GenerateEdgeSilhouette( CameraWorldPosition, _In[2], _In[4], _OutStream );
// 	GenerateEdgeSilhouette( CameraWorldPosition, _In[4], _In[0], _OutStream );

	// Real edges
// 	GenerateEdgeSilhouetteWithAdjacency( CameraWorldPosition, _In[0], _In[2], _In[4], _In[1], _OutStream );
// 	GenerateEdgeSilhouetteWithAdjacency( CameraWorldPosition, _In[2], _In[4], _In[0], _In[3], _OutStream );
// 	GenerateEdgeSilhouetteWithAdjacency( CameraWorldPosition, _In[4], _In[0], _In[2], _In[5], _OutStream );
}


// Generates strips for silhouette triangles
[maxvertexcount( 7*6 )]
void GS_Silhouette( triangle VS_IN _In[3], inout TriangleStream<PS_IN> _OutStream )
{
	float3	CameraWorldPosition = Camera2World[3].xyz;

	// Main triangle
	VS_IN	Vertices0[3] = { _In[0], _In[1], _In[2] };
	GenerateSilhouette( CameraWorldPosition, Vertices0, _OutStream );
}


// Builds tangent space curvature for the given vertex and face global data
//
float2	BuildCurvature( in VS_IN _V, float3 _Barycenter, float3 _Normal, float3 _Tangent, float _BiTangent )
{
	// Project vertex normal in tangent and bitangent planes
	float	fTangentProjection = dot( _V.Normal, _Tangent );
	float	fBiTangentProjection = dot( _V.Normal, _BiTangent );
	float3	NormalTangent = normalize( _V.Normal - fBiTangentProjection * _BiTangent );
	float3	NormalBiTangent = normalize( _V.Normal - fTangentProjection * _Tangent );

	return float2( fTangentProjection, fBiTangentProjection );

	// Project vector from barycenter into tangent and bitangent planes
	float3	Edge = _V.Position - _Barycenter;
	float	fEdgeTangent = dot( Edge, _Tangent );
	float	fEdgeBiTangent = dot( Edge, _BiTangent );

	return float2( abs(fEdgeTangent), abs(fEdgeBiTangent) );

	// At this stage, we have 2 projected edges as well as 2 normals for tangent and bitangent
	// Each is treated separately as if in a 2D plane and is assumed to behave like this (bitangent case) :
	//
	//		^ N
	//		|                            / N' <= vertex normal projected in bitangent plane
	//		|                           /
	//		|                          /
	//		|        B                /
	//	  C o-------->...............o V
	//	   /
	//    /
	//   /
	//  /
	// T
	//
	// Then curvature is computed as Radius = (distance between V and barycenter C) / sin( angle between N and N' )
	//
	float	fDotTangent = dot( _Normal, NormalTangent );
	float	fCurvatureTangent = fEdgeTangent / max( 1e-3, sqrt( 1.0 - fDotTangent*fDotTangent ) );
	float	fDotBiTangent = dot( _Normal, NormalBiTangent );
	float	fCurvatureBiTangent = fEdgeBiTangent / max( 1e-3, sqrt( 1.0 - fDotBiTangent*fDotBiTangent ) );

//	return float2( dot( Normal0, normalize( _V1.Normal ) ), fDotBiTangent );

	return float2( fCurvatureTangent, fCurvatureBiTangent );
}

float2	BuildCurvature2( in VS_IN _In[3], float3 _Barycenter, float3 _Normal, float3 _Tangent, float3 _BiTangent )
{
	// Build curvature radii for each vertex
	float3	ToV0 = _In[0].Position - _Barycenter;
	float	D0 = length( ToV0 );
	ToV0 /= D0;
	float	Dot0 = dot( normalize( _In[0].Normal ), _Normal );

	float3	ToV1 = _In[1].Position - _Barycenter;
	float	D1 = length( ToV1 );
	ToV1 /= D1;
	float	Dot1 = dot( normalize( _In[1].Normal ), _Normal );

	float3	ToV2 = _In[2].Position - _Barycenter;
	float	D2 = length( ToV2 );
	ToV2 /= D2;
	float	Dot2 = dot( normalize( _In[2].Normal ), _Normal );

	float	fCurvatures[3] =
	{
		D0 / max( 1e-3, sqrt( 1-Dot0*Dot0 ) ),		// Osculating circle Radius = D / sin( angle )
		D1 / max( 1e-3, sqrt( 1-Dot1*Dot1 ) ),		// Osculating circle Radius = D / sin( angle )
		D2 / max( 1e-3, sqrt( 1-Dot2*Dot2 ) ),		// Osculating circle Radius = D / sin( angle )
	};

	// Compute face curvature along tangent direction
	float	DotB0 = dot( _BiTangent, ToV0 );
	float	DotB1 = dot( _BiTangent, ToV1 );
	float	DotB2 = dot( _BiTangent, ToV2 );

	float	fCurvatureT = 0.0;
	float	fWeightT = 0.0;
	if ( DotB0 * DotB1 <= 0.0 )
	{
		float	t = (0.0 - DotB0) / (DotB1 - DotB0);
		float3	Intersection = lerp( ToV0, ToV1, t );
		float	fWeight = abs( dot( _Tangent, Intersection ) );
		fCurvatureT += fWeight * lerp( fCurvatures[0], fCurvatures[1], t );
		fWeightT += fWeight;
	}
	if ( DotB1 * DotB2 <= 0.0 )
	{
		float	t = (0.0 - DotB1) / (DotB2 - DotB1);
		float3	Intersection = lerp( ToV1, ToV2, t );
		float	fWeight = abs( dot( _Tangent, Intersection ) );
		fCurvatureT += fWeight * lerp( fCurvatures[1], fCurvatures[2], t );
		fWeightT += fWeight;
	}
	if ( DotB2 * DotB0 <= 0.0 )
	{
		float	t = (0.0 - DotB2) / (DotB0 - DotB2);
		float3	Intersection = lerp( ToV2, ToV0, t );
		float	fWeight = abs( dot( _Tangent, Intersection ) );
		fCurvatureT += fWeight * lerp( fCurvatures[2], fCurvatures[0], t );
		fWeightT += fWeight;
	}
	fCurvatureT /= fWeightT;

	// Compute face curvature along bitangent direction
	float	DotT0 = dot( _Tangent, ToV0 );
	float	DotT1 = dot( _Tangent, ToV1 );
	float	DotT2 = dot( _Tangent, ToV2 );
	float	fCurvatureB = 0.0;
	float	fWeightB = 0.0;
	if ( DotT0 * DotT1 <= 0.0 )
	{
		float	t = (0.0 - DotT0) / (DotT1 - DotT0);
		float3	Intersection = lerp( ToV0, ToV1, t );
		float	fWeight = abs( dot( _BiTangent, Intersection ) );
		fCurvatureB += fWeight * lerp( fCurvatures[0], fCurvatures[1], t );
		fWeightB += fWeight;
	}
	if ( DotT1 * DotT2 <= 0.0 )
	{
		float	t = (0.0 - DotT1) / (DotT2 - DotT1);
		float3	Intersection = lerp( ToV1, ToV2, t );
		float	fWeight = abs( dot( _BiTangent, Intersection ) );
		fCurvatureB += fWeight * lerp( fCurvatures[1], fCurvatures[2], t );
		fWeightB += fWeight;
	}
	if ( DotT2 * DotT0 <= 0.0 )
	{
		float	t = (0.0 - DotT2) / (DotT0 - DotT2);
		float3	Intersection = lerp( ToV2, ToV0, t );
		float	fWeight = abs( dot( _BiTangent, Intersection ) );
		fCurvatureB += fWeight * lerp( fCurvatures[2], fCurvatures[0], t );
		fWeightB += fWeight;
	}
	fCurvatureB /= fWeightB;

//	return float2( fWeightT, fWeightB );

	return float2( fCurvatureT, fCurvatureB );
}

// This GS attempts to build face curvature
//
[maxvertexcount( 3 )]
void	GS( triangle VS_IN _In[3], inout TriangleStream<PS_IN> _OutStream )
{
	PS_IN	Out;

	// Compute barycenter and face normal, tangent and bitangent
	float3	Barycenter = (_In[0].Position + _In[1].Position + _In[2].Position) / 3.0;
	float3	FaceNormal = normalize( cross( _In[2].Position - _In[1].Position, _In[0].Position - _In[1].Position ) );
	float3	FaceTangent = normalize( _In[0].Tangent + _In[1].Tangent + _In[2].Tangent );
	float3	FaceBiTangent = normalize( _In[0].BiTangent + _In[1].BiTangent + _In[2].BiTangent );
	
	float2	CurvatureRadii = BuildCurvature2( _In, Barycenter, FaceNormal, FaceTangent, FaceBiTangent );

	// Pass input augmented with curvature
	Out.WorldPosition = _In[0].Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Normal = _In[0].Normal;
	Out.Tangent = _In[0].Tangent;
	Out.BiTangent = _In[0].BiTangent;
	Out.UV = _In[0].UV;
//	Out.CurvatureHeight = float3( BuildCurvature( _In[0], Barycenter, FaceNormal, FaceTangent, FaceBiTangent ), Height.x );
	Out.CurvatureHeight = float3( CurvatureRadii, Height.x );
	_OutStream.Append( Out );

	Out.WorldPosition = _In[1].Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Normal = _In[1].Normal;
	Out.Tangent = _In[1].Tangent;
	Out.BiTangent = _In[1].BiTangent;
	Out.UV = _In[1].UV;
//	Out.CurvatureHeight = float3( BuildCurvature( _In[1], Barycenter, FaceNormal, FaceTangent, FaceBiTangent ), Height.x );
	Out.CurvatureHeight = float3( CurvatureRadii, Height.x );
	_OutStream.Append( Out );

	Out.WorldPosition = _In[2].Position;
	Out.Position = mul( float4( Out.WorldPosition, 1 ), World2Proj );
	Out.Normal = _In[2].Normal;
	Out.Tangent = _In[2].Tangent;
	Out.BiTangent = _In[2].BiTangent;
	Out.UV = _In[2].UV;
//	Out.CurvatureHeight = float3( BuildCurvature( _In[2], Barycenter, FaceNormal, FaceTangent, FaceBiTangent ), Height.x );
	Out.CurvatureHeight = float3( CurvatureRadii, Height.x );
	_OutStream.Append( Out );
}

// This shader applies Parallax Occlusion Mapping (POM) to an object
//
float4 PS( PS_IN _In, uniform int _StepsCount ) : SV_Target
{
	// Compute camera ray
	float3	FromPixel = Camera2World[3].xyz - _In.WorldPosition;
	float	fDistance2Pixel = length( FromPixel );
			FromPixel /= fDistance2Pixel;

	// Transform it into tangent space
	//	Tangent varies along with U
	//	BiTangent varies along with -V
	//
	float3	Normal = normalize( _In.Normal );
	float3	Tangent = normalize( _In.Tangent );
	float3	BiTangent = normalize( _In.BiTangent );

	float3	View = -float3( dUdV.y * dot( BiTangent, FromPixel ), dot( Normal, FromPixel ), dUdV.x * dot( Tangent, FromPixel ) );

	float	fCurvatureT = dUdV.x * _In.CurvatureHeight.x;
	float	fCurvatureB = dUdV.y * _In.CurvatureHeight.y;

	float3	TargetPos = float3( _In.UV.y, _In.CurvatureHeight.z, _In.UV.x );	// This is the point we're watching in Tangent Space

	// Compute the camera ray intersection with the elevated plane (i.e. the plane above the surface we're lighting)
	float	fMaxDistance = min( 0.75 * fCurvatureT, min( 0.75 * fCurvatureB, 10.0 * Height.x ) );	// This is the maximum distance we can start from
	float	fMinValue = Height.x / fMaxDistance;
	float	fDistance2ElevatedPlane = Height.x / max( fMinValue, -View.y );		// The max() here makes sure we don't go further than the max distance computed before
	float3	CurrentPos = TargetPos - View * fDistance2ElevatedPlane;			// Start at intersection with plane height

	// Do the same with plane at height = 0
	fMaxDistance = min( 0.75 * fCurvatureT, min( 0.75 * fCurvatureB, 10.0 * TargetPos.y ) );	// This is the maximum distance we can end to
	float	fDistance2FloorPlane = TargetPos.y / max( fMinValue, -View.y );
//	float3	EndPos = TargetPos;// + View * fDistance2FloorPlane;
	float3	EndPos = TargetPos + 0.01 * View;

	// Given curvatures, we approximate the portion of the osculating circles for tangent and bi-tangent planes using a parabola.
	//
	// ^ N
	// |
	// |
	// |     T       D
	// O----->.......x..............> X
	// |       ---   :     ^
	// |           . :     | h
	// |             x.....v 
	// |             :. 
	// |             : |   
	// |          H  : |   
	// |             :  |
	// |             :  |
	// |             :  |
	// C-------------:--o <== Radius R
	// 
	// 
	//  D is the distance in the tangent plane from O (the end position) at which we start
	//  We express h in function of X € [0,R] :
	// 
	//  h(X) = R - sqrt( R² - X² )
	// 
	//  and the derivative :
	// 
	//  dh(X)/dX = h'(X) = X / sqrt( R² - X² )
	// 
	//  That we map to a parabola of equation : ~h(X) = aX² + bX + c  and its derivative  ~h'(X) = 2aX + b
	//  So at distance X=D (our start distance) :
	// 
	//  h(D) = aD² + bD   (we know c=0 anyway)
	//  h'(D) = 2aD + b
	// 
	//  And solving for a we find :
	//	b = h'(D) - 2aD
	//  h(D) = aD² + Dh'(D) - 2aD² = Dh'(D) - aD²	=>	a = (Dh'(D) - h(D)) / D²
	// 
	//  Implying b :
	//  b = h'(D) - 2D (Dh'(D) - h(D)) / D²			=>	b = 2h(D)/D - h'(D)
	// 
	//	Armed with the parabola equations, we can compute ~h(x)
	// 
	float2	DistanceBT = EndPos.xz - CurrentPos.xz;
	float	fCosT = abs( normalize( View.xz ).y );
	float2	CosSinT = float2( fCosT, sqrt( 1.0 - fCosT*fCosT ) );

	float2	abT;
	{	// Tangent
		float	D = max( 1e-2, DistanceBT.y );
		float	R = fCurvatureT;
		float	SqrtR = sqrt( R*R - D*D );
		float	h = R - SqrtR;
		float	dh = D / SqrtR;

		float	a = (D * dh - h)/(D*D);
		float	b = 2*h/D - dh;

		abT = float2( a, b );
	}

	float2	abB;
	{	// Bi-Tangent
		float	D = max( 1e-2, DistanceBT.x );
		float	R = fCurvatureB;
		float	SqrtR = sqrt( R*R - D*D );
		float	h = R - SqrtR;
		float	dh = D / SqrtR;

		float	a = (D * dh - h)/(D*D);
		float	b = 2*h/D - dh;

		abB = float2( a, b );
	}

	// Compute actual steps count
	// We need less steps when viewing perpendicular to the surface and more when viewing at grazing angles
	const float	fOneOverCosLimitAngle = 1.0 / 0.5;		// 0.5 is cos(60°) so we reach the maximum samples count at this angle
	int		StepsCount = clamp( fOneOverCosLimitAngle*(1.0+View.y) * _StepsCount, 4, _StepsCount );

	// Now, perform ray marching from start to end in _StepsCount steps and check for an intersection
	float3	Step = (EndPos - CurrentPos) / StepsCount;
	CurrentPos += 0.5 * Step;		// March a little

	float	fLastHeight = CurrentPos.y;
	float	fCurrentHeight = CurrentPos.y;	// Start at maximum height
	for ( int StepIndex=0; StepIndex <= StepsCount; StepIndex++ )
	{
		// Compute height offsets on tangent and bi-tangent planes
		float	dT = CurrentPos.z - TargetPos.z;
		float	hT = abs( dT * (abT.y + dT * abT.x) );
		float	dB = CurrentPos.x - TargetPos.x;
		float	hB = abs( dB * (abB.y + dB * abB.x) );
		float	h = CosSinT.x * hT + CosSinT.y * hB;	// Final height offset that is a combination of tangent and bi-tangent offsets based on view direction

		// Read height at current position
		fLastHeight = fCurrentHeight;
		fCurrentHeight = Height.x * TexHeight.SampleLevel( TextureSampler, CurrentPos.zx, 0.0 ).r;
		fCurrentHeight -= h;	// Subtract the little height offset based on face curvature so we make the height field appear lower...
		if ( CurrentPos.y < fCurrentHeight )
			break;			// We're below the current height so we must have intersected some place within the current step !

		CurrentPos += Step;		// March !
	}

	// Is there a hit or did we pass just through ?
	if ( CurrentPos.y > fCurrentHeight )
		discard;

	// We found an intersection during the last step
	// We know the height slope and the step slope so we may find a nice approximation of
	//	the exact intersection position by computing the intersection of these 2 segments
	//
	float	fDeltaHeight = fCurrentHeight - fLastHeight;
	float	t = (CurrentPos.y-Step.y - fLastHeight) / (fDeltaHeight - Step.y);

	CurrentPos += (t-1.0) * Step;	// Step back a little to the computed intersection

	// This is our new UV position !
	_In.UV.xy = CurrentPos.zx;

	// =================== Perform Lighting ===================
	float4	DiffuseSpecularColor = TexDiffuseSpecular.Sample( TextureSampler, _In.UV );

	// Sample TS normal and compute WS normal
	float3	LocalNormal = 2.0 * TexNormal.Sample( TextureSampler, _In.UV ).rgb - 1.0;
	Normal = LocalNormal.r * Tangent + LocalNormal.g * BiTangent + LocalNormal.b * Normal;

	// Compute diffuse and specular
// 	float	fAmbient = 1.0f;
// 	float	fDiffuse = clamp( fAmbient+DotLightUnclamped( Normal ), 0, 2 ) / (1.0 + fAmbient);

 	float	fDiffuse = DotLightUnclamped( Normal );
	float3	Ambient = 0.1 * saturate( 1+fDiffuse ) * float3( 0.1, 0.6, 0.9 );
			fDiffuse = saturate( fDiffuse );

	float3	Half = 0.5 * (FromPixel + LightDirection);
	float	fSpecular = pow( saturate( dot( Half, Normal ) ), 4.0 );
			fSpecular *= DiffuseSpecularColor.a;

	// Compute approximate AO by sampling height map
	float	D = 0.02f;
	float	fSumHeight  = TexHeight.SampleLevel( TextureSampler, _In.UV + float2( -D*dUdV.x, -D*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +0*dUdV.x, -D*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +D*dUdV.x, -D*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( -D*dUdV.x, +D*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +0*dUdV.x, +D*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +D*dUdV.x, +D*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( -D*dUdV.x, +0*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +D*dUdV.x, +0*dUdV.y ), 0.0 ).r;
			fSumHeight = fSumHeight * 0.125 * Height.x - fCurrentHeight;
	float	fApertureAngle = atan( fSumHeight / D );	// This yields the average aperture angle this point can see of the outer world

	float	fAOFactor = 4.0;
	float	AO = saturate( 1.0 - fAOFactor * fApertureAngle / (0.5 * PI) );

	float3	Result = AO * Ambient + DiffuseSpecularColor.rgb * (fDiffuse + fSpecular) * LightColor.rgb;

	return	float4( Result, 1 );
}

technique10 Render
{
	// Somewhat failed attempt at auto-computation of mesh curvature...
// 	pass P0
// 	{
// 		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
// 		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
// 		SetPixelShader( CompileShader( ps_4_0, PS( 20 ) ) );
// 	}

	// Main rendering
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS( 20 ) ) );
	}

	// Silhouette rendering
	pass P1
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Silhouette() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS_Silhouette() ) );
		SetPixelShader( CompileShader( ps_4_0, PS( 40 ) ) );	// This pass uses many more samples !
	}
}
