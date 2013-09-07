// ===============================================================
// Include this file to obtain tone mapping informations support
// ===============================================================
//
#if !defined(TONE_MAPPING_SUPPORT_FX)
#define TONE_MAPPING_SUPPORT_FX

#include "Samplers.fx"

Texture2D	ImageAverageLuminance	: IMAGE_LUMINANCE;

// Gets the average luminance of the tone mapped image
//
float	GetImageAverageLuminance()
{
	return ImageAverageLuminance.SampleLevel( NearestClamp, 0.5, 0 ).x;
}

#endif