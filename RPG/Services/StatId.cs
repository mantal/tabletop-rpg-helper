using System;
using System.Linq;
using Newtonsoft.Json;

namespace RPG.Services
{
	[JsonConverter(typeof(StatIdTypeConvert))]
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

	[JsonConverter(typeof(VariableIdTypeConvert))]
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

		private class VariableIdTypeConvert : JsonConverter<VariableId>
		{
			public override void WriteJson(JsonWriter writer, VariableId value, JsonSerializer serializer)
			{
				writer.WriteValue(value);
			}

			public override VariableId ReadJson(JsonReader reader,
												Type objectType,
												VariableId existingValue,
												bool hasExistingValue,
												JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
					throw new JsonSerializationException($"Cannot convert {typeof(VariableId)} to null");

				if (reader.TokenType != JsonToken.String)
					throw new JsonSerializationException($"Cannot convert {reader.Value} to {typeof(VariableId)}");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
				return new VariableId((string)reader.Value);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
			}
		}
	}

	public static class StringExtensions
	{
		public static bool IsValidStatId(this string? s) 
			=> !string.IsNullOrEmpty(s) && s.All(char.IsLetter);

		public static bool IsValidVariableId(this string? s)
			=> !string.IsNullOrEmpty(s)
			   && s[^1] != ':'
			   && s.All(c => char.IsLetter(c) || c == ':')
			   && s.Count(c => c == ':') <= 1;
	}
}