namespace SequencorEditor
{
	partial class SequencerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadMusicToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.asBinaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.asXMLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copySelectedTrackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copySelectedIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copySelectedKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sequenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createNewParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.createNewIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this.addClipToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeClipToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panelTimeLine = new System.Windows.Forms.Panel();
			this.toolStripMain = new System.Windows.Forms.ToolStrip();
			this.toolStripButtonCreateNewParameter = new System.Windows.Forms.ToolStripButton();
			this.panelMain = new System.Windows.Forms.Panel();
			this.panelTracks = new System.Windows.Forms.Panel();
			this.sequencerControl = new SequencorEditor.SequencerControl();
			this.panelTrackTitles = new System.Windows.Forms.Panel();
			this.openFileDialogProject = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialogProject = new System.Windows.Forms.SaveFileDialog();
			this.saveFileDialogBinary = new System.Windows.Forms.SaveFileDialog();
			this.openFileDialogMusic = new System.Windows.Forms.OpenFileDialog();
			this.menuStrip1.SuspendLayout();
			this.panelTimeLine.SuspendLayout();
			this.toolStripMain.SuspendLayout();
			this.panelMain.SuspendLayout();
			this.panelTracks.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.sequenceToolStripMenuItem,
            this.helpToolStripMenuItem} );
			this.menuStrip1.Location = new System.Drawing.Point( 0, 0 );
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size( 1051, 24 );
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStripMain";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.loadMusicToolStripMenuItem,
            this.toolStripMenuItem1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItem2,
            this.exportToolStripMenuItem,
            this.toolStripMenuItem4,
            this.exitToolStripMenuItem} );
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.fileToolStripMenuItem.Size = new System.Drawing.Size( 37, 20 );
			this.fileToolStripMenuItem.Text = "&File";
			this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler( this.fileToolStripMenuItem_DropDownOpening );
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.newToolStripMenuItem.Text = "&New";
			this.newToolStripMenuItem.Click += new System.EventHandler( this.newToolStripMenuItem_Click );
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.Click += new System.EventHandler( this.openToolStripMenuItem_Click );
			// 
			// loadMusicToolStripMenuItem
			// 
			this.loadMusicToolStripMenuItem.Name = "loadMusicToolStripMenuItem";
			this.loadMusicToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.loadMusicToolStripMenuItem.Text = "Load &Music...";
			this.loadMusicToolStripMenuItem.Click += new System.EventHandler( this.loadMusicToolStripMenuItem_Click );
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size( 192, 6 );
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler( this.saveToolStripMenuItem_Click );
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) (((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.S)));
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.saveAsToolStripMenuItem.Text = "S&ave As...";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler( this.saveAsToolStripMenuItem_Click );
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size( 192, 6 );
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.asBinaryToolStripMenuItem,
            this.asXMLToolStripMenuItem} );
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.exportToolStripMenuItem.Text = "&Export";
			// 
			// asBinaryToolStripMenuItem
			// 
			this.asBinaryToolStripMenuItem.Name = "asBinaryToolStripMenuItem";
			this.asBinaryToolStripMenuItem.Size = new System.Drawing.Size( 132, 22 );
			this.asBinaryToolStripMenuItem.Text = "As &Binary...";
			this.asBinaryToolStripMenuItem.Click += new System.EventHandler( this.asBinaryToolStripMenuItem_Click );
			// 
			// asXMLToolStripMenuItem
			// 
			this.asXMLToolStripMenuItem.Enabled = false;
			this.asXMLToolStripMenuItem.Name = "asXMLToolStripMenuItem";
			this.asXMLToolStripMenuItem.Size = new System.Drawing.Size( 132, 22 );
			this.asXMLToolStripMenuItem.Text = "As &XML...";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size( 192, 6 );
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size( 195, 22 );
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler( this.exitToolStripMenuItem_Click );
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.copySelectedTrackToolStripMenuItem,
            this.copySelectedIntervalToolStripMenuItem,
            this.copySelectedKeyToolStripMenuItem,
            this.pasteToolStripMenuItem} );
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size( 39, 20 );
			this.editToolStripMenuItem.Text = "&Edit";
			this.editToolStripMenuItem.DropDownOpening += new System.EventHandler( this.editToolStripMenuItem_DropDownOpening );
			// 
			// copySelectedTrackToolStripMenuItem
			// 
			this.copySelectedTrackToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.copySelectedTrackToolStripMenuItem.Name = "copySelectedTrackToolStripMenuItem";
			this.copySelectedTrackToolStripMenuItem.Size = new System.Drawing.Size( 191, 22 );
			this.copySelectedTrackToolStripMenuItem.Text = "Copy Selected &Track";
			this.copySelectedTrackToolStripMenuItem.Click += new System.EventHandler( this.copySelectedTrackToolStripMenuItem_Click );
			// 
			// copySelectedIntervalToolStripMenuItem
			// 
			this.copySelectedIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.copySelectedIntervalToolStripMenuItem.Name = "copySelectedIntervalToolStripMenuItem";
			this.copySelectedIntervalToolStripMenuItem.Size = new System.Drawing.Size( 191, 22 );
			this.copySelectedIntervalToolStripMenuItem.Text = "Copy Selected &Interval";
			this.copySelectedIntervalToolStripMenuItem.Click += new System.EventHandler( this.copySelectedIntervalToolStripMenuItem_Click );
			// 
			// copySelectedKeyToolStripMenuItem
			// 
			this.copySelectedKeyToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.copySelectedKeyToolStripMenuItem.Name = "copySelectedKeyToolStripMenuItem";
			this.copySelectedKeyToolStripMenuItem.Size = new System.Drawing.Size( 191, 22 );
			this.copySelectedKeyToolStripMenuItem.Text = "Copy Selected &Key";
			this.copySelectedKeyToolStripMenuItem.Click += new System.EventHandler( this.copySelectedKeyToolStripMenuItem_Click );
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Paste;
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size( 191, 22 );
			this.pasteToolStripMenuItem.Text = "&Paste";
			this.pasteToolStripMenuItem.Click += new System.EventHandler( this.pasteToolStripMenuItem_Click );
			// 
			// sequenceToolStripMenuItem
			// 
			this.sequenceToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.createNewParameterToolStripMenuItem,
            this.editParameterToolStripMenuItem,
            this.deleteParameterToolStripMenuItem,
            this.toolStripMenuItem3,
            this.createNewIntervalToolStripMenuItem,
            this.deleteIntervalToolStripMenuItem,
            this.toolStripMenuItem5,
            this.addClipToolStripMenuItem,
            this.removeClipToolStripMenuItem} );
			this.sequenceToolStripMenuItem.Name = "sequenceToolStripMenuItem";
			this.sequenceToolStripMenuItem.Size = new System.Drawing.Size( 70, 20 );
			this.sequenceToolStripMenuItem.Text = "&Sequence";
			this.sequenceToolStripMenuItem.DropDownOpening += new System.EventHandler( this.sequenceToolStripMenuItem_DropDownOpening );
			// 
			// createNewParameterToolStripMenuItem
			// 
			this.createNewParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Property_Indicator;
			this.createNewParameterToolStripMenuItem.Name = "createNewParameterToolStripMenuItem";
			this.createNewParameterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.createNewParameterToolStripMenuItem.Text = "&Create New Parameter...";
			this.createNewParameterToolStripMenuItem.Click += new System.EventHandler( this.createNewParameterToolStripMenuItem_Click );
			// 
			// editParameterToolStripMenuItem
			// 
			this.editParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Variant___Random;
			this.editParameterToolStripMenuItem.Name = "editParameterToolStripMenuItem";
			this.editParameterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.editParameterToolStripMenuItem.Text = "&Edit Parameter...";
			this.editParameterToolStripMenuItem.Click += new System.EventHandler( this.editParameterToolStripMenuItem_Click );
			// 
			// deleteParameterToolStripMenuItem
			// 
			this.deleteParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Remove;
			this.deleteParameterToolStripMenuItem.Name = "deleteParameterToolStripMenuItem";
			this.deleteParameterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.deleteParameterToolStripMenuItem.Text = "&Delete Parameter";
			this.deleteParameterToolStripMenuItem.Click += new System.EventHandler( this.deleteParameterToolStripMenuItem_Click );
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size( 198, 6 );
			// 
			// createNewIntervalToolStripMenuItem
			// 
			this.createNewIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Interval;
			this.createNewIntervalToolStripMenuItem.Name = "createNewIntervalToolStripMenuItem";
			this.createNewIntervalToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.createNewIntervalToolStripMenuItem.Text = "C&reate New Interval";
			// 
			// deleteIntervalToolStripMenuItem
			// 
			this.deleteIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Remove;
			this.deleteIntervalToolStripMenuItem.Name = "deleteIntervalToolStripMenuItem";
			this.deleteIntervalToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.deleteIntervalToolStripMenuItem.Text = "De&lete Interval";
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size( 198, 6 );
			// 
			// addClipToolStripMenuItem
			// 
			this.addClipToolStripMenuItem.Enabled = false;
			this.addClipToolStripMenuItem.Name = "addClipToolStripMenuItem";
			this.addClipToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.addClipToolStripMenuItem.Text = "Add Cl&ip";
			// 
			// removeClipToolStripMenuItem
			// 
			this.removeClipToolStripMenuItem.Enabled = false;
			this.removeClipToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Remove;
			this.removeClipToolStripMenuItem.Name = "removeClipToolStripMenuItem";
			this.removeClipToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.removeClipToolStripMenuItem.Text = "Remo&ve Clip";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size( 44, 20 );
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// panelTimeLine
			// 
			this.panelTimeLine.Controls.Add( this.toolStripMain );
			this.panelTimeLine.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelTimeLine.Location = new System.Drawing.Point( 0, 24 );
			this.panelTimeLine.Name = "panelTimeLine";
			this.panelTimeLine.Size = new System.Drawing.Size( 1051, 29 );
			this.panelTimeLine.TabIndex = 1;
			// 
			// toolStripMain
			// 
			this.toolStripMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStripMain.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonCreateNewParameter} );
			this.toolStripMain.Location = new System.Drawing.Point( 0, 0 );
			this.toolStripMain.Name = "toolStripMain";
			this.toolStripMain.Size = new System.Drawing.Size( 1051, 25 );
			this.toolStripMain.TabIndex = 0;
			// 
			// toolStripButtonCreateNewParameter
			// 
			this.toolStripButtonCreateNewParameter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonCreateNewParameter.Enabled = false;
			this.toolStripButtonCreateNewParameter.Image = global::SequencorEditor.Properties.Resources.Property_Indicator;
			this.toolStripButtonCreateNewParameter.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonCreateNewParameter.Name = "toolStripButtonCreateNewParameter";
			this.toolStripButtonCreateNewParameter.Size = new System.Drawing.Size( 23, 22 );
			this.toolStripButtonCreateNewParameter.Text = "Create New Parameter";
			this.toolStripButtonCreateNewParameter.ToolTipText = "Creates a new parameter";
			this.toolStripButtonCreateNewParameter.Click += new System.EventHandler( this.toolStripButtonCreateNewParameter_Click );
			// 
			// panelMain
			// 
			this.panelMain.Controls.Add( this.panelTracks );
			this.panelMain.Controls.Add( this.panelTrackTitles );
			this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelMain.Location = new System.Drawing.Point( 0, 53 );
			this.panelMain.Name = "panelMain";
			this.panelMain.Size = new System.Drawing.Size( 1051, 535 );
			this.panelMain.TabIndex = 2;
			// 
			// panelTracks
			// 
			this.panelTracks.Controls.Add( this.sequencerControl );
			this.panelTracks.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTracks.Location = new System.Drawing.Point( 10, 0 );
			this.panelTracks.Name = "panelTracks";
			this.panelTracks.Size = new System.Drawing.Size( 1041, 535 );
			this.panelTracks.TabIndex = 1;
			// 
			// sequencerControl
			// 
			this.sequencerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sequencerControl.Enabled = false;
			this.sequencerControl.Location = new System.Drawing.Point( 0, 0 );
			this.sequencerControl.Name = "sequencerControl";
			this.sequencerControl.SelectedInterval = null;
			this.sequencerControl.SelectedTrack = null;
			this.sequencerControl.Sequencer = null;
			this.sequencerControl.Size = new System.Drawing.Size( 1041, 535 );
			this.sequencerControl.TabIndex = 0;
			this.sequencerControl.SequencePlay += new System.EventHandler( this.sequencerControl_SequencePlay );
			this.sequencerControl.SequencePause += new System.EventHandler( this.sequencerControl_SequencePause );
			this.sequencerControl.SequenceTimeChanged += new System.EventHandler( this.sequencerControl_SequenceTimeChanged );
			// 
			// panelTrackTitles
			// 
			this.panelTrackTitles.Dock = System.Windows.Forms.DockStyle.Left;
			this.panelTrackTitles.Location = new System.Drawing.Point( 0, 0 );
			this.panelTrackTitles.Name = "panelTrackTitles";
			this.panelTrackTitles.Size = new System.Drawing.Size( 10, 535 );
			this.panelTrackTitles.TabIndex = 0;
			// 
			// openFileDialogProject
			// 
			this.openFileDialogProject.DefaultExt = "*.sqcProj";
			this.openFileDialogProject.Filter = "Sequence Project (*.sqcProj)|*.sqcProj|All Files|*.*";
			this.openFileDialogProject.Title = "Load sequencer project...";
			// 
			// saveFileDialogProject
			// 
			this.saveFileDialogProject.DefaultExt = "*.sqcProj";
			this.saveFileDialogProject.Filter = "Sequence Project (*.sqcProj)|*.sqcProj|All Files|*.*";
			this.saveFileDialogProject.Title = "Save sequencer project...";
			// 
			// saveFileDialogBinary
			// 
			this.saveFileDialogBinary.DefaultExt = "*.sqc";
			this.saveFileDialogBinary.Filter = "Sequence File (*.sqc)|*.sqcProj|All Files|*.*";
			this.saveFileDialogBinary.Title = "Export sequenc file...";
			// 
			// openFileDialogMusic
			// 
			this.openFileDialogMusic.DefaultExt = "*.mp3";
			this.openFileDialogMusic.Filter = "Music File (*.mp3,*.wav,*.ogg,*.mod)|*.mp3;*.wav;*.ogg;*.mod|All Files|*.*";
			this.openFileDialogMusic.Title = "Load sequencer music track...";
			// 
			// SequencerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1051, 588 );
			this.Controls.Add( this.panelMain );
			this.Controls.Add( this.panelTimeLine );
			this.Controls.Add( this.menuStrip1 );
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "SequencerForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Sequencor Editor v0.1b";
			this.menuStrip1.ResumeLayout( false );
			this.menuStrip1.PerformLayout();
			this.panelTimeLine.ResumeLayout( false );
			this.panelTimeLine.PerformLayout();
			this.toolStripMain.ResumeLayout( false );
			this.toolStripMain.PerformLayout();
			this.panelMain.ResumeLayout( false );
			this.panelTracks.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.Panel panelTimeLine;
		private System.Windows.Forms.Panel panelMain;
		private System.Windows.Forms.Panel panelTrackTitles;
		private System.Windows.Forms.Panel panelTracks;
		private SequencerControl sequencerControl;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sequenceToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createNewParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem deleteParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createNewIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem asBinaryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem asXMLToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem addClipToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem removeClipToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadMusicToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog openFileDialogProject;
		private System.Windows.Forms.SaveFileDialog saveFileDialogProject;
		private System.Windows.Forms.SaveFileDialog saveFileDialogBinary;
		private System.Windows.Forms.ToolStrip toolStripMain;
		private System.Windows.Forms.OpenFileDialog openFileDialogMusic;
		private System.Windows.Forms.ToolStripButton toolStripButtonCreateNewParameter;
		private System.Windows.Forms.ToolStripMenuItem copySelectedTrackToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copySelectedIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copySelectedKeyToolStripMenuItem;
	}
}