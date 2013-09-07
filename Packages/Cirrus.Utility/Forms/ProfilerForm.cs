using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Nuaj;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// A simple profiler that display the profile strings and times for the device (assuming a profiling is started every frame)
	/// Simply create an instance of the form and on a button event call "MyProfilerForm.Show( this );"
	/// </summary>
	public partial class ProfilerForm : Form
	{
		#region NESTED TYPES

		protected class		Entry
		{
			public ListViewItem	m_Item0;
			public ListViewItem	m_Item1;

			public double		m_DurationAccum = 0.0;
			public double		m_DurationRatioAccum = 0.0;
		}

		#endregion

		#region FIELDS

		protected Device		m_Device = null;

		protected Dictionary<Device.ProfileTaskInfos,Entry>	m_PTI2Entry = new Dictionary<Device.ProfileTaskInfos,Entry>();
		protected int			m_FramesCount = 0;
		protected DateTime		m_LastUpdate = DateTime.Now;

		#endregion

		#region PROPERTIES

		public bool		FlushEveryOnTask	{ get { return Visible && checkBoxFlush.Checked; } }

		#endregion

		#region METHODS

		public ProfilerForm( Device _Device )
		{
			InitializeComponent();

			m_Device = _Device;
			m_Device.ProfilingStopped += new EventHandler( Device_ProfilingStopped );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			e.Cancel = true;
			Hide();

			base.OnClosing( e );
		}

		#endregion

		#region EVENT HANDLERS

		protected void Device_ProfilingStopped( object sender, EventArgs e )
		{
			if ( !this.Visible || checkBoxPause.Checked )
				return;

			// Accumulate durations
			Device.ProfileTaskInfos	PTI = m_Device.ProfilingRootTask;
			while ( PTI != null )
			{
				if ( !m_PTI2Entry.ContainsKey( PTI ) )
				{
					Entry	NewEntry = new Entry();
					
					ListViewItem	Item = new ListViewItem( PTI.Source != null ? PTI.Source.Name : "<ANONYMOUS>" );
					Item.SubItems.Add( PTI.Category );
					Item.SubItems.Add( "" );
					Item.SubItems.Add( "" );
					listView.Items.Add( Item );
					NewEntry.m_Item0 = Item;

					Item = new ListViewItem( "" );
					Item.SubItems.Add( PTI.InfoString );
					Item.SubItems.Add( "" );
					Item.SubItems.Add( "" );
					listView.Items.Add( Item );
					NewEntry.m_Item1 = Item;

					m_PTI2Entry[PTI] = NewEntry;
				}

				// Accumulate durations
				Entry	E = m_PTI2Entry[PTI];
				E.m_DurationAccum += PTI.Duration;
				E.m_DurationRatioAccum += PTI.DurationRatio;

				PTI = PTI.Next;
			}

			m_FramesCount++;

			DateTime	Now = DateTime.Now;
			if ( m_FramesCount < trackBarDelay.Value || (Now - m_LastUpdate).TotalSeconds < 1.0f )
				return;	// Not enough frame or update is too fast

			// Update display
			listView.BeginUpdate();

			double	DurationAccum = 0.0;
			double	DurationRatioAccum = 0.0;

			PTI = m_Device.ProfilingRootTask;
			while ( PTI != null )
			{
				Entry	E = m_PTI2Entry[PTI];

				// Average durations & reset accumulators
				double	Duration = E.m_DurationAccum / m_FramesCount;
				double	DurationRatio = E.m_DurationRatioAccum / m_FramesCount;

				E.m_DurationAccum = 0.0;
				E.m_DurationRatioAccum = 0.0;

				// Update items
				E.m_Item0.BackColor = Duration < 2.0 ? Color.PowderBlue : (Duration < 5.0 ? Color.IndianRed : (Duration < 10.0 ? Color.DarkOrange : Color.Red));
				E.m_Item0.SubItems[2].Text = Duration.ToString( "G4" ) + " ms";
				E.m_Item0.SubItems[3].Text = DurationAccum.ToString( "G4" ) + " ms";

				E.m_Item1.SubItems[2].Text = (DurationRatio * 100.0).ToString( "G4" ) + " %";
				E.m_Item1.SubItems[3].Text = (DurationRatioAccum * 100.0).ToString( "G4" ) + " %";

				// Accumulate
				DurationAccum += Duration;
				DurationRatioAccum += DurationRatio;

				PTI = PTI.Next;
			}

			listView.EndUpdate();

			// Reset count
			m_FramesCount = 0;
			m_LastUpdate = DateTime.Now;
		}

		#endregion
	}
}
