﻿/*******************************************************************************
 * Copyright (C) 2014-2015 Anton Gustafsson
 *
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 ******************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TConvert.Util;

namespace TConvert.Extract {
	public static class XactExtractor {
		// XWB parsing was adapted from MonoGame

		// Track codecs
		private const int MiniFormatTag_PCM = 0x0;
		private const int MiniFormatTag_XMA = 0x1;
		private const int MiniFormatTag_ADPCM = 0x2;
		private const int MiniFormatTag_WMA = 0x3;

		private const int Flag_Compact = 0x00020000;

		// WAV Encoding
		private static readonly byte[] Label_RIFF = Encoding.UTF8.GetBytes("RIFF");
		private static readonly byte[] Label_WAVE = Encoding.UTF8.GetBytes("WAVE");
		private static readonly byte[] Label_XWMA = Encoding.UTF8.GetBytes("XWMA");
		// Note the space after fmt.
		private static readonly byte[] Label_fmt = Encoding.UTF8.GetBytes("fmt ");
		private static readonly byte[] Label_dpds = Encoding.UTF8.GetBytes("dpds");
		private static readonly byte[] Label_data = Encoding.UTF8.GetBytes("data");
		private static readonly int WavHeaderSize =
			Label_RIFF.Length + 4 + Label_WAVE.Length + Label_fmt.Length +
			4 + 2 + 2 + 4 + 4 + 2 + 2 + Label_data.Length + 4;
		
		private const string WaveBankList = "TrackList.txt";

		/** Mapping of music wave bank indexes to their names */
		private static string[] TrackNames = {
			"Music_1", // Overworld Day
			"Music_2", // Eerie
			"Music_3", // Night
			"Music_4", // Underground
			"Music_5", // Boss 1
			"Music_6", // Title (Classic)
			"Music_7", // Jungle
			"Music_8", // Corruption
			"Music_9", // Hallow
			"Music_10", // Underground Corruption
			"Music_11", // Underground Hallow
			"Music_12", // Boss 2
			"Music_13", // Boss 3
			"Music_14", // Snow
			"Music_15", // Space Night
			"Music_16", // Crimson
			"Music_17", // Boss 4
			"Music_18", // Alt Overworld Day
			"Music_19", // Rain
			"Music_20", // Ice
			"Music_21", // Desert
			"Music_22", // Ocean Day
			"Music_23", // Dungeon
			"Music_24", // Plantera
			"Music_25", // Boss 5
			"Music_26", // Temple
			"Music_27", // Solar Eclipse
			"Music_28", // Rain Ambience
			"Music_29", // Mushrooms
			"Music_30", // Pumpkin Moon
			"Music_31", // Alt Underground
			"Music_32", // Frost Moon
			"Music_33", // Underground Crimson
			"Music_34", // The Towers
			"Music_35", // Pirate Invasion
			"Music_36", // Hell
			"Music_37", // Martian Madness
			"Music_38", // Lunar Boss
			"Music_39", // Goblin Invasion
			"Music_40", // Sandstorm
			"Music_41", // Old One's Army
			"Music_42", // Space Day
			"Music_43", // Ocean Night
			"Music_44", // Windy Day
			"Music_45", // Wind Ambience
			"Music_46", // Town Day
			"Music_47", // Town Night
			"Music_48", // Slime Rain
			"Music_49", // Overworld Day Remix
			"Music_50", // Journey's Beginning
			"Music_51", // Journey's Beginning Loop
			"Music_52", // Storm
			"Music_53", // Graveyard
			"Music_54", // Underground Jungle
			"Music_55", // Jungle Night
			"Music_56", // Queen Slime
			"Music_57", // Empress of Light
			"Music_58", // Duke Fishron
			"Music_59", // Morning Rain
			"Music_60", // Alt Title
			"Music_61", // Underground Desert
			"Music_62", // Otherworldly Rain
			"Music_63", // Otherworldly Day
			"Music_64", // Otherworldly Night
			"Music_65", // Otherworldly Underground
			"Music_66", // Otherworldly Desert
			"Music_67", // Otherworldly Ocean
			"Music_68", // Otherworldly Mushrooms
			"Music_69", // Otherworldly Dungeon
			"Music_70", // Otherworldly Space
			"Music_71", // Otherworldly Underworld
			"Music_72", // Otherworldly Snow
			"Music_73", // Otherworldly Corruption
			"Music_74", // Otherworldly Underground Corruption
			"Music_75", // Otherworldly Crimson
			"Music_76", // Otherworldly Underground Crimson
			"Music_77", // Otherworldly Ice
			"Music_78", // Otherworldly Underground Hallow
			"Music_79", // Otherworldly Eerie
			"Music_80", // Otherworldly Boss 2
			"Music_81", // Otherworldly Boss 1
			"Music_82", // Otherworldly Invasion
			"Music_83", // Otherworldly The Towers
			"Music_84", // Otherworldly Lunar Boss
			"Music_85", // Otherworldly Plantera
			"Music_86", // Otherworldly Jungle
			"Music_87", // Otherworldly Wall of Flesh
			"Music_88", // Otherworldly Hallow
			"Music_89", // Journey's End
			"Music_90", // Deerclops
			"Music_91" // Aether
		};

		static XactExtractor() {
			// Try to find updated names of wave bank songs.
			// This way even if TConvert is not maintained, the wavebank can be updated.
			string path = Path.Combine(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				WaveBankList
			);
			try {
				if (File.Exists(path)) {
					ReadWaveBankList(path);
					return;
				}
			}
			catch { }
			path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"My Games", "Terraria", WaveBankList
			);
			try {
				if (File.Exists(path)) {
					ReadWaveBankList(path);
					return;
				}
			}
			catch { }
			if (!string.IsNullOrEmpty(TerrariaLocator.TerrariaContentDirectory)) {
				path = Path.Combine(
					Path.GetDirectoryName(TerrariaLocator.TerrariaContentDirectory),
					WaveBankList
				);
				try {
					if (File.Exists(path)) {
						ReadWaveBankList(path);
						return;
					}
				}
				catch { }
			}
		}
		private static void ReadWaveBankList(string filepath) {
			StreamReader reader = new StreamReader(filepath);
			List<string> tracknames = new List<string>();
			do {
				string name = reader.ReadLine();
				if (name != string.Empty)
					tracknames.Add(name);
			} while (!reader.EndOfStream);
			reader.Close();
			TrackNames = tracknames.ToArray();
		}

		private static void Status(String status) {
		}

		private static void Percentage(float percentage) {
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
		/**
		 * @param inputFile The XWB file to extract
		 * @param outputDirectory The directory to put the extracted files inside
		 * @param statusReporter The status reporter to use for reporting which tracks that are currently extracted.
		 * 
		 * @throws XnbException If the XWB file was malformed
		 * @throws IOException If an I/O error occurs
		 */
		public static bool Extract(string inputFile, string outputDirectory) {
			Status("Parsing XWB file header");
			Percentage(0f);

			//ByteBuffer buffer = ByteBuffer.wrap(FileUtils.readFileToByteArray(inputFile));
			//buffer.order(ByteOrder.LITTLE_ENDIAN);
			BinaryReader reader = new BinaryReader(new FileStream(inputFile, FileMode.Open));

			int Format = 0;
			int PlayRegionLength = 0;
			int PlayRegionOffset = 0;

			int wavebank_offset = 0;

			if (!CompareBytesToString(reader.ReadBytes(4), "WBND")) {
				throw new XwbException("not an XWB file: " + Path.GetFileName(inputFile));
			}

			int Version = reader.ReadInt32();

			// Skip trailing bytes of the version
			reader.ReadInt32();

			if (Version != 46) {
				throw new XwbException("unsupported version: " + Version);
			}

			int[] segmentOffsets = new int[5];
			int[] segmentLengths = new int[5];

			for (int i = 0; i< 5; i++) {
				segmentOffsets[i] = reader.ReadInt32();
				segmentLengths[i] = reader.ReadInt32();
			}

			reader.BaseStream.Position = segmentOffsets[0];

			int Flags = reader.ReadInt32();
			int EntryCount = reader.ReadInt32();

			// Skip terraria's wave bank's name. "Wave Bank".
			reader.BaseStream.Position += 64;

			int EntryMetaDataElementSize = reader.ReadInt32();
			reader.ReadInt32(); // EntryNameElementSize
			reader.ReadInt32(); // Alignment
			wavebank_offset = segmentOffsets[1];

			if ((Flags & Flag_Compact) != 0) {
				throw new XwbException("compact wavebanks are not supported");
			}

			int playregion_offset = segmentOffsets[4];
			for (int current_entry = 0; current_entry<EntryCount; current_entry++) {
				String track = current_entry < TrackNames.Length ? TrackNames[current_entry] : (current_entry + 1) + " Unknown";

				Status("Extracting " + track);
				Percentage(0.1f + (0.9f / EntryCount) * current_entry);

				reader.BaseStream.Position = wavebank_offset;
				if (EntryMetaDataElementSize >= 4)
					reader.ReadInt32(); // FlagsAndDuration
				if (EntryMetaDataElementSize >= 8)
					Format = reader.ReadInt32();
				if (EntryMetaDataElementSize >= 12)
					PlayRegionOffset = reader.ReadInt32();
				if (EntryMetaDataElementSize >= 16)
					PlayRegionLength = reader.ReadInt32();
				if (EntryMetaDataElementSize >= 20)
					reader.ReadInt32(); // LoopRegionOffset
				if (EntryMetaDataElementSize >= 24)
					reader.ReadInt32(); // LoopRegionLength

				wavebank_offset += EntryMetaDataElementSize;
				PlayRegionOffset += playregion_offset;

				int codec = (Format) & ((1 << 2) - 1);
				int chans = (Format >> (2)) & ((1 << 3) - 1);
				int rate = (Format >> (2 + 3)) & ((1 << 18) - 1);
				int align = (Format >> (2 + 3 + 18)) & ((1 << 8) - 1);

				reader.BaseStream.Position = PlayRegionOffset;
				byte[] audiodata = reader.ReadBytes(PlayRegionLength);

				// The codecs used by Terraria are currently xWMA and ADPCM.
				// The xWMA format is not supported by FNA, so it's only used
				// on Windows. This implementation uses ffmpeg to convert the raw
				// xWMA data to WAVE; a minified Windows executable is embedded.
				// PCM was introduced for the last tracks in the 1.3.3 update.
				string path = Path.Combine(outputDirectory, track + ".wav");
				if (codec == MiniFormatTag_PCM) {
					FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
					BinaryWriter writer = new BinaryWriter(stream);
					stream.SetLength(0);
					writer.Write(Label_RIFF); // chunk id
					writer.Write(audiodata.Length + 36); // chunk size
					writer.Write(Label_WAVE); // RIFF type
					writer.Write(Label_fmt); // chunk id
					writer.Write((int)16); // format header size
					writer.Write((short)1); // format (PCM)
					writer.Write((short)chans); // channels
					writer.Write(rate); // samples per second
					int bitsPerSample = 16;
					int blockAlign = (bitsPerSample / 8) * chans;
					writer.Write(rate * blockAlign); // byte rate/ average bytes per second
					writer.Write((short) blockAlign);
					writer.Write((short) bitsPerSample);
					writer.Write(Label_data); // chunk id
					writer.Write(audiodata.Length); // data size


					writer.Write(audiodata);
					writer.Close();
				} else if (codec == MiniFormatTag_WMA) {
					// Note that it could still be another codec than xWma,
					// but that scenario isn't handled here.

					// This part has been ported from XWMA-to-pcm-u8
					// Not the most beautiful code in the world,
					// but it does the job.

					// I do not know if this code outputs valid XWMA files,
					// but FFMPEG accepts them so it's all right for this usage.

					//File xWmaFile = new File(outputDirectory, track + ".wma");

					//FileOutputStream xWmaOutput = FileUtils.openOutputStream(xWmaFile);
					// xWmaOutput.write(output.array(), output.arrayOffset(), output.position());
					string wmaPath = Path.Combine(outputDirectory, track + ".wma");
					BinaryWriter writer = new BinaryWriter(new FileStream(wmaPath, FileMode.OpenOrCreate));

					//BufferWriter output = new BufferWriter(xWmaOutput);
					//output.setOrder(ByteOrder.LITTLE_ENDIAN);
					writer.Write(Label_RIFF); // chunk id
					writer.Write(0); // Full file size, ignored by ffmpeg
					writer.Write(Label_XWMA); // RIFF type
					writer.Write(Label_fmt); // chunk id
					writer.Write((int)18); // format header size
					writer.Write((short)0x161); // format (PCM)
					writer.Write((short)chans); // channels
					writer.Write(rate); // samples per second


					int[] wmaAverageBytesPerSec = new int[] {
						12000, 24000, 4000, 6000, 8000, 20000
					};
					int[] wmaBlockAlign = new int[] {
						929, 1487, 1280, 2230, 8917, 8192, 4459, 5945,
						2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462
					};

					int averageBytesPerSec = align > wmaAverageBytesPerSec.Length ? wmaAverageBytesPerSec[align >> 5] : wmaAverageBytesPerSec[align];

					int blockAlign = align > wmaBlockAlign.Length ? wmaBlockAlign[align & 0xf] : wmaBlockAlign[align];

					writer.Write(averageBytesPerSec);
					writer.Write((short) blockAlign);
					writer.Write((short) 16);
					writer.Write((short) 0);
					writer.Write(Label_dpds);
					int packetLength = blockAlign;
					int packetNum = audiodata.Length / packetLength;
					writer.Write(packetNum * 4);

					int fullSize = (PlayRegionLength * averageBytesPerSec % 4096 != 0) ? (1 + (int) (PlayRegionLength
							* averageBytesPerSec / 4096)) * 4096
							: PlayRegionLength;
					int allBlocks = fullSize / 4096;
					int avgBlocksPerPacket = allBlocks / packetNum;
					int spareBlocks = allBlocks - (avgBlocksPerPacket * packetNum);

					int accu = 0;
					for (int i = 0; i<packetNum; ++i) {
						accu += avgBlocksPerPacket* 4096;
						if (spareBlocks != 0) {
							accu += 4096;
							--spareBlocks;
						}
						writer.Write(accu);
					}

					writer.Write(Label_data);

					writer.Write(PlayRegionLength);
					writer.Write(audiodata);
					// Replacing the file size placeholder, dosen't matter with ffmpeg
					// int pos = output.position();
					// output.position(odRIChunkSize);
					// output.putInt(pos - 8);
					// output.position(pos);
					writer.Close();
					
					FFmpeg.Convert(wmaPath, path);

					File.Delete(wmaPath);
				} else if (codec == MiniFormatTag_ADPCM) {
					// Convert ADPCM data to PCM
					audiodata = ADPCMConverter.ConvertToPCM(audiodata, (short)chans, (short)align);
					// Encode PCM as a WAVE file; note that most magic values used
					// here were obtained via trial and error, so it might break...
					//ByteBuffer writeBuffer = ByteBuffer.allocate(wavHeaderSize);
					//writeBuffer.order(ByteOrder.LITTLE_ENDIAN);
					FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
					BinaryWriter writer = new BinaryWriter(stream);
					stream.SetLength(0);
					writer.Write(Label_RIFF);
					writer.Write(audiodata.Length + 36);
					writer.Write(Label_WAVE);
					writer.Write(Label_fmt);
					writer.Write((int)16);
					writer.Write((short) 1); // format code
					writer.Write((short) chans); // channels
					writer.Write(rate); // blocks per second
					writer.Write(rate * 4); // bytes per second
					writer.Write((short) 4); // data block alignment
					writer.Write((short) 16); // bits per sample
					writer.Write(Label_data);
					writer.Write(audiodata.Length); // dataChunkSize

					writer.Write(audiodata);
					writer.Close();
				} else {
					throw new XwbException("unimplemented codec " + codec);
				}
			}
			reader.Close();
			return true;
		}
	}
}
