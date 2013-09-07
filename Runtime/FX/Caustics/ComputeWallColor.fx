// Various common helpers
//
static const float	PI = 3.14159265358979;
static const float	INTENSITY_FACTOR = 0.5;
static const float	IOR = 1.33;
static const float	COS_BREWSTER = 0.60096110438175551415262588748006;	// Cos( Atan( n2 / n1 ) ) with n2=1.33 and n1=1.0
static const float	R0 = (IOR-1.0)*(IOR-1.0) / ((IOR+1.0)*(IOR+1.0));	// ((IOR-1.0) / (IOR+1.0))²

// Deformed sphere constants
static const float3	INTERNAL_SPHERE_COLOR = float3( 0.1, 0.4, 0.45 );
static const float	INTERNAL_EXTINCTION_COEFFICIENT = 2.0;
static const float	INTERNAL_SCATTERING_COEFFICIENT = 3.0;	// I know this can be greated than extinction but do you know the theorem of Eidon G. Iveupheuk ?

static const int	CausticFace2WallTextureIndex[6] =
{
	0, 0,	// 2 walls
	1, 1,	// Ceiling/Floor
	0, 0,	// 2 walls
};

static const float3	CausticFace2Normal[6] =
{
	float3( +1.0, 0.0, 0.0 ),	// +X
	float3( -1.0, 0.0, 0.0 ),	// -X
	float3( 0.0, +1.0, 0.0 ),	// +Y
	float3( 0.0, -1.0, 0.0 ),	// -Y
	float3( 0.0, 0.0, +1.0 ),	// +Z
	float3( 0.0, 0.0, -1.0 ),	// -Z
};

SamplerState LinearWrap
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

SamplerState NearestWrap
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

Texture2DArray	CausticsTexture;
Texture2DArray	WallTextures;

Texture3D NoiseTexture0;
Texture3D NoiseTexture1;
Texture3D NoiseTexture2;
Texture3D NoiseTexture3;

#include "3DNoise.fx"

float		Time = 0.0;
float3		LightPosition = float3( 0.0, 0.0, 0.0 );	// Light position within the cube
float		LightRadius = 0.1;							// Shimmering sphere radius
float		LightIntensity = 0.5;
float3		SpherePosition = float3( 0.0, 0.0, 0.0 );	// Shimmering sphere position
float		SphereRadius = 0.2;							// Shimmering sphere radius

float3x3	BumpRotationOctave0;
float3x3	BumpRotationOctave1;
float3x3	BumpRotationOctave2;

float3x3	Quat2Matrix( float3 _Axis, float _Angle )
{
	float3x3	Ret;

	// Convert angle axis into a quaternion
	_Angle = cos( 0.5 * _Angle );
	_Axis *= sqrt( 1.0 - _Angle*_Angle );

	float	xs, ys, zs, wx, wy, wz, xx, xy, xz, yy, yz, zz;

	xs = 2.0 * _Axis.x;	ys = 2.0 * _Axis.y;	zs = 2.0 * _Axis.z;

	wx = _Angle * xs;	wy = _Angle * ys;	wz = _Angle * zs;
	xx = _Axis.x * xs;	xy = _Axis.x * ys;	xz = _Axis.x * zs;
	yy = _Axis.y * ys;	yz = _Axis.y * zs;	zz = _Axis.z * zs;

	Ret[0][0] = 1.0 -	yy - zz;
	Ret[0][1] =			xy + wz;
	Ret[0][2] =			xz - wy;

	Ret[1][0] =			xy - wz;
	Ret[1][1] = 1.0 -	xx - zz;
	Ret[1][2] =			yz + wx;

	Ret[2][0] =			xz + wy;
	Ret[2][1] =			yz - wx;
	Ret[2][2] = 1.0 -	xx - yy;

	return	Ret;
}

// Reads a normal from 3D noise maps
//
float4	FetchNormalHeight3D( float3 _ObjectNormal )
{
	// Transform normals
	float3	Normal0 = mul( _ObjectNormal, BumpRotationOctave0 );
	float3	Normal1 = mul( _ObjectNormal, BumpRotationOctave1 );
	float3	Normal2 = mul( _ObjectNormal, BumpRotationOctave2 );

	float3x3	DeltaRotX = Quat2Matrix( normalize( float3( _ObjectNormal.z, 0.0, -_ObjectNormal.x ) ), 0.01 );
	float3x3	DeltaRotY = Quat2Matrix( float3( 0.0, 1.0, 0.0 ), 0.01 );
	float3		ObjectNormalDx = mul( _ObjectNormal, DeltaRotX );
	float3		ObjectNormalDy = mul( _ObjectNormal, DeltaRotY );

	// Fetch noise in rotated direction + delta rotations for smoothing and normal
	float3		Normal0Dx = mul( Normal0, DeltaRotX );
	float3		Normal0Dy = mul( Normal0, DeltaRotY );
	float3		Trans0 = 0.2 * Time * float3( 0.01, 0.05, -0.04 );
	float3		Pos0   = 0.1 * Normal0   + Trans0;
	float3		Pos0Dx = 0.1 * Normal0Dx + Trans0;
	float3		Pos0Dy = 0.1 * Normal0Dy + Trans0;
	float		Noise0 = NHQu( Pos0, NoiseTexture0 );
	float		Noise0Dx = NHQu( Pos0Dx, NoiseTexture0 );
	float		Noise0Dy = NHQu( Pos0Dy, NoiseTexture0 );

	float3		Normal1Dx = mul( Normal1, DeltaRotX );
	float3		Normal1Dy = mul( Normal1, DeltaRotY );
	float3		Trans1 = 0.4 * Time * float3( 0.01, -0.25, 0.02 );
	float3		Pos1   = 0.2 * Normal1   + Trans1;
	float3		Pos1Dx = 0.2 * Normal1Dx + Trans1;
	float3		Pos1Dy = 0.2 * Normal1Dy + Trans1;
	float		Noise1 = 0.5 * NHQu( Pos1, NoiseTexture1 );
	float		Noise1Dx = 0.5 * NHQu( Pos1Dx, NoiseTexture1 );
	float		Noise1Dy = 0.5 * NHQu( Pos1Dy, NoiseTexture1 );

	// Build heights
	float		H = Noise0 + Noise1;
	float		Hx = Noise0Dx + Noise1Dx;
	float		Hy = Noise0Dy + Noise1Dy;

	// Compute normal from delta heights in delta directions
	float	fNormalAttenuation = 0.04;
	Pos0 = _ObjectNormal * H;
	Pos0Dx = ObjectNormalDx * lerp( H, Hx, fNormalAttenuation );
	Pos0Dy = ObjectNormalDy * lerp( H, Hy, fNormalAttenuation );
	float3	Normal = normalize( cross( Pos0Dx - Pos0, Pos0Dy - Pos0 ) );

	return float4( Normal, H );
}

// Compute the intersection of a ray with the cube and returns the hit face index and the hit UVs
//
void	ComputeWallHit( float3 _Position, float3 _Direction, out int _HitFaceIndex, out float2 _HitUV, out float2 _HitUVX, out float2 _HitUVY, out float2 _HitUVZ, out float _HitDistance )
{
	// Determine which 3 faces we may hit based on direction on each axis
	int		XIndex = _Direction.x < 0.0 ? 0 : 1;	// -X => Left    +X => Right
	int		YIndex = _Direction.y < 0.0 ? 0 : 1;	// -Y => Bottom  +Y => Top
	int		ZIndex = _Direction.z < 0.0 ? 0 : 1;	// -Z => Back    +Z => Front

	// Compute hit distance for each hit face
	float	HitDistanceX = (2.0*XIndex-1.0 - _Position.x) / _Direction.x;
	float	HitDistanceY = (2.0*YIndex-1.0 - _Position.y) / _Direction.y;
	float	HitDistanceZ = (2.0*ZIndex-1.0 - _Position.z) / _Direction.z;

	// Compute hit UV positions for each hit face
	_HitUVX = 0.5 * (1.0 + (_Position + HitDistanceX * _Direction).zy);	// U=Z V=Y
	_HitUVY = 0.5 * (1.0 + (_Position + HitDistanceY * _Direction).xz);	// U=X V=Z
	_HitUVZ = 0.5 * (1.0 + (_Position + HitDistanceZ * _Direction).xy);	// U=X V=Y

	// Now, determine the actual hit face index based on the shortest hit distance
	if ( HitDistanceX < HitDistanceY )
	{
		if ( HitDistanceX < HitDistanceZ )
		{	// We hit X face first
			_HitFaceIndex = 0+XIndex;
			_HitUV = _HitUVX;
			_HitDistance = HitDistanceX;
		}
		else
		{	// We hit Z face first
			_HitFaceIndex = 4+ZIndex;
			_HitUV = _HitUVZ;
			_HitDistance = HitDistanceZ;
		}
	}
	else
	{
		if ( HitDistanceY < HitDistanceZ )
		{	// We hit Y face first
			_HitFaceIndex = 2+YIndex;
			_HitUV = _HitUVY;
			_HitDistance = HitDistanceY;
		}
		else
		{	// We hit Z face first
			_HitFaceIndex = 4+ZIndex;
			_HitUV = _HitUVZ;
			_HitDistance = HitDistanceZ;
		}
	}
}

// Computes the Fresnel term for complex refraction indices
// This is an extension of Shlick's simple model that compensates for complex IOR so we can render metals
// (source http://sirkan.iit.bme.hu/~szirmay/fresnel.pdf)
//
float	ComputeComplexFresnel( float _CosTheta, float k )
{
	float	C = 1.0 - _CosTheta;
	float	C2 = C * C;
	float	C4 = C2 * C2;

	return saturate( ((1.0-IOR)*(1.0-IOR) + 4.0 * IOR * C4*C + k*k) / ((1.0+IOR)*(1.0+IOR) + k*k) );
}

// Computes the wall color from the caustics face index and UVs
//
float3	ComputeWallColor( int _FaceIndex, float2 _UV, float3 _Position )
{
	// Map caustic face ID to wall texture ID
	int		WallTextureIndex = CausticFace2WallTextureIndex[_FaceIndex];

	// Transform face UV to match texture orientation
	float	CausticsColor = CausticsTexture.SampleLevel( LinearWrap, float3( _UV.x, 1.0-_UV.y, _FaceIndex ), 0 ).x;
			CausticsColor = max( 0.01, CausticsColor );	// Make sure it's never 0 so multiplying it by a high lighting value gives something anyway

	// Retrieve diffuse wall texture
	float3	WallColor = WallTextures.Sample( LinearWrap, float3( _UV, WallTextureIndex ) ).xyz;

	// Compute diffuse lighting
	float3	ToLight = LightPosition - _Position;
	float	Distance2Light = length( ToLight );
			ToLight /= Distance2Light;
	float3	WallNormal = CausticFace2Normal[_FaceIndex];
	float	DotDiffuse = saturate( dot( ToLight, WallNormal ) );

	// Compute occlusion by sphere
	float3	ToSphere = SpherePosition - _Position;
	float	Distance2Sphere = length( ToSphere );
			ToSphere /= Distance2Sphere;
	float	DotSolidAngle = 1.0;
	if ( Distance2Light > Distance2Sphere-SphereRadius )
	{	// Account for solid angle attenuation
		float	CosSolidAngle = Distance2Sphere / sqrt( Distance2Sphere*Distance2Sphere + SphereRadius*SphereRadius );
		DotSolidAngle = saturate( (1.0 - saturate( dot( ToLight, ToSphere ) )) / (1.0-CosSolidAngle) );
	}
	DotDiffuse *= DotSolidAngle;	// Mask diffuse
//	return DotSolidAngle;
//	return 0.5 * DotDiffuse;

	return WallColor * (0.1 + 0.3*DotDiffuse + 1*CausticsColor);
//	return WallColor * (0.0 + 0.5*DotDiffuse + 0*1*CausticsColor);
}

