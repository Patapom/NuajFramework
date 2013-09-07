using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The base document class
	/// </summary>
	public abstract class BaseDocument : IDisposable
	{
		#region FIELDS

		protected Database		m_Database = null;
		protected Annotation	m_Annotation = null;		// Document annotation

		#endregion

		#region PROPERTIES

		public Database				Database	{ get { return m_Database; } }
		public Annotation			Annotation	{ get { return m_Annotation; } }

		public event EventHandler	Disposed;

		#endregion

		#region METHODS

		public BaseDocument( Database _Database )
		{
			m_Database = _Database;
			m_Annotation = new Annotation( _Database );
		}

		public BaseDocument( Database _Database, XmlElement _Element )
		{
			m_Database = _Database;
			Load( _Element );
		}

		/// <summary>
		/// Saves the document to a parent XML element
		/// </summary>
		/// <param name="_ParentElement"></param>
		public virtual void	Save( XmlElement _ParentElement )
		{
			m_Annotation.Save( _ParentElement );
		}

		/// <summary>
		/// Loads the document from its XML element
		/// </summary>
		/// <param name="_DocumentElement"></param>
		public virtual void Load( XmlElement _DocumentElement )
		{
			m_Annotation = new Annotation( m_Database, _DocumentElement["Annotation"] );
		}

		#region IDisposable Members

		public virtual void Dispose()
		{
			if ( m_Annotation != null )
				m_Annotation.Dispose();

			// Notify
			if ( Disposed != null )
				Disposed( this, EventArgs.Empty );
		}

		#endregion

		#endregion
	}
}
