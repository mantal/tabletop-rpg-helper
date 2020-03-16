using System;

namespace RPG.Services
{
	public enum RoundingMethod
	{
		None = 0,
		Floor = 1,
		Ceiling = 2,
	}

	public static class IntConversion
	{
		public static double Convert(this RoundingMethod method, double value)
			=> method switch
			   {
				   RoundingMethod.None => value,
				   RoundingMethod.Floor => Math.Floor(value),
				   RoundingMethod.Ceiling      => Math.Ceiling(value),
				   _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
			   };
	}
}