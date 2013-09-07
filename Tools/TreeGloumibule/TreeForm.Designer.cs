namespace TreeGloumibule
{
	partial class TreeForm
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
			this.propertyGridParameters = new System.Windows.Forms.PropertyGrid();
			this.panelBottom = new System.Windows.Forms.Panel();
			this.curvesPanel = new TreeGloumibule.CurvePanel();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.panelLightDome = new TreeGloumibule.OutputPanel();
			this.panelMain = new System.Windows.Forms.Panel();
			this.splitterMain = new System.Windows.Forms.Splitter();
			this.outputPanel = new TreeGloumibule.ViewportPanel();
			this.panelProperties = new System.Windows.Forms.Panel();
			this.tabControlProperties = new System.Windows.Forms.TabControl();
			this.tabPageParameters = new System.Windows.Forms.TabPage();
			this.tabPageSelection = new System.Windows.Forms.TabPage();
			this.propertyGridSelection = new System.Windows.Forms.PropertyGrid();
			this.toolStripMain = new System.Windows.Forms.ToolStrip();
			this.toolStripButtonStep = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonPlayStop = new System.Windows.Forms.ToolStripButton();
			this.timer = new System.Windows.Forms.Timer( this.components );
			this.panelBottom.SuspendLayout();
			this.panelMain.SuspendLayout();
			this.panelProperties.SuspendLayout();
			this.tabControlProperties.SuspendLayout();
			this.tabPageParameters.SuspendLayout();
			this.tabPageSelection.SuspendLayout();
			this.toolStripMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// propertyGridParameters
			// 
			this.propertyGridParameters.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGridParameters.Location = new System.Drawing.Point( 3, 3 );
			this.propertyGridParameters.Name = "propertyGridParameters";
			this.propertyGridParameters.Size = new System.Drawing.Size( 384, 433 );
			this.propertyGridParameters.TabIndex = 0;
			// 
			// panelBottom
			// 
			this.panelBottom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelBottom.Controls.Add( this.curvesPanel );
			this.panelBottom.Controls.Add( this.splitter1 );
			this.panelBottom.Controls.Add( this.panelLightDome );
			this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelBottom.Location = new System.Drawing.Point( 0, 490 );
			this.panelBottom.Name = "panelBottom";
			this.panelBottom.Size = new System.Drawing.Size( 990, 161 );
			this.panelBottom.TabIndex = 1;
			// 
			// curvesPanel
			// 
			this.curvesPanel.BackColor = System.Drawing.Color.Black;
			this.curvesPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.curvesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.curvesPanel.Location = new System.Drawing.Point( 169, 0 );
			this.curvesPanel.Name = "curvesPanel";
			this.curvesPanel.Size = new System.Drawing.Size( 817, 157 );
			this.curvesPanel.TabIndex = 2;
			// 
			// splitter1
			// 
			this.splitter1.Location = new System.Drawing.Point( 166, 0 );
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size( 3, 157 );
			this.splitter1.TabIndex = 1;
			this.splitter1.TabStop = false;
			// 
			// panelLightDome
			// 
			this.panelLightDome.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelLightDome.Dock = System.Windows.Forms.DockStyle.Left;
			this.panelLightDome.Location = new System.Drawing.Point( 0, 0 );
			this.panelLightDome.Name = "panelLightDome";
			this.panelLightDome.Size = new System.Drawing.Size( 166, 157 );
			this.panelLightDome.TabIndex = 0;
			// 
			// panelMain
			// 
			this.panelMain.Controls.Add( this.splitterMain );
			this.panelMain.Controls.Add( this.outputPanel );
			this.panelMain.Controls.Add( this.panelProperties );
			this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelMain.Location = new System.Drawing.Point( 0, 25 );
			this.panelMain.Name = "panelMain";
			this.panelMain.Size = new System.Drawing.Size( 990, 465 );
			this.panelMain.TabIndex = 3;
			// 
			// splitterMain
			// 
			this.splitterMain.Dock = System.Windows.Forms.DockStyle.Right;
			this.splitterMain.Location = new System.Drawing.Point( 589, 0 );
			this.splitterMain.Name = "splitterMain";
			this.splitterMain.Size = new System.Drawing.Size( 3, 465 );
			this.splitterMain.TabIndex = 2;
			this.splitterMain.TabStop = false;
			// 
			// outputPanel
			// 
			this.outputPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.outputPanel.Location = new System.Drawing.Point( 0, 0 );
			this.outputPanel.Name = "outputPanel";
			this.outputPanel.Size = new System.Drawing.Size( 592, 465 );
			this.outputPanel.TabIndex = 1;
			this.outputPanel.MouseDown += new System.Windows.Forms.MouseEventHandler( this.outputPanel_MouseDown );
			// 
			// panelProperties
			// 
			this.panelProperties.Controls.Add( this.tabControlProperties );
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelProperties.Location = new System.Drawing.Point( 592, 0 );
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size( 398, 465 );
			this.panelProperties.TabIndex = 3;
			// 
			// tabControlProperties
			// 
			this.tabControlProperties.Controls.Add( this.tabPageParameters );
			this.tabControlProperties.Controls.Add( this.tabPageSelection );
			this.tabControlProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControlProperties.Location = new System.Drawing.Point( 0, 0 );
			this.tabControlProperties.Name = "tabControlProperties";
			this.tabControlProperties.SelectedIndex = 0;
			this.tabControlProperties.Size = new System.Drawing.Size( 398, 465 );
			this.tabControlProperties.TabIndex = 1;
			// 
			// tabPageParameters
			// 
			this.tabPageParameters.Controls.Add( this.propertyGridParameters );
			this.tabPageParameters.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageParameters.Name = "tabPageParameters";
			this.tabPageParameters.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageParameters.Size = new System.Drawing.Size( 390, 439 );
			this.tabPageParameters.TabIndex = 0;
			this.tabPageParameters.Text = "Parameters";
			this.tabPageParameters.UseVisualStyleBackColor = true;
			// 
			// tabPageSelection
			// 
			this.tabPageSelection.Controls.Add( this.propertyGridSelection );
			this.tabPageSelection.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageSelection.Name = "tabPageSelection";
			this.tabPageSelection.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageSelection.Size = new System.Drawing.Size( 258, 439 );
			this.tabPageSelection.TabIndex = 1;
			this.tabPageSelection.Text = "Selection";
			this.tabPageSelection.UseVisualStyleBackColor = true;
			// 
			// propertyGridSelection
			// 
			this.propertyGridSelection.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGridSelection.Location = new System.Drawing.Point( 3, 3 );
			this.propertyGridSelection.Name = "propertyGridSelection";
			this.propertyGridSelection.Size = new System.Drawing.Size( 252, 433 );
			this.propertyGridSelection.TabIndex = 1;
			// 
			// toolStripMain
			// 
			this.toolStripMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStripMain.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonStep,
            this.toolStripButtonPlayStop} );
			this.toolStripMain.Location = new System.Drawing.Point( 0, 0 );
			this.toolStripMain.Name = "toolStripMain";
			this.toolStripMain.Size = new System.Drawing.Size( 990, 25 );
			this.toolStripMain.TabIndex = 4;
			// 
			// toolStripButtonStep
			// 
			this.toolStripButtonStep.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonStep.Image = global::TreeGloumibule.Properties.Resources.Step;
			this.toolStripButtonStep.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonStep.Name = "toolStripButtonStep";
			this.toolStripButtonStep.Size = new System.Drawing.Size( 23, 22 );
			this.toolStripButtonStep.Text = "Grow One Step";
			this.toolStripButtonStep.Click += new System.EventHandler( this.toolStripButtonStep_Click );
			// 
			// toolStripButtonPlayStop
			// 
			this.toolStripButtonPlayStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonPlayStop.Image = global::TreeGloumibule.Properties.Resources.Play;
			this.toolStripButtonPlayStop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonPlayStop.Name = "toolStripButtonPlayStop";
			this.toolStripButtonPlayStop.Size = new System.Drawing.Size( 23, 22 );
			this.toolStripButtonPlayStop.Text = "Play / Stop";
			this.toolStripButtonPlayStop.Click += new System.EventHandler( this.toolStripButtonPlayStop_Click );
			// 
			// timer
			// 
			this.timer.Interval = 200;
			this.timer.Tick += new System.EventHandler( this.timer_Tick );
			// 
			// TreeForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 990, 651 );
			this.Controls.Add( this.panelMain );
			this.Controls.Add( this.toolStripMain );
			this.Controls.Add( this.panelBottom );
			this.Name = "TreeForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Tree Gloumibule Studio Pregnant Suite 2013 XGT beta 12";
			this.panelBottom.ResumeLayout( false );
			this.panelMain.ResumeLayout( false );
			this.panelProperties.ResumeLayout( false );
			this.tabControlProperties.ResumeLayout( false );
			this.tabPageParameters.ResumeLayout( false );
			this.tabPageSelection.ResumeLayout( false );
			this.toolStripMain.ResumeLayout( false );
			this.toolStripMain.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel panelBottom;
		private System.Windows.Forms.PropertyGrid propertyGridParameters;
		private System.Windows.Forms.Splitter splitter1;
		private OutputPanel panelLightDome;
		private ViewportPanel outputPanel;
		private System.Windows.Forms.Panel panelMain;
		private System.Windows.Forms.Splitter splitterMain;
		private System.Windows.Forms.ToolStrip toolStripMain;
		private System.Windows.Forms.ToolStripButton toolStripButtonStep;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.ToolStripButton toolStripButtonPlayStop;
		private CurvePanel curvesPanel;
		private System.Windows.Forms.Panel panelProperties;
		private System.Windows.Forms.TabControl tabControlProperties;
		private System.Windows.Forms.TabPage tabPageParameters;
		private System.Windows.Forms.TabPage tabPageSelection;
		private System.Windows.Forms.PropertyGrid propertyGridSelection;
	}
}

