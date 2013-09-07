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
	/// This is the interface to 3D render targets
	/// </summary>
	public interface IRenderTarget3D : ITexture3D
	{
		/// <summary>
		/// Gets the render target that can be bound to the output merger
		/// </summary>
		RenderTargetView		RenderTargetView	{ get; }

		/// <summary>
		/// Gets a single render target view at the specified index in the mip hierarchy
		/// The render target view contains a single render target element at the specified mip level
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the render target was created with mip maps, otherwise use 0)</param>
		RenderTargetView		GetSingleRenderTargetView( int _MipLevelIndex );
	}
}
