using System;
using System.Collections.Generic;
using System.Text;

namespace Atmospheric.Constants.Spectra
{
	class Sun
	{
		public static readonly Spectrum.SpectrumRegular		RADIANCE = new Atmospheric.Spectrum.SpectrumRegular( 380e-9, 10e-9, new double[]
		#region Spectrum Values
									{	  16559.0, 16233.7, 21127.5, 25888.2, 25829.1,
										  24232.3, 26760.5, 29658.3, 30545.4, 30057.5,
										  30663.7, 28830.4, 28712.1, 27825.0, 27100.6,
										  27233.6, 26361.3, 25503.8, 25060.2, 25311.6,
										  25355.9, 25134.2, 24631.5, 24173.2, 23685.3,
										  23212.1, 22827.7, 22339.8, 21970.2, 21526.7,
										  21097.9, 20728.3, 20240.4, 19870.8, 19427.2,
										  19072.4, 18628.9, 18259.2 }
		#endregion
		);

		public static readonly Spectrum.SpectrumIrregular	ATTENUATION_OZONE = new Atmospheric.Spectrum.SpectrumIrregular( 
		new double[]
		#region Spectrum Lambas
											{	300e-9, 305e-9, 310e-9, 315e-9, 320e-9,
												325e-9, 330e-9, 335e-9, 340e-9, 345e-9,
												350e-9, 355e-9,
												445e-9, 450e-9, 455e-9, 460e-9, 465e-9,
												470e-9, 475e-9, 480e-9, 485e-9, 490e-9,
												495e-9,
												500e-9, 505e-9, 510e-9, 515e-9, 520e-9,
												525e-9, 530e-9, 535e-9, 540e-9, 545e-9,
												550e-9, 555e-9, 560e-9, 565e-9, 570e-9,
												575e-9, 580e-9, 585e-9, 590e-9, 595e-9,
												600e-9, 605e-9, 610e-9, 620e-9, 630e-9,
												640e-9, 650e-9, 660e-9, 670e-9, 680e-9,
												690e-9,
												700e-9, 710e-9, 720e-9, 730e-9, 740e-9,
												750e-9, 760e-9, 770e-9, 780e-9, 790e-9, },
		#endregion
		new double[]
		#region Spectrum Values
											{	10.0, 4.8, 2.7, 1.35, .8,
												.380, .160, .075, .04, .019,
												.007, .0,
												.003, .003, .004, .006, .008,
												.009, .012, .014, .017, .021,
												.025,
												.03, .035, .04, .045, .048,
												.057, .063, .07, .075, .08,
												.085, .095, .103, .110, .12,
												.122, .12, .118, .115, .12,
												.125, .130, .12, .105, .09,
												.079, .067, .057, .048, .036,
												.028,
												.023, .018, .014, .011, .010,
												.009, .007, .004, .0, .0 }
		#endregion
		);

		public static readonly Spectrum.SpectrumIrregular	ATTENUATION_GASES = new Atmospheric.Spectrum.SpectrumIrregular( 
		new double[]
		#region Spectrum Lambas
											{	759e-9, 760e-9, 770e-9, 771e-9 },
		#endregion
		new double[]
		#region Spectrum Values
											{	0.0,  3.0, 0.210, 0.0 }
		#endregion
		);

		public static readonly Spectrum.SpectrumIrregular	ATTENUATION_WATER = new Atmospheric.Spectrum.SpectrumIrregular( 
		new double[]
		#region Spectrum Lambas
											{	689e-9, 690e-9, 700e-9, 710e-9, 720e-9,
												730e-9, 740e-9, 750e-9, 760e-9, 770e-9,
												780e-9, 790e-9, 800e-9 },
		#endregion
		new double[]
		#region Spectrum Values
											{	0.0, 0.160e-1, 0.240e-1, 0.125e-1, 0.100e+1,
												0.870, 0.610e-1, 0.100e-2, 0.100e-4, 0.100e-4,
												0.600e-3, 0.175e-1, 0.360e-1 }
		#endregion
		);
	}
}
