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
	public interface	IVertexFieldProvider
	{
		/// <summary>
		/// Gets the field at the given index. The index corresponds to the field enumeration in the corresponding IVertexSignature
		/// </summary>
		/// <typeparam name="T">The required field type</typeparam>
		/// <param name="_VertexIndex">The index of the vertex to get the field of</param>
		/// <param name="_FieldIndex">The index of the field to get</param>
		/// <returns></returns>
		object	GetField( int _VertexIndex, int _FieldIndex );
	}
}
