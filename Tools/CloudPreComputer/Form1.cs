//////////////////////////////////////////////////////////////////////////
// 
// At last, I got my idea right (hopefully) !
// 
// The goal of this precomputer is to generate a 3D table for light scattering through a slab of cloud
// This table will contain Spherical Harmonics coefficients to dot with the actual light coefficients
//	to yield the energy perceived by the observer through this slab of cloud.
// 
// As stated in "Real-time realistic illumination and shading of stratiform clouds" by Bouthors et Al. (2006),
//	the RGB variance of Mie scattering through a cloud tends to vanish when considering the effective phase
//	function using a droplet size distribution model. So we only need to store 1 set of SH coefficients instead
//	of 3 for RGB (that's why clouds are shades of grey).
// 
// Also, using SH instead of a specific lighting direction, we account for sky light as well as sun light and
//	we don't need to consider the light direction at all, which alleviates the table of 1 or 2 dimensions,
//	depending on the assumptions we're making.
// 
// The idea is to compute a table that will solve in-scattering SH coefficients for a viewer positioned at the
//	entrance of the slab, at a given height (H) within a slab of a given thickness (T) and of given length (L).
// H, T and L are our 3 varying parameters for the 3D table.
// 
// We don't consider the orientation of the viewer as the slab is always parallel to the view direction.
// H and T are sensitive parameters so they are finely represented in the table (i.e. they are the main 2 dimensions)
//	while L is discretized using fixed steps of specified length (say, 8 meters for example).
// 
//     ^
//     |
//     |   v
//     o===+============================+=====
//     |   |                            |
//     |   |    v                       |
// E --+--------+-----------------------+---------> View direction
//     |   |    |                       |
//     |   |    | H (height)            |
//     |   |    |                       |
//     |   | T (thickness)              |
//     |   |    |                       |
//     o===+====+=======================+=====---->
//     |   ^    ^                       |
//    >|--------------------------------|<
//                L (length)
// 
// Only the front hemisphere of directions is accounted for : rays coming from backward (i.e. behind the viewer)
//	are ignored.
// These rays are important for the simulation though, but a clever use of the table allows us to account for them
//	easily : we simply reverse the view direction and use the table the other way round.
// 
// Using the table for runtime is done this way :
//
// Ray march the view direction from the exit point back to the entry point of the viewing ray using
//	fixed steps of length equal to the length we discretized L with (e.g. 8 meters).
//
// That means :
// For each step
//   Energy *= Extinction along last step (i.e. exp( -sigma * Dx ) with Dx = step length)
//   Compute slab thickness and view height for current step
//	 Energy += dot( SHLight(view), ReadTable( StepIndex * StepLength, T, H )  <= samples the table in view direction (in-scattering from forward directions)
//	 Energy += dot( SHLight(-view), ReadTable( L-StepIndex * StepLength, T, H )  <= samples the table in opposite view direction (in-scattering from backward directions)
//  
// And you're done !
// 
//
// ---------------------------------------------------------------------------------------
// Computing the table
// Making the scattering events computation from O(N^S) to O(S*N)
// 
// Say you have N sampling rays to cast from each point of interest to compute the in-scattering at that point.
// Normally, single scattering goes this way :
//  Ray march from back to front using fixed steps and at each point, sample the incoming energy from all directions using N rays
//
// Doing this for each T and H entry in the slab using M steps of ray-marching costs (T*H*M)*N rays. That's a lot, but that's not the real problem here.
// In order to compute 2nd order scattering (i.e. rays that get bent twice before reaching the viewer's eye), you need to use that same process (i.e.
//	ray-march and launch rays) for every ray you originaly cast in the single scattering process.
// 
// That means, for each single-scattering ray you need to march M steps and throw N rays, so M*N rays per single scattering ray.
// But we already launch M*N single scattering rays, that means we need (M*N)^2 rays for double scattering !
// Well, yes. And that's the main problem as, in order to simulate clouds properly, you need a lot of scattering events (about 30).
// That means you would need to cast about (M*N)^30 rays to simulate the whole process. And you need to do that for T*H possibilities in the table.
// 
// Useless to say you won't have enough time in your life to compute even a hundredth of that table !
// And that's where I come in to save your wasted life ! ^__^
// 
// The idea consists in re-using any previously computed table (scattering order-1) to compute the current scattering order.
// 
// Indeed, double scattering along a single direction consists in ray-marching that direction, and for every position to launch
//	rays, and for each of these rays, ray-march them and launch secondary rays. A O(N^2) process as we said.
// 
// But, instead of ray-marching secondary rays, why don't we simply re-use the table of single scattering events that we just computed ?
// This table helps to compute just that : the energy perceived by the viewer at a given position within the cloud for rays that have
//	been scattered exactly once.
// 
// The process then boils down to ray-marching the view direction and, for each position along the ray, to use the previous table to
//	retrieve the energy due to single-scattering along this ray.
// Eventually, you only need to ray-march the view direction and cast N rays : a O(N) process...
// 
// Why stop there ? It's exactly as straightforward to use the now easily computed 2nd scattering table to compute the 3rd scattering table, and so on.
//
// And that's how we transformed a O(N^S) process into a O(S*N) process, and they lived happily ever after...
// 
// NOTE: The final table, concentrating all the scattering orders is simply the accumulation of all the individual scattering tables.
// ¨¨¨¨
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CloudPreComputer
{
	public partial class Form1 : Form
	{
		#region CONSTANTS

		public const int		SH_ORDER = 3;
		public const int		SH_SQORDER = SH_ORDER*SH_ORDER;
		public const int		SH_SAMPLES_THETA_COUNT = 20;	// Total amount of rays cast = 2*SH_SAMPLES_THETA_COUNT*SH_SAMPLES_THETA_COUNT
//		public const int		SH_SAMPLES_THETA_COUNT = 4;	// Total amount of rays cast = 2*SH_SAMPLES_THETA_COUNT*SH_SAMPLES_THETA_COUNT
		public const double		SLAB_MAX_THICKNESS = 400.0;		// Maximum encoded thickness
		public const double		SLAB_MIN_THICKNESS = 2.0;		// Minimum encoded thickness
		public const int		SLAB_TEXTURE_SIZE = 128;		// Thickness and height will be encoded in a 128x128 texture for each length step
		public const double		SLAB_STEP_LENGTH = 8.0;			// Each slab length is a multiple of this step (same step we use for ray-marching)
		public const int		SLAB_TEXTURE_DEPTH = 32;		// Length will be encoded in 32 slices of the 128x128 textures
		public const double		SLAB_MAX_DEPTH = SLAB_TEXTURE_DEPTH * SLAB_STEP_LENGTH;		// Maximum encoded length

		public const int		SCATTERING_EVENTS_COUNT = 30;	// The amount of computed scattering events


		// The total size of the 3D texture is computed like this :
		// Size = SLAB_TEXTURE_SIZE * SLAB_TEXTURE_SIZE * SLAB_TEXTURE_DEPTH * PixelSize
		// PixelSize = RGBE encoding of 9 SH coefficients = RGBE + RGBE + RGBE = 3*4
		// So for a 128x128x32 texture, the size in memory will be 6,291,456 bytes

		// Scattering & Extinction coefficients computation
		protected const double	N0						= 300.0 * 1e6;						// Density of droplets = 300cm-3
		protected const double	Re						= 7.0 * 1e-6;						// Droplets effective radius = 7µm
		protected const double	EXTINCTION_CROSS_SECTION= 0.5 * Math.PI * Re * Re;			// Extinction Cross Section = Pi * Re * Re (see that as a disc covering a single droplet)
														// Notice the 0.5 factor here to take into account the fact that we removed the forward peak of the Mie phase function (induces a reduced cross section)
		protected const double	EXTINCTION_COEFFICIENT	= N0 * EXTINCTION_CROSS_SECTION;	// Extinction Coefficient in m^-1 (Sigma = 0.046181412007769960605400857734209)
		protected const double	SCATTERING_COEFFICIENT	= EXTINCTION_COEFFICIENT;			// Scattering and extinction are almost the same in clouds because of albedo almost == 1

		#endregion

		#region NESTED TYPES

		public class SHVector
		{
			public double[]	V = new double[SH_SQORDER];

			public void	Read( System.IO.BinaryReader _Reader )
			{
				for ( int SHCoeffIndex=0; SHCoeffIndex < SH_SQORDER; SHCoeffIndex++ )
					V[SHCoeffIndex] = _Reader.ReadDouble();
			}
			public void	Write( System.IO.BinaryWriter _Writer )
			{
				for ( int SHCoeffIndex=0; SHCoeffIndex < SH_SQORDER; SHCoeffIndex++ )
					_Writer.Write( V[SHCoeffIndex] );
			}
			public static SHVector operator+( SHVector _V0, SHVector _V1 )
			{
				SHVector	R = new SHVector();
				for ( int SHCoeffIndex=0; SHCoeffIndex < SH_SQORDER; SHCoeffIndex++ )
					R.V[SHCoeffIndex] = _V0.V[SHCoeffIndex] + _V1.V[SHCoeffIndex];

				return R;
			}
		}

		#endregion

		#region FIELDS

		// Single scattering samples & phase factors
		protected SphericalHarmonics.SHSamplesCollection.SHSample[]	m_SamplesSingle = null;
		protected double[]		m_PhaseFactorsSingle = null;

		// Multiple scattering samples & phase factors (we only keep the FORWARD samples and the phase integrates to 0.25 instead of 0.5)
		protected SphericalHarmonics.SHSamplesCollection.SHSample[]	m_Samples = null;
		protected double[]		m_PhaseFactors = null;

		protected WMath.Matrix3x3[]	m_RotationMatrices = null;
		protected double[][,]	m_SHRotationForward = null;
		protected double[][,]	m_SHRotationBackward = null;

		#endregion

		#region METHODS

		public Form1()
		{
			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Initialize the SH samples
			SphericalHarmonics.SHSamplesCollection	SamplesCollection = new SphericalHarmonics.SHSamplesCollection( 1 );
			SamplesCollection.Initialize( SH_ORDER, SH_SAMPLES_THETA_COUNT );

			// Only keep forward samples
			List<SphericalHarmonics.SHSamplesCollection.SHSample>	ForwardSamples = new List<SphericalHarmonics.SHSamplesCollection.SHSample>();
			foreach ( SphericalHarmonics.SHSamplesCollection.SHSample Sample in SamplesCollection )
				if ( Sample.m_Direction.x >= 0.0f )
					ForwardSamples.Add( Sample );

			m_Samples = ForwardSamples.ToArray();
			m_SamplesSingle = SamplesCollection.Samples;	// Copy all samples for single scattering...

			//////////////////////////////////////////////////////////////////////////
			// Initialize the phase functions
			Atmospheric.PhaseFunction	Phase = new Atmospheric.PhaseFunction();

			//////////////////////////////////////////////////////////////////////////
			// Pre-compute phase factors and make sure they integrate to 0.25
			// They must not integrate to 1 as we clipped the tip of the phase function below 5° and this
			//	tip represents about 50% of the energy gained through strong forward scattering
			// Also, we only kept the forward samples as the table stores only half the hemisphere of
			//	incoming directions. The runtime table is actually accessed twice in both directions
			//	and the contributions from forward and backward directions are added together, this
			//	way we get the entire sphere of directions whose contribution sums up to 0.5.
			//
			// Finally, an additional 50% energy missing from single scattering will be added on top of the
			//	value retrieved from the scattering table, through analytic computation...
			//
			m_PhaseFactors = new double[m_Samples.Length];
 			Phase.Init( Atmospheric.CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION, 5.0f * (float) Math.PI / 180.0f, (float) Math.PI, 1024 );

			double	IntegralCheck = 0.0;
			for ( int PhaseFactorIndex=0; PhaseFactorIndex < m_Samples.Length; PhaseFactorIndex++ )
			{
				float	fAngle = (float) Math.Acos( -m_Samples[PhaseFactorIndex].m_Direction.x );
				m_PhaseFactors[PhaseFactorIndex] = Phase.GetPhaseFactor( fAngle );
				IntegralCheck += m_PhaseFactors[PhaseFactorIndex] * Math.Sin( fAngle );
			}

			double	fNormalizationFactor = 0.5 / IntegralCheck;
			IntegralCheck = 0.0;
			for ( int PhaseFactorIndex=0; PhaseFactorIndex < m_Samples.Length; PhaseFactorIndex++ )
			{
				float	fAngle = (float) Math.Acos( -m_Samples[PhaseFactorIndex].m_Direction.x );
				m_PhaseFactors[PhaseFactorIndex] *= fNormalizationFactor;
				IntegralCheck += m_PhaseFactors[PhaseFactorIndex] * Math.Sin( fAngle );
			}
			// IntegralCheck should be 0.5
	
			// Single scattering now...
			m_PhaseFactorsSingle = new double[m_SamplesSingle.Length];
 			Phase.Init( Atmospheric.CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION, 0.0f * (float) Math.PI / 180.0f, (float) Math.PI, 1024 );

			IntegralCheck = 0.0;
			for ( int PhaseFactorIndex=0; PhaseFactorIndex < m_SamplesSingle.Length; PhaseFactorIndex++ )
			{
				float	fAngle = (float) Math.Acos( -m_SamplesSingle[PhaseFactorIndex].m_Direction.x );
				m_PhaseFactorsSingle[PhaseFactorIndex] = Phase.GetPhaseFactor( fAngle );
				IntegralCheck += m_PhaseFactorsSingle[PhaseFactorIndex] * Math.Sin( fAngle );
			}

			fNormalizationFactor = 1.0 / IntegralCheck;
			IntegralCheck = 0.0;
			for ( int PhaseFactorIndex=0; PhaseFactorIndex < m_SamplesSingle.Length; PhaseFactorIndex++ )
			{
				float	fAngle = (float) Math.Acos( -m_SamplesSingle[PhaseFactorIndex].m_Direction.x );
				m_PhaseFactorsSingle[PhaseFactorIndex] *= fNormalizationFactor;
				IntegralCheck += m_PhaseFactorsSingle[PhaseFactorIndex] * Math.Sin( fAngle );
			}
			// IntegralCheck should be 1.0

			// Pre-compute rotation matrices of SH coefficients, 2 for each sample direction (1 forward and 1 backward)
			m_RotationMatrices = new WMath.Matrix3x3[m_Samples.Length];
			m_SHRotationForward = new double[m_Samples.Length][,];
			m_SHRotationBackward = new double[m_Samples.Length][,];

			WMath.Matrix3x3	Rotation = new WMath.Matrix3x3();

			for ( int SHSampleIndex=0; SHSampleIndex < m_Samples.Length; SHSampleIndex++ )
			{
				SphericalHarmonics.SHSamplesCollection.SHSample	Sample = m_Samples[SHSampleIndex];

				// The rotation matrix should be built upon the opposite rotation induced by the sample's direction
				BuildInverseRotationMatrix( Sample.m_Direction, Rotation );
				m_SHRotationForward[SHSampleIndex] = new double[SH_SQORDER,SH_SQORDER];
				SphericalHarmonics.SHFunctions.BuildRotationMatrix( Rotation, m_SHRotationForward[SHSampleIndex], SH_ORDER );

				// Keep the actual rotation matrix
				m_RotationMatrices[SHSampleIndex] = Rotation.Invert();

				// Same here except the samples's direction is reversed
				BuildInverseRotationMatrix( -Sample.m_Direction, Rotation );
				m_SHRotationBackward[SHSampleIndex] = new double[SH_SQORDER,SH_SQORDER];
				SphericalHarmonics.SHFunctions.BuildRotationMatrix( Rotation, m_SHRotationBackward[SHSampleIndex], SH_ORDER );
			}
		}

		/// <summary>
		/// Builds a rotation matrix by extracting the Y and Z vectors from the provided X vector
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Rotation"></param>
		protected void	BuildInverseRotationMatrix( WMath.Vector _X, WMath.Matrix3x3 _Rotation )
		{
			_Rotation.SetRow0( _X );
			WMath.Vector	Z = (_X ^ new WMath.Vector( 0,1,0 )).Normalize();
			_Rotation.SetRow2( Z );
			_Rotation.SetRow1( Z ^ _X );
			_Rotation.Invert();
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			// Let's compute when we're ready...
//			Application.Idle += new EventHandler(Application_Idle);

			// View mode
			outputPanel.m_ComputedTable = new SHVector[SLAB_TEXTURE_SIZE,SLAB_TEXTURE_SIZE,SLAB_TEXTURE_DEPTH];
//			ViewTable( 1, outputPanel.m_ComputedTable );

			// Accumulate final table
//			BuildFinalTable( outputPanel.m_ComputedTable );
			LoadTable( "AccumulatedScattering.sh", outputPanel.m_ComputedTable );
			ViewTable( outputPanel.m_ComputedTable );
		}

		protected bool		m_bComputed = false;

		void  Application_Idle(object sender, EventArgs e)
		{
			if ( m_bComputed )
				return;	// Already computed...


// Start computing from there
int	StartScatteringEventIndex = 1;


			DateTime	StartTime = DateTime.Now;

			//////////////////////////////////////////////////////////////////////////
			// Compute single scattering
			SHVector[,,]	Table = new SHVector[SLAB_TEXTURE_SIZE,SLAB_TEXTURE_SIZE,SLAB_TEXTURE_DEPTH];

			if ( StartScatteringEventIndex == 0 )
			{	// Compute the initial single-scattering table
				StartSingleScatteringThreads( Table );
				SaveTable( "Scattering1.sh", Table );
			}
			else
			{	// Reload previous table
				ViewTable( StartScatteringEventIndex, Table );
				Application.DoEvents();
			}

			//////////////////////////////////////////////////////////////////////////
			// Build multiple scattering event tables based on previous table
			for ( int ScatteringEventsIndex=Math.Max( 1, StartScatteringEventIndex ); ScatteringEventsIndex <= SCATTERING_EVENTS_COUNT; ScatteringEventsIndex++ )
			{
				SHVector[,,]	PreviousTable = Table;
				Table = new SHVector[SLAB_TEXTURE_SIZE,SLAB_TEXTURE_SIZE,SLAB_TEXTURE_DEPTH];

				// Build slices
				StartMultipleScatteringThreads( Table, PreviousTable, StartTime, ScatteringEventsIndex, StartScatteringEventIndex );
				SaveTable( "Scattering" + (ScatteringEventsIndex+1) + ".sh", Table );
			}

			m_bComputed = true;
		}

		/// <summary>
		/// Reloads and display a computed table
		/// </summary>
		/// <param name="_ScatteringEventIndex"></param>
		/// <param name="_Table"></param>
		protected void		ViewTable( int _ScatteringEventIndex, SHVector[,,] _Table )
		{
			LoadTable( "Scattering" + _ScatteringEventIndex + ".sh", _Table );
			outputPanel.m_Title = "Reloaded Scattering Order " + _ScatteringEventIndex;
			ViewTable( _Table );
		}
		protected void		ViewTable( SHVector[,,] _Table )
		{
			outputPanel.m_ComputedTable = _Table;
			for ( int i=0; i < SLAB_TEXTURE_DEPTH; i++ )
				outputPanel.m_bComputedDepthSlices[i] = true;
			outputPanel.UpdateBitmap();
		}

		public const int	THREADS_COUNT = 4;
		protected bool[]	m_bComputedSlices = new bool[SLAB_TEXTURE_DEPTH];
		protected int		m_ComputedSlicesCount = 0;
		protected System.Threading.Mutex	m_SliceMutex = new System.Threading.Mutex();

		protected void	StartSingleScatteringThreads( SHVector[,,] _Table )
		{
			outputPanel.m_ComputedTable = _Table;
			Array.Clear( outputPanel.m_bComputedDepthSlices, 0, SLAB_TEXTURE_DEPTH );
			Array.Clear( m_bComputedSlices, 0, SLAB_TEXTURE_DEPTH );

			DateTime	StartTime = DateTime.Now;
			bool		bNeedsRefresh = false;

			// Compute the thickness factor
			// Thickness is actually stored exponentially in the table using  T = MAX * exp( -k.(1-x) )
			// We pose that T = MIN if x=0, so MIN = MAX * exp( -k )
			// Hence, k = -ln( MIN / MAX )
			//
			double	ThicknessExpFactor = Math.Log( SLAB_MIN_THICKNESS / SLAB_MAX_THICKNESS );

			// Build the threads
			System.Threading.Thread[]	Threads = new System.Threading.Thread[THREADS_COUNT];
			int[]						ComputingSliceIndex = new int[THREADS_COUNT];

			for ( int ThreadIndex=0; ThreadIndex < THREADS_COUNT; ThreadIndex++ )
			{
				Threads[ThreadIndex] = new System.Threading.Thread( ( object _ThreadIndex ) =>
				{
					int	OurThreadIndex = (int) _ThreadIndex;
					do
					{
						// Access the array of remaining slices to compute
						m_SliceMutex.WaitOne();
						int	SliceIndex = -1;
						for ( int SliceToComputeIndex=0; SliceToComputeIndex < SLAB_TEXTURE_DEPTH; SliceToComputeIndex++ )
							if ( !m_bComputedSlices[SliceToComputeIndex] )
							{
								m_bComputedSlices[SliceToComputeIndex] = true;	// We're computing that one !
								SliceIndex = SliceToComputeIndex;
								break;
							}

						ComputingSliceIndex[OurThreadIndex] = SliceIndex;
						m_SliceMutex.ReleaseMutex();

						if ( SliceIndex == -1 )
							return;	// We're done !

						// Compute the slice
						for ( int ThicknessIndex=0; ThicknessIndex < SLAB_TEXTURE_SIZE; ThicknessIndex++ )
						{
							double	fThicknessIndex = 1.0 - (double) ThicknessIndex / (SLAB_TEXTURE_SIZE-1);
							double	T = SLAB_MAX_THICKNESS * Math.Exp( ThicknessExpFactor * fThicknessIndex );

							for ( int HeightIndex=0; HeightIndex < SLAB_TEXTURE_SIZE; HeightIndex++ )
							{
								double	H = T * HeightIndex / (SLAB_TEXTURE_SIZE-1);

								// Here ! We have our 3 parameters L, T and H to compute for...
								_Table[ThicknessIndex,HeightIndex,SliceIndex] = new SHVector();
								ComputeSingleScattering( _Table[ThicknessIndex,HeightIndex,SliceIndex], SliceIndex, T, H );
							}
						}

						// Give feedback
						m_SliceMutex.WaitOne();
					
						m_ComputedSlicesCount++;	// One more computed slice
						outputPanel.m_bComputedDepthSlices[SliceIndex] = true;

						TimeSpan	ETA = EstimateRemainingTime( StartTime, m_ComputedSlicesCount, SLAB_TEXTURE_DEPTH );
						outputPanel.m_Title = "Scattering Order 1 Slice #" + SliceIndex + " (" + ((1+SliceIndex)*SLAB_STEP_LENGTH) + " meters) - StartTime="
							+ StartTime.ToString( "HH:mm:ss" )
							+ " Est. EndTime=" + (StartTime + ETA).ToString( "HH:mm:ss" )
							+ " (" + FormatTimeSpan( EstimateRemainingTime( StartTime, m_ComputedSlicesCount, SLAB_TEXTURE_DEPTH ) ) + ")";
						bNeedsRefresh = true;

						m_SliceMutex.ReleaseMutex();

					} while ( true );	// Loop to next slice to compute...
				} );

				Threads[ThreadIndex].Name = "Slice Thread #" + ThreadIndex;
				Threads[ThreadIndex].Priority = System.Threading.ThreadPriority.Highest;

				// Start the thread
				Threads[ThreadIndex].Start( ThreadIndex );
			}

			// Loop until all threads are done...
			int	ComputingThreadsCount = 0;;
			do
			{
				System.Threading.Thread.Sleep( 5000 );	// Check every half second

				// Check for refresh
				m_SliceMutex.WaitOne();
				
				if ( bNeedsRefresh )
				{
					outputPanel.m_Title += "\r\n";
					for ( int ThreadIndex=0; ThreadIndex < THREADS_COUNT; ThreadIndex++ )
						outputPanel.m_Title += "Thread #" + ThreadIndex + " Computing slice " + ComputingSliceIndex[ThreadIndex] + "\r\n";
					outputPanel.m_Title += "(ThreadsCount = " + ComputingThreadsCount + ")";
					outputPanel.UpdateBitmap();
					Application.DoEvents();
				}
				bNeedsRefresh = false;

				m_SliceMutex.ReleaseMutex();

				// Check if threads are still alive
				ComputingThreadsCount = 0;
				foreach ( System.Threading.Thread T in Threads )
					if ( T.IsAlive )
						ComputingThreadsCount++;

			} while ( ComputingThreadsCount > 0 );
		}

		protected void	StartMultipleScatteringThreads( SHVector[,,] _Table, SHVector[,,] _PreviousTable, DateTime _GlobalStartTime, int _ScatteringEventIndex, int _StartScatteringEventIndexIndex )
		{
			outputPanel.m_ComputedTable = _Table;
			Array.Clear( outputPanel.m_bComputedDepthSlices, 0, SLAB_TEXTURE_DEPTH );
			Array.Clear( m_bComputedSlices, 0, SLAB_TEXTURE_DEPTH );

			DateTime	StartTime = DateTime.Now;
			bool		bNeedsRefresh = false;

// DEBUG TEST
//ComputeMultipleScattering( _PreviousTable, new SHVector(), 0, 0, 0 );
// DEBUG TEST

			// Compute the thickness factor
			// Thickness is actually stored exponentially in the table using  T = MAX * exp( -k.(1-x) )
			// We pose that T = MIN if x=0, so MIN = MAX * exp( -k )
			// Hence, k = -ln( MIN / MAX )
			//
			double	ThicknessExpFactor = Math.Log( SLAB_MIN_THICKNESS / SLAB_MAX_THICKNESS );

			// Build the threads
			System.Threading.Thread[]	Threads = new System.Threading.Thread[THREADS_COUNT];
			int[]						ComputingSliceIndex = new int[THREADS_COUNT];

			for ( int ThreadIndex=0; ThreadIndex < THREADS_COUNT; ThreadIndex++ )
			{
				Threads[ThreadIndex] = new System.Threading.Thread( ( object _ThreadIndex ) =>
				{
					int	OurThreadIndex = (int) _ThreadIndex;
					do
					{
						// Access the array of remaining slices to compute
						m_SliceMutex.WaitOne();
						int	SliceIndex = -1;
						for ( int SliceToComputeIndex=0; SliceToComputeIndex < SLAB_TEXTURE_DEPTH; SliceToComputeIndex++ )
							if ( !m_bComputedSlices[SliceToComputeIndex] )
							{
								m_bComputedSlices[SliceToComputeIndex] = true;	// We're computing that one !
								SliceIndex = SliceToComputeIndex;
								break;
							}

						ComputingSliceIndex[OurThreadIndex] = SliceIndex;
						m_SliceMutex.ReleaseMutex();

						if ( SliceIndex == -1 )
							return;	// We're done !

						// Compute the slice
						for ( int ThicknessIndex=0; ThicknessIndex < SLAB_TEXTURE_SIZE; ThicknessIndex++ )
						{
							double	fThicknessIndex = 1.0 - (double) ThicknessIndex / (SLAB_TEXTURE_SIZE-1);
							double	T = SLAB_MAX_THICKNESS * Math.Exp( ThicknessExpFactor * fThicknessIndex );

							for ( int HeightIndex=0; HeightIndex < SLAB_TEXTURE_SIZE; HeightIndex++ )
							{
								double	H = T * HeightIndex / (SLAB_TEXTURE_SIZE-1);

								// Here ! We have our 3 parameters L, T and H to compute for...
								_Table[ThicknessIndex,HeightIndex,SliceIndex] = new SHVector();
								ComputeMultipleScattering( _PreviousTable, _Table[ThicknessIndex,HeightIndex,SliceIndex], SliceIndex, T, H );
							}
						}

						// Give feedback
						m_SliceMutex.WaitOne();
					
						m_ComputedSlicesCount++;	// One more computed slice
						outputPanel.m_bComputedDepthSlices[SliceIndex] = true;

						int	ComputedSlicesCount = SLAB_TEXTURE_DEPTH * (_ScatteringEventIndex-_StartScatteringEventIndexIndex) + m_ComputedSlicesCount;
						int	TotalSlicesCount = SLAB_TEXTURE_DEPTH * (SCATTERING_EVENTS_COUNT-_StartScatteringEventIndexIndex);
						TimeSpan	GlobalETA = EstimateRemainingTime( _GlobalStartTime, ComputedSlicesCount, TotalSlicesCount );
						TimeSpan	ETA = EstimateRemainingTime( StartTime, m_ComputedSlicesCount, SLAB_TEXTURE_DEPTH );

						outputPanel.m_Title = "Scattering Order " + (_ScatteringEventIndex+1) + " Slice #" + SliceIndex + " (" + ((1+SliceIndex)*SLAB_STEP_LENGTH) + " meters)"
							+ "\r\n	SliceStartTime=" + StartTime.ToString( "HH:mm:ss" ) + " Est. SliceEndTime=" + (StartTime+ETA).ToString( "HH:mm:ss" ) + " (" + FormatTimeSpan( ETA ) + ")"
							+ "\r\n	TotalStartTime=" + _GlobalStartTime.ToString( "HH:mm:ss" ) + " Est. TotalEndTime=" + (_GlobalStartTime+GlobalETA).ToString( "HH:mm:ss" ) + " (" + FormatTimeSpan( GlobalETA ) + ")";
						bNeedsRefresh = true;

						m_SliceMutex.ReleaseMutex();

					} while ( true );	// Loop to next slice to compute...
				} );

				Threads[ThreadIndex].Name = "Slice Thread #" + ThreadIndex;
				Threads[ThreadIndex].Priority = System.Threading.ThreadPriority.Highest;

				// Start the thread
				Threads[ThreadIndex].Start( ThreadIndex );
			}

			// Loop until all threads are done...
			int	ComputingThreadsCount = 0;;
			do
			{
				System.Threading.Thread.Sleep( 500 );	// Check every half second

				// Check for refresh
				m_SliceMutex.WaitOne();
				
				if ( bNeedsRefresh )
				{
					outputPanel.m_Title += "\r\n";
					for ( int ThreadIndex=0; ThreadIndex < THREADS_COUNT; ThreadIndex++ )
						outputPanel.m_Title += "Thread #" + ThreadIndex + " Computing slice " + ComputingSliceIndex[ThreadIndex] + "\r\n";
					outputPanel.m_Title += "(ThreadsCount = " + ComputingThreadsCount + ")";
					outputPanel.UpdateBitmap();
					Application.DoEvents();
				}
				bNeedsRefresh = false;

				m_SliceMutex.ReleaseMutex();

				// Check if threads are still alive
				ComputingThreadsCount = 0;
				foreach ( System.Threading.Thread T in Threads )
					if ( T.IsAlive )
						ComputingThreadsCount++;

			} while ( ComputingThreadsCount > 0 );
		}

		protected TimeSpan	EstimateRemainingTime( DateTime _StartTime, int _PerformedSteps, int _TotalSteps )
		{
			TimeSpan	ElapsedTime = DateTime.Now - _StartTime;
			TimeSpan	SingleStepTime = new TimeSpan( ElapsedTime.Ticks / _PerformedSteps );
			return new TimeSpan( SingleStepTime.Ticks * (_TotalSteps - _PerformedSteps) );
		}

		protected string	FormatTimeSpan( TimeSpan _Span )
		{
			return _Span.ToString( @"hh\:mm\:ss" );
		}

		/// <summary>
		/// Computes single scattering through a slab of given length and thickness, viewed at a given height
		/// </summary>
		/// <param name="_Vector">The SH vector to fill up with results</param>
		/// <param name="_DepthIndex">Depth index</param>
		/// <param name="T">Slab thickness</param>
		/// <param name="H">Viewer height (within slab thickness)</param>
		protected void	ComputeSingleScattering( SHVector _Vector, int _DepthIndex, double T, double H )
		{
			double	L = (1+_DepthIndex) * SLAB_STEP_LENGTH;	// Slab length in meters

			// Extinction by marching 1 step
			double	ExtinctionFactor = Math.Exp( -2.0 * EXTINCTION_COEFFICIENT * SLAB_STEP_LENGTH );
			double	InScatteringFactor = 2.0 * EXTINCTION_COEFFICIENT * SLAB_STEP_LENGTH;	// In-scattering received along a single step

			double	MinHitLength = +double.MaxValue;
			double	MaxHitLength = -double.MaxValue;
			double	AvgHitLength = 0.0;
			double	TotalAvgHitLength = 0.0;

			// Start at the end, back half the first step
			double	X = L - 0.5*SLAB_STEP_LENGTH;
			for ( int StepIndex=_DepthIndex; StepIndex >= 0; StepIndex-- )
			{
				// Apply extinction of energy gathered from previous step
				for ( int l=0; l < SH_SQORDER; l++ )
					_Vector.V[l] *= ExtinctionFactor;

				// Sample some directions
				AvgHitLength = 0.0;
				for ( int SampleIndex=0; SampleIndex < m_SamplesSingle.Length; SampleIndex++ )
				{
					SphericalHarmonics.SHSamplesCollection.SHSample Sample = m_SamplesSingle[SampleIndex];

					// Compute hit direction with the slab in the sample's direction
					double	HitDistance = ComputeHitDistance( Sample.m_Direction, X, L, T, H );

					// Compute the attenuation resulting from following that sample until it exits the slab
					double	fInScatteredEnergy = InScatteringFactor * m_PhaseFactorsSingle[SampleIndex] * Math.Exp( -2.0 * EXTINCTION_COEFFICIENT * HitDistance );

					// Accumulate SH coefficients
					for ( int l=0; l < SH_SQORDER; l++ )
						_Vector.V[l] += fInScatteredEnergy * Sample.m_SHFactors[l];

					// Stats
					MinHitLength = Math.Min( MinHitLength, HitDistance );
					MaxHitLength = Math.Max( MaxHitLength, HitDistance );
					AvgHitLength += HitDistance;
				}

				TotalAvgHitLength += AvgHitLength;
				AvgHitLength /= m_SamplesSingle.Length;

				// March backward
				X -= SLAB_STEP_LENGTH;
			}

			TotalAvgHitLength /= m_SamplesSingle.Length * (_DepthIndex+1);
		}

		/// <summary>
		/// Computes multiple scattering through a slab of given length and thickness, viewed at a given height
		/// </summary>
		/// <param name="_PreviousScatteringTable">The previous scattering table of an order less than currently computed order</param>
		/// <param name="_Vector">The SH vector to fill up with results</param>
		/// <param name="_DepthIndex">Depth index</param>
		/// <param name="T">Slab thickness</param>
		/// <param name="H">Viewer height (within slab thickness)</param>
		protected void	ComputeMultipleScattering( SHVector[,,] _PreviousScatteringTable, SHVector _Vector, int _DepthIndex, double T, double H )
		{
			double	L = (1+_DepthIndex) * SLAB_STEP_LENGTH;	// Slab length in meters

			// Extinction by marching 1 step
			double	ExtinctionFactor = Math.Exp( -EXTINCTION_COEFFICIENT * SLAB_STEP_LENGTH );
			double	InScatteringFactor = EXTINCTION_COEFFICIENT * SLAB_STEP_LENGTH;	// In-scattering received along a single step

			double	MinHitLength = +double.MaxValue;
			double	MaxHitLength = -double.MaxValue;
			double	AvgHitLength = 0.0;
			double	TotalAvgHitLength = 0.0;

			// Start at the end, back half the first step
			double	X = L - 0.5*SLAB_STEP_LENGTH;
			for ( int StepIndex=_DepthIndex; StepIndex >= 0; StepIndex-- )
			{
				// Apply extinction of energy gathered from previous step
				for ( int l=0; l < SH_SQORDER; l++ )
					_Vector.V[l] *= ExtinctionFactor;

				// Sample some directions
				AvgHitLength = 0.0;
				for ( int SampleIndex=0; SampleIndex < m_Samples.Length; SampleIndex++ )
				{
					SphericalHarmonics.SHSamplesCollection.SHSample Sample = m_Samples[SampleIndex];
					WMath.Matrix3x3	SampleRotation = m_RotationMatrices[SampleIndex];

// DEBUG TEST
// L = 8 * SLAB_STEP_LENGTH;
// X = 0.5 * L;
// T = 200.0;
// H = 100.0;
// // Sample.m_Direction.Set( 1, 0, 0 );
// // SampleRotation.MakeIdentity();
// Sample.m_Direction.Set( 0, 0, 1 );
// Sample.m_Direction.Normalize();
// BuildInverseRotationMatrix( Sample.m_Direction, SampleRotation );
// SampleRotation.Invert();
// DEBUG TEST

					// Compute hit direction with the slab in the sample's 6 main directions
					double	HitDistanceX0 = ComputeHitDistance( Sample.m_Direction, X, L, T, H );
					double	HitDistanceX1 = ComputeHitDistance( -Sample.m_Direction, X, L, T, H );
					double	HitDistanceY0 = ComputeHitDistance( SampleRotation.GetRow1(), X, L, T, H );
					double	HitDistanceY1 = ComputeHitDistance( -SampleRotation.GetRow1(), X, L, T, H );
					double	HitDistanceZ0 = ComputeHitDistance( SampleRotation.GetRow2(), X, L, T, H );
					double	HitDistanceZ1 = ComputeHitDistance( -SampleRotation.GetRow2(), X, L, T, H );

					// Devise the length, thickness & height of the slab along the ray
					double	SlabSizeY = HitDistanceY0 + HitDistanceY1;
					double	SlabSizeZ = HitDistanceZ0 + HitDistanceZ1;
					double	SlabThickness = 0.5 * (SlabSizeY + SlabSizeZ);	// An average of both sizes along Y and Z gives us the approximate diameter of the secondary slab cylinder
					double	SlabHeight = 0.5 * (HitDistanceY1 / SlabSizeY + HitDistanceZ1 / SlabSizeZ) * SlabThickness;

					// Fetch in-scattering from previous table with these slab data, both for backward and forward directions
					double	fInScatteredEnergyFactor = InScatteringFactor * m_PhaseFactors[SampleIndex];
					ComputeInScatteredEnergy(	_PreviousScatteringTable,
												HitDistanceX0, SlabThickness, SlabHeight,
												fInScatteredEnergyFactor,
												m_SHRotationForward[SampleIndex],
												_Vector );	// Forward lobe
					ComputeInScatteredEnergy(	_PreviousScatteringTable,
												HitDistanceX1, SlabThickness, SlabHeight,
												fInScatteredEnergyFactor,
												m_SHRotationBackward[SampleIndex],
												_Vector );	// Backward lobe

					// Stats
					MinHitLength = Math.Min( MinHitLength, HitDistanceX0 );
					MinHitLength = Math.Min( MinHitLength, HitDistanceX1 );
					MaxHitLength = Math.Max( MaxHitLength, HitDistanceX0 );
					MaxHitLength = Math.Max( MaxHitLength, HitDistanceX1 );
					AvgHitLength += 0.5*(HitDistanceX0+HitDistanceX1);
				}

				TotalAvgHitLength += AvgHitLength;
				AvgHitLength /= m_Samples.Length;

				// March backward
				X -= SLAB_STEP_LENGTH;
			}

			TotalAvgHitLength /= m_Samples.Length * (_DepthIndex+1);
		}

		/// <summary>
		/// Retrieve in-scattered energy from a previously computed table
		/// The SH coefficients from the table are then rotated and accumulated with a factor to the provided SH vector
		/// </summary>
		/// <param name="_Table">The previously computed table to retrieve the energy from</param>
		/// <param name="_Length">The length of the slab to fetch</param>
		/// <param name="_Thickness">The thickness of the slab to fetch</param>
		/// <param name="_Height">The height of the observer within that slab</param>
		/// <param name="_Factor">The factor to apply to retrieved SH coefficients</param>
		/// <param name="_SHRotation">The rotation to apply to retrieved SH coefficients</param>
		/// <param name="_SH">The SH vector where to accumulate the energy</param>
		protected double[]	m_TempSH = new double[SH_SQORDER];
		protected double[]	m_TempRotatedSH = new double[SH_SQORDER];
		protected void	ComputeInScatteredEnergy( SHVector[,,] _Table, double _Length, double _Thickness, double _Height, double _Factor, double[,] _SHRotation, SHVector _SH )
		{
			// Fetch SH from the table
			int		X = (int) (SLAB_TEXTURE_SIZE * (1.0 - Math.Log( SLAB_MAX_THICKNESS / _Thickness ) / Math.Log( SLAB_MAX_THICKNESS / SLAB_MIN_THICKNESS )));
			X = Math.Min( SLAB_TEXTURE_SIZE-1, X );
			int		Y = (int) (SLAB_TEXTURE_SIZE * _Height / _Thickness);
			Y = Math.Min( SLAB_TEXTURE_SIZE-1, Y );
			int		Z0 = (int) Math.Floor( _Length / SLAB_STEP_LENGTH );
			double	Dz = (_Length - Z0 * SLAB_STEP_LENGTH) / SLAB_STEP_LENGTH;
			double	RDz = 1.0 - Dz;
			Z0 = Math.Min( SLAB_TEXTURE_DEPTH-1, Z0 );
			int		Z1 = Math.Min( SLAB_TEXTURE_DEPTH-1, Z0+1 );

			SHVector	V0 = _Table[X,Y,Z0];
			SHVector	V1 = _Table[X,Y,Z1];
			for ( int l=0; l < SH_SQORDER; l++ )
				m_TempSH[l] = V0.V[l] * RDz + V1.V[l] * Dz;

			// Rotate SH using the provided matrix
			CompactRotate( m_TempSH, _SHRotation, m_TempRotatedSH );

			// Accumulate SH coefficients
			for ( int l=0; l < SH_SQORDER; l++ )
				_SH.V[l] += _Factor * m_TempRotatedSH[l];
		}

		// Hardcoded rotation of SH coefficients using the precomputed rotation matrix
		// This works only for SH order of 3!
		public void		CompactRotate( double[] _Coeffs, double[,] _RotationMatrix, double[] _RotatedCoeffs )
		{
			_RotatedCoeffs[0] = _Coeffs[0] * _RotationMatrix[0,0];

			_RotatedCoeffs[1] = _Coeffs[1] * _RotationMatrix[1+0,1+0] + _Coeffs[2] * _RotationMatrix[1+1,1+0] + _Coeffs[3] * _RotationMatrix[1+2,1+0];
			_RotatedCoeffs[2] = _Coeffs[1] * _RotationMatrix[1+0,1+1] + _Coeffs[2] * _RotationMatrix[1+1,1+1] + _Coeffs[3] * _RotationMatrix[1+2,1+1];
			_RotatedCoeffs[3] = _Coeffs[1] * _RotationMatrix[1+0,1+2] + _Coeffs[2] * _RotationMatrix[1+1,1+2] + _Coeffs[3] * _RotationMatrix[1+2,1+2];

			_RotatedCoeffs[4] = _Coeffs[4] * _RotationMatrix[4+0,4+0] + _Coeffs[5] * _RotationMatrix[4+1,4+0] + _Coeffs[6] * _RotationMatrix[4+2,4+0] + _Coeffs[7] * _RotationMatrix[4+3,4+0] + _Coeffs[8] * _RotationMatrix[4+4,4+0];
			_RotatedCoeffs[5] = _Coeffs[4] * _RotationMatrix[4+0,4+1] + _Coeffs[5] * _RotationMatrix[4+1,4+1] + _Coeffs[6] * _RotationMatrix[4+2,4+1] + _Coeffs[7] * _RotationMatrix[4+3,4+1] + _Coeffs[8] * _RotationMatrix[4+4,4+1];
			_RotatedCoeffs[6] = _Coeffs[4] * _RotationMatrix[4+0,4+2] + _Coeffs[5] * _RotationMatrix[4+1,4+2] + _Coeffs[6] * _RotationMatrix[4+2,4+2] + _Coeffs[7] * _RotationMatrix[4+3,4+2] + _Coeffs[8] * _RotationMatrix[4+4,4+2];
			_RotatedCoeffs[7] = _Coeffs[4] * _RotationMatrix[4+0,4+3] + _Coeffs[5] * _RotationMatrix[4+1,4+3] + _Coeffs[6] * _RotationMatrix[4+2,4+3] + _Coeffs[7] * _RotationMatrix[4+3,4+3] + _Coeffs[8] * _RotationMatrix[4+4,4+3];
			_RotatedCoeffs[8] = _Coeffs[4] * _RotationMatrix[4+0,4+4] + _Coeffs[5] * _RotationMatrix[4+1,4+4] + _Coeffs[6] * _RotationMatrix[4+2,4+4] + _Coeffs[7] * _RotationMatrix[4+3,4+4] + _Coeffs[8] * _RotationMatrix[4+4,4+4];

// 			_RotatedCoeffs[ 9] = _Coeffs[9] * _RotationMatrix[9+0,9+0] + _Coeffs[10] * _RotationMatrix[9+1,9+0] + _Coeffs[11] * _RotationMatrix[9+2,9+0] + _Coeffs[12] * _RotationMatrix[9+3,9+0] + _Coeffs[13] * _RotationMatrix[9+4,9+0] + _Coeffs[14] * _RotationMatrix[9+5,9+0] + _Coeffs[15] * _RotationMatrix[9+6,9+0];
// 			_RotatedCoeffs[10] = _Coeffs[9] * _RotationMatrix[9+0,9+1] + _Coeffs[10] * _RotationMatrix[9+1,9+1] + _Coeffs[11] * _RotationMatrix[9+2,9+1] + _Coeffs[12] * _RotationMatrix[9+3,9+1] + _Coeffs[13] * _RotationMatrix[9+4,9+1] + _Coeffs[14] * _RotationMatrix[9+5,9+1] + _Coeffs[15] * _RotationMatrix[9+6,9+1];
// 			_RotatedCoeffs[11] = _Coeffs[9] * _RotationMatrix[9+0,9+2] + _Coeffs[10] * _RotationMatrix[9+1,9+2] + _Coeffs[11] * _RotationMatrix[9+2,9+2] + _Coeffs[12] * _RotationMatrix[9+3,9+2] + _Coeffs[13] * _RotationMatrix[9+4,9+2] + _Coeffs[14] * _RotationMatrix[9+5,9+2] + _Coeffs[15] * _RotationMatrix[9+6,9+2];
// 			_RotatedCoeffs[12] = _Coeffs[9] * _RotationMatrix[9+0,9+3] + _Coeffs[10] * _RotationMatrix[9+1,9+3] + _Coeffs[11] * _RotationMatrix[9+2,9+3] + _Coeffs[12] * _RotationMatrix[9+3,9+3] + _Coeffs[13] * _RotationMatrix[9+4,9+3] + _Coeffs[14] * _RotationMatrix[9+5,9+3] + _Coeffs[15] * _RotationMatrix[9+6,9+3];
// 			_RotatedCoeffs[13] = _Coeffs[9] * _RotationMatrix[9+0,9+4] + _Coeffs[10] * _RotationMatrix[9+1,9+4] + _Coeffs[11] * _RotationMatrix[9+2,9+4] + _Coeffs[12] * _RotationMatrix[9+3,9+4] + _Coeffs[13] * _RotationMatrix[9+4,9+4] + _Coeffs[14] * _RotationMatrix[9+5,9+4] + _Coeffs[15] * _RotationMatrix[9+6,9+4];
// 			_RotatedCoeffs[14] = _Coeffs[9] * _RotationMatrix[9+0,9+5] + _Coeffs[10] * _RotationMatrix[9+1,9+5] + _Coeffs[11] * _RotationMatrix[9+2,9+5] + _Coeffs[12] * _RotationMatrix[9+3,9+5] + _Coeffs[13] * _RotationMatrix[9+4,9+5] + _Coeffs[14] * _RotationMatrix[9+5,9+5] + _Coeffs[15] * _RotationMatrix[9+6,9+5];
// 			_RotatedCoeffs[15] = _Coeffs[9] * _RotationMatrix[9+0,9+6] + _Coeffs[10] * _RotationMatrix[9+1,9+6] + _Coeffs[11] * _RotationMatrix[9+2,9+6] + _Coeffs[12] * _RotationMatrix[9+3,9+6] + _Coeffs[13] * _RotationMatrix[9+4,9+6] + _Coeffs[14] * _RotationMatrix[9+5,9+6] + _Coeffs[15] * _RotationMatrix[9+6,9+6];
		}

		/// <summary>
		/// Computes the distance at which the ray hits the slab given the slab's dimensions and the viewer's position and view vector
		/// The "slab" is actually a piece of cylinder aligned with the X axis : the "thickness" represents the diameter of the cylinder
		/// Thus, the ray can either exit through the front or back faces of the cylinder, or through the cylinder's sides
		/// </summary>
		/// <param name="_Direction">NORMALIZED</param>
		/// <param name="X">in [0,L]</param>
		/// <param name="L"></param>
		/// <param name="T"></param>
		/// <param name="H">in [0,T]</param>
		/// <returns></returns>
		protected double	ComputeHitDistance( WMath.Vector _Direction, double X, double L, double T, double H )
		{
			// Compute front (or back) hit
			double	HitFront = Math.Abs( _Direction.x ) > 1e-6 ? ((_Direction.x > 0.0f ? (L-X) : -X) / _Direction.x) : double.PositiveInfinity;

			// Compute side hit
			double	R = 0.5*T;	// Radius is half thickness
			double	C = R;		// Center is at half the diameter above
			double	P = H - C;
			double	c = P*P - R*R;												// P.P - R²
			double	b = P*_Direction.y;											// P.V
			double	a = _Direction.y*_Direction.y+_Direction.z*_Direction.z;	// V.V
			if ( Math.Abs( a ) < 1e-6 )
				return HitFront;	// Direction is aligned with slab's axis : no possible side hit...
			double	Delta = b*b-a*c;
			if ( Delta < 0.0 )
				throw new Exception( "This is inacceptable !" );	// Shouldn't happen as we're INSIDE the slab at all times
//				return HitFront;	// No side hit !

			double	HitSide = (-b+Math.Sqrt( Delta )) / a;	// Got our hit !

// 			// DEBUG CHECK
// 			double	HitY = P + HitSide * _Direction.y;
// 			double	HitZ = 0 + HitSide * _Direction.z;
// 			double	HitRadius = Math.Sqrt( HitY*HitY+HitZ*HitZ );	// Should be equal to R
// 			// DEBUG CHECK

			return Math.Max( 0.0, Math.Min( HitSide, HitFront ) );
		}

		/// <summary>
		/// Saves a freshly computed 3D scattering table
		/// </summary>
		/// <param name="_TableName"></param>
		/// <param name="_Table"></param>
		protected void SaveTable( string _TableName, SHVector[,,] _Table )
		{
			string TargetPath = "../../Tables/" + _TableName;

			System.IO.FileInfo File = new System.IO.FileInfo( TargetPath );
			System.IO.FileStream Stream = File.Create();
			System.IO.BinaryWriter Writer = new System.IO.BinaryWriter( Stream );

			int	LX = _Table.GetLength( 0 );
			int	LY = _Table.GetLength( 1 );
			int	LZ = _Table.GetLength( 2 );
			for ( int X=0; X < LX; X++ )
				for ( int Y=0; Y < LY; Y++ )
					for ( int Z=0; Z < LZ; Z++ )
						_Table[X,Y,Z].Write( Writer );

			Writer.Close();
			Writer.Dispose();
			Stream.Close();
			Stream.Dispose();
		}

		/// <summary>
		/// Loads a previously computed 3D scattering table
		/// </summary>
		/// <param name="_TableName"></param>
		/// <param name="_Table"></param>
		protected void LoadTable( string _TableName, SHVector[,,] _Table )
		{
			string TargetPath = "../../Tables/" + _TableName;

			System.IO.FileInfo File = new System.IO.FileInfo( TargetPath );
			System.IO.FileStream Stream = File.OpenRead();
			System.IO.BinaryReader Reader = new System.IO.BinaryReader( Stream );

			int	LX = _Table.GetLength( 0 );
			int	LY = _Table.GetLength( 1 );
			int	LZ = _Table.GetLength( 2 );
			for ( int X=0; X < LX; X++ )
				for ( int Y=0; Y < LY; Y++ )
					for ( int Z=0; Z < LZ; Z++ )
					{
						_Table[X,Y,Z] = new SHVector();
						_Table[X,Y,Z].Read( Reader );
					}

			Reader.Close();
			Reader.Dispose();
			Stream.Close();
			Stream.Dispose();
		}

		/// <summary>
		/// Loads and accumulate all possible tables into a single table
		/// </summary>
		/// <param name="_Table"></param>
		protected void	BuildFinalTable( SHVector[,,] _Table )
		{
			int	StartScatteringEventIndex = 1;	// Ignore single scattering

			int	LX = _Table.GetLength( 0 );
			int	LY = _Table.GetLength( 1 );
			int	LZ = _Table.GetLength( 2 );

			for ( int X=0; X < LX; X++ )
				for ( int Y=0; Y < LY; Y++ )
					for ( int Z=0; Z < LZ; Z++ )
						_Table[X,Y,Z] = new SHVector();

			for ( int ScatteringEventIndex=StartScatteringEventIndex; ScatteringEventIndex < SCATTERING_EVENTS_COUNT; ScatteringEventIndex++ )
			{
				SHVector[,,]	TempTable = new SHVector[SLAB_TEXTURE_SIZE,SLAB_TEXTURE_SIZE,SLAB_TEXTURE_DEPTH];
				try
				{
					LoadTable( "Scattering" + ScatteringEventIndex + ".sh", TempTable );
				}
				catch ( Exception )
				{
					break;	// No more available events I suppose...
				}

				// Accumulate into final table
				for ( int X=0; X < LX; X++ )
					for ( int Y=0; Y < LY; Y++ )
						for ( int Z=0; Z < LZ; Z++ )
							_Table[X,Y,Z] += TempTable[X,Y,Z];

			}

			SaveTable( "AccumulatedScattering.sh", _Table );
		}

		#endregion

		#region EVENT HANDLERS

		protected int		m_ViewModeScatteringEventIndex = 1;

		private void Form1_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			switch ( e.KeyCode )
			{
				case Keys.Add:
					m_ViewModeScatteringEventIndex = 1 + (m_ViewModeScatteringEventIndex % SCATTERING_EVENTS_COUNT);
					ViewTable( m_ViewModeScatteringEventIndex, outputPanel.m_ComputedTable );
					break;
				case Keys.Subtract:
					m_ViewModeScatteringEventIndex = 1 + ((m_ViewModeScatteringEventIndex+SCATTERING_EVENTS_COUNT-2) % SCATTERING_EVENTS_COUNT);
					ViewTable( m_ViewModeScatteringEventIndex, outputPanel.m_ComputedTable );
					break;

				case Keys.NumPad7:
					outputPanel.m_ViewModeThickness += 10.0;
 					outputPanel.UpdateBitmap();
					break;
				case Keys.NumPad1:
					outputPanel.m_ViewModeThickness -= 10.0;
					outputPanel.m_ViewModeHeight = Math.Min( outputPanel.m_ViewModeThickness, outputPanel.m_ViewModeHeight );
 					outputPanel.UpdateBitmap();
					break;

				case Keys.NumPad8:
					outputPanel.m_ViewModeHeight = Math.Min( outputPanel.m_ViewModeThickness, outputPanel.m_ViewModeHeight + 0.1 * outputPanel.m_ViewModeThickness );
 					outputPanel.UpdateBitmap();
					break;
				case Keys.NumPad2:
					outputPanel.m_ViewModeHeight = Math.Max( 0, outputPanel.m_ViewModeHeight - 0.1 * outputPanel.m_ViewModeThickness );
 					outputPanel.UpdateBitmap();
					break;

				case Keys.NumPad9:
					outputPanel.m_ScaleY += 0.01f;
 					outputPanel.UpdateBitmap();
					break;
				case Keys.NumPad3:
					outputPanel.m_ScaleY -= 0.01f;
 					outputPanel.UpdateBitmap();
					break;
			}
		}

		#endregion
	}
}
