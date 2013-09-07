using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Deferred Rendering Technique for Sky drawing (should be called last)
	/// Rendering is using an adaptation of "Display of Earth Taking into Account Atmospheric Scattering" (1993)
	///  by Nishita et Al. (http://nis-lab.is.s.u-tokyo.ac.jp/~nis/cdrom/sig93_nis.pdf)
	/// </example>
	public class EmissiveRenderingSky : DeferredRenderTechnique
	{
		#region CONSTANTS

		protected const double				EARTH_RADIUS = 6400.0f;				// Earth radius (in kilometers)
		protected const double				ATMOSPHERE_RADIUS = 6500.0f;		// Atmosphere radius (in kilometers)
		protected const double				H0_AIR = 7.994;						// Altitude scale factor for air molecules
		protected const double				H0_AEROSOLS = 1.200;				// Altitude scale factor for aerosols
		protected const double				WORLD_UNIT_TO_KILOMETER = 0.1;		// 1 World Unit equals XXX kilometers
		protected const int					DENSITY_TEXTURE_SIZE = 256;			// Size of the density texture
		protected const int					STEPS_COUNT = 32;					// Ray marching steps count for the density texture

		protected static readonly Vector3	WAVELENGTHS = new Vector3( 0.650f, 0.570f, 0.475f );								// RGB wavelengths λ in µm 
		protected static readonly Vector3	WAVELENGTHS_POW4 = new Vector3( 0.17850625f, 0.10556001f, 0.050906640625f );		// λ^4 
		protected static readonly Vector3	INV_WAVELENGTHS_POW4 = new Vector3( 5.6020447463324113301354994573019f, 9.4732844379230354373782268493533f, 19.643802610477206282947491194819f );	// 1/λ^4 

		#endregion

		#region FIELDS

		protected Material<VS_Pt4V3T2>		m_Material = null;

		protected Texture2D<PF_RGBA16F>		m_DensityTexture = null;
		protected ITexture2D				m_NightSkyCubeMap = null;
		protected Helpers.ScreenQuad		m_SkyQuad = null;

		// The downsampled geometry buffer coming from the lighting technique
		// We use it to trace inside it and cast volumetric shadows
		protected IRenderTarget				m_LightGeometryBuffer = null;

		protected ITexture2D				m_TestCubeMap0 = null;
		protected ITexture2D				m_TestCubeMap1 = null;

		// Sky parameters
		protected float						m_ScatteringAnisotropy = -0.75f;
		protected float						m_DensityRayleigh = 4.0f;
		protected float						m_DensityMie = 8.0f;

		protected float						m_SunIntensity = 1000.0f;
		protected float						m_SunPhi = 0.0f;
		protected float						m_SunTheta = 80.0f * (float) Math.PI / 180.0f;
		protected float						m_FinalFactor = 4.0f;

		// Cached SH
		protected Vector4[]					m_CachedSkySH = new Vector4[9];

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Browsable( false )]
		public IRenderTarget	LightGeometryBuffer		{ get { return m_LightGeometryBuffer; } set { m_LightGeometryBuffer = value; } }

		[System.ComponentModel.Browsable( false )]
		public ITexture2D		TestCubeMap0			{ get { return m_TestCubeMap0; } set { m_TestCubeMap0 = value; } }
		[System.ComponentModel.Browsable( false )]
		public ITexture2D		TestCubeMap1			{ get { return m_TestCubeMap1; } set { m_TestCubeMap1 = value; } }

		public float			ScatteringAnisotropy	{ get { return m_ScatteringAnisotropy; } set { m_ScatteringAnisotropy = value; RebuildSH(); } }
		public float			DensityRayleigh			{ get { return m_DensityRayleigh; } set { m_DensityRayleigh = value; RebuildSH(); } }
		public float			DensityMie				{ get { return m_DensityMie; } set { m_DensityMie = value; RebuildSH(); } }
		public float			SunIntensity			{ get { return m_SunIntensity; } set { m_SunIntensity = value; RebuildSH(); } }
		public float			SunPhi					{ get { return m_SunPhi * 180.0f / (float) Math.PI; } set { m_SunPhi = value * (float) Math.PI / 180.0f; RebuildSH(); } }
		public float			SunTheta				{ get { return m_SunTheta * 180.0f / (float) Math.PI; } set { m_SunTheta = value * (float) Math.PI / 180.0f; RebuildSH(); } }
		public float			FinalFactor				{ get { return m_FinalFactor; } set { m_FinalFactor = value; RebuildSH(); } }

		[System.ComponentModel.Browsable( false )]
		public Vector3			SunDirection
		{
			get
			{
				return new Vector3(	(float) (Math.Sin( m_SunPhi ) * Math.Sin( m_SunTheta )),
									(float) Math.Cos( m_SunTheta ),
									(float) (Math.Cos( m_SunPhi ) * Math.Sin( m_SunTheta )) );
			}
			set
			{
				value.Normalize();
				m_SunTheta = (float) Math.Acos( value.Y );
				m_SunPhi = (float) Math.Atan2( value.X, value.Z );
			}
		}
		/// <summary>
		/// Gets the color of the Sun at sea level using the current elevation and density parameters
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Vector3			SunColor				{ get { return ComputeSunColor( Vector3.Zero, SunDirection ); } }

		/// <summary>
		/// Gets the SH coefficients encoding the sky light
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Vector4[]		SkyLightSH				{ get { return m_CachedSkySH; } }

		#endregion

		#region METHODS

		public EmissiveRenderingSky( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Sky Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/RenderEmissiveSky.fx" ) ) );

//			m_NightSkyCubeMap = ToDispose( Texture2D<PF_RGBA8>.CreateFromFile( m_Device, "NightSkyCube", new System.IO.FileInfo( "Media/CubeMaps/Sky_Space_01_1024.dds" ) ) );
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "./Media/CubeMaps/milkywaypan_brunier_2048.jpg" ) as System.Drawing.Bitmap )
				using ( ImageCube<PF_RGBA8> I = new ImageCube<PF_RGBA8>( m_Device, "NightSkyImage", B, ImageCube<PF_RGBA8>.FORMATTED_IMAGE_TYPE.CYLINDRICAL, 0, 2.2f ))
					m_NightSkyCubeMap = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "NightSkyCube", I ) );

			// Create our sky quad as a back screen quad
			m_SkyQuad = ToDispose( new Helpers.ScreenQuad( m_Device, "Sky Quad", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, false ) );

			// Create the extinction texture
			CreateDensityTexture();

			// Build cached SH coefficients
			RebuildSH();
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Main Pass", "Render Sky" );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				CurrentMaterial.GetVariableByName( "ScreenInfos" ).AsVector.Set( new Vector2( 1.0f / m_Device.DefaultRenderTarget.Width, 1.0f / m_Device.DefaultRenderTarget.Height ) );

				float	fDensityRayleigh = 1e-5f * m_DensityRayleigh;
				float	fDensityMie = 1e-4f * m_DensityMie;
				Vector3	ExtinctionRayleigh = 4.0f * (float) Math.PI * fDensityRayleigh * INV_WAVELENGTHS_POW4;
				float	ExtinctionMie = 4.0f * (float) Math.PI * fDensityMie;

				// Computation parameters
				CurrentMaterial.GetVariableByName( "MiePhaseAnisotropy" ).AsScalar.Set( m_ScatteringAnisotropy );
				CurrentMaterial.GetVariableByName( "K_RAYLEIGH" ).AsScalar.Set( fDensityRayleigh );
				CurrentMaterial.GetVariableByName( "K_MIE" ).AsScalar.Set( fDensityMie );
				CurrentMaterial.GetVariableByName( "ExtinctionCoeffRayleigh" ).AsVector.Set( ExtinctionRayleigh );
				CurrentMaterial.GetVariableByName( "ExtinctionCoeffMie" ).AsScalar.Set( ExtinctionMie );
				CurrentMaterial.GetVariableByName( "DensityTexture" ).AsResource.SetResource( m_DensityTexture );
	 			CurrentMaterial.GetVariableByName( "NightSkyCubeMap" ).AsResource.SetResource( m_NightSkyCubeMap );
	 			CurrentMaterial.GetVariableByName( "GeometryBuffer" ).AsResource.SetResource( m_LightGeometryBuffer );


CurrentMaterial.GetVariableByName( "EnvironmentTestCubeMap0" ).AsResource.SetResource( m_TestCubeMap0 );
CurrentMaterial.GetVariableByName( "EnvironmentTestCubeMap1" ).AsResource.SetResource( m_TestCubeMap1 );


				// Light parameters
				CurrentMaterial.GetVariableByName( "SunColor" ).AsVector.Set( m_SunIntensity * Vector3.One );
				CurrentMaterial.GetVariableByName( "SunDirection" ).AsVector.Set( SunDirection );

				CurrentMaterial.GetVariableByName( "FinalFactor" ).AsScalar.Set( m_FinalFactor );


// 2 Passes method
// 				// Write sky
// 				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.NOWRITE_CLOSEST_OR_EQUAL );
// 				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawSky" );
// 				CurrentMaterial.Render( ( A, B, C ) => { m_SkyQuad.Render(); } );
// 
// 				// Write atmospheric perspective
// 				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.NOWRITE_FARTHEST );
// 				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawAtmosphericScattering" );
// 				CurrentMaterial.Render( ( A, B, C ) => { m_SkyQuad.Render(); } );

// Single pass method
				// Write both sky & atmospheric perspective in a single pass
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
 				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawHybrid" );
 				CurrentMaterial.Render( ( A, B, C ) => { m_SkyQuad.Render(); } );
			}
		}

		/// <summary>
		/// Computes the texture storing precomputed atmosphere density for particules responsible
		///  for Rayleigh and Mie scattering (i.e. air molecules and aerosols respectively)
		/// The U direction varies along with view angle where U=0 is UP and U=1 is DOWN
		/// The V direction varies along with altitude where V=0 is 0m (i.e. sea level) and V=1 is 100km (i.e. top of the atmosphere)
		/// 
		/// The XY components of the texture store the Rayleigh/Mie density at current altitude
		/// The ZW components of the texture store the Rayleigh/Mie densities accumulated up to the top of
		///		the atmosphere from the start altitude to the top of the atmosphere by following the view vector
		/// </summary>
		protected Vector4[,]	m_DensityTextureCPU = new Vector4[DENSITY_TEXTURE_SIZE,DENSITY_TEXTURE_SIZE];	
		protected void	CreateDensityTexture()
		{
			Vector2	Pos = new Vector2();
			Vector2	View = new Vector2();

			using ( Image<PF_RGBA16F> DensityImage = new Image<PF_RGBA16F>( m_Device, "Density Image", DENSITY_TEXTURE_SIZE, DENSITY_TEXTURE_SIZE,
				( int _X, int _Y, ref Vector4 _Color ) =>
					{
						double	Altitude = (ATMOSPHERE_RADIUS - EARTH_RADIUS) * _Y / 255;
						View.Y = 1.0f - 2.0f * _X / 255;
						View.X = (float) Math.Sqrt( 1.0f - View.Y*View.Y );

						// Compute intersection of ray with upper atmosphere
						double	D = EARTH_RADIUS + Altitude;
						double	b = D * View.Y;
						double	c = D*D-ATMOSPHERE_RADIUS*ATMOSPHERE_RADIUS;
						double	Delta = Math.Sqrt( b*b-c );
						double	HitDistance = Delta - b;	// Distance at which we hit the upper atmosphere (in kilometers)

						// Compute air molecules and aerosols density at current altitude
						_Color.X = (float) Math.Exp( -Altitude / H0_AIR );
						_Color.Y = (float) Math.Exp( -Altitude / H0_AEROSOLS );

						// Accumulate densities along the ray
						double	SumDensityRayleigh = 0.0;
						double	SumDensityMie = 0.0;

						float	StepLength = (float) HitDistance / STEPS_COUNT;
						Pos.X = 0.5f * StepLength * View.X;
						Pos.Y = (float) D + 0.5f * StepLength * View.Y;

						for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
						{
							Altitude = Pos.Length() - EARTH_RADIUS;	// Relative height from sea level
							Altitude = Math.Max( 0.0, Altitude );	// Don't go below the ground...

							// Compute and accumulate densities at current altitude
							double	Rho_air = Math.Exp( -Altitude / H0_AIR );
							double	Rho_aerosols = Math.Exp( -Altitude / H0_AEROSOLS );
							SumDensityRayleigh += Rho_air;
							SumDensityMie += Rho_aerosols;

							// March
							Pos.X += StepLength * View.X;
							Pos.Y += StepLength * View.Y;
						}

						SumDensityRayleigh *= HitDistance / STEPS_COUNT;
						SumDensityMie *= HitDistance / STEPS_COUNT;

						// Write accumulated densities
						_Color.Z = (float) Math.Min( 1e4, SumDensityRayleigh );
						_Color.W = (float) Math.Min( 1e4, SumDensityMie );

						// Copy values for our CPU texture equivalent
						m_DensityTextureCPU[_X,_Y] = _Color;

					}, 1 ) )
			m_DensityTexture = ToDispose( new Texture2D<PF_RGBA16F>( m_Device, "Sky Density Texture", DensityImage ) );
		}

		/// <summary>
		/// Computes the color of the sky given the provided view position & direction and using the current density parameters
		/// </summary>
		/// <param name="_ViewPosition">The viewing position (in WORLD space)</param>
		/// <param name="_ViewDirection">The viewing direction (in WORLD space)</param>
		/// <param name="_BackgroundColor">The background color that will be attenuated by the atmosphere
		/// (use 0 for empty space and 1 for occluded space, then use as a multiplier of the  existing background)</param>
		/// <param name="_Terminator">the terminator value (1 is fully lit, 0 is in Earth's shadow)</param>
		/// <returns>The color of the sky</returns>
		public Vector3	ComputeSkyColor( Vector3 _ViewPosition, Vector3 _ViewDirection, ref Vector3 _BackgroundColor, out float _Terminator )
		{
			// Compute view ray intersection with the upper atmosphere
			double	CameraHeight = WORLD_UNIT_TO_KILOMETER * _ViewPosition.Y;	// Relative height from sea level
			double	D = CameraHeight + EARTH_RADIUS;
			double	b = D * _ViewDirection.Y;
			double	c = D*D-ATMOSPHERE_RADIUS*ATMOSPHERE_RADIUS;
			double	Delta = Math.Sqrt( b*b-c );
			double	HitDistance = Delta - b;		// Distance at which we hit the upper atmosphere (in kilometers)
			HitDistance /= WORLD_UNIT_TO_KILOMETER;	// Back in WORLD units

			// Return color of the sky at that position
			return ComputeSkyColor( _ViewPosition, _ViewDirection, (float) HitDistance, ref _BackgroundColor, out _Terminator );
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
		public Vector3	ComputeSkyColor( Vector3 _ViewPosition, Vector3 _ViewDirection, float _ViewDistance, ref Vector3 _BackgroundColor, out float _Terminator )
		{
			// Compute density & extinction parameters
			float	fDensityRayleigh = 1e-5f * m_DensityRayleigh;
			float	fDensityMie = 1e-4f * m_DensityMie;
			Vector3	ExtinctionRayleighCoeff = 4.0f * (float) Math.PI * fDensityRayleigh * INV_WAVELENGTHS_POW4;
			float	ExtinctionMieCoeff = 4.0f * (float) Math.PI * fDensityMie;

			// Compute camera height & hit distance in kilometers
			double	Height = EARTH_RADIUS + WORLD_UNIT_TO_KILOMETER * _ViewPosition.Y;
			double	HitDistance = WORLD_UNIT_TO_KILOMETER * _ViewDistance;

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
				double	c = Vector2.Dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
				double	Delta = b*b - a*c;
				if ( Delta >= 0.0f )
					_Terminator = 1.0f - (float) Math.Max( 0.0, Math.Min( 1.0, (-b+Math.Sqrt(Delta)) / (a * HitDistance) ) );
			}

			// Ray-march the view ray
			Vector3	AccumulatedLightRayleigh = Vector3.Zero;
			Vector3	AccumulatedLightMie = Vector3.Zero;

			double	StepSize = HitDistance / STEPS_COUNT;
			Vector3	Step = (float) StepSize * _ViewDirection;
			CurrentPosition += (STEPS_COUNT-0.5f) * Step;	// Start from end point

			Vector3	ExtinctionRayleigh = Vector3.Zero;
			float	ExtinctionMie = 0.0f;
			for ( int StepIndex=0; StepIndex < STEPS_COUNT; StepIndex++ )
			{
				// =============================================
				// Sample extinction at current altitude and view direction
				Vector4	OpticalDepth = ComputeOpticalDepth( CurrentPosition, _ViewDirection );

				// Retrieve densities
				float	Rho_air = OpticalDepth.X;
				float	Rho_aerosols = OpticalDepth.Y;

				// ...and extinctions
				ExtinctionRayleigh.X = (float) Math.Exp( -ExtinctionRayleighCoeff.X * Rho_air * StepSize );
				ExtinctionRayleigh.Y = (float) Math.Exp( -ExtinctionRayleighCoeff.Y * Rho_air * StepSize );
				ExtinctionRayleigh.Z = (float) Math.Exp( -ExtinctionRayleighCoeff.Z * Rho_air * StepSize );
				ExtinctionMie = (float) Math.Exp( -ExtinctionMieCoeff * Rho_aerosols * StepSize );

				// =============================================
				// Perform extinction for previous step's energy
				AccumulatedLightRayleigh.X *= ExtinctionRayleigh.X;
				AccumulatedLightRayleigh.Y *= ExtinctionRayleigh.Y;
				AccumulatedLightRayleigh.Z *= ExtinctionRayleigh.Z;
				AccumulatedLightMie *= ExtinctionMie;
				_BackgroundColor.X *= ExtinctionRayleigh.X * ExtinctionMie;
				_BackgroundColor.Y *= ExtinctionRayleigh.Y * ExtinctionMie;
				_BackgroundColor.Z *= ExtinctionRayleigh.Z * ExtinctionMie;

				// =============================================
				// Retrieve sun light attenuated when passing through the atmosphere
				ExtinctionRayleigh.X = (float) Math.Exp( -ExtinctionRayleighCoeff.X * OpticalDepth.Z );
				ExtinctionRayleigh.Y = (float) Math.Exp( -ExtinctionRayleighCoeff.Y * OpticalDepth.Z );
				ExtinctionRayleigh.Z = (float) Math.Exp( -ExtinctionRayleighCoeff.Z * OpticalDepth.Z );
				ExtinctionMie = (float) Math.Exp( -ExtinctionMieCoeff * OpticalDepth.W );
				Vector3	Light = m_SunIntensity * (ExtinctionRayleigh * ExtinctionMie);

				// Compute in-scattering
				Vector3	InScatteringRayleigh = Light * fDensityRayleigh * PhaseRayleigh * Rho_air;
						InScatteringRayleigh.X *= INV_WAVELENGTHS_POW4.X * ExtinctionRayleigh.X;
						InScatteringRayleigh.Y *= INV_WAVELENGTHS_POW4.Y * ExtinctionRayleigh.Y;
						InScatteringRayleigh.Z *= INV_WAVELENGTHS_POW4.Z * ExtinctionRayleigh.Z;
				Vector3	InScatteringMie = Light * fDensityMie * PhaseMie * Rho_aerosols * ExtinctionMie;

				// Accumulate light
				AccumulatedLightRayleigh += InScatteringRayleigh;
				AccumulatedLightMie += InScatteringMie;

				// March
				CurrentPosition -= Step;
			}

			return m_FinalFactor * _Terminator * (AccumulatedLightRayleigh + AccumulatedLightMie) * (float) HitDistance / STEPS_COUNT;
		}

		/// <summary>
		/// Computes the color of the Sun given the provided direction and using the current density parameters
		/// </summary>
		/// <param name="_ViewPosition">The viewing position (in WORLD space)</param>
		/// <param name="_SunDirection">The NORMALIZED Sun direction (in WORLD space, and pointing TOWARD the Sun)</param>
		/// <returns>The color of the Sun after passing through the atmosphere</returns>
		public Vector3	ComputeSunColor( Vector3 _ViewPosition, Vector3 _SunDirection )
		{
			Vector3	OriginalSunColor = m_SunIntensity * Vector3.One;	// We also compute the sky color but don't use it...
			float	Terminator;	// Same for the terminator
			ComputeSkyColor( _ViewPosition, _SunDirection, ref OriginalSunColor, out Terminator );

			return OriginalSunColor;
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
			Altitude = Math.Max( 0.0f, (float) ((Altitude - EARTH_RADIUS) / (ATMOSPHERE_RADIUS - EARTH_RADIUS)) );

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
			int	SamplesCount = 30;
			WMath.Vector[]	Coefficients = new WMath.Vector[9];
			WMath.Vector	Direction = new WMath.Vector();
			Vector3			DXDirection, SkyColor, BackgroundColor = Vector3.Zero;
			float			Terminator;
			SphericalHarmonics.SHFunctions.EncodeIntoSH( Coefficients, SamplesCount, 2*SamplesCount, 1, 3,
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
					SkyColor = ComputeSkyColor( _ViewPosition, DXDirection, ref BackgroundColor, out Terminator );

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
	}
}
