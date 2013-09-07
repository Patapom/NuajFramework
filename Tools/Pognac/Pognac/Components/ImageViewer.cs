using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace Pognac
{
	public partial class ImageViewer : Panel
	{
		#region FIELDS

		protected Bitmap		m_Image = null;
		protected Rectangle		m_Crop = Rectangle.Empty;
		protected RectangleF	m_ViewRectangle;

		// Manipulation
		protected MouseButtons	m_Buttons = MouseButtons.None;
		protected Point			m_ButtonDownPosition;
		protected RectangleF	m_ButtonDownViewRectangle;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the image to display
		/// </summary>
		public Bitmap	Image
		{
			get { return m_Image; }
			set
			{
				if ( value == m_Image )
					return;

				m_Image = value;
				if ( m_Image != null )
				{	// Create the centered view rectangle
					float	fZoomFactorY = Math.Min( 1.0f, (float) Height / m_Image.Height );
					float	fZoomFactorX = Math.Min( 1.0f, (float) Width / m_Image.Width );

					// Select the zoom factor that keeps the image within the screen's borders
					float	fZoomFactor = m_Image.Width * fZoomFactorY > Width ? fZoomFactorX : fZoomFactorY;

					float	fNewWidth = m_Image.Width * fZoomFactor;
					float	fNewHeight = m_Image.Height * fZoomFactor;
					float	X = 0.5f * (Width - fNewWidth);
					float	Y = 0.5f * (Height - fNewHeight);

					m_ViewRectangle = new RectangleF( X, Y, fNewWidth, fNewHeight );
				}

				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets the crop rectangle to crop the image with
		/// </summary>
		public Rectangle	Crop
		{
			get { return m_Crop; }
			set
			{
				m_Crop = value;
				Invalidate();
			}
		}

		#endregion

		#region METHODS

		public ImageViewer()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			InitializeComponent();
		}

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );
			m_Buttons |= e.Button;
			m_ButtonDownPosition = e.Location;
			m_ButtonDownViewRectangle = m_ViewRectangle;
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );

			if ( m_Buttons != MouseButtons.Left )
				return;

			// Pan
			Point	Delta = new Point( e.Location.X - m_ButtonDownPosition.X, e.Location.Y - m_ButtonDownPosition.Y );
			m_ViewRectangle = new RectangleF( m_ButtonDownViewRectangle.X + Delta.X, m_ButtonDownViewRectangle.Y + Delta.Y, m_ButtonDownViewRectangle.Width, m_ButtonDownViewRectangle.Height );
			Refresh();
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );
			m_Buttons &= ~e.Button;
		}

		protected override void OnMouseWheel( MouseEventArgs e )
		{
			base.OnMouseWheel( e );

			float	fZoomFactor = e.Delta < 0 ? 1.1f : 1.0f / 1.1f;

			// Zoom and keep current position fixed
			float	fNewWidth = m_ViewRectangle.Width * fZoomFactor;
			float	fNewHeight = m_ViewRectangle.Height * fZoomFactor;
			float	fNewX = fZoomFactor * (e.X - m_ViewRectangle.X);
			float	fNewY = fZoomFactor * (e.Y - m_ViewRectangle.Y);

			m_ViewRectangle = new RectangleF( fNewX, fNewY, fNewWidth, fNewHeight );
			Refresh();
		}

		protected override void OnPaintBackground( PaintEventArgs e )
		{
//			base.OnPaintBackground( e );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			e.Graphics.FillRectangle( Brushes.Black, ClientRectangle );
			if ( m_Image != null )
			{
				// The view rectangle indicates where the image is placed in the screen
				// The crop rectangle indicates the part of the image to show
				Rectangle	Crop = m_Crop.IsEmpty ? new Rectangle( 0, 0, m_Image.Width, m_Image.Height ) : m_Crop;

				e.Graphics.DrawImage( m_Image, m_ViewRectangle, Crop, GraphicsUnit.Pixel );
			}
			base.OnPaint( e );
		}

		#endregion
	}
}
