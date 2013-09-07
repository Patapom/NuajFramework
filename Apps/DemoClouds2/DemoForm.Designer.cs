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
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DemoForm));
			this.panelProperties = new System.Windows.Forms.Panel();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.richTextBoxOutput = new Nuaj.Cirrus.Utility.LogTextBox(this.components);
			this.splitterProperties = new System.Windows.Forms.Splitter();
			this.treeViewObjects = new System.Windows.Forms.TreeView();
			this.panelControl = new System.Windows.Forms.Panel();
			this.panelSettings1 = new System.Windows.Forms.Panel();
			this.buttonShadowMapViewer = new System.Windows.Forms.Button();
			this.buttonToneMappingSetup = new System.Windows.Forms.Button();
			this.buttonProfiler = new System.Windows.Forms.Button();
			this.buttonGoToPage2 = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.floatTrackbarControlAerosolsAmount = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlFogAmount = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label6 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarControlCloudAltitude = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudSize = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlShadowOpacity = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlIsotropicFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlDirectionalFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudAlbedo = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCloudDensity = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCoverageOffsetPow = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCoverageContrast = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCoverageOffsetBottom = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlCoverageOffsetTop = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSunElevation = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControlSunAzimuth = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label13 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label27 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label28 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.panelSettings2 = new System.Windows.Forms.Panel();
			this.radioButtonRGBRampInset = new System.Windows.Forms.RadioButton();
			this.radioButtonRGBRampFullscreen = new System.Windows.Forms.RadioButton();
			this.radioButtonDEBUGLuminanceCustom = new System.Windows.Forms.RadioButton();
			this.radioButtonDEBUGLuminanceNormalized = new System.Windows.Forms.RadioButton();
			this.radioButtonDEBUGNone = new System.Windows.Forms.RadioButton();
			this.panelSpectrum = new System.Windows.Forms.Panel();
			this.buttonRebuildSkyDensityLinear = new System.Windows.Forms.Button();
			this.buttonCloudProfiler = new System.Windows.Forms.Button();
			this.buttonRebuildSkyDensityExp = new System.Windows.Forms.Button();
			this.buttonGoToPage1 = new System.Windows.Forms.Button();
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlDEBUGLuminanceMarker = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSkyDensityAltitudeOffset = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlWavelengthR = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlWavelengthG = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlWavelengthB = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSkyDensityAerosolsFactor = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlToneMapAvgMax = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label18 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.label26 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.label25 = new System.Windows.Forms.Label();
			this.label23 = new System.Windows.Forms.Label();
			this.label24 = new System.Windows.Forms.Label();
			this.label19 = new System.Windows.Forms.Label();
			this.label21 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.panelOutput = new Demo.OutputPanel(this.components);
			this.panelProperties.SuspendLayout();
			this.panelControl.SuspendLayout();
			this.panelSettings1.SuspendLayout();
			this.panelSettings2.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelProperties
			// 
			this.panelProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelProperties.Controls.Add(this.propertyGrid);
			this.panelProperties.Controls.Add(this.richTextBoxOutput);
			this.panelProperties.Controls.Add(this.splitterProperties);
			this.panelProperties.Controls.Add(this.treeViewObjects);
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelProperties.Location = new System.Drawing.Point(1024, 0);
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size(274, 768);
			this.panelProperties.TabIndex = 1;
			// 
			// propertyGrid
			// 
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.Location = new System.Drawing.Point(0, 143);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(272, 517);
			this.propertyGrid.TabIndex = 2;
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.richTextBoxOutput.Location = new System.Drawing.Point(0, 660);
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size(272, 106);
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
			// 
			// splitterProperties
			// 
			this.splitterProperties.Dock = System.Windows.Forms.DockStyle.Top;
			this.splitterProperties.Location = new System.Drawing.Point(0, 140);
			this.splitterProperties.Name = "splitterProperties";
			this.splitterProperties.Size = new System.Drawing.Size(272, 3);
			this.splitterProperties.TabIndex = 1;
			this.splitterProperties.TabStop = false;
			// 
			// treeViewObjects
			// 
			this.treeViewObjects.Dock = System.Windows.Forms.DockStyle.Top;
			this.treeViewObjects.FullRowSelect = true;
			this.treeViewObjects.HideSelection = false;
			this.treeViewObjects.Location = new System.Drawing.Point(0, 0);
			this.treeViewObjects.Name = "treeViewObjects";
			this.treeViewObjects.Size = new System.Drawing.Size(272, 140);
			this.treeViewObjects.TabIndex = 0;
			this.treeViewObjects.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewObjects_AfterSelect);
			// 
			// panelControl
			// 
			this.panelControl.Controls.Add(this.panelSettings1);
			this.panelControl.Controls.Add(this.panelSettings2);
			this.panelControl.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelControl.Location = new System.Drawing.Point(0, 768);
			this.panelControl.Name = "panelControl";
			this.panelControl.Size = new System.Drawing.Size(1298, 107);
			this.panelControl.TabIndex = 1;
			// 
			// panelSettings1
			// 
			this.panelSettings1.Controls.Add(this.buttonShadowMapViewer);
			this.panelSettings1.Controls.Add(this.buttonToneMappingSetup);
			this.panelSettings1.Controls.Add(this.buttonProfiler);
			this.panelSettings1.Controls.Add(this.buttonGoToPage2);
			this.panelSettings1.Controls.Add(this.label7);
			this.panelSettings1.Controls.Add(this.label8);
			this.panelSettings1.Controls.Add(this.label11);
			this.panelSettings1.Controls.Add(this.label10);
			this.panelSettings1.Controls.Add(this.label9);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlAerosolsAmount);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlFogAmount);
			this.panelSettings1.Controls.Add(this.label6);
			this.panelSettings1.Controls.Add(this.label2);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCloudAltitude);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCloudSize);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlShadowOpacity);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlIsotropicFactor);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlDirectionalFactor);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCloudAlbedo);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCloudDensity);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCoverageOffsetPow);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCoverageContrast);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCoverageOffsetBottom);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlCoverageOffsetTop);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlSunElevation);
			this.panelSettings1.Controls.Add(this.label1);
			this.panelSettings1.Controls.Add(this.floatTrackbarControlSunAzimuth);
			this.panelSettings1.Controls.Add(this.label13);
			this.panelSettings1.Controls.Add(this.label5);
			this.panelSettings1.Controls.Add(this.label27);
			this.panelSettings1.Controls.Add(this.label12);
			this.panelSettings1.Controls.Add(this.label28);
			this.panelSettings1.Controls.Add(this.label3);
			this.panelSettings1.Controls.Add(this.label4);
			this.panelSettings1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelSettings1.Location = new System.Drawing.Point(0, 0);
			this.panelSettings1.Name = "panelSettings1";
			this.panelSettings1.Size = new System.Drawing.Size(1298, 107);
			this.panelSettings1.TabIndex = 3;
			// 
			// buttonShadowMapViewer
			// 
			this.buttonShadowMapViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonShadowMapViewer.Location = new System.Drawing.Point(1211, 50);
			this.buttonShadowMapViewer.Name = "buttonShadowMapViewer";
			this.buttonShadowMapViewer.Size = new System.Drawing.Size(75, 23);
			this.buttonShadowMapViewer.TabIndex = 2;
			this.buttonShadowMapViewer.Text = "Shadow";
			this.buttonShadowMapViewer.UseVisualStyleBackColor = true;
			this.buttonShadowMapViewer.Click += new System.EventHandler(this.buttonShadowMapViewer_Click);
			// 
			// buttonToneMappingSetup
			// 
			this.buttonToneMappingSetup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonToneMappingSetup.Location = new System.Drawing.Point(1211, 28);
			this.buttonToneMappingSetup.Name = "buttonToneMappingSetup";
			this.buttonToneMappingSetup.Size = new System.Drawing.Size(75, 23);
			this.buttonToneMappingSetup.TabIndex = 2;
			this.buttonToneMappingSetup.Text = "Tone Map";
			this.buttonToneMappingSetup.UseVisualStyleBackColor = true;
			this.buttonToneMappingSetup.Click += new System.EventHandler(this.buttonToneMappingSetup_Click);
			// 
			// buttonProfiler
			// 
			this.buttonProfiler.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonProfiler.Location = new System.Drawing.Point(1211, 6);
			this.buttonProfiler.Name = "buttonProfiler";
			this.buttonProfiler.Size = new System.Drawing.Size(75, 23);
			this.buttonProfiler.TabIndex = 2;
			this.buttonProfiler.Text = "Profiler";
			this.buttonProfiler.UseVisualStyleBackColor = true;
			this.buttonProfiler.Click += new System.EventHandler(this.buttonProfiler_Click);
			// 
			// buttonGoToPage2
			// 
			this.buttonGoToPage2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonGoToPage2.Location = new System.Drawing.Point(1211, 81);
			this.buttonGoToPage2.Name = "buttonGoToPage2";
			this.buttonGoToPage2.Size = new System.Drawing.Size(75, 23);
			this.buttonGoToPage2.TabIndex = 2;
			this.buttonGoToPage2.Text = "Page 2";
			this.buttonGoToPage2.UseVisualStyleBackColor = true;
			this.buttonGoToPage2.Click += new System.EventHandler(this.buttonGoToPage2_Click);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(303, 65);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(58, 13);
			this.label7.TabIndex = 1;
			this.label7.Text = "Air Amount";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(896, 60);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(65, 13);
			this.label8.TabIndex = 1;
			this.label8.Text = "Altitude (km)";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(595, 58);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(85, 13);
			this.label11.TabIndex = 1;
			this.label11.Text = "Shadow Opacity";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(595, 36);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(70, 13);
			this.label10.TabIndex = 1;
			this.label10.Text = "Cloud Albedo";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(595, 14);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(72, 13);
			this.label9.TabIndex = 1;
			this.label9.Text = "Cloud Density";
			// 
			// floatTrackbarControlAerosolsAmount
			// 
			this.floatTrackbarControlAerosolsAmount.Location = new System.Drawing.Point(389, 62);
			this.floatTrackbarControlAerosolsAmount.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlAerosolsAmount.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlAerosolsAmount.Name = "floatTrackbarControlAerosolsAmount";
			this.floatTrackbarControlAerosolsAmount.RangeMax = 100F;
			this.floatTrackbarControlAerosolsAmount.RangeMin = 0F;
			this.floatTrackbarControlAerosolsAmount.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlAerosolsAmount.TabIndex = 0;
			this.floatTrackbarControlAerosolsAmount.Value = 8F;
			this.floatTrackbarControlAerosolsAmount.VisibleRangeMax = 50F;
			this.floatTrackbarControlAerosolsAmount.VisibleRangeMin = 8F;
			this.floatTrackbarControlAerosolsAmount.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlAerosolsAmount_ValueChanged);
			// 
			// floatTrackbarControlFogAmount
			// 
			this.floatTrackbarControlFogAmount.Location = new System.Drawing.Point(389, 83);
			this.floatTrackbarControlFogAmount.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlFogAmount.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlFogAmount.Name = "floatTrackbarControlFogAmount";
			this.floatTrackbarControlFogAmount.RangeMax = 100F;
			this.floatTrackbarControlFogAmount.RangeMin = 0F;
			this.floatTrackbarControlFogAmount.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlFogAmount.TabIndex = 0;
			this.floatTrackbarControlFogAmount.Value = 2F;
			this.floatTrackbarControlFogAmount.VisibleRangeMax = 40F;
			this.floatTrackbarControlFogAmount.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlFogAmount_ValueChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(303, 86);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(64, 13);
			this.label6.TabIndex = 1;
			this.label6.Text = "Fog Amount";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 14);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(73, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Sun Elevation";
			// 
			// floatTrackbarControlCloudAltitude
			// 
			this.floatTrackbarControlCloudAltitude.Location = new System.Drawing.Point(982, 57);
			this.floatTrackbarControlCloudAltitude.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCloudAltitude.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCloudAltitude.Name = "floatTrackbarControlCloudAltitude";
			this.floatTrackbarControlCloudAltitude.RangeMax = 12F;
			this.floatTrackbarControlCloudAltitude.RangeMin = -8F;
			this.floatTrackbarControlCloudAltitude.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCloudAltitude.TabIndex = 0;
			this.floatTrackbarControlCloudAltitude.Value = 4F;
			this.floatTrackbarControlCloudAltitude.VisibleRangeMax = 12F;
			this.floatTrackbarControlCloudAltitude.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCloudAltitude_ValueChanged);
			// 
			// floatTrackbarControlCloudSize
			// 
			this.floatTrackbarControlCloudSize.Location = new System.Drawing.Point(982, 81);
			this.floatTrackbarControlCloudSize.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCloudSize.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCloudSize.Name = "floatTrackbarControlCloudSize";
			this.floatTrackbarControlCloudSize.RangeMax = 12F;
			this.floatTrackbarControlCloudSize.RangeMin = 0F;
			this.floatTrackbarControlCloudSize.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCloudSize.TabIndex = 0;
			this.floatTrackbarControlCloudSize.Value = 8F;
			this.floatTrackbarControlCloudSize.VisibleRangeMax = 12F;
			this.floatTrackbarControlCloudSize.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCloudSize_ValueChanged);
			// 
			// floatTrackbarControlShadowOpacity
			// 
			this.floatTrackbarControlShadowOpacity.Location = new System.Drawing.Point(682, 55);
			this.floatTrackbarControlShadowOpacity.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlShadowOpacity.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlShadowOpacity.Name = "floatTrackbarControlShadowOpacity";
			this.floatTrackbarControlShadowOpacity.RangeMax = 100F;
			this.floatTrackbarControlShadowOpacity.RangeMin = 0F;
			this.floatTrackbarControlShadowOpacity.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlShadowOpacity.TabIndex = 0;
			this.floatTrackbarControlShadowOpacity.Value = 8F;
			this.floatTrackbarControlShadowOpacity.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlDensitySumFactor_ValueChanged);
			// 
			// floatTrackbarControlIsotropicFactor
			// 
			this.floatTrackbarControlIsotropicFactor.Location = new System.Drawing.Point(982, 33);
			this.floatTrackbarControlIsotropicFactor.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlIsotropicFactor.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlIsotropicFactor.Name = "floatTrackbarControlIsotropicFactor";
			this.floatTrackbarControlIsotropicFactor.RangeMax = 10F;
			this.floatTrackbarControlIsotropicFactor.RangeMin = 0F;
			this.floatTrackbarControlIsotropicFactor.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlIsotropicFactor.TabIndex = 0;
			this.floatTrackbarControlIsotropicFactor.Value = 1F;
			this.floatTrackbarControlIsotropicFactor.VisibleRangeMax = 1F;
			this.floatTrackbarControlIsotropicFactor.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlIsotropicFactor_ValueChanged);
			// 
			// floatTrackbarControlDirectionalFactor
			// 
			this.floatTrackbarControlDirectionalFactor.Location = new System.Drawing.Point(982, 11);
			this.floatTrackbarControlDirectionalFactor.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlDirectionalFactor.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlDirectionalFactor.Name = "floatTrackbarControlDirectionalFactor";
			this.floatTrackbarControlDirectionalFactor.RangeMax = 10F;
			this.floatTrackbarControlDirectionalFactor.RangeMin = 0F;
			this.floatTrackbarControlDirectionalFactor.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlDirectionalFactor.TabIndex = 0;
			this.floatTrackbarControlDirectionalFactor.Value = 1F;
			this.floatTrackbarControlDirectionalFactor.VisibleRangeMax = 1F;
			this.floatTrackbarControlDirectionalFactor.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlDirectionalFactor_ValueChanged);
			// 
			// floatTrackbarControlCloudAlbedo
			// 
			this.floatTrackbarControlCloudAlbedo.Location = new System.Drawing.Point(682, 33);
			this.floatTrackbarControlCloudAlbedo.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCloudAlbedo.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCloudAlbedo.Name = "floatTrackbarControlCloudAlbedo";
			this.floatTrackbarControlCloudAlbedo.RangeMax = 1F;
			this.floatTrackbarControlCloudAlbedo.RangeMin = 0F;
			this.floatTrackbarControlCloudAlbedo.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCloudAlbedo.TabIndex = 0;
			this.floatTrackbarControlCloudAlbedo.Value = 0.9F;
			this.floatTrackbarControlCloudAlbedo.VisibleRangeMax = 1F;
			this.floatTrackbarControlCloudAlbedo.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCloudScattering_ValueChanged);
			// 
			// floatTrackbarControlCloudDensity
			// 
			this.floatTrackbarControlCloudDensity.Location = new System.Drawing.Point(682, 11);
			this.floatTrackbarControlCloudDensity.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCloudDensity.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCloudDensity.Name = "floatTrackbarControlCloudDensity";
			this.floatTrackbarControlCloudDensity.RangeMax = 20F;
			this.floatTrackbarControlCloudDensity.RangeMin = 0F;
			this.floatTrackbarControlCloudDensity.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCloudDensity.TabIndex = 0;
			this.floatTrackbarControlCloudDensity.Value = 1F;
			this.floatTrackbarControlCloudDensity.VisibleRangeMax = 2F;
			this.floatTrackbarControlCloudDensity.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCloudExtinction_ValueChanged);
			// 
			// floatTrackbarControlCoverageOffsetPow
			// 
			this.floatTrackbarControlCoverageOffsetPow.Location = new System.Drawing.Point(94, 77);
			this.floatTrackbarControlCoverageOffsetPow.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCoverageOffsetPow.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCoverageOffsetPow.Name = "floatTrackbarControlCoverageOffsetPow";
			this.floatTrackbarControlCoverageOffsetPow.RangeMax = 10F;
			this.floatTrackbarControlCoverageOffsetPow.RangeMin = -10F;
			this.floatTrackbarControlCoverageOffsetPow.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCoverageOffsetPow.TabIndex = 0;
			this.floatTrackbarControlCoverageOffsetPow.Value = 1F;
			this.floatTrackbarControlCoverageOffsetPow.VisibleRangeMax = 4F;
			this.floatTrackbarControlCoverageOffsetPow.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCoverageOffsetPow_ValueChanged);
			// 
			// floatTrackbarControlCoverageContrast
			// 
			this.floatTrackbarControlCoverageContrast.Location = new System.Drawing.Point(389, 33);
			this.floatTrackbarControlCoverageContrast.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCoverageContrast.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCoverageContrast.Name = "floatTrackbarControlCoverageContrast";
			this.floatTrackbarControlCoverageContrast.RangeMax = 10F;
			this.floatTrackbarControlCoverageContrast.RangeMin = 0F;
			this.floatTrackbarControlCoverageContrast.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCoverageContrast.TabIndex = 0;
			this.floatTrackbarControlCoverageContrast.Value = 0.2F;
			this.floatTrackbarControlCoverageContrast.VisibleRangeMax = 1F;
			this.floatTrackbarControlCoverageContrast.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCloudDensity_ValueChanged);
			// 
			// floatTrackbarControlCoverageOffsetBottom
			// 
			this.floatTrackbarControlCoverageOffsetBottom.Location = new System.Drawing.Point(94, 55);
			this.floatTrackbarControlCoverageOffsetBottom.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCoverageOffsetBottom.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCoverageOffsetBottom.Name = "floatTrackbarControlCoverageOffsetBottom";
			this.floatTrackbarControlCoverageOffsetBottom.RangeMax = 4F;
			this.floatTrackbarControlCoverageOffsetBottom.RangeMin = -4F;
			this.floatTrackbarControlCoverageOffsetBottom.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCoverageOffsetBottom.TabIndex = 0;
			this.floatTrackbarControlCoverageOffsetBottom.Value = -0.7F;
			this.floatTrackbarControlCoverageOffsetBottom.VisibleRangeMax = 1F;
			this.floatTrackbarControlCoverageOffsetBottom.VisibleRangeMin = -2F;
			this.floatTrackbarControlCoverageOffsetBottom.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCoverage_ValueChanged);
			// 
			// floatTrackbarControlCoverageOffsetTop
			// 
			this.floatTrackbarControlCoverageOffsetTop.Location = new System.Drawing.Point(94, 33);
			this.floatTrackbarControlCoverageOffsetTop.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlCoverageOffsetTop.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlCoverageOffsetTop.Name = "floatTrackbarControlCoverageOffsetTop";
			this.floatTrackbarControlCoverageOffsetTop.RangeMax = 4F;
			this.floatTrackbarControlCoverageOffsetTop.RangeMin = -4F;
			this.floatTrackbarControlCoverageOffsetTop.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlCoverageOffsetTop.TabIndex = 0;
			this.floatTrackbarControlCoverageOffsetTop.Value = 0F;
			this.floatTrackbarControlCoverageOffsetTop.VisibleRangeMax = 1F;
			this.floatTrackbarControlCoverageOffsetTop.VisibleRangeMin = -1F;
			this.floatTrackbarControlCoverageOffsetTop.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlCoverage_ValueChanged);
			// 
			// floatTrackbarControlSunElevation
			// 
			this.floatTrackbarControlSunElevation.Location = new System.Drawing.Point(94, 11);
			this.floatTrackbarControlSunElevation.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlSunElevation.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlSunElevation.Name = "floatTrackbarControlSunElevation";
			this.floatTrackbarControlSunElevation.RangeMax = 180F;
			this.floatTrackbarControlSunElevation.RangeMin = 0F;
			this.floatTrackbarControlSunElevation.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlSunElevation.TabIndex = 0;
			this.floatTrackbarControlSunElevation.Value = 33F;
			this.floatTrackbarControlSunElevation.VisibleRangeMax = 140F;
			this.floatTrackbarControlSunElevation.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlSunElevation_ValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(303, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(66, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Sun Azimuth";
			// 
			// floatTrackbarControlSunAzimuth
			// 
			this.floatTrackbarControlSunAzimuth.Location = new System.Drawing.Point(389, 11);
			this.floatTrackbarControlSunAzimuth.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlSunAzimuth.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlSunAzimuth.Name = "floatTrackbarControlSunAzimuth";
			this.floatTrackbarControlSunAzimuth.RangeMax = 180F;
			this.floatTrackbarControlSunAzimuth.RangeMin = -180F;
			this.floatTrackbarControlSunAzimuth.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlSunAzimuth.TabIndex = 0;
			this.floatTrackbarControlSunAzimuth.Value = 0F;
			this.floatTrackbarControlSunAzimuth.VisibleRangeMax = 180F;
			this.floatTrackbarControlSunAzimuth.VisibleRangeMin = -180F;
			this.floatTrackbarControlSunAzimuth.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlSunAzimuth_ValueChanged);
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(895, 36);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(80, 13);
			this.label13.TabIndex = 1;
			this.label13.Text = "Isotropic Factor";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(896, 84);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(79, 13);
			this.label5.TabIndex = 1;
			this.label5.Text = "Thickness (km)";
			// 
			// label27
			// 
			this.label27.AutoSize = true;
			this.label27.Location = new System.Drawing.Point(8, 58);
			this.label27.Name = "label27";
			this.label27.Size = new System.Drawing.Size(89, 13);
			this.label27.TabIndex = 1;
			this.label27.Text = "Coverage Bottom";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(895, 14);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(90, 13);
			this.label12.TabIndex = 1;
			this.label12.Text = "Directional Factor";
			// 
			// label28
			// 
			this.label28.AutoSize = true;
			this.label28.Location = new System.Drawing.Point(8, 80);
			this.label28.Name = "label28";
			this.label28.Size = new System.Drawing.Size(77, 13);
			this.label28.TabIndex = 1;
			this.label28.Text = "Coverage Pow";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 36);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(75, 13);
			this.label3.TabIndex = 1;
			this.label3.Text = "Coverage Top";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(303, 36);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(95, 13);
			this.label4.TabIndex = 1;
			this.label4.Text = "Coverage Contrast";
			// 
			// panelSettings2
			// 
			this.panelSettings2.Controls.Add(this.radioButtonRGBRampInset);
			this.panelSettings2.Controls.Add(this.radioButtonRGBRampFullscreen);
			this.panelSettings2.Controls.Add(this.radioButtonDEBUGLuminanceCustom);
			this.panelSettings2.Controls.Add(this.radioButtonDEBUGLuminanceNormalized);
			this.panelSettings2.Controls.Add(this.radioButtonDEBUGNone);
			this.panelSettings2.Controls.Add(this.panelSpectrum);
			this.panelSettings2.Controls.Add(this.buttonRebuildSkyDensityLinear);
			this.panelSettings2.Controls.Add(this.buttonCloudProfiler);
			this.panelSettings2.Controls.Add(this.buttonRebuildSkyDensityExp);
			this.panelSettings2.Controls.Add(this.buttonGoToPage1);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlDEBUGLuminanceMarkerTolerance);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlDEBUGLuminanceMarker);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlSkyDensityAltitudeOffset);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlWavelengthR);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlWavelengthG);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlWavelengthB);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlSkyDensityAerosolsFactor);
			this.panelSettings2.Controls.Add(this.floatTrackbarControlToneMapAvgMax);
			this.panelSettings2.Controls.Add(this.label18);
			this.panelSettings2.Controls.Add(this.label17);
			this.panelSettings2.Controls.Add(this.label16);
			this.panelSettings2.Controls.Add(this.label20);
			this.panelSettings2.Controls.Add(this.label26);
			this.panelSettings2.Controls.Add(this.label15);
			this.panelSettings2.Controls.Add(this.label25);
			this.panelSettings2.Controls.Add(this.label23);
			this.panelSettings2.Controls.Add(this.label24);
			this.panelSettings2.Controls.Add(this.label19);
			this.panelSettings2.Controls.Add(this.label21);
			this.panelSettings2.Controls.Add(this.label14);
			this.panelSettings2.Controls.Add(this.label22);
			this.panelSettings2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelSettings2.Location = new System.Drawing.Point(0, 0);
			this.panelSettings2.Name = "panelSettings2";
			this.panelSettings2.Size = new System.Drawing.Size(1298, 107);
			this.panelSettings2.TabIndex = 4;
			this.panelSettings2.Visible = false;
			// 
			// radioButtonRGBRampInset
			// 
			this.radioButtonRGBRampInset.AutoSize = true;
			this.radioButtonRGBRampInset.Location = new System.Drawing.Point(793, 8);
			this.radioButtonRGBRampInset.Name = "radioButtonRGBRampInset";
			this.radioButtonRGBRampInset.Size = new System.Drawing.Size(74, 17);
			this.radioButtonRGBRampInset.TabIndex = 4;
			this.radioButtonRGBRampInset.Text = "RGB Inset";
			this.radioButtonRGBRampInset.UseVisualStyleBackColor = true;
			this.radioButtonRGBRampInset.CheckedChanged += new System.EventHandler(this.radioButtonRGBRampInset_CheckedChanged);
			// 
			// radioButtonRGBRampFullscreen
			// 
			this.radioButtonRGBRampFullscreen.AutoSize = true;
			this.radioButtonRGBRampFullscreen.Location = new System.Drawing.Point(705, 8);
			this.radioButtonRGBRampFullscreen.Name = "radioButtonRGBRampFullscreen";
			this.radioButtonRGBRampFullscreen.Size = new System.Drawing.Size(99, 17);
			this.radioButtonRGBRampFullscreen.TabIndex = 4;
			this.radioButtonRGBRampFullscreen.Text = "RGB Fullscreen";
			this.radioButtonRGBRampFullscreen.UseVisualStyleBackColor = true;
			this.radioButtonRGBRampFullscreen.CheckedChanged += new System.EventHandler(this.radioButtonRGBRampFullscreen_CheckedChanged);
			// 
			// radioButtonDEBUGLuminanceCustom
			// 
			this.radioButtonDEBUGLuminanceCustom.AutoSize = true;
			this.radioButtonDEBUGLuminanceCustom.Location = new System.Drawing.Point(613, 8);
			this.radioButtonDEBUGLuminanceCustom.Name = "radioButtonDEBUGLuminanceCustom";
			this.radioButtonDEBUGLuminanceCustom.Size = new System.Drawing.Size(86, 17);
			this.radioButtonDEBUGLuminanceCustom.TabIndex = 4;
			this.radioButtonDEBUGLuminanceCustom.Text = "Lum. Custom";
			this.radioButtonDEBUGLuminanceCustom.UseVisualStyleBackColor = true;
			this.radioButtonDEBUGLuminanceCustom.CheckedChanged += new System.EventHandler(this.radioButtonDEBUGLuminanceCustom_CheckedChanged);
			// 
			// radioButtonDEBUGLuminanceNormalized
			// 
			this.radioButtonDEBUGLuminanceNormalized.AutoSize = true;
			this.radioButtonDEBUGLuminanceNormalized.Location = new System.Drawing.Point(530, 8);
			this.radioButtonDEBUGLuminanceNormalized.Name = "radioButtonDEBUGLuminanceNormalized";
			this.radioButtonDEBUGLuminanceNormalized.Size = new System.Drawing.Size(77, 17);
			this.radioButtonDEBUGLuminanceNormalized.TabIndex = 4;
			this.radioButtonDEBUGLuminanceNormalized.Text = "Luminance";
			this.radioButtonDEBUGLuminanceNormalized.UseVisualStyleBackColor = true;
			this.radioButtonDEBUGLuminanceNormalized.CheckedChanged += new System.EventHandler(this.radioButtonDEBUGLuminanceNormalized_CheckedChanged);
			// 
			// radioButtonDEBUGNone
			// 
			this.radioButtonDEBUGNone.AutoSize = true;
			this.radioButtonDEBUGNone.Checked = true;
			this.radioButtonDEBUGNone.Location = new System.Drawing.Point(450, 8);
			this.radioButtonDEBUGNone.Name = "radioButtonDEBUGNone";
			this.radioButtonDEBUGNone.Size = new System.Drawing.Size(74, 17);
			this.radioButtonDEBUGNone.TabIndex = 4;
			this.radioButtonDEBUGNone.TabStop = true;
			this.radioButtonDEBUGNone.Text = "No Debug";
			this.radioButtonDEBUGNone.UseVisualStyleBackColor = true;
			this.radioButtonDEBUGNone.CheckedChanged += new System.EventHandler(this.radioButtonDEBUGNone_CheckedChanged);
			// 
			// panelSpectrum
			// 
			this.panelSpectrum.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelSpectrum.BackgroundImage")));
			this.panelSpectrum.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.panelSpectrum.Location = new System.Drawing.Point(10, 6);
			this.panelSpectrum.Name = "panelSpectrum";
			this.panelSpectrum.Size = new System.Drawing.Size(434, 19);
			this.panelSpectrum.TabIndex = 3;
			// 
			// buttonRebuildSkyDensityLinear
			// 
			this.buttonRebuildSkyDensityLinear.Location = new System.Drawing.Point(1026, 80);
			this.buttonRebuildSkyDensityLinear.Name = "buttonRebuildSkyDensityLinear";
			this.buttonRebuildSkyDensityLinear.Size = new System.Drawing.Size(75, 23);
			this.buttonRebuildSkyDensityLinear.TabIndex = 2;
			this.buttonRebuildSkyDensityLinear.Text = "Rebuild Lin.";
			this.buttonRebuildSkyDensityLinear.UseVisualStyleBackColor = true;
			this.buttonRebuildSkyDensityLinear.Click += new System.EventHandler(this.buttonRebuildLinear_Click);
			// 
			// buttonCloudProfiler
			// 
			this.buttonCloudProfiler.Location = new System.Drawing.Point(1188, 3);
			this.buttonCloudProfiler.Name = "buttonCloudProfiler";
			this.buttonCloudProfiler.Size = new System.Drawing.Size(98, 23);
			this.buttonCloudProfiler.TabIndex = 2;
			this.buttonCloudProfiler.Text = "Shape Profiler";
			this.buttonCloudProfiler.UseVisualStyleBackColor = true;
			this.buttonCloudProfiler.Click += new System.EventHandler(this.buttonCloudProfiler_Click);
			// 
			// buttonRebuildSkyDensityExp
			// 
			this.buttonRebuildSkyDensityExp.Location = new System.Drawing.Point(1026, 56);
			this.buttonRebuildSkyDensityExp.Name = "buttonRebuildSkyDensityExp";
			this.buttonRebuildSkyDensityExp.Size = new System.Drawing.Size(75, 23);
			this.buttonRebuildSkyDensityExp.TabIndex = 2;
			this.buttonRebuildSkyDensityExp.Text = "Rebuild Exp.";
			this.buttonRebuildSkyDensityExp.UseVisualStyleBackColor = true;
			this.buttonRebuildSkyDensityExp.Click += new System.EventHandler(this.buttonRebuildSkyDensity_Click);
			// 
			// buttonGoToPage1
			// 
			this.buttonGoToPage1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonGoToPage1.Location = new System.Drawing.Point(1211, 81);
			this.buttonGoToPage1.Name = "buttonGoToPage1";
			this.buttonGoToPage1.Size = new System.Drawing.Size(75, 23);
			this.buttonGoToPage1.TabIndex = 2;
			this.buttonGoToPage1.Text = "Page 1";
			this.buttonGoToPage1.UseVisualStyleBackColor = true;
			this.buttonGoToPage1.Click += new System.EventHandler(this.buttonGoToPage1_Click);
			// 
			// floatTrackbarControlDEBUGLuminanceMarkerTolerance
			// 
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.Location = new System.Drawing.Point(925, 28);
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.Name = "floatTrackbarControlDEBUGLuminanceMarkerTolerance";
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.RangeMax = 10F;
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.RangeMin = 0F;
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.TabIndex = 0;
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.Value = 0.1F;
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.VisibleRangeMax = 1F;
			this.floatTrackbarControlDEBUGLuminanceMarkerTolerance.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlDEBUGLuminanceMarkerTolerance_ValueChanged);
			// 
			// floatTrackbarControlDEBUGLuminanceMarker
			// 
			this.floatTrackbarControlDEBUGLuminanceMarker.Location = new System.Drawing.Point(925, 5);
			this.floatTrackbarControlDEBUGLuminanceMarker.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlDEBUGLuminanceMarker.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlDEBUGLuminanceMarker.Name = "floatTrackbarControlDEBUGLuminanceMarker";
			this.floatTrackbarControlDEBUGLuminanceMarker.RangeMax = 1000F;
			this.floatTrackbarControlDEBUGLuminanceMarker.RangeMin = 0F;
			this.floatTrackbarControlDEBUGLuminanceMarker.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlDEBUGLuminanceMarker.TabIndex = 0;
			this.floatTrackbarControlDEBUGLuminanceMarker.Value = 50F;
			this.floatTrackbarControlDEBUGLuminanceMarker.VisibleRangeMax = 100F;
			this.floatTrackbarControlDEBUGLuminanceMarker.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlDEBUGLuminanceMarker_ValueChanged);
			// 
			// floatTrackbarControlSkyDensityAltitudeOffset
			// 
			this.floatTrackbarControlSkyDensityAltitudeOffset.Location = new System.Drawing.Point(820, 81);
			this.floatTrackbarControlSkyDensityAltitudeOffset.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlSkyDensityAltitudeOffset.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlSkyDensityAltitudeOffset.Name = "floatTrackbarControlSkyDensityAltitudeOffset";
			this.floatTrackbarControlSkyDensityAltitudeOffset.RangeMax = 20F;
			this.floatTrackbarControlSkyDensityAltitudeOffset.RangeMin = -20F;
			this.floatTrackbarControlSkyDensityAltitudeOffset.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlSkyDensityAltitudeOffset.TabIndex = 0;
			this.floatTrackbarControlSkyDensityAltitudeOffset.Value = -10F;
			this.floatTrackbarControlSkyDensityAltitudeOffset.VisibleRangeMax = 20F;
			this.floatTrackbarControlSkyDensityAltitudeOffset.VisibleRangeMin = -20F;
			// 
			// floatTrackbarControlWavelengthR
			// 
			this.floatTrackbarControlWavelengthR.Location = new System.Drawing.Point(530, 43);
			this.floatTrackbarControlWavelengthR.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlWavelengthR.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlWavelengthR.Name = "floatTrackbarControlWavelengthR";
			this.floatTrackbarControlWavelengthR.RangeMax = 750F;
			this.floatTrackbarControlWavelengthR.RangeMin = 350F;
			this.floatTrackbarControlWavelengthR.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlWavelengthR.TabIndex = 0;
			this.floatTrackbarControlWavelengthR.Value = 650F;
			this.floatTrackbarControlWavelengthR.VisibleRangeMax = 750F;
			this.floatTrackbarControlWavelengthR.VisibleRangeMin = 350F;
			this.floatTrackbarControlWavelengthR.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlWavelength_ValueChanged);
			// 
			// floatTrackbarControlWavelengthG
			// 
			this.floatTrackbarControlWavelengthG.Location = new System.Drawing.Point(530, 62);
			this.floatTrackbarControlWavelengthG.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlWavelengthG.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlWavelengthG.Name = "floatTrackbarControlWavelengthG";
			this.floatTrackbarControlWavelengthG.RangeMax = 750F;
			this.floatTrackbarControlWavelengthG.RangeMin = 350F;
			this.floatTrackbarControlWavelengthG.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlWavelengthG.TabIndex = 0;
			this.floatTrackbarControlWavelengthG.Value = 570F;
			this.floatTrackbarControlWavelengthG.VisibleRangeMax = 750F;
			this.floatTrackbarControlWavelengthG.VisibleRangeMin = 350F;
			this.floatTrackbarControlWavelengthG.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlWavelength_ValueChanged);
			// 
			// floatTrackbarControlWavelengthB
			// 
			this.floatTrackbarControlWavelengthB.Location = new System.Drawing.Point(530, 81);
			this.floatTrackbarControlWavelengthB.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlWavelengthB.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlWavelengthB.Name = "floatTrackbarControlWavelengthB";
			this.floatTrackbarControlWavelengthB.RangeMax = 750F;
			this.floatTrackbarControlWavelengthB.RangeMin = 350F;
			this.floatTrackbarControlWavelengthB.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlWavelengthB.TabIndex = 0;
			this.floatTrackbarControlWavelengthB.Value = 475F;
			this.floatTrackbarControlWavelengthB.VisibleRangeMax = 750F;
			this.floatTrackbarControlWavelengthB.VisibleRangeMin = 350F;
			this.floatTrackbarControlWavelengthB.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlWavelength_ValueChanged);
			// 
			// floatTrackbarControlSkyDensityAerosolsFactor
			// 
			this.floatTrackbarControlSkyDensityAerosolsFactor.Location = new System.Drawing.Point(820, 59);
			this.floatTrackbarControlSkyDensityAerosolsFactor.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlSkyDensityAerosolsFactor.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlSkyDensityAerosolsFactor.Name = "floatTrackbarControlSkyDensityAerosolsFactor";
			this.floatTrackbarControlSkyDensityAerosolsFactor.RangeMax = 10F;
			this.floatTrackbarControlSkyDensityAerosolsFactor.RangeMin = 0F;
			this.floatTrackbarControlSkyDensityAerosolsFactor.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlSkyDensityAerosolsFactor.TabIndex = 0;
			this.floatTrackbarControlSkyDensityAerosolsFactor.Value = 7F;
			// 
			// floatTrackbarControlToneMapAvgMax
			// 
			this.floatTrackbarControlToneMapAvgMax.Location = new System.Drawing.Point(92, 51);
			this.floatTrackbarControlToneMapAvgMax.MaximumSize = new System.Drawing.Size(10000, 20);
			this.floatTrackbarControlToneMapAvgMax.MinimumSize = new System.Drawing.Size(70, 20);
			this.floatTrackbarControlToneMapAvgMax.Name = "floatTrackbarControlToneMapAvgMax";
			this.floatTrackbarControlToneMapAvgMax.RangeMax = 1F;
			this.floatTrackbarControlToneMapAvgMax.RangeMin = 0F;
			this.floatTrackbarControlToneMapAvgMax.Size = new System.Drawing.Size(200, 20);
			this.floatTrackbarControlToneMapAvgMax.TabIndex = 0;
			this.floatTrackbarControlToneMapAvgMax.Value = 0.1F;
			this.floatTrackbarControlToneMapAvgMax.VisibleRangeMax = 1F;
			this.floatTrackbarControlToneMapAvgMax.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler(this.floatTrackbarControlToneMapAvgMax_ValueChanged);
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(323, 28);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(27, 13);
			this.label18.TabIndex = 1;
			this.label18.Text = "75%";
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Location = new System.Drawing.Point(214, 28);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(27, 13);
			this.label17.TabIndex = 1;
			this.label17.Text = "50%";
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(106, 28);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(27, 13);
			this.label16.TabIndex = 1;
			this.label16.Text = "25%";
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Location = new System.Drawing.Point(873, 32);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(55, 13);
			this.label20.TabIndex = 1;
			this.label20.Text = "Tolerance";
			// 
			// label26
			// 
			this.label26.AutoSize = true;
			this.label26.Location = new System.Drawing.Point(512, 46);
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size(15, 13);
			this.label26.TabIndex = 1;
			this.label26.Text = "R";
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(427, 28);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(33, 13);
			this.label15.TabIndex = 1;
			this.label15.Text = "100%";
			// 
			// label25
			// 
			this.label25.AutoSize = true;
			this.label25.Location = new System.Drawing.Point(512, 65);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(15, 13);
			this.label25.TabIndex = 1;
			this.label25.Text = "G";
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Location = new System.Drawing.Point(735, 84);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(73, 13);
			this.label23.TabIndex = 1;
			this.label23.Text = "Altitude Offset";
			// 
			// label24
			// 
			this.label24.AutoSize = true;
			this.label24.Location = new System.Drawing.Point(512, 84);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(14, 13);
			this.label24.TabIndex = 1;
			this.label24.Text = "B";
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point(873, 10);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(40, 13);
			this.label19.TabIndex = 1;
			this.label19.Text = "Marker";
			// 
			// label21
			// 
			this.label21.AutoSize = true;
			this.label21.Location = new System.Drawing.Point(735, 62);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(80, 13);
			this.label21.TabIndex = 1;
			this.label21.Text = "Aerosols Factor";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(1, 28);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(21, 13);
			this.label14.TabIndex = 1;
			this.label14.Text = "0%";
			// 
			// label22
			// 
			this.label22.AutoSize = true;
			this.label22.Location = new System.Drawing.Point(7, 54);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(80, 13);
			this.label22.TabIndex = 1;
			this.label22.Text = "Tone Avg. Max";
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point(0, 0);
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size(1024, 768);
			this.panelOutput.TabIndex = 0;
			this.panelOutput.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.panelOutput_PreviewKeyDown);
			// 
			// DemoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1298, 875);
			this.Controls.Add(this.panelOutput);
			this.Controls.Add(this.panelProperties);
			this.Controls.Add(this.panelControl);
			this.Name = "DemoForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Clouds Demo";
			this.panelProperties.ResumeLayout(false);
			this.panelControl.ResumeLayout(false);
			this.panelSettings1.ResumeLayout(false);
			this.panelSettings1.PerformLayout();
			this.panelSettings2.ResumeLayout(false);
			this.panelSettings2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private OutputPanel panelOutput;
		private System.Windows.Forms.Panel panelProperties;
		private System.Windows.Forms.PropertyGrid propertyGrid;
		private System.Windows.Forms.Splitter splitterProperties;
		private System.Windows.Forms.TreeView treeViewObjects;
		private System.Windows.Forms.Panel panelControl;
		private Nuaj.Cirrus.Utility.LogTextBox richTextBoxOutput;
		private System.Windows.Forms.Panel panelSettings1;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunAzimuth;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSunElevation;
		private System.Windows.Forms.Label label3;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCoverageOffsetTop;
		private System.Windows.Forms.Label label4;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCoverageContrast;
		private System.Windows.Forms.Label label5;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudSize;
		private System.Windows.Forms.Label label6;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlFogAmount;
		private System.Windows.Forms.Label label7;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlAerosolsAmount;
		private System.Windows.Forms.Label label8;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudAltitude;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label9;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudAlbedo;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCloudDensity;
		private System.Windows.Forms.Label label11;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlShadowOpacity;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlIsotropicFactor;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlDirectionalFactor;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Button buttonGoToPage2;
		private System.Windows.Forms.Panel panelSettings2;
		private System.Windows.Forms.Button buttonGoToPage1;
		private System.Windows.Forms.Label label22;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlToneMapAvgMax;
		private System.Windows.Forms.Panel panelSpectrum;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.RadioButton radioButtonRGBRampInset;
		private System.Windows.Forms.RadioButton radioButtonRGBRampFullscreen;
		private System.Windows.Forms.RadioButton radioButtonDEBUGLuminanceCustom;
		private System.Windows.Forms.RadioButton radioButtonDEBUGLuminanceNormalized;
		private System.Windows.Forms.RadioButton radioButtonDEBUGNone;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlDEBUGLuminanceMarker;
		private System.Windows.Forms.Label label19;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlDEBUGLuminanceMarkerTolerance;
		private System.Windows.Forms.Label label20;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSkyDensityAerosolsFactor;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.Button buttonRebuildSkyDensityExp;
		private System.Windows.Forms.Button buttonRebuildSkyDensityLinear;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSkyDensityAltitudeOffset;
		private System.Windows.Forms.Label label23;
		private System.Windows.Forms.Button buttonProfiler;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlWavelengthR;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlWavelengthG;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlWavelengthB;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.Label label24;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCoverageOffsetPow;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlCoverageOffsetBottom;
		private System.Windows.Forms.Label label27;
		private System.Windows.Forms.Label label28;
		private System.Windows.Forms.Button buttonToneMappingSetup;
		private System.Windows.Forms.Button buttonShadowMapViewer;
		private System.Windows.Forms.Button buttonCloudProfiler;
	}
}

