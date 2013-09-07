using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The database contains global informations about the managed documents as well as the list of those documents
	/// The database has a physical representation as it's the root directory containing all documents.
	/// </summary>
	public class Database : IDisposable, IComparer<Document>
	{
		#region CONSTANTS

		protected static readonly string	DATABASE_FILE_NAME = "Pognac.xml";
		protected const int					TIME_BEFORE_UPDATE = 5000;			// Wait 5 seconds before accessing the pending attachments after a watcher notification

		#endregion

		#region FIELDS

		protected DirectoryInfo		m_RootDirectory = null;

		protected List<Institution>	m_Institutions = new List<Institution>();	// The list of registered institutions
		protected List<Tag>			m_Tags = new List<Tag>();					// The list of registered tags
		protected List<Document>	m_Documents = new List<Document>();			// The list of registered documents

		protected Institution		m_InvalidInstitution = null;

		// Maps
		protected Dictionary<int,Tag>			m_ID2Tag = new Dictionary<int,Tag>();
		protected Dictionary<int,Institution>	m_ID2Institution = new Dictionary<int,Institution>();
		protected Dictionary<int,Document>		m_ID2Document = new Dictionary<int,Document>();
		protected Dictionary<string,Page>		m_CodeName2Page = new Dictionary<string,Page>();

		protected Dictionary<Tag,List<Document>>			m_Tag2Documents = new Dictionary<Tag,List<Document>>();
		protected Dictionary<Institution,List<Document>>	m_Institution2Documents = new Dictionary<Institution,List<Document>>();
		protected Dictionary<Page,Document>					m_Page2Document = new Dictionary<Page,Document>();

		// Counters for unique IDs
		protected int				m_TagsID = 0;
		protected int				m_InstitutionsID = 0;
		protected int				m_DocumentsID = 0;

		// Attachments management
		protected List<Attachment>	m_RegisteredAttachments = new List<Attachment>();
		protected List<Attachment>	m_UnAssignedAttachments = new List<Attachment>();

		// Directory watcher that will dynamically check for new attachments
		protected FileSystemWatcher	m_DirectoryWatcher = null;
		protected List<string>		m_PendingFiles2Add = new List<string>();
		protected List<string>		m_PendingFiles2Remove = new List<string>();
		protected List<FileInfo>	m_UnRecognizedFiles = new List<FileInfo>();
		protected System.Threading.Mutex	m_PendingFilesMutex = new System.Threading.Mutex();
		protected DateTime					m_PendingFilesLastAccess = DateTime.Now;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the documents in the database
		/// </summary>
		public Document[]			Documents			{ get { return m_Documents.ToArray(); } }

		/// <summary>
		/// Gets the amount of documents in the database
		/// </summary>
		public int					DocumentsCount		{ get { return m_Documents.Count; } }

		/// <summary>
		/// Occurs whenever the list of documents changes
		/// </summary>
		public event EventHandler	DocumentsChanged;

		/// <summary>
		/// Gets the tags in the database
		/// </summary>
		public Tag[]				Tags				{ get { return m_Tags.ToArray(); } }

		/// <summary>
		/// Gets the amount of tags in the database
		/// </summary>
		public int					TagsCount			{ get { return m_Tags.Count; } }

		/// <summary>
		/// Occurs whenever the list of documents changes
		/// </summary>
		public event EventHandler	TagsChanged;

		/// <summary>
		/// Gets the tags in the database
		/// </summary>
		public Institution[]		Institutions		{ get { return m_Institutions.ToArray(); } }

		/// <summary>
		/// Gets the amount of tags in the database
		/// </summary>
		public int					InstitutionsCount	{ get { return m_Institutions.Count; } }

		/// <summary>
		/// Occurs whenever the list of documents changes
		/// </summary>
		public event EventHandler	InstitutionsChanged;

		/// <summary>
		/// Gets the invalid institution to create empty documents with
		/// </summary>
		public Institution			InvalidInstitution	{ get { return m_InvalidInstitution; } }

		/// <summary>
		/// Gets the list of registered attachments (i.e. all attachments, whether they are attached to a document or not)
		/// </summary>
		public Attachment[]			RegisteredAttachments	{ get { return m_RegisteredAttachments.ToArray(); } }

		/// <summary>
		/// Gets the amount of registered attachments
		/// </summary>
		public int					RegisteredAttachmentsCount	{ get { return m_RegisteredAttachments.Count; } }

		/// <summary>
		/// Gets the list of attachments not yet assigned to a document
		/// </summary>
		public Attachment[]			UnAssignedAttachments	{ get { return m_UnAssignedAttachments.ToArray(); } }

		/// <summary>
		/// Gets the amount of unassigned attachments
		/// </summary>
		public int					UnAssignedAttachmentsCount	{ get { return m_UnAssignedAttachments.Count; } }

		/// <summary>
		/// Occurs whenever the list of un-assigned attachments changes
		/// </summary>
		public event EventHandler	UnAssignedAttachmentsChanged;

		/// <summary>
		/// Tells if there are attachments pending for registration
		/// </summary>
		public bool					HasPendingAttachments	{ get { return m_PendingFiles2Add.Count > 0 || m_PendingFiles2Remove.Count > 0; } }

		/// <summary>
		/// Gets the list of files that are not recognized (i.e. supported) by the database
		/// </summary>
		public FileInfo[]			UnRecognizedFiles	{ get { return m_UnRecognizedFiles.ToArray(); } }

		#endregion

		#region METHODS

		public Database( DirectoryInfo _RootDirectory )
		{
			if ( _RootDirectory == null )
				throw new Exception( "Invalid directory !" );
			if ( !_RootDirectory.Exists )
				throw new Exception( "Provided directory \"" + _RootDirectory + "\" does not physically exist on disk !" );

			m_RootDirectory = _RootDirectory;

			// Create the default, invalid institution to create empty documents with...
			m_InvalidInstitution = new Institution( this, -1, "INVALID" );

			BuildAndWatchAttachments();
		}

		#region IDisposable Members

		public void Dispose()
		{
			// Dispose of the watcher first
			m_DirectoryWatcher.Dispose();
			m_DirectoryWatcher = null;

			// Dispose of documents
			while ( m_Documents.Count > 0 )
				RemoveDocument( m_Documents[0] );

			// Dispose of tags
			while ( m_Tags.Count > 0 )
				RemoveTag( m_Tags[0] );

			// Dispose of institutions
			while ( m_Institutions.Count > 0 )
				RemoveInstitution( m_Institutions[0] );

			// Reset counters
			m_TagsID = 0;
			m_InstitutionsID = 0;
			m_DocumentsID = 0;
		}

		#endregion

		#region Load / Save

		public void		Save()
		{
			FileInfo	DatabaseFileName = new FileInfo( Path.Combine( m_RootDirectory.FullName, DATABASE_FILE_NAME ) );

			//////////////////////////////////////////////////////////////////////////
			// Save the database :
			//	_ Tags
			//	_ Institutions
			//	_ Documents
			//		=> Document pages
			//
			try
			{
				XmlDocument	Doc = new XmlDocument();
				XmlElement	Root = Doc.CreateElement( "Root" );
				Doc.AppendChild( Root );

				// Save tags
				XmlComment	Comment = Doc.CreateComment( "This section contains all the registered tags" );
				Root.AppendChild( Comment );

				XmlElement	TagsElement = Doc.CreateElement( "Tags" );
				Root.AppendChild( TagsElement );
				foreach ( Tag T in m_Tags )
					T.Save( TagsElement );

				// Save institutions
				Comment = Doc.CreateComment( "This section contains all the registered institutions" );
				Root.AppendChild( Comment );

				XmlElement	InstitutionsElement = Doc.CreateElement( "Institutions" );
				Root.AppendChild( InstitutionsElement );
				foreach ( Institution I in m_Institutions )
					I.Save( InstitutionsElement );

				// Save documents
				Comment = Doc.CreateComment( "This section contains all the registered documents" );
				Root.AppendChild( Comment );

				XmlElement	DocumentsElement = Doc.CreateElement( "Documents" );
				Root.AppendChild( DocumentsElement );
				foreach ( Document D in m_Documents )
					D.Save( DocumentsElement );

				// ===============================================================
				// Perform backup
				Backup( DatabaseFileName );

				// Actual save
				Doc.Save( DatabaseFileName.FullName );
			}
			catch ( Exception _e )
			{
				throw new Exception( "An error occurred while saving the database !", _e );
			}
			finally
			{
			}
		}

		public void		Load()
		{
			FileInfo	DatabaseFileName = new FileInfo( Path.Combine( m_RootDirectory.FullName, DATABASE_FILE_NAME ) );
			if ( !DatabaseFileName.Exists )
				return;	// First time creation...

			//////////////////////////////////////////////////////////////////////////
			// Load the database :
			//	_ Tags
			//	_ Institutions
			//	_ Documents
			//		=> Document pages
			//
			ErrorSheet	Errors = new ErrorSheet();

			try
			{
				XmlDocument	Doc = new XmlDocument();
				Doc.Load( DatabaseFileName.FullName );

				XmlElement	Root = Doc["Root"];
				if ( Root == null )
					throw new Exception( "Failed to retrieve the ROOT element !" );

				// Load tags
				XmlElement	TagsElement = Root["Tags"];
				if ( TagsElement == null )
					throw new Exception( "Failed to retrieve the \"Tags\" element !" );

				foreach ( XmlElement TagElement in TagsElement.ChildNodes )
					try
					{
						Tag	T = new Tag( TagElement );
						RegisterTag( T );
						m_TagsID = Math.Max( m_TagsID, T.ID );
					}
					catch ( Exception _e )
					{
						Errors.AddError( _e );
					}

				// Load institutions
				XmlElement	InstitutionsElement = Root["Institutions"];
				if ( InstitutionsElement == null )
					throw new Exception( "Failed to retrieve the \"Institutions\" element !" );

				foreach ( XmlElement InstitutionElement in InstitutionsElement.ChildNodes )
					try
					{
						Institution	I = new Institution( this, InstitutionElement );
						RegisterInstitution( I );
						m_InstitutionsID = Math.Max( m_InstitutionsID, I.ID );
					}
					catch ( Exception _e )
					{
						Errors.AddError( _e );
					}

				// Load documents
				XmlElement	DocumentsElement = Root["Documents"];
				if ( DocumentsElement == null )
					throw new Exception( "Failed to retrieve the \"Documents\" element !" );

				foreach ( XmlElement DocumentElement in DocumentsElement.ChildNodes )
					try
					{
						Document	D = new Document( this, DocumentElement );
						RegisterDocument( D );
						m_DocumentsID = Math.Max( m_DocumentsID, D.ID );
					}
					catch ( ErrorSheetException _e )
					{
						Errors.AddError( _e );
					}
					catch ( Exception _e )
					{
						Errors.AddError( "An unhandled error occurred while opening one of the documents : " + _e.Message );
					}
			}
			catch ( Exception _e )
			{
				Errors.AddError( _e );

				throw new ErrorSheetException( "An error occurred while loading the database !", Errors );
			}
			finally
			{
			}
		}

		#endregion

		#region Query

		/// <summary>
		/// Performs a query
		/// </summary>
		/// <param name="_Tags">The list of tags to search documents</param>
		/// <param name="_Institutions">The list of institutions that issued the documents</param>
		/// <returns></returns>
		public Document[]	Query( Tag[] _Tags, Institution[] _Institutions )
		{
			return Query( _Tags, _Institutions, new DateTime( 1975, 9, 12 ), DateTime.Now );
		}

		/// <summary>
		/// Performs a query
		/// </summary>
		/// <param name="_Tags">The list of tags to search documents</param>
		/// <param name="_Institutions">The list of institutions that issued the documents</param>
		/// <param name="_From">The start issue date to check</param>
		/// <param name="_To">The end issue date to check</param>
		/// <returns></returns>
		public Document[]	Query( Tag[] _Tags, Institution[] _Institutions, DateTime _From, DateTime _To )
		{
			// Search tags first as we can early exit
			Document[]	TaggedDocuments = m_Documents.ToArray();	// Use all documents
			if ( _Tags != null )
			{	// Search by tag using AND operations
				foreach ( Tag T in _Tags )
				{
					if ( !m_Tag2Documents.ContainsKey( T ) )
					{	// No use to go further as no document is tagged with that tag...
						TaggedDocuments = new Document[0];
						break;
					}

					Document[]	SubSet = m_Tag2Documents[T].ToArray();

					// Reduce the set by intersection
					TaggedDocuments = CollectionsOperations<Document>.Intersection( TaggedDocuments, SubSet );
					if ( TaggedDocuments.Length == 0 )
						return TaggedDocuments;	// No use to go further...
				}
			}

			// If no institution was provided, return the current result
			if ( _Institutions != null && _Institutions.Length > 0 )
			{
				// Search institutions by augmenting the set of documents
				Document[]	InstitutionDocuments = new Document[0];	// Use no document
				foreach ( Institution I in _Institutions )
					if ( m_Institution2Documents.ContainsKey( I ) )
					{
						Document[]	SubSet = m_Institution2Documents[I].ToArray();

						// Augment the set by union
						InstitutionDocuments = CollectionsOperations<Document>.Union( InstitutionDocuments, SubSet );
					}

				// The final result is the intersection between tagged documents and institution documents
				TaggedDocuments = CollectionsOperations<Document>.Intersection( TaggedDocuments, InstitutionDocuments );
			}

			// Finally, filter by date
			List<Document>	Result = new List<Document>();
			foreach ( Document D in TaggedDocuments )
			{
				DateTime	DocDate = D.BestSortDate;
				if ( DocDate >= _From && DocDate <= _To )
					Result.Add( D );
			}

			Result.Sort( this );

			return Result.ToArray();
		}

		#endregion

		#region Creation / Removal

		/// <summary>
		/// Creates a new document
		/// </summary>
		/// <param name="_Sender"></param>
		/// <returns></returns>
		public Document		CreateDocument( Institution _Sender )
		{
			Document	D = new Document( this, ++m_DocumentsID, _Sender );
			RegisterDocument( D );

			// Notify
			if ( DocumentsChanged != null )
				DocumentsChanged( this, EventArgs.Empty );

			return D;
		}

		/// <summary>
		/// Removes an existing document
		/// </summary>
		/// <param name="_Document"></param>
		public void			RemoveDocument( Document _Document )
		{
			if ( _Document == null )
				throw new Exception( "Invalid document !" );

			_Document.Dispose();

			m_Documents.Remove( _Document );
			m_ID2Document.Remove( _Document.ID );

			// Notify
			if ( DocumentsChanged != null )
				DocumentsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Creates a new tag
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public Tag			CreateTag( string _Name )
		{
			Tag	T = new Tag( ++m_TagsID, _Name );
			RegisterTag( T );

			// Notify
			if ( TagsChanged != null )
				TagsChanged( this, EventArgs.Empty );

			return T;
		}

		/// <summary>
		/// Removes an existing tag
		/// </summary>
		/// <param name="_Tag"></param>
		public void			RemoveTag( Tag _Tag )
		{
			if ( _Tag == null )
				throw new Exception( "Invalid tag !" );
			if ( !m_Tags.Contains( _Tag ) )
				throw new Exception( "Tag " + _Tag + " is not registered !" );

//			_Tag.Dispose();

			m_Tags.Remove( _Tag );
			m_ID2Tag.Remove( _Tag.ID );

			// Notify
			if ( TagsChanged != null )
				TagsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Gets the list of all documents referencing the provided tag
		/// </summary>
		/// <param name="_Tag"></param>
		/// <returns></returns>
		public Document[]	GetDocumentsUsingTag( Tag _Tag )
		{
			if ( _Tag == null )
				throw new Exception( "Invalid tag !" );

			return m_Tag2Documents.ContainsKey( _Tag ) ? m_Tag2Documents[_Tag].ToArray() : new Document[0];
		}

		/// <summary>
		/// Creates a new institution
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public Institution	CreateInstitution( string _Name )
		{
			Institution	I = new Institution( this, ++m_InstitutionsID, _Name );
			RegisterInstitution( I );

			// Notify
			if ( InstitutionsChanged != null )
				InstitutionsChanged( this, EventArgs.Empty );

			return I;
		}

		/// <summary>
		/// Removes an existing institution
		/// </summary>
		/// <param name="_Institution"></param>
		public void			RemoveInstitution( Institution _Institution )
		{
			if ( _Institution == null )
				throw new Exception( "Invalid institution !" );
			if ( !m_Institutions.Contains( _Institution ) )
				throw new Exception( "Institution " + _Institution + " is not registered !" );

			_Institution.Dispose();

			m_Institutions.Remove( _Institution );
			m_ID2Institution.Remove( _Institution.ID );

			// Notify
			if ( InstitutionsChanged != null )
				InstitutionsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Gets the list of all documents referencing the provided institution
		/// </summary>
		/// <param name="_Tag"></param>
		/// <returns></returns>
		public Document[]	GetDocumentsUsingInstitution( Institution _Institution )
		{
			if ( _Institution == null )
				throw new Exception( "Invalid institution !" );

			return m_Institution2Documents.ContainsKey( _Institution ) ? m_Institution2Documents[_Institution].ToArray() : new Document[0];
		}

		#endregion

		#region Find Methods

		/// <summary>
		/// Finds a tag by ID
		/// </summary>
		/// <param name="_ID"></param>
		/// <returns></returns>
		public Tag		FindTag( int _ID )
		{
			return m_ID2Tag.ContainsKey( _ID ) ? m_ID2Tag[_ID] : null;
		}

		/// <summary>
		/// Finds a tag by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public Tag		FindTag( string _Name, bool _bCaseSensitive )
		{
			string NameLower = _Name.ToLower();
			foreach ( Tag T in m_Tags )
				if ( (_bCaseSensitive && T.Name == _Name) || (!_bCaseSensitive && T.Name.ToLower() == NameLower) )
					return T;

			return null;
		}

		/// <summary>
		/// Finds an institution by ID
		/// </summary>
		/// <param name="_ID"></param>
		/// <returns></returns>
		public Institution	FindInstitution( int _ID )
		{
			return m_ID2Institution.ContainsKey( _ID ) ? m_ID2Institution[_ID] : null;
		}

		/// <summary>
		/// Finds an institution by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public Institution	FindInstitution( string _Name, bool _bCaseSensitive )
		{
			string NameLower = _Name.ToLower();
			foreach ( Institution I in m_Institutions )
				if ( (_bCaseSensitive && I.Name == _Name) || (!_bCaseSensitive && I.Name.ToLower() == NameLower) )
					return I;

			return null;
		}

		/// <summary>
		/// Finds a page by codename
		/// </summary>
		/// <param name="_CodeName"></param>
		/// <returns></returns>
		public Page		FindPage( string _CodeName )
		{
			return m_CodeName2Page.ContainsKey( _CodeName ) ? m_CodeName2Page[_CodeName] : null;
		}

		#endregion

		#region Attachments Management

		/// <summary>
		/// Finds an attachment by ID
		/// </summary>
		/// <param name="_AttachementID"></param>
		/// <returns></returns>
		public Attachment	FindAttachment( string _AttachementID )
		{
			if ( _AttachementID == "" )
				return null;

			foreach ( Attachment A in m_RegisteredAttachments )
				if ( A.ID == _AttachementID )
					return A;

			return null;
		}

		/// <summary>
		/// Finds an attachment by file name
		/// </summary>
		/// <param name="_AttachementID"></param>
		/// <returns></returns>
		public Attachment	FindAttachment( FileInfo _FileName )
		{
			foreach ( Attachment A in m_RegisteredAttachments )
				if ( A.FileName.FullName == _FileName.FullName )
					return A;

			return null;
		}

		/// <summary>
		/// Tells if the file is already registered in the database
		/// </summary>
		/// <param name="_FileName"></param>
		/// <returns></returns>
		public bool			IsFileRegistered( FileInfo _FileName )
		{
			return FindAttachment( _FileName ) != null;
		}

		/// <summary>
		/// Claims an attachment for an element in the database and removes the attachment from the list of available attachments
		/// </summary>
		/// <param name="_Attachment"></param>
		public void			ClaimAttachment( Attachment _Attachment )
		{
			if ( !m_UnAssignedAttachments.Contains( _Attachment ) )
				return;

			m_UnAssignedAttachments.Remove( _Attachment );

			// Notify
			if ( UnAssignedAttachmentsChanged != null )
				UnAssignedAttachmentsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Releases an attachment from an element in the database and puts it back into the list of available attachments
		/// </summary>
		/// <param name="_Attachment"></param>
		public void			ReleaseAttachment( Attachment _Attachment )
		{
			if ( _Attachment == null )
				return;

			m_UnAssignedAttachments.Add( _Attachment );

			// Notify
			if ( UnAssignedAttachmentsChanged != null )
				UnAssignedAttachmentsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Checks and updates the list of attachments
		/// This should be called by the main thread in the Idle or a timer call whenever new attachments are detected by the DirectoryWatcher.
		/// New attachments are detected and signaled through our "HasPendingAttachments" property.
		/// </summary>
		public void			CheckForNewAttachments()
		{
			if ( (DateTime.Now - m_PendingFilesLastAccess).TotalMilliseconds < TIME_BEFORE_UPDATE )
				return;	// Last access was too early...

			// Copy pending list of files
			if ( !m_PendingFilesMutex.WaitOne() )
				return;

			string[]	FilesToAdd = m_PendingFiles2Add.ToArray();
			string[]	FilesToRemove = m_PendingFiles2Remove.ToArray();
			m_PendingFiles2Add.Clear();
			m_PendingFiles2Remove.Clear();
			m_PendingFilesLastAccess = DateTime.Now;

			m_PendingFilesMutex.ReleaseMutex();

			// Add new attachments
			bool	AttachmentsChanged = false;

			foreach ( string FileToAdd in FilesToAdd )
			{
				FileInfo	FI = new FileInfo( FileToAdd );
				if ( IsFileRegistered( FI ) )
					continue;	// Already registered...

				try
				{
					Attachment	A = BuildAttachmentFromFile( FI );
					m_RegisteredAttachments.Add( A );
					m_UnAssignedAttachments.Add( A );

					AttachmentsChanged = true;
				}
				catch ( Exception )
				{
					m_UnRecognizedFiles.Add( FI );
				}
			}

			// Remove obsolete attachments
			foreach ( string FileToRemove in FilesToRemove )
			{
				FileInfo	FI = new FileInfo( FileToRemove );
				if ( !IsFileRegistered( FI ) )
					continue;	// Not registered...

				// Remove the attachment
				Attachment	A = FindAttachment( FI );
				m_RegisteredAttachments.Remove( A );
				m_UnAssignedAttachments.Remove( A );

				// Dispose of the attachment
				A.Dispose();

				AttachmentsChanged = true;
			}

			// Notify
			if ( AttachmentsChanged && UnAssignedAttachmentsChanged != null )
				UnAssignedAttachmentsChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Builds the list of attachments from disk
		/// </summary>
		protected void		BuildAndWatchAttachments()
		{
			// Create the directory watcher to monitor new files
			m_DirectoryWatcher = new FileSystemWatcher( m_RootDirectory.FullName );
			m_DirectoryWatcher.BeginInit();
			m_DirectoryWatcher.IncludeSubdirectories = false;
			m_DirectoryWatcher.Changed += new FileSystemEventHandler( DirectoryWatcher_Changed );
			m_DirectoryWatcher.Created += new FileSystemEventHandler( DirectoryWatcher_Created );
			m_DirectoryWatcher.Deleted += new FileSystemEventHandler( DirectoryWatcher_Deleted );
			m_DirectoryWatcher.Error += new ErrorEventHandler( DirectoryWatcher_Error );
			m_DirectoryWatcher.Renamed += new RenamedEventHandler( DirectoryWatcher_Renamed );
			m_DirectoryWatcher.EnableRaisingEvents = true;
			m_DirectoryWatcher.EndInit();

			// Parse the directory for any file
			foreach ( FileInfo FI in m_RootDirectory.EnumerateFiles() )
				if ( !IsInternalFile( FI ) )
					m_PendingFiles2Add.Add( FI.FullName );

			// Build available attachments from these files
			m_PendingFilesLastAccess = new DateTime( 1975, 9, 12 );
			CheckForNewAttachments();
		}

		/// <summary>
		/// Builds an attachment file from a file name based on supported extensions
		/// </summary>
		/// <param name="_FileName"></param>
		/// <returns></returns>
		protected Attachment	BuildAttachmentFromFile( FileInfo _FileName )
		{
			string	Extension = _FileName.Extension.ToLower();
			switch ( Extension )
			{
				case ".jpg":
				case ".bmp":
				case ".png":
				case ".gif":
					return new AttachmentImage( this, _FileName );	// Standard image

				case ".pdf":
					return new AttachmentPDF( this, _FileName );	// PDF

				default:
					throw new Exception( "Unsupported extension \"" + Extension + "\" !" );
			}
		}

		#endregion

		#region Registration

		/// <summary>
		/// Registers a new document to the database
		/// </summary>
		/// <param name="_Document"></param>
		public void		RegisterDocument( Document _Document )
		{
			m_Documents.Add( _Document );
			m_ID2Document.Add( _Document.ID, _Document );
		}

		/// <summary>
		/// Registers a new tag to the database
		/// </summary>
		/// <param name="_Document"></param>
		public void		RegisterTag( Tag _Tag )
		{
			m_Tags.Add( _Tag );
			m_ID2Tag.Add( _Tag.ID, _Tag );
		}

		/// <summary>
		/// Registers a new institution to the database
		/// </summary>
		/// <param name="_Document"></param>
		public void		RegisterInstitution( Institution _Institution )
		{
			m_Institutions.Add( _Institution );
			m_ID2Institution.Add( _Institution.ID, _Institution );
		}

		/// <summary>
		/// Notifies a document registered a tag
		/// </summary>
		/// <param name="_Document"></param>
		/// <param name="_Tag"></param>
		internal void	DocumentRegisteredTag( Document _Document, Tag _Tag )
		{
			if ( !m_Tag2Documents.ContainsKey( _Tag ) )
				m_Tag2Documents.Add( _Tag, new List<Document>() );
			m_Tag2Documents[_Tag].Add( _Document );
		}

		/// <summary>
		/// Notifies a document unregistered a tag
		/// </summary>
		/// <param name="_Document"></param>
		/// <param name="_Tag"></param>
		internal void	DocumentUnregisteredTag( Document _Document, Tag _Tag )
		{
			m_Tag2Documents[_Tag].Remove( _Document );
			if ( m_Tag2Documents[_Tag].Count == 0 )
				m_Tag2Documents.Remove( _Tag );
		}

		/// <summary>
		/// Notifies a document registered a tag
		/// </summary>
		/// <param name="_Document"></param>
		/// <param name="_Tag"></param>
		internal void	DocumentRegisteredInstitution( Document _Document, Institution _Institution )
		{
			if ( !m_Institution2Documents.ContainsKey( _Institution ) )
				m_Institution2Documents.Add( _Institution, new List<Document>() );
			m_Institution2Documents[_Institution].Add( _Document );
		}

		/// <summary>
		/// Notifies a document unregistered a tag
		/// </summary>
		/// <param name="_Document"></param>
		/// <param name="_Tag"></param>
		internal void	DocumentUnregisteredInstitution( Document _Document, Institution _Institution )
		{
			m_Institution2Documents[_Institution].Remove( _Document );
			if ( m_Institution2Documents[_Institution].Count == 0 )
				m_Institution2Documents.Remove( _Institution );
		}

		/// <summary>
		/// Notifies a document registered a page
		/// </summary>
		/// <param name="_Document"></param>
		/// <param name="_Tag"></param>
		internal void	DocumentRegisteredPage( Document _Document, Page _Page )
		{
			m_Page2Document[_Page] = _Document;
		}

		/// <summary>
		/// Notifies a document unregistered a page
		/// </summary>
		/// <param name="_Document"></param>
		/// <param name="_Tag"></param>
		internal void	DocumentUnregisteredPage( Document _Document, Page _Page )
		{
			m_Page2Document.Remove( _Page );
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Gets the serializable relative file name from an absolute file name that exists on disk
		/// </summary>
		/// <param name="_AbsoluteFileName">The name of the file that physically exists on disk (can be null)</param>
		/// <returns>The serializable name of the file, relative to that database's root</returns>
		internal string		GetRelativeFileName( FileInfo _AbsoluteFileName )
		{
			if ( _AbsoluteFileName == null )
				return "";

			if ( _AbsoluteFileName.FullName.IndexOf( m_RootDirectory.FullName ) != 0 )
				throw new Exception( "File \"" + _AbsoluteFileName + "\" is not relative to the database's root directory (i.e. \"" + m_RootDirectory + "\") !" );

			return _AbsoluteFileName.FullName.Remove( 0, m_RootDirectory.FullName.Length+1 );	// +1 so we remove the trailing \
		}

		/// <summary>
		/// Gets the absolute file name that exists on disk from a serializable relative file name
		/// </summary>
		/// <param name="_RelativeFileName">The serializable name of the file, relative to that database's root</param>
		/// <returns>The name of the file that physically exists on disk (can be null)</returns>
		internal FileInfo	GetAbsoluteFileName( string _RelativeFileName )
		{
			if ( _RelativeFileName == null || _RelativeFileName == "" )
				return null;

			return new FileInfo( Path.Combine( m_RootDirectory.FullName, _RelativeFileName ) );
		}

		internal void		Backup( FileInfo _FileToBackup )
		{
			if ( _FileToBackup == null || !_FileToBackup.Exists )
				return;

			FileInfo	Target = new FileInfo( _FileToBackup.FullName + ".bak" );
			_FileToBackup.CopyTo( Target.FullName, true );
		}

		internal string		DateToString( DateTime _Date )
		{
			return _Date.ToString( "s" );
		}

		internal DateTime	StringToDate( string _Date )
		{
			DateTime	Result = DateTime.Now;
			if ( !DateTime.TryParse( _Date, out Result ) )
				Result = new DateTime( 1975, 9, 12 );

			return Result;
		}

		/// <summary>
		/// Parses a user-fed list of tags and institutions separated by ,;/|: and returns lists of actual tags and institutions existing in the database
		/// (the parsing is case-insensitive)
		/// </summary>
		/// <param name="_Database"></param>
		/// <param name="_List">The list of tags and institutions separated by commas to parse</param>
		/// <param name="_Tags">The list of actual recognized tags</param>
		/// <param name="_Institutions">The list of actual recognized institutions</param>
		/// <param name="_Errors">The error sheet to output parsing errors (i.e. unrecognized items)</param>
		/// <param name="_CleanedUpList">The final list of tags and institutions free of mis-parsed items (i.e. unrecognized items are trimmed out)</param>
		public static void	ParseTagsAndInstitutions( Database _Database, string _List, List<Tag> _Tags, List<Institution> _Institutions, ErrorSheet _Errors, out string _CleanedUpList )
		{
			string[]					QueryTerms = _List.Split( ',', ';', '/', '|', ':' );

			_CleanedUpList = "";
			foreach ( string DirtyQueryTerm in QueryTerms )
				if ( DirtyQueryTerm != "" )
				{
					string	QueryTerm = DirtyQueryTerm.Trim();

					Documents.Tag	T = _Database.FindTag( QueryTerm, false );
					if ( T != null )
					{
						_Tags.Add( T );
						_CleanedUpList += (_CleanedUpList != "" ? ", " : "") + T.Name;
					}
					else
					{
						Documents.Institution	I = _Database.FindInstitution( QueryTerm, false );
						if ( I != null )
						{
							_Institutions.Add( I );
							_CleanedUpList += (_CleanedUpList != "" ? ", " : "") + I.Name;
						}
						else
							_Errors.AddError( "Query term \"" + QueryTerm + "\" doesn't match any tag or institution name (not included in query)..." );
					}
				}
		}

		/// <summary>
		/// Checks if the provided file is a file used internally by the database (in which case, it shouldn't be processed)
		/// </summary>
		/// <param name="_File"></param>
		/// <returns></returns>
		public static bool	IsInternalFile( FileInfo _File )
		{
			if ( _File == null )
				return false;	// Invalid file anyway...

			string	Extension = _File.Extension.ToLower();

			return	Extension == ".xml" ||	// For the moment, don't process any XML file...
					Extension == ".bak" ||	// ...or any backup file
					Extension == ".thumb";	// ...or thumbnail file
		}

		#endregion

		#region IComparer<Document> Members

		// Used to sort query results by date
		public int Compare( Document x, Document y )
		{
			return x.BestSortDate.CompareTo( y.BestSortDate );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		// File System Watcher

		protected void DirectoryWatcher_Created( object sender, FileSystemEventArgs e )
		{
			if ( IsInternalFile( new FileInfo( e.FullPath ) ) )
				return;

			if ( !m_PendingFilesMutex.WaitOne() )
				return;

			m_PendingFiles2Add.Add( e.FullPath );
			m_PendingFilesLastAccess = DateTime.Now;

			m_PendingFilesMutex.ReleaseMutex();
		}

		protected void DirectoryWatcher_Deleted( object sender, FileSystemEventArgs e )
		{
			if ( IsInternalFile( new FileInfo( e.FullPath ) ) )
				return;

			if ( !m_PendingFilesMutex.WaitOne() )
				return;

			m_PendingFiles2Remove.Add( e.FullPath );
			m_PendingFilesLastAccess = DateTime.Now;

			m_PendingFilesMutex.ReleaseMutex();
		}

		protected void DirectoryWatcher_Changed( object sender, FileSystemEventArgs e )
		{
			if ( !m_PendingFilesMutex.WaitOne() )
				return;

			m_PendingFilesLastAccess = DateTime.Now;

			m_PendingFilesMutex.ReleaseMutex();
		}

		protected void DirectoryWatcher_Renamed( object sender, RenamedEventArgs e )
		{
		}

		protected void DirectoryWatcher_Error( object sender, ErrorEventArgs e )
		{
		}

		#endregion
	}
}
