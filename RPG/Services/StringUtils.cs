using System.Collections.Generic;
using System.Linq;

namespace RPG.Services
{
	public static class StringUtils
	{
		public static string AsString(this IEnumerable<char> chars) => chars.Aggregate("", (s, c) => s + c);
		public static string AsString(this IEnumerable<string> chars) => chars.Aggregate("", (s, s1) => s + s1);
	}
}