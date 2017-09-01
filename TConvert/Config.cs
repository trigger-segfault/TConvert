using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TConvert.Properties;

namespace TConvert {
	public enum InputModes {
		Folder = 0,
		File = 1
	}
	public enum Tabs {
		Extract = 0,
		Convert = 1,
		Backup = 2,
		Script = 3
	}
	public static class Config {
		//=========== MEMBERS ============
		#region Members

		public static string TerrariaContentDirectory { get; set; }
		public static Tabs CurrentTab { get; set; }
		public static bool AutoCloseProgress { get; set; }
		public static bool AutoCloseDropProgress { get; set; }
		public static bool AutoCloseCmdProgress { get; set; }

		#endregion
		//=========== CLASSES ============
		#region Classes

		public static class Extract {
			public static InputModes Mode;
			public static string FolderInput { get; set; }
			public static string FolderOutput { get; set; }
			public static string FileInput { get; set; }
			public static string FileOutput { get; set; }
			public static bool AllowImages { get; set; }
			public static bool AllowSounds { get; set; }
			public static bool AllowWaveBank { get; set; }
			public static bool UseInput { get; set; }
			public static string CurrentInput {
				get {
					switch (Mode) {
					case InputModes.Folder: return FolderInput;
					case InputModes.File: return FileInput;
					}
					return "";
				}
				set {
					switch (Mode) {
					case InputModes.Folder: FolderInput = value; break;
					case InputModes.File: FileInput = value; break;
					}
				}
			}
			public static string CurrentOutput {
				get {
					switch (Mode) {
					case InputModes.Folder: return FolderOutput;
					case InputModes.File: return FileOutput;
					}
					return "";
				}
				set {
					switch (Mode) {
					case InputModes.Folder: FolderOutput = value; break;
					case InputModes.File: FileOutput = value; break;
					}
				}
			}
		}
		public static class Convert {
			public static InputModes Mode { get; set; }
			public static string FolderInput { get; set; }
			public static string FolderOutput { get; set; }
			public static string FileInput { get; set; }
			public static string FileOutput { get; set; }
			public static string ScriptInput { get; set; }
			public static bool AllowImages { get; set; }
			public static bool AllowSounds { get; set; }
			public static bool UseInput { get; set; }
			public static string CurrentInput {
				get {
					switch (Mode) {
					case InputModes.Folder: return FolderInput;
					case InputModes.File: return FileInput;
					}
					return "";
				}
				set {
					switch (Mode) {
					case InputModes.Folder: FolderInput = value; break;
					case InputModes.File: FileInput = value; break;
					}
				}
			}
			public static string CurrentOutput {
				get {
					switch (Mode) {
					case InputModes.Folder: return FolderOutput;
					case InputModes.File: return FileOutput;
					}
					return "";
				}
				set {
					switch (Mode) {
					case InputModes.Folder: FolderOutput = value; break;
					case InputModes.File: FileOutput = value; break;
					}
				}
			}
		}
		public static class Backup {
			public static string FolderContent { get; set; }
			public static string FolderBackup { get; set; }
		}
		public static class Script {
			public static string File { get; set; }
		}

		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Loads the settings.</summary>*/
		public static void Load() {
			TerrariaContentDirectory = Settings.Default.TerrariaContentDirectory;
			if (TerrariaContentDirectory == "") {
				TerrariaContentDirectory = TerrariaLocator.TerrariaContentDirectory;
			}
			Tabs tab;
			Enum.TryParse<Tabs>(Settings.Default.CurrentTab, out tab);
			CurrentTab = tab;
			AutoCloseProgress = Settings.Default.AutoCloseProgress;
			AutoCloseDropProgress = Settings.Default.AutoCloseDropProgress;
			AutoCloseCmdProgress = Settings.Default.AutoCloseCmdProgress;

			Extract.FolderInput = Settings.Default.ExtractFolderInput;
			Extract.FolderOutput = Settings.Default.ExtractFolderOutput;
			Extract.FileInput = Settings.Default.ExtractFileInput;
			Extract.FileOutput = Settings.Default.ExtractFileOutput;

			Convert.FolderInput = Settings.Default.ConvertFolderInput;
			Convert.FolderOutput = Settings.Default.ConvertFolderOutput;
			Convert.FileInput = Settings.Default.ConvertFileInput;
			Convert.FileOutput = Settings.Default.ConvertFileOutput;
			Convert.ScriptInput = Settings.Default.ConvertFileInput;

			Backup.FolderContent = Settings.Default.BackupFolderContent;
			Backup.FolderBackup = Settings.Default.BackupFolderBackup;

			Script.File = Settings.Default.ScriptFile;

			InputModes mode;
			Enum.TryParse<InputModes>(Settings.Default.ExtractMode, out mode);
			Extract.Mode = mode;
			
			Enum.TryParse<InputModes>(Settings.Default.ConvertMode, out mode);
			Convert.Mode = mode;
			
			Extract.AllowImages = Settings.Default.ExtractAllowImages;
			Extract.AllowSounds = Settings.Default.ExtractAllowSounds;
			Extract.AllowWaveBank = Settings.Default.ExtractAllowWaveBank;
			Extract.UseInput = Settings.Default.ExtractUseInput;

			Convert.AllowImages = Settings.Default.ConvertAllowImages;
			Convert.AllowSounds = Settings.Default.ConvertAllowSounds;
			Convert.UseInput = Settings.Default.ConvertUseInput;

		}
		/**<summary>Saves the settings.</summary>*/
		public static void Save() {
			Settings.Default.TerrariaContentDirectory = TerrariaContentDirectory;
			Settings.Default.CurrentTab = CurrentTab.ToString();
			Settings.Default.AutoCloseProgress = AutoCloseProgress;
			Settings.Default.AutoCloseDropProgress = AutoCloseDropProgress;
			Settings.Default.AutoCloseCmdProgress = AutoCloseCmdProgress;

			Settings.Default.ExtractFolderInput = Extract.FolderInput;
			Settings.Default.ExtractFolderOutput = Extract.FolderOutput;
			Settings.Default.ExtractFileInput = Extract.FileInput;
			Settings.Default.ExtractFileOutput = Extract.FileOutput;

			Settings.Default.ConvertFolderInput = Convert.FolderInput;
			Settings.Default.ConvertFolderOutput = Convert.FolderOutput;
			Settings.Default.ConvertFileInput = Convert.FileInput;
			Settings.Default.ConvertFileOutput = Convert.FileOutput;
			Settings.Default.ConvertFileInput = Convert.ScriptInput;

			Settings.Default.BackupFolderContent = Backup.FolderContent;
			Settings.Default.BackupFolderBackup = Backup.FolderBackup;

			Settings.Default.ScriptFile = Script.File;

			Settings.Default.ExtractMode = Extract.Mode.ToString();
			Settings.Default.ConvertMode = Convert.Mode.ToString();

			Settings.Default.ExtractAllowImages = Extract.AllowImages;
			Settings.Default.ExtractAllowSounds = Extract.AllowSounds;
			Settings.Default.ExtractAllowWaveBank = Extract.AllowWaveBank;
			Settings.Default.ExtractUseInput = Extract.UseInput;

			Settings.Default.ConvertAllowImages = Convert.AllowImages;
			Settings.Default.ConvertAllowSounds = Convert.AllowSounds;
			Settings.Default.ConvertUseInput = Convert.UseInput;

			Settings.Default.Save();
		}

		#endregion
	}
}
