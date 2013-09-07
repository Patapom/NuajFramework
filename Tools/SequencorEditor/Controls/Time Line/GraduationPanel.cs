using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SequencorEditor
{
	/// <summary>
	/// Draws a graduated interval
	/// </summary>
	public class GraduationPanel : Panel
	{
		#region CONSTANTS

		protected const int		MAX_TEXTS_COUNT		= 5;

		#endregion

		#region NESTED TYPES

		// All these delegates are used for custom event handling by external sources
		// The corresponding events are triggered right before the actual Windows event is triggered
		// If the delegate returns "true", it means the message was handled and no Windows message is sent
		//

		public delegate bool		CustomMouseDownEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool		CustomMouseMoveEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool		CustomMouseUpEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool		CustomMouseHoverEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool		CustomMouseDoubleClickEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool		CustomKeyDownEventHandler( GraduationPanel _Panel, KeyEventArgs _e );

		// Custom paint can be added after the control finished to paint itself but before it gets displayed
		//

		public delegate void		CustomPaintEventHandler( GraduationPanel _Panel, PaintEventArgs _e );

		#endregion

		#region FIELDS

		protected float		m_BoundMin = 0.0f;
		protected float		m_BoundMax = 1.0f;
		protected int		m_MaxGraduations = 100;

		protected bool		m_bShowLargeGraduations = true;
		protected float		m_LargeGraduationSize = 0.25f;
		protected bool		m_bShowMediumGraduations = true;
		protected float		m_MediumGraduationSize = 0.05f;
		protected bool		m_bShowSmallGraduations = true;
		protected float		m_SmallGraduationSize = 0.025f;

		// Appearance
		protected Color		m_GraduationColor = SystemColors.ControlText;
		protected Pen		m_GraduationPen = null;
		protected Pen		m_GraduationPenLarge = null;
		private ToolTip toolTipTrackInfos;
		private System.ComponentModel.IContainer components;
		protected Brush		m_TextBrush = null;

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Category( "Graduation" )]
		public float		BoundMin
		{
			get { return m_BoundMin; }
			set { m_BoundMin = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		public float		BoundMax
		{
			get { return m_BoundMax; }
			set { m_BoundMax = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		public int			MaxVisibleGraduations
		{
			get { return m_MaxGraduations; }
			set { m_MaxGraduations = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		/// <summary>
		/// Gets or sets the flag that shows or hides the large graduation
		/// </summary>
		public bool			ShowLargeGraduations
		{
			get { return m_bShowLargeGraduations; }
			set { if ( m_bShowLargeGraduations == value ) return; m_bShowLargeGraduations = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		/// <summary>
		/// Gets or sets the size of a large graduation
		/// </summary>
		public float		LargeGraduationSize
		{
			get { return m_LargeGraduationSize; }
			set { m_LargeGraduationSize = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		/// <summary>
		/// Gets or sets the flag that shows or hides the medium graduations
		/// </summary>
		public bool			ShowMediumGraduations
		{
			get { return m_bShowMediumGraduations; }
			set { if ( m_bShowMediumGraduations == value ) return; m_bShowMediumGraduations = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		/// <summary>
		/// Gets or sets the size of a medium graduation
		/// </summary>
		public float		MediumGraduationSize
		{
			get { return m_MediumGraduationSize; }
			set { m_MediumGraduationSize = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		/// <summary>
		/// Gets or sets the flag that shows or hides the small graduations
		/// </summary>
		public bool			ShowSmallGraduations
		{
			get { return m_bShowSmallGraduations; }
			set { if ( m_bShowSmallGraduations == value ) return; m_bShowSmallGraduations = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Graduation" )]
		/// <summary>
		/// Gets or sets the size of a small graduation
		/// </summary>
		public float		SmallGraduationSize
		{
			get { return m_SmallGraduationSize; }
			set { m_SmallGraduationSize = value; Refresh(); }
		}

		[System.ComponentModel.Category( "Appearance" )]
		public Color		GraduationColor
		{
			get { return m_GraduationColor; }
			set
			{
				m_GraduationColor = value;

				// Dispose of the former pen
				m_GraduationPen.Dispose();
				m_GraduationPenLarge.Dispose();

				// Create a new one with the new color
				m_GraduationPen = new Pen( m_GraduationColor);
				m_GraduationPenLarge = new Pen( m_GraduationColor, 2.0f );

				Refresh();
			}
		}

		[System.ComponentModel.Category( "Mouse" )]
		public event CustomMouseDownEventHandler		CustomMouseDown;

		[System.ComponentModel.Category( "Mouse" )]
		public event CustomMouseMoveEventHandler		CustomMouseMove;

		[System.ComponentModel.Category( "Mouse" )]
		public event CustomMouseUpEventHandler			CustomMouseUp;

		[System.ComponentModel.Category( "Mouse" )]
		public event CustomMouseHoverEventHandler		CustomMouseHover;

		[System.ComponentModel.Category( "Mouse" )]
		public event CustomMouseDoubleClickEventHandler	CustomMouseDoubleClick;

		[System.ComponentModel.Category( "Key" )]
		public event CustomKeyDownEventHandler			CustomKeyDown;

		[System.ComponentModel.Category( "Appearance" )]
		public event CustomPaintEventHandler			CustomPaint;

		#endregion

		#region METHODS

		public	GraduationPanel()
		{
			SetStyle( ControlStyles.Selectable, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.UserPaint, true );

			InitializeComponent();

			// Initialize the graduation pens with our fore color
			m_GraduationPen = new Pen( m_GraduationColor );
			m_GraduationPenLarge = new Pen( m_GraduationColor, 2.0f );
			m_TextBrush = new SolidBrush( this.ForeColor );
		}

		protected override void Dispose( bool disposing )
		{
			// Dispose of the objects
			m_GraduationPen.Dispose();
			m_GraduationPenLarge.Dispose();
			m_TextBrush.Dispose();

			base.Dispose( disposing );
		}

		#region Designer's Code

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.toolTipTrackInfos = new System.Windows.Forms.ToolTip( this.components );
			this.SuspendLayout();
			// 
			// toolTipTrackInfos
			// 
			this.toolTipTrackInfos.AutoPopDelay = 5000;
			this.toolTipTrackInfos.InitialDelay = 0;
			this.toolTipTrackInfos.ReshowDelay = 100;
			this.toolTipTrackInfos.ShowAlways = true;
			this.ResumeLayout( false );

		}

		#endregion

		/// <summary>
		/// Sets both min and max bounds with a single update
		/// </summary>
		/// <param name="_BoundMin">The new min bound</param>
		/// <param name="_BoundMax">The new max bound</param>
		public void		SetRange( float _BoundMin, float _BoundMax )
		{
			m_BoundMin = _BoundMin;
			m_BoundMax = _BoundMax;
			Refresh();
		}

		/// <summary>
		/// Helper that converts a CLIENT SPACE position into a graduation position
		/// </summary>
		/// <param name="_ClientPosition">The client position to convert</param>
		/// <returns>The position in graduation space</returns>
		public float	ClientToGraduation( int _ClientPosition )
		{
			return	m_BoundMin + _ClientPosition * (m_BoundMax - m_BoundMin) / Width;
		}

		/// <summary>
		/// Helper that converts a GRADUATION SPACE position into a client position
		/// </summary>
		/// <param name="_fGraduationPosition">The graduation position to convert</param>
		/// <returns>The position in client space</returns>
		public float	GraduationToClient( float _fGraduationPosition )
		{
			return	(_fGraduationPosition - m_BoundMin) * Width / (m_BoundMax - m_BoundMin);
		}

		public void		SimulateMouseDown( MouseEventArgs _e )
		{
			OnMouseDown( _e );
		}

		public void		SimulateMouseMove( MouseEventArgs _e )
		{
			OnMouseMove( _e );
		}

		public void		SimulateMouseUp( MouseEventArgs _e )
		{
			OnMouseUp( _e );
		}

		public void		SimulateMouseWheel( MouseEventArgs _e )
		{
			OnMouseWheel( _e );
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

		protected override void OnMouseDown( MouseEventArgs e )
		{
			if ( CustomMouseDown != null && CustomMouseDown( this, e ) )
				return;	// We handled the mouse event...

			base.OnMouseDown( e );
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			this.Focus();

			if ( CustomMouseMove != null && CustomMouseMove( this, e ) )
			{	// We handled the mouse event...
				toolTipTrackInfos.SetToolTip( this, "" );
				return;
			}

			// Setup a tooltip giving the current time based on mouse position
			toolTipTrackInfos.SetToolTip( this, "[" + TimeSpan.FromSeconds( ClientToGraduation( e.X ) ).ToString() + "]" );

			base.OnMouseMove( e );
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			if ( CustomMouseUp != null && CustomMouseUp( this, e ) )
				return;	// We handled the mouse event...

			base.OnMouseUp( e );
		}

		protected override void OnMouseHover( EventArgs e )
		{
			Point	ClientPos = PointToClient( Control.MousePosition );
			if ( CustomMouseHover != null && CustomMouseHover( this, new MouseEventArgs( MouseButtons.None, 0, ClientPos.X, ClientPos.Y, 0 ) ) )
				return;

			base.OnMouseHover( e );
		}

		protected override void OnMouseDoubleClick( MouseEventArgs e )
		{
			if ( CustomMouseDoubleClick != null && CustomMouseDoubleClick( this, e ) )
				return;	// We handled the mouse event...

			base.OnMouseDoubleClick( e );
		}

		protected override void  OnKeyDown(KeyEventArgs e)
		{
			if ( CustomKeyDown != null && CustomKeyDown( this, e ) )
				return;

		 	 base.OnKeyDown( e );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
//			base.OnPaint( e );
			if ( m_BoundMax - m_BoundMin < 1e-3f )
				return;	// Can't trace invalid range!

			e.Graphics.FillRectangle( Brushes.WhiteSmoke, 0, .8f * Height, Width, .2f * Height );

			// Draw small graduations
			if ( m_bShowSmallGraduations )
			{
				int MinGraduationIndex = (int) Math.Floor( m_BoundMin / m_SmallGraduationSize );
				int MaxGraduationIndex = (int) Math.Floor( m_BoundMax / m_SmallGraduationSize );
				int	GraduationsCount = (1 + MaxGraduationIndex - MinGraduationIndex);
				int	GraduationsStep = 1;

				if ( GraduationsCount > m_MaxGraduations )
				{	// Make sure we always have less than the maximum requested number of graduations
					GraduationsStep = (int) Math.Ceiling( (float) GraduationsCount / m_MaxGraduations );
				}

				for ( int GraduationIndex=MinGraduationIndex; GraduationIndex <= MaxGraduationIndex; GraduationIndex+=GraduationsStep )
				{
					float	fPosition = (GraduationIndex * m_SmallGraduationSize - m_BoundMin) * this.Width / (m_BoundMax - m_BoundMin);
					e.Graphics.DrawLine( m_GraduationPen, fPosition, this.Height, fPosition, .75f * this.Height );
				}
			}

			// Draw medium graduations
			if ( m_bShowMediumGraduations )
			{
				int MinGraduationIndex = (int) Math.Floor( m_BoundMin / m_MediumGraduationSize );
				int MaxGraduationIndex = (int) Math.Floor( m_BoundMax / m_MediumGraduationSize );
				int	GraduationsCount = (1 + MaxGraduationIndex - MinGraduationIndex);
				int	GraduationsStep = 1;

				if ( GraduationsCount > m_MaxGraduations )
				{	// Make sure we always have less than the maximum requested number of graduations
					GraduationsStep = (int) Math.Ceiling( (float) GraduationsCount / m_MaxGraduations );
				}

				for ( int GraduationIndex=MinGraduationIndex; GraduationIndex <= MaxGraduationIndex; GraduationIndex+=GraduationsStep )
				{
					float	fPosition = (GraduationIndex * m_MediumGraduationSize - m_BoundMin) * this.Width / (m_BoundMax - m_BoundMin);
					e.Graphics.DrawLine( m_GraduationPen, fPosition, this.Height, fPosition, .5f * this.Height );
				}
			}

			// Draw large graduations
			if ( m_bShowLargeGraduations )
			{
				int MinGraduationIndex = (int) Math.Floor( m_BoundMin / m_LargeGraduationSize );
				int MaxGraduationIndex = (int) Math.Floor( m_BoundMax / m_LargeGraduationSize );
				int	GraduationsCount = (1 + MaxGraduationIndex - MinGraduationIndex);
				int	GraduationsStep = 1;

				if ( GraduationsCount > m_MaxGraduations )
				{	// Make sure we always have less than the maximum requested number of graduations
					GraduationsStep = (int) Math.Ceiling( (float) GraduationsCount / m_MaxGraduations );
				}

				for ( int GraduationIndex=MinGraduationIndex; GraduationIndex <= MaxGraduationIndex; GraduationIndex+=GraduationsStep )
				{
					float	fPosition = (GraduationIndex * m_LargeGraduationSize - m_BoundMin) * this.Width / (m_BoundMax - m_BoundMin);
					e.Graphics.DrawLine( m_GraduationPenLarge, fPosition, this.Height, fPosition, 0.0f );
				}
			}

			// Draw some text choosing the appropriate scale based on the current bounds
			int		SmallGraduationsTextsCount = m_bShowSmallGraduations ? (int) ((m_BoundMax - m_BoundMin) / m_SmallGraduationSize) : int.MaxValue;
			int		MediumGraduationsTextsCount = m_bShowMediumGraduations ? (int) ((m_BoundMax - m_BoundMin) / m_MediumGraduationSize) : int.MaxValue;
			int		LargeGraduationsTextsCount = m_bShowLargeGraduations ? (int) ((m_BoundMax - m_BoundMin) / m_LargeGraduationSize) : int.MaxValue;

			int		MinDistance = Math.Abs( SmallGraduationsTextsCount - MAX_TEXTS_COUNT );
			int		MinTextsCount = SmallGraduationsTextsCount;
			float	fCandidateSize = m_SmallGraduationSize;
			if ( Math.Abs( MediumGraduationsTextsCount - MAX_TEXTS_COUNT ) < MinDistance )
			{
				MinDistance = Math.Abs( MediumGraduationsTextsCount - MAX_TEXTS_COUNT );
				MinTextsCount = MediumGraduationsTextsCount;
				fCandidateSize = m_MediumGraduationSize;
			}
			if ( Math.Abs( LargeGraduationsTextsCount - MAX_TEXTS_COUNT ) < MinDistance )
			{
				MinDistance = Math.Abs( LargeGraduationsTextsCount - MAX_TEXTS_COUNT );
				MinTextsCount = LargeGraduationsTextsCount;
				fCandidateSize = m_LargeGraduationSize;
			}

			if ( MinTextsCount > MAX_TEXTS_COUNT )
				fCandidateSize *= (float) Math.Ceiling( (float) MinTextsCount / MAX_TEXTS_COUNT );
			else if ( MinTextsCount > MAX_TEXTS_COUNT )
				fCandidateSize /= (float) Math.Floor( (float) MAX_TEXTS_COUNT / MinTextsCount );

			int		MinTextIndex = (int) Math.Ceiling( m_BoundMin / fCandidateSize );
			int		MaxTextIndex = (int) Math.Ceiling( m_BoundMax / fCandidateSize );

			for ( int TextIndex=MinTextIndex; TextIndex <= MaxTextIndex; TextIndex++ )
			{
				float	fValue = TextIndex * fCandidateSize;
				float	fPosition = (fValue - m_BoundMin) * this.Width / (m_BoundMax - m_BoundMin);

//				string	Text = fValue.ToString( "G4" );

				// Format as MM:ss:mmm
				int		Minutes = (int) Math.Floor( fValue / 60.0f );
				fValue -= Minutes * 60;
				int		Seconds = (int) Math.Floor( fValue );
				fValue -= Seconds;
				int		MilliSeconds = (int) Math.Floor( fValue * 1000.0f );
				string	Text = (Minutes > 0 ? Minutes.ToString() + ":" : "") + (Minutes > 0 ? Seconds.ToString( "D2" ) : Seconds.ToString()) + ":" + MilliSeconds.ToString( "D3" );

				SizeF	TextSize = e.Graphics.MeasureString( Text, Font );
				e.Graphics.DrawString( Text, Font, m_TextBrush, Math.Max( 0.0f, Math.Min( Width-TextSize.Width, fPosition - .5f * TextSize.Width ) ), 0.0f );
			}

			// Notify for custom painting
			if ( CustomPaint != null )
				CustomPaint( this, e );

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
