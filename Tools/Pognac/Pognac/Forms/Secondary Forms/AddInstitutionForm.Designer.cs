namespace Pognac
{
	partial class AddInstitutionForm
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
			this.listBoxInstitutions = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// listBoxInstitutions
			// 
			this.listBoxInstitutions.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxInstitutions.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.listBoxInstitutions.FormattingEnabled = true;
			this.listBoxInstitutions.IntegralHeight = false;
			this.listBoxInstitutions.ItemHeight = 20;
			this.listBoxInstitutions.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxInstitutions.Name = "listBoxInstitutions";
			this.listBoxInstitutions.Size = new System.Drawing.Size( 169, 286 );
			this.listBoxInstitutions.TabIndex = 3;
			this.listBoxInstitutions.SelectedIndexChanged += new System.EventHandler( this.listBoxInstitutions_SelectedIndexChanged );
			// 
			// AddInstitutionForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 169, 286 );
			this.Controls.Add( this.listBoxInstitutions );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.KeyPreview = true;
			this.Name = "AddInstitutionForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Add Institution";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxInstitutions;

	}
}