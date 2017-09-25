using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

namespace TConvert.Util {
	/**<summary>Extract embedded resources.</summary>*/
	public static class EmbeddedResources {
		//========== EXTRACTING ==========
		#region Extracting

		/**<summary>Extract an embedded resource from byte array.</summary>*/
		public static void Extract(string resourcePath, byte[] resourceBytes) {
			string dirName = Path.GetDirectoryName(resourcePath);
			if (!Directory.Exists(dirName)) {
				Directory.CreateDirectory(dirName);
			}

			bool rewrite = true;
			if (File.Exists(resourcePath)) {
				byte[] existing = File.ReadAllBytes(resourcePath);
				if (resourceBytes.SequenceEqual(existing)) {
					rewrite = false;
				}
			}
			if (rewrite) {
				File.WriteAllBytes(resourcePath, resourceBytes);
			}
		}
		/**<summary>Extract an embedded resource from stream.</summary>*/
		public static void Extract(string resourcePath, Stream resourceStream) {
			byte[] resourceBytes = new byte[resourceStream.Length];
			resourceStream.Read(resourceBytes, 0, resourceBytes.Length);

			Extract(resourcePath, resourceBytes);
		}
		/**<summary>Extract an embedded resource from name.</summary>*/
		public static void Extract(string resourcePath, string resourceName) {
			Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			Extract(resourcePath, resourceStream);
		}

		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Load a dll.</summary>*/
		public static void LoadDll(string dllPath) {
			IntPtr h = LoadLibrary(dllPath);
			if (h == IntPtr.Zero) {
				Exception e = new Win32Exception();
				throw new DllNotFoundException("Unable to load library: " + dllPath, e);
			}
		}

		#endregion
		//============ NATIVE ============
		#region Native

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr LoadLibrary(string lpFileName);

		#endregion
	}
}