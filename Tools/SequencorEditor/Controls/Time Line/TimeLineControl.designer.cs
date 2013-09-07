namespace SequencorEditor
{
	partial class TimeLineControl
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
			this.components = new System.ComponentModel.Container();
			this.timerScroll = new System.Windows.Forms.Timer( this.components );
			this.panelGraduation = new GraduationPanel();
			this.panelCursor = new CursorPanel();
			this.SuspendLayout();
			// 
			// timerScroll
			// 
			this.timerScroll.Interval = 50;
			this.timerScroll.Tick += new System.EventHandler( this.timerScroll_Tick );
			// 
			// panelGraduation
			// 
			this.panelGraduation.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.panelGraduation.BoundMax = 1F;
			this.panelGraduation.BoundMin = 0F;
			this.panelGraduation.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelGraduation.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.panelGraduation.GraduationColor = System.Drawing.Color.Silver;
			this.panelGraduation.LargeGraduationSize = 0.1F;
			this.panelGraduation.Location = new System.Drawing.Point( 0, 0 );
			this.panelGraduation.MaxVisibleGraduations = 100;
			this.panelGraduation.MediumGraduationSize = 0.05F;
			this.panelGraduation.Name = "panelGraduation";
			this.panelGraduation.ShowLargeGraduations = true;
			this.panelGraduation.ShowMediumGraduations = true;
			this.panelGraduation.ShowSmallGraduations = true;
			this.panelGraduation.Size = new System.Drawing.Size( 728, 30 );
			this.panelGraduation.SmallGraduationSize = 0.025F;
			this.panelGraduation.TabIndex = 1;
			this.panelGraduation.CustomMouseDoubleClick += new GraduationPanel.CustomMouseDoubleClickEventHandler( this.panelGraduation_CustomMouseDoubleClick );
			this.panelGraduation.CustomMouseHover += new GraduationPanel.CustomMouseHoverEventHandler( this.panelGraduation_CustomMouseHover );
			this.panelGraduation.MouseDown += new System.Windows.Forms.MouseEventHandler( this.panelGraduation_MouseDown );
			this.panelGraduation.MouseMove += new System.Windows.Forms.MouseEventHandler( this.panelGraduation_MouseMove );
			this.panelGraduation.CustomKeyDown += new GraduationPanel.CustomKeyDownEventHandler( this.panelGraduation_CustomKeyDown );
			this.panelGraduation.CustomMouseDown += new GraduationPanel.CustomMouseDownEventHandler( this.panelGraduation_CustomMouseDown );
			this.panelGraduation.MouseUp += new System.Windows.Forms.MouseEventHandler( this.panelGraduation_MouseUp );
			this.panelGraduation.CustomMouseUp += new GraduationPanel.CustomMouseUpEventHandler( this.panelGraduation_CustomMouseUp );
			this.panelGraduation.CustomMouseMove += new GraduationPanel.CustomMouseMoveEventHandler( this.panelGraduation_CustomMouseMove );
			this.panelGraduation.CustomPaint += new GraduationPanel.CustomPaintEventHandler( this.panelGraduation_CustomPaint );
			// 
			// panelCursor
			// 
			this.panelCursor.BackColor = System.Drawing.Color.Gainsboro;
			this.panelCursor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelCursor.BoundMax = 1F;
			this.panelCursor.BoundMin = 0F;
			this.panelCursor.CursorColor = System.Drawing.Color.ForestGreen;
			this.panelCursor.CursorPosition = 0.5F;
			this.panelCursor.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelCursor.Location = new System.Drawing.Point( 0, 30 );
			this.panelCursor.Name = "panelCursor";
			this.panelCursor.Size = new System.Drawing.Size( 728, 24 );
			this.panelCursor.TabIndex = 0;
			this.panelCursor.MouseDown += new System.Windows.Forms.MouseEventHandler( this.panelCursor_MouseDown );
			this.panelCursor.MouseMove += new System.Windows.Forms.MouseEventHandler( this.panelCursor_MouseMove );
			this.panelCursor.MouseUp += new System.Windows.Forms.MouseEventHandler( this.panelCursor_MouseUp );
			this.panelCursor.CursorMoved += new CursorPanel.CursorMovedEventHandler( this.panelCursor_CursorMoved );
			// 
			// TimeLineControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add( this.panelGraduation );
			this.Controls.Add( this.panelCursor );
			this.Name = "TimeLineControl";
			this.Size = new System.Drawing.Size( 728, 54 );
			this.ResumeLayout( false );

		}

		#endregion

		private CursorPanel panelCursor;
		private GraduationPanel panelGraduation;
		private System.Windows.Forms.Timer timerScroll;
	}
}
