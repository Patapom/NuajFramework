// This shader applies Parallax Occlusion Mapping (POM) to a silhouette
//
// A silhouette triangle is different from a standard triangle in that it
//	faces the camera and is extruded from the surface.
//
// As such, rays hitting the triangle can start anywhere in the height map
//	and can miss it entirely.
//
// Some additional shader parameters are available like :
//	_ Height, the height of the pixel from the surface
//	_ CurvatureRadius, the radius of the surface curvature where the silhouette was extruded
//		A low radius will browse closer to the extrusion zone while
//		a large radius will browse further.
//
// It's different from a standard POM as it takes into account the fact
//	that rays are coming at grazing angles on a curved surface.
//
float4 SilhouettePS( PS_IN _In, uniform int _StepsCount ) : SV_Target
{
	// Compute camera ray
	float3	FromPixel = Camera2World[3].xyz - _In.WorldPosition;
	float	fDistance2Pixel = length( FromPixel );
			FromPixel /= fDistance2Pixel;

	// Transform into tangent space
	//	Tangent varies along with U
	//	BiTangent varies along with -V
	//
	float3	Normal = normalize( _In.Normal );
	float3	Tangent = normalize( _In.Tangent );
	float3	BiTangent = normalize( _In.BiTangent );

	float3	View = -float3( dUdV.y * dot( BiTangent, FromPixel ), -abs(dot( Normal, FromPixel )), dUdV.x * dot( Tangent, FromPixel ) );

	// Compute start and end position in tangent space
	float3	CurrentPos = float3( _In.UV.y, _In.CurvatureHeight.z, _In.UV.x ) - View * 0.5;
	float3	EndPos = float3( _In.UV.y, _In.CurvatureHeight.z, _In.UV.x ) + View * 0.1;

	// Now, perform ray marching from start to end in _StepsCount steps and check for an intersection
	float3	Step = (EndPos - CurrentPos) / _StepsCount;
	CurrentPos += 0.5 * Step;	// March a little

	float	fLastHeight = CurrentPos.y;
	float	fCurrentHeight = CurrentPos.y;	// Start at maximum height
	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		fLastHeight = fCurrentHeight;
		fCurrentHeight = Height.x * TexHeight.SampleLevel( TextureSampler, CurrentPos.zx, 0.0 ).r;
		if ( CurrentPos.y <= fCurrentHeight )
			break;			// We're below the current height so we must have intersected some place within the current step !

		CurrentPos += Step;	// March !
	}

	if ( CurrentPos.y > fCurrentHeight )
		discard;	// No intersection...
//		return float4( 0.5, 0.0, 0.0, 1.0 );

	// We found an intersection during the last step
	// We know the height slope and the step slope so we may find a nice approximation of
	//	the exact intersection position by computing the intersection of these 2 segments
	//
	float	fDeltaHeight = fCurrentHeight - fLastHeight;
	float	t = saturate( (CurrentPos.y-Step.y - fLastHeight) / (fDeltaHeight - Step.y) );

	CurrentPos += (t-1.0) * Step;	// Step back a little to the computed intersection

	_In.UV.xy = CurrentPos.zx;

	// =================== Perform Lighting ===================
	float4	DiffuseSpecularColor = TexDiffuseSpecular.Sample( TextureSampler, _In.UV );

	// Sample TS normal and compute WS normal
	float3	LocalNormal = 2.0 * TexNormal.Sample( TextureSampler, _In.UV ).rgb - 1.0;
	Normal = LocalNormal.r * Tangent + LocalNormal.g * BiTangent + LocalNormal.b * Normal;

	// Compute diffuse and specular
 	float	fDiffuse = DotLightUnclamped( Normal );
	float3	Ambient = 0.1 * saturate( 1+fDiffuse ) * float3( 0.1, 0.6, 0.9 );
			fDiffuse = saturate( fDiffuse );

	float3	Half = normalize( FromPixel + LightDirection );
	float	fSpecular = pow( saturate( dot( Half, Normal ) ), 4.0 );
			fSpecular *= DiffuseSpecularColor.a;

	// Compute approximate AO by sampling height map
	float	Dist = 0.02f;
	float	fSumHeight  = TexHeight.SampleLevel( TextureSampler, _In.UV + float2( -Dist*dUdV.x, -Dist*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +0*dUdV.x, -Dist*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +Dist*dUdV.x, -Dist*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( -Dist*dUdV.x, +Dist*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +0*dUdV.x, +Dist*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +Dist*dUdV.x, +Dist*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( -Dist*dUdV.x, +0*dUdV.y ), 0.0 ).r;
			fSumHeight += TexHeight.SampleLevel( TextureSampler, _In.UV + float2( +Dist*dUdV.x, +0*dUdV.y ), 0.0 ).r;
			fSumHeight = fSumHeight * 0.125 * Height.x - fCurrentHeight;
	float	fApertureAngle = atan( fSumHeight / Dist );	// This yields the average aperture angle this point can see of the outer world

	float	fAOFactor = 4.0;
	float	AO = saturate( 1.0 - fAOFactor * fApertureAngle / (0.5 * PI) );

	float3	Result = AO * Ambient + DiffuseSpecularColor.rgb * (fDiffuse + fSpecular) * LightColor.rgb;

	return	float4( Result, 1 );
}
