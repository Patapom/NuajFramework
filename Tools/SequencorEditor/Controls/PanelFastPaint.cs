using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

using System.Windows.Forms;

namespace SequencorEditor
{
	public partial class PanelFastPaint : Panel
	{
		#region CONSTANTS

		protected const int		ANIMATION_TRACKS_OFFSET = 158;	// Animation display starts from this amount of pixels

		#endregion

		#region FIELDS

		protected SequencerControl	m_Owner = null;

		protected Control			m_MimickedControl = null;
		protected Bitmap			m_Bitmap = null;

		protected Color				m_CursorTimeColor = Color.ForestGreen;
		protected Pen				m_PenCursorTime = null;

		#endregion

		#region PROPERTIES

		[Browsable( false )]
		public SequencerControl		Owner	{ get { return m_Owner; } set { m_Owner = value; } }

		[Browsable( false )]
		public Control	MimickedControl		{ get { return m_MimickedControl; } set { m_MimickedControl = value; RenderControl(); } }

		[Category( "Appearance" )]
		public Color		CursorTimeColor
		{
			get { return m_CursorTimeColor; }
			set
			{
				m_CursorTimeColor = value;

				// Rebuild brush and pens
				if ( m_PenCursorTime != null )
					m_PenCursorTime.Dispose();
				m_PenCursorTime = new Pen( m_CursorTimeColor, 1.0f );
				m_PenCursorTime.DashStyle = DashStyle.Dash;
			}
		}

		#endregion

		#region METHODS

		public PanelFastPaint()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			InitializeComponent();

			// Should rebuild pen
			CursorTimeColor = CursorTimeColor;
		}

		/// <summary>
		/// Renders the mimicked control to the bitmap
		/// </summary>
		public void		RenderControl()
		{
			if ( m_MimickedControl == null || m_Bitmap == null )
				return;

			m_Owner.ShowCursorTime = false;
			m_MimickedControl.DrawToBitmap( m_Bitmap, m_MimickedControl.ClientRectangle );
			m_Owner.ShowCursorTime = true;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
				if ( m_Bitmap != null )
					m_Bitmap.Dispose();
			}
			base.Dispose( disposing );
		}

		protected override void OnResize( EventArgs eventargs )
		{
			base.OnResize( eventargs );

			if ( m_Bitmap != null )
				m_Bitmap.Dispose();
			m_Bitmap = null;

			if ( Width <= 0 || Height <= 0 )
				return;

			m_Bitmap = new Bitmap( Width, Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb );
			RenderControl();
		}

		protected override void OnPaintBackground( PaintEventArgs e )
		{
//			base.OnPaintBackground( e );
			if ( m_Bitmap != null )
				e.Graphics.DrawImage( m_Bitmap, 0, 0 );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint( e );
			if ( m_Owner == null || m_Bitmap == null )
				return;

			// Draw cursor time
			int		ClientWidth = Width - ANIMATION_TRACKS_OFFSET - SystemInformation.VerticalScrollBarWidth;
			float	CursorX = ANIMATION_TRACKS_OFFSET + ClientWidth * (m_Owner.TimeLineControl.CursorPosition - m_Owner.TimeLineControl.VisibleBoundMin) / (m_Owner.TimeLineControl.VisibleBoundMax - m_Owner.TimeLineControl.VisibleBoundMin);
			e.Graphics.DrawLine( m_PenCursorTime, CursorX, 0, CursorX, Height );
		}

		#endregion
	}
}
