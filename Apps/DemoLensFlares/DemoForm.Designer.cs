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
			this.comboBoxLensFlare = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.labelHoveredObject = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarBrightness = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.richTextBoxOutput = new Demo.LogTextBox( this.components );
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
			this.panelErrors.Location = new System.Drawing.Point( 0, 612 );
			this.panelErrors.Name = "panelErrors";
			this.panelErrors.Size = new System.Drawing.Size( 920, 101 );
			this.panelErrors.TabIndex = 1;
			// 
			// panelBars
			// 
			this.panelBars.Controls.Add( this.comboBoxLensFlare );
			this.panelBars.Controls.Add( this.label1 );
			this.panelBars.Controls.Add( this.labelHoveredObject );
			this.panelBars.Controls.Add( this.label2 );
			this.panelBars.Controls.Add( this.floatTrackbarBrightness );
			this.panelBars.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelBars.Location = new System.Drawing.Point( 0, 0 );
			this.panelBars.Name = "panelBars";
			this.panelBars.Size = new System.Drawing.Size( 623, 101 );
			this.panelBars.TabIndex = 3;
			// 
			// comboBoxLensFlare
			// 
			this.comboBoxLensFlare.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxLensFlare.FormattingEnabled = true;
			this.comboBoxLensFlare.Location = new System.Drawing.Point( 101, 16 );
			this.comboBoxLensFlare.Name = "comboBoxLensFlare";
			this.comboBoxLensFlare.Size = new System.Drawing.Size( 206, 21 );
			this.comboBoxLensFlare.TabIndex = 2;
			this.comboBoxLensFlare.SelectedIndexChanged += new System.EventHandler( this.comboBoxLensFlare_SelectedIndexChanged );
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 12, 19 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 83, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Lens Flare Type";
			// 
			// labelHoveredObject
			// 
			this.labelHoveredObject.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelHoveredObject.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.labelHoveredObject.Location = new System.Drawing.Point( 15, 56 );
			this.labelHoveredObject.Name = "labelHoveredObject";
			this.labelHoveredObject.Size = new System.Drawing.Size( 228, 25 );
			this.labelHoveredObject.TabIndex = 1;
			this.labelHoveredObject.Text = "No hovered object...";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 329, 19 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 56, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Brightness";
			// 
			// floatTrackbarBrightness
			// 
			this.floatTrackbarBrightness.Location = new System.Drawing.Point( 401, 16 );
			this.floatTrackbarBrightness.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarBrightness.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarBrightness.Name = "floatTrackbarBrightness";
			this.floatTrackbarBrightness.RangeMax = 10F;
			this.floatTrackbarBrightness.RangeMin = 0F;
			this.floatTrackbarBrightness.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarBrightness.TabIndex = 0;
			this.floatTrackbarBrightness.Value = 1F;
			this.floatTrackbarBrightness.VisibleRangeMax = 2F;
			this.floatTrackbarBrightness.ValueChanged += new Nuaj.Cirrus.Utility.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarBrightness_ValueChanged );
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Right;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 623, 0 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 297, 101 );
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 920, 612 );
			this.panelOutput.TabIndex = 0;
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
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarBrightness;
		private System.Windows.Forms.ComboBox comboBoxLensFlare;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelHoveredObject;
	}
}

