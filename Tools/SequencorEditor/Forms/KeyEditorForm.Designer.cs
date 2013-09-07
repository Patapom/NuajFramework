namespace SequencorEditor
{
	partial class KeyEditorForm
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panelInteger = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.panelEvent = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxEventGUID = new System.Windows.Forms.TextBox();
			this.panelFloat = new System.Windows.Forms.Panel();
			this.labelW = new System.Windows.Forms.Label();
			this.labelZ = new System.Windows.Forms.Label();
			this.labelY = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.panelBool = new System.Windows.Forms.Panel();
			this.checkBoxValueBool = new System.Windows.Forms.CheckBox();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.labelKeyType = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.buttonEditTime = new System.Windows.Forms.Button();
			this.buttonSampleValue = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip( this.components );
			this.buttonColorPicker = new System.Windows.Forms.Button();
			this.labelLabelKeyName = new System.Windows.Forms.Label();
			this.labelKeyName = new System.Windows.Forms.Label();
			this.floatTrackbarControlTime = new SequencorEditor.FloatTrackbarControl();
			this.integerTrackbarControl = new SequencorEditor.IntegerTrackbarControl();
			this.floatTrackbarControlFloatW = new SequencorEditor.FloatTrackbarControl();
			this.floatTrackbarControlFloatZ = new SequencorEditor.FloatTrackbarControl();
			this.floatTrackbarControlFloatY = new SequencorEditor.FloatTrackbarControl();
			this.floatTrackbarControlFloatX = new SequencorEditor.FloatTrackbarControl();
			this.groupBox1.SuspendLayout();
			this.panelInteger.SuspendLayout();
			this.panelEvent.SuspendLayout();
			this.panelFloat.SuspendLayout();
			this.panelBool.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add( this.panelInteger );
			this.groupBox1.Controls.Add( this.panelEvent );
			this.groupBox1.Controls.Add( this.panelFloat );
			this.groupBox1.Controls.Add( this.panelBool );
			this.groupBox1.Location = new System.Drawing.Point( 12, 70 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 401, 110 );
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Key Value";
			// 
			// panelInteger
			// 
			this.panelInteger.Controls.Add( this.label2 );
			this.panelInteger.Controls.Add( this.integerTrackbarControl );
			this.panelInteger.Location = new System.Drawing.Point( 13, 67 );
			this.panelInteger.Name = "panelInteger";
			this.panelInteger.Size = new System.Drawing.Size( 263, 31 );
			this.panelInteger.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 3, 4 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 34, 13 );
			this.label2.TabIndex = 0;
			this.label2.Text = "Value";
			// 
			// panelEvent
			// 
			this.panelEvent.Controls.Add( this.label1 );
			this.panelEvent.Controls.Add( this.textBoxEventGUID );
			this.panelEvent.Location = new System.Drawing.Point( 106, 24 );
			this.panelEvent.Name = "panelEvent";
			this.panelEvent.Size = new System.Drawing.Size( 263, 31 );
			this.panelEvent.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 3, 4 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 65, 13 );
			this.label1.TabIndex = 0;
			this.label1.Text = "Event GUID";
			// 
			// textBoxEventGUID
			// 
			this.textBoxEventGUID.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxEventGUID.Location = new System.Drawing.Point( 74, 1 );
			this.textBoxEventGUID.Name = "textBoxEventGUID";
			this.textBoxEventGUID.Size = new System.Drawing.Size( 121, 20 );
			this.textBoxEventGUID.TabIndex = 0;
			this.textBoxEventGUID.Text = "0";
			this.textBoxEventGUID.Validating += new System.ComponentModel.CancelEventHandler( this.textBoxEventGUID_Validating );
			// 
			// panelFloat
			// 
			this.panelFloat.Controls.Add( this.floatTrackbarControlFloatW );
			this.panelFloat.Controls.Add( this.labelW );
			this.panelFloat.Controls.Add( this.floatTrackbarControlFloatZ );
			this.panelFloat.Controls.Add( this.labelZ );
			this.panelFloat.Controls.Add( this.floatTrackbarControlFloatY );
			this.panelFloat.Controls.Add( this.labelY );
			this.panelFloat.Controls.Add( this.floatTrackbarControlFloatX );
			this.panelFloat.Controls.Add( this.label5 );
			this.panelFloat.Location = new System.Drawing.Point( 52, 64 );
			this.panelFloat.Name = "panelFloat";
			this.panelFloat.Size = new System.Drawing.Size( 258, 85 );
			this.panelFloat.TabIndex = 3;
			// 
			// labelW
			// 
			this.labelW.AutoSize = true;
			this.labelW.Location = new System.Drawing.Point( 3, 67 );
			this.labelW.Name = "labelW";
			this.labelW.Size = new System.Drawing.Size( 18, 13 );
			this.labelW.TabIndex = 0;
			this.labelW.Text = "W";
			// 
			// labelZ
			// 
			this.labelZ.AutoSize = true;
			this.labelZ.Location = new System.Drawing.Point( 3, 46 );
			this.labelZ.Name = "labelZ";
			this.labelZ.Size = new System.Drawing.Size( 14, 13 );
			this.labelZ.TabIndex = 0;
			this.labelZ.Text = "Z";
			// 
			// labelY
			// 
			this.labelY.AutoSize = true;
			this.labelY.Location = new System.Drawing.Point( 3, 25 );
			this.labelY.Name = "labelY";
			this.labelY.Size = new System.Drawing.Size( 14, 13 );
			this.labelY.TabIndex = 0;
			this.labelY.Text = "Y";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 3, 4 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 14, 13 );
			this.label5.TabIndex = 0;
			this.label5.Text = "X";
			// 
			// panelBool
			// 
			this.panelBool.Controls.Add( this.checkBoxValueBool );
			this.panelBool.Location = new System.Drawing.Point( 10, 24 );
			this.panelBool.Name = "panelBool";
			this.panelBool.Size = new System.Drawing.Size( 83, 31 );
			this.panelBool.TabIndex = 3;
			// 
			// checkBoxValueBool
			// 
			this.checkBoxValueBool.AutoSize = true;
			this.checkBoxValueBool.Location = new System.Drawing.Point( 3, 3 );
			this.checkBoxValueBool.Name = "checkBoxValueBool";
			this.checkBoxValueBool.Size = new System.Drawing.Size( 51, 17 );
			this.checkBoxValueBool.TabIndex = 0;
			this.checkBoxValueBool.Text = "State";
			this.checkBoxValueBool.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 338, 188 );
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
			this.buttonOK.Location = new System.Drawing.Point( 257, 188 );
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size( 75, 23 );
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 12, 10 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 52, 13 );
			this.label4.TabIndex = 0;
			this.label4.Text = "Key Type";
			// 
			// labelKeyType
			// 
			this.labelKeyType.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelKeyType.BackColor = System.Drawing.SystemColors.Info;
			this.labelKeyType.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.labelKeyType.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.labelKeyType.Location = new System.Drawing.Point( 81, 8 );
			this.labelKeyType.Name = "labelKeyType";
			this.labelKeyType.Size = new System.Drawing.Size( 84, 17 );
			this.labelKeyType.TabIndex = 0;
			this.labelKeyType.Text = "<UNKNOWN>";
			this.labelKeyType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 12, 34 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 51, 13 );
			this.label6.TabIndex = 0;
			this.label6.Text = "Key Time";
			// 
			// buttonEditTime
			// 
			this.buttonEditTime.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.buttonEditTime.Location = new System.Drawing.Point( 387, 29 );
			this.buttonEditTime.Name = "buttonEditTime";
			this.buttonEditTime.Size = new System.Drawing.Size( 26, 23 );
			this.buttonEditTime.TabIndex = 5;
			this.buttonEditTime.Text = "...";
			this.toolTip1.SetToolTip( this.buttonEditTime, "Edit accurate time" );
			this.buttonEditTime.UseVisualStyleBackColor = true;
			this.buttonEditTime.Click += new System.EventHandler( this.buttonEditTime_Click );
			// 
			// buttonSampleValue
			// 
			this.buttonSampleValue.Image = global::SequencorEditor.Properties.Resources.eyedropper;
			this.buttonSampleValue.Location = new System.Drawing.Point( 102, 63 );
			this.buttonSampleValue.Name = "buttonSampleValue";
			this.buttonSampleValue.Size = new System.Drawing.Size( 22, 22 );
			this.buttonSampleValue.TabIndex = 4;
			this.toolTip1.SetToolTip( this.buttonSampleValue, "Samples the current key value" );
			this.buttonSampleValue.UseVisualStyleBackColor = true;
			this.buttonSampleValue.Visible = false;
			this.buttonSampleValue.Click += new System.EventHandler( this.buttonSampleValue_Click );
			// 
			// buttonColorPicker
			// 
			this.buttonColorPicker.BackgroundImage = global::SequencorEditor.Properties.Resources.Gradient;
			this.buttonColorPicker.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.buttonColorPicker.Location = new System.Drawing.Point( 78, 63 );
			this.buttonColorPicker.Name = "buttonColorPicker";
			this.buttonColorPicker.Size = new System.Drawing.Size( 22, 22 );
			this.buttonColorPicker.TabIndex = 4;
			this.buttonColorPicker.UseVisualStyleBackColor = true;
			this.buttonColorPicker.Visible = false;
			this.buttonColorPicker.Click += new System.EventHandler( this.buttonColorPicker_Click );
			// 
			// labelLabelKeyName
			// 
			this.labelLabelKeyName.AutoSize = true;
			this.labelLabelKeyName.Location = new System.Drawing.Point( 171, 10 );
			this.labelLabelKeyName.Name = "labelLabelKeyName";
			this.labelLabelKeyName.Size = new System.Drawing.Size( 35, 13 );
			this.labelLabelKeyName.TabIndex = 0;
			this.labelLabelKeyName.Text = "Name";
			this.labelLabelKeyName.Visible = false;
			// 
			// labelKeyName
			// 
			this.labelKeyName.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelKeyName.BackColor = System.Drawing.SystemColors.Info;
			this.labelKeyName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.labelKeyName.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.labelKeyName.Location = new System.Drawing.Point( 212, 8 );
			this.labelKeyName.Name = "labelKeyName";
			this.labelKeyName.Size = new System.Drawing.Size( 199, 17 );
			this.labelKeyName.TabIndex = 0;
			this.labelKeyName.Text = "<UNKNOWN>";
			this.labelKeyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.labelKeyName.Visible = false;
			// 
			// floatTrackbarControlTime
			// 
			this.floatTrackbarControlTime.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlTime.Location = new System.Drawing.Point( 81, 31 );
			this.floatTrackbarControlTime.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlTime.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlTime.Name = "floatTrackbarControlTime";
			this.floatTrackbarControlTime.RangeMin = 0F;
			this.floatTrackbarControlTime.Size = new System.Drawing.Size( 300, 20 );
			this.floatTrackbarControlTime.TabIndex = 4;
			this.toolTip1.SetToolTip( this.floatTrackbarControlTime, "Edits the time of the current key" );
			this.floatTrackbarControlTime.Value = 0F;
			// 
			// integerTrackbarControl
			// 
			this.integerTrackbarControl.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.integerTrackbarControl.Location = new System.Drawing.Point( 39, 1 );
			this.integerTrackbarControl.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControl.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControl.Name = "integerTrackbarControl";
			this.integerTrackbarControl.Size = new System.Drawing.Size( 223, 20 );
			this.integerTrackbarControl.TabIndex = 4;
			this.integerTrackbarControl.Value = 0;
			this.integerTrackbarControl.VisibleRangeMax = 1000;
			// 
			// floatTrackbarControlFloatW
			// 
			this.floatTrackbarControlFloatW.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlFloatW.Location = new System.Drawing.Point( 27, 63 );
			this.floatTrackbarControlFloatW.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlFloatW.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlFloatW.Name = "floatTrackbarControlFloatW";
			this.floatTrackbarControlFloatW.Size = new System.Drawing.Size( 231, 20 );
			this.floatTrackbarControlFloatW.TabIndex = 4;
			this.floatTrackbarControlFloatW.Value = 0F;
			// 
			// floatTrackbarControlFloatZ
			// 
			this.floatTrackbarControlFloatZ.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlFloatZ.Location = new System.Drawing.Point( 27, 42 );
			this.floatTrackbarControlFloatZ.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlFloatZ.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlFloatZ.Name = "floatTrackbarControlFloatZ";
			this.floatTrackbarControlFloatZ.Size = new System.Drawing.Size( 231, 20 );
			this.floatTrackbarControlFloatZ.TabIndex = 4;
			this.floatTrackbarControlFloatZ.Value = 0F;
			// 
			// floatTrackbarControlFloatY
			// 
			this.floatTrackbarControlFloatY.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlFloatY.Location = new System.Drawing.Point( 27, 21 );
			this.floatTrackbarControlFloatY.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlFloatY.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlFloatY.Name = "floatTrackbarControlFloatY";
			this.floatTrackbarControlFloatY.Size = new System.Drawing.Size( 231, 20 );
			this.floatTrackbarControlFloatY.TabIndex = 4;
			this.floatTrackbarControlFloatY.Value = 0F;
			// 
			// floatTrackbarControlFloatX
			// 
			this.floatTrackbarControlFloatX.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.floatTrackbarControlFloatX.Location = new System.Drawing.Point( 27, 0 );
			this.floatTrackbarControlFloatX.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlFloatX.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlFloatX.Name = "floatTrackbarControlFloatX";
			this.floatTrackbarControlFloatX.Size = new System.Drawing.Size( 231, 20 );
			this.floatTrackbarControlFloatX.TabIndex = 4;
			this.floatTrackbarControlFloatX.Value = 0F;
			// 
			// KeyEditorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 425, 223 );
			this.Controls.Add( this.buttonColorPicker );
			this.Controls.Add( this.buttonSampleValue );
			this.Controls.Add( this.buttonEditTime );
			this.Controls.Add( this.floatTrackbarControlTime );
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.groupBox1 );
			this.Controls.Add( this.labelKeyName );
			this.Controls.Add( this.labelKeyType );
			this.Controls.Add( this.label6 );
			this.Controls.Add( this.labelLabelKeyName );
			this.Controls.Add( this.label4 );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "KeyEditorForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Key Editor";
			this.groupBox1.ResumeLayout( false );
			this.panelInteger.ResumeLayout( false );
			this.panelInteger.PerformLayout();
			this.panelEvent.ResumeLayout( false );
			this.panelEvent.PerformLayout();
			this.panelFloat.ResumeLayout( false );
			this.panelFloat.PerformLayout();
			this.panelBool.ResumeLayout( false );
			this.panelBool.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TextBox textBoxEventGUID;
		private System.Windows.Forms.Label label1;
		private FloatTrackbarControl floatTrackbarControlTime;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label labelKeyType;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Button buttonEditTime;
		private System.Windows.Forms.Panel panelEvent;
		private System.Windows.Forms.Panel panelBool;
		private System.Windows.Forms.CheckBox checkBoxValueBool;
		private System.Windows.Forms.Button buttonSampleValue;
		private System.Windows.Forms.Panel panelFloat;
		private FloatTrackbarControl floatTrackbarControlFloatW;
		private System.Windows.Forms.Label labelW;
		private FloatTrackbarControl floatTrackbarControlFloatZ;
		private System.Windows.Forms.Label labelZ;
		private FloatTrackbarControl floatTrackbarControlFloatY;
		private System.Windows.Forms.Label labelY;
		private FloatTrackbarControl floatTrackbarControlFloatX;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Panel panelInteger;
		private System.Windows.Forms.Label label2;
		private IntegerTrackbarControl integerTrackbarControl;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button buttonColorPicker;
		private System.Windows.Forms.Label labelLabelKeyName;
		private System.Windows.Forms.Label labelKeyName;
	}
}