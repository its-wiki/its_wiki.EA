using its_wiki.EA.Binary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Big
{
	public class BigFile : StreamConsumer, IDisposable
	{
		public BigFileFlag FileFlag { get; private set; }
		public BigFilePart[] FileParts { get; private set; }
		public long TotalLength { get; private set; }
		public Stream BigFileStream { get; private set; }

		public BigFile(Stream BigFileStream)
		{
			this.BigFileStream = BigFileStream;
		}



		public void Dispose()
		{
			BigFileStream.Dispose();
			BigFileStream = null;
			Array.Clear(FileParts, 0, FileParts.Length);
			FileParts = null;
		}

		public void LoadArchive()
		{
			lock (BigFileStream)
			{
				BigFileStream.Seek(0, SeekOrigin.Begin);
				this.FileFlag = Read_UTF8(BigFileStream, 4).FromString();
				this.TotalLength = BigFileStream.Length;

				uint file_len = (uint)Read_INT32_LE(BigFileStream);
				uint number_of_files = (uint)Read_INT32_BE(BigFileStream);

				this.FileParts = new BigFilePart[number_of_files];

				uint Header_len = (uint)Read_INT32_BE(BigFileStream) - 25;
				for (int i = 0; i < number_of_files; i++)
				{
					this.FileParts[i] = new BigFilePart((uint)Read_INT32_BE(BigFileStream), (uint)Read_INT32_BE(BigFileStream), Read_UTF8_NT(BigFileStream), this);
				}
			}
		}

		public void ReadStream(byte[] buffer, int bufferOffset, uint Length, uint Offset = 0)
		{
			lock (BigFileStream)
			{
				if (Offset != 0) BigFileStream.Seek(Offset, SeekOrigin.Begin);
				BigFileStream.Read(buffer, bufferOffset, (int)Length);
			}
		}

		public BigFilePart GetPart(string Uri, bool IgnoreCase = true)
		{
			lock (BigFileStream)
			{
				string uri = IgnoreCase ? Uri.ToLower() : Uri;
				return FileParts.FirstOrDefault(fp => (IgnoreCase ? fp.FileName.ToLower() : fp.FileName) == uri);
			}
		}
		public IEnumerable<BigFilePart> PartStartsWith(string Uri, bool IgnoreCase = true)
		{
			lock (BigFileStream)
			{
				string uri = IgnoreCase ? Uri.ToLower() : Uri;
				return FileParts.Where(bfp => (IgnoreCase ? bfp.FileName.ToLower() : bfp.FileName).StartsWith(uri));
			}
		}
	}
}
