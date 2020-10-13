using System;
using System.Linq;

namespace RPG.Engine.Ids
{
	public record FunctionId
	{
		public string Id { get; }

		public FunctionId(string id)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
		}

		public static explicit operator FunctionId(string id) => new FunctionId(id);

		public override string ToString() => Id;
	}

	public static class FunctionStringExtensions
	{
		public static bool IsValidFunctionId(this string s)
			=> !string.IsNullOrWhiteSpace(s)
			   && s.Length > 1
			   && s.StartsWith('$')
			   && s.Skip(1).All(c => char.IsLetterOrDigit(c)
									 || c == '_'
									 || c == '-');

	}
}