using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace TConvert.Util {
	public static class Extensions {
		/**<summary>Fills an array with a value.</summary>*/
		public static void Fill<T>(this T[] originalArray, T with) {
			for (int i = 0; i < originalArray.Length; i++) {
				originalArray[i] = with;
			}
		}
	}
}
