using its_wiki.EA.Big;
using its_wiki.EA.Replays.KW;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.TesterApplication
{
	class Program
	{
		private const string output_dir = @"D:\tmp2\bigstuff\map_mp_4_bass\";
		static void Main(string[] args)
		{

			using (FileStream fs = new FileStream(@"C:\Users\wiki\Desktop\Replays\R11\2015.09.07\0003.[1.02+] Twisted Arena.HateMeLikeAPro.Lucwasbeer.KWReplay", FileMode.Open, FileAccess.Read))
			{
				KWReplayFile replay = new KWReplayFile();
				replay.ParseStream(fs);
				foreach (var player in replay.PlayerSlots)
				{

				}
			}

			using (FileStream fs = new FileStream(@"D:\tmp2\bigstuff\MPMaps.big", FileMode.Open, FileAccess.Read))
			{
				BigFile bf = new BigFile(fs);
				bf.LoadArchive();

				BigFilePart part = bf.GetPart(@"data\mapmetadata.bin");

				//if (!Directory.Exists(output_dir)) Directory.CreateDirectory(output_dir);
				//foreach (BigFilePart part in bf.PartStartsWith(@"data\maps\official\map_mp_4_bass"))
				//{
				//	using (FileStream sfs = new FileStream(Path.Combine(output_dir, part.FileName.Substring(part.FileName.LastIndexOf('\\') + 1)), FileMode.Create, FileAccess.ReadWrite))
				//	{
				//		using (Stream content = part.OpenStream()) content.CopyTo(sfs);
				//	}
				//}

				using (FileStream sf = new FileStream(@"D:\tmp2\bigstuff\map_mp_4_bass\test.tga", FileMode.Create, FileAccess.Write))
				{
					using (Stream content = part.OpenStream())
					{
						content.CopyTo(sf);
					}
					//using (StreamWriter sr = new StreamWriter(sf))
					//{
					//	foreach (BigFilePart bfp in bf.FileParts)
					//	{
					//		sr.WriteLine("[{0}]: {1}", bfp.Offset, bfp.FileName);
					//	}
					//}

				}
			}
			Console.ReadLine();
		}
	}
}
