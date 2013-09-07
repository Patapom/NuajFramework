// ===============================================================
// Include this file to obtain linear tone mapping support
// ===============================================================
//
// Linear tone mapping is really no tone mapping at all, it's simply
// a global reduction factor you apply at the end of your rendering
// so you can light objects with intensities larger than 1 then
// bring back the intensities in the [0,1] range...
//
#if !defined(LINEAR_TONE_MAPPING_SUPPORT_FX)
#define LINEAR_TONE_MAPPING_SUPPORT_FX

float	fToneMappingFactor : TONE_MAPPING_FACTOR = 1.0;
float	fInvGamma : TONE_MAPPING_INV_GAMMA = 1.0 / 2.2;

float3	ApplyToneMapping( float3 _Color )
{
	return _Color * fToneMappingFactor;
}

float3	ApplyToneMappingGamma( float3 _Color )
{
	return pow( _Color * fToneMappingFactor, fInvGamma );
}

#endif