using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;

namespace RPG.Engine.Services
{
	public class FunctionService
	{
		private readonly IDictionary<FunctionId, Function> _functions = new Dictionary<FunctionId, Function>()
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
		};

		public Function Get(FunctionId id) => _functions[id];

		public double Execute(FunctionId id, params double[] parameters) => _functions[id].Execute(parameters);

		public bool Exists(FunctionId id) => _functions.ContainsKey(id);
	}
}
