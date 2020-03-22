using System;
using System.Linq;
using Newtonsoft.Json;

namespace RPG.Services
{
	// TODO => StatRef = StatId + InnerStatId
	[JsonConverter(typeof(StatIdTypeConvert))]
	public class StatId
	{
		public readonly string Id;
		public readonly string? InnerId;
		
		public StatId(string id)
		{
			if (!id.IsValidStatId()) 
				throw new ArgumentException($"Invalid name: {id}", nameof(id));

			if (!id.Contains(':', StringComparison.InvariantCultureIgnoreCase))
				Id = id;
			else
			{
				var s = id.Split(':');
				Id = s[0];
				InnerId = s[1];
			}
		}

		public static implicit operator StatId(string id) => new StatId(id.ToUpperInvariant());
		public static implicit operator string(StatId id) => $"{id.Id}{(id.InnerId == null ? "" : $":{id.InnerId}")}";

		public static bool operator ==(StatId? a, StatId? b)
		{
			if (a is null || b is null)
				return a is null && b is null;
			if (a.InnerId == null || b.InnerId == null)
				return a.InnerId == null && b.InnerId == null;
			return string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase) == 0
				&& string.Compare(a.InnerId, b.InnerId, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		public static bool operator !=(StatId a, StatId b) => !(a == b);

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(this, obj)) return true;
			if (obj is null) return false;
			if (obj.GetType() != typeof(StatId)) return false;
			return ((StatId) obj).Id == Id
				   && ((StatId) obj).InnerId == InnerId;
		}

		public override int GetHashCode() => (_id: Id, _innerId: InnerId).GetHashCode();
		
		public override string ToString() => this;

		//TODO fix serialization w/ innerId
		private class StatIdTypeConvert : JsonConverter<StatId>
		{
			public override void WriteJson(JsonWriter writer, StatId value, JsonSerializer serializer)
			{
				writer.WriteValue(value);
			}

			public override StatId ReadJson(JsonReader reader,
											Type objectType,
											StatId existingValue,
											bool hasExistingValue,
											JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
					throw new JsonSerializationException($"Cannot convert {typeof(StatId)} to null");

				if (reader.TokenType != JsonToken.String)
					throw new JsonSerializationException($"Cannot convert {reader.Value} to {typeof(StatId)}");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
				return new StatId((string)reader.Value);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
			}
		}
	}

	public static class StringExtensions
	{
		public static bool IsValidStatId(this string? s)
		{
			if (string.IsNullOrEmpty(s))
				return false;

			return s[^1] != ':'
				   && s.All(c => char.IsLetter(c) || c == ':')
				   && s.Count(c => c == ':') <= 1;
		}

		/// <summary>
		/// Expand statId, ex:":innerId" to "STAT:innerId"
		/// </summary>
		public static string ExpandStatId(this string s, StatId parentId) => s[0] != ':' ? s : parentId + s;
	}
}