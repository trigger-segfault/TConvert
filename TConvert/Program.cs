using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using TConvert.Convert;
using TConvert.Extract;
using TConvert.Util;
using TConvert.Windows;

namespace TConvert {
	public class Script {
		public List<PathIOPair> Backups;
		public List<PathIOPair> Restores;
		public List<PathIOPair> Extracts;
		public List<PathIOPair> Converts;
	}
	public static class Program {


		//=========== MEMBERS ============
		#region Members

		private static readonly TimeSpan UpdateSpan = TimeSpan.FromMilliseconds(50);
		private static DateTime lastUpdate = DateTime.MinValue;

		private static int totalFiles = 0;
		private static int filesCompleted = 0;

		private static ProgressWindow progressWindow;

		private static bool autoCloseProgress;

		public static MainWindow MainWindow;

		public static List<LogError> errorLog = new List<LogError>();

		#endregion
		//=========== PROGRESS ===========
		#region Progress

		public static void StartProgressThread(Window owner, string message, bool autoClose, Thread thread) {
			lastUpdate = DateTime.MinValue;
			autoCloseProgress = autoClose;
			filesCompleted = 0;
			totalFiles = 0;
			progressWindow = new ProgressWindow(thread);
			if (owner != null) {
				progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				progressWindow.Owner = owner;
			}
			progressWindow.ShowDialog();
			progressWindow = null;
			filesCompleted = 0;
			totalFiles = 0;
		}
		public static void UpdateProgress(string message, bool forceUpdate = false) {
			if (progressWindow != null) {
				if (lastUpdate + UpdateSpan < DateTime.Now || forceUpdate) {
					progressWindow.Dispatcher.Invoke(() => {
						progressWindow.Update(message, totalFiles == 0 ? 0 : ((double)filesCompleted / totalFiles));
					});
					lastUpdate = DateTime.Now;
				}
			}
		}
		public static void FinishProgress(string message, bool final = true) {
			if (progressWindow != null) {
				progressWindow.Dispatcher.Invoke(() => {
					progressWindow.Finish(message, final && autoCloseProgress);
				});
				if (final) {
					ErrorLogger.Close();
					if (errorLog.Count > 0) {
						ShowErrorLog();
						errorLog.Clear();
					}
				}
			}
		}
		public static void ShowErrorLog() {
			DispatcherObject dispatcher;
			Window window = null;
			if (progressWindow != null) {
				window = progressWindow;
				dispatcher = window;
			}
			else if (MainWindow != null) {
				window = MainWindow;
				dispatcher = window;
			}
			else {
				dispatcher = Application.Current;
			}
			dispatcher.Dispatcher.Invoke(() => {
				ErrorLogWindow.Show(window, errorLog.ToArray());
				errorLog.Clear();
			});
		}

		public static void LogWarning(string message, string reason = "") {
			errorLog.Add(new LogError(true, message, reason));
			ErrorLogger.WriteLine("Warning: " + message);
			if (reason != String.Empty)
				ErrorLogger.WriteLine("    Reason: " + reason);
		}
		public static void LogError(string message, string reason = "") {
			errorLog.Add(new LogError(false, message, reason));
			ErrorLogger.WriteLine("Error: " + message);
			if (reason != String.Empty)
				ErrorLogger.WriteLine("    Reason: " + reason);
		}

		public static void ProcessDropFiles(string[] extractFiles, string[] convertFiles, string[] scriptFiles) {
			List<Script> scripts = new List<Script>();

			foreach (string scriptFile in scriptFiles) {
				Script script = LoadScript(scriptFile);
				if (script != null) {
					scripts.Add(script);
					totalFiles += script.Extracts.Count + script.Converts.Count;
					foreach (PathIOPair backup in script.Backups) {
						totalFiles += Helpers.GetFileCount(backup.InPath);
					}
					foreach (PathIOPair restore in script.Restores) {
						totalFiles += Helpers.GetFileCount(restore.InPath);
					}
				}
			}
			totalFiles += extractFiles.Length + convertFiles.Length;

			if (extractFiles.Length != 0)
				ExtractDropFiles(extractFiles);
			if (convertFiles.Length != 0)
				ConvertDropFiles(convertFiles);
			foreach (Script script in scripts) {
				RunScript(script, false);
			}
			FinishProgress("Finished Dropping Files");
		}

		#endregion
		//========== EXTRACTING ==========
		#region Extracting

		public static void ExtractAll(string inputDirectory, string outputDirectory, bool includeImages = true, bool includeSounds = true, bool includeWaveBank = true) {
			string[] files = Helpers.FindAllFiles(inputDirectory);
			totalFiles += files.Length;

			int extractCount = 0;
			foreach (string inputFile in files) {
				if (ExtractFile(inputFile, inputDirectory, outputDirectory, includeImages, includeSounds, includeWaveBank))
					extractCount++;
			}

			FinishProgress("Finished Extracting " + extractCount + " Files");
		}
		public static void ExtractSingleFile(string inputFile, string outputFile) {
			totalFiles += 1;
			
			ExtractFile(inputFile, Path.GetDirectoryName(inputFile), Path.GetDirectoryName(outputFile));

			FinishProgress("Finished Extracting");
		}
		public static void ExtractDropFiles(string[] inputFiles) {

			foreach (string inputFile in inputFiles) {
				string inputDirectory = Path.GetDirectoryName(inputFile);
				ExtractFile(inputFile, inputDirectory, inputDirectory);
			}

			FinishProgress("Finished Extracting", false);
		}
		public static bool ExtractFile(string inputFile, string inputDirectory, string outputDirectory, bool includeImages = true, bool includeSounds = true, bool includeWaveBank = true) {
			bool extracted = false;
			try {
				string outputFile = Helpers.GetOutputPath(inputFile, inputDirectory, outputDirectory);
				string ext = Path.GetExtension(inputFile).ToLower();
				if ((ext == ".xnb" && (includeImages || includeSounds)) || (ext == ".xwb" && includeWaveBank)) {
					UpdateProgress("Extracting: " + Helpers.GetRelativePath(inputFile, inputDirectory), ext == ".xwb");
				}
				if (ext == ".xnb" && (includeImages || includeSounds)) {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (XnbExtractor.Extract(inputFile, outputFile, true, includeImages, includeSounds))
						extracted = true;
				}
				else if (ext == ".xwb" && includeWaveBank) {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (XactExtractor.Extract(inputFile, Path.GetDirectoryName(outputFile)))
						extracted = true;
				}
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Extracting: " + inputFile, "Unauthorized access (" + ex.Message + ")");
			}
			catch (FileNotFoundException ex) {
				LogError("Extracting: " + inputFile, "File not found (" + ex.Message + ")");
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Extracting: " + inputFile, "Directory not found (" + ex.Message + ")");
			}
			catch (IOException ex) {
				LogError("Extracting: " + inputFile, "IO error (" + ex.Message + ")");
			}
			catch (XnbException ex) {
				LogError("Extracting: " + inputFile, "Xnb error (" + ex.Message + ")");
			}
			catch (Exception ex) {
				LogError("Extracting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return extracted;
		}

		public static bool ExtractFile2(string inputFile, string outputFile) {
			bool extracted = false;
			try {
				string ext = Path.GetExtension(inputFile).ToLower();
				if (ext == ".xnb" || ext == ".xwb") {
					UpdateProgress("Extracting: " + Path.GetFileName(inputFile), ext == ".xwb");
				}
				if (ext == ".xnb") {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (XnbExtractor.Extract(inputFile, outputFile, true, true, true))
						extracted = true;
				}
				else if (ext == ".xwb") {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (XactExtractor.Extract(inputFile, Path.GetDirectoryName(outputFile)))
						extracted = true;
				}
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Extracting: " + inputFile, "Unauthorized access (" + ex.Message + ")");
			}
			catch (FileNotFoundException ex) {
				LogError("Extracting: " + inputFile, "File not found (" + ex.Message + ")");
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Extracting: " + inputFile, "Directory not found (" + ex.Message + ")");
			}
			catch (IOException ex) {
				LogError("Extracting: " + inputFile, "IO error (" + ex.Message + ")");
			}
			catch (XnbException ex) {
				LogError("Extracting: " + inputFile, "Xnb error (" + ex.Message + ")");
			}
			catch (Exception ex) {
				LogError("Extracting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return extracted;
		}

		#endregion
		//========== CONVERTING ==========
		#region Converting

		public static void ConvertAll(string inputDirectory, string outputDirectory, bool includeImages, bool includeSounds) {
			string[] files = Helpers.FindAllFiles(inputDirectory);
			totalFiles += files.Length;

			int convertCount = 0;
			foreach (string inputFile in files) {
				if (ConvertFile(inputFile, inputDirectory, outputDirectory, includeImages, includeSounds))
					convertCount++;
			}

			FinishProgress("Finished Converting " + convertCount + " Files");
		}
		public static void ConvertSingleFile(string inputFile, string outputFile) {
			totalFiles += 1;
			
			ConvertFile(inputFile, Path.GetDirectoryName(inputFile), Path.GetDirectoryName(outputFile));

			FinishProgress("Finished Converting");
		}
		public static void ConvertDropFiles(string[] inputFiles) {

			foreach (string inputFile in inputFiles) {
				string inputDirectory = Path.GetDirectoryName(inputFile);
				ConvertFile(inputFile, inputDirectory, inputDirectory);
			}

			FinishProgress("Finished Converting", false);
		}
		public static bool ConvertFile(string inputFile, string inputDirectory, string outputDirectory, bool includeImages = true, bool includeSounds = true) {
			bool converted = false;
			try {
				string outputFile = Helpers.GetOutputPath(inputFile, inputDirectory, outputDirectory);
				string ext = Path.GetExtension(inputFile).ToLower();
				if (((ext == ".png" || ext == ".bmp" || ext == ".jpg") && includeImages) || (ext == ".wav" && includeSounds)) {
					UpdateProgress("Converting: " + Helpers.GetRelativePath(inputFile, inputDirectory));
				}
				if ((ext == ".png" || ext == ".bmp" || ext == ".jpg") && includeImages) {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (PngConverter.Convert(inputFile, outputFile, true, XCompress.IsAvailable, true))
						converted = true;
				}
				else if (ext == ".wav" && includeSounds) {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (WavConverter.Convert(inputFile, outputFile, true))
						converted = true;
				}
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Converting: " + inputFile, "Unauthorized access (" + ex.Message + ")");
			}
			catch (FileNotFoundException ex) {
				LogError("Converting: " + inputFile, "File not found (" + ex.Message + ")");
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Converting: " + inputFile, "Directory not found (" + ex.Message + ")");
			}
			catch (IOException ex) {
				LogError("Converting: " + inputFile, "IO error (" + ex.Message + ")");
			}
			catch (PngException ex) {
				LogError("Converting: " + inputFile, "Png error (" + ex.Message + ")");
			}
			catch (WavException ex) {
				LogError("Converting: " + inputFile, "Wav error (" + ex.Message + ")");
			}
			catch (Exception ex) {
				LogError("Converting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return converted;
		}
		public static bool ConvertFile2(string inputFile, string outputFile) {
			bool converted = false;
			try {
				string ext = Path.GetExtension(inputFile).ToLower();
				if (ext == ".png" || ext == ".bmp" || ext == ".jpg" || ext == ".wav") {
					UpdateProgress("Converting: " + Path.GetFileName(inputFile));
				}
				if (ext == ".png" || ext == ".bmp" || ext == ".jpg") {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (PngConverter.Convert(inputFile, outputFile, false, XCompress.IsAvailable, true))
						converted = true;
				}
				else if (ext == ".wav") {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (WavConverter.Convert(inputFile, outputFile, false))
						converted = true;
				}
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Converting: " + inputFile, "Unauthorized access (" + ex.Message + ")");
			}
			catch (FileNotFoundException ex) {
				LogError("Converting: " + inputFile, "File not found (" + ex.Message + ")");
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Converting: " + inputFile, "Directory not found (" + ex.Message + ")");
			}
			catch (IOException ex) {
				LogError("Converting: " + inputFile, "IO error (" + ex.Message + ")");
			}
			catch (PngException ex) {
				LogError("Converting: " + inputFile, "Png error (" + ex.Message + ")");
			}
			catch (WavException ex) {
				LogError("Converting: " + inputFile, "Wav error (" + ex.Message + ")");
			}
			catch (Exception ex) {
				LogError("Converting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return converted;
		}

		#endregion
		//========== SCRIPTING ===========
		#region Scripting

		public static void RunScript(string inputScript) {
			UpdateProgress("Loading Script...", true);

			Script script = LoadScript(inputScript);

			// Add up all the files and restores
			totalFiles += script.Extracts.Count + script.Converts.Count;
			foreach (PathIOPair backup in script.Backups) {
				totalFiles += Helpers.GetFileCount(backup.InPath);
			}
			foreach (PathIOPair restore in script.Restores) {
				totalFiles += Helpers.GetFileCount(restore.InPath);
			}

			if (script != null) {
				RunScript(script);
			}
			else {
				FinishProgress("Finished Script");
			}
		}
		public static void RunScript(Script script, bool final = true) {

			int backupCount = 0;
			foreach (PathIOPair backup in script.Backups) {
				if (!Directory.Exists(backup.InPath)) {
					LogError("Backing Up: " + backup.InPath, "Directory does not exist");
					continue;
				}
				string[] backupFiles = Helpers.FindAllFiles(backup.InPath);

				foreach (string inputFile in backupFiles) {
					if (BackupFile(inputFile, backup.InPath, backup.OutPath))
						backupCount++;
				}
			}

			int restoreCount = 0;
			foreach (PathIOPair restore in script.Restores) {
				if (!Directory.Exists(restore.InPath)) {
					LogError("Restoring: " + restore.InPath, "Directory does not exist");
					continue;
				}
				string[] restoreFiles = Helpers.FindAllFiles(restore.InPath);
				
				foreach (string inputFile in restoreFiles) {
					if (RestoreFile(inputFile, restore.InPath, restore.OutPath))
						restoreCount++;
				}
			}

			int extractCount = 0;
			foreach (PathIOPair file in script.Extracts) {
				if (ExtractFile2(file.InPath, file.OutPath))
					extractCount++;
			}

			int convertCount = 0;
			foreach (PathIOPair file in script.Converts) {
				if (ConvertFile2(file.InPath, file.OutPath))
					convertCount++;
			}
			string message = "Finished ";
			if (extractCount > 0) {
				if (convertCount > 0)
					message += "Extracting and Converting " + (extractCount + convertCount) + " Files";
				else
					message += "Extracting " + extractCount + " Files";
			}
			else if (convertCount > 0)
				message += "Converting " + convertCount + " Files";
			else if (restoreCount > 0)
				message += "Restoring " + restoreCount + " Files";
			else if (backupCount > 0)
				message += "Backing Up " + backupCount + " Files";
			else
				message += "Script";
			FinishProgress(message, final);
		}
		public static Script LoadScript(string inputScript) {
			List<PathIOPair> files = new List<PathIOPair>();
			List<PathIOPair> backups = new List<PathIOPair>();
			List<PathIOPair> restores = new List<PathIOPair>();

			try {
				Directory.SetCurrentDirectory(Path.GetDirectoryName(inputScript));
			}
			catch (Exception ex) {
				LogError("Setting working directory failed", ex.Message);
				FinishProgress("Finished Script");
				return null;
			}
			XmlDocument doc = new XmlDocument();
			try {
				doc.Load(inputScript);
			}
			catch (XmlException ex) {
				LogError("Failed to parse script: " + inputScript, ex.Message);
				FinishProgress("Finished Script");
				return null;
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Reading Script: " + inputScript, "Unauthorized access (" + ex.Message + ")");
				FinishProgress("Finished Script");
				return null;
			}
			catch (FileNotFoundException ex) {
				LogError("Reading Script: " + inputScript, "File not found (" + ex.Message + ")");
				FinishProgress("Finished Script");
				return null;
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Reading Script: " + inputScript, "Directory not found (" + ex.Message + ")");
				FinishProgress("Finished Script");
				return null;
			}
			catch (IOException ex) {
				LogError("Reading Script: " + inputScript, "IO error (" + ex.Message + ")");
				FinishProgress("Finished Script");
				return null;
			}
			catch (Exception ex) {
				LogError("Reading Script: " + inputScript, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
				FinishProgress("Finished Script");
				return null;
			}

			XmlElement root = doc["ConvertScript"];

			// Find all the files and restores
			if (root != null) {
				LoadScriptFolder(root, files, backups, restores, "", "", true);
			}
			else {
				LogError("Reading Script", "No root ConvertScript");
				FinishProgress("Finished Script");
				return null;
			}

			List<PathIOPair> extracts = new List<PathIOPair>();
			List<PathIOPair> converts = new List<PathIOPair>();
			foreach (PathIOPair file in files) {
				string ext = Path.GetExtension(file.InPath).ToLower();
				switch (ext) {
				case ".xnb": case ".xwb": extracts.Add(file); break;
				default: converts.Add(file); break;
				}
			}
			
			return new Script { Extracts=extracts, Converts=converts, Backups=backups, Restores=restores };
		}
		private static void LoadScriptFolder(XmlElement element, List<PathIOPair> files, List<PathIOPair> backups, List<PathIOPair> restores, string output, string path, bool isRoot = false) {
			string newOutput = output;
			XmlAttribute attribute;
			foreach (XmlElement next in element) {
				switch (next.Name) {
				case "Backup":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						string nextPath;
						if (path == string.Empty)
							nextPath = attribute.InnerText;
						else
							nextPath = Path.Combine(path, attribute.InnerText);
						backups.Add(new PathIOPair { InPath=nextPath, OutPath=newOutput });
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Backup.");
					}
					break;
				case "Restore":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						string nextPath;
						if (path == string.Empty)
							nextPath = attribute.InnerText;
						else
							nextPath = Path.Combine(path, attribute.InnerText);
						restores.Add(new PathIOPair { InPath=nextPath, OutPath=newOutput });
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Restore.");
					}
					break;
				case "Output":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (output == string.Empty)
							newOutput = attribute.InnerText;
						else
							newOutput = Path.Combine(output, attribute.InnerText);
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Output.");
					}
					break;
				case "Folder":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						string nextPath;
						if (path == string.Empty)
							nextPath = attribute.InnerText;
						else
							nextPath = Path.Combine(path, attribute.InnerText);
						LoadScriptFolder(next, files, backups, restores, newOutput, nextPath);
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Folder.");
					}
					break;
				case "File":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						string nextPath;
						if (path == string.Empty)
							nextPath = attribute.InnerText;
						else
							nextPath = Path.Combine(path, attribute.InnerText);
						attribute = next.Attributes["OutPath"];
						if (attribute != null) {
							string nextOutput;
							if (newOutput == string.Empty)
								nextOutput = attribute.InnerText;
							else
								nextOutput = Path.Combine(newOutput, attribute.InnerText);
							if (!Path.HasExtension(nextOutput))
								nextOutput = Path.ChangeExtension(nextOutput, ".xnb");
							files.Add(new PathIOPair { InPath=nextPath, OutPath=nextOutput });
						}
						LoadScriptFile(next, files, newOutput, nextPath);
					}
					else {
						LogWarning("Reading Script", "No Path attribute in File.");
					}
					break;
				default:
					LogWarning("Reading Script", "Invalid element inside " + (isRoot ? "ConvertScript" : "Folder") + " '" + next.Name + "'.");
					break;
				}
			}
		}
		private static void LoadScriptFile(XmlElement element, List<PathIOPair> files, string output, string path) {
			string newOutput = output;
			XmlAttribute attribute;
			foreach (XmlElement next in element) {
				switch (next.Name) {
				case "Output":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (output == string.Empty)
							newOutput = attribute.InnerText;
						else
							newOutput = Path.Combine(output, attribute.InnerText);
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Output.");
					}
					break;
				case "Out":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						string nextOutput;
						if (newOutput == string.Empty)
							nextOutput = attribute.InnerText;
						else
							nextOutput = Path.Combine(newOutput, attribute.InnerText);
						if (!Path.HasExtension(nextOutput))
							nextOutput = Path.ChangeExtension(nextOutput, ".xnb");
						files.Add(new PathIOPair { InPath=path, OutPath=nextOutput });
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Out");
					}
					break;
				default:
					LogWarning("Reading Script", "Invalid element inside File '" + next.Name + "'.");
					break;
				}
			}
		}

		#endregion
		//============ BACKUP ============
		#region Backup

		public static void BackupFiles(string inputDirectory, string outputDirectory) {
			string[] files = Helpers.FindAllFiles(inputDirectory);
			totalFiles = files.Length;

			foreach (string inputFile in files) {
				BackupFile(inputFile, inputDirectory, outputDirectory);
			}

			FinishProgress("Finished Backing Up " + files.Length + " Files");
		}
		public static void RestoreFiles(string inputDirectory, string outputDirectory) {
			string[] files = Helpers.FindAllFiles(inputDirectory);
			totalFiles = files.Length;

			int restoreCount = 0;
			foreach (string inputFile in files) {
				if (RestoreFile(inputFile, inputDirectory, outputDirectory))
					restoreCount++;
			}
			
			FinishProgress("Finished Restoring " + restoreCount + " Files");
		}
		public static bool BackupFile(string inputFile, string inputDirectory, string outputDirectory) {
			bool backedUp = false;
			try {
				UpdateProgress("Backing Up: " + Helpers.GetRelativePath(inputFile, inputDirectory));
				
				string outputFile = Helpers.GetOutputPath(inputFile, inputDirectory, outputDirectory);
				Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
				File.Copy(inputFile, outputFile, true);
				backedUp = true;
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Backing up: " + inputFile, "Unauthorized access (" + ex.Message + ")");
			}
			catch (FileNotFoundException ex) {
				LogError("Backing up: " + inputFile, "File not found (" + ex.Message + ")");
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Backing up: " + inputFile, "Directory not found (" + ex.Message + ")");
			}
			catch (IOException ex) {
				LogError("Backing up: " + inputFile, "IO error (" + ex.Message + ")");
			}
			catch (Exception ex) {
				LogError("Backing up: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return backedUp;
		}
		public static bool RestoreFile(string inputFile, string inputDirectory, string outputDirectory) {
			bool filedCopied = false;
			try {
				UpdateProgress("Restoring: " + Helpers.GetRelativePath(inputFile, inputDirectory));
				
				string outputFile = Helpers.GetOutputPath(inputFile, inputDirectory, outputDirectory);
				bool shouldCopy = true;
				Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
				if (File.Exists(outputFile)) {
					FileInfo info1 = new FileInfo(inputFile);
					FileInfo info2 = new FileInfo(outputFile);
					shouldCopy = info1.LastWriteTime != info2.LastWriteTime || info1.Length != info2.Length;
				}
				if (shouldCopy) {
					File.Copy(inputFile, outputFile, true);
					filedCopied = true;
				}
			}
			catch (UnauthorizedAccessException ex) {
				LogError("Restoring: " + inputFile, "Unauthorized access (" + ex.Message + ")");
			}
			catch (FileNotFoundException ex) {
				LogError("Restoring: " + inputFile, "File not found (" + ex.Message + ")");
			}
			catch (DirectoryNotFoundException ex) {
				LogError("Restoring: " + inputFile, "Directory not found (" + ex.Message + ")");
			}
			catch (IOException ex) {
				LogError("Restoring: " + inputFile, "IO error (" + ex.Message + ")");
			}
			catch (Exception ex) {
				LogError("Restoring: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return filedCopied;
		}

		#endregion
	}
}
