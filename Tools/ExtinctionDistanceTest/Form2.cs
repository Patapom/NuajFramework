using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SharpDX;

namespace ExtinctionDistanceTest
{
	/// <summary>
	/// This little app lets you test the filmic tone mapping operator
	/// </summary>
	public partial class Form2 : Form
	{
		#region CONSTANTS

		protected const float	PLANET_RADIUS_KM = 6400.0f;
		protected const float	DEG2RAD = (float) Math.PI / 180.0f;
		protected const float	CAMERA_FOV = 80.0f * DEG2RAD;
		protected const float	CAMERA_ASPECT = 16.0f / 9.0f;

		#endregion

		#region METHODS

		public Form2()
		{
			InitializeComponent();
		}

		protected unsafe override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			panelOutput.m_HDRMaxIntensity = floatTrackbarControlHDRMaxIntensity.Value;
 			panelOutput.m_HDRWhitePoint = floatTrackbarControlHDRWhitePoint.Value;
 			panelOutput.m_LDRWhitePoint = floatTrackbarControlLDRWhitePoint.Value;
 			panelOutput.m_MaxX = floatTrackbarControlMaxX.Value;
			panelOutput.m_MaxY = floatTrackbarControlMaxY.Value;

			floatTrackbarControlA.Value = panelOutput.A;
			floatTrackbarControlB.Value = panelOutput.B;
			floatTrackbarControlC.Value = panelOutput.C;
			floatTrackbarControlD.Value = panelOutput.D;
			floatTrackbarControlE.Value = panelOutput.E;
			floatTrackbarControlF.Value = panelOutput.F;
	
			panelOutput.UpdateBitmap();
		}

		#endregion

		#region EVENT HANDLERS

		private void Form1_Load( object sender, EventArgs e )
		{

		}

		private void floatTrackbarControlCloudExtinction_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_HDRMaxIntensity = floatTrackbarControlHDRMaxIntensity.Value;
			panelOutput.UpdateBitmap();
		}

		private void floatTrackbarControlOpacityCoefficient_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_HDRWhitePoint = floatTrackbarControlHDRWhitePoint.Value;
			panelOutput.UpdateBitmap();
		}

		private void floatTrackbarControlStepSize_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_LDRWhitePoint = floatTrackbarControlLDRWhitePoint.Value;
			panelOutput.UpdateBitmap();
		}

		private void floatTrackbarControlMaxX_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_MaxX = floatTrackbarControlMaxX.Value;
			panelOutput.UpdateBitmap();
		}

		private void floatTrackbarControlMaxY_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_MaxY = floatTrackbarControlMaxY.Value;
			panelOutput.UpdateBitmap();
		}

		private void panelOutput_MouseDown( object sender, MouseEventArgs e )
		{
		}

		#endregion

		private void floatTrackbarControlA_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.A = floatTrackbarControlA.Value;
			panelOutput.B = floatTrackbarControlB.Value;
			panelOutput.C = floatTrackbarControlC.Value;
			panelOutput.D = floatTrackbarControlD.Value;
			panelOutput.E = floatTrackbarControlE.Value;
			panelOutput.F = floatTrackbarControlF.Value;
			panelOutput.UpdateBitmap();
		}
	}
}
