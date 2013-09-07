namespace Demo
{
	partial class CloudProfilerForm
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
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.integerTrackbarControlControlPointsCount = new Nuaj.Cirrus.Utility.IntegerTrackbarControl();
			this.buttonBuild = new System.Windows.Forms.Button();
			this.panelOutput = new CloudProfilerOutputPanel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panel1.Controls.Add(this.buttonBuild);
			this.panel1.Controls.Add(this.integerTrackbarControlControlPointsCount);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Location = new System.Drawing.Point(0, 518);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(357, 116);
			this.panel1.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 7);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(103, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Curve Control Points";
			// 
			// integerTrackbarControlControlPointsCount
			// 
			this.integerTrackbarControlControlPointsCount.Location = new System.Drawing.Point(127, 3);
			this.integerTrackbarControlControlPointsCount.MaximumSize = new System.Drawing.Size(10000, 20);
			this.integerTrackbarControlControlPointsCount.MinimumSize = new System.Drawing.Size(70, 20);
			this.integerTrackbarControlControlPointsCount.Name = "integerTrackbarControlControlPointsCount";
			this.integerTrackbarControlControlPointsCount.RangeMax = 10;
			this.integerTrackbarControlControlPointsCount.RangeMin = 2;
			this.integerTrackbarControlControlPointsCount.Size = new System.Drawing.Size(200, 20);
			this.integerTrackbarControlControlPointsCount.TabIndex = 3;
			this.integerTrackbarControlControlPointsCount.Value = 5;
			this.integerTrackbarControlControlPointsCount.VisibleRangeMax = 10;
			this.integerTrackbarControlControlPointsCount.VisibleRangeMin = 2;
			this.integerTrackbarControlControlPointsCount.ValueChanged += new Nuaj.Cirrus.Utility.IntegerTrackbarControl.ValueChangedEventHandler(this.integerTrackbarControlControlPointsCount_ValueChanged);
			// 
			// buttonBuild
			// 
			this.buttonBuild.Location = new System.Drawing.Point(123, 41);
			this.buttonBuild.Name = "buttonBuild";
			this.buttonBuild.Size = new System.Drawing.Size(110, 54);
			this.buttonBuild.TabIndex = 4;
			this.buttonBuild.Text = "Rebuild Profile";
			this.buttonBuild.UseVisualStyleBackColor = true;
			this.buttonBuild.Click += new System.EventHandler(this.buttonBuild_Click);
			// 
			// panelOutput
			// 
			this.panelOutput.Location = new System.Drawing.Point(7, 10);
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size(345, 500);
			this.panelOutput.TabIndex = 0;
			// 
			// Form3
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(357, 635);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panelOutput);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "Form3";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Cloud Profiler";
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private CloudProfilerOutputPanel panelOutput;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonBuild;
		private Nuaj.Cirrus.Utility.IntegerTrackbarControl integerTrackbarControlControlPointsCount;
	}
}