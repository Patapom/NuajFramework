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

using SequencorLib;

namespace SequencorEditor
{
	public partial class SequencerForm : Form
	{
		#region CONSTANTS

		public const string		APPLICATION_REGISTRY_URL = @"Software\Patapom\Sequencor";

		public const int		ARROWS_MILLISECONDS_LEAP = 1000;	// The time (in ms) the arrow keys move in the music during play

		#endregion

		#region FIELDS

		protected Sequencor				m_Sequencer = null;
		protected FileInfo				m_OpenedFile = null;
		protected FileInfo				m_OpenedMusicFile = null;

		protected bool					m_bMusicPlayerAvailable = false;
		protected SoundLib.MP3Player	m_MusicPlayer = null;
		protected FileStream			m_MusicStream = null;

		protected bool					m_bIsEmbedded = false;	// False is standalone

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the sequencer to edit
		/// </summary>
		public Sequencor		Sequencer
		{
			get { return m_Sequencer; }
			set
			{
				if ( value == m_Sequencer )
					return;	// No change...

				if ( m_Sequencer != null )
				{	// Unsubscribe
					sequencerControl.Sequencer = null;

					// Update GUI
					toolStripButtonCreateNewParameter.Enabled = false;
				}

				m_Sequencer = value;

				if ( m_Sequencer != null )
				{	// Subscribe
					sequencerControl.Sequencer = m_Sequencer;

					// Update GUI
					toolStripButtonCreateNewParameter.Enabled = true;
				}
			}
		}

		/// <summary>
		/// Gets the sequencer control to interface with
		/// </summary>
		public SequencerControl	SequencerControl
		{
			get { return sequencerControl; }
		}

		/// <summary>
		/// Tells if the form is used as embedded in another application
		/// </summary>
		public bool			IsEmbedded
		{
			get { return m_bIsEmbedded; }
			set { m_bIsEmbedded = value; }
		}

		#endregion

		#region METHODS

		public SequencerForm()
		{
			InitializeComponent();

			string DefaultPath = Path.GetDirectoryName( Application.ExecutablePath );
			openFileDialogProject.InitialDirectory = DefaultPath;
			openFileDialogMusic.InitialDirectory = DefaultPath;
			saveFileDialogProject.InitialDirectory = DefaultPath;
			saveFileDialogBinary.InitialDirectory = DefaultPath;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();

				// Dispose of player & any open stream
				if ( m_MusicPlayer != null )
				{
					m_MusicPlayer.Stop();
					m_MusicPlayer.Dispose();
				}
				if ( m_MusicStream != null )
					m_MusicStream.Dispose();
			}
			base.Dispose( disposing );
		}

		protected override void OnLoad( EventArgs e )
		{
			// Try and open a music player
			try
			{
				SoundLib.MP3Player	Test = new SoundLib.MP3Player();
				Test.Dispose();
				m_bMusicPlayerAvailable = true;
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( this, "The music player failed to open due to the following error :\r\n" + _e.Message + "\r\n\r\nMusic won't be available for sequencing...", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			}

			base.OnLoad( e );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			if ( m_bIsEmbedded )
			{
				Visible = false;	// Simply hide...
				e.Cancel = true;
				return;
			}

			if ( m_Sequencer != null && !AskForSave( "exiting the application" ) )
			{
				e.Cancel = true;
				return;
			}
			base.OnClosing( e );
		}

		/// <summary>
		/// Saves the current project
		/// </summary>
		/// <returns></returns>
		protected bool	SaveProject()
		{
			if ( m_OpenedFile == null && !UserSelectProjectFileToSaveAs() )
				return false;	// Failed to select a valid file...

			XmlDocument	Doc = new XmlDocument();
			try
			{
				XmlElement	Root = Doc.CreateElement( "ROOT" );
				Doc.AppendChild( Root );

				//////////////////////////////////////////////////////////////////////////
				// Save Project Infos
				XmlElement	ProjectElement = Doc.CreateElement( "Project" );
				Root.AppendChild( ProjectElement );
				{
					// Store music data
					XmlElement	MusicFileElement = Doc.CreateElement( "Music" );
					ProjectElement.AppendChild( MusicFileElement );

					if ( m_OpenedMusicFile != null )
						MusicFileElement.SetAttribute( "FileName", m_OpenedMusicFile.FullName );
				}

				//////////////////////////////////////////////////////////////////////////
				// Save UI infos
				XmlElement	UIElement = Doc.CreateElement( "UI" );
				Root.AppendChild( UIElement );
				{
					// Save time line bounds & cursor
					XmlElement	TimeLineElement = Doc.CreateElement( "TimeLine" );
					UIElement.AppendChild( TimeLineElement );
					TimeLineElement.SetAttribute( "VisibleBoundMin", sequencerControl.TimeLineControl.VisibleBoundMin.ToString() );
					TimeLineElement.SetAttribute( "VisibleBoundMax", sequencerControl.TimeLineControl.VisibleBoundMax.ToString() );
					TimeLineElement.SetAttribute( "CursorPosition", sequencerControl.TimeLineControl.CursorPosition.ToString() );
					
					// Save per-track data
					XmlElement	TrackDataElement = Doc.CreateElement( "TrackData" );
					UIElement.AppendChild( TrackDataElement );

					foreach ( Sequencor.ParameterTrack Track in m_Sequencer.Tracks )
					{
						FoldableTrackControl	FTC = sequencerControl.FindTrackControl( Track );
						if ( FTC == null )
							continue;	// Strange !

						XmlElement	TrackElement = Doc.CreateElement( "Track" );
									TrackElement.SetAttribute( "Name", Track.Name );
									TrackElement.SetAttribute( "AnimationTrackVisible", FTC.TrackControl.AnimationTrackVisible.ToString() );
						TrackDataElement.AppendChild( TrackElement );

						// Save animation control's minimum size
						TrackElement.SetAttribute( "AnimEditorMinSize", FTC.AnimationEditorControl.MinimumSize.Height.ToString() );

						// Save animation vertical range
						XmlElement	AnimationRangeElement = Doc.CreateElement( "AnimationRange" );
						TrackElement.AppendChild( AnimationRangeElement );
						AnimationRangeElement.SetAttribute( "VerticalRangeMin", FTC.AnimationEditorControl.GetAnimationVerticalRangeMin().ToString() );
						AnimationRangeElement.SetAttribute( "VerticalRangeMax", FTC.AnimationEditorControl.GetAnimationVerticalRangeMax().ToString() );

						// Save show tangents state
						TrackElement.SetAttribute( "ShowTangents", FTC.AnimationEditorControl.ShowTangents.ToString() );

						// Save gradient track visible state
						TrackElement.SetAttribute( "GradientTrackVisible", FTC.AnimationEditorControl.ShowGradientTrack.ToString() );

						// Save track color
						TrackElement.SetAttribute( "TrackColorR", FTC.TrackControl.IntervalsColor.R.ToString() );
						TrackElement.SetAttribute( "TrackColorG", FTC.TrackControl.IntervalsColor.G.ToString() );
						TrackElement.SetAttribute( "TrackColorB", FTC.TrackControl.IntervalsColor.B.ToString() );
					}

					XmlElement	FileDialogElement = Doc.CreateElement( "FileDialogs" );
					UIElement.AppendChild( FileDialogElement );

					FileDialogElement.SetAttribute( "OpenProjectLastPath", openFileDialogProject.InitialDirectory );
					FileDialogElement.SetAttribute( "OpenMusicLastPath", openFileDialogMusic.InitialDirectory );
					FileDialogElement.SetAttribute( "SaveProjectLastPath", saveFileDialogProject.InitialDirectory );
					FileDialogElement.SetAttribute( "SaveBinaryLastPath", saveFileDialogBinary.InitialDirectory );
				}

				//////////////////////////////////////////////////////////////////////////
				// Save sequence infos
				XmlElement	SequenceElement = Doc.CreateElement( "Sequence" );
				Root.AppendChild( SequenceElement );

				// Save the sequencer data to a binary stream
				MemoryStream	Stream = new MemoryStream();
				BinaryWriter	Writer = new BinaryWriter( Stream );
				m_Sequencer.Save( Writer );
				Writer.Close();
				Writer.Dispose();
				Stream.Close();

				// Encode in base64
				Base64Encoder	Encoder = new Base64Encoder( Stream.ToArray() );
				string			Base64Encoded = new string( Encoder.GetEncoded() );

				Stream.Dispose();

				// Save as CData
				XmlCDataSection	CData = Doc.CreateCDataSection( Base64Encoded );
				SequenceElement.AppendChild( CData );

				Doc.Save( m_OpenedFile.FullName );

				SequencerControl.MessageBox( this, "Sequencer project \"" + m_OpenedFile.FullName + "\" was successfully saved.", MessageBoxButtons.OK, MessageBoxIcon.Information );
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( this, "An error occurred while saving sequencer project :\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
				return false;
			}

			return true;
		}

		protected bool	UserLoadProject()
		{
			if ( openFileDialogProject.ShowDialog( this ) != DialogResult.OK )
				return false;

			return LoadProject( new FileInfo( openFileDialogProject.FileName ), null );
		}

		/// <summary>
		/// Loads a project file
		/// </summary>
		/// <param name="_ProjectFile"></param>
		/// <param name="_TagNeeded">An optional delegate that will be queried when the project is loaded</param>
		/// <returns></returns>
		public bool		LoadProject( FileInfo _ProjectFile, Sequencor.TagNeededEventHander _TagNeeded )
		{
			if ( _ProjectFile == null )
				throw new Exception( "Invalid project file name !" );
			if ( !_ProjectFile.Exists )
				throw new Exception( "Project file \"" + _ProjectFile + "\" does not exist !" );

			Sequencor	NewSequencer = new Sequencor();
			if ( _TagNeeded != null )
				NewSequencer.TagNeeded += _TagNeeded;

			XmlDocument	Doc = new XmlDocument();
			XmlElement	TrackDataElement = null;	// The XML element where per-track data are stored. We can only restore them once the sequencer is fully loaded...

			try
			{
				Doc.Load( _ProjectFile.FullName );

				XmlElement	Root = Doc["ROOT"];
				if ( Root == null )
					throw new Exception( "Failed to retrieve the root element !" );

				//////////////////////////////////////////////////////////////////////////
				// Load Project Infos
				XmlElement	ProjectElement = Root["Project"];
				if ( ProjectElement != null )
				{
					XmlElement	MusicFileElement = ProjectElement["Music"];
					if ( MusicFileElement != null && MusicFileElement.GetAttribute( "FileName" ) != "" )
						LoadMusicFile( new FileInfo( MusicFileElement.GetAttribute( "FileName" ) ) );
				}

				//////////////////////////////////////////////////////////////////////////
				// Load UI Infos
				XmlElement	UIElement = Root["UI"];
				if ( UIElement != null )
				{
					// Restore time line bounds & cursor
					XmlElement	TimeLineElement = UIElement["TimeLine"];
					if ( TimeLineElement != null )
					{
						float	BoundMin, BoundMax, CursorPosition;
						if ( float.TryParse( TimeLineElement.GetAttribute( "VisibleBoundMin" ), out BoundMin ) &&
 							 float.TryParse( TimeLineElement.GetAttribute( "VisibleBoundMax" ), out BoundMax ) &&
 							 float.TryParse( TimeLineElement.GetAttribute( "CursorPosition" ), out CursorPosition ) )
						{
							sequencerControl.TimeLineControl.SetVisibleRange( BoundMin, BoundMax );
							sequencerControl.TimeLineControl.MoveCursorTo( CursorPosition );
						}
					}

					// Retrieve tracks element for restore later
					TrackDataElement = UIElement["TrackData"];

					XmlElement	FileDialogElement = UIElement["FileDialogs"];
					if ( FileDialogElement != null )
					{
						openFileDialogProject.InitialDirectory = FileDialogElement.GetAttribute( "OpenProjectLastPath" );
						openFileDialogMusic.InitialDirectory = FileDialogElement.GetAttribute( "OpenMusicLastPath" );
						saveFileDialogProject.InitialDirectory = FileDialogElement.GetAttribute( "SaveProjectLastPath" );
						saveFileDialogBinary.InitialDirectory = FileDialogElement.GetAttribute( "SaveBinaryLastPath" );
					}
				}

				//////////////////////////////////////////////////////////////////////////
				// Load Sequence Infos
				XmlElement	SequenceElement = Root["Sequence"];
				if ( SequenceElement == null )
					throw new Exception( "Failed to retrieve the sequence element !" );

				XmlCDataSection	CData = SequenceElement.FirstChild as XmlCDataSection;
				string			Base64Encoded = CData.Data;

				// Decode into binary
				Base64Decoder	Decoder = new Base64Decoder( Base64Encoded.ToCharArray() );
				byte[]			Base64Decoded = Decoder.GetDecoded();

				// Load the sequencer data to a binary stream
				NewSequencer.LoadFromBinaryMemory( Base64Decoded );
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( this, "An error occurred while saving sequencer project :\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
				return false;
			}

			// Assign new sequencer
			try
			{
				m_OpenedFile = _ProjectFile;
				Sequencer = NewSequencer;

				//////////////////////////////////////////////////////////////////////////
				// Restore per-track data
				if ( TrackDataElement != null )
				{
					XmlElement	TrackElement = TrackDataElement["Track"];
					while ( TrackElement != null )
					{
						XmlElement	CurrentTrackElement = TrackElement;
						string	TrackName = CurrentTrackElement.GetAttribute( "Name" );

						TrackElement = TrackElement.NextSibling as XmlElement;

						// Attempt to retrieve track & track control
						Sequencor.ParameterTrack	Track = m_Sequencer.FindParameterTrack( TrackName );
						if ( Track == null )
							continue;
						FoldableTrackControl	FTC = sequencerControl.FindTrackControl( Track );
						if ( FTC == null )
							continue;

						// Restore visible state
						bool	AnimationTrackVisible = false;
						if ( bool.TryParse( CurrentTrackElement.GetAttribute( "AnimationTrackVisible" ), out AnimationTrackVisible ) )
							FTC.TrackControl.AnimationTrackVisible = AnimationTrackVisible;
						
						// Restore animation control's minimum size
						int		AnimEditorMinSize = 1;
						if ( int.TryParse( CurrentTrackElement.GetAttribute( "AnimEditorMinSize" ), out AnimEditorMinSize ) )
							FTC.AnimationEditorControl.MinimumSize = new System.Drawing.Size( 0, AnimEditorMinSize );

						// Restore show tangents state
						bool	ShowTangents = false;
						if ( bool.TryParse( CurrentTrackElement.GetAttribute( "ShowTangents" ), out ShowTangents ) )
							FTC.AnimationEditorControl.ShowTangents = ShowTangents;
						
						// Restore gradient track visible state
						bool	GradientTrackVisible = false;
						if ( bool.TryParse( CurrentTrackElement.GetAttribute( "GradientTrackVisible" ), out GradientTrackVisible ) )
							FTC.AnimationEditorControl.ShowGradientTrack = GradientTrackVisible;

						// Restore track color
						int R, G, B;
						if ( int.TryParse( CurrentTrackElement.GetAttribute( "TrackColorR" ), out R ) &&
							 int.TryParse( CurrentTrackElement.GetAttribute( "TrackColorG" ), out G ) &&
							 int.TryParse( CurrentTrackElement.GetAttribute( "TrackColorB" ), out B ) )
							FTC.TrackControl.IntervalsColor = Color.FromArgb( R, G, B );

						// Restore animation vertical range
						XmlElement	AnimationRangeElement = CurrentTrackElement["AnimationRange"];
						if ( AnimationRangeElement != null )
						{
							float	RangeMin, RangeMax;
							if ( float.TryParse( AnimationRangeElement.GetAttribute( "VerticalRangeMin" ), out RangeMin ) &&
 								 float.TryParse( AnimationRangeElement.GetAttribute( "VerticalRangeMax" ), out RangeMax ) )
								FTC.AnimationEditorControl.SetVerticalRange( RangeMin, RangeMax );
						}
					}
				}
			}
			catch ( Exception _e )
			{
				m_OpenedFile = null;
				SequencerControl.MessageBox( this, "An error occurred while assigning new sequencer data :\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
				return false;
			}

			return true;
		}

		/// <summary>
		/// Saves the current project as a specific file
		/// </summary>
		/// <returns></returns>
		protected bool	UserSelectProjectFileToSaveAs()
		{
			if ( saveFileDialogProject.ShowDialog( this ) != DialogResult.OK )
				return false;

			m_OpenedFile = new FileInfo( saveFileDialogProject.FileName );

			return true;
		}

		protected bool	AskForSave( string _Action )
		{
			DialogResult Result = SequencerControl.MessageBox( this, "Do you want to save your project before " + _Action + " ?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question );
			if ( Result == DialogResult.Cancel )
				return false;
			if ( Result == DialogResult.No )
				return true;

			return SaveProject();
		}

		/// <summary>
		/// Loads a music file and set it as current music for the sequence
		/// </summary>
		/// <param name="_MusicFile"></param>
		/// <returns></returns>
		protected bool	LoadMusicFile( FileInfo _MusicFile )
		{
			try
			{
				FileStream	MusicStream = _MusicFile.OpenRead();
				SoundLib.MP3Player	MusicPlayer = new SoundLib.MP3Player();
				MusicPlayer.Load( MusicStream );

				if ( m_MusicPlayer != null )
				{
					m_MusicPlayer.Stop();
					m_MusicPlayer.Dispose();
				}
				if ( m_MusicStream != null )
					m_MusicStream.Dispose();

				m_OpenedMusicFile = _MusicFile;
				m_MusicPlayer = MusicPlayer;
				m_MusicStream = MusicStream;
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( this, "An error occurred while loading the music file \"" + openFileDialogMusic.FileName + ": \r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
				return false;
			}

			return true;
		}

		protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
		{
			if ( m_Sequencer != null )
			{
				if ( keyData == Keys .Space )
				{	// PLAY/PAUSE
					sequencerControl.TogglePlay();
					return true;
				}
				else if ( keyData == Keys.Back )
				{	// STOP
					sequencerControl.Stop();
					return true;
				}
				else if ( keyData == Keys.Left )
				{	// REWIND
					if ( m_MusicPlayer != null && m_MusicPlayer.Playing )
						m_MusicPlayer.Position -= ARROWS_MILLISECONDS_LEAP;
					else
						sequencerControl.SetSequenceTime( Sequencer.Time, Sequencer.Time - ARROWS_MILLISECONDS_LEAP / 1000.0f );
					return true;
				}
				else if ( keyData == Keys.Right )
				{	// FF
					if ( m_MusicPlayer != null && m_MusicPlayer.Playing )
						m_MusicPlayer.Position += ARROWS_MILLISECONDS_LEAP;
					else
						sequencerControl.SetSequenceTime( Sequencer.Time, Sequencer.Time +  ARROWS_MILLISECONDS_LEAP / 1000.0f );
					return true;
				}
				else if ( keyData == Keys.Return )
				{	// Drop realtime event key
					sequencerControl.DropRealTimeKey( Sequencer.Time );
					return true;
				}
			}

			return base.ProcessCmdKey( ref msg, keyData );
		}

		#endregion

		#region EVENT HANDLERS

		#region Main Menu

		#region File

		private void fileToolStripMenuItem_DropDownOpening( object sender, EventArgs e )
		{
			bool	bSequencerValid = m_Sequencer != null;

			newToolStripMenuItem.Enabled = true;
			openToolStripMenuItem.Enabled = true;
			loadMusicToolStripMenuItem.Enabled = bSequencerValid && m_bMusicPlayerAvailable;
			saveToolStripMenuItem.Enabled = bSequencerValid;
			saveAsToolStripMenuItem.Enabled = bSequencerValid;
			exportToolStripMenuItem.Enabled = bSequencerValid;
		}

		private void newToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( m_Sequencer != null && !AskForSave( "creating a new project" ) )
				return;

			try
			{
				m_OpenedFile = null;
				Sequencer = new Sequencor();
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( this, "An error occurred while creating a new sequencer project :\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		private void openToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( m_Sequencer != null && !AskForSave( "opening another project") )
				return;

			UserLoadProject();
		}

		private void loadMusicToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( openFileDialogMusic.ShowDialog( this ) != DialogResult.OK )
				return;

			LoadMusicFile( new FileInfo( openFileDialogMusic.FileName ) );
		}

		private void saveToolStripMenuItem_Click( object sender, EventArgs e )
		{
			SaveProject();
		}

		private void saveAsToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( UserSelectProjectFileToSaveAs() )
				SaveProject();
		}

		private void asBinaryToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( saveFileDialogBinary.ShowDialog( this ) != DialogResult.OK )
				return;

			// Save the sequencer data to a binary stream
			try
			{
				FileStream		Stream = new FileInfo( saveFileDialogBinary.FileName ).Create();
				BinaryWriter	Writer = new BinaryWriter( Stream );
				m_Sequencer.Save( Writer );
				Writer.Close();
				Writer.Dispose();
				Stream.Close();
				Stream.Dispose();
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( this, "An error occurred while exporting sequencer data :\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		private void exitToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Close();
		}

		#endregion

		#region Edit

		protected Point					m_ContextMenuPosition;
		protected FoldableTrackControl	m_EditMenuSourceFTC = null;
		private void editToolStripMenuItem_DropDownOpening( object sender, EventArgs e )
		{
			bool	bSequencerValid = m_Sequencer != null;
			m_ContextMenuPosition = Control.MousePosition;
			m_EditMenuSourceFTC = sequencerControl.FindTrackControl( sequencerControl.SelectedTrack );

			copySelectedTrackToolStripMenuItem.Enabled = bSequencerValid && sequencerControl.SelectedTrack != null;
			copySelectedIntervalToolStripMenuItem.Enabled = bSequencerValid && m_EditMenuSourceFTC != null && m_EditMenuSourceFTC.TrackControl.SelectedInterval != null;
			copySelectedKeyToolStripMenuItem.Enabled = bSequencerValid && m_EditMenuSourceFTC != null && m_EditMenuSourceFTC.AnimationEditorControl.SelectedKey != null;

			bool	bCanPasteParameter = sequencerControl.CanPasteParameter( m_Sequencer );
			bool	bCanPasteInterval = sequencerControl.CanPasteIntervalToParameter( sequencerControl.SelectedTrack );
			bool	bCanPasteKey = m_EditMenuSourceFTC != null && sequencerControl.CanPasteKeyToInterval( m_EditMenuSourceFTC.TrackControl.SelectedInterval );
			pasteToolStripMenuItem.Enabled = bCanPasteParameter || bCanPasteInterval || bCanPasteKey;
		}

		private void copySelectedTrackToolStripMenuItem_Click( object sender, EventArgs e )
		{
			sequencerControl.CopyToClipboard( sequencerControl.SelectedTrack );
		}

		private void copySelectedIntervalToolStripMenuItem_Click( object sender, EventArgs e )
		{
			sequencerControl.CopyToClipboard( m_EditMenuSourceFTC.TrackControl.SelectedInterval );
		}

		private void copySelectedKeyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			sequencerControl.CopyToClipboard( m_EditMenuSourceFTC.AnimationEditorControl.GetBuddyKeys( m_EditMenuSourceFTC.AnimationEditorControl.SelectedKey ) );
		}

		private void pasteToolStripMenuItem_Click( object sender, EventArgs e )
		{
			try
			{
				if ( sequencerControl.CanPasteParameter( m_Sequencer ) )
				{	// Paste a whole parameter track
					sequencerControl.PasteParameter( m_Sequencer );
				}
				else if ( sequencerControl.CanPasteIntervalToParameter( sequencerControl.SelectedTrack ) )
				{	// Paste an interval
					float	NewIntervalTime = m_EditMenuSourceFTC.ClientToSequenceTime( m_EditMenuSourceFTC.PointToClient( m_ContextMenuPosition ).X );
					sequencerControl.PasteIntervalToParameter( sequencerControl.SelectedTrack, NewIntervalTime );
				}
				else if ( m_EditMenuSourceFTC != null && sequencerControl.CanPasteKeyToInterval( m_EditMenuSourceFTC.TrackControl.SelectedInterval ) )
				{	// Paste keys
					float	NewKeysTime = m_EditMenuSourceFTC.ClientToSequenceTime( m_EditMenuSourceFTC.PointToClient( m_ContextMenuPosition ).X );
					sequencerControl.PasteKeysToInterval( m_EditMenuSourceFTC.TrackControl.SelectedInterval, NewKeysTime );
				}
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( "An error occurred while pasting from clipboard :\r\n\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		#endregion

		#region Sequence

		private void sequenceToolStripMenuItem_DropDownOpening( object sender, EventArgs e )
		{
			bool	bSequencerValid = m_Sequencer != null;
			bool	bSelectedParameterValid = bSequencerValid && sequencerControl.SelectedTrack != null;
			bool	bSelectedIntervalValid = bSelectedParameterValid && sequencerControl.SelectedInterval != null;

			createNewParameterToolStripMenuItem.Enabled = bSequencerValid;
			editParameterToolStripMenuItem.Enabled = bSelectedParameterValid;
			deleteParameterToolStripMenuItem.Enabled = bSelectedParameterValid;

			createNewIntervalToolStripMenuItem.Enabled = bSelectedParameterValid;
			deleteIntervalToolStripMenuItem.Enabled = bSelectedIntervalValid;

			// TODO
			addClipToolStripMenuItem.Enabled = false;
			removeClipToolStripMenuItem.Enabled = false;
		}

		private void createNewParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			sequencerControl.UserCreateNewParameterTrack();
		}

		private void editParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			sequencerControl.UserEditSelectedParameterTrackData();
		}

		private void deleteParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			sequencerControl.UserConfirmTrackDelete( sequencerControl.SelectedTrack );
		}

		#endregion

		#region Help
		#endregion

		#endregion

		#region Toolbar

		private void toolStripButtonCreateNewParameter_Click( object sender, EventArgs e )
		{
			createNewParameterToolStripMenuItem_Click( sender, e );
		}

		#endregion

		private void sequencerControl_SequencePlay( object sender, EventArgs e )
		{
			if ( m_MusicPlayer == null )
				return;

			// Make use provide the sequence time
			sequencerControl.SequenceTimeNeeded += new SequencerControl.ProvideSequenceTimeEventHandler( sequencerControl_SequenceTimeNeeded );
			m_MusicPlayer.Play();
		}

		private void sequencerControl_SequencePause( object sender, EventArgs e )
		{
			if ( m_MusicPlayer == null )
				return;

			// Stop providing the sequence time
			m_MusicPlayer.Pause();
			sequencerControl.SequenceTimeNeeded -= new SequencerControl.ProvideSequenceTimeEventHandler( sequencerControl_SequenceTimeNeeded );
		}

		private void sequencerControl_SequenceTimeChanged( object sender, EventArgs e )
		{
			if ( m_MusicPlayer == null )
				return;

			if ( m_MusicPlayer.Playing )
				return;	// If playing, the music sets the sequence position...
			else
				m_MusicPlayer.Position = (int) (sequencerControl.SequenceTime * 1000.0f);	// Otherwise, set the music position...
		}

		protected float sequencerControl_SequenceTimeNeeded( SequencerControl _Sender )
		{
			return m_MusicPlayer.Position * 1000.0f;
		}

		#endregion
	}
}
