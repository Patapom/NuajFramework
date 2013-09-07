using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SequencorEditor
{
	/// <summary>
	/// Draws a cursor inside a panel
	/// </summary>
	public class CursorPanel : Panel
	{
		#region CONSTANTS

		private const float		CURSOR_ARROW_HALF_WIDTH	= 8.0f;

		#endregion

		#region NESTED TYPES

		public delegate void	CursorMovedEventHandler( CursorPanel _Sender );

		#endregion

		#region FIELDS

		protected float		m_BoundMin = 0.0f;
		protected float		m_BoundMax = 1.0f;
		protected float		m_CursorPosition = 0.5f;

		// Appearance
		protected Color		m_CursorColor = SystemColors.ControlText;
		protected Pen		m_CursorPen = null;
		protected Brush		m_CursorBrush = null;
		protected Brush		m_TextBrush = null;

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Category( "Cursor" )]
		public float		BoundMin
		{
			get { return m_BoundMin; }
			set { m_BoundMin = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Cursor" )]
		public float		BoundMax
		{
			get { return m_BoundMax; }
			set { m_BoundMax = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Cursor" )]
		public float		CursorPosition
		{
			get { return m_CursorPosition; }
			set
			{
				value = Math.Max( m_BoundMin, Math.Min( m_BoundMax, value ) );
				if ( Math.Abs( value - m_CursorPosition ) < 1e-3f )
					return;	// No change...

				m_CursorPosition = value;
				Refresh();

				if ( CursorMoved != null )
					CursorMoved( this );
			}
		}

		[System.ComponentModel.Category( "Appearance" )]
		public Color		CursorColor
		{
			get { return m_CursorColor; }
			set
			{
				m_CursorColor = value;

				// Dispose of the former pen & brush
				m_CursorPen.Dispose();
				m_CursorBrush.Dispose();

				// Create new ones with the new color
				m_CursorPen = new Pen( m_CursorColor );
//				m_CursorBrush = new SolidBrush( Crownwood.DotNetMagic.Common.ColorHelper.TabBackgroundFromBaseColor( m_CursorColor ) );
				m_CursorBrush = new SolidBrush( m_CursorColor );

				Refresh();
			}
		}

		[System.ComponentModel.Category( "Cursor" )]
		public event CursorMovedEventHandler	CursorMoved;

		#endregion

		#region METHODS

		public	CursorPanel()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			// Initialize the graduation pens with our fore color
			m_CursorPen = new Pen( m_CursorColor );
//			m_CursorBrush = new SolidBrush( Crownwood.DotNetMagic.Common.ColorHelper.TabBackgroundFromBaseColor( m_CursorColor ) );
			m_CursorBrush = new SolidBrush( m_CursorColor );
			m_TextBrush = new SolidBrush( this.ForeColor );
		}

		protected override void Dispose( bool disposing )
		{
			// Dispose of the objects
			m_CursorPen.Dispose();
			m_CursorBrush.Dispose();
			m_TextBrush.Dispose();

			base.Dispose( disposing );
		}

		/// <summary>
		/// Sets both min and max bounds with a single update
		/// </summary>
		/// <param name="_BoundMin">The new min bound</param>
		/// <param name="_BoundMax">The new max bound</param>
		public void		SetRange( float _BoundMin, float _BoundMax )
		{
			m_BoundMin = _BoundMin;
			m_BoundMax = _BoundMax;
			CursorPosition = CursorPosition;	// Should update the clamped cursor position

			Refresh();
		}

		/// <summary>
		/// Sets the cursors position given a CLIENT SPACE position
		/// </summary>
		/// <param name="_ClientPosition">The client space position</param>
		public void		SetClientCursorPosition( int _ClientPosition )
		{
			CursorPosition = m_BoundMin + _ClientPosition * (m_BoundMax - m_BoundMin) / this.Width;
		}

		#region Control members

		protected override void OnForeColorChanged( EventArgs e )
		{
			base.OnForeColorChanged( e );

			// Create a new brush
			m_TextBrush.Dispose();
			m_TextBrush = new SolidBrush( this.ForeColor );
		}

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );

			Refresh();
		}

		protected override void OnPaint( PaintEventArgs e )
		{
//			base.OnPaint( e );
			if ( Math.Abs( m_BoundMax - m_BoundMin ) < 1e-3f )
				return;	// Can't trace invalid range!

			float	fCursorPosition = (m_CursorPosition - m_BoundMin) * (Width-1) / (m_BoundMax - m_BoundMin);

			// Draw the cursor as a thin arrow
			GraphicsPath	Path = new GraphicsPath();
			Path.AddPolygon( new PointF[] {	new PointF( fCursorPosition, 0.0f ),
											new PointF( fCursorPosition-CURSOR_ARROW_HALF_WIDTH, Height ),
											new PointF( fCursorPosition+CURSOR_ARROW_HALF_WIDTH, Height )
											} );

			e.Graphics.FillPath( m_CursorBrush, Path );
			e.Graphics.DrawPath( m_CursorPen, Path );

			// Draw some text
//			string	Text = m_CursorPosition.ToString( "G4" );

			// Format as MM:ss:mmm
			float	fValue = m_CursorPosition;
			int		Minutes = (int) Math.Floor( fValue / 60.0f );
			fValue -= Minutes * 60;
			int		Seconds = (int) Math.Floor( fValue );
			fValue -= Seconds;
			int		MilliSeconds = (int) Math.Floor( fValue * 1000.0f );
			string	Text = (Minutes > 0 ? Minutes.ToString() + ":" : "") + (Minutes > 0 ? Seconds.ToString( "D2" ) : Seconds.ToString()) + ":" + MilliSeconds.ToString( "D3" );

			SizeF	TextSize = e.Graphics.MeasureString( Text, Font );
			if ( fCursorPosition < .5f * Width )
				e.Graphics.DrawString( Text, Font, m_TextBrush, fCursorPosition + CURSOR_ARROW_HALF_WIDTH, 0.0f );
			else
				e.Graphics.DrawString( Text, Font, m_TextBrush, fCursorPosition - CURSOR_ARROW_HALF_WIDTH - TextSize.Width, 0.0f );

			if ( Enabled )
				return;

			// Draw a disabled look
			HatchBrush	DisableBrush = new HatchBrush( HatchStyle.Percent50, SystemColors.Control, Color.Transparent );
			e.Graphics.FillRectangle( DisableBrush, ClientRectangle );
			DisableBrush.Dispose();
		}

		#endregion

		#endregion
	}
}
