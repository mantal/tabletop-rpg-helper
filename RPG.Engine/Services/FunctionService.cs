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
		private readonly Random _random;
		private readonly IDictionary<FunctionId, Function> _functions;
		private readonly Stack<object[]> _stack;

		public FunctionService(Random random)
		{
			_random = random;
			_stack = new Stack<object[]>(new[] { Array.Empty<object>() }); // don't let the stack be empty
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
					new InMemoryFunction(new FunctionId("$D"), 1, 3, null,
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

											 if (args.Length == 3) 
												 result = Execute((Expression) args[2], new [] { (object) result });

											 return result;
										 })
				},
			};
		}

		public Function Get(FunctionId id)
		{
			if (!_functions.ContainsKey(id) && Function.ArgumentRegex.IsMatch(id.Id)) 
				AddUserFunctionArgument(int.Parse(id.Id[1..]));

			return _functions[id];
		}

		public IEnumerable<string> Add(Function function)
		{
			if (Exists(function.Id))
				return new[] { $"function already exists {function.Id}" };
			_functions[function.Id] = function;

			return Enumerable.Empty<string>();
		}

		public double Execute(FunctionId id, object[] parameters)
		{
			var function = _functions[id];

			if (function is InMemoryFunction)
				return function.Execute(parameters);

			_stack.Push(parameters.Concat(_stack.Peek().Skip(parameters.Length)).ToArray());

			var result = function.Execute(parameters);

			_stack.Pop();

			return result;
		}

		public double Execute(Expression expression, object[] parameters)
		{
			_stack.Push(parameters.Concat(_stack.Peek().Skip(parameters.Length)).ToArray());

			var result = expression.Resolve();

			_stack.Pop();

			return result;
		}

		public bool Exists(FunctionId id)
		{
			if (Function.ArgumentRegex.IsMatch(id.Id))
				return true;
			return _functions.ContainsKey(id);
		}

		private void AddUserFunctionArgument(int n)
			=> _functions[new FunctionId($"${n}")] = new InMemoryFunction(new FunctionId($"${n}"), 
																		  0,
																		  0,
																		  _ => (double)_stack.Peek()[n - 1]);
	}

	public static class FunctionServiceExtensions
	{
		public static double[] ToDouble(this IEnumerable<object> array)
			=> array.Cast<double>().ToArray();
	}
}
