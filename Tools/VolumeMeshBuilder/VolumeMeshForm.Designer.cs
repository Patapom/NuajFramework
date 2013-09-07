namespace VolumeMeshBuilder
{
	partial class VolumeMeshForm
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
			this.panelRight = new System.Windows.Forms.Panel();
			this.integerTrackbarControlBoxesCount = new Nuaj.Cirrus.Utility.IntegerTrackbarControl();
			this.buttonProfiler = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxInfos = new System.Windows.Forms.TextBox();
			this.panelOutput = new Nuaj.Cirrus.Utility.PanelOutput( this.components );
			this.panelRight.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelRight
			// 
			this.panelRight.Controls.Add( this.integerTrackbarControlBoxesCount );
			this.panelRight.Controls.Add( this.buttonProfiler );
			this.panelRight.Controls.Add( this.label2 );
			this.panelRight.Controls.Add( this.label1 );
			this.panelRight.Controls.Add( this.textBoxInfos );
			this.panelRight.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelRight.Location = new System.Drawing.Point( 832, 0 );
			this.panelRight.Name = "panelRight";
			this.panelRight.Size = new System.Drawing.Size( 200, 663 );
			this.panelRight.TabIndex = 1;
			// 
			// integerTrackbarControlBoxesCount
			// 
			this.integerTrackbarControlBoxesCount.Location = new System.Drawing.Point( 6, 590 );
			this.integerTrackbarControlBoxesCount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlBoxesCount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlBoxesCount.Name = "integerTrackbarControlBoxesCount";
			this.integerTrackbarControlBoxesCount.RangeMin = 1;
			this.integerTrackbarControlBoxesCount.Size = new System.Drawing.Size( 188, 20 );
			this.integerTrackbarControlBoxesCount.TabIndex = 3;
			this.integerTrackbarControlBoxesCount.Value = 100;
			this.integerTrackbarControlBoxesCount.VisibleRangeMin = 1;
			this.integerTrackbarControlBoxesCount.ValueChanged += new Nuaj.Cirrus.Utility.IntegerTrackbarControl.ValueChangedEventHandler( this.integerTrackbarControlBoxesCount_ValueChanged );
			// 
			// buttonProfiler
			// 
			this.buttonProfiler.Location = new System.Drawing.Point( 65, 628 );
			this.buttonProfiler.Name = "buttonProfiler";
			this.buttonProfiler.Size = new System.Drawing.Size( 75, 23 );
			this.buttonProfiler.TabIndex = 2;
			this.buttonProfiler.Text = "Profiler";
			this.buttonProfiler.UseVisualStyleBackColor = true;
			this.buttonProfiler.Click += new System.EventHandler( this.buttonProfiler_Click );
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label2.Location = new System.Drawing.Point( 6, 574 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 78, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Boxes Count";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label1.Location = new System.Drawing.Point( 6, 9 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 92, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Partitions Infos";
			// 
			// textBoxInfos
			// 
			this.textBoxInfos.Location = new System.Drawing.Point( 6, 25 );
			this.textBoxInfos.Multiline = true;
			this.textBoxInfos.Name = "textBoxInfos";
			this.textBoxInfos.Size = new System.Drawing.Size( 188, 541 );
			this.textBoxInfos.TabIndex = 0;
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 832, 663 );
			this.panelOutput.TabIndex = 0;
			// 
			// VolumeMeshForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1032, 663 );
			this.Controls.Add( this.panelOutput );
			this.Controls.Add( this.panelRight );
			this.Name = "VolumeMeshForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Form1";
			this.panelRight.ResumeLayout( false );
			this.panelRight.PerformLayout();
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.Panel panelRight;
		private System.Windows.Forms.Button buttonProfiler;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxInfos;
		private Nuaj.Cirrus.Utility.PanelOutput panelOutput;
		private Nuaj.Cirrus.Utility.IntegerTrackbarControl integerTrackbarControlBoxesCount;
		private System.Windows.Forms.Label label2;

	}
}

