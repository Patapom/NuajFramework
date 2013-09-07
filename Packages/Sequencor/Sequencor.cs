using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;

namespace SequencorLib
{
	/// <summary>
	/// The Sequencer class that allows you to synchronize and animate your effects
	/// It features a collection of parameter tracks that contain animation intervals.
	/// Each interval contains a list of animation tracks with keys that can be of the following type :
	///		BOOL, true of false
	///		EVENT, triggers an event
	///		INT, integers
	///		FLOAT, single floating point value
	///		FLOAT2, 2 floating point values
	///		FLOAT3, 3 floating point values
	///		FLOAT4, 4 floating point values
	///		PRS, Position / Rotation / Scale track
	///	
	/// Each type (except BOOL, INTEGER and EVENT) can be animated using linear or bicubic interpolation.
	/// 
	/// The hierarchy is like this :
	/// 
	/// * Sequencor
	///   * ParameterTrack0 (Some parameter of the types enumerated above)
	///		* Interval0 (TimeStart, TimeEnd)
	///			* AnimationTrack0 (Single float, boolean, event or quaternion)
	///				*Key0 *Key1 *Key2 (...)
	///			* AnimationTrack1 (if a float2, float3, float4 or PRS)
	///				*Key0 *Key1 *Key2 (...)
	///			* AnimationTrack2 (if a float3, float4 or PRS)
	///				*Key0 *Key1 *Key2 (...)
	///			(...) (as many animation tracks as components in the parameter)
	///		* Interval1 (TimeStart >= Interval0.TimeStart, TimeEnd)
	///		(...)
	///			
	///   * ParameterTrack1 (Some other parameter)
	///	  (etc.)
	/// 
	/// You can receive the following events :
	///		* IntervalStart, we entered a new interval
	///		* IntervalEnd, we exited an interval
	///		* ValueChanged, a single value of a parameter changed (that can be individually X, Y or Z of a float3)
	///		* ParameterChanged, the global value of the parameter changed (there is no specifics about the animation track that changed, use ValueChanged for that)
	///	
	/// All these events exist at different levels :
	///		* AnimationTrack, you can subscribe to events for all animation intervals in this track
	///		* ParameterTrack, you can subscribe to events for all animations tracks in this parameter
	///		* Sequencor, you can subscribe to events for all animations of all animation tracks of all parameters (this is an aggregation of all higher level events)
	///		
	/// A really simple way to monitor parameters is to subscribe to the Sequencor :
	/// 
	/// Sequencor	S = new Sequencor();
	/// S.ParameterChanged += new ParameterChangedEventHandler(
	///		( ParameterTrack _Parameter, ParameterTrack.Interval _Interval ) =>
	///		{
	///			switch ( _Parameter.GUID )
	///			{
	///				case	0:	// Handle new value for parameter 0 (a float2)
	///					MyParam0 = _Parameter.ValueAsFloat2;
	///					break;
	///				case	1:	// Handle new value for parameter 1 (a boolean)
	///					MyParam1 = _Parameter.ValueAsBool;
	///					break;
	///			}
	///		} );
	/// </summary>
	public class Sequencor : ISerializable
	{
		#region CONSTANTS

		protected const float	DEFAULT_TANGENT_X = 0.05f;
		protected const float	DEFAULT_TANGENT_Y = 0.0f;
		protected const float	TANGENT_LENGTH_FACTOR = 2.0f;

		#endregion

		#region NESTED TYPES

		public delegate object	TagNeededEventHander( ParameterTrack _Parameter );
		public delegate void	IntervalEventHandler( ParameterTrack _Parameter, ParameterTrack.Interval _Interval );
		public delegate void	ParameterChangedEventHandler( ParameterTrack _Parameter, ParameterTrack.Interval _Interval );
		public delegate void	ValueChangedEventHandler( ParameterTrack _Parameter, ParameterTrack.Interval _Interval, AnimationTrack _Track );
		public delegate void	EventFiredEventHandler( ParameterTrack _Parameter, ParameterTrack.Interval _Interval, AnimationTrack _Track, int _EventGUID );

		#region Animation Tracks

		public abstract class	AnimationTrack : ISerializable
		{
			#region NESTED TYPES

			public delegate void	KeyValueChangedEventHandler( AnimationTrack _Sender, Key _Key );

			/// <summary>
			/// An animation key
			/// </summary>
			public class	Key : IComparable<Key>, ISerializable
			{
				#region FIELDS

				protected AnimationTrack	m_Owner = null;
				protected float				m_Time = 0.0f;

				#endregion

				#region PROPERTIES

				/// <summary>
				/// Gets the owner animation track
				/// </summary>
				public AnimationTrack	ParentAnimationTrack	{ get { return m_Owner; } }

				/// <summary>
				/// Gets the index of this key in its parent track
				/// </summary>
				public int			Index		{ get { return m_Owner.IndexOf( this ); } }

				/// <summary>
				/// Gets or sets this key position in normalized interval time
				/// </summary>
				public float		NormalizedTime	{ get { return m_Time; } set { m_Time = value; m_Owner.SortKeys(); m_Owner.NotifyKeyValueChanged( this ); } }

				/// <summary>
				/// Gets or sets the time in standard track time
				/// </summary>
				public float		TrackTime
				{
					get { return m_Owner.m_Owner.TimeStart + (m_Owner.m_Owner.TimeEnd - m_Owner.m_Owner.TimeStart) * m_Time; }
					set
					{
						float	NewTime = (value - m_Owner.m_Owner.TimeStart) / (m_Owner.m_Owner.TimeEnd - m_Owner.m_Owner.TimeStart);
						NormalizedTime = Math.Max( 0.0f, Math.Min( 1.0f, NewTime ) );
					}
				}

				#endregion

				#region METHODS

				public Key( AnimationTrack _Owner )
				{
					m_Owner = _Owner;
				}

				public override string ToString()
				{
					float	TrackTime = this.TrackTime;
					return m_Owner.m_Title + "Key #" + Index + " [" + TrackTime.ToString( "G4" ) + "]";
				}

				#region IComparable<Key> Members

				public int CompareTo( Key other )
				{
					return other.m_Time < m_Time ? +1 : (other.m_Time > m_Time ? -1 : 0);
				}

				#endregion

				#region ISerializable Members

				public virtual void Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Time );
				}

				public virtual void Load( System.IO.BinaryReader _Reader )
				{
					m_Time = _Reader.ReadSingle();
				}

				#endregion

				#endregion
			}

			#endregion

			#region FIELDS

			protected ParameterTrack.Interval	m_Owner = null;
			protected string		m_Title = "";
			protected List<Key>		m_Keys = new List<Key>();

			// Currently evaluated interval
			protected int			m_CurrentKeyIndex = 0;
			protected Key			m_CurrentKey = null;	// Current key : the evaluation interval stands between current & next key
			protected Key			m_NextKey = null;		// Next key (if none exist, equal to the current key)

			#endregion

			#region PROPERTIES

			/// <summary>
			/// Gets the track's parent interval
			/// </summary>
			public ParameterTrack.Interval	ParentInterval { get { return m_Owner; } }

			/// <summary>
			/// Gets the amount of keys in that track
			/// </summary>
			public int		KeysCount	{ get { return m_Keys.Count; } }

			/// <summary>
			/// Gets the list of keys in that track
			/// </summary>
			public Key[]	Keys		{ get { return m_Keys.ToArray(); } }

			/// <summary>
			/// Gets a specific key in the list of keys
			/// </summary>
			/// <param name="_Index"></param>
			/// <returns></returns>
			public Key		this[int _Index]	{ get { return m_Keys[_Index]; } }

			public AnimationTrackBool	AsBool	{ get {return this as AnimationTrackBool; } }
			public AnimationTrackEvent	AsEvent	{ get {return this as AnimationTrackEvent; } }
			public AnimationTrackFloat	AsFloat	{ get {return this as AnimationTrackFloat; } }
			public AnimationTrackQuat	AsQuat	{ get {return this as AnimationTrackQuat; } }

			/// <summary>
			/// Occurs when the list of keys changed
			/// </summary>
			public event EventHandler	KeysChanged;

			/// <summary>
			/// Occurs when the value or time of a key changed
			/// </summary>
			public event KeyValueChangedEventHandler	KeyValueChanged;

			#endregion

			#region METHODS

			public AnimationTrack( ParameterTrack.Interval _Owner, string _Title )
			{
				m_Owner = _Owner;
				m_Title = _Title;
			}

			/// <summary>
			/// Creates a new key from a binary content created using the Save() method
			/// </summary>
			/// <param name="_Reader"></param>
			/// <returns></returns>
			public Key	CreateKey( System.IO.BinaryReader _Reader )
			{
				Key	Result = CreateKey();
				m_Keys.Add( Result );

				// Load into new key
				Result.Load( _Reader );

				// Sort
				SortKeys();

				// Notify
				NotifyKeysChanged();

				return Result;
			}

			/// <summary>
			/// Removes an animation key
			/// </summary>
			/// <param name="_Key"></param>
			public void	RemoveKey( Key _Key )
			{
				if ( !m_Keys.Contains( _Key ) )
					return;

				m_Keys.Remove( _Key );

				// Notify
				NotifyKeysChanged();
			}

			/// <summary>
			/// Returns the index of a given key
			/// </summary>
			/// <param name="_Key"></param>
			/// <returns></returns>
			public int IndexOf( Key _Key )
			{
				return m_Keys.IndexOf( _Key );
			}

			/// <summary>
			/// Clones a source key
			/// </summary>
			/// <param name="_Key"></param>
			/// <returns></returns>
			public Key	Clone( Key _Key )
			{
				// Save to memory
				System.IO.MemoryStream	Buffer = new System.IO.MemoryStream();
				System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Buffer );
				System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Buffer );
				_Key.Save( Writer );
				Buffer.Position = 0;

				// Load into new key
				Key	Result = CreateKey( Reader );

				// Dispose of memory
				Writer.Dispose();
				Reader.Dispose();
				Buffer.Dispose();

				return Result;
			}

			/// <summary>
			/// Animates the interval for the provided time interval (_NewTime > _OldTime)
			/// </summary>
			/// <param name="_OldTime">The former NORMALIZED time</param>
			/// <param name="_NewTime">The new NORMALIZED time</param>
			/// <returns>true if the key interval changed</returns>
			/// <remarks>This method assumes an initial SetTime() has been performed</remarks>
			internal virtual bool	AnimateSequenceForward( float _OldTime, float _NewTime )
			{
				if ( m_Keys.Count == 0 )
					return false;
				if ( _OldTime < m_CurrentKey.NormalizedTime && _NewTime >= m_CurrentKey.NormalizedTime )
					FireEvent( m_CurrentKey );	// First event
				if ( _NewTime < m_NextKey.NormalizedTime )
					return false;	// Still before the next key... No change !

				while ( _NewTime >= m_NextKey.NormalizedTime )
				{
					if ( m_CurrentKeyIndex >= m_Keys.Count-2 )
					{	// We've used all keys. We're now done with this interval...
						if ( _OldTime < m_NextKey.NormalizedTime && _NewTime >= m_NextKey.NormalizedTime )
							FireEvent( m_NextKey );	// Last event
						return false;
					}

					// Get the next key
					m_CurrentKey = m_NextKey;
					m_CurrentKeyIndex++;
					m_NextKey = m_Keys[m_CurrentKeyIndex+1];

					// Fire the event key
					FireEvent( m_CurrentKey );
				}

				return true;
			}

			/// <summary>
			/// Animates the interval during the provided time interval (_NewTime < _OldTime)
			/// </summary>
			/// <param name="_OldTime">The former NORMALIZED time</param>
			/// <param name="_NewTime">The new NORMALIZED time</param>
			/// <returns>true if the key interval changed</returns>
			/// <remarks>This method assumes an initial SetTime() has been performed</remarks>
			internal virtual bool	AnimateSequenceBackward( float _OldTime, float _NewTime )
			{
				if ( m_Keys.Count == 0 )
					return false;
				if ( _OldTime > m_NextKey.NormalizedTime && _NewTime <= m_NextKey.NormalizedTime )
					FireEvent( m_NextKey );	// First event
				if ( _NewTime >= m_CurrentKey.NormalizedTime )
					return false;	// Still after the current key... No change !

				while ( _NewTime < m_CurrentKey.NormalizedTime )
				{
					if ( m_CurrentKeyIndex <= 0 )
					{	// We've used all intervals. We're now done with this track...
						if ( _OldTime >= m_CurrentKey.NormalizedTime && _NewTime < m_CurrentKey.NormalizedTime )
							FireEvent( m_CurrentKey );	// Last event !
						return false;
					}

					// Fire the event key
					FireEvent( m_CurrentKey );

					// Get the previous key
					m_NextKey = m_CurrentKey;
					m_CurrentKey = m_Keys[--m_CurrentKeyIndex];
				}

				return true;
			}

			/// <summary>
			/// Sets the absolute interval time without animation (simple parameters evaluation, no event triggering)
			/// </summary>
			/// <param name="_NewTime">The new NORMALIZED time</param>
			internal virtual void SetTime( float _NewTime )
			{
				if ( m_Keys.Count == 0 )
				{	// No key...
					m_CurrentKeyIndex = 0;
					m_CurrentKey = m_NextKey = null;
					return;
				}

				if ( _NewTime <= m_Keys[0].NormalizedTime )
				{	// We're before the first key
					m_CurrentKeyIndex = 0;
					m_CurrentKey = m_Keys[0];
					m_NextKey = m_Keys.Count > 1 ? m_Keys[1] : m_CurrentKey;
				}
				else if ( _NewTime >= m_Keys[m_Keys.Count-1].NormalizedTime )
				{	// We're after the last key
					m_CurrentKeyIndex = m_Keys.Count > 1 ? m_Keys.Count-2 : m_Keys.Count-1;
					m_CurrentKey = m_Keys[m_CurrentKeyIndex];
					m_NextKey = m_Keys[m_Keys.Count-1];
				}
				else
				{	// We're somewhere within a key interval
					for ( m_CurrentKeyIndex=0; m_CurrentKeyIndex < m_Keys.Count-1; m_CurrentKeyIndex++ )
					{
						m_CurrentKey = m_Keys[m_CurrentKeyIndex];
						m_NextKey = m_Keys[m_CurrentKeyIndex+1];
						if ( _NewTime >= m_CurrentKey.NormalizedTime && _NewTime <= m_NextKey.NormalizedTime )
							return;	// Found the interval we're in !
					}
				}
			}

			/// <summary>
			/// Re-order keys
			/// </summary>
			protected void	SortKeys()
			{
				m_Keys.Sort();
			}

			/// <summary>
			/// Notifies the list of keys changed
			/// </summary>
			protected void	NotifyKeysChanged()
			{
				if ( KeysChanged != null )
					KeysChanged( this, EventArgs.Empty );
			}

			/// <summary>
			/// Notifies a key value or time changed
			/// </summary>
			internal void	NotifyKeyValueChanged( Key _Caller )
			{
				if ( KeyValueChanged != null )
					KeyValueChanged( this, _Caller );
			}

			/// <summary>
			/// Creates a new key of the appropriate type (implemented by typed child classes)
			/// </summary>
			/// <returns></returns>
			protected abstract Key	CreateKey();

			/// <summary>
			/// Fires the event associated to the specified key (used only by KeyEvents)
			/// </summary>
			/// <param name="_Key"></param>
			protected virtual void	FireEvent( Key _Key )
			{
				// Overridden in EVENT track
			}

			#region ISerializable Members

			public virtual void	Save( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( m_Keys.Count );
				foreach ( Key K in m_Keys )
					K.Save( _Writer );
			}

			public virtual void	Load( System.IO.BinaryReader _Reader )
			{
				int	KeysCount = _Reader.ReadInt32();
				for ( int KeyIndex=0; KeyIndex < KeysCount; KeyIndex++ )
				{
					Key	NewKey = CreateKey();
					m_Keys.Add( NewKey );
					NewKey.Load( _Reader );
				}

				SortKeys();
			}

			#endregion

			#endregion
		}

		/// <summary>
		/// Animation track for boolean values
		/// </summary>
		public class	AnimationTrackBool : AnimationTrack
		{
			#region NESTED TYPES

			public class KeyBool : Key
			{
				#region FIELDS

				protected bool		m_Value = false;

				#endregion

				#region PROPERTIES

				public bool			Value		{ get { return m_Value; } set { m_Value = value; m_Owner.NotifyKeyValueChanged( this ); } }

				#endregion

				#region METHODS

				public KeyBool( AnimationTrack _Owner ) : base( _Owner )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " Value=" + m_Value;
				}

				#region ISerializable Members

				public override void Save( System.IO.BinaryWriter _Writer )
				{
					base.Save( _Writer );
					_Writer.Write( m_Value );
				}

				public override void Load( System.IO.BinaryReader _Reader )
				{
					base.Load( _Reader );
					m_Value = _Reader.ReadBoolean();
				}

				#endregion

				#endregion
			}

			#endregion

			#region FIELDS

			protected bool		m_Value = false;		// The last evaluated value

			#endregion

			#region PROPERTIES

			public bool		Value	{ get { return m_Value; } }

			#endregion

			#region METHODS

			public AnimationTrackBool( ParameterTrack.Interval _Owner, string _Title ) : base( _Owner, _Title )
			{
			}

			/// <summary>
			/// Adds a new key
			/// </summary>
			/// <param name="t">The key in SEQUENCE time</param>
			/// <param name="value"></param>
			/// <returns></returns>
			public KeyBool	AddKey( float t, bool value )
			{
				KeyBool	Result = CreateKey() as KeyBool;
				m_Keys.Add( Result );
				Result.Value = value;
				Result.TrackTime = t;		// Should reorder keys

				NotifyKeysChanged();

				return Result;
			}

			protected override Key CreateKey()
			{
				return new KeyBool( this );
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceForward( float _OldTime, float _NewTime )
			{
				if ( !base.AnimateSequenceForward( _OldTime, _NewTime ) )
					return false;

				// Evaluate value
				bool	NewValue = (m_CurrentKey as KeyBool).Value;
				if ( NewValue == m_Value )
					return	true;	// No change...

				m_Value = NewValue;
				m_Owner.NotifyValueChanged( this );

				return true;
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceBackward( float _OldTime, float _NewTime )
			{
				if ( !base.AnimateSequenceBackward( _OldTime, _NewTime ) )
					return false;

				// Evaluate value
				bool	NewValue = (m_CurrentKey as KeyBool).Value;
				if ( NewValue == m_Value )
					return true;	// No change...

				m_Value = NewValue;
				m_Owner.NotifyValueChanged( this );

				return true;
			}

			internal override void SetTime( float _NewTime )
			{
				base.SetTime( _NewTime );
				if ( m_CurrentKey == null )
					return;

				// Evaluate value
				bool	NewValue = (m_CurrentKey as KeyBool).Value;
				if ( NewValue == m_Value )
					return;	// No change...

				m_Value = NewValue;
				m_Owner.NotifyValueChanged( this );
			}

			#endregion
		}

		/// <summary>
		/// Animation track for events
		/// </summary>
		public class	AnimationTrackEvent : AnimationTrack
		{
			#region NESTED TYPES

			public delegate void	EventTrackEventHandler( AnimationTrackEvent _Sender, int _EventGUID );

			public class KeyEvent : Key
			{
				#region FIELDS

				protected int	m_EventGUID = 0;

				#endregion

				#region PROPERTIES

				public int		EventGUID		{ get { return m_EventGUID; } set { m_EventGUID = value; m_Owner.NotifyKeyValueChanged( this ); } }

				#endregion

				#region METHODS

				public KeyEvent( AnimationTrack _Owner ) : base( _Owner )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " GUID=" + m_EventGUID;
				}

				#region ISerializable Members

				public override void Save( System.IO.BinaryWriter _Writer )
				{
					base.Save( _Writer );
					_Writer.Write( m_EventGUID );
				}

				public override void Load( System.IO.BinaryReader _Reader )
				{
					base.Load( _Reader );
					m_EventGUID = _Reader.ReadInt32();
				}

				#endregion

				#endregion
			}

			#endregion

			#region PROPERTIES

			public event EventTrackEventHandler		Event;

			#endregion

			#region METHODS

			public AnimationTrackEvent( ParameterTrack.Interval _Owner, string _Title ) : base( _Owner, _Title )
			{
			}

			/// <summary>
			/// Adds a new key
			/// </summary>
			/// <param name="t">Key time in SEQUENCE space</param>
			/// <param name="_EventGUID"></param>
			/// <returns></returns>
			public KeyEvent	AddKey( float t, int _EventGUID )
			{
				KeyEvent	Result = CreateKey() as KeyEvent;
				m_Keys.Add( Result );
				Result.EventGUID = _EventGUID;
				Result.TrackTime = t;		// Should reorder keys

				NotifyKeysChanged();

				return Result;
			}

			protected override Key CreateKey()
			{
				return new KeyEvent( this );
			}

			/// <summary>
			/// Fires the event stored in the specified key
			/// </summary>
			/// <param name="_Key"></param>
			protected override void	FireEvent( Key _Key )
			{
				if ( Event != null )
					Event( this, (_Key as KeyEvent).EventGUID );
			}

			#endregion
		}

		/// <summary>
		/// Animation track for integer value
		/// </summary>
		public class	AnimationTrackInt : AnimationTrack
		{
			#region NESTED TYPES

			public class KeyInt : Key
			{
				#region FIELDS

				protected int		m_Value = 0;

				#endregion

				#region PROPERTIES

				public int		Value		{ get { return m_Value; } set { m_Value = value; m_Owner.NotifyKeyValueChanged( this ); } }

				#endregion

				#region METHODS

				public KeyInt( AnimationTrack _Owner ) : base( _Owner )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " Value=" + m_Value;
				}

				#region ISerializable Members

				public override void Save( System.IO.BinaryWriter _Writer )
				{
					base.Save( _Writer );
					_Writer.Write( m_Value );
				}

				public override void Load( System.IO.BinaryReader _Reader )
				{
					base.Load( _Reader );
					m_Value = _Reader.ReadInt32();
				}

				#endregion

				#endregion
			}

			#endregion

			#region FIELDS

			protected int		m_Value = 0;			// The last evaluated value

			// Interval helpers
			protected float		m_IntervalNormalizer = 0.0f;
			protected Vector2	m_LinearFactors = Vector2.Zero;

			#endregion

			#region PROPERTIES

			public int	Value	{ get { return m_Value; } }

			#endregion

			#region METHODS

			public AnimationTrackInt( ParameterTrack.Interval _Owner, string _Title ) : base( _Owner, _Title )
			{
			}

			/// <summary>
			/// Adds a new key
			/// </summary>
			/// <param name="t">Key time in SEQUENCE space</param>
			/// <param name="value"></param>
			/// <returns></returns>
			public KeyInt	AddKey( float t, int value )
			{
				KeyInt	Result = CreateKey() as KeyInt;
				m_Keys.Add( Result );
				Result.Value = value;
				Result.TrackTime = t;		// Should reorder keys

				NotifyKeysChanged();

				return Result;
			}

			protected override Key CreateKey()
			{
				return new KeyInt( this );
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceForward( float _OldTime, float _NewTime )
			{
				if ( base.AnimateSequenceForward( _OldTime, _NewTime ) )
					UpdateIntervalHelpers();	// Compute interval helpers

				Evaluate( _NewTime );

				return true;
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceBackward( float _OldTime, float _NewTime )
			{
				if ( base.AnimateSequenceBackward( _OldTime, _NewTime ) )
					UpdateIntervalHelpers();	// Compute interval helpers

				Evaluate( _NewTime );

				return true;
			}

			internal override void SetTime( float _NewTime )
			{
				base.SetTime( _NewTime );
				if ( m_CurrentKey == null )
					return;

				// Compute interval helpers
				UpdateIntervalHelpers();

				// Evaluate value
				Evaluate( _NewTime );
			}

			protected void	Evaluate( float _Time )
			{
				if ( m_CurrentKey == null )
					return;

				// Evaluate
				float	t = Math.Max( 0.0f, Math.Min( 1.0f, (_Time - m_CurrentKey.NormalizedTime) * m_IntervalNormalizer ) );
				m_Value = (int) (m_LinearFactors.X + m_LinearFactors.Y * t);

				m_Owner.NotifyValueChanged( this );
			}

			/// <summary>
			/// Recomputes the time normalizer factors
			/// </summary>
			protected void	UpdateIntervalHelpers()
			{
				float	Dt = m_NextKey.NormalizedTime - m_CurrentKey.NormalizedTime;
				if ( Dt < 0.0f )
					throw new Exception( "Found an inverted keys interval !" );
				m_IntervalNormalizer = Dt > 1e-6f ? 1.0f / Dt : 0.0f;

				// Compute linear factors
				m_LinearFactors.X = (m_CurrentKey as KeyInt).Value;
				m_LinearFactors.Y = (m_NextKey as KeyInt).Value - m_LinearFactors.X;
			}

			/// <summary>
			/// Slow immediate evaluation at given SEQUENCE time (used for UI, not for realtime eval as it's slow)
			/// </summary>
			/// <param name="t">SEQUENCE time</param>
			/// <returns></returns>
			/// <remarks>Don't use that method at runtime as it seeks for key intervals every time and is quite slow</remarks>
			public int	ImmediateEval( float _t )
			{
				if ( m_Keys.Count == 0 )
					return 0;

				float	t = (_t - m_Owner.TimeStart) / (m_Owner.TimeEnd - m_Owner.TimeStart);
				t = Math.Max( 0.0f, Math.Min( 1.0f, t ) );

				// Retrieve key interval
				float	fResult = 0.0f;
				if ( t <= m_Keys[0].NormalizedTime )
					fResult = (m_Keys[0] as KeyInt).Value;
				else if ( t >= m_Keys[m_Keys.Count-1].NormalizedTime )
					fResult = (m_Keys[m_Keys.Count-1] as KeyInt).Value;
				else
				{
					for ( int KeyIndex=0; KeyIndex < m_Keys.Count-1; KeyIndex++ )
					{
						KeyInt	K0 = m_Keys[KeyIndex] as KeyInt;
						KeyInt	K1 = m_Keys[KeyIndex+1] as KeyInt;
						if ( t > K1.NormalizedTime )
							continue;	// Wrong interval

						t = (t-K0.NormalizedTime) / (K1.NormalizedTime - K0.NormalizedTime);	// t in interval time

// 						if ( m_Owner.ParentTrack.CubicInterpolation )
// 						{	// Cubic interpolation
// 							float	a = K0.Value;
// 							float	b = K0.TangentOutValue;
// 							float	c = -3.0f * K0.Value - 2.0f * K0.TangentOutValue + 3.0f * K1.Value - K1.TangentInValue;
// 							float	d = 2.0f * K0.Value + K0.TangentOutValue - 2.0f * K1.Value + K1.TangentInValue;
// 
// 							fResult = a + t * (b + t * (c + t * d));
// 						}
// 						else
						{	// Linear interpolation
							fResult = K0.Value + (K1.Value - K0.Value) * t;
						}

						break;
					}
				}

				// Clip
				fResult = Math.Max( m_Owner.ParentTrack.ClipMin, Math.Min( m_Owner.ParentTrack.ClipMax, fResult ) );

				return (int) fResult;
			}

			#endregion
		}

		/// <summary>
		/// Animation track for float value
		/// </summary>
		public class	AnimationTrackFloat : AnimationTrack
		{
			#region NESTED TYPES

			public class KeyFloat : Key
			{
				#region FIELDS

				protected float		m_Value = 0.0f;
				protected Vector2	m_TangentIn = new Vector2( DEFAULT_TANGENT_X, DEFAULT_TANGENT_Y );
				protected Vector2	m_TangentOut = new Vector2( DEFAULT_TANGENT_X, DEFAULT_TANGENT_Y );

				protected float		m_TangentInValue = 0.0f;
				protected float		m_TangentOutValue = 0.0f;

				#endregion

				#region PROPERTIES

				public float	Value		{ get { return m_Value; } set { m_Value = value; m_Owner.NotifyKeyValueChanged( this ); } }
				public Vector2	TangentIn
				{
					get { return m_TangentIn; }
					set
					{
						m_TangentIn = value; m_Owner.NotifyKeyValueChanged( this );
						m_TangentInValue = TANGENT_LENGTH_FACTOR * m_TangentIn.Length() * m_TangentIn.Y / Math.Max( 1e-6f, m_TangentIn.X );
//						m_TangentInValue = m_TangentIn.Y / Math.Max( 1e-6f, m_TangentIn.X );
					}
				}
				public Vector2	TangentOut
				{
					get { return m_TangentOut; }
					set
					{
						m_TangentOut = value; m_Owner.NotifyKeyValueChanged( this );
						m_TangentOutValue = TANGENT_LENGTH_FACTOR * m_TangentOut.Length() * m_TangentOut.Y / Math.Max( 1e-6f, m_TangentOut.X );
//						m_TangentOutValue = m_TangentOut.Y / Math.Max( 1e-6f, m_TangentOut.X );
					}
				}

				public float	TangentInValue	{ get { return m_TangentInValue; } }
				public float	TangentOutValue	{ get { return m_TangentOutValue; } }

				#endregion

				#region METHODS

				public KeyFloat( AnimationTrack _Owner ) : base( _Owner )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " Value=" + m_Value.ToString( "G4" );
				}

				#region ISerializable Members

				public override void Save( System.IO.BinaryWriter _Writer )
				{
					base.Save( _Writer );
					_Writer.Write( m_Value );
					_Writer.Write( m_TangentIn.X );
					_Writer.Write( m_TangentIn.Y );
					_Writer.Write( m_TangentOut.X );
					_Writer.Write( m_TangentOut.Y );
				}

				public override void Load( System.IO.BinaryReader _Reader )
				{
					base.Load( _Reader );
					m_Value = _Reader.ReadSingle();

					Vector2	T;
					T.X = _Reader.ReadSingle();
					T.Y = _Reader.ReadSingle();
					TangentIn = T;

					T.X = _Reader.ReadSingle();
					T.Y = _Reader.ReadSingle();
					TangentOut = T;
				}

				#endregion

				#endregion
			}

			#endregion

			#region FIELDS

			protected float		m_Value = 0.0f;			// The last evaluated value
			protected float		m_dValue = 0.0f;		// And its derivative

			// Interval helpers
			protected float		m_IntervalNormalizer = 0.0f;
			protected Vector2	m_LinearFactors = Vector2.Zero;
			protected Vector4	m_HermiteFactors = Vector4.Zero;

			#endregion

			#region PROPERTIES

			public float	Value	{ get { return m_Value; } }
			public float	dValue	{ get { return m_dValue; } }

			#endregion

			#region METHODS

			public AnimationTrackFloat( ParameterTrack.Interval _Owner, string _Title ) : base( _Owner, _Title )
			{
			}

			/// <summary>
			/// Adds a new key
			/// </summary>
			/// <param name="t">Key time in SEQUENCE space</param>
			/// <param name="value"></param>
			/// <returns></returns>
			public KeyFloat	AddKey( float t, float value )
			{
				KeyFloat	Result = CreateKey() as KeyFloat;
				m_Keys.Add( Result );
				Result.Value = value;
				Result.TrackTime = t;		// Should reorder keys

				NotifyKeysChanged();

				return Result;
			}

			protected override Key CreateKey()
			{
				return new KeyFloat( this );
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceForward( float _OldTime, float _NewTime )
			{
				if ( base.AnimateSequenceForward( _OldTime, _NewTime ) )
					UpdateIntervalHelpers();	// Compute interval helpers

				Evaluate( _NewTime );

				return true;
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceBackward( float _OldTime, float _NewTime )
			{
				if ( base.AnimateSequenceBackward( _OldTime, _NewTime ) )
					UpdateIntervalHelpers();	// Compute interval helpers

				Evaluate( _NewTime );

				return true;
			}

			internal override void SetTime( float _NewTime )
			{
				base.SetTime( _NewTime );
				if ( m_CurrentKey == null )
					return;

				// Compute interval helpers
				UpdateIntervalHelpers();

				// Evaluate value
				Evaluate( _NewTime );
			}

			protected void	Evaluate( float _Time )
			{
				if ( m_CurrentKey == null )
					return;

				// Evaluate
				float	t = Math.Max( 0.0f, Math.Min( 1.0f, (_Time - m_CurrentKey.NormalizedTime) * m_IntervalNormalizer ) );
				if ( !m_Owner.CubicInterpolation )
				{
					m_Value = m_LinearFactors.X + m_LinearFactors.Y * t; 
					m_dValue = m_LinearFactors.Y;
				}
				else
				{
					m_Value = m_HermiteFactors.X + t * (m_HermiteFactors.Y + t * (m_HermiteFactors.Z + t * m_HermiteFactors.W));
					m_dValue = m_HermiteFactors.Y + t * (2.0f * m_HermiteFactors.Z + t * 3.0f * m_HermiteFactors.W);
				}

				// Apply value clipping
				m_Value = Math.Max( m_Owner.ParentTrack.ClipMin, Math.Min( m_Owner.ParentTrack.ClipMax, m_Value ) );

				m_Owner.NotifyValueChanged( this );
			}

			/// <summary>
			/// Recomputes the time normalizer and Hermite factors
			/// </summary>
			protected void	UpdateIntervalHelpers()
			{
				float	Dt = m_NextKey.NormalizedTime - m_CurrentKey.NormalizedTime;
				if ( Dt < 0.0f )
					throw new Exception( "Found an inverted keys interval !" );
				m_IntervalNormalizer = Dt > 1e-6f ? 1.0f / Dt : 0.0f;

				// Compute linear factors
				m_LinearFactors.X = (m_CurrentKey as KeyFloat).Value;
				m_LinearFactors.Y = (m_NextKey as KeyFloat).Value - m_LinearFactors.X;

				// Compute Hermite cubic spline factors
				m_HermiteFactors.X = (m_CurrentKey as KeyFloat).Value;
				m_HermiteFactors.Y = (m_CurrentKey as KeyFloat).TangentOutValue;
				m_HermiteFactors.Z = -3.0f * (m_CurrentKey as KeyFloat).Value - 2.0f * (m_CurrentKey as KeyFloat).TangentOutValue + 3.0f * (m_NextKey as KeyFloat).Value - (m_NextKey as KeyFloat).TangentInValue;
				m_HermiteFactors.W = 2.0f * (m_CurrentKey as KeyFloat).Value + (m_CurrentKey as KeyFloat).TangentOutValue - 2.0f * (m_NextKey as KeyFloat).Value + (m_NextKey as KeyFloat).TangentInValue;
			}

			/// <summary>
			/// Slow immediate evaluation at given SEQUENCE time (used for UI, not for realtime eval as it's slow)
			/// </summary>
			/// <param name="t">SEQUENCE time</param>
			/// <returns></returns>
			/// <remarks>Don't use that method at runtime as it seeks for key intervals every time and is quite slow</remarks>
			public float	ImmediateEval( float _t )
			{
				if ( m_Keys.Count == 0 )
					return 0.0f;

				float	t = (_t - m_Owner.TimeStart) / (m_Owner.TimeEnd - m_Owner.TimeStart);
				t = Math.Max( 0.0f, Math.Min( 1.0f, t ) );

				// Retrieve key interval
				float	fResult = 0.0f;
				if ( t <= m_Keys[0].NormalizedTime )
					fResult = (m_Keys[0] as KeyFloat).Value;
				else if ( t >= m_Keys[m_Keys.Count-1].NormalizedTime )
					fResult = (m_Keys[m_Keys.Count-1] as KeyFloat).Value;
				else
				{
					for ( int KeyIndex=0; KeyIndex < m_Keys.Count-1; KeyIndex++ )
					{
						KeyFloat	K0 = m_Keys[KeyIndex] as KeyFloat;
						KeyFloat	K1 = m_Keys[KeyIndex+1] as KeyFloat;
						if ( t > K1.NormalizedTime )
							continue;	// Wrong interval

						t = (t-K0.NormalizedTime) / (K1.NormalizedTime - K0.NormalizedTime);	// t in interval time

						if ( m_Owner.ParentTrack.CubicInterpolation )
						{	// Cubic interpolation
							float	a = K0.Value;
							float	b = K0.TangentOutValue;
							float	c = -3.0f * K0.Value - 2.0f * K0.TangentOutValue + 3.0f * K1.Value - K1.TangentInValue;
							float	d = 2.0f * K0.Value + K0.TangentOutValue - 2.0f * K1.Value + K1.TangentInValue;

							fResult = a + t * (b + t * (c + t * d));
						}
						else
						{	// Linear interpolation
							fResult = K0.Value + (K1.Value - K0.Value) * t;
						}

						break;
					}
				}

				// Clip
				fResult = Math.Max( m_Owner.ParentTrack.ClipMin, Math.Min( m_Owner.ParentTrack.ClipMax, fResult ) );

				return fResult;
			}

			#endregion
		}

		/// <summary>
		/// Animation track for quaternion value
		/// Quaternions are stored as independent Axis + Angle values so angles can take any value
		/// _ The Axis part is converted as a quaternion and interpolated using a SQUAD (or a SLERP if interpolating in linear mode)
		/// _ The Angle part is interpolated using a standard Hermit cubic spline (or a lerp if interpolating in linear mode)
		/// Finally, at each evaluation, the resulting quaternion is rebuilt from the interpolated Axis + Angle
		/// </summary>
		public class	AnimationTrackQuat : AnimationTrack
		{
			#region NESTED TYPES

			public class KeyQuat : Key
			{
				#region FIELDS

				protected Vector3	m_Axis = Vector3.UnitX;
				protected float		m_Angle = 0.0f;

				#endregion

				#region PROPERTIES

//				public Quaternion	Value		{ get { return m_Value; } set { m_Value = value; m_Owner.NotifyKeyValueChanged( this ); } }
				public Vector3		Axis		{ get { return m_Axis; } set { m_Axis = value; m_Owner.NotifyKeyValueChanged( this ); } }
				public float		Angle		{ get { return m_Angle; } set { m_Angle = value; m_Owner.NotifyKeyValueChanged( this ); } }

				#endregion

				#region METHODS

				public KeyQuat( AnimationTrack _Owner ) : base( _Owner )
				{
				}

				/// <summary>
				/// Sets the angle axis in a single shot
				/// </summary>
				/// <param name="_Angle"></param>
				/// <param name="_Axis"></param>
				public void		SetAngleAxis( float _Angle, Vector3 _Axis )
				{
					m_Angle = _Angle;
					m_Axis = _Axis;
					m_Owner.NotifyKeyValueChanged( this );
				}

				#region ISerializable Members

				public override void Save( System.IO.BinaryWriter _Writer )
				{
					base.Save( _Writer );
					_Writer.Write( m_Axis.X );
					_Writer.Write( m_Axis.Y );
					_Writer.Write( m_Axis.Z );
					_Writer.Write( m_Angle );
				}

				public override void Load( System.IO.BinaryReader _Reader )
				{
					base.Load( _Reader );
					m_Axis.X = _Reader.ReadSingle();
					m_Axis.Y = _Reader.ReadSingle();
					m_Axis.Z = _Reader.ReadSingle();
					m_Angle = _Reader.ReadSingle();
				}

				#endregion

				#endregion
			}

			#endregion

			#region FIELDS

			protected Quaternion	m_Value = Quaternion.Identity;		// The last evaluated value

			// Interval helpers
			protected float			m_IntervalNormalizer = 0.0f;
			protected Vector4		m_AngleHermiteFactors = Vector4.Zero;
			protected Vector2		m_AngleLinearFactors = Vector2.Zero;
			protected Quaternion	m_Q1 = Quaternion.Zero;
			protected Quaternion[]	m_SQUADSetup = null;

			#endregion

			#region PROPERTIES

			public Quaternion		Value	{ get { return m_Value; } }

			#endregion

			#region METHODS

			public AnimationTrackQuat( ParameterTrack.Interval _Owner, string _Title ) : base( _Owner, _Title )
			{
// QUATERNION TESTS WITH LOG/EXP
// 			SharpDX.Quaternion[]	Tests = new SharpDX.Quaternion[20];
// 			for ( int i=0; i < 20; i++ )
// //				Tests[i] = SharpDX.Quaternion.RotationAxis( SharpDX.Vector3.UnitX, (float) (i * 0.5f * Math.PI) );
// 				Tests[i] = SharpDX.Quaternion.RotationAxis( 4.0f * SharpDX.Vector3.UnitX, (float) (i * 0.5f * Math.PI) );
//
// 	Quat Log() const
// 	{
// 		const float a = acosf( (w > 1.0f) ? 1.0f : ((w < -1.0f) ? -1.0f : w) );
// 		const float n = sqrtf(x*x+y*y+z*z);
// 		const Quat q(x,y,z,0.0f);
// 		const Quat r = (n > 0.0f) ? (q*(a/n)) : q;
// 		return r;
// 	}
// 	Quat Exp() const
// 	{
// 		const float a = sqrtf(x*x+y*y+z*z);
// 		const float n = (a != 0) ? sinf(a)  /a : 0;
// 		const Quat q(n*x,n*y,n*z,cosf(a) );
// 		return q;
// 	}
// 
// 	// Pack
// 	Q = Quat.FromAxisAngle( XYZ, 0 ).Log() * 37 * PI   ??
// 
// 	// Unpack
// 	Angle = Q_log.Length();
// 	Q = (Q_log / Angle).Exp() ?
// 
// 	q0_log = Axis0*Angle0
			}

			/// <summary>
			/// Adds a new key
			/// </summary>
			/// <param name="t">Key time in SEQUENCE space</param>
			/// <param name="value"></param>
			/// <returns></returns>
			public KeyQuat	AddKey( float t, float angle, Vector3 axis )
			{
				KeyQuat	Result = CreateKey() as KeyQuat;
				m_Keys.Add( Result );
				Result.SetAngleAxis( angle, axis );
				Result.TrackTime = t;		// Should reorder keys

				NotifyKeysChanged();

				return Result;
			}

			protected override Key CreateKey()
			{
				return new KeyQuat( this );
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceForward( float _OldTime, float _NewTime )
			{
				if ( base.AnimateSequenceForward( _OldTime, _NewTime ) )
					UpdateIntervalHelpers();	// Compute interval helpers

				Evaluate( _NewTime );

				return true;
			}

			// Remarks : This method assumes an initial SetTime() has been performed
			internal override bool AnimateSequenceBackward( float _OldTime, float _NewTime )
			{
				if ( base.AnimateSequenceBackward( _OldTime, _NewTime ) )
					UpdateIntervalHelpers();	// Compute interval helpers

				Evaluate( _NewTime );

				return true;
			}

			internal override void SetTime( float _NewTime )
			{
				base.SetTime( _NewTime );
				if ( m_CurrentKey == null )
					return;

				// Compute interval helpers
				UpdateIntervalHelpers();

				// Evaluate value
				Evaluate( _NewTime );
			}

			protected void	Evaluate( float _Time )
			{
				if ( m_CurrentKey == null )
					return;

				float	t = Math.Max( 0.0f, Math.Min( 1.0f, (_Time - m_CurrentKey.NormalizedTime) * m_IntervalNormalizer ) );

				// Interpolate axis using quaternions
				Quaternion	QInterpolatedAxis;
				if ( !m_Owner.CubicInterpolation )
					Quaternion.Slerp( ref m_Q1, ref m_SQUADSetup[2], t, out QInterpolatedAxis );
				else
					Quaternion.Squad( ref m_Q1, ref m_SQUADSetup[0], ref m_SQUADSetup[1], ref m_SQUADSetup[2], t, out QInterpolatedAxis );
				Vector3	InterpolatedAxis = QInterpolatedAxis.Axis;
				InterpolatedAxis.Normalize();

				// Interpolate angle using Hermite cubic spline
				float	InterpolatedAngle = 0.0f;
				if ( m_Owner.CubicInterpolation )
					InterpolatedAngle = m_AngleHermiteFactors.X + t * (m_AngleHermiteFactors.Y + t * (m_AngleHermiteFactors.Z + t * m_AngleHermiteFactors.W));
				else
					InterpolatedAngle = m_AngleLinearFactors.X + m_AngleLinearFactors.Y * t;

				// Recompose final quaternion
				m_Value = Quaternion.RotationAxis( InterpolatedAxis, InterpolatedAngle );

				m_Owner.NotifyValueChanged( this );
			}

			/// <summary>
			/// Recomputes the time normalizer and Hermite factors
			/// </summary>
			protected void	UpdateIntervalHelpers()
			{
				float	Dt = m_NextKey.NormalizedTime - m_CurrentKey.NormalizedTime;
				if ( Dt < 0.0f )
					throw new Exception( "Found an inverted keys interval !" );
				m_IntervalNormalizer = Dt > 1e-6f ? 1.0f / Dt : 0.0f;

				// Retrieve 4 sets of keys (we're interpolating within k1-k2 here)
				KeyQuat	k0 = (m_CurrentKeyIndex > 0 ? m_Keys[m_CurrentKeyIndex-1] : m_CurrentKey) as KeyQuat;
				KeyQuat	k1 = m_CurrentKey as KeyQuat;
				KeyQuat	k2 = m_NextKey as KeyQuat;
				KeyQuat	k3 = (m_CurrentKeyIndex < m_Keys.Count-2 ? m_Keys[m_CurrentKeyIndex+2] : m_NextKey) as KeyQuat;

				// Auto evaluate Hermite spline tangents for angle
				// (using the three-point difference fromhttp://en.wikipedia.org/wiki/Cubic_Hermite_spline )
				float	t1 = 0.0f;
				if ( k0 != k1 )
					t1 = 0.5f * ((k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime ) + (k1.Angle - k0.Angle) / Math.Max( 1e-3f, k1.NormalizedTime - k0.NormalizedTime ));
				else
					t1 = 0.5f * (k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime );

				float	t2 = 0.0f;
				if ( k3 != k2 )
					t2 = 0.5f * ((k3.Angle - k2.Angle) / Math.Max( 1e-3f, k3.NormalizedTime - k2.NormalizedTime ) + (k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime ));
				else
					t2 = 0.5f * (k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime );

				m_AngleHermiteFactors.X = k1.Angle;
				m_AngleHermiteFactors.Y = t1;
				m_AngleHermiteFactors.Z = -3.0f * k1.Angle - 2.0f * t1 + 3.0f * k2.Angle - t2;
				m_AngleHermiteFactors.W = 2.0f * k1.Angle + t1 - 2.0f * k2.Angle + t2;

				// Compute linear factors
				m_AngleLinearFactors.X = k1.Angle;
				m_AngleLinearFactors.Y = k2.Angle - k1.Angle;

				// Setup SQUAD interpolation
				// (cf. windows_graphics.chm/direct3d10/d3d10_d3dxquaternionsquadsetup.htm)
				Quaternion	q0 = Quaternion.RotationAxis( k0.Axis, 0.5f * (float) Math.PI );
				m_Q1 = Quaternion.RotationAxis( k1.Axis, 0.5f * (float) Math.PI );
				Quaternion	q2 = Quaternion.RotationAxis( k2.Axis, 0.5f * (float) Math.PI );
				Quaternion	q3 = Quaternion.RotationAxis( k3.Axis, 0.5f * (float) Math.PI );

				m_SQUADSetup = Quaternion.SquadSetup( q0, m_Q1, q2, q3 );
			}

			/// <summary>
			/// Slow immediate evaluation at given SEQUENCE time (used for UI, not for realtime eval as it's slow)
			/// </summary>
			/// <param name="_t">SEQUENCE time</param>
			/// <param name="_Angle">The resulting interpolated angle</param>
			/// <param name="_Axis">The resulting interpolated axis</param>
			/// <remarks>Don't use that method at runtime as it seeks for key intervals every time and is quite slow</remarks>
			public void		ImmediateEval( float _t, out float _Angle, out Vector3 _Axis )
			{
				_Angle = 0.0f;
				_Axis = Vector3.UnitX;
				if ( m_Keys.Count == 0 )
					return;

				float	t = (_t - m_Owner.TimeStart) / (m_Owner.TimeEnd - m_Owner.TimeStart);
				t = Math.Max( 0.0f, Math.Min( 1.0f, t ) );

				// Retrieve key interval
				if ( t <= m_Keys[0].NormalizedTime )
				{
					_Angle = (m_Keys[0] as KeyQuat).Angle;
					_Axis = (m_Keys[0] as KeyQuat).Axis;
				}
				else if ( t >= m_Keys[m_Keys.Count-1].NormalizedTime )
				{
					_Angle = (m_Keys[m_Keys.Count-1] as KeyQuat).Angle;
					_Axis = (m_Keys[m_Keys.Count-1] as KeyQuat).Axis;
				}
				else
				{
					for ( int KeyIndex=0; KeyIndex < m_Keys.Count-1; KeyIndex++ )
					{
						KeyQuat	k1 = m_Keys[KeyIndex] as KeyQuat;
						KeyQuat	k2 = m_Keys[KeyIndex+1] as KeyQuat;
						if ( t > k2.NormalizedTime )
							continue;	// Wrong interval

						t = (t-k1.NormalizedTime) / (k2.NormalizedTime - k1.NormalizedTime);	// t in interval time

						Quaternion	AxisQuat;
						if ( m_Owner.CubicInterpolation )
						{
							// Retrieve 4 sets of keys (we're interpolating within k1-k2 here)
							KeyQuat	k0 = KeyIndex > 0 ? (m_Keys[KeyIndex-1]) as KeyQuat : k1;
							KeyQuat	k3 = KeyIndex < m_Keys.Count-2 ? (m_Keys[m_CurrentKeyIndex+2] as KeyQuat) : k2;

							// Auto evaluate Hermite spline tangents for angle
							// (using the three-point difference fromhttp://en.wikipedia.org/wiki/Cubic_Hermite_spline )
							float	t1 = 0.0f;
							if ( k0 != k1 )
								t1 = 0.5f * ((k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime ) + (k1.Angle - k0.Angle) / Math.Max( 1e-3f, k1.NormalizedTime - k0.NormalizedTime ));
							else
								t1 = 0.5f * (k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime );

							float	t2 = 0.0f;
							if ( k3 != k2 )
								t2 = 0.5f * ((k3.Angle - k2.Angle) / Math.Max( 1e-3f, k3.NormalizedTime - k2.NormalizedTime ) + (k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime ));
							else
								t2 = 0.5f * (k2.Angle - k1.Angle) / Math.Max( 1e-3f, k2.NormalizedTime - k1.NormalizedTime );

							float	a = k1.Angle;
							float	b = t1;
							float	c = -3.0f * k1.Angle - 2.0f * t1 + 3.0f * k2.Angle - t2;
							float	d = 2.0f * k1.Angle + t1 - 2.0f * k2.Angle + t2;

							_Angle = a + t * (b + t * (c + t * d));

							// Setup SQUAD interpolation
							// (cf. windows_graphics.chm/direct3d10/d3d10_d3dxquaternionsquadsetup.htm)
							Quaternion	q0 = Quaternion.RotationAxis( k0.Axis, 0.5f * (float) Math.PI );
							Quaternion	q1 = Quaternion.RotationAxis( k1.Axis, 0.5f * (float) Math.PI );
							Quaternion	q2 = Quaternion.RotationAxis( k2.Axis, 0.5f * (float) Math.PI );
							Quaternion	q3 = Quaternion.RotationAxis( k3.Axis, 0.5f * (float) Math.PI );

							Quaternion[]	SQUADSetup = Quaternion.SquadSetup( q0, q1, q2, q3 );
							AxisQuat = Quaternion.Squad( q1, SQUADSetup[0], SQUADSetup[1], SQUADSetup[2], t );
						}
						else
						{
							_Angle = k1.Angle * (1.0f - t) + k2.Angle * t;

							Quaternion	q1 = Quaternion.RotationAxis( k1.Axis, 0.5f * (float) Math.PI );
							Quaternion	q2 = Quaternion.RotationAxis( k2.Axis, 0.5f * (float) Math.PI );
							AxisQuat = Quaternion.Slerp( q1, q2, t );
						}
				
						_Axis = AxisQuat.Axis;
						_Axis.Normalize();

						return;
					}
				}
			}

			#endregion
		}

		#endregion

		/// <summary>
		/// The ParameterTrack class holds informations concerning a parameter animationand also is a container of sorted intervals
		/// All intervals are sorted in ascending start time order and don't overlap.
		/// Intervals are guaranteed that at any time their start time is >= to their predecessor's end time and their end time is <= to their successor's start time
		/// </summary>
		[System.Diagnostics.DebuggerDisplay( "Name={Name} Type={Type} GUID={GUID}" )]
		public class	ParameterTrack : ISerializable
		{
			#region NESTED TYPES

			public enum PARAMETER_TYPE
			{
				UNKNOWN,
				BOOL,
				EVENT,
				INT,
				FLOAT,
				FLOAT2,
				FLOAT3,
				FLOAT4,
				PRS,
			}

			/// <summary>
			/// The interval class hosts animation tracks
			/// Animation tracks use the normalized interval time for their animation t = (T - m_TimeStart) / (m_TimeEnd - m_TimeStart)
			/// </summary>
			[System.Diagnostics.DebuggerDisplay( "Start={TimeStart} End={TimeEnd}" )]
			public class	Interval : IComparable<Interval>, ISerializable
			{
				#region NESTED TYPES
				
				public delegate void	KeyValueChangedEventHandler( Interval _Sender, AnimationTrack.Key _Key );

				#endregion

				#region FIELDS

				protected ParameterTrack		m_Owner = null;
				protected bool					m_bCubicInterpolation = false;

				// The "actual" interval time
				// This is the TRUE interval that doesn't account for neighbouring intervals overlaps
				protected float					m_ActualTimeStart = 0.0f;
				protected float					m_ActualTimeEnd = 0.0f;

				// The "visible" interval time
				// This is the interval we use for animation. This interval is packed so it never overlaps any neighbouring interval.
				// It's guaranted that after a sort, m_TimeStart >= Predecessor.m_TimeEnd and m_TimeEnd <= Successor.m_TimeStart
				protected float					m_TimeStart = 0.0f;
				protected float					m_TimeEnd = 0.0f;
				protected float					m_TimeNormalizer = 0.0f;

				// An interval contains several animation tracks, depending on the type of the owner parameter track
				// For example, a float3 type contains 3 float animation tracks
				// and a PRS type contains 7 animation tracks (3 floats for position, 3 floats for scale and 1 quaternion for rotation)
				protected AnimationTrack[]		m_AnimationTracks = null;

				// A flag that is set if an animation track changed its value during an animation step
				protected bool					m_bTrackValuesChanged = false;

				#endregion

				#region PROPERTIES

				public ParameterTrack	ParentTrack
				{
					get { return m_Owner; }
				}

				public AnimationTrack[]	AnimationTracks
				{
					get { return m_AnimationTracks; }
				}

				public int			AnimationTracksCount
				{
					get { return m_AnimationTracks.Length; }
				}

				public bool			CanInterpolateCubic
				{
					get { return m_Owner.CanInterpolateCubic; }
				}

				public bool			CubicInterpolation
				{
					get { return m_bCubicInterpolation; }
					set
					{
						if ( value == m_bCubicInterpolation )
							return;

						m_bCubicInterpolation = value;

						if ( CubicInterpolationChanged != null )
							CubicInterpolationChanged( this, EventArgs.Empty );
					}
				}

				/// <summary>
				/// Gets or sets the actual start time (i.e. the time at which the interval starts without accounting for overlaps with other intervals)
				/// </summary>
				public float		ActualTimeStart
				{
					get { return m_ActualTimeStart; }
					set
					{
						if ( value == m_ActualTimeStart )
							return;

						m_ActualTimeStart = value;
						m_Owner.SortIntervals();

						// Notify
						if ( ActualTimeStartChanged != null )
							ActualTimeStartChanged( this, EventArgs.Empty );
					}
				}

				/// <summary>
				/// Gets or sets the actual end time (i.e. the time at which the interval ends without accounting for overlaps with other intervals)
				/// </summary>
				public float		ActualTimeEnd
				{
					get { return m_ActualTimeEnd; }
					set
					{
						if ( value == m_ActualTimeEnd )
							return;

						m_ActualTimeEnd = value;
						m_Owner.SortIntervals();

						// Notify
						if ( ActualTimeEndChanged != null )
							ActualTimeEndChanged( this, EventArgs.Empty );
					}
				}

				/// <summary>
				/// Gets the visible start time (i.e. the time that is used for animation that accounts for overlaps with other intervals)
				/// </summary>
				public float		TimeStart
				{
					get { return m_TimeStart; }
					internal set { m_TimeStart = value; m_TimeNormalizer = 1.0f / Math.Max( 1e-6f, m_TimeEnd - m_TimeStart ); }
				}

				/// <summary>
				/// Gets the visible end time (i.e. the time that is used for animation that accounts for overlaps with other intervals)
				/// </summary>
				public float		TimeEnd
				{
					get { return m_TimeEnd; }
					internal set { m_TimeEnd = value; m_TimeNormalizer = 1.0f / Math.Max( 1e-6f, m_TimeEnd - m_TimeStart ); }
				}

				/// <summary>
				/// Gets the visible duration (i.e. the time that is used for animation that accounts for overlaps with other intervals)
				/// </summary>
				public float		Duration
				{
					get { return m_TimeEnd - m_TimeStart; }
				}

				/// <summary>
				/// Gets the sequencer time relative to the interval's start time
				/// </summary>
				public float		SequencorTimeRelative
				{
					get { return m_Owner.m_Owner.Time - m_TimeStart; }
				}

				/// <summary>
				/// Gets the sequencer time normalized within the interval
				/// </summary>
				public float		SequencorTimeNormalized
				{
					get { return (m_Owner.m_Owner.Time - m_TimeStart) / Math.Max( 1e-6f, m_TimeEnd - m_TimeStart ); }
				}

				/// <summary>
				/// Gets an animation track at the specific index
				/// </summary>
				/// <param name="_TrackIndex"></param>
				/// <returns></returns>
				public AnimationTrack		this[int _TrackIndex]
				{
					get { return m_AnimationTracks[_TrackIndex]; }
				}

				/// <summary>
				/// Occurs when the start time changed
				/// </summary>
				public event EventHandler	ActualTimeStartChanged;

				/// <summary>
				/// Occurs when the end time changed
				/// </summary>
				public event EventHandler	ActualTimeEndChanged;

				/// <summary>
				/// Occurs when the list of keys of one of our animation tracks changed
				/// </summary>
				public event EventHandler	KeysChanged;

				/// <summary>
				/// Occurs when the time or value of a key in one of our animation tracks changed
				/// </summary>
				public event KeyValueChangedEventHandler	KeyValueChanged;

				/// <summary>
				/// Occurs when the cubic interpolation state changed
				/// </summary>
				public event EventHandler	CubicInterpolationChanged;

				#endregion

				#region METHODS

				public Interval( ParameterTrack _Owner )
				{
					m_Owner = _Owner;

					// Create animation tracks
					switch ( m_Owner.Type )
					{
						case PARAMETER_TYPE.BOOL:
							m_AnimationTracks = new AnimationTrack[1];
							m_AnimationTracks[0] = new AnimationTrackBool( this, "" );
							break;
						case PARAMETER_TYPE.EVENT:
							m_AnimationTracks = new AnimationTrack[1];
							m_AnimationTracks[0] = new AnimationTrackEvent( this, "" );
							m_AnimationTracks[0].AsEvent.Event += new AnimationTrackEvent.EventTrackEventHandler( AnimationTrackEvent_Event );
							break;
						case PARAMETER_TYPE.INT:
							m_AnimationTracks = new AnimationTrack[1];
							m_AnimationTracks[0] = new AnimationTrackInt( this, "" );
							break;
						case PARAMETER_TYPE.FLOAT:
							m_AnimationTracks = new AnimationTrack[1];
							m_AnimationTracks[0] = new AnimationTrackFloat( this, "" );
							break;
						case PARAMETER_TYPE.FLOAT2:
							m_AnimationTracks = new AnimationTrack[2];
							m_AnimationTracks[0] = new AnimationTrackFloat( this, "X " );
							m_AnimationTracks[1] = new AnimationTrackFloat( this, "Y " );
							break;
						case PARAMETER_TYPE.FLOAT3:
							m_AnimationTracks = new AnimationTrack[3];
							m_AnimationTracks[0] = new AnimationTrackFloat( this, "X " );
							m_AnimationTracks[1] = new AnimationTrackFloat( this, "Y " );
							m_AnimationTracks[2] = new AnimationTrackFloat( this, "Z " );
							break;
						case PARAMETER_TYPE.FLOAT4:
							m_AnimationTracks = new AnimationTrack[4];
							m_AnimationTracks[0] = new AnimationTrackFloat( this, "X " );
							m_AnimationTracks[1] = new AnimationTrackFloat( this, "Y " );
							m_AnimationTracks[2] = new AnimationTrackFloat( this, "Z " );
							m_AnimationTracks[3] = new AnimationTrackFloat( this, "W " );
							break;
						case PARAMETER_TYPE.PRS:
							m_AnimationTracks = new AnimationTrack[7];
							// Position
							m_AnimationTracks[0] = new AnimationTrackFloat( this, "P.X " );
							m_AnimationTracks[1] = new AnimationTrackFloat( this, "P.Y " );
							m_AnimationTracks[2] = new AnimationTrackFloat( this, "P.Z " );
							// Rotation
							m_AnimationTracks[3] = new AnimationTrackQuat( this, "R " );
							// Scale
							m_AnimationTracks[4] = new AnimationTrackFloat( this, "S.X " );
							m_AnimationTracks[5] = new AnimationTrackFloat( this, "S.Y " );
							m_AnimationTracks[6] = new AnimationTrackFloat( this, "S.Z " );
							break;
					}

					// Subscribe to track events
					foreach ( AnimationTrack T in m_AnimationTracks )
					{
						T.KeysChanged += new EventHandler( Track_KeysChanged );
						T.KeyValueChanged += new AnimationTrack.KeyValueChangedEventHandler( Track_KeyValueChanged );
					}
				}

				/// <summary>
				/// When we're scrolling an interval, both its start & end time move at the same time
				///  so we need to update both of them in a single blow otherwise we'll get discrepancies
				///  if they're changed one after the other
				/// </summary>
				/// <param name="_ActualTimeStart"></param>
				/// <param name="_ActualTimeEnd"></param>
				public void		UpdateBothTimes( float _ActualTimeStart, float _ActualTimeEnd )
				{
					m_ActualTimeStart = _ActualTimeStart;
					m_ActualTimeEnd = _ActualTimeEnd;
					m_Owner.SortIntervals();

					// Notify
					if ( ActualTimeStartChanged != null )
						ActualTimeStartChanged( this, EventArgs.Empty );
					if ( ActualTimeEndChanged != null )
						ActualTimeEndChanged( this, EventArgs.Empty );
				}

				/// <summary>
				/// Animates the interval for the provided time interval (_NewTime > _OldTime)
				/// </summary>
				/// <param name="_OldTime"></param>
				/// <param name="_NewTime"></param>
				public void		AnimateSequenceForward( float _OldTime, float _NewTime )
				{
					if ( _NewTime < m_TimeStart )
						return;	// Not in the interval yet...

					if ( _OldTime < m_TimeStart && _NewTime >= m_TimeStart )
						m_Owner.NotifyIntervalStart( this );

					if ( _OldTime <= m_TimeEnd && _NewTime > m_TimeEnd )
					{	// We've exited the interval...
						m_Owner.NotifyIntervalEnd( this );
						return;
					}

					// Normalize times
					float	OldTime = (_OldTime - m_TimeStart) * m_TimeNormalizer;
					float	NewTime = (_NewTime - m_TimeStart) * m_TimeNormalizer;

					// Animate tracks
					m_bTrackValuesChanged = false;
					foreach ( AnimationTrack T in m_AnimationTracks )
						T.AnimateSequenceForward( OldTime, NewTime );

					// Notify of any change
					if ( m_bTrackValuesChanged )
						m_Owner.NotifyParameterChanged( this );
				}

				/// <summary>
				/// Animates the interval during the provided time interval (_NewTime < _OldTime)
				/// </summary>
				/// <param name="_OldTime"></param>
				/// <param name="_NewTime"></param>
				public void		AnimateSequenceBackward( float _OldTime, float _NewTime )
				{
					if ( _NewTime > m_TimeEnd )
						return;	// Not in the interval yet...

					if ( _OldTime > m_TimeEnd && _NewTime <= m_TimeEnd )
						m_Owner.NotifyIntervalEnd( this );

					if ( _OldTime >= m_TimeStart && _NewTime < m_TimeStart )
					{	// We've exited the interval...
						m_Owner.NotifyIntervalStart( this );
						return;
					}

					// Normalize times
					float	OldTime = (_OldTime - m_TimeStart) * m_TimeNormalizer;
					float	NewTime = (_NewTime - m_TimeStart) * m_TimeNormalizer;

					// Animate tracks
					m_bTrackValuesChanged = false;
					foreach ( AnimationTrack T in m_AnimationTracks )
						T.AnimateSequenceBackward( OldTime, NewTime );

					// Notify of any change
					if ( m_bTrackValuesChanged )
						m_Owner.NotifyParameterChanged( this );
				}

				/// <summary>
				/// Sets the absolute interval time without animation (simple parameters evaluation, no event triggering)
				/// </summary>
				/// <param name="_NewTime"></param>
				public void		SetTime( float _NewTime )
				{
					m_bTrackValuesChanged = false;
					foreach ( AnimationTrack T in m_AnimationTracks )
						T.SetTime( (_NewTime - m_TimeStart) * m_TimeNormalizer );

					// Notify of any change
					if ( m_bTrackValuesChanged )
						m_Owner.NotifyParameterChanged( this );
				}

				/// <summary>
				/// Gets the index of the provided animation track in our list of tracks
				/// </summary>
				/// <param name="_Track"></param>
				/// <returns></returns>
				public int		IndexOf( AnimationTrack _Track )
				{
					for ( int TrackIndex=0; TrackIndex < m_AnimationTracks.Length; TrackIndex++ )
						if ( m_AnimationTracks[TrackIndex] == _Track )
							return TrackIndex;

					return -1;
				}

				/// <summary>
				/// Forwards a notification a track changed value
				/// </summary>
				/// <param name="_Track"></param>
				internal void	NotifyValueChanged( AnimationTrack _Track )
				{
					// Enable global notification
					m_bTrackValuesChanged = true;

					// Forward to the parent
					m_Owner.NotifyValueChanged( this, _Track );
				}

				#region IComparable<Interval> Members

				public int CompareTo( Interval other )
				{
					if ( m_ActualTimeStart > other.m_ActualTimeStart )
						return 1;	// This interval stands after the other one...
					if ( m_ActualTimeEnd < other.m_ActualTimeEnd )
						return -1;	// This interval stands before the other one...

					return 0;
				}

				#endregion

				#region ISerializable Members

				public void Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_ActualTimeStart );
					_Writer.Write( m_ActualTimeEnd );
					_Writer.Write( m_bCubicInterpolation );
					foreach ( AnimationTrack T in m_AnimationTracks )
						T.Save( _Writer );
				}

				public void Load( System.IO.BinaryReader _Reader )
				{
					m_ActualTimeStart = _Reader.ReadSingle();
					m_ActualTimeEnd = _Reader.ReadSingle();
					m_bCubicInterpolation = _Reader.ReadBoolean();
					foreach ( AnimationTrack T in m_AnimationTracks )
						T.Load( _Reader );
				}

				#endregion

				#endregion

				#region EVENT HANDLERS

				protected void Track_KeysChanged( object sender, EventArgs e )
				{
					// Forward
					if ( KeysChanged != null )
						KeysChanged( this, e );
				}

				protected void Track_KeyValueChanged( AnimationTrack _Sender, AnimationTrack.Key _Key )
				{
					// Forward
					if ( KeyValueChanged != null )
						KeyValueChanged( this, _Key );
				}

				protected void AnimationTrackEvent_Event( AnimationTrackEvent _Sender, int _EventGUID )
				{
					m_Owner.NotifyEventFired( this, _Sender, _EventGUID );
				}

				#endregion
			}

			#endregion

			#region FIELDS

			protected Sequencor				m_Owner = null;

			protected string				m_Name = null;
			protected int					m_GUID = -1;						// The parameter GUID used when sending events
			protected PARAMETER_TYPE		m_Type = PARAMETER_TYPE.UNKNOWN;	// The parameter type
			protected object				m_Tag = null;						// The tag associated to the track (usually, the parameter to animate)
			protected List<Interval>		m_Intervals = new List<Interval>();
			protected bool					m_bCubicInterpolation = true;

			protected float					m_ClipMin = float.NegativeInfinity;
			protected float					m_ClipMax = float.PositiveInfinity;

			protected int					m_CurrentIntervalIndex = 0;
			protected Interval				m_CurrentInterval = null;	

			#endregion

			#region PROPERTIES

			public Sequencor		Owner		{ get { return m_Owner; } }

			public string			Name		{ get { return m_Name; } set { m_Name = value; if ( NameChanged != null ) NameChanged( this, EventArgs.Empty ); } }
			public int				GUID		{ get { return m_GUID; } set { m_GUID = value; if ( GUIDChanged != null ) GUIDChanged( this, EventArgs.Empty ); } }
			public PARAMETER_TYPE	Type		{ get { return m_Type; } }
			public object			Tag			{ get { return m_Tag; } set { m_Tag = value; } }
			public float			ClipMin		{ get { return m_ClipMin; } set { m_ClipMin = value; if ( ClipChanged != null ) ClipChanged( this, EventArgs.Empty ); } }
			public float			ClipMax		{ get { return m_ClipMax; } set { m_ClipMax = value; if ( ClipChanged != null ) ClipChanged( this, EventArgs.Empty ); } }

			public bool				CanClip
			{
				get
				{
					switch ( Type )
					{
						case PARAMETER_TYPE.FLOAT:
						case PARAMETER_TYPE.FLOAT2:
						case PARAMETER_TYPE.FLOAT3:
						case PARAMETER_TYPE.FLOAT4:
							return true;
					}
					return false;
				}
			}

			/// <summary>
			/// Gets the amount of intervals in this track
			/// </summary>
			public int				IntervalsCount	{ get { return m_Intervals.Count; } }

			/// <summary>
			/// Gets the list of intervals of this track
			/// </summary>
			public Interval[]		Intervals	{ get { return m_Intervals.ToArray(); } }

			/// <summary>
			/// Tells if the track can perform cubic interpolation
			/// </summary>
			public bool				CanInterpolateCubic
			{
				get
				{
					switch ( m_Type )
					{
						case PARAMETER_TYPE.FLOAT:
						case PARAMETER_TYPE.FLOAT2:
						case PARAMETER_TYPE.FLOAT3:
						case PARAMETER_TYPE.FLOAT4:
						case PARAMETER_TYPE.PRS:
							return true;
					}

					return false;
				}
			}

			public bool			CubicInterpolation
			{
				get { return m_bCubicInterpolation; }
				set
				{
					if ( value == m_bCubicInterpolation )
						return;

					m_bCubicInterpolation = value;

					foreach ( Interval I in m_Intervals )
						I.CubicInterpolation = value;

					if ( CubicInterpolationChanged != null )
						CubicInterpolationChanged( this, EventArgs.Empty );
				}
			}

			/// <summary>
			/// Gets the last evaluated boolean value
			/// </summary>
			public bool				ValueAsBool		{ get { return m_CurrentInterval != null ? m_CurrentInterval[0].AsBool.Value : false; } }

			/// <summary>
			/// Gets the last evaluated float value
			/// </summary>
			public float			ValueAsFloat	{ get { return m_CurrentInterval != null ? m_CurrentInterval[0].AsFloat.Value : 0.0f; } }

			/// <summary>
			/// Gets the last evaluated float2 value
			/// </summary>
			public Vector2			ValueAsFloat2
			{
				get
				{
					if ( m_CurrentInterval == null )
						return Vector2.Zero;

					Vector2	Result;
					Result.X = m_CurrentInterval[0].AsFloat.Value;
					Result.Y = m_CurrentInterval[1].AsFloat.Value;

					return Result;
				}
			}

			/// <summary>
			/// Gets the last evaluated float3 value
			/// </summary>
			public Vector3			ValueAsFloat3
			{
				get
				{
					if ( m_CurrentInterval == null )
						return Vector3.Zero;

					Vector3	Result;
					Result.X = m_CurrentInterval[0].AsFloat.Value;
					Result.Y = m_CurrentInterval[1].AsFloat.Value;
					Result.Z = m_CurrentInterval[2].AsFloat.Value;

					return Result;
				}
			}

			/// <summary>
			/// Gets the last evaluated float4 value
			/// </summary>
			public Vector4			ValueAsFloat4
			{
				get
				{
					if ( m_CurrentInterval == null )
						return Vector4.Zero;

					Vector4	Result;
					Result.X = m_CurrentInterval[0].AsFloat.Value;
					Result.Y = m_CurrentInterval[1].AsFloat.Value;
					Result.Z = m_CurrentInterval[2].AsFloat.Value;
					Result.W = m_CurrentInterval[3].AsFloat.Value;

					return Result;
				}
			}

			/// <summary>
			/// Gets the last evaluated PRS value in the form of a 4x4 matrix
			/// </summary>
			public Matrix		ValueAsPRS
			{
				get
				{
					if ( m_CurrentInterval == null )
						return Matrix.Identity;

					Vector3	Position;
					Position.X = m_CurrentInterval[0].AsFloat.Value;
					Position.Y = m_CurrentInterval[1].AsFloat.Value;
					Position.Z = m_CurrentInterval[2].AsFloat.Value;

					Quaternion	Rotation = m_CurrentInterval[3].AsQuat.Value;

					Vector3	Scale;
					if ( m_CurrentInterval[4].KeysCount > 0 )
					{
						Scale.X = m_CurrentInterval[4].AsFloat.Value;
						Scale.Y = m_CurrentInterval[5].AsFloat.Value;
						Scale.Z = m_CurrentInterval[6].AsFloat.Value;
					}
					else
						Scale = Vector3.One;

					Matrix	Result;
					Matrix.RotationQuaternion( ref Rotation, out Result );

					Result[0,0] *= Scale.X;
					Result[0,1] *= Scale.X;
					Result[0,2] *= Scale.X;
					Result[1,0] *= Scale.Y;
					Result[1,1] *= Scale.Y;
					Result[1,2] *= Scale.Y;
					Result[2,0] *= Scale.Z;
					Result[2,1] *= Scale.Z;
					Result[2,2] *= Scale.Z;
					Result[3,0] = Position.X;
					Result[3,1] = Position.Y;
					Result[3,2] = Position.Z;

					return Result;
				}
			}

			/// <summary>
			/// Occurs when the track is created/reloaded and a tag is needed
			/// </summary>
			public event TagNeededEventHander		TagNeeded;

			/// <summary>
			/// Occurs when the time cursor enters a new interval
			/// </summary>
			public event IntervalEventHandler		IntervalStart;

			/// <summary>
			/// Occurs when the time cursor exits an interval
			/// </summary>
			public event IntervalEventHandler		IntervalEnd;

			/// <summary>
			/// Occurs when a parameter's value changed
			/// </summary>
			public event ValueChangedEventHandler	ValueChanged;

			/// <summary>
			/// Occurs when a parameter's value changed
			/// </summary>
			public event ParameterChangedEventHandler 	ParameterChanged;

			/// <summary>
			/// Occurs when this is an EVENT parameter and an event is fired
			/// </summary>
			public event EventFiredEventHandler		EventFired;

			/// <summary>
			/// Occurs when the list of intervals changed
			/// </summary>
			public event EventHandler				IntervalsChanged;

			/// <summary>
			/// Occurs when the parameter name changed
			/// </summary>
			public event EventHandler				NameChanged;

			/// <summary>
			/// Occurs when the parameter GUID changed
			/// </summary>
			public event EventHandler				GUIDChanged;

			/// <summary>
			/// Occurs when the parameter clipping range changed
			/// </summary>
			public event EventHandler				ClipChanged;

			/// <summary>
			/// Occurs when the cubic interpolation state changed
			/// </summary>
			public event EventHandler				CubicInterpolationChanged;

			#endregion

			#region METHODS

			public ParameterTrack( Sequencor _Owner, string _Name, int _GUID, PARAMETER_TYPE _Type ) : this( _Owner )
			{
				m_Name = _Name;
				m_GUID = _GUID;
				m_Type = _Type;
			}

			// Constructor used when reloading track
			internal ParameterTrack( Sequencor _Owner )
			{
				m_Owner = _Owner;
			}

			/// <summary>
			/// Creates a new animation interval
			/// </summary>
			/// <param name="_TimeStart"></param>
			/// <param name="_TimeEnd"></param>
			/// <returns></returns>
			public Interval	CreateInterval( float _TimeStart, float _TimeEnd )
			{
				Interval	Result = new Interval( this );
				m_Intervals.Add( Result );
				Result.ActualTimeStart = _TimeStart;
				Result.ActualTimeEnd = _TimeEnd;
				Result.CubicInterpolation = CubicInterpolation;

				SortIntervals();

				return Result;
			}

			/// <summary>
			/// Creates a new animation interval from a binary content created using the Save() method
			/// </summary>
			/// <param name="_IntervalStream"></param>
			/// <returns></returns>
			public Interval	CreateInterval( System.IO.BinaryReader _IntervalStream )
			{
				// Create a new interval
				Interval	Result = new Interval( this );
				m_Intervals.Add( Result );

				// Load into new interval
				Result.Load( _IntervalStream );

				// Sort
				SortIntervals();

				return Result;
			}

			/// <summary>
			/// Removes an animation interval
			/// </summary>
			/// <param name="_Interval"></param>
			public void		RemoveInterval( Interval _Interval )
			{
				m_Intervals.Remove( _Interval );

				SortIntervals();
			}

			/// <summary>
			/// Clones a source interval
			/// </summary>
			/// <param name="_Interval"></param>
			/// <returns></returns>
			public Interval	Clone( Interval _Interval )
			{
				if ( _Interval == null )
					throw new Exception( "Invalid source interval to clone !" );
				if ( _Interval.ParentTrack.Type != Type )
					throw new Exception( "Source interval is not part of a " + Type + " parameter !" );

				// Save to memory
				System.IO.MemoryStream	Buffer = new System.IO.MemoryStream();
				System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Buffer );
				System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Buffer );
				_Interval.Save( Writer );
				Buffer.Position = 0;

				// Load into new interval
				Interval	Result = CreateInterval( Reader );

				// Dispose of memory
				Writer.Dispose();
				Reader.Dispose();
				Buffer.Dispose();

				return Result;
			}

			/// <summary>
			/// Gets the index of an interval or -1 if not found
			/// </summary>
			/// <param name="_Interval"></param>
			/// <returns></returns>
			public int		IndexOf( Interval _Interval )
			{
				for ( int IntervalIndex=0; IntervalIndex < m_Intervals.Count; IntervalIndex++ )
					if ( _Interval == m_Intervals[IntervalIndex] )
						return IntervalIndex;

				return -1;
			}

			/// <summary>
			/// Animates the parameter for the provided time interval (_NewTime > _OldTime)
			/// </summary>
			/// <param name="_OldTime"></param>
			/// <param name="_NewTime"></param>
			public void		AnimateSequenceForward( float _OldTime, float _NewTime )
			{
				// Cycle forward
				while ( m_CurrentInterval == null || _NewTime > m_CurrentInterval.TimeEnd )
				{
					if ( m_CurrentIntervalIndex >= m_Intervals.Count-1 )
					{	// We've used all intervals. We're now done with this track...
						m_CurrentInterval = null;
						return;
					}

					// Get the next interval
					m_CurrentInterval = m_Intervals[++m_CurrentIntervalIndex];
					m_CurrentInterval.SetTime( _OldTime );	// Prepare next keys
				}

				// Animate in current interval
				if ( m_CurrentInterval != null )
					m_CurrentInterval.AnimateSequenceForward( _OldTime, _NewTime );
			}

			/// <summary>
			/// Animates the parameter during the provided time interval (_NewTime < _OldTime)
			/// </summary>
			/// <param name="_OldTime"></param>
			/// <param name="_NewTime"></param>
			public void		AnimateSequenceBackward( float _OldTime, float _NewTime )
			{
				// Cycle backward
				while ( m_CurrentInterval == null || _NewTime < m_CurrentInterval.TimeStart )
				{
					if ( m_CurrentIntervalIndex <= 0 )
					{	// We've used all intervals. We're now done with this track...
						m_CurrentInterval = null;
						return;
					}

					// Get the previous interval
					m_CurrentInterval = m_Intervals[--m_CurrentIntervalIndex];
					m_CurrentInterval.SetTime( _OldTime );	// Prepare next keys
				}

				// Animate in current interval
				if ( m_CurrentInterval != null )
					m_CurrentInterval.AnimateSequenceBackward( _OldTime, _NewTime );
			}

			/// <summary>
			/// Sets the absolute track time without animation (simple parameters evaluation, no event triggering)
			/// </summary>
			/// <param name="_NewTime"></param>
			public void		SetTime( float _NewTime )
			{
				for ( m_CurrentIntervalIndex=0; m_CurrentIntervalIndex < m_Intervals.Count; m_CurrentIntervalIndex++ )
				{
					Interval	I = m_Intervals[m_CurrentIntervalIndex];
					if ( _NewTime >= I.TimeStart && _NewTime <= I.TimeEnd )
					{	// Found the interval we're in !
						m_CurrentInterval = I;
						m_CurrentInterval.SetTime( _NewTime );
						return;
					}
				}

				// Failed to find any interval...
				m_CurrentInterval = null;
				m_CurrentIntervalIndex = 0;
			}

			/// <summary>
			/// Moves the track up
			/// </summary>
			public void		MoveUp()
			{
				m_Owner.MoveUp( this );
			}

			/// <summary>
			/// Moves the track down
			/// </summary>
			public void		MoveDown()
			{
				m_Owner.MoveDown( this );
			}

			/// <summary>
			/// Moves the track to top
			/// </summary>
			public void		MoveTop()
			{
				m_Owner.MoveTop( this );
			}

			/// <summary>
			/// Moves the track to bottom
			/// </summary>
			public void		MoveBottom()
			{
				m_Owner.MoveBottom( this );
			}

			/// <summary>
			/// Re-order intervals
			/// </summary>
			internal void	SortIntervals()
			{
				m_Intervals.Sort();
				UpdateIntervalTimes();

				// Notify
				if ( IntervalsChanged != null )
					IntervalsChanged( this, EventArgs.Empty );
			}

			/// <summary>
			/// Ensures all (already sorted) intervals have "visible times" that don't overlap
			/// The unique rule is that preceding intervals's end time stays the same so current interval's start time gets compressed
			/// </summary>
			internal void	UpdateIntervalTimes()
			{
				Interval	Previous = null;
				for ( int IntervalIndex=0; IntervalIndex < m_Intervals.Count; IntervalIndex++ )
				{
					Interval	Current = m_Intervals[IntervalIndex];
					if ( Previous != null )
					{	// Always keep predecessor's end time as our boundary
						Current.TimeStart = Math.Max( Current.ActualTimeStart, Previous.ActualTimeEnd );
						Current.TimeEnd = Math.Max( Current.ActualTimeEnd, Previous.ActualTimeEnd );
					}
					else
					{	// Actual & visible times are the same
						Current.TimeStart = Current.ActualTimeStart;
						Current.TimeEnd = Current.ActualTimeEnd;
					}

					Previous = Current;
				}
			}

			/// <summary>
			/// Notifies we entered an interval
			/// </summary>
			/// <param name="_Interval"></param>
			protected void	NotifyIntervalStart( Interval _Interval )
			{
				if ( IntervalStart != null )
					IntervalStart( this, _Interval );

				// Forward to our owner's aggregator event
				m_Owner.NotifyIntervalStart( this, _Interval );
			}

			/// <summary>
			/// Notifies we exited an interval
			/// </summary>
			/// <param name="_Interval"></param>
			protected void	NotifyIntervalEnd( Interval _Interval )
			{
				if ( IntervalEnd != null )
					IntervalEnd( this, _Interval );

				// Forward to our owner's aggregator event
				m_Owner.NotifyIntervalEnd( this, _Interval );
			}

			/// <summary>
			/// Notifies a parameter changed globally (i.e. any animation track in the parameter track could have changed)
			/// </summary>
			protected void	NotifyParameterChanged( ParameterTrack.Interval _Interval )
			{
				if ( ParameterChanged != null )
					ParameterChanged( this, _Interval );

				// Forward to our owner's aggregator event
				m_Owner.NotifyParameterChanged( this, _Interval );
			}

			/// <summary>
			/// Notifies a value in one of our parameter tracks changed
			/// </summary>
			/// <param name="_Track"></param>
			/// <param name="_Interval"></param>
			protected void	NotifyValueChanged( ParameterTrack.Interval _Interval, AnimationTrack _Track )
			{
				if ( ValueChanged != null )
					ValueChanged( this, _Interval, _Track );

				// Forward to our owner's aggregator event
				m_Owner.NotifyValueChanged( this, _Interval, _Track );
			}

			protected void	NotifyEventFired( ParameterTrack.Interval _Interval, AnimationTrackEvent _Track, int _EventGUID )
			{
				if ( EventFired != null )
					EventFired( this, _Interval, _Track, _EventGUID );

				// Forward to our owner's aggregator event
				m_Owner.NotifyEventFired( this, _Interval, _Track, _EventGUID );
			}

			#region ISerializable Members

			public void Save( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( m_Name );
				_Writer.Write( m_GUID );
				_Writer.Write( (int) m_Type );
				_Writer.Write( m_bCubicInterpolation );
				_Writer.Write( m_ClipMin );
				_Writer.Write( m_ClipMax );

				// Save intervals
				_Writer.Write( m_Intervals.Count );
				foreach ( Interval I in m_Intervals )
					I.Save( _Writer );
			}

			public void Load( System.IO.BinaryReader _Reader )
			{
				m_Name = _Reader.ReadString();
				m_GUID = _Reader.ReadInt32();
				m_Type = (PARAMETER_TYPE) _Reader.ReadInt32();
				m_bCubicInterpolation = _Reader.ReadBoolean();
				m_ClipMin = _Reader.ReadSingle();
				m_ClipMax = _Reader.ReadSingle();

				if ( TagNeeded != null )
					m_Tag = TagNeeded( this );	// Re-assign tag

				// Load intervals
				int IntervalsCount = _Reader.ReadInt32();
				for ( int IntervalIndex=0; IntervalIndex < IntervalsCount; IntervalIndex++ )
				{
					Interval	I = new Interval( this );
					m_Intervals.Add( I );
					I.Load( _Reader );
				}

				UpdateIntervalTimes();
				SortIntervals();
			}

			#endregion

			#endregion
		}

		#endregion

		#region FIELDS

		// The list of parameter tracks
		protected List<ParameterTrack>	m_Tracks = new List<ParameterTrack>();

		// Main time in seconds
		protected float					m_Time = 0.0f;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the sequencer time
		/// </summary>
		public float		Time
		{
			get { return m_Time; }
			set { float OldTime = m_Time; m_Time = value; AnimateSequence( OldTime, m_Time ); }
		}

		/// <summary>
		/// Gets the list of parameter tracks hosted by the sequencer
		/// </summary>
		public ParameterTrack[]	Tracks
		{
			get { return m_Tracks.ToArray(); }
		}

		/// <summary>
		/// Gets the amount of available parameter tracks
		/// </summary>
		public int			TracksCount
		{
			get { return m_Tracks.Count; }
		}

		/// <summary>
		/// Occurs when a parameter track needs tagging
		/// </summary>
		public event TagNeededEventHander		TagNeeded;

		/// <summary>
		/// Occurs when the time cursor enters a new interval
		/// </summary>
		public event IntervalEventHandler		IntervalStart;

		/// <summary>
		/// Occurs when the time cursor exits an interval
		/// </summary>
		public event IntervalEventHandler		IntervalEnd;

		/// <summary>
		/// Occurs when a parameter's value changed in one of the animation tracks (e.g. you'll be notified 3 times if the 3 components of a float3 change)
		/// </summary>
		public event ValueChangedEventHandler	ValueChanged;

		/// <summary>
		/// Occurs when a parameter's value changed globally (i.e. no particular animation track is qualified) (e.g. you'll be notified only once even if the 3 components of a float3 change)
		/// </summary>
		public event ParameterChangedEventHandler	ParameterChanged;

		/// <summary>
		/// Occurs when an EVENT parameter's event is fired
		/// </summary>
		public event EventFiredEventHandler		EventFired;

		/// <summary>
		/// Occurs when the list of tracks changed
		/// </summary>
		public event EventHandler				TracksChanged;

		#endregion

		#region METHODS

		public Sequencor()
		{
		}

		/// <summary>
		/// Creates a new parameter track
		/// </summary>
		/// <param name="_Name">The name of the track to create</param>
		/// <param name="_GUID">A unique identifier for the track</param>
		/// <param name="_Type">The type of track to create</param>
		/// <param name="_Tag">The tag associated to the track (usually, the parameter to animate)</param>
		/// <returns></returns>
		public ParameterTrack	CreateTrack( string _Name, int _GUID, ParameterTrack.PARAMETER_TYPE _Type, object _Tag )
		{
			ParameterTrack	Result = new ParameterTrack( this, _Name, _GUID, _Type );
			Result.Tag = _Tag;

			m_Tracks.Add( Result );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );

			return Result;
		}

		/// <summary>
		/// Creates a new parameter track from a binary content created using the Save() method
		/// </summary>
		/// <param name="_ParameterTrackStream"></param>
		/// <returns></returns>
		public ParameterTrack	CreateTrack( System.IO.BinaryReader _ParameterTrackStream )
		{
			// Create a new interval
			ParameterTrack	Result = new ParameterTrack( this );
			m_Tracks.Add( Result );

			// Load into new interval
			Result.Load( _ParameterTrackStream );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );

			return Result;
		}

		/// <summary>
		/// Removes an existing parameter track
		/// </summary>
		/// <param name="_Track"></param>
		public void		RemoveTrack( ParameterTrack _Track )
		{
			m_Tracks.Remove( _Track );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Clones a source parameter track
		/// </summary>
		/// <param name="_ParameterTrack"></param>
		/// <returns></returns>
		public ParameterTrack	Clone( ParameterTrack _ParameterTrack )
		{
			if ( _ParameterTrack == null )
				throw new Exception( "Invalid source interval to clone !" );

			// Save to memory
			System.IO.MemoryStream	Buffer = new System.IO.MemoryStream();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Buffer );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Buffer );
			_ParameterTrack.Save( Writer );
			Buffer.Position = 0;

			// Load into new track
			ParameterTrack	Result = CreateTrack( Reader );

			// Dispose of memory
			Writer.Dispose();
			Reader.Dispose();
			Buffer.Dispose();

			return Result;
		}

		/// <summary>
		/// Finds a parameter track by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public ParameterTrack	FindParameterTrack( string _Name )
		{
			foreach ( ParameterTrack Track in m_Tracks )
				if ( Track.Name == _Name )
					return Track;

			return null;
		}

		/// <summary>
		/// Gets the index of the provided parameter track or -1 if not found
		/// </summary>
		/// <param name="_Track"></param>
		/// <returns></returns>
		public int		IndexOf( ParameterTrack _Track )
		{
			for ( int TrackIndex=0; TrackIndex < m_Tracks.Count; TrackIndex++ )
				if ( m_Tracks[TrackIndex] == _Track )
					return TrackIndex;

			return -1;
		}

		/// <summary>
		/// Animates the registered parameters during the provided time interval
		/// </summary>
		/// <param name="_OldTime"></param>
		/// <param name="_NewTime"></param>
		public void		AnimateSequence( float _OldTime, float _NewTime )
		{
			m_Time = _NewTime;
			if ( _NewTime > _OldTime )
				AnimateSequenceForward( _OldTime, _NewTime );
			else if ( _NewTime < _OldTime )
				AnimateSequenceBackward( _OldTime, _NewTime );
		}

		/// <summary>
		/// Sets the absolution track time without animation (simple parameters evaluation, no event triggering)
		/// </summary>
		/// <param name="_NewTime"></param>
		public void		SetTime( float _NewTime )
		{
			m_Time = _NewTime;
			foreach ( ParameterTrack T in m_Tracks )
				T.SetTime( _NewTime );
		}

		/// <summary>
		/// Animates the registered parameters during the provided time interval
		/// </summary>
		/// <param name="_OldTime"></param>
		/// <param name="_NewTime"></param>
		protected void	AnimateSequenceForward( float _OldTime, float _NewTime )
		{
			foreach ( ParameterTrack T in m_Tracks )
				T.AnimateSequenceForward( _OldTime, _NewTime );
		}

		/// <summary>
		/// Animates the registered parameters during the provided time interval
		/// </summary>
		/// <param name="_OldTime"></param>
		/// <param name="_NewTime"></param>
		protected void	AnimateSequenceBackward( float _OldTime, float _NewTime )
		{
			foreach ( ParameterTrack T in m_Tracks )
				T.AnimateSequenceBackward( _OldTime, _NewTime );
		}

		/// <summary>
		/// Moves a track down (i.e. next)
		/// </summary>
		/// <param name="_Track"></param>
		protected void		MoveDown( ParameterTrack _Track )
		{
			int	Index = m_Tracks.IndexOf( _Track );
			if ( Index == m_Tracks.Count-1 )
				return;	// Already last...

			m_Tracks.Remove( _Track );
			m_Tracks.Insert( Index+1, _Track );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Moves a track up (i.e. previous)
		/// </summary>
		/// <param name="_Track"></param>
		protected void		MoveUp( ParameterTrack _Track )
		{
			int	Index = m_Tracks.IndexOf( _Track );
			if ( Index == 0 )
				return;	// Already first...

			m_Tracks.Remove( _Track );
			m_Tracks.Insert( Index-1, _Track );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Moves a track to bottom (i.e. last)
		/// </summary>
		/// <param name="_Track"></param>
		protected void		MoveBottom( ParameterTrack _Track )
		{
			int	Index = m_Tracks.IndexOf( _Track );
			if ( Index == m_Tracks.Count-1 )
				return;	// Already last...

			m_Tracks.Remove( _Track );
			m_Tracks.Add( _Track );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Moves a track to top (i.e. first)
		/// </summary>
		/// <param name="_Track"></param>
		protected void		MoveTop( ParameterTrack _Track )
		{
			int	Index = m_Tracks.IndexOf( _Track );
			if ( Index == 0 )
				return;	// Already first...

			m_Tracks.Remove( _Track );
			m_Tracks.Insert( 0, _Track );

			// Notify
			if ( TracksChanged != null )
				TracksChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Loads a sequencer binary file
		/// </summary>
		/// <param name="_File"></param>
		public void		LoadFromBinaryFile( System.IO.FileInfo _File )
		{
			System.IO.FileStream	Stream = _File.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			Load( Reader );
			Reader.Close();
			Reader.Dispose();
			Stream.Close();
			Stream.Dispose();
		}

		/// <summary>
		/// Loads a sequencer binary file in memory
		/// </summary>
		/// <param name="_File"></param>
		public void		LoadFromBinaryMemory( byte[] _File )
		{
			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( _File );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			Load( Reader );
			Reader.Close();
			Reader.Dispose();
			Stream.Close();
			Stream.Dispose();
		}

		#region Notifications Aggregation

		/// <summary>
		/// Notifies we entered a new interval in one of our parameter tracks
		/// </summary>
		/// <param name="_Track"></param>
		/// <param name="_Interval"></param>
		protected void	NotifyIntervalStart( ParameterTrack _Parameter, ParameterTrack.Interval _Interval )
		{
			if ( IntervalStart != null )
				IntervalStart( _Parameter, _Interval );
		}

		/// <summary>
		/// Notifies we exited an interval in one of our parameter tracks
		/// </summary>
		/// <param name="_Track"></param>
		/// <param name="_Interval"></param>
		protected void	NotifyIntervalEnd( ParameterTrack _Parameter, ParameterTrack.Interval _Interval )
		{
			if ( IntervalEnd != null )
				IntervalEnd( _Parameter, _Interval );
		}

		/// <summary>
		/// Notifies a parameter changed globally (i.e. any animation track in the parameter track could have changed)
		/// </summary>
		protected void	NotifyParameterChanged( ParameterTrack _Parameter, ParameterTrack.Interval _Interval )
		{
			if ( ParameterChanged != null )
				ParameterChanged( _Parameter, _Interval );
		}

		/// <summary>
		/// Notifies a value in one of the animation tracks of one of our parameter changed
		/// </summary>
		/// <param name="_Track"></param>
		/// <param name="_Interval"></param>
		protected void	NotifyValueChanged( ParameterTrack _Parameter, ParameterTrack.Interval _Interval, AnimationTrack _Track )
		{
			if ( ValueChanged != null )
				ValueChanged( _Parameter, _Interval, _Track );
		}

		/// <summary>
		/// Notifies an EVENT parameter fired an event
		/// </summary>
		/// <param name="_Interval"></param>
		/// <param name="_Track"></param>
		/// <param name="_EventGUID"></param>
		protected void	NotifyEventFired( ParameterTrack _Parameter, ParameterTrack.Interval _Interval, AnimationTrackEvent _Track, int _EventGUID )
		{
			if ( EventFired != null )
				EventFired( _Parameter, _Interval, _Track, _EventGUID );
		}

		#endregion

		#region ISerializable Members

		public void Save( System.IO.BinaryWriter _Writer )
		{
			_Writer.Write( m_Tracks.Count );
			foreach ( ParameterTrack T in m_Tracks )
				T.Save( _Writer );
		}

		public void Load( System.IO.BinaryReader _Reader )
		{
			int TracksCount = _Reader.ReadInt32();
			for ( int TrackIndex=0; TrackIndex < TracksCount; TrackIndex++ )
			{
				ParameterTrack T = new ParameterTrack( this );
				m_Tracks.Add( T );
				T.TagNeeded += new TagNeededEventHander( ParameterTrack_TagNeeded );

				T.Load( _Reader );
			}

			// Set time to 0 to initialize current data properly
			SetTime( 0.0f );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected object ParameterTrack_TagNeeded( Sequencor.ParameterTrack _Track )
		{
			return TagNeeded != null ? TagNeeded( _Track ) : null;
		}

		#endregion
	}
}
