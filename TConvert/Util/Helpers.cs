using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TConvert.Util {
	/**<summary>A class of static helpers.</summary>*/
	public static class Helpers {

		/**<summary>Gets the output path relative to the input path.</summary>*/
		public static string GetOutputPath(string inputPath, string inputDirectory, string outputDirectory) {
			string fullPath = Path.GetFullPath(inputPath);
			return Path.Combine(outputDirectory, fullPath.Substring(Math.Min(fullPath.Length, inputDirectory.Length + 1)));
		}
		/**<summary>Gets the relative path based on the input directory.</summary>*/
		public static string GetRelativePath(string path, string inputDirectory) {
			string fullPath = Path.GetFullPath(path);
			return fullPath.Substring(Math.Min(fullPath.Length, inputDirectory.Length + 1));
		}
		/**<summary>Gets the output paths relative to the input path.</summary>*/
		public static string[] GetOutputFiles(string[] inputFiles, string inputDirectory, string outputDirectory) {
			string[] outputFiles = new string[inputFiles.Length];
			for (int i = 0; i < inputFiles.Length; i++) {
				outputFiles[i] = GetOutputPath(inputFiles[i], inputDirectory, outputDirectory);
			}
			return outputFiles;
		}

		/**<summary>Finds all files in a directory and subdirectories.</summary>*/
		public static string[] FindAllFiles(string path, bool excludeCurrent = false) {
			List<string> files = new List<string>();
			try {
				foreach (string directory in Directory.EnumerateDirectories(path)) {
					files.AddRange(FindAllFiles(directory));
				}
				if (!excludeCurrent)
					files.AddRange(Directory.GetFiles(path));
			}
			catch { }
			return files.ToArray();
		}
		/**<summary>Gets the file count in a directory and subdirectories.</summary>*/
		public static int GetFileCount(string path, bool excludeCurrent = false) {
			int count = 0;
			try {
				foreach (string directory in Directory.EnumerateDirectories(path)) {
					count += GetFileCount(directory);
				}
				if (!excludeCurrent)
					count += Directory.GetFiles(path).Length;
			}
			catch { }
			return count;
		}

		/**<summary>Safely attempts to create a directory.</summary>*/
		public static void CreateDirectorySafe(string directory) {
			try {
				if (!Directory.Exists(directory)) {
					Directory.CreateDirectory(directory);
				}
			}
			catch { }
		}
		/**<summary>Safely gets the full path.</summary>*/
		public static string FixPathSafe(string path) {
			try {
				path = Path.GetFullPath(path);
				if (path != string.Empty && (path.Last() == '\\' || path.Last() == '/'))
					path = path.Remove(path.Length - 1);
				return path;
			}
			catch {
				return "";
			}
		}
		/**<summary>Safely tests if a directory exists.</summary>*/
		public static bool DirectoryExistsSafe(string path) {
			try {
				if (Directory.Exists(path))
					return true;
			}
			catch { }
			return false;
		}
		/**<summary>Safely tests if a file exists.</summary>*/
		public static bool FileExistsSafe(string path) {
			try {
				if (File.Exists(path))
					return true;
			}
			catch { }
			return false;
		}
		/**<summary>Safely tests if a file exists.</summary>*/
		public static bool IsPathValid(string path) {
			try {
				Path.GetFullPath(path);
				return true;
			}
			catch {
				return false;
			}
		}
	}
}
