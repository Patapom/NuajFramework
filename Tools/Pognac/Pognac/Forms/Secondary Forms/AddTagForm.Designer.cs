namespace Pognac
{
	partial class AddTagForm
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
			this.listBoxTags = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// listBoxTags
			// 
			this.listBoxTags.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxTags.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.listBoxTags.FormattingEnabled = true;
			this.listBoxTags.IntegralHeight = false;
			this.listBoxTags.ItemHeight = 20;
			this.listBoxTags.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxTags.Name = "listBoxTags";
			this.listBoxTags.Size = new System.Drawing.Size( 169, 286 );
			this.listBoxTags.TabIndex = 3;
			this.listBoxTags.SelectedIndexChanged += new System.EventHandler( this.listBoxTags_SelectedIndexChanged );
			// 
			// AddTagForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 169, 286 );
			this.Controls.Add( this.listBoxTags );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.KeyPreview = true;
			this.Name = "AddTagForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Add Tag";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxTags;

	}
}