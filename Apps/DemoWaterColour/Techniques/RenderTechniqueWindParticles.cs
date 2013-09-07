using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
/*
* Render in off-screen buffer (smaller ?) and compose with backbuffer
* In general, shouldn't we do that for ALL particle effects : a separate, downscaled G-Buffer ?
* */
	/// <summary>
	/// Wind particles effect
	/// </example>
	public class RenderTechniqueWindParticles : RenderTechniqueBase
	{
		#region CONSTANTS

		protected const int					PARTICLES_COUNT = 256;
		protected const int					ITERATIONS_COUNT = 2;
		protected const float				PARTICLE_MIN_RADIUS = 4 * 0.005f;
		protected const float				PARTICLE_MAX_RADIUS = 4 * 0.01f;

		protected const float				PARTICLES_MAX_WEIGHT = 0.1f;

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// This structure holds particles positions for each particle instance
		/// </summary>
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_PARTICLE
		{
			[Semantic( "RADIUS" )]
			public float		Radius;
			[Semantic( "COLOR" )]
			public Vector4		Color;
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4>			m_MaterialProcessDynamics = null;
 		protected Material<VS_PARTICLE>		m_MaterialDisplayParticles = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects

		// Screen quad for dynamics processing
		protected Helpers.ScreenQuad		m_Quad = null;

		// The spheres used to represent particles
		protected VertexBuffer<VS_PARTICLE>	m_VBParticlesData = null;

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected RenderTarget<PF_RGBA32F>[]	m_ParticlePositions = new RenderTarget<PF_RGBA32F>[3];


		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected Vector3					m_WindVelocity = new Vector3( 3.0f, 0.0f, 0.0f );
		protected Vector3					m_ParticlesBoxCenter = new Vector3( 0.0f, 5.0f, 0.0f );
		protected float						m_WindTunnelWidth = 40.0f;
		protected float						m_WindTunnelHeight = 10.0f;
		protected float						m_WindTunnelDepth = 20.0f;
		protected float						m_TurbulenceScale = 0.01f;
		protected float						m_TurbulenceFactor = 1.0f;
		protected float						m_TurbulenceBias = 0.9f;
		protected float						m_TurbulenceVerticalBias = 0.0f;
		protected float						m_TurbulenceOffsetSpeed = -3.0f;
		protected float						m_ParticleWeightFactor = 1.0f;	// Weight based on radius
		protected float						m_VelocitySpread = 1.0f;
		protected bool						m_bProcessDynamics = true;

		protected float						m_AmbientFactor = 0.05f;
		protected float						m_DiffuseFactor = 0.1f;
		protected float						m_SpecularFactor = 1.0f;

		#endregion

		#region PROPERTIES

		public Vector3						WindVelocity			{ get { return m_WindVelocity; } set { m_WindVelocity = value; } }
		public Vector3						ParticlesBoxCenter		{ get { return m_ParticlesBoxCenter; } set { m_ParticlesBoxCenter = value; } }
		public float						WindTunnelWidth			{ get { return m_WindTunnelWidth; } set { m_WindTunnelWidth = value; } }
		public float						WindTunnelHeight		{ get { return m_WindTunnelHeight; } set { m_WindTunnelHeight = value; } }
		public float						WindTunnelDepth			{ get { return m_WindTunnelDepth; } set { m_WindTunnelDepth = value; } }
		public float						TurbulenceScale			{ get { return m_TurbulenceScale; } set { m_TurbulenceScale = value; } }
		public float						TurbulenceFactor		{ get { return m_TurbulenceFactor; } set { m_TurbulenceFactor = value; } }
		public float						TurbulenceBias			{ get { return m_TurbulenceBias; } set { m_TurbulenceBias = value; } }
		public float						TurbulenceVerticalBias	{ get { return m_TurbulenceVerticalBias; } set { m_TurbulenceVerticalBias = value; } }
		public float						TurbulenceOffsetSpeed	{ get { return m_TurbulenceOffsetSpeed; } set { m_TurbulenceOffsetSpeed = value; } }
		public float						ParticleWeightFactor	{ get { return m_ParticleWeightFactor; } set { m_ParticleWeightFactor = value; } }
		public float						VelocitySpread			{ get { return m_VelocitySpread; } set { m_VelocitySpread = value; } }
		public bool							ProcessDynamics			{ get { return m_bProcessDynamics; } set { m_bProcessDynamics = value; } }

		public float						AmbientFactor			{ get { return m_AmbientFactor; } set { m_AmbientFactor = value; } }
		public float						DiffuseFactor			{ get { return m_DiffuseFactor; } set { m_DiffuseFactor = value; } }
		public float						SpecularFactor			{ get { return m_SpecularFactor; } set { m_SpecularFactor = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueWindParticles( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			m_Renderer = _Renderer;

			// Create our main materials
 			m_MaterialProcessDynamics = m_Renderer.LoadMaterial<VS_Pt4>( "WindParticles Dynamics", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/WindParticlesDynamics.fx" ) );
 			m_MaterialDisplayParticles = m_Renderer.LoadMaterial<VS_PARTICLE>( "WindParticles Display", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/WindParticlesDisplay.fx" ) );

			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Particles Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the particles
			Random	RNG = new Random( 1 );

			VS_PARTICLE[]	Particles = new VS_PARTICLE[PARTICLES_COUNT];
			for ( int ParticleIndex=0; ParticleIndex < PARTICLES_COUNT; ParticleIndex++ )
			{
				float	fRadius = PARTICLE_MIN_RADIUS + (float) RNG.NextDouble() * (PARTICLE_MAX_RADIUS - PARTICLE_MIN_RADIUS);
				float	fColor = 1.0f - 0.9f * fRadius / PARTICLE_MAX_RADIUS;
				float	fAlpha = 0.5f + 0.5f * fRadius / PARTICLE_MAX_RADIUS;

				Particles[ParticleIndex] = new VS_PARTICLE()
				{
					Radius = fRadius,
					Color = new Vector4( fColor, fColor, fColor, fAlpha )
				};
			}

			m_VBParticlesData = ToDispose( new VertexBuffer<VS_PARTICLE>( m_Device, "ParticlesData", Particles ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the particles' position buffers
			using ( Image<PF_RGBA32F> InitialPositions = new Image<PF_RGBA32F>( m_Device, "ParticlesInitialPositionsImage", PARTICLES_COUNT, 1, ( int _X, int _Y, ref Vector4 _Color ) =>
				{
					_Color.X = m_ParticlesBoxCenter.X + ((float) RNG.NextDouble() - 0.5f) * m_WindTunnelWidth;
					_Color.Y = m_ParticlesBoxCenter.Y + ((float) RNG.NextDouble() - 0.5f) * m_WindTunnelHeight;
					_Color.Z = m_ParticlesBoxCenter.Z + ((float) RNG.NextDouble() - 0.5f) * m_WindTunnelDepth;
					_Color.W = Particles[_X].Radius * PARTICLES_MAX_WEIGHT;
				}, 1 ) )
			{
				m_ParticlePositions[0] = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "ParticlesPositions0", InitialPositions ) );
				m_ParticlePositions[1] = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "ParticlesPositions1", InitialPositions ) );
			}
			m_ParticlePositions[2] = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "ParticlesPositions2", PARTICLES_COUNT, 1, 1 ) );
		}

		public void		SetParticlesBoxFromMesh( Cirrus.Scene.Mesh _Mesh )
		{
			BoundingBox	WorldBBox = _Mesh.WorldBBox;

			m_ParticlesBoxCenter = 0.5f * (WorldBBox.Minimum + WorldBBox.Maximum);

			Vector3	TunnelDimensions = WorldBBox.Maximum - WorldBBox.Minimum;
			m_WindTunnelWidth = TunnelDimensions.X;
			m_WindTunnelHeight = TunnelDimensions.Y;
			m_WindTunnelDepth = TunnelDimensions.Z;
		}

		protected float		m_PreviousTime = 0.0f;
		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			//////////////////////////////////////////////////////////////////////////
			// 1] Process particles dynamics
			if ( m_bProcessDynamics )
			using ( m_MaterialProcessDynamics.UseLock() )
			{
				m_Device.SetViewport( 0, 0, PARTICLES_COUNT, 1, 0.0f, 1.0f );
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				CurrentMaterial.GetVariableByName( "ParticlesCount" ).AsScalar.Set( PARTICLES_COUNT );
				CurrentMaterial.GetVariableByName( "BBoxMin" ).AsVector.Set( m_ParticlesBoxCenter - 0.5f * new Vector3( m_WindTunnelWidth, m_WindTunnelHeight, m_WindTunnelDepth ) );
				CurrentMaterial.GetVariableByName( "BBoxMax" ).AsVector.Set( +0.5f * new Vector3( m_WindTunnelWidth, m_WindTunnelHeight, m_WindTunnelDepth ) );
				CurrentMaterial.GetVariableByName( "WindVelocity" ).AsVector.Set( m_WindVelocity );
				CurrentMaterial.GetVariableByName( "TurbulenceOffset" ).AsVector.Set( m_TurbulenceOffsetSpeed * m_WindVelocity * m_Renderer.Time );
				CurrentMaterial.GetVariableByName( "TurbulenceScale" ).AsScalar.Set( m_TurbulenceScale );
				CurrentMaterial.GetVariableByName( "TurbulenceFactor" ).AsScalar.Set( m_TurbulenceFactor );
				CurrentMaterial.GetVariableByName( "TurbulenceBias" ).AsScalar.Set( m_TurbulenceBias );
				CurrentMaterial.GetVariableByName( "TurbulenceVerticalBias" ).AsScalar.Set( m_TurbulenceVerticalBias );
				CurrentMaterial.GetVariableByName( "ParticleWeightFactor" ).AsScalar.Set( m_ParticleWeightFactor );

				float	CurrentTime = m_Renderer.Time;
				float	Dt = CurrentTime - m_PreviousTime;
				m_PreviousTime = CurrentTime;
				CurrentMaterial.GetVariableByName( "Dt" ).AsScalar.Set( Dt / ITERATIONS_COUNT );

				for ( int IterationIndex=0; IterationIndex < ITERATIONS_COUNT; IterationIndex++ )
				{
					m_Device.SetRenderTarget( m_ParticlePositions[2] );

					CurrentMaterial.GetVariableByName( "TextureCurrentPositions" ).AsResource.SetResource( IterationIndex == 0 ? m_ParticlePositions[0] : m_ParticlePositions[1] );

					CurrentMaterial.ApplyPass( 0 );
					m_Quad.Render();

					// Cycle buffers #1 & #2
					RenderTarget<PF_RGBA32F>	Temp = m_ParticlePositions[1];
					m_ParticlePositions[1] = m_ParticlePositions[2];
					m_ParticlePositions[2] = Temp;
				}

				// Cycle all 3 buffers
				RenderTarget<PF_RGBA32F>	Temp2 = m_ParticlePositions[0];
				m_ParticlePositions[0] = m_ParticlePositions[1];
				m_ParticlePositions[1] = m_ParticlePositions[2];
				m_ParticlePositions[2] = Temp2;

// 				// DEBUG
// 				m_Renderer.SetDefaultRenderTarget();
// 				CurrentMaterial.GetVariableByName( "TextureCurrentPositions" ).AsResource.SetResource( m_ParticlePositions[1] );
// 				CurrentMaterial.ApplyPass( 0 );
// 				m_Quad.Render();
			}

			//////////////////////////////////////////////////////////////////////////
			// 2] Display particles
			using ( m_MaterialDisplayParticles.UseLock() )
			{
				m_Renderer.SetDefaultRenderTarget();
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
//				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				CurrentMaterial.GetVariableByName( "ParticlesCount" ).AsScalar.Set( PARTICLES_COUNT );
				CurrentMaterial.GetVariableByName( "TexturePreviousPositions" ).AsResource.SetResource( m_ParticlePositions[2] );
				CurrentMaterial.GetVariableByName( "TextureCurrentPositions" ).AsResource.SetResource( m_ParticlePositions[0] );
				CurrentMaterial.GetVariableByName( "VelocitySpread" ).AsScalar.Set( m_VelocitySpread );
				CurrentMaterial.GetVariableByName( "AmbientFactor" ).AsScalar.Set( m_AmbientFactor );
				CurrentMaterial.GetVariableByName( "DiffuseFactor" ).AsScalar.Set( m_DiffuseFactor );
				CurrentMaterial.GetVariableByName( "SpecularFactor" ).AsScalar.Set( m_SpecularFactor );

				CurrentMaterial.ApplyPass( 0 );

				m_Device.InputAssembler.SetPrimitiveTopology( SharpDX.Direct3D.PrimitiveTopology.PointList );
				m_VBParticlesData.Use();
				m_VBParticlesData.Draw();
			}
		}

		#endregion
	}
}
