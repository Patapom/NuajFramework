namespace SequencorEditor
{
	partial class SetTimeForm
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.integerTrackbarControlMinutes = new SequencorEditor.IntegerTrackbarControl();
			this.integerTrackbarControlMilliSeconds = new SequencorEditor.IntegerTrackbarControl();
			this.label4 = new System.Windows.Forms.Label();
			this.integerTrackbarControlSeconds = new SequencorEditor.IntegerTrackbarControl();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.integerTrackbarControl = new SequencorEditor.IntegerTrackbarControl();
			this.floatTrackbarControlTime = new SequencorEditor.FloatTrackbarControl();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add( this.groupBox2 );
			this.groupBox1.Controls.Add( this.integerTrackbarControl );
			this.groupBox1.Controls.Add( this.floatTrackbarControlTime );
			this.groupBox1.Controls.Add( this.label2 );
			this.groupBox1.Controls.Add( this.label1 );
			this.groupBox1.Location = new System.Drawing.Point( 12, 12 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 436, 210 );
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Time Setup";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add( this.integerTrackbarControlMinutes );
			this.groupBox2.Controls.Add( this.integerTrackbarControlMilliSeconds );
			this.groupBox2.Controls.Add( this.label4 );
			this.groupBox2.Controls.Add( this.integerTrackbarControlSeconds );
			this.groupBox2.Controls.Add( this.label5 );
			this.groupBox2.Controls.Add( this.label6 );
			this.groupBox2.Location = new System.Drawing.Point( 10, 88 );
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size( 420, 111 );
			this.groupBox2.TabIndex = 4;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Formatted Time";
			// 
			// integerTrackbarControlMinutes
			// 
			this.integerTrackbarControlMinutes.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.integerTrackbarControlMinutes.Location = new System.Drawing.Point( 41, 19 );
			this.integerTrackbarControlMinutes.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlMinutes.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlMinutes.Name = "integerTrackbarControlMinutes";
			this.integerTrackbarControlMinutes.RangeMin = 0;
			this.integerTrackbarControlMinutes.Size = new System.Drawing.Size( 304, 20 );
			this.integerTrackbarControlMinutes.TabIndex = 0;
			this.integerTrackbarControlMinutes.Value = 0;
			this.integerTrackbarControlMinutes.VisibleRangeMax = 10;
			this.integerTrackbarControlMinutes.ValueChanged += new SequencorEditor.IntegerTrackbarControl.ValueChangedEventHandler( this.integerTrackbarControlMinutes_ValueChanged );
			// 
			// integerTrackbarControlMilliSeconds
			// 
			this.integerTrackbarControlMilliSeconds.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.integerTrackbarControlMilliSeconds.Location = new System.Drawing.Point( 41, 71 );
			this.integerTrackbarControlMilliSeconds.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlMilliSeconds.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlMilliSeconds.Name = "integerTrackbarControlMilliSeconds";
			this.integerTrackbarControlMilliSeconds.RangeMax = 1000;
			this.integerTrackbarControlMilliSeconds.RangeMin = 0;
			this.integerTrackbarControlMilliSeconds.Size = new System.Drawing.Size( 304, 20 );
			this.integerTrackbarControlMilliSeconds.TabIndex = 2;
			this.integerTrackbarControlMilliSeconds.Value = 0;
			this.integerTrackbarControlMilliSeconds.VisibleRangeMax = 1000;
			this.integerTrackbarControlMilliSeconds.ValueChanged += new SequencorEditor.IntegerTrackbarControl.ValueChangedEventHandler( this.integerTrackbarControlMilliSeconds_ValueChanged );
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 354, 23 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 44, 13 );
			this.label4.TabIndex = 0;
			this.label4.Text = "Minutes";
			// 
			// integerTrackbarControlSeconds
			// 
			this.integerTrackbarControlSeconds.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.integerTrackbarControlSeconds.Location = new System.Drawing.Point( 41, 45 );
			this.integerTrackbarControlSeconds.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlSeconds.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlSeconds.Name = "integerTrackbarControlSeconds";
			this.integerTrackbarControlSeconds.RangeMax = 59;
			this.integerTrackbarControlSeconds.RangeMin = 0;
			this.integerTrackbarControlSeconds.Size = new System.Drawing.Size( 304, 20 );
			this.integerTrackbarControlSeconds.TabIndex = 1;
			this.integerTrackbarControlSeconds.Value = 0;
			this.integerTrackbarControlSeconds.VisibleRangeMax = 59;
			this.integerTrackbarControlSeconds.ValueChanged += new SequencorEditor.IntegerTrackbarControl.ValueChangedEventHandler( this.integerTrackbarControlSeconds_ValueChanged );
			// 
			// label5
			// 
			this.label5.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 354, 49 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 49, 13 );
			this.label5.TabIndex = 0;
			this.label5.Text = "Seconds";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 354, 75 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 64, 13 );
			this.label6.TabIndex = 0;
			this.label6.Text = "Milliseconds";
			// 
			// integerTrackbarControl
			// 
			this.integerTrackbarControl.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.integerTrackbarControl.Location = new System.Drawing.Point( 160, 52 );
			this.integerTrackbarControl.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControl.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControl.Name = "integerTrackbarControl";
			this.integerTrackbarControl.RangeMin = 0;
			this.integerTrackbarControl.Size = new System.Drawing.Size( 270, 20 );
			this.integerTrackbarControl.TabIndex = 1;
			this.integerTrackbarControl.Value = 0;
			this.integerTrackbarControl.VisibleRangeMax = 10000;
			this.integerTrackbarControl.ValueChanged += new SequencorEditor.IntegerTrackbarControl.ValueChangedEventHandler( this.integerTrackbarControl_ValueChanged );
			// 
			// floatTrackbarControlTime
			// 
			this.floatTrackbarControlTime.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlTime.Location = new System.Drawing.Point( 160, 28 );
			this.floatTrackbarControlTime.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlTime.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlTime.Name = "floatTrackbarControlTime";
			this.floatTrackbarControlTime.RangeMin = 0F;
			this.floatTrackbarControlTime.Size = new System.Drawing.Size( 270, 20 );
			this.floatTrackbarControlTime.TabIndex = 0;
			this.floatTrackbarControlTime.Value = 0F;
			this.floatTrackbarControlTime.ValueChanged += new SequencorEditor.FloatTrackbarControl.ValueChangedEventHandler( this.floatTrackbarControlTime_ValueChanged );
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 7, 57 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 142, 13 );
			this.label2.TabIndex = 0;
			this.label2.Text = "Integer Time (in milliseconds)";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 7, 31 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 131, 13 );
			this.label1.TabIndex = 0;
			this.label1.Text = "Decimal Time (in seconds)";
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 373, 228 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 75, 23 );
			this.buttonCancel.TabIndex = 2;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point( 292, 228 );
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size( 75, 23 );
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// SetTimeForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 460, 263 );
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.groupBox1 );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "SetTimeForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Time Edition";
			this.groupBox1.ResumeLayout( false );
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout( false );
			this.groupBox2.PerformLayout();
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private FloatTrackbarControl floatTrackbarControlTime;
		private IntegerTrackbarControl integerTrackbarControl;
		private IntegerTrackbarControl integerTrackbarControlMilliSeconds;
		private IntegerTrackbarControl integerTrackbarControlSeconds;
		private IntegerTrackbarControl integerTrackbarControlMinutes;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.GroupBox groupBox2;
	}
}