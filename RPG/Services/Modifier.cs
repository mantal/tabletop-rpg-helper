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
		public readonly StatId? InnerStatId;

		public StatModifier(ModifierType type,
							StatId statId,
							StatId? innerStatId = null,
							RoundingMethod roundingMethod = RoundingMethod.None)
			: base(type, roundingMethod)
		{
			StatId = statId;
			InnerStatId = innerStatId;
		}

		public override double GetValue(StatService stats)
		{
			if (InnerStatId == null)
				return stats.GetValue(StatId);
			return stats.Get(StatId).GetInner(InnerStatId);
		}

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

		public override double GetValue(StatService statService) => Modifier;

		public override string ToString() => $"{Type} {Modifier}";
	}
}