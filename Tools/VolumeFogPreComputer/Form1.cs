//////////////////////////////////////////////////////////////////////////
// This pre-computer is used to compute a volume texture that will encode
//  the perceived values of a fog with noise filling a cylinder. 
//
// This re-uses one of my old ideas to display volumetric lighting through a
//	colored stained-glass window by pre-computing a "tomographic section" of
//	the window.
//
// Here is a simple explanation of the technique :
//	For example, take the shape of a stained-glass window that looks like this :
//
//           U=0 | U=1
//            / /\ 
//           / /  \
//          / /    \  ._
//         / /      \ |\ U+
//        / /        \  \
//    U+ | |          | |
//       | |       Uin| |    ___ Camera
//       V |         _+_|---'
//         |  ___---' | |
//     ___-+-'        | |
//  <-'    |Uout      | |
//         |          | |
//         |          | |
//         |__________| |
//             U~0.5
//
// We parametrize the contour using the U coordinate that goes from 0 (top, going left)
//	to 1 (reaching the top again, from the right, after a full turn around the shape)
//
// Now imagine the interior of the shape is mapped with a stained-glass texture.
// What we're going to do is to assume the camera is orthogonal to the volume extruded
//	from that shape, so its rays go through the shape as shown by the ugly attempt at
//	drawing a transversal line.
//
// This ray has crosses the shape at 2 places that we name Uin and Uout.
//
// The amount of light perceived by the ray when it goes through a theoretical
//	volume extruded from the shape can be easily pre-computed for each pair of (Uin, Uout)
//  value. This yields a 2D texture, or "tomographic slice" of the texture.
//
// Of course, the camera rays are not all entering the volume orthogonally, so I added
//	also another parametrisation (V) along the extrusion direction where V=0 where the light
//	start, and V=D for any point at distance D from the window.
//
// Using now another pair (Vin, Vout) for the entry and exit points of the camera ray, I 
//	could approximate some factors to change the perceived light value. That is wrong of course
//	as you would have to really compute the integral of light scattering all over again, but
//	that was close enough for a nice volumetric effect (we're talking about something I did
//	in 1995 here ! I had no cool reference papers about light scattering or anything at the time)
//
// =================================================================================
// Well, 15 years later, here I go again with a lot of more math in my head, and a lot more power
//	in my PC ! Talk of 3D textures for example...
// What can we do with 3D textures now ?
//
// Take the example of a slab filled with volume fog whose density is guided by a low frequency noise.
// It's completely possible to precompute, say, a 32x32 texture that encodes the perceived accumulated density
//	of the noise for any ray that enters in a vertical plane perpendicular to the slab at Uin and exits at Uout
//
//         |                |  --- Camera
//         |              --|-
//         |          ---   | Uin
//         |       ---      |
//         |   ---          |
//    Uout ---              |
//     <-- |                |
//         |                |
//         |                |
//         |                |  <= Vertical slice of the slab
//
// If the noise is low frequency enough, it's okay to only have 32x32 possibilities...
// Now, for the 3rd dimension, well we simply encode a full noise dimension using 128 entries or something.
// That yields us a 32x32x128 texture, and if each encoded value is a simple 16 bits float, that
//	gives us a 256Kb texture.
//
// This slab is really made to be viewed orthogonally and should only be used "tied to the camera".
// To account for non orthogonal rays, I ponder that fetching 2 values from the table : 1 at point of entry
//  and the other one at point of exit, and blending the 2 together should give us a satisfying and coherent
//	"change in appearance" based on angular deviation.
//
// =================================================================================
// The volume rendering equation for radiance is :
//
// L(x,w) = Int{0,s}[ exp( -Tau(x,x') ).sigma_s(x').Int{0,4PI}[Phase(x',w,w').Li(x',w').dw'].dx' ]
//		  + Int{0,s}[ exp( -Tau(x,x') ).dx' ] * L(x+s.w,w)
//
// I make the assumption the Phase and incoming radiance are not dependent of the position so we come up with :
//
// L(x,w) = P(w).Li(w) * Int{0,s}[ exp( -Tau(x,x') ).sigma_s(x').dx' ]	<= in-scattering
//		  + L(x+s.w,w) * Int{0,s}[ exp( -Tau(x,x') ).dx' ]				<= extinction
//
// Tau(x,x') = Int{x,x'}[sigma_t(t).dt]
//
// We can therefore separate both terms into 2 values stored in the table : one for in-scattering and one for extinction.
// sigma_s(x) and sigma_t(x) are both values depending on the medium density at x.
// I define a ratio r = sigma_s / sigma_t with sigma_t fixed to 1.0, rather than specifying specific sigmas.
// This way I can vary both sigmas by multiplying the values found in the table.
//
// So we're left with computing extinction and extinction * sigma_s at each step.
// We do this by ray-marching the volume backward :
//
// For in-scattering	: L_n+1(x,w) = sigma_s(x).Dx + exp( -sigma_t(x).Dx ) * L_n(x+Dx,w)
//	with L_0(x+s.w,w) = 0  (no in-scattered light)
// For extinction		: L_n+1(x,w) = exp( _sigma_t(x).Dx ) * L_n(x+Dx,w)
//	with L_0(x+s.w,w) = 1  (full background transparency)
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;

namespace VolumeFogPreComputer
{
	public partial class Form1 : Form
	{
		#region CONSTANTS

		public const int		NOISE_TEXTURE_SIZE = 32;

// 		public const int		TEXTURE_SIZE = 32;
// 		public const int		SLICES_COUNT = 128;
// 		public const int		MARCH_STEPS_COUNT = 100;
// 
// 		public const float		SLAB_THICKNESS = 4.0f;
// 		public const float		SLAB_HEIGHT = 8.0f;
// 		public const float		SLAB_LENGTH = 32.0f;
// 		public const float		NOISE_SIZE = 2.0f;
// 
// 		public const double		MAX_EXTINCTION = 0.60;		// Maximum extinction readable from noise
// 		public const double		SCATTERING_RATIO = 0.45;	// Ratio of scattering over extinction (always < 1)

		#endregion

		#region NESTED TYPES
		#endregion

		#region FIELDS

		protected Vector4[][,,]	m_NoiseTables = new Vector4[4][,,];
		protected Vector2[,,]	m_VolumeTable = null;

		#endregion

		#region METHODS

		public Form1()
		{
			InitializeComponent();
			BuildNoise();

// 			Application.Idle += new EventHandler(Application_Idle);
//			Application_Idle(this, EventArgs.Empty);
		}
		
		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
		}

		protected bool		m_bComputed = false;

		protected void  Application_Idle(object sender, EventArgs e)
		{
			if ( m_bComputed )
				return;

			buttonCompute_Click( this, EventArgs.Empty );

			m_bComputed = true;
		}

		#region Noise Computation

		protected void	BuildNoise()
		{
			Random	RNG = new Random( 1 );
			for ( int NoiseTableIndex=0; NoiseTableIndex < 4; NoiseTableIndex++ )
			{
				Vector4[,,]	Table = new Vector4[NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE,NOISE_TEXTURE_SIZE];
				m_NoiseTables[NoiseTableIndex] = Table;

				// Build initial noise
				for ( int Z=0; Z < NOISE_TEXTURE_SIZE; Z++ )
					for ( int Y=0; Y < NOISE_TEXTURE_SIZE; Y++ )
						for ( int X=0; X < NOISE_TEXTURE_SIZE; X++ )
							Table[X,Y,Z].X = (float) RNG.NextDouble();

				// Pad with neighbor values
				for ( int Z=0; Z < NOISE_TEXTURE_SIZE; Z++ )
					for ( int Y=0; Y < NOISE_TEXTURE_SIZE; Y++ )
						for ( int X=0; X < NOISE_TEXTURE_SIZE; X++ )
						{
							Table[X,Y,Z].Y = Table[X,(Y+1) & (NOISE_TEXTURE_SIZE-1),Z].X;								// +Y
							Table[X,Y,Z].Z = Table[X,Y,(Z+1) & (NOISE_TEXTURE_SIZE-1)].X;								// +Z
							Table[X,Y,Z].W = Table[X,(Y+1) & (NOISE_TEXTURE_SIZE-1),(Z+1) & (NOISE_TEXTURE_SIZE-1)].X;	// +YZ
						}
			}
		}

		// Noise + Derivatives
		// From Iñigo Quilez (http://www.iquilezles.org/www/articles/morenoise/morenoise.htm)
		//
		protected float Noise( Vector3 _UVW, Vector4[,,] _Noise, out Vector3 _Derivatives ) 
		{
			int		X = (int) Math.Floor( _UVW.X );
			int		Y = (int) Math.Floor( _UVW.Y );
			int		Z = (int) Math.Floor( _UVW.Z );

			Vector3	uvw;
			uvw.X = _UVW.X - X;
			uvw.Y = _UVW.Y - Y;
			uvw.Z = _UVW.Z - Z;

			X &= (NOISE_TEXTURE_SIZE-1);
			Y &= (NOISE_TEXTURE_SIZE-1);
			Z &= (NOISE_TEXTURE_SIZE-1);
			int		NX = (X+1) & (NOISE_TEXTURE_SIZE-1);

			Vector4	N0 = _Noise[X,Y,Z];
			Vector4	N1 = _Noise[NX,Y,Z];

			// Quintic interpolation from Ken Perlin :
			//	u(x) = 6x^5 - 15x^4 + 10x^3			<= This equation has 0 first and second derivatives if x=0 or x=1
			//	du/dx = 30x^4 - 60x^3 + 30x^2
			//
			Vector3	dudvdw;
			dudvdw.X = 30.0f*uvw.X*uvw.X*(uvw.X*(uvw.X-2.0f)+1.0f);
			dudvdw.Y = 30.0f*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y-2.0f)+1.0f);
			dudvdw.Z = 30.0f*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z-2.0f)+1.0f);

			uvw.X = uvw.X*uvw.X*uvw.X*(uvw.X*(uvw.X*6.0f-15.0f)+10.0f);
			uvw.Y = uvw.Y*uvw.Y*uvw.Y*(uvw.Y*(uvw.Y*6.0f-15.0f)+10.0f);
			uvw.Z = uvw.Z*uvw.Z*uvw.Z*(uvw.Z*(uvw.Z*6.0f-15.0f)+10.0f);

			float	a = N0.X;
			float	b = N1.X;
			float	c = N0.Y;
			float	d = N1.Y;
			float	e = N0.Z;
			float	f = N1.Z;
			float	g = N0.W;
			float	h = N1.W;

			float	k0 =   a;
			float	k1 =   b - a;
			float	k2 =   c - a;
			float	k3 =   e - a;
			float	k4 =   a - b - c + d;
			float	k5 =   a - c - e + g;
			float	k6 =   a - b - e + f;
			float	k7 = - a + b + c - d + e - f - g + h;

			_Derivatives.X = dudvdw.X * (k1 + k4*uvw.Y + k6*uvw.Z + k7*uvw.Y*uvw.Z);
			_Derivatives.Y = dudvdw.Y * (k2 + k5*uvw.Z + k4*uvw.X + k7*uvw.Z*uvw.X);
			_Derivatives.Z = dudvdw.Z * (k3 + k6*uvw.X + k5*uvw.Y + k7*uvw.X*uvw.Y);

			return k0 + k1*uvw.X + k2*uvw.Y + k3*uvw.Z + k4*uvw.X*uvw.Y + k5*uvw.Y*uvw.Z + k6*uvw.Z*uvw.X + k7*uvw.X*uvw.Y*uvw.Z;
		}

		#endregion
 
// 		uniform vec3	dUV = vec3( 1.0 / Width, 1.0 / Height, 0.0 );
// 		uniform vec3	LightPosition = vec3( 0.5, 0.5, 4.0 );	// Ici, le Z du vecteur détermine la hauteur de la lampe par rapport à ton plan (X,Y sont exprimés comme des UV : 0.5 c'est le centre)
// 
// 		float	GetHeight( vec2 _UV )
// 		{
// 			return sin( _UV.x ) * sin( _UV.y );	// Do some choucroutage with a nice function here...
// 		}
// 
// 		vec4	MonPixelShader()
// 		{
// 			vec2	UV = UV du pixel courant;
// 
// 			const int	KERNEL_HALF_SIZE = 4;
// 			const float AO_SHADOWING_FACTOR = 1.0;
// 			const float NORMAL_STRENGTH = 1.0;
// 
// 			// Compute current height
// 			float	PixelHeight = GetHeight( UV );
// 
// 			// Compute pixel normal
// 			float	HX0 = GetHeight( UV - dUV.xz );	// Left height
// 			float	HX1 = GetHeight( UV + dUV.xz );	// Right height
// 			float	HY0 = GetHeight( UV - dUV.zy );	// Top height
// 			float	HY1 = GetHeight( UV + dUV.zy );	// Bottom height
// 
// 			vec3	Dx = vec3( NORMAL_STRENGTH * dUV.x, 0.0, HX1 - HX0 );
// 			vec3	Dy = vec3( 0.0, NORMAL_STRENGTH * dUV.y, HY1 - HY0 );
// 			vec3	Normal = normalize( cross( Dx, Dy ) );
// 
// 			// Compute AO
// 			float	AO = 1.0;	// Start with full ambient
// 			for ( int Y=-KERNEL_HALF_SIZE; Y <= KERNEL_HALF_SIZE; Y++ )
// 				for ( int X=-KERNEL_HALF_SIZE; X <= KERNEL_HALF_SIZE; X++ )
// 					AO -= AO_SHADOWING_FACTOR * max( 0.0, GetHeight( UV + X * dUV.xz + Y * dUV.zy ) - PixelHeight );
// 
// 			// Compute lighting
// 			vec3	LightColor = vec3( 1.0, 1.0, 1.0 );
// 			vec3	ToLight = normalize( LightPosition - vec3( UV, 0.0 ) );
// 			float	Diffuse = max( 0.0, dot( ToLight, Normal ) );
// 
// 			return vec4( 0.5 * AO.xxx + Diffuse * LightColor, 1.0 );
// 		}

		protected override void OnPreviewKeyDown( PreviewKeyDownEventArgs e )
		{
			base.OnPreviewKeyDown( e );

			switch ( e.KeyCode )
			{
				case Keys.Add:
					break;
				case Keys.Subtract:
					break;
			}
		}

		/// <summary>
		/// Saves a freshly computed 3D scattering table
		/// </summary>
		/// <param name="_TableName"></param>
		/// <param name="_Table"></param>
		protected void SaveTable( string _TableName )
		{
//			string TargetPath = "../../Tables/" + _TableName;
			string TargetPath = "../../../../Runtime/Data/WaterColour/" + _TableName;

			System.IO.FileInfo File = new System.IO.FileInfo( TargetPath );
			System.IO.FileStream Stream = File.Create();
			System.IO.BinaryWriter Writer = new System.IO.BinaryWriter( Stream );

			int	LX = m_VolumeTable.GetLength( 0 );
			Writer.Write( LX );
			int	LY = m_VolumeTable.GetLength( 1 );
			Writer.Write( LY );
			int	LZ = m_VolumeTable.GetLength( 2 );
			Writer.Write( LZ );
			for ( int X=0; X < LX; X++ )
				for ( int Y=0; Y < LY; Y++ )
					for ( int Z=0; Z < LZ; Z++ )
					{
						Writer.Write( m_VolumeTable[X,Y,Z].X );
						Writer.Write( m_VolumeTable[X,Y,Z].Y );
					}

			Writer.Close();
			Writer.Dispose();
			Stream.Close();
			Stream.Dispose();
		}

		#endregion

		#region EVENT HANDLERS

		private void buttonCompute_Click( object sender, EventArgs e )
		{
			this.Enabled = false;

			int		SliceSize = integerTrackbarControlSliceSize.Value;
			int		SlicesCount = integerTrackbarControlSlicesCount.Value;

			m_VolumeTable = new Vector2[SliceSize,SliceSize,SlicesCount];

			float	SlabLength = floatTrackbarControlSlabLength.Value;
			float	SlabHeight = floatTrackbarControlSlabHeight.Value;
			float	SlabThickness = floatTrackbarControlSlabThickness.Value;
			float	ScatteringRatio = floatTrackbarControlInScatteringRatio.Value;
			int		MarchStepsCount = integerTrackbarControlMarchingStepsCount.Value;

			for ( int SliceIndex=0; SliceIndex < SlicesCount; SliceIndex++ )
			{
				float	Z = SlabLength * SliceIndex / SlicesCount;
				for ( int X=0; X < SliceSize; X++ )
				{
					float	Yin = SlabHeight * ((float) X / (SliceSize-1) - 0.5f);
					Vector3	In = new Vector3( 0.0f, Yin, Z );

					for ( int Y=0; Y < SliceSize; Y++ )
					{
						float	Yout = SlabHeight * ((float) Y / (SliceSize-1) - 0.5f);
						Vector3	Out = new Vector3( SlabThickness, (float) Yout, Z );

						Vector3	Out2In = In - Out;
						float	StepLength = Out2In.Length() / (MarchStepsCount+1);
						Vector3	MarchStep = Out2In / (MarchStepsCount+1);
						Vector3	Current = Out + 0.5f * MarchStep;	// March a little inside

						double	Extinction = 1.0;
						double	InScattering = 0.0;
						double	DeltaExtinction = 0.0;
						double	Sigma_t = 0.0;
						double	Sigma_s = 0.0;
						for ( int MarchStepIndex=0; MarchStepIndex < MarchStepsCount; MarchStepIndex++ )
						{
							// Get extinction coefficient value at position
							Sigma_t = ComputeExtinctionCoefficient( Current );
							Sigma_s = ScatteringRatio * Sigma_t;

							// Compute small extinction/in-scattering step
							DeltaExtinction = Math.Exp( -Sigma_t * StepLength );

							Extinction *= DeltaExtinction;
							InScattering = InScattering * DeltaExtinction + Sigma_s * StepLength;

							// March one step to entry point
							Current += MarchStep;
						}

						m_VolumeTable[X,Y,SliceIndex].X = (float) Extinction;
						m_VolumeTable[X,Y,SliceIndex].Y = (float) InScattering;
					}
				}
			}

			SaveTable( "VolumeFog0.fog" );

			this.Enabled = true;
		}

		/// <summary>
		/// Performs FBM
		/// </summary>
		protected Vector3	m_Derivatives;
		protected double	ComputeExtinctionCoefficient( Vector3 _Position )
		{
			if ( checkBoxTube.Checked )
			{
				float	SlabLength = floatTrackbarControlSlabLength.Value;
//				float	SlabHeight = 0.5f * floatTrackbarControlSlabHeight.Value;
				float	SlabDepth = 0.5f * floatTrackbarControlSlabThickness.Value;
				float	SlabRadius = SlabDepth * 0.5f * (1.0f + (float) Math.Cos( 2.0 * Math.PI * _Position.Z / SlabLength ));

				return (_Position - new Vector3( SlabDepth, 0.0f, _Position.Z )).LengthSquared() < SlabRadius*SlabRadius ? floatTrackbarControlMaxExtinction.Value : 0.0;
			}

			_Position *= floatTrackbarControlNoiseSize.Value;

			return floatTrackbarControlMaxExtinction.Value * Saturate( floatTrackbarControlNoiseScale.Value *
				(
					1.00f  * Noise( 1.0f * _Position, m_NoiseTables[0], out m_Derivatives ) +
					0.50f  * Noise( 2.0f * _Position, m_NoiseTables[1], out m_Derivatives ) +
					0.25f  * Noise( 4.0f * _Position, m_NoiseTables[2], out m_Derivatives ) +
					0.125f * Noise( 8.0f * _Position, m_NoiseTables[3], out m_Derivatives )
				) - floatTrackbarControlNoiseOffset.Value );
		}

		protected double	Saturate( double v )
		{
			return Math.Max( 0.0, Math.Min( 1.0, v ) );
		}

		#endregion
	}
}
