using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The document class represents a group of pages, typically contained within a single mail.
	/// </summary>
	public class Document : BaseDocument
	{
		#region CONSTANTS

		public static readonly DateTime	INVALID_DATE = new DateTime( 1900, 1, 1 );

		#endregion

		#region FIELDS

		protected int				m_ID = -1;								// The unique document ID
		protected string			m_Title = "";							// Title
		protected List<Page>		m_Pages = new List<Page>();				// The list of pages in that document
		protected List<Tag>			m_Tags = new List<Tag>();				// The list of tags pertaining to the document

		protected Institution		m_Sender = null;						// The institution that sent/issued the document
		protected DateTime			m_RegistrationDate = DateTime.Now;		// The date at which this document was registered in the database
		protected DateTime			m_IssueDate = DateTime.Now;				// The date at which the document was issued
		protected bool				m_IssueDateInvalid = true;
		protected DateTime			m_ReceptionDate = DateTime.Now;			// The date at which the document was received
		protected bool				m_ReceptionDateInvalid = true;
		protected DateTime			m_DueDate = DateTime.Now;				// The date at which the document is due (e.g. payment)
		protected bool				m_DueDateInvalid = true;
		protected float				m_Amount = 0.0f;						// An optional amount (e.g. bill amounts)

		protected bool				m_bInternalChange = false;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the document ID
		/// </summary>
		public int				ID			{ get { return m_ID; } }

		/// <summary>
		/// Gets or sets the document's title
		/// </summary>
		public string			Title		{ get { return m_Title; } set { m_Title = value; if ( TitleChanged != null ) TitleChanged( this, EventArgs.Empty ); } }

		/// <summary>
		/// Gets the list of pages in that document
		/// </summary>
		public Page[]			Pages		{ get { return m_Pages.ToArray(); } }

		/// <summary>
		/// Gets the list of tags attached to that document
		/// </summary>
		public Tag[]			Tags
		{
			get { return m_Tags.ToArray(); }
			set
			{
				if ( value == null )
					value = new Tag[0];

				Tag[]	CurrentTags = Tags;
				Tag[]	ToAdd = CollectionsOperations<Tag>.Subtraction( value, CurrentTags );
				Tag[]	ToRemove = CollectionsOperations<Tag>.Subtraction( CurrentTags, value );

				if ( ToAdd.Length == 0 && ToRemove.Length == 0 )
					return;

				m_bInternalChange = true;
				foreach ( Tag T in ToAdd )
					AddTag( T );
				foreach ( Tag T in ToRemove )
					RemoveTag( T );
				m_bInternalChange = false;

				// Notify only once
				if ( TagsChanged != null )
					TagsChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the institution that sent/issued the document
		/// </summary>
		public Institution		Sender
		{
			get { return m_Sender; }
			set
			{
				if ( value == m_Sender )
					return;

				if ( m_Sender != null )
					m_Database.DocumentUnregisteredInstitution( this, m_Sender );

				m_Sender = value;

				if ( m_Sender != null )
					m_Database.DocumentRegisteredInstitution( this, m_Sender );

				// Notify
				if ( SenderChanged != null )
					SenderChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets the date at which the document was registered in the database
		/// </summary>
		public DateTime			RegistrationDate	{ get { return m_RegistrationDate; } }

		/// <summary>
		/// Gets or sets the date at which the document was issued
		/// </summary>
		public DateTime			IssueDate
		{
			get { return m_IssueDate; }
			set
			{
				if ( value == m_IssueDate )
					return;

				m_IssueDate = value;
				m_IssueDateInvalid = false;

				// Also assign reception/due date as they're usually very close
				if ( m_ReceptionDateInvalid )
					ReceptionDate = value;
				if ( m_DueDateInvalid )
					DueDate = value;
		
				// Notify
				if ( DatesChanged != null )
					DatesChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Tells if the date is valid (must be assigned at least once to be valid)
		/// </summary>
		public bool				IsIssueDateInvalid	{ get { return m_IssueDateInvalid; } }

		/// <summary>
		/// Gets or sets the date at which the document was received
		/// </summary>
		public DateTime			ReceptionDate
		{
			get { return m_ReceptionDate; }
			set
			{
				if ( value == m_ReceptionDate )
					return;

				m_ReceptionDate = value;
				m_ReceptionDateInvalid = false;

				// Also assign issue/due date as they're usually very close
				if ( m_IssueDateInvalid )
					IssueDate = value;
				if ( m_DueDateInvalid )
					DueDate = value;

				// Notify
				if ( DatesChanged != null )
					DatesChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Tells if the date is valid (must be assigned at least once to be valid)
		/// </summary>
		public bool				IsReceptionDateInvalid	{ get { return m_ReceptionDateInvalid; } }

		/// <summary>
		/// Gets or sets the date at which the document is due
		/// </summary>
		public DateTime			DueDate
		{
			get { return m_DueDate; }
			set
			{
				if ( value == m_DueDate )
					return;

				m_DueDate = value;
				m_DueDateInvalid = false;

				// Also assign issue/reception date as they're usually very close
				if ( m_IssueDateInvalid )
					IssueDate = value;
				if ( m_ReceptionDateInvalid )
					ReceptionDate = value;

				// Notify
				if ( DatesChanged != null )
					DatesChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Tells if the date is valid (must be assigned at least once to be valid)
		/// </summary>
		public bool				IsDueDateInvalid	{ get { return m_DueDateInvalid; } }

		/// <summary>
		/// Gets the date at which the document was registered in the database
		/// </summary>
		public DateTime			BestSortDate	{ get { return !m_IssueDateInvalid ? m_IssueDate : (!m_ReceptionDateInvalid ? m_ReceptionDate : DateTime.Today); } }

		/// <summary>
		/// Gets or sets the amount due or received
		/// </summary>
		public float			Amount		{ get { return m_Amount; } set { m_Amount = value; if ( AmountChanged != null ) AmountChanged( this, EventArgs.Empty ); } }


		public event EventHandler	TitleChanged;
		public event EventHandler	SenderChanged;
		public event EventHandler	TagsChanged;
		public event EventHandler	PagesChanged;
		public event EventHandler	DatesChanged;
		public event EventHandler	AmountChanged;

		#endregion

		#region METHODS

		public Document( Database _Database, int _ID, Institution _Sender ) : base( _Database )
		{
			if ( _Sender == null )
				throw new Exception( "Invalid sender to create document with !" );

			m_ID = _ID;
			Sender = _Sender;
		}

		public Document( Database _Database, XmlElement _DocumentElement ) : base( _Database, _DocumentElement )
		{
		}

		public override string ToString()
		{
			return "ID:" + m_ID + " > " + m_Pages.Count + " Pages from:" + m_Sender + " Received:" + m_ReceptionDate.ToString( "f" );
		}

		/// <summary>
		/// Adds a new page to the document
		/// </summary>
		/// <param name="_Type"></param>
		/// <param name="_CodeName"></param>
		/// <param name="_Attachment"></param>
		/// <returns></returns>
		public Page	AddPage( Page.TYPE _Type, string _CodeName, Attachment _Attachment )
		{
			Page	NewPage = new Page( m_Database, this, _Type, _CodeName, _Attachment );
			RegisterPage( NewPage );

			return NewPage;
		}

		/// <summary>
		/// Removes an existing page from the document
		/// </summary>
		/// <param name="_Page"></param>
		public void	RemovePage( Page _Page )
		{
			if ( !m_Pages.Contains( _Page ) )
				return;

			_Page.Dispose();

			m_Pages.Remove( _Page );
			m_Database.DocumentUnregisteredPage( this, _Page );

			// Notify
			if ( !m_bInternalChange && PagesChanged != null )
				PagesChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Moves a page to the specific index
		/// </summary>
		/// <param name="_Page"></param>
		/// <param name="_Index"></param>
		public void	MovePage( Page _Page, int _Index )
		{
			int	CurrentIndex = m_Pages.IndexOf( _Page );
			m_Pages.Remove( _Page );

//			if ( _Index <= CurrentIndex )
				m_Pages.Insert( _Index, _Page );
//			else
//				m_Pages.Insert( _Index-1, _Page );

			// Notify
			if ( !m_bInternalChange && PagesChanged != null )
				PagesChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Gets the index of the specified page
		/// </summary>
		/// <param name="_Page"></param>
		/// <returns></returns>
		public int	GetIndexOfPage( Page _Page )
		{
			return m_Pages.IndexOf( _Page );
		}

		/// <summary>
		/// Clears the list of pages
		/// </summary>
		public void	ClearPages()
		{
			while ( m_Pages.Count > 0 )
				RemovePage( m_Pages[0] );
		}

		/// <summary>
		/// Finds the document page with the specified attachment
		/// </summary>
		/// <param name="_Attachment"></param>
		public Page	FindPageWithAttachment( Attachment _Attachment )
		{
			foreach ( Page P in m_Pages )
				if ( P.Attachment == _Attachment )
					return P;

			return null;
		}

		/// <summary>
		/// Attaches a new tag to the document
		/// </summary>
		/// <param name="_Tag"></param>
		public void	AddTag( Tag _Tag )
		{
			if ( m_Tags.Contains( _Tag ) )
				return;	// We already have it !

			m_Tags.Add( _Tag );
			m_Database.DocumentRegisteredTag( this, _Tag );

			// Notify
			if ( !m_bInternalChange && TagsChanged != null )
				TagsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Detaches an existing tag from the document
		/// </summary>
		/// <param name="_Tag"></param>
		public void	RemoveTag( Tag _Tag )
		{
			if ( !m_Tags.Contains( _Tag ) )
				return;

			m_Tags.Remove( _Tag );
			m_Database.DocumentUnregisteredTag( this, _Tag );

			// Notify
			if ( !m_bInternalChange && TagsChanged != null )
				TagsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Clears the list of pages
		/// </summary>
		public void	ClearTags()
		{
			while ( m_Tags.Count > 0 )
				RemoveTag( m_Tags[0] );
		}

		/// <summary>
		/// Performs a backup of the document
		/// </summary>
		/// <returns></returns>
		public object	Backup()
		{
			XmlDocument	Doc = new XmlDocument();
			XmlElement	Root = Doc.CreateElement( "ROOT" );
			Doc.AppendChild( Root );
			Save( Root );

			return Doc;
		}

		/// <summary>
		/// Restores the document from an earlier backup
		/// </summary>
		/// <param name="_Backup"></param>
		public void		Restore( object _Backup )
		{
			if ( !HasChanged( _Backup ) )
				return;

			XmlDocument	Doc = _Backup as XmlDocument;
			Load( Doc["ROOT"]["Document"] );
		}

		/// <summary>
		/// Tells if the document has changed based on a backup stored earlier
		/// </summary>
		/// <param name="XmlDocument"></param>
		/// <returns></returns>
		public bool		HasChanged( object _Backup )
		{
			XmlDocument	Doc = _Backup as XmlDocument;
			if ( Doc == null )
				throw new Exception( "Invalid backup !" );

			XmlDocument	CurrentStateDoc = Backup() as XmlDocument;

			return Doc.InnerXml != CurrentStateDoc.InnerXml;
		}

		public override void	Save( XmlElement _Parent )
		{
			XmlElement	DocumentElement = _Parent.OwnerDocument.CreateElement( "Document" );
			_Parent.AppendChild( DocumentElement );

			// Save document ID
			DocumentElement.SetAttribute( "ID", m_ID.ToString() );

			// Save title
			DocumentElement.SetAttribute( "Title", m_Title );

			// Save pages
			XmlElement	PagesElement = _Parent.OwnerDocument.CreateElement( "Pages" );
			DocumentElement.AppendChild( PagesElement );
			foreach ( Page P in m_Pages )
				P.Save( PagesElement );

			// Save tags
			XmlElement	TagsElement = _Parent.OwnerDocument.CreateElement( "Tags" );
			DocumentElement.AppendChild( TagsElement );
			foreach ( Tag T in m_Tags )
			{
				XmlElement	TagElement = _Parent.OwnerDocument.CreateElement( "Tag" );
				TagsElement.AppendChild( TagElement );
				TagElement.SetAttribute( "ID", T.ID.ToString() );
			}

			// Save internal data
			XmlElement	DataElement = _Parent.OwnerDocument.CreateElement( "Data" );
			DocumentElement.AppendChild( DataElement );

			DataElement.SetAttribute( "SenderID", m_Sender != null ? m_Sender.ID.ToString() : "" );
			DataElement.SetAttribute( "RegistrationDate", m_Database.DateToString( m_RegistrationDate ) );
			if ( !m_IssueDateInvalid )
				DataElement.SetAttribute( "IssueDate", m_Database.DateToString( m_IssueDate ) );
			if ( !m_ReceptionDateInvalid )
				DataElement.SetAttribute( "ReceptionDate", m_Database.DateToString( m_ReceptionDate ) );
			if ( !m_DueDateInvalid )
				DataElement.SetAttribute( "DueDate", m_Database.DateToString( m_DueDate ) );
			DataElement.SetAttribute( "Amount", m_Amount.ToString() );

			base.Save( DocumentElement );
		}

		public override void	Load( XmlElement _DocumentElement )
		{
			Dispose();

			base.Load(_DocumentElement);

			ErrorSheet	Errors = new ErrorSheet();

			// Load document ID
			if ( !int.TryParse( _DocumentElement.GetAttribute( "ID" ), out m_ID ) )
				Errors.AddError( "Failed to parse document ID !" );

			// Load title
			Title = _DocumentElement.GetAttribute( "Title" );

			// Load pages
			XmlElement	PagesElement = _DocumentElement["Pages"];
			foreach ( XmlElement PageElement in PagesElement.ChildNodes )
			{
				Page	P = new Page( m_Database, this, PageElement );
				RegisterPage( P );
			}

			// Load tags
			XmlElement	TagsElement = _DocumentElement["Tags"];
			foreach ( XmlElement TagElement in TagsElement.ChildNodes )
			{
				int	TagID;
				if ( !int.TryParse( TagElement.GetAttribute( "ID" ), out TagID ) )
				{
					Errors.AddError( "Failed to parse tag ID" );
					continue;
				}

				Tag	T = m_Database.FindTag( TagID );
				if ( T == null )
				{
					Errors.AddError( "Failed to retrieve tag with ID " + TagID );
					continue;
				}
				AddTag( T );
			}

			// Load internal data
			XmlElement	DataElement = _DocumentElement["Data"];

			int	SenderID;
			if ( int.TryParse( DataElement.GetAttribute( "SenderID" ), out SenderID ) )
			{
				Sender = m_Database.FindInstitution( SenderID );
				if ( m_Sender == null )
					Errors.AddError( "Failed to retrieve institution with ID " + SenderID );
			}
			else
				Errors.AddError( "Failed to parse sender ID" );

			m_RegistrationDate = m_Database.StringToDate( DataElement.GetAttribute( "RegistrationDate" ) );
			if ( DataElement.GetAttribute( "IssueDate" ) != "" )
			{
				m_IssueDate = m_Database.StringToDate( DataElement.GetAttribute( "IssueDate" ) );
				m_IssueDateInvalid = false;
			}
			if ( DataElement.GetAttribute( "ReceptionDate" ) != "" )
			{
				m_ReceptionDate = m_Database.StringToDate( DataElement.GetAttribute( "ReceptionDate" ) );
				m_ReceptionDateInvalid = false;
			}
			if ( DataElement.GetAttribute( "DueDate" ) != "" )
			{
				m_DueDate = m_Database.StringToDate( DataElement.GetAttribute( "DueDate" ) );
				m_DueDateInvalid = false;
			}
			float.TryParse( DataElement.GetAttribute( "Amount" ), out m_Amount );

			if ( Errors.HasErrors )
				throw new ErrorSheetException( "Errors occurred during document creation !", Errors );
		}

		protected void	RegisterPage( Page _Page )
		{
			m_Pages.Add( _Page );
			m_Database.DocumentRegisteredPage( this, _Page );

			// Notify
			if ( !m_bInternalChange && PagesChanged != null )
				PagesChanged( this, EventArgs.Empty );
		}

		public override void Dispose()
		{
			while ( m_Pages.Count > 0 )
				RemovePage( m_Pages[0] );

			while ( m_Tags.Count > 0 )
				RemoveTag( m_Tags[0] );

			Sender = null;

			base.Dispose();
		}

		#endregion
	}
}
