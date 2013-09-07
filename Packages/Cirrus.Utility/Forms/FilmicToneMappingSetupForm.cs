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

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This little app lets you test the filmic tone mapping operator
	/// </summary>
	public partial class FilmicToneMappingSetupForm : Form
	{
		#region FIELDS

		protected RenderTechniquePostProcessToneMappingFilmic	m_ToneMappingTechnique = null;
		protected bool	m_bInternalChange = false;
		#endregion

		#region PROPERTIES

		public RenderTechniquePostProcessToneMappingFilmic	ToneMappingTechnique
		{
			get { return m_ToneMappingTechnique; }
			set
			{
				if ( value == null )
					return;

				m_ToneMappingTechnique = value;

				// Readback values
				m_bInternalChange = true;
				floatTrackbarControlA.Value = m_ToneMappingTechnique.A;
				floatTrackbarControlB.Value = m_ToneMappingTechnique.B;
				floatTrackbarControlC.Value = m_ToneMappingTechnique.C;
				floatTrackbarControlD.Value = m_ToneMappingTechnique.D;
				floatTrackbarControlE.Value = m_ToneMappingTechnique.E;
				floatTrackbarControlF.Value = m_ToneMappingTechnique.F;

				floatTrackbarControlExposureBias.Value = m_ToneMappingTechnique.ExposureBias;
				floatTrackbarControlHDRWhitePoint.Value = m_ToneMappingTechnique.HDRWhitePointLuminance;
				floatTrackbarControlLDRWhitePoint.Value = m_ToneMappingTechnique.LDRWhitePointLuminance;
				m_bInternalChange = false;

				// Notify change for update...
				floatTrackbarControl_ValueChanged( null, 0.0f );
			}
		}

		#endregion

		#region METHODS

		public FilmicToneMappingSetupForm()
		{
			InitializeComponent();
		}

		protected unsafe override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			floatTrackbarControl_ValueChanged( null, 0.0f );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			e.Cancel = true;
			Visible = false;		// Hide instead !
			base.OnClosing( e );
		}

		#endregion

		#region EVENT HANDLERS

		private void floatTrackbarControl_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			if ( m_bInternalChange )
				return;

			// Reflect changes in the graph
			panelOutput.m_ExposureBias = floatTrackbarControlExposureBias.Value;
			panelOutput.m_HDRMaxIntensity = floatTrackbarControlHDRMaxIntensity.Value;
			panelOutput.m_HDRWhitePoint = floatTrackbarControlHDRWhitePoint.Value;
			panelOutput.m_LDRWhitePoint = floatTrackbarControlLDRWhitePoint.Value;
			panelOutput.m_MaxX = floatTrackbarControlMaxX.Value;
			panelOutput.m_MaxY = floatTrackbarControlMaxY.Value;

			panelOutput.A = floatTrackbarControlA.Value;
			panelOutput.B = floatTrackbarControlB.Value;
			panelOutput.C = floatTrackbarControlC.Value;
			panelOutput.D = floatTrackbarControlD.Value;
			panelOutput.E = floatTrackbarControlE.Value;
			panelOutput.F = floatTrackbarControlF.Value;
			panelOutput.UpdateBitmap();

			if ( m_ToneMappingTechnique == null )
				return;

			// Also reflect changes in the technique
			m_ToneMappingTechnique.ExposureBias = floatTrackbarControlExposureBias.Value;
			m_ToneMappingTechnique.HDRWhitePointLuminance = floatTrackbarControlHDRWhitePoint.Value;
			m_ToneMappingTechnique.LDRWhitePointLuminance = floatTrackbarControlLDRWhitePoint.Value;

			m_ToneMappingTechnique.A = floatTrackbarControlA.Value;
			m_ToneMappingTechnique.B = floatTrackbarControlB.Value;
			m_ToneMappingTechnique.C = floatTrackbarControlC.Value;
			m_ToneMappingTechnique.D = floatTrackbarControlD.Value;
			m_ToneMappingTechnique.E = floatTrackbarControlE.Value;
			m_ToneMappingTechnique.F = floatTrackbarControlF.Value;
		}

		#endregion
	}
}
