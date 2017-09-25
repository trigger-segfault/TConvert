using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Win32;

namespace TConvert.Util {
	/**<summary>Finds the Terraria Content folder.</summary>*/
	public static class TerrariaLocator {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The located or empty Terraria Content folder.</summary>*/
		public static readonly string TerrariaContentDirectory;
		/**<summary>The path to Terra Launcher's configuration file.</summary>*/
		public static readonly string TerraLauncherConfigPath;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Start looking for the Terraria Content folder.</summary>*/
		static TerrariaLocator() {
			TerrariaContentDirectory = FindTerrariaContentDirectory();
		}

		#endregion
		//========== SAVE PATHS ==========
		#region Save Paths

		/**<summary>Reads the specified executable's selected save directory from Terra Launcher's config.</summary>*/
		public static string FindTerraLauncherSaveDirectory(string exePath) {
			string arguments = "";
			if (!string.IsNullOrWhiteSpace(TerraLauncherConfigPath)) {
				string oldCurrentDirectory = Directory.GetCurrentDirectory();
				try {
					// Set the current directory for relative paths
					Directory.SetCurrentDirectory(Path.GetDirectoryName(TerraLauncherConfigPath));

					exePath = Path.GetFullPath(exePath).ToLower();

					XmlDocument doc = new XmlDocument();
					doc.Load(TerraLauncherConfigPath);

					// Check if the user disable Trigger Tool Integration
					bool boolValue;
					XmlNode integration = doc.SelectSingleNode("TerraLauncher/Integration");
					if (integration != null && bool.TryParse(integration.InnerText, out boolValue) && boolValue) {
						XmlNode gamesNode = doc.SelectSingleNode("TerraLauncher/Games");
						if (gamesNode != null) {
							string result = ReadConfigFolder(exePath, gamesNode);
							if (!string.IsNullOrWhiteSpace(result))
								arguments = "-savedirectory \"" + result + "\"";
						}
					}
				}
				catch { }
				// Set the current directory back
				Directory.SetCurrentDirectory(oldCurrentDirectory);
			}
			return arguments;
		}
		/**<summary>Reads a Terra Launcher config folder and returns the save directory.</summary>*/
		private static string ReadConfigFolder(string exePath, XmlNode folderNode) {
			XmlNodeList nodeList = folderNode.SelectNodes("Folder");
			foreach (XmlNode node in nodeList) {
				string result = ReadConfigFolder(exePath, node);
				if (!string.IsNullOrWhiteSpace(result))
					return result;
			}

			nodeList = folderNode.SelectNodes("Game");

			foreach (XmlNode node in folderNode) {
				string result = null;
				if (node.Name == "Folder") {
					result = ReadConfigFolder(exePath, node);
				}
				else if (node.Name == "Game") {
					XmlNode exeNode = node.SelectSingleNode("ExePath");
					if (exeNode != null && Path.GetFullPath(exeNode.InnerText).ToLower() == exePath) {
						XmlNode saveNode = node.SelectSingleNode("SaveDirectory");
						if (saveNode != null && !string.IsNullOrWhiteSpace(saveNode.InnerText))
							return saveNode.InnerText;
					}
				}
				if (!string.IsNullOrWhiteSpace(result))
					return result;
			}
			foreach (XmlNode node in nodeList) {
				XmlNode exeNode = node.SelectSingleNode("ExePath");
				if (exeNode != null && Path.GetFullPath(exeNode.InnerText).ToLower() == exePath) {
					XmlNode saveNode = node.SelectSingleNode("SaveDirectory");
					if (saveNode != null && !string.IsNullOrWhiteSpace(saveNode.InnerText))
						return saveNode.InnerText;
				}
			}
			return null;
		}

		#endregion
		//=========== LOCATORS ===========
		#region Locators

		/**<summary>Starts looking for the Terra Launcher config path.</summary>*/
		private static string FindConfigPath() {
			return Registry.GetValue("HKEY_CURRENT_USER\\Software\\TriggersToolsGames\\TerraLauncher", "ConfigPath", null) as string;
		}
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
