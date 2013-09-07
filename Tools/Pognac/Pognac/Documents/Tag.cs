using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The tag class represents a string tag attached to a document that will help to search the database.
	/// </summary>
	public class Tag
	{
		#region FIELDS

		protected string		m_Name = "";				// The name of the tag
		protected int			m_ID = -1;					// The tag ID

		#endregion

		#region PROPERTIES

		public string			Name	{ get { return m_Name; } set { m_Name = value; if ( NameChanged != null ) NameChanged( this, EventArgs.Empty ); } }
		public int				ID		{ get { return m_ID; } }

		public event EventHandler	NameChanged;

		#endregion

		#region METHODS

		public Tag( int _ID, string _Name )
		{
			m_Name = _Name;
			m_ID = _ID;
		}

		public Tag( XmlElement _TagElement )
		{
			Load( _TagElement );
		}

		public override string ToString()
		{
			return m_Name;
		}

		public void	Save( XmlElement _Parent )
		{
			XmlElement	TagElement = _Parent.OwnerDocument.CreateElement( "Tag" );
			_Parent.AppendChild( TagElement );
			TagElement.SetAttribute( "Name", m_Name );
			TagElement.SetAttribute( "ID", m_ID.ToString() );
		}

		public void	Load( XmlElement _TagElement )
		{
			m_Name = _TagElement.GetAttribute( "Name" );
			if ( !int.TryParse( _TagElement.GetAttribute( "ID" ), out m_ID ) )
				throw new Exception( "Failed to parse ID for tag \"" + this + "\" !" );
		}

		#endregion
	}
}
