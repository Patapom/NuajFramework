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
	/// Stoopid floohid
	/// This is an adptation of the famous "water effect" first shown in the Iguana demo named "Heartquake" in 1994
	///	except this time it's in 3D.
	///	The fluid is computed in a 3D texture then a mesh is created using marching cubes from density values read
	///	from the fluid texture
	///	
	/// That's funny but not very convincing, I still have a bug where all movements are biased toward the positive Z axis for some reason...
	/// </example>
	public class RenderTechniqueFluid : RenderTechnique
	{
		#region CONSTANTS

		protected const float	VOXEL_BLOCK_SIZE = 4.0f;			// The size of a block of voxels in WORLD units

		// These determine the size of the volume texture to compute
		protected const int		DENSITY_VOXEL_CELLS_COUNT = 48;
		protected const int		DENSITY_VOXEL_CORNERS_COUNT = DENSITY_VOXEL_CELLS_COUNT+1;

		// High-LOD (USE AT YOUR OWN RISKS ! I HAD TO REBOOT THE PC 3 TIMES ALREADY ! ^___^ )
		protected const int		VOXEL_CELLS_COUNT = 48;
		protected const int		MAX_TRIANGLES_COUNT_PER_VOXEL = 3*64*64*64;	// The maximum amount of triangles to emit per voxel

		#endregion

		#region NESTED TYPES

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		protected struct VS_UIntX_UIntY
		{
			[Semantic( "POSITION_X" )]
			public UInt32	PositionX;
			[Semantic( "POSITION_Y" )]
			public UInt32	PositionY;
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Density + geometry building
		protected Material<VS_P3T2>			m_MaterialBuildDensity = null;
		protected Material<VS_UIntX_UIntY>	m_MaterialBuildGeometry = null;
		protected Material<VS_P3T2>			m_MaterialDisplayDensity = null;

		// The quad VB used to render densities into the 3D texture
		protected VertexBuffer<VS_P3T2>		m_DensityQuadVB = null;

		// The slice of NxN voxel position elements used to generate the geometry
		protected VertexBuffer<VS_UIntX_UIntY>	m_VoxelSliceVB = null;

		// The list of volume textures that can be filled per frame
		protected RenderTarget3D<PF_R16F>[]	m_DensityTextures = new RenderTarget3D<PF_R16F>[3];

		// Grass & rock textures
		protected Texture2D<PF_RGBA8>		m_TextureGrass = null;
		protected Texture2D<PF_RGBA8>		m_TextureRock = null;

		protected Camera	m_Camera = null;
		public Camera		Camera
		{
			get { return m_Camera; }
			set { m_Camera = value; }
		}

		protected bool	m_bDraw = false;
		public bool	Draw
		{
			get { return m_bDraw; }
			set { m_bDraw = value; }
		}

		protected System.Drawing.Point	m_MousePosition;
		public System.Drawing.Point	MousePosition
		{
			set { m_MousePosition = value; }
		}

		#endregion

		#region METHODS

		public	RenderTechniqueFluid( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Create our main materials
			m_MaterialBuildDensity = ToDispose( new Material<VS_P3T2>( m_Device, "Build Density Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Fluid/BuildDensityVolume.fx" ) ) );
			m_MaterialBuildGeometry = ToDispose( new Material<VS_UIntX_UIntY>( m_Device, "Build Geometry Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Fluid/BuildGeometry.fx" ) ) );
			m_MaterialDisplayDensity = ToDispose( new Material<VS_P3T2>( m_Device, "Display Density Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Fluid/DisplayDensityVolume.fx" ) ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the fixed number of density targets to compute the density for the same amount of candidate voxels
			m_DensityTextures[0] = ToDispose( new RenderTarget3D<PF_R16F>( Device, "Density Texture 0", DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, 1 ) );
			m_DensityTextures[1] = ToDispose( new RenderTarget3D<PF_R16F>( Device, "Density Texture 1", DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, 1 ) );
			m_DensityTextures[2] = ToDispose( new RenderTarget3D<PF_R16F>( Device, "Density Texture 2", DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, 1 ) );

			m_Device.ClearRenderTarget( m_DensityTextures[0], new Color4( 0, 0, 0, 0 ) );
			m_Device.ClearRenderTarget( m_DensityTextures[1], new Color4( 0, 0, 0, 0 ) );
			m_Device.ClearRenderTarget( m_DensityTextures[2], new Color4( 0, 0, 0, 0 ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the geometries

			// Create the quad VB used to render the densities
			VS_P3T2[]	Vertices = new VS_P3T2[]
			{	//  vertices are organized for a triangle strip with 2 triangles
				new VS_P3T2() { Position=new Vector3( -1.0f, +1.0f, 0.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_P3T2() { Position=new Vector3( -1.0f, -1.0f, 0.0f ), UV=new Vector2( 0.0f, 1.0f ) },
				new VS_P3T2() { Position=new Vector3( +1.0f, +1.0f, 0.0f ), UV=new Vector2( 1.0f, 0.0f ) },
				new VS_P3T2() { Position=new Vector3( +1.0f, -1.0f, 0.0f ), UV=new Vector2( 1.0f, 1.0f ) },
			};

			m_DensityQuadVB = ToDispose( new VertexBuffer<VS_P3T2>( m_Device, "DensityQuadVB", Vertices ) );

			// Create the voxel slice made of points, one for each cell in a single depth of a voxel
			VS_UIntX_UIntY[]	Vertices2 = new VS_UIntX_UIntY[VOXEL_CELLS_COUNT*VOXEL_CELLS_COUNT];
			for ( int Y=0; Y < VOXEL_CELLS_COUNT; Y++ )
				for ( int X=0; X < VOXEL_CELLS_COUNT; X++ )
				{
					Vertices2[VOXEL_CELLS_COUNT*Y+X].PositionX = (uint) X;
					Vertices2[VOXEL_CELLS_COUNT*Y+X].PositionY = (uint) Y;
				}
			m_VoxelSliceVB = ToDispose( new VertexBuffer<VS_UIntX_UIntY>( m_Device, "VoxelSliceVB", Vertices2 ) );

			//////////////////////////////////////////////////////////////////////////
			// Load grass & rock
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "./Media/Terrain/ground_grass_1024_tile.jpg" ) as System.Drawing.Bitmap )
				using ( Image<PF_RGBA8> ImageGrass = new Image<PF_RGBA8>( m_Device, "Grass", B, 0, 1.0f ) )
					m_TextureGrass = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Grass", ImageGrass ) );

			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "./Media/Terrain/rock_02.jpg" ) as System.Drawing.Bitmap )
				using ( Image<PF_RGBA8> ImageRock = new Image<PF_RGBA8>( m_Device, "Rock", B, 0, 1.0f ) )
					m_TextureRock = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Rock", ImageRock ) );
		}

		public override void	Render( int _FrameToken )
		{
			//////////////////////////////////////////////////////////////////////////
			// 1] Build the geometry from the density volumes using marching cubes
  			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialBuildGeometry.UseLock() )
			{
				m_VoxelSliceVB.Use();
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
 				m_Device.SetDefaultRenderTarget();

				m_MaterialBuildGeometry.GetVariableByName( "TextureSize" ).AsScalar.Set( DENSITY_VOXEL_CORNERS_COUNT );
				m_MaterialBuildGeometry.GetVariableByName( "InvTextureSize" ).AsVector.Set( new Vector2( 1.0f / DENSITY_VOXEL_CORNERS_COUNT, 0.0f ) );
				m_MaterialBuildGeometry.GetVariableByName( "InvVoxelCellsCount" ).AsScalar.Set( 1.0f / VOXEL_CELLS_COUNT );
				m_MaterialBuildDensity.GetVariableByName( "BlockWorldSize" ).AsScalar.Set( VOXEL_BLOCK_SIZE );
				m_MaterialBuildGeometry.GetVariableByName( "BlockCellWorldSize" ).AsScalar.Set( VOXEL_BLOCK_SIZE / VOXEL_CELLS_COUNT );
				m_MaterialBuildGeometry.GetVariableByName( "BlockWorldPosition" ).AsVector.Set( Vector3.Zero );
				m_MaterialBuildGeometry.GetVariableByName( "DensityVolumeTexture" ).AsResource.SetResource( m_DensityTextures[1].TextureView );

				m_MaterialBuildGeometry.ApplyPass( 0 );

				// Draw "cells count" instances of a "cells count * cells count" array of points
				// Each point has an X,Y integer coordinate, the instance ID being the Z coordinate of the cell being drawn
				m_VoxelSliceVB.DrawInstanced( VOXEL_CELLS_COUNT );
			}

 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.BLEND );

			using ( m_MaterialDisplayDensity.UseLock() )
			{
				m_DensityQuadVB.Use();
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
 				m_Device.SetDefaultRenderTarget();

				VariableMatrix		vLocal2World = m_MaterialDisplayDensity.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix;
				m_MaterialDisplayDensity.GetVariableByName( "DensityVolumeTexture" ).AsResource.SetResource( m_DensityTextures[1].TextureView );

				Matrix	Local2World = Matrix.Identity;

				// Locate at the center of the voxel
				Local2World.M41 = 0.5f * VOXEL_BLOCK_SIZE;
				Local2World.M42 = 0.5f * VOXEL_BLOCK_SIZE;
				Local2World.M43 = 0.5f * VOXEL_BLOCK_SIZE;
				vLocal2World.SetMatrix( Local2World );

				m_MaterialDisplayDensity.ApplyPass( 0 );
				m_DensityQuadVB.Draw();
			}

			//////////////////////////////////////////////////////////////////////////
			// 2] Compute density volumes for voxel
			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialBuildDensity.UseLock() )
			{
				m_DensityQuadVB.Use();
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
 				m_Device.SetViewport( 0, 0, DENSITY_VOXEL_CORNERS_COUNT, DENSITY_VOXEL_CORNERS_COUNT, 0.0f, 1.0f );

				m_MaterialBuildDensity.GetVariableByName( "TextureSize" ).AsScalar.Set( DENSITY_VOXEL_CORNERS_COUNT );
				m_MaterialBuildDensity.GetVariableByName( "BlockWorldSize" ).AsScalar.Set( VOXEL_BLOCK_SIZE );

				m_MaterialBuildDensity.GetVariableByName( "Draw" ).AsScalar.Set( m_bDraw );
				m_bDraw = false;	// Reset so the guy needs to continually press space to draw...

				// Compute mouse 2D position's intersection with the plane centered on the voxel
				Vector3	VoxelCenter = new Vector3( 2.0f, 2.0f, 2.0f );
				Plane	P = new Plane( VoxelCenter, -new Vector3( m_Camera.Camera2World.M31, m_Camera.Camera2World.M32, m_Camera.Camera2World.M33 ) );

				Vector3	View = new Vector3(
						m_Camera.AspectRatio * (float) Math.Tan( 0.5 * m_Camera.PerspectiveFOV ) * (2.0f * m_MousePosition.X / m_Device.DefaultRenderTarget.Width - 1.0f),
						(float) Math.Tan( 0.5 * m_Camera.PerspectiveFOV ) * (1.0f - 2.0f * m_MousePosition.Y / m_Device.DefaultRenderTarget.Height),
						1.0f );
				View.Normalize();
				View = Vector3.TransformNormal( View, m_Camera.Camera2World );

				Ray		R = new Ray( new Vector3( m_Camera.Camera2World.M41, m_Camera.Camera2World.M42, m_Camera.Camera2World.M43 ), View );

				float	HitDistance = -Vector3.Dot( R.Position - VoxelCenter, P.Normal ) / Vector3.Dot( P.Normal, R.Direction );
				Vector3	HitPosition = R.Position + HitDistance * R.Direction;

				m_MaterialBuildDensity.GetVariableByName( "DrawCenter" ).AsVector.Set( HitPosition );


				m_MaterialBuildDensity.GetVariableByName( "BlockWorldPosition" ).AsVector.Set( Vector3.Zero );

				// Render to our density volume
				m_Device.SetRenderTarget( m_DensityTextures[2] );
				m_MaterialBuildDensity.GetVariableByName( "OldDensityVolumeTexture0" ).AsResource.SetResource( m_DensityTextures[0].TextureView );
				m_MaterialBuildDensity.GetVariableByName( "OldDensityVolumeTexture1" ).AsResource.SetResource( m_DensityTextures[1].TextureView );

				m_MaterialBuildDensity.ApplyPass( 0 );

				// Render as many quad instances as voxel corners
				//	each instance will render in a different slice of the 3D target
				m_DensityQuadVB.DrawInstanced( DENSITY_VOXEL_CORNERS_COUNT );

				// Scroll density textures
				RenderTarget3D<PF_R16F>	Temp = m_DensityTextures[0];
				m_DensityTextures[0] = m_DensityTextures[1];
				m_DensityTextures[1] = m_DensityTextures[2];
				m_DensityTextures[2] = Temp;
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
			float[,,]	Noise = new float[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];

	// Make it tile :		http://www.gamedev.net/community/forums/topic.asp?topic_id=409855

// 			Vector3		Position;
// 			for ( int Z=0; Z < NOISE_SIZE; Z++ )
// 			{
// 				Position.Z = Offsets[_NoiseIndex].Z + (0+Z) * GLOBAL_SCALE * Scales[_NoiseIndex].Z;
// 				for ( int Y=0; Y < NOISE_SIZE; Y++ )
// 				{
// 					Position.Y = Offsets[_NoiseIndex].Y + Y * GLOBAL_SCALE * Scales[_NoiseIndex].Y;
// 					for ( int X=0; X < NOISE_SIZE; X++ )
// 					{
// 						Position.X = Offsets[_NoiseIndex].X + X * GLOBAL_SCALE * Scales[_NoiseIndex].X;
// 						Noise[X,Y,Z] = NoiseSimplex( Position.X, Position.Y, Position.Z );
// //						Noise[X,Y,Z] = NoisePeriodic( Position.X, Position.Y, Position.Z, 16, 16, 16 );
// //						Noise[X,Y,Z] = 0.8f - (new Vector3( X - NOISE_SIZE/2, Y - NOISE_SIZE/2, Z - NOISE_SIZE/2 ) * 2.0f / NOISE_SIZE).Length();
// //						Noise[X,Y,Z] = 0.8f - (new Vector3( X, Y, Z ) / NOISE_SIZE).Length();
// 					}
// 				}
// 			}

			// Read noise from disk
			System.IO.FileInfo		NoiseFile = new System.IO.FileInfo( @"..\Apps\DemoTerrain\Resources\packednoise_half_16cubed_mips_0" + _NoiseIndex + ".vol" );
			System.IO.FileStream	Stream = NoiseFile.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			int	XS, YS, ZS, PS;
			XS = Reader.ReadInt32();
			YS = Reader.ReadInt32();
			ZS = Reader.ReadInt32();
			PS = Reader.ReadInt32();

			Half	Temp = new Half();
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

		#region Perlin Noise (stolen from http://staffwww.itn.liu.se/~stegu/aqsis/aqsis-newnoise/)

		//---------------------------------------------------------------------
		// 3D float Perlin periodic noise.
		//
		protected float	NoisePeriodic( float x, float y, float z, int px, int py, int pz )
		{
			int ix0, iy0, ix1, iy1, iz0, iz1;
			float fx0, fy0, fz0, fx1, fy1, fz1;
			float s, t, r;
			float nxy0, nxy1, nx0, nx1, n0, n1;

			ix0 = (int) Math.Floor( x );	// Integer part of x
			iy0 = (int) Math.Floor( y );	// Integer part of y
			iz0 = (int) Math.Floor( z );	// Integer part of z
			fx0 = x - ix0;					// Fractional part of x
			fy0 = y - iy0;					// Fractional part of y
			fz0 = z - iz0;					// Fractional part of z
			fx1 = fx0 - 1.0f;
			fy1 = fy0 - 1.0f;
			fz1 = fz0 - 1.0f;
			ix1 = (( ix0 + 1 ) % px ) & 0xff; // Wrap to 0..px-1 and wrap to 0..255
			iy1 = (( iy0 + 1 ) % py ) & 0xff; // Wrap to 0..py-1 and wrap to 0..255
			iz1 = (( iz0 + 1 ) % pz ) & 0xff; // Wrap to 0..pz-1 and wrap to 0..255
			ix0 = ( ix0 % px ) & 0xff;
			iy0 = ( iy0 % py ) & 0xff;
			iz0 = ( iz0 % pz ) & 0xff;
    
			r = FADE( fz0 );
			t = FADE( fy0 );
			s = FADE( fx0 );

			nxy0 = grad(ms_PermutationTable[ix0 + ms_PermutationTable[iy0 + ms_PermutationTable[iz0]]], fx0, fy0, fz0);
			nxy1 = grad(ms_PermutationTable[ix0 + ms_PermutationTable[iy0 + ms_PermutationTable[iz1]]], fx0, fy0, fz1);
			nx0 = LERP( r, nxy0, nxy1 );

			nxy0 = grad(ms_PermutationTable[ix0 + ms_PermutationTable[iy1 + ms_PermutationTable[iz0]]], fx0, fy1, fz0);
			nxy1 = grad(ms_PermutationTable[ix0 + ms_PermutationTable[iy1 + ms_PermutationTable[iz1]]], fx0, fy1, fz1);
			nx1 = LERP( r, nxy0, nxy1 );

			n0 = LERP( t, nx0, nx1 );

			nxy0 = grad(ms_PermutationTable[ix1 + ms_PermutationTable[iy0 + ms_PermutationTable[iz0]]], fx1, fy0, fz0);
			nxy1 = grad(ms_PermutationTable[ix1 + ms_PermutationTable[iy0 + ms_PermutationTable[iz1]]], fx1, fy0, fz1);
			nx0 = LERP( r, nxy0, nxy1 );

			nxy0 = grad(ms_PermutationTable[ix1 + ms_PermutationTable[iy1 + ms_PermutationTable[iz0]]], fx1, fy1, fz0);
			nxy1 = grad(ms_PermutationTable[ix1 + ms_PermutationTable[iy1 + ms_PermutationTable[iz1]]], fx1, fy1, fz1);
			nx1 = LERP( r, nxy0, nxy1 );

			n1 = LERP( t, nx0, nx1 );
    
			return 0.936f * ( LERP( s, n0, n1 ) );
		}

		//---------------------------------------------------------------------
		// This implementation is "Simplex Noise" as presented by
		// Ken Perlin at a relatively obscure and not often cited course
		// session "Real-Time Shading" at Siggraph 2001 (before real
		// time shading actually took on), under the title "hardware noise".
		// The 3D function is numerically equivalent to his Java reference
		// code available in the PDF course notes, although I re-implemented
		// it from scratch to get more readable code. The 1D, 2D and 4D cases
		// were implemented from scratch by me from Ken Perlin's text.
		// 
		protected float NoiseSimplex( float x, float y, float z )
		{
			// Simple skewing factors for the 3D case
			const float F3 = 0.333333333f;
			const float G3 = 0.166666667f;

			float n0, n1, n2, n3;	// Noise contributions from the four corners

			// Skew the input space to determine which simplex cell we're in
			float s = (x+y+z)*F3;	// Very nice and simple skew factor for 3D
			float xs = x+s;
			float ys = y+s;
			float zs = z+s;
			int i = (int) Math.Floor(xs);
			int j = (int) Math.Floor(ys);
			int k = (int) Math.Floor(zs);

			float t = (float)(i+j+k)*G3; 
			float X0 = i-t;			// Unskew the cell origin back to (x,y,z) space
			float Y0 = j-t;
			float Z0 = k-t;
			float x0 = x-X0;		// The x,y,z distances from the cell origin
			float y0 = y-Y0;
			float z0 = z-Z0;

			// For the 3D case, the simplex shape is a slightly irregular tetrahedron.
			// Determine which simplex we are in.
			int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
			int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

			/* This code would benefit from a backport from the GLSL version! */
			if(x0>=y0)
			{
				if(y0>=z0)
				{ i1=1; j1=0; k1=0; i2=1; j2=1; k2=0; } // X Y Z order
				else if(x0>=z0) { i1=1; j1=0; k1=0; i2=1; j2=0; k2=1; } // X Z Y order
				else { i1=0; j1=0; k1=1; i2=1; j2=0; k2=1; } // Z X Y order
			}
			else // x0<y0
			{
				if(y0<z0) { i1=0; j1=0; k1=1; i2=0; j2=1; k2=1; } // Z Y X order
				else if(x0<z0) { i1=0; j1=1; k1=0; i2=0; j2=1; k2=1; } // Y Z X order
				else { i1=0; j1=1; k1=0; i2=1; j2=1; k2=0; } // Y X Z order
			}

			// A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
			// a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
			// a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
			// c = 1/6.

			float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
			float y1 = y0 - j1 + G3;
			float z1 = z0 - k1 + G3;
			float x2 = x0 - i2 + 2.0f*G3; // Offsets for third corner in (x,y,z) coords
			float y2 = y0 - j2 + 2.0f*G3;
			float z2 = z0 - k2 + 2.0f*G3;
			float x3 = x0 - 1.0f + 3.0f*G3; // Offsets for last corner in (x,y,z) coords
			float y3 = y0 - 1.0f + 3.0f*G3;
			float z3 = z0 - 1.0f + 3.0f*G3;

			// Wrap the integer indices at 256, to avoid indexing ms_PermutationTable[] out of bounds
			int ii = i & 0xFF;
			int jj = j & 0xFF;
			int kk = k & 0xFF;

			// Calculate the contribution from the four corners
			float t0 = 0.6f - x0*x0 - y0*y0 - z0*z0;
			if(t0 < 0.0f) n0 = 0.0f;
			else {
				t0 *= t0;
				n0 = t0 * t0 * grad(ms_PermutationTable[ii+ms_PermutationTable[jj+ms_PermutationTable[kk]]], x0, y0, z0);
			}

			float t1 = 0.6f - x1*x1 - y1*y1 - z1*z1;
			if(t1 < 0.0f) n1 = 0.0f;
			else {
				t1 *= t1;
				n1 = t1 * t1 * grad(ms_PermutationTable[ii+i1+ms_PermutationTable[jj+j1+ms_PermutationTable[kk+k1]]], x1, y1, z1);
			}

			float t2 = 0.6f - x2*x2 - y2*y2 - z2*z2;
			if(t2 < 0.0f) n2 = 0.0f;
			else {
				t2 *= t2;
				n2 = t2 * t2 * grad(ms_PermutationTable[ii+i2+ms_PermutationTable[jj+j2+ms_PermutationTable[kk+k2]]], x2, y2, z2);
			}

			float t3 = 0.6f - x3*x3 - y3*y3 - z3*z3;
			if(t3<0.0f) n3 = 0.0f;
			else {
				t3 *= t3;
				n3 = t3 * t3 * grad(ms_PermutationTable[ii+1+ms_PermutationTable[jj+1+ms_PermutationTable[kk+1]]], x3, y3, z3);
			}

			// Add contributions from each corner to get the final noise value.
			// The result is scaled to stay just inside [-1,1]
			return 32.0f * (n0 + n1 + n2 + n3); // TODO: The scale factor is preliminary!
		}

		protected float	LERP( float t, float x0, float x1 )
		{
			return (1.0f-t) * x0 + t * x1;
		}

		// This is the new and improved, C(2) continuous interpolant
		protected float FADE( float t )
		{
			return t * t * t * ( t * ( t * 6 - 15 ) + 10 );
		}

		// Helper functions to compute gradients-dot-residualvectors (1D to 4D)
		// Note that these generate gradients of more than unit length. To make
		// a close match with the value range of classic Perlin noise, the final
		// noise values need to be rescaled. To match the RenderMan noise in a
		// statistical sense, the approximate scaling values (empirically
		// determined from test renderings) are:
		// 1D noise needs rescaling with 0.188
		// 2D noise needs rescaling with 0.507
		// 3D noise needs rescaling with 0.936
		// 4D noise needs rescaling with 0.87
		// Note that these noise functions are the most practical and useful
		// signed version of Perlin noise. To return values according to the
		// RenderMan specification from the SL noise() and pnoise() functions,
		// the noise values need to be scaled and offset to [0,1], like this:
		// float SLnoise = (noise(x,y,z) + 1.0) * 0.5;
		//
		protected float  grad( int hash, float x, float y , float z )
		{
			int h = hash & 0xF;		// Convert low 4 bits of hash code into 12 simple
			float u = h<8 ? x : y;	// gradient directions, and compute dot product.
			float v = h<4 ? y : ((h==12 || h==14) ? x : z); // Fix repeats at h = 12 to 15
			return ((h&1) != 0 ? -u : u) + ((h&2) != 0 ? -v : v);
		}

		// Permutation table. This is just a random jumble of all numbers 0-255,
		// repeated twice to avoid wrapping the index at 255 for each lookup.
		// This needs to be exactly the same for all instances on all platforms,
		// so it's easiest to just keep it as static explicit data.
		// This also removes the need for any initialisation of this class.
		//
		// Note that making this an int[] instead of a char[] might make the
		// code run faster on platforms with a high penalty for unaligned single
		// byte addressing. Intel x86 is generally single-byte-friendly, but
		// some other CPUs are faster with 4-aligned reads.
		// However, a char[] is smaller, which avoids cache trashing, and that
		// is probably the most important aspect on most architectures.
		// This array is accessed a *lot* by the noise functions.
		// A vector-valued noise over 3D accesses it 96 times, and a
		// float-valued 4D noise 64 times. We want this to fit in the cache!
		//
		protected byte[]	ms_PermutationTable = new byte[2*256]
		{
		  151,160,137,91,90,15,
		  131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
		  190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
		  88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
		  77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
		  102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
		  135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
		  5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
		  223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
		  129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
		  251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
		  49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
		  138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,

		  151,160,137,91,90,15,
		  131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
		  190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
		  88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
		  77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
		  102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
		  135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
		  5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
		  223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
		  129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
		  251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
		  49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
		  138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
		};

		#endregion

		#endregion
	}
}
