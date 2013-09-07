using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj
{
	/// <summary>
	/// This is the interface to implement to write geometry data provided by the geometry builders
	/// </summary>
	public interface IGeometryWriter<VS,I>  where VS:struct  where I:struct
	{
		/// <summary>
		/// Called to write vertex data into the provided vertex
		/// </summary>
		/// <param name="_Vertex"></param>
		/// <param name="_Position"></param>
		/// <param name="_Normal"></param>
		/// <param name="_Tangent"></param>
		/// <param name="_BiTangent"></param>
		/// <param name="_UVW"></param>
		/// <param name="_Color"></param>
		void	WriteVertexData( ref VS _Vertex, Vector3 _Position, Vector3 _Normal, Vector3 _Tangent, Vector3 _BiTangent, Vector3 _UVW, Color4 _Color );

		/// <summary>
		/// Called to write index data into the provided index
		/// </summary>
		/// <param name="_Index"></param>
		/// <param name="_Value"></param>
		void	WriteIndexData( ref I _Index, int _Value );

		/// <summary>
		/// Called to read index data
		/// </summary>
		/// <param name="_Index"></param>
		/// <returns></returns>
		int		ReadIndexData( I _Index );
	}
}
