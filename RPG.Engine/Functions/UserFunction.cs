using System;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Parser;

namespace RPG.Engine.Functions
{
	public record UserFunction : Function
	{
		private readonly Expression _expression;

		public UserFunction(FunctionId id, Expression expression)
			: base(id)
		{
			RequiredParameterNumber = GetArgumentCount(expression);
			MaxParameterNumber = RequiredParameterNumber;
			ParameterBatchSize = null;
			ParameterTypes = Enumerable.Repeat(ArgumentType.Number, RequiredParameterNumber).ToList().AsReadOnly();
			_expression = expression;
		}

		public override double Execute(object[] args)
		{
			if (args.Length < RequiredParameterNumber)
				throw new ArgumentException($"not enough argument to call function {Id}, required {RequiredParameterNumber} but got {args.Length}", nameof(args));
			
			var result = _expression.Resolve();

			return result;
		}

		private int GetArgumentCount(Expression expression)
		{
			var args = expression.FlatNodes.OfType<FunctionNode>()
								 .Where(node => ArgumentRegex.IsMatch(node.Id.Id))
								 .Select(node => int.Parse(node.Id.Id[1..]))
								 .OrderBy(i => i)
								 .Distinct();

			//TODO handle errors: $0
			//TODO handle errors: $1 $2 $4

			return args.Any() ? args.Max(i => i) : 0;
		}
	}
}