using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Modifiers;

namespace RPG.Engine.Services
{
	//TODO check if string overloads are useful outside of tests
	public class StatService
	{
		public IDictionary<StatId, Stat> Stats = new Dictionary<StatId, Stat>();
		private readonly IDictionary<StatId, double> _cache = new Dictionary<StatId, double>();

		public Stat Get(string id) => Stats[(StatId) id];

		public Stat Get(StatId id) => Stats[id];

		public double GetValue(string id)
		{
			if (id.IsValidStatId())
				return GetValue((StatId) id);
			return Get(((VariableId) id).StatId).GetVariable((VariableId) id);
		}

		public double GetValue(VariableId id) => Stats[id.StatId].GetVariable(id);

		public double GetValue(StatId id)
		{
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

		public IEnumerable<string> Add(string id, double @base, string? rawModifiers = null)
		{
			var errors = Add(id, rawModifiers);
			if (errors.Any())
				return errors;
			Get((StatId) id).AddOrUpdateVariable(new VariableId(":base", (StatId) id), @base);
			var stat = Get(id);
			stat.Modifiers = stat.Modifiers.Prepend(new VariableModifier(ModifierType.Add, new VariableId(":base", (StatId) id)));

			return errors;
		}

		public IEnumerable<string> Add(string id, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();

			if (!id.IsValidStatId())
			{
				//TODO better msg
				errors = errors.Append($"Invalid stat id: {id}");
				return errors;
			}
			return errors.Concat(Add((StatId) id, rawModifiers));
		}

		public IEnumerable<string> Add(StatId id, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();
			
			if (Exists(id))
				errors = errors.Append($"Stat already exists: {id}");

			errors = errors.Concat(Stat.FromString(out var stat, id.ToString(), rawModifiers)).ToList();
			if (stat == null)
				return errors;

			errors = errors.Concat(AreModifierTargetsValid(stat));

			if (!errors.Any())
				Stats.Add(stat.Id, stat);

			return errors;
		}

		public IEnumerable<string> AddOrUpdate(VariableId id, double value)
		{
			var stat = Stats[id.StatId];

			if (stat.TryGetVariable(id) != null)
				_cache.Clear();
			stat.AddOrUpdateVariable(id, value);

			return System.Array.Empty<string>();
		}

		public IEnumerable<string> Update(string id, string? rawModifiers = null)
			=> Update((StatId) id, rawModifiers);

		public IEnumerable<string> Update(StatId id, string? rawModifiers = null)
		{
			IEnumerable<string> errors = new List<string>();

			if (!Exists(id)) 
				errors = errors.Append($"{id} does not exists"); //throw??

			errors = errors.Concat(Stat.FromString(out var stat, id.ToString(), rawModifiers)).ToList();
			if (stat == null)
				return errors;

			errors = errors.Concat(AreModifierTargetsValid(stat));

			if (!errors.Any())
			{
				Stats[id] = stat;
				_cache.Clear();
			}

			return errors;
		}

		public IEnumerable<string> Remove(string id, bool cascade = false)
			=> Remove((StatId)id, cascade);

		public IEnumerable<string> Remove(StatId id, bool cascade = false)
		{
			IEnumerable<string> errors = new List<string>();

			if (!Exists(id))
				errors = errors.Append($"{id} does not exists"); //throw??

			var deps = Stats.Where(s => s.Value.Id != id)
							.Where(s => s.Value.Modifiers.Any(m =>
							{
								if (m is StatModifier sm && sm.StatId == id) 
									return true;
								if (m is VariableModifier vm && vm.VariableId.StatId == id)
									return true;
								return false;
							}))
							.Select(s => s.Value.Id)
							.ToArray();

			if (!deps.Any())
			{
				Stats.Remove(id);
				return errors;
			}

			if (!cascade)
				return errors.Concat(deps.Select(depId => $"Cannot remove {depId} because {depId} depends on it"));

			errors = errors.Concat(deps.SelectMany(stat => Remove(stat, true)));
			Stats.Remove(id);
			_cache.Remove(id);

			return errors;
		}

		public bool Exists(string id)
		{
			if (id.IsValidStatId())
				return Exists((StatId) id);
			return Exists((VariableId) id);
		}

		public bool Exists(StatId id) => Stats.ContainsKey(id);

		public bool Exists(VariableId id)
		{
			if (!Exists(id.StatId)) 
				return false;
			return Stats[id.StatId].TryGetVariable(id) != null;
		}

		private IEnumerable<string> AreModifierTargetsValid(Stat stat)
		{
			var errors = stat.Modifiers.Select(m =>
				  {
					  if (m is StatModifier statMod)
					  {
						  if (!Exists(statMod.StatId))
							return $"Undefined stat: {statMod.StatId}";
					  }
					  else if (m is VariableModifier varMod && varMod.VariableId.StatId != stat.Id)
					  {
						  if (!Exists(varMod.VariableId.StatId))
							  return $"Undefined stat: {varMod.VariableId.StatId}";
						  if (!Exists(varMod.VariableId))
							  return $"Undefined variable: {varMod.VariableId}";
					  }
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
			var ids = stat.Modifiers.Select(m => m switch
												 {
													 StatModifier statMod => statMod.StatId,
													 VariableModifier varMod when varMod.VariableId.StatId != stat.Id 
														=> varMod.VariableId.StatId,
													 _ => null
												 })
						  .Where(id => id != null)
#pragma warning disable CS8604 // Possible null reference argument.
						  .SelectMany(id => IsRecursive(Stats[id], stack))
#pragma warning restore CS8604 // Possible null reference argument.
						  .ToList();
			stack.Pop();

			return ids;
		}
	}
}