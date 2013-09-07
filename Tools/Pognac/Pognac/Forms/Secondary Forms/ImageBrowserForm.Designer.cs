namespace Pognac
{
	partial class ImageBrowserForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( ImageBrowserForm ) );
			this.toolStrip = new System.Windows.Forms.ToolStrip();
			this.toolStripButtonPrint = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonDefineCrop = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonZoomOut = new System.Windows.Forms.ToolStripButton();
			this.zoomableImagesViewer = new Pognac.ZoomableImagesViewer();
			this.toolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip
			// 
			this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonPrint,
            this.toolStripButtonDefineCrop,
            this.toolStripButtonZoomOut} );
			this.toolStrip.Location = new System.Drawing.Point( 0, 0 );
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.Size = new System.Drawing.Size( 851, 25 );
			this.toolStrip.TabIndex = 1;
			// 
			// toolStripButtonPrint
			// 
			this.toolStripButtonPrint.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonPrint.Enabled = false;
			this.toolStripButtonPrint.Image = ((System.Drawing.Image) (resources.GetObject( "toolStripButtonPrint.Image" )));
			this.toolStripButtonPrint.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonPrint.Name = "toolStripButtonPrint";
			this.toolStripButtonPrint.Size = new System.Drawing.Size( 23, 22 );
			this.toolStripButtonPrint.Text = "toolStripButton1";
			this.toolStripButtonPrint.ToolTipText = "Opens the print form to print the selected page.";
			this.toolStripButtonPrint.Click += new System.EventHandler( this.toolStripButtonPrint_Click );
			// 
			// toolStripButtonDefineCrop
			// 
			this.toolStripButtonDefineCrop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonDefineCrop.Enabled = false;
			this.toolStripButtonDefineCrop.Image = ((System.Drawing.Image) (resources.GetObject( "toolStripButtonDefineCrop.Image" )));
			this.toolStripButtonDefineCrop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonDefineCrop.Name = "toolStripButtonDefineCrop";
			this.toolStripButtonDefineCrop.Size = new System.Drawing.Size( 23, 22 );
			this.toolStripButtonDefineCrop.Text = "toolStripButtonDefineCrop";
			this.toolStripButtonDefineCrop.ToolTipText = "Enters the Define Crop mode.";
			this.toolStripButtonDefineCrop.Click += new System.EventHandler( this.toolStripButtonDefineCrop_Click );
			// 
			// toolStripButtonZoomOut
			// 
			this.toolStripButtonZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonZoomOut.Image = ((System.Drawing.Image) (resources.GetObject( "toolStripButtonZoomOut.Image" )));
			this.toolStripButtonZoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonZoomOut.Name = "toolStripButtonZoomOut";
			this.toolStripButtonZoomOut.Size = new System.Drawing.Size( 23, 22 );
			this.toolStripButtonZoomOut.Text = "toolStripButton1";
			this.toolStripButtonZoomOut.ToolTipText = "Zooms on selection";
			this.toolStripButtonZoomOut.Click += new System.EventHandler( this.toolStripButtonZoomOut_Click );
			// 
			// zoomableImagesViewer
			// 
			this.zoomableImagesViewer.Attachments = new Pognac.Documents.Attachment[0];
			this.zoomableImagesViewer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.zoomableImagesViewer.Location = new System.Drawing.Point( 0, 25 );
			this.zoomableImagesViewer.MultipleSelection = new Pognac.Documents.Attachment[0];
			this.zoomableImagesViewer.Name = "zoomableImagesViewer";
			this.zoomableImagesViewer.SingleSelection = null;
			this.zoomableImagesViewer.Size = new System.Drawing.Size( 851, 542 );
			this.zoomableImagesViewer.TabIndex = 0;
			this.zoomableImagesViewer.UseMultipleSelection = false;
			this.zoomableImagesViewer.SelectionChanged += new System.EventHandler( this.zoomableImagesViewer_SelectionChanged );
			// 
			// ImageBrowserForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 851, 567 );
			this.Controls.Add( this.zoomableImagesViewer );
			this.Controls.Add( this.toolStrip );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.KeyPreview = true;
			this.Name = "ImageBrowserForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Image Browser";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.toolStrip.ResumeLayout( false );
			this.toolStrip.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip;
		private System.Windows.Forms.ToolStripButton toolStripButtonDefineCrop;
		private ZoomableImagesViewer zoomableImagesViewer;
		private System.Windows.Forms.ToolStripButton toolStripButtonPrint;
		private System.Windows.Forms.ToolStripButton toolStripButtonZoomOut;


	}
}