/*******************************************************************************
 *	Copyright (C) 2017  sullerandras
 *	
 *	This program is free software: you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation, either version 3 of the License, or
 *	(at your option) any later version.
 *	
 *	This program is distributed in the hope that it will be useful,
 *	but WITHOUT ANY WARRANTY; without even the implied warranty of
 *	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *	GNU General Public License for more details.
 *	
 *	You should have received a copy of the GNU General Public License
 *	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 ******************************************************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TConvert.Util;

namespace TConvert.Convert {
	public class PngConverter {

		private const string Texture2DType =
			"Microsoft.Xna.Framework.Content.Texture2DReader, " + 
			"Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, " + 
			"Culture=neutral, PublicKeyToken=842cf8be1de50553";
		private const int HeaderSize = 3 + 1 + 1 + 1;
		private const int CompressedFileSize = 4;
		private const int TypeReaderCountSize = 1;
		private static readonly int TypeSize = 2 + Texture2DType.Length + 4;
		private const int SharedResourceCountSize = 1;
		private const int ObjectHeaderSize = 21;
		
		private static readonly int MetadataSize =
			HeaderSize + CompressedFileSize + TypeReaderCountSize +
			TypeSize + SharedResourceCountSize + ObjectHeaderSize;


		private static void WriteCompressedData(BinaryWriter writer, Bitmap png) {
			using (MemoryStream stream = new MemoryStream()) {
				byte[] uncompressedData;
				using (BinaryWriter writer2 = new BinaryWriter(stream)) {
					WriteData(png, writer2);
					uncompressedData = stream.ToArray();
				}
				byte[] compressedData = XCompress.Compress(uncompressedData);
				writer.Write(6 + 4 + 4 + compressedData.Length); // compressed file size including headers
				writer.Write(uncompressedData.Length); // uncompressed data size (exluding headers! only the data)
				writer.Write(compressedData);
			}
		}

		private static void WriteData(Bitmap bmp, BinaryWriter writer) {
			Xnb.Write7BitEncodedInt(writer, 1);					// type-reader-count
			Xnb.Write7BitEncodedString(writer, Texture2DType);	// type-reader-name
			writer.Write((int)0);								// reader version number
			Xnb.Write7BitEncodedInt(writer, 0);                 // shared-resource-count
			// writing the image pixel data
			writer.Write((byte)1);
			writer.Write((int)0);
			writer.Write(bmp.Width);
			writer.Write(bmp.Height);
			writer.Write((int)1);
			writer.Write(bmp.Width * bmp.Height * 4);
			if (bmp.PixelFormat != PixelFormat.Format32bppArgb) {
				Bitmap newBmp = new Bitmap(bmp);
				bmp = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format32bppArgb);
			}
			BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			try {
				var length = bitmapData.Stride * bitmapData.Height;
				byte[] bytes = new byte[length];
				Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
				for (int i = 0; i < bytes.Length; i += 4) {
					byte b = bytes[i];
					bytes[i] = bytes[i + 2];
					bytes[i + 2] = b;
				}
				writer.Write(bytes);
			}
			catch (Exception ex) {
				throw ex;
			}
			finally {
				bmp.UnlockBits(bitmapData);
			}
		}

		public static bool Convert(string inputFile, string outputFile, bool changeExtension, bool compressed, bool reach) {
			if (changeExtension) {
				outputFile = Path.ChangeExtension(outputFile, ".xnb");
			}
			if (!Directory.Exists(Path.GetDirectoryName(inputFile))) {
				throw new DirectoryNotFoundException("Could not find a part of the path '" + inputFile + "'.");
			}
			using (Bitmap bmp = new Bitmap(inputFile)) {
				using (FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write)) {
					using (BinaryWriter writer = new BinaryWriter(stream)) {
						writer.Write(Encoding.UTF8.GetBytes("XNB"));    // format-identifier
						writer.Write(Encoding.UTF8.GetBytes("w"));      // target-platform
						writer.Write((byte)5);                          // xnb-format-version
						byte flagBits = 0;
						if (!reach) {
							flagBits |= 0x01;
						}
						if (compressed) {
							flagBits |= 0x80;
						}
						writer.Write(flagBits); // flag-bits; 00=reach, 01=hiprofile, 80=compressed, 00=uncompressed
						if (compressed) {
							WriteCompressedData(writer, bmp);
						}
						else {
							writer.Write(MetadataSize + bmp.Width * bmp.Height * 4); // compressed file size
							WriteData(bmp, writer);
						}
					}
				}
			}
			return true;
		}
	}
}
