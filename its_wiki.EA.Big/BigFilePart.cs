using its_wiki.EA.Binary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Big
{
	public class BigFilePart : StreamConsumer
	{
		public const ushort FileTypeMask = 0x3EFF;
		public const ushort FileTypeIndicator = 0x10FB;
		public const ushort CompressionMarker = 0x8000;
		public const ushort FileSkip3Mask = 0x0100;

		public uint Offset { get; private set; }
		public uint Length { get; private set; }
		public string FileName { get; private set; }
		public BigFile Container { get; private set; }

		public bool? IsCompressed { get; private set; }
		public uint? DecompressedSize { get; private set; }

		public BigFilePart(uint Offset, uint Length, string FileName, BigFile Container)
		{
			this.Offset = Offset;
			this.Length = Length;
			this.FileName = FileName;
			this.Container = Container;
		}

		public MemoryStream OpenStream()
		{
			byte[] buffer = new byte[Length];
			Container.ReadStream(buffer, 0, Length, Offset);


			MemoryStream ms = new MemoryStream(buffer);
			ushort flag = (ushort)Read_INT16_BE(ms);

			//Check if the file is compressed:
			if ((flag & FileTypeMask) == FileTypeIndicator && (flag & CompressionMarker) == 0)
			{
				if ((flag & FileSkip3Mask) > 0) ms.Seek(3, SeekOrigin.Current);
				this.IsCompressed = true;

				byte[] raw_decompressed_len = new byte[3];
				ms.Read(raw_decompressed_len, 0, 3);

				this.DecompressedSize = (uint)((raw_decompressed_len[0] << 16) | (raw_decompressed_len[1] << 8) | raw_decompressed_len[2]);
			}


			return new MemoryStream(buffer);
		}
	}
}
