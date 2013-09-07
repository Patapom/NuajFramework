//////////////////////////////////////////////////////////////////////////
// This examples demonstrates the use of the Nuaj.Bitmap class to obtain
//	linear space textures in your shaders.
//
// 
//
//////////////////////////////////////////////////////////////////////////
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

using Nuaj;

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// Cube primitive
		protected Texture2D<PF_RGB32F>		m_HDRTexture = null;
		protected Texture2D<PF_RGB32F>		m_KernelTexture = null;

		protected float						m_BlurRadius = 8.0f;

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public unsafe DemoForm()
		{
			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					OutputHandle = Handle,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, this ) );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}





			//////////////////////////////////////////////////////////////////////////
			// Test Bitmaps

			// All these bitmaps have been loaded and saved with Photoshop
//*
			string		SourceDirectory = "./Media/BitmapClassTest/TestWhite+Normal/";
			string[]	FileNames = new string[]
			{
				// ================= WHITE + NORMAL TEST =================
//				"White.jpg",									// No profile found
//				"White_sRGB.jpg",								// Found sRGB from the EXIF:ColorSpace tag (although there is also the photoshop:ICCProfile tag later)

//				"Ramp_Procedural.jpg",
// 				"Ramp_Procedural.png",
// 
				"Ramp_WorksRGB_StoreNoProfile.jpg",
				"Ramp_WorksRGB_StoresRGB.jpg",
				"Ramp_WorksRGB_StoreNoProfile.png",
				"Ramp_WorkAdobeRGB_StoreNoProfile.png",
				"Ramp_WorkProPhoto_StoreNoProfile.png",
// 
// 				"TestNormal_NEW_sRGB.jpg",
// 				"TestNormal_NEW_sRGB.png",
// 
// 				"TestNormal.jpg",
// 				"TestNormal_sRGB.jpg",
//				"TestNormal.gif",
// 				"TestNormal.bmp",
//				"TestNormal.tga",
// 				"TestNormal_StandardSave.png",					// Standard save, no sRGB
				"TestNormal.png",								// Save for Web with all metadata, no sRGB
				"TestNormal_nometa.png",						// Save for Web, no sRGB
				"TestNormal_sRGB.png",							// Save for Web with all metadata, sRGB
				"TestNormal_sRGB_nometa.png",					// Save for Web, sRGB
			};

			// Build 2 procedural gray ramps
			using ( Bitmap B = new Bitmap( 256, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb ) )
			{
				System.Drawing.Imaging.BitmapData	LockedBitmap = B.LockBits( new System.Drawing.Rectangle( 0, 0, 256, 1 ), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				for ( int Y=0; Y < 32; Y++ )
				{
					byte*	pScanline = (byte*) LockedBitmap.Scan0.ToPointer() + LockedBitmap.Stride * Y;
					for ( int X=0; X < 256; X++ )
					{
						*pScanline++ = (byte) X;
						*pScanline++ = (byte) X;
						*pScanline++ = (byte) X;
						*pScanline++ = 0xFF;
					}
				}

				B.UnlockBits( LockedBitmap );

				B.Save( SourceDirectory + "Ramp_Procedural.jpg", System.Drawing.Imaging.ImageFormat.Jpeg );
				B.Save( SourceDirectory + "Ramp_Procedural.png", System.Drawing.Imaging.ImageFormat.Png );
			}
//*/

/*
			string		SourceDirectory = "./Media/BitmapClassTest/FormatsSupport/";
			string[]	FileNames = new string[]
			{
// 				// ================= FORMATS TEST =================
// 				// JPG
// 				"Test.jpg",										// Standard JPG demonstrating a 2.2 gamma
// 				"Test_sRGB.jpg",								// Standard JPG demonstrating a 2.2 gamma
// 
// 				// PNG
// 				"Test_Alpha_8Bits.png",							// 8 bits per component with Alpha channel => Alpha is NOT present because for PNG to have alpha you need to set the alpha with a vector mask
// 				"Test_AlphaAsVectorMask_8Bits.png",				// 8 bits per component (normal save)
// 				"Test_AlphaAsVectorMask_8Bits_sRGB.png",		// 8 bits per component with "sRGB profile" (save for web)
// 				"Test_Alpha_16Bits.png",						// 16 bits per component with Alpha channel => Alpha is NOT present because for PNG to have alpha you need to set the alpha with a vector mask
// 				"Test_AlphaAsVectorMask_16Bits.png",			// 16 bits per component (normal save)
// 				"Test_AlphaAsVectorMask_16Bits_sRGB.png",		// 16 bits per component with "sRGB profile" (save for web)
// 
// 				// TGA
// 				"Test_Alpha_16bpp.tga",
// 				"Test_Alpha_24bpp.tga",
// 				"Test_Alpha_32bpp.tga",
// 				"Test_NoAlpha_16bpp.tga",
// 				"Test_NoAlpha_24bpp.tga",
// 				"Test_NoAlpha_32bpp.tga",
// 
// 				// TIFF (uncompressed)
// 				"Test_NoCompression_Alpha_16Bits.tif",
// 				"Test_NoCompression_Alpha_16Bits_sRGB.tif",
// 				"Test_NoCompression_Alpha_8Bits.tif",
// 				"Test_NoCompression_Alpha_8Bits_sRGB.tif",
// 				"Test_NoCompression_Alpha_F16.tif",
// 				"Test_NoCompression_Alpha_F16_sRGB.tif",
// 				"Test_NoCompression_Alpha_F24.tif",
// 				"Test_NoCompression_Alpha_F24_sRGB.tif",
// 				"Test_NoCompression_Alpha_F32.tif",
// 				"Test_NoCompression_Alpha_F32_sRGB.tif",
// 
// 				// TIFF (compressed)
// 				"Test_LZW_Alpha_F16.tif",
// 				"Test_LZW_Alpha_F16_sRGB.tif",
// 				"Test_LZW_Alpha_F24.tif",
// 				"Test_LZW_Alpha_F24_sRGB.tif",
// 				"Test_LZW_Alpha_F32.tif",
// 				"Test_LZW_Alpha_F32_sRGB.tif",
// 
// 				// HDR
// 				"Test.hdr",
// 
				// BMP
				"Test24.bmp",
				"Test32.bmp",

				// GIF
				"Test.gif",										// 256 colors GIF
			};
//*/
	
/*
			string		SourceDirectory = "./Media/BitmapClassTest/TestColorSpaces/";
 			string[]	FileNames = new string[]
			{
				// ================= COLOR SPACES TEST =================
				"ProPhoto_Test.hdr",

// 				// TargetAdobeRGB ("ICC Profile" option checked in save dialog) => Profile is correctly recognized as AdobeRGB for all formats
// 				"WorkAdobeRGB_TargetAdobeRGB_Source16_Target16.png",	// CORRECT => Adobe RGB read back from /cHRM meta
// 				"WorkAdobeRGB_TargetAdobeRGB_Source16_Target16.tif",	// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta
// 				"WorkAdobeRGB_TargetAdobeRGB_Source8_Target8.jpg",		// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta
// 				"WorkAdobeRGB_TargetAdobeRGB_Source8_Target8.tif",		// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta
// 				"WorkAdobeRGB_TargetNone_Source16_Target16.tif",		// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta
// 
// 				// TargetNone (i.e. "ICC profile" option unchecked in save dialog)
// 				// => Only PNG still embeds the profile in the /cHRM tag so we can identify it as Adobe RGB
// 				// => All other formats are assumed sRGB and this is wrong !
// 				"WorkAdobeRGB_TargetNone_Source32F_Target32F.tif",		// WRONG => Assumed sRGB profile with 2.2 gamma override
// 				"WorkAdobeRGB_TargetNone_Source8_Target8.jpg",			// WRONG => Assumed sRGB profile with 2.2 gamma override
// 				"WorkAdobeRGB_TargetNone_Source8_Target8.png",			// CORRECT => /cHRM guides our choice toward Adobe RGB default profile
// 				"WorkAdobeRGB_TargetNone_Source8_Target8.tif",			// WRONG => Assumed sRGB profile with 2.2 gamma override
// 
// 				"WorkAdobeRGB_TargetsRGB_Source32F_Target32F.tif",		// CORRECT => sRGB read back from "photoshop:ICCProfile" meta
// 
// 				"WorkProPhoto_TargetAdobeRGB_Source16_Target16.png",	// CORRECT => Adobe RGB read back from /cHRM meta
// 				"WorkProPhoto_TargetAdobeRGB_Source16_Target16.tif",	// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta
// 				"WorkProPhoto_TargetAdobeRGB_Source8_Target8.jpg",		// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta
// 				"WorkProPhoto_TargetAdobeRGB_Source8_Target8.png",		// CORRECT => Adobe RGB read back from /cHRM meta
// 				"WorkProPhoto_TargetAdobeRGB_Source8_Target8.tif",		// CORRECT => Adobe RGB read back from "photoshop:ICCProfile" meta (NOT PREMULTIPLIED !?)
// 
// 				"WorkProPhoto_TargetNone_Source16_Target16.tif",		// WRONG => Assumed sRGB profile with 2.2 gamma override
// 				"WorkProPhoto_TargetNone_Source32F_Target16F.tif",		// WRONG => Assumed sRGB profile with 2.2 gamma override
// 				"WorkProPhoto_TargetNone_Source32F_Target32F.tif",		// WRONG => Assumed sRGB profile with 2.2 gamma override
// 				"WorkProPhoto_TargetNone_Source8_Target8.jpg",			// WRONG => Assumed sRGB profile with 2.2 gamma override
// 				"WorkProPhoto_TargetNone_Source8_Target8.tif",			// WRONG => Assumed sRGB profile with 2.2 gamma override (NOT PREMULTIPLIED !?)
// 				"WorkProPhoto_TargetsRGB_Source32F_Target16F.tif",		// CORRECT => sRGB read back from "photoshop:ICCProfile" meta
// 				"WorkProPhoto_TargetsRGB_Source32F_Target32F.tif",		// CORRECT => sRGB read back from "photoshop:ICCProfile" meta


// TODO: Embedded ProPhoto profile shows as "Adobe RGB (1998) (Linear RGB Profile)" ?? WTF is that "Linear RGB Profile" that we also find on sRGB color profile images?
"WorkProPhoto_TargetProPhoto_Source32F_Target32F.tif",
			};
//*/

			//////////////////////////////////////////////////////////////////////////
			Bitmap2[]	Bitmaps = new Bitmap2[FileNames.Length];

			string		Errors = "";
			for ( int FileIndex=0; FileIndex < FileNames.Length; FileIndex++ )
			{
				System.IO.FileInfo	FileName = new System.IO.FileInfo( SourceDirectory + FileNames[FileIndex] );
				try
				{
					int	W, H;
					using ( Bitmap B = Bitmap.FromFile( FileName.FullName ) as Bitmap )
						Bitmap2.LoadBitmap( B, out W, out H );


					Bitmaps[FileIndex] = new Bitmap2( m_Device, "Bitmap#" + FileIndex, FileName );
				}
				catch ( Exception _e )
				{
					Errors += "Error loading \"" + FileName + "\" : " + _e.Message;
				}
			}






			//////////////////////////////////////////////////////////////////////////
			// Build the HDR texture to be post-processed
			System.IO.FileInfo	F = new System.IO.FileInfo( @".\Media\HDR Images\nave.hdr" );
//			System.IO.FileInfo	F = new System.IO.FileInfo( @".\Media\HDR Images\rosette.hdr" );
//			System.IO.FileInfo	F = new System.IO.FileInfo( @".\Media\HDR Images\vinesunset.hdr" );
//			System.IO.FileInfo	F = new System.IO.FileInfo( @".\Media\HDR Images\memorial.hdr" );

			byte[]	HDRFileContent = null;
			using ( System.IO.FileStream Stream = F.OpenRead() )
			{
				HDRFileContent = new byte[(int) Stream.Length];
				Stream.Read( HDRFileContent, 0, (int) Stream.Length );
			}

			Vector4[,]	HDRSource = Nuaj.Image<PF_RGBA32F>.LoadAndDecodeHDRFormat( HDRFileContent );
			using ( Nuaj.Image<PF_RGB32F> HDRImage = ToDispose( new Nuaj.Image<PF_RGB32F>( m_Device, "HDR Image", HDRSource, +1.0f, 1 ) ) )
			{
				m_HDRTexture = ToDispose( new Texture2D<PF_RGB32F>( m_Device, "HDR Texture", HDRImage ) );
			}

// 			//////////////////////////////////////////////////////////////////////////
// 			// Build the kernel texture
// 			using ( Nuaj.Image<PF_RGB32F> KernelImage = new Nuaj.Image<PF_RGB32F>( m_Device, "Kernel Image", Properties.Resources.BokehTest0, 0, 1.0f ) )
// 			{
// 				m_KernelTexture = ToDispose( new Texture2D<PF_RGB32F>( m_Device, "Kernel", KernelImage ) );
// 			}
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			//////////////////////////////////////////////////////////////////////////
			// Create a perspective camera
			Camera		Cam = ToDispose( new Camera( m_Device, "Default Camera" ) );
						Cam.CreatePerspectiveCamera( 0.5f * (float) Math.PI, (float) ClientSize.Width / ClientSize.Height, 0.01f, 100.0f );

			Cam.Activate();


			//////////////////////////////////////////////////////////////////////////
			// Create a fullscreen quad for post-processing test
			Nuaj.Helpers.ScreenQuad	PostProcessQuad = ToDispose( new Nuaj.Helpers.ScreenQuad( m_Device, "Post-Process Quad" ) );
			Material<VS_Pt4V3T2>	PostProcessMaterial_Defocus;
			Material<VS_Pt4V3T2>	PostProcessMaterial;
			try
			{
				PostProcessMaterial_Defocus = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Post-Process Material: Defocus", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/PostProcessDefocus/Defocus.fx" ) ) );
				PostProcessMaterial = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Post-Process Material: Display", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/PostProcessDefocus/Display.fx" ) ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );		// Disable depth-stencil

			//////////////////////////////////////////////////////////////////////////
			// Create temporary render targets for downscaling
			RenderTarget<PF_RGBA32F>	TempTarget0 = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "TempTarget0", m_HDRTexture.Width, m_HDRTexture.Height, 1 ) );
			RenderTarget<PF_RGBA32F>	TempTarget1 = ToDispose( new RenderTarget<PF_RGBA32F>( m_Device, "TempTarget1", m_HDRTexture.Width, m_HDRTexture.Height, 1 ) );

			Nuaj.Helpers.TextureScaler<PF_RGB32F,PF_RGBA32F>	ImageScaler = ToDispose( new Nuaj.Helpers.TextureScaler<PF_RGB32F,PF_RGBA32F>( m_Device, "ImageScaler", m_HDRTexture, TempTarget0, TempTarget1, Nuaj.Helpers.TextureScaler<PF_RGB32F,PF_RGBA32F>.QUALITY.DEFAULT, Nuaj.Helpers.TextureScaler<PF_RGB32F,PF_RGBA32F>.METHOD.DEFAULT ) );


			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============
				ShaderResourceView	ResultView = m_HDRTexture.TextureView;
				if ( m_BlurRadius > 0.0f )
				{
					const int	MAX_KERNEL_SAMPLES = 8;   // We can only use a maximum of 16 samples per dimension (so 256 samples for 2D kernels)

					int			DefocusPrecision = 1*1;
					int			KernelPassesCount = 1 + DefocusPrecision;

					float		fBlurSize = m_BlurRadius;


					// Compute image scale factors based on blur size and max kernel radius
					int   KernelSamplesCount = MAX_KERNEL_SAMPLES;					// By default, use the maximum amount of samples
					int   MaxKernelSize = KernelSamplesCount * KernelPassesCount;	// This is our final maximum kernel size
					float fImageFactor = 1.0f;										// By default, no downscaling of the source image

					if ( fBlurSize < MaxKernelSize )
					{	// Reduce the amount of samples for the kernel or it would be overdefinite...
						KernelSamplesCount = Math.Max( 1, (int) Math.Floor( MAX_KERNEL_SAMPLES * fBlurSize / MaxKernelSize ) );
						MaxKernelSize = KernelSamplesCount * KernelPassesCount;
					}

					if ( fBlurSize > MaxKernelSize )
						fImageFactor = MaxKernelSize / fBlurSize;					// DownSample the image so we can keep our kernel radius constant...

					// =============== Post-Process : DownSample source image ===============
					int		DownSampledWidth = (int) Math.Ceiling( fImageFactor * m_HDRTexture.Width );
					int		DownSampledHeight = (int) Math.Ceiling( fImageFactor * m_HDRTexture.Height );
					bool	bUseOriginalTexture = DownSampledWidth == m_HDRTexture.Width && DownSampledHeight == m_HDRTexture.Height;

					ImageScaler.Reset();
					ImageScaler.Scale( DownSampledWidth, DownSampledHeight, null );

					// =============== Post-Process : Apply defocus ===============
					float fScalePOT = -(float) (Math.Log( fImageFactor ) / Math.Log( 2.0 ));
					float fKernelMipLevel = Math.Max( 0.0f, fScalePOT );

					Defocus( DownSampledWidth, DownSampledHeight, fBlurSize, fKernelMipLevel, MaxKernelSize, ImageScaler.LastRenderedRenderTarget, ImageScaler.PreviouslyRenderedRenderTarget, PostProcessMaterial_Defocus, PostProcessQuad, bUseOriginalTexture );
					ImageScaler.SwapRenderTargets();

					// =============== Post-Process : UpSample source image ===============
					ImageScaler.Scale( m_HDRTexture.Width, m_HDRTexture.Height, null );

					ResultView = ImageScaler.LastRenderedRenderTarget.TextureView;
				}

				// =============== Post-process ===============
				m_Device.SetDefaultRenderTarget();

				VariableResource	vBackgroundTexture = PostProcessMaterial.GetVariableByName( "TexBackground" ).AsResource;
									vBackgroundTexture.SetResource( ResultView );

				using ( PostProcessMaterial.UseLock() )
					PostProcessMaterial.Render( ( _Sender, _Pass, _PassIndex ) => { PostProcessQuad.Render(); } );

				// Show !
				m_Device.Present();
			});
		}

		/// <summary>
		/// Performs defocus on an image
		/// </summary>
		/// <param name="_SourceWidth"></param>
		/// <param name="_SourceHeight"></param>
		/// <param name="_fBlurSize"></param>
		/// <param name="_fKernelMipIndex"></param>
		/// <param name="_TempTarget0"></param>
		/// <param name="_TempTarget1"></param>
		/// <param name="_DefocusMaterial"></param>
		/// <param name="_Quad"></param>
		/// <param name="_bUseOriginalTexture"></param>
		protected void	Defocus( int _SourceWidth, int _SourceHeight, float _fBlurSize, float _fKernelMipIndex, int _KernelSamplesCount, RenderTarget<PF_RGBA32F> _SourceTarget, RenderTarget<PF_RGBA32F> _DestTarget, Material<VS_Pt4V3T2> _DefocusMaterial, Nuaj.Helpers.ScreenQuad _Quad, bool _bUseOriginalTexture )
		{
			int	SourceImageWidth = _bUseOriginalTexture ? m_HDRTexture.Width : _SourceTarget.Width;
			int	SourceImageHeight = _bUseOriginalTexture ? m_HDRTexture.Height : _SourceTarget.Height;

			// Setup source data
			VariableResource		vSourceTexture = _DefocusMaterial.GetVariableByName( "TexSource" ).AsResource;
			vSourceTexture.SetResource( _bUseOriginalTexture ? m_HDRTexture.TextureView : _SourceTarget.TextureView );
			VariableResource		vKernelTexture = _DefocusMaterial.GetVariableByName( "TexKernel" ).AsResource;
			vKernelTexture.SetResource( m_KernelTexture.TextureView );
			VariableVector		vTextureFullSize = _DefocusMaterial.GetVariableByName( "TextureFullSize" ).AsVector;
			vTextureFullSize.Set( new Vector2( SourceImageWidth, SourceImageHeight ) );
			VariableVector		vTextureSubSize = _DefocusMaterial.GetVariableByName( "TextureSubSize" ).AsVector;
			vTextureSubSize.Set( new Vector2( _SourceWidth, _SourceHeight ) );

			VariableScalar		vKernelMipIndex = _DefocusMaterial.GetVariableByName( "fKernelMipIndex" ).AsScalar;
			vKernelMipIndex.Set( _fKernelMipIndex );
			VariableScalar		vKernelSamplesCount = _DefocusMaterial.GetVariableByName( "KernelSamplesCount" ).AsScalar;
			if ( vKernelSamplesCount != null )
				vKernelSamplesCount.Set( _KernelSamplesCount );

			VariableVector		vBlurSize = _DefocusMaterial.GetVariableByName( "BlurSize" ).AsVector;
			vBlurSize.Set( new Vector2( _fBlurSize * _SourceWidth / SourceImageWidth, _fBlurSize * _SourceHeight / SourceImageHeight ) );

			// Render
			m_Device.SetRenderTarget( _DestTarget );
			m_Device.SetViewport( 0, 0, _SourceWidth, _SourceHeight, 0.0f, 1.0f );

			_DefocusMaterial.Render( ( _Sender, _Pass, _PassIndex ) => { _Quad.Render(); } );
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		protected override void OnKeyDown( KeyEventArgs e )
		{
			base.OnKeyDown( e );
		}

		protected override void OnKeyPress( KeyPressEventArgs e )
		{
			base.OnKeyPress( e );

			if ( e.KeyChar == 0x2b )
				m_BlurRadius += 1.0f;
			else if ( e.KeyChar == 0x2d )
				m_BlurRadius -= 1.0f;

			m_BlurRadius = Math.Max( 0.0f, m_BlurRadius );
		}

		#endregion
	}
}
