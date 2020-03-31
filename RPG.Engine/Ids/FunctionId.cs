using System;
using System.Linq;

namespace RPG.Engine.Ids
{
	public class FunctionId
	{
		public string Id { get; }

		public FunctionId(string id)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
		}

		public static explicit operator FunctionId(string id) => new FunctionId(id);

		public static bool operator ==(FunctionId? a, FunctionId? b)
		{
			if (a is null || b is null)
				return a is null && b is null;
			return string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		public static bool operator !=(FunctionId? a, FunctionId? b) => !(a == b);

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(this, obj)) return true;
			if (obj is null) return false;
			if (obj.GetType() != typeof(FunctionId)) return false;
			return ((FunctionId)obj).Id == Id;
		}

		public override int GetHashCode() => Id.GetHashCode(StringComparison.InvariantCultureIgnoreCase);

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