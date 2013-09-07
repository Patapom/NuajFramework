#define DEEP_SHADOW_MAP_HI_RES	// Define this to use hi-def deep shadow map (make sure to also define this in the shader !)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;
using Nuaj.Cirrus;

namespace Demo
{
	/// <summary>
	/// Motherfucking Cloud Effect
	/// </example>
	public class RenderTechniqueVolumeClouds : RenderTechnique, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const int				DEEP_SHADOW_MAP_SIZE = 256;
		protected const int				LIGHTNINGS_COUNT = 1;

		#endregion

		#region NESTED TYPES

		protected class	ICloudSupport : ShaderInterfaceBase
		{
			[Semantic( "CLOUD_SIGMA_S" )]
			public float		SigmaScattering			{ set { SetScalar( "CLOUD_SIGMA_S", value ); } }
			[Semantic( "CLOUD_SIGMA_T" )]
			public float		SigmaExtinction			{ set { SetScalar( "CLOUD_SIGMA_T", value ); } }

			[Semantic( "CLOUD_DENSITY_SUM_FACTOR" )]
			public float		DensitySumFactor		{ set { SetScalar( "CLOUD_DENSITY_SUM_FACTOR", value ); } }
			[Semantic( "CLOUD_SHADOW_DISTANCE" )]
			public float		ShadowDistance			{ set { SetScalar( "CLOUD_SHADOW_DISTANCE", value ); } }
			[Semantic( "CLOUD_PLANE_HEIGHT_TOP" )]
			public float		CloudPlaneHeightTop		{ set { SetScalar( "CLOUD_PLANE_HEIGHT_TOP", value ); } }
			[Semantic( "CLOUD_PLANE_HEIGHT_BOTTOM" )]
			public float		CloudPlaneHeightBottom	{ set { SetScalar( "CLOUD_PLANE_HEIGHT_BOTTOM", value ); } }
			[Semantic( "CLOUD_LIGHTNING_INTENSITY" )]
			public float		LightningIntensity		{ set { SetScalar( "CLOUD_LIGHTNING_INTENSITY", value ); } }

			[Semantic( "CLOUD_SHADOW_RECTANGLE" )]
			public Vector4		ShadowRectangle			{ set { SetVector( "CLOUD_SHADOW_RECTANGLE", value ); } }
			[Semantic( "CLOUD_LIGHTNING_POSITION" )]
			public Vector3		LightningPosition		{ set { SetVector( "CLOUD_LIGHTNING_POSITION", value ); } }

			[Semantic( "CLOUD_DSM0" )]
			public ITexture2D	DeepShadowMap0			{ set { SetResource( "CLOUD_DSM0", value ); } }
			[Semantic( "CLOUD_DSM1" )]
			public ITexture2D	DeepShadowMap1			{ set { SetResource( "CLOUD_DSM1", value ); } }
		}

		protected class Lightning : IDisposable
		{
			#region FIELDS

			protected Nuaj.Device				m_Device = null;
			protected Primitive<VS_P3T2,int>	m_Primitive = null;

			// Generation parameters
			protected Random	m_LightningRNG = new Random( 1 );

			protected float		m_StepAngleMax = 1.0f;
			protected float		m_SplitAngleMin = 1.0f;
			protected float		m_SplitAngleMax = 1.0f;

			protected float		m_EnergyDecreaseMin = 0.0f;
			protected float		m_EnergyDecreaseMax = 0.0f;
			protected float		m_SplitEnergyBiasMin = 0.0f;
			protected float		m_SplitEnergyBiasMax = 0.0f;

			protected float		m_MaxDistance = 0.0f;
			protected float		m_MinY = 0.0f;

			#endregion

			#region METHODS

			public Lightning( Nuaj.Device _Device, int _RandomSeed )
			{
				m_Device = _Device;
				m_LightningRNG = new Random( _RandomSeed );
			}

			/// <summary>
			/// Builds a new lightning primitive
			/// </summary>
			/// <param name="_StepAngleMax">Maximum bend angle per step</param>
			/// <param name="_SplitAngleMin">Minimum bend angle per split</param>
			/// <param name="_SplitAngleMax">Maximum bend angle per split</param>
			/// <param name="_SplitThreshold">Energy threshold above which we split a new branch</param>
			/// <param name="_EnergyDecreaseMin">Minimum energy loss per step</param>
			/// <param name="_EnergyDecreaseMax">Maximum energy loss per step</param>
			/// <param name="_SplitEnergyBiasMin">Minimum energy bias per split (0=no energy for split branch, 1=all energy for split branch)</param>
			/// <param name="_SplitEnergyBiasMax">Maximum energy bias per split</param>
			public void		Build( float _StepAngleMax, float _SplitAngleMin, float _SplitAngleMax, float _SplitThreshold, float _EnergyDecreaseMin, float _EnergyDecreaseMax, float _SplitEnergyBiasMin, float _SplitEnergyBiasMax )
			{
				m_StepAngleMax = _StepAngleMax;
				m_SplitAngleMin = _SplitAngleMin;
				m_SplitAngleMax = _SplitAngleMax;
				m_EnergyDecreaseMin = _EnergyDecreaseMin;
				m_EnergyDecreaseMax = _EnergyDecreaseMax;
				m_SplitEnergyBiasMin = _SplitEnergyBiasMin;
				m_SplitEnergyBiasMax = _SplitEnergyBiasMax;

				m_MaxDistance = 0.0f;
				m_MinY = 0.0f;

				// Build vertices and indices
				List<VS_P3T2>	Vertices = new List<VS_P3T2>();
				List<int>		Indices = new List<int>();
				RecurseBranch(
					Vector3.Zero,						// Start from origin
					-Vector3.UnitY,						// Go straight down
					-1, -1,								// No previous branch
					0.0f,								// Initial distance from origin
					1.0f,								// Initial energy
					_SplitThreshold,
					Vertices,
					Indices );

				// Renormalize lightning shape and UVs
				float	SizeNormalizer = 1.0f / Math.Abs( m_MinY );
				float	DistanceNormalizer = 1.0f / m_MaxDistance;
				for ( int VertexIndex=0; VertexIndex < Vertices.Count; VertexIndex++ )
				{
					VS_P3T2	V = Vertices[VertexIndex];

					V.Position *= SizeNormalizer;
					V.UV.Y *= DistanceNormalizer;
					Vertices[VertexIndex] = V;
				}

				// Build the primitive
				if ( m_Primitive != null )
					m_Primitive.Dispose();

				m_Primitive = new Primitive<VS_P3T2,int>( m_Device, "Lightning", PrimitiveTopology.LineListWithAdjacency, Vertices.ToArray(), Indices.ToArray() );
			}

			/// <summary>
			/// Render the lightning
			/// </summary>
			public void		Render()
			{
				m_Primitive.RenderOverride();
			}

			/// <summary>
			/// Creates a recursive lightning branch
			/// </summary>
			/// <param name="_Position">The initial position</param>
			/// <param name="_Direction">The initial direction</param>
			/// <param name="_ParentPreviousVertexIndex">The index of the previous vertex from which we branched off</param>
			/// <param name="_ParentVertexIndex">The index of the vertex from which we branched off</param>
			/// <param name="_Distance">The initial distance from origin</param>
			/// <param name="_Energy">The initial energy</param>
			/// <param name="_SplitThreshold">Energy threshold above which we split a new branch</param>
			/// <param name="_Vertices">The list of vertices</param>
			/// <param name="_Indices">The list of indices</param>
			protected void	RecurseBranch(
				Vector3 _Position,
				Vector3 _Direction,
				int _ParentPreviousVertexIndex,
				int _ParentVertexIndex,
				float _Distance,
				float _Energy,
				float _SplitThreshold,
				List<VS_P3T2> _Vertices,
				List<int> _Indices )
			{
				List<int>	BranchIndices = new List<int>();

				float	SplittingEnergy = 0.0f;
				while ( _Energy > 0.0f )
				{
					// Create a single vertex
					VS_P3T2	NewVertex = new VS_P3T2()
					{
						Position = _Position,
						UV = new Vector2( _Energy, _Distance )
					};
					BranchIndices.Add( _Vertices.Count );
					_Vertices.Add( NewVertex );

					m_MaxDistance = Math.Max( m_MaxDistance, _Distance );
					m_MinY = Math.Min( m_MinY, _Position.Y );

					// Rotate a little
					float	BendAngle = Rand( 0.0f, m_StepAngleMax );
					_Direction = RotateAxis( _Direction, BendAngle );

					// March one step
					_Position += _Direction;
					_Distance += 1.0f;

					// Decrease energy
					_Energy -= Rand( m_EnergyDecreaseMin, m_EnergyDecreaseMax );

					// Accumulate splitting energy
					SplittingEnergy += _Energy;
					if ( SplittingEnergy > _SplitThreshold )
					{	// Split !
						float	SplitBias = Rand( m_SplitEnergyBiasMin, m_SplitEnergyBiasMax );
						float	SplitAngle = Rand( m_SplitAngleMin, m_SplitAngleMax );

						// Distribute rotation in this branch and the split one according to bias
						Vector3	SplitDirection = RotateAxis( _Direction, SplitAngle * (1.0f - SplitBias) );
						_Direction = RotateAxis( _Direction, SplitAngle * SplitBias );

						// Split energy
						float	NewBranchEnergy = SplitBias * _Energy;
						_Energy -= NewBranchEnergy;

						// Start again
						RecurseBranch( _Position, SplitDirection,
							BranchIndices.Count > 1 ? BranchIndices[BranchIndices.Count-2] : -1,
							BranchIndices.Count > 0 ? BranchIndices[BranchIndices.Count-1] : -1,
							_Distance,
							NewBranchEnergy,
							_SplitThreshold * SplitBias,
							_Vertices,
							_Indices );

						SplittingEnergy -= _SplitThreshold;
					}
				}

				// Build final branch indices
				if ( BranchIndices.Count < 2 )
					return;

				int	PrevIndex = _ParentPreviousVertexIndex;
				int	CurrentIndex = _ParentVertexIndex;
				int	NextIndex = BranchIndices[0];
				for ( int IndexIndex=1; IndexIndex < BranchIndices.Count; IndexIndex++ )
				{
					int	NextNextIndex = BranchIndices[IndexIndex];

					// Build segment with adjacencies
					_Indices.AddRange( new int[] {
						PrevIndex,
						CurrentIndex,
						NextIndex,
						NextNextIndex
					} );

					// Scroll indices
					PrevIndex = CurrentIndex;
					CurrentIndex = NextIndex;
					NextIndex = NextNextIndex;
				}
			}

			protected float		Rand( float _Min, float _Max )
			{
				return _Min + (float) m_LightningRNG.NextDouble() * (_Max - _Min);
			}

			/// <summary>
			/// Randomly rotates the axis by the specified angle
			/// </summary>
			/// <param name="_CurrentDirection"></param>
			/// <param name="_Angle"></param>
			/// <returns></returns>
			protected Vector3	RotateAxis( Vector3 _Axis, float _Angle )
			{
				Vector3	X = Vector3.Cross( _Axis, Math.Abs( Vector3.Dot( _Axis, Vector3.UnitX ) ) < Math.Abs( Vector3.Dot( _Axis, Vector3.UnitZ ) ) ? Vector3.UnitX : Vector3.UnitZ );
				X.Normalize();
				Vector3	Z = Vector3.Cross( X, _Axis );

				float	Phi = 2.0f * (float) (Math.PI * m_LightningRNG.NextDouble());
				Vector3	Ortho = (float) Math.Cos( Phi ) * X + (float) Math.Sin( Phi ) * Z;

				Vector3	Result = (float) Math.Cos( _Angle ) * _Axis + (float) Math.Sin( _Angle ) * Ortho;
				return Result;
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_Primitive.Dispose();
			}

			#endregion

			#endregion
		}

		#endregion

		#region FIELDS

		protected RendererSetupBasic		m_Renderer = null;

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_Pt4>			m_MaterialCloud = null;
		protected Material<VS_P3T2>			m_MaterialLightning = null;

		//////////////////////////////////////////////////////////////////////////
		// Sky support
		protected Nuaj.Cirrus.Atmosphere.SkySupport	m_SkySupport = null;

		//////////////////////////////////////////////////////////////////////////
		// Primitives
		protected Nuaj.Helpers.ScreenQuad	m_Quad = null;				// Screen quad for post-processing
		protected Lightning[]				m_Lightnings = new Lightning[LIGHTNINGS_COUNT];

		protected BoundingBox				m_TerrainAABB = new BoundingBox();

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected IRenderTarget[]			m_RenderTargets = null;

		protected RenderTarget<PF_RGBA16F>	m_VolumeRenderInScattering = null;
		protected RenderTarget<PF_RGBA16F>	m_VolumeRenderExtinction = null;

		protected RenderTarget<PF_RGBA16F>[]	m_DeepShadowMaps = new RenderTarget<PF_RGBA16F>[2];

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected DirectionalLight			m_Light = null;

		protected float						m_BilateralOffset = 0.9f;
		protected float						m_BilateralThreshold = 10.0f;

		protected float						m_DensityCloud = 0.5f;
		protected float						m_DensitySumFactor = 0.001f;
		protected float						m_DirectionalFactor = 1.0f;
		protected float						m_IsotropicFactor = 1.0f;
		protected float						m_ScatteringRatio = 0.8f;
		protected float						m_SigmaExtinction = 10.0f;

		protected float						m_PhaseWeightBackward = 0.25f;
		protected float						m_PhaseWeightForward = 0.3f;
		protected float						m_PhaseWeightSide = 0.8f;
		protected float						m_PhaseWeightSide2 = 0.2f;
		protected float						m_PhaseWeightStrongForward = 0.08f;
		protected float						m_ScatteringAnisotropyBackward = -0.4f;
		protected float						m_ScatteringAnisotropyForward = 0.8f;
		protected float						m_ScatteringAnisotropySide = -0.2f;
		protected float						m_ScatteringAnisotropyStrongForward = 0.95f;

		protected float						m_CloudPlaneHeightBottom = 60.0f;
		protected float						m_CloudPlaneHeightTop = 120.0f;

		protected float						m_CloudCoverage = 0.1f;
		protected float						m_NoiseSize = 0.1f;
		protected float						m_NoiseFrequencyFactor = 4.0f;
		protected float						m_CloudSpeed = 0.005f;
		protected float						m_CloudEvolutionSpeed = 0.03f;

		protected float						m_FarClipAir = 800.0f;
		protected float						m_FarClipClouds = 800.0f;

		protected float						m_Time = 0.0f;

		// Lightning
		protected Vector3					m_LightningPosition = new Vector3( 0.0f, 110.0f, 0.0f );
		protected float						m_LightningIntensity = 0.0f;//10000.0f;

		protected float						m_LightningEnergyDecreaseMin = 0.005f;
		protected float						m_LightningEnergyDecreaseMax = 0.01f;
		protected float						m_LightningEnergyBiasMin = 0.2f;
		protected float						m_LightningEnergyBiasMax = 0.3f;
		protected float						m_LightningEnergySplitThreshold = 1.0f;
		protected float						m_LightningTurnAngleStep = 1.0f * (float) Math.PI / 180.0f;
		protected float						m_LightningTurnAngleSplitMin = 45.0f * (float) Math.PI / 180.0f;
		protected float						m_LightningTurnAngleSplitMax = 60.0f * (float) Math.PI / 180.0f;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the light driven by the clouds simulation
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public DirectionalLight				Light					{ get { return m_Light; } set { m_Light = value; UpdateLightValues(); } }

		[System.ComponentModel.Browsable( false )]
		public float						Time					{ get { return m_Time; } set { m_Time = value; } }

		[System.ComponentModel.Browsable( false )]
		public BoundingBox					TerrainAABB				{ get { return m_TerrainAABB; } set { m_TerrainAABB = value; } }

		// Sky support variables
		[System.ComponentModel.Category( "Sky" )]
		public float						SunPhi					{ get { return m_SkySupport.SunPhi; } set { m_SkySupport.SunPhi = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						SunTheta				{ get { return m_SkySupport.SunTheta; } set { m_SkySupport.SunTheta = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						SunIntensity			{ get { return m_SkySupport.SunIntensity; } set { m_SkySupport.SunIntensity = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						DensityRayleigh			{ get { return m_SkySupport.DensityRayleigh; } set { m_SkySupport.DensityRayleigh = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						DensityMie				{ get { return m_SkySupport.DensityMie; } set { m_SkySupport.DensityMie = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						ScatteringAnisotropy	{ get { return m_SkySupport.ScatteringAnisotropy; } set { m_SkySupport.ScatteringAnisotropy = value; UpdateLightValues(); } }
		[System.ComponentModel.Category( "Sky" )]
		public float						WorldUnit2Kilometer		{ get { return m_SkySupport.WorldUnit2Kilometer; } set { m_SkySupport.WorldUnit2Kilometer = value; UpdateLightValues(); } }

		[System.ComponentModel.Browsable( false )]
		public Vector3						SunDirection			{ get { return m_SkySupport.SunDirection; } }

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudPlaneHeightBottom	{ get { return m_CloudPlaneHeightBottom; } set { m_CloudPlaneHeightBottom = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudPlaneHeightTop		{ get { return m_CloudPlaneHeightTop; } set { m_CloudPlaneHeightTop = value; } }

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudSpeed				{ get { return m_CloudSpeed; } set { m_CloudSpeed = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudEvolutionSpeed		{ get { return m_CloudEvolutionSpeed; } set { m_CloudEvolutionSpeed = value; } }

		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						CloudCoverage			{ get { return m_CloudCoverage; } set { m_CloudCoverage = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						NoiseSize				{ get { return m_NoiseSize; } set { m_NoiseSize = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						NoiseFrequencyFactor	{ get { return m_NoiseFrequencyFactor; } set { m_NoiseFrequencyFactor = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						FarClipAir				{ get { return m_FarClipAir; } set { m_FarClipAir = value; } }
		[System.ComponentModel.Category( "Clouds Geometry" )]
		public float						FarClipClouds			{ get { return m_FarClipClouds; } set { m_FarClipClouds = value; } }


		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						DensityCloud			{ get { return m_DensityCloud; } set { m_DensityCloud = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						DensitySumFactor		{ get { return m_DensitySumFactor; } set { m_DensitySumFactor = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						SigmaExtinction			{ get { return m_SigmaExtinction; } set { m_SigmaExtinction = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						ScatteringRatio			{ get { return m_ScatteringRatio; } set { m_ScatteringRatio = value; } }

		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						DirectionalFactor		{ get { return m_DirectionalFactor; } set { m_DirectionalFactor = value; } }
		[System.ComponentModel.Category( "Clouds Physics" )]
		public float						IsotropicFactor			{ get { return m_IsotropicFactor; } set { m_IsotropicFactor = value; } }

		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropyForward		{ get { return m_ScatteringAnisotropyForward; } set { m_ScatteringAnisotropyForward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightForward		{ get { return m_PhaseWeightForward; } set { m_PhaseWeightForward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropyBackward	{ get { return m_ScatteringAnisotropyBackward; } set { m_ScatteringAnisotropyBackward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightBackward		{ get { return m_PhaseWeightBackward; } set { m_PhaseWeightBackward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropySide		{ get { return m_ScatteringAnisotropySide; } set { m_ScatteringAnisotropySide = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightSide			{ get { return m_PhaseWeightSide; } set { m_PhaseWeightSide = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightSide2		{ get { return m_PhaseWeightSide2; } set { m_PhaseWeightSide2 = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						ScatteringAnisotropyStrongForward	{ get { return m_ScatteringAnisotropyStrongForward; } set { m_ScatteringAnisotropyStrongForward = value; } }
		[System.ComponentModel.Category( "Clouds Phases" )]
		public float						PhaseWeightStrongForward	{ get { return m_PhaseWeightStrongForward; } set { m_PhaseWeightStrongForward = value; } }

		[System.ComponentModel.Category( "Combine" )]
		public float						BilateralOffset			{ get { return m_BilateralOffset; } set { m_BilateralOffset = value; } }
		[System.ComponentModel.Category( "Combine" )]
		public float						BilateralDepthThreshold	{ get { return m_BilateralThreshold; } set { m_BilateralThreshold = value; } }

		[System.ComponentModel.Category( "Lightning" )]
		public Vector3						LightningPosition		{ get { return m_LightningPosition; } set { m_LightningPosition = value; } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningIntensity		{ get { return m_LightningIntensity; } set { m_LightningIntensity = value; } }

		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningEnergyDecreaseMin		{ get { return m_LightningEnergyDecreaseMin; } set { m_LightningEnergyDecreaseMin = value; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningEnergyDecreaseMax		{ get { return m_LightningEnergyDecreaseMax; } set { m_LightningEnergyDecreaseMax = value; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningEnergyBiasMin			{ get { return m_LightningEnergyBiasMin; } set { m_LightningEnergyBiasMin = value; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningEnergyBiasMax			{ get { return m_LightningEnergyBiasMax; } set { m_LightningEnergyBiasMax = value; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningEnergySplitThreshold	{ get { return m_LightningEnergySplitThreshold; } set { m_LightningEnergySplitThreshold = value; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningTurnAngleStep			{ get { return m_LightningTurnAngleStep * 180.0f / (float) Math.PI; } set { m_LightningTurnAngleStep = value * (float) Math.PI / 180.0f; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningTurnAngleSplitMin		{ get { return m_LightningTurnAngleSplitMin * 180.0f / (float) Math.PI; } set { m_LightningTurnAngleSplitMin = value * (float) Math.PI / 180.0f; UpdateLightning(); } }
		[System.ComponentModel.Category( "Lightning" )]
		public float						LightningTurnAngleSplitMax		{ get { return m_LightningTurnAngleSplitMax * 180.0f / (float) Math.PI; } set { m_LightningTurnAngleSplitMax = value * (float) Math.PI / 180.0f; UpdateLightning(); } }

		#endregion

		#region METHODS

		public RenderTechniqueVolumeClouds( RendererSetupBasic _Renderer, IRenderTarget[] _RenderTargets, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;
			m_RenderTargets = _RenderTargets;

			// Create and configure the sky support
			// We must do that BEFORE compiling our material that uses the sky support
			m_SkySupport = ToDispose( new Nuaj.Cirrus.Atmosphere.SkySupport( m_Device, "Cloud Sky Support" ) );

			// Register us as a cloud support provider
			m_Device.DeclareShaderInterface( typeof(ICloudSupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(ICloudSupport), this );

			// Create our main materials
			m_MaterialCloud = ToDispose( new Material<VS_Pt4>( m_Device, "Cloud Display Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Clouds/CloudDisplay2.fx" ) ) );
			m_MaterialLightning = ToDispose( new Material<VS_P3T2>( m_Device, "Lightning Display Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Clouds/LightningDisplay.fx" ) ) );

			// Create downscaled render buffer
			int	TargetWidth = m_RenderTargets[0].Width / 4;
			int	TargetHeight = m_RenderTargets[0].Height / 4;
			m_VolumeRenderInScattering = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Volume Target InScattering", TargetWidth, TargetHeight, 1 ) );
			m_VolumeRenderExtinction = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Volume Target Extinction", TargetWidth, TargetHeight, 1 ) );

			// Create the deep shadow maps
			m_DeepShadowMaps[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Deep Shadow Map 0", DEEP_SHADOW_MAP_SIZE, DEEP_SHADOW_MAP_SIZE, 1 ) );
			m_DeepShadowMaps[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Deep Shadow Map 1", DEEP_SHADOW_MAP_SIZE, DEEP_SHADOW_MAP_SIZE, 1 ) );

			// Create the post-process quad
			m_Quad = ToDispose( new Nuaj.Helpers.ScreenQuad( m_Device, "PostProcess Quad" ) );

			// Create the lightning primitives
			BuildLightningPrimitives();
		}

		public override void	Render( int _FrameToken )
		{
			ComputeShadowProjection();

			using ( m_MaterialCloud.UseLock() )
			{
 				m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

				//////////////////////////////////////////////////////////////////////////
				// Render the deep shadow map
				m_Device.SetMultipleRenderTargets( m_DeepShadowMaps );
				m_Device.SetViewport( 0, 0, m_DeepShadowMaps[0].Width, m_DeepShadowMaps[0].Height, 0.0f, 1.0f );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderDeepShadowMap" );

				CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector.Set( m_DeepShadowMaps[0].InvSize2 );
// 				CurrentMaterial.GetVariableByName( "CloudPlaneHeightBottom" ).AsScalar.Set( m_CloudPlaneHeightBottom );
// 				CurrentMaterial.GetVariableByName( "CloudPlaneHeightTop" ).AsScalar.Set( m_CloudPlaneHeightTop );
				CurrentMaterial.GetVariableByName( "CloudTime" ).AsScalar.Set( m_Time );

				CurrentMaterial.GetVariableByName( "ShadowRectangle" ).AsVector.Set( m_ShadowRectangle );
				CurrentMaterial.GetVariableByName( "ShadowVector" ).AsVector.Set( m_ShadowVector );

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// Render the low resolution clouds as 2 float3 : Extinction & In-Scattering
				m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_VolumeRenderInScattering, m_VolumeRenderExtinction } );
				m_Device.SetViewport( 0, 0, m_VolumeRenderInScattering.Width, m_VolumeRenderInScattering.Height, 0.0f, 1.0f );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Display" );

				CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector.Set( m_VolumeRenderInScattering.InvSize2 );

				CurrentMaterial.GetVariableByName( "SunColor" ).AsVector.Set( m_SkySupport.SunColor );

				CurrentMaterial.GetVariableByName( "FarClipAir" ).AsScalar.Set( m_FarClipAir );
				CurrentMaterial.GetVariableByName( "FarClipClouds" ).AsScalar.Set( m_FarClipClouds );
 
				CurrentMaterial.GetVariableByName( "NoiseOffset" ).AsScalar.Set( m_CloudCoverage - 1.0f );
				CurrentMaterial.GetVariableByName( "NoiseSize" ).AsScalar.Set( m_NoiseSize );
				CurrentMaterial.GetVariableByName( "NoiseFrequencyFactor" ).AsScalar.Set( m_NoiseFrequencyFactor );
				CurrentMaterial.GetVariableByName( "CloudSpeed" ).AsScalar.Set( m_CloudSpeed );
				CurrentMaterial.GetVariableByName( "CloudEvolutionSpeed" ).AsScalar.Set( m_CloudEvolutionSpeed );

				CurrentMaterial.GetVariableByName( "DensityCloud" ).AsScalar.Set( m_DensityCloud );
// 				CurrentMaterial.GetVariableByName( "DensitySumFactor" ).AsScalar.Set( m_DensitySumFactor );
// 				CurrentMaterial.GetVariableByName( "Sigma_t" ).AsScalar.Set( m_SigmaExtinction );
// 				CurrentMaterial.GetVariableByName( "Sigma_s" ).AsScalar.Set( m_ScatteringRatio * m_SigmaExtinction );

				CurrentMaterial.GetVariableByName( "DirectionalFactor" ).AsScalar.Set( m_DirectionalFactor );
				CurrentMaterial.GetVariableByName( "IsotropicFactor" ).AsScalar.Set( m_IsotropicFactor );


				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyStrongForward" ).AsScalar.Set( m_ScatteringAnisotropyStrongForward );
				CurrentMaterial.GetVariableByName( "PhaseWeightStrongForward" ).AsScalar.Set( m_PhaseWeightStrongForward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyForward" ).AsScalar.Set( m_ScatteringAnisotropyForward );
				CurrentMaterial.GetVariableByName( "PhaseWeightForward" ).AsScalar.Set( m_PhaseWeightForward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyBackward" ).AsScalar.Set( m_ScatteringAnisotropyBackward );
				CurrentMaterial.GetVariableByName( "PhaseWeightBackward" ).AsScalar.Set( m_PhaseWeightBackward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropySide" ).AsScalar.Set( m_ScatteringAnisotropySide );
				CurrentMaterial.GetVariableByName( "PhaseWeightSide" ).AsScalar.Set( m_PhaseWeightSide );
				CurrentMaterial.GetVariableByName( "PhaseWeightSide2" ).AsScalar.Set( m_PhaseWeightSide2 );

				// Deep shadow map (we need to re-update new values)
 				CurrentMaterial.GetVariableByName( "ShadowRectangle" ).AsVector.Set( m_ShadowInvRectangle );
// 				CurrentMaterial.GetVariableByName( "ShadowDistance" ).AsScalar.Set( m_ShadowDistance );
				CurrentMaterial.GetVariableByName( "DeepShadowMap0" ).AsResource.SetResource( m_DeepShadowMaps[0] );
				CurrentMaterial.GetVariableByName( "DeepShadowMap1" ).AsResource.SetResource( m_DeepShadowMaps[1] );

				// Lightning
				CurrentMaterial.GetVariableByName( "LightningPosition" ).AsVector.Set( m_LightningPosition );
				CurrentMaterial.GetVariableByName( "LightningIntensity" ).AsScalar.Set( m_LightningIntensity );

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// Combine result
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Combine" );
				m_Device.SetRenderTarget( m_RenderTargets[1], null );
				m_Device.SetViewport( 0, 0, m_RenderTargets[1].Width, m_RenderTargets[1].Height, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector.Set( m_RenderTargets[1].InvSize2 );
				CurrentMaterial.GetVariableByName( "SourceBuffer" ).AsResource.SetResource( m_RenderTargets[0] );
				CurrentMaterial.GetVariableByName( "VolumeBufferInvSize" ).AsVector.Set( new Vector3( m_BilateralOffset / m_VolumeRenderInScattering.Width, m_BilateralOffset / m_VolumeRenderInScattering.Height, 0.0f ) );
				CurrentMaterial.GetVariableByName( "VolumeTextureInScattering" ).AsResource.SetResource( m_VolumeRenderInScattering );
				CurrentMaterial.GetVariableByName( "VolumeTextureExtinction" ).AsResource.SetResource( m_VolumeRenderExtinction );
				CurrentMaterial.GetVariableByName( "BilateralThreshold" ).AsScalar.Set( m_BilateralThreshold );

				CurrentMaterial.ApplyPass(0);
				m_Quad.Render();
			}
		}

		public void		DisplayLightning()
		{
			using ( m_MaterialLightning.UseLock() )
			{
 				m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

				CurrentMaterial.GetVariableByName( "Scale" ).AsScalar.Set( m_CloudPlaneHeightBottom );

				CurrentMaterial.ApplyPass( 0 );
				m_Lightnings[0].Render();
			}
		}

		protected Matrix	m_Light2World;
		protected Vector4	m_ShadowRectangle;
		protected Vector4	m_ShadowInvRectangle;
		protected Vector3	m_ShadowVector;
		protected Vector2	m_ShadowBufferInvSize;
		protected float		m_ShadowDistance;

		protected void	ComputeShadowProjection()
		{
			// Compute camera frustum's AABB
			BoundingBox	CameraAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );

			Matrix	Camera2World = m_Renderer.Camera.Camera2World;
			float	CameraFOV = m_Renderer.Camera.PerspectiveFOV;
			float	CameraAspectRatio = m_Renderer.Camera.AspectRatio;
			float	CameraNear = m_Renderer.Camera.Near;
			float	CameraFar = m_Renderer.Camera.Far;

			float	TanHalfFOV = (float) Math.Tan( 0.5 * CameraFOV );
			for ( int Y=0; Y < 2; Y++ )
				for ( int X=0; X < 2; X++ )
				{
					Vector3	View = new Vector3( CameraAspectRatio * TanHalfFOV * (2.0f * X - 1.0f), TanHalfFOV * (2.0f * Y - 1.0f), 1.0f );
					View.Normalize();

					Vector3	Corner = Vector3.TransformCoordinate( CameraNear * View, Camera2World );
					CameraAABB.Minimum = Vector3.Min( CameraAABB.Minimum, Corner );
					CameraAABB.Maximum = Vector3.Max( CameraAABB.Maximum, Corner );

					Corner = Vector3.TransformCoordinate( CameraFar * View, Camera2World );
					CameraAABB.Minimum = Vector3.Min( CameraAABB.Minimum, Corner );
					CameraAABB.Maximum = Vector3.Max( CameraAABB.Maximum, Corner );
				}

			// Clamp object's AABB (i.e. terrain) into camera's AABB
			BoundingBox	WorldAABB = new BoundingBox(
				Vector3.Max( CameraAABB.Minimum, m_TerrainAABB.Minimum ),
				Vector3.Min( CameraAABB.Maximum, m_TerrainAABB.Maximum ) );


			// Compute LIGHT2WORLD matrix
			Vector3	LightDirection = m_SkySupport.SunDirection;
			Vector3	Right = LightDirection.Y < 0.99f ?
				Vector3.Cross( Vector3.UnitY, LightDirection ) :
				Vector3.Cross( Vector3.UnitX, LightDirection );
			Right.Normalize();
			Vector3	Up = Vector3.Cross( LightDirection, Right );

			m_Light2World = Matrix.Identity;
			m_Light2World.Row1 = new Vector4( Right, 0.0f );
			m_Light2World.Row2 = new Vector4( Up, 0.0f );
			m_Light2World.Row3 = new Vector4( -LightDirection, 0.0f );
			m_Light2World.Row4 = Vector4.UnitW;

			// Project world AABB to top cloud plane
			// The idea is to determine which square area on the top cloud plane will encompass
			//	the objects visible to the camera and to render only that particular area into
			//	the shadow map :
			//
			//   Min                      Max
			//  ==*========================*================= Top Cloud Plane
			//     \                        \
			//      \                        \
			//       \                        \
			//        \                        \
			//         \                        \
			//          \                        \
			//           \     ____________________
			//            \    |                  |
			//             \   |                  |
			//              \  |     A A B B      |
			//               \ |                  |
			//                \|                  |
			//                 --------------------
			//
			float	MinX = +float.MaxValue, MinZ = +float.MaxValue;
			float	MaxX = -float.MaxValue, MaxZ = -float.MaxValue;
			foreach ( Vector3 Corner in WorldAABB.GetCorners() )
			{
				float	HitDistance = (m_CloudPlaneHeightTop - Corner.Y) / LightDirection.Y;
				Vector3	ProjectedCorner = Corner + HitDistance * LightDirection;

				MinX = Math.Min( MinX, ProjectedCorner.X );
				MaxX = Math.Max( MaxX, ProjectedCorner.X );
				MinZ = Math.Min( MinZ, ProjectedCorner.Z );
				MaxZ = Math.Max( MaxZ, ProjectedCorner.Z );
			}

			m_ShadowRectangle = new Vector4( MinX, MinZ, MaxX-MinX, MaxZ-MinZ );
			m_ShadowInvRectangle = new Vector4( -MinX, -MinZ, 1.0f / (MaxX-MinX), 1.0f / (MaxZ-MinZ) );

			// Compute the shadow vector that will start from top cloud plane and arrive at bottom cloud plane in 8 steps
			// (because the deep shadow map encodes 8 density samples)
			float	CloudLayerThickness = m_CloudPlaneHeightTop - m_CloudPlaneHeightBottom;
			m_ShadowDistance = CloudLayerThickness / LightDirection.Y;
//			m_ShadowDistance = Math.Min( m_ShadowDistance, 100.0f );	// No more than that amount
			m_ShadowDistance = Math.Min( m_ShadowDistance, 3.0f * CloudLayerThickness );	// No more than that amount

#if DEEP_SHADOW_MAP_HI_RES
			m_ShadowVector = -LightDirection * 0.1111f * m_ShadowDistance;	// Divide into 9 equal steps as we start at 0.5 a step and end at 8.5 steps so we're always within the cloud layer
#else
			m_ShadowVector = -LightDirection * 0.2f * m_ShadowDistance;		// Divide into 5 equal steps as we start at 0.5 a step and end at 4.5 steps so we're always within the cloud layer
#endif
		}

		protected void	BuildLightningPrimitives()
		{
			for ( int LightningIndex=0; LightningIndex < LIGHTNINGS_COUNT; LightningIndex++ )
				m_Lightnings[LightningIndex] = ToDispose( new Lightning( m_Device, 1 ) );

			UpdateLightning();
		}

		protected void	UpdateLightning()
		{
			for ( int LightningIndex=0; LightningIndex < LIGHTNINGS_COUNT; LightningIndex++ )
				m_Lightnings[LightningIndex].Build(
					m_LightningTurnAngleStep,
					m_LightningTurnAngleSplitMin, m_LightningTurnAngleSplitMax,
					m_LightningEnergySplitThreshold,
					m_LightningEnergyDecreaseMin, m_LightningEnergyDecreaseMax,
					m_LightningEnergyBiasMin, m_LightningEnergyBiasMax
				);
		}

		protected void	UpdateLightValues()
		{
			m_Light.Direction = m_SkySupport.SunDirection;
			m_Light.Color = new Vector4( m_SkySupport.SunColor, 1.0f );
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			ICloudSupport	I = _Interface as ICloudSupport;

			I.SigmaExtinction = m_SigmaExtinction;
			I.SigmaScattering = m_ScatteringRatio * m_SigmaExtinction;
			I.DensitySumFactor = m_DensitySumFactor;
			I.ShadowDistance = m_ShadowDistance;
			I.CloudPlaneHeightTop = m_CloudPlaneHeightTop;
			I.CloudPlaneHeightBottom = m_CloudPlaneHeightBottom;
			I.LightningIntensity = m_LightningIntensity;
			I.ShadowRectangle = m_ShadowInvRectangle;
			I.LightningPosition = m_LightningPosition;
			I.DeepShadowMap0 = m_DeepShadowMaps[0];
			I.DeepShadowMap1 = m_DeepShadowMaps[1];
		}

		#endregion

		#endregion
	}
}
