using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pognac
{
	public partial class DocumentsListControl : UserControl
	{
		#region CONSTANTS
		
		protected const int		TOOLTIP_DURATION = 10000;

		#endregion

		#region NESTED TYPES

		protected class DummyAttachment : Documents.Attachment
		{
			public override Bitmap Bitmap
			{
				get { return Properties.Resources.document_icon; }
			}

			public DummyAttachment( Documents.Database _Database, int _ID ) : base( _Database, new System.IO.FileInfo( _ID.ToString() ) )
			{
			}
		}

		#endregion

		#region FIELDS

		protected Documents.Document[]	m_Documents = new Documents.Document[0];
		protected Documents.Document	m_Selection = null;

		// Thumbnails management
		protected Dictionary<Documents.Document,Documents.Attachment>	m_Document2Attachment = new Dictionary<Documents.Document,Documents.Attachment>();
		protected Dictionary<Documents.Attachment,Documents.Document>	m_Attachment2Document = new Dictionary<Documents.Attachment,Documents.Document>();

		protected int	m_DummyID = 0;

		#endregion

		#region PROPERTIES

		public Documents.Document[]	Documents
		{
			get { return m_Documents; }
			set
			{
				if ( value == null )
					value = new Documents.Document[0];
				if ( value == m_Documents )
					return;

				if ( m_Documents != null )
					foreach ( Documents.Document D in m_Documents )
					{
						D.PagesChanged -= new EventHandler( Document_PagesChanged );
						D.Disposed -= new EventHandler(Document_Disposed);
					}

				m_Documents = value;
				m_Attachment2Document.Clear();
				m_Document2Attachment.Clear();

				if ( m_Documents == null )
				{
					thumbnailBrowser.Attachments = null;
					return;
				}

				// Build attachments for each document
				bool	bContainsSelection = false;
				m_bInternalChange = true;
				foreach ( Documents.Document D in m_Documents )
				{
					D.PagesChanged += new EventHandler( Document_PagesChanged );
					D.Disposed += new EventHandler(Document_Disposed);
					Document_PagesChanged( D, EventArgs.Empty );

					if ( D == m_Selection )
						bContainsSelection = true;
				}
				m_bInternalChange = false;
				RebuildThumbnails();

				if ( !bContainsSelection )
					Selection = null;	// Clear selection as it's not part of our new documents
			}
		}

		/// <summary>
		/// Gets the currently selected document
		/// </summary>
		public Documents.Document	Selection
		{
			get { return m_Selection; }
			set
			{
				if ( value == m_Selection )
					return;

				m_Selection = value;

				// Update thumbnail
				thumbnailBrowser.Selection = m_Selection != null && m_Document2Attachment.ContainsKey( m_Selection ) ? m_Document2Attachment[m_Selection] : null;

				// Notify
				if ( SelectionChanged != null )
					SelectionChanged( this, EventArgs.Empty );
			}
		}

		public event EventHandler	SelectionChanged;

		#endregion

		#region METHODS

		public DocumentsListControl()
		{
			InitializeComponent();
		}

		#endregion

		#region EVENT HANDLERS

		void Document_PagesChanged( object sender, EventArgs e )
		{
			Documents.Document	D = sender as Documents.Document;

			// Remove previous attachment
			if ( m_Document2Attachment.ContainsKey( D ) )
			{
				m_Attachment2Document.Remove( m_Document2Attachment[D] );
				m_Document2Attachment.Remove( D );
			}

			// Find an attachment that represents the document
			Documents.Attachment	A = null;
			foreach ( Documents.Page P in D.Pages )
				if ( P.Attachment != null )
				{	// Use the first available page attachment
					A = P.Attachment;
					break;
				}

			if ( A == null )
				A = new DummyAttachment( D.Database, ++m_DummyID );

			m_Attachment2Document.Add( A, D );
			m_Document2Attachment.Add( D, A );

			RebuildThumbnails();
		}

		protected bool	m_bInternalChange = false;
		protected void	RebuildThumbnails()
		{
			if ( m_bInternalChange )
				return;

			Documents.Attachment[]	Attachments = m_Document2Attachment.Values.ToArray();
			thumbnailBrowser.Attachments = Attachments;

			// Restore selection
			if ( Selection != null && m_Document2Attachment.ContainsKey( Selection ) )
				thumbnailBrowser.Selection = m_Document2Attachment[Selection];
		}

		void  Document_Disposed(object sender, EventArgs e)
		{
 			Documents.Document[]	CurrentDocuments = Documents;
 			Documents.Document[]	NewDocuments = CollectionsOperations<Documents.Document>.Subtraction( CurrentDocuments, new Documents.Document[] { sender as Documents.Document } );
			Documents = NewDocuments;
		}

		private void thumbnailBrowser_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			OnMouseDoubleClick( e );
		}

		private void thumbnailBrowser_SelectionChanged( object sender, EventArgs e )
		{
			Selection = thumbnailBrowser.Selection != null && m_Attachment2Document.ContainsKey( thumbnailBrowser.Selection ) ? m_Attachment2Document[thumbnailBrowser.Selection] : null;
		}

		protected DateTime	m_LastMotionTime = DateTime.Now;
		protected Point		m_LastPosition;
		private void thumbnailBrowser_MouseMove( object sender, MouseEventArgs e )
		{
			if ( e.Location != m_LastPosition )
				m_LastMotionTime = DateTime.Now;
			m_LastPosition = e.Location;
		}

		private void thumbnailBrowser_Enter( object sender, EventArgs e )
		{
			timerTooltip.Enabled = true;
		}

		private void thumbnailBrowser_Leave( object sender, EventArgs e )
		{
			timerTooltip.Enabled = false;
		}

		private void timerTooltip_Tick( object sender, EventArgs e )
		{
			if ( !Visible )
				return;
			if ( (DateTime.Now - m_LastMotionTime).TotalMilliseconds < toolTip1.InitialDelay )
				return;

			Point	Pos = PointToClient( Control.MousePosition );
			Pognac.Documents.Attachment	A = thumbnailBrowser.GetAttachmentAtPoint( Pos );
			if ( A == null )
				return;
			Pognac.Documents.Document	Doc = m_Attachment2Document[A];

			// Display a nice tooltip
			string	Text = "Title: " + Doc.Title + " (" + Doc.Pages.Length + " pages)\r\n";
			Text += "From : " + (Doc.Sender != null ? Doc.Sender.ToString() : "<UNKNOWN SENDER>") + "\r\n";
			Text += "Received on : " + Doc.ReceptionDate.ToString( "D" ) + "\r\n";
			Text += "Due on : " + Doc.DueDate.ToString( "D" ) + "\r\n";
			if ( Doc.Amount > 0.0f )
				Text += "Amount : " + Doc.Amount.ToString( "G4" ) + "\r\n";
			if ( Doc.Tags.Length > 0 )
			{
				Text += "Tags :\r\n";

				string	TagsText = "";
				foreach ( Pognac.Documents.Tag T in Doc.Tags )
					TagsText += (TagsText != "" ? ", " : "") + T.ToString();
				Text += " .  " + TagsText;
			}

			toolTip1.Show( Text, this, Pos, TOOLTIP_DURATION );
		}

		#endregion
	}
}
