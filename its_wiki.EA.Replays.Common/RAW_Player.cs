using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Replays.Common
{
	public class RAW_Player
	{
		public int PlayerIndex { get; private set; }
		public int PlayerID { get; private set; }
		public string PlayerName { get; private set; }
		public byte PlayerTeam { get; private set; }

		public RAW_Player(int PlayerIndex, int PlayerID, string PlayerName, byte PlayerTeam = 255)
		{
			this.PlayerIndex = PlayerIndex;
			this.PlayerID = PlayerID;
			this.PlayerName = PlayerName;
			this.PlayerTeam = PlayerTeam;
		}
	}
}
