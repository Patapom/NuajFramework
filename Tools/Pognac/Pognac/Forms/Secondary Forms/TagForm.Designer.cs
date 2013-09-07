namespace Pognac
{
	partial class TagForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( TagForm ) );
			this.buttonSave = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxTagName = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonSave.Enabled = false;
			this.buttonSave.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonSave.Location = new System.Drawing.Point( 137, 87 );
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
			this.label1.Size = new System.Drawing.Size( 90, 20 );
			this.label1.TabIndex = 2;
			this.label1.Text = "Tag Name :";
			// 
			// textBoxTagName
			// 
			this.textBoxTagName.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTagName.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxTagName.Location = new System.Drawing.Point( 108, 18 );
			this.textBoxTagName.Name = "textBoxTagName";
			this.textBoxTagName.Size = new System.Drawing.Size( 252, 26 );
			this.textBoxTagName.TabIndex = 3;
			this.textBoxTagName.TextChanged += new System.EventHandler( this.textBoxTagName_TextChanged );
			// 
			// TagForm
			// 
			this.AcceptButton = this.buttonSave;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 372, 127 );
			this.Controls.Add( this.textBoxTagName );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.buttonSave );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.Name = "TagForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Tag Editor";
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSave;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxTagName;
	}
}