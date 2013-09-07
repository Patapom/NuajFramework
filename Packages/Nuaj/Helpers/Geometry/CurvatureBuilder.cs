using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	///////////////////////////
	// DOESN'T WORK AT ALL ! DAMN SHIT §
	///////////////////////////

	/// <summary>
	/// This class helps to build the tangent space curvature from a given mesh
	/// The requirements are that the vertex structure has the POSITION, NORMAL, TANGENT, BITANGENT and CURVATURE semantics
	/// </summary>
	/// <typeparam name="VS"></typeparam>
	/// <typeparam name="I"></typeparam>
	public class	Curvature<VS,I> where VS:struct where I:struct
	{
		public static void	BuildCurvature( VS[] _Vertices, I[] _Indices, IIndexAccessor<I> _Accessor )
		{
			VertexAccessor<VS>	Accessor = new VertexAccessor<VS>();
			int	IndicesCount = _Indices.Length;
			int	TrianglesCount = IndicesCount / 3;

			// =============================================
			// 1] Build the list of shared edges
			Dictionary<int,Dictionary<int,int>>	Index2SharedEdges = new Dictionary<int,Dictionary<int,int>>();
			for ( int TriangleIndex=0; TriangleIndex < TrianglesCount; TriangleIndex++ )
			{
				int	V0 = _Accessor.ToInt( _Indices[3*TriangleIndex+0] );
				int	V1 = _Accessor.ToInt( _Indices[3*TriangleIndex+1] );
				int	V2 = _Accessor.ToInt( _Indices[3*TriangleIndex+2] );

				if ( !Index2SharedEdges.ContainsKey( V0 ) )
					Index2SharedEdges.Add( V0, new Dictionary<int,int>() );
				Index2SharedEdges[V0][V1] = V1;
				Index2SharedEdges[V0][V2] = V2;

				if ( !Index2SharedEdges.ContainsKey( V1 ) )
					Index2SharedEdges.Add( V1, new Dictionary<int,int>() );
				Index2SharedEdges[V1][V0] = V0;
				Index2SharedEdges[V1][V2] = V2;

				if ( !Index2SharedEdges.ContainsKey( V2 ) )
					Index2SharedEdges.Add( V2, new Dictionary<int,int>() );
				Index2SharedEdges[V2][V0] = V0;
				Index2SharedEdges[V2][V1] = V1;
			}

			// =============================================
			// 2] Build the curvatures
			for ( int VertexIndex=0; VertexIndex < _Vertices.Length; VertexIndex++ )
			{
				if ( !Index2SharedEdges.ContainsKey( VertexIndex ) )
					continue;	// Unused vertex ?

				// Read our reference tangent space data
				Vector3	Position0 = ReadPosition( Accessor, _Vertices[VertexIndex] );
				Vector3	Tangent0 = ReadTangent( Accessor, _Vertices[VertexIndex] );
				Tangent0.Normalize();
				Vector3	BiTangent0 = ReadBiTangent( Accessor, _Vertices[VertexIndex] );
				BiTangent0.Normalize();
				Vector3	Normal0 = ReadNormal( Accessor, _Vertices[VertexIndex] );
				Normal0.Normalize();

				// Accumulate curvature from all surrounding edges
				Dictionary<int,int>.ValueCollection	SharedEdges = Index2SharedEdges[VertexIndex].Values;// This is the list of edges that share that vertex

				float	fSumCurvatureT = 0.0f;
				float	fSumCurvatureB = 0.0f;
				foreach ( int OtherVertexIndex in SharedEdges )
				{
					// Read position and normal
					Vector3	Position1 = ReadPosition( Accessor, _Vertices[OtherVertexIndex] );
					Vector3	Normal1 = ReadNormal( Accessor, _Vertices[OtherVertexIndex] );
					Normal1.Normalize();

					Vector3	ToOtherVertex = Position1 - Position0;

					// Project position along tangent and compute curvature
					float	fTangentProjection = Vector3.Dot( ToOtherVertex, Tangent0 );
					float	fDotNormalsT = Vector3.Dot( Normal0, Normal1 );
					float	fSinT = (float) Math.Sqrt( 1.0 - fDotNormalsT*fDotNormalsT );
					fSumCurvatureT += Math.Abs( fSinT ) > 1e-4f ? fTangentProjection / fSinT : 1e4f;

					// Project position along bitangent and compute curvature
					float	fBiTangentProjection = Vector3.Dot( ToOtherVertex, BiTangent0 );
					float	fDotNormalsB = Vector3.Dot( Normal0, Normal1 );
					float	fSinB = (float) Math.Sqrt( 1.0 - fDotNormalsB*fDotNormalsB );
					fSumCurvatureB += Math.Abs( fSinB ) > 1e-4f ? fBiTangentProjection / fSinB : 1e4f;
				}

				if ( SharedEdges.Count > 1 )
				{
					fSumCurvatureT /= SharedEdges.Count;
					fSumCurvatureB /= SharedEdges.Count;
				}

				// Write curvature for this vertex
				_Vertices[VertexIndex] = WriteCurvature( Accessor, _Vertices[VertexIndex], new Vector2( fSumCurvatureT, fSumCurvatureB ) );
			}
		}

		protected static Vector3	ReadPosition( VertexAccessor<VS> _Accessor, VS _Vertex )
		{
			return _Accessor.GetValue<Vector3>( _Vertex, SemanticAttribute.POSITION );
		}

		protected static Vector3	ReadTangent( VertexAccessor<VS> _Accessor, VS _Vertex )
		{
			return _Accessor.GetValue<Vector3>( _Vertex, SemanticAttribute.TANGENT );
		}

		protected static Vector3	ReadNormal( VertexAccessor<VS> _Accessor, VS _Vertex )
		{
			return _Accessor.GetValue<Vector3>( _Vertex, SemanticAttribute.NORMAL );
		}

		protected static Vector3	ReadBiTangent( VertexAccessor<VS> _Accessor, VS _Vertex )
		{
			return _Accessor.GetValue<Vector3>( _Vertex, SemanticAttribute.BITANGENT );
		}

		protected static VS		WriteCurvature( VertexAccessor<VS> _Accessor, VS _Vertex, Vector2 _Curvature )
		{
			return _Accessor.SetValue<Vector2>( _Vertex, SemanticAttribute.CURVATURE, _Curvature );
		}
	}
}
