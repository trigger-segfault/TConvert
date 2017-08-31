
/*
 * Implementation of LZX decoding,
 * a java port of LzxDecoder.cs from MonoGame 
 */

/* This file was derived from libmspack
 * (C) 2003-2004 Stuart Caie.
 * (C) 2011 Ali Scissons.
 *
 * The LZX method was created by Jonathan Forbes and Tomi Poutanen, adapted
 * by Microsoft Corporation.
 *
 * This source file is Dual licensed; meaning the end-user of this source file
 * may redistribute/modify it under the LGPL 2.1 or MS-PL licenses.
 */
// LGPL License
/* GNU LESSER GENERAL PUBLIC LICENSE version 2.1
 * LzxDecoder is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1 
 */
// MS-PL License
/* 
 * MICROSOFT PUBLIC LICENSE
 * This source code is subject to the terms of the Microsoft Public License (Ms-PL). 
 *  
 * Redistribution and use in source and binary forms, with or without modification, 
 * is permitted provided that redistributions of the source code retain the above 
 * copyright notices and this file header. 
 *  
 * Additional copyright notices should be appended to the list above. 
 * 
 * For details, see <http://www.opensource.org/licenses/ms-pl.html>. 
 */
/*
 * This derived work is recognized by Stuart Caie and is authorized to adapt
 * any changes made to lzxd.c in his libmspack library and will still retain
 * this dual licensing scheme. Big thanks to Stuart Caie!
 * 
 * DETAILS
 * This file is a pure C# port of the lzxd.c file from libmspack, with minor
 * changes towards the decompression of XNB files. The original decompression
 * software of LZX encoded data was written by Suart Caie in his
 * libmspack/cabextract projects, which can be located at 
 * http://http://www.cabextract.org.uk/
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TConvert.Util;

namespace TConvert.Extract {

	public static class XnbExtractor {

		private const int SurfaceFormatColor = 0;
		private const int HeaderSize = 14;

		private static readonly byte[] Label_RIFF = Encoding.UTF8.GetBytes("RIFF");
		private static readonly byte[] Label_WAVE = Encoding.UTF8.GetBytes("WAVE");

		private static readonly byte[] Label_fmt = Encoding.UTF8.GetBytes("fmt ");
		private static readonly byte[] Label_data = Encoding.UTF8.GetBytes("data");

		private static LzxDecoder lzxDecoder = new LzxDecoder();

		public static bool Extract(string inputFile, string outputFile, bool changeExtension, bool extractImages, bool extractSounds) {
			BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(inputFile)));
			
			if (!CompareBytesToString(reader.ReadBytes(3), "XNB")) {
				throw new XnbException("not an XNB file: " + Path.GetFileName(inputFile));
			}

			// Ignore target platform, it shouldn't matter
			int targetPlatform = reader.ReadByte();

			int version = reader.ReadByte();
			if (version != 5) {
				throw new XnbException("unsupported XNB version: " + version);
			}

			bool compressed = (reader.ReadByte() & 0x80) != 0;

			int compressedSize = reader.ReadInt32();
			int decompressedSize = (compressed ? reader.ReadInt32() : compressedSize);

			if (compressed) {
				MemoryStream decompressedStream = new MemoryStream(decompressedSize);

				lzxDecoder.Decompress(reader, compressedSize - HeaderSize, decompressedStream, decompressedSize);

				decompressedStream.Position = 0;

				reader.Close();
				reader = new BinaryReader(decompressedStream);
			}

			int typeReaderCount = Xnb.Get7BitEncodedInt(reader);

			// The first type reader is used for reading the primary asset
			string typeReaderName = Xnb.Get7BitEncodedString(reader);
			// The type reader version - Dosen't matter
			reader.ReadInt32();

			// Type reader names MIGHT contain assembly information
			int assemblyInformationIndex = typeReaderName.IndexOf(',');
			if (assemblyInformationIndex != -1)
				typeReaderName = typeReaderName.Substring(0, assemblyInformationIndex);

			// Skip the remaining type readers, as all types are known
			for (int k = 1; k < typeReaderCount; k++) {
				Xnb.Get7BitEncodedString(reader);
				reader.ReadInt32();
			}

			// Shared resources are unused by Terraria assets
			if (Xnb.Get7BitEncodedInt(reader) != 0) {
				throw new XnbException("shared resources are not supported");
			}

			if (Xnb.Get7BitEncodedInt(reader) != 1) {
				throw new XnbException("primary asset is null; this shouldn't happen");
			}

			string baseFileName = Path.GetFileNameWithoutExtension(inputFile);

			// Switch on the type reader name, excluding assembly information
			switch (typeReaderName) {
			case "Microsoft.Xna.Framework.Content.Texture2DReader": {
					if (!extractImages) {
						reader.Close();
						return false;
					}
					if (changeExtension) {
						outputFile = Path.ChangeExtension(outputFile, ".png");
					}

					int surfaceFormat = reader.ReadInt32();

					int width = reader.ReadInt32();
					int height = reader.ReadInt32();

					// Mip count
					int mipCount = reader.ReadInt32();
					// Size
					int size = reader.ReadInt32();

					if (mipCount != 1) {
						throw new XnbException("unexpected mipCount: " + mipCount);
					}

					if (size != width * height * 4) {
						throw new XnbException("unexpected size: " + size);
					}

					if (surfaceFormat != SurfaceFormatColor) {
						throw new XnbException("unexpected surface format: " + surfaceFormat);
					}
					byte[] source = reader.ReadBytes(size);
					for (int i = 0; i < width * height; i++) {
						byte swap = source[i * 4 + 0];
						source[i * 4 + 0] = source[i * 4 + 2];
						source[i * 4 + 2] = swap;
					}
					Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
					BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
					IntPtr data = bmpData.Scan0;

					int index = 0;
					for (int y = 0; y < height; y++) {
						int stride = width * 4;
						Marshal.Copy(source, index, data, stride);
						data += stride;
						index += stride;
					}
					bmp.UnlockBits(bmpData);
					bmp.Save(outputFile, ImageFormat.Png);
					return true;
				}
			case "Microsoft.Xna.Framework.Content.SoundEffectReader": {
					if (!extractSounds) {
						reader.Close();
						return false;
					}
					if (changeExtension) {
						outputFile = Path.ChangeExtension(outputFile, ".wav");
					}

					int audioFormat = reader.ReadInt32();
					if (audioFormat != 18) {
						throw new XnbException("unimplemented audio format: " + audioFormat);
					}

					int wavCodec = reader.ReadInt16();
					if (wavCodec != 1) {
						throw new XnbException("unimplemented wav codec: " + wavCodec);
					}

					int channels = reader.ReadInt16() & 0xffff;
					int samplesPerSecond = reader.ReadInt32();
					int averageBytesPerSecond = reader.ReadInt32();
					int blockAlign = reader.ReadInt16() & 0xffff;
					int bitsPerSample = reader.ReadInt16() & 0xffff;
					reader.ReadInt16(); // Unknown
					int dataChunkSize = reader.ReadInt32();

					// Note that the samples are written directly from the source buffer

					BinaryWriter writer = new BinaryWriter(new FileStream(outputFile, FileMode.OpenOrCreate));

					// Write header
					writer.Write(Label_RIFF);
					writer.Write(dataChunkSize + 36);
					writer.Write(Label_WAVE);
					writer.Write(Label_fmt);
					writer.Write((int)16);
					writer.Write((short)1);
					writer.Write((short)channels);
					writer.Write(samplesPerSecond);
					writer.Write(averageBytesPerSecond);
					writer.Write((short)blockAlign);
					writer.Write((short)bitsPerSample);
					writer.Write(Label_data);
					writer.Write(dataChunkSize);
					
					// Write samples
					writer.Write(reader.ReadBytes(dataChunkSize), 0, dataChunkSize);

					writer.Close();
					reader.Close();
					return true;
				}
			case "ReLogic.Graphics.DynamicSpriteFontReader":
			case "Microsoft.Xna.Framework.Content.SpriteFontReader":
			case "Microsoft.Xna.Framework.Content.EffectReader": {
					// Not supported
					return false;
				}
			default: {
					throw new XnbException("unsupported asset type: " + typeReaderName);
				}
			}

		}

		public static bool CompareBytes(byte[] a, byte[] b) {
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		public static bool CompareBytesToString(byte[] a, string s) {
			return CompareBytes(a, Encoding.ASCII.GetBytes(s));
		}
	}
}
