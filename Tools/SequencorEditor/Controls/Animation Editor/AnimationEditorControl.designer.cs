namespace SequencorEditor
{
	partial class AnimationEditorControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( AnimationEditorControl ) );
			this.toolTip = new System.Windows.Forms.ToolTip( this.components );
			this.buttonZoomOut = new System.Windows.Forms.Button();
			this.checkBoxGradient = new System.Windows.Forms.CheckBox();
			this.checkBoxInterpolation = new System.Windows.Forms.CheckBox();
			this.checkBoxShowTangents = new System.Windows.Forms.CheckBox();
			this.buttonSampleValue = new System.Windows.Forms.Button();
			this.floatTrackbarControlClipMax = new SequencorEditor.FloatTrackbarControl();
			this.floatTrackbarControlClipMin = new SequencorEditor.FloatTrackbarControl();
			this.panelInfos = new System.Windows.Forms.Panel();
			this.groupBoxClipping = new System.Windows.Forms.GroupBox();
			this.label5 = new System.Windows.Forms.Label();
			this.checkBoxClipMaxInfinity = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.checkBoxClipMinInfinity = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.floatTrackbarControlIntervalDuration = new SequencorEditor.FloatTrackbarControl();
			this.label2 = new System.Windows.Forms.Label();
			this.floatTrackbarControlIntervalEnd = new SequencorEditor.FloatTrackbarControl();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControlIntervalStart = new SequencorEditor.FloatTrackbarControl();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.createKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createKeyAtMousePositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createKeyAtMousePositionToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.positionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.rotationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.scaleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pRSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createKeyAtCursorPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createKeyAtCursorPositionToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.positionToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.rotationToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.scaleToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.pRSToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.createKeyFromCurrentValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.updateKeyFromCurrentValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.updateKeyFromCurrentValueToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.positionToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.rotationToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.scaleToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.pRSToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.editKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.alignKeyToCursorPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveKeyAtTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.copyKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.deleteKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.timer1 = new System.Windows.Forms.Timer( this.components );
			this.animationTrackPanel = new SequencorEditor.AnimationTrackPanel();
			this.buttonExit = new System.Windows.Forms.Button();
			this.gradientTrackPanel = new SequencorEditor.GradientTrackPanel();
			this.createInterpolatedKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.positionToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.rotationToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.scaleToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.pRSToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.panelInfos.SuspendLayout();
			this.groupBoxClipping.SuspendLayout();
			this.contextMenuStrip.SuspendLayout();
			this.animationTrackPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolTip
			// 
			this.toolTip.AutomaticDelay = 0;
			this.toolTip.AutoPopDelay = 5000;
			this.toolTip.InitialDelay = 0;
			this.toolTip.ReshowDelay = 0;
			// 
			// buttonZoomOut
			// 
			this.buttonZoomOut.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonZoomOut.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "buttonZoomOut.BackgroundImage" )));
			this.buttonZoomOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.buttonZoomOut.Enabled = false;
			this.buttonZoomOut.Location = new System.Drawing.Point( 135, 238 );
			this.buttonZoomOut.Name = "buttonZoomOut";
			this.buttonZoomOut.Size = new System.Drawing.Size( 22, 22 );
			this.buttonZoomOut.TabIndex = 6;
			this.toolTip.SetToolTip( this.buttonZoomOut, "Zooms to fit the interval vertically" );
			this.buttonZoomOut.UseVisualStyleBackColor = true;
			this.buttonZoomOut.Click += new System.EventHandler( this.buttonZoomOut_Click );
			// 
			// checkBoxGradient
			// 
			this.checkBoxGradient.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxGradient.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxGradient.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "checkBoxGradient.BackgroundImage" )));
			this.checkBoxGradient.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.checkBoxGradient.Location = new System.Drawing.Point( 69, 238 );
			this.checkBoxGradient.Name = "checkBoxGradient";
			this.checkBoxGradient.Size = new System.Drawing.Size( 22, 22 );
			this.checkBoxGradient.TabIndex = 0;
			this.toolTip.SetToolTip( this.checkBoxGradient, "Toggles color gradient visualisation" );
			this.checkBoxGradient.UseVisualStyleBackColor = true;
			this.checkBoxGradient.Visible = false;
			this.checkBoxGradient.CheckedChanged += new System.EventHandler( this.checkBoxGradient_CheckedChanged );
			this.checkBoxGradient.MouseDown += new System.Windows.Forms.MouseEventHandler( this.checkBoxLoop_MouseDown );
			// 
			// checkBoxInterpolation
			// 
			this.checkBoxInterpolation.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxInterpolation.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxInterpolation.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "checkBoxInterpolation.BackgroundImage" )));
			this.checkBoxInterpolation.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.checkBoxInterpolation.Location = new System.Drawing.Point( 113, 238 );
			this.checkBoxInterpolation.Name = "checkBoxInterpolation";
			this.checkBoxInterpolation.Size = new System.Drawing.Size( 22, 22 );
			this.checkBoxInterpolation.TabIndex = 0;
			this.toolTip.SetToolTip( this.checkBoxInterpolation, "Switches between linear and cubic interpolation" );
			this.checkBoxInterpolation.UseVisualStyleBackColor = true;
			this.checkBoxInterpolation.CheckedChanged += new System.EventHandler( this.checkBoxInterpolation_CheckedChanged );
			this.checkBoxInterpolation.MouseDown += new System.Windows.Forms.MouseEventHandler( this.checkBoxLoop_MouseDown );
			// 
			// checkBoxShowTangents
			// 
			this.checkBoxShowTangents.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxShowTangents.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxShowTangents.BackgroundImage = global::SequencorEditor.Properties.Resources.Tangent;
			this.checkBoxShowTangents.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.checkBoxShowTangents.Location = new System.Drawing.Point( 91, 238 );
			this.checkBoxShowTangents.Name = "checkBoxShowTangents";
			this.checkBoxShowTangents.Size = new System.Drawing.Size( 22, 22 );
			this.checkBoxShowTangents.TabIndex = 0;
			this.toolTip.SetToolTip( this.checkBoxShowTangents, "Toggles display of tangents" );
			this.checkBoxShowTangents.UseVisualStyleBackColor = true;
			this.checkBoxShowTangents.Visible = false;
			this.checkBoxShowTangents.CheckedChanged += new System.EventHandler( this.checkBoxShowTangents_CheckedChanged );
			this.checkBoxShowTangents.MouseDown += new System.Windows.Forms.MouseEventHandler( this.checkBoxLoop_MouseDown );
			// 
			// buttonSampleValue
			// 
			this.buttonSampleValue.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonSampleValue.Image = ((System.Drawing.Image) (resources.GetObject( "buttonSampleValue.Image" )));
			this.buttonSampleValue.Location = new System.Drawing.Point( 47, 238 );
			this.buttonSampleValue.Name = "buttonSampleValue";
			this.buttonSampleValue.Size = new System.Drawing.Size( 22, 22 );
			this.buttonSampleValue.TabIndex = 5;
			this.toolTip.SetToolTip( this.buttonSampleValue, "Updates the selected key by sampling current parameter" );
			this.buttonSampleValue.UseVisualStyleBackColor = true;
			this.buttonSampleValue.Visible = false;
			this.buttonSampleValue.Click += new System.EventHandler( this.buttonSampleValue_Click );
			// 
			// floatTrackbarControlClipMax
			// 
			this.floatTrackbarControlClipMax.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlClipMax.Enabled = false;
			this.floatTrackbarControlClipMax.Location = new System.Drawing.Point( 8, 33 );
			this.floatTrackbarControlClipMax.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlClipMax.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlClipMax.Name = "floatTrackbarControlClipMax";
			this.floatTrackbarControlClipMax.Size = new System.Drawing.Size( 141, 20 );
			this.floatTrackbarControlClipMax.TabIndex = 2;
			this.toolTip.SetToolTip( this.floatTrackbarControlClipMax, "Sets the maximum clipping value" );
			this.floatTrackbarControlClipMax.Value = 4F;
			this.floatTrackbarControlClipMax.ValueChanged += new SequencorEditor.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlClipMax_ValueChanged );
			// 
			// floatTrackbarControlClipMin
			// 
			this.floatTrackbarControlClipMin.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlClipMin.Enabled = false;
			this.floatTrackbarControlClipMin.Location = new System.Drawing.Point( 9, 78 );
			this.floatTrackbarControlClipMin.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlClipMin.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlClipMin.Name = "floatTrackbarControlClipMin";
			this.floatTrackbarControlClipMin.Size = new System.Drawing.Size( 141, 20 );
			this.floatTrackbarControlClipMin.TabIndex = 2;
			this.toolTip.SetToolTip( this.floatTrackbarControlClipMin, "Sets the minimum clipping value" );
			this.floatTrackbarControlClipMin.Value = 0F;
			this.floatTrackbarControlClipMin.ValueChanged += new SequencorEditor.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlClipMin_ValueChanged );
			// 
			// panelInfos
			// 
			this.panelInfos.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panelInfos.Controls.Add( this.buttonSampleValue );
			this.panelInfos.Controls.Add( this.groupBoxClipping );
			this.panelInfos.Controls.Add( this.buttonZoomOut );
			this.panelInfos.Controls.Add( this.label3 );
			this.panelInfos.Controls.Add( this.floatTrackbarControlIntervalDuration );
			this.panelInfos.Controls.Add( this.label2 );
			this.panelInfos.Controls.Add( this.floatTrackbarControlIntervalEnd );
			this.panelInfos.Controls.Add( this.label1 );
			this.panelInfos.Controls.Add( this.floatTrackbarControlIntervalStart );
			this.panelInfos.Controls.Add( this.checkBoxShowTangents );
			this.panelInfos.Controls.Add( this.checkBoxGradient );
			this.panelInfos.Controls.Add( this.checkBoxInterpolation );
			this.panelInfos.Dock = System.Windows.Forms.DockStyle.Left;
			this.panelInfos.Location = new System.Drawing.Point( 0, 0 );
			this.panelInfos.Name = "panelInfos";
			this.panelInfos.Size = new System.Drawing.Size( 158, 265 );
			this.panelInfos.TabIndex = 2;
			this.panelInfos.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.panelInfos_MouseDoubleClick );
			this.panelInfos.MouseDown += new System.Windows.Forms.MouseEventHandler( this.panelInfos_MouseDown );
			// 
			// groupBoxClipping
			// 
			this.groupBoxClipping.Controls.Add( this.floatTrackbarControlClipMax );
			this.groupBoxClipping.Controls.Add( this.label5 );
			this.groupBoxClipping.Controls.Add( this.floatTrackbarControlClipMin );
			this.groupBoxClipping.Controls.Add( this.checkBoxClipMaxInfinity );
			this.groupBoxClipping.Controls.Add( this.label4 );
			this.groupBoxClipping.Controls.Add( this.checkBoxClipMinInfinity );
			this.groupBoxClipping.Location = new System.Drawing.Point( 1, 121 );
			this.groupBoxClipping.Name = "groupBoxClipping";
			this.groupBoxClipping.Size = new System.Drawing.Size( 153, 107 );
			this.groupBoxClipping.TabIndex = 7;
			this.groupBoxClipping.TabStop = false;
			this.groupBoxClipping.Text = "Clipping";
			this.groupBoxClipping.Visible = false;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 5, 16 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 57, 13 );
			this.label5.TabIndex = 3;
			this.label5.Text = "Max Value";
			// 
			// checkBoxClipMaxInfinity
			// 
			this.checkBoxClipMaxInfinity.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxClipMaxInfinity.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxClipMaxInfinity.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "checkBoxClipMaxInfinity.BackgroundImage" )));
			this.checkBoxClipMaxInfinity.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.checkBoxClipMaxInfinity.Checked = true;
			this.checkBoxClipMaxInfinity.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxClipMaxInfinity.Location = new System.Drawing.Point( 127, 10 );
			this.checkBoxClipMaxInfinity.Name = "checkBoxClipMaxInfinity";
			this.checkBoxClipMaxInfinity.Size = new System.Drawing.Size( 22, 22 );
			this.checkBoxClipMaxInfinity.TabIndex = 0;
			this.checkBoxClipMaxInfinity.UseVisualStyleBackColor = true;
			this.checkBoxClipMaxInfinity.CheckedChanged += new System.EventHandler( this.checkBoxClipMaxInfinity_CheckedChanged );
			this.checkBoxClipMaxInfinity.MouseDown += new System.Windows.Forms.MouseEventHandler( this.checkBoxLoop_MouseDown );
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 6, 61 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 54, 13 );
			this.label4.TabIndex = 3;
			this.label4.Text = "Min Value";
			// 
			// checkBoxClipMinInfinity
			// 
			this.checkBoxClipMinInfinity.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxClipMinInfinity.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxClipMinInfinity.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "checkBoxClipMinInfinity.BackgroundImage" )));
			this.checkBoxClipMinInfinity.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.checkBoxClipMinInfinity.Checked = true;
			this.checkBoxClipMinInfinity.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxClipMinInfinity.Location = new System.Drawing.Point( 128, 55 );
			this.checkBoxClipMinInfinity.Name = "checkBoxClipMinInfinity";
			this.checkBoxClipMinInfinity.Size = new System.Drawing.Size( 22, 22 );
			this.checkBoxClipMinInfinity.TabIndex = 0;
			this.checkBoxClipMinInfinity.UseVisualStyleBackColor = true;
			this.checkBoxClipMinInfinity.CheckedChanged += new System.EventHandler( this.checkBoxClipMinInfinity_CheckedChanged );
			this.checkBoxClipMinInfinity.MouseDown += new System.Windows.Forms.MouseEventHandler( this.checkBoxLoop_MouseDown );
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 3, 79 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 85, 13 );
			this.label3.TabIndex = 3;
			this.label3.Text = "Interval Duration";
			// 
			// floatTrackbarControlIntervalDuration
			// 
			this.floatTrackbarControlIntervalDuration.Enabled = false;
			this.floatTrackbarControlIntervalDuration.Location = new System.Drawing.Point( 1, 95 );
			this.floatTrackbarControlIntervalDuration.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlIntervalDuration.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlIntervalDuration.Name = "floatTrackbarControlIntervalDuration";
			this.floatTrackbarControlIntervalDuration.RangeMin = 0F;
			this.floatTrackbarControlIntervalDuration.Size = new System.Drawing.Size( 153, 20 );
			this.floatTrackbarControlIntervalDuration.TabIndex = 2;
			this.floatTrackbarControlIntervalDuration.Value = 0F;
			this.floatTrackbarControlIntervalDuration.SliderDragStop += new SequencorEditor.FloatTrackbarControl.SliderDragStopEventHandler( this.floatTrackbarControlIntervalDuration_SliderDragStop );
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 3, 40 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 64, 13 );
			this.label2.TabIndex = 3;
			this.label2.Text = "Interval End";
			// 
			// floatTrackbarControlIntervalEnd
			// 
			this.floatTrackbarControlIntervalEnd.Enabled = false;
			this.floatTrackbarControlIntervalEnd.Location = new System.Drawing.Point( 1, 56 );
			this.floatTrackbarControlIntervalEnd.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlIntervalEnd.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlIntervalEnd.Name = "floatTrackbarControlIntervalEnd";
			this.floatTrackbarControlIntervalEnd.RangeMin = 0F;
			this.floatTrackbarControlIntervalEnd.Size = new System.Drawing.Size( 153, 20 );
			this.floatTrackbarControlIntervalEnd.TabIndex = 2;
			this.floatTrackbarControlIntervalEnd.Value = 0F;
			this.floatTrackbarControlIntervalEnd.SliderDragStop += new SequencorEditor.FloatTrackbarControl.SliderDragStopEventHandler( this.floatTrackbarControlIntervalEnd_SliderDragStop );
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 3, 1 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 67, 13 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Interval Start";
			// 
			// floatTrackbarControlIntervalStart
			// 
			this.floatTrackbarControlIntervalStart.Enabled = false;
			this.floatTrackbarControlIntervalStart.Location = new System.Drawing.Point( 1, 17 );
			this.floatTrackbarControlIntervalStart.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlIntervalStart.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlIntervalStart.Name = "floatTrackbarControlIntervalStart";
			this.floatTrackbarControlIntervalStart.RangeMin = 0F;
			this.floatTrackbarControlIntervalStart.Size = new System.Drawing.Size( 153, 20 );
			this.floatTrackbarControlIntervalStart.TabIndex = 2;
			this.floatTrackbarControlIntervalStart.Value = 0F;
			this.floatTrackbarControlIntervalStart.SliderDragStop += new SequencorEditor.FloatTrackbarControl.SliderDragStopEventHandler( this.floatTrackbarControlIntervalStart_SliderDragStop );
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.createKeyToolStripMenuItem,
            this.createKeyAtMousePositionToolStripMenuItem,
            this.createKeyAtMousePositionToolStripMenuItem1,
            this.createInterpolatedKeyToolStripMenuItem,
            this.createKeyAtCursorPositionToolStripMenuItem,
            this.createKeyAtCursorPositionToolStripMenuItem1,
            this.createKeyFromCurrentValueToolStripMenuItem,
            this.updateKeyFromCurrentValueToolStripMenuItem,
            this.updateKeyFromCurrentValueToolStripMenuItem1,
            this.editKeyToolStripMenuItem,
            this.alignKeyToCursorPositionToolStripMenuItem,
            this.moveKeyAtTimeToolStripMenuItem,
            this.toolStripMenuItem2,
            this.copyKeyToolStripMenuItem,
            this.pasteKeyToolStripMenuItem,
            this.toolStripMenuItem1,
            this.deleteKeyToolStripMenuItem} );
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size( 300, 368 );
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler( this.contextMenuStrip_Opening );
			// 
			// createKeyToolStripMenuItem
			// 
			this.createKeyToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "createKeyToolStripMenuItem.Image" )));
			this.createKeyToolStripMenuItem.Name = "createKeyToolStripMenuItem";
			this.createKeyToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.createKeyToolStripMenuItem.Text = "Create &Key...";
			this.createKeyToolStripMenuItem.ToolTipText = "Creates a new key, opening a form to edit its new value...";
			this.createKeyToolStripMenuItem.Click += new System.EventHandler( this.createKeyToolStripMenuItem_Click );
			// 
			// createKeyAtMousePositionToolStripMenuItem
			// 
			this.createKeyAtMousePositionToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "createKeyAtMousePositionToolStripMenuItem.Image" )));
			this.createKeyAtMousePositionToolStripMenuItem.Name = "createKeyAtMousePositionToolStripMenuItem";
			this.createKeyAtMousePositionToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.createKeyAtMousePositionToolStripMenuItem.Text = "&Create Key at Mouse Position";
			this.createKeyAtMousePositionToolStripMenuItem.ToolTipText = "Creates a new key at mouse position";
			this.createKeyAtMousePositionToolStripMenuItem.Click += new System.EventHandler( this.createKeyAtMousePositionToolStripMenuItem_Click );
			// 
			// createKeyAtMousePositionToolStripMenuItem1
			// 
			this.createKeyAtMousePositionToolStripMenuItem1.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.positionToolStripMenuItem,
            this.rotationToolStripMenuItem,
            this.scaleToolStripMenuItem,
            this.pRSToolStripMenuItem} );
			this.createKeyAtMousePositionToolStripMenuItem1.Image = ((System.Drawing.Image) (resources.GetObject( "createKeyAtMousePositionToolStripMenuItem1.Image" )));
			this.createKeyAtMousePositionToolStripMenuItem1.Name = "createKeyAtMousePositionToolStripMenuItem1";
			this.createKeyAtMousePositionToolStripMenuItem1.Size = new System.Drawing.Size( 299, 22 );
			this.createKeyAtMousePositionToolStripMenuItem1.Text = "&Create Key at Mouse Position";
			// 
			// positionToolStripMenuItem
			// 
			this.positionToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "positionToolStripMenuItem.Image" )));
			this.positionToolStripMenuItem.Name = "positionToolStripMenuItem";
			this.positionToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
			this.positionToolStripMenuItem.Text = "&Position";
			this.positionToolStripMenuItem.ToolTipText = "Creates a new position key at mouse position";
			this.positionToolStripMenuItem.Click += new System.EventHandler( this.positionToolStripMenuItem_Click );
			// 
			// rotationToolStripMenuItem
			// 
			this.rotationToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "rotationToolStripMenuItem.Image" )));
			this.rotationToolStripMenuItem.Name = "rotationToolStripMenuItem";
			this.rotationToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
			this.rotationToolStripMenuItem.Text = "&Rotation";
			this.rotationToolStripMenuItem.ToolTipText = "Creates a new rotation key at mouse position";
			this.rotationToolStripMenuItem.Click += new System.EventHandler( this.rotationToolStripMenuItem_Click );
			// 
			// scaleToolStripMenuItem
			// 
			this.scaleToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "scaleToolStripMenuItem.Image" )));
			this.scaleToolStripMenuItem.Name = "scaleToolStripMenuItem";
			this.scaleToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
			this.scaleToolStripMenuItem.Text = "&Scale";
			this.scaleToolStripMenuItem.ToolTipText = "Creates a new scale key at mouse position";
			this.scaleToolStripMenuItem.Click += new System.EventHandler( this.scaleToolStripMenuItem_Click );
			// 
			// pRSToolStripMenuItem
			// 
			this.pRSToolStripMenuItem.Name = "pRSToolStripMenuItem";
			this.pRSToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
			this.pRSToolStripMenuItem.Text = "PRS";
			this.pRSToolStripMenuItem.Click += new System.EventHandler( this.pRSToolStripMenuItem_Click );
			// 
			// createKeyAtCursorPositionToolStripMenuItem
			// 
			this.createKeyAtCursorPositionToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "createKeyAtCursorPositionToolStripMenuItem.Image" )));
			this.createKeyAtCursorPositionToolStripMenuItem.Name = "createKeyAtCursorPositionToolStripMenuItem";
			this.createKeyAtCursorPositionToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.createKeyAtCursorPositionToolStripMenuItem.Text = "C&reate Key at Cursor Position";
			this.createKeyAtCursorPositionToolStripMenuItem.ToolTipText = "Creates a new key at time cursor position";
			this.createKeyAtCursorPositionToolStripMenuItem.Click += new System.EventHandler( this.createKeyAtCursorPositionToolStripMenuItem_Click );
			// 
			// createKeyAtCursorPositionToolStripMenuItem1
			// 
			this.createKeyAtCursorPositionToolStripMenuItem1.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.positionToolStripMenuItem1,
            this.rotationToolStripMenuItem1,
            this.scaleToolStripMenuItem1,
            this.pRSToolStripMenuItem1} );
			this.createKeyAtCursorPositionToolStripMenuItem1.Image = ((System.Drawing.Image) (resources.GetObject( "createKeyAtCursorPositionToolStripMenuItem1.Image" )));
			this.createKeyAtCursorPositionToolStripMenuItem1.Name = "createKeyAtCursorPositionToolStripMenuItem1";
			this.createKeyAtCursorPositionToolStripMenuItem1.Size = new System.Drawing.Size( 299, 22 );
			this.createKeyAtCursorPositionToolStripMenuItem1.Text = "C&reate Key at Cursor Position";
			// 
			// positionToolStripMenuItem1
			// 
			this.positionToolStripMenuItem1.Image = ((System.Drawing.Image) (resources.GetObject( "positionToolStripMenuItem1.Image" )));
			this.positionToolStripMenuItem1.Name = "positionToolStripMenuItem1";
			this.positionToolStripMenuItem1.Size = new System.Drawing.Size( 119, 22 );
			this.positionToolStripMenuItem1.Text = "&Position";
			this.positionToolStripMenuItem1.ToolTipText = "Creates a new position key at time cursor position";
			this.positionToolStripMenuItem1.Click += new System.EventHandler( this.positionToolStripMenuItem1_Click );
			// 
			// rotationToolStripMenuItem1
			// 
			this.rotationToolStripMenuItem1.Image = ((System.Drawing.Image) (resources.GetObject( "rotationToolStripMenuItem1.Image" )));
			this.rotationToolStripMenuItem1.Name = "rotationToolStripMenuItem1";
			this.rotationToolStripMenuItem1.Size = new System.Drawing.Size( 119, 22 );
			this.rotationToolStripMenuItem1.Text = "&Rotation";
			this.rotationToolStripMenuItem1.ToolTipText = "Creates a new rotation key at time cursor position";
			this.rotationToolStripMenuItem1.Click += new System.EventHandler( this.rotationToolStripMenuItem1_Click );
			// 
			// scaleToolStripMenuItem1
			// 
			this.scaleToolStripMenuItem1.Image = ((System.Drawing.Image) (resources.GetObject( "scaleToolStripMenuItem1.Image" )));
			this.scaleToolStripMenuItem1.Name = "scaleToolStripMenuItem1";
			this.scaleToolStripMenuItem1.Size = new System.Drawing.Size( 119, 22 );
			this.scaleToolStripMenuItem1.Text = "&Scale";
			this.scaleToolStripMenuItem1.ToolTipText = "Creates a new scale key at time cursor position";
			this.scaleToolStripMenuItem1.Click += new System.EventHandler( this.scaleToolStripMenuItem1_Click );
			// 
			// pRSToolStripMenuItem1
			// 
			this.pRSToolStripMenuItem1.Name = "pRSToolStripMenuItem1";
			this.pRSToolStripMenuItem1.Size = new System.Drawing.Size( 119, 22 );
			this.pRSToolStripMenuItem1.Text = "PRS";
			this.pRSToolStripMenuItem1.ToolTipText = "Creates new PRS keys at time cursor position";
			this.pRSToolStripMenuItem1.Click += new System.EventHandler( this.pRSToolStripMenuItem1_Click );
			// 
			// createKeyFromCurrentValueToolStripMenuItem
			// 
			this.createKeyFromCurrentValueToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Key;
			this.createKeyFromCurrentValueToolStripMenuItem.Name = "createKeyFromCurrentValueToolStripMenuItem";
			this.createKeyFromCurrentValueToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.createKeyFromCurrentValueToolStripMenuItem.Text = "Cr&eate Key from Current Value";
			this.createKeyFromCurrentValueToolStripMenuItem.Click += new System.EventHandler( this.createKeyFromCurrentValueToolStripMenuItem_Click );
			// 
			// updateKeyFromCurrentValueToolStripMenuItem
			// 
			this.updateKeyFromCurrentValueToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "updateKeyFromCurrentValueToolStripMenuItem.Image" )));
			this.updateKeyFromCurrentValueToolStripMenuItem.Name = "updateKeyFromCurrentValueToolStripMenuItem";
			this.updateKeyFromCurrentValueToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Return)));
			this.updateKeyFromCurrentValueToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.updateKeyFromCurrentValueToolStripMenuItem.Text = "&Update Key from Current Value";
			this.updateKeyFromCurrentValueToolStripMenuItem.ToolTipText = "Updates the selected key from the current value of the parameter";
			this.updateKeyFromCurrentValueToolStripMenuItem.Click += new System.EventHandler( this.updateKeyFromCurrentValueToolStripMenuItem_Click );
			// 
			// updateKeyFromCurrentValueToolStripMenuItem1
			// 
			this.updateKeyFromCurrentValueToolStripMenuItem1.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.positionToolStripMenuItem2,
            this.rotationToolStripMenuItem2,
            this.scaleToolStripMenuItem2,
            this.pRSToolStripMenuItem2} );
			this.updateKeyFromCurrentValueToolStripMenuItem1.Image = ((System.Drawing.Image) (resources.GetObject( "updateKeyFromCurrentValueToolStripMenuItem1.Image" )));
			this.updateKeyFromCurrentValueToolStripMenuItem1.Name = "updateKeyFromCurrentValueToolStripMenuItem1";
			this.updateKeyFromCurrentValueToolStripMenuItem1.Size = new System.Drawing.Size( 299, 22 );
			this.updateKeyFromCurrentValueToolStripMenuItem1.Text = "&Update Key from Current Value";
			this.updateKeyFromCurrentValueToolStripMenuItem1.ToolTipText = "Updates the selected key from the current value of the parameter";
			// 
			// positionToolStripMenuItem2
			// 
			this.positionToolStripMenuItem2.Image = ((System.Drawing.Image) (resources.GetObject( "positionToolStripMenuItem2.Image" )));
			this.positionToolStripMenuItem2.Name = "positionToolStripMenuItem2";
			this.positionToolStripMenuItem2.Size = new System.Drawing.Size( 119, 22 );
			this.positionToolStripMenuItem2.Text = "&Position";
			this.positionToolStripMenuItem2.ToolTipText = "Updates the position key";
			this.positionToolStripMenuItem2.Click += new System.EventHandler( this.positionToolStripMenuItem2_Click );
			// 
			// rotationToolStripMenuItem2
			// 
			this.rotationToolStripMenuItem2.Image = ((System.Drawing.Image) (resources.GetObject( "rotationToolStripMenuItem2.Image" )));
			this.rotationToolStripMenuItem2.Name = "rotationToolStripMenuItem2";
			this.rotationToolStripMenuItem2.Size = new System.Drawing.Size( 119, 22 );
			this.rotationToolStripMenuItem2.Text = "&Rotation";
			this.rotationToolStripMenuItem2.ToolTipText = "Updates the rotation key";
			this.rotationToolStripMenuItem2.Click += new System.EventHandler( this.rotationToolStripMenuItem2_Click );
			// 
			// scaleToolStripMenuItem2
			// 
			this.scaleToolStripMenuItem2.Image = ((System.Drawing.Image) (resources.GetObject( "scaleToolStripMenuItem2.Image" )));
			this.scaleToolStripMenuItem2.Name = "scaleToolStripMenuItem2";
			this.scaleToolStripMenuItem2.Size = new System.Drawing.Size( 119, 22 );
			this.scaleToolStripMenuItem2.Text = "&Scale";
			this.scaleToolStripMenuItem2.ToolTipText = "Updates the scale key";
			this.scaleToolStripMenuItem2.Click += new System.EventHandler( this.scaleToolStripMenuItem2_Click );
			// 
			// pRSToolStripMenuItem2
			// 
			this.pRSToolStripMenuItem2.Name = "pRSToolStripMenuItem2";
			this.pRSToolStripMenuItem2.Size = new System.Drawing.Size( 119, 22 );
			this.pRSToolStripMenuItem2.Text = "PRS";
			this.pRSToolStripMenuItem2.ToolTipText = "Updates all  the PRS keys";
			this.pRSToolStripMenuItem2.Click += new System.EventHandler( this.pRSToolStripMenuItem2_Click );
			// 
			// editKeyToolStripMenuItem
			// 
			this.editKeyToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "editKeyToolStripMenuItem.Image" )));
			this.editKeyToolStripMenuItem.Name = "editKeyToolStripMenuItem";
			this.editKeyToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.editKeyToolStripMenuItem.Text = "&Edit Key...";
			this.editKeyToolStripMenuItem.Click += new System.EventHandler( this.editKeyToolStripMenuItem_Click );
			// 
			// alignKeyToCursorPositionToolStripMenuItem
			// 
			this.alignKeyToCursorPositionToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "alignKeyToCursorPositionToolStripMenuItem.Image" )));
			this.alignKeyToCursorPositionToolStripMenuItem.Name = "alignKeyToCursorPositionToolStripMenuItem";
			this.alignKeyToCursorPositionToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.alignKeyToCursorPositionToolStripMenuItem.Text = "&Align Key to Cursor Position";
			this.alignKeyToCursorPositionToolStripMenuItem.Click += new System.EventHandler( this.alignKeyToCursorPositionToolStripMenuItem_Click );
			// 
			// moveKeyAtTimeToolStripMenuItem
			// 
			this.moveKeyAtTimeToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "moveKeyAtTimeToolStripMenuItem.Image" )));
			this.moveKeyAtTimeToolStripMenuItem.Name = "moveKeyAtTimeToolStripMenuItem";
			this.moveKeyAtTimeToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.moveKeyAtTimeToolStripMenuItem.Text = "&Move Key at Time...";
			this.moveKeyAtTimeToolStripMenuItem.Click += new System.EventHandler( this.moveKeyAtTimeToolStripMenuItem_Click );
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size( 296, 6 );
			// 
			// copyKeyToolStripMenuItem
			// 
			this.copyKeyToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Copy;
			this.copyKeyToolStripMenuItem.Name = "copyKeyToolStripMenuItem";
			this.copyKeyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyKeyToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.copyKeyToolStripMenuItem.Text = "Co&py Key";
			this.copyKeyToolStripMenuItem.Click += new System.EventHandler( this.copyKeyToolStripMenuItem_Click );
			// 
			// pasteKeyToolStripMenuItem
			// 
			this.pasteKeyToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Paste;
			this.pasteKeyToolStripMenuItem.Name = "pasteKeyToolStripMenuItem";
			this.pasteKeyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys) ((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteKeyToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.pasteKeyToolStripMenuItem.Text = "Pa&ste Key";
			this.pasteKeyToolStripMenuItem.Click += new System.EventHandler( this.pasteKeyToolStripMenuItem_Click );
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size( 296, 6 );
			// 
			// deleteKeyToolStripMenuItem
			// 
			this.deleteKeyToolStripMenuItem.Image = ((System.Drawing.Image) (resources.GetObject( "deleteKeyToolStripMenuItem.Image" )));
			this.deleteKeyToolStripMenuItem.Name = "deleteKeyToolStripMenuItem";
			this.deleteKeyToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.deleteKeyToolStripMenuItem.Text = "&Delete Key";
			this.deleteKeyToolStripMenuItem.Click += new System.EventHandler( this.deleteKeyToolStripMenuItem_Click );
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 250;
			this.timer1.Tick += new System.EventHandler( this.timer1_Tick );
			// 
			// animationTrackPanel
			// 
			this.animationTrackPanel.BackColor = System.Drawing.Color.WhiteSmoke;
			this.animationTrackPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.animationTrackPanel.ClipLinesColor = System.Drawing.Color.Crimson;
			this.animationTrackPanel.ContextMenuStrip = this.contextMenuStrip;
			this.animationTrackPanel.Controls.Add( this.buttonExit );
			this.animationTrackPanel.CursorTimeColor = System.Drawing.Color.ForestGreen;
			this.animationTrackPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.animationTrackPanel.ForeColor = System.Drawing.Color.LightSteelBlue;
			this.animationTrackPanel.HoveredKey = null;
			this.animationTrackPanel.HoveredKeyTangentIn = null;
			this.animationTrackPanel.HoveredKeyTangentOut = null;
			this.animationTrackPanel.Location = new System.Drawing.Point( 158, 0 );
			this.animationTrackPanel.MainGraduationsColor = System.Drawing.Color.Black;
			this.animationTrackPanel.Name = "animationTrackPanel";
			this.animationTrackPanel.Owner = null;
			this.animationTrackPanel.PreventRefresh = false;
			this.animationTrackPanel.SelectedColor = System.Drawing.Color.IndianRed;
			this.animationTrackPanel.SelectedInterval = null;
			this.animationTrackPanel.SelectedIntervalColor = System.Drawing.Color.MistyRose;
			this.animationTrackPanel.SelectedKey = null;
			this.animationTrackPanel.Size = new System.Drawing.Size( 515, 235 );
			this.animationTrackPanel.SmallGraduationsColor = System.Drawing.Color.Gray;
			this.animationTrackPanel.TabIndex = 3;
			this.animationTrackPanel.Track = null;
			this.animationTrackPanel.RangeChanged += new System.EventHandler( this.animationTrackPanel_RangeChanged );
			this.animationTrackPanel.KeyDown += new System.Windows.Forms.KeyEventHandler( this.animationTrackPanel_KeyDown );
			this.animationTrackPanel.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.animationTrackPanel_MouseDoubleClick );
			this.animationTrackPanel.MouseDown += new System.Windows.Forms.MouseEventHandler( this.animationTrackPanel_MouseDown );
			this.animationTrackPanel.MouseMove += new System.Windows.Forms.MouseEventHandler( this.animationTrackPanel_MouseMove );
			this.animationTrackPanel.MouseUp += new System.Windows.Forms.MouseEventHandler( this.animationTrackPanel_MouseUp );
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonExit.BackgroundImage = ((System.Drawing.Image) (resources.GetObject( "buttonExit.BackgroundImage" )));
			this.buttonExit.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.buttonExit.Location = new System.Drawing.Point( 494, 3 );
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size( 16, 16 );
			this.buttonExit.TabIndex = 4;
			this.buttonExit.UseVisualStyleBackColor = true;
			this.buttonExit.Visible = false;
			this.buttonExit.Click += new System.EventHandler( this.buttonExit_Click );
			// 
			// gradientTrackPanel
			// 
			this.gradientTrackPanel.BackColor = System.Drawing.Color.Black;
			this.gradientTrackPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.gradientTrackPanel.Cursor = System.Windows.Forms.Cursors.Default;
			this.gradientTrackPanel.CursorTimeColor = System.Drawing.Color.ForestGreen;
			this.gradientTrackPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.gradientTrackPanel.Location = new System.Drawing.Point( 158, 235 );
			this.gradientTrackPanel.Name = "gradientTrackPanel";
			this.gradientTrackPanel.Owner = null;
			this.gradientTrackPanel.SelectedKey = null;
			this.gradientTrackPanel.Size = new System.Drawing.Size( 515, 30 );
			this.gradientTrackPanel.TabIndex = 4;
			this.gradientTrackPanel.Track = null;
			this.gradientTrackPanel.Visible = false;
			this.gradientTrackPanel.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler( this.gradientTrackPanel_MouseDoubleClick );
			this.gradientTrackPanel.MouseDown += new System.Windows.Forms.MouseEventHandler( this.gradientTrackPanel_MouseDown );
			this.gradientTrackPanel.MouseMove += new System.Windows.Forms.MouseEventHandler( this.gradientTrackPanel_MouseMove );
			// 
			// createInterpolatedKeyToolStripMenuItem
			// 
			this.createInterpolatedKeyToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.positionToolStripMenuItem3,
            this.rotationToolStripMenuItem3,
            this.scaleToolStripMenuItem3,
            this.pRSToolStripMenuItem3} );
			this.createInterpolatedKeyToolStripMenuItem.Image = global::SequencorEditor.Properties.Resources.Key;
			this.createInterpolatedKeyToolStripMenuItem.Name = "createInterpolatedKeyToolStripMenuItem";
			this.createInterpolatedKeyToolStripMenuItem.Size = new System.Drawing.Size( 299, 22 );
			this.createInterpolatedKeyToolStripMenuItem.Text = "Create Interpolated Key";
			// 
			// positionToolStripMenuItem3
			// 
			this.positionToolStripMenuItem3.Image = global::SequencorEditor.Properties.Resources.Tool___Position;
			this.positionToolStripMenuItem3.Name = "positionToolStripMenuItem3";
			this.positionToolStripMenuItem3.Size = new System.Drawing.Size( 152, 22 );
			this.positionToolStripMenuItem3.Text = "&Position";
			this.positionToolStripMenuItem3.Click += new System.EventHandler( this.positionToolStripMenuItem3_Click );
			// 
			// rotationToolStripMenuItem3
			// 
			this.rotationToolStripMenuItem3.Image = global::SequencorEditor.Properties.Resources.Tool___Rotation;
			this.rotationToolStripMenuItem3.Name = "rotationToolStripMenuItem3";
			this.rotationToolStripMenuItem3.Size = new System.Drawing.Size( 152, 22 );
			this.rotationToolStripMenuItem3.Text = "&Rotation";
			this.rotationToolStripMenuItem3.Click += new System.EventHandler( this.rotationToolStripMenuItem3_Click );
			// 
			// scaleToolStripMenuItem3
			// 
			this.scaleToolStripMenuItem3.Image = global::SequencorEditor.Properties.Resources.Tool___Scale;
			this.scaleToolStripMenuItem3.Name = "scaleToolStripMenuItem3";
			this.scaleToolStripMenuItem3.Size = new System.Drawing.Size( 152, 22 );
			this.scaleToolStripMenuItem3.Text = "&Scale";
			this.scaleToolStripMenuItem3.Click += new System.EventHandler( this.scaleToolStripMenuItem3_Click );
			// 
			// pRSToolStripMenuItem3
			// 
			this.pRSToolStripMenuItem3.Name = "pRSToolStripMenuItem3";
			this.pRSToolStripMenuItem3.Size = new System.Drawing.Size( 152, 22 );
			this.pRSToolStripMenuItem3.Text = "PRS";
			this.pRSToolStripMenuItem3.Click += new System.EventHandler( this.pRSToolStripMenuItem3_Click );
			// 
			// AnimationEditorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add( this.animationTrackPanel );
			this.Controls.Add( this.gradientTrackPanel );
			this.Controls.Add( this.panelInfos );
			this.MinimumSize = new System.Drawing.Size( 0, 150 );
			this.Name = "AnimationEditorControl";
			this.Size = new System.Drawing.Size( 673, 265 );
			this.panelInfos.ResumeLayout( false );
			this.panelInfos.PerformLayout();
			this.groupBoxClipping.ResumeLayout( false );
			this.groupBoxClipping.PerformLayout();
			this.contextMenuStrip.ResumeLayout( false );
			this.animationTrackPanel.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxInterpolation;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Panel panelInfos;
		private AnimationTrackPanel animationTrackPanel;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem deleteKeyToolStripMenuItem;
		private FloatTrackbarControl floatTrackbarControlIntervalStart;
		private System.Windows.Forms.Label label2;
		private FloatTrackbarControl floatTrackbarControlIntervalEnd;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label3;
		private FloatTrackbarControl floatTrackbarControlIntervalDuration;
		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.ToolStripMenuItem createKeyAtMousePositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createKeyAtCursorPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createKeyAtMousePositionToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem positionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem rotationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem scaleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createKeyAtCursorPositionToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem positionToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem rotationToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem scaleToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem updateKeyFromCurrentValueToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editKeyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem alignKeyToCursorPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveKeyAtTimeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createKeyToolStripMenuItem;
		private System.Windows.Forms.Button buttonSampleValue;
		private System.Windows.Forms.Button buttonZoomOut;
		private System.Windows.Forms.ToolStripMenuItem pRSToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pRSToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem updateKeyFromCurrentValueToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem positionToolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem rotationToolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem scaleToolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem pRSToolStripMenuItem2;
		private System.Windows.Forms.GroupBox groupBoxClipping;
		private System.Windows.Forms.Label label4;
		private FloatTrackbarControl floatTrackbarControlClipMin;
		private System.Windows.Forms.CheckBox checkBoxClipMinInfinity;
		private FloatTrackbarControl floatTrackbarControlClipMax;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox checkBoxClipMaxInfinity;
		private GradientTrackPanel gradientTrackPanel;
		private System.Windows.Forms.CheckBox checkBoxGradient;
		private System.Windows.Forms.ToolStripMenuItem copyKeyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteKeyToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.CheckBox checkBoxShowTangents;
		private System.Windows.Forms.ToolStripMenuItem createKeyFromCurrentValueToolStripMenuItem;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ToolStripMenuItem createInterpolatedKeyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem positionToolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem rotationToolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem scaleToolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem pRSToolStripMenuItem3;




	}
}
