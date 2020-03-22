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

		public abstract double GetValue(StatService statService);
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

		public override double GetValue(StatService stats) => stats.GetValue(StatId);

		public override string ToString() => $"{Type} {StatId}";
	}

	public class VariableModifier : Modifier
	{
		public readonly VariableId VariableId;

		public VariableModifier(ModifierType type,
								VariableId variableId,
								RoundingMethod roundingMethod = RoundingMethod.None)
			: base(type, roundingMethod)
		{
			VariableId = variableId;
		}

		public override double GetValue(StatService stats) => stats.GetValue(VariableId);

		public override string ToString() => $"{Type} {VariableId}";
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

		public override double GetValue(StatService statService) => Modifier;

		public override string ToString() => $"{Type} {Modifier}";
	}
}