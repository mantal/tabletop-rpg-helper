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

		public Stat Get(StatId id) => Stats[id];

		public double GetValue(StatId id)
		{
			if (id.InnerId != null) return Stats[id].GetInner(id);
			if (_cache.ContainsKey(id)) return _cache[id];

			var stat = Stats[id];
			var value = .0;

			if (stat.Modifiers.Any())
			{
				var modifiers = new LinkedList<Modifier>(stat.Modifiers);
				var priority = 1;

				while (modifiers.Count > 1)
				{
					var modifier = modifiers.Last;
					while (modifier != null && modifier.Value.Type.Priority > priority)
						modifier = modifier.Previous;
					if (modifier?.Previous == null)
					{
						priority++;
						if (priority > ModifierType.MinPriority)
							break;
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

		//todo reflechier a la de/serialization
		public IEnumerable<string> Add(string id, double @base, string? rawModifiers = null)
		{
			var errors = Add(id, $"+ :base {rawModifiers}");

			if (errors.Any())
				return errors;

			Get(id).AddOrUpdateInner("base", @base);

			return errors;
		}

		public IEnumerable<string> Add(string id, IEnumerable<(string, double)> innerStats, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();

			if (!id.IsValidStatId())
			{
				//TODO better msg
				errors = errors.Append($"Invalid stat id: {id}");
				return errors;
			}
			if (Exists(id))
				errors = errors.Append($"Stat already exists: {id}");

			errors = errors.Concat(Stat.FromString(out var stat, id, rawModifiers)).ToList();
			if (stat == null)
				return errors;

			errors = errors.Concat(AreModifiersValid(stat));

			if (!errors.Any())
				Stats.Add(stat.Id, stat);

			return errors;
		}

		public IEnumerable<string> Update(StatId id, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();

			if (!Exists(id)) 
				errors = errors.Append($"{id} does not exists"); //throw??

			errors = errors.Concat(Stat.FromString(out var stat, id, rawModifiers)).ToList();
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

		public bool Exists(StatId id)
		{
			if (!Stats.ContainsKey(id))
				return false;
			if (id.InnerId == null)
				return true;
			return Stats[id.Id].TryGetInner(id.InnerId) != null;
		}

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