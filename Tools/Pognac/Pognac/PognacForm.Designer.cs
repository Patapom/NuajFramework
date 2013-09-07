namespace Pognac
{
	partial class PognacForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( PognacForm ) );
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabPageDocuments = new System.Windows.Forms.TabPage();
			this.groupBoxQueryResults = new System.Windows.Forms.GroupBox();
			this.textBoxQuery = new System.Windows.Forms.TextBox();
			this.labelWorkingDirectory = new System.Windows.Forms.Label();
			this.labelDocumentsToProcess = new System.Windows.Forms.Label();
			this.labelRegisteredDocumentsCount = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonQuery = new System.Windows.Forms.Button();
			this.buttonAddInstitution = new System.Windows.Forms.Button();
			this.buttonAddTag = new System.Windows.Forms.Button();
			this.buttonProcessDocuments = new System.Windows.Forms.Button();
			this.tabPageTags = new System.Windows.Forms.TabPage();
			this.groupBoxDocumentsWithTag = new System.Windows.Forms.GroupBox();
			this.label5 = new System.Windows.Forms.Label();
			this.listBoxTags = new System.Windows.Forms.ListBox();
			this.buttonCreateTag = new System.Windows.Forms.Button();
			this.tabPageInstitutions = new System.Windows.Forms.TabPage();
			this.groupBoxDocumentsFromInstitution = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.listBoxInstitutions = new System.Windows.Forms.ListBox();
			this.buttonCreateInstitution = new System.Windows.Forms.Button();
			this.timerIdle = new System.Windows.Forms.Timer( this.components );
			this.notifyIcon = new System.Windows.Forms.NotifyIcon( this.components );
			this.contextMenuStripTray = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.openPognacToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dateTimePickerQueryFrom = new System.Windows.Forms.DateTimePicker();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.dateTimePickerQueryTo = new System.Windows.Forms.DateTimePicker();
			this.documentsListQuery = new Pognac.DocumentsListControl();
			this.documentsListTags = new Pognac.DocumentsListControl();
			this.documentsListInstitutions = new Pognac.DocumentsListControl();
			this.processWaitingFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tabPageDocuments.SuspendLayout();
			this.groupBoxQueryResults.SuspendLayout();
			this.tabPageTags.SuspendLayout();
			this.groupBoxDocumentsWithTag.SuspendLayout();
			this.tabPageInstitutions.SuspendLayout();
			this.groupBoxDocumentsFromInstitution.SuspendLayout();
			this.contextMenuStripTray.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.preferencesToolStripMenuItem} );
			this.menuStrip1.Location = new System.Drawing.Point( 0, 0 );
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size( 763, 24 );
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// preferencesToolStripMenuItem
			// 
			this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
			this.preferencesToolStripMenuItem.Size = new System.Drawing.Size( 80, 20 );
			this.preferencesToolStripMenuItem.Text = "&Preferences";
			this.preferencesToolStripMenuItem.Click += new System.EventHandler( this.preferencesToolStripMenuItem_Click );
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add( this.tabPageDocuments );
			this.tabControl.Controls.Add( this.tabPageTags );
			this.tabControl.Controls.Add( this.tabPageInstitutions );
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.Location = new System.Drawing.Point( 0, 24 );
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size( 763, 496 );
			this.tabControl.TabIndex = 2;
			// 
			// tabPageDocuments
			// 
			this.tabPageDocuments.Controls.Add( this.dateTimePickerQueryTo );
			this.tabPageDocuments.Controls.Add( this.label8 );
			this.tabPageDocuments.Controls.Add( this.dateTimePickerQueryFrom );
			this.tabPageDocuments.Controls.Add( this.label7 );
			this.tabPageDocuments.Controls.Add( this.groupBoxQueryResults );
			this.tabPageDocuments.Controls.Add( this.textBoxQuery );
			this.tabPageDocuments.Controls.Add( this.labelWorkingDirectory );
			this.tabPageDocuments.Controls.Add( this.labelDocumentsToProcess );
			this.tabPageDocuments.Controls.Add( this.labelRegisteredDocumentsCount );
			this.tabPageDocuments.Controls.Add( this.label4 );
			this.tabPageDocuments.Controls.Add( this.label3 );
			this.tabPageDocuments.Controls.Add( this.label2 );
			this.tabPageDocuments.Controls.Add( this.label1 );
			this.tabPageDocuments.Controls.Add( this.buttonQuery );
			this.tabPageDocuments.Controls.Add( this.buttonAddInstitution );
			this.tabPageDocuments.Controls.Add( this.buttonAddTag );
			this.tabPageDocuments.Controls.Add( this.buttonProcessDocuments );
			this.tabPageDocuments.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageDocuments.Name = "tabPageDocuments";
			this.tabPageDocuments.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageDocuments.Size = new System.Drawing.Size( 755, 470 );
			this.tabPageDocuments.TabIndex = 0;
			this.tabPageDocuments.Text = "Documents";
			this.tabPageDocuments.UseVisualStyleBackColor = true;
			// 
			// groupBoxQueryResults
			// 
			this.groupBoxQueryResults.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxQueryResults.Controls.Add( this.documentsListQuery );
			this.groupBoxQueryResults.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.groupBoxQueryResults.Location = new System.Drawing.Point( 6, 214 );
			this.groupBoxQueryResults.Name = "groupBoxQueryResults";
			this.groupBoxQueryResults.Size = new System.Drawing.Size( 741, 248 );
			this.groupBoxQueryResults.TabIndex = 3;
			this.groupBoxQueryResults.TabStop = false;
			this.groupBoxQueryResults.Text = "Results";
			// 
			// textBoxQuery
			// 
			this.textBoxQuery.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxQuery.Enabled = false;
			this.textBoxQuery.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxQuery.Location = new System.Drawing.Point( 6, 184 );
			this.textBoxQuery.Name = "textBoxQuery";
			this.textBoxQuery.Size = new System.Drawing.Size( 683, 26 );
			this.textBoxQuery.TabIndex = 2;
			this.textBoxQuery.KeyDown += new System.Windows.Forms.KeyEventHandler( this.textBoxQuery_KeyDown );
			// 
			// labelWorkingDirectory
			// 
			this.labelWorkingDirectory.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelWorkingDirectory.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.labelWorkingDirectory.Location = new System.Drawing.Point( 228, 3 );
			this.labelWorkingDirectory.Name = "labelWorkingDirectory";
			this.labelWorkingDirectory.Size = new System.Drawing.Size( 519, 20 );
			this.labelWorkingDirectory.TabIndex = 1;
			this.labelWorkingDirectory.Text = "<NOT SET => GO TO PREFERENCES>";
			// 
			// labelDocumentsToProcess
			// 
			this.labelDocumentsToProcess.AutoSize = true;
			this.labelDocumentsToProcess.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.labelDocumentsToProcess.Location = new System.Drawing.Point( 240, 70 );
			this.labelDocumentsToProcess.Name = "labelDocumentsToProcess";
			this.labelDocumentsToProcess.Size = new System.Drawing.Size( 18, 20 );
			this.labelDocumentsToProcess.TabIndex = 1;
			this.labelDocumentsToProcess.Text = "0";
			// 
			// labelRegisteredDocumentsCount
			// 
			this.labelRegisteredDocumentsCount.AutoSize = true;
			this.labelRegisteredDocumentsCount.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.labelRegisteredDocumentsCount.Location = new System.Drawing.Point( 240, 36 );
			this.labelRegisteredDocumentsCount.Name = "labelRegisteredDocumentsCount";
			this.labelRegisteredDocumentsCount.Size = new System.Drawing.Size( 18, 20 );
			this.labelRegisteredDocumentsCount.TabIndex = 1;
			this.labelRegisteredDocumentsCount.Text = "0";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label4.Location = new System.Drawing.Point( 5, 135 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 310, 20 );
			this.label4.TabIndex = 1;
			this.label4.Text = "Document Query (separate with commas) :";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label3.Location = new System.Drawing.Point( 6, 70 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 226, 20 );
			this.label3.TabIndex = 1;
			this.label3.Text = "Files Waiting to be Processed :";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label2.Location = new System.Drawing.Point( 6, 36 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 228, 20 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Registered Documents Count :";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label1.Location = new System.Drawing.Point( 8, 3 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 214, 20 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Current Working Directory is :";
			// 
			// buttonQuery
			// 
			this.buttonQuery.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonQuery.Enabled = false;
			this.buttonQuery.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonQuery.Location = new System.Drawing.Point( 695, 183 );
			this.buttonQuery.Name = "buttonQuery";
			this.buttonQuery.Size = new System.Drawing.Size( 52, 28 );
			this.buttonQuery.TabIndex = 0;
			this.buttonQuery.Text = "Go";
			this.buttonQuery.UseVisualStyleBackColor = true;
			this.buttonQuery.Click += new System.EventHandler( this.buttonQuery_Click );
			// 
			// buttonAddInstitution
			// 
			this.buttonAddInstitution.Enabled = false;
			this.buttonAddInstitution.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonAddInstitution.Location = new System.Drawing.Point( 423, 131 );
			this.buttonAddInstitution.Name = "buttonAddInstitution";
			this.buttonAddInstitution.Size = new System.Drawing.Size( 140, 28 );
			this.buttonAddInstitution.TabIndex = 4;
			this.buttonAddInstitution.Text = "Add Institution";
			this.buttonAddInstitution.UseVisualStyleBackColor = true;
			this.buttonAddInstitution.Click += new System.EventHandler( this.buttonAddInstitution_Click );
			// 
			// buttonAddTag
			// 
			this.buttonAddTag.Enabled = false;
			this.buttonAddTag.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonAddTag.Location = new System.Drawing.Point( 318, 131 );
			this.buttonAddTag.Name = "buttonAddTag";
			this.buttonAddTag.Size = new System.Drawing.Size( 99, 28 );
			this.buttonAddTag.TabIndex = 3;
			this.buttonAddTag.Text = "Add Tag";
			this.buttonAddTag.UseVisualStyleBackColor = true;
			this.buttonAddTag.Click += new System.EventHandler( this.buttonAddTag_Click );
			// 
			// buttonProcessDocuments
			// 
			this.buttonProcessDocuments.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonProcessDocuments.Location = new System.Drawing.Point( 315, 32 );
			this.buttonProcessDocuments.Name = "buttonProcessDocuments";
			this.buttonProcessDocuments.Size = new System.Drawing.Size( 245, 58 );
			this.buttonProcessDocuments.TabIndex = 2;
			this.buttonProcessDocuments.Text = "Process Waiting Files";
			this.buttonProcessDocuments.UseVisualStyleBackColor = true;
			this.buttonProcessDocuments.Visible = false;
			this.buttonProcessDocuments.Click += new System.EventHandler( this.buttonProcessDocuments_Click );
			// 
			// tabPageTags
			// 
			this.tabPageTags.Controls.Add( this.groupBoxDocumentsWithTag );
			this.tabPageTags.Controls.Add( this.label5 );
			this.tabPageTags.Controls.Add( this.listBoxTags );
			this.tabPageTags.Controls.Add( this.buttonCreateTag );
			this.tabPageTags.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageTags.Name = "tabPageTags";
			this.tabPageTags.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageTags.Size = new System.Drawing.Size( 755, 470 );
			this.tabPageTags.TabIndex = 1;
			this.tabPageTags.Text = "Tags";
			this.tabPageTags.UseVisualStyleBackColor = true;
			// 
			// groupBoxDocumentsWithTag
			// 
			this.groupBoxDocumentsWithTag.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxDocumentsWithTag.Controls.Add( this.documentsListTags );
			this.groupBoxDocumentsWithTag.Location = new System.Drawing.Point( 257, 6 );
			this.groupBoxDocumentsWithTag.Name = "groupBoxDocumentsWithTag";
			this.groupBoxDocumentsWithTag.Size = new System.Drawing.Size( 431, 408 );
			this.groupBoxDocumentsWithTag.TabIndex = 5;
			this.groupBoxDocumentsWithTag.TabStop = false;
			this.groupBoxDocumentsWithTag.Text = "Documents using Selected Tag";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label5.Location = new System.Drawing.Point( 8, 15 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 111, 20 );
			this.label5.TabIndex = 3;
			this.label5.Text = "Existing Tags :";
			// 
			// listBoxTags
			// 
			this.listBoxTags.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.listBoxTags.Enabled = false;
			this.listBoxTags.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.listBoxTags.FormattingEnabled = true;
			this.listBoxTags.IntegralHeight = false;
			this.listBoxTags.ItemHeight = 20;
			this.listBoxTags.Location = new System.Drawing.Point( 12, 45 );
			this.listBoxTags.Name = "listBoxTags";
			this.listBoxTags.Size = new System.Drawing.Size( 239, 366 );
			this.listBoxTags.TabIndex = 2;
			this.listBoxTags.SelectedIndexChanged += new System.EventHandler( this.listBoxTags_SelectedIndexChanged );
			this.listBoxTags.DoubleClick += new System.EventHandler( this.listBoxTags_DoubleClick );
			// 
			// buttonCreateTag
			// 
			this.buttonCreateTag.Enabled = false;
			this.buttonCreateTag.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonCreateTag.Location = new System.Drawing.Point( 125, 11 );
			this.buttonCreateTag.Name = "buttonCreateTag";
			this.buttonCreateTag.Size = new System.Drawing.Size( 126, 28 );
			this.buttonCreateTag.TabIndex = 1;
			this.buttonCreateTag.Text = "Create Tag";
			this.buttonCreateTag.UseVisualStyleBackColor = true;
			this.buttonCreateTag.Click += new System.EventHandler( this.buttonCreateTag_Click );
			// 
			// tabPageInstitutions
			// 
			this.tabPageInstitutions.Controls.Add( this.groupBoxDocumentsFromInstitution );
			this.tabPageInstitutions.Controls.Add( this.label6 );
			this.tabPageInstitutions.Controls.Add( this.listBoxInstitutions );
			this.tabPageInstitutions.Controls.Add( this.buttonCreateInstitution );
			this.tabPageInstitutions.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageInstitutions.Name = "tabPageInstitutions";
			this.tabPageInstitutions.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageInstitutions.Size = new System.Drawing.Size( 755, 470 );
			this.tabPageInstitutions.TabIndex = 2;
			this.tabPageInstitutions.Text = "Institutions";
			this.tabPageInstitutions.UseVisualStyleBackColor = true;
			// 
			// groupBoxDocumentsFromInstitution
			// 
			this.groupBoxDocumentsFromInstitution.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxDocumentsFromInstitution.Controls.Add( this.documentsListInstitutions );
			this.groupBoxDocumentsFromInstitution.Location = new System.Drawing.Point( 337, 7 );
			this.groupBoxDocumentsFromInstitution.Name = "groupBoxDocumentsFromInstitution";
			this.groupBoxDocumentsFromInstitution.Size = new System.Drawing.Size( 351, 408 );
			this.groupBoxDocumentsFromInstitution.TabIndex = 7;
			this.groupBoxDocumentsFromInstitution.TabStop = false;
			this.groupBoxDocumentsFromInstitution.Text = "Documents using Selected Institution";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label6.Location = new System.Drawing.Point( 8, 15 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 154, 20 );
			this.label6.TabIndex = 6;
			this.label6.Text = "Existing Institutions :";
			// 
			// listBoxInstitutions
			// 
			this.listBoxInstitutions.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.listBoxInstitutions.Enabled = false;
			this.listBoxInstitutions.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.listBoxInstitutions.FormattingEnabled = true;
			this.listBoxInstitutions.IntegralHeight = false;
			this.listBoxInstitutions.ItemHeight = 20;
			this.listBoxInstitutions.Location = new System.Drawing.Point( 12, 45 );
			this.listBoxInstitutions.Name = "listBoxInstitutions";
			this.listBoxInstitutions.Size = new System.Drawing.Size( 319, 367 );
			this.listBoxInstitutions.TabIndex = 5;
			this.listBoxInstitutions.SelectedIndexChanged += new System.EventHandler( this.listBoxInstitutions_SelectedIndexChanged );
			this.listBoxInstitutions.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.listBoxInstitutions_MouseDoubleClick );
			// 
			// buttonCreateInstitution
			// 
			this.buttonCreateInstitution.Enabled = false;
			this.buttonCreateInstitution.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonCreateInstitution.Location = new System.Drawing.Point( 168, 11 );
			this.buttonCreateInstitution.Name = "buttonCreateInstitution";
			this.buttonCreateInstitution.Size = new System.Drawing.Size( 163, 28 );
			this.buttonCreateInstitution.TabIndex = 4;
			this.buttonCreateInstitution.Text = "Create Institution";
			this.buttonCreateInstitution.UseVisualStyleBackColor = true;
			this.buttonCreateInstitution.Click += new System.EventHandler( this.buttonCreateInstitution_Click );
			// 
			// timerIdle
			// 
			this.timerIdle.Enabled = true;
			this.timerIdle.Interval = 5000;
			// 
			// notifyIcon
			// 
			this.notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.notifyIcon.ContextMenuStrip = this.contextMenuStripTray;
			this.notifyIcon.Icon = ((System.Drawing.Icon) (resources.GetObject( "notifyIcon.Icon" )));
			this.notifyIcon.Text = "Pognac";
			this.notifyIcon.Visible = true;
			this.notifyIcon.BalloonTipClicked += new System.EventHandler( this.notifyIcon_BalloonTipClicked );
			this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.notifyIcon_MouseDoubleClick );
			// 
			// contextMenuStripTray
			// 
			this.contextMenuStripTray.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.openPognacToolStripMenuItem,
            this.processWaitingFilesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem} );
			this.contextMenuStripTray.Name = "contextMenuStripTray";
			this.contextMenuStripTray.Size = new System.Drawing.Size( 185, 98 );
			// 
			// openPognacToolStripMenuItem
			// 
			this.openPognacToolStripMenuItem.Name = "openPognacToolStripMenuItem";
			this.openPognacToolStripMenuItem.Size = new System.Drawing.Size( 184, 22 );
			this.openPognacToolStripMenuItem.Text = "Open Pognac";
			this.openPognacToolStripMenuItem.Click += new System.EventHandler( this.openPognacToolStripMenuItem_Click );
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size( 181, 6 );
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size( 184, 22 );
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler( this.exitToolStripMenuItem_Click );
			// 
			// dateTimePickerQueryFrom
			// 
			this.dateTimePickerQueryFrom.CalendarFont = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.dateTimePickerQueryFrom.Location = new System.Drawing.Point( 65, 162 );
			this.dateTimePickerQueryFrom.Name = "dateTimePickerQueryFrom";
			this.dateTimePickerQueryFrom.Size = new System.Drawing.Size( 190, 20 );
			this.dateTimePickerQueryFrom.TabIndex = 6;
			this.dateTimePickerQueryFrom.Value = new System.DateTime( 1975, 9, 12, 18, 43, 0, 0 );
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label7.Location = new System.Drawing.Point( 5, 162 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 54, 20 );
			this.label7.TabIndex = 5;
			this.label7.Text = "From :";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label8.Location = new System.Drawing.Point( 276, 161 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 35, 20 );
			this.label8.TabIndex = 5;
			this.label8.Text = "To :";
			// 
			// dateTimePickerQueryTo
			// 
			this.dateTimePickerQueryTo.CalendarFont = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.dateTimePickerQueryTo.Location = new System.Drawing.Point( 317, 160 );
			this.dateTimePickerQueryTo.Name = "dateTimePickerQueryTo";
			this.dateTimePickerQueryTo.Size = new System.Drawing.Size( 190, 20 );
			this.dateTimePickerQueryTo.TabIndex = 6;
			// 
			// documentsListQuery
			// 
			this.documentsListQuery.Dock = System.Windows.Forms.DockStyle.Fill;
			this.documentsListQuery.Documents = new Pognac.Documents.Document[0];
			this.documentsListQuery.Location = new System.Drawing.Point( 3, 22 );
			this.documentsListQuery.Name = "documentsListQuery";
			this.documentsListQuery.Selection = null;
			this.documentsListQuery.Size = new System.Drawing.Size( 735, 223 );
			this.documentsListQuery.TabIndex = 0;
			this.documentsListQuery.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.documentsListQuery_MouseDoubleClick );
			// 
			// documentsListTags
			// 
			this.documentsListTags.Dock = System.Windows.Forms.DockStyle.Fill;
			this.documentsListTags.Documents = new Pognac.Documents.Document[0];
			this.documentsListTags.Location = new System.Drawing.Point( 3, 16 );
			this.documentsListTags.Name = "documentsListTags";
			this.documentsListTags.Selection = null;
			this.documentsListTags.Size = new System.Drawing.Size( 425, 389 );
			this.documentsListTags.TabIndex = 4;
			this.documentsListTags.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.documentsListTags_MouseDoubleClick );
			// 
			// documentsListInstitutions
			// 
			this.documentsListInstitutions.Dock = System.Windows.Forms.DockStyle.Fill;
			this.documentsListInstitutions.Documents = new Pognac.Documents.Document[0];
			this.documentsListInstitutions.Location = new System.Drawing.Point( 3, 16 );
			this.documentsListInstitutions.Name = "documentsListInstitutions";
			this.documentsListInstitutions.Selection = null;
			this.documentsListInstitutions.Size = new System.Drawing.Size( 345, 389 );
			this.documentsListInstitutions.TabIndex = 4;
			this.documentsListInstitutions.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.documentsListInstitutions_MouseDoubleClick );
			// 
			// processWaitingFilesToolStripMenuItem
			// 
			this.processWaitingFilesToolStripMenuItem.Enabled = false;
			this.processWaitingFilesToolStripMenuItem.Name = "processWaitingFilesToolStripMenuItem";
			this.processWaitingFilesToolStripMenuItem.Size = new System.Drawing.Size( 184, 22 );
			this.processWaitingFilesToolStripMenuItem.Text = "&Process Waiting Files";
			this.processWaitingFilesToolStripMenuItem.Click += new System.EventHandler( this.processWaitingFilesToolStripMenuItem_Click );
			// 
			// PognacForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 763, 520 );
			this.Controls.Add( this.tabControl );
			this.Controls.Add( this.menuStrip1 );
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size( 720, 510 );
			this.Name = "PognacForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Pognac v0.1";
			this.menuStrip1.ResumeLayout( false );
			this.menuStrip1.PerformLayout();
			this.tabControl.ResumeLayout( false );
			this.tabPageDocuments.ResumeLayout( false );
			this.tabPageDocuments.PerformLayout();
			this.groupBoxQueryResults.ResumeLayout( false );
			this.tabPageTags.ResumeLayout( false );
			this.tabPageTags.PerformLayout();
			this.groupBoxDocumentsWithTag.ResumeLayout( false );
			this.tabPageInstitutions.ResumeLayout( false );
			this.tabPageInstitutions.PerformLayout();
			this.groupBoxDocumentsFromInstitution.ResumeLayout( false );
			this.contextMenuStripTray.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageDocuments;
		private System.Windows.Forms.TabPage tabPageTags;
		private System.Windows.Forms.TabPage tabPageInstitutions;
		private System.Windows.Forms.Label labelWorkingDirectory;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelRegisteredDocumentsCount;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label labelDocumentsToProcess;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonProcessDocuments;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBoxQuery;
		private System.Windows.Forms.Button buttonQuery;
		private System.Windows.Forms.Button buttonAddInstitution;
		private System.Windows.Forms.Button buttonAddTag;
		private System.Windows.Forms.GroupBox groupBoxQueryResults;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ListBox listBoxTags;
		private System.Windows.Forms.Button buttonCreateTag;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ListBox listBoxInstitutions;
		private System.Windows.Forms.Button buttonCreateInstitution;
		private DocumentsListControl documentsListQuery;
		private System.Windows.Forms.GroupBox groupBoxDocumentsWithTag;
		private DocumentsListControl documentsListTags;
		private System.Windows.Forms.GroupBox groupBoxDocumentsFromInstitution;
		private DocumentsListControl documentsListInstitutions;
		private System.Windows.Forms.Timer timerIdle;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ContextMenuStrip contextMenuStripTray;
		private System.Windows.Forms.ToolStripMenuItem openPognacToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.DateTimePicker dateTimePickerQueryTo;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.DateTimePicker dateTimePickerQueryFrom;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.ToolStripMenuItem processWaitingFilesToolStripMenuItem;
	}
}

