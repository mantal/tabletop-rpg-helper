using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPG.Services
{
	public class Stat
	{
		public double Base { get; set; }
		public IEnumerable<Modifier> Modifiers { get; set; } = new List<Modifier>();

		[JsonConverter(typeof(StringEnumConverter))]
		public RoundingMethod RoundingMethod
		{
			get => _roundingMethod;
			set
			{
				if (value == RoundingMethod.None) 
					throw new ArgumentOutOfRangeException(nameof(RoundingMethod), value, "Stat must be converted to int");
				_roundingMethod = value;
			}
		}

		private RoundingMethod _roundingMethod = RoundingMethod.Ceiling;

		public override string ToString() => "{BASE}" + Modifiers.Aggregate("", (res, m) => res + " " + m);
	}
}