using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TConvert {
	public static class TerrariaLocator {

		public static readonly string TerrariaContentDirectory;

		static TerrariaLocator() {
			TerrariaContentDirectory = FindTerrariaContentDirectory();
		}

		private static string FindTerrariaContentDirectory() {
			try {
				// Check the windows registry for steam installation path
				string steamPath = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null) as string;
				string result = SeekDirectory(steamPath);
				if (result != null) {
					return result;
				}
			}
			catch { }
			try {
				// Try to find relevant environment variables
				foreach (KeyValuePair<string, string> envVar in Environment.GetEnvironmentVariables()) {
					string result = null;
					if (envVar.Key.ToLower().Contains("terraria") ||
							envVar.Key.ToLower().Contains("tapi")) {
						result = SeekDirectory(envVar.Value);
					}
					else if (envVar.Key.ToLower().Contains("steam")) {
						result = SeekDirectory(envVar.Value);
					}
					if (result != null) {
						return result;
					}
				}
			}
			catch { }

			// If nothing other works, then prompt the user
			return null;
		}

		private static string SeekDirectory(string steamDirectory) {
			if (steamDirectory == null || !Directory.Exists(steamDirectory)) {
				return null;
			}
			
			string path = Path.Combine(steamDirectory, "SteamApps", "Common", "Terraria", "Content");
			if (Directory.Exists(path)) {
				path = GetProperDirectoryCapitalization(new DirectoryInfo(path));
				if (path.Length >= 2 && path[1] == ':') {
					path = char.ToUpper(path[0]) + path.Substring(1);
					return path;
				}
			}
			return null;
		}

		static string GetProperDirectoryCapitalization(DirectoryInfo dirInfo) {
			DirectoryInfo parentDirInfo = dirInfo.Parent;
			if (null == parentDirInfo)
				return dirInfo.Name;
			return Path.Combine(GetProperDirectoryCapitalization(parentDirInfo),
								parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
		}

		static string GetProperFilePathCapitalization(string filename) {
			FileInfo fileInfo = new FileInfo(filename);
			DirectoryInfo dirInfo = fileInfo.Directory;
			return Path.Combine(GetProperDirectoryCapitalization(dirInfo),
								dirInfo.GetFiles(fileInfo.Name)[0].Name);
		}

	}
}
