using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TConvert {
	/**<summary>Finds the Terraria Content folder.</summary>*/
	public static class TerrariaLocator {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The located or empty Terraria Content folder.</summary>*/
		public static readonly string TerrariaContentDirectory;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Start looking for the Terraria Content folder.</summary>*/
		static TerrariaLocator() {
			TerrariaContentDirectory = FindTerrariaContentDirectory();
		}

		#endregion
		//=========== LOCATORS ===========
		#region Locators

		/**<summary>Starts looking for the Terraria Content folder.</summary>*/
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

		/**<summary>Seeks a directory for the Terraria Content folder.</summary>*/
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

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Gets the proper capitalization of a path so it looks nice.</summary>*/
		private static string GetProperDirectoryCapitalization(DirectoryInfo dirInfo) {
			DirectoryInfo parentDirInfo = dirInfo.Parent;
			if (null == parentDirInfo)
				return dirInfo.Name;
			return Path.Combine(GetProperDirectoryCapitalization(parentDirInfo),
								parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
		}
		/**<summary>Recursively gets the proper capitalization of a path so it looks nice.</summary>*/
		private static string GetProperFilePathCapitalization(string filename) {
			FileInfo fileInfo = new FileInfo(filename);
			DirectoryInfo dirInfo = fileInfo.Directory;
			return Path.Combine(GetProperDirectoryCapitalization(dirInfo),
								dirInfo.GetFiles(fileInfo.Name)[0].Name);
		}

		#endregion
	}
}
