//#define VOXEL_CLOUDS
#define LOAD_PRECOMP_NOISE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

using Nuaj;
using Nuaj.Cirrus;
using Nuaj.Cirrus.Utility;

namespace Demo
{
	public partial class DemoForm : Form, IShaderInterfaceProvider, IMaterialLoader
	{
		#region CONSTANTS

		protected const int		TERRAIN_TILES_COUNT = 512;
		protected const float	TERRAIN_SCALE_HORIZONTAL = 1000.0f;
		protected const float	TERRAIN_OFFSET_VERTICAL = -80.0f;
		protected const float	TERRAIN_SCALE_VERTICAL = 100.0f;
		protected const float	TERRAIN_NOISE_SCALE = 0.001f;

		protected const int		SH_NOISE_ENCODING_SAMPLES_COUNT = 32;

		#endregion

		#region NESTED TYPES

		protected class	INoise3D : ShaderInterfaceBase
		{
			[Semantic( "NOISE3D_TEX0" )]
			public Texture3D<PF_RGBA16F>	Noise0		{ set { SetResource( "NOISE3D_TEX0", value ); } }
			[Semantic( "NOISE3D_TEX1" )]
			public Texture3D<PF_RGBA16F>	Noise1		{ set { SetResource( "NOISE3D_TEX1", value ); } }
			[Semantic( "NOISE3D_TEX2" )]
			public Texture3D<PF_RGBA16F>	Noise2		{ set { SetResource( "NOISE3D_TEX2", value ); } }
			[Semantic( "NOISE3D_TEX3" )]
			public Texture3D<PF_RGBA16F>	Noise3		{ set { SetResource( "NOISE3D_TEX3", value ); } }
			[Semantic( "LARGE_NOISE3D_TEX" )]
			public ITexture3D				LargeNoise	{ set { SetResource( "LARGE_NOISE3D_TEX", value ); } }
		}

		#endregion

		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
#if VOXEL_CLOUDS
		protected RenderTechniqueVoxelClouds	m_RenderTechniqueClouds = null;	// The advanced render technique for mesh clouds
#else
		protected RenderTechniqueVolumeClouds	m_RenderTechniqueClouds = null;	// The advanced render technique for volume clouds
#endif
		protected RenderTechniquePostProcessToneMappingFilmic	m_ToneMapping = null;

		protected RenderTarget<PF_RGBA16F>[]	m_RenderTargets = new RenderTarget<PF_RGBA16F>[2];

		protected float						m_SkyIntensity = 4.0f;

		// Ground terrain (no fancy stuff here, just a large mesh to test ZBuffer interaction)
		protected Material<VS_P3N3>			m_MaterialGround = null;
		protected Texture2D<PF_RGBA8>		m_TerrainTexture = null;
		protected Primitive<VS_P3N3,int>	m_Terrain = null;

		// Gnome
		protected Material<VS_P3N3T2>		m_MaterialGnome = null;
		protected Texture2D<PF_RGBA8>		m_GnomeTexture = null;
		protected Primitive<VS_P3N3T2,int>	m_Gnome = null;

		// The 16x16x16 noise textures
		protected Texture3D<PF_RGBA16F>[]	m_NoiseTextures = new Texture3D<PF_RGBA16F>[4];
		protected Vector4[][,,]				m_CPUNoiseTextures = new Vector4[4][,,];
//		protected Texture3D<PF_RGBA16F>		m_LargeNoiseTexture = null;
		protected Texture3D<PF_R16F>		m_LargeNoiseTexture2 = null;

		protected StringBuilder				m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region PROPERTIES

		public float			SkyIntensity
		{
			get { return m_SkyIntensity; }
			set { m_SkyIntensity = value; }
		}

		#endregion

		#region METHODS

		public DemoForm()
		{
			InitializeComponent();

//			BuildCaseTable();	// This table has holes because of missing cases :(
//			BuildCaseTable2();

			BuildNoiseArray();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( panelOutput.Width, panelOutput.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, panelOutput, true ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}

			//////////////////////////////////////////////////////////////////////////
			// Register ourselves as shader interface provider
			m_Device.DeclareShaderInterface( typeof(INoise3D) );
			m_Device.RegisterShaderInterfaceProvider( typeof(INoise3D), this );				// Register the INoise3D interface

			//////////////////////////////////////////////////////////////////////////
			// Create our scene render target, with mip-maps for tone mapping
			m_RenderTargets[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Render Target 0 (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );
			m_RenderTargets[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "Render Target 1 (HDR)", m_Device.DefaultRenderTarget.Width, m_Device.DefaultRenderTarget.Height, 0 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the noise textures
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = CreateNoiseTexture( NoiseIndex, out m_CPUNoiseTextures[NoiseIndex] );

//			m_LargeNoiseTexture = CreateLargeNoiseTexture();
			m_LargeNoiseTexture2 = CreateLargeNoiseTexture2();

			//////////////////////////////////////////////////////////////////////////
			// Create the renderer
			RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
			{
				CameraFOV = 45.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 1000.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

#if VOXEL_CLOUDS
			// Setup the default lights
//			m_Renderer.MainLight.Color = 10 * new Color4( 1, 1, 1, 1 );
			m_Renderer.MainLight.Color = 1 * new Color4( 1, 1, 1, 1 );
			m_Renderer.MainLight.Direction = new Vector3( 0.001f, 1.0f, 0.0f );
//			m_Renderer.MainLight.Direction = new Vector3( 0.5f, -2.0f, 1.0f );
			m_Renderer.ToneMappingFactor = 0.9f;

			m_RenderTechniqueClouds = ToDispose( new RenderTechniqueVoxelClouds( m_Device, "Voxel Clouds Render Technique" ) );
			m_RenderTechniqueClouds.Light = m_Renderer.MainLight;
			m_RenderTechniqueClouds.Camera = m_Renderer.Camera;
#else
			m_RenderTechniqueClouds = ToDispose( new RenderTechniqueVolumeClouds( m_Renderer, m_RenderTargets, "Volume Clouds Render Technique" ) );
			m_RenderTechniqueClouds.Light = m_Renderer.MainLight;
#endif
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).InsertTechnique( 0, m_RenderTechniqueClouds );	// Insert our technique at the beginning


			//////////////////////////////////////////////////////////////////////////
			// Tone mapping technique
			m_ToneMapping = ToDispose( new RenderTechniquePostProcessToneMappingFilmic( m_Renderer.Device, "Tone Mapping", this, false ) );
			m_ToneMapping.SourceImage = m_RenderTargets[1];
			m_ToneMapping.TargetImage = m_Device.DefaultRenderTarget;
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).AddTechnique( m_ToneMapping );

			//////////////////////////////////////////////////////////////////////////
			// Create the terrain
			m_MaterialGround = ToDispose( new Material<VS_P3N3>( m_Device, "Ground Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Clouds/GroundDisplay.fx" ) ) );
			m_TerrainTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Ground Texture", new System.IO.FileInfo( "./Media/Terrain/ground_grass_1024_tile.jpg" ), 0, 1.0f ) );
			CreateTerrainPrimitive();

			//////////////////////////////////////////////////////////////////////////
			// Create the gnome
			m_MaterialGnome = ToDispose( new Material<VS_P3N3T2>( m_Device, "Gnome Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Clouds/GnomeDisplay.fx" ) ) );
			m_GnomeTexture = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "Gnome Texture", new System.IO.FileInfo( "./Media/Characters/Gael.png" ), 0, 1.0f ) );
			CreateGnomePrimitive();

			BuildHierarchyTree();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			//////////////////////////////////////////////////////////////////////////
			// Create a camera manipulator
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
//			CamManip.InitializeCamera( new Vector3( 0.0f, 16.0f, 45.0f ), new Vector3( 0.0f, 16.0f, 0.0f ), Vector3.UnitY );
#if VOXEL_CLOUDS
			CamManip.InitializeCamera( new Vector3( 0.0f, 8.0f, 20.0f ), new Vector3( 0.0f, 8.0f, 0.0f ), Vector3.UnitY );
#else
			CamManip.InitializeCamera( new Vector3( -235.0f, 54.0f, -210.0f ), new Vector3( -233.0f, 54.0f, -190.0f ), Vector3.UnitY );
#endif

			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			string		InitialText = Text;
			DateTime	LastFPSTime = DateTime.Now;
			int			FPSFramesCount = 0;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Lightning Animation ===============
				float	DeltaTimeLightning = (float) (CurrentFrameTime - m_LightningStrikeTime).TotalSeconds;

				float	IntensityEnveloppe = (float) Math.Exp( -10.0 * DeltaTimeLightning*DeltaTimeLightning );
				float	LightningAmplitude = (float) Math.Max( 0.0, Math.Cos( 10.0 * DeltaTimeLightning * Math.PI ) );
				m_RenderTechniqueClouds.LightningIntensity = 50000.0f * IntensityEnveloppe * LightningAmplitude;

				// =============== Render Scene ===============

				// Clear render target
				m_Device.ClearRenderTarget( m_RenderTargets[0], m_SkyIntensity * new Color4( Color.CornflowerBlue.ToArgb() ) );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Render terrain
				using ( m_MaterialGround.UseLock() )
				{
					m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.CULL_BACK );
					m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
					m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );
					m_Device.SetRenderTarget( m_RenderTargets[0], m_Device.DefaultDepthStencil );
					m_Device.SetViewport( 0, 0, m_RenderTargets[0].Width, m_RenderTargets[0].Height, 0.0f, 1.0f );

					m_MaterialGround.GetVariableByName( "GroundTexture" ).AsResource.SetResource( m_TerrainTexture );
					m_MaterialGround.ApplyPass(0);
					m_Terrain.Render();
				}

				// Render gnome
				using ( m_MaterialGnome.UseLock() )
				{
					m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );

					m_MaterialGnome.GetVariableByName( "GnomeTex" ).AsResource.SetResource( m_GnomeTexture );
					m_MaterialGnome.ApplyPass(0);
					m_Gnome.Render();
				}

				// Render lightning
				m_RenderTechniqueClouds.DisplayLightning();

				// Draw
				m_RenderTechniqueClouds.Time = fTotalTime;
				m_Renderer.Render();

				// Show !
				m_Device.Present();

				// Update FPS
				FPSFramesCount++;
				DateTime	Now = DateTime.Now;
				if ( (Now - LastFPSTime).TotalMilliseconds > 1000 )
				{
					float	FPS = (float) (FPSFramesCount / (Now - LastFPSTime).TotalSeconds);
					LastFPSTime = Now;
					FPSFramesCount = 0;
					Text = InitialText + " - " + FPS.ToString( "G4" ) + " FPS";
				}
			});
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			// Provide noise data
			INoise3D	I = _Interface as INoise3D;
			if ( I != null )
			{
				I.Noise0 = m_NoiseTextures[0];
				I.Noise1 = m_NoiseTextures[1];
				I.Noise2 = m_NoiseTextures[2];
				I.Noise3 = m_NoiseTextures[3];
				I.LargeNoise = m_LargeNoiseTexture2;
				return;
			}
		}

		#endregion

		#region IMaterialLoader Members

		public Material<VS> LoadMaterial<VS>( string _Name, ShaderModel _SM, System.IO.FileInfo _FileName ) where VS : struct
		{
			return ToDispose( new Material<VS>( m_Device, _Name, _SM, _FileName ) );
		}

		#endregion

		#region Tree View Management

		protected TreeNode	m_ShaderInterfaceProvidersNode = null;
		protected Dictionary<IShaderInterfaceProvider,TreeNode>	m_ShaderInterfaceProvider2TreeNode = new Dictionary<IShaderInterfaceProvider,TreeNode>();
		protected void	BuildHierarchyTree()
		{
			// Build the renderer nodes
			TreeNode	RendererNode = new TreeNode( "Renderer" );
						RendererNode.Tag = m_Renderer;
			treeViewObjects.Nodes.Add( RendererNode );

			foreach ( Pipeline P in m_Renderer.Renderer.Pipelines )
			{
				TreeNode	PipelineNode = new TreeNode( P.Name + " (" + P.Type + ")" );
							PipelineNode.Tag = P;
				RendererNode.Nodes.Add( PipelineNode );

				foreach ( RenderTechnique RT in P.RenderTechniques )
				{
					TreeNode	RenderTechniqueNode = new TreeNode( RT.Name );
								RenderTechniqueNode.Tag = RT;
					PipelineNode.Nodes.Add( RenderTechniqueNode );
				}
			}

			// Build the renderer nodes
			m_ShaderInterfaceProvidersNode = new TreeNode( "Shader Providers" );
			m_ShaderInterfaceProvidersNode.Tag = m_Device;
			treeViewObjects.Nodes.Add( m_ShaderInterfaceProvidersNode );

			foreach ( IShaderInterfaceProvider SIP in m_Device.RegisteredShaderInterfaceProviders )
			{
				TreeNode	ProviderNode = new TreeNode( SIP.ToString() );
							ProviderNode.Tag = SIP;
				m_ShaderInterfaceProvidersNode.Nodes.Add( ProviderNode );
				m_ShaderInterfaceProvider2TreeNode[SIP] = ProviderNode;
			}

			m_Device.ShaderInterfaceProviderAdded += new Nuaj.Device.ShaderInterfaceEventHandler( Device_ShaderInterfaceProviderAdded );
			m_Device.ShaderInterfaceProviderRemoved += new Nuaj.Device.ShaderInterfaceEventHandler( Device_ShaderInterfaceProviderRemoved );

			treeViewObjects.ExpandAll();
		}

		void Device_ShaderInterfaceProviderAdded( IShaderInterfaceProvider _Provider )
		{
			TreeNode	ProviderNode = new TreeNode( _Provider.ToString() );
						ProviderNode.Tag = _Provider;
			m_ShaderInterfaceProvidersNode.Nodes.Add( ProviderNode );
			m_ShaderInterfaceProvider2TreeNode[_Provider] = ProviderNode;
		}

		void Device_ShaderInterfaceProviderRemoved( IShaderInterfaceProvider _Provider )
		{
			m_ShaderInterfaceProvidersNode.Nodes.Remove( m_ShaderInterfaceProvider2TreeNode[_Provider] );
		}

		#endregion

		#region Case Tables Building

		protected class		Edge
		{
			public Edge		m_Previous = null;
			public Edge		m_Next = null;
			public int		m_VertexIndex = -1;	// The source vertex we followed the edge from
			public int		m_LinkIndex = -1;	// The index of the link in [0,2] that we followed from the original vertex
			public int		m_EdgeIndex = -1;	// The actual edge index
			public bool		m_bVisited = false;	// A flag to know if we already browsed that edge (i.e. indicating we looped)

			protected int	m_ID = -1;
			protected static int	ms_UniqueID = 0;

			public Edge( int _VertexIndex, int _LinkIndex, bool[] _UsedEdges )
			{
				m_ID = ms_UniqueID++;
				Update( _VertexIndex, _LinkIndex, _UsedEdges );
			}

			public override string ToString()
			{
				return "[" + m_ID + "] " + (m_Previous != null ? m_Previous.m_EdgeIndex : -1) + " <- " + m_EdgeIndex + " -> " + (m_Next != null ? m_Next.m_EdgeIndex : -1) + "  " + (m_bVisited ? "[V]" : "[ ]");
			}

			public void	LinkWith( Edge _Next )
			{
				_Next.m_Next = m_Next;
				_Next.m_Previous = this;
				if ( m_Next != null )
					m_Next.m_Previous = _Next;
				m_Next = _Next;
			}

			public void	Update( int _VertexIndex, int _LinkIndex, bool[] _UsedEdges )
			{
				m_VertexIndex = _VertexIndex;
				m_LinkIndex = _LinkIndex;
				m_EdgeIndex = ms_Vertex2Edges[_VertexIndex,_LinkIndex];
				_UsedEdges[m_EdgeIndex] = true;
			}
		}

		protected class	Triangle
		{
			public int	m_EdgeIndex0;
			public int	m_EdgeIndex1;
			public int	m_EdgeIndex2;
			public Triangle( int _EdgeIndex0, int _EdgeIndex1, int _EdgeIndex2, bool _bInvert )
			{
				m_EdgeIndex0 = _EdgeIndex0;
				// Here it would seem the inversion is itself inverted but eges loops are CCW oriented
				//	meaning following them and extracting the normal would point toward the INSIDE of the volume
				// We need triangles whose normals point toward the OUTSIDE, hence the "double inversion"
				m_EdgeIndex1 = _bInvert ? _EdgeIndex1 : _EdgeIndex2;
				m_EdgeIndex2 = _bInvert ? _EdgeIndex2 : _EdgeIndex1;
			}

			public override string ToString()
			{
				return m_EdgeIndex0 + " -> " + m_EdgeIndex1 + " -> " + m_EdgeIndex2;
			}
		}

		// Cube geometry goes like this :
		//
		//       ^ +Y
		//       |
		//       |
		//       5-----5-----6
		//      /|          /|
		//     9 |         / |
		//    /  4       10  6
		//   /   |       /   |
		//  1-----1-----2    |
		//  |    4-----7|----7---> +X
		//  |   /       |   /
		//  0  8        2  11
		//  | /         | /
		//  |/          |/
		//  0-----3-----3
		// /
		//+Z
		//
		// The start edge index is irrelevant but the
		// edges are listed in counter clockwise order
		protected static int[,]	ms_Vertex2Edges = new int[8,3]
			{
				{ 0, 8, 3 },	// Edges linking from vertex 0
				{ 0, 1, 9 },	// Edges linking from vertex 1
				{ 1, 2, 10 },	// Edges linking from vertex 2
				{ 3, 11, 2 },	// Edges linking from vertex 3
				{ 4, 7, 8 },	// Edges linking from vertex 4
				{ 9, 5, 4 },	// Edges linking from vertex 5
				{ 5, 10, 6 },	// Edges linking from vertex 6
				{ 6, 11, 7 },	// Edges linking from vertex 7
			};

		// The vertices we reach by following the edges from the previous array
		protected static int[,]	ms_Vertex2OtherVertex = new int[8,3]
			{
				{ 1, 4, 3 },	// Edges leads from vertex 0
				{ 0, 2, 5 },	// Edges leads from vertex 1
				{ 1, 3, 6 },	// Edges leads from vertex 2
				{ 0, 7, 2 },	// Edges leads from vertex 3
				{ 5, 7, 0 },	// Edges leads from vertex 4
				{ 1, 6, 4 },	// Edges leads from vertex 5
				{ 5, 2, 7 },	// Edges leads from vertex 6
				{ 6, 3, 4 },	// Edges leads from vertex 7
			};

		/// <summary>
		/// Builds the table of marching cube triangles
		/// 
		/// The algorithm works like this :
		///  1) Locate a vertex that is filled
		///  2) Create a start edge loop with the 3 edges that surround that vertex
		///  3) Follow each edge in the loop in turn until all are visited
		///    3.1) If the edge leads to a non-filled vertex, go to next edge in the loop, go to 3)
		///    3.2) Retrieve the 2 new edges to follow by checking the 3 edges of the vertex the current edge leads to
		///	     3.2.2) If the first edge has not been visited yet, update current edge to be that new edge
		///	            Otherwise, link over current edge, shrinking the loop
		///	     3.2.3) If the second edge has not been visited yet, create a new edge and append it to the current one
		///	            Otherwise, link over next edge, shrinking the loop
		///	   3.3) Goto 3)
		///	 4) Split the edge loop into triangles
		///	 5) Loop to 1) until all 8 vertices have been visited
		///	 
		/// You can imagine the algorithm as some kind of "virus edge loop" spreading across
		///  the cube from an "infected first vertex", following edges that link to another
		///  "infected vertex", growing or shrinking according to vertices' infection state.
		/// 
		/// Also, this algorithm fails when the edge loop should split into 2 distinct loops
		///  like for cases 0x7D or 0xD7 that should generate 2 distinct triangles.
		/// Only a single loop can be cared for at the same time for a single seed vertex.
		/// Fortunately, these "complex" cases can be easily made simpler by inverting their
		///  bits => case 0x7D becomes case 0x82 which generates the exact same 2 triangles,
		///  starting from 2 distinct seed vertices generating 2 distinct loops instead of a
		///  single big and strange one that should be split at some point.
		/// In a general manner, we always choose the case that has the least bits from an initial case
		///  and its inverse...
		/// </summary>
		protected void		BuildCaseTable()
		{
			List<Triangle>[]	Triangles = new List<Triangle>[256];
			bool[]				Vertices = new bool[8];

			int	MaxTrianglesCount = 0;
			for ( int CaseIndex=0; CaseIndex < 256; CaseIndex++ )
			{
				Triangles[CaseIndex] = new List<Triangle>();

				// Use the case that has the least full bits
				//	as they're easier to solve than complex
				//	cases and they're the same anyway...
				int	ComputedCase = CaseIndex;
				bool	bInvertTriangles = GetBestCase( CaseIndex, out ComputedCase );

				// Check which vertices are full
				Vertices[0] = (ComputedCase & 1) != 0;
				Vertices[1] = (ComputedCase & 2) != 0;
				Vertices[2] = (ComputedCase & 4) != 0;
				Vertices[3] = (ComputedCase & 8) != 0;
				Vertices[4] = (ComputedCase & 16) != 0;
				Vertices[5] = (ComputedCase & 32) != 0;
				Vertices[6] = (ComputedCase & 64) != 0;
				Vertices[7] = (ComputedCase & 128) != 0;

				// Build faces
				bool[]	UsedEdges = new bool[12];

				for ( int VertexIndex=0; VertexIndex < 8; VertexIndex++ )
				{
					if ( !Vertices[VertexIndex] )
						continue;	// This vertex is not full...
					
					// Check the 3 edges from that vertex haven't already been used
					if ( UsedEdges[ms_Vertex2Edges[VertexIndex,0]] ||
						 UsedEdges[ms_Vertex2Edges[VertexIndex,1]] ||
						 UsedEdges[ms_Vertex2Edges[VertexIndex,2]] )
						continue;

					// Initialize a simple loop issued from the 3 edges connecting from that vertex
					Edge	E0 = new Edge( VertexIndex, 0, UsedEdges );
					Edge	E1 = new Edge( VertexIndex, 1, UsedEdges );
					Edge	E2 = new Edge( VertexIndex, 2, UsedEdges );
					E0.m_Next = E1;
					E1.m_Next = E2;
					E2.m_Next = E0;
					E0.m_Previous = E2;
					E1.m_Previous = E0;
					E2.m_Previous = E1;

					// Expand the triangle by following edges
					Edge	Current = E0;
					while ( !Current.m_bVisited )
					{
						int	TargetVertexIndex = ms_Vertex2OtherVertex[Current.m_VertexIndex,Current.m_LinkIndex];
						if ( !Vertices[TargetVertexIndex] )
						{	// This vertex is not full, stop spreading and inspect next edge...
							Current.m_bVisited = true;
							Current = Current.m_Next;
							continue;
						}

						// Spread the edge loop : split the edge into 2 sub-edges
						int	LinkIndex0, LinkIndex1;
						if ( ms_Vertex2Edges[TargetVertexIndex,0] == Current.m_EdgeIndex )
						{
							LinkIndex0 = 1;
							LinkIndex1 = 2;
						}
						else if ( ms_Vertex2Edges[TargetVertexIndex,1] == Current.m_EdgeIndex )
						{
							LinkIndex0 = 2;
							LinkIndex1 = 0;
						}
						else if ( ms_Vertex2Edges[TargetVertexIndex,2] == Current.m_EdgeIndex )
						{
							LinkIndex0 = 0;
							LinkIndex1 = 1;
						}
						else
							throw new Exception( "Error in edge table !" );

						// Check if current edge should continue to grow
						int	NextEdgeIndex = ms_Vertex2Edges[TargetVertexIndex,LinkIndex0];
						if ( UsedEdges[NextEdgeIndex] )
						{	// Edge is already used, and it MUST be by our predecessor edge (otherwise, it's a bug !)
							if ( Current.m_Previous.m_EdgeIndex != NextEdgeIndex )
								throw new Exception( "WRONG!" );

							// Don't split that edge, simply remove current one, shrinking the edge loop
							Current = Current.m_Previous;
							Current.m_Next = Current.m_Next.m_Next;
							Current.m_Next.m_Previous = Current;
							Current.Update( TargetVertexIndex, LinkIndex1, UsedEdges );
							if ( Current.m_Next == Current.m_Previous )
								break;	// The loop collapsed on itself...
							continue;
						}
						else
						{	// Update current edge by following link #0
							Current.Update( TargetVertexIndex, LinkIndex0, UsedEdges );
						}

						// Check if we should create a new split edge
						NextEdgeIndex = ms_Vertex2Edges[TargetVertexIndex,LinkIndex1];
						if ( UsedEdges[NextEdgeIndex] )
						{	// Edge is already used, and it MUST be by our successor edge (otherwise, it's a bug !)
							if ( Current.m_Next.m_EdgeIndex != NextEdgeIndex )
								throw new Exception( "WRONG!" );

							// Link over next edge, actually shrinking the edge loop
							Current.m_Next = Current.m_Next.m_Next;
							Current.m_Next.m_Previous = Current;
						}
						else
						{	// Create a new successor edge by following link #1
							Edge	Split = new Edge( TargetVertexIndex, LinkIndex1, UsedEdges );
							Current.LinkWith( Split );
						}
					}

					// Generate triangles from the edge loop
					if ( Current.m_Next != Current.m_Previous )
					{	// The loop is more than 2 elements...
						// Stoopid generation using triangle fans
// 						E0 = Current;
// 						E1 = E0.m_Next;
// 						E2 = E1.m_Next;
// 						while ( E2 != Current )
// 						{
// 							Triangles[CaseIndex].Add( new Triangle( E0.m_EdgeIndex, E2.m_EdgeIndex, E1.m_EdgeIndex, bInvertTriangles ) );
// 
// 							E1 = E2;
// 							E2 = E2.m_Next;
// 						}

						// 1] Unfold the edge loop
						List<int>	EdgeLoop = new List<int>();
						E0 = Current;
						do
						{
							EdgeLoop.Add( E0.m_EdgeIndex );
							E0 = E0.m_Next;
						} while ( E0 != Current );

						// 2] Act on amount of edges
						switch ( EdgeLoop.Count )
						{
							case 3:	// Simple triangle
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[1], EdgeLoop[2], bInvertTriangles ) );
								break;
							case 4:	// Two triangles
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[1], EdgeLoop[2], bInvertTriangles ) );
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[2], EdgeLoop[3], bInvertTriangles ) );
								break;
							case 5:	// Three triangles
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[1], EdgeLoop[2], bInvertTriangles ) );
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[2], EdgeLoop[3], bInvertTriangles ) );
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[3], EdgeLoop[4], bInvertTriangles ) );
								break;
							case 6:	// 4 triangles : this case must not be a fan
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[1], EdgeLoop[2], bInvertTriangles ) );
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[2], EdgeLoop[3], bInvertTriangles ) );
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[0], EdgeLoop[3], EdgeLoop[5], bInvertTriangles ) );
								Triangles[CaseIndex].Add( new Triangle( EdgeLoop[3], EdgeLoop[4], EdgeLoop[5], bInvertTriangles ) );
								break;
						}
					}
				}	// Cube vertex iteration

				MaxTrianglesCount = Math.Max( MaxTrianglesCount, Triangles[CaseIndex].Count );
			}	// Case iteration

			System.IO.FileInfo		GeneratedShaderFile = new System.IO.FileInfo( @"FX\Clouds\Case2TrianglesTable.fx" );
			System.IO.StreamWriter	Writer = GeneratedShaderFile.CreateText();

			// Write header
			Writer.Write(	"// AUTO-GENERATED ! DON'T MODIFY !\r\n" +
							"// This table gives the cube edge indices for each triangle to generate and for each cube case\r\n" +
							"//   256*4 = 1024 entries\r\n" +
							"//\r\n" +
							"uint3	Case2Triangles[256][4] = {\r\n" );

			// Write triangles for all cases
			for ( int CaseIndex=0; CaseIndex < 256; CaseIndex++ )
			{
				List<Triangle>	CaseTriangles = Triangles[CaseIndex];

				Writer.Write( "	{ " );

				foreach ( Triangle T in CaseTriangles )
//					Writer.Write( "uint3( {0]{1:G2}, {2]{3:G2}, {4]{5:G2} ), ", T.m_EdgeIndex0 < 10 ? " " : "", T.m_EdgeIndex0, T.m_EdgeIndex1 < 10 ? " " : "", T.m_EdgeIndex1, T.m_EdgeIndex2 < 10 ? " " : "", T.m_EdgeIndex2 );
					Writer.Write( "uint3( {0:G2}, {1:G2}, {2:G2} ), ", T.m_EdgeIndex0, T.m_EdgeIndex1, T.m_EdgeIndex2 );
				for ( int RemainingTrianglesIndex=CaseTriangles.Count; RemainingTrianglesIndex < 4; RemainingTrianglesIndex++ )
					Writer.Write( "uint3( -1, -1, -1 ), " );

				Writer.Write( "},\r\n" );
			}

			// Write the table of triangles count for each case
			Writer.Write(	"};\r\n" +
							"\r\n" +
							"uint	Case2TrianglesCount[] = {\r\n" );

			for ( int i=0; i < 8; i++ )
			{
				for ( int j=0; j < 32; j++ )
					Writer.Write( Triangles[32*i+j].Count + ", " );
				Writer.Write( "\r\n" );
			}

			// Write footer
			Writer.Write( "};\r\n" );

			Writer.Close();
		}

		/// <summary>
		/// Gets the best case to work with
		/// Chooses between the provided case and its inverse based on bits count
		///  because least bits cases are the easiest to solve
		/// </summary>
		/// <param name="_CaseIndex"></param>
		/// <param name="_CaseIndexToCompute">The actual case index to use for computation</param>
		/// <returns></returns>
		protected bool		GetBestCase( int _CaseIndex, out int _CaseIndexToCompute )
		{
			int	MirroredCase = _CaseIndex ^ 0xFF;
			int	BitsCount0 = 0;
			int	BitsCount1 = 0;
			for ( int BitIndex=0; BitIndex < 8; BitIndex++ )
			{
				BitsCount0 += (_CaseIndex & (1 << BitIndex)) != 0 ? 1 : 0;
				BitsCount1 += (MirroredCase & (1 << BitIndex)) != 0 ? 1 : 0;
			}

			if ( BitsCount0 > BitsCount1 )
			{	// Invert
				_CaseIndexToCompute = MirroredCase;
				return true;
			}

			// Normal case
			_CaseIndexToCompute = _CaseIndex;
			return  false;
		}

		protected void		BuildCaseTable2()
		{
			System.IO.FileInfo		TableFile = new System.IO.FileInfo( @"..\Apps\DemoClouds\SourceTable.table" );
			System.IO.FileInfo		GeneratedShaderFile = new System.IO.FileInfo( @"FX\Clouds\Case2TrianglesTable.fx" );
			System.IO.StreamReader	Reader = TableFile.OpenText();
			System.IO.StreamWriter	Writer = GeneratedShaderFile.CreateText();

			// Write header
			Writer.Write(	"// AUTO-GENERATED ! DON'T MODIFY !\r\n" +
							"// This table gives the cube edge indices for each triangle to generate and for each cube case\r\n" +
							"//   256*5 = 1280 entries\r\n" +
							"//\r\n" +
							"uint3	Case2Triangles[256][5] = {\r\n" );

			// For check
			int[]	Case2TrianglesCount = {
				0, 1, 1, 2, 1, 2, 2, 3,  1, 2, 2, 3, 2, 3, 3, 2,  1, 2, 2, 3, 2, 3, 3, 4,  2, 3, 3, 4, 3, 4, 4, 3,  
				1, 2, 2, 3, 2, 3, 3, 4,  2, 3, 3, 4, 3, 4, 4, 3,  2, 3, 3, 2, 3, 4, 4, 3,  3, 4, 4, 3, 4, 5, 5, 2,  
				1, 2, 2, 3, 2, 3, 3, 4,  2, 3, 3, 4, 3, 4, 4, 3,  2, 3, 3, 4, 3, 4, 4, 5,  3, 4, 4, 5, 4, 5, 5, 4,  
				2, 3, 3, 4, 3, 4, 2, 3,  3, 4, 4, 5, 4, 5, 3, 2,  3, 4, 4, 3, 4, 5, 3, 2,  4, 5, 5, 4, 5, 2, 4, 1,  
				1, 2, 2, 3, 2, 3, 3, 4,  2, 3, 3, 4, 3, 4, 4, 3,  2, 3, 3, 4, 3, 4, 4, 5,  3, 2, 4, 3, 4, 3, 5, 2,  
				2, 3, 3, 4, 3, 4, 4, 5,  3, 4, 4, 5, 4, 5, 5, 4,  3, 4, 4, 3, 4, 5, 5, 4,  4, 3, 5, 2, 5, 4, 2, 1,  
				2, 3, 3, 4, 3, 4, 4, 5,  3, 4, 4, 5, 2, 3, 3, 2,  3, 4, 4, 5, 4, 5, 5, 2,  4, 3, 5, 4, 3, 2, 4, 1,  
				3, 4, 4, 5, 4, 5, 3, 4,  4, 5, 5, 2, 3, 4, 2, 1,  2, 3, 3, 2, 3, 4, 2, 1,  3, 2, 4, 1, 2, 1, 1, 0 };


			int	CaseIndex = 0;
			while ( !Reader.EndOfStream )
			{
				string		Line = Reader.ReadLine();
				string[]	TriangleEntries = Line.Split( ',' );	// Should be 5 entries per line

				Writer.Write( "	{ " );

				int	TrianglesCount = 0;
				for ( int EntryIndex=0; EntryIndex < TriangleEntries.Length; EntryIndex++ )
				{
					int			Index = -1;
					string[]	Indices = TriangleEntries[EntryIndex].Split( ' ' );
					List<int>	VectorResult = new List<int>();
					for ( int IndexIndex=0; IndexIndex < Indices.Length; IndexIndex++ )
						if ( int.TryParse( Indices[IndexIndex], out Index ) )
							VectorResult.Add( Index );

					if ( VectorResult.Count == 0 )
						continue;
					if ( VectorResult.Count != 4 )
						throw new Exception( "Invalid indices count in vector !" );

					Writer.Write( "uint3( {0:G2}, {1:G2}, {2:G2} ), ", VectorResult[0], VectorResult[2], VectorResult[1] );
					TrianglesCount += VectorResult[0] != -1 ? 1 : 0;
				}
				Writer.Write( "},\r\n" );

				if ( TrianglesCount != Case2TrianglesCount[CaseIndex] )
					throw new Exception( "FOR FUCK SAKE ! Couldn't find as many triangles as expected !" );

				CaseIndex++;
			}

			// Write the table of triangles count for each case
			Writer.Write(	"};\r\n" +
							"\r\n" +
							"uint	Case2TrianglesCount[] = {\r\n" );

			for ( int i=0; i < 8; i++ )
			{
				for ( int j=0; j < 32; j++ )
					Writer.Write( Case2TrianglesCount[32*i+j] + ", " );
				Writer.Write( "\r\n" );
			}

			// Write footer
			Writer.Write( "};\r\n" );

			Writer.Close();
			Reader.Close();
		}

		#endregion

		#region Noise Computation

		/// <summary>
		/// Creates a 3D noise texture
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateNoiseTexture( int _NoiseIndex, out Vector4[,,] _CPUNoiseTexture )
		{
			const int	NOISE_SIZE = 16;
//			const float	GLOBAL_SCALE = 2.0f;

			// Build the volume filled with noise
			float[,,]	Noise = new float[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];

			// Read noise from resources
			byte[][]	NoiseTextures = new byte[][]
			{
				Properties.Resources.packednoise_half_16cubed_mips_00,
				Properties.Resources.packednoise_half_16cubed_mips_01,
				Properties.Resources.packednoise_half_16cubed_mips_02,
				Properties.Resources.packednoise_half_16cubed_mips_03,
			};

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( NoiseTextures[_NoiseIndex] );
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
			Reader.Dispose();
			Stream.Dispose();

			// Build the 3D image and the 3D texture from it...
			Vector4[,,]	CPUTexture = new Vector4[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];
			_CPUNoiseTexture = CPUTexture;

			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_SIZE, NOISE_SIZE, NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{																			// (XYZ)
					_Color.X = Noise[_X,_Y,_Z];												// (000)
					_Color.Y = Noise[_X,(_Y+1) & (NOISE_SIZE-1),_Z];						// (010)
					_Color.Z = Noise[_X,_Y,(_Z+1) & (NOISE_SIZE-1)];						// (001)
					_Color.W = Noise[_X,(_Y+1) & (NOISE_SIZE-1),(_Z+1) & (NOISE_SIZE-1)];	// (011)

					CPUTexture[_X,_Y,_Z] = _Color;
				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}

		protected const int	LARGE_NOISE_SIZE = 128;

		/// <summary>
		/// Creates the large noise texture that encodes 4 octaves of the 16x16x16 standard noise
		/// </summary>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	CreateLargeNoiseTexture()
		{
			// Build the volume filled with noise
			Vector3		UVW = Vector3.Zero;
			Vector3		Derivatives, SumDerivatives = Vector3.Zero;
			float[,,]	Noise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			float[,,]	SumNoise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						UVW.X = (float) X / LARGE_NOISE_SIZE;
						UVW.Y = (float) Y / LARGE_NOISE_SIZE;
						UVW.Z = (float) Z / LARGE_NOISE_SIZE;

						float	Value = 0.0f;

						float	TempValue = ComputeNoise( UVW, m_CPUNoiseTextures[0], out Derivatives );
						SumDerivatives = Derivatives;
						Value += 1.0f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 2.0f * UVW, m_CPUNoiseTextures[1], out Derivatives );
						SumDerivatives += 0.5f * Derivatives;
						Value += 0.5f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 4.0f * UVW, m_CPUNoiseTextures[2], out Derivatives );
						SumDerivatives += 0.25f * Derivatives;
						Value += 0.25f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 8.0f * UVW, m_CPUNoiseTextures[3], out Derivatives );
						SumDerivatives += 0.125f * Derivatives;
						Value += 0.125f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						Noise[X,Y,Z] = Value;

						if ( Y > 0 )
							SumNoise[X,Y,Z] = SumNoise[X,Y-1,Z] + Value;
						else
							SumNoise[X,Y,Z] = 0.0f;
					}

// 			Vector4[,,]	SHNoise = new Vector4[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
// 			ComputeSHNoise( Noise, SHNoise );

			Vector3[,,]	PDNoise = new Vector3[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			ComputePrincipalDirectionsNoise( Noise, PDNoise );

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "LargeNoiseImage", LARGE_NOISE_SIZE, LARGE_NOISE_SIZE, LARGE_NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{
					_Color.X = Noise[_X,_Y,_Z];

//					_Color.Y = SumNoise[_X,_Y,_Z];

// 					_Color.Y = SHNoise[_X,_Y,_Z].X;
// 					_Color.Z = SHNoise[_X,_Y,_Z].Y;
// 					_Color.W = SHNoise[_X,_Y,_Z].Z;

					_Color.Y = PDNoise[_X,_Y,_Z].X;
					_Color.Z = PDNoise[_X,_Y,_Z].Y;
					_Color.W = PDNoise[_X,_Y,_Z].Z;

				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "LargeNoise", NoiseImage ) );
			}
		}

		/// <summary>
		/// Creates the large noise texture that encodes 4 octaves of the 16x16x16 standard noise
		/// </summary>
		/// <returns></returns>
		public Texture3D<PF_R16F>		CreateLargeNoiseTexture2()
		{
			// Build the volume filled with noise
			Vector3		UVW = Vector3.Zero;
			Vector3		Derivatives, SumDerivatives = Vector3.Zero;
			float[,,]	Noise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			float[,,]	SumNoise = new float[LARGE_NOISE_SIZE,LARGE_NOISE_SIZE,LARGE_NOISE_SIZE];
			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						UVW.X = (float) X / LARGE_NOISE_SIZE;
						UVW.Y = (float) Y / LARGE_NOISE_SIZE;
						UVW.Z = (float) Z / LARGE_NOISE_SIZE;

						float	Value = 0.0f;

						float	TempValue = ComputeNoise( UVW, m_CPUNoiseTextures[0], out Derivatives );
						SumDerivatives = Derivatives;
						Value += 1.0f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 2.0f * UVW, m_CPUNoiseTextures[1], out Derivatives );
						SumDerivatives += 0.5f * Derivatives;
						Value += 0.5f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 4.0f * UVW, m_CPUNoiseTextures[2], out Derivatives );
						SumDerivatives += 0.25f * Derivatives;
						Value += 0.25f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						TempValue = ComputeNoise( 8.0f * UVW, m_CPUNoiseTextures[3], out Derivatives );
						SumDerivatives += 0.125f * Derivatives;
						Value += 0.125f * TempValue / (1.0f + SumDerivatives.LengthSquared());

						Noise[X,Y,Z] = Value;

						if ( Y > 0 )
							SumNoise[X,Y,Z] = SumNoise[X,Y-1,Z] + Value;
						else
							SumNoise[X,Y,Z] = 0.0f;
					}

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_R16F>	NoiseImage = new Image3D<PF_R16F>( m_Device, "LargeNoiseImage", LARGE_NOISE_SIZE, LARGE_NOISE_SIZE, LARGE_NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{
					_Color.X = Noise[_X,_Y,_Z];
				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_R16F>( m_Device, "LargeNoise", NoiseImage ) );
			}
		}

		/// <summary>
		/// Computes the accumulated density in several directions and encodes it into SH using 2 bands (i.e. 4 coefficients)
		/// </summary>
		/// <param name="_SHNoise"></param>
		protected void	ComputeSHNoise( float[,,] _SourceNoise, Vector4[,,] _SHNoise )
		{
			System.IO.FileInfo		SHNoiseFile = new System.IO.FileInfo( "Data/PIPO_DemoClouds.SHNoise" );

#if LOAD_PRECOMP_NOISE

			if ( !SHNoiseFile.Exists )
				return;

			// Load the result
			System.IO.FileStream	Stream = SHNoiseFile.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						_SHNoise[X,Y,Z].X = Reader.ReadSingle();
						_SHNoise[X,Y,Z].Y = Reader.ReadSingle();
						_SHNoise[X,Y,Z].Z = Reader.ReadSingle();
						_SHNoise[X,Y,Z].W = Reader.ReadSingle();
					}

			Reader.Close();
			Reader.Dispose();
			Stream.Dispose();

#else
/*
			SphericalHarmonics.SHSamplesCollection	SHSamples = new SphericalHarmonics.SHSamplesCollection( 1 );
			SHSamples.Initialize( 2, SH_NOISE_ENCODING_SAMPLES_COUNT );

			Vector3		CurrentPosition, Step;
			float		DirY, HitDistance, MarchDistance;
			double[]	SH = new double[4];

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						SH[0] = 0.0; SH[1] = 0.0; SH[2] = 0.0; SH[3] = 0.0;
						foreach ( SphericalHarmonics.SHSamplesCollection.SHSample Sample in SHSamples )
						{
							CurrentPosition.X = X + 0.5f;
							CurrentPosition.Y = Y + 0.5f;
							CurrentPosition.Z = Z + 0.5f;
							Step.X = Sample.m_Direction.x;
							Step.Y = Sample.m_Direction.y;
							Step.Z = Sample.m_Direction.z;

							DirY = Math.Max( 1e-4f, Math.Abs( Step.Y ) );
							HitDistance = Sample.m_Direction.y > 0.0f ? Y / DirY : (LARGE_NOISE_SIZE - Y) / DirY;
							MarchDistance = Math.Min( LARGE_NOISE_SIZE, HitDistance );

							int		MarchStepsCount = (int) Math.Floor( MarchDistance );
							double	OpticalDepth = 0.0;
							for ( int StepIndex=0; StepIndex < MarchStepsCount; StepIndex++ )
							{
								OpticalDepth += SampleLargeNoise( ref CurrentPosition, _SourceNoise );
								CurrentPosition.X += Step.X;
								CurrentPosition.Y += Step.Y;
								CurrentPosition.Z += Step.Z;
							}
							OpticalDepth /= LARGE_NOISE_SIZE;

							// Encode into SH
							SH[0] += OpticalDepth * Sample.m_SHFactors[0];
							SH[1] += OpticalDepth * Sample.m_SHFactors[1];
							SH[2] += OpticalDepth * Sample.m_SHFactors[2];
							SH[3] += OpticalDepth * Sample.m_SHFactors[3];
						}
						SH[0] /= SHSamples.SamplesCount;
						SH[1] /= SHSamples.SamplesCount;
						SH[2] /= SHSamples.SamplesCount;
						SH[3] /= SHSamples.SamplesCount;

						_SHNoise[X,Y,Z] = new Vector4( (float) SH[1], (float) SH[2], (float) SH[3], (float) SH[0] );
					}
*/

			// Faster precomputation than SH
			// => Precompute 

			// Save the result
			System.IO.FileStream	Stream = SHNoiseFile.OpenWrite();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						Writer.Write( _SHNoise[X,Y,Z].X );
						Writer.Write( _SHNoise[X,Y,Z].Y );
						Writer.Write( _SHNoise[X,Y,Z].Z );
						Writer.Write( _SHNoise[X,Y,Z].W );
					}

			Writer.Close();
			Writer.Dispose();
			Stream.Dispose();

#endif
		}

		/// <summary>
		/// Computes the accumulated density in 3 principal directions
		/// </summary>
		/// <param name="_SourceNoise"></param>
		/// <param name="_SHNoise"></param>
		protected void	ComputePrincipalDirectionsNoise( float[,,] _SourceNoise, Vector3[,,] _PDNoise )
		{
			System.IO.FileInfo		PDNoiseFile = new System.IO.FileInfo( "Data/DemoClouds.PDNoise" );

#if LOAD_PRECOMP_NOISE

			if ( !PDNoiseFile.Exists )
				return;

			// Load the result
			System.IO.FileStream	Stream = PDNoiseFile.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						_PDNoise[X,Y,Z].X = Reader.ReadSingle();
						_PDNoise[X,Y,Z].Y = Reader.ReadSingle();
						_PDNoise[X,Y,Z].Z = Reader.ReadSingle();
					}

			Reader.Close();
			Reader.Dispose();
			Stream.Dispose();

#else

			int		BEAM_COUNT = 32;
			float	BEAM_OFF_ANGLE = 10.0f * (float) Math.PI / 180.0f;
			float	STEP_SIZE = 2.0f;

			Random		RNG = new Random( 1 );

			// These are the 3 main directions as defined in http://www2.ati.com/developer/gdc/D3DTutorial10_Half-Life2_Shading.pdf
			Vector3	PX = new Vector3( (float) Math.Sqrt( 2.0 / 3.0 ), (float) Math.Sqrt( 1.0 / 3.0 ), 0.0f );
			Vector3	PY = new Vector3( -(float) Math.Sqrt( 1.0 / 6.0 ), (float) Math.Sqrt( 1.0 / 3.0 ), -(float) Math.Sqrt( 1.0 / 2.0 ) );
			Vector3	PZ = new Vector3( -(float) Math.Sqrt( 1.0 / 6.0 ), (float) Math.Sqrt( 1.0 / 3.0 ), (float) Math.Sqrt( 1.0 / 2.0 ) );

			Vector3[]	Basis = new Vector3[]
			{
				PX, PY, PZ
			};

			// Draw random vectors to make a beam about the central Y direction
			Vector3[]	Beam = new Vector3[BEAM_COUNT];
			float		SumWeights = 0.0f;
			for ( int BeamIndex=0; BeamIndex < BEAM_COUNT; BeamIndex++ )
			{
				float	Theta = BEAM_OFF_ANGLE * (float) RNG.NextDouble();
				float	Phi = 2.0f * (float) Math.PI * (float) RNG.NextDouble();
				Beam[BeamIndex] = new Vector3( (float) (Math.Cos( Phi ) * Math.Sin( Theta )), (float) Math.Cos( Theta ), (float) (Math.Cos( Phi ) * Math.Sin( Theta )) );
				SumWeights += Beam[BeamIndex].Y;
			}
			float	InvSumWeights = 1.0f / SumWeights;

			int			StepsCount;
			Vector3		DX, DY, DZ, BeamDirection, CurrentPosition, Step;
			float		HitDistance, Weight;
			double[]	SumDensities = new double[3];
			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						// Iterate through the 3 main directions
						for ( int MainDirectionIndex=0; MainDirectionIndex < 3; MainDirectionIndex++ )
						{
							DX = Basis[(MainDirectionIndex+2)%3];
							DY = Basis[MainDirectionIndex+0];
							DZ = Basis[(MainDirectionIndex+1)%3];

							// Trace along all directions of the beam
							SumDensities[MainDirectionIndex] = 0.0;
							for ( int BeamIndex=0; BeamIndex < BEAM_COUNT; BeamIndex++ )
							{
								BeamDirection = Beam[BeamIndex].X * DX + Beam[BeamIndex].Y * DY + Beam[BeamIndex].Z * DZ;
								Weight = Beam[BeamIndex].Y;	// Weight is a measure of off-axis

								// Compute distance at which the beam escapes through the top of the cloud
								HitDistance = Y / BeamDirection.Y;
								StepsCount = (int) Math.Floor( HitDistance / STEP_SIZE );
								Step = STEP_SIZE * BeamDirection;

								CurrentPosition.X = X + 0.5f;
								CurrentPosition.Y = Y + 0.5f;
								CurrentPosition.Z = Z + 0.5f;

								double	SumDensity = 0.0;
								for ( int StepIndex=0; StepIndex < StepsCount; StepIndex++ )
								{
									SumDensity += STEP_SIZE * SampleLargeNoise( ref CurrentPosition, _SourceNoise );
									CurrentPosition.X += Step.X;
									CurrentPosition.Y -= Step.Y;
									CurrentPosition.Z += Step.Z;
								}
								SumDensities[MainDirectionIndex] += Weight * SumDensity / LARGE_NOISE_SIZE;
							}
							SumDensities[MainDirectionIndex] *= InvSumWeights;
						}

						_PDNoise[X,Y,Z] = new Vector3( (float) SumDensities[0], (float) SumDensities[1], (float) SumDensities[2] );
					}

			// Save the result
			System.IO.FileStream	Stream = PDNoiseFile.OpenWrite();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );

			for ( int Z=0; Z < LARGE_NOISE_SIZE; Z++ )
				for ( int Y=0; Y < LARGE_NOISE_SIZE; Y++ )
					for ( int X=0; X < LARGE_NOISE_SIZE; X++ )
					{
						Writer.Write( _PDNoise[X,Y,Z].X );
						Writer.Write( _PDNoise[X,Y,Z].Y );
						Writer.Write( _PDNoise[X,Y,Z].Z );
					}

			Writer.Close();
			Writer.Dispose();
			Stream.Dispose();

#endif
		}

		protected float	SampleLargeNoise( ref Vector3 _Position, float[,,] _Noise )
		{
			int		X = (int) Math.Floor( _Position.X );
			int		Y = (int) Math.Floor( _Position.Y );
			int		Z = (int) Math.Floor( _Position.Z );
			float	dX = _Position.X - X;
			float	dY = _Position.Y - Y;
			float	dZ = _Position.Z - Z;
			float	rdX = 1.0f - dX;
			float	rdY = 1.0f - dY;
			float	rdZ = 1.0f - dZ;
			X = X & (LARGE_NOISE_SIZE-1);
			Y = Y & (LARGE_NOISE_SIZE-1);
			Z = Z & (LARGE_NOISE_SIZE-1);
			int		NX = (X+1) & (LARGE_NOISE_SIZE-1);
			int		NY = (Y+1) & (LARGE_NOISE_SIZE-1);
			int		NZ = (Z+1) & (LARGE_NOISE_SIZE-1);

			float	V000 = _Noise[X,Y,Z];
			float	V001 = _Noise[NX,Y,Z];
			float	V011 = _Noise[NX,NY,Z];
			float	V010 = _Noise[X,NY,Z];
			float	V100 = _Noise[X,Y,NZ];
			float	V101 = _Noise[NX,Y,NZ];
			float	V111 = _Noise[NX,NY,NZ];
			float	V110 = _Noise[X,NY,NZ];

			float	V00 = rdX * V000 + dX * V001;
			float	V01 = rdX * V010 + dX * V011;
			float	V0 = rdY * V00 + dX * V01;
			float	V10 = rdX * V100 + dX * V101;
			float	V11 = rdX * V110 + dX * V111;
			float	V1 = rdY * V10 + dX * V11;

			return rdZ * V0 + dZ * V1;
		}

		protected const int	NOISE_TEXTURE_SIZE = 16;

		// Noise + Derivatives
		// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
		//
		protected float ComputeNoise2( Vector3 _UVW, Vector4[,,] _Noise, out Vector3 _Derivatives )
		{
			_UVW *= NOISE_TEXTURE_SIZE;

			int		X = (int) Math.Floor( _UVW.X );
			int		Y = (int) Math.Floor( _UVW.Y );
			int		Z = (int) Math.Floor( _UVW.Z );

			Vector3	uvw;
			uvw.X = _UVW.X - X;
			uvw.Y = _UVW.Y - Y;
			uvw.Z = _UVW.Z - Z;

			X &= (NOISE_TEXTURE_SIZE-1);
			Y &= (NOISE_TEXTURE_SIZE-1);
			Z &= (NOISE_TEXTURE_SIZE-1);
			int		NX = (X+1) & (NOISE_TEXTURE_SIZE-1);

			Vector4	N0 = _Noise[X,Y,Z];
			Vector4	N1 = _Noise[NX,Y,Z];

			// Quintic interpolation from Ken Perlin :
			//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
			//	du/dx = 30x^4 - 60x^3 + 30x^2
			//
			Vector3	dudvdw;
			dudvdw.X = 30.0f*uvw.X*uvw.X*(uvw.X*(uvw.X-2.0f)+1.0f);
			dudvdw.Y = 30.0f*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y-2.0f)+1.0f);
			dudvdw.Z = 30.0f*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z-2.0f)+1.0f);

			uvw.X = uvw.X*uvw.X*uvw.X*(uvw.X*(uvw.X*6.0f-15.0f)+10.0f);
			uvw.Y = uvw.Y*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y*6.0f-15.0f)+10.0f);
			uvw.Z = uvw.Z*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z*6.0f-15.0f)+10.0f);

			float	a = N0.X;
			float	b = N1.X;
			float	c = N0.Y;
			float	d = N1.Y;
			float	e = N0.Z;
			float	f = N1.Z;
			float	g = N0.W;
			float	h = N1.W;

			float	k0 =   a;
			float	k1 =   b - a;
			float	k2 =   c - a;
			float	k3 =   e - a;
			float	k4 =   a - b - c + d;
			float	k5 =   a - c - e + g;
			float	k6 =   a - b - e + f;
			float	k7 = - a + b + c - d + e - f - g + h;

			_Derivatives.X = dudvdw.X * (k1 + k4*uvw.Y + k6*uvw.Z + k7*uvw.Y*uvw.Z);
			_Derivatives.Y = dudvdw.Y * (k2 + k5*uvw.Z + k4*uvw.X + k7*uvw.Z*uvw.X);
			_Derivatives.Z = dudvdw.Z * (k3 + k6*uvw.X + k5*uvw.Y + k7*uvw.X*uvw.Y);

			return k0 + k1*uvw.X + k2*uvw.Y + k3*uvw.Z + k4*uvw.X*uvw.Y + k5*uvw.Y*uvw.Z + k6*uvw.Z*uvw.X + k7*uvw.X*uvw.Y*uvw.Z;
		}

		protected float ComputeNoise( Vector3 _UVW, Vector4[,,] _Noise, out Vector3 _Derivatives ) 
		{
			_UVW *= NOISE_TEXTURE_SIZE;

			int		X = (int) Math.Floor( _UVW.X );
			int		Y = (int) Math.Floor( _UVW.Y );
			int		Z = (int) Math.Floor( _UVW.Z );

			Vector3	uvw;
			uvw.X = _UVW.X - X;
			uvw.Y = _UVW.Y - Y;
			uvw.Z = _UVW.Z - Z;

			X &= (NOISE_TEXTURE_SIZE-1);
			Y &= (NOISE_TEXTURE_SIZE-1);
			Z &= (NOISE_TEXTURE_SIZE-1);
			int		NX = (X+1) & (NOISE_TEXTURE_SIZE-1);

			Vector4	N0 = _Noise[X,Y,Z];
			Vector4	N1 = _Noise[NX,Y,Z];

// 			// Quintic interpolation from Ken Perlin :
// 			//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
// 			//	du/dx = 30x^4 - 60x^3 + 30x^2
// 			//
// 			Vector3	dudvdw;
// 			dudvdw.X = 30.0f*uvw.X*uvw.X*(uvw.X*(uvw.X-2.0f)+1.0f);
// 			dudvdw.Y = 30.0f*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y-2.0f)+1.0f);
// 			dudvdw.Z = 30.0f*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z-2.0f)+1.0f);
// 
// 			uvw.X = uvw.X*uvw.X*uvw.X*(uvw.X*(uvw.X*6.0f-15.0f)+10.0f);
// 			uvw.Y = uvw.Y*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y*6.0f-15.0f)+10.0f);
// 			uvw.Z = uvw.Z*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z*6.0f-15.0f)+10.0f);

			float	a = N0.X;
			float	b = N1.X;
			float	c = N0.Y;
			float	d = N1.Y;
			float	e = N0.Z;
			float	f = N1.Z;
			float	g = N0.W;
			float	h = N1.W;

			float	k0 =   a;
			float	k1 =   b - a;
			float	k2 =   c - a;
			float	k3 =   e - a;
			float	k4 =   a - b - c + d;
			float	k5 =   a - c - e + g;
			float	k6 =   a - b - e + f;
			float	k7 = - a + b + c - d + e - f - g + h;

			_Derivatives.X = 0.0f;
			_Derivatives.Y = 0.0f;
			_Derivatives.Z = 0.0f;

			return k0 + k1*uvw.X + k2*uvw.Y + k3*uvw.Z + k4*uvw.X*uvw.Y + k5*uvw.Y*uvw.Z + k6*uvw.Z*uvw.X + k7*uvw.X*uvw.Y*uvw.Z;
		}

		protected void	BuildNoiseArray()
		{
			string	Table = "float4	NoiseTable[257] = { ";

			Random	RNG = new Random( 1 );
			string	FirstValue = "";
			for ( int i=0; i < 256; i++ )
			{
				string	Value = "float4( " + RNG.NextDouble().ToString() + ", "  + RNG.NextDouble().ToString() + ", " + RNG.NextDouble().ToString() + ", " + RNG.NextDouble().ToString() + " )";
				if ( i == 0 )
				{
					FirstValue = Value;
					Table += Value;
				}
				else
					Table += ", " + Value;
			}

			Table += ", " + FirstValue + " };";
		}

		#endregion

		#region Terrain

		protected BoundingBox	m_TerrainAABB;

		/// <summary>
		/// This creates a simple fbm terrain as a fixed size mesh
		/// </summary>
		protected void	CreateTerrainPrimitive()
		{
			VS_P3N3[]	Vertices = new VS_P3N3[(TERRAIN_TILES_COUNT+1)*(TERRAIN_TILES_COUNT+1)];
			int[]		Indices = new int[TERRAIN_TILES_COUNT*2*(TERRAIN_TILES_COUNT+2)];

			// Compute vertices
			m_TerrainAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			Vector3	P = Vector3.Zero;
			for ( int Z=0; Z <= TERRAIN_TILES_COUNT; Z++ )
			{
				P.Z = TERRAIN_SCALE_HORIZONTAL * ((float) Z / TERRAIN_TILES_COUNT - 0.5f);
				for ( int X=0; X <= TERRAIN_TILES_COUNT; X++ )
				{
					P.X = TERRAIN_SCALE_HORIZONTAL * ((float) X / TERRAIN_TILES_COUNT - 0.5f);
					P.Y = SampleTerrain( X, Z );
					Vertices[(TERRAIN_TILES_COUNT+1)*Z+X] = new VS_P3N3() { Position = P };

					m_TerrainAABB.Minimum = Vector3.Min( m_TerrainAABB.Minimum, P );
					m_TerrainAABB.Maximum = Vector3.Max( m_TerrainAABB.Maximum, P );
				}
			}

			m_RenderTechniqueClouds.TerrainAABB = m_TerrainAABB;

			// Redo a pass to compute normals
			Vector3	DX = new Vector3( 2.0f, 0.0f, 0.0f );
			Vector3	DZ = new Vector3( 0.0f, 0.0f, 2.0f );
			Vector3	N = Vector3.Zero;
			for ( int Z=0; Z <= TERRAIN_TILES_COUNT; Z++ )
			{
				int	PZ = (Z+TERRAIN_TILES_COUNT)%(TERRAIN_TILES_COUNT+1);
				int	NZ = (Z+1)%(TERRAIN_TILES_COUNT+1);

				for ( int X=0; X <= TERRAIN_TILES_COUNT; X++ )
				{
					int	PX = (X+TERRAIN_TILES_COUNT)%(TERRAIN_TILES_COUNT+1);
					int	NX = (X+1)%(TERRAIN_TILES_COUNT+1);

					float	HPX = Vertices[(TERRAIN_TILES_COUNT+1)*Z+PX].Position.Y / TERRAIN_SCALE_VERTICAL;
					float	HNX = Vertices[(TERRAIN_TILES_COUNT+1)*Z+NX].Position.Y / TERRAIN_SCALE_VERTICAL;
					float	HPZ = Vertices[(TERRAIN_TILES_COUNT+1)*PZ+X].Position.Y / TERRAIN_SCALE_VERTICAL;
					float	HNZ = Vertices[(TERRAIN_TILES_COUNT+1)*NZ+X].Position.Y / TERRAIN_SCALE_VERTICAL;

					DX.Y = HNX - HPX;
					DZ.Y = HNZ - HPZ;
					N = Vector3.Cross( DZ, DX );

					Vertices[(TERRAIN_TILES_COUNT+1)*Z+X].Normal = N;
				}
			}

			// Build indices
			int	IndexCount = 0;
			for ( int Z=0; Z < TERRAIN_TILES_COUNT; Z++ )
			{
				for ( int X=0; X < TERRAIN_TILES_COUNT; X++ )
				{
					Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+0)+X;
					Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+X;
				}

				// Finalize strip
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+0)+TERRAIN_TILES_COUNT;
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+TERRAIN_TILES_COUNT;

				// Add 2 degenerate points to link to next strip
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+TERRAIN_TILES_COUNT;
				Indices[IndexCount++] = (TERRAIN_TILES_COUNT+1)*(Z+1)+0;
			}

			m_Terrain = ToDispose( new Primitive<VS_P3N3,int>( m_Device, "Terrain", PrimitiveTopology.TriangleStrip, Vertices, Indices ) );
		}

		protected float	SampleTerrain( float _X, float _Z )
		{
			Vector3	Pos = new Vector3( TERRAIN_NOISE_SCALE * _X, 0.0f, TERRAIN_NOISE_SCALE * _Z );
			Vector3	TempNormal;
			float	Value = 0.0f;
			Value += 1.0f   * ComputeNoise2( 1.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );
			Value += 0.5f   * ComputeNoise2( 2.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );
			Value += 0.25f  * ComputeNoise2( 4.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );
			Value += 0.125f * ComputeNoise2( 8.0f * Pos, m_CPUNoiseTextures[0], out TempNormal );

			return TERRAIN_SCALE_VERTICAL * Value + TERRAIN_OFFSET_VERTICAL;
		}

		#endregion

		#region Gnome

		protected void	CreateGnomePrimitive()
		{
//			Vector3	GnomePosition = new Vector3( 1.0f, 72.0f, 52.0f );

			// Random gnome
			Random	RNG = new Random();
			Vector3	GnomePosition = new Vector3( 1.0f, 72.0f, 52.0f );
			float	TempX = (float) RNG.NextDouble() * TERRAIN_TILES_COUNT;
			float	TempZ = (float) RNG.NextDouble() * TERRAIN_TILES_COUNT;
			GnomePosition.X = TERRAIN_SCALE_HORIZONTAL * (TempX / TERRAIN_TILES_COUNT - 0.5f);
			GnomePosition.Z = TERRAIN_SCALE_HORIZONTAL * (TempZ / TERRAIN_TILES_COUNT - 0.5f);
			GnomePosition.Y = SampleTerrain( TempX, TempZ );

			Vector3	GnomeLookAt = new Vector3( 0.0f, 0.0f, 1.0f );
			float	GnomeSize = 5.0f;

			float	AspectRatio = 361.0f / 402.0f;
			Vector3	Normal = Vector3.UnitY;

			GnomeLookAt.Normalize();
			Vector3	X = Vector3.Cross( Vector3.UnitY, GnomeLookAt );
			X.Normalize();
			Vector3	Y = Vector3.UnitY;

			VS_P3N3T2[]	Vertices = new VS_P3N3T2[4];
			Vertices[0] = new VS_P3N3T2() { Position=GnomePosition, Normal = new Vector3( - AspectRatio * 0.5f * GnomeSize, + GnomeSize, 0.0f ), UV=new Vector2(0.0f,0.0f) };
			Vertices[1] = new VS_P3N3T2() { Position=GnomePosition, Normal = new Vector3( - AspectRatio * 0.5f * GnomeSize, + 0.0f, 0.0f ), UV=new Vector2(0.0f,1.0f) };
			Vertices[2] = new VS_P3N3T2() { Position=GnomePosition, Normal = new Vector3( + AspectRatio * 0.5f * GnomeSize, + GnomeSize, 0.0f ), UV=new Vector2(1.0f,0.0f) };
			Vertices[3] = new VS_P3N3T2() { Position=GnomePosition, Normal = new Vector3( + AspectRatio * 0.5f * GnomeSize, + 0.0f, 0.0f ), UV=new Vector2(1.0f,1.0f) };

			int[]	Indices = new int[4]
			{
				0, 1, 2, 3
			};

			m_Gnome = ToDispose( new Primitive<VS_P3N3T2,int>( m_Device, "Gnome Primitive", PrimitiveTopology.TriangleStrip, Vertices, Indices ) );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		private void Device_MaterialEffectRecompiled( object sender, EventArgs e )
		{
			if ( richTextBoxOutput.InvokeRequired )
			{
				richTextBoxOutput.BeginInvoke( new EventHandler( Device_MaterialEffectRecompiled ), sender, e );
				return;
			}

			IMaterial	M = sender as IMaterial;
			richTextBoxOutput.Log( DateTime.Now.ToString( "HH:mm:ss" ) + " > \"" + M.ToString() + "\" recompiled...\r\n" );
			if ( M.HasErrors )
				richTextBoxOutput.LogError( "ERRORS:\r\n" + M.CompilationErrors );
			else if ( M.CompilationErrors != "" )
				richTextBoxOutput.LogWarning( "WARNINGS:\r\n" + M.CompilationErrors );
			else
				richTextBoxOutput.LogSuccess( "0 error...\r\n" );
			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );
		}

		private void treeViewObjects_AfterSelect( object sender, TreeViewEventArgs e )
		{
			propertyGrid.SelectedObject = e.Node.Tag;
		}

		protected DateTime	m_LightningStrikeTime = DateTime.Today;
		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
#if VOXEL_CLOUDS
			if ( e.KeyCode == Keys.Add )
			{
				m_RenderTechniqueClouds.PlaneDistance += 0.025f;
				richTextBoxOutput.Log( m_RenderTechniqueClouds.PlaneDistance + "\n" );
			}
			else if ( e.KeyCode == Keys.Subtract )
			{
				m_RenderTechniqueClouds.PlaneDistance -= 0.025f;
				richTextBoxOutput.Log( m_RenderTechniqueClouds.PlaneDistance + "\n" );
			}
#endif

			// Lightning strike !
			if ( e.KeyCode == Keys.Space )
				m_LightningStrikeTime = DateTime.Now;
		}

		private void floatTrackbarControlSunAzimuth_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.SunPhi = _Sender.Value;
		}

		protected float	m_OriginalIsotropicFactor = -1.0f;
		private void floatTrackbarControlSunElevation_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( m_OriginalIsotropicFactor < 0.0 )
				m_OriginalIsotropicFactor = m_RenderTechniqueClouds.IsotropicFactor;

			m_RenderTechniqueClouds.SunTheta = _Sender.Value;

			// Also change Rayleigh & Mie densities as evenings are moister and foggier
			UpdateFogAerosols();

			// Increase isotropic scattering so the bottom of clouds is lighter in the evening
			float	fSunFactor = 1.0f - Math.Max( 0.0f, m_RenderTechniqueClouds.SunDirection.Y );
// 			m_RenderTechniqueClouds.IsotropicFactor = Lerp( m_OriginalIsotropicFactor, 2.0f, (float) Math.Pow( fSunFactor, 2.0f ) );
// 			m_RenderTechniqueClouds.DensitySumFactor = Lerp( 0.01f, 0.001f, (float) Math.Pow( fSunFactor, 2.0f ) );

			m_RenderTechniqueClouds.IsotropicFactor = Lerp( 1.0f, 1.0f, (float) Math.Pow( fSunFactor, 2.0f ) );
			m_RenderTechniqueClouds.DensitySumFactor = Lerp( 0.001f, 0.001f, (float) Math.Pow( fSunFactor, 2.0f ) );
		}

		private void floatTrackbarControlCoverage_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RenderTechniqueClouds.CloudCoverage = Lerp( -0.4f, 0.6f, _Sender.Value );
		}

		protected float	m_OriginalDensityCloud = -1.0f;
		protected float	m_OriginalDensitySumFactor = -1.0f;
		protected float	m_OriginalSigmaExtinction = -1.0f;
		protected float	m_OriginalScatteringRatio = -1.0f;
		private void floatTrackbarControlCloudType_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( m_OriginalDensityCloud < 0.0 )
			{
				m_OriginalDensityCloud = m_RenderTechniqueClouds.DensityCloud;
				m_OriginalDensitySumFactor = m_RenderTechniqueClouds.DensitySumFactor;
				m_OriginalSigmaExtinction = m_RenderTechniqueClouds.SigmaExtinction;
				m_OriginalScatteringRatio = m_RenderTechniqueClouds.ScatteringRatio;
			}

// 			m_RenderTechniqueClouds.SigmaExtinction = Lerp( 6.0f, 25.0f, _Sender.Value );
// //			m_RenderTechniqueClouds.ScatteringRatio = Lerp( 1.21333f * m_OriginalScatteringRatio, 0.2f * m_OriginalScatteringRatio, _Sender.Value );
// 			m_RenderTechniqueClouds.ScatteringRatio = Lerp( 0.1f, 0.02f, _Sender.Value );

			m_RenderTechniqueClouds.SigmaExtinction = Lerp( 10.0f, 25.0f, _Sender.Value );
			m_RenderTechniqueClouds.ScatteringRatio = Lerp( 0.8f, 0.1f, _Sender.Value );
		}

		private void floatTrackbarControlFogAmount_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			UpdateFogAerosols();
		}

		private void floatTrackbarControlAerosolsAmount_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			UpdateFogAerosols();
		}

		private void floatTrackbarControlCloudSize_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			UpdateSizeAltitude();
		}

		private void floatTrackbarControlCloudAltitude_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			UpdateSizeAltitude();
		}

		protected float	m_OriginalDensityRayleigh = -1.0f;
		protected float	m_OriginalDensityMie = -1.0f;
		protected void		UpdateFogAerosols()
		{
			if ( m_OriginalDensityRayleigh < 0.0 )
			{
				m_OriginalDensityRayleigh = m_RenderTechniqueClouds.DensityRayleigh;
				m_OriginalDensityMie = m_RenderTechniqueClouds.DensityMie;
			}

			float	fFogFactor = Lerp( 0.25f, 8.0f, floatTrackbarControlFogAmount.Value );
			float	fAerosolsFactor = Lerp( 0.25f, 8.0f, floatTrackbarControlAerosolsAmount.Value );

			float	fSunFactor = 1.0f - Math.Max( 0.0f, m_RenderTechniqueClouds.SunDirection.Y );
			float	fDensityFactor = 1.0f + 15.0f * fSunFactor;
			m_RenderTechniqueClouds.DensityRayleigh = m_OriginalDensityRayleigh * fAerosolsFactor * fDensityFactor;
			m_RenderTechniqueClouds.DensityMie = m_OriginalDensityMie * fFogFactor * fDensityFactor;
		}

		protected float	m_OriginalCloudBottom = -1.0f;
		protected float	m_OriginalCloudTop = -1.0f;
		protected float	m_OriginalNoiseSize = -1.0f;
		protected float	m_OriginalFarClipClouds = -1.0f;
		protected void		UpdateSizeAltitude()
		{
			if ( m_OriginalCloudBottom < 0.0 )
			{
				m_OriginalCloudBottom = m_RenderTechniqueClouds.CloudPlaneHeightBottom;
				m_OriginalCloudTop = m_RenderTechniqueClouds.CloudPlaneHeightTop;
				m_OriginalNoiseSize = m_RenderTechniqueClouds.NoiseSize;
				m_OriginalFarClipClouds = m_RenderTechniqueClouds.FarClipClouds;
			}

// 			float	Thickness = Lerp( 60.0f, 200.0f, floatTrackbarControlCloudSize.Value );
// 			m_RenderTechniqueClouds.CloudPlaneHeightBottom = floatTrackbarControlCloudAltitude.Value;
// 			m_RenderTechniqueClouds.CloudPlaneHeightTop = floatTrackbarControlCloudAltitude.Value + Thickness;
// 			m_RenderTechniqueClouds.FarClipClouds = m_OriginalFarClipClouds * Lerp( 1.0f, 3.0f, floatTrackbarControlCloudSize.Value );
// 			m_RenderTechniqueClouds.NoiseSize = Lerp( 0.1f, 0.15f, floatTrackbarControlCloudSize.Value );
// 			m_RenderTechniqueClouds.DensityCloud = Lerp( 0.5f, 0.2f, floatTrackbarControlCloudSize.Value );

			float	Thickness = Lerp( 5.0f, 150.0f, floatTrackbarControlCloudSize.Value );
			m_RenderTechniqueClouds.CloudPlaneHeightBottom = floatTrackbarControlCloudAltitude.Value;
			m_RenderTechniqueClouds.CloudPlaneHeightTop = floatTrackbarControlCloudAltitude.Value + Thickness;
			m_RenderTechniqueClouds.FarClipClouds = m_OriginalFarClipClouds * Lerp( 1.0f, 3.0f, floatTrackbarControlCloudSize.Value );
			m_RenderTechniqueClouds.NoiseSize = Math.Min( 0.1f, Math.Max( 0.01f, 0.01f * (float) Math.Pow( 2.0, 0.1f * Thickness ) ) );
			m_RenderTechniqueClouds.DensityCloud = Lerp( 0.5f, 0.2f, floatTrackbarControlCloudSize.Value );
		}

		protected float		Lerp( float _v0, float _v1, float _t )
		{
			return _v0 + (_v1 - _v0) * _t;
		}

		#endregion
	}
}
