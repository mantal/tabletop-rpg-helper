using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class AdditionOperatorNode : Node
	{
		private readonly char _symbol;
		private readonly bool _isUnary;

		public AdditionOperatorNode(StatService statService, NodeType type) 
			: base(statService, type, 2)
		{
			switch (type)
			{
				case NodeType.PlusOperator:  
					_symbol = '+';
					_isUnary = false;
					break;
				case NodeType.UnaryPlusOperator:
					_symbol = '+';
					_isUnary = true;
					break;
				case NodeType.MinusOperator: 
					_symbol = '-';
					_isUnary = false;
					break;
				case NodeType.UnaryMinusOperator:
					_symbol = '-';
					_isUnary = true;
					break;
				default:
					throw new ArgumentException(nameof(type));
			}
		}

		public override LinkedListNode<Node> Transform(LinkedListNode<Node> token)
		{
			var isUnary = token.Previous == null || !token.Previous.Value.IsExpression();
			if (!isUnary)
				return token;

			var type = Type == NodeType.PlusOperator ? NodeType.UnaryPlusOperator : NodeType.UnaryMinusOperator;

			return new LinkedListNode<Node>(new AdditionOperatorNode(StatService, type));
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			var errors = new List<string>();

			if (token.Next == null)
				return new [] { $"Expected operand after operator '{_symbol}' but found nothing" };

			var next = token.Next.Value;

			if (!next.IsExpression())
			{
				errors.Add($"Expected operand after operator '{_symbol}' but found '{next}'");
				return errors;
			}
			
			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var previous = node.Previous?.Value;

			var a = 0d;
			if (!_isUnary)
				a = ((ValueNode)previous).GetValue();

			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var b = ((ValueNode)node.Next.Value).GetValue();

			var value = new NumberNode(StatService, Type == NodeType.PlusOperator ? a + b : a - b);

			var result = node.List.AddAfter(node.Next, value);

			if (!_isUnary)
				result.List.Remove(result.Previous.Previous.Previous);
			result.List.Remove(result.Previous.Previous);
			result.List.Remove(result.Previous);

			return result;
		}

		public override string ToString() => _symbol.ToString();

		public override bool IsExpression() => _isUnary;
	}
}