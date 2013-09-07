using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nuaj
{
	public class NException : Exception
	{
		#region FIELDS

		protected Component		m_Thrower = null;

		#endregion

		#region PROPERTIES

		public Component	Thrower	{ get { return m_Thrower; } }

		#endregion

		#region METHODS

		public	NException( Component _Thrower, string _Message ) : base( _Message )
		{
			m_Thrower = _Thrower;
		}

		public	NException( Component _Thrower, string _Message, System.Exception _InnerException ) : base( _Message, _InnerException )
		{
			m_Thrower = _Thrower;
		}

		public override string ToString()
		{
			string	Text = m_Thrower != null ? ("Component \"" + m_Thrower.Name + "\" :\r\n") : "";
			Text += "  > " + Message + "\r\n";

			Exception	e = InnerException;
			while ( e != null )
			{
				Text += "  > " + e.Message + "\r\n";
				e = e.InnerException;
			}

			return Text;
		}

		#endregion
	}
}
