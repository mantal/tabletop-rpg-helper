using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPG.Services
{
	public class Stat
	{
		public StatId Id { get; }
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

		public override string ToString() => $"{Base}" + Modifiers.Aggregate("", (res, m) => res + " " + m);

		public Stat(StatId id, double @base = 0)
		{
			Id = id;
			Base = @base;
		}
		
		public static IEnumerable<string> FromString(out Stat? stat, string id, double @base = 0, string? rawModifiers = null)
		{
			var errors = new List<string>();
			
			stat = new Stat(id, @base);
			if (string.IsNullOrWhiteSpace(rawModifiers))
				return errors;

			rawModifiers = rawModifiers.Replace("+", " + ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("-", " - ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("*", " * ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("/", " / ", StringComparison.InvariantCultureIgnoreCase)
									   .Trim()
				; // Cleanup
				  // Add implicit +
			if (ModifierType.FromString(rawModifiers[0].ToString()) == null)
				rawModifiers = "+ " + rawModifiers;

			var tokens = rawModifiers.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < tokens.Length; i++)
			{
				var type = ModifierType.FromString(tokens[i]);
				if (type == null)
				{
					errors.Add($"Unknown operator: {tokens[i]}, expected one of: '+', '-', '*', '/'");
					return errors;
				}
				if (i + 1 >= tokens.Length)
				{
					errors.Add($"Missing identifier after {tokens[i]}");
					return errors;
				}

				i++;
				var modRef = tokens[i];
				var isId = modRef.IsValidStatId();
				var isNumber = double.TryParse(modRef, NumberStyles.Float, null, out var modValue);
				if (!isId && !isNumber)
				{
					errors.Add($"Expected a stat id or a number after {type} but found {modRef}");
					return errors;
				}

				if (isId)
					stat.Modifiers = stat.Modifiers.Append(new StatModifier(type, modRef));
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (isNumber)
					stat.Modifiers = stat.Modifiers.Append(new StaticModifier(type, modValue));
			}

			return errors;
		}
	}
}