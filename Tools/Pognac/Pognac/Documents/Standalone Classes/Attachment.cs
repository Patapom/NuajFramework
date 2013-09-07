using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace Pognac.Documents
{
	/// <summary>
	/// The Attachment class encapsulates an attached file name and loads the file on demand.
	/// An attachment is typically any file lying in the database's root directory that the
	///  database is capable of attaching to one of its elements (document, page, institution, etc.)
	/// An attachment can only be assigned to a single element :
	///  _ By default, an un-assigned attachment is a file that is not attached to any database element and you can find it through the "Database.UnAssignedAttachments" property
	///  _ Otherwise, the attachment is assigned to a database element and is not available for use anymore until the element releases the attachment (i.e. gets disposed or assigned another attachement)
	/// </summary>
	[System.Diagnostics.DebuggerDisplay( "{FileName}" )]
	public abstract class Attachment : IDisposable
	{
		#region FIELDS

		protected Database		m_Database = null;
		protected FileInfo		m_FileName = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the attachment's unique ID
		/// </summary>
		public virtual string		ID	{ get { return m_FileName != null ? m_Database.GetRelativeFileName( m_FileName ) : ""; } }

		/// <summary>
		/// Gets the file name
		/// </summary>
		public FileInfo				FileName
		{
			get { return m_FileName; }
		}

		/// <summary>
		/// Gets the bitmap representation associated to the file
		/// </summary>
		public abstract Bitmap		Bitmap
		{
			get;
		}

		/// <summary>
		/// Gets the crop rectangle to apply to the bitmap
		/// </summary>
		public virtual Rectangle	Crop
		{
			get { return new Rectangle( 0, 0, Bitmap.Width, Bitmap.Height ); }
			set {}
		}

		/// <summary>
		/// Occurs when the attachment gets disposed of
		/// </summary>
		public event EventHandler	Disposed;

		#endregion

		#region METHODS

		protected Attachment( Database _Database, FileInfo _FileName )
		{
			if ( _FileName == null )
				throw new Exception( "Invalid file !" );

			m_Database = _Database;
			m_FileName = _FileName;
		}

		public override bool Equals( object obj )
		{
			Attachment	Other = obj as Attachment;
			return Other != null && Other.m_FileName == m_FileName;
		}

		public override int GetHashCode()
		{
			return m_FileName.FullName.ToLower().GetHashCode();
		}

		#region IDisposable Members

		public virtual void Dispose()
		{
			// Notify
			if ( Disposed != null )
				Disposed( this, EventArgs.Empty );
		}

		#endregion

		#endregion
	}
}
