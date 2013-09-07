// Holds Key/Rim/Fill light support
//
float3	LightPositionKey : LIGHT_KEY_POSITION;
float3	LightDirectionKey : LIGHT_KEY_DIRECTION;
float4	LightColorKey : LIGHT_KEY_COLOR;
float4	LightDataKey : LIGHT_KEY_DATA;
float4	LightData2Key : LIGHT_KEY_DATA2;

float3	LightPositionRim : LIGHT_RIM_POSITION;
float3	LightDirectionRim : LIGHT_RIM_DIRECTION;
float4	LightColorRim : LIGHT_RIM_COLOR;
float4	LightDataRim : LIGHT_RIM_DATA;
float4	LightData2Rim : LIGHT_RIM_DATA2;

float3	LightPositionFill : LIGHT_FILL_POSITION;
float3	LightDirectionFill : LIGHT_FILL_DIRECTION;
float4	LightColorFill : LIGHT_FILL_COLOR;
float4	LightDataFill : LIGHT_FILL_DATA;
float4	LightData2Fill : LIGHT_FILL_DATA2;

// Compute attenuation due to spotlight constraints light near/far range and hotspot/falloff angles
// Also returns the normalized vector pointing from _Position toward _LightPosition
float	ComputeSpotLightAttenuation( float3 _Position, float3 _LightPosition, float3 _LightDirection, float4 _LightData, float4 _LightData2, out float3 _Point2Light )
{
	_Point2Light = _LightPosition - _Position;
	float	Distance2Light = length( _Point2Light );
	if ( Distance2Light > _LightData.y )
		return	0.0;	// Too far !
	_Point2Light /= Distance2Light;

	// Compute angular factor
	float	CosPhaseLight = dot( _Point2Light, _LightDirection );
	if ( CosPhaseLight < _LightData2.y )
		return	0.0;	// Outside of light cone !

	// Square distance attenuation
	float	DistanceAttenuation = saturate( (_LightData.y - Distance2Light) / (_LightData.y - _LightData.x) );
	DistanceAttenuation *= DistanceAttenuation;

	// Square angular attenuation
	float	AngularAttenuation = saturate( (CosPhaseLight - _LightData2.y) / (_LightData2.x - _LightData2.y) );
	AngularAttenuation *= AngularAttenuation;

	return DistanceAttenuation * AngularAttenuation;;
}

float	ComputeSpotLightAttenuationKey( float3 _Position, out float3 _PointToLight ){ return ComputeSpotLightAttenuation( _Position, LightPositionKey, LightDirectionKey, LightDataKey, LightData2Key, _PointToLight ); }
float	ComputeSpotLightAttenuationRim( float3 _Position, out float3 _PointToLight ){ return ComputeSpotLightAttenuation( _Position, LightPositionRim, LightDirectionRim, LightDataRim, LightData2Rim, _PointToLight ); }
float	ComputeSpotLightAttenuationKFill( float3 _Position, out float3 _PointToLight ){ return ComputeSpotLightAttenuation( _Position, LightPositionFill, LightDirectionFill, LightDataFill, LightData2Fill, _PointToLight ); }

// Computes diffuse and specular lighting due to a spotlight
//
void	ComputeSpotLight( float3 _Position, float3 _Normal, float _SpecularPower, float3 _ReflectedView, float3 _LightPosition, float3 _LightDirection, float4 _LightColor, float4 _LightData, float4 _LightData2, out float4 _DiffuseColor, out float4 _SpecularColor )
{
	_DiffuseColor = 0.0;
	_SpecularColor = 0.0;

	float3	Point2Light;
	float	SpotAttenuation = ComputeSpotLightAttenuation( _Position, _LightPosition, _LightDirection, _LightData, _LightData2, Point2Light );

	float	DiffuseDot = dot( Point2Light, _Normal );
	if ( DiffuseDot <= 0.0 )
		return;	// Behind surface !

	float4	AttenuatedColor = _LightColor * DiffuseDot * SpotAttenuation;

	_DiffuseColor = AttenuatedColor;
	_SpecularColor = AttenuatedColor * pow( saturate( dot( _ReflectedView, _LightDirection ) ), _SpecularPower );
}

void	ComputeSpotLightKey( float3 _Position, float3 _Normal, float _SpecularPower, float3 _ReflectedView, out float4 _DiffuseColor, out float4 _SpecularColor ){ ComputeSpotLight( _Position, _Normal, _SpecularPower, _ReflectedView, LightPositionKey, LightDirectionKey, LightColorKey, LightDataKey, LightData2Key, _DiffuseColor, _SpecularColor ); }
void	ComputeSpotLightRim( float3 _Position, float3 _Normal, float _SpecularPower, float3 _ReflectedView, out float4 _DiffuseColor, out float4 _SpecularColor ){ ComputeSpotLight( _Position, _Normal, _SpecularPower, _ReflectedView, LightPositionRim, LightDirectionRim, LightColorRim, LightDataRim, LightData2Rim, _DiffuseColor, _SpecularColor ); }
void	ComputeSpotLightFill( float3 _Position, float3 _Normal, float _SpecularPower, float3 _ReflectedView, out float4 _DiffuseColor, out float4 _SpecularColor ){ ComputeSpotLight( _Position, _Normal, _SpecularPower, _ReflectedView, LightPositionFill, LightDirectionFill, LightColorFill, LightDataFill, LightData2Fill, _DiffuseColor, _SpecularColor ); }


// Computes lighting without shadowing
//
void	ComputeLightingNoShadow( float3 _Position, float3 _Normal, float3 _ToPixel, float _SpecularPower, out float4 _DiffuseColor, out float4 _SpecularColor )
{
 	float3	ReflectedView = reflect( _ToPixel, _Normal );

	// Key light
	ComputeSpotLightKey( _Position, _Normal, _SpecularPower, ReflectedView, _DiffuseColor, _SpecularColor );

	// Rim light
	float4	TempDiffuseColor, TempSpecularColor;
	ComputeSpotLightRim( _Position, _Normal, _SpecularPower, ReflectedView, TempDiffuseColor, TempSpecularColor );
	_DiffuseColor += TempDiffuseColor;
	_SpecularColor += TempSpecularColor;

	// Fill light
	ComputeSpotLightFill( _Position, _Normal, _SpecularPower, ReflectedView, TempDiffuseColor, TempSpecularColor );
	_DiffuseColor += TempDiffuseColor;
	_SpecularColor += TempSpecularColor;
}

#if defined(SHADOW_MAP_SUPPORT)

// Computes lighting with shadowing
//
void	ComputeLightingShadow( float3 _Position, float3 _Normal, float3 _ToPixel, float _SpecularPower, out float4 _DiffuseColor, out float4 _SpecularColor )
{
 	float3	ReflectedView = reflect( _ToPixel, _Normal );

	// Key light
	ComputeSpotLightKey( _Position, _Normal, _SpecularPower, ReflectedView, _DiffuseColor, _SpecularColor );
	float	Shadow = ComputeShadowKey( _Position );
	_DiffuseColor *= Shadow;
	_SpecularColor *= Shadow;

	// Rim light
	float4	TempDiffuseColor, TempSpecularColor;
	ComputeSpotLightRim( _Position, _Normal, _SpecularPower, ReflectedView, TempDiffuseColor, TempSpecularColor );
	Shadow = ComputeShadowRim( _Position );
	_DiffuseColor += Shadow * TempDiffuseColor;
	_SpecularColor += Shadow * TempSpecularColor;

	// Fill light
	ComputeSpotLightFill( _Position, _Normal, _SpecularPower, ReflectedView, TempDiffuseColor, TempSpecularColor );
	Shadow = ComputeShadowFill( _Position );
	_DiffuseColor += Shadow * TempDiffuseColor;
	_SpecularColor += Shadow * TempSpecularColor;
}

#endif

// Computes lighting & shadowing due to Key/Rim/Fill spot lights
//	_Position, the WORLD position
//	_Normal, the WORLD normal direction
//	_ToPixel, the normalized vector from camera to pixel
//	_DiffuseColor [OUT], the resulting diffuse lighting
//	_SpecularColor [OUT], the resulting specular lighting
//
void	ComputeLighting( float3 _Position, float3 _Normal, float3 _ToPixel, float _SpecularPower, out float4 _DiffuseColor, out float4 _SpecularColor )
{
#if defined(SHADOW_MAP_SUPPORT)
	ComputeLightingShadow( _Position, _Normal, _ToPixel, _SpecularPower, _DiffuseColor, _SpecularColor );
#else
	ComputeLightingNoShadow( _Position, _Normal, _ToPixel, _SpecularPower, _DiffuseColor, _SpecularColor );
#endif
}