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
	/// Interface to primitive objects
	/// A call to renderable objects' "RenderOverride()" method should not setup any material or state but plainly render any stored primitive
	/// </summary>
	public interface IPrimitive : IComponent
	{
		/// <summary>
		/// Renders the primitive using its associated material
		/// </summary>
		void		Render();

		/// <summary>
		/// Renders the primitive using the currently set material
		/// </summary>
		/// <remarks>The primitive can be assigned a material of its own but using this method will not use the primitive's material, rather the currently used material</remarks>
		void		RenderOverride();
	}
}
