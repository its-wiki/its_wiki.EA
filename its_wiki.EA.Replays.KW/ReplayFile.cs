using its_wiki.EA.Replays.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Replays.KW
{
    public class KWReplayFile : ReplayFile
	{
		#region ReplayFile Fields
		public Version GameVersion { get; private set; }
		public byte GameType { get; private set; }
		public byte CommentaryFlag { get; private set; }

		public string FileName { get; set; }
		public string MatchTitle { get; set; }
		public string MatchDescription { get; set; }
		public string MapName { get; private set; }
		public string MapID { get; private set; }
		public string MapVersion { get; private set; }

		public byte TotalPlayers { get; private set; }
		public RAW_Player[] Raw_Player_Info { get; private set; }

		public RuleSet GameRules { get; private set; }
		public PlayerSlot[] PlayerSlots { get; private set; }


		public int ChunkOffset { get; private set; }
		public int MatchTimestamp { get; private set; }

		public byte ReplaySaver { get; set; }
		#endregion
		#region DummyReplayfile
		private static KWReplayFile _dummy = new KWReplayFile()
		{
			CommentaryFlag = 0x06,
			FileName = "DummyFile.KWReplay",
			GameRules = new RuleSet(10000, false),
			GameVersion = new Version(1, 2, 0, 0),
			MapName = "wikiMapName",
			MapID = "FakeMapID",
			MapVersion = "10",
			MatchTimestamp = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerSecond),
			GameType = 0x05,
			MatchTitle = "sup",
			MatchDescription = "No Match Description",
			ChunkOffset = 0,
			Raw_Player_Info = new RAW_Player[0],
			ReplaySaver = 0,
			PlayerSlots = new PlayerSlot[]{
				new PlayerSlot(0, true, "its_wiki", new byte[4], Enums.PlayerColor.Blue, Enums.PlayerFaction.Random, 0, 1),
				new PlayerSlot(1, true, "Artifite", new byte[4], Enums.PlayerColor.Orange, Enums.PlayerFaction.Random, 0, 2)
			}
		};
		public static KWReplayFile DummyReplay { get { return _dummy; } }
		#endregion

		public override bool VerifyStream(Stream stream)
		{
			try
			{
				string header = Read_UTF8(stream, 18);
				stream.Seek(0, SeekOrigin.Begin);
				return header == "C&C3 REPLAY HEADER";
			}
			catch { return false; }
		}
		public override void ParseStream(Stream stream)
		{
			if (!VerifyStream(stream)) throw new InvalidOperationException("The stream could not be verified!");
			stream.Seek(18, SeekOrigin.Begin);

			this.GameType = (byte)stream.ReadByte();

			int gameMajor = Read_INT32_LE(stream);
			int gameMinor = Read_INT32_LE(stream);
			int buildMajor = Read_INT32_LE(stream);
			int buildMinor = Read_INT32_LE(stream);
			this.GameVersion = new Version(gameMajor, gameMinor, buildMajor, buildMinor);

			this.CommentaryFlag = (byte)stream.ReadByte();
			stream.Seek(1, SeekOrigin.Current);

			this.MatchTitle = Read_UTF16_NT(stream);
			this.MatchDescription = Read_UTF16_NT(stream);
			this.MapName = Read_UTF16_NT(stream);
			this.MapID = Read_UTF16_NT(stream);

			this.TotalPlayers = (byte)stream.ReadByte();

			this.Raw_Player_Info = new RAW_Player[TotalPlayers];


			for (int i = 0; i < this.TotalPlayers; i++)
			{
				int pID = Read_INT32_LE(stream);
				string pName = Read_UTF16_NT(stream);
				byte pTeam = 255;
				if (GameType == 0x05)
				{
					pTeam = (byte)stream.ReadByte();
				}
				Raw_Player_Info[i] = new RAW_Player(i, pID, pName, pTeam);
			}

			//OLD CODE:
			//if (GameType == 0x05) stream.Seek(7, SeekOrigin.Current);
			//else stream.Seek(6, SeekOrigin.Current);

			//NEW CODE:
			while (stream.ReadByte() != 0x08) ;
			stream.Seek(-5, SeekOrigin.Current);

			//The reason for this new code was I found this strange behavior:
			//When the game host closes a slot and lets the second player join in the third slot, the post commentator gets added to the RAW_PLAYER structure,
			//I didn't found any way to detect if the PostCommentator was there, so I decided to search for the Length variable of "CNC3RPL\0", which is always 0x08

			this.ChunkOffset = (int)stream.Position + 16 + Read_INT32_LE(stream);
			int l = Read_INT32_LE(stream);

			if (l != 0x08) throw new InvalidOperationException("The size of CNC3RPL\\0 was not 8! (Maybe a corrupt replay?)");

			stream.Seek(8, SeekOrigin.Current);

			this.MatchTimestamp = Read_INT32_LE(stream);

			//Unknown data:
			stream.Seek(33, SeekOrigin.Current);

			int UTF8_HeaderLen = Read_INT32_LE(stream);
			string text_header = Read_UTF8(stream, UTF8_HeaderLen);

			//Parse the string headers:
			ParseHeader(text_header);

			this.ReplaySaver = (byte)stream.ReadByte();
			
			//8 zero bytes:
			stream.Seek(8, SeekOrigin.Current);

			int filename_len = Read_INT32_LE(stream);
			this.FileName = Read_UTF16(stream, filename_len);

			//16 random bytes (MatchTimeStamp2???)
			stream.Seek(16, SeekOrigin.Current);
		}

		#region PlayerSlot loading
		private void ParseHeader(string header)
		{
			try
			{
				//The string headers is a ';' seperated array:
				string[] parts = header.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

				//Get the Map id:
				string M = parts.FirstOrDefault(s => s.StartsWith("m=", StringComparison.OrdinalIgnoreCase));
				//Get the Map Candidate (? name is a guess)(used for PlusPatch detection): 
				string MC = parts.FirstOrDefault(s => s.StartsWith("mc=", StringComparison.OrdinalIgnoreCase));
				//Get the RuleSet:
				string RU = parts.FirstOrDefault(s => s.StartsWith("ru=", StringComparison.OrdinalIgnoreCase));
				//Get the player slots:
				string S = parts.FirstOrDefault(s => s.StartsWith("s=", StringComparison.OrdinalIgnoreCase));

				this.MapVersion = MC.Substring(MC.IndexOf("=") + 1);
				RU = RU.Substring(RU.IndexOf("=") + 1);
				S = S.Substring(S.IndexOf("=") + 1);

				string[] RU_parts = RU.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				//RU_Parts[2] == Start Money
				//RU_Parts[6] == 1 when crates are enabled, else 0
				this.GameRules = new RuleSet(Convert.ToInt32(RU_parts[2]), RU_parts[6] == "1");

				//The Player slots is also an array, except it is seperated by ':'
				string[] S_parts = S.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

				//Prepare 9 Player slots, this may sound wierd because you can only play with 8 players.
				//You need to remember the game ALWAYS adds a "fake" player called "post Commentator", so a 9th slot is required!
				PlayerSlot[] slots = new PlayerSlot[9];

				//Loop through all the player slots
				for (int pindex = 0; pindex <= S_parts.Length - 1; pindex++)
				{
					if (S_parts[pindex].ToLower() == "x")
					{
						//The player slot is emtpy:
						slots[pindex] = PlayerSlot.Empty;
						continue;
					}
					//Again this slot is a ',' seperated array:
					string[] PInfo = S_parts[pindex].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

					if (PInfo[0].ToLower().StartsWith("h"))
					{
						//The player slot starts with 'h', this means the player slot is containing a Human player:
						PInfo[0] = PInfo[0].Substring(1); //Get the name, without the 'h'
						if (PInfo[0].ToLower() == "post commentator")
						{
							//This player slot contains the fake player:
							slots[pindex] = PlayerSlot.PostCommentator(pindex); //We save the player index, because a ReplayPatch can use this info
							continue;
						}
						byte[] pip = Get_INT32_BE(Convert.ToInt32(PInfo[1], 16)); //Player IP: stored in a Hexadecial number, converted back using BigEndian gives you the player IP
						Enums.PlayerColor pc = (Enums.PlayerColor)Convert.ToInt32(PInfo[4]); //Player Color
						Enums.PlayerFaction pf = (Enums.PlayerFaction)Convert.ToInt32(PInfo[5]); //Player Faction
						int pl = Convert.ToInt32(PInfo[6]); //Player Location
						int pt = Convert.ToInt32(PInfo[7]); //Player Team

						//Fill the current slot with the current player
						slots[pindex] = new PlayerSlot(pindex, true, PInfo[0], pip, pc, pf, pl, pt);
					}
					else
					{
						//Computer:
						PInfo[0] = PInfo[0].Substring(1); //Player Name, represents the difficulty
						switch (PInfo[0].ToLower())
						{
							case "e":
								PInfo[0] = "Easy";
								break;
							case "m":
								PInfo[0] = "Medium";
								break;
							case "h":
								PInfo[0] = "Hard";
								break;
							case "b":
								PInfo[0] = "Brutal";
								break;
						}
						Enums.PlayerColor pc = (Enums.PlayerColor)Convert.ToInt32(PInfo[1]); //Player Color
						Enums.PlayerFaction pf = (Enums.PlayerFaction)Convert.ToInt32(PInfo[2]); //Player Faction
						int pl = Convert.ToInt32(PInfo[3]); //Player Location
						int pt = Convert.ToInt32(PInfo[4]); //Player Team

						slots[pindex] = new PlayerSlot(pindex, false, PInfo[0], new byte[4], pc, pf, pl, pt);
					}
				}
				this.PlayerSlots = slots;
			}
			catch (ArgumentNullException ane)
			{
				throw new InvalidOperationException("Failed to find M, MC, RU or S from the replay file. This may be caused by a corrupt replay file!", ane);
			}
		}
		#endregion
	}

	public class PlayerSlot
	{
		private static PlayerSlot _empty = null;
		public static PlayerSlot Empty
		{
			get
			{
				if (_empty == null) _empty = new PlayerSlot(-1, false, null, new byte[4], (Enums.PlayerColor)(-1), (Enums.PlayerFaction)(-1), -1, -1);
				return _empty;
			}
		}
		public static PlayerSlot PostCommentator(int PlayerSlot)
		{
			return new PlayerSlot(PlayerSlot, false, "Post Commentator", new byte[4], (Enums.PlayerColor)(-1), (Enums.PlayerFaction)(-1), -1, -1);
		}
		public bool IsPostCommentator { get { return PlayerName.ToLower() == "post commentator"; } }

		/// <summary>
		/// The index of the player, as in the game lobby
		/// </summary>
		public int PlayerIndex { get; private set; }
		/// <summary>
		/// If true, this player is a human player
		/// </summary>
		public bool IsHuman { get; private set; }
		/// <summary>
		/// The name of the player, if not human: the difficulty
		/// </summary>
		public string PlayerName { get; private set; }
		/// <summary>
		/// The player IP, only available in Network/Online replays
		/// </summary>
		public byte[] PlayerIP { get; private set; }
		/// <summary>
		/// The color of the player
		/// </summary>
		public Enums.PlayerColor PlayerColor { get; private set; }
		/// <summary>
		/// The faction the player chose
		/// </summary>
		public Enums.PlayerFaction PlayerFaction { get; private set; }
		/// <summary>
		/// The player position on the map
		/// </summary>
		public int PlayerPosition { get; private set; }
		/// <summary>
		/// The players' team
		/// </summary>
		public int PlayerTeam { get; private set; }


		/// <summary>
		/// Initializes a new instance of a PlayerSlot, for more info on the parameters, check the properties
		/// </summary>
		/// <param name="PlayerIndex"></param>
		/// <param name="IsHuman"></param>
		/// <param name="PlayerName"></param>
		/// <param name="PlayerIP"></param>
		/// <param name="PlayerColor"></param>
		/// <param name="PlayerFaction"></param>
		/// <param name="PlayerPosition"></param>
		/// <param name="PlayerTeam"></param>
		public PlayerSlot(int PlayerIndex, bool IsHuman, string PlayerName, byte[] PlayerIP, Enums.PlayerColor PlayerColor, Enums.PlayerFaction PlayerFaction, int PlayerPosition, int PlayerTeam)
		{
			this.PlayerIndex = PlayerIndex;
			this.IsHuman = IsHuman;
			this.PlayerName = PlayerName;
			this.PlayerIP = PlayerIP;
			this.PlayerColor = PlayerColor;
			this.PlayerFaction = PlayerFaction;
			this.PlayerPosition = PlayerPosition;
			this.PlayerTeam = PlayerTeam;
		}

		/// <summary>
		/// Returns the string version of this PlayerSlot, which is "Empty" for empty slots, and "*PlayerName*" for an occupied slot
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (this == Empty) return "Empty";
			return PlayerName;
		}
	}
	/// <summary>
	/// The rule set available for KW games
	/// </summary>
	public class RuleSet
	{
		/// <summary>
		/// The amount of money you'll start the game with
		/// </summary>
		public int StartingMoney { get; private set; }
		/// <summary>
		/// If true, crates will spawn randomly
		/// </summary>
		public bool CratesEnabled { get; private set; }
		
		/// <summary>
		/// Initializes a new instance of RuleSet
		/// </summary>
		/// <param name="StartingMoney"></param>
		/// <param name="CreatesEnabled"></param>
		public RuleSet(int StartingMoney, bool CreatesEnabled)
		{
			this.StartingMoney = StartingMoney;
			this.CratesEnabled = CratesEnabled;
		}
	}

}

namespace its_wiki.EA.Replays.KW.Enums
{
	/// <summary>
	/// Defines all possible colors ingame
	/// </summary>
	public enum PlayerColor : int
	{
		Random = -1,
		Blue = 0,
		Yellow = 1,
		Green = 2,
		Orange = 3,
		Pink = 4,
		Purple = 5,
		Red = 6,
		Cyan = 7
	}
	/// <summary>
	/// Defines all possible factions ingame
	/// </summary>
	public enum PlayerFaction
	{
		Random = 1,
		Observer = 2,
		Commentator = 3,
		GDI = 6,
		Steel_Talons = 7,
		ZOCOM = 8,
		Nod = 9,
		Black_Hand = 10,
		Marked_of_Kane = 11,
		Scrin = 12,
		Reaper_17 = 13,
		Traveler_59 = 14
	}
	/// <summary>
	/// Defines all version "magics", deprecated: use the GameVersion_Map or game version
	/// </summary>
	public enum GameVersion_Magic : uint
	{
		V1_2_0_0 = 0x2A53C634,
		V1_2_P_1 = 0x517C6B4B,
		V1_2_P_3 = 0x7B600E19,
		V1_2_P_4 = 0xF9121013,
		V1_2_P_5 = 0x07DF6A13,
		V1_2_P_6 = 0xADC6426B
	}
	/// <summary>
	/// Defines all possible map pack version (1.02+)
	/// </summary>
	public enum GameVersion_Map : uint
	{
		MPP_R02 = 0x04,
		MPP_R03 = 0x06,
		MPP_R04 = 0x07,
		MPP_R05 = 0x09,
		MPP_R06 = 0x0B,
		/// <summary>
		/// Thanks to CGF for noting that R7 2v2 maps have an invalid value!
		/// </summary>
		MPP_R07_INVALID = 0x00,

		MPP_R07 = 0x0D,
		MPP_R08 = 0x0F,
		MPP_R09 = 0x11,
		MPP_R10 = 0x15,
		MPP_R11 = 0x16,
	}
	/// <summary>
	/// Enumeration Extensions
	/// </summary>
	public static class EnumExtends
	{
		/// <summary>
		/// Extends the PlayerFaction.ToString(), this will return the friendly faction name
		/// </summary>
		/// <param name="faction"></param>
		/// <returns></returns>
		public static string ToString(this PlayerFaction faction)
		{
			return faction.ToString().Replace("_", " ");
		}
		/// <summary>
		/// Create a PlayerColor.ToColor() function, this will return the appropriate System.Drawing.Color
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public static Color ToColor(this PlayerColor color)
		{
			switch (color)
			{
				case PlayerColor.Blue:
					return Color.Blue;
				case PlayerColor.Yellow:
					return Color.Yellow;
				case PlayerColor.Green:
					return Color.Green;
				case PlayerColor.Orange:
					return Color.Orange;
				case PlayerColor.Pink:
					return Color.HotPink;
				case PlayerColor.Purple:
					return Color.Purple;
				case PlayerColor.Red:
					return Color.Red;
				case PlayerColor.Cyan:
					return Color.Cyan;
				default:
					return Color.Transparent;
			}
		}
	}
}