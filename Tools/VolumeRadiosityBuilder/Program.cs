using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace VolumeRadiosityBuilder
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			VolumeRadiosityForm	F = new VolumeRadiosityForm();
							F.RunMessageLoop();
		}
	}
}
