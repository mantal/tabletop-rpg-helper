using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Functions;
using RPG.Engine.Ids;
using RPG.Engine.Utils;

namespace RPG.Engine.Services
{
	public class FunctionService
	{
		private static readonly Random _random = new Random();
		private readonly IDictionary<FunctionId, Function> _functions;

		public FunctionService()
		{
			_functions = new Dictionary<FunctionId, Function>
		{
			{
				new FunctionId("$ZERO"),
				new InMemoryFunction(new FunctionId("$ZERO"), 0, 0, _ => 0)
			},
			{
				new FunctionId("$ABS"),
				new InMemoryFunction(new FunctionId("$ABS"), 1, 1, parameters => Math.Abs((double)parameters[0]))
			},
			{
				new FunctionId("$MIN"),
				new InMemoryFunction(new FunctionId("$MIN"), 2, parameters => parameters.ToDouble().Min())
			},
			{
				new FunctionId("$MAX"),
				new InMemoryFunction(new FunctionId("$MAX"), 2, parameters => parameters.ToDouble().Max())
			},
			{
				new FunctionId("$FLOOR"),
				new InMemoryFunction(new FunctionId("$FLOOR"), 1, 1, parameters => Math.Floor((double)parameters[0]))
			},
			{
				new FunctionId("$CEILING"),
				new InMemoryFunction(new FunctionId("$CEILING"), 1, 1, parameters => Math.Ceiling((double)parameters[0]))
			},
			{
				// the first batch argument if the condition
				// the second is the success value
				// repeat until there's zero or one argument left
				// returns 0 or the last argument if there is one
				new FunctionId("$IF"),
				new InMemoryFunction(new FunctionId("$IF"), 2, null, null,
									 // use Expression for the condition argument because we can't yet express the optional final else argument being a number
									 // use Expression for branch arguments because they must no be evaluate if they are not taken as that would break recursion
									 new [] { typeof(Expression), typeof(Expression) },
									 args =>
									 {
										 var i = 0;
										 for (; i < args.Length; i += 2)
										 {
											 // is this an else statement
											 if (i == args.Length - 1)
												 return ((Expression) args[i]).Resolve();

											 var condition = ((Expression) args[i]).Resolve();
											 if (condition.ToBool())
												 return ((Expression) args[i + 1]).Resolve();
										 }

										 return 0;
									 })
			},
			{
				new FunctionId("$D"),
				new InMemoryFunction(new FunctionId("$D"), 1, 2, null,
									 new [] { typeof(double), typeof(double), typeof(Expression), },
									 args =>
									 {
										 var dArgs = args.Take(2).ToDouble();
										 //TODO generate a warning on runtime if one argument is not an integer
										 var number = args.Length == 1 ? 1 : (int)dArgs[0];
										 var dice = args.Length == 1 ? (int)dArgs[0] : (int)dArgs[1];

										 var result = (double)Enumerable.Range(1, number)
																.Select(_ => _random.Next(1, dice + 1))
																.Sum();


										 return result;
									 })
			},
		};
		}

		public Function Get(FunctionId id)
		{
			if (!_functions.ContainsKey(id) && Function.ArgumentRegex.IsMatch(id.Id)) 
				AddUserFunctionArgument(int.Parse(id.Id[1..]), 0);
			return _functions[id];
		}

		public IEnumerable<string> Add(Function function)
		{
			if (Exists(function.Id))
				return new[] { $"function already exists {function.Id}" };
			_functions[function.Id] = function;

			return Enumerable.Empty<string>();
		}

		public void PrepareUserFunctionCall(params double[] args)
		{
			for (var i = 0; i < args.Length; i++)
				AddUserFunctionArgument(i + 1, args[i]);
		}

		public void AddUserFunctionArgument(int n, double value)
			=> _functions[new FunctionId($"${n}")] = new InMemoryFunction(new FunctionId($"${n}"), 
																		  0,
																		  0,
																		  _ => value);

		public double Execute(FunctionId id, object[] parameters) 
			=> _functions[id].Execute(parameters);

		public bool Exists(FunctionId id)
		{
			if (Function.ArgumentRegex.IsMatch(id.Id))
				return true;
			return _functions.ContainsKey(id);
		}
	}

	public static class FunctionServiceExtensions
	{
		public static double[] ToDouble(this IEnumerable<object> array)
			=> array.Cast<double>().ToArray();
	}
}
