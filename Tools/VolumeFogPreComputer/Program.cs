using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace VolumeFogPreComputer
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
			Application.Run( new Form1() );

// 			Atmospheric.PhaseFunction	Phase = new Atmospheric.PhaseFunction();
// 			Phase.Init( Atmospheric.CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION, 5.0f * (float) Math.PI / 180.0f, (float) Math.PI, Atmospheric.CloudPhase.CloudPhaseFunction.MIE_PHASE_FUNCTION.Length );
// 			Application.Run( new Atmospheric.Helpers.PhasePolarPlotForm( Phase ) );
		}
	}
}
