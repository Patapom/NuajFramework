using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	public delegate void	PrimitiveCollectionChangedEventHandler( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive );

	/// <summary>
	/// This is the interface to render techniques that support creation and rendering of external objects.
	/// Such render techniques are responsible for :
	///		_ Publishing a supported vertex signature
	///		_ Creating and registering primitives either from Vertex/Index providers, or from a stream
	///		_ Saving primitives to a stream
	/// </summary>
	/// <example>Check the Nuaj.Cirrus.RenderTechniqueDefault implementation that supports the interface.</example>
	public interface	ITechniqueSupportsObjects
	{
		/// <summary>
		/// The (hopefully) unique name of the technique that will be used to retrieve it when de-serializing primitive data
		/// </summary>
		string					Name					{ get; }

		/// <summary>
		/// Gets the vertex signature this technique can recognize and process
		/// </summary>
		IVertexSignature		RecognizedSignature		{ get; }

		/// <summary>
		/// Gets the main material used by the technique
		/// </summary>
		IMaterial				MainMaterial			{ get; }

		/// <summary>
		/// Creates and registers a Cirrus.Scene primitive that uses that technique
		/// </summary>
		/// <param name="_Parent">The parent mesh for the primitive</param>
		/// <param name="_Name">The name of the primitive to create</param>
		/// <param name="_Signature">The vertex signature for the vertices that are provided</param>
		/// <param name="_VerticesCount">The amount of provided vertices</param>
		/// <param name="_VertexFieldProvider">The vertex provider interface</param>
		/// <param name="_IndicesCount">The amount of provided indices (use 0 for non-indexed triangle lists and -1 for triangle strips)</param>
		/// <param name="_IndexProvider">The index provider interface</param>
		/// <param name="_Parameters">The material parameters associated to the primitive</param>
		/// <returns>The Cirrus primitive that was created and registered by the technique</returns>
		Scene.Mesh.Primitive	CreatePrimitive( Scene.Mesh _Parent, string _Name, IVertexSignature _Signature, int _VerticesCount, IVertexFieldProvider _VertexFieldProvider, int _IndicesCount, IIndexProvider _IndexProvider, Scene.MaterialParameters _Parameters );

		/// <summary>
		/// Creates a primitive from a stream
		/// </summary>
		/// <param name="_Parent">The parent mesh that owns the primitive</param>
		/// <param name="_Reader"></param>
		/// <returns>The loaded primitive</returns>
		Scene.Mesh.Primitive	CreatePrimitive( Scene.Mesh _Parent, System.IO.BinaryReader _Reader );

		/// <summary>
		/// Saves a primitive to a stream
		/// </summary>
		/// <param name="_Primitive"></param>
		/// <param name="_Writer"></param>
		void					SavePrimitive( Scene.Mesh.Primitive _Primitive, System.IO.BinaryWriter _Writer );

		/// <summary>
		/// Removes an existing primitive
		/// </summary>
		/// <param name="_Primitive"></param>
		void					RemovePrimitive( Scene.Mesh.Primitive _Primitive );

		/// <summary>
		/// Occurs when the technique supports a new primitive (after CreatePrimitive)
		/// </summary>
		event PrimitiveCollectionChangedEventHandler	PrimitiveAdded;

		/// <summary>
		/// Occurs when a primitive is removed from the the technique (after RemovePrimitive)
		/// </summary>
		event PrimitiveCollectionChangedEventHandler	PrimitiveRemoved;
	}
}
