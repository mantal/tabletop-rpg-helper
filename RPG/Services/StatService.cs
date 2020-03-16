using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RPG.Services
{
	public class StatService
    {
        public IDictionary<string, Stat> Stats = new Dictionary<string, Stat>
        {
            {
                "CON",
                new Stat
                {
                    Base = 10,
                }
            },
			{
                "FOR",
                new Stat
				{
                    Base = 10,
				}
			},
            {
                "HP",
                new Stat
                {
                    Base = 10,
                    Modifiers = new [] { new StatModifier(ModifierType.Add, "CON"), },
                }
            },
			{
				"ATT",
				new Stat
				{
					Base = 100,
					Modifiers = new Modifier[]
					{
						new StatModifier(ModifierType.Add, "FOR"),
						new StaticModifier(ModifierType.Mult, 2), //todo fix prio
					},
				}
			},
        };
        private readonly IDictionary<string, double> _cache = new Dictionary<string, double>();

        public double Get(StatId id)
		{
			if (_cache.ContainsKey(id)) return _cache[id];

            var stat = Stats[id];
            
			var result = stat.Base;
			foreach (var modifier in stat.Modifiers)
			{
				result = modifier.Apply(this, result);
			}

			var value = stat.RoundingMethod.Convert(result);

            _cache.Add(id, value);
			return value;
		}

		public bool Add(StatId id, Stat stat)
		{
            _cache.Clear();
			return Stats.TryAdd(id, stat);
		}

		public IEnumerable<string> Add(string id, double @base = 0, string? rawModifiers = null)
		{
			var errors = new List<string>();

			if (string.IsNullOrWhiteSpace(id))
			{
				errors.Add($"Name can not be empty");
				return errors;
			}
			if (!id.All(char.IsLetter))
			{
				errors.Add($"Name ({id}) must be letters only");
				return errors;
			}
			if (Exists(id))
			{
				errors.Add($"Stat already exists: {id}");
				return errors;
			}

			var stat = new Stat { Base = @base, };
			if (string.IsNullOrWhiteSpace(rawModifiers))
			{
				Add(id, stat);
				return errors;
			}

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
				{
					if (!Exists(modRef))
					{
						errors.Add($"Unknown stat id: {modRef}");
						return errors;
					}
					stat.Modifiers = stat.Modifiers.Append(new StatModifier(type, modRef));
				}
				else if (isNumber)
					stat.Modifiers = stat.Modifiers.Append(new StaticModifier(type, modValue));
			}

			Add(id, stat);
			return errors;
		}

		public bool Exists(StatId id) => Stats.ContainsKey(id);
	}
}