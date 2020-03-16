using System;
using Newtonsoft.Json;

namespace RPG.Services
{
	[JsonConverter(typeof(ModifierTypeTypeConvert))]
	public class ModifierType
	{
		public readonly string Symbol;
		/// <summary>
		/// Lower value means higher priority
		/// </summary>
		public readonly int Priority;

		public static readonly ModifierType Add = new ModifierType("+", 2);
		public static readonly ModifierType Sub = new ModifierType("-", 2);
		public static readonly ModifierType Mult = new ModifierType("*", 1);
		public static readonly ModifierType Div = new ModifierType("/", 1);

		private ModifierType() => throw new NotSupportedException();

		private ModifierType(string symbol, int priority)
		{
			Symbol = symbol;
			Priority = priority;
		}

		public static implicit operator string(ModifierType type) => type.Symbol;

		public double Apply(double a, double b)
			=> Symbol switch
			   {
				   "+" => a + b,
				   "-" => a - b,
				   "*" => a * b,
				   "/" => a / b,
				   _   => throw new Exception(),
			   };

		public static ModifierType? FromString(string s) 
			=> s switch
			   {
				   "+" => ModifierType.Add,
				   "-" => ModifierType.Sub,
				   "*" => ModifierType.Mult,
				   "/" => ModifierType.Div,
				   _   => null,
			   };

		public override string ToString() => Symbol;

		private class ModifierTypeTypeConvert : JsonConverter<ModifierType>
		{
			public override void WriteJson(JsonWriter writer, ModifierType value, JsonSerializer serializer)
			{
				writer.WriteValue(value.Symbol);
			}

			public override ModifierType ReadJson(JsonReader reader,
												  Type objectType,
												  ModifierType existingValue,
												  bool hasExistingValue,
												  JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
					throw new JsonSerializationException($"Cannot convert {typeof(ModifierType)} to null");
				if (reader.TokenType != JsonToken.String)
					throw new JsonSerializationException($"Cannot convert {reader.Value} to {typeof(ModifierType)}");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
				var modifier = ModifierType.FromString((string) reader.Value);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

				if (modifier == null)
					throw new JsonSerializationException($"Invalid modifier: {reader.Value}");

				return modifier;
			}
		}
	}
}