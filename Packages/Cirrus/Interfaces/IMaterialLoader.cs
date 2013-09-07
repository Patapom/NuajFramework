using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// An interface that should be implemented by any object that is able to load materials.
	/// This interface is useful to abstract the material loading from any particular source.
	/// Usually, the implementer also implements the IFileLoader interface that will load the material shader.
	/// </summary>
	/// <remarks>The interface implementer should be responsible for disposing of the material</remarks>
	public interface IMaterialLoader
	{
		/// <summary>
		/// Loads a material
		/// </summary>
		/// <typeparam name="VS">The Vertex Structure the material should be compiled against</typeparam>
		/// <param name="_Name">The name of the material to create</param>
		/// <param name="_SM">The shader model the material is using</param>
		/// <param name="_FileName">The name of the material file</param>
		/// <returns>The requested material</returns>
		/// <remarks>Note the material will be disposed of by the IMaterialProvider implementer !</remarks>
		Material<VS>	LoadMaterial<VS>( string _Name, ShaderModel _SM, System.IO.FileInfo _FileName ) where VS:struct;
	}
}
