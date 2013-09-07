using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D10.Buffer;

namespace Nuaj
{
	/// <summary>
	/// This wraps a DirectX 3D texture
	/// </summary>
	public class Texture3D<PF> : Component, ITexture3D where PF:struct,IPixelFormat
	{
		#region FIELDS

		// Standard default parameters
		protected int							m_Width = 0;
		protected int							m_Height = 0;
		protected int							m_Depth = 0;
		protected Format						m_Format = Format.Unknown;
		protected int							m_MipLevelsCount = 0;
		protected bool							m_bHasAlpha = false;

		protected SharpDX.Direct3D10.Texture3D	m_Texture = null;
		protected ShaderResourceView			m_TextureView = null;
		protected ShaderResourceView[]			m_TextureViews = null;

		protected Vector3						m_Size3;
		protected Vector4						m_Size4;
		protected Vector3						m_InvSize3;
		protected Vector4						m_InvSize4;

		#endregion

		#region PROPERTIES

		#region ITexture3D Members

		public int		Width							{ get { return m_Width; } }
		public int		Height							{ get { return m_Height; } }
		public int		Depth							{ get { return m_Depth; } }
		public Format	Format							{ get { return m_Format; } }
		public bool		HasMipMaps						{ get { return m_MipLevelsCount > 1; } }
		public bool		HasAlpha						{ get { return m_bHasAlpha; } }
		public int		MipLevelsCount					{ get { return m_MipLevelsCount; } }
		public FormatSupport	Support					{ get { return m_Device.DirectXDevice.CheckFormatSupport( m_Format ); } }

		public SharpDX.Direct3D10.Texture3D	Texture		{ get { return m_Texture; } }
		public ShaderResourceView			TextureView	{ get { return m_TextureView; } }

		public Vector3	Size3							{ get { return m_Size3; } }
		public Vector4	Size4							{ get { return m_Size4; } }
		public Vector3	InvSize3						{ get { return m_InvSize3; } }
		public Vector4	InvSize4						{ get { return m_InvSize4; } }

		#endregion

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default Texture3D
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		protected	Texture3D( Device _Device, string _Name, int _Width, int _Height, int _Depth, int _MipLevelsCount ) : base( _Device, _Name )
		{
			m_Width = _Width;
			m_Height = _Height;
			m_Depth = _Depth;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_MipLevelsCount = CheckMipLevels( _MipLevelsCount );

			m_Size3 = new Vector3( m_Width, m_Height, m_Depth );
			m_Size4 = new Vector4( m_Size3, 0.0f );
			m_InvSize3 = new Vector3( 1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Depth );
			m_InvSize4 = new Vector4( m_InvSize3, 0.0f );
		}

		/// <summary>
		/// Initializes the 3D texture as a readonly (i.e. immutable) texture array
		/// As such, the texture must be initialized with an array of images immediately
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Images">Initial array of image data (compulsory for immutable textures !)
		/// NOTE: All the textures in the array must have the same size, format and mip-map depth</param>
		public	Texture3D( Device _Device, string _Name, Image3D<PF> _Image ) : base( _Device, _Name )
		{
			if ( _Image == null )
				throw new NException( this, "Immutable textures must be provided their data at initialization !" );

			m_Width = _Image.Width;
			m_Height = _Image.Height;
			m_Depth = _Image.Depth;
			m_Format = new PF().DirectXFormat;	// A bit ugly there but it's a single pixel so who cares ?
			m_MipLevelsCount = CheckMipLevels( _Image.MipLevelsCount );

			m_Size3 = new Vector3( m_Width, m_Height, m_Depth );
			m_Size4 = new Vector4( m_Size3, 0.0f );
			m_InvSize3 = new Vector3( 1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Depth );
			m_InvSize4 = new Vector4( m_InvSize3, 0.0f );

			Init( _Image );
		}

		/// <summary>
		/// Copies this texture to the specified target
		/// </summary>
		/// <param name="_Target"></param>
		/// <remarks>The target texture must have identical dimensions and format</remarks>
		public void		CopyTo( ITexture3D _Target )
		{
			if ( _Target == null )
				throw new NException( this, "Invalid target texture to copy to !" );
			if ( _Target.Width != m_Width || _Target.Height != m_Height || _Target.Depth != m_Depth || _Target.MipLevelsCount != m_MipLevelsCount )
				throw new NException( this, "The provided target texture doesn't have the same dimensions or mip levels count as the source texture !" );

			m_Device.DirectXDevice.CopyResource( m_Texture, _Target.Texture );
		}

		protected virtual void	Init( Image3D<PF> _Image )
		{
			Texture3DDescription	Desc = new Texture3DDescription();
			Desc.BindFlags = BindFlags.ShaderResource;
			Desc.CpuAccessFlags = CpuAccessFlags.None;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.Depth = m_Depth;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Immutable;

			if ( _Image != null )
			{
				m_bHasAlpha = _Image.HasAlpha;	// Copy alpha state
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc, _Image.DataBoxes ) );
			}
			else
				m_Texture = ToDispose( new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc ) );

			// Create the view
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Dimension = ShaderResourceViewDimension.Texture3D;
			ViewDesc.Format = m_Format;
			ViewDesc.Texture3D.MipLevels = m_MipLevelsCount;
            ViewDesc.Texture3D.MostDetailedMip = 0;
			ViewDesc.Format = m_Format;

			m_TextureView = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			// Create an empty array of texture views that we will fill if GetSingleTextureView() gets called...
			m_TextureViews = new ShaderResourceView[m_MipLevelsCount];
		}

		public ShaderResourceView	GetSingleTextureView( int _MipLevelIndex )
		{
			if ( m_TextureViews[_MipLevelIndex] != null )
				return m_TextureViews[_MipLevelIndex];

			// Create the view for that particular array entry
			ShaderResourceViewDescription	ViewDesc = new ShaderResourceViewDescription();
			ViewDesc.Dimension = ShaderResourceViewDimension.Texture3D;
			ViewDesc.Format = m_Format;
			ViewDesc.Texture3D.MipLevels = 1;
			ViewDesc.Texture3D.MostDetailedMip = _MipLevelIndex;

			m_TextureViews[_MipLevelIndex] = ToDispose( new ShaderResourceView( m_Device.DirectXDevice, m_Texture, ViewDesc ) );

			return m_TextureViews[_MipLevelIndex];
		}

		/// <summary>
		/// Saves to a binary stream
		/// </summary>
		/// <param name="_Stream"></param>
		public unsafe void				Save( System.IO.Stream _Stream )
		{
			//////////////////////////////////////////////////////////////////////////
			// Copy the render target to a staging resource
			Texture3DDescription	Desc = new Texture3DDescription();
			Desc.BindFlags = BindFlags.None;
			Desc.CpuAccessFlags = CpuAccessFlags.Read;
			Desc.Format = m_Format;
			Desc.Width = m_Width;
			Desc.Height = m_Height;
			Desc.Depth = m_Depth;
			Desc.MipLevels = m_MipLevelsCount;
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Staging;

			SharpDX.Direct3D10.Texture3D TempTexture = new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc );
			m_Device.DirectXDevice.CopyResource( m_Texture, TempTexture );

			//////////////////////////////////////////////////////////////////////////
			// Save to the stream
			using ( System.IO.BinaryWriter Writer = new System.IO.BinaryWriter( _Stream ) )
			{
				// Write dimensions
				Writer.Write( (int) m_Format );
				Writer.Write( m_Width );
				Writer.Write( m_Height );
				Writer.Write( m_Depth );
				Writer.Write( m_MipLevelsCount );

				// Write each mip level
				int	CurrentWidth = m_Width;
				int	CurrentHeight = m_Height;
				int	CurrentDepth = m_Depth;
				int	PixelSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(PF) );

				for ( int MipLevel=0; MipLevel < m_MipLevelsCount; MipLevel++ )
				{
					DataStream	Stream;
					DataBox		Data = TempTexture.Map( MipLevel, MapMode.Read, SharpDX.Direct3D10.MapFlags.None, out Stream );

// NO! There's a bug when you do that !
// 					// Read the entire stream content
// 					int		StreamLength = CurrentWidth * CurrentHeight * CurrentDepth * PixelSize;
// 					StreamLength = Data.SlicePitch * CurrentDepth;	// Just a check...
// 					byte[]	ByteContent = new byte[StreamLength];
//	 				Stream.Read( ByteContent, 0, ByteContent.Length );

					// Read byte by byte otherwise it doesn't work... (problem in SharpDX ??)
					byte[]	Content = new byte[CurrentWidth * CurrentHeight * CurrentDepth * PixelSize];
					int		Index = 0;
					for ( int Z=0; Z < CurrentDepth; Z++ )
						for ( int Y=0; Y < CurrentHeight; Y++ )
							for ( int X=0; X < CurrentWidth; X++ )
								for ( int ByteIndex=0; ByteIndex < PixelSize; ByteIndex++ )
									Content[Index++] = Stream.Read<byte>();

					// Write it back
					Writer.Write( Content.Length );
					_Stream.Write( Content, 0, Content.Length );

					Stream.Dispose();
					TempTexture.Unmap( MipLevel );

					// Down one level
					CurrentWidth = Math.Max( 1, CurrentWidth >> 1 );
					CurrentHeight = Math.Max( 1, CurrentHeight >> 1 );
					CurrentDepth = Math.Max( 1, CurrentDepth >> 1 );
				}
			}

			TempTexture.Dispose();
		}

		/// <summary>
		/// Loads from a binary stream
		/// WARNING: This throws an exception if the texture is not of the same size and format as the one stored in the stream
		/// </summary>
		/// <param name="_Stream"></param>
		public void					Load( System.IO.Stream _Stream )
		{
			SharpDX.Direct3D10.Texture3D TempTexture = null;
			try
			{
				//////////////////////////////////////////////////////////////////////////
				// Create the staging resource
				Texture3DDescription	Desc = new Texture3DDescription();
				Desc.BindFlags = BindFlags.None;
				Desc.CpuAccessFlags = CpuAccessFlags.Write;
				Desc.Format = m_Format;
				Desc.Width = m_Width;
				Desc.Height = m_Height;
				Desc.Depth = m_Depth;
				Desc.MipLevels = m_MipLevelsCount;
				Desc.OptionFlags = ResourceOptionFlags.None;
				Desc.Usage = ResourceUsage.Staging;

				TempTexture = new SharpDX.Direct3D10.Texture3D( m_Device.DirectXDevice, Desc );

				//////////////////////////////////////////////////////////////////////////
				// Load from the stream
				using ( System.IO.BinaryReader Reader = new System.IO.BinaryReader( _Stream ) )
				{
					// Read dimensions
					SharpDX.DXGI.Format	Format = (SharpDX.DXGI.Format) Reader.ReadInt32();
					if ( Format != m_Format )
						throw new Exception( "Format mismatch ! Read value is " + Format + "." );
					int	Width = Reader.ReadInt32();
					if ( Width != m_Width )
						throw new Exception( "Width mismatch ! Read value is " + Width + "." );
					int	Height = Reader.ReadInt32();
					if ( Height != m_Height )
						throw new Exception( "Height mismatch ! Read value is " + Height + "." );
					int	Depth = Reader.ReadInt32();
					if ( Depth != m_Depth )
						throw new Exception( "Depth mismatch ! Read value is " + Depth + "." );
					int	MipLevelsCount = Reader.ReadInt32();
					if ( MipLevelsCount != m_MipLevelsCount )
						throw new Exception( "Mip levels count mismatch ! Read value is " + MipLevelsCount + "." );

					// Read each mip level
					for ( int MipLevel=0; MipLevel < m_MipLevelsCount; MipLevel++ )
					{
						DataStream	Stream;
						DataBox		Data = TempTexture.Map( MipLevel, MapMode.Write, SharpDX.Direct3D10.MapFlags.None, out Stream );

						// Read the entire mip level content
						int		MipLevelDataLength = Reader.ReadInt32();
						byte[]	Temp = new byte[MipLevelDataLength];
						_Stream.Read( Temp, 0, Temp.Length );

						// Write it back
						Stream.Write( Temp, 0, Temp.Length );

						Stream.Dispose();
						TempTexture.Unmap( MipLevel );
					}
				}

				//////////////////////////////////////////////////////////////////////////
				// Copy from staging to current resource
				m_Device.DirectXDevice.CopyResource( TempTexture, m_Texture );
			}
			catch ( Exception )
			{
				throw;	
			}
			finally
			{
				TempTexture.Dispose();
			}
		}

		protected int	CheckMipLevels( int _MipLevelsCount )
		{
			if ( _MipLevelsCount > 0 )
				return	_MipLevelsCount;

			int	Size = Math.Max( m_Width, m_Height );
				Size = Math.Max( Size, m_Depth );
			_MipLevelsCount = (int) Math.Ceiling( Math.Log( Size ) / Math.Log( 2.0 ) );

			return _MipLevelsCount;
		}

		#endregion
	}
}
