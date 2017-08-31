using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TConvert.Windows {

	public struct LogError {
		public bool IsWarning;
		public string Message;
		public string Reason;
		public LogError(bool isWarning, string message, string reason) {
			IsWarning = isWarning;
			Message = message;
			Reason = reason;
		}
	}
	/// <summary>
	/// Interaction logic for ErrorLogWindow.xaml
	/// </summary>
	public partial class ErrorLogWindow : Window {

		int lines;

		private ErrorLogWindow(LogError[] errors) {
			InitializeComponent();

			lines = 0;
			textBlockMessage.Text = "";
			foreach (LogError log in errors) {
				if (lines >= 300) {
					textBlockMessage.Inlines.Add(new Run("Issues continued in log file..."));
					textBlockMessage.Inlines.Add(new LineBreak());
					break;
				}
				AddError(log);
			}
		}

		private void AddError(LogError log) {
			Run run = new Run(log.IsWarning ? "Warning: " : "Error: " + log.Message);
			ColorRun(log.IsWarning, run);
			textBlockMessage.Inlines.Add(run);
			textBlockMessage.Inlines.Add(new LineBreak());
			lines++;
			if (log.Reason != String.Empty) {
				run = new Run("    Reason: " + log.Reason);
				ColorRun(log.IsWarning, run);
				textBlockMessage.Inlines.Add(run);
				textBlockMessage.Inlines.Add(new LineBreak());
				lines++;
			}
		}
		private void ColorRun(bool isWarning, Run run) {
			if (isWarning)
				run.Foreground = new SolidColorBrush(Colors.Orange);
			else
				run.Foreground = new SolidColorBrush(Colors.Red);
		}

		private void OnOpenLogFile(object sender, RoutedEventArgs e) {
			Process.Start(ErrorLogger.LogPath);
		}

		public static void Show(Window owner, LogError[] errors) {
			ErrorLogWindow window = new ErrorLogWindow(errors);
			if (owner == null || owner.Visibility != Visibility.Visible)
				window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			else
				window.Owner = owner;
			window.ShowDialog();
		}
	}
}
