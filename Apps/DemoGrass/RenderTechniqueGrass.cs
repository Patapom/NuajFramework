// First method combines a fixed vertex buffer of 4 vertices defining a triangle strip
//	and an instance buffer containing positions & colors of each blade of grass
//
//#define USE_METHOD0	// 69 FPS

// Second method only takes instance informations as a point list and lets the GS generate the geometry
//
//#define USE_METHOD1	// 76 FPS

// Third method is the same as method 2 but uses a 50% packed vertex format (just to see the impact of packed vertices)
//
//#define USE_METHOD2	// 75 FPS <== HAHAHA ! No gain !

// Fourth method uses no instance data except SV_InstanceID and fetches informations from precomputed textures
//
//#define USE_METHOD3	// 17 FPS <== OUCH !!

// Fourth method is same as second method but changes VB according to camera orientation to always maximize front to back objects
//
#define USE_METHOD4	// 69 FPS <== Even slower than no pre-orientation !

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
	/// Grass effect
	/// </example>
	public class RenderTechniqueGrass : RenderTechnique
	{
		#region CONSTANTS

		protected const int		GRASS_SIZE = 256;
		protected const float	GRASS_LAND_SIZE = 16.0f;	// The space to cover
		protected const float	GRASS_TUFT_SIZE = 0.075f;	// Size of a grass tuft

		#endregion

		#region NESTED TYPES

#if	USE_METHOD0
		// This is the VB containing instance data for each grass tuft
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_GRASS_INSTANCE
		{
			[InstanceSemantic( SemanticAttribute.POSITION, 1 )]
			public Vector3	InstancePosition;
			[InstanceSemantic( SemanticAttribute.COLOR, 1 )]
			public Vector3	Color;
		}

		// This is the composite VB type used to build the material, it's composed of both the VB containing
		//	a single quad and of a VB containing the instance data
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_GRASS
		{
			// Per-Vertex data
			[VertexBufferStart( 0 )]
			public VS_T2	Vertex;

			// Per-Instance data
			[VertexBufferStart( 1 )]
			public VS_GRASS_INSTANCE	Instance;
		}

#elif USE_METHOD1 || USE_METHOD4
		// This is the VB containing instance data for each grass tuft
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_GRASS_INSTANCE
		{
			[Semantic( SemanticAttribute.POSITION )]
			public Vector3	InstancePosition;
			[Semantic( SemanticAttribute.COLOR )]
			public Vector3	Color;
		}
#elif USE_METHOD2
		// This is the VB containing instance data for each grass tuft
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_GRASS_INSTANCE
		{
			[Semantic( SemanticAttribute.POSITION, 0 )]
			public Half		PositionX;
			[Semantic( SemanticAttribute.POSITION, 1 )]
			public Half		PositionY;
			[Semantic( SemanticAttribute.POSITION, 2 )]
			public Half		PositionZ;
			[Semantic( SemanticAttribute.COLOR, 0 )]
			public Half		ColorR;
			[Semantic( SemanticAttribute.COLOR, 1 )]
			public Half		ColorG;
			[Semantic( SemanticAttribute.COLOR, 2 )]
			public Half		ColorB;
		}
#elif USE_METHOD3
 		protected struct VS_GRASS_INSTANCE
		{
			// NOTHING !
		}
#endif

		#endregion

		#region FIELDS

		protected Camera					m_Camera = null;

		//////////////////////////////////////////////////////////////////////////
		// Materials
#if	USE_METHOD0
		protected Material<VS_GRASS>			m_MaterialGrass = null;
#elif USE_METHOD1 || USE_METHOD2 || USE_METHOD4
		protected Material<VS_GRASS_INSTANCE>	m_MaterialGrass = null;
#elif USE_METHOD3
		protected Material<VS_GRASS_INSTANCE>	m_MaterialGrass = null;
#endif
		// Grass blades primitives & textures
		protected VertexBuffer<VS_T2>				m_GrassQuad = null;
		protected VertexBuffer<VS_GRASS_INSTANCE>	m_GrassInstances = null;
		protected Texture2D<PF_RGB32F>				m_GrassPositionTexture = null;
		protected Texture2D<PF_RGB32F>				m_GrassColorTexture = null;
		protected VertexBuffer<VS_GRASS_INSTANCE>[]	m_GrassInstancesCamera = new VertexBuffer<VS_GRASS_INSTANCE>[8];

		// Grass texture array
		protected Texture2D<PF_RGBA8>		m_GrassTextures = null;

		// The 16x16x16 noise textures
		protected Texture3D<PF_RGBA16F>[]	m_NoiseTextures = new Texture3D<PF_RGBA16F>[4];

		// Motion texture
		protected Texture2D<PF_RG32F>		m_MotionTexture = null;

		// Parameters for animation
		protected bool						m_bGust = false;
		protected float						m_Time = 0.0f;
		protected float						m_GustTime = 0.0f;
		protected float						m_LastTime = 0.0f;
		protected float						m_WindForce = 1.0f;
		protected Vector3					m_WindPosition0 = new Vector3();
		protected Vector3					m_WindPosition1 = new Vector3();
		protected Vector3					m_WindDirection = new Vector3( 1.0f, 0.0f, 0.0f );

		#endregion

		#region PROPERTIES

		public Camera						Camera				{ get { return m_Camera; } set { m_Camera = value; } }
		public float						Time				{ get { return m_Time; } set { m_LastTime = m_Time; m_Time = value; } }
		public float						WindForce			{ get { return m_WindForce; } set { m_WindForce = value; } }
		public bool							Gust				{ get { return m_bGust; } set { m_bGust = value; } }

		#endregion

		#region METHODS

		public unsafe	RenderTechniqueGrass( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Create our main materials
#if	USE_METHOD0
			m_MaterialGrass = ToDispose( new Material<VS_GRASS>( m_Device, "Grass Material 0", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Grass/Grass0.fx" ) ) );
#elif	USE_METHOD1 || USE_METHOD4
			m_MaterialGrass = ToDispose( new Material<VS_GRASS_INSTANCE>( m_Device, "Grass Material 1", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Grass/Grass1.fx" ) ) );
#elif	USE_METHOD2
			m_MaterialGrass = ToDispose( new Material<VS_GRASS_INSTANCE>( m_Device, "Grass Material 2", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Grass/Grass2.fx" ) ) );
#elif	USE_METHOD3
			m_MaterialGrass = ToDispose( new Material<VS_GRASS_INSTANCE>( m_Device, "Grass Material 3", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Grass/Grass3.fx" ) ) );
#endif
			//////////////////////////////////////////////////////////////////////////
			// Create the noise textures
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = CreateNoiseTexture( NoiseIndex );

			//////////////////////////////////////////////////////////////////////////
			// Load grass textures
			System.Drawing.Bitmap Bitmap0 = System.Drawing.Bitmap.FromFile( "Media/Vegetation/Grass/grassWalpha256.png" ) as System.Drawing.Bitmap;
			System.Drawing.Bitmap Bitmap1 = System.Drawing.Bitmap.FromFile( "Media/Vegetation/Grass/678.png" ) as System.Drawing.Bitmap;
			Image<PF_RGBA8>	ImageGrass0 = new Image<PF_RGBA8>( m_Device, "Grass Image", Bitmap0, 0, 1.0f );
			Image<PF_RGBA8>	ImageGrass1 = new Image<PF_RGBA8>( m_Device, "Grass Image", Bitmap1, 0, 1.0f );
			
//			m_GrassTextures = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Grass Textures", new Image<PF_RGBA8>[] { ImageGrass0, ImageGrass1 } ) );
			m_GrassTextures = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Grass Textures", ImageGrass0 ) );

			ImageGrass0.Dispose();
			ImageGrass1.Dispose();
			Bitmap0.Dispose();
			Bitmap1.Dispose();

			//////////////////////////////////////////////////////////////////////////
			// Load motion textures
			{
				byte[]	Motion = Device.LoadFileContent( new System.IO.FileInfo( "Media/MotionTextures/Motion0_256x256.complex" ) );
				System.IO.MemoryStream	S = new System.IO.MemoryStream( Motion );
				System.IO.BinaryReader	R = new System.IO.BinaryReader( S );

				float	Min = float.MaxValue;
				float	Max = -float.MaxValue;
				using ( Image<PF_RG32F> MotionImage = new Image<PF_RG32F>( m_Device, "Motion Image", 256, 256, ( int _X, int _Y, ref Vector4 _Color ) => {

					float	Real = R.ReadSingle();
					float	Imag = R.ReadSingle();

					_Color.X = Real;
					_Color.Y = Imag;

					Min = Math.Min( Min, Real );
					Max = Math.Max( Max, Real );

				}, 0 ))
					m_MotionTexture = ToDispose( new Texture2D<PF_RG32F>( m_Device, "Motion Texture", MotionImage ) );
			}

			//////////////////////////////////////////////////////////////////////////
			// Create the buffers for instancing
			VS_T2[]		QuadVertices = new VS_T2[4]
			{
				new VS_T2( ) { UV=new Vector2( 0.0f, 0.0f ) },
				new VS_T2( ) { UV=new Vector2( 0.0f, 1.0f ) },
				new VS_T2( ) { UV=new Vector2( 1.0f, 0.0f ) },
				new VS_T2( ) { UV=new Vector2( 1.0f, 1.0f ) },
			};
			m_GrassQuad = ToDispose( new VertexBuffer<VS_T2>( m_Device, "GrassQuad", QuadVertices ) );

			// Build grass tufts instances
#if USE_METHOD0 || USE_METHOD1 || USE_METHOD2
			Random	RNG = new Random( 1 );
			VS_GRASS_INSTANCE[]	GrassInstances = new VS_GRASS_INSTANCE[GRASS_SIZE*GRASS_SIZE];
			for ( int Y=0; Y < GRASS_SIZE; Y++ )
			{
				for ( int X=0; X < GRASS_SIZE; X++ )
				{
					float	fX = GRASS_LAND_SIZE * ((X + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
					float	fZ = GRASS_LAND_SIZE * ((Y + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
#if	USE_METHOD0 || USE_METHOD1
					GrassInstances[GRASS_SIZE*Y+X].InstancePosition = new Vector3( fX, 0.0f, fZ );
//					GrassInstances[GRASS_SIZE*Y+X].Color = new Vector3( 0.5f, 0.2f, 0.0f );
					GrassInstances[GRASS_SIZE*Y+X].Color = new Vector3( (float) RNG.NextDouble(), (float) RNG.NextDouble(), (float) RNG.NextDouble() );
#elif	USE_METHOD2
					GrassInstances[GRASS_SIZE*Y+X].PositionX = (Half) fX;
					GrassInstances[GRASS_SIZE*Y+X].PositionY = (Half) 0.0f;
					GrassInstances[GRASS_SIZE*Y+X].PositionZ = (Half) fZ;
					GrassInstances[GRASS_SIZE*Y+X].ColorR = (Half) 0.5f;
					GrassInstances[GRASS_SIZE*Y+X].ColorG = (Half) 0.2f;
					GrassInstances[GRASS_SIZE*Y+X].ColorB = (Half) 0.0f;
#endif
				}
			}
			m_GrassInstances = ToDispose( new VertexBuffer<VS_GRASS_INSTANCE>( m_Device, "GrassInstances", GrassInstances ) );
#elif USE_METHOD3
			// Create 2 textures : 1 for positions and 1 for colors
			Random	RNG = new Random( 1 );
			using ( Image<PF_RGB32F> Image = new Image<PF_RGB32F>( m_Device, "Grass Position Image", GRASS_SIZE, GRASS_SIZE, ( int _X, int _Y, ref Vector4 _Color ) =>
				{
					_Color.X = GRASS_LAND_SIZE * ((_X + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
					_Color.Y = 0.0f;
					_Color.Z = GRASS_LAND_SIZE * ((_Y + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
				}, 1 ))
				m_GrassPositionTexture = ToDispose( new Texture2D<PF_RGB32F>( m_Device, "Grass Position Texture", Image ) );

			using ( Image<PF_RGB32F> Image = new Image<PF_RGB32F>( m_Device, "Grass Color Image", GRASS_SIZE, GRASS_SIZE, ( int _X, int _Y, ref Vector4 _Color ) =>
				{
					_Color.X = 0.5f;
					_Color.Y = 0.2f;
					_Color.Z = 0.0f;
				}, 1 ))
				m_GrassColorTexture = ToDispose( new Texture2D<PF_RGB32F>( m_Device, "Grass Color Texture", Image ) );

			VS_GRASS_INSTANCE[]	Pipo = new VS_GRASS_INSTANCE[GRASS_SIZE*GRASS_SIZE];
//			Pipo[0].Pipo = (Half) 0.0f;
			m_GrassInstances = ToDispose( new VertexBuffer<VS_GRASS_INSTANCE>( m_Device, "GrassInstances", Pipo ) );
#elif USE_METHOD4
			// Build the unsorted grass instances
			Random	RNG = new Random( 1 );
			VS_GRASS_INSTANCE[]	GrassInstances = new VS_GRASS_INSTANCE[GRASS_SIZE*GRASS_SIZE];
			for ( int Y=0; Y < GRASS_SIZE; Y++ )
			{
				for ( int X=0; X < GRASS_SIZE; X++ )
				{
					float	fX = GRASS_LAND_SIZE * ((X + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
					float	fZ = GRASS_LAND_SIZE * ((Y + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
					GrassInstances[GRASS_SIZE*Y+X].InstancePosition = new Vector3( fX, 0.0f, fZ );
					GrassInstances[GRASS_SIZE*Y+X].Color = new Vector3( (float) RNG.NextDouble(), (float) RNG.NextDouble(), (float) RNG.NextDouble() );
				}
			}

			// Build 8 orientation-sorted buffers
			float[]	Keys = new float[GRASS_SIZE*GRASS_SIZE];
			for ( int DirectionIndex=0; DirectionIndex < 8; DirectionIndex++ )
			{
				float	Phi = 2.0f * (float) Math.PI * DirectionIndex / 8;
				Vector3	View = new Vector3( (float) Math.Sin( Phi ), 0.0f, (float) Math.Cos( Phi ) );

				// Build sort keys
				for ( int Index=0; Index < GRASS_SIZE*GRASS_SIZE; Index++ )
					Keys[Index] = -Vector3.Dot( GrassInstances[Index].InstancePosition, View );

				// Sort the instances
				Array.Sort( Keys, GrassInstances );

				m_GrassInstancesCamera[DirectionIndex] = ToDispose( new VertexBuffer<VS_GRASS_INSTANCE>( m_Device, "GrassInstances", GrassInstances ) );
			}
#endif
		}

		public override void	Render( int _FrameToken )
		{
			//////////////////////////////////////////////////////////////////////////
			// Update wind
			float	fDeltaTime = m_Time - m_LastTime;
			float	fMarch0 = Math.Min( 1.0f, (float) (1.8 + 1.4 * Math.Sin( 0.1 * 2.0 * Math.PI * m_Time )) );
			m_WindPosition0 += 0.2f * fDeltaTime * fMarch0 * m_WindDirection;
			float	fMarch1 = Math.Min( 1.0f, (float) (2.1 + 2.0 * Math.Sin( 0.37 * 2.0 * Math.PI * m_Time )) );
			m_WindPosition1 += 0.1f * fDeltaTime * fMarch1 * m_WindDirection;

			if ( m_bGust )
			{	// Restart gust of wind
				m_GustTime = 0.0f;
				m_bGust = false;
			}
			m_GustTime += 4.0f * fDeltaTime * m_WindForce;

			//////////////////////////////////////////////////////////////////////////
			// Render grass scene
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ALPHA2COVERAGE );

#if USE_METHOD0
			using ( m_MaterialGrass.UseLock() )
			{
				// RESULTS:
				// We get something like 91 FPS with default camera settings without moving

				m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.TriangleStrip );

				CurrentMaterial.GetVariableByName( "WindPosition0" ).AsVector.Set( m_WindPosition0 );
				CurrentMaterial.GetVariableByName( "WindPosition1" ).AsVector.Set( m_WindPosition1 );
				CurrentMaterial.GetVariableByName( "WindDirection" ).AsVector.Set( m_WindDirection );
				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				CurrentMaterial.GetVariableByName( "GrassSize" ).AsScalar.Set( GRASS_TUFT_SIZE );
 				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "GrassTextures" ).AsResource.SetResource( m_GrassTextures.TextureView );

				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "DrawGrass" );

				Tech.GetPassByIndex( 0 ).Apply();
				m_Device.InputAssembler.SetVertexBuffers( 0, m_GrassQuad.Binding, m_GrassInstances.Binding );
				m_GrassQuad.DrawInstanced( GRASS_SIZE*GRASS_SIZE );
			}
#elif USE_METHOD1 || USE_METHOD2 || USE_METHOD4
			using ( m_MaterialGrass.UseLock() )
			{
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				CurrentMaterial.GetVariableByName( "GustTime" ).AsScalar.Set( m_GustTime );
				CurrentMaterial.GetVariableByName( "WindPosition0" ).AsVector.Set( m_WindPosition0 );
				CurrentMaterial.GetVariableByName( "WindPosition1" ).AsVector.Set( m_WindPosition1 );
				CurrentMaterial.GetVariableByName( "WindDirection" ).AsVector.Set( m_WindDirection );
				CurrentMaterial.GetVariableByName( "WindForce" ).AsScalar.Set( m_WindForce );
				CurrentMaterial.GetVariableByName( "GrassSize" ).AsScalar.Set( GRASS_TUFT_SIZE );
 				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "GrassTextures" ).AsResource.SetResource( m_GrassTextures.TextureView );
				CurrentMaterial.GetVariableByName( "MotionTexture" ).AsResource.SetResource( m_MotionTexture.TextureView );

				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "DrawGrass" );

				Tech.GetPassByIndex( 0 ).Apply();
#if !USE_METHOD4
				m_GrassInstances.Use();
				m_GrassInstances.Draw();
#else
				// Choose VB according to camera view
				double	Phi = Math.Atan2( m_Camera.Camera2World.M41, m_Camera.Camera2World.M43 ) + Math.PI / 8;
						Phi = 4.0 * ((Phi + 2.0 * Math.PI) % (2.0 * Math.PI)) / Math.PI;
				int		ViewIndex = (int) Math.Floor( Phi );	// BEST CASE TEST
//				int		ViewIndex = 7-(int) Math.Floor( Phi );	// WORST CASE TEST

				m_GrassInstancesCamera[ViewIndex].Use();
				m_GrassInstancesCamera[ViewIndex].Draw();
#endif
			}
#elif USE_METHOD3
			using ( m_MaterialGrass.UseLock() )
			{
				m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.PointList );

				CurrentMaterial.GetVariableByName( "WindPosition0" ).AsVector.Set( m_WindPosition0 );
				CurrentMaterial.GetVariableByName( "WindPosition1" ).AsVector.Set( m_WindPosition1 );
				CurrentMaterial.GetVariableByName( "WindDirection" ).AsVector.Set( m_WindDirection );
				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				CurrentMaterial.GetVariableByName( "GrassSize" ).AsScalar.Set( GRASS_TUFT_SIZE );
 				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "GrassTextures" ).AsResource.SetResource( m_GrassTextures.TextureView );
				CurrentMaterial.GetVariableByName( "GrassPositionTexture" ).AsResource.SetResource( m_GrassPositionTexture.TextureView );
				CurrentMaterial.GetVariableByName( "GrassColorTexture" ).AsResource.SetResource( m_GrassColorTexture.TextureView );

				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "DrawGrass" );

				Tech.GetPassByIndex( 0 ).Apply();
				m_GrassInstances.Use();
//				m_GrassInstances.DrawInstanced( GRASS_SIZE*GRASS_SIZE );
				m_GrassInstances.Draw();
			}
#endif
		}

		/// <summary>
		/// Creates a 3D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateNoiseTexture( int _NoiseIndex )
		{
			const int	NOISE_SIZE = 16;
//			const float	GLOBAL_SCALE = 2.0f;

			// Static offsets and scales for each noise texture
			Vector3[]	Offsets = new Vector3[]
			{
				new Vector3( 0.0f, 0.0f, 0.0f ),
				new Vector3( 0.0f, 0.0f, 0.0f ),
				new Vector3( 0.0f, 0.0f, 0.0f ),
				new Vector3( 0.0f, 0.0f, 0.0f ),
			};
			Vector3[]	Scales = new Vector3[]
			{
				new Vector3( 0.1234f, 0.097f, 0.15f ),
				new Vector3( 0.3f, 0.3f, 0.3f ),
				new Vector3( 0.3f, 0.3f, 0.3f ),
				new Vector3( 0.3f, 0.3f, 0.3f ),
			};

			// Build the volume filled with noise
			byte[][]	NoiseResources = new byte[4][]
			{
				Demo.Properties.Resources.packednoise_half_16cubed_mips_00,
				Demo.Properties.Resources.packednoise_half_16cubed_mips_01,
				Demo.Properties.Resources.packednoise_half_16cubed_mips_02,
				Demo.Properties.Resources.packednoise_half_16cubed_mips_03,
			};

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( NoiseResources[_NoiseIndex] );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			int	XS, YS, ZS, PS;
			XS = Reader.ReadInt32();
			YS = Reader.ReadInt32();
			ZS = Reader.ReadInt32();
			PS = Reader.ReadInt32();

			Half		Temp = new Half();
			float[,,]	Noise = new float[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];
			for ( int Z=0; Z < NOISE_SIZE; Z++ )
				for ( int Y=0; Y < NOISE_SIZE; Y++ )
					for ( int X=0; X < NOISE_SIZE; X++ )
					{
						Temp.RawValue = Reader.ReadUInt16();
						Reader.ReadUInt16();
						Reader.ReadUInt16();
						Reader.ReadUInt16();
						Noise[X,Y,Z] = (float) Temp;
					}
			Reader.Close();
			Reader.Dispose();

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_SIZE, NOISE_SIZE, NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{																			// (XYZ)
					_Color.X = Noise[_X,_Y,_Z];												// (000)
					_Color.Y = Noise[_X,(_Y+1) & (NOISE_SIZE-1),_Z];						// (010)
					_Color.Z = Noise[_X,_Y,(_Z+1) & (NOISE_SIZE-1)];						// (001)
					_Color.W = Noise[_X,(_Y+1) & (NOISE_SIZE-1),(_Z+1) & (NOISE_SIZE-1)];	// (011)

				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}

		#endregion
	}
}
