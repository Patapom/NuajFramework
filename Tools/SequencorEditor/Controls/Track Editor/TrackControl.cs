using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using SequencorLib;

namespace SequencorEditor
{
	/// <summary>
	/// This control represents a sequence track for an emitter
	/// </summary>
	[System.Diagnostics.DebuggerDisplay( "Track={Track.Name} Selected={Selected}" )]
	public partial class TrackControl : UserControl
	{
		#region CONSTANTS

		protected const float	ANCHOR_PIXEL_TOLERANCE	= 8.0f;	// Anchor to an interval boundary if less than 8 pixels appart

		#endregion

		#region NESTED TYPES

		public delegate void	IntervalEditEventHandler( TrackIntervalPanel _Sender, Sequencor.ParameterTrack.Interval _Interval );

		protected enum MANIPULATION_MODE	{	NONE,
												INTERVAL_MOVE,
												INTERVAL_START_BOUND,
												INTERVAL_END_BOUND
											}

		[System.Diagnostics.DebuggerDisplay( "[ {m_TimeStart}, {m_TimeEnd} ]" )]
		protected class		IntervalBackup
		{
			public float	m_ActualTimeStart = 0.0f;
			public float	m_ActualTimeEnd = 0.0f;

			public	IntervalBackup( Sequencor.ParameterTrack.Interval _Interval )
			{
				m_ActualTimeStart = _Interval.ActualTimeStart;
				m_ActualTimeEnd = _Interval.ActualTimeEnd;
			}
		};

		#endregion

		#region FIELDS

		protected SequencerControl			m_Owner = null;

		protected Sequencor.ParameterTrack	m_Track = null;
		protected bool						m_bSelected = false;
		protected Sequencor.ParameterTrack.Interval	m_SelectedInterval = null;

		// Intervals manipulation
		protected MANIPULATION_MODE			m_ManipulationMode = MANIPULATION_MODE.NONE;
		protected Sequencor.ParameterTrack.Interval	m_HoveredInterval = null;

			// Button down state
		protected MouseButtons				m_MouseButtonsDown = MouseButtons.None;
		protected Sequencor.ParameterTrack.Interval	m_ManipulatedInterval = null;	// The interval we're manipulating
		protected IntervalBackup			m_ButtonDownInterval = null;			// The manipulated interval's bounds as backup
		protected Dictionary<Sequencor.ParameterTrack.Interval,IntervalBackup>	m_ButtonDownIntervals = new Dictionary<Sequencor.ParameterTrack.Interval,IntervalBackup>();	// Backup of all interval bounds in the track at button down time
		protected PointF					m_MouseDownPosition = PointF.Empty;

		// Context Menu
		protected PointF					m_ContextMenuPosition = PointF.Empty;

		// Appearance
		protected Color						m_IntervalsColor = Color.RoyalBlue;
		protected Color						m_SelectedColor = Color.IndianRed;

		protected bool						m_bInternalChange = false;

		#endregion

		#region PROPERTIES

		[Browsable( false )]
		public SequencerControl		Owner
		{
			get { return m_Owner; }
			set
			{
				if ( value == m_Owner )
					return;

				m_Owner = value;
				m_Owner.SequenceTimeChanged += new EventHandler( Owner_SequenceTimeChanged );
			}
		}

		/// <summary>
		/// Gets or sets the observed track
		/// </summary>
		[Browsable( false )]
		public Sequencor.ParameterTrack	Track
		{
			get { return m_Track; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_Track )
					return;

				if ( m_Track != null )
				{
// 					m_Track.EnabledChanged -= new SequenceTrackEventHandler( Track_EnabledChanged );
					m_Track.NameChanged -= new EventHandler( Track_NameChanged );
					m_Track.GUIDChanged -= new EventHandler( Track_GUIDChanged );

					labelTrack.Text = "<INVALID>";
					labelGUID.Text = "(0)";
				}

				m_Track = value;

				if ( m_Track != null )
				{
// 					m_Track.EnabledChanged += new SequenceTrackEventHandler( Track_EnabledChanged );
// 					Track_EnabledChanged( m_Track );
					m_Track.NameChanged += new EventHandler( Track_NameChanged );
					Track_NameChanged( m_Track, EventArgs.Empty );
					m_Track.GUIDChanged += new EventHandler( Track_GUIDChanged );
					Track_GUIDChanged( m_Track, EventArgs.Empty );
				}

				// Update the GUI
				m_bInternalChange = true;
				trackIntervalPanel.Track = m_Track;
				Enabled = m_Track != null;
				m_bInternalChange = false;
			}
		}

		/// <summary>
		/// Gets or sets the track selection state
		/// </summary>
		[Category( "Selection" )]
		public bool			Selected
		{
			get { return m_bSelected; }
			set
			{
				m_bSelected = value;
				trackIntervalPanel.Selected = value;

				panelInfos.BackColor = value ? m_SelectedColor : SystemColors.ControlDark;
			}
		}

		/// <summary>
		/// Gets or sets the currently selected interval in that track
		/// </summary>
		[Category( "Selection" )]
		public Sequencor.ParameterTrack.Interval	SelectedInterval
		{
			get { return trackIntervalPanel.SelectedInterval; }
			set
			{
				if ( value == trackIntervalPanel.SelectedInterval )
					return;

				trackIntervalPanel.SelectedInterval = value;

				if ( SelectedIntervalChanged != null )
					SelectedIntervalChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Tells if the animation editor is visible (unfolded) or invisible (folded)
		/// </summary>
		[Category( "Track" )]
		protected bool	m_bAnimationTrackVisible = false;
		public bool		AnimationTrackVisible
		{
			get { return m_bAnimationTrackVisible; }
			set
			{
				if ( value == m_bAnimationTrackVisible )
					return;

				m_bAnimationTrackVisible = value;
				checkBoxShowTrackAnim.Checked = value;

				if ( AnimationTrackVisibleStateChanged != null )
					AnimationTrackVisibleStateChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the color of the intervals in that track
		/// </summary>
		[Category( "Track" )]
		public Color								TrackColor
		{
			get { return trackIntervalPanel.ForeColor; }
			set { trackIntervalPanel.ForeColor = value; }
		}

		[Category( "Track" )]
		public event IntervalEditEventHandler		IntervalEdit;

		[Category( "Track" )]
		public event EventHandler					TrackRename;

		[Category( "Track" )]
		public event EventHandler					SelectedIntervalChanged;

		[Category( "Track" )]
		public event EventHandler					AnimationTrackVisibleStateChanged;

		[Category( "Appearance" )]
		public Color								IntervalsColor
		{
			get { return m_IntervalsColor; }
			set
			{
				m_IntervalsColor = value;
				trackIntervalPanel.ForeColor = m_IntervalsColor;
				Invalidate( true );
			}
		}

		protected bool		IsAnchorMode
		{
			get { return (ModifierKeys & Keys.Alt) == 0; }	// Alt disables anchoring mode
		}

		protected bool		IsCopyMode
		{
			get { return (ModifierKeys & Keys.Control) != 0; }
		}

		#endregion

		#region METHODS

		public TrackControl()
		{
			InitializeComponent();

			trackIntervalPanel.Owner = this;
			trackIntervalPanel.MouseWheel += new MouseEventHandler( trackIntervalPanel_MouseWheel );
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				Track = null;

				if (components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Sets both min and max range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetRange( float _RangeMin, float _RangeMax )
		{
			trackIntervalPanel.SetRange( _RangeMin, _RangeMax );
		}

		/// <summary>
		/// Attempts to find an "anchor time" given a base time value
		/// </summary>
		/// <param name="_AnchoringInterval">The interval to anchor and which is excluded from anchor search</param>
		/// <param name="_TimeToAnchor">The time to find an anchor for</param>
		/// <param name="_fAnchoredTime">The anchored time</param>
		/// <returns>True if a valid anchor was found</returns>
		protected bool	FindAnchor( Sequencor.ParameterTrack.Interval _AnchoringInterval, float _TimeToAnchor, out float _fAnchoredTime )
		{
			_fAnchoredTime = -1.0f;
			bool	bFoundAnchor = false;
			float	fBestAnchorDistance = float.MaxValue;

			// Convert pixel tolerance into time tolerance
			float	fAnchorTimeTolerance = ANCHOR_PIXEL_TOLERANCE * (m_Owner.TimeLineControl.VisibleBoundMax - m_Owner.TimeLineControl.VisibleBoundMin) / m_Owner.TimeLineControl.Width;

			// Check anchoring with cursor
			float	fAnchorDistance = Math.Abs( _TimeToAnchor - m_Owner.TimeLineControl.CursorPosition );
			if ( fAnchorDistance < fAnchorTimeTolerance )
			{
				fBestAnchorDistance = fAnchorDistance;
				_fAnchoredTime = m_Owner.TimeLineControl.CursorPosition;
				bFoundAnchor = true;
			}

			// Check anchoring with start time
			fAnchorDistance = Math.Abs( _TimeToAnchor - 0.0f );
			if ( fAnchorDistance < fAnchorTimeTolerance )
			{
				fBestAnchorDistance = fAnchorDistance;
				_fAnchoredTime = 0.0f;
				bFoundAnchor = true;
			}

			// Check anchoring with end time
// 			fAnchorDistance = Math.Abs( _TimeToAnchor - m_Sequencer.SequenceTimeEnd );
// 			if ( fAnchorDistance < fAnchorTimeTolerance && fAnchorDistance < fBestAnchorDistance )
// 			{
// 				fBestAnchorDistance = fAnchorDistance;
// 				_fAnchoredTime = m_Sequencer.SequenceTimeEnd;
// 				bFoundAnchor = true;
// 			}

			// Analyze each track for intervals with a boundary close enough to anchor to it
			foreach ( Sequencor.ParameterTrack Track in m_Track.Owner.Tracks )
				foreach ( Sequencor.ParameterTrack.Interval Interval in Track.Intervals )
					if ( Interval != _AnchoringInterval )
					{
						// Check interval's start boundary
						fAnchorDistance = Math.Abs( Interval.TimeStart - _TimeToAnchor );
						if ( fAnchorDistance < fAnchorTimeTolerance && fAnchorDistance < fBestAnchorDistance )
						{	// Found a new anchor
							fBestAnchorDistance = fAnchorDistance;
							_fAnchoredTime = Interval.TimeStart;
							bFoundAnchor = true;
						}

						// Check interval's end boundary
						fAnchorDistance = Math.Abs( Interval.TimeEnd - _TimeToAnchor );
						if ( fAnchorDistance < fAnchorTimeTolerance && fAnchorDistance < fBestAnchorDistance )
						{	// Found a new anchor
							fBestAnchorDistance = fAnchorDistance;
							_fAnchoredTime = Interval.TimeEnd;
							bFoundAnchor = true;
						}
					}

			return	bFoundAnchor;
		}

		#endregion

		#region EVENT HANDLERS

		protected void Owner_SequenceTimeChanged( object sender, EventArgs e )
		{
			trackIntervalPanel.Invalidate();
		}

		protected void Track_NameChanged( object sender, EventArgs e )
		{
			if ( m_Track == null )
			{
				labelTrack.Text = "<INVALID>";
				return;
			}

			labelTrack.Text = m_Track.Name + " (" + m_Track.Type + ")";
		}

		protected void Track_GUIDChanged( object sender, EventArgs e )
		{
			labelGUID.Text = "(" + (m_Track != null ? m_Track.GUID : 0).ToString() + ")";
		}

		#region Context Menu

		#region Intervals Menu

		private void contextMenuStrip_Opening( object sender, CancelEventArgs e )
		{
			m_ContextMenuPosition = trackIntervalPanel.PointToClient( MousePosition );

			bool	bValidTrack = m_Track != null;
			bool	bValidHoveredInterval = bValidTrack && m_HoveredInterval != null;
			bool	bCursorInInterval = bValidHoveredInterval && m_Owner.TimeLineControl.CursorPosition > m_HoveredInterval.ActualTimeStart && m_Owner.TimeLineControl.CursorPosition < m_HoveredInterval.ActualTimeEnd;

			createIntervalAtPositionToolStripMenuItem.Enabled = bValidTrack;
			moveIntervalAtCursorPositionToolStripMenuItem.Enabled = bValidHoveredInterval;
			cloneIntervalToolStripMenuItem.Enabled = bValidHoveredInterval;
			splitIntervalAtCursorPositionToolStripMenuItem.Enabled = bCursorInInterval;
			mergeWithNextIntervalToolStripMenuItem.Enabled = bValidHoveredInterval && m_Track.IndexOf( m_HoveredInterval ) < m_Track.IntervalsCount-1;
			editTrackColorToolStripMenuItem.Enabled = bValidTrack;
			deleteIntervalToolStripMenuItem.Enabled = bValidHoveredInterval;

			copyIntervalToolStripMenuItem.Enabled = bValidHoveredInterval;
			pasteIntervalToolStripMenuItem.Enabled = m_Owner.CanPasteIntervalToParameter( Track );
		}

		private void createIntervalAtPositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			float	fNewIntervalPosition = trackIntervalPanel.ClientToSequenceTime( m_ContextMenuPosition.X );

			// Retrieve the index where to insert the interval
			int IntervalIndex = 0;
			Sequencor.ParameterTrack.Interval[]	Intervals = m_Track.Intervals;
			for ( ; IntervalIndex < Intervals.Length; IntervalIndex++ )
			{
				Sequencor.ParameterTrack.Interval	Interval = Intervals[IntervalIndex];
				if ( Interval.TimeStart > fNewIntervalPosition )
					break;	// This interval stands after, insert here...
			}

			trackIntervalPanel.PreventRefresh = true;

			// Create a new interval
			trackIntervalPanel.SelectedInterval = m_Track.CreateInterval( fNewIntervalPosition, fNewIntervalPosition + 0.1f * (m_Owner.TimeLineControl.VisibleBoundMax - m_Owner.TimeLineControl.VisibleBoundMin) );

			trackIntervalPanel.PreventRefresh = false;
			trackIntervalPanel.Refresh();
		}

		private void moveIntervalAtCursorPositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			trackIntervalPanel.PreventRefresh = true;

			float	Duration = m_HoveredInterval.Duration;
			m_HoveredInterval.ActualTimeStart = m_Owner.TimeLineControl.CursorPosition;
			m_HoveredInterval.ActualTimeEnd = m_HoveredInterval.ActualTimeStart + Duration;

			trackIntervalPanel.PreventRefresh = false;
			trackIntervalPanel.Refresh();

		}

		internal void copyIntervalToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Sequencor.ParameterTrack.Interval	SourceInterval = m_HoveredInterval != null ? m_HoveredInterval : SelectedInterval;
			if ( SourceInterval != null )
				m_Owner.CopyToClipboard( SourceInterval );
			else
				SequencerControl.MessageBox( "No source interval (hovered or selected) to copy from !", MessageBoxButtons.OK, MessageBoxIcon.Warning );
		}

		private void cloneIntervalToolStripMenuItem_Click( object sender, EventArgs e )
		{
			float	SourceEndTime = m_HoveredInterval.ActualTimeEnd;
			float	SourceDuration = m_HoveredInterval.Duration;
			Sequencor.ParameterTrack.Interval	Copy = m_Track.Clone( m_HoveredInterval );
			Copy.UpdateBothTimes( SourceEndTime, SourceEndTime + SourceDuration );
			SelectedInterval = Copy;
		}

		private void pasteIntervalToolStripMenuItem_Click( object sender, EventArgs e )
		{
			try
			{
				float	NewIntervalPosition = trackIntervalPanel.ClientToSequenceTime( trackIntervalPanel.PointToClient( MousePosition ).X );
				m_Owner.PasteIntervalToParameter( Track, NewIntervalPosition );
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( "An error occurred while pasting an interval from clipboard :\r\n\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		private void splitIntervalAtCursorPositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Sequencor.ParameterTrack.Interval		CurrentInterval = m_HoveredInterval;

			// Save the keys in track time
			List<Sequencor.AnimationTrack.Key>[]	Keys0 = new List<Sequencor.AnimationTrack.Key>[CurrentInterval.AnimationTracksCount];	// For first part
			List<Sequencor.AnimationTrack.Key>[]	Keys1 = new List<Sequencor.AnimationTrack.Key>[CurrentInterval.AnimationTracksCount];	// For second part
			List<float>[]							KeyTimes0 = new List<float>[CurrentInterval.AnimationTracksCount];
			List<float>[]							KeyTimes1 = new List<float>[CurrentInterval.AnimationTracksCount];
			float[]									ContinuityKeyValue = new float[CurrentInterval.AnimationTracksCount];

			float	SplitPosition = m_Owner.TimeLineControl.CursorPosition;
			for ( int AnimTrackIndex=0; AnimTrackIndex < CurrentInterval.AnimationTracksCount; AnimTrackIndex++ )
			{
				Keys0[AnimTrackIndex] = new List<Sequencor.AnimationTrack.Key>();
				KeyTimes0[AnimTrackIndex] = new List<float>();
				Keys1[AnimTrackIndex] = new List<Sequencor.AnimationTrack.Key>();
				KeyTimes1[AnimTrackIndex] = new List<float>();

				foreach ( Sequencor.AnimationTrack.Key K in CurrentInterval[AnimTrackIndex].Keys )
				{
					if ( K.TrackTime < SplitPosition )
					{
						Keys0[AnimTrackIndex].Add( K );
						KeyTimes0[AnimTrackIndex].Add( K.TrackTime );
					}
					else
					{
						Keys1[AnimTrackIndex].Add( K );
						KeyTimes1[AnimTrackIndex].Add( K.TrackTime );
					}
				}

				// Store continuity key value
				if ( CurrentInterval[AnimTrackIndex] is Sequencor.AnimationTrackFloat )
					ContinuityKeyValue[AnimTrackIndex] = (CurrentInterval[AnimTrackIndex] as Sequencor.AnimationTrackFloat).ImmediateEval( SplitPosition );
			}

			// Change interval range
			float	OldTimeEnd = CurrentInterval.ActualTimeEnd;
			CurrentInterval.ActualTimeEnd = SplitPosition;

			// Create new interval range
			Sequencor.ParameterTrack.Interval	NewInterval = m_Track.CreateInterval( SplitPosition, OldTimeEnd );

			// Update keys in first interval
			for ( int AnimTrackIndex=0; AnimTrackIndex < CurrentInterval.AnimationTracksCount; AnimTrackIndex++ )
			{
				Sequencor.AnimationTrack	T = CurrentInterval[AnimTrackIndex];
				for ( int KeyIndex=0; KeyIndex < Keys0[AnimTrackIndex].Count; KeyIndex++ )
				{
					Sequencor.AnimationTrack.Key K = Keys0[AnimTrackIndex][KeyIndex];
					K.TrackTime = KeyTimes0[AnimTrackIndex][KeyIndex];	// Restore track time
				}

				// Remove keys that now stand in the new interval
				foreach ( Sequencor.AnimationTrack.Key K in Keys1[AnimTrackIndex] )
					T.RemoveKey( K );

				// Add continuity keys for float tracks
				if ( T is Sequencor.AnimationTrackFloat )
					(T as Sequencor.AnimationTrackFloat).AddKey( SplitPosition, ContinuityKeyValue[AnimTrackIndex] );
			}

			// Add keys in new interval
			for ( int AnimTrackIndex=0; AnimTrackIndex < CurrentInterval.AnimationTracksCount; AnimTrackIndex++ )
			{
				Sequencor.AnimationTrack	T = NewInterval[AnimTrackIndex];
				for ( int KeyIndex=0; KeyIndex < Keys1[AnimTrackIndex].Count; KeyIndex++ )
				{
					Sequencor.AnimationTrack.Key	SourceKey = Keys1[AnimTrackIndex][KeyIndex];
					Sequencor.AnimationTrack.Key	TargetKey = T.Clone( SourceKey );

					TargetKey.TrackTime = KeyTimes1[AnimTrackIndex][KeyIndex];	// Restore track time
				}
				// Add continuity keys for float tracks
				if ( T is Sequencor.AnimationTrackFloat )
					(T as Sequencor.AnimationTrackFloat).AddKey( SplitPosition, ContinuityKeyValue[AnimTrackIndex] );
			}
		}

		private void mergeWithNextIntervalToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Sequencor.ParameterTrack.Interval	CurrentInterval = m_HoveredInterval;
			Sequencor.ParameterTrack.Interval	NextInterval = m_Track.Intervals[m_Track.IndexOf( CurrentInterval ) + 1];

			// Gather keys & their track time from both intervals
			List<Sequencor.AnimationTrack.Key>[]	Keys1 = new List<Sequencor.AnimationTrack.Key>[CurrentInterval.AnimationTracksCount];
			List<float>[]							KeyTimes0 = new List<float>[CurrentInterval.AnimationTracksCount];
			List<float>[]							KeyTimes1 = new List<float>[CurrentInterval.AnimationTracksCount];

			for ( int AnimTrackIndex=0; AnimTrackIndex < CurrentInterval.AnimationTracksCount; AnimTrackIndex++ )
			{
				Keys1[AnimTrackIndex] = new List<Sequencor.AnimationTrack.Key>();
				KeyTimes0[AnimTrackIndex] = new List<float>();
				KeyTimes1[AnimTrackIndex] = new List<float>();
				
				foreach ( Sequencor.AnimationTrack.Key K in CurrentInterval[AnimTrackIndex].Keys )
					KeyTimes0[AnimTrackIndex].Add( K.TrackTime );
				foreach ( Sequencor.AnimationTrack.Key K in NextInterval[AnimTrackIndex].Keys )
				{
					Keys1[AnimTrackIndex].Add( K );
					KeyTimes1[AnimTrackIndex].Add( K.TrackTime );
				}
			}
			
			// Delete next interval
			m_Track.RemoveInterval( NextInterval );

			// Update current interval time & keys
			CurrentInterval.ActualTimeEnd = NextInterval.ActualTimeEnd;
			for ( int AnimTrackIndex=0; AnimTrackIndex < CurrentInterval.AnimationTracksCount; AnimTrackIndex++ )
			{
				Sequencor.AnimationTrack	T = CurrentInterval[AnimTrackIndex];

				// Update existing keys' time
				for ( int KeyIndex=0; KeyIndex < CurrentInterval[AnimTrackIndex].KeysCount; KeyIndex++ )
				{
					Sequencor.AnimationTrack.Key K = T[KeyIndex];
					K.TrackTime = KeyTimes0[AnimTrackIndex][KeyIndex];
				}

				// Add new keys
				for ( int KeyIndex=0; KeyIndex < Keys1[AnimTrackIndex].Count; KeyIndex++ )
				{
					Sequencor.AnimationTrack.Key	SourceKey = Keys1[AnimTrackIndex][KeyIndex];
					Sequencor.AnimationTrack.Key	NewKey = T.Clone( SourceKey );
					NewKey.TrackTime = KeyTimes1[AnimTrackIndex][KeyIndex];
				}
			}
		}

		private void editTrackColorToolStripMenuItem_Click( object sender, EventArgs e )
		{
			ColorPickerForm	F = new ColorPickerForm( IntervalsColor );
			if ( F.ShowDialog( this ) != DialogResult.OK )
				return;

			IntervalsColor = F.ColorLDR;
		}

		private void deleteIntervalToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( SequencerControl.MessageBox( "Are you sure you want to delete the selected interval ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2 ) != DialogResult.Yes )
				return;

			m_Track.RemoveInterval( m_HoveredInterval );
		}

		#endregion

		#region Parameter Track Menu

		private void contextMenuStripSequence_Opening( object sender, CancelEventArgs e )
		{
			bool	bHasTrack = Track != null;

			moveUpToolStripMenuItem.Enabled = bHasTrack && m_Track.Owner.IndexOf( m_Track ) > 0;
			moveDownToolStripMenuItem.Enabled = bHasTrack && m_Track.Owner.IndexOf( m_Track ) < m_Track.Owner.TracksCount-1;
			moveToFirstToolStripMenuItem.Enabled = bHasTrack && m_Track.Owner.IndexOf( m_Track ) > 0;
			moveToLastToolStripMenuItem.Enabled = bHasTrack && m_Track.Owner.IndexOf( m_Track ) < m_Track.Owner.TracksCount-1;

			copyParameterToolStripMenuItem.Enabled = bHasTrack;
			pasteParameterToolStripMenuItem.Enabled = m_Owner.CanPasteParameter( m_Owner.Sequencer );
		}

		private void moveUpToolStripMenuItem_Click( object sender, EventArgs e )
		{
			m_Track.MoveUp();
		}

		private void moveDownToolStripMenuItem_Click( object sender, EventArgs e )
		{
			m_Track.MoveDown();
		}

		private void moveToFirstToolStripMenuItem_Click( object sender, EventArgs e )
		{
			m_Track.MoveTop();
		}

		private void moveToLastToolStripMenuItem_Click( object sender, EventArgs e )
		{
			m_Track.MoveBottom();
		}

		internal void copyParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( m_Track != null )
				m_Owner.CopyToClipboard( m_Track );
			else
				SequencerControl.MessageBox( "No source track to copy from !", MessageBoxButtons.OK, MessageBoxIcon.Warning );
		}

		private void pasteParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			try
			{
				m_Owner.PasteParameter( m_Owner.Sequencer );
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( "An error occurred while pasting a parameter track from clipboard :\r\n\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		private void renameEmitterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( TrackRename != null )
				TrackRename( this, e );
		}

		private void removeParameterToolStripMenuItem_Click( object sender, EventArgs e )
		{
			m_Owner.UserConfirmTrackDelete( m_Track );
		}

		#endregion

		#endregion

		#region GUI

		private void checkBoxShowTrackAnim_CheckedChanged( object sender, EventArgs e )
		{
			checkBoxShowTrackAnim.BackgroundImage = checkBoxShowTrackAnim.Checked ? Properties.Resources.Fold : Properties.Resources.Unfold;
			AnimationTrackVisible = checkBoxShowTrackAnim.Checked;
		}

		private void checkBoxShowTrackAnim_MouseDown( object sender, MouseEventArgs e )
		{
			OnMouseDown( e );
		}

		private void panelInfos_MouseDown( object sender, MouseEventArgs e )
		{
			OnMouseDown( e );
		}

		private void labelTrack_MouseDown( object sender, MouseEventArgs e )
		{
			OnMouseDown( e );
		}

		private void checkBoxLoop_MouseDown( object sender, MouseEventArgs e )
		{
			OnMouseDown( e );
		}

		private void labelTrack_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			OnMouseDoubleClick( e );
		}

		private void panelInfos_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			OnMouseDoubleClick( e );
		}

		#endregion

		#region Manipulation

		private void trackIntervalPanel_MouseDown( object sender, MouseEventArgs _e )
		{
			OnMouseDown( _e );

			if ( _e.Button != MouseButtons.Left )
				return;

			//////////////////////////////////////////////////////////////////////////
			// Perform interval copy if CopyMode is activated
			if ( m_HoveredInterval != null && IsCopyMode )
			{
				Sequencor.ParameterTrack.Interval	Copy = m_Track.Clone( m_HoveredInterval );
				m_HoveredInterval = Copy;
			}

			//////////////////////////////////////////////////////////////////////////
			// Prepare manipulation data
			m_MouseButtonsDown |= _e.Button;
			m_ManipulatedInterval = m_HoveredInterval;
			m_MouseDownPosition = _e.Location;

			// New selection
			SelectedInterval = m_HoveredInterval;

			trackIntervalPanel.Capture = true;

			if ( m_HoveredInterval != null )
			{	// Backup all intervals
				m_ButtonDownInterval = new IntervalBackup( m_HoveredInterval );
				m_ButtonDownIntervals.Clear();
				foreach ( Sequencor.ParameterTrack.Interval Interval in m_Track.Intervals )
					m_ButtonDownIntervals[Interval] = new IntervalBackup( Interval );
			}
			else
			{	// Forward the event to the time line as we're not modifying any interval here...
				m_Owner.TimeLineControl.SimulateMouseDown( _e );
				return;
			}
		}

		private void trackIntervalPanel_MouseMove( object sender, MouseEventArgs _e )
		{
			//////////////////////////////////////////////////////////////////////////
			// Update tooltip
			m_HoveredInterval = trackIntervalPanel.GetIntervalAt( _e.Location );
// 			if ( m_HoveredInterval != null )
// 				toolTip.SetToolTip( trackIntervalPanel, "[ " + m_HoveredInterval.TimeStart.ToString( "G4" ) + ", " + m_HoveredInterval.TimeEnd.ToString( "G4" ) +
// 									" ] Duration: " + (m_HoveredInterval.TimeEnd - m_HoveredInterval.TimeStart).ToString( "G4" ) + "s" );
// 			else
// 				toolTip.SetToolTip( trackIntervalPanel, "" );

			//////////////////////////////////////////////////////////////////////////
			// Determine manipulation type
			//
			if ( m_MouseButtonsDown == MouseButtons.None )
			{	// Determine which interval can be manipulated
				m_ManipulationMode = MANIPULATION_MODE.NONE;
				if ( m_HoveredInterval == null )
				{
					this.Cursor = this.DefaultCursor;
					return;
				}

				RectangleF	IntervalRectangle = trackIntervalPanel.GetIntervalClientRectangle( m_HoveredInterval );

				// Check for end bound manipulation (end bound is checked first to allow extending intervals that are really small)
				if ( Math.Abs( _e.X - IntervalRectangle.Right ) < 3 )
				{
					m_ManipulationMode = MANIPULATION_MODE.INTERVAL_END_BOUND;
					trackIntervalPanel.Cursor = Cursors.SizeWE;
				}
				// Check for start bound manipulation
				else if ( Math.Abs( _e.X - IntervalRectangle.Left ) < 3 )
				{
					m_ManipulationMode = MANIPULATION_MODE.INTERVAL_START_BOUND;
					trackIntervalPanel.Cursor = Cursors.SizeWE;
				}
				else
				{	// Default move manipulation
					m_ManipulationMode = MANIPULATION_MODE.INTERVAL_MOVE;
					trackIntervalPanel.Cursor = Cursors.Hand;
				}

				return;
			}

			if ( m_MouseButtonsDown != MouseButtons.Left )
				return;	// Can only manipulate with left button...

			//////////////////////////////////////////////////////////////////////////
			// Perform actual manipulation
			//
			switch ( m_ManipulationMode )
			{
				case MANIPULATION_MODE.INTERVAL_MOVE:
				{
					// Compute new interval's positions
					float	fIntervalDeltaPosition = trackIntervalPanel.ClientToSequenceTime( _e.X ) - trackIntervalPanel.ClientToSequenceTime( m_MouseDownPosition.X );
					if ( Math.Abs( fIntervalDeltaPosition ) < 1e-3f )
						return;	// Too small a change...
					float	fNewTimeStart = m_ButtonDownInterval.m_ActualTimeStart + fIntervalDeltaPosition;
					float	fNewTimeEnd = m_ButtonDownInterval.m_ActualTimeEnd + fIntervalDeltaPosition;

					// Modify the position according to anchors
					if ( IsAnchorMode )
					{
						float	fAnchoredTime = -1.0f;
						if ( FindAnchor( m_ManipulatedInterval, fNewTimeStart, out fAnchoredTime ) )
						{	// Move the interval to this anchor
							float	fDeltaAnchor = fAnchoredTime - fNewTimeStart;
							fNewTimeStart += fDeltaAnchor;
							fNewTimeEnd += fDeltaAnchor;
						}
						else if ( FindAnchor( m_ManipulatedInterval, fNewTimeEnd, out fAnchoredTime ) )
						{	// Move the interval to this anchor
							float	fDeltaAnchor = fAnchoredTime - fNewTimeEnd;
							fNewTimeStart += fDeltaAnchor;
							fNewTimeEnd += fDeltaAnchor;
						}
					}

					// Avoid flickering
					trackIntervalPanel.PreventRefresh = true;

					m_ManipulatedInterval.UpdateBothTimes( Math.Max( 0.0f, fNewTimeStart ), Math.Max( 0.0f, fNewTimeEnd ) );

					// Re-evaluate time in this track
					ReEvaluateTrackTime();

					// Refresh
					trackIntervalPanel.PreventRefresh = false;
					trackIntervalPanel.Refresh();

					break;
				}

				case MANIPULATION_MODE.INTERVAL_START_BOUND:
				{
					// Compute new interval's positions
					float	fIntervalDeltaPosition = trackIntervalPanel.ClientToSequenceTime( _e.X ) - trackIntervalPanel.ClientToSequenceTime( m_MouseDownPosition.X );
					if ( Math.Abs( fIntervalDeltaPosition ) < 1e-3f )
						return;	// Too small a change...
					float	fNewTimeStart = m_ButtonDownInterval.m_ActualTimeStart + fIntervalDeltaPosition;

					// Modify the position according to anchors
					if ( IsAnchorMode )
					{
						float	fAnchoredTime = -1.0f;
						if ( FindAnchor( m_ManipulatedInterval, fNewTimeStart, out fAnchoredTime ) )
						{	// Move the interval to this anchor
							fNewTimeStart = fAnchoredTime;
						}
					}

					// Avoid flickering
					trackIntervalPanel.PreventRefresh = true;

					m_ManipulatedInterval.ActualTimeStart = Math.Min( m_ManipulatedInterval.TimeEnd - 1e-3f, fNewTimeStart );

					// Re-evaluate time in this track
					ReEvaluateTrackTime();

					// Refresh
					trackIntervalPanel.PreventRefresh = false;
					trackIntervalPanel.Refresh();

					break;
				}

				case MANIPULATION_MODE.INTERVAL_END_BOUND:
				{
					// Compute new interval's positions
					float	fIntervalDeltaPosition = trackIntervalPanel.ClientToSequenceTime( _e.X ) - trackIntervalPanel.ClientToSequenceTime( m_MouseDownPosition.X );
					if ( Math.Abs( fIntervalDeltaPosition ) < 1e-3f )
						return;	// Too small a change...
					float	fNewTimeEnd = m_ButtonDownInterval.m_ActualTimeEnd + fIntervalDeltaPosition;

					// Modify the position according to anchors
					if ( IsAnchorMode )
					{
						float	fAnchoredTime = -1.0f;
						if ( FindAnchor( m_ManipulatedInterval, fNewTimeEnd, out fAnchoredTime ) )
						{	// Move the interval to this anchor
							fNewTimeEnd = fAnchoredTime;
						}
					}

					// Avoid flickering
					trackIntervalPanel.PreventRefresh = true;

					m_ManipulatedInterval.ActualTimeEnd = Math.Max( m_ManipulatedInterval.TimeStart + 1e-3f, fNewTimeEnd );

					// Re-evaluate time in this track
					ReEvaluateTrackTime();

					// Refresh
					trackIntervalPanel.PreventRefresh = false;
					trackIntervalPanel.Refresh();

					break;
				}

				default:
					m_Owner.TimeLineControl.SimulateMouseMove( _e );
					break;
			}
		}

		/// <summary>
		/// When intervals change in the current track, the time must be reset in absolute so correct current intervals are found once again
		/// </summary>
		protected void	ReEvaluateTrackTime()
		{
			m_Track.SetTime( m_Owner.TimeLineControl.CursorPosition );
		}

		private void trackIntervalPanel_MouseUp( object sender, MouseEventArgs _e )
		{
			m_Owner.TimeLineControl.SimulateMouseUp( _e );

			m_MouseButtonsDown &= ~_e.Button;
			m_ManipulationMode = MANIPULATION_MODE.NONE;
			this.Cursor = DefaultCursor;

			trackIntervalPanel.Capture = m_MouseButtonsDown == MouseButtons.None;
		}

		private void trackIntervalPanel_MouseWheel( object sender, MouseEventArgs _e )
		{
			m_Owner.TimeLineControl.SimulateMouseWheel( _e );
		}

		private void trackIntervalPanel_KeyDown( object sender, KeyEventArgs _e )
		{
			if ( _e.KeyCode == Keys.Left )
			{
				float	fRange = m_Owner.TimeLineControl.VisibleBoundMax - m_Owner.TimeLineControl.VisibleBoundMin;
				float	fNewMin = Math.Max( 0.0f, m_Owner.TimeLineControl.VisibleBoundMin - 0.25f * fRange );
				m_Owner.TimeLineControl.SetVisibleRange( fNewMin, fNewMin + fRange );
			}
			else if ( _e.KeyCode == Keys.Right )
			{
				float	fRange = m_Owner.TimeLineControl.VisibleBoundMax - m_Owner.TimeLineControl.VisibleBoundMin;
//				float	fNewMax = Math.Min( m_Sequencer.SequenceTimeEnd, m_Owner.TimeLineControl.VisibleBoundMax + 0.25f * fRange );
				float	fNewMax = m_Owner.TimeLineControl.VisibleBoundMax + 0.25f * fRange;
				m_Owner.TimeLineControl.SetVisibleRange( fNewMax - fRange, fNewMax );
			}
			else if ( _e.KeyCode == Keys.Delete )
			{
				if ( m_HoveredInterval != null )
					deleteIntervalToolStripMenuItem_Click( trackIntervalPanel, EventArgs.Empty );
			}
			
			if ( _e.KeyCode != Keys.Escape )
				return;

			// Cancel manipulation
			m_ManipulationMode = MANIPULATION_MODE.NONE;
			this.Cursor = DefaultCursor;

			// Avoid flickering
			trackIntervalPanel.PreventRefresh = true;

			// Restore former intervals' data
			foreach ( Sequencor.ParameterTrack.Interval Interval in trackIntervalPanel.Track.Intervals )
				if ( m_ButtonDownIntervals.ContainsKey( Interval ) )
				{
					Interval.ActualTimeStart = m_ButtonDownIntervals[Interval].m_ActualTimeStart;
					Interval.ActualTimeEnd = m_ButtonDownIntervals[Interval].m_ActualTimeEnd;
				}

			// Refresh
			trackIntervalPanel.PreventRefresh = false;
			trackIntervalPanel.Refresh();
		}

		private void trackIntervalPanel_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			if ( m_HoveredInterval != null && IntervalEdit != null )
				IntervalEdit( trackIntervalPanel, m_HoveredInterval );
		}

		#endregion

		#endregion
	}
}
