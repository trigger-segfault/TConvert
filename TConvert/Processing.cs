using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Xml;
using TConvert.Convert;
using TConvert.Extract;
using TConvert.Util;
#if !(CONSOLE)
using TConvert.Windows;
#endif

namespace TConvert {
	/**<summary>A log error.</summary>*/
	public struct LogError {
		/**<summary>True if the log item is a warning and not an error.</summary>*/
		public bool IsWarning;
		/**<summary>The log message.</summary>*/
		public string Message;
		/**<summary>The error/warning reason.</summary>*/
		public string Reason;
		/**<summary>Constructs a log error.</summary>*/
		public LogError(bool isWarning, string message, string reason) {
			IsWarning = isWarning;
			Message = message;
			Reason = reason;
		}
	}
	/**<summary>A pair of input and output paths.</summary>*/
	public struct PathPair {
		/**<summary>The input path.</summary>*/
		public string Input;
		/**<summary>The output path.</summary>*/
		public string Output;
		/**<summary>True if compression should be used.</summary>*/
		public bool Compress;
		/**<summary>True if alpha is premultiplied.</summary>*/
		public bool Premultiply;
		/**<summary>Constructs a path pair.</summary>*/
		public PathPair(string input, string output) {
			Input = input;
			Output = output;
			Compress = false;
			Premultiply = true;
		}
		/**<summary>Constructs a path pair.</summary>*/
		public PathPair(string input, string output, bool compress, bool premultiply) {
			Input = input;
			Output = output;
			Compress = compress;
			Premultiply = premultiply;
		}
	}
	/**<summary>A loaded script.</summary>*/
	public class Script {
		/**<summary>The list of backup directories.</summary>*/
		public List<PathPair> Backups;
		/**<summary>The list of restore directories.</summary>*/
		public List<PathPair> Restores;
		/**<summary>The list of extract files.</summary>*/
		public List<PathPair> Extracts;
		/**<summary>The list of convert files.</summary>*/
		public List<PathPair> Converts;
	}
	/**<summary>The process modes available.</summary>*/
	public enum ProcessModes {
		Any,
		Extract,
		Convert,
		Backup,
		Restore,
		Script
	}
	/**<summary>Processes file requests.</summary>*/
	public static class Processing {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The duration before updating the progress again.</summary>*/
		private static readonly TimeSpan UpdateSpan = TimeSpan.FromMilliseconds(50);

		#endregion
		//=========== MEMBERS ============
		#region Members
		//--------------------------------
		#region Processing

		/**<summary>The lasy time the progress was updated.</summary>*/
		private static DateTime lastUpdate = DateTime.MinValue;
		/**<summary>The total number of files to process.</summary>*/
		private static int totalFiles = 0;
		/**<summary>The number of files completed.</summary>*/
		private static int filesCompleted = 0;
		/**<summary>The list of errors and warnings that occurred.</summary>*/
		private static List<LogError> errorLog = new List<LogError>();
		/**<summary>True if an error occurred.</summary>*/
		//private static bool errorOccurred = false;
		/**<summary>True if an warning occurred.</summary>*/
		//private static bool warningOccurred = false;
		/**<summary>True if images should be compressed.</summary>*/
		private static bool compressImages = true;
		/**<summary>True if a sound is played upon completion.</summary>*/
		private static bool completionSound;
		/**<summary>True if alpha is premultiplied when converting back to xnb.</summary>*/
		private static bool premultiplyAlpha = true;

		#endregion
		//--------------------------------
		#region Console Only

		/**<summary>The starting X position of the console output.</summary>*/
		private static int consoleX;
		/**<summary>The starting Y position of the console output.</summary>*/
		private static int consoleY;
		/**<summary>True if there's no console output.</summary>*/
		private static bool silent;
		/**<summary>The start time of the console operation.</summary>*/
		private static DateTime startTime;

		#endregion
		//--------------------------------
		#region Window Only

#if !(CONSOLE)
		private static ProgressWindow progressWindow;
		private static bool autoCloseProgress;
		private static bool console = false;
#endif

		#endregion
		//--------------------------------
		#endregion
		//=========== STARTING ===========
		#region Starting

#if !(CONSOLE)
		/**<summary>Starts a progress window processing thread.</summary>*/
		public static void StartProgressThread(Window owner, string message, bool autoClose, bool compress, bool sound, bool premultiply, Thread thread) {
			console = false;
			compressImages = compress;
			premultiplyAlpha = premultiply;
			completionSound = sound;
			lastUpdate = DateTime.MinValue;
			autoCloseProgress = autoClose;
			filesCompleted = 0;
			totalFiles = 0;
			errorLog.Clear();
			progressWindow = new ProgressWindow(thread, OnProgressCancel);
			if (owner != null) {
				progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				progressWindow.Owner = owner;
			}
			if (Application.Current.MainWindow == null)
				Application.Current.MainWindow = progressWindow;
			// Prevent Explorer from freazing until the progress window is closed
			Thread showThread = new Thread(() => {
				Application.Current.Dispatcher.Invoke(() => {
					progressWindow.ShowDialog();
				});
			});
			showThread.Start();
		}
#endif
		/**<summary>Starts a console processing thread.</summary>*/
		public static void StartConsoleThread(string message, bool silent, bool compress, bool sound, bool premultiply, Thread thread) {
			#if !(CONSOLE)
			console = true;
			#endif
			compressImages = compress;
			premultiplyAlpha = premultiply;
			completionSound = sound;
			Processing.silent = silent;
			startTime = DateTime.Now;
			lastUpdate = DateTime.MinValue;
			filesCompleted = 0;
			totalFiles = 0;
			errorLog.Clear();
			consoleX = Console.CursorLeft;
			consoleY = Console.CursorTop;
			WriteTimeAndPercentage(message);
			thread.Start();
			// Wait for the thread to finish
			thread.Join();
			if (!silent)
				Console.WriteLine();
			#if !(CONSOLE)
			Console.Write("Press enter to continue...");
			#endif
		}

		#endregion
		//=========== PROGRESS ===========
		#region Progress

		/**<summary>Updates the progress on the console.</summary>*/
		private static void WriteTimeAndPercentage(string message, bool finished = false) {
			if (!silent) {
				// Prepare to overwrite the leftover message
				int oldX = Console.CursorLeft;
				int oldY = Console.CursorTop;
				int oldXY = oldY * Console.BufferWidth + oldX;
				if (!Console.IsOutputRedirected) {
					Console.SetCursorPosition(consoleX, consoleY);
				}

				string timeStr = (finished ? "Total " : "");
				timeStr += "Time: " + (DateTime.Now - startTime).ToString(@"m\:ss");
				timeStr += " (" + (int)(totalFiles == 0 ? 0 : ((double)filesCompleted / totalFiles * 100)) + "%)";
				timeStr += "      ";
				Console.WriteLine(timeStr);
				Console.Write(message);

				// Overwrite the leftover message
				if (!Console.IsOutputRedirected) {
					int newX = Console.CursorLeft;
					int newY = Console.CursorTop;
					int newXY = newY * Console.BufferWidth + newX;
					if (newXY < oldXY) {
						Console.Write(new string(' ',
							(oldY - newY) * Console.BufferWidth + (oldX - newX)
						));
					}
				}
			}
		}
		/**<summary>Called when the progress window is canceled.</summary>*/
		private static void OnProgressCancel() {
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			ErrorLogger.Close();
			if (errorLog.Count > 0) {
				#if !(CONSOLE)
				progressWindow = null;
				#endif
				ShowErrorLog();
				errorLog.Clear();
			}
		}
		/**<summary>Called to update the progress with a message.</summary>*/
		public static void UpdateProgress(string message, bool forceUpdate = false) {
			#if !(CONSOLE)
			if (progressWindow != null) {
				if (lastUpdate + UpdateSpan < DateTime.Now || forceUpdate) {
					progressWindow.Dispatcher.Invoke(() => {
						progressWindow.Update(message, totalFiles == 0 ? 0 : ((double)filesCompleted / totalFiles));
					});
					lastUpdate = DateTime.Now;
				}
			}
			else if (console)
			#endif
			{
				if (lastUpdate + UpdateSpan < DateTime.Now || forceUpdate) {
					WriteTimeAndPercentage(message);
					lastUpdate = DateTime.Now;
				}
			}
		}
		/**<summary>Called to finish the progress with a message.</summary>*/
		public static void FinishProgress(string message) {
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			#if !(CONSOLE)
			if (progressWindow != null) {
				progressWindow.Dispatcher.Invoke(() => {
					progressWindow.Finish(message, errorLog.Count > 0);
				});
				ErrorLogger.Close();
				if (errorLog.Count > 0) {
					if (completionSound)
						SystemSounds.Exclamation.Play();
					ShowErrorLog();
					errorLog.Clear();
				}
				else if (completionSound) {
					SystemSounds.Asterisk.Play();
				}
				if (autoCloseProgress) {
					progressWindow.Dispatcher.Invoke(() => {
						progressWindow.Close();
					});
				}
			}
			else if (console)
			#endif
			{
				WriteTimeAndPercentage(message);
				ErrorLogger.Close();
				if (errorLog.Count > 0) {
					ShowErrorLog();
					errorLog.Clear();
				}
			}
		}
		/**<summary>Shows the error log if necissary.</summary>*/
		public static void ShowErrorLog() {
			#if !(CONSOLE)
			if (!console) {
				App.Current.Dispatcher.Invoke(() => {
					DispatcherObject dispatcher;
					Window window = null;
					if (progressWindow != null && !autoCloseProgress) {
						window = progressWindow;
						dispatcher = window;
					}
					else if (App.Current.MainWindow != null) {
						window = App.Current.MainWindow;
						dispatcher = window;
					}
					else {
						dispatcher = Application.Current;
					}
					ErrorLogWindow.Show(window, errorLog.ToArray());
					errorLog.Clear();
				});
			}
			else
			#endif
			{
				if (!silent) {
					ConsoleColor oldColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Errors or warnings were encountered during the process.\nSee '" + Path.GetFileName(ErrorLogger.LogPath) + "' for more details.");
					Console.ForegroundColor = oldColor;
				}
			}
		}
		/**<summary>Logs an error.</summary>*/
		public static void LogError(string message, string reason = "") {
			errorLog.Add(new LogError(false, message, reason));
			ErrorLogger.WriteLine("Error: " + message);
			if (reason != String.Empty)
				ErrorLogger.WriteLine("    Reason: " + reason);
		}
		/**<summary>Logs a warning.</summary>*/
		public static void LogWarning(string message, string reason = "") {
			errorLog.Add(new LogError(true, message, reason));
			ErrorLogger.WriteLine("Warning: " + message);
			if (reason != String.Empty)
				ErrorLogger.WriteLine("    Reason: " + reason);
		}

		#endregion
		//========== PROCESSING ==========
		#region Processing


		/**<summary>Checks if the extension is that of an audio file.</summary>*/
		public static bool IsAudioExtension(string ext) {
			switch (ext) {
			case ".wav":
			case ".mp3":
			case ".mp2":
			case ".mpga":
			case ".m4a":
			case ".aac":
			case ".flac":
			case ".ogg":
			case ".wma":
			case ".aif":
			case ".aiff":
			case ".aifc":
				return true;
			}
			return false;
		}
		/**<summary>Processes drop files.</summary>*/
		public static void ProcessDropFiles(string[] extractFiles, string[] convertFiles, string[] scriptFiles) {
			List<Script> scripts = new List<Script>();

			foreach (string scriptFile in scriptFiles) {
				Script script = LoadScript(scriptFile);
				if (script != null) {
					scripts.Add(script);
					totalFiles += script.Extracts.Count + script.Converts.Count;
					foreach (PathPair backup in script.Backups) {
						totalFiles += Helpers.GetFileCount(backup.Input);
					}
					foreach (PathPair restore in script.Restores) {
						totalFiles += Helpers.GetFileCount(restore.Input);
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
			FinishProgress("Finished Processing Files");
		}
		/**<summary>Processes console files.</summary>*/
		public static void ProcessFiles(ProcessModes mode, string[] inputFiles, string[] outputFiles) {
			List<PathPair> files = new List<PathPair>();

			// Allow processing of directories too
			for (int i = 0; i < inputFiles.Length; i++) {
				string input = inputFiles[i];
				string output = outputFiles[i];
				if (Directory.Exists(input)) {
					string[] dirFiles = Helpers.FindAllFiles(input);
					foreach (string dirFile in dirFiles) {
						files.Add(new PathPair(dirFile, Helpers.GetOutputPath(dirFile, input, output)));
					}
				}
				else {
					files.Add(new PathPair(input, output));
				}
			}

			if (mode != ProcessModes.Backup && mode != ProcessModes.Restore) {
				List<PathPair> extractFiles = new List<PathPair>();
				List<PathPair> convertFiles = new List<PathPair>();
				List<string> scriptFiles = new List<string>();

				foreach (PathPair pair in files) {
					string ext = Path.GetExtension(pair.Input).ToLower();
					switch (ext) {
					case ".xnb":
					case ".xwb":
						if (mode == ProcessModes.Any || mode == ProcessModes.Extract)
							extractFiles.Add(pair);
						break;
					case ".png":
					case ".bmp":
					case ".jpg":
						if (mode == ProcessModes.Any || mode == ProcessModes.Convert)
							convertFiles.Add(pair);
						break;
					case ".xml":
						if (mode == ProcessModes.Any || mode == ProcessModes.Script)
							scriptFiles.Add(pair.Input);
						break;
					default:
						if (IsAudioExtension(ext) && (mode == ProcessModes.Any || mode == ProcessModes.Convert))
							convertFiles.Add(pair);
						break;
					}
				}
				
				List<Script> scripts = new List<Script>();
				foreach (string scriptFile in scriptFiles) {
					Script script = LoadScript(scriptFile);
					if (script != null) {
						scripts.Add(script);
						totalFiles += script.Extracts.Count + script.Converts.Count;
						foreach (PathPair backup in script.Backups) {
							totalFiles += Helpers.GetFileCount(backup.Input);
						}
						foreach (PathPair restore in script.Restores) {
							totalFiles += Helpers.GetFileCount(restore.Input);
						}
					}
				}
				totalFiles += extractFiles.Count + convertFiles.Count;

				foreach (var pair in extractFiles) {
					ExtractFile2(pair.Input, pair.Output);
				}
				foreach (var pair in convertFiles) {
					ConvertFile2(pair.Input, pair.Output, compressImages);
				}
				foreach (Script script in scripts) {
					RunScript(script, false);
				}
			}
			else if (mode == ProcessModes.Backup) {
				foreach (var pair in files) {
					BackupFile2(pair.Input, pair.Output);
				}
			}
			else if (mode == ProcessModes.Restore) {
				foreach (var pair in files) {
					RestoreFile2(pair.Input, pair.Output);
				}
			}
			
			FinishProgress("Finished Processing Files");
		}

		#endregion
		//========== EXTRACTING ==========
		#region Extracting

		/**<summary>Extracts all files in a directory.</summary>*/
		public static void ExtractAll(string inputDirectory, string outputDirectory, bool includeImages, bool includeSounds, bool includeFonts, bool includeWaveBank) {
			string[] files = Helpers.FindAllFiles(inputDirectory);
			totalFiles += files.Length;

			int extractCount = 0;
			foreach (string inputFile in files) {
				if (ExtractFile(inputFile, inputDirectory, outputDirectory, includeImages, includeSounds, includeFonts, includeWaveBank))
					extractCount++;
			}

			FinishProgress("Finished Extracting " + extractCount + " Files");
		}
		/**<summary>Extracts a single file.</summary>*/
		public static void ExtractSingleFile(string inputFile, string outputFile) {
			totalFiles += 1;
			
			ExtractFile(inputFile, Path.GetDirectoryName(inputFile), Path.GetDirectoryName(outputFile));

			FinishProgress("Finished Extracting");
		}
		/**<summary>Extracts drop files.</summary>*/
		private static void ExtractDropFiles(string[] inputFiles) {

			foreach (string inputFile in inputFiles) {
				string inputDirectory = Path.GetDirectoryName(inputFile);
				ExtractFile(inputFile, inputDirectory, inputDirectory);
			}

			UpdateProgress("Finished Extracting", true);
		}
		/**<summary>Extracts a file.</summary>*/
		private static bool ExtractFile(string inputFile, string inputDirectory, string outputDirectory, bool includeImages = true, bool includeSounds = true, bool includeFonts = true, bool includeWaveBank = true) {
			bool extracted = false;
			try {
				string outputFile = Helpers.GetOutputPath(inputFile, inputDirectory, outputDirectory);
				string ext = Path.GetExtension(inputFile).ToLower();
				if ((ext == ".xnb" && (includeImages || includeSounds || includeFonts)) || (ext == ".xwb" && includeWaveBank)) {
					UpdateProgress("Extracting: " + Helpers.GetRelativePath(inputFile, inputDirectory), ext == ".xwb");
				}
				if (ext == ".xnb" && (includeImages || includeSounds || includeFonts)) {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (XnbExtractor.Extract(inputFile, outputFile, true, includeImages, includeSounds, includeFonts))
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Extracting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return extracted;
		}
		/**<summary>Extracts a file with different parameters.</summary>*/
		private static bool ExtractFile2(string inputFile, string outputFile) {
			bool extracted = false;
			try {
				string ext = Path.GetExtension(inputFile).ToLower();
				if (ext == ".xnb" || ext == ".xwb") {
					UpdateProgress("Extracting: " + Path.GetFileName(inputFile), ext == ".xwb");
				}
				if (ext == ".xnb") {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (XnbExtractor.Extract(inputFile, outputFile, true, true, true, true))
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Extracting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return extracted;
		}

		#endregion
		//========== CONVERTING ==========
		#region Converting

		/**<summary>Converts all files in a directory.</summary>*/
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
		/**<summary>Converts a single file.</summary>*/
		public static void ConvertSingleFile(string inputFile, string outputFile) {
			totalFiles += 1;
			
			ConvertFile(inputFile, Path.GetDirectoryName(inputFile), Path.GetDirectoryName(outputFile));

			FinishProgress("Finished Converting");
		}
		/**<summary>Converts drop files.</summary>*/
		private static void ConvertDropFiles(string[] inputFiles) {

			foreach (string inputFile in inputFiles) {
				string inputDirectory = Path.GetDirectoryName(inputFile);
				ConvertFile(inputFile, inputDirectory, inputDirectory);
			}

			UpdateProgress("Finished Converting", true);
		}
		/**<summary>Converts a file.</summary>*/
		private static bool ConvertFile(string inputFile, string inputDirectory, string outputDirectory, bool includeImages = true, bool includeSounds = true) {
			bool converted = false;
			try {
				string outputFile = Helpers.GetOutputPath(inputFile, inputDirectory, outputDirectory);
				string ext = Path.GetExtension(inputFile).ToLower();
				if (((ext == ".png" || ext == ".bmp" || ext == ".jpg") && includeImages) || (IsAudioExtension(ext) && includeSounds)) {
					UpdateProgress("Converting: " + Helpers.GetRelativePath(inputFile, inputDirectory));
				}
				if ((ext == ".png" || ext == ".bmp" || ext == ".jpg") && includeImages) {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (PngConverter.Convert(inputFile, outputFile, true, compressImages, true, premultiplyAlpha))
						converted = true;
				}
				else if (IsAudioExtension(ext) && includeSounds) {
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Converting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return converted;
		}
		/**<summary>Converts a file with different paramters.</summary>*/
		private static bool ConvertFile2(string inputFile, string outputFile, bool compress) {
			bool converted = false;
			try {
				string ext = Path.GetExtension(inputFile).ToLower();
				if (ext == ".png" || ext == ".bmp" || ext == ".jpg" || IsAudioExtension(ext)) {
					UpdateProgress("Converting: " + Path.GetFileName(inputFile));
				}
				if (ext == ".png" || ext == ".bmp" || ext == ".jpg") {
					Helpers.CreateDirectorySafe(Path.GetDirectoryName(outputFile));
					if (PngConverter.Convert(inputFile, outputFile, true, compress, true, premultiplyAlpha))
						converted = true;
				}
				else if (IsAudioExtension(ext)) {
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Converting: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return converted;
		}

		#endregion
		//========== SCRIPTING ===========
		#region Scripting

		/**<summary>Loads and runs a script.</summary>*/
		public static void RunScript(string inputScript) {
			UpdateProgress("Loading Script...", true);

			Script script = LoadScript(inputScript);

			// Add up all the files and restores
			totalFiles += script.Extracts.Count + script.Converts.Count;
			foreach (PathPair backup in script.Backups) {
				totalFiles += Helpers.GetFileCount(backup.Input);
			}
			foreach (PathPair restore in script.Restores) {
				totalFiles += Helpers.GetFileCount(restore.Input);
			}

			if (script != null) {
				RunScript(script);
			}
			else {
				FinishProgress("Finished Script");
			}
		}
		/**<summary>Runs a a preloaded script.</summary>*/
		public static void RunScript(Script script, bool final = true) {

			int backupCount = 0;
			foreach (PathPair backup in script.Backups) {
				if (!Directory.Exists(backup.Input)) {
					LogError("Backing Up: " + backup.Input, "Directory does not exist");
					continue;
				}
				string[] backupFiles = Helpers.FindAllFiles(backup.Input);

				foreach (string inputFile in backupFiles) {
					if (BackupFile(inputFile, backup.Input, backup.Output))
						backupCount++;
				}
			}

			int restoreCount = 0;
			foreach (PathPair restore in script.Restores) {
				if (!Directory.Exists(restore.Input)) {
					LogError("Restoring: " + restore.Input, "Directory does not exist");
					continue;
				}
				string[] restoreFiles = Helpers.FindAllFiles(restore.Input);
				
				foreach (string inputFile in restoreFiles) {
					if (RestoreFile(inputFile, restore.Input, restore.Output))
						restoreCount++;
				}
			}

			int extractCount = 0;
			foreach (PathPair file in script.Extracts) {
				if (ExtractFile2(file.Input, file.Output))
					extractCount++;
			}

			int convertCount = 0;
			foreach (PathPair file in script.Converts) {
				if (ConvertFile2(file.Input, file.Output, file.Compress))
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
			if (final)
				FinishProgress(message);
			else
				UpdateProgress(message, true);
		}
		/**<summary>Loads a script.</summary>*/
		public static Script LoadScript(string inputScript) {
			List<PathPair> files = new List<PathPair>();
			List<PathPair> backups = new List<PathPair>();
			List<PathPair> restores = new List<PathPair>();

			try {
				Directory.SetCurrentDirectory(Path.GetDirectoryName(inputScript));
			}
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Reading Script: " + inputScript, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
				FinishProgress("Finished Script");
				return null;
			}

			XmlElement root = doc["TConvertScript"];

			// Find all the files and restores
			if (root != null) {
				LoadScriptFolder(root, files, backups, restores, "", "", compressImages, premultiplyAlpha, true);
			}
			else {
				LogError("Reading Script", "No root element TConvertScript.");
				FinishProgress("Finished Script");
				return null;
			}

			List<PathPair> extracts = new List<PathPair>();
			List<PathPair> converts = new List<PathPair>();
			foreach (PathPair file in files) {
				string ext = Path.GetExtension(file.Input).ToLower();
				switch (ext) {
				case ".xnb": case ".xwb":
					extracts.Add(file); break;
				case ".png": case ".bmp": case ".jpg":
					converts.Add(file); break;
				default:
					if (IsAudioExtension(ext))
						converts.Add(file);
					break;
				}
			}
			
			return new Script { Extracts=extracts, Converts=converts, Backups=backups, Restores=restores };
		}
		/**<summary>Loads a script folder or root element.</summary>*/
		private static void LoadScriptFolder(XmlElement element, List<PathPair> files, List<PathPair> backups, List<PathPair> restores, string output, string path, bool compress, bool premultiply, bool isRoot = false) {
			string newOutput = output;
			bool newCompress = compress;
			bool newPremultiply = premultiply;
			XmlAttribute attribute;
			foreach (XmlNode nextNode in element) {
				XmlElement next = nextNode as XmlElement;
				if (next == null)
					continue;
				switch (next.Name) {
				case "Compress":
					attribute = next.Attributes["Value"];
					if (attribute != null) {
						bool nextCompress;
						if (bool.TryParse(attribute.InnerText, out nextCompress))
							newCompress = nextCompress;
						else
							LogWarning("Reading Script", "Failed to parse Value attribute in Compress: '" + attribute.InnerText + "'.");
					}
					else {
						LogWarning("Reading Script", "No Value attribute in Compress.");
					}
					break;
				case "Premultiply":
					attribute = next.Attributes["Value"];
					if (attribute != null) {
						bool nextPremultiply;
						if (bool.TryParse(attribute.InnerText, out nextPremultiply))
							newPremultiply = nextPremultiply;
						else
							LogWarning("Reading Script", "Failed to parse Value attribute in Premultiply: '" + attribute.InnerText + "'.");
					}
					else {
						LogWarning("Reading Script", "No Value attribute in Premultiply.");
					}
					break;
				case "Backup":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							string nextPath;
							if (path == string.Empty)
								nextPath = attribute.InnerText;
							else
								nextPath = Path.Combine(path, attribute.InnerText);
							backups.Add(new PathPair(nextPath, newOutput));
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in Backup: '" + attribute.InnerText + "'.");
						}
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Backup.");
					}
					break;
				case "Restore":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							string nextPath;
							if (path == string.Empty)
								nextPath = attribute.InnerText;
							else
								nextPath = Path.Combine(path, attribute.InnerText);
							restores.Add(new PathPair(nextPath, newOutput));
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in Restore: '" + attribute.InnerText + "'.");
						}
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Restore.");
					}
					break;
				case "Output":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							if (output == string.Empty)
								newOutput = attribute.InnerText;
							else
								newOutput = Path.Combine(output, attribute.InnerText);
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in Output: '" + attribute.InnerText + "'.");
						}
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Output.");
					}
					break;
				case "Folder":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							string nextPath;
							if (path == string.Empty)
								nextPath = attribute.InnerText;
							else
								nextPath = Path.Combine(path, attribute.InnerText);
							LoadScriptFolder(next, files, backups, restores, newOutput, nextPath, newCompress, newPremultiply);
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in Folder: '" + attribute.InnerText + "'.");
						}
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Folder.");
					}
					break;
				case "File":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							string nextPath;
							bool nextCompress = newCompress;
							bool nextPremultiply = true;
							if (path == string.Empty)
								nextPath = attribute.InnerText;
							else
								nextPath = Path.Combine(path, attribute.InnerText);
							attribute = next.Attributes["Compress"];
							if (attribute != null) {
								if (!bool.TryParse(attribute.InnerText, out nextCompress)) {
									LogWarning("Reading Script", "Failed to parse Compress attribute in File: '" + attribute.InnerText + "'.");
									nextCompress = newCompress;
								}
							}
							attribute = next.Attributes["Premultiply"];
							if (attribute != null) {
								if (!bool.TryParse(attribute.InnerText, out nextPremultiply)) {
									LogWarning("Reading Script", "Failed to parse Premultiply attribute in Out: '" + attribute.InnerText + "'.");
									nextPremultiply = true;
								}
							}
							attribute = next.Attributes["OutPath"];
							if (attribute != null) {
								if (Helpers.IsPathValid(attribute.InnerText)) {
									string nextOutput;
									if (newOutput == string.Empty)
										nextOutput = Helpers.FixPathSafe(attribute.InnerText);
									else
										nextOutput = Path.Combine(newOutput, attribute.InnerText);
									files.Add(new PathPair(nextPath, nextOutput, nextCompress, nextPremultiply));
								}
								else {
									LogWarning("Reading Script", "Invalid OutPath attribute in File: '" + attribute.InnerText + "'."); ;
								}
							}
							LoadScriptFile(next, files, newOutput, nextPath, nextCompress, nextPremultiply);
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in File: '" + attribute.InnerText + "'.");
						}
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
		/**<summary>Loads a script file.</summary>*/
		private static void LoadScriptFile(XmlElement element, List<PathPair> files, string output, string path, bool compress, bool premultiply) {
			string newOutput = output;
			bool newCompress = compress;
			bool newPremultiply = premultiply;
			XmlAttribute attribute;
			foreach (XmlNode nextNode in element) {
				XmlElement next = nextNode as XmlElement;
				if (next == null)
					continue;
				switch (next.Name) {
				case "Compress":
					attribute = next.Attributes["Value"];
					if (attribute != null) {
						bool nextCompress;
						if (bool.TryParse(attribute.InnerText, out nextCompress))
							newCompress = nextCompress;
						else
							LogWarning("Reading Script", "Failed to parse Value attribute in Compress: '" + attribute.InnerText + "'.");
					}
					else {
						LogWarning("Reading Script", "No Value attribute in Compress.");
					}
					break;
				case "Premultiply":
					attribute = next.Attributes["Value"];
					if (attribute != null) {
						bool nextPremultiply;
						if (bool.TryParse(attribute.InnerText, out nextPremultiply))
							newPremultiply = nextPremultiply;
						else
							LogWarning("Reading Script", "Failed to parse Value attribute in Premultiply: '" + attribute.InnerText + "'.");
					}
					else {
						LogWarning("Reading Script", "No Value attribute in Premultiply.");
					}
					break;
				case "Output":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							if (output == string.Empty)
								newOutput = attribute.InnerText;
							else
								newOutput = Path.Combine(output, attribute.InnerText);
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in Output: '" + attribute.InnerText + "'.");
						}
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Output.");
					}
					break;
				case "Out":
					attribute = next.Attributes["Path"];
					if (attribute != null) {
						if (Helpers.IsPathValid(attribute.InnerText)) {
							string nextOutput;
							bool nextCompress = newCompress;
							bool nextPremultiply = newPremultiply;
							if (newOutput == string.Empty)
								nextOutput = attribute.InnerText;
							else
								nextOutput = Path.Combine(newOutput, attribute.InnerText);
							attribute = next.Attributes["Compress"];
							if (attribute != null) {
								if (!bool.TryParse(attribute.InnerText, out nextCompress)) {
									LogWarning("Reading Script", "Failed to parse Compress attribute in Out: '" + attribute.InnerText + "'.");
									nextCompress = newCompress;
								}
							}
							attribute = next.Attributes["Premultiply"];
							if (attribute != null) {
								if (!bool.TryParse(attribute.InnerText, out nextPremultiply)) {
									LogWarning("Reading Script", "Failed to parse Premultiply attribute in Out: '" + attribute.InnerText + "'.");
									nextPremultiply = newPremultiply;
								}
							}
							files.Add(new PathPair(path, nextOutput, nextCompress, nextPremultiply));
						}
						else {
							LogWarning("Reading Script", "Invalid Path attribute in Out: '" + attribute.InnerText + "'.");
						}
					}
					else {
						LogWarning("Reading Script", "No Path attribute in Out.");
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

		/**<summary>Backs up a directory.</summary>*/
		public static void BackupFiles(string inputDirectory, string outputDirectory) {
			string[] files = Helpers.FindAllFiles(inputDirectory);
			totalFiles = files.Length;

			foreach (string inputFile in files) {
				BackupFile(inputFile, inputDirectory, outputDirectory);
			}

			FinishProgress("Finished Backing Up " + files.Length + " Files");
		}
		/**<summary>Restores a directory.</summary>*/
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
		/**<summary>Backs up a file.</summary>*/
		private static bool BackupFile(string inputFile, string inputDirectory, string outputDirectory) {
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Backing up: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return backedUp;
		}
		/**<summary>Backs up a file with different parameters.</summary>*/
		private static bool BackupFile2(string inputFile, string outputFile) {
			bool backedUp = false;
			try {
				UpdateProgress("Backing Up: " + Path.GetFileName(inputFile));
				
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Backing up: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return backedUp;
		}
		/**<summary>Restores a file.</summary>*/
		private static bool RestoreFile(string inputFile, string inputDirectory, string outputDirectory) {
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Restoring: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return filedCopied;
		}
		/**<summary>Restores a file with different parameters.</summary>*/
		private static bool RestoreFile2(string inputFile, string outputFile) {
			bool filedCopied = false;
			try {
				UpdateProgress("Restoring: " + Path.GetFileName(inputFile));
				
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
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				LogError("Restoring: " + inputFile, ex.GetType().ToString().Split('.').Last() + " (" + ex.Message + ")");
			}
			filesCompleted++;
			return filedCopied;
		}

		#endregion
	}
}
