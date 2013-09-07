namespace CloudPreComputer
{
	partial class Form1OLD
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
			this.outputPanel = new CloudPreComputer.OutputPanel( this.components );
			this.SuspendLayout();
			// 
			// outputPanel
			// 
			this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.outputPanel.Location = new System.Drawing.Point( 0, 0 );
			this.outputPanel.Name = "outputPanel";
			this.outputPanel.Size = new System.Drawing.Size( 877, 617 );
			this.outputPanel.TabIndex = 0;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 877, 617 );
			this.Controls.Add( this.outputPanel );
			this.Name = "Form1";
			this.Text = "Form1";
			this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler( this.Form1_PreviewKeyDown );
			this.ResumeLayout( false );

		}

		#endregion

		private OutputPanel outputPanel;
	}
}

