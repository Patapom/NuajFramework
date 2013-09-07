/******************************************************************/
/*****                                                        *****/
/*****     Project:           Adobe Color Picker Clone 1      *****/
/*****     Filename:          ColorPickerForm.cs              *****/
/*****     Original Author:   Danny Blanchard                 *****/
/*****                        - scrabcakes@gmail.com          *****/
/*****     Updates:	                                          *****/
/*****      3/28/2005 - Initial Version : Danny Blanchard     *****/
/*****                                                        *****/
/******************************************************************/
//
// And also by Patapom ^_^ :
//
// _ an upgrade to support Alpha
// _ Full HDR refactor
// _ Palette support with storage in the Registry
// _ various code factoring and optimization
// _ used nice sliders
//

using System;
using System.Reflection;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using SharpDX;

namespace SequencorEditor
{
	/// <summary>
	/// An improved, photoshop-like color picker with also Alpha and HDR colors support
	/// </summary>
	public partial class ColorPickerForm : System.Windows.Forms.Form
	{
		#region CONSTANTS

		protected const float	MIN_COMPONENT_VALUE	= 0.0f;//1e-3f;

		protected const int		PALETTE_ENTRIES = 3 * 12;	// 3 rows of 12 buttons

		#endregion

		#region NESTED TYPES

		[Flags()]
		public enum		VECTOR_DAMAGE_ON_EDITION	{	NONE = 0,
														XYZ_WILL_BE_MADE_POSITIVE = 1,
														ALPHA_WILL_BE_MADE_POSITIVE = 2,
														INVALID = -1
													};

		public enum		DONT_REFRESH				{	NONE,
														COLOR_SLIDER,
														ALPHA_SLIDER,
														COLOR_BOX
													};

		public enum	DRAW_STYLE
		{
			Hue,
			Saturation,
			Brightness,
			Red,
			Green,
			Blue
		}

		/// <summary>
		/// The delegate to use to subscribe to the ColorChanged event
		/// </summary>
		/// <param name="_Sender">The color picker whose color changed</param>
		public delegate void				ColorChangedEventHandler( ColorPickerForm _Sender );

		#endregion

		#region FIELDS

		protected AdobeColors.HSL		m_HSL = new AdobeColors.HSL( 1, 1, 1 );
		protected Vector4				m_RGB = (Vector4) AdobeColors.HSL_to_RGB( new AdobeColors.HSL( 1, 1, 1 ) );

		protected Vector4				m_PrimaryColor = Vector4.Zero;
		protected Vector4				m_SecondaryColor = Vector4.Zero;

		protected DONT_REFRESH			m_DontRefresh = DONT_REFRESH.NONE;

		#endregion

		#region PROPERTIES

		public Vector4	ColorHDR
		{
			get { return m_RGB; }
			set
			{
				if ( value == null )
					throw new Exception( "Invalid color value!" );

				// Setup RGB & HSL colors
				Vector3	TempValue = new Vector3( Math.Max( 0.0f, value.X ), Math.Max( 0.0f, value.Y ), Math.Max( 0.0f, value.Z ) );
				if ( TempValue.LengthSquared() < MIN_COMPONENT_VALUE * MIN_COMPONENT_VALUE )
					m_RGB = new Vector4( Math.Max( value.X, MIN_COMPONENT_VALUE ), Math.Max( value.Y, MIN_COMPONENT_VALUE ), Math.Max( value.Z, MIN_COMPONENT_VALUE ), value.W );
				else
					m_RGB = new Vector4( TempValue.X, TempValue.Y, TempValue.Z, value.W );

				m_HSL = AdobeColors.RGB_to_HSL( (Vector3) m_RGB );

				// Setup color & alpha boxes
				if ( m_DontRefresh != DONT_REFRESH.COLOR_BOX )
					colorBoxControl.HSL = m_HSL;
				if ( m_DontRefresh != DONT_REFRESH.COLOR_SLIDER )
					sliderControlHSL.HSL = m_HSL;
				m_DontRefresh = DONT_REFRESH.NONE;

				// Setup primary & secondary colors
				m_PrimaryColor = value;
				labelPrimaryColor.BackColor = AdobeColors.ConvertHDR2LDR( (Vector3) m_PrimaryColor );
				if ( m_SecondaryColor == null )
				{
					m_SecondaryColor = value;
					labelSecondaryColor.BackColor = AdobeColors.ConvertHDR2LDR( (Vector3) m_SecondaryColor );
				}

				// Update text boxes
				UpdateTextBoxes();

				// Notify of a color change
				if ( ColorChanged != null )
					ColorChanged( this );
			}
		}

		public Color		ColorLDR
		{
			get { return ConvertHDR2LDR( m_RGB ); }
			set { ColorHDR = AdobeColors.RGB_LDR_to_RGB_HDR( value ); }
		}

		protected DRAW_STYLE	DrawStyle
		{
			get
			{
				if ( buttonHue.Checked )
					return DRAW_STYLE.Hue;
				else if ( buttonSaturation.Checked )
					return DRAW_STYLE.Saturation;
				else if ( buttonBrightness.Checked )
					return DRAW_STYLE.Brightness;
				else if ( buttonRed.Checked )
					return DRAW_STYLE.Red;
				else if ( buttonGreen.Checked )
					return DRAW_STYLE.Green;
				else if ( buttonBlue.Checked )
					return DRAW_STYLE.Blue;
				else
					return DRAW_STYLE.Hue;
			}
			set
			{
				switch ( value )
				{
					case DRAW_STYLE.Hue :
						buttonHue.Checked = true;
						break;
					case DRAW_STYLE.Saturation :
						buttonSaturation.Checked = true;
						break;
					case DRAW_STYLE.Brightness :
						buttonBrightness.Checked = true;
						break;
					case DRAW_STYLE.Red :
						buttonRed.Checked = true;
						break;
					case DRAW_STYLE.Green :
						buttonGreen.Checked = true;
						break;
					case DRAW_STYLE.Blue :
						buttonBlue.Checked = true;
						break;
					default :
						buttonHue.Checked = true;
						break;
				}
			}
		}

		/// <summary>
		/// The event to susbcribe to to be notified the color changed
		/// </summary>
		public event ColorChangedEventHandler		ColorChanged;


		// [PATACODE]
		/// <summary>
		/// This accessor allows to setup the RGB color without erasing the alpha
		/// It should be used by all methods who assign the RGB color from other color spaces
		///  so alpha is preserved.
		/// Methods dealing with RGB color space should access m_RGB directly though...
		/// </summary>
		protected Vector3	RGB
		{
			get { return (Vector3) m_RGB; }
			set { ColorHDR = new Vector4( value.X, value.Y, value.Z, m_RGB.W ); }
		}
		// [PATACODE]

		#endregion

		#region METHODS

		public ColorPickerForm()
		{
			InitializeComponent();

			CustomInit();
		}

		public ColorPickerForm( Vector4 _ColorHDR )
		{
			InitializeComponent();

			CustomInit();

			ColorHDR = _ColorHDR;
		}

		public ColorPickerForm( Color _ColorLDR )
		{
			InitializeComponent();

			CustomInit();

			ColorLDR = _ColorLDR;
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Custom Control initialization
		/// </summary>
		protected void	CustomInit()
		{
			// Subscribe to the palette buttons' events
			FieldInfo[]	Fields = this.GetType().GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
			foreach ( FieldInfo Field in Fields )
			{
				if ( Field.FieldType != typeof(PaletteButton) )
					continue;
				if ( Field.Name.IndexOf( "radioButtonPalette" ) == -1 )
					continue;

				PaletteButton	Button = Field.GetValue( this ) as PaletteButton;
								Button.DoubleClick += new EventHandler( RadioButtonPalette_DoubleClick );
								Button.SelectedChanged += new EventHandler( RadioButtonPalette_SelectedChanged );
			}

			// Setup the palette buttons' back colors
			for ( int PaletteIndex=0; PaletteIndex < PALETTE_ENTRIES; PaletteIndex++ )
				UpdatePaletteButtonColor( PaletteIndex );
		}

		protected void WriteHexData( Vector4 _RGB )
		{
			Color	RGB = ConvertHDR2LDR( _RGB );
			textBoxHexa.Text = RGB.R.ToString( "X02" ) + RGB.G.ToString( "X02" ) + RGB.B.ToString( "X02" ) + RGB.A.ToString( "X02" );
			textBoxHexa.Update();
		}

		protected void	UpdateTextBoxes()
		{
			floatTrackbarControlHue.Value = (float) m_HSL.H * 360.0f;
			floatTrackbarControlSaturation.Value = (float) m_HSL.S;
			floatTrackbarControlLuminance.Value = (float) m_HSL.L;

			floatTrackbarControlRed.Value = m_RGB.X;
			floatTrackbarControlGreen.Value = m_RGB.Y;
			floatTrackbarControlBlue.Value = m_RGB.Z;
			floatTrackbarControlAlpha.Value = m_RGB.W;

			// Update RGB gradients
			Color	LDR = ColorLDR;
			floatTrackbarControlRed.ColorMin = Color.FromArgb( 0, LDR.G, LDR.B );
			floatTrackbarControlRed.ColorMax = Color.FromArgb( LDR.R, LDR.G, LDR.B );
			floatTrackbarControlGreen.ColorMin = Color.FromArgb( LDR.R, 0, LDR.B );
			floatTrackbarControlGreen.ColorMax = Color.FromArgb( LDR.R, LDR.G, LDR.B );
			floatTrackbarControlBlue.ColorMin = Color.FromArgb( LDR.R, LDR.G, 0 );
			floatTrackbarControlBlue.ColorMax = Color.FromArgb( LDR.R, LDR.G, LDR.B );
			floatTrackbarControlAlpha.ColorMin = Color.FromArgb( 128, 0, 0, 0 );
			floatTrackbarControlAlpha.ColorMax = Color.FromArgb( 128 + LDR.A / 2, LDR.A, LDR.A, LDR.A );

			// Update hexa LDR value
			WriteHexData( m_RGB );
		}

		#region Static Palette Access

		public static Vector4	GetPaletteColor( int _PaletteIndex )
		{
			Microsoft.Win32.RegistryKey	PaletteKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( SequencerForm.APPLICATION_REGISTRY_URL + @"\ColorPicker" );
			string	PaletteEntry = PaletteKey.GetValue( "Entry" + _PaletteIndex, null ) as string;

			if ( PaletteEntry != null && PaletteEntry != "" )
				try
				{
					string[]	Split = PaletteEntry.Split( ' ' );
					Vector4	Result;
					if ( float.TryParse( Split[0].Split( ':' )[1], out Result.X ) &&
						 float.TryParse( Split[1].Split( ':' )[1], out Result.Y ) &&
						 float.TryParse( Split[2].Split( ':' )[1], out Result.Z ) &&
						 float.TryParse( Split[3].Split( ':' )[1], out Result.W ) )
						return	Result;
				}
				catch ( System.Exception )
				{
				}

			// If we get here, we have no color for that palette entry so we create a default one...
			switch ( _PaletteIndex )
			{
				case	0:
					return new Vector4( 0.0f, 0.0f, 0.0f, 1.0f );
				case	1:
					return new Vector4( 1.0f, 1.0f, 1.0f, 1.0f );
				case	2:
					return new Vector4( 1.0f, 0.0f, 0.0f, 1.0f );
				case	3:
					return new Vector4( 1.0f, 1.0f, 0.0f, 1.0f );
				case	4:
					return new Vector4( 0.0f, 1.0f, 0.0f, 1.0f );
				case	5:
					return new Vector4( 0.0f, 1.0f, 1.0f, 1.0f );
				case	6:
					return new Vector4( 0.0f, 0.0f, 1.0f, 1.0f );
				case	7:
					return new Vector4( 1.0f, 0.0f, 1.0f, 1.0f );
				case	8:
					return new Vector4( 0.2f, 0.0f, 0.0f, 1.0f );
				case	9:
					return new Vector4( 0.4f, 0.0f, 0.0f, 1.0f );
				case	10:
					return new Vector4( 0.6f, 0.0f, 0.0f, 1.0f );
				case	11:
					return new Vector4( 0.8f, 0.0f, 0.0f, 1.0f );

				case	12:
					return new Vector4( 0.113f, 0.113f, 0.113f, 1.0f );
				case	13:									   
					return new Vector4( 0.225f, 0.225f, 0.225f, 1.0f );
				case	14:									   
					return new Vector4( 0.338f, 0.338f, 0.338f, 1.0f );
				case	15:									   
					return new Vector4( 0.450f, 0.450f, 0.450f, 1.0f );
				case	16:									   
					return new Vector4( 0.562f, 0.562f, 0.562f, 1.0f );
				case	17:									   
					return new Vector4( 0.675f, 0.675f, 0.675f, 1.0f );
				case	18:									   
					return new Vector4( 0.788f, 0.788f, 0.788f, 1.0f );
				case	19:									   
					return new Vector4( 0.900f, 0.900f, 0.900f, 1.0f );
				case	20:
					return new Vector4( 0.0f, 0.2f, 0.0f, 1.0f );
				case	21:
					return new Vector4( 0.0f, 0.4f, 0.0f, 1.0f );
				case	22:
					return new Vector4( 0.0f, 0.6f, 0.0f, 1.0f );
				case	23:
					return new Vector4( 0.0f, 0.8f, 0.0f, 1.0f );

				case	24:
					return new Vector4( 0.113f, 0.113f, 0.0f, 1.0f );
				case	25:					   				 
					return new Vector4( 0.225f, 0.225f, 0.0f, 1.0f );
				case	26:					   				 
					return new Vector4( 0.338f, 0.338f, 0.0f, 1.0f );
				case	27:					   				 
					return new Vector4( 0.450f, 0.450f, 0.0f, 1.0f );
				case	28:					   				 
					return new Vector4( 0.562f, 0.562f, 0.0f, 1.0f );
				case	29:					   				 
					return new Vector4( 0.675f, 0.675f, 0.0f, 1.0f );
				case	30:					   				 
					return new Vector4( 0.788f, 0.788f, 0.0f, 1.0f );
				case	31:					   				 
					return new Vector4( 0.900f, 0.900f, 0.0f, 1.0f );
				case	32:
					return new Vector4( 0.0f, 0.0f, 0.2f, 1.0f );
				case	33:
					return new Vector4( 0.0f, 0.0f, 0.4f, 1.0f );
				case	34:
					return new Vector4( 0.0f, 0.0f, 0.6f, 1.0f );
				case	35:
					return new Vector4( 0.0f, 0.0f, 0.8f, 1.0f );
			}

			return	Vector4.Zero;
		}

		public static void				SetPaletteColor( int _PaletteIndex, Vector4 _HDRColor )
		{
			Microsoft.Win32.RegistryKey	PaletteKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( SequencerForm.APPLICATION_REGISTRY_URL + @"\ColorPicker" );
										PaletteKey.SetValue( "Entry" + _PaletteIndex, _HDRColor.ToString() );
		}

		// Retrieves the index of the palette entry given a palette radio button
		//
		protected int	GetPaletteButtonIndex( PaletteButton _Button )
		{
			return	int.Parse( _Button.Name.Replace( "radioButtonPalette", "" ) );
		}

		// Retrieves the index of the palette entry given a palette radio button
		//
		protected PaletteButton	GetPaletteButton( int _PaletteIndex )
		{
			FieldInfo[]	Fields = this.GetType().GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
			foreach ( FieldInfo Field in Fields )
			{
				if ( Field.FieldType != typeof(PaletteButton) )
					continue;
				if ( Field.Name.IndexOf( "radioButtonPalette" ) == -1 )
					continue;

				PaletteButton	Button = Field.GetValue( this ) as PaletteButton;

				if ( GetPaletteButtonIndex( Button ) == _PaletteIndex )
					return	Button;
			}

			return	null;
		}

		// Retrieves the index of the selected palette button
		//
		protected int	GetSelectedPaletteButtonIndex()
		{
			FieldInfo[]	Fields = this.GetType().GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
			foreach ( FieldInfo Field in Fields )
			{
				if ( Field.FieldType != typeof(PaletteButton) )
					continue;
				if ( Field.Name.IndexOf( "radioButtonPalette" ) == -1 )
					continue;

				// Check the button's is checked
				PaletteButton	Button = Field.GetValue( this ) as PaletteButton;
				if ( Button.Selected )
					return	GetPaletteButtonIndex( Button );
			}

			return	-1;
		}

		// Retrieves the index of the selected palette button
		//
		protected void	SetSelectedPaletteButton( PaletteButton _Button )
		{
			FieldInfo[]	Fields = this.GetType().GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
			foreach ( FieldInfo Field in Fields )
			{
				if ( Field.FieldType != typeof(PaletteButton) )
					continue;
				if ( Field.Name.IndexOf( "radioButtonPalette" ) == -1 )
					continue;

				// Check the button's is checked
				PaletteButton	Button = Field.GetValue( this ) as PaletteButton;
								Button.Selected = Button == _Button;
			}
		}

		// Updates the palette button's back color based on the associated palette entry
		//
		protected void	UpdatePaletteButtonColor( int _PaletteIndex )
		{
			PaletteButton	Button = GetPaletteButton( _PaletteIndex );
							Button.Vector = GetPaletteColor( _PaletteIndex );
		}

		#endregion

		/// <summary>
		/// This helper method will tell if the color picker can safely edit the provided vector without damaging it
		/// Indeed, a vector must have certain characteristics to be used as a HDR color
		/// </summary>
		/// <param name="_Vector">The vector to test</param>
		/// <returns>A combination of damage flags. If none is set, then the vector can be safely edited without damage</returns>
		public static VECTOR_DAMAGE_ON_EDITION	GetVectorDamage( Vector4 _Vector )
		{
			if ( _Vector == null )
				return	VECTOR_DAMAGE_ON_EDITION.INVALID;

			VECTOR_DAMAGE_ON_EDITION	Result = VECTOR_DAMAGE_ON_EDITION.NONE;
			if ( _Vector.X < 0.0f || _Vector.Y < 0.0f || _Vector.Z < 0.0f )
				Result |= VECTOR_DAMAGE_ON_EDITION.XYZ_WILL_BE_MADE_POSITIVE;
			if ( _Vector.W < 0.0f )
				Result |= VECTOR_DAMAGE_ON_EDITION.ALPHA_WILL_BE_MADE_POSITIVE;

			return	Result;
		}

		/// <summary>
		/// Returns a RGB color from a 3D vector
		/// </summary>
		/// <param name="_Vector">The vector to get the RGB color from</param>
		/// <returns>The color from the vector</returns>
		/// <remarks>You should check if the vector can be cast into a color without damage using the above "GetVectorDamage()" method</remarks>
		public static Color	ConvertHDR2LDR( Vector3 _Vector )
		{
			return AdobeColors.ConvertHDR2LDR( new Vector3( Math.Max( MIN_COMPONENT_VALUE, _Vector.X ), Math.Max( MIN_COMPONENT_VALUE, _Vector.Y ), Math.Max( MIN_COMPONENT_VALUE, _Vector.Z ) ) );
		}

		/// <summary>
		/// Returns a RGBA color from a 4D vector
		/// </summary>
		/// <param name="_Vector">The vector to get the RGBA color from</param>
		/// <returns>The color from the vector</returns>
		/// <remarks>You should check if the vector can be cast into a color without damage using the above "GetVectorDamage()" method</remarks>
		public static Color	ConvertHDR2LDR( Vector4 _Vector )
		{
			return Color.FromArgb( Math.Max( 0, Math.Min( 255, (int) (255.0f * _Vector.W) ) ), AdobeColors.ConvertHDR2LDR( new Vector3( Math.Max( MIN_COMPONENT_VALUE, _Vector.X ), Math.Max( MIN_COMPONENT_VALUE, _Vector.Y ), Math.Max( MIN_COMPONENT_VALUE, _Vector.Z ) ) ) );
		}

		#endregion

		#region EVENT HANDLERS

		#region General Events

		protected void ColorPickerForm_Load(object sender, System.EventArgs e)
		{

		}


		protected void m_cmd_OK_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}


		protected void m_cmd_Cancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		#endregion

		protected void colorBoxControl_Scroll(object sender, System.EventArgs e)
		{
			m_DontRefresh = DONT_REFRESH.COLOR_BOX;
			RGB = AdobeColors.HSL_to_RGB( colorBoxControl.HSL );
		}

		protected void sliderControlHSL_Scroll(object sender, System.EventArgs e)
		{
			m_DontRefresh = DONT_REFRESH.COLOR_SLIDER;
			RGB = AdobeColors.HSL_to_RGB( sliderControlHSL.HSL );

			// Handle special cases where saturation is 0 (shades of gray) and it's not possible to devise a hue
			// Simply use the hue dictated by the color slider...
			if ( sliderControlHSL.HSL.S < 1e-4 )
			{
				AdobeColors.HSL	TempHSL = AdobeColors.RGB_to_HSL( (Vector3) m_RGB );
								TempHSL.H = sliderControlHSL.HSL.H;

				colorBoxControl.HSL = TempHSL;
			}
		}

		#region Color Boxes

		protected void labelPrimaryColor_Click(object sender, System.EventArgs e)
		{
			ColorHDR = m_PrimaryColor;
		}

		protected void labelSecondaryColor_Click(object sender, System.EventArgs e)
		{
			ColorHDR = m_SecondaryColor;
		}

		#endregion

		#region Radio Buttons

		protected void buttonHue_CheckedChanged(object sender, System.EventArgs e)
		{
			if ( buttonHue.Checked )
			{
				sliderControlHSL.DrawStyle = DRAW_STYLE.Hue;
				colorBoxControl.DrawStyle = DRAW_STYLE.Hue;
			}
		}

		protected void buttonSaturation_CheckedChanged(object sender, System.EventArgs e)
		{
			if ( buttonSaturation.Checked )
			{
				sliderControlHSL.DrawStyle = DRAW_STYLE.Saturation;
				colorBoxControl.DrawStyle = DRAW_STYLE.Saturation;
			}
		}


		protected void buttonBrightness_CheckedChanged(object sender, System.EventArgs e)
		{
			if ( buttonBrightness.Checked )
			{
				sliderControlHSL.DrawStyle = DRAW_STYLE.Brightness;
				colorBoxControl.DrawStyle = DRAW_STYLE.Brightness;
			}
		}


		protected void buttonRed_CheckedChanged(object sender, System.EventArgs e)
		{
			if ( buttonRed.Checked )
			{
				sliderControlHSL.DrawStyle = DRAW_STYLE.Red;
				colorBoxControl.DrawStyle = DRAW_STYLE.Red;
			}
		}

		protected void buttonGreen_CheckedChanged(object sender, System.EventArgs e)
		{
			if ( buttonGreen.Checked )
			{
				sliderControlHSL.DrawStyle = DRAW_STYLE.Green;
				colorBoxControl.DrawStyle = DRAW_STYLE.Green;
			}
		}


		protected void buttonBlue_CheckedChanged(object sender, System.EventArgs e)
		{
			if ( buttonBlue.Checked )
			{
				sliderControlHSL.DrawStyle = DRAW_STYLE.Blue;
				colorBoxControl.DrawStyle = DRAW_STYLE.Blue;
			}
		}

		#endregion

		#region Trackbars

		private void floatTrackbarControlHue_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_HSL.H = _Sender.Value / 360;

			RGB = AdobeColors.HSL_to_RGB( m_HSL );
		}

		private void floatTrackbarControlSaturation_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_HSL.S = _Sender.Value;

			RGB = AdobeColors.HSL_to_RGB( m_HSL );
		}

		private void floatTrackbarControlLuminance_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_HSL.L = _Sender.Value;

			RGB = AdobeColors.HSL_to_RGB( m_HSL );
		}

		private void floatTrackbarControlRed_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RGB.X = _Sender.Value;

			ColorHDR = m_RGB;
		}

		private void floatTrackbarControlGreen_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RGB.Y = _Sender.Value;

			ColorHDR = m_RGB;
		}

		private void floatTrackbarControlBlue_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RGB.Z = _Sender.Value;

			ColorHDR = m_RGB;
		}

		private void floatTrackbarControlAlpha_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue )
		{
			m_RGB.W = _Sender.Value;

			ColorHDR = m_RGB;
		}

		#endregion

		#region Hexa Text Box

		protected void	textBoxHexa_Validating(object sender, CancelEventArgs e)
		{
			string text = textBoxHexa.Text.ToUpper();
			bool has_illegal_chars = false;

			if ( textBoxHexa.Text.Length != 8 )
				has_illegal_chars = true;

			foreach ( char letter in text )
			{
				if ( !char.IsNumber(letter) )
				{
					if ( letter >= 'A' && letter <= 'F' )
						continue;
					has_illegal_chars = true;
					break;
				}
			}

			if ( has_illegal_chars )
			{
				MessageBox.Show( "Hex must be a hex value between 0x00000000 and 0xFFFFFFFF" );
				WriteHexData( m_RGB );
				return;
			}

			// Parse value
			string a_text, r_text, g_text, b_text;
			int a, r, g, b;

			r_text = textBoxHexa.Text.Substring(0, 2);
			g_text = textBoxHexa.Text.Substring(2, 2);
			b_text = textBoxHexa.Text.Substring(4, 2);
			a_text = textBoxHexa.Text.Substring(6, 2);

			a = int.Parse(a_text, System.Globalization.NumberStyles.HexNumber);
			r = int.Parse(r_text, System.Globalization.NumberStyles.HexNumber);
			g = int.Parse(g_text, System.Globalization.NumberStyles.HexNumber);
			b = int.Parse(b_text, System.Globalization.NumberStyles.HexNumber);

			ColorHDR = AdobeColors.RGB_LDR_to_RGB_HDR( r, g, b, a );
		}

		private void textBoxHexa_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.KeyCode != Keys.Return )
				return;

			e.Handled = true;
			textBoxHexa_Validating( sender, new CancelEventArgs() );
		}

		#endregion

		#region Palette Handling

		protected void	RadioButtonPalette_SelectedChanged( object sender, EventArgs e )
		{
			if ( (sender as PaletteButton).Selected )
				SetSelectedPaletteButton( sender as PaletteButton );
		}

		protected void	RadioButtonPalette_DoubleClick( object sender, EventArgs e )
		{
			int	PaletteIndex = GetPaletteButtonIndex( sender as PaletteButton );
			ColorHDR = GetPaletteColor( PaletteIndex );
		}

		protected void	buttonAssignColor_Click( object sender, EventArgs e )
		{
			int	PaletteIndex = GetSelectedPaletteButtonIndex();
			SetPaletteColor( PaletteIndex, ColorHDR );
			UpdatePaletteButtonColor( PaletteIndex );
		}

		#endregion

		#endregion
	}
}
