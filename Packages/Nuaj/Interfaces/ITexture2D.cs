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
	/// This is the interface to 2D textures
	/// </summary>
	public interface ITexture2D : IComponent
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
		/// Gets the array size for 2D texture arrays
		/// </summary>
		int					ArraySize						{ get; }

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
		/// Gets the actual amount of multi samples used for the texture
		/// </summary>
		int					MultiSamplesCount				{ get; }

		/// <summary>
		/// Gets the formats and features this texture supports
		/// </summary>
		FormatSupport		Support							{ get; }

		/// <summary>
		/// Gets the DirectX texture resource
		/// </summary>
		Texture2D			Texture		{ get; }

		/// <summary>
		/// Gets the texture view that can be attached to the pipeline
		/// </summary>
		ShaderResourceView	TextureView	{ get; }

		/// <summary>
		/// Gets a single texture view at the specified index in the mip hierarchy and in the array of textures
		/// The texture view contains a single texture element at the specified mip level and array index
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the texture was created with mip maps, otherwise use 0)</param>
		/// <param name="_ArrayIndex">The index in the array of textures (valid only if the texture was created as an array of textures, otherwise use 0)</param>
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
		ShaderResourceView	GetSingleTextureView( int _MipLevelIndex, int _ArrayIndex );

		/// <summary>
		/// Gets a band texture view at the specified index in the mip hierarchy and in the array of textures
		/// The texture view contains all the mip level texture elements from the specified mip level and array index
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the texture was created with mip maps, otherwise use 0)</param>
		/// <param name="_ArrayIndex">The index in the array of textures (valid only if the texture was created as an array of textures, otherwise use 0)</param>
		/// <example>Here is what the view covers with _MipLevelIndex=1 and _ArrayIndex=1
		/// 
		///        Array0 Array1 Array2
		///       ______________________
		///  Mip0 |      |      |      |
		///       |------+------+------|
		///  Mip1 |      |  X   |      |
		///       |------+------+------|
		///  Mip2 |      |  X   |      |
		///       ----------------------
		/// </example>
		ShaderResourceView	GetMipBandTextureView( int _MipLevelIndex, int _ArrayIndex );

		/// <summary>
		/// Gets a band texture view at the specified index in the mip hierarchy and in the array of textures
		/// The texture view contains all the array texture elements from the specified mip level and array index
		/// </summary>
		/// <param name="_MipLevelIndex">The index in the pyramid of mip levels (valid only if the texture was created with mip maps, otherwise use 0)</param>
		/// <param name="_ArrayIndex">The index in the array of textures (valid only if the texture was created as an array of textures, otherwise use 0)</param>
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
		ShaderResourceView	GetArrayBandTextureView( int _MipLevelIndex, int _ArrayIndex );

		// Helpers
		/// <summary>
		/// Gets the texture's aspect ratio
		/// </summary>
		float				AspectRatio			{ get; }
		/// <summary>
		/// Gets the size of the texture Vector2( Width, Height )
		/// </summary>
		Vector2				Size2				{ get; }
		/// <summary>
		/// Gets the size of the texture Vector3( Width, Height, 0 )
		/// </summary>
		Vector3				Size3				{ get; }
		/// <summary>
		/// Gets your typical Vector2( 1/Width, 1/Height )
		/// </summary>
		Vector2				InvSize2			{ get; }
		/// <summary>
		/// Gets your typical Vector3( 1/Width, 1/Height, 0 )
		/// </summary>
		Vector3				InvSize3			{ get; }
	}
}
