namespace SequencorEditor
{
	partial class FoldableTrackControl
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
			this.animationEditorControl = new SequencorEditor.AnimationEditorControl();
			this.trackControl = new SequencorEditor.TrackControl();
			this.SuspendLayout();
			// 
			// animationEditorControl
			// 
			this.animationEditorControl.AutoSize = true;
			this.animationEditorControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.animationEditorControl.BackColor = System.Drawing.SystemColors.Control;
			this.animationEditorControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.animationEditorControl.Location = new System.Drawing.Point( 0, 33 );
			this.animationEditorControl.Margin = new System.Windows.Forms.Padding( 1 );
			this.animationEditorControl.MinimumSize = new System.Drawing.Size( 0, 150 );
			this.animationEditorControl.Name = "animationEditorControl";
			this.animationEditorControl.Owner = null;
			this.animationEditorControl.SelectedInterval = null;
			this.animationEditorControl.SelectedKey = null;
			this.animationEditorControl.ShowGradientTrack = false;
			this.animationEditorControl.Size = new System.Drawing.Size( 799, 150 );
			this.animationEditorControl.TabIndex = 0;
			this.animationEditorControl.Track = null;
			this.animationEditorControl.Visible = false;
			this.animationEditorControl.SelectedIntervalChanged += new System.EventHandler( this.animationEditorControl_SelectedIntervalChanged );
			this.animationEditorControl.SelectedKeyChanged += new System.EventHandler( this.animationEditorControl_SelectedKeyChanged );
			this.animationEditorControl.Exit += new System.EventHandler( this.animationEditorControl_Exit );
			// 
			// trackControl
			// 
			this.trackControl.AnimationTrackVisible = false;
			this.trackControl.BackColor = System.Drawing.SystemColors.Control;
			this.trackControl.Dock = System.Windows.Forms.DockStyle.Top;
			this.trackControl.Location = new System.Drawing.Point( 0, 0 );
			this.trackControl.Margin = new System.Windows.Forms.Padding( 1 );
			this.trackControl.MaximumSize = new System.Drawing.Size( 10000, 60 );
			this.trackControl.Name = "trackControl";
			this.trackControl.Owner = null;
			this.trackControl.Selected = false;
			this.trackControl.SelectedInterval = null;
			this.trackControl.Size = new System.Drawing.Size( 799, 33 );
			this.trackControl.TabIndex = 1;
			this.trackControl.Track = null;
			this.trackControl.TrackColor = System.Drawing.Color.RoyalBlue;
			this.trackControl.IntervalEdit += new SequencorEditor.TrackControl.IntervalEditEventHandler( this.trackControl_IntervalEdit );
			this.trackControl.TrackRename += new System.EventHandler( this.trackControl_TrackRename );
			this.trackControl.SelectedIntervalChanged += new System.EventHandler( this.trackControl_SelectedIntervalChanged );
			this.trackControl.AnimationTrackVisibleStateChanged += new System.EventHandler( this.trackControl_AnimationTrackVisibleStateChanged );
			this.trackControl.MouseDown += new System.Windows.Forms.MouseEventHandler( this.trackControl_MouseDown );
			// 
			// FoldableTrackControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.Controls.Add( this.animationEditorControl );
			this.Controls.Add( this.trackControl );
			this.Name = "FoldableTrackControl";
			this.Size = new System.Drawing.Size( 799, 183 );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private AnimationEditorControl animationEditorControl;
		private TrackControl trackControl;
	}
}
