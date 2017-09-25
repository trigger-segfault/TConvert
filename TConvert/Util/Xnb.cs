using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TConvert.Util {
	/**<summary>Xnb extensions for binary readers and writers.</summary>*/
	public static class XnbExtensions {
		/**<summary>Reads an Xnb 7-bit encoded int.</summary>*/
		public static int Read7BitEncodedInt(this BinaryReader reader) {
			int result = 0;
			int bitsRead = 0;
			int value;

			do {
				value = reader.ReadByte();
				result |= (value & 0x7f) << bitsRead;
				bitsRead += 7;
			} while ((value & 0x80) != 0);

			return result;
		}
		/**<summary>Reads an Xnb 7-bit encoded string.</summary>*/
		public static String Read7BitEncodedString(this BinaryReader reader) {
			int length = reader.Read7BitEncodedInt();
			return Encoding.UTF8.GetString(reader.ReadBytes(length));
		}
		/**<summary>Writes an Xnb 7-bit encoded int.</summary>*/
		public static void Write7BitEncodedInt(this BinaryWriter writer, int i) {
			while (i >= 0x80) {
				writer.Write((byte)(i & 0xff));
				i >>= 7;
			}
			writer.Write((byte)i);
		}
		/**<summary>Writes an Xnb 7-bit encoded string.</summary>*/
		public static void Write7BitEncodedString(this BinaryWriter writer, string s) {
			writer.Write7BitEncodedInt(s.Length);
			writer.Write(Encoding.UTF8.GetBytes(s));
		}
		/**<summary>Fills an array with a value.</summary>*/
		public static void Fill<T>(this T[] array, T with) {
			for (int i = 0; i < array.Length; i++) {
				array[i] = with;
			}
		}
		/**<summary>Checks if a byte array matches a string.</summary>*/
		public static bool ReadAndCompareString(this BinaryReader reader, string s) {
			byte[] stringData = Encoding.UTF8.GetBytes(s);
			byte[] data = reader.ReadBytes(stringData.Length);
			if (data.Length != stringData.Length)
				return false;
			for (int i = 0; i < data.Length; i++) {
				if (data[i] != stringData[i])
					return false;
			}
			return true;
		}
	}

	/**<summary>An exception thrown during Xnb extraction.</summary>*/
	public class XnbException : Exception {
		public XnbException(string message) : base(message) { }
		public XnbException(string message, Exception innerException)
			: base(message, innerException) { }
	}
	/**<summary>An exception thrown during Wave Bank extraction.</summary>*/
	public class XwbException : Exception {
		public XwbException(string message) : base(message) { }
		public XwbException(string message, Exception innerException)
			: base(message, innerException) { }
	}
	/**<summary>An exception thrown during Xnb to Png conversion.</summary>*/
	public class PngException : Exception {
		public PngException(string message) : base(message) { }
		public PngException(string message, Exception innerException)
			: base(message, innerException) { }
	}
	/**<summary>An exception thrown during Xnb to Wav conversion.</summary>*/
	public class WavException : Exception {
		public WavException(string message) : base(message) { }
		public WavException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
