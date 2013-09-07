using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The page class represents a single page of a document
	/// </summary>
	public class Page : BaseDocument
	{
		#region NESTED TYPES

		public enum TYPE
		{
			FACTURE,			// Your typical bill
			FRAIS,				// de Notaire, d'huissier, etc.
			AVIS,				// Imposition, échéance paiments, etc.
			BULLETIN,			// Paie
			DEVIS,				// Devis, accord de location, etc.
			INFORMATION,		// Standard information like "Hey, here's is your new EDF compteur !"
			MISC,				// None of the above categories
		}

		#endregion

		#region FIELDS

		protected Document		m_Owner = null;			// Our owner document
		protected TYPE			m_Type = TYPE.MISC;		// The page type
		protected bool			m_bRecto = true;		// Recto/Verso
		protected string		m_CodeName = "";		// Code name (usually, the name of the attached file without extension)
		protected Attachment	m_Attachment = null;	// The attached file

		#endregion

		#region PROPERTIES

		public Document			Owner			{ get { return m_Owner; } }
		public TYPE				Type			{ get { return m_Type; } set { m_Type = value; if ( TypeChanged != null ) TypeChanged( this, EventArgs.Empty ); } }
		public bool				Recto			{ get { return m_bRecto; } set { m_bRecto = value; if ( RectoChanged != null ) RectoChanged( this, EventArgs.Empty ); } }
		public string			CodeName		{ get { return m_CodeName; } }
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

		public event EventHandler	TypeChanged;
		public event EventHandler	RectoChanged;
		public event EventHandler	AttachmentChanged;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a new page
		/// </summary>
		/// <param name="_Type"></param>
		/// <param name="_CodeName"></param>
		/// <param name="_Attachment"></param>
		internal	Page( Database _Database, Document _Owner, TYPE _Type, string _CodeName, Attachment _Attachment ) : base( _Database )
		{
			m_Owner = _Owner;
			Type = _Type;
			m_CodeName = _CodeName;
			Attachment = _Attachment;
		}

		internal	Page( Database _Database, Document _Owner, XmlElement _PageElement ) : base( _Database, _PageElement )
		{
			m_Owner = _Owner;
		}

		public override string ToString()
		{
			return m_CodeName + " (" + m_Type + ")";
		}

		/// <summary>
		/// Performs a backup of the document
		/// </summary>
		/// <returns></returns>
		public object	Backup()
		{
			XmlDocument	Doc = new XmlDocument();
			XmlElement	Root = Doc.CreateElement( "ROOT" );
			Doc.AppendChild( Root );
			Save( Root );

			return Doc;
		}

		/// <summary>
		/// Restores the document from an earlier backup
		/// </summary>
		/// <param name="_Backup"></param>
		public void		Restore( object _Backup )
		{
			if ( !HasChanged( _Backup ) )
				return;

			XmlDocument	Doc = _Backup as XmlDocument;
			Load( Doc["ROOT"]["Page"] );
		}

		/// <summary>
		/// Tells if the document has changed based on a backup stored earlier
		/// </summary>
		/// <param name="XmlDocument"></param>
		/// <returns></returns>
		public bool		HasChanged( object _Backup )
		{
			XmlDocument	Doc = _Backup as XmlDocument;
			if ( Doc == null )
				throw new Exception( "Invalid backup !" );

			XmlDocument	CurrentStateDoc = Backup() as XmlDocument;

			return Doc.InnerXml != CurrentStateDoc.InnerXml;
		}

		public override void	Save( XmlElement _Parent )
		{
			XmlElement	PageElement = _Parent.OwnerDocument.CreateElement( "Page" );
			_Parent.AppendChild( PageElement );

			PageElement.SetAttribute( "Type", m_Type.ToString() );
			PageElement.SetAttribute( "Recto", m_bRecto.ToString() );
			PageElement.SetAttribute( "CodeName", m_CodeName );
			PageElement.SetAttribute( "Attachment", m_Attachment != null ? m_Attachment.ID : "" );

			// Save annotation
			base.Save( PageElement );
		}

		public override void  Load( XmlElement _PageElement )
		{
			Dispose();

 			base.Load( _PageElement );

			Enum.TryParse<TYPE>( _PageElement.GetAttribute( "Type" ), out m_Type );
			bool.TryParse( _PageElement.GetAttribute( "Recto" ), out m_bRecto );
			m_CodeName = _PageElement.GetAttribute( "CodeName" );
			Attachment = m_Database.FindAttachment( _PageElement.GetAttribute( "Attachment" ) );
		}

		public override void Dispose()
		{
			Attachment = null;
			base.Dispose();
		}

		#endregion

		#region EVENT HANDLERS

		protected void Attachment_Disposed( object sender, EventArgs e )
		{
			Attachment = null;
		}

		#endregion
	}
}
