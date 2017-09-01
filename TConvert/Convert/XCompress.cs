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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TConvert.Properties;
using TConvert.Util;

namespace TConvert.Convert {

	public static class XCompress {
		public enum XMEMCODEC_TYPE {
			XMEMCODEC_DEFAULT = 0,
			XMEMCODEC_LZX = 1
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct XMEMCODEC_PARAMETERS_LZX {
			[FieldOffset(0)]
			public int Flags;
			[FieldOffset(4)]
			public int WindowSize;
			[FieldOffset(8)]
			public int CompressionPartitionSize;
		}

		[DllImport("xcompress32.dll", EntryPoint = "XMemCompress")]
		public static extern int XMemCompress(int Context,
											  byte[] pDestination, ref int pDestSize,
											  byte[] pSource, int pSrcSize);

		[DllImport("xcompress32.dll", EntryPoint = "XMemCreateCompressionContext")]
		public static extern int XMemCreateCompressionContext(
			XMEMCODEC_TYPE CodecType, ref XMEMCODEC_PARAMETERS_LZX pCodecParams,
			int Flags, ref int pContext);

		[DllImport("xcompress32.dll", EntryPoint = "XMemDestroyCompressionContext")]
		public static extern void XMemDestroyCompressionContext(int Context);

		[DllImport("xcompress32.dll", EntryPoint = "XMemDecompress")]
		public static extern int XMemDecompress(int Context,
												byte[] pDestination, ref int pDestSize,
												byte[] pSource, int pSrcSize);

		[DllImport("xcompress32.dll", EntryPoint = "XMemCreateDecompressionContext")]
		public static extern int XMemCreateDecompressionContext(
			XMEMCODEC_TYPE CodecType,
			ref XMEMCODEC_PARAMETERS_LZX pCodecParams,
			int Flags, ref int pContext);

		[DllImport("xcompress32.dll", EntryPoint = "XMemDestroyDecompressionContext")]
		public static extern void XMemDestroyDecompressionContext(int Context);

		public static byte[] Compress(byte[] decompressedData) {
			// Setup our compression context
			int compressionContext = 0;

			XMEMCODEC_PARAMETERS_LZX codecParams;
			codecParams.Flags = 0;
			codecParams.WindowSize = 64 * 1024;
			codecParams.CompressionPartitionSize = 256 * 1024;

			XMemCreateCompressionContext(
				XMEMCODEC_TYPE.XMEMCODEC_LZX,
				ref codecParams, 0, ref compressionContext);

			// Now lets compress
			int compressedLen = decompressedData.Length * 2;
			byte[] compressed = new byte[compressedLen];
			int decompressedLen = decompressedData.Length;
			XMemCompress(compressionContext,
				compressed, ref compressedLen,
				decompressedData, decompressedLen);
			// Go ahead and destory our context
			XMemDestroyCompressionContext(compressionContext);

			// Resize our compressed data
			Array.Resize<byte>(ref compressed, compressedLen);

			// Now lets return it
			return compressed;
		}

		public static byte[] Decompress(byte[] compressedData, byte[] decompressedData) {
			// Setup our decompression context
			int DecompressionContext = 0;

			XMEMCODEC_PARAMETERS_LZX codecParams;
			codecParams.Flags = 0;
			codecParams.WindowSize = 64 * 1024;
			codecParams.CompressionPartitionSize = 256 * 1024;

			XMemCreateDecompressionContext(
				XMEMCODEC_TYPE.XMEMCODEC_LZX,
				ref codecParams, 0, ref DecompressionContext);

			// Now lets decompress
			int compressedLen = compressedData.Length;
			int decompressedLen = decompressedData.Length;
			try {
				XMemDecompress(DecompressionContext,
					decompressedData, ref decompressedLen,
					compressedData, compressedLen);
			}
			catch {
			}
			// Go ahead and destory our context
			XMemDestroyDecompressionContext(DecompressionContext);
			// Return our decompressed bytes
			return decompressedData;
		}

		public static readonly bool IsAvailable;

		static XCompress() {
			try {
				// Seems to make bigger files even larger in size. Also very slow. Let's ignore this.
				//EmbeddedDlls.ExtractEmbeddedDlls("xcompress32.dll", Resources.xcompress32);
				//EmbeddedDlls.LoadDll("xcompress32.dll");
				Compress(new byte[1]);
				IsAvailable = true;
			}
			catch (DllNotFoundException e) {
				IsAvailable = !e.Message.Contains("xcompress32.dll");
			}
		}
	}
}
