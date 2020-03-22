using System;

namespace RPG.Engine.Modifiers
{
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
		public const int MinPriority = 2;

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
	}
}