using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	public enum	VERTEX_FIELD_TYPE
	{
		UNKNOWN,
		FLOAT,
		FLOAT2,
		FLOAT3,
		FLOAT4,
		COLOR3,
		COLOR4,
	};

	public enum	VERTEX_FIELD_USAGE
	{
		UNKNOWN,
		POSITION,
		NORMAL,
		TANGENT,
		BITANGENT,
		COLOR,
		DIFFUSE,
		SPECULAR,
		TEX_COORD2D,
		TEX_COORD3D,
		TEX_COORD4D,
	}

	/// <summary>
	/// This interface helps to declare and recognize specific vertex formats : it serves as a connector
	/// between "abstract meshes" that only declare the informations they have per vertex and the render
	/// techniques, that publish the signatures they are able to support.
	/// 
	/// The render technique that recognizes the most vertex fields gets the primitive and is then responsible for it.
	/// </summary>
	public interface	IVertexSignature
	{
		/// <summary>
		/// Gets the amount of fields available on the vertex
		/// </summary>
		int					VertexFieldsCount		{ get; }

		/// <summary>
		/// Gets the usage of a vertex field
		/// </summary>
		/// <param name="_FieldIndex"></param>
		/// <returns></returns>
		VERTEX_FIELD_USAGE	GetVertexFieldUsage( int _FieldIndex );

		/// <summary>
		/// Gets the type of a vertex field
		/// </summary>
		/// <param name="_FieldIndex"></param>
		/// <returns></returns>
		VERTEX_FIELD_TYPE	GetVertexFieldType( int _FieldIndex );

		/// <summary>
		/// Gets the name of a vertex field (usually empty but we can sometimes have better information and be even more precise in our matching)
		/// </summary>
		/// <param name="_FieldIndex"></param>
		/// <returns></returns>
		string				GetVertexFieldName( int _FieldIndex );

		/// <summary>
		/// Gets the index of a vertex field
		/// </summary>
		/// <param name="_FieldIndex"></param>
		/// <returns></returns>
		int					GetVertexFieldIndex( int _FieldIndex );

		/// <summary>
		/// Compares our signature with the provided signatures and returns true if the provided signature can be used to fill all of the fields in our signature
		/// </summary>
		/// <param name="_Signature"></param>
		/// <returns>True if all the fields of our signature can be filled from fields of the provided signature</returns>
		bool				CheckMatch( IVertexSignature _Signature );

		/// <summary>
		/// Creates a map that will map the fields in the provided signature to the fields in our own signature
		/// </summary>
		/// <param name="_Signature">The signature to map</param>
		/// <returns>A map that will give the index in the provided signature to get a field from our own signature or null if no complete match can be found</returns>
		/// <example>You can then use the map like this :
		/// int OtherFieldIndex = GetVertexFieldsMap( IVertexSignature _Signature )[OurFieldIndex];
		/// Copy OtherData[OtherFieldIndex] to OurData[OurFieldIndex]
		/// </example>
		Dictionary<int,int>	GetVertexFieldsMap( IVertexSignature _Signature );
	}
}
