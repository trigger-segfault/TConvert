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

namespace TConvert.Extract {
	public class LzxBuffer {
		private BinaryReader reader;
		private int remainingBits = 0;
		private int bitBuffer = 0;

		public LzxBuffer(BinaryReader reader) {
			this.reader = reader;
		}

		public void Reset() {
			remainingBits = 0;
			bitBuffer = 0;
		}

		public void EnsureBits(int bitCount) {
			if (bitCount < 0 || bitCount > 32) {
				throw new ArgumentException(bitCount.ToString());
			}

			while (remainingBits < bitCount) {
				int lo = reader.ReadByte() & 0xff;
				int hi = reader.ReadByte() & 0xff;
				bitBuffer |= ((hi << 8) | lo) << (4 * 8 - 16 - remainingBits);
				remainingBits += 16;
			}
		}

		public int PeekBits(int bitCount) {
			if (bitCount > remainingBits) {
				throw new ArgumentException("Not enough bits: required "
						+ bitCount + " has " + remainingBits);
			}

			return (int)((uint)bitBuffer >> (32 - bitCount));
		}

		public void RemoveBits(int bitCount) {
			bitBuffer <<= bitCount;
			remainingBits -= bitCount;
		}

		public int ReadBits(int bitCount) {
			int result = 0;

			if (bitCount > 0) {
				EnsureBits(bitCount);
				result = PeekBits(bitCount);
				RemoveBits(bitCount);
			}

			return result;
		}

		public int BitBuffer {
			get { return bitBuffer; }
		}

		public int RemainingBits {
			get { return remainingBits; }
		}
	}
}
