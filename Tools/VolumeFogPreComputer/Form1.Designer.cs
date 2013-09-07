namespace VolumeFogPreComputer
{
	partial class Form1
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
			this.outputPanel = new VolumeFogPreComputer.OutputPanel( this.components );
			this.integerTrackbarControlSliceSize = new VolumeFogPreComputer.IntegerTrackbarControl();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.integerTrackbarControlSlicesCount = new VolumeFogPreComputer.IntegerTrackbarControl();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label9 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.floatTrackbarControlSlabThickness = new VolumeFogPreComputer.FloatTrackbarControl();
			this.floatTrackbarControlSlabHeight = new VolumeFogPreComputer.FloatTrackbarControl();
			this.floatTrackbarControlSlabLength = new VolumeFogPreComputer.FloatTrackbarControl();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.floatTrackbarControlNoiseScale = new VolumeFogPreComputer.FloatTrackbarControl();
			this.floatTrackbarControlNoiseOffset = new VolumeFogPreComputer.FloatTrackbarControl();
			this.floatTrackbarControlNoiseSize = new VolumeFogPreComputer.FloatTrackbarControl();
			this.label6 = new System.Windows.Forms.Label();
			this.floatTrackbarControlInScatteringRatio = new VolumeFogPreComputer.FloatTrackbarControl();
			this.floatTrackbarControlMaxExtinction = new VolumeFogPreComputer.FloatTrackbarControl();
			this.integerTrackbarControlMarchingStepsCount = new VolumeFogPreComputer.IntegerTrackbarControl();
			this.checkBoxTube = new System.Windows.Forms.CheckBox();
			this.buttonCompute = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// outputPanel
			// 
			this.outputPanel.Location = new System.Drawing.Point( 12, 6 );
			this.outputPanel.Name = "outputPanel";
			this.outputPanel.Size = new System.Drawing.Size( 640, 229 );
			this.outputPanel.TabIndex = 0;
			this.outputPanel.Visible = false;
			// 
			// integerTrackbarControlSliceSize
			// 
			this.integerTrackbarControlSliceSize.Location = new System.Drawing.Point( 81, 19 );
			this.integerTrackbarControlSliceSize.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlSliceSize.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlSliceSize.Name = "integerTrackbarControlSliceSize";
			this.integerTrackbarControlSliceSize.RangeMax = 128;
			this.integerTrackbarControlSliceSize.RangeMin = 8;
			this.integerTrackbarControlSliceSize.Size = new System.Drawing.Size( 200, 20 );
			this.integerTrackbarControlSliceSize.TabIndex = 0;
			this.integerTrackbarControlSliceSize.Value = 32;
			this.integerTrackbarControlSliceSize.VisibleRangeMax = 64;
			this.integerTrackbarControlSliceSize.VisibleRangeMin = 8;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add( this.label2 );
			this.groupBox1.Controls.Add( this.label1 );
			this.groupBox1.Controls.Add( this.integerTrackbarControlSlicesCount );
			this.groupBox1.Controls.Add( this.integerTrackbarControlSliceSize );
			this.groupBox1.Location = new System.Drawing.Point( 12, 13 );
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size( 297, 87 );
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "3D Texture Parameters";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 6, 50 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 66, 13 );
			this.label2.TabIndex = 3;
			this.label2.Text = "Slices Count";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 6, 24 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 53, 13 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Slice Size";
			// 
			// integerTrackbarControlSlicesCount
			// 
			this.integerTrackbarControlSlicesCount.Location = new System.Drawing.Point( 81, 45 );
			this.integerTrackbarControlSlicesCount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlSlicesCount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlSlicesCount.Name = "integerTrackbarControlSlicesCount";
			this.integerTrackbarControlSlicesCount.RangeMax = 256;
			this.integerTrackbarControlSlicesCount.RangeMin = 8;
			this.integerTrackbarControlSlicesCount.Size = new System.Drawing.Size( 200, 20 );
			this.integerTrackbarControlSlicesCount.TabIndex = 1;
			this.integerTrackbarControlSlicesCount.Value = 128;
			this.integerTrackbarControlSlicesCount.VisibleRangeMax = 256;
			this.integerTrackbarControlSlicesCount.VisibleRangeMin = 8;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add( this.label9 );
			this.groupBox2.Controls.Add( this.label3 );
			this.groupBox2.Controls.Add( this.label4 );
			this.groupBox2.Controls.Add( this.floatTrackbarControlSlabThickness );
			this.groupBox2.Controls.Add( this.floatTrackbarControlSlabHeight );
			this.groupBox2.Controls.Add( this.floatTrackbarControlSlabLength );
			this.groupBox2.Location = new System.Drawing.Point( 12, 106 );
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size( 297, 109 );
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Slab Dimensions";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point( 6, 76 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 56, 13 );
			this.label9.TabIndex = 3;
			this.label9.Text = "Thickness";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 6, 50 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 38, 13 );
			this.label3.TabIndex = 3;
			this.label3.Text = "Height";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 6, 24 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 40, 13 );
			this.label4.TabIndex = 3;
			this.label4.Text = "Length";
			// 
			// floatTrackbarControlSlabThickness
			// 
			this.floatTrackbarControlSlabThickness.Location = new System.Drawing.Point( 81, 71 );
			this.floatTrackbarControlSlabThickness.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSlabThickness.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSlabThickness.Name = "floatTrackbarControlSlabThickness";
			this.floatTrackbarControlSlabThickness.RangeMax = 256F;
			this.floatTrackbarControlSlabThickness.RangeMin = 0F;
			this.floatTrackbarControlSlabThickness.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSlabThickness.TabIndex = 2;
			this.floatTrackbarControlSlabThickness.Value = 4F;
			this.floatTrackbarControlSlabThickness.VisibleRangeMax = 8F;
			// 
			// floatTrackbarControlSlabHeight
			// 
			this.floatTrackbarControlSlabHeight.Location = new System.Drawing.Point( 81, 45 );
			this.floatTrackbarControlSlabHeight.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSlabHeight.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSlabHeight.Name = "floatTrackbarControlSlabHeight";
			this.floatTrackbarControlSlabHeight.RangeMax = 256F;
			this.floatTrackbarControlSlabHeight.RangeMin = 0F;
			this.floatTrackbarControlSlabHeight.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSlabHeight.TabIndex = 1;
			this.floatTrackbarControlSlabHeight.Value = 8F;
			this.floatTrackbarControlSlabHeight.VisibleRangeMax = 16F;
			// 
			// floatTrackbarControlSlabLength
			// 
			this.floatTrackbarControlSlabLength.Location = new System.Drawing.Point( 81, 19 );
			this.floatTrackbarControlSlabLength.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSlabLength.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSlabLength.Name = "floatTrackbarControlSlabLength";
			this.floatTrackbarControlSlabLength.RangeMax = 256F;
			this.floatTrackbarControlSlabLength.RangeMin = 0F;
			this.floatTrackbarControlSlabLength.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlSlabLength.TabIndex = 0;
			this.floatTrackbarControlSlabLength.Value = 32F;
			this.floatTrackbarControlSlabLength.VisibleRangeMax = 64F;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add( this.label7 );
			this.groupBox3.Controls.Add( this.label11 );
			this.groupBox3.Controls.Add( this.label10 );
			this.groupBox3.Controls.Add( this.label8 );
			this.groupBox3.Controls.Add( this.label5 );
			this.groupBox3.Controls.Add( this.floatTrackbarControlNoiseScale );
			this.groupBox3.Controls.Add( this.floatTrackbarControlNoiseOffset );
			this.groupBox3.Controls.Add( this.floatTrackbarControlNoiseSize );
			this.groupBox3.Controls.Add( this.label6 );
			this.groupBox3.Controls.Add( this.floatTrackbarControlInScatteringRatio );
			this.groupBox3.Controls.Add( this.floatTrackbarControlMaxExtinction );
			this.groupBox3.Controls.Add( this.integerTrackbarControlMarchingStepsCount );
			this.groupBox3.Location = new System.Drawing.Point( 315, 13 );
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size( 337, 202 );
			this.groupBox3.TabIndex = 3;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Rendering Parameters";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point( 6, 50 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 106, 13 );
			this.label7.TabIndex = 3;
			this.label7.Text = "Max Extinction Value";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point( 6, 165 );
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size( 64, 13 );
			this.label11.TabIndex = 3;
			this.label11.Text = "Noise Scale";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point( 6, 142 );
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size( 65, 13 );
			this.label10.TabIndex = 3;
			this.label10.Text = "Noise Offset";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point( 6, 120 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 57, 13 );
			this.label8.TabIndex = 3;
			this.label8.Text = "Noise Size";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 6, 77 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 95, 13 );
			this.label5.TabIndex = 3;
			this.label5.Text = "In-Scattering Ratio";
			// 
			// floatTrackbarControlNoiseScale
			// 
			this.floatTrackbarControlNoiseScale.Location = new System.Drawing.Point( 124, 161 );
			this.floatTrackbarControlNoiseScale.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlNoiseScale.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlNoiseScale.Name = "floatTrackbarControlNoiseScale";
			this.floatTrackbarControlNoiseScale.RangeMax = 40F;
			this.floatTrackbarControlNoiseScale.RangeMin = -40F;
			this.floatTrackbarControlNoiseScale.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlNoiseScale.TabIndex = 4;
			this.floatTrackbarControlNoiseScale.Value = 0.7F;
			this.floatTrackbarControlNoiseScale.VisibleRangeMax = 2F;
			// 
			// floatTrackbarControlNoiseOffset
			// 
			this.floatTrackbarControlNoiseOffset.Location = new System.Drawing.Point( 124, 138 );
			this.floatTrackbarControlNoiseOffset.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlNoiseOffset.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlNoiseOffset.Name = "floatTrackbarControlNoiseOffset";
			this.floatTrackbarControlNoiseOffset.RangeMax = 40F;
			this.floatTrackbarControlNoiseOffset.RangeMin = -40F;
			this.floatTrackbarControlNoiseOffset.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlNoiseOffset.TabIndex = 4;
			this.floatTrackbarControlNoiseOffset.Value = -0.16F;
			this.floatTrackbarControlNoiseOffset.VisibleRangeMax = 1F;
			this.floatTrackbarControlNoiseOffset.VisibleRangeMin = -1F;
			// 
			// floatTrackbarControlNoiseSize
			// 
			this.floatTrackbarControlNoiseSize.Location = new System.Drawing.Point( 124, 116 );
			this.floatTrackbarControlNoiseSize.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlNoiseSize.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlNoiseSize.Name = "floatTrackbarControlNoiseSize";
			this.floatTrackbarControlNoiseSize.RangeMax = 10F;
			this.floatTrackbarControlNoiseSize.RangeMin = 0F;
			this.floatTrackbarControlNoiseSize.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlNoiseSize.TabIndex = 4;
			this.floatTrackbarControlNoiseSize.Value = 2F;
			this.floatTrackbarControlNoiseSize.VisibleRangeMax = 4F;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 6, 24 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 112, 13 );
			this.label6.TabIndex = 3;
			this.label6.Text = "Marching Steps Count";
			// 
			// floatTrackbarControlInScatteringRatio
			// 
			this.floatTrackbarControlInScatteringRatio.Location = new System.Drawing.Point( 124, 73 );
			this.floatTrackbarControlInScatteringRatio.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlInScatteringRatio.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlInScatteringRatio.Name = "floatTrackbarControlInScatteringRatio";
			this.floatTrackbarControlInScatteringRatio.RangeMax = 1F;
			this.floatTrackbarControlInScatteringRatio.RangeMin = 0F;
			this.floatTrackbarControlInScatteringRatio.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlInScatteringRatio.TabIndex = 3;
			this.floatTrackbarControlInScatteringRatio.Value = 0.22F;
			this.floatTrackbarControlInScatteringRatio.VisibleRangeMax = 1F;
			// 
			// floatTrackbarControlMaxExtinction
			// 
			this.floatTrackbarControlMaxExtinction.Location = new System.Drawing.Point( 124, 47 );
			this.floatTrackbarControlMaxExtinction.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlMaxExtinction.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlMaxExtinction.Name = "floatTrackbarControlMaxExtinction";
			this.floatTrackbarControlMaxExtinction.RangeMax = 10F;
			this.floatTrackbarControlMaxExtinction.RangeMin = 0F;
			this.floatTrackbarControlMaxExtinction.Size = new System.Drawing.Size( 200, 20 );
			this.floatTrackbarControlMaxExtinction.TabIndex = 2;
			this.floatTrackbarControlMaxExtinction.Value = 0.6F;
			this.floatTrackbarControlMaxExtinction.VisibleRangeMax = 1F;
			// 
			// integerTrackbarControlMarchingStepsCount
			// 
			this.integerTrackbarControlMarchingStepsCount.Location = new System.Drawing.Point( 124, 19 );
			this.integerTrackbarControlMarchingStepsCount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlMarchingStepsCount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlMarchingStepsCount.Name = "integerTrackbarControlMarchingStepsCount";
			this.integerTrackbarControlMarchingStepsCount.RangeMax = 100;
			this.integerTrackbarControlMarchingStepsCount.RangeMin = 1;
			this.integerTrackbarControlMarchingStepsCount.Size = new System.Drawing.Size( 200, 20 );
			this.integerTrackbarControlMarchingStepsCount.TabIndex = 1;
			this.integerTrackbarControlMarchingStepsCount.Value = 50;
			this.integerTrackbarControlMarchingStepsCount.VisibleRangeMin = 1;
			// 
			// checkBoxTube
			// 
			this.checkBoxTube.AutoSize = true;
			this.checkBoxTube.Location = new System.Drawing.Point( 439, 236 );
			this.checkBoxTube.Name = "checkBoxTube";
			this.checkBoxTube.Size = new System.Drawing.Size( 51, 17 );
			this.checkBoxTube.TabIndex = 5;
			this.checkBoxTube.Text = "Tube";
			this.checkBoxTube.UseVisualStyleBackColor = true;
			// 
			// buttonCompute
			// 
			this.buttonCompute.Location = new System.Drawing.Point( 544, 227 );
			this.buttonCompute.Name = "buttonCompute";
			this.buttonCompute.Size = new System.Drawing.Size( 108, 32 );
			this.buttonCompute.TabIndex = 0;
			this.buttonCompute.Text = "Compute";
			this.buttonCompute.UseVisualStyleBackColor = true;
			this.buttonCompute.Click += new System.EventHandler( this.buttonCompute_Click );
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 664, 271 );
			this.Controls.Add( this.checkBoxTube );
			this.Controls.Add( this.groupBox3 );
			this.Controls.Add( this.buttonCompute );
			this.Controls.Add( this.groupBox2 );
			this.Controls.Add( this.groupBox1 );
			this.Controls.Add( this.outputPanel );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Volume Fog Pre-Computer";
			this.groupBox1.ResumeLayout( false );
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout( false );
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout( false );
			this.groupBox3.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private OutputPanel outputPanel;
		private IntegerTrackbarControl integerTrackbarControlSliceSize;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private IntegerTrackbarControl integerTrackbarControlSlicesCount;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label4;
		private FloatTrackbarControl floatTrackbarControlSlabLength;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private FloatTrackbarControl floatTrackbarControlMaxExtinction;
		private IntegerTrackbarControl integerTrackbarControlMarchingStepsCount;
		private FloatTrackbarControl floatTrackbarControlInScatteringRatio;
		private System.Windows.Forms.Label label8;
		private FloatTrackbarControl floatTrackbarControlNoiseSize;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label3;
		private FloatTrackbarControl floatTrackbarControlSlabThickness;
		private FloatTrackbarControl floatTrackbarControlSlabHeight;
		private System.Windows.Forms.Button buttonCompute;
		private System.Windows.Forms.CheckBox checkBoxTube;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;
		private FloatTrackbarControl floatTrackbarControlNoiseScale;
		private FloatTrackbarControl floatTrackbarControlNoiseOffset;
	}
}

