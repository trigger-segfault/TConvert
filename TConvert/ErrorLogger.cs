using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TConvert {
	public static class ErrorLogger {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The path of the error log.</summary>*/
		public static readonly string LogPath = Path.Combine(
			Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
			"TConvert-ErrorLog.txt"
		);

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The writer for the error log file.</summary>*/
		private static StreamWriter writer = null;

		#endregion
		//=========== WRITING ============
		#region Writing

		/**<summary>Returns true if the error log file is open.</summary>*/
		public static bool IsOpen {
			get { return writer != null; }
		}

		/**<summary>Opens the error log file.</summary>*/
		public static bool Open() {
			Close();
			try {
				writer = new StreamWriter(LogPath);
				WriteErrorHeader();
				return true;
			}
			catch {
				return false;
			}
		}
		/**<summary>Closes the error log file.</summary>*/
		public static void Close() {
			try {
				if (writer != null) {
					writer.Close();
					writer = null;
				}
			}
			catch { }

		}
		/**<summary>Starts a new line.</summary>*/
		public static void WriteLine() {
			if (writer != null || Open())
				writer.WriteLine();
		}
		/**<summary>Writes the text then starts a new line.</summary>*/
		public static void WriteLine(string text) {
			if (writer != null || Open())
				writer.WriteLine(text);
		}
		/**<summary>Writes the text.</summary>*/
		public static void Write(string text) {
			if (writer != null || Open())
				writer.Write(text);
		}

		/**<summary>Writes the standard error log header.</summary>*/
		private static void WriteErrorHeader() {
			if (writer != null) {
				writer.WriteLine("------------------------------------------------");
				writer.WriteLine("Time: " + DateTime.Now.ToString());
				writer.WriteLine();
			}
		}

		#endregion
	}
}
