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
			this.panelErrors = new System.Windows.Forms.Panel();
			this.richTextBoxOutput = new Demo.LogTextBox( this.components );
			this.panelOutput = new Demo.OutputPanel( this.components );
			this.panelProperties.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panelErrors.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelProperties
			// 
			this.panelProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelProperties.Controls.Add( this.propertyGrid );
			this.panelProperties.Controls.Add( this.panel1 );
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
			this.propertyGrid.Size = new System.Drawing.Size( 272, 344 );
			this.propertyGrid.TabIndex = 1;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add( this.label1 );
			this.panel1.Controls.Add( this.labelHoveredObject );
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point( 0, 619 );
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
			// panelErrors
			// 
			this.panelErrors.Controls.Add( this.richTextBoxOutput );
			this.panelErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelErrors.Location = new System.Drawing.Point( 0, 523 );
			this.panelErrors.Name = "panelErrors";
			this.panelErrors.Size = new System.Drawing.Size( 913, 141 );
			this.panelErrors.TabIndex = 1;
			// 
			// richTextBoxOutput
			// 
			this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBoxOutput.Location = new System.Drawing.Point( 0, 0 );
			this.richTextBoxOutput.Name = "richTextBoxOutput";
			this.richTextBoxOutput.Size = new System.Drawing.Size( 913, 141 );
			this.richTextBoxOutput.TabIndex = 0;
			this.richTextBoxOutput.Text = "";
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 913, 523 );
			this.panelOutput.TabIndex = 0;
			this.panelOutput.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler( this.panelOutput_PreviewKeyDown );
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
			this.Text = "Caustics Demo - Use Ctrl+MouseDrag to move the light and the deforming sphere";
			this.panelProperties.ResumeLayout( false );
			this.panel1.ResumeLayout( false );
			this.panelErrors.ResumeLayout( false );
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
		private System.Windows.Forms.Label labelHoveredObject;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
	}
}

