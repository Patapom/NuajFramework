using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace BokehSamplesGenerator
{
	static class Program
	{
		[System.Diagnostics.DebuggerDisplay( "X={X} Y={Y}" )]
		class Sample
		{
			public float	X, Y;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static unsafe void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			string	Vectors = "";
			for ( int Z=-1; Z <= +1; Z++ )
				for ( int Y=-1; Y <= +1; Y++ )
					for ( int X=-1; X <= +1; X++ )
						if ( Z!=0 || Y!=0 || X!=0 )
						{
							float	fSize = (float) Math.Sqrt(X*X+Y*Y+Z*Z );
							float	fX = X / fSize;
							float	fY = Y / fSize;
							float	fZ = Z / fSize;

							Vectors += "float3( " + fX.ToString( "G5" ) + ", " + fY.ToString( "G5" ) + ", " + fZ.ToString( "G5" ) + " ),\r\n";
						}


			// Parameters
			Bitmap		BokehImage = Properties.Resources.Hexagon;
			int			Size = 11;
			float		fRotationAngle = 15.0f * (float) Math.PI / 180.0f;


			float	c = (float) Math.Cos( fRotationAngle );
			float	s = (float) Math.Sin( fRotationAngle );

			// Load the image in a nice array
			int			W = BokehImage.Width;
			int			H = BokehImage.Height;
			float[,]	Bokeh = new float[W,H];
			BitmapData	Lock = BokehImage.LockBits( new Rectangle( 0, 0, W, H ), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb );

			for ( int Y=0; Y < H; Y++ )
			{
				byte*	pScanline = (byte*) Lock.Scan0.ToPointer() + Lock.Stride * Y;
				for ( int X=0; X < W; X++, pScanline+=3 )
					Bokeh[X,Y] = *pScanline > 128 ? 1.0f : 0.0f;
			}
			BokehImage.UnlockBits( Lock );

			List<Sample>	Samples = new List<Sample>();
#if false
			// Downsample to some size and count resulting samples
			float		fJitterAmplitude = 0.1f;

			Random	RNG = new Random( 1 );
			for ( int Y=0; Y < Size; Y++ )
			{
				int	SourceY = Y * H / Size;
				for ( int X=0; X < Size; X++ )
				{
					int	SourceX = X * W / Size;
					if ( Bokeh[SourceX,SourceY] < 0.5f )
						continue;	// Not a sample

					float	JitterX = fJitterAmplitude * (2.0f * (float) RNG.NextDouble() - 1.0f);
					float	JitterY = fJitterAmplitude * (2.0f * (float) RNG.NextDouble() - 1.0f);

					// Get normalized coordinate
					float	NX = (2.0f * (SourceX + JitterX) - W) / W;
					float	NY = (H - 2.0f * (SourceY + JitterY)) / H;

					// Build a new sample
					Sample	S = new Sample();
					S.X = c * NX + s * NY;
					S.Y = -s * NX + c * NY;

					Samples.Add( S );
				}
			}
#else
			// Use bins and add samples to their corresponding bin
			List<Sample>[,]	BinSamples = new List<Sample>[Size,Size];
			for ( int Y=0; Y < H; Y++ )
			{
				int	BinY = Y * Size / H;
				for ( int X=0; X < W; X++ )
				{
					int	BinX = X * Size / W;
					if ( Bokeh[X,Y] < 0.5f )
						continue;	// Not a sample

					// Build a new sample using normalized coordinates
					Sample	S = new Sample();
					S.X = (2.0f * X - W) / W;
					S.Y = (H - 2.0f * Y) / H;

					if ( BinSamples[BinX,BinY] == null )
						BinSamples[BinX,BinY] = new List<Sample>();
					BinSamples[BinX,BinY].Add( S );
				}
			}

			// Sort best samples in each bin
			float	MaxDistance = 1.4143f / Size;
			float	MaxSqDistance = MaxDistance * MaxDistance;
			for ( int Y=0; Y < Size; Y++ )
			{
				float	BinCenterY = 1.0f - 2.0f * (Y+0.5f) / Size;
				for ( int X=0; X < Size; X++ )
				{
					float	BinCenterX = 2.0f * (X+0.5f) / Size - 1.0f;
					
					List<Sample>	ThisBin = BinSamples[X,Y];
					if ( ThisBin == null )
						continue;	// Empty bin...

#if true
					// Use an average position of all samples
					Sample	BestSample = new Sample();
					foreach ( Sample S in ThisBin )
					{
						BestSample.X += S.X;
						BestSample.Y += S.Y;
					}
					BestSample.X /= ThisBin.Count;
					BestSample.Y /= ThisBin.Count;
#else
					// Sample closest to center
					float	fBestSqDistance = float.MaxValue;
					Sample	BestSample = null;
					float	fSqDistance;
					foreach ( Sample S in ThisBin )
					{
						float	Dx = S.X - BinCenterX;
						float	Dy = S.Y - BinCenterY;
						fSqDistance = Dx*Dx + Dy*Dy;
						if ( fSqDistance > fBestSqDistance )
							continue;

						fBestSqDistance = fSqDistance;
						BestSample = S;
					}

					if ( BestSample == null )
						continue;

					if ( fBestSqDistance > MaxSqDistance )
						throw new Exception( "Best distance lies outside the bin's range !" );
#endif
					// Rotate sample
					Sample	RotatedSample = new Sample();
					RotatedSample.X = c * BestSample.X + s * BestSample.Y;
					RotatedSample.Y = -s * BestSample.X + c * BestSample.Y;

					Samples.Add( RotatedSample );
				}
			}
#endif

			// Piss out the samples as text
			string	ResultCode = "";
			foreach ( Sample S in Samples )
				ResultCode += "float2( " + S.X.ToString( "G4" ) + ", " + S.Y.ToString( "G4" ) + " ),\r\n";
		}
	}
}
