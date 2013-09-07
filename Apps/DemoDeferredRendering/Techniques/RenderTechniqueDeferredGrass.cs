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
	/// Deferred Rendering Technique for Grass instancing & Trees rendering
	/// </example>
	public class DeferredRenderingGrass : DeferredRenderTechnique
	{
		#region CONSTANTS

		protected const int		GRASS_SIZE = 128;
		protected const float	GRASS_LAND_SIZE = 16.0f;	// The space to cover
		protected const float	GRASS_TUFT_SIZE = 0.15f;	// Size of a grass tuft

//		protected const int		TREE_LEVELS = 4;			// Amount of recursion levels for trees

		#endregion

		#region NESTED TYPES

		// This is the VS containing instance data for each grass tuft
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_GRASS_INSTANCE
		{
			[Semantic( SemanticAttribute.POSITION )]
			public Vector3	InstancePosition;
			[Semantic( SemanticAttribute.COLOR )]
			public Vector3	Color;
		}

// 		// This is the VS containing data for each tree segment
// 		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
// 		protected struct VS_TREE_SEGMENT
// 		{
// 			[Semantic( SemanticAttribute.POSITION )]
// 			public Vector3	BranchPosition;
// 			[Semantic( SemanticAttribute.COLOR )]
// 			public Vector3	Color;
// 		}

		#endregion

		#region FIELDS

		protected Material<VS_GRASS_INSTANCE>	m_Material = null;

		//////////////////////////////////////////////////////////////////////////
		// Grass blades primitives
		protected VertexBuffer<VS_GRASS_INSTANCE>	m_GrassInstances = null;

		// Grass texture array
		protected Texture2D<PF_RGBA8>		m_GrassTexture = null;

		// Motion texture
		protected Texture2D<PF_RG32F>		m_MotionTexture = null;

// 		//////////////////////////////////////////////////////////////////////////
// 		// Tree primitive
// 		protected Primitive<VS_TREE_SEGMENT,int>	m_TreePrimitive = null;
// 
// 		// Motion textures
// 		protected Texture2D<PF_RGBA16F>[]	m_TreeMotionTextures = new Texture2D<PF_RGBA16F>[TREE_LEVELS];

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected bool						m_bGust = false;
		protected float						m_Time = 0.0f;
		protected float						m_GustTime = 0.0f;
		protected float						m_LastTime = 0.0f;
		protected float						m_WindForce = 1.0f;
		protected Vector3					m_WindDirection = new Vector3( 1.0f, 0.0f, 0.0f );

		#endregion

		#region PROPERTIES

		public float						Time				{ get { return m_Time; } set { m_LastTime = m_Time; m_Time = value; } }
		[System.ComponentModel.Browsable( false )]
		public Vector3						WindDirection		{ get { return m_WindDirection; } set { m_WindDirection = value; } }
		public float						WindForce			{ get { return m_WindForce; } set { m_WindForce = value; } }
		public bool							Gust				{ get { return m_bGust; } set { m_bGust = value; } }

		#endregion

		#region METHODS

		public	DeferredRenderingGrass( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_GRASS_INSTANCE>( m_Device, "Grass Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/RenderMRTGrass.fx" ) ) );

			//////////////////////////////////////////////////////////////////////////
			// Load grass texture
			m_GrassTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Grass Texture", new System.IO.FileInfo( "Media/Grass/grassWalpha256.png" ), 0, 1.0f ) );

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
			Random	RNG = new Random( 1 );
			VS_GRASS_INSTANCE[]	GrassInstances = new VS_GRASS_INSTANCE[GRASS_SIZE*GRASS_SIZE];
			for ( int Y=0; Y < GRASS_SIZE; Y++ )
			{
				for ( int X=0; X < GRASS_SIZE; X++ )
				{
					float	fX = GRASS_LAND_SIZE * ((X + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
					float	fZ = GRASS_LAND_SIZE * ((Y + (float) RNG.NextDouble()) / GRASS_SIZE - 0.5f);
					GrassInstances[GRASS_SIZE*Y+X].InstancePosition = new Vector3( fX, 0.0f, fZ );
//					GrassInstances[GRASS_SIZE*Y+X].Color = new Vector3( 0.5f, 0.2f, 0.0f );
					GrassInstances[GRASS_SIZE*Y+X].Color = new Vector3( (float) RNG.NextDouble(), (float) RNG.NextDouble(), (float) RNG.NextDouble() );
				}
			}
			m_GrassInstances = ToDispose( new VertexBuffer<VS_GRASS_INSTANCE>( m_Device, "GrassInstances", GrassInstances ) );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Render Grass" );

			//////////////////////////////////////////////////////////////////////////
			// Update wind
			float	fDeltaTime = m_Time - m_LastTime;
			m_GustTime += 4.0f * fDeltaTime * m_WindForce;
			if ( m_bGust )
			{	// Restart gust of wind
				m_GustTime = 0.0f;
				m_bGust = false;
			}

			//////////////////////////////////////////////////////////////////////////
			// Render grass scene
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
//			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ALPHA2COVERAGE );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				CurrentMaterial.GetVariableByName( "GustTime" ).AsScalar.Set( m_GustTime );
				CurrentMaterial.GetVariableByName( "WindDirection" ).AsVector.Set( m_WindDirection );
				CurrentMaterial.GetVariableByName( "WindForce" ).AsScalar.Set( m_WindForce );
				CurrentMaterial.GetVariableByName( "GrassSize" ).AsScalar.Set( GRASS_TUFT_SIZE );
				CurrentMaterial.GetVariableByName( "GrassTexture" ).AsResource.SetResource( m_GrassTexture.TextureView );
				CurrentMaterial.GetVariableByName( "MotionTexture" ).AsResource.SetResource( m_MotionTexture.TextureView );

				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "DrawGrass" );

				Tech.GetPassByIndex( 0 ).Apply();
				m_GrassInstances.Use();
				m_GrassInstances.Draw();
			}
		}

		#endregion
	}
}
