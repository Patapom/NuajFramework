using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pognac
{
	public partial class AnnotationControl : UserControl
	{
		#region FIELDS

		protected Documents.Annotation	m_Annotation = null;

		#endregion

		#region PROPERTIES

		public Documents.Annotation		Annotation
		{
			get { return m_Annotation; }
			set
			{
				if ( value == m_Annotation )
					return;

				if ( m_Annotation != null )
				{
					m_Annotation.TextChanged -= new EventHandler( Annotation_TextChanged );
					m_Annotation.AttachmentChanged -= new EventHandler( Annotation_AttachmentChanged );
				}

				m_Annotation = value;

				if ( m_Annotation != null )
				{
					m_Annotation.TextChanged += new EventHandler( Annotation_TextChanged );
					m_Annotation.AttachmentChanged += new EventHandler( Annotation_AttachmentChanged );
				}

				// Update GUI
				Enabled = m_Annotation != null;
				Annotation_TextChanged( m_Annotation, EventArgs.Empty );
				Annotation_AttachmentChanged( m_Annotation, EventArgs.Empty );
			}
		}

		#endregion

		#region METHODS

		public AnnotationControl()
		{
			InitializeComponent();
		}

		#endregion

		#region EVENT HANDLERS

		void Annotation_AttachmentChanged( object sender, EventArgs e )
		{
			labelAttachment.Text = m_Annotation != null && m_Annotation.Attachment != null ? m_Annotation.Attachment.ToString() : "";
		}

		void Annotation_TextChanged( object sender, EventArgs e )
		{
			richTextBox.Text = m_Annotation != null ? m_Annotation.Text : "";
		}

		private void richTextBox_Leave( object sender, EventArgs e )
		{
			if ( m_Annotation != null )
				m_Annotation.Text = richTextBox.Text;
		}

		#endregion
	}
}
