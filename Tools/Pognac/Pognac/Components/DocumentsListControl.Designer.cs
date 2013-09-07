namespace Pognac
{
	partial class DocumentsListControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.thumbnailBrowser = new Pognac.ThumbnailBrowser();
			this.toolTip1 = new System.Windows.Forms.ToolTip( this.components );
			this.timerTooltip = new System.Windows.Forms.Timer( this.components );
			this.SuspendLayout();
			// 
			// thumbnailBrowser
			// 
			this.thumbnailBrowser.Attachments = new Pognac.Documents.Attachment[0];
			this.thumbnailBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.thumbnailBrowser.DoubleClickOpensViewer = false;
			this.thumbnailBrowser.Location = new System.Drawing.Point( 0, 0 );
			this.thumbnailBrowser.Name = "thumbnailBrowser";
			this.thumbnailBrowser.Selection = null;
			this.thumbnailBrowser.Size = new System.Drawing.Size( 504, 263 );
			this.thumbnailBrowser.TabIndex = 0;
			this.thumbnailBrowser.SelectionChanged += new System.EventHandler( this.thumbnailBrowser_SelectionChanged );
			this.thumbnailBrowser.Enter += new System.EventHandler( this.thumbnailBrowser_Enter );
			this.thumbnailBrowser.Leave += new System.EventHandler( this.thumbnailBrowser_Leave );
			this.thumbnailBrowser.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.thumbnailBrowser_MouseDoubleClick );
			this.thumbnailBrowser.MouseMove += new System.Windows.Forms.MouseEventHandler( this.thumbnailBrowser_MouseMove );
			// 
			// timerTooltip
			// 
			this.timerTooltip.Enabled = true;
			this.timerTooltip.Interval = 500;
			this.timerTooltip.Tick += new System.EventHandler( this.timerTooltip_Tick );
			// 
			// DocumentsListControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add( this.thumbnailBrowser );
			this.Name = "DocumentsListControl";
			this.Size = new System.Drawing.Size( 504, 263 );
			this.ResumeLayout( false );

		}

		#endregion

		private ThumbnailBrowser thumbnailBrowser;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Timer timerTooltip;

	}
}
