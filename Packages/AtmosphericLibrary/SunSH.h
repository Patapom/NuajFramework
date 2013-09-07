
#include <math.h>

const float PI = (float)3.1415926535897932;
const float SQRT2 = (float)1.4142135623730950488016887242097;

inline
int doubleFactorial(int x)
{
	int result;

	if (x == 0 || x == -1) return (1);

	result = x;
	while ((x -= 2) > 0) result *= x;

	return (result);
}

inline
int factorial(int x)
{
	int result;

	if (x == 0 || x == -1) return (1);

	result = x;
	while ((x -= 1) > 0) result *= x;

	return (result);
}

/* Compute the associated Legendre polynomial for x for indexes l and m */
float ALPStd(float x, int l, int m)
{
	if (l == m) return (pow((float)(-1),m) * doubleFactorial(2 * m - 1) * pow(sqrt(1 - x * x), m));

	if (l == m + 1) return (x * (2 * m + 1) * ALPStd(x, m, m));

	return ((x * (2 * l - 1) * ALPStd(x, l - 1, m) - (l + m - 1) * ALPStd(x, l - 2, m)) / (l - m));
}

inline
float evaluateK(int l, int m)
{
	float result;

	result = (float) (((2.0 * l + 1.0) * factorial(l - m)) / (4 * PI * factorial(l + m)));

	return sqrt((result));
}

inline
float evaluateSH(int l, int m, float theta, float phi)
{
	float SH = 0.0;

	if (m == 0)
	{
		SH = evaluateK(l, 0) * ALPStd(cos(theta), l, 0);
	}
	else if (m > 0)
	{
		SH = SQRT2 * evaluateK(l, m) * cos(m * phi) * ALPStd(cos(theta), l, m);
	}
	else
	{
		SH = SQRT2 * evaluateK(l, -m) * sin(-m * phi) * ALPStd(cos(theta), l, -m);
	}

	return SH;
}

void CalculateSunSH(float theta, 
		   float phi, 
		   float turbidity, 
		   int numbands, 
		   float coeffs_r[], //!MUST be the size of numbands*numbands, NO boundary checking
		   float coeffs_g[], 
		   float coeffs_b[], 
		   float scale)//!additional global scale
{

	
	float thetacos2 = cos( theta) * cos( theta );


#define CBQ(X)		((X) * (X) * (X))
#define SQR(X)		((X) * (X))

	

	float zenithColor_x  = ( 0.00165f * CBQ(theta) - 0.00374f  * SQR(theta) +
			 0.00208f *       theta +     0.0f) * SQR(turbidity) +
		   (-0.02902f * CBQ(theta) + 0.06377f  * SQR(theta) -
		   	 0.03202f *       theta  + 0.00394f) *        turbidity +
		   ( 0.11693f * CBQ(theta) - 0.21196f  * SQR(theta) +
	   		 0.06052f *       theta + 0.25885f);

	float zenithColor_y  = ( 0.00275f * CBQ(theta) - 0.00610f  * SQR(theta) +
			 0.00316f *       theta +     0.0f) * SQR(turbidity) +
		   (-0.04214f * CBQ(theta) + 0.08970f  * SQR(theta) -
		     0.04153f *       theta  + 0.00515f) *turbidity  +
		   ( 0.15346f * CBQ(theta) - 0.26756f  * SQR(theta) +
		     0.06669f *       theta  + 0.26688f);

	float zenithColor_z  = (float)((4.0453f * turbidity - 4.9710f) *
			tan((4.0f / 9.0f - turbidity / 120.0f) * (PI - 2.0f * theta)) -
			0.2155f * turbidity + 2.4192f);
	// convert kcd/m² to cd/m²
	zenithColor_z *= 1000;

	float ABCDE_x[5], ABCDE_y[5], ABCDE_Y[5];

		ABCDE_x[0] = -0.01925f * turbidity   - 0.25922f;
		ABCDE_x[1] = -0.06651f * turbidity   + 0.00081f;
		ABCDE_x[2] = -0.00041f * turbidity   + 0.21247f;
		ABCDE_x[3] = -0.06409f * turbidity   - 0.89887f;
		ABCDE_x[4] = -0.00325f * turbidity   + 0.04517f;

		ABCDE_y[0] = -0.01669f * turbidity  - 0.26078f;
		ABCDE_y[1] = -0.09495f * turbidity   + 0.00921f;
		ABCDE_y[2] = -0.00792f * turbidity   + 0.21023f;
		ABCDE_y[3] = -0.04405f * turbidity   - 1.65369f;
		ABCDE_y[4] = -0.01092f * turbidity   + 0.05291f;

		ABCDE_Y[0] =  0.17872f * turbidity   - 1.46303f;
		ABCDE_Y[1] = -0.35540f * turbidity   + 0.42749f;
		ABCDE_Y[2] = -0.02266f * turbidity   + 5.32505f;
		ABCDE_Y[3] =  0.12064f * turbidity   - 2.57705f;
		ABCDE_Y[4] = -0.06696f * turbidity   + 0.37027f;

    

	float num_x = (1.0f + ABCDE_x[0] * exp( ABCDE_x[1] / cos(theta))) * (1.0f + ABCDE_x[2])+ ABCDE_x[4];    
	float num_y = (1.0f + ABCDE_y[0] * exp( ABCDE_y[1] / cos(theta))) * (1.0f + ABCDE_y[2])+ ABCDE_y[4];   
	float num_z = (1.0f + ABCDE_Y[0] * exp( ABCDE_Y[1] / cos(theta))) * (1.0f + ABCDE_Y[2])+ ABCDE_Y[4];  
 	
	float den_x = (1.0f + ABCDE_x[0] * exp( ABCDE_x[1] )) * (1.0f + ABCDE_x[2] * exp( ABCDE_x[3] * theta ) + ABCDE_x[4] * thetacos2);  
	float den_y  = (1.0f + ABCDE_y[0] * exp( ABCDE_y[1] )) * (1.0f + ABCDE_y[2] * exp( ABCDE_y[3] * theta ) + ABCDE_y[4] * thetacos2);   
	float den_z  = (1.0f + ABCDE_Y[0] * exp( ABCDE_Y[1] )) * (1.0f + ABCDE_Y[2] * exp( ABCDE_Y[3] * theta ) + ABCDE_Y[4] * thetacos2);   


    float  t_x = num_x/den_x *zenithColor_x;
	float  t_y = num_y/den_y *zenithColor_y;
	float  t_Y = num_z/den_z *zenithColor_z;    
                                                                                                                                                                                                       
    float X = (t_x / t_y) * t_Y;                                                                        
    float Y = t_Y;                                                                                          
    float Z = ((1.0f - t_x - t_y ) / t_y ) * t_Y;                                                                
	
	float color_r = 3.240479f*X -1.537150f*Y-0.498535f*Z;
	float color_g = -0.969256f*X +1.875992f*Y +0.041556f*Z;	
	float color_b = 0.055648f*X -0.204043f*Y +1.057311f*Z;

	color_r *= scale;
	color_g *= scale;
	color_b *= scale;


	for(int l = 0; l < numbands; ++l)  {
		for(int m = -l; m <=l;++m)  {

		int bandindex =  l+m;

			float val  = evaluateSH(l, m, theta, phi);
			
			int k = l*(l+1) + m;
			coeffs_r[k] +=color_r*val;
			coeffs_g[k] +=color_g*val;
			coeffs_b[k] +=color_b*val;



		}
	}




}