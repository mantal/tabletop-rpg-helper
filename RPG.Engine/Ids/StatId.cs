using System;
using System.Linq;

namespace RPG.Engine.Ids
{
	public class StatId
	{
		public readonly string Id;
		
		public StatId(string id)
		{
			if (!id.IsValidStatId()) 
				throw new ArgumentException($"Invalid name: {id}", nameof(id));

			Id = id;
		}

		public static explicit operator StatId(string id) => new StatId(id.ToUpperInvariant());
		public static explicit operator string(StatId id) => id.Id;

		public static bool operator ==(StatId? a, StatId? b)
		{
			if (a is null || b is null)
				return a is null && b is null;
			return string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		public static bool operator !=(StatId? a, StatId? b) => !(a == b);

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(this, obj)) return true;
			if (obj is null) return false;
			if (obj.GetType() != typeof(StatId)) return false;
			return ((StatId) obj).Id == Id;
		}

		public override int GetHashCode() => Id.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
		
		public override string ToString() => (string) this;
	}

	public static class StatIdStringExtensions
	{
		public static bool IsValidStatId(this string? s) 
			=> !string.IsNullOrEmpty(s)
			   && s.All(c => char.IsLetterOrDigit(c)
							 || c == '_'
							 || c == '-');
	}
}