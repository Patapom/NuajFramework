using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

using Components.Core;

namespace Components.Editors.VFX.v2
{
	/// <summary>
	/// Draws a list of intervals inside a panel
	/// </summary>
	public partial class TrackIntervalPanel : Panel
	{
		#region CONSTANTS

		private const int	LOOP_GHOST_INTERVALS_COUNT	= 8;
		private const int	GHOST_INTERVAL_START_ALPHA	= 100;
		private const int	GHOST_INTERVAL_END_ALPHA	= 32;

		private const float	INTERVAL_HEIGHT_RATIO		= 0.75f;
		private const float	GHOST_INTERVAL_HEIGHT_RATIO	= 0.70f;

		#endregion

		#region NESTED TYPES

		public delegate void	CustomIntervalPaintEventHandler( TrackIntervalPanel _Sender, Graphics _Graphics, RectangleF _IntervalRectangle );

		protected class		DrawnInterval
		{
			public IEmitterTrackInterval	m_Interval = null;
			public RectangleF				m_Rectangle = RectangleF.Empty;

			public DrawnInterval( IEmitterTrackInterval _Interval, RectangleF _Rectangle )
			{
				m_Interval = _Interval;
				m_Rectangle = _Rectangle;
			}
		};

		#endregion

		#region FIELDS

		protected IDataEmitterSequenceTrack	m_Track = null;
		protected bool						m_bSelected = false;
		protected IEmitterTrackInterval		m_SelectedInterval = null;

		// Visual range
		protected float		m_RangeMin = 0.0f;
		protected float		m_RangeMax = 10.0f;

		// Appearance
		protected bool		m_bPreventRefresh = false;
		protected Color		m_SelectedColor = Color.MistyRose;
		protected Color		m_SelectedIntervalColor = Color.IndianRed;
		protected Pen		m_Pen = null;
		protected Brush		m_Brush = null;
		protected Pen		m_PenSelected = null;
		protected Brush		m_BrushSelected = null;
		protected Pen		m_PenSelectedInterval = null;
		protected Brush		m_BrushSelectedInterval = null;

		// Cached list of drawn intervals
		protected List<DrawnInterval>	m_DrawnIntervals = new List<DrawnInterval>();

		#endregion

		#region PROPERTIES

		[Browsable( false )]
		public IDataEmitterSequenceTrack	Track
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
					m_Track.Invalidated -= new DataInvalidatedEventHandler( Track_Invalidated );
					m_Track.EnabledChanged -= new SequenceTrackEventHandler( Track_EnabledChanged );
					m_Track.LoopChanged -= new SequenceTrackEventHandler( Track_LoopChanged );
					m_Track.IntervalUpdated -= new TrackIntervalUpdatedEventHandler( Track_IntervalUpdated );
					m_Track.Updated -= new SequenceTrackEventHandler( Track_Updated );
					m_Track.Owner.Owner.SequenceEndTimeChanged -= new ParticlesEventHandler( Particles_SequenceEndTimeChanged );
				}

				m_Track = value;

				if ( m_Track != null )
				{
					m_Track.Invalidated += new DataInvalidatedEventHandler( Track_Invalidated );
					m_Track.EnabledChanged += new SequenceTrackEventHandler( Track_EnabledChanged );
					m_Track.LoopChanged += new SequenceTrackEventHandler( Track_LoopChanged );
					m_Track.IntervalUpdated += new TrackIntervalUpdatedEventHandler( Track_IntervalUpdated );
					m_Track.Updated += new SequenceTrackEventHandler( Track_Updated );
					m_Track.Owner.Owner.SequenceEndTimeChanged += new ParticlesEventHandler( Particles_SequenceEndTimeChanged );
				}

				// Update the GUI
				Enabled = m_Track != null;

				if ( !m_bPreventRefresh )
					Invalidate();
			}
		}

		[Browsable( false )]
		public IEmitterTrackInterval	SelectedInterval
		{
			get { return m_SelectedInterval; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_SelectedInterval )
					return;

				m_SelectedInterval = value;

				if ( !m_bPreventRefresh )
					Refresh();
			}
		}

		[Browsable( false )]
		public bool			Selected
		{
			get { return m_bSelected; }
			set
			{
				m_bSelected = value;

				if ( !m_bPreventRefresh )
					Invalidate();
			}
		}

		[Category( "Appearance" )]
		public Color		SelectedColor
		{
			get { return m_SelectedColor; }
			set
			{
				m_SelectedColor = value;

				// Rebuild brush and pens
				if ( m_BrushSelected != null )
					m_BrushSelected.Dispose();
				if ( m_PenSelected != null )
					m_PenSelected.Dispose();
				m_BrushSelected = new SolidBrush( m_SelectedColor );
				m_PenSelected = new Pen( Crownwood.DotNetMagic.Common.ColorHelper.TabBackgroundFromBaseColor( m_SelectedColor ), 2.0f );
			}
		}

		[Category( "Appearance" )]
		public Color		SelectedIntervalColor
		{
			get { return m_SelectedIntervalColor; }
			set
			{
				m_SelectedIntervalColor = value;

				// Rebuild brush and pens
				if ( m_BrushSelectedInterval != null )
					m_BrushSelectedInterval.Dispose();
				if ( m_PenSelectedInterval != null )
					m_PenSelectedInterval.Dispose();
				m_BrushSelectedInterval = new SolidBrush( m_SelectedIntervalColor );
				m_PenSelectedInterval = new Pen( Crownwood.DotNetMagic.Common.ColorHelper.TabBackgroundFromBaseColor( m_SelectedIntervalColor ), 3.0f );
			}
		}

		[Category( "Custom Paint" )]
		public event CustomIntervalPaintEventHandler	CustomIntervalPaint;

		[Category( "Key" )]
		[Browsable( true )]
		public new event KeyEventHandler	KeyDown;

		public bool		PreventRefresh
		{
			get { return m_bPreventRefresh; }
			set { m_bPreventRefresh = value; }
		}

		#endregion

		#region METHODS

		public	TrackIntervalPanel()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			// Initialize the graduation pens with our forecolor
			m_Brush = new SolidBrush( this.ForeColor );
			m_Pen = new Pen( Crownwood.DotNetMagic.Common.ColorHelper.TabBackgroundFromBaseColor( this.ForeColor ), 2.0f );

			SelectedColor = SelectedColor;	// Should build the pens & brushes
			SelectedIntervalColor = SelectedIntervalColor;		
		}

		protected override void Dispose( bool disposing )
		{
			// Dispose of the objects
			m_Pen.Dispose();
			m_Brush.Dispose();
			m_PenSelected.Dispose();
			m_BrushSelected.Dispose();
			m_PenSelectedInterval.Dispose();
			m_BrushSelectedInterval.Dispose();

			base.Dispose( disposing );
		}

		/// <summary>
		/// Sets both min and max range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetRange( float _RangeMin, float _RangeMax )
		{
			m_RangeMin = _RangeMin;
			m_RangeMax = _RangeMax;

			if ( !m_bPreventRefresh )
				Refresh();
		}

		#region Control members

		protected override void OnMouseDown( MouseEventArgs e )
		{
			Focus();

			base.OnMouseDown( e );
		}

		protected override void WndProc( ref Message m )
		{
			if ( m.Msg == (int) Crownwood.DotNetMagic.Win32.Msgs.WM_KEYDOWN && KeyDown != null )
				KeyDown( this, new KeyEventArgs( (Keys) m.WParam ) );

			base.WndProc( ref m );
		}

		protected override void OnForeColorChanged( EventArgs e )
		{
			base.OnForeColorChanged( e );

			// Create a new brush
			m_Brush.Dispose();
			m_Pen.Dispose();
			m_Brush = new SolidBrush( this.ForeColor );
			m_Pen = new Pen( Crownwood.DotNetMagic.Common.ColorHelper.TabBackgroundFromBaseColor( this.ForeColor ), 2.0f );
		}

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );

			if ( !m_bPreventRefresh )
				Refresh();
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			m_DrawnIntervals.Clear();

			if ( m_RangeMax - m_RangeMin < 1e-3f )
				return;	// Can't trace invalid range!

			if ( m_Track == null )
				return;

			IEmitterTrackInterval[]	Intervals = m_Track.Intervals.ToArray();

			// Draw the intervals...
			Pen		P = m_bSelected ? m_PenSelected : m_Pen;
			Brush	B = m_bSelected ? m_BrushSelected : m_Brush;

			foreach ( IEmitterTrackInterval Interval in Intervals )
			{
				if ( Interval.EndTime < m_RangeMin || Interval.StartTime > m_RangeMax )
					continue;	// Out of range...

				float	fIntervalPosStart = SequenceTimeToClient( Interval.StartTime );
				float	fIntervalPosEnd = SequenceTimeToClient( Interval.EndTime );

				RectangleF	IntervalRect = new RectangleF( fIntervalPosStart, (1.0f - INTERVAL_HEIGHT_RATIO) * Height, Math.Max( 2.0f, fIntervalPosEnd - fIntervalPosStart ), INTERVAL_HEIGHT_RATIO * Height );

				// Add this rectangle to the list of drawn intervals
				m_DrawnIntervals.Add( new DrawnInterval( Interval, IntervalRect ) );

				// Draw the background rectangle
				e.Graphics.FillRectangle( Interval == SelectedInterval ? m_BrushSelectedInterval : B, IntervalRect );

				// Call custom paint
				if ( CustomIntervalPaint != null )
					CustomIntervalPaint( this, e.Graphics, IntervalRect );

				// Draw the border rectangle
				e.Graphics.DrawRectangle( Interval == SelectedInterval ? m_PenSelectedInterval : P, IntervalRect.X, IntervalRect.Y, IntervalRect.Width, IntervalRect.Height );
			}

			// Draw the looping intervals
			if ( m_Track.Loop && Intervals.Length > 0 )
			{	// Draw looping intervals

				// Retrieve the last interval's "delay"
				IEmitterTrackInterval	LastInterval = Intervals[Intervals.Length-1];
				float	fDelay = LastInterval.StartTime - (Intervals.Length > 1 ? Intervals[Intervals.Length-2].EndTime : 0.0f);
				float	fLoopTotalTime = fDelay + LastInterval.EndTime - LastInterval.StartTime;

				Pen			SourcePen = LastInterval == SelectedInterval ? m_PenSelectedInterval : P;
				SolidBrush	SourceBrush = (LastInterval == SelectedInterval ? m_BrushSelectedInterval : B) as SolidBrush;

				// Draw several "ghost" intervals
				for ( int LoopingIntervalIndex=0; LoopingIntervalIndex < LOOP_GHOST_INTERVALS_COUNT; LoopingIntervalIndex++ )
				{
					float	fIntervalStart = LastInterval.EndTime + fLoopTotalTime * LoopingIntervalIndex + fDelay;
					float	fIntervalEnd = LoopingIntervalIndex == LOOP_GHOST_INTERVALS_COUNT-1 ? +float.MaxValue : LastInterval.EndTime + fLoopTotalTime * (LoopingIntervalIndex+1);

					if ( fIntervalEnd < m_RangeMin || fIntervalStart > m_RangeMax )
						continue;	// Out of range...

					int		Alpha = GHOST_INTERVAL_START_ALPHA + (GHOST_INTERVAL_END_ALPHA - GHOST_INTERVAL_START_ALPHA) * LoopingIntervalIndex / LOOP_GHOST_INTERVALS_COUNT;

					Pen		TempPen = new Pen( Color.FromArgb( Alpha, SourcePen.Color ), 1.0f );
					Brush	TempBrush = new SolidBrush( Color.FromArgb( Alpha, SourceBrush.Color ) );

					float	fIntervalPosStart = Math.Max( 0.0f, SequenceTimeToClient( fIntervalStart ) );
					float	fIntervalPosEnd = LoopingIntervalIndex == LOOP_GHOST_INTERVALS_COUNT-1 ? Width : SequenceTimeToClient( fIntervalEnd );

					RectangleF	IntervalRect = new RectangleF( fIntervalPosStart, (1.0f - GHOST_INTERVAL_HEIGHT_RATIO) * Height, Math.Max( 0.0f, fIntervalPosEnd - fIntervalPosStart ), GHOST_INTERVAL_HEIGHT_RATIO * Height );

					// Draw the background rectangle
					e.Graphics.FillRectangle( TempBrush, IntervalRect );

					// Draw the border rectangle
					e.Graphics.DrawRectangle( TempPen, IntervalRect.X, IntervalRect.Y, IntervalRect.Width, IntervalRect.Height );

					TempBrush.Dispose();
					TempPen.Dispose();
				}
			}

			// Draw sequence end time if visible
			float	fSequenceEndTime = m_Track.Owner.Owner.SequenceEndTime;
			if ( fSequenceEndTime < m_RangeMax )
			{
				float	fEndTimeClientPosition = Math.Max( 0, SequenceTimeToClient( fSequenceEndTime ) );

				// Draw the sequence EndTime boundary itself
				e.Graphics.FillRectangle( Brushes.Red, fEndTimeClientPosition, 0.0f, 5.0f, Height );

				// Draw the time after EndTime as a shaded rectangle
				HatchBrush EndTimeBrush = new HatchBrush( HatchStyle.Percent50, Color.Red, Color.Transparent );
				e.Graphics.FillRectangle( EndTimeBrush, fEndTimeClientPosition, 0.0f, Width - fEndTimeClientPosition, Height );
				EndTimeBrush.Dispose();
			}

			if ( !Enabled )
			{
				// Draw a disabled look
				HatchBrush DisableBrush = new HatchBrush( HatchStyle.Percent50, Color.Black, Color.Transparent );
				e.Graphics.FillRectangle( DisableBrush, ClientRectangle );
				DisableBrush.Dispose();
			}

			// Call base so we trigger the Paint event for some user...
			base.OnPaint( e );
		}

		#endregion

		/// <summary>
		/// Attempts to retrieve the interval under the specified position
		/// </summary>
		/// <param name="_ClientPosition">The position in CLIENT space</param>
		/// <returns>The interval at this position</returns>
		public IEmitterTrackInterval	GetIntervalAt( Point _ClientPosition )
		{
			foreach ( DrawnInterval DI in m_DrawnIntervals )
				if ( DI.m_Rectangle.Contains( _ClientPosition.X, _ClientPosition.Y ) )
					return	DI.m_Interval;

			return	null;
		}

		/// <summary>
		/// Retrieves the client rectangle representing the requested interval
		/// </summary>
		/// <param name="_Interval">The interval to retrieve the rectangle of</param>
		/// <returns>The interval's CLIENT SPACE rectangle</returns>
		public RectangleF				GetIntervalClientRectangle( IEmitterTrackInterval _Interval )
		{
			DrawnInterval	DI = FindDrawnInterval( _Interval );

			return	DI != null ? DI.m_Rectangle : RectangleF.Empty;
		}

		/// <summary>
		/// Converts a client position into a sequence time
		/// </summary>
		/// <param name="_fClientPosition">The CLIENT SPACE position</param>
		/// <returns>The equivalent sequence time</returns>
		public float					ClientToSequenceTime( float _fClientPosition )
		{
			return	m_RangeMin + (m_RangeMax - m_RangeMin) * _fClientPosition / Width;
		}

		/// <summary>
		/// Converts a sequence time into a client position
		/// </summary>
		/// <param name="_fSequenceTime">The sequence time</param>
		/// <returns>The equivalent CLIENT SPACE position</returns>
		public float					SequenceTimeToClient( float _fSequenceTime )
		{
			return	(_fSequenceTime - m_RangeMin) * Width / (m_RangeMax - m_RangeMin);
		}

		protected DrawnInterval			FindDrawnInterval( IEmitterTrackInterval _Interval )
		{
			if ( _Interval == null )
				return	null;

			foreach ( DrawnInterval DI in m_DrawnIntervals )
				if ( DI.m_Interval == _Interval )
					return	DI;

			return	null;
		}

		#endregion

		#region EVENT HANDLERS

		protected void	Track_Invalidated( IData _InvalidatedData )
		{
			Track = null;
		}

		protected void	Track_EnabledChanged( IDataEmitterSequenceTrack _Sender )
		{
			Enabled = Track.Enabled;

			if ( !m_bPreventRefresh )
				Invalidate();
		}

		protected void	Track_LoopChanged( IDataEmitterSequenceTrack _Sender )
		{
			if ( !m_bPreventRefresh )
				Invalidate();
		}

		protected void	Track_Updated( IDataEmitterSequenceTrack _Sender )
		{
			if ( !m_bPreventRefresh )
				Invalidate();	
		}

		protected void	Track_IntervalUpdated( IDataEmitterSequenceTrack _Sender, IEmitterTrackInterval _Interval )
		{
			if ( !m_bPreventRefresh )
				Invalidate();
		}

		protected void	Particles_SequenceEndTimeChanged( IDataParticles _Sender )
		{
			if ( !m_bPreventRefresh )
				Invalidate();
		}

		#endregion
	}
}
