﻿using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RPG.Services
{
	[JsonConverter(typeof(StatIdTypeConvert))]
	[DebuggerDisplay("{_id}")]
	public class StatId
	{
		private readonly string _id;
		
		public StatId(string id)
		{
			if (!id.IsValidStatId()) 
				throw new ArgumentException($"Invalid name: {id}", nameof(id));

			_id = id;
		}

		public static implicit operator string(StatId id) => id._id;
		public static implicit operator StatId(string id) => new StatId(id.ToUpperInvariant());

		public static bool operator ==(StatId a, StatId b) 
			=> string.Compare(a._id, b._id, StringComparison.InvariantCultureIgnoreCase) == 0;

		public static bool operator !=(StatId a, StatId b) => !(a == b);

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(this, obj)) return true;
			if (obj is null) return false;
			if (obj.GetType() != typeof(StatId)) return false;
			return ((StatId)obj)._id == _id;
		}

		public override int GetHashCode() => _id.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
		
		public override string ToString() => this;

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
			return !string.IsNullOrEmpty(s) 
				   && (char.IsLetter(s[0]) 
					   || s[0] == '{' && s[^1] == '}');
		}
	}
}