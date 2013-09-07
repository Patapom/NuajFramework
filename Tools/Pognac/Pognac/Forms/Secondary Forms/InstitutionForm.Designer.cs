namespace Pognac
{
	partial class InstitutionForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( InstitutionForm ) );
			this.buttonSave = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxInstitutionName = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.annotationControl = new Pognac.AnnotationControl();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonSave.Enabled = false;
			this.buttonSave.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold );
			this.buttonSave.Location = new System.Drawing.Point( 216, 279 );
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
			this.label1.Size = new System.Drawing.Size( 133, 20 );
			this.label1.TabIndex = 2;
			this.label1.Text = "Institution Name :";
			// 
			// textBoxInstitutionName
			// 
			this.textBoxInstitutionName.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxInstitutionName.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.textBoxInstitutionName.Location = new System.Drawing.Point( 151, 18 );
			this.textBoxInstitutionName.Name = "textBoxInstitutionName";
			this.textBoxInstitutionName.Size = new System.Drawing.Size( 367, 26 );
			this.textBoxInstitutionName.TabIndex = 0;
			this.textBoxInstitutionName.TextChanged += new System.EventHandler( this.textBoxInstitutionName_TextChanged );
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add( this.annotationControl );
			this.groupBox1.Location = new System.Drawing.Point( 12, 50 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 506, 223 );
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Annotations";
			// 
			// annotationControl
			// 
			this.annotationControl.Annotation = null;
			this.annotationControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.annotationControl.Enabled = false;
			this.annotationControl.Location = new System.Drawing.Point( 3, 16 );
			this.annotationControl.Name = "annotationControl";
			this.annotationControl.Size = new System.Drawing.Size( 500, 204 );
			this.annotationControl.TabIndex = 0;
			// 
			// InstitutionForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 530, 319 );
			this.Controls.Add( this.groupBox1 );
			this.Controls.Add( this.textBoxInstitutionName );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.buttonSave );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon) (resources.GetObject( "$this.Icon" )));
			this.MinimumSize = new System.Drawing.Size( 490, 270 );
			this.Name = "InstitutionForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Institution Editor";
			this.groupBox1.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSave;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxInstitutionName;
		private AnnotationControl annotationControl;
		private System.Windows.Forms.GroupBox groupBox1;
	}
}