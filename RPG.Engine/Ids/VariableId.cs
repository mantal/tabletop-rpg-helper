using System;
using System.Linq;

namespace RPG.Engine.Ids
{
	public record VariableId
	{
		public StatId StatId { get; }
		public string Id { get; }

		public VariableId(string id, StatId? statId = null)
		{
			if (!id.IsValidVariableId())
				throw new ArgumentException($"Invalid variable id: {id}", nameof(id));
			if (id[0] == '.')
			{
				if (statId == null)
					throw new ArgumentException($"Shorthand variable id requires explicit stat id", nameof(id));
				StatId = statId;
				Id = id[1..];
			}
			else
			{
				var s = id.Split('.');
				StatId = new StatId(s[0]);
				Id = s[1];
			}
		}

		public static explicit operator VariableId(string id) => new VariableId(id);
		
		public override string ToString() => $"{StatId}.{Id}";
	}

	public static class VariableIdStringExtensions
	{
		public static bool IsValidVariableId(this string? s)
			=> !string.IsNullOrEmpty(s)
			   && s.Length > 1
			   && s[^1] != '.'
			   && s.Count(c => c == '.') == 1
			   && char.IsLetter(s[s.IndexOf('.') + 1])
			   && s.All(c => char.IsLetterOrDigit(c)
							 || c == '.'
							 || c == '_'
							 || c == '-');
	}
}