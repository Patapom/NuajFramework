//#define RENDER_PARTICLES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;
using Nuaj.Cirrus;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Nebula Effect
	/// </example>
	public class RenderTechniqueNebula : RenderTechniqueBase, IComparer<VS_T4>
	{
		#region CONSTANTS

		protected const int		NEBULA_SIZE = 129;
		protected const int		NEBULA_HEIGHT = 65;
		protected const int		DIFFUSION_SLICES_COUNT = 16;		// Amount of slices to compute per frame
		protected const float	NEBULA_TO_WORLD_RATIO = 0.1f;

		protected const int		PARTICLES_COUNT = 10000;

		#endregion

		#region NESTED TYPES

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_Pt4>			m_MaterialCompute = null;
#if RENDER_PARTICLES
		protected Material<VS_T4>			m_MaterialDisplay = null;
#else
		protected Material<VS_Pt4>			m_MaterialDisplay = null;
#endif

		//////////////////////////////////////////////////////////////////////////
		// Primitives
		protected VertexBuffer<VS_T4>[]		m_Particles = new VertexBuffer<VS_T4>[8];

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected RenderTarget3D<PF_RGBA16F>[]	m_DiffusionBuffers = new RenderTarget3D<PF_RGBA16F>[2];
		protected Texture2D<PF_RGBA8>		m_CloudSprites = null;
		protected Texture2D<PF_RGBA8>		m_CloudSpriteNormals = null;

		protected RenderTarget<PF_RGBA16F>	m_VolumeRender = null;

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected float						m_CloudTime = 0.0f;

		protected float						m_LightFlux = 4.0f;
		protected Vector3					m_LightPosition = Vector3.Zero;
		protected float						m_LightRadius = 4.0f;

		protected float						m_NoiseScale = 0.5f;
		protected float						m_NoiseOffset = -0.4f;
		protected float						m_NoiseSize = 0.4f;

		protected float						m_ScatteringAnisotropy = 0.7f;
		protected float						m_SigmaExtinction = 0.2f;
		protected float						m_SigmaScattering = 0.4f;
		protected float						m_SigmaAerosols = 0.03f;

#if RENDER_PARTICLES
// 		protected float						m_AlphaPower = 2.0f;
// 		protected float						m_AmbientFactor = 2.0f;
// 		protected float						m_DiffuseFactor = 1.0f;
// 		protected float						m_SpecularFactor = 0.5f;
//		protected float						m_SpriteSizeFactor = 5.0f;
#else
		protected float						m_DirectionalFactor = 1.0f;
		protected float						m_IsotropicFactor = 1.0f;
		protected float						m_AerosolsFactor = 1.0f;
#endif
		protected int						m_SliceIndex = -1;

		#endregion

		#region PROPERTIES

		public Vector3						LightPosition			{ get { return m_LightPosition; } set { m_LightPosition = value; } }
		public float						LightRadius				{ get { return m_LightRadius; } set { m_LightRadius = value; } }
		public float						LightFlux				{ get { return m_LightFlux; } set { m_LightFlux = value; } }

		public float						NoiseScale				{ get { return m_NoiseScale; } set { m_NoiseScale = value; } }
		public float						NoiseOffset				{ get { return m_NoiseOffset; } set { m_NoiseOffset = value; } }
		public float						NoiseSize				{ get { return m_NoiseSize; } set { m_NoiseSize = value; } }

		public float						SigmaExtinction			{ get { return m_SigmaExtinction; } set { m_SigmaExtinction = value; } }
		public float						SigmaScattering			{ get { return m_SigmaScattering; } set { m_SigmaScattering = value; } }
		public float						SigmaAerosols			{ get { return m_SigmaAerosols; } set { m_SigmaAerosols = value; } }
		public float						ScatteringAnisotropy	{ get { return m_ScatteringAnisotropy; } set { m_ScatteringAnisotropy = value; } }

#if RENDER_PARTICLES
		public float						AmbientFactor			{ get { return m_AmbientFactor; } set { m_AmbientFactor = value; } }
		public float						DiffuseFactor			{ get { return m_DiffuseFactor; } set { m_DiffuseFactor = value; } }
		public float						SpecularFactor			{ get { return m_SpecularFactor; } set { m_SpecularFactor = value; } }
		public float						AlphaPower				{ get { return m_AlphaPower; } set { m_AlphaPower = value; } }
		public float						SpriteSizeFactor		{ get { return m_SpriteSizeFactor; } set { m_SpriteSizeFactor = value; } }
#else
		public float						DirectionalFactor		{ get { return m_DirectionalFactor; } set { m_DirectionalFactor = value; } }
		public float						IsotropicFactor			{ get { return m_IsotropicFactor; } set { m_IsotropicFactor = value; } }
		public float						AerosolsFactor			{ get { return m_AerosolsFactor; } set { m_AerosolsFactor = value; } }
#endif

		#endregion

		#region METHODS

		public RenderTechniqueNebula( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_MaterialCompute = m_Renderer.LoadMaterial<VS_Pt4>( "Nebula Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/NebulaCompute.fx" ) );
#if RENDER_PARTICLES
			m_MaterialDisplay = m_Renderer.LoadMaterial<VS_T4>( "Nebula Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/NebulaDisplay.fx" ) );
#else
			m_MaterialDisplay = m_Renderer.LoadMaterial<VS_Pt4>( "Nebula Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/NebulaDisplay2.fx" ) );
#endif

			// Create diffusion buffers
 			Vector3	GalacticCenter = new Vector3( 0.5f * NEBULA_SIZE, 0.5f * NEBULA_SIZE, 0.5f * NEBULA_HEIGHT );
			float	HeightAttenuation = 0.025f;
			float	CenterHeight = 0.25f;

			using ( Image3D<PF_RGBA16F> I = new Image3D<PF_RGBA16F>( m_Device, "Nebula Image", NEBULA_SIZE, NEBULA_SIZE, NEBULA_HEIGHT,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{
					Vector3	Position = new Vector3( _X, _Y, _Z );
// 					float	Distance2GalacticCenter = (Position - GalacticCenter).Length();
//  					float	Height = 0.05f * (Position.Z - GalacticCenter.Z);
// 					float	HeightFactor = (float) Math.Exp( -Height * Height );
//					_Color.Z = 0.5f * (1.0f + (float) Math.Cos( 0.2f * Distance2GalacticCenter )) * HeightFactor;

					float	Distance2GalacticCenter = HeightAttenuation * ((Vector2) Position - (Vector2) GalacticCenter).Length();
 					float	MaxHeight = CenterHeight * (float) Math.Exp( -Distance2GalacticCenter * Distance2GalacticCenter );
 					float	Height = 0.5f * Math.Abs( Position.Z - GalacticCenter.Z ) / NEBULA_HEIGHT;
					float	RelativeHeight = -0.01f + Height / MaxHeight;

 					_Color.Z = 1.0f;//(float) Math.Exp( -RelativeHeight * RelativeHeight );
					_Color.Z = 1.0f - Math.Min( 1.0f, RelativeHeight );

				},
				1 ) )
				{
					m_DiffusionBuffers[0] = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "Diffusion Buffer 0", I ) );
					m_DiffusionBuffers[1] = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "Diffusion Buffer 1", I ) );
				}

#if RENDER_PARTICLES
			// Create particle points
			Random	RNG = new Random( 1 );
			List<VS_T4>	Particles = new List<VS_T4>( PARTICLES_COUNT );
			for ( int ParticleIndex=0; ParticleIndex < PARTICLES_COUNT; ParticleIndex++ )
			{
				int		SpriteIndex = RNG.Next( 16 );
				float	Size = 0.5f + 0.499f * (float) RNG.NextDouble();

				Vector2	PlanarPosition = new Vector2( ((float) RNG.NextDouble() - 0.5f) * NEBULA_SIZE, ((float) RNG.NextDouble() - 0.5f) * NEBULA_SIZE );
				float	Distance2GalacticCenter = HeightAttenuation * (PlanarPosition - new Vector2( GalacticCenter.X - NEBULA_SIZE/2, GalacticCenter.Y - NEBULA_SIZE/2 )).Length();
 				float	MaxHeight = CenterHeight * (float) Math.Exp( -Distance2GalacticCenter * Distance2GalacticCenter );
				float	Height = ((float) RNG.NextDouble() - 0.5f) * MaxHeight * NEBULA_HEIGHT;

				Particles.Add(
					new VS_T4()
					{
						UV = new Vector4(
// 							((float) RNG.NextDouble() - 0.5f) * NEBULA_SIZE,
// 							((float) RNG.NextDouble() - 0.5f) * NEBULA_SIZE,
// 							((float) RNG.NextDouble() - 0.5f) * NEBULA_HEIGHT,

							PlanarPosition.X,
							PlanarPosition.Y,
 							Height,
//							GalacticCenter.Z,

							SpriteIndex + Size	// Integer part is sprite index, remainder is normalized sprite size
					) }
				);
			}

			// Sort along 8 pre-computed directions
			for ( int DirectionIndex=0; DirectionIndex < 8; DirectionIndex++ )
			{
				float	ViewAngle = DirectionIndex * (float) Math.PI / 4;
				m_SortDirection = new Vector3( (float) Math.Cos( ViewAngle ), (float) Math.Sin( ViewAngle ), 0.0f );

				Particles.Sort( this );
				VS_T4[]	SortedParticles = Particles.ToArray();
				m_Particles[DirectionIndex] = ToDispose( new VertexBuffer<VS_T4>( m_Device, "Nebula Particles", SortedParticles ) );
			}
			// Create the clouds TPage
			m_CloudSprites = ToDispose( m_Renderer.LoadTPage<PF_RGBA8>( new System.IO.FileInfo( "Media/WaterColour/CloudTPage.png" ), "Cloud Sprites", 256, 256, 0 ) );
			m_CloudSpriteNormals = ToDispose( m_Renderer.LoadTPage<PF_RGBA8>( new System.IO.FileInfo( "Media/WaterColour/CloudTPage_NRM+DISP2.png" ), "Cloud Sprite Normals", 256, 256, 0 ) );
#else	
			m_VolumeRender = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Volume Target", m_Renderer.MaterialBuffer.Width / 4, m_Renderer.MaterialBuffer.Height / 4, 1 ) );
#endif
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			//////////////////////////////////////////////////////////////////////////
			// Compute light diffusion
			m_Device.AddProfileTask( this, "Nebula", "Compute" );

 			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialCompute.UseLock() )
			{
				m_Device.SetViewport( 0, 0, NEBULA_SIZE, NEBULA_SIZE, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "CellOffset" ).AsVector.Set( new Vector3( NEBULA_SIZE >> 1, NEBULA_SIZE >> 1, NEBULA_HEIGHT >> 1 ) );
				CurrentMaterial.GetVariableByName( "CloudTime" ).AsScalar.Set( m_CloudTime );

				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightRadius" ).AsScalar.Set( m_LightRadius );
				CurrentMaterial.GetVariableByName( "LightFlux" ).AsScalar.Set( m_LightFlux );

				CurrentMaterial.GetVariableByName( "NoiseScale" ).AsScalar.Set( m_NoiseScale );
				CurrentMaterial.GetVariableByName( "NoiseOffset" ).AsScalar.Set( m_NoiseOffset );
				CurrentMaterial.GetVariableByName( "NoiseSize" ).AsScalar.Set( m_NoiseSize );

				CurrentMaterial.GetVariableByName( "Sigma_s" ).AsScalar.Set( m_SigmaScattering );
				CurrentMaterial.GetVariableByName( "Sigma_t" ).AsScalar.Set( m_SigmaExtinction );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropy" ).AsScalar.Set( m_ScatteringAnisotropy );

				CurrentMaterial.GetVariableByName( "SliceInvSize" ).AsVector.Set( new Vector3( 1.0f / NEBULA_SIZE, 1.0f / NEBULA_SIZE, 1.0f / NEBULA_HEIGHT ) );

				VariableResource	vSourceDiffusionBuffer = CurrentMaterial.GetVariableByName( "SourceDiffusionTexture" ).AsResource;
				VariableScalar		vSliceIndex = CurrentMaterial.GetVariableByName( "SliceIndex" ).AsScalar;
 				EffectPass			Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				for ( int PassIndex=0; PassIndex < DIFFUSION_SLICES_COUNT; PassIndex++ )
				{
					m_SliceIndex = (m_SliceIndex+1) % NEBULA_HEIGHT;
					if ( m_SliceIndex == 0 )
					{
						// Swap buffers
						RenderTarget3D<PF_RGBA16F>	Temp = m_DiffusionBuffers[0];
						m_DiffusionBuffers[0] = m_DiffusionBuffers[1];
						m_DiffusionBuffers[1] = Temp;

						// Setup new source
						vSourceDiffusionBuffer.SetResource( m_DiffusionBuffers[0] );

						// Update cloud time
						m_CloudTime += 1.0f;
					}

					m_Device.SetRenderTarget( m_DiffusionBuffers[1] );

					vSliceIndex.Set( m_SliceIndex );
					Pass.Apply();
					m_Renderer.RenderPostProcessQuadInstanced( 1 );
				}
			}

#if RENDER_PARTICLES
			//////////////////////////////////////////////////////////////////////////
			// Display light diffusion using point sprites
			m_Device.AddProfileTask( this, "Nebula", "Display" );

 			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.NOWRITE_CLOSEST );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.BLEND );

			using ( m_MaterialDisplay.UseLock() )
			{
				m_Renderer.SetDefaultRenderTarget();
				m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.PointList );

				CurrentMaterial.GetVariableByName( "CellOffset" ).AsVector.Set( new Vector3( NEBULA_SIZE >> 1, NEBULA_SIZE >> 1, NEBULA_HEIGHT >> 1 ) );
				CurrentMaterial.GetVariableByName( "Nebula2WorldRatio" ).AsScalar.Set( NEBULA_TO_WORLD_RATIO );
				CurrentMaterial.GetVariableByName( "SpriteSizeFactor" ).AsScalar.Set( m_SpriteSizeFactor );

				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropy" ).AsScalar.Set( m_ScatteringAnisotropy );

				CurrentMaterial.GetVariableByName( "DiffuseFactor" ).AsScalar.Set( m_DiffuseFactor );
				CurrentMaterial.GetVariableByName( "SpecularFactor" ).AsScalar.Set( m_SpecularFactor );
				CurrentMaterial.GetVariableByName( "AmbientFactor" ).AsScalar.Set( m_AmbientFactor );
				CurrentMaterial.GetVariableByName( "AlphaPower" ).AsScalar.Set( m_AlphaPower );

				CurrentMaterial.GetVariableByName( "SliceInvSize" ).AsVector.Set( new Vector3( 1.0f / NEBULA_SIZE, 1.0f / NEBULA_SIZE, 1.0f / NEBULA_HEIGHT ) );
				CurrentMaterial.GetVariableByName( "SourceDiffusionTexture" ).AsResource.SetResource( m_DiffusionBuffers[0] );
				CurrentMaterial.GetVariableByName( "CloudTexture" ).AsResource.SetResource( m_CloudSprites );
				CurrentMaterial.GetVariableByName( "CloudNormalTexture" ).AsResource.SetResource( m_CloudSpriteNormals );

				CurrentMaterial.ApplyPass( 0 );

				// Retrieve the best view angle
				Vector4	View = -m_Renderer.Camera.Camera2World.Row3;
				float	ViewAngle = 0.5f + (float) (4.0 * Math.Atan2( View.Z, View.X ) / Math.PI);
				int		VBIndex = (int) Math.Floor( ViewAngle ) & 0x7;

				m_Particles[VBIndex].Use();
				m_Particles[VBIndex].Draw();
			}
#else
			//////////////////////////////////////////////////////////////////////////
			// Display light diffusion using point sprites
			m_Device.AddProfileTask( this, "Nebula", "Display" );

 			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialDisplay.UseLock() )
			{
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Display" );

 				m_Device.SetRenderTarget( m_VolumeRender );
				m_Device.SetViewport( 0, 0, m_VolumeRender.Width, m_VolumeRender.Height, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector.Set( new Vector2( 1.0f / m_VolumeRender.Width, 1.0f / m_VolumeRender.Height ) );

				CurrentMaterial.GetVariableByName( "CellOffset" ).AsVector.Set( new Vector3( NEBULA_SIZE >> 1, NEBULA_SIZE >> 1, NEBULA_HEIGHT >> 1 ) );
				CurrentMaterial.GetVariableByName( "Nebula2WorldRatio" ).AsScalar.Set( NEBULA_TO_WORLD_RATIO );
				CurrentMaterial.GetVariableByName( "SliceInvSize" ).AsVector.Set( new Vector3( 1.0f / NEBULA_SIZE, 1.0f / NEBULA_SIZE, 1.0f / NEBULA_HEIGHT ) );
				CurrentMaterial.GetVariableByName( "SourceDiffusionTexture" ).AsResource.SetResource( m_DiffusionBuffers[0] );

				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightRadius" ).AsScalar.Set( m_LightRadius );
				CurrentMaterial.GetVariableByName( "LightFlux" ).AsScalar.Set( m_LightFlux );

				CurrentMaterial.GetVariableByName( "Sigma_t" ).AsScalar.Set( m_SigmaExtinction );
				CurrentMaterial.GetVariableByName( "Sigma_s" ).AsScalar.Set( m_SigmaScattering );
				CurrentMaterial.GetVariableByName( "Sigma_Aerosols" ).AsScalar.Set( m_SigmaAerosols );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropy" ).AsScalar.Set( m_ScatteringAnisotropy );

				CurrentMaterial.GetVariableByName( "DirectionalFactor" ).AsScalar.Set( m_DirectionalFactor );
				CurrentMaterial.GetVariableByName( "IsotropicFactor" ).AsScalar.Set( m_IsotropicFactor );
				CurrentMaterial.GetVariableByName( "AerosolsFactor" ).AsScalar.Set( m_AerosolsFactor );

				CurrentMaterial.ApplyPass(0);
				m_Renderer.RenderPostProcessQuad();

				// Splat result
				m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.BLEND );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "Splat" );
				m_Renderer.SetDefaultRenderTarget();

				CurrentMaterial.GetVariableByName( "VolumeTexture" ).AsResource.SetResource( m_VolumeRender );

				CurrentMaterial.ApplyPass(0);
				m_Renderer.RenderPostProcessQuad();
			}
#endif
			m_Device.AddProfileTask( this, "Nebula", "<END>" );
		}

		#region IComparer<VS_T4> Members

		protected Vector3	m_SortDirection = Vector3.Zero;
		public int Compare( VS_T4 x, VS_T4 y )
		{
			Vector3	P0 = (Vector3) x.UV;
			Vector3	P1 = (Vector3) y.UV;

			float	Distance0 = Vector3.Dot( P0, m_SortDirection );
			float	Distance1 = Vector3.Dot( P1, m_SortDirection );

			return Distance0 < Distance1 ? -1 : (Distance0 > Distance1 ? +1 : 0);
		}

		#endregion

		#endregion
	}
}
