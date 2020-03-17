using System.Collections.Generic;
using System.Linq;
using Hjson;
using Newtonsoft.Json;

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
			var res = Stats.TryAdd(id, stat);
			if (res)
				_cache.Clear();
			return res;
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

			errors = errors.Concat(Stat.FromString(out var stat, @base, rawModifiers)).ToList();

			if (stat == null)
				return errors;

			errors = errors.Concat(stat.Modifiers.Select(m =>
			{
				if (m is StatModifier statMod && !Exists(statMod.StatId))
					return $"Undefined stat: {statMod.StatId}";
				return string.Empty;
			}).Where(s => !string.IsNullOrEmpty(s)))
						   .ToList();

			if (!errors.Any())
				Add(id, stat);

			return errors;
		}

		public bool Exists(StatId id) => Stats.ContainsKey(id);

		public string Serialize()
		{
			var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.Ignore,
			});
			return JsonValue.Parse(json).ToString(Stringify.Hjson);
		}

		public string? Deserialize(string hjson)
		{
			try
			{
				var stats = JsonConvert.DeserializeObject<IDictionary<string, Stat>>(HjsonValue.Parse(hjson),
					new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore,
						MissingMemberHandling = MissingMemberHandling.Error,
					});
				if (stats == null)
					return "json returned null";
				Stats = stats;
				_cache.Clear();
			}
			catch (JsonSerializationException e)
			{
				return e.Message;
			}

			return null;
		}
	}
}