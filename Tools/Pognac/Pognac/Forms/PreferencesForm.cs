using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Pognac
{
	public partial class PreferencesForm : Form
	{
		#region FIELDS

		protected DirectoryInfo	m_WorkingDirectory = null;

		#endregion

		#region PROPERTIES

		public DirectoryInfo	WorkingDirectory
		{
			get { return m_WorkingDirectory; }
			set { m_WorkingDirectory = value; textBoxWorkingDirectory.Text = value != null ? value.FullName : ""; }
		}

		#endregion

		#region METHODS

		public PreferencesForm()
		{
			InitializeComponent();
		}

		#endregion

		#region EVENT HANDLERS

		private void buttonSave_Click( object sender, EventArgs e )
		{
			DialogResult = DialogResult.OK;
		}

		private void buttonBrowse_Click( object sender, EventArgs e )
		{
			if ( WorkingDirectory != null )
				folderBrowserDialog.SelectedPath = WorkingDirectory.FullName;

			if ( folderBrowserDialog.ShowDialog( this ) != DialogResult.OK )
				return;

			WorkingDirectory = new DirectoryInfo( folderBrowserDialog.SelectedPath );
		}

		#endregion
	}
}
