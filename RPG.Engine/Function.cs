using System;
using System.Linq;
using System.Text.RegularExpressions;
using RPG.Engine.Ids;
using RPG.Engine.Parser;

namespace RPG.Engine.Services
{
	public class Function
	{
		//TODO better regex
		public static readonly Regex ArgumentRegex =
			new Regex("^\\$[1-9]\\d?$", RegexOptions.Compiled | RegexOptions.Singleline);

		public FunctionId Id { get; }
		public int RequiredParameterNumber { get; }
		public int? MaxParameterNumber { get; }
		public int? ParameterBatchSize { get; }

		private readonly Func<double[], double>? _apply;
		private readonly Expression? _expression;
		private readonly FunctionService _functionService;
		
		public Function(FunctionId id,
						int requiredParameterNumber,
						Func<double[], double> apply)
			: this(id, requiredParameterNumber, null, null, apply)
		{ }

		public Function(FunctionId id,
						int requiredParameterNumber,
						int? maxParameterNumber,
						Func<double[], double> apply)
			: this(id, requiredParameterNumber, maxParameterNumber, null, apply)
		{ }

		public Function(FunctionId id,
						int requiredParameterNumber,
						int? maxParameterNumber,
						int? parameterBatchSize,
						Func<double[], double> apply)
		{
			if (parameterBatchSize > maxParameterNumber)
				throw new ArgumentOutOfRangeException(nameof(parameterBatchSize), $"{nameof(parameterBatchSize)} cannot be greater than {nameof(maxParameterNumber)}");
			Id = id;
			RequiredParameterNumber = requiredParameterNumber;
			MaxParameterNumber = maxParameterNumber;
			ParameterBatchSize = parameterBatchSize;
			_apply = apply;
		}

		public Function(FunctionId id,
						Expression expression,
						FunctionService functionService)
		{
			Id = id;
			RequiredParameterNumber = GetArgumentCount(expression);
			MaxParameterNumber = RequiredParameterNumber;
			ParameterBatchSize = null;
			_expression = expression;
			_functionService = functionService;
		}
		
		public double Execute(params double[] parameters)
		{
			if (parameters.Length < RequiredParameterNumber)
				throw new ArgumentException(nameof(parameters));

			if (_apply != null)
				return _apply(parameters);

			for (var i = 0; i < RequiredParameterNumber; i++)
				_functionService.AddArgumentFunction(i + 1, parameters[i]);

			var result = _expression!.Resolve();

			return result;
		}

		private int GetArgumentCount(Expression expression)
		{
			var args = expression.Nodes.Where(n => n is FunctionNode functionNode
										&& ArgumentRegex.IsMatch(functionNode.Id.Id))
								 .Select(n => int.Parse(((FunctionNode)n).Id.Id[1..]))
								 .OrderBy(i => i)
								 .Distinct();

			//TODO handle errors: $0
			//TODO handle errors: $1 $2 $4

			return args.Any() ? args.Max(i => i) : 0;
		}
	}
}