using RPG.Engine.Services;

namespace RPG.Engine.Modifiers
{
	public class StaticModifier : Modifier
	{
		public readonly double Modifier;

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