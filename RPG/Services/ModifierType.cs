using System;
using Newtonsoft.Json;

namespace RPG.Services
{
	[JsonConverter(typeof(ModifierTypeTypeConvert))]
	public class ModifierType
	{
		private readonly string _symbol;
		public static readonly ModifierType Add = new ModifierType("+");
		public static readonly ModifierType Sub = new ModifierType("-");
		public static readonly ModifierType Mult = new ModifierType("*");
		public static readonly ModifierType Div = new ModifierType("/");

		private ModifierType() => throw new NotSupportedException();

		private ModifierType(string symbol)
		{
			_symbol = symbol;
		}

		public static implicit operator string(ModifierType type) => type._symbol;

		public double Apply(double a, double b)
			=> _symbol switch
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

		public override string ToString() => _symbol;

		private class ModifierTypeTypeConvert : JsonConverter<ModifierType>
		{
			public override void WriteJson(JsonWriter writer, ModifierType value, JsonSerializer serializer)
			{
				writer.WriteValue(value._symbol);
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
				return ModifierType.FromString((string) reader.Value);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
			}
		}
	}
}