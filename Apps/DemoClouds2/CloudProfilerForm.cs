using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;

namespace Demo
{
	public partial class CloudProfilerForm : Form
	{
		#region CONSTANTS

		protected const string	ROOT_KEY_NAME = @"Software\Patapom\Nuaj\DemoCloud2";

		#endregion

		#region FIELDS

		protected Microsoft.Win32.RegistryKey	m_ROOT = null;
		protected RenderTechniqueVolumeClouds	m_Clouds = null;

		#endregion

		#region PROPERTIES

		public RenderTechniqueVolumeClouds	Clouds
		{
			get { return m_Clouds; }
			set
			{
				m_Clouds = value;
				RebuildProfileTexture();
			}
		}

		#endregion

		#region METHODS

		public CloudProfilerForm()
		{
			InitializeComponent();
			m_ROOT = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( ROOT_KEY_NAME );

			// Reload curve settings
			int		ControlPointsCount;
			if ( !int.TryParse( m_ROOT.GetValue( "ControlPointsCount", "" ) as string, out ControlPointsCount ) )
			{
				panelOutput.ControlPointsCount = integerTrackbarControlControlPointsCount.Value;
				return;
			}

			panelOutput.ControlPointsCount = ControlPointsCount;
			for ( int i=0; i < ControlPointsCount; i++ )
			{
				Vector2	Value = new Vector2( 0.0f, (float) i / (ControlPointsCount-1) );
				float.TryParse( m_ROOT.GetValue( "ControlPoint" + i + "_X" ) as string, out Value.X );
				float.TryParse( m_ROOT.GetValue( "ControlPoint" + i + "_Y" ) as string, out Value.Y );
				panelOutput.m_Points[i] = Value;
			}
			panelOutput.UpdateBitmap();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			e.Cancel = true;
			Visible = false;
			base.OnClosing( e );
		}

		public void	RebuildProfileTexture()
		{
			if ( m_Clouds == null )
				return;

			m_Clouds.BuildCloudProfile( ( float y ) =>
			{
				return Math.Max( 0.0f, Math.Min( 1.0f, panelOutput.ComputePolynomial( y ) ) );
			} );
		}

		#endregion

		#region EVENT HANDLERS

		private void integerTrackbarControlControlPointsCount_ValueChanged( Nuaj.Cirrus.Utility.IntegerTrackbarControl _Sender, int _FormerValue )
		{
			panelOutput.ControlPointsCount = _Sender.Value;
		}

		private void buttonBuild_Click( object sender, EventArgs e )
		{
			// Save curve settings
			m_ROOT.SetValue( "ControlPointsCount", panelOutput.ControlPointsCount.ToString() );
			for ( int i=0; i < panelOutput.ControlPointsCount; i++ )
			{
				m_ROOT.SetValue( "ControlPoint" + i + "_X", panelOutput.m_Points[i].X.ToString() );
				m_ROOT.SetValue( "ControlPoint" + i + "_Y", panelOutput.m_Points[i].Y.ToString() );
			}

			// Rebuild texture
			RebuildProfileTexture();
		}

		#endregion
	}
}
