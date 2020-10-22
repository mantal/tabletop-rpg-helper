using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
	public class GrammarNode : Node
	{
		public GrammarNode(string text, NodeType type, int priority)
			: base(text, type, priority)
		{ }

		public override bool IsValidLeftOperand()
			=> Type == NodeType.RightBracket
			   || Type == NodeType.RightParenthesis;

		public override bool IsValidRightOperand() => false;
		
		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			if (node.Previous == null)
				return new[] { $"Expected expression before {Text} but found nothing" };
			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> throw new InvalidOperationException($"{nameof(Apply)} should not be called on {nameof(GrammarNode)}");

		public override Node Clone() => new GrammarNode(Text, Type, Priority);
	}
}