// Simple lambert shader with texture for the terrain
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../DirectionalLighting.fx"

Texture2D	GnomeTex;

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	UV			: TEXCOORD0;
};

struct PS_IN
{
	float4	__Position	: SV_POSITION;
	float3	Position	: POSITION;
	float3	BillboardPosition	: BILLBOARD_POSITION;
	float3	Normal		: NORMAL;
	float2	UV			: TEXCOORD0;
};


// ===================================================================================
// Display gnome
PS_IN	VS( VS_IN _In )
{
	float	Scale = 1.0;
	float3	X = Scale * Camera2World[0].xyz;
	float3	Y = Scale * Camera2World[1].xyz;
	float3	WorldPosition = _In.Position + X * _In.Normal.x + Y * _In.Normal.y;

	PS_IN	Out;
	Out.__Position = mul( float4( WorldPosition, 1.0 ), World2Proj );
	Out.Position = WorldPosition;
	Out.BillboardPosition = _In.Position;
	Out.Normal = float3( 0, 1, 0 );
	Out.UV = _In.UV;

	return Out;
}

float3	PS( PS_IN _In ) : SV_TARGET0
{
	float4	TextureColor = GnomeTex.Sample( LinearClamp, _In.UV );
	clip( TextureColor.w - 0.5 );

	float3	Normal = normalize( _In.Normal );
	float3	DiffuseFactor = 0.05 + 0.95 * saturate( dot( Normal, LightDirection ) );

	TextureColor *= 0.1;

	return LightColor.xyz * DiffuseFactor * TextureColor.xyz;
}

// ===================================================================================
// Display gnome with vertical constraint
float	GetHitDistance( float3 _Position, float3 _View, float3 _PlanePosition, float3 _PlaneNormal )
{
	float	DeltaPosition = dot( _Position - _PlanePosition, _PlaneNormal );
	float	DeltaView = -dot( _View, _PlaneNormal );
	return DeltaPosition / DeltaView;			// The distance it will take to reach the plane following the view vector
}
// 
// float3	ProjectToCameraPlane( float3 _Position, float3 _View, float3 _PlanePosition )
// {
// 	float3	PlaneNormal = -Camera2World[2].xyz;	// Camera At is the plane's normal
// 	return _Position + GetHitDistance( _Position, _View, _PlanePosition, PlaneNormal ) * _View;
// }
// 
// float2	TransformWorld2UVOLD( float3 _Position, float3 _BillboardPosition, float3 _AxisX, float3 _AxisY )
// {
// 	// Compute the WORLD => UV transform
// 	float3	CameraPosition = Camera2World[3].xyz;
// 	float3	CameraView = Camera2World[2].xyz;
// 	float2	BillboardSize = 5.0 * float2( 361.0 / 402.0, 1.0 );
// 	float3	PositionUV00 = ProjectToCameraPlane( _BillboardPosition + (-0.5 * BillboardSize.x * _AxisX + BillboardSize.y * _AxisY), CameraView, _BillboardPosition );
// 	float3	PositionUV10 = ProjectToCameraPlane( _BillboardPosition + (+0.5 * BillboardSize.x * _AxisX + BillboardSize.y * _AxisY), CameraView, _BillboardPosition );
// 	float3	PositionUV01 = ProjectToCameraPlane( _BillboardPosition + (-0.5 * BillboardSize.x * _AxisX), CameraView, _BillboardPosition );
// 
// 	float3	UVx = PositionUV10 - PositionUV00;
// 			UVx /= dot( UVx, UVx );
// 	float3	UVy = PositionUV01 - PositionUV00;
// 			UVy /= dot( UVy, UVy );
// 
// 	// Now, project actual corner
// 	float3	CornerPosition = ProjectToCameraPlane( _Position, _Position - CameraPosition, _BillboardPosition );
// 
// 	// Recompute UVs
// 	float3	dUV = CornerPosition - PositionUV00;
// 	float2	UV = float2(
// 		dot( dUV, UVx ),
// 		dot( dUV, UVy ) );
// 
// 	return UV;
// }

float2	TransformWorld2UV( float3 _Position, float3 _BillboardPosition )
{
	// Compute the WORLD => UV transform
	float3	CameraPosition = Camera2World[3].xyz;
	float3	CameraView = Camera2World[2].xyz;
	float2	BillboardSize = 5.0 * float2( 361.0 / 402.0, 1.0 );
	float3	AxisX = Camera2World[0].xyz;
	float3	AxisY = Camera2World[1].xyz;
	float3	PositionUV00 = _BillboardPosition + (-0.5 * BillboardSize.x * AxisX + BillboardSize.y * AxisY);
	float3	PositionUV10 = _BillboardPosition + (+0.5 * BillboardSize.x * AxisX + BillboardSize.y * AxisY);
	float3	PositionUV01 = _BillboardPosition + (-0.5 * BillboardSize.x * AxisX);

	float3	UVx = PositionUV10 - PositionUV00;
			UVx /= dot( UVx, UVx );
	float3	UVy = PositionUV01 - PositionUV00;
			UVy /= dot( UVy, UVy );

	// Recompute UVs
	float3	dUV = _Position - PositionUV00;
	float2	UV = float2(
		dot( dUV, UVx ),
		dot( dUV, UVy ) );

	return UV;
}

float3	ProjectToVerticalPlane( float3 _Position, float3 _View, float3 _PlanePosition )
{
	float3	PlaneX = Camera2World[0].xyz;
	float3	PlaneY = float3( 0, 1, 0 );
	float3	PlaneNormal = normalize( cross( PlaneX, PlaneY ) );
	return _Position + GetHitDistance( _Position, _View, _PlanePosition, PlaneNormal ) * _View;
}

float3	ProjectToCameraPlane( float3 _Position, float3 _View, float3 _PlanePosition )
{
	float3	PlaneX = Camera2World[0].xyz;
	float3	PlaneY = Camera2World[1].xyz;
	float3	PlaneNormal = normalize( cross( PlaneX, PlaneY ) );
	return _Position + GetHitDistance( _Position, _View, _PlanePosition, PlaneNormal ) * _View;
}

PS_IN	VS2( VS_IN _In )
{
	float3	CameraPosition = Camera2World[3].xyz;
	float3	X = Camera2World[0].xyz;
	float3	Y = Camera2World[1].xyz;
	float3	WorldPosition = _In.Position + X * _In.Normal.x + Y * _In.Normal.y;

	// Project to vertical plane
	WorldPosition = ProjectToVerticalPlane( CameraPosition, WorldPosition - CameraPosition, _In.Position );

	PS_IN	Out;
	Out.__Position = mul( float4( WorldPosition, 1.0 ), World2Proj );
	Out.Position = WorldPosition;
	Out.BillboardPosition = _In.Position;
	Out.Normal = float3( 0, 1, 0 );
	Out.UV = _In.UV;

	return Out;
}

float3	PS2( PS_IN _In ) : SV_TARGET0
{
	// Project position to camera plane
	float3	CameraPosition = Camera2World[3].xyz;
	float3	Position = ProjectToCameraPlane( CameraPosition, _In.Position - CameraPosition, _In.BillboardPosition );

	// Retrieve UVs
	_In.UV = TransformWorld2UV( Position, _In.BillboardPosition );

	float4	TextureColor = GnomeTex.Sample( LinearClamp, _In.UV );
	clip( TextureColor.w - 0.5 );

	float3	Normal = normalize( _In.Normal );
	float3	DiffuseFactor = 0.05 + 0.95 * saturate( dot( Normal, LightDirection ) );

	TextureColor *= 0.1;

	return LightColor.xyz * DiffuseFactor * TextureColor.xyz;
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS2() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS2() ) );
	}
}
