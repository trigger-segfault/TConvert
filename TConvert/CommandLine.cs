using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TConvert.Properties;

namespace TConvert {
	public static class CommandLine {
		[Flags]
		public enum ArgTypes {
			None		= 0x0,
			Path		= 0x1,
			Input		= 0x2,
			Output		= 0x4,

			// Mode
			Extract		= 0x8,
			Convert		= 0x10,
			Backup		= 0x20,
			Restore		= 0x40,
			Script		= 0x80,

			// Misc
			Help		= 0x100,
			Log			= 0x200,
			Silent		= 0x400,
			AutoClose	= 0x800,
			KeepOpen	= 0x1000,
			Console		= 0x2000,
			AutoCloseDef= 0x4000,
			KeepOpenDef = 0x8000
		}

		struct ArgInfo {
			public Action Action;
			public string Name;
			public string Description;
			public string[] Options;
			public string PostOptions;
			public ArgInfo(Action action, string name, string description, string postOptions, params string[] options) {
				Action = action;
				Name = name;
				Description = description;
				Options = options;
				PostOptions = postOptions;
			}
			public string OptionsToString() {
				string s = (Options.Length > 0 ? Options[0] : "");
				for (int i = 1; i < Options.Length; i++)
					s += " " + Options[i];
				return s;
			}
		}

		private static readonly Dictionary<ArgTypes, ArgInfo> ArgInfos = new Dictionary<ArgTypes, ArgInfo>() {
			{ ArgTypes.Input,	new ArgInfo(ProcessInput, "Input", "Specify input files & folders.", "[filepaths]", "-i", "--input") },
			{ ArgTypes.Output,	new ArgInfo(ProcessOutput, "Output", "Specify output files & folders.", "[filepaths]", "-o", "--output") },
			#if !(CONSOLE)
			{ ArgTypes.Console,   new ArgInfo(ProcessConsole, "Console", "Don't display a progress window.", null, "-C", "--Console") },
			#endif
			{ ArgTypes.Extract,	new ArgInfo(ProcessExtract, "Extract", "Sets the mode to extract.", null, "-e", "--extract") },
			{ ArgTypes.Convert,	new ArgInfo(ProcessConvert, "Convert", "Sets the mode to convert.", null, "-c", "--convert") },
			{ ArgTypes.Backup,	new ArgInfo(ProcessBackup, "Backup", "Sets the mode to backup.", null, "-b", "--backup") },
			{ ArgTypes.Restore,	new ArgInfo(ProcessRestore, "Restore", "Sets the mode to restore.", null, "-r", "--restore") },
			{ ArgTypes.Script,	new ArgInfo(ProcessScript, "Script", "Sets the mode to run script.", null, "-x", "--script") },

			{ ArgTypes.Help,	new ArgInfo(ProcessHelp, "Help", "Shows this help information.", null, "-h", "--help") },
			//{ ArgTypes.Log,		new ArgInfo(ProcessLog, "Log", "Specify a log file path after this option.", "filename", "-l", "--log") },
			{ ArgTypes.Silent,	new ArgInfo(ProcessSilent, "Silent", "No console output will be produced.", null, "-s", "--silent") },
			
			#if !(CONSOLE)
			{ ArgTypes.AutoClose,new ArgInfo(ProcessAutoClose, "Auto-Close", "Auto-closes the progress window when done.", null, "-a", "--auto-close") },
			{ ArgTypes.KeepOpen,new ArgInfo(ProcessKeepOpen, "Keep Open", "Keeps open the progress window when done.", null, "-k", "--keep-open") },

			{ ArgTypes.AutoCloseDef,new ArgInfo(ProcessAutoCloseDefault, "Auto-Close Default", "Sets the default close setting to auto-close.", null, "-ad", "--auto-close-def") },
			{ ArgTypes.KeepOpenDef,new ArgInfo(ProcessKeepOpenDefault, "Keep Open Default", "Sets the default close setting to keep open.", null, "-kd", "--keep-open-def") }
			#endif
		};

		//=========== MEMBERS ============
		#region Members

		private static ArgTypes passedArgs = ArgTypes.None;
		private static ArgTypes lastArg = ArgTypes.None;
		private static ArgTypes argMode = ArgTypes.None;
		private static ProcessModes processMode = ProcessModes.Any;
		private static List<string> inputs = new List<string>();
		private static List<string> outputs = new List<string>();
		private static bool error = false;
		private static string[] args;
		private static string logFile = null;
		private static bool silent = false;
		private static bool autoClose = Settings.Default.AutoCloseCmdProgress;
		private static bool console = false;

		private static List<KeyValuePair<ConsoleColor, string>> log = new List<KeyValuePair<ConsoleColor, string>>();

		private static string ExeName {
			get { return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location); }
		}

		#endregion
		//=========== LOGGING ============
		#region Logging

		private static void Log(string message) {
			log.Add(new KeyValuePair<ConsoleColor, string>(
				ConsoleColor.Gray,
				message
			));
		}
		private static void Log(ConsoleColor color, string message) {
			log.Add(new KeyValuePair<ConsoleColor, string>(
				color,
				message
			));
		}
		private static void LogError(string message) {
			Log(ConsoleColor.Red, "Error: " + message);
			error = true;
		}
		private static void LogWarning(string message) {
			Log(ConsoleColor.Yellow, "Warning: " + message);
		}
		private static void LogModeAlreadyInUse(ArgTypes argType) {
			LogError("TConvert mode already specified as " +
				ArgInfos[argMode].Name + " " + ArgInfos[argMode].OptionsToString() + ". " +
				"Cannot specify option " + ArgInfos[argType].Name + " (" +
				ArgInfos[argType].OptionsToString() + ").");
		}
		private static void LogOptionAlreadySpecified(ArgTypes argType) {
			LogError(ArgInfos[argMode].Name + " option (" +
				ArgInfos[argMode].OptionsToString() + ") already specified.");
		}
		private static void WriteLog() {
			Console.WriteLine();
			ConsoleColor oldColor = Console.ForegroundColor;
			foreach (var line in log) {
				if (Console.ForegroundColor != line.Key)
					Console.ForegroundColor = line.Key;
				Console.WriteLine(line.Value);
			}
			Console.ForegroundColor = oldColor;
		}

		#endregion
		//=========== PARSING ============
		#region Parsing

		public static void ParseCommand(string[] args) {
			CommandLine.args = args;
			for (int i = 0; i < args.Length && !error; i++) {
				ArgTypes argType = ParseArgument(args[i]);
				passedArgs |= argType;
				if (argType >= ArgTypes.Extract && argType <= ArgTypes.Script) {
					argMode = argType;
				}
				if (argType != ArgTypes.Path || lastArg == ArgTypes.Log) {
					lastArg = argType;
				}
			}
			// Show a help message when there's no input
			if (args.Length == 0) {
				ProcessHelp();
			}
			if (!error) {
				ProcessCommand();
			}
			else if (!silent) {
				WriteLog();
			}
			Environment.ExitCode = (error ? 1 : 0);
		}
		private static ArgTypes ParseArgument(string arg) {
			switch (lastArg) {
			case ArgTypes.Input:
				if (inputs.Count > 0)
					goto default;
				break;
			case ArgTypes.Output:
				if (outputs.Count > 0)
					goto default;
				break;
			case ArgTypes.Log:
				break;
			default:
				foreach (var argInfo in ArgInfos) {
					foreach (string option in argInfo.Value.Options) {
						if (arg == option) {
							argInfo.Value.Action();
							return argInfo.Key;
						}
					}
				}
				break;
			}
			ProcessFileName(arg);
			return ArgTypes.Path;
		}

		#endregion
		//========== PROCESSING ==========
		#region Processing
		//--------------------------------
		#region Final

		private static void ProcessCommand() {
			if (outputs.Count == 0) {
				foreach (string input in inputs) {
					outputs.Add(input);
				}
			}
			else if (inputs.Count != outputs.Count) {
				LogError("Number of input filepaths must be the same number as output filenames.");
			}
			for (int i = 0; i < inputs.Count && !error; i++) {
				string input = inputs[i];
				string output = outputs[i];
				string inputFull = "";
				string outputFull = "";
				try {
					inputFull = Path.GetFullPath(inputs[i]);
				}
				catch (ArgumentException) {
					LogError("Invalid input path: " + input);
					return;
				}
				try {
					outputFull = Path.GetFullPath(outputs[i]);
				}
				catch (ArgumentException) {
					LogError("Invalid output path: " + input);
					return;
				}
				if (File.Exists(input) || Directory.Exists(input)) {
					bool isDir = Directory.Exists(input);
					bool outputExists = File.Exists(output) || Directory.Exists(output);
					if (outputExists && isDir != Directory.Exists(output)) {
						if (isDir)
							LogError("Input is folder while output is file:\n    " + input + "\n    " + output);
						else
							LogError("Input is file while output is folder:\n    " + input + "\n    " + output);
						return;
					}
					inputs[i] = inputFull;
					outputs[i] = outputFull;
				}
				else {
					LogError("Input path does not exist: " + input);
				}
			}
			if (!silent)
				WriteLog();
			if (!error && inputs.Count > 0) {
				#if !(CONSOLE)
				if (!console) {
					App app = new App();
					app.InitializeComponent();
					app.Run();
				}
				else
				#endif
				{
					ProcessFiles();
				}
			}
		}

		#endregion
		//--------------------------------
		#region Input/Output

		private static void ProcessInput() {
			if (passedArgs.HasFlag(ArgTypes.Input))
				LogOptionAlreadySpecified(ArgTypes.Input);
			else if (inputs.Count > 0)
				LogError("File paths already specified. Cannot use " +
					ArgInfos[ArgTypes.Input].Name + " option (" +
					ArgInfos[ArgTypes.Input].OptionsToString() + ").");
		}
		private static void ProcessOutput() {
			if (passedArgs.HasFlag(ArgTypes.Output))
				LogOptionAlreadySpecified(ArgTypes.Output);
		}
		private static void ProcessFileName(string arg) {
			if (arg.StartsWith("-") && !File.Exists(arg)) {
				switch (lastArg) {
				case ArgTypes.Input:
					if (inputs.Count == 0)
						LogError("Expected a filepath: " + arg);
					else
						goto default;
					break;
				case ArgTypes.Output:
					if (outputs.Count == 0)
						LogError("Expected a filepath: " + arg);
					else
						goto default;
					break;
				case ArgTypes.Log:
					if (logFile == null)
						LogError("Expected a filename: " + arg);
					else
						goto default;
					break;
				default:
					LogError("Invalid option: " + arg);
					break;
				}
			}
			else {
				switch (lastArg) {
				case ArgTypes.None:
				case ArgTypes.Input:
					inputs.Add(arg);
					break;
				case ArgTypes.Output:
					outputs.Add(arg);
					break;
				case ArgTypes.Log:
					logFile = arg;
					break;
				default:
					LogError("Invalid option: " + arg);
					break;
				}
			}
		}

		#endregion
		//--------------------------------
		#region Modes

		private static void ProcessExtract() {
			if (passedArgs.HasFlag(ArgTypes.Extract))
				LogOptionAlreadySpecified(ArgTypes.Extract);
			if (argMode != ArgTypes.None && argMode != ArgTypes.Extract)
				LogModeAlreadyInUse(ArgTypes.Extract);
			processMode = ProcessModes.Extract;
		}
		private static void ProcessConvert() {
			if (passedArgs.HasFlag(ArgTypes.Convert))
				LogOptionAlreadySpecified(ArgTypes.Convert);
			if (argMode != ArgTypes.None && argMode != ArgTypes.Convert)
				LogModeAlreadyInUse(ArgTypes.Convert);
			processMode = ProcessModes.Convert;
		}
		private static void ProcessBackup() {
			if (passedArgs.HasFlag(ArgTypes.Backup))
				LogOptionAlreadySpecified(ArgTypes.Backup);
			if (argMode != ArgTypes.None && argMode != ArgTypes.Backup)
				LogModeAlreadyInUse(ArgTypes.Backup);
			processMode = ProcessModes.Convert;
		}
		private static void ProcessRestore() {
			if (passedArgs.HasFlag(ArgTypes.Restore))
				LogOptionAlreadySpecified(ArgTypes.Restore);
			if (argMode != ArgTypes.None && argMode != ArgTypes.Restore)
				LogModeAlreadyInUse(ArgTypes.Restore);
			processMode = ProcessModes.Restore;
		}
		private static void ProcessScript() {
			if (passedArgs.HasFlag(ArgTypes.Script))
				LogOptionAlreadySpecified(ArgTypes.Script);
			if (argMode != ArgTypes.None && argMode != ArgTypes.Script)
				LogModeAlreadyInUse(ArgTypes.Script);
			processMode = ProcessModes.Script;
		}

		#endregion
		//--------------------------------
		#region Misc

		private static void ProcessHelp() {
			Log(ConsoleColor.White, "[" + ExeName + "] A combination tool for managing Terraria content resources.");
			Log("usage: " + ExeName + " [filepaths] [options]");
			Log("  options:");
			foreach (var argInfo in ArgInfos) {
				string line = "    ";
				line += argInfo.Value.OptionsToString();
				if (argInfo.Value.PostOptions != null)
					line += " " + argInfo.Value.PostOptions;
				if (line.Length < 22)
					line += new string(' ', 22 - line.Length);
				else if (line.Length < 30)
					line += new string(' ', 30 - line.Length);
				line += argInfo.Value.Description;
				Log(line);
			}
		}
		private static void ProcessLog() {
			if (logFile != null)
				LogOptionAlreadySpecified(ArgTypes.Log);
		}
		private static void ProcessSilent() {
			if (silent)
				LogOptionAlreadySpecified(ArgTypes.Silent);
			else
				silent = true;
		}
		private static void ProcessAutoClose() {
			if (passedArgs.HasFlag(ArgTypes.AutoClose))
				LogOptionAlreadySpecified(ArgTypes.AutoClose);
			if (passedArgs.HasFlag(ArgTypes.KeepOpen))
				LogOptionAlreadySpecified(ArgTypes.KeepOpen);
			else
				autoClose = true;
		}
		private static void ProcessKeepOpen() {
			if (passedArgs.HasFlag(ArgTypes.AutoClose))
				LogOptionAlreadySpecified(ArgTypes.AutoClose);
			if (passedArgs.HasFlag(ArgTypes.KeepOpen))
				LogOptionAlreadySpecified(ArgTypes.KeepOpen);
			else
				autoClose = false;
		}
		private static void ProcessAutoCloseDefault() {
			if (passedArgs.HasFlag(ArgTypes.AutoCloseDef))
				LogOptionAlreadySpecified(ArgTypes.AutoCloseDef);
			if (passedArgs.HasFlag(ArgTypes.KeepOpenDef))
				LogOptionAlreadySpecified(ArgTypes.KeepOpenDef);
			else {
				Settings.Default.AutoCloseCmdProgress = true;
				Settings.Default.Save();
				Log(ConsoleColor.White, "Progress window finish set to: Auto-Close");
			}
		}
		private static void ProcessKeepOpenDefault() {
			if (passedArgs.HasFlag(ArgTypes.AutoCloseDef))
				LogOptionAlreadySpecified(ArgTypes.AutoCloseDef);
			if (passedArgs.HasFlag(ArgTypes.KeepOpenDef))
				LogOptionAlreadySpecified(ArgTypes.KeepOpenDef);
			else {
				Settings.Default.AutoCloseCmdProgress = false;
				Settings.Default.Save();
				Log(ConsoleColor.White, "Progress window finish set to: Keep Open");
			}
		}
		private static void ProcessConsole() {
			if (console)
				LogOptionAlreadySpecified(ArgTypes.Console);
			else
				console = true;
		}

		#endregion
		//--------------------------------
		#endregion

		public static void ProcessFiles() {
			Thread thread = new Thread(() => {
				Program.ProcessDropFiles(processMode, inputs.ToArray(), outputs.ToArray());
			});
			#if !(CONSOLE)
			if (!console) {
				Program.StartProgressThread(null, "Processing Files...", autoClose, thread);
			}
			else
			#endif
			{
				Program.StartConsoleThread("Processing Files...", silent, thread);
			}
		}
	}
}
