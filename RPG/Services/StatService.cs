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
			var value = stat.RoundingMethod.Convert(stat.Base);

			if (stat.Modifiers.Any())
			{
				var modifiers = new LinkedList<Modifier>(stat.Modifiers);
				var modifier = modifiers.Last;
				var priority = 1;

				// List is not empty so First can't be null
#pragma warning disable CS8604 // Possible null reference argument.
				modifiers.AddBefore(modifiers.First, new StaticModifier(ModifierType.Add, stat.Base, RoundingMethod.None));
#pragma warning restore CS8604 // Possible null reference argument.
				while (modifiers.Count > 1)
				{
					while (modifier != null && modifier.Value.Type.Priority > priority)
						modifier = modifier.Previous;
					if (modifier?.Previous == null)
					{
						priority++;
						if (priority > ModifierType.MinPriority)
							break;
						modifier = modifiers.Last;
						continue;
					}

					var prev = modifier.Previous.Value;
					var current = modifier.Value;
					var a = prev.GetValue(this);
					var b = current.GetValue(this);
					var res = current.RoundingMethod.Convert(current.Type.Apply(a, b));
				
					modifiers.AddBefore(modifier.Previous, new StaticModifier(prev.Type, res));
					modifiers.Remove(prev);
					modifiers.Remove(current);
				}
				// Since count > 1 First can't be null
#pragma warning disable CS8602 // Dereference of a possibly null reference.
				value = stat.RoundingMethod.Convert(modifiers.First.Value.GetValue(this));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
			}
			
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
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (isNumber)
					stat.Modifiers = stat.Modifiers.Append(new StaticModifier(type, modValue));
			}

			Add(id, stat);
			return errors;
		}

		public bool Exists(StatId id) => Stats.ContainsKey(id);
	}
}