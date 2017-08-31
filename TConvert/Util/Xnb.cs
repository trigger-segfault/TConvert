using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TConvert.Util {
	public static class Xnb {
		public static int Get7BitEncodedInt(BinaryReader reader) {
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
		public static void Write7BitEncodedInt(BinaryWriter writer, int i) {
			while (i >= 0x80) {
				writer.Write((byte)(i & 0xff));
				i >>= 7;
			}
			writer.Write((byte)i);
		}

		public static String Get7BitEncodedString(BinaryReader reader) {
			int length = Get7BitEncodedInt(reader);
			return Encoding.UTF8.GetString(reader.ReadBytes(length));
		}
		public static void Write7BitEncodedString(BinaryWriter writer, string s) {
			Write7BitEncodedInt(writer, s.Length);
			writer.Write(Encoding.UTF8.GetBytes(s));
		}
	}

	public class XnbException : Exception {
		public XnbException(string message) : base(message) { }
		public XnbException(string message, Exception innerException)
			: base(message, innerException) { }
	}
	public class XwbException : Exception {
		public XwbException(string message) : base(message) { }
		public XwbException(string message, Exception innerException)
			: base(message, innerException) { }
	}
	public class PngException : Exception {
		public PngException(string message) : base(message) { }
		public PngException(string message, Exception innerException)
			: base(message, innerException) { }
	}
	public class WavException : Exception {
		public WavException(string message) : base(message) { }
		public WavException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
