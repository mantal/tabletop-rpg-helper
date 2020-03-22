using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Modifiers
{
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
}