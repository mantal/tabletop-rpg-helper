using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPG.Services
{
	public class Stat
	{
		public int Base { get; set; }
		public IEnumerable<Modifier> Modifiers { get; set; } = new List<Modifier>();

		[JsonConverter(typeof(StringEnumConverter))]
		public IntConversionMethod IntConversionMethod
		{
			get => _intConversionMethod;
			set
			{
				if (value == IntConversionMethod.NoConversion) 
					throw new ArgumentOutOfRangeException(nameof(IntConversionMethod), value, "Stat must be converted to int");
				_intConversionMethod = value;
			}
		}

		private IntConversionMethod _intConversionMethod = IntConversionMethod.Ceiling;

		public override string ToString() => "{BASE}" + Modifiers.Aggregate("", (res, m) => res + " " + m);
	}
}