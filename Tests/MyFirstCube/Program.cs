using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MyFirstCube
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

			Form1	F = new Form1();
					F.RunMessageLoop();
		}
	}
}
