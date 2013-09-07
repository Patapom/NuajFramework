namespace Pognac
{
	partial class DocumentForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( DocumentForm ) );
			this.buttonSave = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxTitle = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.dateTimePickerCreation = new System.Windows.Forms.DateTimePicker();
			this.dateTimePickerIssue = new System.Windows.Forms.DateTimePicker();
			this.dateTimePickerReception = new System.Windows.Forms.DateTimePicker();
			this.dateTimePickerDue = new System.Windows.Forms.DateTimePicker();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.textBoxInstitution = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.textBoxTags = new System.Windows.Forms.TextBox();
			this.buttonPickTag = new System.Windows.Forms.Button();
			this.label9 = new System.Windows.Forms.Label();
			this.toolTip = new System.Windows.Forms.ToolTip( this.components );
			this.buttonRemovePage = new System.Windows.Forms.Button();
			this.buttonEditSelectedPage = new System.Windows.Forms.Button();
			this.buttonCreatePage = new System.Windows.Forms.Button();
			this.buttonCreateTag = new System.Windows.Forms.Button();
			this.buttonCreateInstitution = new System.Windows.Forms.Button();
			this.buttonDelete = new System.Windows.Forms.Button();
			this.buttonMovePageLeft = new System.Windows.Forms.Button();
			this.buttonMovePageRight = new System.Windows.Forms.Button();
			this.annotationControl = new Pognac.AnnotationControl();
			this.floatTrackbarControlAmount = new Pognac.FloatTrackbarControl();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.errorProvider = new System.Windows.Forms.ErrorProvider( this.components );
			this.thumbnailBrowser = new Pognac.ThumbnailBrowser();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize) (this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonSave.Enabled = false;
			this.buttonSave.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonSave.Location = new System.Drawing.Point( 193, 596 );
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.Size = new System.Drawing.Size( 99, 28 );
			this.buttonSave.TabIndex = 15;
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
			this.label1.Size = new System.Drawing.Size( 124, 20 );
			this.label1.TabIndex = 2;
			this.label1.Text = "Document Title :";
			this.toolTip.SetToolTip( this.label1, "The title to give to the document (optional). You should only use that for import" +
					"ant documents." );
			// 
			// textBoxTitle
			// 
			this.textBoxTitle.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTitle.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxTitle.Location = new System.Drawing.Point( 142, 18 );
			this.textBoxTitle.Name = "textBoxTitle";
			this.textBoxTitle.Size = new System.Drawing.Size( 330, 26 );
			this.textBoxTitle.TabIndex = 0;
			this.toolTip.SetToolTip( this.textBoxTitle, "The title to give to the document (optional). You should only use that for import" +
					"ant documents." );
			this.textBoxTitle.TextChanged += new System.EventHandler( this.textBoxTitle_TextChanged );
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label2.Location = new System.Drawing.Point( 33, 87 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 95, 20 );
			this.label2.TabIndex = 2;
			this.label2.Text = "Issue Date :";
			this.toolTip.SetToolTip( this.label2, "The date at which the document was issued." );
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label3.Location = new System.Drawing.Point( 33, 113 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 129, 20 );
			this.label3.TabIndex = 2;
			this.label3.Text = "Reception Date :";
			this.toolTip.SetToolTip( this.label3, "The date at which the document was received by you." );
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label5.Location = new System.Drawing.Point( 33, 137 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 86, 20 );
			this.label5.TabIndex = 2;
			this.label5.Text = "Due Date :";
			this.toolTip.SetToolTip( this.label5, "An optional due date, for a bill for example." );
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label4.Location = new System.Drawing.Point( 33, 53 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 116, 20 );
			this.label4.TabIndex = 2;
			this.label4.Text = "Creation Date :";
			this.toolTip.SetToolTip( this.label4, "The date at which the document was created in the database." );
			// 
			// dateTimePickerCreation
			// 
			this.dateTimePickerCreation.CalendarFont = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.dateTimePickerCreation.Enabled = false;
			this.dateTimePickerCreation.Location = new System.Drawing.Point( 163, 53 );
			this.dateTimePickerCreation.Name = "dateTimePickerCreation";
			this.dateTimePickerCreation.Size = new System.Drawing.Size( 190, 20 );
			this.dateTimePickerCreation.TabIndex = 1;
			this.toolTip.SetToolTip( this.dateTimePickerCreation, "The date at which the document was created in the database." );
			this.dateTimePickerCreation.Value = new System.DateTime( 2011, 9, 17, 21, 23, 0, 0 );
			// 
			// dateTimePickerIssue
			// 
			this.dateTimePickerIssue.CalendarFont = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.dateTimePickerIssue.Location = new System.Drawing.Point( 163, 86 );
			this.dateTimePickerIssue.Name = "dateTimePickerIssue";
			this.dateTimePickerIssue.Size = new System.Drawing.Size( 190, 20 );
			this.dateTimePickerIssue.TabIndex = 2;
			this.toolTip.SetToolTip( this.dateTimePickerIssue, "The date at which the document was issued." );
			this.dateTimePickerIssue.CloseUp += new System.EventHandler( this.dateTimePickerIssue_CloseUp );
			// 
			// dateTimePickerReception
			// 
			this.dateTimePickerReception.CalendarFont = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.dateTimePickerReception.Location = new System.Drawing.Point( 163, 112 );
			this.dateTimePickerReception.Name = "dateTimePickerReception";
			this.dateTimePickerReception.Size = new System.Drawing.Size( 190, 20 );
			this.dateTimePickerReception.TabIndex = 3;
			this.toolTip.SetToolTip( this.dateTimePickerReception, "The date at which the document was received by you." );
			this.dateTimePickerReception.CloseUp += new System.EventHandler( this.dateTimePickerReception_CloseUp );
			// 
			// dateTimePickerDue
			// 
			this.dateTimePickerDue.CalendarFont = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.dateTimePickerDue.Location = new System.Drawing.Point( 163, 136 );
			this.dateTimePickerDue.Name = "dateTimePickerDue";
			this.dateTimePickerDue.Size = new System.Drawing.Size( 190, 20 );
			this.dateTimePickerDue.TabIndex = 4;
			this.toolTip.SetToolTip( this.dateTimePickerDue, "An optional due date, for a bill for example." );
			this.dateTimePickerDue.CloseUp += new System.EventHandler( this.dateTimePickerDue_CloseUp );
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label6.Location = new System.Drawing.Point( 33, 173 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 114, 20 );
			this.label6.TabIndex = 2;
			this.label6.Text = "Cash Amount :";
			this.toolTip.SetToolTip( this.label6, "An optional amount of cash mentioned by the document." );
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label7.Location = new System.Drawing.Point( 12, 221 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 135, 20 );
			this.label7.TabIndex = 2;
			this.label7.Text = "Issuer Institution :";
			this.toolTip.SetToolTip( this.label7, "The institution that issued the document." );
			// 
			// textBoxInstitution
			// 
			this.textBoxInstitution.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxInstitution.BackColor = System.Drawing.Color.LightSteelBlue;
			this.textBoxInstitution.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxInstitution.Location = new System.Drawing.Point( 153, 218 );
			this.textBoxInstitution.Name = "textBoxInstitution";
			this.textBoxInstitution.ReadOnly = true;
			this.textBoxInstitution.Size = new System.Drawing.Size( 280, 26 );
			this.textBoxInstitution.TabIndex = 6;
			this.toolTip.SetToolTip( this.textBoxInstitution, "The institution that issued the document." );
			this.textBoxInstitution.Click += new System.EventHandler( this.textBoxInstitution_Click );
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label8.Location = new System.Drawing.Point( 12, 258 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 225, 20 );
			this.label8.TabIndex = 2;
			this.label8.Text = "Tags (separate with commas) :";
			// 
			// textBoxTags
			// 
			this.textBoxTags.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTags.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxTags.Location = new System.Drawing.Point( 16, 290 );
			this.textBoxTags.Name = "textBoxTags";
			this.textBoxTags.Size = new System.Drawing.Size( 456, 26 );
			this.textBoxTags.TabIndex = 10;
			this.textBoxTags.Validated += new System.EventHandler( this.textBoxTags_Validated );
			// 
			// buttonPickTag
			// 
			this.buttonPickTag.Enabled = false;
			this.buttonPickTag.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonPickTag.Location = new System.Drawing.Point( 243, 252 );
			this.buttonPickTag.Name = "buttonPickTag";
			this.buttonPickTag.Size = new System.Drawing.Size( 33, 32 );
			this.buttonPickTag.TabIndex = 8;
			this.buttonPickTag.Text = "...";
			this.toolTip.SetToolTip( this.buttonPickTag, "Click to pick and add a tag for the document." );
			this.buttonPickTag.UseVisualStyleBackColor = true;
			this.buttonPickTag.Click += new System.EventHandler( this.buttonPickTag_Click );
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label9.Location = new System.Drawing.Point( 12, 478 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 62, 20 );
			this.label9.TabIndex = 2;
			this.label9.Text = "Pages :";
			// 
			// toolTip
			// 
			this.toolTip.IsBalloon = true;
			this.toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.toolTip.ToolTipTitle = "Information";
			// 
			// buttonRemovePage
			// 
			this.buttonRemovePage.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonRemovePage.BackgroundImage = global::Pognac.Properties.Resources.Remove_icon;
			this.buttonRemovePage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonRemovePage.Enabled = false;
			this.buttonRemovePage.Location = new System.Drawing.Point( 443, 561 );
			this.buttonRemovePage.Name = "buttonRemovePage";
			this.buttonRemovePage.Size = new System.Drawing.Size( 29, 29 );
			this.buttonRemovePage.TabIndex = 14;
			this.toolTip.SetToolTip( this.buttonRemovePage, "Click to remove selected page from the document." );
			this.buttonRemovePage.UseVisualStyleBackColor = true;
			this.buttonRemovePage.Click += new System.EventHandler( this.buttonRemovePage_Click );
			// 
			// buttonEditSelectedPage
			// 
			this.buttonEditSelectedPage.BackgroundImage = global::Pognac.Properties.Resources.document_icon;
			this.buttonEditSelectedPage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonEditSelectedPage.Enabled = false;
			this.buttonEditSelectedPage.Location = new System.Drawing.Point( 12, 538 );
			this.buttonEditSelectedPage.Name = "buttonEditSelectedPage";
			this.buttonEditSelectedPage.Size = new System.Drawing.Size( 58, 52 );
			this.buttonEditSelectedPage.TabIndex = 12;
			this.toolTip.SetToolTip( this.buttonEditSelectedPage, "Click to edit selected page." );
			this.buttonEditSelectedPage.UseVisualStyleBackColor = true;
			this.buttonEditSelectedPage.Click += new System.EventHandler( this.buttonEditSelectedPage_Click );
			// 
			// buttonCreatePage
			// 
			this.buttonCreatePage.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCreatePage.BackgroundImage = global::Pognac.Properties.Resources.document_icon_ADD;
			this.buttonCreatePage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonCreatePage.Enabled = false;
			this.buttonCreatePage.Location = new System.Drawing.Point( 443, 527 );
			this.buttonCreatePage.Name = "buttonCreatePage";
			this.buttonCreatePage.Size = new System.Drawing.Size( 29, 29 );
			this.buttonCreatePage.TabIndex = 13;
			this.toolTip.SetToolTip( this.buttonCreatePage, "Click to add new pages to the document." );
			this.buttonCreatePage.UseVisualStyleBackColor = true;
			this.buttonCreatePage.Click += new System.EventHandler( this.buttonCreatePage_Click );
			// 
			// buttonCreateTag
			// 
			this.buttonCreateTag.BackgroundImage = global::Pognac.Properties.Resources.New;
			this.buttonCreateTag.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.buttonCreateTag.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonCreateTag.Location = new System.Drawing.Point( 282, 252 );
			this.buttonCreateTag.Name = "buttonCreateTag";
			this.buttonCreateTag.Size = new System.Drawing.Size( 33, 32 );
			this.buttonCreateTag.TabIndex = 9;
			this.toolTip.SetToolTip( this.buttonCreateTag, "Click to create a new tag." );
			this.buttonCreateTag.UseVisualStyleBackColor = true;
			this.buttonCreateTag.Click += new System.EventHandler( this.buttonCreateTag_Click );
			// 
			// buttonCreateInstitution
			// 
			this.buttonCreateInstitution.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCreateInstitution.BackgroundImage = global::Pognac.Properties.Resources.New;
			this.buttonCreateInstitution.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.buttonCreateInstitution.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonCreateInstitution.Location = new System.Drawing.Point( 439, 215 );
			this.buttonCreateInstitution.Name = "buttonCreateInstitution";
			this.buttonCreateInstitution.Size = new System.Drawing.Size( 33, 32 );
			this.buttonCreateInstitution.TabIndex = 7;
			this.toolTip.SetToolTip( this.buttonCreateInstitution, "Click to create a new issuer institution." );
			this.buttonCreateInstitution.UseVisualStyleBackColor = true;
			this.buttonCreateInstitution.Click += new System.EventHandler( this.buttonCreateInstitution_Click );
			// 
			// buttonDelete
			// 
			this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDelete.BackgroundImage = global::Pognac.Properties.Resources.Trashcan;
			this.buttonDelete.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonDelete.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonDelete.Location = new System.Drawing.Point( 427, 50 );
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.Size = new System.Drawing.Size( 45, 45 );
			this.buttonDelete.TabIndex = 16;
			this.toolTip.SetToolTip( this.buttonDelete, "Press this button to delete the document (it will ask confirmation again before d" +
					"oing so)" );
			this.buttonDelete.UseVisualStyleBackColor = true;
			this.buttonDelete.Click += new System.EventHandler( this.buttonDelete_Click );
			// 
			// buttonMovePageLeft
			// 
			this.buttonMovePageLeft.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonMovePageLeft.BackgroundImage = global::Pognac.Properties.Resources.left_arrow;
			this.buttonMovePageLeft.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonMovePageLeft.Enabled = false;
			this.buttonMovePageLeft.Location = new System.Drawing.Point( 12, 501 );
			this.buttonMovePageLeft.Name = "buttonMovePageLeft";
			this.buttonMovePageLeft.Size = new System.Drawing.Size( 26, 31 );
			this.buttonMovePageLeft.TabIndex = 13;
			this.toolTip.SetToolTip( this.buttonMovePageLeft, "Move selected page to the left" );
			this.buttonMovePageLeft.UseVisualStyleBackColor = true;
			this.buttonMovePageLeft.Click += new System.EventHandler( this.buttonMovePageLeft_Click );
			// 
			// buttonMovePageRight
			// 
			this.buttonMovePageRight.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonMovePageRight.BackgroundImage = global::Pognac.Properties.Resources.right_arrow;
			this.buttonMovePageRight.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.buttonMovePageRight.Enabled = false;
			this.buttonMovePageRight.Location = new System.Drawing.Point( 44, 501 );
			this.buttonMovePageRight.Name = "buttonMovePageRight";
			this.buttonMovePageRight.Size = new System.Drawing.Size( 26, 31 );
			this.buttonMovePageRight.TabIndex = 13;
			this.toolTip.SetToolTip( this.buttonMovePageRight, "Move selected page to the right" );
			this.buttonMovePageRight.UseVisualStyleBackColor = true;
			this.buttonMovePageRight.Click += new System.EventHandler( this.buttonMovePageRight_Click );
			// 
			// annotationControl
			// 
			this.annotationControl.Annotation = null;
			this.annotationControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.annotationControl.Enabled = false;
			this.annotationControl.Location = new System.Drawing.Point( 3, 16 );
			this.annotationControl.Name = "annotationControl";
			this.annotationControl.Size = new System.Drawing.Size( 454, 124 );
			this.annotationControl.TabIndex = 0;
			this.toolTip.SetToolTip( this.annotationControl, "Add some personnal notes to the document." );
			// 
			// floatTrackbarControlAmount
			// 
			this.floatTrackbarControlAmount.Location = new System.Drawing.Point( 163, 173 );
			this.floatTrackbarControlAmount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlAmount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlAmount.Name = "floatTrackbarControlAmount";
			this.floatTrackbarControlAmount.RangeMin = 0F;
			this.floatTrackbarControlAmount.Size = new System.Drawing.Size( 190, 20 );
			this.floatTrackbarControlAmount.TabIndex = 5;
			this.toolTip.SetToolTip( this.floatTrackbarControlAmount, "An optional amount of cash mentioned by the document." );
			this.floatTrackbarControlAmount.Value = 0F;
			this.floatTrackbarControlAmount.VisibleRangeMax = 1000F;
			this.floatTrackbarControlAmount.ValueChanged += new Pognac.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlAmount_ValueChanged );
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add( this.annotationControl );
			this.groupBox1.Location = new System.Drawing.Point( 12, 322 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 460, 143 );
			this.groupBox1.TabIndex = 9;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Notes";
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// thumbnailBrowser
			// 
			this.thumbnailBrowser.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.thumbnailBrowser.Attachments = new Pognac.Documents.Attachment[0];
			this.thumbnailBrowser.DoubleClickOpensViewer = true;
			this.thumbnailBrowser.Location = new System.Drawing.Point( 76, 475 );
			this.thumbnailBrowser.Name = "thumbnailBrowser";
			this.thumbnailBrowser.Selection = null;
			this.thumbnailBrowser.Size = new System.Drawing.Size( 361, 115 );
			this.thumbnailBrowser.TabIndex = 11;
			this.thumbnailBrowser.SelectionChanged += new System.EventHandler( this.thumbnailBrowser_SelectionChanged );
			// 
			// DocumentForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 484, 636 );
			this.Controls.Add( this.thumbnailBrowser );
			this.Controls.Add( this.groupBox1 );
			this.Controls.Add( this.buttonRemovePage );
			this.Controls.Add( this.buttonEditSelectedPage );
			this.Controls.Add( this.buttonMovePageRight );
			this.Controls.Add( this.buttonMovePageLeft );
			this.Controls.Add( this.buttonCreatePage );
			this.Controls.Add( this.floatTrackbarControlAmount );
			this.Controls.Add( this.dateTimePickerDue );
			this.Controls.Add( this.dateTimePickerReception );
			this.Controls.Add( this.dateTimePickerIssue );
			this.Controls.Add( this.dateTimePickerCreation );
			this.Controls.Add( this.textBoxTags );
			this.Controls.Add( this.textBoxInstitution );
			this.Controls.Add( this.textBoxTitle );
			this.Controls.Add( this.label6 );
			this.Controls.Add( this.label5 );
			this.Controls.Add( this.label4 );
			this.Controls.Add( this.label3 );
			this.Controls.Add( this.label9 );
			this.Controls.Add( this.label8 );
			this.Controls.Add( this.label7 );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.buttonCreateTag );
			this.Controls.Add( this.buttonCreateInstitution );
			this.Controls.Add( this.buttonPickTag );
			this.Controls.Add( this.buttonDelete );
			this.Controls.Add( this.buttonSave );
			this.Enabled = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.MinimumSize = new System.Drawing.Size( 490, 660 );
			this.Name = "DocumentForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Document Editor";
			this.groupBox1.ResumeLayout( false );
			((System.ComponentModel.ISupportInitialize) (this.errorProvider)).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSave;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxTitle;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.DateTimePicker dateTimePickerCreation;
		private System.Windows.Forms.DateTimePicker dateTimePickerIssue;
		private System.Windows.Forms.DateTimePicker dateTimePickerReception;
		private System.Windows.Forms.DateTimePicker dateTimePickerDue;
		private FloatTrackbarControl floatTrackbarControlAmount;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox textBoxInstitution;
		private System.Windows.Forms.Button buttonCreateInstitution;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox textBoxTags;
		private System.Windows.Forms.Button buttonPickTag;
		private System.Windows.Forms.Button buttonCreatePage;
		private System.Windows.Forms.Button buttonCreateTag;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.ToolTip toolTip;
		private AnnotationControl annotationControl;
		private System.Windows.Forms.GroupBox groupBox1;
		private ThumbnailBrowser thumbnailBrowser;
		private System.Windows.Forms.Button buttonRemovePage;
		private System.Windows.Forms.Button buttonDelete;
		private System.Windows.Forms.Button buttonEditSelectedPage;
		private System.Windows.Forms.Button buttonMovePageLeft;
		private System.Windows.Forms.Button buttonMovePageRight;
		private System.Windows.Forms.ErrorProvider errorProvider;
	}
}