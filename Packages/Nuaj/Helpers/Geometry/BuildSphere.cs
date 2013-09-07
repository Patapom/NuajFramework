using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	public class	Sphere<VS,I> : GeometryBuilder<VS,I> where VS:struct where I:struct
	{
		/// <summary>
		/// Builds a subdivided unit radius sphere
		/// </summary>
		/// <param name="_SubdivisionsCount">The amount of sphere subdivisions along the Theta angle (the sphere will be subdivided twice more on along the Phi angle)</param>
		/// <param name="_UVTiling">The tiling factor on U & V (by default, they tile once on the entire sphere) NOTE: U is mapped to the Phi angle while V is mapped to the Theta angle</param>
		/// <param name="_Writer">The accessor that is able to write to the vertex/index buffers</param>
		/// <param name="_VB">The resulting vertex buffer</param>
		/// <param name="_IB">The resulting index buffer</param>
		public static Primitive<VS,I>	Build( Device _Device, string _Name, int _SubdivisionsCount, Vector2 _UVTiling, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess )
		{
			return Build( _Device, _Name, _SubdivisionsCount, _UVTiling, _Writer, _PostProcess, false, 0.0f );
		}

		public static Primitive<VS,I>	Build( Device _Device, string _Name, int _SubdivisionsCount, Vector2 _UVTiling, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess, bool _bBuildAdjacency, float _fVertexThreshold )
		{
			int		PhiSubdivisionsCount = 2 * _SubdivisionsCount;
			int		ThetaSubdivisionsCount = 1+_SubdivisionsCount+1;

			int		VerticesCount = (PhiSubdivisionsCount+1) * ThetaSubdivisionsCount;
			int		IndicesCount =	3 * (PhiSubdivisionsCount *
									(1 +						// Triangles for top band
									2*(_SubdivisionsCount-1) +	// Quads for main bands
									1));						// Triangles for bottom bands

			VS[]	Vertices = new VS[VerticesCount];
			I[]		Indices = new I[IndicesCount];

			int		VertexIndex = 0;
			int		IndexIndex = 0;

			// Build the vertices
			for ( int ThetaIndex=0; ThetaIndex < ThetaSubdivisionsCount; ThetaIndex++ )
			{
				float	fTheta = ThetaIndex * (float) Math.PI / (ThetaSubdivisionsCount - 1);
				for ( int PhiIndex=0; PhiIndex <= PhiSubdivisionsCount; PhiIndex++ )
				{
					float	fPhi = PhiIndex * (float) Math.PI / _SubdivisionsCount;

					Vector3	Normal = new Vector3( (float) (Math.Sin( fPhi ) * Math.Sin( fTheta )), (float) Math.Cos( fTheta ), (float) (Math.Cos( fPhi ) * Math.Sin( fTheta )) );
					Vector3	Tangent = Vector3.Cross( Vector3.UnitY, Normal );
							Tangent.Normalize();
					Vector3	BiTangent = Vector3.Cross( Normal, Tangent );

					_Writer.WriteVertexData( ref Vertices[VertexIndex++], Normal, Normal, Tangent, BiTangent, new Vector3( _UVTiling.X * 0.5f * fPhi / (float) Math.PI, _UVTiling.Y * (1.0f - fTheta / (float) Math.PI), 0.0f ), new Color4() );
				}
			}

			// Build the indices
			int		BandStride = PhiSubdivisionsCount+1;
			for ( int PhiIndex=0; PhiIndex < PhiSubdivisionsCount; PhiIndex++ )
			{
				// Top band of triangles
				_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*0 + PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*1 + PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*1 + PhiIndex+1 );

				// Main bands of quads
				for ( int ThetaIndex=1; ThetaIndex < ThetaSubdivisionsCount-2; ThetaIndex++ )
				{
					_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaIndex+0) + PhiIndex );
					_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaIndex+1) + PhiIndex );
					_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaIndex+1) + PhiIndex+1 );

					_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaIndex+0) + PhiIndex );
					_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaIndex+1) + PhiIndex+1 );
					_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaIndex+0) + PhiIndex+1 );
			}

				// Bottom band of triangles
				_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaSubdivisionsCount-2) + PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaSubdivisionsCount-1) + PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], BandStride*(ThetaSubdivisionsCount-2) + PhiIndex+1 );
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
	}
}
