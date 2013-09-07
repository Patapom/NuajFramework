namespace Pognac
{
	partial class PreferencesForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( PreferencesForm ) );
			this.buttonSave = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxWorkingDirectory = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.textBoxInfos = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonSave.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonSave.Location = new System.Drawing.Point( 247, 339 );
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.Size = new System.Drawing.Size( 99, 28 );
			this.buttonSave.TabIndex = 1;
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
			this.label1.Size = new System.Drawing.Size( 142, 20 );
			this.label1.TabIndex = 2;
			this.label1.Text = "Working Directory :";
			// 
			// textBoxWorkingDirectory
			// 
			this.textBoxWorkingDirectory.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxWorkingDirectory.Enabled = false;
			this.textBoxWorkingDirectory.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxWorkingDirectory.Location = new System.Drawing.Point( 160, 18 );
			this.textBoxWorkingDirectory.Name = "textBoxWorkingDirectory";
			this.textBoxWorkingDirectory.ReadOnly = true;
			this.textBoxWorkingDirectory.Size = new System.Drawing.Size( 378, 26 );
			this.textBoxWorkingDirectory.TabIndex = 3;
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonBrowse.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonBrowse.Location = new System.Drawing.Point( 548, 17 );
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size( 32, 28 );
			this.buttonBrowse.TabIndex = 1;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler( this.buttonBrowse_Click );
			// 
			// folderBrowserDialog
			// 
			this.folderBrowserDialog.Description = "Browse for a database root directory...";
			this.folderBrowserDialog.ShowNewFolderButton = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add( this.textBoxInfos );
			this.groupBox1.Location = new System.Drawing.Point( 12, 50 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 568, 283 );
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Infos";
			// 
			// textBoxInfos
			// 
			this.textBoxInfos.BackColor = System.Drawing.SystemColors.Info;
			this.textBoxInfos.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxInfos.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10F );
			this.textBoxInfos.Location = new System.Drawing.Point( 3, 16 );
			this.textBoxInfos.Multiline = true;
			this.textBoxInfos.Name = "textBoxInfos";
			this.textBoxInfos.ReadOnly = true;
			this.textBoxInfos.Size = new System.Drawing.Size( 562, 264 );
			this.textBoxInfos.TabIndex = 0;
			this.textBoxInfos.Text = resources.GetString( "textBoxInfos.Text" );
			// 
			// PreferencesForm
			// 
			this.AcceptButton = this.buttonSave;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 592, 379 );
			this.Controls.Add( this.groupBox1 );
			this.Controls.Add( this.textBoxWorkingDirectory );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.buttonBrowse );
			this.Controls.Add( this.buttonSave );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.Name = "PreferencesForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Preferences";
			this.groupBox1.ResumeLayout( false );
			this.groupBox1.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSave;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxWorkingDirectory;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TextBox textBoxInfos;
	}
}