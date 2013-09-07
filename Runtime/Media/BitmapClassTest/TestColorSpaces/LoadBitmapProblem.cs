/// <summary>
/// Actual load from a byte[] in memory
/// </summary>
/// <param name="_ImageFileContent">The source image content as a byte[]</param>
/// <param name="_FileType">The type of file to load</param>
/// <exception cref="NotSupportedException">Occurs if the image type is not supported by the Bitmap class</exception>
/// <exception cref="NException">Occurs if the source image format cannot be converted to RGBA32F which is the generic format we read from</exception>
public void	LoadWIC( byte[] _ImageFileContent, FILE_TYPE _FileType )
{
	BitmapFrameDecode	Frame = null;
	try
	{
		// Load the bitmap source
		switch ( _FileType )
		{
			case FILE_TYPE.JPEG:
				{
					BuildDataStream( _ImageFileContent, ( DataStream _MemoryStream ) =>
					{
						using ( WICStream Stream = new WICStream( m_Factory, _MemoryStream ) )
						{
							using ( JpegBitmapDecoder Decoder = new JpegBitmapDecoder( m_Factory ) )
							{
								Decoder.Initialize( Stream, DecodeOptions.CacheOnDemand );
								Frame = Decoder.GetFrame( 0 );
								using ( MetadataQueryReader Meta = Frame.MetadataQueryReader )
									m_ColorProfile = new ColorProfile( Meta, _FileType );
							}
						}
					} );
					break;
				}

			case FILE_TYPE.PNG:
				{
					BuildDataStream( _ImageFileContent, ( DataStream _MemoryStream ) =>
					{
						using ( WICStream Stream = new WICStream( m_Factory, _MemoryStream ) )
						{
							using ( PngBitmapDecoder Decoder = new PngBitmapDecoder( m_Factory ) )
							{
								Decoder.Initialize( Stream, DecodeOptions.CacheOnDemand );
								Frame = Decoder.GetFrame( 0 );
								using ( MetadataQueryReader Meta = Frame.MetadataQueryReader )
									m_ColorProfile = new ColorProfile( Meta, _FileType );
							}
						}
					} );
					break;
				}

			case FILE_TYPE.TIFF:
				{
					BuildDataStream( _ImageFileContent, ( DataStream _MemoryStream ) =>
					{
						using ( WICStream Stream = new WICStream( m_Factory, _MemoryStream ) )
						{
							using ( TiffBitmapDecoder Decoder = new TiffBitmapDecoder( m_Factory ) )
							{
								Decoder.Initialize( Stream, DecodeOptions.CacheOnDemand );
								Frame = Decoder.GetFrame( 0 );
								using ( MetadataQueryReader Meta = Frame.MetadataQueryReader )
									m_ColorProfile = new ColorProfile( Meta, _FileType );
							}
						}
					} );
					break;
				}

			case FILE_TYPE.GIF:
				{
					BuildDataStream( _ImageFileContent, ( DataStream _MemoryStream ) =>
					{
						using ( WICStream Stream = new WICStream( m_Factory, _MemoryStream ) )
						{
							using ( GifBitmapDecoder Decoder = new GifBitmapDecoder( m_Factory ) )
							{
								Decoder.Initialize( Stream, DecodeOptions.CacheOnDemand );
								Frame = Decoder.GetFrame( 0 );
								using ( MetadataQueryReader Meta = Frame.MetadataQueryReader )
									m_ColorProfile = new ColorProfile( Meta, _FileType );
							}
						}
					} );
					break;
				}

			case FILE_TYPE.TGA:
				{
					// Load as a System.Drawing.Bitmap and convert to Vector4
					using ( System.IO.MemoryStream Stream = new System.IO.MemoryStream( _ImageFileContent ) )
						using ( Nuaj.Helpers.TargaImage TGA = new Nuaj.Helpers.TargaImage( Stream ) )
						{
							// Create a default sRGB linear color profile
							m_ColorProfile = new ColorProfile(
									ColorProfile.Chromaticities.sRGB,					// Use default sRGB color profile
									ColorProfile.GAMMA_CURVE.STANDARD,	// But with a standard gamma curve
									TGA.ExtensionArea.GammaRatio						// Whose gamma is retrieved from extension data
								);

							// Convert
							byte[]	ImageContent = LoadBitmap( TGA.Image, out m_Width, out m_Height );
							m_Bitmap = new Vector4[m_Width,m_Height];
							byte	A;
							int		i = 0;
							for ( int Y=0; Y < m_Height; Y++ )
								for ( int X=0; X < m_Width; X++ )
								{
									m_Bitmap[X,Y].X = BYTE_TO_FLOAT * ImageContent[i++];
									m_Bitmap[X,Y].Y = BYTE_TO_FLOAT * ImageContent[i++];
									m_Bitmap[X,Y].Z = BYTE_TO_FLOAT * ImageContent[i++];

									A = ImageContent[i++];
									m_bHasAlpha |= A != 0xFF;

									m_Bitmap[X,Y].W = BYTE_TO_FLOAT * A;
								}

							// Convert to CIEXYZ
							m_ColorProfile.RGB2XYZ( m_Bitmap );
						}
					return;
				}

			case FILE_TYPE.HDR:
				{
					// Load as XYZ
					m_Bitmap = LoadAndDecodeHDRFormat( _ImageFileContent, true, out m_ColorProfile );
					m_Width = m_Bitmap.GetLength( 0 );
					m_Height = m_Bitmap.GetLength( 1 );
					return;
				}

			default:
				throw new NotSupportedException( "The image file type \"" + _FileType + "\" is not supported by the Bitmap class !" );
		}

		// Ensure we have a valid color profile !
		if ( m_ColorProfile == null )
			throw new NException( this, "Invalid profile : can't convert to CIEXYZ !" );

		// Convert the frame to RGBA32F
		m_Width = Frame.Size.Width;
		m_Height = Frame.Size.Height;

		float[]	TempBitmap = new float[m_Width*m_Height*4];
		Utilities.Pin<float>( TempBitmap, ( IntPtr _Pointer ) =>
		{
			using ( DataStream TargetStream = new DataStream( _Pointer, Utilities.SizeOf( TempBitmap ), false, true ) )
			{
				using ( FormatConverter Converter = new FormatConverter( m_Factory ) )
				{
					if ( !Converter.CanConvert( Frame.PixelFormat, GENERIC_PIXEL_FORMAT ) )
						throw new NException( this, "WIC Cannot convert from " + Frame.PixelFormat + " to " + GENERIC_PIXEL_FORMAT_NAME + " !" );

					Result	Res = Result.Ok;
					if ( (Res = Converter.Initialize( Frame, GENERIC_PIXEL_FORMAT )).Failure )
						throw new NException( this, "Conversion from " + Frame.PixelFormat + " to " + GENERIC_PIXEL_FORMAT_NAME + " failed with error code : " + Res.ToString() );

					Converter.CopyPixels( m_Width * Utilities.SizeOf<Vector4>(), TargetStream );
				}
			}
		} );

		// Build the target bitmap
		m_bHasAlpha = false;
		m_Bitmap = new Vector4[m_Width,m_Height];

		int		Position = 0;
		Vector4	Temp;
		for ( int Y=0; Y < m_Height; Y++ )
			for ( int X=0; X < m_Width; X++ )
			{
				Temp.X = TempBitmap[Position++];
				Temp.Y = TempBitmap[Position++];
				Temp.Z = TempBitmap[Position++];
				Temp.W = TempBitmap[Position++];
				m_Bitmap[X,Y] = Temp;

				if ( Temp.W != 1.0f )
					m_bHasAlpha = true;
			}

		// Convert to CIE XYZ
		m_ColorProfile.RGB2XYZ( m_Bitmap );
	}
	catch ( Exception )
	{
		throw;	// Go on !
	}
	finally
	{
		if ( Frame != null )
			Frame.Dispose();
	}
}
