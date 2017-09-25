using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
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

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the app and sets up embedded assembly resolving.</summary>*/
		public App() {
			AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssemblies;
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private Assembly OnResolveAssemblies(object sender, ResolveEventArgs args) {
			var executingAssembly = Assembly.GetExecutingAssembly();
			var assemblyName = new AssemblyName(args.Name);

			string path = assemblyName.Name + ".dll";
			if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false) {
				path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
			}

			using (Stream stream = executingAssembly.GetManifestResourceStream(path)) {
				if (stream == null)
					return null;

				byte[] assemblyRawBytes = new byte[stream.Length];
				stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
				return Assembly.Load(assemblyRawBytes);
			}
		}
		private void OnAppStartup(object sender, StartupEventArgs e) {
			// Catch exceptions not in a UI thread
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnAppDomainUnhandledException);
			TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
			if (e.Args.Length > 0) {
				// Only reach here from CommandLine starting up the app to use the progress window.
				CommandLine.ProcessFiles();
			}
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
