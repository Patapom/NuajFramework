namespace Pognac
{
	partial class AnnotationControl
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
			this.richTextBox = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.labelAttachment = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// richTextBox
			// 
			this.richTextBox.AcceptsTab = true;
			this.richTextBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.richTextBox.Location = new System.Drawing.Point( 3, 3 );
			this.richTextBox.Name = "richTextBox";
			this.richTextBox.Size = new System.Drawing.Size( 619, 441 );
			this.richTextBox.TabIndex = 0;
			this.richTextBox.Text = "";
			this.richTextBox.Leave += new System.EventHandler( this.richTextBox_Leave );
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.label1.Location = new System.Drawing.Point( 3, 447 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 100, 20 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Attachment :";
			// 
			// labelAttachment
			// 
			this.labelAttachment.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelAttachment.AutoEllipsis = true;
			this.labelAttachment.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelAttachment.Font = new System.Drawing.Font( "Microsoft Sans Serif", 12F );
			this.labelAttachment.Location = new System.Drawing.Point( 109, 447 );
			this.labelAttachment.Name = "labelAttachment";
			this.labelAttachment.Size = new System.Drawing.Size( 513, 20 );
			this.labelAttachment.TabIndex = 3;
			// 
			// AnnotationControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add( this.labelAttachment );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.richTextBox );
			this.Enabled = false;
			this.Name = "AnnotationControl";
			this.Size = new System.Drawing.Size( 625, 467 );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox richTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelAttachment;
	}
}
