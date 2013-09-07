using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Demo
{
	public partial class ShadowMapViewForm : Form
	{
		protected RenderTechniqueVolumeClouds	m_Clouds = null;

		public RenderTechniqueVolumeClouds	Clouds
		{
			get { return m_Clouds; }
			set
			{
				if ( m_Clouds != null )
					m_Clouds.DEBUGEventRefreshShadow -= new EventHandler( Clouds_DEBUGEventRefreshShadow );

				m_Clouds = value;
				shadowMapOutputPanel.m_Clouds = value;

				if ( m_Clouds != null )
					m_Clouds.DEBUGEventRefreshShadow += new EventHandler( Clouds_DEBUGEventRefreshShadow );
			}
		}

		public ShadowMapViewForm()
		{
			InitializeComponent();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			e.Cancel = true;
			Visible = false;	// Hide instead...
			base.OnClosing( e );
		}

		private void shadowMapOutputPanel_MouseDown( object sender, MouseEventArgs e )
		{
			if ( m_Clouds == null )
				return;

			shadowMapOutputPanel.m_P = shadowMapOutputPanel.TransformInverse( e.Location );
			shadowMapOutputPanel.m_UV = m_Clouds.ShadowQuad2UV( shadowMapOutputPanel.m_P );
			shadowMapOutputPanel.UpdateBitmap();
		}

		protected DateTime	m_LastRefresh = DateTime.Now;
		void Clouds_DEBUGEventRefreshShadow( object sender, EventArgs e )
		{
			if ( !Visible )
				return;

			DateTime	Now = DateTime.Now;
			if ( (Now - m_LastRefresh).TotalMilliseconds < 30 )
				return;	// Don't refresh too often !

			m_LastRefresh = Now;
			shadowMapOutputPanel.UpdateBitmap();
		}
	}
}
