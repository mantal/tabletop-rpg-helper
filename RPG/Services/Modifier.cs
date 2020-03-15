using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPG.Services
{
	public class Modifier
	{
		public readonly StatId StatId;
		public readonly ModifierType Type;
		public readonly double Multiplier;
		[JsonConverter(typeof(StringEnumConverter))]
		public readonly IntConversionMethod IntConversionMethod = IntConversionMethod.NoConversion;
		
		[JsonConstructor]
		public Modifier(StatId statId,
						ModifierType type,
						double multiplier = 1,
						IntConversionMethod conversionMethod = IntConversionMethod.NoConversion)
		{
			StatId = statId;
			Type = type;
			IntConversionMethod = conversionMethod;
			Multiplier = multiplier;
		}

		public override string ToString() => $"{Type} {StatId}{(Multiplier == 1 ? "" : $" * {Multiplier}" )}";
	}

	public class StaticModifier
	{
		public readonly ModifierType Type;
		public readonly double Modifier;
		[JsonConverter(typeof(StringEnumConverter))]
		public readonly IntConversionMethod IntConversionMethod = IntConversionMethod.NoConversion;

		[JsonConstructor]
		public StaticModifier(ModifierType type, double modifier)
		{
			Type = type;
			Modifier = modifier;
		}

		public override string ToString() => $"{Type} {Modifier}";
	}
}