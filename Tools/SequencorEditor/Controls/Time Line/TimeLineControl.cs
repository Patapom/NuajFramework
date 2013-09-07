using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SequencorEditor
{
	public partial class TimeLineControl : UserControl
	{
		#region CONSTANTS

		protected const float		WHEEL_ZOOM_FACTOR	= 1.0f;

		#endregion

		#region NESTED TYPES

		public delegate void	CursorMovedEventHandler( TimeLineControl _Sender );

		public delegate void	VisibleRangeChangedEventHandler( TimeLineControl _Sender );

		public delegate void	CustomPaintEventHandler( GraduationPanel _Panel, PaintEventArgs _e );

		public delegate bool	CustomMouseDownEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool	CustomMouseMoveEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool	CustomMouseUpEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool	CustomMouseHoverEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool	CustomMouseDoubleClickEventHandler( GraduationPanel _Panel, MouseEventArgs _e );

		public delegate bool	CustomKeyDownEventHandler( GraduationPanel _Panel, KeyEventArgs _e );

		#endregion

		#region FIELDS

		// The absolute bounds that the user can't escape
		protected float		m_BoundMin = 0.0f;
		protected float		m_BoundMax = 1.0f;

		// The visible bounds the user can see (always within the absolute bounds)
		protected float		m_VisibleBoundMin = 0.0f;
		protected float		m_VisibleBoundMax = 1.0f;

		// Cursor manipulation
		protected bool		m_bButtonDown = false;
		protected float		m_ButtonDownPosition = 0.0f;
		protected float		m_ButtonDownVisibleBoundMin = 0.0f;
		protected float		m_ButtonDownVisibleBoundMax = 0.0f;
		protected bool		m_bScroll = false;
		protected float		m_ScrollSpeed = 0.0f;

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Category( "Range Absolute" )]
		public float		BoundMin
		{
			get { return m_BoundMin; }
			set { SetRange( value, m_BoundMax ); }
		}

		[System.ComponentModel.Category( "Range Absolute" )]
		public float		BoundMax
		{
			get { return m_BoundMax; }
			set { SetRange( m_BoundMin, value ); }
		}

		[System.ComponentModel.Category( "Range Visible" )]
		public float		VisibleBoundMin
		{
			get { return m_VisibleBoundMin; }
			set
			{
				m_VisibleBoundMin = Math.Min( m_BoundMax, Math.Max( m_BoundMin, value ) );

				// Update controls
				panelGraduation.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );
				panelCursor.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );

				if ( VisibleRangeChanged != null )
					VisibleRangeChanged( this );
			}
		}

		[System.ComponentModel.Category( "Range Visible" )]
		public float		VisibleBoundMax
		{
			get { return m_VisibleBoundMax; }
			set
			{
				m_VisibleBoundMax = Math.Min( m_BoundMax, Math.Max( m_BoundMin, value ) );

				// Update controls
				panelGraduation.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );
				panelCursor.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );

				if ( VisibleRangeChanged != null )
					VisibleRangeChanged( this );
			}
		}

		[System.ComponentModel.Category( "Range Visible" )]
		public float		CursorPosition
		{
			get { return panelCursor.CursorPosition; }
			set
			{
				MoveCursorTo( value );
				Refresh();
			}
		}

		[System.ComponentModel.Category( "Graduations" )]
		public event CursorMovedEventHandler			CursorMoved;

		[System.ComponentModel.Category( "Graduations" )]
		public event VisibleRangeChangedEventHandler	VisibleRangeChanged;

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
		public event CustomPaintEventHandler			CustomGraduationPaint;

		#endregion

		#region METHODS

		public TimeLineControl()
		{
			InitializeComponent();
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

			SetVisibleRange( m_VisibleBoundMin, m_VisibleBoundMax );
		}

		/// <summary>
		/// Sets both min and max bounds with a single update
		/// </summary>
		/// <param name="_BoundMin">The new min bound</param>
		/// <param name="_BoundMax">The new max bound</param>
		public void		SetVisibleRange( float _BoundMin, float _BoundMax )
		{
			panelCursor.SuspendLayout();
			panelGraduation.SuspendLayout();

			m_VisibleBoundMin = Math.Min( m_BoundMax, Math.Max( m_BoundMin, _BoundMin ) );
			m_VisibleBoundMax = Math.Min( m_BoundMax, Math.Max( m_BoundMin, _BoundMax ) );

			panelGraduation.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );
			panelCursor.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );

			panelCursor.ResumeLayout( true );
			panelGraduation.ResumeLayout( true );

			if ( VisibleRangeChanged != null )
				VisibleRangeChanged( this );
		}

		public void		SetAllRanges( float _BoundMin, float _BoundMax, float _fCursor )
		{
			m_BoundMin = m_VisibleBoundMin = _BoundMin;
			m_BoundMax = m_VisibleBoundMax = _BoundMax;
			
			SetVisibleRange( m_VisibleBoundMin, m_VisibleBoundMax );
			CursorPosition = _fCursor;
		}

		/// <summary>
		/// Moves the cursor to the start
		/// </summary>
		public void		MoveCursorToStart()
		{
			panelCursor.SuspendLayout();
			panelGraduation.SuspendLayout();

			float	fVisibleRange = m_VisibleBoundMax - m_VisibleBoundMin;
			m_VisibleBoundMin = m_BoundMin;
			m_VisibleBoundMax = m_BoundMin + fVisibleRange;

			panelGraduation.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );
			panelCursor.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );

			panelCursor.ResumeLayout( true );
			panelGraduation.ResumeLayout( true );

			if ( VisibleRangeChanged != null )
				VisibleRangeChanged( this );

			panelCursor.CursorPosition = m_BoundMin;
		}

		/// <summary>
		/// Moves the cursor to the specified position
		/// </summary>
		/// <param name="_fTime">The time at which to move the cursor to</param>
		/// <remarks>This method is different from the "CursorPosition" set accessor as the
		/// "CursorPosition" property will clamp the cursor to the visible range whereas this
		/// method will change the visible range so the requested cursor position stays within bounds</remarks>
		public void		MoveCursorTo( float _fTime )
		{
			_fTime = Math.Max( m_BoundMin, Math.Min( m_BoundMax, _fTime ) );

			if ( _fTime >= m_VisibleBoundMin && _fTime <= m_VisibleBoundMax )
			{	// No sweat : the cursor is still in visible range!
				panelCursor.CursorPosition = _fTime;
				return;
			}

			// We keep the same visible range but make it move with the cursor in the middle
			panelCursor.SuspendLayout();
			panelGraduation.SuspendLayout();

			float	fVisibleRange = m_VisibleBoundMax - m_VisibleBoundMin;
			if ( _fTime - 0.5f * fVisibleRange < m_BoundMin )
			{
				m_VisibleBoundMin = m_BoundMin;
				m_VisibleBoundMax = m_BoundMin + fVisibleRange;
			}
			else if ( _fTime + 0.5f * fVisibleRange > m_BoundMax )
			{
				m_VisibleBoundMax = m_BoundMax;
				m_VisibleBoundMin = m_BoundMax - fVisibleRange;
			}
			else
			{
				m_VisibleBoundMin = Math.Max( m_BoundMin, _fTime - .5f * fVisibleRange );
				m_VisibleBoundMax = Math.Min( m_BoundMax, _fTime + .5f * fVisibleRange );
			}

			panelGraduation.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );
			panelCursor.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );

			panelCursor.ResumeLayout( true );
			panelGraduation.ResumeLayout( true );

			if ( VisibleRangeChanged != null )
				VisibleRangeChanged( this );

			panelCursor.CursorPosition = _fTime;
		}

		/// <summary>
		/// Moves the cursor to the end
		/// </summary>
		public void		MoveCursorToEnd()
		{
			panelCursor.SuspendLayout();
			panelGraduation.SuspendLayout();

			float	fVisibleRange = m_VisibleBoundMax - m_VisibleBoundMin;
			m_VisibleBoundMax = m_BoundMax;
			m_VisibleBoundMin = m_BoundMax - fVisibleRange;

			panelGraduation.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );
			panelCursor.SetRange( m_VisibleBoundMin, m_VisibleBoundMax );

			panelCursor.ResumeLayout( true );
			panelGraduation.ResumeLayout( true );

			if ( VisibleRangeChanged != null )
				VisibleRangeChanged( this );

			panelCursor.CursorPosition = m_BoundMax;
		}

		/// <summary>
		/// Helper that converts a CLIENT SPACE position into a graduation position
		/// </summary>
		/// <param name="_ClientPosition">The client position to convert</param>
		/// <returns>The position in graduation space</returns>
		public float	ClientToGraduation( int _ClientPosition )
		{
			return	panelGraduation.ClientToGraduation( _ClientPosition );
		}

		/// <summary>
		/// Helper that converts a GRADUATION SPACE position into a client position
		/// </summary>
		/// <param name="_fGraduationPosition">The graduation position to convert</param>
		/// <returns>The position in client space</returns>
		public float	GraduationToClient( float _fGraduationPosition )
		{
			return	panelGraduation.GraduationToClient( _fGraduationPosition );
		}

		protected bool	m_bSimulated = false;
		public void		SimulateMouseDown( MouseEventArgs _e )
		{
			m_bSimulated = true;
			panelGraduation.SimulateMouseDown( _e );
			m_bSimulated = false;
		}

		public void		SimulateMouseMove( MouseEventArgs _e )
		{
			m_bSimulated = true;
			panelGraduation.SimulateMouseMove( _e );
			m_bSimulated = false;
		}

		public void		SimulateMouseUp( MouseEventArgs _e )
		{
			m_bSimulated = true;
			panelGraduation.SimulateMouseUp( _e );
			m_bSimulated = false;
		}

		public void		SimulateMouseWheel( MouseEventArgs _e )
		{
			m_bSimulated = true;
			OnMouseWheel( _e );
			m_bSimulated = false;
		}

		#region Control members


		public override void Refresh()
		{
			base.Refresh();

			panelCursor.Refresh();
			panelGraduation.Refresh();
		}

		protected override void OnMouseWheel( MouseEventArgs e )
		{
			base.OnMouseWheel( e );

			// Zoom in & out
			float	fZoomCenterPosition = m_VisibleBoundMin + e.X * (m_VisibleBoundMax - m_VisibleBoundMin) / Width;

			float	fZoomFactor = 1.0f;
			if ( e.Delta > 0 )
				fZoomFactor = 1.0f / (1.0f + WHEEL_ZOOM_FACTOR * e.Delta * 0.0005f);
			else
				fZoomFactor = 1.0f - WHEEL_ZOOM_FACTOR * e.Delta * 0.0005f;

			float	fNewBoundMin = fZoomCenterPosition - Math.Max( 1e-3f, (fZoomCenterPosition - m_VisibleBoundMin) * fZoomFactor );
			float	fNewBoundMax = fZoomCenterPosition + Math.Max( 1e-3f, (m_VisibleBoundMax - fZoomCenterPosition) * fZoomFactor );

			SetVisibleRange( fNewBoundMin, fNewBoundMax );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		//////////////////////////////////////////////////////////////////////////
		// Graduation scrolling

		private void panelGraduation_MouseDown( object sender, MouseEventArgs e )
		{
			this.Focus();

			if ( (e.Button & MouseButtons.Left) == 0 )
				return;

			m_bButtonDown = true;
			m_bScroll = false;
			m_ButtonDownPosition = m_VisibleBoundMin + e.X * (m_VisibleBoundMax - m_VisibleBoundMin) / Width;
			m_ButtonDownVisibleBoundMin = m_VisibleBoundMin;
			m_ButtonDownVisibleBoundMax = m_VisibleBoundMax;
			if ( !m_bSimulated )
				panelGraduation.Capture = true;
			timerScroll.Enabled = true;
		}

		private void panelGraduation_MouseUp( object sender, MouseEventArgs e )
		{
// 			if ( (e.Button & MouseButtons.Left) == 0 )
// 				return;

			m_bButtonDown = false;
			panelGraduation.Capture = false;
			timerScroll.Enabled = false;
		}

		private void panelGraduation_MouseMove( object sender, MouseEventArgs e )
		{
			if ( !m_bButtonDown )
				return;

			// Check we're within the horizontal bounds of the control
			m_bScroll = false;
			m_ScrollSpeed = 0.0f;
			if ( e.X < 0 || e.X >= Width )
			{
				m_bScroll = true;
				m_ScrollSpeed = m_BoundMax - m_BoundMin;
				if ( e.X < 0 )
				{	// Scroll left
					m_ScrollSpeed = -e.X * 0.0001f;
				}
				else
				{	// Scroll right
					m_ScrollSpeed = -(e.X - Width) * 0.0001f;
				}
			}
			else
			{	// Normal cursor setting within the current range
				float	fCurrentPosition = m_ButtonDownVisibleBoundMin + e.X * (m_ButtonDownVisibleBoundMax - m_ButtonDownVisibleBoundMin) / Width;
				if ( fCurrentPosition < m_ButtonDownPosition )
				{	// Scroll left
					float fNewVisibleBoundMin = Math.Max( m_BoundMin, m_ButtonDownVisibleBoundMin + m_ButtonDownPosition - fCurrentPosition );
					SetVisibleRange( fNewVisibleBoundMin, fNewVisibleBoundMin + m_ButtonDownVisibleBoundMax - m_ButtonDownVisibleBoundMin );
				}
				else
				{	// Scroll right
					float fNewVisibleBoundMax = Math.Min( m_BoundMax, m_ButtonDownVisibleBoundMax + m_ButtonDownPosition - fCurrentPosition );
					SetVisibleRange( fNewVisibleBoundMax + m_ButtonDownVisibleBoundMin - m_ButtonDownVisibleBoundMax, fNewVisibleBoundMax );
				}
			}
		}
		
		//////////////////////////////////////////////////////////////////////////
		// Graduation custom events

		private bool panelGraduation_CustomMouseDown( GraduationPanel _Panel, MouseEventArgs _e )
		{
			if ( CustomMouseDown == null )
				return	false;

			return CustomMouseDown( _Panel, _e );
		}

		private bool panelGraduation_CustomMouseHover( GraduationPanel _Panel, MouseEventArgs _e )
		{
			if ( CustomMouseHover == null )
				return	false;

			return CustomMouseHover( _Panel, _e );
		}

		private bool panelGraduation_CustomMouseMove( GraduationPanel _Panel, MouseEventArgs _e )
		{
			if ( CustomMouseMove == null )
				return	false;

			return CustomMouseMove( _Panel, _e );
		}

		private bool panelGraduation_CustomMouseUp( GraduationPanel _Panel, MouseEventArgs _e )
		{
			if ( CustomMouseUp == null )
				return	false;

			return CustomMouseUp( _Panel, _e );
		}

		private bool panelGraduation_CustomMouseDoubleClick( GraduationPanel _Panel, MouseEventArgs _e )
		{
			if ( CustomMouseDoubleClick == null )
				return	false;

			return CustomMouseDoubleClick( _Panel, _e );
		}

		private bool panelGraduation_CustomKeyDown( GraduationPanel _Panel, KeyEventArgs _e )
		{
			if ( CustomKeyDown == null )
				return	false;

			return	CustomKeyDown( _Panel, _e );
		}

		private void panelGraduation_CustomPaint( GraduationPanel _Panel, PaintEventArgs _e )
		{
			// Forward the event
			if ( CustomGraduationPaint != null )
				CustomGraduationPaint( _Panel, _e );
		}

		//////////////////////////////////////////////////////////////////////////
		// Cursor scrolling

		private void panelCursor_CursorMoved( CursorPanel _Sender )
		{
			// Forward
			if ( CursorMoved != null )
				CursorMoved( this );
		}

		private void panelCursor_MouseDown( object sender, MouseEventArgs e )
		{
			this.Focus();

			if ( (e.Button & MouseButtons.Left) == 0 )
				return;

			m_bButtonDown = true;
			m_bScroll = false;
			if ( !m_bSimulated )
				panelCursor.Capture = true;
			timerScroll.Enabled = true;

			panelCursor_MouseMove( sender, e );
		}

		private void panelCursor_MouseUp( object sender, MouseEventArgs e )
		{
			if ( (e.Button & MouseButtons.Left) == 0 )
				return;

			m_bButtonDown = false;
			if ( !m_bSimulated )
				panelCursor.Capture = false;
			timerScroll.Enabled = false;
		}

		private void panelCursor_MouseMove( object sender, MouseEventArgs e )
		{
			if ( !m_bButtonDown )
				return;

			// Check we're within the horizontal bounds of the control
			m_bScroll = false;
			m_ScrollSpeed = 0.0f;
			if ( e.X < 0 || e.X >= Width )
			{
				m_bScroll = true;
				m_ScrollSpeed = m_BoundMax - m_BoundMin;
				if ( e.X < 0 )
				{	// Scroll left
					m_ScrollSpeed = e.X * 0.0001f;
				}
				else
				{	// Scroll right
					m_ScrollSpeed = (e.X - Width) * 0.0001f;
				}
			}

			panelCursor.SetClientCursorPosition( e.X );
		}

		//////////////////////////////////////////////////////////////////////////
		// Timer scrolling

		private void timerScroll_Tick( object sender, EventArgs e )
		{
			if ( !m_bButtonDown || !m_bScroll )
				return;	// Don't scroll

			float	fScrollSpeedFactor = (float) timerScroll.Interval / 25.0f;	// Since we set the scrolling speed based on a 25ms timer, if the delay changes, the speed stays the same

			if ( m_ScrollSpeed < 0.0f )
			{	// Scroll left
				float	fTargetMinBound = Math.Max( m_BoundMin, m_VisibleBoundMin + fScrollSpeedFactor * m_ScrollSpeed );
				m_ButtonDownPosition += m_ScrollSpeed;
				SetVisibleRange( fTargetMinBound, fTargetMinBound + m_VisibleBoundMax - m_VisibleBoundMin );
			}
			else
			{	// Scroll right
				float	fTargetMaxBound = Math.Min( m_BoundMax, m_VisibleBoundMax + fScrollSpeedFactor * m_ScrollSpeed );
				m_ButtonDownPosition += m_ScrollSpeed;
				SetVisibleRange( fTargetMaxBound + m_VisibleBoundMin - m_VisibleBoundMax, fTargetMaxBound );
			}

			// Send a fake mouse move to update cursor stuff
			Point	P = panelCursor.PointToClient( Control.MousePosition );
			panelCursor.SetClientCursorPosition( P.X );
		}

		#endregion
	}
}
