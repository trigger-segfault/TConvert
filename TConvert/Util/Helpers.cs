using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TConvert.Util {
	public struct PathIOPair {
		public string InPath;
		public string OutPath;
	}
	public static class Helpers {

		public static string GetOutputPath(string inputPath, string inputDirectory, string outputDirectory) {
			string fullPath = Path.GetFullPath(inputPath);
			return Path.Combine(outputDirectory, fullPath.Substring(Math.Min(fullPath.Length, inputDirectory.Length + 1)));
		}
		public static string GetRelativePath(string path, string inputDirectory) {
			string fullPath = Path.GetFullPath(path);
			return fullPath.Substring(Math.Min(fullPath.Length, inputDirectory.Length + 1));
		}

		public static string[] GetOutputFiles(string[] inputFiles, string inputDirectory, string outputDirectory) {
			string[] outputFiles = new string[inputFiles.Length];
			for (int i = 0; i < inputFiles.Length; i++) {
				outputFiles[i] = GetOutputPath(inputFiles[i], inputDirectory, outputDirectory);
			}
			return outputFiles;
		}

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

		public static void CreateAllDirectories(string[] inputFiles, string inputDirectory, string outputDirectory) {
			CreateAllDirectories(GetOutputFiles(inputFiles, inputDirectory, outputDirectory));
		}
		public static void CreateAllDirectories(string[] files) {
			try {
				foreach (string file in files) {
					string directory = Path.GetDirectoryName(file);
					if (!Directory.Exists(directory))
						Directory.CreateDirectory(directory);
				}
			}
			catch { }
		}
		public static void CreateAllDirectories(PathIOPair[] files) {
			try {
				foreach (PathIOPair file in files) {
					string directory = Path.GetDirectoryName(file.OutPath);
					if (!Directory.Exists(directory))
						Directory.CreateDirectory(directory);
				}
			}
			catch { }
		}

		public static void CreateDirectorySafe(string directory) {
			try {
				if (!Directory.Exists(directory)) {
					Directory.CreateDirectory(directory);
				}
			}
			catch { }
		}
	}
}
