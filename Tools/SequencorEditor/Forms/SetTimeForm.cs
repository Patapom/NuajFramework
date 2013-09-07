using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SequencorLib;

namespace SequencorEditor
{
	public partial class SetTimeForm : Form
	{
		#region FIELDS

		protected bool	m_bInternalChange = false;

		#endregion

		#region PROPERTIES

		public float			Time
		{
			get { return floatTrackbarControlTime.Value; }
			set
			{
				if ( m_bInternalChange )
					return;

				m_bInternalChange = true;

				floatTrackbarControlTime.Value = value;
				integerTrackbarControl.Value = (int) (1000.0f * value);

				integerTrackbarControlMinutes.Value = (int) Math.Floor( value / 60.0f );
				value -= 60.0f * integerTrackbarControlMinutes.Value;
				integerTrackbarControlSeconds.Value = (int) Math.Floor( value );
				value -= integerTrackbarControlSeconds.Value;
				integerTrackbarControlMilliSeconds.Value = (int) Math.Floor( 1000.0f * value );

				m_bInternalChange = false;
			}
		}

		#endregion

		#region METHODS

		public SetTimeForm()
		{
			InitializeComponent();
		}

		protected float GetFormattedTime()
		{
			return integerTrackbarControlMinutes.Value * 60.0f + integerTrackbarControlSeconds.Value + integerTrackbarControlMilliSeconds.Value * 0.001f;
		}

		#endregion

		#region EVENT HANDLERS

		private void floatTrackbarControlTime_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( m_bInternalChange )
				return;

			Time = _Sender.Value;
		}

		private void integerTrackbarControl_ValueChanged( IntegerTrackbarControl _Sender, int _FormerValue )
		{
			if ( m_bInternalChange )
				return;

			Time = integerTrackbarControl.Value / 1000.0f;
		}

		private void integerTrackbarControlMinutes_ValueChanged( IntegerTrackbarControl _Sender, int _FormerValue )
		{
			if ( m_bInternalChange )
				return;

			Time = GetFormattedTime();
		}

		private void integerTrackbarControlSeconds_ValueChanged( IntegerTrackbarControl _Sender, int _FormerValue )
		{
			if ( m_bInternalChange )
				return;

			Time = GetFormattedTime();
		}

		private void integerTrackbarControlMilliSeconds_ValueChanged( IntegerTrackbarControl _Sender, int _FormerValue )
		{
			if ( m_bInternalChange )
				return;

			Time = GetFormattedTime();
		}

		#endregion
	}
}
