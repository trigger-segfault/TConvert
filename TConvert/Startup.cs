using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TConvert {
	static class Startup {
		//============= MAIN =============
		#region Main

		[STAThread]
		static void Main(string[] args) {

#if !(CONSOLE)
			if (args.Length > 0) {
				AttachConsole(-1);
				CommandLine.ParseCommand(args);
			}
			if (args.Length == 0) {
				App app = new App();
				app.InitializeComponent();
				app.Run(new MainWindow());
			}
#else
			CommandLine.ParseCommand(args);
#endif
		}

		[DllImport("kernel32.dll")]
		private static extern bool AttachConsole(int pid);

		#endregion
	}
}
