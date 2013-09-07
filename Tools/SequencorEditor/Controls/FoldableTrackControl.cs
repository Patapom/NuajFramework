using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SequencorLib;

namespace SequencorEditor
{
	public partial class FoldableTrackControl : UserControl
	{
		#region FIELDS

		protected SequencerControl	m_Owner = null;
		protected Sequencor.ParameterTrack	m_Track = null;
		protected bool				m_bSelected = false;
		protected Sequencor.ParameterTrack.Interval	m_SelectedInterval = null;

		#endregion

		#region PROPERTIES

		public SequencerControl			Owner
		{
			get { return m_Owner; }
			set
			{
				m_Owner = value;
				trackControl.Owner = value;
				animationEditorControl.Owner = value;
			}
		}

		/// <summary>
		/// Gets or sets the track this control is observing
		/// </summary>
		public Sequencor.ParameterTrack	Track
		{
			get { return m_Track; }
			set
			{
				if ( value == m_Track )
					return;

				m_Track = value;
				trackControl.Track = value;
				animationEditorControl.Track = value;
			}
		}

		/// <summary>
		/// Gets or sets the selected interval in that track
		/// </summary>
		public Sequencor.ParameterTrack.Interval	SelectedInterval
		{
			get { return m_SelectedInterval; }
			set
			{
				if ( value == m_SelectedInterval )
					return;

				m_SelectedInterval = value;
				trackControl.SelectedInterval = value;
				animationEditorControl.SelectedInterval = value;

				if ( SelectedIntervalChanged != null )
					SelectedIntervalChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the visible state of that control
		/// </summary>
		public bool						Selected
		{
			get { return m_bSelected; }
			set
			{
				if ( value == m_bSelected )
					return;

				m_bSelected = value;
				trackControl.Selected = value;
				BackColor = value ? Color.Red : SystemColors.Control;

				// Notify
				if ( SelectedChanged != null )
					SelectedChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets the child TrackControl used to edit animation intervals
		/// </summary>
		public TrackControl			TrackControl
		{
			get { return trackControl; }
		}

		/// <summary>
		/// Gets the child AnimationEditorControl used to edit animation keys
		/// </summary>
		public AnimationEditorControl	AnimationEditorControl
		{
			get { return animationEditorControl; }
		}

		public event EventHandler	SelectedIntervalChanged;
		public event EventHandler	TrackRename;
		public event EventHandler	SelectedChanged;

		#endregion

		#region METHODS

		public FoldableTrackControl()
		{
			InitializeComponent();
		}

		public void		SetRange( float _RangeMin, float _RangeMax )
		{
			trackControl.SetRange( _RangeMin, _RangeMax );
			animationEditorControl.SetRange( _RangeMin, _RangeMax );
		}

		/// <summary>
		/// Gets the vertical range of the animation track editor
		/// </summary>
		/// <returns></returns>
		public float	GetAnimationVerticalRangeMin()
		{
			return animationEditorControl.GetAnimationVerticalRangeMin();
		}

		/// <summary>
		/// Gets the vertical range of the animation track editor
		/// </summary>
		/// <returns></returns>
		public float	GetAnimationVerticalRangeMax()
		{
			return animationEditorControl.GetAnimationVerticalRangeMax();
		}

		/// <summary>
		/// Sets the vertical ranges of the animation track editor
		/// </summary>
		/// <returns></returns>
		public void	SetAnimationVerticalRanges( float _RangeMin, float _RangeMax )
		{
			animationEditorControl.SetAnimationVerticalRanges( _RangeMin, _RangeMax );
		}

		/// <summary>
		/// Converts a client position into a sequence time
		/// </summary>
		/// <param name="_fClientPosition">The CLIENT SPACE position</param>
		/// <returns>The equivalent sequence time</returns>
		public float	ClientToSequenceTime( float _fClientPosition )
		{
			return	trackControl.trackIntervalPanel.ClientToSequenceTime( _fClientPosition );
		}

		/// <summary>
		/// Converts a sequence time into a client position
		/// </summary>
		/// <param name="_fSequenceTime">The sequence time</param>
		/// <returns>The equivalent CLIENT SPACE position</returns>
		public float					SequenceTimeToClient( float _fSequenceTime )
		{
			return	trackControl.trackIntervalPanel.SequenceTimeToClient( _fSequenceTime );
		}

		#endregion

		#region EVENT HANDLERS

		private void trackControl_AnimationTrackVisibleStateChanged( object sender, EventArgs e )
		{
			animationEditorControl.Visible = trackControl.AnimationTrackVisible;
		}

		private void animationEditorControl_Exit( object sender, EventArgs e )
		{
			trackControl.AnimationTrackVisible = false;
		}

		private void animationEditorControl_SelectedIntervalChanged( object sender, EventArgs e )
		{
			SelectedInterval = animationEditorControl.SelectedInterval;
		}

		private void animationEditorControl_SelectedKeyChanged( object sender, EventArgs e )
		{

		}

		private void trackControl_IntervalEdit( TrackIntervalPanel _Sender, Sequencor.ParameterTrack.Interval _Interval )
		{
			trackControl.AnimationTrackVisible = true;
		}

		private void trackControl_SelectedIntervalChanged( object sender, EventArgs e )
		{
			SelectedInterval = trackControl.SelectedInterval;
		}

		private void trackControl_TrackRename( object sender, EventArgs e )
		{
			if ( TrackRename != null )
				TrackRename( this, e );
		}

		private void trackControl_MouseDown( object sender, MouseEventArgs e )
		{
			Selected = true;
		}

		#endregion
	}
}
