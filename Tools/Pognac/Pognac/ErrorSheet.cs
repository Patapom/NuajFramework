using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Pognac
{
	public class ErrorSheetException : Exception
	{
		public ErrorSheet	m_ErrorSheet = null;

		public ErrorSheetException( string _Message, ErrorSheet _ErrorSheet ) : base( _Message )
		{
			m_ErrorSheet = _ErrorSheet;
		}

		public void		Display()
		{
			m_ErrorSheet.DisplayErrors( Message );
		}
	}

	/// <summary>
	/// The error sheet class allows to report errors in bulk.
	/// </summary>
	public class ErrorSheet
	{
		#region NESTED TYPES

		public enum DISPLAY_TYPE
		{
			SIMPLE,			// Simple display, line by line
			TIME_STAMPS		// Displays time stamps for each error line
		}

		public abstract class	Error
		{
			#region FIELDS

			protected DateTime	m_Time = DateTime.Now;

			#endregion

			#region PROPERTIES

			public DateTime		Time	{ get { return m_Time; } }

			#endregion

			#region METHODS

			#endregion
		}

		public class	ErrorString : Error
		{
			#region FIELDS

			protected string	m_Error = "";

			#endregion

			#region PROPERTIES

			public string	Error	{ get { return m_Error; } }

			#endregion

			#region METHODS

			public ErrorString( string _Error )
			{
				m_Error = _Error;
			}

			public override string ToString()
			{
				return m_Error;
			}

			#endregion
		}

		public class	ErrorException : Error
		{
			#region FIELDS

			protected Exception	m_Error = null;

			#endregion

			#region PROPERTIES

			public Exception	Error	{ get { return m_Error; } }

			#endregion

			#region METHODS

			public ErrorException( Exception _Error )
			{
				m_Error = _Error;
			}

			public override string ToString()
			{
				return m_Error.Message;
			}

			#endregion
		}

		public class	ErrorSheetChild : Error
		{
			#region FIELDS

			protected ErrorSheet	m_Error = null;

			#endregion

			#region PROPERTIES

			public ErrorSheet	Error	{ get { return m_Error; } }

			#endregion

			#region METHODS

			public ErrorSheetChild( ErrorSheet _Error )
			{
				m_Error = _Error;
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected List<Error>	m_Errors = new List<Error>();

		#endregion

		#region PROPERTIES

		public Error[]			Errors		{ get { return m_Errors.ToArray(); } }

		public bool				HasErrors	{ get { return m_Errors.Count > 0; } }

		#endregion

		#region METHODS

		public ErrorSheet()
		{
		}

		public void	AddError( string _Error )
		{
			m_Errors.Add( new ErrorString( _Error ) );
		}

		public void	AddError( Exception _Error )
		{
			m_Errors.Add( new ErrorException( _Error ) );
		}

		public void	AddError( ErrorSheet _Error )
		{
			m_Errors.Add( new ErrorSheetChild( _Error ) );
		}

		public void	DisplayErrors( string _Title )
		{
			DisplayErrors( _Title, DISPLAY_TYPE.SIMPLE );
		}

		protected void	DisplayErrors( string _Title, DISPLAY_TYPE _DisplayType )
		{
			StringBuilder	SB = new StringBuilder();
			ExpandErrorSheet( this, SB, _DisplayType );

			PognacForm.MessageBox( _Title + "\r\n\r\n" + SB.ToString(), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error );
		}

		protected void	ExpandErrorSheet( ErrorSheet _Sheet, StringBuilder _String, DISPLAY_TYPE _DisplayType )
		{
			foreach ( ErrorSheet.Error Error in _Sheet.Errors )
			{
				if ( Error is ErrorSheet.ErrorString )
					_String.Append( (Error as ErrorSheet.ErrorString).Error + "\r\n" );
				else if ( Error is ErrorSheet.ErrorException )
					_String.Append( (Error as ErrorSheet.ErrorException).Error.Message + "\r\n" );
				else if ( Error is ErrorSheet.ErrorSheetChild )
					ExpandErrorSheet( (Error as ErrorSheet.ErrorSheetChild).Error, _String, _DisplayType );
			}
		}

		#endregion
	}
}
