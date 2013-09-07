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
	/// This is the interface to render targets
	/// </summary>
	/// <remarks>Contrary to textures, there is no "GetMipBandRenderTargetView()" equivalent simply
	/// because you cannot render to multiple render targets if they don't have the same size</remarks>
	public interface IRenderTarget : ITexture2D
	{
		/// <summary>
		/// Gets the render target that can be bound to the output merger
		/// </summary>
		RenderTargetView		RenderTargetView	{ get; }

		/// <summary>
		/// Gets a single render target view at the specified index in the mip hierarchy and in the array of render targets
		/// The render target view contains a single render target element at the specified mip level and array index
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the render target was created with mip maps, otherwise use 0)</param>
		/// <param name="_ArrayIndex">The index in the array of render targets (valid only if the render target was created as an array of render targets, otherwise use 0)</param>
		/// <example>Here is what the view covers with _MipLevelIndex=1 and _ArrayIndex=1
		/// 
		///        Array0 Array1 Array2
		///       ______________________
		///  Mip0 |      |      |      |
		///       |------+------+------|
		///  Mip1 |      |  X   |      |
		///       |------+------+------|
		///  Mip2 |      |      |      |
		///       ----------------------
		/// </example>
		RenderTargetView	GetSingleRenderTargetView( int _MipLevelIndex, int _ArrayIndex );

		/// <summary>
		/// Gets a band render target view at the specified index in the mip hierarchy and in the array of render targets
		/// The render target view contains all the array render target elements from the specified mip level and array index
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the render target was created with mip maps, otherwise use 0)</param>
		/// <param name="_ArrayIndex">The index in the array of render targets (valid only if the render target was created as an array of render targets, otherwise use 0)</param>
		/// <example>Here is what the view covers with _MipLevelIndex=1 and _ArrayIndex=1
		/// 
		///        Array0 Array1 Array2
		///       ______________________
		///  Mip0 |      |      |      |
		///       |------+------+------|
		///  Mip1 |      |  X   |  X   |
		///       |------+------+------|
		///  Mip2 |      |      |      |
		///       ----------------------
		/// </example>
		RenderTargetView	GetArrayBandRenderTargetView( int _MipLevelIndex, int _ArrayIndex );
	}
}
