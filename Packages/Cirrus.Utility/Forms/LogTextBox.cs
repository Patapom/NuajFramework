﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nuaj.Cirrus.Utility
{
	public partial class LogTextBox : RichTextBox
	{
		StringBuilder	m_Log = new StringBuilder();

		public LogTextBox()
		{
			InitializeComponent();
		}

		public LogTextBox( IContainer container )
		{
			container.Add( this );

			InitializeComponent();
		}

		public void		Log( string _Text )
		{
			_Text = _Text.Replace( "\n", @"\line" );
			m_Log.Append( @"\cf1 " + _Text );
			UpdateRTF();
		}

		public void		LogSuccess( string _Text )
		{
			_Text = _Text.Replace( "\n", @"\line" );
			m_Log.Append( @"\cf2 " + _Text );
			UpdateRTF();
		}

		public void		LogWarning( string _Text )
		{
			_Text = _Text.Replace( "\n", @"\line" );
			m_Log.Append( @"\cf3 " + _Text );
			UpdateRTF();
		}

		public void		LogError( string _Text )
		{
			_Text = _Text.Replace( "\n", @"\line" );
			m_Log.Append( @"\cf4 " + _Text );
			UpdateRTF();
		}

		public void		LogSceneTextureProvider( SceneTextureProvider _TextureProvider )
		{
			Log( "Texture Provider :\n" );
			Log( "> " + _TextureProvider.LoadedTexturesCount + " textures loaded.\n" );
			int	MinSize = (int) Math.Sqrt( _TextureProvider.MinTextureSurface );
			int	MaxSize = (int) Math.Sqrt( _TextureProvider.MaxTextureSurface );
			int	AvgSize = (int) Math.Sqrt( _TextureProvider.AverageTextureSurface );
			int	TotalSize = (int) Math.Sqrt( _TextureProvider.TotalTextureSurface );
			Log( "> Surface Min=" + MinSize + "x" + MinSize + " Max=" + MaxSize + "x" + MaxSize + " Avg=" + AvgSize + "x" + AvgSize + "\n" );
			LogWarning( "> Surface Total=" + TotalSize + "x" + TotalSize + " (Memory=" + (_TextureProvider.TotalTextureMemory>>10) + " Kb)\n" );

			if ( _TextureProvider.HasErrors )
			{	// Display errors
				Log( "The texture provider has some errors !\r\n\r\n" );
				foreach ( string Error in _TextureProvider.TextureErrors )
					LogError( "   ●  " + Error + "\r\n" );
			}
			Log( "------------------------------------------------------------------\r\n\r\n" );
		}

		protected void	UpdateRTF()
		{
			string RTFText = @"{\rtf1\ansi\deff0" +
			@"{\colortbl;\red0\green0\blue0;\red32\green128\blue32;\red192\green128\blue0;\red255\green0\blue0;}" +
			m_Log.ToString() + "}";

			this.Rtf = RTFText;
			SelectionStart = this.Rtf.Length;
			this.ScrollToCaret();
		}
	}
}