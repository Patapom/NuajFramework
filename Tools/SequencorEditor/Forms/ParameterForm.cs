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
	public partial class ParameterForm : Form
	{
		#region FIELDS

		protected int	m_LastValidGUID = 0;

		#endregion

		#region PROPERTIES

		public string			ParameterName
		{
			get { return textBoxName.Text; }
			set { textBoxName.Text = value; }
		}

		public int				ParameterGUID
		{
			get { return int.Parse( textBoxGUID.Text ); }
			set { textBoxGUID.Text = value.ToString(); m_LastValidGUID = value; }
		}

		public Sequencor.ParameterTrack.PARAMETER_TYPE	ParameterType
		{
			get { return (Sequencor.ParameterTrack.PARAMETER_TYPE) (1+comboBoxType.SelectedIndex); }
			set { comboBoxType.SelectedIndex = (int) value - 1; comboBoxType.Enabled = false; }
		}

		#endregion

		#region METHODS

		public ParameterForm()
		{
			InitializeComponent();

			comboBoxType.SelectedIndex = (int) Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT - 1;
		}

		#endregion

		#region EVENT HANDLERS

		private void textBoxGUID_Validating( object sender, CancelEventArgs e )
		{
			int GUID = 0;
			if ( int.TryParse( textBoxGUID.Text, out GUID ) )
			{
				m_LastValidGUID = GUID;
				return;
			}

			textBoxGUID.Text = m_LastValidGUID.ToString();
			e.Cancel = true;
		}

		#endregion
	}
}
