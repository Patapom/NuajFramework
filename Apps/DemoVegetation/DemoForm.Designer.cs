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
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.labelHoveredObject = new System.Windows.Forms.Label();
			this.splitterProperties = new System.Windows.Forms.Splitter();
			this.treeViewObjects = new System.Windows.Forms.TreeView();
			this.buttonProfiling = new System.Windows.Forms.Button();
			this.panelParameters = new System.Windows.Forms.Panel();
			this.radioButton4 = new System.Windows.Forms.RadioButton();
			this.radioButton3 = new System.Windows.Forms.RadioButton();
			this.radioButton2 = new System.Windows.Forms.RadioButton();
			this.radioButton1 = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarControlSunTheta = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudSmoothness = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudNormalAmplitude = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudExtinction = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudAmplitudeFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudFrequencyFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudSpeed = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudDensity = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlAltitude = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudNoiseSize = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudThickness = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSunPhi = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.panelOutput = new Demo.OutputPanel( this.components );
			this.richTextBoxOutput = new Demo.LogTextBox( this.components );
			this.panelProperties.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panelParameters.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelProperties
			// 
			this.panelProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelProperties.Controls.Add( this.propertyGrid );
			this.panelProperties.Controls.Add( this.richTextBoxOutput );
			this.panelProperties.Controls.Add( this.panel1 );
			this.panelProperties.Controls.Add( this.splitterProperties );
			this.panelProperties.Controls.Add( this.treeViewObjects );
			this.panelProperties.Controls.Add( this.buttonProfiling );
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelProperties.Location = new System.Drawing.Point( 941, 0 );
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size( 274, 780 );
			this.panelProperties.TabIndex = 1;
			// 
			// propertyGrid
			// 
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.Location = new System.Drawing.Point( 0, 275 );
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size( 272, 334 );
			this.propertyGrid.TabIndex = 1;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add( this.label1 );
			this.panel1.Controls.Add( this.labelHoveredObject );
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point( 0, 712 );
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size( 272, 43 );
			this.panel1.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.label1.Location = new System.Drawing.Point( 0, 4 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 270, 13 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Use \"Control\" to manipulate the hovered object";
			// 
			// labelHoveredObject
			// 
			this.labelHoveredObject.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.labelHoveredObject.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.labelHoveredObject.Location = new System.Drawing.Point( 0, 17 );
			this.labelHoveredObject.Name = "labelHoveredObject";
			this.labelHoveredObject.Size = new System.Drawing.Size( 270, 24 );
			this.labelHoveredObject.TabIndex = 2;
			this.labelHoveredObject.Text = "No hovered object...";
			this.labelHoveredObject.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
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
			// buttonProfiling
			// 
			this.buttonProfiling.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.buttonProfiling.Location = new System.Drawing.Point( 0, 755 );
			this.buttonProfiling.Name = "buttonProfiling";
			this.buttonProfiling.Size = new System.Drawing.Size( 272, 23 );
			this.buttonProfiling.TabIndex = 5;
			this.buttonProfiling.Text = "Show Profiler";
			this.buttonProfiling.UseVisualStyleBackColor = true;
			this.buttonProfiling.Click += new System.EventHandler( this.buttonProfiling_Click );
			// 
			// panelParameters
			// 
			this.panelParameters.Controls.Add( this.radioButton4 );
			this.panelParameters.Controls.Add( this.radioButton3 );
			this.panelParameters.Controls.Add( this.radioButton2 );
			this.panelParameters.Controls.Add( this.radioButton1 );
			this.panelParameters.Controls.Add( this.label3 );
			this.panelParameters.Controls.Add( this.label8 );
			this.panelParameters.Controls.Add( this.label7 );
			this.panelParameters.Controls.Add( this.label6 );
			this.panelParameters.Controls.Add( this.label12 );
			this.panelParameters.Controls.Add( this.label11 );
			this.panelParameters.Controls.Add( this.label13 );
			this.panelParameters.Controls.Add( this.label5 );
			this.panelParameters.Controls.Add( this.label9 );
			this.panelParameters.Controls.Add( this.label10 );
			this.panelParameters.Controls.Add( this.label4 );
			this.panelParameters.Controls.Add( this.label2 );
			this.panelParameters.Controls.Add( this.floatTrackbarControlSunTheta );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudSmoothness );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudNormalAmplitude );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudExtinction );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudAmplitudeFactor );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudFrequencyFactor );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudSpeed );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudDensity );
			this.panelParameters.Controls.Add( this.floatTrackbarControlAltitude );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudNoiseSize );
			this.panelParameters.Controls.Add( this.floatTrackbarControlCloudThickness );
			this.panelParameters.Controls.Add( this.floatTrackbarControlSunPhi );
			this.panelParameters.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelParameters.Location = new System.Drawing.Point( 0, 660 );
			this.panelParameters.Name = "panelParameters";
			this.panelParameters.Size = new System.Drawing.Size( 941, 120 );
			this.panelParameters.TabIndex = 1;
			// 
			// radioButton4
			// 
			this.radioButton4.AutoSize = true;
			this.radioButton4.Enabled = false;
			this.radioButton4.Location = new System.Drawing.Point( 283, 63 );
			this.radioButton4.Name = "radioButton4";
			this.radioButton4.Size = new System.Drawing.Size( 60, 17 );
			this.radioButton4.TabIndex = 2;
			this.radioButton4.Text = "Layer 3";
			this.radioButton4.UseVisualStyleBackColor = true;
			this.radioButton4.CheckedChanged += new System.EventHandler( this.radioButton1_CheckedChanged );
			// 
			// radioButton3
			// 
			this.radioButton3.AutoSize = true;
			this.radioButton3.Enabled = false;
			this.radioButton3.Location = new System.Drawing.Point( 283, 44 );
			this.radioButton3.Name = "radioButton3";
			this.radioButton3.Size = new System.Drawing.Size( 60, 17 );
			this.radioButton3.TabIndex = 2;
			this.radioButton3.Text = "Layer 2";
			this.radioButton3.UseVisualStyleBackColor = true;
			this.radioButton3.CheckedChanged += new System.EventHandler( this.radioButton1_CheckedChanged );
			// 
			// radioButton2
			// 
			this.radioButton2.AutoSize = true;
			this.radioButton2.Enabled = false;
			this.radioButton2.Location = new System.Drawing.Point( 283, 26 );
			this.radioButton2.Name = "radioButton2";
			this.radioButton2.Size = new System.Drawing.Size( 60, 17 );
			this.radioButton2.TabIndex = 2;
			this.radioButton2.Text = "Layer 1";
			this.radioButton2.UseVisualStyleBackColor = true;
			this.radioButton2.CheckedChanged += new System.EventHandler( this.radioButton1_CheckedChanged );
			// 
			// radioButton1
			// 
			this.radioButton1.AutoSize = true;
			this.radioButton1.Enabled = false;
			this.radioButton1.Location = new System.Drawing.Point( 283, 8 );
			this.radioButton1.Name = "radioButton1";
			this.radioButton1.Size = new System.Drawing.Size( 60, 17 );
			this.radioButton1.TabIndex = 2;
			this.radioButton1.Text = "Layer 0";
			this.radioButton1.UseVisualStyleBackColor = true;
			this.radioButton1.CheckedChanged += new System.EventHandler( this.radioButton1_CheckedChanged );
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 12, 30 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 57, 13 );
			this.label3.TabIndex = 1;
			this.label3.Text = "Sun Theta";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point( 349, 103 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 65, 13 );
			this.label8.TabIndex = 1;
			this.label8.Text = "Smoothness";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point( 349, 79 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 67, 13 );
			this.label7.TabIndex = 1;
			this.label7.Text = "Normal Amp.";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 624, 57 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 55, 13 );
			this.label6.TabIndex = 1;
			this.label6.Text = "Scattering";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point( 349, 57 );
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size( 66, 13 );
			this.label12.TabIndex = 1;
			this.label12.Text = "Ampl. Factor";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point( 349, 34 );
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size( 64, 13 );
			this.label11.TabIndex = 1;
			this.label11.Text = "Freq. Factor";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point( 624, 103 );
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size( 38, 13 );
			this.label13.TabIndex = 1;
			this.label13.Text = "Speed";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 624, 81 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 42, 13 );
			this.label5.TabIndex = 1;
			this.label5.Text = "Density";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point( 624, 10 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 42, 13 );
			this.label9.TabIndex = 1;
			this.label9.Text = "Altitude";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point( 349, 9 );
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size( 27, 13 );
			this.label10.TabIndex = 1;
			this.label10.Text = "Size";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 624, 34 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 56, 13 );
			this.label4.TabIndex = 1;
			this.label4.Text = "Thickness";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 12, 10 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 44, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Sun Phi";
			// 
			// floatTrackbarControlSunTheta
			// 
			this.floatTrackbarControlSunTheta.Location = new System.Drawing.Point( 77, 29 );
			this.floatTrackbarControlSunTheta.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSunTheta.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSunTheta.Name = "floatTrackbarControlSunTheta";
			this.floatTrackbarControlSunTheta.RangeMax = 180F;
			this.floatTrackbarControlSunTheta.RangeMin = 0F;
			this.floatTrackbarControlSunTheta.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSunTheta.TabIndex = 0;
			this.floatTrackbarControlSunTheta.Value = 0F;
			this.floatTrackbarControlSunTheta.VisibleRangeMax = 180F;
			this.floatTrackbarControlSunTheta.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlSunTheta_ValueChanged );
			// 
			// floatTrackbarControlCloudSmoothness
			// 
			this.floatTrackbarControlCloudSmoothness.Location = new System.Drawing.Point( 414, 99 );
			this.floatTrackbarControlCloudSmoothness.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudSmoothness.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudSmoothness.Name = "floatTrackbarControlCloudSmoothness";
			this.floatTrackbarControlCloudSmoothness.RangeMax = 8F;
			this.floatTrackbarControlCloudSmoothness.RangeMin = 0F;
			this.floatTrackbarControlCloudSmoothness.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudSmoothness.TabIndex = 0;
			this.floatTrackbarControlCloudSmoothness.Value = 4F;
			this.floatTrackbarControlCloudSmoothness.VisibleRangeMax = 8F;
			this.floatTrackbarControlCloudSmoothness.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudSmoothness_ValueChanged );
			// 
			// floatTrackbarControlCloudNormalAmplitude
			// 
			this.floatTrackbarControlCloudNormalAmplitude.Location = new System.Drawing.Point( 414, 75 );
			this.floatTrackbarControlCloudNormalAmplitude.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudNormalAmplitude.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudNormalAmplitude.Name = "floatTrackbarControlCloudNormalAmplitude";
			this.floatTrackbarControlCloudNormalAmplitude.RangeMax = 10F;
			this.floatTrackbarControlCloudNormalAmplitude.RangeMin = 0F;
			this.floatTrackbarControlCloudNormalAmplitude.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudNormalAmplitude.TabIndex = 0;
			this.floatTrackbarControlCloudNormalAmplitude.Value = 0.5F;
			this.floatTrackbarControlCloudNormalAmplitude.VisibleRangeMax = 2F;
			this.floatTrackbarControlCloudNormalAmplitude.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudNormalAmplitude_ValueChanged );
			// 
			// floatTrackbarControlCloudExtinction
			// 
			this.floatTrackbarControlCloudExtinction.Location = new System.Drawing.Point( 689, 53 );
			this.floatTrackbarControlCloudExtinction.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudExtinction.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudExtinction.Name = "floatTrackbarControlCloudExtinction";
			this.floatTrackbarControlCloudExtinction.RangeMax = 1F;
			this.floatTrackbarControlCloudExtinction.RangeMin = 0F;
			this.floatTrackbarControlCloudExtinction.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudExtinction.TabIndex = 0;
			this.floatTrackbarControlCloudExtinction.Value = 0.02F;
			this.floatTrackbarControlCloudExtinction.VisibleRangeMax = 0.04F;
			this.floatTrackbarControlCloudExtinction.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudExtinction_ValueChanged );
			// 
			// floatTrackbarControlCloudAmplitudeFactor
			// 
			this.floatTrackbarControlCloudAmplitudeFactor.Location = new System.Drawing.Point( 414, 53 );
			this.floatTrackbarControlCloudAmplitudeFactor.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudAmplitudeFactor.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudAmplitudeFactor.Name = "floatTrackbarControlCloudAmplitudeFactor";
			this.floatTrackbarControlCloudAmplitudeFactor.RangeMax = 10F;
			this.floatTrackbarControlCloudAmplitudeFactor.RangeMin = -10F;
			this.floatTrackbarControlCloudAmplitudeFactor.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudAmplitudeFactor.TabIndex = 0;
			this.floatTrackbarControlCloudAmplitudeFactor.Value = 1F;
			this.floatTrackbarControlCloudAmplitudeFactor.VisibleRangeMax = 2F;
			this.floatTrackbarControlCloudAmplitudeFactor.VisibleRangeMin = -2F;
			this.floatTrackbarControlCloudAmplitudeFactor.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudFrequencyFactorAnisotropy_ValueChanged );
			// 
			// floatTrackbarControlCloudFrequencyFactor
			// 
			this.floatTrackbarControlCloudFrequencyFactor.Location = new System.Drawing.Point( 414, 30 );
			this.floatTrackbarControlCloudFrequencyFactor.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudFrequencyFactor.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudFrequencyFactor.Name = "floatTrackbarControlCloudFrequencyFactor";
			this.floatTrackbarControlCloudFrequencyFactor.RangeMax = 4F;
			this.floatTrackbarControlCloudFrequencyFactor.RangeMin = -4F;
			this.floatTrackbarControlCloudFrequencyFactor.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudFrequencyFactor.TabIndex = 0;
			this.floatTrackbarControlCloudFrequencyFactor.Value = 1.337F;
			this.floatTrackbarControlCloudFrequencyFactor.VisibleRangeMax = 2F;
			this.floatTrackbarControlCloudFrequencyFactor.VisibleRangeMin = -2F;
			this.floatTrackbarControlCloudFrequencyFactor.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudFrequencyFactor_ValueChanged );
			// 
			// floatTrackbarControlCloudSpeed
			// 
			this.floatTrackbarControlCloudSpeed.Location = new System.Drawing.Point( 689, 99 );
			this.floatTrackbarControlCloudSpeed.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudSpeed.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudSpeed.Name = "floatTrackbarControlCloudSpeed";
			this.floatTrackbarControlCloudSpeed.RangeMax = 10F;
			this.floatTrackbarControlCloudSpeed.RangeMin = -10F;
			this.floatTrackbarControlCloudSpeed.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudSpeed.TabIndex = 0;
			this.floatTrackbarControlCloudSpeed.Value = 1F;
			this.floatTrackbarControlCloudSpeed.VisibleRangeMin = -1F;
			this.floatTrackbarControlCloudSpeed.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudSpeed_ValueChanged );
			// 
			// floatTrackbarControlCloudDensity
			// 
			this.floatTrackbarControlCloudDensity.Location = new System.Drawing.Point( 689, 77 );
			this.floatTrackbarControlCloudDensity.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudDensity.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudDensity.Name = "floatTrackbarControlCloudDensity";
			this.floatTrackbarControlCloudDensity.RangeMax = 1F;
			this.floatTrackbarControlCloudDensity.RangeMin = -1F;
			this.floatTrackbarControlCloudDensity.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudDensity.TabIndex = 0;
			this.floatTrackbarControlCloudDensity.Value = 0F;
			this.floatTrackbarControlCloudDensity.VisibleRangeMax = 1F;
			this.floatTrackbarControlCloudDensity.VisibleRangeMin = -1F;
			this.floatTrackbarControlCloudDensity.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudDensity_ValueChanged );
			// 
			// floatTrackbarControlAltitude
			// 
			this.floatTrackbarControlAltitude.Location = new System.Drawing.Point( 689, 6 );
			this.floatTrackbarControlAltitude.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlAltitude.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlAltitude.Name = "floatTrackbarControlAltitude";
			this.floatTrackbarControlAltitude.RangeMax = 100F;
			this.floatTrackbarControlAltitude.RangeMin = 0F;
			this.floatTrackbarControlAltitude.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlAltitude.TabIndex = 0;
			this.floatTrackbarControlAltitude.Value = 10F;
			this.floatTrackbarControlAltitude.VisibleRangeMax = 20F;
			this.floatTrackbarControlAltitude.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlAltitude_ValueChanged );
			// 
			// floatTrackbarControlCloudNoiseSize
			// 
			this.floatTrackbarControlCloudNoiseSize.Location = new System.Drawing.Point( 414, 5 );
			this.floatTrackbarControlCloudNoiseSize.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudNoiseSize.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudNoiseSize.Name = "floatTrackbarControlCloudNoiseSize";
			this.floatTrackbarControlCloudNoiseSize.RangeMax = 2F;
			this.floatTrackbarControlCloudNoiseSize.RangeMin = 0F;
			this.floatTrackbarControlCloudNoiseSize.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudNoiseSize.TabIndex = 0;
			this.floatTrackbarControlCloudNoiseSize.Value = 0.8F;
			this.floatTrackbarControlCloudNoiseSize.VisibleRangeMax = 2F;
			this.floatTrackbarControlCloudNoiseSize.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudNoiseSize_ValueChanged );
			// 
			// floatTrackbarControlCloudThickness
			// 
			this.floatTrackbarControlCloudThickness.Location = new System.Drawing.Point( 689, 30 );
			this.floatTrackbarControlCloudThickness.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlCloudThickness.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlCloudThickness.Name = "floatTrackbarControlCloudThickness";
			this.floatTrackbarControlCloudThickness.RangeMax = 10F;
			this.floatTrackbarControlCloudThickness.RangeMin = 0F;
			this.floatTrackbarControlCloudThickness.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlCloudThickness.TabIndex = 0;
			this.floatTrackbarControlCloudThickness.Value = 0.1F;
			this.floatTrackbarControlCloudThickness.VisibleRangeMax = 1F;
			this.floatTrackbarControlCloudThickness.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlCloudThickness_ValueChanged );
			// 
			// floatTrackbarControlSunPhi
			// 
			this.floatTrackbarControlSunPhi.Location = new System.Drawing.Point( 77, 6 );
			this.floatTrackbarControlSunPhi.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSunPhi.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSunPhi.Name = "floatTrackbarControlSunPhi";
			this.floatTrackbarControlSunPhi.RangeMax = 180F;
			this.floatTrackbarControlSunPhi.RangeMin = -180F;
			this.floatTrackbarControlSunPhi.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSunPhi.TabIndex = 0;
			this.floatTrackbarControlSunPhi.Value = 0F;
			this.floatTrackbarControlSunPhi.VisibleRangeMax = 180F;
			this.floatTrackbarControlSunPhi.VisibleRangeMin = -180F;
			this.floatTrackbarControlSunPhi.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlSunPhi_ValueChanged );
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 941, 660 );
			this.panelOutput.TabIndex = 0;
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 0, 609 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 272, 103 );
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
			// 
			// DemoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1215, 780 );
			this.Controls.Add( this.panelOutput );
			this.Controls.Add( this.panelParameters );
			this.Controls.Add( this.panelProperties );
			this.Name = "DemoForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Vegetation Demo";
			this.panelProperties.ResumeLayout( false );
			this.panel1.ResumeLayout( false );
			this.panelParameters.ResumeLayout( false );
			this.panelParameters.PerformLayout();
			this.ResumeLayout( false );

		}

		#endregion

		private OutputPanel panelOutput;
		private System.Windows.Forms.Panel panelProperties;
		private System.Windows.Forms.PropertyGrid propertyGrid;
		private System.Windows.Forms.Splitter splitterProperties;
		private System.Windows.Forms.TreeView treeViewObjects;
		private System.Windows.Forms.Panel panelParameters;
		private LogTextBox richTextBoxOutput;
		private System.Windows.Forms.Label labelHoveredObject;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonProfiling;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunPhi;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunTheta;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudExtinction;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudDensity;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudThickness;
		private System.Windows.Forms.Label label7;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudNormalAmplitude;
		private System.Windows.Forms.Label label8;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudSmoothness;
		private System.Windows.Forms.RadioButton radioButton4;
		private System.Windows.Forms.RadioButton radioButton3;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.Label label9;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlAltitude;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudFrequencyFactor;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudNoiseSize;
		private System.Windows.Forms.Label label12;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudAmplitudeFactor;
		private System.Windows.Forms.Label label13;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudSpeed;
	}
}

