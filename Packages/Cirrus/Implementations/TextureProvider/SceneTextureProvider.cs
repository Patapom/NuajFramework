using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This class implements the features of a basic texture provider.
	/// It's abstract from the loading mechanism (i.e. disk, archive or memory) through the IFileLoader interface.
	/// </summary>
	/// <remarks>If you specify a NULL loader (like with the lightweight constructor) then a default DISK loader will be used</remarks>
	public class SceneTextureProvider : Component, Scene.ITextureProvider, IFileLoader
	{
		#region NESTED TYPES

		public class	WatchedTexture : IDisposable
		{
			#region FIELDS

			protected SceneTextureProvider				m_Owner = null;
			protected ITexture2D						m_Texture = null;

			// Auto-update
			protected System.IO.FileInfo				m_TextureFile = null;
			protected System.IO.FileInfo				m_OpacityTextureFile = null;
			protected bool								m_bCreateMipMaps = false;
			protected Scene.TextureUpdatedEventHandler	m_TextureUpdateHandler = null;
			protected FileSystemWatcher					m_TextureWatcher = null;
			protected FileSystemWatcher					m_OpacityTextureWatcher = null;

			#endregion

			#region PROPERTIES

			public System.IO.FileInfo		TextureFile			{ get { return m_TextureFile; } }
			public System.IO.FileInfo		OpacityTextureFile	{ get { return m_TextureFile; } }
			public ITexture2D				Texture				{ get { return m_Texture; } }

			#endregion

			#region METHODS

			public WatchedTexture( SceneTextureProvider _Owner, FileInfo _TextureFile, FileInfo _OpacityTextureFile, bool _bCreateMipMaps, ITexture2D _Texture, Scene.TextureUpdatedEventHandler _TextureUpdateHandler )
			{
				m_Owner = _Owner;
				m_TextureFile = _TextureFile;
				m_OpacityTextureFile = _OpacityTextureFile;
				m_bCreateMipMaps = _bCreateMipMaps;
				m_Texture = _Texture;
				m_TextureUpdateHandler = _TextureUpdateHandler;

				if ( m_TextureUpdateHandler == null || !m_Owner.m_bWatchChanges )
					return;	// No need to watch for changes...

				m_TextureWatcher = new FileSystemWatcher( _TextureFile.DirectoryName, _TextureFile.Name );
				m_TextureWatcher.IncludeSubdirectories = false;
				m_TextureWatcher.Changed += new FileSystemEventHandler( Watcher_TextureChanged );
				m_TextureWatcher.Deleted += new FileSystemEventHandler( Watcher_TextureDeleted );
				m_TextureWatcher.Error += new ErrorEventHandler( Watcher_TextureError );
				m_TextureWatcher.EnableRaisingEvents = true;

				if ( _OpacityTextureFile != null )
				{
					m_OpacityTextureWatcher = new FileSystemWatcher( _OpacityTextureFile.DirectoryName, _OpacityTextureFile.Name );
					m_OpacityTextureWatcher.IncludeSubdirectories = false;
					m_OpacityTextureWatcher.Changed += new FileSystemEventHandler( Watcher_TextureChanged );
					m_OpacityTextureWatcher.Deleted += new FileSystemEventHandler( Watcher_TextureDeleted );
					m_OpacityTextureWatcher.Error += new ErrorEventHandler( Watcher_TextureError );
					m_OpacityTextureWatcher.EnableRaisingEvents = true;
				}
			}

			#region IDisposable Members

			public void  Dispose()
			{
 				m_Texture.Dispose();
				m_Texture = null;

				// Dispose of watchers
				if ( m_TextureWatcher != null )
					m_TextureWatcher.Dispose();
				m_TextureWatcher = null;
				if ( m_OpacityTextureWatcher != null )
					m_OpacityTextureWatcher.Dispose();
				m_OpacityTextureWatcher = null;
			}

			#endregion

			#endregion

			#region EVENT HANDLERS

			protected DateTime	m_LastChanged = DateTime.Now;
			void Watcher_TextureChanged( object sender, FileSystemEventArgs e )
			{
				DateTime	Current = DateTime.Now;
				if ( (Current - m_LastChanged).TotalSeconds < 1.0 )
					return;	// Too soon ! Sometimes we're notified several times...

				m_TextureWatcher.EnableRaisingEvents = false;
				m_OpacityTextureWatcher.EnableRaisingEvents = false;

				// Reload...
				ITexture2D	NewTexture = null;
				try
				{
					NewTexture = m_Owner.LoadTexture( m_TextureFile, m_OpacityTextureFile, m_bCreateMipMaps );

					// Notify
					m_TextureUpdateHandler( m_Owner, m_Texture, NewTexture );

					if ( m_Texture != null )
						m_Texture.Dispose();	// Dispose of the old texture...
					m_Texture = NewTexture;		// Use the new texture instead...
					NewTexture = null;			// Clean pointer otherwise it will get disposed of in the 'finally' clause

					m_LastChanged = Current;
				}
				catch ( Exception )
				{
					// Silently fail... :(
				}
				finally
				{
					// Dispose of the new texture if we failed to assign it...
					if ( NewTexture != null )
						NewTexture.Dispose();

					m_TextureWatcher.EnableRaisingEvents = true;
					m_OpacityTextureWatcher.EnableRaisingEvents = true;
				}
			}

			void Watcher_TextureDeleted( object sender, FileSystemEventArgs e )
			{
			}

			void Watcher_TextureError( object sender, ErrorEventArgs e )
			{
			}

			#endregion
		}

		/// <summary>
		/// Used for the TextureNeedsLoading event.
		/// Subscribe to the event to add your custom texture loader that will take precedence over the default texture loaders.
		/// Return a null textre to use the default loaders.
		/// </summary>
		/// <param name="_FileName"></param>
		/// <param name="_OpacityFileName"></param>
		/// <returns>The loaded texture or null to use default the loaders</returns>
		public delegate ITexture2D	LoadTextureEventHandler( FileInfo _FileName, FileInfo _OpacityFileName );

		/// <summary>
		/// Used for the TextureGammaRequested event.
		/// Subscrive to the event to override the default ImageGamma and UseSRGB settings per image
		/// </summary>
		/// <param name="_FileName">The texture file to retrieve informations about</param>
		/// <param name="_ImageGamma">The image gamma to apply to the image</param>
		/// <param name="_sRGB">Tells if the image should be considered as sRGB-encoded</param>
		public delegate void		TextureGammaRequestedEventHandler( FileInfo _FileName, out float _ImageGamma, out bool _sRGB );

		#endregion

		#region FIELDS

		protected IFileLoader		m_Loader = null;
		protected bool				m_bWatchChanges = false;
		protected float				m_GammaCorrection = Image<PF_Empty>.GAMMA_JPEG;
		protected bool				m_bUsesRGB = false;

		protected List<string>		m_SourceRootPathsToStripOff = new List<string>();
		protected DirectoryInfo		m_BaseDirectory = null;
		protected bool				m_bForceCreateMipMaps = false;
		protected bool				m_bExceptionOnTextureNotFound = true;

		protected Dictionary<string,WatchedTexture>	m_URL2Texture = new Dictionary<string,WatchedTexture>();
		protected List<string>		m_TextureErrors = new List<string>();

		// Statistics
		protected int				m_LoadedTexturesCount = 0;
		protected int				m_SumSurface = 0;
		protected int				m_SumMemory = 0;
		protected int				m_MinSurface = int.MaxValue;
		protected int				m_MaxSurface = 0;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the source root paths that we need to strip out when receiving texture load requests
		/// </summary>
		public string[]				SourceRootPathsToStripOff	{ get { return m_SourceRootPathsToStripOff.ToArray(); } }

		/// <summary>
		/// Gets or sets the base directory to load textures from
		/// </summary>
		public DirectoryInfo		BaseDirectory			{ get { return m_BaseDirectory; } set { m_BaseDirectory = value; } }

		/// <summary>
		/// Gets or sets the mip-map creation override state
		/// If set to true, mip-maps will always be created despite specific requests for no mip-maps
		/// </summary>
		public bool					ForceCreateMipMaps		{ get { return m_bForceCreateMipMaps; } set { m_bForceCreateMipMaps = value; } }

		/// <summary>
		/// Gets or sets the default gamma correction to apply to the loaded images
		/// </summary>
		public float				GammaCorrection			{ get { return m_GammaCorrection; } set { m_GammaCorrection = value; } }

		/// <summary>
		/// Gets or sets the usage of sRGB input images.
		/// If true, the textures will be considered sRGB-encoded
		/// </summary>
		public bool					UseSRGB					{ get { return m_bUsesRGB; } set { m_bUsesRGB = value; } }

		/// <summary>
		/// Gets or sets the flag that will make us throw an exception if a texture is not found
		/// </summary>
		public bool					ExceptionOnTextureNotFound	{ get { return m_bExceptionOnTextureNotFound; } set { m_bExceptionOnTextureNotFound = value; } }

		public bool					HasErrors				{ get { return m_TextureErrors.Count > 0; } }
		public string[]				TextureErrors			{ get { return m_TextureErrors.ToArray(); } }

		/// <summary>
		/// Occurs when a texture is requested to load
		/// </summary>
		public event LoadTextureEventHandler	TextureNeedsLoading;

		/// <summary>
		/// Occurs when a texture is loaded to manually provide for a gamma correction value
		/// </summary>
		public event TextureGammaRequestedEventHandler	TextureGammaRequested;

		// Statistics
		public int					LoadedTexturesCount		{ get { return m_LoadedTexturesCount; } }
		public int					MinTextureSurface		{ get { return m_LoadedTexturesCount > 0 ? m_MinSurface : 0; } }
		public int					MaxTextureSurface		{ get { return m_MaxSurface; } }
		public int					AverageTextureSurface	{ get { return m_SumSurface / Math.Max( 1, m_LoadedTexturesCount ); } }
		public int					TotalTextureSurface		{ get { return m_SumSurface; } }
		public int					TotalTextureMemory		{ get { return m_SumMemory; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a FILE texture provider that will fetch its textures from the provided base directory
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BaseDirectory">The base directory where tetures can be found</param>
		public	SceneTextureProvider( Device _Device, string _Name, DirectoryInfo _BaseDirectory ) : this( _Device, _Name, _BaseDirectory, null, false )
		{
		}

		/// <summary>
		/// Creates a texture provider that will fetch its textures from the provided base directory
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_BaseDirectory">The base directory where tetures can be found</param>
		/// <param name="_Loader">The abstract file loader</param>
		/// <param name="_bWatchTextureChanges">True to watch texture changes and be notified in such case (so you can dynamiclly reload them)</param>
		public	SceneTextureProvider( Device _Device, string _Name, DirectoryInfo _BaseDirectory, IFileLoader _Loader, bool _bWatchTextureChanges ) : base( _Device, _Name )
		{
			m_BaseDirectory = _BaseDirectory;
			m_Loader = _Loader != null ? _Loader : this;	// If no loader is provided, use us who implement a default provider from disk
			m_bWatchChanges = _bWatchTextureChanges;
		}

		public override void Dispose()
		{
			base.Dispose();

			foreach ( string URL in m_URL2Texture.Keys )
				m_URL2Texture[URL].Dispose();
			m_URL2Texture.Clear();
		}

		#region ITextureProvider Members

		public ITexture2D LoadTexture( string _URL, string _OpacityURL, bool _bCreateMipMaps, Scene.TextureUpdatedEventHandler _TextureUpdatedHandler )
		{
			string FullURL = _URL + "|" + _OpacityURL;
			if ( m_URL2Texture.ContainsKey( FullURL ) )
				return m_URL2Texture[FullURL].Texture;

// DEBUG
// if ( _URL.IndexOf( "Mlyn_02_DS" ) != -1 )
// 	_URL = CleanUpURL( _URL );
// DEBUG

			// Strip off source root paths
			_URL = CleanUpURL( _URL );
			FileInfo	FullName = new FileInfo( Path.Combine( m_BaseDirectory.FullName, _URL ) );
			if ( !FullName.Exists )
			{
				if ( m_bExceptionOnTextureNotFound )
					throw new NException( this, "Texture \"" + FullName + "\" does not exist !" );
				return null;
			}

			FileInfo	OpacityFullName = null;
			if ( _OpacityURL != null )
			{	// We have an opacity map !
				_OpacityURL = CleanUpURL( _OpacityURL );
				OpacityFullName = new FileInfo( Path.Combine( m_BaseDirectory.FullName, _OpacityURL ) );
			}

			_bCreateMipMaps |= m_bForceCreateMipMaps;

			ITexture2D	Result = LoadTexture( FullName, OpacityFullName, _bCreateMipMaps );
			if ( Result == null )
				return null;	// Failed...

			m_URL2Texture[FullURL] = new WatchedTexture( this, FullName, OpacityFullName, _bCreateMipMaps, Result, _TextureUpdatedHandler );

			return Result;
		}

		#endregion

		/// <summary>
		/// Loads a texture given its path on disk
		/// </summary>
		/// <param name="_FileName"></param>
		/// <param name="OpacityFullName"></param>
		/// <param name="_bCreateMipMaps"></param>
		/// <returns></returns>
		public ITexture2D	LoadTexture( FileInfo _FileName, FileInfo _OpacityFileName, bool _bCreateMipMaps )
		{
			if ( !_FileName.Exists )
			{
				if ( m_bExceptionOnTextureNotFound )
					throw new NException( this, "Texture \"" + _FileName + "\" does not exist !" );
				return null;
			}

			// Ask for custom load
			if ( TextureNeedsLoading != null )
				foreach ( Delegate D in TextureNeedsLoading.GetInvocationList() )
				{
					ITexture2D	Result = D.DynamicInvoke( _FileName, _OpacityFileName ) as ITexture2D;
					if ( Result != null )
						return Result;
				}

			// Attempt to load the opacity bitmap
			System.Drawing.Bitmap	OpacityBitmap = null;
			if ( _OpacityFileName != null && _OpacityFileName.Exists )
			{
				try
				{
					OpacityBitmap = LoadBitmap( _OpacityFileName.FullName );
				}
				catch ( Exception _e )
				{
					m_TextureErrors.Add( "Failed to load opacity bitmap \"" + _OpacityFileName + "\" ! =>" + _e.Message + "\r\n(using error bitmap)" );
					OpacityBitmap = Properties.Resources.ErrorTexture;
				}
			}

			// Ask for gamma
			float	Gamma = m_GammaCorrection;
			bool	IssRGB = m_bUsesRGB;
			(TextureGammaRequested ?? DefaultGammaEventHandler)( _FileName, out Gamma, out IssRGB );
// 			if ( TextureGammaRequested != null )
// 				TextureGammaRequested( _FileName, out Gamma, out IssRGB );

			System.Drawing.Bitmap	Bitmap = null;
			try
			{
				string	FileType = _FileName.Extension.ToUpper();
				switch ( FileType )
				{
					case ".JPG":
					case ".JPEG":
					case ".PNG":
					case ".BMP":
					case ".TGA":
					{
						try
						{
							Bitmap = LoadBitmap( _FileName.FullName );
						}
						catch ( Exception _e )
						{
							m_TextureErrors.Add( "Failed to load bitmap \"" + _FileName + "\" ! =>" + _e.Message + "\r\n(using error bitmap)" );
							Bitmap = Properties.Resources.ErrorTexture;
						}

						// Update statistics
						int	Surface = Bitmap.Width * Bitmap.Height;
						m_LoadedTexturesCount++;
						m_SumSurface += Surface;
						m_SumMemory += 4*Surface;
						m_MinSurface = Math.Min( m_MinSurface, Surface );
						m_MaxSurface = Math.Max( m_MaxSurface, Surface );

						// Create the texture
						if ( OpacityBitmap != null )
						{	// Create a texture with alpha
							if ( IssRGB )
								using ( Image<PF_RGBA8_sRGB> Image = new Image<PF_RGBA8_sRGB>( m_Device, _FileName.FullName, Bitmap, OpacityBitmap, _bCreateMipMaps ? 0 : 1, Gamma ) )
									return new Texture2D<PF_RGBA8_sRGB>( m_Device, _FileName.FullName, Image );
							else
								using ( Image<PF_RGBA8> Image = new Image<PF_RGBA8>( m_Device, _FileName.FullName, Bitmap, OpacityBitmap, _bCreateMipMaps ? 0 : 1, Gamma ) )
									return new Texture2D<PF_RGBA8>( m_Device, _FileName.FullName, Image );
						}
						else
						{	// Create an opaque texture
							if ( IssRGB )
								using ( Image<PF_RGBA8_sRGB> Image = new Image<PF_RGBA8_sRGB>( m_Device, _FileName.FullName, Bitmap, _bCreateMipMaps ? 0 : 1, Gamma ) )
									return new Texture2D<PF_RGBA8_sRGB>( m_Device, _FileName.FullName, Image );
							else
								using ( Image<PF_RGBA8> Image = new Image<PF_RGBA8>( m_Device, _FileName.FullName, Bitmap, _bCreateMipMaps ? 0 : 1, Gamma ) )
									return new Texture2D<PF_RGBA8>( m_Device, _FileName.FullName, Image );
						}
					}

					case ".HDR":
					{
						Stream	Stream = m_Loader.OpenFile( _FileName );
						byte[]	Content = new byte[Stream.Length];
						Stream.Read( Content, 0, (int) Stream.Length );
						Stream.Close();

						Vector4[,]	HDRImage = Image<PF_RGBA16F>.LoadAndDecodeHDRFormat( Content );

						// Update statistics
						int	Surface = HDRImage.GetLength( 0 ) * HDRImage.GetLength( 1 );
						m_LoadedTexturesCount++;
						m_SumSurface += Surface;
						m_SumMemory += 2*4*Surface;
						m_MinSurface = Math.Min( m_MinSurface, Surface );
						m_MaxSurface = Math.Max( m_MaxSurface, Surface );

						// Create the texture
						using ( Image<PF_RGBA16F> Image = new Image<PF_RGBA16F>( m_Device, _FileName.FullName, HDRImage, 0.0f, _bCreateMipMaps ? 0 : 1 ) )
							return new Texture2D<PF_RGBA16F>( m_Device, _FileName.FullName, Image );
					}

					default:
						throw new Exception( "Unsupported file format " + FileType + "..." );
				}
			}
			catch ( Exception _e )
			{
				m_TextureErrors.Add( "Failed to load texture \"" + _FileName + "\" ! =>" + _e.Message );
			}
			finally
			{
				// Dispose of bitmaps
				if ( Bitmap != null && Bitmap != Properties.Resources.ErrorTexture )
					Bitmap.Dispose();
				if ( OpacityBitmap != null && OpacityBitmap != Properties.Resources.ErrorTexture )
					OpacityBitmap.Dispose();
			}

			return null;
		}

		/// <summary>
		/// Adds a source root path to strip off the provided URLs
		/// </summary>
		/// <param name="_SourceRootPathToStripOff"></param>
		/// <remarks>This is useful for example if you receive complex texture URLs which have a common root path and you want to relocate that root path to the target base directory.</remarks>
		/// <example>Say you have all input texture URLs in the form "C:/My/Complex/Root/I/Dont/Want/Anymore/(some texture name.jpg)" and you only need (some texture name.jpg),
		/// you just have to set "C:/My/Complex/Root/I/Dont/Want/Anymore/" as the source root path and it will be automatically stripped off the source texture name</example>
		public void		AddSourceRootPathToStripOff( string _SourceRootPathToStripOff )
		{
			if ( _SourceRootPathToStripOff == null )
				return;

			_SourceRootPathToStripOff = _SourceRootPathToStripOff.Replace( @"\", "/" );
			if ( !_SourceRootPathToStripOff.EndsWith( "/" ) )
				_SourceRootPathToStripOff += "/";

			m_SourceRootPathsToStripOff.Add( _SourceRootPathToStripOff );
		}

		/// <summary>
		/// This will clear all the cached textures
		/// </summary>
		public void		ClearTextures()
		{
			base.Dispose();

			// Clear statistics
			m_LoadedTexturesCount = 0;
			m_SumSurface = 0;
			m_SumMemory = 0;
			m_MinSurface = int.MaxValue;
			m_MaxSurface = 0;
		}

		#region Bitmap Loaders

		/// <summary>
		/// Loads a bitmap from a file (the file MUST exist !)
		/// </summary>
		/// <param name="_FileName"></param>
		/// <returns></returns>
		protected System.Drawing.Bitmap	LoadBitmap( string _FileName )
		{
			string	FileType = Path.GetExtension( _FileName ).ToUpper();
			switch ( FileType )
			{
				case ".JPG":
				case ".JPEG":
				case ".PNG":
				case ".BMP":
					using ( Stream S = m_Loader.OpenFile( new FileInfo( _FileName ) ) )
						return System.Drawing.Bitmap.FromStream( S ) as System.Drawing.Bitmap;

				case ".TGA":
					using ( Stream S = m_Loader.OpenFile( new FileInfo( _FileName ) ) )
						using ( Nuaj.Helpers.TargaImage TGA = new Nuaj.Helpers.TargaImage( S ) )
							return new System.Drawing.Bitmap( TGA.Image );

				default:
					throw new Exception( "Unsupported file format " + FileType + "..." );
			}
		}

		#endregion

		protected string	CleanUpURL( string _URL )
		{
			_URL =_URL.Replace( @"\", "/" );
			foreach ( string SourceRootPathToStripOff in m_SourceRootPathsToStripOff )
				if ( _URL.IndexOf( SourceRootPathToStripOff ) == 0 )
				{	// Strip off !
					_URL = _URL.Remove( 0, SourceRootPathToStripOff.Length );
					break;
				}

			return _URL;
		}

		// We implement a default DISK loader in case no loader is specified

		#region IFileLoader Members

		public Stream OpenFile( FileInfo _FileName )
		{
			return _FileName.OpenRead();
		}

		public void ReadBinaryFile( FileInfo _FileName, FileReaderDelegate _Reader )
		{
			using ( Stream S = OpenFile( _FileName ) )
				using ( BinaryReader Reader = new BinaryReader( S ) )
					_Reader( Reader );
		}

		#endregion

		/// <summary>
		/// This is the default implementation 
		/// </summary>
		/// <param name="_FileName"></param>
		/// <param name="_ImageGamma"></param>
		/// <param name="_sRGB"></param>
		public void		DefaultGammaEventHandler( FileInfo _FileName, out float _ImageGamma, out bool _sRGB )
		{
			_ImageGamma = m_GammaCorrection;
			_sRGB = m_bUsesRGB;
		}

		#endregion
	}
}
