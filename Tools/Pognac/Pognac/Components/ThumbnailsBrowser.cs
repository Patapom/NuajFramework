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
	/// <summary>
	/// This should be used to view thumbnails for a list of images
	/// Use for pages in a document, orphan files waiting to be added to a document, etc.
	/// Double clicking it should open the fullscreen images browser
	/// </summary>
	public partial class ThumbnailBrowser : Panel
	{
		#region CONSTANTS

		protected const float	THUMBNAIL_TOP_GAP = 4;		// Gap between thumbnail and top/bottom borders
		protected const float	THUMBNAIL_INTER_GAP = 10;	// Gap between 2 consecutive thumbnails

		#endregion

		#region NESTED TYPES

		protected class		Thumbnail
		{
			#region FIELDS

			protected Documents.Attachment	m_Attachment = null;
			protected Size					m_Size;
			protected float					m_DisplayWidth = 0.0f;
			protected float					m_DisplayHeight = 0.0f;

			#endregion

			#region PROPERTIES

			public Documents.Attachment		Attachment			{ get { return m_Attachment; } }
			public Size						Size				{ get { return m_Size; } }
			public float					DisplayWidth		{ get { return m_DisplayWidth; } }
			public float					DisplayHeight		{ get { return m_DisplayHeight; } }

			#endregion

			#region METHODS

			public Thumbnail( Documents.Attachment _Attachment )
			{
				m_Attachment = _Attachment;
				m_Size = new Size( _Attachment.Crop.Width, _Attachment.Crop.Height );
			}

			/// <summary>
			/// Computes the display size of the thumbnail based on current client height
			/// </summary>
			/// <param name="_ClientHeight"></param>
			public void		ComputeDisplaySize( int _ClientHeight )
			{
				float	fZoomFactor = (float) (_ClientHeight-2*THUMBNAIL_TOP_GAP) / m_Size.Height;

				m_DisplayWidth = fZoomFactor * m_Size.Width;	// Width of the thumbnail on screen
				m_DisplayHeight = fZoomFactor * m_Size.Height;	// Height of the thumbnail on screen
			}

			/// <summary>
			/// Draw the thumbnail at the specified position
			/// </summary>
			/// <param name="_Graphics"></param>
			/// <param name="_Position"></param>
			/// <param name="_ClientRectangle"></param>
			/// <param name="_bSelected"></param>
			public void		Draw( Graphics _Graphics, ref float _Position, Rectangle _ClientRectangle, bool _bSelected )
			{
				RectangleF	DestRect = new RectangleF( _Position + THUMBNAIL_INTER_GAP, THUMBNAIL_TOP_GAP, m_DisplayWidth, m_DisplayHeight );

				_Position += DestRect.Width + 2 * THUMBNAIL_INTER_GAP;	// Increase position for next thumbnail

				if ( DestRect.Left >= _ClientRectangle.Width || DestRect.Right < 0 )
					return;

				// Part of the thumbnail is visible so display...
				_Graphics.DrawImage( m_Attachment.Bitmap, DestRect, m_Attachment.Crop, GraphicsUnit.Pixel );
				if ( _bSelected )
					_Graphics.DrawRectangle( Pens.Red, DestRect.Left-2, DestRect.Top-2, DestRect.Width+4, DestRect.Height+4 );
			}

			/// <summary>
			/// Returns the horizontal position of the center of this thumbnail
			/// </summary>
			/// <param name="_Position"></param>
			/// <returns></returns>
			public float	GetCenterPosition( ref float _Position )
			{
				RectangleF	DestRect = new RectangleF( _Position + THUMBNAIL_INTER_GAP, THUMBNAIL_TOP_GAP, m_DisplayWidth, m_DisplayHeight );
				_Position += DestRect.Width + 2 * THUMBNAIL_INTER_GAP;	// Increase position for next thumbnail

				return 0.5f * (DestRect.Left + DestRect.Right);
			}

			/// <summary>
			/// Tells if the thumbnail rectangle contains the provided position (i.e. clicked)
			/// </summary>
			/// <param name="_ClickPosition"></param>
			/// <param name="_Position"></param>
			/// <returns></returns>
			public bool		Contains( Point _ClickPosition, ref float _Position )
			{
				RectangleF	DestRect = new RectangleF( _Position + THUMBNAIL_INTER_GAP, THUMBNAIL_TOP_GAP, m_DisplayWidth, m_DisplayHeight );
				_Position += DestRect.Width + 2 * THUMBNAIL_INTER_GAP;	// Increase position for next thumbnail

				return DestRect.Contains( _ClickPosition );
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected Documents.Attachment[]	m_Attachments = new Documents.Attachment[0];
		protected Thumbnail[]				m_Thumbnails = new Thumbnail[0];
		protected float						m_ViewOffset = 0.0f;

		protected Documents.Attachment		m_Selection = null;

		protected bool						m_bDoubleClickOpensViewer = true;

		protected float						m_TotalThumbnailsWidth = 0.0f;
		protected float						m_ViewOffsetMin = 0.0f;
		protected float						m_ViewOffsetMax = 0.0f;

		// Manipulation
		protected MouseButtons				m_Buttons = MouseButtons.None;
		protected Point						m_ButtonDownPosition;
		protected float						m_ButtonDownViewOffset = 0.0f;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the images to display
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Documents.Attachment[]	Attachments
		{
			get { return m_Attachments; }
			set
			{
				if ( value == null )
					value = new Documents.Attachment[0];
				if ( value == m_Attachments )
					return;

				m_Attachments = value;

				// Create thumbnails
				bool	bSelectionIsValid = false;
				m_Thumbnails = new Thumbnail[m_Attachments.Length];
				for ( int ImageIndex=0; ImageIndex < m_Attachments.Length; ImageIndex++ )
				{
					m_Thumbnails[ImageIndex] = new Thumbnail( m_Attachments[ImageIndex] );
					if ( m_Attachments[ImageIndex] == Selection )
						bSelectionIsValid = true;
				}

				if ( !bSelectionIsValid )
					Selection = null;	// Clear selection as it's not part of our new attachments any more
				else if ( SelectionChanged != null )
					SelectionChanged( this, EventArgs.Empty );	// Simply notify selection changed (although it stayed the same)

				OnResize( EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the selected attachment
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Documents.Attachment		Selection
		{
			get { return m_Selection; }
			set
			{
				if ( value == m_Selection )
					return;

				m_Selection = value;
				Refresh();

				// Notify
				if ( SelectionChanged != null )
					SelectionChanged( this, EventArgs.Empty );
			}
		}

		public event EventHandler		SelectionChanged;

		public bool						DoubleClickOpensViewer
		{
			get { return m_bDoubleClickOpensViewer; }
			set { m_bDoubleClickOpensViewer = value; }
		}

		#endregion

		#region METHODS

		public ThumbnailBrowser()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			InitializeComponent();
		}

		/// <summary>
		/// Gets the attachment under the specified position
		/// </summary>
		/// <param name="_Position">Position in CLIENT space</param>
		/// <returns></returns>
		public Documents.Attachment	GetAttachmentAtPoint( Point _Position )
		{
			if ( !ClientRectangle.Contains( _Position ) )
				return null;

			float	CurrentOffset = -m_ViewOffset;
			foreach ( Thumbnail T in m_Thumbnails )
				if ( T.Contains( _Position, ref CurrentOffset ) )
					return T.Attachment;

			return null;
		}

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );

			// Update thumbnail sizes
			m_TotalThumbnailsWidth = 0.0f;
			foreach ( Thumbnail T in m_Thumbnails )
			{
				T.ComputeDisplaySize( Height );
				m_TotalThumbnailsWidth += T.DisplayWidth;
			}

			// Compute boundary view offsets
			if ( m_Thumbnails.Length > 0 )
			{
				m_ViewOffsetMin = 0.5f * (m_Thumbnails[0].DisplayWidth - Width);
				m_ViewOffsetMax = m_TotalThumbnailsWidth - 0.5f * (m_Thumbnails[m_Thumbnails.Length-1].DisplayWidth + Width);
				m_ViewOffset = Math.Max( m_ViewOffsetMin, Math.Min( m_ViewOffsetMax, m_ViewOffset ) );
			}

			Invalidate();
		}

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );
			m_Buttons |= e.Button;
			m_ButtonDownPosition = e.Location;
			m_ButtonDownViewOffset = m_ViewOffset;
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );

			if ( m_Buttons != MouseButtons.Left )
				return;

			// Pan
			float	Delta = m_ButtonDownPosition.X - e.Location.X;
			m_ViewOffset = Math.Max( m_ViewOffsetMin, Math.Min( m_ViewOffsetMax, m_ButtonDownViewOffset + Delta ) );
			Refresh();
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );
			m_Buttons &= ~e.Button;

			if ( Math.Abs(e.X - m_ButtonDownPosition.X) > SystemInformation.DragSize.Width || Math.Abs(e.Y - m_ButtonDownPosition.Y) > SystemInformation.DragSize.Height )
				return;	// We moved so that was not a proper selection...

			// Update selection
			Selection = GetAttachmentAtPoint( e.Location );
		}

		protected override void OnMouseDoubleClick( MouseEventArgs e )
		{
			base.OnMouseDoubleClick( e );

			if ( !m_bDoubleClickOpensViewer )
				return;

			// Spawn a fullscreen image browser
			ImageBrowserForm	F = new ImageBrowserForm();
			F.Attachments = m_Attachments;
			F.Selection = Selection != null ? Selection : FindMostCenteredThumbnail();
			F.ShowDialog();
		}

		protected override void OnPaintBackground( PaintEventArgs e )
		{
//			base.OnPaintBackground( e );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			e.Graphics.FillRectangle( Brushes.Black, ClientRectangle );
			if ( m_Attachments.Length > 0 )
			{
				float	fCurrentOffset = -m_ViewOffset;
				foreach ( Thumbnail T in m_Thumbnails )
					T.Draw( e.Graphics, ref fCurrentOffset, ClientRectangle, T.Attachment == m_Selection );

			}
			base.OnPaint( e );
		}

		/// <summary>
		/// Finds the most centered thumbnail (the one most in the center of the screen)
		/// </summary>
		/// <returns></returns>
		protected Documents.Attachment	FindMostCenteredThumbnail()
		{
			float					fClosestDistance = float.MaxValue;
			Documents.Attachment	ClosestThumbnail = null;

			float	fCurrentOffset = m_ViewOffset;
			foreach ( Thumbnail T in m_Thumbnails )
			{
				float	fThumbnailCenterPosition = T.GetCenterPosition( ref fCurrentOffset );
				float	fDistance2Center = Math.Abs( fThumbnailCenterPosition - 0.5f * Width );
				if ( fDistance2Center > fClosestDistance )
					continue;	// Not best position ever...

				// We got a new best candidate !
				fClosestDistance = fDistance2Center;
				ClosestThumbnail = T.Attachment;
			}

			return ClosestThumbnail;
		}

		#endregion
	}
}
