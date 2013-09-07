namespace ShadowMapTest
{
	partial class Form1
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
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
			this.floatTrackbarControlSunPhi = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControl2 = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCameraTheta = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSunTheta = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCameraPhi = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.panelOutput = new ShadowMapTest.OutputPanel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// floatTrackbarControlSunPhi
			// 
			this.floatTrackbarControlSunPhi.Location = new System.Drawing.Point(68, 3);
			this.floatTrackbarControlSunPhi.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlSunPhi.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlSunPhi.Name = "floatTrackbarControlSunPhi";
			this.floatTrackbarControlSunPhi.RangeMax = 180F;
			this.floatTrackbarControlSunPhi.RangeMin = -180F;
			this.floatTrackbarControlSunPhi.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlSunPhi.TabIndex = 1;
			this.floatTrackbarControlSunPhi.Value = 0F;
			this.floatTrackbarControlSunPhi.VisibleRangeMax = 180F;
			this.floatTrackbarControlSunPhi.VisibleRangeMin = -180F;
			this.floatTrackbarControlSunPhi.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlSunPhi_ValueChanged);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.floatTrackbarControl2);
			this.panel1.Controls.Add(this.floatTrackbarControlCameraTheta);
			this.panel1.Controls.Add(this.floatTrackbarControlSunTheta);
			this.panel1.Controls.Add(this.floatTrackbarControlCameraPhi);
			this.panel1.Controls.Add(this.floatTrackbarControlSunPhi);
			this.panel1.Location = new System.Drawing.Point(0, 470);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(599, 164);
			this.panel1.TabIndex = 2;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(277, 27);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(74, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "Camera Theta";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(277, 7);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(57, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Sun Theta";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 27);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(61, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Camera Phi";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 7);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Sun Phi";
			// 
			// floatTrackbarControl2
			// 
			this.floatTrackbarControl2.Location = new System.Drawing.Point(83, 103);
			this.floatTrackbarControl2.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControl2.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControl2.Name = "floatTrackbarControl2";
			this.floatTrackbarControl2.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControl2.TabIndex = 1;
			this.floatTrackbarControl2.Value = 0F;
			// 
			// floatTrackbarControlCameraTheta
			// 
			this.floatTrackbarControlCameraTheta.Location = new System.Drawing.Point(353, 23);
			this.floatTrackbarControlCameraTheta.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCameraTheta.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCameraTheta.Name = "floatTrackbarControlCameraTheta";
			this.floatTrackbarControlCameraTheta.RangeMax = 180F;
			this.floatTrackbarControlCameraTheta.RangeMin = 0F;
			this.floatTrackbarControlCameraTheta.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCameraTheta.TabIndex = 1;
			this.floatTrackbarControlCameraTheta.Value = 90F;
			this.floatTrackbarControlCameraTheta.VisibleRangeMax = 180F;
			this.floatTrackbarControlCameraTheta.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlSunPhi_ValueChanged);
			// 
			// floatTrackbarControlSunTheta
			// 
			this.floatTrackbarControlSunTheta.Location = new System.Drawing.Point(353, 3);
			this.floatTrackbarControlSunTheta.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlSunTheta.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlSunTheta.Name = "floatTrackbarControlSunTheta";
			this.floatTrackbarControlSunTheta.RangeMax = 180F;
			this.floatTrackbarControlSunTheta.RangeMin = 0F;
			this.floatTrackbarControlSunTheta.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlSunTheta.TabIndex = 1;
			this.floatTrackbarControlSunTheta.Value = 0F;
			this.floatTrackbarControlSunTheta.VisibleRangeMax = 180F;
			this.floatTrackbarControlSunTheta.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlSunPhi_ValueChanged);
			// 
			// floatTrackbarControlCameraPhi
			// 
			this.floatTrackbarControlCameraPhi.Location = new System.Drawing.Point(68, 23);
			this.floatTrackbarControlCameraPhi.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCameraPhi.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCameraPhi.Name = "floatTrackbarControlCameraPhi";
			this.floatTrackbarControlCameraPhi.RangeMax = 180F;
			this.floatTrackbarControlCameraPhi.RangeMin = -180F;
			this.floatTrackbarControlCameraPhi.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCameraPhi.TabIndex = 1;
			this.floatTrackbarControlCameraPhi.Value = 0F;
			this.floatTrackbarControlCameraPhi.VisibleRangeMax = 180F;
			this.floatTrackbarControlCameraPhi.VisibleRangeMin = -180F;
			this.floatTrackbarControlCameraPhi.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlSunPhi_ValueChanged);
			// 
			// panelOutput
			// 
			this.panelOutput.Location = new System.Drawing.Point(56, 12);
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size(474, 438);
			this.panelOutput.TabIndex = 0;
			this.panelOutput.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelOutput_MouseDown);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(600, 635);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panelOutput);
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Shadow Map Enclosure Test";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private OutputPanel panelOutput;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunPhi;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControl2;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCameraTheta;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunTheta;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCameraPhi;
	}
}

