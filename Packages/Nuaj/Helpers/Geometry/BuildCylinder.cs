using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	public class	Cylinder<VS,I> : GeometryBuilder<VS,I> where VS:struct where I:struct
	{
		/// <summary>
		/// Builds a subdivided unit radius cylinder of height 2
		/// </summary>
		/// <param name="_SubdivisionsCount">The amount of sphere subdivisions along the Theta angle (the sphere will be subdivided twice more on along the Phi angle)</param>
		/// <param name="_UVTiling">The tiling factor on U & V (by default, they tile once on the entire sphere) NOTE: U is mapped to the Phi angle while V is mapped to the Theta angle</param>
		/// <param name="_Writer">The accessor that is able to write to the vertex/index buffers</param>
		/// <param name="_VB">The resulting vertex buffer</param>
		/// <param name="_IB">The resulting index buffer</param>
		public static Primitive<VS,I>	Build( Device _Device, string _Name, int _SubdivisionsCount, GeometryMapper _Mapper, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess )			
		{
			return Build( _Device, _Name, _SubdivisionsCount, _Mapper, _Writer, _PostProcess, false, 0.0f );
		}

		public static Primitive<VS,I>	Build( Device _Device, string _Name, int _SubdivisionsCount, GeometryMapper _Mapper, IGeometryWriter<VS,I> _Writer, PostProcessMeshDelegate _PostProcess, bool _bBuildAdjacency, float _fVertexThreshold )
		{
			int	VerticesCount = 1 + (1+2+1)*_SubdivisionsCount + 2 + 1;
			int	IndicesCount = 3 * (_SubdivisionsCount + 2*(_SubdivisionsCount+1) + _SubdivisionsCount);

			VS[]	Vertices = new VS[VerticesCount];
			I[]		Indices = new I[IndicesCount];

			int		VertexIndex = 0;
			int		IndexIndex = 0;

			Vector3	Position, Normal;
			Vector3	Tangent = new Vector3();
			Vector3	BiTangent = new Vector3();
			Vector3	UVW = new Vector3();

			// Build vertices
				// Top cap
			Position = new Vector3( 0.0f, +1.0f, 0.0f );
			Normal = new Vector3( 0.0f, +1.0f, 0.0f );
			_Mapper.BuildUVW( Position, Normal, Normal, ref Tangent, ref BiTangent, ref UVW, false );
			_Writer.WriteVertexData( ref Vertices[VertexIndex++], Position, Normal, Tangent, BiTangent, UVW, new Color4() );
			for ( int PhiIndex=0; PhiIndex < _SubdivisionsCount; PhiIndex++ )
			{
				float	fPhi = PhiIndex * 2.0f * (float) Math.PI / _SubdivisionsCount;
				Position = new Vector3( (float) Math.Sin( fPhi ), 1.0f, (float) Math.Cos( fPhi ) );
				_Mapper.BuildUVW( Position, Normal, Normal, ref Tangent, ref BiTangent, ref UVW, false);
				_Writer.WriteVertexData( ref Vertices[VertexIndex++], Position, Normal, Tangent, BiTangent, UVW , new Color4() );
			}

				// Main
			for ( int PhiIndex=0; PhiIndex <= _SubdivisionsCount; PhiIndex++ )
			{
				float	fPhi = PhiIndex * 2.0f * (float) Math.PI / _SubdivisionsCount;
				Position = new Vector3( (float) Math.Sin( fPhi ), 1.0f, (float) Math.Cos( fPhi ) );
				Normal = new Vector3( (float) Math.Sin( fPhi ), 0.0f, (float) Math.Cos( fPhi ) );
				_Mapper.BuildUVW( Position, Normal, Normal, ref Tangent, ref BiTangent, ref UVW, PhiIndex==_SubdivisionsCount );
				_Writer.WriteVertexData( ref Vertices[VertexIndex++], Position, Normal, Tangent, BiTangent, UVW, new Color4() );
			}
			for ( int PhiIndex=0; PhiIndex <= _SubdivisionsCount; PhiIndex++ )
			{
				float	fPhi = PhiIndex * 2.0f * (float) Math.PI / _SubdivisionsCount;
				Position = new Vector3( (float) Math.Sin( fPhi ), -1.0f, (float) Math.Cos( fPhi ) );
				Normal = new Vector3( (float) Math.Sin( fPhi ), 0.0f, (float) Math.Cos( fPhi ) );
				_Mapper.BuildUVW( Position, Normal, Normal, ref Tangent, ref BiTangent, ref UVW, PhiIndex==_SubdivisionsCount );
				_Writer.WriteVertexData( ref Vertices[VertexIndex++], Position, Normal, Tangent, BiTangent, UVW, new Color4() );
			}

				// Bottom cap
			Normal = new Vector3( 0.0f, -1.0f, 0.0f );
			for ( int PhiIndex=0; PhiIndex < _SubdivisionsCount; PhiIndex++ )
			{
				float	fPhi = PhiIndex * 2.0f * (float) Math.PI / _SubdivisionsCount;
				Position = new Vector3( (float) Math.Sin( fPhi ), -1.0f, (float) Math.Cos( fPhi ) );
				_Mapper.BuildUVW( Position, Normal, Normal, ref Tangent, ref BiTangent, ref UVW, false );
				_Writer.WriteVertexData( ref Vertices[VertexIndex++], Position, Normal, Tangent, BiTangent, UVW, new Color4() );
			}
			Position = new Vector3( 0.0f, -1.0f, 0.0f );
			_Mapper.BuildUVW( Position, Normal, Normal, ref Tangent, ref BiTangent, ref UVW, false );
			_Writer.WriteVertexData( ref Vertices[VertexIndex++], Position, Normal, Tangent, BiTangent, UVW, new Color4() );

			// Build indices

				// Top singular band
			for ( int PhiIndex=0; PhiIndex < _SubdivisionsCount; PhiIndex++ )
			{
				_Writer.WriteIndexData( ref Indices[IndexIndex++], 0 );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], 1+PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], 1+((PhiIndex+1) % _SubdivisionsCount) );
			}

				// Main band
			int	Offset = 1+_SubdivisionsCount;
			int BandSubdivisionsCount = _SubdivisionsCount+1;
			for ( int PhiIndex=0; PhiIndex <= _SubdivisionsCount; PhiIndex++ )
			{
				_Writer.WriteIndexData( ref Indices[IndexIndex++], Offset+PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], Offset+BandSubdivisionsCount+((PhiIndex+0) % BandSubdivisionsCount) );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], Offset+BandSubdivisionsCount+((PhiIndex+1) % BandSubdivisionsCount) );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], Offset+PhiIndex );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], Offset+BandSubdivisionsCount+((PhiIndex+1) % BandSubdivisionsCount) );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], Offset+((PhiIndex+1) % BandSubdivisionsCount) );
			}

				// Bottom singular band
			for ( int PhiIndex=0; PhiIndex < _SubdivisionsCount; PhiIndex++ )
			{
				_Writer.WriteIndexData( ref Indices[IndexIndex++], VerticesCount-1 );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], VerticesCount-1-_SubdivisionsCount+((PhiIndex+1) % _SubdivisionsCount) );
				_Writer.WriteIndexData( ref Indices[IndexIndex++], VerticesCount-1-_SubdivisionsCount+PhiIndex );
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
