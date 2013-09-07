using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;

namespace Pognac
{
	public partial class InstitutionForm : Form
	{
		#region FIELDS

		protected Documents.Institution	m_Institution = null;
		protected XmlDocument			m_Original = null;

		#endregion

		#region PROPERTIES

		public Documents.Institution	Institution
		{
			get { return m_Institution; }
			set
			{
				if ( value == m_Institution )
					return;

				if ( m_Institution != null )
					m_Institution.NameChanged += new EventHandler( Institution_NameChanged );
			
				m_Institution = value;

				if ( m_Institution != null )
				{
					m_Institution.NameChanged += new EventHandler( Institution_NameChanged );

					annotationControl.Annotation = m_Institution.Annotation;

					// Perform a backup in case of cancel
					m_Original = new XmlDocument();
					XmlElement	Root = m_Original.CreateElement( "ROOT" );
					m_Original.AppendChild( Root );
					m_Institution.Save( Root );
				}

				// Update GUI
				Enabled = m_Institution != null;
				Institution_NameChanged( m_Institution, EventArgs.Empty );
			}
		}

		#endregion

		#region METHODS

		public InstitutionForm()
		{
			InitializeComponent();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			base.OnClosing( e );

			if ( DialogResult == DialogResult.OK )
				return;	// Validate changes...

			// Restore former institution
			m_Institution.Load( m_Original["ROOT"]["Institution"] );
		}

		#endregion

		#region EVENT HANDLERS

		void Institution_NameChanged( object sender, EventArgs e )
		{
			textBoxInstitutionName.Text = m_Institution != null ? m_Institution.Name : "";
		}

		private void textBoxInstitutionName_TextChanged( object sender, EventArgs e )
		{
			if ( m_Institution != null )
				m_Institution.Name = textBoxInstitutionName.Text;
			buttonSave.Enabled = textBoxInstitutionName.Text != "";
		}

		private void buttonSave_Click( object sender, EventArgs e )
		{
			DialogResult = DialogResult.OK;
		}

		#endregion
	}
}
