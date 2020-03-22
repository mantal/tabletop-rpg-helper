
using RPG.Engine.Services;

namespace RPG.Engine.Modifiers
{
	public abstract class Modifier
	{
		public readonly ModifierType Type;
		public readonly RoundingMethod RoundingMethod;
		
		protected Modifier(ModifierType type,
						   RoundingMethod roundingMethod = RoundingMethod.None)
		{
			Type = type;
			RoundingMethod = roundingMethod;
		}

		public abstract double GetValue(StatService statService);
		public abstract override string ToString();
	}
}