using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;

namespace RPG.Engine.Functions
{
	public class UserFunction : Function
	{
		private readonly Expression _expression;
		private readonly FunctionService _functionService;

		public UserFunction(FunctionId id,
							Expression expression,
							FunctionService functionService)
			: base(id)
		{
			RequiredParameterNumber = GetArgumentCount(expression);
			MaxParameterNumber = RequiredParameterNumber;
			ParameterBatchSize = null;
			ParameterTypes = Enumerable.Repeat(typeof(double), RequiredParameterNumber).ToList().AsReadOnly();
			_expression = expression;
			_functionService = functionService;
		}

		public override double Execute(object[] args)
		{
			if (args.Length < RequiredParameterNumber)
				throw new ArgumentException($"not enough argument to call function {Id}, required {RequiredParameterNumber} but got {args.Length}", nameof(args));


			_functionService.PrepareUserFunctionCall(args.ToDouble());

			var result = _expression.Resolve();

			return result;
		}

		private int GetArgumentCount(Expression expression)
		{
			var a = FlattenFunctionDependencies(expression).ToList();
			var args = a
					   .Where(node => ArgumentRegex.IsMatch(node.Id.Id))
					   .Select(node => int.Parse(node.Id.Id[1..]))
					   .OrderBy(i => i)
					   .Distinct();

			//TODO handle errors: $0
			//TODO handle errors: $1 $2 $4

			return args.Any() ? args.Max(i => i) : 0;
		}

		private IEnumerable<FunctionNode> FlattenFunctionDependencies(Expression expression)
		{
			foreach (var node in expression.Nodes.OfType<IParentNode>())
			{
				if (node is FunctionNode)
					yield return (FunctionNode) node;
				foreach (var functionNode in node.Children.SelectMany(FlattenFunctionDependencies))
					yield return functionNode;
			}
		}
	}
}