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
	/// Caustics effect
	/// </example>
	public class RenderTechniqueCaustics : RenderTechnique
	{
		#region CONSTANTS

		protected const int		CELLS_COUNT = 200;			// Amount of hexagonal cells on a single line (square that to get total cells count)
		protected const int		RENDER_TEXTURE_SIZE = 512;	// Size of the caustics texture

		#endregion

		#region NESTED TYPES

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Density + geometry building
		protected Material<VS_T2>			m_MaterialBuildCaustics = null;
		protected Material<VS_P3T2>			m_MaterialDisplayCaustics = null;

		// The hexagonal grid that will get deformed and rendered to the caustics texture
		protected float						m_TriangleNominalArea = 0.0f;
		protected Primitive<VS_T2,int>		m_HexagonalGrid = null;

		// The texture to render caustics to
		protected RenderTarget<PF_R16F>		m_CausticsTexture = null;

		// 2 normal textures for deformation
		protected Texture2D<PF_RGBA8>		m_NormalTexture0 = null;
		protected Texture2D<PF_RGBA8>		m_NormalTexture1 = null;

		// The render quad
		protected VertexBuffer<VS_P3T2>		m_CausticsQuadVB = null;

		// Time for animation
		protected float						m_Time = 0.0f;

		#endregion

		#region PROPERTIES

		public float						Time				{ get { return m_Time; } set { m_Time = value; } }

		#endregion

		#region METHODS

		public	RenderTechniqueCaustics( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Create our main materials
			m_MaterialBuildCaustics = ToDispose( new Material<VS_T2>( m_Device, "Caustics Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/BuildCaustics.fx" ) ) );
			m_MaterialDisplayCaustics = ToDispose( new Material<VS_P3T2>( m_Device, "Display Caustics Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Caustics/DisplayCaustics.fx" ) ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the caustics texture
			m_CausticsTexture = ToDispose( new RenderTarget<PF_R16F>( Device, "Caustics Texture", RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 1 ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the normal textures
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "Media/NormalMaps/normalmap2.png" ) as System.Drawing.Bitmap )
				using ( Image<PF_RGBA8> Image = new Image<PF_RGBA8>( m_Device, "Normal Image", B, 0, 1.0f ) )
					m_NormalTexture0 = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Normal Texture 0", Image ) );

			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( "Media/NormalMaps/04_dot4.png" ) as System.Drawing.Bitmap )
				using ( Image<PF_RGBA8> Image = new Image<PF_RGBA8>( m_Device, "Normal Image", B, 0, 1.0f ) )
					m_NormalTexture1 = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Normal Texture 1", Image ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the hexagonal grid
			//
			// An hexagonal grid is quite simple to build if we split the hexagons vertically into bands of odd and even triangles :
			//
			//      o---o---o---o      <= CELLS_COUNT+1 vertices
			//  E  / \ / \ / \ / \
			//    o---o---o---o---o    <= CELLS_COUNT+2 vertices
			//  O  \ / \ / \ / \ /
			//      o---o---o---o
			//  E  / \ / \ / \ / \
			//    o---o---o---o---o
			//  O  \ / \ / \ / \ /
			//      o---o---o---o
			//
			// If we fix the amount of horizontal cells, we just need to determine how many lines of triangles are necessary
			//	to cover the vertical span of the texture (i.e. [0,1]).
			// If we fix L the horizontal space between vertices, we need to compute H, the vertical space between lines of
			//	vertices so all triangles are equilateral. Using Pythagore we find that H = Sqrt(3)/2 * L
			//
			float		L = 1.0f / CELLS_COUNT;						// Length of all triangle edges
			float		H = 0.5f * (float) Math.Sqrt( 3.0 ) * L;	// Height between lines of triangles so they're all equilateral

			m_TriangleNominalArea = 0.5f * H * L;					// This is the undistorted area covered by the triangle

			int			TotalTriangleLinesCount = (int) Math.Ceiling( 1.0 / H );// Total amount of triangle lines
			int			LinesCountEven = 1+TotalTriangleLinesCount/2;			// Amount of even vertices lines
			int			LinesCountOdd = (TotalTriangleLinesCount+1)/2;			// Amount of odd vertices lines

			int			TotalVerticesCount = (CELLS_COUNT+1) * LinesCountEven + (CELLS_COUNT+2) * LinesCountOdd;
			int			TrianglesCountPerLine = 1+2*CELLS_COUNT;
			int			TotalTrianglesCount = TrianglesCountPerLine * TotalTriangleLinesCount;

			VS_T2[]		Vertices = new VS_T2[TotalVerticesCount];
			int[]		Indices = new int[6*TotalTrianglesCount];

			// Build vertices
			bool	bEven = true;
			float	fY = 0.0f;
			int		VertexIndex = 0;
			for ( int Y=0; Y <= TotalTriangleLinesCount; Y++, fY+=H )
			{
				int		LineVerticesCount = bEven ? CELLS_COUNT+1 : CELLS_COUNT+2;
				float	fX = bEven ? 0.0f : -0.5f * L;
				for ( int X=0; X < LineVerticesCount; X++, fX+=L, VertexIndex++ )
				{
					Vertices[VertexIndex].UV.X = fX;
					Vertices[VertexIndex].UV.Y = fY;
				}
				bEven = !bEven;
			}

			// Build indices
			bEven = true;
			int		TriangleIndex = 0;
			int		MorePreviousLineIndex = 0;			// Index of the vertices from previous previous line
			int		PreviousLineIndex = 0;				// Index of the vertices from previous line
			int		CurrentLineIndex = CELLS_COUNT+1;	// Index of the vertices from current line
			int		NextLineIndex = CurrentLineIndex+CELLS_COUNT+2;	// Index of the vertices from next line
			for ( int Y=0; Y < TotalTriangleLinesCount; Y++ )
			{
				if ( bEven )
				{	// Odd lines : previous line has CELL+1 vertices, current line has CELL+2 vertices

					// 2 triangles per cell
					int	X = 0;
					for ( X=0; X < CELLS_COUNT; X++ )
					{
						Indices[6*TriangleIndex+0] = PreviousLineIndex+X;
						Indices[6*TriangleIndex+1] = Math.Max( PreviousLineIndex, PreviousLineIndex+X-1 );
						Indices[6*TriangleIndex+2] = CurrentLineIndex+X;
						Indices[6*TriangleIndex+3] = Math.Min( Indices.Length-1, NextLineIndex+X );
						Indices[6*TriangleIndex+4] = CurrentLineIndex+X+1;
						Indices[6*TriangleIndex+5] = PreviousLineIndex+X+1;
						TriangleIndex++;

						Indices[6*TriangleIndex+0] = PreviousLineIndex+X;
						Indices[6*TriangleIndex+1] = CurrentLineIndex+X;
						Indices[6*TriangleIndex+2] = CurrentLineIndex+X+1;
						Indices[6*TriangleIndex+3] = CurrentLineIndex+X+2;
						Indices[6*TriangleIndex+4] = PreviousLineIndex+X+1;
						Indices[6*TriangleIndex+5] = MorePreviousLineIndex+X+1;
						TriangleIndex++;
					}

					// Final triangle
					Indices[6*TriangleIndex+0] = PreviousLineIndex+X;
					Indices[6*TriangleIndex+1] = PreviousLineIndex+X-1;
					Indices[6*TriangleIndex+2] = CurrentLineIndex+X;
					Indices[6*TriangleIndex+3] = Math.Min( Indices.Length-1, NextLineIndex+X );
					Indices[6*TriangleIndex+4] = CurrentLineIndex+X+1;
					Indices[6*TriangleIndex+5] = CurrentLineIndex+X+1;	// Null area face
					TriangleIndex++;

					// Update indices
					MorePreviousLineIndex = PreviousLineIndex;
					PreviousLineIndex = CurrentLineIndex;
					CurrentLineIndex = NextLineIndex;
					NextLineIndex += CELLS_COUNT+1;
				}
				else
				{	// Odd lines : previous line has CELL+2 vertices, current line has CELL+1 vertices

					// 2 triangles per cell
					int	X = 0;
					for ( X=0; X < CELLS_COUNT; X++ )
					{
						Indices[6*TriangleIndex+0] = PreviousLineIndex+X;
						Indices[6*TriangleIndex+1] = Math.Max( CurrentLineIndex, CurrentLineIndex+X-1 );
						Indices[6*TriangleIndex+2] = CurrentLineIndex+X;
						Indices[6*TriangleIndex+3] = CurrentLineIndex+X+1;
						Indices[6*TriangleIndex+4] = PreviousLineIndex+X+1;
						Indices[6*TriangleIndex+5] = MorePreviousLineIndex+X;
						TriangleIndex++;

						Indices[6*TriangleIndex+0] = PreviousLineIndex+X+1;
						Indices[6*TriangleIndex+1] = PreviousLineIndex+X;
						Indices[6*TriangleIndex+2] = CurrentLineIndex+X;
						Indices[6*TriangleIndex+3] = Math.Min( Indices.Length-1, NextLineIndex+X+1 );
						Indices[6*TriangleIndex+4] = CurrentLineIndex+X+1;
						Indices[6*TriangleIndex+5] = PreviousLineIndex+X+2;
						TriangleIndex++;
					}

					// Final triangle
					Indices[6*TriangleIndex+0] = PreviousLineIndex+X;
					Indices[6*TriangleIndex+1] = CurrentLineIndex+X-1;
					Indices[6*TriangleIndex+2] = CurrentLineIndex+X;
					Indices[6*TriangleIndex+3] = CurrentLineIndex+X;	// Null area face
					Indices[6*TriangleIndex+4] = PreviousLineIndex+X+1;
					Indices[6*TriangleIndex+5] = MorePreviousLineIndex+X;
					TriangleIndex++;

					// Update indices
					MorePreviousLineIndex = PreviousLineIndex;
					PreviousLineIndex = CurrentLineIndex;
					CurrentLineIndex = NextLineIndex;
					NextLineIndex += CELLS_COUNT+2;
				}

				bEven = !bEven;
			}

			m_HexagonalGrid = ToDispose( new Primitive<VS_T2,int>( m_Device, "Caustics Grid", PrimitiveTopology.TriangleListWithAdjacency, Vertices, Indices ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the quad VB used to render the caustics
			VS_P3T2[]	Vertices2 = new VS_P3T2[]
			{	//  vertices are organized for a triangle strip with 2 triangles
				new VS_P3T2() { Position=new Vector3( -1.0f, +1.0f, 0.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_P3T2() { Position=new Vector3( -1.0f, -1.0f, 0.0f ), UV=new Vector2( 0.0f, 1.0f ) },
				new VS_P3T2() { Position=new Vector3( +1.0f, +1.0f, 0.0f ), UV=new Vector2( 1.0f, 0.0f ) },
				new VS_P3T2() { Position=new Vector3( +1.0f, -1.0f, 0.0f ), UV=new Vector2( 1.0f, 1.0f ) },
			};

			m_CausticsQuadVB = ToDispose( new VertexBuffer<VS_P3T2>( m_Device, "CausticsQuadVB", Vertices2 ) );
		}

		public override void	Render( int _FrameToken )
		{
			//////////////////////////////////////////////////////////////////////////
			// 1] Build the caustics from the hexagonal grid
  			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );

			using ( m_MaterialBuildCaustics.UseLock() )
			{
				m_Device.ClearRenderTarget( m_CausticsTexture, new Color4( 0, 0, 0, 0 ) );
 				m_Device.SetRenderTarget( m_CausticsTexture );
 				m_Device.SetViewport( 0, 0, RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 0.0f, 1.0f );

				m_MaterialBuildCaustics.GetVariableByName( "NormalMap0" ).AsResource.SetResource( m_NormalTexture0.TextureView );
				m_MaterialBuildCaustics.GetVariableByName( "NormalMap1" ).AsResource.SetResource( m_NormalTexture1.TextureView );
				m_MaterialBuildCaustics.GetVariableByName( "TriangleNominalArea" ).AsScalar.Set( m_TriangleNominalArea );
				m_MaterialBuildCaustics.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				m_MaterialBuildCaustics.GetVariableByName( "InvTexture" ).AsVector.Set( new Vector2( 1.0f / CELLS_COUNT, 0.0f ) );

				m_MaterialBuildCaustics.ApplyPass( 0 );

				// Render the caustics grid
				m_HexagonalGrid.Render();
			}

			//////////////////////////////////////////////////////////////////////////
			// 2] Render caustics
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialDisplayCaustics.UseLock() )
			{
				CurrentMaterial.CurrentTechnique = m_MaterialDisplayCaustics.GetTechniqueByName( "DisplayCaustics" );
//				CurrentMaterial.CurrentTechnique = m_MaterialDisplayCaustics.GetTechniqueByName( "DisplayCausticsBackward" );

				m_CausticsQuadVB.Use();
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
 				m_Device.SetDefaultRenderTarget();

				Matrix	Local2World = Matrix.Identity;
				m_MaterialDisplayCaustics.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix.SetMatrix( Local2World );
				m_MaterialDisplayCaustics.GetVariableByName( "CausticsTexture" ).AsResource.SetResource( m_CausticsTexture.TextureView );

				// Data for backward tracing
				m_MaterialDisplayCaustics.GetVariableByName( "NormalMap0" ).AsResource.SetResource( m_NormalTexture0.TextureView );
				m_MaterialDisplayCaustics.GetVariableByName( "NormalMap1" ).AsResource.SetResource( m_NormalTexture1.TextureView );
				m_MaterialDisplayCaustics.GetVariableByName( "Time" ).AsScalar.Set( m_Time );
				m_MaterialDisplayCaustics.GetVariableByName( "InvTexture" ).AsVector.Set( new Vector2( 1.0f / CELLS_COUNT, 0.0f ) );

				CurrentMaterial.ApplyPass( 0 );
				m_CausticsQuadVB.Draw();
			}
		}

		#endregion
	}
}
