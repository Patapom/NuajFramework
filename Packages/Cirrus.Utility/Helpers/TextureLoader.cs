using System;
using System.Collections.Generic;
using System.Linq;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This helper class loads textures from an abstract file source using the IFileLoader interface.
	/// </summary>
	public class TextureLoader : Component
	{
		#region FIELDS

		protected IFileLoader	m_Loader = null;

		#endregion

		#region METHODS

		public TextureLoader( Device _Device, string _Name, IFileLoader _Loader ) : base( _Device, _Name )
		{
			m_Loader = _Loader;
		}

		/// <summary>
		/// Loads a 2D texture
		/// </summary>
		/// <typeparam name="PF">The pixel format for the texture</typeparam>
		/// <param name="_Name">The name to give to the texture</param>
		/// <param name="_FileName">The file name of the texture to load</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns>The requested texture</returns>
		public Texture2D<PF>	LoadTexture<PF>( string _Name, System.IO.FileInfo _FileName, float _ImageGamma ) where PF:struct,IPixelFormat
		{
			return LoadTexture<PF>( _Name, _FileName, 0, _ImageGamma );
		}

		/// <summary>
		/// Loads a 2D texture
		/// </summary>
		/// <typeparam name="PF">The pixel format for the texture</typeparam>
		/// <param name="_Name">The name to give to the texture</param>
		/// <param name="_FileName">The file name of the texture to load</param>
		/// <param name="_MipLevelsCount">The amount of mip levels to create</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns>The requested texture</returns>
		public Texture2D<PF>	LoadTexture<PF>( string _Name, System.IO.FileInfo _FileName, int _MipLevelsCount, float _ImageGamma ) where PF:struct,IPixelFormat
		{
			using ( System.IO.Stream BitmapStream = m_Loader.OpenFile( _FileName ) )
				return ToDispose( Texture2D<PF>.CreateFromBitmapStream( m_Device, _Name, BitmapStream, _MipLevelsCount, _ImageGamma ) );
		}

		/// <summary>
		/// Loads several 2D textures into a single texture array
		/// </summary>
		/// <typeparam name="PF">The pixel format for the texture</typeparam>
		/// <param name="_Name">The name to give to the texture</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <param name="_FileName">The file name of the texture to load</param>
		/// <returns>The requested texture</returns>
		public Texture2D<PF>	LoadTexture<PF>( string _Name, float _ImageGamma, params System.IO.FileInfo[] _FileNames ) where PF:struct,IPixelFormat
		{
			return LoadTexture<PF>( _Name, 0, _ImageGamma, _FileNames );
		}

		/// <summary>
		/// Loads several 2D textures into a single texture array
		/// </summary>
		/// <typeparam name="PF">The pixel format for the texture</typeparam>
		/// <param name="_Name">The name to give to the texture</param>
		/// <param name="_MipLevelsCount">The amount of mip levels to create</param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <param name="_FileName">The file name of the texture to load</param>
		/// <returns>The requested texture</returns>
		public Texture2D<PF>	LoadTexture<PF>( string _Name, int _MipLevelsCount, float _ImageGamma, params System.IO.FileInfo[] _FileNames ) where PF:struct,IPixelFormat
		{
			// Load images
			Image<PF>[]	Images = new Image<PF>[_FileNames.Length];
			for ( int ImageIndex=0; ImageIndex < Images.Length; ImageIndex++ )
				using ( System.IO.Stream S = m_Loader.OpenFile( _FileNames[ImageIndex] ) )
					Images[ImageIndex] = Image<PF>.CreateFromBitmapStream( m_Device, "Temp", S, _MipLevelsCount, _ImageGamma );

			// Create the texture
			Texture2D<PF>	Result = ToDispose( new Texture2D<PF>( m_Device, _Name, Images ) );

			// Dispose of images
			foreach ( Image<PF> I in Images )
				I.Dispose();

			return Result;
		}

		/// <summary>
		/// Loads a set of sprites organized in a TPage into a single texture array
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <param name="_Name"></param>
		/// <param name="_FileName"></param>
		/// <param name="_SpriteWidth"></param>
		/// <param name="_SpriteHeight"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ImageGamma">The gamma correction the image was encoded with (JPEG is 2.2 for example, if not sure use 1.0)</param>
		/// <returns></returns>
		public Texture2D<PF>	LoadTPage<PF>( string _Name, System.IO.FileInfo _FileName, int _SpriteWidth, int _SpriteHeight, int _MipLevelsCount, float _ImageGamma ) where PF:struct,IPixelFormat
		{
			using ( System.IO.Stream BitmapStream = m_Loader.OpenFile( _FileName ) )
			{
				using ( System.Drawing.Bitmap BitmapSprites = System.Drawing.Bitmap.FromStream( BitmapStream ) as System.Drawing.Bitmap )
				{
					Image<PF>[]		Sprites = Image<PF>.LoadFromTPage( m_Device, _Name, _SpriteWidth, _SpriteHeight, BitmapSprites, _MipLevelsCount, _ImageGamma );

					Texture2D<PF>	Result = ToDispose( new Texture2D<PF>( m_Device, _Name, Sprites ) );

					foreach ( Image<PF> Sprite in Sprites )
						Sprite.Dispose();

					return Result;
				}
			}
		}

		#endregion
	}
}
