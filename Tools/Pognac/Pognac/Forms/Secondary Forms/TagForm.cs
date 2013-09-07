using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;

namespace Pognac
{
	public partial class TagForm : Form
	{
		#region FIELDS

		protected Documents.Tag		m_Tag = null;
		protected XmlDocument		m_Original = null;

		#endregion

		#region PROPERTIES

		public new Documents.Tag	Tag
		{
			get { return m_Tag; }
			set
			{
				if ( value == m_Tag )
					return;

				if ( m_Tag != null )
					m_Tag.NameChanged += new EventHandler( Tag_NameChanged );

				m_Tag = value;

				if ( m_Tag != null )
				{
					m_Tag.NameChanged += new EventHandler( Tag_NameChanged );

					// Perform a backup in case of cancel
					m_Original = new XmlDocument();
					XmlElement	Root = m_Original.CreateElement( "ROOT" );
					m_Original.AppendChild( Root );
					m_Tag.Save( Root );
				}

				// Update GUI
				Tag_NameChanged( m_Tag, EventArgs.Empty );
			}
		}

		#endregion

		#region METHODS

		public TagForm()
		{
			InitializeComponent();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			base.OnClosing( e );

			if ( DialogResult == DialogResult.OK )
				return;	// Validate changes...

			// Restore former tag
			m_Tag.Load( m_Original["ROOT"]["Tag"] );
		}

		#endregion

		#region EVENT HANDLERS

		void Tag_NameChanged( object sender, EventArgs e )
		{
			textBoxTagName.Text = m_Tag != null ? m_Tag.Name : "";
		}

		private void textBoxTagName_TextChanged( object sender, EventArgs e )
		{
			if ( m_Tag != null )
				m_Tag.Name = textBoxTagName.Text;
			buttonSave.Enabled = textBoxTagName.Text != "";
		}

		private void buttonSave_Click( object sender, EventArgs e )
		{
			DialogResult = DialogResult.OK;
		}

		#endregion
	}
}
