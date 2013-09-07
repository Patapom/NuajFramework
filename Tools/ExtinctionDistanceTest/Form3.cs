using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SharpDX;

namespace ExtinctionDistanceTest
{
	/// <summary>
	/// This little app lets you test the filmic tone mapping operator
	/// </summary>
	public partial class Form3 : Form
	{
		#region CONSTANTS

		protected const string	ROOT_KEY_NAME = @"Software\Patapom\Nuaj\DemoCloud2";

		#endregion

		#region FIELDS

		protected Microsoft.Win32.RegistryKey	m_ROOT = null;

		#endregion

		#region METHODS

		public Form3()
		{
			InitializeComponent();
			m_ROOT = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( ROOT_KEY_NAME );
		}

		protected unsafe override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

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
		}

		#endregion
	}
}
