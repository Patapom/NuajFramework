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
	public partial class AnimationTrackPanel : Panel
	{
		#region CONSTANTS

		protected const float	KEY_HALF_GRAB_SIZE = 4.0f;		// The half size of the grab zone around a key for manipulation
		protected const float	TANGENT_HANDLE_SIZE = 5.0f;		// The size of a tangent handle
		protected const float	TANGENT_PHASE_SYNC = 3.0f * (float) Math.PI / 180.0f;	// The angle below which tangents are "synched" (i.e. auto-aligned)
		protected const float	DEFAULT_VERTICAL_RANGE_MIN = -0.1f;
		protected const float	DEFAULT_VERTICAL_RANGE_MAX = 2.0f;
		protected const float	VERTICAL_ZOOM_FACTOR = 0.0008333f;
		protected const int		FLOAT_SUBDIVISIONS_COUNT = 12;

		#endregion

		#region NESTED TYPES

		public delegate void	CustomIntervalPaintEventHandler( AnimationTrackPanel _Sender, Graphics _Graphics, RectangleF _IntervalRectangle );

		public enum KEY_TYPE
		{
			DEFAULT,	// Default key type (for bool, event or float types that don't have separate meanings for their animation tracks)
			POSITION,	// Position key
			ROTATION,	// Rotation key
			SCALE		// Scale key
		}

		protected class		DrawnInterval : IDisposable
		{
			#region NESTED TYPES

			protected abstract class	AnimationTrack
			{
				#region NESTED TYPES

				public class	DrawnKey
				{
					protected AnimationTrack			m_Owner = null;
					public PointF						m_Position;
					public Sequencor.AnimationTrack.Key	m_Key = null;

					public bool			Selected
					{
						get { return m_Owner.m_Owner.m_Owner.SelectedKey == m_Key; }
					}

					public bool			Hovered
					{
						get { return m_Owner.m_Owner.m_Owner.HoveredKey == m_Key; }
					}

					public DrawnKey( AnimationTrack _Owner, PointF _Position, Sequencor.AnimationTrack.Key _Key )
					{
						m_Owner = _Owner;
						m_Position = _Position;
						m_Key = _Key;
					}
				}

				#endregion

				#region FIELDS

				protected DrawnInterval	m_Owner = null;
				protected Sequencor.AnimationTrack	m_Track = null;

				protected Color		m_Color;
				protected Color		m_SelectedColor;
				protected Brush		m_Brush;
				protected Brush		m_SelectedBrush;
				protected Pen		m_Pen;
				protected Pen		m_SelectedPen;

				protected List<DrawnKey>	m_DrawnKeys = new List<DrawnKey>();

				#endregion

				#region PROPERTIES

				public bool			Selected
				{
					get { return m_Owner.m_Owner.SelectedKey != null && m_Owner.m_Owner.SelectedKey.ParentAnimationTrack == m_Track; }
				}

				protected virtual float	KeyHalfSize
				{
					get { return KEY_HALF_GRAB_SIZE; }
				}

				#endregion

				#region METHODS

				public AnimationTrack( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor )
				{
					m_Owner = _Owner;
					m_Track = _Track;
					m_Color = _Color;
					m_Brush = new SolidBrush( m_Color );
					m_Pen = new Pen( m_Color, 1.0f );
					m_SelectedColor = _SelectedColor;
					m_SelectedBrush = new SolidBrush( m_SelectedColor );
					m_SelectedPen = new Pen( m_SelectedColor, 2.0f );
				}

				/// <summary>
				/// Updates the track's visual data
				/// </summary>
				/// <param name="_Clip">The clip rectangle in sequence space</param>
				public virtual void	Update( RectangleF _Clip )
				{
					// Check which keys can be displayed
					m_DrawnKeys.Clear();
					foreach ( Sequencor.AnimationTrack.Key K in m_Track.Keys )
					{
						PointF	KeyPos = GetKeyPosition( K );
						if ( !_Clip.Contains( KeyPos ) )
							continue;	// Out of range

						m_DrawnKeys.Add( new DrawnKey( this, KeyPos, K ) );
					}
				}

				/// <summary>
				/// Draw the track's visual data
				/// </summary>
				/// <param name="G"></param>
				public abstract void	Draw( Graphics G );

				/// <summary>
				/// Attempts to retrieve the key under the specified position
				/// </summary>
				/// <param name="_ClientPosition">The position in CLIENT space</param>
				/// <returns>The key at this position</returns>
				public Sequencor.AnimationTrack.Key		GetKeyAt( Point _ClientPosition )
				{
					foreach ( DrawnKey DK in m_DrawnKeys )
					{
						PointF	KeyClientPos = m_Owner.m_Owner.SequenceTimeToClient( DK.m_Position.X, DK.m_Position.Y );
						if ( Math.Abs( _ClientPosition.X - KeyClientPos.X ) <= KeyHalfSize &&
							 Math.Abs( _ClientPosition.Y - KeyClientPos.Y ) <= KeyHalfSize )
							return DK.m_Key;
					}

					return null;
				}

				/// <summary>
				/// Attempts to retrieve the key if we're pointing at its IN tangent
				/// </summary>
				/// <param name="_ClientPosition">The position in CLIENT space</param>
				/// <returns>The key at this position</returns>
				public virtual Sequencor.AnimationTrackFloat.KeyFloat	GetKeyTangentInAt( Point _ClientPosition )
				{
					return null;
				}

				/// <summary>
				/// Attempts to retrieve the key if we're pointing at its IN tangent
				/// </summary>
				/// <param name="_ClientPosition">The position in CLIENT space</param>
				/// <returns>The key at this position</returns>
				public virtual Sequencor.AnimationTrackFloat.KeyFloat	GetKeyTangentOutAt( Point _ClientPosition )
				{
					return null;
				}

				/// <summary>
				/// Returns the position of the specified key in sequence space (not in normalized key space or client space !)
				/// </summary>
				/// <param name="_Key"></param>
				/// <returns></returns>
				protected abstract PointF	GetKeyPosition( Sequencor.AnimationTrack.Key _Key );

				/// <summary>
				/// Draw the array of visible keys
				/// </summary>
				/// <param name="G"></param>
				protected void				DrawKeys( Graphics G )
				{
					foreach ( DrawnKey DK in m_DrawnKeys )
						DrawKey( G, DK );
				}

				/// <summary>
				/// Draw the provided key
				/// </summary>
				/// <param name="G"></param>
				/// <param name="_Key"></param>
				protected virtual void		DrawKey( Graphics G, DrawnKey _Key )
				{
					PointF	ClientPos = m_Owner.m_Owner.SequenceTimeToClient( _Key.m_Position.X, _Key.m_Position.Y );
					G.DrawImage( _Key.Selected || _Key.Hovered ? Properties.Resources.KeySelected_Alpha : Properties.Resources.Key_Alpha, ClientPos.X - KeyHalfSize, ClientPos.Y - KeyHalfSize, 2*KeyHalfSize, 2*KeyHalfSize );
				}

				/// <summary>
				/// Creates the appropriate type of animation track
				/// </summary>
				/// <param name="_Track"></param>
				/// <param name="_Color"></param>
				/// <param name="_SelectedColor"></param>
				/// <returns></returns>
				public static AnimationTrack	Create( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor )
				{
					if ( _Track is Sequencor.AnimationTrackBool )
						return new AnimationTrackBool( _Owner, _Track, _Color, _SelectedColor );
					else if ( _Track is Sequencor.AnimationTrackEvent )
						return new AnimationTrackEvent( _Owner, _Track, _Color, _SelectedColor );
					else if ( _Track is Sequencor.AnimationTrackInt )
						return new AnimationTrackInt( _Owner, _Track, _Color, _SelectedColor );
					else if ( _Track is Sequencor.AnimationTrackFloat )
						return new AnimationTrackFloat( _Owner, _Track, _Color, _SelectedColor );
					else if ( _Track is Sequencor.AnimationTrackQuat )
						return new AnimationTrackQuat( _Owner, _Track, _Color, _SelectedColor );

					return null;
				}

				#endregion
			}

			protected class		AnimationTrackBool : AnimationTrack
			{
				#region FIELDS

				protected List<RectangleF>	m_StateRectangles = new List<RectangleF>();

				#endregion

				#region PROPERTIES

				protected override float KeyHalfSize
				{
					get { return 2*base.KeyHalfSize; }
				}

				#endregion

				#region METHODS

				public AnimationTrackBool( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor ) : base( _Owner, _Track, _Color, _SelectedColor )
				{
				}

				public override void	Update( RectangleF _Clip )
				{
					base.Update( _Clip );

					m_StateRectangles.Clear();
					if ( m_Track.KeysCount == 0 )
						return;

					Sequencor.AnimationTrackBool	Track = m_Track as Sequencor.AnimationTrackBool;

					// First rectangle is from 0 to first key
					Sequencor.AnimationTrack.Key	FirstKey = Track[0];
					AddStateRectangle( m_Owner.m_Interval.TimeStart, FirstKey.TrackTime, (FirstKey as Sequencor.AnimationTrackBool.KeyBool).Value, _Clip );

					// Standard keys
					for ( int KeyIndex=0; KeyIndex < Track.KeysCount-1; KeyIndex++ )
					{
						Sequencor.AnimationTrackBool.KeyBool K0 = Track.Keys[KeyIndex] as Sequencor.AnimationTrackBool.KeyBool;
						Sequencor.AnimationTrackBool.KeyBool K1 = Track.Keys[KeyIndex+1] as Sequencor.AnimationTrackBool.KeyBool;
						AddStateRectangle( K0.TrackTime, K1.TrackTime, K0.Value, _Clip );
					}

					// Last rectangle is from last key to 1
					Sequencor.AnimationTrack.Key	LastKey = Track[Track.KeysCount-1];
					AddStateRectangle( LastKey.TrackTime, m_Owner.m_Interval.TimeEnd, (LastKey as Sequencor.AnimationTrackBool.KeyBool).Value, _Clip );
				}

				/// <summary>
				/// Adds a new state rectangle
				/// </summary>
				/// <param name="_TimeStart">Start time in SEQUENCE space</param>
				/// <param name="_TimeEnd">End time in SEQUENCE space</param>
				/// <param name="_bState">Rectangle state</param>
				/// <param name="_Clip">Clip rectangle in sequence space to clip against</param>
				protected void	AddStateRectangle( float _TimeStart, float _TimeEnd, bool _bState, RectangleF _Clip )
				{
					if ( _TimeStart > _Clip.Right || _TimeEnd < _Clip.Left )
						return;

					PointF	Start = m_Owner.m_Owner.SequenceTimeToClient( _TimeStart, 0.0f );
					PointF	End = m_Owner.m_Owner.SequenceTimeToClient( _TimeEnd, _bState ? 1.0f : 0.02f );
					float	MinY = Math.Min( Start.Y, End.Y );
					float	MaxY = Math.Max( Start.Y, End.Y );

					m_StateRectangles.Add( new RectangleF( Start.X, MinY, End.X - Start.X, MaxY - MinY ) );
				}

				public override void	Draw( Graphics G )
				{
					foreach ( RectangleF R in m_StateRectangles )
					{
						G.FillRectangle( Selected ? m_SelectedBrush : m_Brush, R );
						G.DrawRectangle( Selected ? m_SelectedPen : m_Pen, R.X, R.Y, R.Width, Math.Max( 2.0f, R.Height ) );
					}

					DrawKeys( G );
				}

				protected override PointF GetKeyPosition( Sequencor.AnimationTrack.Key _Key )
				{
					return new PointF( _Key.TrackTime, (_Key as Sequencor.AnimationTrackBool.KeyBool).Value ? 1.0f : 0.0f );
				}

				#endregion
			}

			protected class		AnimationTrackEvent : AnimationTrack
			{
				#region FIELDS

				#endregion

				#region PROPERTIES

				protected override float KeyHalfSize
				{
					get { return 2*base.KeyHalfSize; }
				}

				#endregion

				#region METHODS

				public AnimationTrackEvent( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor ) : base( _Owner, _Track, _Color, _SelectedColor )
				{
				}

				public override void	Draw( Graphics G )
				{
					DrawKeys( G );
				}

				protected override PointF GetKeyPosition( Sequencor.AnimationTrack.Key _Key )
				{
					return new PointF( _Key.TrackTime, 0.0f );
				}

				#endregion
			}

			protected class		AnimationTrackInt : AnimationTrack
			{
				#region FIELDS

				protected List<PointF>	m_Points = new List<PointF>();

				#endregion

				#region METHODS

				public AnimationTrackInt( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor ) : base( _Owner, _Track, _Color, _SelectedColor )
				{
				}

				public override void	Update( RectangleF _Clip )
				{
					base.Update( _Clip );

					m_Points.Clear();
					if ( m_Track.KeysCount == 0 )
						return;

					Sequencor.AnimationTrackInt	Track = m_Track as Sequencor.AnimationTrackInt;

					// First point from 0 to first key
					Sequencor.AnimationTrackInt.KeyInt	FirstKey = Track[0] as Sequencor.AnimationTrackInt.KeyInt;
					m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( m_Owner.m_Interval.TimeStart, FirstKey.Value ) );

					// Standard points
					for ( int KeyIndex=0; KeyIndex < Track.KeysCount; KeyIndex++ )
					{
						Sequencor.AnimationTrackInt.KeyInt	K = Track[KeyIndex] as Sequencor.AnimationTrackInt.KeyInt;
						m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( K.TrackTime, K.Value ) );
					}

					// Last point from last key to 1
					Sequencor.AnimationTrackInt.KeyInt	LastKey = Track[Track.KeysCount-1] as Sequencor.AnimationTrackInt.KeyInt;
					m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( m_Owner.m_Interval.TimeEnd, LastKey.Value ) );
				}

				public override void	Draw( Graphics G )
				{
					if ( m_Points.Count > 1 )
						G.DrawLines( Selected ? m_SelectedPen : m_Pen, m_Points.ToArray() );
					DrawKeys( G );
				}

				protected override PointF GetKeyPosition( Sequencor.AnimationTrack.Key _Key )
				{
					return new PointF( _Key.TrackTime, (_Key as Sequencor.AnimationTrackInt.KeyInt).Value );
				}

				#endregion
			}

			protected class		AnimationTrackFloat : AnimationTrack
			{
				#region FIELDS

				protected List<PointF>	m_Points = new List<PointF>();

				#endregion

				#region METHODS

				public AnimationTrackFloat( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor ) : base( _Owner, _Track, _Color, _SelectedColor )
				{
				}

				public override void	Update( RectangleF _Clip )
				{
					base.Update( _Clip );

					m_Points.Clear();
					if ( m_Track.KeysCount == 0 )
						return;
					float	StartX = m_Owner.m_Owner.SequenceTimeToClient( m_Owner.m_Interval.TimeStart );
					float	EndX = m_Owner.m_Owner.SequenceTimeToClient( m_Owner.m_Interval.TimeEnd );
					if ( Math.Abs( EndX - StartX ) < 2.0f )
						return;

					Sequencor.AnimationTrackFloat	Track = m_Track as Sequencor.AnimationTrackFloat;
	
					Sequencor.AnimationTrackFloat.KeyFloat	FirstKey = Track[0] as Sequencor.AnimationTrackFloat.KeyFloat;
					m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( m_Owner.m_Interval.TimeStart, Clip( FirstKey.Value ) ) );

					// Standard points
					for ( int KeyIndex=0; KeyIndex < Track.KeysCount-1; KeyIndex++ )
					{
						Sequencor.AnimationTrackFloat.KeyFloat	K0 = Track[KeyIndex] as Sequencor.AnimationTrackFloat.KeyFloat;
						Sequencor.AnimationTrackFloat.KeyFloat	K1 = Track[KeyIndex+1] as Sequencor.AnimationTrackFloat.KeyFloat;

						for ( int DivIndex=0; DivIndex < FLOAT_SUBDIVISIONS_COUNT; DivIndex++ )
						{
							float	t = K0.TrackTime + (K1.TrackTime - K0.TrackTime) * DivIndex / FLOAT_SUBDIVISIONS_COUNT;
							float	Value = Track.ImmediateEval( t );
							m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( t, Value ) );
						}
					}

					// Last point from last key to 1
					Sequencor.AnimationTrackFloat.KeyFloat	LastKey = Track[Track.KeysCount-1] as Sequencor.AnimationTrackFloat.KeyFloat;
					m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( LastKey.TrackTime, Clip( LastKey.Value ) ) );
					m_Points.Add( m_Owner.m_Owner.SequenceTimeToClient( m_Owner.m_Interval.TimeEnd, Clip( LastKey.Value ) ) );
				}

				/// <summary>
				/// Clips the provided value
				/// </summary>
				/// <param name="_Point"></param>
				/// <returns></returns>
				protected float		Clip( float _Value )
				{
					float	ClipMin = m_Track.ParentInterval.ParentTrack.ClipMin;
					if ( !float.IsInfinity( ClipMin ) )
						_Value = Math.Max( ClipMin, _Value );
					float	ClipMax = m_Track.ParentInterval.ParentTrack.ClipMax;
					if ( !float.IsInfinity( ClipMax ) )
						_Value = Math.Min( ClipMax, _Value );

					return _Value;
				}

				public override void	Draw( Graphics G )
				{
					// Draw clipping limits
					float	ClipMin = m_Track.ParentInterval.ParentTrack.ClipMin;
					if ( !float.IsInfinity( ClipMin ) )
					{
						PointF	P0 = m_Owner.m_Owner.SequenceTimeToClient( m_Owner.Interval.TimeStart, ClipMin );
						PointF	P1 = m_Owner.m_Owner.SequenceTimeToClient( m_Owner.Interval.TimeEnd, ClipMin );
						G.DrawLine( m_Owner.m_Owner.m_PenClip, P0, P1 );
					}

					float	ClipMax = m_Track.ParentInterval.ParentTrack.ClipMax;
					if ( !float.IsInfinity( ClipMax ) )
					{
						PointF	P0 = m_Owner.m_Owner.SequenceTimeToClient( m_Owner.Interval.TimeStart, ClipMax );
						PointF	P1 = m_Owner.m_Owner.SequenceTimeToClient( m_Owner.Interval.TimeEnd, ClipMax );
						G.DrawLine( m_Owner.m_Owner.m_PenClip, P0, P1 );
					}

					// Draw curve
					if ( m_Points.Count > 1 )
						G.DrawLines( Selected ? m_SelectedPen : m_Pen, m_Points.ToArray() );

					DrawKeys( G );
				}

				public override Sequencor.AnimationTrackFloat.KeyFloat GetKeyTangentInAt( Point _ClientPosition )
				{
					float fTangentHalfSize = 0.5f * TANGENT_HANDLE_SIZE;
					foreach ( DrawnKey DK in m_DrawnKeys )
					{
						Sequencor.AnimationTrackFloat.KeyFloat	Key = DK.m_Key as Sequencor.AnimationTrackFloat.KeyFloat;
						if ( Key.ParentAnimationTrack.IndexOf( Key ) == 0 )
							continue;	// Discard tangent IN if first key

						PointF	KeyTangentInClientPos = m_Owner.m_Owner.TangentInSequenceToClient( Key );
						if ( Math.Abs( _ClientPosition.X - KeyTangentInClientPos.X ) <= fTangentHalfSize &&
							 Math.Abs( _ClientPosition.Y - KeyTangentInClientPos.Y ) <= fTangentHalfSize )
							return Key;
					}

					return null;
				}

				public override Sequencor.AnimationTrackFloat.KeyFloat GetKeyTangentOutAt( Point _ClientPosition )
				{
					float fTangentHalfSize = 0.5f * TANGENT_HANDLE_SIZE;
					foreach ( DrawnKey DK in m_DrawnKeys )
					{
						Sequencor.AnimationTrackFloat.KeyFloat	Key = DK.m_Key as Sequencor.AnimationTrackFloat.KeyFloat;
						if ( Key.ParentAnimationTrack.IndexOf( Key ) == Key.ParentAnimationTrack.KeysCount-1 )
							continue;	// Discard tangent OUT if last key

						PointF	KeyTangentInClientPos = m_Owner.m_Owner.TangentOutSequenceToClient( Key );
						if ( Math.Abs( _ClientPosition.X - KeyTangentInClientPos.X ) <= fTangentHalfSize &&
							 Math.Abs( _ClientPosition.Y - KeyTangentInClientPos.Y ) <= fTangentHalfSize )
							return Key;
					}

					return null;
				}

				protected override void DrawKey( Graphics G, AnimationTrack.DrawnKey _Key )
				{
					if ( !m_Owner.m_Owner.m_Owner.ShowTangents )
					{
						base.DrawKey( G, _Key );
						return;
					}

					// Draw tangents
					Sequencor.AnimationTrackFloat.KeyFloat	Key = _Key.m_Key as Sequencor.AnimationTrackFloat.KeyFloat;

					bool	bIsFirst = Key.ParentAnimationTrack.IndexOf( Key ) == 0;
					bool	bIsLast = Key.ParentAnimationTrack.IndexOf( Key ) == Key.ParentAnimationTrack.KeysCount-1;

					PointF	TangentInPos = m_Owner.m_Owner.TangentInSequenceToClient( Key );
					PointF	TangentOutPos = m_Owner.m_Owner.TangentOutSequenceToClient( Key );
					PointF	KeyPos = m_Owner.m_Owner.SequenceTimeToClient( _Key.m_Position.X, _Key.m_Position.Y );
					bool	bKeyTooClose =  Math.Abs( KeyPos.X - TangentInPos.X  ) < TANGENT_HANDLE_SIZE || Math.Abs( KeyPos.Y - TangentInPos.Y ) < TANGENT_HANDLE_SIZE || 
											Math.Abs( KeyPos.X - TangentOutPos.X ) < TANGENT_HANDLE_SIZE || Math.Abs( KeyPos.Y - TangentOutPos.Y ) < TANGENT_HANDLE_SIZE;

					bool	bTangentInSelected = m_Owner.m_Owner.m_HoveredKeyTangentIn == Key;
					bool	bTangentOutSelected = m_Owner.m_Owner.m_HoveredKeyTangentOut == Key;

					if ( !bIsFirst )
						G.DrawLine( bTangentInSelected ? Pens.Red : Pens.Orange, KeyPos, TangentInPos );
					if ( !bIsLast )
						G.DrawLine( bTangentOutSelected ? Pens.Red : Pens.Orange, KeyPos, TangentOutPos );

					// Display key BEFORE tangents if they're too close
					if ( bKeyTooClose )
						base.DrawKey( G, _Key );

					if ( !bIsFirst )
						G.FillEllipse( bTangentInSelected ? Brushes.Red : Brushes.Orange, TangentInPos.X-0.5f*TANGENT_HANDLE_SIZE, TangentInPos.Y-0.5f*TANGENT_HANDLE_SIZE, TANGENT_HANDLE_SIZE, TANGENT_HANDLE_SIZE );
					if ( !bIsLast )
						G.FillEllipse( bTangentOutSelected ? Brushes.Red : Brushes.Orange, TangentOutPos.X-0.5f*TANGENT_HANDLE_SIZE, TangentOutPos.Y-0.5f*TANGENT_HANDLE_SIZE, TANGENT_HANDLE_SIZE, TANGENT_HANDLE_SIZE );

					// Display key OVER tangents ONLY if it's far enough
					if ( !bKeyTooClose )
						base.DrawKey( G, _Key );
				}

				protected override PointF GetKeyPosition( Sequencor.AnimationTrack.Key _Key )
				{
					return new PointF( _Key.TrackTime, (_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value );
				}

				#endregion
			}

			protected class		AnimationTrackQuat : AnimationTrack
			{
				#region FIELDS
				#endregion

				#region PROPERTIES

				protected override float KeyHalfSize
				{
					get { return 2*base.KeyHalfSize; }
				}

				#endregion

				#region METHODS

				public AnimationTrackQuat( DrawnInterval _Owner, Sequencor.AnimationTrack _Track, Color _Color, Color _SelectedColor ) : base( _Owner, _Track, _Color, _SelectedColor )
				{
				}

				public override void	Draw( Graphics G )
				{
					DrawKeys( G );
				}

				protected override PointF GetKeyPosition( Sequencor.AnimationTrack.Key _Key )
				{
					return new PointF( _Key.TrackTime, 0.0f );
				}

				#endregion
			}

			#endregion

			#region FIELDS

			protected AnimationTrackPanel	m_Owner = null;
			protected Sequencor.ParameterTrack.Interval	m_Interval = null;

			protected AnimationTrack[]		m_AnimTracks = null;

			// Drawing data
			protected bool					m_bVisible = true;
			protected RectangleF			m_Rectangle = RectangleF.Empty;

			#endregion

			#region PROPERTIES

			public Sequencor.ParameterTrack.Interval	Interval	{ get { return m_Interval; } }

			#endregion

			#region METHODS

			public DrawnInterval( AnimationTrackPanel _Owner, Sequencor.ParameterTrack.Interval _Interval )
			{
				m_Owner = _Owner;
				m_Owner.RangeChanged += new EventHandler( Owner_RangeChanged );

				m_Interval = _Interval;
				m_Interval.ActualTimeStartChanged += new EventHandler( Interval_ActualTimeStartChanged );
				m_Interval.ActualTimeEndChanged += new EventHandler( Interval_ActualTimeEndChanged );
				m_Interval.KeysChanged += new EventHandler( Interval_KeysChanged );
				m_Interval.KeyValueChanged += new Sequencor.ParameterTrack.Interval.KeyValueChangedEventHandler( Interval_KeyValueChanged );

				// Build the animation tracks
				m_AnimTracks = new AnimationTrack[m_Interval.AnimationTracksCount];

				Color[]	TrackColors = new Color[]
				{
					Color.DarkRed,		// PX / float / bool / int / event
					Color.DarkGreen,	// PY / float2
					Color.DarkBlue,		// PZ / float3
					Color.Gold,			// Rotation / float4
					Color.DarkRed,		// SX
					Color.DarkGreen,	// SY
					Color.DarkBlue,		// SZ
				};
				Color[]	TrackSelectedColors = new Color[]
				{
					Color.Red,			// PX / float / bool / int / event
					Color.Green,		// PY / float2
					Color.Blue,			// PZ / float3
					Color.OrangeRed,	// Rotation / float4
					Color.Red,			// SX
					Color.Green,		// SY
					Color.Blue,			// SZ
				};

				for ( int TrackIndex=0; TrackIndex < m_Interval.AnimationTracksCount; TrackIndex++ )
					m_AnimTracks[TrackIndex] = AnimationTrack.Create( this, m_Interval[TrackIndex], TrackColors[TrackIndex], TrackSelectedColors[TrackIndex] );

				// Build interval data
				Update();
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_Owner.RangeChanged -= new EventHandler( Owner_RangeChanged );
				m_Interval.ActualTimeStartChanged -= new EventHandler( Interval_ActualTimeStartChanged );
				m_Interval.ActualTimeEndChanged -= new EventHandler( Interval_ActualTimeEndChanged );
				m_Interval.KeysChanged -= new EventHandler( Interval_KeysChanged );
				m_Interval.KeyValueChanged -= new Sequencor.ParameterTrack.Interval.KeyValueChangedEventHandler( Interval_KeyValueChanged );
			}

			#endregion

			/// <summary>
			/// Updates the interval's visual data
			/// </summary>
			/// <param name="_Clip">The clip rectangle in sequence space</param>
			public void		Update()
			{
				RectangleF ClipRect = new RectangleF(
					m_Owner.m_RangeMin,
					m_Owner.m_VerticalRangeMin,
					m_Owner.m_RangeMax - m_Owner.m_RangeMin,
					m_Owner.m_VerticalRangeMax - m_Owner.m_VerticalRangeMin );

				float	fIntervalPosStart = m_Owner.SequenceTimeToClient( m_Interval.TimeStart );
				float	fIntervalPosEnd = m_Owner.SequenceTimeToClient( m_Interval.TimeEnd );

				m_Rectangle = new RectangleF( fIntervalPosStart, 0.0f, Math.Max( 2.0f, fIntervalPosEnd - fIntervalPosStart ), m_Owner.Height );
				m_bVisible = fIntervalPosEnd > 0.0f && fIntervalPosStart < m_Owner.Width;
				if ( !m_bVisible )
					return;

				// Build animation tracks
				foreach ( AnimationTrack T in m_AnimTracks )
					T.Update( ClipRect );
			}

			public void		DrawBackground( Graphics G )
			{
				if ( !m_bVisible )
					return;

				// Draw a huge rectangle
				bool bSelected = m_Owner.SelectedInterval == m_Interval;
				G.FillRectangle( bSelected ? m_Owner.m_BrushSelectedInterval : m_Owner.m_Brush, m_Rectangle );
			}

			public void		Draw( Graphics G )
			{
				if ( !m_bVisible )
					return;

				// Draw a huge rectangle
				bool bSelected = m_Owner.SelectedInterval == m_Interval;
// 				try
// 				{
// 					G.DrawRectangle( bSelected ? m_Owner.m_PenSelectedInterval : m_Owner.m_Pen, m_Rectangle.X, m_Rectangle.Y, m_Rectangle.Width, m_Rectangle.Height );
// 				}
// 				catch ( Exception )
// 				{
// 					// WTH ?? This crashes from time to time if I draw too big a rectangle...
// 				}
//				G.DrawLine( bSelected ? m_Owner.m_PenSelectedInterval : m_Owner.m_Pen, m_Rectangle.X, 0, m_Rectangle.X, m_Rectangle.Height );
//				G.DrawLine( bSelected ? m_Owner.m_PenSelectedInterval : m_Owner.m_Pen, m_Rectangle.X + m_Rectangle.Width, 0, m_Rectangle.X + m_Rectangle.Width, m_Rectangle.Height );
				G.DrawLine( bSelected ? m_Owner.m_PenSelected : m_Owner.m_Pen, m_Rectangle.X, 0, m_Rectangle.X, m_Rectangle.Height );
				G.DrawLine( bSelected ? m_Owner.m_PenSelected : m_Owner.m_Pen, m_Rectangle.X + m_Rectangle.Width, 0, m_Rectangle.X + m_Rectangle.Width, m_Rectangle.Height );

				foreach ( AnimationTrack T in m_AnimTracks )
					T.Draw( G );
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
				foreach ( AnimationTrack T in m_AnimTracks )
				{
					Sequencor.AnimationTrack.Key	Key = T.GetKeyAt( _ClientPosition );
					if ( Key != null )
						return Key;
				}

				return null;
			}

			/// <summary>
			/// Attempts to retrieve the key if we're pointing at its IN tangent
			/// </summary>
			/// <param name="_ClientPosition">The position in CLIENT space</param>
			/// <returns>The key at this position</returns>
			public Sequencor.AnimationTrackFloat.KeyFloat	GetKeyTangentInAt( Point _ClientPosition )
			{
				foreach ( AnimationTrack T in m_AnimTracks )
				{
					Sequencor.AnimationTrackFloat.KeyFloat	Key = T.GetKeyTangentInAt( _ClientPosition );
					if ( Key != null )
						return Key;
				}

				return null;
			}

			/// <summary>
			/// Attempts to retrieve the key if we're pointing at its OUT tangent
			/// </summary>
			/// <param name="_ClientPosition">The position in CLIENT space</param>
			/// <returns>The key at this position</returns>
			public Sequencor.AnimationTrackFloat.KeyFloat	GetKeyTangentOutAt( Point _ClientPosition )
			{
				foreach ( AnimationTrack T in m_AnimTracks )
				{
					Sequencor.AnimationTrackFloat.KeyFloat	Key = T.GetKeyTangentOutAt( _ClientPosition );
					if ( Key != null )
						return Key;
				}

				return null;
			}

			#endregion

			#region EVENT HANDLERS

			protected void Owner_RangeChanged( object sender, EventArgs e )
			{
//				Update();
				m_Owner.Invalidate();
			}

			protected void Interval_ActualTimeStartChanged( object sender, EventArgs e )
			{
//				Update();
				m_Owner.Invalidate();
			}

			protected void Interval_ActualTimeEndChanged( object sender, EventArgs e )
			{
//				Update();
				m_Owner.Invalidate();
			}

			protected void Interval_KeysChanged( object sender, EventArgs e )
			{
//				Update();
				m_Owner.Invalidate();
			}

			protected void Interval_KeyValueChanged( Sequencor.ParameterTrack.Interval _Sender, Sequencor.AnimationTrack.Key _Key )
			{
//				Update();
				m_Owner.Invalidate();
			}

			#endregion
		};

		#endregion

		#region FIELDS

		protected AnimationEditorControl			m_Owner = null;
		protected Sequencor.ParameterTrack			m_Track = null;
		protected Sequencor.ParameterTrack.Interval	m_SelectedInterval = null;
		protected Sequencor.AnimationTrack.Key		m_SelectedKey = null;
		protected Sequencor.AnimationTrack.Key		m_HoveredKey = null;
		protected Sequencor.AnimationTrack.Key		m_HoveredKeyTangentIn = null;
		protected Sequencor.AnimationTrack.Key		m_HoveredKeyTangentOut = null;

		// Visual range
		protected float						m_RangeMin = 0.0f;
		protected float						m_RangeMax = 10.0f;

		protected float						m_VerticalRangeMin = DEFAULT_VERTICAL_RANGE_MIN;
		protected float						m_VerticalRangeMax = DEFAULT_VERTICAL_RANGE_MAX;

		// Appearance
		protected bool						m_bPreventRefresh = false;
		protected Color						m_SelectedColor = Color.MistyRose;
		protected Color						m_SelectedIntervalColor = Color.IndianRed;
		protected Color						m_SmallGraduationsColor = Color.DarkGray;
		protected Color						m_MainGraduationsColor = Color.Black;
		protected Color						m_CursorTimeColor = Color.ForestGreen;
		protected Color						m_ClipColor = Color.Crimson;
		protected Pen						m_Pen = null;
		protected Brush						m_Brush = null;
		protected Pen						m_PenSelected = null;
		protected Brush						m_BrushSelected = null;
		protected Pen						m_PenSelectedInterval = null;
		protected Brush						m_BrushSelectedInterval = null;
		protected Pen						m_PenSmallGraduations = null;
		protected Brush						m_BrushSmallGraduations = null;
		protected Pen						m_PenMainGraduations = null;
		protected Brush						m_BrushMainGraduations = null;
		protected Pen						m_PenCursorTime = null;
		protected Pen						m_PenClip = null;

		// Cached list of drawn intervals
		protected List<DrawnInterval>		m_Intervals = new List<DrawnInterval>();

		#endregion

		#region PROPERTIES

		[Browsable( false )]
		public AnimationEditorControl	Owner		{ get { return m_Owner; } set { m_Owner = value; } }

		[Browsable( false )]
		public float			RangeMin			{ get { return m_RangeMin; } }

		[Browsable( false )]
		public float			RangeMax			{ get { return m_RangeMax; } }

		[Browsable( false )]
		public float			VerticalRangeMin	{ get { return m_VerticalRangeMin; } }

		[Browsable( false )]
		public float			VerticalRangeMax	{ get { return m_VerticalRangeMax; } }

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

				if ( !m_bPreventRefresh )
					Refresh();
			}
		}

		[Browsable( false )]
		public Sequencor.AnimationTrack.Key			HoveredKey
		{
			get { return m_HoveredKey; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_HoveredKey )
					return;

				m_HoveredKey = value;

				if ( !m_bPreventRefresh )
					Refresh();
			}
		}

		[Browsable( false )]
		public Sequencor.AnimationTrack.Key			HoveredKeyTangentIn
		{
			get { return m_HoveredKeyTangentIn; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_HoveredKeyTangentIn )
					return;

				m_HoveredKeyTangentIn = value;

				if ( !m_bPreventRefresh )
					Refresh();
			}
		}

		[Browsable( false )]
		public Sequencor.AnimationTrack.Key			HoveredKeyTangentOut
		{
			get { return m_HoveredKeyTangentOut; }
			set
			{
				if ( DesignMode )
					return;

				if ( value == m_HoveredKeyTangentOut )
					return;

				m_HoveredKeyTangentOut = value;

				if ( !m_bPreventRefresh )
					Refresh();
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
		public Color		SmallGraduationsColor
		{
			get { return m_SmallGraduationsColor; }
			set
			{
				m_SmallGraduationsColor = value;

				// Rebuild brush and pens
				if ( m_BrushSmallGraduations != null )
					m_BrushSmallGraduations.Dispose();
				if ( m_PenSelectedInterval != null )
					m_PenSelectedInterval.Dispose();
				m_BrushSmallGraduations = new SolidBrush( m_SmallGraduationsColor );
				m_PenSmallGraduations = new Pen( m_SmallGraduationsColor, 1.0f );
			}
		}

		[Category( "Appearance" )]
		public Color		MainGraduationsColor
		{
			get { return m_MainGraduationsColor; }
			set
			{
				m_MainGraduationsColor = value;

				// Rebuild brush and pens
				if ( m_BrushMainGraduations != null )
					m_BrushMainGraduations.Dispose();
				if ( m_PenSelectedInterval != null )
					m_PenSelectedInterval.Dispose();
				m_BrushMainGraduations = new SolidBrush( m_MainGraduationsColor );
				m_PenMainGraduations = new Pen( m_MainGraduationsColor, 1.0f );
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

		[Category( "Appearance" )]
		public Color		ClipLinesColor
		{
			get { return m_ClipColor; }
			set
			{
				m_ClipColor = value;

				// Rebuild brush and pens
				if ( m_PenClip != null )
					m_PenClip.Dispose();
				m_PenClip = new Pen( m_ClipColor, 1.0f );
				m_PenClip.DashStyle = DashStyle.Dot;
			}
		}

// 		[Category( "Custom Paint" )]
// 		public event CustomIntervalPaintEventHandler	CustomIntervalPaint;
//
// 		[Category( "Key" )]
// 		[Browsable( true )]
// 		public new event KeyEventHandler	KeyDown;

		public bool		PreventRefresh
		{
			get { return m_bPreventRefresh; }
			set { m_bPreventRefresh = value; }
		}

		public Pen		PenCursorTime
		{
			get { return m_PenCursorTime; }
			set { m_PenCursorTime = value; }
		}

		public event EventHandler		RangeChanged;

		#endregion

		#region METHODS

		public	AnimationTrackPanel()
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
			SmallGraduationsColor = SmallGraduationsColor;
			MainGraduationsColor = MainGraduationsColor;
			CursorTimeColor = CursorTimeColor;
			ClipLinesColor = ClipLinesColor;
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
			m_PenSmallGraduations.Dispose();
			m_BrushSmallGraduations.Dispose();
			m_PenMainGraduations.Dispose();
			m_BrushMainGraduations.Dispose();

			base.Dispose( disposing );
		}

		/// <summary>
		/// Sets both min and max range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetHorizontalRange( float _RangeMin, float _RangeMax )
		{
			if ( Math.Abs( _RangeMin - m_RangeMin ) < 1e-3f && Math.Abs( _RangeMax - m_RangeMax ) < 1e-3f )
				return;	// No change...

			m_RangeMin = _RangeMin;
			m_RangeMax = _RangeMax;

			if ( RangeChanged != null )
				RangeChanged( this, EventArgs.Empty );

			if ( !m_bPreventRefresh )
				Refresh();
		}

		/// <summary>
		/// Sets both min and max range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetVerticalRange( float _RangeMin, float _RangeMax )
		{
			if ( Math.Abs( _RangeMin - m_VerticalRangeMin ) < 1e-3f && Math.Abs( _RangeMax - m_VerticalRangeMax ) < 1e-3f )
				return;	// No change...

			m_VerticalRangeMin = _RangeMin;
			m_VerticalRangeMax = _RangeMax;

			if ( RangeChanged != null )
				RangeChanged( this, EventArgs.Empty );

			if ( !m_bPreventRefresh )
				Refresh();
		}

		#region Control Members

		protected float		m_ButtonDownVerticalRangeMin;
		protected float		m_ButtonDownVerticalRangeMax;
		protected Point		m_ButtonDownPosition;
		protected MouseButtons	m_MouseButtonsDown = MouseButtons.None;

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );
			Focus();

			m_ButtonDownVerticalRangeMin = m_VerticalRangeMin;
			m_ButtonDownVerticalRangeMax = m_VerticalRangeMax;
			m_ButtonDownPosition = e.Location;
			m_MouseButtonsDown |= e.Button;
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );

			if ( m_MouseButtonsDown == MouseButtons.Middle )
			{	// Simulate vertical panning
				float	Delta = (e.Y - m_ButtonDownPosition.Y) * (m_ButtonDownVerticalRangeMax - m_ButtonDownVerticalRangeMin) / Height;
				SetVerticalRange( m_ButtonDownVerticalRangeMin + Delta, m_ButtonDownVerticalRangeMax + Delta );
			}
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );
			Focus();

			m_MouseButtonsDown &= ~e.Button;
		}

		protected override void OnMouseWheel( MouseEventArgs e )
		{
			if ( (Control.ModifierKeys & (Keys.Shift | Keys.Control)) != 0 )
			{	// Update vertical visual range
				float	VerticalRange = m_VerticalRangeMax - m_VerticalRangeMin;
				float	CenterY = m_VerticalRangeMax - VerticalRange * e.Y / Height;

				float	fZoomFactor = 1.0f;
				if ( e.Delta > 0 )
					fZoomFactor = 1.0f / (1.0f + VERTICAL_ZOOM_FACTOR * e.Delta);
				else
					fZoomFactor = 1.0f + VERTICAL_ZOOM_FACTOR * -e.Delta;

				SetVerticalRange( CenterY + fZoomFactor * (m_VerticalRangeMin - CenterY), CenterY + fZoomFactor * (m_VerticalRangeMax - CenterY) );

				if ( (Control.ModifierKeys & Keys.Shift) != 0 )
					return;
			}

			base.OnMouseWheel( e );
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
			if ( m_Track == null )
				return;
			if ( m_RangeMax - m_RangeMin < 1e-3f )
				return;	// Can't trace invalid range!
			if ( m_VerticalRangeMax - m_VerticalRangeMin < 1e-3f )
				return;	// Can't trace invalid range!

			// Rebuild intervals
			RebuildIntervals();

			// Draw background
			foreach ( DrawnInterval DI in m_Intervals )
				DI.DrawBackground( e.Graphics );

			// Draw graduations
			float	GraduationSizeX = (float) Math.Pow( 10.0, Math.Floor( Math.Log10( m_RangeMax - m_RangeMin ) ) );
			float	GraduationStartX = (float) Math.Floor( m_RangeMin / GraduationSizeX ) * GraduationSizeX;
			float	GraduationSizeY = (float) Math.Pow( 10.0, Math.Floor( Math.Log10( m_VerticalRangeMax - m_VerticalRangeMin ) ) );
			float	GraduationStartY = (float) Math.Floor( m_VerticalRangeMin / GraduationSizeY ) * GraduationSizeY;

			for ( int GradIndex=0; GradIndex < 10; GradIndex++ )
			{
				float	GradX = ((GraduationStartX + GradIndex * GraduationSizeX) - m_RangeMin) * Width / (m_RangeMax - m_RangeMin);
				float	GradY = ((GraduationStartY + GradIndex * GraduationSizeY) - m_VerticalRangeMax) * Height / (m_VerticalRangeMin - m_VerticalRangeMax);
				e.Graphics.DrawLine( m_PenSmallGraduations, 0, GradY, Width, GradY );
				e.Graphics.DrawLine( m_PenSmallGraduations, GradX, 0, GradX, Height );
			}

			// Draw main graduations
			e.Graphics.DrawLine( m_PenMainGraduations, 10, 0, 10, Height );

			float	OrdinatesPosition = (0.0f - m_VerticalRangeMax) * Height / (m_VerticalRangeMin - m_VerticalRangeMax);
			OrdinatesPosition = Math.Max( 10.0f, Math.Min( Height-20.0f, OrdinatesPosition ) );

			e.Graphics.DrawLine( m_PenMainGraduations, 0, OrdinatesPosition, Width, OrdinatesPosition );

			for ( int GradIndex=0; GradIndex < 10; GradIndex++ )
			{
				float	GradX = ((GraduationStartX + GradIndex * GraduationSizeX) - m_RangeMin) * Width / (m_RangeMax - m_RangeMin);
				float	GradY = ((GraduationStartY + GradIndex * GraduationSizeY) - m_VerticalRangeMax) * Height / (m_VerticalRangeMin - m_VerticalRangeMax);

				e.Graphics.DrawLine( m_PenMainGraduations, GradX, OrdinatesPosition-4, GradX, OrdinatesPosition+4 );
				float	ValueX = GraduationStartX + GradIndex * GraduationSizeX;
				e.Graphics.DrawString( ValueX.ToString( "G4" ), Font, m_BrushMainGraduations, GradX, OrdinatesPosition+6 );

				e.Graphics.DrawLine( m_PenMainGraduations, 10-4, GradY, 10+4, GradY );
				float	ValueY = GraduationStartY + GradIndex * GraduationSizeY;
				e.Graphics.DrawString( ValueY.ToString( "G4" ), Font, m_BrushMainGraduations, 10+5, GradY-6 );
			}

 			// Draw the intervals...
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

		protected Color	ComputeBackColor( Color _C )
		{
			return Color.FromArgb( (int) (0.8f * _C.R), (int) (0.8f * _C.G), (int) (0.8f * _C.B) );
		}

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
		/// Attempts to retrieve the key if we're pointing to its IN tangent
		/// </summary>
		/// <param name="_ClientPosition">The position in CLIENT space</param>
		/// <returns>The key at this position</returns>
		public Sequencor.AnimationTrackFloat.KeyFloat	GetKeyTangentInAt( Point _ClientPosition )
		{
			foreach ( DrawnInterval DI in m_Intervals )
			{
				Sequencor.AnimationTrackFloat.KeyFloat	Key = DI.GetKeyTangentInAt( _ClientPosition );
				if ( Key != null )
					return Key;
			}

			return null;
		}

		/// <summary>
		/// Attempts to retrieve the key if we're pointing to its OUT tangent
		/// </summary>
		/// <param name="_ClientPosition">The position in CLIENT space</param>
		/// <returns>The key at this position</returns>
		public Sequencor.AnimationTrackFloat.KeyFloat	GetKeyTangentOutAt( Point _ClientPosition )
		{
			foreach ( DrawnInterval DI in m_Intervals )
			{
				Sequencor.AnimationTrackFloat.KeyFloat	Key = DI.GetKeyTangentOutAt( _ClientPosition );
				if ( Key != null )
					return Key;
			}

			return null;
		}

		/// <summary>
		/// Creates a new key at provided position
		/// </summary>
		/// <param name="_SequencePosition">The position in SEQUENCE space</param>
		/// <param name="_KeyType">The type of key to create</param>
		/// <param name="_Value">An optional generic value to create the parameter with:
		/// KeyType			ObjectType
		/// BOOL			bool
		/// EVENT			int (event GUID)
		/// FLOAT			-> _SequencePosition.Y used as value
		/// FLOAT2			-> _SequencePosition.Y used as value
		/// FLOAT3			-> _SequencePosition.Y used as value
		/// FLOAT4			-> _SequencePosition.Y used as value
		/// PRS:POSITION	Vector3
		/// PRS:ROTATION	Use CreateRotationKeyAt() instead !
		/// PRS:SCALE		Vector3
		/// </param>
		public void			CreateKeyAt( float _SequencePosition, KEY_TYPE _KeyType, object _Value )
		{
			if ( m_SelectedInterval == null )
				throw new Exception( "There is no currently selected interval to create a key into !" );

			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					(m_SelectedInterval[0] as Sequencor.AnimationTrackBool).AddKey( _SequencePosition, (bool) _Value );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					(m_SelectedInterval[0] as Sequencor.AnimationTrackEvent).AddKey( _SequencePosition, (int) _Value );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					(m_SelectedInterval[0] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, (float) _Value );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					{
						SharpDX.Vector2	Value = (SharpDX.Vector2) _Value;
						(m_SelectedInterval[0] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.X );
						(m_SelectedInterval[1] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Y );
					}
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					{
						SharpDX.Vector3	Value = (SharpDX.Vector3) _Value;
						(m_SelectedInterval[0] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.X );
						(m_SelectedInterval[1] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Y );
						(m_SelectedInterval[2] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Z );
					}
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					{
						SharpDX.Vector4	Value = (SharpDX.Vector4) _Value;
						(m_SelectedInterval[0] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.X );
						(m_SelectedInterval[1] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Y );
						(m_SelectedInterval[2] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Z );
						(m_SelectedInterval[3] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.W );
					}
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					switch ( _KeyType )
					{
						case KEY_TYPE.POSITION:
							{
								SharpDX.Vector3	Value = (SharpDX.Vector3) _Value;
								(m_SelectedInterval[0] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.X );
								(m_SelectedInterval[1] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Y );
								(m_SelectedInterval[2] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Z );
							}
							break;
						case KEY_TYPE.ROTATION:
							throw new Exception( "Use CreateRotationKeyAt() to create a rotation key !" );

						case KEY_TYPE.SCALE:
							{
								SharpDX.Vector3	Value = (SharpDX.Vector3) _Value;
								(m_SelectedInterval[4] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.X );
								(m_SelectedInterval[5] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Y );
								(m_SelectedInterval[6] as Sequencor.AnimationTrackFloat).AddKey( _SequencePosition, Value.Z );
							}
							break;
						default:
							throw new Exception( "Can't create a DEFAULT type key in a PRS animation track : you must choose between POSITION, ROTATION or SCALE !" );
					}
					break;
			}

			// Re-evaluate track time to account for the new key
			m_Track.SetTime( m_Owner.Owner.TimeLineControl.CursorPosition );
		}

		/// <summary>
		/// Creates a new rotation key from an Angle + Axis couple
		/// </summary>
		/// <param name="_SequencePosition"></param>
		/// <param name="_Angle"></param>
		/// <param name="_Axis"></param>
		public void			CreateRotationKeyAt( float _SequencePosition, float _Angle, SharpDX.Vector3 _Axis )
		{
			if ( m_SelectedInterval == null )
				throw new Exception( "There is no currently selected interval to create a key into !" );

			// We attack the 4th animation track which MUST be a Quaternion track
			(m_SelectedInterval[3] as Sequencor.AnimationTrackQuat).AddKey( _SequencePosition, _Angle, _Axis );

			// Re-evaluate track time to account for the new key
			m_Track.SetTime( m_Owner.Owner.TimeLineControl.CursorPosition );
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

		/// <summary>
		/// Converts a client position into a sequence time & value
		/// </summary>
		/// <param name="_fClientPositionX">The CLIENT SPACE X position</param>
		/// <param name="_fClientPositionY">The CLIENT SPACE Y position</param>
		/// <returns>The equivalent sequence time & value</returns>
		public PointF		ClientToSequenceTime( float _fClientPositionX, float _ClientPositionY )
		{
			return new PointF(	m_RangeMin + (m_RangeMax - m_RangeMin) * _fClientPositionX / Width,
								m_VerticalRangeMax + (m_VerticalRangeMin - m_VerticalRangeMax) * _ClientPositionY / Height );
		}

		/// <summary>
		/// Converts a sequence time into a client position
		/// </summary>
		/// <param name="_fSequenceTimeX">The sequence time</param>
		/// <param name="_fSequenceValue">The sequence value</param>
		/// <returns>The equivalent CLIENT SPACE position</returns>
		public PointF		SequenceTimeToClient( float _fSequenceTime, float _fSequenceValue )
		{
			return new PointF( SequenceTimeToClient( _fSequenceTime ), (m_VerticalRangeMax - _fSequenceValue) * Height / (m_VerticalRangeMax - m_VerticalRangeMin) );
		}

		/// <summary>
		/// Converts a sequence space tangent position into a client space tangent position
		/// </summary>
		/// <param name="_Key"></param>
		/// <returns></returns>
		public PointF		TangentInSequenceToClient( Sequencor.AnimationTrackFloat.KeyFloat _Key )
		{
			float	IntervalDuration = _Key.ParentAnimationTrack.ParentInterval.Duration;
			PointF	SequencePosition = new PointF( _Key.TrackTime - IntervalDuration * _Key.TangentIn.X, _Key.Value - _Key.TangentIn.Y );
			return SequenceTimeToClient( SequencePosition.X, SequencePosition.Y );
		}

		/// <summary>
		/// Converts a client space tangent position into a sequence space tangent position
		/// </summary>
		/// <param name="_Key"></param>
		/// <param name="_ClientPos"></param>
		/// <returns></returns>
		public void			TangentInClientToSequence( Sequencor.AnimationTrackFloat.KeyFloat _Key, PointF _ClientPos )
		{
			float	IntervalDuration = _Key.ParentAnimationTrack.ParentInterval.Duration;
			PointF	SequencePosition = ClientToSequenceTime( _ClientPos.X, _ClientPos.Y );

			SharpDX.Vector2	NewTangent = new SharpDX.Vector2( -(SequencePosition.X - _Key.TrackTime) / IntervalDuration, _Key.Value - SequencePosition.Y );

// 			// Check for align with tangent OUT
// 			SharpDX.Vector2	OtherTangent = _Key.TangentOut;
// 			if ( NewTangent.LengthSquared() > 1e-6f && OtherTangent.LengthSquared() > 1e-6f )
// 			{
// 				SharpDX.Vector2	ThisTangent = NewTangent;
// 				float	ThisTangentLength = ThisTangent.Length();
// 				ThisTangent /= ThisTangentLength;
// 				float	OtherTangentLength = OtherTangent.Length();
// 				OtherTangent /= OtherTangentLength;
// 
// 				// Check for length sync with tangent OUT
// 				float	DeltaLengthSequence = _Key.ParentAnimationTrack.ParentInterval.Duration * Math.Abs( OtherTangentLength - ThisTangentLength );
// 				float	DeltaLengthClient = SequenceTimeToClient( DeltaLengthSequence );
// 				if ( DeltaLengthClient < KEY_HALF_GRAB_SIZE )
// 					ThisTangentLength = OtherTangentLength;
// 
// 				// Check for phase sync with tangent OUT
// 				float	fPhase = (float) Math.Acos( SharpDX.Vector2.Dot( ThisTangent, OtherTangent ) );
// 				if ( fPhase < TANGENT_PHASE_SYNC )
// 					ThisTangent = OtherTangent;
// 				else if ( fPhase > (float) Math.PI - TANGENT_PHASE_SYNC )
// 					ThisTangent = -OtherTangent;
// 
// 				NewTangent = ThisTangentLength * ThisTangent;
// 			}

			_Key.TangentIn = NewTangent;
		}

		/// <summary>
		/// Converts a sequence space tangent position into a client space tangent position
		/// </summary>
		/// <param name="_Key"></param>
		/// <returns></returns>
		public PointF		TangentOutSequenceToClient( Sequencor.AnimationTrackFloat.KeyFloat _Key )
		{
			float	IntervalDuration = _Key.ParentAnimationTrack.ParentInterval.Duration;
			PointF	SequencePosition = new PointF( _Key.TrackTime + IntervalDuration * _Key.TangentOut.X, _Key.Value + _Key.TangentOut.Y );
			return SequenceTimeToClient( SequencePosition.X, SequencePosition.Y );
		}

		/// <summary>
		/// Converts a client space tangent position into a sequence space tangent position
		/// </summary>
		/// <param name="_Key"></param>
		/// <param name="_ClientPos"></param>
		/// <returns></returns>
		public void			TangentOutClientToSequence( Sequencor.AnimationTrackFloat.KeyFloat _Key, PointF _ClientPos )
		{
			float	IntervalDuration = _Key.ParentAnimationTrack.ParentInterval.Duration;
			PointF	SequencePosition = ClientToSequenceTime( _ClientPos.X, _ClientPos.Y );

			SharpDX.Vector2	NewTangent = new SharpDX.Vector2( (SequencePosition.X - _Key.TrackTime) / IntervalDuration, SequencePosition.Y - _Key.Value );

// 			// Check for align with tangent IN
// 			SharpDX.Vector2	OtherTangent = _Key.TangentIn;
// 			if ( NewTangent.LengthSquared() > 1e-6f && OtherTangent.LengthSquared() > 1e-6f )
// 			{
// 				SharpDX.Vector2	ThisTangent = NewTangent;
// 				float	ThisTangentLength = ThisTangent.Length();
// 				ThisTangent /= ThisTangentLength;
// 				float	OtherTangentLength = OtherTangent.Length();
// 				OtherTangent /= OtherTangentLength;
// 
// 				// Check for length sync with tangent IN
// 				float	DeltaLengthSequence = _Key.ParentAnimationTrack.ParentInterval.Duration * Math.Abs( OtherTangentLength - ThisTangentLength );
// 				float	DeltaLengthClient = SequenceTimeToClient( DeltaLengthSequence );
// 				if ( DeltaLengthClient < KEY_HALF_GRAB_SIZE )
// 					ThisTangentLength = OtherTangentLength;
// 
// 				// Check for phase sync with tangent IN
// 				float	fPhase = (float) Math.Acos( SharpDX.Vector2.Dot( ThisTangent, OtherTangent ) );
// 				if ( fPhase < TANGENT_PHASE_SYNC )
// 					ThisTangent = OtherTangent;
// 				else if ( fPhase > (float) Math.PI - TANGENT_PHASE_SYNC )
// 					ThisTangent = -OtherTangent;
// 
// 				NewTangent = ThisTangentLength * ThisTangent;
// 			}

			_Key.TangentOut = NewTangent;
		}

		protected DrawnInterval	FindInterval( Sequencor.ParameterTrack.Interval _Interval )
		{
			if ( _Interval == null )
				return	null;

			foreach ( DrawnInterval DI in m_Intervals )
				if ( DI.Interval == _Interval )
					return	DI;

			return	null;
		}

		protected void	RebuildIntervals()
		{
			// Rebuild intervals
			foreach ( DrawnInterval DI in m_Intervals )
				DI.Dispose();

			m_Intervals.Clear();
			foreach ( Sequencor.ParameterTrack.Interval Interval in m_Track.Intervals )
				m_Intervals.Add( new DrawnInterval( this, Interval ) );
		}

		#endregion

		#region EVENT HANDLERS

		protected void Track_IntervalsChanged( object sender, EventArgs e )
		{
//			RebuildIntervals();
			Invalidate();
		}

		#endregion
	}
}
