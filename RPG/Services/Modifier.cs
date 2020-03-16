using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPG.Services
{
	public abstract class Modifier
	{
		public readonly ModifierType Type;
		[JsonConverter(typeof(StringEnumConverter))]
		public readonly RoundingMethod RoundingMethod;
		
		[JsonConstructor]
		protected Modifier(ModifierType type,
						   RoundingMethod roundingMethod = RoundingMethod.None)
		{
			Type = type;
			RoundingMethod = roundingMethod;
		}

		public abstract double Apply(StatService statService, double value);
		public abstract override string ToString();
	}

	public class StatModifier : Modifier
	{
		public readonly StatId StatId;

		public StatModifier(ModifierType type,
							StatId statId,
							RoundingMethod roundingMethod = RoundingMethod.None)
			: base(type, roundingMethod)
		{
			StatId = statId;
		}

		public override double Apply(StatService statService, double value) 
			=> RoundingMethod.Convert(Type.Apply(value, statService.Get(StatId)));

		public override string ToString() => $"{Type} {StatId}";
	}

	public class StaticModifier : Modifier
	{
		public readonly double Modifier;

		[JsonConstructor]
		public StaticModifier(ModifierType type,
							  double modifier,
							  RoundingMethod roundingMethod = RoundingMethod.None) 
			: base(type, roundingMethod)
		{
			Modifier = modifier;
		}

		public override double Apply(StatService statService, double value)
			=> RoundingMethod.Convert(Type.Apply(value, Modifier));

		public override string ToString() => $"{Type} {Modifier}";
	}
}