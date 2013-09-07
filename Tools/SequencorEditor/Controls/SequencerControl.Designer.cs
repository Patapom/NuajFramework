using SequencorLib;

namespace SequencorEditor
{
	partial class SequencerControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( SequencerControl ) );
			this.panelMain = new System.Windows.Forms.Panel();
			this.panelTracks = new SequencorEditor.PanelNoMouseWheel( this.components );
			this.contextMenuStripTracks = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.createParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.deleteParameterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.labelStartProject = new System.Windows.Forms.Label();
			this.panelFastPaint = new SequencorEditor.PanelFastPaint();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.setCursorAtTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setSequenceEndTimeAtMousePositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setSequenceEndTimeAtCursorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setSequenceEndTimeAtInfinityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panelTop = new System.Windows.Forms.Panel();
			this.buttonZoomOut = new System.Windows.Forms.Button();
			this.checkBoxPlay = new System.Windows.Forms.CheckBox();
			this.buttonStop = new System.Windows.Forms.Button();
			this.timeLineControl = new SequencorEditor.TimeLineControl();
			this.timer = new System.Windows.Forms.Timer( this.components );
			this.toolTip = new System.Windows.Forms.ToolTip( this.components );
			this.panelMain.SuspendLayout();
			this.panelTracks.SuspendLayout();
			this.contextMenuStripTracks.SuspendLayout();
			this.contextMenuStrip.SuspendLayout();
			this.panelTop.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelMain
			// 
			this.panelMain.Controls.Add( this.panelTracks );
			this.panelMain.Controls.Add( this.panelFastPaint );
			this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelMain.Location = new System.Drawing.Point( 0, 50 );
			this.panelMain.Name = "panelMain";
			this.panelMain.Size = new System.Drawing.Size( 785, 463 );
			this.panelMain.TabIndex = 1;
			// 
			// panelTracks
			// 
			this.panelTracks.AutoScroll = true;
			this.panelTracks.BackColor = System.Drawing.SystemColors.Control;
			this.panelTracks.ContextMenuStrip = this.contextMenuStripTracks;
			this.panelTracks.Controls.Add( this.labelStartProject );
			this.panelTracks.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTracks.Location = new System.Drawing.Point( 0, 0 );
			this.panelTracks.Name = "panelTracks";
			this.panelTracks.Size = new System.Drawing.Size( 785, 463 );
			this.panelTracks.TabIndex = 2;
			// 
			// contextMenuStripTracks
			// 
			this.contextMenuStripTracks.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.createParameterToolStripMenuItem,
            this.editParameterToolStripMenuItem,
            this.toolStripSeparator1,
            this.deleteParameterToolStripMenuItem} );
			this.contextMenuStripTracks.Name = "contextMenuStripTracks";
			this.contextMenuStripTracks.Size = new System.Drawing.Size( 175, 98 );
			this.contextMenuStripTracks.Opening += new System.ComponentModel.CancelEventHandler( this.contextMenuStripTracks_Opening );
			// 
			// createParameterToolStripMenuItem
			// 
			this.createParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Property_Indicator;
			this.createParameterToolStripMenuItem.Name = "createParameterToolStripMenuItem";
			this.createParameterToolStripMenuItem.Size = new System.Drawing.Size( 174, 22 );
			this.createParameterToolStripMenuItem.Text = "&Create Parameter...";
			this.createParameterToolStripMenuItem.Click += new System.EventHandler( this.createParameterToolStripMenuItem_Click );
			// 
			// editParameterToolStripMenuItem
			// 
			this.editParameterToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Variant___Random;
			this.editParameterToolStripMenuItem.Name = "editParameterToolStripMenuItem";
			this.editParameterToolStripMenuItem.Size = new System.Drawing.Size( 174, 22 );
			this.editParameterToolStripMenuItem.Text = "&Edit Parameter...";
			this.editParameterToolStripMenuItem.Click += new System.EventHandler( this.editParameterToolStripMenuItem_Click );
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size( 171, 6 );
			// 
			// deleteParameterToolStripMenuItem
			// 
			this.deleteParameterToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "deleteParameterToolStripMenuItem.Image" )));
			this.deleteParameterToolStripMenuItem.Name = "deleteParameterToolStripMenuItem";
			this.deleteParameterToolStripMenuItem.Size = new System.Drawing.Size( 174, 22 );
			this.deleteParameterToolStripMenuItem.Text = "&Delete Parameter";
			this.deleteParameterToolStripMenuItem.Click += new System.EventHandler( this.deleteParameterToolStripMenuItem_Click );
			// 
			// labelStartProject
			// 
			this.labelStartProject.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.labelStartProject.AutoSize = true;
			this.labelStartProject.Location = new System.Drawing.Point( 292, 225 );
			this.labelStartProject.Name = "labelStartProject";
			this.labelStartProject.Size = new System.Drawing.Size( 185, 13 );
			this.labelStartProject.TabIndex = 0;
			this.labelStartProject.Text = "Create or Open a sequencer project...";
			// 
			// panelFastPaint
			// 
			this.panelFastPaint.CursorTimeColor = System.Drawing.Color.ForestGreen;
			this.panelFastPaint.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelFastPaint.Location = new System.Drawing.Point( 0, 0 );
			this.panelFastPaint.MimickedControl = null;
			this.panelFastPaint.Name = "panelFastPaint";
			this.panelFastPaint.Owner = null;
			this.panelFastPaint.Size = new System.Drawing.Size( 785, 463 );
			this.panelFastPaint.TabIndex = 3;
			this.panelFastPaint.Visible = false;
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.setCursorAtTimeToolStripMenuItem,
            this.setSequenceEndTimeAtMousePositionToolStripMenuItem,
            this.setSequenceEndTimeAtCursorToolStripMenuItem,
            this.setSequenceEndTimeAtInfinityToolStripMenuItem} );
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size( 296, 92 );
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler( this.contextMenuStrip_Opening );
			// 
			// setCursorAtTimeToolStripMenuItem
			// 
			this.setCursorAtTimeToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "setCursorAtTimeToolStripMenuItem.Image" )));
			this.setCursorAtTimeToolStripMenuItem.Name = "setCursorAtTimeToolStripMenuItem";
			this.setCursorAtTimeToolStripMenuItem.Size = new System.Drawing.Size( 295, 22 );
			this.setCursorAtTimeToolStripMenuItem.Text = "&Set Cursor at Time...";
			this.setCursorAtTimeToolStripMenuItem.ToolTipText = "Sets the cursor position at an exact time";
			this.setCursorAtTimeToolStripMenuItem.Click += new System.EventHandler( this.setCursorAtTimeToolStripMenuItem_Click );
			// 
			// setSequenceEndTimeAtMousePositionToolStripMenuItem
			// 
			this.setSequenceEndTimeAtMousePositionToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.SequenceEnd;
			this.setSequenceEndTimeAtMousePositionToolStripMenuItem.Name = "setSequenceEndTimeAtMousePositionToolStripMenuItem";
			this.setSequenceEndTimeAtMousePositionToolStripMenuItem.Size = new System.Drawing.Size( 295, 22 );
			this.setSequenceEndTimeAtMousePositionToolStripMenuItem.Text = "Set Sequence End Time at Mouse Position";
			// 
			// setSequenceEndTimeAtCursorToolStripMenuItem
			// 
			this.setSequenceEndTimeAtCursorToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.SequenceEnd;
			this.setSequenceEndTimeAtCursorToolStripMenuItem.Name = "setSequenceEndTimeAtCursorToolStripMenuItem";
			this.setSequenceEndTimeAtCursorToolStripMenuItem.Size = new System.Drawing.Size( 295, 22 );
			this.setSequenceEndTimeAtCursorToolStripMenuItem.Text = "Set Sequence End Time at Cursor Position";
			// 
			// setSequenceEndTimeAtInfinityToolStripMenuItem
			// 
			this.setSequenceEndTimeAtInfinityToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "setSequenceEndTimeAtInfinityToolStripMenuItem.Image" )));
			this.setSequenceEndTimeAtInfinityToolStripMenuItem.Name = "setSequenceEndTimeAtInfinityToolStripMenuItem";
			this.setSequenceEndTimeAtInfinityToolStripMenuItem.Size = new System.Drawing.Size( 295, 22 );
			this.setSequenceEndTimeAtInfinityToolStripMenuItem.Text = "Set Sequence End Time at Infinity";
			// 
			// panelTop
			// 
			this.panelTop.Controls.Add( this.buttonZoomOut );
			this.panelTop.Controls.Add( this.checkBoxPlay );
			this.panelTop.Controls.Add( this.buttonStop );
			this.panelTop.Controls.Add( this.timeLineControl );
			this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelTop.Location = new System.Drawing.Point( 0, 0 );
			this.panelTop.Name = "panelTop";
			this.panelTop.Size = new System.Drawing.Size( 785, 50 );
			this.panelTop.TabIndex = 2;
			// 
			// buttonZoomOut
			// 
			this.buttonZoomOut.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "buttonZoomOut.BackgroundImage" )));
			this.buttonZoomOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.buttonZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.buttonZoomOut.ImageIndex = 3;
			this.buttonZoomOut.Location = new System.Drawing.Point( 129, 22 );
			this.buttonZoomOut.Name = "buttonZoomOut";
			this.buttonZoomOut.Size = new System.Drawing.Size( 22, 22 );
			this.buttonZoomOut.TabIndex = 4;
			this.buttonZoomOut.UseVisualStyleBackColor = true;
			this.buttonZoomOut.Click += new System.EventHandler( this.buttonZoomOut_Click );
			// 
			// checkBoxPlay
			// 
			this.checkBoxPlay.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxPlay.BackgroundImage = global::SequencorEditor.Properties.Resources.Track___Play;
			this.checkBoxPlay.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.checkBoxPlay.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.ControlDark;
			this.checkBoxPlay.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Azure;
			this.checkBoxPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.checkBoxPlay.Location = new System.Drawing.Point( 3, 3 );
			this.checkBoxPlay.Name = "checkBoxPlay";
			this.checkBoxPlay.Size = new System.Drawing.Size( 32, 32 );
			this.checkBoxPlay.TabIndex = 3;
			this.checkBoxPlay.UseVisualStyleBackColor = true;
			this.checkBoxPlay.CheckedChanged += new System.EventHandler( this.checkBoxPlay_CheckedChanged );
			// 
			// buttonStop
			// 
			this.buttonStop.BackgroundImage = global::SequencorEditor.Properties.Resources.Track___Stop;
			this.buttonStop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.buttonStop.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Azure;
			this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonStop.ImageIndex = 3;
			this.buttonStop.Location = new System.Drawing.Point( 36, 3 );
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size( 32, 32 );
			this.buttonStop.TabIndex = 2;
			this.buttonStop.UseVisualStyleBackColor = true;
			this.buttonStop.Click += new System.EventHandler( this.buttonStop_Click );
			// 
			// timeLineControl
			// 
			this.timeLineControl.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.timeLineControl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.timeLineControl.BoundMax = 10000F;
			this.timeLineControl.BoundMin = 0F;
			this.timeLineControl.ContextMenuStrip = this.contextMenuStrip;
			this.timeLineControl.CursorPosition = 0.5F;
			this.timeLineControl.Location = new System.Drawing.Point( 157, 3 );
			this.timeLineControl.Name = "timeLineControl";
			this.timeLineControl.Size = new System.Drawing.Size( 612, 44 );
			this.timeLineControl.TabIndex = 0;
			this.timeLineControl.VisibleBoundMax = 1F;
			this.timeLineControl.VisibleBoundMin = 0F;
			this.timeLineControl.CursorMoved += new SequencorEditor.TimeLineControl.CursorMovedEventHandler( this.timeLineControl_CursorMoved );
			this.timeLineControl.VisibleRangeChanged += new SequencorEditor.TimeLineControl.VisibleRangeChangedEventHandler( this.timeLineControl_VisibleRangeChanged );
			this.timeLineControl.CustomGraduationPaint += new SequencorEditor.TimeLineControl.CustomPaintEventHandler( this.timeLineControl_CustomGraduationPaint );
			this.timeLineControl.MouseMove += new System.Windows.Forms.MouseEventHandler( this.timeLineControl_MouseMove );
			// 
			// timer
			// 
			this.timer.Interval = 10;
			this.timer.Tick += new System.EventHandler( this.timer_Tick );
			// 
			// SequencerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add( this.panelMain );
			this.Controls.Add( this.panelTop );
			this.Enabled = false;
			this.Name = "SequencerControl";
			this.Size = new System.Drawing.Size( 785, 513 );
			this.panelMain.ResumeLayout( false );
			this.panelTracks.ResumeLayout( false );
			this.panelTracks.PerformLayout();
			this.contextMenuStripTracks.ResumeLayout( false );
			this.contextMenuStrip.ResumeLayout( false );
			this.panelTop.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		private TimeLineControl timeLineControl;
		private System.Windows.Forms.Panel panelMain;
		private System.Windows.Forms.Panel panelTop;
		private PanelNoMouseWheel panelTracks;
		private System.Windows.Forms.CheckBox checkBoxPlay;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.Button buttonZoomOut;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem setSequenceEndTimeAtCursorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setSequenceEndTimeAtInfinityToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setSequenceEndTimeAtMousePositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setCursorAtTimeToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip contextMenuStripTracks;
		private System.Windows.Forms.ToolStripMenuItem createParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editParameterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteParameterToolStripMenuItem;
		private System.Windows.Forms.Label labelStartProject;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private PanelFastPaint panelFastPaint;




	}
}
