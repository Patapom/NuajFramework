namespace Pognac
{
	partial class UnAssignedAttachmentsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( UnAssignedAttachmentsForm ) );
			this.label1 = new System.Windows.Forms.Label();
			this.toolTip = new System.Windows.Forms.ToolTip( this.components );
			this.buttonCreateDocument = new System.Windows.Forms.Button();
			this.buttonAddPages = new System.Windows.Forms.Button();
			this.buttonPick = new System.Windows.Forms.Button();
			this.labelUnAssignedFiles = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.panelCreateDocument = new System.Windows.Forms.Panel();
			this.panelAddPagesToDocument = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.panelPickNewPageAttachment = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.zoomableImagesViewer = new Pognac.ZoomableImagesViewer();
			this.buttonZoomOut = new System.Windows.Forms.Button();
			this.panelCreateDocument.SuspendLayout();
			this.panelAddPagesToDocument.SuspendLayout();
			this.panelPickNewPageAttachment.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label1.Location = new System.Drawing.Point( 12, 21 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 213, 20 );
			this.label1.TabIndex = 2;
			this.label1.Text = "Currently Un-Assigned Files :";
			this.toolTip.SetToolTip( this.label1, "The title to give to the document (optional). You should only use that for import" +
					"ant documents." );
			// 
			// toolTip
			// 
			this.toolTip.IsBalloon = true;
			this.toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.toolTip.ToolTipTitle = "Information";
			// 
			// buttonCreateDocument
			// 
			this.buttonCreateDocument.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonCreateDocument.BackgroundImage = global::Pognac.Properties.Resources.New;
			this.buttonCreateDocument.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.buttonCreateDocument.Enabled = false;
			this.buttonCreateDocument.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonCreateDocument.Location = new System.Drawing.Point( 327, -1 );
			this.buttonCreateDocument.Name = "buttonCreateDocument";
			this.buttonCreateDocument.Size = new System.Drawing.Size( 48, 45 );
			this.buttonCreateDocument.TabIndex = 11;
			this.toolTip.SetToolTip( this.buttonCreateDocument, "Click to create a new document from the selected files." );
			this.buttonCreateDocument.UseVisualStyleBackColor = true;
			this.buttonCreateDocument.Click += new System.EventHandler( this.buttonCreateDocument_Click );
			// 
			// buttonAddPages
			// 
			this.buttonAddPages.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonAddPages.BackgroundImage = global::Pognac.Properties.Resources.New;
			this.buttonAddPages.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.buttonAddPages.Enabled = false;
			this.buttonAddPages.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonAddPages.Location = new System.Drawing.Point( 340, -1 );
			this.buttonAddPages.Name = "buttonAddPages";
			this.buttonAddPages.Size = new System.Drawing.Size( 48, 45 );
			this.buttonAddPages.TabIndex = 11;
			this.toolTip.SetToolTip( this.buttonAddPages, "Click to add the selected files as pages for the existing document." );
			this.buttonAddPages.UseVisualStyleBackColor = true;
			this.buttonAddPages.Click += new System.EventHandler( this.buttonAddPages_Click );
			// 
			// buttonPick
			// 
			this.buttonPick.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonPick.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonPick.Enabled = false;
			this.buttonPick.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonPick.Location = new System.Drawing.Point( 260, 8 );
			this.buttonPick.Name = "buttonPick";
			this.buttonPick.Size = new System.Drawing.Size( 99, 28 );
			this.buttonPick.TabIndex = 14;
			this.buttonPick.Text = "Pick";
			this.toolTip.SetToolTip( this.buttonPick, "Click to pick a new file for the page" );
			this.buttonPick.UseVisualStyleBackColor = true;
			this.buttonPick.Click += new System.EventHandler( this.buttonPick_Click );
			// 
			// labelUnAssignedFiles
			// 
			this.labelUnAssignedFiles.AutoSize = true;
			this.labelUnAssignedFiles.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.labelUnAssignedFiles.Location = new System.Drawing.Point( 231, 21 );
			this.labelUnAssignedFiles.Name = "labelUnAssignedFiles";
			this.labelUnAssignedFiles.Size = new System.Drawing.Size( 18, 20 );
			this.labelUnAssignedFiles.TabIndex = 2;
			this.labelUnAssignedFiles.Text = "0";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label2.Location = new System.Drawing.Point( 3, 11 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 318, 20 );
			this.label2.TabIndex = 2;
			this.label2.Text = "Create a new document from selected files :";
			// 
			// panelCreateDocument
			// 
			this.panelCreateDocument.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panelCreateDocument.Controls.Add( this.label2 );
			this.panelCreateDocument.Controls.Add( this.buttonCreateDocument );
			this.panelCreateDocument.Location = new System.Drawing.Point( 16, 467 );
			this.panelCreateDocument.Name = "panelCreateDocument";
			this.panelCreateDocument.Size = new System.Drawing.Size( 583, 45 );
			this.panelCreateDocument.TabIndex = 13;
			// 
			// panelAddPagesToDocument
			// 
			this.panelAddPagesToDocument.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panelAddPagesToDocument.Controls.Add( this.buttonAddPages );
			this.panelAddPagesToDocument.Controls.Add( this.label3 );
			this.panelAddPagesToDocument.Location = new System.Drawing.Point( 16, 467 );
			this.panelAddPagesToDocument.Name = "panelAddPagesToDocument";
			this.panelAddPagesToDocument.Size = new System.Drawing.Size( 583, 45 );
			this.panelAddPagesToDocument.TabIndex = 14;
			this.panelAddPagesToDocument.Visible = false;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label3.Location = new System.Drawing.Point( 3, 11 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 331, 20 );
			this.label3.TabIndex = 2;
			this.label3.Text = "Add selected files as pages to the document :";
			// 
			// panelPickNewPageAttachment
			// 
			this.panelPickNewPageAttachment.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panelPickNewPageAttachment.Controls.Add( this.label4 );
			this.panelPickNewPageAttachment.Controls.Add( this.buttonPick );
			this.panelPickNewPageAttachment.Location = new System.Drawing.Point( 16, 466 );
			this.panelPickNewPageAttachment.Name = "panelPickNewPageAttachment";
			this.panelPickNewPageAttachment.Size = new System.Drawing.Size( 704, 45 );
			this.panelPickNewPageAttachment.TabIndex = 15;
			this.panelPickNewPageAttachment.Visible = false;
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label4.Location = new System.Drawing.Point( 10, 12 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 244, 20 );
			this.label4.TabIndex = 15;
			this.label4.Text = "Assign selection to current page :";
			// 
			// zoomableImagesViewer
			// 
			this.zoomableImagesViewer.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.zoomableImagesViewer.Attachments = new Pognac.Documents.Attachment[0];
			this.zoomableImagesViewer.Location = new System.Drawing.Point( 16, 44 );
			this.zoomableImagesViewer.MultipleSelection = new Pognac.Documents.Attachment[0];
			this.zoomableImagesViewer.Name = "zoomableImagesViewer";
			this.zoomableImagesViewer.SingleSelection = null;
			this.zoomableImagesViewer.Size = new System.Drawing.Size( 712, 417 );
			this.zoomableImagesViewer.TabIndex = 12;
			this.zoomableImagesViewer.UseMultipleSelection = true;
			this.zoomableImagesViewer.SelectionChanged += new System.EventHandler( this.zoomableImagesViewer_SelectionChanged );
			// 
			// buttonZoomOut
			// 
			this.buttonZoomOut.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonZoomOut.Location = new System.Drawing.Point( 653, 18 );
			this.buttonZoomOut.Name = "buttonZoomOut";
			this.buttonZoomOut.Size = new System.Drawing.Size( 75, 23 );
			this.buttonZoomOut.TabIndex = 16;
			this.buttonZoomOut.Text = "ZoomOut";
			this.buttonZoomOut.UseVisualStyleBackColor = true;
			this.buttonZoomOut.Click += new System.EventHandler( this.buttonZoomOut_Click );
			// 
			// UnAssignedAttachmentsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 740, 517 );
			this.Controls.Add( this.buttonZoomOut );
			this.Controls.Add( this.zoomableImagesViewer );
			this.Controls.Add( this.labelUnAssignedFiles );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.panelAddPagesToDocument );
			this.Controls.Add( this.panelCreateDocument );
			this.Controls.Add( this.panelPickNewPageAttachment );
			this.Enabled = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.MinimumSize = new System.Drawing.Size( 402, 412 );
			this.Name = "UnAssignedAttachmentsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "UnAssigned Files Editor";
			this.panelCreateDocument.ResumeLayout( false );
			this.panelCreateDocument.PerformLayout();
			this.panelAddPagesToDocument.ResumeLayout( false );
			this.panelAddPagesToDocument.PerformLayout();
			this.panelPickNewPageAttachment.ResumeLayout( false );
			this.panelPickNewPageAttachment.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Label labelUnAssignedFiles;
		private System.Windows.Forms.Button buttonCreateDocument;
		private System.Windows.Forms.Label label2;
		private ZoomableImagesViewer zoomableImagesViewer;
		private System.Windows.Forms.Panel panelCreateDocument;
		private System.Windows.Forms.Panel panelAddPagesToDocument;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonAddPages;
		private System.Windows.Forms.Panel panelPickNewPageAttachment;
		private System.Windows.Forms.Button buttonPick;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button buttonZoomOut;
	}
}