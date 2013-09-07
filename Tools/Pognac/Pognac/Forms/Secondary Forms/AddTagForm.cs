using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pognac
{
	public partial class AddTagForm : Form
	{
		#region PROPERTIES

		public Documents.Tag	SelectedTag
		{
			get { return listBoxTags.SelectedItem as Documents.Tag; }
		}

		#endregion

		#region METHODS

		public AddTagForm( Control _PositionControl, Documents.Database _Database )
		{
			InitializeComponent();

			// Pop below the source control
			this.Location = _PositionControl.PointToScreen( new Point( 0, _PositionControl.Height ) );

			// Populate the list
			listBoxTags.BeginUpdate();
			foreach ( Documents.Tag Tag in _Database.Tags )
				listBoxTags.Items.Add( Tag );
			listBoxTags.EndUpdate();
		}

		protected override void OnDeactivate( EventArgs e )
		{
			base.OnDeactivate( e );
		
			// Closes form if clicking outside...
			DialogResult = DialogResult.Cancel;
			Close();
		}

		protected override bool ProcessKeyPreview( ref Message m )
		{
			if ( m.Msg == 0x100 && m.WParam.ToInt32() == (int) Keys.Escape )	// WM_KEYDOWN
				OnDeactivate( EventArgs.Empty );

			return base.ProcessKeyPreview( ref m );
		}

		#region Modeless DropDown Helper

		protected static AddTagForm	ms_DropDownInstance = null;
		public static void	ShowDropDown( Form _Owner, Control _PositionControl, Documents.Database _Database, EventHandler _DropDownClosed )
		{
			if ( ms_DropDownInstance == null )
			{	// Create the modeless form
				ms_DropDownInstance = new AddTagForm( _PositionControl, _Database );
				ms_DropDownInstance.FormClosed += new FormClosedEventHandler(
					( object sender, FormClosedEventArgs e ) =>
					{
						// Notify
						if ( ms_DropDownInstance.DialogResult == DialogResult.OK && ms_DropDownInstance.SelectedTag != null )
							_DropDownClosed( sender, e );

						// Dispose
						ms_DropDownInstance.Dispose();
						ms_DropDownInstance = null;
					} );
				ms_DropDownInstance.Show( _Owner );
			}
			else
			{	// Dispose of the modeless form
				ms_DropDownInstance.DialogResult = DialogResult.Cancel;
				ms_DropDownInstance.Close();
			}
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		private void listBoxTags_SelectedIndexChanged( object sender, EventArgs e )
		{
			if ( listBoxTags.SelectedItem == null )
				return;

			DialogResult = DialogResult.OK;
			Close();
		}

		#endregion
	}
}
