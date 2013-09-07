using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	public class	Adjacency<VS,I> where VS:struct where I:struct
	{
		/// <summary>
		/// This is a stripped triangle class that only contains the opposite welded vertex index to compare with our own welded index
		///  and the corresponding adjacent vertex index
		/// </summary>
		protected class Triangle
		{
			public int	m_AdjacentVertex = -1;
			public int	m_PreviousWeldedVertex = -1;

			public Triangle( int _Vertex0, int _Vertex1, int _Vertex2, int _WeldedVertex0, int _WeldedVertex1, int _WeldedVertex2 )
			{
				m_AdjacentVertex = _Vertex1;
				m_PreviousWeldedVertex = _WeldedVertex2;
			}
		}

		/// <summary>
		/// Builds a triangle list of indices with adjacency (i.e. PrimitiveTopology.TriangleListWithAdjacency) given a list of vertices and simple indices (i.e. PrimitiveTopology.TriangleList)
		/// </summary>
		/// <param name="_Vertices">The list of vertices</param>
		/// <param name="_Indices">The list of indices</param>
		/// <param name="_fVertexThreshold">A vertex adjacency threshold to weld close vertices together</param>
		/// <param name="_Accessor">The accessor to read/write indices</param>
		/// <param name="_AdjacentIndices">The resulting list of adjacent indices</param>
		/// <returns>True if some edges contain more than 2 sharing faces, meaning your mesh is somewhat bugged...</returns>
		/// <typeparam name="VS">VertexStructure type</typeparam>
		/// <typeparam name="I">Index type</typeparam>
		public static bool	BuildTriangleListAdjacency( VS[] _Vertices, I[] _Indices, IIndexAccessor<I> _Accessor, float _fVertexThreshold, out I[] _AdjacentIndices )
		{
			VertexAccessor<VS>	Accessor = new VertexAccessor<VS>();
			int		IndicesCount = _Indices.Length;

			// =============================================
			// 1] Weld all possible vertices
			List<Vector3>		WeldedPositions = new List<Vector3>();
			Dictionary<int,int>	OriginalIndex2WeldedIndex = new Dictionary<int,int>();
			for ( int VertexIndex=0; VertexIndex < _Vertices.Length; VertexIndex++ )
			{
				Vector3	Position = ReadPosition( Accessor, _Vertices[VertexIndex] );

				int	MatchedWeldedVertexIndex = -1;
				for ( int WeldedVertexIndex=0; WeldedVertexIndex < WeldedPositions.Count; WeldedVertexIndex++ )
				{
					Vector3	WeldedPosition = WeldedPositions[WeldedVertexIndex];
					Vector3	DeltaPosition = Position - WeldedPosition;

					// Using dummy manhattan distance here as welding is somewhat of a N² process !
					if ( Math.Abs( DeltaPosition.X ) > _fVertexThreshold ||
						 Math.Abs( DeltaPosition.Y ) > _fVertexThreshold ||
						 Math.Abs( DeltaPosition.Z ) > _fVertexThreshold )
						continue;	// Too far away...

					// Found a match !
					MatchedWeldedVertexIndex = WeldedVertexIndex;
					break;
				}

				if ( MatchedWeldedVertexIndex == -1 )
				{	// No match found ! This is a new vertex !
					MatchedWeldedVertexIndex = WeldedPositions.Count;
					WeldedPositions.Add( Position );
				}

				// Map the 2 indices together
				OriginalIndex2WeldedIndex[VertexIndex] = MatchedWeldedVertexIndex;
			}

			// =============================================
			// 2] Build the list of triangles sharing the welded vertices
			Dictionary<int,List<Triangle>>	WeldedVertexIndex2SharedFaces = new Dictionary<int,List<Triangle>>();

			int TrianglesCount = IndicesCount/3;
			int CurrentVertexIndex = 0;
			for ( int TriangleIndex=0; TriangleIndex < TrianglesCount; TriangleIndex++ )
			{
				int	V0 = _Accessor.ToInt( _Indices[CurrentVertexIndex++] );
				int	V1 = _Accessor.ToInt( _Indices[CurrentVertexIndex++] );
				int	V2 = _Accessor.ToInt( _Indices[CurrentVertexIndex++] );
				int	WV0 = OriginalIndex2WeldedIndex[V0];
				int	WV1 = OriginalIndex2WeldedIndex[V1];
				int	WV2 = OriginalIndex2WeldedIndex[V2];

				if ( !WeldedVertexIndex2SharedFaces.ContainsKey( WV0 ) )
					WeldedVertexIndex2SharedFaces.Add( WV0, new List<Triangle>() );
				WeldedVertexIndex2SharedFaces[WV0].Add( new Triangle( V0, V1, V2, WV0, WV1, WV2 ) );

				if ( !WeldedVertexIndex2SharedFaces.ContainsKey( WV1 ) )
					WeldedVertexIndex2SharedFaces.Add( WV1, new List<Triangle>() );
				WeldedVertexIndex2SharedFaces[WV1].Add( new Triangle( V1, V2, V0, WV1, WV2, WV0 ) );

				if ( !WeldedVertexIndex2SharedFaces.ContainsKey( WV2 ) )
					WeldedVertexIndex2SharedFaces.Add( WV2, new List<Triangle>() );
				WeldedVertexIndex2SharedFaces[WV2].Add( new Triangle( V2, V0, V1, WV2, WV0, WV1 ) );
			}

			// =============================================
			// 3] Build the resulting list of adjacencies
			// Source : mk:@MSITStore:C:\Program%20Files%20(x86)\Microsoft%20DirectX%20SDK%20(June%202010)\Documentation\DirectX9\windows_graphics.chm::/direct3d10/d3d10_graphics_programming_guide_primitive_topologies.htm
			//
			_AdjacentIndices = new I[6*TrianglesCount];

			int	SourceVertexIndex = 0;
			int	DestVertexIndex = 0;
			for ( int TriangleIndex=0; TriangleIndex < TrianglesCount; TriangleIndex++ )
			{
				int	V0 = _Accessor.ToInt( _Indices[SourceVertexIndex++] );
				int	V1 = _Accessor.ToInt( _Indices[SourceVertexIndex++] );
				int	V2 = _Accessor.ToInt( _Indices[SourceVertexIndex++] );

				_AdjacentIndices[DestVertexIndex++] = _Accessor.FromInt( V0 );
				_AdjacentIndices[DestVertexIndex++] = _Accessor.FromInt( FindAdjacentVertex( V0, V1, OriginalIndex2WeldedIndex, WeldedVertexIndex2SharedFaces ) );
				_AdjacentIndices[DestVertexIndex++] = _Accessor.FromInt( V1 );
				_AdjacentIndices[DestVertexIndex++] = _Accessor.FromInt( FindAdjacentVertex( V1, V2, OriginalIndex2WeldedIndex, WeldedVertexIndex2SharedFaces ) );
				_AdjacentIndices[DestVertexIndex++] = _Accessor.FromInt( V2 );
				_AdjacentIndices[DestVertexIndex++] = _Accessor.FromInt( FindAdjacentVertex( V2, V0, OriginalIndex2WeldedIndex, WeldedVertexIndex2SharedFaces ) );
			}

			return false;
		}

		protected static Vector3	ReadPosition( VertexAccessor<VS> _Accessor, VS _Vertex )
		{
			return _Accessor.GetValue<Vector3>( _Vertex, SemanticAttribute.POSITION );
		}

		protected static int		FindAdjacentVertex( int _V0, int _V1, Dictionary<int,int> _OriginalIndex2WeldedIndex, Dictionary<int,List<Triangle>> _WeldedVertexIndex2SharedFaces )
		{
			// Find all the faces that share the welded V0 index
			int		WV0 = _OriginalIndex2WeldedIndex[_V0];
			int		NextWeldedVertex = _OriginalIndex2WeldedIndex[_V1];

			foreach ( Triangle T in _WeldedVertexIndex2SharedFaces[WV0] )
			{
				// The matching adjacent triangle should have its previous welded vertex equal our next welded vertex...
				if ( T.m_PreviousWeldedVertex == NextWeldedVertex )
					return	T.m_AdjacentVertex;	// This is our adjacent vertex !
			}

			return _V1;	// No adjacency
		}
	}
}
