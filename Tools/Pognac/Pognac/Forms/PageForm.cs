using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace Pognac
{
	public partial class PageForm : Form
	{
		#region FIELDS

		protected Documents.Page	m_Page = null;
		protected object			m_Original = null;

		#endregion

		#region PROPERTIES

		public Documents.Page	Page
		{
			get { return m_Page; }
			set
			{
				if ( value == m_Page )
					return;

				if ( m_Page != null )
				{
					m_Page.TypeChanged -= new EventHandler( Page_TypeChanged );
					m_Page.RectoChanged -= new EventHandler( Page_RectoChanged );
					m_Page.AttachmentChanged -= new EventHandler( Page_AttachmentChanged );
					m_Page.Database.UnAssignedAttachmentsChanged -= new EventHandler( Database_UnAssignedAttachmentsChanged );
				}
			
				m_Page = value;

				if ( m_Page != null )
				{
					m_Page.TypeChanged += new EventHandler( Page_TypeChanged );
					m_Page.RectoChanged += new EventHandler( Page_RectoChanged );
					m_Page.AttachmentChanged += new EventHandler( Page_AttachmentChanged );
					m_Page.Database.UnAssignedAttachmentsChanged += new EventHandler( Database_UnAssignedAttachmentsChanged );

					annotationControl.Annotation = m_Page.Annotation;

					// Perform a backup in case of cancel
					m_Original = m_Page.Backup();
				}

				// Update GUI
				Enabled = m_Page != null;
				Page_TypeChanged( m_Page, EventArgs.Empty );
				Page_RectoChanged( m_Page, EventArgs.Empty );
				Page_AttachmentChanged( m_Page, EventArgs.Empty );
				Database_UnAssignedAttachmentsChanged( m_Page.Database, EventArgs.Empty );
			}
		}

		public AnnotationControl		Annotation
		{
			get { return annotationControl; }
		}

		#endregion

		#region METHODS

		public PageForm()
		{
			InitializeComponent();

			comboBoxPageType.Items.Clear();
			comboBoxPageType.Items.AddRange( Enum.GetNames( typeof(Documents.Page.TYPE) ) );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			base.OnClosing( e );

			if ( DialogResult == DialogResult.OK )
				return;	// Validate changes...

			// Restore former page
			if ( m_Page != null && m_Original != null )
			{
				if ( m_Page.HasChanged( m_Original ) && PognacForm.MessageBox( "You have made changes to the page.\r\nCancelling the form will lose all these changes.\r\n\r\nAre you sure you wish to proceed ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning ) == DialogResult.No )
				{
					e.Cancel = true;
					return;
				}
				m_Page.Restore( m_Original );
			}
		}

		#endregion

		#region EVENT HANDLERS

		#region Page Events

		void Page_TypeChanged( object sender, EventArgs e )
		{
			comboBoxPageType.SelectedIndex = m_Page != null ? (int) m_Page.Type : 0;
		}

		void Page_RectoChanged( object sender, EventArgs e )
		{
			radioButtonRecto.Checked = m_Page != null && m_Page.Recto;
		}

		void Page_AttachmentChanged( object sender, EventArgs e )
		{
			if ( m_Page != null && m_Page.Attachment != null )
			{
				thumbnailBrowser.Attachments = new Documents.Attachment[] { m_Page.Attachment };
				textBoxFileName.Text = m_Page.Attachment.FileName.FullName;
				buttonLocateFile.Enabled = true;
			}
			else
			{
				thumbnailBrowser.Attachments = null;
				textBoxFileName.Text = "";
				buttonLocateFile.Enabled = false;
			}
		}

		void Database_UnAssignedAttachmentsChanged( object sender, EventArgs e )
		{
			buttonChangePage.Visible = m_Page.Database.UnAssignedAttachmentsCount > 0;
		}

		#endregion

		private void radioButtonRecto_CheckedChanged( object sender, EventArgs e )
		{
			m_Page.Recto = radioButtonRecto.Checked;
		}

		private void comboBoxPageType_SelectedIndexChanged( object sender, EventArgs e )
		{
			m_Page.Type = (Documents.Page.TYPE) comboBoxPageType.SelectedIndex;
		}

		private void buttonLocateFile_Click( object sender, EventArgs e )
		{
			PognacForm.MessageBox( "TODO: Open a shell and locate file..." );
		}

		private void buttonChangePage_Click( object sender, EventArgs e )
		{
			UnAssignedAttachmentsForm	F = new UnAssignedAttachmentsForm();
			try
			{
				PognacForm.RestoreForm( F );

				F.Page = m_Page;
				F.ShowDialog( this );
			}
			catch ( Exception _e )
			{
				PognacForm.MessageBox( "An error occurred while opening the UnAssignedFile form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				PognacForm.SaveForm( F );
			}
		}

		private void buttonSave_Click( object sender, EventArgs e )
		{
			DialogResult = DialogResult.OK;
		}

		#endregion
	}
}
