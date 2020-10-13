using System;
using System.Linq;

namespace RPG.Engine.Ids
{
	public record StatId
	{
		public string Id { get; }
		
		public StatId(string id)
		{
			if (!id.IsValidStatId()) 
				throw new ArgumentException($"Invalid name: {id}", nameof(id));

			Id = id;
		}

		public static explicit operator StatId(string id) => new StatId(id);

		public override string ToString() => Id;
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