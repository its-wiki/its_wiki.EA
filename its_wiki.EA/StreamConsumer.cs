using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Binary
{
	public abstract class StreamConsumer
	{
		/* Helper functions for random stuff */
		#region ETC Utilities
		protected int Peek(Stream stream)
		{
			if (!stream.CanSeek) throw new NotSupportedException("Cannot peek when stream is non seek-able!");

			int rslt = stream.ReadByte();
			stream.Seek(-1, SeekOrigin.Current);
			return rslt;
		}
		#endregion
		/* Helper functions to read/write INT16 values (short) */
		#region INT16
		[DebuggerStepThrough]
		protected short Read_INT16_LE(Stream input)
		{
			byte[] buffer = new byte[2];
			if (input.Read(buffer, 0, 2) != 2) throw new InvalidOperationException("[READINT16_LE]The stream could not give more than 2 bytes!");

			return (short)(buffer[0] | (buffer[1] << 8));
		}
		[DebuggerStepThrough]
		protected short Read_INT16_BE(Stream input)
		{
			byte[] buffer = new byte[2];
			if (input.Read(buffer, 0, 2) != 2) throw new InvalidOperationException("[READINT16_BE]The stream could not give more than 2 bytes!");

			return (short)((buffer[0] << 8) | buffer[1]);
		}
		[DebuggerStepThrough]
		protected byte[] Get_INT16_LE(short input)
		{
			return new byte[] { (byte)input, (byte)(input >> 8) };
		}
		[DebuggerStepThrough]
		protected byte[] Get_INT16_BE(short input)
		{
			return new byte[] { (byte)(input >> 8), (byte)(input) };
		}
		#endregion

		/* Helper functions to read/write INT32 values (int) */
		#region INT32
		[DebuggerStepThrough]
		protected int Read_INT32_LE(Stream input)
		{
			byte[] buffer = new byte[4];
			if (input.Read(buffer, 0, 4) != 4) throw new InvalidOperationException("[READINT32_LE]The stream could not give more than 4 bytes!");

			return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
		}
		[DebuggerStepThrough]
		protected int Read_INT32_BE(Stream input)
		{
			byte[] buffer = new byte[4];
			if (input.Read(buffer, 0, 4) != 4) throw new InvalidOperationException("[READINT32_BE]The stream could not give more than 4 bytes!");

			return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
		}
		[DebuggerStepThrough]
		protected byte[] Get_INT32_LE(int input)
		{
			return new byte[] { (byte)input, (byte)(input >> 8), (byte)(input >> 16), (byte)(input >> 24) };
		}
		[DebuggerStepThrough]
		protected byte[] Get_INT32_BE(int input)
		{
			return new byte[] { (byte)(input >> 24), (byte)(input >> 16), (byte)(input >> 8), (byte)(input) };
		}
		#endregion

		/* Helper functions to read UTF8 values (string) */
		#region UTF8
		[DebuggerStepThrough]
		protected string Read_UTF8(Stream input, int Length)
		{
			byte[] buffer = new byte[Length];
			if (input.Read(buffer, 0, buffer.Length) != buffer.Length) throw new InvalidOperationException("[READUTF8]The stream could not give enough bytes!");

			return System.Text.Encoding.UTF8.GetString(buffer);
		}
		[DebuggerStepThrough]
		protected string Read_UTF8_NT(Stream input)
		{
			using (MemoryStream bufferedStream = new MemoryStream())
			{
				int current = -1;
				while ((current = input.ReadByte()) != 0)
				{
					if (current == -1) throw new InvalidOperationException("[READUTF8_NT]The stream could not give enough bytes!");
					bufferedStream.WriteByte((byte)current);
				}

				return System.Text.Encoding.UTF8.GetString(bufferedStream.ToArray());
			}
		}
		#endregion

		/* Helper functions to read UTF16 values (string) */
		#region UTF16
		[DebuggerStepThrough]
		protected string Read_UTF16(Stream input, int Length)
		{
			byte[] buffer = new byte[Length * 2];
			if (input.Read(buffer, 0, buffer.Length) != buffer.Length) throw new InvalidOperationException("[READUTF16]The stream could not give enough bytes!");

			return System.Text.Encoding.Unicode.GetString(buffer);
		}
		[DebuggerStepThrough]
		protected string Read_UTF16_NT(Stream input)
		{
			using (MemoryStream bufferedStream = new MemoryStream())
			{
				int current = -1;
				while ((current = input.ReadByte()) != 0)
				{
					if (current == -1) throw new InvalidOperationException("[READUTF16_NT]The stream could not give enough bytes!");
					bufferedStream.WriteByte((byte)current);
					input.Seek(1, SeekOrigin.Current);
				}
				input.Seek(1, SeekOrigin.Current);

				return System.Text.Encoding.UTF8.GetString(bufferedStream.ToArray());
			}
		}
		#endregion
	}
}

