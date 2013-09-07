namespace VolumeRadiosityBuilder
{
	partial class VolumeRadiosityForm
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
			this.panelRight = new System.Windows.Forms.Panel();
			this.checkBoxIndirectOnly = new System.Windows.Forms.CheckBox();
			this.checkBoxAnimateLight = new System.Windows.Forms.CheckBox();
			this.floatTrackbarControlIndirectBoost = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlLightAnim = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlSkyIntensity = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlTimeSlicing = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlLightIntensity = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlDisplayMipBias = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.floatTrackbarControlGatheringMipBias = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall = new Nuaj.Cirrus.Utility.IntegerTrackbarControl();
			this.integerTrackbarControlBouncesCount = new Nuaj.Cirrus.Utility.IntegerTrackbarControl();
			this.buttonProfiler = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxInfos = new System.Windows.Forms.TextBox();
			this.panelOutput = new Nuaj.Cirrus.Utility.PanelOutput( this.components );
			this.panelRight.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelRight
			// 
			this.panelRight.Controls.Add( this.checkBoxIndirectOnly );
			this.panelRight.Controls.Add( this.checkBoxAnimateLight );
			this.panelRight.Controls.Add( this.floatTrackbarControlIndirectBoost );
			this.panelRight.Controls.Add( this.floatTrackbarControlLightAnim );
			this.panelRight.Controls.Add( this.floatTrackbarControlSkyIntensity );
			this.panelRight.Controls.Add( this.floatTrackbarControlTimeSlicing );
			this.panelRight.Controls.Add( this.floatTrackbarControlLightIntensity );
			this.panelRight.Controls.Add( this.floatTrackbarControlDisplayMipBias );
			this.panelRight.Controls.Add( this.floatTrackbarControlGatheringMipBias );
			this.panelRight.Controls.Add( this.integerTrackbarControlSlicesCountPerGatheringDrawCall );
			this.panelRight.Controls.Add( this.integerTrackbarControlBouncesCount );
			this.panelRight.Controls.Add( this.buttonProfiler );
			this.panelRight.Controls.Add( this.label8 );
			this.panelRight.Controls.Add( this.label7 );
			this.panelRight.Controls.Add( this.label6 );
			this.panelRight.Controls.Add( this.label4 );
			this.panelRight.Controls.Add( this.label3 );
			this.panelRight.Controls.Add( this.label5 );
			this.panelRight.Controls.Add( this.label9 );
			this.panelRight.Controls.Add( this.label2 );
			this.panelRight.Controls.Add( this.label1 );
			this.panelRight.Controls.Add( this.textBoxInfos );
			this.panelRight.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelRight.Location = new System.Drawing.Point( 832, 0 );
			this.panelRight.Name = "panelRight";
			this.panelRight.Size = new System.Drawing.Size( 200, 663 );
			this.panelRight.TabIndex = 1;
			// 
			// checkBoxIndirectOnly
			// 
			this.checkBoxIndirectOnly.AutoSize = true;
			this.checkBoxIndirectOnly.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold );
			this.checkBoxIndirectOnly.Location = new System.Drawing.Point( 9, 603 );
			this.checkBoxIndirectOnly.Name = "checkBoxIndirectOnly";
			this.checkBoxIndirectOnly.Size = new System.Drawing.Size( 182, 17 );
			this.checkBoxIndirectOnly.TabIndex = 5;
			this.checkBoxIndirectOnly.Text = "Show Indirect Lighting Only";
			this.checkBoxIndirectOnly.UseVisualStyleBackColor = true;
			// 
			// checkBoxAnimateLight
			// 
			this.checkBoxAnimateLight.AutoSize = true;
			this.checkBoxAnimateLight.Checked = true;
			this.checkBoxAnimateLight.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAnimateLight.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold );
			this.checkBoxAnimateLight.Location = new System.Drawing.Point( 9, 494 );
			this.checkBoxAnimateLight.Name = "checkBoxAnimateLight";
			this.checkBoxAnimateLight.Size = new System.Drawing.Size( 103, 17 );
			this.checkBoxAnimateLight.TabIndex = 5;
			this.checkBoxAnimateLight.Text = "Animate Light";
			this.checkBoxAnimateLight.UseVisualStyleBackColor = true;
			// 
			// floatTrackbarControlIndirectBoost
			// 
			this.floatTrackbarControlIndirectBoost.Location = new System.Drawing.Point( 6, 577 );
			this.floatTrackbarControlIndirectBoost.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlIndirectBoost.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlIndirectBoost.Name = "floatTrackbarControlIndirectBoost";
			this.floatTrackbarControlIndirectBoost.RangeMax = 10F;
			this.floatTrackbarControlIndirectBoost.RangeMin = 0F;
			this.floatTrackbarControlIndirectBoost.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlIndirectBoost.TabIndex = 4;
			this.floatTrackbarControlIndirectBoost.Value = 1F;
			this.floatTrackbarControlIndirectBoost.VisibleRangeMax = 4F;
			// 
			// floatTrackbarControlLightAnim
			// 
			this.floatTrackbarControlLightAnim.Location = new System.Drawing.Point( 6, 515 );
			this.floatTrackbarControlLightAnim.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlLightAnim.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlLightAnim.Name = "floatTrackbarControlLightAnim";
			this.floatTrackbarControlLightAnim.RangeMax = 1F;
			this.floatTrackbarControlLightAnim.RangeMin = 0F;
			this.floatTrackbarControlLightAnim.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlLightAnim.TabIndex = 4;
			this.floatTrackbarControlLightAnim.Value = 0F;
			this.floatTrackbarControlLightAnim.VisibleRangeMax = 1F;
			// 
			// floatTrackbarControlSkyIntensity
			// 
			this.floatTrackbarControlSkyIntensity.Location = new System.Drawing.Point( 6, 468 );
			this.floatTrackbarControlSkyIntensity.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlSkyIntensity.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlSkyIntensity.Name = "floatTrackbarControlSkyIntensity";
			this.floatTrackbarControlSkyIntensity.RangeMax = 10F;
			this.floatTrackbarControlSkyIntensity.RangeMin = 0F;
			this.floatTrackbarControlSkyIntensity.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlSkyIntensity.TabIndex = 4;
			this.floatTrackbarControlSkyIntensity.Value = 0.1F;
			this.floatTrackbarControlSkyIntensity.VisibleRangeMax = 0.2F;
			// 
			// floatTrackbarControlTimeSlicing
			// 
			this.floatTrackbarControlTimeSlicing.Location = new System.Drawing.Point( 6, 213 );
			this.floatTrackbarControlTimeSlicing.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlTimeSlicing.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlTimeSlicing.Name = "floatTrackbarControlTimeSlicing";
			this.floatTrackbarControlTimeSlicing.RangeMax = 10F;
			this.floatTrackbarControlTimeSlicing.RangeMin = 0F;
			this.floatTrackbarControlTimeSlicing.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlTimeSlicing.TabIndex = 4;
			this.floatTrackbarControlTimeSlicing.Value = 1F;
			this.floatTrackbarControlTimeSlicing.VisibleRangeMax = 4F;
			// 
			// floatTrackbarControlLightIntensity
			// 
			this.floatTrackbarControlLightIntensity.Location = new System.Drawing.Point( 6, 429 );
			this.floatTrackbarControlLightIntensity.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlLightIntensity.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlLightIntensity.Name = "floatTrackbarControlLightIntensity";
			this.floatTrackbarControlLightIntensity.RangeMax = 100F;
			this.floatTrackbarControlLightIntensity.RangeMin = 0F;
			this.floatTrackbarControlLightIntensity.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlLightIntensity.TabIndex = 4;
			this.floatTrackbarControlLightIntensity.Value = 7F;
			// 
			// floatTrackbarControlDisplayMipBias
			// 
			this.floatTrackbarControlDisplayMipBias.Location = new System.Drawing.Point( 6, 353 );
			this.floatTrackbarControlDisplayMipBias.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlDisplayMipBias.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlDisplayMipBias.Name = "floatTrackbarControlDisplayMipBias";
			this.floatTrackbarControlDisplayMipBias.RangeMax = 8F;
			this.floatTrackbarControlDisplayMipBias.RangeMin = 0F;
			this.floatTrackbarControlDisplayMipBias.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlDisplayMipBias.TabIndex = 4;
			this.floatTrackbarControlDisplayMipBias.Value = 1F;
			this.floatTrackbarControlDisplayMipBias.VisibleRangeMax = 8F;
			// 
			// floatTrackbarControlGatheringMipBias
			// 
			this.floatTrackbarControlGatheringMipBias.Location = new System.Drawing.Point( 6, 314 );
			this.floatTrackbarControlGatheringMipBias.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlGatheringMipBias.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlGatheringMipBias.Name = "floatTrackbarControlGatheringMipBias";
			this.floatTrackbarControlGatheringMipBias.RangeMax = 8F;
			this.floatTrackbarControlGatheringMipBias.RangeMin = 0F;
			this.floatTrackbarControlGatheringMipBias.Size = new System.Drawing.Size( 188, 20 );
			this.floatTrackbarControlGatheringMipBias.TabIndex = 4;
			this.floatTrackbarControlGatheringMipBias.Value = 0.5F;
			this.floatTrackbarControlGatheringMipBias.VisibleRangeMax = 8F;
			// 
			// integerTrackbarControlSlicesCountPerGatheringDrawCall
			// 
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.Location = new System.Drawing.Point( 6, 275 );
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.Name = "integerTrackbarControlSlicesCountPerGatheringDrawCall";
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.RangeMax = 100;
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.RangeMin = 1;
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.Size = new System.Drawing.Size( 188, 20 );
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.TabIndex = 3;
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.Value = 1;
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.VisibleRangeMax = 10;
			this.integerTrackbarControlSlicesCountPerGatheringDrawCall.VisibleRangeMin = 1;
			// 
			// integerTrackbarControlBouncesCount
			// 
			this.integerTrackbarControlBouncesCount.Location = new System.Drawing.Point( 6, 174 );
			this.integerTrackbarControlBouncesCount.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.integerTrackbarControlBouncesCount.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.integerTrackbarControlBouncesCount.Name = "integerTrackbarControlBouncesCount";
			this.integerTrackbarControlBouncesCount.RangeMax = 10;
			this.integerTrackbarControlBouncesCount.RangeMin = 0;
			this.integerTrackbarControlBouncesCount.Size = new System.Drawing.Size( 188, 20 );
			this.integerTrackbarControlBouncesCount.TabIndex = 3;
			this.integerTrackbarControlBouncesCount.Value = 3;
			this.integerTrackbarControlBouncesCount.VisibleRangeMax = 10;
			// 
			// buttonProfiler
			// 
			this.buttonProfiler.Location = new System.Drawing.Point( 65, 628 );
			this.buttonProfiler.Name = "buttonProfiler";
			this.buttonProfiler.Size = new System.Drawing.Size( 75, 23 );
			this.buttonProfiler.TabIndex = 2;
			this.buttonProfiler.Text = "Profiler";
			this.buttonProfiler.UseVisualStyleBackColor = true;
			this.buttonProfiler.Click += new System.EventHandler( this.buttonProfiler_Click );
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label8.Location = new System.Drawing.Point( 6, 561 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 135, 13 );
			this.label8.TabIndex = 1;
			this.label8.Text = "Indirect Lighting Boost";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label7.Location = new System.Drawing.Point( 6, 452 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 80, 13 );
			this.label7.TabIndex = 1;
			this.label7.Text = "Sky Intensity";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label6.Location = new System.Drawing.Point( 6, 413 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 87, 13 );
			this.label6.TabIndex = 1;
			this.label6.Text = "Light Intensity";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label4.Location = new System.Drawing.Point( 6, 337 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 100, 13 );
			this.label4.TabIndex = 1;
			this.label4.Text = "Display Mip Bias";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label3.Location = new System.Drawing.Point( 6, 298 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 114, 13 );
			this.label3.TabIndex = 1;
			this.label3.Text = "Gathering Mip Bias";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label5.Location = new System.Drawing.Point( 6, 259 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 159, 13 );
			this.label5.TabIndex = 1;
			this.label5.Text = "Slices Count For Gathering";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label9.Location = new System.Drawing.Point( 6, 197 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 103, 13 );
			this.label9.TabIndex = 1;
			this.label9.Text = "Time Slicing (ms)";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label2.Location = new System.Drawing.Point( 6, 158 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 93, 13 );
			this.label2.TabIndex = 1;
			this.label2.Text = "Bounces Count";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)) );
			this.label1.Location = new System.Drawing.Point( 6, 9 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 136, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "Radiosity Update Infos";
			// 
			// textBoxInfos
			// 
			this.textBoxInfos.Location = new System.Drawing.Point( 6, 25 );
			this.textBoxInfos.Multiline = true;
			this.textBoxInfos.Name = "textBoxInfos";
			this.textBoxInfos.Size = new System.Drawing.Size( 188, 117 );
			this.textBoxInfos.TabIndex = 0;
			// 
			// panelOutput
			// 
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point( 0, 0 );
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Size = new System.Drawing.Size( 832, 663 );
			this.panelOutput.TabIndex = 0;
			// 
			// VolumeRadiosityForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1032, 663 );
			this.Controls.Add( this.panelOutput );
			this.Controls.Add( this.panelRight );
			this.KeyPreview = true;
			this.Name = "VolumeRadiosityForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Form1";
			this.panelRight.ResumeLayout( false );
			this.panelRight.PerformLayout();
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.Panel panelRight;
		private System.Windows.Forms.Button buttonProfiler;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxInfos;
		private Nuaj.Cirrus.Utility.PanelOutput panelOutput;
		private Nuaj.Cirrus.Utility.IntegerTrackbarControl integerTrackbarControlBouncesCount;
		private System.Windows.Forms.Label label2;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlGatheringMipBias;
		private System.Windows.Forms.Label label3;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlDisplayMipBias;
		private System.Windows.Forms.Label label4;
		private Nuaj.Cirrus.Utility.IntegerTrackbarControl integerTrackbarControlSlicesCountPerGatheringDrawCall;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox checkBoxAnimateLight;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlLightIntensity;
		private System.Windows.Forms.Label label6;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlSkyIntensity;
		private System.Windows.Forms.Label label7;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlIndirectBoost;
		private System.Windows.Forms.Label label8;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlTimeSlicing;
		private System.Windows.Forms.Label label9;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlLightAnim;
		private System.Windows.Forms.CheckBox checkBoxIndirectOnly;

	}
}

