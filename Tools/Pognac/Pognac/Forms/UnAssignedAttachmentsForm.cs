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
	/// <summary>
	/// This form allows to work in 3 different ways :
	///  1) Either from the database with no document so the user can create documents from the selection of un-assigned files.
	///  2) Assigning an existing document to the form allows the user to pick additional pages for that document.
	///  3) Assigning an existing page to the form allows the user to pick a new single attachement for that page
	/// </summary>
	public partial class UnAssignedAttachmentsForm : Form
	{
		#region FIELDS

		protected Documents.Database	m_Database = null;
		protected Documents.Document	m_Document = null;
		protected Documents.Page		m_Page = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// The compulsory database
		/// </summary>
		public Documents.Database		Database
		{
			get { return m_Database; }
			set
			{
				if ( value == m_Database )
					return;

				if ( m_Database != null )
					m_Database.UnAssignedAttachmentsChanged -= new EventHandler( Database_UnAssignedAttachmentsChanged );

				m_Database = value;

				if ( m_Database != null )
					m_Database.UnAssignedAttachmentsChanged += new EventHandler( Database_UnAssignedAttachmentsChanged );

				// Update GUI
				Enabled = m_Database != null;
				Database_UnAssignedAttachmentsChanged( m_Database, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Assign a document so the form helps to add new pages in bulk
		/// </summary>
		public Documents.Document		Document
		{
			get { return m_Document; }
			set
			{
				if ( value == m_Document )
					return;

				m_Document = value;

				if ( m_Document != null )
					Database = m_Document.Database;

				panelAddPagesToDocument.Visible = m_Document != null;
				panelPickNewPageAttachment.Visible = m_Page != null;
				panelCreateDocument.Visible = m_Document == null;
				zoomableImagesViewer.UseMultipleSelection = m_Page == null;
			}
		}

		/// <summary>
		/// Assign a page so the form helps to pick a single new attachment for the page
		/// </summary>
		public Documents.Page			Page
		{
			get { return m_Page; }
			set
			{
				if ( value == m_Page )
					return;

				m_Page = value;

				if ( m_Page != null )
					Database = m_Page.Database;

				panelPickNewPageAttachment.Visible = m_Page != null;
				panelAddPagesToDocument.Visible = m_Document != null;
				panelCreateDocument.Visible = m_Page == null;
				zoomableImagesViewer.UseMultipleSelection = m_Page == null;
			}
		}

		#endregion

		#region METHODS

		public UnAssignedAttachmentsForm()
		{
			InitializeComponent();
		}

		#endregion

		#region EVENT HANDLERS

		void Database_UnAssignedAttachmentsChanged( object sender, EventArgs e )
		{
			zoomableImagesViewer.Attachments = m_Database.UnAssignedAttachments;
			labelUnAssignedFiles.Text = m_Database.UnAssignedAttachmentsCount.ToString();
		}

		private void zoomableImagesViewer_SelectionChanged( object sender, EventArgs e )
		{
			buttonCreateDocument.Enabled = buttonAddPages.Enabled = buttonPick.Enabled = zoomableImagesViewer.MultipleSelection.Length > 0;
		}

		private void buttonZoomOut_Click( object sender, EventArgs e )
		{
			zoomableImagesViewer.ZoomOnSelection();
		}

		private void buttonCreateDocument_Click( object sender, EventArgs e )
		{
			DocumentForm		F = new DocumentForm();
			Documents.Document	NewDocument = null;
			try
			{
				PognacForm.RestoreForm( F );

				// Create the document and its pages
				ErrorSheet	Errors = new ErrorSheet();
				
				NewDocument = m_Database.CreateDocument( m_Database.InvalidInstitution );
				foreach ( Documents.Attachment A in zoomableImagesViewer.MultipleSelection )
					try
					{
						Documents.Page	P = NewDocument.AddPage( Documents.Page.TYPE.MISC, "", A );
					}
					catch ( Exception _e )
					{
						Errors.AddError( _e );
					}
				if ( Errors.HasErrors )
					throw new ErrorSheetException( "Some errors occurred while creating the new document", Errors );

				F.Document = NewDocument;
				if ( F.ShowDialog( this ) == DialogResult.OK )
					return;

				// Cancel
				m_Database.RemoveDocument( NewDocument );
			}
			catch ( ErrorSheetException _e )
			{
				m_Database.RemoveDocument( NewDocument );
				_e.Display();
			}
			catch ( Exception _e )
			{
				m_Database.RemoveDocument( NewDocument );
				PognacForm.MessageBox( "An error occurred while opening the CreateDocuments form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				PognacForm.SaveForm( F );
			}
		}

		private void buttonAddPages_Click( object sender, EventArgs e )
		{
			ErrorSheet	Errors = new ErrorSheet();
			foreach ( Documents.Attachment SelectedFile in zoomableImagesViewer.MultipleSelection )
			{
				try
				{
					Documents.Page	NewPage = m_Document.AddPage( Documents.Page.TYPE.MISC, "", SelectedFile );
				}
				catch ( Exception _e )
				{
					Errors.AddError( _e );
				}
			}

			if ( Errors.HasErrors )
				Errors.DisplayErrors( "Some errors occurred while adding pages to the document :" );

			DialogResult = DialogResult.OK;
		}

		private void buttonPick_Click( object sender, EventArgs e )
		{
			m_Page.Attachment = zoomableImagesViewer.SingleSelection;

			DialogResult = DialogResult.OK;
		}

		#endregion
	}
}
