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
	/// This components handles the display and selection of a set of images.
	/// The images are displayed as a square and the selection works as a toggle when clicking on an image.
	/// </summary>
	public partial class ZoomableImagesViewer : Panel
	{
		#region CONSTANTS

		protected const int		IMAGE_GAP = 10;			// Vertical and horizontal gap between images
		protected const int		MAX_IMAGES_VIEW = 5;	// The default side of the square of viewable images
		protected const int		VIEW_GAP = 10;			// Vertical and horizontal gap of view rectangle with borders (only used for initial rectangle computation)

		#endregion

		#region FIELDS

		protected Documents.Attachment[]	m_Attachments = new Documents.Attachment[0];
		protected Rectangle[]				m_AttachmentPositions = new Rectangle[0];
		protected RectangleF				m_ViewRectangle;
		protected bool[]					m_MultipleSelection = new bool[0];
		protected Documents.Attachment		m_SingleSelection = null;
		protected bool						m_bUseMultipleSelection = true;

		// Manipulation
		protected MouseButtons				m_Buttons = MouseButtons.None;
		protected Point						m_ButtonDownPosition;
		protected RectangleF				m_ButtonDownViewRectangle;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the image to display
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
				m_AttachmentPositions = new Rectangle[m_Attachments.Length];
				m_MultipleSelection = new bool[m_Attachments.Length];
				
				// Find attachments' dimensions
				int		Width = (int) Math.Ceiling( Math.Sqrt( m_Attachments.Length ) );
				int		Height = (int) Math.Ceiling( (float) m_Attachments.Length / Width );

				int		MaxWidth = 0, MaxHeight = 0;
				foreach ( Documents.Attachment A in m_Attachments )
				{
					Rectangle	Crop = A.Crop;
					MaxWidth = Math.Max( MaxWidth, Crop.Width );
					MaxHeight = Math.Max( MaxHeight, Crop.Height );
				}
				MaxWidth += IMAGE_GAP;
				MaxHeight += IMAGE_GAP;

				// Update attachment positions
				int	X = 0, Y = 0;
				for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
				{
					Rectangle	Crop = m_Attachments[AttachmentIndex].Crop;

					m_AttachmentPositions[AttachmentIndex] = new Rectangle(
							X + (MaxWidth-Crop.Width) / 2,
							Y + (MaxHeight-Crop.Height) / 2,
							Crop.Width,
							Crop.Height
						);

					X += MaxWidth;
					if ( (AttachmentIndex+1) % Width == 0 )
					{
						X = 0;
						Y += MaxHeight;
					}
				}

				// Setup default view rectangle that keeps the image within the screen's borders
				float	OffsetX = 0.0f, OffsetY = 0.0f;
				float	SizeX = VIEW_GAP + Math.Min( MAX_IMAGES_VIEW, Math.Max( 1, Width ) ) * MaxWidth + VIEW_GAP;
				float	SizeY = VIEW_GAP + Math.Min( MAX_IMAGES_VIEW, Math.Max( 1, Height ) ) * MaxHeight + VIEW_GAP;
				if ( SizeY * this.Width > SizeX * this.Height )
				{	// Vertical fit
					float	OldSize = SizeX;
					SizeX = SizeY * this.Width / this.Height;
					OffsetX = 0.5f * (OldSize - SizeX);
				}
				else
				{	// Horizontal fit
					float	OldSize = SizeY;
					SizeY = SizeX * this.Height / this.Width;
					OffsetY = 0.5f * (OldSize - SizeY);
				}

				m_ViewRectangle = new RectangleF( OffsetX, OffsetY, SizeX, SizeY );

				Invalidate();
			}
		}

		public bool							UseMultipleSelection
		{
			get { return m_bUseMultipleSelection; }
			set { m_bUseMultipleSelection = value; }
		}

		[System.ComponentModel.Browsable( false )]
		public Documents.Attachment[]		MultipleSelection
		{
			get
			{
				List<Documents.Attachment>	Result = new List<Documents.Attachment>();
				for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
					if ( m_MultipleSelection[AttachmentIndex] )
						Result.Add( m_Attachments[AttachmentIndex] );

				return Result.ToArray();
			}
			set
			{
				if ( value == null )
					value = new Documents.Attachment[0];

				// Update multiple selection booleans
				List<Documents.Attachment>	NewSelection = new List<Documents.Attachment>( value );
				for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
					m_MultipleSelection[AttachmentIndex] = NewSelection.Contains( m_Attachments[AttachmentIndex] );
				Refresh();

				// Notify
				if ( SelectionChanged != null )
					SelectionChanged( this, EventArgs.Empty );
			}
		}

		[System.ComponentModel.Browsable( false )]
		public Documents.Attachment			SingleSelection
		{
			get { return m_SingleSelection; }
			set
			{
				bool	bOneOfOurs = false;
				foreach ( Documents.Attachment A in m_Attachments )
					if ( value == A )
					{
						bOneOfOurs = true;
						break;
					}

				if ( !bOneOfOurs )
					value = null;	// This selection is not part of our attachments !

				if ( value == m_SingleSelection )
					return;

				m_SingleSelection = value;

				// Update multiple selection booleans
				for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
					m_MultipleSelection[AttachmentIndex] = m_Attachments[AttachmentIndex] == m_SingleSelection;
				Refresh();

				// Notify
				if ( SelectionChanged != null )
					SelectionChanged( this, EventArgs.Empty );
			}
		}

		public event EventHandler			SelectionChanged;

		#endregion

		#region METHODS

		public ZoomableImagesViewer()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			InitializeComponent();
		}

		public void		ZoomOnSelection()
		{
			// Zoom all if nothing is selected, otherwise zoom on selection
			bool	bZoomAll = MultipleSelection.Length == 0;

			// Retrieve selection's bounding rectangle
			int		MinX = +int.MaxValue;
			int		MaxX = -int.MaxValue;
			int		MinY = +int.MaxValue;
			int		MaxY = -int.MaxValue;
			for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
				if ( m_MultipleSelection[AttachmentIndex] || bZoomAll )
				{
					MinX = Math.Min( MinX, m_AttachmentPositions[AttachmentIndex].Left );
					MaxX = Math.Max( MaxX, m_AttachmentPositions[AttachmentIndex].Right );
					MinY = Math.Min( MinY, m_AttachmentPositions[AttachmentIndex].Top );
					MaxY = Math.Max( MaxY, m_AttachmentPositions[AttachmentIndex].Bottom );
				}

			// Focus on that rectangle
			float	NewWidth = MaxX - MinX, NewHeight = MaxY - MinY;
			if ( NewHeight * m_ViewRectangle.Width > NewWidth * m_ViewRectangle.Height )
				NewWidth = NewHeight * m_ViewRectangle.Width / m_ViewRectangle.Height;
			else
				NewHeight = NewWidth * m_ViewRectangle.Height / m_ViewRectangle.Width;

			NewWidth *= 1.05f;
			NewHeight *= 1.05f;

			m_ViewRectangle = new RectangleF(
				0.5f * (MinX+MaxX-NewWidth),
				0.5f * (MinY+MaxY-NewHeight),
				NewWidth,
				NewHeight
				);
			Refresh();
		}

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );

			float	fOldAspectRatio = m_ViewRectangle.Width / m_ViewRectangle.Height;
			float	fNewAspectRatio = (float) Math.Max( 1, Width ) / Math.Max( 1, Height );

			float	CenterX = 0.5f * (m_ViewRectangle.Left + m_ViewRectangle.Right);
			float	CenterY = 0.5f * (m_ViewRectangle.Top + m_ViewRectangle.Bottom);
			float	NewWidth = m_ViewRectangle.Width * fNewAspectRatio / fOldAspectRatio;
			float	NewHeight = m_ViewRectangle.Height;

			m_ViewRectangle = new RectangleF( CenterX - 0.5f * NewWidth, CenterY - 0.5f * NewHeight, NewWidth, NewHeight );

			Invalidate();
		}

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );
			m_Buttons |= e.Button;
			m_ButtonDownPosition = e.Location;
			m_ButtonDownViewRectangle = m_ViewRectangle;

			this.Focus();
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );

			if ( m_Buttons != MouseButtons.Left && m_Buttons != MouseButtons.Middle )
				return;

			// Pan
			Point	Delta = new Point( m_ButtonDownPosition.X - e.Location.X, m_ButtonDownPosition.Y - e.Location.Y );

			float	ViewDeltaX = Delta.X * m_ViewRectangle.Width / Width;
			float	ViewDeltaY = Delta.Y * m_ViewRectangle.Height / Height;

			m_ViewRectangle = new RectangleF( m_ButtonDownViewRectangle.X + ViewDeltaX, m_ButtonDownViewRectangle.Y + ViewDeltaY, m_ButtonDownViewRectangle.Width, m_ButtonDownViewRectangle.Height );
			Refresh();
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );
			m_Buttons &= ~e.Button;

			if ( Math.Abs(e.X - m_ButtonDownPosition.X) > SystemInformation.DragSize.Width || Math.Abs(e.Y - m_ButtonDownPosition.Y) > SystemInformation.DragSize.Height )
				return;	// We moved so that was not a proper selection...

			// Check selection
			float	fScaleFactorX = Width / m_ViewRectangle.Width;
			float	fScaleFactorY = Height / m_ViewRectangle.Height;
			
			Documents.Attachment		NewSingleSelection = null;
			List<Documents.Attachment>	NewMultipleSelection = new List<Documents.Attachment>( MultipleSelection );

			for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
			{
				Documents.Attachment	A = m_Attachments[AttachmentIndex];
				Rectangle	ArrayPosition = m_AttachmentPositions[AttachmentIndex];
				RectangleF	ScreenPosition = new RectangleF(
					(ArrayPosition.X - m_ViewRectangle.X) * fScaleFactorX,
					(ArrayPosition.Y - m_ViewRectangle.Y) * fScaleFactorY,
					ArrayPosition.Width * fScaleFactorX,
					ArrayPosition.Height * fScaleFactorY
					);

				if ( !ScreenPosition.Contains( e.Location ) )
					continue;

				// Toggle selection
				if ( m_MultipleSelection[AttachmentIndex] )
					NewMultipleSelection.Remove( A  );
				else
				{
					NewMultipleSelection.Add( A );
					NewSingleSelection = A;
				}
			}

			// Update selection
			if ( m_bUseMultipleSelection )
				MultipleSelection = NewMultipleSelection.ToArray();
			else
				SingleSelection = NewSingleSelection;
		}

		protected override void OnMouseWheel( MouseEventArgs e )
		{
			base.OnMouseWheel( e );

			float	fZoomFactor = e.Delta < 0 ? 1.1f : 1.0f / 1.1f;

			float	ViewMouseX = m_ViewRectangle.X + e.X * m_ViewRectangle.Width / Width;
			float	ViewMouseY = m_ViewRectangle.Y + e.Y * m_ViewRectangle.Height / Height;

			// Zoom and keep current position fixed
			float	fNewWidth = m_ViewRectangle.Width * fZoomFactor;
			float	fNewHeight = m_ViewRectangle.Height * fZoomFactor;
			float	fNewX = ViewMouseX + fZoomFactor * (m_ViewRectangle.X - ViewMouseX);
			float	fNewY = ViewMouseY + fZoomFactor * (m_ViewRectangle.Y - ViewMouseY);

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

			float	fScaleFactorX = Width / m_ViewRectangle.Width;
			float	fScaleFactorY = Height / m_ViewRectangle.Height;
			for ( int AttachmentIndex=0; AttachmentIndex < m_Attachments.Length; AttachmentIndex++ )
			{
				Documents.Attachment	A = m_Attachments[AttachmentIndex];
				Rectangle	ArrayPosition = m_AttachmentPositions[AttachmentIndex];
				RectangleF	ScreenPosition = new RectangleF(
					(ArrayPosition.X - m_ViewRectangle.X) * fScaleFactorX,
					(ArrayPosition.Y - m_ViewRectangle.Y) * fScaleFactorY,
					ArrayPosition.Width * fScaleFactorX,
					ArrayPosition.Height * fScaleFactorY
					);

				if ( ScreenPosition.Left >= Width || ScreenPosition.Right < 0 )
					continue;	// Out of screen
				if ( ScreenPosition.Top >= Height || ScreenPosition.Bottom < 0 )
					continue;	// Out of screen

				e.Graphics.DrawImage( A.Bitmap, ScreenPosition, A.Crop, GraphicsUnit.Pixel );

				// Draw selection
				if ( m_MultipleSelection[AttachmentIndex] )
					e.Graphics.DrawRectangle( Pens.Red, ScreenPosition.X-2, ScreenPosition.Y-2, ScreenPosition.Width+4, ScreenPosition.Height+4 );
			}

			base.OnPaint( e );
		}

		#endregion
	}
}
