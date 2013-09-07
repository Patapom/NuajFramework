using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace Pognac.Documents
{
	/// <summary>
	/// The AttachmentPDF class encapsulates a PDF document thumbnail and is used as a graphic placeholder.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay( "{FileName}" )]
	public class AttachmentPDF : Attachment
	{
		#region FIELDS

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Simply return a PDF logo placeholder
		/// </summary>
		public override Bitmap	Bitmap
		{
			get { return Properties.Resources.PDF; }
		}

		#endregion

		#region METHODS

		public AttachmentPDF( Documents.Database _Database, FileInfo _FileName ) : base( _Database, _FileName )
		{
		}

		#endregion
	}
}
