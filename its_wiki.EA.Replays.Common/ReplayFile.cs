using its_wiki.EA.Binary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Replays.Common
{
	public abstract class ReplayFile : StreamConsumer
	{
		public abstract void ParseStream(Stream stream);
		public abstract bool VerifyStream(Stream stream);
	}
}
