using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
	public class ArgumentSeparatorNode : Node
	{
		public ArgumentSeparatorNode(string text, NodeType type)
			: base(text, type, -1)
		{ }

		public override bool IsValidLeftOperand() => false;

		public override bool IsValidRightOperand() => false;

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			if (node.Previous == null)
				return new[] { $"Expected argument before argument separator ',' but found nothing" };
			if (!node.Previous.Value.IsValidLeftOperand())
				return new[] { $"Expected argument before argument separator ',' but found {node.Previous.Value}"};
			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> throw new InvalidOperationException($"{nameof(Apply)} should not be called on {nameof(ArgumentSeparatorNode)}");

		public override Node Clone() => new ArgumentSeparatorNode(Text, Type);
	}
}