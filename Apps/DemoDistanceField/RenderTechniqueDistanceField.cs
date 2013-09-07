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
	/// Distance field ray-marching
	/// </example>
	public class RenderTechniqueDistanceField : RenderTechnique
	{
		#region CONSTANTS

		#endregion

		#region NESTED TYPES

		#endregion

		#region FIELDS

		protected Camera					m_Camera = null;

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_Pt4V3T2>		m_MaterialRayTracer = null;

		// Screen quad for ray-tracer post-process
		protected Helpers.ScreenQuad		m_ScreenQuad = null;

		// Wall texture
		protected Texture2D<PF_RGBA8>		m_WallTextures = null;
		protected Texture2D<PF_RGBA8>		m_SpectrumTexture = null;

		// The 16x16x16 noise textures
		protected Texture3D<PF_RGBA16F>[]	m_NoiseTextures = new Texture3D<PF_RGBA16F>[4];

		// Parameters for animation
		protected float						m_Time = 0.0f;

		#endregion

		#region PROPERTIES

		public Camera						Camera				{ get { return m_Camera; } set { m_Camera = value; } }
		public float						Time				{ get { return m_Time; } set { m_Time = value; } }

		#endregion

		#region METHODS

		public unsafe	RenderTechniqueDistanceField( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Create our main materials
			m_MaterialRayTracer = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Ray-Tracer Post-Process Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/DistanceField/RayTracer.fx" ) ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the noise textures
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = CreateNoiseTexture( NoiseIndex );

			//////////////////////////////////////////////////////////////////////////
			// Load wall textures
			System.Drawing.Bitmap BitmapWall0 = System.Drawing.Bitmap.FromFile( "Media/Walls/Wall_Texture_by_shadowh3_512.jpg" ) as System.Drawing.Bitmap;
			System.Drawing.Bitmap BitmapWall1 = System.Drawing.Bitmap.FromFile( "Media/Walls/concrete_by_shadowh3.jpg" ) as System.Drawing.Bitmap;
			Image<PF_RGBA8>	ImageWall0 = new Image<PF_RGBA8>( m_Device, "Wall Image", BitmapWall0, true, 0, 1.0f );
			Image<PF_RGBA8>	ImageWall1 = new Image<PF_RGBA8>( m_Device, "Floor Image", BitmapWall1, true, 0, 1.0f );
			
			m_WallTextures = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Wall Textures", new Image<PF_RGBA8>[] { ImageWall0, ImageWall1 } ) );

			ImageWall0.Dispose();
			ImageWall1.Dispose();
			BitmapWall0.Dispose();
			BitmapWall1.Dispose();

			m_SpectrumTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "False Colors", new System.IO.FileInfo( "Media/FalseColorsSpectrum.png" ), 1, 1.0f ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the screen quad for ray-tracing post process
			m_ScreenQuad = ToDispose( new Helpers.ScreenQuad( m_Device, "Screen Quad" ) );
		}

		public override void  Render(int _FrameToken)
		{
			//////////////////////////////////////////////////////////////////////////
			// Render ray-traced scene as a post-process
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialRayTracer.UseLock() )
			{
				m_Device.SetDefaultRenderTarget();

				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				CurrentMaterial.GetVariableByName( "AspectRatio" ).AsVector.Set( new Vector3( 1.0f, 1.0f / m_Camera.AspectRatio, 1.0f ) );
				float	StartEpsilon = (float) Math.Tan( 0.5 * m_Camera.PerspectiveFOV ) / m_Device.DefaultRenderTarget.Height;
				CurrentMaterial.GetVariableByName( "StartEpsilon" ).AsScalar.Set( StartEpsilon );
 				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
 				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "WallTextures" ).AsResource.SetResource( m_WallTextures.TextureView );
 				CurrentMaterial.GetVariableByName( "SpectrumTexture" ).AsResource.SetResource( m_SpectrumTexture.TextureView );

 				// ---------------------------------------------------------------------------
				// 5.3] Final display as post-process
				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "RayTracer" );

				Tech.GetPassByIndex( 0 ).Apply();
				m_ScreenQuad.Render();
			}
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
