namespace Demo
{
	partial class ShadowMapViewForm
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
			this.components = new System.ComponentModel.Container();
			this.shadowMapOutputPanel = new Demo.ShadowMapOutputPanel(this.components);
			this.SuspendLayout();
			// 
			// shadowMapOutputPanel
			// 
			this.shadowMapOutputPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.shadowMapOutputPanel.Location = new System.Drawing.Point(12, 12);
			this.shadowMapOutputPanel.Name = "shadowMapOutputPanel";
			this.shadowMapOutputPanel.Size = new System.Drawing.Size(465, 443);
			this.shadowMapOutputPanel.TabIndex = 0;
			this.shadowMapOutputPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.shadowMapOutputPanel_MouseDown);
			// 
			// ShadowMapViewForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(489, 467);
			this.Controls.Add(this.shadowMapOutputPanel);
			this.Name = "ShadowMapViewForm";
			this.Text = "Shadow Map Viewer";
			this.ResumeLayout(false);

		}

		#endregion

		private ShadowMapOutputPanel shadowMapOutputPanel;
	}
}