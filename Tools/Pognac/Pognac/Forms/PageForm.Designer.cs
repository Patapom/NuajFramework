namespace Pognac
{
	partial class PageForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( PageForm ) );
			this.buttonSave = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.textBoxFileName = new System.Windows.Forms.TextBox();
			this.buttonLocateFile = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip( this.components );
			this.buttonChangePage = new System.Windows.Forms.Button();
			this.annotationControl = new Pognac.AnnotationControl();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.comboBoxPageType = new System.Windows.Forms.ComboBox();
			this.radioButtonRecto = new System.Windows.Forms.RadioButton();
			this.radioButtonVerso = new System.Windows.Forms.RadioButton();
			this.thumbnailBrowser = new Pognac.ThumbnailBrowser();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonSave.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonSave.Location = new System.Drawing.Point( 193, 543 );
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.Size = new System.Drawing.Size( 99, 28 );
			this.buttonSave.TabIndex = 7;
			this.buttonSave.Text = "Save";
			this.buttonSave.UseVisualStyleBackColor = true;
			this.buttonSave.Click += new System.EventHandler( this.buttonSave_Click );
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label1.Location = new System.Drawing.Point( 12, 21 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 92, 20 );
			this.label1.TabIndex = 2;
			this.label1.Text = "Page Type :";
			this.toolTip.SetToolTip( this.label1, "The title to give to the document (optional). You should only use that for import" +
					"ant documents." );
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label7.Location = new System.Drawing.Point( 12, 89 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 42, 20 );
			this.label7.TabIndex = 2;
			this.label7.Text = "File :";
			this.toolTip.SetToolTip( this.label7, "The institution that issued the document." );
			// 
			// textBoxFileName
			// 
			this.textBoxFileName.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFileName.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxFileName.Location = new System.Drawing.Point( 60, 86 );
			this.textBoxFileName.Name = "textBoxFileName";
			this.textBoxFileName.ReadOnly = true;
			this.textBoxFileName.Size = new System.Drawing.Size( 388, 26 );
			this.textBoxFileName.TabIndex = 3;
			this.toolTip.SetToolTip( this.textBoxFileName, "The name of the file representing that page." );
			// 
			// buttonLocateFile
			// 
			this.buttonLocateFile.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonLocateFile.Enabled = false;
			this.buttonLocateFile.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonLocateFile.Location = new System.Drawing.Point( 454, 83 );
			this.buttonLocateFile.Name = "buttonLocateFile";
			this.buttonLocateFile.Size = new System.Drawing.Size( 33, 32 );
			this.buttonLocateFile.TabIndex = 4;
			this.buttonLocateFile.Text = "...";
			this.toolTip.SetToolTip( this.buttonLocateFile, "Brings you the the file\'s location on disk." );
			this.buttonLocateFile.UseVisualStyleBackColor = true;
			this.buttonLocateFile.Click += new System.EventHandler( this.buttonLocateFile_Click );
			// 
			// toolTip
			// 
			this.toolTip.IsBalloon = true;
			this.toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.toolTip.ToolTipTitle = "Information";
			// 
			// buttonChangePage
			// 
			this.buttonChangePage.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonChangePage.BackgroundImage = global::Pognac.Properties.Resources.document_icon;
			this.buttonChangePage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonChangePage.Location = new System.Drawing.Point( 407, 325 );
			this.buttonChangePage.Name = "buttonChangePage";
			this.buttonChangePage.Size = new System.Drawing.Size( 57, 59 );
			this.buttonChangePage.TabIndex = 6;
			this.toolTip.SetToolTip( this.buttonChangePage, "Click to change the page file and choose one of the un-processed files." );
			this.buttonChangePage.UseVisualStyleBackColor = true;
			this.buttonChangePage.Visible = false;
			this.buttonChangePage.Click += new System.EventHandler( this.buttonChangePage_Click );
			// 
			// annotationControl
			// 
			this.annotationControl.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.annotationControl.Annotation = null;
			this.annotationControl.Enabled = false;
			this.annotationControl.Location = new System.Drawing.Point( 3, 16 );
			this.annotationControl.Name = "annotationControl";
			this.annotationControl.Size = new System.Drawing.Size( 454, 124 );
			this.annotationControl.TabIndex = 0;
			this.toolTip.SetToolTip( this.annotationControl, "Add some personnal notes to the document." );
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add( this.annotationControl );
			this.groupBox1.Location = new System.Drawing.Point( 12, 390 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 475, 143 );
			this.groupBox1.TabIndex = 9;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Notes";
			// 
			// comboBoxPageType
			// 
			this.comboBoxPageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPageType.FormattingEnabled = true;
			this.comboBoxPageType.Location = new System.Drawing.Point( 110, 23 );
			this.comboBoxPageType.Name = "comboBoxPageType";
			this.comboBoxPageType.Size = new System.Drawing.Size( 229, 21 );
			this.comboBoxPageType.TabIndex = 0;
			this.comboBoxPageType.SelectedIndexChanged += new System.EventHandler( this.comboBoxPageType_SelectedIndexChanged );
			// 
			// radioButtonRecto
			// 
			this.radioButtonRecto.AutoSize = true;
			this.radioButtonRecto.Checked = true;
			this.radioButtonRecto.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.radioButtonRecto.Location = new System.Drawing.Point( 110, 55 );
			this.radioButtonRecto.Name = "radioButtonRecto";
			this.radioButtonRecto.Size = new System.Drawing.Size( 70, 24 );
			this.radioButtonRecto.TabIndex = 1;
			this.radioButtonRecto.TabStop = true;
			this.radioButtonRecto.Text = "Recto";
			this.radioButtonRecto.UseVisualStyleBackColor = true;
			this.radioButtonRecto.CheckedChanged += new System.EventHandler( this.radioButtonRecto_CheckedChanged );
			// 
			// radioButtonVerso
			// 
			this.radioButtonVerso.AutoSize = true;
			this.radioButtonVerso.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.radioButtonVerso.Location = new System.Drawing.Point( 186, 55 );
			this.radioButtonVerso.Name = "radioButtonVerso";
			this.radioButtonVerso.Size = new System.Drawing.Size( 69, 24 );
			this.radioButtonVerso.TabIndex = 2;
			this.radioButtonVerso.Text = "Verso";
			this.radioButtonVerso.UseVisualStyleBackColor = true;
			// 
			// thumbnailBrowser
			// 
			this.thumbnailBrowser.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.thumbnailBrowser.Attachments = new Pognac.Documents.Attachment[0];
			this.thumbnailBrowser.DoubleClickOpensViewer = true;
			this.thumbnailBrowser.Location = new System.Drawing.Point( 98, 118 );
			this.thumbnailBrowser.Name = "thumbnailBrowser";
			this.thumbnailBrowser.Selection = null;
			this.thumbnailBrowser.Size = new System.Drawing.Size( 303, 266 );
			this.thumbnailBrowser.TabIndex = 5;
			// 
			// PageForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 499, 583 );
			this.Controls.Add( this.radioButtonVerso );
			this.Controls.Add( this.radioButtonRecto );
			this.Controls.Add( this.comboBoxPageType );
			this.Controls.Add( this.thumbnailBrowser );
			this.Controls.Add( this.groupBox1 );
			this.Controls.Add( this.buttonChangePage );
			this.Controls.Add( this.textBoxFileName );
			this.Controls.Add( this.label7 );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.buttonLocateFile );
			this.Controls.Add( this.buttonSave );
			this.Enabled = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.MinimumSize = new System.Drawing.Size( 400, 520 );
			this.Name = "PageForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Page Editor";
			this.groupBox1.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSave;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox textBoxFileName;
		private System.Windows.Forms.Button buttonLocateFile;
		private System.Windows.Forms.Button buttonChangePage;
		private System.Windows.Forms.ToolTip toolTip;
		private AnnotationControl annotationControl;
		private System.Windows.Forms.GroupBox groupBox1;
		private ThumbnailBrowser thumbnailBrowser;
		private System.Windows.Forms.ComboBox comboBoxPageType;
		private System.Windows.Forms.RadioButton radioButtonRecto;
		private System.Windows.Forms.RadioButton radioButtonVerso;
	}
}