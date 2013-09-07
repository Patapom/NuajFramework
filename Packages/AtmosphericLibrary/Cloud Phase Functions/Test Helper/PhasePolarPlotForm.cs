using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace Atmospheric.Helpers
{
	/// <summary>
	/// Displays a phase function in polar or cartesian coordinates
	/// This is a simple helper to verify your phase function
	/// </summary>
	public class PhasePolarPlotForm : System.Windows.Forms.Form
	{
		private PanelOutput panelOutput;
		private System.Windows.Forms.CheckBox checkBoxPolar;
		private System.ComponentModel.IContainer components;


		protected PhaseFunction		m_Phase	= null;
		protected float				m_PhaseMin = +float.MaxValue;
		protected float				m_PhaseMax = -float.MaxValue;

		public PhasePolarPlotForm( PhaseFunction _Phase )
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_Phase = _Phase;

			// Retrieve Min/Max phase
			for ( int Index=0; Index < m_Phase.FactorsCount; Index++ )
			{
				m_PhaseMin = Math.Min( m_PhaseMin, m_Phase.PhaseFactors[Index] );
				m_PhaseMax = Math.Max( m_PhaseMax, m_Phase.PhaseFactors[Index] );
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.panelOutput = new Atmospheric.Helpers.PanelOutput( this.components );
			this.checkBoxPolar = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// panelOutput
			// 
			this.panelOutput.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panelOutput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelOutput.Location = new System.Drawing.Point( 8, 8 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 512, 512 );
			this.panelOutput.TabIndex = 2;
			this.panelOutput.Paint += new System.Windows.Forms.PaintEventHandler( this.panelOutput_Paint );
			// 
			// checkBoxPolar
			// 
			this.checkBoxPolar.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxPolar.Checked = true;
			this.checkBoxPolar.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxPolar.Location = new System.Drawing.Point( 8, 528 );
			this.checkBoxPolar.Name = "checkBoxPolar";
			this.checkBoxPolar.Size = new System.Drawing.Size( 104, 24 );
			this.checkBoxPolar.TabIndex = 1;
			this.checkBoxPolar.Text = "Polar plot";
			this.checkBoxPolar.CheckedChanged += new System.EventHandler( this.checkBoxPolar_CheckedChanged );
			// 
			// PhasePolarPlotForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size( 5, 13 );
			this.ClientSize = new System.Drawing.Size( 528, 557 );
			this.Controls.Add( this.checkBoxPolar );
			this.Controls.Add( this.panelOutput );
			this.Name = "PhasePolarPlotForm";
			this.Text = "Mie Polar Plot";
			this.ResumeLayout( false );

		}
		#endregion

		protected override void OnResize( EventArgs e )
		{
			base.OnResize( e );

			panelOutput.Invalidate();
		}

		private void panelOutput_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			e.Graphics.FillRectangle( Brushes.White, panelOutput.ClientRectangle );

			double	fLogPhaseMin = Math.Log10( m_PhaseMin );
			double	fLogPhaseMax = Math.Log10( m_PhaseMax );

			if ( checkBoxPolar.Checked )
			{
				e.Graphics.DrawLine( Pens.Gray, .5f * 1.2f * panelOutput.Width, 0, .5f * 1.2f * panelOutput.Width, panelOutput.Height );
				e.Graphics.DrawLine( Pens.Gray, 0, .5f * panelOutput.Height, panelOutput.Width, .5f * panelOutput.Height );

				for ( int Index=0; Index < m_Phase.FactorsCount-1; Index++ )
				{
					float		fPhaseAngle0 = Index * (float) Math.PI / (m_Phase.FactorsCount-1);
					double		PhaseValue0 = m_Phase.GetPhaseFactor( fPhaseAngle0 );
					double		fLogPhase0 = Math.Log10( PhaseValue0 );
					float		fRadius0 = 0.01f + 1.0f * (float) ((fLogPhase0 - fLogPhaseMin) / (fLogPhaseMax - fLogPhaseMin));

					float		fPhaseAngle1 = (Index+1) * (float) Math.PI / (m_Phase.FactorsCount-1);
					double		PhaseValue1 = m_Phase.GetPhaseFactor( fPhaseAngle1 );
					double		fLogPhase1 = Math.Log10( PhaseValue1 );
					float		fRadius1 = 0.01f + 1.0f * (float) ((fLogPhase1 - fLogPhaseMin) / (fLogPhaseMax - fLogPhaseMin));

					e.Graphics.DrawLine( Pens.Black,	.5f * panelOutput.Width * (1.2f - fRadius0 * (float) Math.Cos( fPhaseAngle0 )), .5f * panelOutput.Height * (1.0f - fRadius0 * (float) Math.Sin( fPhaseAngle0 )),
														.5f * panelOutput.Width * (1.2f - fRadius1 * (float) Math.Cos( fPhaseAngle1 )), .5f * panelOutput.Height * (1.0f - fRadius1 * (float) Math.Sin( fPhaseAngle1 )));
					e.Graphics.DrawLine( Pens.Black,	.5f * panelOutput.Width * (1.2f - fRadius0 * (float) Math.Cos( fPhaseAngle0 )), .5f * panelOutput.Height * (1.0f + fRadius0 * (float) Math.Sin( fPhaseAngle0 )),
														.5f * panelOutput.Width * (1.2f - fRadius1 * (float) Math.Cos( fPhaseAngle1 )), .5f * panelOutput.Height * (1.0f + fRadius1 * (float) Math.Sin( fPhaseAngle1 )));
				}
			}
			else
			{
				WMath.Matrix3x3		XYZ_TO_RGB = new WMath.Matrix3x3( new float[3,3]	{	{  3.240790f, -0.969256f,  0.055648f },
																							{ -1.537150f,  1.875992f, -0.204043f },
																							{ -0.498535f,  0.041556f,  1.057311f } } );

				Point	Old = new Point( 0, panelOutput.ClientRectangle.Height );
				for ( int X=0; X < panelOutput.Width; X++ )
				{
					float	fPhaseAngle = (float) Math.PI * X / panelOutput.Width;
					double	PhaseValue = m_Phase.GetPhaseFactor( fPhaseAngle );
					double	fLogPhase = Math.Log10( PhaseValue );

					Point	New = new Point( X, (int) (panelOutput.Height * (0.95f - .9f * (fLogPhase - fLogPhaseMin) / (fLogPhaseMax - fLogPhaseMin))) );
					e.Graphics.DrawLine( Pens.Black, Old, New );
					Old = New;
				}
			}

			// Compute phase integral
			double	Integral = 0.0;
			for ( int Index=0; Index < m_Phase.FactorsCount; Index++ )
			{
				double	fTheta = Index * Math.PI / m_Phase.FactorsCount;

				Integral += m_Phase.PhaseFactors[Index] * Math.Sin( fTheta );
			}

			Integral *= Math.PI / m_Phase.FactorsCount;
			Integral *= 2.0 * Math.PI;

 			e.Graphics.DrawString( "Integral = " + Integral, Font, Brushes.Black, 0, 0 );
		}

		private void checkBoxPolar_CheckedChanged(object sender, System.EventArgs e)
		{
			panelOutput.Refresh();
		}
	}
}
