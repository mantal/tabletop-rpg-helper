using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;

namespace RPG.Engine.Utils
{
	public static class StringUtils
	{
		public static string Join(this IEnumerable<char> chars) => chars.Aggregate("", (s, c) => s + c);
		public static string Join(this IEnumerable<string> strings, string separator = "") => string.Join(separator, strings);
		public static string Join(this IEnumerable<StatId> chars) => chars.Aggregate("", (s, s1) => s + s1);
		public static string Join(this IEnumerable<VariableId> chars) => chars.Aggregate("", (s, s1) => s + s1);
	}
}