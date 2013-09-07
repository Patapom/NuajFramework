using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Demo
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

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			DemoForm	F = new DemoForm();
						F.RunMessageLoop();
		}
	}
}
