using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

using WMath;

namespace Atmospheric
{
	public class	PhaseFunction
	{
		#region FIELDS

		protected float[]	m_PhaseFactors = null;

		#endregion

		#region PROPERTIES

		public int				FactorsCount
		{
			get { return m_PhaseFactors.Length; }
		}

		public float[]			PhaseFactors
		{
			get { return m_PhaseFactors; }
		}

		#endregion

		#region METHODS

		public		PhaseFunction()
		{
		}

		public void		Init()
		{
			Init( CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION, 0.0f, (float) Math.PI, 1024 );
		}

		/// <summary>
		/// Initializes the phase function with the provided original table
		/// 
		/// You can provide 2 angles clipping the phase function. The final phase function will actually
		///  expand from the [Start,End] angles interval to [0,PI], thus cutting any undesired peaks at
		///  extreme angles.
		/// 
		/// For example, it's common to remove the 5° peak of a Mie phase function as it's very directional
		///  and should be taken into account by other means than sampling the phase function.
		/// To do this, you simply enter 5 * PI / 180 as the start angle and PI as the end angle
		/// 
		/// NOTE: Final phase function will always integrate to 1, no matter what clipping is chosen.
		/// </summary>
		/// <param name="_PhaseFunction">The phase function to initialize with</param>
		/// <param name="_fStartAngle">Assuming the provided phase function covers 180°, this is the angle where to start collapsing into a table</param>
		/// <param name="_fEndAngle">Assuming the provided phase function covers 180°, this is the angle where to end collapsing into a table</param>
		/// <param name="_CompiledTableSize">The size of the final table stored by this object</param>
		public void		Init( double[] _PhaseFunction, float _fStartAngle, float _fEndAngle, int _CompiledTableSize )
		{
			// Compute MIN/MAX indices of the phase function
			int		MinIndex = (int) (_PhaseFunction.Length * _fStartAngle / Math.PI);
			int		MaxIndex = (int) (_PhaseFunction.Length * _fEndAngle / Math.PI);

			// Compute integral of provided function
			double	fIntegral = 0.0;
			for ( int Index=0; Index < _CompiledTableSize; Index++ )
			{
				float	fTheta = Index * (float) Math.PI / _CompiledTableSize;

				int		OriginalPhaseIndex = MinIndex + (MaxIndex - MinIndex) * Index / _CompiledTableSize;

				fIntegral += _PhaseFunction[OriginalPhaseIndex];// * Math.Sin( fTheta );
			}

			fIntegral *= Math.PI / _CompiledTableSize;	// * dTheta
			fIntegral *= 2.0 * Math.PI;

			// Copy source function into collapsed table
			m_PhaseFactors = new float[_CompiledTableSize];

			fIntegral = 1.0 / fIntegral;
			for ( int Index=0; Index < _CompiledTableSize; Index++ )
			{
				int		OriginalPhaseIndex = MinIndex + (MaxIndex - MinIndex) * Index / _CompiledTableSize;
				m_PhaseFactors[Index] = (float) (_PhaseFunction[OriginalPhaseIndex] * fIntegral);
			}

			// Verify integral converges to 1
			fIntegral = 0.0;
			for ( int Index=0; Index < _CompiledTableSize; Index++ )
			{
				float	fTheta = Index * (float) Math.PI / _CompiledTableSize;

				fIntegral += m_PhaseFactors[Index];// * Math.Sin( fTheta );
			}

			fIntegral *= Math.PI / _CompiledTableSize;	// * dTheta
			fIntegral *= 2.0 * Math.PI;
		}

		/// <summary>
		/// Initializes the phase function from a text stream that was generated using MiePlot
		/// Otherwise, it takes the same parameters as the above function
		/// </summary>
		/// <param name="_MiePlotTextStream">The stream to initialize from</param>
		/// <param name="_fStartAngle"></param>
		/// <param name="_fEndAngle"></param>
		/// <param name="_CompiledTableSize"></param>
		public void		Init( StreamReader _MiePlotTextStream, float _fStartAngle, float _fEndAngle, int _CompiledTableSize )
		{
			// Read averaged phase function
			string		Pattern = "Angle\tR+G+B: Perpendicular\tR+G+B: Parallel\tR+G+B: Unpolarised\r\n";
			string		FileContent = _MiePlotTextStream.ReadToEnd();

			string		DataStart = FileContent.Remove( 0, FileContent.IndexOf( Pattern )+Pattern.Length );
			string[]	PhaseStringValues = DataStart.Split( '\n' );

			double[]	PhaseValues = new double[1801];
			for ( int PhaseIndex=0; PhaseIndex < 1801; PhaseIndex++ )
			{
				string[]	Values = PhaseStringValues[PhaseIndex].Split( '\t' );
				PhaseValues[PhaseIndex] = double.Parse( Values[3] );
			}
			
			// Forward to standard method
			Init( PhaseValues, _fStartAngle, _fEndAngle, _CompiledTableSize );
		}

		/// <summary>
		/// Gets the phase value for the requested angle
		/// </summary>
		/// <param name="_fAngle"></param>
		/// <returns></returns>
		public float		GetPhaseFactor( float _fAngle )
		{
			_fAngle = (float) (((_fAngle % Math.PI) + Math.PI) % Math.PI);

			float	fIntegerAngle = _fAngle * m_PhaseFactors.Length / (float) Math.PI;
			int		Index0 = (int) Math.Floor( fIntegerAngle );
			int		Index1 = Index0 < m_PhaseFactors.Length - 1 ? Index0+1 : Index0;
			float	t = fIntegerAngle - Index0;

			return	m_PhaseFactors[Index0] * (1.0f - t) + m_PhaseFactors[Index1] * t;
		}

		#endregion
	}
}
