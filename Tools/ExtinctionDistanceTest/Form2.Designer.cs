namespace ExtinctionDistanceTest
{
	partial class Form2
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
			this.floatTrackbarControlHDRMaxIntensity = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControlMaxX = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlLDRWhitePoint = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlHDRWhitePoint = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlMaxY = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label5 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.label10 = new System.Windows.Forms.Label();
			this.floatTrackbarControlA = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlB = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label6 = new System.Windows.Forms.Label();
			this.floatTrackbarControlC = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label7 = new System.Windows.Forms.Label();
			this.floatTrackbarControlD = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label8 = new System.Windows.Forms.Label();
			this.floatTrackbarControlE = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label9 = new System.Windows.Forms.Label();
			this.floatTrackbarControlF = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label11 = new System.Windows.Forms.Label();
			this.panelOutput = new ExtinctionDistanceTest.OutputPanel2();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// floatTrackbarControlHDRMaxIntensity
			// 
			this.floatTrackbarControlHDRMaxIntensity.Location = new System.Drawing.Point(127, 3);
			this.floatTrackbarControlHDRMaxIntensity.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlHDRMaxIntensity.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlHDRMaxIntensity.Name = "floatTrackbarControlHDRMaxIntensity";
			this.floatTrackbarControlHDRMaxIntensity.RangeMax = 1000F;
			this.floatTrackbarControlHDRMaxIntensity.RangeMin = 0F;
			this.floatTrackbarControlHDRMaxIntensity.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlHDRMaxIntensity.TabIndex = 1;
			this.floatTrackbarControlHDRMaxIntensity.Value = 10F;
			this.floatTrackbarControlHDRMaxIntensity.VisibleRangeMax = 100F;
			this.floatTrackbarControlHDRMaxIntensity.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCloudExtinction_ValueChanged);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panel1.Controls.Add(this.label5);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.floatTrackbarControlMaxY);
			this.panel1.Controls.Add(this.floatTrackbarControlMaxX);
			this.panel1.Controls.Add(this.floatTrackbarControlLDRWhitePoint);
			this.panel1.Controls.Add(this.floatTrackbarControlHDRWhitePoint);
			this.panel1.Controls.Add(this.floatTrackbarControlHDRMaxIntensity);
			this.panel1.Location = new System.Drawing.Point(0, 470);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(712, 164);
			this.panel1.TabIndex = 2;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(354, 6);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(37, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "Max X";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 58);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(87, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "LDR White Point";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(89, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "HDR White Point";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 7);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(96, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "HDR Max Intensity";
			// 
			// floatTrackbarControlMaxX
			// 
			this.floatTrackbarControlMaxX.Location = new System.Drawing.Point(478, 3);
			this.floatTrackbarControlMaxX.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlMaxX.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlMaxX.Name = "floatTrackbarControlMaxX";
			this.floatTrackbarControlMaxX.RangeMax = 100F;
			this.floatTrackbarControlMaxX.RangeMin = 0F;
			this.floatTrackbarControlMaxX.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlMaxX.TabIndex = 1;
			this.floatTrackbarControlMaxX.Value = 10F;
			this.floatTrackbarControlMaxX.VisibleRangeMax = 100F;
			this.floatTrackbarControlMaxX.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlMaxX_ValueChanged);
			// 
			// floatTrackbarControlLDRWhitePoint
			// 
			this.floatTrackbarControlLDRWhitePoint.Location = new System.Drawing.Point(127, 55);
			this.floatTrackbarControlLDRWhitePoint.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlLDRWhitePoint.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlLDRWhitePoint.Name = "floatTrackbarControlLDRWhitePoint";
			this.floatTrackbarControlLDRWhitePoint.RangeMax = 100F;
			this.floatTrackbarControlLDRWhitePoint.RangeMin = 0F;
			this.floatTrackbarControlLDRWhitePoint.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlLDRWhitePoint.TabIndex = 1;
			this.floatTrackbarControlLDRWhitePoint.Value = 1F;
			this.floatTrackbarControlLDRWhitePoint.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlStepSize_ValueChanged);
			// 
			// floatTrackbarControlHDRWhitePoint
			// 
			this.floatTrackbarControlHDRWhitePoint.Location = new System.Drawing.Point(127, 29);
			this.floatTrackbarControlHDRWhitePoint.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlHDRWhitePoint.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlHDRWhitePoint.Name = "floatTrackbarControlHDRWhitePoint";
			this.floatTrackbarControlHDRWhitePoint.RangeMax = 1000F;
			this.floatTrackbarControlHDRWhitePoint.RangeMin = 0F;
			this.floatTrackbarControlHDRWhitePoint.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlHDRWhitePoint.TabIndex = 1;
			this.floatTrackbarControlHDRWhitePoint.Value = 10F;
			this.floatTrackbarControlHDRWhitePoint.VisibleRangeMax = 100F;
			this.floatTrackbarControlHDRWhitePoint.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlOpacityCoefficient_ValueChanged);
			// 
			// floatTrackbarControlMaxY
			// 
			this.floatTrackbarControlMaxY.Location = new System.Drawing.Point(478, 29);
			this.floatTrackbarControlMaxY.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlMaxY.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlMaxY.Name = "floatTrackbarControlMaxY";
			this.floatTrackbarControlMaxY.RangeMax = 100F;
			this.floatTrackbarControlMaxY.RangeMin = 0F;
			this.floatTrackbarControlMaxY.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlMaxY.TabIndex = 1;
			this.floatTrackbarControlMaxY.Value = 1F;
			this.floatTrackbarControlMaxY.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlMaxY_ValueChanged);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(354, 32);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(37, 13);
			this.label5.TabIndex = 2;
			this.label5.Text = "Max Y";
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panel2.Controls.Add(this.label11);
			this.panel2.Controls.Add(this.label9);
			this.panel2.Controls.Add(this.label8);
			this.panel2.Controls.Add(this.label7);
			this.panel2.Controls.Add(this.label6);
			this.panel2.Controls.Add(this.label10);
			this.panel2.Controls.Add(this.floatTrackbarControlF);
			this.panel2.Controls.Add(this.floatTrackbarControlE);
			this.panel2.Controls.Add(this.floatTrackbarControlD);
			this.panel2.Controls.Add(this.floatTrackbarControlC);
			this.panel2.Controls.Add(this.floatTrackbarControlB);
			this.panel2.Controls.Add(this.floatTrackbarControlA);
			this.panel2.Location = new System.Drawing.Point(718, 12);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(235, 622);
			this.panel2.TabIndex = 2;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(3, 7);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(14, 13);
			this.label10.TabIndex = 2;
			this.label10.Text = "A";
			// 
			// floatTrackbarControlA
			// 
			this.floatTrackbarControlA.Location = new System.Drawing.Point(23, 4);
			this.floatTrackbarControlA.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlA.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlA.Name = "floatTrackbarControlA";
			this.floatTrackbarControlA.RangeMax = 10F;
			this.floatTrackbarControlA.RangeMin = -10F;
			this.floatTrackbarControlA.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlA.TabIndex = 1;
			this.floatTrackbarControlA.Value = 0.15F;
			this.floatTrackbarControlA.VisibleRangeMax = 1F;
			this.floatTrackbarControlA.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlA_ValueChanged);
			// 
			// floatTrackbarControlB
			// 
			this.floatTrackbarControlB.Location = new System.Drawing.Point(23, 30);
			this.floatTrackbarControlB.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlB.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlB.Name = "floatTrackbarControlB";
			this.floatTrackbarControlB.RangeMax = 10F;
			this.floatTrackbarControlB.RangeMin = -10F;
			this.floatTrackbarControlB.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlB.TabIndex = 1;
			this.floatTrackbarControlB.Value = 0.5F;
			this.floatTrackbarControlB.VisibleRangeMax = 1F;
			this.floatTrackbarControlB.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlA_ValueChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(3, 33);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(14, 13);
			this.label6.TabIndex = 2;
			this.label6.Text = "B";
			// 
			// floatTrackbarControlC
			// 
			this.floatTrackbarControlC.Location = new System.Drawing.Point(23, 56);
			this.floatTrackbarControlC.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlC.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlC.Name = "floatTrackbarControlC";
			this.floatTrackbarControlC.RangeMax = 10F;
			this.floatTrackbarControlC.RangeMin = -10F;
			this.floatTrackbarControlC.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlC.TabIndex = 1;
			this.floatTrackbarControlC.Value = 0.1F;
			this.floatTrackbarControlC.VisibleRangeMax = 1F;
			this.floatTrackbarControlC.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlA_ValueChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(3, 59);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(14, 13);
			this.label7.TabIndex = 2;
			this.label7.Text = "C";
			// 
			// floatTrackbarControlD
			// 
			this.floatTrackbarControlD.Location = new System.Drawing.Point(23, 82);
			this.floatTrackbarControlD.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlD.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlD.Name = "floatTrackbarControlD";
			this.floatTrackbarControlD.RangeMax = 10F;
			this.floatTrackbarControlD.RangeMin = -10F;
			this.floatTrackbarControlD.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlD.TabIndex = 1;
			this.floatTrackbarControlD.Value = 0.2F;
			this.floatTrackbarControlD.VisibleRangeMax = 1F;
			this.floatTrackbarControlD.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlA_ValueChanged);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(3, 85);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(15, 13);
			this.label8.TabIndex = 2;
			this.label8.Text = "D";
			// 
			// floatTrackbarControlE
			// 
			this.floatTrackbarControlE.Location = new System.Drawing.Point(23, 108);
			this.floatTrackbarControlE.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlE.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlE.Name = "floatTrackbarControlE";
			this.floatTrackbarControlE.RangeMax = 10F;
			this.floatTrackbarControlE.RangeMin = -10F;
			this.floatTrackbarControlE.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlE.TabIndex = 1;
			this.floatTrackbarControlE.Value = 0.02F;
			this.floatTrackbarControlE.VisibleRangeMax = 1F;
			this.floatTrackbarControlE.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlA_ValueChanged);
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(3, 111);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(14, 13);
			this.label9.TabIndex = 2;
			this.label9.Text = "E";
			// 
			// floatTrackbarControlF
			// 
			this.floatTrackbarControlF.Location = new System.Drawing.Point(23, 134);
			this.floatTrackbarControlF.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlF.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlF.Name = "floatTrackbarControlF";
			this.floatTrackbarControlF.RangeMax = 10F;
			this.floatTrackbarControlF.RangeMin = -10F;
			this.floatTrackbarControlF.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlF.TabIndex = 1;
			this.floatTrackbarControlF.Value = 0.3F;
			this.floatTrackbarControlF.VisibleRangeMax = 1F;
			this.floatTrackbarControlF.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlA_ValueChanged);
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(3, 137);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(13, 13);
			this.label11.TabIndex = 2;
			this.label11.Text = "F";
			// 
			// panelOutput
			// 
			this.panelOutput.Location = new System.Drawing.Point(12, 12);
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size(700, 438);
			this.panelOutput.TabIndex = 0;
			this.panelOutput.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelOutput_MouseDown);
			// 
			// Form2
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(958, 635);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panelOutput);
			this.Name = "Form2";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Filmic Tone Mapping Operator Test";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private OutputPanel2 panelOutput;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlHDRMaxIntensity;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlHDRWhitePoint;
		private System.Windows.Forms.Label label3;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlLDRWhitePoint;
		private System.Windows.Forms.Label label4;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlMaxX;
		private System.Windows.Forms.Label label5;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlMaxY;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label10;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlA;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlF;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlE;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlD;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlC;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlB;
	}
}

