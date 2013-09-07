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
	public partial class DocumentForm : Form
	{
		#region NESTED TYPES

		protected class DummyAttachment : Documents.Attachment
		{
			public override Bitmap Bitmap
			{
				get { return Properties.Resources.ImageError; }
			}

			public DummyAttachment( Documents.Database _Database, int _ID ) : base( _Database, new System.IO.FileInfo( _ID.ToString() ) )
			{
			}
		}

		#endregion

		#region FIELDS

		protected Documents.Document	m_Document = null;
		protected object				m_Original = null;

		// Attachments management
		protected Dictionary<Documents.Attachment,Documents.Page>	m_Attachment2Page = new Dictionary<Documents.Attachment,Documents.Page>();

		protected int	m_DummyID = 0;

		#endregion

		#region PROPERTIES

		public Documents.Document		Document
		{
			get { return m_Document; }
			set
			{
				if ( value == m_Document )
					return;

				if ( m_Document != null )
				{
					m_Document.TitleChanged -= new EventHandler( Document_TitleChanged );
					m_Document.SenderChanged -= new EventHandler( Document_SenderChanged );
					m_Document.TagsChanged -= new EventHandler( Document_TagsChanged );
					m_Document.PagesChanged -= new EventHandler( Document_PagesChanged );
					m_Document.DatesChanged -= new EventHandler( Document_DatesChanged );
					m_Document.AmountChanged -= new EventHandler( Document_AmountChanged );
					m_Document.Database.UnAssignedAttachmentsChanged -= new EventHandler( Database_UnAssignedAttachmentsChanged );
				}

				m_Document = value;

				if ( m_Document != null )
				{
					m_Document.TitleChanged += new EventHandler( Document_TitleChanged );
					m_Document.SenderChanged += new EventHandler( Document_SenderChanged );
					m_Document.TagsChanged += new EventHandler( Document_TagsChanged );
					m_Document.PagesChanged += new EventHandler( Document_PagesChanged );
					m_Document.DatesChanged += new EventHandler( Document_DatesChanged );
					m_Document.AmountChanged += new EventHandler( Document_AmountChanged );
					m_Document.Database.UnAssignedAttachmentsChanged += new EventHandler( Database_UnAssignedAttachmentsChanged );

					dateTimePickerCreation.Value = m_Document.RegistrationDate;
					annotationControl.Annotation = m_Document.Annotation;

					// Perform a backup in case of cancel
					m_Original = m_Document.Backup();
				}

				// Update GUI
				Enabled = m_Document != null;
				buttonPickTag.Enabled = m_Document != null && m_Document.Database.TagsCount > 0;
				textBoxInstitution.Enabled = m_Document != null && m_Document.Database.InstitutionsCount > 0;
				Document_TitleChanged( m_Document, EventArgs.Empty );
				Document_SenderChanged( m_Document, EventArgs.Empty );
				Document_TagsChanged( m_Document, EventArgs.Empty );
				Document_PagesChanged( m_Document, EventArgs.Empty );
				Document_DatesChanged( m_Document, EventArgs.Empty );
				Document_AmountChanged( m_Document, EventArgs.Empty );
				Database_UnAssignedAttachmentsChanged( m_Document != null ? m_Document.Database : null, EventArgs.Empty );
			}
		}

		#endregion

		#region METHODS

		public DocumentForm()
		{
			InitializeComponent();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			base.OnClosing( e );

			if ( DialogResult == DialogResult.OK )
				return;	// Validate changes...

			// Restore former document
			if ( m_Document != null && m_Original != null )
			{
				if ( m_Document.HasChanged( m_Original ) && PognacForm.MessageBox( "You have made changes to the document.\r\nCancelling the form will lose all these changes.\r\n\r\nAre you sure you wish to proceed ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning ) == DialogResult.No )
				{
					e.Cancel = true;
					return;
				}

				m_Document.Restore( m_Original );
			}
		}

		#endregion

		#region EVENT HANDLERS

		#region Document Events

		void Document_PagesChanged( object sender, EventArgs e )
		{
			// Un-subscribe from previous pages' events
			foreach ( Documents.Page P in m_Attachment2Page.Values )
				P.AttachmentChanged -= new EventHandler( Page_AttachmentChanged );

			m_Attachment2Page.Clear();

			List<Documents.Attachment>	Attachments = new List<Documents.Attachment>();
			if ( m_Document != null )
				foreach ( Documents.Page P in m_Document.Pages )
				{
					Documents.Attachment	A = P.Attachment;
					if ( A == null )
						A = new DummyAttachment( P.Database, ++m_DummyID );

					Attachments.Add( A );
					m_Attachment2Page[A] = P;

					// Subscribe to any attachment change
					P.AttachmentChanged += new EventHandler( Page_AttachmentChanged );
				}

			thumbnailBrowser.Attachments = Attachments.ToArray();
		}

		void Page_AttachmentChanged( object sender, EventArgs e )
		{
			// Rebuild attachments
			Document_PagesChanged( sender, e );
		}

		void Document_TagsChanged( object sender, EventArgs e )
		{
			textBoxTags.Text = "";
			if ( m_Document != null )
				foreach ( Documents.Tag T in m_Document.Tags )
					textBoxTags.Text += (textBoxTags.Text != "" ? ", " : "") + T.ToString();
		}

		void Document_SenderChanged( object sender, EventArgs e )
		{
			textBoxInstitution.Text = m_Document != null && m_Document.Sender != null ? m_Document.Sender.ToString() : "";
			buttonSave.Enabled = m_Document != null &&
				m_Document.Sender != m_Document.Database.InvalidInstitution &&
				!m_Document.IsIssueDateInvalid &&
				!m_Document.IsReceptionDateInvalid;
		}

		void Document_TitleChanged( object sender, EventArgs e )
		{
			textBoxTitle.Text = m_Document != null ? m_Document.Title : "";
		}

		void Document_DatesChanged( object sender, EventArgs e )
		{
			if ( m_Document == null )
				return;

			dateTimePickerCreation.Value = m_Document.RegistrationDate;
			dateTimePickerIssue.Value = m_Document.IssueDate;
			dateTimePickerReception.Value = m_Document.ReceptionDate;
			dateTimePickerDue.Value = m_Document.DueDate;

			errorProvider.SetError( dateTimePickerIssue, m_Document.IsIssueDateInvalid ? "Date has not been set !" : "" );
			errorProvider.SetError( dateTimePickerReception, m_Document.IsReceptionDateInvalid ? "Date has not been set !" : "" );

			Document_SenderChanged( sender, e );
		}

		void Document_AmountChanged( object sender, EventArgs e )
		{
			floatTrackbarControlAmount.Value = m_Document != null ? m_Document.Amount : 0.0f;
		}

		void Database_UnAssignedAttachmentsChanged( object sender, EventArgs e )
		{
			buttonCreatePage.Enabled = m_Document != null && m_Document.Database.UnAssignedAttachmentsCount > 0;
		}

		#endregion

		private void textBoxTitle_TextChanged( object sender, EventArgs e )
		{
			if ( m_Document != null )
				m_Document.Title = textBoxTitle.Text;
		}

		private void dateTimePickerIssue_CloseUp( object sender, EventArgs e )
		{
			if ( m_Document != null )
				m_Document.IssueDate = dateTimePickerIssue.Value;
		}

		private void dateTimePickerReception_CloseUp( object sender, EventArgs e )
		{
			if ( m_Document != null )
				m_Document.ReceptionDate = dateTimePickerReception.Value;
		}

		private void dateTimePickerDue_CloseUp( object sender, EventArgs e )
		{
			if ( m_Document != null )
				m_Document.DueDate = dateTimePickerDue.Value;
		}

		private void floatTrackbarControlAmount_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( m_Document != null )
				m_Document.Amount = _Sender.Value;
		}

		private void textBoxTags_Validated( object sender, EventArgs e )
		{
			if ( m_Document == null )
				return;

			List<Documents.Tag>	Result = new List<Documents.Tag>();
			List<Documents.Institution>	Institutions = new List<Documents.Institution>();
			ErrorSheet			Errors = new ErrorSheet();
			string				FinalList;
			Documents.Database.ParseTagsAndInstitutions( m_Document.Database, textBoxTags.Text, Result, Institutions, Errors, out FinalList );

			m_Document.Tags = Result.ToArray();

			// Force a refresh of the textbox anyway
			Document_TagsChanged( m_Document, EventArgs.Empty );
		}

		private void textBoxInstitution_Click( object sender, EventArgs e )
		{
			AddInstitutionForm.ShowDropDown( this, textBoxInstitution, m_Document.Database,
				( object form, EventArgs e2 ) =>
					{
						m_Document.Sender = (form as AddInstitutionForm).SelectedInstitution;
					}
				);
		}

		private void buttonCreateInstitution_Click( object sender, EventArgs e )
		{
			InstitutionForm	F = new InstitutionForm();
			try
			{
				PognacForm.RestoreForm( F );

				Documents.Institution	NewInstitution = m_Document.Database.CreateInstitution( "" );

				F.Institution = NewInstitution;
				if ( F.ShowDialog( this ) == DialogResult.OK )
					return;

				// Cancel...
				m_Document.Database.RemoveInstitution( NewInstitution );
			}
			catch ( Exception _e )
			{				
				PognacForm.MessageBox( "An error occurred while opening the CreateInstitution form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				PognacForm.SaveForm( F );
			}
		}

		private void buttonPickTag_Click( object sender, EventArgs e )
		{
			AddTagForm.ShowDropDown( this, buttonPickTag, m_Document.Database,
				( object form, EventArgs e2 ) =>
					{
						textBoxTags.Text += (textBoxTags.Text != "" ? ", " : "") + (form as AddTagForm).SelectedTag.Name;
						textBoxTags_Validated( textBoxTags, EventArgs.Empty );
					}
				);
		}

		private void buttonCreateTag_Click( object sender, EventArgs e )
		{
			TagForm	F = new TagForm();
			try
			{
				PognacForm.RestoreForm( F );

				Documents.Tag	NewTag = m_Document.Database.CreateTag( "" );

				F.Tag = NewTag;
				if ( F.ShowDialog( this ) == DialogResult.OK )
					return;
			
				// Cancel...
				m_Document.Database.RemoveTag( NewTag );
			}
			catch ( Exception _e )
			{
				PognacForm.MessageBox( "An error occurred while opening the CreateTag form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				PognacForm.SaveForm( F );
			}
		}

		private void thumbnailBrowser_SelectionChanged( object sender, EventArgs e )
		{
			buttonRemovePage.Enabled = buttonEditSelectedPage.Enabled = thumbnailBrowser.Selection != null;

			int	PageIndex = m_Document != null && thumbnailBrowser.Selection != null ? m_Document.GetIndexOfPage( m_Attachment2Page[thumbnailBrowser.Selection] ) : -1;
			buttonMovePageLeft.Enabled = PageIndex > 0;
			buttonMovePageRight.Enabled = m_Document != null && PageIndex != -1 && PageIndex < m_Document.Pages.Length-1;
		}

		private void buttonEditSelectedPage_Click( object sender, EventArgs e )
		{
			PageForm	F = new PageForm();
			try
			{
				PognacForm.RestoreForm( F );

				if ( !m_Attachment2Page.ContainsKey( thumbnailBrowser.Selection ) )
					throw new Exception( "Failed to retrieve the page corresponding to the selected attachment !" );

				F.Page = m_Attachment2Page[thumbnailBrowser.Selection];
				F.ShowDialog( this );
			}
			catch ( Exception _e )
			{
				PognacForm.MessageBox( "An error occurred while opening the UnAssignedFiles form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				PognacForm.SaveForm( F );
			}
		}

		private void buttonMovePageLeft_Click( object sender, EventArgs e )
		{
			Documents.Page	P = m_Attachment2Page[thumbnailBrowser.Selection];
			int	PageIndex = m_Document.GetIndexOfPage( P );
			m_Document.MovePage( P, PageIndex-1 );
		}

		private void buttonMovePageRight_Click( object sender, EventArgs e )
		{
			Documents.Page	P = m_Attachment2Page[thumbnailBrowser.Selection];
			int	PageIndex = m_Document.GetIndexOfPage( P );
			m_Document.MovePage( P, PageIndex+1 );
		}

		private void buttonCreatePage_Click( object sender, EventArgs e )
		{
			UnAssignedAttachmentsForm	F = new UnAssignedAttachmentsForm();
			try
			{
				PognacForm.RestoreForm( F );

				F.Document = m_Document;
				F.ShowDialog( this );
			}
			catch ( Exception _e )
			{
				PognacForm.MessageBox( "An error occurred while opening the UnAssignedFiles form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				PognacForm.SaveForm( F );
			}
		}

		private void buttonRemovePage_Click( object sender, EventArgs e )
		{
			if ( PognacForm.MessageBox( "Are you sure you wish to remove the selected page from the document ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2 ) != DialogResult.Yes )
				return;

			Documents.Page	P = m_Document.FindPageWithAttachment( thumbnailBrowser.Selection );
			if ( P == null )
			{
				PognacForm.MessageBox( "I failed to retrieve the selected page in the document !\r\nThis should not happen !", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}
			m_Document.RemovePage( P );
		}

		private void buttonSave_Click( object sender, EventArgs e )
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonDelete_Click( object sender, EventArgs e )
		{
			if ( PognacForm.MessageBox( "Are you sure you wish to delete this document from the database ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2 ) != DialogResult.Yes )
				return;

			m_Document.Database.RemoveDocument( m_Document );

			Document = null;
			DialogResult = DialogResult.OK;
			Close();
		}

		#endregion
	}
}
