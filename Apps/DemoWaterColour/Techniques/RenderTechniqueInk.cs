﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Caustics effect
	/// </example>
	public class RenderTechniqueCaustics2 : RenderTechniqueBase, IGeometryWriter<VS_P3T2,int>, IDepthPassRenderable
	{
		#region CONSTANTS

		protected const int		GEODESIC_SPHERE_SUBDIVISIONS = 6;	// Amount of sphere subdivisions (triangles count = 20 * 4^SUBDIVISIONS)
		protected const int		GEODESIC_SPHERE_SUBDIVISIONS2 = 3;	// Amount of sphere subdivisions for the deformed sphere (triangles count = 20 * 4^SUBDIVISIONS)
		protected const int		GEODESIC_SPHERE_SUBDIVISIONS3 = 2;	// Amount of sphere subdivisions for the "lo-res" deformed sphere (triangles count = 20 * 4^SUBDIVISIONS)
		protected const int		RENDER_TEXTURE_SIZE = 512;			// Size of the caustics texture

		#endregion

		#region NESTED TYPES

		class ProjectedSphereWriter : IGeometryWriter<VS_P3,int>
		{
			#region IGeometryWriter<VS_P3N3T2,int> Members

			public void WriteVertexData( ref VS_P3 _Vertex, Vector3 _Position, Vector3 _Normal, Vector3 _Tangent, Vector3 _BiTangent, Vector3 _UVW, Color4 _Color )
			{
				_Vertex.Position = _Position;
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
		}

		class CubeWriter : IGeometryWriter<VS_P3N3T2,int>
		{
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
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_P3T2>			m_MaterialBuildCaustics = null;
		protected Material<VS_P3N3T2>		m_MaterialDisplayCaustics = null;
		protected Material<VS_P3T2>			m_MaterialDisplaySphere = null;
		protected Material<VS_P3T2>			m_MaterialDisplayLensFlare = null;
		protected Material<VS_Pt4V3T2>		m_MaterialLensFlarePostProcess = null;

		// The projection sphere that will get deformed and rendered to the caustics texture
		protected Primitive<VS_P3,int>		m_CausticsProjectionSphere = null;

		// The deformed sphere that will actually be displayed
		protected Primitive<VS_P3T2,int>	m_DeformedSphere = null;
		protected Primitive<VS_P3T2,int>	m_LensFlareSphere = null;	// Same but used for the lens-flare

		// Screen quad for lens-flare post-process
		protected Helpers.ScreenQuad		m_LensFlareQuad = null;

		// The render cube
		protected Primitive<VS_P3N3T2,int>	m_CausticsCube = null;

		// The texture to render caustics to
		protected RenderTarget<PF_R16F>		m_CausticsTexture = null;

		// The textures to render the lens flare to
		protected RenderTarget<PF_RGBA8>[]	m_LensFlareTextures = new RenderTarget<PF_RGBA8>[2];
		protected DepthStencil<PF_D32>		m_LensFlareDepthStencil = null;

		// Wall texture
		protected Texture2D<PF_RGBA8>		m_WallTextures = null;

		// Parameters for animation
		protected Vector3					m_LightPosition = new Vector3( 0.5f, 0.5f, -0.5f );
		protected float						m_LightIntensity = 1.0f;
		protected float						m_LightRadius = 0.1f;
		protected Vector3					m_SpherePosition = new Vector3( 0, 0, 0 );
		protected float						m_SphereRadius = 0.2f;
		protected Matrix					m_Local2World = Matrix.Identity;

		#endregion

		#region PROPERTIES

		public Vector3						LightPosition		{ get { return m_LightPosition; } set { m_LightPosition = value; } }
		public float						LightRadius			{ get { return m_LightRadius; } set { m_LightRadius = value; } }
		public float						LightIntensity		{ get { return m_LightIntensity; } set { m_LightIntensity = value; } }
		public Vector3						SpherePosition		{ get { return m_SpherePosition; } set { m_SpherePosition = value; } }
		public float						SphereRadius		{ get { return m_SphereRadius; } set { m_SphereRadius = value; } }
		public Matrix						Local2World			{ get { return m_Local2World; } set { m_Local2World = value; } }

		#endregion

		#region METHODS

		public	RenderTechniqueCaustics2( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_MaterialBuildCaustics = m_Renderer.LoadMaterial<VS_P3T2>( "Caustics Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/BuildCaustics2.fx" ) );
			m_MaterialDisplayCaustics = m_Renderer.LoadMaterial<VS_P3N3T2>( "Display Caustics Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/DisplayCaustics2.fx" ) );
			m_MaterialDisplaySphere = m_Renderer.LoadMaterial<VS_P3T2>( "Display Sphere Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/DisplaySphere.fx" ) );
			m_MaterialDisplayLensFlare = m_Renderer.LoadMaterial<VS_P3T2>( "Display LensFlare Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/DisplayLensFlare.fx" ) );
			m_MaterialLensFlarePostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "LensFlare Post-Process Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/LensFlarePostProcess.fx" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the caustics texture array (6 cube faces)
			m_CausticsTexture = ToDispose( new RenderTarget<PF_R16F>( Device, "Caustics Texture", RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 1, 6, 1 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the 2 lens flare textures
			m_LensFlareTextures[0] = ToDispose( new RenderTarget<PF_RGBA8>( Device, "LensFlare Texture 0", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, 1 ) );
			m_LensFlareTextures[1] = ToDispose( new RenderTarget<PF_RGBA8>( Device, "LensFlare Texture 1", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, 1 ) );
			m_LensFlareDepthStencil = ToDispose( new DepthStencil<PF_D32>( Device, "LensFlare DepthStencil", Device.DefaultRenderTarget.Width / 4, Device.DefaultRenderTarget.Height / 4, false ) );


			//////////////////////////////////////////////////////////////////////////
			// Load wall textures
			m_WallTextures = m_Renderer.TextureLoader.LoadTexture<PF_RGBA8>( "Wall Textures", new System.IO.FileInfo( "Media/Walls/Wall_Texture_by_shadowh3_512.jpg" ), new System.IO.FileInfo( "Media/Walls/concrete_by_shadowh3.jpg" ) );

// 			System.Drawing.Bitmap BitmapWall0 = System.Drawing.Bitmap.FromFile( "Media/Walls/Wall_Texture_by_shadowh3_512.jpg" ) as System.Drawing.Bitmap;
// 			System.Drawing.Bitmap BitmapWall1 = System.Drawing.Bitmap.FromFile( "Media/Walls/concrete_by_shadowh3.jpg" ) as System.Drawing.Bitmap;
// 			Image<PF_RGBA8>	ImageWall0 = new Image<PF_RGBA8>( m_Device, "Wall Image", BitmapWall0, true, 0 );
// 			Image<PF_RGBA8>	ImageWall1 = new Image<PF_RGBA8>( m_Device, "Floor Image", BitmapWall1, true, 0 );
// 			
// 			m_WallTextures = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Wall Textures", new Image<PF_RGBA8>[] { ImageWall0, ImageWall1 } ) );
// 
// 			ImageWall0.Dispose();
// 			ImageWall1.Dispose();
// 			BitmapWall0.Dispose();
// 			BitmapWall1.Dispose();
// 
			//////////////////////////////////////////////////////////////////////////
			// Create the caustics projection sphere that will be projected onto the cube faces
			m_CausticsProjectionSphere = ToDispose( Helpers.GeodesicSphere<VS_P3,int>.Build( m_Device, "Caustics Projection Sphere", 
				Helpers.GEODESIC_SPHERE_BASE_SHAPE.ICOSAHEDRON,
				GEODESIC_SPHERE_SUBDIVISIONS,
				Helpers.GeometryMapperSpherical.DEFAULT,	// Default spherical mapping
//				new Helpers.GeometryMapperCube( false ),	// Cube mapping
				new ProjectedSphereWriter(), null ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the deformed sphere
			m_DeformedSphere = ToDispose( Helpers.GeodesicSphere<VS_P3T2,int>.Build( m_Device, "Deformed Sphere", 
				Helpers.GEODESIC_SPHERE_BASE_SHAPE.ICOSAHEDRON,
				GEODESIC_SPHERE_SUBDIVISIONS2,
				Helpers.GeometryMapperSpherical.DEFAULT,	// Default spherical mapping
//				new Helpers.GeometryMapperCube( false ),	// Cube mapping
				this, null ) );

			m_LensFlareSphere = ToDispose( Helpers.GeodesicSphere<VS_P3T2,int>.Build( m_Device, "Lens-Flare Sphere", 
				Helpers.GEODESIC_SPHERE_BASE_SHAPE.ICOSAHEDRON,
				GEODESIC_SPHERE_SUBDIVISIONS3,
				Helpers.GeometryMapperSpherical.DEFAULT,	// Default spherical mapping
				this, null ) );

			m_LensFlareQuad = ToDispose( new Helpers.ScreenQuad( m_Device, "Lens-Flare Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the caustics cube primitive used to render the caustics
			m_CausticsCube = ToDispose( Helpers.Cube<VS_P3N3T2,int>.Build( m_Device, "Caustics Cube", new Vector2( 1.0f, 1.0f ), new CubeWriter(), null ) );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			//////////////////////////////////////////////////////////////////////////
			// 0] Build some rotation matrices for noise octaves
			float			fTime = 1.0f * m_Renderer.Time;
			float			Angle0 = 0.02f * fTime * 2.0f * (float) Math.PI;
//			float			Angle0 = 0.0f;
			WMath.Vector	Axis0 = new WMath.Vector( 1, 1, 1 ).Normalize();
			float			Angle1 = 0.03f * fTime * 2.0f * (float) Math.PI;
//			float			Angle1 = 0.0f;
			WMath.Vector	Axis1 = new WMath.Vector( 0.5f, 2, 1 ).Normalize();
			float			Angle2 = 0.04f * fTime * 2.0f * (float) Math.PI;
//			float			Angle2 = 0.0f;
			WMath.Vector	Axis2 = new WMath.Vector( -1.0f, 0.5f, 2 ).Normalize();

			WMath.Matrix4x4	Temp = (WMath.Matrix4x4) (WMath.Quat) new WMath.AngleAxis( Angle0, Axis0 );
			Matrix	BumpRotation0 = new Matrix();
			BumpRotation0.M11 = Temp[0,0];
			BumpRotation0.M12 = Temp[0,1];
			BumpRotation0.M13 = Temp[0,2];
			BumpRotation0.M21 = Temp[1,0];
			BumpRotation0.M22 = Temp[1,1];
			BumpRotation0.M23 = Temp[1,2];
			BumpRotation0.M31 = Temp[2,0];
			BumpRotation0.M32 = Temp[2,1];
			BumpRotation0.M33 = Temp[2,2];

			Temp = (WMath.Matrix4x4) (WMath.Quat) new WMath.AngleAxis( Angle1, Axis1 );
			Matrix	BumpRotation1 = new Matrix();
			BumpRotation1.M11 = Temp[0,0];
			BumpRotation1.M12 = Temp[0,1];
			BumpRotation1.M13 = Temp[0,2];
			BumpRotation1.M21 = Temp[1,0];
			BumpRotation1.M22 = Temp[1,1];
			BumpRotation1.M23 = Temp[1,2];
			BumpRotation1.M31 = Temp[2,0];
			BumpRotation1.M32 = Temp[2,1];
			BumpRotation1.M33 = Temp[2,2];

			Temp = (WMath.Matrix4x4) (WMath.Quat) new WMath.AngleAxis( Angle2, Axis2 );
			Matrix	BumpRotation2 = new Matrix();
			BumpRotation2.M11 = Temp[0,0];
			BumpRotation2.M12 = Temp[0,1];
			BumpRotation2.M13 = Temp[0,2];
			BumpRotation2.M21 = Temp[1,0];
			BumpRotation2.M22 = Temp[1,1];
			BumpRotation2.M23 = Temp[1,2];
			BumpRotation2.M31 = Temp[2,0];
			BumpRotation2.M32 = Temp[2,1];
			BumpRotation2.M33 = Temp[2,2];

			//////////////////////////////////////////////////////////////////////////
			// 1] Build the caustics from the hexagonal grid
  			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );
#if true
			using ( m_MaterialBuildCaustics.UseLock() )
			{
				m_Device.ClearRenderTarget( m_CausticsTexture, new Color4( 0, 0, 0, 0 ) );
 				m_Device.SetRenderTarget( m_CausticsTexture );
 				m_Device.SetViewport( 0, 0, RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 0.0f, 1.0f );

				int	TrianglesCount = 20 * (int) Math.Pow( 4, GEODESIC_SPHERE_SUBDIVISIONS );	// Icosahedron based
//				int	TrianglesCount = 2*6 * (int) Math.Pow( 4, GEODESIC_SPHERE_SUBDIVISIONS );		// Cube based

 				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Renderer.Time );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave0" ).AsMatrix.SetMatrix( BumpRotation0 );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave1" ).AsMatrix.SetMatrix( BumpRotation1 );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave2" ).AsMatrix.SetMatrix( BumpRotation2 );
				CurrentMaterial.GetVariableByName( "TriangleNominalArea" ).AsScalar.Set( 4.0f * (float) Math.PI / TrianglesCount );
				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightIntensity" ).AsScalar.Set( m_LightIntensity );
				CurrentMaterial.GetVariableByName( "SpherePosition" ).AsVector.Set( m_SpherePosition );
				CurrentMaterial.GetVariableByName( "SphereRadius" ).AsScalar.Set( m_SphereRadius );

				EffectPass	Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				Pass.Apply();

				// Render the caustics projection sphere
				m_CausticsProjectionSphere.RenderOverride();
			}

			//////////////////////////////////////////////////////////////////////////
			// 2] Render caustics
  			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_FRONT );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST_OR_EQUAL );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialDisplayCaustics.UseLock() )
			{
				m_Renderer.SetDefaultRenderTarget();

				CurrentMaterial.GetVariableByName( "CausticsTexture" ).AsResource.SetResource( m_CausticsTexture.TextureView );
				CurrentMaterial.GetVariableByName( "WallTextures" ).AsResource.SetResource( m_WallTextures.TextureView );
				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightIntensity" ).AsScalar.Set( m_LightIntensity );
				CurrentMaterial.GetVariableByName( "SpherePosition" ).AsVector.Set( m_SpherePosition );
				CurrentMaterial.GetVariableByName( "SphereRadius" ).AsScalar.Set( m_SphereRadius );
				CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix.SetMatrix( m_Local2World );

				EffectPass	Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				Pass.Apply();
				m_CausticsCube.RenderOverride();
//				m_CausticsProjectionSphere.RenderOverride();
			}

			//////////////////////////////////////////////////////////////////////////
			// 3] Render deformed sphere
  			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST_OR_EQUAL );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialDisplaySphere.UseLock() )
			{
 				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Renderer.Time );
				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "SpherePosition" ).AsVector.Set( m_SpherePosition );
				CurrentMaterial.GetVariableByName( "SphereRadius" ).AsScalar.Set( m_SphereRadius);
//  				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave0" ).AsMatrix.SetMatrix( BumpRotation0 );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave1" ).AsMatrix.SetMatrix( BumpRotation1 );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave2" ).AsMatrix.SetMatrix( BumpRotation2 );
				CurrentMaterial.GetVariableByName( "CausticsTexture" ).AsResource.SetResource( m_CausticsTexture.TextureView );
				CurrentMaterial.GetVariableByName( "WallTextures" ).AsResource.SetResource( m_WallTextures.TextureView );

				EffectPass	Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				Pass.Apply();
				m_DeformedSphere.RenderOverride();
			}
#endif
			//////////////////////////////////////////////////////////////////////////
			// 4] Draw lens-flare
  			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialDisplayLensFlare.UseLock() )
			{
				m_Device.SetRenderTarget( m_LensFlareTextures[0], m_LensFlareDepthStencil );
				m_Device.SetViewport( 0, 0, m_LensFlareTextures[0].Width, m_LensFlareTextures[0].Height, 0.0f, 1.0f );
				m_Device.ClearRenderTarget( m_LensFlareTextures[0], new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
				m_Device.ClearRenderTarget( m_LensFlareTextures[1], new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
				m_Device.ClearDepthStencil( m_LensFlareDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

 				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Renderer.Time );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2].TextureView );
//  				CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3].TextureView );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave0" ).AsMatrix.SetMatrix( BumpRotation0 );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave1" ).AsMatrix.SetMatrix( BumpRotation1 );
				CurrentMaterial.GetVariableByName( "BumpRotationOctave2" ).AsMatrix.SetMatrix( BumpRotation2 );
				CurrentMaterial.GetVariableByName( "LightIntensity" ).AsScalar.Set( m_LightIntensity );

				// 4.1] Draw lens-flare sphere
				CurrentMaterial.GetVariableByName( "SpherePosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "SphereRadius" ).AsScalar.Set( m_LightRadius );

				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "DrawLensFlare" );
				Tech.GetPassByIndex( 0 ).Apply();
				m_LensFlareSphere.RenderOverride();

				// 4.2] Draw deformed sphere to potentially deform the lens-flare sphere
				CurrentMaterial.GetVariableByName( "SpherePosition" ).AsVector.Set( m_SpherePosition );
				CurrentMaterial.GetVariableByName( "SphereRadius" ).AsScalar.Set( m_SphereRadius );
				CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector.Set( m_LightPosition );
				CurrentMaterial.GetVariableByName( "LightRadius" ).AsScalar.Set( m_LightRadius );

				Tech = CurrentMaterial.GetTechniqueByName( "DrawDeformedSphere" );
				Tech.GetPassByIndex( 0 ).Apply();
				m_LensFlareSphere.RenderOverride();
			}

			//////////////////////////////////////////////////////////////////////////
			// 5] Render lens-flare as a post-process
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );

			using ( m_MaterialLensFlarePostProcess.UseLock() )
			{
				VariableResource	vPreviousPass = CurrentMaterial.GetVariableByName( "RadialPreviousPassTexture" ).AsResource;

				// Project light position in 2D for radial blur
				Vector3	LightPosCamera = Vector3.TransformCoordinate( m_LightPosition, m_Renderer.Camera.World2Camera );

				float	IFOVY = 1.0f / (float) Math.Tan( 0.5 * m_Renderer.Camera.PerspectiveFOV );
				float	IFOVX = IFOVY / m_Renderer.Camera.AspectRatio;
				float	IDepth = 1.0f / LightPosCamera.Z;

				Vector2	LightPosition2D = new Vector2(
					0.5f * (1.0f + LightPosCamera.X * IFOVX * IDepth),
					0.5f * (1.0f + LightPosCamera.Y * IFOVY * IDepth)
					);

				// ---------------------------------------------------------------------------
				// 5.1] Perform a little blur
 				EffectTechnique	Tech = CurrentMaterial.GetTechniqueByName( "Blur" );
				m_Device.SetRenderTarget( m_LensFlareTextures[1] );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
				vPreviousPass.SetResource( m_LensFlareTextures[0].TextureView );

				CurrentMaterial.GetVariableByName( "InvTextureSize" ).AsVector.Set( new Vector3( 1.0f / m_LensFlareTextures[0].Width, 1.0f / m_LensFlareTextures[0].Height, 0.0f ) );

				Tech.GetPassByIndex( 0 ).Apply();
				m_LensFlareQuad.Render();

 				// ---------------------------------------------------------------------------
				// 5.2] Perform radial blur
 				Tech = CurrentMaterial.GetTechniqueByName( "RadialBlur" );
				m_Device.SetRenderTarget( m_LensFlareTextures[0] );
				vPreviousPass.SetResource( m_LensFlareTextures[1].TextureView );

				CurrentMaterial.GetVariableByName( "RadialCenter" ).AsVector.Set( LightPosition2D );
				CurrentMaterial.GetVariableByName( "InvAspectRatio" ).AsVector.Set( new Vector2( 1.0f / m_Renderer.Camera.AspectRatio, 1.0f ) );

				Tech.GetPassByIndex( 0 ).Apply();
				m_LensFlareQuad.Render();

 				// ---------------------------------------------------------------------------
				// 5.3] Final display as post-process
				Tech = CurrentMaterial.GetTechniqueByName( "DrawPostProcess" );
				m_Renderer.SetDefaultRenderTarget();
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );
				vPreviousPass.SetResource( m_LensFlareTextures[0].TextureView );

				Tech.GetPassByIndex( 0 ).Apply();
				m_LensFlareQuad.Render();
			}
  		}

		#region IGeometryWriter<VS_P3T2,int> Members

		public void WriteVertexData( ref VS_P3T2 _Vertex, Vector3 _Position, Vector3 _Normal, Vector3 _Tangent, Vector3 _BiTangent, Vector3 _UVW, Color4 _Color )
		{
			_Vertex.Position = _Position;
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

		#region IDepthPassRenderable Members

		public void RenderDepthPass( int _FrameToken, EffectPass _Pass, VariableMatrix _vLocal2World )
		{
			if ( !m_bEnabled )
				return;

			_vLocal2World.SetMatrix( m_Local2World );
			_Pass.Apply();
			m_CausticsCube.RenderOverride();
		}

		#endregion

		#endregion
	}
}
