using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;

namespace RPG.Engine.Services
{
	public class FunctionService
	{
		private readonly IDictionary<FunctionId, Function> _functions = new Dictionary<FunctionId, Function>
		{
			{
				new FunctionId("$ZERO"),
				new Function(new FunctionId("$ZERO"), 0, 0, _ => 0)
			},
			{
				new FunctionId("$ABS"),
				new Function(new FunctionId("$ABS"), 1, 1, parameters => Math.Abs(parameters[0]))
			},
			{
				new FunctionId("$MIN"),
				new Function(new FunctionId("$MIN"), 2, parameters => parameters.Min())
			},
			{
				new FunctionId("$MAX"),
				new Function(new FunctionId("$MAX"), 2, parameters => parameters.Max())
			},
			{
				new FunctionId("$FLOOR"),
				new Function(new FunctionId("$FLOOR"), 1, 1, parameters => Math.Floor(parameters[0]))
			},
			{
				new FunctionId("$CEILING"),
				new Function(new FunctionId("$CEILING"), 1, 1, parameters => Math.Ceiling(parameters[0]))
			},
			{
				//TODO gerer des cas plus complexes que l'égalité / ajouter une valeur par défaut
				new FunctionId("$IFZ"),
				new Function(new FunctionId("$IFZ"), 3, null, 2, 
							 args =>
							 {
								 var v = args[0];
								 for (var i = 1; i < args.Length; i += 2)
								 {
									 if (Math.Abs(v - args[i]) < 0.0001)
										 return args[i + 1];
								 }

								 return 0;
							 })
			},
		};

		public Function Get(FunctionId id)
		{
			if (Function.ArgumentRegex.IsMatch(id.Id)) 
				AddArgumentFunction(int.Parse(id.Id[1..]), 0);
			return _functions[id];
		}

		public IEnumerable<string> Add(Function function)
		{
			if (Exists(function.Id))
				return new[] { $"function already exists {function.Id}" };
			_functions[function.Id] = function;

			return Enumerable.Empty<string>();
		}

		public void AddArgumentFunction(int n, double value)
			=> _functions[new FunctionId($"${n}")] = new Function(new FunctionId($"${n}"), 
				   0,
				   0,
				   _ => value);

		public double Execute(FunctionId id, params double[] parameters) => _functions[id].Execute(parameters);

		public bool Exists(FunctionId id)
		{
			if (Function.ArgumentRegex.IsMatch(id.Id))
				return true;
			return _functions.ContainsKey(id);
		}
	}
}
