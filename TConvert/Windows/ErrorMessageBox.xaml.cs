using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

namespace TConvert.Windows {
	/**<summary>Shows an error that occured in the program.</summary>*/
	public partial class ErrorMessageBox : Window {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The exception that was raised.</summary>*/
		private Exception exception = null;
		/**<summary>The non-exception object that was raised.</summary>*/
		private object exceptionObject = null;
		/**<summary>True if viewing the full exception.</summary>*/
		private bool viewingFull = false;
		/**<summary>The timer for changing the copy button back to its original text.</summary>*/
		private Timer copyTimer = new Timer(1000);
		/**<summary>The text of the copy to clipboard button.</summary>*/
		private readonly string copyText;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the error message box with an exception.</summary>*/
		public ErrorMessageBox(Exception exception, bool alwaysContinue) {
			InitializeComponent();

			this.textBlockMessage.Text = "Exception:\n" + exception.Message;
			this.exception = exception;
			this.copyTimer.Elapsed += OnCopyTimer;
			this.copyTimer.AutoReset = false;
			this.copyText = buttonCopy.Content as string;
			if (alwaysContinue) {
				this.buttonExit.Visibility = Visibility.Collapsed;
				this.buttonContinue.IsDefault = true;
			}
		}
		/**<summary>Constructs the error message box with an exception object.</summary>*/
		public ErrorMessageBox(object exceptionObject, bool alwaysContinue) {
			InitializeComponent();

			this.textBlockMessage.Text = "Exception:\n" + (exceptionObject is Exception ? (exceptionObject as Exception).Message : exceptionObject.ToString());
			this.exception = (exceptionObject is Exception ? exceptionObject as Exception : null);
			this.exceptionObject = (exceptionObject is Exception ? null : exceptionObject);
			this.copyTimer.Elapsed += OnCopyTimer;
			this.copyTimer.AutoReset = false;
			this.copyText = buttonCopy.Content as string;
			if (!(exceptionObject is Exception)) {
				this.buttonException.IsEnabled = false;
			}
			if (alwaysContinue) {
				this.buttonExit.Visibility = Visibility.Collapsed;
				this.buttonContinue.IsDefault = true;
			}
		}


		#endregion
		//============ EVENTS ============
		#region Events

		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			copyTimer.Stop();
		}
		private void OnExit(object sender, RoutedEventArgs e) {
			DialogResult = true;
			Close();
		}
		private void OnCopyTimer(object sender, ElapsedEventArgs e) {
			Dispatcher.Invoke(() => {
				buttonCopy.Content = copyText;
			});
		}
		private void OnCopyToClipboard(object sender, RoutedEventArgs e) {
			Clipboard.SetText(exception != null ? exception.ToString() : exceptionObject.ToString());
			buttonCopy.Content = "Exception Copied!";
			copyTimer.Stop();
			copyTimer.Start();
		}
		private void OnSeeFullException(object sender, RoutedEventArgs e) {
			viewingFull = !viewingFull;
			if (!viewingFull) {
				buttonException.Content = "See Full Exception";
				textBlockMessage.Text = "Exception:\n" + exception.Message;
				clientArea.Height = 230;
				scrollViewer.ScrollToTop();
			}
			else {
				buttonException.Content = "Hide Full Exception";
				// Size may not be changed yet so just incase we also have OnMessageSizeChanged
				textBlockMessage.Text = "Exception:\n" + exception.ToString();
				clientArea.Height = Math.Min(480, Math.Max(230, textBlockMessage.ActualHeight + 102));
				scrollViewer.ScrollToTop();
			}
		}
		private void OnMessageSizeChanged(object sender, SizeChangedEventArgs e) {
			if (viewingFull) {
				clientArea.Height = Math.Min(480, Math.Max(230, textBlockMessage.ActualHeight + 102));
				scrollViewer.ScrollToTop();
			}
		}
		private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			var focused = FocusManager.GetFocusedElement(this);
			switch (e.Key) {
			case Key.Right:
				if (focused == buttonContinue && buttonExit.Visibility == Visibility.Visible)
					buttonExit.Focus();
				else if (focused == buttonCopy)
					buttonContinue.Focus();
				else if (focused == buttonException)
					buttonCopy.Focus();
				e.Handled = true;
				break;
			case Key.Left:
				if (focused == null) {
					if (buttonExit.Visibility == Visibility.Visible)
						buttonContinue.Focus();
					else
						buttonCopy.Focus();
				}
				else if (focused == buttonExit)
					buttonContinue.Focus();
				else if (focused == buttonContinue)
					buttonCopy.Focus();
				else if (focused == buttonCopy && buttonException.IsEnabled)
					buttonException.Focus();
				e.Handled = true;
				break;
			}
		}
		private void OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			Process.Start((sender as Hyperlink).NavigateUri.ToString());
		}

		#endregion
		//=========== SHOWING ============
		#region Showing

		/**<summary>Shows an error message box with an exception.</summary>*/
		public static bool Show(Exception exception, bool alwaysContinue = false) {
			ErrorMessageBox messageBox = new ErrorMessageBox(exception, alwaysContinue);
			var result = messageBox.ShowDialog();
			return result.HasValue && result.Value;
		}
		/**<summary>Shows an error message box with an exception object.</summary>*/
		public static bool Show(object exceptionObject, bool alwaysContinue = false) {
			ErrorMessageBox messageBox = new ErrorMessageBox(exceptionObject, alwaysContinue);
			var result = messageBox.ShowDialog();
			return result.HasValue && result.Value;
		}

		#endregion
	}
}
