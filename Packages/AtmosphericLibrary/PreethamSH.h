
#ifndef PREETHAMSHFUNC_H
#define PREETHAMSHFUNC_H


#include "poly_coeffs.h"
#include <math.h>

void CalculatePreethamSH(float theta, 
		   float phi, 
		   float turbulence, 
		   int numbands, 
		   bool gibbs_suppression, 
		   float coeffs_r[], //!MUST be the size of numbands*numbands, NO boundary checking
		   float coeffs_g[], 
		   float coeffs_b[], 
		   float scale)//!additional global scale
{
  
	//!Generate the parameter matrix
	double powmat[14][8];
	double thetapow[14];
	double turbpow[8];

	thetapow[0] = powmat[0][0] = 1.0;
	thetapow[1] = powmat[1][0] = theta;
	for(int i = 2;i < 14;++i)  {

		thetapow[i] = thetapow[i-1]*theta;
		powmat[i][0] = thetapow[i];
	}
	turbpow[0] = 1.0;
	turbpow[1] = powmat[0][1] = turbulence;
	for(int j = 2;j < 8;++j)  {

		turbpow[j] = turbpow[j-1]*turbulence;
		powmat[0][j] = turbpow[j];
	}

	for(int i = 0;i < 14;++i)  {
		for(int j = 0;j < 8;++j) {

		powmat[i][j] = thetapow[i]*turbpow[j];

		}
	}


	//! Execute coefficient multiplication for each coefficient
	//! For fixed band number, delete higher bands
	for(int l = 0; l < numbands; ++l)  {
		for(int m = -l; m <=l;++m)  {

			int bandindex =  l+m;

			double cr, cg, cb;
			cr = cg = cb = 0.0;
		
			for(int i = 0;i < 14;++i)  {
				for(int j = 0;j < 8;++j) {

					if(l == 0) {
					cr += powmat[i][j]* PreethamSHband0[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband0[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband0[bandindex][i][j][2];
					}
					if(l == 1) {
					cr += powmat[i][j]* PreethamSHband1[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband1[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband1[bandindex][i][j][2];
					}
					if(l == 2) {
					cr += powmat[i][j]* PreethamSHband2[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband2[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband2[bandindex][i][j][2];
					}
					if(l == 3) {
					cr += powmat[i][j]* PreethamSHband3[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband3[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband3[bandindex][i][j][2];
					}
					if(l == 4) {
					cr += powmat[i][j]* PreethamSHband4[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband4[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband4[bandindex][i][j][2];
					}
					if(l == 5) {
					cr += powmat[i][j]* PreethamSHband5[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband5[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband5[bandindex][i][j][2];
					}
					if(l == 6) {
					cr += powmat[i][j]* PreethamSHband6[bandindex][i][j][0];
					cg += powmat[i][j]* PreethamSHband6[bandindex][i][j][1];
					cb += powmat[i][j]* PreethamSHband6[bandindex][i][j][2];
					}
				}
			}

			int k = l*(l+1) + m;
			coeffs_r[k] =(float)cr;
			coeffs_g[k] =(float)cg;
			coeffs_b[k] =(float)cb;
		}
	}



	for (int l = 0; l < numbands; ++l)
	{
		for (int m = 1; m <= l; ++m) 
		{
			int k_m = l*(l+1) + m;
			int k_minus_m = l*(l+1) - m;

			double c_m_r = coeffs_r[k_m];
			double c_m_g = coeffs_g[k_m];
			double c_m_b = coeffs_b[k_m];

			double c_minus_m_r = coeffs_r[k_minus_m];
			double c_minus_m_g = coeffs_g[k_minus_m];
			double c_minus_m_b = coeffs_b[k_minus_m];
			
			double tcos = cos(m*phi);
			double tsin = sin(m*phi);

			coeffs_r[k_m] = (float)(c_m_r*tcos - c_minus_m_r*tsin);
			coeffs_g[k_m] = (float)(c_m_g*tcos - c_minus_m_g*tsin);
			coeffs_b[k_m] = (float)(c_m_b*tcos - c_minus_m_b*tsin);
			
			coeffs_r[k_minus_m] = (float)(c_minus_m_r*tcos + c_m_r*tsin);
			coeffs_g[k_minus_m] = (float)(c_minus_m_g*tcos + c_m_g*tsin);
			coeffs_b[k_minus_m] = (float)(c_minus_m_b*tcos + c_m_b*tsin);

		
		}
	}

	if(gibbs_suppression)  {


		for(int l = 1; l < numbands; ++l)  {

			int k = l*(l+1);

			double mult = sin(3.1415926535897932*(l)/(numbands))/(3.1415926535897932*(l)/(numbands));

			coeffs_r[k] *= (float)mult;
			coeffs_g[k] *= (float)mult;
			coeffs_b[k] *= (float)mult;
	
		}
		/*for(int l = 1; l < numbands; ++l)  {
			for(int m = -l; m <=l; ++m)  {
			
				if(m != 0) {
					int k = l*(l+1)+m;

					double mult = sin(2*3.1415926535897932*(l)/(numbands))/(2*3.1415926535897932*(l)/(numbands));

					coeffs_r[k] *= mult;
					coeffs_g[k] *= mult;
					coeffs_b[k] *= mult;
				}
	
			}
		}*/

	}

	const int num_coeffs = numbands*numbands;
	for(int i = 0; i < num_coeffs;++i)  {

			coeffs_r[i] *= scale;
			coeffs_g[i] *= scale;
			coeffs_b[i] *= scale;

	}

	}

#endif