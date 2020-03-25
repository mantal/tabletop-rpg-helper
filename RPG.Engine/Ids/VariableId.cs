using System;
using System.Linq;

namespace RPG.Engine.Ids
{
	public class VariableId
	{
		public readonly StatId StatId;
		public readonly string Id;

		public VariableId(string id, StatId? statId = null)
		{
			if (!id.IsValidVariableId())
				throw new ArgumentException($"Invalid variable id: {id}", nameof(id));
			if (id[0] == ':')
			{
				if (statId == null)
					throw new ArgumentException($"Shorthand variable id requires explicit stat id", nameof(id));
				StatId = statId;
				Id = id.Substring(1);
			}
			else
			{
				var s = id.Split(':');
				StatId = new StatId(s[0]);
				Id = s[1];
			}
		}

		public static explicit operator VariableId(string id) => new VariableId(id);
		public static explicit operator string(VariableId id) => $"{id.StatId}:{id.Id}";

		public static bool operator ==(VariableId? a, VariableId? b)
		{
			if (a is null || b is null)
				return a is null && b is null;
			return string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase) == 0
				   && a.StatId == b.StatId;
		}

		public static bool operator !=(VariableId? a, VariableId? b) => !(a == b);

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(this, obj)) return true;
			if (obj is null) return false;
			if (obj.GetType() != typeof(VariableId)) return false;
			return ((VariableId)obj).StatId == StatId
				   && ((VariableId)obj).Id == Id;
		}

		public override int GetHashCode() => (Id, StatId).GetHashCode();

		public override string ToString() => (string) this;
	}

	public static class VariableIdStringExtensions
	{
		public static bool IsValidVariableId(this string? s)
			=> !string.IsNullOrEmpty(s)
			   && s[^1] != ':'
			   && s.All(c => char.IsLetterOrDigit(c) 
							 || c == ':'
							 || c == '_'
							 || c == '-')
			   && s.Count(c => c == ':') == 1;
	}
}