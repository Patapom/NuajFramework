using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This should be implemented by geometry providers that wish to create a primitive using a render technique
	/// </summary>
	public interface	IIndexProvider
	{
		/// <summary>
		/// Gets the index at the given index.
		/// </summary>
		/// <param name="_TriangleIndex">The index of the triangle whose vertex index we need</param>
		/// <param name="_TriangleVertexIndex">The index of the vertex index to get in [0,2]</param>
		/// <returns></returns>
		int		GetIndex( int _TriangleIndex, int _TriangleVertexIndex );
	}
}
