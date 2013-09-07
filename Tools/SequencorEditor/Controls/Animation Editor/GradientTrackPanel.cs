using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using SequencorLib;

namespace SequencorEditor
{
	public partial class GradientTrackPanel : Panel
	{
		#region CONSTANTS

		protected const int		GRADIENT_SUBDIVISIONS_COUNT = 12;
		protected const float	COLOR_KEY_WIDTH = 3.0f;
		protected const float	COLOR_KEY_HEIGHT = 10.0f;

		#endregion

		#region NESTED TYPES

		protected class DrawnInterval : IDisposable
		{
			#region FIELDS

			protected GradientTrackPanel	m_Owner = null;
			protected Sequencor.ParameterTrack.Interval	m_Interval = null;

			protected ColorBlend			m_ColorBlend = new ColorBlend();
			protected RectangleF			m_Rectangle;

			#endregion

			#region PROPERTIES

			public Sequencor.ParameterTrack.Interval	Interval	{ get { return m_Interval; } }

			#endregion

			#region METHODS

			public DrawnInterval( GradientTrackPanel _Owner, Sequencor.ParameterTrack.Interval _Interval )
			{
				m_Owner = _Owner;
				m_Interval = _Interval;
				m_Interval.KeysChanged += new EventHandler( Interval_KeysChanged );
				m_Interval.KeyValueChanged += new Sequencor.ParameterTrack.Interval.KeyValueChangedEventHandler( Interval_KeyValueChanged );
				m_Interval.ActualTimeStartChanged += new EventHandler( Interval_ActualTimeStartChanged );
				m_Interval.ActualTimeEndChanged += new EventHandler( Interval_ActualTimeEndChanged );

				RebuildGradient();
			}

			public void		Draw( Graphics G )
			{
				if ( m_Interval.TimeStart >= m_Owner.m_RangeMax || m_Interval.TimeEnd <= m_Owner.m_RangeMin )
					return;	// Out of range...

				float	X0 = m_Owner.SequenceTimeToClient( m_Interval.TimeStart );
				float	X1 = m_Owner.SequenceTimeToClient( m_Interval.TimeEnd );
				X1 = Math.Max( X0+2.0f, X1 );
// 				if ( X1-X0 < 1.0f )
// 					return;	// Crashes if empty!

				int			Height = m_Owner.Height - 2;
				m_Rectangle = new RectangleF( X0, 0, 1+X1-X0, Height );

				using ( LinearGradientBrush	Gradient = new LinearGradientBrush( new PointF( X0, Height/2 ), new PointF( X1, Height/2), Color.Black, Color.Black ) )
				{
					Gradient.InterpolationColors = m_ColorBlend;
					G.FillRectangle( Gradient, m_Rectangle );
				}

				// Draw little keys
				foreach ( Sequencor.AnimationTrack.Key K in m_Interval[0].Keys )
				{
					float	X = m_Owner.SequenceTimeToClient( K.TrackTime );
					G.FillRectangle( Brushes.Gold, X-0.5f*COLOR_KEY_WIDTH, Height-COLOR_KEY_HEIGHT, COLOR_KEY_WIDTH, COLOR_KEY_HEIGHT );
				}
			}

			/// <summary>
			/// Rebuild the gradients color blend
			/// </summary>
			protected void	RebuildGradient()
			{
				int	ReferenceKeysCount = m_Interval[0].KeysCount;
				foreach ( Sequencor.AnimationTrackFloat T in m_Interval.AnimationTracks )
					if ( T.KeysCount == 0 || T.KeysCount != ReferenceKeysCount )
					{	// No key on that track (this occurs when the interval creates its very first key and keys are added track by track)
						// Or not the same amount of keys on each track (this occurs when we delete keys as we delete them track by track)
						m_ColorBlend.Colors = new Color[2] { Color.Black, Color.Black };
						m_ColorBlend.Positions = new float[2] { 0.0f, 1.0f };
						return;
					}

				// Retrieve track infos as colors
				int			GradientPointsCount = 1 + (m_Interval[0].KeysCount-1) * GRADIENT_SUBDIVISIONS_COUNT + 2;
				float[]		Times = new float[GradientPointsCount];
				float[,]	Colors = new float[GradientPointsCount,3];

				for ( int AnimTrackIndex=0; AnimTrackIndex < Math.Min( 3, m_Interval.AnimationTracksCount ); AnimTrackIndex++ )
				{
					Sequencor.AnimationTrackFloat	T = m_Interval[AnimTrackIndex] as Sequencor.AnimationTrackFloat;
					if ( m_Interval[0].KeysCount == 0 )
					{	// No key on that track (this occurs when the interval creates its very first key and keys are added track by track)
						m_ColorBlend.Colors = new Color[2] { Color.Black, Color.Black };
						m_ColorBlend.Positions = new float[2] { 0.0f, 1.0f };
						return;
					}

					int		GradientPointIndex = 0;

					// Compute start value
					Times[GradientPointIndex] = 0.0f;
					Colors[GradientPointIndex++,AnimTrackIndex] = (T[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value;

					// Compute key intervals
					for ( int KeyIndex=0; KeyIndex < T.KeysCount-1; KeyIndex++ )
					{
						Sequencor.AnimationTrackFloat.KeyFloat	K0 = T[KeyIndex] as Sequencor.AnimationTrackFloat.KeyFloat;
						Sequencor.AnimationTrackFloat.KeyFloat	K1 = T[KeyIndex+1] as Sequencor.AnimationTrackFloat.KeyFloat;

						for ( int DivIndex=0; DivIndex < GRADIENT_SUBDIVISIONS_COUNT; DivIndex++ )
						{
							Times[GradientPointIndex] = K0.NormalizedTime + (K1.NormalizedTime - K0.NormalizedTime) * DivIndex / GRADIENT_SUBDIVISIONS_COUNT;
							Colors[GradientPointIndex,AnimTrackIndex] = T.ImmediateEval( m_Interval.TimeStart + Times[GradientPointIndex] * m_Interval.Duration );
							GradientPointIndex++;
						}
					}

					// Compute end values
					Times[GradientPointIndex] = T[T.KeysCount-1].NormalizedTime;
					Colors[GradientPointIndex++,AnimTrackIndex] = (T[T.KeysCount-1] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					Times[GradientPointIndex] = 1.0f;
					Colors[GradientPointIndex++,AnimTrackIndex] = (T[T.KeysCount-1] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
				}

				// Build LDR colors instead
				Color[]	GradientColors = new Color[GradientPointsCount];
				for ( int ColorIndex=0; ColorIndex < GradientPointsCount; ColorIndex++ )
				{
					float	R = Colors[ColorIndex,0];
					float	G = Colors[ColorIndex,1];
					float	B = Colors[ColorIndex,2];
					// Patch for FLOAT1
					if ( m_Interval.ParentTrack.Type == Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT )
						G = B = R;

					GradientColors[ColorIndex] = Color.FromArgb( Math.Min( 255, Math.Max( 0, (int) (255.0f * R) ) ), Math.Min( 255, Math.Max( 0, (int) (255.0f * G) ) ), Math.Min( 255, Math.Max( 0, (int) (255.0f * B) ) ) );
				}

				// Rebuild color blend
				m_ColorBlend.Positions = Times;
				m_ColorBlend.Colors = GradientColors;
			}

			public bool		Contains( float _X, float _Y )
			{
				return m_Rectangle.Contains( _X, _Y );
			}

			/// <summary>
			/// Attempts to retrieve the key under the specified position
			/// </summary>
			/// <param name="_ClientPosition">The position in CLIENT space</param>
			/// <returns>The key at this position</returns>
			public Sequencor.AnimationTrack.Key		GetKeyAt( Point _ClientPosition )
			{
				int			Height = m_Owner.Height;
				if ( _ClientPosition.Y < Height-COLOR_KEY_HEIGHT )
					return null;

				foreach ( Sequencor.AnimationTrack.Key K in m_Interval[0].Keys )
				{
					float	X = m_Owner.SequenceTimeToClient( K.TrackTime );
					if ( Math.Abs( _ClientPosition.X - X ) < 0.5f * COLOR_KEY_WIDTH )
						return K;
				}

				return null;
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_Interval.KeysChanged -= new EventHandler( Interval_KeysChanged );
				m_Interval.KeyValueChanged -= new Sequencor.ParameterTrack.Interval.KeyValueChangedEventHandler( Interval_KeyValueChanged );
				m_Interval.ActualTimeStartChanged -= new EventHandler( Interval_ActualTimeStartChanged );
				m_Interval.ActualTimeEndChanged -= new EventHandler( Interval_ActualTimeEndChanged );
			}

			#endregion

			#endregion

			#region EVENT HANDLERS

			protected void Interval_KeyValueChanged( Sequencor.ParameterTrack.Interval _Sender, Sequencor.AnimationTrack.Key _Key )
			{
				RebuildGradient();
				m_Owner.Invalidate();
			}

			protected void Interval_KeysChanged( object sender, EventArgs e )
			{
				RebuildGradient();
				m_Owner.Invalidate();
			}

			protected void Interval_ActualTimeEndChanged( object sender, EventArgs e )
			{
				m_Owner.Invalidate();
			}

			protected void Interval_ActualTimeStartChanged( object sender, EventArgs e )
			{
				m_Owner.Invalidate();
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected AnimationEditorControl	m_Owner = null;
		protected Sequencor.ParameterTrack	m_Track = null;
		protected Sequencor.AnimationTrack.Key	m_SelectedKey = null;

		protected float						m_RangeMin = 0.0f;
		protected float						m_RangeMax = 10.0f;

		protected List<DrawnInterval>		m_Intervals = new List<DrawnInterval>();

		// Appearance
		protected Color						m_CursorTimeColor = Color.ForestGreen;
		protected Pen						m_PenCursorTime = null;

		#endregion

		#region PROPERTIES

		[Browsable( false )]
		public AnimationEditorControl		Owner		{ get { return m_Owner; } set { m_Owner = value; } }

		[Browsable( false )]
		public Sequencor.ParameterTrack		Track
		{
			get { return m_Track; }
			set
			{
				if ( value == m_Track )
					return;

				if ( m_Track != null )
				{
					m_Track.IntervalsChanged -= new EventHandler( Track_IntervalsChanged );
					m_Track.ClipChanged -= new EventHandler( Track_ClipChanged );
					m_Track.CubicInterpolationChanged -= new EventHandler( Track_CubicInterpolationChanged );
				}

				m_Track = value;

				if ( m_Track != null )
				{
					m_Track.IntervalsChanged += new EventHandler( Track_IntervalsChanged );
					Track_IntervalsChanged( m_Track, EventArgs.Empty );
					m_Track.ClipChanged += new EventHandler( Track_ClipChanged );
					m_Track.CubicInterpolationChanged += new EventHandler( Track_CubicInterpolationChanged );
				}
			}
		}

		[Browsable( false )]
		public Sequencor.AnimationTrack.Key			SelectedKey
		{
			get { return m_SelectedKey; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_SelectedKey )
					return;

				m_SelectedKey = value;

				Refresh();
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

		#endregion

		#region METHODS

		public GradientTrackPanel()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			// Should create the pens
			CursorTimeColor = CursorTimeColor;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
				ClearIntervals();
			base.Dispose( disposing );
		}
		/// <summary>
		/// Sets both min and max range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetRange( float _RangeMin, float _RangeMax )
		{
			if ( Math.Abs( _RangeMin - m_RangeMin ) < 1e-3f && Math.Abs( _RangeMax - m_RangeMax ) < 1e-3f )
				return;	// No change...

			m_RangeMin = _RangeMin;
			m_RangeMax = _RangeMax;

			Invalidate();
		}

		#region Control Members

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );
			Refresh();
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			if ( m_Track == null )
				return;
			if ( m_RangeMax - m_RangeMin < 1e-3f )
				return;	// Can't trace invalid range!

			foreach ( DrawnInterval DI in m_Intervals )
				DI.Draw( e.Graphics );

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
			foreach ( DrawnInterval DI in m_Intervals )
				if ( DI.Contains( _ClientPosition.X, _ClientPosition.Y ) )
					return	DI.Interval;

			return	null;
		}

		/// <summary>
		/// Attempts to retrieve the key under the specified position
		/// </summary>
		/// <param name="_ClientPosition">The position in CLIENT space</param>
		/// <returns>The key at this position</returns>
		public Sequencor.AnimationTrack.Key			GetKeyAt( Point _ClientPosition )
		{
			foreach ( DrawnInterval DI in m_Intervals )
				if ( DI.Contains( _ClientPosition.X, _ClientPosition.Y ) )
					return DI.GetKeyAt( _ClientPosition );

			return null;
		}

		/// <summary>
		/// Converts a client position into a sequence time
		/// </summary>
		/// <param name="_fClientPosition">The CLIENT SPACE position</param>
		/// <returns>The equivalent sequence time</returns>
		public float		ClientToSequenceTime( float _fClientPosition )
		{
			return	m_RangeMin + (m_RangeMax - m_RangeMin) * _fClientPosition / Width;
		}

		/// <summary>
		/// Converts a sequence time into a client position
		/// </summary>
		/// <param name="_fSequenceTime">The sequence time</param>
		/// <returns>The equivalent CLIENT SPACE position</returns>
		public float		SequenceTimeToClient( float _fSequenceTime )
		{
			return	(_fSequenceTime - m_RangeMin) * Width / (m_RangeMax - m_RangeMin);
		}

		protected void	ClearIntervals()
		{
			foreach ( DrawnInterval DI in m_Intervals )
				DI.Dispose();
			m_Intervals.Clear();
		}

		protected void	RebuildIntervals()
		{
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					break;

				default:
					return;	// Those tracks don't have color
			}

			ClearIntervals();
			foreach ( Sequencor.ParameterTrack.Interval I in m_Track.Intervals )
				m_Intervals.Add( new DrawnInterval( this, I ) );
		}

		#endregion

		#region EVENT HANDLERS

		protected void Track_CubicInterpolationChanged( object sender, EventArgs e )
		{
			Invalidate();
		}

		protected void Track_ClipChanged( object sender, EventArgs e )
		{
			Invalidate();
		}

		protected void Track_IntervalsChanged( object sender, EventArgs e )
		{
			RebuildIntervals();
			Invalidate();
		}

		#endregion
	}
}
