//#define USE_PERPARTICLE_FRACTAL_DISTORT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Mega particles effect
	/// from http://www.inframez.com/events_volclouds_slide18.htm
	/// </example>
	public class RenderTechniqueMegaParticles : RenderTechniqueBase, IGeometryWriter<VS_P3N3T2,int>
	{
		#region CONSTANTS

		protected const int		PARTICLES_COUNT = 40;
		protected const float	PARTICLE_RANGE_HORIZ = 1.0f;
		protected const float	PARTICLE_RANGE_VERT = 1.0f;
		protected const float	PARTICLE_RADIUS_MAX = 1.0f;
		protected const int		ICOSAHEDRON_SUBDIVISIONS_COUNT = 2;
		protected const int		DEEP_SHADOW_MAP_SIZE = 1024;
		protected const float	CLOUD_MAP_SIZE_FACTOR = 2.0f;

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// This structure holds particles positions for each particle instance
		/// </summary>
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_PP3
		{
			[InstanceSemantic( "PARTICLE_POSITION", 1 )]
			public Vector3		Position;
			[InstanceSemantic( "PARTICLE_RADIUS", 1 )]
			public float		Radius;
		}

		/// <summary>
		/// This composite structure holds particles geometry (N vertices per instance) and a particle position (1 per instance)
		/// </summary>
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_Particle
		{
			[VertexBufferStart( 0 )]
			public VS_P3N3T2	Geometry;

			[VertexBufferStart( 1 )]
			public VS_PP3		Position;
		}

		[System.Diagnostics.DebuggerDisplay( "D={Distance} [{Index}]" )]
		protected class		Particle : IComparable<Particle>
		{
			public int		Index;
			public float	Distance;

			#region IComparable<Particle> Members

			public int CompareTo( Particle other )
			{
				return other.Distance < Distance ? -1 : (other.Distance > Distance ? +1 : 0);
			}

			#endregion
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Particle>		m_MaterialDisplayParticles = null;
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;
 		protected Material<VS_T2>			m_MaterialCloudDistortMesh = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects

		// Screen quad for post-processing
		protected Helpers.ScreenQuad		m_Quad = null;

		// The spheres used to represent particles
		protected Primitive<VS_P3N3T2,int>	m_ParticlesSpheres = null;

		protected Vector3[]					m_ParticlePositions = new Vector3[PARTICLES_COUNT];
		protected float[]					m_ParticleRadius = new float[PARTICLES_COUNT];
		protected List<Particle>			m_ParticleDistances = new List<Particle>();

		// The vertex buffer of positions for each particle instance
		protected VertexBuffer<VS_PP3>		m_VBParticlesPositions = null;

		protected Primitive<VS_T2,int>		m_ScreenTiles = null;

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets

		// The deep shadow map
		protected RenderTarget<PF_RGBA16F>	m_DeepShadowMap = null;

		// The cloud map we render the cloud to and we blend with screen
		protected RenderTarget<PF_RGBA16F>		m_CloudMapNormalDepth = null;
		protected RenderTarget<PF_RG16F>[]		m_CloudDepthMapsBlurred = new RenderTarget<PF_RG16F>[2];
		protected RenderTarget<PF_RGBA16F>[]	m_CloudMaps = new RenderTarget<PF_RGBA16F>[2];


		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected Vector3					m_LightPosition = new Vector3( 1, 1, -1 );
		protected float						m_LightIntensity = 1.0f;
		protected float						m_LightRadius = 0.1f;
		protected Vector3					m_CloudPosition = new Vector3( 0, 0, 0 );
		protected float						m_CloudRadius = 0.2f;

		protected float						m_ExtinctionCoefficient = 0.5f;
		protected float						m_ScatteringCoefficient = 0.01f;
		protected float						m_ScatteringAnisotropyBackward = -0.2f;
		protected float						m_ScatteringAnisotropyForward = 0.98f;
		protected float						m_ScatteringAnisotropySide = 0.5f;
		protected float						m_PhaseWeightBackward = 1.0f;
		protected float						m_PhaseWeightForward = 0.05f;
		protected float						m_PhaseWeightSide = 2.5f;
		protected float						m_MaxMarchDistance = 0.05f;
		protected float						m_DiffuseFactor = 0.3f;
		protected float						m_DiffuseBias = 0.1f;
		protected float						m_SpecularFactor = 0.05f;
		protected float						m_SpecularPower = 1.0f;

		protected float						m_GaussDistance = 1.0f;
		protected float						m_GaussDistanceDepth = 2.0f;
		protected float						m_GaussWeight = 1.0f;
		protected Vector2					m_Distance2PlaneFactors = new Vector2( 0.02f, 1.0f );

		protected int						m_OctavesCount = 4;
		protected float						m_FrequencyFactor = 0.5f;
		protected float						m_OffsetFactor = 0.02f;

		protected Vector4					m_GP = Vector4.Zero;

		#endregion

		#region PROPERTIES

		public Vector3						LightPosition		{ get { return m_LightPosition; } set { m_LightPosition = value; } }
//		public float						LightRadius			{ get { return m_LightRadius; } set { m_LightRadius = value; } }
		public float						LightIntensity		{ get { return m_LightIntensity; } set { m_LightIntensity = value; } }
//		public Vector3						CloudPosition		{ get { return m_CloudPosition; } set { m_CloudPosition = value; } }
//		public float						CloudRadius			{ get { return m_CloudRadius; } set { m_CloudRadius = value; } }

		public float						ExtinctionCoefficient	{ get { return m_ExtinctionCoefficient; } set { m_ExtinctionCoefficient = value; } }
		public float						ScatteringCoefficient	{ get { return m_ScatteringCoefficient; } set { m_ScatteringCoefficient = value; } }
		public float						ScatteringAnisotropyForward	{ get { return m_ScatteringAnisotropyForward; } set { m_ScatteringAnisotropyForward = value; } }
		public float						PhaseWeightForward		{ get { return m_PhaseWeightForward; } set { m_PhaseWeightForward = value; } }
		public float						ScatteringAnisotropyBackward	{ get { return m_ScatteringAnisotropyBackward; } set { m_ScatteringAnisotropyBackward = value; } }
		public float						PhaseWeightBackward		{ get { return m_PhaseWeightBackward; } set { m_PhaseWeightBackward = value; } }
		public float						ScatteringAnisotropySide	{ get { return m_ScatteringAnisotropySide; } set { m_ScatteringAnisotropySide = value; } }
		public float						PhaseWeightSide			{ get { return m_PhaseWeightSide; } set { m_PhaseWeightSide = value; } }
		public float						MaxMarchDistance		{ get { return m_MaxMarchDistance; } set { m_MaxMarchDistance = value; } }
		public float						DiffuseFactor		{ get { return m_DiffuseFactor; } set { m_DiffuseFactor = value; } }
		public float						DiffuseBias			{ get { return m_DiffuseBias; } set { m_DiffuseBias = value; } }
		public float						SpecularFactor		{ get { return m_SpecularFactor; } set { m_SpecularFactor = value; } }
		public float						SpecularPower		{ get { return m_SpecularPower; } set { m_SpecularPower = value; } }

		public float						GaussDistance		{ get { return m_GaussDistance; } set { m_GaussDistance = value; } }
		public float						GaussDistanceDepth	{ get { return m_GaussDistanceDepth; } set { m_GaussDistanceDepth = value; } }
		public float						GaussWeight			{ get { return m_GaussWeight; } set { m_GaussWeight = value; } }
		public Vector2						Distance2PlaneFactors	{ get { return m_Distance2PlaneFactors; } set { m_Distance2PlaneFactors = value; } }

		public int							OctavesCount 		{ get { return m_OctavesCount; } set { m_OctavesCount = value; } }
		public float						FrequencyFactor		{ get { return m_FrequencyFactor; } set { m_FrequencyFactor = value; } }
		public float						OffsetFactor 		{ get { return m_OffsetFactor; } set { m_OffsetFactor = value; } }

		public Vector4						GP					{ get { return m_GP; } set { m_GP = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueMegaParticles( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
 			m_MaterialDisplayParticles = m_Renderer.LoadMaterial<VS_Particle>( "Display Mega Particles Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/DisplayMegaParticles.fx" ) );
 			m_MaterialPostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "MegaParticles Post-Process Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/MegaParticlesPostProcess.fx" ) );
 			m_MaterialCloudDistortMesh = m_Renderer.LoadMaterial<VS_T2>( "MegaParticles Distort Mesh Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/CloudDistortMesh.fx" ) );

// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the caustics texture array (6 cube faces)
// 			m_CausticsTexture = ToDispose( new RenderTarget<PF_R16F>( Device, "Caustics Texture", RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 1, 6, 1 ) );
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the 2 lens flare textures
// 			m_LensFlareTextures[0] = ToDispose( new RenderTarget<PF_RGBA8>( Device, "LensFlare Texture 0", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, 1 ) );
// 			m_LensFlareTextures[1] = ToDispose( new RenderTarget<PF_RGBA8>( Device, "LensFlare Texture 1", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, 1 ) );
// 			m_LensFlareDepthStencil = ToDispose( new DepthStencil<PF_D32>( Device, "LensFlare DepthStencil", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, false ) );

			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Lens-Flare Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the cloud maps
			int	CloudMapWidth = (int) (m_Device.DefaultRenderTarget.Width / CLOUD_MAP_SIZE_FACTOR);
			int	CloudMapHeight = (int) (m_Device.DefaultRenderTarget.Height / CLOUD_MAP_SIZE_FACTOR);
			m_CloudMapNormalDepth = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "CloudMapNormalDepth", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 1 ) );
			m_CloudDepthMapsBlurred[0] = ToDispose( new RenderTarget<PF_RG16F>( m_Device, "CloudBDepthMaplurred0", CloudMapWidth, CloudMapHeight, 1 ) );
			m_CloudDepthMapsBlurred[1] = ToDispose( new RenderTarget<PF_RG16F>( m_Device, "CloudBDepthMaplurred1", CloudMapWidth, CloudMapHeight, 1 ) );
			m_CloudMaps[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "CloudMap0", CloudMapWidth, CloudMapHeight, 1 ) );
			m_CloudMaps[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "CloudMap1", CloudMapWidth, CloudMapHeight, 1 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the deep shadow map
			m_DeepShadowMap = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "DeepShadowMap", DEEP_SHADOW_MAP_SIZE, DEEP_SHADOW_MAP_SIZE, 1 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the particles
			// For the moment, particles are displayed as spheres, there are 2 intertwined vertex buffers
			// 1 for the sphere geometry
			// 1 for instance positioning
			m_ParticlesSpheres = ToDispose( Helpers.GeodesicSphere<VS_P3N3T2,int>.Build( m_Device, "ParticleSphere", Helpers.GEODESIC_SPHERE_BASE_SHAPE.ICOSAHEDRON, ICOSAHEDRON_SUBDIVISIONS_COUNT, Helpers.GeometryMapperCube.DEFAULT, this, null ) );

			Random	RNG = new Random( 1 );

			m_ParticlesBBoxWorld.Minimum = +float.MaxValue * Vector3.One;
			m_ParticlesBBoxWorld.Maximum = -float.MaxValue * Vector3.One;

			VS_PP3[]	ParticlePositions = new VS_PP3[PARTICLES_COUNT];
			for ( int ParticleIndex=0; ParticleIndex < PARTICLES_COUNT; ParticleIndex++ )
			{
				m_ParticlePositions[ParticleIndex] = 0.5f * new Vector3(
					PARTICLE_RANGE_HORIZ * (float) (2.0 * RNG.NextDouble() - 1.0),
					PARTICLE_RANGE_VERT * (float) (2.0 * RNG.NextDouble() - 1.0),
					PARTICLE_RANGE_HORIZ * (float) (2.0 * RNG.NextDouble() - 1.0) );
				m_ParticleRadius[ParticleIndex] = PARTICLE_RADIUS_MAX * (0.1f + (float) (0.2 * RNG.NextDouble()));
				m_ParticleDistances.Add( new Particle() );

// 				if ( ParticleIndex > PARTICLES_COUNT/2 )
// 				{
// 					m_ParticlePositions[ParticleIndex].X += 4.0f;
// 				}

//m_ParticlePositions[ParticleIndex] = Vector3.Zero;

				ParticlePositions[ParticleIndex].Position = m_ParticlePositions[ParticleIndex];
				ParticlePositions[ParticleIndex].Radius = m_ParticleRadius[ParticleIndex];

				// Update BBox
				m_ParticlesBBoxWorld.Minimum = Vector3.Min( m_ParticlesBBoxWorld.Minimum, ParticlePositions[ParticleIndex].Position - m_ParticleRadius[ParticleIndex] * Vector3.One );
				m_ParticlesBBoxWorld.Maximum = Vector3.Max( m_ParticlesBBoxWorld.Maximum, ParticlePositions[ParticleIndex].Position + m_ParticleRadius[ParticleIndex] * Vector3.One );
			}
			m_VBParticlesPositions = ToDispose( new VertexBuffer<VS_PP3>( m_Device, "ParticlePositions", ParticlePositions ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the screen tiling
			const int	SCREEN_TILE_SIZE = 500;

			VS_T2[]	Vertices = new VS_T2[(1+SCREEN_TILE_SIZE)*(1+SCREEN_TILE_SIZE)];
			for ( int Y=0; Y <= SCREEN_TILE_SIZE; Y++ )
			{
				float	V = (float) Y / SCREEN_TILE_SIZE;
				for ( int X=0; X <= SCREEN_TILE_SIZE; X++ )
				{
					float	U = (float) X / SCREEN_TILE_SIZE;
					Vertices[(1+SCREEN_TILE_SIZE)*Y+X].UV = new Vector2( U, V );
				}
			}

			int[]	Indices = new int[SCREEN_TILE_SIZE*(2*(SCREEN_TILE_SIZE+1)+2)];
			int		IndexCount = 0;
			for ( int Y=0; Y < SCREEN_TILE_SIZE; Y++ )
			{
				for ( int X=0; X <= SCREEN_TILE_SIZE; X++ )
				{
					Indices[IndexCount++] = (SCREEN_TILE_SIZE+1) * Y + X;
					Indices[IndexCount++] = (SCREEN_TILE_SIZE+1) * (Y+1) + X;
				}

				// Degenerate vertex
				Indices[IndexCount++] = (SCREEN_TILE_SIZE+1) * (Y+1) + SCREEN_TILE_SIZE;
				Indices[IndexCount++] = (SCREEN_TILE_SIZE+1) * (Y+1) + 0;
			}

			m_ScreenTiles = ToDispose( new Primitive<VS_T2,int>( m_Device, "Screen Tiling", SharpDX.Direct3D.PrimitiveTopology.TriangleStrip, Vertices, Indices ) );
		}

		protected  BoundingBox	m_ParticlesBBoxWorld;
		protected Matrix	m_Light2World;
		protected Matrix	m_World2Light;
		protected Matrix	m_Light2Proj;
		protected Matrix	m_World2LightProj;
		protected Vector4	m_SliceMin;
		protected Vector4	m_SliceMax;

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			//////////////////////////////////////////////////////////////////////////
			// 1] Compute LIGHT space data
			Vector3	BBoxCenter = 0.5f * (m_ParticlesBBoxWorld.Minimum + m_ParticlesBBoxWorld.Maximum);

			// Compute light space transform
			Vector3	Light2Cloud = m_CloudPosition - m_LightPosition;
			Light2Cloud.Normalize();
			Vector3	Right = Vector3.Cross( Light2Cloud, Vector3.UnitY );
			Right.Normalize();
			Vector3	Up = Vector3.Cross( Right, Light2Cloud );
			m_Light2World.Row1 = new Vector4( Right, 0.0f );
			m_Light2World.Row2 = new Vector4( Up, 0.0f );
			m_Light2World.Row3 = new Vector4( Light2Cloud, 0.0f );
			m_Light2World.Row4 = new Vector4( BBoxCenter, 1.0f );

			m_World2Light = m_Light2World;
			m_World2Light.Invert();

			// Compute particles' bounding box in LIGHT space
			Vector3[]	BBoxCornersWorld = m_ParticlesBBoxWorld.GetCorners();
			BoundingBox	ParticlesBBoxLight = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			for ( int CornerIndex=0; CornerIndex < 8; CornerIndex++ )
			{
				Vector3	CornerLight = Vector3.TransformCoordinate( BBoxCornersWorld[CornerIndex], m_World2Light );
				ParticlesBBoxLight.Minimum = Vector3.Min( ParticlesBBoxLight.Minimum, CornerLight );
				ParticlesBBoxLight.Maximum = Vector3.Max( ParticlesBBoxLight.Maximum, CornerLight );
			}

			// Compute light space projection
			Vector3	Scale = new Vector3( 2.0f / (ParticlesBBoxLight.Maximum.X - ParticlesBBoxLight.Minimum.X),
										 2.0f / (ParticlesBBoxLight.Maximum.Y - ParticlesBBoxLight.Minimum.Y),
										 1.0f / (ParticlesBBoxLight.Maximum.Z - ParticlesBBoxLight.Minimum.Z) );
			m_Light2Proj = new Matrix();
			m_Light2Proj.Row1 = new Vector4( Scale.X, 0.0f, 0.0f, 0.0f );
			m_Light2Proj.Row2 = new Vector4( 0.0f, Scale.Y, 0.0f, 0.0f );
			m_Light2Proj.Row3 = new Vector4( 0.0f, 0.0f, Scale.Z, 0.0f );
			m_Light2Proj.Row4 = new Vector4( -1.0f - ParticlesBBoxLight.Minimum.X * Scale.X, -1.0f - ParticlesBBoxLight.Minimum.Y * Scale.Y, -ParticlesBBoxLight.Minimum.Z * Scale.Z, 1.0f );

// DEBUG
// Vector3	Test;
// for ( int CornerIndex=0; CornerIndex < 8; CornerIndex++ )
// {
// 	Vector3	CornerLight = Vector3.TransformCoordinate( BBoxCornersWorld[CornerIndex], m_World2Light );
// 	Test = Vector3.TransformCoordinate( CornerLight, m_Light2Proj );
// }
// DEBUG

			m_World2LightProj = m_World2Light * m_Light2Proj;

			// Compute slices' min/max boundaries
			m_SliceMin = ParticlesBBoxLight.Minimum.Z * Vector4.One + (ParticlesBBoxLight.Maximum.Z - ParticlesBBoxLight.Minimum.Z) * new Vector4( 0.0f, 0.25f, 0.5f, 0.75f );
			m_SliceMax = ParticlesBBoxLight.Minimum.Z * Vector4.One + (ParticlesBBoxLight.Maximum.Z - ParticlesBBoxLight.Minimum.Z) * new Vector4( 0.25f, 0.5f, 0.75f, 1.0f );

			//////////////////////////////////////////////////////////////////////////
			// 2] Render mega particles to the deep shadow map
			using ( m_MaterialDisplayParticles.UseLock() )
			{
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderDeepShadowMap" );

				m_Device.SetRenderTarget( m_DeepShadowMap );
				m_Device.SetViewport( 0, 0, DEEP_SHADOW_MAP_SIZE, DEEP_SHADOW_MAP_SIZE, 0.0f, 1.0f );
				m_Device.ClearRenderTarget( m_DeepShadowMap, new Color4( 0.0f ) );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );

				m_Device.InputAssembler.SetPrimitiveTopology( m_ParticlesSpheres.Topology );
				m_Device.InputAssembler.SetVertexBuffers( 0, m_ParticlesSpheres.VertexBuffer.Binding, m_VBParticlesPositions.Binding );
				m_ParticlesSpheres.IndexBuffer.Use();

				CurrentMaterial.GetVariableByName( "World2Light" ).AsMatrix.SetMatrix( m_World2Light );
				CurrentMaterial.GetVariableByName( "Light2Proj" ).AsMatrix.SetMatrix( m_Light2Proj );
				CurrentMaterial.GetVariableByName( "SliceMin" ).AsVector.Set( m_SliceMin );
				CurrentMaterial.GetVariableByName( "SliceMax" ).AsVector.Set( m_SliceMax );

				// Render the front slices
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.SUBTRACTIVE );
				CurrentMaterial.ApplyPass( 0 );
				m_ParticlesSpheres.IndexBuffer.DrawInstanced( PARTICLES_COUNT );

				// Render the back slices
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_FRONT );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );
				CurrentMaterial.ApplyPass( 0 );
				m_ParticlesSpheres.IndexBuffer.DrawInstanced( PARTICLES_COUNT );
			}

#if !USE_PERPARTICLE_FRACTAL_DISTORT
			//////////////////////////////////////////////////////////////////////////
			// 2] Render mega particles in depth stencil only
			using ( m_MaterialDisplayParticles.UseLock() )
			{
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderParticles" );

				m_Device.SetRenderTarget( m_CloudMapNormalDepth, m_Device.DefaultDepthStencil );
				m_Device.ClearRenderTarget( m_CloudMapNormalDepth, new Color4( 1e3f, 0.0f, 0.0f, 0.0f ) );
				m_Device.SetViewport( 0, 0, m_CloudMapNormalDepth.Width, m_CloudMapNormalDepth.Height, 0.0f, 1.0f );
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				CurrentMaterial.GetVariableByName( "OctavesCount" ).AsScalar.Set( m_OctavesCount );
				CurrentMaterial.GetVariableByName( "FrequencyFactor" ).AsScalar.Set( m_FrequencyFactor );
				CurrentMaterial.GetVariableByName( "OffsetFactor" ).AsScalar.Set( m_OffsetFactor );

				CurrentMaterial.ApplyPass( 0 );

				m_Device.InputAssembler.SetPrimitiveTopology( m_ParticlesSpheres.Topology );
				m_Device.InputAssembler.SetVertexBuffers( 0, m_ParticlesSpheres.VertexBuffer.Binding, m_VBParticlesPositions.Binding );
				m_ParticlesSpheres.IndexBuffer.Use();
				m_ParticlesSpheres.IndexBuffer.DrawInstanced( PARTICLES_COUNT );
			}

			using ( m_MaterialPostProcess.UseLock() )
			{
				VariableResource	vSourceTexture = CurrentMaterial.GetVariableByName( "SourceTexture" ).AsResource;
				VariableVector		vInvSourceTextureSize = CurrentMaterial.GetVariableByName( "InvSourceTextureSize" ).AsVector;

				//////////////////////////////////////////////////////////////////////////
				// 3] Perform cloud rendering in screen space
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderCloudLighting" );

				m_Device.SetRenderTarget( m_CloudMaps[0] );
				m_Device.SetViewport( 0, 0, m_CloudMaps[0].Width, m_CloudMaps[0].Height, 0.0f, 1.0f );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.ClearRenderTarget( m_CloudMaps[0], new Color4( 0.0f ) );

				vSourceTexture.SetResource( m_CloudMapNormalDepth );
				vInvSourceTextureSize.Set( new Vector3( 1.0f / m_Device.DefaultRenderTarget.Width, 1.0f / m_Device.DefaultRenderTarget.Height, 0.0f ) );

				// DSM data
				CurrentMaterial.GetVariableByName( "World2LightProj" ).AsMatrix.SetMatrix( m_World2LightProj );
				CurrentMaterial.GetVariableByName( "DeepShadowMap" ).AsResource.SetResource( m_DeepShadowMap );
				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightIntensity" ).AsScalar.Set( m_LightIntensity );

				// Noise data
				CurrentMaterial.GetVariableByName( "OctavesCount" ).AsScalar.Set( m_OctavesCount );
				CurrentMaterial.GetVariableByName( "FrequencyFactor" ).AsScalar.Set( m_FrequencyFactor );
				CurrentMaterial.GetVariableByName( "OffsetFactor" ).AsScalar.Set( m_OffsetFactor );

				// Lighting data
				CurrentMaterial.GetVariableByName( "ExtinctionCoefficient" ).AsScalar.Set( m_ExtinctionCoefficient );
				CurrentMaterial.GetVariableByName( "ScatteringCoefficient" ).AsScalar.Set( m_ScatteringCoefficient );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyForward" ).AsScalar.Set( m_ScatteringAnisotropyForward );
				CurrentMaterial.GetVariableByName( "PhaseWeightForward" ).AsScalar.Set( m_PhaseWeightForward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyBackward" ).AsScalar.Set( m_ScatteringAnisotropyBackward );
				CurrentMaterial.GetVariableByName( "PhaseWeightBackward" ).AsScalar.Set( m_PhaseWeightBackward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropySide" ).AsScalar.Set( m_ScatteringAnisotropySide );
				CurrentMaterial.GetVariableByName( "PhaseWeightSide" ).AsScalar.Set( m_PhaseWeightSide );
				CurrentMaterial.GetVariableByName( "MaxMarchDistance" ).AsScalar.Set( m_MaxMarchDistance );
				CurrentMaterial.GetVariableByName( "DiffuseFactor" ).AsScalar.Set( m_DiffuseFactor );
				CurrentMaterial.GetVariableByName( "DiffuseBias" ).AsScalar.Set( m_DiffuseBias );
				CurrentMaterial.GetVariableByName( "SpecularFactor" ).AsScalar.Set( m_SpecularFactor );
				CurrentMaterial.GetVariableByName( "SpecularPower" ).AsScalar.Set( m_SpecularPower );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// 4] Blur depth map
				CurrentMaterial.GetVariableByName( "GaussDistanceDepth" ).AsScalar.Set( m_GaussDistanceDepth );
				CurrentMaterial.GetVariableByName( "GaussWeight" ).AsScalar.Set( m_GaussWeight );
				CurrentMaterial.GetVariableByName( "PlaneDepthFactors" ).AsVector.Set( m_Distance2PlaneFactors );

				// ==========================================
				// Horizontal blur
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "GaussianBlurDepthH" );
				m_Device.SetRenderTarget( m_CloudDepthMapsBlurred[0] );

				vInvSourceTextureSize.Set( new Vector2( 2.0f / m_CloudMapNormalDepth.Width, 0.0f ) );
				vSourceTexture.SetResource( m_CloudMapNormalDepth );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

				// ==========================================
				// Vertical blur
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "GaussianBlurDepthV" );
				m_Device.SetRenderTarget( m_CloudDepthMapsBlurred[1] );

				vInvSourceTextureSize.Set( new Vector2( 0.0f, 1.0f / m_CloudDepthMapsBlurred[0].Height  ) );
				vSourceTexture.SetResource( m_CloudDepthMapsBlurred[0] );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();


				//////////////////////////////////////////////////////////////////////////
				// 5] Blur the diffuse map
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "GaussianBlurDiffuse" );
				CurrentMaterial.GetVariableByName( "GaussDistance" ).AsScalar.Set( m_GaussDistance );

				// ==========================================
				// Horizontal blur
				m_Device.SetRenderTarget( m_CloudMaps[1] );

				vInvSourceTextureSize.Set( new Vector2( 1.0f / m_CloudMaps[0].Width, 0.0f ) );
				vSourceTexture.SetResource( m_CloudMaps[0] );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

				// ==========================================
				// Vertical blur
				m_Device.SetRenderTarget( m_CloudMaps[0] );

				vInvSourceTextureSize.Set( new Vector2( 0.0f, 1.0f / m_CloudMaps[1].Height  ) );
				vSourceTexture.SetResource( m_CloudMaps[1] );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

#if true
				//////////////////////////////////////////////////////////////////////////
				// 6] Fractal distort
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "FractalDistort" );

				m_Device.SetRenderTarget( m_CloudMaps[1] );
				vSourceTexture.SetResource( m_CloudMaps[0] );
				CurrentMaterial.GetVariableByName( "SourceDepthTexture" ).AsResource.SetResource( m_CloudDepthMapsBlurred[1] );

				CurrentMaterial.GetVariableByName( "GP" ).AsVector.Set( m_GP );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();

				//////////////////////////////////////////////////////////////////////////
				// 7] Blend result
				m_Renderer.SetDefaultRenderTarget();
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.BLEND );
//				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.PREMULTIPLY_ALPHA );

				vSourceTexture.SetResource( m_CloudMaps[1] );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "BlendResult" );
				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}
#else
			}

			using ( m_MaterialCloudDistortMesh.UseLock() )
			{
//				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DistortCloud" );

				//////////////////////////////////////////////////////////////////////////
				// 6] Fractal distort
				m_Device.SetDefaultRenderTarget();
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.BLEND );
//				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.PREMULTIPLY_ALPHA );

				CurrentMaterial.GetVariableByName( "SourceTexture" ).AsResource.SetResource( m_CloudMaps[0] );
				CurrentMaterial.GetVariableByName( "SourceDepthTexture" ).AsResource.SetResource( m_CloudDepthMapsBlurred[1] );

				// Noise data
				CurrentMaterial.GetVariableByName( "OctavesCount" ).AsScalar.Set( m_OctavesCount );
				CurrentMaterial.GetVariableByName( "FrequencyFactor" ).AsScalar.Set( m_FrequencyFactor );
				CurrentMaterial.GetVariableByName( "OffsetFactor" ).AsScalar.Set( m_OffsetFactor );

				CurrentMaterial.ApplyPass( 0 );
				m_ScreenTiles.RenderOverride();
			}
#endif

#else
			// ==========================================
			// Per-particle fractal distort
			// Particles must be sorted...
			using ( m_MaterialPostProcess.UseLock() )
			{
				m_Device.SetDefaultRenderTarget();
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.BLEND );

				//
				CurrentMaterial.GetVariableByName( "World2LightProj" ).AsMatrix.SetMatrix( m_World2LightProj );
				CurrentMaterial.GetVariableByName( "DeepShadowMap" ).AsResource.SetResource( m_DeepShadowMap );
				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightIntensity" ).AsScalar.Set( m_LightIntensity );

				CurrentMaterial.GetVariableByName( "ExtinctionCoefficient" ).AsScalar.Set( m_ExtinctionCoefficient );
				CurrentMaterial.GetVariableByName( "ScatteringCoefficient" ).AsScalar.Set( m_ScatteringCoefficient );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyForward" ).AsScalar.Set( m_ScatteringAnisotropyForward );
				CurrentMaterial.GetVariableByName( "PhaseWeightForward" ).AsScalar.Set( m_PhaseWeightForward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropyBackward" ).AsScalar.Set( m_ScatteringAnisotropyBackward );
				CurrentMaterial.GetVariableByName( "PhaseWeightBackward" ).AsScalar.Set( m_PhaseWeightBackward );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropySide" ).AsScalar.Set( m_ScatteringAnisotropySide );
				CurrentMaterial.GetVariableByName( "PhaseWeightSide" ).AsScalar.Set( m_PhaseWeightSide );
				CurrentMaterial.GetVariableByName( "MaxMarchDistance" ).AsScalar.Set( m_MaxMarchDistance );

 				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "FractalDistort_Particle" );
 				CurrentMaterial.ApplyPass( 0 );

				// Build distance list & sort particles
				Vector3	CameraPosition = (Vector3) m_Camera.Camera2World.Row4;
				for ( int ParticleIndex=0; ParticleIndex < PARTICLES_COUNT; ParticleIndex++ )
				{
					m_ParticleDistances[ParticleIndex].Index = ParticleIndex;
					m_ParticleDistances[ParticleIndex].Distance = (m_ParticlePositions[ParticleIndex] - CameraPosition).LengthSquared();
				}
				m_ParticleDistances.Sort();

				// Build temporary vertex buffer with sorted particles
				VS_T4[]	SortedParticles = new VS_T4[PARTICLES_COUNT];
				for ( int ParticleIndex=0; ParticleIndex < PARTICLES_COUNT; ParticleIndex++ )
				{
					int	SortedIndex = m_ParticleDistances[ParticleIndex].Index;
					Vector3	Pos = m_ParticlePositions[SortedIndex];
					float	Radius = m_ParticleRadius[SortedIndex];
					SortedParticles[ParticleIndex].UV.X = Pos.X;
					SortedParticles[ParticleIndex].UV.Y = Pos.Y;
					SortedParticles[ParticleIndex].UV.Z = Pos.Z;
					SortedParticles[ParticleIndex].UV.W = 2.0f * Radius;
				}

				m_Device.InputAssembler.SetPrimitiveTopology( SharpDX.Direct3D.PrimitiveTopology.PointList );

				using ( VertexBuffer<VS_T4> VB = new VertexBuffer<VS_T4>( m_Device, "Temp", SortedParticles ) )
				{
					VB.Use();
					VB.Draw();
				}
			}
#endif
		}

		#region IGeometryWriter<VS_P3N3T2,int> Members

		public void WriteVertexData( ref VS_P3N3T2 _Vertex, Vector3 _Position, Vector3 _Normal, Vector3 _Tangent, Vector3 _BiTangent, Vector3 _UVW, Color4 _Color )
		{
			_Vertex.Position = _Position;
			_Vertex.Normal = _Normal;
			_Vertex.UV = new Vector2( _UVW.X, _UVW.Y );
		}

		public void WriteIndexData( ref int _Index, int _Value )
		{
			_Index = _Value;
		}

		public int ReadIndexData( int _Index )
		{
			return _Index;
		}

		#endregion

		#endregion
	}
}
