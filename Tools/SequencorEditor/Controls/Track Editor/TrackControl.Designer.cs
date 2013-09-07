namespace SequencorEditor
{
	partial class TrackControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( TrackControl ) );
			this.toolTip = new System.Windows.Forms.ToolTip( this.components );
			this.checkBoxShowTrackAnim = new System.Windows.Forms.CheckBox();
			this.labelTrack = new System.Windows.Forms.Label();
			this.contextMenuStripSequence = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveToFirstToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveToLastToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.renameEmitterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.removeParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panelInfos = new System.Windows.Forms.Panel();
			this.labelGUID = new System.Windows.Forms.Label();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.createIntervalAtPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveIntervalAtCursorPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cloneIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitIntervalAtCursorPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mergeWithNextIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.editTrackColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.trackIntervalPanel = new SequencorEditor.TrackIntervalPanel();
			this.contextMenuStripSequence.SuspendLayout();
			this.panelInfos.SuspendLayout();
			this.contextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkBoxShowTrackAnim
			// 
			this.checkBoxShowTrackAnim.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxShowTrackAnim.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxShowTrackAnim.BackColor = System.Drawing.Color.Transparent;
			this.checkBoxShowTrackAnim.BackgroundImage = global::SequencorEditor.Properties.Resources.Unfold;
			this.checkBoxShowTrackAnim.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.checkBoxShowTrackAnim.FlatAppearance.BorderSize = 0;
			this.checkBoxShowTrackAnim.FlatAppearance.CheckedBackColor = System.Drawing.Color.AliceBlue;
			this.checkBoxShowTrackAnim.Location = new System.Drawing.Point( 136, 12 );
			this.checkBoxShowTrackAnim.Margin = new System.Windows.Forms.Padding( 1 );
			this.checkBoxShowTrackAnim.Name = "checkBoxShowTrackAnim";
			this.checkBoxShowTrackAnim.Size = new System.Drawing.Size( 20, 20 );
			this.checkBoxShowTrackAnim.TabIndex = 0;
			this.toolTip.SetToolTip( this.checkBoxShowTrackAnim, "Shows or hides the animation editor" );
			this.checkBoxShowTrackAnim.UseVisualStyleBackColor = false;
			this.checkBoxShowTrackAnim.CheckedChanged += new System.EventHandler( this.checkBoxShowTrackAnim_CheckedChanged );
			this.checkBoxShowTrackAnim.MouseDown += new System.Windows.Forms.MouseEventHandler( this.checkBoxShowTrackAnim_MouseDown );
			// 
			// labelTrack
			// 
			this.labelTrack.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelTrack.BackColor = System.Drawing.Color.Transparent;
			this.labelTrack.ContextMenuStrip = this.contextMenuStripSequence;
			this.labelTrack.Location = new System.Drawing.Point( 3, 1 );
			this.labelTrack.Name = "labelTrack";
			this.labelTrack.Size = new System.Drawing.Size( 129, 18 );
			this.labelTrack.TabIndex = 1;
			this.labelTrack.Text = "Track Name";
			this.labelTrack.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.labelTrack_MouseDoubleClick );
			this.labelTrack.MouseDown += new System.Windows.Forms.MouseEventHandler( this.labelTrack_MouseDown );
			// 
			// contextMenuStripSequence
			// 
			this.contextMenuStripSequence.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.moveUpToolStripMenuItem,
            this.moveDownToolStripMenuItem,
            this.moveToFirstToolStripMenuItem,
            this.moveToLastToolStripMenuItem,
            this.toolStripMenuItem1,
            this.renameEmitterToolStripMenuItem,
            this.copyParameterToolStripMenuItem,
            this.pasteParameterToolStripMenuItem,
            this.toolStripMenuItem3,
            this.removeParameterToolStripMenuItem} );
			this.contextMenuStripSequence.Name = "contextMenuStripSequence";
			this.contextMenuStripSequence.Size = new System.Drawing.Size( 202, 214 );
			this.contextMenuStripSequence.Opening += new System.ComponentModel.CancelEventHandler( this.contextMenuStripSequence_Opening );
			// 
			// moveUpToolStripMenuItem
			// 
			this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
			this.moveUpToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.moveUpToolStripMenuItem.Text = "Move &Up";
			this.moveUpToolStripMenuItem.Click += new System.EventHandler( this.moveUpToolStripMenuItem_Click );
			// 
			// moveDownToolStripMenuItem
			// 
			this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
			this.moveDownToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.moveDownToolStripMenuItem.Text = "Move &Down";
			this.moveDownToolStripMenuItem.Click += new System.EventHandler( this.moveDownToolStripMenuItem_Click );
			// 
			// moveToFirstToolStripMenuItem
			// 
			this.moveToFirstToolStripMenuItem.Name = "moveToFirstToolStripMenuItem";
			this.moveToFirstToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.moveToFirstToolStripMenuItem.Text = "Move to &Top";
			this.moveToFirstToolStripMenuItem.Click += new System.EventHandler( this.moveToFirstToolStripMenuItem_Click );
			// 
			// moveToLastToolStripMenuItem
			// 
			this.moveToLastToolStripMenuItem.Name = "moveToLastToolStripMenuItem";
			this.moveToLastToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.moveToLastToolStripMenuItem.Text = "Move to &Bottom";
			this.moveToLastToolStripMenuItem.Click += new System.EventHandler( this.moveToLastToolStripMenuItem_Click );
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size( 198, 6 );
			// 
			// renameEmitterToolStripMenuItem
			// 
			this.renameEmitterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Variant___Random;
			this.renameEmitterToolStripMenuItem.Name = "renameEmitterToolStripMenuItem";
			this.renameEmitterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.renameEmitterToolStripMenuItem.Text = "&Edit Parameter";
			this.renameEmitterToolStripMenuItem.Click += new System.EventHandler( this.renameEmitterToolStripMenuItem_Click );
			// 
			// copyParameterToolStripMenuItem
			// 
			this.copyParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.copyParameterToolStripMenuItem.Name = "copyParameterToolStripMenuItem";
			this.copyParameterToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyParameterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.copyParameterToolStripMenuItem.Text = "&Copy Parameter";
			this.copyParameterToolStripMenuItem.Click += new System.EventHandler( this.copyParameterToolStripMenuItem_Click );
			// 
			// pasteParameterToolStripMenuItem
			// 
			this.pasteParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Paste;
			this.pasteParameterToolStripMenuItem.Name = "pasteParameterToolStripMenuItem";
			this.pasteParameterToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteParameterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.pasteParameterToolStripMenuItem.Text = "&Paste Parameter";
			this.pasteParameterToolStripMenuItem.Click += new System.EventHandler( this.pasteParameterToolStripMenuItem_Click );
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size( 198, 6 );
			// 
			// removeParameterToolStripMenuItem
			// 
			this.removeParameterToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "removeParameterToolStripMenuItem.Image" )));
			this.removeParameterToolStripMenuItem.Name = "removeParameterToolStripMenuItem";
			this.removeParameterToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.removeParameterToolStripMenuItem.Text = "Remo&ve Parameter";
			this.removeParameterToolStripMenuItem.Click += new System.EventHandler( this.removeParameterToolStripMenuItem_Click );
			// 
			// panelInfos
			// 
			this.panelInfos.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panelInfos.ContextMenuStrip = this.contextMenuStripSequence;
			this.panelInfos.Controls.Add( this.checkBoxShowTrackAnim );
			this.panelInfos.Controls.Add( this.labelGUID );
			this.panelInfos.Controls.Add( this.labelTrack );
			this.panelInfos.Dock = System.Windows.Forms.DockStyle.Left;
			this.panelInfos.Location = new System.Drawing.Point( 0, 0 );
			this.panelInfos.Name = "panelInfos";
			this.panelInfos.Size = new System.Drawing.Size( 158, 33 );
			this.panelInfos.TabIndex = 2;
			this.panelInfos.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.panelInfos_MouseDoubleClick );
			this.panelInfos.MouseDown += new System.Windows.Forms.MouseEventHandler( this.panelInfos_MouseDown );
			// 
			// labelGUID
			// 
			this.labelGUID.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelGUID.BackColor = System.Drawing.Color.Transparent;
			this.labelGUID.Location = new System.Drawing.Point( 3, 16 );
			this.labelGUID.Name = "labelGUID";
			this.labelGUID.Size = new System.Drawing.Size( 57, 18 );
			this.labelGUID.TabIndex = 1;
			this.labelGUID.Text = "(0)";
			this.labelGUID.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.labelTrack_MouseDoubleClick );
			this.labelGUID.MouseDown += new System.Windows.Forms.MouseEventHandler( this.labelTrack_MouseDown );
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.createIntervalAtPositionToolStripMenuItem,
            this.moveIntervalAtCursorPositionToolStripMenuItem,
            this.copyIntervalToolStripMenuItem,
            this.cloneIntervalToolStripMenuItem,
            this.pasteIntervalToolStripMenuItem,
            this.splitIntervalAtCursorPositionToolStripMenuItem,
            this.mergeWithNextIntervalToolStripMenuItem,
            this.toolStripMenuItem2,
            this.editTrackColorToolStripMenuItem,
            this.toolStripSeparator1,
            this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem,
            this.deleteIntervalToolStripMenuItem} );
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size( 298, 236 );
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler( this.contextMenuStrip_Opening );
			// 
			// createIntervalAtPositionToolStripMenuItem
			// 
			this.createIntervalAtPositionToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Interval;
			this.createIntervalAtPositionToolStripMenuItem.Name = "createIntervalAtPositionToolStripMenuItem";
			this.createIntervalAtPositionToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.createIntervalAtPositionToolStripMenuItem.Text = "&Create Interval at Position";
			this.createIntervalAtPositionToolStripMenuItem.Click += new System.EventHandler( this.createIntervalAtPositionToolStripMenuItem_Click );
			// 
			// moveIntervalAtCursorPositionToolStripMenuItem
			// 
			this.moveIntervalAtCursorPositionToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Interval;
			this.moveIntervalAtCursorPositionToolStripMenuItem.Name = "moveIntervalAtCursorPositionToolStripMenuItem";
			this.moveIntervalAtCursorPositionToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.moveIntervalAtCursorPositionToolStripMenuItem.Text = "&Move Interval at Cursor Position";
			this.moveIntervalAtCursorPositionToolStripMenuItem.Click += new System.EventHandler( this.moveIntervalAtCursorPositionToolStripMenuItem_Click );
			// 
			// copyIntervalToolStripMenuItem
			// 
			this.copyIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.copyIntervalToolStripMenuItem.Name = "copyIntervalToolStripMenuItem";
			this.copyIntervalToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyIntervalToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.copyIntervalToolStripMenuItem.Text = "Co&py Interval";
			this.copyIntervalToolStripMenuItem.Click += new System.EventHandler( this.copyIntervalToolStripMenuItem_Click );
			// 
			// cloneIntervalToolStripMenuItem
			// 
			this.cloneIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.cloneIntervalToolStripMenuItem.Name = "cloneIntervalToolStripMenuItem";
			this.cloneIntervalToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.cloneIntervalToolStripMenuItem.Text = "C&lone Interval";
			this.cloneIntervalToolStripMenuItem.Click += new System.EventHandler( this.cloneIntervalToolStripMenuItem_Click );
			// 
			// pasteIntervalToolStripMenuItem
			// 
			this.pasteIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Paste;
			this.pasteIntervalToolStripMenuItem.Name = "pasteIntervalToolStripMenuItem";
			this.pasteIntervalToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteIntervalToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.pasteIntervalToolStripMenuItem.Text = "Pas&te Interval";
			this.pasteIntervalToolStripMenuItem.Click += new System.EventHandler( this.pasteIntervalToolStripMenuItem_Click );
			// 
			// splitIntervalAtCursorPositionToolStripMenuItem
			// 
			this.splitIntervalAtCursorPositionToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.cut;
			this.splitIntervalAtCursorPositionToolStripMenuItem.Name = "splitIntervalAtCursorPositionToolStripMenuItem";
			this.splitIntervalAtCursorPositionToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.splitIntervalAtCursorPositionToolStripMenuItem.Text = "&Split Interval at Cursor Position";
			this.splitIntervalAtCursorPositionToolStripMenuItem.Click += new System.EventHandler( this.splitIntervalAtCursorPositionToolStripMenuItem_Click );
			// 
			// mergeWithNextIntervalToolStripMenuItem
			// 
			this.mergeWithNextIntervalToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.merge;
			this.mergeWithNextIntervalToolStripMenuItem.Name = "mergeWithNextIntervalToolStripMenuItem";
			this.mergeWithNextIntervalToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.mergeWithNextIntervalToolStripMenuItem.Text = "Mer&ge with Next Interval";
			this.mergeWithNextIntervalToolStripMenuItem.Click += new System.EventHandler( this.mergeWithNextIntervalToolStripMenuItem_Click );
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size( 294, 6 );
			// 
			// editTrackColorToolStripMenuItem
			// 
			this.editTrackColorToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "editTrackColorToolStripMenuItem.Image" )));
			this.editTrackColorToolStripMenuItem.Name = "editTrackColorToolStripMenuItem";
			this.editTrackColorToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.editTrackColorToolStripMenuItem.Text = "Edit Track Co&lor...";
			this.editTrackColorToolStripMenuItem.Click += new System.EventHandler( this.editTrackColorToolStripMenuItem_Click );
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size( 294, 6 );
			// 
			// setSequenceEndTimeAtThisIntervalEndToolStripMenuItem
			// 
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem.Enabled = false;
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.SequenceEnd;
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem.Name = "setSequenceEndTimeAtThisIntervalEndToolStripMenuItem";
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem.Text = "Set Sequence End Time at this Interval End";
			this.setSequenceEndTimeAtThisIntervalEndToolStripMenuItem.Visible = false;
			// 
			// deleteIntervalToolStripMenuItem
			// 
			this.deleteIntervalToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "deleteIntervalToolStripMenuItem.Image" )));
			this.deleteIntervalToolStripMenuItem.Name = "deleteIntervalToolStripMenuItem";
			this.deleteIntervalToolStripMenuItem.Size = new System.Drawing.Size( 297, 22 );
			this.deleteIntervalToolStripMenuItem.Text = "&Delete Interval";
			this.deleteIntervalToolStripMenuItem.Click += new System.EventHandler( this.deleteIntervalToolStripMenuItem_Click );
			// 
			// trackIntervalPanel
			// 
			this.trackIntervalPanel.BackColor = System.Drawing.Color.Gainsboro;
			this.trackIntervalPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.trackIntervalPanel.ContextMenuStrip = this.contextMenuStrip;
			this.trackIntervalPanel.CursorTimeColor = System.Drawing.Color.ForestGreen;
			this.trackIntervalPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackIntervalPanel.ForeColor = System.Drawing.Color.RoyalBlue;
			this.trackIntervalPanel.Location = new System.Drawing.Point( 158, 0 );
			this.trackIntervalPanel.Name = "trackIntervalPanel";
			this.trackIntervalPanel.Owner = null;
			this.trackIntervalPanel.PreventRefresh = false;
			this.trackIntervalPanel.Selected = false;
			this.trackIntervalPanel.SelectedColor = System.Drawing.Color.IndianRed;
			this.trackIntervalPanel.SelectedInterval = null;
			this.trackIntervalPanel.SelectedIntervalColor = System.Drawing.Color.Red;
			this.trackIntervalPanel.Size = new System.Drawing.Size( 988, 33 );
			this.trackIntervalPanel.TabIndex = 3;
			this.trackIntervalPanel.Track = null;
			this.trackIntervalPanel.KeyDown += new System.Windows.Forms.KeyEventHandler( this.trackIntervalPanel_KeyDown );
			this.trackIntervalPanel.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.trackIntervalPanel_MouseDoubleClick );
			this.trackIntervalPanel.MouseDown += new System.Windows.Forms.MouseEventHandler( this.trackIntervalPanel_MouseDown );
			this.trackIntervalPanel.MouseMove += new System.Windows.Forms.MouseEventHandler( this.trackIntervalPanel_MouseMove );
			this.trackIntervalPanel.MouseUp += new System.Windows.Forms.MouseEventHandler( this.trackIntervalPanel_MouseUp );
			// 
			// TrackControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add( this.trackIntervalPanel );
			this.Controls.Add( this.panelInfos );
			this.MaximumSize = new System.Drawing.Size( 10000, 60 );
			this.Name = "TrackControl";
			this.Size = new System.Drawing.Size( 1146, 33 );
			this.contextMenuStripSequence.ResumeLayout( false );
			this.panelInfos.ResumeLayout( false );
			this.contextMenuStrip.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxShowTrackAnim;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Label labelTrack;
		private System.Windows.Forms.Panel panelInfos;
		internal TrackIntervalPanel trackIntervalPanel;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem createIntervalAtPositionToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip contextMenuStripSequence;
		private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveDownToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveToFirstToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveToLastToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem renameEmitterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem deleteIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setSequenceEndTimeAtThisIntervalEndToolStripMenuItem;
		private System.Windows.Forms.Label labelGUID;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem removeParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveIntervalAtCursorPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editTrackColorToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem splitIntervalAtCursorPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mergeWithNextIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cloneIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyIntervalToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteIntervalToolStripMenuItem;




	}
}
