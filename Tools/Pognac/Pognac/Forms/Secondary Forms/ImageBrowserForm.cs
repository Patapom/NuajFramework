using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pognac
{
	public partial class ImageBrowserForm : Form
	{
		#region CONSTANTS

		protected const int		BORDER_OFFSET = 10;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the set of images to browse from
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Documents.Attachment[]	Attachments
		{
			get { return zoomableImagesViewer.Attachments; }
			set { zoomableImagesViewer.Attachments = value; }
		}

		/// <summary>
		/// Gets or sets the selected image
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Documents.Attachment		Selection
		{
			get { return zoomableImagesViewer.SingleSelection; }
			set { zoomableImagesViewer.SingleSelection = value; }
		}

		#endregion

		#region METHODS

		public ImageBrowserForm()
		{
			InitializeComponent();
		}

		public void		ZoomOnSelection()
		{
			zoomableImagesViewer.ZoomOnSelection();
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			zoomableImagesViewer.Focus();
		}

		protected override void OnPreviewKeyDown( PreviewKeyDownEventArgs e )
		{
			base.OnPreviewKeyDown( e );

			if ( e.KeyCode == Keys.Escape )
			{	// Close...
				DialogResult = DialogResult.Cancel;
				Close();
			}
		}

		protected override bool ProcessKeyPreview( ref Message m )
		{
			if ( m.Msg == 0x100 && m.WParam.ToInt32() == (int) Keys.Escape )
			{	// Close...
				DialogResult = DialogResult.Cancel;
				Close();
			}

			return base.ProcessKeyPreview( ref m );
		}

		#endregion

		#region EVENT HANDLERS

		private void zoomableImagesViewer_SelectionChanged( object sender, EventArgs e )
		{
			toolStripButtonPrint.Enabled = toolStripButtonDefineCrop.Enabled = Selection != null;
		}

		private void toolStripButtonPrint_Click( object sender, EventArgs e )
		{
			PognacForm.MessageBox( "TODO" );
		}

		private void toolStripButtonDefineCrop_Click( object sender, EventArgs e )
		{
			PognacForm.MessageBox( "TODO" );
		}

		private void toolStripButtonZoomOut_Click( object sender, EventArgs e )
		{
			ZoomOnSelection();
		}

		#endregion
	}
}
