using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac.Documents
{
	/// <summary>
	/// The institution class represents an institution or entity or the sender of a document
	/// For example : Impôts, EDF, GDF, Véolia, Pôle Emploi, etc.
	/// </summary>
	public class Institution : BaseDocument
	{
		#region FIELDS

		protected int			m_ID = -1;					// The instritution ID
		protected string		m_Name = "";				// Name of the institution

		#endregion

		#region PROPERTIES

		public int				ID		{ get { return m_ID; } }
		public string			Name	{ get { return m_Name; } set { m_Name = value; if ( NameChanged != null ) NameChanged( this, EventArgs.Empty ); } }

		public event EventHandler	NameChanged;

		#endregion

		#region METHODS

		public Institution( Database _Database, int _ID, string _Name ) : base( _Database )
		{
			m_ID = _ID;
			m_Name = _Name;
		}

		public Institution( Database _Database, XmlElement _InstitutionElement ) : base( _Database, _InstitutionElement )
		{
		}

		public override string ToString()
		{
			return m_Name;
		}

		public override void	Save( XmlElement _Parent )
		{
			XmlElement	InstitutionElement = _Parent.OwnerDocument.CreateElement( "Institution" );
			_Parent.AppendChild( InstitutionElement );
			InstitutionElement.SetAttribute( "Name", m_Name );
			InstitutionElement.SetAttribute( "ID", m_ID.ToString() );

			// Save annotations
			base.Save( InstitutionElement );
		}

		public override void Load( XmlElement _InstitutionElement )
		{
			Dispose();

			base.Load( _InstitutionElement );

			m_Name = _InstitutionElement.GetAttribute( "Name" );
			if ( !int.TryParse( _InstitutionElement.GetAttribute( "ID" ), out m_ID ) )
				throw new Exception( "Failed to parse ID for institution \"" + this + "\" !" );
		}

		#endregion
	}
}
