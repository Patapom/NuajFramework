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
	/// This is the interface to pixel format structures needed for images and textures
	/// </summary>
	public interface IShaderInterfaceProvider
	{
		/// <summary>
		/// Called when an object needs to be provided with the data this provider has
		/// </summary>
		/// <param name="_Interface">The shader interface needing data</param>
		void				ProvideData( IShaderInterface _Interface );
	}
}
