using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Parser;

namespace RPG.Engine.Services
{
	//TODO check if string overloads are useful outside of tests
	public class StatService
	{
		private readonly FunctionService _functionService;
		public readonly IDictionary<StatId, Stat> Stats = new Dictionary<StatId, Stat>();
		private readonly IDictionary<StatId, double> _cache = new Dictionary<StatId, double>();
		private readonly Parser.Parser _parser = new Parser.Parser();

		public StatService(FunctionService functionService)
		{
			_functionService = functionService;
		}

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

			var value = Stats[id].Resolve();

			_cache.Add(id, value);
			return value;
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

			var context = new ParsingContext
			{
				StatService = this,
				FunctionService = _functionService,
				StatId = id,
			};
			errors = errors.Concat(_parser.Parse(out var stat, context, id.ToString(), rawModifiers));
			if (stat == null)
				return errors;

			errors = errors.Concat(AreModifierTargetsValid(stat));

			if (errors.Any())
				return errors;

			Stats.Add(stat.Id, stat);

			return errors;
		}

		public IEnumerable<string> Add(Stat stat)
		{
			if (Exists(stat.Id))
				return new [] { $"Stat already exists: {stat.Id}" };

			Stats[stat.Id] = stat;

			return Enumerable.Empty<string>();
		}

		public IEnumerable<string> AddOrUpdate(VariableId id, double value = 0)
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
				errors = errors.Append($"{id} does not exists");

			var context = new ParsingContext
			{
				StatService = this,
				StatId = id,
			};
			errors = errors.Concat(_parser.Parse(out var stat, context, id.ToString(), rawModifiers));
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
							.Where(s => s.Value.Expressions.SelectMany(e => e.Nodes)
										 .Any(node =>
							{
								if (node is StatNode sn && sn.Id == id)
									return true;
								if (node is VariableNode vn && vn.Id.StatId == id)
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
			return IsRecursive(stat);
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

			var ids = stat.Expressions
						  .SelectMany(e => e.Nodes)
						  .SelectMany(node => node switch
												 {
													 StatNode statNode => new[] { statNode.Id },
													 VariableNode varNode when varNode.Id.StatId != stat.Id
														=> new[] { varNode.Id.StatId },
													 FunctionNode funcNode => funcNode
																			  .Arguments.SelectMany(
																				  a => a.Nodes.Select(n => n switch
																										   {
																											   StatNode statNode => statNode.Id,
																											   VariableNode varNode when varNode.Id.StatId != stat.Id
																											   => varNode.Id.StatId,
																											   _ => null!,
																										   })
																						.Where(id => id != null)),
													 _ => Enumerable.Empty<StatId>()
												 })
						  .Where(Exists)
						  .SelectMany(id => IsRecursive(Stats[id], stack))
						  .ToList();
			stack.Pop();

			return ids;
		}
	}
}