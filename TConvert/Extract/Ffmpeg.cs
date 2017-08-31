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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TConvert.Extract {
	public static class Ffmpeg {
		private static readonly string cmd = Path.Combine(
			Assembly.GetExecutingAssembly().Location,
			"ffmpeg.exe"
		);

		public static void Convert(string input, string output) {
			/*
			 * Note: From version 1.4, TExtract uses a special version of the
			 * ffmpeg executable configured with the following options:
			 * --disable-everything --enable-muxer=wav --enable-encoder=pcm_s16le
			 * --enable-demuxer=xwma --enable-decoder=wmav2
			 * --enable-protocol=file --enable-filter=aresample
			 * It can therefore not resample to another format without
			 * recompilation with appropriate options. The reason behind this
			 * is that the original weighted about 27 megabytes, whereas the new
			 * one weights only 1,5 megabytes.
			 */
			if (!File.Exists(cmd)) {
				File.WriteAllBytes(cmd, Properties.Resources.ffmpeg);
			}

			List<string> command = new List<string>();
			string arguments =
				"-i" + " " +
				Path.GetFullPath(input) + " " +
				"-acodec" + " " +
				"pcm_s16le" + " " +
				"-nostdin" + " " +
				"-ab" + " " +
				"128k" + " " +
				Path.GetFullPath(output);

			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = cmd;
			start.Arguments = arguments;
			start.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

			Process process = Process.Start(start);
			process.WaitForExit();
		}

		public static void Cleanup() {
			if (File.Exists(cmd)) {
				try {
					File.Delete(cmd);
				}
				catch { }
			}
		}
	}
}
