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
	/// This is the interface to depth stencil buffers
	/// </summary>
	public interface IDepthStencil : ITexture2D
	{
		/// <summary>
		/// Gets the depth stencil view that can be bound to the output merger
		/// </summary>
		DepthStencilView		DepthStencilView	{ get; }
	}
}
