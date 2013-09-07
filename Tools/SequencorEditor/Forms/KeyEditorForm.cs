using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SequencorLib;
using SharpDX;

namespace SequencorEditor
{
	public partial class KeyEditorForm : Form
	{
		#region CONSTANTS

		protected readonly static string	REGISTRY_KEY = SequencerForm.APPLICATION_REGISTRY_URL + @"\KeyEditor";

		#endregion

		#region FIELDS

		protected SequencerControl			m_SequencerControl = null;
		protected Sequencor.ParameterTrack.Interval	m_Interval = null;
		protected Sequencor.AnimationTrack.Key		m_Key = null;

		protected AnimationTrackPanel.KEY_TYPE	m_KeyType = AnimationTrackPanel.KEY_TYPE.DEFAULT;

		protected int						m_LastValidGUID = 0;

		protected static int				ms_EventGUID = -1;

		#endregion

		#region PROPERTIES

		public Sequencor.ParameterTrack.Interval	Interval
		{
			get { return m_Interval; }
			set
			{
				m_Interval = value;
				floatTrackbarControlTime.RangeMin = m_Interval.ActualTimeStart;
				floatTrackbarControlTime.VisibleRangeMin = m_Interval.ActualTimeStart;
				floatTrackbarControlTime.RangeMax = m_Interval.ActualTimeEnd;
				floatTrackbarControlTime.VisibleRangeMax = m_Interval.ActualTimeEnd;
				buttonColorPicker.Visible = false;

				switch ( m_Interval.ParentTrack.Type )
				{
					case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
						labelKeyType.Text = "BOOL";
						panelBool.Visible = true;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.EVENT:
						labelKeyType.Text = "EVENT";
						panelEvent.Visible = true;
						buttonSampleValue.Visible = false;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
						labelKeyType.Text = "INTEGER";
						panelInteger.Visible = true;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
						labelKeyType.Text = "FLOAT";
						panelFloat.Visible = true;
						labelY.Visible = false;
						floatTrackbarControlFloatY.Visible = false;
						labelZ.Visible = false;
						floatTrackbarControlFloatZ.Visible = false;
						labelW.Visible = false;
						floatTrackbarControlFloatW.Visible = false;
						buttonColorPicker.Visible = true;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
						labelKeyType.Text = "FLOAT2";
						panelFloat.Visible = true;
						labelY.Visible = true;
						floatTrackbarControlFloatY.Visible = true;
						labelZ.Visible = false;
						floatTrackbarControlFloatZ.Visible = false;
						labelW.Visible = false;
						floatTrackbarControlFloatW.Visible = false;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
						labelKeyType.Text = "FLOAT3";
						panelFloat.Visible = true;
						labelY.Visible = true;
						floatTrackbarControlFloatY.Visible = true;
						labelZ.Visible = true;
						floatTrackbarControlFloatZ.Visible = true;
						labelW.Visible = false;
						floatTrackbarControlFloatW.Visible = false;
						buttonColorPicker.Visible = true;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
						labelKeyType.Text = "FLOAT4";
						panelFloat.Visible = true;
						labelY.Visible = true;
						floatTrackbarControlFloatY.Visible = true;
						labelZ.Visible = true;
						floatTrackbarControlFloatZ.Visible = true;
						labelW.Visible = true;
						floatTrackbarControlFloatW.Visible = true;
						buttonColorPicker.Visible = true;
						break;

					case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
						panelFloat.Visible = true;
						break;
				}
			}
		}

		public Sequencor.AnimationTrack.Key			Key
		{
			get { return m_Key; }
			set
			{
				if ( value == m_Key )
					return;

				m_Key = value;

				labelLabelKeyName.Visible = m_Key != null;
				labelKeyName.Visible = m_Key != null;
				labelKeyName.Text = m_Key != null ? m_Key.ToString() : "";
			}
		}

		public AnimationTrackPanel.KEY_TYPE			KeyType
		{
			get { return m_KeyType; }
			set
			{
				m_KeyType = value;

				switch ( m_KeyType )
				{
					case AnimationTrackPanel.KEY_TYPE.POSITION:
					case AnimationTrackPanel.KEY_TYPE.SCALE:
						labelY.Visible = true;
						floatTrackbarControlFloatY.Visible = true;
						labelZ.Visible = true;
						floatTrackbarControlFloatZ.Visible = true;
						labelW.Visible = false;
						labelW.Text = "W";
						floatTrackbarControlFloatW.Visible = false;
						break;

					case AnimationTrackPanel.KEY_TYPE.ROTATION:
						labelY.Visible = true;
						floatTrackbarControlFloatY.Visible = true;
						labelZ.Visible = true;
						floatTrackbarControlFloatZ.Visible = true;
						labelW.Visible = true;
						labelW.Text = "A";
						floatTrackbarControlFloatW.Visible = true;
						break;
				}

				switch ( m_KeyType )
				{
					case AnimationTrackPanel.KEY_TYPE.POSITION:
						labelKeyType.Text = "POSITION";
						break;
					case AnimationTrackPanel.KEY_TYPE.ROTATION:
						labelKeyType.Text = "ROTATION";
						break;
					case AnimationTrackPanel.KEY_TYPE.SCALE:
						labelKeyType.Text = "SCALE";
						break;
				}
			}
		}

		public float	KeyTime
		{
			get { return floatTrackbarControlTime.Value; }
			set { floatTrackbarControlTime.Value = value; }
		}

		public bool		ValueBool
		{
			get { return checkBoxValueBool.Checked; }
			set {checkBoxValueBool.Checked = value; }
		}

		public int		ValueEvent
		{
			get { return int.Parse( textBoxEventGUID.Text ); }
			set { textBoxEventGUID.Text = value.ToString(); m_LastValidGUID = value; }
		}

		public int		ValueInt
		{
			get { return integerTrackbarControl.Value; }
			set { integerTrackbarControl.Value = value; }
		}

		public float	ValueFloat
		{
			get { return floatTrackbarControlFloatX.Value; }
			set { floatTrackbarControlFloatX.Value = value; }
		}

		public Vector2	ValueFloat2
		{
			get { return new Vector2( floatTrackbarControlFloatX.Value, floatTrackbarControlFloatY.Value ); }
			set { floatTrackbarControlFloatX.Value = value.X; floatTrackbarControlFloatY.Value = value.Y; }
		}

		public Vector3	ValueFloat3
		{
			get { return new Vector3( floatTrackbarControlFloatX.Value, floatTrackbarControlFloatY.Value, floatTrackbarControlFloatZ.Value ); }
			set { floatTrackbarControlFloatX.Value = value.X; floatTrackbarControlFloatY.Value = value.Y; floatTrackbarControlFloatZ.Value = value.Z; }
		}

		public Vector4	ValueFloat4
		{
			get { return new Vector4( floatTrackbarControlFloatX.Value, floatTrackbarControlFloatY.Value, floatTrackbarControlFloatZ.Value, floatTrackbarControlFloatW.Value ); }
			set { floatTrackbarControlFloatX.Value = value.X; floatTrackbarControlFloatY.Value = value.Y; floatTrackbarControlFloatZ.Value = value.Z; floatTrackbarControlFloatW.Value = value.W; }
		}

		public Vector3	ValuePosition
		{
			get { return ValueFloat3; }
			set { ValueFloat3 = value; }
		}

		public Vector3	ValueScale
		{
			get { return ValueFloat3; }
			set { ValueFloat3 = value; }
		}

		public Vector3	ValueRotationAxis
		{
			get { return ValueFloat3; }
			set { ValueFloat3 = value; }
		}

		public float	ValueRotationAngle
		{
			get { return floatTrackbarControlFloatW.Value * (float) Math.PI / 180.0f; }
			set { floatTrackbarControlFloatW.Value = value * 180.0f / (float) Math.PI; }
		}

		/// <summary>
		/// Gets the last selected event GUID
		/// </summary>
		public static int	LastUsedEventGUID
		{
			get
			{
				if ( ms_EventGUID == -1 )
				{
					Microsoft.Win32.RegistryKey	KeyEditorKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( REGISTRY_KEY );
					int.TryParse( KeyEditorKey.GetValue( "EventGUID", "0" ) as string, out ms_EventGUID );
				}
				
				return ms_EventGUID;
			}
		}

		#endregion

		#region METHODS

		public KeyEditorForm( SequencerControl _SequencerControl )
		{
			m_SequencerControl = _SequencerControl;

			InitializeComponent();

			panelBool.Dock = DockStyle.Fill;
			panelBool.Visible = false;
			panelEvent.Dock = DockStyle.Fill;
			panelEvent.Visible = false;
			panelFloat.Dock = DockStyle.Fill;
			panelFloat.Visible = false;
			panelInteger.Dock = DockStyle.Fill;
			panelInteger.Visible = false;

			if ( _SequencerControl.CanQueryParameterValue )
				buttonSampleValue.Visible = true;

			// Reload last settings from registry
			textBoxEventGUID.Text = LastUsedEventGUID.ToString();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			// Save settings to registry
			Microsoft.Win32.RegistryKey	KeyEditorKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( REGISTRY_KEY );
			KeyEditorKey.SetValue( "EventGUID", textBoxEventGUID.Text );

			// Also store it as static member for query
			ms_EventGUID = ValueEvent;

			base.OnClosing( e );
		}

		/// <summary>
		/// Samples current parameter value
		/// </summary>
		/// <returns>True if the value was successfully sampled</returns>
		public bool	SampleCurrentValue()
		{
			object Value = m_SequencerControl.QueryCurrentParameterValue( m_Interval.ParentTrack );
			if ( Value == null )
			{	// Invalid
				SequencerControl.MessageBox( this, "The attached application failed to return a valid sample value for parameter \"" + m_Interval.ParentTrack.Name + "\" !", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				return	false;
			}

			switch ( m_Interval.ParentTrack.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.BOOL:
					ValueBool = (bool) Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.INT:
					ValueInt = (int) Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					ValueFloat = (float) Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT2:
					ValueFloat2 = (Vector2) Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					ValueFloat3 = (Vector3) Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					ValueFloat4 = (Vector4) Value;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.PRS:
					{
						Vector3		Position, Scale;
						Quaternion	Rotation;
						((Matrix) Value).Decompose( out Scale, out Rotation, out Position );

						switch ( m_KeyType )
						{
							case AnimationTrackPanel.KEY_TYPE.POSITION:
								ValuePosition = Position;
								break;
							case AnimationTrackPanel.KEY_TYPE.ROTATION:
								ValueRotationAngle = Rotation.Angle;
								ValueRotationAxis = Rotation.Axis;
								break;
							case AnimationTrackPanel.KEY_TYPE.SCALE:
								ValueScale = Scale;
								break;
						}
					}
					break;
			}

			return true;
		}

		#endregion

		#region EVENT HANDLERS

		private void textBoxEventGUID_Validating( object sender, CancelEventArgs e )
		{
			int GUID = 0;
			if ( int.TryParse( textBoxEventGUID.Text, out GUID ) )
			{
				m_LastValidGUID = GUID;
				return;
			}

			textBoxEventGUID.Text = m_LastValidGUID.ToString();
			e.Cancel = true;
		}

		private void buttonEditTime_Click( object sender, EventArgs e )
		{
			SetTimeForm	F = new SetTimeForm();
			F.Time = KeyTime;
			F.Text = "Setup the new key time...";
			if ( F.ShowDialog( this ) != DialogResult.OK )
				return;

			// Update time
			KeyTime = F.Time;
		}

		private void buttonSampleValue_Click( object sender, EventArgs e )
		{
			SampleCurrentValue();
		}

		private void buttonColorPicker_Click( object sender, EventArgs e )
		{
			ColorPickerForm	F = new ColorPickerForm();
			F.ColorChanged += new ColorPickerForm.ColorChangedEventHandler( PickerForm_ColorChanged );

			Vector4	InitialColor = Vector4.Zero;
			switch ( m_Interval.ParentTrack.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					InitialColor = new Vector4( 0.0f, 0.0f, 0.0f, ValueFloat );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					InitialColor = new Vector4( ValueFloat3, 1.0f );
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					InitialColor = ValueFloat4;
					break;
			}
			F.ColorHDR = InitialColor;

			if ( F.ShowDialog( this ) != DialogResult.OK )
				return;
		}

		void PickerForm_ColorChanged( ColorPickerForm _Sender )
		{
			switch ( m_Interval.ParentTrack.Type )
			{
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT:
					ValueFloat = _Sender.ColorHDR.W;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT3:
					ValueFloat3 = (Vector3) _Sender.ColorHDR;
					break;
				case Sequencor.ParameterTrack.PARAMETER_TYPE.FLOAT4:
					ValueFloat4 = _Sender.ColorHDR;
					break;
			}
		}

		#endregion
	}
}
