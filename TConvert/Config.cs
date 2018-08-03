using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TConvert.Convert;
using TConvert.Properties;
using TConvert.Util;

namespace TConvert {
	/**<summary>The types of input modes for convert or extract.</summary>*/
	public enum InputModes {
		Folder = 0,
		File = 1
	}
	/**<summary>The tabs to be switched to and from.</summary>*/
	public enum Tabs {
		Extract = 0,
		Convert = 1,
		Backup = 2,
		Script = 3
	}
	/**<summary>The config settings handler.</summary>*/
	public static class Config {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The specified Terraria Content folder.</summary>*/
		public static string TerrariaContentDirectory { get; set; }
		/**<summary>The current tab.</summary>*/
		public static Tabs CurrentTab { get; set; }
		/**<summary>True if normal progress windows are auto-closed.</summary>*/
		public static bool AutoCloseProgress { get; set; }
		/**<summary>True if file drop progress windows are auto-closed.</summary>*/
		public static bool AutoCloseDropProgress { get; set; }
		/**<summary>True if command line progress windows are auto-closed.</summary>*/
		public static bool AutoCloseCmdProgress { get; set; }
		/**<summary>True if images are compressed.</summary>*/
		public static bool CompressImages { get; set; }
		/**<summary>True if a sound is played on completion.</summary>*/
		public static bool CompletionSound { get; set; }
		/**<summary>True if alpha is premultiplied by default when converting to xnb.</summary>*/
		public static bool PremultiplyAlpha { get; set; }

		#endregion
		//=========== CLASSES ============
		#region Classes

		/**<summary>A container for extract settings.</summary>*/
		public static class Extract {
			/**<summary>File or Folder mode.</summary>*/
			public static InputModes Mode;
			/**<summary>The input for folder mode.</summary>*/
			public static string FolderInput { get; set; }
			/**<summary>The output for folder mode.</summary>*/
			public static string FolderOutput { get; set; }
			/**<summary>The input for file mode.</summary>*/
			public static string FileInput { get; set; }
			/**<summary>The output for file mode.</summary>*/
			public static string FileOutput { get; set; }
			/**<summary>True if images are extracted.</summary>*/
			public static bool AllowImages { get; set; }
			/**<summary>True if sounds are extracted.</summary>*/
			public static bool AllowSounds { get; set; }
			/**<summary>True if fonts are extracted.</summary>*/
			public static bool AllowFonts { get; set; }
			/**<summary>True if wave banks are extracted.</summary>*/
			public static bool AllowWaveBank { get; set; }
			/**<summary>True if the output path is the input path.</summary>*/
			public static bool UseInput { get; set; }
			/**<summary>Gets the current input path.</summary>*/
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
			/**<summary>Gets the current output path.</summary>*/
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
		/**<summary>A container for convert settings.</summary>*/
		public static class Convert {
			/**<summary>File or Folder mode.</summary>*/
			public static InputModes Mode { get; set; }
			/**<summary>The input for folder mode.</summary>*/
			public static string FolderInput { get; set; }
			/**<summary>The output for folder mode.</summary>*/
			public static string FolderOutput { get; set; }
			/**<summary>The input for file mode.</summary>*/
			public static string FileInput { get; set; }
			/**<summary>The output for file mode.</summary>*/
			public static string FileOutput { get; set; }
			/**<summary>True if images are converted.</summary>*/
			public static bool AllowImages { get; set; }
			/**<summary>True if sounds are converted.</summary>*/
			public static bool AllowSounds { get; set; }
			/**<summary>True if the output path is the input path.</summary>*/
			public static bool UseInput { get; set; }
			/**<summary>Gets the current input path.</summary>*/
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
			/**<summary>Gets the current output path.</summary>*/
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
		/**<summary>A container for backup settings.</summary>*/
		public static class Backup {
			/**<summary>The last selected content folder.</summary>*/
			public static string FolderContent { get; set; }
			/**<summary>The last selected backup folder.</summary>*/
			public static string FolderBackup { get; set; }
		}
		/**<summary>A container for script settings.</summary>*/
		public static class Script {
			/**<summary>The last selected script file.</summary>*/
			public static string File { get; set; }
		}

		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Loads the settings.</summary>*/
		public static void Load() {
			TerrariaContentDirectory = Settings.Default.TerrariaContentDirectory;
			if (TerrariaContentDirectory == "" && (TerrariaLocator.TerrariaContentDirectory != null && TerrariaLocator.TerrariaContentDirectory  != "")) {
				TerrariaContentDirectory = TerrariaLocator.TerrariaContentDirectory;
			}
			Tabs tab;
			Enum.TryParse<Tabs>(Settings.Default.CurrentTab, out tab);
			CurrentTab = tab;
			AutoCloseProgress = Settings.Default.AutoCloseProgress;
			AutoCloseDropProgress = Settings.Default.AutoCloseDropProgress;
			AutoCloseCmdProgress = Settings.Default.AutoCloseCmdProgress;
			CompressImages = Settings.Default.CompressImages && XCompress.IsAvailable;
			CompletionSound = Settings.Default.CompletionSound;
			PremultiplyAlpha = Settings.Default.PremultiplyAlpha;

			Extract.FolderInput = Settings.Default.ExtractFolderInput;
			Extract.FolderOutput = Settings.Default.ExtractFolderOutput;
			Extract.FileInput = Settings.Default.ExtractFileInput;
			Extract.FileOutput = Settings.Default.ExtractFileOutput;

			Convert.FolderInput = Settings.Default.ConvertFolderInput;
			Convert.FolderOutput = Settings.Default.ConvertFolderOutput;
			Convert.FileInput = Settings.Default.ConvertFileInput;
			Convert.FileOutput = Settings.Default.ConvertFileOutput;

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
			Extract.AllowFonts = Settings.Default.ExtractAllowFonts;
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
			Settings.Default.CompressImages = CompressImages;
			Settings.Default.CompletionSound = CompletionSound;
			Settings.Default.PremultiplyAlpha = PremultiplyAlpha;

			Settings.Default.ExtractFolderInput = Extract.FolderInput;
			Settings.Default.ExtractFolderOutput = Extract.FolderOutput;
			Settings.Default.ExtractFileInput = Extract.FileInput;
			Settings.Default.ExtractFileOutput = Extract.FileOutput;

			Settings.Default.ConvertFolderInput = Convert.FolderInput;
			Settings.Default.ConvertFolderOutput = Convert.FolderOutput;
			Settings.Default.ConvertFileInput = Convert.FileInput;
			Settings.Default.ConvertFileOutput = Convert.FileOutput;

			Settings.Default.BackupFolderContent = Backup.FolderContent;
			Settings.Default.BackupFolderBackup = Backup.FolderBackup;

			Settings.Default.ScriptFile = Script.File;

			Settings.Default.ExtractMode = Extract.Mode.ToString();
			Settings.Default.ConvertMode = Convert.Mode.ToString();

			Settings.Default.ExtractAllowImages = Extract.AllowImages;
			Settings.Default.ExtractAllowSounds = Extract.AllowSounds;
			Settings.Default.ExtractAllowFonts = Extract.AllowFonts;
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
