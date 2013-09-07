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

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
		protected RenderTechniqueVoxelTerrain	m_RenderTechniqueTerrain = null;	// The advanced render technique for mesh clouds

		protected StringBuilder				m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public DemoForm()
		{
			InitializeComponent();


//			BuildCaseTable();	// This table has holes because of missing cases :(
			BuildCaseTable2();


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

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, panelOutput ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}


			//////////////////////////////////////////////////////////////////////////
			// Create the renderer & a default scene
			RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
			{
				CameraFOV = 45.0f * (float) Math.PI / 180.0f,
				CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
				CameraClipNear = 0.01f,
				CameraClipFar = 400.0f,
				bUseAlphaToCoverage = true
			};

			m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );

			// Setup the default lights
			m_Renderer.MainLight.Color = 10 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.FillLight.Color = 4 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.ToneMappingFactor = 0.9f;

			m_RenderTechniqueTerrain = ToDispose( new RenderTechniqueVoxelTerrain( m_Device, "Voxel Terrain Render Technique" ) );
			m_Renderer.Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).InsertTechnique( 0, m_RenderTechniqueTerrain );	// Insert our technique at the beginning


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
//			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/Masha" ) ) );
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Media/Terrain" ) ) );
			TextureProvider.ForceCreateMipMaps = true;


			// Display statistics & errors
			richTextBoxOutput.Log( "Texture Provider :\n" );
			richTextBoxOutput.Log( "> " + TextureProvider.LoadedTexturesCount + " textures loaded.\n" );
			int	MinSize = (int) Math.Sqrt( TextureProvider.MinTextureSurface );
			int	MaxSize = (int) Math.Sqrt( TextureProvider.MaxTextureSurface );
			int	AvgSize = (int) Math.Sqrt( TextureProvider.AverageTextureSurface );
			int	TotalSize = (int) Math.Sqrt( TextureProvider.TotalTextureSurface );
			richTextBoxOutput.Log( "> Surface Min=" + MinSize + "x" + MinSize + " Max=" + MaxSize + "x" + MaxSize + " Avg=" + AvgSize + "x" + AvgSize + "\n" );
			richTextBoxOutput.LogWarning( "> Surface Total=" + TotalSize + "x" + TotalSize + " (Memory=" + (TextureProvider.TotalTextureMemory>>10) + " Kb)\n" );

			if ( TextureProvider.HasErrors )
			{	// Display errors
				richTextBoxOutput.Log( "The texture provider has some errors !\r\n\r\n" );
				foreach ( string Error in TextureProvider.TextureErrors )
					richTextBoxOutput.LogError( "   ●  " + Error + "\r\n" );
			}
			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );

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
			CamManip.InitializeCamera( new Vector3( -85.0f, 75.0f, 50.0f ), Vector3.Zero, Vector3.UnitY );


			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_Renderer.Render();

				// Show !
				m_Device.Present();
			});
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

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

			System.IO.FileInfo		GeneratedShaderFile = new System.IO.FileInfo( @"FX\Terrain\Case2TrianglesTable.fx" );
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
			System.IO.FileInfo		TableFile = new System.IO.FileInfo( @"..\Apps\DemoTerrain\SourceTable.table" );
			System.IO.FileInfo		GeneratedShaderFile = new System.IO.FileInfo( @"FX\Terrain\Case2TrianglesTable.fx" );
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

		#region EVENT HANDLERS

		private void Device_MaterialEffectRecompiled( object sender, EventArgs e )
		{
			if ( richTextBoxOutput.InvokeRequired )
			{
				richTextBoxOutput.BeginInvoke( new EventHandler( Device_MaterialEffectRecompiled ), sender, e );
				return;
			}

			IMaterial	M = sender as IMaterial;
			richTextBoxOutput.Log( "\"" + M.ToString() + "\" recompiled...\r\n" );
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

		private void panelOutput_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			if ( e.KeyCode == Keys.Add )
			{
				m_RenderTechniqueTerrain.PlaneDistance += 0.025f;
				richTextBoxOutput.Log( m_RenderTechniqueTerrain.PlaneDistance + "\n" );
			}
			else if ( e.KeyCode == Keys.Subtract )
			{
				m_RenderTechniqueTerrain.PlaneDistance -= 0.025f;
				richTextBoxOutput.Log( m_RenderTechniqueTerrain.PlaneDistance + "\n" );
			}
		}

		#endregion
	}
}
