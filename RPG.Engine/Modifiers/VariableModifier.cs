using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Modifiers
{
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
}