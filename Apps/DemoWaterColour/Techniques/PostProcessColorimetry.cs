using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Colorimetry & Tone Mapping
	/// </example>
	public class PostProcessColorimetry : RenderTechniqueBase
	{
		#region CONSTANTS

		#endregion

		#region NESTED TYPES

		public class		ColorEditor : System.Drawing.Design.UITypeEditor
		{
			public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle( System.ComponentModel.ITypeDescriptorContext context )
			{
				return System.Drawing.Design.UITypeEditorEditStyle.Modal;
			}

			protected PostProcessColorimetry					m_Instance = null;
			protected System.ComponentModel.PropertyDescriptor	m_PropDesc = null;
			public override object EditValue( System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value )
			{
				m_Instance = context.Instance as PostProcessColorimetry;
				m_PropDesc = context.PropertyDescriptor;

				SequencorEditor.ColorPickerForm	F = new SequencorEditor.ColorPickerForm( (Vector4) value );
				F.ColorChanged += new SequencorEditor.ColorPickerForm.ColorChangedEventHandler( Picker_ColorChanged );
				if ( F.ShowDialog() != System.Windows.Forms.DialogResult.OK )
				{
					m_PropDesc.SetValue( m_Instance, value );
					return value;
				}
				
				return m_PropDesc.GetValue( m_Instance );
			}

			void Picker_ColorChanged( SequencorEditor.ColorPickerForm _Sender )
			{
				m_PropDesc.SetValue( m_Instance, _Sender.ColorHDR );
			}
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected bool						m_bShowMire = false;
		protected bool						m_bEnableColorimetry = false;
		protected float						m_MaxLuminance = 1.0f;						// The maximum encoded luminance in the input image
		protected float						m_ToneSharpness = 0.5f;						// The sharpness factor used to delimit the shadows/midtones/highlights boundaries (0 is smooth, 1 is ultra-crisp)

		protected Vector3					m_Shift_Highlights = 0.5f * Vector3.One;	// RGB shift for highlights
		protected float						m_Saturation_Highlights = 0.0f;				// Shift saturation
		protected float						m_Contrast_Highlights = 0.5f;				// Highlights contrast
			 
		protected Vector3					m_Shift_Midtones = 0.5f * Vector3.One;		// RGB shift for middle tones
		protected float						m_Saturation_Midtones = 0.0f;				// Shift saturation
		protected float						m_Contrast_Midtones = 0.5f;					// Middle tones contrast

		protected Vector3					m_Shift_Shadows = 0.5f * Vector3.One;		// RGB shift for shadows
		protected float						m_Saturation_Shadows = 0.0f;				// Shift saturation
		protected float						m_Contrast_Shadows = 0.5f;					// Shadows contrast

		protected Texture2D<PF_RGBA8>		m_Mire = null;

		#endregion

		#region PROPERTIES

		public bool							ShowMire			{ get { return m_bShowMire; } set { m_bShowMire = value; } }
		public bool							EnableColorimetry	{ get { return m_bEnableColorimetry; } set { m_bEnableColorimetry = value; } }
		[System.ComponentModel.Description( "Defines the maximum luminance encoded in the scene" )]
		public float						MaxLuminance		{ get { return m_MaxLuminance; } set { m_MaxLuminance = value; } }
		[System.ComponentModel.Description( "Defines the sharpness of the separation between shadows, midtones and highlights ranges" )]
		public float						ToneSharpness		{ get { return m_ToneSharpness; } set { m_ToneSharpness = value; } }

		// High Tones
		[System.ComponentModel.Description( "Defines the RGB shift for high tones" )]
		[System.ComponentModel.Editor( typeof(ColorEditor), typeof(System.Drawing.Design.UITypeEditor) )]
		public Vector4						Shift_Highlights	{ get { return new Vector4( m_Shift_Highlights, m_Contrast_Highlights ); } set { SetHighlightsShiftSatContrast( value ); } }
		[System.ComponentModel.Description( "Defines the saturation for high tones in [0,1]" )]
		public float						Saturation_Highlights	{ get { return m_Saturation_Highlights; } set { m_Saturation_Highlights = value; } }
		[System.ComponentModel.Description( "Defines the contrast for high tones in [0,1]" )]
		public float						Contrast_Highlights	{ get { return m_Contrast_Highlights; } set { m_Contrast_Highlights = value; } }

		// Middle Tones
		[System.ComponentModel.Description( "Defines the RGB shift for middle tones" )]
		[System.ComponentModel.Editor( typeof(ColorEditor), typeof(System.Drawing.Design.UITypeEditor) )]
		public Vector4						Shift_Midtones		{ get { return new Vector4( m_Shift_Midtones, m_Contrast_Midtones ); } set { SetMidtonesShiftSatContrast( value ); } }
		[System.ComponentModel.Description( "Defines the saturation for middle tones in [0,1]" )]
		public float						Saturation_Midtones	{ get { return m_Saturation_Midtones; } set { m_Saturation_Midtones = value; } }
		[System.ComponentModel.Description( "Defines the contrast for middle tones in [0,1]" )]
		public float						Contrast_Midtones	{ get { return m_Contrast_Midtones; } set { m_Contrast_Midtones = value; } }

		// Shadow Tones
		[System.ComponentModel.Description( "Defines the RGB shift for shadow tones" )]
		[System.ComponentModel.Editor( typeof(ColorEditor), typeof(System.Drawing.Design.UITypeEditor) )]
		public Vector4						Shift_Shadows		{ get { return new Vector4( m_Shift_Shadows, m_Contrast_Shadows ); } set { SetShadowsShiftSatContrast( value ); } }
		[System.ComponentModel.Description( "Defines the saturation for shadow tones in [0,1]" )]
		public float						Saturation_Shadows	{ get { return m_Saturation_Shadows; } set { m_Saturation_Shadows = value; } }
		[System.ComponentModel.Description( "Defines the contrast for shadow tones in [0,1]" )]
		public float						Contrast_Shadows	{ get { return m_Contrast_Shadows; } set { m_Contrast_Shadows = value; } }

		#endregion

		#region METHODS

		public	PostProcessColorimetry( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
m_bEnabled = true;
//m_bEnableColorimetry = true;

			// Create our main materials
 			m_MaterialPostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "Post-Process Colorimetry Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/PostProcessColorimetry.fx" ) );

			m_Mire = m_Renderer.TextureLoader.LoadTexture<PF_RGBA8>( "Mire", new System.IO.FileInfo( "Media/mire-kodak.jpg" ) );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			//////////////////////////////////////////////////////////////////////////
			// 1] Apply colorimetry
			using ( m_MaterialPostProcess.UseLock() )
			{
				m_Device.SetDefaultRenderTarget();
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				CurrentMaterial.GetVariableByName( "Enabled" ).AsScalar.Set( m_bEnableColorimetry );
				CurrentMaterial.GetVariableByName( "MaxLuminance" ).AsScalar.Set( m_MaxLuminance );
				CurrentMaterial.GetVariableByName( "ToneSharpness" ).AsScalar.Set( (float) Math.Tan( m_ToneSharpness * 0.5 * Math.PI ) );

				CurrentMaterial.GetVariableByName( "Shift_Shadows" ).AsVector.Set( m_Shift_Shadows );
				CurrentMaterial.GetVariableByName( "SatContrast_Shadows" ).AsVector.Set( new Vector2( m_Saturation_Shadows, (float) Math.Tan( 0.5 * Math.PI * m_Contrast_Shadows ) ) );
				CurrentMaterial.GetVariableByName( "Shift_Midtones" ).AsVector.Set( m_Shift_Midtones );
				CurrentMaterial.GetVariableByName( "SatContrast_Midtones" ).AsVector.Set( new Vector2( m_Saturation_Midtones, (float) Math.Tan( 0.5 * Math.PI * m_Contrast_Midtones ) ) );
				CurrentMaterial.GetVariableByName( "Shift_Highlights" ).AsVector.Set( m_Shift_Highlights );
				CurrentMaterial.GetVariableByName( "SatContrast_Highlights" ).AsVector.Set( new Vector2( m_Saturation_Highlights, (float) Math.Tan( 0.5 * Math.PI * m_Contrast_Highlights ) ) );

				if ( m_bShowMire )
					CurrentMaterial.GetVariableBySemantic( "GBUFFER_TEX0" ).AsResource.SetResource( m_Mire );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();

			}
		}

		public void		SetShadowsShiftSatContrast( Vector4 _RGB )
		{
			m_Shift_Shadows = (Vector3) _RGB;

			float	Max = Math.Max( _RGB.X, Math.Max( _RGB.Y, _RGB.Z ) );
			float	Min = Math.Min( _RGB.X, Math.Min( _RGB.Y, _RGB.Z ) );

			m_Saturation_Shadows = Max > 1e-6 ? 1.0f - Min / Max : 0.0f;
			m_Contrast_Shadows = _RGB.W;
		}

		public void		SetMidtonesShiftSatContrast( Vector4 _RGB )
		{
			m_Shift_Midtones = (Vector3) _RGB;

			float	Max = Math.Max( _RGB.X, Math.Max( _RGB.Y, _RGB.Z ) );
			float	Min = Math.Min( _RGB.X, Math.Min( _RGB.Y, _RGB.Z ) );

			m_Saturation_Midtones = Max > 1e-6 ? 1.0f - Min / Max : 0.0f;
			m_Contrast_Midtones = _RGB.W;
		}

		public void		SetHighlightsShiftSatContrast( Vector4 _RGB )
		{
			m_Shift_Highlights = (Vector3) _RGB;

			float	Max = Math.Max( _RGB.X, Math.Max( _RGB.Y, _RGB.Z ) );
			float	Min = Math.Min( _RGB.X, Math.Min( _RGB.Y, _RGB.Z ) );

			m_Saturation_Highlights = Max > 1e-6 ? 1.0f - Min / Max : 0.0f;
			m_Contrast_Highlights = _RGB.W;
		}

		/// <summary> 
		/// Converts a colour from HSL to RGB 
		/// </summary> 
		/// <remarks>Adapted from the algoritm in Foley and Van-Dam</remarks> 
		/// <param name="hsl">The HSL value</param> 
		/// <returns>A Vector3 structure containing the equivalent RGB values</returns> 
		public static Vector3	HSL2RGB( Vector3 hsl )
		{
			float	Max, Mid, Min;
			double q;

			Max = hsl.X;
			Min = ((1.0f - hsl.Y) * hsl.X);
			q   = Max - Min;

			if ( hsl.X >= 0 && hsl.X <= 1.0/6.0 )
			{
				Mid = (float) (((hsl.X - 0) * q) * 6 + Min);
				return new Vector3( Max,Mid,Min );
			}
			else if ( hsl.X <= 1.0/3.0 )
			{
				Mid = (float) (-((hsl.X - 1.0/6.0) * q) * 6 + Max);
				return new Vector3( Mid,Max,Min);
			}
			else if ( hsl.X <= 0.5 )
			{
				Mid = (float) (((hsl.X - 1.0/3.0) * q) * 6 + Min);
				return new Vector3( Min,Max,Mid);
			}
			else if ( hsl.X <= 2.0/3.0 )
			{
				Mid = (float) (-((hsl.X - 0.5) * q) * 6 + Max);
				return new Vector3( Min,Mid,Max);
			}
			else if ( hsl.X <= 5.0/6.0 )
			{
				Mid = (float) (((hsl.X - 2.0/3.0) * q) * 6 + Min);
				return new Vector3( Mid,Min,Max);
			}
			else if ( hsl.X <= 1.0 )
			{
				Mid = (float) (-((hsl.X - 5.0/6.0) * q) * 6 + Max);
				return new Vector3( Max,Min,Mid);
			}
			else	return new Vector3( 0,0,0);
		} 

		/// <summary> 
		/// Converts RGB to HSL 
		/// </summary> 
		/// <param name="c">The RGB Vector3 to convert</param> 
		/// <returns>An HSL value</returns> 
		public static Vector3 RGB2HSL( Vector3 c )
		{ 
			Vector3 hsl = Vector3.Zero;
          
			float Max, Min, Diff, Sum;

			//	Of our RGB values, assign the highest value to Max, and the Smallest to Min
			if ( c.X > c.Y )	{ Max = c.X; Min = c.Y; }
			else				{ Max = c.Y; Min = c.X; }
			if ( c.Z > Max )	  Max = c.Z;
			else if ( c.Z < Min ) Min = c.Z;

			Diff = Max - Min;
			Sum = Max + Min;

			//	Luminance - a.k.a. Brightness - Adobe photoshop uses the logic that the
			//	site VBspeed regards (regarded) as too primitive = superior decides the 
			//	level of brightness.
			hsl.Z = Max;
//			hsl.Z = 0.5f * (Min + Max);

			//	Saturation
			if ( Max == 0 ) hsl.Y = 0;		//	Protecting from the impossible operation of division by zero.
			else hsl.Y = Diff / Max;		//	The logic of Adobe Photoshops is this simple.

			//	Hue		R is situated at the angel of 360 eller noll degrees; 
			//			G vid 120 degrees
			//			B vid 240 degrees
			float q;
			if ( Diff == 0 ) q = 0; // Protecting from the impossible operation of division by zero.
			else q = 60.0f/Diff;
			
			if ( Max == c.X )
			{
				if ( c.Y < c.Z )	hsl.X = (360.0f + q * (c.Y - c.Z))/360.0f;
				else				hsl.X = (q * (c.Y - c.Z))/360.0f;
			}
			else if ( Max == c.Y )	hsl.X = (120.0f + q * (c.Z - c.X))/360.0f;
			else if ( Max == c.Z )	hsl.X = (240.0f + q * (c.X - c.Y))/360.0f;
			else					hsl.X = 0.0f;

			return hsl; 
		} 

// 		/// <summary>
// 		/// Converts RGB to HSL color
// 		/// </summary>
// 		/// <param name="_RGB"></param>
// 		/// <returns>HSL value with H in [0,6]</returns>
// 		public static Vector3	RGB2HSL( Vector3 _RGB )
// 		{
// 			Vector2  MinMaxRGB = new Vector2( Math.Min( _RGB.X, Math.Min( _RGB.Y, _RGB.Z ) ), Math.Max( _RGB.X, Math.Max( _RGB.Y, _RGB.Z ) ) );
// 			Vector3  HSL = Vector3.Zero;
// 
// 			// 1] Luminance is 0.5 * (min + max)
// 			HSL.Z = 0.5f * (MinMaxRGB.X + MinMaxRGB.Y);
// 
// 			if ( MinMaxRGB.X != MinMaxRGB.Y )
// 			{	// H and S can be defined...
// 
// 				// 2] Saturation
// 				if ( HSL.Z < 0.5 )
// 					HSL.Y = (MinMaxRGB.Y - MinMaxRGB.X) / (MinMaxRGB.Y + MinMaxRGB.X);
// 				else
// 					HSL.Y = (MinMaxRGB.Y - MinMaxRGB.X) / (2.0f - MinMaxRGB.Y - MinMaxRGB.X);
// 
// 				// 3] Hue
// 				float OneOverMaxMinusMin = 1.0f / (MinMaxRGB.Y - MinMaxRGB.X);
// 				if ( MinMaxRGB.Y == _RGB.X )
// 					HSL.X = (_RGB.Y-_RGB.Z) * OneOverMaxMinusMin;
// 				else if ( MinMaxRGB.Y == _RGB.Y )
// 					HSL.X = 2.0f + (_RGB.Z-_RGB.X) * OneOverMaxMinusMin;
// 				else
// 					HSL.X = 4.0f + (_RGB.X-_RGB.Y) * OneOverMaxMinusMin;
// 			}
// 
// 			return HSL;
// 		}
// 
// 		/// <summary>
// 		/// Converts HSL to RGB color
// 		/// </summary>
// 		/// <param name="_HSL"></param>
// 		/// <returns></returns>
// 		public static Vector3	HSL2RGB( Vector3 _HSL )
// 		{
// 			_HSL.Z = Math.Max( 0.0f, Math.Min( 1.0f, _HSL.Z ) );
// 			float Chroma = (1.0f - Math.Abs( 2.0f * _HSL.Z - 1.0f )) * _HSL.Y;
// 			float X = Chroma * (1.0f - Math.Abs( (_HSL.X % 2.0f) - 1.0f ));
// 
// 			Vector3  tempRGB;
// 			if ( _HSL.X < 1.0 )
// 				tempRGB = new Vector3( Chroma, X, 0 );
// 			else if ( _HSL.X < 2.0 )
// 				tempRGB = new Vector3( X, Chroma, 0 );
// 			else if ( _HSL.X < 3.0 )
// 				tempRGB = new Vector3( 0, Chroma, X );
// 			else if ( _HSL.X < 4.0 )
// 				tempRGB = new Vector3( 0, X, Chroma );
// 			else if ( _HSL.X < 5.0 )
// 				tempRGB = new Vector3( X, 0, Chroma );
// 			else
// 				tempRGB = new Vector3( Chroma, 0, X );
// 
// 			return tempRGB + (_HSL.Z - 0.5f * Chroma) * Vector3.One;
// 		}

		#endregion
	}
}
