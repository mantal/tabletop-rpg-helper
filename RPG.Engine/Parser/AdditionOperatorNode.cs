using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class AdditionOperatorNode : Node
	{
		private readonly char _symbol;

		public AdditionOperatorNode(StatService statService, NodeType type) 
			: base(statService, type, 2)
		{
			if (type == NodeType.PlusOperator)
				_symbol = '+';
			else if (type == NodeType.MinusOperator)
				_symbol = '-';
			else
				throw new ArgumentException();
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			var errors = new List<string>();

			if (token.Next == null)
				return new [] { $"Expected operand after operator '{_symbol}' but found nothing" };

			var next = token.Next.Value;

			if (next is GrammarNode grammarNode)
			{
				errors.Add($"Expected operand after operator '{_symbol}' but found '{grammarNode.Text}'");
				return errors;
			}
			
			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var previous = node.Previous?.Value as ValueNode;
			var isBinary = previous != null;

			var a = 0d;
			if (isBinary)
				a = previous.GetValue();

			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var b = ((ValueNode)node.Next.Value).GetValue();

			var value = new NumberNode(StatService, Type == NodeType.PlusOperator ? a + b : a - b);

			var result = node.List.AddAfter(node.Next, value);

			if (isBinary)
				result.List.Remove(result.Previous.Previous.Previous);
			result.List.Remove(result.Previous.Previous);
			result.List.Remove(result.Previous);

			return result;
		}

		public override string ToString() => _symbol.ToString();
	}
}