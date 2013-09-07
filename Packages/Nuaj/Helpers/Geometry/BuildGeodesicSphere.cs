using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{	/// <summary>
	/// The base shapes that can be used to build the sphere from
	/// </summary>
	public enum	GEODESIC_SPHERE_BASE_SHAPE
	{
		TETRAHEDRON,	// 3 triangles
		OCTAHEDRON,		// 8 triangles
		ICOSAHEDRON,	// 20 triangles
		CUBE,			// 12 triangles
		CUBE_DETACHED,	// 12 triangles with distinct corners for each face (allows to map UVs in [0,1] without nasty wrapping)
	}

	public class	GeodesicSphere<VS,I> : GeometryBuilder<VS,I> where VS:struct where I:struct
	{
		/// <summary>
		/// Builds a subdivided unit radius geodesic sphere
		/// </summary>
		/// <param name="_SubdivisionsCount">The amount of subdivisions for each triangle of the base shape</param>
		/// <param name="_Shape">The base shape to subdivide</param>
		/// <param name="_Mapper">The tiling factor on U & V (by default, they tile once on the entire sphere) NOTE: U is mapped to the Phi angle while V is mapped to the Theta angle</param>
		/// <param name="_Writer">The accessor that is able to write to the vertex/index buffers</param>
		/// <param name="_VB">The resulting vertex buffer</param>
		/// <param name="_IB">The resulting index buffer</param>
		/// <remarks>The base icosahedron shape has 20 triangles.
		/// _ A subdivision of 0 leave the triangles intact so you get 20 triangles.
		/// _ A subdivision of 1 splits each triangle into 4 new triangles, so you obtain 4*20=80 triangles total.
		/// _ A subdivision of 2 splits each triangle's 4 triangles into 4 new triangles, so you obtain 4*4*20=320 triangles total.
		/// You get the idea...
		/// The general formule is TotalTrianglesCount = BaseShapeTrianglesCount * 4^SubdivisionsCount
		/// </remarks>
		public static Primitive<VS,I>	Build( Device _Device, string _Name, GEODESIC_SPHERE_BASE_SHAPE _Shape, int _SubdivisionsCount, GeometryMapper _Mapper, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess )
		{
			return Build( _Device, _Name, _Shape, _SubdivisionsCount, _Mapper, _Writer, _PostProcess, false, 0.0f );
		}

		public static Primitive<VS,I>	Build( Device _Device, string _Name, GEODESIC_SPHERE_BASE_SHAPE _Shape, int _SubdivisionsCount, GeometryMapper _Mapper, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess, bool _bBuildAdjacency, float _fVertexThreshold )
		{
			// Build base shape vertices & indices
			Vector3[]	BaseVertices = null;
			int[,]		BaseIndices = null;
			bool		bQuads = false;
			switch ( _Shape )
			{
				case GEODESIC_SPHERE_BASE_SHAPE.TETRAHEDRON:
					BuildBaseShapeTetrahedron( out BaseVertices, out BaseIndices );
					break;
				case GEODESIC_SPHERE_BASE_SHAPE.OCTAHEDRON:
					BuildBaseShapeOctahedron( out BaseVertices, out BaseIndices );
					break;
				case GEODESIC_SPHERE_BASE_SHAPE.ICOSAHEDRON:
					BuildBaseShapeIcosahedron( out BaseVertices, out BaseIndices );
					break;
				case GEODESIC_SPHERE_BASE_SHAPE.CUBE:
					BuildBaseShapeCube( out BaseVertices, out BaseIndices, false );
					bQuads = true;
					break;
				case GEODESIC_SPHERE_BASE_SHAPE.CUBE_DETACHED:
					BuildBaseShapeCube( out BaseVertices, out BaseIndices, true );
					bQuads = true;
					break;
			}

			VS[]	Vertices = null;
			I[]		Indices = null;
			if ( !bQuads )
			{	// Build subdivided triangles
				int		BaseTrianglesCount = BaseIndices.GetLength( 0 );
				int		TrianglesCount = BaseTrianglesCount * (int) Math.Pow( 4, _SubdivisionsCount );
				int		IndicesCount = 3 * TrianglesCount;
				int		VerticesCount = BaseTrianglesCount * (3 + (int) Math.Pow( 4, _SubdivisionsCount ) - 1);

				Vertices = new VS[VerticesCount];
				Indices = new I[IndicesCount];

				int	VertexIndex = 0;
				int	IndexIndex = 0;
				for ( int SeedTriangleIndex=0; SeedTriangleIndex < BaseTrianglesCount; SeedTriangleIndex++ )
				{
					Vector3	Vertex0 = BaseVertices[BaseIndices[SeedTriangleIndex,0]];
					Vector3	Vertex1 = BaseVertices[BaseIndices[SeedTriangleIndex,1]];
					Vector3	Vertex2 = BaseVertices[BaseIndices[SeedTriangleIndex,2]];

					Vector3	FaceNormal = Vector3.Cross( Vertex2-Vertex1, Vertex0-Vertex1 );
					FaceNormal.Normalize();

					// Create the 3 initial vertices
					Vector3[]	TriangleVertices = new Vector3[3];
					Vector3		Normal, Tangent, BiTangent, UVW;
					int	I0 = VertexIndex;
					BuildVertexData( Vertex0, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					TriangleVertices[0] = Normal;

					int	I1 = VertexIndex;
					BuildVertexData( Vertex1, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					TriangleVertices[1] = Normal;

					int	I2 = VertexIndex;
					BuildVertexData( Vertex2, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					TriangleVertices[2] = Normal;

					BuildTriangles( Vertices, Indices, ref VertexIndex, ref IndexIndex, FaceNormal, _Mapper, TriangleVertices, I0, I1, I2, _Writer, _SubdivisionsCount );
				}
			}
			else
			{	// Build subdivided quads
				int		BaseQuadsCount = BaseIndices.GetLength( 0 );
				int		TrianglesCount = 2*BaseQuadsCount * (int) Math.Pow( 4, _SubdivisionsCount );
				int		IndicesCount = 3 * TrianglesCount;
				int		VerticesCount = BaseQuadsCount * (5 + (int) Math.Pow( 5, _SubdivisionsCount ) - 1);

				Vertices = new VS[VerticesCount];
				Indices = new I[IndicesCount];

				int	VertexIndex = 0;
				int	IndexIndex = 0;
				for ( int SeedQuadIndex=0; SeedQuadIndex < BaseQuadsCount; SeedQuadIndex++ )
				{
					Vector3	Vertex0 = PushVertex( BaseVertices[BaseIndices[SeedQuadIndex,0]] );
					Vector3	Vertex1 = PushVertex( BaseVertices[BaseIndices[SeedQuadIndex,1]] );
					Vector3	Vertex2 = PushVertex( BaseVertices[BaseIndices[SeedQuadIndex,2]] );
					Vector3	Vertex3 = PushVertex( BaseVertices[BaseIndices[SeedQuadIndex,3]] );

					Vector3	FaceNormal = Vector3.Cross( Vertex2-Vertex1, Vertex0-Vertex1 );
					FaceNormal.Normalize();

					// Create the 4 initial vertices
					Vector3[]	QuadVertices = new Vector3[4];
					Vector3		Normal, Tangent, BiTangent, UVW;
					int	I0 = VertexIndex;
					BuildVertexData( Vertex0, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					QuadVertices[0] = Normal;

					int	I1 = VertexIndex;
					BuildVertexData( Vertex1, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					QuadVertices[1] = Normal;

					int	I2 = VertexIndex;
					BuildVertexData( Vertex2, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					QuadVertices[2] = Normal;

					int	I3 = VertexIndex;
					BuildVertexData( Vertex3, FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );
					QuadVertices[3] = Normal;

					BuildQuads( Vertices, Indices, ref VertexIndex, ref IndexIndex, FaceNormal, _Mapper, QuadVertices, I0, I1, I2, I3, _Writer, _SubdivisionsCount );
				}
			}

			// Last chance for a post-processing !
			if ( _PostProcess != null )
				_PostProcess( Vertices, Indices );

			// Build optional adjacency list
			if ( _bBuildAdjacency )
			{
				IndexAccessor	Accessor = new IndexAccessor( _Writer );
				Adjacency<VS,I>.BuildTriangleListAdjacency( Vertices, Indices, Accessor, _fVertexThreshold, out Indices );
			}

			// Create the primitive
			return new Primitive<VS,I>( _Device, _Name, _bBuildAdjacency ? PrimitiveTopology.TriangleListWithAdjacency : PrimitiveTopology.TriangleList, Vertices, Indices );
		}

		/// <summary>
		/// Builds a tetrahedron base shape
		/// </summary>
		/// <param name="_Vertices"></param>
		/// <param name="_Indices"></param>
		protected static void	BuildBaseShapeTetrahedron( out Vector3[] _Vertices, out int[,] _Indices )
		{
			// Build the original tetrahedron vertices & triangles
			_Vertices = new Vector3[4];
			_Indices = new int[4,3];

			_Vertices[0] = new Vector3( 0.0f, 1.0f, 0.0f );	// Top vertex

			float	BottomY = -1.0f / 3.0f;
			float	BottomRadius = (float) Math.Sqrt( 1.0 - BottomY*BottomY );
			_Vertices[1] = new Vector3( BottomRadius, BottomY, 0.0f );
			_Vertices[2] = new Vector3( BottomRadius * (float) Math.Cos( 2.0 * Math.PI / 3.0 ), BottomY, -BottomRadius * (float) Math.Sin( 2.0 * Math.PI / 3.0 ) );
			_Vertices[3] = new Vector3( BottomRadius * (float) Math.Cos( 2.0 * Math.PI / 3.0 ), BottomY, BottomRadius * (float) Math.Sin( 2.0 * Math.PI / 3.0 ) );

			_Indices[0,0] = 0;
			_Indices[0,1] = 1;
			_Indices[0,2] = 2;
			_Indices[1,0] = 0;
			_Indices[1,1] = 2;
			_Indices[1,2] = 3;
			_Indices[2,0] = 0;
			_Indices[2,1] = 3;
			_Indices[2,2] = 1;
			// Bottom face
			_Indices[3,0] = 1;
			_Indices[3,1] = 3;
			_Indices[3,2] = 2;
		}

		/// <summary>
		/// Builds an octahedron base shape
		/// </summary>
		/// <param name="_Vertices"></param>
		/// <param name="_Indices"></param>
		protected static void	BuildBaseShapeOctahedron( out Vector3[] _Vertices, out int[,] _Indices )
		{
			// Build the original octahedron vertices & triangles
			_Vertices = new Vector3[1+4+1];
			_Indices = new int[8,3];

			_Vertices[0] = new Vector3( 0.0f, 1.0f, 0.0f );		// Top vertex
			_Vertices[1] = new Vector3( 1.0f, 0.0f, 0.0f );
			_Vertices[2] = new Vector3( 0.0f, 0.0f, -1.0f );
			_Vertices[3] = new Vector3( -1.0f, 0.0f, 0.0f );
			_Vertices[4] = new Vector3( 0.0f, 0.0f, 1.0f );
			_Vertices[5] = new Vector3( 0.0f, -1.0f, 0.0f );	// Bottom vertex

			// Top faces
			_Indices[0,0] = 0;
			_Indices[0,1] = 1;
			_Indices[0,2] = 2;
			_Indices[1,0] = 0;
			_Indices[1,1] = 2;
			_Indices[1,2] = 3;
			_Indices[2,0] = 0;
			_Indices[2,1] = 3;
			_Indices[2,2] = 4;
			_Indices[3,0] = 0;
			_Indices[3,1] = 4;
			_Indices[3,2] = 1;

			// Bottom faces
			_Indices[4,0] = 5;
			_Indices[4,1] = 2;
			_Indices[4,2] = 1;
			_Indices[5,0] = 5;
			_Indices[5,1] = 3;
			_Indices[5,2] = 2;
			_Indices[6,0] = 5;
			_Indices[6,1] = 4;
			_Indices[6,2] = 3;
			_Indices[7,0] = 5;
			_Indices[7,1] = 1;
			_Indices[7,2] = 4;
		}

		/// <summary>
		/// Builds an icosahedron base shape
		/// </summary>
		/// <param name="_Vertices"></param>
		/// <param name="_Indices"></param>
		protected static void	BuildBaseShapeIcosahedron( out Vector3[] _Vertices, out int[,] _Indices )
		{
			// Build the original icosahedron vertices & triangles
			_Vertices = new Vector3[1+5+5+1];
			_Indices = new int[20,3];

			// From http://www.vb-helper.com/tutorial_platonic_solids.html#Icosahedron
			// R = (S/2) / Sin(PI/5) => S = 2*R*sin(PI/5)
			// H = sqrt(S * S - R * R)  => with R=1  H = sqrt( 4*sin²(PI/5)-1 )
			//
			float	H = (float) Math.Sqrt( 4.0 * Math.Sin( 0.2 * Math.PI ) * Math.Sin( 0.2 * Math.PI ) - 1.0 );
			float	TopY = 1.0f - H;
			float	ThetaTop = (float) Math.Acos( TopY );
			float	BottomY = -1.0f + H;
			float	ThetaBottom = (float) Math.Acos( BottomY );

			_Vertices[0] = new Vector3( 0.0f, 1.0f, 0.0f );	// Top
			for ( int i=0; i < 5; i++ )
			{
				int		Ni = (i+1) % 5;
				float	PhiTop = (float) (i * 2.0 * Math.PI / 5.0);
				float	PhiBottom = (float) ((i+0.5) * 2.0 * Math.PI / 5.0);

				// Build top ring
				_Vertices[1+i] = new Vector3( (float) (Math.Cos( PhiTop ) * Math.Sin( ThetaTop )), TopY, -(float) (Math.Sin( PhiTop ) * Math.Sin( ThetaTop )) );
				// Build top ring
				_Vertices[6+i] = new Vector3( (float) (Math.Cos( PhiBottom ) * Math.Sin( ThetaBottom )), BottomY, -(float) (Math.Sin( PhiBottom ) * Math.Sin( ThetaBottom )) );

				// Build top triangles
				_Indices[0+i,0] = 0;
				_Indices[0+i,1] = 1+i;
				_Indices[0+i,2] = 1+Ni;

				// Build middle ring "quads"
				_Indices[5+2*i+0,0] = 1+i;
				_Indices[5+2*i+0,1] = 6+i;
				_Indices[5+2*i+0,2] = 1+Ni;
				_Indices[5+2*i+1,0] = 1+Ni;
				_Indices[5+2*i+1,1] = 6+i;
				_Indices[5+2*i+1,2] = 6+Ni;

				// Build bottom triangles
				_Indices[15+i,0] = 11;
				_Indices[15+i,1] = 6+Ni;
				_Indices[15+i,2] = 6+i;
			}
			_Vertices[11] = new Vector3( 0.0f, -1.0f, 0.0f );	// Bottom
		}

		/// <summary>
		/// Builds a cube base shape
		/// </summary>
		/// <param name="_Vertices"></param>
		/// <param name="_Indices"></param>
		protected static void	BuildBaseShapeCube( out Vector3[] _Vertices, out int[,] _Indices, bool _DetachedVertices )
		{
			// Build the original octahedron vertices & triangles
			_Vertices = new Vector3[_DetachedVertices ? 6*4 : 8];
			_Indices = new int[6,4];

			if ( _DetachedVertices )
			{	// Build 4 distinct vertices for each face
				// Front
				_Vertices[0] = new Vector3( -1.0f, -1.0f, +1.0f );
				_Vertices[1] = new Vector3( +1.0f, -1.0f, +1.0f );
				_Vertices[2] = new Vector3( +1.0f, +1.0f, +1.0f );
				_Vertices[3] = new Vector3( -1.0f, +1.0f, +1.0f );
				// Back
				_Vertices[4] = new Vector3( -1.0f, -1.0f, -1.0f );
				_Vertices[5] = new Vector3( -1.0f, +1.0f, -1.0f );
				_Vertices[6] = new Vector3( +1.0f, +1.0f, -1.0f );
				_Vertices[7] = new Vector3( +1.0f, -1.0f, -1.0f );
				// Left
				_Vertices[8] = new Vector3( -1.0f, -1.0f, -1.0f );
				_Vertices[9] = new Vector3( -1.0f, -1.0f, +1.0f );
				_Vertices[10] = new Vector3( -1.0f, +1.0f, +1.0f );
				_Vertices[11] = new Vector3( -1.0f, +1.0f, -1.0f );
				// Right
				_Vertices[12] = new Vector3( +1.0f, -1.0f, +1.0f );
				_Vertices[13] = new Vector3( +1.0f, -1.0f, -1.0f );
				_Vertices[14] = new Vector3( +1.0f, +1.0f, -1.0f );
				_Vertices[15] = new Vector3( +1.0f, +1.0f, +1.0f );
				// Top
				_Vertices[16] = new Vector3( -1.0f, +1.0f, -1.0f );
				_Vertices[17] = new Vector3( -1.0f, +1.0f, +1.0f );
				_Vertices[18] = new Vector3( +1.0f, +1.0f, +1.0f );
				_Vertices[19] = new Vector3( +1.0f, +1.0f, -1.0f );
				// Bottom
				_Vertices[20] = new Vector3( -1.0f, -1.0f, +1.0f );
				_Vertices[21] = new Vector3( -1.0f, -1.0f, -1.0f );
				_Vertices[22] = new Vector3( +1.0f, -1.0f, -1.0f );
				_Vertices[23] = new Vector3( +1.0f, -1.0f, +1.0f );

				// Indices are pretty straightforward
				_Indices[0,0] = 0;
				_Indices[0,1] = 1;
				_Indices[0,2] = 2;
				_Indices[0,3] = 3;
				_Indices[1,0] = 4;
				_Indices[1,1] = 5;
				_Indices[1,2] = 6;
				_Indices[1,3] = 7;
				_Indices[2,0] = 8;
				_Indices[2,1] = 9;
				_Indices[2,2] = 10;
				_Indices[2,3] = 11;
				_Indices[3,0] = 12;
				_Indices[3,1] = 13;
				_Indices[3,2] = 14;
				_Indices[3,3] = 15;
				_Indices[4,0] = 16;
				_Indices[4,1] = 17;
				_Indices[4,2] = 18;
				_Indices[4,3] = 19;
				_Indices[5,0] = 20;
				_Indices[5,1] = 21;
				_Indices[5,2] = 22;
				_Indices[5,3] = 23;
			}
			else
			{	// Simply build the 8 corners and 6 quads that link them
				_Vertices[0] = new Vector3( -1.0f, -1.0f, +1.0f );
				_Vertices[1] = new Vector3( +1.0f, -1.0f, +1.0f );
				_Vertices[2] = new Vector3( +1.0f, +1.0f, +1.0f );
				_Vertices[3] = new Vector3( -1.0f, +1.0f, +1.0f );
				_Vertices[4] = new Vector3( -1.0f, -1.0f, -1.0f );
				_Vertices[5] = new Vector3( +1.0f, -1.0f, -1.0f );
				_Vertices[6] = new Vector3( +1.0f, +1.0f, -1.0f );
				_Vertices[7] = new Vector3( -1.0f, +1.0f, -1.0f );

				// Front
				_Indices[0,0] = 0;
				_Indices[0,1] = 1;
				_Indices[0,2] = 2;
				_Indices[0,3] = 3;
				// Back
				_Indices[1,0] = 5;
				_Indices[1,1] = 4;
				_Indices[1,2] = 7;
				_Indices[1,3] = 6;
				// Left
				_Indices[2,0] = 4;
				_Indices[2,1] = 0;
				_Indices[2,2] = 3;
				_Indices[2,3] = 7;
				// Right
				_Indices[3,0] = 1;
				_Indices[3,1] = 5;
				_Indices[3,2] = 6;
				_Indices[3,3] = 2;
				// Top
				_Indices[4,0] = 7;
				_Indices[4,1] = 3;
				_Indices[4,2] = 2;
				_Indices[4,3] = 6;
				// Bottom
				_Indices[5,0] = 0;
				_Indices[5,1] = 4;
				_Indices[5,2] = 5;
				_Indices[5,3] = 1;
			}
		}

		// Recursively builds the triangles for each seed triangle
		protected static void	BuildTriangles( VS[] _Vertices, I[] _Indices, ref int _VertexIndex, ref int _IndexIndex, Vector3 _FaceNormal, GeometryMapper _Mapper, Vector3[] _TriangleVertices, int _I0, int _I1, int _I2, IGeometryWriter<VS,I> _Writer, int _SubdivisionsCount )
		{
			if ( _SubdivisionsCount == 0 )
			{	// Generate the triangle
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I0 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I1 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I2 );
				return;
			}

			// Subdivide the triangle into 3 additional vertices
			Vector3[]	NewVertices = new Vector3[3]
			{
				PushVertex( 0.5f * (_TriangleVertices[0] + _TriangleVertices[1]) ),
				PushVertex( 0.5f * (_TriangleVertices[1] + _TriangleVertices[2]) ),
				PushVertex( 0.5f * (_TriangleVertices[2] + _TriangleVertices[0]) ),
			};
			Vector3	Normal, Tangent, BiTangent, UVW;

			int	NewI0 = _VertexIndex;
			BuildVertexData( NewVertices[0], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			int	NewI1 = _VertexIndex;
			BuildVertexData( NewVertices[1], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			int	NewI2 = _VertexIndex;
			BuildVertexData( NewVertices[2], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			// Recurse through the 4 new triangles
			_SubdivisionsCount--;
			BuildTriangles( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { _TriangleVertices[0], NewVertices[0], NewVertices[2] },
							_I0, NewI0, NewI2,
							_Writer, _SubdivisionsCount );

			BuildTriangles( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { NewVertices[0], _TriangleVertices[1], NewVertices[1] },
							NewI0, _I1, NewI1,
							_Writer, _SubdivisionsCount );

			BuildTriangles( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { NewVertices[2], NewVertices[1], _TriangleVertices[2] },
							NewI2, NewI1, _I2,
							_Writer, _SubdivisionsCount );

			BuildTriangles( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { NewVertices[0], NewVertices[1], NewVertices[2] },
							NewI0, NewI1, NewI2,
							_Writer, _SubdivisionsCount );
		}

		// Recursively builds the quads for each seed quad
		protected static void	BuildQuads( VS[] _Vertices, I[] _Indices, ref int _VertexIndex, ref int _IndexIndex, Vector3 _FaceNormal, GeometryMapper _Mapper, Vector3[] _QuadVertices, int _I0, int _I1, int _I2, int _I3, IGeometryWriter<VS,I> _Writer, int _SubdivisionsCount )
		{
			if ( _SubdivisionsCount == 0 )
			{	// Generate the 2 triangles for the quad
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I0 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I1 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I2 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I0 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I2 );
				_Writer.WriteIndexData( ref _Indices[_IndexIndex++], _I3 );
				return;
			}

			// Subdivide the quad into 5 additional vertices
			Vector3[]	NewVertices = new Vector3[5]
			{
				PushVertex( 0.5f * (_QuadVertices[0] + _QuadVertices[1]) ),
				PushVertex( 0.5f * (_QuadVertices[1] + _QuadVertices[2]) ),
				PushVertex( 0.5f * (_QuadVertices[2] + _QuadVertices[3]) ),
				PushVertex( 0.5f * (_QuadVertices[3] + _QuadVertices[0]) ),
				PushVertex( 0.25f * (_QuadVertices[0] + _QuadVertices[1] + _QuadVertices[2] + _QuadVertices[3]) ),
			};
			Vector3	Normal, Tangent, BiTangent, UVW;

			int	NewI0 = _VertexIndex;
			BuildVertexData( NewVertices[0], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			int	NewI1 = _VertexIndex;
			BuildVertexData( NewVertices[1], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			int	NewI2 = _VertexIndex;
			BuildVertexData( NewVertices[2], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			int	NewI3 = _VertexIndex;
			BuildVertexData( NewVertices[3], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

				// Center vertex
			int	NewI4 = _VertexIndex;
			BuildVertexData( NewVertices[4], _FaceNormal, _Mapper, out Normal, out Tangent, out BiTangent, out UVW );
			_Writer.WriteVertexData( ref _Vertices[_VertexIndex++], Normal, Normal, Tangent, BiTangent, UVW, new Color4( 0, 0, 0, 1 ) );

			// Recurse through the 4 new quads
			_SubdivisionsCount--;
			BuildQuads( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { _QuadVertices[0], NewVertices[0], NewVertices[4], NewVertices[3] },
							_I0, NewI0, NewI4, NewI3,
							_Writer, _SubdivisionsCount );

			BuildQuads( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { NewVertices[0], _QuadVertices[1], NewVertices[1], NewVertices[4] },
							NewI0, _I1, NewI1, NewI4,
							_Writer, _SubdivisionsCount );

			BuildQuads( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { NewVertices[4], NewVertices[1], _QuadVertices[2], NewVertices[2] },
							NewI4, NewI1, _I2, NewI2,
							_Writer, _SubdivisionsCount );

			BuildQuads( _Vertices, _Indices, ref _VertexIndex, ref _IndexIndex, _FaceNormal, _Mapper,
							new Vector3[] { NewVertices[3], NewVertices[4], NewVertices[2], _QuadVertices[3] },
							NewI3, NewI4, NewI2, _I3,
							_Writer, _SubdivisionsCount );
		}

		// Builds the vertex data
		protected static void	BuildVertexData( Vector3 _VertexPosition, Vector3 _FaceNormal, GeometryMapper _Mapper, out Vector3 _Normal, out Vector3 _Tangent, out Vector3 _BiTangent, out Vector3 _UVW )
		{
			// Build NTB
			_Normal = _VertexPosition;

			// Build UVW, Tangent & BiTangent
			_Tangent = new Vector3();
			_BiTangent = new Vector3();
			_UVW = new Vector3();
			_Mapper.BuildUVW( _VertexPosition, _Normal, _FaceNormal, ref _Tangent, ref _BiTangent, ref _UVW, false );
		}

		// Pushes the vertex to the surface of the sphere
		protected static Vector3	PushVertex( Vector3 _Vertex )
		{
			_Vertex.Normalize();
			return _Vertex;
		}
	}
}
