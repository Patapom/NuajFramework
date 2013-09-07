using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This class helps to create and use the SH Environment Map method (http://wiki.patapom.com/index.php/SHEnvironmentMap)
	/// The idea is to sample your environment at multiple relevant positions and encode it into "EnvironmentNodes" that will later be
	///  woven into a mesh and rendered into a small resolution map. The map is rendered at every frame and closely sticks to the camera.
	/// You can then benefit from ambient SH lighting by simply including the "SHSupport.fx" file in your shader and use the available functions there.
	/// 
	/// The manager provides 2 main functionalities :
	/// _ Management of environment nodes and generation of the SHEnv Map to use in your rendering
	/// _ Generation of the environment nodes's SH coefficients (i.e. rendering)
	/// 
	/// =================================================================================================================================
	/// To simply USE the manager with a set of existing environment nodes :
	///  1) Create the SHEnvMapManager
	///  2) Call LoadEnvironmentNodes() to load your network of already computed environment nodes
	///  3) Each frame, call the RenderSHEnvironmentMap() method by providing
	///		_ the camera you're rendering with
	///		_ the SHCoefficients of your indirect light (XYZ) and direct light (W) (cf. RenderSHEnvironmentMap() doc for how to configure that)
	/// 
	/// =================================================================================================================================
	/// To use the manager as a RENDERER of environment nodes you need to :
	///  1) Create the SHEnvMapManager
	///  2) Create as many environment nodes (empty or not) as necessary at positions relevant to your world/scene/whatever
	///  3) Call BeginEnvironmentRendering() providing a ISHCubeMapRenderer capable of rendering cube maps on demand
	///	 4) For each rendering pass (i.e. Pass #0 = Direct lighting, Pass #1 = Indirect First Bounce, etc.)
	///		4.1) For each environment node to render
	///			4.1.1) Render the cube map for that node
	///			4.1.2) Encode either its direct or indirect SH coefficients and store them in an array
	///		4.2) For each environment node to render
	///			4.2.1) Update its SH Coefficients with the ones just rendered (use the EnvironmentNode.UpdateCoefficients() method)
	///			4.2.2) Accumulate the SH Coefficients in a second array
	///	 5) For each environment node to render
	///		Update its SH Coefficients with the ones accumulated in the second array (use the EnvironmentNode.UpdateCoefficientsReflected() method)
	///			=> These are your final coefficients
	///	 6) Call EndEnvironmentRendering()
	/// 
	/// You can find an example of the process in the Implementations/SHEnvNodesRenderer.cs file.
	/// </summary>
	public class SHEnvMapManager : Component, IShaderInterfaceProvider
	{
		#region NESTED TYPES

		/// <summary>
		/// Environment SH Map Support
		/// </summary>
		public class	IEnvironmentSHMap : ShaderInterfaceBase
		{
			[Semantic( "SHENVMAP_OFFSET" )]
			public Vector2						SHEnvMapOffset	{ set { SetVector( "SHENVMAP_OFFSET", value ); } }
			[Semantic( "SHENVMAP_SCALE" )]
			public Vector2						SHEnvMapScale	{ set { SetVector( "SHENVMAP_SCALE", value ); } }
			[Semantic( "SHENVMAP_SIZE" )]
			public Vector2						SHEnvMapSize	{ set { SetVector( "SHENVMAP_SIZE", value ); } }
			[Semantic( "SHENVMAP" )]
			public RenderTarget3D<PF_RGBA16F>	SHEnvMap		{ set { SetResource( "SHENVMAP", value ); } }
		}

		/// <summary>
		/// This vertex will encode an environment node's 9 SH coefficients
		/// </summary>
		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		public struct VS_P3SH9
		{
			[Semantic( SemanticAttribute.POSITION )]
			public Vector3	Position;
			[Semantic( "SH", 0 )]
			public Vector4	SH0;
			[Semantic( "SH", 1 )]
			public Vector4	SH1;
			[Semantic( "SH", 2 )]
			public Vector4	SH2;
			[Semantic( "SH", 3 )]
			public Vector4	SH3;
			[Semantic( "SH", 4 )]
			public Vector4	SH4;
			[Semantic( "SH", 5 )]
			public Vector4	SH5;
			[Semantic( "SH", 6 )]
			public Vector4	SH6;
			[Semantic( "SH", 7 )]
			public Vector4	SH7;
			[Semantic( "SH", 8 )]
			public Vector4	SH8;
		}

		/// <summary>
		/// Represents a SHEnvMesh vertex (i.e. a position in space + 9 SH Coefficients)
		/// </summary>
		[System.Diagnostics.DebuggerDisplay( "Position={V.Position}" )]
		public class	EnvironmentNode
		{
			#region FIELDS

			protected SHEnvMapManager	m_Owner;
			public VS_P3SH9				V;

			#endregion

			#region METHODS

			public EnvironmentNode( SHEnvMapManager _Owner, Vector3 _Position, Vector4[] _SHCoefficients )
			{
				m_Owner = _Owner;
				V.Position = _Position;
				UpdateCoefficients( _SHCoefficients );
			}

			public EnvironmentNode( SHEnvMapManager _Owner, System.IO.BinaryReader _Reader )
			{
				m_Owner = _Owner;

				// Read back the vertex
				V.Position.X = _Reader.ReadSingle();
				V.Position.Y = _Reader.ReadSingle();
				V.Position.Z = _Reader.ReadSingle();

				V.SH0.X = _Reader.ReadSingle();
				V.SH0.Y = _Reader.ReadSingle();
				V.SH0.Z = _Reader.ReadSingle();
				V.SH0.W = _Reader.ReadSingle();

				V.SH1.X = _Reader.ReadSingle();
				V.SH1.Y = _Reader.ReadSingle();
				V.SH1.Z = _Reader.ReadSingle();
				V.SH1.W = _Reader.ReadSingle();

				V.SH2.X = _Reader.ReadSingle();
				V.SH2.Y = _Reader.ReadSingle();
				V.SH2.Z = _Reader.ReadSingle();
				V.SH2.W = _Reader.ReadSingle();

				V.SH3.X = _Reader.ReadSingle();
				V.SH3.Y = _Reader.ReadSingle();
				V.SH3.Z = _Reader.ReadSingle();
				V.SH3.W = _Reader.ReadSingle();

				V.SH4.X = _Reader.ReadSingle();
				V.SH4.Y = _Reader.ReadSingle();
				V.SH4.Z = _Reader.ReadSingle();
				V.SH4.W = _Reader.ReadSingle();

				V.SH5.X = _Reader.ReadSingle();
				V.SH5.Y = _Reader.ReadSingle();
				V.SH5.Z = _Reader.ReadSingle();
				V.SH5.W = _Reader.ReadSingle();

				V.SH6.X = _Reader.ReadSingle();
				V.SH6.Y = _Reader.ReadSingle();
				V.SH6.Z = _Reader.ReadSingle();
				V.SH6.W = _Reader.ReadSingle();

				V.SH7.X = _Reader.ReadSingle();
				V.SH7.Y = _Reader.ReadSingle();
				V.SH7.Z = _Reader.ReadSingle();
				V.SH7.W = _Reader.ReadSingle();

				V.SH8.X = _Reader.ReadSingle();
				V.SH8.Y = _Reader.ReadSingle();
				V.SH8.Z = _Reader.ReadSingle();
				V.SH8.W = _Reader.ReadSingle();
			}

			public void	Write( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( V.Position.X );
				_Writer.Write( V.Position.Y );
				_Writer.Write( V.Position.Z );

				_Writer.Write( V.SH0.X );
				_Writer.Write( V.SH0.Y );
				_Writer.Write( V.SH0.Z );
				_Writer.Write( V.SH0.W );

				_Writer.Write( V.SH1.X );
				_Writer.Write( V.SH1.Y );
				_Writer.Write( V.SH1.Z );
				_Writer.Write( V.SH1.W );

				_Writer.Write( V.SH2.X );
				_Writer.Write( V.SH2.Y );
				_Writer.Write( V.SH2.Z );
				_Writer.Write( V.SH2.W );

				_Writer.Write( V.SH3.X );
				_Writer.Write( V.SH3.Y );
				_Writer.Write( V.SH3.Z );
				_Writer.Write( V.SH3.W );

				_Writer.Write( V.SH4.X );
				_Writer.Write( V.SH4.Y );
				_Writer.Write( V.SH4.Z );
				_Writer.Write( V.SH4.W );

				_Writer.Write( V.SH5.X );
				_Writer.Write( V.SH5.Y );
				_Writer.Write( V.SH5.Z );
				_Writer.Write( V.SH5.W );

				_Writer.Write( V.SH6.X );
				_Writer.Write( V.SH6.Y );
				_Writer.Write( V.SH6.Z );
				_Writer.Write( V.SH6.W );

				_Writer.Write( V.SH7.X );
				_Writer.Write( V.SH7.Y );
				_Writer.Write( V.SH7.Z );
				_Writer.Write( V.SH7.W );

				_Writer.Write( V.SH8.X );
				_Writer.Write( V.SH8.Y );
				_Writer.Write( V.SH8.Z );
				_Writer.Write( V.SH8.W );
			}

			/// <summary>
			/// Updates the node's coefficients (will trigger a primitive update next time the env mesh is used)
			/// </summary>
			/// <param name="_SHCoefficients"></param>
			public void	UpdateCoefficients( Vector4[] _SHCoefficients )
			{
				if ( _SHCoefficients == null )
					return;

				V.SH0 = _SHCoefficients[0];
				V.SH1 = _SHCoefficients[1];
				V.SH2 = _SHCoefficients[2];
				V.SH3 = _SHCoefficients[3];
				V.SH4 = _SHCoefficients[4];
				V.SH5 = _SHCoefficients[5];
				V.SH6 = _SHCoefficients[6];
				V.SH7 = _SHCoefficients[7];
				V.SH8 = _SHCoefficients[8];

				// The environment mesh must be recomputed !
				m_Owner.m_bEnvMeshCoefficientsDirty = true;
			}

			/// <summary>
			/// Updates the node's coefficients (will trigger a primitive update next time the env mesh is used)
			/// </summary>
			/// <param name="_SHCoefficients"></param>
			/// <remarks>As the indirect-lit objects need to sample the environment around them, we store the coefficients in the opposite direction)</remarks>
			public void	UpdateCoefficientsReflected( Vector4[] _SHCoefficients )
			{
				if ( _SHCoefficients == null )
					return;

				V.SH0 = _SHCoefficients[0];
				V.SH1 = new Vector4( -_SHCoefficients[1].X, -_SHCoefficients[1].Y, -_SHCoefficients[1].Z, _SHCoefficients[1].W );
				V.SH2 = new Vector4( -_SHCoefficients[2].X, -_SHCoefficients[2].Y, -_SHCoefficients[2].Z, _SHCoefficients[2].W );
				V.SH3 = new Vector4( -_SHCoefficients[3].X, -_SHCoefficients[3].Y, -_SHCoefficients[3].Z, _SHCoefficients[3].W );
				V.SH4 = _SHCoefficients[4];
				V.SH5 = _SHCoefficients[5];
				V.SH6 = _SHCoefficients[6];
				V.SH7 = _SHCoefficients[7];
				V.SH8 = _SHCoefficients[8];

				// The environment mesh must be recomputed !
				m_Owner.m_bEnvMeshCoefficientsDirty = true;
			}

			/// <summary>
			/// Creates a cosine lobe occlusion SH vector
			/// </summary>
			/// <returns></returns>
			public void	MakeCosineLobe( Vector3 _Direction )
			{
				double[]	ZHCoeffs = new double[3]
				{
					0.88622692545275801364908374167057,	// sqrt(PI) / 2
					1.0233267079464884884795516248893,	// sqrt(PI / 3)
					0.49541591220075137666812859564002	// sqrt(5PI) / 8
				};

				double	cl0 = 3.5449077018110320545963349666823 * ZHCoeffs[0];
				double	cl1 = 2.0466534158929769769591032497785 * ZHCoeffs[1];
				double	cl2 = 1.5853309190424044053380115060481 * ZHCoeffs[2];

				double	f0 = 0.5 / Math.Sqrt(Math.PI);
				double	f1 = Math.Sqrt(3.0) * f0;
				double	f2 = Math.Sqrt(15.0) * f0;
				f0 *= cl0;
				f1 *= cl1;
				f2 *= cl2;

				Vector4[]	SHCoeffs = new Vector4[9];
				SHCoeffs[0].W = (float) f0;
				SHCoeffs[1].W = (float) (-f1 * _Direction.X);
				SHCoeffs[2].W = (float) (f1 * _Direction.Y);
				SHCoeffs[3].W = (float) (-f1 * _Direction.Z);
				SHCoeffs[4].W = (float) (f2 * _Direction.X * _Direction.Z);
				SHCoeffs[5].W = (float) (-f2 * _Direction.X * _Direction.Y);
				SHCoeffs[6].W = (float) (f2 / (2.0 * Math.Sqrt(3.0)) * (3.0 * _Direction.Y*_Direction.Y - 1.0));
				SHCoeffs[7].W = (float) (-f2 * _Direction.Z * _Direction.Y);
				SHCoeffs[8].W = (float) (f2 * 0.5 * (_Direction.Z*_Direction.Z - _Direction.X*_Direction.X));

				UpdateCoefficients( SHCoeffs );
			}

			/// <summary>
			/// Makes the node an ambient node (i.e. no change in lighting)
			/// </summary>
			public void	MakeAmbient()
			{
				Vector4[]	SHCoeffs = new Vector4[9];
				SHCoeffs[0] = 3.5449077018110320545963349666822f * Vector4.One;
				for ( int SHCoeffIndex=1; SHCoeffIndex < 9; SHCoeffIndex++ )
					SHCoeffs[SHCoeffIndex] = Vector4.Zero;

				UpdateCoefficients( SHCoeffs );
			}

			#endregion
		}

		/// <summary>
		/// CCW Triangles used in the Delaunay triangulation
		/// </summary>
		protected class	DelaunayTriangle
		{
			#region NESTED TYPES

			public class DelaunayEdge
			{
				public int					Index = 0;					// Edge index from Vertex[Index] to Vertex[Index+1]
				public DelaunayTriangle		Owner = null;
				protected DelaunayTriangle	m_Adjacent = null;
				protected int				m_FlipCounter = -1;			// The last flip counter

				public DelaunayTriangle	Adjacent
				{
					get { return m_Adjacent; }
					set
					{
						m_Adjacent = value;
						if ( m_Adjacent == null )
							return;

						// Also fix adjacent triangle's adjacency
						m_Adjacent.Edges[AdjacentEdgeIndex].m_Adjacent = Owner;
					}
				}

				public int				AdjacentEdgeIndex
				{
					get
					{
						if ( m_Adjacent == null )
							return -1;

						int	V0 = Owner.Vertices[Index];
						int	V1 = Owner.Vertices[Index+1];
						if ( m_Adjacent.Vertices[0] == V1 && m_Adjacent.Vertices[1] == V0 )
							return 0;
						else if ( m_Adjacent.Vertices[1] == V1 && m_Adjacent.Vertices[2] == V0 )
							return 1;
						else if ( m_Adjacent.Vertices[2] == V1 && m_Adjacent.Vertices[0] == V0 )
							return 2;

						throw new Exception( "Failed to retrieve our triangle in adjacent triangle's adjacencies !" );
					}
				}

				public float			Length = 0.0f;
				public Vector2			Direction = Vector2.Zero;
				public Vector2			Normal = Vector2.Zero;		// Pointing OUTWARD of the triangle

				public DelaunayEdge( DelaunayTriangle _Owner, int _Index, DelaunayTriangle _Adjacent )
				{
					Index = _Index;
					Owner = _Owner;
					Adjacent = _Adjacent;
				}

				/// <summary>
				/// Update edge infos
				/// </summary>
				/// <param name="_Vertices"></param>
				public void		UpdateInfos()
				{
					Direction = Owner[Index+1] - Owner[Index];
					Length = Direction.Length();
					Direction /= Length;
					Normal.X = Direction.Y;
					Normal.Y = -Direction.X;
				}

				public override string ToString()
				{
					return "O=" + Owner.m_ID + " V0=" + Owner.Vertices[Index] + " V1=" + Owner.Vertices[Index+1] + " Adj" + (Adjacent != null ? Adjacent.ToString() : "><") + " L=" + Length + " D=(" + Direction + ") N=(" + Normal + ")";
				}

				/// <summary>
				/// Flips the edge
				/// </summary>
				public void	Flip( int _FlipCounter )
				{
					if ( m_FlipCounter == _FlipCounter )
						return;	// Already flipped !
					m_FlipCounter = _FlipCounter;	// So we can't flip this edge again

					DelaunayTriangle	AdjacentTriangle = Adjacent;
					if ( AdjacentTriangle == null )
						return;	// Nothing to flip...

					// Check if the opposite vertex fits the Delaunay condition (i.e. lies outside this triangle's circumbscribed circle)
					int	AdjacentIndex = AdjacentEdgeIndex;
					AdjacentTriangle.Edges[AdjacentIndex].m_FlipCounter = _FlipCounter;	// So we can't flip the equivalent adjacent edge either...

					int	OppositeVertexIndex = AdjacentTriangle.Vertices[(AdjacentIndex+2)%3];
					if ( !Owner.IsInsideCircle( OppositeVertexIndex ) )
						return;	// The triangles satisfy Delaunay condition...

					// Flip the edge
					int[]	QuadVertices = new int[4];
					QuadVertices[0] = Owner.Vertices[(Index+1)%3];
					QuadVertices[1] = Owner.Vertices[(Index+2)%3];
					QuadVertices[2] = Owner.Vertices[Index];
					QuadVertices[3] = OppositeVertexIndex;

					DelaunayTriangle[]	QuadAdjacentTriangles = new DelaunayTriangle[4];
					QuadAdjacentTriangles[0] = Owner.Edges[(Index+1)%3].Adjacent;
					QuadAdjacentTriangles[1] = Owner.Edges[(Index+2)%3].Adjacent;
					QuadAdjacentTriangles[2] = AdjacentTriangle.Edges[(AdjacentIndex+1)%3].Adjacent;
					QuadAdjacentTriangles[3] = AdjacentTriangle.Edges[(AdjacentIndex+2)%3].Adjacent;

					// Re-order our triangle so this edge is also the new flipped edge
					Owner.Vertices[Index] = QuadVertices[3];
					Owner.Vertices[(Index+1)%3] = QuadVertices[1];
					Owner.Vertices[(Index+2)%3] = QuadVertices[2];
					Owner.UpdateInfos();

					// Re-order adjacent triangle so its edge is also the new flipped edge
					AdjacentTriangle.Vertices[AdjacentIndex] = QuadVertices[1];
					AdjacentTriangle.Vertices[(AdjacentIndex+1)%3] = QuadVertices[3];
					AdjacentTriangle.Vertices[(AdjacentIndex+2)%3] = QuadVertices[0];
					AdjacentTriangle.UpdateInfos();

					// Update adjacencies
					Owner.Edges[(Index+1)%3].Adjacent = QuadAdjacentTriangles[1];
					Owner.Edges[(Index+2)%3].Adjacent = QuadAdjacentTriangles[2];
					AdjacentTriangle.Edges[(AdjacentIndex+1)%3].Adjacent = QuadAdjacentTriangles[3];
					AdjacentTriangle.Edges[(AdjacentIndex+2)%3].Adjacent = QuadAdjacentTriangles[0];

// CHECK => That flip should not flip the edge again since we now comply with Delaunay condition !
//FlipEdge( _FlipCounter-1, Owner, (Index+1)%3 );

					// Recursively split all 4 other edges
					DelaunayEdge	Temp = Owner.Edges[(Index+1)%3];
					Temp.Flip( _FlipCounter );
					Temp = Owner.Edges[(Index+2)%3];
					Temp.Flip( _FlipCounter );

					Temp = AdjacentTriangle.Edges[(AdjacentIndex+1)%3];
					Temp.Flip( _FlipCounter );
					Temp = AdjacentTriangle.Edges[(AdjacentIndex+2)%3];
					Temp.Flip( _FlipCounter );
				}
			}

			#endregion

			#region FIELDS

			protected int			m_ID = 0;
			protected Vector2[]		m_SourceVertices = null;

			public int[]			Vertices = new int[4];
			public DelaunayEdge[]	Edges = new DelaunayEdge[3];

			// Circumscribed circle
			public Vector2			CircleCenter;
			public float			CircleRadius;

			public static int		ms_Index = 0;

			#endregion

			#region PROPERTIES

			public Vector2		V0					{ get { return m_SourceVertices[Vertices[0]]; } }
			public Vector2		V1					{ get { return m_SourceVertices[Vertices[1]]; } }
			public Vector2		V2					{ get { return m_SourceVertices[Vertices[2]]; } }
			public Vector2		this[int _Index]	{ get { return m_SourceVertices[Vertices[_Index]]; } }

			#endregion

			#region METHODS

			public DelaunayTriangle( Vector2[] _Vertices, int _V0, int _V1, int _V2, DelaunayTriangle _T0, DelaunayTriangle _T1, DelaunayTriangle _T2 )
			{
				m_ID = ms_Index++;
				m_SourceVertices = _Vertices;

				Vertices[0] = _V0;
				Vertices[1] = _V1;
				Vertices[2] = _V2;

				// Build edges
				Edges[0] = new DelaunayEdge( this, 0, _T0 );
				Edges[1] = new DelaunayEdge( this, 1, _T1 );
				Edges[2] = new DelaunayEdge( this, 2, _T2 );

				// Build infos
				UpdateInfos();
			}

			public override string ToString()
			{
				return "ID=" + m_ID;
			}

			/// <summary>
			/// Builds the circumscribed circle of that triangle & recomputes edges length
			/// (from http://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations)
			/// </summary>
			public void		UpdateInfos()
			{
				Vertices[3] = Vertices[0];	// Redundant so edges don't need to bother with %3

				// Update edge infos
				Edges[0].UpdateInfos();
				Edges[1].UpdateInfos();
				Edges[2].UpdateInfos();

				// Rebuild circumbscribed circle
				Vector2	a = V0 - V2;
				Vector2	b = V1 - V2;
				Vector3	axb = Vector3.Cross( new Vector3( a, 0.0f ), new Vector3( b, 0.0f ) );
				float	a_Length = a.Length();
				float	b_Length = b.Length();
				float	axb_Length = axb.Length();

				CircleRadius = 0.5f * a_Length * b_Length * (V0 - V1).Length() / axb_Length;

				Vector2	a2b_b2a = a_Length*a_Length * b - b_Length*b_Length * a;
				Vector3	Num = Vector3.Cross( new Vector3( a2b_b2a, 0.0f ), axb );
				CircleCenter = V2 + 0.5f * new Vector2( Num.X, Num.Y ) / (axb_Length*axb_Length);

// Check
// float	D0 = (this[0] - CircleCenter).Length();
// float	D1 = (this[1] - CircleCenter).Length();
// float	D2 = (this[2] - CircleCenter).Length();
			}

			public bool	IsInsideCircle( int _VertexIndex )
			{
				// Handle the degenerate circle case when a vertex lies on an edge...
				if ( float.IsInfinity( CircleRadius) )
					return true;

				Vector2	Center2Vertex = m_SourceVertices[_VertexIndex] - CircleCenter;
				return Center2Vertex.LengthSquared() < CircleRadius*CircleRadius;
			}

			#endregion
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// The SHEnvMap rendering & the network of environment nodes

		// The network of environmental nodes
		protected bool								m_bEnvMeshDirty = true;
		protected bool								m_bEnvMeshCoefficientsDirty = true;
		protected List<EnvironmentNode>				m_EnvNodes = new List<EnvironmentNode>();
		protected Primitive<VS_P3SH9,int>			m_EnvMesh = null;
		protected Material<VS_P3SH9>				m_SHEnvMapMaterial = null;
		protected bool								m_bEnableAmbientSH = true;
		protected bool								m_bEnableIndirectSH = true;

		// The last rendered environment map and its offset & scale
		protected Vector2							m_SHEnvOffset;
		protected Vector2							m_SHEnvScale;
		protected Vector2							m_SHEnvMapSize;
		protected RenderTarget3D<PF_RGBA16F>		m_SHEnvMap = null;


		//////////////////////////////////////////////////////////////////////////
		// The cube map renderer to create environment nodes
		protected ISHCubeMapsRenderer				m_CubeMapsRenderer = null;
		protected int								m_CubeMapSize = 0;

		// Cube map post-processing
		protected Material<VS_Pt4>					m_MaterialPostProcessCubeMapFace = null;
		protected Helpers.ScreenQuad				m_Quad = null;
		protected float								m_IndirectLightingBoostFactor = 1.0f;
		protected Camera							m_CubeMapCamera = null;
		protected Matrix							m_EnvironmentCamera2World = Matrix.Identity;
		protected float								m_DepthBufferInfinity = 0.0f;

		// 2 textures sent to the cube map face post-processor
		protected RenderTarget<PF_RGBA32F>			m_CubeMapFaceAlbedo = null;
		protected RenderTarget<PF_RGBA32F>			m_CubeMapFaceNormalDepth = null;
		protected Texture2D							m_StagingAlbedo = null;
		protected Texture2D							m_StagingNormalDepth = null;

		// The resulting indirect SH lighting textures storing the 3*9 SH coefficients for indirect lighting
		protected RenderTarget<PF_RGBA32F>[]		m_IndirectLighting = new RenderTarget<PF_RGBA32F>[7];
		protected Texture2D[]						m_StagingIndirectLighting = new Texture2D[7];

		#endregion

		#region PROPERTIES

		public bool							EnableAmbientSH			{ get { return m_bEnableAmbientSH; } set { m_bEnableAmbientSH = value; } }
		public bool							EnableIndirectSH		{ get { return m_bEnableIndirectSH; } set { m_bEnableIndirectSH = value; } }

		public EnvironmentNode[]			EnvironementNodes		{ get { return m_EnvNodes.ToArray(); } }

		#endregion

		#region METHODS

		/// <summary>
		/// Initializes the manager
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_SHEnvMapSize">Size of the environment map to render and use (typical values are 256 or 512)</param>
		/// <param name="_MatLoader">Abstract material loader to create our materials</param>
		public	SHEnvMapManager( Device _Device, string _Name, int _SHEnvMapSize, IMaterialLoader _MatLoader ) : base( _Device, _Name )
		{
			//////////////////////////////////////////////////////////////////////////
			// Register shader interfaces
			m_Device.DeclareShaderInterface( typeof(IEnvironmentSHMap) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IEnvironmentSHMap), this );

			//////////////////////////////////////////////////////////////////////////
			// Create the SH 3D envmap that will contain the 9 SH coefficients in its 7 successive layers
			// (Indeed, we need to store 3*9=27 coefficients that we can pack into 7 RGBA slots that will amount to a total of 4*7=28 coefficients)
			m_SHEnvMap = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "EnvSHMap", _SHEnvMapSize, _SHEnvMapSize, 7, 1 ) );
			m_SHEnvMapSize = new Vector2( _SHEnvMapSize, 1.0f / _SHEnvMapSize );

			// And its rendering material
			m_SHEnvMapMaterial = _MatLoader.LoadMaterial<VS_P3SH9>( "Environment SH Map Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/SHEnvMap/EnvironmentSHMap.fx" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the cube-map post-process material for environment nodes rendering
			m_MaterialPostProcessCubeMapFace = _MatLoader.LoadMaterial<VS_Pt4>( "CubeMap PostProcess Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/SHEnvMap/PostProcessEnvMap.fx" ) );
		}

		public override void Dispose()
		{
			// Destroy any environment mesh
			ClearEnvironmentMesh();

			base.Dispose();
		}

		/// <summary>
		/// Renders the environment mesh into the environment map fit for the given camera
		/// </summary>
		/// <param name="_Camera">The camera viewing the environment</param>
		/// <param name="_EnvLightSH">The complex SH light for the environment
		/// NOTE: RGB encodes colored environment light (i.e. without direct light) and A encodes monochromatic (i.e. white) direct light (i.e. without environment)
		/// You can check an example of the generation of such coefficients in the DemoDeferredRendering/Techniques/RenderTechniqueEmissiveSky.cs file seeing the BuildSkySH() method.
		/// </param>
		public void				RenderSHEnvironmentMap( Camera _Camera, Vector4[] _EnvLightSH )
		{
			// Make sure the environment mesh is built
			BuildEnvironmentMesh();

			// 1] Build the clip offset & scale to map the camera SH environment
			// 1.1] First, we project the camera frustum in the XZ 2D plane
			Matrix	Camera2World = _Camera.Camera2World;

			Vector2	WorldMin = new Vector2( +float.MaxValue, +float.MaxValue );
			Vector2	WorldMax = new Vector2( -float.MaxValue, -float.MaxValue );
			foreach ( Vector3 V in _Camera.Frustum.Vertices )
			{
				Vector3	VWorld = Vector3.TransformCoordinate( V, Camera2World );
				WorldMin.X = Math.Min( WorldMin.X, VWorld.X );
				WorldMax.X = Math.Max( WorldMax.X, VWorld.X );
				WorldMin.Y = Math.Min( WorldMin.Y, VWorld.Z );
				WorldMax.Y = Math.Max( WorldMax.Y, VWorld.Z );
			}

			// 1.2] Then we build the clip offset & scale to map the camera SH environment
			m_SHEnvOffset = WorldMin;
			m_SHEnvScale.X = 1.0f / (WorldMax.X - WorldMin.X);
			m_SHEnvScale.Y = 1.0f / (WorldMax.Y - WorldMin.Y);

			// 2] Render the SH Environment mesh
			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_SHEnvMapMaterial.UseLock() )
			{
				m_Device.SetRenderTarget( m_SHEnvMap );
				m_Device.SetViewport( 0, 0, m_SHEnvMap.Width, m_SHEnvMap.Height, 0.0f, 1.0f );
//				m_Device.ClearRenderTarget( m_SHEnvMap, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );

				m_SHEnvMapMaterial.GetVariableByName( "SHLight" ).AsVector.Set( _EnvLightSH );
				m_SHEnvMapMaterial.GetVariableByName( "EnableAmbientSH" ).AsScalar.Set( m_bEnableAmbientSH );
				m_SHEnvMapMaterial.GetVariableByName( "EnableIndirectSH" ).AsScalar.Set( m_bEnableIndirectSH );

				m_SHEnvMapMaterial.ApplyPass( 0 );
				m_EnvMesh.RenderOverride();
			}
		}

		/// <summary>
		/// Adds a new empty environment node (i.e. yet to be computed)
		/// </summary>
		/// <param name="_Position">The position of the node</param>
		public EnvironmentNode	AddEnvironmentNode( Vector3 _Position )
		{
			return AddEnvironmentNode( _Position, null );
		}

		/// <summary>
		/// Adds a new environment node with computed coefficients
		/// </summary>
		/// <param name="_Position">The position of the node</param>
		/// <param name="_SHCoefficients">The 9 SH coefficients encoding the environment's occlusion and reflection
		/// The RGB SH coefficients should encode indirect light reflection and A coefficients should encode direct lighting occlusion
		/// </param>
		public EnvironmentNode	AddEnvironmentNode( Vector3 _Position, Vector4[] _SHCoefficients )
		{
			EnvironmentNode	Node = new EnvironmentNode( this, _Position, _SHCoefficients );
			m_EnvNodes.Add( Node );
			m_bEnvMeshDirty = true;

			return Node;
		}

		/// <summary>
		/// Clears existing environment nodes
		/// </summary>
		public void				ClearEnvironmentNodes()
		{
			m_EnvNodes.Clear();
			m_bEnvMeshDirty = true;
		}

		/// <summary>
		/// Loads the environment nodes from a file
		/// </summary>
		/// <param name="_EnvNodesFile"></param>
		public void				LoadEnvironmentNodes( System.IO.FileInfo _EnvNodesFile )
		{
			using ( System.IO.FileStream S = _EnvNodesFile.OpenRead() )
				LoadEnvironmentNodes( S );
		}

		/// <summary>
		/// Loads the environment nodes from a stream
		/// </summary>
		/// <param name="_EnvNodesFile"></param>
		public void				LoadEnvironmentNodes( System.IO.Stream _EnvNodesStream )
		{
			using ( System.IO.BinaryReader R = new System.IO.BinaryReader( _EnvNodesStream ) )
			{
				int	NodesCount = R.ReadInt32();
				for ( int NodeIndex=0; NodeIndex < NodesCount; NodeIndex++ )
					m_EnvNodes.Add( new EnvironmentNode( this, R ) );
			}

			m_bEnvMeshDirty = true;
		}

		/// <summary>
		/// Saves the environment nodes to a file
		/// </summary>
		/// <param name="_EnvNodesFile"></param>
		public void				SaveEnvironmentNodes( System.IO.FileInfo _EnvNodesFile )
		{
			using ( System.IO.FileStream S = _EnvNodesFile.Create() )
				SaveEnvironmentNodes( S );
		}

		/// <summary>
		/// Saves the environment nodes to a stream
		/// </summary>
		/// <param name="_EnvNodesFile"></param>
		public void				SaveEnvironmentNodes( System.IO.Stream _EnvNodesStream )
		{
			using ( System.IO.BinaryWriter W = new System.IO.BinaryWriter( _EnvNodesStream ) )
			{
				W.Write( m_EnvNodes.Count );
				foreach ( EnvironmentNode N in m_EnvNodes )
					N.Write( W );
			}
		}

		#region Environment Nodes Rendering

		/// <summary>
		/// Starts the environment rendering
		/// </summary>
		/// <param name="_CubeMapsRenderer">The cube map renderer that will be able to render our environment nodes creation</param>
		/// <param name="_CubeMapSize">The size of the cube map used to sample the environment</param>
		/// <param name="_NearClip">Near clip for cube maps rendering</param>
		/// <param name="_FarClip">Far clip for cube maps rendering</param>
		/// <param name="_IndirectLightingBoostFactor">The boost factor to apply to indirect lighting (default is 1)</param>
		public void				BeginEnvironmentRendering( ISHCubeMapsRenderer _CubeMapsRenderer, int _CubeMapSize, float _NearClip, float _FarClip, float _IndirectLightingBoostFactor )
		{
			if ( _CubeMapsRenderer == null )
				throw new NException( this, "You didn't specify a CubeMapsRenderer when you created the SHEnvMapManager ! You cannot render cube maps or SHEnvironmentNodes..." );

			m_CubeMapsRenderer = _CubeMapsRenderer;
			m_CubeMapSize = _CubeMapSize;
			m_IndirectLightingBoostFactor = _IndirectLightingBoostFactor;

			// Notify we're starting...
			m_CubeMapsRenderer.BeginRender( _CubeMapSize );

			//////////////////////////////////////////////////////////////////////////
			// Create a camera that will feed our transforms
			m_CubeMapCamera = new Camera( m_Device, "CubeCamera" );
			m_CubeMapCamera.CreatePerspectiveCamera( 0.5f * (float) Math.PI, 1.0f, _NearClip, _FarClip );
			m_CubeMapCamera.Activate();

			//////////////////////////////////////////////////////////////////////////
			// Create the 2 albedo / normal textures
			m_CubeMapFaceAlbedo = new RenderTarget<PF_RGBA32F>( m_Device, "CubeMapAlbedo", m_CubeMapSize, m_CubeMapSize, 1 );
			m_CubeMapFaceNormalDepth = new RenderTarget<PF_RGBA32F>( m_Device, "CubeMapNormalDepth", m_CubeMapSize, m_CubeMapSize, 1 );

			//////////////////////////////////////////////////////////////////////////
			// Create the 7 targets that will store the 9 RGB coefficients for indirect lighting
			// (3*9 = 27 coefficients that are packed into 4*7 = 28 RGBA slots of 7 cube maps)
			for ( int i=0; i < 7; i++ )
				m_IndirectLighting[i] = new RenderTarget<PF_RGBA32F>( m_Device, "CubeMapIndirectLighting" + i, m_CubeMapSize, m_CubeMapSize, 1 );

			//////////////////////////////////////////////////////////////////////////
			// Create the staging resources where we will copy the cube map faces for encoding
			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.BindFlags = BindFlags.None;
			Desc.CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write;
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Staging;
			Desc.Format = SharpDX.DXGI.Format.R32G32B32A32_Float;
			Desc.ArraySize = 1;
			Desc.Width = m_CubeMapSize;
			Desc.Height = m_CubeMapSize;
			Desc.MipLevels = 1;
			Desc.SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 );
			m_StagingAlbedo = new Texture2D( m_Device.DirectXDevice, Desc );
			m_StagingNormalDepth = new Texture2D( m_Device.DirectXDevice, Desc );

			// Create the staging resource from which we will copy the resulting SH coefficients
			Desc.CpuAccessFlags = CpuAccessFlags.Read;
			for ( int i=0; i < 7; i++ )
				m_StagingIndirectLighting[i] = new Texture2D( m_Device.DirectXDevice, Desc );

			//////////////////////////////////////////////////////////////////////////
			// Create the screen quad
			m_Quad = new Helpers.ScreenQuad( m_Device, "CubeMapPostProcessQuad" );
		}

		/// <summary>
		/// Destroys cube map resources created for environment rendering
		/// </summary>
		public void				EndEnvironmentRendering()
		{
			// Dispose of render quad
			m_Quad.Dispose();

			// Dispose of camera
			m_CubeMapCamera.Dispose();

			// Delete render targets
			if ( m_CubeMapFaceAlbedo != null )
				m_CubeMapFaceAlbedo.Dispose();
			m_CubeMapFaceAlbedo = null;

			if ( m_CubeMapFaceNormalDepth != null )
				m_CubeMapFaceNormalDepth.Dispose();
			m_CubeMapFaceNormalDepth = null;

			for ( int i=0; i < 7; i++ )
				if ( m_IndirectLighting[i] != null )
					m_IndirectLighting[i].Dispose();

			// Delete staging resources
			if ( m_StagingAlbedo != null )
				m_StagingAlbedo.Dispose();
			m_StagingAlbedo = null;

			if ( m_StagingNormalDepth != null )
				m_StagingNormalDepth.Dispose();
			m_StagingNormalDepth = null;

			for ( int i=0; i < 7; i++ )
			{
				if ( m_StagingIndirectLighting[i] != null )
					m_StagingIndirectLighting[i].Dispose();
				m_StagingIndirectLighting[i] = null;
			}

			// Notify we stopped
			m_CubeMapsRenderer.EndRender();
		}

		/// <summary>
		/// Renders the scene's environment from the specified position
		/// </summary>
		/// <param name="_Position"></param>
		/// <param name="_At"></param>
		/// <param name="_Up"></param>
		public void				RenderCubeMap( Vector3 _Position, Vector3 _At, Vector3 _Up )
		{
			// Create the side transforms
			Matrix[]	SideTransforms = new Matrix[6]
			{
				Matrix.RotationY( +0.5f * (float) Math.PI ),	// +X (look right)
				Matrix.RotationY( -0.5f * (float) Math.PI ),	// -X (look left)
				Matrix.RotationX( -0.5f * (float) Math.PI ),	// +Y (look up)
				Matrix.RotationX( +0.5f * (float) Math.PI ),	// -Y (look down)
				Matrix.RotationY( +0.0f * (float) Math.PI ),	// +Z (look front) (default)
				Matrix.RotationY( +1.0f * (float) Math.PI ),	// -Z (look back)
			};

			// Create the main camera matrix
			m_EnvironmentCamera2World = Camera.CreateLookAt( _Position, _Position + _At, _Up );

			// Start rendering
			m_DepthBufferInfinity = m_CubeMapsRenderer.BeginRenderCubeMap();
			for ( int FaceIndex=0; FaceIndex < 6; FaceIndex++ )
			{
				Matrix	Side2World = SideTransforms[FaceIndex] * m_EnvironmentCamera2World;
				m_CubeMapCamera.Camera2World = Side2World;

				m_CubeMapsRenderer.RenderCubeMapFace( m_CubeMapCamera, FaceIndex );
			}
			m_CubeMapsRenderer.EndRenderCubeMap();

			// Destroy the camera
			m_CubeMapCamera.DeActivate();
		}

		/// <summary>
		/// Encodes the last rendered cube map environment into 9 SH vectors
		/// </summary>
		/// <returns>The 9 SH coefficients encoding the environment's occlusion for direct lighting
		/// NOTE: Only the W component is relevant, XYZ are filled up by the EncodeSHEnvironmentIndirect() method</returns>
		public Vector4[]		EncodeSHEnvironmentDirect()
		{
			// Create the side transforms
			Matrix[]	SideTransforms = new Matrix[6]
			{
				Matrix.RotationY( +0.5f * (float) Math.PI ),	// +X (look right)
				Matrix.RotationY( -0.5f * (float) Math.PI ),	// -X (look left)
				Matrix.RotationX( -0.5f * (float) Math.PI ),	// +Y (look up)
				Matrix.RotationX( +0.5f * (float) Math.PI ),	// -Y (look down)
				Matrix.RotationY( +0.0f * (float) Math.PI ),	// +Z (look front) (default)
				Matrix.RotationY( +1.0f * (float) Math.PI ),	// -Z (look back)
			};

			// Differential area of a cube map texel
			double	dA = 4.0 / (m_CubeMapSize*m_CubeMapSize);
			double	SumSolidAngle = 0.0;

			double	f0 = 0.5 / Math.Sqrt(Math.PI);
			double	f1 = Math.Sqrt(3.0) * f0;
			double	f2 = Math.Sqrt(15.0) * f0;
			double	f3 = f2 / (2.0 * Math.Sqrt(3.0));

			// Read back cube faces
			double[]	SHCoefficients = new double[9];
			double[]	CurrentSHCoefficients = new double[9];
			int[]		OccludedRaysCount = new int[6];
			int[]		FreeRaysCount = new int[6];
			int[]		SumRaysCount = new int[6];

			Vector3		Albedo = Vector3.Zero, Normal = Vector3.Zero;
			float		Depth = 0.0f;

			for ( int CubeFaceIndex=0; CubeFaceIndex < 6; CubeFaceIndex++ )
			{
				m_CubeMapsRenderer.BeginReadCubeMapFace( CubeFaceIndex );

				Matrix			Side2World = SideTransforms[CubeFaceIndex] * m_EnvironmentCamera2World;

				Vector3			ViewLocal, ViewWorld;
				ViewLocal.Z = 1.0f;
				for ( int Y=0; Y < m_CubeMapSize; Y++ )
				{
					ViewLocal.Y = 1.0f - 2.0f * Y / m_CubeMapSize;
					for ( int X=0; X < m_CubeMapSize; X++ )
					{
						// Read pixel values
						m_CubeMapsRenderer.ReadPixel( ref Albedo, ref Normal, ref Depth );
						if ( Depth < 0.99f * m_DepthBufferInfinity )
						{	// Here, we're only interested in knowing if the ray hit something (occlusion) or fled to infinity (no occlusion)
							OccludedRaysCount[CubeFaceIndex]++;	// Statistics
							continue;	// We hit something on the way... Don't add any contribution
						}
						FreeRaysCount[CubeFaceIndex]++;	// Statistics

						ViewLocal.X = 2.0f * X / m_CubeMapSize - 1.0f;
						ViewWorld = Vector3.TransformNormal( ViewLocal, Side2World );

						float	SqDistance = ViewWorld.LengthSquared();
						float	Distance = (float) Math.Sqrt( SqDistance );
						ViewWorld /= Distance;

						// Solid angle (from http://people.cs.kuleuven.be/~philip.dutre/GI/TotalCompendium.pdf)
						// dw = cos(Theta).dA / r²
						// cos(Theta) = Adjacent/Hypothenuse = 1/r
						//
						double	SolidAngle = dA / (Distance * SqDistance);
						SumSolidAngle += SolidAngle;

 						// Accumulate SH in that direction
						SHCoefficients[0] += SolidAngle * f0;
						SHCoefficients[1] += SolidAngle * -f1 * ViewWorld.X;
						SHCoefficients[2] += SolidAngle * f1 * ViewWorld.Y;
						SHCoefficients[3] += SolidAngle * -f1 * ViewWorld.Z;
						SHCoefficients[4] += SolidAngle * f2 * ViewWorld.X * ViewWorld.Z;
						SHCoefficients[5] += SolidAngle * -f2 * ViewWorld.X * ViewWorld.Y;
						SHCoefficients[6] += SolidAngle * f3 * (3.0 * ViewWorld.Y*ViewWorld.Y - 1.0);
						SHCoefficients[7] += SolidAngle * -f2 * ViewWorld.Z * ViewWorld.Y;
						SHCoefficients[8] += SolidAngle * f2 * 0.5 * (ViewWorld.Z*ViewWorld.Z - ViewWorld.X*ViewWorld.X);
					}
				}

				m_CubeMapsRenderer.EndReadCubeMapFace( CubeFaceIndex );

				// Statistics
				SumRaysCount[CubeFaceIndex] = FreeRaysCount[CubeFaceIndex] + OccludedRaysCount[CubeFaceIndex];
			}

			// Build the final vector that will only contain direct light occlusion
			Vector4[]	Result = new Vector4[9];
			for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
			{
				Result[SHCoeffIndex] = Vector4.Zero;
				Result[SHCoeffIndex].W = (float) SHCoefficients[SHCoeffIndex];
			}

			return Result;
		}

		/// <summary>
		/// Encodes the indirect lighting fom the cube map environment into 9 SH vectors
		/// </summary>
		/// <param name="_LightBounceIndex">The index of light bounce NOTE: Use -1 for the last pass ! (0 is direct, 1 is first indirect bounce, 2 is second bounce and so on...)</param>
		/// <returns>The 9 SH coefficients encoding the environment's indirect lighting
		/// NOTE: Only the XYZ components are relevant, W should have been filled up by the EncodeSHEnvironmentDirect() method</returns>
		public Vector4[]		EncodeSHEnvironmentIndirect( int _IndirectLightBounceIndex )
		{
			// Create the side transforms
			Matrix[]	SideTransforms = new Matrix[6]
			{
				Matrix.RotationY( +0.5f * (float) Math.PI ),	// +X (look right)
				Matrix.RotationY( -0.5f * (float) Math.PI ),	// -X (look left)
				Matrix.RotationX( -0.5f * (float) Math.PI ),	// +Y (look up)
				Matrix.RotationX( +0.5f * (float) Math.PI ),	// -Y (look down)
				Matrix.RotationY( +0.0f * (float) Math.PI ),	// +Z (look front) (default)
				Matrix.RotationY( +1.0f * (float) Math.PI ),	// -Z (look back)
			};

			// Create a uniform ambient SH light so we don't modify previous nodes SH coefficients
			// The long factor 3.54490(...) is the inverse of the first DC SH factor K00 = 1/2 * sqrt(1/PI)
			Vector4[]	EnvLightSH = new Vector4[9];
			if ( _IndirectLightBounceIndex == 1 )
				EnvLightSH[0] = new Vector4( 3.5449077018110320545963349666822f * Vector3.One, 0.0f );	// First bounce => keep direct occlusion
			else
				EnvLightSH[0] = new Vector4( Vector3.Zero, 3.5449077018110320545963349666822f );		// Next bounces => keep previous SH
			for ( int SHCoeffIndex=1; SHCoeffIndex < 9; SHCoeffIndex++ )
				EnvLightSH[SHCoeffIndex] = Vector4.Zero;

			// Differential area of a cube map texel
			double	dA = 4.0 / (m_CubeMapSize*m_CubeMapSize);
			double	SumSolidAngle = 0.0;

			// Backup states and allow ambient + indirect SH as we're computing them
			bool	bOldEnableAmbientSH = m_bEnableAmbientSH;
			bool	bOldEnableIndirectSH = m_bEnableIndirectSH;
			m_bEnableAmbientSH = true;
			m_bEnableIndirectSH = true;

			// Read back cube faces
			double[,]	SHCoefficients = new double[9,3];
			Vector3		Albedo = Vector3.Zero, Normal = Vector3.Zero;
			float		Depth = 0.0f;

			for ( int CubeFaceIndex=0; CubeFaceIndex < 6; CubeFaceIndex++ )
			{
				Matrix	Side2World = SideTransforms[CubeFaceIndex] * m_EnvironmentCamera2World;
				m_CubeMapCamera.Camera2World = Side2World;

				// =============================================
				// Transform the user's format to our own (i.e. 1 target with [Albedo(RGB)+0(A)], 1 target with [WorldNormal(RGB)+Depth(A)])
				m_CubeMapsRenderer.BeginReadCubeMapFace( CubeFaceIndex );

				DataStream		StreamAlbedo = null;
				DataRectangle	RectAlbedo = m_StagingAlbedo.Map( 0, MapMode.Write, MapFlags.None, out StreamAlbedo );
				DataStream		StreamNormalDepth = null;
				DataRectangle	RectNormalDepth = m_StagingNormalDepth.Map( 0, MapMode.Write, MapFlags.None, out StreamNormalDepth );

				for ( int Y=0; Y < m_CubeMapSize; Y++ )
					for ( int X=0; X < m_CubeMapSize; X++ )
					{
						// Read unknown format
						m_CubeMapsRenderer.ReadPixel( ref Albedo, ref Normal, ref Depth );

						// Write our format
						StreamAlbedo.Write( new Vector4( Albedo, 0.0f ) );
						StreamNormalDepth.Write( new Vector4( Normal, Depth ) );
					}

				StreamAlbedo.Dispose();
				m_StagingAlbedo.Unmap( 0 );
				StreamNormalDepth.Dispose();
				m_StagingNormalDepth.Unmap( 0 );

				m_CubeMapsRenderer.EndReadCubeMapFace( CubeFaceIndex );

				// Copy from staging to actual targets
				m_Device.DirectXDevice.CopyResource( m_StagingAlbedo, m_CubeMapFaceAlbedo.Texture );
				m_Device.DirectXDevice.CopyResource( m_StagingNormalDepth, m_CubeMapFaceNormalDepth.Texture );

				// =============================================
				// Post-process cube map face

				// Render the environment map so we can re-use any previously computed SH coefficients
				m_Device.ClearRenderTarget( m_SHEnvMap, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
				RenderSHEnvironmentMap( m_CubeMapCamera, EnvLightSH );

				// Post-process
				PostProcessCubeMapFace();

				// =============================================
				// Map SH indirect lighting
				DataStream[]	StreamIndirectLighting = new DataStream[7];
				DataRectangle[]	RectIndirectLighting = new DataRectangle[7];
				for ( int i=0; i < 7; i++ )
					RectIndirectLighting[i] = m_StagingIndirectLighting[i].Map( 0, MapMode.Read, MapFlags.None, out StreamIndirectLighting[i] );
				RectNormalDepth = m_StagingNormalDepth.Map( 0, MapMode.Read, MapFlags.None, out StreamNormalDepth );

				Vector3			ViewLocal, ViewWorld;
				ViewLocal.Z = 1.0f;
				for ( int Y=0; Y < m_CubeMapSize; Y++ )
				{
					ViewLocal.Y = 1.0f - 2.0f * Y / m_CubeMapSize;
					for ( int X=0; X < m_CubeMapSize; X++ )
					{
						ViewLocal.X = 2.0f * X / m_CubeMapSize - 1.0f;

						Vector4	Geometry = StreamNormalDepth.Read<Vector4>();

						Vector4	Coeffs0 = StreamIndirectLighting[0].Read<Vector4>();
						Vector4	Coeffs1 = StreamIndirectLighting[1].Read<Vector4>();
						Vector4	Coeffs2 = StreamIndirectLighting[2].Read<Vector4>();
						Vector4	Coeffs3 = StreamIndirectLighting[3].Read<Vector4>();
						Vector4	Coeffs4 = StreamIndirectLighting[4].Read<Vector4>();
						Vector4	Coeffs5 = StreamIndirectLighting[5].Read<Vector4>();
						Vector4	Coeffs6 = StreamIndirectLighting[6].Read<Vector4>();

						// Here, we're only interested in knowing if the ray hit something (reflection) or fled to infinity (direct lighting, which was already computed)
						float	HitDistance = Geometry.W;
						if ( HitDistance > 0.99f * m_DepthBufferInfinity )
							continue;	// We didn't hit anything on the way... Don't add any contribution

						ViewWorld = Vector3.TransformNormal( ViewLocal, Side2World );

						float	SqDistance = ViewWorld.LengthSquared();
						float	Distance = (float) Math.Sqrt( SqDistance );
						ViewWorld /= Distance;

						// Solid angle (from http://people.cs.kuleuven.be/~philip.dutre/GI/TotalCompendium.pdf)
						// dw = cos(Theta).dA / r²
						// cos(Theta) = Adjacent/Hypothenuse = 1/r
						//
						double	SolidAngle = dA / (Distance * SqDistance);
						SumSolidAngle += SolidAngle;

						// Accumulate SH in that direction
						SHCoefficients[0,0] += SolidAngle * Coeffs0.X;
						SHCoefficients[0,1] += SolidAngle * Coeffs0.Y;
						SHCoefficients[0,2] += SolidAngle * Coeffs0.Z;
						SHCoefficients[1,0] += SolidAngle * Coeffs0.W;
						SHCoefficients[1,1] += SolidAngle * Coeffs1.X;
						SHCoefficients[1,2] += SolidAngle * Coeffs1.Y;
						SHCoefficients[2,0] += SolidAngle * Coeffs1.Z;
						SHCoefficients[2,1] += SolidAngle * Coeffs1.W;
						SHCoefficients[2,2] += SolidAngle * Coeffs2.X;
						SHCoefficients[3,0] += SolidAngle * Coeffs2.Y;
						SHCoefficients[3,1] += SolidAngle * Coeffs2.Z;
						SHCoefficients[3,2] += SolidAngle * Coeffs2.W;
						SHCoefficients[4,0] += SolidAngle * Coeffs3.X;
						SHCoefficients[4,1] += SolidAngle * Coeffs3.Y;
						SHCoefficients[4,2] += SolidAngle * Coeffs3.Z;
						SHCoefficients[5,0] += SolidAngle * Coeffs3.W;
						SHCoefficients[5,1] += SolidAngle * Coeffs4.X;
						SHCoefficients[5,2] += SolidAngle * Coeffs4.Y;
						SHCoefficients[6,0] += SolidAngle * Coeffs4.Z;
						SHCoefficients[6,1] += SolidAngle * Coeffs4.W;
						SHCoefficients[6,2] += SolidAngle * Coeffs5.X;
						SHCoefficients[7,0] += SolidAngle * Coeffs5.Y;
						SHCoefficients[7,1] += SolidAngle * Coeffs5.Z;
						SHCoefficients[7,2] += SolidAngle * Coeffs5.W;
						SHCoefficients[8,0] += SolidAngle * Coeffs6.X;
						SHCoefficients[8,1] += SolidAngle * Coeffs6.Y;
						SHCoefficients[8,2] += SolidAngle * Coeffs6.Z;
					}
				}

				StreamNormalDepth.Dispose();
				m_StagingNormalDepth.Unmap( 0 );
				for ( int i=0; i < 7; i++ )
				{
					StreamIndirectLighting[i].Dispose();
					m_StagingIndirectLighting[i].Unmap( 0 );
				}
			}

			// Build the final vector
			Vector4[]	Result = new Vector4[9];
			for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
			{
				Result[SHCoeffIndex].X = (float) SHCoefficients[SHCoeffIndex,0];
				Result[SHCoeffIndex].Y = (float) SHCoefficients[SHCoeffIndex,1];
				Result[SHCoeffIndex].Z = (float) SHCoefficients[SHCoeffIndex,2];
				Result[SHCoeffIndex].W = 0.0f;
			}

			// Restore states
			m_bEnableAmbientSH = bOldEnableAmbientSH;
			m_bEnableIndirectSH = bOldEnableIndirectSH;

			return Result;
		}

		/// <summary>
		/// Applies the post-process to the cube map face to generate indirect lighting SH coefficients
		/// </summary>
		protected void			PostProcessCubeMapFace()
		{
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
			m_Device.SetViewport( 0, 0, m_CubeMapSize, m_CubeMapSize, 0.0f, 1.0f );

			RenderTargetView[]	TargetsIndirectLighting = new RenderTargetView[7];
			for ( int i=0; i < 7; i++ )
				TargetsIndirectLighting[i] = m_IndirectLighting[i].GetSingleRenderTargetView( 0, 0 );

			using ( m_MaterialPostProcessCubeMapFace.UseLock() )
			{
				m_Device.SetMultipleRenderTargets( TargetsIndirectLighting );

// 				if ( _IndirectLightBounceIndex != -1 )
// 					m_MaterialPostProcessCubeMapFace.CurrentTechnique = m_MaterialPostProcessCubeMapFace.GetTechniqueByName( "IndirectLighting" );
// 				else
// 					// Use final gather technique to capture the finished environment
// 					m_MaterialPostProcessCubeMapFace.CurrentTechnique = m_MaterialPostProcessCubeMapFace.GetTechniqueByName( "IndirectLighting_FinalGather" );	

				m_MaterialPostProcessCubeMapFace.GetVariableByName( "BufferInvSize" ).AsScalar.Set( 1.0f / m_CubeMapSize );
				m_MaterialPostProcessCubeMapFace.GetVariableByName( "IndirectLightingBoostFactor" ).AsScalar.Set( m_IndirectLightingBoostFactor );
				m_MaterialPostProcessCubeMapFace.GetVariableByName( "MaterialBuffer" ).AsResource.SetResource( m_CubeMapFaceAlbedo );
				m_MaterialPostProcessCubeMapFace.GetVariableByName( "GeometryBuffer" ).AsResource.SetResource( m_CubeMapFaceNormalDepth );

				m_MaterialPostProcessCubeMapFace.ApplyPass( 0 );
				m_Quad.Render();
			}

			// Copy rendered SH to staging resources
			for ( int i=0; i < 7; i++ )
				m_Device.DirectXDevice.CopyResource( m_IndirectLighting[i].Texture, m_StagingIndirectLighting[i] );
		}

		#endregion

		#region Environment Mesh Construction

		/// <summary>
		/// Clears any existing environment mesh
		/// </summary>
		protected void	ClearEnvironmentMesh()
		{
			if ( m_EnvMesh != null )
				m_EnvMesh.Dispose();
			m_EnvMesh = null;
			m_bEnvMeshDirty = true;
		}

		protected int[]	m_LastEnvMeshPrimitiveIndices = null;

		/// <summary>
		/// Triangulates the environment nodes to build an environment mesh of environmental SH vertices
		///  that will laater be rendered into a world space environment map
		/// </summary>
		public void	BuildEnvironmentMesh()
		{
			if ( m_EnvNodes.Count < 3 )
				throw new NException( this, "There must be at least 3 environment nodes to build a valid environment mesh !" );
			if ( !m_bEnvMeshDirty )
			{	// Already built !
				if ( !m_bEnvMeshCoefficientsDirty )
					return;	// Coefficients are also up to date...

				m_EnvMesh.Dispose();

				// Update mesh coefficients only
				VS_P3SH9[]	NewPrimitiveVertices = new VS_P3SH9[m_EnvNodes.Count];
				for ( int Vertexindex=0; Vertexindex < m_EnvNodes.Count; Vertexindex++ )
					NewPrimitiveVertices[Vertexindex] = m_EnvNodes[Vertexindex].V;

				m_EnvMesh = new Primitive<VS_P3SH9,int>( m_Device, "EnvMesh", PrimitiveTopology.TriangleList, NewPrimitiveVertices, m_LastEnvMeshPrimitiveIndices );

				m_bEnvMeshCoefficientsDirty = false;
				return;
			}

			if ( m_EnvMesh != null )
				m_EnvMesh.Dispose();

			//////////////////////////////////////////////////////////////////////////
			// Apply Delaunay triangulation on the env nodes
			List<DelaunayTriangle>	Triangles = new List<DelaunayTriangle>();
			DelaunayTriangle.ms_Index = 0;

			Vector2[]	Vertices = new Vector2[m_EnvNodes.Count];
			for ( int VertexIndex=0; VertexIndex < m_EnvNodes.Count; VertexIndex++ )
				Vertices[VertexIndex] = new Vector2( m_EnvNodes[VertexIndex].V.Position.X, m_EnvNodes[VertexIndex].V.Position.Z );

			// 1] First, the seed triangle
			int	V0 = 0, V1 = 1, V2 = 2;
			Vector2	D0 = Vertices[V2] - Vertices[V1];
			Vector2	D1 = Vertices[V0] - Vertices[V1];
			float	CrossZ = D0.X * D1.Y - D0.Y * D1.X;
			if ( CrossZ < 0.0f )
			{	// Make the first triangle CCW !
				V1 = 2;
				V2 = 1;
			}

			DelaunayTriangle	T = new DelaunayTriangle( Vertices, V0, V1, V2, null, null, null );
			Triangles.Add( T );

			// 2] Next, the remaining vertices that we add one by one
			for ( int VertexIndex=3; VertexIndex < Vertices.Length; VertexIndex++ )
			{
				// Get current vertex's 2D position
				Vector2	Position = Vertices[VertexIndex];

				// 2.1] Check if the vertex belongs to an existing triangle, or the closest triangle for that matter
				float				ClosestEdgeSqDistance = +float.MaxValue;
				DelaunayTriangle	ClosestTriangle = null;
				int					ClosestEdgeIndex = -1;

				float[]	s = new float[3];	// Edge segments' parameters (inside edge if 0 <= s <= 1)
				bool[]	b = new bool[3];	// Inside states
				float[]	d = new float[3];	// Distances to edges (whether the vertex is inside the edge or not)
				for ( int TriangleIndex=0; TriangleIndex < Triangles.Count; TriangleIndex++ )
				{
					T = Triangles[TriangleIndex];

					// Check the vertex is inside that triangle
					b[0] = Vector2.Dot( Position - T[0], T.Edges[0].Normal ) <= 0.0f;
					b[1] = Vector2.Dot( Position - T[1], T.Edges[1].Normal ) <= 0.0f;
					b[2] = Vector2.Dot( Position - T[2], T.Edges[2].Normal ) <= 0.0f;

					if ( b[0] && b[1] && b[2] )
					{	// We're inside this triangle !
						// Split the tirangle in 3 other triangles by inserting the new vertex
						ClosestTriangle = T;
						ClosestEdgeIndex = -1;
						break;
					}

					// Compute the distance to the 3 edges of that triangle
					s[0] = Vector2.Dot( Position - T[0], T[1] - T[0] ) / (T.Edges[0].Length*T.Edges[0].Length);
					s[1] = Vector2.Dot( Position - T[1], T[2] - T[1] ) / (T.Edges[1].Length*T.Edges[1].Length);
					s[2] = Vector2.Dot( Position - T[2], T[0] - T[2] ) / (T.Edges[2].Length*T.Edges[2].Length);

					d[0] = (Position - Vector2.Lerp( T[0], T[1], s[0] )).LengthSquared();
					if ( d[0] < ClosestEdgeSqDistance )
					{
						ClosestEdgeSqDistance = d[0];
						ClosestTriangle = T;
						ClosestEdgeIndex = 0;
					}
					d[1] = (Position - Vector2.Lerp( T[1], T[2], s[1] )).LengthSquared();
					if ( d[1] < ClosestEdgeSqDistance )
					{
						ClosestEdgeSqDistance = d[1];
						ClosestTriangle = T;
						ClosestEdgeIndex = 1;
					}
					d[2] = (Position - Vector2.Lerp( T[2], T[0], s[2] )).LengthSquared();
					if ( d[2] < ClosestEdgeSqDistance )
					{
						ClosestEdgeSqDistance = d[2];
						ClosestTriangle = T;
						ClosestEdgeIndex = 2;
					}
				}

				if ( ClosestTriangle == null )
					throw new Exception( "Couldn't find a candidate triangle !" );

				// 2.2] Split the existing triangle and flip its edges
				if ( ClosestEdgeIndex == -1 )
				{	
					// Create 2 additional triangles
					DelaunayTriangle	T0 = new DelaunayTriangle( Vertices,
							ClosestTriangle.Vertices[1],
							ClosestTriangle.Vertices[2],
							VertexIndex,
							ClosestTriangle.Edges[1].Adjacent,
							null,
							null
							);
					Triangles.Add( T0 );

					DelaunayTriangle	T1 = new DelaunayTriangle( Vertices,
							ClosestTriangle.Vertices[2],
							ClosestTriangle.Vertices[0],
							VertexIndex,
							ClosestTriangle.Edges[2].Adjacent,
							null,
							null
							);
					Triangles.Add( T1 );

					// Change current triangle's 3rd vertex
					ClosestTriangle.Vertices[2] = VertexIndex;
					ClosestTriangle.UpdateInfos();

					// Update adjacencies
					T0.Edges[1].Adjacent = T1;
					T1.Edges[1].Adjacent = ClosestTriangle;
					ClosestTriangle.Edges[1].Adjacent = T0;

					// Flip the 3 edges
					ClosestTriangle.Edges[0].Flip( VertexIndex );
					T0.Edges[0].Flip( VertexIndex );
					T1.Edges[0].Flip( VertexIndex );

					continue;
				}

				// 2.3] Attach new triangles to the mesh
				// We have 3 cases to handle here :
				//
				//          Edge1
				//    xxxxxxxxx\     A0     .
				//    xxxxxxxxxx\         .
				//    xxxxxxxxxxx\      .
				//    xxxxxxxxxxxx\   .     A1
				//   ______________o._ _ _ _ _ _
				//   Edge0         |\
				//                    
				//                 |  \    A2
				//                     
				//                 |    \
				//
				// The vertex lies in area A0 :
				//	_ We must create a new triangle connected to Edge1 and flip Edge1
				//
				// The vertex lies in area A1 :
				//	_ We must create a new triangle connected to Edge1 and flip Edge1
				//
				// The vertex lies in area A2 :
				//	_ We must create 2 new triangles connected to both Edge0 and Edge1
				//
				float	DistanceToEdgeVertex0 = (Position - ClosestTriangle[ClosestEdgeIndex]).Length();
				float	DistanceToEdgeVertex1 = (Position - ClosestTriangle[ClosestEdgeIndex+1]).Length();
				int		OtherCandidateEdgeIndex = (DistanceToEdgeVertex0 < DistanceToEdgeVertex1 ? ClosestEdgeIndex+3-1 : ClosestEdgeIndex+1) % 3;
				Vector2	TipVertex = DistanceToEdgeVertex0 < DistanceToEdgeVertex1 ? ClosestTriangle[ClosestEdgeIndex] : ClosestTriangle[ClosestEdgeIndex+1];
				Vector2	N0 = ClosestTriangle.Edges[ClosestEdgeIndex].Normal;
				Vector2	N1 = ClosestTriangle.Edges[OtherCandidateEdgeIndex].Normal;
				bool	bInFrontEdge0 = Vector2.Dot( Position - TipVertex, N0 ) >= 0.0f;
				bool	bInFrontEdge1 = Vector2.Dot( Position - TipVertex, N1 ) >= 0.0f;
				if ( bInFrontEdge0 && !bInFrontEdge1 )
				{	// Add to edge 0
					T = new DelaunayTriangle( Vertices,
							ClosestTriangle.Vertices[ClosestEdgeIndex+1],
							ClosestTriangle.Vertices[ClosestEdgeIndex],
							VertexIndex,
							ClosestTriangle,
							null, null
							);
					Triangles.Add( T );
					ClosestTriangle.Edges[ClosestEdgeIndex].Adjacent = T;

					T.Edges[0].Flip( VertexIndex );
				}
				else if ( !bInFrontEdge0 && bInFrontEdge1 )
				{	// Add to edge 1
					T = new DelaunayTriangle( Vertices,
							ClosestTriangle.Vertices[OtherCandidateEdgeIndex+1],
							ClosestTriangle.Vertices[OtherCandidateEdgeIndex],
							VertexIndex,
							ClosestTriangle,
							null, null
							);
					Triangles.Add( T );
					ClosestTriangle.Edges[OtherCandidateEdgeIndex].Adjacent = T;

					T.Edges[0].Flip( VertexIndex );
				}
				else if ( bInFrontEdge0 && bInFrontEdge1 )
				{	// Add to both edges
					DelaunayTriangle	T0 = new DelaunayTriangle( Vertices,
							ClosestTriangle.Vertices[ClosestEdgeIndex+1],
							ClosestTriangle.Vertices[ClosestEdgeIndex],
							VertexIndex,
							ClosestTriangle,
							null, null
							);
					Triangles.Add( T0 );
					ClosestTriangle.Edges[ClosestEdgeIndex].Adjacent = T0;

					DelaunayTriangle	T1 = new DelaunayTriangle( Vertices,
							ClosestTriangle.Vertices[OtherCandidateEdgeIndex+1],
							ClosestTriangle.Vertices[OtherCandidateEdgeIndex],
							VertexIndex,
							ClosestTriangle,
							null, null
							);
					Triangles.Add( T1 );
					ClosestTriangle.Edges[OtherCandidateEdgeIndex].Adjacent = T1;

					if ( (OtherCandidateEdgeIndex+1)%3 == ClosestEdgeIndex )
					{	// Other edge is previous edge
						T0.Edges[1].Adjacent = T1;
						T1.Edges[2].Adjacent = T0;
					}
					else if ( (ClosestEdgeIndex+1)%3 == OtherCandidateEdgeIndex )
					{	// Other edge is next edge
						T0.Edges[2].Adjacent = T1;
						T1.Edges[1].Adjacent = T0;
					}
					else
						throw new Exception( "We got 2 edges that are not consecutive !" );
				}
				else
					throw new Exception( "Should be in front of at least one edge !" );
			}

			//////////////////////////////////////////////////////////////////////////
			// Build the final primitive
			VS_P3SH9[]	PrimitiveVertices = new VS_P3SH9[m_EnvNodes.Count];
			for ( int Vertexindex=0; Vertexindex < m_EnvNodes.Count; Vertexindex++ )
				PrimitiveVertices[Vertexindex] = m_EnvNodes[Vertexindex].V;

			m_LastEnvMeshPrimitiveIndices = new int[3*Triangles.Count];
			for ( int TriangleIndex=0; TriangleIndex < Triangles.Count; TriangleIndex++ )
			{
				T = Triangles[TriangleIndex];
				m_LastEnvMeshPrimitiveIndices[3*TriangleIndex+0] = T.Vertices[0];
				m_LastEnvMeshPrimitiveIndices[3*TriangleIndex+1] = T.Vertices[1];
				m_LastEnvMeshPrimitiveIndices[3*TriangleIndex+2] = T.Vertices[2];
			}

			m_EnvMesh = new Primitive<VS_P3SH9,int>( m_Device, "EnvMesh", PrimitiveTopology.TriangleList, PrimitiveVertices, m_LastEnvMeshPrimitiveIndices );

			m_bEnvMeshDirty = false;
			m_bEnvMeshCoefficientsDirty = false;
		}

		#endregion

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			// Provide environment SH map interface data
			IEnvironmentSHMap	I2 = _Interface as IEnvironmentSHMap;
			if ( I2 != null )
			{
				I2.SHEnvMapOffset = m_SHEnvOffset;
				I2.SHEnvMapScale = m_SHEnvScale;
				I2.SHEnvMapSize = m_SHEnvMapSize;
				I2.SHEnvMap = m_SHEnvMap;
			}
		}

		#endregion

		#endregion
	}
}
