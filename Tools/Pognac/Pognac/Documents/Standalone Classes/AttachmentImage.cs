using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace Pognac.Documents
{
	/// <summary>
	/// The AttachmentImage class encapsulates an image document.
	/// It also handles an optional crop rectangle.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay( "{FileName}" )]
	public class AttachmentImage : Attachment
	{
		#region FIELDS

		protected Bitmap		m_Image = null;
		protected Rectangle		m_Crop = Rectangle.Empty;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the bitmap associated to the file
		/// </summary>
		public override Bitmap	Bitmap
		{
			get
			{
				if ( m_Image != null )
					return m_Image;

				if ( m_FileName == null )
					return null;

				try
				{
					m_Image = Bitmap.FromFile( m_FileName.FullName ) as Bitmap;
				}
				catch ( Exception )
				{
					m_Image = Properties.Resources.ImageError;
				}
			
				return m_Image;
			}
		}

		/// <summary>
		/// Gets or sets the optional crop rectangle to isolate a part of the image (use Rectangle.Empty for the whole image)
		/// </summary>
		public override Rectangle		Crop
		{
			get { return !m_Crop.IsEmpty || Bitmap == null ? m_Crop : new Rectangle( 0, 0, Bitmap.Width, Bitmap.Height ); }
			set { m_Crop = value; }
		}

		#endregion

		#region METHODS

		public AttachmentImage( Documents.Database _Database, FileInfo _FileName ) : base( _Database, _FileName )
		{
		}

		#region IDisposable Members

		public override void Dispose()
		{
			base.Dispose();

			if ( m_Image == null || m_Image == Properties.Resources.ImageError )
				return;

			m_Image.Dispose();
			m_Image = null;
		}

		#endregion

		#endregion
	}
}
