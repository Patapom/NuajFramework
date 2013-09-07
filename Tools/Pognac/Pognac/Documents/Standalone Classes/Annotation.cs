using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The annotation class represents various annotations that can be attached to any document.
	/// You can enter valuable informations as well as links to other documents, or attach some files.
	/// </summary>
	public class Annotation : IDisposable
	{
		#region FIELDS

		protected Database		m_Database = null;
		protected string		m_Text = "";				// The annotation text
		protected Attachment	m_Attachment = null;		// An optional attached file

		#endregion

		#region PROPERTIES

		public string			Text		{ get { return m_Text; } set { m_Text = value; if ( TextChanged != null ) TextChanged( this, EventArgs.Empty ); } }
		public Attachment		Attachment
		{
			get { return m_Attachment; }
			set
			{
				if ( value == m_Attachment )
					return;

				if ( m_Attachment != null )
				{
					m_Database.ReleaseAttachment( m_Attachment );
					m_Attachment.Disposed -= new EventHandler( Attachment_Disposed );
				}

				m_Attachment = value;

				if ( m_Attachment != null )
				{
					m_Database.ClaimAttachment( m_Attachment );
					m_Attachment.Disposed += new EventHandler( Attachment_Disposed );
				}

				// Notify
				if ( AttachmentChanged != null )
					AttachmentChanged( this, EventArgs.Empty );
			}
		}

		public event EventHandler	TextChanged;
		public event EventHandler	AttachmentChanged;

		#endregion

		#region METHODS

		public Annotation( Database _Database )
		{
			m_Database = _Database;
		}

		public Annotation( Database _Database, XmlElement _AnnotationElement )
		{
			m_Database = _Database;
			Load( _AnnotationElement );
		}

		public override string ToString()
		{
			return Text;
		}

		public void	Save( XmlElement _Parent )
		{
			XmlElement	AnnotationElement = _Parent.OwnerDocument.CreateElement( "Annotation" );
			_Parent.AppendChild( AnnotationElement );
			AnnotationElement.InnerText = m_Text;
			AnnotationElement.SetAttribute( "Attachment", m_Attachment != null ? m_Attachment.ID : "" );
		}

		public void	Load( XmlElement _AnnotationElement )
		{
			if ( _AnnotationElement == null )
				return;

			Text = _AnnotationElement.InnerText;
			Attachment = m_Database.FindAttachment( _AnnotationElement.GetAttribute( "Attachment" ) );
		}

		#region IDisposable Members

		public void Dispose()
		{
			Attachment = null;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected void Attachment_Disposed( object sender, EventArgs e )
		{
			Attachment = null;
		}

		#endregion
	}
}
