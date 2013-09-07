using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus.Atmosphere
{
	/// <summary>
	/// The sky class supports an adaptation of "Display of Earth Taking into Account Atmospheric Scattering" (1993)
	///  by Nishita et Al. (http://nis-lab.is.s.u-tokyo.ac.jp/~nis/cdrom/sig93_nis.pdf)
	/// 
	/// The class supports a software as well as hardware rendering of the sky.
	/// Software rendering is also used to encode sky irradiance into 9 SH coefficients that can be
	/// used for environment rendering (the feature is disabled by default but you can enable the
	/// auto SH generation through the "EncodeSkyIntoSH" property)
	/// </summary>
	public class SkySupport : Nuaj.Component, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const int					DENSITY_TEXTURE_SIZE = 256;			// Size of the density texture

		protected const double				EARTH_RADIUS_KM = 6400.0f;						// Earth radius (in kilometers)
		protected const double				ATMOSPHERE_RADIUS_KM = EARTH_RADIUS_KM + 60.0f;	// Atmosphere radius (in kilometers)
		protected const double				H0_AIR = 7.994;						// Altitude scale factor for air molecules
		protected const double				H0_AEROSOLS = 1.200;				// Altitude scale factor for aerosols
		protected const float				WORLD_UNIT_TO_KILOMETER = 0.01f;	// 1 World Unit equals XXX kilometers
		protected const int					DENSITY_TEXTURE_STEPS_COUNT = 128;	// Ray marching steps count for the density texture precomputation

		protected const int					DEFAULT_MARCHING_STEPS_COUNT = 8;	// Ray marching steps count for default sky evaluations (used for Sun & Zenith computations)

		protected const int					SKY_SH_SAMPLES_COUNT = 32;			// The amount of samples along theta for SH sky encoding (total samples is 2*SamplesCount²)
		protected const int					SKY_SH_MARCHING_STEPS_COUNT = 4;	// Ray marching steps count for the SH evaluation

		#endregion

		#region NESTED TYPES

		protected class		ISkySupport : ShaderInterfaceBase
		{
			[Semantic( "SKYSUPPORT_MIE_ANISOTROPY" )]
			public float		MieAnisotropy	{ set { SetScalar( "SKYSUPPORT_MIE_ANISOTROPY", value ); } }
			[Semantic( "SKYSUPPORT_DENSITY_RAYLEIGH" )]
			public float		DensityRayleigh	{ set { SetScalar( "SKYSUPPORT_DENSITY_RAYLEIGH", value ); } }
			[Semantic( "SKYSUPPORT_DENSITY_MIE" )]
			public float		DensityMie		{ set { SetScalar( "SKYSUPPORT_DENSITY_MIE", value ); } }
			[Semantic( "SKYSUPPORT_SIGMA_RAYLEIGH" )]
			public Vector3		SigmaRayleigh	{ set { SetVector( "SKYSUPPORT_SIGMA_RAYLEIGH", value ); } }
			[Semantic( "SKYSUPPORT_SIGMA_MIE" )]
			public float		SigmaMie		{ set { SetScalar( "SKYSUPPORT_SIGMA_MIE", value ); } }
			[Semantic( "SKYSUPPORT_SUN_INTENSITY" )]
			public float		SunIntensity	{ set { SetScalar( "SKYSUPPORT_SUN_INTENSITY", value ); } }
			[Semantic( "SKYSUPPORT_SKY_ZENITH" )]
			public Vector3		SkyZenith		{ set { SetVector( "SKYSUPPORT_SKY_ZENITH", value ); } }
			[Semantic( "SKYSUPPORT_SUN_DIRECTION" )]
			public Vector3		SunDirection	{ set { SetVector( "SKYSUPPORT_SUN_DIRECTION", value ); } }
			[Semantic( "SKYSUPPORT_DENSITY_TEXTURE" )]
			public ITexture2D	DensityTexture	{ set { SetResource( "SKYSUPPORT_DENSITY_TEXTURE", value ); } }
			[Semantic( "SKYSUPPORT_UNITS_SCALE" )]
			public float		WorldUnit2Kilometer	{ set { SetScalar( "SKYSUPPORT_UNITS_SCALE", value ); } }
		}

		public delegate double	ComputeFogDensityDelegate( double _AltitudeKm );

		#endregion

		#region FIELDS

		protected Texture2D<PF_RGBA16F>		m_DensityTexture = null;
		protected Vector4[,]				m_DensityTextureCPU = new Vector4[DENSITY_TEXTURE_SIZE,DENSITY_TEXTURE_SIZE];	

		// Scale parameters
		protected float						m_WorldUnit2Kilometer = WORLD_UNIT_TO_KILOMETER;
		protected float						m_Kilometer2WorldUnit = 1.0f / WORLD_UNIT_TO_KILOMETER;

		// Sky parameters
		protected float						m_ScatteringAnisotropy = 0.75f;
		protected float						m_DensityRayleigh = 1e-5f * 4.0f;
		protected float						m_DensityMie = 1e-4f * 8.0f;
		protected Vector3					m_Wavelengths = new Vector3( 0.650f, 0.570f, 0.475f );	// RGB wavelengths λ in µm 

		// Sun parameters
		protected float						m_SunIntensity = 100.0f;
		protected float						m_SunPhi = 0.0f;
		protected float						m_SunTheta = 0.0f * (float) Math.PI / 180.0f;

		// Cached values
		protected Vector3					m_SigmaRayleigh = Vector3.Zero;	//4.0f * (float) Math.PI * m_DensityRayleigh * INV_WAVELENGTHS_POW4;
		protected float						m_SigmaMie = 0.0f;				//4.0f * (float) Math.PI * m_DensityMie;
		protected Vector3					m_SkyZenith = Vector3.Zero;
		protected Vector3					m_SkyAmbient = Vector3.Zero;
		protected Vector3					m_SunDirection = Vector3.UnitY;

		// Cached SH
		protected bool						m_bEncodeSkyIntoSH = false;
		protected Vector4[]					m_CachedSkySH = new Vector4[9];

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the scale factor converting WORLD units into kilometers
		/// </summary>
		public float			WorldUnit2Kilometer		{ get { return m_WorldUnit2Kilometer; } set { m_WorldUnit2Kilometer = value; m_Kilometer2WorldUnit = 1.0f / value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets the scale factor converting kilometers into WORLD units
		/// </summary>
		public float			Kilometer2WorldUnit		{ get { return m_Kilometer2WorldUnit; } }

		/// <summary>
		/// Gets or sets the prefered scattering direction in [-1,+1] (backward/forward)
		/// </summary>
		public float			ScatteringAnisotropy	{ get { return m_ScatteringAnisotropy; } set { m_ScatteringAnisotropy = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the global density of air molecules (i.e. Rayleigh scattering)
		/// </summary>
		public float			DensityRayleigh			{ get { return m_DensityRayleigh; } set { m_DensityRayleigh = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the global density of aerosols (i.e. Mie scattering)
		/// </summary>
		public float			DensityMie				{ get { return m_DensityMie; } set { m_DensityMie = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the wavelengths (in µm) at which the sky scattering is computed
		/// </summary>
		public Vector3			Wavelengths				{ get { return m_Wavelengths; } set { m_Wavelengths = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the intensity of the Sun
		/// </summary>
		public float			SunIntensity			{ get { return m_SunIntensity; } set { m_SunIntensity = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the Sun's azimuth (in radians)
		/// </summary>
		public float			SunPhi					{ get { return m_SunPhi; } set { m_SunPhi = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the Sun's elevation (in radians)
		/// </summary>
		public float			SunTheta				{ get { return m_SunTheta; } set { m_SunTheta = value; UpdateCachedValues(); } }

		/// <summary>
		/// Gets or sets the direction of the sun (equivalent to setting both azimuth and elevation)
		/// </summary>
		/// <remarks>The vector is pointing TOWARD the Sun</remarks>
		public Vector3			SunDirection
		{
			get { return m_SunDirection; }
			set
			{
				value.Normalize();
				m_SunDirection = value;
				m_SunTheta = (float) Math.Acos( value.Y );
				m_SunPhi = (float) Math.Atan2( value.X, value.Z );

				UpdateCachedValues();
			}
		}

		/// <summary>
		/// Gets the color of the Sun at sea level using the current elevation and density parameters
		/// </summary>
		public Vector3			SunColor				{ get { return ComputeSunColor( Vector3.Zero, SunDirection ); } }

		/// <summary>
		/// Gets the color at Zenith at sea level using the current elevation and density parameters
		/// </summary>
		public Vector3			ZenithColor				{ get { return m_SkyZenith; } }

		/// <summary>
		/// Gets the color at Zenith at sea level using the current elevation and density parameters
		/// </summary>
		public Vector3			SkyAmbientColor			{ get { return m_SkyAmbient; } }

		/// <summary>
		/// Gets or sets the SH encoding flag. If true, every time a sky parameter changes the sky is also encoded into SH coefficients.
		/// </summary>
		public bool				EncodeSkyIntoSH			{ get { return m_bEncodeSkyIntoSH; } set { m_bEncodeSkyIntoSH = value; RebuildSH(); } }

		/// <summary>
		/// Gets the SH coefficients encoding the sky light
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Vector4[]		SkyLightSH				{ get { return m_CachedSkySH; } }

		#endregion

		#region METHODS

		public SkySupport( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Declare our sky support shader interface and register ourselves as a provider
			m_Device.DeclareShaderInterface( typeof(ISkySupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(ISkySupport), this );

			// Create the density/extinction texture
			BuildDensityTexture();

			// Update cached values
			UpdateCachedValues();
		}

		public override void Dispose()
		{
			m_DensityTexture.Dispose();
			base.Dispose();
		}

		/// <summary>
		/// Computes the ambient sky color
		/// It's quite an ugly and not very accurate piece of code but it does the job...
		/// </summary>
		/// <returns></returns>
		protected Vector3	ComputeAmbientSkyColor()
		{
			Vector3	SumColor = m_SkyZenith;
			int		SumCount = 1;
			Vector3	View, Extinction;
			float	Terminator;

			int	THETA_COUNT = 3;
			int	PHI_COUNT = 4;

			for ( int Y=0; Y < THETA_COUNT; Y++ )
			{
				float	Theta = 0.5f * (0.5f + Y) * (float) Math.PI / THETA_COUNT;
				View.Y = (float) Math.Cos( Theta );
				float	fSinTheta = (float) Math.Sin( Theta );
				for ( int X=0; X < PHI_COUNT; X++ )
				{
					float	Phi = 2.0f * (float) Math.PI * X / PHI_COUNT;
					View.X = (float) Math.Cos( Phi ) * fSinTheta;
					View.Z = (float) Math.Sin( Phi ) * fSinTheta;
					SumColor += ComputeSkyColor( Vector3.Zero, View, out Extinction, out Terminator, 4 );
					SumCount++;
				}
			}

			return SumColor / SumCount;
		}

		/// <summary>
		/// Default density texture build
		/// </summary>
		public void		BuildDensityTexture()
		{
//			BuildDensityTexture( ( double _AltitudeKm ) => { return Math.Exp( -_AltitudeKm / H0_AEROSOLS ); } );
			BuildDensityTexture( ( double _AltitudeKm ) => { return Math.Exp( -Math.Max( 0.0, _AltitudeKm - 10.0) / 7.0 ); } );
		}

		/// <summary>
		/// Computes the texture storing atmosphere density for particules responsible for Rayleigh and Mie scattering (i.e. air molecules and aerosols respectively)
		/// The U direction varies along with view angle where U=0 is UP and U=1 is DOWN
		/// The V direction varies along with altitude where V=0 is 0m (i.e. sea level) and V=1 is 100km (i.e. top of the atmosphere)
		/// 
		/// The XY components of the texture store the Rayleigh/Mie density at current altitude
		/// The ZW components of the texture store the Rayleigh/Mie densities accumulated from the start altitude to the top of the atmosphere by following the view vector
		/// </summary>
		public void		BuildDensityTexture( ComputeFogDensityDelegate _ComputeFogDensity )
		{
			if ( m_DensityTexture != null )
				m_DensityTexture.Dispose();	// Dispose first...
			m_DensityTexture = null;

			Vector2	Pos = new Vector2();
			Vector2	View = new Vector2();

			using ( Image<PF_RGBA16F> DensityImage = new Image<PF_RGBA16F>( m_Device, "Density Image", DENSITY_TEXTURE_SIZE, DENSITY_TEXTURE_SIZE,
				( int _X, int _Y, ref Vector4 _Color ) =>
					{
						double	AltitudeKm = (ATMOSPHERE_RADIUS_KM - EARTH_RADIUS_KM) * _Y / (DENSITY_TEXTURE_SIZE-1);
						View.Y = 1.0f - 2.0f * _X / (DENSITY_TEXTURE_SIZE-1);
						View.X = (float) Math.Sqrt( 1.0f - View.Y*View.Y );

						// Compute intersection of ray with upper atmosphere
						double	D = EARTH_RADIUS_KM + AltitudeKm;
						double	b = D * View.Y;
						double	c = D*D-ATMOSPHERE_RADIUS_KM*ATMOSPHERE_RADIUS_KM;
						double	Delta = Math.Sqrt( b*b-c );
						double	HitDistanceKm = Delta - b;	// Distance at which we hit the upper atmosphere (in kilometers)

						// Compute air molecules and aerosols density at current altitude
						_Color.X = (float) Math.Exp( -AltitudeKm / H0_AIR );
						_Color.Y = (float) _ComputeFogDensity( AltitudeKm );

						// Accumulate densities along the ray
						double	SumDensityRayleigh = 0.0;
						double	SumDensityMie = 0.0;

						float	StepLengthKm = (float) HitDistanceKm / DENSITY_TEXTURE_STEPS_COUNT;
						Pos.X = 0.5f * StepLengthKm * View.X;
						Pos.Y = (float) D + 0.5f * StepLengthKm * View.Y;

						for ( int StepIndex=0; StepIndex < DENSITY_TEXTURE_STEPS_COUNT; StepIndex++ )
						{
							AltitudeKm = Pos.Length() - EARTH_RADIUS_KM;	// Relative height from sea level
							AltitudeKm = Math.Max( 0.0, AltitudeKm );	// Don't go below the ground...

							// Compute and accumulate densities at current altitude
							double	Rho_air = Math.Exp( -AltitudeKm / H0_AIR );
							double	Rho_aerosols = _ComputeFogDensity( AltitudeKm );
							SumDensityRayleigh += Rho_air;
							SumDensityMie += Rho_aerosols;

							// March
							Pos.X += StepLengthKm * View.X;
							Pos.Y += StepLengthKm * View.Y;
						}

						SumDensityRayleigh *= HitDistanceKm / DENSITY_TEXTURE_STEPS_COUNT;
						SumDensityMie *= HitDistanceKm / DENSITY_TEXTURE_STEPS_COUNT;

						// Write accumulated densities (clamp because of Float16)
						_Color.Z = (float) Math.Min( 1e4, SumDensityRayleigh );
						_Color.W = (float) Math.Min( 1e4, SumDensityMie );

						// Copy values for our CPU texture equivalent
						m_DensityTextureCPU[_X,_Y] = _Color;

					}, 1 ) )
			m_DensityTexture = new Texture2D<PF_RGBA16F>( m_Device, "Sky Density Texture", DensityImage );
		}

		#region Software Sky Computation

		/// <summary>
		/// Computes the color of the sky given the provided view position & direction and using the current density parameters
		/// </summary>
		/// <param name="_ViewPosition">The viewing position (in WORLD space)</param>
		/// <param name="_ViewDirection">The viewing direction (in WORLD space)</param>
		/// <param name="_BackgroundColor">The background color that will be attenuated by the atmosphere
		/// (use 0 for empty space and 1 for occluded space, then use as a multiplier of the  existing background)</param>
		/// <param name="_Terminator">the terminator value (1 is fully lit, 0 is in Earth's shadow)</param>
		/// <returns>The color of the sky</returns>
		public Vector3	ComputeSkyColor( Vector3 _ViewPosition, Vector3 _ViewDirection, out Vector3 _Extinction, out float _Terminator, int _StepsCount )
		{
			// Compute view ray intersection with the upper atmosphere
			double	CameraHeight = m_WorldUnit2Kilometer * _ViewPosition.Y;	// Relative height from sea level
			double	D = CameraHeight + EARTH_RADIUS_KM;
			double	b = D * _ViewDirection.Y;
			double	c = D*D-ATMOSPHERE_RADIUS_KM*ATMOSPHERE_RADIUS_KM;
			double	Delta = Math.Sqrt( b*b-c );
			double	HitDistance = Delta - b;		// Distance at which we hit the upper atmosphere (in kilometers)
			HitDistance /= m_WorldUnit2Kilometer;	// Back in WORLD units

			// Return color of the sky at that position
			return ComputeSkyColor( _ViewPosition, _ViewDirection, (float) HitDistance, out _Extinction, out _Terminator, _StepsCount );
		}

		/// <summary>
		/// Computes the color of the sky given the provided view position & direction and using the current density parameters
		/// </summary>
		/// <param name="_ViewPosition">The viewing position (in WORLD space)</param>
		/// <param name="_ViewDirection">The viewing direction (in WORLD space)</param>
		/// <param name="_ViewDistance">The distance to the point we're viewing (in WORLD space)</param>
		/// <param name="_BackgroundColor">The background color that will be attenuated by the atmosphere
		/// (use 0 for empty space and 1 for occluded space, then use as a multiplier of the  existing background)</param>
		/// <param name="_Terminator">the terminator value (1 is fully lit, 0 is in Earth's shadow)</param>
		/// <returns>The color of the sky</returns>
		public Vector3	ComputeSkyColor( Vector3 _ViewPosition, Vector3 _ViewDirection, float _ViewDistance, out Vector3 _Extinction, out float _Terminator, int _StepsCount )
		{
			Vector3	InvWavelengthPow4 = new Vector3(
				(float) Math.Pow( m_Wavelengths.X, -4.0 ),
				(float) Math.Pow( m_Wavelengths.Y, -4.0 ),
				(float) Math.Pow( m_Wavelengths.Z, -4.0 ) );

			// Compute camera height & hit distance in kilometers
			double	Height = EARTH_RADIUS_KM + m_WorldUnit2Kilometer * _ViewPosition.Y;
			double	HitDistance = m_WorldUnit2Kilometer * _ViewDistance;

			// Compute phases
			float	CosTheta = Vector3.Dot( _ViewDirection, SunDirection );
			float	PhaseRayleigh = 0.75f * (1.0f + CosTheta*CosTheta);
			float	PhaseMie = 1.0f / (1.0f + m_ScatteringAnisotropy * CosTheta);
					PhaseMie = (1.0f - m_ScatteringAnisotropy*m_ScatteringAnisotropy) * PhaseMie * PhaseMie;

			// Compute potential intersection with earth's shadow
			Vector3	CurrentPosition = new Vector3( 0.0f, (float) Height, 0.0f );

			_Terminator = 1.0f;
			if ( Vector3.Dot( CurrentPosition, SunDirection ) < 0.0 )
			{	// Project current position in the 2D plane normal to the light to test the intersection with the shadow cylinder cast by the Earth
				Vector3	X = Vector3.Cross( SunDirection, _ViewDirection );
				X.Normalize();
				Vector3	Y = Vector3.Cross( X, SunDirection );
				Vector2	P = new Vector2( Vector3.Dot( CurrentPosition, X ), Vector3.Dot( CurrentPosition, Y ) );
				Vector2	V = new Vector2( Vector3.Dot( _ViewDirection, X ), Vector3.Dot( _ViewDirection, Y ) );
				double	a = Vector2.Dot( V, V );
				double	b = Vector2.Dot( P, V );
				double	c = Vector2.Dot( P, P ) - EARTH_RADIUS_KM*EARTH_RADIUS_KM;
				double	Delta = b*b - a*c;
				if ( Delta >= 0.0f )
					_Terminator = 1.0f - (float) Math.Max( 0.0, Math.Min( 1.0, (-b+Math.Sqrt(Delta)) / (a * HitDistance) ) );
			}

			// Ray-march the view ray
			Vector3	AccumulatedLightRayleigh = Vector3.Zero;
			Vector3	AccumulatedLightMie = Vector3.Zero;
			_Extinction = Vector3.One;

			float	StepSize = (float) HitDistance / _StepsCount;
			Vector3	Step = StepSize * _ViewDirection;

			Vector3	ExtinctionRayleigh = Vector3.Zero;
			float	ExtinctionMie = 0.0f;
			for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
			{
				// =============================================
				// Sample extinction at current altitude and view direction
				Vector4	OpticalDepth = ComputeOpticalDepth( CurrentPosition, m_SunDirection );

				// Retrieve densities
				float	Rho_air = OpticalDepth.X;
				float	Rho_aerosols = OpticalDepth.Y;

				// =============================================
				// Retrieve sun light attenuated when passing through the atmosphere
				ExtinctionRayleigh.X = (float) Math.Exp( -m_SigmaRayleigh.X * OpticalDepth.Z );
				ExtinctionRayleigh.Y = (float) Math.Exp( -m_SigmaRayleigh.Y * OpticalDepth.Z );
				ExtinctionRayleigh.Z = (float) Math.Exp( -m_SigmaRayleigh.Z * OpticalDepth.Z );
				ExtinctionMie = (float) Math.Exp( -m_SigmaMie * OpticalDepth.W );
				Vector3	Light = m_SunIntensity * (ExtinctionRayleigh * ExtinctionMie);

				// =============================================
				// Compute in-scattered light
				Vector3	InScatteringRayleigh = Light * Rho_air * m_DensityRayleigh * PhaseRayleigh * StepSize;
						InScatteringRayleigh.X *= InvWavelengthPow4.X * _Extinction.X;
						InScatteringRayleigh.Y *= InvWavelengthPow4.Y * _Extinction.Y;
						InScatteringRayleigh.Z *= InvWavelengthPow4.Z * _Extinction.Z;
				Vector3	InScatteringMie = Light * Rho_aerosols * m_DensityMie * PhaseMie * StepSize;
						InScatteringMie.X *= _Extinction.X;
						InScatteringMie.Y *= _Extinction.Y;
						InScatteringMie.Z *= _Extinction.Z;

				// Accumulate light
				AccumulatedLightRayleigh += InScatteringRayleigh;
				AccumulatedLightMie += InScatteringMie;

				// =============================================
				// Perform extinction of previous step's energy
				ExtinctionRayleigh.X = (float) Math.Exp( -m_SigmaRayleigh.X * Rho_air * StepSize );
				ExtinctionRayleigh.Y = (float) Math.Exp( -m_SigmaRayleigh.Y * Rho_air * StepSize );
				ExtinctionRayleigh.Z = (float) Math.Exp( -m_SigmaRayleigh.Z * Rho_air * StepSize );
				ExtinctionMie = (float) Math.Exp( -m_SigmaMie * Rho_aerosols * StepSize );
	
				_Extinction.X *= ExtinctionRayleigh.X * ExtinctionMie;
				_Extinction.Y *= ExtinctionRayleigh.Y * ExtinctionMie;
				_Extinction.Z *= ExtinctionRayleigh.Z * ExtinctionMie;

				// March
				CurrentPosition += Step;
			}

			return AccumulatedLightRayleigh + AccumulatedLightMie;
		}

		/// <summary>
		/// Computes the color of the Sun given the provided direction and using the current density parameters
		/// </summary>
		/// <param name="_ViewPosition">The viewing position (in WORLD space)</param>
		/// <param name="_SunDirection">The NORMALIZED Sun direction (in WORLD space, and pointing TOWARD the Sun)</param>
		/// <returns>The color of the Sun after passing through the atmosphere</returns>
		public Vector3	ComputeSunColor( Vector3 _ViewPosition, Vector3 _SunDirection )
		{
			Vector3	Extinction;
			float	Terminator;
			ComputeSkyColor( _ViewPosition, _SunDirection, out Extinction , out Terminator, DEFAULT_MARCHING_STEPS_COUNT );

			return m_SunIntensity * Extinction;
		}

		/// <summary>
		/// Gets the rayleigh & mie densities from the density texture
		/// ρ(s,s') = Integral[s,s']( ρ(h(l)) dl )
		/// </summary>
		/// <param name="_ViewPosition"></param>
		/// <param name="_ViewDirection"></param>
		/// <returns></returns>
		protected Vector4	ComputeOpticalDepth( Vector3 _ViewPosition, Vector3 _ViewDirection )
		{
			Vector3	EarthNormal = _ViewPosition;
			float	Altitude = EarthNormal.Length();
			EarthNormal /= Altitude;

			// Normalize altitude
			Altitude = Math.Max( 0.0f, (float) ((Altitude - EARTH_RADIUS_KM) / (ATMOSPHERE_RADIUS_KM - EARTH_RADIUS_KM)) );

			// Actual view direction
			float	CosTheta = Vector3.Dot( _ViewDirection, EarthNormal );

			Vector2	UV = new Vector2( 0.5f * (1.0f - CosTheta), Altitude );
			return SampleDensityTexture( UV );
		}

		protected Vector4	SampleDensityTexture( Vector2 _UV )
		{
			float	U = _UV.X * DENSITY_TEXTURE_SIZE;
			int		X0 = (int) Math.Floor( U );
			float	s = U - X0;
					X0 = Math.Min( DENSITY_TEXTURE_SIZE-1, X0 );
			int		X1 = Math.Min( DENSITY_TEXTURE_SIZE-1, X0+1 );

			float	V = _UV.Y * DENSITY_TEXTURE_SIZE;
			int		Y0 = (int) Math.Floor( V );
			float	t = V - Y0;
					Y0 = Math.Min( DENSITY_TEXTURE_SIZE-1, Y0 );
			int		Y1 = Math.Min( DENSITY_TEXTURE_SIZE-1, Y0+1 );

			Vector4	C00 = m_DensityTextureCPU[X0,Y0];
			Vector4	C01 = m_DensityTextureCPU[X1,Y0];
			Vector4	C10 = m_DensityTextureCPU[X0,Y1];
			Vector4	C11 = m_DensityTextureCPU[X1,Y1];

			Vector4	C0 = Vector4.Lerp( C00, C01, s );
			Vector4	C1 = Vector4.Lerp( C10, C11, s );
			return Vector4.Lerp( C0, C1, t );
		}

		protected void	RebuildSH()
		{
			if ( !m_bEncodeSkyIntoSH )
				return;	// Disabled...

			m_CachedSkySH = BuildSkySH( Vector3.Zero, true );
		}

		/// <summary>
		/// Builds the SH coefficients for the sky at the provided position
		/// </summary>
		/// <param name="_ViewPosition">The position to view the sky from</param>
		/// <param name="_MaskBelowHorizon">True to set the colors to 0 below the horizon</param>
		/// <returns>9 SH vectors (only RGB is used, the alpha is for covenience with the lighting stage that takes Vector4)</returns>
		public Vector4[]	BuildSkySH( Vector3 _ViewPosition, bool _MaskBelowHorizon )
		{
			WMath.Vector[]	Coefficients = new WMath.Vector[9];
			WMath.Vector	Direction = new WMath.Vector();
			Vector3			DXDirection, SkyColor, Extinction;
			float			Terminator;
			SphericalHarmonics.SHFunctions.EncodeIntoSH( Coefficients, SKY_SH_SAMPLES_COUNT, 2*SKY_SH_SAMPLES_COUNT, 1, 3,
				( double _Theta, double _Phi, WMath.Vector _Value ) =>
				{
					SphericalHarmonics.SHFunctions.SphericalToCartesian( _Theta, _Phi, Direction );
					if ( _MaskBelowHorizon && Direction.z < 0.0f )
					{	// Mask colors below the horizon
						// TODO: slowly attenuate to 0 to avoid ringing
						Direction.z = 0.0f;
						Direction.Normalize();
					}

					// Transform from SH basis to our basis (a simple shift)
					DXDirection.X = Direction.y;
					DXDirection.Y = Direction.z;
					DXDirection.Z = Direction.x;

					// Evaluate sky color in that direction
					SkyColor = ComputeSkyColor( _ViewPosition, DXDirection, out Extinction, out Terminator, SKY_SH_MARCHING_STEPS_COUNT );

					_Value.x = SkyColor.X;
					_Value.y = SkyColor.Y;
					_Value.z = SkyColor.Z;
				} );

			Vector4[]	Result = new Vector4[9];
			for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
			{
				Result[SHCoeffIndex].X = Coefficients[SHCoeffIndex].X;
				Result[SHCoeffIndex].Y = Coefficients[SHCoeffIndex].Y;
				Result[SHCoeffIndex].Z = Coefficients[SHCoeffIndex].Z;
			}

			//////////////////////////////////////////////////////////////////////////
			// Also compute Sun SH in W
			float	SunIntensity = Vector3.Dot( SunColor, new Vector3( 0.2126f, 0.7152f, 0.0722f ) );
//			float	SunIntensity = m_SunIntensity;

			// Cosine lobe
			double[]	ZHCoeffs = new double[3]
			{
				0.88622692545275801364908374167057,	// sqrt(PI) / 2
				1.0233267079464884884795516248893,	// sqrt(PI / 3)
				0.49541591220075137666812859564002	// sqrt(5PI) / 8
			};

			// Cone
// 			double		Angle = Math.PI / 8.0;
// 			double[]	ZHCoeffs = new double[]
// 			{
// 				1.7724538509055160272981674833411 * (1.0 - Math.Cos(Angle)),											// sqrt(PI) (1 - cos(a))
// 				1.5349900619197327327193274373339 * Math.Sin(Angle) * Math.Sin(Angle),									// 0.5 sqrt(3PI) sin(a)^2
// 				1.9816636488030055066725143825601 * Math.Cos(Angle) * (Math.Cos(Angle) - 1.0) * (Math.Cos(Angle) + 1.0)	// 0.5 sqrt(5PI) cos(a) (cos(a)-1) (cos(a)+1)
// 			};

			double	cl0 = 3.5449077018110320545963349666823 * ZHCoeffs[0];
			double	cl1 = 2.0466534158929769769591032497785 * ZHCoeffs[1];
			double	cl2 = 1.5853309190424044053380115060481 * ZHCoeffs[2];

			double	f0 = 0.5 / Math.Sqrt(Math.PI);
			double	f1 = Math.Sqrt(3.0) * f0;
			double	f2 = Math.Sqrt(15.0) * f0;
			f0 *= cl0;
			f1 *= cl1;
			f2 *= cl2;

			Result[0].W = SunIntensity * (float) f0;
			Result[1].W = SunIntensity * (float) (-f1 * SunDirection.X);
			Result[2].W = SunIntensity * (float) (f1 * SunDirection.Y);
			Result[3].W = SunIntensity * (float) (-f1 * SunDirection.Z);
			Result[4].W = SunIntensity * (float) (f2 * SunDirection.X * SunDirection.Z);
			Result[5].W = SunIntensity * (float) (-f2 * SunDirection.X * SunDirection.Y);
			Result[6].W = SunIntensity * (float) (f2 / (2.0 * Math.Sqrt(3.0)) * (3.0 * SunDirection.Y*SunDirection.Y - 1.0));
			Result[7].W = SunIntensity * (float) (-f2 * SunDirection.Z * SunDirection.Y);
			Result[8].W = SunIntensity * (float) (f2 * 0.5 * (SunDirection.Z*SunDirection.Z - SunDirection.X*SunDirection.X));

			return Result;
		}

		#endregion

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			ISkySupport	I = _Interface as ISkySupport;

			I.DensityMie = m_DensityMie;
			I.DensityRayleigh = m_DensityRayleigh;
			I.SigmaMie = m_SigmaMie;
			I.SigmaRayleigh = m_SigmaRayleigh;
			I.MieAnisotropy = m_ScatteringAnisotropy;
			I.SunIntensity = m_SunIntensity;
			I.SkyZenith = m_SkyZenith;
			I.SunDirection = m_SunDirection;
			I.WorldUnit2Kilometer = m_WorldUnit2Kilometer;
			I.DensityTexture = m_DensityTexture;
		}

		#endregion

		protected void	UpdateCachedValues()
		{
// 		protected static readonly Vector3	WAVELENGTHS = n
// 		protected static readonly Vector3	WAVELENGTHS_POW4 = new Vector3( 0.17850625f, 0.10556001f, 0.050906640625f );		// λ^4 
// 		protected static readonly Vector3	INV_WAVELENGTHS_POW4 = new Vector3( 5.6020447463324113301354994573019f, 9.4732844379230354373782268493533f, 19.643802610477206282947491194819f );	// 1/λ^4 

			Vector3	InvWavelengthPow4 = new Vector3(
				(float) Math.Pow( m_Wavelengths.X, -4.0 ),
				(float) Math.Pow( m_Wavelengths.Y, -4.0 ),
				(float) Math.Pow( m_Wavelengths.Z, -4.0 ) );

			m_SigmaMie = 4.0f * (float) Math.PI * m_DensityMie;
			m_SigmaRayleigh = 4.0f * (float) Math.PI * m_DensityRayleigh * InvWavelengthPow4;
			m_SunDirection = new Vector3(
				(float) (Math.Sin( m_SunPhi ) * Math.Sin( m_SunTheta )),
				(float) Math.Cos( m_SunTheta ),
				(float) (Math.Cos( m_SunPhi ) * Math.Sin( m_SunTheta )) );


			// Compute sky zenith value
			Vector3	Extinction;
			float	Terminator;
			m_SkyZenith = ComputeSkyColor( Vector3.Zero, Vector3.UnitY, out Extinction, out Terminator, DEFAULT_MARCHING_STEPS_COUNT );

			m_SkyAmbient = ComputeAmbientSkyColor();

			// Rebuild the cached SH coefficients
			RebuildSH();
		}

		#endregion
	}
}
