// This shader displays a generated voxel mesh from a stream output
//
#include "../Camera.fx"
#include "../DirectionalLighting.fx"
#include "../LinearToneMapping.fx"

// ===================================================================================

static const float	PI = 3.14159265358979;

// Scattering & Extinction coefficients computation
static const float	EXTINCTION_COEFFICIENT	= 0.046181412007769960605400857734209;	// Extinction Coefficient in m^-1 (Sigma = 0.046181412007769960605400857734209)
static const float	SCATTERING_COEFFICIENT	= EXTINCTION_COEFFICIENT;				// Scattering and extinction are almost the same in clouds because of albedo almost == 1
static const float	EXTINCTION_COEFFICIENT_SINGLE_SCATTERING = 2.0 * EXTINCTION_COEFFICIENT;	// Single scattering extinction is floatd compared to the one used for multiple-scattering

static const float	MARCH_STEP_LENGTH = 8.0;	// The encoded steps in the 3D table for cloud length are multiple of 8 meters
static const float	CLOUD_SIZE_FACTOR = 10.0;	// Actual size in meters from 1 WORLD unit


// Don't forget that the multiple scattering table values must be sampled and added twice
//	as the phase function sums up to 0.25 for each table => Final sum is 0.5
//
// Also, the missing 50% energy comes from single scattering computed analytically through
//	ray marching using EXPONENTIAL steps (that phase function should integrate to 0.5)
//
// Don't forget that EXTINCTION must be floatd for single scattering compared to the one used in
//	the tables, because of the 5° peak cutoff that also reduces the water droplets' cross-section
//
// Don't forget the SH rotation matrix is sparse !
//
// Use ZH for light so they can be rotated per-pixel or vertex
//

//ERROR => Read my comment above you forgetful moron !


// Depth maps
float4x4	World2LightProj;
float4x4	World2Light;
Texture2D	TextureDepthLight;
Texture2D	TextureEnterLight;
Texture2D	TextureExitLight;
Texture2D	TextureDepthCamera;
Texture2D	TextureEnterCamera;
Texture2D	TextureExitCamera;

// Light scattering & SH data
float4x4	World2CameraSH;		// The WORLD->CAMERA matrix to use to transform SH (the Z is reversed compared to the orignal WORLD->CAMERA)
Texture3D	TextureScattering0;	// Texture containing the preprocessed scattering table's SH coefficients 0->3
Texture3D	TextureScattering1;	// Texture containing the preprocessed scattering table's SH coefficients 4->7
Texture3D	TextureScattering2;	// Texture containing the preprocessed scattering table's SH coefficient 8
Texture2D	TexturePhase;		// 1D Texture containing the Mie phase function used for analytical integration of single scattering
float4		SHLight[9];			// Light SH coefficients in CAMERA space
float4		SHLightReversed[9];	// Reverse light SH coefficients in CAMERA space

SamplerState LinearWrap
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

SamplerState LinearClamp
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

struct VS_IN
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
};

struct PS_IN
{
	float4	Position		: SV_POSITION;
	float3	WorldPosition	: TEXCOORD0;
	float3	WorldNormal		: TEXCOORD1;
	float3	ProjPosition	: TEXCOORD2;	// Projected position in camera space
};

PS_IN VS( VS_IN _In )
{
	PS_IN	Out;
	Out.Position = mul( float4( _In.Position, 1 ), World2Proj );
	Out.WorldPosition = _In.Position;
	Out.WorldNormal = _In.Normal;
	Out.ProjPosition = Out.Position.xyz / Out.Position.w;

	return Out;
}

// Displays the color perceived by the pixel given its orientation in WORLD space (its normal)
// It's the simple dot product with the light SH coefficients
//
float3	DebugSH( float3 _Normal )
{
	// Transform normal in CAMERA space (as SH light is in CAMERA space)
	float3	Normal = mul( float4( _Normal, 0 ), World2CameraSH ).xyz;
			Normal = float3( Normal.z, -Normal.x, Normal.y );	// PPSloan convention switch

	// Compute SH coefficients for that direction (from PPSloan paper "Stupid SH tricks")
	float	SH[9];
	float	RSqPI = 0.5 / sqrt(PI);
	SH[0] = RSqPI;
	float	f0 = sqrt(3.0) * RSqPI;
	SH[1] = -f0 * Normal.y;
	SH[2] = f0 * Normal.z;
	SH[3] = -f0 * Normal.x;
	float	f1 = sqrt(15.0) * RSqPI;
	SH[4] = f1 * Normal.y * Normal.x;
	SH[5] = -f1 * Normal.y * Normal.z;
	SH[6] = f1 / (2.0 * sqrt(3.0)) * (3.0 * Normal.z*Normal.z - 1.0);
	SH[7] = -f1 * Normal.x * Normal.z;
	SH[8] = f1 * 0.5 * (Normal.x*Normal.x - Normal.y*Normal.y);

	return	SHLight[0].xyz * SH[0].xxx
		+	SHLight[1].xyz * SH[1].xxx
		+	SHLight[2].xyz * SH[2].xxx
		+	SHLight[3].xyz * SH[3].xxx
		+	SHLight[4].xyz * SH[4].xxx
		+	SHLight[5].xyz * SH[5].xxx
		+	SHLight[6].xyz * SH[6].xxx
		+	SHLight[7].xyz * SH[7].xxx
		+	SHLight[8].xyz * SH[8].xxx;
}

// Retrieves light Entry/Exit points and thickness at specified world position
//	_WorldPosition, the world position inside the cloud to retrieve light infos for
//
void GetLightInfos( float3 _WorldPosition, out float3 _LightEnter, out float3 _LightExit, out float _LightThickness, out float _LightEnter2ExitDistance )
{
	float3	PositionLightProj = mul( float4( _WorldPosition, 1 ), World2LightProj ).xyz;
	float2	UV_Light = 0.5 * (1+PositionLightProj.xy);
			UV_Light.y = 1.0 - UV_Light.y;

	_LightEnter = TextureEnterLight.SampleLevel( LinearClamp, UV_Light, 0 ).xyz;
	_LightExit = TextureExitLight.SampleLevel( LinearClamp, UV_Light, 0 ).xyz;
	_LightThickness = TextureDepthLight.SampleLevel( LinearClamp, UV_Light, 0 ).x;
	_LightEnter2ExitDistance = length( _LightExit - _LightEnter );
	_LightThickness = max( 0, min( _LightThickness, _LightEnter2ExitDistance ) );	// Clamp
}

// Computes the cloud thickness and observer height at provided position
//	_WorldPosition, the position at which to evaluate the cloud's thickness
//	_Thickness, cloud thickness in a direction orthogonal to view direction
//	_Height, observer height within that thickness
//
// NOTE: The precomputed scattering table encoded scattering within slabs of clouds aligned with the view direction. These slabs are in fact cylinders.
// The viewer is positioned at the entry point of the cylinder and watches through, aligned with the cylinder's axis.
// The viewer is also watching from a "relative height" that can go to 0 (bottom of the cylinder) to 1 (top of the cylinder)
//
void	ComputeThicknessHeight( float3 _WorldPosition, out float _Thickness, out float _Height )
{
	// Compute light infos at current position
	float3	LightEnter, LightExit;
	float	LightThickness, LightEnter2ExitLength;
	GetLightInfos( _WorldPosition, LightEnter, LightExit, LightThickness, LightEnter2ExitLength );

	_Thickness = LightEnter2ExitLength;
	_Height = dot( _WorldPosition-LightExit, LightEnter-LightExit ) / LightEnter2ExitLength;
	_Thickness *= CLOUD_SIZE_FACTOR;
	_Height *= CLOUD_SIZE_FACTOR;
}

// Computes the complex in-scattering integral by fetching some data in the table
//	_SHLight, the SH coefficient for the light
//	_CloudLength, the length of the cloud in the view direction
//	_CloudThickness, the thickness of the cloud in direction perpendicular to view
//	_ObserverHeight, the height of the observer within the cloud
//
static const float		SLAB_MAX_THICKNESS = 400.0;		// Maximum encoded thickness
static const float		SLAB_MIN_THICKNESS = 2.0;		// Minimum encoded thickness
static const float		SLAB_STEP_LENGTH = 8.0;			// Each slab length is a multiple of this step (same step we use for ray-marching)
static const int		SLAB_TEXTURE_DEPTH = 32;		// Length is encoded in 32 slices of textures
static const float		SLAB_MAX_LENGTH = SLAB_STEP_LENGTH * SLAB_TEXTURE_DEPTH;	// Maximum encoded length in the table

float3	ComputeInScattering( float4 _SHLight[9], float _CloudLength, float _CloudThickness, float _ObserverHeight )
{
	float3	UVW;

	// Thickness is encoded exponentially within the scattering table using T = MAX * exp( -k.(1-x) )
	// Thus : x = 1 - log(Max) - log(T)) / k
	//
	static const float	k = -log( SLAB_MIN_THICKNESS / SLAB_MAX_THICKNESS );
	static const float	LogMax = log( SLAB_MAX_THICKNESS );
	UVW.x = saturate( 1.0 - (LogMax - log( _CloudThickness )) / k );

	// Observer height is relative to cloud thickness
	UVW.y = _ObserverHeight / _CloudThickness;

	// Finally, the length...
	UVW.z = saturate( _CloudLength / SLAB_MAX_LENGTH );

	// Fetch the SH coeffs
	float4	SH03 = TextureScattering0.SampleLevel( LinearClamp, UVW, 0 ).xyzw;	// Coefficients 0->3
	float4	SH47 = TextureScattering1.SampleLevel( LinearClamp, UVW, 0 ).xyzw;	// Coefficients 4->7
	float	SH8 = TextureScattering2.SampleLevel( LinearClamp, UVW, 0 ).x;		// Coefficient 8

	// Dot with the light, here we are !
	float3	Result = float3( 0.0, 0.0, 0.0 );
	Result += _SHLight[0].xyz * SH03.xxx;
	Result += _SHLight[1].xyz * SH03.yyy;
	Result += _SHLight[2].xyz * SH03.zzz;
	Result += _SHLight[3].xyz * SH03.www;
	Result += _SHLight[4].xyz * SH47.xxx;
	Result += _SHLight[5].xyz * SH47.yyy;
	Result += _SHLight[6].xyz * SH47.zzz;
	Result += _SHLight[7].xyz * SH47.www;
	Result += _SHLight[8].xyz *  SH8.xxx;

	return Result;
}

// This shader performs ray-marching through a slab of cloud using a pre-computed scattering table
// The lighting equation for a participating medium (from "Realistic Image Synthesis using Photon Mapping" by H. Jensen, pp. 122) is :
//
// L  (x,w) = Integral[L(x,w ).p(w,w )] * Sigma * ∆x + Exp[ -Sigma * ∆x ] . L (x+∆x,w)
//  n+1                     s       s          s                  t          n
//
// With :
//	L, the perceived radiance at a given position and for a given view direction
//	x, the position within the cloud currently being computed
//	w, the view direction
//	n, the marching step index
//	w , the in-scattered ray direction (L(x,w ) thus being the in-scattered radiance)
//   s                                       s
//	p(w,w ), the phase function (here, the Mie phase function)
//       s
//	Sigma , the scattering coefficient
//       s
//	Sigma , the extinction coefficient (same as the scattering coefficient in the case of clouds because there is almost no absorption)
//       t
//	∆x, the marching step
//
//
// Conveniently for us, the complex multiple-scattering integral boils down to the pre-computed 3D table.
// The equation then ends up like this :
//
// L  (x,w) = TableFetch[x,w] * Sigma * ∆x + Exp[ -Sigma * ∆x ] . L (x+∆x,w)
//  n+1                              s                  t          n
//
//
float4	PS( PS_IN _In, uniform int _MarchStepsCount = 16 ) : SV_TARGET0
{
//	return float4( DebugSH( _In.WorldNormal ), 1 );

	// Retrieve camera infos
	float2	UV_Camera = 0.5 * (1+_In.ProjPosition.xy);
			UV_Camera.y = 1.0 - UV_Camera.y;

	float3	EnterCamera = TextureEnterCamera.SampleLevel( LinearWrap, UV_Camera, 0 ).xyz;	// In WORLD space
	float3	ExitCamera = TextureExitCamera.SampleLevel( LinearWrap, UV_Camera, 0 ).xyz;		// In WORLD space
	float3	Enter2ExitCamera = ExitCamera - EnterCamera;
	float	Enter2ExitCameraLength = length( Enter2ExitCamera );
	float	CameraDepth = TextureDepthCamera.SampleLevel( LinearWrap, UV_Camera, 0 ).x;		// In WORLD space
//	CameraDepth = max( 0, min( CameraDepth, Enter2ExitCameraLength ) );	// Clamp

	float3	View = Enter2ExitCamera / Enter2ExitCameraLength;
	float3	MarchStep = View * (MARCH_STEP_LENGTH / CLOUD_SIZE_FACTOR);						// One step in WORLD space

	// Compute cloud length & march parameters
//	float	fCloudLength = CLOUD_SIZE_FACTOR * CameraDepth;									// This is the length to ray-march in CLOUD space
	float	fCloudLength = CLOUD_SIZE_FACTOR * Enter2ExitCameraLength;						// This is the length to ray-march in CLOUD space
	int		MarchStepsCount = (int) floor( fCloudLength / MARCH_STEP_LENGTH );				// Amount of steps to perform in CLOUD space
			MarchStepsCount = min( 40, MarchStepsCount );

	float	fStepRemainder = fCloudLength - MarchStepsCount * MARCH_STEP_LENGTH;			// Decimal part of integer march steps
	float	FinalExtinction = exp( -EXTINCTION_COEFFICIENT * 0.5 * fStepRemainder );		// The small extinction factor to apply for the final energy

	float	fInitialMarchedLength = max( 0, MarchStepsCount-0.5 ) * MARCH_STEP_LENGTH		// Start at the end of the cloud, at half the first backward step
									+ 0.5 * fStepRemainder;									// And also half of the remainder step so we're always inside the cloud

	float3	CurrentPosition = EnterCamera + View * fInitialMarchedLength / CLOUD_SIZE_FACTOR;// Start at the end of the cloud

	float	fBackwardCloudLength = fInitialMarchedLength;									// We see a lot more backward as we're yet to exit the cloud
	float	fForwardCloudLength = fCloudLength - fInitialMarchedLength;						// We don't see much forward as we barely entered the cloud

	// Compute the extinction/in-scattering we get for marching one step further
// 	static const float	StepFactorExtinction = exp( -EXTINCTION_COEFFICIENT * MARCH_STEP_LENGTH );
// 	static const float	StepFactorInScattering = SCATTERING_COEFFICIENT * MARCH_STEP_LENGTH;
	float	StepFactorExtinction = exp( -EXTINCTION_COEFFICIENT * (fCloudLength-0.5*fStepRemainder) / MarchStepsCount );
	float	StepFactorInScattering = 0.5 * SCATTERING_COEFFICIENT * (fCloudLength-0.5*fStepRemainder) / MarchStepsCount;

	// Compute thickness & height at current position
	float	T, H;
	ComputeThicknessHeight( CurrentPosition, T, H );

	// March !
	float3	Energy = float3( 0.0, 0.0, 0.0 );
	for ( int MarchStepIndex=0; MarchStepIndex <= MarchStepsCount; MarchStepIndex++ )
	{
		// Perform extinction of previously gathered energy
		Energy *= StepFactorExtinction.xxx;

		// Accumulate in-scattered energy in the FORWARD direction
		Energy += StepFactorInScattering.xxx * ComputeInScattering( SHLight, fForwardCloudLength, T, H );

		// March backward
		fForwardCloudLength += MARCH_STEP_LENGTH;	// Forward length increases
		fBackwardCloudLength -= MARCH_STEP_LENGTH;	// while backward length decreases as we approach the entry point
		CurrentPosition -= MarchStep;				// We come closer to the entry point

		// Compute new thickness and height at new position
		ComputeThicknessHeight( CurrentPosition, T, H );

		// Accumulate in-scattered energy in the BACKWARD direction
		Energy += StepFactorInScattering.xxx * ComputeInScattering( SHLightReversed, fBackwardCloudLength, T, H );
	}

	// Apply final extinction (a little extinction caused by the fact we have a remaining distance to exit the clouds, not an integral step)
//	Energy *= FinalExtinction.xxx;

	return float4( fToneMappingFactor * Energy, 1);
}


// ===================================================================================
// 
// float SilhouetteHeight = 2.0;
// float SilhouetteAngleThreshold = 0.01;
// 
// VS_IN	VS_Silhouette( VS_IN _In )
// {
// 	return _In;	// Pass through
// }
// 
// // Don't you know the fin factor ?? That's what makes your game more or less fin...
// void	GenerateFin( float _FinFactor0, VS_IN _In0, float _FinFactor1, VS_IN _In1, inout TriangleStream<PS_IN> _OutStream )
// {
// 	// Project normals into camera plane
// 	float3	CameraRight = Camera2World[0].xyz;
// 	float3	CameraUp = Camera2World[1].xyz;
// 	float3	Normal0 = dot( _In0.Normal, CameraRight ) * CameraRight + dot( _In0.Normal, CameraUp ) * CameraUp;
// 	float3	Normal1 = dot( _In1.Normal, CameraRight ) * CameraRight + dot( _In1.Normal, CameraUp ) * CameraUp;
// 
// 	PS_IN	Out[4];
// 
// 	Out[0].WorldPosition = _In0.Position + _FinFactor0 * SilhouetteHeight * Normal0;
// 	Out[0].Position = mul( float4( Out[0].WorldPosition, 1 ), World2Proj );
// 	Out[0].WorldNormal = _In0.Position;
// 
// 	Out[1].WorldPosition = _In0.Position;
// 	Out[1].Position = mul( float4( Out[1].WorldPosition, 1 ), World2Proj );
// 	Out[1].WorldNormal = _In0.Position;
// 
// 	Out[2].WorldPosition = _In1.Position + _FinFactor1 * SilhouetteHeight * Normal1;
// 	Out[2].Position = mul( float4( Out[2].WorldPosition, 1 ), World2Proj );
// 	Out[2].WorldNormal = _In1.Position;
// 
// 	Out[3].WorldPosition = _In1.Position;
// 	Out[3].Position = mul( float4( Out[3].WorldPosition, 1 ), World2Proj );
// 	Out[3].WorldNormal = _In1.Position;
// 
// 	_OutStream.Append( Out[0] );
// 	_OutStream.Append( Out[1] );
// 	_OutStream.Append( Out[2] );
// 	_OutStream.RestartStrip();
// 
// 	_OutStream.Append( Out[2] );
// 	_OutStream.Append( Out[1] );
// 	_OutStream.Append( Out[3] );
// 	_OutStream.RestartStrip();
// }
// 
// // Generates a silhouette strip from a triangle if view angle falls below a given angle threshold
// [maxvertexcount( 6 )]
// void GS_Silhouette( triangle VS_IN _In[3], inout TriangleStream<PS_IN> _OutStream )
// {
// 	float3	CameraWorldPosition = Camera2World[3].xyz;
// 
// 	float3	Views[3] =
// 	{
// 		CameraWorldPosition - _In[0].Position,
// 		CameraWorldPosition - _In[1].Position,
// 		CameraWorldPosition - _In[2].Position
// 	};	
// 
// 	// Prepare 1/Z values for perspective correction (good aulde times striking back ^_^)
// 	// Actually, I'm lying, I'm using 1/distance...
// 	float	Iz[3] =
// 	{
// 		1.0 / length( Views[0] ),
// 		1.0 / length( Views[1] ),
// 		1.0 / length( Views[2] )
// 	};
// 
// 	Views[0] *= Iz[0];
// 	Views[1] *= Iz[1];
// 	Views[2] *= Iz[2];
// 
// 	float3	Normals[3] =
// 	{
// 		normalize( _In[0].Normal ),
// 		normalize( _In[1].Normal ),
// 		normalize( _In[2].Normal )
// 	};
// 
// 	float	Dots[3] =
// 	{
// 		dot( Views[0], Normals[0] ),
// 		dot( Views[1], Normals[1] ),
// 		dot( Views[2], Normals[2] )
// 	};
// 
// 	// Decrease dots using some cos(angle) threshold
// 	Dots[0] -= SilhouetteAngleThreshold;
// 	Dots[1] -= SilhouetteAngleThreshold;
// 	Dots[2] -= SilhouetteAngleThreshold;
// 
// 	int	FrontCount  = Dots[0] >= 0.0 ? 1 : 0;
// 		FrontCount += Dots[1] >= 0.0 ? 1 : 0;
// 		FrontCount += Dots[2] >= 0.0 ? 1 : 0;
// 	if ( FrontCount == 3 )
// 		return;	// This face is completely above the horizon
// 	if ( FrontCount == 0 )
// 		return;	// This face is completely below the horizon
// 	
// 	// We're handling the case where only one vertex is below the horizon (i.e. front count = 2)
// 	// If we have two points below the horizon then we simple invert the dot products
// 	int		I0, I1, I2;
// 	float	t0, t1;
// 	if ( FrontCount == 1 )
// 	{	// 2 points below the horizon, reversed case => Revert triangle indices otherwise strip will be generated backward !
// 		// Make sure I0 always is the index of the point above the horizon
// 		if ( Dots[1] > 0.0 )		{ I0 = 1; I1 = 0; I2 = 2; }
// 		else if ( Dots[2] > 0.0 )	{ I0 = 2; I1 = 1; I2 = 0; }
// 		else						{ I0 = 0; I1 = 2; I2 = 1; }
// 	}
// 	else
// 	{	// 2 points above the horizon, expected case
// 		// Make sure I0 always is the index of the point below the horizon
// 		if ( Dots[1] < 0.0 )		{ I0 = 1; I1 = 2; I2 = 0; }
// 		else if ( Dots[2] < 0.0 )	{ I0 = 2; I1 = 0; I2 = 1; }
// 		else						{ I0 = 0; I1 = 1; I2 = 2; }
// 	}
// 	
// 	// Compute the 2 intersections with the camera ray and the horizon
// 	t0 = (Dots[I1] - 0.0) / (Dots[I1] - Dots[I0]);
// 	t1 = (Dots[I2] - 0.0) / (Dots[I2] - Dots[I0]);
// 
// 	// Interpolate left data using perspective correction
// 	float	ZL = 1.0 / lerp( Iz[I1], Iz[I0], t0 );
// 	VS_IN	Left;
// 	Left.Position = lerp( _In[I1].Position * Iz[I1], _In[I0].Position * Iz[I0], t0 ) * ZL;
// 	Left.Normal = lerp( _In[I1].Normal * Iz[I1], _In[I0].Normal * Iz[I0], t0 ) * ZL;
// 	Left.Normal = normalize( Left.Normal );
// 	float3	LeftView = lerp( Views[I1], Views[I0], t0 );
// 
// 	// Interpolate right data using perspective correction
// 	VS_IN	Right;
// 	float	ZR = 1.0 / lerp( Iz[I2], Iz[I0], t1 );
// 	Right.Position = lerp( _In[I2].Position * Iz[I2], _In[I0].Position * Iz[I0], t1 ) * ZR;
// 	Right.Normal = lerp( _In[I2].Normal * Iz[I2], _In[I0].Normal * Iz[I0], t1 ) * ZR;
// 	Right.Normal = normalize( Right.Normal );
// 	float3	RightView = lerp( Views[I2], Views[I0], t1 );
// 
// 	GenerateFin( 1-dot( Left.Normal, LeftView ), Left, 1-dot( Right.Normal, RightView ), Right, _OutStream );
// }
// 
// float4 PS_Silhouette( PS_IN _In, uniform int _StepsCount ) : SV_Target
// {
// 	return float4( 1, 0, 0, 1 );	// RED !! ^___^
// }

// ===================================================================================
// Default technique displays the volume using quads and ray-marching
technique10 DisplayVoxel
{
	// Main rendering
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}

// 	// Silhouette rendering
// 	pass P1
// 	{
// 		SetVertexShader( CompileShader( vs_4_0, VS_Silhouette() ) );
// 		SetGeometryShader( CompileShader( gs_4_0, GS_Silhouette() ) );
// 		SetPixelShader( CompileShader( ps_4_0, PS_Silhouette( 40 ) ) );
// 	}
}
