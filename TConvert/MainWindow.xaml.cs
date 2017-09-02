using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using TConvert.Convert;
using TConvert.Extract;
using TConvert.Util;
using TConvert.Windows;
using Path = System.IO.Path;

namespace TConvert {
	/**<summary>The main TConvert window.</summary>*/
	public partial class MainWindow : Window {
		//=========== MEMBERS ============
		#region Members

		/**<summary>True if the window has been loaded.</summary>*/
		bool loaded;
		/**<summary>The last path visited with a folder browser.</summary>*/
		string lastFolderPath;
		/**<summary>The last path visited with a file browser.</summary>*/
		string lastFilePath;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the main window.</summary>*/
		public MainWindow() {
			loaded = false;
			InitializeComponent();
			lastFolderPath = "";
			lastFilePath = "";

			// Disable drag/drop text in textboxes so you can scroll their contents easily
			DataObject.AddCopyingHandler(textBoxTerrariaContent, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxExtractInput, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxExtractOutput, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxConvertInput, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxConvertOutput, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxContent, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxBackup, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxScript, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });

			labelDrop.Visibility = Visibility.Hidden;

			LoadConfig();
		}

		#endregion
		//============ CONFIG ============
		#region Config

		/**<summary>Loads the config and updates controls.</summary>*/
		private void LoadConfig() {
			loaded = false;

			Config.Load();

			textBoxTerrariaContent.Text = Config.TerrariaContentDirectory;

			comboBoxExtractMode.SelectedIndex = (int)Config.Extract.Mode;
			textBoxExtractInput.Text = Config.Extract.CurrentInput;
			textBoxExtractOutput.Text = Config.Extract.CurrentOutput;
			checkBoxExtractImages.IsChecked = Config.Extract.AllowImages;
			checkBoxExtractSounds.IsChecked = Config.Extract.AllowSounds;
			checkBoxExtractFonts.IsChecked = Config.Extract.AllowFonts;
			checkBoxExtractWaveBank.IsChecked = Config.Extract.AllowWaveBank;
			checkBoxExtractUseInput.IsChecked = Config.Extract.UseInput;
			textBoxExtractOutput.IsEnabled = !Config.Extract.UseInput;
			buttonExtractOutput.IsEnabled = !Config.Extract.UseInput;
			buttonExtractUseTerraria.IsEnabled = !Config.Extract.UseInput && Config.Extract.Mode == InputModes.Folder;
			checkBoxExtractImages.IsEnabled = Config.Extract.Mode == InputModes.Folder;
			checkBoxExtractSounds.IsEnabled = Config.Extract.Mode == InputModes.Folder;
			checkBoxExtractWaveBank.IsEnabled = Config.Extract.Mode == InputModes.Folder;
			switch (Config.Extract.Mode) {
			case InputModes.Folder:
				labelExtractInput.Content = "Input Folder";
				labelExtractOutput.Content = "Output Folder";
				break;
			case InputModes.File:
				labelExtractInput.Content = "Input File";
				labelExtractOutput.Content = "Output File";
				break;
			}

			comboBoxConvertMode.SelectedIndex = (int)Config.Convert.Mode;
			textBoxConvertInput.Text = Config.Convert.CurrentInput;
			textBoxConvertOutput.Text = Config.Convert.CurrentOutput;
			checkBoxConvertImages.IsChecked = Config.Convert.AllowImages;
			checkBoxConvertSounds.IsChecked = Config.Convert.AllowSounds;
			checkBoxConvertUseInput.IsChecked = Config.Convert.UseInput;
			textBoxConvertOutput.IsEnabled = !Config.Convert.UseInput;
			buttonConvertOutput.IsEnabled = !Config.Convert.UseInput;
			buttonConvertUseTerraria.IsEnabled = !Config.Convert.UseInput && Config.Convert.Mode == InputModes.Folder;
			checkBoxConvertImages.IsEnabled = Config.Convert.Mode == InputModes.Folder;
			checkBoxConvertSounds.IsEnabled = Config.Convert.Mode == InputModes.Folder;
			switch (Config.Convert.Mode) {
			case InputModes.Folder:
				labelConvertInput.Content = "Input Folder";
				labelConvertOutput.Content = "Output Folder";
				break;
			case InputModes.File:
				labelConvertInput.Content = "Input File";
				labelConvertOutput.Content = "Output File";
				break;
			}

			textBoxContent.Text = Config.Backup.FolderContent;
			textBoxBackup.Text = Config.Backup.FolderBackup;

			textBoxScript.Text = Config.Script.File;

			tabControl.SelectedIndex = (int)Config.CurrentTab;

			menuItemCompressImages.IsEnabled = XCompress.IsAvailable;
			menuItemCompressImages.IsChecked = Config.CompressImages;
			menuItemCompletionSound.IsChecked = Config.CompletionSound;
			menuItemAutoCloseProgress.IsChecked = Config.AutoCloseProgress;
			menuItemAutoCloseDropProgress.IsChecked = Config.AutoCloseDropProgress;
			menuItemAutoCloseCmdProgress.IsChecked = Config.AutoCloseCmdProgress;
		}
		
		#endregion
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region General

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}
		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			try {
				Config.Save();
			}
			catch { }
		}
		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			// Make text boxes lose focus on click away
			FocusManager.SetFocusedElement(this, this);
		}
		private void OnTabChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			Config.CurrentTab = (Tabs)tabControl.SelectedIndex;
		}
		private void OnBrowseTerraria(object sender, RoutedEventArgs e) {
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.ShowNewFolderButton = false;
			dialog.Description = "Choose Terraria Content folder";
			dialog.SelectedPath = Path.GetFullPath(Config.TerrariaContentDirectory);
			if (dialog.SelectedPath == string.Empty)
				dialog.SelectedPath = lastFolderPath;
			var result = FolderBrowserLauncher.ShowFolderBrowser(dialog);
			if (result == System.Windows.Forms.DialogResult.OK) {
				Config.TerrariaContentDirectory = dialog.SelectedPath;
				textBoxTerrariaContent.Text = dialog.SelectedPath;
			}
			lastFolderPath = dialog.SelectedPath;
		}
		private void OnTerrariaContentChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.TerrariaContentDirectory = textBoxTerrariaContent.Text;
		}

		#endregion
		//--------------------------------
		#region Extracting

		private void OnExtract(object sender, RoutedEventArgs e) {
			string input = Config.Extract.CurrentInput;
			string output = (Config.Extract.UseInput ? Config.Extract.CurrentInput : Config.Extract.CurrentOutput);
			bool allowImages = Config.Extract.AllowImages;
			bool allowSounds = Config.Extract.AllowSounds;
			bool allowFonts = Config.Extract.AllowFonts;
			bool allowWaveBank = Config.Extract.AllowWaveBank;

			Thread thread;
			if (Config.Extract.Mode == InputModes.Folder) {
				if (!Helpers.DirectoryExistsSafe(input)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the input folder.", "Invalid Path");
					return;
				}
				if (!Helpers.IsPathValid(output)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Output folder path is invalid.", "Invalid Path");
					return;
				}
				input = Helpers.FixPathSafe(input);
				output = Helpers.FixPathSafe(output);
				thread = new Thread(() => {
					Processing.ExtractAll(input, output, allowImages, allowSounds, allowFonts, allowWaveBank);
				});
			}
			else {
				if (!Helpers.FileExistsSafe(input)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the input file.", "Invalid Path");
					return;
				}
				if (!Helpers.IsPathValid(output)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Output file path is invalid.", "Invalid Path");
					return;
				}
				input = Helpers.FixPathSafe(input);
				output = Helpers.FixPathSafe(output);
				thread = new Thread(() => {
					Processing.ExtractSingleFile(input, output);
				});
			}
			Processing.StartProgressThread(this, "Extracting...", Config.AutoCloseProgress, Config.CompressImages, Config.CompletionSound, thread);
		}
		private void OnExtractModeChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.Mode = (InputModes)comboBoxExtractMode.SelectedIndex;
			
			switch (Config.Extract.Mode) {
			case InputModes.Folder:
				labelExtractInput.Content = "Input Folder";
				labelExtractOutput.Content = "Output Folder";
				break;
			case InputModes.File:
				labelExtractInput.Content = "Input File";
				labelExtractOutput.Content = "Output File";
				break;
			}
			textBoxExtractInput.Text = Config.Extract.CurrentInput;
			textBoxExtractOutput.Text = Config.Extract.CurrentOutput;
			buttonExtractUseTerraria.IsEnabled = !Config.Extract.UseInput && Config.Extract.Mode == InputModes.Folder;
			checkBoxExtractImages.IsEnabled = Config.Extract.Mode == InputModes.Folder;
			checkBoxExtractSounds.IsEnabled = Config.Extract.Mode == InputModes.Folder;
			checkBoxExtractWaveBank.IsEnabled = Config.Extract.Mode == InputModes.Folder;
		}
		private void OnExtractChangeInput(object sender, RoutedEventArgs e) {
			string path = GetPath(Config.Extract.CurrentInput, true, true);
			if (path != null) {
				Config.Extract.CurrentInput = path;
				textBoxExtractInput.Text = path;
			}
		}
		private void OnExtractChangeOutput(object sender, RoutedEventArgs e) {
			string path = GetPath(Config.Extract.CurrentOutput, false, true);
			if (path != null) {
				Config.Extract.CurrentOutput = path;
				textBoxExtractOutput.Text = path;
			}
		}
		private void OnExtractUseInputChecked(object sender, RoutedEventArgs e) {
			Config.Extract.UseInput = checkBoxExtractUseInput.IsChecked.Value;
			textBoxExtractOutput.IsEnabled = !Config.Extract.UseInput;
			buttonExtractOutput.IsEnabled = !Config.Extract.UseInput;
			buttonExtractUseTerraria.IsEnabled = !Config.Extract.UseInput && Config.Extract.Mode == InputModes.Folder;
		}
		private void OnExtractImagesChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.AllowImages = checkBoxExtractImages.IsChecked.Value;
		}
		private void OnExtractSoundsChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.AllowSounds = checkBoxExtractSounds.IsChecked.Value;
		}
		private void OnExtractFontsChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.AllowFonts = checkBoxExtractFonts.IsChecked.Value;
		}
		private void OnExtractWaveBankChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.AllowWaveBank = checkBoxExtractWaveBank.IsChecked.Value;
		}
		private void OnExtractUseTerraria(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.FolderInput = Config.TerrariaContentDirectory;
			textBoxExtractInput.Text = Config.TerrariaContentDirectory;
		}
		private void OnExtractInputChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.CurrentInput = textBoxExtractInput.Text;
		}
		private void OnExtractOutputChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Extract.CurrentOutput = textBoxExtractOutput.Text;
		}

		#endregion
		//--------------------------------
		#region Converting

		private void OnConvert(object sender, RoutedEventArgs e) {
			string input = Config.Convert.CurrentInput;
			string output = (Config.Convert.UseInput ? Config.Convert.CurrentInput : Config.Convert.CurrentOutput);
			bool allowImages = Config.Convert.AllowImages;
			bool allowSounds = Config.Convert.AllowSounds;

			Thread thread;
			if (Config.Convert.Mode == InputModes.Folder) {
				if (!Helpers.DirectoryExistsSafe(input)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the input folder.", "Invalid Path");
					return;
				}
				if (!Helpers.IsPathValid(output)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Output folder path is invalid.", "Invalid Path");
					return;
				}
				input = Helpers.FixPathSafe(input);
				output = Helpers.FixPathSafe(output);
				thread = new Thread(() => {
					Processing.ConvertAll(input, output, allowImages, allowSounds);
				});
			}
			else {
				if (!Helpers.FileExistsSafe(input)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the input file", "Invalid Path");
					return;
				}
				if (!Helpers.IsPathValid(output)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Output file path is invalid.", "Invalid Path");
					return;
				}
				input = Helpers.FixPathSafe(input);
				output = Helpers.FixPathSafe(output);
				thread = new Thread(() => {
					Processing.ConvertSingleFile(input, output);
				});
			}
			Processing.StartProgressThread(this, "Converting...", Config.AutoCloseProgress, Config.CompressImages, Config.CompletionSound, thread);
		}
		private void OnConvertModeChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			Config.Convert.Mode = (InputModes)comboBoxConvertMode.SelectedIndex;

			switch (Config.Convert.Mode) {
			case InputModes.Folder:
				labelConvertInput.Content = "Input Folder";
				labelConvertOutput.Content = "Output Folder";
				break;
			case InputModes.File:
				labelConvertInput.Content = "Input File";
				labelConvertOutput.Content = "Output File";
				break;
			}
			textBoxConvertInput.Text = Config.Convert.CurrentInput;
			textBoxConvertOutput.Text = Config.Convert.CurrentOutput;
			buttonConvertUseTerraria.IsEnabled = !Config.Convert.UseInput && Config.Convert.Mode == InputModes.Folder;
			checkBoxConvertImages.IsEnabled = Config.Convert.Mode == InputModes.Folder;
			checkBoxConvertSounds.IsEnabled = Config.Convert.Mode == InputModes.Folder;
		}
		private void OnConvertChangeInput(object sender, RoutedEventArgs e) {
			string path = GetPath(Config.Convert.CurrentInput, true, false);
			if (path != null) {
				Config.Convert.CurrentInput = path;
				textBoxConvertInput.Text = path;
			}
		}
		private void OnConvertChangeOutput(object sender, RoutedEventArgs e) {
			string path = GetPath(Config.Convert.CurrentOutput, false, false);
			if (path != null) {
				Config.Convert.CurrentOutput = path;
				textBoxConvertOutput.Text = path;
			}
		}
		private void OnConvertUseInputChecked(object sender, RoutedEventArgs e) {
			Config.Convert.UseInput = checkBoxConvertUseInput.IsChecked.Value;
			textBoxConvertOutput.IsEnabled = !Config.Convert.UseInput;
			buttonConvertOutput.IsEnabled = !Config.Convert.UseInput;
			buttonConvertUseTerraria.IsEnabled = !Config.Convert.UseInput && Config.Convert.Mode == InputModes.Folder;
		}
		private void OnConvertImagesChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Convert.AllowImages = checkBoxConvertImages.IsChecked.Value;
		}
		private void OnConvertSoundsChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Convert.AllowSounds = checkBoxConvertSounds.IsChecked.Value;
		}
		private void OnConvertUseTerraria(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Convert.FolderOutput = Config.TerrariaContentDirectory;
			textBoxConvertOutput.Text = Config.TerrariaContentDirectory;
		}
		private void OnConvertInputChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Convert.CurrentInput = textBoxConvertInput.Text;
		}
		private void OnConvertOutputChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Convert.CurrentOutput = textBoxConvertOutput.Text;
		}

		#endregion
		//--------------------------------
		#region Backup/Restore

		private void OnBackup(object sender, RoutedEventArgs e) {
			string input = Config.Backup.FolderContent;
			string output = Config.Backup.FolderBackup;

			if (!Helpers.DirectoryExistsSafe(input)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the Content folder.", "Invalid Path");
				return;
			}
			if (!Helpers.IsPathValid(output)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The Backup folder path is invalid.", "Invalid Path");
				return;
			}
			if (string.Compare(Path.GetFullPath(input), Path.GetFullPath(output), true) == 0) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Backup paths cannot be the same folder.", "Invalid Path");
				return;
			}

			input = Helpers.FixPathSafe(input);
			output = Helpers.FixPathSafe(output);
			Thread thread = new Thread(() => {
				Processing.BackupFiles(input, output);
			});
			Processing.StartProgressThread(this, "Backing Up...", Config.AutoCloseProgress, Config.CompressImages, Config.CompletionSound, thread);
		}
		private void OnRestore(object sender, RoutedEventArgs e) {
			string input = Config.Backup.FolderBackup;
			string output = Config.Backup.FolderContent;

			if (!Helpers.DirectoryExistsSafe(input)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the Backup folder.", "Invalid Path");
				return;
			}
			if (!Helpers.DirectoryExistsSafe(output)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the Content folder.", "Invalid Path");
				return;
			}
			if (string.Compare(Path.GetFullPath(input), Path.GetFullPath(output), true) == 0) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Restore paths cannot be the same folder.", "Invalid Path");
				return;
			}

			input = Helpers.FixPathSafe(input);
			output = Helpers.FixPathSafe(output);
			Thread thread = new Thread(() => {
				Processing.RestoreFiles(input, output);
			});
			Processing.StartProgressThread(this, "Restoring...", Config.AutoCloseProgress, Config.CompressImages, Config.CompletionSound, thread);
		}
		private void OnBackupChangeContent(object sender, RoutedEventArgs e) {
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.ShowNewFolderButton = true;
			dialog.Description = "Choose Content folder";
			dialog.SelectedPath = Config.Backup.FolderContent;
			if (dialog.SelectedPath == string.Empty)
				dialog.SelectedPath = lastFolderPath;
			var result = FolderBrowserLauncher.ShowFolderBrowser(dialog);
			if (result == System.Windows.Forms.DialogResult.OK) {
				Config.Backup.FolderContent = dialog.SelectedPath;
				textBoxContent.Text = dialog.SelectedPath;
			}
			lastFolderPath = dialog.SelectedPath;
		}
		private void OnBackupChangeBackup(object sender, RoutedEventArgs e) {
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.ShowNewFolderButton = true;
			dialog.Description = "Choose Backup folder";
			dialog.SelectedPath = Config.Backup.FolderBackup;
			if (dialog.SelectedPath == string.Empty)
				dialog.SelectedPath = lastFolderPath;
			var result = FolderBrowserLauncher.ShowFolderBrowser(dialog);
			if (result == System.Windows.Forms.DialogResult.OK) {
				Config.Backup.FolderBackup = dialog.SelectedPath;
				textBoxBackup.Text = dialog.SelectedPath;
			}
			lastFolderPath = dialog.SelectedPath;
		}
		private void OnBackupUseTerraria(object sender, RoutedEventArgs e) {
			Config.Backup.FolderContent = Config.TerrariaContentDirectory;
			textBoxContent.Text = Config.TerrariaContentDirectory;
		}
		private void OnContentChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Backup.FolderContent = textBoxContent.Text;
		}
		private void OnBackupChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Backup.FolderBackup = textBoxBackup.Text;
		}

		#endregion
		//--------------------------------
		#region Scripting

		private void OnRunScript(object sender, RoutedEventArgs e) {
			string input = Config.Script.File;

			Thread thread;
			if (!Helpers.FileExistsSafe(input)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find the script file.", "Invalid Path");
				return;
			}
			input = Helpers.FixPathSafe(input);
			thread = new Thread(() => {
				Processing.RunScript(input);
			});
			Processing.StartProgressThread(this, "Running Script...", Config.AutoCloseProgress, Config.CompressImages, Config.CompletionSound, thread);
		}
		private void OnChangeScript(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Xml files (*.xml)|*.xml|All files (*.*)|*.*";
			dialog.FilterIndex = 0;
			dialog.Title = "Choose script file";
			dialog.CheckFileExists = true;
			if (Config.Script.File != string.Empty) {
				dialog.FileName = Path.GetFileName(Config.Script.File);
				dialog.InitialDirectory = Path.GetDirectoryName(Config.Script.File);
			}
			else {
				dialog.InitialDirectory = lastFilePath;
			}
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				textBoxScript.Text = dialog.FileName;
				Config.Script.File = dialog.FileName;
			}
			lastFilePath = Path.GetDirectoryName(dialog.FileName);
		}
		private void OnScriptChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Script.File = textBoxScript.Text;
		}

		#endregion
		//--------------------------------
		#region File Drop

		private void OnFileDrop(object sender, DragEventArgs e) {
			labelDrop.Visibility = Visibility.Hidden;
			if (this.OwnedWindows.Count == 0 && e.Data.GetDataPresent(DataFormats.FileDrop)) {
				List<string> files = new List<string>();
				List<string> extractFiles = new List<string>();
				List<string> convertFiles = new List<string>();
				List<string> scriptFiles = new List<string>();
				string[] initialFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

				// Allow extracting/converting of directories too
				foreach (string file in initialFiles) {
					if (Directory.Exists(file))
						files.AddRange(Helpers.FindAllFiles(file));
					else
						files.Add(file);
				}
				
				foreach (string file in files) {
					string ext = Path.GetExtension(file).ToLower();
					switch (ext) {
					case ".xnb":
					case ".xwb":
						extractFiles.Add(file);
						break;
					case ".png":
					case ".bmp":
					case ".jpg":
					case ".wav":
						convertFiles.Add(file);
						break;
					case ".xml":
						scriptFiles.Add(file);
						break;
					}
				}

				if (extractFiles.Count == 0 && convertFiles.Count == 0 && scriptFiles.Count == 0) {
					// Prevent Explorer from freazing until the message box is closed
					Thread showThread = new Thread(() => {
						Application.Current.Dispatcher.Invoke(() => {
							TriggerMessageBox.Show(this, MessageIcon.Warning, "No files to convert or extract, or scripts to run!", "File Drop");
						});
					});
					showThread.Start();
				}
				else {
					Thread thread = new Thread(() => {
						Processing.ProcessDropFiles(extractFiles.ToArray(), convertFiles.ToArray(), scriptFiles.ToArray());
					});
					Processing.StartProgressThread(this, "Processing Drop Files...", Config.AutoCloseDropProgress, Config.CompressImages, Config.CompletionSound, thread);
				}
			}
		}
		private void OnFileDropEnter(object sender, DragEventArgs e) {
			if (this.OwnedWindows.Count == 0 && e.Data.GetDataPresent(DataFormats.FileDrop)) {
				labelDrop.Visibility = Visibility.Visible;
				e.Effects = DragDropEffects.Copy;
			}
			else {
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
		}
		private void OnFileDropOver(object sender, DragEventArgs e) {
			if (this.OwnedWindows.Count == 0 && e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effects = DragDropEffects.Copy;
			}
			else {
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
		}
		private void OnFileDropLeave(object sender, DragEventArgs e) {
			labelDrop.Visibility = Visibility.Hidden;
		}

		#endregion
		//--------------------------------
		#region Menu Items

		private void OnLaunchTerraria(object sender, RoutedEventArgs e) {
			if (Config.TerrariaContentDirectory != string.Empty) {
				try {
					string dir = Config.TerrariaContentDirectory;
					string terraria = Path.Combine(dir, "Terraria.exe");
					if (File.Exists(terraria)) {
						Process.Start(terraria);
					}
					else {
						try {
							dir = Path.GetDirectoryName(Config.TerrariaContentDirectory);
							terraria = Path.Combine(dir, "Terraria.exe");
							if (File.Exists(terraria)) {
								Process.Start(terraria);
								return;
							}
						}
						catch { }
					}
				}
				catch { }
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to locate Terraria executable.", "Missing Exe");
			}
			else {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "No path to Terraria specified.", "No Path");
			}
		}
		private void OnOpenTerrariaFolder(object sender, RoutedEventArgs e) {
			if (Config.TerrariaContentDirectory != string.Empty) {
				string dir = Config.TerrariaContentDirectory;
				try {
					dir = Path.GetDirectoryName(Config.TerrariaContentDirectory);
				}
				catch { }
				Process.Start(dir);
			}
		}
		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}

		private void OnCompressImagesChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.CompressImages = menuItemCompressImages.IsChecked;
		}
		private void OnCompletionSoundChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.CompletionSound = menuItemCompletionSound.IsChecked;
		}
		private void OnAutoCloseProgress(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.AutoCloseProgress = menuItemAutoCloseProgress.IsChecked;
		}
		private void OnAutoCloseDropProgress(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.AutoCloseDropProgress = menuItemAutoCloseDropProgress.IsChecked;
		}
		private void OnAutoCloseCmdProgress(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.AutoCloseCmdProgress = menuItemAutoCloseCmdProgress.IsChecked;
		}

		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutWindow.Show(this);
		}
		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TConvert/wiki");
		}
		private void OnCredits(object sender, RoutedEventArgs e) {
			CreditsWindow.Show(this);
		}
		private void OnViewOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TConvert");
		}

		#endregion
		//--------------------------------
		#endregion
		//=========== HELPERS ============
		#region Helpers

		private string GetPath(string currentPath, bool input, bool extract) {
			switch (extract ? Config.Extract.Mode : Config.Convert.Mode) {
			case InputModes.Folder: {
					System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
					dialog.ShowNewFolderButton = true;
					dialog.Description = "Choose " + (input ? "input" : "output") + " folder";
					dialog.SelectedPath = currentPath;
					if (dialog.SelectedPath == string.Empty)
						dialog.SelectedPath = lastFolderPath;
					var result = FolderBrowserLauncher.ShowFolderBrowser(dialog);
					lastFolderPath = dialog.SelectedPath;
					if (result == System.Windows.Forms.DialogResult.OK) {
						return dialog.SelectedPath;
					}
					break;
				}
			case InputModes.File: {
					FileDialog dialog;
					if (input)
						dialog = new OpenFileDialog();
					else
						dialog = new SaveFileDialog();
					dialog.Filter = (extract == input ?
						"Xna files (*.xnb, *.xwb)|*.xnb;*.xwb|" +
						"Xnb files (*.xnb)|*.xnb|" +
						"Xwb files (*.xwb)|*.xwb|" :
						"Image & Wav files (*.png, *.bmp, *.jpg, *.wav)|*.png;*.bmp;*.jpg;*.wav|" +
						"Image files (*.png, *.bmp, *.jpg)|*.png;*.bmp;*.jpg;|" +
						"Wav files (*.wav)|*.wav|"
						) + "All files (*.*)|*.*";
					dialog.FilterIndex = 0;
					dialog.Title = "Choose " + (input ? "input" : "output") + " file";
					dialog.CheckFileExists = input;
					if (currentPath != string.Empty) {
						dialog.FileName = Path.GetFileName(currentPath);
						dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
					}
					else {
						dialog.InitialDirectory = lastFilePath;
					}
					var result = dialog.ShowDialog(this);
					lastFilePath = Path.GetDirectoryName(dialog.FileName);
					if (result.HasValue && result.Value) {
						return dialog.FileName;
					}
					break;
				}
			}
			return null;
		}

		#endregion
	}
}
