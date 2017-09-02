using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TConvert.Util;

namespace TConvert.Convert {
	public static class WavConverter {

		public static bool Convert(string inputFile, string outputFile, bool changeExtension) {
			if (changeExtension) {
				outputFile = Path.ChangeExtension(outputFile, ".xnb");
			}
			ushort wFormatTag;
			ushort nChannels;
			uint nSamplesPerSec;
			uint nAvgBytesPerSec;
			ushort nBlockAlign;
			ushort wBitsPerSample;

			int dataChunkSize;
			byte[] waveData;

			using (FileStream stream = new FileStream(inputFile, FileMode.Open)) {
				using (BinaryReader reader = new BinaryReader(stream)) {
					string format = new string(reader.ReadChars(4));
					if (format != "RIFF")
						throw new WavException("Invalid file format: " + format);
					
					uint fileLength = reader.ReadUInt32();
					if (fileLength != stream.Length-8)
						throw new WavException("File length mismatch: " + fileLength + " - should be " + stream.Length);
					
					format = new string(reader.ReadChars(4));
					if (format != "WAVE")
						throw new WavException("No WAVE tag");
					
					format = new string(reader.ReadChars(4));
					if (format != "fmt ")
						throw new WavException("No fmt tag");
					
					int a = reader.ReadInt32();
					if (a < 16)
						throw new WavException("Incorrect format length");
					a += (int)reader.BaseStream.Position;
					
					if ((wFormatTag = reader.ReadUInt16()) != 1)
						throw new Exception("Unimplemented wav codec (must be PCM)");
					
					nChannels = reader.ReadUInt16();
					
					nSamplesPerSec = reader.ReadUInt32();
					
					nAvgBytesPerSec = reader.ReadUInt32();
					
					nBlockAlign = reader.ReadUInt16();
					
					wBitsPerSample = reader.ReadUInt16();

					reader.BaseStream.Position = a;
					
					format = new string(reader.ReadChars(4));
					if (format != "data") throw new WavException("No data tag");

					if (nAvgBytesPerSec != (nSamplesPerSec * nChannels * (wBitsPerSample / 8)))
						throw new WavException("Average bytes per second number incorrect");
					if (nBlockAlign != (nChannels * (wBitsPerSample / 8)))
						throw new WavException("Block align number incorrect");

					dataChunkSize = reader.ReadInt32();
					waveData = reader.ReadBytes(dataChunkSize);
				}
			}

			using (FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate)) {
				using (BinaryWriter writer = new BinaryWriter(stream)) {
					stream.SetLength(0);
					// Format identifier
					writer.Write("XNB".ToCharArray());
					// TargetPlatform Windows
					writer.Write("w".ToCharArray());
					// XNB format version
					writer.Write((byte)5);
					// Flag bits: 
					writer.Write((byte)0);
					// File Size TODO
					writer.Write(dataChunkSize + 105); //??  61?
					// Type Reader count
					writer.Write7BitEncodedInt(1);
					// String reader name
					writer.Write("Microsoft.Xna.Framework.Content.SoundEffectReader");
					// reader version number
					writer.Write(0);
					// shared Resource Count
					writer.Write((byte)0);
					// Object Primary asset data....?
					{
						writer.Write((byte)1);
						// Format size
						writer.Write(18);
						// Format
						{
							// WORD wFormatTag;
							writer.Write((ushort)wFormatTag);
							//     ushort wFormatTag;
							// WORD nChannels;
							// ushort nChannels;
							writer.Write((ushort)nChannels);
							// DWORD nSamplesPerSec;
							// uint nSamplesPerSec;
							writer.Write((uint)nSamplesPerSec);
							//  DWORD nAvgBytesPerSec;
							//  uint nAvgBytesPerSec;
							writer.Write((uint)nAvgBytesPerSec);
							// WORD nBlockAlign;
							//  ushort nBlockAlign;
							writer.Write((ushort)nBlockAlign);
							//  WORD wBitsPerSample;
							//  ushort wBitsPerSample;
							writer.Write((ushort)wBitsPerSample);
							// WORD cbSize;
							writer.Write((ushort)0);
						}
						// Uint32 Data Size
						writer.Write(dataChunkSize);
						// Byte[data size] data
						writer.Write(waveData);

						// int32 loop start
						writer.Write(0);
						// int32 loop length
						writer.Write(dataChunkSize / nBlockAlign);
						// Console.WriteLine("Loop?? " + 1);

						// int32 duration
						writer.Write((int)(1000 * dataChunkSize / (nChannels * wBitsPerSample * nSamplesPerSec / 8)));

					}
				}
			}
			return true;
		}

	}
}
