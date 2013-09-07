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
	/// This little app is used to visually debug the computation of a shadow map's bounding quadrilateral
	/// 
	/// The camera's frustum is defined by a pyramid whose apex is the camera's position.
	/// This pyramid is projected onto a plane (the shadow plane) and can thus take several configurations.
	/// Two of them are distinguished :
	///  1) The camera's position is projected inside the pyramid's base (viewing up case), in which case the base is used as the quadrilateral to map
	///  2) The camera's position is projected outside the pyramid's base (general case), in which case we need to find a triangle whose apex is the
	///		projected camera position and whose 2 other vertices need to be computed so the triangle encompasses the 4 vertices of the pyramid's base.
	///		(the triangle is thus a degenerated quadrilateral)
	///	
	/// Later, we show how to retrieve the (u,v) position parametrizing the quadrilateral from any point P projected onto the shadow plane.
	/// </summary>
	public partial class Form1 : Form
	{
		#region CONSTANTS

		protected const float	PLANET_RADIUS_KM = 6400.0f;
		protected const float	DEG2RAD = (float) Math.PI / 180.0f;
		protected const float	CAMERA_FOV = 80.0f * DEG2RAD;
		protected const float	CAMERA_ASPECT = 16.0f / 9.0f;

		#endregion

		#region METHODS

		public Form1()
		{
			InitializeComponent();
		}

		protected unsafe override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			panelOutput.m_CloudExtinction = floatTrackbarControlCloudExtinction.Value;
			panelOutput.m_OpacityFactor = floatTrackbarControlOpacityCoefficient.Value;
			panelOutput.m_StepSize = 0.001f * floatTrackbarControlStepSize.Value;
			panelOutput.UpdateBitmap();
		}

		#endregion

		#region EVENT HANDLERS

		private void Form1_Load( object sender, EventArgs e )
		{

		}

		private void floatTrackbarControlCloudExtinction_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_CloudExtinction = _Sender.Value;
			panelOutput.UpdateBitmap();
		}

		private void floatTrackbarControlOpacityCoefficient_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_OpacityFactor = _Sender.Value;
			panelOutput.UpdateBitmap();
		}

		private void floatTrackbarControlStepSize_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			panelOutput.m_StepSize = 0.001f * _Sender.Value;
			panelOutput.UpdateBitmap();
		}

		private void panelOutput_MouseDown( object sender, MouseEventArgs e )
		{
		}

		#endregion
	}
}
