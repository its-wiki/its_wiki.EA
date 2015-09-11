using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace its_wiki.EA.Big
{
	public enum BigFileFlag : byte
	{
		UNKNOWN = 0x00,


		BIG4 = 0x01,
		BIGF = 0x02,
	}
	public static class BigFileFlagExtensions
	{
		public static BigFileFlag FromString(this string input)
		{
			switch (input.ToLower())
			{
				case "big4":
					return BigFileFlag.BIG4;
				case "bigf":
					return BigFileFlag.BIGF;
				default:
					return BigFileFlag.UNKNOWN;
			}
		}
	}
}
