using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SequencorLib;

namespace SequencorEditor
{
	/// <summary>
	/// This control displays the sequencer's parameter tracks
	/// </summary>
	public partial class SequencerControl : UserControl
	{
		#region CONSTANTS

		protected const float	DEFAULT_VISIBLE_RANGE	= 10.0f;	// Default is up to 10 seconds
		protected const float	STALL_TIMER_DELAY		= 1.0f;		// Stall timer every X seconds to let the other messages get processed sometime...

		#endregion

		#region NESTED TYPES

		public delegate object		ProvideParameterValueEventHandler( SequencerControl _Sender, Sequencor.ParameterTrack _Track );
		public delegate float		ProvideSequenceTimeEventHandler( SequencerControl _Sender );

		#endregion

		#region FIELDS

		protected Sequencor					m_Sequencer = null;
		protected Sequencor.ParameterTrack	m_SelectedTrack = null;
		protected Sequencor.ParameterTrack.Interval	m_SelectedInterval = null;

		protected List<FoldableTrackControl>	m_TrackControls = new List<FoldableTrackControl>();

		// Sequence playing
		protected float						m_SequenceTime = 0.0f;

		// Context menu
		protected Point						m_ContextMenuPosition = Point.Empty;

		// Appearance
		protected bool						m_bShowCursorTime = true;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the sequencor object to work with
		/// </summary>
		[Browsable( false )]
		public Sequencor					Sequencer
		{
			get { return m_Sequencer; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_Sequencer )
					return;

				if ( m_Sequencer != null )
				{
					m_Sequencer.TracksChanged -= new EventHandler( Sequencer_TracksChanged );
					ClearTracks();

					panelTracks.BackColor = SystemColors.Control;
					labelStartProject.Visible = true;

					SelectedTrack = null;
					Enabled = false;
				}

				m_Sequencer = value;

				if ( m_Sequencer != null )
				{
					panelTracks.BackColor = SystemColors.ControlDarkDark;
					labelStartProject.Visible = false;

					m_Sequencer.TracksChanged += new EventHandler( Sequencer_TracksChanged );
					Sequencer_TracksChanged( m_Sequencer, EventArgs.Empty );
					Enabled = true;

					buttonZoomOut_Click( null, EventArgs.Empty );
				}
			}
		}

		/// <summary>
		/// Gets or sets the currently selected track to work with
		/// </summary>
		[Browsable( false )]
		public Sequencor.ParameterTrack		SelectedTrack
		{
			get { return m_SelectedTrack; }
			set
			{
				if ( value == m_SelectedTrack )
					return;	// No change

				if ( m_SelectedTrack != null )
				{	// Clear previous selection
					FoldableTrackControl	TC = FindTrackControl( m_SelectedTrack );
					if ( TC != null )
						TC.Selected = false;
				}

				m_SelectedTrack = value;

				if ( m_SelectedTrack != null )
				{	// Set new selection
					FoldableTrackControl	TC = FindTrackControl( m_SelectedTrack );
					if ( TC != null )
						TC.Selected = true;
				}

				// Notify
				if ( SelectedTrackChanged != null )
					SelectedTrackChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets the currently selected animation interval in the currently selected track
		/// </summary>
		[Browsable( false )]
		protected bool	m_bInternalChange = false;
		public Sequencor.ParameterTrack.Interval	SelectedInterval
		{
			get { return m_SelectedInterval; }
			set
			{
				if ( value == m_SelectedInterval || m_bInternalChange )
					return;

				m_bInternalChange = true;

				if ( m_SelectedInterval != null )
				{
					FoldableTrackControl	TC = FindTrackControl( m_SelectedInterval.ParentTrack );
					if ( TC != null )
						TC.SelectedInterval = null;
				}

				m_SelectedInterval = value;

				if ( m_SelectedInterval != null )
				{
					FoldableTrackControl	TC = FindTrackControl( m_SelectedInterval.ParentTrack );
					if ( TC != null )
						TC.SelectedInterval = m_SelectedInterval;
				}

				if ( m_SelectedInterval != null )
					SelectedTrack = m_SelectedInterval.ParentTrack;
				else
					SelectedTrack = null;

				m_bInternalChange = false;
			}
		}

		/// <summary>
		/// Tells if the sequence is currently playing
		/// </summary>
		[Browsable( false )]
		public bool							IsPlaying	{ get { return checkBoxPlay.Checked; } }

		/// <summary>
		/// Occurs when the sequence starts playing
		/// </summary>
		[Category( "Sequencing" )]
		public event EventHandler			SequencePlay;

		/// <summary>
		/// Occurs when the sequence pauses playing
		/// </summary>
		[Category( "Sequencing" )]
		public event EventHandler			SequencePause;

		/// <summary>
		/// Gets the current sequence time
		/// </summary>
		[Category( "Sequencing" )]
		public float						SequenceTime	{ get { return m_SequenceTime; } }

		/// <summary>
		/// Occurs when the sequence time changed
		/// </summary>
		[Category( "Sequencing" )]
		public event EventHandler			SequenceTimeChanged;

		/// <summary>
		/// Occurs when the control needs the current time for the sequencer
		/// You should subscribe to that event if you wish to provide the sequence time externally
		///  otherwise, the sequence time is set by the internal timer of the control
		/// </summary>
		[Category( "Sequencing" )]
		public event ProvideSequenceTimeEventHandler	SequenceTimeNeeded;

		/// <summary>
		/// Occurs when the selected track changed
		/// </summary>
		[Category( "Selection" )]
		public event EventHandler			SelectedTrackChanged;

		/// <summary>
		/// Occurs when the control needs the current value of a parameter
		/// You should subscribe to that event to enable key creation by parameter sampling :
		///  this way, you can change the value of a parameter in your application then create a new key
		///  or update an existing key using that parameter's value
		/// </summary>
		[Category( "Sequencing" )]
		public event ProvideParameterValueEventHandler	ParameterValueNeeded;

		[Browsable( false )]
		internal bool						CanQueryParameterValue	{ get { return ParameterValueNeeded != null; } }

		[Browsable( false )]
		internal TimeLineControl			TimeLineControl	{ get { return timeLineControl; } }

		[Browsable( false )]
		internal bool						ShowCursorTime	{ get { return m_bShowCursorTime; } set { m_bShowCursorTime = value; } }

		#endregion

		#region METHODS

		public SequencerControl()
		{
			InitializeComponent();

			timeLineControl.VisibleBoundMax = DEFAULT_VISIBLE_RANGE;
			panelFastPaint.Owner = this;
			panelFastPaint.MimickedControl = panelTracks;

			Application.Idle += new EventHandler( Application_Idle );
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				Sequencer = null;

				if (components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Creates a new parameter track after spawning a user form to get parameter informations
		/// </summary>
		/// <returns>The newly created parameter track or null if the user canceled creation</returns>
		public Sequencor.ParameterTrack		UserCreateNewParameterTrack()
		{
			ParameterForm	F = new ParameterForm();
			if ( F.ShowDialog( this ) != DialogResult.OK )
				return null;

			Sequencor.ParameterTrack	NewTrack = Sequencer.CreateTrack( F.ParameterName, F.ParameterGUID, F.ParameterType, null );

			// Also create a default interval
			NewTrack.CreateInterval( TimeLineControl.CursorPosition, TimeLineControl.CursorPosition + 0.1f * (timeLineControl.VisibleBoundMax - timeLineControl.VisibleBoundMin) );

			return NewTrack;
		}

		/// <summary>
		/// Edits the currently selected parameter track
		/// </summary>
		public void		UserEditSelectedParameterTrackData()
		{
			UserEditParameterTrackData( SelectedTrack );
		}

		/// <summary>
		/// Edits the data of an existing parameter track
		/// </summary>
		/// <param name="_Track"></param>
		public void		UserEditParameterTrackData( Sequencor.ParameterTrack _Track )
		{
			if ( _Track == null )
				return;

			ParameterForm	F = new ParameterForm();
			F.ParameterName = _Track.Name;
			F.ParameterType = _Track.Type;
			F.ParameterGUID = _Track.GUID;
			if ( F.ShowDialog( this ) != DialogResult.OK )
				return;

			_Track.Name = F.ParameterName;
			_Track.GUID = F.ParameterGUID;
		}

		/// <summary>
		/// Prompts the user for track delete
		/// </summary>
		/// <param name="_Track"></param>
		/// <returns></returns>
		public bool	UserConfirmTrackDelete( Sequencor.ParameterTrack _Track )
		{
			if ( MessageBox( "Are you sure you want to delete parameter \"" + _Track.Name + "\" ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2 ) != DialogResult.Yes )
				return	false;

			m_Sequencer.RemoveTrack( _Track );

			return true;
		}

		/// <summary>
		/// Clears the list of existing tracks
		/// </summary>
		protected void		ClearTracks()
		{
 			SuspendLayout();
			panelTracks.Controls.Clear();
			ResumeLayout();

			while ( m_TrackControls.Count > 0 )
			{
				m_TrackControls[0].Dispose();
				m_TrackControls.RemoveAt( 0 );
			}
		}

		/// <summary>
		/// Rebuilds the list of tracks
		/// </summary>
		protected void		BuildTracks()
		{
			// Build the list of current tracks
			List<Sequencor.ParameterTrack>	CurrentTracksList = new List<Sequencor.ParameterTrack>();
			foreach ( FoldableTrackControl FTC in m_TrackControls )
				CurrentTracksList.Add( FTC.Track );
			Sequencor.ParameterTrack[]	CurrentTracks = CurrentTracksList.ToArray();

			Sequencor.ParameterTrack[]	TracksToCreate = CollectionsOperations<Sequencor.ParameterTrack>.Subtraction( m_Sequencer.Tracks, CurrentTracks );
			Sequencor.ParameterTrack[]	TracksToDelete = CollectionsOperations<Sequencor.ParameterTrack>.Subtraction( CurrentTracks, m_Sequencer.Tracks );

 			SuspendLayout();
			panelTracks.Controls.Clear();

			// Create new tracks
			Random	RNG = new Random();
			foreach ( Sequencor.ParameterTrack T in TracksToCreate )
			{
				FoldableTrackControl	FTC = new FoldableTrackControl();
				FTC.Owner = this;
				FTC.Dock = DockStyle.Top;
				FTC.AutoSize = true;
				FTC.Track = T;
				FTC.SetRange( timeLineControl.VisibleBoundMin, timeLineControl.VisibleBoundMax );
				FTC.SelectedChanged += new EventHandler( TrackControl_SelectedChanged );
				FTC.TrackRename += new EventHandler( TrackInfo_TrackRename );
				FTC.SelectedIntervalChanged += new EventHandler( TrackInfo_SelectedIntervalChanged );
				m_TrackControls.Add( FTC );

				// Pick a new random color
				int		R, G, B;
				while ( true )
				{
					R = RNG.Next( 5 );
					G = RNG.Next( 5 );
					B = RNG.Next( 5 );
					if ( R != G || G != B )
						break;	// Avoid gray
				}
				FTC.TrackControl.IntervalsColor = Color.FromArgb( 50 * (1+R), 50 * (1+G), 50 * (1+B) );
			}

			// Remove old tracks
			foreach ( Sequencor.ParameterTrack T in TracksToDelete )
			{
				FoldableTrackControl	FTC = FindTrackControl( T );
				m_TrackControls.Remove( FTC );
				FTC.Dispose();
			}

			// Insert back tracks in the container in the correct order
 			for ( int TrackIndex=m_Sequencer.Tracks.Length-1; TrackIndex >= 0; TrackIndex-- )
			{
 				FoldableTrackControl	FTC = FindTrackControl( m_Sequencer.Tracks[TrackIndex] );

				Splitter	Split = new Splitter();
				Split.Dock = DockStyle.Top;
				Split.Tag = FTC;
				Split.SplitterMoving += new SplitterEventHandler( Split_SplitterMoving );

				panelTracks.Controls.Add( Split );
				panelTracks.Controls.Add( FTC );
			}

 			ResumeLayout( true );
		}

		void Split_SplitterMoving( object sender, SplitterEventArgs e )
		{
			FoldableTrackControl	FTC = (sender as Splitter).Tag as FoldableTrackControl;
			if ( !FTC.AnimationEditorControl.Visible )
				return;

			int		FTCOffset = FTC.Top;
			int		TrackControlHeight = FTC.TrackControl.Height;
			int		NewFTCAnimationHeight = e.Y - FTCOffset - TrackControlHeight;
			NewFTCAnimationHeight = Math.Max( FTC.AnimationEditorControl.VerticalSizeLimit, NewFTCAnimationHeight );

			FTC.AnimationEditorControl.MinimumSize = new System.Drawing.Size( 0, NewFTCAnimationHeight );
		}

		/// <summary>
		/// Starts/Stops auto play
		/// </summary>
		public void			Play()
		{
			checkBoxPlay.Checked = true;
		}

		/// <summary>
		/// Starts/Stops auto play
		/// </summary>
		public void			Pause()
		{
			checkBoxPlay.Checked = false;
		}

		/// <summary>
		/// Stops auto play
		/// </summary>
		public void			Stop()
		{
			checkBoxPlay.Checked = false;

			// Reset sequence time
			SetSequenceTime( 0.0f, 0.0f );
		}

		/// <summary>
		/// Starts/Stops auto play
		/// </summary>
		public void			TogglePlay()
		{
			checkBoxPlay.Checked = !checkBoxPlay.Checked;
		}

		/// <summary>
		/// Resets auto play
		/// </summary>
		public void			Reset()
		{
			m_LastTimeStamp = DateTime.Now;
			SetSequenceTime( 0.0f, 0.0f );
		}

		/// <summary>
		/// Sets the current and last sequence time
		/// It's important that we correctly set the delta time ourselves as the editor is completely driving the simulation
		/// It must not be different from the engine's auto simulation
		/// </summary>
		/// <param name="_PreviousTime">The sequence time at the last frame</param>
		/// <param name="_NewTime">The new current time for the sequence</param>
		public void			SetSequenceTime( float _PreviousTime, float _NewTime )
		{
			if ( Math.Abs( _NewTime - m_SequenceTime ) < 1e-3f )
				return;

			m_SequenceTime = _NewTime;

			// Set the actual sequencor time
			if ( m_Sequencer != null )
			{
				if ( checkBoxPlay.Checked )
					m_Sequencer.AnimateSequence( _PreviousTime, _NewTime );	// Play mode => ANIMATE !
				else
					m_Sequencer.SetTime( _NewTime );						// Edit mode => ABSOLUTE SET !
			}

			// Update cursor position
			timeLineControl.MoveCursorTo( m_SequenceTime );

			// Notify
			if ( SequenceTimeChanged != null )
				SequenceTimeChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Drops a default EVENT key in the selected track if the provided time (usually cursor time) is within an interval
		/// This is used when in PLAY mode to drop keys on a single keystroke
		/// </summary>
		/// <param name="_Time"></param>
		public void			DropRealTimeKey( float _Time )
		{
			if ( SelectedTrack == null )
				return;
			if ( SelectedTrack.Type != Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT )
			{	// Not an event track !
				MessageBox( "You can't drop a key on a non EVENT track !\r\nSelect an EVENT track to drop realtime keys...", MessageBoxButtons.OK, MessageBoxIcon.Information );
				return;
			}

			FoldableTrackControl	FTC = FindTrackControl( SelectedTrack );
			foreach ( Sequencor.ParameterTrack.Interval I in SelectedTrack.Intervals )
				if ( _Time >= I.TimeStart && _Time <= I.TimeEnd )
				{	// Got an interval to drop to !
					FTC.AnimationEditorControl.CreateInterpolatedKey( _Time, AnimationTrackPanel.KEY_TYPE.DEFAULT );

					// Re-render into the bitmap to update keys...
					panelFastPaint.RenderControl();

					return;
				}
		}

		/// <summary>
		/// Copies the provided parameter track to clipboard
		/// </summary>
		/// <param name="_Track"></param>
		public void			CopyToClipboard( Sequencor.ParameterTrack _Track )
		{
			System.IO.MemoryStream	Stream = new System.IO.MemoryStream();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );
			_Track.Save( Writer );
			Writer.Close();
			Stream.Close();

			Clipboard.SetData( "Sequencor.ParameterTrack", Stream.ToArray() );

			Writer.Dispose();
			Stream.Dispose();
		}

		/// <summary>
		/// Tells if the parameter currently copied in the clipboard can be pasted to the provided sequencor
		/// </summary>
		/// <param name="_Interval"></param>
		/// <returns></returns>
		public bool			CanPasteParameter( Sequencor _Sequencer )
		{
			return _Sequencer != null && Clipboard.ContainsData( "Sequencor.ParameterTrack" );
		}

		/// <summary>
		/// Pastes the parameter currently copied in the clipboard to the provided sequencor
		/// </summary>
		/// <param name="_Interval"></param>
		/// <returns>The pasted track</returns>
		public Sequencor.ParameterTrack		PasteParameter( Sequencor _Sequencer )
		{
			if ( !Clipboard.ContainsData( "Sequencor.ParameterTrack" ) )
				throw new Exception( "Clipboard does not contain parameter track data !" );

			byte[]	Parameter = Clipboard.GetData( "Sequencor.ParameterTrack" ) as byte[];

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( Parameter );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			Sequencor.ParameterTrack	Result = _Sequencer.CreateTrack( Reader );

			Reader.Dispose();
			Stream.Dispose();

			return Result;
		}

		/// <summary>
		/// Copies the provided interval to clipboard
		/// </summary>
		/// <param name="_Track"></param>
		public void			CopyToClipboard( Sequencor.ParameterTrack.Interval _Interval )
		{
			System.IO.MemoryStream	Stream = new System.IO.MemoryStream();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );

			Writer.Write( (int) _Interval.ParentTrack.Type );	// Save interval type
			_Interval.Save( Writer );

			Writer.Close();
			Stream.Close();

			Clipboard.SetData( "Sequencor.ParameterTrack.Interval", Stream.ToArray() );

			Writer.Dispose();
			Stream.Dispose();
		}

		/// <summary>
		/// Tells if the interval currently copied in the clipboard can be pasted to the provided parameter track
		/// </summary>
		/// <param name="_Interval"></param>
		/// <returns></returns>
		public bool			CanPasteIntervalToParameter( Sequencor.ParameterTrack _Track )
		{
			if ( _Track == null || !Clipboard.ContainsData( "Sequencor.ParameterTrack.Interval" ) )
				return false;

			byte[]	Interval = Clipboard.GetData( "Sequencor.ParameterTrack.Interval" ) as byte[];

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( Interval );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			Sequencor.ParameterTrack.PARAMETER_TYPE	Type = (Sequencor.ParameterTrack.PARAMETER_TYPE) Reader.ReadInt32();
			Reader.Dispose();
			Stream.Dispose();

			return _Track.Type == Type;
		}

		/// <summary>
		/// Pastes the interval currently copied in the clipboard to the provided parameter track
		/// </summary>
		/// <param name="_Track"></param>
		/// <param name="_NewIntervalTime">A new track time for the pasted interval</param>
		/// <returns>The pasted interval</returns>
		public Sequencor.ParameterTrack.Interval	PasteIntervalToParameter( Sequencor.ParameterTrack _Track, float _NewIntervalTime )
		{
			if ( !Clipboard.ContainsData( "Sequencor.ParameterTrack.Interval" ) )
				throw new Exception( "Clipboard does not contain interval data !" );

			byte[]	Interval = Clipboard.GetData( "Sequencor.ParameterTrack.Interval" ) as byte[];

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( Interval );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			Sequencor.ParameterTrack.PARAMETER_TYPE	Type = (Sequencor.ParameterTrack.PARAMETER_TYPE) Reader.ReadInt32();
			if ( Type != _Track.Type )
				throw new Exception( "Interval in the clipboard is not compatible with provided parameter track !" );

			Sequencor.ParameterTrack.Interval	Result = _Track.CreateInterval( Reader );
			float	Duration = Result.Duration;
			Result.UpdateBothTimes( _NewIntervalTime, _NewIntervalTime + Duration );

			Reader.Dispose();
			Stream.Dispose();

			return Result;
		}

		/// <summary>
		/// Copies the provided keys to clipboard
		/// </summary>
		/// <param name="_Track"></param>
		public void			CopyToClipboard( Sequencor.AnimationTrack.Key[] _Keys )
		{
			System.IO.MemoryStream	Stream = new System.IO.MemoryStream();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );

			Writer.Write( (int) _Keys[0].ParentAnimationTrack.ParentInterval.ParentTrack.Type );	// Save key types
			Writer.Write( _Keys.Length );
			foreach ( Sequencor.AnimationTrack.Key K in _Keys )
			{
				Writer.Write( K.ParentAnimationTrack.ParentInterval.IndexOf( K.ParentAnimationTrack ) );	// Save animation track index for re-inserting
				K.Save( Writer );
			}

			Writer.Close();
			Stream.Close();

			Clipboard.SetData( "Sequencor.AnimationKeys", Stream.ToArray() );

			Writer.Dispose();
			Stream.Dispose();
		}

		/// <summary>
		/// Tells if the keys currently copied in the clipboard can be pasted to the provided interval
		/// </summary>
		/// <param name="_Interval"></param>
		/// <returns></returns>
		public bool			CanPasteKeyToInterval( Sequencor.ParameterTrack.Interval _Interval )
		{
			if ( _Interval == null || !Clipboard.ContainsData( "Sequencor.AnimationKeys" ) )
				return false;

			byte[]	Keys = Clipboard.GetData( "Sequencor.AnimationKeys" ) as byte[];

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( Keys );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			Sequencor.ParameterTrack.PARAMETER_TYPE	Type = (Sequencor.ParameterTrack.PARAMETER_TYPE) Reader.ReadInt32();
			Reader.Dispose();
			Stream.Dispose();

			return _Interval.ParentTrack.Type == Type;
		}

		/// <summary>
		/// Pastes the interval currently copied in the clipboard to the provided parameter track
		/// </summary>
		/// <param name="_Interval"></param>
		/// <param name="_NewKeyTrackTime">A new track time for the pasted keys</param>
		public void			PasteKeysToInterval( Sequencor.ParameterTrack.Interval _Interval, float _NewKeyTrackTime )
		{
			if ( !Clipboard.ContainsData( "Sequencor.AnimationKeys" ) )
				throw new Exception( "Clipboard does not contain key data !" );

			byte[]	Keys = Clipboard.GetData( "Sequencor.AnimationKeys" ) as byte[];

			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( Keys );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );

			Sequencor.ParameterTrack.PARAMETER_TYPE	Type = (Sequencor.ParameterTrack.PARAMETER_TYPE) Reader.ReadInt32();
			if ( Type != _Interval.ParentTrack.Type )
				throw new Exception( "Keys in the clipboard are not compatible with provided interval !" );

			int	KeysCount = Reader.ReadInt32();
			for ( int KeyIndex=0; KeyIndex < KeysCount; KeyIndex++ )
			{
				int	AnimTrackIndex = Reader.ReadInt32();
				Sequencor.AnimationTrack.Key	K = _Interval[AnimTrackIndex].CreateKey( Reader );
				K.TrackTime = _NewKeyTrackTime;
			}

			Reader.Dispose();
			Stream.Dispose();
		}

		/// <summary>
		/// Finds the track control wrapping the specified track
		/// </summary>
		/// <param name="_Track"></param>
		/// <returns></returns>
		internal FoldableTrackControl	FindTrackControl( Sequencor.ParameterTrack _Track )
		{
			if ( _Track == null )
				return	null;

			foreach ( FoldableTrackControl TC in m_TrackControls )
				if ( TC.Track == _Track )
					return	TC;

			return	null;
		}

		/// <summary>
		/// Queries the current value of a parameter from an external application
		/// </summary>
		/// <param name="_Track"></param>
		/// <returns></returns>
		internal object		QueryCurrentParameterValue( Sequencor.ParameterTrack _Track )
		{
			return ParameterValueNeeded != null ? ParameterValueNeeded( this, _Track ) : null;
		}

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
			return System.Windows.Forms.MessageBox.Show( _Message, "Sequencor", _Buttons, _Icon, _DefaultButton );
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
			return System.Windows.Forms.MessageBox.Show( _Owner, _Message, "Sequencor", _Buttons, _Icon, _DefaultButton );
		}

		#endregion

		#endregion

		#region EVENT HANDLER

		protected void Sequencer_TracksChanged( object sender, EventArgs e )
		{
			BuildTracks();
		}

		protected void TrackInfo_TrackRename( object sender, EventArgs e )
		{
			UserEditParameterTrackData( (sender as FoldableTrackControl).Track );
		}

		#region GUI

		private void timeLineControl_CustomGraduationPaint( GraduationPanel _Panel, PaintEventArgs _e )
		{
			if ( m_Sequencer == null )
				return;
		}

		private void timeLineControl_MouseMove( object sender, MouseEventArgs e )
		{
			// Update time tooltip
			toolTip.SetToolTip( timeLineControl, timeLineControl.ClientToGraduation( e.X ).ToString() );
		}

		private void timeLineControl_CursorMoved( TimeLineControl _Sender )
		{
 			if ( !timer.Enabled )
				SetSequenceTime( SequenceTime, _Sender.CursorPosition );

			// Invalidate fast paint panel when in play mode
			if ( checkBoxPlay.Checked )
				panelFastPaint.Invalidate();
		}

		private void timeLineControl_VisibleRangeChanged( TimeLineControl _Sender )
		{
			foreach ( FoldableTrackControl TC in m_TrackControls )
				TC.SetRange( _Sender.VisibleBoundMin, _Sender.VisibleBoundMax );

			// Update fast paint bitmap
			if ( checkBoxPlay.Checked )
				panelFastPaint.RenderControl();
		}

		private void checkBoxPlay_CheckedChanged( object sender, EventArgs e )
		{
			checkBoxPlay.BackgroundImage = checkBoxPlay.Checked ? Properties.Resources.Track___Pause : Properties.Resources.Track___Play;

			if ( checkBoxPlay.Checked )
			{	// Show fast paint panel
				panelFastPaint.RenderControl();
				panelFastPaint.Visible = true;
				panelTracks.Visible = false;
			}
			else
			{	// Hide fast paint panel
				panelFastPaint.Visible = false;
				panelTracks.Visible = true;
			}

			m_LastTimeStamp = m_TimeAtLastStall = DateTime.Now;
			timer.Enabled = checkBoxPlay.Checked;

			// Notify of play or stop
			if ( checkBoxPlay.Checked && SequencePlay != null )
				SequencePlay( this, EventArgs.Empty );
			else if ( !checkBoxPlay.Checked && SequencePause != null )
				SequencePause( this, EventArgs.Empty );
		}

		private void buttonStop_Click( object sender, EventArgs e )
		{
			Stop();
		}

		private void buttonZoomOut_Click( object sender, EventArgs e )
		{
			// Retrieve the global range of intervals
			float	fIntervalTimeMin = +float.MaxValue;
			float	fIntervalTimeMax = -float.MaxValue;

			if ( SelectedInterval == null )
				foreach ( FoldableTrackControl TC in m_TrackControls )
					foreach ( Sequencor.ParameterTrack.Interval I in TC.Track.Intervals )
					{
						fIntervalTimeMin = Math.Min( fIntervalTimeMin, I.TimeStart );
						fIntervalTimeMax = Math.Max( fIntervalTimeMax, I.TimeEnd );
					}
			else
			{	// Use selected interval
				fIntervalTimeMin = SelectedInterval.TimeStart;
				fIntervalTimeMax = SelectedInterval.TimeEnd;
			}

			fIntervalTimeMin /= 1.05f;
			fIntervalTimeMax *= 1.051f;

			// Ensure we have not a stupid range
			if ( fIntervalTimeMax - fIntervalTimeMin < 0.01f )
			{
				fIntervalTimeMin = 0.0f;
				fIntervalTimeMax = DEFAULT_VISIBLE_RANGE;
			}

			timeLineControl.SetVisibleRange( fIntervalTimeMin, fIntervalTimeMax );
		}

		#endregion

		#region Context Menu

		#region Time Line Menu

		private void contextMenuStrip_Opening( object sender, CancelEventArgs e )
		{
			m_ContextMenuPosition = timeLineControl.PointToClient( MousePosition );

			bool	bSequenceTimeCanChange = false;

			setCursorAtTimeToolStripMenuItem.Enabled = true;
			setSequenceEndTimeAtCursorToolStripMenuItem.Enabled = bSequenceTimeCanChange;
			setSequenceEndTimeAtMousePositionToolStripMenuItem.Enabled = bSequenceTimeCanChange;
			setSequenceEndTimeAtInfinityToolStripMenuItem.Enabled = bSequenceTimeCanChange;
		}

		private void setCursorAtTimeToolStripMenuItem_Click( object sender, EventArgs e )
		{
			SetTimeForm	F = new SetTimeForm();
			F.Text = "Select time at which to set the cursor...";
			F.Time = timeLineControl.CursorPosition;
			if ( F.ShowDialog( this ) != DialogResult.OK )
				return;

			timeLineControl.CursorPosition = F.Time;
		}

		private void setSequenceTimeEndAtMousePositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
//			m_Sequencer.SequenceTimeEnd = timeLineControl.ClientToGraduation( m_ContextMenuPosition.X );
		}

		private void setSequenceTimeEndAtCursorToolStripMenuItem_Click( object sender, EventArgs e )
		{
//			m_Sequencer.SequenceTimeEnd = timeLineControl.CursorPosition;
		}

		private void setSequenceTimeEndAtInfinityToolStripMenuItem_Click( object sender, EventArgs e )
		{
//			m_Sequencer.SequenceTimeEnd = float.MaxValue;
		}

		#endregion

		#region Tracks Menu

		private void contextMenuStripTracks_Opening( object sender, CancelEventArgs e )
		{
			deleteParameterToolStripMenuItem.Enabled = SelectedTrack != null;
		}

		private void createParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserCreateNewParameterTrack();
		}

		private void editParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserEditSelectedParameterTrackData();
		}

		private void editTrackColorToolStripMenuItem_Click( object sender, EventArgs e )
		{
		}

		private void deleteParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserConfirmTrackDelete( SelectedTrack );
		}

		#endregion

		#endregion

		#region Track Info Controls Events

		protected void TrackControl_SelectedChanged( object sender, EventArgs e )
		{
			FoldableTrackControl	Sender = sender as FoldableTrackControl;

			// Update selection
			if ( Sender.Selected )
				SelectedTrack = Sender.Track;
		}

		protected void TrackInfo_SelectedIntervalChanged( object sender, EventArgs e )
		{
			SelectedInterval = (sender as FoldableTrackControl).SelectedInterval;
		}

		protected void	TrackInfo_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			// Simulate a selection change (a selection update really)
			if ( SelectedTrackChanged != null )
				SelectedTrackChanged( this, EventArgs.Empty );
		}

		#endregion

		protected DateTime	m_LastTimeStamp = DateTime.Now;		// The last time stamp from the timer
		protected float		m_LastTimeStampExternal = 0.0f;
		protected DateTime	m_TimeAtLastStall = DateTime.Now;
		protected bool		m_bAutoEnableTimer = false;			// Set to true if the timer is stalled
		private void	timer_Tick( object sender, EventArgs e )
		{
			DateTime	CurrentTimeStamp = DateTime.Now;
			float		fDeltaTime = (float) (CurrentTimeStamp - m_LastTimeStamp).TotalMilliseconds * 0.001f;
			m_LastTimeStamp = CurrentTimeStamp;

			float	fPredictedSequenceTime = SequenceTime + fDeltaTime;

			// Query the sequence time externally
			if ( SequenceTimeNeeded != null )
			{
				fPredictedSequenceTime = SequenceTimeNeeded( this );
				fDeltaTime = fPredictedSequenceTime - m_LastTimeStampExternal;
				m_LastTimeStampExternal = fPredictedSequenceTime;
			}

			// Step...
			SetSequenceTime( fPredictedSequenceTime - fDeltaTime, fPredictedSequenceTime );

			// Check if the timer should be stalled
			float		fTimeFromLastStall = (float) (CurrentTimeStamp - m_TimeAtLastStall).TotalMilliseconds * 0.001f;
			if ( fTimeFromLastStall > STALL_TIMER_DELAY )
			{	// Stall the timer and set the auto-enable flag (so it's restarted on idle, once messages have been processed)
				m_bAutoEnableTimer = true;
				timer.Enabled = false;
			}
		}

		protected void	Application_Idle( object sender, EventArgs e )
		{
			if ( !m_bAutoEnableTimer )
				return;

			// The timer was stalled, restart it
			m_bAutoEnableTimer = false;
			timer.Enabled = true;
			m_TimeAtLastStall = DateTime.Now;
		}

		#endregion
	}
}
