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
	/// Terrain rendering
	/// </example>
	public class RenderTechniqueTerrain : RenderTechniqueBase
	{
		#region CONSTANTS

		protected const int				TILE_SIZE = 1024;
		protected const int				GEOMETRY_SIZE = 128;
		protected const float			TILE_WORLD_SIZE = 64.0f;

		#endregion

		#region FIELDS

		// Materials
		protected Material<VS_P3T2>			m_Material = null;

		// Textures
		protected Texture2D<PF_RGBA8>[]		m_TilesDiffuse = null;
		protected Texture2D<PF_R16F>[]		m_TilesHeight = null;
		protected Vector3[]					m_TilesPosition = null;

		// Geometry
		protected VertexBuffer<VS_P3T2>		m_TileVB = null;

		#endregion

		#region PROPERTIES

		#endregion

		#region METHODS

		public RenderTechniqueTerrain( RendererSetup _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main material
			m_Material = ToDispose( new Material<VS_P3T2>( m_Device, "Terrain Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Vegetation/TerrainRendering.fx" ) ) );

			BuildGeometry();
			BuildTiles( new System.IO.FileInfo( "./Media/Terrain/LargeMap/textureterrain.jpg" ), new System.IO.FileInfo( "./Media/Terrain/LargeMap/alphaterrain.jpg" ) );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Main Pass", "Render Terrain" );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

			using ( m_Material.UseLock() )
			{
				VariableResource	vTileDiffuse = CurrentMaterial.GetVariableByName( "DiffuseTexture" ).AsResource;
				VariableResource	vTileHeight = CurrentMaterial.GetVariableByName( "HeightTexture" ).AsResource;
				VariableVector		vTilePosition = CurrentMaterial.GetVariableByName( "TilePosition" ).AsVector;
				CurrentMaterial.GetVariableByName( "HeightFactor" ).AsScalar.Set( 1.0f );

				EffectPass			Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				m_TileVB.Use();

				for ( int TileIndex=0; TileIndex < m_TilesPosition.Length; TileIndex++ )
				{
					vTileDiffuse.SetResource( m_TilesDiffuse[TileIndex] );
					vTileHeight.SetResource( m_TilesHeight[TileIndex] );
					vTilePosition.Set( m_TilesPosition[TileIndex] );
					Pass.Apply();

					m_TileVB.Draw();
				}
			}
		}

		protected void	BuildGeometry()
		{
			List<VS_P3T2>	Vertices = new List<VS_P3T2>();
			for ( int Y=0; Y < GEOMETRY_SIZE; Y++ )
			{
				float	fY = (float) Y / GEOMETRY_SIZE;
				float	fYp = (float) (Y+1) / GEOMETRY_SIZE;
				for ( int X=0; X <= GEOMETRY_SIZE; X++ )
				{
					float	fX = (float) X / GEOMETRY_SIZE;
					float	fXp = (float) (X+1) / GEOMETRY_SIZE;

					BuildVertex( fX, fY, Vertices );
					BuildVertex( fX, fYp, Vertices );
				}

				BuildVertex( 1.0f, fYp, Vertices );
				BuildVertex( 0.0f, fYp, Vertices );
			}

			m_TileVB = ToDispose( new VertexBuffer<VS_P3T2>( m_Device, "Tile Geometry", Vertices.ToArray() ) );
		}

		protected void	BuildVertex( float _X, float _Y, List<VS_P3T2> _Vertices )
		{
			_Vertices.Add( new VS_P3T2()
			{
				Position = TILE_WORLD_SIZE * new Vector3( _X, 0.0f, _Y ),
				UV = new Vector2( _X, _Y )
			} );
		}

		protected unsafe void	BuildTiles( System.IO.FileInfo _DiffuseTex, System.IO.FileInfo _AlphaTex )
		{
			float	Factor = 1.0f / 255.0f;
			int		Width, Height;
			int		TilesCountX, TilesCountY;
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( _DiffuseTex.FullName ) as System.Drawing.Bitmap )
			{
				Width = B.Width;
				Height = B.Height;

				System.Drawing.Imaging.BitmapData	LockedBitmap = B.LockBits( new System.Drawing.Rectangle( 0, 0, Width, Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

// 				TilesCountX = (int) Math.Ceiling( (float) Width / TILE_SIZE );
// 				TilesCountY = (int) Math.Ceiling( (float) Height / TILE_SIZE );
				TilesCountX = 2;
				TilesCountY = 2;

				m_TilesDiffuse = new Texture2D<PF_RGBA8>[TilesCountY*TilesCountX];
				m_TilesHeight = new Texture2D<PF_R16F>[TilesCountY*TilesCountX];
				m_TilesPosition = new Vector3[TilesCountY*TilesCountX];

				int		MaxPos = 4 * (Width * Height - 1);

				// Generate diffuse tiles
				for ( int TileY=0; TileY < TilesCountY; TileY++ )
				{
					for ( int TileX=0; TileX < TilesCountX; TileX++ )
					{
						byte*	pOrigin = (byte*) LockedBitmap.Scan0.ToPointer() + 4 * (TILE_SIZE * (Width*TileY + TileX));
						byte*	pPixel = null;

						Image<PF_RGBA8>	I = new Image<PF_RGBA8>( m_Device, "Pipo", TILE_SIZE+1, TILE_SIZE+1, ( int _X, int _Y, ref Vector4 _Color ) =>
						{
							pPixel = pOrigin + ((Width * _Y + _X) << 2);
							_Color.Z = *pPixel++ * Factor;
							_Color.Y = *pPixel++ * Factor;
							_Color.X = *pPixel++ * Factor;
							_Color.W = 1.0f;
						}, 0 );

						m_TilesDiffuse[TilesCountY*TileY+TileX] = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "DiffuseTile", I ) );

						I.Dispose();
					}
				}

				B.UnlockBits( LockedBitmap );
			}

			// Force collection
			GC.Collect();

			// Generate height tiles
			using ( System.Drawing.Bitmap B = System.Drawing.Bitmap.FromFile( _AlphaTex.FullName ) as System.Drawing.Bitmap )
			{
				System.Drawing.Imaging.BitmapData	LockedBitmap = B.LockBits( new System.Drawing.Rectangle( 0, 0, Width, Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				for ( int TileY=0; TileY < TilesCountY; TileY++ )
				{
					for ( int TileX=0; TileX < TilesCountX; TileX++ )
					{
						byte*	pOrigin = (byte*) LockedBitmap.Scan0.ToPointer() + 4 * (TILE_SIZE * (Width*TileY + TileX));
						byte*	pPixel = null;

						Image<PF_R16F>	I = new Image<PF_R16F>( m_Device, "Pipo", TILE_SIZE, TILE_SIZE, ( int _X, int _Y, ref Vector4 _Color ) =>
						{
							pPixel = pOrigin + ((Width * _Y + _X) << 2);
							_Color.X = *pPixel++ * Factor;
							_Color.Y = _Color.Z = _Color.W = 1.0f;
						}, 0 );

						m_TilesHeight[TilesCountY*TileY+TileX] = ToDispose( new Texture2D<PF_R16F>( m_Device, "HeightTile", I ) );

						I.Dispose();
					}
				}

				B.UnlockBits( LockedBitmap );
			}

			// Force collection
			GC.Collect();

			// Generate positions
			for ( int TileY=0; TileY < TilesCountY; TileY++ )
			{
				for ( int TileX=0; TileX < TilesCountX; TileX++ )
				{
					m_TilesPosition[TilesCountY*TileY+TileX] = new Vector3( TILE_WORLD_SIZE * TileX, 0.0f, TILE_WORLD_SIZE * TileY );
				}
			}
		}

		#endregion
	}
}
