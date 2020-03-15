using System;

namespace RPG.Services
{
	public enum IntConversionMethod
	{
		NoConversion = 0,
		Floor = 1,
		Ceiling = 2,
	}

	public static class IntConversion
	{
		public static double Convert(this IntConversionMethod method, double value)
			=> method switch
			   {
				   IntConversionMethod.Floor        => Math.Floor(value),
				   IntConversionMethod.Ceiling      => Math.Ceiling(value),
				   IntConversionMethod.NoConversion => value,
				   _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
			   };
	}
}