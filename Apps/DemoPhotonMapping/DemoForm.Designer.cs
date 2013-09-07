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
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.labelHoveredObject = new System.Windows.Forms.Label();
			this.splitterProperties = new System.Windows.Forms.Splitter();
			this.treeViewObjects = new System.Windows.Forms.TreeView();
			this.buttonProfiling = new System.Windows.Forms.Button();
			this.panelParameters = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarControlSunTheta = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSunPhi = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.panelOutput = new Demo.OutputPanel( this.components );
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
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 0, 609 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 272, 103 );
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
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
			this.panelParameters.Controls.Add( this.label3 );
			this.panelParameters.Controls.Add( this.label2 );
			this.panelParameters.Controls.Add( this.floatTrackbarControlSunTheta );
			this.panelParameters.Controls.Add( this.floatTrackbarControlSunPhi );
			this.panelParameters.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelParameters.Location = new System.Drawing.Point( 0, 660 );
			this.panelParameters.Name = "panelParameters";
			this.panelParameters.Size = new System.Drawing.Size( 941, 120 );
			this.panelParameters.TabIndex = 1;
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
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 941, 660 );
			this.panelOutput.TabIndex = 0;
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
			this.Text = "Photon Mapping Demo";
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
	}
}

