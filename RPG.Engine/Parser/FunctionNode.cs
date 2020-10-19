using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Functions;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class FunctionNode : ValueNode
	{
		public FunctionId Id { get; }
		public Expression[] Arguments { get; private set; } = Array.Empty<Expression>();

		private readonly FunctionService _functionService;
		private int _argumentCount = -1;
		private FunctionId? _parentId;
		
		//todo quand impl: update une fonction ne peut pas changer son nombre d'args
		public FunctionNode(FunctionService functionService, string id, FunctionId? parentId)
			: base(id, NodeType.Function, 1)
		{
			_functionService = functionService;
			Id = new FunctionId(id);
			_parentId = parentId;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			var errors = base.IsValid(node).ToList();

			_argumentCount = CountArguments(node.Next);

			if (!_functionService.Exists(Id))
			{
				if (Id == _parentId)
					return errors;

				errors.Add($"Undefined function {Id}");
			}

			var function = _functionService.Get(Id);
			
			if (_argumentCount < function.RequiredParameterNumber
				|| (function.MaxParameterNumber != null
					&& _argumentCount > function.MaxParameterNumber))
				//TODO ajouter un warning quand un argument requis et le node suivant est + ou - incitant a ajouter des {}, ex: $ABS -2 devrait etre $ABS{-2}
				errors.Add($"Function {Id} should have {GetArgumentNumberErrorMessage(function)} but found {_argumentCount}");
			else if (function.ParameterBatchSize != null && (_argumentCount - function.RequiredParameterNumber) % function.ParameterBatchSize != 0)
				errors.Add($"Function {Id} have an invalid number of arguments ({_argumentCount}. Arguments after the {function.RequiredParameterNumber}th should come in batch of {function.ParameterBatchSize}");

			return errors;
		}

		public override LinkedListNode<Node> OnAfterValidation(LinkedListNode<Node> start)
		{
			Arguments = new Expression[_argumentCount];

			var node = start.Next;

			if (node == null)
				return start;
			if (node.Value.Type != NodeType.LeftBracket
				&& _argumentCount < 2)
			{
				if (_argumentCount == 0)
					return start;

				node = node.Value.OnAfterValidation(node)!;
				var arg = new LinkedList<Node>(new []{ node.Value });
				
				Arguments[0] = new Expression(arg);

				node.List!.Remove(node);

				return start;
			}

			node = node.Consume(); // remove {

			var i = 0;
			while (node != null
				   && node.Value.Type != NodeType.RightBracket)
			{
				var arg = new LinkedList<Node>();

				while (node != null
					   && node.Value.Type != NodeType.ArgumentDivider
					   && node.Value.Type != NodeType.RightBracket)
				{
					node = node.Value.OnAfterValidation(node)!;

					arg.AddLast(node.Value);

					node = node.Consume();
				}

				if (node != null && node.Value.Type == NodeType.ArgumentDivider) 
					node = node.Consume();

				Arguments[i] = new Expression(arg);
				i++;
			}

			if (node != null && node.Value.Type == NodeType.RightBracket)
			{
				node.List!.Remove(node);
				// make sure we don't accidentally reuse the node after it's removed
				node = null;
			}

			return start;
		}

		public override double GetValue()
		{
			var function = _functionService.Get(Id);
			IEnumerable<object> args = new List<object>();
			if (Arguments.Length > function.RequiredParameterNumber)
			{
				args = function.ParameterTypes.Take(Arguments.Length)
								   .Select((type, i) => ConvertArgument(type, Arguments[i]))
								   .ToList();


				if (args.Count() < Arguments.Length)
				{
					var batch = function.ParameterTypes.Skip(args.Count() - (function.ParameterBatchSize ?? 1)).ToArray();
					args = args.Concat(Arguments.Skip(args.Count())
												.Select((arg, i) => ConvertArgument(batch[i % batch.Length], arg)))
							   .ToList();
				}
			}
			else
			{
				args = Arguments.Select((e, i) => function.ParameterTypes[i] == typeof(double) ? (object)e.Resolve() : e);
			}

			return _functionService.Execute(Id, args.ToArray());
		}

		public override string ToString()
		{
			if (_argumentCount == 0 || Arguments.Length == 0)
				return Id.ToString();
			if (_argumentCount == 1
				&& (Arguments[0].Nodes.First!.Value is not FunctionNode f
					|| f.Arguments.Length == 0
					|| f.Arguments[0].Nodes.Count != 0))
				return $"{Id} {Arguments[0]}";

			return $"{Id}{{{string.Join(", ", Arguments.Select(a => a.ToString()))}}}";
		}

		public override Node Clone() => new FunctionNode(_functionService, Id.Id, _parentId);

		private string GetArgumentNumberErrorMessage(Function function)
		{
			if (function.RequiredParameterNumber == 0)
			{
				if (function.MaxParameterNumber == null)
					return "any number of arguments";
				if (function.MaxParameterNumber == 0)
					return "no argument";
				return $"up to {function.MaxParameterNumber} arguments";
			}

			if (function.MaxParameterNumber == null)
				return $"at least {function.RequiredParameterNumber} arguments";
			if (function.RequiredParameterNumber == function.MaxParameterNumber)
				return $"exactly {function.RequiredParameterNumber} arguments";

			return $"{function.RequiredParameterNumber} to {function.MaxParameterNumber} arguments";
		}

		private int CountArguments(LinkedListNode<Node>? token)
		{
			if (token == null)
				return 0;
			if (token.Value.IsValidRightOperand()) //$FUNC +2 TODO
				return 1;
			if (token.Value.Type != NodeType.LeftBracket)
				return 0;

			token = token.Next;
			if (token == null)
				return 0;
			if (token.Value.Type == NodeType.RightBracket)
				return 0;

			var bracketCount = 0;
			var argCount = 1;
			for (; token?.Next != null && bracketCount >= 0; token = token.Next)
			{
				if (token.Value.Type == NodeType.ArgumentDivider
					&& bracketCount == 0)
					argCount++;
				if (token.Value.Type == NodeType.LeftBracket)
					bracketCount++;
				if (token.Value.Type == NodeType.RightBracket)
					bracketCount--;
			}

			return argCount;
		}

		private object ConvertArgument(Type type, Expression arg)
			=> type == typeof(double)
				   ? (object) arg.Resolve()
				   : arg;
	}
}