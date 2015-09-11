using its_wiki.EA.Binary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA
{
	// A class that provides refpack compression/decompression
	public class RefPack : StreamConsumer
	{
		private static readonly RefPack instance = new RefPack();
		public static RefPackInfo Decompress(Stream CompressedStream, Stream OutputStream, uint CompressedSize)
		{
			// Lets read the first 2 bytes into memory, these should contain the header byte and static flag 0xFB
			byte[] signature = new byte[2];
			CompressedStream.Read(signature, 0, 2);


			// HeaderFlag is a bitwise flag enum, this means each enum type represents a bit in a byte
			// See the HeaderFlag enum for more information
			HeaderFlag header = (HeaderFlag)signature[0];
			if (signature[1] != 0xFB) return new RefPackInfo(false, 0, 0);

			Func<Stream, int> SizeReader = null;
			if (header.HasFlag(HeaderFlag.SizeMode32Bits))
			{
				// The header defines that the size mode is 32 bit, use a int32_be reader:
				SizeReader = instance.Read_INT32_BE;
			}
			else
			{
				// The header defines that the size mode is 24 bits, use a int24_be reader:
				SizeReader = Read_INT24_BE;
			}

			uint compressed_size, decompressed_size = 0;

			// Check if the compressed size is stored, if so read this value from the stream
			if (header.HasFlag(HeaderFlag.CompressedSizePresent)) compressed_size = (uint)SizeReader(CompressedStream);
			// The compressed size is NOT stored in the stream, use the one we where givin
			else compressed_size = CompressedSize;

			decompressed_size = (uint)SizeReader(CompressedStream);
			bool decompressed = false;
			OutputStream.SetLength(decompressed_size);

			// Here the actual decoding will take place:
			#region refpack_decoding

			byte byte0, byte1, byte2, byte3 = 0;
			uint proceeding_length, reference_length, reference_distance = 0;

			//Todo: while (! overflowed(CompressedStream, OutputStream))
			while (true)
			{
				byte0 = (byte)CompressedStream.ReadByte();
				if ((byte0 & 0x80) == 0)
				{
					// 2 byte opcode
					byte1 = (byte)CompressedStream.ReadByte();

					proceeding_length = (byte)(byte0 & 0x03);
					for (uint i = 0; i < proceeding_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());

					reference_distance = (uint)(((byte0 & 0x60) << 3) - byte1 - 1);
					reference_length = (uint)((byte0 >> 2) & 0x07) + 3;

					//for (uint i = 0; i < reference_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());
					self_copy(OutputStream, reference_distance, reference_length);
				}
				else if ((byte0 & 0x40) == 0)
				{
					// 3 byte opcode
					byte1 = (byte)CompressedStream.ReadByte();
					byte2 = (byte)CompressedStream.ReadByte();

					proceeding_length = (uint)(byte1 >> 6);
					for (uint i = 0; i < proceeding_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());

					reference_distance = (uint)(((byte1 & 0x3F) << 8) - byte2 - 1);
					reference_length = (uint)((byte0 & 0x3F) + 4);

					//for (uint i = 0; i < reference_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());
					self_copy(OutputStream, reference_distance, reference_length);
				}
				else if ((byte0 & 0x20) == 0)
				{
					// 4 byte opcode
					byte1 = (byte)CompressedStream.ReadByte();
					byte2 = (byte)CompressedStream.ReadByte();
					byte3 = (byte)CompressedStream.ReadByte();

					proceeding_length = (uint)(byte0 & 0x03);
					for (uint i = 0; i < proceeding_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());

					reference_distance = (uint)(((byte0 & 0x10) << 12) - (byte1 << 8)  - byte2 - 1);
					reference_length = (uint)((byte0 & 0x0C) << 6) + byte3 + 5;

					//for (uint i = 0; i < reference_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());
					self_copy(OutputStream, reference_distance, reference_length);
				}
				else
				{
					// 1 byte opcode
					proceeding_length = (uint)(((byte0 & 0x1F) * 4) + 4);

					if (proceeding_length <= 0x70)
					{
						// NO STOP FLAG:
						for (uint i = 0; i < proceeding_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());
					}
					else
					{
						// STOP FLAG:
						proceeding_length = (uint)(byte0 & 0x03);
						for (uint i = 0; i < proceeding_length; i++) OutputStream.WriteByte((byte)CompressedStream.ReadByte());

						decompressed = true;
						break;
					}
				}
			}
			#endregion

			return new RefPackInfo(decompressed, compressed_size, decompressed_size);
		}


		[DebuggerStepThrough]
		private static int Read_INT24_BE(Stream input)
		{
			byte[] buffer = new byte[3];
			if (input.Read(buffer, 0, 3) != 3) throw new InvalidOperationException("[READINT24_BE]The stream could not give more than 3 bytes!");

			return ((buffer[0] << 16) | (buffer[1] << 8) | buffer[2]);
		}

		public static void self_copy(Stream stream, uint distance, uint length)
		{
			uint origin = (uint)(stream.Position - distance);
			uint current = (uint)stream.Position;
			for (uint i = 0; i < length; i++)
			{
				stream.Seek(origin + i, SeekOrigin.Begin);
				int bt = stream.ReadByte();
				stream.Seek(current + i, SeekOrigin.Begin);
				stream.WriteByte((byte)bt);
			}
		}
	}

	public class RefPackInfo
	{
		/// <summary>
		///		Indicates that the compression/decompression was succesfull
		/// </summary>
		public bool Succesfull { get; private set; }
		/// <summary>
		///		The size of the payload when decompressed
		/// </summary>
		public uint DecompressedSize { get; private set; }
		/// <summary>
		///		The size of the payload when compressed
		/// </summary>
		public uint CompressedSize { get; private set; }

		/// <summary>
		///		Initializes a new instance of RefPackInfo
		/// </summary>
		/// <param name="Succesfull"></param>
		/// <param name="CompressedSize"></param>
		/// <param name="DecompressedSize"></param>
		public RefPackInfo(bool Succesfull, uint CompressedSize, uint DecompressedSize)
		{
			this.Succesfull = Succesfull;
			this.CompressedSize = CompressedSize;
			this.DecompressedSize = DecompressedSize;
		}
	}

	[Flags]
	public enum HeaderFlag : byte
	{

		/// <summary>
		///		This will be the most significant bit:
		///		00 00 00 00 <--
		///		When set (00 00 00 01) the stream contains the compressed size
		/// </summary>
		CompressedSizePresent = 1,


		/// <summary>
		///		This will be the least significant bit:
		///		--> 00 00 00 00
		///		When set (10 00 00 00) the size mode is 32 bits
		///		When UNSET the size mode is 24 bits
		/// </summary>
		SizeMode32Bits = 128,
	}
}
