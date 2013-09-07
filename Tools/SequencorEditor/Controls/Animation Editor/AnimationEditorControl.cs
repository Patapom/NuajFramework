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
	/// This control represents an animation editor for a parameter track
	/// If focuses on a single track and allows to create/remove/manipulate animation keys in the animation intervals of this track
	/// </summary>
	[System.Diagnostics.DebuggerDisplay( "Track={Track} Selected={Selected}" )]
	public partial class AnimationEditorControl : UserControl
	{
		#region CONSTANTS

		protected const float	ANCHOR_PIXEL_TOLERANCE	= 4.0f;	// Anchor to an interval boundary if less than 4 pixels appart

		#endregion

		#region NESTED TYPES

		protected enum MANIPULATION_TYPE
		{
			KEY,
			TANGENT_IN,
			TANGENT_OUT
		}

		#endregion

		#region FIELDS

		protected SequencerControl			m_Owner = null;
		protected Sequencor.ParameterTrack	m_Track = null;

		// Context Menu
		protected PointF					m_ContextMenuPosition = PointF.Empty;
		protected Sequencor.ParameterTrack.Interval	m_HoveredInterval = null;
		protected Sequencor.AnimationTrack.Key	m_HoveredKey = null;
		protected Sequencor.AnimationTrack.Key	m_HoveredKeyTangentIn = null;
		protected Sequencor.AnimationTrack.Key	m_HoveredKeyTangentOut = null;

		// Keys manipulation
		protected MouseButtons				m_MouseButtonsDown = MouseButtons.None;
		protected Sequencor.AnimationTrack.Key		m_ManipulatedKey = null;	// The key we're manipulating
		protected Sequencor.AnimationTrack.Key[]	m_ManipulatedBuddyKeys = null;	// The buddy keys we're manipulating
		protected AnimationTrackPanel.KEY_TYPE		m_ManipulatedKeyType = AnimationTrackPanel.KEY_TYPE.DEFAULT;
		protected MANIPULATION_TYPE					m_ManipulationType = MANIPULATION_TYPE.KEY;
		protected Sequencor.AnimationTrack.Key		m_ManipulatedKeyTangentIn = null;	// The key's IN tangent we're manipulating
		protected Sequencor.AnimationTrack.Key		m_ManipulatedKeyTangentOut = null;	// The key's IN tangent we're manipulating
		protected PointF					m_MouseDownPosition = PointF.Empty;
		protected PointF					m_MouseDownKeyClientPos;
		protected PointF					m_MouseDownKeyClientPosOpposite;	// Used to store mouse down position of opposite tangents

		protected bool						m_bInternalChange = false;

		#endregion

		#region PROPERTIES

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
					m_Track.CubicInterpolationChanged -= new EventHandler( Track_CubicInterpolationChanged );
					m_Track.ClipChanged -= new EventHandler( Track_ClipChanged );
					animationTrackPanel.Enabled = false;
					gradientTrackPanel.Enabled = false;
					checkBoxInterpolation.Visible = false;
					checkBoxShowTangents.Visible = false;
					checkBoxGradient.Visible = false;
					buttonSampleValue.Visible = false;
				}

				m_Track = value;

				if ( m_Track != null )
				{
					m_Track.CubicInterpolationChanged += new EventHandler( Track_CubicInterpolationChanged );
					Track_CubicInterpolationChanged( m_Track, EventArgs.Empty );
					m_Track.ClipChanged += new EventHandler( Track_ClipChanged );
					animationTrackPanel.Enabled = true;
					gradientTrackPanel.Enabled = true;
					checkBoxInterpolation.Visible = m_Track.CanInterpolateCubic;
					checkBoxShowTangents.Visible = m_Track.CanInterpolateCubic;
					buttonSampleValue.Visible = m_Owner.CanQueryParameterValue;
					buttonSampleValue.Enabled = false;

					// Enable gradient for specific types
					switch ( m_Track.Type )
					{
						case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
						case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
						case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
							checkBoxGradient.Visible = true;
							break;

						default:
							checkBoxGradient.Visible = false;
							break;
					}

					// Update clipping values
					if ( m_Track.CanClip )
					{	// If the clip box is visble, we use more vertical space
						groupBoxClipping.Visible = true;

						if ( !float.IsInfinity( m_Track.ClipMin ) )
						{
							floatTrackbarControlClipMin.Value = m_Track.ClipMin;
							checkBoxClipMinInfinity.Checked = false;
						}
						else
							checkBoxClipMinInfinity.Checked = true;

						if ( !float.IsInfinity( m_Track.ClipMax ) )
						{
							floatTrackbarControlClipMax.Value = m_Track.ClipMax;
							checkBoxClipMaxInfinity.Checked = false;
						}
						else
							checkBoxClipMaxInfinity.Checked = true;
					}
					else
					{	// If the clip box is invisble, we can fold more
						groupBoxClipping.Visible = false;
					}

					// Constrain absolute minimum size
					if ( MinimumSize.Height < VerticalSizeLimit )
						MinimumSize = new Size( 0, VerticalSizeLimit );
				}

				// Update the GUI
				m_bInternalChange = true;
				animationTrackPanel.Track = m_Track;
				gradientTrackPanel.Track = m_Track;
				Enabled = m_Track != null;
				m_bInternalChange = false;
			}
		}

		public int	VerticalSizeLimit
		{
			get { return m_Track != null ? (m_Track.CanClip ? 265 : 150) : 150; }
		}

		[Browsable( false )]
		public Sequencor.ParameterTrack.Interval	SelectedInterval
		{
			get { return animationTrackPanel.SelectedInterval; }
			set
			{
				if ( value == animationTrackPanel.SelectedInterval )
					return;

				if ( animationTrackPanel.SelectedInterval != null )
				{
					animationTrackPanel.SelectedInterval.ActualTimeStartChanged -= new EventHandler( SelectedInterval_ActualTimeStartChanged );
					animationTrackPanel.SelectedInterval.ActualTimeEndChanged -= new EventHandler( SelectedInterval_ActualTimeEndChanged );

					floatTrackbarControlIntervalStart.Enabled = false;
					floatTrackbarControlIntervalDuration.Enabled = false;
					floatTrackbarControlIntervalEnd.Enabled = false;

					buttonZoomOut.Enabled = false;
				}

				animationTrackPanel.SelectedInterval = value;

				if ( animationTrackPanel.SelectedInterval != null )
				{
					animationTrackPanel.SelectedInterval.ActualTimeStartChanged += new EventHandler( SelectedInterval_ActualTimeStartChanged );
					SelectedInterval_ActualTimeStartChanged( animationTrackPanel.SelectedInterval, EventArgs.Empty );
					animationTrackPanel.SelectedInterval.ActualTimeEndChanged += new EventHandler( SelectedInterval_ActualTimeEndChanged );
					SelectedInterval_ActualTimeEndChanged( animationTrackPanel.SelectedInterval, EventArgs.Empty );

					floatTrackbarControlIntervalDuration.Value = SelectedInterval.ActualTimeEnd - SelectedInterval.ActualTimeStart;

					floatTrackbarControlIntervalStart.Enabled = true;
					floatTrackbarControlIntervalDuration.Enabled = true;
					floatTrackbarControlIntervalEnd.Enabled = true;

					buttonZoomOut.Enabled = true;
				}

				// Notify
				if ( SelectedIntervalChanged != null )
					SelectedIntervalChanged( this, EventArgs.Empty );
			}
		}

		[Browsable( false )]
		public Sequencor.AnimationTrack.Key			SelectedKey
		{
			get { return animationTrackPanel.SelectedKey; }
			set
			{
				if ( value == SelectedKey )
					return;

				animationTrackPanel.SelectedKey = value;
				gradientTrackPanel.SelectedKey = value;
				buttonSampleValue.Enabled = value != null;

				// Notify
				if ( SelectedKeyChanged != null )
					SelectedKeyChanged( this, EventArgs.Empty );
			}
		}

		public bool			ShowGradientTrack
		{
			get { return checkBoxGradient.Enabled && checkBoxGradient.Checked; }
			set
			{
				if ( checkBoxGradient.Enabled )
					checkBoxGradient.Checked = value;
			}
		}

		public bool			ShowTangents
		{
			get { return checkBoxShowTangents.Checked && checkBoxInterpolation.Checked; }
			set { checkBoxShowTangents.Checked = value; }
		}

		/// <summary>
		/// Occurs when the selected interval changed
		/// </summary>
		public event EventHandler					SelectedIntervalChanged;

		/// <summary>
		/// Occurs when the selected key changed
		/// </summary>
		public event EventHandler					SelectedKeyChanged;

		/// <summary>
		/// Occurs when the user presses the exit button or the escape key
		/// </summary>
		public event EventHandler					Exit;

		protected bool		IsAnchorMode
		{
			get { return (ModifierKeys & Keys.Alt) == 0; }	// Alt disables anchoring mode
		}

		protected bool		IsAxisConstraintMode
		{
			get { return (ModifierKeys & Keys.Shift) != 0; }	// Shift constrains motion horizontally or vertically
		}

		protected bool		IsCopyMode
		{
			get { return (ModifierKeys & Keys.Control) != 0; }
		}

		protected bool		IsTangentBreakMode
		{
			get { return (ModifierKeys & Keys.Alt) != 0; }	// Alt enables individual tangent manipulation
		}

		#endregion

		#region METHODS

		public AnimationEditorControl()
		{
			InitializeComponent();

			animationTrackPanel.Owner = this;
			animationTrackPanel.MouseWheel += new MouseEventHandler( animationTrackPanel_MouseWheel );
			gradientTrackPanel.Owner = this;
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

		protected override void OnResize( EventArgs e )
		{
			base.OnResize( e );
			Refresh();
		}

		/// <summary>
		/// Zooms on a specific interval
		/// </summary>
		/// <param name="_Interval"></param>
		public void		ZoomOnInterval( Sequencor.ParameterTrack.Interval _Interval )
		{
			float	Duration = _Interval.Duration;
			SetRange( _Interval.TimeStart - 0.05f * Duration, _Interval.TimeEnd + 0.05f * Duration );
		}

		/// <summary>
		/// Sets both min and max range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetRange( float _RangeMin, float _RangeMax )
		{
			animationTrackPanel.SetHorizontalRange( _RangeMin, _RangeMax );
			gradientTrackPanel.SetRange( _RangeMin, _RangeMax );
		}

		/// <summary>
		/// Sets both min and max vertical range with a single update
		/// </summary>
		/// <param name="_RangeMin">The new min range</param>
		/// <param name="_RangeMax">The new max range</param>
		public void		SetVerticalRange( float _VerticalRangeMin, float _VerticalRangeMax )
		{
			animationTrackPanel.SetVerticalRange( _VerticalRangeMin, _VerticalRangeMax );
		}

		public bool	UserCreateNewKey( PointF _SequencePosition, AnimationTrackPanel.KEY_TYPE _KeyType )
		{
			return UserCreateNewKey( _SequencePosition, _KeyType, false );
		}

		/// <summary>
		/// Spawns a form to edit an create a new key
		/// </summary>
		/// <param name="_SequencePosition">The position in SEQUENCE space to create the key at</param>
		/// <param name="_KeyType">The optional key type if creating a PRS key</param>
		/// <param name="_bSilentUseCurrentValues">True to ony use the form as a vessel to carry current parameter and create a key without asking for user interaction</param>
		public bool	UserCreateNewKey( PointF _SequencePosition, AnimationTrackPanel.KEY_TYPE _KeyType, bool _bSilentUseCurrentValues )
		{
			// Spawn the key editor form
			KeyEditorForm	F = new KeyEditorForm( m_Owner );

			F.Interval = SelectedInterval;
			F.KeyType = _KeyType;

			F.KeyTime = _SequencePosition.X;
			if ( _KeyType != AnimationTrackPanel.KEY_TYPE.ROTATION )
				F.ValueFloat4 = _SequencePosition.Y * SharpDX.Vector4.One;
			else
			{
				F.ValueRotationAxis = SharpDX.Vector3.UnitX;
				F.ValueRotationAngle = _SequencePosition.Y;
			}

			if ( !_bSilentUseCurrentValues )
			{
				if ( F.ShowDialog( this ) != DialogResult.OK )
					return false;
			}
			else
			{	// Silently query current value
				if ( !F.SampleCurrentValue() )
					return false;
			}

			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueBool );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueEvent );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueInt );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueFloat );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueFloat2 );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueFloat3 );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.DEFAULT, F.ValueFloat4 );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					{
						switch ( _KeyType )
						{
							case AnimationTrackPanel.KEY_TYPE.POSITION:
								animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.POSITION, F.ValuePosition );
								break;
							case AnimationTrackPanel.KEY_TYPE.ROTATION:
								animationTrackPanel.CreateRotationKeyAt( _SequencePosition.X, F.ValueRotationAngle, F.ValueRotationAxis );
								break;
							case AnimationTrackPanel.KEY_TYPE.SCALE:
								animationTrackPanel.CreateKeyAt( _SequencePosition.X, AnimationTrackPanel.KEY_TYPE.SCALE, F.ValueScale );
								break;
						}
					}
					break;
			}

			if ( _bSilentUseCurrentValues )
				F.Dispose();

			return true;
		}

		/// <summary>
		/// Creates an interpolated key at sequence position in the currently selected interval
		/// </summary>
		/// <param name="_SequencePosition">The time in SEQUENCE space where to interpolate keys</param>
		/// <param name="_KeyType">The optional key type for PRS tracks</param>
		public void	CreateInterpolatedKey( float _SequencePosition, AnimationTrackPanel.KEY_TYPE _KeyType )
		{
			if ( SelectedInterval == null )
				return;

			object	Value = null;
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					Value = _KeyType == AnimationTrackPanel.KEY_TYPE.DEFAULT;	// Here we use KEY TYPE to determine boolean state
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					Value = KeyEditorForm.LastUsedEventGUID;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					Value = (SelectedInterval[0] as Sequencor.AnimationTrackInt).ImmediateEval( _SequencePosition );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					Value = (SelectedInterval[0] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					Value = new SharpDX.Vector2(
						(SelectedInterval[0] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
						(SelectedInterval[1] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ) );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					Value = new SharpDX.Vector3(
						(SelectedInterval[0] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
						(SelectedInterval[1] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
						(SelectedInterval[2] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ) );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					Value = new SharpDX.Vector4(
						(SelectedInterval[0] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
						(SelectedInterval[1] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
						(SelectedInterval[2] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
						(SelectedInterval[3] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ) );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					switch ( _KeyType )
					{
						case AnimationTrackPanel.KEY_TYPE.POSITION:
							Value = new SharpDX.Vector3(
								(SelectedInterval[0] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
								(SelectedInterval[1] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
								(SelectedInterval[2] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ) );
							break;
						case AnimationTrackPanel.KEY_TYPE.ROTATION:
							{
								float			Angle;
								SharpDX.Vector3	Axis;
								(SelectedInterval[3] as Sequencor.AnimationTrackQuat).ImmediateEval( _SequencePosition, out Angle, out Axis );
								animationTrackPanel.CreateRotationKeyAt( _SequencePosition, Angle, Axis );
								return;
							}
						case AnimationTrackPanel.KEY_TYPE.SCALE:
							Value = new SharpDX.Vector3(
								(SelectedInterval[4] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
								(SelectedInterval[5] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ),
								(SelectedInterval[6] as Sequencor.AnimationTrackFloat).ImmediateEval( _SequencePosition ) );
							break;
					}
					break;
			}

			if ( Value != null )
				animationTrackPanel.CreateKeyAt( _SequencePosition, _KeyType, Value );
		}

		/// <summary>
		/// Spawns a form to edit the selected key
		/// </summary>
		/// <returns></returns>
		public bool	UserUpdateKey( Sequencor.AnimationTrack.Key _Key, bool _bSilentUseCurrentValues )
		{
			// Spawn the key editor form
			KeyEditorForm	F = new KeyEditorForm( m_Owner );

			F.Interval = _Key.ParentAnimationTrack.ParentInterval;
			F.Key = _Key;
			F.KeyType = GetKeyType( _Key );

			F.KeyTime = _Key.TrackTime;

			Sequencor.AnimationTrack.Key[]	Keys = GetBuddyKeys( _Key );

			// Feed values
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					F.ValueBool = (_Key as Sequencor.AnimationTrackBool.KeyBool).Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					F.ValueEvent = (_Key as Sequencor.AnimationTrackEvent.KeyEvent).EventGUID;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					F.ValueInt = (_Key as Sequencor.AnimationTrackInt.KeyInt).Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					F.ValueFloat = (_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					F.ValueFloat2 = new SharpDX.Vector2(
						(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
						(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					F.ValueFloat3 = new SharpDX.Vector3(
						(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
						(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
						(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					F.ValueFloat4 = new SharpDX.Vector4(
						(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
						(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
						(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
						(Keys[3] as Sequencor.AnimationTrackFloat.KeyFloat).Value );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					{
						switch ( GetKeyType( _Key ) )
						{
							case AnimationTrackPanel.KEY_TYPE.POSITION:
								F.ValuePosition = new SharpDX.Vector3(
									(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
									(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
									(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value );
								break;
							case AnimationTrackPanel.KEY_TYPE.ROTATION:
								F.ValueRotationAxis = (Keys[0] as Sequencor.AnimationTrackQuat.KeyQuat).Axis;
								F.ValueRotationAngle = (Keys[0] as Sequencor.AnimationTrackQuat.KeyQuat).Angle;
								break;
							case AnimationTrackPanel.KEY_TYPE.SCALE:
								F.ValueScale = new SharpDX.Vector3(
									(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
									(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value,
									(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value );
								break;
						}
					}
					break;
			}

			if ( !_bSilentUseCurrentValues )
			{
				if ( F.ShowDialog( this ) != DialogResult.OK )
					return false;
			}
			else
			{	// Silently query current value
				F.SampleCurrentValue();
			}

			// Read back modified values
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					(_Key as Sequencor.AnimationTrackBool.KeyBool).Value = F.ValueBool;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					(_Key as Sequencor.AnimationTrackEvent.KeyEvent).EventGUID = F.ValueEvent;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					(_Key as Sequencor.AnimationTrackInt.KeyInt).Value = F.ValueInt;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					(_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat2.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat2.Y;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat3.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat3.Y;
					(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat3.Z;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat4.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat4.Y;
					(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat4.Z;
					(Keys[3] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueFloat4.W;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					{
						switch ( GetKeyType( _Key ) )
						{
							case AnimationTrackPanel.KEY_TYPE.POSITION:
								(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValuePosition.X;
								(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValuePosition.Y;
								(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValuePosition.Z;
								break;
							case AnimationTrackPanel.KEY_TYPE.ROTATION:
								(Keys[0] as Sequencor.AnimationTrackQuat.KeyQuat).SetAngleAxis( F.ValueRotationAngle, F.ValueRotationAxis );
								break;
							case AnimationTrackPanel.KEY_TYPE.SCALE:
								(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueScale.X;
								(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueScale.Y;
								(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = F.ValueScale.Z;
								break;
						}
					}
					break;
			}

			// Update buddy keys' time
			foreach ( Sequencor.AnimationTrack.Key Key in GetBuddyKeys( _Key ) )
				Key.TrackTime = F.KeyTime;

			return false;
		}

		/// <summary>
		/// Retrieves the "buddy keys" of a given key
		/// It will return :
		///	_ the XYZ keys of a float3, position or scale
		/// _ the XYZW keys of a float4 or quaternion
		/// 
		/// This method is useful to update all the keys at once like when changing their individual values or time
		/// </summary>
		/// <param name="_Key"></param>
		/// <returns></returns>
		public Sequencor.AnimationTrack.Key[]	GetBuddyKeys( Sequencor.AnimationTrack.Key _Key )
		{
			if ( _Key == null )
				return new Sequencor.AnimationTrack.Key[0];

			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					return new Sequencor.AnimationTrack.Key[] { _Key };	// Only a single key for those types

				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					{
						int	KeyIndex = _Key.Index;
						Sequencor.ParameterTrack.Interval	Interval = _Key.ParentAnimationTrack.ParentInterval;
						return new Sequencor.AnimationTrack.Key[]
						{
							Interval[0][KeyIndex],
							Interval[1][KeyIndex],
						};
					}

				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					{
						int	KeyIndex = _Key.Index;
						Sequencor.ParameterTrack.Interval	Interval = _Key.ParentAnimationTrack.ParentInterval;
						return new Sequencor.AnimationTrack.Key[]
						{
							Interval[0][KeyIndex],
							Interval[1][KeyIndex],
							Interval[2][KeyIndex],
						};
					}

				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					{
						int	KeyIndex = _Key.Index;
						Sequencor.ParameterTrack.Interval	Interval = _Key.ParentAnimationTrack.ParentInterval;
						return new Sequencor.AnimationTrack.Key[]
						{
							Interval[0][KeyIndex],
							Interval[1][KeyIndex],
							Interval[2][KeyIndex],
							Interval[3][KeyIndex],
						};
					}

				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					{
						int	KeyIndex = _Key.Index;
						Sequencor.ParameterTrack.Interval	Interval = _Key.ParentAnimationTrack.ParentInterval;
						switch ( GetKeyType( _Key ) )
						{
							case AnimationTrackPanel.KEY_TYPE.POSITION:
								return new Sequencor.AnimationTrack.Key[]
								{
									Interval[0][KeyIndex],
									Interval[1][KeyIndex],
									Interval[2][KeyIndex],
								};

							case AnimationTrackPanel.KEY_TYPE.ROTATION:
								return new Sequencor.AnimationTrack.Key[]
								{
									Interval[3][KeyIndex],
								};

							case AnimationTrackPanel.KEY_TYPE.SCALE:
								return new Sequencor.AnimationTrack.Key[]
								{
									Interval[4][KeyIndex],
									Interval[5][KeyIndex],
									Interval[6][KeyIndex],
								};
						}
						break;
					}
			}

			return null;
		}

		/// <summary>
		/// Gets the type of a PRS key
		/// </summary>
		/// <param name="_Key"></param>
		/// <returns></returns>
		public AnimationTrackPanel.KEY_TYPE	GetKeyType( Sequencor.AnimationTrack.Key _Key )
		{
			if ( _Key == null )
				return AnimationTrackPanel.KEY_TYPE.DEFAULT;

			if ( m_Track.Type != Sequencor.ParameterTrack.PARAMETER_TYPE.PRS )
				return AnimationTrackPanel.KEY_TYPE.DEFAULT;

			int	IndexOfTrack = _Key.ParentAnimationTrack.ParentInterval.IndexOf( _Key.ParentAnimationTrack );
			if ( IndexOfTrack < 3 )
				return AnimationTrackPanel.KEY_TYPE.POSITION;
			else if ( IndexOfTrack < 4 )
				return AnimationTrackPanel.KEY_TYPE.ROTATION;

			return AnimationTrackPanel.KEY_TYPE.SCALE;
		}

		/// <summary>
		/// Gets the vertical range of the animation track editor
		/// </summary>
		/// <returns></returns>
		public float	GetAnimationVerticalRangeMin()
		{
			return animationTrackPanel.VerticalRangeMin;
		}

		/// <summary>
		/// Gets the vertical range of the animation track editor
		/// </summary>
		/// <returns></returns>
		public float	GetAnimationVerticalRangeMax()
		{
			return animationTrackPanel.VerticalRangeMax;
		}

		/// <summary>
		/// Sets the vertical ranges of the animation track editor
		/// </summary>
		/// <returns></returns>
		public void	SetAnimationVerticalRanges( float _RangeMin, float _RangeMax )
		{
			animationTrackPanel.SetVerticalRange( _RangeMin, _RangeMax );
		}

		/// <summary>
		/// Attempts to find an "anchor time" given a base time value for a key
		/// </summary>
		/// <param name="_AnchoringKey">The key to anchor and which is excluded from anchor search</param>
		/// <param name="_TimeToAnchor">The time to find an anchor for</param>
		/// <param name="_fAnchoredTime">The anchored time</param>
		/// <returns>True if a valid anchor was found</returns>
		protected bool	FindAnchor( Sequencor.AnimationTrack.Key _AnchoringKey, float _TimeToAnchor, out float _fAnchoredTime )
		{
			_fAnchoredTime = -1.0f;
			bool	bFoundAnchor = false;
			float	fBestAnchorDistance = float.MaxValue;
			float	fAnchorDistance;

			// Exclude our interval from interval anchoring search
			Sequencor.ParameterTrack.Interval	ExcludedInterval = _AnchoringKey.ParentAnimationTrack.ParentInterval;

			// Convert pixel tolerance into time tolerance
			float	fAnchorTimeTolerance = ANCHOR_PIXEL_TOLERANCE * (m_Owner.TimeLineControl.VisibleBoundMax - m_Owner.TimeLineControl.VisibleBoundMin) / m_Owner.TimeLineControl.Width;

			// Check cursor position
			fAnchorDistance = Math.Abs( m_Owner.TimeLineControl.CursorPosition - _TimeToAnchor );
			if ( fAnchorDistance < fAnchorTimeTolerance && fAnchorDistance < fBestAnchorDistance )
			{	// Found a new anchor
				fBestAnchorDistance = fAnchorDistance;
				_fAnchoredTime = m_Owner.TimeLineControl.CursorPosition;
				bFoundAnchor = true;
			}

			// Analyze each track for keys & intervals with a boundary close enough to anchor to it
			foreach ( Sequencor.ParameterTrack Track in m_Track.Owner.Tracks )
				foreach ( Sequencor.ParameterTrack.Interval Interval in Track.Intervals )
					if ( Interval != ExcludedInterval && Interval.TimeStart <= _AnchoringKey.TrackTime && Interval.TimeEnd >= _AnchoringKey.TrackTime )
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

						// Check that interval's keys
						foreach ( Sequencor.AnimationTrack AnimTrack in Interval.AnimationTracks )
							foreach ( Sequencor.AnimationTrack.Key Key in AnimTrack.Keys )
							{
								fAnchorDistance = Math.Abs( Key.TrackTime - _TimeToAnchor );
								if ( fAnchorDistance < fAnchorTimeTolerance && fAnchorDistance < fBestAnchorDistance )
								{	// Found a new anchor
									fBestAnchorDistance = fAnchorDistance;
									_fAnchoredTime = Key.TrackTime;
									bFoundAnchor = true;
								}
							}
					}

			return	bFoundAnchor;
		}

		/// <summary>
		/// Gets the current value of a key
		/// </summary>
		/// <param name="_Key">The key to get a value from</param>
		/// <returns>The vertical position in the animation panel, in SEQUENCE space, corresponding to the value of the key</returns>
		protected float	GetKeyValue( Sequencor.AnimationTrack.Key _Key )
		{
			switch ( _Key.ParentAnimationTrack.ParentInterval.ParentTrack.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					return (_Key as Sequencor.AnimationTrackInt.KeyInt).Value;

				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					return (_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value;

				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					switch ( m_ManipulatedKeyType )
					{
						case AnimationTrackPanel.KEY_TYPE.POSITION:
						case AnimationTrackPanel.KEY_TYPE.SCALE:
							return (_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					}
					break;
			}

			return 0.0f;
		}

		/// <summary>
		/// Sets the new value for a key
		/// </summary>
		/// <param name="_Key">The key to set a new value for</param>
		/// <param name="_SequencePositionY">The vertical position in the animation panel, in SEQUENCE space</param>
		protected void	SetKeyValue( Sequencor.AnimationTrack.Key _Key, float _SequencePositionY )
		{
			switch ( _Key.ParentAnimationTrack.ParentInterval.ParentTrack.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					(_Key as Sequencor.AnimationTrackBool.KeyBool).Value = _SequencePositionY > 0.5f;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					(_Key as Sequencor.AnimationTrackInt.KeyInt).Value = (int) _SequencePositionY;
					break;

					// We can manipulate any float/vector type freely
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					(_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value = _SequencePositionY;
					break;

					// We can only manipulate position & scale
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					switch ( m_ManipulatedKeyType )
					{
						case AnimationTrackPanel.KEY_TYPE.POSITION:
						case AnimationTrackPanel.KEY_TYPE.SCALE:
							(_Key as Sequencor.AnimationTrackFloat.KeyFloat).Value = _SequencePositionY;
							break;
					}
					break;
			}
		}

		/// <summary>
		/// Converts a sequence time into a client position
		/// </summary>
		/// <param name="_fSequenceTimeX">The sequence time</param>
		/// <param name="_fSequenceValue">The sequence value</param>
		/// <returns>The equivalent CLIENT SPACE position</returns>
		public PointF		SequenceTimeToClient( Sequencor.AnimationTrack.Key _Key )
		{
			return animationTrackPanel.SequenceTimeToClient( _Key.TrackTime, GetKeyValue( _Key ) );
		}

		#endregion

		#region EVENT HANDLERS

		protected void Owner_SequenceTimeChanged( object sender, EventArgs e )
		{
			animationTrackPanel.Invalidate();
			gradientTrackPanel.Invalidate();
		}

		protected void Track_CubicInterpolationChanged( object sender, EventArgs e )
		{
			checkBoxInterpolation.Checked = m_Track.CubicInterpolation;
			checkBoxShowTangents.Enabled = checkBoxInterpolation.Checked;
			animationTrackPanel.Invalidate();
			gradientTrackPanel.Invalidate();
		}

		protected void Track_ClipChanged( object sender, EventArgs e )
		{
			checkBoxClipMinInfinity.Checked = float.IsInfinity( m_Track.ClipMin );
			checkBoxClipMaxInfinity.Checked = float.IsInfinity( m_Track.ClipMax );
			animationTrackPanel.Invalidate();
			gradientTrackPanel.Invalidate();
		}

		private void buttonExit_Click( object sender, EventArgs e )
		{
			if ( Exit != null )
				Exit( this, EventArgs.Empty );
		}

		private void animationTrackPanel_RangeChanged( object sender, EventArgs e )
		{
			m_Owner.TimeLineControl.SetVisibleRange( animationTrackPanel.RangeMin, animationTrackPanel.RangeMax );
		}

		#region Context Menu

		private void contextMenuStrip_Opening( object sender, CancelEventArgs e )
		{
			m_ContextMenuPosition = animationTrackPanel.PointToClient( MousePosition );

			bool	bIntervalSelected = SelectedInterval != null;
			bool	bIsPRSTrack = m_Track.Type == Sequencor.ParameterTrack.PARAMETER_TYPE.PRS;
			bool	bKeyHovered = m_HoveredKey != null;
			bool	bIsHookedToValueProvider = m_Owner.CanQueryParameterValue;

			// Creation for non PRS tracks
			createKeyToolStripMenuItem.Enabled = bIntervalSelected;
			createKeyToolStripMenuItem.Visible = !bIsPRSTrack;
			createKeyAtCursorPositionToolStripMenuItem.Enabled = bIntervalSelected;
			createKeyAtCursorPositionToolStripMenuItem.Visible = !bIsPRSTrack;
			createKeyAtMousePositionToolStripMenuItem.Enabled = bIntervalSelected;
			createKeyAtMousePositionToolStripMenuItem.Visible = !bIsPRSTrack;
			createInterpolatedKeyToolStripMenuItem.Visible = bIntervalSelected && bIsPRSTrack;
			createKeyFromCurrentValueToolStripMenuItem.Visible = bIntervalSelected && bIsHookedToValueProvider;
			pRSToolStripMenuItem.Visible = false;//bIsHookedToValueProvider;
			pRSToolStripMenuItem1.Visible = false;//bIsHookedToValueProvider;

			// Same but for PRS tracks
			createKeyAtMousePositionToolStripMenuItem1.Visible = bIntervalSelected && bIsPRSTrack;
			createKeyAtCursorPositionToolStripMenuItem1.Visible = bIntervalSelected && bIsPRSTrack;

//			createKeyFromCurrentValueToolStripMenuItem.Visible = bIntervalSelected && bIsHookedToValueProvider;

			// Modification of existing keys
			updateKeyFromCurrentValueToolStripMenuItem.Visible = bIsHookedToValueProvider && bKeyHovered;
			updateKeyFromCurrentValueToolStripMenuItem1.Visible = false;//bIsHookedToValueProvider && bKeyHovered && bIsPRSTrack;
			alignKeyToCursorPositionToolStripMenuItem.Enabled = bKeyHovered;
			moveKeyAtTimeToolStripMenuItem.Enabled = bKeyHovered;
			copyKeyToolStripMenuItem.Enabled = bKeyHovered;
			pasteKeyToolStripMenuItem.Enabled = bIntervalSelected && m_Owner.CanPasteKeyToInterval( SelectedInterval );
			editKeyToolStripMenuItem.Enabled  = bKeyHovered;
			deleteKeyToolStripMenuItem.Enabled = bKeyHovered;
		}

		private void createKeyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( PointF.Empty, AnimationTrackPanel.KEY_TYPE.DEFAULT );
		}

		private void createKeyFromCurrentValueToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( m_Track.Type != Sequencor.ParameterTrack.PARAMETER_TYPE.PRS )
				UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.DEFAULT, true );
			else
			{
				if ( !UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.POSITION, true ) )
					return;	// Failed to create a basic position key, useless to go any further...
				UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.ROTATION, true );
				UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.SCALE, true );
			}
		}

		// PRS at mouse position
		private void createKeyAtMousePositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			PointF	SequencePosition = animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y );
			CreateInterpolatedKey( SequencePosition.X,
				// The following line is quite ugly but BOOL keys are the only ones that don't really interpolate and rather
				//  still use the mouse vertical position as value... As it's a particular case, I used the KEY_TYPE to determine
				//	the bool state to set : DEFAULT is true while others are FALSE
				SequencePosition.Y > 0.5f ? AnimationTrackPanel.KEY_TYPE.DEFAULT : AnimationTrackPanel.KEY_TYPE.POSITION );
		}

		private void positionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.POSITION );
		}

		private void rotationToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.ROTATION );
		}

		private void scaleToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.SCALE );
		}

		private void pRSToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( !UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.POSITION, true ) )
				return;	// Failed to create a basic position key, useless to go any further...
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.ROTATION, true );
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X, m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.SCALE, true );
		}

		// Interpolate PRS at mouse position
		private void positionToolStripMenuItem3_Click( object sender, EventArgs e )
		{
			CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X ), AnimationTrackPanel.KEY_TYPE.POSITION );
		}

		private void rotationToolStripMenuItem3_Click( object sender, EventArgs e )
		{
			CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X ), AnimationTrackPanel.KEY_TYPE.ROTATION );
		}

		private void scaleToolStripMenuItem3_Click( object sender, EventArgs e )
		{
			CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X ), AnimationTrackPanel.KEY_TYPE.SCALE );
		}

		private void pRSToolStripMenuItem3_Click( object sender, EventArgs e )
		{
			CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X ), AnimationTrackPanel.KEY_TYPE.POSITION );
			CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X ), AnimationTrackPanel.KEY_TYPE.ROTATION );
			CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( m_ContextMenuPosition.X ), AnimationTrackPanel.KEY_TYPE.SCALE );
		}

		// PRS at cursor position
		private void createKeyAtCursorPositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.DEFAULT );
		}

		private void positionToolStripMenuItem1_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.POSITION );
		}

		private void rotationToolStripMenuItem1_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.ROTATION );
		}

		private void scaleToolStripMenuItem1_Click( object sender, EventArgs e )
		{
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.SCALE );
		}

		private void pRSToolStripMenuItem1_Click( object sender, EventArgs e )
		{
			if ( !UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.POSITION, true ) )
				return;	// Failed to create a basic position key, useless to go any further...
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.ROTATION, true );
			UserCreateNewKey( animationTrackPanel.ClientToSequenceTime( animationTrackPanel.SequenceTimeToClient( m_Owner.TimeLineControl.CursorPosition ), m_ContextMenuPosition.Y ), AnimationTrackPanel.KEY_TYPE.SCALE, true );
		}

		// Update & edition
		private void updateKeyFromCurrentValueToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Sequencor.AnimationTrack.Key	K = m_HoveredKey != null ? m_HoveredKey : SelectedKey;
			UserUpdateKey( K, true );
		}

		private void positionToolStripMenuItem2_Click( object sender, EventArgs e )
		{
			SequencerControl.MessageBox( "TODO" );
		}

		private void rotationToolStripMenuItem2_Click( object sender, EventArgs e )
		{
			SequencerControl.MessageBox( "TODO" );
		}

		private void scaleToolStripMenuItem2_Click( object sender, EventArgs e )
		{
			SequencerControl.MessageBox( "TODO" );
		}

		private void pRSToolStripMenuItem2_Click( object sender, EventArgs e )
		{
			SequencerControl.MessageBox( "TODO" );
		}

		private void editKeyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			UserUpdateKey( m_HoveredKey, false );
		}

		private void alignKeyToCursorPositionToolStripMenuItem_Click( object sender, EventArgs e )
		{
			// Changing a key's time also implies changing its "buddy keys'"
			Sequencor.AnimationTrack.Key[]	Keys = GetBuddyKeys( m_HoveredKey );
			foreach ( Sequencor.AnimationTrack.Key Key in Keys )
				Key.TrackTime = m_Owner.TimeLineControl.CursorPosition;
		}

		private void moveKeyAtTimeToolStripMenuItem_Click( object sender, EventArgs e )
		{
			SetTimeForm	F = new SetTimeForm();
			F.Text = "Choose time for selected key...";
			F.Time = m_HoveredKey.TrackTime;
			if ( F.ShowDialog( this ) != DialogResult.OK )
				return;

			// Changing a key's time also implies changing its "buddy keys'"
			Sequencor.AnimationTrack.Key[]	Keys = GetBuddyKeys( m_HoveredKey );
			foreach ( Sequencor.AnimationTrack.Key Key in Keys )
				Key.TrackTime = F.Time;
		}

		internal void copyKeyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Sequencor.AnimationTrack.Key	SourceKey = m_HoveredKey != null ? m_HoveredKey : SelectedKey;
			if ( SourceKey != null )
				m_Owner.CopyToClipboard( GetBuddyKeys( SourceKey ) );
			else
				SequencerControl.MessageBox( "No source key (hovered or selected) to copy from !", MessageBoxButtons.OK, MessageBoxIcon.Warning );
		}

		internal void pasteKeyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			try
			{
				float	NewKeysPosition = animationTrackPanel.ClientToSequenceTime( animationTrackPanel.PointToClient( MousePosition ).X );
				m_Owner.PasteKeysToInterval( SelectedInterval, NewKeysPosition );
			}
			catch ( Exception _e )
			{
				SequencerControl.MessageBox( "An error occurred while pasting keys from clipboard :\r\n\r\n" + _e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		private void deleteKeyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if ( SequencerControl.MessageBox( "Are you sure you want to delete key " + m_HoveredKey + " ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2 ) != DialogResult.Yes )
				return;

			Sequencor.AnimationTrack.Key[]	Keys = GetBuddyKeys( m_HoveredKey );
			foreach ( Sequencor.AnimationTrack.Key Key in Keys )
				Key.ParentAnimationTrack.RemoveKey( Key );
		}

		#endregion

		#region GUI

		private void checkBoxInterpolation_CheckedChanged( object sender, EventArgs e )
		{
			checkBoxInterpolation.BackgroundImage = checkBoxInterpolation.Checked ? Properties.Resources.Variant___Curve : Properties.Resources.Variant___Constant;
			m_Track.CubicInterpolation = checkBoxInterpolation.Checked;
		}

		private void checkBoxShowTangents_CheckedChanged( object sender, EventArgs e )
		{
			animationTrackPanel.Invalidate();
		}

		private void checkBoxGradient_CheckedChanged( object sender, EventArgs e )
		{
			gradientTrackPanel.Visible = checkBoxGradient.Checked;
		}

		private void buttonZoomOut_Click( object sender, EventArgs e )
		{
//			animationTrackPanel.SetHorizontalRange( );

			float	VBoundMin = +float.MaxValue;
			float	VBoundMax = -float.MaxValue;

			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					VBoundMin = -0.5f;
					VBoundMax = 4.0f;
					break;

				case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
					// Let stupid bounds check handle this...
					break;
				
				default:
					foreach ( Sequencor.AnimationTrack T in SelectedInterval.AnimationTracks )
						if ( T is Sequencor.AnimationTrackFloat )
							foreach ( Sequencor.AnimationTrackFloat.KeyFloat K in T.Keys )
							{
								VBoundMin = Math.Min( VBoundMin, K.Value );
								VBoundMax = Math.Max( VBoundMax, K.Value );
							}
					break;
			}

			// Check bounds are not stoopid
			if ( VBoundMax - VBoundMin < 1.0f )
			{
				VBoundMin = -1.0f;
				VBoundMax = 10.0f;
			}

			// Expand a bit
			if ( VBoundMin < 0 )
				VBoundMin *= 1.05f;
			else
				VBoundMin /= 1.05f;
			VBoundMax *= 1.05f;

			animationTrackPanel.SetVerticalRange( VBoundMin, VBoundMax );
		}

		private void panelInfos_MouseDown( object sender, MouseEventArgs e )
		{
			OnMouseDown( e );
		}

		private void labelTrack_MouseDown( object sender, MouseEventArgs e )
		{
			OnMouseDown( e );
		}

		private void checkBoxEnabled_MouseDown( object sender, MouseEventArgs e )
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

		private void animationTrackPanel_MouseDown( object sender, MouseEventArgs _e )
		{
			OnMouseDown( _e );

			//////////////////////////////////////////////////////////////////////////
			// Perform interval copy if CopyMode is activated
			if ( m_HoveredKey != null && _e.Button == MouseButtons.Left && IsCopyMode )
			{
				Sequencor.AnimationTrack.Key[]	Sources = GetBuddyKeys( m_HoveredKey );
				Sequencor.AnimationTrack.Key[]	Copies = new Sequencor.AnimationTrack.Key[Sources.Length];
				for ( int SourceKeyIndex=0; SourceKeyIndex < Sources.Length; SourceKeyIndex++ )
				{
					Sequencor.AnimationTrack.Key	SourceKey = Sources[SourceKeyIndex];

					int	AnimTrackIndex = m_HoveredInterval.IndexOf( SourceKey.ParentAnimationTrack );
					Copies[SourceKeyIndex] = m_HoveredInterval[AnimTrackIndex].Clone( SourceKey );
					if ( SourceKey == m_HoveredKey )
						m_HoveredKey = Copies[SourceKeyIndex];	// New selection !
				}
			}

			//////////////////////////////////////////////////////////////////////////
			// Prepare manipulation data
			if ( _e.Button == System.Windows.Forms.MouseButtons.Left )
			{	// New selection
				SelectedInterval = m_HoveredInterval;

				// Check manipulation type
				if ( m_HoveredKeyTangentOut != null )
				{	// Manipulating tangent OUT...
					m_ManipulatedKey = m_HoveredKeyTangentOut;
					m_ManipulationType = MANIPULATION_TYPE.TANGENT_OUT;
					SelectedKey = m_HoveredKeyTangentOut;
				}
				else if ( m_HoveredKeyTangentIn != null )
				{	// Manipulating tangent IN...
					m_ManipulatedKey = m_HoveredKeyTangentIn;
					m_ManipulationType = MANIPULATION_TYPE.TANGENT_IN;
					SelectedKey = m_HoveredKeyTangentIn;
				}
				else
				{	// Manipulate main key...
					m_ManipulatedKey = m_HoveredKey;
					m_ManipulationType = MANIPULATION_TYPE.KEY;
					SelectedKey = m_HoveredKey;
				}
			}
			else
			{	// Clear manipulation
				m_ManipulatedKey = null;
			}

			m_MouseButtonsDown |= _e.Button;
			m_MouseDownPosition = _e.Location;
			m_ManipulatedBuddyKeys = GetBuddyKeys( m_ManipulatedKey );
			m_ManipulatedKeyType = GetKeyType( m_ManipulatedKey );

			// Compute original key (or tangent) position in CLIENT space
			m_MouseDownKeyClientPos = m_MouseDownPosition;
			if ( m_ManipulatedKey != null )
			{
				switch ( m_ManipulationType )
				{
					case MANIPULATION_TYPE.KEY:
						m_MouseDownKeyClientPos = SequenceTimeToClient( m_ManipulatedKey );
						break;
					case MANIPULATION_TYPE.TANGENT_IN:
						m_MouseDownKeyClientPos = animationTrackPanel.TangentInSequenceToClient( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat );
						m_MouseDownKeyClientPosOpposite = animationTrackPanel.TangentOutSequenceToClient( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat );
						break;
					case MANIPULATION_TYPE.TANGENT_OUT:
						m_MouseDownKeyClientPos = animationTrackPanel.TangentOutSequenceToClient( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat );
						m_MouseDownKeyClientPosOpposite = animationTrackPanel.TangentInSequenceToClient( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat );
						break;
				}
			}

			animationTrackPanel.Capture = true;

			if ( m_MouseButtonsDown == MouseButtons.Middle )
			{	// Scroll...
				MouseEventArgs	NewArgs = new MouseEventArgs( MouseButtons.Left, _e.Clicks, _e.X, _e.Y, _e.Delta );
				m_Owner.TimeLineControl.SimulateMouseDown( NewArgs );
			}
		}

		protected DateTime	m_LastMoveTime;
		private void animationTrackPanel_MouseMove( object sender, MouseEventArgs _e )
		{
			m_LastMoveTime = DateTime.Now;

			if ( m_MouseButtonsDown == MouseButtons.Middle )
			{	// Simulate horizontal panning...
				MouseEventArgs	NewArgs = new MouseEventArgs( MouseButtons.Left, _e.Clicks, _e.X, _e.Y, _e.Delta );
				m_Owner.TimeLineControl.SimulateMouseMove( NewArgs );
				return;
			}

			Sequencor.AnimationTrack.Key	OldHoveredKey = m_HoveredKey;

			m_HoveredInterval = animationTrackPanel.GetIntervalAt( _e.Location );
			m_HoveredKey = animationTrackPanel.GetKeyAt( _e.Location );
			if ( ShowTangents )
			{	// Check if hovering tangents
				m_HoveredKeyTangentIn = animationTrackPanel.GetKeyTangentInAt( _e.Location );
				m_HoveredKeyTangentOut = animationTrackPanel.GetKeyTangentOutAt( _e.Location );
			}

			// Reflect hovering state
			animationTrackPanel.HoveredKey = m_HoveredKey;
			animationTrackPanel.HoveredKeyTangentIn = m_HoveredKeyTangentIn;
			animationTrackPanel.HoveredKeyTangentOut = m_HoveredKeyTangentOut;

			if ( m_MouseButtonsDown != MouseButtons.Left )
				return;	// Just hovering...

			if ( m_ManipulatedKey == null || m_MouseButtonsDown != MouseButtons.Left )
				return;

			// Apply constraints
			float	MouseDx = _e.X - m_MouseDownPosition.X;
			float	MouseDy = _e.Y - m_MouseDownPosition.Y;
			PointF	NewKeyClientPos = new PointF( m_MouseDownKeyClientPos.X + MouseDx, m_MouseDownKeyClientPos.Y + MouseDy );
			if ( IsAxisConstraintMode )
			{
				switch ( m_ManipulationType )
				{
					case MANIPULATION_TYPE.KEY:
						if ( Math.Abs( MouseDx ) > Math.Abs( MouseDy ) )
						{	// Horizontal constraint
							NewKeyClientPos.Y = m_MouseDownKeyClientPos.Y;
						}
						else
						{	// Vertical constraint
							NewKeyClientPos.X = m_MouseDownKeyClientPos.X;
						}
						break;

					case MANIPULATION_TYPE.TANGENT_IN:
						{	// Align with tangent OUT
							PointF	KeyPosition = SequenceTimeToClient( m_ManipulatedKey );
							PointF	TangentOutPosition = animationTrackPanel.TangentOutSequenceToClient( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat );

							SharpDX.Vector2	Key2TangentIn = new SharpDX.Vector2( NewKeyClientPos.X - KeyPosition.X, NewKeyClientPos.Y - KeyPosition.Y );
							SharpDX.Vector2	Key2TangentOut = new SharpDX.Vector2( TangentOutPosition.X - KeyPosition.X, TangentOutPosition.Y - KeyPosition.Y );
							if ( Key2TangentOut.LengthSquared() > 1e-6f && Key2TangentIn.LengthSquared() > 1e-6f )
							{
								float	Key2TangentInLength = Key2TangentIn.Length();
								float	Key2TangentOutLength = Key2TangentOut.Length();
								Key2TangentOut /= Key2TangentOutLength;

								SharpDX.Vector2	Ortho = new SharpDX.Vector2( -Key2TangentOut.Y, Key2TangentOut.X );

								// Project tangent IN vector on tangent OUT
								Key2TangentIn -= SharpDX.Vector2.Dot( Key2TangentIn, Ortho ) * Ortho;

								// Check length constraint
								if ( Math.Abs( Key2TangentInLength - Key2TangentOutLength ) < 4.0f )
								{
									Key2TangentIn *= Key2TangentOutLength / Key2TangentInLength;
									// This will act as a signal
									m_HoveredKeyTangentIn = m_HoveredKeyTangentOut = m_ManipulatedKey;
								}

								// Update tangent IN
								NewKeyClientPos.X = KeyPosition.X + Key2TangentIn.X;
								NewKeyClientPos.Y = KeyPosition.Y + Key2TangentIn.Y;
							}
						}
						break;

					case MANIPULATION_TYPE.TANGENT_OUT:
						{	// Align with tangent IN
							PointF	KeyPosition = SequenceTimeToClient( m_ManipulatedKey );
							PointF	TangentInPosition = animationTrackPanel.TangentInSequenceToClient( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat );

							SharpDX.Vector2	Key2TangentOut = new SharpDX.Vector2( NewKeyClientPos.X - KeyPosition.X, NewKeyClientPos.Y - KeyPosition.Y );
							SharpDX.Vector2	Key2TangentIn = new SharpDX.Vector2( TangentInPosition.X - KeyPosition.X, TangentInPosition.Y - KeyPosition.Y );
							if ( Key2TangentOut.LengthSquared() > 1e-6f && Key2TangentIn.LengthSquared() > 1e-6f )
							{
								float	Key2TangentInLength = Key2TangentIn.Length();
								float	Key2TangentOutLength = Key2TangentOut.Length();
								Key2TangentIn /= Key2TangentInLength;

								SharpDX.Vector2	Ortho = new SharpDX.Vector2( -Key2TangentIn.Y, Key2TangentIn.X );

								// Project tangent IN vector on tangent OUT
								Key2TangentOut -= SharpDX.Vector2.Dot( Key2TangentOut, Ortho ) * Ortho;

								// Check length constraint
								if ( Math.Abs( Key2TangentInLength - Key2TangentOutLength ) < 4.0f )
								{
									Key2TangentOut *= Key2TangentInLength / Key2TangentOutLength;
									// This will act as a signal
									m_HoveredKeyTangentIn = m_HoveredKeyTangentOut = m_ManipulatedKey;
								}

								// Update tangent IN
								NewKeyClientPos.X = KeyPosition.X + Key2TangentOut.X;
								NewKeyClientPos.Y = KeyPosition.Y + Key2TangentOut.Y;
							}
						}
						break;
				}
			}

			// Perform actual manipulation
			switch ( m_ManipulationType )
			{
				case MANIPULATION_TYPE.KEY:
					{
						// Apply anchoring
						PointF	SequencePosition = animationTrackPanel.ClientToSequenceTime( NewKeyClientPos.X, NewKeyClientPos.Y );
						if ( IsAnchorMode )
						{
							float	fAnchoredTime = -1.0f;
							if ( FindAnchor( m_ManipulatedKey, SequencePosition.X, out fAnchoredTime ) )
								SequencePosition.X = fAnchoredTime;	// Move the key to the anchor
						}

						// Move key
						SetKeyValue( m_ManipulatedKey, SequencePosition.Y );

						// Move buddy keys horizontally
						foreach ( Sequencor.AnimationTrack.Key K in m_ManipulatedBuddyKeys )
							K.TrackTime = SequencePosition.X;
					}
					break;

				case MANIPULATION_TYPE.TANGENT_IN:
					animationTrackPanel.TangentInClientToSequence( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat, NewKeyClientPos );
					if ( !IsTangentBreakMode )
					{	// Report the manipulation symmetrically to the tangent OUT
						PointF			KeyPosition = SequenceTimeToClient( m_ManipulatedKey );
						SharpDX.Vector2	Key2TangentIn = new SharpDX.Vector2( NewKeyClientPos.X - KeyPosition.X, NewKeyClientPos.Y - KeyPosition.Y );
						SharpDX.Vector2	Key2TangentInOriginal = new SharpDX.Vector2( m_MouseDownKeyClientPos.X - KeyPosition.X, m_MouseDownKeyClientPos.Y - KeyPosition.Y );
						SharpDX.Vector2	Key2TangentOutOriginal = new SharpDX.Vector2( m_MouseDownKeyClientPosOpposite.X - KeyPosition.X, m_MouseDownKeyClientPosOpposite.Y - KeyPosition.Y );
						float			Key2TangentInLength = Key2TangentIn.Length();
						float			Key2TangentInOriginalLength = Key2TangentInOriginal.Length();

						// Change in angle
						float	AngleChange = 0.0f;
						if ( Key2TangentInOriginalLength > 0.0f && Key2TangentInLength > 0.0f )
						{
							Key2TangentIn /= Key2TangentInLength;
							Key2TangentInOriginal /= Key2TangentInOriginalLength;
							AngleChange = (float) (Math.Atan2( Key2TangentIn.Y, Key2TangentIn.X ) - Math.Atan2( Key2TangentInOriginal.Y, Key2TangentInOriginal.X ));
						}

						// Recompose new tangent OUT
						if ( Key2TangentOutOriginal.Length() > 1e-6f && Key2TangentInOriginalLength > 1e-6f )
							Key2TangentOutOriginal *= Key2TangentInLength / Key2TangentInOriginalLength;
						else
							Key2TangentOutOriginal = -Key2TangentIn;

						// Rotate
						SharpDX.Vector2	Key2TangentOut;
						Key2TangentOut.X = KeyPosition.X + Key2TangentOutOriginal.X * (float) Math.Cos( AngleChange ) - Key2TangentOutOriginal.Y * (float) Math.Sin( AngleChange );
						Key2TangentOut.Y = KeyPosition.Y + Key2TangentOutOriginal.X * (float) Math.Sin( AngleChange ) + Key2TangentOutOriginal.Y * (float) Math.Cos( AngleChange );;
					
						animationTrackPanel.TangentOutClientToSequence( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat, new PointF( Key2TangentOut.X, Key2TangentOut.Y ) );
					}
					break;

				case MANIPULATION_TYPE.TANGENT_OUT:
					animationTrackPanel.TangentOutClientToSequence( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat, NewKeyClientPos );
					if ( !IsTangentBreakMode )
					{	// Report the manipulation symmetrically to the tangent IN
						PointF			KeyPosition = SequenceTimeToClient( m_ManipulatedKey );
						SharpDX.Vector2	Key2TangentOut = new SharpDX.Vector2( NewKeyClientPos.X - KeyPosition.X, NewKeyClientPos.Y - KeyPosition.Y );
						SharpDX.Vector2	Key2TangentOutOriginal = new SharpDX.Vector2( m_MouseDownKeyClientPos.X - KeyPosition.X, m_MouseDownKeyClientPos.Y - KeyPosition.Y );
						SharpDX.Vector2	Key2TangentInOriginal = new SharpDX.Vector2( m_MouseDownKeyClientPosOpposite.X - KeyPosition.X, m_MouseDownKeyClientPosOpposite.Y - KeyPosition.Y );
						float			Key2TangentOutLength = Key2TangentOut.Length();
						float			Key2TangentOutOriginalLength = Key2TangentOutOriginal.Length();

						// Change in angle
						float	AngleChange = 0.0f;
						if ( Key2TangentOutOriginalLength > 0.0f && Key2TangentOutLength > 0.0f )
						{
							Key2TangentOut /= Key2TangentOutLength;
							Key2TangentOutOriginal /= Key2TangentOutOriginalLength;
							AngleChange = (float) (Math.Atan2( Key2TangentOut.Y, Key2TangentOut.X ) - Math.Atan2( Key2TangentOutOriginal.Y, Key2TangentOutOriginal.X ));
						}

						// Recompose new tangent OUT
						if ( Key2TangentInOriginal.Length() > 1e-6f && Key2TangentOutOriginalLength > 1e-6f )
							Key2TangentInOriginal *= Key2TangentOutLength / Key2TangentOutOriginalLength;
						else
							Key2TangentInOriginal = -Key2TangentOut;

						// Rotate
						SharpDX.Vector2	Key2TangentIn;
						Key2TangentIn.X = KeyPosition.X + Key2TangentInOriginal.X * (float) Math.Cos( AngleChange ) - Key2TangentInOriginal.Y * (float) Math.Sin( AngleChange );
						Key2TangentIn.Y = KeyPosition.Y + Key2TangentInOriginal.X * (float) Math.Sin( AngleChange ) + Key2TangentInOriginal.Y * (float) Math.Cos( AngleChange );;
					
						animationTrackPanel.TangentInClientToSequence( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat, new PointF( Key2TangentIn.X, Key2TangentIn.Y ) );
					}
					break;
			}

			// Re-evaluate interval (and keys) time => This should also notify of a parameter change to any attached application
			SelectedInterval.SetTime( m_Owner.TimeLineControl.CursorPosition );
		}

		private void animationTrackPanel_MouseUp( object sender, MouseEventArgs _e )
		{
			m_Owner.TimeLineControl.SimulateMouseUp( _e );

			m_MouseButtonsDown &= ~_e.Button;
			m_ManipulatedKey = null;
			m_ManipulatedBuddyKeys = null;
			this.Cursor = DefaultCursor;

			animationTrackPanel.Capture = m_MouseButtonsDown == MouseButtons.None;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			if ( (DateTime.Now - m_LastMoveTime).TotalSeconds < 0.5f )
				return;	// Too soon for tooltip...

			m_LastMoveTime = DateTime.Now;

			if ( m_HoveredKey != null )
				toolTip.SetToolTip( animationTrackPanel, m_HoveredKey.ToString() );
			else
				toolTip.SetToolTip( animationTrackPanel, null );
		}

		private void animationTrackPanel_MouseWheel( object sender, MouseEventArgs _e )
		{
			m_Owner.TimeLineControl.SimulateMouseWheel( _e );
		}

		private void animationTrackPanel_KeyDown( object sender, KeyEventArgs e )
		{
			switch ( e.KeyCode )
			{
				case Keys.Escape:
					if ( m_ManipulatedKey != null && m_MouseButtonsDown != MouseButtons.None )
					{	// Abort manipulation
						switch ( m_ManipulationType )
						{
							case MANIPULATION_TYPE.KEY:
								{
									PointF	OriginalKeyPosition = animationTrackPanel.ClientToSequenceTime( m_MouseDownKeyClientPos.X, m_MouseDownKeyClientPos.Y );
									SetKeyValue( m_ManipulatedKey, OriginalKeyPosition.Y );
									foreach ( Sequencor.AnimationTrack.Key K in m_ManipulatedBuddyKeys )
										K.TrackTime = OriginalKeyPosition.X;
								}
								break;

							case MANIPULATION_TYPE.TANGENT_IN:
								animationTrackPanel.TangentInClientToSequence( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat, m_MouseDownKeyClientPos );
								break;

							case MANIPULATION_TYPE.TANGENT_OUT:
								animationTrackPanel.TangentOutClientToSequence( m_ManipulatedKey as Sequencor.AnimationTrackFloat.KeyFloat, m_MouseDownKeyClientPos );
								break;
						}

						animationTrackPanel_MouseUp( sender, new MouseEventArgs( MouseButtons.Left, 1, 0, 0, 0 ) );
					}
					else if ( Exit != null )
						Exit( this, EventArgs.Empty );
					break;

				case Keys.Delete:
					if ( SelectedKey != null )
						deleteKeyToolStripMenuItem_Click( sender, e );
					break;
			}

		}

		private void animationTrackPanel_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			if ( SelectedKey != null )
				editKeyToolStripMenuItem_Click( animationTrackPanel, EventArgs.Empty );
			else
			{	// Create a new interpolated key
				CreateInterpolatedKey( animationTrackPanel.ClientToSequenceTime( e.X ), AnimationTrackPanel.KEY_TYPE.DEFAULT );
			}
		}

		private void gradientTrackPanel_MouseDown( object sender, MouseEventArgs _e )
		{
			if ( _e.Button != System.Windows.Forms.MouseButtons.Left )
				return;

			// New selection
			SelectedInterval = m_HoveredInterval;
			SelectedKey = m_HoveredKey;
		}

		private void gradientTrackPanel_MouseMove( object sender, MouseEventArgs _e )
		{
			Sequencor.AnimationTrack.Key	OldHoveredKey = m_HoveredKey;

			m_HoveredInterval = gradientTrackPanel.GetIntervalAt( _e.Location );
			m_HoveredKey = gradientTrackPanel.GetKeyAt( _e.Location );

			if ( m_HoveredKey != OldHoveredKey )
			{
				if ( m_HoveredKey != null )
					gradientTrackPanel.Cursor = Cursors.Hand;
				else
					gradientTrackPanel.Cursor = this.DefaultCursor;
			}
		}

		private void gradientTrackPanel_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			if ( SelectedKey == null )
				return;

			Sequencor.AnimationTrack.Key[]	Keys = GetBuddyKeys( SelectedKey );

			SharpDX.Vector4	Color = SharpDX.Vector4.One;
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					Color.X = Color.Y = Color.Z = (Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					Color.X = (Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					Color.Y = (Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					Color.Z = (Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					Color.X = (Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					Color.Y = (Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					Color.Z = (Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					Color.W = (Keys[3] as Sequencor.AnimationTrackFloat.KeyFloat).Value;
					break;
			}

			ColorPickerForm	F = new ColorPickerForm( Color );
			F.ColorChanged += new ColorPickerForm.ColorChangedEventHandler( ColorPicker_ColorChanged );
			if ( F.ShowDialog( this ) == DialogResult.OK )
				return;

			// Cancel
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.X;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.Y;
					(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.Z;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.Y;
					(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.Z;
					(Keys[3] as Sequencor.AnimationTrackFloat.KeyFloat).Value = Color.W;
					break;
			}
		}

		void ColorPicker_ColorChanged( ColorPickerForm _Sender )
		{
			Sequencor.AnimationTrack.Key[]	Keys = GetBuddyKeys( SelectedKey );

			// Readback result
			switch ( m_Track.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.X;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.Y;
					(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.Z;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					(Keys[0] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.X;
					(Keys[1] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.Y;
					(Keys[2] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.Z;
					(Keys[3] as Sequencor.AnimationTrackFloat.KeyFloat).Value = _Sender.ColorHDR.W;
					break;
			}

			// Re-evaluate interval (and keys) time => This should also notify of a parameter change to any attached application
			SelectedInterval.SetTime( m_Owner.TimeLineControl.CursorPosition );
		}

		#endregion

		#region Interval Range Change

		protected void SelectedInterval_ActualTimeStartChanged( object sender, EventArgs e )
		{
			floatTrackbarControlIntervalStart.Value = SelectedInterval.ActualTimeStart;
			floatTrackbarControlIntervalEnd.RangeMin = SelectedInterval.ActualTimeStart;
			floatTrackbarControlIntervalDuration.Value = SelectedInterval.ActualTimeEnd - SelectedInterval.ActualTimeStart;
		}

		protected void SelectedInterval_ActualTimeEndChanged( object sender, EventArgs e )
		{
			floatTrackbarControlIntervalEnd.Value = SelectedInterval.ActualTimeEnd;
			floatTrackbarControlIntervalStart.RangeMax = SelectedInterval.ActualTimeEnd;
			floatTrackbarControlIntervalDuration.Value = SelectedInterval.ActualTimeEnd - SelectedInterval.ActualTimeStart;
		}

		private void floatTrackbarControlIntervalStart_SliderDragStop( FloatTrackbarControl _Sender, float _fStartValue )
		{
			if ( m_bInternalChange )
				return;
			m_bInternalChange = true;

			SelectedInterval.ActualTimeStart = _Sender.Value;

			m_bInternalChange = false;
		}

		private void floatTrackbarControlIntervalEnd_SliderDragStop( FloatTrackbarControl _Sender, float _fStartValue )
		{
			if ( m_bInternalChange )
				return;
			m_bInternalChange = true;

			SelectedInterval.ActualTimeEnd = _Sender.Value;

			m_bInternalChange = false;
		}

		private void floatTrackbarControlIntervalDuration_SliderDragStop( FloatTrackbarControl _Sender, float _fStartValue )
		{
			if ( m_bInternalChange )
				return;
			m_bInternalChange = true;

			SelectedInterval.ActualTimeEnd = SelectedInterval.ActualTimeStart + _Sender.Value;

			m_bInternalChange = false;
		}

		private void checkBoxClipMinInfinity_CheckedChanged( object sender, EventArgs e )
		{
			floatTrackbarControlClipMin.Enabled = !checkBoxClipMinInfinity.Checked;
			m_Track.ClipMin = checkBoxClipMinInfinity.Checked ? float.NegativeInfinity : floatTrackbarControlClipMin.Value;
		}

		private void checkBoxClipMaxInfinity_CheckedChanged( object sender, EventArgs e )
		{
			floatTrackbarControlClipMax.Enabled = !checkBoxClipMaxInfinity.Checked;
			m_Track.ClipMax = checkBoxClipMaxInfinity.Checked ? float.PositiveInfinity : floatTrackbarControlClipMax.Value;
		}

		private void floatTrackbarControlClipMin_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_Track.ClipMin = floatTrackbarControlClipMin.Value;
		}

		private void floatTrackbarControlClipMax_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_Track.ClipMax = floatTrackbarControlClipMax.Value;
		}

		#endregion

		private void buttonSampleValue_Click( object sender, EventArgs e )
		{
			updateKeyFromCurrentValueToolStripMenuItem_Click( sender, e );
		}

		#endregion
	}
}
