using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TreeGloumibule
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// Override current culture to force it to US with their decimal numbers that use points instead of commas as in French...
			System.Globalization.CultureInfo	CultureOverride = new System.Globalization.CultureInfo( "en-US" );
			System.Threading.Thread.CurrentThread.CurrentCulture = CultureOverride;

			Application.SetUnhandledExceptionMode( UnhandledExceptionMode.ThrowException );
			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler( Application_ThreadException );
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler( CurrentDomain_UnhandledException );

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			TreeForm	F = new TreeForm();
						F.RunMessageLoop();
		}

		static void CurrentDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			ShowError( e.ExceptionObject as Exception );
		}

		static void Application_ThreadException( object sender, System.Threading.ThreadExceptionEventArgs _e )
		{
			ShowError( _e.Exception );
		}

		static void	ShowError( Exception _e )
		{
			string	ExceptionText = _e.GetType().FullName + " => ";

			Exception	Current = _e;
			while ( Current != null )
			{
				ExceptionText += Current.Message + "\r\n";
				Current = Current.InnerException;
			}
			ExceptionText += _e.StackTrace;

			MessageBox.Show( "An unhandled exception occurred while launching the program :\r\n\r\n" + ExceptionText, "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Error );
		}
	}
}
