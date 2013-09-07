using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	public class	Cube<VS,I> : GeometryBuilder<VS,I> where VS:struct where I:struct
	{
		/// <summary>
		/// Builds a cube of 2x2x2
		/// </summary>
		/// <param name="_SubdivisionsCount">The amount of sphere subdivisions along the Theta angle (the sphere will be subdivided twice more on along the Phi angle)</param>
		/// <param name="_UVTiling">The tiling factor on U and V (by default, they tile once on the entire cube)</param>
		/// <param name="_Writer">The accessor that is able to write to the vertex/index buffers</param>
		/// <param name="_bInvert">Inverts the cube, making it point inward and display inside faces</param>
		/// <param name="_VB">The resulting vertex buffer</param>
		/// <param name="_IB">The resulting index buffer</param>
		public static Primitive<VS,I>	Build( Device _Device, string _Name, Vector2 _UVTiling, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess, bool _bInvert )
		{
			return Build( _Device, _Name, _UVTiling, _Writer, _PostProcess, _bInvert, false, 0.0f );
		}

		public static Primitive<VS,I>	Build( Device _Device, string _Name, Vector2 _UVTiling, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess, bool _bInvert, bool _bBuildAdjacency, float _fVertexThreshold )
		{
			int	FacesCount = 6;
			int	VerticesCount = FacesCount*4;
			int	IndicesCount = 3 * 2*FacesCount;

			VS[]	Vertices = new VS[VerticesCount];
			I[]		Indices = new I[IndicesCount];

			Vector3[]	FaceNormals = new Vector3[]
			{
				new Vector3( -1.0f, 0.0f, 0.0f ),	// Left (-X)
				new Vector3( +1.0f, 0.0f, 0.0f ),	// Right (+X)
				new Vector3( 0.0f, -1.0f, 0.0f ),	// Bottom (-Y)
				new Vector3( 0.0f, 1.0f, 0.0f ),	// Top (+Y)
				new Vector3( 0.0f, 0.0f, -1.0f ),	// Back (-Z)
				new Vector3( 0.0f, 0.0f, 1.0f ),	// Front (+Z)
			};

			// The face tangents will also indicate the U direction
			Vector3[]	FaceTangents = new Vector3[]
			{
				new Vector3( 0.0f, 0.0f, +1.0f ),	// Left
				new Vector3( 0.0f, 0.0f, -1.0f ),	// Right
				new Vector3( 1.0f, 0.0f, 0.0f ),	// Bottom
				new Vector3( 1.0f, 0.0f, 0.0f ),	// Top
				new Vector3( -1.0f, 0.0f, 0.0f ),	// Back
				new Vector3( 1.0f, 0.0f, 0.0f ),	// Front
			};

			// The UV Offsets/Factors packed into Vector4s
			Vector4[]	UVOffsetFactors = new Vector4[]
			{
				new Vector4( 0.0f, +1.0f, 0.0f, +1.0f ),	// Left		(U=+Z V=+Y)
				new Vector4( 1.0f, -1.0f, 0.0f, +1.0f ),	// Right	(U=+Z V=+Y)
				new Vector4( 0.0f, +1.0f, 0.0f, +1.0f ),	// Bottom	(U=+X V=+Z)
				new Vector4( 0.0f, +1.0f, 1.0f, -1.0f ),	// Top		(U=+X V=+Z)
				new Vector4( 1.0f, -1.0f, 0.0f, +1.0f ),	// Back		(U=+X V=+Y)
				new Vector4( 0.0f, +1.0f, 0.0f, +1.0f ),	// Front	(U=+X V=+Y)
			};

			for ( int FaceIndex=0; FaceIndex < FacesCount; FaceIndex++ )
			{
				Vector3	Normal = FaceNormals[FaceIndex];
				Vector3	Tangent = FaceTangents[FaceIndex];
				Vector3	BiTangent = Vector3.Cross( Normal, Tangent );

				float	NormalFactor = _bInvert ? -1.0f : 1.0f;

				// Issue 4 vertices
				_Writer.WriteVertexData( ref Vertices[4*FaceIndex+0], Normal - Tangent + BiTangent, NormalFactor * Normal, Tangent, BiTangent,
					new Vector3( UVOffsetFactors[FaceIndex].X + UVOffsetFactors[FaceIndex].Y * 0.0f, UVOffsetFactors[FaceIndex].Z + UVOffsetFactors[FaceIndex].W * _UVTiling.Y, 0.0f ),
					(Color4) System.Drawing.Color.Black );
				_Writer.WriteVertexData( ref Vertices[4*FaceIndex+1], Normal - Tangent - BiTangent, NormalFactor * Normal, Tangent, BiTangent
					, new Vector3( UVOffsetFactors[FaceIndex].X + UVOffsetFactors[FaceIndex].Y * 0.0f, UVOffsetFactors[FaceIndex].Z + UVOffsetFactors[FaceIndex].W * 0.0f, 0.0f ),
					(Color4) System.Drawing.Color.Black );
				_Writer.WriteVertexData( ref Vertices[4*FaceIndex+2], Normal + Tangent + BiTangent, NormalFactor * Normal, Tangent, BiTangent,
					new Vector3( UVOffsetFactors[FaceIndex].X + UVOffsetFactors[FaceIndex].Y * _UVTiling.X, UVOffsetFactors[FaceIndex].Z + UVOffsetFactors[FaceIndex].W * _UVTiling.Y, 0.0f ),
					(Color4) System.Drawing.Color.Black );
				_Writer.WriteVertexData( ref Vertices[4*FaceIndex+3], Normal + Tangent - BiTangent, NormalFactor * Normal, Tangent, BiTangent,
					new Vector3( UVOffsetFactors[FaceIndex].X + UVOffsetFactors[FaceIndex].Y * _UVTiling.X, UVOffsetFactors[FaceIndex].Z + UVOffsetFactors[FaceIndex].W * 0.0f,  0.0f ),
					(Color4) System.Drawing.Color.Black );

				// Issue 6 indices
				if ( !_bInvert )
				{
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 0) + 0], 4 * FaceIndex + 0 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 0) + 1], 4 * FaceIndex + 1 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 0) + 2], 4 * FaceIndex + 2 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 1) + 0], 4 * FaceIndex + 1 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 1) + 1], 4 * FaceIndex + 3 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 1) + 2], 4 * FaceIndex + 2 );
				}
				else
				{
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 0) + 0], 4 * FaceIndex + 2 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 0) + 1], 4 * FaceIndex + 1 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 0) + 2], 4 * FaceIndex + 0 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 1) + 0], 4 * FaceIndex + 2 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 1) + 1], 4 * FaceIndex + 3 );
					_Writer.WriteIndexData( ref Indices[3 * (2 * FaceIndex + 1) + 2], 4 * FaceIndex + 1 );
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
	}
}
