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
			this.panelOutput = new Demo.OutputPanel( this.components );
			this.panelProperties = new System.Windows.Forms.Panel();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.richTextBoxOutput = new Nuaj.Cirrus.Utility.LogTextBox( this.components );
			this.splitterProperties = new System.Windows.Forms.Splitter();
			this.treeViewObjects = new System.Windows.Forms.TreeView();
			this.panelErrors = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControlNormalAmplitude = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlDiffusionDistance = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.integerTrackbarControlDebug = new Nuaj.Cirrus.Utility.IntegerTrackbarControl();
			this.label3 = new System.Windows.Forms.Label();
			this.panelProperties.SuspendLayout();
			this.panelErrors.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 913, 523 );
			this.panelOutput.TabIndex = 0;
			// 
			// panelProperties
			// 
			this.panelProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelProperties.Controls.Add( this.propertyGrid );
			this.panelProperties.Controls.Add( this.richTextBoxOutput );
			this.panelProperties.Controls.Add( this.splitterProperties );
			this.panelProperties.Controls.Add( this.treeViewObjects );
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelProperties.Location = new System.Drawing.Point( 913, 0 );
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size( 274, 664 );
			this.panelProperties.TabIndex = 1;
			// 
			// propertyGrid
			// 
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.Location = new System.Drawing.Point( 0, 275 );
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size( 272, 256 );
			this.propertyGrid.TabIndex = 2;
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 0, 531 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 272, 131 );
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
			this.panelErrors.Controls.Add( this.integerTrackbarControlDebug );
			this.panelErrors.Controls.Add( this.label3 );
			this.panelErrors.Controls.Add( this.label2 );
			this.panelErrors.Controls.Add( this.label1 );
			this.panelErrors.Controls.Add( this.floatTrackbarControlNormalAmplitude );
			this.panelErrors.Controls.Add( this.floatTrackbarControlDiffusionDistance );
			this.panelErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelErrors.Location = new System.Drawing.Point( 0, 523 );
			this.panelErrors.Name = "panelErrors";
			this.panelErrors.Size = new System.Drawing.Size( 913, 141 );
			this.panelErrors.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 13, 35 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 89, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Normal Amplitude";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 13, 9 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 93, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Diffusion Distance";
			// 
			// floatTrackbarControlNormalAmplitude
			// 
			this.floatTrackbarControlNormalAmplitude.Location = new System.Drawing.Point( 112, 32 );
			this.floatTrackbarControlNormalAmplitude.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlNormalAmplitude.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlNormalAmplitude.Name = "floatTrackbarControlNormalAmplitude";
			this.floatTrackbarControlNormalAmplitude.RangeMax = 32F;
			this.floatTrackbarControlNormalAmplitude.RangeMin = 0F;
			this.floatTrackbarControlNormalAmplitude.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlNormalAmplitude.TabIndex = 0;
			this.floatTrackbarControlNormalAmplitude.Value = 2F;
			this.floatTrackbarControlNormalAmplitude.VisibleRangeMax = 4F;
			this.floatTrackbarControlNormalAmplitude.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlNormalAmplitude_ValueChanged );
			// 
			// floatTrackbarControlDiffusionDistance
			// 
			this.floatTrackbarControlDiffusionDistance.Location = new System.Drawing.Point( 112, 6 );
			this.floatTrackbarControlDiffusionDistance.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlDiffusionDistance.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlDiffusionDistance.Name = "floatTrackbarControlDiffusionDistance";
			this.floatTrackbarControlDiffusionDistance.RangeMax = 64F;
			this.floatTrackbarControlDiffusionDistance.RangeMin = 0.001F;
			this.floatTrackbarControlDiffusionDistance.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlDiffusionDistance.TabIndex = 0;
			this.floatTrackbarControlDiffusionDistance.Value = 16F;
			this.floatTrackbarControlDiffusionDistance.VisibleRangeMax = 32F;
			this.floatTrackbarControlDiffusionDistance.VisibleRangeMin = 0.001F;
			this.floatTrackbarControlDiffusionDistance.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlDiffusionDistance_ValueChanged );
			// 
			// integerTrackbarControlDebug
			// 
			this.integerTrackbarControlDebug.Location = new System.Drawing.Point( 112, 58 );
			this.integerTrackbarControlDebug.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlDebug.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlDebug.Name = "integerTrackbarControlDebug";
			this.integerTrackbarControlDebug.RangeMax = 5;
			this.integerTrackbarControlDebug.RangeMin = 0;
			this.integerTrackbarControlDebug.Size = new System.Drawing.Size( 200, 20 );
			this.integerTrackbarControlDebug.TabIndex = 2;
			this.integerTrackbarControlDebug.Value = 0;
			this.integerTrackbarControlDebug.VisibleRangeMax = 5;
			this.integerTrackbarControlDebug.ValueChanged += new Nuaj.Cirrus.Utility.IntegerTrackbarControl.ValueChangedEventHandler( this.integerTrackbarControlDebug_ValueChanged );
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 13, 61 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 39, 13 );
			this.label3.TabIndex = 1;
			this.label3.Text = "Debug";
			// 
			// DemoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1187, 664 );
			this.Controls.Add( this.panelOutput );
			this.Controls.Add( this.panelErrors );
			this.Controls.Add( this.panelProperties );
			this.Name = "DemoForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "SubSurface Scattering Demo";
			this.panelProperties.ResumeLayout( false );
			this.panelErrors.ResumeLayout( false );
			this.panelErrors.PerformLayout();
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
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlNormalAmplitude;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlDiffusionDistance;
		private Nuaj.Cirrus.Utility.IntegerTrackbarControl integerTrackbarControlDebug;
		private System.Windows.Forms.Label label3;
	}
}

