using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace Pognac
{
	public partial class PognacForm : Form
	{
		#region CONSTANTS

		protected static readonly string	APPLICATION_REGISTRY_URL = @"Software\Patapom";

		#endregion

		#region NESTED TYPES

		protected class	Registry
		{
			#region FIELDS

			protected Microsoft.Win32.RegistryKey	m_RegistryKey = null;
			protected bool							m_bFirstTime = false;

			protected Dictionary<string,string>		m_DynamicFields = new Dictionary<string,string>();

			// Declare your registry data here
// 			public bool			m_bTestBool = true;
// 			public string		m_TestString = "Bisou";
// 			public float		m_TestFloat = 3.14f;
// 			public int			m_TestInt = 1234;

			public string		DocumentsDirectory = "";	// The main directory that contains the documents to manage

			#endregion

			#region PROPERTIES

			public bool			FirstTimeUse	{ get { return m_bFirstTime; } }

			#endregion

			#region METHODS

			public Registry()
			{
				string KeyName = APPLICATION_REGISTRY_URL + @"\Pognac";
				m_RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey( KeyName, true );
				if ( m_RegistryKey == null )
				{
					m_RegistryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( KeyName );
					m_bFirstTime = true;
				}
			}

			public void		SetDynamicField( string _Field, string _Value )
			{
				m_DynamicFields[_Field] = _Value;
			}

			public string	GetDynamicField( string _Field )
			{
				return m_DynamicFields.ContainsKey( _Field ) ? m_DynamicFields[_Field] : "";
			}

			public void		Load()
			{
				bool	TempBool;
				float	TempFloat;
				int		TempInt;
				string	ValueString;

				System.Reflection.FieldInfo[]	PublicFields = this.GetType().GetFields();
				foreach ( System.Reflection.FieldInfo Field in PublicFields )
				{
					object	Value = m_RegistryKey.GetValue( Field.Name, null );
					if ( Value == null )
						continue;	// Failed to retrieve that value...

					ValueString = Value as string;

					if ( Field.FieldType == typeof(string) )
						Field.SetValue( this, ValueString );
					else if ( Field.FieldType == typeof(bool) && bool.TryParse( ValueString, out TempBool ) )
						Field.SetValue( this, TempBool );
					else if ( Field.FieldType == typeof(float) && float.TryParse( ValueString, out TempFloat ) )
						Field.SetValue( this, TempFloat );
					else if ( Field.FieldType == typeof(int) && int.TryParse( ValueString, out TempInt ) )
						Field.SetValue( this, TempInt );
				}

				// Load dynamic fields
				string		KeyNames = m_RegistryKey.GetValue( "DynamicFieldKeys", "" ) as string;
				string[]	Keys = KeyNames.Split( ',' );
				foreach ( string Key in Keys )
					if ( Key != "" )
						m_DynamicFields[Key] = m_RegistryKey.GetValue( "DynField_"+Key, "" ) as string;
			}

			public void		Save()
			{
				string	ValueString;

				System.Reflection.FieldInfo[]	PublicFields = this.GetType().GetFields();
				foreach ( System.Reflection.FieldInfo Field in PublicFields )
				{
					if ( Field.FieldType != typeof(string) &&
						Field.FieldType != typeof(float) &&
						Field.FieldType != typeof(int) &&
						Field.FieldType != typeof(bool) )
						continue;

					ValueString = Field.GetValue( this ).ToString();
					m_RegistryKey.SetValue( Field.Name, ValueString );
				}

				// Save dynamic fields
				string	Keys = "";
				foreach ( string Key in m_DynamicFields.Keys )
				{
					Keys += (Keys != "" ? "," : "") + Key;
					m_RegistryKey.SetValue( "DynField_"+Key, m_DynamicFields[Key] );
				}
				m_RegistryKey.SetValue( "DynamicFieldKeys", Keys );
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected static Registry		ms_Registry = new Registry();

		// The directory we're working with
		protected DirectoryInfo			m_WorkingDirectory = null;

		// The main database
		protected Documents.Database	m_Database = null;

		// The list of opened documents editors
		protected Dictionary<Documents.Document,DocumentForm>	m_Document2Form = new Dictionary<Documents.Document,DocumentForm>();
		protected Dictionary<DocumentForm,Documents.Document>	m_Form2Document = new Dictionary<DocumentForm,Documents.Document>();

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Assigning a working directory will automatically open the database
		/// </summary>
		public DirectoryInfo		WorkingDirectory
		{
			get { return m_WorkingDirectory; }
			set
			{
				if ( value == m_WorkingDirectory )
					return;

				if ( m_WorkingDirectory != null )
					CloseDatabase();		// Save and close...

				m_WorkingDirectory = value;

				bool	bDirectoryValid = m_WorkingDirectory != null && m_WorkingDirectory.Exists;

				if ( bDirectoryValid )
				{
					OpenDatabase();

					// Save in registry
					ms_Registry.DocumentsDirectory = m_WorkingDirectory != null ? m_WorkingDirectory.FullName : "";
				}

				// Update GUI
				labelWorkingDirectory.Text = m_WorkingDirectory != null ? m_WorkingDirectory.FullName : "<NOT SET => GO TO PREFERENCES>";
			}
		}

		protected Documents.Database	Database
		{
			get { return m_Database; }
			set
			{
				if ( value == m_Database )
					return;

				// Close existing database
				if ( m_Database != null )
				{
					m_Database.DocumentsChanged -= new EventHandler( Database_DocumentsChanged );
					m_Database.TagsChanged -= new EventHandler( Database_TagsChanged );
					m_Database.InstitutionsChanged -= new EventHandler( Database_InstitutionsChanged );
					m_Database.UnAssignedAttachmentsChanged -= new EventHandler( Database_UnAssignedAttachmentsChanged );

					m_Database.Dispose();
				}

				m_Database = value;

				if ( m_Database != null )
				{
					m_Database.DocumentsChanged += new EventHandler( Database_DocumentsChanged );
					m_Database.TagsChanged += new EventHandler( Database_TagsChanged );
					m_Database.InstitutionsChanged += new EventHandler( Database_InstitutionsChanged );
					m_Database.UnAssignedAttachmentsChanged += new EventHandler( Database_UnAssignedAttachmentsChanged );
				}

				// Update GUI

				// === Documents Tab ===
				Database_DocumentsChanged( m_Database, EventArgs.Empty );
				Database_UnAssignedAttachmentsChanged( m_Database, EventArgs.Empty );

					// Query
				buttonAddTag.Enabled = m_Database != null;
				buttonAddInstitution.Enabled = m_Database != null;
				textBoxQuery.Enabled = m_Database != null;
				buttonQuery.Enabled = m_Database != null;
				groupBoxQueryResults.Enabled = m_Database != null;

				// === Tags Tab ===
				listBoxTags.Enabled = m_Database != null;
				buttonCreateTag.Enabled = m_Database != null;
				Database_TagsChanged( m_Database, EventArgs.Empty );

				// === Institutions Tab ===
				listBoxInstitutions.Enabled = m_Database != null;
				buttonCreateInstitution.Enabled = m_Database != null;
				Database_InstitutionsChanged( m_Database, EventArgs.Empty );
			}
		}

		#endregion

		#region METHODS

		public PognacForm()
		{
			ms_Registry.Load();

			InitializeComponent();

			dateTimePickerQueryTo.Value = DateTime.Now;

			Application.Idle += new EventHandler( Application_Idle );
		}

		protected override void OnLoad( EventArgs e )
		{
			// Hide
			Visible = true;

			base.OnLoad( e );

			// Restore form's location
			RestoreForm( this );

			// Configure first time use
			if ( ms_Registry.FirstTimeUse )
			{
				MessageBox(
					"Welcome to Pognac !\r\n" +
					"\r\n" +
					"Since it's the first time you are using this application,\r\n" +
					"you will be redirected to the Preferences panel where you\r\n" +
					"can set the directory where you want to store your documents.\r\n" +
					"\r\n" +
					"Press the OK button to proceed.\r\n",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
					);

				preferencesToolStripMenuItem_Click( this, EventArgs.Empty );
			}
			else
			{	// Restore registry settings
				if ( Directory.Exists( ms_Registry.DocumentsDirectory ) )
					WorkingDirectory = new DirectoryInfo( ms_Registry.DocumentsDirectory );	// This should open the database
			}
		}

		protected bool	m_bExit = false;
		protected override void OnClosing( CancelEventArgs e )
		{
			if ( !m_bExit )
			{	// Replace close by hide...
				e.Cancel = true;
				this.Visible = false;

				// Also a good time to save the database
				if ( m_Database != null )
					m_Database.Save();
			}

			base.OnClosing( e );
		}

		protected override void OnClosed( EventArgs e )
		{
			base.OnClosed( e );

			// Save form's location
			SaveForm( this );

			// Save registry
			ms_Registry.Save();

			// This should save & close the database
			WorkingDirectory = null;
		}

		protected override void OnVisibleChanged( EventArgs e )
		{
			ShowInTaskbar = this.Visible;
			base.OnVisibleChanged( e );
		}

		#region Database Handling

		protected void	OpenDatabase()
		{
			// Check directory is valid
			if ( m_WorkingDirectory == null || !m_WorkingDirectory.Exists )
			{
				MessageBox( "Current working directory is invalid (not set or does not point to an existing directory).\r\nPlease select a working directory from the Perferences panel.", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				return;
			}

			// Open the database
			try
			{
				Documents.Database	DB = new Documents.Database( m_WorkingDirectory );
				DB.Load();
				Database = DB;
			}
			catch ( ErrorSheetException _e )
			{
				_e.m_ErrorSheet.DisplayErrors( "Some errors occurred while opening the database" );
				return;
			}
			catch ( Exception _e )
			{
				ShowError( "An unhandled error occurred while opening the database", _e );
				return;
			}
		}

		protected void	CloseDatabase()
		{
			// Save database
			if ( Database != null )
				Database.Save();

			Database = null;
		}

		#endregion

		#region Message Box

		public static DialogResult	MessageBox( string _Message )
		{
			return MessageBox( _Message, MessageBoxButtons.OK );
		}
		public static DialogResult	MessageBox( string _Message, MessageBoxButtons _Buttons )
		{
			return MessageBox( _Message, _Buttons, MessageBoxIcon.Information );
		}
		public static DialogResult	MessageBox( string _Message, MessageBoxButtons _Buttons, MessageBoxIcon _Icon )
		{
			return MessageBox( _Message, _Buttons, _Icon, MessageBoxDefaultButton.Button1 );
		}
		public static DialogResult	MessageBox( string _Message, MessageBoxButtons _Buttons, MessageBoxIcon _Icon, MessageBoxDefaultButton _DefaultButton )
		{
			return System.Windows.Forms.MessageBox.Show( _Message, "Pognac", _Buttons, _Icon, _DefaultButton );
		}
		public static DialogResult	MessageBox( Form _Owner, string _Message )
		{
			return MessageBox( _Owner, _Message, MessageBoxButtons.OK );
		}
		public static DialogResult	MessageBox( Form _Owner, string _Message, MessageBoxButtons _Buttons )
		{
			return MessageBox( _Owner, _Message, _Buttons, MessageBoxIcon.Information );
		}
		public static DialogResult	MessageBox( Form _Owner, string _Message, MessageBoxButtons _Buttons, MessageBoxIcon _Icon )
		{
			return MessageBox( _Owner, _Message, _Buttons, _Icon, MessageBoxDefaultButton.Button1 );
		}
		public static DialogResult	MessageBox( Form _Owner, string _Message, MessageBoxButtons _Buttons, MessageBoxIcon _Icon, MessageBoxDefaultButton _DefaultButton )
		{
			return System.Windows.Forms.MessageBox.Show( _Owner, _Message, "Pognac", _Buttons, _Icon, _DefaultButton );
		}

		#endregion

		#region Visual Error Management

		public static void	ShowError( Exception _e )
		{
			ShowError( null, _e );
		}

		public static void	ShowError( string _Topic, Exception _e )
		{
			string	ExceptionText = (_Topic != null ? _Topic : "") + _e.GetType().FullName + " => ";

			Exception	Current = _e;
			while ( Current != null )
			{
				ExceptionText += Current.Message + "\r\n";
				Current = Current.InnerException;
			}
			ExceptionText += _e.StackTrace;

			MessageBox( "An unhandled exception occurred while launching the program :\r\n\r\n" + ExceptionText, MessageBoxButtons.OK, MessageBoxIcon.Error );
		}

		#endregion

		#region Form Registry Update

		/// <summary>
		/// Saves the form's data into the registry
		/// </summary>
		/// <param name="_Form"></param>
		public static void	SaveForm( Form _Form )
		{
			string	Prefix = _Form.GetType().Name;

			SaveParam( Prefix, "X", _Form.Location.X.ToString() );
			SaveParam( Prefix, "Y", _Form.Location.Y.ToString() );
			SaveParam( Prefix, "Width", _Form.Width.ToString() );
			SaveParam( Prefix, "Height", _Form.Height.ToString() );
			SaveParam( Prefix, "State", _Form.WindowState.ToString() );
		}

		public static void	RestoreForm( Form _Form )
		{
			string	Prefix = _Form.GetType().Name;

			int		Temp;
			Point	Location = _Form.Location;
			if ( int.TryParse( LoadParam( Prefix, "X" ), out Temp ) ) Location.X = Temp;
			if ( int.TryParse( LoadParam( Prefix, "Y" ), out Temp ) ) Location.Y = Temp;

			Size	Size = _Form.Size;
			if ( int.TryParse( LoadParam( Prefix, "Width" ), out Temp ) ) Size.Width = Temp;
			if ( int.TryParse( LoadParam( Prefix, "Height" ), out Temp ) ) Size.Height = Temp;

			FormWindowState	State = FormWindowState.Normal;
			Enum.TryParse<FormWindowState>( LoadParam( Prefix, "State" ), out State );

			// Assign
			_Form.Location = Location;
			_Form.Size = Size;
			_Form.WindowState = State;
		}

		protected static void	SaveParam( string _Prefix, string _Suffix, string _Value )
		{
			ms_Registry.SetDynamicField( _Prefix+_Suffix, _Value );
		}

		protected static string	LoadParam( string _Prefix, string _Suffix )
		{
			return ms_Registry.GetDynamicField( _Prefix+_Suffix );
		}

		#endregion

		/// <summary>
		/// Spawns a new modeless document form singleton or shows the form if already open
		/// </summary>
		/// <param name="_Document"></param>
		protected void	SpawnDocumentForm( Documents.Document _Document )
		{
			if ( _Document == null )
				return;

			if ( m_Document2Form.ContainsKey( _Document ) )
			{	// Focus and leave...
				m_Document2Form[_Document].Focus();
				return;
			}

			// Create a new document form...
			DocumentForm	F = new DocumentForm();
			m_Document2Form.Add( _Document, F );
			m_Form2Document.Add( F, _Document );

			F.FormClosed += new FormClosedEventHandler( DocumentForm_FormClosed );

			PognacForm.RestoreForm( F );

			F.Document = _Document;
			F.Show( this );
		}

		void DocumentForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			DocumentForm	F = sender as DocumentForm;
			PognacForm.SaveForm( F );

			m_Document2Form.Remove( m_Form2Document[F] );
			m_Form2Document.Remove( F );
		}

		#endregion

		#region EVENT HANDLERS

		protected void Database_DocumentsChanged( object sender, EventArgs e )
		{
			labelRegisteredDocumentsCount.Text = m_Database != null ? m_Database.DocumentsCount.ToString() : "0";
		}

		protected void Database_TagsChanged( object sender, EventArgs e )
		{
			listBoxTags.BeginUpdate();
			listBoxTags.Items.Clear();

			// Unsubscribe from previous tags
			foreach ( Documents.Tag Tag in listBoxTags.Items )
				Tag.NameChanged -= new EventHandler( Tag_NameChanged );

			Documents.Tag	PreviousSelection = listBoxTags.SelectedItem as Documents.Tag;

			// Create new tags
			Documents.Tag[]	Tags = m_Database != null ? m_Database.Tags : new Documents.Tag[0];
			foreach ( Documents.Tag Tag in Tags )
			{
				Tag.NameChanged += new EventHandler( Tag_NameChanged );
				listBoxTags.Items.Add( Tag );
			}

			listBoxTags.SelectedItem = PreviousSelection;			// Restore selection

			listBoxTags.EndUpdate();
		}

		void Tag_NameChanged( object sender, EventArgs e )
		{
			listBoxTags.Items.Remove( sender );
			listBoxTags.Items.Add( sender );
		}

		protected void Database_InstitutionsChanged( object sender, EventArgs e )
		{
			listBoxInstitutions.BeginUpdate();
			listBoxInstitutions.Items.Clear();

			// Unsubscribe from previous institutions
			foreach ( Documents.Institution Institution in listBoxInstitutions.Items )
				Institution.NameChanged -= new EventHandler( Institution_NameChanged );

			Documents.Institution	PreviousSelection = listBoxInstitutions.SelectedItem as Documents.Institution;

			// Create new institutions
			Documents.Institution[]	Institutions = m_Database != null ? m_Database.Institutions : new Documents.Institution[0];
			foreach ( Documents.Institution Institution in Institutions )
			{
				Institution.NameChanged += new EventHandler( Institution_NameChanged );
				listBoxInstitutions.Items.Add( Institution );
			}

			listBoxInstitutions.SelectedItem = PreviousSelection;			// Restore selection

			listBoxInstitutions.EndUpdate();
		}

		void Institution_NameChanged( object sender, EventArgs e )
		{
			listBoxInstitutions.Items.Remove( sender );
			listBoxInstitutions.Items.Add( sender );
		}

		protected void Database_UnAssignedAttachmentsChanged( object sender, EventArgs e )
		{
			labelDocumentsToProcess.Text = m_Database != null ? m_Database.UnAssignedAttachmentsCount.ToString() : "0";
			buttonProcessDocuments.Visible = processWaitingFilesToolStripMenuItem.Enabled = m_Database != null && m_Database.UnAssignedAttachmentsCount > 0;
			notifyIcon.Text = this.Text + (m_Database != null ? " (" + m_Database.UnAssignedAttachmentsCount + " files waiting)" : "");

			if ( m_Database == null || m_Database.UnAssignedAttachmentsCount == 0 )
				return;

			// Show tray notification
			notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
			notifyIcon.BalloonTipTitle = "New Event !";
			notifyIcon.BalloonTipText = m_Database.UnAssignedAttachmentsCount + " new files are waiting to be processed...";
			notifyIcon.ShowBalloonTip( 5000 );
		}

		private void preferencesToolStripMenuItem_Click( object sender, EventArgs e )
		{
			PreferencesForm	F = new PreferencesForm();
			try
			{
				RestoreForm( F );

				F.WorkingDirectory = WorkingDirectory;

				if ( F.ShowDialog() != DialogResult.OK )
					return;

				WorkingDirectory = F.WorkingDirectory;
			}
			catch ( Exception _e )
			{
				MessageBox( "An error occurred while opening the preferences form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				SaveForm( F );
			}
		}

		#region Tab Control

		#region Documents Tab

		private void buttonProcessDocuments_Click( object sender, EventArgs e )
		{
			UnAssignedAttachmentsForm	F = new UnAssignedAttachmentsForm();
			try
			{
				RestoreForm( F );

				F.Database = Database;
				F.ShowDialog( this );
			}
			catch ( Exception _e )
			{
				MessageBox( "An error occurred while opening the ProcessDocuments form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				SaveForm( F );
			}
		}

		private void buttonAddTag_Click( object sender, EventArgs e )
		{
			AddTagForm.ShowDropDown( this, buttonAddTag, Database,
				( object form, EventArgs e2 ) =>
					{
						textBoxQuery.Text += (textBoxQuery.Text != ""  ? ", " : "") + (form as AddTagForm).SelectedTag.Name;
					}
				);
		}

		private void buttonAddInstitution_Click( object sender, EventArgs e )
		{
			AddInstitutionForm.ShowDropDown( this, buttonAddInstitution, Database,
				( object form, EventArgs e2 ) =>
					{
						textBoxQuery.Text += (textBoxQuery.Text != ""  ? ", " : "") + (form as AddInstitutionForm).SelectedInstitution.Name;
					}
				);
		}
		
		private void textBoxQuery_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.KeyCode == Keys.Return )
				buttonQuery_Click( buttonQuery, EventArgs.Empty );
		}

		private void buttonQuery_Click( object sender, EventArgs e )
		{
			try
			{
				// Retrieve the tags and institutions from the query line
				List<Documents.Tag>			QueryTags = new List<Documents.Tag>();
				List<Documents.Institution>	QueryInstitutions = new List<Documents.Institution>();
				ErrorSheet					Errors = new ErrorSheet();
				string						ActualQueryText = "";
				Documents.Database.ParseTagsAndInstitutions( Database, textBoxQuery.Text, QueryTags, QueryInstitutions, Errors, out ActualQueryText );

				// Perform query
				Documents.Document[]	QueryResult = m_Database.Query( QueryTags.ToArray(), QueryInstitutions.ToArray(), dateTimePickerQueryFrom.Value, dateTimePickerQueryTo.Value );
				documentsListQuery.Documents = QueryResult;

				groupBoxQueryResults.Text = "Results (" + QueryResult.Length + " matches)";

				// Report errors
				if ( Errors.HasErrors )
				{
					string	ErrorText = "Some query terms were not included in the search :\r\n\r\n";
					foreach ( ErrorSheet.ErrorString Error in Errors.Errors )
						ErrorText += " . " + Error.Error + "\r\n";
					ErrorText += "\r\nActual query was :\r\n";
					ErrorText += "\t" + (ActualQueryText != "" ? ActualQueryText : "<EMPTY>");

					MessageBox( ErrorText, MessageBoxButtons.OK, MessageBoxIcon.Warning );
				}
			}
			catch ( Exception _e )
			{
				MessageBox( "A terrible error occurred during query : " + _e.Message + "\r\n" + _e.StackTrace, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		private void documentsListQuery_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			SpawnDocumentForm( documentsListQuery.Selection );
		}

		#endregion

		#region Tags Tab

		private void buttonCreateTag_Click( object sender, EventArgs e )
		{
			TagForm	F = new TagForm();
			try
			{
				RestoreForm( F );

				Documents.Tag	NewTag = m_Database.CreateTag( "" );
				F.Tag = NewTag;

				if ( F.ShowDialog( this ) == DialogResult.OK )
					return;
			
				// Cancel...
				m_Database.RemoveTag( NewTag );
			}
			catch ( Exception _e )
			{
				MessageBox( "An error occurred while opening the CreateTag form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				SaveForm( F );
			}
		}

		private void listBoxTags_DoubleClick( object sender, EventArgs e )
		{
			Documents.Tag	SelectedTag = listBoxTags.SelectedItem as Documents.Tag;
			if ( SelectedTag == null )
				return;

			TagForm	F = new TagForm();
			try
			{
				RestoreForm( F );

				F.Tag = SelectedTag;
				F.ShowDialog( this );
			}
			catch ( Exception _e )
			{
				MessageBox( "An error occurred while opening the EditTag form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				SaveForm( F );
			}
		}

		private void listBoxTags_SelectedIndexChanged( object sender, EventArgs e )
		{
			Documents.Tag	SelectedTag = listBoxTags.SelectedItem as Documents.Tag;
			if ( SelectedTag != null )
			{
				Documents.Document[]	QueryResult = m_Database.Query( new Documents.Tag[] { SelectedTag }, null );
				documentsListTags.Documents = QueryResult;

				groupBoxDocumentsWithTag.Text = "Documents using Selected Tag (" + QueryResult.Length + " matches)";
			}
			else
				documentsListTags.Documents = null;
		}

		private void documentsListTags_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			SpawnDocumentForm( documentsListTags.Selection );
		}

		#endregion

		#region Institutions Tab

		private void buttonCreateInstitution_Click( object sender, EventArgs e )
		{
			InstitutionForm	F = new InstitutionForm();
			try
			{
				RestoreForm( F );

				Documents.Institution	NewInstitution = m_Database.CreateInstitution( "" );
				F.Institution = NewInstitution;

				if ( F.ShowDialog( this ) == DialogResult.OK )
					return;
			
				// Cancel...
				m_Database.RemoveInstitution( NewInstitution );
			}
			catch ( Exception _e )
			{
				MessageBox( "An error occurred while opening the CreateInstitution form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				SaveForm( F );
			}
		}

		private void listBoxInstitutions_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			Documents.Institution	SelectedInstitution = listBoxInstitutions.SelectedItem as Documents.Institution;
			if ( SelectedInstitution == null )
				return;

			InstitutionForm	F = new InstitutionForm();
			try
			{
				RestoreForm( F );

				F.Institution = SelectedInstitution;
				F.ShowDialog( this );
			}
			catch ( Exception _e )
			{
				MessageBox( "An error occurred while opening the EditInstitution form : " + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
			finally
			{
				SaveForm( F );
			}
		}

		private void listBoxInstitutions_SelectedIndexChanged( object sender, EventArgs e )
		{
			Documents.Institution	SelectedInstitution = listBoxInstitutions.SelectedItem as Documents.Institution;
			if ( SelectedInstitution != null )
			{
				Documents.Document[]	QueryResult = m_Database.Query( null, new Documents.Institution[] { SelectedInstitution } );
				documentsListInstitutions.Documents = QueryResult;

				groupBoxDocumentsFromInstitution.Text = "Documents using Selected Institution (" + QueryResult.Length + " matches)";
			}
			else
				documentsListInstitutions.Documents = null;
		}

		private void documentsListInstitutions_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			SpawnDocumentForm( documentsListInstitutions.Selection );
		}

		#endregion

		#endregion

		#region System Tray Icon

		private void openPognacToolStripMenuItem_Click( object sender, EventArgs e )
		{
			this.Visible = true;
			this.Focus();
		}

		private void processWaitingFilesToolStripMenuItem_Click( object sender, EventArgs e )
		{
			buttonProcessDocuments_Click( sender, e );
		}

		private void exitToolStripMenuItem_Click( object sender, EventArgs e )
		{
			m_bExit = true;
			Close();
		}

		private void notifyIcon_BalloonTipClicked( object sender, EventArgs e )
		{
			buttonProcessDocuments_Click( sender, e );
		}

		private void notifyIcon_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			this.Visible = !this.Visible;
			if ( this.Visible )
				this.Focus();
		}

		#endregion

		protected void Application_Idle( object sender, EventArgs e )
		{
			if ( m_Database == null || !m_Database.HasPendingAttachments )
				return;

			m_Database.CheckForNewAttachments();
		}

		#endregion
	}
}
