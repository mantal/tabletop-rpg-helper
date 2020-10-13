using System;

namespace RPG.Engine.Utils
{
    public static class NumberExtensions
	{
		public static bool IsCloseTo(this double d, double t)
			=> Math.Abs(d - t) < 0.001;

		public static bool ToBool(this double d)
			=> !d.IsCloseTo(0);

		public static double ToDouble(this bool b)
			=> b ? 1 : 0;
	}
}
