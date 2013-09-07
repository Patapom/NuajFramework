using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.Windows.Forms;

namespace SequencorEditor
{
	public partial class PanelNoMouseWheel : Panel
	{
		const int WM_MOUSEWHEEL = 0x020A;
		const int WS_VSCROLL = 0x00200000;

		public PanelNoMouseWheel( IContainer container )
		{
			container.Add( this );

			InitializeComponent();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams	Result = base.CreateParams;
				Result.Style |= WS_VSCROLL;	// Force vertical scrollbar to show
				return Result;
			}
		}

		protected override void WndProc(ref Message m)
		{
			if ( m.Msg == WM_MOUSEWHEEL )
			{
				// Ignore mouse wheel on the panel itself...
				FoldableTrackControl	FoldableTrack = GetChildAtPoint( PointToClient( Control.MousePosition ) ) as FoldableTrackControl;
				if ( FoldableTrack != null )
				{
					TrackControl	TC = FoldableTrack.GetChildAtPoint( FoldableTrack.PointToClient( Control.MousePosition ) ) as TrackControl;
					if ( TC != null )
					{
						TrackIntervalPanel	TIC = TC.GetChildAtPoint( TC.PointToClient( Control.MousePosition ) ) as TrackIntervalPanel;
						if ( TIC != null )
						{	// Forward to child control...
							m.HWnd = TIC.Handle;
							return;
						}
					}
					AnimationEditorControl	AEC = FoldableTrack.GetChildAtPoint( FoldableTrack.PointToClient( Control.MousePosition ) ) as AnimationEditorControl;
					if ( AEC != null )
					{
						AnimationTrackPanel	ATP = AEC.GetChildAtPoint( AEC.PointToClient( Control.MousePosition ) ) as AnimationTrackPanel;
						GradientTrackPanel	GTP = AEC.GetChildAtPoint( AEC.PointToClient( Control.MousePosition ) ) as GradientTrackPanel;
						if ( ATP != null || GTP != null )
						{	// Forward to child control...
							m.HWnd = ATP.Handle;
							return;
						}
					}
				}
			}

			base.WndProc( ref m );
		}
	}
}
