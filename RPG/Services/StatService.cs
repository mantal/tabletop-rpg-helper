using System.Collections.Generic;
using System.Linq;
using Hjson;
using Newtonsoft.Json;

namespace RPG.Services
{
	public class StatService
	{
		public IDictionary<string, Stat> Stats = new Dictionary<string, Stat>();
		private readonly IDictionary<string, double> _cache = new Dictionary<string, double>();

		public double GetValue(StatId id)
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

		public Stat Get(StatId id) => Stats[id];

		public IEnumerable<string> Add(string id, double @base = 0, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();
			if (string.IsNullOrWhiteSpace(id))
			{
				errors = errors.Append($"Name can not be empty");
				return errors;
			}
			if (!id.All(char.IsLetter))
			{
				errors = errors.Append($"Name ({id}) must be letters only");
				return errors;
			}
			if (Exists(id))
				errors = errors.Append($"Stat already exists: {id}");

			errors = errors.Concat(Stat.FromString(out var stat, id, @base, rawModifiers)).ToList();
			if (stat == null)
				return errors;

			errors = errors.Concat(AreModifiersValid(stat));

			if (!errors.Any())
				Stats.Add(stat.Id, stat);

			return errors;
		}

		public IEnumerable<string> Update(StatId id, double @base = 0, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();

			if (!Exists(id)) 
				errors = errors.Append($"{id} does not exists"); //throw??

			errors = errors.Concat(Stat.FromString(out var stat, id, @base, rawModifiers)).ToList();
			if (stat == null)
				return errors;

			errors = errors.Concat(AreModifiersValid(stat));

			if (!errors.Any())
			{
				Stats[id] = stat;
				_cache.Clear();
			}

			return errors;
		}

		public IEnumerable<string> Remove(StatId id, bool cascade = false)
		{
			IEnumerable<string> errors = new List<string>();

			if (!Exists(id))
				errors = errors.Append($"{id} does not exists"); //throw??

			var deps = Stats.Where(s => s.Value.Id != id)
							.Where(s => s.Value.Modifiers.Any(m => m is StatModifier sm && sm.StatId == id))
							.Select(s => s.Key)
							.ToArray();

			if (!deps.Any())
			{
				Stats.Remove(id);
				return errors;
			}

			if (!cascade)
				return errors.Concat(deps.Select(depId => $"Cannot remove {depId} because {depId} depends on it"));

			errors = errors.Concat(deps.SelectMany(d => Remove(d, true)));
			Stats.Remove(id);
			_cache.Remove(id);

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
				var settings = new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					MissingMemberHandling = MissingMemberHandling.Error,
				};
				var stats = JsonConvert.DeserializeObject<IDictionary<string, Stat>>(HjsonValue.Parse(hjson), settings);

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

		private IEnumerable<string> AreModifiersValid(Stat stat)
		{
			var errors = stat.Modifiers.Select(m =>
				  {
					  if (m is StatModifier statMod && !Exists(statMod.StatId))
						  return $"Undefined stat: {statMod.StatId}";
					  return string.Empty;
				  }).Where(s => !string.IsNullOrEmpty(s));

			if (errors.Any())
				return errors;

			return errors.Concat(IsRecursive(stat));
		}

		private IEnumerable<string> IsRecursive(Stat stat)
		{
			return IsRecursive(stat, new Stack<StatId>());
		}

		private IEnumerable<string> IsRecursive(Stat stat, Stack<StatId> stack)
		{
			if (stack.Contains(stat.Id))
				return new[] { $"Circular dependency detected: {stack.Aggregate("", (res, id) => $"->{id}")}" };

			stack.Push(stat.Id);
			var ids = stat.Modifiers.Where(m => m is StatModifier)
						  .Cast<StatModifier>()
						  .SelectMany(m => IsRecursive(Stats[m.StatId], stack))
						  .ToList();
			stack.Pop();

			return ids;
		}
	}
}