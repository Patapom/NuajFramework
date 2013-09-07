using System;
using System.Collections.Generic;
using System.Text;

namespace Atmospheric.Spectrum
{
	public abstract class	Spectrum
	{
		#region FIELDS

		protected double		m_LambdaMin = double.NaN;
		protected double		m_LambdaMax = double.NaN;

		#endregion

		#region PROPERTIES

		public double			LambdaMin
		{
			get { return m_LambdaMin; }
		}

		public double			LambdaMax
		{
			get { return m_LambdaMax; }
		}

		public abstract double		this[double _fLambda]	{ get; }

		#endregion

		#region METHODS

		// Operations
		public double				KernelIntegrate( Spectrum _Kernel, double _fDLambda )
		{
			if ( _fDLambda <= 0.0 )
				throw new Exception( "Integration step must be strictly positive!" );

			double	fResult = 0.0;
			double	fCurrentLambda = m_LambdaMin;
			while ( fCurrentLambda < m_LambdaMax )
			{
				double	fV0 = this[ fCurrentLambda ];
				double	fV1 = _Kernel[ fCurrentLambda ];

				fResult += fV0 * fV1;

				fCurrentLambda += _fDLambda;
			}

			return	fResult * _fDLambda;
		}

		public abstract Spectrum	Trim();

		#endregion
	}

	public class	SpectrumRegular : Spectrum
	{
		#region FIELDS

		protected double		m_dLambda = 0.0;

		protected int			m_SlotsCount = 0;
		protected double[]		m_Spectrum = null;

		#endregion

		#region PROPERTIES

		public override double  this[double _fLambda]
		{
			get 
			{ 
				// Check boundaries
				int	dwBoundMin = (int) System.Math.Floor( (float) ((_fLambda - m_LambdaMin) / m_dLambda) );
				if ( dwBoundMin < 0 )
					return	m_Spectrum[0];
				if ( dwBoundMin >= m_SlotsCount-1 )
					return	m_Spectrum[m_SlotsCount-1];

				// Interpolate
				double	Min = m_Spectrum[dwBoundMin];
				double	Max = m_Spectrum[dwBoundMin+1];
				return	Min + (_fLambda - (m_LambdaMin + dwBoundMin * m_dLambda)) * (Max - Min) / m_dLambda;
			}
		}

		#endregion

		#region METHODS

		public 				SpectrumRegular( int _SpectrumSlotsCount, double _LambdaStart, double _dLambda )
		{
			m_SlotsCount = _SpectrumSlotsCount;
			m_LambdaMin = _LambdaStart;
			m_LambdaMax = _LambdaStart + (_SpectrumSlotsCount - 1) * _dLambda;
			m_dLambda = _dLambda;

			m_Spectrum = new double[_SpectrumSlotsCount];
		}

		public 				SpectrumRegular( double _LambdaStart, double _dLambda, double[] _Source )
		{
			m_SlotsCount = _Source.Length;
			m_LambdaMin = _LambdaStart;
			m_LambdaMax = _LambdaStart + (m_SlotsCount - 1) * _dLambda;
			m_dLambda = _dLambda;

			m_Spectrum = new double[m_SlotsCount];
			Buffer.BlockCopy( _Source, 0, m_Spectrum, 0, m_SlotsCount*sizeof(double) );
		}

		// Slot access
		public double		GetSlotValue( int _SlotIndex )
		{
			return	m_Spectrum[_SlotIndex];
		}

		public double		GetSlotLambda( int _SlotIndex )
		{
			return	m_LambdaMin + m_dLambda * _SlotIndex;
		}

		public void			SetSlotValue( int _SlotIndex, double _Value )
		{
			m_Spectrum[_SlotIndex] = _Value;
		}

		// Operations
		public override Spectrum	Trim()
		{
			for ( int SlotIndex=0; SlotIndex < m_SlotsCount; SlotIndex++ )
				m_Spectrum[SlotIndex] = System.Math.Max( 0.0, m_Spectrum[SlotIndex] );

			return	this;
		}

		#endregion
	}

	public class	SpectrumIrregular : Spectrum
	{
		#region NESTED TYPES

		protected class	SlotCell
		{
			public double			m_Lambda;
			public double			m_Value;

			public SlotCell( double _Lambda, double _Value )
			{
				m_Lambda = _Lambda;
				m_Value = _Value;
			}
		};

		#endregion

		#region FIELDS

		protected int			m_SlotsCount = 0;
		protected SlotCell[]	m_Spectrum = null;

		#endregion

		#region PROPERTIES

		public override double  this[double _fLambda]
		{
			get 
			{ 
				// Check boundaries
				if ( _fLambda <= m_LambdaMin )
					return	m_Spectrum[0].m_Value;
				if ( _fLambda >= m_LambdaMax )
					return	m_Spectrum[m_SlotsCount-1].m_Value;

				// Retrieve the appropriate slot for interpolation
				SlotCell	SlotMin = null;
				SlotCell	SlotMax = m_Spectrum[0];
				int			SlotIndex = 1;
				for ( ; SlotIndex < m_SlotsCount; SlotIndex++ )
				{
					SlotMin = SlotMax;
					SlotMax = m_Spectrum[SlotIndex];

					if ( _fLambda >= SlotMin.m_Lambda && _fLambda <= SlotMax.m_Lambda )
						break;	// Found it!
				}

				if ( SlotIndex >= m_SlotsCount )
					throw new Exception( "Lambda out of range!" );

				// Interpolate
				return	SlotMin.m_Value + (_fLambda - SlotMin.m_Lambda) * (SlotMax.m_Value - SlotMin.m_Value) / (SlotMax.m_Lambda - SlotMin.m_Lambda);
			}
		}

		#endregion

		#region METHODS

		public 				SpectrumIrregular( int _SlotsCount )
		{
			m_LambdaMin = +double.MaxValue;
			m_LambdaMax = -double.MaxValue;
			m_SlotsCount = _SlotsCount;
			m_Spectrum = new SlotCell[_SlotsCount];
		}

		public 				SpectrumIrregular( double[] _SourceLambdas, double[] _SourceValues )
		{
			m_LambdaMin = +double.MaxValue;
			m_LambdaMax = -double.MaxValue;
			m_SlotsCount = _SourceLambdas.Length;
			m_Spectrum = new SlotCell[_SourceLambdas.Length];
			for ( int SlotIndex=0; SlotIndex < _SourceLambdas.Length; SlotIndex++ )
			{
				m_Spectrum[SlotIndex] = new SlotCell( _SourceLambdas[SlotIndex], _SourceValues[SlotIndex] );

				m_LambdaMin = System.Math.Min( m_LambdaMin, m_Spectrum[SlotIndex].m_Lambda );
				m_LambdaMax = System.Math.Max( m_LambdaMax, m_Spectrum[SlotIndex].m_Lambda );
			}
		}

		// Slot access
		public double		GetSlotValue( int _SlotIndex )
		{
			return m_Spectrum[_SlotIndex].m_Value;
		}

		public double		GetSlotLambda( int _SlotIndex )
		{
			return m_Spectrum[_SlotIndex].m_Lambda;
		}

		public void			SetSlotValue( int _SlotIndex, double _Lambda, double _Value )
		{
			m_Spectrum[_SlotIndex].m_Lambda = _Lambda;
			m_Spectrum[_SlotIndex].m_Value = _Value;

			m_LambdaMin = System.Math.Min( m_LambdaMin, _Lambda );
			m_LambdaMax = System.Math.Max( m_LambdaMax, _Lambda );
		}

		// Operations
		public override Spectrum	Trim()
		{
			for ( int SlotIndex=0; SlotIndex < m_Spectrum.Length; SlotIndex++ )
				m_Spectrum[SlotIndex].m_Value = System.Math.Max( 0.0, m_Spectrum[SlotIndex].m_Value );

			return	this;
		}

		#endregion
	}
}
