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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TConvert.Util;

namespace TConvert.Extract {
	public class HuffTable {
		private short[] table;
		private byte[] length;

		private int maxSymbols;
		private int tableBits;

		private const int Safety = 64;

		public HuffTable(int maxSymbols, int tableBits) {
			this.maxSymbols = maxSymbols;
			this.tableBits = tableBits;
			table = new short[(1 << tableBits) + (maxSymbols << 1)];
			length = new byte[maxSymbols + Safety];
		}

		public int MaxSymbols {
			get { return maxSymbols; }
		}

		public void MakeDecodeTable() {
			short sym;
			int leaf;
			byte bit_num = 1;
			int fill;
			int pos = 0; /* the current position in the decode table */
			int table_mask = (1 << (int) tableBits);
			int bit_mask = (int)((uint)table_mask >> 1); /* don't do 0 length codes */
			int next_symbol = bit_mask; /* base of allocation for long codes */

			/* fill entries for codes short enough for a direct mapping */
			while (bit_num <= tableBits) {
				for (sym = 0; sym < maxSymbols; sym++) {
					if (length[sym] == bit_num) {
						leaf = pos;

						if ((pos += bit_mask) > table_mask)
							return;// 1; /* table overrun */

						/* fill all possible lookups of this symbol with the symbol itself */
						fill = bit_mask;
						while (fill-- > 0)
							table[leaf++] = sym;
					}
				}
				uint zeroShift = (uint)bit_mask;
				zeroShift >>= 1;
				bit_mask = (int)zeroShift;
				bit_num++;
			}

			/* if there are any codes longer than tableBits */
			if (pos != table_mask) {
				/* clear the remainder of the table */
				for (sym = (short)pos; sym < table_mask; sym++)
					table[sym] = 0;

				/* give ourselves room for codes to grow by up to 16 more bits */
				pos <<= 16;
				table_mask <<= 16;
				bit_mask = 1 << 15;

				while (bit_num <= 16) {
					for (sym = 0; sym < maxSymbols; sym++) {
						if (length[sym] == bit_num) {
							leaf = (int)((uint)pos >> 16);
							for (fill = 0; fill < bit_num - tableBits; fill++) {
								/* if this path hasn't been taken yet, 'allocate' two entries */
								if (table[leaf] == 0) {
									table[(next_symbol << 1)] = 0;
									table[(next_symbol << 1) + 1] = 0;
									table[leaf] = (short)(next_symbol++);
								}
								/* follow the path and select either left or right for next bit */
								leaf = (table[leaf] << 1);
								if (((int)((uint)pos >> (int)(15 - fill)) & 1) == 1)
									leaf++;
							}
							table[leaf] = sym;

							if ((pos += bit_mask) > table_mask)
								return;// 1;
						}
					}
					uint zeroShift = (uint)bit_mask;
					zeroShift >>= 1;
					bit_mask = (int)zeroShift;
					bit_num++;
				}
			}

			/* full talbe? */
			if (pos == table_mask)
				return;// 0;

			/* either erroneous table, or all elements are 0 - let's find out. */
			for (sym = 0; sym < maxSymbols; sym++)
				if (length[sym] != 0)
					return;// 1;
		}

		public int ReadHuffSym(LzxBuffer buffer) {
			int i, j;
			buffer.EnsureBits(16);
			if ((i = table[buffer.PeekBits((byte)tableBits)]) >= maxSymbols) {
				j = (int)(1 << (int)((4 * 8) - tableBits));
				do {
					uint zeroShift = (uint)j;
					zeroShift >>= 1;
					j = (int)zeroShift;
					i <<= 1;
					i |= (buffer.BitBuffer & j) != 0 ? (int)1 : 0;

					if (j == 0)
						throw new Exception(); // return 0;

				} while ((i = table[i]) >= maxSymbols);
			}
			j = length[i];
			buffer.RemoveBits((byte)j);

			return i;
		}

		public void Reset() {
			
			table.Fill((short)0);
			length.Fill((byte)0);
		}

		public short[] Table {
			get { return table; }
		}

		public byte[] Length {
			get { return length; }
		}
	}
}
