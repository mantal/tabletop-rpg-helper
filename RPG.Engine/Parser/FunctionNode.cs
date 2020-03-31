using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class FunctionNode : ValueNode
	{
		public FunctionId Id { get; }
		private readonly FunctionService _functionService;
		private int _argumentCount = -1;
		public Expression[] Arguments { get; private set; } = new Expression[0];

		//todo quand impl: update une fonction ne peut pas changer son numbre d'args
		public FunctionNode(StatService statService, FunctionService functionService, string id)
			: base(statService, NodeType.Function, 1)
		{
			_functionService = functionService;
			Id = new FunctionId(id);
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			if (!_functionService.Exists(Id)) 
				return new [] { $"Undefined function {Id}" };

			var function = _functionService.Get(Id);

			_argumentCount = CountArguments(token.Next);
			if (_argumentCount < function.RequiredParameterNumber
				|| (function.MaxParameterNumber != null
					&& _argumentCount > function.MaxParameterNumber))
				return new[] { $"Function {Id} should have {GetArgumentNumberErrorMessage(function)} but found {_argumentCount}" };

			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Transform(LinkedListNode<Node> start)
		{
			LinkedListNode<Node>? token = start;

			if (token.Next != null && token.Next.Value.Type == NodeType.LeftBracket) 
				token.List.Remove(token.Next);

			token = token.Next;
			Arguments = new Expression[_argumentCount];

			var i = 0;
			while (token != null
				   && token.Value.Type != NodeType.RightBracket)
			{
				var arg = new LinkedList<Node>();

				while (token != null
					   && token.Value.Type != NodeType.ArgumentDivider
					   && token.Value.Type != NodeType.RightBracket)
				{
					token = token.Value.Transform(token);

					arg.AddLast(token.Value);

					token = token.Consume();

				}

				if (token != null && token.Value.Type == NodeType.ArgumentDivider) 
					token = token.Consume();

				Arguments[i] = new Expression(arg);
				i++;
			}

			if (token != null && token.Value.Type == NodeType.RightBracket)
			{
				token.List.Remove(token);
				// make sure we don't accidentally reuse token after it's removed
				token = null;
			}

			return start;
		}

		public override double GetValue()
		{
			var args = Arguments.Select(a => a.Resolve()).ToArray();

			return _functionService.Execute(Id, args);
		}

		public override string ToString()
		{
			if (_argumentCount == 0)
				return Id.ToString();
			if (_argumentCount == 1)
				return $"{Id} {Arguments[0]}";
			return $"{Id}{{{string.Join(", ", Arguments.Select(a => a.ToString()))}}}";
		}

		public override bool IsExpression() => true;

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

		private int CountArguments(LinkedListNode<Node> token)
		{
			if (token == null)
				return 0;
			if (token.Value.IsExpression()) //$FUNC +2 TODO
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
	}
}