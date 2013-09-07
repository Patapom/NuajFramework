using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using SharpDX;

namespace SequencorEditor
{
	public partial class PaletteButton : Panel
	{
		#region FIELDS

		protected Vector4			m_Vector = Vector4.Zero;
		protected Color				m_Color = Color.White;

		protected bool				m_bSelected = false;

		#endregion

		#region PROPERTIES

		public Vector4		Vector
		{
			get { return m_Vector; }
			set
			{
				m_Vector = value;
				if ( m_Vector != null )
					m_Color = AdobeColors.ConvertHDR2LDR( (Vector3) m_Vector );

				Refresh();
			}
		}

		public bool					Selected
		{
			get { return m_bSelected; }
			set
			{
				if ( value == m_bSelected )
					return;

				m_bSelected = value;
				Refresh();

				if ( SelectedChanged != null )
					SelectedChanged( this, new EventArgs() );
			}
		}

		public event EventHandler	SelectedChanged;

		#endregion

		#region METHODS

		public PaletteButton()
		{
			InitializeComponent();

			SetStyle( ControlStyles.Selectable, true );
		}

		protected override void OnPaintBackground( PaintEventArgs pevent )
		{
// 			if ( !m_bValid )
//			base.OnPaintBackground( pevent );
		}

		protected override void OnPaint( PaintEventArgs pevent )
		{
//			base.OnPaint( pevent );

			SolidBrush	Brush = new SolidBrush( m_Color );
			pevent.Graphics.FillRectangle( Brush, ClientRectangle );
			Brush.Dispose();

			if ( m_bSelected )
			{
				System.Drawing.Rectangle	Rect = new System.Drawing.Rectangle( 0, 0, Width-1, Height-1 );

				double	fIntensity = (0.3 * m_Color.R + 0.5 * m_Color.G + 0.2 * m_Color.B) / 255.0;

				Color	C = fIntensity < 0.4 ? Color.White : (fIntensity > 0.6 ? Color.Black : Color.FromArgb( 255, 0, 0 ));

				Pen	Pen = new Pen( C );
				pevent.Graphics.DrawRectangle( Pen, Rect );
				Pen.Dispose();
			}
		}

		protected override void OnClick( EventArgs e )
		{
			base.OnClick( e );

			Selected = true;
		}

		protected override void OnDoubleClick( EventArgs e )
		{
			base.OnDoubleClick( e );
		}

		#endregion
	}
}
