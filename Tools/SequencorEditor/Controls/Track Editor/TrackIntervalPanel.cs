using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

using SequencorLib;

namespace SequencorEditor
{
	/// <summary>
	/// Draws a list of intervals inside a parameter track
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

		protected class		DrawnInterval : IDisposable
		{
			protected TrackIntervalPanel	m_Owner = null;
			public Sequencor.ParameterTrack.Interval	m_Interval = null;
			public RectangleF				m_Rectangle = RectangleF.Empty;

			/// <summary>
			/// Creates a new interval
			/// </summary>
			/// <param name="_Interval"></param>
			/// <param name="_Rectangle">The rectangle </param>
			public DrawnInterval( TrackIntervalPanel _Owner, Sequencor.ParameterTrack.Interval _Interval, RectangleF _Rectangle )
			{
				m_Owner = _Owner;
				m_Interval = _Interval;
				m_Interval.KeysChanged += new EventHandler( Interval_KeysChanged );
				m_Interval.KeyValueChanged += new Sequencor.ParameterTrack.Interval.KeyValueChangedEventHandler( Interval_KeyValueChanged );
				m_Rectangle = _Rectangle;
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_Interval.KeysChanged -= new EventHandler( Interval_KeysChanged );
				m_Interval.KeyValueChanged -= new Sequencor.ParameterTrack.Interval.KeyValueChangedEventHandler( Interval_KeyValueChanged );
			}

			#endregion

			protected void Interval_KeysChanged( object sender, EventArgs e )
			{
				m_Owner.Invalidate();
			}

			protected void Interval_KeyValueChanged( Sequencor.ParameterTrack.Interval _Sender, Sequencor.AnimationTrack.Key _Key )
			{
				m_Owner.Invalidate();
			}
		};

		#endregion

		#region FIELDS

		protected TrackControl				m_Owner = null;
		protected Sequencor.ParameterTrack	m_Track = null;
		protected bool						m_bSelected = false;
		protected Sequencor.ParameterTrack.Interval	m_SelectedInterval = null;

		// Visual range
		protected float						m_RangeMin = 0.0f;
		protected float						m_RangeMax = 10.0f;

		// Appearance
		protected bool						m_bPreventRefresh = false;
		protected Color						m_SelectedColor = Color.MistyRose;
		protected Color						m_SelectedIntervalColor = Color.IndianRed;
		protected Color						m_CursorTimeColor = Color.ForestGreen;
		protected Pen						m_Pen = null;
		protected Brush						m_Brush = null;
		protected Pen						m_PenSelected = null;
		protected Brush						m_BrushSelected = null;
		protected Pen						m_PenSelectedInterval = null;
		protected Brush						m_BrushSelectedInterval = null;
		protected Pen						m_PenCursorTime = null;

		// Cached list of drawn intervals
		protected List<DrawnInterval>		m_DrawnIntervals = new List<DrawnInterval>();

		#endregion

		#region PROPERTIES

		[Browsable( false )]
		public TrackControl				Owner		{ get { return m_Owner; } set { m_Owner = value; } }

		/// <summary>
		/// Gets or sets the currently edited track
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
					m_Track.IntervalsChanged -= new EventHandler( Track_IntervalsChanged );
				}

				m_Track = value;

				if ( m_Track != null )
				{
					m_Track.IntervalsChanged += new EventHandler( Track_IntervalsChanged );
					Track_IntervalsChanged( m_Track, EventArgs.Empty );
				}

				// Update the GUI
				Enabled = m_Track != null;

				if ( !m_bPreventRefresh )
					Invalidate();
			}
		}

		[Browsable( false )]
		public Sequencor.ParameterTrack.Interval	SelectedInterval
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
				m_PenSelected = new Pen( ComputeBackColor( m_SelectedColor ), 2.0f );
			}
		}

		protected Color	ComputeBackColor( Color _C )
		{
			return Color.FromArgb( (int) (0.8f * _C.R), (int) (0.8f * _C.G), (int) (0.8f * _C.B) );
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
				m_PenSelectedInterval = new Pen( ComputeBackColor( m_SelectedIntervalColor ), 3.0f );
			}
		}

		[Category( "Appearance" )]
		public Color		CursorTimeColor
		{
			get { return m_CursorTimeColor; }
			set
			{
				m_CursorTimeColor = value;

				// Rebuild brush and pens
				if ( m_PenCursorTime != null )
					m_PenCursorTime.Dispose();
				m_PenCursorTime = new Pen( m_CursorTimeColor, 1.0f );
				m_PenCursorTime.DashStyle = DashStyle.Dash;
			}
		}

		[Category( "Custom Paint" )]
		public event CustomIntervalPaintEventHandler	CustomIntervalPaint;

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
			m_Pen = new Pen( ComputeBackColor( this.ForeColor ), 2.0f );

			SelectedColor = SelectedColor;	// Should build the pens & brushes
			SelectedIntervalColor = SelectedIntervalColor;		
			CursorTimeColor = CursorTimeColor;
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
// 			if ( m.Msg == (int) Crownwood.DotNetMagic.Win32.Msgs.WM_KEYDOWN && KeyDown != null )
// 				KeyDown( this, new KeyEventArgs( (Keys) m.WParam ) );

			base.WndProc( ref m );
		}

		protected override void OnForeColorChanged( EventArgs e )
		{
			base.OnForeColorChanged( e );

			// Create a new brush
			m_Brush.Dispose();
			m_Pen.Dispose();
			m_Brush = new SolidBrush( this.ForeColor );
			m_Pen = new Pen( ComputeBackColor( this.ForeColor ), 2.0f );
		}

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );
			if ( !m_bPreventRefresh )
				Refresh();
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			if ( m_RangeMax - m_RangeMin < 1e-3f )
				return;	// Can't trace invalid range!
			if ( m_Track == null )
				return;

			Sequencor.ParameterTrack.Interval[]	Intervals = m_Track.Intervals;

			// Clear existing intervals
			foreach ( DrawnInterval DI in m_DrawnIntervals )
				DI.Dispose();
			m_DrawnIntervals.Clear();

			// Draw the intervals...
			Pen		P = m_bSelected ? m_PenSelected : m_Pen;
			Brush	B = m_bSelected ? m_BrushSelected : m_Brush;

			foreach ( Sequencor.ParameterTrack.Interval Interval in Intervals )
			{
				if ( Interval.TimeEnd < m_RangeMin || Interval.TimeStart > m_RangeMax )
					continue;	// Out of range...

				float	fIntervalPosStart = SequenceTimeToClient( Interval.TimeStart );
				float	fIntervalPosEnd = SequenceTimeToClient( Interval.TimeEnd );

				RectangleF	IntervalRect = new RectangleF( fIntervalPosStart, (1.0f - INTERVAL_HEIGHT_RATIO) * Height, Math.Max( 2.0f, fIntervalPosEnd - fIntervalPosStart ), INTERVAL_HEIGHT_RATIO * Height );

				// Add this rectangle to the list of drawn intervals
				// (we're manipulating using screen space's drawn items)
				m_DrawnIntervals.Add( new DrawnInterval( this, Interval, IntervalRect ) );

				// Draw the background rectangle
				e.Graphics.FillRectangle( Interval == SelectedInterval ? m_BrushSelectedInterval : B, IntervalRect );

				// Call custom paint
				if ( CustomIntervalPaint != null )
					CustomIntervalPaint( this, e.Graphics, IntervalRect );

				// Draw the keys
				foreach ( Sequencor.AnimationTrack.Key K in Interval[0].Keys )
				{
					float	ClientPos = SequenceTimeToClient( K.TrackTime );
					e.Graphics.DrawImage( Properties.Resources.Key, ClientPos-4.0f, IntervalRect.Y + IntervalRect.Height/2 - 4.0f, 8.0f, 8.0f );
				}

				// Draw the border rectangle
				e.Graphics.DrawRectangle( Interval == SelectedInterval ? m_PenSelectedInterval : P, IntervalRect.X, IntervalRect.Y, IntervalRect.Width, IntervalRect.Height );
			}

			// Draw cursor time
			if ( m_Owner.Owner.ShowCursorTime )
			{
				float	CursorX = SequenceTimeToClient( m_Owner.Owner.TimeLineControl.CursorPosition );
				e.Graphics.DrawLine( m_PenCursorTime, CursorX, 0, CursorX, Height );
			}

			if ( !Enabled )
			{
				// Draw a disabled look
				HatchBrush DisableBrush = new HatchBrush( HatchStyle.Percent50, Color.Black, Color.Transparent );
				e.Graphics.FillRectangle( DisableBrush, ClientRectangle );
				DisableBrush.Dispose();
			}

			// Base call so we trigger the Paint event for some stoopid user... Oh sorry ! It was you ?
			base.OnPaint( e );
		}

		#endregion

		/// <summary>
		/// Attempts to retrieve the interval under the specified position
		/// </summary>
		/// <param name="_ClientPosition">The position in CLIENT space</param>
		/// <returns>The interval at this position</returns>
		public Sequencor.ParameterTrack.Interval	GetIntervalAt( Point _ClientPosition )
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
		public RectangleF				GetIntervalClientRectangle( Sequencor.ParameterTrack.Interval _Interval )
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

		protected DrawnInterval			FindDrawnInterval( Sequencor.ParameterTrack.Interval _Interval )
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

		protected void Track_IntervalsChanged( object sender, EventArgs e )
		{
			Invalidate();
		}

// 		protected void	Track_Invalidated( IData _InvalidatedData )
// 		{
// 			Track = null;
// 		}
// 
// 		protected void	Track_EnabledChanged( IDataEmitterSequenceTrack _Sender )
// 		{
// 			Enabled = Track.Enabled;
// 
// 			if ( !m_bPreventRefresh )
// 				Invalidate();
// 		}
// 
// 		protected void	Track_LoopChanged( IDataEmitterSequenceTrack _Sender )
// 		{
// 			if ( !m_bPreventRefresh )
// 				Invalidate();
// 		}
// 
// 		protected void	Track_Updated( IDataEmitterSequenceTrack _Sender )
// 		{
// 			if ( !m_bPreventRefresh )
// 				Invalidate();	
// 		}
// 
// 		protected void	Track_IntervalUpdated( IDataEmitterSequenceTrack _Sender, Sequencor.ParameterTrack.Interval _Interval )
// 		{
// 			if ( !m_bPreventRefresh )
// 				Invalidate();
// 		}
// 
// 		protected void	Particles_SequenceTimeEndChanged( IDataParticles _Sender )
// 		{
// 			if ( !m_bPreventRefresh )
// 				Invalidate();
// 		}

		#endregion
	}
}
