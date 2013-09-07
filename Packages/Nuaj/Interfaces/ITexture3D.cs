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
	/// This is the interface to 3D textures
	/// </summary>
	public interface ITexture3D : IComponent
	{
		/// <summary>
		/// Gets the width of the texture
		/// </summary>
		int					Width							{ get; }

		/// <summary>
		/// Gets the height of the texture
		/// </summary>
		int					Height							{ get; }

		/// <summary>
		/// Gets the depth of the texture
		/// </summary>
		int					Depth							{ get; }

		/// <summary>
		/// Gets the format of the texture
		/// </summary>
		Format				Format							{ get; }

		/// <summary>
		/// Tells if the texture has mip maps
		/// </summary>
		bool				HasMipMaps						{ get; }

		/// <summary>
		/// Tells if the texture has alpha
		/// </summary>
		bool				HasAlpha						{ get; }

		/// <summary>
		/// Gets the mip levels count
		/// </summary>
		int					MipLevelsCount					{ get; }

		/// <summary>
		/// Gets the formats and features this texture supports
		/// </summary>
		FormatSupport		Support							{ get; }

		/// <summary>
		/// Gets the DirectX texture resource
		/// </summary>
		Texture3D			Texture							{ get; }

		/// <summary>
		/// Gets the texture view that can be attached to the pipeline
		/// </summary>
		ShaderResourceView	TextureView						{ get; }

		// Helpers
		/// <summary>
		/// Gets the size of the texture Vector3( Width, Height, Depth )
		/// </summary>
		Vector3				Size3							{ get; }
		/// <summary>
		/// Gets the size of the texture Vector4( Width, Height, Depth, 0 )
		/// </summary>
		Vector4				Size4							{ get; }
		/// <summary>
		/// Gets your typical Vector3( 1/Width, 1/Height, 1/Depth )
		/// </summary>
		Vector3				InvSize3						{ get; }
		/// <summary>
		/// Gets your typical Vector4( 1/Width, 1/Height, 1/Depth, 0 )
		/// </summary>
		Vector4				InvSize4						{ get; }

		/// <summary>
		/// Gets a single texture view at the specified index in the mip hierarchy
		/// The texture view contains a single texture element at the specified mip level
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the texture was created with mip maps, otherwise use 0)</param>
		ShaderResourceView	GetSingleTextureView( int _MipLevelIndex );
	}
}
