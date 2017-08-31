using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace TConvert.Windows {
	/**<summary>Shows an error that occured in the program.</summary>*/
	public partial class ErrorMessageBox : Window {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The exception that was raised.</summary>*/
		private Exception exception;
		/**<summary>The non-exception object that was raised.</summary>*/
		private object exceptionObject;
		/**<summary>True if viewing the full exception.</summary>*/
		private bool viewingFull;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the error message box with an exception.</summary>*/
		public ErrorMessageBox(Exception exception, bool alwaysContinue) {
			InitializeComponent();

			this.textBlockMessage.Text = "Exception:\n" + exception.Message;
			this.exception = exception;
			this.exceptionObject = null;
			this.viewingFull = false;
			this.buttonExit.IsEnabled = !alwaysContinue;
		}
		/**<summary>Constructs the error message box with an exception object.</summary>*/
		public ErrorMessageBox(object exceptionObject, bool alwaysContinue) {
			InitializeComponent();

			this.textBlockMessage.Text = "Exception:\n" + (exceptionObject is Exception ? (exceptionObject as Exception).Message : exceptionObject.ToString());
			this.exception = (exceptionObject is Exception ? exceptionObject as Exception : null);
			this.exceptionObject = (exceptionObject is Exception ? null : exceptionObject); ;
			this.viewingFull = false;
			if (!(exceptionObject is Exception)) {
				this.buttonException.IsEnabled = false;
				this.buttonCopy.IsEnabled = false;
			}
			this.buttonExit.IsEnabled = !alwaysContinue;
		}


		#endregion
		//============ EVENTS ============
		#region Events
		
		private void OnContinue(object sender, RoutedEventArgs e) {
			Close();
		}
		private void OnClose(object sender, RoutedEventArgs e) {
			DialogResult = true;
			Close();
		}
		private void OnCopyToClipboard(object sender, RoutedEventArgs e) {
			Clipboard.SetText(exception.ToString());
		}
		private void OnSeeFullException(object sender, RoutedEventArgs e) {
			if (viewingFull) {
				buttonException.Content = "See Full Exception";
				textBlockMessage.Text = "Exception:\n" + exception.Message;
				Height = 250;
			}
			else {
				buttonException.Content = "Hide Full Exception";
				textBlockMessage.Text = "Exception:\n" + exception.ToString();
				Height = 500;
			}
			viewingFull = !viewingFull;
		}

		private void OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.ToString());
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
