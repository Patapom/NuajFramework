namespace Nuaj.Cirrus.Utility
{
	partial class ProfilerForm
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
			this.listView = new System.Windows.Forms.ListView();
			this.columnHeaderSource = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderInfos = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderTime = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderTimeAccum = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader()));
			this.trackBarDelay = new System.Windows.Forms.TrackBar();
			this.checkBoxFlush = new System.Windows.Forms.CheckBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkBoxPause = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize) (this.trackBarDelay)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// listView
			// 
			this.listView.Columns.AddRange( new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderSource,
            this.columnHeaderInfos,
            this.columnHeaderTime,
            this.columnHeaderTimeAccum} );
			this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView.FullRowSelect = true;
			this.listView.GridLines = true;
			this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listView.HideSelection = false;
			this.listView.Location = new System.Drawing.Point( 0, 22 );
			this.listView.Name = "listView";
			this.listView.Size = new System.Drawing.Size( 441, 527 );
			this.listView.TabIndex = 0;
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			// 
			// columnHeaderSource
			// 
			this.columnHeaderSource.Text = "Source";
			this.columnHeaderSource.Width = 100;
			// 
			// columnHeaderInfos
			// 
			this.columnHeaderInfos.Text = "Infos";
			this.columnHeaderInfos.Width = 150;
			// 
			// columnHeaderTime
			// 
			this.columnHeaderTime.Text = "Time";
			this.columnHeaderTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.columnHeaderTime.Width = 80;
			// 
			// columnHeaderTimeAccum
			// 
			this.columnHeaderTimeAccum.Text = "Accum.";
			this.columnHeaderTimeAccum.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.columnHeaderTimeAccum.Width = 80;
			// 
			// trackBarDelay
			// 
			this.trackBarDelay.AutoSize = false;
			this.trackBarDelay.BackColor = System.Drawing.SystemColors.Control;
			this.trackBarDelay.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.trackBarDelay.Location = new System.Drawing.Point( 0, 549 );
			this.trackBarDelay.Maximum = 100;
			this.trackBarDelay.Minimum = 1;
			this.trackBarDelay.Name = "trackBarDelay";
			this.trackBarDelay.Size = new System.Drawing.Size( 441, 32 );
			this.trackBarDelay.TabIndex = 1;
			this.trackBarDelay.TickFrequency = 5;
			this.trackBarDelay.Value = 40;
			// 
			// checkBoxFlush
			// 
			this.checkBoxFlush.AutoSize = true;
			this.checkBoxFlush.Location = new System.Drawing.Point( 3, 3 );
			this.checkBoxFlush.Name = "checkBoxFlush";
			this.checkBoxFlush.Size = new System.Drawing.Size( 204, 17 );
			this.checkBoxFlush.TabIndex = 2;
			this.checkBoxFlush.Text = "Flush GPU Commands on Every Task";
			this.checkBoxFlush.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Controls.Add( this.checkBoxPause );
			this.panel1.Controls.Add( this.checkBoxFlush );
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point( 0, 0 );
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size( 441, 22 );
			this.panel1.TabIndex = 3;
			// 
			// checkBoxPause
			// 
			this.checkBoxPause.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxPause.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.checkBoxPause.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7F );
			this.checkBoxPause.Location = new System.Drawing.Point( 379, 0 );
			this.checkBoxPause.Margin = new System.Windows.Forms.Padding( 0 );
			this.checkBoxPause.Name = "checkBoxPause";
			this.checkBoxPause.Size = new System.Drawing.Size( 59, 22 );
			this.checkBoxPause.TabIndex = 2;
			this.checkBoxPause.Text = "Pause";
			this.checkBoxPause.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.checkBoxPause.UseVisualStyleBackColor = true;
			// 
			// ProfilerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 441, 581 );
			this.Controls.Add( this.listView );
			this.Controls.Add( this.panel1 );
			this.Controls.Add( this.trackBarDelay );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "ProfilerForm";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Profiler Form";
			((System.ComponentModel.ISupportInitialize) (this.trackBarDelay)).EndInit();
			this.panel1.ResumeLayout( false );
			this.panel1.PerformLayout();
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.ColumnHeader columnHeaderSource;
		private System.Windows.Forms.ColumnHeader columnHeaderInfos;
		private System.Windows.Forms.ColumnHeader columnHeaderTime;
		private System.Windows.Forms.ColumnHeader columnHeaderTimeAccum;
		private System.Windows.Forms.TrackBar trackBarDelay;
		private System.Windows.Forms.CheckBox checkBoxFlush;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckBox checkBoxPause;
	}
}