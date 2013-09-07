namespace Demo
{
	partial class DemoForm
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
			this.components = new System.ComponentModel.Container();
			this.panelProperties = new System.Windows.Forms.Panel();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.richTextBoxOutput = new Demo.LogTextBox( this.components );
			this.splitterProperties = new System.Windows.Forms.Splitter();
			this.treeViewObjects = new System.Windows.Forms.TreeView();
			this.panelErrors = new System.Windows.Forms.Panel();
			this.panelBars = new System.Windows.Forms.Panel();
			this.label7 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.floatTrackbarControlStreaksAttenuation = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlStreaksThreshold = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlStreaksCoverAngle = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlStreaksAngle = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label9 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarControlBloomGamma = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlStreaksFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlBloomRadius = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlWhiteLevel = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlBloomFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlBloomThreshold = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label6 = new System.Windows.Forms.Label();
			this.panelOutput = new Demo.OutputPanel( this.components );
			this.panelProperties.SuspendLayout();
			this.panelErrors.SuspendLayout();
			this.panelBars.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelProperties
			// 
			this.panelProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelProperties.Controls.Add( this.propertyGrid );
			this.panelProperties.Controls.Add( this.richTextBoxOutput );
			this.panelProperties.Controls.Add( this.splitterProperties );
			this.panelProperties.Controls.Add( this.treeViewObjects );
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelProperties.Location = new System.Drawing.Point( 889, 0 );
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size( 274, 801 );
			this.panelProperties.TabIndex = 1;
			// 
			// propertyGrid
			// 
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.Location = new System.Drawing.Point( 0, 275 );
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size( 272, 417 );
			this.propertyGrid.TabIndex = 2;
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 0, 692 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 272, 107 );
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
			// 
			// splitterProperties
			// 
			this.splitterProperties.Dock = System.Windows.Forms.DockStyle.Top;
			this.splitterProperties.Location = new System.Drawing.Point( 0, 272 );
			this.splitterProperties.Name = "splitterProperties";
			this.splitterProperties.Size = new System.Drawing.Size( 272, 3 );
			this.splitterProperties.TabIndex = 1;
			this.splitterProperties.TabStop = false;
			// 
			// treeViewObjects
			// 
			this.treeViewObjects.Dock = System.Windows.Forms.DockStyle.Top;
			this.treeViewObjects.FullRowSelect = true;
			this.treeViewObjects.HideSelection = false;
			this.treeViewObjects.Location = new System.Drawing.Point( 0, 0 );
			this.treeViewObjects.Name = "treeViewObjects";
			this.treeViewObjects.Size = new System.Drawing.Size( 272, 272 );
			this.treeViewObjects.TabIndex = 0;
			this.treeViewObjects.AfterSelect += new System.Windows.Forms.TreeViewEventHandler( this.treeViewObjects_AfterSelect );
			// 
			// panelErrors
			// 
			this.panelErrors.Controls.Add( this.panelBars );
			this.panelErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelErrors.Location = new System.Drawing.Point( 0, 679 );
			this.panelErrors.Name = "panelErrors";
			this.panelErrors.Size = new System.Drawing.Size( 889, 122 );
			this.panelErrors.TabIndex = 1;
			// 
			// panelBars
			// 
			this.panelBars.Controls.Add( this.label7 );
			this.panelBars.Controls.Add( this.label4 );
			this.panelBars.Controls.Add( this.label5 );
			this.panelBars.Controls.Add( this.label8 );
			this.panelBars.Controls.Add( this.label1 );
			this.panelBars.Controls.Add( this.label3 );
			this.panelBars.Controls.Add( this.floatTrackbarControlStreaksAttenuation );
			this.panelBars.Controls.Add( this.floatTrackbarControlStreaksThreshold );
			this.panelBars.Controls.Add( this.floatTrackbarControlStreaksCoverAngle );
			this.panelBars.Controls.Add( this.floatTrackbarControlStreaksAngle );
			this.panelBars.Controls.Add( this.label9 );
			this.panelBars.Controls.Add( this.label2 );
			this.panelBars.Controls.Add( this.floatTrackbarControlBloomGamma );
			this.panelBars.Controls.Add( this.floatTrackbarControlStreaksFactor );
			this.panelBars.Controls.Add( this.floatTrackbarControlBloomRadius );
			this.panelBars.Controls.Add( this.floatTrackbarControlWhiteLevel );
			this.panelBars.Controls.Add( this.floatTrackbarControlBloomFactor );
			this.panelBars.Controls.Add( this.floatTrackbarControlBloomThreshold );
			this.panelBars.Controls.Add( this.label6 );
			this.panelBars.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelBars.Location = new System.Drawing.Point( 0, 0 );
			this.panelBars.Name = "panelBars";
			this.panelBars.Size = new System.Drawing.Size( 889, 122 );
			this.panelBars.TabIndex = 3;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point( 280, 34 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 93, 13 );
			this.label7.TabIndex = 1;
			this.label7.Text = "Streaks Threshold";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 280, 78 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 73, 13 );
			this.label4.TabIndex = 1;
			this.label4.Text = "Streaks Angle";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 280, 56 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 76, 13 );
			this.label5.TabIndex = 1;
			this.label5.Text = "Streaks Factor";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point( 3, 100 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 75, 13 );
			this.label8.TabIndex = 1;
			this.label8.Text = "Bloom Gamma";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 3, 78 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 72, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Bloom Radius";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 3, 56 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 69, 13 );
			this.label3.TabIndex = 1;
			this.label3.Text = "Bloom Factor";
			// 
			// floatTrackbarControlStreaksAttenuation
			// 
			this.floatTrackbarControlStreaksAttenuation.Location = new System.Drawing.Point( 372, 97 );
			this.floatTrackbarControlStreaksAttenuation.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlStreaksAttenuation.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlStreaksAttenuation.Name = "floatTrackbarControlStreaksAttenuation";
			this.floatTrackbarControlStreaksAttenuation.RangeMax = 0.9999F;
			this.floatTrackbarControlStreaksAttenuation.RangeMin = 0F;
			this.floatTrackbarControlStreaksAttenuation.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlStreaksAttenuation.TabIndex = 0;
			this.floatTrackbarControlStreaksAttenuation.Value = 0.9F;
			this.floatTrackbarControlStreaksAttenuation.VisibleRangeMax = 0.9999F;
			this.floatTrackbarControlStreaksAttenuation.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlStreaksThreshold
			// 
			this.floatTrackbarControlStreaksThreshold.Location = new System.Drawing.Point( 372, 31 );
			this.floatTrackbarControlStreaksThreshold.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlStreaksThreshold.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlStreaksThreshold.Name = "floatTrackbarControlStreaksThreshold";
			this.floatTrackbarControlStreaksThreshold.RangeMax = 4F;
			this.floatTrackbarControlStreaksThreshold.RangeMin = 0F;
			this.floatTrackbarControlStreaksThreshold.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlStreaksThreshold.TabIndex = 0;
			this.floatTrackbarControlStreaksThreshold.Value = 0.7F;
			this.floatTrackbarControlStreaksThreshold.VisibleRangeMax = 1F;
			this.floatTrackbarControlStreaksThreshold.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlStreaksCoverAngle
			// 
			this.floatTrackbarControlStreaksCoverAngle.Location = new System.Drawing.Point( 457, 75 );
			this.floatTrackbarControlStreaksCoverAngle.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlStreaksCoverAngle.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlStreaksCoverAngle.Name = "floatTrackbarControlStreaksCoverAngle";
			this.floatTrackbarControlStreaksCoverAngle.RangeMax = 180F;
			this.floatTrackbarControlStreaksCoverAngle.RangeMin = 0F;
			this.floatTrackbarControlStreaksCoverAngle.Size = new System.Drawing.Size( 99, 20 );
			this.floatTrackbarControlStreaksCoverAngle.TabIndex = 0;
			this.floatTrackbarControlStreaksCoverAngle.Value = 180F;
			this.floatTrackbarControlStreaksCoverAngle.VisibleRangeMax = 180F;
			this.floatTrackbarControlStreaksCoverAngle.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlStreaksAngle
			// 
			this.floatTrackbarControlStreaksAngle.Location = new System.Drawing.Point( 355, 75 );
			this.floatTrackbarControlStreaksAngle.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlStreaksAngle.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlStreaksAngle.Name = "floatTrackbarControlStreaksAngle";
			this.floatTrackbarControlStreaksAngle.RangeMax = 360F;
			this.floatTrackbarControlStreaksAngle.RangeMin = 0F;
			this.floatTrackbarControlStreaksAngle.Size = new System.Drawing.Size( 99, 20 );
			this.floatTrackbarControlStreaksAngle.TabIndex = 0;
			this.floatTrackbarControlStreaksAngle.Value = 30F;
			this.floatTrackbarControlStreaksAngle.VisibleRangeMax = 360F;
			this.floatTrackbarControlStreaksAngle.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point( 97, 8 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 64, 13 );
			this.label9.TabIndex = 1;
			this.label9.Text = "White Level";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 3, 34 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 86, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Bloom Threshold";
			// 
			// floatTrackbarControlBloomGamma
			// 
			this.floatTrackbarControlBloomGamma.Location = new System.Drawing.Point( 95, 97 );
			this.floatTrackbarControlBloomGamma.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlBloomGamma.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlBloomGamma.Name = "floatTrackbarControlBloomGamma";
			this.floatTrackbarControlBloomGamma.RangeMax = 100F;
			this.floatTrackbarControlBloomGamma.RangeMin = 0.01F;
			this.floatTrackbarControlBloomGamma.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlBloomGamma.TabIndex = 0;
			this.floatTrackbarControlBloomGamma.Value = 1F;
			this.floatTrackbarControlBloomGamma.VisibleRangeMax = 4F;
			this.floatTrackbarControlBloomGamma.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlStreaksFactor
			// 
			this.floatTrackbarControlStreaksFactor.Location = new System.Drawing.Point( 372, 53 );
			this.floatTrackbarControlStreaksFactor.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlStreaksFactor.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlStreaksFactor.Name = "floatTrackbarControlStreaksFactor";
			this.floatTrackbarControlStreaksFactor.RangeMax = 10F;
			this.floatTrackbarControlStreaksFactor.RangeMin = 0F;
			this.floatTrackbarControlStreaksFactor.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlStreaksFactor.TabIndex = 0;
			this.floatTrackbarControlStreaksFactor.Value = 1F;
			this.floatTrackbarControlStreaksFactor.VisibleRangeMax = 1F;
			this.floatTrackbarControlStreaksFactor.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlBloomRadius
			// 
			this.floatTrackbarControlBloomRadius.Location = new System.Drawing.Point( 95, 75 );
			this.floatTrackbarControlBloomRadius.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlBloomRadius.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlBloomRadius.Name = "floatTrackbarControlBloomRadius";
			this.floatTrackbarControlBloomRadius.RangeMax = 100F;
			this.floatTrackbarControlBloomRadius.RangeMin = 0F;
			this.floatTrackbarControlBloomRadius.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlBloomRadius.TabIndex = 0;
			this.floatTrackbarControlBloomRadius.Value = 1F;
			this.floatTrackbarControlBloomRadius.VisibleRangeMax = 2F;
			this.floatTrackbarControlBloomRadius.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlWhiteLevel
			// 
			this.floatTrackbarControlWhiteLevel.Location = new System.Drawing.Point( 163, 4 );
			this.floatTrackbarControlWhiteLevel.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlWhiteLevel.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlWhiteLevel.Name = "floatTrackbarControlWhiteLevel";
			this.floatTrackbarControlWhiteLevel.RangeMax = 100F;
			this.floatTrackbarControlWhiteLevel.RangeMin = 0F;
			this.floatTrackbarControlWhiteLevel.Size = new System.Drawing.Size( 264, 20 );
			this.floatTrackbarControlWhiteLevel.TabIndex = 0;
			this.floatTrackbarControlWhiteLevel.Value = 15F;
			this.floatTrackbarControlWhiteLevel.VisibleRangeMax = 30F;
			this.floatTrackbarControlWhiteLevel.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlBloomFactor
			// 
			this.floatTrackbarControlBloomFactor.Location = new System.Drawing.Point( 95, 53 );
			this.floatTrackbarControlBloomFactor.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlBloomFactor.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlBloomFactor.Name = "floatTrackbarControlBloomFactor";
			this.floatTrackbarControlBloomFactor.RangeMax = 100F;
			this.floatTrackbarControlBloomFactor.RangeMin = 0F;
			this.floatTrackbarControlBloomFactor.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlBloomFactor.TabIndex = 0;
			this.floatTrackbarControlBloomFactor.Value = 1F;
			this.floatTrackbarControlBloomFactor.VisibleRangeMax = 2F;
			this.floatTrackbarControlBloomFactor.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// floatTrackbarControlBloomThreshold
			// 
			this.floatTrackbarControlBloomThreshold.Location = new System.Drawing.Point( 95, 31 );
			this.floatTrackbarControlBloomThreshold.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlBloomThreshold.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlBloomThreshold.Name = "floatTrackbarControlBloomThreshold";
			this.floatTrackbarControlBloomThreshold.RangeMax = 4F;
			this.floatTrackbarControlBloomThreshold.RangeMin = 0F;
			this.floatTrackbarControlBloomThreshold.Size = new System.Drawing.Size( 179, 20 );
			this.floatTrackbarControlBloomThreshold.TabIndex = 0;
			this.floatTrackbarControlBloomThreshold.Value = 0.5F;
			this.floatTrackbarControlBloomThreshold.VisibleRangeMax = 1F;
			this.floatTrackbarControlBloomThreshold.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlStreaksFactor_ValueChanged );
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 280, 100 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 79, 13 );
			this.label6.TabIndex = 1;
			this.label6.Text = "Streaks Length";
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 889, 679 );
			this.panelOutput.TabIndex = 0;
			// 
			// DemoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1163, 801 );
			this.Controls.Add( this.panelOutput );
			this.Controls.Add( this.panelErrors );
			this.Controls.Add( this.panelProperties );
			this.Name = "DemoForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Bloom & Streaks Demo";
			this.panelProperties.ResumeLayout( false );
			this.panelErrors.ResumeLayout( false );
			this.panelBars.ResumeLayout( false );
			this.panelBars.PerformLayout();
			this.ResumeLayout( false );

		}

		#endregion

		private OutputPanel panelOutput;
		private System.Windows.Forms.Panel panelProperties;
		private System.Windows.Forms.PropertyGrid propertyGrid;
		private System.Windows.Forms.Splitter splitterProperties;
		private System.Windows.Forms.TreeView treeViewObjects;
		private System.Windows.Forms.Panel panelErrors;
		private LogTextBox richTextBoxOutput;
		private System.Windows.Forms.Panel panelBars;
		private System.Windows.Forms.Label label2;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlBloomThreshold;
		private System.Windows.Forms.Label label3;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlBloomFactor;
		private System.Windows.Forms.Label label5;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlStreaksFactor;
		private System.Windows.Forms.Label label7;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlStreaksThreshold;
		private System.Windows.Forms.Label label1;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlBloomRadius;
		private System.Windows.Forms.Label label4;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlStreaksAttenuation;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlStreaksAngle;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label8;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlBloomGamma;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlStreaksCoverAngle;
		private System.Windows.Forms.Label label9;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlWhiteLevel;
	}
}

