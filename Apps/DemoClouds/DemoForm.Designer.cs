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
			this.splitterProperties = new System.Windows.Forms.Splitter();
			this.treeViewObjects = new System.Windows.Forms.TreeView();
			this.panelErrors = new System.Windows.Forms.Panel();
			this.panelBars = new System.Windows.Forms.Panel();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.floatTrackbarControlAerosolsAmount = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlFogAmount = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label6 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarControlCloudAltitude = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudSize = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudType = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCoverage = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSunElevation = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControlSunAzimuth = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.richTextBoxOutput = new Nuaj.Cirrus.Utility.LogTextBox( this.components );
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
			this.panelProperties.Controls.Add( this.splitterProperties );
			this.panelProperties.Controls.Add( this.treeViewObjects );
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelProperties.Location = new System.Drawing.Point( 920, 0 );
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size( 274, 713 );
			this.panelProperties.TabIndex = 1;
			// 
			// propertyGrid
			// 
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.Location = new System.Drawing.Point( 0, 275 );
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size( 272, 436 );
			this.propertyGrid.TabIndex = 2;
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
			this.panelErrors.Controls.Add( this.richTextBoxOutput );
			this.panelErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelErrors.Location = new System.Drawing.Point( 0, 606 );
			this.panelErrors.Name = "panelErrors";
			this.panelErrors.Size = new System.Drawing.Size( 920, 107 );
			this.panelErrors.TabIndex = 1;
			// 
			// panelBars
			// 
			this.panelBars.Controls.Add( this.label7 );
			this.panelBars.Controls.Add( this.label8 );
			this.panelBars.Controls.Add( this.label5 );
			this.panelBars.Controls.Add( this.label4 );
			this.panelBars.Controls.Add( this.label3 );
			this.panelBars.Controls.Add( this.floatTrackbarControlAerosolsAmount );
			this.panelBars.Controls.Add( this.floatTrackbarControlFogAmount );
			this.panelBars.Controls.Add( this.label6 );
			this.panelBars.Controls.Add( this.label2 );
			this.panelBars.Controls.Add( this.floatTrackbarControlCloudAltitude );
			this.panelBars.Controls.Add( this.floatTrackbarControlCloudSize );
			this.panelBars.Controls.Add( this.floatTrackbarControlCloudType );
			this.panelBars.Controls.Add( this.floatTrackbarControlCoverage );
			this.panelBars.Controls.Add( this.floatTrackbarControlSunElevation );
			this.panelBars.Controls.Add( this.label1 );
			this.panelBars.Controls.Add( this.floatTrackbarControlSunAzimuth );
			this.panelBars.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelBars.Location = new System.Drawing.Point( 0, 0 );
			this.panelBars.Name = "panelBars";
			this.panelBars.Size = new System.Drawing.Size( 623, 107 );
			this.panelBars.TabIndex = 3;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point( 33, 58 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 58, 13 );
			this.label7.TabIndex = 1;
			this.label7.Text = "Air Amount";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point( 317, 80 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 72, 13 );
			this.label8.TabIndex = 1;
			this.label8.Text = "Cloud Altitude";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 33, 80 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 57, 13 );
			this.label5.TabIndex = 1;
			this.label5.Text = "Cloud Size";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 317, 36 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 73, 13 );
			this.label4.TabIndex = 1;
			this.label4.Text = "Cloud Opacity";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 33, 36 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 53, 13 );
			this.label3.TabIndex = 1;
			this.label3.Text = "Coverage";
			// 
			// floatTrackbarControlAerosolsAmount
			// 
			this.floatTrackbarControlAerosolsAmount.Location = new System.Drawing.Point( 105, 55 );
			this.floatTrackbarControlAerosolsAmount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlAerosolsAmount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlAerosolsAmount.Name = "floatTrackbarControlAerosolsAmount";
			this.floatTrackbarControlAerosolsAmount.RangeMax = 1F;
			this.floatTrackbarControlAerosolsAmount.RangeMin = 0F;
			this.floatTrackbarControlAerosolsAmount.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlAerosolsAmount.TabIndex = 0;
			this.floatTrackbarControlAerosolsAmount.Value = 0.1F;
			this.floatTrackbarControlAerosolsAmount.VisibleRangeMax = 1F;
			this.floatTrackbarControlAerosolsAmount.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlAerosolsAmount_ValueChanged );
			// 
			// floatTrackbarControlFogAmount
			// 
			this.floatTrackbarControlFogAmount.Location = new System.Drawing.Point( 389, 55 );
			this.floatTrackbarControlFogAmount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlFogAmount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlFogAmount.Name = "floatTrackbarControlFogAmount";
			this.floatTrackbarControlFogAmount.RangeMax = 1F;
			this.floatTrackbarControlFogAmount.RangeMin = 0F;
			this.floatTrackbarControlFogAmount.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlFogAmount.TabIndex = 0;
			this.floatTrackbarControlFogAmount.Value = 0.1F;
			this.floatTrackbarControlFogAmount.VisibleRangeMax = 1F;
			this.floatTrackbarControlFogAmount.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlFogAmount_ValueChanged );
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 317, 58 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 64, 13 );
			this.label6.TabIndex = 1;
			this.label6.Text = "Fog Amount";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 33, 14 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 73, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Sun Elevation";
			// 
			// floatTrackbarControlCloudAltitude
			// 
			this.floatTrackbarControlCloudAltitude.Location = new System.Drawing.Point( 389, 77 );
			this.floatTrackbarControlCloudAltitude.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudAltitude.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudAltitude.Name = "floatTrackbarControlCloudAltitude";
			this.floatTrackbarControlCloudAltitude.RangeMax = 120F;
			this.floatTrackbarControlCloudAltitude.RangeMin = -40F;
			this.floatTrackbarControlCloudAltitude.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudAltitude.TabIndex = 0;
			this.floatTrackbarControlCloudAltitude.Value = 60F;
			this.floatTrackbarControlCloudAltitude.VisibleRangeMax = 120F;
			this.floatTrackbarControlCloudAltitude.VisibleRangeMin = -40F;
			this.floatTrackbarControlCloudAltitude.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudAltitude_ValueChanged );
			// 
			// floatTrackbarControlCloudSize
			// 
			this.floatTrackbarControlCloudSize.Location = new System.Drawing.Point( 105, 77 );
			this.floatTrackbarControlCloudSize.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudSize.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudSize.Name = "floatTrackbarControlCloudSize";
			this.floatTrackbarControlCloudSize.RangeMax = 1F;
			this.floatTrackbarControlCloudSize.RangeMin = 0F;
			this.floatTrackbarControlCloudSize.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudSize.TabIndex = 0;
			this.floatTrackbarControlCloudSize.Value = 0.38F;
			this.floatTrackbarControlCloudSize.VisibleRangeMax = 1F;
			this.floatTrackbarControlCloudSize.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudSize_ValueChanged );
			// 
			// floatTrackbarControlCloudType
			// 
			this.floatTrackbarControlCloudType.Location = new System.Drawing.Point( 389, 33 );
			this.floatTrackbarControlCloudType.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudType.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudType.Name = "floatTrackbarControlCloudType";
			this.floatTrackbarControlCloudType.RangeMax = 1F;
			this.floatTrackbarControlCloudType.RangeMin = 0F;
			this.floatTrackbarControlCloudType.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudType.TabIndex = 0;
			this.floatTrackbarControlCloudType.Value = 0F;
			this.floatTrackbarControlCloudType.VisibleRangeMax = 1F;
			this.floatTrackbarControlCloudType.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudType_ValueChanged );
			// 
			// floatTrackbarControlCoverage
			// 
			this.floatTrackbarControlCoverage.Location = new System.Drawing.Point( 105, 33 );
			this.floatTrackbarControlCoverage.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCoverage.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCoverage.Name = "floatTrackbarControlCoverage";
			this.floatTrackbarControlCoverage.RangeMax = 1F;
			this.floatTrackbarControlCoverage.RangeMin = 0F;
			this.floatTrackbarControlCoverage.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCoverage.TabIndex = 0;
			this.floatTrackbarControlCoverage.Value = 0.5F;
			this.floatTrackbarControlCoverage.VisibleRangeMax = 1F;
			this.floatTrackbarControlCoverage.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCoverage_ValueChanged );
			// 
			// floatTrackbarControlSunElevation
			// 
			this.floatTrackbarControlSunElevation.Location = new System.Drawing.Point( 105, 11 );
			this.floatTrackbarControlSunElevation.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSunElevation.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSunElevation.Name = "floatTrackbarControlSunElevation";
			this.floatTrackbarControlSunElevation.RangeMax = 180F;
			this.floatTrackbarControlSunElevation.RangeMin = 0F;
			this.floatTrackbarControlSunElevation.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSunElevation.TabIndex = 0;
			this.floatTrackbarControlSunElevation.Value = 0F;
			this.floatTrackbarControlSunElevation.VisibleRangeMax = 110F;
			this.floatTrackbarControlSunElevation.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlSunElevation_ValueChanged );
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 317, 14 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 66, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Sun Azimuth";
			// 
			// floatTrackbarControlSunAzimuth
			// 
			this.floatTrackbarControlSunAzimuth.Location = new System.Drawing.Point( 389, 11 );
			this.floatTrackbarControlSunAzimuth.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSunAzimuth.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSunAzimuth.Name = "floatTrackbarControlSunAzimuth";
			this.floatTrackbarControlSunAzimuth.RangeMax = 180F;
			this.floatTrackbarControlSunAzimuth.RangeMin = -180F;
			this.floatTrackbarControlSunAzimuth.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSunAzimuth.TabIndex = 0;
			this.floatTrackbarControlSunAzimuth.Value = 0F;
			this.floatTrackbarControlSunAzimuth.VisibleRangeMax = 180F;
			this.floatTrackbarControlSunAzimuth.VisibleRangeMin = -180F;
			this.floatTrackbarControlSunAzimuth.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlSunAzimuth_ValueChanged );
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Right;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 623, 0 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 297, 107 );
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 920, 606 );
			this.panelOutput.TabIndex = 0;
			this.panelOutput.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler( this.panelOutput_PreviewKeyDown );
			// 
			// DemoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1194, 713 );
			this.Controls.Add( this.panelOutput );
			this.Controls.Add( this.panelErrors );
			this.Controls.Add( this.panelProperties );
			this.Name = "DemoForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Clouds Demo";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
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
		private Nuaj.Cirrus.Utility.LogTextBox richTextBoxOutput;
		private System.Windows.Forms.Panel panelBars;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunAzimuth;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunElevation;
		private System.Windows.Forms.Label label3;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCoverage;
		private System.Windows.Forms.Label label4;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudType;
		private System.Windows.Forms.Label label5;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudSize;
		private System.Windows.Forms.Label label6;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlFogAmount;
		private System.Windows.Forms.Label label7;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlAerosolsAmount;
		private System.Windows.Forms.Label label8;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudAltitude;
	}
}

