// Define this to use the hemispherical skydome which is more suitable than the spherical ones that show bad artefacts on vertices
#define USE_HEMISPHERICAL_SKYDOME

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
	/// Sky rendering
	/// </example>
	public class RenderTechniqueSky : RenderTechniqueBase
	{
		#region CONSTANTS

		protected const int		DOME_SUBDIVISIONS_COUNT_PHI = 40;
		protected const int		DOME_SUBDIVISIONS_COUNT_THETA = 40;

		#endregion

		#region FIELDS

		protected Nuaj.Cirrus.Atmosphere.SkySupport	m_SkySupport = null;

		protected Material<VS_P3N3>			m_Material = null;
		protected Primitive<VS_P3N3,int>	m_SkyDome = null;

		protected ITexture2D				m_NightSkyCubeMap = null;

		#endregion

		#region PROPERTIES

		public float			ScatteringAnisotropy	{ get { return m_SkySupport.ScatteringAnisotropy; } set { m_SkySupport.ScatteringAnisotropy = value; } }
		public float			DensityRayleigh			{ get { return m_SkySupport.DensityRayleigh; } set { m_SkySupport.DensityRayleigh = value; } }
		public float			DensityMie				{ get { return m_SkySupport.DensityMie; } set { m_SkySupport.DensityMie = value; } }
		public float			SunIntensity			{ get { return m_SkySupport.SunIntensity; } set { m_SkySupport.SunIntensity = value; } }
		public Vector3			SunDirection			{ get { return m_SkySupport.SunDirection; } set { m_SkySupport.SunDirection = value; } }
		public float			SunPhi					{ get { return m_SkySupport.SunPhi; } set { m_SkySupport.SunPhi = value; } }
		public float			SunTheta				{ get { return m_SkySupport.SunTheta; } set { m_SkySupport.SunTheta = value; } }
		public Vector3			SunColor				{ get { return m_SkySupport.SunColor; } }
		public Vector3			AmbientSkyColor			{ get { return m_SkySupport.SkyAmbientColor; } }
		public float			WorldUnit2Kilometers	{ get { return m_SkySupport.WorldUnit2Kilometer; } set { m_SkySupport.WorldUnit2Kilometer = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueSky( RendererSetup _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create the sky support that will help us render the sky
			// (this object publishes the shader interface necessary to use the Atmosphere/SkySupport.fx file)
			m_SkySupport = ToDispose( new Nuaj.Cirrus.Atmosphere.SkySupport( m_Device, "SkySupport" ) );

return;

			// Create our main material
			m_Material = ToDispose( new Material<VS_P3N3>( m_Device, "Sky Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/RenderSky.fx" ) ) );

			// Load the night sky cube map
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "./Media/CubeMaps/milkywaypan_brunier_2048.jpg" ) as System.Drawing.Bitmap )
				using ( ImageCube<PF_RGBA8> I = new ImageCube<PF_RGBA8>( m_Device, "NightSkyImage", B, ImageCube<PF_RGBA8>.FORMATTED_IMAGE_TYPE.CYLINDRICAL, 0, 1.0f ))
					m_NightSkyCubeMap = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "NightSkyCube", I ) );

			//////////////////////////////////////////////////////////////////////////
			// Create our sky dome
			BuildSkyDome();
		}

		public override void	Render( int _FrameToken )
		{
return;
			m_Device.AddProfileTask( this, "Emissive Pass", "Render Sky" );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

#if !USE_HEMISPHERICAL_SKYDOME
			// Build the skydome rotation matrix that will orient the skydome toward the Sun
			Matrix	SkyDomeRotation;

			float	Phi = m_SkySupport.SunPhi * (float) Math.PI / 180.0f;
			Vector3	X = new Vector3( (float) Math.Cos( Phi ), 0.0f, -(float) Math.Sin( Phi ) );
			SkyDomeRotation.M11 = X.X;	SkyDomeRotation.M12 = X.Y;	SkyDomeRotation.M13 = X.Z;	SkyDomeRotation.M14 = 0.0f;
			
			Vector3	Y = SunDirection;
			SkyDomeRotation.M21 = Y.X;	SkyDomeRotation.M22 = Y.Y;	SkyDomeRotation.M23 = Y.Z;	SkyDomeRotation.M24 = 0.0f;

			Vector3	Z = Vector3.Cross( X, Y );
			SkyDomeRotation.M31 = Z.X;	SkyDomeRotation.M32 = Z.Y;	SkyDomeRotation.M33 = Z.Z;	SkyDomeRotation.M34 = 0.0f;

			SkyDomeRotation.M41 = 0.0f;	SkyDomeRotation.M42 = 0.0f;	SkyDomeRotation.M43 = 0.0f;	SkyDomeRotation.M44 = 1.0f;
#endif
			// Render
			using ( m_Material.UseLock() )
			{
				CurrentMaterial.GetVariableByName( "BufferInvSize" ).AsVector.Set( m_Device.DefaultRenderTarget.InvSize2 );
#if !USE_HEMISPHERICAL_SKYDOME
				CurrentMaterial.GetVariableByName( "SkyDomeRotation" ).AsMatrix.SetMatrix( SkyDomeRotation );
#endif
				CurrentMaterial.GetVariableByName( "NightSkyCubeMap" ).AsResource.SetResource( m_NightSkyCubeMap );
				CurrentMaterial.ApplyPass( 0 );
				m_SkyDome.Render();
			}

			m_Renderer.SwapEmissiveBuffers();

			//////////////////////////////////////////////////////////////////////////
			// Setup the Sun's color into the main directional light
			m_Renderer.Sun.Color = new Vector4( m_SkySupport.SunColor, 0.0f );
		}

		#region SkyDome Building

#if !USE_HEMISPHERICAL_SKYDOME
		/// <summary>
		/// Builds a spherical sky dome with more resolution near the north pole
		/// This skydome rotates in the direction of the Sun and moves along with the camera
		/// </summary>
		protected void	BuildSkyDome()
		{
			VS_P3[]		Vertices = new VS_P3[1+DOME_SUBDIVISIONS_COUNT_PHI*DOME_SUBDIVISIONS_COUNT_THETA+1];

			int	VertexIndex = 0;
			Vertices[VertexIndex++] = new VS_P3() { Position=Vector3.UnitY };	// Top vertex

			for ( int Y=0; Y < DOME_SUBDIVISIONS_COUNT_THETA; Y++ )
			{
				float	Theta = (float) Math.PI * (float) Math.Pow( (float) (Y+1) / (DOME_SUBDIVISIONS_COUNT_THETA+1), 2.0f );

				float	fCosTheta = (float) Math.Cos( Theta );
				float	fSinTheta = (float) Math.Sin( Theta );

				for ( int X=0; X < DOME_SUBDIVISIONS_COUNT_PHI; X++ )
				{
					float	Phi = 2.0f * (float) Math.PI * X / DOME_SUBDIVISIONS_COUNT_PHI;
					Vertices[VertexIndex++] = new VS_P3() { Position=new Vector3( (float) (Math.Cos( Phi ) * fSinTheta), fCosTheta, (float) (Math.Sin( Phi ) * fSinTheta) ) };
				}
			}
			Vertices[VertexIndex++] = new VS_P3() { Position=-Vector3.UnitY };	// Bottom vertex

			// Build indices
			int[]	Indices = new int[3*DOME_SUBDIVISIONS_COUNT_PHI*(1+2*(DOME_SUBDIVISIONS_COUNT_THETA-1)+1)];

			int	IndexIndex = 0;
				// Top band (triangles)
			for ( int TriangleIndex=0; TriangleIndex < DOME_SUBDIVISIONS_COUNT_PHI; TriangleIndex++ )
			{
				Indices[IndexIndex++] = 0;
				Indices[IndexIndex++] = 1+TriangleIndex;
				Indices[IndexIndex++] = 1+(TriangleIndex+1) % DOME_SUBDIVISIONS_COUNT_PHI;
			}

				// Middle bands (quads)
			for ( int BandIndex=0; BandIndex < DOME_SUBDIVISIONS_COUNT_THETA-1; BandIndex++ )
			{
				int	BandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * BandIndex;
				int	NextBandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * (BandIndex+1);
				for ( int TriangleIndex=0; TriangleIndex < DOME_SUBDIVISIONS_COUNT_PHI; TriangleIndex++ )
				{
					int	NextTriangleIndex = (TriangleIndex+1) % DOME_SUBDIVISIONS_COUNT_PHI;

					Indices[IndexIndex++] = BandOffset+TriangleIndex;
					Indices[IndexIndex++] = NextBandOffset+TriangleIndex;
					Indices[IndexIndex++] = NextBandOffset+NextTriangleIndex;

					Indices[IndexIndex++] = BandOffset+TriangleIndex;
					Indices[IndexIndex++] = NextBandOffset+NextTriangleIndex;
					Indices[IndexIndex++] = BandOffset+NextTriangleIndex;
				}
			}

				// Bottom band (triangles)
			int	LastBandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * (DOME_SUBDIVISIONS_COUNT_THETA-1);
			for ( int TriangleIndex=0; TriangleIndex < DOME_SUBDIVISIONS_COUNT_PHI; TriangleIndex++ )
			{
				Indices[IndexIndex++] = VertexIndex-1;
				Indices[IndexIndex++] = LastBandOffset+TriangleIndex;
				Indices[IndexIndex++] = LastBandOffset+(TriangleIndex+1) % DOME_SUBDIVISIONS_COUNT_PHI;
			}

			m_SkyDome = ToDispose( new Primitive<VS_P3,int>( m_Device, "SkyDome", PrimitiveTopology.TriangleList, Vertices, Indices ) );
		}
#else
		/// <summary>
		/// Builds a hemispherical sky dome with more resolution near the horizon.
		/// This skydome is fixed in rotation but moves along with the camera
		/// </summary>
		protected void	BuildSkyDome()
		{
			VS_P3N3[]	Vertices = new VS_P3N3[1+DOME_SUBDIVISIONS_COUNT_PHI*(DOME_SUBDIVISIONS_COUNT_THETA+1)];

			int	VertexIndex = 0;
			Vertices[VertexIndex++] = new VS_P3N3() { Position=Vector3.UnitY, Normal=Vector3.UnitY };	// Top vertex

			for ( int Y=0; Y < DOME_SUBDIVISIONS_COUNT_THETA; Y++ )
			{
				float	Theta = 0.5f * (float) Math.PI * (float) Math.Pow( (float) (Y+1) / DOME_SUBDIVISIONS_COUNT_THETA, 0.5f );

				float	fCosTheta = (float) Math.Cos( Theta );
				float	fSinTheta = (float) Math.Sin( Theta );

				for ( int X=0; X < DOME_SUBDIVISIONS_COUNT_PHI; X++ )
				{
					float	Phi = 2.0f * (float) Math.PI * X / DOME_SUBDIVISIONS_COUNT_PHI;
					Vector3	View = new Vector3( (float) (Math.Cos( Phi ) * fSinTheta), fCosTheta, (float) (Math.Sin( Phi ) * fSinTheta) );
					Vertices[VertexIndex++] = new VS_P3N3() { Position=View, Normal=View };
				}
			}
				// Bottom vertices => A copy of the horizon vertices all brought back to a point downward
			for ( int X=0; X < DOME_SUBDIVISIONS_COUNT_PHI; X++ )
			{
				float	Phi = 2.0f * (float) Math.PI * X / DOME_SUBDIVISIONS_COUNT_PHI;
				Vector3	View = new Vector3( (float) Math.Cos( Phi ), 0.0f, (float) Math.Sin( Phi ) );
				Vertices[VertexIndex++] = new VS_P3N3() { Position=-0.1f * Vector3.UnitY, Normal=View };
			}

			// Build indices
			int[]	Indices = new int[3*DOME_SUBDIVISIONS_COUNT_PHI*(1+2*DOME_SUBDIVISIONS_COUNT_THETA-1)];

			int	IndexIndex = 0;
				// Top band (triangles)
			for ( int TriangleIndex=0; TriangleIndex < DOME_SUBDIVISIONS_COUNT_PHI; TriangleIndex++ )
			{
				Indices[IndexIndex++] = 0;
				Indices[IndexIndex++] = 1+TriangleIndex;
				Indices[IndexIndex++] = 1+(TriangleIndex+1) % DOME_SUBDIVISIONS_COUNT_PHI;
			}

				// Middle bands (quads)
			for ( int BandIndex=0; BandIndex < DOME_SUBDIVISIONS_COUNT_THETA-1; BandIndex++ )
			{
				int	BandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * BandIndex;
				int	NextBandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * (BandIndex+1);
				for ( int TriangleIndex=0; TriangleIndex < DOME_SUBDIVISIONS_COUNT_PHI; TriangleIndex++ )
				{
					int	NextTriangleIndex = (TriangleIndex+1) % DOME_SUBDIVISIONS_COUNT_PHI;

					Indices[IndexIndex++] = BandOffset+TriangleIndex;
					Indices[IndexIndex++] = NextBandOffset+TriangleIndex;
					Indices[IndexIndex++] = NextBandOffset+NextTriangleIndex;

					Indices[IndexIndex++] = BandOffset+TriangleIndex;
					Indices[IndexIndex++] = NextBandOffset+NextTriangleIndex;
					Indices[IndexIndex++] = BandOffset+NextTriangleIndex;
				}
			}

				// Bottom band (triangles)
			int	PreviousLastBandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * (DOME_SUBDIVISIONS_COUNT_THETA-1);
			int	LastBandOffset = 1 + DOME_SUBDIVISIONS_COUNT_PHI * DOME_SUBDIVISIONS_COUNT_THETA;
			for ( int TriangleIndex=0; TriangleIndex < DOME_SUBDIVISIONS_COUNT_PHI; TriangleIndex++ )
			{
				int	NextTriangleIndex = (TriangleIndex+1) % DOME_SUBDIVISIONS_COUNT_PHI;

				Indices[IndexIndex++] = LastBandOffset+TriangleIndex;
				Indices[IndexIndex++] = PreviousLastBandOffset+TriangleIndex;
				Indices[IndexIndex++] = PreviousLastBandOffset+NextTriangleIndex;
			}

			m_SkyDome = ToDispose( new Primitive<VS_P3N3,int>( m_Device, "SkyDome", PrimitiveTopology.TriangleList, Vertices, Indices ) );
		}
#endif

		#endregion

		#endregion
	}
}
