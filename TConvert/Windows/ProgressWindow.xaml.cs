using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using Timer = System.Timers.Timer;
using TConvert.Util;

namespace TConvert.Windows {
	/// <summary>
	/// Interaction logic for ProgressWindow.xaml
	/// </summary>
	public partial class ProgressWindow : Window {


		Thread thread;

		DateTime startTime;
		Timer timer;

		public ProgressWindow(Thread thread) {
			InitializeComponent();

			this.thread = thread;
			this.timer = new Timer();
			timer.Interval = 200;
			timer.AutoReset = true;
			timer.Elapsed += TimerEllapsed;
		}

		private void TimerEllapsed(object sender, ElapsedEventArgs e) {
			Dispatcher.Invoke(() => {
				labelTime.Content = "Time: " + (DateTime.Now - startTime).ToString(@"m\:ss");
			});
		}

		public void Update(string status, double progress) {
			labelStatus.Content = status;
			progressBar.Value = progress;
			TaskbarItemInfo.ProgressValue = progress;
			//labelTime.Content = "Time: " + (DateTime.Now - startTime).ToString(@"m\:ss");
		}

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			startTime = DateTime.Now;
			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
			TaskbarItemInfo.ProgressValue = 0;
			timer.Start();
			thread.Start();
		}
		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (thread.ThreadState != ThreadState.Stopped) {
				try {
					thread.Abort();
				}
				catch { }
			}
		}

		public void Finish(string status, bool close) {
			timer.Stop();
			labelStatus.Content = status;
			progressBar.Value = 1.0;
			TaskbarItemInfo.ProgressValue = 1.0;
			labelTime.Content = "Total Time: " + (DateTime.Now - startTime).ToString(@"m\:ss");
			buttonFinish.IsEnabled = true;
			buttonCancel.Visibility = Visibility.Hidden;
			buttonFinish.Margin = new Thickness(0, 0, 10, 10);
			buttonFinish.IsDefault = true;
			buttonFinish.Focus();
			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
			if (close)
				Close();
		}
		
		private void OnCancel(object sender, RoutedEventArgs e) {
			if (thread.ThreadState != ThreadState.Stopped) {
				try {
					thread.Abort();
				}
				catch { }
			}
			DialogResult = false;
		}
		private void OnFinish(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

	}
}
