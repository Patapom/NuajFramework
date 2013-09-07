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
	/// Deferred Rendering Technique for Terrain drawing
	/// 
	/// This technique implements 2 important algorithms :
	///	 _ Geo MipMaps, that handle dynamic LOD of tiles based on their perceptual error on screen
	///	 _ Sparse Virtual Textures (http://www.silverspaceship.com/src/svt/), a giant texture split in pages where only the
	///		relevant (i.e. visible) parts of the texture are rendered
	///	
	/// The rendering algorithm goes like this :
	/// ----------------------------------------
	/// 1] The camera frustum is clipped against the horizontal 2D plane. The resulting polygon is used
	///  to fill an array of tiles like old times polygon fillers.
	///  
	/// 2] Each tile in the array is "painted", that is, if it does not exist, it is created and added to the
	///  list of new tiles. If the tile already exists then its time stamp is updated to the current time
	///  stamp (incremented each frame).
	///  
	/// 3] Each new tile then renders its height texture somewhere in the global TPage that contains the height
	///  textures of all valid tiles. The tile is then added to the list of used tiles and can be rendered.
	///  
	/// 4] If a tile's time stamp is older than a predefined value, then the tile is added to the list of old
	///  tiles and recycled.
	/// 
	/// 5] The list of used tiles is then rendered each frame.
	///  => The terrain first renders its tiles (World Normal + Pixel Depth) into a buffer during the Depth Pass
	///  => A fullscreen quad is then rendered that reads the depth-pass buffer, transforms the normal into camera space
	///			and applies terrain materials that are then stored in the deferred buffers as other objects
	/// </example>
	public class DeferredRenderingTerrain : DeferredRenderTechnique, IDepthPassRenderable
	{
		#region CONSTANTS

		protected const float	TERRAIN_RESOLUTION = 0.5f;				// The resolution of the terrain (in WORLD units) => each vertex will be separated by that value at maximum resolution
		protected const int		TILE_MAX_LEVEL = 6;						// The maximum amount of subdivisions (PowerOfTwo) for the max LOD
		protected const int		TILE_MAX_SUBDIV = 1 << TILE_MAX_LEVEL;	// The maximum amount of subdivisions per side
		protected const int		TILE_MAX_VERTEX = TILE_MAX_SUBDIV + 1;	// The maximum amount of vertices per side
		protected const float	TILE_SIZE = TERRAIN_RESOLUTION * TILE_MAX_SUBDIV;	// The size of a tile in WORLD units

		protected const float	TERRAIN_LOD_TOLERANCE = 4.0f;			// The tolerance in pixels before we can step to an inferior LOD

		protected const int		TPAGE_TILES_COUNT = 32;					// The amount of tiles that can fit in one TPage dimension
																		// For example, with a subdivision level 6, you get (2^6+1)*32 = 2080*2080 TPage size
		protected const int		TPAGE_TOTAL_TILES_COUNT = TPAGE_TILES_COUNT*TPAGE_TILES_COUNT;
		protected const int		TPAGE_SIZE = TILE_MAX_VERTEX * TPAGE_TILES_COUNT;	// The side size of the mega TPage holding tiles

		protected const int		TILES_ARRAY_SIZE = 64;					// The dimension of the array of tiles
		protected const int		MAX_TILES_PER_FRAME = 32;				// The maximum amount of tiles that can be rendered per frame
		protected const int		FRAMES_BEFORE_TILE_RECYCLE = 50;		// The amount of frames before an unused tile is recycled

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// The class hosting informations about a terrain tile
		/// A tile always renders at the same place in the TPage but its actual position in the
		///  terrain can change to any world position
		/// </summary>
		[System.Diagnostics.DebuggerDisplay( "Tile #{m_ID} XZ={m_X},{m_Z} TPagePos={m_TPagePositionX},{m_TPagePositionY}" )]
		protected class		Tile : IDisposable
		{
			#region FIELDS

			protected DeferredRenderingTerrain	m_Owner = null;
			protected int						m_ID = -1;
			protected Tile						m_Previous = null;
			protected Tile						m_Next = null;

			protected int						m_X = -1;
			protected int						m_Z = -1;
			protected Vector2					m_CachedXZ = new Vector2();
			protected int						m_TPagePositionX = 0;
			protected int						m_TPagePositionY = 0;
			protected Vector2					m_TPageUVOffset = new Vector2();

			// LOD management
			protected int						m_LastLOD = 0;			// LOD used for previous frame
			protected int						m_LODCombination = 0;	// Combination used for LOD, depending on adjacent tiles
			protected Vector4[]					m_ErrorLODs = new Vector4[TILE_MAX_LEVEL];

			// Frame time stamp
			protected int						m_LastUsedFrameTimeStamp = -1;

			protected static int				ms_GlobalID = 0;

			#endregion

			#region PROPERTIES

			public Tile				Previous				{ get { return m_Previous; } set { m_Previous = value; } }
			public Tile				Next					{ get { return m_Next; } set { m_Next = value; } }

			public int				PositionX				{ get { return m_X; } }
			public int				PositionZ				{ get { return m_Z; } }

			public int				LOD						{ get { return m_LastLOD; } }
			public int				LODCombination			{ get { return m_LODCombination; } set { m_LODCombination = value; } }
			public int				LastUsedFrameTimeStamp	{ get { return m_LastUsedFrameTimeStamp; } set { m_LastUsedFrameTimeStamp = value; } }

			public static int		TilesCount				{ get { return ms_GlobalID; } }

			// DEBUG HELPERS
			public int				__NextTilesCount		{ get { return m_Next != null ? 1+m_Next.__NextTilesCount : 0; } }
			public int				__PreviousTilesCount	{ get { return m_Previous != null ? 1+m_Previous.__PreviousTilesCount : 0; } }
			public int				__ListTilesCount		{ get { return __PreviousTilesCount + 1 + __NextTilesCount; } }
			public bool				__VerifyListIntegrity
			{
				get
				{
					if ( m_Previous != null && m_Previous.Next != this )
						return false;
					if ( m_Next != null && m_Next.Previous != this )
						return false;

					return m_Next == null || m_Next.__VerifyListIntegrity;
				}
			}

			#endregion

			#region METHODS
		
			public Tile( DeferredRenderingTerrain _Owner )
			{
				m_Owner = _Owner;
				m_ID = ms_GlobalID++;

				// Compute the fixed position of that tile in the TPage
				m_TPagePositionX = m_ID % TPAGE_TILES_COUNT;
				m_TPagePositionY = m_ID / TPAGE_TILES_COUNT;
				m_TPageUVOffset = new Vector2( (float) m_TPagePositionX / TPAGE_TILES_COUNT, (float) m_TPagePositionY / TPAGE_TILES_COUNT );
			}

			#region IDisposable Members

			public void  Dispose()
			{
			}

			#endregion

			/// <summary>
			/// Updates the tile's position & error LODs, invalidating the cached tile texture
			/// </summary>
			/// <param name="_X"></param>
			/// <param name="_Z"></param>
			public void		UpdatePosition( int _X, int _Z, Vector4[] _ErrorLODs )
			{
				if ( _X == m_X && _Z == m_Z )
					return;	// No change...

				m_X = _X;
				m_Z = _Z;
				m_ErrorLODs = _ErrorLODs;
				m_CachedXZ.X = m_X - TILES_ARRAY_SIZE / 2;	// The actual tile position in WORLD is offset back from the center of the array of tiles
				m_CachedXZ.Y = m_Z - TILES_ARRAY_SIZE / 2;
			}

			/// <summary>
			/// Computes the LOD appropriate for this tile
			/// </summary>
			/// <param name="_CameraPosition"></param>
			/// <param name="_CameraView"></param>
			public void		ComputeTileLOD( WMath.Point _CameraPosition, WMath.Vector _CameraView )
			{
				// Compute error at current LOD
				float	CurrentError = ComputeErrorLOD( _CameraPosition, _CameraView, m_LastLOD );
				if ( CurrentError > TERRAIN_LOD_TOLERANCE )
					m_LastLOD = Math.Max( 0, m_LastLOD-1 );	// Increase LOD
				else if ( m_LastLOD < TILE_MAX_LEVEL-1 )
				{	// Check if next LOD's error is also negligible
					float	NextError = ComputeErrorLOD( _CameraPosition, _CameraView, m_LastLOD+1 );
					if ( NextError < TERRAIN_LOD_TOLERANCE )
						m_LastLOD = m_LastLOD+1;			// Decrease LOD
				}
			}

			/// <summary>
			/// Renders the tile's texture if it's dirty
			/// </summary>
			public void		RenderTileTexture( EffectPass _Pass, VariableVector _vTilePosition )
			{
				// Set the viewport so we render exactly what we need in the TPage
				m_Owner.m_Device.SetViewport(
					m_TPagePositionX * TILE_MAX_VERTEX,
					m_TPagePositionY * TILE_MAX_VERTEX,
					TILE_MAX_VERTEX,
					TILE_MAX_VERTEX,
					0.0f, 1.0f );

				// Upload the terrain position so we render the appropriate terrain chunk
				_vTilePosition.Set( m_CachedXZ );
				_Pass.Apply();

				m_Owner.m_TileQuad.Render();
			}

			/// <summary>
			/// Renders the tile using the appropriate primitive based on last computed LOD and combination
			/// </summary>
			/// <<param name="_Pass"></param>
			/// <param name="_vTilePosition"></param>
			/// <param name="_vTPageUVOffset"></param>
			public void		RenderTile( EffectPass _Pass, VariableVector _vTilePosition, VariableVector _vTPageUVOffset )
			{
				_vTilePosition.Set( m_CachedXZ );
				_vTPageUVOffset.Set( m_TPageUVOffset );
				_Pass.Apply();
				m_Owner.m_TileLODs[m_LODCombination,m_LastLOD].RenderOverride();
			}

			/// <summary>
			/// Computes the perceptive error 
			/// </summary>
			/// <param name="_CameraPosition">The current camera position</param>
			/// <param name="_CameraView">The current camera view vector</param>
			/// <param name="_LOD">The LOD at which to compute the error</param>
			/// <returns></returns>
			protected float	ComputeErrorLOD( WMath.Point _CameraPosition, WMath.Vector _CameraView, int _LOD )
			{
				// TODO: Compute actual error !
				return TERRAIN_LOD_TOLERANCE + 1.0f;	// Should always return max LOD for now...
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected Camera						m_Camera = null;

		// The terrain material and render target
		protected Material<VS_Pt4V3T2>			m_Material = null;
		protected Material<VS_P3T2>				m_TerrainMaterial = null;
		protected RenderTarget<PF_RGBA16F>		m_TerrainGeometry = null;	// The render target that will contain the World Normal (XYZ) and Depth (W)
		protected IDepthStencil					m_DepthPassDepthStencil = null;

		// The 16x16x16 noise textures for terrain generation
		protected Texture2D<PF_RGBA16F>[]		m_NoiseTextures = new Texture2D<PF_RGBA16F>[4];

		// The terrain material textures (grass, rock, etc.)
		protected Texture2D<PF_RGBA8>			m_TerrainTexture = null;
		protected Texture2D<PF_RGBA8>			m_TerrainAtlas = null;

		// Some tweakable parameters
		protected float							m_TerrainHeightOffset = -50.0f;
		protected float							m_TerrainHeightScale = 100.0f;
		protected float							m_TerrainWorldPosition2NoiseScale = 0.00015f;
		protected float							m_TextureScale = 0.1f;

		// Tiles management
		protected int							m_FrameTimeStamp = 0;	// A time stamp increased at each frame used for LRU tiles dismiss
																		// Tiles that have not been stamped for more than FRAMES_BEFORE_TILE_RECYCLE frames get recycled

		protected Tile							m_RootTileUsed = null;	// The root to the linked list of used tiles (i.e. displayable)
		protected Tile							m_RootTileFree = null;	// The root to the linked list of free tiles (i.e. available for recycling)
		protected Tile							m_RootTileNew = null;	// The root to the linked list of new tiles (i.e. the tiles that need their texture redrawn)

		protected Tile[,]						m_Tiles = new Tile[TILES_ARRAY_SIZE,TILES_ARRAY_SIZE];			// The array of contiguous tiles	
		protected Vector4[,][]					m_TileErrors = new Vector4[TILES_ARRAY_SIZE,TILES_ARRAY_SIZE][];// The pre-computed array of LOD errors

		protected RenderTarget<PF_RGBA16F>		m_TPage = null;			// The TPage containing all the rendered height tiles
		protected Primitive<VS_P3T2,short>[,]	m_TileLODs = new Primitive<VS_P3T2,short>[16,TILE_MAX_LEVEL];	// The LOD for tiles, and their combinations based on adjacent tiles' LODs

		protected Helpers.ScreenQuad			m_TileQuad = null;		// The quad used to render tiles's normal + height in the TPage
		protected Helpers.ScreenQuad			m_TerrainQuad = null;	// The quad used to render the terrain in the deferred MRTs

		#endregion

		#region PROPERTIES

		public float			Position2NoiseScale	{ get { return m_TerrainWorldPosition2NoiseScale; } set { m_TerrainWorldPosition2NoiseScale = value; } }
		public float			TerrainHeightOffset	{ get { return m_TerrainHeightOffset; } set { m_TerrainHeightOffset = value; } }
		public float			TerrainHeightScale	{ get { return m_TerrainHeightScale; } set { m_TerrainHeightScale = value; } }
		public float			TextureScale		{ get { return m_TextureScale; } set { m_TextureScale = value; } }

		[System.ComponentModel.Browsable( false )]
		public Camera			Camera
		{
			get { return m_Camera; }
			set { m_Camera = value; }
		}

		[System.ComponentModel.Browsable( false )]
		public IDepthStencil	DepthPassDepthStencil
		{
			get { return m_DepthPassDepthStencil; }
			set { m_DepthPassDepthStencil = value; }
		}

		#endregion

		#region METHODS

		public DeferredRenderingTerrain( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Terrain Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/RenderMRTTerrain.fx" ) ) );
			m_TerrainMaterial = ToDispose( new Material<VS_P3T2>( m_Device, "Terrain Geometry Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/RenderTerraingeometry.fx" ) ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the global TPage that will contain tiles' rendering of world normal + height
 			m_TPage = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Terrain Tiles TPage", TPAGE_SIZE, TPAGE_SIZE, 1 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the terrain render target that will be rendered during ZPass and that will contain Normal + Depth
 			m_TerrainGeometry = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Terrain Render Target", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 1 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the noise textures
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = CreateNoiseTexture2D( NoiseIndex );

			//////////////////////////////////////////////////////////////////////////
			// Create the terrain texture & atlas
			m_TerrainTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Terrain Texture", new System.IO.FileInfo( "Media/Terrain/Mix.png" ), 1, 2.2f ) );
			m_TerrainAtlas = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFiles( m_Device, "Terrain Textures",
				new System.IO.FileInfo[] {
					new System.IO.FileInfo( "Media/Terrain/ground_grass_1024_tile.jpg" ),	// Grass
					new System.IO.FileInfo( "Media/Terrain/rock_01.jpg" ),					// Rock
				}, 0, 2.2f ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the terrain tile primitives
			BuildGeoMipMaps();

			// Create the terrain quad made to render the tile textures
			m_TileQuad = ToDispose( new Helpers.ScreenQuad( m_Device, "Tile Quad" ) );

			// Create the terrain quad made to render the fullscreen terrain
			m_TerrainQuad = ToDispose( new Helpers.ScreenQuad( m_Device, "Terrain Quad" ) );//, m_TerrainGeometry.Width, m_TerrainGeometry.Height, false ) );
		}

		public override void	Render( int _FrameToken )
		{
#if DEBUG
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Main Pass", "Render Terrain" );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Render the terrain geometry & material in fullscreen
			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.NOWRITE_CLOSEST_OR_EQUAL );
//			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				// Assign textures to material
				CurrentMaterial.GetVariableByName( "TerrainGeometry" ).AsResource.SetResource( m_TerrainGeometry );
				CurrentMaterial.GetVariableByName( "TerrainAtlas" ).AsResource.SetResource( m_TerrainAtlas );
				CurrentMaterial.GetVariableByName( "TerrainMixTexture" ).AsResource.SetResource( m_TerrainTexture );

				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "WriteTerrainMRT" );
				CurrentMaterial.ApplyPass( 0 );
				m_TerrainQuad.Render();
			}
		}

		#region IDepthPassRenderable Members

		public void RenderDepthPass( int _FrameToken, EffectPass _Pass, VariableMatrix _vLocal2World )
		{
			//////////////////////////////////////////////////////////////////////////
			// Update the list of used/unused tiles + compute tiles LOD and border combinations
#if DEBUG
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Depth Pass", "Update Terrain Tiles" );
#endif

			UpdateTiles();

			//////////////////////////////////////////////////////////////////////////
			// Render the terrain geometry (camera normal + depth) in a specific render target
#if DEBUG
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Depth Pass", "Render Terrain Geometry" );
#endif
			using ( m_TerrainMaterial.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				m_Device.SetRenderTarget( m_TerrainGeometry, m_DepthPassDepthStencil );
				m_Device.SetViewport( 0, 0, m_TerrainGeometry.Width, m_TerrainGeometry.Height, 0.0f, 1.0f );

				m_Device.ClearRenderTarget( m_TerrainGeometry, new Color4( RendererSetupDeferred.DEPTH_BUFFER_INFINITY, 0.0f, 0.0f, 0.0f ) );

				CurrentMaterial.GetVariableByName( "TileSize" ).AsScalar.Set( TILE_SIZE );
				CurrentMaterial.GetVariableByName( "TilesTPage" ).AsResource.SetResource( m_TPage );
				CurrentMaterial.GetVariableByName( "InvTPageUVTileSize" ).AsScalar.Set( (float) TILE_MAX_VERTEX / (TPAGE_SIZE+1) );

				VariableVector	vTilePosition = CurrentMaterial.GetVariableByName( "TilePosition" ).AsVector;
				VariableVector	vTPageUVOffset = CurrentMaterial.GetVariableByName( "TPageUVOffset" ).AsVector;

				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				Tile	Current = m_RootTileUsed;
				while ( Current != null )
				{
					Current.RenderTile( Pass, vTilePosition, vTPageUVOffset );
					Current = Current.Next;
				}

				// Restore previous render target state : render only to the depth stencil 
				m_Device.SetRenderTarget( null as IRenderTarget, m_DepthPassDepthStencil );
			}
		}

		#endregion

		/// <summary>
		/// Performs update for the tiles :
		///	1) Uses current camera frustum to paint the list of required tiles and creates the missing tiles
		///	2) Render the new tiles' textures (up to a limited amount per frame)
		///	3) "Destroys" tiles that have not been used for N frames
		///	4) Computes the required LOD & adjacent LOD combinations to render the tiles
		/// </summary>
		protected void	UpdateTiles()
		{
			m_FrameTimeStamp++;

			//////////////////////////////////////////////////////////////////////////
			// 1] Paint the used tiles
			Matrix	Camera2World = m_Camera.Camera2World;
			Vector2	CamPos = new Vector2( Camera2World[3,0], Camera2World[3,2] );
			Vector2	CameraDir = new Vector2( Camera2World[2,0], Camera2World[2,2] );
			CameraDir.Normalize();
			if ( float.IsNaN( CameraDir.X ) )
				return;
			Vector2	CameraRight = new Vector2( Camera2World[0,0], Camera2World[0,2] );
			CameraRight.Normalize();

			float	FOVFactor = 1.5f;	// Cover a little more FOV
			float	TanHalfFOV = m_Camera.AspectRatio * (float) Math.Tan( FOVFactor * 0.5f * m_Camera.PerspectiveFOV );

			// 1.1] Build the frustum's 2D projection
			float	ViewReach = 200.0f;
			// TODO: Do real frustum cut with horizontal plane...
			Vector2[]	Vertices = new Vector2[3];
			Vertices[0] = CamPos;
			Vertices[1] = CamPos + m_Camera.Far * (CameraDir + TanHalfFOV * CameraRight);
			Vertices[2] = CamPos + m_Camera.Far * (CameraDir - TanHalfFOV * CameraRight);

			// 1.2] Fill the area
			FillTiles( Vertices );

			// 1.3] Also add a square surrounding the camera
			{
				int	D = (int) Math.Floor( ViewReach / TILE_SIZE );
				int	CX = TILES_ARRAY_SIZE / 2 + (int) Math.Floor( CamPos.X / TILE_SIZE );
				int	X0 = Math.Max( CX - D, 0 );
				int	X1 = Math.Min( CX + D, TILES_ARRAY_SIZE-1 );
				int	CY = TILES_ARRAY_SIZE / 2 + (int) Math.Floor( CamPos.Y / TILE_SIZE );
				int	Y0 = Math.Max( CY - D, 0 );
				int	Y1 = Math.Min( CY + D, TILES_ARRAY_SIZE-1 );
				for ( int Y=Y0; Y < Y1; Y++ )
					for ( int X=X0; X < X1; X++ )
						PaintTile( X, Y );
			}

			//////////////////////////////////////////////////////////////////////////
			// 2] Render the new terrain tiles' textures
			if ( m_RootTileNew != null )
				using ( m_Material.UseLock() )
				{
					m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
					m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );

					m_Device.SetRenderTarget( m_TPage );	// Update the TPage

					CurrentMaterial.GetVariableByName( "TileSize" ).AsScalar.Set( TILE_SIZE );

					CurrentMaterial.GetVariableByName( "TerrainWorldPosition2NoiseScale" ).AsScalar.Set( m_TerrainWorldPosition2NoiseScale );
					CurrentMaterial.GetVariableByName( "TerrainHeightOffset" ).AsScalar.Set( m_TerrainHeightOffset );
					CurrentMaterial.GetVariableByName( "TerrainHeightScale" ).AsScalar.Set( m_TerrainHeightScale );
					CurrentMaterial.GetVariableByName( "TextureScale" ).AsScalar.Set( m_TextureScale );


					// I have to admit I don't have the courage to understand why I need this particular UV factor that makes no sense
					// I'm tired of DirectX and their "so called" trivial way of mapping texels to pixels : it NEVER works. Period.
					CurrentMaterial.GetVariableByName( "TileUVFactor" ).AsScalar.Set( (float) (TILE_MAX_VERTEX-1) / (TILE_MAX_VERTEX-2) );
					
					CurrentMaterial.GetVariableByName( "NoiseTexture0" ).AsResource.SetResource( m_NoiseTextures[0] );
					CurrentMaterial.GetVariableByName( "NoiseTexture1" ).AsResource.SetResource( m_NoiseTextures[1] );
					CurrentMaterial.GetVariableByName( "NoiseTexture2" ).AsResource.SetResource( m_NoiseTextures[2] );
					CurrentMaterial.GetVariableByName( "NoiseTexture3" ).AsResource.SetResource( m_NoiseTextures[3] );

					VariableVector	vTilePosition = CurrentMaterial.GetVariableByName( "TilePosition" ).AsVector;

					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "RenderTileTexture" );
					EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

					int		NewTilesCount = 0;
					while ( m_RootTileNew != null && NewTilesCount < MAX_TILES_PER_FRAME )
					{
						// Compute new tile's texture
						m_RootTileNew.RenderTileTexture( Pass, vTilePosition );
						NewTilesCount++;

						// Remove it from new tiles and add it to used tiles
						Tile	NewTile = m_RootTileNew;
						m_RootTileNew = m_RootTileNew.Next;
						if ( m_RootTileNew != null )
							m_RootTileNew.Previous = null;

						if ( m_RootTileUsed != null )
							m_RootTileUsed.Previous = NewTile;
						NewTile.Next = m_RootTileUsed;
						m_RootTileUsed = NewTile;
					}
				}

			//////////////////////////////////////////////////////////////////////////
			// 3] Browse the used tiles and recycle those that are too old
			// Compute required LOD for tiles still in use
			WMath.Point		Pos = new WMath.Point( Camera2World[3,0], Camera2World[3,1], Camera2World[3,2] );
			WMath.Vector	View = new WMath.Vector( Camera2World[2,0], Camera2World[2,1], Camera2World[2,2] );

			Tile	Current = m_RootTileUsed;
			while ( Current != null )
			{
				Tile	T = Current;
				Current = Current.Next;
				if ( m_FrameTimeStamp - T.LastUsedFrameTimeStamp < FRAMES_BEFORE_TILE_RECYCLE )
				{	// That one is still used...
					T.ComputeTileLOD( Pos, View );	// Compute individual LOD
					continue;
				}

				// Unlink the tile
				if ( T.Previous != null )
					T.Previous.Next = T.Next;
				else
					m_RootTileUsed = T.Next;
				if ( T.Next != null )
					T.Next.Previous = T.Previous;

				// and recycle...
				T.Previous = null;
				T.Next = m_RootTileFree;
				if ( m_RootTileFree != null )
					m_RootTileFree.Previous = T;
				m_RootTileFree = T;

				// Also clear the position where it was before
				m_Tiles[T.PositionX,T.PositionZ] = null;
			}

			// Compute adjacent tiles' combination
			Current = m_RootTileUsed;
			while ( Current != null )
			{
				int	X = Current.PositionX;
				int	Z = Current.PositionZ;

				int	Combination = 0;
				Combination |= X > 0 && m_Tiles[X-1,Z] != null && m_Tiles[X-1,Z].LOD > Current.LOD ? 1 : 0;
				Combination |= X < TILES_ARRAY_SIZE-1 && m_Tiles[X+1,Z] != null && m_Tiles[X+1,Z].LOD > Current.LOD ? 2 : 0;
				Combination |= Z > 0 && m_Tiles[X,Z-1] != null && m_Tiles[X,Z-1].LOD > Current.LOD ? 4 : 0;
				Combination |= Z < TILES_ARRAY_SIZE-1 && m_Tiles[X,Z+1] != null && m_Tiles[X,Z+1].LOD > Current.LOD ? 8 : 0;

				Current.LODCombination = Combination;
				Current = Current.Next;
			}
		}

		/// <summary>
		/// "Fills" a triangle of terrain tiles
		/// </summary>
		/// <param name="_Vertices">An array of CCW vertices in WORLD space</param>
		protected void	FillTiles( Vector2[] _Vertices )
		{
			// We choose the Y axis for the scanlines
			// 1] Transform the vertices into TILE space and determine minimum Y
			Vector2[]	Vertices = new Vector2[2*_Vertices.Length];	// A looping array
			int			LIndex = -1;
			float		MinY = +float.MaxValue;

			for ( int VertexIndex=0; VertexIndex < _Vertices.Length; VertexIndex++ )
			{
				Vertices[VertexIndex] = new Vector2( TILES_ARRAY_SIZE / 2, TILES_ARRAY_SIZE / 2 ) + _Vertices[VertexIndex] / TILE_SIZE;
				Vertices[_Vertices.Length+VertexIndex] = Vertices[VertexIndex];

				if ( Vertices[VertexIndex].Y < MinY )
				{
					MinY = Vertices[VertexIndex].Y;
					LIndex = VertexIndex;
				}
			}

			int	RIndex = LIndex + _Vertices.Length;

			// 2] Draw
			Vector2	L = Vector2.Zero, R = Vector2.Zero;
			float	LSlopeX = 0.0f, RSlopeX = 0.0f;
			int		X, iRX, iLY = 0, iRY = 0;
			int		iLDy = 0, iRDy = 0;

			while ( iLY < TILES_ARRAY_SIZE )
			{
				// 2.1] Recompute left & right slopes if needed
				while ( iLDy <= 0 )
				{
					L = Vertices[LIndex];
					Vector2	Next = Vertices[LIndex+1];
					iLY = (int) Math.Ceiling( L.Y );
					iLDy = (int) Math.Ceiling( Next.Y ) - iLY;

					// Compute slope
					Vector2	LD = Next - L;
					LSlopeX = LD.X / LD.Y;
					L.X += (iLY - L.Y) * LSlopeX;	// Sub-pixel accuracy
					LIndex++;
					if ( LIndex > RIndex )
						return;	// We're done !
				}
				while ( iRDy <= 0 )
				{
					R = Vertices[RIndex];
					Vector2	Next = Vertices[RIndex-1];
					iRY = (int) Math.Ceiling( R.Y );
					iRDy = (int) Math.Ceiling( Next.Y ) - iRY;

					// Compute slope
					Vector2	RD = Next - R;
					RSlopeX = RD.X / RD.Y;
					R.X += (iRY - R.Y) * RSlopeX;	// Sub-pixel accuracy
					RIndex--;
					if ( RIndex < LIndex )
						return;	// We're done !
				}

				// 2.2] Draw scanline
				if ( iLY >= 0 )
				{
					X = (int) Math.Floor( L.X );
					X = Math.Max( 0, Math.Min( TILES_ARRAY_SIZE-1, X ) );
					iRX = (int) Math.Ceiling( R.X );
					iRX = Math.Max( 0, Math.Min( TILES_ARRAY_SIZE-1, iRX ) );

					for ( ; X < iRX; X++ )
						PaintTile( X, iLY );
				}

				// 2.3] March one scanline
				L.X += LSlopeX;
				iLDy--;
				R.X += RSlopeX;
				iRDy--;
				iLY++;
				iRY++;
			}
		}

		/// <summary>
		/// "Paints" a terrain tile
		/// (i.e. creates the tile if it does not exist and updates its time stamp)
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Z"></param>
		protected void	PaintTile( int _X, int _Z )
		{
			if ( m_Tiles[_X,_Z] == null )
				m_Tiles[_X,_Z] = CreateTile( _X, _Z );

			Tile	T = m_Tiles[_X,_Z];
			if ( T != null )	// This test is here because CreateTile() can return null if the TPage is full...
				T.LastUsedFrameTimeStamp = m_FrameTimeStamp;	// Update time stamp for that tile
		}

		/// <summary>
		/// Creates or recycles a tile at the given position
		/// </summary>
		/// <param name="_X">Tile position in X</param>
		/// <param name="_Z">Tile position in Z</param>
		/// <returns></returns>
		protected Tile	CreateTile( int _X, int _Z )
		{
			Tile	Result = null;
			if ( m_RootTileFree != null )
			{	// Recycle a cached tile
				Result = m_RootTileFree;
				if ( m_RootTileFree.Next != null )
					m_RootTileFree.Next.Previous = null;
				m_RootTileFree = m_RootTileFree.Next;
			}
			else
			{	// Create a brand new tile
				if ( Tile.TilesCount == TPAGE_TOTAL_TILES_COUNT )
					return null;	// No tile is available right now... The TPage is full (not a good sign ! We should change the recycling rate here...)

				Result = ToDispose( new Tile( this ) );
			}

			// Update its position & errors
			Result.UpdatePosition( _X, _Z, m_TileErrors[_X,_Z] );

			// Add it as NEW tile (so its texture is rebuilt ASAP)
			if ( m_RootTileNew != null )
				m_RootTileNew.Previous = Result;
			Result.Previous = null;
			Result.Next = m_RootTileNew;
			m_RootTileNew = Result;

			return Result;
		}

		/// <summary>
		/// Creates a 2D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture2D<PF_RGBA16F>	CreateNoiseTexture2D( int _NoiseIndex )
		{
			const int	NOISE_SIZE = 32;

			// Build the slice filled with noise
			Random	RNG = new Random( 1+_NoiseIndex );
			float[,]	Noise = new float[NOISE_SIZE,NOISE_SIZE];
			for ( int Y=0; Y < NOISE_SIZE; Y++ )
				for ( int X=0; X < NOISE_SIZE; X++ )
					Noise[X,Y] = (float) RNG.NextDouble();

			// Build the image and the texture from it...
			using ( Image<PF_RGBA16F>	NoiseImage = new Image<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_SIZE, NOISE_SIZE,
				( int _X, int _Y, ref Vector4 _Color ) =>
				{																			// (XY)
					_Color.X = Noise[_X,_Y];												// (00)
					_Color.Y = Noise[(_X+1) & (NOISE_SIZE-1),_Y];							// (10)
					_Color.Z = Noise[_X,(_Y+1) & (NOISE_SIZE-1)];							// (01)
					_Color.W = Noise[(_X+1) & (NOISE_SIZE-1),(_Y+1) & (NOISE_SIZE-1)];		// (11)
				}, 0 ) )
			{
				return ToDispose( new Texture2D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}

		/// <summary>
		/// Creates a 3D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateNoiseTexture3D( int _NoiseIndex )
		{
			const int	NOISE_SIZE = 16;

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

		/// <summary>
		/// Builds all the LODs of terrain tiles, and their variations based on neighbor tiles' LODs
		/// </summary>
		protected void	BuildGeoMipMaps()
		{
			for ( int LOD=0; LOD < TILE_MAX_LEVEL; LOD++ )
			{
				int		SubdivisionsCount = 1 << (TILE_MAX_LEVEL-LOD);
				int		VerticesCount = SubdivisionsCount+1;
				int		InternalSubdivisionsCount = SubdivisionsCount-2;

				VS_P3T2[]	Vertices = new VS_P3T2[VerticesCount*VerticesCount];
				List<int>	Indices = new List<int>();

				// Build vertices
				for ( int Y=0; Y < VerticesCount; Y++ )
				{
					bool	IsBorderY = Y == 0 || Y == VerticesCount-1;
					for ( int X=0; X < VerticesCount; X++ )
					{
						float	IsBorder = IsBorderY || X == 0 || X == VerticesCount-1 ? 0.0f : 1.0f;
						Vertices[VerticesCount*Y+X] = new VS_P3T2()
							{	Position=new Vector3( (float) X / (VerticesCount-1), 0.0f, (float) Y / (VerticesCount-1) ),
								UV=new Vector2( IsBorder, 1.0f )
							};
					}
				}

				// Build indices for internal triangles
				for ( int Y=0; Y < InternalSubdivisionsCount; Y++ )
					for ( int X=0; X < InternalSubdivisionsCount; X++ )
					{
						// First triangle
						Indices.Add( VerticesCount * (1+Y) + (1+X) );
						Indices.Add( VerticesCount * (1+Y+1) + (1+X) );
						Indices.Add( VerticesCount * (1+Y+1) + (1+X+1) );
						// Second triangle
						Indices.Add( VerticesCount * (1+Y) + (1+X) );
						Indices.Add( VerticesCount * (1+Y+1) + (1+X+1) );
						Indices.Add( VerticesCount * (1+Y) + (1+X+1) );
					}

				// Build borders
				for ( int BorderCase=0; BorderCase < 16; BorderCase++ )
				{
					//////////////////////////////////////////////////////////////////////////
					// Left border
					if ( (BorderCase & 1) == 0 )
					{	// This LOD

						// Top left triangle
						Indices.Add( VerticesCount * (0) + (0) );
						Indices.Add( VerticesCount * (1) + (0) );
						Indices.Add( VerticesCount * (1) + (1) );

						// Vertical band triangles
						for ( int Y=0; Y < InternalSubdivisionsCount; Y++ )
						{
							// First triangle
							Indices.Add( VerticesCount * (1+Y) + (0) );
							Indices.Add( VerticesCount * (1+Y+1) + (0) );
							Indices.Add( VerticesCount * (1+Y+1) + (1) );
							// Second triangle
							Indices.Add( VerticesCount * (1+Y) + (0) );
							Indices.Add( VerticesCount * (1+Y+1) + (1) );
							Indices.Add( VerticesCount * (1+Y) + (1) );
						}

						// Bottom left triangle
						Indices.Add( VerticesCount * (VerticesCount-2) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (1) );
					}
					else
					{	// Lower LOD

						// Top left triangles
						Indices.Add( VerticesCount * (0) + (0) );
						Indices.Add( VerticesCount * (2) + (0) );
						Indices.Add( VerticesCount * (2) + (1) );

						Indices.Add( VerticesCount * (0) + (0) );
						Indices.Add( VerticesCount * (2) + (1) );
						Indices.Add( VerticesCount * (1) + (1) );

						// Vertical band triangles
						for ( int Y=1; Y < InternalSubdivisionsCount-1; Y+=2 )
						{
							// First triangle
							Indices.Add( VerticesCount * (1+Y) + (0) );
							Indices.Add( VerticesCount * (1+Y+2) + (0) );
							Indices.Add( VerticesCount * (1+Y+2) + (1) );
							// Second triangle
							Indices.Add( VerticesCount * (1+Y) + (0) );
							Indices.Add( VerticesCount * (1+Y+2) + (1) );
							Indices.Add( VerticesCount * (1+Y+1) + (1) );
							// Third triangle
							Indices.Add( VerticesCount * (1+Y) + (0) );
							Indices.Add( VerticesCount * (1+Y+1) + (1) );
							Indices.Add( VerticesCount * (1+Y) + (1) );
						}

						// Bottom left triangles
						Indices.Add( VerticesCount * (VerticesCount-3) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (1) );

						Indices.Add( VerticesCount * (VerticesCount-3) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (1) );
						Indices.Add( VerticesCount * (VerticesCount-3) + (1) );
					}

					//////////////////////////////////////////////////////////////////////////
					// Right border
					if ( (BorderCase & 2) == 0 )
					{	// This LOD

						// Top right triangle
						Indices.Add( VerticesCount * (0) + (VerticesCount-1) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-1) );

						// Vertical band triangles
						for ( int Y=0; Y < InternalSubdivisionsCount; Y++ )
						{
							// First triangle
							Indices.Add( VerticesCount * (1+Y) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+1) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+1) + (VerticesCount-1) );
							// Second triangle
							Indices.Add( VerticesCount * (1+Y) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+1) + (VerticesCount-1) );
							Indices.Add( VerticesCount * (1+Y) + (VerticesCount-1) );
						}

						// Bottom right triangle
						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-1) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-1) );
					}
					else
					{	// Lower LOD

						// Top right triangles
						Indices.Add( VerticesCount * (0) + (VerticesCount-1) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (2) + (VerticesCount-1) );

						Indices.Add( VerticesCount * (1) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (2) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (2) + (VerticesCount-1) );

						// Vertical band triangles
						for ( int Y=1; Y < InternalSubdivisionsCount-1; Y+=2 )
						{
							// First triangle
							Indices.Add( VerticesCount * (1+Y) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+2) + (VerticesCount-1) );
							Indices.Add( VerticesCount * (1+Y) + (VerticesCount-1) );
							// Second triangle
							Indices.Add( VerticesCount * (1+Y) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+1) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+2) + (VerticesCount-1) );
							// Third triangle
							Indices.Add( VerticesCount * (1+Y+1) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+2) + (VerticesCount-2) );
							Indices.Add( VerticesCount * (1+Y+2) + (VerticesCount-1) );
						}

						// Bottom right triangles
						Indices.Add( VerticesCount * (VerticesCount-3) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-1) );

						Indices.Add( VerticesCount * (VerticesCount-3) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-1) );
						Indices.Add( VerticesCount * (VerticesCount-3) + (VerticesCount-1) );
					}

					//////////////////////////////////////////////////////////////////////////
					// Top border
					if ( (BorderCase & 4) == 0 )
					{	// This LOD

						// Top left triangle
						Indices.Add( VerticesCount * (0) + (0) );
						Indices.Add( VerticesCount * (1) + (1) );
						Indices.Add( VerticesCount * (0) + (1) );

						// Horizontal band triangles
						for ( int X=0; X < InternalSubdivisionsCount; X++ )
						{
							// First triangle
							Indices.Add( VerticesCount * (0) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X+1) );
							// Second triangle
							Indices.Add( VerticesCount * (0) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X+1) );
							Indices.Add( VerticesCount * (0) + (1+X+1) );
						}

						// Top right triangle
						Indices.Add( VerticesCount * (0) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (0) + (VerticesCount-1) );
					}
					else
					{	// Lower LOD

						// Top left triangles
						Indices.Add( VerticesCount * (0) + (0) );
						Indices.Add( VerticesCount * (1) + (1) );
						Indices.Add( VerticesCount * (1) + (2) );

						Indices.Add( VerticesCount * (0) + (0) );
						Indices.Add( VerticesCount * (1) + (2) );
						Indices.Add( VerticesCount * (0) + (2) );

						// Horizontal band triangles
						for ( int X=1; X < InternalSubdivisionsCount-1; X+=2 )
						{
							// First triangle
							Indices.Add( VerticesCount * (0) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X+1) );
							// Second triangle
							Indices.Add( VerticesCount * (0) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X+1) );
							Indices.Add( VerticesCount * (1) + (1+X+2) );
							// Third triangle
							Indices.Add( VerticesCount * (0) + (1+X) );
							Indices.Add( VerticesCount * (1) + (1+X+2) );
							Indices.Add( VerticesCount * (0) + (1+X+2) );
						}

						// Top right triangles
						Indices.Add( VerticesCount * (0) + (VerticesCount-3) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-3) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-2) );

						Indices.Add( VerticesCount * (0) + (VerticesCount-3) );
						Indices.Add( VerticesCount * (1) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (0) + (VerticesCount-1) );
					}
	
					//////////////////////////////////////////////////////////////////////////
					// Bottom border
					if ( (BorderCase & 8) == 0 )
					{	// This LOD

						// Bottom left triangle
						Indices.Add( VerticesCount * (VerticesCount-2) + (1) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (1) );

						// Horizontal band triangles
						for ( int X=0; X < InternalSubdivisionsCount; X++ )
						{
							// First triangle
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X+1) );
							// Second triangle
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X+1) );
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X+1) );
						}

						// Bottom right triangle
						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-2) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-1) );
					}
					else
					{	// Lower LOD

						// Bottom left triangles
						Indices.Add( VerticesCount * (VerticesCount-2) + (1) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (0) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (2) );

						Indices.Add( VerticesCount * (VerticesCount-2) + (1) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (2) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (2) );

						// Horizontal band triangles
						for ( int X=1; X < InternalSubdivisionsCount-1; X+=2 )
						{
							// First triangle
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X+1) );
							// Second triangle
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X+2) );
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X+1) );
							// Third triangle
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X+1) );
							Indices.Add( VerticesCount * (VerticesCount-1) + (1+X+2) );
							Indices.Add( VerticesCount * (VerticesCount-2) + (1+X+2) );
						}

						// Bottom right triangles
						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-3) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-3) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-1) );

						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-3) );
						Indices.Add( VerticesCount * (VerticesCount-1) + (VerticesCount-1) );
						Indices.Add( VerticesCount * (VerticesCount-2) + (VerticesCount-2) );
					}

					// Build the primitive
					short[]	IndicesArray = new short[Indices.Count];
					for ( int Index=0; Index < Indices.Count; Index++ )
						IndicesArray[Index] = (short) Indices[Index];

					m_TileLODs[BorderCase,LOD] = ToDispose( new Primitive<VS_P3T2,short>( m_Device, "TileLOD[" + LOD + "," + BorderCase + "]", PrimitiveTopology.TriangleList, Vertices, IndicesArray ) );
				}
			}



			// DEBUG
			// Build a simple tile for level 0
			{
// 				VS_P3T2[]	PipoVertices = new VS_P3T2[4];
// 				short[]		PipoIndices = new short[2*3];
// 				PipoVertices[0] = new VS_P3T2() { Position=new Vector3( 0.0f, 0.0f, 0.0f ), UV=new Vector2( 1.0f, 1.0f ) };
// 				PipoVertices[1] = new VS_P3T2() { Position=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ) };
// 				PipoVertices[2] = new VS_P3T2() { Position=new Vector3( 1.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ) };
// 				PipoVertices[3] = new VS_P3T2() { Position=new Vector3( 1.0f, 0.0f, 0.0f ), UV=new Vector2( 1.0f, 1.0f ) };
// 				PipoIndices[3*0+0] = 0;
// 				PipoIndices[3*0+1] = 1;
// 				PipoIndices[3*0+2] = 2;
// 				PipoIndices[3*1+0] = 0;
// 				PipoIndices[3*1+1] = 2;
// 				PipoIndices[3*1+2] = 3;

				VS_P3T2[]	PipoVertices = new VS_P3T2[TILE_MAX_VERTEX*TILE_MAX_VERTEX];
				short[]		PipoIndices = new short[3*2*TILE_MAX_SUBDIV*TILE_MAX_SUBDIV];

				for ( int Y=0; Y < TILE_MAX_VERTEX; Y++ )
					for ( int X=0; X < TILE_MAX_VERTEX; X++ )
						PipoVertices[TILE_MAX_VERTEX*Y+X] = new VS_P3T2()
						{
							Position = new Vector3( (float) X / (TILE_MAX_VERTEX-1), 0.0f, (float) Y / (TILE_MAX_VERTEX-1) ),
							UV = new Vector2( 1.0f, 1.0f )
						};

				for ( int Y=0; Y < TILE_MAX_SUBDIV; Y++ )
					for ( int X=0; X < TILE_MAX_SUBDIV; X++ )
					{
						PipoIndices[2*3*(TILE_MAX_SUBDIV*Y+X) + 3*0 + 0] = (short) (TILE_MAX_VERTEX*(Y+0) + (X+0));
						PipoIndices[2*3*(TILE_MAX_SUBDIV*Y+X) + 3*0 + 1] = (short) (TILE_MAX_VERTEX*(Y+1) + (X+0));
						PipoIndices[2*3*(TILE_MAX_SUBDIV*Y+X) + 3*0 + 2] = (short) (TILE_MAX_VERTEX*(Y+1) + (X+1));
						PipoIndices[2*3*(TILE_MAX_SUBDIV*Y+X) + 3*1 + 0] = (short) (TILE_MAX_VERTEX*(Y+0) + (X+0));
						PipoIndices[2*3*(TILE_MAX_SUBDIV*Y+X) + 3*1 + 1] = (short) (TILE_MAX_VERTEX*(Y+1) + (X+1));
						PipoIndices[2*3*(TILE_MAX_SUBDIV*Y+X) + 3*1 + 2] = (short) (TILE_MAX_VERTEX*(Y+0) + (X+1));
					}

				m_TileLODs[0,0] = ToDispose( new Primitive<VS_P3T2,short>( m_Device, "PipoTile", PrimitiveTopology.TriangleList, PipoVertices, PipoIndices ) );
			}
			// DEBUG
		}

		/// <summary>
		/// Bakes all the error data for all LODs of all used terrain tiles (i.e. a Vector4 per LOD per tile, to be saved)
		/// </summary>
		protected void	BakeTerrainErrors()
		{
			// TODO !
		}

		#endregion
	}
}
