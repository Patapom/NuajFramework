using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This is the base mesh primitive builder class that helps you to easily create mesh primitives
	/// All you have to do is derive that class and :
	/// 1) Implement the IVertexFieldProvider interface that will return the fields in your vertex structure
	/// 2) In the constructor :
	///  . Create your vertex signature using m_VertexSignature.AddField()
	///  . Build the list of vertices and indices for your primitive by filling up the m_Vertices & m_Indices arrays
	/// 
	/// 
	/// Then, you simply call "BuildPrimitive()" using the appropriate RenderTechnique and Scene.MaterialParameters for the primitive.
	/// You are returned a Scene.Mesh.Primitive you can add to your Scene.Mesh
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class		MeshPrimitiveBuilderBase<VS> : IVertexFieldProvider, IIndexProvider where VS:struct
	{
		#region FIELDS

		protected DynamicVertexSignature	m_VertexSignature = new DynamicVertexSignature();
		protected List<VS>					m_Vertices = new List<VS>();
		protected List<int>					m_Indices = new List<int>();

		#endregion

		#region PROPERTIES

		public IVertexSignature		VertexSignature	{ get { return m_VertexSignature; } }
		public int					VerticesCount	{ get { return m_Vertices.Count; } }
		public int					IndicesCount	{ get { return m_Indices.Count; } }

		#endregion

		#region METHODS

		public	MeshPrimitiveBuilderBase()
		{
			// TODO :
			// . Create your vertex signature using m_VertexSignature.AddField()
			// . Build the list of vertices and indices for your primitive by filling up the m_Vertices & m_Indices arrays
		}

		/// <summary>
		/// Builds a mesh primitive with the given material parameters
		/// </summary>
		/// <param name="_RenderTechnique">The render technique that will create and render the primitive</param>
		/// <param name="_Parent">The parent mesh hosting the primitive</param>
		/// <param name="_PrimitiveName">The name to give to the primitive</param>
		/// <param name="_MaterialParameters">The material parameters associated to the primitive</param>
		/// <returns></returns>
		public Scene.Mesh.Primitive	BuildPrimitive( ITechniqueSupportsObjects _RenderTechnique, Scene.Mesh _Parent, string _PrimitiveName, Scene.MaterialParameters _MaterialParameters )
		{
			return _RenderTechnique.CreatePrimitive( _Parent, _PrimitiveName, m_VertexSignature, m_Vertices.Count, this, m_Indices.Count, this, _MaterialParameters );
		}

		#region IVertexFieldProvider Members

		public abstract object GetField( int _VertexIndex, int _FieldIndex );

		#endregion

		#region IIndexProvider Members

		public int GetIndex( int _TriangleIndex, int _TriangleVertexIndex )
		{
			return m_Indices[3*_TriangleIndex+_TriangleVertexIndex];
		}

		#endregion

		#endregion
	}
}
