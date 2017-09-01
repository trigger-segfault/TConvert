using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TConvert.Extract;
using TConvert.Windows;

namespace TConvert {
	/**<summary>The application class.</summary>*/
	public partial class App : Application {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The last exception. Used to prevent multiple error windows for the same error.</summary>*/
		private static object lastException = null;
		
		public static string AppDirectory {
			get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
		}

		#endregion
		//============= MAIN =============
		#region Main

		/*[STAThread]
		static void Main(string[] args) {
			if (args.Length > 0) {
				App app = new App();
				app.InitializeComponent();
				app.Run();
				CommandLine.ParseCommand(args);
			}
			if (args.Length == 0) {
				App.Current.MainWindow = new MainWindow();
				App.Current.MainWindow.Show();
			}
		}*/
		/*public new void Run() {
			string[] args = Environment.GetCommandLineArgs();
			commandLine = new Thread(() => {
				Thread.Sleep(100);
				CommandLine.ParseCommand(args);
			});
			if (args.Length > 0) {
				commandLine.Start();
			}
			base.Run();
			/*if (args.Length == 0) {
				MainWindow = new MainWindow();
				MainWindow.Show();
			}
		}*/

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnAppStartup(object sender, StartupEventArgs e) {
			// Catch exceptions not in a UI thread
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnAppDomainUnhandledException);
			TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
			if (e.Args.Length > 0) {
				//CommandLine.ParseCommand(e.Args);
				CommandLine.ProcessFiles();
			}
			else if (e.Args.Length > 0) {
				//this.MainWindow = new MainWindow();
				//this.MainWindow.Show();
			}
		}
		private void OnAppExit(object sender, ExitEventArgs e) {
			Ffmpeg.Cleanup();
			//Environment.Exit(Environment.ExitCode);
		}
		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			if (e.Exception != lastException) {
				lastException = e.Exception;
				if (ErrorMessageBox.Show(e.Exception))
					Environment.Exit(0);
				e.Handled = true;
			}
		}
		private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) {
			if (e.ExceptionObject != lastException) {
				lastException = e.ExceptionObject;
				Dispatcher.Invoke(() => {
					if (ErrorMessageBox.Show(e.ExceptionObject))
						Environment.Exit(0);
				});
			}
		}
		private void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
			if (e.Exception != lastException) {
				lastException = e.Exception;
				Dispatcher.Invoke(() => {
					if (ErrorMessageBox.Show(e.Exception))
						Environment.Exit(0);
				});
			}
		}

		#endregion
	}
}
