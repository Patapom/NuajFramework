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
	/// This setups and encapsulates a complex deferred renderer with plenty of pipelines and plenty of render techniques
	/// </summary>
	public class RendererSetupDeferred : Component, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const float	LIGHT_BUFFER_SIZE_RATIO = 0.5f;
		public const float		DEPTH_BUFFER_INFINITY = 10000.0f;

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// Deferred Shading Support
		/// </summary>
		public class	IDeferredRendering : ShaderInterfaceBase
		{
			[Semantic( "GBUFFER_TEX0" )]
			public RenderTarget<PF_RGBA16F>	GBuffer0	{ set { SetResource( "GBUFFER_TEX0", value ); } }
			[Semantic( "GBUFFER_TEX1" )]
			public RenderTarget<PF_RGBA16F>	GBuffer1	{ set { SetResource( "GBUFFER_TEX1", value ); } }
			[Semantic( "GBUFFER_TEX2" )]
			public RenderTarget<PF_RGBA16F>	GBuffer2	{ set { SetResource( "GBUFFER_TEX2", value ); } }
			[Semantic( "LIGHTBUFFER_TEX" )]
			public RenderTarget<PF_RGBA16F>	LightBuffer	{ set { SetResource( "LIGHTBUFFER_TEX", value ); } }
		}

// 		/// <summary>
// 		/// Environment SH Map Support
// 		/// </summary>
// 		public class	IEnvironmentSHMap : ShaderInterfaceBase
// 		{
// 			[Semantic( "SHENVMAP_OFFSET" )]
// 			public Vector2						SHEnvMapOffset	{ set { SetVector( "SHENVMAP_OFFSET", value ); } }
// 			[Semantic( "SHENVMAP_SCALE" )]
// 			public Vector2						SHEnvMapScale	{ set { SetVector( "SHENVMAP_SCALE", value ); } }
// 			[Semantic( "SHENVMAP_SIZE" )]
// 			public Vector2						SHEnvMapSize	{ set { SetVector( "SHENVMAP_SIZE", value ); } }
// 			[Semantic( "SHENVMAP" )]
// 			public RenderTarget3D<PF_RGBA16F>	SHEnvMap		{ set { SetResource( "SHENVMAP", value ); } }
// 		}
// 
// 		/// <summary>
// 		/// This vertex will encode an environment node's 9 SH coefficients
// 		/// </summary>
// 		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
// 		public struct VS_P3SH9
// 		{
// 			[Semantic( SemanticAttribute.POSITION )]
// 			public Vector3	Position;
// 			[Semantic( "SH", 0 )]
// 			public Vector4	SH0;
// 			[Semantic( "SH", 1 )]
// 			public Vector4	SH1;
// 			[Semantic( "SH", 2 )]
// 			public Vector4	SH2;
// 			[Semantic( "SH", 3 )]
// 			public Vector4	SH3;
// 			[Semantic( "SH", 4 )]
// 			public Vector4	SH4;
// 			[Semantic( "SH", 5 )]
// 			public Vector4	SH5;
// 			[Semantic( "SH", 6 )]
// 			public Vector4	SH6;
// 			[Semantic( "SH", 7 )]
// 			public Vector4	SH7;
// 			[Semantic( "SH", 8 )]
// 			public Vector4	SH8;
// 		}
// 
// 		public class	EnvironmentNode
// 		{
// 			#region FIELDS
// 
// 			protected RendererSetupDeferred	m_Owner;
// 			public VS_P3SH9					V;
// 
// 			#endregion
// 
// 			#region METHODS
// 
// 			public EnvironmentNode( RendererSetupDeferred _Owner, Vector3 _Position, Vector4[] _SHCoefficients )
// 			{
// 				m_Owner = _Owner;
// 				V.Position = _Position;
// 				UpdateCoefficients( _SHCoefficients );
// 			}
// 
// 			public EnvironmentNode( RendererSetupDeferred _Owner, System.IO.BinaryReader _Reader )
// 			{
// 				m_Owner = _Owner;
// 
// 				// Read back the vertex
// 				V.Position.X = _Reader.ReadSingle();
// 				V.Position.Y = _Reader.ReadSingle();
// 				V.Position.Z = _Reader.ReadSingle();
// 
// 				V.SH0.X = _Reader.ReadSingle();
// 				V.SH0.Y = _Reader.ReadSingle();
// 				V.SH0.Z = _Reader.ReadSingle();
// 				V.SH0.W = _Reader.ReadSingle();
// 
// 				V.SH1.X = _Reader.ReadSingle();
// 				V.SH1.Y = _Reader.ReadSingle();
// 				V.SH1.Z = _Reader.ReadSingle();
// 				V.SH1.W = _Reader.ReadSingle();
// 
// 				V.SH2.X = _Reader.ReadSingle();
// 				V.SH2.Y = _Reader.ReadSingle();
// 				V.SH2.Z = _Reader.ReadSingle();
// 				V.SH2.W = _Reader.ReadSingle();
// 
// 				V.SH3.X = _Reader.ReadSingle();
// 				V.SH3.Y = _Reader.ReadSingle();
// 				V.SH3.Z = _Reader.ReadSingle();
// 				V.SH3.W = _Reader.ReadSingle();
// 
// 				V.SH4.X = _Reader.ReadSingle();
// 				V.SH4.Y = _Reader.ReadSingle();
// 				V.SH4.Z = _Reader.ReadSingle();
// 				V.SH4.W = _Reader.ReadSingle();
// 
// 				V.SH5.X = _Reader.ReadSingle();
// 				V.SH5.Y = _Reader.ReadSingle();
// 				V.SH5.Z = _Reader.ReadSingle();
// 				V.SH5.W = _Reader.ReadSingle();
// 
// 				V.SH6.X = _Reader.ReadSingle();
// 				V.SH6.Y = _Reader.ReadSingle();
// 				V.SH6.Z = _Reader.ReadSingle();
// 				V.SH6.W = _Reader.ReadSingle();
// 
// 				V.SH7.X = _Reader.ReadSingle();
// 				V.SH7.Y = _Reader.ReadSingle();
// 				V.SH7.Z = _Reader.ReadSingle();
// 				V.SH7.W = _Reader.ReadSingle();
// 
// 				V.SH8.X = _Reader.ReadSingle();
// 				V.SH8.Y = _Reader.ReadSingle();
// 				V.SH8.Z = _Reader.ReadSingle();
// 				V.SH8.W = _Reader.ReadSingle();
// 			}
// 
// 			public void	Write( System.IO.BinaryWriter _Writer )
// 			{
// 				_Writer.Write( V.Position.X );
// 				_Writer.Write( V.Position.Y );
// 				_Writer.Write( V.Position.Z );
// 
// 				_Writer.Write( V.SH0.X );
// 				_Writer.Write( V.SH0.Y );
// 				_Writer.Write( V.SH0.Z );
// 				_Writer.Write( V.SH0.W );
// 
// 				_Writer.Write( V.SH1.X );
// 				_Writer.Write( V.SH1.Y );
// 				_Writer.Write( V.SH1.Z );
// 				_Writer.Write( V.SH1.W );
// 
// 				_Writer.Write( V.SH2.X );
// 				_Writer.Write( V.SH2.Y );
// 				_Writer.Write( V.SH2.Z );
// 				_Writer.Write( V.SH2.W );
// 
// 				_Writer.Write( V.SH3.X );
// 				_Writer.Write( V.SH3.Y );
// 				_Writer.Write( V.SH3.Z );
// 				_Writer.Write( V.SH3.W );
// 
// 				_Writer.Write( V.SH4.X );
// 				_Writer.Write( V.SH4.Y );
// 				_Writer.Write( V.SH4.Z );
// 				_Writer.Write( V.SH4.W );
// 
// 				_Writer.Write( V.SH5.X );
// 				_Writer.Write( V.SH5.Y );
// 				_Writer.Write( V.SH5.Z );
// 				_Writer.Write( V.SH5.W );
// 
// 				_Writer.Write( V.SH6.X );
// 				_Writer.Write( V.SH6.Y );
// 				_Writer.Write( V.SH6.Z );
// 				_Writer.Write( V.SH6.W );
// 
// 				_Writer.Write( V.SH7.X );
// 				_Writer.Write( V.SH7.Y );
// 				_Writer.Write( V.SH7.Z );
// 				_Writer.Write( V.SH7.W );
// 
// 				_Writer.Write( V.SH8.X );
// 				_Writer.Write( V.SH8.Y );
// 				_Writer.Write( V.SH8.Z );
// 				_Writer.Write( V.SH8.W );
// 			}
// 
// 			/// <summary>
// 			/// Updates the node's coefficients (will trigger a primitive update next time the env mesh is used)
// 			/// </summary>
// 			/// <param name="_SHCoefficients"></param>
// 			public void	UpdateCoefficients( Vector4[] _SHCoefficients )
// 			{
// 				if ( _SHCoefficients == null )
// 					return;
// 
// 				V.SH0 = _SHCoefficients[0];
// 				V.SH1 = _SHCoefficients[1];
// 				V.SH2 = _SHCoefficients[2];
// 				V.SH3 = _SHCoefficients[3];
// 				V.SH4 = _SHCoefficients[4];
// 				V.SH5 = _SHCoefficients[5];
// 				V.SH6 = _SHCoefficients[6];
// 				V.SH7 = _SHCoefficients[7];
// 				V.SH8 = _SHCoefficients[8];
// 
// 				// The environment mesh must be recomputed !
// 				m_Owner.m_bEnvMeshCoefficientsDirty = true;
// 			}
// 
// 			/// <summary>
// 			/// Updates the node's coefficients (will trigger a primitive update next time the env mesh is used)
// 			/// </summary>
// 			/// <param name="_SHCoefficients"></param>
// 			/// <remarks>As the indirect-lit objects need to sample the environment around them, we store the coefficients in the opposite direction)</remarks>
// 			public void	UpdateCoefficientsReflected( Vector4[] _SHCoefficients )
// 			{
// 				if ( _SHCoefficients == null )
// 					return;
// 
// 				V.SH0 = _SHCoefficients[0];
// 				V.SH1 = new Vector4( -_SHCoefficients[1].X, -_SHCoefficients[1].Y, -_SHCoefficients[1].Z, _SHCoefficients[1].W );
// 				V.SH2 = new Vector4( -_SHCoefficients[2].X, -_SHCoefficients[2].Y, -_SHCoefficients[2].Z, _SHCoefficients[2].W );
// 				V.SH3 = new Vector4( -_SHCoefficients[3].X, -_SHCoefficients[3].Y, -_SHCoefficients[3].Z, _SHCoefficients[3].W );
// 				V.SH4 = _SHCoefficients[4];
// 				V.SH5 = _SHCoefficients[5];
// 				V.SH6 = _SHCoefficients[6];
// 				V.SH7 = _SHCoefficients[7];
// 				V.SH8 = _SHCoefficients[8];
// 
// 				// The environment mesh must be recomputed !
// 				m_Owner.m_bEnvMeshCoefficientsDirty = true;
// 			}
// 
// 			/// <summary>
// 			/// Creates a cosine lobe occlusion SH vector
// 			/// </summary>
// 			/// <returns></returns>
// 			public void	MakeCosineLobe( Vector3 _Direction )
// 			{
// 				double[]	ZHCoeffs = new double[3]
// 				{
// 					0.88622692545275801364908374167057,	// sqrt(PI) / 2
// 					1.0233267079464884884795516248893,	// sqrt(PI / 3)
// 					0.49541591220075137666812859564002	// sqrt(5PI) / 8
// 				};
// 
// 				double	cl0 = 3.5449077018110320545963349666823 * ZHCoeffs[0];
// 				double	cl1 = 2.0466534158929769769591032497785 * ZHCoeffs[1];
// 				double	cl2 = 1.5853309190424044053380115060481 * ZHCoeffs[2];
// 
// 				double	f0 = 0.5 / Math.Sqrt(Math.PI);
// 				double	f1 = Math.Sqrt(3.0) * f0;
// 				double	f2 = Math.Sqrt(15.0) * f0;
// 				f0 *= cl0;
// 				f1 *= cl1;
// 				f2 *= cl2;
// 
// 				Vector4[]	SHCoeffs = new Vector4[9];
// 				SHCoeffs[0].W = (float) f0;
// 				SHCoeffs[1].W = (float) (-f1 * _Direction.X);
// 				SHCoeffs[2].W = (float) (f1 * _Direction.Y);
// 				SHCoeffs[3].W = (float) (-f1 * _Direction.Z);
// 				SHCoeffs[4].W = (float) (f2 * _Direction.X * _Direction.Z);
// 				SHCoeffs[5].W = (float) (-f2 * _Direction.X * _Direction.Y);
// 				SHCoeffs[6].W = (float) (f2 / (2.0 * Math.Sqrt(3.0)) * (3.0 * _Direction.Y*_Direction.Y - 1.0));
// 				SHCoeffs[7].W = (float) (-f2 * _Direction.Z * _Direction.Y);
// 				SHCoeffs[8].W = (float) (f2 * 0.5 * (_Direction.Z*_Direction.Z - _Direction.X*_Direction.X));
// 
// 				UpdateCoefficients( SHCoeffs );
// 			}
// 
// 			/// <summary>
// 			/// Makes the node an ambient node (i.e. no change in lighting)
// 			/// </summary>
// 			public void	MakeAmbient()
// 			{
// 				Vector4[]	SHCoeffs = new Vector4[9];
// 				SHCoeffs[0] = 3.5449077018110320545963349666822f * Vector4.One;
// 				for ( int SHCoeffIndex=1; SHCoeffIndex < 9; SHCoeffIndex++ )
// 					SHCoeffs[SHCoeffIndex] = Vector4.Zero;
// 
// 				UpdateCoefficients( SHCoeffs );
// 			}
// 
// 			#endregion
// 		}
// 
// 		/// <summary>
// 		/// CCW Triangles used in the Delaunay triangulation
// 		/// </summary>
// 		protected class	DelaunayTriangle
// 		{
// 			#region NESTED TYPES
// 
// 			public class DelaunayEdge
// 			{
// 				public int					Index = 0;					// Edge index from Vertex[Index] to Vertex[Index+1]
// 				public DelaunayTriangle		Owner = null;
// 				protected DelaunayTriangle	m_Adjacent = null;
// 				protected int				m_FlipCounter = -1;			// The last flip counter
// 
// 				public DelaunayTriangle	Adjacent
// 				{
// 					get { return m_Adjacent; }
// 					set
// 					{
// 						m_Adjacent = value;
// 						if ( m_Adjacent == null )
// 							return;
// 
// 						// Also fix adjacent triangle's adjacency
// 						m_Adjacent.Edges[AdjacentEdgeIndex].m_Adjacent = Owner;
// 					}
// 				}
// 
// 				public int				AdjacentEdgeIndex
// 				{
// 					get
// 					{
// 						if ( m_Adjacent == null )
// 							return -1;
// 
// 						int	V0 = Owner.Vertices[Index];
// 						int	V1 = Owner.Vertices[Index+1];
// 						if ( m_Adjacent.Vertices[0] == V1 && m_Adjacent.Vertices[1] == V0 )
// 							return 0;
// 						else if ( m_Adjacent.Vertices[1] == V1 && m_Adjacent.Vertices[2] == V0 )
// 							return 1;
// 						else if ( m_Adjacent.Vertices[2] == V1 && m_Adjacent.Vertices[0] == V0 )
// 							return 2;
// 
// 						throw new Exception( "Failed to retrieve our triangle in adjacent triangle's adjacencies !" );
// 					}
// 				}
// 
// 				public float			Length = 0.0f;
// 				public Vector2			Direction = Vector2.Zero;
// 				public Vector2			Normal = Vector2.Zero;		// Pointing OUTWARD of the triangle
// 
// 				public DelaunayEdge( DelaunayTriangle _Owner, int _Index, DelaunayTriangle _Adjacent )
// 				{
// 					Index = _Index;
// 					Owner = _Owner;
// 					Adjacent = _Adjacent;
// 				}
// 
// 				/// <summary>
// 				/// Update edge infos
// 				/// </summary>
// 				/// <param name="_Vertices"></param>
// 				public void		UpdateInfos()
// 				{
// 					Direction = Owner[Index+1] - Owner[Index];
// 					Length = Direction.Length();
// 					Direction /= Length;
// 					Normal.X = Direction.Y;
// 					Normal.Y = -Direction.X;
// 				}
// 
// 				public override string ToString()
// 				{
// 					return "O=" + Owner.m_ID + " V0=" + Owner.Vertices[Index] + " V1=" + Owner.Vertices[Index+1] + " Adj" + (Adjacent != null ? Adjacent.ToString() : "><") + " L=" + Length + " D=(" + Direction + ") N=(" + Normal + ")";
// 				}
// 
// 				/// <summary>
// 				/// Flips the edge
// 				/// </summary>
// 				public void	Flip( int _FlipCounter )
// 				{
// 					if ( m_FlipCounter == _FlipCounter )
// 						return;	// Already flipped !
// 					m_FlipCounter = _FlipCounter;	// So we can't flip this edge again
// 
// 					DelaunayTriangle	AdjacentTriangle = Adjacent;
// 					if ( AdjacentTriangle == null )
// 						return;	// Nothing to flip...
// 
// 					// Check if the opposite vertex fits the Delaunay condition (i.e. lies outside this triangle's circumbscribed circle)
// 					int	AdjacentIndex = AdjacentEdgeIndex;
// 					AdjacentTriangle.Edges[AdjacentIndex].m_FlipCounter = _FlipCounter;	// So we can't flip the equivalent adjacent edge either...
// 
// 					int	OppositeVertexIndex = AdjacentTriangle.Vertices[(AdjacentIndex+2)%3];
// 					if ( !Owner.IsInsideCircle( OppositeVertexIndex ) )
// 						return;	// The triangles satisfy Delaunay condition...
// 
// 					// Flip the edge
// 					int[]	QuadVertices = new int[4];
// 					QuadVertices[0] = Owner.Vertices[(Index+1)%3];
// 					QuadVertices[1] = Owner.Vertices[(Index+2)%3];
// 					QuadVertices[2] = Owner.Vertices[Index];
// 					QuadVertices[3] = OppositeVertexIndex;
// 
// 					DelaunayTriangle[]	QuadAdjacentTriangles = new DelaunayTriangle[4];
// 					QuadAdjacentTriangles[0] = Owner.Edges[(Index+1)%3].Adjacent;
// 					QuadAdjacentTriangles[1] = Owner.Edges[(Index+2)%3].Adjacent;
// 					QuadAdjacentTriangles[2] = AdjacentTriangle.Edges[(AdjacentIndex+1)%3].Adjacent;
// 					QuadAdjacentTriangles[3] = AdjacentTriangle.Edges[(AdjacentIndex+2)%3].Adjacent;
// 
// 					// Re-order our triangle so this edge is also the new flipped edge
// 					Owner.Vertices[Index] = QuadVertices[3];
// 					Owner.Vertices[(Index+1)%3] = QuadVertices[1];
// 					Owner.Vertices[(Index+2)%3] = QuadVertices[2];
// 					Owner.UpdateInfos();
// 
// 					// Re-order adjacent triangle so its edge is also the new flipped edge
// 					AdjacentTriangle.Vertices[AdjacentIndex] = QuadVertices[1];
// 					AdjacentTriangle.Vertices[(AdjacentIndex+1)%3] = QuadVertices[3];
// 					AdjacentTriangle.Vertices[(AdjacentIndex+2)%3] = QuadVertices[0];
// 					AdjacentTriangle.UpdateInfos();
// 
// 					// Update adjacencies
// 					Owner.Edges[(Index+1)%3].Adjacent = QuadAdjacentTriangles[1];
// 					Owner.Edges[(Index+2)%3].Adjacent = QuadAdjacentTriangles[2];
// 					AdjacentTriangle.Edges[(AdjacentIndex+1)%3].Adjacent = QuadAdjacentTriangles[3];
// 					AdjacentTriangle.Edges[(AdjacentIndex+2)%3].Adjacent = QuadAdjacentTriangles[0];
// 
// // CHECK => That flip should not flip the edge again since we now comply with Delaunay condition !
// //FlipEdge( _FlipCounter-1, Owner, (Index+1)%3 );
// 
// 					// Recursively split all 4 other edges
// 					DelaunayEdge	Temp = Owner.Edges[(Index+1)%3];
// 					Temp.Flip( _FlipCounter );
// 					Temp = Owner.Edges[(Index+2)%3];
// 					Temp.Flip( _FlipCounter );
// 
// 					Temp = AdjacentTriangle.Edges[(AdjacentIndex+1)%3];
// 					Temp.Flip( _FlipCounter );
// 					Temp = AdjacentTriangle.Edges[(AdjacentIndex+2)%3];
// 					Temp.Flip( _FlipCounter );
// 				}
// 			}
// 
// 			#endregion
// 
// 			#region FIELDS
// 
// 			protected int			m_ID = 0;
// 			protected Vector2[]		m_SourceVertices = null;
// 
// 			public int[]			Vertices = new int[4];
// 			public DelaunayEdge[]	Edges = new DelaunayEdge[3];
// 
// 			// Circumscribed circle
// 			public Vector2			CircleCenter;
// 			public float			CircleRadius;
// 
// 			public static int		ms_Index = 0;
// 
// 			#endregion
// 
// 			#region PROPERTIES
// 
// 			public Vector2		V0					{ get { return m_SourceVertices[Vertices[0]]; } }
// 			public Vector2		V1					{ get { return m_SourceVertices[Vertices[1]]; } }
// 			public Vector2		V2					{ get { return m_SourceVertices[Vertices[2]]; } }
// 			public Vector2		this[int _Index]	{ get { return m_SourceVertices[Vertices[_Index]]; } }
// 
// 			#endregion
// 
// 			#region METHODS
// 
// 			public DelaunayTriangle( Vector2[] _Vertices, int _V0, int _V1, int _V2, DelaunayTriangle _T0, DelaunayTriangle _T1, DelaunayTriangle _T2 )
// 			{
// 				m_ID = ms_Index++;
// 				m_SourceVertices = _Vertices;
// 
// 				Vertices[0] = _V0;
// 				Vertices[1] = _V1;
// 				Vertices[2] = _V2;
// 
// 				// Build edges
// 				Edges[0] = new DelaunayEdge( this, 0, _T0 );
// 				Edges[1] = new DelaunayEdge( this, 1, _T1 );
// 				Edges[2] = new DelaunayEdge( this, 2, _T2 );
// 
// 				// Build infos
// 				UpdateInfos();
// 			}
// 
// 			public override string ToString()
// 			{
// 				return "ID=" + m_ID;
// 			}
// 
// 			/// <summary>
// 			/// Builds the circumscribed circle of that triangle & recomputes edges length
// 			/// (from http://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations)
// 			/// </summary>
// 			public void		UpdateInfos()
// 			{
// 				Vertices[3] = Vertices[0];	// Redundant so edges don't need to bother with %3
// 
// 				// Update edge infos
// 				Edges[0].UpdateInfos();
// 				Edges[1].UpdateInfos();
// 				Edges[2].UpdateInfos();
// 
// 				// Rebuild circumbscribed circle
// 				Vector2	a = V0 - V2;
// 				Vector2	b = V1 - V2;
// 				Vector3	axb = Vector3.Cross( new Vector3( a, 0.0f ), new Vector3( b, 0.0f ) );
// 				float	a_Length = a.Length();
// 				float	b_Length = b.Length();
// 				float	axb_Length = axb.Length();
// 
// 				CircleRadius = 0.5f * a_Length * b_Length * (V0 - V1).Length() / axb_Length;
// 
// 				Vector2	a2b_b2a = a_Length*a_Length * b - b_Length*b_Length * a;
// 				Vector3	Num = Vector3.Cross( new Vector3( a2b_b2a, 0.0f ), axb );
// 				CircleCenter = V2 + 0.5f * new Vector2( Num.X, Num.Y ) / (axb_Length*axb_Length);
// 
// // Check
// // float	D0 = (this[0] - CircleCenter).Length();
// // float	D1 = (this[1] - CircleCenter).Length();
// // float	D2 = (this[2] - CircleCenter).Length();
// 			}
// 
// 			public bool	IsInsideCircle( int _VertexIndex )
// 			{
// 				// Handle the degenerate circle case when a vertex lies on an edge...
// 				if ( float.IsInfinity( CircleRadius) )
// 					return true;
// 
// 				Vector2	Center2Vertex = m_SourceVertices[_VertexIndex] - CircleCenter;
// 				return Center2Vertex.LengthSquared() < CircleRadius*CircleRadius;
// 			}
// 
// 			#endregion
// 		}

		#endregion

		#region FIELDS

		// Main renderer & techniques
		protected Renderer							m_Renderer = null;
		protected RenderTechniqueDepthPass			m_DepthPass = null;
		protected RenderTechniqueShadowMapping		m_ShadowMapping = null;
		protected DeferredRenderingScene			m_DeferredScene = null;
		protected DeferredRenderingGrass			m_DeferredGrass = null;
		protected DeferredRenderingTerrain			m_DeferredTerrain = null;
		protected EmissiveRenderingSky				m_EmissiveSky = null;
		protected RenderTechniqueInferredLighting	m_InferredLighting = null;

		// Post-processes
		protected RenderTechniquePostProcessFinalCompositing	m_PostProcessFinalCompositing = null;
		protected RenderTechniquePostProcessToneMapping			m_PostProcessToneMapping = null;
// 			// For env-map rendering
// 		protected RenderTechniquePostProcessEnvMap	m_PostProcessEnvMap = null;

		// Attributes
		protected Camera							m_Camera = null;
		protected RenderTechniqueInferredLighting.LightDirectional	m_SunLight = null;
		protected float								m_Time = 0.0f;

		// GBuffers at normal resolution
		protected RenderTarget<PF_RGBA16F>			m_GeometryBuffer = null;	// The geometry buffer that will store the normals, depth and surface roughness
		protected RenderTarget<PF_RGBA16F>			m_MaterialBuffer = null;	// The material buffer that will diffuse & specular albedos of materials
		protected RenderTarget<PF_RGBA16F>			m_EmissiveBuffer = null;	// The emissive buffer that will store the emissive/unlit object colors and the global extinction factor

// 		// The network of environmental nodes
// 		protected bool								m_bEnvMeshDirty = true;
// 		protected bool								m_bEnvMeshCoefficientsDirty = true;
// 		protected Frustum							m_EnvMapCameraFrustum = new Frustum();
// 		protected List<EnvironmentNode>				m_EnvNodes = new List<EnvironmentNode>();
// 		protected Primitive<VS_P3SH9,int>			m_EnvMesh = null;
// 		protected Material<VS_P3SH9>				m_SHEnvMapMaterial = null;
// 		protected bool								m_bEnableAmbientSH = true;
// 		protected bool								m_bEnableIndirectSH = true;
// 
// 			// The last rendered environment map and its offset & scale
// 		protected Vector2							m_SHEnvOffset;
// 		protected Vector2							m_SHEnvScale;
// 		protected Vector2							m_SHEnvMapSize = new Vector2( ENV_SH_MAP_SIZE, 1.0f / ENV_SH_MAP_SIZE );
// 		protected RenderTarget3D<PF_RGBA16F>		m_SHEnvMap = null;
// 
// 		// Alternate renderer for environment cube maps
// 		protected Renderer							m_EnvMapRenderer = null;

		#endregion

		#region PROPERTIES

		public Renderer						Renderer					{ get { return m_Renderer; } }
		public RenderTechniqueDepthPass		DepthPass					{ get { return m_DepthPass; } }
		public RenderTechniqueShadowMapping	ShadowMapping				{ get { return m_ShadowMapping; } }
		public DeferredRenderingScene		SceneTechnique				{ get { return m_DeferredScene; } }
		public DeferredRenderingGrass		GrassTechnique				{ get { return m_DeferredGrass; } }
		public DeferredRenderingTerrain		TerrainTechnique			{ get { return m_DeferredTerrain; } }
		public EmissiveRenderingSky			SkyTechnique				{ get { return m_EmissiveSky; } }
		public RenderTechniqueInferredLighting	LightingTechnique		{ get { return m_InferredLighting; } }

		public Camera						Camera						{ get { return m_Camera; } }
		public float						Time						{ get { return m_Time; } set { m_Time = value; } }
		public RenderTechniqueInferredLighting.LightDirectional	SunLight{ get { return m_SunLight; } }

// 		public bool							EnableAmbientSH				{ get { return m_bEnableAmbientSH; } set { m_bEnableAmbientSH = value; } }
// 		public bool							EnableIndirectSH			{ get { return m_bEnableIndirectSH; } set { m_bEnableIndirectSH = value; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Setups a default renderer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_bUseAlphaToCoverage">True to use alpha to coverage instead of alpha blending</param>
		/// <param name="_ShadowMapSize"></param>
		public	RendererSetupDeferred( Device _Device, string _Name, bool _bUseAlphaToCoverage, float _CameraFOV, float _CameraAspectRatio, float _CameraNear, float _CameraFar ) : base( _Device, _Name )
		{
			m_Renderer = ToDispose( new Renderer( m_Device, m_Name ) );

			//////////////////////////////////////////////////////////////////////////
			// Register shader interfaces
			m_Device.DeclareShaderInterface( typeof(IDeferredRendering) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IDeferredRendering), this );	// Register the IDeferredRendering interface

			//////////////////////////////////////////////////////////////////////////
			// Create rendering buffers
			int	DefaultWidth = m_Device.DefaultRenderTarget.Width;
			int	DefaultHeight = m_Device.DefaultRenderTarget.Height;
			int	LightBufferWith = (int) Math.Floor( DefaultWidth * LIGHT_BUFFER_SIZE_RATIO );
			int	LightBufferHeight = (int) Math.Floor( DefaultHeight * LIGHT_BUFFER_SIZE_RATIO );

			// Build screen resolution targets
			m_GeometryBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "GeometryBuffer", DefaultWidth, DefaultHeight, 1 ) );
			m_MaterialBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "MaterialBuffer", DefaultWidth, DefaultHeight, 1 ) );
			m_EmissiveBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "EmissiveBuffer", DefaultWidth, DefaultHeight, 1 ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the pipelines
			Pipeline	Depth = ToDispose( new Pipeline( m_Device, "Depth Pass Pipeline", Pipeline.TYPE.DEPTH_PASS ) );
			m_Renderer.AddPipeline( Depth );
			Depth.RenderingStart += new Pipeline.PipelineRenderingEventHandler( DepthPipeline_RenderingStart );

			Pipeline	Shadow = ToDispose( new Pipeline( m_Device, "Shadow Pipeline", Pipeline.TYPE.SHADOW_MAPPING ) );
			m_Renderer.AddPipeline( Shadow );

			Pipeline	Main = ToDispose( new Pipeline( m_Device, "Main Pipeline", Pipeline.TYPE.MAIN_RENDERING ) );
			m_Renderer.AddPipeline( Main );
			Main.RenderingStart += new Pipeline.PipelineRenderingEventHandler( MainPipeline_RenderingStart );

			Pipeline	DeferredLighting = ToDispose( new Pipeline( m_Device, "Deferred Lighting Pipeline", Pipeline.TYPE.DEFERRED_LIGHTING ) );
			m_Renderer.AddPipeline( DeferredLighting );

			Pipeline	Emissive = ToDispose( new Pipeline( m_Device, "Emissive Pipeline", Pipeline.TYPE.EMISSIVE_UNLIT ) );
			m_Renderer.AddPipeline( Emissive );
			Emissive.RenderingStart += new Pipeline.PipelineRenderingEventHandler( EmissivePipeline_RenderingStart );

			Pipeline	PostProcessing = ToDispose( new Pipeline( m_Device, "Post-Processing Pipeline", Pipeline.TYPE.POST_PROCESSING ) );
			m_Renderer.AddPipeline( PostProcessing );

			//////////////////////////////////////////////////////////////////////////
			// Create the depth pass render technique
			m_DepthPass = ToDispose( new RenderTechniqueDepthPass( m_Renderer, "Depth Pass" ) );
			Depth.AddTechnique( m_DepthPass );


			//////////////////////////////////////////////////////////////////////////
			// Create the shadow mapping technique
			m_ShadowMapping = ToDispose( new RenderTechniqueShadowMapping( m_Renderer, "Shadow Mapping" ) );
//			Shadow.AddTechnique( m_ShadowMapping );

			//////////////////////////////////////////////////////////////////////////
			// Create the render techniques for drawing a scene into the deferred render targets
			m_DeferredScene = ToDispose( new DeferredRenderingScene( m_Renderer, "Scene Rendering", true ) );
			Main.AddTechnique( m_DeferredScene );
			m_DepthPass.AddRenderable( m_DeferredScene );
			m_ShadowMapping.AddRenderable( m_DeferredScene );

			m_DeferredGrass = ToDispose( new DeferredRenderingGrass( m_Renderer, "Grass" ) );
//			Main.AddTechnique( m_DeferredGrass );
//			m_DepthPass.AddRenderable( m_DeferredGrass ); NOT DEPTH RENDERABLE !

			m_DeferredTerrain = ToDispose( new DeferredRenderingTerrain( m_Renderer, "Terrain" ) );
			Main.AddTechnique( m_DeferredTerrain );
			m_DepthPass.AddRenderable( m_DeferredTerrain );
			m_ShadowMapping.AddRenderable( m_DeferredTerrain );
			m_DeferredTerrain.DepthPassDepthStencil = m_Device.DefaultDepthStencil;

			m_EmissiveSky = ToDispose( new EmissiveRenderingSky( m_Renderer, "Sky" ) );
			Emissive.AddTechnique( m_EmissiveSky );
//			m_DepthPass.AddRenderable( m_EmissiveSky ); NOT DEPTH RENDERABLE !

			// TODO: Add others like skin, trees, etc.


			//////////////////////////////////////////////////////////////////////////
			// Create the inferred lighting technique
			m_InferredLighting = ToDispose( new RenderTechniqueInferredLighting( m_Renderer, "Inferred Lighting", LightBufferWith, LightBufferHeight ) );
 			DeferredLighting.AddTechnique( m_InferredLighting );
			m_InferredLighting.SourceGeometryBuffer = m_GeometryBuffer;
			m_InferredLighting.SourceDepthStencil = m_Device.DefaultDepthStencil;

			// The sky uses the downsampled geometry buffer
			m_EmissiveSky.LightGeometryBuffer = m_InferredLighting.GeometryBuffer;

			//////////////////////////////////////////////////////////////////////////
			// Create the post-process render techniques

			// Final image compositing that creates the final image to further tone map and post-process
			m_PostProcessFinalCompositing = ToDispose( new RenderTechniquePostProcessFinalCompositing( m_Renderer, "FinalCompositing" ) );
			m_PostProcessFinalCompositing.LightBuffer = m_InferredLighting.LightBuffer;
			m_PostProcessFinalCompositing.LightGeometryBuffer = m_InferredLighting.GeometryBuffer;
			m_PostProcessFinalCompositing.LightDepthStencil = m_InferredLighting.DepthStencil;
			PostProcessing.AddTechnique( m_PostProcessFinalCompositing );

			// Tone-mapping operator to map the HDR image into a LDR one
			m_PostProcessToneMapping = ToDispose( new RenderTechniquePostProcessToneMapping( m_Renderer, "ToneMapper" ) );
			m_PostProcessToneMapping.SourceImage = m_PostProcessFinalCompositing.CompositedImage;
			PostProcessing.AddTechnique( m_PostProcessToneMapping );


			//////////////////////////////////////////////////////////////////////////
			// Additional data

			// Create a perspective camera
			m_Camera = ToDispose( new Camera( m_Device, "Default Camera" ) );
			m_Camera.CreatePerspectiveCamera( _CameraFOV, _CameraAspectRatio, _CameraNear, _CameraFar );
			m_Camera.Activate();

// 			m_Camera.Camera2World = 
// 			new Matrix(
// 				-0.5583776f, 0f, 0.8295871f,0f,
// 				0.4235461f, 0.8598477f, 0.28508f, 0f,
// 				0.7133185f, -0.5105506f, 0.4801197f, 0f,
// 				-19.44753f, 38.8369f, -34.90257f, 1
// 				);


			m_ShadowMapping.Camera = m_Camera;
			m_DeferredTerrain.Camera = m_Camera;
			m_InferredLighting.Camera = m_Camera;
			m_PostProcessToneMapping.Camera = m_Camera;

// 			// Create a reduced camera frustum for environment map
// 			m_EnvMapCameraFrustum = Frustum.FromPerspective( 0.5f * (float) Math.PI, 1.0f, m_Camera.Near, 200.0f);//0.5f * m_Camera.Far );

			// Create the default directional lights
			m_SunLight = m_InferredLighting.CreateLightDirectional( "Sun", Vector3.UnitY, Vector3.Zero );
			m_ShadowMapping.Light = m_SunLight;


// So it lights in front of the opening of the room for our tests
m_EmissiveSky.SunPhi = 270.0f;


// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the cube map renderer
// 			m_EnvMapRenderer = ToDispose( new Renderer( m_Device, "CubeMap Renderer" ) );
// 
// 			// We re-use the depth pass and main pipelines that will give us material albedos and normals+depths
// 			m_EnvMapRenderer.AddPipeline( Depth );
// 			m_EnvMapRenderer.AddPipeline( Main );
// 
// 			// We add a post-processing pipeline...
// 			Pipeline	PostProcessingEnvironment = ToDispose( new Pipeline( m_Device, "Environment Post-Processing Pipeline", Pipeline.TYPE.POST_PROCESSING ) );
// 			m_EnvMapRenderer.AddPipeline( PostProcessingEnvironment );
// 
// 			// ...with a technique to transform normals into WORLD space for easier processing
// 			// and that also computes the SH's first bounce for indirect lighting
// 			m_PostProcessEnvMap = ToDispose( new RenderTechniquePostProcessEnvMap( m_EnvMapRenderer, "WorldSpaceConversion+IndirectLighting" ) );
// 			PostProcessingEnvironment.AddTechnique( m_PostProcessEnvMap );
// 
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the SH 3D envmap that will contain the 9 SH coefficients in its 7 successive layers
// 			// (Indeed, we need to store 3*9=27 coefficients that we can pack into 7 RGBA slots that will amount to a total of 4*7=28 coefficients)
// 			m_SHEnvMap = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "EnvSHMap", ENV_SH_MAP_SIZE, ENV_SH_MAP_SIZE, 7, 1 ) );
// 
// 			// And its rendering material
// 			m_SHEnvMapMaterial = ToDispose( new Material<VS_P3SH9>( m_Device, "Environment SH Map Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/EnvironmentSHMap.fx" ) ) );
		}

// 		public override void Dispose()
// 		{
// 			// Destroy any remaining environment data
// 			EndEnvironmentRendering();
// 
// 			// Destroy any environment mesh
// 			ClearEnvironmentMesh();
// 
// 			base.Dispose();
// 		}

		/// <summary>
		/// Renders the objects registered to our renderer
		/// </summary>
		public void	Render()
		{
			//////////////////////////////////////////////////////////////////////////
			// Clear stuff
			m_Device.AddProfileTask( this, "Prepare Rendering", "Clear Deferred Targets" );

			// Clear normals to 0 and depth to "infinity"
			m_Device.ClearRenderTarget( m_GeometryBuffer, new Color4( 0.0f, 0.0f, 0.0f, DEPTH_BUFFER_INFINITY ) );

			// Clear materials
			m_Device.ClearRenderTarget( m_MaterialBuffer, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );

			// Clear emissive to black and no extinction
			m_Device.ClearRenderTarget( m_EmissiveBuffer, new Color4( 1.0f, 0.0f, 0.0f, 0.0f ) );

			//////////////////////////////////////////////////////////////////////////
			// Propagate parameters
			m_DeferredGrass.Time = m_Time;
			m_SunLight.Direction = m_EmissiveSky.SunDirection;
			m_SunLight.Color = m_EmissiveSky.SunColor;

			//////////////////////////////////////////////////////////////////////////
			// Clear stuff
			m_Device.AddProfileTask( this, "Rendering", "<START>" );
			m_Renderer.Render();
			m_Device.AddProfileTask( this, "Rendering", "<END>" );
		}

// 		#region Environment Rendering
// 
// 		protected int						m_EnvironmentCubeMapSize = -1;
// 		protected Matrix					m_EnvironmentCamera2World = Matrix.Identity;
// 		protected int						m_EnvironmentCubeMapFaceIndex = -1;
// 
// 		// Texture arrays to render the environment
// 		protected RenderTarget<PF_RGBA32F>	m_EnvironmentArrayMaterial = null;
// 		protected RenderTarget<PF_RGBA32F>	m_EnvironmentArrayGeometry = null;
// 		// Cube maps filled by post-process
// 		protected RenderTarget<PF_RGBA32F>	m_EnvironmentCubeMapMaterial = null;
// 		protected RenderTarget<PF_RGBA32F>	m_EnvironmentCubeMapGeometry = null;
// 		protected RenderTarget<PF_RGBA32F>[]	m_EnvironmentCubeMapIndirectLighting = new RenderTarget<PF_RGBA32F>[7];
// 		protected DepthStencil<PF_D32>		m_EnvironmentDepth = null;
// 
// 		// Staging cube maps to read back results
// 		protected Texture2D					m_StagingEnvironmentCubeMapGeometry = null;
// 		protected Texture2D[]				m_StagingEnvironmentCubeMapIndirectLighting = new Texture2D[7];
// 
// 		/// <summary>
// 		/// Adds a new environment node
// 		/// </summary>
// 		/// <param name="_Position">The position of the node</param>
// 		/// <param name="_SHCoefficients">The 9 SH coefficients encoding the environment's occlusion and reflection
// 		/// The RGB SH coefficients should encode indirect light reflection and A coefficients should encode direct lighting occlusion
// 		/// </param>
// 		public EnvironmentNode	AddEnvironmentNode( Vector3 _Position, Vector4[] _SHCoefficients )
// 		{
// 			EnvironmentNode	Node = new EnvironmentNode( this, _Position, _SHCoefficients );
// 			m_EnvNodes.Add( Node );
// 
// 			return Node;
// 		}
// 		/// <summary>
// 		/// Adds a new environment node
// 		/// </summary>
// 		/// <param name="_Position">The position of the node</param>
// 		public EnvironmentNode	AddEnvironmentNode( Vector3 _Position )
// 		{
// 			m_bEnvMeshDirty = true;
// 			return AddEnvironmentNode( _Position, null );
// 		}
// 
// 		/// <summary>
// 		/// Clears existing environment nodes
// 		/// </summary>
// 		public void		ClearEnvironmentNodes()
// 		{
// 			m_EnvNodes.Clear();
// 			m_bEnvMeshDirty = true;
// 		}
// 
// 		/// <summary>
// 		/// Loads the environment nodes from a file
// 		/// </summary>
// 		/// <param name="_EnvNodesFile"></param>
// 		public void		LoadEnvironmentNodes( System.IO.FileInfo _EnvNodesFile )
// 		{
// 			System.IO.FileStream	S = _EnvNodesFile.OpenRead();
// 			System.IO.BinaryReader	R = new System.IO.BinaryReader( S );
// 
// 			int	NodesCount = R.ReadInt32();
// 			for ( int NodeIndex=0; NodeIndex < NodesCount; NodeIndex++ )
// 				m_EnvNodes.Add( new EnvironmentNode( this, R ) );
// 
// 			R.Dispose();
// 			S.Dispose();
// 
// 			m_bEnvMeshDirty = true;
// 		}
// 
// 		/// <summary>
// 		/// Saves the environment nodes to a file
// 		/// </summary>
// 		/// <param name="_EnvNodesFile"></param>
// 		public void		SaveEnvironmentNodes( System.IO.FileInfo _EnvNodesFile )
// 		{
// 			System.IO.FileStream	S = _EnvNodesFile.Create();
// 			System.IO.BinaryWriter	W = new System.IO.BinaryWriter( S );
// 
// 			W.Write( m_EnvNodes.Count );
// 			foreach ( EnvironmentNode N in m_EnvNodes )
// 				N.Write( W );
// 
// 			W.Dispose();
// 			S.Dispose();
// 		}
// 
// 		/// <summary>
// 		/// Renders the environment mesh into the environment map fit for the given camera
// 		/// </summary>
// 		/// <param name="_Camera">The camera viewing the environment</param>
// 		/// <param name="_EnvLightSH">The complex SH light for the environment
// 		/// NOTE: RGB encodes colored environment light (i.e. without direct light) and A encodes monochromatic (i.e. white) direct light (i.e. without environment)
// 		/// </param>
// 		public void RenderEnvironmentMap( Camera _Camera, Vector4[] _EnvLightSH )
// 		{
// 			// Make sure the environment mesh is built
// 			BuildEnvironmentMesh();
// 
// 			// 1] Build the clip offset & scale to map the camera SH environment
// 			// 1.1] First, we project the camera frustum in the XZ 2D plane
// 			Matrix	Camera2World = _Camera.Camera2World;
// 
// 			Vector2	WorldMin = new Vector2( +float.MaxValue, +float.MaxValue );
// 			Vector2	WorldMax = new Vector2( -float.MaxValue, -float.MaxValue );
// 			foreach ( Vector3 V in m_EnvMapCameraFrustum.Vertices )
// 			{
// 				Vector3	VWorld = Vector3.TransformCoordinate( V, Camera2World );
// 				WorldMin.X = Math.Min( WorldMin.X, VWorld.X );
// 				WorldMax.X = Math.Max( WorldMax.X, VWorld.X );
// 				WorldMin.Y = Math.Min( WorldMin.Y, VWorld.Z );
// 				WorldMax.Y = Math.Max( WorldMax.Y, VWorld.Z );
// 			}
// 
// 			// 1.2] Then we build the clip offset & scale to map the camera SH environment
// 			m_SHEnvOffset = WorldMin;
// 			m_SHEnvScale.X = 1.0f / (WorldMax.X - WorldMin.X);
// 			m_SHEnvScale.Y = 1.0f / (WorldMax.Y - WorldMin.Y);
// 
// 			// 2] Render the SH Environment mesh
// 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
// 			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
// 			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
// 
// 			using ( m_SHEnvMapMaterial.UseLock() )
// 			{
// 				m_Device.SetRenderTarget( m_SHEnvMap );
// 				m_Device.SetViewport( 0, 0, ENV_SH_MAP_SIZE, ENV_SH_MAP_SIZE, 0.0f, 1.0f );
// //				m_Device.ClearRenderTarget( m_SHEnvMap, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
// 
// 				m_SHEnvMapMaterial.GetVariableByName( "SHLight" ).AsVector.Set( _EnvLightSH );
// 				m_SHEnvMapMaterial.GetVariableByName( "EnableAmbientSH" ).AsScalar.Set( m_bEnableAmbientSH );
// 				m_SHEnvMapMaterial.GetVariableByName( "EnableIndirectSH" ).AsScalar.Set( m_bEnableIndirectSH );
// 
// 				m_SHEnvMapMaterial.CurrentTechnique.GetPassByIndex(0).Apply();
// 				m_EnvMesh.RenderOverride();
// 			}
// 		}
// 
// 		/// <summary>
// 		/// Builds the cube map data for environment rendering
// 		/// </summary>
// 		/// <param name="_CubeSize">The size of the cube map used to sample the environment</param>
// 		/// <param name="_IndirectLightingBoostFactor">The boost factor to apply to indirect lighting (default is 1)</param>
// 		public void	BeginEnvironmentRendering( int _CubeSize, float _IndirectLightingBoostFactor )
// 		{
// 			EndEnvironmentRendering();
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the cube map render targets
// 
// 			m_EnvironmentCubeMapSize = _CubeSize;
// 
// 			// 1] We first create texture arrays that we will be able to lock side by side for the post process
// 
// 			// The material array that will contain the diffuse and specular surface albedos
// 			m_EnvironmentArrayMaterial = new RenderTarget<PF_RGBA32F>( m_Device, "ArrayMaterial", _CubeSize, _CubeSize, 1, 6, 1 );
// 			// The geometry array that will contain the normals, depth and surface roughness
// 			m_EnvironmentArrayGeometry = new RenderTarget<PF_RGBA32F>( m_Device, "ArrayGeometry", _CubeSize, _CubeSize, 1, 6, 1 );
// 
// 			// 2] Then we create the actual cube maps whose each face will be filled up by the post process
// 
// 			// The material cube map that will contain the diffuse and specular surface albedos
// 			m_EnvironmentCubeMapMaterial = new RenderTarget<PF_RGBA32F>( m_Device, "CubeMapMaterial", _CubeSize, _CubeSize, 1, RenderTarget<PF_RGBA32F>.CUBE_MAP, 1 );
// 
// 			// The geometry cube map that will contain the normals in WORLD space and depth
// 			m_EnvironmentCubeMapGeometry = new RenderTarget<PF_RGBA32F>( m_Device, "CubeMapGeometry", _CubeSize, _CubeSize, 1, RenderTarget<PF_RGBA32F>.CUBE_MAP, 1 );
// 
// 			// 3] Create the 7 cube maps that will store the 9 RGB coefficients for indirect lighting
// 			// (3*9 = 27 coefficients that are packed into 4*7 = 28 RGBA slots of 7 cube maps)
// 			for ( int i=0; i < 7; i++ )
// 				m_EnvironmentCubeMapIndirectLighting[i] = new RenderTarget<PF_RGBA32F>( m_Device, "CubeMapIndirectLighting" + i, _CubeSize, _CubeSize, 1, RenderTarget<PF_RGBA32F>.CUBE_MAP, 1 );
// 
// 			// 4] Finally, the depth stencil for rendering the cube map
// 			m_EnvironmentDepth = new DepthStencil<PF_D32>( m_Device, "CubeMapDepth", _CubeSize, _CubeSize, false );
// 
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Initialize the post-process
// 			m_PostProcessEnvMap.IndirectLightingBoostFactor = _IndirectLightingBoostFactor;
// 			m_PostProcessEnvMap.ArrayMaterialSource = m_EnvironmentArrayMaterial;
// 			m_PostProcessEnvMap.ArrayGeometrySource = m_EnvironmentArrayGeometry;
// 			m_PostProcessEnvMap.CubeMapMaterialTarget = m_EnvironmentCubeMapMaterial;
// 			m_PostProcessEnvMap.CubeMapGeometryTarget = m_EnvironmentCubeMapGeometry;
// 			for ( int i=0; i < 7; i++ )
// 				m_PostProcessEnvMap.SetCubeMapIndirectLighting( i, m_EnvironmentCubeMapIndirectLighting[i] );
// 
// 			m_PostProcessEnvMap.InitQuad( _CubeSize );
// 
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Render with normal renderer so the scenes get updated at least once...
// 			// Indeed, scenes are created with a renderer (i.e. our main deferred renderer) and their
// 			//	nodes get updated when the renderer's frame token changes... If we decide to render
// 			//	an environment before the actual pipeline has rendered, scenes won't have their nodes
// 			//	reflect their world matrices correctly...
// 			Render();
// 
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the staging resources where we will copy the cube map faces for encoding
// 			Texture2DDescription	Desc = new Texture2DDescription();
// 			Desc.BindFlags = BindFlags.None;
// 			Desc.CpuAccessFlags = CpuAccessFlags.Read;
// 			Desc.OptionFlags = ResourceOptionFlags.None;
// 			Desc.Usage = ResourceUsage.Staging;
// 			Desc.Format = SharpDX.DXGI.Format.R32G32B32A32_Float;
// 			Desc.ArraySize = 6;
// 			Desc.Width = m_EnvironmentCubeMapSize;
// 			Desc.Height = m_EnvironmentCubeMapSize;
// 			Desc.MipLevels = 1;
// 			Desc.SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 );
// 			m_StagingEnvironmentCubeMapGeometry = new Texture2D( m_Device.DirectXDevice, Desc );
// 			for ( int i=0; i < 7; i++ )
// 				m_StagingEnvironmentCubeMapIndirectLighting[i] = new Texture2D( m_Device.DirectXDevice, Desc );
// 
// // DEBUG
// m_EmissiveSky.TestCubeMap0 = m_EnvironmentCubeMapMaterial;
// m_EmissiveSky.TestCubeMap1 = m_EnvironmentCubeMapGeometry;
// 		}
// 
// 		/// <summary>
// 		/// Renders the scene from the specified position into a geometry and material cube map + 7 indirect lighting cube maps
// 		/// </summary>
// 		/// <param name="_Position"></param>
// 		/// <param name="_At"></param>
// 		/// <param name="_Up"></param>
// 		/// <param name="_NearClip"></param>
// 		/// <param name="_FarClip"></param>
// 		/// <param name="_LightBounceIndex">The index of light bounce (0 is direct, 1 is first indirect bounce, 2 is second bounce and so on...)</param>
// 		public void	RenderCubeMap( Vector3 _Position, Vector3 _At, Vector3 _Up, float _NearClip, float _FarClip, int _LightBounceIndex )
// 		{
// 			if ( m_EnvironmentArrayGeometry == null )
// 				throw new NException( this, "You must first call \"BeginEnvironmentRendering()\" prior using that method !" );
// 
// 			m_PostProcessEnvMap.LightBounceIndex = _LightBounceIndex;
// 
// 			// Clear normals to 0 and depth to "infinity"
// 			m_Device.ClearRenderTarget( m_EnvironmentArrayGeometry, new Color4( 0.0f, 0.0f, 0.0f, DEPTH_BUFFER_INFINITY ) );
// 
// 			// Clear materials
// 			m_Device.ClearRenderTarget( m_EnvironmentArrayMaterial, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
// 
// 			// Create the main camera matrix
// 			m_EnvironmentCamera2World = Camera.CreateLookAt( _Position, _Position + _At, _Up );
// 
// 			// Create the side transforms
// 			Matrix[]	SideTransforms = new Matrix[6]
// 			{
// 				Matrix.RotationY( +0.5f * (float) Math.PI ),	// +X (look right)
// 				Matrix.RotationY( -0.5f * (float) Math.PI ),	// -X (look left)
// 				Matrix.RotationX( -0.5f * (float) Math.PI ),	// +Y (look up)
// 				Matrix.RotationX( +0.5f * (float) Math.PI ),	// -Y (look down)
// 				Matrix.RotationY( +0.0f * (float) Math.PI ),	// +Z (look front) (default)
// 				Matrix.RotationY( +1.0f * (float) Math.PI ),	// -Z (look back)
// 			};
// 
// 			// Create a camera that will feed our transforms
// 			Camera	CubeCamera = new Camera( m_Device, "CubeCamera" );
// 			CubeCamera.CreatePerspectiveCamera( 0.5f * (float) Math.PI, 1.0f, _NearClip, _FarClip );
// 			CubeCamera.Activate();
// 
// 			// Create a uniform ambient SH light so we keep nodes' occlusion by default
// 			// The long factor 3.54490(...) is the inverse of the first DC SH factor K00 = 1/2 * sqrt(1/PI)
// 			Vector4[]	EnvLightSH = new Vector4[9];
// 			if ( _LightBounceIndex == 1 )
// 				EnvLightSH[0] = new Vector4( 3.5449077018110320545963349666822f * Vector3.One, 0.0f );
// 			else
// 				EnvLightSH[0] = new Vector4( Vector3.Zero, 3.5449077018110320545963349666822f );
// 			for ( int SHCoeffIndex=1; SHCoeffIndex < 9; SHCoeffIndex++ )
// 				EnvLightSH[SHCoeffIndex] = Vector4.Zero;
// 
// 			// Start rendering
// 			m_Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).RenderingStart += new Pipeline.PipelineRenderingEventHandler( CubeMapRender_RenderingStart );
// 
// 			IDepthStencil	OldDepthStencil = m_DepthPass.DepthStencil;
// 			m_DepthPass.DepthStencil = m_EnvironmentDepth;
// 
// 			bool			bOldEnableAmbientSH = m_bEnableAmbientSH;
// 			bool			bOldEnableIndirectSH = m_bEnableIndirectSH;
// 			m_bEnableAmbientSH = true;
// 			m_bEnableIndirectSH = true;
// 
// 			for ( m_EnvironmentCubeMapFaceIndex=0; m_EnvironmentCubeMapFaceIndex < 6; m_EnvironmentCubeMapFaceIndex++ )
// 			{
// 				Matrix	Side2World = SideTransforms[m_EnvironmentCubeMapFaceIndex] * m_EnvironmentCamera2World;
// 				CubeCamera.Camera2World = Side2World;
// 
// 				// Render the environment map
// 				m_Device.ClearRenderTarget( m_SHEnvMap, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );
// 				RenderEnvironmentMap( CubeCamera, EnvLightSH );
// 
// 				// Update the post-process cube face index
// 				m_PostProcessEnvMap.CubeMapFaceIndex = m_EnvironmentCubeMapFaceIndex;
// 
// 				// Render the cube face
// 				m_EnvMapRenderer.Render();
// 			}
// 
// 			m_DepthPass.DepthStencil = OldDepthStencil;
// 			m_bEnableAmbientSH = bOldEnableAmbientSH;
// 			m_bEnableIndirectSH = bOldEnableIndirectSH;
// 
// 			m_Renderer.FindPipeline( Pipeline.TYPE.MAIN_RENDERING ).RenderingStart -= new Pipeline.PipelineRenderingEventHandler( CubeMapRender_RenderingStart );
// 
// 			// Destroy the camera
// 			CubeCamera.DeActivate();
// 			CubeCamera.Dispose();
// 		}
// 
// 		/// <summary>
// 		/// Encodes the last rendered cube map environment into 9 SH vectors
// 		/// </summary>
// 		/// <returns>The 9 SH coefficients encoding the environment's occlusion for direct lighting
// 		/// NOTE: Only the W component is relevant, XYZ are filled up by the EncodeSHEnvironmentIndirect() method</returns>
// 		public Vector4[]	EncodeSHEnvironmentDirect()
// 		{
// 			// Copy cube maps for CPU read
// 			m_Device.DirectXDevice.CopyResource( m_EnvironmentCubeMapGeometry.Texture, m_StagingEnvironmentCubeMapGeometry );
// 
// 			// Create the side transforms
// 			Matrix[]	SideTransforms = new Matrix[6]
// 			{
// 				Matrix.RotationY( +0.5f * (float) Math.PI ),	// +X (look right)
// 				Matrix.RotationY( -0.5f * (float) Math.PI ),	// -X (look left)
// 				Matrix.RotationX( -0.5f * (float) Math.PI ),	// +Y (look up)
// 				Matrix.RotationX( +0.5f * (float) Math.PI ),	// -Y (look down)
// 				Matrix.RotationY( +0.0f * (float) Math.PI ),	// +Z (look front) (default)
// 				Matrix.RotationY( +1.0f * (float) Math.PI ),	// -Z (look back)
// 			};
// 
// 			// Differential area of a cube map texel
// 			double	dA = 4.0 / (m_EnvironmentCubeMapSize*m_EnvironmentCubeMapSize);
// 			double	SumSolidAngle = 0.0;
// 
// 			double	f0 = 0.5 / Math.Sqrt(Math.PI);
// 			double	f1 = Math.Sqrt(3.0) * f0;
// 			double	f2 = Math.Sqrt(15.0) * f0;
// 			double	f3 = f2 / (2.0 * Math.Sqrt(3.0));
// 
// 			// Read back cube faces
// 			double[]	SHCoefficients = new double[9];
// 			double[]	CurrentSHCoefficients = new double[9];
// 			int[]		OccludedRaysCount = new int[6];
// 			int[]		FreeRaysCount = new int[6];
// 			int[]		SumRaysCount = new int[6];
// 			for ( int CubeFaceIndex=0; CubeFaceIndex < 6; CubeFaceIndex++ )
// 			{
// 				Matrix			Side2World = SideTransforms[CubeFaceIndex] * m_EnvironmentCamera2World;
// 
// 				DataRectangle	RectGeometry = m_StagingEnvironmentCubeMapGeometry.Map( CubeFaceIndex, MapMode.Read, MapFlags.None );
// 
// 				Vector3			ViewLocal, ViewWorld;
// 				ViewLocal.Z = 1.0f;
// 				for ( int Y=0; Y < m_EnvironmentCubeMapSize; Y++ )
// 				{
// 					ViewLocal.Y = 1.0f - 2.0f * Y / m_EnvironmentCubeMapSize;
// 					for ( int X=0; X < m_EnvironmentCubeMapSize; X++ )
// 					{
// 						ViewLocal.X = 2.0f * X / m_EnvironmentCubeMapSize - 1.0f;
// 
// 						Vector4	Geometry = RectGeometry.Data.Read<Vector4>();
// 
// 						// Here, we're only interested in knowing if the ray hit something (occlusion) or fled to infinity (no occlusion)
// 						float	HitDistance = Geometry.W;
// 						if ( HitDistance < 0.5f * DEPTH_BUFFER_INFINITY )
// 						{
// 							OccludedRaysCount[CubeFaceIndex]++;
// 							continue;	// We hit something on the way... Don't add any contribution
// 						}
// 
// 						FreeRaysCount[CubeFaceIndex]++;
// 
// 						ViewWorld = Vector3.TransformNormal( ViewLocal, Side2World );
// 
// 						float	SqDistance = ViewWorld.LengthSquared();
// 						float	Distance = (float) Math.Sqrt( SqDistance );
// 						ViewWorld /= Distance;
// 
// 						// Solid angle (from http://people.cs.kuleuven.be/~philip.dutre/GI/TotalCompendium.pdf)
// 						// dw = cos(Theta).dA / r²
// 						// cos(Theta) = Adjacent/Hypothenuse = 1/r
// 						//
// 						double	SolidAngle = dA / (Distance * SqDistance);
// 						SumSolidAngle += SolidAngle;
// 
//  						// Accumulate SH in that direction
// 						SHCoefficients[0] += SolidAngle * f0;
// 						SHCoefficients[1] += SolidAngle * -f1 * ViewWorld.X;
// 						SHCoefficients[2] += SolidAngle * f1 * ViewWorld.Y;
// 						SHCoefficients[3] += SolidAngle * -f1 * ViewWorld.Z;
// 						SHCoefficients[4] += SolidAngle * f2 * ViewWorld.X * ViewWorld.Z;
// 						SHCoefficients[5] += SolidAngle * -f2 * ViewWorld.X * ViewWorld.Y;
// 						SHCoefficients[6] += SolidAngle * f3 * (3.0 * ViewWorld.Y*ViewWorld.Y - 1.0);
// 						SHCoefficients[7] += SolidAngle * -f2 * ViewWorld.Z * ViewWorld.Y;
// 						SHCoefficients[8] += SolidAngle * f2 * 0.5 * (ViewWorld.Z*ViewWorld.Z - ViewWorld.X*ViewWorld.X);
// 					}
// 				}
// 
// 				m_StagingEnvironmentCubeMapGeometry.Unmap( CubeFaceIndex );
// 
// 				SumRaysCount[CubeFaceIndex] = FreeRaysCount[CubeFaceIndex] + OccludedRaysCount[CubeFaceIndex];
// 			}
// 
// 			// Build the final vector that will only contain direct light occlusion
// 			Vector4[]	Result = new Vector4[9];
// 			for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
// 			{
// 				Result[SHCoeffIndex] = Vector4.Zero;
// 				Result[SHCoeffIndex].W = (float) SHCoefficients[SHCoeffIndex];
// 			}
// 
// 			return Result;
// 		}
// 
// 		/// <summary>
// 		/// Encodes the indirect lighting collected by the last rendered cube map environment into 9 SH vectors
// 		/// </summary>
// 		/// <returns>The 9 SH coefficients encoding the environment's indirect lighting
// 		/// NOTE: Only the XYZ components are relevant, W should have been filled up by the EncodeSHEnvironmentDirect() method</returns>
// 		public Vector4[]	EncodeSHEnvironmentIndirect()
// 		{
// 			// Copy cube maps for CPU read
// 			m_Device.DirectXDevice.CopyResource( m_EnvironmentCubeMapGeometry.Texture, m_StagingEnvironmentCubeMapGeometry );
// 			for ( int i=0; i < 7; i++ )
// 				m_Device.DirectXDevice.CopyResource( m_EnvironmentCubeMapIndirectLighting[i].Texture, m_StagingEnvironmentCubeMapIndirectLighting[i] );
// 
// 			// Create the side transforms
// 			Matrix[]	SideTransforms = new Matrix[6]
// 			{
// 				Matrix.RotationY( +0.5f * (float) Math.PI ),	// +X (look right)
// 				Matrix.RotationY( -0.5f * (float) Math.PI ),	// -X (look left)
// 				Matrix.RotationX( -0.5f * (float) Math.PI ),	// +Y (look up)
// 				Matrix.RotationX( +0.5f * (float) Math.PI ),	// -Y (look down)
// 				Matrix.RotationY( +0.0f * (float) Math.PI ),	// +Z (look front) (default)
// 				Matrix.RotationY( +1.0f * (float) Math.PI ),	// -Z (look back)
// 			};
// 
// 			// Differential area of a cube map texel
// 			double	dA = 4.0 / (m_EnvironmentCubeMapSize*m_EnvironmentCubeMapSize);
// 			double	SumSolidAngle = 0.0;
// 
// 			// Read back cube faces
// 			double[,]	SHCoefficients = new double[9,3];
// 			for ( int CubeFaceIndex=0; CubeFaceIndex < 6; CubeFaceIndex++ )
// 			{
// 				Matrix			Side2World = SideTransforms[CubeFaceIndex] * m_EnvironmentCamera2World;
// 
// 				DataRectangle	RectGeometry = m_StagingEnvironmentCubeMapGeometry.Map( CubeFaceIndex, MapMode.Read, MapFlags.None );
// 
// 				DataRectangle[]	RectIndirectLighting = new DataRectangle[7];
// 				for ( int i=0; i < 7; i++ )
// 					RectIndirectLighting[i] = m_StagingEnvironmentCubeMapIndirectLighting[i].Map( CubeFaceIndex, MapMode.Read, MapFlags.None );
// 
// 				Vector3			ViewLocal, ViewWorld;
// 				ViewLocal.Z = 1.0f;
// 				for ( int Y=0; Y < m_EnvironmentCubeMapSize; Y++ )
// 				{
// 					ViewLocal.Y = 1.0f - 2.0f * Y / m_EnvironmentCubeMapSize;
// 					for ( int X=0; X < m_EnvironmentCubeMapSize; X++ )
// 					{
// 						ViewLocal.X = 2.0f * X / m_EnvironmentCubeMapSize - 1.0f;
// 
// 						Vector4	Geometry = RectGeometry.Data.Read<Vector4>();
// 
// 						Vector4	Coeffs0 = RectIndirectLighting[0].Data.Read<Vector4>();
// 						Vector4	Coeffs1 = RectIndirectLighting[1].Data.Read<Vector4>();
// 						Vector4	Coeffs2 = RectIndirectLighting[2].Data.Read<Vector4>();
// 						Vector4	Coeffs3 = RectIndirectLighting[3].Data.Read<Vector4>();
// 						Vector4	Coeffs4 = RectIndirectLighting[4].Data.Read<Vector4>();
// 						Vector4	Coeffs5 = RectIndirectLighting[5].Data.Read<Vector4>();
// 						Vector4	Coeffs6 = RectIndirectLighting[6].Data.Read<Vector4>();
// 
// 						// Here, we're only interested in knowing if the ray hit something (reflection) or fled to infinity (direct lighting, which was already computed)
// 						float	HitDistance = Geometry.W;
// 						if ( HitDistance > 0.5f * DEPTH_BUFFER_INFINITY )
// 							continue;	// We didn't hit anything on the way... Don't add any contribution
// 
// // 						if ( HitDistance < 4.01f )
// // 							HitDistance += 2.0f;
// 
// 						ViewWorld = Vector3.TransformNormal( ViewLocal, Side2World );
// 
// 						float	SqDistance = ViewWorld.LengthSquared();
// 						float	Distance = (float) Math.Sqrt( SqDistance );
// 						ViewWorld /= Distance;
// 
// 						// Solid angle (from http://people.cs.kuleuven.be/~philip.dutre/GI/TotalCompendium.pdf)
// 						// dw = cos(Theta).dA / r²
// 						// cos(Theta) = Adjacent/Hypothenuse = 1/r
// 						//
// 						double	SolidAngle = dA / (Distance * SqDistance);
// 						SumSolidAngle += SolidAngle;
// 
// 						// Accumulate SH in that direction
// 						SHCoefficients[0,0] += SolidAngle * Coeffs0.X;
// 						SHCoefficients[0,1] += SolidAngle * Coeffs0.Y;
// 						SHCoefficients[0,2] += SolidAngle * Coeffs0.Z;
// 						SHCoefficients[1,0] += SolidAngle * Coeffs0.W;
// 						SHCoefficients[1,1] += SolidAngle * Coeffs1.X;
// 						SHCoefficients[1,2] += SolidAngle * Coeffs1.Y;
// 						SHCoefficients[2,0] += SolidAngle * Coeffs1.Z;
// 						SHCoefficients[2,1] += SolidAngle * Coeffs1.W;
// 						SHCoefficients[2,2] += SolidAngle * Coeffs2.X;
// 						SHCoefficients[3,0] += SolidAngle * Coeffs2.Y;
// 						SHCoefficients[3,1] += SolidAngle * Coeffs2.Z;
// 						SHCoefficients[3,2] += SolidAngle * Coeffs2.W;
// 						SHCoefficients[4,0] += SolidAngle * Coeffs3.X;
// 						SHCoefficients[4,1] += SolidAngle * Coeffs3.Y;
// 						SHCoefficients[4,2] += SolidAngle * Coeffs3.Z;
// 						SHCoefficients[5,0] += SolidAngle * Coeffs3.W;
// 						SHCoefficients[5,1] += SolidAngle * Coeffs4.X;
// 						SHCoefficients[5,2] += SolidAngle * Coeffs4.Y;
// 						SHCoefficients[6,0] += SolidAngle * Coeffs4.Z;
// 						SHCoefficients[6,1] += SolidAngle * Coeffs4.W;
// 						SHCoefficients[6,2] += SolidAngle * Coeffs5.X;
// 						SHCoefficients[7,0] += SolidAngle * Coeffs5.Y;
// 						SHCoefficients[7,1] += SolidAngle * Coeffs5.Z;
// 						SHCoefficients[7,2] += SolidAngle * Coeffs5.W;
// 						SHCoefficients[8,0] += SolidAngle * Coeffs6.X;
// 						SHCoefficients[8,1] += SolidAngle * Coeffs6.Y;
// 						SHCoefficients[8,2] += SolidAngle * Coeffs6.Z;
// 					}
// 				}
// 
// 				for ( int i=0; i < 7; i++ )
// 					m_StagingEnvironmentCubeMapIndirectLighting[i].Unmap( CubeFaceIndex );
// 
// 				m_StagingEnvironmentCubeMapGeometry.Unmap( CubeFaceIndex );
// 			}
// 
// 			// Build the final vector
// 			Vector4[]	Result = new Vector4[9];
// 			for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
// 			{
// 				Result[SHCoeffIndex].X = (float) SHCoefficients[SHCoeffIndex,0];
// 				Result[SHCoeffIndex].Y = (float) SHCoefficients[SHCoeffIndex,1];
// 				Result[SHCoeffIndex].Z = (float) SHCoefficients[SHCoeffIndex,2];
// 				Result[SHCoeffIndex].W = 0.0f;
// 			}
// 
// 			return Result;
// 		}
// 
// 		/// <summary>
// 		/// Destroys cube maps created for environment rendering
// 		/// </summary>
// 		public void		EndEnvironmentRendering()
// 		{
// 			if ( m_EnvironmentArrayMaterial != null )
// 				m_EnvironmentArrayMaterial.Dispose();
// 			m_EnvironmentArrayMaterial = null;
// 
// 			if ( m_EnvironmentArrayGeometry != null )
// 				m_EnvironmentArrayGeometry.Dispose();
// 			m_EnvironmentArrayGeometry = null;
// 
// 			if ( m_EnvironmentCubeMapMaterial != null )
// 				m_EnvironmentCubeMapMaterial.Dispose();
// 			m_EnvironmentCubeMapMaterial = null;
// 
// 			if ( m_EnvironmentCubeMapGeometry != null )
// 				m_EnvironmentCubeMapGeometry.Dispose();
// 			m_EnvironmentCubeMapGeometry = null;
// 
// 			if ( m_EnvironmentDepth != null )
// 				m_EnvironmentDepth.Dispose();
// 			m_EnvironmentDepth = null;
// 
// 			if ( m_StagingEnvironmentCubeMapGeometry != null )
// 				m_StagingEnvironmentCubeMapGeometry.Dispose();
// 			m_StagingEnvironmentCubeMapGeometry= null;
// 
// 			for ( int i=0; i < 7; i++ )
// 			{
// 				if ( m_EnvironmentCubeMapIndirectLighting[i] != null )
// 					m_EnvironmentCubeMapIndirectLighting[i].Dispose();
// 				m_EnvironmentCubeMapIndirectLighting[i] = null;
// 
// 				if ( m_StagingEnvironmentCubeMapIndirectLighting[i] != null )
// 					m_StagingEnvironmentCubeMapIndirectLighting[i].Dispose();
// 				m_StagingEnvironmentCubeMapIndirectLighting[i] = null;
// 			}
// 		}
// 
// 		#region Environment Mesh Construction
// 
// 		/// <summary>
// 		/// Clears any existing environment mesh
// 		/// </summary>
// 		protected void	ClearEnvironmentMesh()
// 		{
// 			if ( m_EnvMesh != null )
// 				m_EnvMesh.Dispose();
// 			m_EnvMesh = null;
// 			m_bEnvMeshDirty = true;
// 		}
// 
// 		protected int[]	m_LastEnvMeshPrimitiveIndices = null;
// 
// 		/// <summary>
// 		/// Triangulates the environment nodes to build an environment mesh of environmental SH vertices
// 		///  that will laater be rendered into a world space environment map
// 		/// </summary>
// 		public void	BuildEnvironmentMesh()
// 		{
// 			if ( m_EnvNodes.Count < 3 )
// 				throw new NException( this, "There must be at least 3 environment nodes to build a valid environment mesh !" );
// 			if ( !m_bEnvMeshDirty )
// 			{	// Already built !
// 				if ( !m_bEnvMeshCoefficientsDirty )
// 					return;	// Coefficients are also up to date...
// 
// 				m_EnvMesh.Dispose();
// 
// 				// Update mesh coefficients only
// 				VS_P3SH9[]	NewPrimitiveVertices = new VS_P3SH9[m_EnvNodes.Count];
// 				for ( int Vertexindex=0; Vertexindex < m_EnvNodes.Count; Vertexindex++ )
// 					NewPrimitiveVertices[Vertexindex] = m_EnvNodes[Vertexindex].V;
// 
// 				m_EnvMesh = new Primitive<VS_P3SH9,int>( m_Device, "EnvMesh", PrimitiveTopology.TriangleList, NewPrimitiveVertices, m_LastEnvMeshPrimitiveIndices );
// 
// 				m_bEnvMeshCoefficientsDirty = false;
// 				return;
// 			}
// 
// 			if ( m_EnvMesh != null )
// 				m_EnvMesh.Dispose();
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Apply Delaunay triangulation on the env nodes
// 			List<DelaunayTriangle>	Triangles = new List<DelaunayTriangle>();
// 			DelaunayTriangle.ms_Index = 0;
// 
// 			Vector2[]	Vertices = new Vector2[m_EnvNodes.Count];
// 			for ( int VertexIndex=0; VertexIndex < m_EnvNodes.Count; VertexIndex++ )
// 				Vertices[VertexIndex] = new Vector2( m_EnvNodes[VertexIndex].V.Position.X, m_EnvNodes[VertexIndex].V.Position.Z );
// 
// 			// 1] First, the seed triangle
// 			int	V0 = 0, V1 = 1, V2 = 2;
// 			Vector2	D0 = Vertices[V2] - Vertices[V1];
// 			Vector2	D1 = Vertices[V0] - Vertices[V1];
// 			float	CrossZ = D0.X * D1.Y - D0.Y * D1.X;
// 			if ( CrossZ < 0.0f )
// 			{	// Make the first triangle CCW !
// 				V1 = 2;
// 				V2 = 1;
// 			}
// 
// 			DelaunayTriangle	T = new DelaunayTriangle( Vertices, V0, V1, V2, null, null, null );
// 			Triangles.Add( T );
// 
// 			// 2] Next, the remaining vertices that we add one by one
// 			for ( int VertexIndex=3; VertexIndex < Vertices.Length; VertexIndex++ )
// 			{
// 				// Get current vertex's 2D position
// 				Vector2	Position = Vertices[VertexIndex];
// 
// 				// 2.1] Check if the vertex belongs to an existing triangle, or the closest triangle for that matter
// 				float				ClosestEdgeSqDistance = +float.MaxValue;
// 				DelaunayTriangle	ClosestTriangle = null;
// 				int					ClosestEdgeIndex = -1;
// 
// 				float[]	s = new float[3];	// Edge segments' parameters (inside edge if 0 <= s <= 1)
// 				bool[]	b = new bool[3];	// Inside states
// 				float[]	d = new float[3];	// Distances to edges (whether the vertex is inside the edge or not)
// 				for ( int TriangleIndex=0; TriangleIndex < Triangles.Count; TriangleIndex++ )
// 				{
// 					T = Triangles[TriangleIndex];
// 
// 					// Check the vertex is inside that triangle
// 					b[0] = Vector2.Dot( Position - T[0], T.Edges[0].Normal ) <= 0.0f;
// 					b[1] = Vector2.Dot( Position - T[1], T.Edges[1].Normal ) <= 0.0f;
// 					b[2] = Vector2.Dot( Position - T[2], T.Edges[2].Normal ) <= 0.0f;
// 
// 					if ( b[0] && b[1] && b[2] )
// 					{	// We're inside this triangle !
// 						// Split the tirangle in 3 other triangles by inserting the new vertex
// 						ClosestTriangle = T;
// 						ClosestEdgeIndex = -1;
// 						break;
// 					}
// 
// 					// Compute the distance to the 3 edges of that triangle
// 					s[0] = Vector2.Dot( Position - T[0], T[1] - T[0] ) / (T.Edges[0].Length*T.Edges[0].Length);
// 					s[1] = Vector2.Dot( Position - T[1], T[2] - T[1] ) / (T.Edges[1].Length*T.Edges[1].Length);
// 					s[2] = Vector2.Dot( Position - T[2], T[0] - T[2] ) / (T.Edges[2].Length*T.Edges[2].Length);
// 
// 					d[0] = (Position - Vector2.Lerp( T[0], T[1], s[0] )).LengthSquared();
// 					if ( d[0] < ClosestEdgeSqDistance )
// 					{
// 						ClosestEdgeSqDistance = d[0];
// 						ClosestTriangle = T;
// 						ClosestEdgeIndex = 0;
// 					}
// 					d[1] = (Position - Vector2.Lerp( T[1], T[2], s[1] )).LengthSquared();
// 					if ( d[1] < ClosestEdgeSqDistance )
// 					{
// 						ClosestEdgeSqDistance = d[1];
// 						ClosestTriangle = T;
// 						ClosestEdgeIndex = 1;
// 					}
// 					d[2] = (Position - Vector2.Lerp( T[2], T[0], s[2] )).LengthSquared();
// 					if ( d[2] < ClosestEdgeSqDistance )
// 					{
// 						ClosestEdgeSqDistance = d[2];
// 						ClosestTriangle = T;
// 						ClosestEdgeIndex = 2;
// 					}
// 				}
// 
// 				if ( ClosestTriangle == null )
// 					throw new Exception( "Couldn't find a candidate triangle !" );
// 
// 				// 2.2] Split the existing triangle and flip its edges
// 				if ( ClosestEdgeIndex == -1 )
// 				{	
// 					// Create 2 additional triangles
// 					DelaunayTriangle	T0 = new DelaunayTriangle( Vertices,
// 							ClosestTriangle.Vertices[1],
// 							ClosestTriangle.Vertices[2],
// 							VertexIndex,
// 							ClosestTriangle.Edges[1].Adjacent,
// 							null,
// 							null
// 							);
// 					Triangles.Add( T0 );
// 
// 					DelaunayTriangle	T1 = new DelaunayTriangle( Vertices,
// 							ClosestTriangle.Vertices[2],
// 							ClosestTriangle.Vertices[0],
// 							VertexIndex,
// 							ClosestTriangle.Edges[2].Adjacent,
// 							null,
// 							null
// 							);
// 					Triangles.Add( T1 );
// 
// 					// Change current triangle's 3rd vertex
// 					ClosestTriangle.Vertices[2] = VertexIndex;
// 					ClosestTriangle.UpdateInfos();
// 
// 					// Update adjacencies
// 					T0.Edges[1].Adjacent = T1;
// 					T1.Edges[1].Adjacent = ClosestTriangle;
// 					ClosestTriangle.Edges[1].Adjacent = T0;
// 
// 					// Flip the 3 edges
// 					ClosestTriangle.Edges[0].Flip( VertexIndex );
// 					T0.Edges[0].Flip( VertexIndex );
// 					T1.Edges[0].Flip( VertexIndex );
// 
// 					continue;
// 				}
// 
// 				// 2.3] Attach new triangles to the mesh
// 				// We have 3 cases to handle here :
// 				//
// 				//          Edge1
// 				//    xxxxxxxxx\     A0     .
// 				//    xxxxxxxxxx\         .
// 				//    xxxxxxxxxxx\      .
// 				//    xxxxxxxxxxxx\   .     A1
// 				//   ______________o._ _ _ _ _ _
// 				//   Edge0         |\
// 				//                    
// 				//                 |  \    A2
// 				//                     
// 				//                 |    \
// 				//
// 				// The vertex lies in area A0 :
// 				//	_ We must create a new triangle connected to Edge1 and flip Edge1
// 				//
// 				// The vertex lies in area A1 :
// 				//	_ We must create a new triangle connected to Edge1 and flip Edge1
// 				//
// 				// The vertex lies in area A2 :
// 				//	_ We must create 2 new triangles connected to both Edge0 and Edge1
// 				//
// 				float	DistanceToEdgeVertex0 = (Position - ClosestTriangle[ClosestEdgeIndex]).Length();
// 				float	DistanceToEdgeVertex1 = (Position - ClosestTriangle[ClosestEdgeIndex+1]).Length();
// 				int		OtherCandidateEdgeIndex = (DistanceToEdgeVertex0 < DistanceToEdgeVertex1 ? ClosestEdgeIndex+3-1 : ClosestEdgeIndex+1) % 3;
// 				Vector2	TipVertex = DistanceToEdgeVertex0 < DistanceToEdgeVertex1 ? ClosestTriangle[ClosestEdgeIndex] : ClosestTriangle[ClosestEdgeIndex+1];
// 				Vector2	N0 = ClosestTriangle.Edges[ClosestEdgeIndex].Normal;
// 				Vector2	N1 = ClosestTriangle.Edges[OtherCandidateEdgeIndex].Normal;
// 				bool	bInFrontEdge0 = Vector2.Dot( Position - TipVertex, N0 ) >= 0.0f;
// 				bool	bInFrontEdge1 = Vector2.Dot( Position - TipVertex, N1 ) >= 0.0f;
// 				if ( bInFrontEdge0 && !bInFrontEdge1 )
// 				{	// Add to edge 0
// 					T = new DelaunayTriangle( Vertices,
// 							ClosestTriangle.Vertices[ClosestEdgeIndex+1],
// 							ClosestTriangle.Vertices[ClosestEdgeIndex],
// 							VertexIndex,
// 							ClosestTriangle,
// 							null, null
// 							);
// 					Triangles.Add( T );
// 					ClosestTriangle.Edges[ClosestEdgeIndex].Adjacent = T;
// 
// 					T.Edges[0].Flip( VertexIndex );
// 				}
// 				else if ( !bInFrontEdge0 && bInFrontEdge1 )
// 				{	// Add to edge 1
// 					T = new DelaunayTriangle( Vertices,
// 							ClosestTriangle.Vertices[OtherCandidateEdgeIndex+1],
// 							ClosestTriangle.Vertices[OtherCandidateEdgeIndex],
// 							VertexIndex,
// 							ClosestTriangle,
// 							null, null
// 							);
// 					Triangles.Add( T );
// 					ClosestTriangle.Edges[OtherCandidateEdgeIndex].Adjacent = T;
// 
// 					T.Edges[0].Flip( VertexIndex );
// 				}
// 				else if ( bInFrontEdge0 && bInFrontEdge1 )
// 				{	// Add to both edges
// 					DelaunayTriangle	T0 = new DelaunayTriangle( Vertices,
// 							ClosestTriangle.Vertices[ClosestEdgeIndex+1],
// 							ClosestTriangle.Vertices[ClosestEdgeIndex],
// 							VertexIndex,
// 							ClosestTriangle,
// 							null, null
// 							);
// 					Triangles.Add( T0 );
// 					ClosestTriangle.Edges[ClosestEdgeIndex].Adjacent = T0;
// 
// 					DelaunayTriangle	T1 = new DelaunayTriangle( Vertices,
// 							ClosestTriangle.Vertices[OtherCandidateEdgeIndex+1],
// 							ClosestTriangle.Vertices[OtherCandidateEdgeIndex],
// 							VertexIndex,
// 							ClosestTriangle,
// 							null, null
// 							);
// 					Triangles.Add( T1 );
// 					ClosestTriangle.Edges[OtherCandidateEdgeIndex].Adjacent = T1;
// 
// 					if ( (OtherCandidateEdgeIndex+1)%3 == ClosestEdgeIndex )
// 					{	// Other edge is previous edge
// 						T0.Edges[1].Adjacent = T1;
// 						T1.Edges[2].Adjacent = T0;
// 					}
// 					else if ( (ClosestEdgeIndex+1)%3 == OtherCandidateEdgeIndex )
// 					{	// Other edge is next edge
// 						T0.Edges[2].Adjacent = T1;
// 						T1.Edges[1].Adjacent = T0;
// 					}
// 					else
// 						throw new Exception( "We got 2 edges that are not consecutive !" );
// 				}
// 				else
// 					throw new Exception( "Should be in front of at least one edge !" );
// 			}
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Build the final primitive
// 			VS_P3SH9[]	PrimitiveVertices = new VS_P3SH9[m_EnvNodes.Count];
// 			for ( int Vertexindex=0; Vertexindex < m_EnvNodes.Count; Vertexindex++ )
// 				PrimitiveVertices[Vertexindex] = m_EnvNodes[Vertexindex].V;
// 
// 			m_LastEnvMeshPrimitiveIndices = new int[3*Triangles.Count];
// 			for ( int TriangleIndex=0; TriangleIndex < Triangles.Count; TriangleIndex++ )
// 			{
// 				T = Triangles[TriangleIndex];
// 				m_LastEnvMeshPrimitiveIndices[3*TriangleIndex+0] = T.Vertices[0];
// 				m_LastEnvMeshPrimitiveIndices[3*TriangleIndex+1] = T.Vertices[1];
// 				m_LastEnvMeshPrimitiveIndices[3*TriangleIndex+2] = T.Vertices[2];
// 			}
// 
// 			m_EnvMesh = new Primitive<VS_P3SH9,int>( m_Device, "EnvMesh", PrimitiveTopology.TriangleList, PrimitiveVertices, m_LastEnvMeshPrimitiveIndices );
// 
// 			m_bEnvMeshDirty = false;
// 			m_bEnvMeshCoefficientsDirty = false;
// 		}
// 
// 		#endregion
// 
// 		#endregion

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			// Provide deferred rendering interface data
			IDeferredRendering	I = _Interface as IDeferredRendering;
			if ( I != null )
			{
				I.GBuffer0 = m_MaterialBuffer;
				I.GBuffer1 = m_GeometryBuffer;
				I.GBuffer2 = m_EmissiveBuffer;
				I.LightBuffer = m_InferredLighting.LightBuffer;
				return;
			}
 
// 			// Provide environment SH map interface data
// 			IEnvironmentSHMap	I2 = _Interface as IEnvironmentSHMap;
// 			if ( I2 != null )
// 			{
// 				I2.SHEnvMapOffset = m_SHEnvOffset;
// 				I2.SHEnvMapScale = m_SHEnvScale;
// 				I2.SHEnvMapSize = m_SHEnvMapSize;
// 				I2.SHEnvMap = m_SHEnvMap;
// 			}
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected void DepthPipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup only a depth stencil (no render target) and clear it
			m_Device.SetRenderTarget( null as IRenderTarget, m_Device.DefaultDepthStencil );
			m_Device.SetViewport( 0, 0, m_Device.DefaultDepthStencil.Width, m_Device.DefaultDepthStencil.Height, 0.0f, 1.0f );
			m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );
		}

		protected void EmissivePipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup the emissive render target and clear it
			m_Device.SetRenderTarget( m_EmissiveBuffer, m_Device.DefaultDepthStencil );
			m_Device.SetViewport( 0, 0, m_EmissiveBuffer.Width, m_EmissiveBuffer.Height, 0.0f, 1.0f );
		}

		protected void MainPipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup our multiple render targets
			m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_MaterialBuffer, m_GeometryBuffer }, m_Device.DefaultDepthStencil );
			m_Device.SetViewport( 0, 0, m_MaterialBuffer.Width, m_MaterialBuffer.Height, 0.0f, 1.0f );
		}

// 		protected void CubeMapRender_RenderingStart( Pipeline _Sender )
// 		{
// 			// Setup our multiple render targets
// 			m_Device.SetMultipleRenderTargets( new RenderTargetView[]
// 				{
// 					m_EnvironmentArrayMaterial.GetSingleRenderTargetView( 0, m_EnvironmentCubeMapFaceIndex ),
// 					m_EnvironmentArrayGeometry.GetSingleRenderTargetView( 0, m_EnvironmentCubeMapFaceIndex )
// 				}, m_EnvironmentDepth.DepthStencilView );
// 			m_Device.SetViewport( 0, 0, m_EnvironmentArrayGeometry.Width, m_EnvironmentArrayGeometry.Height, 0.0f, 1.0f );
// 		}

		#endregion
	}
}
