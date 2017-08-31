using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
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

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs and sets up the message box.</summary>*/
		public TriggerMessageBox(MessageIcon icon, string title, string message, MessageBoxButton buttons, string buttonName1 = null, string buttonName2 = null, string buttonName3 = null) {
			InitializeComponent();
			this.buttons = buttons;
			this.minWidth = 280;

			#region Load Message Icons
			switch (icon) {
			case MessageIcon.Info:
				SystemSounds.Asterisk.Play();
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/InfoIcon.png"));
				break;
			case MessageIcon.Question:
				SystemSounds.Asterisk.Play();
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/QuestionIcon.png"));
				break;
			case MessageIcon.Warning:
				SystemSounds.Exclamation.Play();
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/WarningIcon.png"));
				break;
			case MessageIcon.Error:
				SystemSounds.Hand.Play();
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/ErrorIcon.png"));
				break;
			}
			#endregion

			// Setup the buttons
			switch (buttons) {
			#region MessageBoxButton.OK
			case MessageBoxButton.OK:
				button1.Visibility = Visibility.Hidden;
				button2.Visibility = Visibility.Hidden;
				button3.IsDefault = true;
				button3.Content = "OK";
				button3.Tag = MessageBoxResult.OK;
				minWidth -= 85 * 2;
				result = MessageBoxResult.OK;
				if (buttonName1 != null)
					button3.Content = buttonName1;
				break;
			#endregion
			#region MessageBoxButton.OKCancel
			case MessageBoxButton.OKCancel:
				button1.Visibility = Visibility.Hidden;
				button2.IsDefault = true;
				button2.Content = "OK";
				button2.Tag = MessageBoxResult.OK;
				button3.IsCancel = true;
				button3.Content = "Cancel";
				button3.Tag = MessageBoxResult.Cancel;
				minWidth -= 85;
				result = MessageBoxResult.Cancel;
				if (buttonName1 != null)
					button2.Content = buttonName1;
				if (buttonName2 != null)
					button3.Content = buttonName2;
				break;
			#endregion
			#region MessageBoxButton.YesNo
			case MessageBoxButton.YesNo:
				button1.Visibility = Visibility.Hidden;
				button2.IsDefault = true;
				button2.Content = "Yes";
				button2.Tag = MessageBoxResult.Yes;
				button3.IsCancel = true;
				button3.Content = "No";
				button3.Tag = MessageBoxResult.No;
				minWidth -= 85;
				result = MessageBoxResult.No;
				if (buttonName1 != null)
					button2.Content = buttonName1;
				if (buttonName2 != null)
					button3.Content = buttonName2;
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
		//============ EVENTS ============
		#region Events

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			this.Width = Math.Max(minWidth, textBlockMessage.ActualWidth + 60);
			this.Height += textBlockMessage.ActualHeight - 16;
		}
		private void OnButtonClicked(object sender, RoutedEventArgs e) {
			result = (MessageBoxResult)((Button)sender).Tag;
			Close();
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
