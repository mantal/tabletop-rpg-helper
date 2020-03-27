using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class MultiplierOperatorNode : Node
	{
		private readonly char _symbol;

		public MultiplierOperatorNode(StatService statService, NodeType type) 
			: base(statService, type, 3)
		{
			if (type == NodeType.MultiplierOperator)
				_symbol = '*';
			else if (type == NodeType.DivideOperator)
				_symbol = '/';
			else if (type == NodeType.ModuloOperator)
				_symbol = '%';
			else
				throw new ArgumentException();
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			var errors = new List<string>();

			if (token.Previous == null)
				return new[] { $"Expected operand before operator '{_symbol}' but found nothing" };

			var previous = token.Previous.Value;
			if (previous is GrammarNode previousNode)
			{
				errors.Add($"Expected operand before operator '{_symbol}' but found '{previousNode.Text}'");
				return errors;
			}

			if (token.Next == null)
				return new[] { $"Expected operand after operator '{_symbol}' but found nothing" };

			var next = token.Next.Value;
			if (next is GrammarNode nextNode)
			{
				errors.Add($"Expected operand after operator '{_symbol}' but found '{nextNode.Text}'");
				return errors;
			}

			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var a = ((ValueNode)node.Previous.Value).GetValue();
			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var b = ((ValueNode)node.Next.Value).GetValue();

			var x = Type switch
					{
						NodeType.MultiplierOperator => a * b,
						NodeType.DivideOperator     => a / b,
						NodeType.ModuloOperator     => a % b,
						_                           => throw new InvalidOperationException(),
					};

			var value = new NumberNode(StatService, x);

			var result = node.List.AddAfter(node.Next, value);

			result.List.Remove(result.Previous.Previous.Previous);
			result.List.Remove(result.Previous.Previous);
			result.List.Remove(result.Previous);

			return result;
		}

		public override string ToString() => _symbol.ToString();
	}
}