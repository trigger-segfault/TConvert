/*******************************************************************************
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
using System.Text;
using System.Threading.Tasks;
using TConvert.Util;

namespace TConvert.Extract {
	public class LzxDecoder {

		private const int MinMatch = 2;
		private const int NumChars = 256;
		private const int PretreeNumElements = 20;
		private const int AlignedNumElements = 8;
		private const int NumPrimaryLengths = 7;
		private const int NumSecondaryLengths = 249;

		private const int PretreeMaxSymbols = PretreeNumElements;
		private const int PretreeTableBits = 6;
		private const int MaintreeMaxSymbols = NumChars + 50 * 8;
		private const int MaintreeTableBits = 12;
		private const int LengthMaxSymbols = NumSecondaryLengths + 1;
		private const int LengthTableBits = 12;
		private const int AlignedMaxSymbols = AlignedNumElements;
		private const int AlignedTableBits = 7;

		private static readonly int[] positionBase;
		private static readonly int[] extraBits;

		static LzxDecoder() {
			extraBits = new int[52];
			for (int i = 0, j = 0; i <= 50; i += 2) {
				extraBits[i] = extraBits[i + 1] = (byte)j;
				if ((i != 0) && (j < 17))
					j++;
			}

			positionBase = new int[51];
			for (int i = 0, j = 0; i <= 50; i++) {
				positionBase[i] = j;
				j += 1 << extraBits[i];
			}
		}

		private enum LzxBlockType {
			Invalid,
			Verbatim,
			Aligned,
			Uncompressed
		}


		/**<summary>LRU offset system</summary>*/
		private int R0, R1, R2;

		/**<summary>Decoding window</summary>*/
		private byte[] window;
		private int windowSize;
		private int windowPos;

		private int mainElementCount;

		/**<summary>Current block information</summary>*/
		private LzxBlockType blockType;
		private int blockLength = 0;
		private int blockRemaining = 0;

		/**<summary>Intel CALL instruction optimization</summary>*/
		private int intelFileSize;
		private int intelCurrentPosition;
		private bool intelStarted;

		/**<summary>Decoding tables</summary>*/
		private HuffTable preTree;
		private HuffTable mainTree;
		private HuffTable lengthTree;
		private HuffTable alignedTree;

		// private int actualSize = 0;
		private int framesRead = 0;

		/**<summary>Whether the file header should be read</summary>*/
		private bool readHeader = true;


		public LzxDecoder() {
			preTree = new HuffTable(PretreeMaxSymbols, PretreeTableBits);
			mainTree = new HuffTable(MaintreeMaxSymbols, MaintreeTableBits);
			lengthTree = new HuffTable(LengthMaxSymbols, LengthTableBits);
			alignedTree = new HuffTable(AlignedMaxSymbols, AlignedTableBits);
		}

		private void Reset() {
			R0 = R1 = R2 = 1;
			readHeader = true;
			windowSize = 1 << 16;
			// actualSize = windowSize;
			window = new byte[windowSize];
			window.Fill((byte)0xDC);
			
			windowPos = 0;
			mainElementCount = NumChars + (16 << 4);
			readHeader = true;
			framesRead = 0;
			blockRemaining = 0;
			blockType = LzxBlockType.Invalid;
			intelCurrentPosition = 0;
			intelStarted = false;

			preTree.Reset();
			mainTree.Reset();
			lengthTree.Reset();
			alignedTree.Reset();
		}

		public void Decompress(BinaryReader input, int inputLength,
				MemoryStream output, int outputLength) {

			Reset();

			int endPosition = (int)input.BaseStream.Position + inputLength;
			// the size of the input (compressed data)
			int blockSize;
			// the size of the output (decompressed data)
			int frameSize;
			int pos = (int)input.BaseStream.Position;

			while ((int)input.BaseStream.Position < endPosition) {

				// System.out.println("pos=" + pos);

				// seek to the correct position
				// input.rewind();
				input.BaseStream.Position = pos;

				// System.out.println("input.position= " + input.position());

				int hi, lo;
				hi = input.ReadByte() & 0xFF;
				lo = input.ReadByte() & 0xFF;
				blockSize = (hi << 8) | lo;
				// all blocks by default will output 32Kb of data, so thus
				// is our frame size
				frameSize = 0x8000;
				// ... unless this block is special, that it outputs a different
				// amount of data. this blocks header is identified by a 0xFF byte
				if (hi == 0xFF) {
					// that means the lo byte was the hi byte
					hi = lo;
					lo = input.ReadByte() & 0xFF;
					// ... which combined to a different output/frame size for this
					// particular block
					frameSize = (hi << 8) | lo;
					// now get our block size
					hi = input.ReadByte() & 0xFF;
					lo = input.ReadByte() & 0xFF;
					blockSize = (hi << 8) | lo;
					pos += 5;
				}
				else {
					pos += 2;
				}

				// System.out.println("FrameSize=" + frameSize);
				// System.out.println("#BlockSize=" + blockSize);

				// either says there is nothing to decode
				if (blockSize == 0 || frameSize == 0) {
					// System.out.println("Done decompressing");
					break;
				}

				DecompressBlock(input, blockSize, output, frameSize);
				pos += blockSize;
			}

		}

		private void DecompressBlock(BinaryReader input, int inputLength,
				MemoryStream output, int outputLength) {
			BinaryReader outputReader = new BinaryReader(output);
			BinaryWriter outputWriter = new BinaryWriter(output);

			int startpos = (int)input.BaseStream.Position;
			int endpos = startpos + inputLength;

			LzxBuffer buffer = new LzxBuffer(input);

			if (readHeader) {
				if (buffer.ReadBits(1) == 1) {
					// Intel optimization header
					int hi = buffer.ReadBits(16);
					int lo = buffer.ReadBits(16);
					intelFileSize = (hi << 16) | lo;

					// System.out.println("Intel file size: " + intelFileSize);
				}

				readHeader = false;
			}

			int window_posn = windowPos;
			int window_size = windowSize;
			int R0 = this.R0;
			int R1 = this.R1;
			int R2 = this.R2;

			int togo = outputLength;
			int this_run, main_element, match_length, match_offset, length_footer, extra, verbatim_bits;
			int rundest, runsrc, copy_length, aligned_bits;

			// System.out.println("window_posn=" + window_posn);
			// System.out.println("window_size=" + window_size);
			// System.out.println("R0=" + R0);
			// System.out.println("R1=" + R1);
			// System.out.println("R2=" + R2);

			while (togo > 0) {
				// System.out.println("Togo: " + togo);

				if (blockRemaining == 0) {
					// System.out.println("Current block type: " + blockType);
					if (blockType == LzxBlockType.Uncompressed) {

						// realign bitstream to word
						if ((blockLength & 1) == 1) {
							input.ReadByte();
						}
						buffer.Reset();
					}

					int nextBlockType = buffer.ReadBits(3);
					if (nextBlockType > 3) {
						throw new Exception("Invalid block type: " + nextBlockType);
					}

					blockType = (LzxBlockType)nextBlockType;

					// System.out.println("New block type: " + blockType);

					int a = buffer.ReadBits(16);
					int b = buffer.ReadBits(8);

					blockLength = (a << 8) | b;
					blockRemaining = blockLength;

					// System.out.println("Block length: " + blockLength);

					switch (blockType) {
					case LzxBlockType.Aligned:
						for (int i = 0, j = 0; i < 8; ++i) {
							j = buffer.ReadBits(3);
							alignedTree.Length[i] = (byte)j;
							// System.out.println("I= " + i + ", J=" + j);
						}
						alignedTree.MakeDecodeTable();
						/*
						 * Rest of aligned header is the same as verbatim,
						 * fall through case.
						 */
						goto case LzxBlockType.Verbatim;
					case LzxBlockType.Verbatim:
						ReadLengths(mainTree.Length, 0, 256, buffer);
						ReadLengths(mainTree.Length, 256, mainElementCount, buffer);
						mainTree.MakeDecodeTable();
						if (mainTree.Length[0xE8] != 0)
							intelStarted = true;

						ReadLengths(lengthTree.Length, 0, NumSecondaryLengths, buffer);
						lengthTree.MakeDecodeTable();
						break;
					case LzxBlockType.Uncompressed:
						intelStarted = true; /* because we can't assume otherwise */
						buffer.EnsureBits(16); /* get up to 16 pad bits into the buffer */
						if (buffer.RemainingBits > 16) {
							input.BaseStream.Position -= 2; /* and align the bitstream! */
						}
						byte hi, mh, ml, lo;
						lo = input.ReadByte();
						ml = input.ReadByte();
						mh = input.ReadByte();
						hi = input.ReadByte();
						R0 = (int)(lo | ml << 8 | mh << 16 | hi << 24);
						lo = input.ReadByte();
						ml = input.ReadByte();
						mh = input.ReadByte();
						hi = input.ReadByte();
						R1 = (int)(lo | ml << 8 | mh << 16 | hi << 24);
						lo = input.ReadByte();
						ml = input.ReadByte();
						mh = input.ReadByte();
						hi = input.ReadByte();
						R2 = (int)(lo | ml << 8 | mh << 16 | hi << 24);
						break;
					default:
						throw new Exception("Unknown block type " + blockType);
					}
				}

				/* buffer exhaustion check */
				if ((int)input.BaseStream.Position > (startpos + inputLength)) {
					/*
					 * it's possible to have a file where the next run is less than
					 * 16 bits in size. In this case, the READ_HUFFSYM() macro used
					 * in building the tables will exhaust the buffer, so we should
					 * allow for this, but not allow those accidentally read bits to
					 * be used (so we check that there are at least 16 bits
					 * remaining - in this boundary case they aren't really part of
					 * the compressed data)
					 */
					// System.out.println("WTF");

					if ((int)input.BaseStream.Position > (startpos + inputLength + 2) || buffer.RemainingBits < 16)
						throw new Exception();
				}

				while ((this_run = (int)blockRemaining) > 0 && togo > 0) {
					if (this_run > togo)
						this_run = togo;
					togo -= this_run;
					blockRemaining -= this_run;

					/* apply 2^x-1 mask */
					window_posn &= window_size - 1;

					// System.out.println("this_run= " + this_run);
					// System.out.println("togo= " + togo);
					// System.out.println("blockRemaining= " + blockRemaining);
					// System.out.println("window_posn= " + window_posn);
					// System.out.println("window_size= " + window_size);

					/* runs can't straddle the window wraparound */
					if ((window_posn + this_run) > window_size)
						throw new Exception("(window_posn + this_run) > window_size");

					switch (blockType) {
					case LzxBlockType.Verbatim:
						while (this_run > 0) {
							main_element = mainTree.ReadHuffSym(buffer);
							// main_element = (int)ReadHuffSym(m_state.MAINTREE_table, m_state.MAINTREE_len,
							// LzxConstants.MAINTREE_MAXSYMBOLS, LzxConstants.MAINTREE_TABLEBITS,
							// bitbuf);
							if (main_element < NumChars) {
								/* literal: 0 to NUM_CHARS-1 */
								window[window_posn++] = (byte)main_element;
								this_run--;
							}
							else {
								/* match: NUM_CHARS + ((slot<<3) | length_header (3 bits)) */
								main_element -= NumChars;

								match_length = main_element & NumPrimaryLengths;
								if (match_length == NumPrimaryLengths) {
									// length_footer = (int)ReadHuffSym(m_state.LENGTH_table, m_state.LENGTH_len,
									// LzxConstants.LENGTH_MAXSYMBOLS, LzxConstants.LENGTH_TABLEBITS,
									// bitbuf);
									length_footer = lengthTree.ReadHuffSym(buffer);
									match_length += length_footer;
								}
								match_length += MinMatch;

								match_offset = main_element >> 3;

								if (match_offset > 2) {
									/* not repeated offset */
									if (match_offset != 3) {
										extra = extraBits[match_offset];
										verbatim_bits = (int)buffer.ReadBits(extra);
										match_offset = (int)positionBase[match_offset] - 2 + verbatim_bits;
									}
									else {
										match_offset = 1;
									}

									/* update repeated offset LRU queue */
									R2 = R1;
									R1 = R0;
									R0 = match_offset;
								}
								else if (match_offset == 0) {
									match_offset = (int)R0;
								}
								else if (match_offset == 1) {
									match_offset = (int)R1;
									R1 = R0;
									R0 = match_offset;
								}
								else /* match_offset == 2 */
								{
									match_offset = (int)R2;
									R2 = R0;
									R0 = match_offset;
								}

								rundest = (int)window_posn;
								this_run -= match_length;

								/* copy any wrapped around source data */
								if (window_posn >= match_offset) {
									/* no wrap */
									runsrc = rundest - match_offset;
								}
								else {
									runsrc = rundest + ((int)window_size - match_offset);
									copy_length = match_offset - (int)window_posn;
									if (copy_length < match_length) {
										match_length -= copy_length;
										window_posn += copy_length;
										while (copy_length-- > 0)
											window[rundest++] = window[runsrc++];
										runsrc = 0;
									}
								}
								window_posn += match_length;

								/* copy match data - no worries about destination wraps */
								while (match_length-- > 0)
									window[rundest++] = window[runsrc++];
							}
						}
						break;

					case LzxBlockType.Aligned:
						while (this_run > 0) {
							// main_element = (int)ReadHuffSym(m_state.MAINTREE_table, m_state.MAINTREE_len,
							// LzxConstants.MAINTREE_MAXSYMBOLS, LzxConstants.MAINTREE_TABLEBITS,
							// bitbuf);
							main_element = mainTree.ReadHuffSym(buffer);

							// System.err.println("main_element= " + main_element);
							// System.err.println("this_run= " + this_run);

							if (main_element < NumChars) {
								/* literal 0 to NUM_CHARS-1 */
								window[window_posn++] = (byte)main_element;
								this_run--;
							}
							else {
								/* match: NUM_CHARS + ((slot<<3) | length_header (3 bits)) */
								main_element -= NumChars;

								match_length = main_element & NumPrimaryLengths;
								// System.err.println("match_length= " + match_length);
								if (match_length == NumPrimaryLengths) {
									// length_footer = (int)ReadHuffSym(m_state.LENGTH_table, m_state.LENGTH_len,
									// LzxConstants.LENGTH_MAXSYMBOLS, LzxConstants.LENGTH_TABLEBITS,
									// bitbuf);
									length_footer = lengthTree.ReadHuffSym(buffer);

									// System.err.println("length_footer= " + length_footer);

									match_length += length_footer;
								}
								match_length += MinMatch;

								match_offset = main_element >> 3;

								// System.err.println("match_offset= " + match_offset);

								if (match_offset > 2) {
									/* not repeated offset */
									extra = extraBits[match_offset];
									match_offset = (int)positionBase[match_offset] - 2;
									if (extra > 3) {
										/* verbatim and aligned bits */
										extra -= 3;
										verbatim_bits = (int)buffer.ReadBits(extra);
										match_offset += (verbatim_bits << 3);
										// aligned_bits = (int)ReadHuffSym(m_state.ALIGNED_table, m_state.ALIGNED_len,
										// LzxConstants.ALIGNED_MAXSYMBOLS, LzxConstants.ALIGNED_TABLEBITS,
										// bitbuf);
										aligned_bits = alignedTree.ReadHuffSym(buffer);
										match_offset += aligned_bits;
									}
									else if (extra == 3) {
										/* aligned bits only */
										// aligned_bits = (int)ReadHuffSym(m_state.ALIGNED_table, m_state.ALIGNED_len,
										// LzxConstants.ALIGNED_MAXSYMBOLS, LzxConstants.ALIGNED_TABLEBITS,
										// bitbuf);
										aligned_bits = alignedTree.ReadHuffSym(buffer);
										match_offset += aligned_bits;
									}
									else if (extra > 0) /* extra==1, extra==2 */
									{
										/* verbatim bits only */
										verbatim_bits = buffer.ReadBits(extra);
										match_offset += verbatim_bits;
									}
									else /* extra == 0 */
									{
										/* ??? */
										match_offset = 1;
									}

									/* update repeated offset LRU queue */
									R2 = R1;
									R1 = R0;
									R0 = match_offset;
								}
								else if (match_offset == 0) {
									match_offset = (int)R0;
								}
								else if (match_offset == 1) {
									match_offset = (int)R1;
									R1 = R0;
									R0 = match_offset;
								}
								else /* match_offset == 2 */
								{
									match_offset = (int)R2;
									R2 = R0;
									R0 = match_offset;
								}

								rundest = (int)window_posn;
								this_run -= match_length;

								/* copy any wrapped around source data */
								if (window_posn >= match_offset) {
									/* no wrap */
									runsrc = rundest - match_offset;
								}
								else {
									runsrc = rundest + ((int)window_size - match_offset);
									copy_length = match_offset - (int)window_posn;
									if (copy_length < match_length) {
										match_length -= copy_length;
										window_posn += copy_length;
										while (copy_length-- > 0) {
											window[rundest++] = window[runsrc++];
										}
										runsrc = 0;
									}
								}
								window_posn += match_length;

								/* copy match data - no worries about destination wraps */
								while (match_length-- > 0) {
									window[rundest++] = window[runsrc++];
								}
							}
						}
						break;

					case LzxBlockType.Uncompressed:
						if (((int)input.BaseStream.Position + this_run) > endpos)
							throw new Exception("(input.position() + this_run) > endpos");

						// byte[] temp_buffer = new byte[this_run];
						// inData.Read(temp_buffer, 0, this_run);
						// temp_buffer.CopyTo(window, (int)window_posn);

						// input.get(window, window_posn, window.length - window_posn);
						input.Read(window, window_posn, this_run);
						window_posn += this_run;
						break;

					default:
						throw new Exception("Invalid block type: " + blockType);
					}
				}
			}

			if (togo != 0)
				throw new Exception("togo != 0");

			int start_window_pos = (int) window_posn;

			if (start_window_pos == 0) {
				start_window_pos = (int)window_size;
			}

			start_window_pos -= outputLength;

			// System.out.println("start_window_pos= " + start_window_pos);
			// System.out.println("outputLength= " + outputLength);
			// System.out.println("input.position= " + input.position());

			outputWriter.Write(window, start_window_pos, outputLength);
			// outData.Write(window, start_window_pos, outLen);

			this.windowPos = window_posn;
			this.R0 = R0;
			this.R1 = R1;
			this.R2 = R2;

			// TODO finish intel E8 decoding
			/* intel E8 decoding */
			if ((framesRead++ < 32768) && intelFileSize != 0) {
				if (outputLength <= 6 || !intelStarted) {
					intelCurrentPosition += outputLength;
				}
				else {
					int dataend = outputLength - 10;
					int curpos = intelCurrentPosition;

					intelCurrentPosition = (int)curpos + outputLength;

					while ((int)output.Position < dataend) {
						if (outputReader.ReadByte() != 0xE8) {
							curpos++;
							continue;
						}
					}
				}
				// TODO: Is this an error?
				// return -1;
			}
			// return 0;
		}

		private void ReadLengths(byte[] lens, int first, int last, LzxBuffer buffer) {
			int x, y;
			int z;

			// hufftbl pointer here?

			for (x = 0; x < 20; x++) {
				y = buffer.ReadBits(4);
				preTree.Length[x] = (byte)y;
			}
			preTree.MakeDecodeTable();
			// MakeDecodeTable(LzxConstants.PRETREE_MAXSYMBOLS, LzxConstants.PRETREE_TABLEBITS,
			// m_state.PRETREE_len, m_state.PRETREE_table);

			for (x = first; x < last;) {
				z = preTree.ReadHuffSym(buffer);
				if (z == 17) {
					y = buffer.ReadBits(4);
					y += 4;
					while (y-- != 0)
						lens[x++] = 0;
				}
				else if (z == 18) {
					y = buffer.ReadBits(5);
					y += 20;
					while (y-- != 0)
						lens[x++] = 0;
				}
				else if (z == 19) {
					y = buffer.ReadBits(1);
					y += 4;
					z = preTree.ReadHuffSym(buffer);
					z = lens[x] - z;
					if (z < 0)
						z += 17;
					while (y-- != 0)
						lens[x++] = (byte)z;
				}
				else {
					z = lens[x] - z;
					if (z < 0)
						z += 17;
					lens[x++] = (byte)z;
				}
			}
		}
	}
}
