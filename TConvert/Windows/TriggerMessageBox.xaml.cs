using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TConvert.Windows {
	/**<summary>The different types of icons available for message boxes.</summary>*/
	public enum MessageIcon {
		/**<summary>A blue (i) icon.</summary>*/
		Info,
		/**<summary>A blue (?) icon.</summary>*/
		Question,
		/**<summary>A yellow /!\ icon.</summary>*/
		Warning,
		/**<summary>A red (!) icon.</summary>*/
		Error
	}

	/**<summary>A custom message box that doesn't look like shite.</summary>*/
	public partial class TriggerMessageBox : Window {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The result of from pressing one of the message box buttons.</summary>*/
		private MessageBoxResult result;
		/**<summary>The minimum width of the message box.</summary>*/
		private int minWidth;
		/**<summary>The message box buttons setup.</summary>*/
		private MessageBoxButton buttons;
		/**<summary>The icon of the message.</summary>*/
		private MessageIcon icon;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs and sets up the message box.</summary>*/
		private TriggerMessageBox(MessageIcon icon, string title, string message, MessageBoxButton buttons, string buttonName1 = null, string buttonName2 = null, string buttonName3 = null) {
			InitializeComponent();
			this.buttons = buttons;
			this.icon = icon;
			this.minWidth = 280;

			#region Load Message Icons
			switch (icon) {
			case MessageIcon.Info:
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/InfoIcon.png"));
				break;
			case MessageIcon.Question:
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/QuestionIcon.png"));
				break;
			case MessageIcon.Warning:
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/WarningIcon.png"));
				break;
			case MessageIcon.Error:
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/ErrorIcon.png"));
				break;
			}
			#endregion

			// Setup the buttons
			switch (buttons) {
			#region MessageBoxButton.OK
			case MessageBoxButton.OK:
				button1.IsDefault = true;
				button1.Content = "OK";
				button1.Tag = MessageBoxResult.OK;
				button2.Visibility = Visibility.Collapsed;
				button3.Visibility = Visibility.Collapsed;
				minWidth -= 85 * 2;
				result = MessageBoxResult.OK;
				if (buttonName1 != null)
					button1.Content = buttonName1;
				break;
			#endregion
			#region MessageBoxButton.OKCancel
			case MessageBoxButton.OKCancel:
				button1.IsDefault = true;
				button1.Content = "OK";
				button1.Tag = MessageBoxResult.OK;
				button2.IsCancel = true;
				button2.Content = "Cancel";
				button2.Tag = MessageBoxResult.Cancel;
				button3.Visibility = Visibility.Collapsed;
				minWidth -= 85;
				result = MessageBoxResult.Cancel;
				if (buttonName1 != null)
					button1.Content = buttonName1;
				if (buttonName2 != null)
					button2.Content = buttonName2;
				break;
			#endregion
			#region MessageBoxButton.YesNo
			case MessageBoxButton.YesNo:
				button1.IsDefault = true;
				button1.Content = "Yes";
				button1.Tag = MessageBoxResult.Yes;
				button2.IsCancel = true;
				button2.Content = "No";
				button2.Tag = MessageBoxResult.No;
				button3.Visibility = Visibility.Collapsed;
				minWidth -= 85;
				result = MessageBoxResult.No;
				if (buttonName1 != null)
					button1.Content = buttonName1;
				if (buttonName2 != null)
					button2.Content = buttonName2;
				break;
			#endregion
			#region MessageBoxButton.YesNoCancel
			case MessageBoxButton.YesNoCancel:
				button1.IsDefault = true;
				button1.Content = "Yes";
				button1.Tag = MessageBoxResult.Yes;
				button2.Content = "No";
				button2.Tag = MessageBoxResult.No;
				button3.IsCancel = true;
				button3.Content = "Cancel";
				button3.Tag = MessageBoxResult.Cancel;
				result = MessageBoxResult.Cancel;
				if (buttonName1 != null)
					button1.Content = buttonName1;
				if (buttonName2 != null)
					button2.Content = buttonName2;
				if (buttonName3 != null)
					button3.Content = buttonName3;
				break;
				#endregion
			}

			this.Title = title;
			this.textBlockMessage.Text = message;
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Gets the number of message box buttons.</summary>*/
		private int ButtonCount {
			get {
				switch (buttons) {
				case MessageBoxButton.OK:
					return 1;
				case MessageBoxButton.OKCancel:
				case MessageBoxButton.YesNo:
					return 2;
				case MessageBoxButton.YesNoCancel:
					return 3;
				}
				return 3;
			}
		}
		/**<summary>Gets the button at the specified index.</summary>*/
		private Button GetButtonAt(int index) {
			switch (index) {
			case 0: return button1;
			case 1: return button2;
			case 2: return button3;
			}
			return null;
		}
		/**<summary>Gets the index of the button.</summary>*/
		private int IndexOfButton(Button button) {
			if (button == button1)
				return 0;
			else if (button == button2)
				return 1;
			else if (button == button3)
				return 2;
			return -1;
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			clientArea.Width = Math.Max(minWidth, Math.Max(textBlockMessage.ActualWidth + 60, stackPanelButtons.ActualWidth + 10));
			clientArea.Height += textBlockMessage.ActualHeight - 16;

			#region Load Message Sounds
			switch (icon) {
			case MessageIcon.Info: SystemSounds.Asterisk.Play(); break;
			case MessageIcon.Question: SystemSounds.Asterisk.Play(); break;
			case MessageIcon.Warning: SystemSounds.Exclamation.Play(); break;
			case MessageIcon.Error: SystemSounds.Hand.Play(); break;
			}
			#endregion
		}
		private void OnButtonClicked(object sender, RoutedEventArgs e) {
			result = (MessageBoxResult)((Button)sender).Tag;
			Close();
		}
		private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			var button = FocusManager.GetFocusedElement(this) as Button;
			switch (e.Key) {
			case Key.Right:
				if (button == null && ButtonCount > 1)
					GetButtonAt(1).Focus();
				else if (button != null && IndexOfButton(button) < ButtonCount - 1)
					button.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
				e.Handled = true;
				break;
			case Key.Left:
				if (button != null && IndexOfButton(button) > 0)
					button.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
				e.Handled = true;
				break;
			}
		}

		#endregion
		//=========== SHOWING ============
		#region Showing

		/**<summary>Shows the message box.</summary>*/
		public static MessageBoxResult Show(Window window, MessageIcon icon, string message) {
			return Show(window, icon, message, "", MessageBoxButton.OK);
		}
		/**<summary>Shows the message box.</summary>*/
		public static MessageBoxResult Show(Window window, MessageIcon icon, string message, string title) {
			return Show(window, icon, message, title, MessageBoxButton.OK);
		}
		/**<summary>Shows the message box.</summary>*/
		public static MessageBoxResult Show(Window window, MessageIcon icon, string message, MessageBoxButton buttons) {
			return Show(window, icon, message, "", buttons);
		}
		/**<summary>Shows the message box.</summary>*/
		public static MessageBoxResult Show(Window window, MessageIcon icon, string message, string title, MessageBoxButton buttons, string buttonName1 = null, string buttonName2 = null, string buttonName3 = null) {
			TriggerMessageBox messageBox = new TriggerMessageBox(icon, title, message, buttons, buttonName1, buttonName2, buttonName3);
			if (window == null || window.Visibility != Visibility.Visible)
				messageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			else
				messageBox.Owner = window;
			messageBox.ShowDialog();
			return messageBox.result;
		}

		#endregion
	}
}
